// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }

//  Microsoft (R) .NET Framework IL Disassembler.  Version 2.0.40914.00
//  Copyright (C) Microsoft Corporation. All rights reserved.



// Metadata version: v2.0.40914
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:3600:0
}
.assembly Test2_NonFriendPriInterface
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module Test2_NonFriendPriInterface.dll
// MVID: {492107BD-BB96-4344-A839-40CBDEBAB59A}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03F40000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi IA
{
  .method public hidebysig newslot abstract virtual 
          instance void  methodIA() cil managed
  {
  } // end of method IA::methodIA

} // end of class IA

.class interface private abstract auto ansi 'IB`1'<([mscorlib]System.Object) t>
{
  .method public hidebysig newslot abstract virtual 
          instance void  methodIB() cil managed
  {
  } // end of method 'IB`1'::methodIB

} // end of class 'IB`1'


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Test2_NonFriendPriInterface.res
