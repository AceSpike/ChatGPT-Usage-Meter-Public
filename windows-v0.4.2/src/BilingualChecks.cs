using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Windows.Forms;

public static class BilingualChecks {
    static void Check(bool value,string message) { if(!value)throw new Exception(message); }
    static IEnumerable<Control> All(Control parent) { foreach(Control c in parent.Controls) { yield return c;foreach(var child in All(c))yield return child; } }
    static bool Contains(Form f,string text) { foreach(var c in All(f))if(c.Text==text)return true;return false; }
    static void Bounds(Control parent) { foreach(Control c in parent.Controls) { Check(c.Right<=parent.ClientSize.Width&&c.Bottom<=parent.ClientSize.Height,"bounds "+c.GetType()+" "+c.Text+" child="+c.Bounds+" parent="+parent.ClientSize);if(c is Label)Check(c.Width>=c.PreferredSize.Width&&c.Height>=c.PreferredSize.Height,"label clipping "+c.Text);Bounds(c); } }
    static Color EffectiveBackground(Control c){while(c.BackColor.A==0&&c.Parent!=null)c=c.Parent;return c.BackColor;}
    static double Luminance(Color c) {Func<byte,double> channel=x=>{double n=x/255.0;return n<=0.04045?n/12.92:Math.Pow((n+0.055)/1.055,2.4);};return 0.2126*channel(c.R)+0.7152*channel(c.G)+0.0722*channel(c.B);}
    static double Contrast(Color a,Color b) {double x=Luminance(a),y=Luminance(b);return (Math.Max(x,y)+0.05)/(Math.Min(x,y)+0.05);}
    static void MenuImage(ToolStripDropDown menu,string name) {Pump();using(var b=new Bitmap(menu.Width,menu.Height)) {menu.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height));b.Save(Path.Combine("validation",name+".png"),ImageFormat.Png);}}
    static void Pump() { Application.DoEvents(); }
    static void Render(FloatingUsage f,string name) { Pump();Bounds(f);using(var b=new Bitmap(f.Width,f.Height)) { f.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height));b.Save(Path.Combine("validation",name+".png"),ImageFormat.Png); }Console.WriteLine(name+": "+f.Width+"x"+f.Height); }
    [STAThread] public static int Main(string[] args) {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        SkinSettings.DirectoryOverride=Path.Combine(Environment.CurrentDirectory,"skin-test-"+Guid.NewGuid().ToString("N"));
        MeterPreferences.DirectoryOverride=Path.Combine(Environment.CurrentDirectory,"test-preferences-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory("validation");
        if(args.Length==1&&args[0]=="--live") {
            using(var live=new FloatingUsage(IntPtr.Zero)) {
                live.TopMost=false;live.Opacity=0;live.ShowInTaskbar=false;live.Show();
                var timer=System.Diagnostics.Stopwatch.StartNew();
                while(!live.TestOfficialConnected&&timer.ElapsedMilliseconds<35000) {Pump();System.Threading.Thread.Sleep(100);}
                bool connected=live.TestOfficialConnected;
                live.ContextMenuStrip.Items[4].PerformClick();Pump();
                Check(connected,"live form official quota connection");
                Console.WriteLine("PASS: actual floating form reached fresh official account/rateLimits/read state and exited cleanly; no raw account data logged.");
            }
            return 0;
        }
        var now=DateTimeOffset.UtcNow;var sample=new Snapshot { Official=true,At=now,Primary=new QuotaWindow { Used=31,Reset=now.AddHours(117.7).ToUnixTimeSeconds() } };
        using(var f=new FloatingUsage(IntPtr.Zero,true)) {
            f.TopMost=false;f.Opacity=0;f.ShowInTaskbar=false;f.Show();Pump();
            Check(f.TestCurrentLanguage=="zh-CN"&&f.TestCurrentBackground=="navy","defaults");
            Check(f.TestSharesTrayMenu,"floating and tray must share all menu choices");
            foreach(var c in All(f))Check(!(c is ComboBox)&&c.Text!="语言 / Language","no dashboard settings controls");
            foreach(var language in new[]{"zh-CN","en"})foreach(var factor in new[]{1f,1.5f,2f}) {
                f.TestLanguage(language);f.TestState(sample,false,factor);Render(f,language+"-"+(int)(factor*100));
                Check(Contains(f,"69%")&&Contains(f,"117.7h"),"language must preserve quota");
                Check(Contains(f,language=="en"?"Remaining quota":"用量剩余"),"remaining label");
                Check(Contains(f,language=="en"?"ChatGPT · Remaining Quota":"ChatGPT · 剩余额度"),"visible localized top title");
                Check(f.ContextMenuStrip.Items[4].Text==(language=="en"?"Exit meter":"退出浮条"),"menu language");
            }
            f.TestLanguage("en");f.TestState(null,false,1);Check(Contains(f,"Unknown"),"unknown English");Render(f,"en-unavailable");
            f.TestState(new Snapshot {At=now,Official=true,Primary=new QuotaWindow {Used=5,Reset=now.AddSeconds(-1).ToUnixTimeSeconds()}},false,1);Check(Contains(f,"Refresh due")&&Contains(f,"—"),"expired cannot infer 100%");Render(f,"en-expired");
            f.TestState(sample,true,1);Render(f,"en-cached");
            foreach(var theme in MeterPalette.Keys)foreach(var lang in new[]{"zh-CN","en"}) {
                f.TestLanguage(lang);f.TestBackground(theme);f.TestState(sample,false,1.5f);Render(f,lang+"-"+theme);
                Check(f.BackColor==MeterPalette.Get(theme),"background applied");
                foreach(var c in All(f))if(c is Label) {bool text=false;foreach(char ch in c.Text)if(char.IsLetterOrDigit(ch))text=true;if(text)Check(Contrast(c.ForeColor,EffectiveBackground(c))>=4.5,"contrast "+theme+" "+c.Text);}
                var languageMenu=(ToolStripMenuItem)f.ContextMenuStrip.Items[6];var backgroundMenu=(ToolStripMenuItem)f.ContextMenuStrip.Items[7];
                Check(((ToolStripMenuItem)languageMenu.DropDownItems[lang=="en"?1:0]).Checked,"language checkmark");
                for(int i=0;i<MeterPalette.Keys.Length;i++)Check(((ToolStripMenuItem)backgroundMenu.DropDownItems[i]).Checked==(MeterPalette.Keys[i]==theme),"background radio checkmarks");
            }
            f.TestLanguage("en");f.TestBackground("forest");
            var prefs=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(MeterPreferences.PathName));
            Check(prefs.Count==2&&(string)prefs["language"]=="en"&&(string)prefs["background"]=="forest","only appearance codes persisted");
            f.ContextMenuStrip.Opacity=0;f.ContextMenuStrip.Show(f,new Point(0,0));MenuImage(f.ContextMenuStrip,"menu-en");
            var lm=(ToolStripMenuItem)f.ContextMenuStrip.Items[6];lm.DropDown.Opacity=0;lm.ShowDropDown();MenuImage(lm.DropDown,"menu-language");lm.HideDropDown();
            var bm=(ToolStripMenuItem)f.ContextMenuStrip.Items[7];bm.DropDown.Opacity=0;bm.ShowDropDown();MenuImage(bm.DropDown,"menu-background-en");bm.HideDropDown();f.ContextMenuStrip.Close();
            f.ContextMenuStrip.Items[1].PerformClick();Pump();Check(!f.Visible,"hide");
            f.ContextMenuStrip.Items[0].PerformClick();Pump();Check(f.Visible,"restore");
            f.Location=new Point(-32000,-32000);f.ContextMenuStrip.Items[3].PerformClick();Pump();Check(Screen.FromControl(f).WorkingArea.Contains(f.Bounds),"reposition clamp");
            f.Close();Pump();Check(!f.IsDisposed&&!f.Visible,"close-to-hide");
            f.ContextMenuStrip.Items[0].PerformClick();Pump();f.ContextMenuStrip.Items[4].PerformClick();Pump();Check(f.IsDisposed,"exit");
        }
        using(var reopened=new FloatingUsage(IntPtr.Zero,true)) {Check(reopened.TestCurrentLanguage=="en"&&reopened.TestCurrentBackground=="forest","both preferences survive reopen");Check(Contains(reopened,"Remaining quota"),"reopened English labels");}
        File.WriteAllText(MeterPreferences.PathName,"{\"language\":\"unsupported\",\"background\":\"unknown\"}");Check(MeterPreferences.Load()=="zh-CN"&&MeterPreferences.LoadBackground()=="navy","invalid preferences fallback");
        SkinSettings.DirectoryOverride=Path.Combine(Environment.CurrentDirectory,"skin-test-"+Guid.NewGuid().ToString("N"));
        MeterPreferences.DirectoryOverride=Path.Combine(Environment.CurrentDirectory,"test-migration-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(MeterPreferences.DirectoryOverride);File.WriteAllText(Path.Combine(MeterPreferences.DirectoryOverride,"language.txt"),"en");Check(MeterPreferences.Load()=="en"&&MeterPreferences.LoadBackground()=="navy","v0.2 preference migration");
        string blocked=Path.Combine(Environment.CurrentDirectory,"test-preferences-blocked-"+Guid.NewGuid().ToString("N"));File.WriteAllText(blocked,"test");MeterPreferences.DirectoryOverride=blocked;
        using(var f=new FloatingUsage(IntPtr.Zero,true)) {f.TestLanguage("en");Check(f.TestCurrentLanguage=="en"&&Contains(f,"Remaining quota"),"blocked storage still switches");bool notice=false;foreach(var c in All(f))if(c.Text.Contains("Preferences not saved"))notice=true;Check(notice,"blocked storage notice");}
        Console.WriteLine("PASS: bilingual native labels/menu/unknown/expired/cache; language/background persistence and reload, legacy migration, shared right-click menus/checkmarks, no dashboard settings controls, four palettes with >=4.5 text contrast; invalid/blocked storage; unchanged quota on switch; recursive layout at 100/150/200%; component hide/restore/reposition/close-to-hide/exit.");
        return 0;
    }
}
