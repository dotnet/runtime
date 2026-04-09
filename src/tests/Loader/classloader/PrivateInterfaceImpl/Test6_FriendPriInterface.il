// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:3600:0
}
.assembly Test6_FriendPriInterface
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 23 54 65 73 74 36 5F 73 75 70 70 6C 69 65   // ..#Test6_supplie
                                                                                                                 72 2C 20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65   // r, PublicKeyToke
                                                                                                                 6E 3D 6E 75 6C 6C 00 00 )                         // n=null..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 2C 54 65 73 74 36 5F 49 6D 70 6C 65 6D 65   // ..,Test6_Impleme
                                                                                                                 6E 74 69 6E 67 43 6C 61 73 73 2C 20 50 75 62 6C   // ntingClass, Publ
                                                                                                                 69 63 4B 65 79 54 6F 6B 65 6E 3D 6E 75 6C 6C 00   // icKeyToken=null.
                                                                                                                 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module Test6_FriendPriInterface.dll
// MVID: {1B87B22B-74B6-4B85-86F0-0F70258C2D18}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03E50000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi IA
{
  .method public hidebysig newslot abstract virtual 
          instance void  methodIA() cil managed
  {
  } // end of method IA::methodIA

} // end of class IA

.class interface private abstract auto ansi 'IB`1'<([mscorlib]System.Object) T>
{
  .method public hidebysig newslot abstract virtual 
          instance void  methodIB() cil managed
  {
  } // end of method 'IB`1'::methodIB

} // end of class 'IB`1'


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Test6_FriendPriInterface.res
