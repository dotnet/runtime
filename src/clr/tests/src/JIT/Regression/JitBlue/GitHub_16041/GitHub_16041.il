// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { auto }
.assembly GitHub_16041 { }

// The test originally hit a problem with "Extra flags on tree" on amd64,
// because "fgMorphBlockOperand" did not set flags when morphing comma for addr exposed lcl_Var.
// "StructY" is struct with exposed addr, it needs to be larger than 8 bytes for amd64.


.class private auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       10 (0xa)
    .maxstack  8
    .locals init (valuetype StructX V_0)
    IL_0000:  ldloc      V_0
    IL_0004:  call       void Test::Test(valuetype StructX)
    IL_0009:  ldc.i4 100
    IL_000a:  ret
  } // end of method Test::Main

  .method private hidebysig static void  Test(valuetype StructX StructX) cil managed noinlining
  {
    // Code size       10 (0xa)
    .maxstack  8
    .locals (valuetype StructY V_0)
    IL_0000:  ldarga.s   StructX
    IL_0002:  call       valuetype StructY Test::Convert(valuetype StructX*)
    IL_0007:  stloc.s    V_0
    IL_0009:  ret
  } // end of method Test::Test

  .method public hidebysig static valuetype StructY 
          Convert(valuetype StructX* StructX) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  3
    .locals (valuetype StructY V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldarg.0
    IL_0003:  conv.i
    IL_0004:  ldc.i4.s   12
    IL_0006:  unaligned. 4 // This opcode created the bad tree, "Convert" needed to be inlined to have a LCL_VAR here.
    IL_0009:  cpblk
    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method Test::Convert

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test

.class public sequential ansi sealed beforefieldinit StructX
       extends [mscorlib]System.ValueType
{
  .field private int32 A
  .field private int32 B
  .field private int32 C
} // end of class StructX

.class private sequential ansi sealed beforefieldinit StructY
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 12
  .field private int32 Padding
} // end of class StructY
