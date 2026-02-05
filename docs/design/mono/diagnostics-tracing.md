# MonoVM Diagnostics Tracing Component

## Summary

MonoVM includes support for EventPipe and DiagnosticServer components used to generate nettrace files including both runtime as well as custom `EventSource` events. It is possible to either use dynamic components (Android) or link statically (iOS) depending on build configuration. EventPipe will mainly be used during development/testing cycle and should not be deployed or linked into builds passed to app store for verification and publication.

## Scenarios

.NET supports several different EventPipe scenarios using tools mainly from diagnostics repository. MonoVM include support for several of these scenarios, running tools like `dotnet-counters` and `dotnet-trace` to collect and analyze runtime performance data. Other things like requesting a core dump or attaching profiler over EventPipe is currently not supported on MonoVM.

Due to differences between runtimes many of the NativeRuntimeEvents won't apply to MonoVM. Only a selected amount of NativeRuntimeEvents will initially be added to MonoVM. Current supported NativeRuntimeEvents can be viewed in MonoVM include file, https://github.com/dotnet/runtime/blob/main/src/mono/mono/eventpipe/gen-eventing-event-inc.lst. Since primary focus is EventPipe and mobile platforms (iOS/Android), ETW and LTTng providers have currently not been integrated/enabled for NativeRuntimeEvents on MonoVM.

MonoVM runs on a variety of platforms and depending on platform capabilities MonoVM support different build configurations of EventPipe and DiagnosticServer. For desktop platforms (Windows, Linux, macOS), MonoVM build DiagnosticServer using
`NamedPipes` (Windows) or `UnixDomainSockets` (Linux, macOS) support. This is in line with CoreCLR build configuration of the DiagnosticServer, working in the same way.

On mobile platforms (Android/iOS) or other remote sandboxed environments, MonoVM DiagnosticServer component can be build using TCP/IP support to better handle remote targets. It also handles the connect scenario (runtime act as TCP/IP client connecting back to tooling), as well as the listening scenario (runtime act as a TCP/IP listener waiting for tooling to connect). Depending on platform, allowed capabilities (some platforms won't allow listening on sockets) and tracing scenarios (startup tracing needs suspended runtime), a combination of these scenarios can be used.

Existing diagnostic tooling only supports `NamedPipes`/`UnixDomainSockets`, so in order to reuse these tools transparently when targeting MonoVM running on mobile platforms, a new component have been implemented in diagnostics repro, `dotnet-dsrouter`, https://github.com/dotnet/diagnostics/tree/main/src/Tools/dotnet-dsrouter. `dotnet-dsrouter` represents the application running on a remote target locally to the diagnostic tools, routing local IPC traffic over to TCP/IP handled by MonoVM running on remote target. `dotnet-dsrouter` implements 4 different modes, server-server (IPC server, TCP server), client-server (IPC client, TCP server), server-client (IPC server, TCP client) and client-client (IPC client, TCP client) and depending on configuration all four modes can be used with DiagnosticServer and diagnostic tooling. `dotnet-dsrouter` also improves the reversed connect runtime scenario to support more than one diagnostic tooling client at a time, as well as converting the reversed connect runtime scenario (used in scenarios like startup tracing) into a normal direct connect scenario using `dotnet-dsrouter` in client-server or server-server mode.

For more details around diagnostic scenarios, see:

https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-counters

https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-trace

https://learn.microsoft.com/dotnet/core/diagnostics/event-counter-perf

https://learn.microsoft.com/dotnet/core/diagnostics/debug-highcpu

## Building an application including diagnostic tracing support

Depending on platform, there are different recommended and supported ways to include diagnostic tracing support when building the application.

### Android

Android is built using dynamic component support, meaning that components are included as shared objects and runtime will try to load them from the same location as `libmonosgen-2.0.so`. If runtime fails to load component, it will be disabled, if it successfully loads the component at runtime, it will be enabled and used. Enabling/disabling components is then a matter of including/excluding the needed shared library files in the APK (in same folder as `libmonosgen-2.0.so`). The same runtime build can be used to support any combination of components.

Android runtime pack has the following runtime components included: `debugger`, `hot_reload`, `diagnostics_tracing`, `marshal-ilgen`.

For default scenarios, the dynamic versions should be used together with `libmonosgen-2.0.so`, but runtime pack also includes static versions of the components that can be used if runtime is built statically using `libmonosgen-2.0.a`. In case of static linking, using `libmono-component-*-stub-static.a` library will disable the component, using `libmono-component-*-static.a` will enable it.

```
libmono-component-diagnostics_tracing.so
libmono-component-diagnostics_tracing-static.a
libmono-component-diagnostics_tracing-stub-static.a
```

In order to enable the `diagnostic tracing` runtime component in your build, please take a look at [Enabling runtime components](#enabling-runtime-components) section.

### iOS

iOS is built using static component support, meaning that components are included as static libraries that needs to be linked together with `libmonosgen-2.0.a` to produce final application. Static components come in two flavors, the component library, and a stub library. Linking the component library will enable the component in final application, while linking the stub library disables the component. Depending on linked component flavors it is possible to create a build that enables specific components while disabling others. All components needs to be linked in (using component or stub library) or there will be unresolved symbols in `libmonosgen-2.0.a`.

iOS runtime pack has the following runtime components included: `debugger`, `hot_reload`, `diagnostics_tracing`, `marshal-ilgen`.

Using `libmono-component-*-stub-static.a` library will disable the component, using `libmono-component-*-static.a` will enable it.

```
libmono-component-diagnostics_tracing-static.a
libmono-component-diagnostics_tracing-stub-static.a
```

NOTE, running on iOS simulator offers some additional capabilities, so runtime pack for iOS includes shared as well as static library builds, like the Android use case described above.

In order to enable the `diagnostic tracing` runtime component in your build, please take a look at [Enabling runtime components](#enabling-runtime-components) section.

### Enabling runtime components

When using `AndroidAppBuilderTask` to target `Android`, or `AppleAppBuilderTask` to target `iOS` platforms, there is a MSBuild item: `RuntimeComponents` that can be used to include specific components in the generated application. By default, its empty, meaning all components will be disabled.
To enable a single component (eg: `diagnostic tracing`), by adding the following to your project file:
```xml
<ItemGroup>
    <RuntimeComponents Include="diagnostics_tracing" />
</ItemGroup>
```
will enable only that runtime component.

On the other hand, if it is desired to include all components, there are two options:
1. Manually, include all supported components manually via:
```xml
<ItemGroup>
    <RuntimeComponents Include="debugger" />
    <RuntimeComponents Include="hot_reload" />
    <RuntimeComponents Include="diagnostics_tracing" />
    <RuntimeComponents Include="marshal-ilgen" />
</ItemGroup>
```
2. Automatically, use provided MSBuild property that includes all the supported components for you, in the following way:
    - Import `AndroidBuild.props/targets` in your project file (the file can be found [here](../../../src/mono/msbuild/android/build/AndroidBuild.targets))
    - Set `UseAllRuntimeComponents` MSBuild property to `true` via:
        - By adding: `-p:UseAllRuntimeComponents=true` to your build command, or
        - By adding the following in your project file:
        ```xml
        <PropertyGroup>
            <UseAllRuntimeComponents>true</UseAllRuntimeComponents>
        </PropertyGroup>
        ```

## Install diagnostic client tooling

```sh
$ dotnet tool install -g dotnet-trace --add-source=https://aka.ms/dotnet-tools/index.json
$ dotnet tool install -g dotnet-counters --add-source=https://aka.ms/dotnet-tools/index.json
$ dotnet tool install -g dotnet-dsrouter --add-source=https://aka.ms/dotnet-tools/index.json
```

If tools have already been installed, they can all be updated to latest version using `update` keyword instead of `install` keyword.

NOTE, make sure version of all tools match.

## Run an application including diagnostic tracing support

By default, EventPipe/DiagnosticServer is controlled using the same set of environment variables used by CoreCLR. The single most important one is `DOTNET_DiagnosticPorts` used to setup runtime to connect or accept requests from diagnostic tooling. If not defined, diagnostic server won't startup and it will not be possible to interact with runtime using diagnostic tooling. Depending on platform, capabilities and scenarios, the content of `DOTNET_DiagnosticPorts` will look differently.

Prerequisites when running below diagnostic scenarios:

* Make sure diagnostics_tracing component is enabled when building/deploying application.
* If anything EventSource related is used, make sure EventSourceSupport is enabled when building application (or ILLinker can remove needed EventSource classes). If just running tracing, collecting native EventPipe events emitted by SampleProfiler, DotNetRuntime or MonoProfiler providers, EventSourceSupport can be enabled or disabled.
* Install needed diagnostic tooling on development machine.

### Application running diagnostics using default .NET 8 configuration

Starting in .NET 8, enhancements to `dotnet-dsrouter` supporting most of below described scenarios, using a smaller subset of available configurations using loopback interface on emulators/simulators and physical devices attached over usb. `dotnet-dsrouter` outputs detailed information on how to launch runtime as well as connect to the instance using diagnostic tooling.

Starting `dotnet-dsrouter` to trace an application running on iOS simulator could be done using the `ios-sim` profile:

```sh
$ dotnet-dsrouter ios-sim -i
WARNING: dotnet-dsrouter is a development tool not intended for production environments.

How to connect current dotnet-dsrouter pid=26600 with iOS simulator and diagnostics tooling.
Start an application on iOS simulator with ONE of the following environment variables set:
[Default Tracing]
DOTNET_DiagnosticPorts=127.0.0.1:9000,nosuspend,listen
[Startup Tracing]
DOTNET_DiagnosticPorts=127.0.0.1:9000,suspend,listen
Run diagnostic tool connecting application on iOS simulator through dotnet-dsrouter pid=26600:
dotnet-trace collect -p 26600
See https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dsrouter for additional details and examples.

info: dotnet-dsrouter-26600[0]
      Starting dotnet-dsrouter using pid=26600
info: dotnet-dsrouter-26600[0]
      Starting IPC server (dotnet-diagnostic-dsrouter-26600) <--> TCP client (127.0.0.1:9000) router.
```

Using `-i` outputs enough details on how to configure the `DOTNET_DiagnosticPorts` when launching the application depending on default or startup tracing needs as well as how to run diagnostic tooling against the specific `dotnet-dsrouter` instance.

For example, running default tracing together with above `dotnet-dsrouter` instance, set `DOTNET_DiagnosticPorts=127.0.0.1:9000,nosuspend,listen` environment variable for the launched application running on iOS simulator and then run diagnostic tooling using pid of running `dotnet-dsrouter`:

```sh
$ dotnet-trace collect -p 26600
```

Changing the environment variable for launched application to `DOTNET_DiagnosticPorts=127.0.0.1:9000,suspend,listen` will enable startup tracing using above `dotnet-dsrouter` instance.

.NET 8 version of `dotnet-dsrouter` supports the following profiles that could be used when running diagnostic tool against iOS/Android simulator/emulator/devices:

 * `ios-sim`
 * `ios`
 * `android-emu`
 * `android`

 Running profiles with `-i` gives detailed info on how to launch application and diagnostic tooling together with `dotnet-dsrouter`.

In case the default profiles described above are too limited, the following sections describes all low level configuration details and options needed to trace applications on iOS/Android using `dotnet-dsrouter`.

#### Application running diagnostics using custom configuration on simulator/emulator

Starting up application using `DOTNET_DiagnosticPorts=127.0.0.1:9000,nosuspend` on iOS, or `DOTNET_DiagnosticPorts=10.0.2.2:9000,nosuspend` on Android, will connect to `dotnet-dsrouter` listening on loopback port 9000 (can be any available port) on local machine. Once runtime is connected, it is possible to connect diagnostic tools like `dotnet-counters`, `dotnet-trace`, towards `dotnet-dsrouter` local IPC interface. To include startup events in EventPipe sessions, change `nosuspend` to `suspend` and runtime startup and wait for diagnostic tooling to connect before resuming.

If supported, it is possible to push the TCP/IP listener over to the device and only run a TCP/IP client on the local machine connecting to the runtime listener. Using `DOTNET_DiagnosticPorts=127.0.0.1:9000,nosuspend,listen` will run a local listener binding to loopback interface on simulator/emulator/device. On Android, it is possible to setup `adb` port forwarding while iOS runtime can bind local machine loopback interface directly from simulator. `dotnet-dsrouter` will be configured to use a TCP/IP client when running this scenario.

`dotnet-dsrouter` also includes an argument, `--forward-port`, that simplifies this scenario across simulator/emulator and device connected over usb.

On Android, `--forward-port` will automatically adapt to the supplied configuration and automatically use `adb forward` or `adb reverse` command to forward or reverse the port regardless of using emulator or device. This opens for the ability to always use loopback interface like `127.0.0.1` on Android emulator or device attached over usb. In order run `adb` commands, `dotnet-dsrouter` needs to find `adb` tool as part of `PATH` or setting `ANDROID_SDK_ROOT` pointing to an Android SDK install.

For more information on Android emulator networking and port forwarding:

https://developer.android.com/studio/run/emulator-networking

https://developer.android.com/studio/command-line/adb#forwardports

On IOS, `--forward-port` works in the scenario where DiagnosticServer runs in listening mode on device (connected over usb) using loopback interface.

Runtime configuration:

`DIAGNOSTIC_PORTS=127.0.0.1:9000,suspend|nosuspend,listen`

Use `suspend` keyword if runtime should wait for tooling to connect during startup, normally needed when analyzing startup.

Use `nosuspend` keyword if runtime do regular startup, not waiting for any diagnostic tooling to connect before proceeding regular startup work.

When running towards Android emulator or device (connected over usb):

```sh
$ dotnet-dsrouter server-client -ipcs ~/myport -tcpc 127.0.0.1:9000 --forward-port Android
```

When running towards iOS simulator:

```sh
$ dotnet-dsrouter server-client -ipcs ~/myport -tcpc 127.0.0.1:9000
```

When running towards iOS device (connected over usb):

```sh
$ dotnet-dsrouter server-client -ipcs ~/myport -tcpc 127.0.0.1:9000 --forward-port iOS
```

Run diagnostic tooling like this, regardless of `suspend|nosuspend`, `Android|iOS` or `simulator|emulator|device` scenarios are being used:

```sh
$ dotnet-trace collect --diagnostic-port ~/myport,connect
```

or

```sh
$ dotnet-counters monitor --diagnostic-port ~/myport,connect
```

Android and iOS SDK's have documented similar steps on how to setup profiling/tracing:

https://github.com/xamarin/xamarin-android/blob/main/Documentation/guides/tracing.md

https://github.com/xamarin/xamarin-macios/wiki/Profiling

#### Example using dotnet-counters using sample app on iOS simulator

`dotnet-dsrouter` needs to run using a compatible configuration depending on scenario. Either launch a new instance for every run or have a background instance running over several sessions using same configuration.

##### Default .NET 8 configuration:

Make sure the following is enabled in https://github.com/dotnet/runtime/blob/main/src/mono/sample/iOS/Makefile,

```Makefile
RUNTIME_COMPONENTS=diagnostics_tracing
DIAGNOSTIC_PORTS=127.0.0.1:9000,nosuspend,listen
```

```sh
$ dotnet-dsrouter ios-sim &
```

```sh
$ dotnet-counters monitor -p <dotnet-dsrouter pid>
```

##### Custom configuration:

Make sure the following is enabled in https://github.com/dotnet/runtime/blob/main/src/mono/sample/iOS/Makefile,

```Makefile
RUNTIME_COMPONENTS=diagnostics_tracing
DIAGNOSTIC_PORTS=127.0.0.1:9000,nosuspend
```

```sh
$ dotnet-dsrouter server-server -ipcs ~/myport -tcps 127.0.0.1:9000 &
```

```sh
cd src/mono/sample/iOS/
$ make run-sim
```

```sh
$ dotnet-counters monitor --diagnostic-port ~/myport,connect
```

#### Example using dotnet-counters using sample app on Android emulator

`dotnet-dsrouter` needs to run using a compatible configuration depending on scenario. Either launch a new instance for every run or have a background instance running over several sessions using same configuration.

##### Default .NET 8 configuration:

Make sure the following is enabled in https://github.com/dotnet/runtime/blob/main/src/mono/sample/Android/Makefile,

```Makefile
RUNTIME_COMPONENTS=diagnostics_tracing
DIAGNOSTIC_PORTS=10.0.2.2:9000,nosuspend,connect
```

```sh
$ dotnet-dsrouter android-emu &
```

```sh
cd src/mono/sample/Android/
$ make run
```

```sh
$ dotnet-counters monitor -p <dotnet-dsrouter pid>
```

##### Custom configuration:

Make sure the following is enabled in https://github.com/dotnet/runtime/blob/main/src/mono/sample/Android/Makefile,

```Makefile
RUNTIME_COMPONENTS=diagnostics_tracing
DIAGNOSTIC_PORTS=10.0.2.2:9000,nosuspend
```

```sh
$ dotnet-dsrouter server-server -ipcs ~/myport -tcps 10.0.2.2:9000 &
```

```sh
cd src/mono/sample/Android/
$ make run
```

```sh
$ dotnet-counters monitor --diagnostic-port ~/myport,connect
```

Using `adb` port forwarding it is possible to use `127.0.0.1:9000` in above scenario and adding `--forward-port Android` to `dotnet-dsrouter` launch arguments. That will automatically run needed `adb` commands. NOTE, `dotnet-dsrouter` needs to find `adb` tool for this to work, see above for more details.

#### Example using dotnet-trace startup tracing using sample app on iOS simulator

`dotnet-dsrouter` needs to run using a compatible configuration depending on scenario. Either launch a new instance for every run or have a background instance running over several sessions using same configuration.

##### Default .NET 8 configuration:

Make sure the following is enabled in https://github.com/dotnet/runtime/blob/main/src/mono/sample/iOS/Makefile,

```Makefile
RUNTIME_COMPONENTS=diagnostics_tracing
DIAGNOSTIC_PORTS=127.0.0.1:9000,suspend,listen
```

```sh
$ dotnet-dsrouter ios-sim &
```

```sh
$ dotnet-counters monitor -p <dotnet-dsrouter pid>
```

##### Custom configuration:

Make sure the following is enabled in https://github.com/dotnet/runtime/blob/main/src/mono/sample/iOS/Makefile,

```Makefile
RUNTIME_COMPONENTS=diagnostics_tracing
DIAGNOSTIC_PORTS=127.0.0.1:9000,suspend
```

```sh
$ dotnet-dsrouter client-server -tcpc ~/myport -tcps 127.0.0.1:9000 &
```

```sh
$ dotnet-trace collect --diagnostic-port ~/myport
```

```sh
cd src/mono/sample/iOS/
$ make run-sim
```

Since `dotnet-dsrouter` is capable to run several different modes, it is also possible to do startup tracing using server-server mode.

Make sure the following is enabled in https://github.com/dotnet/runtime/blob/main/src/mono/sample/iOS/Makefile,

```Makefile
RUNTIME_COMPONENTS=diagnostics_tracing
DIAGNOSTIC_PORTS=127.0.0.1:9000,suspend
```

```sh
$ dotnet-dsrouter server-server -ipcs ~/myport -tcps 127.0.0.1:9000 &
```

```sh
cd src/mono/sample/iOS/
$ make run-sim
```

```sh
$ dotnet-trace collect --diagnostic-port ~/myport,connect
```

#### Example using dotnet-trace startup tracing using sample app on Android emulator

`dotnet-dsrouter` needs to run using a compatible configuration depending on scenario. Either launch a new instance for every run or have a background instance running over several sessions using same configuration.

##### Default .NET 8 configuration:

Make sure the following is enabled in https://github.com/dotnet/runtime/blob/main/src/mono/sample/Android/Makefile,

```Makefile
RUNTIME_COMPONENTS=diagnostics_tracing
DIAGNOSTIC_PORTS=10.0.2.2:9000,suspend,connect
```

```sh
$ dotnet-dsrouter android-emu &
```

```sh
cd src/mono/sample/Android/
$ make run
```

```sh
$ dotnet-counters monitor -p <dotnet-dsrouter pid>
```

##### Custom configuration:

Make sure the following is enabled in https://github.com/dotnet/runtime/blob/main/src/mono/sample/Android/Makefile,

```Makefile
RUNTIME_COMPONENTS=diagnostics_tracing
DIAGNOSTIC_PORTS=10.0.2.2:9000,suspend
```

```sh
$ dotnet-dsrouter client-server -tcpc ~/myport -tcps 127.0.0.1:9000 &
```

```sh
$ dotnet-trace collect --diagnostic-port ~/myport
```

```sh
cd src/mono/sample/Android/
$ make run
```

Since `dotnet-dsrouter` is capable to run several different modes, it is also possible to do startup tracing using server-server mode.

Make sure the following is enabled in https://github.com/dotnet/runtime/blob/main/src/mono/sample/Android/Makefile,

```Makefile
RUNTIME_COMPONENTS=diagnostics_tracing
DIAGNOSTIC_PORTS=10.0.2.2:9000,suspend
```

```sh
$ dotnet-dsrouter server-server -ipcs ~/myport -tcps 10.0.2.2:9000 &
```

```sh
cd src/mono/sample/Android/
$ make run
```

```sh
$ dotnet-trace collect --diagnostic-port ~/myport,connect
```

Using `adb` port forwarding it is possible to use `127.0.0.1:9000` in above scenario and adding `--forward-port Android` to `dotnet-dsrouter` launch arguments. That will automatically run needed `adb` commands. NOTE, `dotnet-dsrouter` needs to find `adb` tool for this to work, see above for more details.

### Application running on device

The same environment variable is used when running on device, and if device is connected to development machine using usb, it is possible to use loopback interface as described above, but that requires use of `adb` port forwarding on Android and `usbmux` on iOS.

If loopback interface won't work, it is still possible to use any interface reachable between development machine and device in `DOTNET_DiagnosticPorts` variable, just keep in mind that the connection is unauthenticated and unencrypted.

Below scenarios uses loopback interface together with device connected to development machine over usb.

#### Android

`dotnet-dsrouter` needs to run using a compatible configuration depending on scenario. Either launch a new instance for every run or have a background instance running over several sessions using same configuration.

##### Default .NET 8 configuration:

Launch application using the following environment variable set, `DIAGNOSTIC_PORTS=127.0.0.1:9000,suspend,connect`

```sh
$ dotnet-dsrouter android &
```

Run application on device connected over usb.

```sh
$ dotnet-trace collect -p <dotnet-dsrouter pid>
```

##### Custom configuration:

Launch application using the following environment variable set, `DIAGNOSTIC_PORTS=127.0.0.1:9000,suspend`

```sh
$ dotnet-dsrouter server-server -ipcs ~/myport -tcps 127.0.0.1:9000 --forward-port Android &
```

Run application on device connected over usb.

```sh
$ dotnet-trace collect --diagnostic-port ~/myport,connect
```

The same scenario could be run pushing the TCP/IP listener over to device. Following changes needs to be done to above configuration:

`DIAGNOSTIC_PORTS=127.0.0.1:9000,suspend,listen`

```sh
$ dotnet-dsrouter server-client -ipcs ~/myport -tcpc 127.0.0.1:9000 --forward-port Android &
```

#### iOS

`dotnet-dsrouter` needs to run using a compatible configuration depending on scenario. Either launch a new instance for every run or have a background instance running over several sessions using same configuration.

##### Default .NET 8 configuration:

Launch application using the following environment variable set, `DIAGNOSTIC_PORTS=127.0.0.1:9000,suspend,listen`

```sh
$ dotnet-dsrouter ios &
```

Run application on device connected over usb.

```sh
$ dotnet-trace collect -p <dotnet-dsrouter pid>
```

##### Custom configuration:

Launch application using the following environment variable set, `DIAGNOSTIC_PORTS=*:9000,suspend,listen`

```sh
$ dotnet-dsrouter server-client -ipcs ~/myport -tcpc 127.0.0.1:9000 --forward-port iOS &
```

Run application on device connected over usb.

```sh
$ dotnet-trace collect --diagnostic-port ~/myport,connect
```

NOTE, iOS only support use of loopback interface when running DiagnosticServer in listening mode on device and using `dotnet-dsrouter` in server-client (IPC Server, TCP/IP client) with port forwarding.

### Application running single file based EventPipe session

If application supports controlled runtime shutdown, `mono_jit_cleanup` gets called before terminating process, it is possible to run a single file based EventPipe session using environment variables as described in https://learn.microsoft.com/dotnet/core/diagnostics/eventpipe#trace-using-environment-variables. In .NET 6 an additional variable has been added, `DOTNET_EventPipeOutputStreaming`, making sure data is periodically flushed into the output file.

If application doesn't support controlled runtime shutdown, this mode won't work, since it requires rundown events, only emitted when closing session and flushing memory manager. If application doesn't call `mono_jit_cleanup` before terminating, generated nettrace file will lack rundown events needed to produce callstacks including symbols.

Running using single file based EventPipe session will produce a file in working directory. Use platform specific tooling to extract file once application has terminated. Since file based EventPipe session doesn't use diagnostic server, there is no need to use `DOTNET_DiagnosticPorts` or running `dotnet-dsrouter`.

### Analyze JIT/Loader events during startup

Increasing the default log level in `dotnet-trace` for `Microsoft-Windows-DotNETRuntime` provider will include additional events in nettrace file giving more details around JIT and loader activities, like all loaded assemblies, loaded types, loaded/JIT:ed methods as well as timing and size metrics, all valuable information when analyzing things like startup performance, size of loaded/JIT:ed methods, time it takes to JIT all, subset or individual methods etc. To instruct `dotnet-trace` to only collect `Microsoft-Windows-DotNETRuntime` events during startup, use one of that startup tracing scenarios as described above, but add the following parameters to `dotnet-trace`,

`--clreventlevel verbose --providers Microsoft-Windows-DotNETRuntime`

`Prefview` have built in analyzers for JIT/Loader stats, so either load resulting nettrace file in `Perfview` or analyze file using custom `TraceEvent` parsers.

### Trace MonoVM profiler events during startup

Starting with .NET 8 the experimental profiler provider, `Microsoft-DotNETRuntimeMonoProfiler`, has been disabled by default. In order to enable it, the following needs to be added to MonoVM specific environment variable:

`MONO_DIAGNOSTICS=--diagnostic-mono-profiler=enable`

MonoVM comes with a EventPipe provider mapping most of low-level Mono profiler events into native EventPipe events thought `Microsoft-DotNETRuntimeMonoProfiler`. Mainly this provider exists to simplify transition from old MonoVM log profiler over to nettrace, but it also adds a couple of features available in MonoVM profiler.

Mono profiler includes the concept of method tracing and have support for method enter/leave events (method execution timing). This can be used when running in JIT/Interpreter mode (for AOT it needs to be passed to MonoVM AOT compiler). Enable enter/leave can produce a large set of events so there might be a risk hitting buffer manager size limits, temporary dropping events. This can be mitigated by either increasing buffer manager memory limit or reduce the number of instrumented methods. Since enter/leave needs instrumentation it must be enabled when a method is JIT:ed.

Method tracing can be controlled using a keyword in the MonoProfiler provider, but if an EventPipe session is not running when a method gets JIT:ed, it won't be instrumented and will not fire any enter/leave events. 0x20000000 will start to capture enter/leave events and 0x40000000000 can be used to enable instrumentation including all methods JIT:ed during lifetime of EventPipe session. To make sure all needed methods gets instrumented, runtime should be configured using startup profiling (as described above) and `dotnet-trace` should be run using the following provider configuration,

`--providers Microsoft-DotNETRuntimeMonoProfiler:0x40020000000;4`

To fully control the instrumentation (not depend on running EventPipe session), there is a MonoVM specific environment variable that can be set:

`MONO_DIAGNOSTICS=--diagnostic-mono-profiler-callspec=`

That will enable enter/leave profiling for all JIT:ed methods matching callspec. Callspec uses MonoVM callspec format:

| Keyword | Description |
|:----|:----|
| all | All assemblies |
| none | No assemblies |
| program | Entry point assembly |
| assembly | Specifies an assembly |
| M:Type:Method | Specifies a method |
| N:Namespace | Specifies a namespace |
| T:Type | Specifies a type |
| +EXPR | Includes expression |
| -EXPR | Excludes expression |

It is possible to combine include and exclude expressions using `,` as separator.

Trace all methods, except methods belonging to `System.Int32` type.

`MONO_DIAGNOSTICS=--diagnostic-mono-profiler-callspec=all,-T:System.Int32`

Trace all methods in `Program` type.

`MONO_DIAGNOSTICS=--diagnostic-mono-profiler-callspec=T:Program`

If no EventPipe session is running using MonoProfiler provider with method tracing keyword, no events will be emitted, even if running methods have been instrumented using `MONO_DIAGNOSTICS`.

When instrumenting methods using `MONO_DIAGNOSTICS`, it is possible to run `dotnet-trace` using the following provider configuration,

`--providers Microsoft-DotNETRuntimeMonoProfiler:0x20000000:4`

Since all methods matching callspec will be instrumented it is possible to capture enter/leave events only when needed at any time during application lifetime.

A way to effectively use this precise profiling is to first run with SampleProfiler provider, identifying hot paths worth additional investigation and then enable tracing using a matching callspec and trace execution of methods using MonoProfiler provider tracing keyword. It is of course possible to do enter/leave profiling during startup as well (using callspec or provider enabled instrumentation), just keep in mind that it can produce many events, especially in case when no callspec is in use.

NOTE, EventPipe technology comes with an overhead emitting events that will have impact on enter/leave measurements. If more precise instrumentation is needed it is recommended to implement a Mono profiler provider handling the enter/leave callbacks directly.

### Collect GC dumps on MonoVM .NET 8

Starting with .NET 8 MonoVM support GC dumps functionality using tools like `dotnet-gcdump`. No need to use `Microsoft-DotNETRuntimeMonoProfiler` or custom tooling to analyze GC dumps. For more information capturing GC dumps on MonoVM, see https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-gcdump.

### Collect GC dumps on MonoVM pre .NET 8

MonoVM EventPipe provider `Microsoft-DotNETRuntimeMonoProfiler`, includes several GC related events that can be used to track allocations, roots and other GC events as well as generating GC heap dumps. Heap dumps can be requested on demand using `dotnet-trace` using the following provider configuration,

`--providers Microsoft-DotNETRuntimeMonoProfiler:0x8900001:4`

This will trigger a heap dump including object references, object type info and GC events that can be used to detect when dump starts and completes (to know when to end EventPipe session). If `dotnet-trace` is used, there is no clear indication when full dump has been completed but looking at the stream size counter will indicate when no more data gets written into stream and session can be closed.

Taking different dumps at different time for the same application instance opens ability to diff dumps using custom tools to track GC memory increase/decrease and get information on what object types are responsible for increase/decrease between dumps.

It is also possible to get details about roots registration/deregistration, handle create/delete, GC allocation (including callstack, needs startup argument to be enabled), finalization of objects etc. All this can be combined with heap dumps to get a full trace of GC heap activity and ability to track individual allocations back to its origin callstack.

To track GC allocations with callstack, it needs to be enabled when starting up application using an environment variable:

`MONO_DIAGNOSTICS=--diagnostic-mono-profiler=alloc`

NOTE, this affects runtime performance since it will change the underlying allocator. It however won't emit any events until an EventPipe session has been created with keywords enabling allocation tracking,

`--providers Microsoft-DotNETRuntimeMonoProfiler:0x200000:4`

It is also possible to setup one EventPipe session running over longer periods of time, getting all requested GC events, including multiple heap dumps. A different EventPipe session can be used to trigger heap dumps on demand, but that session
won't get any additional events, just trigger a heap dump.

`--providers Microsoft-DotNETRuntimeMonoProfiler:0x800000:4`

Combining different sessions including different GC information opens up ability to track all GC allocations during a specific time period (to reduce size of captured data), while taking heap dumps into separate sessions, make it possible to do full analysis of GC memory increase/decrease tied allocation callstacks for individual object instances.

## Analyze a nettrace file

Collected events retrieved over EventPipe sessions is stored in a nettrace file that can be analyzed using tooling like PerfView, Speedscope, Chronium or Visual Studio:

https://learn.microsoft.com/dotnet/core/diagnostics/debug-highcpu?tabs=windows#trace-generation

https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-trace#dotnet-trace-convert

https://github.com/dotnet/diagnostics/blob/main/documentation/tutorial/app_running_slow_highcpu.md

It is also possible to analyze the trace file using diagnostic client libraries from diagnostic repro:

https://github.com/dotnet/diagnostics/blob/main/documentation/diagnostics-client-library-instructions.md

Using the diagnostic client library gives full flexibility to use data in nettrace to extract any information contained in file. Using the library, it is also possible to implement custom tooling, that will connect and do live analyzing of event stream retrieved directly from running application.

TraceEvent library, https://www.nuget.org/packages/Microsoft.Diagnostics.Tracing.TraceEvent/ can be used to implement custom nettrace parsers.

https://github.com/lateralusX/diagnostics-nettrace-samples includes a couple of custom tools (startup tracing, instrumented method execution, pre .NET 8 MonoVM GC heap dump analysis) analyzing nettrace files using TraceEvent library.

## Developing EventPipe/DiagnosticServer on MonoVM

** TODO **: EventPipe/DiagnosticServer library design.

** TODO **: How to add a new NativeRuntimeEvent.

** TODO **: How to add a new component API.