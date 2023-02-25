// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





// Metadata version: v2.0.50222
.assembly extern mscorlib
{
  .ver 2:0:0:0
}
.assembly extern xunit.core {}
.assembly 'foo2'
{
}





.class public auto ansi test extends [mscorlib]System.Object
{
  .method privatescope static int32  M1(float32 Arg_0x4) cil managed
  {
    .maxstack  9


    ldc.i4.0
    brfalse   L1

    L0:
        ldarg.0
        br L2

    L1:
        ldc.i4     0xdbe32521
        conv.r8
        br L2

    L2:
        conv.i4

    ret
  }

  .method public static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  20

    IL_0000:  ldc.r4     3.3
    IL_0005:  call       int32 test::M1(float32)
    IL_000a:  ldc.i4     0xdbe324bd
    IL_000f:  sub
    IL_0010:  ret

  } 

}
