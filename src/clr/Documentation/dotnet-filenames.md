.NET Filename Encyclopedia
===

.NET has had many mysterious filenames over time. This document defines their purpose.

- coreclr.dll: The implementation of CoreCLR.
- clr.dll: The implemenation of the .NET Framework CLR since the .NET Framework 4.
- mscorwks.dll: The .NET Framework CLR implementation up until version 2/3.5. It was called "wks" (pronounced "works") because it originally contained the client or "workstation" GC. Up until the .NET Framework 2, there was another variant of the CLR that contained the "server" GC, called msworksvr.dll. In the .NET Framework 2 release, the workstation and server GC were merged together in a single implementation, in mscorwks.dll, while mscorsvr.dll was deprecated.
- mscorsvr.dll: See mscorwks.dll.
- mscordacwks: A variant of mscorwks.dll, used only/primarily while debugger. It contains the "DAC" version of the VM implementation.