# AutoTrace:

> see: `src/vm/autotrace.h|cpp` for the code

AutoTrace is used to run automated testing of the Diagnostic Server based tracing and specifically
EventPipe.  The feature itself is enabled via the feature flag `FEATURE_AUTO_TRACE` in [clrfeatures.cmake](../../../../src/coreclr/clrfeatures.cmake)

## Mechanism:

AutoTrace injects a waitable event into the startup path of the runtime and waits on that event until
some number of Diagnostics IPC (see: Diagnostics IPC in the dotnet/diagnostics repo) connections have occurred.
The runtime then creates some number of processes using a supplied path that typically are Diagnostics IPC based tracers.
Once all the tracers have connected to the server, the event will be signaled and execution will continue as normal.

## Use:

Two environment variables dictate behavior:
- `COMPlus_AutoTrace_N_Tracers`: The number of tracers to create.  Should be a number in `[0,64]` where `0` will bypass the wait for attach.
- `COMPlus_AutoTrace_Command`: The path to the executable to be invoked.  Typically this will be a `run.sh|cmd` script.

> (NB: you should `cd` into the directory you intend to execute `COMPlus_AutoTrace_Command` from as the first line of the script.)

Once turned on, AutoTrace will run the specified command `COMPlus_AutoTrace_N_Tracers` times.
