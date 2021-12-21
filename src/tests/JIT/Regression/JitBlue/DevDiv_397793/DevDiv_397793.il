// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly 'DevDiv_397793'{}
.assembly extern xunit.core {}

// This test originally failed compilation during CSE due to value numbers that were discarded during remorphing.
// The arguments to and return value from `ILGEN_METHOD` are not meaningful; it is enough that this function
// compiles successfully.

.class ILGEN_CLASS
{
    .method static float32 ILGEN_METHOD(int16, char, char, native int)
    {
        .maxstack  65535
        .locals init (int8, native unsigned int, int64, int8, bool, unsigned int64, char, float64, int64, native int, float32, int64, int64, unsigned int16)

        IL_0000: ldloc 0x000d
        IL_0004: not
        IL_0005: ldloc.s 0x06
        IL_0007: cgt.un
        IL_0009: ldloc.s 0x06
        IL_000b: clt.un
        IL_000d: conv.r4
        IL_000e: ldloc.s 0x08
        IL_0010: conv.r.un
        IL_0011: mul
        IL_0012: conv.i8
        IL_0013: ldloc.s 0x03
        IL_0015: conv.u1
        IL_0016: not
        IL_0017: ldloc.s 0x00
        IL_0019: ldloc.s 0x07
        IL_001b: neg
        IL_001c: conv.u1
        IL_001d: xor
        IL_001e: sub
        IL_001f: ldloc 0x000c
        IL_0023: neg
        IL_0024: conv.i8
        IL_0025: ldloc.s 0x07
        IL_0027: neg
        IL_0028: conv.u8
        IL_0029: conv.i8
        IL_002a: ldc.i4 0x2487c9b5
        IL_002f: conv.i8
        IL_0030: rem.un
        IL_0031: ldloc.s 0x09
        IL_0033: conv.i8
        IL_0034: ldloc.s 0x01
        IL_0036: neg
        IL_0037: not
        IL_0038: ldloc 0x0006
        IL_003c: mul
        IL_003d: shr.un
        IL_003e: add
        IL_003f: ldloc.s 0x0b
        IL_0041: conv.i4
        IL_0042: shr.un
        IL_0043: ldarg.s 0x03
        IL_0045: conv.i4
        IL_0046: ldloc.s 0x00
        IL_0048: clt
        IL_004a: neg
        IL_004b: ldloc.s 0x09
        IL_004d: pop
        IL_004e: conv.r.un
        IL_004f: conv.u8
        IL_0050: ldloc.s 0x05
        IL_0052: mul
        IL_0053: ldloc.s 0x0c
        IL_0055: conv.i8
        IL_0056: not
        IL_0057: cgt.un
        IL_0059: neg
        IL_005a: nop
        IL_005b: shl
        IL_005c: ldc.i8 0x6ddee7e52bcb7a50
        IL_0065: ldloc 0x0004
        IL_0069: shr.un
        IL_006a: pop
        IL_006b: and
        IL_006c: conv.u2
        IL_006d: add
        IL_006e: ldarg.s 0x02
        IL_0070: not
        IL_0071: neg
        IL_0072: pop
        IL_0073: ldarg.s 0x01
        IL_0075: ldloc 0x0008
        IL_0079: ldloc 0x000b
        IL_007d: clt
        IL_007f: conv.r.un
        IL_0080: pop
        IL_0081: ldloc 0x000c
        IL_0085: not
        IL_0086: ldloc.s 0x0c
        IL_0088: conv.u8
        IL_0089: mul
        IL_008a: ldloc 0x000b
        IL_008e: add
        IL_008f: ldloc 0x0002
        IL_0093: ldloc.s 0x0b
        IL_0095: rem
        IL_0096: ldc.i8 0xfe6f83985a745065
        IL_009f: add
        IL_00a0: neg
        IL_00a1: cgt.un
        IL_00a3: shr
        IL_00a4: conv.u8
        IL_00a5: nop
        IL_00a6: neg
        IL_00a7: neg
        IL_00a8: pop
        IL_00a9: shr
        IL_00aa: nop
        IL_00ab: conv.r.un
        IL_00ac: ret   
    }

    .method public static int32 Main()
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint

        .try
        {
            ldc.i4 0
            ldc.i4 0
            ldc.i4 0
            ldc.i4 0
            conv.i
            call float32 ILGEN_CLASS::ILGEN_METHOD(int16, char, char, native int)
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
