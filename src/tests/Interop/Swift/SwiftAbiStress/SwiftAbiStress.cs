// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using Xunit;

public class SwiftAbiStress
{
    private const string SwiftLib = "libSwiftAbiStress.dylib";

    [StructLayout(LayoutKind.Sequential, Size = 14)]
    struct F0_S0
    {
        public double F0;
        public uint F1;
        public ushort F2;

        public F0_S0(double f0, uint f1, ushort f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F0_S1
    {
        public ulong F0;

        public F0_S1(ulong f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F0_S2
    {
        public float F0;

        public F0_S2(float f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress10swiftFunc02a02a12a22a32a42a52a62a7Sis5Int16V_s5Int32Vs6UInt64Vs6UInt16VAA5F0_S0VAA0R3_S1Vs5UInt8VAA0R3_S2VtF")]
    private static extern nint SwiftFunc0(short a0, int a1, ulong a2, ushort a3, F0_S0 a4, F0_S1 a5, byte a6, F0_S2 a7);

    [Fact]
    public static void TestSwiftFunc0()
    {
        Console.Write("Running SwiftFunc0: ");
        long result = SwiftFunc0(-23758, 148652722, 3833542748216839160, 21987, new F0_S0(3425626963407448, 989224444, 55562), new F0_S1(1751696348434043356), 14, new F0_S2(1047842));
        Assert.Equal(-5199645484972017144, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 26)]
    struct F1_S0
    {
        public long F0;
        public double F1;
        public sbyte F2;
        public int F3;
        public ushort F4;

        public F1_S0(long f0, double f1, sbyte f2, int f3, ushort f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F1_S1
    {
        public byte F0;

        public F1_S1(byte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F1_S2
    {
        public short F0;

        public F1_S2(short f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress10swiftFunc12a02a12a22a3SiAA5F1_S0V_s5UInt8VAA0J3_S1VAA0J3_S2VtF")]
    private static extern nint SwiftFunc1(F1_S0 a0, byte a1, F1_S1 a2, F1_S2 a3);

    [Fact]
    public static void TestSwiftFunc1()
    {
        Console.Write("Running SwiftFunc1: ");
        long result = SwiftFunc1(new F1_S0(6106136698885217102, 6195715435808, 121, 676336729, 51621), 121, new F1_S1(101), new F1_S2(-11974));
        Assert.Equal(-5789188411070459345, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F2_S0
    {
        public nint F0;
        public nuint F1;

        public F2_S0(nint f0, nuint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 26)]
    struct F2_S1
    {
        public long F0;
        public int F1;
        public short F2;
        public long F3;
        public ushort F4;

        public F2_S1(long f0, int f1, short f2, long f3, ushort f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F2_S2_S0_S0
    {
        public nint F0;

        public F2_S2_S0_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F2_S2_S0
    {
        public F2_S2_S0_S0 F0;

        public F2_S2_S0(F2_S2_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F2_S2
    {
        public F2_S2_S0 F0;

        public F2_S2(F2_S2_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F2_S3
    {
        public byte F0;

        public F2_S3(byte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F2_S4
    {
        public int F0;
        public nuint F1;

        public F2_S4(int f0, nuint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F2_S5
    {
        public float F0;

        public F2_S5(float f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress10swiftFunc22a02a12a22a32a42a52a62a72a82a93a103a113a123a13Sis5Int64V_s5Int16Vs5Int32VAA5F2_S0Vs5UInt8VAvA0W3_S1VAA0W3_S2Vs6UInt16VSfAA0W3_S3VAA0W3_S4VAA0W3_S5VARtF")]
    private static extern nint SwiftFunc2(long a0, short a1, int a2, F2_S0 a3, byte a4, int a5, F2_S1 a6, F2_S2 a7, ushort a8, float a9, F2_S3 a10, F2_S4 a11, F2_S5 a12, long a13);

    [Fact]
    public static void TestSwiftFunc2()
    {
        Console.Write("Running SwiftFunc2: ");
        long result = SwiftFunc2(1467471118999515177, -1109, 1443466834, new F2_S0(unchecked((nint)8641951469425609828), unchecked((nuint)3263825339460718643)), 6, 42857709, new F2_S1(6855376760105631967, 2087467091, 25810, 2495195821026007124, 62146), new F2_S2(new F2_S2_S0(new F2_S2_S0_S0(unchecked((nint)561009218247569242)))), 46110, 7547287, new F2_S3(34), new F2_S4(203178131, unchecked((nuint)8676866947888134131)), new F2_S5(7890213), 5623254678629817168);
        Assert.Equal(-1831688667491861211, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F3_S0_S0
    {
        public nint F0;
        public uint F1;

        public F3_S0_S0(nint f0, uint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F3_S0
    {
        public sbyte F0;
        public F3_S0_S0 F1;
        public uint F2;

        public F3_S0(sbyte f0, F3_S0_S0 f1, uint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F3_S1
    {
        public long F0;
        public float F1;

        public F3_S1(long f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F3_S2
    {
        public float F0;

        public F3_S2(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F3_S3
    {
        public byte F0;
        public nint F1;

        public F3_S3(byte f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 14)]
    struct F3_S4
    {
        public nuint F0;
        public float F1;
        public ushort F2;

        public F3_S4(nuint f0, float f1, ushort f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F3_S5
    {
        public uint F0;
        public long F1;

        public F3_S5(uint f0, long f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 3)]
    struct F3_S6_S0
    {
        public short F0;
        public byte F1;

        public F3_S6_S0(short f0, byte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 5)]
    struct F3_S6
    {
        public F3_S6_S0 F0;
        public sbyte F1;
        public byte F2;

        public F3_S6(F3_S6_S0 f0, sbyte f1, byte f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F3_S7
    {
        public ulong F0;

        public F3_S7(ulong f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress10swiftFunc32a02a12a22a32a42a52a62a72a82a93a103a113a123a13S2i_AA5F3_S0VAA0T3_S1VSdSiAA0T3_S2VAA0T3_S3VAA0T3_S4VAA0T3_S5Vs6UInt16Vs5Int32VAA0T3_S6VSiAA0T3_S7VtF")]
    private static extern nint SwiftFunc3(nint a0, F3_S0 a1, F3_S1 a2, double a3, nint a4, F3_S2 a5, F3_S3 a6, F3_S4 a7, F3_S5 a8, ushort a9, int a10, F3_S6 a11, nint a12, F3_S7 a13);

    [Fact]
    public static void TestSwiftFunc3()
    {
        Console.Write("Running SwiftFunc3: ");
        long result = SwiftFunc3(unchecked((nint)3764414362291906102), new F3_S0(23, new F3_S0_S0(unchecked((nint)3007367655161186204), 549733154), 38928730), new F3_S1(338326426991485790, 7517271), 4025506815523052, unchecked((nint)431338169919855088), new F3_S2(7888763), new F3_S3(57, unchecked((nint)8933588466514096604)), new F3_S4(unchecked((nuint)7769316271655125502), 1663231, 27333), new F3_S5(887161443, 4368322322535461551), 32477, 948591564, new F3_S6(new F3_S6_S0(7033, 124), 67, 221), unchecked((nint)6195032215974632640), new F3_S7(4076570630190469380));
        Assert.Equal(-8840537967093155898, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 6)]
    struct F4_S0
    {
        public ushort F0;
        public short F1;
        public short F2;

        public F4_S0(ushort f0, short f1, short f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F4_S1_S0
    {
        public uint F0;

        public F4_S1_S0(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F4_S1
    {
        public F4_S1_S0 F0;
        public float F1;

        public F4_S1(F4_S1_S0 f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F4_S2_S0
    {
        public nint F0;

        public F4_S2_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F4_S2
    {
        public F4_S2_S0 F0;
        public nint F1;

        public F4_S2(F4_S2_S0 f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F4_S3
    {
        public ulong F0;
        public ulong F1;
        public long F2;

        public F4_S3(ulong f0, ulong f1, long f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress10swiftFunc42a02a12a22a32a42a52a62a72a82a93a103a113a123a133a143a15S2i_AA5F4_S0VSus6UInt64Vs4Int8VSdAA0V3_S1Vs5UInt8Vs5Int32Vs6UInt32VAvA0V3_S2Vs5Int16VSiAA0V3_S3VA4_tF")]
    private static extern nint SwiftFunc4(nint a0, F4_S0 a1, nuint a2, ulong a3, sbyte a4, double a5, F4_S1 a6, byte a7, int a8, uint a9, ulong a10, F4_S2 a11, short a12, nint a13, F4_S3 a14, uint a15);

    [Fact]
    public static void TestSwiftFunc4()
    {
        Console.Write("Running SwiftFunc4: ");
        long result = SwiftFunc4(unchecked((nint)7962207922494873063), new F4_S0(16887, 11193, 20997), unchecked((nuint)938043702598629976), 8692646626431098135, -16, 1244033228990732, new F4_S1(new F4_S1_S0(274421021), 7037264), 154, 1187166500, 1096514224, 7283010216047805604, new F4_S2(new F4_S2_S0(unchecked((nint)3285810526807361976)), unchecked((nint)2934841899954168407)), 3384, unchecked((nint)4857017836321530071), new F4_S3(9030480386017125399, 5466901523025762626, 3430278619936831574), 234522698);
        Assert.Equal(5366279618472372586, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F5_S0
    {
        public nuint F0;

        public F5_S0(nuint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress10swiftFunc52a02a12a22a3SiSu_s6UInt64Vs5UInt8VAA5F5_S0VtF")]
    private static extern nint SwiftFunc5(nuint a0, ulong a1, byte a2, F5_S0 a3);

    [Fact]
    public static void TestSwiftFunc5()
    {
        Console.Write("Running SwiftFunc5: ");
        long result = SwiftFunc5(unchecked((nuint)425569624776371773), 8077063517132296390, 126, new F5_S0(unchecked((nuint)8032431538406335990)));
        Assert.Equal(5832440388901373477, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 17)]
    struct F6_S0
    {
        public int F0;
        public nint F1;
        public byte F2;

        public F6_S0(int f0, nint f1, byte f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F6_S1
    {
        public nint F0;
        public float F1;

        public F6_S1(nint f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F6_S2_S0
    {
        public double F0;

        public F6_S2_S0(double f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F6_S2
    {
        public F6_S2_S0 F0;
        public ushort F1;

        public F6_S2(F6_S2_S0 f0, ushort f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F6_S3
    {
        public double F0;
        public double F1;
        public ulong F2;

        public F6_S3(double f0, double f1, ulong f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F6_S4
    {
        public sbyte F0;

        public F6_S4(sbyte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F6_S5
    {
        public short F0;

        public F6_S5(short f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress10swiftFunc62a02a12a22a32a42a52a62a72a82a93a103a113a123a133a14Sis5Int64V_AA5F6_S0VAA0V3_S1VSus5UInt8Vs5Int32VAA0V3_S2VSfs5Int16VAA0V3_S3Vs6UInt16VSds6UInt32VAA0V3_S4VAA0V3_S5VtF")]
    private static extern nint SwiftFunc6(long a0, F6_S0 a1, F6_S1 a2, nuint a3, byte a4, int a5, F6_S2 a6, float a7, short a8, F6_S3 a9, ushort a10, double a11, uint a12, F6_S4 a13, F6_S5 a14);

    [Fact]
    public static void TestSwiftFunc6()
    {
        Console.Write("Running SwiftFunc6: ");
        long result = SwiftFunc6(7742402881449217499, new F6_S0(158138445, unchecked((nint)4280990415451108676), 220), new F6_S1(unchecked((nint)7698928046973811162), 478730), unchecked((nuint)7348396082620937303), 76, 638113630, new F6_S2(new F6_S2_S0(55341051405503), 61378), 8235930, -20241, new F6_S3(318363825012010, 3586735152618866, 6630554942616673404), 46432, 744827194985602, 1973021571, new F6_S4(103), new F6_S5(-5345));
        Assert.Equal(-8871753131984133391, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F7_S0
    {
        public short F0;
        public nint F1;

        public F7_S0(short f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F7_S1
    {
        public byte F0;

        public F7_S1(byte f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress10swiftFunc72a02a12a22a32a42a5Sis5Int64V_Sis5UInt8VAA5F7_S0VAA0N3_S1Vs6UInt32VtF")]
    private static extern nint SwiftFunc7(long a0, nint a1, byte a2, F7_S0 a3, F7_S1 a4, uint a5);

    [Fact]
    public static void TestSwiftFunc7()
    {
        Console.Write("Running SwiftFunc7: ");
        long result = SwiftFunc7(6953928391541094904, unchecked((nint)2531714261502554653), 224, new F7_S0(14482, unchecked((nint)4704842847707480837)), new F7_S1(148), 659764805);
        Assert.Equal(5963731324167739917, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F8_S0
    {
        public int F0;

        public F8_S0(int f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress10swiftFunc82a02a12a22a32a42a5Sis6UInt16V_SuAJs6UInt64VAA5F8_S0VALtF")]
    private static extern nint SwiftFunc8(ushort a0, nuint a1, ushort a2, ulong a3, F8_S0 a4, ulong a5);

    [Fact]
    public static void TestSwiftFunc8()
    {
        Console.Write("Running SwiftFunc8: ");
        long result = SwiftFunc8(48505, unchecked((nuint)8758330817072549915), 7130, 4163773298933598697, new F8_S0(1934119180), 2843311260726166700);
        Assert.Equal(1919194302322813426, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F9_S0
    {
        public double F0;

        public F9_S0(double f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F9_S1
    {
        public int F0;

        public F9_S1(int f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress10swiftFunc92a02a12a22a32a42a5Sis5Int64V_SfAA5F9_S0Vs6UInt16VAA0M3_S1VANtF")]
    private static extern nint SwiftFunc9(long a0, float a1, F9_S0 a2, ushort a3, F9_S1 a4, ushort a5);

    [Fact]
    public static void TestSwiftFunc9()
    {
        Console.Write("Running SwiftFunc9: ");
        long result = SwiftFunc9(3214937834123081267, 6846768, new F9_S0(1713527158921541), 25670, new F9_S1(1650872599), 39910);
        Assert.Equal(-5878079645235476214, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F10_S0
    {
        public long F0;
        public uint F1;

        public F10_S0(long f0, uint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F10_S1
    {
        public float F0;
        public byte F1;
        public nuint F2;

        public F10_S1(float f0, byte f1, nuint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F10_S2
    {
        public nuint F0;
        public ulong F1;

        public F10_S2(nuint f0, ulong f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F10_S3
    {
        public float F0;

        public F10_S3(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F10_S4
    {
        public long F0;

        public F10_S4(long f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc102a02a12a22a32a42a52a62a72a82a93a103a113a123a133a143a153a163a173a18Sis6UInt16V_AwA6F10_S0Vs6UInt64VSfs4Int8Vs5Int64VA_A3_Sfs5Int32VA5_A3_A_AA0Z3_S1VA3_AA0Z3_S2VAA0Z3_S3VAA0Z3_S4VtF")]
    private static extern nint SwiftFunc10(ushort a0, ushort a1, F10_S0 a2, ulong a3, float a4, sbyte a5, long a6, ulong a7, long a8, float a9, int a10, int a11, long a12, ulong a13, F10_S1 a14, long a15, F10_S2 a16, F10_S3 a17, F10_S4 a18);

    [Fact]
    public static void TestSwiftFunc10()
    {
        Console.Write("Running SwiftFunc10: ");
        long result = SwiftFunc10(57914, 11968, new F10_S0(155502634291755209, 2096010440), 1373054541331378384, 2401784, -16, 9038689080810964859, 521869082023571496, 8919173990791765137, 4890513, 1113752036, 1477591037, 1463349953238439103, 7521124889381630793, new F10_S1(620783, 33, unchecked((nuint)1209731409858919135)), 1560688600815438014, new F10_S2(unchecked((nuint)2244178273746563479), 4252696983313269084), new F10_S3(6539550), new F10_S4(1264398289929487498));
        Assert.Equal(-5714135075575530569, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F11_S0
    {
        public short F0;
        public sbyte F1;
        public ulong F2;
        public short F3;

        public F11_S0(short f0, sbyte f1, ulong f2, short f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F11_S1
    {
        public nuint F0;

        public F11_S1(nuint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F11_S2
    {
        public short F0;

        public F11_S2(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F11_S3_S0
    {
        public float F0;

        public F11_S3_S0(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F11_S3
    {
        public F11_S3_S0 F0;

        public F11_S3(F11_S3_S0 f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc112a02a12a22a32a42a52a62a72a82a93a103a113a12S2i_s6UInt64Vs5UInt8Vs5Int16VAA6F11_S0VAA0V3_S1Vs6UInt16VSdSis6UInt32VAA0V3_S2VAA0V3_S3Vs4Int8VtF")]
    private static extern nint SwiftFunc11(nint a0, ulong a1, byte a2, short a3, F11_S0 a4, F11_S1 a5, ushort a6, double a7, nint a8, uint a9, F11_S2 a10, F11_S3 a11, sbyte a12);

    [Fact]
    public static void TestSwiftFunc11()
    {
        Console.Write("Running SwiftFunc11: ");
        long result = SwiftFunc11(unchecked((nint)6199025647502478201), 6507965430585517144, 205, -31066, new F11_S0(-8843, -2, 7915533514001114122, -3518), new F11_S1(unchecked((nuint)690496938384964820)), 10269, 3817195039757571, unchecked((nint)4394294464475321144), 1182247681, new F11_S2(22246), new F11_S3(new F11_S3_S0(3714370)), 93);
        Assert.Equal(946399036611801834, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F12_S0
    {
        public uint F0;

        public F12_S0(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F12_S1
    {
        public byte F0;

        public F12_S1(byte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F12_S2
    {
        public nuint F0;

        public F12_S2(nuint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc122a02a12a22a32a42a52a62a72a82a93a103a11Sis5UInt8V_s5Int32VAA6F12_S0Vs4Int8VAA0T3_S1VAA0T3_S2Vs6UInt32Vs5Int16VA2VA0_APtF")]
    private static extern nint SwiftFunc12(byte a0, int a1, F12_S0 a2, sbyte a3, F12_S1 a4, F12_S2 a5, uint a6, short a7, sbyte a8, sbyte a9, uint a10, byte a11);

    [Fact]
    public static void TestSwiftFunc12()
    {
        Console.Write("Running SwiftFunc12: ");
        long result = SwiftFunc12(233, 123593469, new F12_S0(1950949830), -122, new F12_S1(47), new F12_S2(unchecked((nuint)2600645483988824242)), 307825058, -49, -98, -5, 1582160629, 26);
        Assert.Equal(102839812138332997, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F13_S0_S0_S0
    {
        public ulong F0;

        public F13_S0_S0_S0(ulong f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F13_S0_S0
    {
        public F13_S0_S0_S0 F0;

        public F13_S0_S0(F13_S0_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F13_S0
    {
        public sbyte F0;
        public F13_S0_S0 F1;

        public F13_S0(sbyte f0, F13_S0_S0 f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F13_S1_S0
    {
        public ulong F0;

        public F13_S1_S0(ulong f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F13_S1
    {
        public F13_S1_S0 F0;

        public F13_S1(F13_S1_S0 f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc132a02a12a22a32a42a5Sis4Int8V_SdAA6F13_S0VAA0M3_S1VAJSdtF")]
    private static extern nint SwiftFunc13(sbyte a0, double a1, F13_S0 a2, F13_S1 a3, sbyte a4, double a5);

    [Fact]
    public static void TestSwiftFunc13()
    {
        Console.Write("Running SwiftFunc13: ");
        long result = SwiftFunc13(-6, 2395768328620295, new F13_S0(44, new F13_S0_S0(new F13_S0_S0_S0(2383685413668225247))), new F13_S1(new F13_S1_S0(5663941717310331870)), -9, 815761320969512);
        Assert.Equal(-6209025030118540066, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F14_S0
    {
        public nint F0;

        public F14_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc142a02a12a22a32a4Sis4Int8V_SiAA6F14_S0VSfSutF")]
    private static extern nint SwiftFunc14(sbyte a0, nint a1, F14_S0 a2, float a3, nuint a4);

    [Fact]
    public static void TestSwiftFunc14()
    {
        Console.Write("Running SwiftFunc14: ");
        long result = SwiftFunc14(-78, unchecked((nint)2423976036967433837), new F14_S0(unchecked((nint)2836433146306492236)), 4916388, unchecked((nuint)7716581850692162517));
        Assert.Equal(1206847964913124869, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F15_S0
    {
        public float F0;
        public short F1;
        public byte F2;
        public long F3;
        public double F4;

        public F15_S0(float f0, short f1, byte f2, long f3, double f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F15_S1_S0
    {
        public sbyte F0;

        public F15_S1_S0(sbyte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F15_S1
    {
        public uint F0;
        public F15_S1_S0 F1;
        public nuint F2;
        public int F3;

        public F15_S1(uint f0, F15_S1_S0 f1, nuint f2, int f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc152a02a12a22a32a42a52a62a7SiAA6F15_S0V_s6UInt64Vs6UInt32VSuANs5Int16VAA0N3_S1Vs5Int64VtF")]
    private static extern nint SwiftFunc15(F15_S0 a0, ulong a1, uint a2, nuint a3, ulong a4, short a5, F15_S1 a6, long a7);

    [Fact]
    public static void TestSwiftFunc15()
    {
        Console.Write("Running SwiftFunc15: ");
        long result = SwiftFunc15(new F15_S0(2392622, -22089, 69, 7123929674797968229, 2951758117520631), 171173680452593621, 357397954, unchecked((nuint)6020399741996935792), 3793854189677149082, 14438, new F15_S1(1572107355, new F15_S1_S0(109), unchecked((nuint)4381395046734445050), 2038949453), 9134476964305239477);
        Assert.Equal(8801999574220262235, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F16_S0_S0
    {
        public double F0;

        public F16_S0_S0(double f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F16_S0
    {
        public nint F0;
        public nint F1;
        public F16_S0_S0 F2;

        public F16_S0(nint f0, nint f1, F16_S0_S0 f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F16_S1
    {
        public short F0;
        public ulong F1;
        public uint F2;

        public F16_S1(short f0, ulong f1, uint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F16_S2
    {
        public byte F0;
        public ulong F1;
        public float F2;

        public F16_S2(byte f0, ulong f1, float f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F16_S3
    {
        public int F0;

        public F16_S3(int f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc162a02a12a22a32a42a52a6Sis6UInt64V_AA6F16_S0VAA0N3_S1Vs6UInt16Vs5Int16VAA0N3_S2VAA0N3_S3VtF")]
    private static extern nint SwiftFunc16(ulong a0, F16_S0 a1, F16_S1 a2, ushort a3, short a4, F16_S2 a5, F16_S3 a6);

    [Fact]
    public static void TestSwiftFunc16()
    {
        Console.Write("Running SwiftFunc16: ");
        long result = SwiftFunc16(3875678837451096765, new F16_S0(unchecked((nint)4720149202348788086), unchecked((nint)7476511841079774603), new F16_S0_S0(1008066799213144)), new F16_S1(3085, 11417298712821513, 12161200), 257, 7667, new F16_S2(186, 2771425808859711833, 3778779), new F16_S3(146689072));
        Assert.Equal(2726423189537230293, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F17_S0
    {
        public short F0;

        public F17_S0(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F17_S1
    {
        public long F0;
        public nuint F1;
        public ulong F2;

        public F17_S1(long f0, nuint f1, ulong f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F17_S2
    {
        public sbyte F0;

        public F17_S2(sbyte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F17_S3
    {
        public sbyte F0;
        public uint F1;

        public F17_S3(sbyte f0, uint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F17_S4
    {
        public ulong F0;

        public F17_S4(ulong f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F17_S5
    {
        public long F0;

        public F17_S5(long f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc172a02a12a22a32a42a52a62a72a82a9SiAA6F17_S0V_s4Int8VAA0P3_S1VAPSuAA0P3_S2Vs5Int64VAA0P3_S3VAA0P3_S4VAA0P3_S5VtF")]
    private static extern nint SwiftFunc17(F17_S0 a0, sbyte a1, F17_S1 a2, sbyte a3, nuint a4, F17_S2 a5, long a6, F17_S3 a7, F17_S4 a8, F17_S5 a9);

    [Fact]
    public static void TestSwiftFunc17()
    {
        Console.Write("Running SwiftFunc17: ");
        long result = SwiftFunc17(new F17_S0(-25916), -37, new F17_S1(927673990059785474, unchecked((nuint)4067467819275701282), 4736163781163880654), 70, unchecked((nuint)1236364146053271187), new F17_S2(54), 6452671878605914679, new F17_S3(17, 1066187627), new F17_S4(961451227454237536), new F17_S5(8720978516408944945));
        Assert.Equal(6084200789584610530, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F18_S0_S0
    {
        public ushort F0;
        public short F1;

        public F18_S0_S0(ushort f0, short f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F18_S0
    {
        public uint F0;
        public F18_S0_S0 F1;
        public ushort F2;

        public F18_S0(uint f0, F18_S0_S0 f1, ushort f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F18_S1
    {
        public nint F0;
        public nint F1;

        public F18_S1(nint f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F18_S2_S0
    {
        public ulong F0;

        public F18_S2_S0(ulong f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F18_S2
    {
        public ulong F0;
        public long F1;
        public byte F2;
        public F18_S2_S0 F3;

        public F18_S2(ulong f0, long f1, byte f2, F18_S2_S0 f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc182a02a12a22a32a42a52a62a72a8Sis5UInt8V_SdAA6F18_S0VAA0P3_S1Vs6UInt16Vs5Int64Vs6UInt64VAA0P3_S2VAWtF")]
    private static extern nint SwiftFunc18(byte a0, double a1, F18_S0 a2, F18_S1 a3, ushort a4, long a5, ulong a6, F18_S2 a7, ulong a8);

    [Fact]
    public static void TestSwiftFunc18()
    {
        Console.Write("Running SwiftFunc18: ");
        long result = SwiftFunc18(153, 2414022997411914, new F18_S0(795806912, new F18_S0_S0(63552, 11471), 47960), new F18_S1(unchecked((nint)6143080814824714071), unchecked((nint)2654471745636317319)), 51304, 4455723326879920366, 6215563249078191014, new F18_S2(7357905541817922655, 8124331887393558663, 146, new F18_S2_S0(8835007006958775606)), 1308697068118476706);
        Assert.Equal(-1238401591549550590, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F19_S0
    {
        public nint F0;
        public double F1;
        public ushort F2;

        public F19_S0(nint f0, double f1, ushort f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc192a02a12a2SiSu_AA6F19_S0Vs5Int16VtF")]
    private static extern nint SwiftFunc19(nuint a0, F19_S0 a1, short a2);

    [Fact]
    public static void TestSwiftFunc19()
    {
        Console.Write("Running SwiftFunc19: ");
        long result = SwiftFunc19(unchecked((nuint)2063900917075180131), new F19_S0(unchecked((nint)7420139040061411172), 4412763638361702, 18542), 32656);
        Assert.Equal(-3737785273912016840, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F20_S0
    {
        public ushort F0;
        public sbyte F1;
        public ulong F2;
        public uint F3;
        public ulong F4;

        public F20_S0(ushort f0, sbyte f1, ulong f2, uint f3, ulong f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F20_S1
    {
        public long F0;

        public F20_S1(long f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc202a02a12a22a32a42a52a6Sis4Int8V_AA6F20_S0Vs6UInt64VSiAA0N3_S1Vs5UInt8Vs5Int64VtF")]
    private static extern nint SwiftFunc20(sbyte a0, F20_S0 a1, ulong a2, nint a3, F20_S1 a4, byte a5, long a6);

    [Fact]
    public static void TestSwiftFunc20()
    {
        Console.Write("Running SwiftFunc20: ");
        long result = SwiftFunc20(-90, new F20_S0(13173, -56, 2350829658938201640, 1333911330, 2505424063423776138), 6738010084636609242, unchecked((nint)819908193119917708), new F20_S1(1349820395385212287), 121, 3289915405437061252);
        Assert.Equal(550863197950258558, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F21_S0
    {
        public uint F0;

        public F21_S0(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F21_S1
    {
        public nint F0;
        public uint F1;
        public byte F2;
        public short F3;

        public F21_S1(nint f0, uint f1, byte f2, short f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 25)]
    struct F21_S2
    {
        public sbyte F0;
        public ulong F1;
        public long F2;
        public byte F3;

        public F21_S2(sbyte f0, ulong f1, long f2, byte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F21_S3
    {
        public double F0;
        public nint F1;

        public F21_S3(double f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc212a02a12a22a32a42a52a62a72a82a93a103a113a12Sis6UInt64V_s4Int8VSuSdSfSiAA6F21_S0VAA0U3_S1Vs6UInt16VAA0U3_S2Vs5UInt8VAA0U3_S3Vs5Int16VtF")]
    private static extern nint SwiftFunc21(ulong a0, sbyte a1, nuint a2, double a3, float a4, nint a5, F21_S0 a6, F21_S1 a7, ushort a8, F21_S2 a9, byte a10, F21_S3 a11, short a12);

    [Fact]
    public static void TestSwiftFunc21()
    {
        Console.Write("Running SwiftFunc21: ");
        long result = SwiftFunc21(5269012897287813953, -91, unchecked((nuint)1201479654570648238), 3289259914874957, 6706247, unchecked((nint)5524961485867187694), new F21_S0(1842933651), new F21_S1(unchecked((nint)3105907069529682628), 1409834375, 228, 24264), 54652, new F21_S2(-49, 3442352645827709069, 7249278047379449391, 213), 207, new F21_S3(3802489474747093, unchecked((nint)7550982300494612851)), -25738);
        Assert.Equal(1242333410237260188, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F22_S0
    {
        public ushort F0;
        public uint F1;
        public short F2;
        public float F3;

        public F22_S0(ushort f0, uint f1, short f2, float f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F22_S1
    {
        public ushort F0;
        public sbyte F1;
        public byte F2;
        public nint F3;
        public nint F4;

        public F22_S1(ushort f0, sbyte f1, byte f2, nint f3, nint f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F22_S2_S0
    {
        public sbyte F0;

        public F22_S2_S0(sbyte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 14)]
    struct F22_S2
    {
        public int F0;
        public int F1;
        public uint F2;
        public byte F3;
        public F22_S2_S0 F4;

        public F22_S2(int f0, int f1, uint f2, byte f3, F22_S2_S0 f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F22_S3
    {
        public short F0;
        public double F1;
        public double F2;
        public int F3;

        public F22_S3(short f0, double f1, double f2, int f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc222a02a12a22a32a42a52a62a7Sis4Int8V_s5Int32VAA6F22_S0VAA0P3_S1VAA0P3_S2Vs6UInt64VAA0P3_S3VSutF")]
    private static extern nint SwiftFunc22(sbyte a0, int a1, F22_S0 a2, F22_S1 a3, F22_S2 a4, ulong a5, F22_S3 a6, nuint a7);

    [Fact]
    public static void TestSwiftFunc22()
    {
        Console.Write("Running SwiftFunc22: ");
        long result = SwiftFunc22(-57, 637612850, new F22_S0(39888, 420817324, 7562, 2757302), new F22_S1(61019, -94, 94, unchecked((nint)2606601177110916370), unchecked((nint)5843896711210899037)), new F22_S2(400565495, 1044629988, 1076814110, 26, new F22_S2_S0(-109)), 6520156438560424018, new F22_S3(8735, 4148868269582632, 2501928198596701, 1401343024), unchecked((nuint)5955700101477425475));
        Assert.Equal(-6205677027164766590, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 6)]
    struct F23_S0
    {
        public uint F0;
        public short F1;

        public F23_S0(uint f0, short f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F23_S1
    {
        public nuint F0;
        public uint F1;

        public F23_S1(nuint f0, uint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 17)]
    struct F23_S2
    {
        public double F0;
        public uint F1;
        public int F2;
        public byte F3;

        public F23_S2(double f0, uint f1, int f2, byte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc232a02a12a22a32a4SiAA6F23_S0V_AA0K3_S1VAA0K3_S2VSds6UInt64VtF")]
    private static extern nint SwiftFunc23(F23_S0 a0, F23_S1 a1, F23_S2 a2, double a3, ulong a4);

    [Fact]
    public static void TestSwiftFunc23()
    {
        Console.Write("Running SwiftFunc23: ");
        long result = SwiftFunc23(new F23_S0(119750622, -9202), new F23_S1(unchecked((nuint)2015683423731520384), 2106419422), new F23_S2(15243057156671, 484733224, 541045687, 128), 335968113268162, 4104726345028490471);
        Assert.Equal(-4893219516767457464, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F24_S0
    {
        public sbyte F0;
        public int F1;

        public F24_S0(sbyte f0, int f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F24_S1
    {
        public sbyte F0;

        public F24_S1(sbyte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F24_S2
    {
        public ushort F0;
        public short F1;
        public double F2;
        public nuint F3;

        public F24_S2(ushort f0, short f1, double f2, nuint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F24_S3
    {
        public nint F0;

        public F24_S3(nint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc242a02a12a22a32a42a5SiAA6F24_S0V_AA0L3_S1VAA0L3_S2VAA0L3_S3VSus6UInt32VtF")]
    private static extern nint SwiftFunc24(F24_S0 a0, F24_S1 a1, F24_S2 a2, F24_S3 a3, nuint a4, uint a5);

    [Fact]
    public static void TestSwiftFunc24()
    {
        Console.Write("Running SwiftFunc24: ");
        long result = SwiftFunc24(new F24_S0(-79, 1590520731), new F24_S1(-91), new F24_S2(20580, 5897, 4259258535235558, unchecked((nuint)5376883129922161134)), new F24_S3(unchecked((nint)6329816641466666679)), unchecked((nuint)749917486894435068), 588417470);
        Assert.Equal(2355459289566446436, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F25_S0_S0
    {
        public sbyte F0;

        public F25_S0_S0(sbyte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F25_S0
    {
        public float F0;
        public F25_S0_S0 F1;
        public uint F2;

        public F25_S0(float f0, F25_S0_S0 f1, uint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F25_S1
    {
        public short F0;
        public sbyte F1;
        public float F2;

        public F25_S1(short f0, sbyte f1, float f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F25_S2
    {
        public long F0;
        public ushort F1;

        public F25_S2(long f0, ushort f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F25_S3
    {
        public ulong F0;

        public F25_S3(ulong f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F25_S4
    {
        public ushort F0;

        public F25_S4(ushort f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc252a02a12a22a32a42a52a62a72a82a93a103a113a12SiSf_AA6F25_S0Vs5Int64Vs5UInt8VAA0S3_S1VSiAA0S3_S2Vs5Int32VA_Sus6UInt64VAA0S3_S3VAA0S3_S4VtF")]
    private static extern nint SwiftFunc25(float a0, F25_S0 a1, long a2, byte a3, F25_S1 a4, nint a5, F25_S2 a6, int a7, int a8, nuint a9, ulong a10, F25_S3 a11, F25_S4 a12);

    [Fact]
    public static void TestSwiftFunc25()
    {
        Console.Write("Running SwiftFunc25: ");
        long result = SwiftFunc25(7574050, new F25_S0(6812822, new F25_S0_S0(-56), 265762114), 8887316512771179060, 123, new F25_S1(-7776, 73, 1925304), unchecked((nint)6156508798007114044), new F25_S2(3356802028835066684, 63590), 1072499355, 1592861041, unchecked((nuint)7083962615260029068), 6662060345720879806, new F25_S3(3582316099656415385), new F25_S4(37071));
        Assert.Equal(3486557296564493762, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F26_S0
    {
        public double F0;

        public F26_S0(double f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc262a02a12a22a32a4Sis6UInt16V_Sds5Int64VAA6F26_S0Vs5UInt8VtF")]
    private static extern nint SwiftFunc26(ushort a0, double a1, long a2, F26_S0 a3, byte a4);

    [Fact]
    public static void TestSwiftFunc26()
    {
        Console.Write("Running SwiftFunc26: ");
        long result = SwiftFunc26(61060, 3605567452716741, 1495534128089493599, new F26_S0(1063426277848136), 89);
        Assert.Equal(5445852553218786939, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F27_S0_S0
    {
        public long F0;

        public F27_S0_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F27_S0
    {
        public ushort F0;
        public F27_S0_S0 F1;
        public double F2;

        public F27_S0(ushort f0, F27_S0_S0 f1, double f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 13)]
    struct F27_S1
    {
        public nint F0;
        public sbyte F1;
        public short F2;
        public byte F3;

        public F27_S1(nint f0, sbyte f1, short f2, byte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F27_S2
    {
        public ushort F0;

        public F27_S2(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F27_S3
    {
        public ulong F0;
        public uint F1;

        public F27_S3(ulong f0, uint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F27_S4
    {
        public byte F0;

        public F27_S4(byte f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc272a02a12a22a32a42a52a62a72a82a93a103a113a12SiAA6F27_S0V_S2ds4Int8VAsA0S3_S1Vs5Int16VAA0S3_S2VASs6UInt16VAA0S3_S3VAA0S3_S4Vs6UInt32VtF")]
    private static extern nint SwiftFunc27(F27_S0 a0, double a1, double a2, sbyte a3, sbyte a4, F27_S1 a5, short a6, F27_S2 a7, sbyte a8, ushort a9, F27_S3 a10, F27_S4 a11, uint a12);

    [Fact]
    public static void TestSwiftFunc27()
    {
        Console.Write("Running SwiftFunc27: ");
        long result = SwiftFunc27(new F27_S0(7130, new F27_S0_S0(6606060428339642921), 4122923031624866), 1451662996356727, 1529297186262631, 1, 24, new F27_S1(unchecked((nint)5075979081296734546), 75, -3781, 198), -26687, new F27_S2(53456), 90, 35194, new F27_S3(6318217926100193736, 1400016900), new F27_S4(11), 628995828);
        Assert.Equal(-5428774405932003643, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F28_S0
    {
        public double F0;
        public short F1;
        public double F2;
        public ulong F3;

        public F28_S0(double f0, short f1, double f2, ulong f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F28_S1
    {
        public nint F0;
        public uint F1;
        public ulong F2;
        public float F3;

        public F28_S1(nint f0, uint f1, ulong f2, float f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F28_S2
    {
        public double F0;
        public ulong F1;

        public F28_S2(double f0, ulong f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F28_S3
    {
        public short F0;
        public ulong F1;
        public double F2;
        public int F3;

        public F28_S3(short f0, ulong f1, double f2, int f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F28_S4
    {
        public nint F0;

        public F28_S4(nint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc282a02a12a22a32a42a52a62a72a82a93a103a113a12Sis5UInt8V_s6UInt16VAA6F28_S0VAA0U3_S1VAA0U3_S2Vs6UInt64Vs5Int32Vs5Int64VSdAsA0U3_S3VAA0U3_S4VSftF")]
    private static extern nint SwiftFunc28(byte a0, ushort a1, F28_S0 a2, F28_S1 a3, F28_S2 a4, ulong a5, int a6, long a7, double a8, ushort a9, F28_S3 a10, F28_S4 a11, float a12);

    [Fact]
    public static void TestSwiftFunc28()
    {
        Console.Write("Running SwiftFunc28: ");
        long result = SwiftFunc28(190, 17255, new F28_S0(3216710004509072, 9709, 4049245410019897, 6996716492380286220), new F28_S1(unchecked((nint)4097715616866617693), 539407084, 4626633991924578918, 1275504), new F28_S2(3574990895078933, 7178808315522215553), 4610456141729135855, 1303811396, 5390518172407783382, 4435699869971486, 62148, new F28_S3(22518, 4183064684428798988, 4007968538134666, 433839184), new F28_S4(unchecked((nint)4835639581253218785)), 778028);
        Assert.Equal(-2948821353897526623, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F29_S0
    {
        public int F0;
        public float F1;
        public short F2;

        public F29_S0(int f0, float f1, short f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F29_S1
    {
        public short F0;
        public sbyte F1;
        public nuint F2;

        public F29_S1(short f0, sbyte f1, nuint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F29_S2
    {
        public ushort F0;

        public F29_S2(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F29_S3
    {
        public long F0;
        public long F1;

        public F29_S3(long f0, long f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc292a02a12a22a32a42a52a62a72a82a93a103a113a123a133a143a15Sis4Int8V_AA6F29_S0Vs5Int32VSuAA0W3_S1Vs6UInt64VAA0W3_S2Vs5Int16Vs5Int64Vs6UInt32VA0_SiAA0W3_S3Vs5UInt8VATSdtF")]
    private static extern nint SwiftFunc29(sbyte a0, F29_S0 a1, int a2, nuint a3, F29_S1 a4, ulong a5, F29_S2 a6, short a7, long a8, uint a9, ulong a10, nint a11, F29_S3 a12, byte a13, sbyte a14, double a15);

    [Fact]
    public static void TestSwiftFunc29()
    {
        Console.Write("Running SwiftFunc29: ");
        long result = SwiftFunc29(-24, new F29_S0(1975390147, 2492976, -22918), 1918385726, unchecked((nuint)4330240195518051787), new F29_S1(20662, 37, unchecked((nuint)3480511823780639511)), 2969238117130521039, new F29_S2(39829), -21356, 4236774320019789885, 650424352, 974567590062881682, unchecked((nint)4949995943007509070), new F29_S3(6288374171493526635, 797442718847899480), 23, 47, 3112540527380411);
        Assert.Equal(-219723436366645712, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F30_S0
    {
        public nuint F0;
        public float F1;

        public F30_S0(nuint f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F30_S1
    {
        public ulong F0;
        public byte F1;
        public double F2;
        public nint F3;

        public F30_S1(ulong f0, byte f1, double f2, nint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F30_S2_S0
    {
        public short F0;
        public short F1;

        public F30_S2_S0(short f0, short f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F30_S2_S1
    {
        public long F0;

        public F30_S2_S1(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F30_S2
    {
        public F30_S2_S0 F0;
        public F30_S2_S1 F1;

        public F30_S2(F30_S2_S0 f0, F30_S2_S1 f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F30_S3
    {
        public sbyte F0;
        public byte F1;
        public ulong F2;
        public uint F3;

        public F30_S3(sbyte f0, byte f1, ulong f2, uint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F30_S4
    {
        public ushort F0;

        public F30_S4(ushort f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc302a02a12a22a32a42a52a62a72a82a93a103a113a12Sis6UInt16V_s5Int16VAqA6F30_S0VAA0U3_S1VAA0U3_S2Vs6UInt64Vs5Int32VSuAA0U3_S3VAqA0U3_S4Vs4Int8VtF")]
    private static extern nint SwiftFunc30(ushort a0, short a1, ushort a2, F30_S0 a3, F30_S1 a4, F30_S2 a5, ulong a6, int a7, nuint a8, F30_S3 a9, ushort a10, F30_S4 a11, sbyte a12);

    [Fact]
    public static void TestSwiftFunc30()
    {
        Console.Write("Running SwiftFunc30: ");
        long result = SwiftFunc30(16858, 2711, 33779, new F30_S0(unchecked((nuint)8711036551441957307), 109551), new F30_S1(5557074438983413757, 145, 1614350045039200, unchecked((nint)962570826922694431)), new F30_S2(new F30_S2_S0(-2145, 18987), new F30_S2_S1(3566641512072703431)), 4070388225227154205, 2068046267, unchecked((nuint)2683069104930642879), new F30_S3(82, 154, 4455096152847314924, 2054397471), 61158, new F30_S4(61860), -85);
        Assert.Equal(-6493337704322390178, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F31_S0
    {
        public nint F0;
        public float F1;
        public uint F2;
        public nint F3;

        public F31_S0(nint f0, float f1, uint f2, nint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc312a02a12a22a3Sis5Int64V_AA6F31_S0Vs6UInt32Vs6UInt64VtF")]
    private static extern nint SwiftFunc31(long a0, F31_S0 a1, uint a2, ulong a3);

    [Fact]
    public static void TestSwiftFunc31()
    {
        Console.Write("Running SwiftFunc31: ");
        long result = SwiftFunc31(854114380819209961, new F31_S0(unchecked((nint)8616284744785848913), 2817216, 1674385679, unchecked((nint)6375864278077977066)), 972945684, 1323893099763572702);
        Assert.Equal(5251289581384890505, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F32_S0
    {
        public short F0;
        public float F1;
        public long F2;

        public F32_S0(short f0, float f1, long f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F32_S1_S0
    {
        public nuint F0;

        public F32_S1_S0(nuint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F32_S1
    {
        public byte F0;
        public F32_S1_S0 F1;

        public F32_S1(byte f0, F32_S1_S0 f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F32_S2
    {
        public uint F0;
        public byte F1;
        public nuint F2;

        public F32_S2(uint f0, byte f1, nuint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F32_S3_S0
    {
        public nuint F0;

        public F32_S3_S0(nuint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F32_S3
    {
        public ulong F0;
        public F32_S3_S0 F1;
        public ulong F2;

        public F32_S3(ulong f0, F32_S3_S0 f1, ulong f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F32_S4
    {
        public double F0;
        public long F1;
        public long F2;
        public float F3;

        public F32_S4(double f0, long f1, long f2, float f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc322a02a12a22a32a42a52a62a72a82a93a10Sis6UInt64V_AA6F32_S0VSdAA0R3_S1VAA0R3_S2VAOSfAA0R3_S3VAA0R3_S4Vs6UInt32Vs5Int16VtF")]
    private static extern nint SwiftFunc32(ulong a0, F32_S0 a1, double a2, F32_S1 a3, F32_S2 a4, ulong a5, float a6, F32_S3 a7, F32_S4 a8, uint a9, short a10);

    [Fact]
    public static void TestSwiftFunc32()
    {
        Console.Write("Running SwiftFunc32: ");
        long result = SwiftFunc32(8029377143582007729, new F32_S0(17278, 7967601, 1978436908876178048), 1789368352608636, new F32_S1(255, new F32_S1_S0(unchecked((nuint)6244652548486446415))), new F32_S2(862868498, 29, unchecked((nuint)1969242341467623483)), 5279845618693914949, 1855163, new F32_S3(6102326739757366863, new F32_S3_S0(unchecked((nuint)8768252353660722957)), 3548360060427751308), new F32_S4(4443676345125115, 9168978488997364066, 3214391615557684463, 6052142), 1797618755, 17578);
        Assert.Equal(-6196943681215505326, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F33_S0
    {
        public sbyte F0;
        public byte F1;

        public F33_S0(sbyte f0, byte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F33_S1
    {
        public ushort F0;
        public byte F1;
        public long F2;

        public F33_S1(ushort f0, byte f1, long f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F33_S2_S0
    {
        public uint F0;

        public F33_S2_S0(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 34)]
    struct F33_S2
    {
        public F33_S2_S0 F0;
        public nuint F1;
        public float F2;
        public double F3;
        public ushort F4;

        public F33_S2(F33_S2_S0 f0, nuint f1, float f2, double f3, ushort f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F33_S3
    {
        public nuint F0;

        public F33_S3(nuint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc332a02a12a22a32a42a52a62a72a82a93a10SiSf_AA6F33_S0Vs6UInt64Vs5Int64VAA0Q3_S1Vs6UInt16VSuAwA0Q3_S2VAA0Q3_S3VSitF")]
    private static extern nint SwiftFunc33(float a0, F33_S0 a1, ulong a2, long a3, F33_S1 a4, ushort a5, nuint a6, ushort a7, F33_S2 a8, F33_S3 a9, nint a10);

    [Fact]
    public static void TestSwiftFunc33()
    {
        Console.Write("Running SwiftFunc33: ");
        long result = SwiftFunc33(7854986, new F33_S0(-88, 250), 5301409185013630861, 59840293674446659, new F33_S1(60084, 209, 8486520240421572730), 47187, unchecked((nuint)3062806578924156555), 27556, new F33_S2(new F33_S2_S0(2034603306), unchecked((nuint)8616790058647815090), 6520318, 4264637592867522, 45572), new F33_S3(unchecked((nuint)8100077493474466447)), unchecked((nint)4177526131236757728));
        Assert.Equal(7131040958707940402, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F34_S0
    {
        public byte F0;

        public F34_S0(byte f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc342a02a12a22a32a42a5Sis5Int64V_AA6F34_S0VS2us5UInt8VSdtF")]
    private static extern nint SwiftFunc34(long a0, F34_S0 a1, nuint a2, nuint a3, byte a4, double a5);

    [Fact]
    public static void TestSwiftFunc34()
    {
        Console.Write("Running SwiftFunc34: ");
        long result = SwiftFunc34(6297959268257433453, new F34_S0(152), unchecked((nuint)684867108943559069), unchecked((nuint)3028084738078866117), 52, 1123384931674176);
        Assert.Equal(-7354337608853973520, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F35_S0
    {
        public short F0;

        public F35_S0(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 3)]
    struct F35_S1_S0
    {
        public ushort F0;
        public sbyte F1;

        public F35_S1_S0(ushort f0, sbyte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F35_S1
    {
        public long F0;
        public F35_S1_S0 F1;
        public float F2;

        public F35_S1(long f0, F35_S1_S0 f1, float f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F35_S2
    {
        public ulong F0;
        public sbyte F1;
        public uint F2;
        public long F3;

        public F35_S2(ulong f0, sbyte f1, uint f2, long f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 5)]
    struct F35_S3_S0_S0
    {
        public uint F0;
        public byte F1;

        public F35_S3_S0_S0(uint f0, byte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F35_S3_S0
    {
        public ushort F0;
        public F35_S3_S0_S0 F1;
        public double F2;

        public F35_S3_S0(ushort f0, F35_S3_S0_S0 f1, double f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F35_S3
    {
        public F35_S3_S0 F0;
        public uint F1;

        public F35_S3(F35_S3_S0 f0, uint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F35_S4
    {
        public float F0;

        public F35_S4(float f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc352a02a12a22a32a42a52a62a72a82a93a10Sis5UInt8V_AA6F35_S0VA2oA0R3_S1Vs5Int32VAA0R3_S2VSis6UInt32VAA0R3_S3VAA0R3_S4VtF")]
    private static extern nint SwiftFunc35(byte a0, F35_S0 a1, byte a2, byte a3, F35_S1 a4, int a5, F35_S2 a6, nint a7, uint a8, F35_S3 a9, F35_S4 a10);

    [Fact]
    public static void TestSwiftFunc35()
    {
        Console.Write("Running SwiftFunc35: ");
        long result = SwiftFunc35(70, new F35_S0(-3405), 57, 4, new F35_S1(1893314071875920321, new F35_S1_S0(21188, -72), 1690358), 331400152, new F35_S2(629066911115913492, 24, 1741513272, 1738852017312447556), unchecked((nint)5964912267274635634), 745754721, new F35_S3(new F35_S3_S0(12969, new F35_S3_S0_S0(1922748035, 11), 1057686301404030), 1301219882), new F35_S4(4792810));
        Assert.Equal(8413899507614185381, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct F36_S0
    {
        public ulong F0;
        public sbyte F1;

        public F36_S0(ulong f0, sbyte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F36_S1
    {
        public long F0;
        public nuint F1;
        public nint F2;
        public int F3;

        public F36_S1(long f0, nuint f1, nint f2, int f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F36_S2
    {
        public nint F0;

        public F36_S2(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F36_S3_S0
    {
        public float F0;

        public F36_S3_S0(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F36_S3
    {
        public long F0;
        public sbyte F1;
        public F36_S3_S0 F2;

        public F36_S3(long f0, sbyte f1, F36_S3_S0 f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F36_S4
    {
        public nuint F0;
        public long F1;
        public double F2;
        public double F3;

        public F36_S4(nuint f0, long f1, double f2, double f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F36_S5
    {
        public byte F0;
        public byte F1;

        public F36_S5(byte f0, byte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F36_S6
    {
        public ushort F0;

        public F36_S6(ushort f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc362a02a12a22a32a42a52a62a72a82a93a103a11SiAA6F36_S0V_Sds6UInt64VAA0R3_S1VAA0R3_S2VAA0R3_S3VAA0R3_S4VSfAA0R3_S5Vs5UInt8VSdAA0R3_S6VtF")]
    private static extern nint SwiftFunc36(F36_S0 a0, double a1, ulong a2, F36_S1 a3, F36_S2 a4, F36_S3 a5, F36_S4 a6, float a7, F36_S5 a8, byte a9, double a10, F36_S6 a11);

    [Fact]
    public static void TestSwiftFunc36()
    {
        Console.Write("Running SwiftFunc36: ");
        long result = SwiftFunc36(new F36_S0(6433294246214898902, -21), 3881104127408136, 2284220855453859614, new F36_S1(4439404430423666401, unchecked((nuint)6899402977735223119), unchecked((nint)5232137643577323921), 622124401), new F36_S2(unchecked((nint)2215893056133254497)), new F36_S3(929506260159009104, -122, new F36_S3_S0(1015742)), new F36_S4(unchecked((nuint)3900865090022814819), 5812191011379795103, 4189883409333787, 3777993202541206), 1483351, new F36_S5(168, 87), 242, 3899885261689271, new F36_S6(49518));
        Assert.Equal(624934575149916284, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F37_S0
    {
        public int F0;

        public F37_S0(int f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F37_S1
    {
        public uint F0;
        public uint F1;
        public float F2;

        public F37_S1(uint f0, uint f1, float f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F37_S2
    {
        public int F0;
        public uint F1;
        public double F2;
        public nuint F3;

        public F37_S2(int f0, uint f1, double f2, nuint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F37_S3_S0
    {
        public nint F0;

        public F37_S3_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F37_S3
    {
        public F37_S3_S0 F0;

        public F37_S3(F37_S3_S0 f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc372a02a12a22a32a42a52a62a72a82a93a103a113a123a13S2i_s6UInt64Vs6UInt32Vs5Int32Vs4Int8Vs5UInt8VArA6F37_S0VAA0Y3_S1Vs5Int16VAA0Y3_S2VSuAA0Y3_S3VARtF")]
    private static extern nint SwiftFunc37(nint a0, ulong a1, uint a2, int a3, sbyte a4, byte a5, ulong a6, F37_S0 a7, F37_S1 a8, short a9, F37_S2 a10, nuint a11, F37_S3 a12, ulong a13);

    [Fact]
    public static void TestSwiftFunc37()
    {
        Console.Write("Running SwiftFunc37: ");
        long result = SwiftFunc37(unchecked((nint)7997876577338840618), 2916693561268448247, 2045535781, 1617618895, 35, 118, 8729954385529497591, new F37_S0(1590622742), new F37_S1(1445653735, 1780802910, 6918266), -302, new F37_S2(504109544, 1827855745, 3682561033291689, unchecked((nuint)6718188397722828326)), unchecked((nuint)4901939155447291041), new F37_S3(new F37_S3_S0(unchecked((nint)7671123806949823347))), 4910913885588390838);
        Assert.Equal(-3950862618349704578, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 6)]
    struct F38_S0
    {
        public ushort F0;
        public short F1;
        public short F2;

        public F38_S0(ushort f0, short f1, short f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F38_S1
    {
        public int F0;

        public F38_S1(int f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F38_S2
    {
        public nuint F0;

        public F38_S2(nuint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc382a02a12a22a32a4Sis6UInt32V_s5Int32VAA6F38_S0VAA0M3_S1VAA0M3_S2VtF")]
    private static extern nint SwiftFunc38(uint a0, int a1, F38_S0 a2, F38_S1 a3, F38_S2 a4);

    [Fact]
    public static void TestSwiftFunc38()
    {
        Console.Write("Running SwiftFunc38: ");
        long result = SwiftFunc38(2061218718, 320687949, new F38_S0(53989, -5186, -13102), new F38_S1(1455203558), new F38_S2(unchecked((nuint)4328826644800782496)));
        Assert.Equal(1423775906233216436, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F39_S0_S0
    {
        public nuint F0;

        public F39_S0_S0(nuint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F39_S0_S1
    {
        public int F0;

        public F39_S0_S1(int f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 36)]
    struct F39_S0
    {
        public nint F0;
        public long F1;
        public uint F2;
        public F39_S0_S0 F3;
        public F39_S0_S1 F4;

        public F39_S0(nint f0, long f1, uint f2, F39_S0_S0 f3, F39_S0_S1 f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F39_S1
    {
        public nuint F0;
        public double F1;

        public F39_S1(nuint f0, double f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc392a02a12a22a32a4SiSu_SuAA6F39_S0VAA0K3_S1VSftF")]
    private static extern nint SwiftFunc39(nuint a0, nuint a1, F39_S0 a2, F39_S1 a3, float a4);

    [Fact]
    public static void TestSwiftFunc39()
    {
        Console.Write("Running SwiftFunc39: ");
        long result = SwiftFunc39(unchecked((nuint)8230747730129668979), unchecked((nuint)4736775119629579479), new F39_S0(unchecked((nint)5173491896684902537), 4915765547454462242, 1028369724, new F39_S0_S0(unchecked((nuint)8662559577682755939)), new F39_S0_S1(436709185)), new F39_S1(unchecked((nuint)3203283942912276541), 3029648293570205), 5675124);
        Assert.Equal(-1722913155676633924, result);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc402a0Sis5Int32V_tF")]
    private static extern nint SwiftFunc40(int a0);

    [Fact]
    public static void TestSwiftFunc40()
    {
        Console.Write("Running SwiftFunc40: ");
        long result = SwiftFunc40(447211275);
        Assert.Equal(8279520253543879998, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F41_S0
    {
        public short F0;
        public float F1;
        public ushort F2;

        public F41_S0(short f0, float f1, ushort f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F41_S1
    {
        public ushort F0;
        public ulong F1;
        public sbyte F2;
        public float F3;
        public ulong F4;

        public F41_S1(ushort f0, ulong f1, sbyte f2, float f3, ulong f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F41_S2_S0_S0
    {
        public short F0;

        public F41_S2_S0_S0(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F41_S2_S0
    {
        public F41_S2_S0_S0 F0;

        public F41_S2_S0(F41_S2_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 22)]
    struct F41_S2
    {
        public int F0;
        public short F1;
        public ulong F2;
        public float F3;
        public F41_S2_S0 F4;

        public F41_S2(int f0, short f1, ulong f2, float f3, F41_S2_S0 f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc412a02a12a22a32a42a52a62a72a8SiSf_AA6F41_S0VAA0O3_S1VAA0O3_S2Vs6UInt32VSuASSis4Int8VtF")]
    private static extern nint SwiftFunc41(float a0, F41_S0 a1, F41_S1 a2, F41_S2 a3, uint a4, nuint a5, uint a6, nint a7, sbyte a8);

    [Fact]
    public static void TestSwiftFunc41()
    {
        Console.Write("Running SwiftFunc41: ");
        long result = SwiftFunc41(5984057, new F41_S0(11791, 7594, 4883), new F41_S1(61253, 4089489613092392334, -39, 4246219, 6241750146529178696), new F41_S2(2097957786, -31595, 2497631910262823657, 1845838, new F41_S2_S0(new F41_S2_S0_S0(-4594))), 2146355885, unchecked((nuint)7552603789122823169), 1034389054, unchecked((nint)5088721772774365291), -61);
        Assert.Equal(-8371592578322439321, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F42_S0
    {
        public uint F0;
        public ulong F1;
        public ulong F2;

        public F42_S0(uint f0, ulong f1, ulong f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F42_S1
    {
        public double F0;
        public double F1;

        public F42_S1(double f0, double f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F42_S2_S0
    {
        public nint F0;

        public F42_S2_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F42_S2
    {
        public byte F0;
        public long F1;
        public F42_S2_S0 F2;
        public nint F3;

        public F42_S2(byte f0, long f1, F42_S2_S0 f2, nint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F42_S3_S0
    {
        public short F0;

        public F42_S3_S0(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 6)]
    struct F42_S3
    {
        public float F0;
        public F42_S3_S0 F1;

        public F42_S3(float f0, F42_S3_S0 f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F42_S4
    {
        public uint F0;

        public F42_S4(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F42_S5_S0
    {
        public uint F0;

        public F42_S5_S0(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F42_S5
    {
        public F42_S5_S0 F0;

        public F42_S5(F42_S5_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F42_S6
    {
        public nuint F0;

        public F42_S6(nuint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc422a02a12a22a32a42a52a62a72a8SiAA6F42_S0V_AA0O3_S1Vs6UInt16VAA0O3_S2VAA0O3_S3VAA0O3_S4VAA0O3_S5VAA0O3_S6Vs5Int16VtF")]
    private static extern nint SwiftFunc42(F42_S0 a0, F42_S1 a1, ushort a2, F42_S2 a3, F42_S3 a4, F42_S4 a5, F42_S5 a6, F42_S6 a7, short a8);

    [Fact]
    public static void TestSwiftFunc42()
    {
        Console.Write("Running SwiftFunc42: ");
        long result = SwiftFunc42(new F42_S0(1751713754, 1990881383827669198, 7688992749840190173), new F42_S1(2820409929234558, 403450751107933), 8553, new F42_S2(0, 4857265047176672349, new F42_S2_S0(unchecked((nint)1659771770143536426)), unchecked((nint)4175194780289529190)), new F42_S3(2068820, new F42_S3_S0(-19086)), new F42_S4(499069670), new F42_S5(new F42_S5_S0(82826892)), new F42_S6(unchecked((nuint)7728490038553858908)), -843);
        Assert.Equal(-5733927999088121133, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F43_S0_S0
    {
        public long F0;

        public F43_S0_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F43_S0
    {
        public F43_S0_S0 F0;

        public F43_S0(F43_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc432a02a12a22a32a42a52a6Sis5Int64V_s5UInt8Vs4Int8VSfAKSiAA6F43_S0VtF")]
    private static extern nint SwiftFunc43(long a0, byte a1, sbyte a2, float a3, long a4, nint a5, F43_S0 a6);

    [Fact]
    public static void TestSwiftFunc43()
    {
        Console.Write("Running SwiftFunc43: ");
        long result = SwiftFunc43(4912883404842918819, 157, 103, 5202238, 1699534526741372140, unchecked((nint)5944804412045224395), new F43_S0(new F43_S0_S0(8392262032814776063)));
        Assert.Equal(7967353118822572137, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F44_S0
    {
        public ulong F0;

        public F44_S0(ulong f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc442a0SiAA6F44_S0V_tF")]
    private static extern nint SwiftFunc44(F44_S0 a0);

    [Fact]
    public static void TestSwiftFunc44()
    {
        Console.Write("Running SwiftFunc44: ");
        long result = SwiftFunc44(new F44_S0(6701010027402704605));
        Assert.Equal(-2463268961390375024, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F45_S0
    {
        public double F0;
        public nint F1;

        public F45_S0(double f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F45_S1_S0
    {
        public double F0;

        public F45_S1_S0(double f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F45_S1_S1
    {
        public float F0;

        public F45_S1_S1(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F45_S1
    {
        public ushort F0;
        public sbyte F1;
        public F45_S1_S0 F2;
        public F45_S1_S1 F3;

        public F45_S1(ushort f0, sbyte f1, F45_S1_S0 f2, F45_S1_S1 f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 14)]
    struct F45_S2
    {
        public ulong F0;
        public float F1;
        public ushort F2;

        public F45_S2(ulong f0, float f1, ushort f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc452a02a12a22a3SiAA6F45_S0V_AA0J3_S1VAA0J3_S2VSitF")]
    private static extern nint SwiftFunc45(F45_S0 a0, F45_S1 a1, F45_S2 a2, nint a3);

    [Fact]
    public static void TestSwiftFunc45()
    {
        Console.Write("Running SwiftFunc45: ");
        long result = SwiftFunc45(new F45_S0(3026820520892803, unchecked((nint)329722294948274546)), new F45_S1(13060, 14, new F45_S1_S0(173821703534560), new F45_S1_S1(6669558)), new F45_S2(7271072737280269762, 2970569, 7063), unchecked((nint)3563249765520844925));
        Assert.Equal(6216079413995056174, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 25)]
    struct F46_S0
    {
        public long F0;
        public byte F1;
        public nuint F2;
        public sbyte F3;

        public F46_S0(long f0, byte f1, nuint f2, sbyte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F46_S1
    {
        public byte F0;

        public F46_S1(byte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F46_S2
    {
        public nint F0;

        public F46_S2(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F46_S3
    {
        public ulong F0;
        public long F1;

        public F46_S3(ulong f0, long f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F46_S4
    {
        public short F0;
        public int F1;
        public uint F2;

        public F46_S4(short f0, int f1, uint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F46_S5
    {
        public ulong F0;

        public F46_S5(ulong f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc462a02a12a22a32a42a52a62a72a82a93a103a113a123a13SiAA6F46_S0V_AA0T3_S1Vs4Int8VSfAA0T3_S2Vs5Int16VAA0T3_S3VAZSfAA0T3_S4Vs6UInt16VSfAvA0T3_S5VtF")]
    private static extern nint SwiftFunc46(F46_S0 a0, F46_S1 a1, sbyte a2, float a3, F46_S2 a4, short a5, F46_S3 a6, short a7, float a8, F46_S4 a9, ushort a10, float a11, sbyte a12, F46_S5 a13);

    [Fact]
    public static void TestSwiftFunc46()
    {
        Console.Write("Running SwiftFunc46: ");
        long result = SwiftFunc46(new F46_S0(717422391795779639, 78, unchecked((nuint)7060282015706292416), -116), new F46_S1(18), 3, 2507216, new F46_S2(unchecked((nint)4201483730092308719)), -18720, new F46_S3(2236255490462487034, 3838628161824947390), -9982, 5460360, new F46_S4(-4606, 1433117890, 835780718), 6752, 6275800, 91, new F46_S5(9211362063136377356));
        Assert.Equal(343358650074914091, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 3)]
    struct F47_S0_S0
    {
        public ushort F0;
        public sbyte F1;

        public F47_S0_S0(ushort f0, sbyte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F47_S0
    {
        public F47_S0_S0 F0;
        public ushort F1;
        public nuint F2;
        public long F3;

        public F47_S0(F47_S0_S0 f0, ushort f1, nuint f2, long f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct F47_S1
    {
        public long F0;
        public byte F1;

        public F47_S1(long f0, byte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc472a02a12a22a3S2i_AA6F47_S0VAA0J3_S1Vs5Int64VtF")]
    private static extern nint SwiftFunc47(nint a0, F47_S0 a1, F47_S1 a2, long a3);

    [Fact]
    public static void TestSwiftFunc47()
    {
        Console.Write("Running SwiftFunc47: ");
        long result = SwiftFunc47(unchecked((nint)4962370882457048382), new F47_S0(new F47_S0_S0(58684, -2), 23837, unchecked((nuint)2492821112189780145), 4191553673129943106), new F47_S1(3653010013906471970, 124), 4972057731925125595);
        Assert.Equal(-2787387042865302571, result);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc482a02a12a22a32a42a52a6Sis4Int8V_s6UInt32Vs5Int16VSfSiSfAMtF")]
    private static extern nint SwiftFunc48(sbyte a0, uint a1, short a2, float a3, nint a4, float a5, uint a6);

    [Fact]
    public static void TestSwiftFunc48()
    {
        Console.Write("Running SwiftFunc48: ");
        long result = SwiftFunc48(93, 1756298153, -26153, 8138154, unchecked((nint)5977260391149529061), 5377189, 1353843369);
        Assert.Equal(-1595422391414550142, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F49_S0
    {
        public ulong F0;

        public F49_S0(ulong f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F49_S1_S0
    {
        public short F0;

        public F49_S1_S0(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F49_S1_S1
    {
        public ushort F0;

        public F49_S1_S1(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F49_S1
    {
        public F49_S1_S0 F0;
        public int F1;
        public F49_S1_S1 F2;
        public nuint F3;

        public F49_S1(F49_S1_S0 f0, int f1, F49_S1_S1 f2, nuint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F49_S2
    {
        public ushort F0;
        public byte F1;
        public float F2;
        public long F3;

        public F49_S2(ushort f0, byte f1, float f2, long f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F49_S3
    {
        public int F0;
        public float F1;

        public F49_S3(int f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F49_S4
    {
        public uint F0;
        public nint F1;
        public nint F2;

        public F49_S4(uint f0, nint f1, nint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc492a02a12a22a32a42a52a62a72a82a93a103a113a123a13Sis6UInt64V_s5UInt8VAA6F49_S0VAA0V3_S1VSus6UInt32VSdAA0V3_S2VAA0V3_S3Vs4Int8VAA0V3_S4Vs5Int32VArTtF")]
    private static extern nint SwiftFunc49(ulong a0, byte a1, F49_S0 a2, F49_S1 a3, nuint a4, uint a5, double a6, F49_S2 a7, F49_S3 a8, sbyte a9, F49_S4 a10, int a11, ulong a12, byte a13);

    [Fact]
    public static void TestSwiftFunc49()
    {
        Console.Write("Running SwiftFunc49: ");
        long result = SwiftFunc49(1758884505462049879, 12, new F49_S0(1193104697993232570), new F49_S1(new F49_S1_S0(-23214), 1970325915, new F49_S1_S1(20900), unchecked((nuint)8432422526033383651)), unchecked((nuint)2433203633589099643), 1858554667, 2299996688980169, new F49_S2(65085, 158, 5839721, 6998202268068265472), new F49_S3(388389487, 5466404), -56, new F49_S4(1497255814, unchecked((nint)6665924212978484968), unchecked((nint)2332855076356772912)), 2065183786, 3874235334202874682, 6);
        Assert.Equal(-6839703945099631142, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F50_S0
    {
        public sbyte F0;
        public short F1;
        public int F2;
        public uint F3;

        public F50_S0(sbyte f0, short f1, int f2, uint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F50_S1
    {
        public int F0;

        public F50_S1(int f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc502a02a12a2SiAA6F50_S0V_s5UInt8VAA0I3_S1VtF")]
    private static extern nint SwiftFunc50(F50_S0 a0, byte a1, F50_S1 a2);

    [Fact]
    public static void TestSwiftFunc50()
    {
        Console.Write("Running SwiftFunc50: ");
        long result = SwiftFunc50(new F50_S0(-64, 4463, 1574267626, 1599903339), 22, new F50_S1(2042416614));
        Assert.Equal(6447602248618864959, result);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc512a02a12a2Sis6UInt16V_s4Int8Vs5Int16VtF")]
    private static extern nint SwiftFunc51(ushort a0, sbyte a1, short a2);

    [Fact]
    public static void TestSwiftFunc51()
    {
        Console.Write("Running SwiftFunc51: ");
        long result = SwiftFunc51(44154, 95, 13522);
        Assert.Equal(-2544044281448828766, result);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc522a02a1Sis5UInt8V_s6UInt64VtF")]
    private static extern nint SwiftFunc52(byte a0, ulong a1);

    [Fact]
    public static void TestSwiftFunc52()
    {
        Console.Write("Running SwiftFunc52: ");
        long result = SwiftFunc52(249, 1201897610107180823);
        Assert.Equal(6106660152306827238, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F53_S0_S0
    {
        public long F0;
        public nuint F1;

        public F53_S0_S0(long f0, nuint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 27)]
    struct F53_S0
    {
        public ulong F0;
        public F53_S0_S0 F1;
        public short F2;
        public byte F3;

        public F53_S0(ulong f0, F53_S0_S0 f1, short f2, byte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F53_S1_S0
    {
        public long F0;

        public F53_S1_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F53_S1
    {
        public F53_S1_S0 F0;

        public F53_S1(F53_S1_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F53_S2
    {
        public byte F0;
        public ulong F1;
        public double F2;

        public F53_S2(byte f0, ulong f1, double f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc532a02a12a22a32a42a52a62a7SiAA6F53_S0V_Sus6UInt64VSfs6UInt32VAA0N3_S1VAA0N3_S2VAPtF")]
    private static extern nint SwiftFunc53(F53_S0 a0, nuint a1, ulong a2, float a3, uint a4, F53_S1 a5, F53_S2 a6, uint a7);

    [Fact]
    public static void TestSwiftFunc53()
    {
        Console.Write("Running SwiftFunc53: ");
        long result = SwiftFunc53(new F53_S0(2962492598802212039, new F53_S0_S0(1217181921916443700, unchecked((nuint)7957002726435705223)), -18332, 65), unchecked((nuint)1996569991268125865), 2786689999092271249, 3627618, 1358803132, new F53_S1(new F53_S1_S0(6851624154761347887)), new F53_S2(12, 3669418545199894911, 3500804251230011), 1238561537);
        Assert.Equal(609186359525793369, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F54_S0_S0
    {
        public nint F0;

        public F54_S0_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F54_S0
    {
        public F54_S0_S0 F0;

        public F54_S0(F54_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F54_S1
    {
        public uint F0;

        public F54_S1(uint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc542a02a12a22a32a42a52a6Sis4Int8V_s5Int32Vs6UInt32VAA6F54_S0VSfs5UInt8VAA0P3_S1VtF")]
    private static extern nint SwiftFunc54(sbyte a0, int a1, uint a2, F54_S0 a3, float a4, byte a5, F54_S1 a6);

    [Fact]
    public static void TestSwiftFunc54()
    {
        Console.Write("Running SwiftFunc54: ");
        long result = SwiftFunc54(56, 918504001, 1944992063, new F54_S0(new F54_S0_S0(unchecked((nint)4622400191672284422))), 7815948, 27, new F54_S1(1866972157));
        Assert.Equal(604312640974773799, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F55_S0
    {
        public double F0;

        public F55_S0(double f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc552a0SiAA6F55_S0V_tF")]
    private static extern nint SwiftFunc55(F55_S0 a0);

    [Fact]
    public static void TestSwiftFunc55()
    {
        Console.Write("Running SwiftFunc55: ");
        long result = SwiftFunc55(new F55_S0(2475083570077114));
        Assert.Equal(4468870103647778776, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F56_S0_S0
    {
        public byte F0;

        public F56_S0_S0(byte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 5)]
    struct F56_S0
    {
        public float F0;
        public F56_S0_S0 F1;

        public F56_S0(float f0, F56_S0_S0 f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F56_S1_S0
    {
        public short F0;

        public F56_S1_S0(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F56_S1
    {
        public F56_S1_S0 F0;
        public double F1;
        public nuint F2;
        public uint F3;

        public F56_S1(F56_S1_S0 f0, double f1, nuint f2, uint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F56_S2
    {
        public short F0;
        public short F1;

        public F56_S2(short f0, short f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F56_S3
    {
        public ushort F0;

        public F56_S3(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F56_S4
    {
        public nuint F0;

        public F56_S4(nuint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc562a02a12a22a32a4SiAA6F56_S0V_AA0K3_S1VAA0K3_S2VAA0K3_S3VAA0K3_S4VtF")]
    private static extern nint SwiftFunc56(F56_S0 a0, F56_S1 a1, F56_S2 a2, F56_S3 a3, F56_S4 a4);

    [Fact]
    public static void TestSwiftFunc56()
    {
        Console.Write("Running SwiftFunc56: ");
        long result = SwiftFunc56(new F56_S0(3251221, new F56_S0_S0(89)), new F56_S1(new F56_S1_S0(-1474), 3308371901004609, unchecked((nuint)3728108803958130353), 1165879205), new F56_S2(-32579, 9771), new F56_S3(42395), new F56_S4(unchecked((nuint)3303076886770130768)));
        Assert.Equal(7176775198947599357, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F57_S0
    {
        public sbyte F0;
        public uint F1;

        public F57_S0(sbyte f0, uint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F57_S1_S0
    {
        public uint F0;

        public F57_S1_S0(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F57_S1_S1
    {
        public nuint F0;

        public F57_S1_S1(nuint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F57_S1
    {
        public F57_S1_S0 F0;
        public F57_S1_S1 F1;
        public short F2;

        public F57_S1(F57_S1_S0 f0, F57_S1_S1 f1, short f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F57_S2
    {
        public nuint F0;

        public F57_S2(nuint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc572a02a12a22a32a42a5Sis6UInt32V_AA6F57_S0VAA0M3_S1VSuAA0M3_S2Vs5Int16VtF")]
    private static extern nint SwiftFunc57(uint a0, F57_S0 a1, F57_S1 a2, nuint a3, F57_S2 a4, short a5);

    [Fact]
    public static void TestSwiftFunc57()
    {
        Console.Write("Running SwiftFunc57: ");
        long result = SwiftFunc57(567633593, new F57_S0(-86, 696416112), new F57_S1(new F57_S1_S0(1314705768), new F57_S1_S1(unchecked((nuint)4597174980182436219)), 21486), unchecked((nuint)1438778133550518555), new F57_S2(unchecked((nuint)1802821206757818124)), 4133);
        Assert.Equal(-4086487603375673584, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F58_S0
    {
        public long F0;

        public F58_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 26)]
    struct F58_S1
    {
        public nuint F0;
        public nint F1;
        public nuint F2;
        public ushort F3;

        public F58_S1(nuint f0, nint f1, nuint f2, ushort f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc582a02a12a22a32a42a52a62a72a82a93a10Sis5UInt8V_AOSiAA6F58_S0VSfs6UInt64Vs4Int8VAA0R3_S1Vs6UInt16Vs5Int64VA_tF")]
    private static extern nint SwiftFunc58(byte a0, byte a1, nint a2, F58_S0 a3, float a4, ulong a5, sbyte a6, F58_S1 a7, ushort a8, long a9, long a10);

    [Fact]
    public static void TestSwiftFunc58()
    {
        Console.Write("Running SwiftFunc58: ");
        long result = SwiftFunc58(51, 253, unchecked((nint)6470303599084560885), new F58_S0(356776366673201597), 612927, 1591484822310744993, -83, new F58_S1(unchecked((nuint)8720809519112624165), unchecked((nint)5290640035451064344), unchecked((nuint)991273095809135742), 45122), 55653, 5992020387203072133, 5336758723611801952);
        Assert.Equal(-9219400197360619686, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F59_S0
    {
        public nuint F0;
        public byte F1;
        public float F2;
        public nint F3;

        public F59_S0(nuint f0, byte f1, float f2, nint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F59_S1
    {
        public byte F0;
        public int F1;

        public F59_S1(byte f0, int f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 13)]
    struct F59_S2
    {
        public nint F0;
        public uint F1;
        public sbyte F2;

        public F59_S2(nint f0, uint f1, sbyte f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F59_S3
    {
        public sbyte F0;
        public float F1;
        public int F2;

        public F59_S3(sbyte f0, float f1, int f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F59_S4_S0
    {
        public byte F0;

        public F59_S4_S0(byte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F59_S4
    {
        public F59_S4_S0 F0;

        public F59_S4(F59_S4_S0 f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc592a02a12a22a32a42a52a62a72a82a93a103a113a123a13SiAA6F59_S0V_Sfs6UInt32VAA0T3_S1VAA0T3_S2Vs6UInt16VSfS2iS2us5Int16VAA0T3_S3VAA0T3_S4VtF")]
    private static extern nint SwiftFunc59(F59_S0 a0, float a1, uint a2, F59_S1 a3, F59_S2 a4, ushort a5, float a6, nint a7, nint a8, nuint a9, nuint a10, short a11, F59_S3 a12, F59_S4 a13);

    [Fact]
    public static void TestSwiftFunc59()
    {
        Console.Write("Running SwiftFunc59: ");
        long result = SwiftFunc59(new F59_S0(unchecked((nuint)1925278801109387173), 250, 6726955, unchecked((nint)4972956627127050696)), 5574199, 1873801510, new F59_S1(124, 272974688), new F59_S2(unchecked((nint)7596794567652280845), 243527419, -47), 26413, 6450212, unchecked((nint)5453709526903953920), unchecked((nint)7927376389197462736), unchecked((nuint)780576731665989106), unchecked((nuint)7709897378564152812), 32023, new F59_S3(80, 4147780, 732950914), new F59_S4(new F59_S4_S0(4)));
        Assert.Equal(6864551615695935641, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F60_S0
    {
        public long F0;

        public F60_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F60_S1
    {
        public uint F0;

        public F60_S1(uint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc602a02a12a22a32a42a52a6Sis5Int32V_s4Int8VAKs6UInt16VSfAA6F60_S0VAA0P3_S1VtF")]
    private static extern nint SwiftFunc60(int a0, sbyte a1, int a2, ushort a3, float a4, F60_S0 a5, F60_S1 a6);

    [Fact]
    public static void TestSwiftFunc60()
    {
        Console.Write("Running SwiftFunc60: ");
        long result = SwiftFunc60(2069764774, -78, 1337682119, 39074, 1949913, new F60_S0(6466100081502457656), new F60_S1(762188122));
        Assert.Equal(-4208534265899748964, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct F61_S0
    {
        public ushort F0;
        public int F1;
        public sbyte F2;

        public F61_S0(ushort f0, int f1, sbyte f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F61_S1
    {
        public double F0;
        public nint F1;

        public F61_S1(double f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F61_S2
    {
        public nint F0;
        public sbyte F1;
        public float F2;
        public ushort F3;
        public float F4;

        public F61_S2(nint f0, sbyte f1, float f2, ushort f3, float f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F61_S3
    {
        public uint F0;
        public ulong F1;
        public nuint F2;
        public nuint F3;

        public F61_S3(uint f0, ulong f1, nuint f2, nuint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F61_S4_S0
    {
        public byte F0;
        public ulong F1;

        public F61_S4_S0(byte f0, ulong f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F61_S4
    {
        public F61_S4_S0 F0;
        public long F1;

        public F61_S4(F61_S4_S0 f0, long f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc612a02a12a22a32a42a52a62a72a82a9SiAA6F61_S0V_s5UInt8VSfAA0P3_S1Vs4Int8Vs5Int64VAA0P3_S2VAA0P3_S3VAA0P3_S4Vs6UInt32VtF")]
    private static extern nint SwiftFunc61(F61_S0 a0, byte a1, float a2, F61_S1 a3, sbyte a4, long a5, F61_S2 a6, F61_S3 a7, F61_S4 a8, uint a9);

    [Fact]
    public static void TestSwiftFunc61()
    {
        Console.Write("Running SwiftFunc61: ");
        long result = SwiftFunc61(new F61_S0(37779, 1838776162, -93), 6, 8289829, new F61_S1(87047161428510, unchecked((nint)1184205589182482579)), -29, 6533985246090322241, new F61_S2(unchecked((nint)2633423837220013660), 79, 307426, 32687, 2612234), new F61_S3(1625158302, 1379744644931696533, unchecked((nuint)1592864959164045790), unchecked((nuint)1112656184684227017)), new F61_S4(new F61_S4_S0(196, 2188268123262546231), 2448137925649839798), 691942709);
        Assert.Equal(-2463957420588616123, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F62_S0
    {
        public long F0;

        public F62_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F62_S1
    {
        public float F0;

        public F62_S1(float f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc622a02a12a22a3SiAA6F62_S0V_s5Int16Vs5Int32VAA0J3_S1VtF")]
    private static extern nint SwiftFunc62(F62_S0 a0, short a1, int a2, F62_S1 a3);

    [Fact]
    public static void TestSwiftFunc62()
    {
        Console.Write("Running SwiftFunc62: ");
        long result = SwiftFunc62(new F62_S0(7225726265078242156), 26594, 457232718, new F62_S1(5266624));
        Assert.Equal(1111474357603006336, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F63_S0
    {
        public nint F0;

        public F63_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc632a0SiAA6F63_S0V_tF")]
    private static extern nint SwiftFunc63(F63_S0 a0);

    [Fact]
    public static void TestSwiftFunc63()
    {
        Console.Write("Running SwiftFunc63: ");
        long result = SwiftFunc63(new F63_S0(unchecked((nint)8434688641118467652)));
        Assert.Equal(6012989597022805528, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F64_S0
    {
        public double F0;
        public ushort F1;
        public int F2;
        public nint F3;
        public double F4;

        public F64_S0(double f0, ushort f1, int f2, nint f3, double f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F64_S1
    {
        public int F0;
        public float F1;
        public uint F2;

        public F64_S1(int f0, float f1, uint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc642a02a12a22a32a42a52a62a72a8SiSd_AA6F64_S0Vs5UInt8VAA0O3_S1Vs5Int32Vs6UInt64Vs4Int8VAWSftF")]
    private static extern nint SwiftFunc64(double a0, F64_S0 a1, byte a2, F64_S1 a3, int a4, ulong a5, sbyte a6, sbyte a7, float a8);

    [Fact]
    public static void TestSwiftFunc64()
    {
        Console.Write("Running SwiftFunc64: ");
        long result = SwiftFunc64(1537265878737137, new F64_S0(3855732434182818, 17371, 213617860, unchecked((nint)7735022256180276511), 3812880695456163), 18, new F64_S1(484340550, 65067, 1337805733), 1841310158, 1819062569669413729, 17, -123, 4111745);
        Assert.Equal(2528424114157798731, result);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc652a02a12a22a3SiSf_SfSuSftF")]
    private static extern nint SwiftFunc65(float a0, float a1, nuint a2, float a3);

    [Fact]
    public static void TestSwiftFunc65()
    {
        Console.Write("Running SwiftFunc65: ");
        long result = SwiftFunc65(3752751, 4441416, unchecked((nuint)9195654236823676231), 1490781);
        Assert.Equal(1666102926850087608, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F66_S0
    {
        public long F0;

        public F66_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F66_S1_S0
    {
        public ushort F0;

        public F66_S1_S0(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F66_S1
    {
        public F66_S1_S0 F0;
        public float F1;

        public F66_S1(F66_S1_S0 f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct F66_S2
    {
        public double F0;
        public byte F1;

        public F66_S2(double f0, byte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F66_S3
    {
        public nuint F0;

        public F66_S3(nuint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc662a02a12a22a3SiAA6F66_S0V_AA0J3_S1VAA0J3_S2VAA0J3_S3VtF")]
    private static extern nint SwiftFunc66(F66_S0 a0, F66_S1 a1, F66_S2 a2, F66_S3 a3);

    [Fact]
    public static void TestSwiftFunc66()
    {
        Console.Write("Running SwiftFunc66: ");
        long result = SwiftFunc66(new F66_S0(7984064468330042160), new F66_S1(new F66_S1_S0(61382), 2971351), new F66_S2(463407482163222, 36), new F66_S3(unchecked((nuint)2172521839193002776)));
        Assert.Equal(4347440879386243204, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F67_S0
    {
        public ushort F0;

        public F67_S0(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F67_S1_S0_S0
    {
        public long F0;

        public F67_S1_S0_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F67_S1_S0
    {
        public F67_S1_S0_S0 F0;

        public F67_S1_S0(F67_S1_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 14)]
    struct F67_S1
    {
        public F67_S1_S0 F0;
        public uint F1;
        public short F2;

        public F67_S1(F67_S1_S0 f0, uint f1, short f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc672a02a12a22a32a42a52a62a72a82a9Sis6UInt64V_s6UInt32Vs6UInt16Vs4Int8VAA6F67_S0VAnA0T3_S1VSuANs5Int64VtF")]
    private static extern nint SwiftFunc67(ulong a0, uint a1, ushort a2, sbyte a3, F67_S0 a4, ulong a5, F67_S1 a6, nuint a7, ulong a8, long a9);

    [Fact]
    public static void TestSwiftFunc67()
    {
        Console.Write("Running SwiftFunc67: ");
        long result = SwiftFunc67(8417618485778766232, 263682468, 8040, 53, new F67_S0(44582), 2312853538155696297, new F67_S1(new F67_S1_S0(new F67_S1_S0_S0(358347933181524465)), 74416027, -11715), unchecked((nuint)3013147554369331538), 8581312208688354849, 3394216999618959997);
        Assert.Equal(-6725369964492065998, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F68_S0_S0_S0
    {
        public ushort F0;

        public F68_S0_S0_S0(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F68_S0_S0
    {
        public F68_S0_S0_S0 F0;

        public F68_S0_S0(F68_S0_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F68_S0
    {
        public F68_S0_S0 F0;

        public F68_S0(F68_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F68_S1
    {
        public ulong F0;
        public ushort F1;

        public F68_S1(ulong f0, ushort f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F68_S2
    {
        public nuint F0;
        public nint F1;
        public ulong F2;
        public double F3;

        public F68_S2(nuint f0, nint f1, ulong f2, double f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F68_S3
    {
        public nint F0;
        public uint F1;
        public uint F2;
        public nuint F3;

        public F68_S3(nint f0, uint f1, uint f2, nuint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F68_S4
    {
        public int F0;

        public F68_S4(int f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc682a02a12a22a32a42a52a62a72a82a93a103a113a123a133a143a15Sis6UInt16V_s5Int64Vs5Int16Vs6UInt64Vs4Int8Vs5Int32Vs5UInt8VAA6F68_S0VA4_AA6F68_S1VAxA6F68_S2VA2xA6F68_S3VAA6F68_S4VtF")]
    private static extern nint SwiftFunc68(ushort a0, long a1, short a2, ulong a3, sbyte a4, int a5, byte a6, F68_S0 a7, byte a8, F68_S1 a9, short a10, F68_S2 a11, short a12, short a13, F68_S3 a14, F68_S4 a15);

    [Fact]
    public static void TestSwiftFunc68()
    {
        Console.Write("Running SwiftFunc68: ");
        long result = SwiftFunc68(39378, 1879467527992319684, 2976, 7557363126592644195, -43, 2065185911, 186, new F68_S0(new F68_S0_S0(new F68_S0_S0_S0(38882))), 147, new F68_S1(7550657789172540141, 11186), 19125, new F68_S2(unchecked((nuint)7379823447100459002), unchecked((nint)2947420338952962953), 8170543862699682458, 4004920770933570), -12770, 19448, new F68_S3(unchecked((nint)4813886599424386410), 456733470, 2124904937, unchecked((nuint)4471482098861948789)), new F68_S4(1149728467));
        Assert.Equal(7624816402828697114, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F69_S0
    {
        public uint F0;
        public nuint F1;

        public F69_S0(uint f0, nuint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F69_S1_S0_S0
    {
        public byte F0;

        public F69_S1_S0_S0(byte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F69_S1_S0
    {
        public F69_S1_S0_S0 F0;
        public sbyte F1;

        public F69_S1_S0(F69_S1_S0_S0 f0, sbyte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F69_S1
    {
        public F69_S1_S0 F0;
        public nuint F1;
        public nint F2;

        public F69_S1(F69_S1_S0 f0, nuint f1, nint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 11)]
    struct F69_S2
    {
        public float F0;
        public uint F1;
        public ushort F2;
        public sbyte F3;

        public F69_S2(float f0, uint f1, ushort f2, sbyte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F69_S3
    {
        public byte F0;
        public double F1;

        public F69_S3(byte f0, double f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F69_S4
    {
        public double F0;

        public F69_S4(double f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F69_S5
    {
        public ulong F0;

        public F69_S5(ulong f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc692a02a12a22a32a42a52a62a72a82a93a103a113a123a13SiAA6F69_S0V_AA0T3_S1VS2is6UInt16Vs5Int16VSdAA0T3_S2VAA0T3_S3VAA0T3_S4VSis5Int32VAA0T3_S5VSftF")]
    private static extern nint SwiftFunc69(F69_S0 a0, F69_S1 a1, nint a2, nint a3, ushort a4, short a5, double a6, F69_S2 a7, F69_S3 a8, F69_S4 a9, nint a10, int a11, F69_S5 a12, float a13);

    [Fact]
    public static void TestSwiftFunc69()
    {
        Console.Write("Running SwiftFunc69: ");
        long result = SwiftFunc69(new F69_S0(906404083, unchecked((nuint)2807168213757166759)), new F69_S1(new F69_S1_S0(new F69_S1_S0_S0(186), 23), unchecked((nuint)8471050292345736986), unchecked((nint)8019232101297716588)), unchecked((nint)1646897491666286061), unchecked((nint)4641745789339591736), 16462, 8795, 2000104158043033, new F69_S2(5507285, 2004746552, 63158, -120), new F69_S3(205, 3126404745245894), new F69_S4(1149593901597831), unchecked((nint)7568671357281245424), 32654713, new F69_S5(9162350932434820903), 7511550);
        Assert.Equal(-6877731561846031803, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F70_S0
    {
        public float F0;
        public long F1;

        public F70_S0(float f0, long f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 6)]
    struct F70_S1
    {
        public ushort F0;
        public sbyte F1;
        public short F2;

        public F70_S1(ushort f0, sbyte f1, short f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F70_S2
    {
        public ushort F0;

        public F70_S2(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F70_S3
    {
        public ushort F0;

        public F70_S3(ushort f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc702a02a12a22a32a42a52a62a72a82a9Sis6UInt64V_AA6F70_S0Vs6UInt16Vs4Int8VSfAA0Q3_S1VSiAA0Q3_S2VAA0Q3_S3Vs6UInt32VtF")]
    private static extern nint SwiftFunc70(ulong a0, F70_S0 a1, ushort a2, sbyte a3, float a4, F70_S1 a5, nint a6, F70_S2 a7, F70_S3 a8, uint a9);

    [Fact]
    public static void TestSwiftFunc70()
    {
        Console.Write("Running SwiftFunc70: ");
        long result = SwiftFunc70(1536666996478548266, new F70_S0(7778910, 3166989107756003196), 13136, 22, 8164102, new F70_S1(26774, 89, 8871), unchecked((nint)3879856935687439957), new F70_S2(24302), new F70_S3(50084), 1197721391);
        Assert.Equal(-4661551892929812411, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F71_S0
    {
        public nint F0;

        public F71_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F71_S1
    {
        public ulong F0;

        public F71_S1(ulong f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc712a02a12a22a32a42a5Sis5Int64V_AA6F71_S0Vs4Int8VAA0M3_S1VSfs6UInt32VtF")]
    private static extern nint SwiftFunc71(long a0, F71_S0 a1, sbyte a2, F71_S1 a3, float a4, uint a5);

    [Fact]
    public static void TestSwiftFunc71()
    {
        Console.Write("Running SwiftFunc71: ");
        long result = SwiftFunc71(823408652288450499, new F71_S0(unchecked((nint)1673096114526242440)), 64, new F71_S1(1767538531468972832), 3230384, 1139683594);
        Assert.Equal(1763261422424450798, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F72_S0_S0
    {
        public nint F0;
        public double F1;

        public F72_S0_S0(nint f0, double f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F72_S0
    {
        public F72_S0_S0 F0;
        public uint F1;

        public F72_S0(F72_S0_S0 f0, uint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F72_S1
    {
        public nint F0;

        public F72_S1(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F72_S2
    {
        public double F0;

        public F72_S2(double f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc722a02a12a2SiAA6F72_S0V_AA0I3_S1VAA0I3_S2VtF")]
    private static extern nint SwiftFunc72(F72_S0 a0, F72_S1 a1, F72_S2 a2);

    [Fact]
    public static void TestSwiftFunc72()
    {
        Console.Write("Running SwiftFunc72: ");
        long result = SwiftFunc72(new F72_S0(new F72_S0_S0(unchecked((nint)42112534105392604), 2206378956781748), 13345585), new F72_S1(unchecked((nint)4236181300943972186)), new F72_S2(3246931881930745));
        Assert.Equal(5209731649169576491, result);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc732a0Sis5Int64V_tF")]
    private static extern nint SwiftFunc73(long a0);

    [Fact]
    public static void TestSwiftFunc73()
    {
        Console.Write("Running SwiftFunc73: ");
        long result = SwiftFunc73(5717467830857180976);
        Assert.Equal(4464612974464506231, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 17)]
    struct F74_S0
    {
        public byte F0;
        public byte F1;
        public double F2;
        public byte F3;

        public F74_S0(byte f0, byte f1, double f2, byte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F74_S1
    {
        public short F0;
        public ushort F1;
        public long F2;
        public nuint F3;

        public F74_S1(short f0, ushort f1, long f2, nuint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F74_S2
    {
        public short F0;
        public double F1;
        public float F2;

        public F74_S2(short f0, double f1, float f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F74_S3
    {
        public short F0;

        public F74_S3(short f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc742a02a12a22a32a42a52a62a72a82a9SiAA6F74_S0V_AA0P3_S1Vs5Int32VAA0P3_S2VSis5Int64Vs5Int16VArA0P3_S3Vs6UInt64VtF")]
    private static extern nint SwiftFunc74(F74_S0 a0, F74_S1 a1, int a2, F74_S2 a3, nint a4, long a5, short a6, int a7, F74_S3 a8, ulong a9);

    [Fact]
    public static void TestSwiftFunc74()
    {
        Console.Write("Running SwiftFunc74: ");
        long result = SwiftFunc74(new F74_S0(126, 165, 938186833815961, 37), new F74_S1(26448, 11115, 1477034907611479508, unchecked((nuint)7258103824495664788)), 1024717487, new F74_S2(-32191, 3877433950972112, 1759541), unchecked((nint)306022299836100497), 3906031458927364257, 105, 1354045377, new F74_S3(15217), 2609577929968659839);
        Assert.Equal(4852068750102322513, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F75_S0_S0_S0
    {
        public short F0;

        public F75_S0_S0_S0(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F75_S0_S0
    {
        public F75_S0_S0_S0 F0;

        public F75_S0_S0(F75_S0_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F75_S0
    {
        public F75_S0_S0 F0;
        public double F1;
        public int F2;

        public F75_S0(F75_S0_S0 f0, double f1, int f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F75_S1_S0_S0
    {
        public ushort F0;

        public F75_S1_S0_S0(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F75_S1_S0
    {
        public nuint F0;
        public F75_S1_S0_S0 F1;
        public long F2;

        public F75_S1_S0(nuint f0, F75_S1_S0_S0 f1, long f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F75_S1
    {
        public F75_S1_S0 F0;
        public nint F1;

        public F75_S1(F75_S1_S0 f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F75_S2
    {
        public ulong F0;

        public F75_S2(ulong f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc752a02a12a22a32a42a52a6SiAA6F75_S0V_SdSiSuSiAA0M3_S1VAA0M3_S2VtF")]
    private static extern nint SwiftFunc75(F75_S0 a0, double a1, nint a2, nuint a3, nint a4, F75_S1 a5, F75_S2 a6);

    [Fact]
    public static void TestSwiftFunc75()
    {
        Console.Write("Running SwiftFunc75: ");
        long result = SwiftFunc75(new F75_S0(new F75_S0_S0(new F75_S0_S0_S0(-10229)), 989267098871942, 1700151366), 1809179048674038, unchecked((nint)8327532491216230311), unchecked((nuint)2400790938015665595), unchecked((nint)9058430068368278195), new F75_S1(new F75_S1_S0(unchecked((nuint)2568090042127844270), new F75_S1_S0_S0(56529), 7258043284683232822), unchecked((nint)2580496344876818585)), new F75_S2(2518371079686790475));
        Assert.Equal(-3602049946494757864, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F76_S0
    {
        public nint F0;

        public F76_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 14)]
    struct F76_S1
    {
        public ulong F0;
        public int F1;
        public short F2;

        public F76_S1(ulong f0, int f1, short f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F76_S2
    {
        public uint F0;

        public F76_S2(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F76_S3
    {
        public nint F0;

        public F76_S3(nint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc762a02a12a22a32a42a52a62a72a82a93a103a113a123a133a143a153a163a173a183a193a20SiSd_s5Int64Vs6UInt16VS2fAA6F76_S0Vs5Int16VAA6F76_S1VAYs6UInt64VA_s5UInt8Vs4Int8VSiAYA11_A11_A3_A_AA6F76_S2VAA6F76_S3VtF")]
    private static extern nint SwiftFunc76(double a0, long a1, ushort a2, float a3, float a4, F76_S0 a5, short a6, F76_S1 a7, long a8, ulong a9, ushort a10, byte a11, sbyte a12, nint a13, long a14, sbyte a15, sbyte a16, short a17, ushort a18, F76_S2 a19, F76_S3 a20);

    [Fact]
    public static void TestSwiftFunc76()
    {
        Console.Write("Running SwiftFunc76: ");
        long result = SwiftFunc76(3446176204630463, 6827398998366360089, 5999, 2160153, 1821316, new F76_S0(unchecked((nint)4235786039908553749)), -1803, new F76_S1(7640434214516127655, 1290566778, -25932), 5980518466723941005, 3543741927421110901, 27548, 183, -92, unchecked((nint)2974474557334557206), 6986327999611060205, -10, -27, -1377, 28809, new F76_S2(971874601), new F76_S3(unchecked((nint)1638507434850613054)));
        Assert.Equal(1945785605876240600, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F77_S0_S0
    {
        public sbyte F0;

        public F77_S0_S0(sbyte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F77_S0
    {
        public ulong F0;
        public F77_S0_S0 F1;
        public sbyte F2;

        public F77_S0(ulong f0, F77_S0_S0 f1, sbyte f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F77_S1
    {
        public ulong F0;
        public nint F1;
        public int F2;

        public F77_S1(ulong f0, nint f1, int f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F77_S2_S0_S0
    {
        public ushort F0;

        public F77_S2_S0_S0(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F77_S2_S0
    {
        public F77_S2_S0_S0 F0;

        public F77_S2_S0(F77_S2_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 6)]
    struct F77_S2
    {
        public F77_S2_S0 F0;
        public short F1;
        public sbyte F2;
        public byte F3;

        public F77_S2(F77_S2_S0 f0, short f1, sbyte f2, byte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 26)]
    struct F77_S3
    {
        public nint F0;
        public nint F1;
        public nint F2;
        public short F3;

        public F77_S3(nint f0, nint f1, nint f2, short f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F77_S4
    {
        public double F0;
        public sbyte F1;
        public uint F2;
        public short F3;
        public uint F4;

        public F77_S4(double f0, sbyte f1, uint f2, short f3, uint f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F77_S5
    {
        public nuint F0;

        public F77_S5(nuint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc772a02a12a22a32a42a52a62a72a82a93a10SiAA6F77_S0V_s5Int16VAA0Q3_S1Vs6UInt32VAA0Q3_S2VAA0Q3_S3VAA0Q3_S4Vs6UInt64VAA0Q3_S5Vs6UInt16VSftF")]
    private static extern nint SwiftFunc77(F77_S0 a0, short a1, F77_S1 a2, uint a3, F77_S2 a4, F77_S3 a5, F77_S4 a6, ulong a7, F77_S5 a8, ushort a9, float a10);

    [Fact]
    public static void TestSwiftFunc77()
    {
        Console.Write("Running SwiftFunc77: ");
        long result = SwiftFunc77(new F77_S0(5280239821396586490, new F77_S0_S0(-88), -25), -22596, new F77_S1(7240134379191021288, unchecked((nint)7659208338594056339), 884422905), 1341388922, new F77_S2(new F77_S2_S0(new F77_S2_S0_S0(45223)), 7237, -31, 116), new F77_S3(unchecked((nint)1688714381756854732), unchecked((nint)22701789196637865), unchecked((nint)76294687751840896), -6664), new F77_S4(668345825700173, -66, 484390251, -29179, 1983850392), 2083761371968657768, new F77_S5(unchecked((nuint)8754131797018708878)), 60699, 6889813);
        Assert.Equal(6252428118328671717, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F78_S0
    {
        public ushort F0;
        public nuint F1;

        public F78_S0(ushort f0, nuint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc782a02a1SiAA6F78_S0V_s6UInt64VtF")]
    private static extern nint SwiftFunc78(F78_S0 a0, ulong a1);

    [Fact]
    public static void TestSwiftFunc78()
    {
        Console.Write("Running SwiftFunc78: ");
        long result = SwiftFunc78(new F78_S0(29770, unchecked((nuint)3187763107953451651)), 8011100719593217510);
        Assert.Equal(-3469054734849002121, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F79_S0
    {
        public double F0;

        public F79_S0(double f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc792a02a12a22a3Sis6UInt32V_AA6F79_S0Vs5Int16VSdtF")]
    private static extern nint SwiftFunc79(uint a0, F79_S0 a1, short a2, double a3);

    [Fact]
    public static void TestSwiftFunc79()
    {
        Console.Write("Running SwiftFunc79: ");
        long result = SwiftFunc79(125852033, new F79_S0(589854369615867), 32411, 2567161537252427);
        Assert.Equal(6919439799927692524, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F80_S0
    {
        public ulong F0;
        public double F1;

        public F80_S0(ulong f0, double f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F80_S1_S0
    {
        public byte F0;

        public F80_S1_S0(byte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 13)]
    struct F80_S1
    {
        public int F0;
        public ushort F1;
        public uint F2;
        public F80_S1_S0 F3;

        public F80_S1(int f0, ushort f1, uint f2, F80_S1_S0 f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 22)]
    struct F80_S2
    {
        public ulong F0;
        public long F1;
        public uint F2;
        public ushort F3;

        public F80_S2(ulong f0, long f1, uint f2, ushort f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F80_S3_S0_S0
    {
        public nint F0;
        public long F1;
        public ulong F2;

        public F80_S3_S0_S0(nint f0, long f1, ulong f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F80_S3_S0
    {
        public F80_S3_S0_S0 F0;
        public uint F1;

        public F80_S3_S0(F80_S3_S0_S0 f0, uint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F80_S3
    {
        public F80_S3_S0 F0;
        public int F1;

        public F80_S3(F80_S3_S0 f0, int f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F80_S4_S0
    {
        public float F0;

        public F80_S4_S0(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F80_S4
    {
        public F80_S4_S0 F0;

        public F80_S4(F80_S4_S0 f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc802a02a12a22a32a42a52a62a72a82a93a10SiAA6F80_S0V_AA0Q3_S1Vs6UInt16Vs5Int64VAA0Q3_S2VSds6UInt64Vs5Int32VAA0Q3_S3VAA0Q3_S4Vs5UInt8VtF")]
    private static extern nint SwiftFunc80(F80_S0 a0, F80_S1 a1, ushort a2, long a3, F80_S2 a4, double a5, ulong a6, int a7, F80_S3 a8, F80_S4 a9, byte a10);

    [Fact]
    public static void TestSwiftFunc80()
    {
        Console.Write("Running SwiftFunc80: ");
        long result = SwiftFunc80(new F80_S0(1355360960230091831, 1784308328429357), new F80_S1(1545826500, 60913, 1298907936, new F80_S1_S0(91)), 45929, 1430265567693421435, new F80_S2(5983675317199180530, 4061656029212457057, 1539740932, 57372), 3111292213584236, 1408283785399541904, 157768849, new F80_S3(new F80_S3_S0(new F80_S3_S0_S0(unchecked((nint)7843547046297667291), 5997146939658037534, 1422472621224237194), 579010799), 912968372), new F80_S4(new F80_S4_S0(6160826)), 91);
        Assert.Equal(-8787757710984015171, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 22)]
    struct F81_S0
    {
        public double F0;
        public ulong F1;
        public uint F2;
        public byte F3;
        public byte F4;

        public F81_S0(double f0, ulong f1, uint f2, byte f3, byte f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F81_S1
    {
        public uint F0;

        public F81_S1(uint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc812a02a12a22a3SiAA6F81_S0V_s5Int32VSfAA0J3_S1VtF")]
    private static extern nint SwiftFunc81(F81_S0 a0, int a1, float a2, F81_S1 a3);

    [Fact]
    public static void TestSwiftFunc81()
    {
        Console.Write("Running SwiftFunc81: ");
        long result = SwiftFunc81(new F81_S0(624904807476328, 8333634025352587313, 1193792370, 12, 123), 1584141967, 2042869, new F81_S1(929252664));
        Assert.Equal(-2553305027552835633, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 17)]
    struct F82_S0
    {
        public int F0;
        public short F1;
        public ulong F2;
        public sbyte F3;

        public F82_S0(int f0, short f1, ulong f2, sbyte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F82_S1_S0
    {
        public long F0;

        public F82_S1_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F82_S1
    {
        public nint F0;
        public int F1;
        public F82_S1_S0 F2;

        public F82_S1(nint f0, int f1, F82_S1_S0 f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F82_S2
    {
        public nint F0;
        public long F1;
        public uint F2;
        public ushort F3;
        public long F4;

        public F82_S2(nint f0, long f1, uint f2, ushort f3, long f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F82_S3
    {
        public byte F0;

        public F82_S3(byte f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc822a02a12a22a32a42a5SiAA6F82_S0V_AA0L3_S1VAA0L3_S2Vs6UInt32VSiAA0L3_S3VtF")]
    private static extern nint SwiftFunc82(F82_S0 a0, F82_S1 a1, F82_S2 a2, uint a3, nint a4, F82_S3 a5);

    [Fact]
    public static void TestSwiftFunc82()
    {
        Console.Write("Running SwiftFunc82: ");
        long result = SwiftFunc82(new F82_S0(1831859482, 13125, 959732722373954890, -77), new F82_S1(unchecked((nint)7895140590879382739), 1095783280, new F82_S1_S0(5569113039995240408)), new F82_S2(unchecked((nint)1146619146691566258), 9105860583981760040, 869172650, 46264, 3390698350483049795), 64268535, unchecked((nint)3935081377884943159), new F82_S3(152));
        Assert.Equal(545035333243758818, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F83_S0_S0
    {
        public byte F0;

        public F83_S0_S0(byte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F83_S0
    {
        public F83_S0_S0 F0;
        public nint F1;
        public float F2;

        public F83_S0(F83_S0_S0 f0, nint f1, float f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F83_S1_S0
    {
        public double F0;

        public F83_S1_S0(double f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F83_S1_S1_S0
    {
        public ushort F0;

        public F83_S1_S1_S0(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F83_S1_S1
    {
        public F83_S1_S1_S0 F0;

        public F83_S1_S1(F83_S1_S1_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F83_S1
    {
        public uint F0;
        public F83_S1_S0 F1;
        public F83_S1_S1 F2;

        public F83_S1(uint f0, F83_S1_S0 f1, F83_S1_S1 f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F83_S2
    {
        public nint F0;

        public F83_S2(nint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc832a02a12a22a32a42a52a62a7SiSf_AA6F83_S0VAA0N3_S1Vs5Int16VSiSfAA0N3_S2Vs6UInt16VtF")]
    private static extern nint SwiftFunc83(float a0, F83_S0 a1, F83_S1 a2, short a3, nint a4, float a5, F83_S2 a6, ushort a7);

    [Fact]
    public static void TestSwiftFunc83()
    {
        Console.Write("Running SwiftFunc83: ");
        long result = SwiftFunc83(215523, new F83_S0(new F83_S0_S0(156), unchecked((nint)6215307075393311297), 6861006), new F83_S1(2039967569, new F83_S1_S0(225951511203809), new F83_S1_S1(new F83_S1_S1_S0(4596))), -9234, unchecked((nint)5460548577590073953), 5802323, new F83_S2(unchecked((nint)7383303204767349238)), 26127);
        Assert.Equal(-2186229543452098356, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F84_S0
    {
        public short F0;
        public sbyte F1;
        public ushort F2;
        public long F3;
        public short F4;

        public F84_S0(short f0, sbyte f1, ushort f2, long f3, short f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F84_S1
    {
        public int F0;

        public F84_S1(int f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F84_S2_S0
    {
        public byte F0;
        public ulong F1;

        public F84_S2_S0(byte f0, ulong f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct F84_S2
    {
        public nuint F0;
        public F84_S2_S0 F1;
        public sbyte F2;
        public double F3;

        public F84_S2(nuint f0, F84_S2_S0 f1, sbyte f2, double f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F84_S3
    {
        public uint F0;

        public F84_S3(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F84_S4
    {
        public float F0;

        public F84_S4(float f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc842a02a12a22a32a42a52a62a72a82a93a103a113a12SiAA6F84_S0V_AA0S3_S1Vs6UInt64VAA0S3_S2Vs6UInt32VAA0S3_S3VSuAA0S3_S4VA2Us6UInt16Vs5Int16VSftF")]
    private static extern nint SwiftFunc84(F84_S0 a0, F84_S1 a1, ulong a2, F84_S2 a3, uint a4, F84_S3 a5, nuint a6, F84_S4 a7, ulong a8, ulong a9, ushort a10, short a11, float a12);

    [Fact]
    public static void TestSwiftFunc84()
    {
        Console.Write("Running SwiftFunc84: ");
        long result = SwiftFunc84(new F84_S0(-4484, -42, 64729, 6703360336708764515, -523), new F84_S1(1991025572), 3784369034793798079, new F84_S2(unchecked((nuint)8950003885832387073), new F84_S2_S0(212, 2246460359298562967), 110, 694425580701573), 590396201, new F84_S3(954246473), unchecked((nuint)4968200866033916175), new F84_S4(7222444), 6840076578020772755, 257938017424612706, 10826, 12362, 5240097);
        Assert.Equal(6470148389371753355, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F85_S0_S0_S0
    {
        public float F0;

        public F85_S0_S0_S0(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F85_S0_S0
    {
        public int F0;
        public F85_S0_S0_S0 F1;

        public F85_S0_S0(int f0, F85_S0_S0_S0 f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F85_S0
    {
        public float F0;
        public F85_S0_S0 F1;
        public nint F2;
        public long F3;

        public F85_S0(float f0, F85_S0_S0 f1, nint f2, long f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F85_S1
    {
        public uint F0;
        public int F1;

        public F85_S1(uint f0, int f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F85_S2
    {
        public nuint F0;

        public F85_S2(nuint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc852a02a12a22a32a42a5SiAA6F85_S0V_AA0L3_S1VAA0L3_S2Vs4Int8Vs6UInt32Vs5Int16VtF")]
    private static extern nint SwiftFunc85(F85_S0 a0, F85_S1 a1, F85_S2 a2, sbyte a3, uint a4, short a5);

    [Fact]
    public static void TestSwiftFunc85()
    {
        Console.Write("Running SwiftFunc85: ");
        long result = SwiftFunc85(new F85_S0(4799349, new F85_S0_S0(1649441954, new F85_S0_S0_S0(7944727)), unchecked((nint)9152994697049435513), 7643247514693376306), new F85_S1(1545626492, 422887320), new F85_S2(unchecked((nuint)6616620791022054982)), -117, 995038971, 27513);
        Assert.Equal(-8992223142373774956, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 22)]
    struct F86_S0
    {
        public int F0;
        public long F1;
        public int F2;
        public ushort F3;

        public F86_S0(int f0, long f1, int f2, ushort f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F86_S1_S0
    {
        public nuint F0;

        public F86_S1_S0(nuint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F86_S1
    {
        public F86_S1_S0 F0;
        public ushort F1;

        public F86_S1(F86_S1_S0 f0, ushort f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F86_S2
    {
        public uint F0;

        public F86_S2(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F86_S3
    {
        public short F0;

        public F86_S3(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F86_S4
    {
        public nint F0;

        public F86_S4(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F86_S5
    {
        public short F0;

        public F86_S5(short f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc862a02a12a22a32a42a52a62a72a82a9SiAA6F86_S0V_S2iSuAA0P3_S1VAA0P3_S2Vs6UInt64VAA0P3_S3VAA0P3_S4VAA0P3_S5VtF")]
    private static extern nint SwiftFunc86(F86_S0 a0, nint a1, nint a2, nuint a3, F86_S1 a4, F86_S2 a5, ulong a6, F86_S3 a7, F86_S4 a8, F86_S5 a9);

    [Fact]
    public static void TestSwiftFunc86()
    {
        Console.Write("Running SwiftFunc86: ");
        long result = SwiftFunc86(new F86_S0(1811942942, 5011425012386160741, 1789481754, 51980), unchecked((nint)6881030792370586912), unchecked((nint)1013091832294910089), unchecked((nuint)7426318018252287878), new F86_S1(new F86_S1_S0(unchecked((nuint)3709534733156518030)), 31161), new F86_S2(2110662074), 1492552132987044101, new F86_S3(18839), new F86_S4(unchecked((nint)3005766501093981786)), new F86_S5(-10373));
        Assert.Equal(4527117515781509085, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F87_S0_S0
    {
        public long F0;

        public F87_S0_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F87_S0
    {
        public F87_S0_S0 F0;
        public float F1;
        public long F2;
        public double F3;

        public F87_S0(F87_S0_S0 f0, float f1, long f2, double f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F87_S1
    {
        public int F0;

        public F87_S1(int f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F87_S2_S0
    {
        public ushort F0;

        public F87_S2_S0(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F87_S2
    {
        public F87_S2_S0 F0;

        public F87_S2(F87_S2_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F87_S3
    {
        public int F0;

        public F87_S3(int f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc872a02a12a22a32a42a52a62a72a82a93a103a113a123a13Sis5Int64V_AA6F87_S0VSus5UInt8VSds5Int16Vs6UInt64VSdSfAA0U3_S1VArA0U3_S2VAA0U3_S3VSftF")]
    private static extern nint SwiftFunc87(long a0, F87_S0 a1, nuint a2, byte a3, double a4, short a5, ulong a6, double a7, float a8, F87_S1 a9, long a10, F87_S2 a11, F87_S3 a12, float a13);

    [Fact]
    public static void TestSwiftFunc87()
    {
        Console.Write("Running SwiftFunc87: ");
        long result = SwiftFunc87(8841098117509422820, new F87_S0(new F87_S0_S0(2192442345186020478), 1545304, 750118731442317544, 3418050830544628), unchecked((nuint)6369165430746397674), 71, 487868533855774, -7094, 2907086057865536952, 1643866436526662, 2614039, new F87_S1(248182038), 6870063012628711946, new F87_S2(new F87_S2_S0(30623)), new F87_S3(1817616635), 3689131);
        Assert.Equal(359195416647062356, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F88_S0
    {
        public byte F0;
        public long F1;
        public ulong F2;
        public nint F3;

        public F88_S0(byte f0, long f1, ulong f2, nint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F88_S1
    {
        public long F0;
        public byte F1;
        public ushort F2;

        public F88_S1(long f0, byte f1, ushort f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F88_S2
    {
        public uint F0;

        public F88_S2(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F88_S3_S0
    {
        public nint F0;

        public F88_S3_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F88_S3
    {
        public int F0;
        public F88_S3_S0 F1;
        public sbyte F2;
        public ushort F3;

        public F88_S3(int f0, F88_S3_S0 f1, sbyte f2, ushort f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F88_S4_S0
    {
        public float F0;

        public F88_S4_S0(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 36)]
    struct F88_S4
    {
        public ushort F0;
        public nuint F1;
        public sbyte F2;
        public nint F3;
        public F88_S4_S0 F4;

        public F88_S4(ushort f0, nuint f1, sbyte f2, nint f3, F88_S4_S0 f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F88_S5
    {
        public float F0;

        public F88_S5(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F88_S6
    {
        public uint F0;

        public F88_S6(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F88_S7_S0
    {
        public nint F0;

        public F88_S7_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F88_S7
    {
        public F88_S7_S0 F0;

        public F88_S7(F88_S7_S0 f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc882a02a12a22a32a42a52a62a72a82a93a10SiAA6F88_S0V_s4Int8VAA0Q3_S1Vs6UInt64VAA0Q3_S2VAA0Q3_S3VAA0Q3_S4Vs5Int16VAA0Q3_S5VAA0Q3_S6VAA0Q3_S7VtF")]
    private static extern nint SwiftFunc88(F88_S0 a0, sbyte a1, F88_S1 a2, ulong a3, F88_S2 a4, F88_S3 a5, F88_S4 a6, short a7, F88_S5 a8, F88_S6 a9, F88_S7 a10);

    [Fact]
    public static void TestSwiftFunc88()
    {
        Console.Write("Running SwiftFunc88: ");
        long result = SwiftFunc88(new F88_S0(66, 2515475983225256977, 8461123965387740223, unchecked((nint)6118352888016174162)), 0, new F88_S1(2355530907227990563, 120, 33210), 2006620539850377306, new F88_S2(2040050135), new F88_S3(1424272615, new F88_S3_S0(unchecked((nint)1176474304741776688)), -37, 57192), new F88_S4(57186, unchecked((nuint)3158759263845266986), 126, unchecked((nint)2352285611293949590), new F88_S4_S0(148232)), -10009, new F88_S5(6466089), new F88_S6(552549040), new F88_S7(new F88_S7_S0(unchecked((nint)4375596076925501643))));
        Assert.Equal(-6799924240836522873, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F89_S0
    {
        public byte F0;
        public sbyte F1;

        public F89_S0(byte f0, sbyte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F89_S1
    {
        public int F0;

        public F89_S1(int f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F89_S2
    {
        public ushort F0;

        public F89_S2(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F89_S3
    {
        public double F0;
        public double F1;

        public F89_S3(double f0, double f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F89_S4
    {
        public uint F0;

        public F89_S4(uint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc892a02a12a22a32a42a52a6SiAA6F89_S0V_AA0M3_S1VAA0M3_S2Vs5UInt8VAA0M3_S3VAA0M3_S4Vs5Int32VtF")]
    private static extern nint SwiftFunc89(F89_S0 a0, F89_S1 a1, F89_S2 a2, byte a3, F89_S3 a4, F89_S4 a5, int a6);

    [Fact]
    public static void TestSwiftFunc89()
    {
        Console.Write("Running SwiftFunc89: ");
        long result = SwiftFunc89(new F89_S0(3, -70), new F89_S1(1399800474), new F89_S2(4503), 65, new F89_S3(2901632902048261, 1806714347370258), new F89_S4(536267264), 1925050147);
        Assert.Equal(-127506756024963910, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F90_S0
    {
        public ushort F0;
        public nint F1;

        public F90_S0(ushort f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F90_S1_S0
    {
        public nint F0;

        public F90_S1_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F90_S1
    {
        public F90_S1_S0 F0;
        public nuint F1;
        public double F2;

        public F90_S1(F90_S1_S0 f0, nuint f1, double f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F90_S2
    {
        public ulong F0;
        public nint F1;
        public ushort F2;

        public F90_S2(ulong f0, nint f1, ushort f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F90_S3_S0
    {
        public long F0;

        public F90_S3_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F90_S3
    {
        public F90_S3_S0 F0;

        public F90_S3(F90_S3_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F90_S4
    {
        public long F0;

        public F90_S4(long f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc902a02a12a22a32a42a52a62a7SiAA6F90_S0V_s4Int8VAA0N3_S1VAA0N3_S2VAA0N3_S3Vs6UInt32VAA0N3_S4Vs5UInt8VtF")]
    private static extern nint SwiftFunc90(F90_S0 a0, sbyte a1, F90_S1 a2, F90_S2 a3, F90_S3 a4, uint a5, F90_S4 a6, byte a7);

    [Fact]
    public static void TestSwiftFunc90()
    {
        Console.Write("Running SwiftFunc90: ");
        long result = SwiftFunc90(new F90_S0(50891, unchecked((nint)3526500586501844267)), 106, new F90_S1(new F90_S1_S0(unchecked((nint)1338488761303901988)), unchecked((nuint)6173879610835810848), 2724509546394616), new F90_S2(6787849318922951518, unchecked((nint)4947656706973797515), 31166), new F90_S3(new F90_S3_S0(9145287685889642436)), 126339746, new F90_S4(7529643579107652424), 32);
        Assert.Equal(3094701713551479277, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F91_S0_S0
    {
        public int F0;

        public F91_S0_S0(int f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F91_S0
    {
        public F91_S0_S0 F0;
        public uint F1;
        public nint F2;

        public F91_S0(F91_S0_S0 f0, uint f1, nint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc912a02a1SiAA6F91_S0V_s5UInt8VtF")]
    private static extern nint SwiftFunc91(F91_S0 a0, byte a1);

    [Fact]
    public static void TestSwiftFunc91()
    {
        Console.Write("Running SwiftFunc91: ");
        long result = SwiftFunc91(new F91_S0(new F91_S0_S0(1253970930), 1885655301, unchecked((nint)148902531378116685)), 122);
        Assert.Equal(887289976736078648, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F92_S0
    {
        public ushort F0;
        public ushort F1;

        public F92_S0(ushort f0, ushort f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F92_S1
    {
        public ulong F0;

        public F92_S1(ulong f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F92_S2
    {
        public ulong F0;
        public ulong F1;

        public F92_S2(ulong f0, ulong f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F92_S3
    {
        public nuint F0;

        public F92_S3(nuint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc922a02a12a22a32a42a52a62a72a82a93a103a113a123a133a143a153a163a173a183a19Sis5Int16V_s6UInt64VSus5Int64VAA6F92_S0VA0_Sds5UInt8Vs4Int8Vs6UInt32VA6_AA6F92_S1VA8_SfAZA4_s5Int32VA8_AA6F92_S2VAA6F92_S3VtF")]
    private static extern nint SwiftFunc92(short a0, ulong a1, nuint a2, long a3, F92_S0 a4, long a5, double a6, byte a7, sbyte a8, uint a9, sbyte a10, F92_S1 a11, uint a12, float a13, ulong a14, byte a15, int a16, uint a17, F92_S2 a18, F92_S3 a19);

    [Fact]
    public static void TestSwiftFunc92()
    {
        Console.Write("Running SwiftFunc92: ");
        long result = SwiftFunc92(21276, 3146876064491681609, unchecked((nuint)3037098519528577447), 9061597632723103558, new F92_S0(4967, 61949), 4798856485492542774, 4305543426365472, 182, -21, 270986478, -37, new F92_S1(7527241857214360309), 1301049439, 6192745, 8959151295191616689, 19, 1578403390, 633901437, new F92_S2(4396088615663569948, 4797465448959123058), new F92_S3(unchecked((nuint)7386458829492133332)));
        Assert.Equal(-7871787038267731510, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F93_S0
    {
        public int F0;
        public nuint F1;
        public double F2;

        public F93_S0(int f0, nuint f1, double f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F93_S1
    {
        public uint F0;

        public F93_S1(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F93_S2
    {
        public double F0;

        public F93_S2(double f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc932a02a12a22a32a4SiAA6F93_S0V_SiAA0K3_S1VSdAA0K3_S2VtF")]
    private static extern nint SwiftFunc93(F93_S0 a0, nint a1, F93_S1 a2, double a3, F93_S2 a4);

    [Fact]
    public static void TestSwiftFunc93()
    {
        Console.Write("Running SwiftFunc93: ");
        long result = SwiftFunc93(new F93_S0(982459422, unchecked((nuint)1427174739694078549), 2736620007792094), unchecked((nint)5873331022463084971), new F93_S1(1169579606), 2110866269939297, new F93_S2(2364749142642625));
        Assert.Equal(432632740260631481, result);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc942a02a12a22a3Sis6UInt64V_s5Int32VAHs5Int64VtF")]
    private static extern nint SwiftFunc94(ulong a0, int a1, ulong a2, long a3);

    [Fact]
    public static void TestSwiftFunc94()
    {
        Console.Write("Running SwiftFunc94: ");
        long result = SwiftFunc94(2878691982818555531, 580037131, 3143309402030542876, 3739683344990129550);
        Assert.Equal(-330124951832302022, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F95_S0
    {
        public long F0;

        public F95_S0(long f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc952a02a1SiAA6F95_S0V_s5Int64VtF")]
    private static extern nint SwiftFunc95(F95_S0 a0, long a1);

    [Fact]
    public static void TestSwiftFunc95()
    {
        Console.Write("Running SwiftFunc95: ");
        long result = SwiftFunc95(new F95_S0(7113705515120682426), 2532424238121218748);
        Assert.Equal(-5365348133343237200, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F96_S0_S0
    {
        public nint F0;

        public F96_S0_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F96_S0
    {
        public ulong F0;
        public double F1;
        public double F2;
        public F96_S0_S0 F3;

        public F96_S0(ulong f0, double f1, double f2, F96_S0_S0 f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F96_S1_S0_S0
    {
        public double F0;

        public F96_S1_S0_S0(double f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F96_S1_S0
    {
        public F96_S1_S0_S0 F0;

        public F96_S1_S0(F96_S1_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F96_S1
    {
        public F96_S1_S0 F0;

        public F96_S1(F96_S1_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F96_S2
    {
        public byte F0;
        public float F1;

        public F96_S2(byte f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F96_S3
    {
        public ushort F0;

        public F96_S3(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F96_S4
    {
        public nint F0;

        public F96_S4(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F96_S5_S0
    {
        public byte F0;

        public F96_S5_S0(byte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F96_S5
    {
        public F96_S5_S0 F0;

        public F96_S5(F96_S5_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F96_S6
    {
        public ulong F0;

        public F96_S6(ulong f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc962a02a12a22a32a42a52a62a72a82a93a103a113a123a133a143a153a163a173a18Sis6UInt16V_AA6F96_S0VAA0Z3_S1VAA0Z3_S2VAWs6UInt64VSis5Int32Vs5Int16VSuAA0Z3_S3VA7_Sis4Int8VA5_s6UInt32VAA0Z3_S4VAA0Z3_S5VAA0Z3_S6VtF")]
    private static extern nint SwiftFunc96(ushort a0, F96_S0 a1, F96_S1 a2, F96_S2 a3, ushort a4, ulong a5, nint a6, int a7, short a8, nuint a9, F96_S3 a10, short a11, nint a12, sbyte a13, int a14, uint a15, F96_S4 a16, F96_S5 a17, F96_S6 a18);

    [Fact]
    public static void TestSwiftFunc96()
    {
        Console.Write("Running SwiftFunc96: ");
        long result = SwiftFunc96(21321, new F96_S0(3140378485759721513, 3334385568992933, 2434271617187235, new F96_S0_S0(unchecked((nint)6455348790423327394))), new F96_S1(new F96_S1_S0(new F96_S1_S0_S0(2421227444572952))), new F96_S2(72, 1265762), 13171, 4895217822310904030, unchecked((nint)5923562627585381292), 1083710828, 12717, unchecked((nuint)8000948766038488291), new F96_S3(43225), -19602, unchecked((nint)248571613858478112), 17, 514773482, 1555810858, new F96_S4(unchecked((nint)5975988026010739585)), new F96_S5(new F96_S5_S0(231)), new F96_S6(4299230038366602170));
        Assert.Equal(-9154394486464436217, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F97_S0
    {
        public float F0;
        public float F1;
        public nint F2;
        public nint F3;
        public nint F4;

        public F97_S0(float f0, float f1, nint f2, nint f3, nint f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc972a02a12a22a32a42a52a62a7Sis4Int8V_s5Int32Vs5UInt8Vs6UInt32VApA6F97_S0VALSitF")]
    private static extern nint SwiftFunc97(sbyte a0, int a1, byte a2, uint a3, byte a4, F97_S0 a5, sbyte a6, nint a7);

    [Fact]
    public static void TestSwiftFunc97()
    {
        Console.Write("Running SwiftFunc97: ");
        long result = SwiftFunc97(-90, 2040542494, 255, 990214241, 129, new F97_S0(3372147, 5204115, unchecked((nint)4061871110726583367), unchecked((nint)5498225315328650601), unchecked((nint)4096658558391048200)), -91, unchecked((nint)8125330763927981736));
        Assert.Equal(-4028368897548286667, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F98_S0_S0_S0
    {
        public float F0;

        public F98_S0_S0_S0(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F98_S0_S0
    {
        public nuint F0;
        public F98_S0_S0_S0 F1;

        public F98_S0_S0(nuint f0, F98_S0_S0_S0 f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F98_S0
    {
        public long F0;
        public F98_S0_S0 F1;
        public nuint F2;

        public F98_S0(long f0, F98_S0_S0 f1, nuint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc982a02a12a22a32a42a5SiAA6F98_S0V_s6UInt16VALs5Int16Vs4Int8Vs6UInt32VtF")]
    private static extern nint SwiftFunc98(F98_S0 a0, ushort a1, ushort a2, short a3, sbyte a4, uint a5);

    [Fact]
    public static void TestSwiftFunc98()
    {
        Console.Write("Running SwiftFunc98: ");
        long result = SwiftFunc98(new F98_S0(3497167808648160462, new F98_S0_S0(unchecked((nuint)2747735625017321807), new F98_S0_S0_S0(4681050)), unchecked((nuint)3446511732552970390)), 61052, 18880, -20869, 35, 1056152744);
        Assert.Equal(7350111494379160095, result);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F99_S0
    {
        public ulong F0;
        public ushort F1;
        public float F2;
        public ulong F3;

        public F99_S0(ulong f0, ushort f1, float f2, ulong f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F99_S1_S0
    {
        public uint F0;

        public F99_S1_S0(uint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F99_S1
    {
        public F99_S1_S0 F0;

        public F99_S1(F99_S1_S0 f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s14SwiftAbiStress11swiftFunc992a02a12a22a3SiAA6F99_S0V_s4Int8VAA0J3_S1Vs5Int64VtF")]
    private static extern nint SwiftFunc99(F99_S0 a0, sbyte a1, F99_S1 a2, long a3);

    [Fact]
    public static void TestSwiftFunc99()
    {
        Console.Write("Running SwiftFunc99: ");
        long result = SwiftFunc99(new F99_S0(1210929052346596858, 3796, 3904675, 8849045203219202310), 97, new F99_S1(new F99_S1_S0(498956895)), 241968587946267390);
        Assert.Equal(7941122870613797512, result);
        Console.WriteLine("OK");
    }

}
