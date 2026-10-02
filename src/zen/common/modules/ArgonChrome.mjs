/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. https://mozilla.org/MPL/2.0/ */
import { parseBang } from "resource:///modules/ArgonBangs.sys.mjs";

// Keep the full URL in Firefox's input and clipboard code. Only the unfocused
// visual label is shortened; origin/security indicators stay native.
function initArgonChrome() {
  const inputBox = gURLBar.inputField.parentElement;
  const domain = document.createElementNS(
    "http://www.w3.org/1999/xhtml",
    "span",
  );
  domain.id = "argon-domain-label";
  domain.setAttribute("aria-hidden", "true");
  inputBox.append(domain);
  const chip = document.createElementNS("http://www.w3.org/1999/xhtml", "span");
  chip.id = "argon-bang-chip";
  chip.hidden = true;
  chip.setAttribute("aria-hidden", "true");
  inputBox.prepend(chip);

  const update = () => {
    const uri = gBrowser.selectedBrowser.currentURI;
    domain.textContent = "";
    gURLBar.removeAttribute("argon-domain-only");
    if (
      ["http", "https"].includes(uri.scheme) &&
      gURLBar.getAttribute("pageproxystate") === "valid"
    ) {
      // Preserve subdomains, punycode and nonstandard ports: hiding them makes
      // visually similar origins difficult to distinguish.
      domain.textContent = `${uri.scheme === "http" ? "http://" : ""}${uri.asciiHostPort}`;
      gURLBar.setAttribute("argon-domain-only", "true");
    }
    const bang = parseBang(gURLBar.inputField.value);
    chip.hidden =
      !bang ||
      !gURLBar.focused ||
      !Services.prefs.getBoolPref("argon.bangs.enabled", true);
    chip.textContent = bang?.name || "";
  };
  const listener = {
    onLocationChange() {
      update();
    },
    QueryInterface: ChromeUtils.generateQI([
      "nsIWebProgressListener",
      "nsISupportsWeakReference",
    ]),
  };
  gBrowser.addProgressListener(listener);
  gURLBar.inputField.addEventListener("input", update);
  gURLBar.inputField.addEventListener("focus", update);
  gURLBar.inputField.addEventListener("blur", update);
  gBrowser.tabContainer.addEventListener("TabSelect", update);
  const observer = new MutationObserver(update);
  observer.observe(gURLBar, {
    attributes: true,
    attributeFilter: ["pageproxystate", "focused"],
  });
  if (Services.prefs.getBoolPref("argon.hosted-workspace", false)) {
    document.documentElement.setAttribute("argon-hosted-workspace", "true");
    // Argon's profile switcher is the only workspace creation entry point in
    // hosted mode. Zen tab-space creation would share this profile's logins.
    document
      .getElementById("cmd_zenOpenWorkspaceCreation")
      ?.setAttribute("disabled", "true");
  }
  update();
  window.addEventListener(
    "unload",
    () => {
      observer.disconnect();
      gBrowser.removeProgressListener(listener);
      gURLBar.inputField.removeEventListener("input", update);
      gURLBar.inputField.removeEventListener("focus", update);
      gURLBar.inputField.removeEventListener("blur", update);
      gBrowser.tabContainer.removeEventListener("TabSelect", update);
    },
    { once: true },
  );
}

gZenStartup.promiseInitialized.then(initArgonChrome).catch(console.error);
