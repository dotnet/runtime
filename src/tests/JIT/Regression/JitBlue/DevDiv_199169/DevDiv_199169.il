// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly DevDiv_199169
{
}
.module DevDiv_199169.exe

.class public sequential ansi sealed beforefieldinit Blob
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 3
}

.field public static valuetype Blob bag

.method assembly static void test() cil managed
{
  .maxstack  3
  IL_0000:  ldsflda    valuetype Blob bag
  IL_0005:  ldc.i4.s   -128 // this constant is outside 0..255
  IL_0007:  ldc.i4.3
  IL_0008:  unaligned. 1
  IL_000b:  initblk
  IL_000d:  ret
}

.method assembly static int32  main(string[] args) cil managed
{
  .entrypoint
  .maxstack  6
  .locals (int32 V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  call       void test()
  IL_0007:  ldsflda    valuetype Blob bag
  IL_000c:  ldc.i4.0
  IL_0011:  add
  IL_0012:  ldind.u1
  IL_0013:  ldc.i4     0x80
  IL_0018:  bne.un.s   IL_0048

  IL_001a:  ldsflda    valuetype Blob bag
  IL_001f:  ldc.i4.1
  IL_0020:  add
  IL_0021:  ldind.u1
  IL_0022:  ldc.i4     0x80
  IL_0027:  bne.un.s   IL_0048

  IL_0029:  ldsflda    valuetype Blob bag
  IL_002e:  ldc.i4.2
  IL_0033:  add
  IL_0034:  ldind.u1
  IL_0035:  ldc.i4     0x80
  IL_003a:  bne.un.s   IL_0048

  IL_003c:  ldc.i4.s   100
  IL_0041:  ret
  IL_0048:  ldc.i4.s   101
  IL_0050:  ret
}
