// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test for a issue when tail call with small return type 
// doesn't pass tail call pattern assert.
// In addition there is "call->branch on return->return" tail call pattern check.


.assembly extern mscorlib {}

.assembly GitHub_CoreRT_2073 {}
.assembly extern xunit.core {}

.class public Test_GitHub_CoreRT_2073
{

.method private hidebysig static bool  tailAsgReturn(int32 deep) cil managed
{
    // Code size       24 (0x18)
    .maxstack  20
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.s   -100
    IL_0003:  bgt.s      IL_0007
    IL_0005:  ldc.i4.1
    IL_0006:  ret
    IL_0007:  ldarg.0
    IL_0008:  ldc.i4.0
    IL_0009:  bgt.s      IL_000f

    IL_000a:  ldarg.0
    IL_000b:  ldc.i4.1
    IL_000c:  sub
    IL_000d:  call       bool Test_GitHub_CoreRT_2073::tailAsgReturn(int32)
    IL_000e:  br IL_0017
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4.2
    IL_0011:  sub
    IL_0012:  call       bool Test_GitHub_CoreRT_2073::tailAsgReturn(int32)
    IL_0017:  ret
} // end of method Test::tailAsgReturn

.method public hidebysig static int32  Main(string[] args) cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  // Code size       14 (0xe)
  .maxstack  8
  IL_0000:  ldc.i4.s   100
  IL_0002:  call       bool Test_GitHub_CoreRT_2073::tailAsgReturn(int32)
  IL_0007:  brtrue.s   IL_000b
  IL_0009:  ldc.i4.m1
  IL_000a:  ret
  IL_000b:  ldc.i4.s   100
  IL_000d:  ret
} // end of method Test::Main
}
