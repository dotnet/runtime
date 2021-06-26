# Building

This depends on `emsdk` to be installed.

## emsdk on mac

* You can run `make provision-wasm`, which will install it to `$reporoot/src/mono/wasm/emsdk`.
Note: Irrespective of `$(EMSDK_PATH)`'s value, `provision-wasm` will always install into `$reporoot/src/mono/wasm/emsdk`.

`EMSDK_PATH` is set to `$reporoot/src/mono/wasm/emsdk` by default, by the Makefile.

Note: `EMSDK_PATH` is set by default in `src/mono/wasm/Makefile`, so building targets from that will have it set. But you might need to set it manually if
you are directly using the `dotnet build`, or `build.sh`.

* Alternatively you can install **correct version** yourself from the [Emscripten SDK guide](https://emscripten.org/docs/getting_started/downloads.html).
Do not install `latest` but rather specific version e.g. `./emsdk install 2.0.23`. See [emscripten-version.txt](./emscripten-version.txt)

Make sure to set `EMSDK_PATH` variable, whenever building, or running tests for wasm.

### Windows dependencies

Windows build [requirements](https://github.com/dotnet/runtime/blob/main/docs/workflow/requirements/windows-requirements.md)

If `EMSDK_PATH` is not set, the `emsdk` should be provisioned automatically during the build.

## Building on mac

* To build the whole thing, with libraries:

`make build-all`

* To build just the runtime (useful when doing incremental builds with only runtime changes):

`make runtime`

### Note: Additional msbuild arguments can be passed with: `make build-all MSBUILD_ARGS="/p:a=b"`

### Bulding on windows

* To build everything

`build.cmd -os Browser -subset mono+libs` in the repo top level directory.

# Running tests

## Installation of JavaScript engines

The latest engines can be installed with jsvu (JavaScript engine Version Updater https://github.com/GoogleChromeLabs/jsvu)

### Mac

* Install npm with brew:

`brew install npm`

* Install jsvu with npm:

`npm install jsvu -g`

* Run jsvu and install `v8`, `SpiderMonkey`, or `JavaScriptCore` engines:

`jsvu`

Add `~/.jsvu` to your `PATH`:

`export PATH="${HOME}/.jsvu:${PATH}"`

### Windows

* Install node/npm from https://nodejs.org/en/ and add its npm and nodejs directories to the `PATH` environment variable

* * Install jsvu with npm:

`npm install jsvu -g`

* Run jsvu and install `v8`, `SpiderMonkey`, or `JavaScriptCore` engines:

`jsvu`

* Add `~/.jsvu` to the `PATH` environment variable

## Libraries tests

Library tests can be run with js engines: `v8`, `SpiderMonkey`,or `JavaScriptCore`:

### Mac

* `v8`: `make run-tests-v8-$(lib_name)`
* SpiderMonkey: `make run-tests-sm-$(lib_name)`
* JavaScriptCore: `make run-tests-jsc-$(lib_name)`
* Or default: `make run-tests-$(lib_name)`. This runs the tests with `v8`.

For example, for `System.Collections.Concurrent`: `make run-tests-v8-System.Collections.Concurrent`

### Windows

Library tests on windows can be run as described in [testing-libraries](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/libraries/testing.md#testing-libraries) documentation. Without setting additional properties, it will run tests for all libraries on `v8` engine:

`.\build.cmd libs.tests -test -os browser`

* `JSEngine` property can be used to specify which engine to use. Right now `v8` and `SpiderMonkey` engines can be used.

Examples of running tests for individual libraries:

`.\dotnet.cmd build /t:Test /p:TargetOS=Browser src\libraries\System.Collections.Concurrent\tests`
`.\dotnet.cmd build /t:Test /p:TargetOS=Browser /p:JSEngine="SpiderMonkey" src\libraries\System.Text.Json\tests`

### Browser tests on mac

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

## Debugger tests on mac

Debugger tests need `Google Chrome` to be installed.

`make run-debugger-tests`

To run a test with `FooBar` in the name:

`make run-debugger-tests TEST_FILTER=FooBar`

(See https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=xunit for filter options)

Additional arguments for `dotnet test` can be passed via `MSBUILD_ARGS` or `TEST_ARGS`. For example `MSBUILD_ARGS="/p:WasmDebugLevel=5"`. Though only one of `TEST_ARGS`, or `TEST_FILTER` can be used at a time.

## Run samples

The samples in `src/mono/sample/wasm` can be build and run like this:

* console Hello world sample

`dotnet build /t:RunSample console/Wasm.Console.Sample.csproj`

* browser TestMeaning sample

`dotnet build /t:RunSample browser/Wasm.Browser.Sample.csproj`

To build and run the samples with AOT, add `/p:RunAOTCompilation=true` to the above command lines.

### Upgrading Emscripten

Bumping Emscripten version involves these steps:

* update https://github.com/dotnet/runtime/blob/main/src/mono/wasm/emscripten-version.txt
* bump emscripten versions in docker images in https://github.com/dotnet/dotnet-buildtools-prereqs-docker
* bump emscripten in https://github.com/dotnet/emsdk
* update version number in docs
* update `Microsoft.NET.Runtime.Emscripten.<emscripten version>.Node.win-x64` package name, version and sha hash in https://github.com/dotnet/runtime/blob/main/eng/Version.Details.xml and in https://github.com/dotnet/runtime/blob/main/eng/Versions.props. the sha is the commit hash in https://github.com/dotnet/emsdk and the package version can be found at https://dev.azure.com/dnceng/public/_packaging?_a=feed&feed=dotnet6
* update packages in the workload manifest https://github.com/dotnet/runtime/blob/main/src/mono/nuget/Microsoft.NET.Workload.Mono.Toolchain.Manifest/WorkloadManifest.json.in
