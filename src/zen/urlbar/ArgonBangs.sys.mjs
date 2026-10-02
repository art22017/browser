/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. https://mozilla.org/MPL/2.0/ */

// Authored templates. Resolution never fetches a manifest or a suggestion.
export const BANGS = Object.freeze(
  [
    ["w", "Wikipedia", "https://en.wikipedia.org/w/index.php?search={query}"],
    ["wa", "Wolfram Alpha", "https://www.wolframalpha.com/input?i={query}"],
    ["k", "Kagi", "https://kagi.com/search?q={query}"],
    ["g", "Google", "https://www.google.com/search?q={query}"],
    ["ddg", "DuckDuckGo", "https://duckduckgo.com/?q={query}"],
    ["b", "Bing", "https://www.bing.com/search?q={query}"],
    ["gh", "GitHub", "https://github.com/search?q={query}"],
    ["yt", "YouTube", "https://www.youtube.com/results?search_query={query}"],
    ["r", "Reddit", "https://www.reddit.com/search/?q={query}"],
    ["so", "Stack Overflow", "https://stackoverflow.com/search?q={query}"],
    ["mdn", "MDN", "https://developer.mozilla.org/en-US/search?q={query}"],
    ["npm", "npm", "https://www.npmjs.com/search?q={query}"],
    ["pypi", "PyPI", "https://pypi.org/search/?q={query}"],
    ["crates", "crates.io", "https://crates.io/search?q={query}"],
    ["steam", "Steam", "https://store.steampowered.com/search/?term={query}"],
    ["imdb", "IMDb", "https://www.imdb.com/find/?q={query}"],
    [
      "maps",
      "Google Maps",
      "https://www.google.com/maps/search/?api=1&query={query}",
    ],
    [
      "translate",
      "Google Translate",
      "https://translate.google.com/?sl=auto&tl=en&text={query}&op=translate",
    ],
    [
      "arxiv",
      "arXiv",
      "https://arxiv.org/search/?query={query}&searchtype=all",
    ],
    [
      "scholar",
      "Google Scholar",
      "https://scholar.google.com/scholar?q={query}",
    ],
    ["cgt", "ChatGPT", "https://chatgpt.com/?q={query}"],
    ["perplexity", "Perplexity", "https://www.perplexity.ai/search?q={query}"],
    ["spotify", "Spotify", "https://open.spotify.com/search/{query}"],
    ["a", "Amazon", "https://www.amazon.com/s?k={query}"],
    ["ebay", "eBay", "https://www.ebay.com/sch/i.html?_nkw={query}"],
  ].map(([key, name, template]) => Object.freeze({ key, name, template }))
);

export function parseBang(text) {
  const match = /^!([a-z0-9]+)(?:\s+(.*))?$/is.exec(text.trim());
  if (!match) {
    return null;
  }
  const bang = BANGS.find(entry => entry.key === match[1].toLowerCase());
  if (!bang) {
    return null;
  }
  const query = (match[2] || "").trim();
  return {
    ...bang,
    query,
    url: bang.template.replace("{query}", encodeURIComponent(query)),
  };
}
