import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';

const root = new URL('../', import.meta.url);
const readJson = async path => JSON.parse(await readFile(new URL(path, root), 'utf8'));

test('local marketplace catalog is packaged and resolves to matching plugin/MCP manifests', async () => {
  const catalog = await readJson('.agents/plugins/marketplace.json');
  assert.equal(catalog.name, 'codex-usage-local');
  assert.equal(catalog.plugins.length, 1);
  const entry = catalog.plugins[0];
  assert.equal(entry.name, 'codex-usage');
  assert.deepEqual(entry.source, {source: 'local', path: './plugins/codex-usage'});
  assert.deepEqual(entry.policy, {installation: 'AVAILABLE', authentication: 'ON_INSTALL'});

  const plugin = await readJson(`${entry.source.path}/plugin.json`);
  const mcp = await readJson(`${entry.source.path}/mcp.json`);
  assert.equal(plugin.name, entry.name);
  assert.equal(mcp.mcpServers[entry.name].type, 'stdio');
  assert.equal(mcp.mcpServers[entry.name].cwd, './');
});

test('private-test version matches package, plugin, MCP and CLI client metadata', async () => {
  const pkg = await readJson('package.json');
  const plugin = await readJson('plugins/codex-usage/plugin.json');
  assert.equal(plugin.version, pkg.version);
  for (const path of ['server/server.mjs', 'server/codex.mjs', 'ui/index.html']) {
    const source = await readFile(new URL(`plugins/codex-usage/${path}`, root), 'utf8');
    const versions = [...source.matchAll(/version:\s*'([^']+)'/g)].map(match => match[1]);
    assert.deepEqual(versions, [pkg.version], path);
  }
});
