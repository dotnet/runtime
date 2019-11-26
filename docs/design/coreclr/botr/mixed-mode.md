# Mixed Mode Assemblies
## Introduction
Most interoperability between managed and native code uses P/Invokes, COM, or WinRT. Since P/Invokes are bound to native code at runtime, they’re susceptible to mistakes ranging from incorrect naming to subtle mistakes in signatures that cause stack corruption. COM can also be used to call from native to managed code, but it often requires registration and can add performance overhead. WinRT avoids those problems but isn’t available in all cases.

C++/CLI provides a different, compiler-verified approach to interoperability called mixed-mode assemblies (also sometimes referred to as It-Just-Works or IJW). Rather than requiring that developers do special declarations like P/Invokes, the C++ compiler automatically generates everything needed to transition to and from managed and native code. Additionally, the C++ compiler decides whether a given C++ method should be managed or native, so even within an assembly, transitions happen regularly and without developer intervention required.

## Calling Native Code
C++/CLI code may call into either native code in the same assembly or a different library. Calls to a different library generates P/Invokes similar to those that might be written by hand in C# (but because the C++ compiler is reading that library's headers, the P/Invoke isn't subject to developer error). However, calls to the same assembly work differently. While P/Invokes to different libraries specify the name of the library and the name of an export to call, P/Invokes to the same library have a null entry point and set an RVA – an address within the library to call. In metadata, that looks like:
```
MethodName: delete (060000EE)
Flags     : [Assem] [Static] [ReuseSlot] [PinvokeImpl] [HasSecurity]  (00006013)
RVA       : 0x0001332a
Pinvoke Map Data:
Entry point:
```
Calling these P/Invokes works the same as those that use named entry points with the exception of manually computing an address based on the module address and RVA instead of looking for an export.

## Calling Managed Code
While native->native calls and managed->native calls can be based on the address of native functions, native->managed calls cannot since the managed code is non-executable IL. To solve that, the compiler generates a lookup table that appears in the CIL metadata header as the ```.vtfixup``` table. ```Vtfixups``` in the library on disk map from an RVA to a managed method token. When the assembly is loaded, the CLR generates a native-callable marshaling stub for each method in the ```.vtfixup``` table that calls the corresponding managed method. It then replaces the tokens with the addresses of the stub methods. When native code goes to call a managed method, it calls indirectly via the new address in the ```.vtfixup``` table.

For example, if a native method in IjwLib.dll wants to call the managed Bar method with token 06000002, it emits:
```
call    IjwLib!Bar (1000112b)
```
At that address, it places a jump indirection:
```
jmp     dword ptr [IjwLib!_mep?Bar$$FYAXXZ (10010008)]
```
Where 10010008 matches a ```.vtfixup``` entry that looks like:
```
.vtfixup [1] int32 retainappdomain at D_00010008 // 06000002 (Bar’s token)
```
According to ECMA 335, ```vtfixups``` can contain multiple entries. However, the Microsoft Visual C++ compiler (MSVC) does not appear to generate those. Vtfixups also contain flags for whether a call should go to the current thread’s appdomain and whether the caller is unmanaged code. MSVC appears to always set those.

## Starting the Runtime
While a mixed mode assembly may be loaded into an already-running CLR, that isn’t always the case. It’s also possible for a mixed mode executable to start a process or for a running native process to load a mixed mode library and call into it. On .NET Framework (the only implementation that currently has this functionality), the native code’s ```Main``` or ```DllMain``` calls into mscoree.dll’s ```_CorDllMain``` function (which is resolved from a well-known location). When that happens, ```_CorDllMain``` is responsible for both starting the runtime and filling in vtfixups as described above.
