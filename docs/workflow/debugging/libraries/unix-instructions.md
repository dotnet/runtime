Debugging CoreFX on Unix
==========================

CoreFX can be debugged on unix using both lldb and visual studio code

## Using lldb and SOS

- Run the test using msbuild at least once with `/t:Test`.
- [Install version 3.9 of lldb](../coreclr/debugging.md#debugging-core-dumps-with-lldb) and launch lldb with dotnet as the process and arguments matching the arguments used when running the test through msbuild.
- Load the sos plugin using `plugin load libsosplugin.so`.
- Type `soshelp` to get help. You can now use all sos commands like `bpmd`.

You may need to supply a path to load SOS. It can be found next to libcoreclr.so. For example:
```
(lldb) plugin load libsosplugin.so
error: no such file
(lldb) image list libcoreclr.so
[  0] ..... /home/dan/dotnet/shared/Microsoft.NETCoreApp/2.0.4/libcoreclr.so
(lldb) plugin load /home/dan/dotnet/shared/Microsoft.NETCoreApp/2.0.4/libsosplugin.so
```

## Debugging core dumps with lldb

It is also possible to debug .NET crash dumps using lldb and SOS. In order to do this, you need all of the following:

- The crash dump file.
- On Linux, there is an utility called `createdump` (see [doc](../../../design/coreclr/botr/xplat-minidump-generation.md "doc")) that can be setup to generate core dumps when a managed app throws an unhandled exception or faults.'

There are instructions for installing lldb and SOS [here](https://github.com/dotnet/diagnostics/blob/master/documentation/sos.md).

Once you have everything listed above, you are ready to start debugging. You need to specify an extra parameter to lldb in order for it to correctly resolve the symbols for libcoreclr.so. Use a command like this:

```
lldb-3.9 -O "settings set target.exec-search-paths <runtime-path>" -o "plugin load <path-to-libsosplugin.so>" --core <core-file-path> <host-path>
```

- `<runtime-path>`: The path containing libcoreclr.so.dbg, as well as the rest of the runtime and framework assemblies.
- `<core-file-path>`: The path to the core dump you are attempting to debug.
- `<host-path>`: The path to the dotnet or corerun executable, potentially in the `<runtime-path>` folder.
- `<path-to-libsosplugin.so>`: The path to libsosplugin.so, should be in the `<runtime-path>` folder.

lldb should start debugging successfully at this point. You should see stacktraces with resolved symbols for libcoreclr.so. At this point, you can run `plugin load <libsosplugin.so-path>`, and begin using SOS commands, as above.

Also see this [link](https://github.com/dotnet/diagnostics/blob/master/documentation/debugging-coredump.md) in the diagnostics repo.

##### Example

```
lldb-3.9 -O "settings set target.exec-search-paths /home/parallels/Downloads/System.Drawing.Common.Tests/home/helixbot/dotnetbuild/work/2a74cf82-3018-4e08-9e9a-744bb492869e/Payload/shared/Microsoft.NETCore.App/$(ProductVersion)/" -o "plugin load /home/parallels/Downloads/System.Drawing.Common.Tests/home/helixbot/dotnetbuild/work/2a74cf82-3018-4e08-9e9a-744bb492869e/Payload/shared/Microsoft.NETCore.App/$(ProductVersion)/libsosplugin.so" --core /home/parallels/Downloads/System.Drawing.Common.Tests/home/helixbot/dotnetbuild/work/2a74cf82-3018-4e08-9e9a-744bb492869e/Work/f6414a62-9b41-4144-baed-756321e3e075/Unzip/core /home/parallels/Downloads/System.Drawing.Common.Tests/home/helixbot/dotnetbuild/work/2a74cf82-3018-4e08-9e9a-744bb492869e/Payload/shared/Microsoft.NETCore.App/$(ProductVersion)/dotnet
```
