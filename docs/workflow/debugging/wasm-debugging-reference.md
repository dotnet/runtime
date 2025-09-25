# WebAssembly Debugging Reference

This document provides consolidated debugging instructions for WebAssembly applications that are referenced by multiple other documentation files.

## Debug with VS Code

To debug WebAssembly applications with Visual Studio Code:

### 1. Basic Configuration

Add the following configuration to your `.vscode/launch.json`:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "WASM Attach",
            "request": "attach",
            "type": "chrome",
            "address": "localhost",
            "port": <PROXY_PORT>
        }
    ]
}
```

Replace `<PROXY_PORT>` with the proxy port shown in your application's output.

### 2. Library Test Debugging

For debugging library tests specifically:

```json
{
    "name": "Libraries",
    "request": "attach", 
    "type": "chrome",
    "address": "localhost",
    "port": <PROXY_PORT>
}
```

### 3. WASI Debugging

For WASI applications, use the Mono Debug extension:

```json
{
    "name": "Attach",
    "type": "mono",
    "request": "attach", 
    "address": "localhost",
    "port": 64000
}
```

### 4. Setup Steps

1. **Set initial breakpoint**: Place a breakpoint in `WasmTestRunner.cs` or your main entry point to prevent execution before you're ready
2. **Run the configuration**: Launch the VS Code debug configuration  
3. **Set additional breakpoints**: Once stopped, set breakpoints in the code you want to debug
4. **Continue execution**: Click Resume or F5 to continue

## Debug with Chrome DevTools

### 1. Basic Setup

1. **Open Chrome Inspector**: Navigate to `chrome://inspect/#devices` in a new Chrome tab
2. **Configure proxy**: Click "Configure" and add the proxy address from your application output (e.g., `127.0.0.1:58346`)
3. **Select target**: Click "Inspect" next to your application in the Remote Targets list

### 2. Using DevTools

1. **Sources tab**: Look for the `file://` directory to browse source files
2. **Wait for files to load**: It may take time for all source files to appear
3. **Set breakpoints**: Click on line numbers to set breakpoints
4. **Initial run strategy**: Consider using the first run to set an initial breakpoint in `WasmTestRunner.cs`, then restart the application

### 3. For Native/C Code Debugging

1. **Install DWARF extension**: Install the "C/C++ DevTools Support (DWARF)" Chrome extension
2. **Enable symbols**: Build with `WasmNativeDebugSymbols=true` and `WasmNativeStrip=false`
3. **Debug native code**: Step through C/C++ code, set breakpoints, and inspect WebAssembly linear memory

## Starting Chrome with Remote Debugging

To enable remote debugging for WebAssembly applications:

```bash
# Close all Chrome instances first
chrome --remote-debugging-port=9222 <APP_URL>
```

Replace `<APP_URL>` with the URL shown in your application's output.

## Common Debugging Workflow

### For Library Tests

1. **Run test with debugging**:
   ```bash
   dotnet run -r browser-wasm -c Debug --project src/libraries/System.Collections/tests/System.Collections.Tests.csproj --debug --host browser -p:DebuggerSupport=true
   ```

2. **Note the output**: Look for lines like:
   ```
   Debug proxy for chrome now listening on http://127.0.0.1:58346/
   App url: http://127.0.0.1:9000/index.html?arg=--debug&arg=--run&arg=WasmTestRunner.dll
   ```

3. **Start Chrome**: Use the app URL with remote debugging enabled
4. **Attach debugger**: Use either Chrome DevTools or VS Code as described above

### For WASI Applications

1. **Build with debug**:
   ```bash
   cd sample/console  
   make debug
   ```

2. **Set up VS Code**: Use the Mono Debug extension configuration above
3. **Set breakpoints**: Place breakpoints in your Program.cs or other C# files
4. **Start debugging**: Launch the VS Code configuration

## Troubleshooting

### Files Not Loading in DevTools
- Wait patiently - source files can take time to load initially
- Try refreshing the DevTools window
- Ensure your build includes debug symbols

### Breakpoints Not Hit
- Verify the proxy port matches your configuration
- Check that Chrome is started with remote debugging enabled
- Ensure your breakpoints are set in code that will actually execute

### Connection Issues
- Verify no firewall is blocking the proxy port
- Check that the proxy is still running (visible in application output)
- Try restarting both the application and Chrome

## Advanced Debugging

### Enable Additional Logging

Add environment variables for more detailed logging:

```javascript
await dotnet
    .withDiagnosticTracing(true)
    .withConfig({
        environmentVariables: {
            "MONO_LOG_LEVEL": "debug",
            "MONO_LOG_MASK": "all"
        }
    })
    .run();
```

### Native Stack Traces

For native crashes with symbols:

1. **Build configuration**:
   ```xml
   <PropertyGroup>
     <WasmNativeDebugSymbols>true</WasmNativeDebugSymbols>
     <WasmNativeStrip>false</WasmNativeStrip>
   </PropertyGroup>
   ```

2. **Install DWARF extension** in Chrome for C/C++ debugging support

### Collect Stack Traces

From runtime code:
```c
#ifdef HOST_WASM
mono_wasm_print_stack_trace();
#endif
```

Or break into JavaScript debugger:
```c
#include <emscripten.h>
EM_ASM(debugger;);
```

## References

- [Testing Libraries on WebAssembly](../testing/libraries/testing-wasm.md)
- [Debugging WebAssembly Libraries](../testing/libraries/debugging-wasm.md)  
- [WASM Runtime Debugging](debugging/mono/wasm-debugging.md)
- [WASI Support](../../src/mono/wasi/README.md)
- [VS Code Debugging Guide](debugging/libraries/debugging-vscode.md)