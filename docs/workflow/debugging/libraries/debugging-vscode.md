# Debugging Libraries with Visual Studio Code

- Install [Visual Studio Code](https://code.visualstudio.com/)
- Install the [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- Open the folder containing the source you want to debug in VS Code - i.e., if you are debugging a test failure in System.Net.Sockets, open `runtime/src/libraries/System.Net.Sockets`
- Open the debug window: `ctrl-shift-D` or click on the button on the left
- Click the gear button at the top to create a launch configuration, select `.NET Core` from the selection dropdown
- In the ".NET Core Launch (console)" `launch.json` configuration file make the following changes:
  - delete the `preLaunchTask` property
  - set `program` to the full path to `dotnet` in the artifacts/bin/testhost directory.
    - something like `artifacts/bin/testhost/netcoreapp-{OS}-{Configuration}-{Architecture}`, plus the full path to your dotnet/runtime directory.
  - set `cwd` to the test bin directory.
    - using the System.Net.Sockets example, it should be something like `artifacts/bin/System.Net.Sockets.Tests/netcoreapp-{OS}-{Configuration}-{Architecture}`, plus the full path to your dotnet/runtime directory.
  - set `args` to the command line arguments to pass to the test
    - something like: `[ "exec", "--runtimeconfig", "{TestProjectName}.runtimeconfig.json", "xunit.console.dll", "{TestProjectName}.dll", "-notrait", ... ]`, where TestProjectName would be `System.Net.Sockets.Tests`
    - to run a specific test, you can append something like: `[ "-method", "System.Net.Sockets.Tests.{ClassName}.{TestMethodName}", ...]`
- Set a breakpoint and launch the debugger, inspecting variables and call stacks will now work
