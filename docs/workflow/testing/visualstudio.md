# Visual Studio Test Explorer support
For Visual Studio Test Explorer to work in dotnet/runtime, the following test settings need to be enabled: 
- Test parameters (like which `dotnet` host to use) are persisted in an auto-generated .runsettings file. For that to work, make sure that the "Auto detect runsettings Files" (`Options -> Test`) option is enabled.
- Make sure that the "Processor Architecture for AnyCPU project" (`Test Explore pane -> Test Explorer toolbar options --> Settings`) value is set to `auto`.

# Visual Studio F5 Debugging support
dotnet/runtime uses `dotnet test` ([VSTest](https://github.com/Microsoft/vstest)) which spawns child processes during test execution.
Visual Studio by default doesn't automatically debug child processes, therefore preliminary steps need to be done to enable Debugging "F5" support.
Note that these steps aren't necessary for Visual Studio Test Explorer support.
1. Install the [Microsoft Child Process Debugging Power Tool](https://marketplace.visualstudio.com/items?itemName=vsdbgplat.MicrosoftChildProcessDebuggingPowerTool) extension.
2. Go to the child process debug settings (`Debug -> Other Debug Targets -> Child Process Debugging Settings...`), enable the "Enable child process debugging" option and hit save.
3. Go to the project debug settings (`Debug -> $ProjectName Properties`) and enable the "Enable native code debugging" option.

## References
- https://github.com/dotnet/project-system/issues/6176 tracks enabling the native code debugging functionality for multiple projects without user interaction.
- https://github.com/dotnet/sdk/issues/7419#issuecomment-298261617 explains the necessary steps to install and enable the mentioned extension in more detail.
