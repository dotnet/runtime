// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly LdfldaHack
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.module LdfldaHack.exe
// MVID: {61DBCACC-D02C-4722-BA4E-7206DE5BD656}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00EB0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit exploit
       extends [mscorlib]System.Object
{
  .field public static literal int32 address = int32(0x00402050)
  .field private static uint8[] bytes
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method exploit::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldc.i4.s   68
    IL_0002:  newarr     [mscorlib]System.Byte
    IL_0007:  dup
    IL_0008:  ldtoken    field valuetype '<PrivateImplementationDetails>{61DBCACC-D02C-4722-BA4E-7206DE5BD656}'/'__StaticArrayInitTypeSize=68' '<PrivateImplementationDetails>{61DBCACC-D02C-4722-BA4E-7206DE5BD656}'::'$$method0x600004f-1'
    IL_000d:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_0012:  stsfld     uint8[] exploit::bytes
    IL_0017:  ret
  } // end of method exploit::.cctor

} // end of class exploit

.class public sequential ansi sealed beforefieldinit Pad0x10
       extends [mscorlib]System.ValueType
{
  .field public int32 a0
  .field public int32 a1
  .field public int32 a2
  .field public int32 a3
} // end of class Pad0x10

.class public sequential ansi sealed beforefieldinit Pad0x100
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x10 b0
  .field public valuetype Pad0x10 b1
  .field public valuetype Pad0x10 b2
  .field public valuetype Pad0x10 b3
  .field public valuetype Pad0x10 b4
  .field public valuetype Pad0x10 b5
  .field public valuetype Pad0x10 b6
  .field public valuetype Pad0x10 b7
  .field public valuetype Pad0x10 b8
  .field public valuetype Pad0x10 b9
  .field public valuetype Pad0x10 ba
  .field public valuetype Pad0x10 bb
  .field public valuetype Pad0x10 bc
  .field public valuetype Pad0x10 bd
  .field public valuetype Pad0x10 be
  .field public valuetype Pad0x10 bf
} // end of class Pad0x100

.class public sequential ansi sealed beforefieldinit Pad0x1000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x100 c0
  .field public valuetype Pad0x100 c1
  .field public valuetype Pad0x100 c2
  .field public valuetype Pad0x100 c3
  .field public valuetype Pad0x100 c4
  .field public valuetype Pad0x100 c5
  .field public valuetype Pad0x100 c6
  .field public valuetype Pad0x100 c7
  .field public valuetype Pad0x100 c8
  .field public valuetype Pad0x100 c9
  .field public valuetype Pad0x100 ca
  .field public valuetype Pad0x100 cb
  .field public valuetype Pad0x100 cc
  .field public valuetype Pad0x100 cd
  .field public valuetype Pad0x100 ce
  .field public valuetype Pad0x100 'cf'
} // end of class Pad0x1000

.class public sequential ansi sealed beforefieldinit Pad0x4000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x1000 d0
  .field public valuetype Pad0x1000 d1
  .field public valuetype Pad0x1000 d2
  .field public valuetype Pad0x1000 d3
} // end of class Pad0x4000

.class public sequential ansi sealed beforefieldinit Seq0x004000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public int32 val0
  .field public int32 val1
  .field public int32 val2
  .field public int32 val3
  .field public int32 val4
  .field public int32 val5
  .field public int32 val6
  .field public int32 val7
  .field public int32 val8
  .field public int32 val9
} // end of class Seq0x004000

.class public sequential ansi sealed beforefieldinit Seq0x008000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x004000 x
} // end of class Seq0x008000

.class public sequential ansi sealed beforefieldinit Seq0x00c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x008000 x
} // end of class Seq0x00c000

.class public sequential ansi sealed beforefieldinit Seq0x010000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x00c000 x
} // end of class Seq0x010000

.class public sequential ansi sealed beforefieldinit Seq0x014000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x010000 x
} // end of class Seq0x014000

.class public sequential ansi sealed beforefieldinit Seq0x018000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x014000 x
} // end of class Seq0x018000

.class public sequential ansi sealed beforefieldinit Seq0x01c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x018000 x
} // end of class Seq0x01c000

.class public sequential ansi sealed beforefieldinit Seq0x020000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x01c000 x
} // end of class Seq0x020000

.class public sequential ansi sealed beforefieldinit Seq0x024000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x020000 x
} // end of class Seq0x024000

.class public sequential ansi sealed beforefieldinit Seq0x028000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x024000 x
} // end of class Seq0x028000

.class public sequential ansi sealed beforefieldinit Seq0x02c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x028000 x
} // end of class Seq0x02c000

.class public sequential ansi sealed beforefieldinit Seq0x030000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x02c000 x
} // end of class Seq0x030000

.class public sequential ansi sealed beforefieldinit Seq0x034000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x030000 x
} // end of class Seq0x034000

.class public sequential ansi sealed beforefieldinit Seq0x038000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x034000 x
} // end of class Seq0x038000

.class public sequential ansi sealed beforefieldinit Seq0x03c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x038000 x
} // end of class Seq0x03c000

.class public sequential ansi sealed beforefieldinit Seq0x040000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x03c000 x
} // end of class Seq0x040000

.class public sequential ansi sealed beforefieldinit Seq0x044000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x040000 x
} // end of class Seq0x044000

.class public sequential ansi sealed beforefieldinit Seq0x048000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x044000 x
} // end of class Seq0x048000

.class public sequential ansi sealed beforefieldinit Seq0x04c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x048000 x
} // end of class Seq0x04c000

.class public sequential ansi sealed beforefieldinit Seq0x050000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x04c000 x
} // end of class Seq0x050000

.class public sequential ansi sealed beforefieldinit Seq0x054000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x050000 x
} // end of class Seq0x054000

.class public sequential ansi sealed beforefieldinit Seq0x058000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x054000 x
} // end of class Seq0x058000

.class public sequential ansi sealed beforefieldinit Seq0x05c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x058000 x
} // end of class Seq0x05c000

.class public sequential ansi sealed beforefieldinit Seq0x060000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x05c000 x
} // end of class Seq0x060000

.class public sequential ansi sealed beforefieldinit Seq0x064000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x060000 x
} // end of class Seq0x064000

.class public sequential ansi sealed beforefieldinit Seq0x068000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x064000 x
} // end of class Seq0x068000

.class public sequential ansi sealed beforefieldinit Seq0x06c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x068000 x
} // end of class Seq0x06c000

.class public sequential ansi sealed beforefieldinit Seq0x070000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x06c000 x
} // end of class Seq0x070000

.class public sequential ansi sealed beforefieldinit Seq0x074000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x070000 x
} // end of class Seq0x074000

.class public sequential ansi sealed beforefieldinit Seq0x078000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x074000 x
} // end of class Seq0x078000

.class public sequential ansi sealed beforefieldinit Seq0x07c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x078000 x
} // end of class Seq0x07c000

.class public sequential ansi sealed beforefieldinit Seq0x080000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x07c000 x
} // end of class Seq0x080000

.class public sequential ansi sealed beforefieldinit Seq0x084000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x080000 x
} // end of class Seq0x084000

.class public sequential ansi sealed beforefieldinit Seq0x088000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x084000 x
} // end of class Seq0x088000

.class public sequential ansi sealed beforefieldinit Seq0x08c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x088000 x
} // end of class Seq0x08c000

.class public sequential ansi sealed beforefieldinit Seq0x090000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x08c000 x
} // end of class Seq0x090000

.class public sequential ansi sealed beforefieldinit Seq0x094000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x090000 x
} // end of class Seq0x094000

.class public sequential ansi sealed beforefieldinit Seq0x098000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x094000 x
} // end of class Seq0x098000

.class public sequential ansi sealed beforefieldinit Seq0x09c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x098000 x
} // end of class Seq0x09c000

.class public sequential ansi sealed beforefieldinit Seq0x0a0000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x09c000 x
} // end of class Seq0x0a0000

.class public sequential ansi sealed beforefieldinit Seq0x0a4000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0a0000 x
} // end of class Seq0x0a4000

.class public sequential ansi sealed beforefieldinit Seq0x0a8000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0a4000 x
} // end of class Seq0x0a8000

.class public sequential ansi sealed beforefieldinit Seq0x0ac000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0a8000 x
} // end of class Seq0x0ac000

.class public sequential ansi sealed beforefieldinit Seq0x0b0000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0ac000 x
} // end of class Seq0x0b0000

.class public sequential ansi sealed beforefieldinit Seq0x0b4000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0b0000 x
} // end of class Seq0x0b4000

.class public sequential ansi sealed beforefieldinit Seq0x0b8000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0b4000 x
} // end of class Seq0x0b8000

.class public sequential ansi sealed beforefieldinit Seq0x0bc000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0b8000 x
} // end of class Seq0x0bc000

.class public sequential ansi sealed beforefieldinit Seq0x0c0000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0bc000 x
} // end of class Seq0x0c0000

.class public sequential ansi sealed beforefieldinit Seq0x0c4000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0c0000 x
} // end of class Seq0x0c4000

.class public sequential ansi sealed beforefieldinit Seq0x0c8000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0c4000 x
} // end of class Seq0x0c8000

.class public sequential ansi sealed beforefieldinit Seq0x0cc000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0c8000 x
} // end of class Seq0x0cc000

.class public sequential ansi sealed beforefieldinit Seq0x0d0000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0cc000 x
} // end of class Seq0x0d0000

.class public sequential ansi sealed beforefieldinit Seq0x0d4000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0d0000 x
} // end of class Seq0x0d4000

.class public sequential ansi sealed beforefieldinit Seq0x0d8000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0d4000 x
} // end of class Seq0x0d8000

.class public sequential ansi sealed beforefieldinit Seq0x0dc000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0d8000 x
} // end of class Seq0x0dc000

.class public sequential ansi sealed beforefieldinit Seq0x0e0000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0dc000 x
} // end of class Seq0x0e0000

.class public sequential ansi sealed beforefieldinit Seq0x0e4000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0e0000 x
} // end of class Seq0x0e4000

.class public sequential ansi sealed beforefieldinit Seq0x0e8000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0e4000 x
} // end of class Seq0x0e8000

.class public sequential ansi sealed beforefieldinit Seq0x0ec000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0e8000 x
} // end of class Seq0x0ec000

.class public sequential ansi sealed beforefieldinit Seq0x0f0000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0ec000 x
} // end of class Seq0x0f0000

.class public sequential ansi sealed beforefieldinit Seq0x0f4000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0f0000 x
} // end of class Seq0x0f4000

.class public sequential ansi sealed beforefieldinit Seq0x0f8000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0f4000 x
} // end of class Seq0x0f8000

.class public sequential ansi sealed beforefieldinit Seq0x0fc000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0f8000 x
} // end of class Seq0x0fc000

.class public sequential ansi sealed beforefieldinit Seq0x100000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x0fc000 x
} // end of class Seq0x100000

.class public sequential ansi sealed beforefieldinit Seq0x104000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x100000 x
} // end of class Seq0x104000

.class public sequential ansi sealed beforefieldinit Seq0x108000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x104000 x
} // end of class Seq0x108000

.class public sequential ansi sealed beforefieldinit Seq0x10c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x108000 x
} // end of class Seq0x10c000

.class public sequential ansi sealed beforefieldinit Seq0x110000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x10c000 x
} // end of class Seq0x110000

.class public sequential ansi sealed beforefieldinit Seq0x114000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x110000 x
} // end of class Seq0x114000

.class public sequential ansi sealed beforefieldinit Seq0x118000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x114000 x
} // end of class Seq0x118000

.class public sequential ansi sealed beforefieldinit Seq0x11c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x118000 x
} // end of class Seq0x11c000

.class public sequential ansi sealed beforefieldinit Seq0x120000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x11c000 x
} // end of class Seq0x120000

.class public sequential ansi sealed beforefieldinit Seq0x124000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x120000 x
} // end of class Seq0x124000

.class public sequential ansi sealed beforefieldinit Seq0x128000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x124000 x
} // end of class Seq0x128000

.class public sequential ansi sealed beforefieldinit Seq0x12c000
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x4000 pre
  .field public valuetype Seq0x128000 x
} // end of class Seq0x12c000

.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .field public valuetype Pad0x1000 pre1000
  .field public valuetype Pad0x1000 pre2000
  .field public valuetype Pad0x1000 pre3000
  .field public valuetype Pad0x100 pre3100
  .field public valuetype Pad0x100 pre3200
  .field public valuetype Pad0x100 pre3300
  .field public valuetype Pad0x100 pre3400
  .field public valuetype Pad0x10 pre3410
  .field public valuetype Pad0x10 pre3420
  .field public valuetype Pad0x10 pre3430
  .field public valuetype Pad0x10 pre3440
  .field public valuetype Pad0x10 pre3450
  .field public valuetype Pad0x10 pre3460
  .field public valuetype Seq0x12c000 x
} // end of class S

.class public auto ansi beforefieldinit LdfldaHack
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  F(valuetype Seq0x004000& t0) cil managed
  {
    // Code size       112 (0x70)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4     0x402050
    IL_0007:  stfld      int32 Seq0x004000::val0
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4     0x402050
    IL_0012:  stfld      int32 Seq0x004000::val1
    IL_0017:  ldarg.0
    IL_0018:  ldc.i4     0x402050
    IL_001d:  stfld      int32 Seq0x004000::val2
    IL_0022:  ldarg.0
    IL_0023:  ldc.i4     0x402050
    IL_0028:  stfld      int32 Seq0x004000::val3
    IL_002d:  ldarg.0
    IL_002e:  ldc.i4     0x402050
    IL_0033:  stfld      int32 Seq0x004000::val4
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4     0x402050
    IL_003e:  stfld      int32 Seq0x004000::val5
    IL_0043:  ldarg.0
    IL_0044:  ldc.i4     0x402050
    IL_0049:  stfld      int32 Seq0x004000::val6
    IL_004e:  ldarg.0
    IL_004f:  ldc.i4     0x402050
    IL_0054:  stfld      int32 Seq0x004000::val7
    IL_0059:  ldarg.0
    IL_005a:  ldc.i4     0x402050
    IL_005f:  stfld      int32 Seq0x004000::val8
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4     0x402050
    IL_006a:  stfld      int32 Seq0x004000::val9
    IL_006f:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x008000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x004000 Seq0x008000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x004000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x00c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x008000 Seq0x00c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x008000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x010000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x00c000 Seq0x010000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x00c000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x014000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x010000 Seq0x014000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x010000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x018000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x014000 Seq0x018000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x014000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x01c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x018000 Seq0x01c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x018000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x020000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x01c000 Seq0x020000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x01c000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x024000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x020000 Seq0x024000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x020000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x028000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x024000 Seq0x028000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x024000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x02c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x028000 Seq0x02c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x028000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x030000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x02c000 Seq0x030000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x02c000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x034000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x030000 Seq0x034000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x030000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x038000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x034000 Seq0x038000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x034000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x03c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x038000 Seq0x03c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x038000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x040000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x03c000 Seq0x040000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x03c000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x044000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x040000 Seq0x044000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x040000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x048000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x044000 Seq0x048000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x044000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x04c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x048000 Seq0x04c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x048000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x050000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x04c000 Seq0x050000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x04c000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x054000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x050000 Seq0x054000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x050000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x058000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x054000 Seq0x058000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x054000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x05c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x058000 Seq0x05c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x058000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x060000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x05c000 Seq0x060000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x05c000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x064000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x060000 Seq0x064000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x060000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x068000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x064000 Seq0x068000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x064000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x06c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x068000 Seq0x06c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x068000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x070000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x06c000 Seq0x070000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x06c000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x074000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x070000 Seq0x074000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x070000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x078000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x074000 Seq0x078000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x074000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x07c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x078000 Seq0x07c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x078000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x080000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x07c000 Seq0x080000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x07c000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x084000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x080000 Seq0x084000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x080000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x088000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x084000 Seq0x088000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x084000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x08c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x088000 Seq0x08c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x088000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x090000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x08c000 Seq0x090000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x08c000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x094000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x090000 Seq0x094000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x090000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x098000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x094000 Seq0x098000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x094000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x09c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x098000 Seq0x09c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x098000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0a0000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x09c000 Seq0x0a0000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x09c000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0a4000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0a0000 Seq0x0a4000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0a0000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0a8000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0a4000 Seq0x0a8000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0a4000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0ac000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0a8000 Seq0x0ac000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0a8000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0b0000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0ac000 Seq0x0b0000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0ac000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0b4000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0b0000 Seq0x0b4000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0b0000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0b8000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0b4000 Seq0x0b8000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0b4000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0bc000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0b8000 Seq0x0bc000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0b8000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0c0000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0bc000 Seq0x0c0000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0bc000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0c4000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0c0000 Seq0x0c4000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0c0000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0c8000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0c4000 Seq0x0c8000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0c4000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0cc000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0c8000 Seq0x0cc000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0c8000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0d0000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0cc000 Seq0x0d0000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0cc000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0d4000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0d0000 Seq0x0d4000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0d0000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0d8000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0d4000 Seq0x0d8000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0d4000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0dc000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0d8000 Seq0x0dc000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0d8000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0e0000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0dc000 Seq0x0e0000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0dc000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0e4000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0e0000 Seq0x0e4000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0e0000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0e8000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0e4000 Seq0x0e8000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0e4000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0ec000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0e8000 Seq0x0ec000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0e8000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0f0000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0ec000 Seq0x0f0000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0ec000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0f4000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0f0000 Seq0x0f4000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0f0000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0f8000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0f4000 Seq0x0f8000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0f4000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x0fc000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0f8000 Seq0x0fc000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0f8000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x100000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x0fc000 Seq0x100000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x0fc000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x104000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x100000 Seq0x104000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x100000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x108000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x104000 Seq0x108000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x104000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x10c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x108000 Seq0x10c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x108000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x110000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x10c000 Seq0x110000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x10c000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x114000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x110000 Seq0x114000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x110000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x118000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x114000 Seq0x118000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x114000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x11c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x118000 Seq0x11c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x118000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x120000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x11c000 Seq0x120000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x11c000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x124000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x120000 Seq0x124000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x120000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x128000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x124000 Seq0x128000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x124000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static void  F(valuetype Seq0x12c000& z) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldflda     valuetype Seq0x128000 Seq0x12c000::x
    IL_0007:  call       void LdfldaHack::F(valuetype Seq0x128000&)
    IL_000c:  nop
    IL_000d:  ret
  } // end of method LdfldaHack::F

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       23 (0x17)
    .maxstack  8
    .locals init (valuetype S& V_0,class [mscorlib]System.NullReferenceException V_1, int32 V_2)
    IL_0000:  nop
    .try
    {
         IL_0009:  ldloc.s   V_0
         IL_000b:  ldflda     valuetype Seq0x12c000 S::x
         IL_0010:  call       void LdfldaHack::F(valuetype Seq0x12c000&)
                   ldstr     "FAIL: expected NullReferenceException not thrown"
                   call      void [System.Console]System.Console::WriteLine(string)
                   ldc.i4    99
                   stloc     V_2
         IL_0016:  leave     IL_00ff
     }
     catch [mscorlib]System.NullReferenceException
     {
	      stloc      V_1
              ldstr      "PASS: caught expected exception: {0}"
              ldloc      V_1
              callvirt   instance string [mscorlib]System.Object::ToString()
              call       void [System.Console]System.Console::WriteLine(string,object)
              ldc.i4     100
              stloc      V_2
              leave      IL_00ff
        
     }
     IL_00ff: ldloc  V_2
              ret
  } // end of method LdfldaHack::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method LdfldaHack::.ctor

} // end of class LdfldaHack

.class private auto ansi '<PrivateImplementationDetails>{61DBCACC-D02C-4722-BA4E-7206DE5BD656}'
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .class explicit ansi sealed nested private '__StaticArrayInitTypeSize=68'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 68
  } // end of class '__StaticArrayInitTypeSize=68'

  .field static assembly valuetype '<PrivateImplementationDetails>{61DBCACC-D02C-4722-BA4E-7206DE5BD656}'/'__StaticArrayInitTypeSize=68' '$$method0x600004f-1' at I_00002050
} // end of class '<PrivateImplementationDetails>{61DBCACC-D02C-4722-BA4E-7206DE5BD656}'


// =============================================================

.data cil I_00002050 = bytearray (
                 33 C0 64 8B 40 30 8B 40 0C 8B 40 0C 8B 08 8B 01  // 3.d.@0.@..@.....
                 8B 00 8B 40 18 50 8B 48 3C 8B 4C 08 78 03 C8 8B  // ...@.P.H<.L.x...
                 51 20 8B 49 1C 83 C1 04 83 C2 04 8B 34 02 81 3C  // Q .I........4..<
                 06 57 69 6E 45 75 EE 03 04 08 68 63 61 6C 63 54  // .WinEu....hcalcT
                 FF D0 EB FE) 
// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file LdfldaHack.res
