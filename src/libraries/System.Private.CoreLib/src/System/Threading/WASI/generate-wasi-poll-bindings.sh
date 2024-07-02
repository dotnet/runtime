#!/bin/sh

set -ex

# This script will regenerate the `wit-bindgen`-generated files in this
# directory.

# Prerequisites:
#   POSIX shell
#   tar
#   [cargo](https://rustup.rs/)
#   [curl](https://curl.se/download.html)

# TODO: Use a crates.io release instead of the Git repo once a release
# containing this commit has been created:
cargo install --locked --no-default-features --features csharp-naot \
      --git https://github.com/bytecodealliance/wit-bindgen --rev 266d638f7a9c4535ba5fa1f1bb2e8cc6b5d58667 \
      wit-bindgen-cli
curl -OL https://github.com/WebAssembly/wasi-http/archive/refs/tags/v0.2.0.tar.gz
tar xzf v0.2.0.tar.gz
cat >wasi-http-0.2.0/wit/world.wit <<EOF
world wasi-poll {
  import wasi:io/poll@0.2.0;
}
EOF
wit-bindgen c-sharp -w wasi-poll -r native-aot --internal --skip-support-files wasi-http-0.2.0/wit
rm -r wasi-http-0.2.0 v0.2.0.tar.gz WasiHttpWorld_wasm_import_linkage_attribute.cs
