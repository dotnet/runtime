// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// initblk is expected to use only the low byte of the initialization value that's on the stack

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly GitHub_27169 {}
.assembly extern xunit.core {}

.class public auto beforefieldinit Program
       extends [System.Runtime]System.Object
{
  .class sequential sealed nested private beforefieldinit Block
         extends [System.Runtime]System.ValueType
  {
    .field public int32 a
    .field public int32 b
    .field public int32 c
    .field public int32 d
    .field public int32 e
    .field public int32 f
    .field public int32 g
    .field public int16 h
    .field public int8 i
  }
  
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack 32
    .locals init (valuetype Program/Block b)
    
    ldloca b
    call void Program::Test(valuetype Program/Block&)

    ldloca b
    ldfld int32 Program/Block::a
    ldloca b
    ldfld int32 Program/Block::b
    or
    ldloca b
    ldfld int32 Program/Block::c
    or
    ldloca b
    ldfld int32 Program/Block::d
    or
    ldloca b
    ldfld int32 Program/Block::e
    or
    ldloca b
    ldfld int32 Program/Block::f
    or
    ldloca b
    ldfld int32 Program/Block::g
    or
    ldloca b
    ldfld int16 Program/Block::h
    or
    ldloca b
    ldfld int8 Program/Block::i
    or
    brtrue FAIL
    ldc.i4 100
    ret
FAIL:
    ldc.i4 1
    ret
  }

  .method private hidebysig static void Test(valuetype Program/Block& p) cil managed noinlining
  {
    .maxstack 3

    ldarg.0
    ldc.i4 0x11100
    ldc.i4 31
    initblk
    ret
  }
}
