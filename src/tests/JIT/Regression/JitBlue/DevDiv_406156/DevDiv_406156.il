// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly ILGEN_MODULE{}

.class ILGEN_CLASS
{
    .method static unsigned int8 ILGEN_METHOD(int32) noinlining
    {
        .maxstack  65535
        .locals init (int16, char)

        IL_0000: ldc.r8 float64(0x2f293f1943eded91)
        IL_0009: conv.i
        IL_000a: brtrue IL_005a
        IL_000f: ldloc 0x0000
        IL_0013: conv.i8
        IL_0014: dup
        IL_0015: ldc.i8 0x7e24f7eec24ea1f1
        IL_001e: ldc.i8 0x99d7d5a1fb14c476
        IL_0027: ldc.i8 0xccab5fe1121b6940
        IL_0030: neg
        IL_0031: conv.ovf.u8
        IL_0032: neg
        IL_0033: and
        IL_0034: mul.ovf
        IL_0035: conv.ovf.i8
        IL_0036: ldloc.s 0x00
        IL_0038: shl
        IL_0039: conv.i
        IL_003a: shr
        IL_003b: cgt.un
        IL_003d: ldloc 0x0001
        IL_0041: conv.ovf.i8.un
        IL_0042: conv.i
        IL_0043: not
        IL_0044: rem.un
        IL_0045: neg
        IL_0046: conv.i8
        IL_0047: not
        IL_0048: conv.ovf.u.un
        IL_0049: neg
        IL_004a: ldc.i8 0x62dc9bbe9ba06d16
        IL_0053: conv.i2
        IL_0054: neg
        IL_0055: nop
        IL_0056: and
        IL_0057: conv.ovf.i8.un
        IL_0058: conv.ovf.u4
        IL_0059: pop
        IL_005a: ldc.r8 float64(0x9b7fef80771a49e8)
        IL_0063: conv.ovf.u8.un
        IL_0064: ldloc.s 0x00
        IL_0066: ldloc.s 0x01
        IL_0068: conv.ovf.u8.un
        IL_0069: ldc.i8 0xb8c24be67f1d543f
        IL_0072: conv.ovf.i8
        IL_0073: clt.un
        IL_0075: ldc.i4 0xd331e1cd
        IL_007a: ldloc.s 0x00
        IL_007c: shr
        IL_007d: ldloc 0x0001
        IL_0081: mul.ovf
        IL_0082: add
        IL_0083: rem.un
        IL_0084: ldc.i8 0xa9415d5c67c0b7b3
        IL_008d: conv.u8
        IL_008e: neg
        IL_008f: conv.i8
        IL_0090: ldloc 0x0000
        IL_0094: starg.s 0x00
        IL_0096: ldloc.s 0x01
        IL_0098: conv.r.un
        IL_0099: conv.ovf.i2.un
        IL_009a: not
        IL_009b: not
        IL_009c: conv.r8
        IL_009d: conv.ovf.i8
        IL_009e: neg
        IL_009f: cgt
        IL_00a1: mul
        IL_00a2: shr
        IL_00a3: conv.ovf.i8.un
        IL_00a4: ldc.i8 0x9e56367dc0f0785c
        IL_00ad: cgt
        IL_00af: ret
    }

    .method static unsigned int8 ILGEN_METHOD_2(int32) noinlining
    {
        .maxstack  65535
        .locals init (int16, char)

        IL_0000: ldarg 0x0000
        IL_0004: conv.r8
        IL_0005: conv.ovf.i8
        IL_0006: ldc.i4 0x3372b383
        IL_000b: shl
        IL_000c: conv.ovf.i8.un
        IL_000d: conv.ovf.u4
        IL_000e: ret
    }

    .method static int32 Main()
    {
        .entrypoint

        .try
        {
            ldc.i4 0
            call unsigned int8 ILGEN_CLASS::ILGEN_METHOD(int32)
            pop
            leave next
        }
        catch [mscorlib]System.Exception
        {
            pop
            leave next
        }

    next:
        .try
        {
            ldc.i4 0
            call unsigned int8 ILGEN_CLASS::ILGEN_METHOD_2(int32)
            pop
            leave done
        }
        catch [mscorlib]System.Exception
        {
            pop
            leave done
        }

    done:
        ldc.i4 100
        ret
    }
}
// Reading from 'lir_cpp__1524____Assertion_failed__next____def_____found_def_after_use_.mc' dumping raw IL for MC Indexes to console
// ProcessName - 'ILGEN'
.assembly extern mscorlib{}
.assembly ILGEN_MODULE{}
.class ILGEN_CLASS
{
}
// Dumped 1
