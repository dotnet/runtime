# Debugging libraries

For building libraries or testing them without debugging, read:
- [Building libraries](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/libraries/README.md),
- [Testing libraries](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/libraries/testing.md).

## Run the tests with debugger support

Run the selected library tests in the browser, e.g. `System.Collections.Concurrent.Tests` this way:
```
dotnet run -r browser-wasm -c Release --project src/libraries/System.Collections.Concurrent/tests/System.Collections.Concurrent.Tests.csproj --debug --host browser -p:DebuggerSupport=true
```
where we choose `browser-wasm` as the runtime and by setting `DebuggerSupport=true` we ensure that tests won't start execution before the debugger will get attached. In the output, among others you should see:

```
Debug proxy for chrome now listening on http://127.0.0.1:PORT/. And expecting chrome at http://localhost:9222/
```
Copy the proxy's url, you will need it in the next step.

## Attach an IDE
You may need to close all Chrome instances. Then, start the browser with debugging mode enabled:

`chrome --remote-debugging-port=9222 <PROXY'S_URL>`

Open the tests in an IDE, e.g. VS and choose the option: `Debug -> Attach to process -> choose the test's process`. If you have problems with choosing the right process's name, check the PID on `PORT` with:

`netstat -aon | findstr <PORT>` and the name with `tasklist | findstr <PID>`.

Use the IDE to debug the tests.
