// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// Test for remorphing subexpressions in casts of long shifts.
.assembly extern System.Private.CoreLib { auto }
.assembly extern mscorlib { auto }

.assembly GitHub_15319_1 {}

.class private auto ansi beforefieldinit Q
       extends [System.Private.CoreLib]System.Object
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

  .method public hidebysig static int32  Main(string[] args) cil managed
  {
       .entrypoint
       ldc.i4.s   100
       conv.i8
       call       int32 Q::P(int64)
       ret
  }
}
