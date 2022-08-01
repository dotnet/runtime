// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly ResetGlobalFields
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {7BF531CB-7206-4C53-A779-930294C1158F}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03090000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi A
       extends [mscorlib]System.Object
{
  .field public static int32 s
  .method public hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.5
    IL_0002:  stsfld     int32 A::s
    IL_0007:  ret
  } // end of method A::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A::.ctor

} // end of class A

.class public auto ansi beforefieldinit Test_ResetGlobalFields
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       61 (0x3d)
    .maxstack  2
    .locals init (class A V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  newobj     instance void A::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldc.i4.0
    IL_0008:  stsfld     int32 A::s

    // call .cctor explicitly
    // global static field should be reset
    call void A::.cctor()

    IL_000d:  ldsfld     int32 A::s
    IL_0012:  ldc.i4.5
    IL_0013:  ceq
    IL_0015:  stloc.2
    IL_0016:  ldloc.2
    IL_0017:  brtrue.s   IL_002a

    IL_0019:  nop
    IL_001a:  ldstr      "FAIL: Global field wasn't reset"
    IL_001f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0024:  nop
    IL_0025:  ldc.i4.s   101
    IL_0027:  stloc.1
    IL_0028:  br.s       IL_003b

    IL_002a:  nop
    IL_002b:  ldstr      "PASS: Global field was reset"
    IL_0030:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0035:  nop
    IL_0036:  ldc.i4.s   100
    IL_0038:  stloc.1
    IL_0039:  br.s       IL_003b

    IL_003b:  ldloc.1
    IL_003c:  ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file ResetGlobalFields.res
