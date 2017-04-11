// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib{}
.assembly ILGEN_MODULE{}
.class ILGEN_CLASS
{
    .method static unsigned int8 ILGEN_METHOD(int32)
    {
        .maxstack  65535
        .locals init (int16, char)
        IL_0000: ldc.r8 float64(0x7c9006f1fe68a964)
        IL_0009: conv.r4
        IL_000a: conv.ovf.u4
        IL_000b: nop
        IL_000c: conv.ovf.i8.un
        IL_000d: conv.ovf.i8.un
        IL_000e: ldc.i8 0xa152fc5b9fbc6fbc
        IL_0017: ldc.i8 0x5a9c2f46c68a4c09
        IL_0020: cgt.un
        IL_0022: shr
        IL_0023: conv.ovf.i8
        IL_0024: conv.u
        IL_0025: ret
    }

    .method static int32 Main()
    {
        .entrypoint

        .try
        {
            ldc.i4 0
            call unsigned int8 ILGEN_CLASS::ILGEN_METHOD(int32)
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
