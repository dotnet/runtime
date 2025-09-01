# Building CoreCLR for WebAssembly

This guide provides instructions for building, running, and debugging CoreCLR on WebAssembly (WASM). This is experimental support and is currently under active development.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Building CoreCLR for WebAssembly](#building-coreclr-for-webassembly)
- [Testing the Runtime](#testing-the-runtime)
- [Debugging](#debugging)
  - [Chrome DevTools with DWARF Support](#chrome-devtools-with-dwarf-support)
  - [VS Code WebAssembly Debugging](#vs-code-webassembly-debugging)

## Prerequisites

Make sure you've prepared your environment and installed all the requirements for your platform. If not, follow this [link](/docs/workflow/README.md#introduction) for the corresponding instructions.

## Building CoreCLR for WebAssembly

To build the CoreCLR runtime for WebAssembly, use the following command from the repository root:

**Linux/macOS:**
```bash
./build.sh -os browser -c Debug -subset clr.runtime
```

**Windows:**
```cmd
.\build.cmd -os browser -c Debug -subset clr.runtime
```

This command will:
- Install the Emscripten SDK (emsdk) automatically
- Build the CoreCLR runtime for WebAssembly
- Build the required libraries

**Note:** The first build may take longer as it downloads and sets up the Emscripten toolchain.

## Testing the Runtime

### Browser Testing (Recommended)

If you don't have `dotnet-serve` installed, you can install it as a global .NET tool with:

```bash
dotnet tool install --global dotnet-serve
```

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

## Debugging

### Chrome DevTools with DWARF Support

For debugging CoreCLR WebAssembly code, the recommended approach is using Chrome browser with the **C/C++ DevTools Support (DWARF)** extension:

1. **Install the Chrome extension:**
   - [C/C++ DevTools Support (DWARF)](https://chrome.google.com/webstore/detail/cc-devtools-support-dwar/odljcjlcidgdhcjhoijagojpnjcgocgd)

2. **Open Chrome DevTools** (F12) while running your WebAssembly application

3. **Set breakpoints** in the Sources tab:
   - Navigate to the WebAssembly modules
   - You can step through C code, set breakpoints, and inspect WebAssembly linear memory
   - The extension provides source-level debugging with DWARF debug information

**Note:** The debugging experience is not perfect but works most of the time. You can step through C code, set breakpoints, and inspect the WebAssembly linear memory.

### VS Code WebAssembly Debugging

VS Code, through Node.js, provides a good debugging option for WebAssembly CoreCLR:

1. **Install the VS Code extension (Optional):**
   - [WebAssembly Dwarf Debugging](https://marketplace.visualstudio.com/items?itemName=ms-vscode.wasm-dwarf-debugging)

2. **Create a launch.json configuration:**
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

3. **Set breakpoints** in `corewasmrun.js` in one of the `put_char` functions (the `stdout`/`stderr` implementation)

4. **Start debugging** and step through the WebAssembly code using the call stack

This approach allows you to debug the JavaScript host and step into WebAssembly code or into the C/C++ code if the Dwarf Debugging extension was installed.
