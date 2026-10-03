# Building and Running CoreCLR Tests on WebAssembly

* [Requirements](#requirements)
* [Building the Runtime](#building-the-runtime)
* [Building the Tests](#building-the-tests)
  * [Building an Individual Test](#building-an-individual-test)
  * [Building a Test Subtree](#building-a-test-subtree)
* [Running the Tests](#running-the-tests)
  * [Running Individual Tests](#running-individual-tests)
  * [Running All Built Tests](#running-all-built-tests)
* [Test Results](#test-results)

This guide covers building and running CoreCLR runtime tests targeting WebAssembly (browser/wasm). For general information about the CoreCLR test suite, see the main [testing doc](testing.md).

## Requirements

- A built CoreCLR runtime and libraries targeting `browser`/`wasm`. See the [building instructions](/docs/workflow/building/coreclr/README.md) for details.
- [Node.js](https://nodejs.org/) installed and available on `PATH` (required for running tests with `corerun.js`).

## Building the Runtime

Build CoreCLR and libraries for browser/wasm from the repo root.

On macOS/Linux:

```bash
./build.sh clr+libs -os browser
```

On Windows:

```cmd
.\build.cmd clr+libs -os browser
```

By default this produces a _Debug_ build. To specify a different configuration:

On macOS/Linux:

```bash
./build.sh clr+libs -os browser -c Release
```

On Windows:

```cmd
.\build.cmd clr+libs -os browser -c Release
```

The build output will be placed under `artifacts/bin/coreclr/browser.wasm.<Configuration>/`.

## Building the Tests

Test building is done via the `src/tests/build` script, specifying `wasm` and `browser` as the architecture and OS targets. The libraries configuration must match what was used during the runtime build.

### Building an Individual Test

To build a single test project:

On macOS/Linux:

```bash
./src/tests/build.sh -wasm -os browser Debug \
    -test:JIT/Regression/Regression_o_1.csproj \
    /p:LibrariesConfiguration=Debug
```

On Windows:

```cmd
.\src\tests\build.cmd wasm browser Debug ^
    test JIT\Regression\Regression_o_1.csproj ^
    /p:LibrariesConfiguration=Debug
```

### Building a Test Subtree

To build an entire subtree of tests:

On macOS/Linux:

```bash
./src/tests/build.sh -wasm -os browser Debug \
    -tree:JIT/Regression \
    /p:LibrariesConfiguration=Debug
```

On Windows:

```cmd
.\src\tests\build.cmd wasm browser Debug ^
    tree JIT\Regression ^
    /p:LibrariesConfiguration=Debug
```

After a successful build, the test binaries and runner scripts will be at:

```
artifacts/tests/coreclr/browser.wasm.<Configuration>/
```

The `Core_Root` folder (containing `corerun.js` and required libraries) will be at:

```
artifacts/tests/coreclr/browser.wasm.<Configuration>/Tests/Core_Root
```

## Running the Tests

Each test project produces a `.sh` (macOS/Linux) or `.cmd` (Windows) runner script in its output directory. Tests are executed using Node.js via the `corerun.js` host.

### Running Individual Tests

Set the `RunWithNodeJS` environment variable and pass the `-coreroot` argument pointing to `Core_Root`.

On macOS/Linux:

```bash
RunWithNodeJS=1 ./artifacts/tests/coreclr/browser.wasm.Debug/JIT/Regression/Regression_o_1/Regression_o_1.sh \
    -coreroot $(pwd)/artifacts/tests/coreclr/browser.wasm.Debug/Tests/Core_Root
```

On Windows (Command Prompt):

```cmd
set RunWithNodeJS=1
.\artifacts\tests\coreclr\browser.wasm.Debug\JIT\Regression\Regression_o_1\Regression_o_1.cmd -coreroot %CD%\artifacts\tests\coreclr\browser.wasm.Debug\Tests\Core_Root
```

On Windows (PowerShell):

```powershell
$Env:RunWithNodeJS = "1"
.\artifacts\tests\coreclr\browser.wasm.Debug\JIT\Regression\Regression_o_1\Regression_o_1.cmd -coreroot "$PWD\artifacts\tests\coreclr\browser.wasm.Debug\Tests\Core_Root"
```

Under the hood this invokes:

```
node <Core_Root>/corerun.js -c <Core_Root> <TestAssembly>.dll
```

### Running All Built Tests

On macOS/Linux:

```bash
./src/tests/run.sh wasm Debug
```

On Windows:

```cmd
.\src\tests\run.cmd wasm Debug
```

## Test Results

A successful test run exits with code **100**. The runner script compares the actual exit code against this expected value and prints:

```
Expected: 100
Actual: 100
END EXECUTION - PASSED
```

Test result XML files are written next to the test assembly (e.g., `Regression_o_1.testResults.xml`) and contain per-test pass/fail/skip information in xUnit format.
