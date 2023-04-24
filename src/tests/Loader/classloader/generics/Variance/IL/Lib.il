// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .hash = (34 49 3D D8 CD 90 8A 7F 24 E0 96 E0 1F 82 E7 47   // 4I=.....$......G
           CD A1 2A A0 )                                     // ..*.
  .ver 2:0:3600:0
}
.assembly Lib
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 

  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module Lib.dll
// MVID: {A0B2C024-0046-46A4-83FE-8A184FC49D0D}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03FC0000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface public abstract auto ansi IPlusT<+([mscorlib]System.Object) PlusT>
{
} // end of class IPlusT


.class interface public abstract auto ansi IMinusT<-([mscorlib]System.Object) MinusT>
{
} // end of class IMinusT

.class interface public abstract auto ansi IPlusTMinusU<+([mscorlib]System.Object) PlusT,-([mscorlib]System.Object) MinusU>
{
} // end of class IPlusTMinusU

.class interface public abstract auto ansi IMinusTPlusU<-([mscorlib]System.Object) PlusT,+([mscorlib]System.Object) MinusU>
{
} // end of class IPlusTMinusU


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file IsInstLib001.res
