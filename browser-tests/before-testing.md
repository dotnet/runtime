# Before Testing Setup

One-time setup steps required before running Browser/WASM CoreCLR tests.

## HTTPS Developer Certificate

The xharness test runner starts a local HTTPS server. You need to generate a developer certificate:

```bash
dotnet dev-certs https
dotnet dev-certs https --trust  # May show warnings on Linux, that's OK
```

## Initial Build

Build the runtime for Browser/WASM with CoreCLR:

```bash
./build.sh -os browser -subset clr+libs+host -c Debug
```

**Note:** This build can take 30-40+ minutes.

## Verify Setup

After the build completes, verify the setup by running a simple test:

```bash
./browser-tests/run-browser-test.sh src/libraries/System.Runtime.InteropServices.JavaScript/tests/System.Runtime.InteropServices.JavaScript.UnitTests/System.Runtime.InteropServices.JavaScript.Tests.csproj
```

## Environment Setup

The `run-browser-test.sh` script sets these automatically, but for manual runs:

```bash
export RuntimeFlavor="CoreCLR"
export Scenario="WasmTestOnChrome"
export InstallFirefoxForTests="false"
export XunitShowProgress="true"
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"
```

If this succeeds, you're ready to run other test suites.

