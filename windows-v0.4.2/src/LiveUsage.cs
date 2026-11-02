using System;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

// Uses only the documented read-only rate-limit RPC. Authentication is owned by installed Codex.
public sealed class CodexReadException : InvalidOperationException {
    public readonly string Code;
    public CodexReadException(string code):base(code){Code=code;}
}
public sealed class LiveUsage : IDisposable {
    Process process;
    BlockingCollection<string> lines;
    int sequence;
    volatile bool disposed;
    readonly object sync=new object();
    readonly Func<string> executableFinder;
    internal int ReadTimeoutMs=15000;
    public LiveUsage():this(FindExecutable){}
    internal LiveUsage(Func<string> finder){executableFinder=finder;}
    static string[] Directories(string path,string pattern){try{return Directory.Exists(path)?Directory.GetDirectories(path,pattern):new string[0];}catch(IOException){return new string[0];}catch(UnauthorizedAccessException){return new string[0];}}
    static string Latest(IEnumerable<string> paths){return paths.Where(File.Exists).OrderByDescending(path=>{try{return File.GetLastWriteTimeUtc(path);}catch{return DateTime.MinValue;}}).ThenBy(path=>path,StringComparer.OrdinalIgnoreCase).FirstOrDefault();}
    public static string FindExecutable(){return FindExecutable(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));}
    // Resolve on each new connection; directory names are opaque version IDs, not sortable versions.
    // npm remains preferred so an existing CLI/account choice does not silently switch.
    public static string FindExecutable(string roamingRoot,string localRoot){
        var npm=new List<string>();
        var package=Path.Combine(roamingRoot,"npm","node_modules","@openai","codex","node_modules","@openai");
        foreach(var dir in Directories(package,"codex-win32-*"))foreach(var target in Directories(Path.Combine(dir,"vendor"),"*"))npm.Add(Path.Combine(target,"bin","codex.exe"));
        string preferred=Latest(npm);if(preferred!=null)return preferred;
        var desktopRoot=Path.Combine(localRoot,"OpenAI","Codex","bin");
        var desktop=Directories(desktopRoot,"*").Select(dir=>Path.Combine(dir,"codex.exe"));
        string bundled=Latest(desktop);if(bundled!=null)return bundled;
        string direct=Path.Combine(desktopRoot,"codex.exe");if(File.Exists(direct))return direct;
        throw new CodexReadException("cli-not-found");
    }
    public static string ErrorCode(Exception error){var known=error as CodexReadException;return known==null?"read-failed":known.Code;}
    public static string ErrorText(string code,bool english){switch(code){
        case "cli-not-found":return english?"Codex CLI not found (npm/desktop)":"未找到 Codex CLI（npm/桌面版）";
        case "cli-start":return english?"Codex CLI could not start":"Codex CLI 启动失败";
        case "timeout":return english?"Codex read timed out":"Codex 查询超时";
        case "rpc-error":return english?"Codex rejected query; check sign-in":"Codex 拒绝查询，请检查登录";
        case "invalid-response":return english?"Invalid quota response":"Codex 返回无效额度";
        default:return english?"Codex read failed":"Codex 读取失败";
    }}
    void Start() {
        if(disposed)throw new InvalidOperationException("额度连接已关闭");
        if(process!=null && !process.HasExited)return;
        Stop();
        var incoming=new BlockingCollection<string>(); lines=incoming;
        process=new Process { StartInfo=new ProcessStartInfo {
            FileName=executableFinder(),Arguments="app-server --stdio",UseShellExecute=false,CreateNoWindow=true,
            WorkingDirectory=AppDomain.CurrentDomain.BaseDirectory,
            RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,
            StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8
        }};
        process.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs args) {
            if(args.Data!=null && args.Data.Length<=2097152)incoming.Add(args.Data);
            if(args.Data==null)incoming.CompleteAdding();
        };
        // Do not log stderr; it can contain account details from the CLI.
        process.ErrorDataReceived+=delegate { };
        try {process.Start();}catch{throw new CodexReadException("cli-start");} process.BeginOutputReadLine(); process.BeginErrorReadLine();
        Call("initialize",new { clientInfo=new { name="subscription_usage_tray",version="0.4.2" } });
        process.StandardInput.WriteLine("{\"method\":\"initialized\",\"params\":{}}");process.StandardInput.Flush();
    }
    Dictionary<string,object> Call(string method,object parameters) {
        var json=new JavaScriptSerializer { MaxJsonLength=2097152 }; int id=++sequence;
        process.StandardInput.WriteLine(json.Serialize(new { id=id,method=method,@params=parameters }));process.StandardInput.Flush();
        var watch=Stopwatch.StartNew();
        while(watch.ElapsedMilliseconds<ReadTimeoutMs) {
            string line;
            if(!lines.TryTake(out line,Math.Max(1,ReadTimeoutMs-(int)watch.ElapsedMilliseconds)))throw new CodexReadException("timeout");
            Dictionary<string,object> response;
            try { response=Reader.Dict(json.DeserializeObject(line)); } catch { continue; }
            object responseId=Reader.Get(response,"id");
            if(Reader.Get(response,"method")!=null) {
                // Never service sign-in/credential requests or invoke tools.
                if(responseId!=null) { process.StandardInput.WriteLine(json.Serialize(new { id=responseId,error=new { code=-32601,message="Read-only quota client" } }));process.StandardInput.Flush(); }
                continue;
            }
            if(responseId==null || !object.Equals(Convert.ToString(responseId),Convert.ToString(id)))continue;
            if(Reader.Get(response,"error")!=null)throw new CodexReadException("rpc-error");
            return Reader.Dict(Reader.Get(response,"result"));
        }
        throw new CodexReadException("timeout");
    }
    public static Snapshot Parse(Dictionary<string,object> result,DateTimeOffset at) {
        var buckets=Reader.Dict(Reader.Get(result,"rateLimitsByLimitId"));
        var bucket=buckets==null?Reader.Dict(Reader.Get(result,"rateLimits")):Reader.Dict(Reader.Get(buckets,"codex"));
        var id=Reader.Get(bucket,"limitId") as string;
        if(bucket==null || id!=null && id!="codex")throw new InvalidOperationException("服务未返回 Codex 额度桶");
        var primary=Reader.Window(Reader.Get(bucket,"primary"),true);var secondary=Reader.Window(Reader.Get(bucket,"secondary"),true);
        if(primary==null && secondary==null)throw new InvalidOperationException("服务未返回有效额度窗口");
        return new Snapshot { At=at,Source="account/rateLimits/read",Official=true,Primary=primary,Secondary=secondary };
    }
    public Snapshot Read() {
        lock(sync) {
            try {Start();var result=Call("account/rateLimits/read",new {});try{return Parse(result,DateTimeOffset.UtcNow);}catch{throw new CodexReadException("invalid-response");}}
            catch(CodexReadException){Stop();throw;}
            catch{Stop();throw new CodexReadException("read-failed");}
        }
    }
    void Stop() {
        var p=process; process=null;
        if(p==null)return;
        try { if(!p.HasExited)p.Kill(); } catch { }
        p.Dispose();
    }
    public void Dispose() { disposed=true; Stop(); }
}

public sealed class UsageState {
    public Snapshot Current;
    public bool QueryFailed;
    public string LastErrorCode;
    public void Accept(Snapshot fresh) { Current=fresh; QueryFailed=false; LastErrorCode=null; }
    public void Fail(Snapshot fallback){Fail(fallback,"read-failed");}
    public void Fail(Snapshot fallback,string code) {
        LastErrorCode=code;
        QueryFailed=true;
        // Once official data exists, do not mix it with potentially different-account session logs.
        if(Current==null || !Current.Official && fallback!=null && fallback.At>Current.At)Current=fallback;
    }
    public bool Cached { get { return Current!=null && (QueryFailed || !Current.Official || Current.Stale); } }
}
