import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {inflateRawSync} from 'node:zlib';

const digest=buffer=>createHash('sha256').update(buffer).digest('hex');
test('private native download contains the reviewed single EXE and no settings or snapshots',async()=>{
  const base=new URL('../windows/downloads/',import.meta.url);
  const zip=await readFile(new URL('ChatGPTUsageMeter-Floating-v0.3-Windows-Test.zip',base));
  const checksum=(await readFile(new URL('SHA256SUMS.txt',base),'utf8')).trim().split(/\s+/)[0];
  assert.equal(digest(zip),checksum);
  assert.equal(checksum,'cf314b50fb958619d410cd51a0e43d10031e41cc6c76797b9605eee6df54583e');
  const end=zip.lastIndexOf(Buffer.from([0x50,0x4b,0x05,0x06]));assert.ok(end>=0);
  const count=zip.readUInt16LE(end+10),names=[];let offset=zip.readUInt32LE(end+16),exe;
  for(let n=0;n<count;n++){
    assert.equal(zip.readUInt32LE(offset),0x02014b50);
    const method=zip.readUInt16LE(offset+10),packed=zip.readUInt32LE(offset+20),size=zip.readUInt32LE(offset+24);
    const nameLength=zip.readUInt16LE(offset+28),extra=zip.readUInt16LE(offset+30),comment=zip.readUInt16LE(offset+32),local=zip.readUInt32LE(offset+42);
    const name=zip.toString('utf8',offset+46,offset+46+nameLength);names.push(name);assert.ok(size<=128*1024);
    assert.equal(zip.readUInt32LE(local),0x04034b50);
    const start=local+30+zip.readUInt16LE(local+26)+zip.readUInt16LE(local+28),data=zip.subarray(start,start+packed);
    const bytes=method===8?inflateRawSync(data,{maxOutputLength:128*1024}):method===0?data:null;assert.ok(bytes);assert.equal(bytes.length,size);
    if(name==='ChatGPTUsageMeter.exe')exe=bytes;
    offset+=46+nameLength+extra+comment;
  }
  assert.deepEqual(names.sort(),['ChatGPTUsageMeter.exe','README.txt','SHA256SUMS.txt']);
  assert.equal(exe.toString('ascii',0,2),'MZ');
  const exeChecksum=(await readFile(new URL('EXE-SHA256SUMS.txt',base),'utf8')).trim().split(/\s+/)[0];
  assert.equal(digest(exe),exeChecksum);assert.equal(exeChecksum,'a568b5f3f6316dc47db24183fcaee88ff5c060fd7829e240d739a35cd47730c0');
});
