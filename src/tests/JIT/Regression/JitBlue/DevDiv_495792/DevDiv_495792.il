// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly 'DevDiv_495792' {}
.assembly extern xunit.core {}

// This test originally triggered an assert Extra_flags_on_tree, because fgMorphCast did not reset an assignment flag, when
// its children was optimized in the assertion propagation.

.class ILGEN_CLASS
{
   .method static unsigned int64 ILGEN_METHOD(unsigned int8, unsigned int64, int32)
   {
        .maxstack  65535
        .locals init (int8, float64)
        IL_0000: ldarg.s 0x01
        IL_0002: ldarg 0x0001
        IL_0006: ldarg.s 0x01
        IL_0008: clt.un
        IL_000a: conv.ovf.i8.un
        IL_000b: cgt.un
        IL_000d: ldloc.s 0x01
        IL_000f: conv.ovf.u1
        IL_0010: and
        IL_0011: conv.r8
        IL_0012: conv.ovf.i8
        IL_0013: neg
        IL_0014: conv.i1
        IL_0015: nop
        IL_0016: ldloc 0x0001
        IL_001a: nop
        IL_001b: dup
        IL_001c: div
        IL_001d: conv.ovf.u4
        IL_001e: ldc.i8 0xf65065040a910acc
        IL_0027: conv.ovf.i8.un
        IL_0028: starg.s 0x01
        IL_002a: rem.un
        IL_002b: ldloc.s 0x00
        IL_002d: ldloc 0x0000
        IL_0031: mul.ovf
        IL_0032: starg 0x0002
        IL_0036: ldloc.s 0x01
        IL_0038: ldarg 0x0001
        IL_003c: conv.r.un
        IL_003d: ldloc 0x0001
        IL_0041: div
        IL_0042: conv.r4
        IL_0043: neg
        IL_0044: add
        IL_0045: ldc.i4 0x79464b58
        IL_004a: conv.ovf.i1.un
        IL_004b: ldarg.s 0x01
        IL_004d: conv.ovf.i4
        IL_004e: mul
        IL_004f: ldloc.s 0x01
        IL_0051: conv.r4
        IL_0052: conv.ovf.i
        IL_0053: ldarg.s 0x00
        IL_0055: shr.un
        IL_0056: mul.ovf
        IL_0057: conv.r8
        IL_0058: add
        IL_0059: conv.ovf.i1
        IL_005a: conv.ovf.i2
        IL_005b: ldc.i8 0x2b5e7f8df42cece0
        IL_0064: starg 0x0001
        IL_0068: neg
        IL_0069: nop
        IL_006a: bgt
        IL_0074
        IL_006f: ldarg 0x0000
        IL_0073: pop
        IL_0074: ldarg.s 0x01
        IL_0076: ldarg 0x0001
        IL_007a: ldarg.s 0x00
        IL_007c: ldloc 0x0001
        IL_0080: ldloc.s 0x01
        IL_0082: neg
        IL_0083: clt.un
        IL_0085: clt
        IL_0087: not
        IL_0088: ldarg.s 0x00
        IL_008a: ldarg.s 0x00
        IL_008c: sub.ovf
        IL_008d: xor
        IL_008e: ldarg.s 0x02
        IL_0090: dup
        IL_0091: rem
        IL_0092: shl
        IL_0093: conv.ovf.u2.un
        IL_0094: shr
        IL_0095: ldarg 0x0001
        IL_0099: pop
        IL_009a: ldarg 0x0001
        IL_009e: clt
        IL_00a0: ldc.r8 float64(0x739233eae9c3b79a)
        IL_00a9: conv.u4
        IL_00aa: div.un
        IL_00ab: pop
        IL_00ac: ret
    }

    .method private static int32 Main()
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .maxstack  3
        .locals init ([0] class [mscorlib]System.OverflowException e,
               [1] int32 V_1)
        IL_0000:  nop
        .try
        {
        IL_0001:  nop
        IL_0002:  ldc.i4.3
        IL_0003:  ldc.i4.2
        IL_0004:  conv.i8
        IL_0005:  ldc.i4.3
        IL_0006:  call       uint64 ILGEN_CLASS::ILGEN_METHOD(uint8,
                                                              uint64,
                                                              int32)
        IL_000b:  pop
        IL_000c:  nop
        IL_000d:  leave.s    IL_0014
        }  // end .try
        catch [mscorlib]System.OverflowException
        {
        IL_000f:  stloc.0
        IL_0010:  nop
        IL_0011:  nop
        IL_0012:  leave.s    IL_0014
        }  // end handler
        IL_0014:  ldc.i4.s   100
        IL_0016:  stloc.1
        IL_0017:  br.s       IL_0019
        IL_0019:  ldloc.1
        IL_001a:  ret
    }
}
