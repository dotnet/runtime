// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly ILGEN_MODULE {}

.class ILGEN_CLASS
{
    .method static native int ILGEN_METHOD(unsigned int8, int64, native int, unsigned int16)
    {
        .maxstack  65535
        .locals init (int16, native unsigned int, int16)

        ldc.i8 0xa67bdf4860e28133
        ldarg.s 0x01
        conv.ovf.u8
        ldarg 0x0002
        not
        not
        ldarg 0x0001
        conv.i
        shr.un
        starg.s 0x03
        ldc.i8 0xb39ec88a74e0306e
        ldc.i8 0xc5a0503d9e231fd1
        not
        ldarg 0x0002
        shr
        neg
        sub
        conv.ovf.u.un
        shl
        clt
        ldloc 0x0000
        ldarg.s 0x01
        ldarg.s 0x01
        mul
        conv.i8
        conv.ovf.i8
        conv.u
        ldloc.s 0x02
        conv.i
        mul
        and
        shl
        neg
        not
        not
        brtrue IL_0055
        ldloc.s 0x02
        pop

IL_0055:
        ldarg.s 0x01
        ldarg.s 0x03
        shr
        ldarg 0x0001
        ldarg.s 0x01
        div
        conv.i2
        ldloc.s 0x00
        sub
        conv.ovf.u8.un
        or
        neg
        conv.ovf.u.un
        not
        ldarg.s 0x02
        ldarg 0x0001
        conv.u2
        neg
        shr.un
        conv.ovf.i8.un
        conv.ovf.u4.un
        neg
        conv.ovf.i8
        ldarg 0x0001
        clt
        neg
        sub
        ldloc 0x0000
        conv.ovf.i1.un
        ldarg.s 0x03
        div.un
        nop
        sub
        not
        not
        not
        ret
    }

    .method public static int32 Main()
    {
        .entrypoint

        .try
        {
            ldc.i4 0
            ldc.i8 0
            ldc.i4 0
            conv.i
            ldc.i4 0
            call native int ILGEN_CLASS::ILGEN_METHOD(unsigned int8, int64, native int, unsigned int16)
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
