// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly ILGEN_MODULE{}
.class ILGEN_CLASS
{
    .method static int64 ILGEN_METHOD(unsigned int32, float32, unsigned int8, char, int64, float64)
    {
       .maxstack  65535
       .locals init (float64 l0, unsigned int32 l1, int64 l2, int32 l3, unsigned int16 l4, unsigned int64 l5, char l6, int8 l7, unsigned int32 l8, bool l9, bool l10)

        ldc.i8 0xfc7246b593d3e823
        not
        neg
        ldarg 0x0005
        conv.i2
        conv.r.un
        ldarg.s 0x05
        conv.r4
        cgt.un
        nop
        ldc.i8 0xb230872abaf2c301
        conv.ovf.i
        or
        ldarg.s 0x02
        conv.r.un
        conv.ovf.i8.un
        conv.ovf.u1.un
        add.ovf.un
        shl
        ldloc 0x0004
        dup
        ldloc.s 0x08
        not
        ceq
        clt
        conv.r4
        pop
        ldarg.s 0x05
        conv.ovf.i8.un
        conv.ovf.i8
        bgt skip
        nop
skip:
        ldarg 0x0004
        conv.u8
        ret
    }

    .method static int32 Main()
    {
        .entrypoint

        .try
        {
            ldc.i4 0
            ldc.r4 0
            ldc.i4 0
            dup
            ldc.i8 0
            ldc.r8 0
            call int64 ILGEN_CLASS::ILGEN_METHOD(unsigned int32, float32, unsigned int8, char, int64, float64)
            pop
            leave.s done
        }
        catch [mscorlib]System.Exception
        {
            leave.s done
        }

    done:
        ldc.i4 100
        ret
    }

}
