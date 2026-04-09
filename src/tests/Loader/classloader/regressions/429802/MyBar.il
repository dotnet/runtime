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
  .ver 2:0:0:0
}
.assembly MyBar
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module M
// MVID: {01C3371C-004D-0000-A745-03831C37C341}
.imagebase 0x034f0000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03090000


// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi beforefieldinit MyBar
       extends [mscorlib_1]System.Object
{
  .method public hidebysig newslot abstract virtual 
          instance int32  DoBar() cil managed
  {
  } // end of method MyBar::DoBar

  .method public hidebysig virtual instance int32 
          DoSelfBar() cil managed
  {
    .override MyBar::DoBar
    // Code size       14 (0xe)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "In MyBar.DoSelfBar"
    IL_0005:  call       void [mscorlib_1]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.1
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ret
  } // end of method MyBar::DoSelfBar

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib_1]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyBar::.ctor

} // end of class MyBar


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
