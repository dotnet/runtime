
# Profiler Attach on CoreCLR

Starting with .Net Core 3 preview6 there is a new profiler attach mechanism for CoreCLR. The desktop .Net Framework has had profiler attach since v4, but we have not had profiler attach on .Net Core. The desktop implementation was very Windows-centric and we did not have a good way to offer a cross platform profiler attach mechanism. The recent diagnostics port work means there is now a cross platform communication channel for external processes to communicate with a running CoreCLR process, and this allowed us to finally offer profiler attach for CoreCLR.

## How do you attach a profiler to a running CoreCLR process?

***Disclaimer: the code in the dotnet/diagnostics repo referred to below is in prelease and is under active development. You should expect things to change before the official release.***

Attaching a profiler to a running CoreCLR process involves sending a message from an external process (the trigger process) on the diagnostics port telling the runtime which profiler to attach. We have a premade managed implementation over at the [Diagnostics repo](https://github.com/dotnet/diagnostics). The attach method is `DiagnosticHelpers.AttachProfiler` in the `Microsoft.Diagnostics.Tools.RuntimeClient` library, which will be shipped on NuGet once it is released. It takes five arguments:

1) `int processId`          - (Required) The process ID to attach to.
2) `uint attachTimeout`     - (Required) A timeout that informs the runtime how long to wait while attempting to attach. This does not impact the timeout of trying to send the attach message.
3) `Guid profilerGuid`      - (Required) The profiler's GUID to use when initializing.
4) `string profilerPath`    - (Required) The path to the profiler on disk.
5) `byte[] additionalData`  - (Optional) A data blob that will be passed to `ICorProfilerCallback3::InitializeForAttach` as `pvClientData`. 

This method returns a status HR following the usual convention, 0 (S_OK) means a profiler was successfully attached and any other value is an error indicating what went wrong.

## What if you can't run managed code in your trigger process?

If you are unable to run managed code as part of your trigger process, it is still possible to request a profiler attach. The spec for the diagnostics port is located [here](https://github.com/dotnet/diagnostics/blob/master/documentation/design-docs/ipc-protocol.md).

You will have to do the following (according to the above spec):
1) Open the appropriate channel - domain socket on Linux and a named pipe on Windows
2) Construct the payload with the appropriate command (Profiler) and command ID (AttachProfiler), plus all of the arguments listed above
3) Send the payload over the channel
4) Parse the response
