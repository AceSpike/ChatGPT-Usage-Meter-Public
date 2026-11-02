import test from 'node:test';import assert from 'node:assert/strict';import {spawn} from 'node:child_process';import {createInterface} from 'node:readline';import {fileURLToPath} from 'node:url';
test('separate process without CLI/home/auth returns unavailable, not fabricated quota',async()=>{
 const cwd=fileURLToPath(new URL('../plugins/codex-usage',import.meta.url));
 const env={PATH:'',SystemRoot:process.env.SystemRoot||'',WINDIR:process.env.WINDIR||''};
 const child=spawn(process.execPath,['server/server.mjs'],{cwd,env,windowsHide:true,stdio:['pipe','pipe','pipe']});
 const lines=createInterface({input:child.stdout});const replies=new Map();let stderr='';child.stderr.on('data',b=>stderr+=b.toString());
 lines.on('line',line=>{const r=JSON.parse(line);replies.get(r.id)?.(r);});let id=0;
 const rpc=(method,params={})=>new Promise((resolve,reject)=>{const n=++id,t=setTimeout(()=>reject(Error('isolated MCP timeout')),5000);replies.set(n,r=>{clearTimeout(t);resolve(r);});child.stdin.write(JSON.stringify({jsonrpc:'2.0',id:n,method,params})+'\n');});
 try{assert.equal((await rpc('initialize')).result.serverInfo.name,'codex-usage');const r=await rpc('tools/call',{name:'get_usage',arguments:{}});assert.equal(r.result.structuredContent.status,'unavailable');assert.equal(r.result.structuredContent.fetchedAt,null);assert.equal(r.result.structuredContent.primary,null);assert.equal(r.result.structuredContent.secondary,null);assert.equal(stderr,'');}
 finally{lines.close();child.stdin.end();await new Promise(resolve=>{const timer=setTimeout(()=>{child.kill();resolve();},3000);child.once('exit',()=>{clearTimeout(timer);resolve();});});}
});
