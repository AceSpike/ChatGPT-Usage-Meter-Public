import {createInterface} from 'node:readline';
import {readFile} from 'node:fs/promises';
import {pathToFileURL} from 'node:url';
import {CodexClient} from './codex.mjs';
import {QuotaService} from './quota.mjs';
const uri='ui://codex-usage/dashboard-v1';
export function attachServer(input,output,service){
  const send=v=>output.write(JSON.stringify(v)+'\n');
  const lines=createInterface({input});
  lines.on('line',async line=>{
    let r;try {if(line.length>2097152)return;r=JSON.parse(line);}catch {send({jsonrpc:'2.0',id:null,error:{code:-32700,message:'Invalid JSON'}});return;}
    if(r.id===undefined)return;
    try {
      let result;
      switch(r.method){
        case 'initialize':result={protocolVersion:'2025-11-25',serverInfo:{name:'codex-usage',version:'0.1.0-private-test.2'},capabilities:{tools:{},resources:{}}};break;
        case 'ping':result={};break;
        case 'tools/list':result={tools:[{name:'get_usage',title:'Codex Usage',description:'Read remaining Codex quota for the local CLI sign-in. Not ChatGPT message quota or API billing.',inputSchema:{type:'object',properties:{},additionalProperties:false},annotations:{readOnlyHint:true,destructiveHint:false,idempotentHint:true,openWorldHint:true},_meta:{ui:{resourceUri:uri},'openai/ui':{entrypoints:[{type:'global'},{type:'thread'}]}}}]};break;
        case 'tools/call':if(r.params?.name!=='get_usage'||Object.keys(r.params.arguments||{}).length)throw new Error('INVALID');{const data=await service.get();result={content:[{type:'text',text:JSON.stringify(data)}],structuredContent:data};}break;
        case 'resources/list':result={resources:[{uri,name:'Codex Usage dashboard',mimeType:'text/html;profile=mcp-app'}]};break;
        case 'resources/read':if(r.params?.uri!==uri)throw new Error('INVALID');result={contents:[{uri,mimeType:'text/html;profile=mcp-app',text:await readFile(new URL('../ui/index.html',import.meta.url),'utf8'),_meta:{ui:{prefersBorder:true,csp:{connectDomains:[],resourceDomains:[]}}}}]};break;
        default:send({jsonrpc:'2.0',id:r.id,error:{code:-32601,message:'Method not found'}});return;
      }
      send({jsonrpc:'2.0',id:r.id,result});
    }catch {send({jsonrpc:'2.0',id:r.id,error:{code:-32602,message:'Invalid request or unavailable resource'}});}
  });return lines;
}
if(process.argv[1]&&import.meta.url===pathToFileURL(process.argv[1]).href){
  const client=new CodexClient();const lines=attachServer(process.stdin,process.stdout,new QuotaService(()=>client.read()));
  const stop=()=>{client.close();lines.close();};process.stdin.on('end',stop);process.once('SIGTERM',()=>{stop();process.exit(0);});process.once('SIGINT',()=>{stop();process.exit(0);});
}
