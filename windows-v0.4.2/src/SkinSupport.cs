using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Web.Script.Serialization;

// Appearance codes only; no account, quota, profile path or credential is persisted here.
public sealed class SkinSettings {
 public string Tray="A", Window="A"; public bool English; public bool Saved=true;
 public static string DirectoryOverride;
 public static bool Valid(string code){return code=="A"||code=="B"||code=="C"||code=="D";}
 public static string PathFor(string app){return Path.Combine(DirectoryOverride??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),app),"skins.json");}
 public static SkinSettings Load(string app){var s=new SkinSettings();try{string p=PathFor(app);if(new FileInfo(p).Length>2048)return s;var d=new JavaScriptSerializer().Deserialize<System.Collections.Generic.Dictionary<string,object>>(File.ReadAllText(p,Encoding.UTF8));object v;if(d.TryGetValue("tray",out v)&&Valid(v as string))s.Tray=(string)v;if(d.TryGetValue("window",out v)&&Valid(v as string))s.Window=(string)v;if(d.TryGetValue("language",out v))s.English=(v as string)=="en";}catch{}return s;}
 public void Save(string app){string p=PathFor(app),temp=p+".tmp";try{Directory.CreateDirectory(Path.GetDirectoryName(p));File.WriteAllText(temp,new JavaScriptSerializer().Serialize(new{tray=Tray,window=Window,language=English?"en":"zh-CN"}),new UTF8Encoding(false));if(File.Exists(p))File.Replace(temp,p,null);else File.Move(temp,p);Saved=true;}catch{Saved=false;try{File.Delete(temp);}catch{}}}
}
public sealed class SkinMenus {
 readonly ToolStripMenuItem trayMenu=new ToolStripMenuItem(),windowMenu=new ToolStripMenuItem();
 readonly ToolStripMenuItem[] trays=new ToolStripMenuItem[4],windows=new ToolStripMenuItem[4];
 readonly SkinSettings settings;
 public SkinMenus(ContextMenuStrip menu,SkinSettings state,Action changed){settings=state;menu.Items.Add(new ToolStripSeparator());menu.Items.Add(trayMenu);menu.Items.Add(windowMenu);for(int i=0;i<4;i++){string key=((char)('A'+i)).ToString();trays[i]=new ToolStripMenuItem("",null,delegate{settings.Tray=key;changed();});windows[i]=new ToolStripMenuItem("",null,delegate{settings.Window=key;changed();});trayMenu.DropDownItems.Add(trays[i]);windowMenu.DropDownItems.Add(windows[i]);}}
 public void Translate(bool en){trayMenu.Text=en?"Tray skin":"托盘皮肤";windowMenu.Text=en?"Floating window skin":"悬浮窗皮肤";string[] tn=en?new[]{"Thin progress ring","Ascending bars","Filled ring","Horizontal pill"}:new[]{"细圆环进度","递增圆角条","实心环","水平胶囊"};string[] wn=en?new[]{"Minimal flat","Glass","Compact progress","Fine outline"}:new[]{"简洁平面","玻璃质感","紧凑进度条","细描边"};for(int i=0;i<4;i++){string key=((char)('A'+i)).ToString();trays[i].Text=key+" · "+tn[i];windows[i].Text=key+" · "+wn[i];trays[i].Checked=settings.Tray==key;windows[i].Checked=settings.Window==key;}}
}
public class SkinWindow : Form {
 public string WindowSkin="A";public Color Accent=Color.FromArgb(72,216,162);
 protected SkinWindow(){DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);}
 public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 protected override void OnPaintBackground(PaintEventArgs e){base.OnPaintBackground(e);if(ClientSize.Width<2||ClientSize.Height<2)return;var r=new Rectangle(0,0,ClientSize.Width-1,ClientSize.Height-1);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var p=Round(r,WindowSkin=="B"?14:WindowSkin=="D"?8:10)){
 if(WindowSkin=="B")using(var b=new LinearGradientBrush(r,Color.FromArgb(45,62,79),Color.FromArgb(15,26,39),70))e.Graphics.FillPath(b,p);
 using(var pen=new Pen(WindowSkin=="D"?Color.FromArgb(161,188,215):WindowSkin=="B"?Color.FromArgb(109,131,151):Color.FromArgb(47,67,86),1))e.Graphics.DrawPath(pen,p);
 }if(WindowSkin=="B"){using(var pen=new Pen(Color.FromArgb(50,255,255,255)))e.Graphics.DrawLine(pen,15,2,Math.Max(15,Width-16),2);}}
 public void ApplySurface(string skin){WindowSkin=skin;if(Opacity>0)Opacity=skin=="B"?0.96:1;Invalidate(true);}
 protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<3||Height<3)return;using(var p=Round(new RectangleF(0,0,Width,Height),WindowSkin=="B"?14:9)){var old=Region;Region=new Region(p);if(old!=null)old.Dispose();}}
}
public sealed class SkinProgress : Control {
 public double? Remaining;public Color Accent=Color.FromArgb(72,216,162);public string Skin="A";
 public SkinProgress(){SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Color.Transparent;Height=7;Width=240;Margin=new Padding(0,5,0,7);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Width<3||Height<3)return;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(0,1,Width-1,Height-2);using(var p=SkinWindow.Round(r,Height/2f))using(var b=new SolidBrush(Color.FromArgb(53,73,92)))e.Graphics.FillPath(b,p);if(Skin=="D")using(var outline=SkinWindow.Round(r,Height/2f))using(var pen=new Pen(Color.FromArgb(125,158,188),1))e.Graphics.DrawPath(pen,outline);if(Remaining.HasValue&&Remaining.Value>0){var fill=new RectangleF(0,1,Math.Max(2,(Width-1)*(float)Math.Max(0,Math.Min(100,Remaining.Value))/100),Height-2);using(var p=SkinWindow.Round(fill,Height/2f))using(var b=new SolidBrush(Accent))e.Graphics.FillPath(b,p);}else if(!Remaining.HasValue){using(var pen=new Pen(Color.SlateGray,1)){pen.DashStyle=DashStyle.Dot;e.Graphics.DrawLine(pen,0,Height/2,Width,Height/2);}}}
}
public static class SkinTray {
 static string Glyph(char c){switch(c){case '0':return "111101101101111";case '1':return "010110010010111";case '2':return "111001111100111";case '3':return "111001111001111";case '4':return "101101111001001";case '5':return "111100111001111";case '6':return "111100111101111";case '7':return "111001010010010";case '8':return "111101111101111";case '9':return "111101111001111";case '!':return "010010010000010";default:return "111001010000010";}}
 public static Bitmap Draw(string text,double? remaining,Color accent,bool marker,string skin,int size){if(!SkinSettings.Valid(skin)||size<16||size>256)throw new ArgumentOutOfRangeException();var bitmap=new Bitmap(size,size,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;float k=size/24f;using(var bg=SkinWindow.Round(new RectangleF(0,0,size-1,size-1),4*k))using(var b=new SolidBrush(Color.FromArgb(18,32,48)))g.FillPath(b,bg);float ratio=remaining.HasValue?(float)Math.Max(0,Math.Min(1,remaining.Value/100)):0;
 float h=size*0.54f,mid=size/2f;using(var track=new Pen(Color.FromArgb(61,81,99),(skin=="C"?2.8f:1.15f)*k))using(var ink=new Pen(accent,track.Width)){ink.StartCap=ink.EndCap=LineCap.Round;
 if(skin=="A"||skin=="C"){float diameter=size-4*k;var circle=new RectangleF(mid-diameter/2,2*k,diameter,diameter);g.DrawEllipse(track,circle);if(ratio>0)g.DrawArc(ink,circle,-90,Math.Max(1,360*ratio));}
 else if(skin=="B"){for(int i=0;i<5;i++){float bh=(4+i*1.8f)*k,bw=2.6f*k,x=(3+i*3.7f)*k;using(var p=SkinWindow.Round(new RectangleF(x,h-bh,bw,bh),k))using(var b=new SolidBrush(ratio>i/5f?accent:track.Color))g.FillPath(b,p);}}
 else {var pill=new RectangleF(3*k,5*k,size-6*k,5*k);using(var p=SkinWindow.Round(pill,2.5f*k))using(var b=new SolidBrush(track.Color))g.FillPath(b,p);if(ratio>0)using(var p=SkinWindow.Round(new RectangleF(pill.X,pill.Y,Math.Max(2*k,pill.Width*ratio),pill.Height),2.5f*k))using(var b=new SolidBrush(accent))g.FillPath(b,p);}}
 g.SmoothingMode=SmoothingMode.None;bool ring=skin=="A"||skin=="C";int cells=text.Length*4-1,scale=Math.Max(1,Math.Min((size-(ring?6:2))/cells,(int)(size*(ring?0.42:0.36))/5)),left=(size-cells*scale)/2,top=ring?(size-5*scale)/2:size-5*scale-1;if(ring&&text.Length<=2&&text!="?"&&text!="!"){g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;using(var font=new Font("Segoe UI",size*0.51f,FontStyle.Bold,GraphicsUnit.Pixel))using(var format=new StringFormat(StringFormat.GenericTypographic)){format.Alignment=StringAlignment.Center;format.LineAlignment=StringAlignment.Center;g.DrawString(text,font,Brushes.White,new RectangleF(0,-0.4f,size,size),format);}}else{for(int digit=0;digit<text.Length;digit++){string glyph=Glyph(text[digit]);for(int i=0;i<15;i++)if(glyph[i]=='1')g.FillRectangle(Brushes.White,left+(digit*4+i%3)*scale,top+i/3*scale,scale,scale);}}if(marker)using(var b=new SolidBrush(accent))g.FillRectangle(b,1,size-2,1,1);
 }return bitmap;}
}
