# Debugging NativeAOT programs

The NativeAOT ahead of time compiler generates fully native executable files that can be debugged by native debuggers on your platform of choice (e.g. WinDbg or Visual Studio on Windows, and gdb or lldb on Unix-like systems).

The NativeAOT compiler generates information about line numbers, types, locals and parameters. The native debugger will let you inspect stack trace and variables, step into/over source lines, or set line breakpoints.

To debug managed exceptions, set a breakpoint on the `RhThrowEx` method - this method is called whenever a managed exception is thrown.

## Visual Studio-specific notes

You can launch a NativeAOT-compiled executable under the VS debugger by opening it in the Visual Studio IDE. In the `File` menu, choose `Open Project/Solution...` and navigate to the native executable. You can set breakpoints as needed. To start debugging the EXE, choose the `Start Debugging` option from the `Debug` menu.

To set a breakpoint that breaks whenever an exception is thrown, choose the `Breakpoints` option from the `Debug` -> `Windows` menu. In the new window, select `New` -> `Function breakpoint`. Specify `RhThrowEx` as the Function Name and leave the Language option at "All Languages" (do not select C#).

To see what exception was thrown, start debugging (`Debug` -> `Start Debugging` or `F5`), open the Watches window (`Debug` -> `Windows` -> `Watch`) and add following expression as one of the watches: `(S_P_CoreLib_System_Exception*)@rcx`. This leverages the fact that at the time `RhThrowEx` is called, the x64 CPU register RCX contains the thrown exception. You can also paste the expression into the `Immediate Window`; the syntax is the same as for watches.
