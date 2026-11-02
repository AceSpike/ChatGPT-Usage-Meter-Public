import {findCodex} from './codex.mjs';
const result={nodeMajor:Number(process.versions.node.split('.')[0]),platform:process.platform,architecture:process.arch,cliFound:false,pluginHostVerified:false,signInVerified:false};
try{await findCodex();result.cliFound=true;}catch{}
console.log(JSON.stringify(result,null,2));
if(result.nodeMajor!==24||!result.cliFound)process.exitCode=1;
