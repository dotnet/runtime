# Building

This depends on `emsdk` to be installed.

## emsdk

* You can either install it yourself (https://emscripten.org/docs/getting_started/downloads.html), and set `EMSDK_PATH` to that. Make sure to have this set whenever building, or running tests for wasm.

* Or you can run `make provision-wasm`, which will install it to `$reporoot/src/mono/wasm/emsdk`.

`EMSDK_PATH` is set to `$reporoot/src/mono/wasm/emsdk` by default, by the Makefile.

## Building

* To build the whole thing, with libraries:

`make build-all`

* To build just the runtime (useful when doing incremental builds with only runtime changes):

`make runtime`

# Running tests

## Libraries tests

Library tests can be run with `v8`, `SpiderMonkey`, or `JavaScriptCore`:

* `v8`: `make run-tests-v8-$(lib_name)`
For example, for `System.Collections.Concurrent`: `make run-tests-v8-System.Collections.Concurrent`

* SpiderMonkey: `make run-tests-sm-$(lib_name)`
For example, for `System.Collections.Concurrent`: `make run-tests-sm-System.Collections.Concurrent`

* JavaScriptCore: `make run-tests-jsc-$(lib_name)`
For example, for `System.Collections.Concurrent`: `make run-tests-jsc-System.Collections.Concurrent`

* Or default: `make run-tests-$(lib_name)`. This runs the tests with `v8`.

## Debugger tests

Debugger tests need `Google Chrome` to be installed.

`make run-debugger-tests`

To run a test with `FooBar` in the name:

`make run-debugger-tests TEST_FILTER=FooBar`

(See https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=xunit for filter options)

Additional arguments for `dotnet test` can be passed via `TEST_ARGS`. Though only one of `TEST_ARGS`, or `TEST_FILTER` can be used at a time.
