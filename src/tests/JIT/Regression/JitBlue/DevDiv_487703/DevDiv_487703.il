// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib {}
.assembly ILGEN_MODULE {}

.class ILGEN_CLASS
{
    .method static native int ILGEN_METHOD(unsigned int8, int64, native int, unsigned int16)
    {
        .maxstack 65535
        .locals init (int16, native unsigned int, int16)

        ldarg 0x0001
        not
        not
        ldloc 0x0001
        ldc.i4 0x2355d706
        shr.un
        conv.ovf.u
        shl
        conv.r4
        conv.ovf.i8.un
        ldarg.s 0x01
        bgt IL_0046
        ldc.r8 float64(0x79823c520c130b7d)
        ldc.r8 float64(0x51258d8dce722010)
        clt.un
        conv.ovf.u8
        conv.ovf.u8
        conv.ovf.u8
        starg 0x0001
        ldc.i8 0xb5b97539501b5ea7
        ldarg.s 0x01
        rem.un
        starg 0x0001

IL_0046:
        ldarg.s 0x01
        conv.r8
        ldc.r8 float64(0x99a91c7c78b49624)
        rem
        ldc.r8 float64(0xb5e326c6a003a09f)
        rem
        ldc.r8 float64(0x2a45f60bdc59b68d)
        conv.r4
        ckfinite
        cgt
        brtrue IL_0070
        nop

IL_0070:
        ldc.r8 float64(0x7c6a79c2ec1fbbd6)
        ldc.r8 float64(0xd9d5979e677611ac)
        neg
        rem
        ldc.r8 float64(0xec1c1addb2cd8fa9)
        ckfinite
        rem
        conv.r8
        ldloc.s 0x01
        conv.r.un
        div
        conv.r.un
        conv.ovf.i8
        brtrue IL_00a1
        ldloc.s 0x01
        stloc 0x0000

IL_00a1:
        ldarg.s 0x00
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
