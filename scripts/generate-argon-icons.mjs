// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. https://mozilla.org/MPL/2.0/
import resvg from "@resvg/resvg-js";
import pngToIco from "png-to-ico";
import fs from "node:fs/promises";

// Original vector artwork; no Helium or Arc assets are redistributed.
const icon = `<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 256 256"><rect x="8" y="8" width="240" height="240" rx="62" fill="#202125"/><g fill="none" stroke="#c8d3ff" stroke-width="9"><ellipse cx="128" cy="128" rx="82" ry="35" transform="rotate(-35 128 128)"/><ellipse cx="128" cy="128" rx="82" ry="35" transform="rotate(35 128 128)"/><ellipse cx="128" cy="128" rx="82" ry="35" transform="rotate(90 128 128)"/></g><circle cx="128" cy="128" r="15" fill="#91a6ff"/></svg>`;
for (const brand of ["release", "twilight"]) {
  const base = `configs/branding/${brand}`;
  await fs.writeFile(`${base}/content/about-logo.svg`, icon);
  const sizes = [16, 22, 24, 32, 48, 64, 128, 256, 512, 1024];
  const images = new Map(
    sizes.map((size) => [
      size,
      resvg.render(icon, { fitTo: { mode: "width", value: size } }),
    ]),
  );
  for (const [size, buffer] of images)
    await fs.writeFile(`${base}/logo${size}.png`, buffer);
  await fs.writeFile(`${base}/logo.png`, images.get(128));
  await fs.writeFile(`${base}/logo-mac.png`, images.get(512));
  await fs.writeFile(`${base}/content/about-logo.png`, images.get(128));
  await fs.writeFile(`${base}/content/about-logo@2x.png`, images.get(256));
  await fs.writeFile(
    `${base}/firefox.ico`,
    await pngToIco([
      images.get(16),
      images.get(32),
      images.get(48),
      images.get(256),
    ]),
  );
  await fs.writeFile(`${base}/firefox64.ico`, await pngToIco(images.get(64)));
  for (const size of [70, 150]) {
    const buffer = resvg.render(icon, {
      fitTo: { mode: "width", value: size },
    });
    await fs.writeFile(`${base}/VisualElements_${size}.png`, buffer);
  }
  const privateIcon = icon.replace(
    "</svg>",
    '<circle cx="202" cy="202" r="40" fill="#7258bd"/><path d="M176 191q26 12 52 0v18q-26 22-52 0z" fill="#ece9ff"/><path d="M182 200h12m16 0h12" stroke="#7258bd" stroke-width="6" stroke-linecap="round"/></svg>',
  );
  await fs.writeFile(`${base}/content/about-logo-private.svg`, privateIcon);
  for (const [size, suffix] of [
    [128, ""],
    [256, "@2x"],
  ])
    await fs.writeFile(
      `${base}/content/about-logo-private${suffix}.png`,
      resvg.render(privateIcon, { fitTo: { mode: "width", value: size } }),
    );
  for (const size of [70, 150])
    await fs.writeFile(
      `${base}/PrivateBrowsing_${size}.png`,
      resvg.render(privateIcon, { fitTo: { mode: "width", value: size } }),
    );
  await fs.writeFile(
    `${base}/pbmode.ico`,
    await pngToIco(
      [16, 32, 48, 256].map((size) =>
        resvg.render(privateIcon, { fitTo: { mode: "width", value: size } }),
      ),
    ),
  );
  const wordmark = `<svg xmlns="http://www.w3.org/2000/svg" width="240" height="68"><text x="8" y="50" font-family="Segoe UI, Arial, sans-serif" font-weight="600" font-size="48" fill="#91a6ff">Argon</text></svg>`;
  await fs.writeFile(`${base}/content/about-wordmark.svg`, wordmark);
  await fs.writeFile(`${base}/content/firefox-wordmark.svg`, wordmark);
}
