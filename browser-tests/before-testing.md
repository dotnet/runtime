# Before Testing Setup

One-time setup steps required before running Browser/WASM CoreCLR tests.

## HTTPS Developer Certificate

The xharness test runner starts a local HTTPS server. You need to generate a developer certificate:

```bash
./dotnet.sh dev-certs https
./dotnet.sh dev-certs https --trust  # May show warnings on Linux, that's OK
```

## Initial Build

Build the runtime for Browser/WASM with CoreCLR:

```bash
export RuntimeFlavor="CoreCLR"
export Scenario="WasmTestOnChrome"
export InstallFirefoxForTests="false"
export XunitShowProgress="true"
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"
./build.sh -os browser -subset clr+libs+host -c Release
```

**Note:** This build can take 30-40+ minutes.
