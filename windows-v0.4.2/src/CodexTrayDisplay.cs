using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;

public enum CodexTrayPhase {Unknown,Fresh,Cached,Stale,Failed,Expired}
public sealed class CodexTrayPlan {
 public string Number,Percent,Tooltip;public Color Color;public bool Underline;public CodexTrayPhase Phase;
}
public static class CodexTrayDisplay {
 [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr handle);
 static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
 static string Age(DateTimeOffset at,DateTimeOffset now){double seconds=(now-at).TotalSeconds;if(seconds<0)return "clock?";if(seconds<60)return Math.Floor(seconds)+"s";if(seconds<3600)return Math.Floor(seconds/60)+"m";if(seconds<172800)return Math.Floor(seconds/3600)+"h";return Math.Floor(seconds/86400)+"d";}
 public static CodexTrayPlan Evaluate(Snapshot s,bool failed,DateTimeOffset now,bool english){
  var w=s==null?null:s.Primary??s.Secondary;
  bool valid=w!=null&&!double.IsNaN(w.Used)&&!double.IsInfinity(w.Used)&&w.Used>=0&&w.Used<=100;
  bool expired=valid&&QuotaDisplay.Expired(w,now);
  bool stale=s!=null&&(now-s.At>TimeSpan.FromMinutes(5)||s.At>now.AddMinutes(1));
  var phase=failed?CodexTrayPhase.Failed:!valid?CodexTrayPhase.Unknown:expired?CodexTrayPhase.Expired:stale?CodexTrayPhase.Stale:!s.Official?CodexTrayPhase.Cached:CodexTrayPhase.Fresh;
  string number=phase==CodexTrayPhase.Failed||phase==CodexTrayPhase.Expired?"!":!valid?"?":Math.Floor(Math.Max(0,Math.Min(100,w.Remaining))).ToString("0",Invariant);
  Color color=phase==CodexTrayPhase.Failed||phase==CodexTrayPhase.Expired?Color.FromArgb(166,49,49):phase==CodexTrayPhase.Cached||phase==CodexTrayPhase.Stale?Color.FromArgb(176,103,17):phase==CodexTrayPhase.Fresh?Color.FromArgb(17,116,120):Color.FromArgb(66,83,104);
  string pct=valid?w.Remaining.ToString("G15",Invariant)+"%":english?"unknown":"未知";
  string state;
  if(phase==CodexTrayPhase.Failed)state=english?(valid?(s.Official?"failed/cache":"failed log/unverified"):"failed/no data"):(valid?(s.Official?"查询失败·缓存":"查询失败·日志未核验"):"查询失败·无数据");
  else if(phase==CodexTrayPhase.Expired)state=english?(s.Official?"expired; await refresh":"expired log/unverified"):(s.Official?"已到重置·待刷新":"已到重置·日志未核验");
  else if(phase==CodexTrayPhase.Stale)state=english?(s.Official?"stale/cache":"stale log/unverified"):(s.Official?"过期缓存":"过期日志·账户未核验");
  else if(phase==CodexTrayPhase.Cached)state=english?"log cache/unverified":"日志缓存·账户未核验";
  else if(phase==CodexTrayPhase.Fresh)state=english?"official fresh":"官方·新鲜";
  else state=english?"unavailable":"暂无可靠数据";
  bool last=valid&&phase!=CodexTrayPhase.Fresh;
  string remaining=(last?(english?"last ":"上次 "):"")+pct;
  var reset=QuotaDisplay.ResetAt(w);string resetText=reset.HasValue?reset.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss'Z'",Invariant):(english?"unknown":"未知");
  string fetched=s==null?(english?"unknown":"无"):s.At.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss'Z'",Invariant);
  string age=s==null?"":Age(s.At,now);
  string tip="Codex "+remaining+" | "+state+"\n"+(english?"Reset ":"重置 ")+resetText+"\n"+(english?"Fetched ":"取数 ")+fetched+(s==null?"":" ("+age+")");
  // Keep all values/timestamps. Never substring-truncate a reset time or freshness state.
  if(tip.Length>127)tip="Codex "+remaining+" | "+state+"\nR "+resetText+"\nAt "+fetched+(s==null?"":" ("+age+")");
  if(tip.Length>127)throw new InvalidOperationException("Tray tooltip budget exceeded");
  return new CodexTrayPlan {Number=number,Percent=pct,Color=color,Phase=phase,Underline=phase==CodexTrayPhase.Stale||phase==CodexTrayPhase.Cached||phase==CodexTrayPhase.Unknown,Tooltip=tip};
 }
 static string Glyph(char c){switch(c){case '0':return "111101101101111";case '1':return "010110010010111";case '2':return "111001111100111";case '3':return "111001111001111";case '4':return "101101111001001";case '5':return "111100111001111";case '6':return "111100111101111";case '7':return "111001010010010";case '8':return "111101111101111";case '9':return "111101111001111";case '!':return "010010010000010";default:return "111001010000010";}}
 public static Bitmap Draw(CodexTrayPlan plan,int size){if(size<16||size>256)throw new ArgumentOutOfRangeException("size");var bmp=new Bitmap(size,size,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(bmp)){g.Clear(plan.Color);string text=plan.Number;int cells=text.Length*4-1,scale=Math.Max(1,Math.Min((size-2)/cells,(size-4)/5)),left=(size-cells*scale)/2,top=(size-5*scale)/2-1;for(int digit=0;digit<text.Length;digit++){string glyph=Glyph(text[digit]);for(int i=0;i<15;i++)if(glyph[i]=='1')g.FillRectangle(Brushes.White,left+(digit*4+i%3)*scale,top+i/3*scale,scale,scale);}if(plan.Underline)using(var pen=new Pen(Color.White,Math.Max(1,size/16)))g.DrawLine(pen,size/4,size-2,size*3/4,size-2);}return bmp;}
 public static Icon IconFor(CodexTrayPlan plan,int size,string skin){using(var bmp=Draw(plan,size,skin)){var handle=bmp.GetHicon();try{using(var borrowed=Icon.FromHandle(handle))return (Icon)borrowed.Clone();}finally{DestroyIcon(handle);}}}
 public static Bitmap Draw(CodexTrayPlan plan,int size,string skin){double value;double? ratio=double.TryParse(plan.Percent.TrimEnd('%'),NumberStyles.Float,Invariant,out value)?(double?)value:null;Color color=plan.Phase==CodexTrayPhase.Fresh?Color.FromArgb(70,225,158):plan.Color;return SkinTray.Draw(plan.Number,ratio,color,plan.Underline,skin,size);}
 public static Icon IconFor(CodexTrayPlan plan,int size){using(var bmp=Draw(plan,size)){var handle=bmp.GetHicon();try{using(var borrowed=Icon.FromHandle(handle))return (Icon)borrowed.Clone();}finally{DestroyIcon(handle);}}}
}
