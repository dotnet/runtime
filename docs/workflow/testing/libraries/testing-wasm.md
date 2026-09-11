# Testing Libraries on WebAssembly

## Prerequisites

### Using JavaScript engines

In order to be able to run tests, the following JavaScript engines should be installed:
- V8
- JavaScriptCore
- SpiderMonkey

They can be installed as a part of [jsvu](https://github.com/GoogleChromeLabs/jsvu).

Please make sure that a JavaScript engine binary is available via command line,
e.g. for V8:
```bash
$ v8
V8 version 8.5.62
```

If you use `jsvu`, first add its location to PATH variable
e.g. for V8

```bash
PATH=/Users/<your_user>/.jsvu/:$PATH V8
```

### Using Browser Instance
It's possible to run tests in a browser instance:

#### Chrome
- An installation of [ChromeDriver - WebDriver for Chrome](https://chromedriver.chromium.org) is required.  Make sure to read [Downloads/Version Selection](https://chromedriver.chromium.org/downloads/version-selection) to setup a working installation of ChromeDriver.
- Include the [ChromeDriver - WebDriver for Chrome](https://chromedriver.chromium.org) location in your PATH environment.  Default is `/Users/<your_user>/.chromedriver`

```bash
PATH=/Users/<your_user>/.chromedriver:$PATH
```

#### Gecko / Firefox

- Requires gecko driver [Github repository of Mozilla](https://github.com/mozilla/geckodriver/releases)
- Include the [Github repository of Mozilla](https://github.com/mozilla/geckodriver/releases) location in your PATH environment.  Default is `/Users/<your_user>/.geckodriver`

```bash
PATH=/Users/<your_user>/.geckodriver:$PATH
```

## Building Libs and Tests for WebAssembly

Now we're ready to build everything for WebAssembly (for more details, please read [this document](../../building/libraries/webassembly-instructions.md#building-everything)):
```bash
./build.sh -os browser -c Release
```
and even run tests one by one for each library:
```
./build.sh libs.tests -test -os browser -c Release
```

### Running individual test suites using JavaScript engine
The following shows how to run tests for a specific library
```
./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Release
```

### Running outer loop tests using JavaScript engine

To run all tests, including "outer loop" tests (which are typically slower and in some test suites less reliable, but which are more comprehensive):
```
./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Release /p:Outerloop=true
```

### Running tests using different JavaScript engines
It's possible to set a JavaScript engine explicitly by adding `/p:JSEngine` property:

```
./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Release /p:JSEngine=SpiderMonkey
```

At the moment supported values are:
- `V8`
- `JavaScriptCore`
- `SpiderMonkey`

By default, `V8` engine is used.

### Running individual test suites using Browser instance

The following shows how to run tests for a specific library

- CLI
    ```
    XHARNESS_COMMAND=test-browser ./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Release
    ```
- Makefile target `run-browser-tests-<test>`
    ```
    make -C src/mono/wasm/ run-browser-tests-System.AppContext
    ```

### Passing arguments to xharness

- `$(WasmXHarnessArgsCli)` - xharness command arguments

    Example: `WasmXHarnessArgsCli="--set-web-server-http-env=DOTNET_TEST_WEBSOCKETHOST"` -> becomes `dotnet xharness wasm test --set-web-server-http-env=DOTNET_TEST_WEBSOCKETHOST`

- `$(WasmXHarnessMonoArgs)` - arguments and variables for mono

    Example: `WasmXHarnessMonoArgs="--runtime-arg=--trace=E --setenv=MONO_LOG_LEVEL=debug"`

- `$(WasmTestAppArgs)` - arguments for the test app itself

### Running outer loop tests using Browser instance

To run all tests, including "outer loop" tests (which are typically slower and in some test suites less reliable, but which are more comprehensive):

- CLI
    ```
    XHARNESS_COMMAND=test-browser ./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Release /p:Outerloop=true
    ```

- Makefile target `run-browser-tests-<test>`

    ```
    MSBUILD_ARGS=/p:OuterLoop=true make -C src/mono/wasm/ run-browser-tests-System.AppContext
    ```

### Running tests using different Browsers
It's possible to set a Browser explicitly by adding `--browser=` command line argument to `XHARNESS_COMMAND`:

- CLI
    ```
    XHARNESS_COMMAND="test-browser --browser=safari" ./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Release
    ```

- Makefile target `run-browser-tests-<test>`

    ```
    XHARNESS_BROWSER=firefox make -C src/mono/wasm/ run-browser-tests-System.AppContext
    ```

At the moment supported values are:
- `chrome`
- `safari`
- `firefox`

By default, `chrome` browser is used.

## CoreCLR ReadyToRun library tests

Build the browser CoreCLR runtime, libraries, packs, and host crossgen2 first:

```bash
./build.sh clr+libs+host+packs -os browser -c Release /p:AotHostArchitecture=arm64 /p:AotHostOS=osx
```

Use the architecture and OS of the build machine for `AotHostArchitecture` and `AotHostOS`
(for example, `x64` and `linux` on an x64 Linux host).
On macOS, ensure a supported Python installation is ahead of Xcode's Python on `PATH`.

Run a library suite with trimmed ReadyToRun images:

```bash
XHARNESS_COMMAND=test-browser ./dotnet.sh build /t:Test \
  src/libraries/System.Runtime/tests/System.IO.UnmanagedMemoryStream.Tests/System.IO.UnmanagedMemoryStream.Tests.csproj \
  /p:TargetOS=browser /p:TargetArchitecture=wasm /p:RuntimeFlavor=CoreCLR /p:Configuration=Release \
  /p:TestWasmReadyToRun=true /p:EnableAggressiveTrimming=true \
  /p:Scenario=WasmTestOnChrome /p:InstallChromeForTests=true
```

`TestWasmReadyToRun` enables `PublishReadyToRun` only in browser CoreCLR library test projects.
Do not pass `PublishReadyToRun=true` globally to `build.sh`: that also attempts to publish
host-side build tools with ReadyToRun.
`EnableAggressiveTrimming` selects the shared mobile test trimming configuration, including
the xUnit and test-utility descriptors and trimming-aware test exclusions.
Library-specific descriptors retain their existing platform conditions; Apple-only roots
are not enabled for browser tests. The property must also be passed when building the test utilities.
R2R browser test apps also set `TEST_READY_TO_RUN_MODE=1`, so existing
`PlatformDetection.IsReadyToRunCompiled` conditions apply. Quarantines for shared WebAssembly
R2R issues use `PlatformDetection.IsWasmReadyToRun`, covering browser and WASI R2R while
leaving interpreter coverage enabled on both hosts.
`TestUtilities.dll` stays interpreted while its guarded platform probes are affected by
[the R2R platform-probe issue](https://github.com/dotnet/runtime/issues/133614).
The library and test assemblies still use ReadyToRun.

For the existing CoreCLR library smoke set, use:

```bash
XHARNESS_COMMAND=test-browser ./build.sh libs.tests -test -os browser -c Release \
  /p:RuntimeFlavor=CoreCLR /p:TestWasmReadyToRun=true /p:EnableAggressiveTrimming=true \
  /p:RunSmokeTestsOnly=true /p:Scenario=WasmTestOnChrome /p:InstallChromeForTests=true
```

Omit `RunSmokeTestsOnly` to build and run all supported library suites. To isolate trimming
from R2R, keep `EnableAggressiveTrimming=true` and pass `PublishReadyToRun=false`.
Use clean project-specific `bin` and `obj` browser-wasm outputs when switching modes;
stale staged assets can otherwise cause assembly-loading failures before tests start.

The `LibraryTestsCoreCLR_R2R` CI jobs use the same configuration and archive the published
tests for execution on Helix. The existing interpreter jobs remain separate.

## AOT library tests

- Building library tests with AOT, and (even) with `EnableAggressiveTrimming` takes 3-9mins on CI, and that adds up for all the assemblies, causing
a large build time. To circumvent that on CI, we build the test assemblies on the build machine, but skip the WasmApp build part of it, since
that includes the expensive AOT step.

- Instead, we take the built test assembly+dependencies, and enough related bits to be able to run the `WasmBuildApp` target, with the original
inputs.

- To recreate a similar build+test run locally, add `/p:BuildAOTTestsOnHelix=true` to the usual command line.
- For example, with `./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Release`

    - AOT:  add `/p:EnableAggressiveTrimming=true /p:RunAOTCompilation=true /p:BuildAOTTestsOnHelix=true`
    - Only trimming (helpful to isolate issues caused by trimming):
        - add `/p:EnableAggressiveTrimming=true /p:BuildAOTTestsOnHelix=true`
## Debugging

### Getting more information

- Line numbers: add `/p:DebuggerSupport=true` to the command line, for `Release` builds. It's enabled by default for `Debug` builds.

## Kicking off outer loop tests from GitHub Interface

Add the following to the comment of a PR.

```
/azp run runtime-libraries-mono outerloop
```

### Test App Design
TBD

### Obtaining the logs
TBD

### Existing Limitations
TBD
