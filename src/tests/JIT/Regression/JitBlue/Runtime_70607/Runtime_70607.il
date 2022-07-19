// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// See Runtime_70607.cs.

//  Microsoft (R) .NET IL Disassembler.  Version 7.0.0-dev



// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:0:0:0
}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:0:0:0
}
.assembly Runtime_70607.dll
{
}
.module Runtime_70607.dll
// MVID: {AC28796A-6A85-4AEA-A696-61332CBEA0D5}
.custom instance void [System.Runtime]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 )
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000002204BB50000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Runtime_70607
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       46 (0x2e)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  call       int32 Runtime_70607::M(uint8)
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4     0xff
    IL_000d:  bne.un.s   IL_001c

    IL_000f:  ldstr      "PASS: Result was 255"
    IL_0014:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0019:  ldc.i4.s   100
    IL_001b:  ret

    IL_001c:  ldstr      "FAIL: Result was {0}"
    IL_0021:  ldloc.0
    IL_0022:  box        [System.Runtime]System.Int32
    IL_0027:  call       void [System.Console]System.Console::WriteLine(string,
                                                                        object)
    IL_002c:  ldloc.0
    IL_002d:  ret
  } // end of method Runtime_70607::Main

  .method public hidebysig static int32  M(uint8 arg0) cil managed noinlining
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.m1
    IL_0001:  starg      arg0
    IL_0005:  ldarg.0
    IL_0006:  ret
  } // end of method Runtime_70607::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Runtime_70607::.ctor

} // end of class Runtime_70607


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
