using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Reflection;

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("CHATGPT USAGE METER Floating")]
[assembly: AssemblyVersion("0.4.2.0")]
[assembly: AssemblyFileVersion("0.4.2.0")]
internal static class MeterPalette {
    internal static readonly string[] Keys={"navy","charcoal","forest","plum"};
    internal static readonly Color[] Colors={Color.FromArgb(18,32,48),Color.FromArgb(30,30,30),Color.FromArgb(16,38,29),Color.FromArgb(38,24,43)};
    internal static bool Valid(string key) { return Array.IndexOf(Keys,key)>=0; }
    internal static Color Get(string key) { int i=Array.IndexOf(Keys,key);return Colors[i<0?0:i]; }
}
internal static class MeterPreferences {
    internal static string DirectoryOverride=null;
    static string DirectoryName { get { return DirectoryOverride??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CodexUsageMeter"); } }
    internal static string PathName { get { return Path.Combine(DirectoryName,"preferences.json"); } }
    static Dictionary<string,object> Read() { try { if(new FileInfo(PathName).Length<=1024)return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(PathName,Encoding.UTF8)); }catch { }return null; }
    internal static string Load() {var d=Read();object value;if(d!=null&&d.TryGetValue("language",out value))return (value as string)=="en"?"en":"zh-CN";
        try {string legacy=Path.Combine(DirectoryName,"language.txt");if(new FileInfo(legacy).Length<=32&&File.ReadAllText(legacy,Encoding.UTF8).Trim()=="en")return "en";}catch { }return "zh-CN";}
    internal static string LoadBackground() {var d=Read();object value;return d!=null&&d.TryGetValue("background",out value)&&MeterPalette.Valid(value as string)?(string)value:"navy";}
    internal static bool Save(string language,string background) { try {Directory.CreateDirectory(DirectoryName);File.WriteAllText(PathName,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {language=language,background=background}),Encoding.UTF8);return true;}catch {return false;} }
}
public sealed class FloatingUsage : SkinWindow {
    readonly SkinSettings skins=SkinSettings.Load("CodexUsageMeter");SkinMenus skinMenus;readonly SkinProgress progress=new SkinProgress();
    FlowLayoutPanel rootLayout,metricRow,leftMetric,rightMetric;Control leading,divider;float fontFactor=1;
    internal Snapshot TestSnapshot {get{return usage.Current;}} internal string TraySkin {get{return skins.Tray;}}internal string FloatingSkin {get{return skins.Window;}}
    internal void SelectSkins(string traySkin,string windowSkin){if(!SkinSettings.Valid(traySkin)||!SkinSettings.Valid(windowSkin))throw new ArgumentException();skins.Tray=traySkin;skins.Window=windowSkin;ChangeSkins();}
    void ChangeSkins(){skins.Save("CodexUsageMeter");ApplyLanguage();}
    void ApplySkinLayout(){if(rootLayout==null)return;SuspendLayout();rootLayout.Controls.Clear();bool compact=skins.Window=="C"||skins.Window=="D";leading.Visible=!compact;
      if(compact){var header=metricRow;header.Controls.Clear();header.Controls.Add(title);header.Controls.Add(percent);percent.Margin=new Padding(14,0,0,0);header.Controls.Add(leading);rootLayout.Controls.Add(header);rootLayout.Controls.Add(progress);var reset=rightMetric;reset.Controls.Clear();reset.FlowDirection=FlowDirection.LeftToRight;reset.Controls.Add(resetLabel);reset.Controls.Add(hours);rootLayout.Controls.Add(reset);remainingLabel.Visible=false;}
      else{metricRow.Controls.Clear();leftMetric.Controls.Clear();rightMetric.Controls.Clear();leftMetric.FlowDirection=rightMetric.FlowDirection=FlowDirection.TopDown;leftMetric.Controls.Add(remainingLabel);leftMetric.Controls.Add(percent);rightMetric.Controls.Add(resetLabel);rightMetric.Controls.Add(hours);metricRow.Controls.Add(leading);metricRow.Controls.Add(leftMetric);metricRow.Controls.Add(divider);metricRow.Controls.Add(rightMetric);percent.Margin=new Padding(0);remainingLabel.Visible=true;rootLayout.Controls.Add(title);rootLayout.Controls.Add(metricRow);rootLayout.Controls.Add(progress);}
      rootLayout.Controls.Add(stamp);ApplySurface(skins.Window);progress.Skin=skins.Window;BackColor=skins.Window=="D"?Color.FromArgb(8,12,24):skins.Window=="C"?Color.FromArgb(16,25,36):MeterPalette.Get(background);progress.Width=compact?Math.Max(230,title.PreferredWidth+percent.PreferredWidth+20):Math.Max(280,metricRow.PreferredSize.Width);progress.Height=compact?7:8;
      fontCache.Clear();progress.Width=1;ScaleFonts(fontFactor);progress.Width=(compact?Math.Max((int)(230*fontFactor),metricRow.PreferredSize.Width):Math.Max((int)(280*fontFactor),metricRow.PreferredSize.Width));SkinTransparent(rootLayout);ResumeLayout(true);LayoutTree(this);}
    static void SkinTransparent(Control c){c.BackColor=Color.Transparent;foreach(Control child in c.Controls)SkinTransparent(child);}
    protected override void OnPaintBackground(PaintEventArgs e){base.OnPaintBackground(e);}

    protected override bool ShowWithoutActivation { get { return true; } }
    readonly Label percent,hours,stamp,title;
    readonly Label remainingLabel,resetLabel;
    readonly ToolStripMenuItem languageMenu,backgroundMenu;
    readonly ToolStripMenuItem[] languageItems=new ToolStripMenuItem[2],backgroundItems=new ToolStripMenuItem[4];
    readonly List<Font> scaledFonts=new List<Font>();
    readonly Dictionary<float,Dictionary<Control,Font>> fontCache=new Dictionary<float,Dictionary<Control,Font>>();
    string language=MeterPreferences.Load(),background=MeterPreferences.LoadBackground();bool preferenceSaved=true;
    bool English { get { return language=="en"; } }
    string T(string zh,string en) { return English?en:zh; }
    void ApplyLanguage() {
        title.Text=(skins.Window=="C"||skins.Window=="D")?"ChatGPT":T("ChatGPT · 剩余额度","ChatGPT · Remaining Quota");remainingLabel.Text=T("用量剩余","Remaining quota");resetLabel.Text=T("距离重置（小时）","Hours until reset");
        string[] items=English?new[]{"Show floating meter","Hide floating meter","Refresh now","Reposition floating meter","Exit meter"}:new[]{"显示浮条","隐藏浮条","立即刷新","重新定位浮条","退出浮条"};
        for(int i=0;i<items.Length;i++)ContextMenuStrip.Items[i].Text=items[i];
        languageMenu.Text="语言 / Language";languageItems[0].Checked=!English;languageItems[1].Checked=English;
        backgroundMenu.Text=T("背景颜色","Background color");
        string[] names=English?new[]{"Dark navy","Charcoal","Forest green","Deep plum"}:new[]{"深海蓝","炭灰","森林绿","暗紫"};
        for(int i=0;i<names.Length;i++) {backgroundItems[i].Text=names[i];backgroundItems[i].Checked=background==MeterPalette.Keys[i];}
        BackColor=skins.Window=="D"?Color.FromArgb(8,12,24):skins.Window=="C"?Color.FromArgb(16,25,36):MeterPalette.Get(background);if(skinMenus!=null)skinMenus.Translate(English);UpdateValues();ApplySkinLayout();LayoutTree(this);
    }
    void SelectAppearance(string newLanguage,string newBackground) {language=newLanguage;background=newBackground;preferenceSaved=MeterPreferences.Save(language,background);skins.English=English;skins.Save("CodexUsageMeter");ApplyLanguage();ApplySkinLayout();Clamp();}
    internal void TestLanguage(string code) {languageItems[code=="en"?1:0].PerformClick();}
    internal void TestBackground(string code) {int i=Array.IndexOf(MeterPalette.Keys,code);if(i>=0)backgroundItems[i].PerformClick();}
    internal void TestFailure(string code){usage.Fail(usage.Current,code);UpdateValues();}
    internal string TestCurrentBackground {get {return background;}}
    internal bool TestSharesTrayMenu {get {return object.ReferenceEquals(tray.ContextMenuStrip,ContextMenuStrip);}}
    internal string TestCurrentLanguage { get { return language; } }
    internal bool TestOfficialConnected { get { return usage.Current!=null&&usage.Current.Official&&!usage.Cached; } }
    internal void TestState(Snapshot snapshot,bool failed,float scale) { usage.Current=snapshot;usage.QueryFailed=failed;ScaleFonts(scale);UpdateValues();if(snapshot!=null)stamp.Text=T("测试数据 · 非实际取数","TEST DATA · Not live quota");PerformLayout(); }

    readonly CodexCleanTray tray=new CodexCleanTray();readonly LiveUsage client=new LiveUsage();readonly UsageState usage=new UsageState();
    readonly System.Windows.Forms.Timer poll=new System.Windows.Forms.Timer(),tick=new System.Windows.Forms.Timer();
    readonly Dictionary<Control,Font> fonts=new Dictionary<Control,Font>();
    readonly Dictionary<Control,Rectangle> boxes=new Dictionary<Control,Rectangle>();
    readonly EventWaitHandle reveal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\CodexFloating_Show"),exit=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\CodexFloating_Exit");
    RegisteredWaitHandle revealWait,exitWait;Icon icon;string iconKey;bool busy,dragging,quitting;Point drag;
    DateTimeOffset attempted=DateTimeOffset.MinValue;readonly IntPtr anchor;
    [StructLayout(LayoutKind.Sequential)] struct Rect { public int Left,Top,Right,Bottom; }
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint id);
    public FloatingUsage(IntPtr codexWindow,bool renderOnly=false) {
        anchor=codexWindow;Text="CHATGPT USAGE METER · Floating v0.4.2 Skins";AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;Padding=new Padding(8);
        BackColor=Color.FromArgb(18,32,48);ForeColor=Color.White;TopMost=true;ShowInTaskbar=true;
        Add("⠿",4,22,18,34,18,Color.SlateGray);Add("用量剩余",26,4,124,20,11,Color.FromArgb(163,180,201));percent=Add("—",22,19,128,43,30,Color.Turquoise);
        Add("距离重置",169,4,143,20,11,Color.FromArgb(163,180,201));hours=Add("未知",165,19,149,43,30,Color.White);
        stamp=Add("等待官方查询…",24,62,290,20,10,Color.FromArgb(163,180,201));
        var separator=new Panel { Left=155,Top=8,Width=1,Height=48,BackColor=Color.FromArgb(56,75,95) };Controls.Add(separator);
        var labels=new System.Collections.Generic.List<Control>();foreach(Control c in Controls)labels.Add(c);Controls.Clear();
        foreach(Control c in labels) { var l=c as Label;if(l!=null) { l.AutoSize=true;l.AutoEllipsis=false; }c.Margin=new Padding(0); }
        var left=new FlowLayoutPanel { AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(8,0,10,0) };
        remainingLabel=(Label)labels[1];left.Controls.Add(remainingLabel);left.Controls.Add(percent);
        var right=new FlowLayoutPanel { AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(10,0,0,0) };
        resetLabel=(Label)labels[3];right.Controls.Add(resetLabel);right.Controls.Add(hours);
        var row=new FlowLayoutPanel { AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=false,Margin=new Padding(0) };
        row.Controls.Add(labels[0]);row.Controls.Add(left);row.Controls.Add(separator);row.Controls.Add(right);
        var root=new FlowLayoutPanel { AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Location=new Point(8,8),Margin=new Padding(0) };
        title=new Label {Name="meterTitle",AutoSize=true,Margin=new Padding(0,0,0,4),Font=new Font("Microsoft YaHei UI",12,FontStyle.Regular,GraphicsUnit.Pixel),UseCompatibleTextRendering=true,ForeColor=Color.LightSteelBlue};root.Controls.Add(title);
        stamp.Margin=new Padding(20,0,0,0);root.Controls.Add(row);root.Controls.Add(stamp);Controls.Add(root);
        var menu=new ContextMenuStrip();menu.Items.Add("显示浮条",null,delegate { Show();Clamp(); });menu.Items.Add("隐藏浮条",null,delegate { Hide(); });menu.Items.Add("立即刷新",null,delegate { RefreshUsage(); });
        menu.Items.Add("放到指定 Codex 窗口顶部",null,delegate { Place(); });menu.Items.Add("退出浮条",null,delegate { Quit(); });
        ContextMenuStrip=menu;rootLayout=root;metricRow=row;leftMetric=left;rightMetric=right;leading=labels[0];divider=separator;progress.ContextMenuStrip=menu;Wire(progress);CaptureControls(root,menu);Wire(this);
        menu.Items.Add(new ToolStripSeparator());
        languageMenu=new ToolStripMenuItem("语言 / Language");
        languageItems[0]=new ToolStripMenuItem("简体中文",null,delegate {SelectAppearance("zh-CN",background);});
        languageItems[1]=new ToolStripMenuItem("English",null,delegate {SelectAppearance("en",background);});
        languageMenu.DropDownItems.AddRange(languageItems);menu.Items.Add(languageMenu);
        backgroundMenu=new ToolStripMenuItem();
        for(int i=0;i<MeterPalette.Keys.Length;i++) {string key=MeterPalette.Keys[i];backgroundItems[i]=new ToolStripMenuItem("",null,delegate {SelectAppearance(language,key);});}
        backgroundMenu.DropDownItems.AddRange(backgroundItems);menu.Items.Add(backgroundMenu);
        skinMenus=new SkinMenus(menu,skins,ChangeSkins);ApplySkinLayout();ApplyLanguage();
        tray.ContextMenuStrip=menu;tray.MouseClick+=delegate(object sender,MouseEventArgs e) { if(e.Button==MouseButtons.Left) { Show();Clamp(); } };
        poll.Interval=60000;poll.Tick+=delegate { RefreshUsage(); };tick.Interval=1000;tick.Tick+=delegate { UpdateValues(); };
        Shown+=delegate { ScaleFonts(DeviceDpi/96f);if(renderOnly)return;Place();UpdateValues();tray.Visible=true;poll.Start();tick.Start();RefreshUsage(); };
        DpiChanged+=delegate(object sender,DpiChangedEventArgs e) { ScaleFonts(e.DeviceDpiNew/96f);UpdateValues();Clamp(); };
        FormClosing+=delegate(object sender,FormClosingEventArgs e) { if(!quitting && e.CloseReason==CloseReason.UserClosing) { e.Cancel=true;Hide(); } };
        if(!renderOnly) { revealWait=ThreadPool.RegisterWaitForSingleObject(reveal,delegate { Post(delegate { Show();Clamp(); }); },null,-1,false);exitWait=ThreadPool.RegisterWaitForSingleObject(exit,delegate { Post(Quit); },null,-1,false); }
    }
    void CaptureControls(Control c,ContextMenuStrip menu) { c.ContextMenuStrip=menu;Wire(c);fonts[c]=c.Font;foreach(Control child in c.Controls)CaptureControls(child,menu); }
    public void RenderTest(float factor) { usage.Accept(new Snapshot { Official=true,At=DateTimeOffset.UtcNow,Primary=new QuotaWindow { Used=31,Reset=DateTimeOffset.UtcNow.AddHours(117.7).ToUnixTimeSeconds() } });ScaleFonts(factor);UpdateValues();stamp.Text="DPI验证 · 示意数据 · 非实际取数"; }
    Label Add(string text,int x,int y,int w,int h,int px,Color color) { var l=new Label { Text=text,AccessibleName=text,Left=x,Top=y,Width=w,Height=h,Font=new Font("Microsoft YaHei UI",px,FontStyle.Bold,GraphicsUnit.Pixel),ForeColor=color,UseCompatibleTextRendering=true,AutoEllipsis=true };Controls.Add(l);return l; }
    void ScaleFonts(float factor) {
        fontFactor=factor;SuspendLayout();Dictionary<Control,Font> cached;if(!fontCache.TryGetValue(factor,out cached)) {cached=new Dictionary<Control,Font>();foreach(var p in fonts) {var f=p.Value;float size=f.Size;if(skins.Window=="C"||skins.Window=="D"){if(p.Key==percent)size=19;if(p.Key==hours)size=13;}var scaled=new Font(f.FontFamily,size*factor,f.Style,f.Unit);cached[p.Key]=scaled;scaledFonts.Add(scaled);}fontCache[factor]=cached;}foreach(var p in cached)p.Key.Font=p.Value;ResumeLayout(true);LayoutTree(this);if(rootLayout!=null){if(skins.Window=="C"||skins.Window=="D")percent.Margin=new Padding(Math.Max(14,(int)(230*factor)-title.PreferredWidth-percent.PreferredWidth),0,0,0);progress.Width=1;LayoutTree(metricRow);progress.Width=Math.Max((int)((skins.Window=="C"||skins.Window=="D"?230:280)*factor),metricRow.PreferredSize.Width);LayoutTree(this);}
    }
    static void LayoutTree(Control c) { foreach(Control child in c.Controls)LayoutTree(child);c.PerformLayout(); }
    void Post(Action action) { if(!IsDisposed&&IsHandleCreated)try { BeginInvoke(action); }catch(InvalidOperationException) { } }
    void Wire(Control c) {
        if(c is ComboBox)return;
        c.MouseDown+=delegate(object sender,MouseEventArgs e) { if(e.Button==MouseButtons.Left) { dragging=true;drag=Cursor.Position;c.Capture=true; } };
        c.MouseMove+=delegate { if(dragging) { var p=Cursor.Position;Location=new Point(Left+p.X-drag.X,Top+p.Y-drag.Y);drag=p; } };
        c.MouseUp+=delegate { dragging=false;c.Capture=false;Clamp(); };
    }
    void Clamp() { var a=Screen.FromRectangle(Bounds).WorkingArea;Location=new Point(Math.Max(a.Left,Math.Min(Left,a.Right-Width)),Math.Max(a.Top,Math.Min(Top,a.Bottom-Height))); }
    void Place() {
        Rect r;uint pid;bool placed=false;
        if(anchor!=IntPtr.Zero && IsWindowVisible(anchor) && GetWindowRect(anchor,out r)) {
            GetWindowThreadProcessId(anchor,out pid);
            try { string name=Process.GetProcessById((int)pid).ProcessName;if(name.IndexOf("Codex",StringComparison.OrdinalIgnoreCase)>=0||name.IndexOf("ChatGPT",StringComparison.OrdinalIgnoreCase)>=0) { Location=new Point(r.Left+(r.Right-r.Left-Width)/2,r.Top+50);placed=true; } }catch { }
        }
        if(!placed) { var a=Screen.PrimaryScreen.WorkingArea;Location=new Point(a.Right-Width-16,a.Top+50); }Clamp();
    }
    void RefreshUsage() {
        if(busy||DateTimeOffset.UtcNow-attempted<TimeSpan.FromSeconds(10))return;busy=true;attempted=DateTimeOffset.UtcNow;
        Post(delegate { stamp.Text=T("正在查询官方额度…","Querying official quota…"); });
        ThreadPool.QueueUserWorkItem(delegate {
            Snapshot fresh=null,fallback=null;string failure="read-failed";try { fresh=client.Read(); }catch(Exception error) { failure=LiveUsage.ErrorCode(error);string ignored;try { if(usage.Current==null||!usage.Current.Official)fallback=Reader.Read(Program.Root,out ignored); }catch { } }
            Post(delegate { if(fresh!=null)usage.Accept(fresh);else usage.Fail(fallback,failure);busy=false;UpdateValues(); });
        });
    }
    void UpdateValues() {
        var now=DateTimeOffset.UtcNow;var s=usage.Current;var w=s==null?null:s.Primary??s.Secondary;bool cached=usage.Cached;
        percent.Text=QuotaDisplay.Percent(w,now);hours.Text=!QuotaDisplay.Hours(w,now).HasValue?T("未知","Unknown"):QuotaDisplay.Expired(w,now)?T("待刷新","Refresh due"):QuotaDisplay.HoursText(w,now)+"h";
        percent.AccessibleName=T("用量剩余 ","Remaining quota ")+percent.Text;hours.AccessibleName=T("距离重置 ","Hours until reset ")+hours.Text;
        percent.ForeColor=Color.White;progress.Accent=cached?Color.Goldenrod:Color.FromArgb(70,225,158);progress.Remaining=QuotaDisplay.Expired(w,now)||w==null?null:(double?)w.Remaining;progress.AccessibleName=T("剩余额度进度 ","Remaining quota progress ")+percent.Text;progress.Invalidate();
        string source=s==null?T("暂无数据","No data"):cached?T("缓存","Cached"):s.Official?T("官方","Official"):T("本地日志","Local log");
        stamp.Text=source+(s==null?"":" "+s.At.ToLocalTime().ToString("HH:mm:ss 'UTC'zzz"))+T(" · 60秒查询"," · 60s polling")+(QuotaDisplay.Expired(w,now)?T(" · 待刷新"," · Refresh due"):"")+(!preferenceSaved||!skins.Saved?T(" · 偏好未保存"," · Preferences not saved"):"");if(usage.QueryFailed)stamp.Text+=" · "+LiveUsage.ErrorText(usage.LastErrorCode,English);stamp.AccessibleName=stamp.Text;
        var trayPlan=CodexTrayDisplay.Evaluate(s,usage.QueryFailed,now,English);
        int traySize=Math.Max(16,SystemInformation.SmallIconSize.Width);
        string key=trayPlan.Number+"/"+trayPlan.Color.ToArgb()+"/"+trayPlan.Underline+"/"+traySize+"/"+skins.Tray;
        if(key!=iconKey) {var old=icon;icon=CodexTrayDisplay.IconFor(trayPlan,traySize,skins.Tray);tray.Icon=icon;Icon=icon;iconKey=key;if(old!=null)old.Dispose();}
        tray.Text=trayPlan.Tooltip;
    }
    void Quit() { quitting=true;poll.Stop();tick.Stop();tray.Visible=false;Close(); }
    protected override void Dispose(bool disposing) { if(disposing) { poll.Dispose();tick.Dispose();client.Dispose();tray.Dispose();if(icon!=null)icon.Dispose();if(revealWait!=null)revealWait.Unregister(null);if(exitWait!=null)exitWait.Unregister(null);reveal.Dispose();exit.Dispose(); }base.Dispose(disposing);if(disposing)foreach(var f in scaledFonts)f.Dispose(); }
}
public static class FloatingProgram {
    [STAThread] public static int Main(string[] args) {
        if(args.Length==1 && args[0]=="--exit") { EventWaitHandle signal;if(EventWaitHandle.TryOpenExisting("Local\\CodexFloating_Exit",out signal))using(signal)signal.Set();return 0; }
        bool created;
        using(var mutex=new Mutex(true,"Local\\CodexUsageTray_20261030",out created)) {
            if(!created) { EventWaitHandle show;if(EventWaitHandle.TryOpenExisting("Local\\CodexFloating_Show",out show))using(show)show.Set();else MessageBox.Show("请先退出旧仪表再启动浮条。\nExit the previous meter before opening this floating version.","CHATGPT USAGE METER v0.4");return 0; }
            long handle=0;if(args.Length==2&&args[0]=="--anchor-window")long.TryParse(args[1],out handle);
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);using(var f=new FloatingUsage(new IntPtr(handle)))Application.Run(f);mutex.ReleaseMutex();return 0;
        }
    }
}
