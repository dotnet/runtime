---
name: wasm-browser-testing
description: Build and run CoreCLR runtime tests on WebAssembly/browser. Use when asked to build, run, or debug WASM tests, run JIT/GC tests on browser/wasm, test with Node.js on WASM, or asked "how do I run tests on wasm", "build for browser", or "run wasm tests". Also use when asked "run runtime test", "run test", or "build and run test" and the runtime is already built for wasm/browser (check for artifacts/tests/coreclr/browser.wasm.Debug, artifacts/tests/coreclr/browser.wasm.Release, artifacts/tests/coreclr/browser.wasm.Checked, artifacts/obj/coreclr/browser.wasm.Debug, artifacts/obj/coreclr/browser.wasm.Release, artifacts/obj/coreclr/browser.wasm.Checked, artifacts/bin/coreclr/browser.wasm.Debug, artifacts/bin/coreclr/browser.wasm.Release, or artifacts/bin/coreclr/browser.wasm.Checked).
---

# Building and Running CoreCLR Tests on WebAssembly/Browser

This skill covers building the CoreCLR runtime for WebAssembly and running tests locally using Node.js on Linux, macOS, and Windows. CoreCLR on WASM is experimental and under active development.

> ⚠️ **Browser/WASM defaults to Mono.** You must explicitly include the `clr` subset (not `mono`) to build with CoreCLR. Omitting `clr` gives you Mono.

## When to Use This Skill

Use this skill when:
- Building CoreCLR for WebAssembly/Browser (`-os browser`)
- Running JIT, GC, or regression tests on browser/wasm
- Running runtime tests with Node.js on WASM
- Running library tests with `RuntimeFlavor=CoreCLR` on browser/wasm
- Debugging CoreCLR WASM issues with Chrome DevTools or VS Code
- Asked questions like "how do I run tests on wasm", "build for browser", "run wasm tests with node"

## Prerequisites

- **Node.js** — Required for running tests via `corerun.js` or corehost `main.mjs`
- **Emscripten SDK** — Auto-downloaded on first build (or set `EMSDK_PATH` to use an existing install)
- **Python 3** — Required by Emscripten and by `run.py` test orchestrator
- **JS engines** (optional, for library tests) — V8, SpiderMonkey, JavaScriptCore via [jsvu](https://github.com/GoogleChromeLabs/jsvu)
- **Browsers** (optional, for browser tests) — Chrome + ChromeDriver, or Firefox + GeckoDriver

## Quick Start

```bash
# 1. Build CoreCLR + libraries for WASM
./build.sh clr+libs -os browser -c Debug

# 2. Build runtime tests
./src/tests/build.sh -wasm -os browser Debug /p:LibrariesConfiguration=Debug

# 3. Run ALL runtime tests with Node.js
./src/tests/run.sh wasm

# 4. Run a single test (coreroot MUST be an absolute path)
RunWithNodeJS=1 ./artifacts/tests/coreclr/browser.wasm.Debug/JIT/Regression/Regression_3/Regression_3.sh \
  -coreroot $(pwd)/artifacts/tests/coreclr/browser.wasm.Debug/Tests/Core_Root
```

**Windows:**
```cmd
REM 1. Build CoreCLR + libraries for WASM
build.cmd clr+libs -os browser -c Debug

REM 2. Build runtime tests
src\tests\build.cmd wasm browser Debug /p:LibrariesConfiguration=Debug

REM 3. Run ALL runtime tests with Node.js
src\tests\run.cmd wasm node

REM 4. Run a single test (coreroot MUST be an absolute path)
set RunWithNodeJS=1
artifacts\tests\coreclr\browser.wasm.Debug\JIT\Regression\Regression_3\Regression_3.cmd -coreroot %CD%\artifacts\tests\coreclr\browser.wasm.Debug\Tests\Core_Root
```

> On Windows, V8 execution is not supported. You must use Node.js (`RunWithNodeJS=1`).

## Step-by-Step Workflow

### Step 1: Build CoreCLR for WASM

Build the CoreCLR runtime and libraries targeting browser/wasm:

**Linux/macOS:**
```bash
./build.sh clr+libs -os browser -c Debug
```

**Windows:**
```cmd
build.cmd clr+libs -os browser -c Debug
```

This will:
- Auto-download and configure the Emscripten SDK
- Build the CoreCLR runtime for WebAssembly
- Build the required libraries

The first build takes longer due to Emscripten setup. Build artifacts land in `artifacts/bin/coreclr/browser.wasm.Debug/`.

To build just the runtime without libraries:

**Linux/macOS:**
```bash
./build.sh -os browser -c Debug -subset clr.runtime
```

**Windows:**
```cmd
build.cmd -os browser -c Debug -subset clr.runtime
```

### Step 2: Build Runtime Tests

Build the CoreCLR test suite (JIT, GC, regression tests) and match the `LibrariesConfiguration` to how you built the libraries.

> ⚠️ **Platform difference:** On Linux/macOS, use `-wasm -os browser` flags. On Windows, use bare keywords `wasm browser` (no dashes) as positional arguments.

**Linux/macOS:**
```bash
./src/tests/build.sh -wasm -os browser Debug /p:LibrariesConfiguration=Debug
```

**Windows:**
```cmd
src\tests\build.cmd wasm browser Debug /p:LibrariesConfiguration=Debug
```

To build a single test:

> ⚠️ **Platform difference for `-test`:** On Linux/macOS, use `-test:Project.csproj` (colon syntax). On Windows, use `-test Project.csproj` (space-separated).

> ⚠️ **The `-test` path must be relative to `src/tests/`**, not the repo root. The build script prepends `src/tests/` internally, so passing a full path like `src/tests/JIT/Regression/Regression_4.csproj` results in a doubled path (`src/tests/src/tests/...`) and a build failure.

**Linux/macOS:**
```bash
./src/tests/build.sh -wasm -os browser Debug -test:JIT/Regression/Regression_4.csproj /p:LibrariesConfiguration=Debug
```

**Windows:**
```cmd
src\tests\build.cmd wasm browser Debug -test JIT\Regression\Regression_4.csproj /p:LibrariesConfiguration=Debug
```

By default, only Priority 0 (Pri0) tests are built. To also build Priority 1 tests:

> ⚠️ **Platform difference for `-priority`:** On Linux/macOS, use `-priority1` (single flag). On Windows, use `priority 1` (space-separated keyword and value).

**Linux/macOS:**
```bash
./src/tests/build.sh -wasm -os browser Debug /p:LibrariesConfiguration=Debug -priority1
```

**Windows:**
```cmd
src\tests\build.cmd wasm browser Debug priority 1 /p:LibrariesConfiguration=Debug
```

This generates per-test runner scripts (`.sh` on Linux/macOS, `.cmd` on Windows) under:
```
artifacts/tests/coreclr/browser.wasm.Debug/
```

Each test directory contains a generated script, for example:
```
artifacts/tests/coreclr/browser.wasm.Debug/JIT/Regression/Regression_3/Regression_3.sh
```

### Step 3: Run All Tests

The `run.sh` script discovers and executes all generated test scripts. When you pass `wasm`, it automatically sets `RunWithNodeJS=1` and `buildOS=browser`.

**Linux/macOS:**
```bash
./src/tests/run.sh wasm
```

**Windows:**
```cmd
src\tests\run.cmd wasm node
```

Common options:

**Linux/macOS:**
```bash
./src/tests/run.sh wasm Debug              # Explicit configuration (default: Debug)
./src/tests/run.sh wasm --sequential       # Run tests one at a time
./src/tests/run.sh wasm --verbose          # Show output from each test
./src/tests/run.sh wasm --jitstress=2      # Run with JIT stress level 2
./src/tests/run.sh wasm --interpreter      # Run with the interpreter enabled
```

**Windows:**
```cmd
src\tests\run.cmd wasm node Debug          REM Explicit configuration (default: Debug)
src\tests\run.cmd wasm node sequential     REM Run tests one at a time
src\tests\run.cmd wasm node jitstress 2    REM Run with JIT stress level 2
src\tests\run.cmd wasm node interpreter    REM Run with the interpreter enabled
```

> ⚠️ **Platform difference:** On Linux/macOS, options use `--` prefix (e.g., `--sequential`). On Windows, use bare keywords without `--` (e.g., `sequential`). Windows `run.cmd` does not support `--verbose`.

### Step 4: Run Individual Tests

Run a single test using its generated `.sh` script with the `-coreroot` argument pointing to `Core_Root`.

> ⚠️ **The `-coreroot` path MUST be absolute.** A relative path causes `corerun.js` to fail loading `System.Private.CoreLib.dll`. Use `$(pwd)` (or `%CD%` on Windows) to construct the absolute path.

**Linux/macOS:**
```bash
RunWithNodeJS=1 ./artifacts/tests/coreclr/browser.wasm.Debug/JIT/Regression/Regression_3/Regression_3.sh \
  -coreroot $(pwd)/artifacts/tests/coreclr/browser.wasm.Debug/Tests/Core_Root
```

**Windows:**
```cmd
set RunWithNodeJS=1
artifacts\tests\coreclr\browser.wasm.Debug\JIT\Regression\Regression_3\Regression_3.cmd ^
  -coreroot %CD%\artifacts\tests\coreclr\browser.wasm.Debug\Tests\Core_Root
```

### Interpreting Test Results

Test runner scripts exit with code **100** on success (not 0). Do not confuse this with a failure.

Detailed results are written to an XML file alongside the test runner script. The file is named `<TestName>.testResults.xml` and located in the same directory as the test runner. For example:
```
artifacts/tests/coreclr/browser.wasm.Debug/<TestArea>/<TestName>/<TestName>.testResults.xml
```

Parse it to get pass/fail/skip counts:

**PowerShell (Windows):**
```powershell
[xml]$xml = Get-Content "artifacts\tests\coreclr\browser.wasm.Debug\Loader\Loader\Loader.testResults.xml"
$a = $xml.SelectNodes("//assembly")[0]
"Total: $($a.total), Passed: $($a.passed), Failed: $($a.failed), Skipped: $($a.skipped)"
```

**Bash (Linux/macOS):**
```bash
xmllint --xpath '//assembly/@total | //assembly/@passed | //assembly/@failed | //assembly/@skipped' \
  artifacts/tests/coreclr/browser.wasm.Debug/Loader/Loader/Loader.testResults.xml
```

Always check the `.testResults.xml` file for actual test counts rather than relying on console output alone.

### Step 5: Console Testing with Node.js

CoreCLR on WASM can be tested directly with Node.js using either `corerun.js` or corehost (`main.mjs`).

#### Using corerun.js

**Linux/macOS:**
```bash
# Copy managed assemblies into the IL directory
cp ./artifacts/bin/microsoft.netcore.app.runtime.browser-wasm/Debug/runtimes/browser-wasm/lib/net11.0/*.dll \
  ./artifacts/bin/coreclr/browser.wasm.Debug/IL

# Copy your test DLL
cp helloworld.dll ./artifacts/bin/coreclr/browser.wasm.Debug/IL

# Run with Node.js (use absolute unix-style paths, even on Windows)
cd ./artifacts/bin/coreclr/browser.wasm.Debug/
node ./corerun.js -c /absolute/path/to/artifacts/bin/coreclr/browser.wasm.Debug/IL \
  /absolute/path/to/artifacts/bin/coreclr/browser.wasm.Debug/IL/helloworld.dll
```

**Windows:**
```cmd
REM Copy managed assemblies into the IL directory
copy artifacts\bin\microsoft.netcore.app.runtime.browser-wasm\Debug\runtimes\browser-wasm\lib\net11.0\*.dll ^
  artifacts\bin\coreclr\browser.wasm.Debug\IL\

REM Copy your test DLL
copy helloworld.dll artifacts\bin\coreclr\browser.wasm.Debug\IL\

REM Run with Node.js (use absolute unix-style paths even on Windows)
cd artifacts\bin\coreclr\browser.wasm.Debug
node corerun.js -c /absolute/path/to/artifacts/bin/coreclr/browser.wasm.Debug/IL ^
  /absolute/path/to/artifacts/bin/coreclr/browser.wasm.Debug/IL/helloworld.dll
```

#### Using corehost (main.mjs)

**Linux/macOS:**
```bash
cp ./artifacts/bin/microsoft.netcore.app.runtime.browser-wasm/Debug/runtimes/browser-wasm/lib/net11.0/*.dll \
  ./artifacts/bin/coreclr/browser.wasm.Debug/corehost

cp helloworld.dll ./artifacts/bin/coreclr/browser.wasm.Debug/corehost

cd ./artifacts/bin/coreclr/browser.wasm.Debug/corehost
node ./main.mjs
```

**Windows:**
```cmd
copy artifacts\bin\microsoft.netcore.app.runtime.browser-wasm\Debug\runtimes\browser-wasm\lib\net11.0\*.dll ^
  artifacts\bin\coreclr\browser.wasm.Debug\corehost\

copy helloworld.dll artifacts\bin\coreclr\browser.wasm.Debug\corehost\

cd artifacts\bin\coreclr\browser.wasm.Debug\corehost
node main.mjs
```

> **Important:** Both `corerun.js` and `main.mjs` require **absolute unix-style paths** for arguments and `CORE_ROOT`, even on Windows.

### Step 6: Library Tests with CoreCLR

To run library tests (e.g., `System.AppContext`) on WASM with CoreCLR, pass `/p:RuntimeFlavor=CoreCLR`:

**Linux/macOS:**
```bash
./dotnet.sh build /t:Test src/libraries/System.AppContext/tests \
  /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Release \
  /p:RuntimeFlavor=CoreCLR
```

**Windows:**
```cmd
dotnet.cmd build /t:Test src\libraries\System.AppContext\tests ^
  /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Release ^
  /p:RuntimeFlavor=CoreCLR
```

To use a specific JS engine:
```bash
./dotnet.sh build /t:Test src/libraries/System.AppContext/tests \
  /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Release \
  /p:RuntimeFlavor=CoreCLR /p:JSEngine=SpiderMonkey
```

Supported `JSEngine` values: `V8` (default), `SpiderMonkey`, `JavaScriptCore`.

To run in a browser instead of a JS engine:
```bash
XHARNESS_COMMAND=test-browser ./dotnet.sh build /t:Test src/libraries/System.AppContext/tests \
  /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Release \
  /p:RuntimeFlavor=CoreCLR
```

## Platform Differences

| Aspect | Linux/macOS | Windows |
|--------|-------------|---------|
| Build script | `./build.sh` | `build.cmd` |
| Test build | `./src/tests/build.sh` | `src\tests\build.cmd` |
| Test build WASM flags | `-wasm -os browser` | `wasm browser` (bare keywords) |
| Test build single test | `-test:Project.csproj` (colon) | `-test Project.csproj` (space) |
| `-test` path base | Relative to `src/tests/` | Relative to `src\tests\` |
| Test build priority | `-priority1` (single flag) | `priority 1` (space-separated) |
| Run all tests | `./src/tests/run.sh wasm` | `src\tests\run.cmd wasm node` |
| Run options prefix | `--sequential`, `--jitstress=2` | `sequential`, `jitstress 2` (no `--`) |
| Generated scripts | `.sh` (bash) | `.cmd` (batch) |
| dotnet CLI | `./dotnet.sh` | `dotnet.cmd` |
| Set env var | `RunWithNodeJS=1 ./test.sh` | `set RunWithNodeJS=1` then run |
| V8 standalone | Supported | Not supported (use Node.js) |

## CoreCLR-Specific Defaults

When `RuntimeFlavor=CoreCLR` is set for browser/wasm, these properties are configured automatically (in `eng/testing/tests.browser.targets`):

| Property | Value | Notes |
|----------|-------|-------|
| `InvariantGlobalization` | `false` | Full ICU support by default |
| `WasmEnableWebcil` | `false` | Webcil packaging disabled |
| `WasmTestSupport` | `true` | Test infrastructure enabled |
| `WasmTestExitOnUnhandledError` | `true` | Fail fast on unhandled errors |
| `WasmTestAppendElementOnExit` | `true` | Signal test completion to harness |
| `WasmTestLogExitCode` | `true` | Log exit codes for diagnostics |

## Key Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `RunWithNodeJS` | Use Node.js to run tests (set to `1`) | Auto-set by `run.sh wasm` |
| `EMSDK_PATH` | Path to existing Emscripten SDK | Auto-downloaded if not set |
| `JS_ENGINE` | XHarness engine flag (e.g., `--engine=V8`) | V8 |
| `XHARNESS_COMMAND` | `test` (JS engine) or `test-browser` (browser) | `test` |
| `V8_PATH_FOR_TESTS` | Custom V8 binary path | Auto-detected |
| `CHROME_PATH_FOR_TESTS` | Custom Chrome binary path | Auto-detected |

## Debugging

### Chrome DevTools with DWARF

1. Install the [C/C++ DevTools Support (DWARF)](https://goo.gle/wasm-debugging-extension) Chrome extension
2. Serve the build output: `dotnet-serve --directory "artifacts/bin/coreclr/browser.wasm.Debug"`
3. Open Chrome DevTools (F12), navigate to Sources tab to set breakpoints in C code

### VS Code with Node.js

Add to `.vscode/launch.json` (replace `/path/to/runtime/` with your absolute repo path):

```json
{
  "type": "node",
  "request": "launch",
  "name": "corerun",
  "program": "corerun.js",
  "env": {
    "CORE_ROOT": "/path/to/runtime/artifacts/bin/coreclr/browser.wasm.Debug/IL/"
  },
  "runtimeArgs": ["--stack-trace-limit=1000"],
  "args": ["/path/to/runtime/artifacts/bin/coreclr/browser.wasm.Debug/IL/helloworld.dll"],
  "cwd": "${workspaceFolder}/artifacts/bin/coreclr/browser.wasm.Debug/",
  "outputCapture": "std"
}
```

Optionally install the [WebAssembly DWARF Debugging](https://marketplace.visualstudio.com/items?itemName=ms-vscode.wasm-dwarf-debugging) extension for source-level C/C++ debugging.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Build defaults to Mono | Include `clr` in subset: `./build.sh clr+libs -os browser` |
| First build is very slow | Emscripten SDK is downloading — wait for it to complete |
| `V8 execution is not supported on Windows` | Set `RunWithNodeJS=1` — Windows requires Node.js |
| Tests not found by `run.sh` | Build tests first: `./src/tests/build.sh -wasm -os browser Debug /p:LibrariesConfiguration=Debug` |
| `-coreroot` relative path fails | The `-coreroot` path **must be absolute** — use `$(pwd)/artifacts/...` or `%CD%\artifacts\...` |
| `-test` path doubled (`src/tests/src/tests/...`) | The `-test` path must be relative to `src/tests/`, not the repo root — use `JIT/Regression/Foo.csproj`, not `src/tests/JIT/Regression/Foo.csproj` |
| `corerun.js` path errors | Use absolute unix-style paths, even on Windows |
| Missing managed DLLs | Copy from `artifacts/bin/microsoft.netcore.app.runtime.browser-wasm/` to IL or corehost directory |
| Library tests use Mono | Add `/p:RuntimeFlavor=CoreCLR` to the build command |

## References

- [Building CoreCLR for WebAssembly](../../../docs/workflow/building/coreclr/wasm.md)
- [Testing CoreCLR on WebAssembly](../../../docs/workflow/testing/coreclr/testing-wasm.md)
- [Testing Libraries on WebAssembly](../../../docs/workflow/testing/libraries/testing-wasm.md)
- [Building Libraries for WebAssembly](../../../docs/workflow/building/libraries/webassembly-instructions.md)
