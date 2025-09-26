# Debugging libraries

For building libraries or testing them without debugging, read:
- [Building libraries](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/libraries/README.md),
- [Testing libraries](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/libraries/testing.md).

## Run the tests with debugger support

Run the selected library tests in the browser, e.g. `System.Collections.Concurrent.Tests` this way:
```
dotnet run -r browser-wasm -c Debug --project src/libraries/System.Collections/tests/System.Collections.Tests.csproj --debug --host browser -p:DebuggerSupport=true
```
where we choose `browser-wasm` as the runtime and by setting `DebuggerSupport=true` we ensure that tests won't start execution before the debugger will get attached. In the output, among others you should see:

```
Debug proxy for chrome now listening on http://127.0.0.1:58346/. And expecting chrome at http://localhost:9222/
App url: http://127.0.0.1:9000/index.html?arg=--debug&arg=--run&arg=WasmTestRunner.dll&arg=System.Collections.Concurrent.Tests.dll
```
The proxy's url/port will be used in the next step.

You may need to close all Chrome instances. Then, start the browser with debugging mode enabled:

`chrome --remote-debugging-port=9222 <APP_URL>`

Now you can choose an IDE to start debugging. Remember that the tests wait only till the debugger gets attached. Once it does, they start running. You may want to set breakpoints first, before attaching the debugger, e.g. setting one in `src\libraries\Common\tests\WasmTestRunner\WasmTestRunner.cs` on the first line of `Main()` will prevent any test to be run before you get prepared.

## Debug with Chrome DevTools

For detailed Chrome DevTools debugging instructions, see the [WebAssembly Debugging Reference](../debugging/wasm-debugging-reference.md#debug-with-chrome-devtools).

## Debug with VS Code

For detailed VS Code debugging instructions, see the [WebAssembly Debugging Reference](../debugging/wasm-debugging-reference.md#debug-with-vs-code).
