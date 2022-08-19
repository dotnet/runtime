# Debugging Libraries with Visual Studio Code

- Install [Visual Studio Code](https://code.visualstudio.com/)
- Install the [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- Open the folder containing the source you want to debug in VS Code - i.e., if you are debugging a test failure in System.Net.Sockets, open `runtime/src/libraries/System.Net.Sockets`
- Open the debug window: `ctrl-shift-D` or click on the button on the left
- Click "create a launch.json file", select the option that includes `.NET Core` from the selection dropdown
- In the ".NET Core Launch (console)" `launch.json` configuration file make the following changes:
  - delete the `preLaunchTask` property
  - set `program` to the full path to `dotnet` in the artifacts/bin/testhost directory.
    - something like `{full path to your dotnet/runtime directory}/artifacts/bin/testhost/net{Version}-{OS}-{Configuration}-{Architecture}/dotnet`.
  - set `cwd` to the test bin directory.
    - using the System.Net.Sockets example, it should be something like `{full path to your dotnet/runtime directory}/artifacts/bin/System.Net.Sockets.Tests/Debug/net{Version}-{OS}`. Exact naming/structure might differ based on which library you are debugging.
  - set `args` to the command line arguments to pass to the test
    - something like: `[ "exec", "--runtimeconfig", "{TestProjectName}.runtimeconfig.json", "xunit.console.dll", "{TestProjectName}.dll", "-notrait", ... ]`, where TestProjectName would be `System.Net.Sockets.Tests`
    - to run a specific test, you can append something like: `[ "-method", "System.Net.Sockets.Tests.{ClassName}.{TestMethodName}", ...]`
    - to find the exact arguments to replicate a test run you are trying to debug, run the test command in a terminal and look for output starting with `exec`. Copy all arguments and reformat them in `launch.json` as shown above.
      - ex. running `dotnet build /t:Test` in `runtime/src/libraries/System.Net.Sockets/tests/FunctionalTests` has terminal output including `"exec --runtimeconfig System.Net.Sockets.Tests.runtimeconfig.json ... -notrait category=failing"`, which can be reformatted into `["exec","--runtimeconfig","System.Net.Sockets.Tests.runtimeconfigjson", ... ,"-notrait","category=failing"]`
      - similarly, running `dotnet build /t:Test /p:xUnitMethodName=System.Net.Sockets.Tests.{ClassName}.{TestMethodName}` will get you the args needed to debug a specific test
- Set a breakpoint and launch the debugger (running ".NET Core Launch (console)"), inspecting variables and call stacks will now work.
- Optionally, save the launch settings in a [workspace](https://code.visualstudio.com/docs/editor/workspaces) file. The advantage is that it doesn't necessarily need to reside in `.vscode` in the currently open directory, so it's much easier to preserve during `git clean -dfx`.

## Debugging Libraries with Visual Studio Code running on Mono

To debug the libraries on a "desktop" platform (Linux/Mac/Windows, not WebAssembly, or iOS or Android) running on Mono runtime, follow the instructions below.
See also [Android debugging](../mono/android-debugging.md) and [WebAssembly debugging](../mono/wasm-debugging.md)

- Install the VS Code [Mono Debugger (`ms-vscode.mono-debug`)](https://marketplace.visualstudio.com/items?itemName=ms-vscode.mono-debug) extension
- Create a `launch.json` file configuration with type `mono`

   ```json
   {
       "version": "0.2.0",
       "configurations": [
           {
               "name": "Attach to Mono",
               "type": "mono",
               "request": "attach",
               "address": "localhost",
               "port": 1235
           }
        ]
   }
   ```

- start a test from the command line, setting the `MONO_ENV_OPTIONS` environment variable to configure the debugger:

  ```sh
  DOTNET_REMOTEEXECUTOR_SUPPORTED=0 MONO_ENV_OPTIONS="--debug --debugger-agent=transport=dt_socket,address=127.0.0.1:1235,server=y,suspend=y" ./dotnet.sh build /t:Test /p:RuntimeFlavor=Mono src/libraries/System.Buffers/tests
  ```

  Note that you also have to set `DOTNET_REMOTEEXECUTOR_SUPPORTED=0` otherwise multiple instances of the runtime will attempt to listen on the same port.

  On Windows, do not pass `--debug` in `MONO_ENV_OPTIONS`.

- Set a breakpoint in a test in VS Code and start debugging in the "Attach to Mono" configuration.
- Note that Mono does not stop on first chance exceptions and xunit catches all exceptions, so if a test is throwing, the debugger won't break on an uncaught exception.
