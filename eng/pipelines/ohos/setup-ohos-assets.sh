#!/usr/bin/env bash
# Prepare the OpenHarmony SDK and the target-side ICU/OpenSSL assets for the
# runtime CI leg. The paths come from the job variables in runtime.yml.
set -euo pipefail

: "${OHOS_NDK_HOME:?OHOS_NDK_HOME must be set}"
: "${OHOS_ASSETS:?OHOS_ASSETS must be set}"

OHOS_SDK_URL="${OHOS_SDK_URL:-https://repo.huaweicloud.com/openharmony/os/6.0-Release/ohos-sdk-windows_linux-public.tar.gz}"
OHOS_SDK_SHA="${OHOS_SDK_SHA:-a315834ac133625efc912bd078f3e2b2550868d04aef1b5aa4f9679c8b3c9d8e}"
OPENSSL_VERSION="${OPENSSL_VERSION:-3.3.1}"
OPENSSL_SHA256="${OPENSSL_SHA256:-777cd596284c883375a2a7a11bf5d2786fc5413255efab20c50d6ffe6d020b7e}"
ICU_VERSION="${ICU_VERSION:-75.1}"
ICU_SHA256="${ICU_SHA256:-cb968df3e4d2e87e8b11c49a5d01c787bd13b9545280fc6642f826527618caef}"

ICU_TAG="${ICU_VERSION/./-}"
ICU_FILE="${ICU_VERSION/./_}"

SDK_ROOT="$(dirname "$OHOS_NDK_HOME")"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

for tool in curl python3 make perl cmake ninja; do
    command -v "$tool" >/dev/null || { echo "missing required tool: $tool" >&2; exit 1; }
done

mkdir -p "$SDK_ROOT" "$OHOS_ASSETS"

echo "== SDK =="
curl -fsSL "$OHOS_SDK_URL" -o "$WORK/sdk.tar.gz"
echo "$OHOS_SDK_SHA  $WORK/sdk.tar.gz" | sha256sum -c -
tar -xzf "$WORK/sdk.tar.gz" -C "$SDK_ROOT"
rm -rf "$SDK_ROOT/windows"
cd "$OHOS_NDK_HOME"
for archive in native-linux-x64-*.zip; do
    # python3 -m zipfile drops the symlinks stored in the SDK; recreate them.
    python3 - "$archive" <<'PY'
import os
import stat
import sys
import zipfile

with zipfile.ZipFile(sys.argv[1]) as zf:
    for info in zf.infolist():
        mode = info.external_attr >> 16
        if stat.S_ISLNK(mode):
            target = zf.read(info).decode()
            parent = os.path.dirname(info.filename)
            if parent:
                os.makedirs(parent, exist_ok=True)
            if os.path.lexists(info.filename):
                os.remove(info.filename)
            os.symlink(target, info.filename)
        else:
            zf.extract(info)
PY
done
chmod -R +x native/llvm/bin
chmod -R +x native/build-tools 2>/dev/null || true
rm -f native-linux-x64-*.zip
test -f "$OHOS_NDK_HOME/native/build/cmake/ohos.toolchain.cmake"

NDK_BIN="$OHOS_NDK_HOME/native/llvm/bin"
export PATH="$NDK_BIN:$PATH"
SYSROOT="$OHOS_NDK_HOME/native/sysroot"
TOOLCHAIN="$WORK/toolchain"
mkdir -p "$TOOLCHAIN"
mk() {
    printf '#!/bin/sh\nexec %s "$@"\n' "$2" > "$TOOLCHAIN/aarch64-linux-ohos-$1"
    chmod +x "$TOOLCHAIN/aarch64-linux-ohos-$1"
}
mk gcc "$NDK_BIN/clang --target=aarch64-linux-ohos --sysroot=$SYSROOT"
mk g++ "$NDK_BIN/clang++ --target=aarch64-linux-ohos --sysroot=$SYSROOT"
mk cc "$NDK_BIN/clang --target=aarch64-linux-ohos --sysroot=$SYSROOT"
mk c++ "$NDK_BIN/clang++ --target=aarch64-linux-ohos --sysroot=$SYSROOT"
mk ar "$NDK_BIN/llvm-ar"
mk ranlib "$NDK_BIN/llvm-ranlib"
mk nm "$NDK_BIN/llvm-nm"
mk strip "$NDK_BIN/llvm-strip"
mk objcopy "$NDK_BIN/llvm-objcopy"
mk objdump "$NDK_BIN/llvm-objdump"

echo "== OpenSSL $OPENSSL_VERSION =="
curl -fsSL "https://github.com/openssl/openssl/releases/download/openssl-$OPENSSL_VERSION/openssl-$OPENSSL_VERSION.tar.gz" \
    -o "$WORK/openssl.tar.gz"
echo "$OPENSSL_SHA256  $WORK/openssl.tar.gz" | sha256sum -c -
tar -xzf "$WORK/openssl.tar.gz" -C "$WORK"
cd "$WORK/openssl-$OPENSSL_VERSION"
./Configure linux-aarch64 \
    --cross-compile-prefix="$TOOLCHAIN/aarch64-linux-ohos-" \
    --prefix="$OHOS_ASSETS/openssl" --openssldir="$OHOS_ASSETS/openssl/ssl" --libdir=lib \
    no-shared no-tests no-async no-apps -fPIC
make -j"$(nproc)"
make install_sw

echo "== ICU $ICU_VERSION =="
curl -fsSL "https://github.com/unicode-org/icu/releases/download/release-$ICU_TAG/icu4c-$ICU_FILE-src.tgz" \
    -o "$WORK/icu.tar.gz"
echo "$ICU_SHA256  $WORK/icu.tar.gz" | sha256sum -c -
tar -xzf "$WORK/icu.tar.gz" -C "$WORK"
mkdir -p "$WORK/icu-host" "$WORK/icu-target"

cd "$WORK/icu-host"
"$WORK/icu/source/configure" --disable-tests --disable-samples --disable-extras
make -j"$(nproc)"

cd "$WORK/icu-target"
CC="$TOOLCHAIN/aarch64-linux-ohos-gcc" CXX="$TOOLCHAIN/aarch64-linux-ohos-g++" \
AR="$TOOLCHAIN/aarch64-linux-ohos-ar" RANLIB="$TOOLCHAIN/aarch64-linux-ohos-ranlib" \
"$WORK/icu/source/configure" --host=aarch64-linux-gnu \
    --with-cross-build="$WORK/icu-host" \
    --disable-tests --disable-samples --disable-extras \
    --enable-static --disable-shared --libdir="$OHOS_ASSETS/icu/lib" \
    --prefix="$OHOS_ASSETS/icu"
make -j"$(nproc)"
make install

echo "== Verify =="
test -f "$OHOS_ASSETS/icu/include/unicode/ucurr.h"
test -f "$OHOS_ASSETS/icu/lib/libicuuc.a"
test -f "$OHOS_ASSETS/icu/lib/libicui18n.a"
test -f "$OHOS_ASSETS/icu/lib/libicudata.a"
test -f "$OHOS_ASSETS/openssl/include/openssl/ssl.h"
test -f "$OHOS_ASSETS/openssl/lib/libcrypto.a"
test -f "$OHOS_ASSETS/openssl/lib/libssl.a"
echo "OpenHarmony assets are ready under $OHOS_ASSETS"
