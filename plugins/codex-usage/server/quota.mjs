export function normalize(result) {
  const buckets=result?.rateLimitsByLimitId;
  const b=buckets && typeof buckets==='object'?buckets.codex:result?.rateLimits;
  if(!b || (b.limitId && b.limitId!=='codex'))throw new Error('NO_QUOTA');
  const window=w=>{
    if(!w || typeof w.usedPercent!=='number' || !Number.isFinite(w.usedPercent) || w.usedPercent<0 || w.usedPercent>100)return null;
    const reset=Number.isSafeInteger(w.resetsAt) && Number.isFinite(new Date(w.resetsAt*1000).getTime())?w.resetsAt:null;
    return {usedPercent:w.usedPercent,windowMinutes:Number.isFinite(w.windowDurationMins)&&w.windowDurationMins>0?w.windowDurationMins:null,resetsAt:reset};
  };
  const data={primary:window(b.primary),secondary:window(b.secondary)};
  if(!data.primary&&!data.secondary)throw new Error('NO_QUOTA');return data;
}
export function present(window,now) {
  if(!window)return null;
  const hours=window.resetsAt===null?null:Math.max(0,(window.resetsAt*1000-now)/3600000);
  const expired=hours===0;
  return {...window,remainingPercent:expired?null:100-window.usedPercent,hoursUntilReset:hours,expired};
}
export class QuotaService {
  constructor(read,{now=()=>Date.now()}={}){this.read=read;this.now=now;this.cached=null;this.failed=false;this.lastAttempt=-Infinity;this.pending=null;}
  async get(){
    const now=this.now();
    if(!this.pending && (now-this.lastAttempt>=10000) && (!this.cached||this.failed||now-this.cached.at>=60000)){
      this.lastAttempt=now;
      this.pending=(async()=>{try {this.cached={at:this.now(),data:normalize(await this.read())};this.cached.at=this.now();this.failed=false;}catch {this.failed=true;}finally {this.pending=null;}})();
    }
    if(this.pending)await this.pending;
    const time=this.now(),s=this.cached;
    return {source:'Codex CLI account/rateLimits/read',accountScope:'local-cli-sign-in',status:!s?'unavailable':this.failed||time-s.at>120000?'cached':'fresh',fetchedAt:s?new Date(s.at).toISOString():null,checkedAt:new Date(time).toISOString(),pollSeconds:60,primary:present(s?.data.primary,time),secondary:present(s?.data.secondary,time),error:this.failed?'Quota unavailable; check the local Codex CLI sign-in and connection.':null};
  }
}
