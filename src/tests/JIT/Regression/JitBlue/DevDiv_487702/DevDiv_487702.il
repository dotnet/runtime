// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly 'DevDiv_487702' {}
.assembly extern xunit.core {}

.class ILGEN_CLASS
{
    .method static native int ILGEN_METHOD(unsigned int8, int64, native int, unsigned int16)
    {
        .maxstack  65535
        .locals init (int16, native unsigned int, int16)

        ldarg.s 0x01
        conv.r4
        conv.ovf.i8
        ldarg 0x0001
        not
        add.ovf.un
        ldc.i8 0x3171ecd9351b8edd
        conv.ovf.i8.un
        ldarg.s 0x03
        shr
        ldloc 0x0002
        ldloc.s 0x01
        conv.ovf.i8
        ldarg 0x0001
        cgt.un
        shr.un
        conv.i2
        ldarg 0x0003
        ldarg.s 0x03
        rem.un
        ldloc 0x0002
        conv.u2
        ldloc.s 0x02
        rem.un
        clt
        cgt
        conv.ovf.u8.un
        ldarg 0x0001
        starg.s 0x01
        ldc.r8 float64(0xa7450abcb249d654)
        ckfinite
        neg
        pop
        mul.ovf
        pop
        brtrue IL_005d
        ldc.r8 float64(0xd997fc4323d4242e)
        pop

IL_005d:
        ldc.r8 float64(0x4ce121de0d887bd3)
        ckfinite
        neg
        neg
        ldc.r8 float64(0x7ecd50b46af15c38)
        ckfinite
        neg
        neg
        ldc.r8 float64(0x731b56a3f8d0df42)
        neg
        conv.r.un
        conv.i8
        not
        neg
        ldloc 0x0000
        ldloc.s 0x00
        shl
        ldarg 0x0003
        ldloc.s 0x00
        conv.r.un
        ldc.r8 float64(0xd7e6bb0db40c537d)
        ceq
        div.un
        or
        shl
        ldarg.s 0x01
        conv.ovf.i8.un
        neg
        ldarg 0x0001
        ldloc 0x0001
        shr
        add.ovf
        starg 0x0001
        starg 0x0001
        ceq
        ret
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
