import {CodexClient} from './codex.mjs';import {QuotaService} from './quota.mjs';
const client=new CodexClient();try {console.log(JSON.stringify(await new QuotaService(()=>client.read()).get(),null,2));}finally {client.close();}
