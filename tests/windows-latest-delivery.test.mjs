import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {inflateRawSync} from 'node:zlib';
const digest=b=>createHash('sha256').update(b).digest('hex');
test('latest skin release ships exactly one versioned EXE with matching regenerated checksums',async()=>{
 const base=new URL('../windows-v0.4.2/downloads/',import.meta.url);
 const zip=await readFile(new URL('ChatGPTUsageMeter-v0.4.2-Windows.zip',base));
 const manifest=(await readFile(new URL('SHA256SUMS.txt',base),'utf8')).trim();
 assert.match(manifest,/^[a-f0-9]{64}  ChatGPTUsageMeter-v0\.4\.2-Windows\.zip$/);
 assert.equal(digest(zip),manifest.slice(0,64));
 const end=zip.lastIndexOf(Buffer.from([0x50,0x4b,0x05,0x06]));assert.ok(end>=0);
 const count=zip.readUInt16LE(end+10),files=new Map();let offset=zip.readUInt32LE(end+16);
 for(let n=0;n<count;n++){
  assert.equal(zip.readUInt32LE(offset),0x02014b50);
  const method=zip.readUInt16LE(offset+10),packed=zip.readUInt32LE(offset+20),size=zip.readUInt32LE(offset+24),nameLength=zip.readUInt16LE(offset+28),extra=zip.readUInt16LE(offset+30),comment=zip.readUInt16LE(offset+32),local=zip.readUInt32LE(offset+42);
  const name=zip.toString('utf8',offset+46,offset+46+nameLength);assert.ok(!files.has(name));assert.ok(size<512*1024);
  assert.equal(zip.readUInt32LE(local),0x04034b50);
  const start=local+30+zip.readUInt16LE(local+26)+zip.readUInt16LE(local+28),data=zip.subarray(start,start+packed);
  const bytes=method===8?inflateRawSync(data,{maxOutputLength:512*1024}):method===0?data:null;assert.ok(bytes);assert.equal(bytes.length,size);files.set(name,bytes);offset+=46+nameLength+extra+comment;
 }
 assert.deepEqual([...files.keys()].sort(),['ChatGPTUsageMeter.exe','LICENSE','README.txt','SHA256SUMS.txt']);
 assert.equal(files.get('LICENSE').toString('utf8'),await readFile(new URL('../LICENSE',import.meta.url),'utf8'));
 const exe=files.get('ChatGPTUsageMeter.exe');assert.equal(exe.toString('ascii',0,2),'MZ');assert.ok(exe.includes(Buffer.from('0.4.2.0\0','utf16le')));
 const expected=(await readFile(new URL('EXE-SHA256SUMS.txt',base),'utf8')).trim();assert.match(expected,/^[a-f0-9]{64}  ChatGPTUsageMeter\.exe$/);
 assert.equal(digest(exe),expected.slice(0,64));assert.equal(files.get('SHA256SUMS.txt').toString('utf8').trim(),expected);
 const readme=files.get('README.txt').toString('utf8');assert.match(readme,/0\.4\.2/);assert.match(readme,/desktop/i);assert.match(readme,/npm/i);
});
test('native application icon has explicit small and large Windows resolutions',async()=>{
 const icon=await readFile(new URL('../windows-v0.4.2/src/App.ico',import.meta.url));assert.equal(icon.readUInt16LE(0),0);assert.equal(icon.readUInt16LE(2),1);
 const sizes=[];for(let i=0;i<icon.readUInt16LE(4);i++)sizes.push(icon[6+i*16]||256);
 assert.deepEqual(sizes.sort((a,b)=>a-b),[16,20,24,32,48,64,128,256]);
});
