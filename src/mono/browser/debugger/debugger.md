# Mono Wasm Debugger

## Native debugging

It's possible to debug native code and managed code using the `BrowserDebugProxy` project.

Steps:
- Install [C/C++ DevTools Support (DWARF) extension](https://chrome.google.com/webstore/detail/cc%20%20-devtools-support-dwa/pdcpmagijalfljmkmjngeonclgbbannb) on chrome.
- Enable DWARF support: Open DevTools, click on the settings, click on experiments and enable WebAssembly Debugging: Enable DWARF support.<br>
![image](https://user-images.githubusercontent.com/4503299/170745664-fc7d185c-469c-4443-9c57-545bd79588b8.png)
- Start the WebAssembly App Without Debugging from VS, or `dotnet run` on command line.
- Run chrome using this startup parameter: `--remote-debugging-port=9222`
- Go to your Blazor App Page
- Press Ctrl-Alt-D (windows) it will open the debugger page
- It will show something like this in the Address Bar: ``http://localhost:9222/devtools/inspector.html?ws=127.0.0.1:9300/devtools/page/97FCDA5A332CA3B72031790B26A264EF``
- Open another tab and go to ``chrome://inspect``<br>
![image](https://user-images.githubusercontent.com/4503299/170746026-8921892b-b936-458d-84f2-8a49b76755d4.png)
- Click on configure and add the port that was showed in the Address Bar: ``127.0.0.1:9300``<br>
![image](https://user-images.githubusercontent.com/4503299/170746126-b2edd688-dcc5-4b67-9162-465782646363.png)
- After some seconds the tabs available to debug will appear<br>
![image](https://user-images.githubusercontent.com/4503299/170746234-456ac8e9-180d-4173-a2fa-93cb8293514a.png)
- Choose the WebAssembly Page and click on Inspect<br>
![image](https://user-images.githubusercontent.com/4503299/170746341-809f8876-3f46-4c5c-b2b3-6f92af8beaa1.png)
- Open the Sources tab and click on ``file://``
- You will see c# files and c files available to add breakpoints.
