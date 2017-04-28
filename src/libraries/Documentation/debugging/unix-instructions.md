Debugging CoreFX on Unix
==========================

CoreFX can be debugged on unix using both lldb and visual studio code

## Using lldb and SOS

- Run the test using msbuild at least once with `/t:BuildAndTest`.
- Install version 3.6 of lldb and launch lldb with dotnet as the process and arguments matching the arguments used when running the test through msbuild.
- Load the sos plugin using `plugin load libsosplugin.so`.
- Type `soshelp` to get help. You can now use all sos commands like `bpmd`.

## Debugging core dumps with lldb

It is also possible to debug .NET Core crash dumps using lldb and SOS. In order to do this, you need all of the following:

- A machine whose environment matches the one used to produce the crash dumps. For crash dumps occurring on CI machines, you can either
  - Log onto a matching machine in the pool.
  - Create a new VM from the matching image.
- The crash dump file. We have a service called "Dumpling" which collects, uploads, and archives crash dump files during all of our CI jobs and official builds.
- Matching runtime bits from the crash. To get these, you should either:
  - Download the matching Jenkins archive onto your repro machine.
  - Check out the corefx repository at the appropriate commit and re-build the necessary portions.
- lldb version 3.8+. Versions 3.6+ of lldb work with regular debugging, but not core debugging. Make sure the version of lldb you have installed is >= 3.8.
- libsosplugin.so built against a matching version of lldb. Unfortunately, the one that is included in the CoreCLR nuget package is built against version 3.6. You will need to build coreclr from source to get the correct version. Luckily, this will help you get the next file:
- Symbols for libcoreclr.so. libcoreclr.so.dbg should be copied to your "runtime" folder. To get this file, you can:
  - Build coreclr at the matching commit. In order to determine which commit was used to build a version of libcoreclr.so, run the following command:
    `strings libcoreclr.so | grep "@(#)"`
  - You can also download the matching "symbols" nuget package from myget.org. You want the same package version that is used to build corefx. There is a "Download Symbols" button in the myget UI for this purpose.

Once you have everything listed above, you are ready to start debugging. You need to specify an extra parameter to lldb in order for it to correctly resolve the symbols for libcoreclr.so. Use a command like this:

```
lldb -O "settings set target.exec-search-paths <runtime-path>" --core <core-file-path> <dotnet-path>
```

- `<runtime-path>`: The path containing libcoreclr.so.dbg, as well as the rest of the runtime and framework assemblies.
- `<core-file-path>`: The path to the core dump you are attempting to debug.
- `<dotnet-path>`: The path to the dotnet executable, potentially in the `<runtime-path>` folder.

lldb should start debugging successfully at this point. You should see stacktraces with resolved symbols for libcoreclr.so. At this point, you can run `plugin load <libsosplugin.so-path>`, and begin using SOS commands, as above.

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
