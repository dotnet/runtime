// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .hash = (0E 33 6B E4 AA 53 F0 DC 70 EA E2 E0 A3 7E BB BC 53 7B 75 B4 )
  .ver 2:0:3600:0
}

.assembly extern Microsoft.VisualC
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .hash = (C4 73 BC B5 51 0F 9C B5 ED 4B 30 6E 4A D1 62 7F 2E 43 D6 5E )
  .ver 8:0:1200:0
}

.assembly b21220
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.module b21220.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003
.corflags 0x00000001

.method public static int32 main() cil managed
{
    .entrypoint
    .custom instance void [Microsoft.VisualC]Microsoft.VisualC.DecoratedNameAttribute::.ctor(string) = ( 01 00 0F 3F 6D 61 69 6E 40 40 24 24 48 59 4D 48 58 5A 00 00 )
    .maxstack  3
    .locals init (class C V_0, int32 V_1)

    newobj     instance void C::.ctor()
    stloc.0
    ldc.i4.s 100
    stloc.1

    .try
    {
        ldloc.0
        call       instance int32& C::retLocalByref()
        pop
        ldstr      "Failed: Expected C::retLocalByref() to throw an System.Security.VerificationException"
        call       void [System.Console]System.Console::WriteLine(string)
        ldc.i4.s 5
        stloc.1
        leave.s Exit
    }
    catch [mscorlib]System.Security.VerificationException
    {
        ldstr      "Passed"
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s Next
    }
    catch [mscorlib]System.Exception
    {
        ldstr      "Failed: Expected C::retLocalByref() to throw an System.Security.VerificationException"
        call       void [System.Console]System.Console::WriteLine(string)
        ldc.i4.s 5
        stloc.1
        leave.s Exit
    }
Next:
    .try
    {
        ldloc.0
        call       instance int32& C::retLocalByref2()
        pop
        ldstr      "Failed: Expected C::retLocalByref() to throw an System.Security.VerificationException"
        call       void [System.Console]System.Console::WriteLine(string)
        ldc.i4.s 5
        stloc.1
        leave.s Exit
    }
    catch [mscorlib]System.Security.VerificationException
    {
        ldstr      "Passed"
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s Exit
    }
    catch [mscorlib]System.Exception
    {
        ldstr      "Failed: Expected C::retLocalByref() to throw an System.Security.VerificationException"
        call       void [System.Console]System.Console::WriteLine(string)
        ldc.i4.s 5
        stloc.1
        leave.s Exit
    }

Exit:  
    ldloc.1
    call       void [System.Console]System.Console::WriteLine(int32)
    ldloc.1
    ret
} 

.class public auto ansi C extends [mscorlib]System.Object
{
    .field public int32 i

    .method public specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack  1

        ldarg.0
        call       instance void [mscorlib]System.Object::.ctor()
        ret
    } 

    .method public instance int32& retLocalByref() cil managed
    {
        .maxstack  4
        .locals init (int32 V_0)

        ldarg.0
        ldfld      int32 C::i
        ldc.i4.0
        bne.un        A
        ldarg.0
        ldflda     int32 C::i
        br         Exit
A: 
        ldloca     V_0
Exit:  
        ret
    }

    .method public instance int32& retLocalByref2() cil managed
    {
        .maxstack  4
        .locals init (int32 V_0)

        ldarg.0
        ldfld      int32 C::i
        ldc.i4.0
        bne.un        A
        ldloca     V_0
        br         Exit
A: 
        ldarg.0
        ldflda     int32 C::i
Exit:  
        ret
    }
}
