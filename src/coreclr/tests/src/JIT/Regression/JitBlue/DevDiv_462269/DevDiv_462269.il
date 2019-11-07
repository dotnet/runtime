// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib{}
.assembly ILGEN_MODULE{}
.class ILGEN_CLASS
{
    .method static int64 ILGEN_METHOD(unsigned int32, float32, unsigned int8, char, int64, float64)
    {
        .maxstack  65535
        .locals init (float64, unsigned int32, int64, int32, unsigned int16, unsigned int64, char, int8, unsigned int32, bool, bool)

        ldloc.s 0x02
        ldloc.s 0x02
        add
        conv.ovf.u2
        not
        ldarg 0x0004
        ldloc 0x0005
        conv.ovf.i8.un
        conv.ovf.i2
        shl
        ldarg.s 0x02
        conv.ovf.u4
        ldarg 0x0004
        conv.ovf.u1.un
        rem.un
        shr.un
        not
        ldarg.s 0x04
        not
        div
        ldloc 0x0005
        add
        conv.ovf.u8.un
        ldloc.s 0x05
        ldloc 0x0002
        ldarg.s 0x04
        div.un
        neg
        ldarg 0x0004
        ldloc.s 0x06
        conv.ovf.i8
        conv.ovf.u1.un
        conv.ovf.i8.un
        sub
        sub.ovf
        and
        ldarg 0x0005
        ldarg.s 0x05
        ceq
        starg.s 0x03
        ldarg 0x0003
        conv.ovf.i8
        ldarg.s 0x03
        conv.ovf.u8
        ldloc.s 0x04
        shl
        cgt
        ldarg.s 0x03
        neg
        cgt
        not
        shr
        ldarg.s 0x01
        pop
        and
        conv.ovf.u4
        ceq
        pop
        ldloc 0x0000
        ldloc 0x0007
        conv.r.un
        neg
        mul
        ckfinite
        ldloc 0x0000
        ckfinite
        neg
        mul
        pop
        ldloc.s 0x05
        ldloc 0x0005
        and
        ldc.r8 float64(0xc4a68d93e8d67cec)
        conv.ovf.i8.un
        dup
        and
        and
        ldarg 0x0004
        ldarg 0x0000
        ldarg 0x0003
        clt
        shl
        conv.ovf.u4
        ldarg.s 0x00
        shr.un
        conv.ovf.u8
        ldloc 0x0006
        pop
        xor
        conv.ovf.i8.un
        conv.i8
        neg
        ldarg.s 0x01
        conv.i1
        conv.u8
        nop
        add.ovf.un
        ldloc.s 0x02
        conv.u8
        mul.ovf
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
