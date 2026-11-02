using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

public static class RuntimeChecks {
    public static int Run() {
        var now=new DateTimeOffset(2026,10,30,3,30,0,TimeSpan.Zero);
        var w=new QuotaWindow { Used=29,Reset=now.AddHours(96.5).ToUnixTimeSeconds(),Minutes=10080 };
        if(QuotaDisplay.Percent(w,now)!="71%" || QuotaDisplay.HoursText(w,now)!="96.5" || QuotaDisplay.HoursIcon(w,now)!="97")return 21;
        if(QuotaDisplay.HoursText(w,now.AddHours(96.5))!="待刷新" || QuotaDisplay.Percent(w,now.AddHours(96.5))!="—" || QuotaDisplay.HoursIcon(w,now.AddDays(5))!="!")return 22;
        if(QuotaDisplay.HoursText(new QuotaWindow { Used=0 },now)!="未知" || QuotaDisplay.HoursText(new QuotaWindow { Reset=long.MaxValue },now)!="未知")return 23;
        var shortWindow=new QuotaWindow { Reset=now.AddSeconds(1).ToUnixTimeSeconds() };
        if(QuotaDisplay.HoursText(shortWindow,now)!="<0.1" || QuotaDisplay.HoursIcon(shortWindow,now)!="<1")return 24;
        if(QuotaDisplay.Percent(new QuotaWindow { Used=100 },now)!="0%" || QuotaDisplay.Percent(new QuotaWindow { Used=0 },now)!="100%")return 25;
        var s=new Snapshot { Primary=w,At=now,Official=true };
        if(QuotaDisplay.Tooltip(s,false,now).Length>63 || !QuotaDisplay.Tooltip(s,false,now).Contains("96.5"))return 26;
        if(QuotaDisplay.Hours(w,now.AddHours(1))!=95.5)return 27;
        Console.WriteLine("PASS: real reset-hour arithmetic, rounding, <0.1h, exact reset, expired, unknown/invalid timestamp, percent bounds, tray values/tooltip, time advancement.");return 0;
    }
    static Snapshot Demo(string state) {
        var now=DateTimeOffset.UtcNow;
        if(state=="unknown")return null;
        return new Snapshot { At=state=="cached"?now.AddMinutes(-10):now,Official=true,
            Primary=new QuotaWindow { Used=state=="full"?0:29,Minutes=10080,Reset=state=="expired"?now.AddSeconds(-1).ToUnixTimeSeconds():now.AddHours(96.5).ToUnixTimeSeconds() },
            Secondary=new QuotaWindow { Used=80,Minutes=300,Reset=now.AddHours(4.1).ToUnixTimeSeconds() } };
    }
    public static int Render(Snapshot live,bool liveMode) {
        Directory.CreateDirectory("validation");
        foreach(var state in liveMode?new[]{"live"}:new[]{"normal","cached","expired","unknown","full"}) {
            foreach(float scale in liveMode?new[]{1f}:new[]{1f,1.5f,2f}) {
                using(var form=new HorizontalDashboard(Program.Root,true)) {
                    form.ShowInTaskbar=false;form.Opacity=0;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);form.Show();Application.DoEvents();
                    if(liveMode)form.Controls[3].Text="实际取数 · 离屏验证";
                    form.SetRenderState(liveMode?live:Demo(state),state=="cached",liveMode?"实际取数 · 离屏渲染": "测试数据 · 状态验证");
                    form.ScaleForValidation(scale);
                    using(var bmp=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(bmp,new Rectangle(0,0,bmp.Width,bmp.Height));bmp.Save(Path.Combine("validation","ui-"+state+"-"+(int)(scale*100)+".png"),ImageFormat.Png); }
                }
            }
        }
        using(var sheet=new Bitmap(520,360))using(var g=Graphics.FromImage(sheet)) {
            g.Clear(Color.FromArgb(16,23,34));
            int column=0;
            foreach(int size in new[]{16,20,24,32,48}) {
                int row=0;
                foreach(string val in new[]{"71%","97h","100%","168h","?h","!h"}) {
                    using(var b=TrayGraphic.Bitmap(val.Substring(0,val.Length-1),val.Substring(val.Length-1),val.EndsWith("%")?Color.FromArgb(23,105,93):Color.FromArgb(129,83,29),size))g.DrawImageUnscaled(b,12+column*100,12+row*56);
                    row++;
                }
                column++;
            }
            sheet.Save("validation/tray-actual-sizes.png",ImageFormat.Png);
        }
        Console.WriteLine("Rendered actual WinForms controls at 100/150/200% for normal/cache/expired/unknown/100% states, plus native-size tray graphics. All mock images are labeled test data.");return 0;
    }
}
