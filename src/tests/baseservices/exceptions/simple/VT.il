// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly VT
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module VT.dll
// MVID: {54DC34D8-2D7D-429C-93F5-6A77ED223377}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00E60000


// =============== CLASS MEMBERS DECLARATION ===================

.class public sequential ansi sealed beforefieldinit VT
       extends [mscorlib]System.ValueType
{
  .field public int32 x
  .field public float64 y
  .field public string z
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       35 (0x23)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.2
    IL_0003:  stfld      int32 VT::x
    IL_0008:  ldarg.0
    IL_0009:  ldc.r8     3.1400000000000001
    IL_0012:  stfld      float64 VT::y
    IL_0017:  ldarg.0
    IL_0018:  ldstr      "England expects that every man will do his duty."
    IL_001d:  stfld      string VT::z
    IL_0022:  newobj     instance void [mscorlib]System.NotSupportedException::.ctor()
    IL_0027:  throw
  } // end of method VT::.ctor

} // end of class VT


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file VT.res
