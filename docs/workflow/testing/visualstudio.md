# Working in dotnet/runtime using Visual Studio

Visual Studio is a great tool to use when working in the dotnet/runtime repo. 

Almost all its features should work well, but there are a few special considerations to bear in mind:

## Test Explorer 

You can run tests from the Visual Studio Test Explorer, but there are a few settings you need:
- Enable `Auto detect runsettings Files` (`Test Explorer window -> Settings button -> Options`). Test parameters (like which `dotnet` host to use) are persisted in an auto-generated .runsettings file, and it's important that Visual Studio knows to use it.
- Set `Processor Architecture for AnyCPU project` to `auto` (`Test Explorer window -> Settings button`).
- Consider whether to disable `Discover tests in real time from C# and Visual Basic .NET source files` (`Test explorer window -> Settings button -> Options`). 
    - You may want it enabled if you're actively writing new tests and want them to show up in Test Explorer without building first.
    - You may want it disabled if you're mostly running existing tests, and some of them have conditional attributes. Many of our unit tests have attributes, like `[SkipOnTargetFramework]`, to indicate that they're only valid in certain configurations. Because the real-time discovery feature does not currently recognize these attributes the tests will show up in Test Explorer as well, and fail or possibly hang when you try to run them.
- Consider whether to enable `Run tests in Parallel` (`Test Explorer window -> Settings button`). 
    - You may want it enabled if some of the unit tests you're working with run slowly or there's many of them.
    - You may want it disabled if you want to simplify debugging or viewing debug output.

If you encounter puzzling behavior while running tests within Visual Studio, first check the settings above, verify they run correctly from the command line, and also make sure you're using the latest Visual Studio. It can be helpful to enable detailed logging of the test runner (`Test explorer window -> Settings button -> Options > Logging Level: Trace`) - it may suggest the problem, or at least provide more information to share.

## Start with Debugging (F5)

dotnet/runtime uses `dotnet test` ([VSTest](https://github.com/Microsoft/vstest)) which spawns child processes during test execution.
Visual Studio by default doesn't automatically debug child processes, therefore preliminary steps need to be done to enable Debugging "F5" support.
Note that these steps aren't necessary for Visual Studio Test Explorer support.
1. Install the [Microsoft Child Process Debugging Power Tool](https://marketplace.visualstudio.com/items?itemName=vsdbgplat.MicrosoftChildProcessDebuggingPowerTool) extension.
2. Go to the child process debug settings (`Debug -> Other Debug Targets -> Child Process Debugging Settings...`), enable the "Enable child process debugging" option and hit save.
3. Go to the project debug settings (`Debug -> $ProjectName Properties`) and enable the "Enable native code debugging" option.

## References
- https://github.com/dotnet/project-system/issues/6176 tracks enabling the native code debugging functionality for multiple projects without user interaction.
- https://github.com/dotnet/sdk/issues/7419#issuecomment-298261617 explains the necessary steps to install and enable the mentioned extension in more detail.
- https://github.com/microsoft/vstest/ is the repo for issues with the Visual Studio test execution features.
