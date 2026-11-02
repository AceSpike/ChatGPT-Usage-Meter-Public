import {spawn} from 'node:child_process';
import {createInterface} from 'node:readline';
import {readdir,access} from 'node:fs/promises';
import {join} from 'node:path';
export async function findCodex(){
  if(process.env.CODEX_CLI_PATH){await access(process.env.CODEX_CLI_PATH);return process.env.CODEX_CLI_PATH;}
  if(process.platform!=='win32')return 'codex';
  if(process.env.APPDATA){
    const root=join(process.env.APPDATA,'npm','node_modules','@openai','codex','node_modules','@openai');
    try {for(const name of await readdir(root)){if(!name.startsWith('codex-win32-'))continue;const vendor=join(root,name,'vendor');for(const target of await readdir(vendor)){const exe=join(vendor,target,'bin','codex.exe');try {await access(exe);return exe;}catch {}}}}catch {}
  }
  for(const dir of (process.env.PATH||'').split(';')){const exe=join(dir,'codex.exe');try {await access(exe);return exe;}catch {}}
  throw new Error('CODEX_NOT_FOUND');
}
export class CodexClient {
  constructor(){this.child=null;this.seq=0;this.requests=new Map();}
  async start(){
    if(this.child && this.child.exitCode===null)return;
    const child=spawn(await findCodex(),['app-server','--stdio'],{shell:false,windowsHide:true,stdio:['pipe','pipe','ignore']});this.child=child;
    const lines=createInterface({input:child.stdout});
    lines.on('line',line=>{
      if(line.length>2097152)return;
      let v;try {v=JSON.parse(line);}catch {return;}
      if(v.method){if(v.id!==undefined)this.send({id:v.id,error:{code:-32601,message:'Read-only quota client'}});return;}
      const request=this.requests.get(v.id);if(!request)return;this.requests.delete(v.id);clearTimeout(request.timer);
      if(v.error)request.reject(new Error('QUOTA_RPC_FAILED'));else request.resolve(v.result);
    });
    const stop=()=>{if(this.child!==child)return;this.child=null;for(const r of this.requests.values()){clearTimeout(r.timer);r.reject(new Error('CODEX_DISCONNECTED'));}this.requests.clear();};
    child.on('error',stop);child.on('exit',stop);
    await this.call('initialize',{clientInfo:{name:'codex_usage_plugin',version:'0.1.0-private-test.2'}});
    this.send({method:'initialized',params:{}});
  }
  send(v){if(this.child?.stdin.writable)this.child.stdin.write(JSON.stringify(v)+'\n');}
  call(method,params){return new Promise((resolve,reject)=>{
    const id=++this.seq,timer=setTimeout(()=>{this.requests.delete(id);reject(new Error('CODEX_TIMEOUT'));},12000);
    this.requests.set(id,{resolve,reject,timer});this.send({id,method,params});
  });}
  async read(){try {await this.start();return await this.call('account/rateLimits/read',{});}catch {this.close();throw new Error('QUOTA_UNAVAILABLE');}}
  close(){const child=this.child;this.child=null;if(child){child.stdin.end();child.kill();}for(const r of this.requests.values()){clearTimeout(r.timer);r.reject(new Error('CLOSED'));}this.requests.clear();}
}
