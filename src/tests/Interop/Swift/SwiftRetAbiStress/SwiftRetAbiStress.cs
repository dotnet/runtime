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

        public override string ToString()
        {
            return $"{{ F0 = {F0}, F1 = {F1}, F2 = {F2} }}";
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func0AA2S0VyF")]
    private static extern S0 SwiftRetFunc0();

    [Fact]
    public static void TestSwiftRetFunc0()
    {
        S0 val = SwiftRetFunc0();
        Assert.Equal(new S0(-17813, 318006528, 1195162122024233590), val);
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

        public override string ToString()
        {
            return $"{{ F0 = {F0}, F1 = {F1}, F2 = {F2}, F3 = {F3} }}";
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func1AA2S1VyF")]
    private static extern S1 SwiftRetFunc1();

    [Fact]
    public static void TestSwiftRetFunc1()
    {
        S1 val = SwiftRetFunc1();
        Assert.Equal(new S1(-29793, 7351779, 133491708229548754, 665726990), val);
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S2_S0
    {
        public ulong F0;

        public S2_S0(ulong f0)
        {
            F0 = f0;
        }

        public override string ToString()
        {
            return $"{{ F0 = {F0} }}";
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

        public override string ToString()
        {
            return $"{{ F0 = {F0}, F1 = {F1}, F2 = {F2}, F3 = {F3}, F4 = {F4} }}";
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func2AA2S2VyF")]
    private static extern S2 SwiftRetFunc2();

    [Fact]
    public static void TestSwiftRetFunc2()
    {
        S2 val = SwiftRetFunc2();
        Assert.Equal(new S2(new S2_S0(2153637757371267722), 150, 48920, 3564327, 1310569731), val);
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

        public override string ToString()
        {
            return $"{{ F0 = {F0}, F1 = {F1}, F2 = {F2}, F3 = {F3}, F4 = {F4}, F5 = {F5}, F6 = {F6} }}";
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func3AA2S3VyF")]
    private static extern S3 SwiftRetFunc3();

    [Fact]
    public static void TestSwiftRetFunc3()
    {
        S3 val = SwiftRetFunc3();
        Assert.Equal(new S3(5610153900386943274, 2431035148834736, 111, 772269424, 19240, 146, 821805530740405), val);
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

        public override string ToString()
        {
            return $"{{ F0 = {F0}, F1 = {F1}, F2 = {F2}, F3 = {F3} }}";
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func4AA2S4VyF")]
    private static extern S4 SwiftRetFunc4();

    [Fact]
    public static void TestSwiftRetFunc4()
    {
        S4 val = SwiftRetFunc4();
        Assert.Equal(new S4(125, 377073381, 964784376430620335, 5588038704850976624), val);
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

        public override string ToString()
        {
            return $"{{ F0 = {F0}, F1 = {F1} }}";
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

        public override string ToString()
        {
            return $"{{ F0 = {F0}, F1 = {F1}, F2 = {F2}, F3 = {F3}, F4 = {F4}, F5 = {F5} }}";
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func5AA2S5VyF")]
    private static extern S5 SwiftRetFunc5();

    [Fact]
    public static void TestSwiftRetFunc5()
    {
        S5 val = SwiftRetFunc5();
        Assert.Equal(new S5(5315019731968023493, 114, unchecked((nuint)1154655179105889397), new S5_S0(1468030771, 3066473182924818), unchecked((nint)6252650621827449809), 129), val);
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

        public override string ToString()
        {
            return $"{{ F0 = {F0}, F1 = {F1}, F2 = {F2}, F3 = {F3} }}";
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func6AA2S6VyF")]
    private static extern S6 SwiftRetFunc6();

    [Fact]
    public static void TestSwiftRetFunc6()
    {
        S6 val = SwiftRetFunc6();
        Assert.Equal(new S6(743741783, -6821, 5908745692727636656, 64295), val);
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S7_S0
    {
        public nint F0;

        public S7_S0(nint f0)
        {
            F0 = f0;
        }

        public override string ToString()
        {
            return $"{{ F0 = {F0} }}";
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

        public override string ToString()
        {
            return $"{{ F0 = {F0} }}";
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func7AA2S7VyF")]
    private static extern S7 SwiftRetFunc7();

    [Fact]
    public static void TestSwiftRetFunc7()
    {
        S7 val = SwiftRetFunc7();
        Assert.Equal(new S7(new S7_S0(unchecked((nint)7625368278886567558))), val);
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct S8
    {
        public nint F0;

        public S8(nint f0)
        {
            F0 = f0;
        }

        public override string ToString()
        {
            return $"{{ F0 = {F0} }}";
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func8AA2S8VyF")]
    private static extern S8 SwiftRetFunc8();

    [Fact]
    public static void TestSwiftRetFunc8()
    {
        S8 val = SwiftRetFunc8();
        Assert.Equal(new S8(unchecked((nint)775279004683334365)), val);
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

        public override string ToString()
        {
            return $"{{ F0 = {F0}, F1 = {F1} }}";
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

        public override string ToString()
        {
            return $"{{ F0 = {F0}, F1 = {F1}, F2 = {F2}, F3 = {F3} }}";
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s17SwiftRetAbiStress05swiftB5Func9AA2S9VyF")]
    private static extern S9 SwiftRetFunc9();

    [Fact]
    public static void TestSwiftRetFunc9()
    {
        S9 val = SwiftRetFunc9();
        Assert.Equal(new S9(1223030410, unchecked((nint)4720638462358523954), new S9_S0(30631, 1033774469), 64474), val);
    }

}
