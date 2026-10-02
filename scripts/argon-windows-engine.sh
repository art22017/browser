#!/usr/bin/env bash
# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. https://mozilla.org/MPL/2.0/
set -euo pipefail
export SURFER_PLATFORM=win32 SURFER_COMPAT=x86_64 ZEN_CROSS_COMPILING=1
export ZEN_RELEASE=1 ZEN_GA_DISABLE_PGO=1 ZEN_DISABLE_LTO=1
export CARGO_INCREMENTAL=0 MOZ_AUTOMATION=1
export PATH="$HOME/.cargo/bin:$PATH"

npm run surfer -- ci --brand release --display-version 0.1a1
npm run download
# Surfer creates an unborn Git branch for source archives. Firefox bootstrap
# queries HEAD's timestamp, so seed metadata without staging gigabytes of source.
if ! git -C engine rev-parse --verify HEAD >/dev/null 2>&1; then
  git -C engine -c user.name='Argon CI' -c user.email='argon-ci@users.noreply.github.com' commit --quiet --allow-empty -m 'Firefox source archive baseline'
fi

mkdir -p "$HOME/win-cross"
curl --fail --location --retry 3 \
  https://firefox-ci-tc.services.mozilla.com/api/index/v1/task/gecko.cache.level-3.toolchains.v3.linux64-wine.latest/artifacts/public/build/wine.tar.zst \
  -o wine.tar.zst
tar --zstd -xf wine.tar.zst -C "$HOME/win-cross"
rm wine.tar.zst
(cd engine && ./mach python --virtualenv build taskcluster/scripts/misc/get_vs.py build/vs/vs2026.yaml "$HOME/win-cross/vs2026")
chmod -R +x "$HOME/win-cross/vs2026"

rustup toolchain install "$(cat .rust-toolchain)" --profile minimal
rustup default "$(cat .rust-toolchain)"
rustup target add x86_64-pc-windows-msvc
# Same windows-rs extraction required by the upstream cross-build.
crate_version=$(cat build/windows/.windows-rs-version)
curl --fail --location --retry 3 https://static.crates.io/crates/cargo-download/cargo-download-0.1.2.crate | tar xz -C /tmp
(cd /tmp/cargo-download-0.1.2 && cargo generate-lockfile && cargo update tinyvec --precise 1.12.0)
RUSTFLAGS="--cap-lints=allow" cargo install --path /tmp/cargo-download-0.1.2 --locked
(cd engine && cargo download -x "windows=$crate_version")
printf '\nexport MOZ_WINDOWS_RS_DIR=%s/engine/windows-%s\n' "$PWD" "$crate_version" >> configs/common/mozconfig
# Keep memory within standard hosted runner limits, and avoid remote-services
# dump updates in a fork's build: use the checked-in import data.
printf '\nmk_add_options MOZ_MAKE_FLAGS="-j2"\n' >> configs/common/mozconfig
npm run ffprefs
npm run surfer -- import --verbose
(cd engine && ./mach --no-interactive bootstrap --application-choice browser)
clang_root=$(find "$HOME/.mozbuild/clang/lib/clang" -mindepth 1 -maxdepth 1 -type d | head -n 1)
printf '\nexport LIB="%s/lib/windows"\n' "$clang_root" >> configs/common/mozconfig
npm run build
test -f engine/obj-x86_64-pc-windows-msvc/dist/bin/argon-engine.exe
# A portable engine distribution is sufficient; Argon's installer packages the
# outer shell. Do not generate a Zen/Firefox engine-only installer or MAR.
(cd engine && ./mach package)
mkdir -p dist
package=$(find engine/obj-x86_64-pc-windows-msvc/dist -maxdepth 1 -name '*.win64.zip' -print -quit)
test -n "$package"
cp "$package" dist/argon-gecko-windows-x64.zip
