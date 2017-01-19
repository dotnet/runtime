Debugging CoreFX on Unix
==========================

CoreFX can be debugged on unix using both lldb and visual studio code

## Using lldb and SOS

- Run the test using msbuild at least once with `/t:BuildAndTest`.
- Install version 3.6 of lldb and launch lldb with corerun as the process and arguments matching the arguments used when running the test through msbuild.
- Load the sos plugin using `plugin load libsosplugin.so`.
- Type `soshelp` to get help. You can now use all sos commands like `bpmd`.

## Using Visual Studio Code

- Install [Visual Studio Code](https://code.visualstudio.com/)
- Install the [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
- Open the folder containing the source you want to debug in VS Code
- Open the debug window: `ctrl-shift-D` or click on the button on the left
- Click the gear button at the top to create a launch configuration, select `.NET Core` from the selection dropdown
- In the `.NET Core Launch (console)` configuration do the following
  - delete the `preLaunchTask` property
  - set `program` to the full path to corerun in the test directory
  - set `cwd` to the test directory
  - set `args` to the command line arguments to pass to the test
    - something like: `[ "xunit.console.netcore.exe", "<test>.dll", "-notrait", .... ]`
- Set a breakpoint and launch the debugger, inspecting variables and call stacks will now work
