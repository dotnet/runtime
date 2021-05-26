# MonoVM Diagnostics Tracing Component

## Summary

MonoVM includes support for EventPipe and DiagnosticServer components used to generate nettrace files including both runtime as well as custom `EventSource` events. It is possible to either use dynamic components (Android) or link statically (iOS) depending on build configuration. EventPipe will mainly be used during development/testing cycle, and should not be deployed or linked into builds passed to app store for verification and publication.

## Scenarios

.Net supports a number of different EventPipe scenarios using tools mainly from diagnostics repository. MonoVM include support for several of these scenarios, running tools like `dotnet-counters` and `dotnet-trace` to collect and analyze runtime performance data. Other things like requesting a core/gc dump or attaching profiler over EventPipe is currently not supported on MonoVM.

MonoVM runs on a variaty of platforms, and depending on platform capabilities MonoVM support different build configurations of EventPipe and DiagnosticServer. For desktop platforms (Windows, Linux, MacOS), MonoVM build DiagnosticServer using
`NamedPipes` (Windows) or `UnixDomainSockets` (Linux, MacOS) support. This is inline with CoreCLR build configuration of the DiagnosticServer, working in the same way.

On mobile platforms (Android/iOS) or other remote sandboxed environments, MonoVM DiagnosticServer component can be build using TCP/IP support to better handle remote targets. It is also handles the connect senario, runtime act as TCP/IP client connecting back to tooling as well as the listening scenario, runtime act as a TCP/IP listener waiting for tooling to connect. Depending on platform, allowed capabilities (some platforms won't allow listening on sockets) and tracing scenarios (startup tracing needs runtime connecting back to tooling), a combination of these scenarios will be used.

Existing diagnostic tooling only supports `NamedPipes`/`UnixDomainSockets`, so in order to reuse these tools transparently when targeting MonoVM running on mobile platforms, a new component have been implmenet in diagnostics repro, `dotnet-dsrouter`, https://github.com/dotnet/diagnostics/tree/main/src/Tools/dotnet-dsrouter. dsrouter represents the application running on a remote target locally to the diagnostic tools, routing local IPC traffic over to TCP/IP handled by MonoVM running on remote target. dsrouter implements 3 different modes, server-server (IPC server, TCP server), client-server (IPC client, TCP server) and server-client (IPC server, TCP client ) and depending on configuration all three modes can be used. dsrouter also improves the reversed connect runtime scenario to support more than one diagnostic tooling client at a time, as well as converting the reversed connect runtime scenario (used when tracing startup) into a normal direct connect scenario using dsrouter in server-server mode.

For more details around diagnostic scenarios, see:

https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters

https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace

https://docs.microsoft.com/en-us/dotnet/core/diagnostics/event-counter-perf

https://docs.microsoft.com/en-us/dotnet/core/diagnostics/debug-highcpu

## Building an application including diagnostic tracing support

Depending on platform, there are different recommended and supported ways to include diagnostic tracing support when building the application.

### Android

Android is build using dynamic component support, meaning that components are included as shared objects and runtime will try to load them from the same location as `libmonosgen-2.0.so`. If runtime fails to load component, it will be disabled, if it successfully load the component at runtime, it will be enabled and used. Enabling/disabling components is then a matter of incuding/excluding the needed shared object files in the APK (in same folder as `libmonosgen-2.0.so`), same runtime build can be used to support any combination of components.

If `AndroidAppBuilderTask` is used, there is a msbuild property, `RuntimeComponents` that can be used to include specific components in the generated application. By default its empty, meaning all components will be disabled, using a `*` will enabled all components and by specify individual components, only those will be enabled. Enabling tracing would look like this, `RuntimeComponents="diagnostics_tracing"`, more components can be enabled by separting them with `;`.

### iOS

iOS is build using static component support, meaning that components are included as static libraries that needs to be linked together with `libmonosgen-2.0.a` to produce final application. Static components comes in two flavours, the component library and a stub library. Linking the component library will enable the component in final application, while linking the stub library disables the component. Depeding on linked component flavours it is possible to create a build that enables specific components while disabling others. All components needs to be linked in (using component or stub library) or there will be unresolved symbols in `libmonosgen-2.0.a`.

If `AppleAppBuilderTask` is used, there is a msbuild property, `RuntimeComponents` that can be used to include specific components in the build application. By default its empty, meaning all components will be disabled, using a `*` will enabled all components and by specify individual components, only those will be enabled. Enabling tracing would look like this, `RuntimeComponents="diagnostics_tracing"`, more components can be enabled by separting them with `;`.

## Run an application including diagnostic tracing support

By default EventPipe/DiagnosticServer is controlled using the same set of environment variables used by CoreCLR. The single most important one is `DOTNET_DiagnosticPorts` used in order to setup runtime to connect or accept requests from diagnostic tooling. If not defined, diagnostic server won't startup and it will not be possible to interact with runtime using diagnostic tooling. Depending on platform, capabilities and scenarios, the content of `DOTNET_DiagnosticPorts` will look differently.

### Application running in simulator/emulator connecting over loopback interface

Starting up application using `DOTNET_DiagnosticPorts=127.0.0.1:9000,nosuspend` on iOS, or `DOTNET_DiagnosticPorts=10.0.2.2:9000,nosuspend` on Android, will connect to dsrouter listening on loopback port 9000 on local machine. Once runtime is connected, it is possible to connect diagnostic tools like dotnet-counters, dotnet-trace, towards dsrouter local IPC interface. In order to include startup events in EventPipe sessions, change `nosuspend` to `suspend` and runtime startup will wait for diagnostic tooling to connect before resuming.

If supported it is possible to push the TCP/IP listener over to the device and only run a TCP/IP client on the local machine connecting to the runtime listener. Using `DOTNET_DiagnosticPorts=127.0.0.1:9000,nosuspend,listen` will run a local listener binding to loopback interface. On Android, it is possible to setup adb port forwading while on iOS runtime can bind local machine loopback interface from simulator. dsrouter will be setup with a TCP/IP client when running in this scenario.

For more information on Android emulator networking and port forwarding:

https://developer.android.com/studio/run/emulator-networking
https://developer.android.com/studio/command-line/adb#forwardports

### Application running on device

The same envrionment variable is used when running on device, and if device is connected to development machine using usb, it is still possible to use loopback interface as described above, but it requires use of adb port forwarding on Android and an implementation of usbmux on iOS, like using mlaunch using --tcp-tunnel argument.

If loopback interface won't work, it is possible to use any interface reachable between development machine and device in `DOTNET_DiagnosticPorts` variable, just keep in mind that the the connection is unauthenticated and unencrypted.

### Application running single file based EventPipe session

If application shutdown runtime on close, it is possible to run a single file based EventPipe session using environment variables as described in https://docs.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe#trace-using-environment-variables. In .net6 an additional variable has been added, `COMPlus_EventPipeOutputStreaming`, making sure data is periodically flushed into the output file.

If application doesn't shutdown runtime on close, this mode won't work, since it requires rundown events, only emitted when closing session and flushing memory manager. If not runtime won't be correctly shutdown, generated nettrace file will be corrupt.

### Examples running application enabling diagnostic tracing

https://github.com/dotnet/diagnostics/blob/main/documentation/dotnet-trace-instructions.md

** TODO **: Example of commands run to launch a dotnet-counters viewing RuntimeEventSource counters.
** TODO **: Example of commands run to launch a dotnet-trace session collecting SampleProfiler and NativeEventSource events.
** TODO **: Example of commands run to launch a dotnet-trace session capturing startup events.
** TODO **: Example of reverse/connect using loopback and port forwarding on Android/iOS using adb and mlaunch.

## Analyze a nettrace file

Collected events retrieved over EventPipe sessions is stored in a nettrace file that can be analyzed using tooling like perfview, Speedscope or Chromium:

https://docs.microsoft.com/en-us/dotnet/core/diagnostics/debug-highcpu?tabs=windows#trace-generation
https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace#dotnet-trace-convert
https://github.com/dotnet/diagnostics/blob/main/documentation/tutorial/app_running_slow_highcpu.md

It is also possible to analyze the trace file using diagnostic client librarys from diagnostic repro:

https://github.com/dotnet/diagnostics/blob/main/documentation/diagnostics-client-library-instructions.md

Using the diagnostic client library gives full flexibilty to use data in nettrace file to extract any information contained in file. Using the library it is also possible to implement custom tooling, that will connect and do live analyzing of event stream retrieved from running application.