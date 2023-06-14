// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This test checks implicit floating point and small type casts in IL

.assembly _
{
    .permissionset reqmin = (
        2e 01 80 8a 53 79 73 74 65 6d 2e 53 65 63 75 72
        69 74 79 2e 50 65 72 6d 69 73 73 69 6f 6e 73 2e
        53 65 63 75 72 69 74 79 50 65 72 6d 69 73 73 69
        6f 6e 41 74 74 72 69 62 75 74 65 2c 20 53 79 73
        74 65 6d 2e 52 75 6e 74 69 6d 65 2c 20 56 65 72
        73 69 6f 6e 3d 37 2e 30 2e 30 2e 30 2c 20 43 75
        6c 74 75 72 65 3d 6e 65 75 74 72 61 6c 2c 20 50
        75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e 3d 62 30
        33 66 35 66 37 66 31 31 64 35 30 61 33 61 15 01
        54 02 10 53 6b 69 70 56 65 72 69 66 69 63 61 74
        69 6f 6e 01
    )
    .hash algorithm 0x00008004 // SHA1
    .ver 0:0:0:0
}

.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi abstract sealed beforefieldinit Program
    extends [System.Runtime]System.Object
{
    // Methods
    .method public hidebysig static
        int32 Main () cil managed
    {
        // Method begins at RVA 0x2068
        // Code size 263 (0x107)
        .maxstack 2
        .locals init (
            [0] int8 sb,
            [1] int16 s,
            [2] int32 exitCode
        )

        IL_0000: ldc.i4.m1
        IL_0001: stloc.0
        IL_0002: ldc.i4.m1
        IL_0003: stloc.1
        IL_0004: ldc.i4.0
        IL_0005: stloc.2
        IL_0006: ldc.r8 1
        IL_000b: call !!1 Unsafe::BitCast<float32, int32>(!!0)
        IL_0010: call !!0 Program::NoInline<int32>(!!0)
        IL_0015: ldc.i4 1065353216
        IL_001a: bne.un.s IL_0021

        IL_001c: ldloc.2
        IL_001d: ldc.i4.s 10
        IL_001f: add
        IL_0020: stloc.2

        IL_0021: ldc.r4 1
        IL_002a: call !!1 Unsafe::BitCast<float64, int64>(!!0)
        IL_002f: call !!0 Program::NoInline<int64>(!!0)
        IL_0034: ldc.i8 4607182418800017408
        IL_003d: bne.un.s IL_0044

        IL_003f: ldloc.2
        IL_0040: ldc.i4.s 10
        IL_0042: add
        IL_0043: stloc.2

        IL_0044: ldc.r8 1
        IL_0049: call !!0 Program::NoInline<float64>(!!0)
        IL_004e: call !!1 Unsafe::BitCast<float32, int32>(!!0)
        IL_0053: call !!0 Program::NoInline<int32>(!!0)
        IL_0058: ldc.i4 1065353216
        IL_005d: bne.un.s IL_0064

        IL_005f: ldloc.2
        IL_0060: ldc.i4.s 10
        IL_0062: add
        IL_0063: stloc.2

        IL_0064: ldc.r4 1
        IL_0069: call !!0 Program::NoInline<float32>(!!0)
        IL_006f: call !!1 Unsafe::BitCast<float64, int64>(!!0)
        IL_0074: call !!0 Program::NoInline<int64>(!!0)
        IL_0079: ldc.i8 4607182418800017408
        IL_0082: bne.un.s IL_0089

        IL_0084: ldloc.2
        IL_0085: ldc.i4.s 10
        IL_0087: add
        IL_0088: stloc.2

        IL_0089: ldloca.s 0
        IL_008b: conv.u
        IL_008c: call !!0* Program::NoInline<int8>(!!0*)
        IL_0091: ldind.i1
        IL_0092: call !!1 Unsafe::BitCast<uint8, uint8>(!!0)
        IL_0097: conv.u8
        IL_0098: call !!0 Program::NoInline<int64>(!!0)
        IL_009d: ldc.i4 255
        IL_00a2: conv.i8
        IL_00a3: bne.un.s IL_00aa

        IL_00a5: ldloc.2
        IL_00a6: ldc.i4.s 15
        IL_00a8: add
        IL_00a9: stloc.2

        IL_00aa: ldloca.s 1
        IL_00ac: conv.u
        IL_00ad: call !!0* Program::NoInline<int16>(!!0*)
        IL_00b2: ldind.i2
        IL_00b3: call !!1 Unsafe::BitCast<uint16, uint16>(!!0)
        IL_00b8: conv.u8
        IL_00b9: call !!0 Program::NoInline<int64>(!!0)
        IL_00be: ldc.i4 65535
        IL_00c3: conv.i8
        IL_00c4: bne.un.s IL_00cb

        IL_00c6: ldloc.2
        IL_00c7: ldc.i4.s 15
        IL_00c9: add
        IL_00ca: stloc.2

        IL_00cb: ldloca.s 0
        IL_00cd: conv.u
        IL_00ce: call !!0* Program::NoInline<uint8>(!!0*)
        IL_00d3: ldind.u1
        IL_00d4: call !!1 Unsafe::BitCast<int8, int8>(!!0)
        IL_00d9: conv.i8
        IL_00da: call !!0 Program::NoInline<int64>(!!0)
        IL_00df: ldc.i4.m1
        IL_00e0: conv.i8
        IL_00e1: bne.un.s IL_00e8

        IL_00e3: ldloc.2
        IL_00e4: ldc.i4.s 15
        IL_00e6: add
        IL_00e7: stloc.2

        IL_00e8: ldloca.s 1
        IL_00ea: conv.u
        IL_00eb: call !!0* Program::NoInline<uint16>(!!0*)
        IL_00f0: ldind.u2
        IL_00f1: call !!1 Unsafe::BitCast<int16, int16>(!!0)
        IL_00f6: conv.i8
        IL_00f7: call !!0 Program::NoInline<int64>(!!0)
        IL_00fc: ldc.i4.m1
        IL_00fd: conv.i8
        IL_00fe: bne.un.s IL_0105

        IL_0100: ldloc.2
        IL_0101: ldc.i4.s 15
        IL_0103: add
        IL_0104: stloc.2

        IL_0105: ldloc.2
        IL_0106: ret
    } // end of method Program::Main

    .method private hidebysig static
        !!T NoInline<T> (
            !!T 'value'
        ) cil managed noinlining
    {
        // Method begins at RVA 0x217b
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: ret
    } // end of method Program::NoInline

    .method private hidebysig static
        !!T* NoInline<T> (
            !!T* 'value'
        ) cil managed noinlining
    {
        // Method begins at RVA 0x217b
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: ret
    } // end of method Program::NoInline

} // end of class Program
