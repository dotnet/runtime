# Building

This depends on `emsdk` to be installed.

## emsdk

* You can either install it yourself (https://emscripten.org/docs/getting_started/downloads.html), and set `EMSDK_PATH` to that. Make sure to have this set whenever building, or running tests for wasm.

* Or you can run `make provision-wasm`, which will install it to `$reporoot/src/mono/wasm/emsdk`.
Note: Irrespective of `$(EMSDK_PATH)`'s value, `provision-wasm` will always install into `$reporoot/src/mono/wasm/emsdk`.

`EMSDK_PATH` is set to `$reporoot/src/mono/wasm/emsdk` by default, by the Makefile.

Note: `EMSDK_PATH` is set by default in `src/mono/wasm/Makefile`, so building targets from that will have it set. But you might need to set it manually if
you are directly using the `dotnet build`, or `build.sh`.

## Building

* To build the whole thing, with libraries:

`make build-all`

* To build just the runtime (useful when doing incremental builds with only runtime changes):

`make runtime`

### Note: Additional msbuild arguments can be passed with: `make build-all MSBUILD_ARGS="/p:a=b"`

# Running tests

## Libraries tests

Library tests can be run with js engines: `v8`, `SpiderMonkey`,or `JavaScriptCore`:

* `v8`: `make run-tests-v8-$(lib_name)`
* SpiderMonkey: `make run-tests-sm-$(lib_name)`
* JavaScriptCore: `make run-tests-jsc-$(lib_name)`
* Or default: `make run-tests-$(lib_name)`. This runs the tests with `v8`.

For example, for `System.Collections.Concurrent`: `make run-tests-v8-System.Collections.Concurrent`

Or they can be run with a browser (Chrome):

`make run-browser-tests-$(lib_name)`

Note: this needs `chromedriver`, and `Google Chrome` to be installed.

For example, for `System.Collections.Concurrent`: `make run-browser-tests-System.Collections.Concurrent`

These tests are run with `xharness wasm test-browser`, for running on the browser. And `xharness wasm test` for others.
The wrapper script used to actually run these tests, accepts:

`$XHARNESS_COMMAND`, which defaults to `test`.
`$XHARNESS_CLI_PATH` (see next section)

### Using a local build of xharness

* set `XHARNESS_CLI_PATH=/path/to/xharness/artifacts/bin/Microsoft.DotNet.XHarness.CLI/Debug/netcoreapp3.1/Microsoft.DotNet.XHarness.CLI.dll`

### Note: Additional msbuild arguments can be passed with: `make ..  MSBUILD_ARGS="/p:a=b"`

## Debugger tests

Debugger tests need `Google Chrome` to be installed.

`make run-debugger-tests`

To run a test with `FooBar` in the name:

`make run-debugger-tests TEST_FILTER=FooBar`

(See https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=xunit for filter options)

Additional arguments for `dotnet test` can be passed via `TEST_ARGS`. Though only one of `TEST_ARGS`, or `TEST_FILTER` can be used at a time.
