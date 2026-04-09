// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }

.assembly extern retargetable mscorlib
{
  .publickeytoken = (96 9D B8 05 3D 33 22 AC )                         // ....=3".
  .ver 0:0:0:0
}
.assembly extern mscorlib as mscorlib_1
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly Bar
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module M
// MVID: {01EC94B1-004D-0000-A745-0383B194EC41}
.imagebase 0x034f0000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03090000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi Bar<T>
       extends [mscorlib_1]System.Object
{
  .method public hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Bar::.cctor

  .method public hidebysig static void  BarMeth() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Bar::BarMeth

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib_1]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Bar::.ctor

} // end of class Bar


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
