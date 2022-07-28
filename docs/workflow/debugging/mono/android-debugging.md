# Debugging Android runtime issues

## Enable verbose runtime logging

If a `net6.0-android` (or later) C# project is available, adding the following:

```xml
 <ItemGroup>
   <AndroidEnvironment Include="AndroidEnv.txt" />
 </ItemGroup>
```

with `AndroidEnv.txt`:

```
debug.mono.log=mono_log_level=debug,mono_log_mask=all
MONO_SDB_ENV_OPTIONS=loglevel=10    # only needed if you're debugging the managed debugger
```

Will enable additional Mono runtime logging in the adb log.  This is often
enough to diagnose issues such as missing assemblies or other loader problems.

## Managed Debugging

Should work from Visual Studio (Windows)

## Native debugging

Install Android Studio.

Download the symbols nupkg corresponding to the runtime pack that is used by
the app.  The runtime pack for the android workload is in a folder like
`${DOTNET_ROOT}/packs/Microsoft.NETCore.App.Runtime.Mono.android-x86/6.0.0-rc.1.21451.13`

The symbols are in a package called
`Microsoft.NETCore.App.Runtime.Mono.android-x86.6.0.0-rc.1.21451.13.symbols.nupkg`
uploaded to (FIXME: where does the symbols nuget go?).  Extract it to some folder using `unzip` and in the
`runtimes/android-x86/native/` folder rename the `*.so.dbg` files to `*.so.so`
(we will need to add the symbols files to Android Studio, but its file picker
only shows `*.so` extensions)


1. Build the APK normally using `dotnet build`, (this will produce a `AppName-Signed.apk` in the output folder)
2. Start an Android emultator
3. Install the app on the emulator using `dotnet build -t:Install`
2. Open the APK in Android Studio with "Profile or Debug APK"
3. In the "Project" viewer, choose the "cpp" folder, then `libmonosgen-2.0.so` then double-click `libmonosgen-2.0.so` inside the `libmonosgen-2.0.so` folder.
4. In the "Debug Symbols" view click "Add", navigate to the extracted runtime symbols nuget and select `libmonosgen-2.0.so.so`
5. In the "Path Mappings" section, select the toplevel folder and add a Local
   Path to a git checkout of the `release/6.0` tree corresponding to the nuget.  Click Apply Changes.
6. Start an emulator or connect to a device
7. On the menu bar select  "Run > Edit Configurations..." and on the "Debugger" tab make sure the "Debug Type" is something other than "Java Only"
8. Start debugging.
9. You should now have function names, local variables, as well as stepping through the runtime C code.

Since you're debugging an optimized release build, it is likely the debugger will not be able to materialize every local variable.

## Native debugging using a local debug build of Mono

Build the runtime for your android architecture: `ANDROID_NDK_ROOT=<path_to_android_ndk> ./build.sh --os android --arch x86 -c Debug`. See the instructions for [Testing Android](../../testing/libraries/testing-android.md) for details.


In the source code for the C# project, add the following to the .csproj (replacing `<RUNTIME_GIT_ROOT>` by the appropriate location):

```
  <Target Name="UpdateRuntimePack"
            AfterTargets="ResolveFrameworkReferences">
      <ItemGroup>
        <ResolvedRuntimePack PackageDirectory="<RUNTIME_GIT_ROOT>/artifacts/bin/microsoft.netcore.app.runtime.android-x86/Debug"
                             Condition="'%(ResolvedRuntimePack.FrameworkName)' == 'Microsoft.NETCore.App'" />
      </ItemGroup>
  </Target>
```

Then rebuild and reinstall the project, open the apk in Android Studio, and debug.  The
runtime native libraries will be stripped, so to make use of debug symbols, you
will need to follow the steps above (rename `*.so.dbg` in the artifacts to
`*.so.so` and add them to the APK project in Android Studio)

## Native and managed debugging or debugging the managed debugger

This workflow is useful to look for issues in the debugger itself, or to debug using a mixture of C and C# debugging.

Install [sdb](https://github.com/mono/sdb).

Start `sdb` and set it to listen  `listen 127.0.0.1 5000` (the port number is up to you).

Run the following `adb` command to set Mono apps to connect to a debugger on startup:

```
$ adb shell setprop debug.mono.extra "debug=10.0.2.2:5000,loglevel=10"
```

(`loglevel=10` will produce debugger protocol messages in the `adb` log.  If
you're not debugging the debugger it can be omitted.  For other debugger
options see [`print_usage()`](https://github.com/dotnet/runtime/blob/main/src/mono/mono/component/debugger-agent.c#L573) in `src/mono/mono/component/debugger-agent.c`)

Now launch the app from Android Studio. It should run and connect to the debugger.

