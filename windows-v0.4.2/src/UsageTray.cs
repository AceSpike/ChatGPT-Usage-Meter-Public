using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

// Offline reader: only token_count.rate_limits is projected. No auth/config/browser files or network.
public sealed class QuotaWindow {
    public double Used;
    public double Remaining { get { return 100.0-Used; } }
    public long? Minutes, Reset;
    public bool Expired { get { return Reset.HasValue && Reset.Value <= DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }
}
public sealed class Snapshot {
    public DateTimeOffset At;
    public QuotaWindow Primary, Secondary;
    public string Source;
    public bool Official;
    public bool Stale { get { return DateTimeOffset.UtcNow - At > TimeSpan.FromMinutes(5) || At > DateTimeOffset.UtcNow.AddMinutes(1); } }
}
public static class Reader {
    public static Dictionary<string,object> Dict(object value) { return value as Dictionary<string,object>; }
    public static object Get(Dictionary<string,object> d,string k) { object v; return d != null && d.TryGetValue(k,out v) ? v : null; }
    static long? Number(object v) {
        if(v==null)return null;
        try { return Convert.ToInt64(v,CultureInfo.InvariantCulture); } catch { return null; }
    }
    public static QuotaWindow Window(object value,bool official=false) {
        var d=Dict(value); var raw=Get(d,official?"usedPercent":"used_percent");
        if(d==null || raw==null)return null;
        double used;
        try { used=Convert.ToDouble(raw,CultureInfo.InvariantCulture); } catch { return null; }
        if(double.IsNaN(used)||double.IsInfinity(used)||used<0||used>100)return null;
        return new QuotaWindow { Used=used,Minutes=Number(Get(d,official?"windowDurationMins":"window_minutes")),Reset=Number(Get(d,official?"resetsAt":"resets_at")) };
    }
    public static Snapshot Parse(string line,string source) {
        try {
            var json=new JavaScriptSerializer { MaxJsonLength=2097152 };
            var d=Dict(json.DeserializeObject(line));
            var payload=Dict(Get(d,"payload"));
            if(!object.Equals(Get(d,"type"),"event_msg") || !object.Equals(Get(payload,"type"),"token_count"))return null;
            var r=Dict(Get(payload,"rate_limits")); if(r==null)return null;
            var id=Get(r,"limit_id") as string;
            if(id!=null && id!="codex")return null;
            DateTimeOffset at;
            if(!DateTimeOffset.TryParse(Get(d,"timestamp") as string,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out at))return null;
            var primary=Window(Get(r,"primary")); var secondary=Window(Get(r,"secondary"));
            if(primary==null && secondary==null || at>DateTimeOffset.UtcNow.AddMinutes(1))return null;
            return new Snapshot { At=at,Primary=primary,Secondary=secondary,Source=source };
        } catch { return null; }
    }
    public static Snapshot Read(string root,out string status) {
        status="尚未找到 Codex 额度记录";
        if(!Directory.Exists(root)) { status="本机没有可读的 Codex 会话目录"; return null; }
        Snapshot latest=null;
        try {
            // Bound filesystem work and bytes read; never read auth.json or config.toml.
            var days=Directory.GetDirectories(root).OrderByDescending(x=>x).Take(2)
                .SelectMany(y=>Directory.GetDirectories(y).OrderByDescending(x=>x).Take(2))
                .SelectMany(m=>Directory.GetDirectories(m).OrderByDescending(x=>x).Take(3));
            var files=days.SelectMany(d=>Directory.GetFiles(d,"*.jsonl")).Select(x=>new FileInfo(x))
                .OrderByDescending(f=>f.LastWriteTimeUtc).Take(32);
            foreach(var file in files) {
                try {
                    using(var stream=new FileStream(file.FullName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)) {
                        bool partial=stream.Length>2097152;
                        stream.Seek(Math.Max(0,stream.Length-2097152),SeekOrigin.Begin);
                        using(var text=new StreamReader(stream,Encoding.UTF8)) {
                            if(partial)text.ReadLine();
                            string line;
                            while((line=text.ReadLine())!=null) {
                                if(line.IndexOf("\"rate_limits\"",StringComparison.Ordinal)<0)continue;
                                var s=Parse(line,file.Name);
                                if(s!=null && (latest==null || s.At>latest.At))latest=s;
                            }
                        }
                    }
                } catch(IOException) { } catch(UnauthorizedAccessException) { }
            }
            if(latest!=null)status=latest.Stale ? "记录已超过 5 分钟 · 等待 Codex 新记录" : "本地额度快照 · 每 30 秒检查记录";
        } catch(UnauthorizedAccessException) { status="会话目录访问受限"; }
          catch(IOException) { status="会话目录暂时不可读"; }
        return latest;
    }
}

public static class Program {
    public static readonly string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex","sessions");
    [STAThread] public static int Main(string[] args) {
        if(args.Any(a=>a.StartsWith("--")))Console.OutputEncoding=Encoding.UTF8;
        if(args.Contains("--self-test")) { int code=SelfTest();return code==0?RuntimeChecks.Run():code; }
        if(args.Contains("--render") || args.Contains("--live-render")) {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            Snapshot snapshot=null;
            if(args.Contains("--live-render"))using(var client=new LiveUsage())snapshot=client.Read();
            return RuntimeChecks.Render(snapshot,args.Contains("--live-render"));
        }
        if(args.Contains("--exit")) { EventWaitHandle signal;if(EventWaitHandle.TryOpenExisting("Local\\CodexUsageTray_Exit_v3",out signal))using(signal)signal.Set();else { Console.WriteLine("No v3 instance is running; older versions must be exited using their tray menu.");return 1; }return 0; }
        if(args.Contains("--check") || args.Contains("--live-check")) {
            using(var client=new LiveUsage()) {
                int count=args.Contains("--live-check")?2:1;
                for(int i=0;i<count;i++) {
                    string message; Snapshot s;
                    try { s=args.Contains("--live-check")?client.Read():Reader.Read(Root,out message); }
                    catch(InvalidOperationException e) { Console.WriteLine(new JavaScriptSerializer().Serialize(new { error=e.Message })); return 1; }
                    var data=new { source=s==null?null:s.Official?"account/rateLimits/read":"local_session_snapshot",timestamp=s==null?(string)null:s.At.ToString("o"),stale=s==null||s.Stale,
                        primary=s==null||s.Primary==null?null:new { remainingPercent=s.Primary.Remaining,usedPercent=s.Primary.Used,windowMinutes=s.Primary.Minutes,resetsAt=s.Primary.Reset,hoursUntilReset=QuotaDisplay.Hours(s.Primary,DateTimeOffset.UtcNow),expired=s.Primary.Expired },
                        secondary=s==null||s.Secondary==null?null:new { remainingPercent=s.Secondary.Remaining,usedPercent=s.Secondary.Used,windowMinutes=s.Secondary.Minutes,resetsAt=s.Secondary.Reset,hoursUntilReset=QuotaDisplay.Hours(s.Secondary,DateTimeOffset.UtcNow),expired=s.Secondary.Expired } };
                    Console.WriteLine(new JavaScriptSerializer().Serialize(data));
                }
            }
            return 0;
        }
        bool created;
        using(var single=new Mutex(true,"Local\\CodexUsageTray_20261030",out created)) {
            if(!created) { EventWaitHandle signal;if(EventWaitHandle.TryOpenExisting("Local\\CodexUsageTray_Show_v3",out signal))using(signal)signal.Set();else MessageBox.Show("旧版仍在运行。请从旧版托盘菜单退出后，再打开横向版。不会启动重复实例。","Codex 用量");return 0; }
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            using(var dashboard=new HorizontalDashboard(Root))Application.Run(dashboard);
            single.ReleaseMutex();
        }
        return 0;
    }
    static int SelfTest() {
        string head="{\"timestamp\":\"2026-10-30T02:00:00Z\",\"type\":\"event_msg\",\"payload\":{\"type\":\"token_count\",\"rate_limits\":";
        Func<string,Snapshot> parse=r=>Reader.Parse(head+r+"}}","fixture");
        var s=parse("{\"limit_id\":\"codex\",\"primary\":{\"used_percent\":28,\"window_minutes\":10080,\"resets_at\":1793774222},\"secondary\":null}");
        if(s==null||s.Primary==null||s.Primary.Used!=28||s.Primary.Minutes!=10080||s.Secondary!=null)return 1;
        if(parse("{\"limit_id\":\"other\"}")!=null)return 2;
        if(parse("{\"primary\":{\"used_percent\":101}} ")!=null)return 3;
        if(parse("{\"primary\":{\"used_percent\":null}} ")!=null)return 4;
        if(Reader.Parse("garbage","fixture")!=null)return 5;
        if(Reader.Parse("{\"type\":\"response_item\",\"payload\":{\"rate_limits\":{}}}","fixture")!=null)return 6;
        var expired=parse("{\"primary\":{\"used_percent\":100,\"resets_at\":1}}");
        if(!expired.Primary.Expired)return 7;
        string status;
        if(Reader.Read(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString()),out status)!=null)return 8;
        if(s.Primary.Remaining!=72 || new QuotaWindow { Used=100 }.Remaining!=0 || new QuotaWindow { Used=0 }.Remaining!=100)return 9;
        var json=new JavaScriptSerializer();
        var official=LiveUsage.Parse(Reader.Dict(json.DeserializeObject("{\"rateLimitsByLimitId\":{\"codex\":{\"primary\":{\"usedPercent\":29,\"windowDurationMins\":10080,\"resetsAt\":1793774222},\"secondary\":{\"usedPercent\":80,\"windowDurationMins\":300,\"resetsAt\":1793774222}}}}")),DateTimeOffset.UtcNow);
        if(official.Primary.Remaining!=71 || official.Primary.Minutes!=10080 || official.Secondary.Remaining!=20 || official.Secondary.Minutes!=300)return 10;
        var state=new UsageState(); state.Accept(official); state.Fail(s);
        if(state.Current!=official || !state.Cached)return 11;
        state.Accept(official); if(state.Cached)return 12;
        var old=new Snapshot { At=DateTimeOffset.UtcNow.AddHours(-1),Primary=s.Primary,Official=true };
        state.Accept(old); if(!state.Cached || state.Current.Primary.Remaining!=72)return 13;
        if(parse("{}")!=null)return 14;
        try { LiveUsage.Parse(Reader.Dict(json.DeserializeObject("{\"rateLimits\":{\"limitId\":\"other\",\"primary\":{\"usedPercent\":0}}}")),DateTimeOffset.UtcNow); return 15; } catch(InvalidOperationException) { }
        try { LiveUsage.Parse(Reader.Dict(json.DeserializeObject("{\"rateLimits\":{\"primary\":null,\"secondary\":null}}")),DateTimeOffset.UtcNow); return 16; } catch(InvalidOperationException) { }
        Console.WriteLine("PASS: remaining math (0/28/29/80/100), both window durations, official bucket/schema, invalid/null/absent data, expired reset, cache retention and recovery, stale-cache visibility.");
        return 0;
    }
}
