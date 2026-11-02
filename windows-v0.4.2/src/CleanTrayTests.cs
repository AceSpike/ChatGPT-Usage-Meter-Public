using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;using System.Globalization;using System.Windows.Forms;
public static class CleanTrayTests {
 static int count;static void Check(bool yes,string message){count++;if(!yes)throw new Exception(message);}
 static Snapshot Fixture(double used,DateTimeOffset at,DateTimeOffset reset,bool official=true){return new Snapshot {At=at,Official=official,Primary=new QuotaWindow {Used=used,Reset=reset.ToUnixTimeSeconds()}};}
 [STAThread]public static int Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Directory.CreateDirectory("tray-evidence");var now=new DateTimeOffset(2030,1,1,12,34,56,TimeSpan.Zero);var normal=Fixture(28.8,now.AddSeconds(-12),now.AddDays(3));
  Check(CodexTrayDisplay.Evaluate(normal,false,now,false).Number=="71","floor rather than round percentage");
  Check(CodexTrayDisplay.Evaluate(Fixture(0,now,now.AddDays(1)),false,now,false).Number=="100","exact full quota");Check(CodexTrayDisplay.Evaluate(Fixture(0.01,now,now.AddDays(1)),false,now,false).Number=="99","do not display premature 100");Check(CodexTrayDisplay.Evaluate(Fixture(100,now,now.AddDays(1)),false,now,false).Number=="0","empty quota");
  Check(CodexTrayDisplay.Evaluate(null,false,now,false).Number=="?","unknown quota");Check(CodexTrayDisplay.Evaluate(null,true,now,false).Number=="!","failed with no cached data");Check(CodexTrayDisplay.Evaluate(normal,true,now,false).Number=="!","failed never looks fresh");
  Check(CodexTrayDisplay.Evaluate(Fixture(1,now,now),false,now,false).Phase==CodexTrayPhase.Expired,"exact reset needs new official data");Check(CodexTrayDisplay.Evaluate(Fixture(1,now,now.AddSeconds(-1)),false,now,false).Number=="!","expired no percentage promise");
  var cached=Fixture(20,now,now.AddDays(1),false);var stale=Fixture(20,now.AddMinutes(-6),now.AddDays(1));Check(CodexTrayDisplay.Evaluate(cached,false,now,false).Underline,"unverified log cache marker");Check(CodexTrayDisplay.Evaluate(stale,false,now,false).Phase==CodexTrayPhase.Stale,"old snapshot distinct from fresh");
  var future=Fixture(20,now.AddMinutes(2),now.AddDays(1));Check(CodexTrayDisplay.Evaluate(future,false,now,false).Phase==CodexTrayPhase.Stale,"future timestamp not fresh");Check(CodexTrayDisplay.Evaluate(Fixture(double.NaN,now,now.AddDays(1)),false,now,false).Number=="?","invalid quota");
  foreach(bool english in new[]{false,true})foreach(var fixture in new[]{normal,cached,stale,future,Fixture(0,now,now),Fixture(100,now,now.AddDays(1)),Fixture(1.0/3,DateTimeOffset.MinValue,DateTimeOffset.MaxValue)})foreach(bool failed in new[]{false,true}){
   var plan=CodexTrayDisplay.Evaluate(fixture,failed,now,english);string tip=plan.Tooltip;Check(tip.Length<=127,"native tooltip capacity");Check(tip.Contains(fixture.Primary.Remaining.ToString("G15",CultureInfo.InvariantCulture)+"%"),"tooltip preserves non-integer percentage");Check(tip.Contains(QuotaDisplay.ResetAt(fixture.Primary).Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss'Z'",CultureInfo.InvariantCulture)),"full reset timestamp preserved");Check(tip.Contains(fixture.At.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss'Z'",CultureInfo.InvariantCulture)),"original observation timestamp preserved");Check(tip.Split('\n').Length==3,"three complete tooltip lines");
   if(!fixture.Official)Check(tip.Contains(english?"unverified":"未核验"),"fallback account provenance");
  }
  Check(CodexCleanTray.NativeDataSize==(IntPtr.Size==8?976:956),"NOTIFYICONDATAW layout matches Windows ABI");
  using(var sheet=new Bitmap(700,320))using(var g=Graphics.FromImage(sheet)){g.Clear(Color.FromArgb(225,230,235));int x=10;foreach(int size in new[]{16,20,24,32}){
   g.DrawString(size+"px actual pixels",SystemFonts.DefaultFont,Brushes.Black,x,5);int y=32;
   foreach(var s in new[]{Fixture(0,now,now.AddDays(1)),normal,Fixture(100,now,now.AddDays(1)),(Snapshot)null,stale}){
    var plan=CodexTrayDisplay.Evaluate(s,false,now,false);using(var bmp=CodexTrayDisplay.Draw(plan,size)){
     g.DrawImageUnscaled(bmp,x,y);bmp.Save("tray-evidence/"+size+"-"+plan.Number.Replace("?","unknown")+"-"+plan.Phase+".png",ImageFormat.Png);
     if(plan.Number=="100"){int scale=Math.Max(1,Math.Min((size-2)/11,(size-4)/5)),left=(size-11*scale)/2,top=(size-5*scale)/2-1;for(int digit=0;digit<3;digit++){int lit=0;for(int py=top;py<top+5*scale;py++)for(int px=left+digit*4*scale;px<left+(digit*4+3)*scale;px++)if(bmp.GetPixel(px,py).ToArgb()==Color.White.ToArgb())lit++;Check(lit>0,"all three 100 digits visible");}}
    }using(var icon=CodexTrayDisplay.IconFor(plan,size))Check(icon.Width==size&&icon.Height==size,"native HICON dimension");y+=45;
   }using(var bmp=CodexTrayDisplay.Draw(CodexTrayDisplay.Evaluate(normal,true,now,false),size))g.DrawImageUnscaled(bmp,x,272);x+=175;
  }sheet.Save("tray-evidence/native-pixel-sheet.png",ImageFormat.Png);}
  foreach(bool english in new[]{false,true})File.WriteAllText("tray-evidence/tooltip-"+(english?"en":"zh")+".txt",CodexTrayDisplay.Evaluate(normal,false,now,english).Tooltip);
  using(var tray=new CodexCleanTray()){tray.Text=new string('x',127);Check(tray.Text.Length==127,"127-char native tooltip supported");bool rejected=false;try{tray.Text=new string('x',128);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"overflow never silently truncates");}
  if(args.Length==1&&args[0]=="--shell-smoke"){
   var plan=CodexTrayDisplay.Evaluate(normal,false,now,true);using(var icon=CodexTrayDisplay.IconFor(plan,Math.Max(16,SystemInformation.SmallIconSize.Width)))using(var tray=new CodexCleanTray()){
    tray.Icon=icon;tray.Text="TEST ICON · simulated Codex quota, not real data\n"+new string('x',70);tray.Visible=true;Check(tray.Registered,"real Shell NIM_ADD");Check(tray.Version4,"real Shell NIM_SETVERSION 4");Check(tray.LastUpdateSucceeded,"real Shell tooltip update");Rectangle rect=Rectangle.Empty;var watch=System.Diagnostics.Stopwatch.StartNew();bool found=false;while(watch.ElapsedMilliseconds<4000){Application.DoEvents();if(tray.TryGetBounds(out rect)){found=true;break;}System.Threading.Thread.Sleep(50);}Check(found,"real Shell icon rectangle");tray.Text=new string('x',127);Check(tray.LastUpdateSucceeded,"real Shell accepted full 127 characters");Console.WriteLine("SHELL PASS: actual notification icon registered; v4 callback mode; 127-character tooltip accepted; icon bounds {0}; system small icon {1}.",rect,SystemInformation.SmallIconSize);tray.Visible=false;Check(!tray.Registered,"real Shell removal");
   }
  }
  Console.WriteLine("PASS: {0} clean tray assertions; pixel-number 16/20/24/32, 100/0/?/!, failure/expiry/cache freshness, bilingual complete tooltips and owned HICON cleanup.",count);return 0;
 }
}
