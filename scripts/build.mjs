import {cp,mkdir,readFile,readdir} from 'node:fs/promises';import {resolve,join} from 'node:path';
import {createHash} from 'node:crypto';import {fileURLToPath} from 'node:url';
const root=resolve(fileURLToPath(new URL('..',import.meta.url)));const plugin=join(root,'plugins','codex-usage');
const manifest=JSON.parse(await readFile(join(plugin,'plugin.json'),'utf8')),mcp=JSON.parse(await readFile(join(plugin,'mcp.json'),'utf8'));
if(manifest.name!=='codex-usage'||mcp.mcpServers['codex-usage'].type!=='stdio'||mcp.mcpServers['codex-usage'].cwd!=='./')throw Error('Manifest mismatch');
async function audit(dir){for(const entry of await readdir(dir,{withFileTypes:true})){if(['dist','node_modules','.git'].includes(entry.name))continue;const path=join(dir,entry.name);if(entry.isDirectory()){if(path===join(root,'windows','bin')||path===join(root,'windows','test-output')||['bin','test-output','package-staging'].some(name=>path===join(root,'windows-v0.4.2',name)))continue;await audit(path);continue;}
// This historical native download is intentional; all other binaries remain rejected.
if(path===join(root,'windows','downloads','ChatGPTUsageMeter-Floating-v0.3-Windows-Test.zip')){
 const digest=createHash('sha256').update(await readFile(path)).digest('hex');
 if(digest!=='cf314b50fb958619d410cd51a0e43d10031e41cc6c76797b9605eee6df54583e')throw Error('Native download checksum mismatch');continue;
}
// Only the named, checksummed latest native archive and its reviewed icon asset are additional exceptions.
if(path===join(root,'windows-v0.4.2','downloads','ChatGPTUsageMeter-v0.4.2-Windows.zip')){
 const manifest=(await readFile(join(root,'windows-v0.4.2','downloads','SHA256SUMS.txt'),'utf8')).trim();
 const match=/^([a-f0-9]{64})  ChatGPTUsageMeter-v0\.4\.2-Windows\.zip$/.exec(manifest);
 const digest=createHash('sha256').update(await readFile(path)).digest('hex');
 if(!match||digest!==match[1])throw Error('Latest native download checksum mismatch');continue;
}
if(path===join(root,'windows-v0.4.2','src','App.ico')){
 const digest=createHash('sha256').update(await readFile(path)).digest('hex');
 if(digest!=='e1204e4c3d9426a7b22c99105f3222712c3f75fffdf978094f0fce5f42d4797f')throw Error('Reviewed application icon checksum mismatch');continue;
}
const text=await readFile(path,'utf8');if(/(?:[A-Z]:[\\/]Users[\\/]|sk-[a-zA-Z0-9]{16,}|eyJ[A-Za-z0-9_-]{30,}\.|libfile_[a-z0-9]{20,}|access_token\s*[:=]\s*["'][^"']{12,})/.test(text))throw Error('Sensitive-looking content detected in '+entry.name);if(/\.(exe|png|zip|log|jsonl)$/i.test(entry.name))throw Error('Unexpected binary or snapshot');}}
await audit(root);await mkdir(join(root,'dist'),{recursive:true});await cp(plugin,join(root,'dist','codex-usage'),{recursive:true,force:true});await audit(join(root,'dist','codex-usage'));console.log('Built source-only plugin in dist/codex-usage; source/package credential/path/snapshot scans and reviewed Windows download checksums passed. No installation or publishing performed.');
