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
public sealed class LiveUsage : IDisposable {
    Process process;
    BlockingCollection<string> lines;
    int sequence;
    volatile bool disposed;
    readonly object sync=new object();
    public static string FindExecutable() {
        var package=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"npm","node_modules","@openai","codex","node_modules","@openai");
        if(!Directory.Exists(package))throw new InvalidOperationException("未找到已安装的 Codex CLI");
        foreach(var dir in Directory.GetDirectories(package,"codex-win32-*")) {
            var vendor=Path.Combine(dir,"vendor"); if(!Directory.Exists(vendor))continue;
            foreach(var target in Directory.GetDirectories(vendor)) {
                var exe=Path.Combine(target,"bin","codex.exe"); if(File.Exists(exe))return exe;
            }
        }
        throw new InvalidOperationException("未找到 Codex CLI 可执行文件");
    }
    void Start() {
        if(disposed)throw new InvalidOperationException("额度连接已关闭");
        if(process!=null && !process.HasExited)return;
        Stop();
        var incoming=new BlockingCollection<string>(); lines=incoming;
        process=new Process { StartInfo=new ProcessStartInfo {
            FileName=FindExecutable(),Arguments="app-server --stdio",UseShellExecute=false,CreateNoWindow=true,
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
        process.Start(); process.BeginOutputReadLine(); process.BeginErrorReadLine();
        Call("initialize",new { clientInfo=new { name="subscription_usage_tray",version="3.0.0" } });
        process.StandardInput.WriteLine("{\"method\":\"initialized\",\"params\":{}}");process.StandardInput.Flush();
    }
    Dictionary<string,object> Call(string method,object parameters) {
        var json=new JavaScriptSerializer { MaxJsonLength=2097152 }; int id=++sequence;
        process.StandardInput.WriteLine(json.Serialize(new { id=id,method=method,@params=parameters }));process.StandardInput.Flush();
        var watch=Stopwatch.StartNew();
        while(watch.ElapsedMilliseconds<15000) {
            string line;
            if(!lines.TryTake(out line,Math.Max(1,15000-(int)watch.ElapsedMilliseconds)))throw new InvalidOperationException("官方查询超时");
            Dictionary<string,object> response;
            try { response=Reader.Dict(json.DeserializeObject(line)); } catch { continue; }
            object responseId=Reader.Get(response,"id");
            if(Reader.Get(response,"method")!=null) {
                // Never service sign-in/credential requests or invoke tools.
                if(responseId!=null) { process.StandardInput.WriteLine(json.Serialize(new { id=responseId,error=new { code=-32601,message="Read-only quota client" } }));process.StandardInput.Flush(); }
                continue;
            }
            if(responseId==null || !object.Equals(Convert.ToString(responseId),Convert.ToString(id)))continue;
            if(Reader.Get(response,"error")!=null)throw new InvalidOperationException("官方查询失败，请检查 Codex CLI 的登录及连接");
            return Reader.Dict(Reader.Get(response,"result"));
        }
        throw new InvalidOperationException("官方查询超时");
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
            try { Start(); return Parse(Call("account/rateLimits/read",new { }),DateTimeOffset.UtcNow); }
            catch(InvalidOperationException) { Stop(); throw; }
            catch { Stop(); throw new InvalidOperationException("官方连接暂不可用"); }
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
    public void Accept(Snapshot fresh) { Current=fresh; QueryFailed=false; }
    public void Fail(Snapshot fallback) {
        QueryFailed=true;
        // Once official data exists, do not mix it with potentially different-account session logs.
        if(Current==null || !Current.Official && fallback!=null && fallback.At>Current.At)Current=fallback;
    }
    public bool Cached { get { return Current!=null && (QueryFailed || !Current.Official || Current.Stale); } }
}
