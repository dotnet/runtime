// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly 'DevDiv_471670'{}
.assembly extern xunit.core {}
.class ILGEN_CLASS
{
    .method static bool ILGEN_METHOD(float64, char, unsigned int32)
    {
        .maxstack  65535
        .locals init (float64, native unsigned int, unsigned int16, native int, unsigned int16, unsigned int8, int64, float64)

        ldarg.s 0x00
        conv.ovf.u2
        ldc.i8 0x5674a8efc99eac21
        pop
        ldloc.s 0x02
        conv.ovf.u1.un
        conv.ovf.u8.un
        conv.u8
        conv.r4
        pop
        ldarg.s 0x02
        bgt IL_0037
        ldloc.s 0x06
        conv.ovf.u8.un
        ldloc 0x0006
        clt.un
        neg
        brtrue IL_002b
        nop
        
    IL_002b:
        nop
        ldloc 0x0003
        ldloc 0x0005
        clt
        pop

    IL_0037:
        ldloc.s 0x04
        brtrue IL_003f
        nop

    IL_003f:
        ldloc 0x0003
        conv.ovf.i2.un
        ldarg.s 0x01
        conv.r8
        ldloc.s 0x00
        ckfinite
        mul
        conv.r4
        ldloc.s 0x00
        conv.ovf.u8.un
        conv.r.un
        clt
        conv.i8
        conv.i4
        add.ovf
        starg.s 0x02
        ldc.i8 0x953f9661d4ec7f53
        neg
        neg
        ldloc.s 0x06
        or
        neg
        conv.r.un
        ldc.i4 0x2f6100eb
        conv.r.un
        neg
        stloc 0x0000
        ldarg 0x0000
        neg
        neg
        ldloc.s 0x07
        neg
        conv.r.un
        add
        cgt
        ret
    }

    .method static int32 Main()
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint

        .try
        {
            ldc.r8 0
            ldc.i4 0
            dup
            call bool ILGEN_CLASS::ILGEN_METHOD(float64, char, unsigned int32)
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
