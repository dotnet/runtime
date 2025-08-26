# Building CoreCLR for WebAssembly

This guide provides instructions for building, running, and debugging CoreCLR on WebAssembly (WASM). This is experimental support and is currently under active development.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Building CoreCLR for WebAssembly](#building-coreclr-for-webassembly)
- [Testing the Runtime](#testing-the-runtime)
- [Debugging](#debugging)
  - [Chrome DevTools with DWARF Support](#chrome-devtools-with-dwarf-support)
  - [VS Code WebAssembly Debugging](#vs-code-webassembly-debugging)
- [Current State](#current-state)
- [Next Steps](#next-steps)
- [Related Issues and Resources](#related-issues-and-resources)

## Prerequisites

Make sure you've prepared your environment and installed all the requirements for your platform. If not, follow this [link](/docs/workflow/README.md#introduction) for the corresponding instructions.

## Building CoreCLR for WebAssembly

To build the CoreCLR runtime for WebAssembly, use the following command from the repository root:

**Linux/macOS:**
```bash
./build.sh -bl -os browser -c Debug -subset clr.runtime+libs
```

**Windows:**
```cmd
.\build.cmd -os browser -c Debug -subset clr.runtime+libs
```

This command will:
- Install the Emscripten SDK (emsdk) automatically
- Build the CoreCLR runtime for WebAssembly
- Build the required libraries

**Note:** The first build may take longer as it downloads and sets up the Emscripten toolchain.

## Testing the Runtime

### Browser Testing (Recommended)

The simplest way to test the runtime in a browser is to use `dotnet-serve`:

**Linux/macOS:**
```bash
dotnet-serve --directory "artifacts/bin/coreclr/browser.wasm.Debug/corewasmrun"
```

**Windows:**
```cmd
dotnet-serve --directory "artifacts\bin\coreclr\browser.wasm.Debug\corewasmrun"
```

This will start a local HTTP server and you can open the provided URL in your browser.

### Console Testing

You can also run the runtime directly in Node.js:

```bash
cd artifacts/bin/coreclr/browser.wasm.Debug/corewasmrun/
node corewasmrun.js
```

### Manual Testing Steps (Alternative Method)

If you need more control over the testing process, you can follow these manual steps:

1. **Clean intermediate artifacts** (temporary workaround):
   ```bash
   rm -rf artifacts/obj/coreclr/browser.wasm.Debug/hosts/corewasmrun
   ```
   This step is necessary because libraries were not available when `corewasmrun` was initially linked. This is a temporary issue that will be resolved with proper assembly loading.

2. **Build the simple-server** (for testing):
   ```bash
   ./dotnet.sh build -c Release src/mono/sample/wasm/simple-server
   ```

3. **Build and run corewasmrun** in the browser:
   For a quicker development roundtrip, run this command from the `src/coreclr` directory:
   ```bash
   (source "../mono/browser/emsdk/emsdk_env.sh" && cd ../../artifacts/obj/coreclr/browser.wasm.Debug/; make corewasmrun -j12) && (cp hosts/corewasmrun/index.html ../../artifacts/obj/coreclr/browser.wasm.Debug/hosts/corewasmrun/; cd ../../artifacts/obj/coreclr/browser.wasm.Debug/hosts/corewasmrun/; ~/git/runtime-main/src/mono/sample/wasm/simple-server/bin/Release/net8.0/HttpServer)
   ```

   **On Windows**, the equivalent steps are:
   ```cmd
   cd artifacts\obj\coreclr\browser.wasm.Debug\
   src\mono\browser\emsdk\emsdk_env.cmd
   ninja corewasmrun
   copy src\coreclr\hosts\corewasmrun\index.html artifacts\obj\coreclr\browser.wasm.Debug\hosts\corewasmrun
   cd artifacts\obj\coreclr\browser.wasm.Debug\hosts\corewasmrun
   src\mono\sample\wasm\simple-server\bin\Release\net8.0\HttpServer.exe
   ```

**Note:** If Chrome is not your default browser, you can manually copy the URL and open it in Chrome for better debugging support.

## Debugging

### Chrome DevTools with DWARF Support

For debugging CoreCLR WebAssembly code, the recommended approach is using Chrome browser with the **C/C++ DevTools Support (DWARF)** extension:

1. **Install the Chrome extension:**
   - [C/C++ DevTools Support (DWARF)](https://chromewebstore.google.com/detail/cc++-devtools-support-dwa/pdcpmagijalfljmkmjngeonclgbbannb)

2. **Open Chrome DevTools** (F12) while running your WebAssembly application

3. **Set breakpoints** in the Sources tab:
   - Navigate to the WebAssembly modules
   - You can step through C code, set breakpoints, and inspect WebAssembly linear memory
   - The extension provides source-level debugging with DWARF debug information

**Note:** The debugging experience is not perfect but works most of the time. You can step through C code, set breakpoints, and inspect the WebAssembly linear memory.

### VS Code WebAssembly Debugging

VS Code provides multiple debugging options for WebAssembly CoreCLR:

#### Option 1: WebAssembly Dwarf Debugging Extension

1. **Install the VS Code extension:**
   - [WebAssembly Dwarf Debugging](https://marketplace.visualstudio.com/items?itemName=wasm-debug.webassembly-dwarf-debug)

2. **Configure your debugging environment** according to the extension's documentation

3. **Set up your launch configuration** in VS Code to attach to the WebAssembly runtime

#### Option 2: Node.js Debugging (Recommended)

For a simpler debugging experience, you can use VS Code's built-in Node.js debugger:

1. **Create a launch.json configuration:**
   ```json
   {
       "version": "0.2.0",
       "configurations": [
           {
               "type": "node",
               "request": "launch",
               "name": "corewasmrun",
               "skipFiles": [
                   "<node_internals>/**"
               ],
               "program": "corewasmrun.js",
               "cwd": "${workspaceFolder}/artifacts/bin/coreclr/browser.wasm.Debug/corewasmrun/"
           }
       ]
   }
   ```
   
   **Note:** On Windows, use backslashes in the path: `"${workspaceFolder}\\artifacts\\bin\\coreclr\\browser.wasm.Debug\\corewasmrun\\"`

2. **Set breakpoints** in `corewasmrun.js` at line 1815 (the `stdout`/`printf` implementation)

3. **Start debugging** and step through the WebAssembly code using the call stack

This approach allows you to debug the JavaScript host and step into WebAssembly code.

## Current State

As of the current development state, the WebAssembly CoreCLR implementation can compile and run several managed methods before encountering limitations. On the main branch, it successfully compiles and executes 6 managed methods:

```
Compiled method: .System.AppContext:Setup(ptr,ptr,int)
Compiled method: .System.Diagnostics.Debug:Assert(bool,System.String)
Compiled method: .System.Diagnostics.Debug:Assert(bool,System.String,System.String)
Compiled method: .System.Collections.Generic.Dictionary`2[System.__Canon,System.__Canon]:.ctor(int)
Compiled method: .System.Collections.Generic.Dictionary`2[System.__Canon,System.__Canon]:.ctor(int,System.Collections.Generic.IEqualityComparer`1[System.__Canon])
Compiled method: .System.Object:.ctor()
```

With the proof-of-concept implementation, this extends to approximately 60 methods and native functions before encountering the current limitations.

## Next Steps

The following areas are identified for continued development to progress with runtime initialization:

### High Priority
1. **Helper calls opcodes** - Implementation of runtime helper calls for various operations
2. **P/Invoke and QCalls** - Support for platform invoke and internal calls to native functions  
3. **Stack walking** - This is currently the main breaking point and requires investigation into WebAssembly-specific stack walking mechanisms

### Additional Work
4. **Compare WebAssembly startup with desktop startup** - Analysis to identify if managed exceptions are being hit during startup
5. **Assembly loading improvements** - Proper assembly loading to eliminate temporary workarounds
6. **Performance optimizations** - Once basic functionality is stable

## Related Issues and Resources

- **Main tracking issue:** [#119002 - wasm coreclr workflow and near future work](https://github.com/dotnet/runtime/issues/119002)
- **Helper calls opcodes:** [#119000](https://github.com/dotnet/runtime/issues/119000)  
- **P/Invoke / QCalls:** [#119001](https://github.com/dotnet/runtime/issues/119001)
- **Proof-of-concept branch:** [clr-interp-qcall-and-helper-call-poc](https://github.com/radekdoulik/runtime/tree/clr-interp-qcall-and-helper-call-poc)

For questions or issues related to CoreCLR WebAssembly development, please refer to the above issues or create new ones with the `arch-wasm` and `area-VM-coreclr` labels.