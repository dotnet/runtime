# Debugging libraries

For building libraries or testing them without debugging, read:
- [Building libraries](/docs/workflow/building/libraries/README.md),
- [Testing libraries](/docs/workflow/testing/libraries/testing.md).

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
Open `chrome://inspect/#devices` in a new tab in the browser you started. Select `Configure`:

![image](https://user-images.githubusercontent.com/32700855/201867874-7f707eb1-e859-441c-8205-abb70a7a0d0b.png)

and paste the address of proxy that was provided in the program output.

![image](https://user-images.githubusercontent.com/32700855/201862487-df76a06c-b24d-41a0-bf06-6959bba59a58.png)

New remote targets will be displayed, select the address you opened in the other tab by clicking `Inspect`.

![image](https://user-images.githubusercontent.com/32700855/201863048-6a4fe20b-a215-435d-b594-47750fcb2872.png)

A new window with Chrome DevTools will be opened. In the tab `sources` you should look for `file://` directory. There you can browse through libraries file tree and open the source code. It can take some time to load the files. When the IDE is ready the tests will start running. You cannot set a breakpoints in Chrome DevTools before the files get loaded, so you might want to use the first run for setting the initial breakpoint in `WasmTestRunner.cs` and then rerun the app. DevTools will stop on the previously set breakpoint and you will have time to set breakpoints in the libs you want to debug and click Resume.

## Debug with VS Code

Add following configuration to your `.vscode/launch.json`:
```
        {
            "name": "Libraries",
            "request": "attach",
            "type": "chrome",
            "address": "localhost",
            "port": <PROXY'S_PORT>
        }
```
Set at least one breakpoint in the libraries, you can do it initially in `WasmTestRunner.cs`.

Run the configuration and be patient, it can take some time. Wait till VS Code will get stopped in `WasmTestRunner`. Set breakpoints in the libs you want to debug and click Resume.
![image](https://user-images.githubusercontent.com/32700855/201894003-fc5394ad-9848-4d07-a132-f687ecd17c50.png)
