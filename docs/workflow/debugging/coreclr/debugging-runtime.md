# Debugging CoreCLR

* [Debugging CoreCLR on Windows](#debugging-coreclr-on-windows)
  * [Using Visual Studio](#using-visual-studio)
    * [Using Visual Studio Open Folder with CMake](#using-visual-studio-open-folder-with-cmake)
  * [Using Visual Studio Code](#using-visual-studio-code)
  * [Using SOS with Windbg or Cdb on Windows](#using-sos-with-windbg-or-cdb-on-windows)
* [Debugging CoreCLR on Linux and macOS](#debugging-coreclr-on-linux-and-macos)
  * [SOS on Unix](#sos-on-unix)
  * [Debugging CoreCLR with lldb](#debugging-coreclr-with-lldb)
    * [Disabling Managed Attach/Debugging](#disabling-managed-attachdebugging)
  * [Debugging core dumps with lldb](#debugging-core-dumps-with-lldb)
* [Debugging AOT compilers](#debugging-aot-compilers)
* [Debugging Managed Code](#debugging-managed-code)
  * [Using Visual Studio Code for Managed Code](#using-visual-studio-code-for-managed-code)
  * [Using Visual Studio for Managed Code](#using-visual-studio-for-managed-code)

These instructions will lead you through debugging CoreCLR.

As an initial note, SOS (the main plugin to aid with CoreCLR debugging) no longer resides here. For more information on how to get it, installing it, and how to use it, check it out in its new home, the [diagnostics repo](https://github.com/dotnet/diagnostics).

## Debugging CoreCLR on Windows

In order to start debugging CoreCLR, it is highly recommended to at least build the _clr_ subset under the _Debug_ configuration, in order to generate the necessary artifact files for the best debugging experience:

```cmd
.\build.cmd -s clr -c Debug
```

Note that you can omit the `-c Debug` flag, since it's the default one when none other is specified. We are leaving it here in this doc for the sake of clarity.

If for some reason `System.Private.CoreLib.dll` is missing, you can rebuild it with the following command, instead of having to go through the whole build again:

```cmd
.\build.cmd -s clr.corelib+clr.nativecorelib -c Debug
```

**NOTE**: When debugging with _CORE\_LIBRARIES_, the `libs` subset must also be built prior to attempting any debugging.

### Using Visual Studio

Visual Studio's capabilities as a full IDE provide a lot of help making the runtime debugging more amiable.

1. Open the CoreCLR solution _(coreclr.sln)_ in Visual Studio.
   * _Method 1_: Use the build scripts to open the solution:
      1. Run `.\build.cmd -vs coreclr.sln -a <architecture> -c <configuration>`. This will create and launch the CoreCLR solution in VS for the specified architecture and configuration. By default, this will be `x64 Debug`.
   * _Method 2_: Manually build and open the solution:
      1. Perform a build of the repo with the `-msbuild` flag.
      2. Open solution `path\to\runtime\artifacts\obj\coreclr\windows.<architecture>.<configuration>\ide\CoreCLR.sln` in Visual Studio. As in the previous method, the architecture and configuration by default are `x64` and `Debug`, unless explicitly stated otherwise.
2. Right-click the **INSTALL** project and choose `Set as StartUp Project`.
3. Bring up the properties page for the **INSTALL** project.
4. Select _Configuration Properties -> Debugging_ from the left side tree control.
5. Set `Command=$(SolutionDir)\..\..\..\..\bin\coreclr\windows.$(Platform).$(Configuration)\corerun.exe`. This points to the folder where the built runtime binaries are present.
6. Set `Command Arguments=<managed app you wish to run>` (e.g. HelloWorld.dll).
7. Set `Working Directory=$(SolutionDir)\..\..\..\..\bin\coreclr\windows.$(Platform).$(Configuration)`. This points to the folder containing CoreCLR binaries.
8. Set `Environment=CORE_LIBRARIES=$(SolutionDir)\..\..\..\..\bin\runtime\<target-framework>-windows-$(Configuration)-$(Platform)`, where '\<target-framework\>' is the target framework of current branch; for example `net6.0`, `net7.0` or `net8.0`. A few notes on this step:

* This points to the folder containing core libraries except `System.Private.CoreLib`.
* This step can be skipped if you are debugging CLR tests that reference only `System.Private.CoreLib`. Otherwise, it's required to debug a real-world application that references anything else, including `System.Runtime`.

9. Right-click the **INSTALL** project and choose `Build`. This will load necessary information from _CMake_ to Visual Studio.
10. Press F11 to start debugging at `wmain` in _corerun_, or set a breakpoint in source and press F5 to run to it. As an example, set a breakpoint for the `EEStartup()` function in `ceemain.cpp` to break into CoreCLR startup.

Steps 1-9 only need to be done once as long as there's been no changes to the CMake files in the repository. Afterwards, step 10 can be repeated whenever you want to start debugging. As of now, it is highly recommended to use Visual Studio 2022 or Visual Studio 2019.

#### Using Visual Studio Open Folder with CMake

1. Open the _dotnet/runtime_ repository in Visual Studio using the _open folder_ feature. When opening the repository root, Visual Studio will prompt about finding _CMake_ files. Select `src\coreclr\CMakeList.txt` as the CMake workspace.
2. Set the `corerun` project as startup project. When using the folder view instead of the CMake targets view, right click `coreclr\hosts\corerun\CMakeLists.txt` to set as startup project, or select it from debug target dropdown of Visual Studio.
3. Right click the `corerun` project and open the `Debug` configuration. You can also click on _Debug -> Debug and Launch Configuration_ from the Visual Studio main bar menu.
4. In the opened `launch.vs.json`, set following properties to the configuration of `corerun`:

```json
    {
      "type": "default",
      "project": "CMakeLists.txt",
      "projectTarget": "corerun.exe (hosts\\corerun\\corerun.exe)",
      "name": "corerun.exe (hosts\\corerun\\corerun.exe)",
      "environment": [
        {
          "name": "CORE_ROOT",
          "value": "${cmake.installRoot}"
        },
        {
          "name": "CORE_LIBRARIES",
          // for example net8.0-windows-Debug-x64
          "value": "${cmake.installRoot}\\..\\..\\runtime\\<tfm>-windows-<configuration>-<arch>\\"
        }
      ],
      "args": [
        // path to a managed application to debug
        // remember to use double backslashes (\\)
        "HelloWorld.dll"
      ]
    }
```

**NOTE**: For Visual Studio 17.3, changing the location of launched executable doesn't work, so the `CORE_ROOT` is necessary.

5. Right click the CoreCLR project or `coreclr\CMakeLists.txt` in the folder view, and then invoke the _Install_ command.
6. Press F10 or F11 to start debugging at main, or set a breakpoint and press F5.

Whenever you make changes to the CoreCLR source code, don't forget to invoke the _Install_ command again to have them set in place.

### Using Visual Studio Code

It will be very nice to be able to achieve all this using Visual Studio Code as well, since it's the editor of choice for lots of developers.

Visual Studio Code instructions coming soon!

### Using SOS with Windbg or Cdb on Windows

Under normal circumstances, SOS usually comes shipped with Windbg, so no additional installation is required. However, if this is not the case for you, you want to use another version, or any other circumstance that requires you to install it separately/additionally, here are two links with useful information on how to get it set up:

* The official [Microsoft docs on SOS](https://docs.microsoft.com/dotnet/core/diagnostics/dotnet-sos).
* The instructions at the [diagnostics repo](https://github.com/dotnet/diagnostics/blob/master/documentation/installing-sos-windows-instructions.md).

For more information on SOS commands click [here](https://github.com/dotnet/diagnostics/blob/master/documentation/sos-debugging-extension-windows.md).

## Debugging CoreCLR on Linux and macOS

Very similarly to Windows, Linux and macOS also require to have at least the _clr_ subset built prior to attempting to debug, most preferably under the _Debug_ configuration:

```bash
./build.sh -s clr -c Debug
```

Note that you can omit the `-c Debug` flag, since it's the default one when none other is specified. We are leaving it here in this doc for the sake of clarity.

If for some reason `System.Private.CoreLib.dll` is missing, you can rebuild it with the following command, instead of having to go through the whole build again:

```bash
./build.sh -s clr.corelib+clr.nativecorelib -c Debug
```

**NOTE**: When debugging with _CORE\_LIBRARIES_, the `libs` subset must also be built prior to attempting any debugging.

### SOS on Unix

For Linux and macOS, you have to install SOS by yourself, as opposed to Windows' Windbg. The instructions are very similar however, and you can find them on these two links:

* The official [Microsoft docs on SOS](https://docs.microsoft.com/dotnet/core/diagnostics/dotnet-sos).
* The instructions at the [diagnostics repo](https://github.com/dotnet/diagnostics/blob/master/documentation/installing-sos-instructions.md).

It might also be the case that you would need the latest changes in SOS, or you're working with a not-officially-supported scenario that actually works. The most common occurrence of this scenario is when using macOS Arm64. In this case, you have to build SOS from the diagnostics repo (linked above). Once you have it done, then simply load it to your `lldb`. More details in the following section.

### Debugging CoreCLR with lldb

**NOTE**: Only `lldb` is supported to use with SOS. You can also use `gdb`, `cgdb`, or other debuggers, but you might not have access to SOS.

1. Perform a build of the _clr_ subset of the runtime repo.
2. Start lldb passing `corerun`, the app to run (e.g. `HelloWorld.dll`), and any arguments this app might need: `lldb /path/to/corerun /path/to/app.dll <app args go here>`
3. If you're using the installed version of SOS, you can skip this step. If you built SOS manually, you have to load it before starting the debugging session: `plugin load /path/to/built/sos/libsosplugin.so`. Note that `.so` is for Linux, and `.dylib` is for macOS. You can find more information in the diagnostics repo [private sos build doc](https://github.com/dotnet/diagnostics/blob/main/documentation/using-sos-private-build.md).
4. Launch program: `process launch -s`
5. To stop breaks on _SIGUSR1_ signals used by the runtime run the following command: `process handle -s false SIGUSR1`
6. Set a breakpoint where CoreCLR is initialized, as it's the most stable point to begin debugging: `breakpoint set -n coreclr_execute_assembly`.
7. Get to that point by issuing `process continue` after setting the breakpoint.
8. Now, you're ready to begin your debugging session. You can set breakpoints or run SOS commands like `clrstack` or `sos VerifyHeap`.  Note that SOS command names are case sensitive.

#### Disabling Managed Attach/Debugging

The `DOTNET_EnableDiagnostics` _environment variable_ can be used to disable managed debugging. This prevents the various OS artifacts used for debugging, such as named pipes and semaphores on Linux and macOS, from being created.

```bash
export DOTNET_EnableDiagnostics=0
```

### Debugging core dumps with lldb

Our friends at the diagnostics repo have a very detailed guide on core dumps debugging [here in their repo](https://github.com/dotnet/diagnostics/blob/master/documentation/debugging-coredump.md).

## Debugging AOT compilers

Debugging AOT compilers is described in [its related document](debugging-aot-compilers.md).

## Debugging Managed Code

Native C++ code is not everything in our runtime. Nowadays, there are lots of stuff to debug that stay in the higher C# managed code level.

### Using Visual Studio Code for Managed Code

* Install the [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp).
* Open the folder containing the source you want to debug in VS Code.
* Open the debug window: `ctrl-shift-D`/`cmd-shift-D` or click on the button on the left.
* Click the gear button at the top to create a launch configuration, and select `.NET 5+ and .NET Core` from the selection dropdown.
* It will create a `launch.json` file, where you can configure what and how you want to debug it. Here is a basic template on how to fill it:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "My Configuration", // Any identifiable name you might like.
      "type": "coreclr", // We want to debug a CoreCLR app.
      "request": "launch", // Start the app with the debugger attached.
      "program": "/path/to/corerun", // Point to your 'corerun', in order to run the app using your build.
      "args": ["app-to-debug.dll", "app arg1", "app arg2"], // First argument is your app, second and on are the app's arguments.
      "cwd": "/path/to/app-to-debug", // Can be anywhere. For simplicity, choose where your app is stationed. Otherwise, you have to adjust paths in the other parameters.
      "stopAtEntry": true, // This can be either. Keeping it to 'true' allows you to see when the debugger is ready.
      "console": "internalConsole", // Use VSCode's internal console instead of launching more terminals.
      "justMyCode": false, // Be able to debug into native assemblies.
      "enableStepFiltering": false, // Be able to debug into class initializations, field accessors, etc.
    }
  ]
}
```

* Set a breakpoint and launch the debugger, inspecting variables and call stacks will now work

### Using Visual Studio for Managed Code

* Use _File -> Open Project_ (not open file) and select the binary you want to use as your host (typically _dotnet.exe_ or _corerun.exe_).
* Open the project properties for the new project that was just created and set the following:
  * _Arguments_: Make this match whatever arguments you would have used at the command-line. For example if you would have run `dotnet.exe exec Foo.dll`, then set `arguments = "exec Foo.dll"` (**NOTE**: Make sure you use `dotnet exec` instead of `dotnet run` because the run verb command is implemented to launch the app in a child process, and the debugger won't be attached to that child process).
  * _Working Directory_: Make this match whatever you would have used on the command-line.
  * _Debugger Type_: Set this to `Managed (.NET Core, .NET 5+)`. If you're going to debug the native C++ code, then you would select `Native Only` instead.
  * _Environment_: Add any environment variables you would have added at the command-line. You may also consider adding `DOTNET_ZapDisable=1` and `DOTNET_ReadyToRun=0`, which disable NGEN and R2R pre-compilation respectively, and allow the JIT to create debuggable code. This will give you a higher quality C# debugging experience inside the runtime framework assemblies, at the cost of somewhat lower app performance.
* For managed debugging, there are some additional settings in _Debug -> Options_, _Debugging -> General_ that might be useful:
  * Uncheck `Just My Code`. This will allow you debug into the framework libraries.
  * Check `Enable .NET Framework Source Stepping`. This will configure the debugger to download symbols and source automatically for runtime framework binaries. If you built the framework yourself, then you can omit this step without any problems.
  * Check `Suppress JIT optimzation on module load`. This tells the debugger to tell the .NET runtime JIT to generate debuggable code even for modules that may not have been compiled in a `Debug` configuration by the C# compiler. This code is slower, but it provides much higher fidelity breakpoints, stepping, and local variable access. It is the same difference you see when debugging .NET apps in the `Debug` project configuration vs the `Release` project configuration.

#### Resolving Signature Validation Errors in Visual Studio

Starting with Visual Studio 2022 version 17.5, Visual Studio will validate that the debugging libraries that shipped with the .NET Runtime are correctly signed before loading them. If they are unsigned, Visual Studio will show an error like:

> Unable to attach to CoreCLR. Signature validation failed for a .NET Runtime Debugger library because the file is unsigned.
>
> This error is expected if you are working with non-official releases of .NET (example: daily builds from https://github.com/dotnet/installer). See https://aka.ms/vs/unsigned-dotnet-debugger-lib for more information.

If the target process is using a .NET Runtime that is either from a daily build, or one that you built on your own computer, this error will show up. **NOTE**: This error should never happen for official builds of the .NET Runtime from Microsoft. So don’t disable the validation if you expect to be using a .NET Runtime supported by Microsoft.

There are three ways to configure Visual Studio to disable signature validation:
1.	The [`DOTNET_ROOT` environment variable](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_root-dotnet_rootx86): if Visual Studio is started from a command prompt where `DOTNET_ROOT` is set, it will ignore unsigned .NET runtime debugger libraries which are under the `DOTNET_ROOT` directory.
2.	The `VSDebugger_ValidateDotnetDebugLibSignatures` environment variable: If you want to temporarily disable signature validation, run `set VSDebugger_ValidateDotnetDebugLibSignatures=0` in a command prompt, and start Visual Studio (devenv.exe) from this command prompt.
3.	Set the `ValidateDotnetDebugLibSignatures` registry key: To disable signature validation on a more permanent basis, you can set the VS registry key to turn it off. To do so, open a Developer Command Prompt, and run `Common7\IDE\VsRegEdit.exe set local HKCU Debugger\EngineSwitches ValidateDotnetDebugLibSignatures dword 0`
