[see also](../../../wasm/runtime/diagnostics/README.md)

To be able to run this sample you need to build the runtime with `/p:WasmEnableThreads=true` and use Chrome browser

# Testing with mock

Build the runtime with `/p:WasmEnableThreads=true /p:MonoDiagnosticsMock=true`
Run this test with `/p:MonoDiagnosticsMock=true`

It will inject file [mock.js](./mock.js) into the worker thread, which is mocking the `dotnet trace` tool.

The sample will communicate with the mock and start the Fibonacci experiment and start and stop the trace recording automatically.
It will also download the .nettrace from the browser. You can covert it to other formats, for example
```
dotnet trace convert --format Speedscope c:\Downloads\trace.1665653486202.nettrace -o c:\Downloads\trace.1665653486202.speedscope
```

# Testing with dotnet trace tool

Build the runtime with `/p:WasmEnableThreads=true`
Build a version of dsrouter with WebSockets support (versions from upstream that target net6.0 or later have the requisite support, see https://github.com/dotnet/diagnostics/blob/main/src/Tools/dotnet-dsrouter/dotnet-dsrouter.csproj)

In console #1 start dsrouter
```
c:\Dev\diagnostics\artifacts\bin\dotnet-dsrouter\Debug\net6.0\dotnet-dsrouter.exe server-websocket -ws http://127.0.0.1:8088/diagnostics -ipcs C:\Dev\diagnostics\socket
```

In console #2 start the sample
```
dotnet build /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Debug /t:RunSample src/mono/sample/wasm/browser-eventpipe /p:MonoDiagnosticsMock=false
```

In console #3 start the dotnet trace
```
dotnet trace collect --diagnostic-port C:\Dev\diagnostics\socket,connect --format Chromium
```
This will use `cpu-sampling`,

In the browser click `Start Work` button and after it finished

In the #3 console press `Enter`

In the browser open dev tools and on performance tab import file `C:\Dev\socket_XXXXXXXX_YYYYYY.chromium.json` which dotnet trace produced
