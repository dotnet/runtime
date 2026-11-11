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
  <WasmRunInWorker>true</WasmRunInWorker>
</PropertyGroup>
```

The build emits `runInWorker: true` into the boot configuration. Start it with
`dotnet.create()`; no separate JavaScript worker opt-in is needed. Serve with
`Cross-Origin-Opener-Policy: same-origin` and
`Cross-Origin-Embedder-Policy: require-corp`.

Shared native and ReadyToRun images are staged into the application's intermediate
directory before asset fingerprinting and integrity computation. The runtime pack
is not modified. Ordinary builds with this property set to false retain non-shared memory
and do not require these isolation headers. Changing the property restages the
application assets; do not patch files in the NuGet cache or published output.
For this temporary coverage experiment, `WasmRunInWorker` defaults to true for
Browser/CoreCLR. Set it to false to opt out. Revert the temporary default commit
before shipping; Mono and other targets are unchanged. V8/Node test harnesses
explicitly select ordinary execution, including when a shared-asset archive is
reused by browser and shell CI lanes.
The property controls both worker startup and shared
asset staging for build and publish. `withConfig({ runInWorker: true })` can
select worker startup only when the application already carries shared assets.
`workerOptions.timeoutMs` optionally adjusts startup timeout but does not enable
worker hosting. The former `withWorker()` API is removed.
Browser ReadyToRun images must be produced by a compiler that includes a maximum
in their memory imports. Older unbounded images fail shared staging explicitly.

The bridge reuses the normal UI-side `dotnet.runtime.js` implementation and native
interop signatures: Boolean, Byte, Char, Int16, Int32, Int52, BigInt64, Single, Double,
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
The worker owns native allocation and managed execution. The UI runs the original
marshalers against shared WASM memory; only native operation identifiers, scalar
ABI parameters and frame pointers cross the boundary. `dotnet.native.js` is loaded
only by the worker. There is no separate value serializer or object-reference
protocol, and no six-argument transport restriction.
IntPtr preserves the native unsigned Wasm32 representation. Dates retain native
millisecond precision. Arrays, objects, exceptions, callbacks and Tasks use the
existing native marshalers and the owning realm's handle/Promise registries. Span lifetime
ends with its import (also on failure); ArraySegment disposal uses the normal
managed-proxy lifecycle. Memory views refresh after growth. UI scratch frames
are allocated from shared heap memory, not the VM's native stack.
Native Task completion is dispatched only after the worker leaves the initial
import, so the managed callback is ready. UI thenables are assimilated through
native Promises to prevent inline completion before that boundary.
Synchronous calls block the UI and service interop requests, not arbitrary Promise
continuations. Managed-entry calls have a 16-call budget, with reserved native
capacity for allocation, release and exception handling. Async UI imports use native
Promises/thenables under the normal marshaler contract. Shutdown disposes normal
proxy state and rejects pending managed Task promises, but does not cancel
arbitrary user Promises. Raw Emscripten Module/FS/Wasm-table access is unsupported;
this prototype still requires API/design review.
Managed proxies and exported functions use normal runtime-lifetime checks after
the worker exits. UI JavaScript objects stay in their original realm.

### Library tests in worker mode

Application JavaScript always runs on the UI/main thread. `JSHost.ImportAsync`
loads modules there, global imports bind there, and `JSHost.GlobalThis` and
`JSHost.DotnetInstance` expose UI-owned objects. Unregistered application imports
never fall back to worker-local lookup. HTTP/WebSocket clients execute in the
worker with normal worker-owned marshalers, handles and Promise holders.
Property helpers route to the target object's owning realm.

This branch is a native-boundary architecture proof, not a complete hosting
implementation. Configuration/ready callbacks and application library
initializers execute on UI. Custom boot-resource and download-progress callbacks
also execute on UI, including requests made before native memory is attached.
The existing loader handles URL overrides, default loading and Response promises;
response streams are transferred to the worker without buffering whole WASM
images. Progress combines UI and worker assets. Native JS modules are imported
only in the worker, even when their URLs are selected by a UI callback.
Shutdown aborts loader-owned fetches and cancels late custom response bodies.
`INTERNAL` selects native-host operations: HTTP/WebSocket services, lazy/satellite assembly loading,
diagnostic collection/router operations and native contract/heap access. UI wrappers
forward assembly and diagnostic requests to the native host; networking marshaling
stays worker-local. Without a worker, operations run locally.
Diagnostic trace data returns to UI for downloads, including native/startup
collections. Existing public diagnostic methods remain UI wrappers. Native-host
names retain their existing INTERNAL paths, so ASP.NET Core lazy/culture callers
do not need a namespace update. UI-facing property/global/module helpers and
proxy diagnostics are under `INTERNAL.TARGET`; property access follows object ownership.
Managed imports and direct JS callers use that explicit target namespace.
Diagnostics sessions and collection continue to execute in the native host;
the DOM download is dispatched to UI without moving trace/GC processing there.

The JS interop test project supports an explicit `WasmTestWithWorker` mode.
It registers its UI imports before managed execution and checks UI-backed
module loading, global binding, object access and nested calls from managed code.
Run the dedicated-worker subset in Chrome:

```powershell
.\dotnet.cmd build -bl /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Debug /p:RuntimeFlavor=CoreCLR /t:Test /p:Scenario=WasmTestOnChrome /p:InstallChromeForTests=false /p:EnableHotReloadInRuntimeConfigDevFile=false /p:WasmTestWithWorker=true /p:XUnitClassName=System.Runtime.InteropServices.JavaScript.Tests.WorkerInteropTest .\src\libraries\System.Runtime.InteropServices.JavaScript\tests\System.Runtime.InteropServices.JavaScript.UnitTests\System.Runtime.InteropServices.JavaScript.Tests.csproj
```

Use `WasmTestWithWorker=false` for the same subset on the ordinary runtime.
The subset includes scalar strings, Unicode and large payloads, native memory
growth, nested calls, concurrent Tasks, dates/nullables, arrays, reference
identity, callbacks, live memory views, errors and cleanup.
Remove `XUnitClassName` to run the full default suite, including the original
helper modules and global bindings. Normal OuterLoop/failing-trait exclusions
and platform-specific skips still apply.

The UI event loop runs independently from managed execution on the worker.
`Task.Delay(0)` completes synchronously without yielding managed execution, but
does not prevent UI timers or Promise continuations from running between calls.
Within a single synchronous export, the UI spin loop still services only interop
requests and does not pump arbitrary microtasks.
Native Task completion first acknowledges worker readiness, then commits the
normal UI PromiseHolder and frame. Cancellation can still win before that commit,
without losing or reusing its handle.
When using the shared browser cache, set `CHROME_PATH_FOR_TESTS` and put its
matching ChromeDriver on `PATH`; a cached driver cannot drive an unrelated
system Chrome major version.
Hot-reload browser tools are disabled for this test run: they are unrelated to
the interop assertions and recent SDKs otherwise inject conflicting assets into
the two referenced HTTP test servers.

The WBT `SharedMemoryIsAppOwnedAndOptIn` case covers prebuilt assets with
`TestUsingWorkloads=false`. `SharedMemoryIsAppOwnedAndOptInNativeRelink` runs
with `TestUsingWorkloads=true` and an installed wasm-tools workload, which
provides Emscripten without `EMSDK_PATH`. Both cases verify worker/ordinary/worker
mode transitions. Native relinking is not part of the no-workload test lane.

V8 shell runs the ordinary non-DOM interop tests. Test-module paths are relative
module specifiers, such as `./JavaScriptWorkerTest.mjs`; the shell does not
provide browser APIs such as `URL` or `TextEncoder`.

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
