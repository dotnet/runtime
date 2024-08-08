#!/bin/sh

set -ex

# This script will regenerate the `wit-bindgen`-generated files in this
# directory.

# Prerequisites:
#   POSIX shell
#   tar
#   [cargo](https://rustup.rs/)
#   [curl](https://curl.se/download.html)

cargo install --locked --no-default-features --features csharp --version 0.29.0 wit-bindgen-cli
curl -OL https://github.com/WebAssembly/wasi-http/archive/refs/tags/v0.2.1.tar.gz
tar xzf v0.2.1.tar.gz
cat >wasi-http-0.2.1/wit/world.wit <<EOF
world wasi-http {
  import outgoing-handler;
}
EOF
wit-bindgen c-sharp -w wasi-http -r native-aot --internal wasi-http-0.2.1/wit
rm -r wasi-http-0.2.1 v0.2.1.tar.gz WasiHttpWorld_wasm_import_linkage_attribute.cs WasiHttpWorld_cabi_realloc.c WasiHttpWorld_component_type.o
