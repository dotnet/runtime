# WebAssembly Debugger

## Overview

The details of launching a Debugger session for a Blazor WebAssembly application is described [here](https://docs.microsoft.com/en-us/aspnet/core/blazor/debug?view=aspnetcore-6.0&tabs=visual-studio).

## Debugger Attributes
Web Assembly Debugger supports usage of following attributes:
- __System.Diagnostics.DebuggerHidden__

  Decorating a method - results:
  - Visual Studio Breakpoints: results in disabling all existing breakpoints in the method and no possibility to set new,enabled ones.
  - Stepping In/Over: results in stepping over the line with method call.
  - Call stack: method does not appear on the call stack, no access to method local variables is provided.

  Decorating a method with a Debugger.Break() call inside:
  - Running in the Debug mode: results in pausing the program on the line with the method call.
  - Stepping In/Over: results in an additional stepping need to proceed to the next line.<br><br>
- __System.Diagnostics.DebuggerDisplay__
- __System.Diagnostics.DebuggerTypeProxy__
- __System.Diagnostics.DebuggerBrowsable__ ([doc](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.debuggerbrowsableattribute?view=net-6.0))
   - Collapsed - displayed normally.
   - RootHidden:
      - Simple type - not displayed in the debugger window.
      - Collection / Array - the values of a collection are displayed in a flat view, using  the naming convention: *rootName[idx]*.

   - Never - not displayed in the debugger window.

