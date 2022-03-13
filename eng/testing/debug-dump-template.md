# Get the Helix payload

[Runfo](https://github.com/jaredpar/runfo/tree/master/runfo#runfo) helps get information about helix test runs and azure devops builds. We will use it to download the payload and symbols (recommended version 0.6.4 or later):
```sh
dotnet tool install --global runfo
dotnet tool update --global runfo
```
If prompted, open a new command prompt to pick up the updated PATH.
Windows:
```cmd
:: assumes %WOUTDIR% does not exist
runfo get-helix-payload -j %JOBID% -w %WORKITEM% -o %WOUTDIR%
```
Linux and macOS:
```sh
# assumes %LOUTDIR% does not exist
runfo get-helix-payload -j %JOBID% -w %WORKITEM% -o %LOUTDIR%
```

Any dump files published by helix will be downloaded.

> NOTE: if the helix job is an internal job, you need to pass down a [helix authentication token](https://helix.dot.net/Account/Tokens) using the `--helix-token` argument.

Now extract the files:

Windows:
```cmd
for /f %i in ('dir /s/b %WOUTDIR%\*zip') do tar -xf %i -C %WOUTDIR%
```
Linux and macOS
```sh
# obtain `unzip` if necessary; eg `sudo apt-get install unzip` or `sudo dnf install unzip`
find %LOUTDIR% -name '*zip' -exec unzip -d %LOUTDIR% {} \;
```

Now use the [dotnet-sos global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-sos) to install the SOS debugging extension.
```cmd
dotnet tool install --global dotnet-sos
dotnet tool update --global dotnet-sos
```
If prompted, open a new command prompt to pick up the updated PATH.
```cmd
:: Install only one: the one matching your dump
dotnet sos install --architecture Arm
dotnet sos install --architecture Arm64
dotnet sos install --architecture x86
dotnet sos install --architecture x64
```

# Now choose a section below based on your OS.

## If it's a Windows dump on Windows...

## ... and you want to debug with WinDbg

Install or update WinDbg if necessary ([external](https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/debugger-download-tools), [internal](https://osgwiki.com/wiki/Installing_WinDbg)). If you don't have a recent WinDbg you may have to do `.update sos`.

Open WinDbg and open the dump with `File>Open Dump`.
```
<win-path-to-dump>
```

```
!setclrpath %WOUTDIR%\shared\Microsoft.NETCore.App\%PRODUCTVERSION%
.sympath+ %WOUTDIR%\shared\Microsoft.NETCore.App\%PRODUCTVERSION%
```

Now you can use regular SOS commands like `!dumpstack`, `!pe`, etc.

## ... and you want to debug with Visual Studio

Currently this is not possible because mscordbi.dll is not signed.

## ... and you want to debug with dotnet-dump

Install the [dotnet-dump global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump).
```cmd
dotnet tool install --global dotnet-dump
dotnet tool update --global dotnet-dump
```
If prompted, open a new command prompt to pick up the updated PATH.
```cmd
dotnet-dump analyze ^
  <win-path-to-dump> ^
  --command "setclrpath %WOUTDIR%\shared\Microsoft.NETCore.App\%PRODUCTVERSION%" ^
  "setsymbolserver -directory %WOUTDIR%\shared\Microsoft.NETCore.App\%PRODUCTVERSION%"
```

Now you can use regular SOS commands like `dumpstack`, `pe`, etc.
If you are debugging a 32 bit dump using 64 bit dotnet, you will get an error `SOS does not support the current target architecture`. In that case replace dotnet-dump with the 32 bit version:
```cmd
dotnet tool uninstall --global dotnet-dump
"C:\Program Files (x86)\dotnet\dotnet.exe" tool install --global dotnet-dump
```
---
## If it's a Linux dump on Windows...

Download the [Cross DAC Binaries](https://dev.azure.com/dnceng/public/_apis/build/builds/%BUILDID%/artifacts?artifactName=CoreCLRCrossDacArtifacts&api-version=6.0&%24format=zip), open it and choose the flavor that matches the dump you are to debug, and copy those files to `%WOUTDIR%\shared\Microsoft.NETCore.App\%PRODUCTVERSION%`.

Now you can debug with WinDbg or `dotnet-dump` as if it was a Windows dump. See above.

---
## If it's a Linux dump on Linux...

## ... and you want to debug with LLDB

Install or update LLDB if necessary ([instructions here](https://github.com/dotnet/diagnostics/blob/master/documentation/lldb/linux-instructions.md))

Load the dump:
```sh
lldb --core <lin-path-to-dump> \
  %LOUTDIR%/shared/Microsoft.NETCore.App/%PRODUCTVERSION%/dotnet \
  -o "setclrpath %LOUTDIR%/shared/Microsoft.NETCore.App/%PRODUCTVERSION%" \
  -o "setsymbolserver -directory %LOUTDIR%/shared/Microsoft.NETCore.App/%PRODUCTVERSION%"
```

If you want to load native symbols
```gdb
loadsymbols
```

## ... and you want to debug with dotnet-dump

Install the [dotnet-dump global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump).
```sh
dotnet tool install --global dotnet-dump
dotnet tool update --global dotnet-dump
```
If prompted, open a new command prompt to pick up the updated PATH.
```sh
dotnet-dump analyze \
  <lin-path-to-dump> \
  --command "setclrpath %LOUTDIR%/shared/Microsoft.NETCore.App/%PRODUCTVERSION%" \
  "setsymbolserver -directory %LOUTDIR%/shared/Microsoft.NETCore.App/%PRODUCTVERSION%"
```

---
## If it's a macOS dump

Instructions for debugging dumps on macOS are essentially the same as [Linux](#If-it's-a-Linux-dump-on-Linux...) with one exception: `dotnet-dump` cannot analyze macOS system dumps yet: you must use `lldb` for those. As of .NET 6, createdump on macOS
will start generating native Mach-O core files. dotnet-dump and ClrMD are still being worked on to handle these dumps.

---
# Other Helpful Information

* [How to debug a Linux core dump with SOS](https://github.com/dotnet/diagnostics/blob/master/documentation/debugging-coredump.md)
