# System.Diagnostics.Process
Contains the source and tests of assembly System.Diagnostics.Process which includes a subset of members of the System.Diagnostics namespace that allow you to interact with local and remote system processes.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.diagnostics. The primary class is [`Process`](https://learn.microsoft.com/dotnet/api/system.diagnostics.process).

## Contribution Bar
- [x] [We only consider fixes to maintain or improve quality](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bar)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aopen+is%3Aissue+label%3Aarea-System.Diagnostics.Process+label%3A%22help+wanted%22) issues.

## Deployment
`System.Diagnostics.Process` is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.0.

## Temporary hang diagnostics

This investigation branch enables xUnit's verbose start/finish reporting on Windows, including
theory arguments, without changing test selection or parallelism. These messages are independent
of `XUNIT_HIDE_PASSING_OUTPUT_DIAGNOSTICS`.

For Windows x86 CoreCLR Helix runs, an eight-minute background watchdog in the xUnit test host
launches that runtime's `createdump.exe --full`. On Windows, this tool dumps its parent process;
it is not launched through a shell and no PID lookup or WER registry configuration is needed.
RemoteExecutor children do not arm the watchdog, and normal runner exit ends the background thread.
Local runs without `HELIX_WORKITEM_UPLOAD_ROOT` do not arm it.

The dump is written directly to `HELIX_WORKITEM_UPLOAD_ROOT` for Helix artifact collection.
Dump capture has a two-minute budget, followed by at most 30 seconds to terminate the dumper.
The test host then exits with code 124, including if dump capture fails, before Helix's
15-minute timeout. Capture failures are logged explicitly. These diagnostics are temporary
and are not a production fix or evidence that different timeout reports share a root cause.
