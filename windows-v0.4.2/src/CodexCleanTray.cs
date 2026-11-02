using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Own native tray window; no reflection into WinForms, no Explorer process access.
// NOTIFYICONDATAW carries a 128-WCHAR tooltip (127 characters plus terminator).
public sealed class CodexCleanTray:IDisposable {
 const uint Add=0,Modify=1,Delete=2,SetFocus=3,SetVersion=4,MessageFlag=1,IconFlag=2,TipFlag=4,ShowTipFlag=128;
 const int Callback=0x8000+129;const uint Id=1;
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct Data {
  public uint cbSize;public IntPtr hWnd;public uint uID,uFlags,uCallbackMessage;public IntPtr hIcon;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string szTip;
  public uint dwState,dwStateMask;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)]public string szInfo;
  public uint uTimeoutOrVersion;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=64)]public string szInfoTitle;
  public uint dwInfoFlags;public Guid guidItem;public IntPtr hBalloonIcon;
 }
 [StructLayout(LayoutKind.Sequential)]struct Identity {public uint cbSize;public IntPtr hWnd;public uint uID;public Guid guidItem;}
 [StructLayout(LayoutKind.Sequential)]struct Rect {public int Left,Top,Right,Bottom;}
 [DllImport("shell32.dll",CharSet=CharSet.Unicode,EntryPoint="Shell_NotifyIconW")]static extern bool Notify(uint command,ref Data data);
 [DllImport("shell32.dll")]static extern int Shell_NotifyIconGetRect(ref Identity id,out Rect rect);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern uint RegisterWindowMessage(string name);
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr handle);
 [DllImport("user32.dll")]static extern bool PostMessage(IntPtr handle,uint msg,IntPtr w,IntPtr l);
 sealed class Sink:NativeWindow {readonly CodexCleanTray owner;internal Sink(CodexCleanTray value){owner=value;CreateHandle(new CreateParams {Caption="CodexUsageMeter.TrayHost.v04"});}internal void Close(){DestroyHandle();}protected override void WndProc(ref Message m){owner.Process(ref m);base.WndProc(ref m);}}
 readonly Sink sink;readonly uint taskbarCreated;bool visible,registered,version4,disposed;Icon icon;string text="";ContextMenuStrip menu;
 public event MouseEventHandler MouseClick;
 public ContextMenuStrip ContextMenuStrip {get{return menu;}set{if(object.ReferenceEquals(menu,value))return;if(menu!=null)menu.Closed-=MenuClosed;menu=value;if(menu!=null)menu.Closed+=MenuClosed;}}
 void MenuClosed(object sender,ToolStripDropDownClosedEventArgs e){if(!disposed&&registered){var d=NewData(0);Notify(SetFocus,ref d);}}
 public bool Registered {get{return registered;}}
 public bool LastUpdateSucceeded {get;private set;}
 public bool Version4 {get{return version4;}}
 public static int NativeDataSize {get{return Marshal.SizeOf(typeof(Data));}}
 public CodexCleanTray(){taskbarCreated=RegisterWindowMessage("TaskbarCreated");sink=new Sink(this);}
 public Icon Icon {get{return icon;}set{icon=value;Update(IconFlag);}}
 public string Text {get{return text;}set{if(value!=null&&value.Length>127)throw new ArgumentOutOfRangeException("value","Native tooltip maximum is 127 characters");text=value??"";Update(TipFlag|ShowTipFlag);}}
 public bool Visible {get{return visible;}set{if(disposed)return;visible=value;if(value){if(!registered)Register();}else Remove();}}
 Data NewData(uint flags){return new Data {cbSize=(uint)Marshal.SizeOf(typeof(Data)),hWnd=sink.Handle,uID=Id,uFlags=flags,uCallbackMessage=Callback,hIcon=icon==null?IntPtr.Zero:icon.Handle,szTip=text,szInfo="",szInfoTitle=""};}
 void Register(){if(disposed||!visible||icon==null)return;version4=false;var data=NewData(MessageFlag|IconFlag|TipFlag|ShowTipFlag);registered=Notify(Add,ref data);LastUpdateSucceeded=registered;if(registered){data.uTimeoutOrVersion=4;version4=Notify(SetVersion,ref data);}}
 void Update(uint flags){if(disposed||!visible)return;if(!registered){Register();return;}var data=NewData(flags);LastUpdateSucceeded=Notify(Modify,ref data);}
 void Remove(){if(!registered)return;var data=NewData(0);Notify(Delete,ref data);registered=false;version4=false;}
 public bool TryGetBounds(out Rectangle bounds){var id=new Identity {cbSize=(uint)Marshal.SizeOf(typeof(Identity)),hWnd=sink.Handle,uID=Id};Rect r;int result=Shell_NotifyIconGetRect(ref id,out r);bounds=Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom);return result==0&&bounds.Width>0&&bounds.Height>0;}
 void Process(ref Message m){if(disposed)return;if(taskbarCreated!=0&&m.Msg==taskbarCreated){registered=false;Register();return;}if(m.Msg!=Callback)return;int message=(int)(m.LParam.ToInt64()&0xffff);
  if(message==(version4?0x400:0x202)||message==0x401){var handler=MouseClick;if(handler!=null)handler(this,new MouseEventArgs(MouseButtons.Left,1,0,0,0));}
  else if(message==(version4?0x7b:0x205)){var menu=ContextMenuStrip;if(menu==null||menu.IsDisposed)return;Point position=Cursor.Position;Rectangle r;if(TryGetBounds(out r)&&!r.Contains(position))position=new Point(r.Left,r.Bottom);SetForegroundWindow(sink.Handle);menu.Show(position);PostMessage(sink.Handle,0,IntPtr.Zero,IntPtr.Zero);}
 }
 public void Dispose(){if(disposed)return;Remove();disposed=true;if(menu!=null)menu.Closed-=MenuClosed;sink.Close();}
}
