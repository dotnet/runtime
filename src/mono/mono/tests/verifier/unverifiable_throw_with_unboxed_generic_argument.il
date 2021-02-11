
//  Microsoft (R) .NET Framework IL Disassembler.  Version 2.0.50727.42
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly 'gtest-443'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module 'gtest-443.exe'
// MVID: {17E388B1-945D-458C-A7B1-23817E20CC50}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x02E80000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C::.ctor

//[IL]: Error: [y:\mono\mono\tmp\gtest.exe : C::M[T]][offset 0x00000005][found (unboxed) 'T'] Expected an ObjRef on the stack.

  .method private hidebysig static void  M<.ctor ([mscorlib]System.Exception) T>() cil managed
  {
    // Code size       23 (0x17)
    .maxstack  2
    .locals init (!!T V_0)
    .try
    {
      IL_0000:  call       !!0 [mscorlib]System.Activator::CreateInstance<!!0>()
      IL_0005:  throw

      IL_0006:  leave      IL_0016

    }  // end .try
    catch !!T 
    {
      IL_000b:  unbox.any  !!T
      IL_0010:  stloc.0
      IL_0011:  leave      IL_0016

    }  // end handler
    IL_0016:  ret
  } // end of method C::M

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  call       void C::M<class [mscorlib]System.ApplicationException>()
    IL_0005:  ldc.i4.0
    IL_0006:  ret
  } // end of method C::Main

} // end of class C


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
