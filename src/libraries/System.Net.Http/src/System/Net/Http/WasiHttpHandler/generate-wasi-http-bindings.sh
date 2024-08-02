#!/bin/sh

set -ex

# This script will regenerate the `wit-bindgen`-generated files in this
# directory.

# Prerequisites:
#   POSIX shell
#   tar
#   [cargo](https://rustup.rs/)
#   [curl](https://curl.se/download.html)

# TODO: Update to the next crates.io release containing fa19e08a884ec62f95191319d8d296874424c736:
cargo install --locked --no-default-features --features csharp --git https://github.com/bytecodealliance/wit-bindgen --rev fa19e08a884ec62f95191319d8d296874424c736 wit-bindgen-cli
curl -OL https://github.com/WebAssembly/wasi-http/archive/refs/tags/v0.2.0.tar.gz
tar xzf v0.2.0.tar.gz
cat >wasi-http-0.2.0/wit/world.wit <<EOF
world wasi-http {
  import outgoing-handler;
}
EOF
wit-bindgen c-sharp -w wasi-http -r native-aot --internal wasi-http-0.2.0/wit
rm -r wasi-http-0.2.0 v0.2.0.tar.gz WasiHttpWorld_wasm_import_linkage_attribute.cs WasiHttpWorld_cabi_realloc.c WasiHttpWorld_component_type.o
