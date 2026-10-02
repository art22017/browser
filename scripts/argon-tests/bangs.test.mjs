import test from 'node:test';
import assert from 'node:assert/strict';
import { BANGS, parseBang } from '../../src/zen/urlbar/ArgonBangs.sys.mjs';

test('bangs route directly to the selected service and encode only the query', () => {
  assert.equal(parseBang('!gh a&b #c').url, 'https://github.com/search?q=a%26b%20%23c');
  assert.equal(parseBang('  !W 日本語  ').url, 'https://en.wikipedia.org/w/index.php?search=%E6%97%A5%E6%9C%AC%E8%AA%9E');
  assert.equal(parseBang('!cgt what is argon?').name, 'ChatGPT');
  assert.equal(parseBang('!wa sqrt(4)').query, 'sqrt(4)');
});

test('unknown bangs and normal URLs are left to the browser', () => {
  for (const text of ['https://example.com/!gh', 'hello !gh', '!not-a-bang test', '!missing test', '']) {
    assert.equal(parseBang(text), null);
  }
});

test('every bundled target is HTTPS and input cannot change its origin', () => {
  assert.equal(new Set(BANGS.map(b => b.key)).size, BANGS.length);
  for (const bang of BANGS) {
    const target = new URL(parseBang(`!${bang.key} //evil.test/#&q=x`).url);
    assert.equal(target.protocol, 'https:');
    assert.equal(target.origin, new URL(bang.template).origin);
    assert.equal(parseBang(`!${bang.key}`).query, '');
  }
});
