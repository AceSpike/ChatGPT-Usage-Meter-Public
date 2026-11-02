import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {spawnSync} from 'node:child_process';
import vm from 'node:vm';

const html=await readFile(new URL('../plugins/codex-usage/ui/index.html',import.meta.url),'utf8');
const script=html.match(/<script type="module">([\s\S]*?)<\/script>/)[1];
const LANGUAGE_KEY='codex-usage.language';
const now=Date.parse('2030-01-01T00:00:00.000Z');
class FixedDate extends Date{
  constructor(...args){super(...(args.length?args:[now]));}
  static now(){return now;}
}
const fresh=()=>({status:'fresh',fetchedAt:new Date(now).toISOString(),primary:{remainingPercent:70,resetsAt:now/1000+7200,windowMinutes:300},secondary:{remainingPercent:20,resetsAt:now/1000+86400,windowMinutes:10080}});

async function loadUI({saved,storage,blockRead=false,blockWrite=false,callTool,withoutBridge=false}={}){
  const values=storage||new Map(saved===undefined?[]:[[LANGUAGE_KEY,saved]]),writes=[];
  const elements=new Map(),listeners=new Map(),classes=new Set(),intervals=[];
  const element=id=>{
    if(!elements.has(id))elements.set(id,{textContent:'',value:'',disabled:false,hidden:false,attributes:{},listeners:new Map(),
      addEventListener(name,fn){this.listeners.set(name,fn);},setAttribute(name,value){this.attributes[name]=value;}});
    return elements.get(id);
  };
  const win={addEventListener:(name,fn)=>listeners.set(name,fn),localStorage:{
    getItem(key){if(blockRead)throw Error('blocked');return values.get(key)??null;},
    setItem(key,value){if(blockWrite)throw Error('blocked');writes.push([key,value]);values.set(key,value);}
  }};
  if(callTool)win.openai={callTool};
  const parent=withoutBridge?{postMessage(){}}:win;
  const document={getElementById:element,documentElement:{lang:'zh-CN'},title:'',body:{classList:{toggle(name,on){on?classes.add(name):classes.delete(name);}}},visibilityState:'hidden'};
  const timeout=withoutBridge?(fn)=>{queueMicrotask(fn);return 0;}:setTimeout;
  await vm.runInNewContext('(async()=>{'+script+'})()',{window:win,parent,document,Date:FixedDate,setInterval:(fn,ms)=>{intervals.push({fn,ms});return 0;},setTimeout:timeout,clearTimeout,console});
  const receive=(data,source=parent)=>listeners.get('message')({source,data:{jsonrpc:'2.0',method:'ui/notifications/tool-result',params:data}});
  const select=value=>{element('language').value=value;element('language').listeners.get('change')();};
  return {element,document,values,writes,classes,intervals,receive,select,refresh:()=>element('refresh').listeners.get('click')()};
}

test('UI syntax and honest standalone state; ignore foreign messages without storing quota',async()=>{
  const syntax=spawnSync(process.execPath,['--check','--input-type=module'],{input:script,encoding:'utf8'});
  assert.equal(syntax.status,0,syntax.stderr);
  const ui=await loadUI();
  assert.match(ui.element('state').textContent,/未连接账户/);
  assert.equal(ui.element('remaining').textContent,'—');
  ui.receive(fresh(),{});
  assert.equal(ui.element('remaining').textContent,'—');
  assert.deepEqual(ui.writes,[]);
  assert.ok(!/document\.cookie|fetch\(/.test(script));
});

test('Chinese and English switch all labels, accessibility, state and dates without changing quota',async()=>{
  let calls=0;const ui=await loadUI({callTool:async()=>{calls++;return fresh();}});
  ui.receive(fresh());
  assert.equal(ui.element('remaining').textContent,'70%');
  const initialReset=ui.element('reset').textContent;
  ui.select('en');
  assert.equal(ui.document.documentElement.lang,'en');
  assert.equal(ui.document.title,'Codex quota');
  assert.equal(ui.element('title').textContent,'Codex quota');
  assert.equal(ui.element('refresh').textContent,'Refresh');
  assert.equal(ui.element('remaining-label').textContent,'Remaining quota');
  assert.equal(ui.element('hours-label').textContent,'Hours until reset');
  assert.equal(ui.element('language').attributes['aria-label'],'Interface language');
  assert.equal(ui.element('remaining-metric').attributes['aria-label'],'Remaining quota');
  assert.equal(ui.element('hours-metric').attributes['aria-label'],'Hours until reset');
  assert.equal(ui.element('duration').textContent,'· 5h window');
  assert.equal(ui.element('secondary').textContent,'Secondary window: 20%');
  assert.match(ui.element('reset').textContent,/^Reset time: .*\(local time zone\)$/);
  assert.notEqual(ui.element('reset').textContent,initialReset);
  assert.match(ui.element('stamp').textContent,/^Fetched at: .*2030-01-01T00:00:00.000Z$/);
  assert.match(ui.element('state').textContent,/Connected to the official quota service/);
  assert.match(ui.element('source').textContent,/queried every 60 seconds/);
  assert.match(ui.element('disclaimer').textContent,/Not ordinary ChatGPT/);
  assert.equal(ui.element('remaining').textContent,'70%');assert.equal(ui.element('hours').textContent,'2.0');
  ui.select('zh-CN');ui.select('en');
  assert.equal(calls,0,'language changes must not query the account');
  assert.equal(ui.element('remaining').textContent,'70%');
  assert.deepEqual(ui.writes,[[LANGUAGE_KEY,'en'],[LANGUAGE_KEY,'zh-CN'],[LANGUAGE_KEY,'en']]);
  assert.deepEqual(ui.intervals.map(i=>i.ms),[60000,1000]);
});

test('language-only preference survives reload; unsupported or hostile values fall back to Chinese',async()=>{
  const storage=new Map();const ui=await loadUI({storage});ui.select('en');
  const reloaded=await loadUI({storage});
  assert.equal(reloaded.element('language').value,'en');
  assert.match(reloaded.element('state').textContent,/No account is connected/);
  assert.deepEqual([...storage],[[LANGUAGE_KEY,'en']]);
  for(const saved of ['fr','__proto__','<script>','']){
    const invalid=await loadUI({saved});assert.equal(invalid.document.documentElement.lang,'zh-CN');
    invalid.select('constructor');assert.equal(invalid.document.documentElement.lang,'zh-CN');
    assert.deepEqual(invalid.writes,[]);
  }
});

test('blocked storage still switches in memory and explains the persistence limit',async()=>{
  const ui=await loadUI({blockRead:true,blockWrite:true});
  ui.select('en');
  assert.equal(ui.document.documentElement.lang,'en');
  assert.equal(ui.element('language-note').hidden,false);
  assert.match(ui.element('language-note').textContent,/cannot save the language preference/);
  assert.deepEqual(ui.writes,[]);
  assert.equal(ui.element('remaining').textContent,'—');
  const writeBlocked=await loadUI({saved:'en',blockWrite:true});writeBlocked.select('zh-CN');
  assert.match(writeBlocked.element('language-note').textContent,/仅在本次打开期间有效/);
});

test('missing, expired, cached and unavailable values remain honest in both languages',async()=>{
  const ui=await loadUI();
  for(const language of ['en','zh-CN']){
    ui.select(language);const english=language==='en';
    const expired=fresh();expired.primary.resetsAt=now/1000;ui.receive(expired);
    assert.equal(ui.element('remaining').textContent,english?'Awaiting refresh':'待刷新');
    assert.equal(ui.element('hours').textContent,english?'Awaiting refresh':'待刷新');
    assert.match(ui.element('state').textContent,english?/not assuming 100%/:/不推算为100%/);
    ui.receive({...fresh(),status:'cached'});
    assert.ok(ui.classes.has('cached'));assert.equal(ui.element('remaining').textContent,'70%');
    assert.match(ui.element('state').textContent,english?/showing cached data/:/当前为缓存/);
    ui.receive({status:'unavailable',fetchedAt:null,primary:null,secondary:null});
    assert.equal(ui.element('remaining').textContent,english?'Not provided':'未提供');
    assert.equal(ui.element('hours').textContent,english?'Unknown':'未知');
    assert.match(ui.element('state').textContent,english?/Quota unavailable/:/额度不可用/);
    assert.ok(!ui.classes.has('cached'));
  }
});

test('query errors, host errors and recovery use selected language; repeated refresh is coalesced',async()=>{
  const noData=await loadUI({saved:'en'});await noData.refresh();
  assert.match(noData.element('state').textContent,/Host connection or query failed/);
  noData.select('zh-CN');assert.match(noData.element('state').textContent,/宿主连接或查询失败/);
  const noBridge=await loadUI({saved:'en',withoutBridge:true});
  assert.match(noBridge.element('state').textContent,/does not provide an MCP Apps bridge/);
  noBridge.select('zh-CN');assert.match(noBridge.element('state').textContent,/未提供 MCP Apps 桥接/);
  let fail=true,resolveRequest,calls=0;
  const ui=await loadUI({saved:'en',callTool:()=>{calls++;return fail?Promise.reject(Error('synthetic failure')):new Promise(resolve=>{resolveRequest=resolve;});}});
  ui.receive(fresh());await ui.refresh();assert.ok(ui.classes.has('cached'));
  assert.equal(ui.element('remaining').textContent,'70%');
  fail=false;const first=ui.refresh();const second=ui.refresh();
  ui.select('zh-CN');ui.select('en');
  assert.equal(ui.element('refresh').disabled,true);assert.equal(calls,2);
  resolveRequest(fresh());await Promise.all([first,second]);
  assert.equal(ui.element('refresh').disabled,false);assert.ok(!ui.classes.has('cached'));
  assert.match(ui.element('state').textContent,/Connected to the official quota service/);
});
