using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

public sealed class MetricSurface : Panel {
    public MetricSurface() { DoubleBuffered=true;BackColor=Color.FromArgb(18,32,48); }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);using(var pen=new Pen(Color.FromArgb(49,67,87)))e.Graphics.DrawLine(pen,Width/2,20,Width/2,Height-20);
    }
}

public sealed class HorizontalDashboard : Form {
    protected override bool ShowWithoutActivation { get { return renderOnly; } }
    readonly NotifyIcon percentTray,hoursTray;
    readonly System.Windows.Forms.Timer poll=new System.Windows.Forms.Timer(),clock=new System.Windows.Forms.Timer();
    readonly LiveUsage live=new LiveUsage();readonly UsageState usage=new UsageState();
    readonly Label percentage,hours,unit,cycle,reset,secondary,status,updated,badge;
    readonly Panel track,fill;
    readonly ToolStripMenuItem mainSummary,secondarySummary,timeSummary,sourceSummary;
    readonly string root;readonly bool renderOnly;
    DateTimeOffset attempted=DateTimeOffset.MinValue;
    string queryMessage="等待查询官方额度";bool busy,quitting;
    Icon percentIcon,hoursIcon;string percentKey,hoursKey;
    Image backgroundAsset;
    readonly Dictionary<Control,Font> baseFonts=new Dictionary<Control,Font>();
    readonly List<Font> scaledFonts=new List<Font>();float fontScale=1;
    readonly EventWaitHandle showSignal,exitSignal;
    RegisteredWaitHandle showWait,exitWait;
    static readonly Color White=Color.FromArgb(240,246,255),Muted=Color.FromArgb(163,180,201),Jade=Color.FromArgb(119,231,202),Amber=Color.FromArgb(255,204,125);
    public HorizontalDashboard(string sourceRoot,bool onlyRender=false) {
        root=sourceRoot;renderOnly=onlyRender;
        Text="Codex 剩余 / 重置小时 · 横向版 v3";ClientSize=new Size(1000,380);
        Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(7,15,28);ForeColor=White;
        AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
        FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
        DoubleBuffered=true;
        var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("UsageTray.Background");
        if(resource!=null)using(resource) { using(var img=Image.FromStream(resource))backgroundAsset=new Bitmap(img); }
        BackgroundImage=backgroundAsset;BackgroundImageLayout=ImageLayout.Stretch;
        AddLabel(this,"●",26,23,22,30,18,Jade,true);
        AddLabel(this,"CODEX",48,21,112,34,23,White,true,true);
        AddLabel(this,"额度仪表",166,24,150,32,18,Muted);
        badge=AddLabel(this,onlyRender?"离屏验证 · 测试数据":"等待查询",730,23,242,32,14,Amber,true);badge.TextAlign=ContentAlignment.MiddleRight;
        var metrics=new MetricSurface { Left=24,Top=78,Width=952,Height=160 };Controls.Add(metrics);
        AddLabel(metrics,"用量剩余",24,16,185,29,16,Muted);
        cycle=AddLabel(metrics,"主要额度 · 等待数据",208,19,245,26,14,Muted);
        percentage=AddLabel(metrics,"—",22,44,360,93,64,Jade,true,true);
        track=new Panel { Left=26,Top=138,Width=390,Height=5,BackColor=Color.FromArgb(40,58,76) };metrics.Controls.Add(track);
        fill=new Panel { Left=0,Top=0,Width=0,Height=5,BackColor=Jade };track.Controls.Add(fill);
        AddLabel(metrics,"距离重置",510,16,390,29,16,Muted);
        hours=AddLabel(metrics,"未知",506,44,300,93,64,White,true,true);
        unit=AddLabel(metrics,"小时",789,94,120,35,20,Muted);
        reset=AddLabel(metrics,"服务未提供有效重置时间",512,133,422,24,13,Muted);
        AddLabel(this,"次要额度",28,253,100,28,14,Muted,true);
        secondary=AddLabel(this,"等待查询",137,253,835,28,14,Muted);
        var divider=new Panel { Left=28,Top=291,Width=944,Height=1,BackColor=Color.FromArgb(38,54,72) };Controls.Add(divider);
        status=AddLabel(this,queryMessage,28,305,630,26,14,Muted);
        updated=AddLabel(this,"取数时间：无 · 每 60 秒查询，非实时推送",28,336,640,24,12,Muted);
        var refresh=ButtonAt("刷新",678,315);refresh.Click+=delegate { RefreshUsage(); };Controls.Add(refresh);
        var hide=ButtonAt("收起",779,315);hide.Click+=delegate { Hide(); };Controls.Add(hide);
        var exit=ButtonAt("退出",880,315);exit.Click+=delegate { Quit(); };Controls.Add(exit);
        CaptureFonts(this);
        if(onlyRender)return;
        var menu=new ContextMenuStrip();
        mainSummary=new ToolStripMenuItem { Enabled=false };secondarySummary=new ToolStripMenuItem { Enabled=false };
        timeSummary=new ToolStripMenuItem { Enabled=false };sourceSummary=new ToolStripMenuItem { Enabled=false };
        menu.Items.AddRange(new ToolStripItem[]{mainSummary,secondarySummary,timeSummary,sourceSummary,new ToolStripSeparator()});
        menu.Items.Add("显示横向仪表",null,delegate { Reveal(); });menu.Items.Add("立即刷新",null,delegate { RefreshUsage(); });
        menu.Items.Add("图标说明：% 剩余 / h 重置小时（向上取整）",null,delegate { Reveal(); });
        menu.Items.Add("退出仪表",null,delegate { Quit(); });menu.Opening+=delegate { RenderState(); };
        percentTray=new NotifyIcon { ContextMenuStrip=menu };hoursTray=new NotifyIcon { ContextMenuStrip=menu };
        percentTray.MouseClick+=delegate(object sender,MouseEventArgs e) { if(e.Button==MouseButtons.Left)Reveal(); };
        hoursTray.MouseClick+=delegate(object sender,MouseEventArgs e) { if(e.Button==MouseButtons.Left)Reveal(); };
        poll.Interval=60000;poll.Tick+=delegate { RefreshUsage(); };clock.Interval=1000;clock.Tick+=delegate { RenderState(); };
        Shown+=delegate { ApplyFontScale(DeviceDpi/96f);FitScreen();RenderState();percentTray.Visible=hoursTray.Visible=true;poll.Start();clock.Start();RefreshUsage(); };
        DpiChanged+=delegate(object sender,DpiChangedEventArgs e) { Post(delegate { ApplyFontScale(e.DeviceDpiNew/96f);FitScreen(); }); };
        FormClosing+=delegate(object sender,FormClosingEventArgs e) { if(!quitting && e.CloseReason==CloseReason.UserClosing) { e.Cancel=true;Hide(); } };
        showSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\CodexUsageTray_Show_v3");
        exitSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\CodexUsageTray_Exit_v3");
        showWait=ThreadPool.RegisterWaitForSingleObject(showSignal,delegate { Post(Reveal); },null,-1,false);
        exitWait=ThreadPool.RegisterWaitForSingleObject(exitSignal,delegate { Post(Quit); },null,-1,false);
    }
    void Post(Action action) { if(!IsDisposed && IsHandleCreated)try { BeginInvoke(action); }catch(InvalidOperationException) { } }
    void CaptureFonts(Control parent) { baseFonts[parent]=parent.Font;foreach(Control child in parent.Controls)CaptureFonts(child); }
    void ApplyFontScale(float factor) {
        var previous=scaledFonts.ToArray();scaledFonts.Clear();fontScale=factor;
        foreach(var pair in baseFonts) { var b=pair.Value;var font=new Font(b.FontFamily,b.Size*factor,b.Style,b.Unit);pair.Key.Font=font;scaledFonts.Add(font); }
        foreach(var old in previous)old.Dispose();
    }
    public void ScaleForValidation(float factor) { if(factor!=1)Scale(new SizeF(factor,factor));ApplyFontScale(factor); }
    void FitScreen() { var area=Screen.FromControl(this).WorkingArea;if(Width>area.Width || Height>area.Height) { float factor=Math.Min((area.Width-20f)/Width,(area.Height-20f)/Height);Scale(new SizeF(factor,factor));ApplyFontScale(fontScale*factor); } }
    static Label AddLabel(Control parent,string text,int x,int y,int w,int h,int px,Color color,bool bold=false,bool digits=false) {
        var label=new Label { Text=text,Left=x,Top=y,Width=w,Height=h,ForeColor=color,BackColor=Color.Transparent,AutoEllipsis=true,
            Font=new Font(digits?"Segoe UI":"Microsoft YaHei UI",px,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel),UseCompatibleTextRendering=true,AccessibleName=text };
        parent.Controls.Add(label);return label;
    }
    static Button ButtonAt(string text,int x,int y) {
        var button=new Button { Text=text,AccessibleName=text,Left=x,Top=y,Width=92,Height=36,Font=new Font("Microsoft YaHei UI",14,FontStyle.Regular,GraphicsUnit.Pixel),BackColor=text=="刷新"?Color.FromArgb(31,78,76):Color.FromArgb(25,40,57),ForeColor=text=="刷新"?Jade:White,FlatStyle=FlatStyle.Flat,TabStop=true };
        button.FlatAppearance.BorderSize=0;return button;
    }
    void Reveal() { Show();WindowState=FormWindowState.Normal;Activate(); }
    void Quit() { quitting=true;poll.Stop();clock.Stop();if(percentTray!=null)percentTray.Visible=hoursTray.Visible=false;Close(); }
    public void SetRenderState(Snapshot snapshot,bool failed,string message) { usage.Current=snapshot;usage.QueryFailed=failed;queryMessage=message;RenderState(); }
    static void Accessible(Label label,string text) { label.Text=text;label.AccessibleName=text; }
    void RenderState() {
        var now=DateTimeOffset.UtcNow;var s=usage.Current;var w=s==null?null:s.Primary??s.Secondary;
        bool cached=usage.Cached,expired=QuotaDisplay.Expired(w,now);
        Accessible(percentage,QuotaDisplay.Percent(w,now));percentage.ForeColor=cached?Amber:w!=null && w.Remaining<=10?Color.Salmon:Jade;
        Accessible(hours,QuotaDisplay.HoursText(w,now));unit.Visible=QuotaDisplay.Hours(w,now).HasValue && !expired;
        Accessible(cycle,(s!=null && s.Primary==null?"次要额度":"主要额度")+" · "+(w==null?"等待数据":QuotaDisplay.Duration(w.Minutes)));
        Accessible(reset,expired?"已到重置时间 · 等待官方新数据":QuotaDisplay.ResetText(w));
        Accessible(secondary,s!=null && s.Primary==null?"主要额度未提供；上方展示次要额度":QuotaDisplay.Summary(s==null?null:s.Secondary,now));
        fill.Width=w==null || expired?0:(int)Math.Round(track.ClientSize.Width*w.Remaining/100);fill.BackColor=percentage.ForeColor;
        Accessible(status,(s==null?"暂无可靠数据":s.Official?"官方额度接口":"本地日志快照（账户未核验）")+" · "+queryMessage+(cached?" · 缓存":"")+(expired?" · 待刷新":""));
        status.ForeColor=cached?Amber:Muted;
        Accessible(updated,(s==null?"取数时间：无":(s.Official?"成功取数 ":"日志记录 ")+s.At.ToLocalTime().ToString("MM-dd HH:mm:ss 'UTC'zzz"))+" · 60 秒查询，非实时推送");
        if(!renderOnly)Accessible(badge,cached?"缓存 · 以取数时间为准":expired?"到期 · 等待新数据":s==null?"等待查询":"官方查询 · 已连接");
        if(percentTray==null)return;
        var pct=QuotaDisplay.PercentIcon(w,now);var hr=QuotaDisplay.HoursIcon(w,now);
        Color pctColor=cached?Color.DarkGoldenrod:w!=null&&w.Remaining<=10?Color.IndianRed:Color.FromArgb(23,105,93);
        Color hourColor=cached?Color.DarkGoldenrod:Color.FromArgb(129,83,29);
        string pk=pct+"/"+pctColor.ToArgb(),hk=hr+"/"+hourColor.ToArgb();
        if(pk!=percentKey) { var old=percentIcon;percentIcon=TrayGraphic.Icon(pct,"%",pctColor);percentTray.Icon=percentIcon;Icon=percentIcon;percentKey=pk;if(old!=null)old.Dispose(); }
        if(hk!=hoursKey) { var old=hoursIcon;hoursIcon=TrayGraphic.Icon(hr,"h",hourColor);hoursTray.Icon=hoursIcon;hoursKey=hk;if(old!=null)old.Dispose(); }
        percentTray.Text=hoursTray.Text=QuotaDisplay.Tooltip(s,cached,now);
        mainSummary.Text="主要："+QuotaDisplay.Summary(s==null?null:s.Primary,now);
        secondarySummary.Text="次要："+QuotaDisplay.Summary(s==null?null:s.Secondary,now);
        timeSummary.Text=s==null?"取数时间：无":"取数："+s.At.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'zzz")+(cached?"（缓存）":"");
        sourceSummary.Text=s==null?"来源：无":s.Official?"来源：官方 account/rateLimits/read（CLI 当前账户）":"来源：本地会话快照（账户未核验）";
    }
    void RefreshUsage() {
        if(renderOnly||busy)return;
        if(DateTimeOffset.UtcNow-attempted<TimeSpan.FromSeconds(10))return;
        attempted=DateTimeOffset.UtcNow;busy=true;queryMessage="正在查询…";RenderState();
        ThreadPool.QueueUserWorkItem(delegate {
            Snapshot fresh=null,fallback=null;string message;
            try { fresh=live.Read();message="每 60 秒查询"; }
            catch(InvalidOperationException e) { message=e.Message;string ignored;try { if(usage.Current==null || !usage.Current.Official)fallback=Reader.Read(root,out ignored); }catch { } }
            Post(delegate { if(fresh!=null)usage.Accept(fresh);else usage.Fail(fallback);queryMessage=message;busy=false;RenderState(); });
        });
    }
    protected override void Dispose(bool disposing) {
        if(disposing) { poll.Dispose();clock.Dispose();live.Dispose();if(showWait!=null)showWait.Unregister(null);if(exitWait!=null)exitWait.Unregister(null);if(showSignal!=null)showSignal.Dispose();if(exitSignal!=null)exitSignal.Dispose();if(percentTray!=null)percentTray.Dispose();if(hoursTray!=null)hoursTray.Dispose();if(percentIcon!=null)percentIcon.Dispose();if(hoursIcon!=null)hoursIcon.Dispose();if(backgroundAsset!=null)backgroundAsset.Dispose(); }
        base.Dispose(disposing);
        if(disposing)foreach(var font in scaledFonts)font.Dispose();
    }
}
