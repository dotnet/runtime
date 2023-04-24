// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Test for remorphing subexpressions in casts of long shifts.
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern mscorlib { auto }

.assembly GitHub_15319_1 {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Q
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32  P(int64 x) cil managed noinlining
  {
       ldarg.s      0x0
       dup
       ldarg.s      0x0
       clt
       shl
       conv.i4
       ret
  }

  .method public hidebysig static int32  Main() cil managed
  {
       .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
           01 00 00 00
       )
       .entrypoint
       ldc.i4.s   100
       conv.i8
       call       int32 Q::P(int64)
       ret
  }
}
