using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;

public static class QuotaDisplay {
    public static DateTimeOffset? ResetAt(QuotaWindow w) {
        if(w==null || !w.Reset.HasValue)return null;
        try { return DateTimeOffset.FromUnixTimeSeconds(w.Reset.Value); }catch { return null; }
    }
    public static bool Expired(QuotaWindow w,DateTimeOffset now) { var at=ResetAt(w);return at.HasValue && at.Value<=now; }
    public static double? Hours(QuotaWindow w,DateTimeOffset now) { var at=ResetAt(w);return at.HasValue?(double?)Math.Max(0,(at.Value-now).TotalHours):null; }
    public static string HoursText(QuotaWindow w,DateTimeOffset now) {
        var h=Hours(w,now);if(!h.HasValue)return "未知";if(h.Value<=0)return "待刷新";
        return h.Value<0.1?"<0.1":h.Value.ToString("0.0",CultureInfo.InvariantCulture);
    }
    public static string Percent(QuotaWindow w,DateTimeOffset now) { return w==null||Expired(w,now)?"—":w.Remaining.ToString("0.#",CultureInfo.InvariantCulture)+"%"; }
    public static string Duration(long? m) {
        if(!m.HasValue||m<=0)return "周期未提供";
        if(m%1440==0)return (m/1440)+" 天周期";
        if(m%60==0)return (m/60)+" 小时周期";
        return m+" 分钟周期";
    }
    public static string ResetText(QuotaWindow w) {
        var at=ResetAt(w);return at.HasValue?at.Value.ToLocalTime().ToString("MM-dd HH:mm 'UTC'zzz")+" 重置":"服务未提供有效重置时间";
    }
    public static string Summary(QuotaWindow w,DateTimeOffset now) {
        if(w==null)return "服务未提供";
        return Duration(w.Minutes)+" · 剩余 "+Percent(w,now)+" / 重置 "+HoursText(w,now)+(Expired(w,now)?"":" 小时");
    }
    public static string PercentIcon(QuotaWindow w,DateTimeOffset now) { return w==null||Expired(w,now)?"?":w.Remaining.ToString("0",CultureInfo.InvariantCulture); }
    public static string HoursIcon(QuotaWindow w,DateTimeOffset now) {
        var h=Hours(w,now);if(!h.HasValue)return "?";if(h.Value<=0)return "!";
        if(h.Value<1)return "<1";
        return h.Value>999?"999+":Math.Ceiling(h.Value).ToString("0",CultureInfo.InvariantCulture);
    }
    public static string Tooltip(Snapshot s,bool cached,DateTimeOffset now) {
        var w=s==null?null:s.Primary??s.Secondary;
        string text="Codex 剩余 "+Percent(w,now)+" / 重置 "+HoursText(w,now)+(Expired(w,now)?"":"h");
        if(s!=null)text+="\n"+(cached?"缓存 ":s.Official?"官方 ":"日志 ")+s.At.ToLocalTime().ToString("MM-dd HH:mm 'UTC'zzz");
        return text.Length<=63?text:text.Substring(0,63);
    }
}

public static class TrayGraphic {
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
    public static Bitmap Bitmap(string value,string unit,Color color,int size) {
        var b=new Bitmap(size,size);
        using(var g=Graphics.FromImage(b)) {
            g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using(var brush=new SolidBrush(color))g.FillRectangle(brush,0,0,size,size);
            string text=value+unit;float fontSize=size*.78f;
            using(var sf=new StringFormat(StringFormat.GenericTypographic) { Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap }) {
                Font f=null;
                try {
                    while(fontSize>=4) { if(f!=null)f.Dispose();f=new Font("Segoe UI",fontSize,FontStyle.Bold,GraphicsUnit.Pixel);var measure=g.MeasureString(text,f,int.MaxValue,sf);if(measure.Width<=size-1 && measure.Height<=size)break;fontSize-=.25f; }
                    g.DrawString(text,f,Brushes.White,new RectangleF(0,0,size,size),sf);
                }finally { if(f!=null)f.Dispose(); }
            }
        }
        return b;
    }
    public static Icon Icon(string value,string unit,Color color) {
        using(var b=Bitmap(value,unit,color,Math.Max(16,System.Windows.Forms.SystemInformation.SmallIconSize.Width))) {
            IntPtr h=b.GetHicon();
            try { using(var temp=System.Drawing.Icon.FromHandle(h))return (Icon)temp.Clone(); }finally { DestroyIcon(h); }
        }
    }
}
