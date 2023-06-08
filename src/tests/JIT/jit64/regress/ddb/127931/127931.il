// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly extern xunit.core {}
.assembly '127931'
{
  .ver 0:0:0:0
}
// MVID: {FB8EB234-0641-4679-8147-BB7819EB64E7}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00000000032A0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi ILGEN_0xf64b7da3
       extends [mscorlib]System.Object
{
  .method privatescope static int16  Method_0xcb0b0b48$PST06000001(uint32 Arg_0x2) cil managed
  {
    // Code size       57 (0x39)
    .maxstack  19
    IL_0000:  ldc.r8     -8.9424969375644573e+103
    IL_0009:  conv.ovf.i1
    IL_000a:  ldarga     Arg_0x2
    IL_000e:  pop
    IL_000f:  pop
    IL_0010:  ldc.r8     1.4895743916107676e+262
    IL_0019:  conv.u1
    IL_001a:  starg      Arg_0x2
    IL_001e:  ldc.i4.8
    IL_001f:  starg      Arg_0x2
    IL_0023:  ldarg      Arg_0x2
    IL_0027:  ldc.i4.s   85
    IL_0029:  rem
    IL_002a:  conv.r4
    IL_002b:  ldc.r4     3.9397709e-035
    IL_0030:  cgt.un
    IL_0032:  dup
    IL_0033:  brtrue     IL_001a

    IL_0038:  ret
  } // end of method ILGEN_0xf64b7da3::Method_0xcb0b0b48

  .method public static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       11 (0xb)
    .maxstack  11
    .try
    {
    IL_0000:  ldc.i4     0xcf956c1
    IL_0005:  call       int16 ILGEN_0xf64b7da3::Method_0xcb0b0b48$PST06000001(uint32)
              pop
              leave IL_000a
    }
    catch [mscorlib]System.OverflowException
    {
              leave IL_000a
    }
              
    IL_000a:  ldc.i4 100
              ret
  } // end of method ILGEN_0xf64b7da3::Main

} // end of class ILGEN_0xf64b7da3


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
