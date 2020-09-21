// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly ILGEN_MODULE {}

.class ILGEN_CLASS
{
    .method static unsigned int64 ILGEN_METHOD(unsigned int64, int16, int32)
    {
        .maxstack  65535
        .locals init (unsigned int64)

        ldloc.s 0x00
        brtrue IL_005c
        ldloc 0x0000
        conv.r4
        pop
        ldloc 0x0000
        ldc.i4 0x6a468471
        shr
        conv.u
        conv.ovf.i2.un
        not
        ldc.i4 0xb2e976cb
        cgt
        neg
        ldc.i8 0x767d5c09ace81d93
        conv.ovf.i1.un
        ldc.i4 0x67236301
        div.un
        clt
        ldc.r8 float64(0xe7f6d2d768530c5d)
        ldc.r8 float64(0x2f88da93e6c4278c)
        clt.un
        ldarg 0x0001
        neg
        neg
        cgt.un
        rem.un
        not
        conv.u
        conv.ovf.u8.un
        conv.i8
        nop
        not
        conv.r4
        neg
        conv.ovf.u
        starg.s 0x02

IL_005c:
        ldloc.s 0x00
        stloc 0x0000
        ldc.r8 float64(0xcf7b2123eddbb4ef)
        ckfinite
        neg
        ldc.i8 0xc2d58efbc2800db6
        ldarg.s 0x00
        not
        cgt
        neg
        ldc.i8 0x205068055965c3cc
        neg
        ldarg 0x0001
        ldarg.s 0x02
        cgt.un
        shr
        ldloc.s 0x00
        ldarg.s 0x02
        shr
        cgt.un
        add.ovf
        nop
        ldarg.s 0x01
        ldarg 0x0001
        rem
        div
        pop
        conv.u8
        neg
        conv.r8
        conv.r8
        ckfinite
        ldarg.s 0x00
        pop
        conv.ovf.u8.un
        ret
    }

    .method public static int32 Main()
    {
        .entrypoint

        .try
        {
            ldc.i8 0
            ldc.i4 0
            ldc.i4 0
            call unsigned int64 ILGEN_CLASS::ILGEN_METHOD(unsigned int64, int16, int32)
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
