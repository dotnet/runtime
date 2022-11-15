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
Content root path: ...\artifacts\bin\System.Collections.Tests\Debug\net7.0\browser-wasm\AppBundle
App url: http://127.0.0.1:9000/index.html?arg=--debug&arg=--run&arg=WasmTestRunner.dll&arg=System.Collections.Concurrent.Tests.dll
```
The proxy's url/port will be used in the next step. If you choose VS Code to debug, `Current root path` will be needed as well.

You may need to close all Chrome instances. Then, start the browser with debugging mode enabled:

`chrome --remote-debugging-port=9222 <APP_URL>`

Now you can choose an IDE to start debugging.

## Debug with Chrome DevTools
Open `chrome://inspect/#devices` in a new tab in the browser you started. Select `Configure`:

![image](https://user-images.githubusercontent.com/32700855/201867874-7f707eb1-e859-441c-8205-abb70a7a0d0b.png)

and paste the address of proxy that was provided in the program output.

![image](https://user-images.githubusercontent.com/32700855/201862487-df76a06c-b24d-41a0-bf06-6959bba59a58.png)

New remote targets will be displayed, select the address you opened in the other tab by clicking `Inspect`.

![image](https://user-images.githubusercontent.com/32700855/201863048-6a4fe20b-a215-435d-b594-47750fcb2872.png)

A new window with Chrome DevTools will be opened. In the tab `sources` you should look for `file://` directory. There you can browse through libraries file tree and open the source code. Initially, the tests are stopped in the beginning of `Main` of `WasmTestRunner`. Set breakpoints in the libs you want to debug and click Resume.

## Debug with VS Code

Add following configuration to your `.vscode/launch.json`:
```
        {
            "name": "Libraries",
            "request": "attach",
            "type": "chrome",
            "address": "localhost",
            "port": <PROXY'S_PORT>,
            "webRoot": "${workspaceFolder}\\<CURRENT_ROOT_PATH>"
        }
```
Run the configuration and wait till VS Code will get stopped in the beginning of `Main` of `WasmTestRunner`. Set breakpoints in the libs you want to debug and click Resume.
![image](https://user-images.githubusercontent.com/32700855/201890837-5c338ce1-2957-4dcf-aa3b-78045d131f0a.png)
