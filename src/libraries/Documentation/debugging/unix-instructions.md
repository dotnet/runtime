Debugging CoreFX on Unix
==========================

CoreFX can be debugged on unix using both lldb and visual studio code

## Using lldb and SOS

- Run the test using msbuild at least once with `/t:BuildAndTest`.
- [Install version 3.9 of lldb](https://github.com/dotnet/coreclr/blob/master/Documentation/building/debugging-instructions.md#debugging-core-dumps-with-lldb) and launch lldb with dotnet as the process and arguments matching the arguments used when running the test through msbuild. 
- Load the sos plugin using `plugin load libsosplugin.so`.
- Type `soshelp` to get help. You can now use all sos commands like `bpmd`.

You may need to supply a path to load SOS. It can be found next to libcoreclr.so. For example:
```
(lldb) plugin load libsosplugin.so
error: no such file
(lldb) image list libcoreclr.so
[  0] ..... /home/dan/dotnet/shared/Microsoft.NETCoreApp/2.0.4/libcoreclr.so
(lldb) plugin load /home/dan/dotnet/shared/Microsoft.NETCoreApp/2.0.4/libcoreclr.so
```

## Debugging core dumps with lldb

It is also possible to debug .NET Core crash dumps using lldb and SOS. In order to do this, you need all of the following:

- You will find the dump url to download the crash dump file in the test logs, something similar to:
<pre>
2017-10-10 21:17:48,020: INFO: proc(54): run_and_log_output: Output: dumplingid:  eefcb1cc36977ccf86f457ee28a33a7b4cc24e13
2017-10-10 21:17:48,020: INFO: proc(54): run_and_log_output: Output: <b>https://dumpling.azurewebsites.net/api/dumplings/archived/eefcb1cc36977ccf86f457ee28a33a7b4cc24e13</b>
</pre>
- The crash dump file. We have a service called "Dumpling" which collects, uploads, and archives crash dump files during all of our CI jobs and official builds.
- On Linux, there is an utility called `createdump` (see [doc](https://github.com/dotnet/coreclr/blob/master/Documentation/botr/xplat-minidump-generation.md "doc")) that can be setup to generate core dumps when a managed app throws an unhandled exception or faults.
- Matching coreclr/corefx runtime bits from the crash. To get these, you should either:
  - Download the matching Jenkins archive onto your repro machine.
  - Check out the coreclr and corefx repositories at the appropriate commit and re-build the necessary portions.
  - You can also download the matching "symbols" nuget package from myget.org. There is a "Download Symbols" button in the myget UI for this purpose.
- lldb version 3.9. The SOS plugin (i.e. libsosplugin.so) provided is now built for lldb 3.9. In order to install lldb 3.9 just run the following commands:
```
~$ echo "deb http://llvm.org/apt/trusty/ llvm-toolchain-trusty-3.9 main" | sudo tee /etc/apt/sources.list.d/llvm.list
~$ wget -O - http://llvm.org/apt/llvm-snapshot.gpg.key | sudo apt-key add -
~$ sudo apt-get update
~$ sudo apt-get install lldb-3.9
```

Once you have everything listed above, you are ready to start debugging. You need to specify an extra parameter to lldb in order for it to correctly resolve the symbols for libcoreclr.so. Use a command like this:

```
lldb-3.9 -O "settings set target.exec-search-paths <runtime-path>" -o "plugin load <path-to-libsosplugin.so>" --core <core-file-path> <host-path>
```

- `<runtime-path>`: The path containing libcoreclr.so.dbg, as well as the rest of the runtime and framework assemblies.
- `<core-file-path>`: The path to the core dump you are attempting to debug.
- `<host-path>`: The path to the dotnet or corerun executable, potentially in the `<runtime-path>` folder.
- `<path-to-libsosplugin.so>`: The path to libsosplugin.so, should be in the `<runtime-path>` folder.

lldb should start debugging successfully at this point. You should see stacktraces with resolved symbols for libcoreclr.so. At this point, you can run `plugin load <libsosplugin.so-path>`, and begin using SOS commands, as above.

##### Example

```
lldb-3.9 -O "settings set target.exec-search-paths /home/parallels/Downloads/System.Drawing.Common.Tests/home/helixbot/dotnetbuild/work/2a74cf82-3018-4e08-9e9a-744bb492869e/Payload/shared/Microsoft.NETCore.App/9.9.9/" -o "plugin load /home/parallels/Downloads/System.Drawing.Common.Tests/home/helixbot/dotnetbuild/work/2a74cf82-3018-4e08-9e9a-744bb492869e/Payload/shared/Microsoft.NETCore.App/9.9.9/libsosplugin.so" --core /home/parallels/Downloads/System.Drawing.Common.Tests/home/helixbot/dotnetbuild/work/2a74cf82-3018-4e08-9e9a-744bb492869e/Work/f6414a62-9b41-4144-baed-756321e3e075/Unzip/core /home/parallels/Downloads/System.Drawing.Common.Tests/home/helixbot/dotnetbuild/work/2a74cf82-3018-4e08-9e9a-744bb492869e/Payload/shared/Microsoft.NETCore.App/9.9.9/dotnet
```

## Using Visual Studio Code

- Install [Visual Studio Code](https://code.visualstudio.com/)
- Install the [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
- Open the folder containing the source you want to debug in VS Code - i.e., if you are debugging a test failure in System.Net.Sockets, open `corefx/src/System.Net.Sockets`
- Open the debug window: `ctrl-shift-D` or click on the button on the left
- Click the gear button at the top to create a launch configuration, select `.NET Core` from the selection dropdown
- In the `.NET Core Launch (console)` configuration do the following
  - delete the `preLaunchTask` property
  - set `program` to the full path to `dotnet` in the bin directory.
    - something like `corefx/bin/testhost/netcoreapp-Linux-{Configuration}-{Architecture}`, plus the full path to your corefx directory.
  - set `cwd` to the test bin directory.
    - using the System.Net.Sockets example, it should be something like `corefx/bin/tests/System.Net.Sockets.Tests/netcoreapp-Linux-{Configuration}-{Architecture}`, plus the full path to your corefx directory.
  - set `args` to the command line arguments to pass to the test
    - something like: `[ "xunit.console.netcore.exe", "<test>.dll", "-notrait", .... ]`
    - to run a specific test, you can append something like: `[ "method", "System.Net.Sockets.Tests.{ClassName}.{TestMethodName}", ...]`
- Set a breakpoint and launch the debugger, inspecting variables and call stacks will now work
