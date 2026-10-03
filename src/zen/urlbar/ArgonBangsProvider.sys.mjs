/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. https://mozilla.org/MPL/2.0/ */
import { UrlbarProvider } from "moz-src:///browser/components/urlbar/UrlbarUtils.sys.mjs";
import { UrlbarShared } from "chrome://browser/content/urlbar/UrlbarShared.mjs";
import { UrlbarResult } from "chrome://browser/content/urlbar/UrlbarResult.mjs";
import { parseBang } from "resource:///modules/ArgonBangs.sys.mjs";

export class ArgonBangsProvider extends UrlbarProvider {
  get name() {
    return "ArgonBangsProvider";
  }
  get type() {
    return UrlbarShared.PROVIDER_TYPE.HEURISTIC;
  }
  getPriority() {
    return 100;
  }
  isActive(context) {
    return (
      !context.searchMode &&
      Services.prefs.getBoolPref("argon.bangs.enabled", true) &&
      !!parseBang(context.searchString)
    );
  }
  startQuery(context, addCallback) {
    const bang = parseBang(context.searchString);
    if (!bang) {
      return;
    }
    const result = new UrlbarResult({
      type: UrlbarShared.RESULT_TYPE.URL,
      source: UrlbarShared.RESULT_SOURCE.OTHER_LOCAL,
      payload: {
        url: bang.url,
        title: `${bang.query || "Search"} — ${bang.name}`,
        icon: "chrome://browser/content/zen-styles/argon-search.svg",
      },
    });
    result.heuristic = true;
    addCallback(this, result);
  }
}
