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
./build.sh -os Browser -c Release
```
and even run tests one by one for each library:
```
./build.sh libs.tests -test -os Browser -c Release
```

### Running individual test suites using JavaScript engine
The following shows how to run tests for a specific library
```
./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=Release
```

### Running outer loop tests using JavaScript engine

To run all tests, including "outer loop" tests (which are typically slower and in some test suites less reliable, but which are more comprehensive):
```
./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=Release /p:Outerloop=true
```

### Running tests using different JavaScript engines
It's possible to set a JavaScript engine explicitly by adding `/p:JSEngine` property:

```
./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=Release /p:JSEngine=SpiderMonkey
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
    XHARNESS_COMMAND=test-browser ./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=Release
    ```
- Makefile target `run-browser-tests-<test>`
    ```
    make -C src/mono/wasm/ run-browser-tests-System.AppContext
    ```

### Passing arguments to xharness

- `$(WasmXHarnessArgs)` - xharness command arguments

    Example: `WasmXHarnessArgs="--xyz"` -> becomes `dotnet xharness wasm test --xyz`

- `$(WasmXHarnessMonoArgs)` - arguments to mono

    Example: `WasmXHarnessMonoArgs="--runtime-arg=--trace=E"`

- `$(WasmTestAppArgs)` - arguments for the test app itself

### Running outer loop tests using Browser instance

To run all tests, including "outer loop" tests (which are typically slower and in some test suites less reliable, but which are more comprehensive):

- CLI
    ```
    XHARNESS_COMMAND=test-browser ./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=Release /p:Outerloop=true
    ```

- Makefile target `run-browser-tests-<test>`

    ```
    MSBUILD_ARGS=/p:OuterLoop=true make -C src/mono/wasm/ run-browser-tests-System.AppContext
    ```

### Running tests using different Browsers
It's possible to set a Browser explicitly by adding `--browser=` command line argument to `XHARNESS_COMMAND`:

- CLI
    ```
    XHARNESS_COMMAND="test-browser --browser=safari" ./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=Release
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
