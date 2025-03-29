// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using Xunit;

public class SwiftRetAbiStress
{
    private const string SwiftLib = "libSwiftRetAbiStress.dylib";

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S0
    {
        public short F0;
        public int F1;
        public ulong F2;

        public S0(short f0, int f1, ulong f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func0AA2S0VyF")]
    private static extern S0 SwiftRetFunc0();

    [Fact]
    public static void TestSwiftRetFunc0()
    {
        Console.Write("Running SwiftRetFunc0: ");
        S0 val = SwiftRetFunc0();
        Assert.Equal((short)-17813, val.F0);
        Assert.Equal((int)318006528, val.F1);
        Assert.Equal((ulong)1195162122024233590, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct S1
    {
        public short F0;
        public float F1;
        public long F2;
        public uint F3;

        public S1(short f0, float f1, long f2, uint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func1AA2S1VyF")]
    private static extern S1 SwiftRetFunc1();

    [Fact]
    public static void TestSwiftRetFunc1()
    {
        Console.Write("Running SwiftRetFunc1: ");
        S1 val = SwiftRetFunc1();
        Assert.Equal((short)-29793, val.F0);
        Assert.Equal((float)7351779, val.F1);
        Assert.Equal((long)133491708229548754, val.F2);
        Assert.Equal((uint)665726990, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S2_S0
    {
        public ulong F0;

        public S2_S0(ulong f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct S2
    {
        public S2_S0 F0;
        public byte F1;
        public ushort F2;
        public float F3;
        public int F4;

        public S2(S2_S0 f0, byte f1, ushort f2, float f3, int f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func2AA2S2VyF")]
    private static extern S2 SwiftRetFunc2();

    [Fact]
    public static void TestSwiftRetFunc2()
    {
        Console.Write("Running SwiftRetFunc2: ");
        S2 val = SwiftRetFunc2();
        Assert.Equal((ulong)2153637757371267722, val.F0.F0);
        Assert.Equal((byte)150, val.F1);
        Assert.Equal((ushort)48920, val.F2);
        Assert.Equal((float)3564327, val.F3);
        Assert.Equal((int)1310569731, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct S3
    {
        public long F0;
        public double F1;
        public sbyte F2;
        public int F3;
        public ushort F4;
        public byte F5;
        public double F6;

        public S3(long f0, double f1, sbyte f2, int f3, ushort f4, byte f5, double f6)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
            F6 = f6;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func3AA2S3VyF")]
    private static extern S3 SwiftRetFunc3();

    [Fact]
    public static void TestSwiftRetFunc3()
    {
        Console.Write("Running SwiftRetFunc3: ");
        S3 val = SwiftRetFunc3();
        Assert.Equal((long)5610153900386943274, val.F0);
        Assert.Equal((double)2431035148834736, val.F1);
        Assert.Equal((sbyte)111, val.F2);
        Assert.Equal((int)772269424, val.F3);
        Assert.Equal((ushort)19240, val.F4);
        Assert.Equal((byte)146, val.F5);
        Assert.Equal((double)821805530740405, val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct S4
    {
        public sbyte F0;
        public uint F1;
        public ulong F2;
        public long F3;

        public S4(sbyte f0, uint f1, ulong f2, long f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func4AA2S4VyF")]
    private static extern S4 SwiftRetFunc4();

    [Fact]
    public static void TestSwiftRetFunc4()
    {
        Console.Write("Running SwiftRetFunc4: ");
        S4 val = SwiftRetFunc4();
        Assert.Equal((sbyte)125, val.F0);
        Assert.Equal((uint)377073381, val.F1);
        Assert.Equal((ulong)964784376430620335, val.F2);
        Assert.Equal((long)5588038704850976624, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S5_S0
    {
        public uint F0;
        public double F1;

        public S5_S0(uint f0, double f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 49)]
    struct S5
    {
        public ulong F0;
        public sbyte F1;
        public nuint F2;
        public S5_S0 F3;
        public nint F4;
        public byte F5;

        public S5(ulong f0, sbyte f1, nuint f2, S5_S0 f3, nint f4, byte f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func5AA2S5VyF")]
    private static extern S5 SwiftRetFunc5();

    [Fact]
    public static void TestSwiftRetFunc5()
    {
        Console.Write("Running SwiftRetFunc5: ");
        S5 val = SwiftRetFunc5();
        Assert.Equal((ulong)5315019731968023493, val.F0);
        Assert.Equal((sbyte)114, val.F1);
        Assert.Equal((nuint)unchecked((nuint)1154655179105889397), val.F2);
        Assert.Equal((uint)1468030771, val.F3.F0);
        Assert.Equal((double)3066473182924818, val.F3.F1);
        Assert.Equal((nint)unchecked((nint)6252650621827449809), val.F4);
        Assert.Equal((byte)129, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct S6
    {
        public int F0;
        public short F1;
        public long F2;
        public ushort F3;

        public S6(int f0, short f1, long f2, ushort f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func6AA2S6VyF")]
    private static extern S6 SwiftRetFunc6();

    [Fact]
    public static void TestSwiftRetFunc6()
    {
        Console.Write("Running SwiftRetFunc6: ");
        S6 val = SwiftRetFunc6();
        Assert.Equal((int)743741783, val.F0);
        Assert.Equal((short)-6821, val.F1);
        Assert.Equal((long)5908745692727636656, val.F2);
        Assert.Equal((ushort)64295, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S7_S0
    {
        public nint F0;

        public S7_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S7
    {
        public S7_S0 F0;

        public S7(S7_S0 f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func7AA2S7VyF")]
    private static extern S7 SwiftRetFunc7();

    [Fact]
    public static void TestSwiftRetFunc7()
    {
        Console.Write("Running SwiftRetFunc7: ");
        S7 val = SwiftRetFunc7();
        Assert.Equal((nint)unchecked((nint)7625368278886567558), val.F0.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S8
    {
        public nint F0;

        public S8(nint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func8AA2S8VyF")]
    private static extern S8 SwiftRetFunc8();

    [Fact]
    public static void TestSwiftRetFunc8()
    {
        Console.Write("Running SwiftRetFunc8: ");
        S8 val = SwiftRetFunc8();
        Assert.Equal((nint)unchecked((nint)775279004683334365), val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S9_S0
    {
        public short F0;
        public int F1;

        public S9_S0(short f0, int f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 26)]
    struct S9
    {
        public uint F0;
        public nint F1;
        public S9_S0 F2;
        public ushort F3;

        public S9(uint f0, nint f1, S9_S0 f2, ushort f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func9AA2S9VyF")]
    private static extern S9 SwiftRetFunc9();

    [Fact]
    public static void TestSwiftRetFunc9()
    {
        Console.Write("Running SwiftRetFunc9: ");
        S9 val = SwiftRetFunc9();
        Assert.Equal((uint)1223030410, val.F0);
        Assert.Equal((nint)unchecked((nint)4720638462358523954), val.F1);
        Assert.Equal((short)30631, val.F2.F0);
        Assert.Equal((int)1033774469, val.F2.F1);
        Assert.Equal((ushort)64474, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S10
    {
        public float F0;
        public float F1;

        public S10(float f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func10AA3S10VyF")]
    private static extern S10 SwiftRetFunc10();

    [Fact]
    public static void TestSwiftRetFunc10()
    {
        Console.Write("Running SwiftRetFunc10: ");
        S10 val = SwiftRetFunc10();
        Assert.Equal((float)3276917, val.F0);
        Assert.Equal((float)6694615, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 21)]
    struct S11
    {
        public double F0;
        public nint F1;
        public uint F2;
        public sbyte F3;

        public S11(double f0, nint f1, uint f2, sbyte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func11AA3S11VyF")]
    private static extern S11 SwiftRetFunc11();

    [Fact]
    public static void TestSwiftRetFunc11()
    {
        Console.Write("Running SwiftRetFunc11: ");
        S11 val = SwiftRetFunc11();
        Assert.Equal((double)938206348036312, val.F0);
        Assert.Equal((nint)unchecked((nint)6559514243876905696), val.F1);
        Assert.Equal((uint)1357772248, val.F2);
        Assert.Equal((sbyte)59, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S12
    {
        public double F0;

        public S12(double f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func12AA3S12VyF")]
    private static extern S12 SwiftRetFunc12();

    [Fact]
    public static void TestSwiftRetFunc12()
    {
        Console.Write("Running SwiftRetFunc12: ");
        S12 val = SwiftRetFunc12();
        Assert.Equal((double)1580503485222363, val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct S13
    {
        public uint F0;

        public S13(uint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func13AA3S13VyF")]
    private static extern S13 SwiftRetFunc13();

    [Fact]
    public static void TestSwiftRetFunc13()
    {
        Console.Write("Running SwiftRetFunc13: ");
        S13 val = SwiftRetFunc13();
        Assert.Equal((uint)1381551558, val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct S14_S0_S0
    {
        public sbyte F0;

        public S14_S0_S0(sbyte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct S14_S0
    {
        public S14_S0_S0 F0;

        public S14_S0(S14_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 26)]
    struct S14
    {
        public int F0;
        public ushort F1;
        public sbyte F2;
        public float F3;
        public ulong F4;
        public S14_S0 F5;
        public sbyte F6;

        public S14(int f0, ushort f1, sbyte f2, float f3, ulong f4, S14_S0 f5, sbyte f6)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
            F6 = f6;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func14AA3S14VyF")]
    private static extern S14 SwiftRetFunc14();

    [Fact]
    public static void TestSwiftRetFunc14()
    {
        Console.Write("Running SwiftRetFunc14: ");
        S14 val = SwiftRetFunc14();
        Assert.Equal((int)1765691191, val.F0);
        Assert.Equal((ushort)56629, val.F1);
        Assert.Equal((sbyte)25, val.F2);
        Assert.Equal((float)2944946, val.F3);
        Assert.Equal((ulong)951929105049584033, val.F4);
        Assert.Equal((sbyte)-30, val.F5.F0.F0);
        Assert.Equal((sbyte)66, val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct S15_S0
    {
        public nuint F0;
        public float F1;

        public S15_S0(nuint f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct S15
    {
        public nint F0;
        public S15_S0 F1;
        public ushort F2;
        public int F3;

        public S15(nint f0, S15_S0 f1, ushort f2, int f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func15AA3S15VyF")]
    private static extern S15 SwiftRetFunc15();

    [Fact]
    public static void TestSwiftRetFunc15()
    {
        Console.Write("Running SwiftRetFunc15: ");
        S15 val = SwiftRetFunc15();
        Assert.Equal((nint)unchecked((nint)2090703541638269172), val.F0);
        Assert.Equal((nuint)unchecked((nuint)6408314016925514463), val.F1.F0);
        Assert.Equal((float)6534515, val.F1.F1);
        Assert.Equal((ushort)30438, val.F2);
        Assert.Equal((int)1745811802, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 33)]
    struct S16
    {
        public uint F0;
        public ulong F1;
        public byte F2;
        public int F3;
        public nuint F4;
        public sbyte F5;

        public S16(uint f0, ulong f1, byte f2, int f3, nuint f4, sbyte f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func16AA3S16VyF")]
    private static extern S16 SwiftRetFunc16();

    [Fact]
    public static void TestSwiftRetFunc16()
    {
        Console.Write("Running SwiftRetFunc16: ");
        S16 val = SwiftRetFunc16();
        Assert.Equal((uint)585220635, val.F0);
        Assert.Equal((ulong)4034210936973794153, val.F1);
        Assert.Equal((byte)48, val.F2);
        Assert.Equal((int)1155081155, val.F3);
        Assert.Equal((nuint)unchecked((nuint)806384837403045657), val.F4);
        Assert.Equal((sbyte)54, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 3)]
    struct S17
    {
        public byte F0;
        public sbyte F1;
        public byte F2;

        public S17(byte f0, sbyte f1, byte f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func17AA3S17VyF")]
    private static extern S17 SwiftRetFunc17();

    [Fact]
    public static void TestSwiftRetFunc17()
    {
        Console.Write("Running SwiftRetFunc17: ");
        S17 val = SwiftRetFunc17();
        Assert.Equal((byte)23, val.F0);
        Assert.Equal((sbyte)112, val.F1);
        Assert.Equal((byte)15, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S18_S0
    {
        public uint F0;
        public float F1;

        public S18_S0(uint f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct S18
    {
        public S18_S0 F0;
        public nint F1;
        public int F2;
        public ushort F3;
        public short F4;

        public S18(S18_S0 f0, nint f1, int f2, ushort f3, short f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func18AA3S18VyF")]
    private static extern S18 SwiftRetFunc18();

    [Fact]
    public static void TestSwiftRetFunc18()
    {
        Console.Write("Running SwiftRetFunc18: ");
        S18 val = SwiftRetFunc18();
        Assert.Equal((uint)1964425016, val.F0.F0);
        Assert.Equal((float)2767295, val.F0.F1);
        Assert.Equal((nint)unchecked((nint)6016563774923595868), val.F1);
        Assert.Equal((int)1648562735, val.F2);
        Assert.Equal((ushort)378, val.F3);
        Assert.Equal((short)-20536, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct S19
    {
        public byte F0;
        public ushort F1;
        public float F2;
        public ulong F3;
        public int F4;

        public S19(byte f0, ushort f1, float f2, ulong f3, int f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func19AA3S19VyF")]
    private static extern S19 SwiftRetFunc19();

    [Fact]
    public static void TestSwiftRetFunc19()
    {
        Console.Write("Running SwiftRetFunc19: ");
        S19 val = SwiftRetFunc19();
        Assert.Equal((byte)188, val.F0);
        Assert.Equal((ushort)47167, val.F1);
        Assert.Equal((float)6781297, val.F2);
        Assert.Equal((ulong)8140268502944465472, val.F3);
        Assert.Equal((int)708690468, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S20_S0
    {
        public uint F0;
        public float F1;

        public S20_S0(uint f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct S20
    {
        public S20_S0 F0;
        public byte F1;

        public S20(S20_S0 f0, byte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func20AA3S20VyF")]
    private static extern S20 SwiftRetFunc20();

    [Fact]
    public static void TestSwiftRetFunc20()
    {
        Console.Write("Running SwiftRetFunc20: ");
        S20 val = SwiftRetFunc20();
        Assert.Equal((uint)2019361333, val.F0.F0);
        Assert.Equal((float)938975, val.F0.F1);
        Assert.Equal((byte)192, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct S21_S0_S0
    {
        public ushort F0;

        public S21_S0_S0(ushort f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct S21_S0
    {
        public S21_S0_S0 F0;

        public S21_S0(S21_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 42)]
    struct S21
    {
        public double F0;
        public double F1;
        public nuint F2;
        public nint F3;
        public ulong F4;
        public S21_S0 F5;

        public S21(double f0, double f1, nuint f2, nint f3, ulong f4, S21_S0 f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func21AA3S21VyF")]
    private static extern S21 SwiftRetFunc21();

    [Fact]
    public static void TestSwiftRetFunc21()
    {
        Console.Write("Running SwiftRetFunc21: ");
        S21 val = SwiftRetFunc21();
        Assert.Equal((double)1693878073402490, val.F0);
        Assert.Equal((double)3392111340517811, val.F1);
        Assert.Equal((nuint)unchecked((nuint)3584917502172813732), val.F2);
        Assert.Equal((nint)unchecked((nint)665495086154608745), val.F3);
        Assert.Equal((ulong)2918107814961929578, val.F4);
        Assert.Equal((ushort)4634, val.F5.F0.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct S22
    {
        public uint F0;

        public S22(uint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func22AA3S22VyF")]
    private static extern S22 SwiftRetFunc22();

    [Fact]
    public static void TestSwiftRetFunc22()
    {
        Console.Write("Running SwiftRetFunc22: ");
        S22 val = SwiftRetFunc22();
        Assert.Equal((uint)640156952, val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 41)]
    struct S23
    {
        public byte F0;
        public short F1;
        public ulong F2;
        public nuint F3;
        public nuint F4;
        public ulong F5;
        public byte F6;

        public S23(byte f0, short f1, ulong f2, nuint f3, nuint f4, ulong f5, byte f6)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
            F6 = f6;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func23AA3S23VyF")]
    private static extern S23 SwiftRetFunc23();

    [Fact]
    public static void TestSwiftRetFunc23()
    {
        Console.Write("Running SwiftRetFunc23: ");
        S23 val = SwiftRetFunc23();
        Assert.Equal((byte)122, val.F0);
        Assert.Equal((short)28995, val.F1);
        Assert.Equal((ulong)25673626033589541, val.F2);
        Assert.Equal((nuint)unchecked((nuint)828363978755325884), val.F3);
        Assert.Equal((nuint)unchecked((nuint)3065573182429720699), val.F4);
        Assert.Equal((ulong)1484484917001276079, val.F5);
        Assert.Equal((byte)209, val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S24
    {
        public ulong F0;
        public ulong F1;

        public S24(ulong f0, ulong f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func24AA3S24VyF")]
    private static extern S24 SwiftRetFunc24();

    [Fact]
    public static void TestSwiftRetFunc24()
    {
        Console.Write("Running SwiftRetFunc24: ");
        S24 val = SwiftRetFunc24();
        Assert.Equal((ulong)2621245238416080387, val.F0);
        Assert.Equal((ulong)6541787564638363256, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S25_S0
    {
        public nint F0;

        public S25_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct S25
    {
        public sbyte F0;
        public sbyte F1;
        public byte F2;
        public S25_S0 F3;
        public uint F4;

        public S25(sbyte f0, sbyte f1, byte f2, S25_S0 f3, uint f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func25AA3S25VyF")]
    private static extern S25 SwiftRetFunc25();

    [Fact]
    public static void TestSwiftRetFunc25()
    {
        Console.Write("Running SwiftRetFunc25: ");
        S25 val = SwiftRetFunc25();
        Assert.Equal((sbyte)30, val.F0);
        Assert.Equal((sbyte)-8, val.F1);
        Assert.Equal((byte)168, val.F2);
        Assert.Equal((nint)unchecked((nint)7601538494489501573), val.F3.F0);
        Assert.Equal((uint)814523741, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct S26
    {
        public float F0;

        public S26(float f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func26AA3S26VyF")]
    private static extern S26 SwiftRetFunc26();

    [Fact]
    public static void TestSwiftRetFunc26()
    {
        Console.Write("Running SwiftRetFunc26: ");
        S26 val = SwiftRetFunc26();
        Assert.Equal((float)3681545, val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    struct S27
    {
        public long F0;
        public double F1;
        public sbyte F2;
        public nint F3;
        public short F4;
        public long F5;

        public S27(long f0, double f1, sbyte f2, nint f3, short f4, long f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func27AA3S27VyF")]
    private static extern S27 SwiftRetFunc27();

    [Fact]
    public static void TestSwiftRetFunc27()
    {
        Console.Write("Running SwiftRetFunc27: ");
        S27 val = SwiftRetFunc27();
        Assert.Equal((long)4847421047018330189, val.F0);
        Assert.Equal((double)3655171692392280, val.F1);
        Assert.Equal((sbyte)46, val.F2);
        Assert.Equal((nint)unchecked((nint)4476120319602257660), val.F3);
        Assert.Equal((short)-6106, val.F4);
        Assert.Equal((long)5756567968111212829, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S28_S0
    {
        public double F0;

        public S28_S0(double f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S28
    {
        public float F0;
        public short F1;
        public S28_S0 F2;
        public double F3;
        public ulong F4;

        public S28(float f0, short f1, S28_S0 f2, double f3, ulong f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func28AA3S28VyF")]
    private static extern S28 SwiftRetFunc28();

    [Fact]
    public static void TestSwiftRetFunc28()
    {
        Console.Write("Running SwiftRetFunc28: ");
        S28 val = SwiftRetFunc28();
        Assert.Equal((float)3491512, val.F0);
        Assert.Equal((short)5249, val.F1);
        Assert.Equal((double)1107064327388314, val.F2.F0);
        Assert.Equal((double)2170381648425673, val.F3);
        Assert.Equal((ulong)5138313315157580943, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 34)]
    struct S29
    {
        public ushort F0;
        public uint F1;
        public short F2;
        public int F3;
        public int F4;
        public ulong F5;
        public short F6;

        public S29(ushort f0, uint f1, short f2, int f3, int f4, ulong f5, short f6)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
            F6 = f6;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func29AA3S29VyF")]
    private static extern S29 SwiftRetFunc29();

    [Fact]
    public static void TestSwiftRetFunc29()
    {
        Console.Write("Running SwiftRetFunc29: ");
        S29 val = SwiftRetFunc29();
        Assert.Equal((ushort)39000, val.F0);
        Assert.Equal((uint)408611655, val.F1);
        Assert.Equal((short)18090, val.F2);
        Assert.Equal((int)351857085, val.F3);
        Assert.Equal((int)1103441843, val.F4);
        Assert.Equal((ulong)5162040247631126074, val.F5);
        Assert.Equal((short)-27930, val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S30_S0
    {
        public sbyte F0;
        public sbyte F1;
        public int F2;

        public S30_S0(sbyte f0, sbyte f1, int f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct S30_S1
    {
        public float F0;

        public S30_S1(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct S30
    {
        public float F0;
        public S30_S0 F1;
        public S30_S1 F2;
        public long F3;

        public S30(float f0, S30_S0 f1, S30_S1 f2, long f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func30AA3S30VyF")]
    private static extern S30 SwiftRetFunc30();

    [Fact]
    public static void TestSwiftRetFunc30()
    {
        Console.Write("Running SwiftRetFunc30: ");
        S30 val = SwiftRetFunc30();
        Assert.Equal((float)6492602, val.F0);
        Assert.Equal((sbyte)76, val.F1.F0);
        Assert.Equal((sbyte)-26, val.F1.F1);
        Assert.Equal((int)1777644423, val.F1.F2);
        Assert.Equal((float)6558571, val.F2.F0);
        Assert.Equal((long)5879147675377398012, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 21)]
    struct S31
    {
        public long F0;
        public ulong F1;
        public ushort F2;
        public ushort F3;
        public sbyte F4;

        public S31(long f0, ulong f1, ushort f2, ushort f3, sbyte f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func31AA3S31VyF")]
    private static extern S31 SwiftRetFunc31();

    [Fact]
    public static void TestSwiftRetFunc31()
    {
        Console.Write("Running SwiftRetFunc31: ");
        S31 val = SwiftRetFunc31();
        Assert.Equal((long)4699402628739628277, val.F0);
        Assert.Equal((ulong)7062790893852687562, val.F1);
        Assert.Equal((ushort)28087, val.F2);
        Assert.Equal((ushort)11088, val.F3);
        Assert.Equal((sbyte)69, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S32
    {
        public int F0;
        public ulong F1;
        public ulong F2;
        public uint F3;
        public short F4;
        public ushort F5;

        public S32(int f0, ulong f1, ulong f2, uint f3, short f4, ushort f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func32AA3S32VyF")]
    private static extern S32 SwiftRetFunc32();

    [Fact]
    public static void TestSwiftRetFunc32()
    {
        Console.Write("Running SwiftRetFunc32: ");
        S32 val = SwiftRetFunc32();
        Assert.Equal((int)688805466, val.F0);
        Assert.Equal((ulong)8860655326984381661, val.F1);
        Assert.Equal((ulong)6943423675662271404, val.F2);
        Assert.Equal((uint)196368476, val.F3);
        Assert.Equal((short)14229, val.F4);
        Assert.Equal((ushort)34635, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct S33
    {
        public ushort F0;
        public uint F1;
        public int F2;
        public ushort F3;
        public float F4;
        public ulong F5;
        public nint F6;

        public S33(ushort f0, uint f1, int f2, ushort f3, float f4, ulong f5, nint f6)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
            F6 = f6;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func33AA3S33VyF")]
    private static extern S33 SwiftRetFunc33();

    [Fact]
    public static void TestSwiftRetFunc33()
    {
        Console.Write("Running SwiftRetFunc33: ");
        S33 val = SwiftRetFunc33();
        Assert.Equal((ushort)9297, val.F0);
        Assert.Equal((uint)7963252, val.F1);
        Assert.Equal((int)556244690, val.F2);
        Assert.Equal((ushort)19447, val.F3);
        Assert.Equal((float)6930550, val.F4);
        Assert.Equal((ulong)126294981263481729, val.F5);
        Assert.Equal((nint)unchecked((nint)2540579257616511618), val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct S34
    {
        public long F0;
        public uint F1;
        public ulong F2;

        public S34(long f0, uint f1, ulong f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func34AA3S34VyF")]
    private static extern S34 SwiftRetFunc34();

    [Fact]
    public static void TestSwiftRetFunc34()
    {
        Console.Write("Running SwiftRetFunc34: ");
        S34 val = SwiftRetFunc34();
        Assert.Equal((long)5845561428743737556, val.F0);
        Assert.Equal((uint)1358941228, val.F1);
        Assert.Equal((ulong)3701080255861218446, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 34)]
    struct S35
    {
        public float F0;
        public float F1;
        public long F2;
        public byte F3;
        public double F4;
        public ushort F5;

        public S35(float f0, float f1, long f2, byte f3, double f4, ushort f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func35AA3S35VyF")]
    private static extern S35 SwiftRetFunc35();

    [Fact]
    public static void TestSwiftRetFunc35()
    {
        Console.Write("Running SwiftRetFunc35: ");
        S35 val = SwiftRetFunc35();
        Assert.Equal((float)5982956, val.F0);
        Assert.Equal((float)3675164, val.F1);
        Assert.Equal((long)229451138397478297, val.F2);
        Assert.Equal((byte)163, val.F3);
        Assert.Equal((double)2925293762193390, val.F4);
        Assert.Equal((ushort)5018, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct S36
    {
        public int F0;
        public long F1;
        public ulong F2;

        public S36(int f0, long f1, ulong f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func36AA3S36VyF")]
    private static extern S36 SwiftRetFunc36();

    [Fact]
    public static void TestSwiftRetFunc36()
    {
        Console.Write("Running SwiftRetFunc36: ");
        S36 val = SwiftRetFunc36();
        Assert.Equal((int)1915776502, val.F0);
        Assert.Equal((long)2197655909333830531, val.F1);
        Assert.Equal((ulong)6072941592567177049, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S37
    {
        public byte F0;
        public double F1;

        public S37(byte f0, double f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func37AA3S37VyF")]
    private static extern S37 SwiftRetFunc37();

    [Fact]
    public static void TestSwiftRetFunc37()
    {
        Console.Write("Running SwiftRetFunc37: ");
        S37 val = SwiftRetFunc37();
        Assert.Equal((byte)18, val.F0);
        Assert.Equal((double)4063164371882658, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S38
    {
        public nuint F0;
        public long F1;
        public byte F2;
        public nuint F3;

        public S38(nuint f0, long f1, byte f2, nuint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func38AA3S38VyF")]
    private static extern S38 SwiftRetFunc38();

    [Fact]
    public static void TestSwiftRetFunc38()
    {
        Console.Write("Running SwiftRetFunc38: ");
        S38 val = SwiftRetFunc38();
        Assert.Equal((nuint)unchecked((nuint)7389960750529773276), val.F0);
        Assert.Equal((long)2725802169582362061, val.F1);
        Assert.Equal((byte)2, val.F2);
        Assert.Equal((nuint)unchecked((nuint)3659261019360356514), val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct S39
    {
        public int F0;
        public int F1;
        public nint F2;
        public short F3;
        public ushort F4;

        public S39(int f0, int f1, nint f2, short f3, ushort f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func39AA3S39VyF")]
    private static extern S39 SwiftRetFunc39();

    [Fact]
    public static void TestSwiftRetFunc39()
    {
        Console.Write("Running SwiftRetFunc39: ");
        S39 val = SwiftRetFunc39();
        Assert.Equal((int)50995691, val.F0);
        Assert.Equal((int)1623216479, val.F1);
        Assert.Equal((nint)unchecked((nint)2906650346451599789), val.F2);
        Assert.Equal((short)28648, val.F3);
        Assert.Equal((ushort)8278, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct S40_S0
    {
        public float F0;
        public byte F1;
        public sbyte F2;
        public nuint F3;
        public double F4;

        public S40_S0(float f0, byte f1, sbyte f2, nuint f3, double f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct S40
    {
        public S40_S0 F0;
        public short F1;
        public short F2;

        public S40(S40_S0 f0, short f1, short f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func40AA3S40VyF")]
    private static extern S40 SwiftRetFunc40();

    [Fact]
    public static void TestSwiftRetFunc40()
    {
        Console.Write("Running SwiftRetFunc40: ");
        S40 val = SwiftRetFunc40();
        Assert.Equal((float)7087264, val.F0.F0);
        Assert.Equal((byte)37, val.F0.F1);
        Assert.Equal((sbyte)-5, val.F0.F2);
        Assert.Equal((nuint)unchecked((nuint)479915249821490487), val.F0.F3);
        Assert.Equal((double)144033730096589, val.F0.F4);
        Assert.Equal((short)28654, val.F1);
        Assert.Equal((short)16398, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S41
    {
        public nuint F0;
        public nuint F1;

        public S41(nuint f0, nuint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func41AA3S41VyF")]
    private static extern S41 SwiftRetFunc41();

    [Fact]
    public static void TestSwiftRetFunc41()
    {
        Console.Write("Running SwiftRetFunc41: ");
        S41 val = SwiftRetFunc41();
        Assert.Equal((nuint)unchecked((nuint)7923718819069382599), val.F0);
        Assert.Equal((nuint)unchecked((nuint)1539666179674725957), val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct S42_S0
    {
        public int F0;

        public S42_S0(int f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S42
    {
        public uint F0;
        public long F1;
        public S42_S0 F2;
        public nuint F3;

        public S42(uint f0, long f1, S42_S0 f2, nuint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func42AA3S42VyF")]
    private static extern S42 SwiftRetFunc42();

    [Fact]
    public static void TestSwiftRetFunc42()
    {
        Console.Write("Running SwiftRetFunc42: ");
        S42 val = SwiftRetFunc42();
        Assert.Equal((uint)1046060439, val.F0);
        Assert.Equal((long)8249831314190867613, val.F1);
        Assert.Equal((int)1097582349, val.F2.F0);
        Assert.Equal((nuint)unchecked((nuint)2864677262092469436), val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct S43_S0_S0
    {
        public float F0;

        public S43_S0_S0(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct S43_S0
    {
        public S43_S0_S0 F0;

        public S43_S0(S43_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 5)]
    struct S43
    {
        public S43_S0 F0;
        public sbyte F1;

        public S43(S43_S0 f0, sbyte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func43AA3S43VyF")]
    private static extern S43 SwiftRetFunc43();

    [Fact]
    public static void TestSwiftRetFunc43()
    {
        Console.Write("Running SwiftRetFunc43: ");
        S43 val = SwiftRetFunc43();
        Assert.Equal((float)1586338, val.F0.F0.F0);
        Assert.Equal((sbyte)104, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct S44
    {
        public byte F0;
        public int F1;
        public nint F2;
        public uint F3;

        public S44(byte f0, int f1, nint f2, uint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func44AA3S44VyF")]
    private static extern S44 SwiftRetFunc44();

    [Fact]
    public static void TestSwiftRetFunc44()
    {
        Console.Write("Running SwiftRetFunc44: ");
        S44 val = SwiftRetFunc44();
        Assert.Equal((byte)94, val.F0);
        Assert.Equal((int)1109076022, val.F1);
        Assert.Equal((nint)unchecked((nint)3135595850598607828), val.F2);
        Assert.Equal((uint)760084013, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S45_S0
    {
        public long F0;

        public S45_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S45
    {
        public short F0;
        public ulong F1;
        public nint F2;
        public S45_S0 F3;

        public S45(short f0, ulong f1, nint f2, S45_S0 f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func45AA3S45VyF")]
    private static extern S45 SwiftRetFunc45();

    [Fact]
    public static void TestSwiftRetFunc45()
    {
        Console.Write("Running SwiftRetFunc45: ");
        S45 val = SwiftRetFunc45();
        Assert.Equal((short)3071, val.F0);
        Assert.Equal((ulong)5908138438609341766, val.F1);
        Assert.Equal((nint)unchecked((nint)5870206722419946629), val.F2);
        Assert.Equal((long)8128455876189744801, val.F3.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S46
    {
        public short F0;
        public sbyte F1;
        public sbyte F2;
        public uint F3;
        public byte F4;
        public int F5;

        public S46(short f0, sbyte f1, sbyte f2, uint f3, byte f4, int f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func46AA3S46VyF")]
    private static extern S46 SwiftRetFunc46();

    [Fact]
    public static void TestSwiftRetFunc46()
    {
        Console.Write("Running SwiftRetFunc46: ");
        S46 val = SwiftRetFunc46();
        Assert.Equal((short)14794, val.F0);
        Assert.Equal((sbyte)60, val.F1);
        Assert.Equal((sbyte)-77, val.F2);
        Assert.Equal((uint)653898879, val.F3);
        Assert.Equal((byte)224, val.F4);
        Assert.Equal((int)266602433, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct S47_S0
    {
        public sbyte F0;

        public S47_S0(sbyte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct S47
    {
        public double F0;
        public S47_S0 F1;

        public S47(double f0, S47_S0 f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func47AA3S47VyF")]
    private static extern S47 SwiftRetFunc47();

    [Fact]
    public static void TestSwiftRetFunc47()
    {
        Console.Write("Running SwiftRetFunc47: ");
        S47 val = SwiftRetFunc47();
        Assert.Equal((double)3195976594911793, val.F0);
        Assert.Equal((sbyte)-91, val.F1.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S48
    {
        public nint F0;

        public S48(nint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func48AA3S48VyF")]
    private static extern S48 SwiftRetFunc48();

    [Fact]
    public static void TestSwiftRetFunc48()
    {
        Console.Write("Running SwiftRetFunc48: ");
        S48 val = SwiftRetFunc48();
        Assert.Equal((nint)unchecked((nint)778504172538154682), val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S49_S0_S0
    {
        public ulong F0;

        public S49_S0_S0(ulong f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S49_S0
    {
        public S49_S0_S0 F0;

        public S49_S0(S49_S0_S0 f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct S49
    {
        public ulong F0;
        public S49_S0 F1;
        public sbyte F2;
        public double F3;
        public uint F4;
        public uint F5;

        public S49(ulong f0, S49_S0 f1, sbyte f2, double f3, uint f4, uint f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func49AA3S49VyF")]
    private static extern S49 SwiftRetFunc49();

    [Fact]
    public static void TestSwiftRetFunc49()
    {
        Console.Write("Running SwiftRetFunc49: ");
        S49 val = SwiftRetFunc49();
        Assert.Equal((ulong)4235011519458710874, val.F0);
        Assert.Equal((ulong)3120420438742285733, val.F1.F0.F0);
        Assert.Equal((sbyte)-8, val.F2);
        Assert.Equal((double)1077419570643725, val.F3);
        Assert.Equal((uint)1985303212, val.F4);
        Assert.Equal((uint)264580506, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct S50
    {
        public int F0;

        public S50(int f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func50AA3S50VyF")]
    private static extern S50 SwiftRetFunc50();

    [Fact]
    public static void TestSwiftRetFunc50()
    {
        Console.Write("Running SwiftRetFunc50: ");
        S50 val = SwiftRetFunc50();
        Assert.Equal((int)1043912405, val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct S51_S0_S0_S0
    {
        public float F0;

        public S51_S0_S0_S0(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 6)]
    struct S51_S0_S0
    {
        public S51_S0_S0_S0 F0;
        public short F1;

        public S51_S0_S0(S51_S0_S0_S0 f0, short f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct S51_S0
    {
        public double F0;
        public S51_S0_S0 F1;
        public byte F2;
        public long F3;

        public S51_S0(double f0, S51_S0_S0 f1, byte f2, long f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S51
    {
        public S51_S0 F0;
        public double F1;

        public S51(S51_S0 f0, double f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func51AA3S51VyF")]
    private static extern S51 SwiftRetFunc51();

    [Fact]
    public static void TestSwiftRetFunc51()
    {
        Console.Write("Running SwiftRetFunc51: ");
        S51 val = SwiftRetFunc51();
        Assert.Equal((double)3266680719186600, val.F0.F0);
        Assert.Equal((float)428247, val.F0.F1.F0.F0);
        Assert.Equal((short)-24968, val.F0.F1.F1);
        Assert.Equal((byte)76, val.F0.F2);
        Assert.Equal((long)183022772513065490, val.F0.F3);
        Assert.Equal((double)2661928101793033, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 41)]
    struct S52
    {
        public uint F0;
        public long F1;
        public uint F2;
        public ulong F3;
        public nint F4;
        public sbyte F5;

        public S52(uint f0, long f1, uint f2, ulong f3, nint f4, sbyte f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func52AA3S52VyF")]
    private static extern S52 SwiftRetFunc52();

    [Fact]
    public static void TestSwiftRetFunc52()
    {
        Console.Write("Running SwiftRetFunc52: ");
        S52 val = SwiftRetFunc52();
        Assert.Equal((uint)1812191671, val.F0);
        Assert.Equal((long)6594574760089190928, val.F1);
        Assert.Equal((uint)831147243, val.F2);
        Assert.Equal((ulong)3301835731003365248, val.F3);
        Assert.Equal((nint)unchecked((nint)5382332538247340743), val.F4);
        Assert.Equal((sbyte)-77, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S53_S0
    {
        public sbyte F0;
        public nuint F1;

        public S53_S0(sbyte f0, nuint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 37)]
    struct S53
    {
        public S53_S0 F0;
        public int F1;
        public long F2;
        public float F3;
        public sbyte F4;

        public S53(S53_S0 f0, int f1, long f2, float f3, sbyte f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func53AA3S53VyF")]
    private static extern S53 SwiftRetFunc53();

    [Fact]
    public static void TestSwiftRetFunc53()
    {
        Console.Write("Running SwiftRetFunc53: ");
        S53 val = SwiftRetFunc53();
        Assert.Equal((sbyte)-123, val.F0.F0);
        Assert.Equal((nuint)unchecked((nuint)3494916243607193741), val.F0.F1);
        Assert.Equal((int)1406699798, val.F1);
        Assert.Equal((long)4018943158751734338, val.F2);
        Assert.Equal((float)1084415, val.F3);
        Assert.Equal((sbyte)-8, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S54_S0
    {
        public double F0;

        public S54_S0(double f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S54
    {
        public nint F0;
        public nint F1;
        public S54_S0 F2;
        public long F3;

        public S54(nint f0, nint f1, S54_S0 f2, long f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func54AA3S54VyF")]
    private static extern S54 SwiftRetFunc54();

    [Fact]
    public static void TestSwiftRetFunc54()
    {
        Console.Write("Running SwiftRetFunc54: ");
        S54 val = SwiftRetFunc54();
        Assert.Equal((nint)unchecked((nint)8623517456704997133), val.F0);
        Assert.Equal((nint)unchecked((nint)1521939500434086364), val.F1);
        Assert.Equal((double)3472783299414218, val.F2.F0);
        Assert.Equal((long)4761507229870258916, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 22)]
    struct S55
    {
        public short F0;
        public uint F1;
        public long F2;
        public uint F3;
        public sbyte F4;
        public byte F5;

        public S55(short f0, uint f1, long f2, uint f3, sbyte f4, byte f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func55AA3S55VyF")]
    private static extern S55 SwiftRetFunc55();

    [Fact]
    public static void TestSwiftRetFunc55()
    {
        Console.Write("Running SwiftRetFunc55: ");
        S55 val = SwiftRetFunc55();
        Assert.Equal((short)-28051, val.F0);
        Assert.Equal((uint)1759912152, val.F1);
        Assert.Equal((long)2038322238348454200, val.F2);
        Assert.Equal((uint)601094102, val.F3);
        Assert.Equal((sbyte)5, val.F4);
        Assert.Equal((byte)75, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct S56
    {
        public ulong F0;
        public float F1;
        public sbyte F2;
        public int F3;

        public S56(ulong f0, float f1, sbyte f2, int f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func56AA3S56VyF")]
    private static extern S56 SwiftRetFunc56();

    [Fact]
    public static void TestSwiftRetFunc56()
    {
        Console.Write("Running SwiftRetFunc56: ");
        S56 val = SwiftRetFunc56();
        Assert.Equal((ulong)6313168909786453069, val.F0);
        Assert.Equal((float)6254558, val.F1);
        Assert.Equal((sbyte)115, val.F2);
        Assert.Equal((int)847834891, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S57
    {
        public nuint F0;
        public short F1;
        public sbyte F2;
        public int F3;

        public S57(nuint f0, short f1, sbyte f2, int f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func57AA3S57VyF")]
    private static extern S57 SwiftRetFunc57();

    [Fact]
    public static void TestSwiftRetFunc57()
    {
        Console.Write("Running SwiftRetFunc57: ");
        S57 val = SwiftRetFunc57();
        Assert.Equal((nuint)unchecked((nuint)546304219852233452), val.F0);
        Assert.Equal((short)-27416, val.F1);
        Assert.Equal((sbyte)47, val.F2);
        Assert.Equal((int)1094575684, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S58
    {
        public ulong F0;
        public ulong F1;

        public S58(ulong f0, ulong f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func58AA3S58VyF")]
    private static extern S58 SwiftRetFunc58();

    [Fact]
    public static void TestSwiftRetFunc58()
    {
        Console.Write("Running SwiftRetFunc58: ");
        S58 val = SwiftRetFunc58();
        Assert.Equal((ulong)4612004722568513699, val.F0);
        Assert.Equal((ulong)2222525519606580195, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 41)]
    struct S59
    {
        public sbyte F0;
        public nuint F1;
        public nint F2;
        public sbyte F3;
        public long F4;
        public byte F5;

        public S59(sbyte f0, nuint f1, nint f2, sbyte f3, long f4, byte f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func59AA3S59VyF")]
    private static extern S59 SwiftRetFunc59();

    [Fact]
    public static void TestSwiftRetFunc59()
    {
        Console.Write("Running SwiftRetFunc59: ");
        S59 val = SwiftRetFunc59();
        Assert.Equal((sbyte)-92, val.F0);
        Assert.Equal((nuint)unchecked((nuint)7281011081566942937), val.F1);
        Assert.Equal((nint)unchecked((nint)8203439771560005792), val.F2);
        Assert.Equal((sbyte)103, val.F3);
        Assert.Equal((long)1003386607251132236, val.F4);
        Assert.Equal((byte)6, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S60
    {
        public ulong F0;
        public nint F1;

        public S60(ulong f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func60AA3S60VyF")]
    private static extern S60 SwiftRetFunc60();

    [Fact]
    public static void TestSwiftRetFunc60()
    {
        Console.Write("Running SwiftRetFunc60: ");
        S60 val = SwiftRetFunc60();
        Assert.Equal((ulong)6922353269487057763, val.F0);
        Assert.Equal((nint)unchecked((nint)103032455997325768), val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct S61_S0
    {
        public long F0;
        public long F1;
        public float F2;

        public S61_S0(long f0, long f1, float f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 36)]
    struct S61
    {
        public ulong F0;
        public S61_S0 F1;
        public short F2;
        public int F3;

        public S61(ulong f0, S61_S0 f1, short f2, int f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func61AA3S61VyF")]
    private static extern S61 SwiftRetFunc61();

    [Fact]
    public static void TestSwiftRetFunc61()
    {
        Console.Write("Running SwiftRetFunc61: ");
        S61 val = SwiftRetFunc61();
        Assert.Equal((ulong)3465845922566501572, val.F0);
        Assert.Equal((long)8266662359091888314, val.F1.F0);
        Assert.Equal((long)7511705648638703076, val.F1.F1);
        Assert.Equal((float)535470, val.F1.F2);
        Assert.Equal((short)-5945, val.F2);
        Assert.Equal((int)523043523, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S62_S0_S0
    {
        public nint F0;

        public S62_S0_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S62_S0
    {
        public ushort F0;
        public short F1;
        public ushort F2;
        public S62_S0_S0 F3;

        public S62_S0(ushort f0, short f1, ushort f2, S62_S0_S0 f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 26)]
    struct S62
    {
        public S62_S0 F0;
        public nint F1;
        public ushort F2;

        public S62(S62_S0 f0, nint f1, ushort f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func62AA3S62VyF")]
    private static extern S62 SwiftRetFunc62();

    [Fact]
    public static void TestSwiftRetFunc62()
    {
        Console.Write("Running SwiftRetFunc62: ");
        S62 val = SwiftRetFunc62();
        Assert.Equal((ushort)50789, val.F0.F0);
        Assert.Equal((short)30245, val.F0.F1);
        Assert.Equal((ushort)35063, val.F0.F2);
        Assert.Equal((nint)unchecked((nint)3102684963408623932), val.F0.F3.F0);
        Assert.Equal((nint)unchecked((nint)792877586576090769), val.F1);
        Assert.Equal((ushort)24697, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S63
    {
        public double F0;
        public nint F1;
        public double F2;
        public sbyte F3;
        public float F4;

        public S63(double f0, nint f1, double f2, sbyte f3, float f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func63AA3S63VyF")]
    private static extern S63 SwiftRetFunc63();

    [Fact]
    public static void TestSwiftRetFunc63()
    {
        Console.Write("Running SwiftRetFunc63: ");
        S63 val = SwiftRetFunc63();
        Assert.Equal((double)4097323000009314, val.F0);
        Assert.Equal((nint)unchecked((nint)4162427097168837193), val.F1);
        Assert.Equal((double)140736061437152, val.F2);
        Assert.Equal((sbyte)-59, val.F3);
        Assert.Equal((float)7331757, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S64_S0
    {
        public ulong F0;

        public S64_S0(ulong f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S64
    {
        public S64_S0 F0;
        public ulong F1;
        public long F2;
        public nint F3;

        public S64(S64_S0 f0, ulong f1, long f2, nint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func64AA3S64VyF")]
    private static extern S64 SwiftRetFunc64();

    [Fact]
    public static void TestSwiftRetFunc64()
    {
        Console.Write("Running SwiftRetFunc64: ");
        S64 val = SwiftRetFunc64();
        Assert.Equal((ulong)2624461610177878495, val.F0.F0);
        Assert.Equal((ulong)5222178027019975511, val.F1);
        Assert.Equal((long)9006949357929457355, val.F2);
        Assert.Equal((nint)unchecked((nint)7966680593035770540), val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct S65
    {
        public nint F0;
        public double F1;
        public ushort F2;
        public short F3;
        public byte F4;
        public int F5;
        public ulong F6;

        public S65(nint f0, double f1, ushort f2, short f3, byte f4, int f5, ulong f6)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
            F6 = f6;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func65AA3S65VyF")]
    private static extern S65 SwiftRetFunc65();

    [Fact]
    public static void TestSwiftRetFunc65()
    {
        Console.Write("Running SwiftRetFunc65: ");
        S65 val = SwiftRetFunc65();
        Assert.Equal((nint)unchecked((nint)6080968957098434687), val.F0);
        Assert.Equal((double)3067343828504927, val.F1);
        Assert.Equal((ushort)56887, val.F2);
        Assert.Equal((short)804, val.F3);
        Assert.Equal((byte)235, val.F4);
        Assert.Equal((int)121742660, val.F5);
        Assert.Equal((ulong)9218677163034827308, val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct S66
    {
        public sbyte F0;
        public ulong F1;
        public uint F2;
        public ulong F3;
        public ulong F4;

        public S66(sbyte f0, ulong f1, uint f2, ulong f3, ulong f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func66AA3S66VyF")]
    private static extern S66 SwiftRetFunc66();

    [Fact]
    public static void TestSwiftRetFunc66()
    {
        Console.Write("Running SwiftRetFunc66: ");
        S66 val = SwiftRetFunc66();
        Assert.Equal((sbyte)-16, val.F0);
        Assert.Equal((ulong)7967447403042597794, val.F1);
        Assert.Equal((uint)2029697750, val.F2);
        Assert.Equal((ulong)4180031087394830849, val.F3);
        Assert.Equal((ulong)5847795120921557969, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S67_S0
    {
        public ulong F0;

        public S67_S0(ulong f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 33)]
    struct S67
    {
        public S67_S0 F0;
        public byte F1;
        public ushort F2;
        public ulong F3;
        public ulong F4;
        public sbyte F5;

        public S67(S67_S0 f0, byte f1, ushort f2, ulong f3, ulong f4, sbyte f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func67AA3S67VyF")]
    private static extern S67 SwiftRetFunc67();

    [Fact]
    public static void TestSwiftRetFunc67()
    {
        Console.Write("Running SwiftRetFunc67: ");
        S67 val = SwiftRetFunc67();
        Assert.Equal((ulong)4844204675254434929, val.F0.F0);
        Assert.Equal((byte)135, val.F1);
        Assert.Equal((ushort)13969, val.F2);
        Assert.Equal((ulong)4897129719050177731, val.F3);
        Assert.Equal((ulong)7233638107485862921, val.F4);
        Assert.Equal((sbyte)-11, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S68_S0
    {
        public double F0;

        public S68_S0(double f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 37)]
    struct S68
    {
        public int F0;
        public ulong F1;
        public uint F2;
        public S68_S0 F3;
        public int F4;
        public sbyte F5;

        public S68(int f0, ulong f1, uint f2, S68_S0 f3, int f4, sbyte f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func68AA3S68VyF")]
    private static extern S68 SwiftRetFunc68();

    [Fact]
    public static void TestSwiftRetFunc68()
    {
        Console.Write("Running SwiftRetFunc68: ");
        S68 val = SwiftRetFunc68();
        Assert.Equal((int)1708606840, val.F0);
        Assert.Equal((ulong)1768121573985581212, val.F1);
        Assert.Equal((uint)1033319213, val.F2);
        Assert.Equal((double)2741322436867931, val.F3.F0);
        Assert.Equal((int)955320338, val.F4);
        Assert.Equal((sbyte)12, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct S69
    {
        public uint F0;

        public S69(uint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func69AA3S69VyF")]
    private static extern S69 SwiftRetFunc69();

    [Fact]
    public static void TestSwiftRetFunc69()
    {
        Console.Write("Running SwiftRetFunc69: ");
        S69 val = SwiftRetFunc69();
        Assert.Equal((uint)2092746473, val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S70
    {
        public byte F0;
        public float F1;

        public S70(byte f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func70AA3S70VyF")]
    private static extern S70 SwiftRetFunc70();

    [Fact]
    public static void TestSwiftRetFunc70()
    {
        Console.Write("Running SwiftRetFunc70: ");
        S70 val = SwiftRetFunc70();
        Assert.Equal((byte)76, val.F0);
        Assert.Equal((float)4138467, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct S71_S0
    {
        public sbyte F0;
        public ulong F1;
        public long F2;

        public S71_S0(sbyte f0, ulong f1, long f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 26)]
    struct S71
    {
        public S71_S0 F0;
        public byte F1;
        public byte F2;

        public S71(S71_S0 f0, byte f1, byte f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func71AA3S71VyF")]
    private static extern S71 SwiftRetFunc71();

    [Fact]
    public static void TestSwiftRetFunc71()
    {
        Console.Write("Running SwiftRetFunc71: ");
        S71 val = SwiftRetFunc71();
        Assert.Equal((sbyte)-98, val.F0.F0);
        Assert.Equal((ulong)8603744544763953916, val.F0.F1);
        Assert.Equal((long)8460721064583106347, val.F0.F2);
        Assert.Equal((byte)10, val.F1);
        Assert.Equal((byte)88, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct S72
    {
        public uint F0;

        public S72(uint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func72AA3S72VyF")]
    private static extern S72 SwiftRetFunc72();

    [Fact]
    public static void TestSwiftRetFunc72()
    {
        Console.Write("Running SwiftRetFunc72: ");
        S72 val = SwiftRetFunc72();
        Assert.Equal((uint)2021509367, val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    struct S73
    {
        public nint F0;
        public short F1;
        public ulong F2;
        public float F3;
        public int F4;
        public nuint F5;
        public nuint F6;

        public S73(nint f0, short f1, ulong f2, float f3, int f4, nuint f5, nuint f6)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
            F6 = f6;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func73AA3S73VyF")]
    private static extern S73 SwiftRetFunc73();

    [Fact]
    public static void TestSwiftRetFunc73()
    {
        Console.Write("Running SwiftRetFunc73: ");
        S73 val = SwiftRetFunc73();
        Assert.Equal((nint)unchecked((nint)6222563427944465437), val.F0);
        Assert.Equal((short)28721, val.F1);
        Assert.Equal((ulong)1313300783845289148, val.F2);
        Assert.Equal((float)6761, val.F3);
        Assert.Equal((int)2074171265, val.F4);
        Assert.Equal((nuint)unchecked((nuint)6232209228889209160), val.F5);
        Assert.Equal((nuint)unchecked((nuint)1423931135184844265), val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 19)]
    struct S74
    {
        public short F0;
        public float F1;
        public double F2;
        public ushort F3;
        public sbyte F4;

        public S74(short f0, float f1, double f2, ushort f3, sbyte f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func74AA3S74VyF")]
    private static extern S74 SwiftRetFunc74();

    [Fact]
    public static void TestSwiftRetFunc74()
    {
        Console.Write("Running SwiftRetFunc74: ");
        S74 val = SwiftRetFunc74();
        Assert.Equal((short)27115, val.F0);
        Assert.Equal((float)1416098, val.F1);
        Assert.Equal((double)4468576755457331, val.F2);
        Assert.Equal((ushort)58864, val.F3);
        Assert.Equal((sbyte)81, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct S75_S0_S0
    {
        public sbyte F0;

        public S75_S0_S0(sbyte f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct S75_S0
    {
        public S75_S0_S0 F0;
        public byte F1;

        public S75_S0(S75_S0_S0 f0, byte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 11)]
    struct S75
    {
        public ulong F0;
        public S75_S0 F1;
        public byte F2;

        public S75(ulong f0, S75_S0 f1, byte f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func75AA3S75VyF")]
    private static extern S75 SwiftRetFunc75();

    [Fact]
    public static void TestSwiftRetFunc75()
    {
        Console.Write("Running SwiftRetFunc75: ");
        S75 val = SwiftRetFunc75();
        Assert.Equal((ulong)8532911974860912350, val.F0);
        Assert.Equal((sbyte)-60, val.F1.F0.F0);
        Assert.Equal((byte)66, val.F1.F1);
        Assert.Equal((byte)200, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct S76_S0_S0
    {
        public short F0;

        public S76_S0_S0(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S76_S0
    {
        public sbyte F0;
        public ulong F1;
        public S76_S0_S0 F2;
        public double F3;

        public S76_S0(sbyte f0, ulong f1, S76_S0_S0 f2, double f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    struct S76
    {
        public byte F0;
        public S76_S0 F1;
        public double F2;

        public S76(byte f0, S76_S0 f1, double f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func76AA3S76VyF")]
    private static extern S76 SwiftRetFunc76();

    [Fact]
    public static void TestSwiftRetFunc76()
    {
        Console.Write("Running SwiftRetFunc76: ");
        S76 val = SwiftRetFunc76();
        Assert.Equal((byte)69, val.F0);
        Assert.Equal((sbyte)-29, val.F1.F0);
        Assert.Equal((ulong)4872234474620951743, val.F1.F1);
        Assert.Equal((short)11036, val.F1.F2.F0);
        Assert.Equal((double)585486652063917, val.F1.F3);
        Assert.Equal((double)2265391710186639, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct S77
    {
        public int F0;
        public int F1;
        public int F2;
        public uint F3;
        public short F4;

        public S77(int f0, int f1, int f2, uint f3, short f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func77AA3S77VyF")]
    private static extern S77 SwiftRetFunc77();

    [Fact]
    public static void TestSwiftRetFunc77()
    {
        Console.Write("Running SwiftRetFunc77: ");
        S77 val = SwiftRetFunc77();
        Assert.Equal((int)4495211, val.F0);
        Assert.Equal((int)1364377405, val.F1);
        Assert.Equal((int)773989694, val.F2);
        Assert.Equal((uint)1121696315, val.F3);
        Assert.Equal((short)7589, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S78
    {
        public uint F0;
        public nuint F1;

        public S78(uint f0, nuint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func78AA3S78VyF")]
    private static extern S78 SwiftRetFunc78();

    [Fact]
    public static void TestSwiftRetFunc78()
    {
        Console.Write("Running SwiftRetFunc78: ");
        S78 val = SwiftRetFunc78();
        Assert.Equal((uint)1767839225, val.F0);
        Assert.Equal((nuint)unchecked((nuint)7917317019379224114), val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S79_S0
    {
        public double F0;
        public uint F1;
        public int F2;

        public S79_S0(double f0, uint f1, int f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S79
    {
        public S79_S0 F0;
        public byte F1;
        public double F2;

        public S79(S79_S0 f0, byte f1, double f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func79AA3S79VyF")]
    private static extern S79 SwiftRetFunc79();

    [Fact]
    public static void TestSwiftRetFunc79()
    {
        Console.Write("Running SwiftRetFunc79: ");
        S79 val = SwiftRetFunc79();
        Assert.Equal((double)495074072703635, val.F0.F0);
        Assert.Equal((uint)417605286, val.F0.F1);
        Assert.Equal((int)171326442, val.F0.F2);
        Assert.Equal((byte)203, val.F1);
        Assert.Equal((double)2976663235490421, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 7)]
    struct S80
    {
        public int F0;
        public short F1;
        public sbyte F2;

        public S80(int f0, short f1, sbyte f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func80AA3S80VyF")]
    private static extern S80 SwiftRetFunc80();

    [Fact]
    public static void TestSwiftRetFunc80()
    {
        Console.Write("Running SwiftRetFunc80: ");
        S80 val = SwiftRetFunc80();
        Assert.Equal((int)999559959, val.F0);
        Assert.Equal((short)19977, val.F1);
        Assert.Equal((sbyte)-4, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S81_S0
    {
        public nuint F0;

        public S81_S0(nuint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct S81
    {
        public int F0;
        public S81_S0 F1;
        public float F2;
        public long F3;
        public uint F4;
        public byte F5;
        public short F6;

        public S81(int f0, S81_S0 f1, float f2, long f3, uint f4, byte f5, short f6)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
            F6 = f6;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func81AA3S81VyF")]
    private static extern S81 SwiftRetFunc81();

    [Fact]
    public static void TestSwiftRetFunc81()
    {
        Console.Write("Running SwiftRetFunc81: ");
        S81 val = SwiftRetFunc81();
        Assert.Equal((int)452603110, val.F0);
        Assert.Equal((nuint)unchecked((nuint)6240652733420985265), val.F1.F0);
        Assert.Equal((float)6469988, val.F2);
        Assert.Equal((long)5775316279348621124, val.F3);
        Assert.Equal((uint)1398033592, val.F4);
        Assert.Equal((byte)105, val.F5);
        Assert.Equal((short)21937, val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S82
    {
        public nint F0;

        public S82(nint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func82AA3S82VyF")]
    private static extern S82 SwiftRetFunc82();

    [Fact]
    public static void TestSwiftRetFunc82()
    {
        Console.Write("Running SwiftRetFunc82: ");
        S82 val = SwiftRetFunc82();
        Assert.Equal((nint)unchecked((nint)6454754584537364459), val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct S83
    {
        public ulong F0;
        public uint F1;
        public float F2;
        public byte F3;
        public float F4;

        public S83(ulong f0, uint f1, float f2, byte f3, float f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func83AA3S83VyF")]
    private static extern S83 SwiftRetFunc83();

    [Fact]
    public static void TestSwiftRetFunc83()
    {
        Console.Write("Running SwiftRetFunc83: ");
        S83 val = SwiftRetFunc83();
        Assert.Equal((ulong)2998238441521688907, val.F0);
        Assert.Equal((uint)9623946, val.F1);
        Assert.Equal((float)2577885, val.F2);
        Assert.Equal((byte)156, val.F3);
        Assert.Equal((float)6678807, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct S84_S0
    {
        public short F0;

        public S84_S0(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct S84
    {
        public S84_S0 F0;

        public S84(S84_S0 f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func84AA3S84VyF")]
    private static extern S84 SwiftRetFunc84();

    [Fact]
    public static void TestSwiftRetFunc84()
    {
        Console.Write("Running SwiftRetFunc84: ");
        S84 val = SwiftRetFunc84();
        Assert.Equal((short)16213, val.F0.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 3)]
    struct S85_S0
    {
        public short F0;
        public sbyte F1;

        public S85_S0(short f0, sbyte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S85
    {
        public long F0;
        public byte F1;
        public S85_S0 F2;
        public float F3;
        public nint F4;

        public S85(long f0, byte f1, S85_S0 f2, float f3, nint f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func85AA3S85VyF")]
    private static extern S85 SwiftRetFunc85();

    [Fact]
    public static void TestSwiftRetFunc85()
    {
        Console.Write("Running SwiftRetFunc85: ");
        S85 val = SwiftRetFunc85();
        Assert.Equal((long)8858924985061791416, val.F0);
        Assert.Equal((byte)200, val.F1);
        Assert.Equal((short)4504, val.F2.F0);
        Assert.Equal((sbyte)60, val.F2.F1);
        Assert.Equal((float)5572917, val.F3);
        Assert.Equal((nint)unchecked((nint)6546369836182556538), val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct S86
    {
        public ushort F0;
        public float F1;
        public uint F2;

        public S86(ushort f0, float f1, uint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func86AA3S86VyF")]
    private static extern S86 SwiftRetFunc86();

    [Fact]
    public static void TestSwiftRetFunc86()
    {
        Console.Write("Running SwiftRetFunc86: ");
        S86 val = SwiftRetFunc86();
        Assert.Equal((ushort)22762, val.F0);
        Assert.Equal((float)4672435, val.F1);
        Assert.Equal((uint)719927700, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct S87
    {
        public int F0;
        public nuint F1;
        public ulong F2;

        public S87(int f0, nuint f1, ulong f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func87AA3S87VyF")]
    private static extern S87 SwiftRetFunc87();

    [Fact]
    public static void TestSwiftRetFunc87()
    {
        Console.Write("Running SwiftRetFunc87: ");
        S87 val = SwiftRetFunc87();
        Assert.Equal((int)361750184, val.F0);
        Assert.Equal((nuint)unchecked((nuint)4206825694012787823), val.F1);
        Assert.Equal((ulong)2885153391732919282, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct S88
    {
        public uint F0;
        public short F1;
        public uint F2;

        public S88(uint f0, short f1, uint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func88AA3S88VyF")]
    private static extern S88 SwiftRetFunc88();

    [Fact]
    public static void TestSwiftRetFunc88()
    {
        Console.Write("Running SwiftRetFunc88: ");
        S88 val = SwiftRetFunc88();
        Assert.Equal((uint)2125094198, val.F0);
        Assert.Equal((short)-10705, val.F1);
        Assert.Equal((uint)182007583, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct S89
    {
        public byte F0;
        public uint F1;
        public int F2;
        public sbyte F3;
        public long F4;

        public S89(byte f0, uint f1, int f2, sbyte f3, long f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func89AA3S89VyF")]
    private static extern S89 SwiftRetFunc89();

    [Fact]
    public static void TestSwiftRetFunc89()
    {
        Console.Write("Running SwiftRetFunc89: ");
        S89 val = SwiftRetFunc89();
        Assert.Equal((byte)175, val.F0);
        Assert.Equal((uint)1062985476, val.F1);
        Assert.Equal((int)1019006263, val.F2);
        Assert.Equal((sbyte)-22, val.F3);
        Assert.Equal((long)6888877252788498422, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct S90
    {
        public byte F0;
        public int F1;
        public short F2;
        public nint F3;
        public uint F4;
        public uint F5;
        public long F6;

        public S90(byte f0, int f1, short f2, nint f3, uint f4, uint f5, long f6)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
            F6 = f6;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func90AA3S90VyF")]
    private static extern S90 SwiftRetFunc90();

    [Fact]
    public static void TestSwiftRetFunc90()
    {
        Console.Write("Running SwiftRetFunc90: ");
        S90 val = SwiftRetFunc90();
        Assert.Equal((byte)221, val.F0);
        Assert.Equal((int)225825436, val.F1);
        Assert.Equal((short)-26231, val.F2);
        Assert.Equal((nint)unchecked((nint)5122880520199505508), val.F3);
        Assert.Equal((uint)907657092, val.F4);
        Assert.Equal((uint)707089277, val.F5);
        Assert.Equal((long)6091814344013414920, val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct S91
    {
        public double F0;
        public sbyte F1;
        public sbyte F2;
        public uint F3;
        public nint F4;
        public sbyte F5;
        public short F6;

        public S91(double f0, sbyte f1, sbyte f2, uint f3, nint f4, sbyte f5, short f6)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
            F6 = f6;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func91AA3S91VyF")]
    private static extern S91 SwiftRetFunc91();

    [Fact]
    public static void TestSwiftRetFunc91()
    {
        Console.Write("Running SwiftRetFunc91: ");
        S91 val = SwiftRetFunc91();
        Assert.Equal((double)3265110225161261, val.F0);
        Assert.Equal((sbyte)62, val.F1);
        Assert.Equal((sbyte)-38, val.F2);
        Assert.Equal((uint)946023589, val.F3);
        Assert.Equal((nint)unchecked((nint)4109819715069879890), val.F4);
        Assert.Equal((sbyte)-73, val.F5);
        Assert.Equal((short)20363, val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S92_S0
    {
        public float F0;
        public long F1;

        public S92_S0(float f0, long f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 44)]
    struct S92
    {
        public long F0;
        public nuint F1;
        public S92_S0 F2;
        public int F3;
        public float F4;
        public float F5;

        public S92(long f0, nuint f1, S92_S0 f2, int f3, float f4, float f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func92AA3S92VyF")]
    private static extern S92 SwiftRetFunc92();

    [Fact]
    public static void TestSwiftRetFunc92()
    {
        Console.Write("Running SwiftRetFunc92: ");
        S92 val = SwiftRetFunc92();
        Assert.Equal((long)3230438394207610137, val.F0);
        Assert.Equal((nuint)unchecked((nuint)3003396252681176136), val.F1);
        Assert.Equal((float)6494422, val.F2.F0);
        Assert.Equal((long)2971773224350614312, val.F2.F1);
        Assert.Equal((int)2063694141, val.F3);
        Assert.Equal((float)3117041, val.F4);
        Assert.Equal((float)1003760, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct S93
    {
        public nint F0;
        public byte F1;
        public uint F2;
        public uint F3;
        public ulong F4;

        public S93(nint f0, byte f1, uint f2, uint f3, ulong f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func93AA3S93VyF")]
    private static extern S93 SwiftRetFunc93();

    [Fact]
    public static void TestSwiftRetFunc93()
    {
        Console.Write("Running SwiftRetFunc93: ");
        S93 val = SwiftRetFunc93();
        Assert.Equal((nint)unchecked((nint)5170226481546239050), val.F0);
        Assert.Equal((byte)11, val.F1);
        Assert.Equal((uint)1120259582, val.F2);
        Assert.Equal((uint)1947849905, val.F3);
        Assert.Equal((ulong)3690113387392112192, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct S94
    {
        public ushort F0;
        public double F1;
        public short F2;
        public double F3;
        public ulong F4;

        public S94(ushort f0, double f1, short f2, double f3, ulong f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func94AA3S94VyF")]
    private static extern S94 SwiftRetFunc94();

    [Fact]
    public static void TestSwiftRetFunc94()
    {
        Console.Write("Running SwiftRetFunc94: ");
        S94 val = SwiftRetFunc94();
        Assert.Equal((ushort)57111, val.F0);
        Assert.Equal((double)1718940123307098, val.F1);
        Assert.Equal((short)-16145, val.F2);
        Assert.Equal((double)1099321301986326, val.F3);
        Assert.Equal((ulong)2972912419231960385, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S95_S0
    {
        public double F0;

        public S95_S0(double f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct S95
    {
        public short F0;
        public S95_S0 F1;
        public ulong F2;

        public S95(short f0, S95_S0 f1, ulong f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func95AA3S95VyF")]
    private static extern S95 SwiftRetFunc95();

    [Fact]
    public static void TestSwiftRetFunc95()
    {
        Console.Write("Running SwiftRetFunc95: ");
        S95 val = SwiftRetFunc95();
        Assert.Equal((short)12620, val.F0);
        Assert.Equal((double)3232445258308074, val.F1.F0);
        Assert.Equal((ulong)97365157264460373, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    struct S96
    {
        public sbyte F0;
        public double F1;
        public ulong F2;
        public ulong F3;
        public int F4;
        public long F5;

        public S96(sbyte f0, double f1, ulong f2, ulong f3, int f4, long f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func96AA3S96VyF")]
    private static extern S96 SwiftRetFunc96();

    [Fact]
    public static void TestSwiftRetFunc96()
    {
        Console.Write("Running SwiftRetFunc96: ");
        S96 val = SwiftRetFunc96();
        Assert.Equal((sbyte)3, val.F0);
        Assert.Equal((double)242355060906873, val.F1);
        Assert.Equal((ulong)3087879465791321798, val.F2);
        Assert.Equal((ulong)7363229136420263380, val.F3);
        Assert.Equal((int)46853328, val.F4);
        Assert.Equal((long)4148307028758236491, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct S97
    {
        public ushort F0;
        public int F1;
        public ushort F2;
        public uint F3;

        public S97(ushort f0, int f1, ushort f2, uint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func97AA3S97VyF")]
    private static extern S97 SwiftRetFunc97();

    [Fact]
    public static void TestSwiftRetFunc97()
    {
        Console.Write("Running SwiftRetFunc97: ");
        S97 val = SwiftRetFunc97();
        Assert.Equal((ushort)10651, val.F0);
        Assert.Equal((int)2068379463, val.F1);
        Assert.Equal((ushort)57307, val.F2);
        Assert.Equal((uint)329271020, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    struct S98
    {
        public double F0;
        public int F1;
        public long F2;
        public nint F3;
        public float F4;
        public double F5;

        public S98(double f0, int f1, long f2, nint f3, float f4, double f5)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
            F5 = f5;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func98AA3S98VyF")]
    private static extern S98 SwiftRetFunc98();

    [Fact]
    public static void TestSwiftRetFunc98()
    {
        Console.Write("Running SwiftRetFunc98: ");
        S98 val = SwiftRetFunc98();
        Assert.Equal((double)2250389231883613, val.F0);
        Assert.Equal((int)1755058358, val.F1);
        Assert.Equal((long)6686142382639170849, val.F2);
        Assert.Equal((nint)unchecked((nint)6456632014163315773), val.F3);
        Assert.Equal((float)2818253, val.F4);
        Assert.Equal((double)1085859434505817, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct S99_S0
    {
        public int F0;

        public S99_S0(int f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S99
    {
        public S99_S0 F0;
        public float F1;

        public S99(S99_S0 f0, float f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB6Func99AA3S99VyF")]
    private static extern S99 SwiftRetFunc99();

    [Fact]
    public static void TestSwiftRetFunc99()
    {
        Console.Write("Running SwiftRetFunc99: ");
        S99 val = SwiftRetFunc99();
        Assert.Equal((int)1117297545, val.F0.F0);
        Assert.Equal((float)1539294, val.F1);
        Console.WriteLine("OK");
    }

}
