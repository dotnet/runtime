# Build WebAssembly

This document covers building .NET for WebAssembly in the browser. For WebAssembly documentation including testing, debugging, and deployment, see [WebAssembly Documentation](../../../docs/workflow/wasm-documentation.md).

If you haven't already done so, please read [this document](../../../docs/workflow/README.md#Build_Requirements) to understand the build requirements for your operating system. If you are specifically interested in building libraries for WebAssembly, read [Libraries WebAssembly](../../../docs/workflow/building/libraries/webassembly-instructions.md). Emscripten that is needed to build the project will be provisioned automatically into a shared cache under the repository's main checkout (see [wasm tool provisioning](../../../docs/workflow/building/libraries/webassembly-instructions.md#wasm-tool-provisioning)), unless the `EMSDK_PATH` variable is set. (The separate `make provision-wasm` target in this directory is a manual workflow that installs a *hackable* upstream emsdk clone into `src/mono/browser/emsdk`; it is independent of the shared cache and is only needed when patching emscripten itself.)

### Windows

Windows build [requirements](../../../docs/workflow/requirements/windows-requirements.md)

**Note:** The EMSDK has an implicit dependency on Python for it to be initialized. A consequence of this is that if the system doesn't have Python installed prior to attempting a build, the automatic provisioning will fail and be in an invalid state. Therefore, if Python needs to be installed after a build attempt the provisioned emscripten cache entry (see [wasm tool provisioning](../../../docs/workflow/building/libraries/webassembly-instructions.md#wasm-tool-provisioning); by default under `<main checkout>/.dotnet/wasm-tools/emscripten/`) should be manually deleted and then a rebuild attempted.

## Building

At this time no other build dependencies are necessary to start building for WebAssembly. If you haven't already done so, please read [this document](../../../docs/workflow/README.md#Configurations) to understand configurations. Artifacts will be placed in `artifacts/bin/microsoft.netcore.app.runtime.browser-wasm/Release/`.

## macOS

* To build the whole repository, including libraries:

`make build-all`

* To build just the runtime (useful when doing incremental builds with only runtime changes):

`make runtime`

**Note:** Additional msbuild arguments can be passed with: `make build-all MSBUILD_ARGS="/p:a=b"`

## Windows

`build.cmd -os browser -subset mono+libs` in the repo top level directory.

## Linux

* To build the whole repository, including libraries:

`build.sh -os browser -subset mono+libs` in the repo top level directory.


# Running tests

## Experimental CoreCLR dedicated worker hosting

The dedicated-worker prototype requires both build-time and runtime opt-in:

```xml
<PropertyGroup>
  <UseMonoRuntime>false</UseMonoRuntime>
  <WasmEnableSharedMemory>true</WasmEnableSharedMemory>
</PropertyGroup>
```

Start it with `dotnet.withWorker().create()` and serve the app with
`Cross-Origin-Opener-Policy: same-origin` and
`Cross-Origin-Embedder-Policy: require-corp`.

Shared native and ReadyToRun images are staged into the application's intermediate
directory before asset fingerprinting and integrity computation. The runtime pack
is not modified. Ordinary builds without this property retain non-shared memory
and do not require these isolation headers. Changing the property restages the
application assets; do not patch files in the NuGet cache or published output.
Browser ReadyToRun images must be produced by a compiler that includes a maximum
in their memory imports. Older unbounded images fail shared staging explicitly.

The current bridge supports up to six mixed arguments using the native interop
marshalers: Boolean, Byte, Char, Int16, Int32, Int52, BigInt64, Single, Double,
String, IntPtr, DateTime/DateTimeOffset, nullable scalars, arrays, Span/ArraySegment,
Action/Function, Task arguments, JSObject/Object and Exception values.
Results use the same native-supported types or void, synchronously or through a
Task. Native signature restrictions still apply; for example, asynchronous array
values use Task<object>/JSType.Any, not unsupported direct Task<T[]> signatures.
Void exports return undefined; Task-without-result completion resolves
to undefined, while a null Task remains null. Void UI imports ignore their return
values. Discard and DiscardNoWait use the existing ST void behavior. Floating-point
transport preserves NaN, infinities and signed zero; Single follows native
float32 rounding. Null and empty strings remain distinct.
Byte, Char and Int16 use their exact native widths and ranges. Char values are
numeric UTF-16 code units, not JavaScript strings. Int52 follows the existing
`long`/`JSType.Number` converter and carries JavaScript safe integers through a
double-valued marshaler slot. BigInt64 follows `long`/`JSType.BigInt`, uses a
signed 64-bit slot and JavaScript `bigint` values, including Task completions;
it never converts through a JavaScript Number.
The worker owns temporary UTF-16 transport allocations and native marshaling;
UI code only copies transport payloads and accesses live shared memory views.
IntPtr preserves the native unsigned Wasm32 representation. Dates retain native
millisecond precision. Arrays are copied; object, exception and callback values
use bidirectional identity-preserving references with weak proxy caches and
per-transmission leases. Reference operations use the reentrant synchronous
rings; Promise arguments settle through messages. Span lifetime ends with its
import (also on failure); ArraySegment disposal runs on the worker. Memory-view
methods refresh shared memory after growth and reject disposed views.
Thrown import/export failures and Task rejections preserve reference identity.
If an exception payload cannot be allocated, the failure explicitly reports that
exception transport failed and uses the bounded shared-error snapshot.
Shared slots and transport buffers are released
after initial invocation, before asynchronous completion messages.
Synchronous calls block the UI and service interop requests, not arbitrary Promise
continuations. Each direction has 16 active synchronous call slots; asynchronous
calls release their slots after initial invocation. Async UI imports require native
Promises, not custom thenables. Shutdown rejects pending proxy calls but does not
cancel arbitrary user Promises. The full RuntimeAPI remains unsupported;
this prototype still requires API/design review.
Operations on worker-owned reference proxies fail after the worker exits;
objects returned to their original UI owner retain their original identity.

### Library tests in worker mode

The JS interop test project supports an explicit `WasmTestWithWorker` mode.
It registers its UI imports before managed execution and checks that managed
code runs without a DOM while its registered imports execute on the UI thread.
Run the dedicated-worker subset in Chrome:

```powershell
.\dotnet.cmd build -bl /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Debug /p:RuntimeFlavor=CoreCLR /t:Test /p:Scenario=WasmTestOnChrome /p:InstallChromeForTests=false /p:EnableHotReloadInRuntimeConfigDevFile=false /p:WasmTestWithWorker=true /p:XUnitClassName=System.Runtime.InteropServices.JavaScript.Tests.WorkerInteropTest .\src\libraries\System.Runtime.InteropServices.JavaScript\tests\System.Runtime.InteropServices.JavaScript.UnitTests\System.Runtime.InteropServices.JavaScript.Tests.csproj
```

Use `WasmTestWithWorker=false` for the same subset on the ordinary runtime.
The subset includes scalar strings, Unicode and large payloads, native memory
growth, nested calls, concurrent Tasks, dates/nullables, arrays, reference
identity, callbacks, live memory views, errors and cleanup. It is not the full
JS interop suite: the existing helper modules still need worker-aware setup
and full-suite execution.
When using the shared browser cache, set `CHROME_PATH_FOR_TESTS` and put its
matching ChromeDriver on `PATH`; a cached driver cannot drive an unrelated
system Chrome major version.
Hot-reload browser tools are disabled for this test run: they are unrelated to
the interop assertions and recent SDKs otherwise inject conflicting assets into
the two referenced HTTP test servers.

## Installation of JavaScript engines

The latest engines can be installed with jsvu (JavaScript engine Version Updater https://github.com/GoogleChromeLabs/jsvu)

### macOS

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

* Install jsvu with npm:

`npm install jsvu -g`

* Run jsvu and install `v8`, `SpiderMonkey`, or `JavaScriptCore` engines:

`jsvu`

* Add `~/.jsvu` to the `PATH` environment variable

## Libraries tests

Library tests can be run with js engines: `v8`, `SpiderMonkey`,or `JavaScriptCore`:

### macOS

* `v8`: `make run-tests-v8-$(lib_name)`
* SpiderMonkey: `make run-tests-sm-$(lib_name)`
* JavaScriptCore: `make run-tests-jsc-$(lib_name)`
* Or default: `make run-tests-$(lib_name)`. This runs the tests with `v8`.

For example, for `System.Collections.Concurrent`: `make run-tests-v8-System.Collections.Concurrent`

### Windows

Library tests on windows can be run as described in [testing-libraries](../../../docs/workflow/testing/libraries/testing.md#testing-libraries) documentation. Without setting additional properties, it will run tests for all libraries on `v8` engine:

`.\build.cmd libs.tests -test -os browser`

* `JSEngine` property can be used to specify which engine to use. Right now `v8` and `SpiderMonkey` engines can be used.

Examples of running tests for individual libraries:

`.\dotnet.cmd build /t:Test /p:TargetOS=browser src\libraries\System.Collections.Concurrent\tests`
`.\dotnet.cmd build /t:Test /p:TargetOS=browser /p:JSEngine="SpiderMonkey" src\libraries\System.Text.Json\tests`

### Browser tests on macOS

Or they can be run with a browser (Chrome):

`make run-browser-tests-$(lib_name)`

Note: this needs `chromedriver`, and `Google Chrome` to be installed.

For example, for `System.Collections.Concurrent`: `make run-browser-tests-System.Collections.Concurrent`

These tests are run with `xharness wasm test-browser`, for running on the browser. And `xharness wasm test` for others.
The wrapper script used to actually run these tests, accepts:

`$XHARNESS_COMMAND`, which defaults to `test`.
`$XHARNESS_CLI_PATH` (see next section)

### Using a local build of xharness

XHarness consists of two pieces for WASM

#### 1. CLI/host

* set `XHARNESS_CLI_PATH=/path/to/xharness/artifacts/bin/Microsoft.DotNet.XHarness.CLI/Debug/net9.0/Microsoft.DotNet.XHarness.CLI.dll`

**Note:** Additional msbuild arguments can be passed with: `make ..  MSBUILD_ARGS="/p:a=b"`

#### 2. Test runner running inside of the browser

All library tests are hosted by `WasmTestRunner.csproj`. The project references XHarness nuget for running tests using Xunit. To make changes and iterate quickly

- Add property `<RestoreAdditionalProjectSources>$(RestoreAdditionalProjectSources);LOCAL_CLONE_OF_XHARNESS\artifacts\packages\Debug\Shipping</RestoreAdditionalProjectSources>` in `WasmTestRunner.csproj`.
- Set environment variable in your terminal `$env:NUGET_PACKAGES="$pwd\.nuget"` (so that nuget packages are restored to local folder `.nuget`)
- Run "Pack" in the XHarness solution in Visual Studio on `Microsoft.DotNet.XHarness.TestRunners.Common` or `Microsoft.DotNet.XHarness.TestRunners.Xunit` based on your changes (it will generate a nuget package in `LOCAL_CLONE_OF_XHARNESS\artifacts\packages\Debug\Shipping`).
- Build WasmTestRunner `.\dotnet.cmd build -c Debug .\src\libraries\Common\tests\WasmTestRunner\WasmTestRunner.csproj`.
- If you need to iterate, delete Xunit or Common nuget cache `rm -r .\.nuget\microsoft.dotnet.xharness.testrunners.xunit\` or `rm -r .\.nuget\microsoft.dotnet.xharness.testrunners.common\`.

### Symbolicating traces

Exceptions thrown after the runtime starts get symbolicating from js itself. Exceptions before that, like asserts containing native traces get symbolicated by xharness using `src/mono/wasm/symbolicator`.

If you need to symbolicate some traces manually, then you need the corresponding `dotnet.native.js.symbols` file. Then:

```console
src/mono/wasm/symbolicator$ dotnet run /path/to/dotnet.native.js.symbols /path/to/file/with/traces
```

When not relinking, or not building with AOT, you can find `dotnet.native.js.symbols` in the runtime pack.

## Debugger tests on macOS

Debugger tests need `Google Chrome` to be installed.

`make run-debugger-tests`

To run a test with `FooBar` in the name:

`make run-debugger-tests TEST_FILTER=FooBar`

(See https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests?pivots=xunit for filter options)

Additional arguments for `dotnet test` can be passed via `MSBUILD_ARGS` or `TEST_ARGS`. For example `MSBUILD_ARGS="/p:WasmDebugLevel=5"`. Though only one of `TEST_ARGS`, or `TEST_FILTER` can be used at a time.

Chrome can be installed for testing by setting `InstallChromeForDebuggerTests=true` when building the tests.

## Run samples

The samples in `src/mono/sample/wasm` can be build and run like this:

* console Hello world sample

`dotnet build /t:RunSample console-v8/Wasm.Console.V8.Sample.csproj`

* browser TestMeaning sample

`dotnet build /t:RunSample browser/Wasm.Browser.Sample.csproj`

To build and run the samples with AOT, add `/p:RunAOTCompilation=true` to the above command lines.

* bench sample

Also check [bench](../sample/wasm/browser-bench/README.md) sample to measure mono/wasm runtime performance.

## Wasm App Host

[Use dotnet run to run wasm applications](host/README.md)

## Templates

The wasm templates, located in the `templates` directory, are templates for `dotnet new`, VS and VS for Mac. They are packaged and distributed as part of the `wasm-experimental` workload. The remaining template is `wasmbrowser` for browser WebAssembly applications.

For details about using `dotnet new` see the dotnet tool [documentation](https://learn.microsoft.com/dotnet/core/tools/dotnet-new).

To test changes in the template, use `dotnet new install --force src/mono/wasm/templates/templates/browser`.

## ES6 modules

JavaScript part of the .NET runtime for WebAssembly is composed of several ES6 modules.

#### dotnet.js

`dotnet.js` is entry point ("loader") containing public API for interacting with .NET runtime. This is the file that you import into your JavaScript code. This file don't change during app development. Adding fingerprint on this file can be turned on by setting msbuild property `WasmFingerprintDotnetJs=true`.

#### dotnet.native.js

`dotnet.native.js` contains emscripten API and is loaded by the loader. This file changes when native build (relink) is invoked. The file name always contains fingerprint to avoid stale cache during app development.

#### dotnet.runtime.js

`dotnet.runtime.js` contains the rest JavaScript part of the .NET runtime. The file name also always contains fingerprint.

## Analyzing binary wasm files

We have few tools to analyze binary wasm files. The [wa-info](https://github.com/radekdoulik/wa-info#wa-info) and [wa-diff](https://github.com/radekdoulik/wa-info#wa-info) to analyze `dotnet.native.wasm` in the `AppBundle` directory, once you build your app. These can be easily [installed](https://github.com/radekdoulik/wa-info#installation) as dotnet tools.

They are handy to quickly disassemble functions and inspect webassembly module sections. The wa-diff is able to compare 2 wasm files, so you can for example check the effect of changes in your source code. You can see changes in the functions code as well as changes in sizes of sections and of code.

There is also the [wa-edit](https://github.com/radekdoulik/wa-info#wa-edit) tool, which is now used to prototype improved warm startup.

## Properties That Trigger Relinking

### What is Relinking?

Relinking is the process of rebuilding the native WebAssembly runtime (`dotnet.native.js`, `dotnet.native.wasm`, etc.) with updated or trimmed content, often resulting in a smaller or more optimized output. This is necessary when certain configuration properties or source changes require regeneration of the native artifacts.

### Properties That Trigger Relinking in `browser.proj`

The following MSBuild properties will trigger a relinking (full rebuild of native artifacts) during the browser/wasm build process:

- **`WasmBuildNative`** - `/p:WasmBuildNative=true` - Forces a fresh build of the native runtime components.
- **`WasmRelinkNative`** - `/p:WasmRelinkNative=true` - Explicitly requests relinking, even if not otherwise required by other property changes.
- **`RunAOTCompilation`** - `/p:RunAOTCompilation=true` - Enables Ahead-of-Time (AOT) compilation. Changing this requires relinking to produce the correct output.
- **`WasmEnableExceptionHandling`** - `/p:WasmEnableExceptionHandling=true` - Changes exception handling mechanisms in the native runtime.
- **`EnableDiagnostics`** - `/p:EnableDiagnostics=true` - Enables or disables diagnostic features in the native runtime.
- **`WasmProfilers`** - `/p:WasmProfilers=...` - Changes profiler configuration in the native runtime.
- **`EmccMaximumHeapSize`** - `/p:EmccMaximumHeapSize=...` - Controls memory layout configuration.
- **`EmccInitialHeapSize`** - `/p:EmccInitialHeapSize=...` - Controls memory layout together with `EmccMaximumHeapSize`. Heap size configuration applies only for browser scenarios.
- **`WasmBuildArgs`** - Any change to arguments passed via `/p:WasmBuildArgs=...` (such as enabling/disabling additional features or options) will trigger a relink.
- **Configuration/Target Architecture** - Changing `/p:Configuration=Debug|Release` or `/p:RuntimeIdentifier=browser-wasm`, etc., can require relinking for correct native output.

#### Notes

- Relinking ensures that the produced WebAssembly binaries reflect the current set of build options and runtime features.
- For incremental developer workflows, minimizing unnecessary relinks speeds up build times.
- The relink process is managed by MSBuild targets in `browser.proj`—refer to that file for the most up-to-date logic.
- Source changes to native code (C/C++/Emscripten sources) in `src/mono/browser` or dependent directories will naturally trigger a relink.

For questions or advanced scenarios, see the [WebAssembly build instructions](../../../docs/workflow/building/libraries/webassembly-instructions.md).

## Upgrading Emscripten

Bumping Emscripten version involves these steps:

* update https://github.com/dotnet/runtime/blob/main/src/mono/browser/emscripten-version.txt
* bump emscripten versions in docker images in https://github.com/dotnet/dotnet-buildtools-prereqs-docker
* bump emscripten in https://github.com/dotnet/emsdk
* bump docker images in https://github.com/dotnet/icu, update emscripten files in eng/patches/
* update version number in docs
* bump `EmsdkVersion` in https://github.com/dotnet/runtime/blob/main/eng/Versions.props
* update the version and sha hash of the `Microsoft.NET.Runtime.Emscripten.Internal` dependency in https://github.com/dotnet/runtime/blob/main/eng/Version.Details.xml. The sha is the commit hash in https://github.com/dotnet/emsdk and the package version can be found at https://dev.azure.com/dnceng/public/_packaging?_a=feed&feed=dotnet6. That package carries no files, it only tracks the version of the emsdk packages, whose own IDs contain the Emscripten version and the RID.
* update packages in the workload manifest https://github.com/dotnet/runtime/blob/main/src/mono/nuget/Microsoft.NET.Workload.Mono.Toolchain.Current.Manifest/WorkloadManifest.json.in

## Upgrading NPM packages

Two things to keep in mind:

1. We use the Azure DevOps NPM registry (configured in `src/mono/browser/runtime/.npmrc`).  When
   updating `package.json`, you will need to be logged in (see instructions for Windows and
   mac/Linux, below) in order for the registry to populate with the correct package versions.
   Otherwise, CI builds will fail.

2. Currently the Emscripten SDK uses NPM version 6 which creates `package-lock.json` files in the
  "v1" format.  When updating NPM packages, it is important to use this older version of NPM (for
  example by using the `emsdk_env.sh` script to set the right environment variables) or by using the
  `--lockfile-format=1` option with more recent versions of NPM.

### Windows

The steps below will download the `vsts-npm-auth` tool from https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet-public-npm/connect/npm

In folder `src\mono\browser\runtime\`

```sh
rm -rf node_modules
rm package-lock.json
npm install -g vsts-npm-auth`
vsts-npm-auth -config .npmrc
npm cache clean --force
npm outdated
npm update --lockfile-version=1
```

### mac/Linux

Go to https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet-public-npm/connect/npm and log in and click on the "Other" tab.
Follow the instructions to set up your `~/.npmrc` with a personal authentication token.

In folder `src/mono/browser/runtime/`

```sh
rm -rf node_modules
rm package-lock.json
npm cache clean --force
npm outdated
npm update --lockfile-version=1
```

## Code style

* Is enforced via [eslint](https://eslint.org/) and rules are in `./.eslintrc.js`
* You could check the style by running `npm run lint` in `src/mono/browser/runtime` directory
* You can install [plugin into your VS Code](https://marketplace.visualstudio.com/items?itemName=dbaeumer.vscode-eslint) to show you the errors as you type

## Builds on CI

* For PRs, tests are generally triggered based on path changes. But if you have a change which would not trigger the relevant builds, then you can run `runtime-wasm` pipeline manually to run all of them. Comment `/azp run runtime-wasm` on the PR.

### How do I know which jobs run on CI, and when?

## PR:

* `only-pc` means `only on relevant path changes`

### Run by default

* `runtime` runs jobs only when relevant paths change. And for `AOT`, only smoke tests are run.

| .                 | runtime                       |
| ----------------- | --------------------          |
| libtests          | linux+windows: all,   only-pc |
| libtests eat      | linux+windows: smoke, only-pc |
| libtests aot      | linux+windows: smoke, only-pc |
| high resource aot | none                          |
| Wasm.Build.Tests  | linux+windows:        only-pc |
| Debugger tests    | linux+windows:        only-pc |
| Runtime tests     | linux+windows:        only-pc |

### Run manually with `/azp run ..`

* `runtime-wasm*` pipelines are triggered manually, and they only run the jobs that would not run on any default pipelines based on path changes.
* The `AOT` jobs run only smoke tests on `runtime`, and on `runtime-wasm*` pipelines all the `AOT` tests are run.

| .                 | runtime-wasm               | runtime-wasm-libtests | runtime-wasm-non-libtests |
| ----------------- | -------------------------- | --------------------  | --------------------      |
| libtests          | linux+windows: all         | linux+windows: all    | none                      |
| libtests eat      | linux:         all         | linux:         all    | none                      |
| libtests aot      | linux+windows: all         | linux+windows: all    | none                      |
| high resource aot | linux+windows: all         | linux+windows: all    | none                      |
| Wasm.Build.Tests  | linux+windows              | none                  | linux+windows             |
| Debugger tests    | linux+windows              | none                  | linux+windows             |
| Runtime tests     | linux                      | none                  | linux                     |
| Multi-thread      | linux: all tests           | linux: all tests      | none                      |

* `runtime-extra-platforms` does not run any wasm jobs on PRs
* `high resource aot` runs a few specific library tests with AOT, that require more memory to AOT.

* `runtime-wasm-dbgtests` runs all the debugger test jobs

## Rolling build (twice a day):

* `runtime` runs all the wasm jobs, but `AOT` still only runs smoke tests.
* `runtime-extra-platforms` also runs by default. And it runs only the cases not covered by `runtime`.

| .                 | runtime                    | runtime-extra-platforms (always run) |
| ----------------- | -------------              | ------------------------------------ |
| libtests          | linux+windows: all         | none                                 |
| libtests eat      | linux: all                 | none                                 |
| libtests aot      | linux+windows: smoke       | linux+windows: all                   |
| high resource aot | none                       | linux+windows: all                   |
|                   |                            |                                      |
| Wasm.Build.Tests  | linux+windows              | none                                 |
| Debugger tests    | linux+windows              | none                                 |
| Runtime tests     | linux                      | none                                 |
| Multi-thread      | linux: build only          | none                                 |

* `high resource aot` runs a few specific library tests with AOT, that require more memory to AOT.


## Test setup on CI

Tests are run with V8, Chrome, node, and wasmtime for the various jobs.

- V8: the version used is from `eng/testing/BrowserVersions.props`. This is used for all the library tests, and WBT, but *not* runtime tests.
- Chrome: Same as V8.
- Node: fixed version from emsdk
- wasmtime - fixed version in `src/mono/wasi/wasmtime-version.txt`.
- wasi-sdk - fixed version in `eng/wasm/wasi-sdk-version.txt`.

All of these are downloaded into the shared wasm tool cache described in [webassembly-instructions.md](../../../docs/workflow/building/libraries/webassembly-instructions.md#wasm-tool-provisioning).

### `eng/testing/BrowserVersions.props`

This file is updated once a week by a github action `.github/workflows/bump-chrome-version.yml`, and the version is obtained by `src/tasks/WasmBuildTasks/GetChromeVersions.cs` task.

# Perf pipeline

- V8 version used to run the microbenchmarks is from `eng/testing/BrowserVersions.props`

TBD

## Updates needed

- when the base OS is upgraded, check if the version of node installed in the `eng/pipelines/coreclr/templates/run-performance-job.yml` needs an upgrade too.
