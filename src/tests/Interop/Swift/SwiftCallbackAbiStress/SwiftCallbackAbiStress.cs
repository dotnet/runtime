// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS8500

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using Xunit;

public unsafe class SwiftCallbackAbiStress
{
    private const string SwiftLib = "libSwiftCallbackAbiStress.dylib";

    [StructLayout(LayoutKind.Sequential, Size = 14)]
    struct F0_S0
    {
        public double F0;
        public uint F1;
        public ushort F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F0_S1
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F0_S2
    {
        public float F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func01fs5Int32VAEs5Int16V_AEs6UInt64Vs6UInt16VAA5F0_S0VAA0K3_S1Vs5UInt8VAA0K3_S2VtXE_tF")]
    private static extern int SwiftCallbackFunc0(delegate* unmanaged[Swift]<short, int, ulong, ushort, F0_S0, F0_S1, byte, F0_S2, SwiftSelf, int> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static int SwiftCallbackFunc0Callback(short a0, int a1, ulong a2, ushort a3, F0_S0 a4, F0_S1 a5, byte a6, F0_S2 a7, SwiftSelf self)
    {
        try
        {
            Assert.Equal((short)-17813, a0);
            Assert.Equal((int)318006528, a1);
            Assert.Equal((ulong)1195162122024233590, a2);
            Assert.Equal((ushort)60467, a3);
            Assert.Equal((double)2239972725713766, a4.F0);
            Assert.Equal((uint)1404066621, a4.F1);
            Assert.Equal((ushort)29895, a4.F2);
            Assert.Equal((ulong)7923486769850554262, a5.F0);
            Assert.Equal((byte)217, a6);
            Assert.Equal((float)2497655, a7.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 1579768470;
    }

    [Fact]
    public static void TestSwiftCallbackFunc0()
    {
        Console.Write("Running SwiftCallbackFunc0: ");
        ExceptionDispatchInfo ex = null;
        int val = SwiftCallbackFunc0(&SwiftCallbackFunc0Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((int)1579768470, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 3)]
    struct F1_S0
    {
        public ushort F0;
        public byte F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F1_S1
    {
        public byte F0;
        public ulong F1;
        public short F2;
        public float F3;
        public float F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F1_S2_S0
    {
        public uint F0;
        public double F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct F1_S2
    {
        public sbyte F0;
        public nuint F1;
        public F1_S2_S0 F2;
        public nint F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F1_S3
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F1_S4
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F1_S5_S0
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F1_S5
    {
        public F1_S5_S0 F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func11fs5UInt8VAEs5Int64V_Sds4Int8VAA5F1_S0VAA0J3_S1VAA0J3_S2VAeigA0J3_S3VSuAA0J3_S4VAA0J3_S5VSitXE_tF")]
    private static extern byte SwiftCallbackFunc1(delegate* unmanaged[Swift]<long, double, sbyte, F1_S0, F1_S1, F1_S2, byte, sbyte, long, F1_S3, nuint, F1_S4, F1_S5, nint, SwiftSelf, byte> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static byte SwiftCallbackFunc1Callback(long a0, double a1, sbyte a2, F1_S0 a3, F1_S1 a4, F1_S2 a5, byte a6, sbyte a7, long a8, F1_S3 a9, nuint a10, F1_S4 a11, F1_S5 a12, nint a13, SwiftSelf self)
    {
        try
        {
            Assert.Equal((long)7920511243396412395, a0);
            Assert.Equal((double)1396130721334528, a1);
            Assert.Equal((sbyte)-55, a2);
            Assert.Equal((ushort)33758, a3.F0);
            Assert.Equal((byte)103, a3.F1);
            Assert.Equal((byte)201, a4.F0);
            Assert.Equal((ulong)7390774039746135757, a4.F1);
            Assert.Equal((short)14699, a4.F2);
            Assert.Equal((float)7235330, a4.F3);
            Assert.Equal((float)7189013, a4.F4);
            Assert.Equal((sbyte)37, a5.F0);
            Assert.Equal((nuint)unchecked((nuint)3310322731568932038), a5.F1);
            Assert.Equal((uint)1100328218, a5.F2.F0);
            Assert.Equal((double)1060779460203640, a5.F2.F1);
            Assert.Equal((nint)unchecked((nint)8325292022909418877), a5.F3);
            Assert.Equal((byte)137, a6);
            Assert.Equal((sbyte)82, a7);
            Assert.Equal((long)1197537325837505041, a8);
            Assert.Equal((ushort)46950, a9.F0);
            Assert.Equal((nuint)unchecked((nuint)8181828233622947597), a10);
            Assert.Equal((nint)unchecked((nint)1851182205030289056), a11.F0);
            Assert.Equal((uint)1971014225, a12.F0.F0);
            Assert.Equal((nint)unchecked((nint)6437995407675718392), a13);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 248;
    }

    [Fact]
    public static void TestSwiftCallbackFunc1()
    {
        Console.Write("Running SwiftCallbackFunc1: ");
        ExceptionDispatchInfo ex = null;
        byte val = SwiftCallbackFunc1(&SwiftCallbackFunc1Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((byte)248, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F2_S0
    {
        public int F0;
        public nuint F1;
        public float F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F2_S1_S0
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F2_S1
    {
        public long F0;
        public ushort F1;
        public F2_S1_S0 F2;
        public nint F3;
        public double F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 11)]
    struct F2_S2
    {
        public float F0;
        public int F1;
        public ushort F2;
        public sbyte F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F2_S3_S0
    {
        public sbyte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F2_S3
    {
        public F2_S3_S0 F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func21fs4Int8VAeA5F2_S0V_AA0H3_S1VAA0H3_S2VSfs6UInt64VAA0H3_S3VtXE_tF")]
    private static extern sbyte SwiftCallbackFunc2(delegate* unmanaged[Swift]<F2_S0, F2_S1, F2_S2, float, ulong, F2_S3, SwiftSelf, sbyte> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static sbyte SwiftCallbackFunc2Callback(F2_S0 a0, F2_S1 a1, F2_S2 a2, float a3, ulong a4, F2_S3 a5, SwiftSelf self)
    {
        try
        {
            Assert.Equal((int)1860840185, a0.F0);
            Assert.Equal((nuint)unchecked((nuint)5407074783834178811), a0.F1);
            Assert.Equal((float)6261766, a0.F2);
            Assert.Equal((long)4033972792915237065, a1.F0);
            Assert.Equal((ushort)22825, a1.F1);
            Assert.Equal((ushort)44574, a1.F2.F0);
            Assert.Equal((nint)unchecked((nint)4536911485304731630), a1.F3);
            Assert.Equal((double)4282944015147385, a1.F4);
            Assert.Equal((float)2579193, a2.F0);
            Assert.Equal((int)586252933, a2.F1);
            Assert.Equal((ushort)47002, a2.F2);
            Assert.Equal((sbyte)71, a2.F3);
            Assert.Equal((float)3225929, a3);
            Assert.Equal((ulong)3599444831393612282, a4);
            Assert.Equal((sbyte)13, a5.F0.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 115;
    }

    [Fact]
    public static void TestSwiftCallbackFunc2()
    {
        Console.Write("Running SwiftCallbackFunc2: ");
        ExceptionDispatchInfo ex = null;
        sbyte val = SwiftCallbackFunc2(&SwiftCallbackFunc2Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((sbyte)115, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F3_S0_S0
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F3_S0
    {
        public F3_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F3_S1
    {
        public uint F0;
        public long F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 3)]
    struct F3_S2_S0
    {
        public short F0;
        public byte F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 5)]
    struct F3_S2
    {
        public F3_S2_S0 F0;
        public sbyte F1;
        public byte F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F3_S3
    {
        public ulong F0;
        public long F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F3_S4
    {
        public short F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F3_Ret
    {
        public ushort F0;
        public byte F1;
        public ushort F2;
        public float F3;

        public F3_Ret(ushort f0, byte f1, ushort f2, float f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func31fAA6F3_RetVAeA0G3_S0V_Sfs6UInt16VAA0G3_S1VAIs5Int32VAA0G3_S2VSiAA0G3_S3VAA0G3_S4VtXE_tF")]
    private static extern F3_Ret SwiftCallbackFunc3(delegate* unmanaged[Swift]<F3_S0, float, ushort, F3_S1, ushort, int, F3_S2, nint, F3_S3, F3_S4, SwiftSelf, F3_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F3_Ret SwiftCallbackFunc3Callback(F3_S0 a0, float a1, ushort a2, F3_S1 a3, ushort a4, int a5, F3_S2 a6, nint a7, F3_S3 a8, F3_S4 a9, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nuint)unchecked((nuint)5610153900386943274), a0.F0.F0);
            Assert.Equal((float)7736836, a1);
            Assert.Equal((ushort)31355, a2);
            Assert.Equal((uint)1159208572, a3.F0);
            Assert.Equal((long)2707818827451590538, a3.F1);
            Assert.Equal((ushort)37580, a4);
            Assert.Equal((int)1453603418, a5);
            Assert.Equal((short)699, a6.F0.F0);
            Assert.Equal((byte)46, a6.F0.F1);
            Assert.Equal((sbyte)-125, a6.F1);
            Assert.Equal((byte)92, a6.F2);
            Assert.Equal((nint)unchecked((nint)94557706586779834), a7);
            Assert.Equal((ulong)2368015527878194540, a8.F0);
            Assert.Equal((long)5026404532195049271, a8.F1);
            Assert.Equal((short)21807, a9.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F3_Ret(51293, 217, 64666, 5667425);
    }

    [Fact]
    public static void TestSwiftCallbackFunc3()
    {
        Console.Write("Running SwiftCallbackFunc3: ");
        ExceptionDispatchInfo ex = null;
        F3_Ret val = SwiftCallbackFunc3(&SwiftCallbackFunc3Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)51293, val.F0);
        Assert.Equal((byte)217, val.F1);
        Assert.Equal((ushort)64666, val.F2);
        Assert.Equal((float)5667425, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F4_S0_S0
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F4_S0
    {
        public F4_S0_S0 F0;
        public float F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F4_Ret_S0
    {
        public nint F0;

        public F4_Ret_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 44)]
    struct F4_Ret
    {
        public int F0;
        public F4_Ret_S0 F1;
        public nint F2;
        public short F3;
        public nint F4;
        public uint F5;

        public F4_Ret(int f0, F4_Ret_S0 f1, nint f2, short f3, nint f4, uint f5)
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
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func41fAA6F4_RetVAESd_AA0G3_S0Vs5UInt8Vs5Int32Vs6UInt32VtXE_tF")]
    private static extern F4_Ret SwiftCallbackFunc4(delegate* unmanaged[Swift]<double, F4_S0, byte, int, uint, SwiftSelf, F4_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F4_Ret SwiftCallbackFunc4Callback(double a0, F4_S0 a1, byte a2, int a3, uint a4, SwiftSelf self)
    {
        try
        {
            Assert.Equal((double)4282972206489588, a0);
            Assert.Equal((uint)611688063, a1.F0.F0);
            Assert.Equal((float)877466, a1.F1);
            Assert.Equal((byte)53, a2);
            Assert.Equal((int)965123506, a3);
            Assert.Equal((uint)1301067653, a4);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F4_Ret(2069454428, new F4_Ret_S0(unchecked((nint)5483154806067048127)), unchecked((nint)2342208892279753870), -21578, unchecked((nint)4641984012938514811), 1691113876);
    }

    [Fact]
    public static void TestSwiftCallbackFunc4()
    {
        Console.Write("Running SwiftCallbackFunc4: ");
        ExceptionDispatchInfo ex = null;
        F4_Ret val = SwiftCallbackFunc4(&SwiftCallbackFunc4Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((int)2069454428, val.F0);
        Assert.Equal((nint)unchecked((nint)5483154806067048127), val.F1.F0);
        Assert.Equal((nint)unchecked((nint)2342208892279753870), val.F2);
        Assert.Equal((short)-21578, val.F3);
        Assert.Equal((nint)unchecked((nint)4641984012938514811), val.F4);
        Assert.Equal((uint)1691113876, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F5_S0
    {
        public nuint F0;
        public uint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F5_S1_S0
    {
        public nint F0;
        public uint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F5_S1_S1
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F5_S1
    {
        public F5_S1_S0 F0;
        public F5_S1_S1 F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F5_S2
    {
        public double F0;
        public sbyte F1;
        public nint F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F5_S3
    {
        public long F0;
        public double F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F5_S4
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 26)]
    struct F5_Ret
    {
        public short F0;
        public int F1;
        public int F2;
        public ulong F3;
        public short F4;

        public F5_Ret(short f0, int f1, int f2, ulong f3, short f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func51fAA6F5_RetVAEs5UInt8V_s5Int16Vs6UInt64VS2uAkgA0G3_S0Vs4Int8VAoA0G3_S1VAA0G3_S2VAA0G3_S3VSdAA0G3_S4Vs6UInt16VS2fAYtXE_tF")]
    private static extern F5_Ret SwiftCallbackFunc5(delegate* unmanaged[Swift]<byte, short, ulong, nuint, nuint, ulong, byte, F5_S0, sbyte, sbyte, F5_S1, F5_S2, F5_S3, double, F5_S4, ushort, float, float, ushort, SwiftSelf, F5_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F5_Ret SwiftCallbackFunc5Callback(byte a0, short a1, ulong a2, nuint a3, nuint a4, ulong a5, byte a6, F5_S0 a7, sbyte a8, sbyte a9, F5_S1 a10, F5_S2 a11, F5_S3 a12, double a13, F5_S4 a14, ushort a15, float a16, float a17, ushort a18, SwiftSelf self)
    {
        try
        {
            Assert.Equal((byte)42, a0);
            Assert.Equal((short)18727, a1);
            Assert.Equal((ulong)3436765034579128495, a2);
            Assert.Equal((nuint)unchecked((nuint)6305137336506323506), a3);
            Assert.Equal((nuint)unchecked((nuint)6280137078630028944), a4);
            Assert.Equal((ulong)6252650621827449809, a5);
            Assert.Equal((byte)129, a6);
            Assert.Equal((nuint)unchecked((nuint)6879980973426111678), a7.F0);
            Assert.Equal((uint)1952654577, a7.F1);
            Assert.Equal((sbyte)-34, a8);
            Assert.Equal((sbyte)102, a9);
            Assert.Equal((nint)unchecked((nint)8389143657021522019), a10.F0.F0);
            Assert.Equal((uint)437030241, a10.F0.F1);
            Assert.Equal((float)7522798, a10.F1.F0);
            Assert.Equal((double)523364011167530, a11.F0);
            Assert.Equal((sbyte)16, a11.F1);
            Assert.Equal((nint)unchecked((nint)3823439046574037759), a11.F2);
            Assert.Equal((long)3767260839267771462, a12.F0);
            Assert.Equal((double)1181031208183008, a12.F1);
            Assert.Equal((double)2338830539621828, a13);
            Assert.Equal((ushort)36276, a14.F0);
            Assert.Equal((ushort)41286, a15);
            Assert.Equal((float)6683955, a16);
            Assert.Equal((float)6399917, a17);
            Assert.Equal((ushort)767, a18);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F5_Ret(-23277, 1015782032, 83490460, 2747931081050267058, -10369);
    }

    [Fact]
    public static void TestSwiftCallbackFunc5()
    {
        Console.Write("Running SwiftCallbackFunc5: ");
        ExceptionDispatchInfo ex = null;
        F5_Ret val = SwiftCallbackFunc5(&SwiftCallbackFunc5Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((short)-23277, val.F0);
        Assert.Equal((int)1015782032, val.F1);
        Assert.Equal((int)83490460, val.F2);
        Assert.Equal((ulong)2747931081050267058, val.F3);
        Assert.Equal((short)-10369, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F6_S0_S0
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F6_S0
    {
        public sbyte F0;
        public sbyte F1;
        public int F2;
        public F6_S0_S0 F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F6_S1
    {
        public int F0;
        public ulong F1;
        public ulong F2;
        public uint F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 11)]
    struct F6_S2
    {
        public long F0;
        public short F1;
        public sbyte F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F6_S3
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F6_Ret_S0
    {
        public long F0;
        public uint F1;

        public F6_Ret_S0(long f0, uint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 29)]
    struct F6_Ret
    {
        public F6_Ret_S0 F0;
        public ulong F1;
        public float F2;
        public sbyte F3;

        public F6_Ret(F6_Ret_S0 f0, ulong f1, float f2, sbyte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func61fAA6F6_RetVAESf_AA0G3_S0Vs5Int64Vs4Int8Vs6UInt16VSuAMs6UInt64VAA0G3_S1Vs5Int16VAA0G3_S2VAA0G3_S3VAMtXE_tF")]
    private static extern F6_Ret SwiftCallbackFunc6(delegate* unmanaged[Swift]<float, F6_S0, long, sbyte, ushort, nuint, ushort, ulong, F6_S1, short, F6_S2, F6_S3, ushort, SwiftSelf, F6_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F6_Ret SwiftCallbackFunc6Callback(float a0, F6_S0 a1, long a2, sbyte a3, ushort a4, nuint a5, ushort a6, ulong a7, F6_S1 a8, short a9, F6_S2 a10, F6_S3 a11, ushort a12, SwiftSelf self)
    {
        try
        {
            Assert.Equal((float)2905241, a0);
            Assert.Equal((sbyte)-27, a1.F0);
            Assert.Equal((sbyte)-77, a1.F1);
            Assert.Equal((int)1315779092, a1.F2);
            Assert.Equal((float)5373970, a1.F3.F0);
            Assert.Equal((long)7022244764256789748, a2);
            Assert.Equal((sbyte)-110, a3);
            Assert.Equal((ushort)2074, a4);
            Assert.Equal((nuint)unchecked((nuint)3560129042279209151), a5);
            Assert.Equal((ushort)2200, a6);
            Assert.Equal((ulong)5730241035812482149, a7);
            Assert.Equal((int)18625011, a8.F0);
            Assert.Equal((ulong)242340713355417257, a8.F1);
            Assert.Equal((ulong)6962175160124965670, a8.F2);
            Assert.Equal((uint)1983617839, a8.F3);
            Assert.Equal((short)-28374, a9);
            Assert.Equal((long)6355748563312062178, a10.F0);
            Assert.Equal((short)-23189, a10.F1);
            Assert.Equal((sbyte)81, a10.F2);
            Assert.Equal((float)4547677, a11.F0);
            Assert.Equal((ushort)6397, a12);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F6_Ret(new F6_Ret_S0(3036123356548380503, 653452587), 4787954187933165977, 5060002, -68);
    }

    [Fact]
    public static void TestSwiftCallbackFunc6()
    {
        Console.Write("Running SwiftCallbackFunc6: ");
        ExceptionDispatchInfo ex = null;
        F6_Ret val = SwiftCallbackFunc6(&SwiftCallbackFunc6Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((long)3036123356548380503, val.F0.F0);
        Assert.Equal((uint)653452587, val.F0.F1);
        Assert.Equal((ulong)4787954187933165977, val.F1);
        Assert.Equal((float)5060002, val.F2);
        Assert.Equal((sbyte)-68, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F7_S0
    {
        public float F0;
        public long F1;
        public nuint F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F7_S1
    {
        public short F0;
        public uint F1;
        public uint F2;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func71fs6UInt16VAEs5Int64V_s5UInt8VSdAeA5F7_S0VAISds6UInt32VAA0J3_S1Vs5Int32VAQSis5Int16VAESis6UInt64VAiStXE_tF")]
    private static extern ushort SwiftCallbackFunc7(delegate* unmanaged[Swift]<long, byte, double, ushort, F7_S0, byte, double, uint, F7_S1, int, int, nint, short, ushort, nint, ulong, byte, short, SwiftSelf, ushort> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static ushort SwiftCallbackFunc7Callback(long a0, byte a1, double a2, ushort a3, F7_S0 a4, byte a5, double a6, uint a7, F7_S1 a8, int a9, int a10, nint a11, short a12, ushort a13, nint a14, ulong a15, byte a16, short a17, SwiftSelf self)
    {
        try
        {
            Assert.Equal((long)7625368278886567558, a0);
            Assert.Equal((byte)70, a1);
            Assert.Equal((double)2146971972122530, a2);
            Assert.Equal((ushort)54991, a3);
            Assert.Equal((float)1072132, a4.F0);
            Assert.Equal((long)3890459003549150599, a4.F1);
            Assert.Equal((nuint)unchecked((nuint)56791000421908673), a4.F2);
            Assert.Equal((byte)227, a5);
            Assert.Equal((double)3248250571953113, a6);
            Assert.Equal((uint)1138780108, a7);
            Assert.Equal((short)-22670, a8.F0);
            Assert.Equal((uint)1796712687, a8.F1);
            Assert.Equal((uint)304251857, a8.F2);
            Assert.Equal((int)1288765591, a9);
            Assert.Equal((int)1382721790, a10);
            Assert.Equal((nint)unchecked((nint)6746417265635727373), a11);
            Assert.Equal((short)-15600, a12);
            Assert.Equal((ushort)47575, a13);
            Assert.Equal((nint)unchecked((nint)7200793040165597188), a14);
            Assert.Equal((ulong)2304985873826892392, a15);
            Assert.Equal((byte)99, a16);
            Assert.Equal((short)-9993, a17);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 31412;
    }

    [Fact]
    public static void TestSwiftCallbackFunc7()
    {
        Console.Write("Running SwiftCallbackFunc7: ");
        ExceptionDispatchInfo ex = null;
        ushort val = SwiftCallbackFunc7(&SwiftCallbackFunc7Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)31412, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F8_S0
    {
        public short F0;
        public short F1;
        public nuint F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F8_S1
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F8_Ret_S0
    {
        public int F0;
        public nuint F1;
        public nint F2;

        public F8_Ret_S0(int f0, nuint f1, nint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 44)]
    struct F8_Ret
    {
        public long F0;
        public F8_Ret_S0 F1;
        public nint F2;
        public uint F3;

        public F8_Ret(long f0, F8_Ret_S0 f1, nint f2, uint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func81fAA6F8_RetVAeA0G3_S0V_AA0G3_S1VtXE_tF")]
    private static extern F8_Ret SwiftCallbackFunc8(delegate* unmanaged[Swift]<F8_S0, F8_S1, SwiftSelf, F8_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F8_Ret SwiftCallbackFunc8Callback(F8_S0 a0, F8_S1 a1, SwiftSelf self)
    {
        try
        {
            Assert.Equal((short)16278, a0.F0);
            Assert.Equal((short)-31563, a0.F1);
            Assert.Equal((nuint)unchecked((nuint)2171308312325435543), a0.F2);
            Assert.Equal((long)8923668560896309835, a1.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F8_Ret(4170441467272673523, new F8_Ret_S0(1940721160, unchecked((nuint)6524670832376567295), unchecked((nint)4210781401091965722)), unchecked((nint)3245727696885859461), 855061841);
    }

    [Fact]
    public static void TestSwiftCallbackFunc8()
    {
        Console.Write("Running SwiftCallbackFunc8: ");
        ExceptionDispatchInfo ex = null;
        F8_Ret val = SwiftCallbackFunc8(&SwiftCallbackFunc8Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((long)4170441467272673523, val.F0);
        Assert.Equal((int)1940721160, val.F1.F0);
        Assert.Equal((nuint)unchecked((nuint)6524670832376567295), val.F1.F1);
        Assert.Equal((nint)unchecked((nint)4210781401091965722), val.F1.F2);
        Assert.Equal((nint)unchecked((nint)3245727696885859461), val.F2);
        Assert.Equal((uint)855061841, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F9_S0_S0
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F9_S0
    {
        public F9_S0_S0 F0;
        public short F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F9_S1_S0
    {
        public long F0;
        public long F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F9_S1
    {
        public nint F0;
        public F9_S1_S0 F1;
        public float F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 19)]
    struct F9_S2
    {
        public ulong F0;
        public double F1;
        public short F2;
        public sbyte F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F9_S3_S0_S0
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F9_S3_S0
    {
        public F9_S3_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F9_S3
    {
        public sbyte F0;
        public F9_S3_S0 F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F9_S4_S0
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct F9_S4
    {
        public F9_S4_S0 F0;
        public sbyte F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F9_S5_S0
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F9_S5
    {
        public uint F0;
        public F9_S5_S0 F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F9_S6
    {
        public double F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func91fs6UInt16VAEs4Int8V_s5UInt8Vs5Int64VAA5F9_S0VAA0K3_S1VAA0K3_S2VSdAA0K3_S3VAA0K3_S4VSdAA0K3_S5VAA0K3_S6VtXE_tF")]
    private static extern ushort SwiftCallbackFunc9(delegate* unmanaged[Swift]<sbyte, byte, long, F9_S0, F9_S1, F9_S2, double, F9_S3, F9_S4, double, F9_S5, F9_S6, SwiftSelf, ushort> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static ushort SwiftCallbackFunc9Callback(sbyte a0, byte a1, long a2, F9_S0 a3, F9_S1 a4, F9_S2 a5, double a6, F9_S3 a7, F9_S4 a8, double a9, F9_S5 a10, F9_S6 a11, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)17, a0);
            Assert.Equal((byte)104, a1);
            Assert.Equal((long)8922699691031703191, a2);
            Assert.Equal((byte)123, a3.F0.F0);
            Assert.Equal((short)31706, a3.F1);
            Assert.Equal((nint)unchecked((nint)1804058604961822948), a4.F0);
            Assert.Equal((long)8772179036715198777, a4.F1.F0);
            Assert.Equal((long)3320511540592563328, a4.F1.F1);
            Assert.Equal((float)679540, a4.F2);
            Assert.Equal((ulong)8642590829466497926, a5.F0);
            Assert.Equal((double)4116322155252965, a5.F1);
            Assert.Equal((short)17992, a5.F2);
            Assert.Equal((sbyte)-48, a5.F3);
            Assert.Equal((double)414017537937894, a6);
            Assert.Equal((sbyte)47, a7.F0);
            Assert.Equal((ulong)7576380984563129085, a7.F1.F0.F0);
            Assert.Equal((ulong)1356827400304742803, a8.F0.F0);
            Assert.Equal((sbyte)-17, a8.F1);
            Assert.Equal((double)4458031413035521, a9);
            Assert.Equal((uint)352075098, a10.F0);
            Assert.Equal((uint)1840980094, a10.F1.F0);
            Assert.Equal((double)396957263013930, a11.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 5567;
    }

    [Fact]
    public static void TestSwiftCallbackFunc9()
    {
        Console.Write("Running SwiftCallbackFunc9: ");
        ExceptionDispatchInfo ex = null;
        ushort val = SwiftCallbackFunc9(&SwiftCallbackFunc9Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)5567, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F10_Ret
    {
        public long F0;
        public uint F1;
        public ushort F2;
        public uint F3;

        public F10_Ret(long f0, uint f1, ushort f2, uint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func101fAA7F10_RetVAEs5Int16VXE_tF")]
    private static extern F10_Ret SwiftCallbackFunc10(delegate* unmanaged[Swift]<short, SwiftSelf, F10_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F10_Ret SwiftCallbackFunc10Callback(short a0, SwiftSelf self)
    {
        try
        {
            Assert.Equal((short)-7168, a0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F10_Ret(7820305774933543349, 1501926289, 39078, 661487951);
    }

    [Fact]
    public static void TestSwiftCallbackFunc10()
    {
        Console.Write("Running SwiftCallbackFunc10: ");
        ExceptionDispatchInfo ex = null;
        F10_Ret val = SwiftCallbackFunc10(&SwiftCallbackFunc10Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((long)7820305774933543349, val.F0);
        Assert.Equal((uint)1501926289, val.F1);
        Assert.Equal((ushort)39078, val.F2);
        Assert.Equal((uint)661487951, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F11_S0_S0
    {
        public sbyte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F11_S0
    {
        public uint F0;
        public F11_S0_S0 F1;
        public nuint F2;
        public int F3;
        public long F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F11_S1_S0
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F11_S1
    {
        public F11_S1_S0 F0;
        public short F1;
        public uint F2;
        public short F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F11_S2
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F11_Ret
    {
        public short F0;
        public short F1;
        public byte F2;
        public long F3;

        public F11_Ret(short f0, short f1, byte f2, long f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func111fAA7F11_RetVAEs6UInt32V_Sus6UInt64Vs5Int16VAA0G3_S0VSfs4Int8Vs6UInt16VAA0G3_S1VAGs5Int64VAgA0G3_S2VtXE_tF")]
    private static extern F11_Ret SwiftCallbackFunc11(delegate* unmanaged[Swift]<uint, nuint, ulong, short, F11_S0, float, sbyte, ushort, F11_S1, uint, long, uint, F11_S2, SwiftSelf, F11_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F11_Ret SwiftCallbackFunc11Callback(uint a0, nuint a1, ulong a2, short a3, F11_S0 a4, float a5, sbyte a6, ushort a7, F11_S1 a8, uint a9, long a10, uint a11, F11_S2 a12, SwiftSelf self)
    {
        try
        {
            Assert.Equal((uint)454751144, a0);
            Assert.Equal((nuint)unchecked((nuint)1696592254558667577), a1);
            Assert.Equal((ulong)5831587230944972245, a2);
            Assert.Equal((short)15352, a3);
            Assert.Equal((uint)1306601347, a4.F0);
            Assert.Equal((sbyte)123, a4.F1.F0);
            Assert.Equal((nuint)unchecked((nuint)3064471520018434938), a4.F2);
            Assert.Equal((int)272956246, a4.F3);
            Assert.Equal((long)3683518307106722029, a4.F4);
            Assert.Equal((float)5606122, a5);
            Assert.Equal((sbyte)-126, a6);
            Assert.Equal((ushort)50801, a7);
            Assert.Equal((ushort)63467, a8.F0.F0);
            Assert.Equal((short)-31828, a8.F1);
            Assert.Equal((uint)2117176776, a8.F2);
            Assert.Equal((short)-27265, a8.F3);
            Assert.Equal((uint)1879606687, a9);
            Assert.Equal((long)4981244336430926707, a10);
            Assert.Equal((uint)1159924856, a11);
            Assert.Equal((byte)29, a12.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F11_Ret(7934, -24509, 20, 5470383170748296608);
    }

    [Fact]
    public static void TestSwiftCallbackFunc11()
    {
        Console.Write("Running SwiftCallbackFunc11: ");
        ExceptionDispatchInfo ex = null;
        F11_Ret val = SwiftCallbackFunc11(&SwiftCallbackFunc11Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((short)7934, val.F0);
        Assert.Equal((short)-24509, val.F1);
        Assert.Equal((byte)20, val.F2);
        Assert.Equal((long)5470383170748296608, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct F12_S0
    {
        public ulong F0;
        public sbyte F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F12_S1_S0_S0
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F12_S1_S0
    {
        public F12_S1_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F12_S1
    {
        public ushort F0;
        public uint F1;
        public F12_S1_S0 F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F12_Ret
    {
        public ulong F0;
        public nint F1;

        public F12_Ret(ulong f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func121fAA7F12_RetVAeA0G3_S0V_s5Int16Vs6UInt64VAA0G3_S1Vs4Int8VtXE_tF")]
    private static extern F12_Ret SwiftCallbackFunc12(delegate* unmanaged[Swift]<F12_S0, short, ulong, F12_S1, sbyte, SwiftSelf, F12_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F12_Ret SwiftCallbackFunc12Callback(F12_S0 a0, short a1, ulong a2, F12_S1 a3, sbyte a4, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ulong)3236871137735400659, a0.F0);
            Assert.Equal((sbyte)-123, a0.F1);
            Assert.Equal((short)-22828, a1);
            Assert.Equal((ulong)2132557792366642035, a2);
            Assert.Equal((ushort)42520, a3.F0);
            Assert.Equal((uint)879349060, a3.F1);
            Assert.Equal((ulong)5694370973277919380, a3.F2.F0.F0);
            Assert.Equal((sbyte)-75, a4);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F12_Ret(4675419585914412295, unchecked((nint)1931022181202552704));
    }

    [Fact]
    public static void TestSwiftCallbackFunc12()
    {
        Console.Write("Running SwiftCallbackFunc12: ");
        ExceptionDispatchInfo ex = null;
        F12_Ret val = SwiftCallbackFunc12(&SwiftCallbackFunc12Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)4675419585914412295, val.F0);
        Assert.Equal((nint)unchecked((nint)1931022181202552704), val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F13_S0_S0
    {
        public long F0;
        public long F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 22)]
    struct F13_S0
    {
        public F13_S0_S0 F0;
        public float F1;
        public short F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F13_S1
    {
        public nint F0;
        public ulong F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F13_S2_S0
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F13_S2
    {
        public F13_S2_S0 F0;
        public double F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 5)]
    struct F13_S3
    {
        public float F0;
        public sbyte F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F13_S4
    {
        public nint F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func131fS2dAA6F13_S0V_s5Int32VSis6UInt16VSuAA0G3_S1VAA0G3_S2VSiSds4Int8VSfSiAA0G3_S3VSuAA0G3_S4VtXE_tF")]
    private static extern double SwiftCallbackFunc13(delegate* unmanaged[Swift]<F13_S0, int, nint, ushort, nuint, F13_S1, F13_S2, nint, double, sbyte, float, nint, F13_S3, nuint, F13_S4, SwiftSelf, double> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static double SwiftCallbackFunc13Callback(F13_S0 a0, int a1, nint a2, ushort a3, nuint a4, F13_S1 a5, F13_S2 a6, nint a7, double a8, sbyte a9, float a10, nint a11, F13_S3 a12, nuint a13, F13_S4 a14, SwiftSelf self)
    {
        try
        {
            Assert.Equal((long)9003727031576598067, a0.F0.F0);
            Assert.Equal((long)8527798284445940986, a0.F0.F1);
            Assert.Equal((float)3585628, a0.F1);
            Assert.Equal((short)-12520, a0.F2);
            Assert.Equal((int)1510815104, a1);
            Assert.Equal((nint)unchecked((nint)5883331525294982326), a2);
            Assert.Equal((ushort)60738, a3);
            Assert.Equal((nuint)unchecked((nuint)5291799143932627546), a4);
            Assert.Equal((nint)unchecked((nint)1949276559361384602), a5.F0);
            Assert.Equal((ulong)876048527237138968, a5.F1);
            Assert.Equal((byte)67, a6.F0.F0);
            Assert.Equal((double)2455575228564859, a6.F1);
            Assert.Equal((nint)unchecked((nint)2321408806345977320), a7);
            Assert.Equal((double)12750323283778, a8);
            Assert.Equal((sbyte)46, a9);
            Assert.Equal((float)6774339, a10);
            Assert.Equal((nint)unchecked((nint)5121910967292140178), a11);
            Assert.Equal((float)8254279, a12.F0);
            Assert.Equal((sbyte)-7, a12.F1);
            Assert.Equal((nuint)unchecked((nuint)7533347207018595125), a13);
            Assert.Equal((nint)unchecked((nint)6605448167191082938), a14.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 2798050901932855;
    }

    [Fact]
    public static void TestSwiftCallbackFunc13()
    {
        Console.Write("Running SwiftCallbackFunc13: ");
        ExceptionDispatchInfo ex = null;
        double val = SwiftCallbackFunc13(&SwiftCallbackFunc13Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((double)2798050901932855, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F14_S0
    {
        public sbyte F0;
        public float F1;
        public ushort F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F14_S1
    {
        public ulong F0;
        public ulong F1;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func141fs5Int64VA2E_AA6F14_S0Vs4Int8Vs6UInt64VAA0H3_S1VSitXE_tF")]
    private static extern long SwiftCallbackFunc14(delegate* unmanaged[Swift]<long, F14_S0, sbyte, ulong, F14_S1, nint, SwiftSelf, long> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static long SwiftCallbackFunc14Callback(long a0, F14_S0 a1, sbyte a2, ulong a3, F14_S1 a4, nint a5, SwiftSelf self)
    {
        try
        {
            Assert.Equal((long)5547219684656041875, a0);
            Assert.Equal((sbyte)-39, a1.F0);
            Assert.Equal((float)5768837, a1.F1);
            Assert.Equal((ushort)53063, a1.F2);
            Assert.Equal((sbyte)-102, a2);
            Assert.Equal((ulong)5745438709817040873, a3);
            Assert.Equal((ulong)2178706453119907411, a4.F0);
            Assert.Equal((ulong)4424726479787355131, a4.F1);
            Assert.Equal((nint)unchecked((nint)5693881223150438553), a5);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 5130561516716417305;
    }

    [Fact]
    public static void TestSwiftCallbackFunc14()
    {
        Console.Write("Running SwiftCallbackFunc14: ");
        ExceptionDispatchInfo ex = null;
        long val = SwiftCallbackFunc14(&SwiftCallbackFunc14Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((long)5130561516716417305, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F15_S0
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F15_S1
    {
        public nint F0;
        public uint F1;
        public byte F2;
        public short F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 25)]
    struct F15_S2
    {
        public sbyte F0;
        public ulong F1;
        public long F2;
        public byte F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F15_S3
    {
        public double F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func151fS2is5UInt8V_s6UInt16Vs6UInt64VAIs4Int8VSuSdSfSiAA6F15_S0VAA0K3_S1VAgA0K3_S2VAeA0K3_S3VtXE_tF")]
    private static extern nint SwiftCallbackFunc15(delegate* unmanaged[Swift]<byte, ushort, ulong, ulong, sbyte, nuint, double, float, nint, F15_S0, F15_S1, ushort, F15_S2, byte, F15_S3, SwiftSelf, nint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static nint SwiftCallbackFunc15Callback(byte a0, ushort a1, ulong a2, ulong a3, sbyte a4, nuint a5, double a6, float a7, nint a8, F15_S0 a9, F15_S1 a10, ushort a11, F15_S2 a12, byte a13, F15_S3 a14, SwiftSelf self)
    {
        try
        {
            Assert.Equal((byte)0, a0);
            Assert.Equal((ushort)31081, a1);
            Assert.Equal((ulong)8814881608835743979, a2);
            Assert.Equal((ulong)4283853687332682681, a3);
            Assert.Equal((sbyte)80, a4);
            Assert.Equal((nuint)unchecked((nuint)7895994601265649979), a5);
            Assert.Equal((double)1855521542692398, a6);
            Assert.Equal((float)3235683, a7);
            Assert.Equal((nint)unchecked((nint)215122646177738904), a8);
            Assert.Equal((uint)2044750195, a9.F0);
            Assert.Equal((nint)unchecked((nint)1772412898183620625), a10.F0);
            Assert.Equal((uint)131256973, a10.F1);
            Assert.Equal((byte)153, a10.F2);
            Assert.Equal((short)25281, a10.F3);
            Assert.Equal((ushort)50965, a11);
            Assert.Equal((sbyte)-83, a12.F0);
            Assert.Equal((ulong)7751486385861474282, a12.F1);
            Assert.Equal((long)3744400479301818340, a12.F2);
            Assert.Equal((byte)150, a12.F3);
            Assert.Equal((byte)179, a13);
            Assert.Equal((double)3108143600787174, a14.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return unchecked((nint)2326283264176371053);
    }

    [Fact]
    public static void TestSwiftCallbackFunc15()
    {
        Console.Write("Running SwiftCallbackFunc15: ");
        ExceptionDispatchInfo ex = null;
        nint val = SwiftCallbackFunc15(&SwiftCallbackFunc15Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)2326283264176371053), val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F16_S0
    {
        public sbyte F0;
        public int F1;
        public ushort F2;
        public ushort F3;
        public uint F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F16_S1
    {
        public ushort F0;
        public sbyte F1;
        public byte F2;
        public nint F3;
        public nint F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F16_S2_S0
    {
        public sbyte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 14)]
    struct F16_S2
    {
        public int F0;
        public int F1;
        public uint F2;
        public byte F3;
        public F16_S2_S0 F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F16_S3
    {
        public short F0;
        public double F1;
        public double F2;
        public int F3;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func161fs4Int8VAeA6F16_S0V_s5Int16VSfAA0H3_S1VAA0H3_S2Vs6UInt64VAA0H3_S3VSutXE_tF")]
    private static extern sbyte SwiftCallbackFunc16(delegate* unmanaged[Swift]<F16_S0, short, float, F16_S1, F16_S2, ulong, F16_S3, nuint, SwiftSelf, sbyte> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static sbyte SwiftCallbackFunc16Callback(F16_S0 a0, short a1, float a2, F16_S1 a3, F16_S2 a4, ulong a5, F16_S3 a6, nuint a7, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)-59, a0.F0);
            Assert.Equal((int)1181591186, a0.F1);
            Assert.Equal((ushort)44834, a0.F2);
            Assert.Equal((ushort)28664, a0.F3);
            Assert.Equal((uint)404461767, a0.F4);
            Assert.Equal((short)2482, a1);
            Assert.Equal((float)2997348, a2);
            Assert.Equal((ushort)22423, a3.F0);
            Assert.Equal((sbyte)-106, a3.F1);
            Assert.Equal((byte)182, a3.F2);
            Assert.Equal((nint)unchecked((nint)3784074551275084420), a3.F3);
            Assert.Equal((nint)unchecked((nint)7092934571108982079), a3.F4);
            Assert.Equal((int)1835134709, a4.F0);
            Assert.Equal((int)246067261, a4.F1);
            Assert.Equal((uint)1986526591, a4.F2);
            Assert.Equal((byte)24, a4.F3);
            Assert.Equal((sbyte)-112, a4.F4.F0);
            Assert.Equal((ulong)1465053746911704089, a5);
            Assert.Equal((short)-27636, a6.F0);
            Assert.Equal((double)1896887612303356, a6.F1);
            Assert.Equal((double)4263157082840190, a6.F2);
            Assert.Equal((int)774653659, a6.F3);
            Assert.Equal((nuint)unchecked((nuint)3755775782607884861), a7);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 103;
    }

    [Fact]
    public static void TestSwiftCallbackFunc16()
    {
        Console.Write("Running SwiftCallbackFunc16: ");
        ExceptionDispatchInfo ex = null;
        sbyte val = SwiftCallbackFunc16(&SwiftCallbackFunc16Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((sbyte)103, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F17_S0
    {
        public int F0;
        public nuint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F17_S1_S0
    {
        public double F0;
        public uint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 17)]
    struct F17_S1
    {
        public F17_S1_S0 F0;
        public int F1;
        public byte F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F17_S2
    {
        public uint F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func171fS2ds6UInt32V_AA6F17_S0VAA0H3_S1VSds6UInt64VAA0H3_S2VtXE_tF")]
    private static extern double SwiftCallbackFunc17(delegate* unmanaged[Swift]<uint, F17_S0, F17_S1, double, ulong, F17_S2, SwiftSelf, double> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static double SwiftCallbackFunc17Callback(uint a0, F17_S0 a1, F17_S1 a2, double a3, ulong a4, F17_S2 a5, SwiftSelf self)
    {
        try
        {
            Assert.Equal((uint)201081002, a0);
            Assert.Equal((int)2018751226, a1.F0);
            Assert.Equal((nuint)unchecked((nuint)8488544433072104028), a1.F1);
            Assert.Equal((double)1190765430157980, a2.F0.F0);
            Assert.Equal((uint)70252071, a2.F0.F1);
            Assert.Equal((int)1297775609, a2.F1);
            Assert.Equal((byte)160, a2.F2);
            Assert.Equal((double)4290084351352688, a3);
            Assert.Equal((ulong)4738339757002694731, a4);
            Assert.Equal((uint)1829312773, a5.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 4214404512040467;
    }

    [Fact]
    public static void TestSwiftCallbackFunc17()
    {
        Console.Write("Running SwiftCallbackFunc17: ");
        ExceptionDispatchInfo ex = null;
        double val = SwiftCallbackFunc17(&SwiftCallbackFunc17Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((double)4214404512040467, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F18_S0
    {
        public sbyte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F18_S1
    {
        public ushort F0;
        public short F1;
        public double F2;
        public nuint F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F18_S2
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F18_Ret_S0
    {
        public short F0;

        public F18_Ret_S0(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F18_Ret
    {
        public F18_Ret_S0 F0;

        public F18_Ret(F18_Ret_S0 f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func181fAA7F18_RetVAeA0G3_S0V_AA0G3_S1VAA0G3_S2VSus6UInt32Vs5Int64Vs5Int16VSdtXE_tF")]
    private static extern F18_Ret SwiftCallbackFunc18(delegate* unmanaged[Swift]<F18_S0, F18_S1, F18_S2, nuint, uint, long, short, double, SwiftSelf, F18_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F18_Ret SwiftCallbackFunc18Callback(F18_S0 a0, F18_S1 a1, F18_S2 a2, nuint a3, uint a4, long a5, short a6, double a7, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)106, a0.F0);
            Assert.Equal((ushort)21619, a1.F0);
            Assert.Equal((short)-4350, a1.F1);
            Assert.Equal((double)3457288266203248, a1.F2);
            Assert.Equal((nuint)unchecked((nuint)9020447812661292883), a1.F3);
            Assert.Equal((nint)unchecked((nint)2317132584983719004), a2.F0);
            Assert.Equal((nuint)unchecked((nuint)7379425918918939512), a3);
            Assert.Equal((uint)2055208746, a4);
            Assert.Equal((long)1042861174364145790, a5);
            Assert.Equal((short)28457, a6);
            Assert.Equal((double)1799004152435515, a7);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F18_Ret(new F18_Ret_S0(-2080));
    }

    [Fact]
    public static void TestSwiftCallbackFunc18()
    {
        Console.Write("Running SwiftCallbackFunc18: ");
        ExceptionDispatchInfo ex = null;
        F18_Ret val = SwiftCallbackFunc18(&SwiftCallbackFunc18Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((short)-2080, val.F0.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F19_S0
    {
        public short F0;
        public sbyte F1;
        public float F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F19_S1
    {
        public long F0;
        public ushort F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F19_S2
    {
        public ulong F0;
        public long F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F19_S3
    {
        public uint F0;
        public int F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F19_Ret_S0
    {
        public long F0;

        public F19_Ret_S0(long f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 56)]
    struct F19_Ret
    {
        public uint F0;
        public long F1;
        public ushort F2;
        public F19_Ret_S0 F3;
        public double F4;
        public double F5;
        public double F6;

        public F19_Ret(uint f0, long f1, ushort f2, F19_Ret_S0 f3, double f4, double f5, double f6)
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
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func191fAA7F19_RetVAEs5Int64V_s5UInt8VAA0G3_S0VSiAA0G3_S1Vs5Int32VAOSus6UInt64VAA0G3_S2Vs6UInt16VAA0G3_S3Vs4Int8VAGtXE_tF")]
    private static extern F19_Ret SwiftCallbackFunc19(delegate* unmanaged[Swift]<long, byte, F19_S0, nint, F19_S1, int, int, nuint, ulong, F19_S2, ushort, F19_S3, sbyte, long, SwiftSelf, F19_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F19_Ret SwiftCallbackFunc19Callback(long a0, byte a1, F19_S0 a2, nint a3, F19_S1 a4, int a5, int a6, nuint a7, ulong a8, F19_S2 a9, ushort a10, F19_S3 a11, sbyte a12, long a13, SwiftSelf self)
    {
        try
        {
            Assert.Equal((long)7456120134117592143, a0);
            Assert.Equal((byte)114, a1);
            Assert.Equal((short)-7583, a2.F0);
            Assert.Equal((sbyte)97, a2.F1);
            Assert.Equal((float)2768322, a2.F2);
            Assert.Equal((nint)unchecked((nint)3605245176125291560), a3);
            Assert.Equal((long)4445885313084714470, a4.F0);
            Assert.Equal((ushort)15810, a4.F1);
            Assert.Equal((int)1179699879, a5);
            Assert.Equal((int)109603412, a6);
            Assert.Equal((nuint)unchecked((nuint)6521628547431964799), a7);
            Assert.Equal((ulong)7687430644226018854, a8);
            Assert.Equal((ulong)8464855230956039883, a9.F0);
            Assert.Equal((long)861462819289140037, a9.F1);
            Assert.Equal((ushort)26519, a10);
            Assert.Equal((uint)1864602741, a11.F0);
            Assert.Equal((int)397176384, a11.F1);
            Assert.Equal((sbyte)81, a12);
            Assert.Equal((long)4909173176891211442, a13);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F19_Ret(301901837, 5183322153843416979, 16744, new F19_Ret_S0(4587948079871666183), 341974742264104, 750011710367955, 681779256292286);
    }

    [Fact]
    public static void TestSwiftCallbackFunc19()
    {
        Console.Write("Running SwiftCallbackFunc19: ");
        ExceptionDispatchInfo ex = null;
        F19_Ret val = SwiftCallbackFunc19(&SwiftCallbackFunc19Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((uint)301901837, val.F0);
        Assert.Equal((long)5183322153843416979, val.F1);
        Assert.Equal((ushort)16744, val.F2);
        Assert.Equal((long)4587948079871666183, val.F3.F0);
        Assert.Equal((double)341974742264104, val.F4);
        Assert.Equal((double)750011710367955, val.F5);
        Assert.Equal((double)681779256292286, val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F20_S0_S0
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F20_S0
    {
        public short F0;
        public nuint F1;
        public F20_S0_S0 F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F20_S1_S0
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 36)]
    struct F20_S1
    {
        public long F0;
        public nuint F1;
        public F20_S1_S0 F2;
        public long F3;
        public int F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F20_S2
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F20_Ret
    {
        public ushort F0;
        public ushort F1;
        public double F2;
        public short F3;
        public double F4;

        public F20_Ret(ushort f0, ushort f1, double f2, short f3, double f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func201fAA7F20_RetVAeA0G3_S0V_AA0G3_S1VS2fs4Int8VAA0G3_S2VSftXE_tF")]
    private static extern F20_Ret SwiftCallbackFunc20(delegate* unmanaged[Swift]<F20_S0, F20_S1, float, float, sbyte, F20_S2, float, SwiftSelf, F20_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F20_Ret SwiftCallbackFunc20Callback(F20_S0 a0, F20_S1 a1, float a2, float a3, sbyte a4, F20_S2 a5, float a6, SwiftSelf self)
    {
        try
        {
            Assert.Equal((short)28858, a0.F0);
            Assert.Equal((nuint)unchecked((nuint)7024100299344418039), a0.F1);
            Assert.Equal((ushort)13025, a0.F2.F0);
            Assert.Equal((long)7900431324553135989, a1.F0);
            Assert.Equal((nuint)unchecked((nuint)8131425055682506706), a1.F1);
            Assert.Equal((float)3884322, a1.F2.F0);
            Assert.Equal((long)605453501265278638, a1.F3);
            Assert.Equal((int)353756684, a1.F4);
            Assert.Equal((float)622319, a2);
            Assert.Equal((float)1401604, a3);
            Assert.Equal((sbyte)-101, a4);
            Assert.Equal((uint)1355570413, a5.F0);
            Assert.Equal((float)2912776, a6);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F20_Ret(53384, 55736, 105589186779121, -24217, 2181722329638192);
    }

    [Fact]
    public static void TestSwiftCallbackFunc20()
    {
        Console.Write("Running SwiftCallbackFunc20: ");
        ExceptionDispatchInfo ex = null;
        F20_Ret val = SwiftCallbackFunc20(&SwiftCallbackFunc20Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)53384, val.F0);
        Assert.Equal((ushort)55736, val.F1);
        Assert.Equal((double)105589186779121, val.F2);
        Assert.Equal((short)-24217, val.F3);
        Assert.Equal((double)2181722329638192, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F21_S0
    {
        public double F0;
        public ulong F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F21_S1
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F21_Ret
    {
        public ushort F0;
        public uint F1;
        public long F2;

        public F21_Ret(ushort f0, uint f1, long f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func211fAA7F21_RetVAEs5Int32V_s5Int16VAA0G3_S0VAgA0G3_S1Vs5Int64Vs6UInt32VAOs5UInt8Vs6UInt16VtXE_tF")]
    private static extern F21_Ret SwiftCallbackFunc21(delegate* unmanaged[Swift]<int, short, F21_S0, int, F21_S1, long, uint, long, byte, ushort, SwiftSelf, F21_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F21_Ret SwiftCallbackFunc21Callback(int a0, short a1, F21_S0 a2, int a3, F21_S1 a4, long a5, uint a6, long a7, byte a8, ushort a9, SwiftSelf self)
    {
        try
        {
            Assert.Equal((int)256017319, a0);
            Assert.Equal((short)14555, a1);
            Assert.Equal((double)2102091966108033, a2.F0);
            Assert.Equal((ulong)8617538752301505079, a2.F1);
            Assert.Equal((int)834677431, a3);
            Assert.Equal((ushort)7043, a4.F0);
            Assert.Equal((long)7166819734655141128, a5);
            Assert.Equal((uint)965538086, a6);
            Assert.Equal((long)3827752442102685645, a7);
            Assert.Equal((byte)110, a8);
            Assert.Equal((ushort)33646, a9);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F21_Ret(13904, 1020161192, 7669588951617295307);
    }

    [Fact]
    public static void TestSwiftCallbackFunc21()
    {
        Console.Write("Running SwiftCallbackFunc21: ");
        ExceptionDispatchInfo ex = null;
        F21_Ret val = SwiftCallbackFunc21(&SwiftCallbackFunc21Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)13904, val.F0);
        Assert.Equal((uint)1020161192, val.F1);
        Assert.Equal((long)7669588951617295307, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F22_S0
    {
        public nint F0;
        public float F1;
        public double F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F22_S1
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F22_S2
    {
        public int F0;
        public double F1;
        public float F2;
        public short F3;
        public ushort F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F22_S3
    {
        public long F0;
        public ushort F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F22_S4
    {
        public double F0;
        public ushort F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 6)]
    struct F22_S5
    {
        public uint F0;
        public short F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F22_S6
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F22_Ret
    {
        public ushort F0;
        public short F1;
        public nuint F2;

        public F22_Ret(ushort f0, short f1, nuint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func221fAA7F22_RetVAEs5Int32V_AA0G3_S0VAA0G3_S1VAA0G3_S2VAA0G3_S3Vs4Int8VAA0G3_S4Vs5UInt8Vs6UInt16Vs5Int64VAA0G3_S5VAYSfAA0G3_S6VAWtXE_tF")]
    private static extern F22_Ret SwiftCallbackFunc22(delegate* unmanaged[Swift]<int, F22_S0, F22_S1, F22_S2, F22_S3, sbyte, F22_S4, byte, ushort, long, F22_S5, long, float, F22_S6, ushort, SwiftSelf, F22_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F22_Ret SwiftCallbackFunc22Callback(int a0, F22_S0 a1, F22_S1 a2, F22_S2 a3, F22_S3 a4, sbyte a5, F22_S4 a6, byte a7, ushort a8, long a9, F22_S5 a10, long a11, float a12, F22_S6 a13, ushort a14, SwiftSelf self)
    {
        try
        {
            Assert.Equal((int)640156952, a0);
            Assert.Equal((nint)unchecked((nint)824774470287401457), a1.F0);
            Assert.Equal((float)6163704, a1.F1);
            Assert.Equal((double)54328782764685, a1.F2);
            Assert.Equal((nuint)unchecked((nuint)1679730195865415747), a2.F0);
            Assert.Equal((int)1462995665, a3.F0);
            Assert.Equal((double)2554087365600344, a3.F1);
            Assert.Equal((float)8193295, a3.F2);
            Assert.Equal((short)16765, a3.F3);
            Assert.Equal((ushort)45388, a3.F4);
            Assert.Equal((long)5560492364570389430, a4.F0);
            Assert.Equal((ushort)48308, a4.F1);
            Assert.Equal((sbyte)71, a5);
            Assert.Equal((double)1639169280741045, a6.F0);
            Assert.Equal((ushort)12045, a6.F1);
            Assert.Equal((byte)217, a7);
            Assert.Equal((ushort)62917, a8);
            Assert.Equal((long)1465918945905384332, a9);
            Assert.Equal((uint)1364750179, a10.F0);
            Assert.Equal((short)3311, a10.F1);
            Assert.Equal((long)9003480567517966914, a11);
            Assert.Equal((float)2157327, a12);
            Assert.Equal((float)6647392, a13.F0);
            Assert.Equal((ushort)1760, a14);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F22_Ret(39726, 21753, unchecked((nuint)5706055053768469840));
    }

    [Fact]
    public static void TestSwiftCallbackFunc22()
    {
        Console.Write("Running SwiftCallbackFunc22: ");
        ExceptionDispatchInfo ex = null;
        F22_Ret val = SwiftCallbackFunc22(&SwiftCallbackFunc22Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)39726, val.F0);
        Assert.Equal((short)21753, val.F1);
        Assert.Equal((nuint)unchecked((nuint)5706055053768469840), val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F23_S0
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F23_S1
    {
        public nint F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func231fS2dSu_s5UInt8Vs4Int8VA2eA6F23_S0VSuAA0I3_S1VSdtXE_tF")]
    private static extern double SwiftCallbackFunc23(delegate* unmanaged[Swift]<nuint, byte, sbyte, byte, byte, F23_S0, nuint, F23_S1, double, SwiftSelf, double> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static double SwiftCallbackFunc23Callback(nuint a0, byte a1, sbyte a2, byte a3, byte a4, F23_S0 a5, nuint a6, F23_S1 a7, double a8, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nuint)unchecked((nuint)5779410841248940897), a0);
            Assert.Equal((byte)192, a1);
            Assert.Equal((sbyte)-128, a2);
            Assert.Equal((byte)133, a3);
            Assert.Equal((byte)20, a4);
            Assert.Equal((nint)unchecked((nint)2959916071636885436), a5.F0);
            Assert.Equal((nuint)unchecked((nuint)3651155214497129159), a6);
            Assert.Equal((nint)unchecked((nint)8141565342203061885), a7.F0);
            Assert.Equal((double)1465425469608034, a8);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 893532429511039;
    }

    [Fact]
    public static void TestSwiftCallbackFunc23()
    {
        Console.Write("Running SwiftCallbackFunc23: ");
        ExceptionDispatchInfo ex = null;
        double val = SwiftCallbackFunc23(&SwiftCallbackFunc23Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((double)893532429511039, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F24_S0
    {
        public sbyte F0;
        public byte F1;
        public ulong F2;
        public uint F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F24_S1
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F24_S2_S0
    {
        public ushort F0;
        public uint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F24_S2_S1
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F24_S2
    {
        public nint F0;
        public uint F1;
        public F24_S2_S0 F2;
        public F24_S2_S1 F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F24_S3
    {
        public short F0;
        public float F1;
        public long F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F24_S4
    {
        public byte F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func241fS2fs5Int32V_SuAA6F24_S0Vs6UInt16VAA0H3_S1Vs4Int8VAA0H3_S2Vs6UInt64VAqA0H3_S3VSdAA0H3_S4VtXE_tF")]
    private static extern float SwiftCallbackFunc24(delegate* unmanaged[Swift]<int, nuint, F24_S0, ushort, F24_S1, sbyte, F24_S2, ulong, ulong, F24_S3, double, F24_S4, SwiftSelf, float> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static float SwiftCallbackFunc24Callback(int a0, nuint a1, F24_S0 a2, ushort a3, F24_S1 a4, sbyte a5, F24_S2 a6, ulong a7, ulong a8, F24_S3 a9, double a10, F24_S4 a11, SwiftSelf self)
    {
        try
        {
            Assert.Equal((int)1710754874, a0);
            Assert.Equal((nuint)unchecked((nuint)6447433131978039331), a1);
            Assert.Equal((sbyte)-92, a2.F0);
            Assert.Equal((byte)181, a2.F1);
            Assert.Equal((ulong)3710374263631495948, a2.F2);
            Assert.Equal((uint)257210428, a2.F3);
            Assert.Equal((ushort)6631, a3);
            Assert.Equal((ushort)2303, a4.F0);
            Assert.Equal((sbyte)15, a5);
            Assert.Equal((nint)unchecked((nint)2509049432824972381), a6.F0);
            Assert.Equal((uint)616918672, a6.F1);
            Assert.Equal((ushort)50635, a6.F2.F0);
            Assert.Equal((uint)1337844540, a6.F2.F1);
            Assert.Equal((long)335964796567786281, a6.F3.F0);
            Assert.Equal((ulong)1114365571136806382, a7);
            Assert.Equal((ulong)8988425145801188208, a8);
            Assert.Equal((short)31969, a9.F0);
            Assert.Equal((float)3008861, a9.F1);
            Assert.Equal((long)5466306080595269107, a9.F2);
            Assert.Equal((double)2027780227887952, a10);
            Assert.Equal((byte)234, a11.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 3470219;
    }

    [Fact]
    public static void TestSwiftCallbackFunc24()
    {
        Console.Write("Running SwiftCallbackFunc24: ");
        ExceptionDispatchInfo ex = null;
        float val = SwiftCallbackFunc24(&SwiftCallbackFunc24Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((float)3470219, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F25_S0
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F25_S1
    {
        public float F0;
        public sbyte F1;
        public float F2;
        public nint F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 25)]
    struct F25_S2
    {
        public nuint F0;
        public nuint F1;
        public long F2;
        public byte F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F25_S3
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F25_S4
    {
        public sbyte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F25_Ret
    {
        public ulong F0;
        public long F1;
        public byte F2;
        public ushort F3;

        public F25_Ret(ulong f0, long f1, byte f2, ushort f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func251fAA7F25_RetVAeA0G3_S0V_s6UInt16VSuAA0G3_S1Vs5Int16VAA0G3_S2Vs6UInt64VA2qA0G3_S3VAA0G3_S4VtXE_tF")]
    private static extern F25_Ret SwiftCallbackFunc25(delegate* unmanaged[Swift]<F25_S0, ushort, nuint, F25_S1, short, F25_S2, ulong, ulong, ulong, F25_S3, F25_S4, SwiftSelf, F25_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F25_Ret SwiftCallbackFunc25Callback(F25_S0 a0, ushort a1, nuint a2, F25_S1 a3, short a4, F25_S2 a5, ulong a6, ulong a7, ulong a8, F25_S3 a9, F25_S4 a10, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nuint)unchecked((nuint)6077761381429658786), a0.F0);
            Assert.Equal((ushort)2300, a1);
            Assert.Equal((nuint)unchecked((nuint)3498354181807010234), a2);
            Assert.Equal((float)5360721, a3.F0);
            Assert.Equal((sbyte)-40, a3.F1);
            Assert.Equal((float)109485, a3.F2);
            Assert.Equal((nint)unchecked((nint)2311625789899959825), a3.F3);
            Assert.Equal((short)-28395, a4);
            Assert.Equal((nuint)unchecked((nuint)8729509817732080529), a5.F0);
            Assert.Equal((nuint)unchecked((nuint)860365359368130822), a5.F1);
            Assert.Equal((long)7498894262834346040, a5.F2);
            Assert.Equal((byte)218, a5.F3);
            Assert.Equal((ulong)961687210282504701, a6);
            Assert.Equal((ulong)7184177441364400868, a7);
            Assert.Equal((ulong)8389319500274436977, a8);
            Assert.Equal((float)4437173, a9.F0);
            Assert.Equal((sbyte)-107, a10.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F25_Ret(8006862079710523876, 7879510716857855733, 114, 3220);
    }

    [Fact]
    public static void TestSwiftCallbackFunc25()
    {
        Console.Write("Running SwiftCallbackFunc25: ");
        ExceptionDispatchInfo ex = null;
        F25_Ret val = SwiftCallbackFunc25(&SwiftCallbackFunc25Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)8006862079710523876, val.F0);
        Assert.Equal((long)7879510716857855733, val.F1);
        Assert.Equal((byte)114, val.F2);
        Assert.Equal((ushort)3220, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F26_S0
    {
        public sbyte F0;
        public nint F1;
        public byte F2;
        public byte F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F26_S1_S0
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F26_S1
    {
        public sbyte F0;
        public int F1;
        public short F2;
        public F26_S1_S0 F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F26_S2
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F26_S3
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct F26_Ret
    {
        public nuint F0;
        public byte F1;

        public F26_Ret(nuint f0, byte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func261fAA7F26_RetVAEs4Int8V_s5UInt8Vs6UInt32VAA0G3_S0VAA0G3_S1VAA0G3_S2VAA0G3_S3VtXE_tF")]
    private static extern F26_Ret SwiftCallbackFunc26(delegate* unmanaged[Swift]<sbyte, byte, uint, F26_S0, F26_S1, F26_S2, F26_S3, SwiftSelf, F26_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F26_Ret SwiftCallbackFunc26Callback(sbyte a0, byte a1, uint a2, F26_S0 a3, F26_S1 a4, F26_S2 a5, F26_S3 a6, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)-16, a0);
            Assert.Equal((byte)220, a1);
            Assert.Equal((uint)72386567, a2);
            Assert.Equal((sbyte)-33, a3.F0);
            Assert.Equal((nint)unchecked((nint)6488877286424796715), a3.F1);
            Assert.Equal((byte)143, a3.F2);
            Assert.Equal((byte)74, a3.F3);
            Assert.Equal((sbyte)104, a4.F0);
            Assert.Equal((int)1719453315, a4.F1);
            Assert.Equal((short)20771, a4.F2);
            Assert.Equal((ulong)3636117595999837800, a4.F3.F0);
            Assert.Equal((long)2279530426119665839, a5.F0);
            Assert.Equal((byte)207, a6.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F26_Ret(unchecked((nuint)1050319650554930471), 89);
    }

    [Fact]
    public static void TestSwiftCallbackFunc26()
    {
        Console.Write("Running SwiftCallbackFunc26: ");
        ExceptionDispatchInfo ex = null;
        F26_Ret val = SwiftCallbackFunc26(&SwiftCallbackFunc26Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nuint)unchecked((nuint)1050319650554930471), val.F0);
        Assert.Equal((byte)89, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F27_S0
    {
        public short F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 3)]
    struct F27_S1_S0
    {
        public ushort F0;
        public sbyte F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F27_S1
    {
        public long F0;
        public F27_S1_S0 F1;
        public float F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F27_S2
    {
        public ulong F0;
        public sbyte F1;
        public uint F2;
        public long F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F27_S3_S0
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F27_S3
    {
        public F27_S3_S0 F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func271fS2fs6UInt64V_s5UInt8VAA6F27_S0VA2gA0I3_S1Vs5Int32VAA0I3_S2VSis6UInt32VAA0I3_S3VtXE_tF")]
    private static extern float SwiftCallbackFunc27(delegate* unmanaged[Swift]<ulong, byte, F27_S0, byte, byte, F27_S1, int, F27_S2, nint, uint, F27_S3, SwiftSelf, float> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static float SwiftCallbackFunc27Callback(ulong a0, byte a1, F27_S0 a2, byte a3, byte a4, F27_S1 a5, int a6, F27_S2 a7, nint a8, uint a9, F27_S3 a10, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ulong)4847421047018330189, a0);
            Assert.Equal((byte)214, a1);
            Assert.Equal((short)31313, a2.F0);
            Assert.Equal((byte)207, a3);
            Assert.Equal((byte)174, a4);
            Assert.Equal((long)4476120319602257660, a5.F0);
            Assert.Equal((ushort)26662, a5.F1.F0);
            Assert.Equal((sbyte)-55, a5.F1.F1);
            Assert.Equal((float)70666, a5.F2);
            Assert.Equal((int)1340306103, a6);
            Assert.Equal((ulong)2772939788297637999, a7.F0);
            Assert.Equal((sbyte)-65, a7.F1);
            Assert.Equal((uint)7500441, a7.F2);
            Assert.Equal((long)4926907273817562134, a7.F3);
            Assert.Equal((nint)unchecked((nint)5862689255099071258), a8);
            Assert.Equal((uint)1077270996, a9);
            Assert.Equal((ushort)35167, a10.F0.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 8117856;
    }

    [Fact]
    public static void TestSwiftCallbackFunc27()
    {
        Console.Write("Running SwiftCallbackFunc27: ");
        ExceptionDispatchInfo ex = null;
        float val = SwiftCallbackFunc27(&SwiftCallbackFunc27Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((float)8117856, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct F28_S0
    {
        public ulong F0;
        public sbyte F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F28_S1
    {
        public long F0;
        public nuint F1;
        public nint F2;
        public int F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F28_S2
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F28_S3
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F28_Ret_S0
    {
        public float F0;

        public F28_Ret_S0(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 6)]
    struct F28_Ret
    {
        public F28_Ret_S0 F0;
        public ushort F1;

        public F28_Ret(F28_Ret_S0 f0, ushort f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func281fAA7F28_RetVAEs6UInt32V_s6UInt16Vs4Int8VAkISfAA0G3_S0VSds6UInt64VAA0G3_S1VAA0G3_S2VAA0G3_S3VtXE_tF")]
    private static extern F28_Ret SwiftCallbackFunc28(delegate* unmanaged[Swift]<uint, ushort, sbyte, sbyte, ushort, float, F28_S0, double, ulong, F28_S1, F28_S2, F28_S3, SwiftSelf, F28_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F28_Ret SwiftCallbackFunc28Callback(uint a0, ushort a1, sbyte a2, sbyte a3, ushort a4, float a5, F28_S0 a6, double a7, ulong a8, F28_S1 a9, F28_S2 a10, F28_S3 a11, SwiftSelf self)
    {
        try
        {
            Assert.Equal((uint)893827094, a0);
            Assert.Equal((ushort)38017, a1);
            Assert.Equal((sbyte)-90, a2);
            Assert.Equal((sbyte)-1, a3);
            Assert.Equal((ushort)16109, a4);
            Assert.Equal((float)5844449, a5);
            Assert.Equal((ulong)176269147098539470, a6.F0);
            Assert.Equal((sbyte)23, a6.F1);
            Assert.Equal((double)1431426259441210, a7);
            Assert.Equal((ulong)6103261251702315645, a8);
            Assert.Equal((long)3776818122826483419, a9.F0);
            Assert.Equal((nuint)unchecked((nuint)9181420263296840471), a9.F1);
            Assert.Equal((nint)unchecked((nint)3281861424961082542), a9.F2);
            Assert.Equal((int)1442905253, a9.F3);
            Assert.Equal((nint)unchecked((nint)8760009193798370900), a10.F0);
            Assert.Equal((long)7119917900929398683, a11.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F28_Ret(new F28_Ret_S0(4515425), 25944);
    }

    [Fact]
    public static void TestSwiftCallbackFunc28()
    {
        Console.Write("Running SwiftCallbackFunc28: ");
        ExceptionDispatchInfo ex = null;
        F28_Ret val = SwiftCallbackFunc28(&SwiftCallbackFunc28Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((float)4515425, val.F0.F0);
        Assert.Equal((ushort)25944, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F29_S0
    {
        public byte F0;
        public double F1;
        public ushort F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F29_S1
    {
        public uint F0;
        public nint F1;
        public ulong F2;
        public uint F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F29_S2
    {
        public int F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F29_S3
    {
        public uint F0;
        public uint F1;
        public float F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F29_S4
    {
        public int F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F29_Ret_S0
    {
        public nint F0;
        public ulong F1;

        public F29_Ret_S0(nint f0, ulong f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 52)]
    struct F29_Ret
    {
        public nuint F0;
        public nuint F1;
        public nuint F2;
        public F29_Ret_S0 F3;
        public ulong F4;
        public uint F5;

        public F29_Ret(nuint f0, nuint f1, nuint f2, F29_Ret_S0 f3, ulong f4, uint f5)
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
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func291fAA7F29_RetVAeA0G3_S0V_Sis6UInt64Vs5UInt8Vs5Int64VAKSiAA0G3_S1Vs5Int32Vs4Int8VAkiA0G3_S2VAA0G3_S3Vs5Int16VAA0G3_S4Vs6UInt32VtXE_tF")]
    private static extern F29_Ret SwiftCallbackFunc29(delegate* unmanaged[Swift]<F29_S0, nint, ulong, byte, long, byte, nint, F29_S1, int, sbyte, byte, ulong, F29_S2, F29_S3, short, F29_S4, uint, SwiftSelf, F29_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F29_Ret SwiftCallbackFunc29Callback(F29_S0 a0, nint a1, ulong a2, byte a3, long a4, byte a5, nint a6, F29_S1 a7, int a8, sbyte a9, byte a10, ulong a11, F29_S2 a12, F29_S3 a13, short a14, F29_S4 a15, uint a16, SwiftSelf self)
    {
        try
        {
            Assert.Equal((byte)152, a0.F0);
            Assert.Equal((double)737900189383874, a0.F1);
            Assert.Equal((ushort)33674, a0.F2);
            Assert.Equal((nint)unchecked((nint)5162040247631126074), a1);
            Assert.Equal((ulong)6524156301721885895, a2);
            Assert.Equal((byte)129, a3);
            Assert.Equal((long)6661424933974053497, a4);
            Assert.Equal((byte)145, a5);
            Assert.Equal((nint)unchecked((nint)7521422786615537370), a6);
            Assert.Equal((uint)1361601345, a7.F0);
            Assert.Equal((nint)unchecked((nint)3366726213840694614), a7.F1);
            Assert.Equal((ulong)7767610514138029164, a7.F2);
            Assert.Equal((uint)1266864987, a7.F3);
            Assert.Equal((int)1115803878, a8);
            Assert.Equal((sbyte)5, a9);
            Assert.Equal((byte)80, a10);
            Assert.Equal((ulong)2041754562738600205, a11);
            Assert.Equal((int)1492686870, a12.F0);
            Assert.Equal((uint)142491811, a13.F0);
            Assert.Equal((uint)1644962309, a13.F1);
            Assert.Equal((float)1905811, a13.F2);
            Assert.Equal((short)-3985, a14);
            Assert.Equal((int)1921386549, a15.F0);
            Assert.Equal((uint)1510666400, a16);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F29_Ret(unchecked((nuint)1866868811776234672), unchecked((nuint)8169323498884891375), unchecked((nuint)2528257272266524428), new F29_Ret_S0(unchecked((nint)4705260670026405131), 8299241689326234556), 4459635217352912270, 188636136);
    }

    [Fact]
    public static void TestSwiftCallbackFunc29()
    {
        Console.Write("Running SwiftCallbackFunc29: ");
        ExceptionDispatchInfo ex = null;
        F29_Ret val = SwiftCallbackFunc29(&SwiftCallbackFunc29Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nuint)unchecked((nuint)1866868811776234672), val.F0);
        Assert.Equal((nuint)unchecked((nuint)8169323498884891375), val.F1);
        Assert.Equal((nuint)unchecked((nuint)2528257272266524428), val.F2);
        Assert.Equal((nint)unchecked((nint)4705260670026405131), val.F3.F0);
        Assert.Equal((ulong)8299241689326234556, val.F3.F1);
        Assert.Equal((ulong)4459635217352912270, val.F4);
        Assert.Equal((uint)188636136, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 7)]
    struct F30_S0
    {
        public ushort F0;
        public short F1;
        public short F2;
        public sbyte F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F30_S1
    {
        public ushort F0;
        public nuint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F30_S2
    {
        public long F0;
        public sbyte F1;
        public ushort F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F30_S3
    {
        public sbyte F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func301fS2fAA6F30_S0V_AA0G3_S1VAA0G3_S2VAA0G3_S3VSitXE_tF")]
    private static extern float SwiftCallbackFunc30(delegate* unmanaged[Swift]<F30_S0, F30_S1, F30_S2, F30_S3, nint, SwiftSelf, float> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static float SwiftCallbackFunc30Callback(F30_S0 a0, F30_S1 a1, F30_S2 a2, F30_S3 a3, nint a4, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ushort)50723, a0.F0);
            Assert.Equal((short)19689, a0.F1);
            Assert.Equal((short)-6469, a0.F2);
            Assert.Equal((sbyte)83, a0.F3);
            Assert.Equal((ushort)51238, a1.F0);
            Assert.Equal((nuint)unchecked((nuint)5879147675377398012), a1.F1);
            Assert.Equal((long)7909999288286190848, a2.F0);
            Assert.Equal((sbyte)-99, a2.F1);
            Assert.Equal((ushort)61385, a2.F2);
            Assert.Equal((sbyte)48, a3.F0);
            Assert.Equal((nint)unchecked((nint)2980085298293056148), a4);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 289587;
    }

    [Fact]
    public static void TestSwiftCallbackFunc30()
    {
        Console.Write("Running SwiftCallbackFunc30: ");
        ExceptionDispatchInfo ex = null;
        float val = SwiftCallbackFunc30(&SwiftCallbackFunc30Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((float)289587, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F31_S0
    {
        public int F0;
        public ulong F1;
        public nuint F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F31_Ret_S0
    {
        public uint F0;
        public float F1;
        public ushort F2;
        public short F3;
        public float F4;

        public F31_Ret_S0(uint f0, float f1, ushort f2, short f3, float f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F31_Ret
    {
        public F31_Ret_S0 F0;
        public ushort F1;

        public F31_Ret(F31_Ret_S0 f0, ushort f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func311fAA7F31_RetVAeA0G3_S0V_SdtXE_tF")]
    private static extern F31_Ret SwiftCallbackFunc31(delegate* unmanaged[Swift]<F31_S0, double, SwiftSelf, F31_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F31_Ret SwiftCallbackFunc31Callback(F31_S0 a0, double a1, SwiftSelf self)
    {
        try
        {
            Assert.Equal((int)1072945099, a0.F0);
            Assert.Equal((ulong)5760996810500287322, a0.F1);
            Assert.Equal((nuint)unchecked((nuint)3952909367135409979), a0.F2);
            Assert.Equal((double)2860786541632685, a1);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F31_Ret(new F31_Ret_S0(1236856932, 1761447, 1260, 25704, 6212541), 44632);
    }

    [Fact]
    public static void TestSwiftCallbackFunc31()
    {
        Console.Write("Running SwiftCallbackFunc31: ");
        ExceptionDispatchInfo ex = null;
        F31_Ret val = SwiftCallbackFunc31(&SwiftCallbackFunc31Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((uint)1236856932, val.F0.F0);
        Assert.Equal((float)1761447, val.F0.F1);
        Assert.Equal((ushort)1260, val.F0.F2);
        Assert.Equal((short)25704, val.F0.F3);
        Assert.Equal((float)6212541, val.F0.F4);
        Assert.Equal((ushort)44632, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F32_Ret
    {
        public nuint F0;
        public double F1;
        public nint F2;

        public F32_Ret(nuint f0, double f1, nint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func321fAA7F32_RetVAEs6UInt16V_s5Int16VtXE_tF")]
    private static extern F32_Ret SwiftCallbackFunc32(delegate* unmanaged[Swift]<ushort, short, SwiftSelf, F32_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F32_Ret SwiftCallbackFunc32Callback(ushort a0, short a1, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ushort)21020, a0);
            Assert.Equal((short)7462, a1);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F32_Ret(unchecked((nuint)868833742355713000), 411817582525317, unchecked((nint)3926422244180816571));
    }

    [Fact]
    public static void TestSwiftCallbackFunc32()
    {
        Console.Write("Running SwiftCallbackFunc32: ");
        ExceptionDispatchInfo ex = null;
        F32_Ret val = SwiftCallbackFunc32(&SwiftCallbackFunc32Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nuint)unchecked((nuint)868833742355713000), val.F0);
        Assert.Equal((double)411817582525317, val.F1);
        Assert.Equal((nint)unchecked((nint)3926422244180816571), val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F33_S0
    {
        public short F0;
        public ulong F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F33_S1_S0
    {
        public short F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F33_S1
    {
        public F33_S1_S0 F0;
        public uint F1;
        public nuint F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F33_S2
    {
        public uint F0;
        public ulong F1;
        public sbyte F2;
        public sbyte F3;
        public nuint F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F33_S3_S0_S0
    {
        public short F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F33_S3_S0
    {
        public F33_S3_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F33_S3
    {
        public F33_S3_S0 F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func331fS2uAA6F33_S0V_SfAA0G3_S1Vs6UInt32VSis4Int8VAKSfs5UInt8VSfAkA0G3_S2VSiAA0G3_S3VSiAItXE_tF")]
    private static extern nuint SwiftCallbackFunc33(delegate* unmanaged[Swift]<F33_S0, float, F33_S1, uint, nint, sbyte, sbyte, float, byte, float, sbyte, F33_S2, nint, F33_S3, nint, uint, SwiftSelf, nuint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static nuint SwiftCallbackFunc33Callback(F33_S0 a0, float a1, F33_S1 a2, uint a3, nint a4, sbyte a5, sbyte a6, float a7, byte a8, float a9, sbyte a10, F33_S2 a11, nint a12, F33_S3 a13, nint a14, uint a15, SwiftSelf self)
    {
        try
        {
            Assert.Equal((short)-23471, a0.F0);
            Assert.Equal((ulong)2736941806609505888, a0.F1);
            Assert.Equal((float)6930550, a1);
            Assert.Equal((short)32476, a2.F0.F0);
            Assert.Equal((uint)165441961, a2.F1);
            Assert.Equal((nuint)unchecked((nuint)3890227499323387948), a2.F2);
            Assert.Equal((uint)591524870, a3);
            Assert.Equal((nint)unchecked((nint)1668420058132495503), a4);
            Assert.Equal((sbyte)-67, a5);
            Assert.Equal((sbyte)94, a6);
            Assert.Equal((float)3180786, a7);
            Assert.Equal((byte)42, a8);
            Assert.Equal((float)7674952, a9);
            Assert.Equal((sbyte)43, a10);
            Assert.Equal((uint)771356149, a11.F0);
            Assert.Equal((ulong)3611576949210389997, a11.F1);
            Assert.Equal((sbyte)-15, a11.F2);
            Assert.Equal((sbyte)7, a11.F3);
            Assert.Equal((nuint)unchecked((nuint)2577587324978560192), a11.F4);
            Assert.Equal((nint)unchecked((nint)8266150294848599489), a12);
            Assert.Equal((short)9216, a13.F0.F0.F0);
            Assert.Equal((nint)unchecked((nint)710302565025364450), a14);
            Assert.Equal((uint)1060812904, a15);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return unchecked((nuint)8322391372382633712);
    }

    [Fact]
    public static void TestSwiftCallbackFunc33()
    {
        Console.Write("Running SwiftCallbackFunc33: ");
        ExceptionDispatchInfo ex = null;
        nuint val = SwiftCallbackFunc33(&SwiftCallbackFunc33Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nuint)unchecked((nuint)8322391372382633712), val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F34_S0_S0
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F34_S0
    {
        public F34_S0_S0 F0;
        public nuint F1;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func341fs6UInt16VAEs6UInt32V_AA6F34_S0VSus5Int16VtXE_tF")]
    private static extern ushort SwiftCallbackFunc34(delegate* unmanaged[Swift]<uint, F34_S0, nuint, short, SwiftSelf, ushort> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static ushort SwiftCallbackFunc34Callback(uint a0, F34_S0 a1, nuint a2, short a3, SwiftSelf self)
    {
        try
        {
            Assert.Equal((uint)2068009847, a0);
            Assert.Equal((uint)845123292, a1.F0.F0);
            Assert.Equal((nuint)unchecked((nuint)5148244462913472487), a1.F1);
            Assert.Equal((nuint)unchecked((nuint)8632568386462910655), a2);
            Assert.Equal((short)7058, a3);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 20647;
    }

    [Fact]
    public static void TestSwiftCallbackFunc34()
    {
        Console.Write("Running SwiftCallbackFunc34: ");
        ExceptionDispatchInfo ex = null;
        ushort val = SwiftCallbackFunc34(&SwiftCallbackFunc34Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)20647, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F35_S0_S0_S0
    {
        public int F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F35_S0_S0
    {
        public long F0;
        public F35_S0_S0_S0 F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F35_S0_S1
    {
        public double F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F35_S0
    {
        public F35_S0_S0 F0;
        public int F1;
        public F35_S0_S1 F2;
        public nint F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F35_S1
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F35_S2_S0
    {
        public double F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F35_S2
    {
        public F35_S2_S0 F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func351fs6UInt64VAEs5UInt8V_s4Int8VSfs5Int64VSiAA6F35_S0VAA0K3_S1VAA0K3_S2VtXE_tF")]
    private static extern ulong SwiftCallbackFunc35(delegate* unmanaged[Swift]<byte, sbyte, float, long, nint, F35_S0, F35_S1, F35_S2, SwiftSelf, ulong> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static ulong SwiftCallbackFunc35Callback(byte a0, sbyte a1, float a2, long a3, nint a4, F35_S0 a5, F35_S1 a6, F35_S2 a7, SwiftSelf self)
    {
        try
        {
            Assert.Equal((byte)182, a0);
            Assert.Equal((sbyte)-16, a1);
            Assert.Equal((float)7763558, a2);
            Assert.Equal((long)5905028570860904693, a3);
            Assert.Equal((nint)unchecked((nint)5991001624972063224), a4);
            Assert.Equal((long)6663912001709962059, a5.F0.F0);
            Assert.Equal((int)1843939591, a5.F0.F1.F0);
            Assert.Equal((int)1095170337, a5.F1);
            Assert.Equal((double)3908756332193409, a5.F2.F0);
            Assert.Equal((nint)unchecked((nint)8246190362462442203), a5.F3);
            Assert.Equal((ushort)52167, a6.F0);
            Assert.Equal((double)283499999631068, a7.F0.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 4329482286317894385;
    }

    [Fact]
    public static void TestSwiftCallbackFunc35()
    {
        Console.Write("Running SwiftCallbackFunc35: ");
        ExceptionDispatchInfo ex = null;
        ulong val = SwiftCallbackFunc35(&SwiftCallbackFunc35Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)4329482286317894385, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F36_S0
    {
        public uint F0;
        public long F1;
        public byte F2;
        public nuint F3;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func361fS2iSu_SdSus5UInt8Vs5Int64VAA6F36_S0Vs4Int8VtXE_tF")]
    private static extern nint SwiftCallbackFunc36(delegate* unmanaged[Swift]<nuint, double, nuint, byte, long, F36_S0, sbyte, SwiftSelf, nint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static nint SwiftCallbackFunc36Callback(nuint a0, double a1, nuint a2, byte a3, long a4, F36_S0 a5, sbyte a6, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nuint)unchecked((nuint)5079603407518207003), a0);
            Assert.Equal((double)2365862518115571, a1);
            Assert.Equal((nuint)unchecked((nuint)6495651757722767835), a2);
            Assert.Equal((byte)46, a3);
            Assert.Equal((long)1550138390178394449, a4);
            Assert.Equal((uint)1858960269, a5.F0);
            Assert.Equal((long)1925263848394986294, a5.F1);
            Assert.Equal((byte)217, a5.F2);
            Assert.Equal((nuint)unchecked((nuint)8520779488644482307), a5.F3);
            Assert.Equal((sbyte)-83, a6);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return unchecked((nint)2889858798271230534);
    }

    [Fact]
    public static void TestSwiftCallbackFunc36()
    {
        Console.Write("Running SwiftCallbackFunc36: ");
        ExceptionDispatchInfo ex = null;
        nint val = SwiftCallbackFunc36(&SwiftCallbackFunc36Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)2889858798271230534), val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F37_S0_S0
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F37_S0
    {
        public nuint F0;
        public uint F1;
        public F37_S0_S0 F2;
        public float F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F37_S1
    {
        public nuint F0;
        public uint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F37_S2
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F37_Ret
    {
        public float F0;
        public byte F1;
        public short F2;
        public ulong F3;

        public F37_Ret(float f0, byte f1, short f2, ulong f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func371fAA7F37_RetVAEs6UInt64V_AA0G3_S0VSds6UInt16VAA0G3_S1VAA0G3_S2VtXE_tF")]
    private static extern F37_Ret SwiftCallbackFunc37(delegate* unmanaged[Swift]<ulong, F37_S0, double, ushort, F37_S1, F37_S2, SwiftSelf, F37_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F37_Ret SwiftCallbackFunc37Callback(ulong a0, F37_S0 a1, double a2, ushort a3, F37_S1 a4, F37_S2 a5, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ulong)1623104856688575867, a0);
            Assert.Equal((nuint)unchecked((nuint)3785544303342575322), a1.F0);
            Assert.Equal((uint)717682682, a1.F1);
            Assert.Equal((nint)unchecked((nint)2674933748436691896), a1.F2.F0);
            Assert.Equal((float)3211458, a1.F3);
            Assert.Equal((double)996705046384579, a2);
            Assert.Equal((ushort)8394, a3);
            Assert.Equal((nuint)unchecked((nuint)1048947722954084863), a4.F0);
            Assert.Equal((uint)252415487, a4.F1);
            Assert.Equal((ushort)3664, a5.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F37_Ret(433224, 163, -5538, 4525229514824359136);
    }

    [Fact]
    public static void TestSwiftCallbackFunc37()
    {
        Console.Write("Running SwiftCallbackFunc37: ");
        ExceptionDispatchInfo ex = null;
        F37_Ret val = SwiftCallbackFunc37(&SwiftCallbackFunc37Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((float)433224, val.F0);
        Assert.Equal((byte)163, val.F1);
        Assert.Equal((short)-5538, val.F2);
        Assert.Equal((ulong)4525229514824359136, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F38_S0_S0
    {
        public nint F0;
        public float F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F38_S0
    {
        public F38_S0_S0 F0;
        public ushort F1;
        public int F2;
        public float F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F38_S1
    {
        public short F0;
        public int F1;
        public uint F2;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func381fS2dAA6F38_S0V_AA0G3_S1VSds5Int16Vs4Int8Vs6UInt32VAISfSiSfAMs5UInt8VSdAKtXE_tF")]
    private static extern double SwiftCallbackFunc38(delegate* unmanaged[Swift]<F38_S0, F38_S1, double, short, sbyte, uint, short, float, nint, float, uint, byte, double, sbyte, SwiftSelf, double> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static double SwiftCallbackFunc38Callback(F38_S0 a0, F38_S1 a1, double a2, short a3, sbyte a4, uint a5, short a6, float a7, nint a8, float a9, uint a10, byte a11, double a12, sbyte a13, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nint)unchecked((nint)7389960750529773276), a0.F0.F0);
            Assert.Equal((float)4749108, a0.F0.F1);
            Assert.Equal((ushort)54323, a0.F1);
            Assert.Equal((int)634649910, a0.F2);
            Assert.Equal((float)83587, a0.F3);
            Assert.Equal((short)-15547, a1.F0);
            Assert.Equal((int)1747384081, a1.F1);
            Assert.Equal((uint)851987981, a1.F2);
            Assert.Equal((double)3543874366683681, a2);
            Assert.Equal((short)5045, a3);
            Assert.Equal((sbyte)-32, a4);
            Assert.Equal((uint)2084540698, a5);
            Assert.Equal((short)25583, a6);
            Assert.Equal((float)3158067, a7);
            Assert.Equal((nint)unchecked((nint)1655263182833369283), a8);
            Assert.Equal((float)829404, a9);
            Assert.Equal((uint)1888859844, a10);
            Assert.Equal((byte)153, a11);
            Assert.Equal((double)222366180309763, a12);
            Assert.Equal((sbyte)61, a13);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 2529010496939244;
    }

    [Fact]
    public static void TestSwiftCallbackFunc38()
    {
        Console.Write("Running SwiftCallbackFunc38: ");
        ExceptionDispatchInfo ex = null;
        double val = SwiftCallbackFunc38(&SwiftCallbackFunc38Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((double)2529010496939244, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F39_S0_S0
    {
        public short F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F39_S0_S1
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F39_S0
    {
        public F39_S0_S0 F0;
        public int F1;
        public F39_S0_S1 F2;
        public nuint F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F39_S1
    {
        public ushort F0;
        public byte F1;
        public float F2;
        public long F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F39_S2
    {
        public int F0;
        public float F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F39_S3
    {
        public uint F0;
        public nint F1;
        public nint F2;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func391fS2iAA6F39_S0V_Sus6UInt32VSdAA0G3_S1VAA0G3_S2Vs4Int8VAA0G3_S3Vs5Int32Vs6UInt64Vs5UInt8VtXE_tF")]
    private static extern nint SwiftCallbackFunc39(delegate* unmanaged[Swift]<F39_S0, nuint, uint, double, F39_S1, F39_S2, sbyte, F39_S3, int, ulong, byte, SwiftSelf, nint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static nint SwiftCallbackFunc39Callback(F39_S0 a0, nuint a1, uint a2, double a3, F39_S1 a4, F39_S2 a5, sbyte a6, F39_S3 a7, int a8, ulong a9, byte a10, SwiftSelf self)
    {
        try
        {
            Assert.Equal((short)-31212, a0.F0.F0);
            Assert.Equal((int)1623216479, a0.F1);
            Assert.Equal((ushort)7181, a0.F2.F0);
            Assert.Equal((nuint)unchecked((nuint)8643545152918150186), a0.F3);
            Assert.Equal((nuint)unchecked((nuint)799631211988519637), a1);
            Assert.Equal((uint)94381581, a2);
            Assert.Equal((double)761127371030426, a3);
            Assert.Equal((ushort)417, a4.F0);
            Assert.Equal((byte)85, a4.F1);
            Assert.Equal((float)1543931, a4.F2);
            Assert.Equal((long)3918460222899735322, a4.F3);
            Assert.Equal((int)883468300, a5.F0);
            Assert.Equal((float)2739152, a5.F1);
            Assert.Equal((sbyte)-94, a6);
            Assert.Equal((uint)1374766954, a7.F0);
            Assert.Equal((nint)unchecked((nint)2042223450490396789), a7.F1);
            Assert.Equal((nint)unchecked((nint)2672454113535023130), a7.F2);
            Assert.Equal((int)946259065, a8);
            Assert.Equal((ulong)6805548458517673751, a9);
            Assert.Equal((byte)61, a10);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return unchecked((nint)3023907365579871618);
    }

    [Fact]
    public static void TestSwiftCallbackFunc39()
    {
        Console.Write("Running SwiftCallbackFunc39: ");
        ExceptionDispatchInfo ex = null;
        nint val = SwiftCallbackFunc39(&SwiftCallbackFunc39Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)3023907365579871618), val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F40_S0
    {
        public short F0;
        public int F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F40_S1
    {
        public int F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 25)]
    struct F40_S2
    {
        public long F0;
        public ushort F1;
        public nint F2;
        public byte F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F40_S3_S0
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F40_S3
    {
        public nuint F0;
        public double F1;
        public F40_S3_S0 F2;
        public double F3;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func401fS2uAA6F40_S0V_s6UInt32Vs5UInt8VAA0G3_S1VAA0G3_S2Vs6UInt64VSuAOSis6UInt16VAgA0G3_S3VSutXE_tF")]
    private static extern nuint SwiftCallbackFunc40(delegate* unmanaged[Swift]<F40_S0, uint, byte, F40_S1, F40_S2, ulong, nuint, ulong, nint, ushort, uint, F40_S3, nuint, SwiftSelf, nuint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static nuint SwiftCallbackFunc40Callback(F40_S0 a0, uint a1, byte a2, F40_S1 a3, F40_S2 a4, ulong a5, nuint a6, ulong a7, nint a8, ushort a9, uint a10, F40_S3 a11, nuint a12, SwiftSelf self)
    {
        try
        {
            Assert.Equal((short)22601, a0.F0);
            Assert.Equal((int)312892872, a0.F1);
            Assert.Equal((uint)1040102825, a1);
            Assert.Equal((byte)56, a2);
            Assert.Equal((int)101203812, a3.F0);
            Assert.Equal((long)4298883321494088257, a4.F0);
            Assert.Equal((ushort)2095, a4.F1);
            Assert.Equal((nint)unchecked((nint)1536552108568739270), a4.F2);
            Assert.Equal((byte)220, a4.F3);
            Assert.Equal((ulong)2564624804830565018, a5);
            Assert.Equal((nuint)unchecked((nuint)173855559108584219), a6);
            Assert.Equal((ulong)6222832940831380264, a7);
            Assert.Equal((nint)unchecked((nint)1898370824516510398), a8);
            Assert.Equal((ushort)3352, a9);
            Assert.Equal((uint)1643571476, a10);
            Assert.Equal((nuint)unchecked((nuint)7940054758811932961), a11.F0);
            Assert.Equal((double)246670432251533, a11.F1);
            Assert.Equal((float)7890596, a11.F2.F0);
            Assert.Equal((double)1094140965415232, a11.F3);
            Assert.Equal((nuint)unchecked((nuint)2081923113238309816), a12);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return unchecked((nuint)4616766375038360400);
    }

    [Fact]
    public static void TestSwiftCallbackFunc40()
    {
        Console.Write("Running SwiftCallbackFunc40: ");
        ExceptionDispatchInfo ex = null;
        nuint val = SwiftCallbackFunc40(&SwiftCallbackFunc40Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nuint)unchecked((nuint)4616766375038360400), val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F41_S0
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F41_Ret
    {
        public ulong F0;
        public double F1;
        public uint F2;
        public uint F3;

        public F41_Ret(ulong f0, double f1, uint f2, uint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func411fAA7F41_RetVAeA0G3_S0VXE_tF")]
    private static extern F41_Ret SwiftCallbackFunc41(delegate* unmanaged[Swift]<F41_S0, SwiftSelf, F41_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F41_Ret SwiftCallbackFunc41Callback(F41_S0 a0, SwiftSelf self)
    {
        try
        {
            Assert.Equal((uint)1430200072, a0.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F41_Ret(5150172797708870426, 3489330932479773, 833949606, 2098665090);
    }

    [Fact]
    public static void TestSwiftCallbackFunc41()
    {
        Console.Write("Running SwiftCallbackFunc41: ");
        ExceptionDispatchInfo ex = null;
        F41_Ret val = SwiftCallbackFunc41(&SwiftCallbackFunc41Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)5150172797708870426, val.F0);
        Assert.Equal((double)3489330932479773, val.F1);
        Assert.Equal((uint)833949606, val.F2);
        Assert.Equal((uint)2098665090, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F42_S0_S0
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F42_S0
    {
        public F42_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F42_S1
    {
        public uint F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func421fS2is5Int32V_s6UInt32VAA6F42_S0VSfs5UInt8VAA0I3_S1VtXE_tF")]
    private static extern nint SwiftCallbackFunc42(delegate* unmanaged[Swift]<int, uint, F42_S0, float, byte, F42_S1, SwiftSelf, nint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static nint SwiftCallbackFunc42Callback(int a0, uint a1, F42_S0 a2, float a3, byte a4, F42_S1 a5, SwiftSelf self)
    {
        try
        {
            Assert.Equal((int)1046060439, a0);
            Assert.Equal((uint)1987212952, a1);
            Assert.Equal((nint)unchecked((nint)4714080408858753964), a2.F0.F0);
            Assert.Equal((float)2364146, a3);
            Assert.Equal((byte)25, a4);
            Assert.Equal((uint)666986488, a5.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return unchecked((nint)4147856807670154637);
    }

    [Fact]
    public static void TestSwiftCallbackFunc42()
    {
        Console.Write("Running SwiftCallbackFunc42: ");
        ExceptionDispatchInfo ex = null;
        nint val = SwiftCallbackFunc42(&SwiftCallbackFunc42Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)4147856807670154637), val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F43_S0
    {
        public int F0;
        public int F1;
        public nint F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F43_S1
    {
        public sbyte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F43_Ret
    {
        public ushort F0;

        public F43_Ret(ushort f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func431fAA7F43_RetVAeA0G3_S0V_AA0G3_S1VtXE_tF")]
    private static extern F43_Ret SwiftCallbackFunc43(delegate* unmanaged[Swift]<F43_S0, F43_S1, SwiftSelf, F43_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F43_Ret SwiftCallbackFunc43Callback(F43_S0 a0, F43_S1 a1, SwiftSelf self)
    {
        try
        {
            Assert.Equal((int)406102630, a0.F0);
            Assert.Equal((int)1946236062, a0.F1);
            Assert.Equal((nint)unchecked((nint)663606396354980308), a0.F2);
            Assert.Equal((sbyte)-8, a1.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F43_Ret(18672);
    }

    [Fact]
    public static void TestSwiftCallbackFunc43()
    {
        Console.Write("Running SwiftCallbackFunc43: ");
        ExceptionDispatchInfo ex = null;
        F43_Ret val = SwiftCallbackFunc43(&SwiftCallbackFunc43Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)18672, val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F44_S0
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F44_S1_S0
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F44_S1_S1
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F44_S1
    {
        public short F0;
        public short F1;
        public F44_S1_S0 F2;
        public F44_S1_S1 F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F44_S2
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F44_S3
    {
        public sbyte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F44_Ret_S0
    {
        public nuint F0;

        public F44_Ret_S0(nuint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F44_Ret
    {
        public nint F0;
        public F44_Ret_S0 F1;
        public double F2;

        public F44_Ret(nint f0, F44_Ret_S0 f1, double f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func441fAA7F44_RetVAESd_AA0G3_S0VAA0G3_S1VAA0G3_S2VAA0G3_S3VtXE_tF")]
    private static extern F44_Ret SwiftCallbackFunc44(delegate* unmanaged[Swift]<double, F44_S0, F44_S1, F44_S2, F44_S3, SwiftSelf, F44_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F44_Ret SwiftCallbackFunc44Callback(double a0, F44_S0 a1, F44_S1 a2, F44_S2 a3, F44_S3 a4, SwiftSelf self)
    {
        try
        {
            Assert.Equal((double)4281406007431544, a0);
            Assert.Equal((uint)2097291497, a1.F0);
            Assert.Equal((short)-10489, a2.F0);
            Assert.Equal((short)-9573, a2.F1);
            Assert.Equal((ushort)62959, a2.F2.F0);
            Assert.Equal((nuint)unchecked((nuint)7144119809173057975), a2.F3.F0);
            Assert.Equal((nuint)unchecked((nuint)168733393207234277), a3.F0);
            Assert.Equal((sbyte)64, a4.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F44_Ret(unchecked((nint)7157474620613398513), new F44_Ret_S0(unchecked((nuint)8272092288451488897)), 8724612718809);
    }

    [Fact]
    public static void TestSwiftCallbackFunc44()
    {
        Console.Write("Running SwiftCallbackFunc44: ");
        ExceptionDispatchInfo ex = null;
        F44_Ret val = SwiftCallbackFunc44(&SwiftCallbackFunc44Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)7157474620613398513), val.F0);
        Assert.Equal((nuint)unchecked((nuint)8272092288451488897), val.F1.F0);
        Assert.Equal((double)8724612718809, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F45_S0
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F45_S1
    {
        public nuint F0;
        public short F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F45_Ret_S0
    {
        public float F0;

        public F45_Ret_S0(float f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    struct F45_Ret
    {
        public double F0;
        public F45_Ret_S0 F1;
        public long F2;
        public double F3;
        public ulong F4;
        public sbyte F5;
        public int F6;

        public F45_Ret(double f0, F45_Ret_S0 f1, long f2, double f3, ulong f4, sbyte f5, int f6)
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
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func451fAA7F45_RetVAeA0G3_S0V_AA0G3_S1Vs5UInt8VtXE_tF")]
    private static extern F45_Ret SwiftCallbackFunc45(delegate* unmanaged[Swift]<F45_S0, F45_S1, byte, SwiftSelf, F45_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F45_Ret SwiftCallbackFunc45Callback(F45_S0 a0, F45_S1 a1, byte a2, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nuint)unchecked((nuint)5311803360204128233), a0.F0);
            Assert.Equal((nuint)unchecked((nuint)2204790044275015546), a1.F0);
            Assert.Equal((short)8942, a1.F1);
            Assert.Equal((byte)207, a2);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F45_Ret(262658215125446, new F45_Ret_S0(3145713), 4924669542959578265, 2052183120467519, 3135406744871464298, 81, 1000720476);
    }

    [Fact]
    public static void TestSwiftCallbackFunc45()
    {
        Console.Write("Running SwiftCallbackFunc45: ");
        ExceptionDispatchInfo ex = null;
        F45_Ret val = SwiftCallbackFunc45(&SwiftCallbackFunc45Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((double)262658215125446, val.F0);
        Assert.Equal((float)3145713, val.F1.F0);
        Assert.Equal((long)4924669542959578265, val.F2);
        Assert.Equal((double)2052183120467519, val.F3);
        Assert.Equal((ulong)3135406744871464298, val.F4);
        Assert.Equal((sbyte)81, val.F5);
        Assert.Equal((int)1000720476, val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 26)]
    struct F46_Ret
    {
        public nuint F0;
        public double F1;
        public long F2;
        public ushort F3;

        public F46_Ret(nuint f0, double f1, long f2, ushort f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func461fAA7F46_RetVAESi_Sus6UInt16VAGs5Int64VtXE_tF")]
    private static extern F46_Ret SwiftCallbackFunc46(delegate* unmanaged[Swift]<nint, nuint, ushort, ushort, long, SwiftSelf, F46_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F46_Ret SwiftCallbackFunc46Callback(nint a0, nuint a1, ushort a2, ushort a3, long a4, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nint)unchecked((nint)1855296013283572041), a0);
            Assert.Equal((nuint)unchecked((nuint)1145047910516899437), a1);
            Assert.Equal((ushort)20461, a2);
            Assert.Equal((ushort)58204, a3);
            Assert.Equal((long)1923767011143317115, a4);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F46_Ret(unchecked((nuint)4268855101008870857), 2061088094528291, 541679466428431692, 30655);
    }

    [Fact]
    public static void TestSwiftCallbackFunc46()
    {
        Console.Write("Running SwiftCallbackFunc46: ");
        ExceptionDispatchInfo ex = null;
        F46_Ret val = SwiftCallbackFunc46(&SwiftCallbackFunc46Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nuint)unchecked((nuint)4268855101008870857), val.F0);
        Assert.Equal((double)2061088094528291, val.F1);
        Assert.Equal((long)541679466428431692, val.F2);
        Assert.Equal((ushort)30655, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F47_S0
    {
        public byte F0;
        public int F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 13)]
    struct F47_S1
    {
        public nint F0;
        public uint F1;
        public sbyte F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F47_S2_S0
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 17)]
    struct F47_S2
    {
        public sbyte F0;
        public float F1;
        public int F2;
        public float F3;
        public F47_S2_S0 F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F47_S3
    {
        public ulong F0;
        public long F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F47_S4
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F47_Ret
    {
        public short F0;
        public short F1;
        public long F2;

        public F47_Ret(short f0, short f1, long f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func471fAA7F47_RetVAESi_Sfs6UInt32VAA0G3_S0VAA0G3_S1Vs6UInt16VSfS2iS2us5Int16VAA0G3_S2VAA0G3_S3VAA0G3_S4VtXE_tF")]
    private static extern F47_Ret SwiftCallbackFunc47(delegate* unmanaged[Swift]<nint, float, uint, F47_S0, F47_S1, ushort, float, nint, nint, nuint, nuint, short, F47_S2, F47_S3, F47_S4, SwiftSelf, F47_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F47_Ret SwiftCallbackFunc47Callback(nint a0, float a1, uint a2, F47_S0 a3, F47_S1 a4, ushort a5, float a6, nint a7, nint a8, nuint a9, nuint a10, short a11, F47_S2 a12, F47_S3 a13, F47_S4 a14, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nint)unchecked((nint)6545360066379352091), a0);
            Assert.Equal((float)1240616, a1);
            Assert.Equal((uint)575670382, a2);
            Assert.Equal((byte)27, a3.F0);
            Assert.Equal((int)1769677101, a3.F1);
            Assert.Equal((nint)unchecked((nint)4175209822525678639), a4.F0);
            Assert.Equal((uint)483151627, a4.F1);
            Assert.Equal((sbyte)-41, a4.F2);
            Assert.Equal((ushort)20891, a5);
            Assert.Equal((float)1011044, a6);
            Assert.Equal((nint)unchecked((nint)8543308148327168378), a7);
            Assert.Equal((nint)unchecked((nint)9126721646663585297), a8);
            Assert.Equal((nuint)unchecked((nuint)5438914191614359864), a9);
            Assert.Equal((nuint)unchecked((nuint)5284613245897089025), a10);
            Assert.Equal((short)-9227, a11);
            Assert.Equal((sbyte)-23, a12.F0);
            Assert.Equal((float)1294109, a12.F1);
            Assert.Equal((int)411726757, a12.F2);
            Assert.Equal((float)6621598, a12.F3);
            Assert.Equal((byte)249, a12.F4.F0);
            Assert.Equal((ulong)5281612261430853979, a13.F0);
            Assert.Equal((long)7161295082465816089, a13.F1);
            Assert.Equal((ulong)1995556861952451598, a14.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F47_Ret(32110, 21949, 479980404077668674);
    }

    [Fact]
    public static void TestSwiftCallbackFunc47()
    {
        Console.Write("Running SwiftCallbackFunc47: ");
        ExceptionDispatchInfo ex = null;
        F47_Ret val = SwiftCallbackFunc47(&SwiftCallbackFunc47Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((short)32110, val.F0);
        Assert.Equal((short)21949, val.F1);
        Assert.Equal((long)479980404077668674, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F48_S0
    {
        public ulong F0;
        public short F1;
        public ulong F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F48_S1_S0
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F48_S1
    {
        public double F0;
        public int F1;
        public int F2;
        public F48_S1_S0 F3;
        public nuint F4;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func481fs5Int64VAEs4Int8V_s5Int16VAIs6UInt32VAA6F48_S0VAkA0K3_S1Vs5Int32VAQs6UInt16VAeKtXE_tF")]
    private static extern long SwiftCallbackFunc48(delegate* unmanaged[Swift]<sbyte, short, short, uint, F48_S0, uint, F48_S1, int, int, ushort, long, uint, SwiftSelf, long> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static long SwiftCallbackFunc48Callback(sbyte a0, short a1, short a2, uint a3, F48_S0 a4, uint a5, F48_S1 a6, int a7, int a8, ushort a9, long a10, uint a11, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)-34, a0);
            Assert.Equal((short)11634, a1);
            Assert.Equal((short)-27237, a2);
            Assert.Equal((uint)1039294154, a3);
            Assert.Equal((ulong)1367847206719062131, a4.F0);
            Assert.Equal((short)22330, a4.F1);
            Assert.Equal((ulong)689282484471011648, a4.F2);
            Assert.Equal((uint)1572626904, a5);
            Assert.Equal((double)3054128759424009, a6.F0);
            Assert.Equal((int)1677338134, a6.F1);
            Assert.Equal((int)1257237843, a6.F2);
            Assert.Equal((float)6264494, a6.F3.F0);
            Assert.Equal((nuint)unchecked((nuint)8397097040610783205), a6.F4);
            Assert.Equal((int)1060447208, a7);
            Assert.Equal((int)269785114, a8);
            Assert.Equal((ushort)20635, a9);
            Assert.Equal((long)7679010342730986048, a10);
            Assert.Equal((uint)1362633148, a11);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 1864372483209206459;
    }

    [Fact]
    public static void TestSwiftCallbackFunc48()
    {
        Console.Write("Running SwiftCallbackFunc48: ");
        ExceptionDispatchInfo ex = null;
        long val = SwiftCallbackFunc48(&SwiftCallbackFunc48Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((long)1864372483209206459, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F49_S0_S0
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F49_S0
    {
        public F49_S0_S0 F0;
        public ulong F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F49_Ret
    {
        public int F0;
        public short F1;
        public byte F2;
        public byte F3;
        public sbyte F4;
        public long F5;

        public F49_Ret(int f0, short f1, byte f2, byte f3, sbyte f4, long f5)
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
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func491fAA7F49_RetVAeA0G3_S0V_s5Int64VtXE_tF")]
    private static extern F49_Ret SwiftCallbackFunc49(delegate* unmanaged[Swift]<F49_S0, long, SwiftSelf, F49_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F49_Ret SwiftCallbackFunc49Callback(F49_S0 a0, long a1, SwiftSelf self)
    {
        try
        {
            Assert.Equal((byte)48, a0.F0.F0);
            Assert.Equal((ulong)7563394992711018452, a0.F1);
            Assert.Equal((long)4358370311341042916, a1);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F49_Ret(1638493854, -13624, 61, 236, -97, 3942201385605817844);
    }

    [Fact]
    public static void TestSwiftCallbackFunc49()
    {
        Console.Write("Running SwiftCallbackFunc49: ");
        ExceptionDispatchInfo ex = null;
        F49_Ret val = SwiftCallbackFunc49(&SwiftCallbackFunc49Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((int)1638493854, val.F0);
        Assert.Equal((short)-13624, val.F1);
        Assert.Equal((byte)61, val.F2);
        Assert.Equal((byte)236, val.F3);
        Assert.Equal((sbyte)-97, val.F4);
        Assert.Equal((long)3942201385605817844, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F50_S0_S0
    {
        public double F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F50_S0
    {
        public ushort F0;
        public F50_S0_S0 F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F50_S1
    {
        public double F0;
        public ushort F1;
        public int F2;
        public nint F3;
        public double F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F50_S2
    {
        public int F0;
        public float F1;
        public uint F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 17)]
    struct F50_S3
    {
        public long F0;
        public int F1;
        public float F2;
        public sbyte F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F50_S4
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F50_S5_S0
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F50_S5
    {
        public F50_S5_S0 F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func501fs5UInt8VAeA6F50_S0V_AA0H3_S1VAeA0H3_S2Vs5Int32Vs6UInt64Vs4Int8VAQSfAA0H3_S3VAA0H3_S4VAA0H3_S5VSftXE_tF")]
    private static extern byte SwiftCallbackFunc50(delegate* unmanaged[Swift]<F50_S0, F50_S1, byte, F50_S2, int, ulong, sbyte, sbyte, float, F50_S3, F50_S4, F50_S5, float, SwiftSelf, byte> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static byte SwiftCallbackFunc50Callback(F50_S0 a0, F50_S1 a1, byte a2, F50_S2 a3, int a4, ulong a5, sbyte a6, sbyte a7, float a8, F50_S3 a9, F50_S4 a10, F50_S5 a11, float a12, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ushort)31857, a0.F0);
            Assert.Equal((double)1743417849706254, a0.F1.F0);
            Assert.Equal((double)4104577461772135, a1.F0);
            Assert.Equal((ushort)13270, a1.F1);
            Assert.Equal((int)2072598986, a1.F2);
            Assert.Equal((nint)unchecked((nint)9056978834867675248), a1.F3);
            Assert.Equal((double)844742439929087, a1.F4);
            Assert.Equal((byte)87, a2);
            Assert.Equal((int)1420884537, a3.F0);
            Assert.Equal((float)78807, a3.F1);
            Assert.Equal((uint)1081688273, a3.F2);
            Assert.Equal((int)336878110, a4);
            Assert.Equal((ulong)1146514566942283069, a5);
            Assert.Equal((sbyte)-93, a6);
            Assert.Equal((sbyte)73, a7);
            Assert.Equal((float)2321639, a8);
            Assert.Equal((long)1940888991336881606, a9.F0);
            Assert.Equal((int)688345394, a9.F1);
            Assert.Equal((float)712275, a9.F2);
            Assert.Equal((sbyte)-128, a9.F3);
            Assert.Equal((long)2638503583829414770, a10.F0);
            Assert.Equal((ushort)23681, a11.F0.F0);
            Assert.Equal((float)8223218, a12);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 252;
    }

    [Fact]
    public static void TestSwiftCallbackFunc50()
    {
        Console.Write("Running SwiftCallbackFunc50: ");
        ExceptionDispatchInfo ex = null;
        byte val = SwiftCallbackFunc50(&SwiftCallbackFunc50Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((byte)252, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F51_S0
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F51_Ret
    {
        public ushort F0;
        public sbyte F1;
        public nint F2;
        public ushort F3;
        public ulong F4;

        public F51_Ret(ushort f0, sbyte f1, nint f2, ushort f3, ulong f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func511fAA7F51_RetVAEs5Int16V_SuAA0G3_S0Vs6UInt64VtXE_tF")]
    private static extern F51_Ret SwiftCallbackFunc51(delegate* unmanaged[Swift]<short, nuint, F51_S0, ulong, SwiftSelf, F51_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F51_Ret SwiftCallbackFunc51Callback(short a0, nuint a1, F51_S0 a2, ulong a3, SwiftSelf self)
    {
        try
        {
            Assert.Equal((short)10812, a0);
            Assert.Equal((nuint)unchecked((nuint)470861239714315155), a1);
            Assert.Equal((long)5415660333180374788, a2.F0);
            Assert.Equal((ulong)2389942629143476149, a3);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F51_Ret(28396, 23, unchecked((nint)4042678034578400305), 16166, 8390419605778076733);
    }

    [Fact]
    public static void TestSwiftCallbackFunc51()
    {
        Console.Write("Running SwiftCallbackFunc51: ");
        ExceptionDispatchInfo ex = null;
        F51_Ret val = SwiftCallbackFunc51(&SwiftCallbackFunc51Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)28396, val.F0);
        Assert.Equal((sbyte)23, val.F1);
        Assert.Equal((nint)unchecked((nint)4042678034578400305), val.F2);
        Assert.Equal((ushort)16166, val.F3);
        Assert.Equal((ulong)8390419605778076733, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F52_S0
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F52_S1
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 33)]
    struct F52_Ret
    {
        public float F0;
        public ushort F1;
        public long F2;
        public short F3;
        public ulong F4;
        public sbyte F5;

        public F52_Ret(float f0, ushort f1, long f2, short f3, ulong f4, sbyte f5)
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
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func521fAA7F52_RetVAESi_AA0G3_S0Vs5Int16VAiA0G3_S1VtXE_tF")]
    private static extern F52_Ret SwiftCallbackFunc52(delegate* unmanaged[Swift]<nint, F52_S0, short, short, F52_S1, SwiftSelf, F52_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F52_Ret SwiftCallbackFunc52Callback(nint a0, F52_S0 a1, short a2, short a3, F52_S1 a4, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nint)unchecked((nint)3233654765973602550), a0);
            Assert.Equal((float)5997729, a1.F0);
            Assert.Equal((short)-7404, a2);
            Assert.Equal((short)-20804, a3);
            Assert.Equal((ushort)17231, a4.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F52_Ret(3003005, 4886, 1846269873983567093, 24151, 1408198981123859746, -41);
    }

    [Fact]
    public static void TestSwiftCallbackFunc52()
    {
        Console.Write("Running SwiftCallbackFunc52: ");
        ExceptionDispatchInfo ex = null;
        F52_Ret val = SwiftCallbackFunc52(&SwiftCallbackFunc52Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((float)3003005, val.F0);
        Assert.Equal((ushort)4886, val.F1);
        Assert.Equal((long)1846269873983567093, val.F2);
        Assert.Equal((short)24151, val.F3);
        Assert.Equal((ulong)1408198981123859746, val.F4);
        Assert.Equal((sbyte)-41, val.F5);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F53_S0_S0_S0
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F53_S0_S0
    {
        public F53_S0_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct F53_S0
    {
        public sbyte F0;
        public F53_S0_S0 F1;
        public byte F2;
        public nuint F3;
        public long F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 5)]
    struct F53_S1
    {
        public float F0;
        public byte F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F53_S2
    {
        public sbyte F0;
        public long F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F53_S3_S0
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F53_S3
    {
        public int F0;
        public uint F1;
        public F53_S3_S0 F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F53_S4
    {
        public short F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F53_S5_S0
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F53_S5_S1_S0
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F53_S5_S1
    {
        public F53_S5_S1_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F53_S5
    {
        public F53_S5_S0 F0;
        public nuint F1;
        public ushort F2;
        public F53_S5_S1 F3;
        public sbyte F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F53_S6
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F53_Ret
    {
        public nint F0;

        public F53_Ret(nint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func531fAA7F53_RetVAeA0G3_S0V_s5UInt8Vs5Int64VAA0G3_S1VAA0G3_S2VAA0G3_S3VAkA0G3_S4VAA0G3_S5VAA0G3_S6VtXE_tF")]
    private static extern F53_Ret SwiftCallbackFunc53(delegate* unmanaged[Swift]<F53_S0, byte, long, F53_S1, F53_S2, F53_S3, long, F53_S4, F53_S5, F53_S6, SwiftSelf, F53_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F53_Ret SwiftCallbackFunc53Callback(F53_S0 a0, byte a1, long a2, F53_S1 a3, F53_S2 a4, F53_S3 a5, long a6, F53_S4 a7, F53_S5 a8, F53_S6 a9, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)-123, a0.F0);
            Assert.Equal((long)3494916243607193741, a0.F1.F0.F0);
            Assert.Equal((byte)167, a0.F2);
            Assert.Equal((nuint)unchecked((nuint)4018943158751734338), a0.F3);
            Assert.Equal((long)6768175524813742847, a0.F4);
            Assert.Equal((byte)207, a1);
            Assert.Equal((long)8667995458064724392, a2);
            Assert.Equal((float)492157, a3.F0);
            Assert.Equal((byte)175, a3.F1);
            Assert.Equal((sbyte)76, a4.F0);
            Assert.Equal((long)5794486968525461488, a4.F1);
            Assert.Equal((int)2146070335, a5.F0);
            Assert.Equal((uint)1109141712, a5.F1);
            Assert.Equal((ushort)44270, a5.F2.F0);
            Assert.Equal((long)3581380181786253859, a6);
            Assert.Equal((short)23565, a7.F0);
            Assert.Equal((uint)1995174927, a8.F0.F0);
            Assert.Equal((nuint)unchecked((nuint)5025417700244056666), a8.F1);
            Assert.Equal((ushort)1847, a8.F2);
            Assert.Equal((byte)6, a8.F3.F0.F0);
            Assert.Equal((sbyte)-87, a8.F4);
            Assert.Equal((nint)unchecked((nint)5737280129078653969), a9.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F53_Ret(unchecked((nint)3955567540648861371));
    }

    [Fact]
    public static void TestSwiftCallbackFunc53()
    {
        Console.Write("Running SwiftCallbackFunc53: ");
        ExceptionDispatchInfo ex = null;
        F53_Ret val = SwiftCallbackFunc53(&SwiftCallbackFunc53Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)3955567540648861371), val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 17)]
    struct F54_S0
    {
        public int F0;
        public float F1;
        public nuint F2;
        public byte F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F54_S1
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F54_S2_S0_S0
    {
        public double F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F54_S2_S0
    {
        public short F0;
        public F54_S2_S0_S0 F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct F54_S2
    {
        public double F0;
        public F54_S2_S0 F1;
        public long F2;
        public ulong F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F54_S3
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 3)]
    struct F54_S4
    {
        public ushort F0;
        public sbyte F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F54_S5
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F54_Ret
    {
        public short F0;
        public nint F1;

        public F54_Ret(short f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func541fAA7F54_RetVAEs6UInt16V_AA0G3_S0VSfAA0G3_S1Vs5Int64Vs5Int32VAA0G3_S2VAA0G3_S3VAA0G3_S4VSfAA0G3_S5VtXE_tF")]
    private static extern F54_Ret SwiftCallbackFunc54(delegate* unmanaged[Swift]<ushort, F54_S0, float, F54_S1, long, int, F54_S2, F54_S3, F54_S4, float, F54_S5, SwiftSelf, F54_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F54_Ret SwiftCallbackFunc54Callback(ushort a0, F54_S0 a1, float a2, F54_S1 a3, long a4, int a5, F54_S2 a6, F54_S3 a7, F54_S4 a8, float a9, F54_S5 a10, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ushort)16440, a0);
            Assert.Equal((int)922752112, a1.F0);
            Assert.Equal((float)7843043, a1.F1);
            Assert.Equal((nuint)unchecked((nuint)1521939500434086364), a1.F2);
            Assert.Equal((byte)50, a1.F3);
            Assert.Equal((float)3111108, a2);
            Assert.Equal((ushort)50535, a3.F0);
            Assert.Equal((long)4761507229870258916, a4);
            Assert.Equal((int)1670668155, a5);
            Assert.Equal((double)432665443852892, a6.F0);
            Assert.Equal((short)13094, a6.F1.F0);
            Assert.Equal((double)669143993481144, a6.F1.F1.F0);
            Assert.Equal((long)30067117315069590, a6.F2);
            Assert.Equal((ulong)874012622621600805, a6.F3);
            Assert.Equal((float)7995066, a7.F0);
            Assert.Equal((ushort)48478, a8.F0);
            Assert.Equal((sbyte)23, a8.F1);
            Assert.Equal((float)4383787, a9);
            Assert.Equal((ushort)61633, a10.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F54_Ret(924, unchecked((nint)7680560643733996038));
    }

    [Fact]
    public static void TestSwiftCallbackFunc54()
    {
        Console.Write("Running SwiftCallbackFunc54: ");
        ExceptionDispatchInfo ex = null;
        F54_Ret val = SwiftCallbackFunc54(&SwiftCallbackFunc54Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((short)924, val.F0);
        Assert.Equal((nint)unchecked((nint)7680560643733996038), val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F55_S0_S0
    {
        public double F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 17)]
    struct F55_S0
    {
        public nuint F0;
        public F55_S0_S0 F1;
        public sbyte F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F55_S1
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F55_S2
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F55_Ret_S0
    {
        public short F0;
        public int F1;

        public F55_Ret_S0(short f0, int f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct F55_Ret
    {
        public nuint F0;
        public nint F1;
        public double F2;
        public F55_Ret_S0 F3;
        public ulong F4;

        public F55_Ret(nuint f0, nint f1, double f2, F55_Ret_S0 f3, ulong f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func551fAA7F55_RetVAeA0G3_S0V_s5Int64VAA0G3_S1Vs4Int8VAA0G3_S2VSftXE_tF")]
    private static extern F55_Ret SwiftCallbackFunc55(delegate* unmanaged[Swift]<F55_S0, long, F55_S1, sbyte, F55_S2, float, SwiftSelf, F55_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F55_Ret SwiftCallbackFunc55Callback(F55_S0 a0, long a1, F55_S1 a2, sbyte a3, F55_S2 a4, float a5, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nuint)unchecked((nuint)2856661562863799725), a0.F0);
            Assert.Equal((double)1260582440479139, a0.F1.F0);
            Assert.Equal((sbyte)5, a0.F2);
            Assert.Equal((long)7945068527720423751, a1);
            Assert.Equal((nint)unchecked((nint)4321616441998677375), a2.F0);
            Assert.Equal((sbyte)-68, a3);
            Assert.Equal((ulong)3311106172201778367, a4.F0);
            Assert.Equal((float)5600069, a5);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F55_Ret(unchecked((nuint)6916953478574785342), unchecked((nint)6448649235859031640), 1920468532326411, new F55_Ret_S0(30394, 40356024), 6146457824330132360);
    }

    [Fact]
    public static void TestSwiftCallbackFunc55()
    {
        Console.Write("Running SwiftCallbackFunc55: ");
        ExceptionDispatchInfo ex = null;
        F55_Ret val = SwiftCallbackFunc55(&SwiftCallbackFunc55Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nuint)unchecked((nuint)6916953478574785342), val.F0);
        Assert.Equal((nint)unchecked((nint)6448649235859031640), val.F1);
        Assert.Equal((double)1920468532326411, val.F2);
        Assert.Equal((short)30394, val.F3.F0);
        Assert.Equal((int)40356024, val.F3.F1);
        Assert.Equal((ulong)6146457824330132360, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F56_S0
    {
        public double F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func561fs6UInt32VAeA6F56_S0VXE_tF")]
    private static extern uint SwiftCallbackFunc56(delegate* unmanaged[Swift]<F56_S0, SwiftSelf, uint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static uint SwiftCallbackFunc56Callback(F56_S0 a0, SwiftSelf self)
    {
        try
        {
            Assert.Equal((double)3082602006731666, a0.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 1601166926;
    }

    [Fact]
    public static void TestSwiftCallbackFunc56()
    {
        Console.Write("Running SwiftCallbackFunc56: ");
        ExceptionDispatchInfo ex = null;
        uint val = SwiftCallbackFunc56(&SwiftCallbackFunc56Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((uint)1601166926, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F57_S0
    {
        public long F0;
        public int F1;
        public ulong F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F57_S1
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F57_S2
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F57_Ret_S0
    {
        public long F0;
        public byte F1;
        public short F2;

        public F57_Ret_S0(long f0, byte f1, short f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 13)]
    struct F57_Ret
    {
        public F57_Ret_S0 F0;
        public byte F1;

        public F57_Ret(F57_Ret_S0 f0, byte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func571fAA7F57_RetVAEs4Int8V_Sus6UInt32Vs5Int64Vs6UInt64Vs5Int16VAkA0G3_S0VAA0G3_S1VAA0G3_S2VtXE_tF")]
    private static extern F57_Ret SwiftCallbackFunc57(delegate* unmanaged[Swift]<sbyte, nuint, uint, long, ulong, short, long, F57_S0, F57_S1, F57_S2, SwiftSelf, F57_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F57_Ret SwiftCallbackFunc57Callback(sbyte a0, nuint a1, uint a2, long a3, ulong a4, short a5, long a6, F57_S0 a7, F57_S1 a8, F57_S2 a9, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)54, a0);
            Assert.Equal((nuint)unchecked((nuint)753245150862584974), a1);
            Assert.Equal((uint)1470962934, a2);
            Assert.Equal((long)1269392070140776313, a3);
            Assert.Equal((ulong)2296560034524654667, a4);
            Assert.Equal((short)12381, a5);
            Assert.Equal((long)198893062684618980, a6);
            Assert.Equal((long)1310571041794038100, a7.F0);
            Assert.Equal((int)18741662, a7.F1);
            Assert.Equal((ulong)7855196891704523814, a7.F2);
            Assert.Equal((byte)156, a8.F0);
            Assert.Equal((float)72045, a9.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F57_Ret(new F57_Ret_S0(3441370978522907304, 105, 24446), 200);
    }

    [Fact]
    public static void TestSwiftCallbackFunc57()
    {
        Console.Write("Running SwiftCallbackFunc57: ");
        ExceptionDispatchInfo ex = null;
        F57_Ret val = SwiftCallbackFunc57(&SwiftCallbackFunc57Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((long)3441370978522907304, val.F0.F0);
        Assert.Equal((byte)105, val.F0.F1);
        Assert.Equal((short)24446, val.F0.F2);
        Assert.Equal((byte)200, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F58_S0
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 6)]
    struct F58_S1
    {
        public float F0;
        public ushort F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F58_S2_S0_S0
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F58_S2_S0
    {
        public F58_S2_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F58_S2
    {
        public F58_S2_S0 F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func581fS2is6UInt64V_s4Int8VSiAA6F58_S0VAA0I3_S1Vs5Int64VAA0I3_S2Vs5Int32VtXE_tF")]
    private static extern nint SwiftCallbackFunc58(delegate* unmanaged[Swift]<ulong, sbyte, nint, F58_S0, F58_S1, long, F58_S2, int, SwiftSelf, nint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static nint SwiftCallbackFunc58Callback(ulong a0, sbyte a1, nint a2, F58_S0 a3, F58_S1 a4, long a5, F58_S2 a6, int a7, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ulong)4612004722568513699, a0);
            Assert.Equal((sbyte)-96, a1);
            Assert.Equal((nint)unchecked((nint)1970590839325113617), a2);
            Assert.Equal((byte)211, a3.F0);
            Assert.Equal((float)5454927, a4.F0);
            Assert.Equal((ushort)48737, a4.F1);
            Assert.Equal((long)921570327236881486, a5);
            Assert.Equal((nint)unchecked((nint)7726203059421444802), a6.F0.F0.F0);
            Assert.Equal((int)491616915, a7);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return unchecked((nint)5337995302960578101);
    }

    [Fact]
    public static void TestSwiftCallbackFunc58()
    {
        Console.Write("Running SwiftCallbackFunc58: ");
        ExceptionDispatchInfo ex = null;
        nint val = SwiftCallbackFunc58(&SwiftCallbackFunc58Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)5337995302960578101), val);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func591fs6UInt64VAEs6UInt16V_s5Int64VSitXE_tF")]
    private static extern ulong SwiftCallbackFunc59(delegate* unmanaged[Swift]<ushort, long, nint, SwiftSelf, ulong> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static ulong SwiftCallbackFunc59Callback(ushort a0, long a1, nint a2, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ushort)9232, a0);
            Assert.Equal((long)7281011081566942937, a1);
            Assert.Equal((nint)unchecked((nint)8203439771560005792), a2);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 7843473552989551261;
    }

    [Fact]
    public static void TestSwiftCallbackFunc59()
    {
        Console.Write("Running SwiftCallbackFunc59: ");
        ExceptionDispatchInfo ex = null;
        ulong val = SwiftCallbackFunc59(&SwiftCallbackFunc59Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)7843473552989551261, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F60_S0
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F60_S1
    {
        public ulong F0;
        public int F1;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func601fs6UInt64VAESf_Sds5Int64Vs6UInt16VS2fAA6F60_S0Vs5Int16VAA0J3_S1VAmGtXE_tF")]
    private static extern ulong SwiftCallbackFunc60(delegate* unmanaged[Swift]<float, double, long, ushort, float, float, F60_S0, short, F60_S1, short, long, SwiftSelf, ulong> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static ulong SwiftCallbackFunc60Callback(float a0, double a1, long a2, ushort a3, float a4, float a5, F60_S0 a6, short a7, F60_S1 a8, short a9, long a10, SwiftSelf self)
    {
        try
        {
            Assert.Equal((float)2682255, a0);
            Assert.Equal((double)2041676057169359, a1);
            Assert.Equal((long)5212916666940122160, a2);
            Assert.Equal((ushort)64444, a3);
            Assert.Equal((float)6372882, a4);
            Assert.Equal((float)8028835, a5);
            Assert.Equal((nint)unchecked((nint)6629286640024570381), a6.F0);
            Assert.Equal((short)1520, a7);
            Assert.Equal((ulong)8398497739914283366, a8.F0);
            Assert.Equal((int)1882981891, a8.F1);
            Assert.Equal((short)7716, a9);
            Assert.Equal((long)6631047215535600409, a10);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 1713850918199577358;
    }

    [Fact]
    public static void TestSwiftCallbackFunc60()
    {
        Console.Write("Running SwiftCallbackFunc60: ");
        ExceptionDispatchInfo ex = null;
        ulong val = SwiftCallbackFunc60(&SwiftCallbackFunc60Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)1713850918199577358, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F61_S0_S0
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F61_S0
    {
        public F61_S0_S0 F0;
        public long F1;
        public uint F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F61_S1
    {
        public sbyte F0;
        public float F1;
        public nint F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F61_S2_S0_S0
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F61_S2_S0
    {
        public F61_S2_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F61_S2_S1
    {
        public sbyte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct F61_S2
    {
        public F61_S2_S0 F0;
        public F61_S2_S1 F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F61_S3
    {
        public ulong F0;
        public nint F1;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func611fs6UInt32VA2E_AeA6F61_S0VAA0H3_S1VAA0H3_S2Vs4Int8Vs5Int16VAA0H3_S3Vs5Int32VAEtXE_tF")]
    private static extern uint SwiftCallbackFunc61(delegate* unmanaged[Swift]<uint, uint, F61_S0, F61_S1, F61_S2, sbyte, short, F61_S3, int, uint, SwiftSelf, uint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static uint SwiftCallbackFunc61Callback(uint a0, uint a1, F61_S0 a2, F61_S1 a3, F61_S2 a4, sbyte a5, short a6, F61_S3 a7, int a8, uint a9, SwiftSelf self)
    {
        try
        {
            Assert.Equal((uint)1070797065, a0);
            Assert.Equal((uint)135220309, a1);
            Assert.Equal((long)6475887024664217162, a2.F0.F0);
            Assert.Equal((long)563444654083452485, a2.F1);
            Assert.Equal((uint)1748956360, a2.F2);
            Assert.Equal((sbyte)-112, a3.F0);
            Assert.Equal((float)3433396, a3.F1);
            Assert.Equal((nint)unchecked((nint)8106074956722850624), a3.F2);
            Assert.Equal((ulong)2318628619979263858, a4.F0.F0.F0);
            Assert.Equal((sbyte)-93, a4.F1.F0);
            Assert.Equal((sbyte)-122, a5);
            Assert.Equal((short)-11696, a6);
            Assert.Equal((ulong)5229393236090246212, a7.F0);
            Assert.Equal((nint)unchecked((nint)4021449757638811198), a7.F1);
            Assert.Equal((int)689517945, a8);
            Assert.Equal((uint)657677740, a9);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 138627237;
    }

    [Fact]
    public static void TestSwiftCallbackFunc61()
    {
        Console.Write("Running SwiftCallbackFunc61: ");
        ExceptionDispatchInfo ex = null;
        uint val = SwiftCallbackFunc61(&SwiftCallbackFunc61Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((uint)138627237, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F62_S0
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F62_Ret
    {
        public ushort F0;
        public long F1;
        public nint F2;
        public long F3;

        public F62_Ret(ushort f0, long f1, nint f2, long f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func621fAA7F62_RetVAeA0G3_S0VXE_tF")]
    private static extern F62_Ret SwiftCallbackFunc62(delegate* unmanaged[Swift]<F62_S0, SwiftSelf, F62_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F62_Ret SwiftCallbackFunc62Callback(F62_S0 a0, SwiftSelf self)
    {
        try
        {
            Assert.Equal((float)6500993, a0.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F62_Ret(63013, 4076138842444340990, unchecked((nint)6876195265868121021), 223819901796794423);
    }

    [Fact]
    public static void TestSwiftCallbackFunc62()
    {
        Console.Write("Running SwiftCallbackFunc62: ");
        ExceptionDispatchInfo ex = null;
        F62_Ret val = SwiftCallbackFunc62(&SwiftCallbackFunc62Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)63013, val.F0);
        Assert.Equal((long)4076138842444340990, val.F1);
        Assert.Equal((nint)unchecked((nint)6876195265868121021), val.F2);
        Assert.Equal((long)223819901796794423, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F63_S0
    {
        public nint F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func631fS2fAA6F63_S0V_s5Int16VtXE_tF")]
    private static extern float SwiftCallbackFunc63(delegate* unmanaged[Swift]<F63_S0, short, SwiftSelf, float> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static float SwiftCallbackFunc63Callback(F63_S0 a0, short a1, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nint)unchecked((nint)8391317504019075904), a0.F0);
            Assert.Equal((short)11218, a1);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 1458978;
    }

    [Fact]
    public static void TestSwiftCallbackFunc63()
    {
        Console.Write("Running SwiftCallbackFunc63: ");
        ExceptionDispatchInfo ex = null;
        float val = SwiftCallbackFunc63(&SwiftCallbackFunc63Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((float)1458978, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F64_S0
    {
        public int F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F64_S1
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F64_S2
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F64_Ret_S0
    {
        public ushort F0;
        public nuint F1;
        public ulong F2;

        public F64_Ret_S0(ushort f0, nuint f1, ulong f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct F64_Ret
    {
        public nuint F0;
        public F64_Ret_S0 F1;
        public double F2;

        public F64_Ret(nuint f0, F64_Ret_S0 f1, double f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func641fAA7F64_RetVAEs4Int8V_AA0G3_S0VAA0G3_S1VSuAA0G3_S2VtXE_tF")]
    private static extern F64_Ret SwiftCallbackFunc64(delegate* unmanaged[Swift]<sbyte, F64_S0, F64_S1, nuint, F64_S2, SwiftSelf, F64_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F64_Ret SwiftCallbackFunc64Callback(sbyte a0, F64_S0 a1, F64_S1 a2, nuint a3, F64_S2 a4, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)-22, a0);
            Assert.Equal((int)1591678205, a1.F0);
            Assert.Equal((ulong)8355549563000003325, a2.F0);
            Assert.Equal((nuint)unchecked((nuint)5441989206466502201), a3);
            Assert.Equal((uint)2097092811, a4.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F64_Ret(unchecked((nuint)7966680593035770540), new F64_Ret_S0(20244, unchecked((nuint)7259704667595065333), 1039021449222712763), 594768504899138);
    }

    [Fact]
    public static void TestSwiftCallbackFunc64()
    {
        Console.Write("Running SwiftCallbackFunc64: ");
        ExceptionDispatchInfo ex = null;
        F64_Ret val = SwiftCallbackFunc64(&SwiftCallbackFunc64Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nuint)unchecked((nuint)7966680593035770540), val.F0);
        Assert.Equal((ushort)20244, val.F1.F0);
        Assert.Equal((nuint)unchecked((nuint)7259704667595065333), val.F1.F1);
        Assert.Equal((ulong)1039021449222712763, val.F1.F2);
        Assert.Equal((double)594768504899138, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F65_S0
    {
        public double F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F65_S1
    {
        public ushort F0;
        public nint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F65_S2
    {
        public short F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F65_S3
    {
        public int F0;
        public uint F1;
        public sbyte F2;
        public nuint F3;
        public double F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F65_Ret
    {
        public nint F0;
        public nint F1;
        public nint F2;
        public float F3;

        public F65_Ret(nint f0, nint f1, nint f2, float f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func651fAA7F65_RetVAeA0G3_S0V_s5Int16VSdSuAA0G3_S1Vs6UInt64VAA0G3_S2VSiAA0G3_S3Vs5Int32Vs5Int64Vs6UInt32VSdtXE_tF")]
    private static extern F65_Ret SwiftCallbackFunc65(delegate* unmanaged[Swift]<F65_S0, short, double, nuint, F65_S1, ulong, F65_S2, nint, F65_S3, int, long, uint, double, SwiftSelf, F65_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F65_Ret SwiftCallbackFunc65Callback(F65_S0 a0, short a1, double a2, nuint a3, F65_S1 a4, ulong a5, F65_S2 a6, nint a7, F65_S3 a8, int a9, long a10, uint a11, double a12, SwiftSelf self)
    {
        try
        {
            Assert.Equal((double)2969223123583220, a0.F0);
            Assert.Equal((short)-10269, a1);
            Assert.Equal((double)3909264978196109, a2);
            Assert.Equal((nuint)unchecked((nuint)522883062031213707), a3);
            Assert.Equal((ushort)37585, a4.F0);
            Assert.Equal((nint)unchecked((nint)5879827541057349126), a4.F1);
            Assert.Equal((ulong)1015270399093748716, a5);
            Assert.Equal((short)19670, a6.F0);
            Assert.Equal((nint)unchecked((nint)1900026319968050423), a7);
            Assert.Equal((int)1440511399, a8.F0);
            Assert.Equal((uint)1203865685, a8.F1);
            Assert.Equal((sbyte)12, a8.F2);
            Assert.Equal((nuint)unchecked((nuint)4061296318630567634), a8.F3);
            Assert.Equal((double)2406524883317724, a8.F4);
            Assert.Equal((int)1594888000, a9);
            Assert.Equal((long)2860599972459787263, a10);
            Assert.Equal((uint)1989052358, a11);
            Assert.Equal((double)1036075606072593, a12);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F65_Ret(unchecked((nint)7810903219784151958), unchecked((nint)8310527878848492866), unchecked((nint)1357258266300958550), 5970506);
    }

    [Fact]
    public static void TestSwiftCallbackFunc65()
    {
        Console.Write("Running SwiftCallbackFunc65: ");
        ExceptionDispatchInfo ex = null;
        F65_Ret val = SwiftCallbackFunc65(&SwiftCallbackFunc65Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)7810903219784151958), val.F0);
        Assert.Equal((nint)unchecked((nint)8310527878848492866), val.F1);
        Assert.Equal((nint)unchecked((nint)1357258266300958550), val.F2);
        Assert.Equal((float)5970506, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 5)]
    struct F66_Ret_S0
    {
        public float F0;
        public byte F1;

        public F66_Ret_S0(float f0, byte f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F66_Ret
    {
        public uint F0;
        public int F1;
        public uint F2;
        public F66_Ret_S0 F3;
        public nint F4;

        public F66_Ret(uint f0, int f1, uint f2, F66_Ret_S0 f3, nint f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func661fAA7F66_RetVAEs5Int64VXE_tF")]
    private static extern F66_Ret SwiftCallbackFunc66(delegate* unmanaged[Swift]<long, SwiftSelf, F66_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F66_Ret SwiftCallbackFunc66Callback(long a0, SwiftSelf self)
    {
        try
        {
            Assert.Equal((long)8300712022174991120, a0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F66_Ret(1855065799, 2029697750, 149423164, new F66_Ret_S0(4327716, 116), unchecked((nint)5847795120921557969));
    }

    [Fact]
    public static void TestSwiftCallbackFunc66()
    {
        Console.Write("Running SwiftCallbackFunc66: ");
        ExceptionDispatchInfo ex = null;
        F66_Ret val = SwiftCallbackFunc66(&SwiftCallbackFunc66Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((uint)1855065799, val.F0);
        Assert.Equal((int)2029697750, val.F1);
        Assert.Equal((uint)149423164, val.F2);
        Assert.Equal((float)4327716, val.F3.F0);
        Assert.Equal((byte)116, val.F3.F1);
        Assert.Equal((nint)unchecked((nint)5847795120921557969), val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F67_S0
    {
        public uint F0;
        public byte F1;
        public byte F2;
        public int F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F67_S1
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F67_S2_S0
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct F67_S2
    {
        public ulong F0;
        public uint F1;
        public nint F2;
        public uint F3;
        public F67_S2_S0 F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F67_S3
    {
        public short F0;
        public ulong F1;
        public ulong F2;
        public float F3;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func671fs5Int32VAESd_AA6F67_S0VSfAA0H3_S1Vs5Int16VSuAA0H3_S2Vs6UInt16VS2uAA0H3_S3Vs6UInt64VtXE_tF")]
    private static extern int SwiftCallbackFunc67(delegate* unmanaged[Swift]<double, F67_S0, float, F67_S1, short, nuint, F67_S2, ushort, nuint, nuint, F67_S3, ulong, SwiftSelf, int> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static int SwiftCallbackFunc67Callback(double a0, F67_S0 a1, float a2, F67_S1 a3, short a4, nuint a5, F67_S2 a6, ushort a7, nuint a8, nuint a9, F67_S3 a10, ulong a11, SwiftSelf self)
    {
        try
        {
            Assert.Equal((double)2365334314089079, a0);
            Assert.Equal((uint)1133369490, a1.F0);
            Assert.Equal((byte)54, a1.F1);
            Assert.Equal((byte)244, a1.F2);
            Assert.Equal((int)411611102, a1.F3);
            Assert.Equal((float)4453912, a2);
            Assert.Equal((uint)837821989, a3.F0);
            Assert.Equal((short)-3824, a4);
            Assert.Equal((nuint)unchecked((nuint)2394019088612006082), a5);
            Assert.Equal((ulong)2219661088889353540, a6.F0);
            Assert.Equal((uint)294254132, a6.F1);
            Assert.Equal((nint)unchecked((nint)5363897228951721947), a6.F2);
            Assert.Equal((uint)2038380379, a6.F3);
            Assert.Equal((nint)unchecked((nint)8364879421385869437), a6.F4.F0);
            Assert.Equal((ushort)27730, a7);
            Assert.Equal((nuint)unchecked((nuint)1854446871602777695), a8);
            Assert.Equal((nuint)unchecked((nuint)5020910156102352016), a9);
            Assert.Equal((short)-2211, a10.F0);
            Assert.Equal((ulong)5910581461792482729, a10.F1);
            Assert.Equal((ulong)9095210648679611609, a10.F2);
            Assert.Equal((float)6138428, a10.F3);
            Assert.Equal((ulong)4274242076331880276, a11);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 391983354;
    }

    [Fact]
    public static void TestSwiftCallbackFunc67()
    {
        Console.Write("Running SwiftCallbackFunc67: ");
        ExceptionDispatchInfo ex = null;
        int val = SwiftCallbackFunc67(&SwiftCallbackFunc67Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((int)391983354, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F68_S0_S0
    {
        public sbyte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct F68_S0
    {
        public long F0;
        public F68_S0_S0 F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F68_S1
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F68_S2_S0
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F68_S2_S1_S0
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F68_S2_S1
    {
        public F68_S2_S1_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F68_S2
    {
        public F68_S2_S0 F0;
        public F68_S2_S1 F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F68_S3
    {
        public short F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F68_Ret
    {
        public ushort F0;
        public long F1;

        public F68_Ret(ushort f0, long f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func681fAA7F68_RetVAEs5UInt8V_Sfs5Int32VSiAA0G3_S0Vs5Int16VSiAISiAA0G3_S1VSdAA0G3_S2VAA0G3_S3VtXE_tF")]
    private static extern F68_Ret SwiftCallbackFunc68(delegate* unmanaged[Swift]<byte, float, int, nint, F68_S0, short, nint, int, nint, F68_S1, double, F68_S2, F68_S3, SwiftSelf, F68_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F68_Ret SwiftCallbackFunc68Callback(byte a0, float a1, int a2, nint a3, F68_S0 a4, short a5, nint a6, int a7, nint a8, F68_S1 a9, double a10, F68_S2 a11, F68_S3 a12, SwiftSelf self)
    {
        try
        {
            Assert.Equal((byte)203, a0);
            Assert.Equal((float)7725681, a1);
            Assert.Equal((int)323096997, a2);
            Assert.Equal((nint)unchecked((nint)7745650233784541800), a3);
            Assert.Equal((long)4103074885750473230, a4.F0);
            Assert.Equal((sbyte)12, a4.F1.F0);
            Assert.Equal((short)28477, a5);
            Assert.Equal((nint)unchecked((nint)3772772447290536725), a6);
            Assert.Equal((int)1075348149, a7);
            Assert.Equal((nint)unchecked((nint)2017898311184593242), a8);
            Assert.Equal((ushort)60280, a9.F0);
            Assert.Equal((double)4052387873895590, a10);
            Assert.Equal((nuint)unchecked((nuint)1321857087602747558), a11.F0.F0);
            Assert.Equal((ulong)9011155097138053416, a11.F1.F0.F0);
            Assert.Equal((short)8332, a12.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F68_Ret(64088, 8144208533922264568);
    }

    [Fact]
    public static void TestSwiftCallbackFunc68()
    {
        Console.Write("Running SwiftCallbackFunc68: ");
        ExceptionDispatchInfo ex = null;
        F68_Ret val = SwiftCallbackFunc68(&SwiftCallbackFunc68Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)64088, val.F0);
        Assert.Equal((long)8144208533922264568, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F69_S0_S0
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F69_S0
    {
        public F69_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F69_S1
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F69_S2
    {
        public int F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F69_S3
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F69_S4_S0
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F69_S4
    {
        public F69_S4_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F69_Ret
    {
        public byte F0;
        public long F1;
        public uint F2;

        public F69_Ret(byte f0, long f1, uint f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func691fAA7F69_RetVAeA0G3_S0V_Sis5Int32VAA0G3_S1Vs6UInt32Vs4Int8VAA0G3_S2VSiAA0G3_S3VAA0G3_S4VtXE_tF")]
    private static extern F69_Ret SwiftCallbackFunc69(delegate* unmanaged[Swift]<F69_S0, nint, int, F69_S1, uint, sbyte, F69_S2, nint, F69_S3, F69_S4, SwiftSelf, F69_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F69_Ret SwiftCallbackFunc69Callback(F69_S0 a0, nint a1, int a2, F69_S1 a3, uint a4, sbyte a5, F69_S2 a6, nint a7, F69_S3 a8, F69_S4 a9, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ulong)7154553222175076145, a0.F0.F0);
            Assert.Equal((nint)unchecked((nint)6685908100026425691), a1);
            Assert.Equal((int)1166526155, a2);
            Assert.Equal((long)6042278185730963289, a3.F0);
            Assert.Equal((uint)182060391, a4);
            Assert.Equal((sbyte)45, a5);
            Assert.Equal((int)1886331345, a6.F0);
            Assert.Equal((nint)unchecked((nint)485542148877875333), a7);
            Assert.Equal((byte)209, a8.F0);
            Assert.Equal((long)6856847647688321191, a9.F0.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F69_Ret(52, 5510942427596951043, 1854355776);
    }

    [Fact]
    public static void TestSwiftCallbackFunc69()
    {
        Console.Write("Running SwiftCallbackFunc69: ");
        ExceptionDispatchInfo ex = null;
        F69_Ret val = SwiftCallbackFunc69(&SwiftCallbackFunc69Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((byte)52, val.F0);
        Assert.Equal((long)5510942427596951043, val.F1);
        Assert.Equal((uint)1854355776, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F70_S0
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F70_S1
    {
        public nint F0;
        public double F1;
        public short F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F70_S2
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 36)]
    struct F70_S3
    {
        public ushort F0;
        public double F1;
        public byte F2;
        public ulong F3;
        public int F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F70_S4_S0
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F70_S4
    {
        public F70_S4_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F70_Ret
    {
        public sbyte F0;
        public uint F1;
        public ulong F2;
        public short F3;
        public short F4;

        public F70_Ret(sbyte f0, uint f1, ulong f2, short f3, short f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func701fAA7F70_RetVAEs5Int16V_s5UInt8VSis6UInt32VAA0G3_S0Vs5Int32VAA0G3_S1VAA0G3_S2VAA0G3_S3Vs5Int64VAOs6UInt16VS2iSuAA0G3_S4VtXE_tF")]
    private static extern F70_Ret SwiftCallbackFunc70(delegate* unmanaged[Swift]<short, byte, nint, uint, F70_S0, int, F70_S1, F70_S2, F70_S3, long, int, ushort, nint, nint, nuint, F70_S4, SwiftSelf, F70_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F70_Ret SwiftCallbackFunc70Callback(short a0, byte a1, nint a2, uint a3, F70_S0 a4, int a5, F70_S1 a6, F70_S2 a7, F70_S3 a8, long a9, int a10, ushort a11, nint a12, nint a13, nuint a14, F70_S4 a15, SwiftSelf self)
    {
        try
        {
            Assert.Equal((short)-13167, a0);
            Assert.Equal((byte)126, a1);
            Assert.Equal((nint)unchecked((nint)3641983584484741827), a2);
            Assert.Equal((uint)1090448265, a3);
            Assert.Equal((long)3696858216713616004, a4.F0);
            Assert.Equal((int)1687025402, a5);
            Assert.Equal((nint)unchecked((nint)714916953527626038), a6.F0);
            Assert.Equal((double)459810445900614, a6.F1);
            Assert.Equal((short)4276, a6.F2);
            Assert.Equal((uint)529194028, a7.F0);
            Assert.Equal((ushort)40800, a8.F0);
            Assert.Equal((double)3934985905568056, a8.F1);
            Assert.Equal((byte)230, a8.F2);
            Assert.Equal((ulong)7358783417346157372, a8.F3);
            Assert.Equal((int)187926922, a8.F4);
            Assert.Equal((long)228428560763393434, a9);
            Assert.Equal((int)146501405, a10);
            Assert.Equal((ushort)58804, a11);
            Assert.Equal((nint)unchecked((nint)7098488973446286248), a12);
            Assert.Equal((nint)unchecked((nint)1283658442251334575), a13);
            Assert.Equal((nuint)unchecked((nuint)3644681944588099582), a14);
            Assert.Equal((nuint)unchecked((nuint)8197135412164695911), a15.F0.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F70_Ret(45, 460004173, 7766748067698372018, 27369, 16509);
    }

    [Fact]
    public static void TestSwiftCallbackFunc70()
    {
        Console.Write("Running SwiftCallbackFunc70: ");
        ExceptionDispatchInfo ex = null;
        F70_Ret val = SwiftCallbackFunc70(&SwiftCallbackFunc70Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((sbyte)45, val.F0);
        Assert.Equal((uint)460004173, val.F1);
        Assert.Equal((ulong)7766748067698372018, val.F2);
        Assert.Equal((short)27369, val.F3);
        Assert.Equal((short)16509, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F71_S0_S0
    {
        public int F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F71_S0
    {
        public F71_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F71_S1
    {
        public long F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func711fs6UInt64VAeA6F71_S0V_AA0H3_S1VtXE_tF")]
    private static extern ulong SwiftCallbackFunc71(delegate* unmanaged[Swift]<F71_S0, F71_S1, SwiftSelf, ulong> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static ulong SwiftCallbackFunc71Callback(F71_S0 a0, F71_S1 a1, SwiftSelf self)
    {
        try
        {
            Assert.Equal((int)258165353, a0.F0.F0);
            Assert.Equal((long)8603744544763953916, a1.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 8460721064583106347;
    }

    [Fact]
    public static void TestSwiftCallbackFunc71()
    {
        Console.Write("Running SwiftCallbackFunc71: ");
        ExceptionDispatchInfo ex = null;
        ulong val = SwiftCallbackFunc71(&SwiftCallbackFunc71Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)8460721064583106347, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F72_S0
    {
        public int F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F72_Ret
    {
        public uint F0;
        public float F1;
        public float F2;
        public long F3;

        public F72_Ret(uint f0, float f1, float f2, long f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func721fAA7F72_RetVAeA0G3_S0V_s5Int64Vs4Int8VtXE_tF")]
    private static extern F72_Ret SwiftCallbackFunc72(delegate* unmanaged[Swift]<F72_S0, long, sbyte, SwiftSelf, F72_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F72_Ret SwiftCallbackFunc72Callback(F72_S0 a0, long a1, sbyte a2, SwiftSelf self)
    {
        try
        {
            Assert.Equal((int)2021509367, a0.F0);
            Assert.Equal((long)2480039820482100351, a1);
            Assert.Equal((sbyte)91, a2);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F72_Ret(1583929847, 2026234, 8092211, 445254465286132488);
    }

    [Fact]
    public static void TestSwiftCallbackFunc72()
    {
        Console.Write("Running SwiftCallbackFunc72: ");
        ExceptionDispatchInfo ex = null;
        F72_Ret val = SwiftCallbackFunc72(&SwiftCallbackFunc72Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((uint)1583929847, val.F0);
        Assert.Equal((float)2026234, val.F1);
        Assert.Equal((float)8092211, val.F2);
        Assert.Equal((long)445254465286132488, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F73_S0
    {
        public int F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F73_S1_S0
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F73_S1
    {
        public F73_S1_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F73_S2
    {
        public int F0;
        public float F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 11)]
    struct F73_S3
    {
        public nuint F0;
        public short F1;
        public sbyte F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F73_S4
    {
        public short F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F73_S5
    {
        public uint F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func731fs4Int8VAESd_SfAA6F73_S0Vs5Int64VAA0H3_S1VAA0H3_S2Vs5Int16VSdAEs5Int32VAiA0H3_S3VSus6UInt64VAqA0H3_S4Vs5UInt8VAA0H3_S5VtXE_tF")]
    private static extern sbyte SwiftCallbackFunc73(delegate* unmanaged[Swift]<double, float, F73_S0, long, F73_S1, F73_S2, short, double, sbyte, int, long, F73_S3, nuint, ulong, int, F73_S4, byte, F73_S5, SwiftSelf, sbyte> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static sbyte SwiftCallbackFunc73Callback(double a0, float a1, F73_S0 a2, long a3, F73_S1 a4, F73_S2 a5, short a6, double a7, sbyte a8, int a9, long a10, F73_S3 a11, nuint a12, ulong a13, int a14, F73_S4 a15, byte a16, F73_S5 a17, SwiftSelf self)
    {
        try
        {
            Assert.Equal((double)3038361048801008, a0);
            Assert.Equal((float)7870661, a1);
            Assert.Equal((int)1555231180, a2.F0);
            Assert.Equal((long)7433951069104961, a3);
            Assert.Equal((ushort)63298, a4.F0.F0);
            Assert.Equal((int)1759846580, a5.F0);
            Assert.Equal((float)1335901, a5.F1);
            Assert.Equal((short)11514, a6);
            Assert.Equal((double)695278874601974, a7);
            Assert.Equal((sbyte)108, a8);
            Assert.Equal((int)48660527, a9);
            Assert.Equal((long)7762050749172332624, a10);
            Assert.Equal((nuint)unchecked((nuint)7486686356276472663), a11.F0);
            Assert.Equal((short)11622, a11.F1);
            Assert.Equal((sbyte)112, a11.F2);
            Assert.Equal((nuint)unchecked((nuint)884183974530885885), a12);
            Assert.Equal((ulong)7434462110419085390, a13);
            Assert.Equal((int)170242607, a14);
            Assert.Equal((short)-26039, a15.F0);
            Assert.Equal((byte)41, a16);
            Assert.Equal((uint)191302504, a17.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 76;
    }

    [Fact]
    public static void TestSwiftCallbackFunc73()
    {
        Console.Write("Running SwiftCallbackFunc73: ");
        ExceptionDispatchInfo ex = null;
        sbyte val = SwiftCallbackFunc73(&SwiftCallbackFunc73Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((sbyte)76, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 17)]
    struct F74_S0_S0
    {
        public ushort F0;
        public nuint F1;
        public sbyte F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F74_S0
    {
        public F74_S0_S0 F0;
        public nint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F74_S1
    {
        public float F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func741fs5Int64VAeA6F74_S0V_AA0H3_S1Vs5Int16VtXE_tF")]
    private static extern long SwiftCallbackFunc74(delegate* unmanaged[Swift]<F74_S0, F74_S1, short, SwiftSelf, long> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static long SwiftCallbackFunc74Callback(F74_S0 a0, F74_S1 a1, short a2, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ushort)59883, a0.F0.F0);
            Assert.Equal((nuint)unchecked((nuint)5554216411943233256), a0.F0.F1);
            Assert.Equal((sbyte)126, a0.F0.F2);
            Assert.Equal((nint)unchecked((nint)724541378819571203), a0.F1);
            Assert.Equal((float)172601, a1.F0);
            Assert.Equal((short)27932, a2);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 7382123574052120438;
    }

    [Fact]
    public static void TestSwiftCallbackFunc74()
    {
        Console.Write("Running SwiftCallbackFunc74: ");
        ExceptionDispatchInfo ex = null;
        long val = SwiftCallbackFunc74(&SwiftCallbackFunc74Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((long)7382123574052120438, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F75_S0
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F75_S1_S0
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F75_S1
    {
        public F75_S1_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F75_S2
    {
        public sbyte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F75_S3_S0
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F75_S3
    {
        public F75_S3_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 36)]
    struct F75_Ret
    {
        public byte F0;
        public double F1;
        public double F2;
        public long F3;
        public uint F4;

        public F75_Ret(byte f0, double f1, double f2, long f3, uint f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func751fAA7F75_RetVAEs4Int8V_A2gA0G3_S0VAA0G3_S1VAA0G3_S2VAA0G3_S3VtXE_tF")]
    private static extern F75_Ret SwiftCallbackFunc75(delegate* unmanaged[Swift]<sbyte, sbyte, sbyte, F75_S0, F75_S1, F75_S2, F75_S3, SwiftSelf, F75_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F75_Ret SwiftCallbackFunc75Callback(sbyte a0, sbyte a1, sbyte a2, F75_S0 a3, F75_S1 a4, F75_S2 a5, F75_S3 a6, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)-105, a0);
            Assert.Equal((sbyte)71, a1);
            Assert.Equal((sbyte)108, a2);
            Assert.Equal((long)7224638108479292438, a3.F0);
            Assert.Equal((byte)126, a4.F0.F0);
            Assert.Equal((sbyte)-88, a5.F0);
            Assert.Equal((ushort)4934, a6.F0.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F75_Ret(8, 494440474432982, 3322048351205313, 7525253715666045341, 7365589);
    }

    [Fact]
    public static void TestSwiftCallbackFunc75()
    {
        Console.Write("Running SwiftCallbackFunc75: ");
        ExceptionDispatchInfo ex = null;
        F75_Ret val = SwiftCallbackFunc75(&SwiftCallbackFunc75Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((byte)8, val.F0);
        Assert.Equal((double)494440474432982, val.F1);
        Assert.Equal((double)3322048351205313, val.F2);
        Assert.Equal((long)7525253715666045341, val.F3);
        Assert.Equal((uint)7365589, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F76_S0
    {
        public ushort F0;
        public nint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F76_S1_S0
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F76_S1
    {
        public F76_S1_S0 F0;
        public nuint F1;
        public double F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F76_S2
    {
        public ulong F0;
        public nint F1;
        public ushort F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F76_S3_S0
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F76_S3
    {
        public F76_S3_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F76_S4
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F76_S5
    {
        public nuint F0;
        public double F1;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func761fs6UInt64VAEs5UInt8V_AA6F76_S0Vs4Int8VAA0I3_S1VAA0I3_S2VAA0I3_S3Vs6UInt32VAA0I3_S4VAgA0I3_S5VSds5Int16VtXE_tF")]
    private static extern ulong SwiftCallbackFunc76(delegate* unmanaged[Swift]<byte, F76_S0, sbyte, F76_S1, F76_S2, F76_S3, uint, F76_S4, byte, F76_S5, double, short, SwiftSelf, ulong> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static ulong SwiftCallbackFunc76Callback(byte a0, F76_S0 a1, sbyte a2, F76_S1 a3, F76_S2 a4, F76_S3 a5, uint a6, F76_S4 a7, byte a8, F76_S5 a9, double a10, short a11, SwiftSelf self)
    {
        try
        {
            Assert.Equal((byte)69, a0);
            Assert.Equal((ushort)25503, a1.F0);
            Assert.Equal((nint)unchecked((nint)4872234474620951743), a1.F1);
            Assert.Equal((sbyte)43, a2);
            Assert.Equal((nint)unchecked((nint)1199076663426903579), a3.F0.F0);
            Assert.Equal((nuint)unchecked((nuint)4639522222462236688), a3.F1);
            Assert.Equal((double)4082956091930029, a3.F2);
            Assert.Equal((ulong)5171821618947987626, a4.F0);
            Assert.Equal((nint)unchecked((nint)3369410144919558564), a4.F1);
            Assert.Equal((ushort)5287, a4.F2);
            Assert.Equal((long)929854460912895550, a5.F0.F0);
            Assert.Equal((uint)1208311201, a6);
            Assert.Equal((long)7033993025788649145, a7.F0);
            Assert.Equal((byte)58, a8);
            Assert.Equal((nuint)unchecked((nuint)1401399014740601512), a9.F0);
            Assert.Equal((double)2523645319232571, a9.F1);
            Assert.Equal((double)230232835550369, a10);
            Assert.Equal((short)-22975, a11);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 2608582352406315143;
    }

    [Fact]
    public static void TestSwiftCallbackFunc76()
    {
        Console.Write("Running SwiftCallbackFunc76: ");
        ExceptionDispatchInfo ex = null;
        ulong val = SwiftCallbackFunc76(&SwiftCallbackFunc76Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)2608582352406315143, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F77_S0
    {
        public long F0;
        public double F1;
        public nuint F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F77_S1
    {
        public short F0;
        public float F1;
        public float F2;
        public long F3;
        public long F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F77_S2
    {
        public ushort F0;
        public sbyte F1;
        public int F2;
        public float F3;
        public float F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F77_Ret
    {
        public double F0;
        public ushort F1;
        public sbyte F2;
        public nuint F3;

        public F77_Ret(double f0, ushort f1, sbyte f2, nuint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func771fAA7F77_RetVAESd_AA0G3_S0VAA0G3_S1VAA0G3_S2Vs6UInt32VtXE_tF")]
    private static extern F77_Ret SwiftCallbackFunc77(delegate* unmanaged[Swift]<double, F77_S0, F77_S1, F77_S2, uint, SwiftSelf, F77_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F77_Ret SwiftCallbackFunc77Callback(double a0, F77_S0 a1, F77_S1 a2, F77_S2 a3, uint a4, SwiftSelf self)
    {
        try
        {
            Assert.Equal((double)1623173949127682, a0);
            Assert.Equal((long)5204451347781433070, a1.F0);
            Assert.Equal((double)3469485630755805, a1.F1);
            Assert.Equal((nuint)unchecked((nuint)7586276835848725004), a1.F2);
            Assert.Equal((short)2405, a2.F0);
            Assert.Equal((float)2419792, a2.F1);
            Assert.Equal((float)6769317, a2.F2);
            Assert.Equal((long)1542327522833750776, a2.F3);
            Assert.Equal((long)1297586130846695275, a2.F4);
            Assert.Equal((ushort)10102, a3.F0);
            Assert.Equal((sbyte)-48, a3.F1);
            Assert.Equal((int)14517107, a3.F2);
            Assert.Equal((float)4856023, a3.F3);
            Assert.Equal((float)2681358, a3.F4);
            Assert.Equal((uint)1463251524, a4);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F77_Ret(1601613740657843, 14373, -17, unchecked((nuint)274065318894652498));
    }

    [Fact]
    public static void TestSwiftCallbackFunc77()
    {
        Console.Write("Running SwiftCallbackFunc77: ");
        ExceptionDispatchInfo ex = null;
        F77_Ret val = SwiftCallbackFunc77(&SwiftCallbackFunc77Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((double)1601613740657843, val.F0);
        Assert.Equal((ushort)14373, val.F1);
        Assert.Equal((sbyte)-17, val.F2);
        Assert.Equal((nuint)unchecked((nuint)274065318894652498), val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F78_S0
    {
        public nuint F0;
        public nint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F78_S1_S0
    {
        public sbyte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F78_S1
    {
        public short F0;
        public ulong F1;
        public F78_S1_S0 F2;
        public int F3;
        public nint F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F78_S2
    {
        public nuint F0;
        public ulong F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F78_S3
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F78_S4
    {
        public ulong F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func781fS2ds6UInt64V_AA6F78_S0VAeA0H3_S1VAA0H3_S2Vs5Int32VAEs5Int64VAA0H3_S3VS2fs6UInt16VAA0H3_S4VSdtXE_tF")]
    private static extern double SwiftCallbackFunc78(delegate* unmanaged[Swift]<ulong, F78_S0, ulong, F78_S1, F78_S2, int, ulong, long, F78_S3, float, float, ushort, F78_S4, double, SwiftSelf, double> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static double SwiftCallbackFunc78Callback(ulong a0, F78_S0 a1, ulong a2, F78_S1 a3, F78_S2 a4, int a5, ulong a6, long a7, F78_S3 a8, float a9, float a10, ushort a11, F78_S4 a12, double a13, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ulong)6780767594736146373, a0);
            Assert.Equal((nuint)unchecked((nuint)6264193481541646332), a1.F0);
            Assert.Equal((nint)unchecked((nint)6600856439035088503), a1.F1);
            Assert.Equal((ulong)1968254881389492170, a2);
            Assert.Equal((short)-17873, a3.F0);
            Assert.Equal((ulong)5581169895682201971, a3.F1);
            Assert.Equal((sbyte)127, a3.F2.F0);
            Assert.Equal((int)1942346704, a3.F3);
            Assert.Equal((nint)unchecked((nint)118658265323815307), a3.F4);
            Assert.Equal((nuint)unchecked((nuint)1489326778640378879), a4.F0);
            Assert.Equal((ulong)1427061853707270770, a4.F1);
            Assert.Equal((int)858391966, a5);
            Assert.Equal((ulong)5830110056171302270, a6);
            Assert.Equal((long)2953614358173898788, a7);
            Assert.Equal((ulong)6761452244699684409, a8.F0);
            Assert.Equal((float)3452451, a9);
            Assert.Equal((float)3507119, a10);
            Assert.Equal((ushort)40036, a11);
            Assert.Equal((ulong)4800085294404376817, a12.F0);
            Assert.Equal((double)780368756754436, a13);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 1088544646657969;
    }

    [Fact]
    public static void TestSwiftCallbackFunc78()
    {
        Console.Write("Running SwiftCallbackFunc78: ");
        ExceptionDispatchInfo ex = null;
        double val = SwiftCallbackFunc78(&SwiftCallbackFunc78Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((double)1088544646657969, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F79_S0_S0
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F79_S0
    {
        public F79_S0_S0 F0;
        public nint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F79_Ret
    {
        public uint F0;
        public ulong F1;
        public double F2;

        public F79_Ret(uint f0, ulong f1, double f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func791fAA7F79_RetVAeA0G3_S0V_SftXE_tF")]
    private static extern F79_Ret SwiftCallbackFunc79(delegate* unmanaged[Swift]<F79_S0, float, SwiftSelf, F79_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F79_Ret SwiftCallbackFunc79Callback(F79_S0 a0, float a1, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nuint)unchecked((nuint)1013911700897046117), a0.F0.F0);
            Assert.Equal((nint)unchecked((nint)7323935615297665289), a0.F1);
            Assert.Equal((float)5159506, a1);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F79_Ret(895629788, 4824209192377460356, 2599150646028906);
    }

    [Fact]
    public static void TestSwiftCallbackFunc79()
    {
        Console.Write("Running SwiftCallbackFunc79: ");
        ExceptionDispatchInfo ex = null;
        F79_Ret val = SwiftCallbackFunc79(&SwiftCallbackFunc79Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((uint)895629788, val.F0);
        Assert.Equal((ulong)4824209192377460356, val.F1);
        Assert.Equal((double)2599150646028906, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F80_S0
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F80_S1_S0_S0
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F80_S1_S0
    {
        public F80_S1_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct F80_S1
    {
        public nint F0;
        public F80_S1_S0 F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F80_S2
    {
        public ulong F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func801fS2fs6UInt64V_Sis5Int32Vs5Int16VSuAA6F80_S0VAISis4Int8VAGs6UInt32VAA0J3_S1VAA0J3_S2VAEtXE_tF")]
    private static extern float SwiftCallbackFunc80(delegate* unmanaged[Swift]<ulong, nint, int, short, nuint, F80_S0, short, nint, sbyte, int, uint, F80_S1, F80_S2, ulong, SwiftSelf, float> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static float SwiftCallbackFunc80Callback(ulong a0, nint a1, int a2, short a3, nuint a4, F80_S0 a5, short a6, nint a7, sbyte a8, int a9, uint a10, F80_S1 a11, F80_S2 a12, ulong a13, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ulong)4470427843910624516, a0);
            Assert.Equal((nint)unchecked((nint)8383677749057878551), a1);
            Assert.Equal((int)2017117925, a2);
            Assert.Equal((short)-10531, a3);
            Assert.Equal((nuint)unchecked((nuint)3438375001906177611), a4);
            Assert.Equal((ushort)65220, a5.F0);
            Assert.Equal((short)7107, a6);
            Assert.Equal((nint)unchecked((nint)7315288835693680178), a7);
            Assert.Equal((sbyte)-48, a8);
            Assert.Equal((int)813870434, a9);
            Assert.Equal((uint)1092037477, a10);
            Assert.Equal((nint)unchecked((nint)7104962838387954470), a11.F0);
            Assert.Equal((byte)236, a11.F1.F0.F0);
            Assert.Equal((ulong)7460392384225808790, a12.F0);
            Assert.Equal((ulong)364121728483540667, a13);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 5169959;
    }

    [Fact]
    public static void TestSwiftCallbackFunc80()
    {
        Console.Write("Running SwiftCallbackFunc80: ");
        ExceptionDispatchInfo ex = null;
        float val = SwiftCallbackFunc80(&SwiftCallbackFunc80Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((float)5169959, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F81_S0
    {
        public float F0;
        public float F1;
        public nint F2;
        public nint F3;
        public nint F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F81_Ret
    {
        public nint F0;

        public F81_Ret(nint f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func811fAA7F81_RetVAEs5UInt8V_s6UInt32VAgA0G3_S0Vs4Int8VtXE_tF")]
    private static extern F81_Ret SwiftCallbackFunc81(delegate* unmanaged[Swift]<byte, uint, byte, F81_S0, sbyte, SwiftSelf, F81_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F81_Ret SwiftCallbackFunc81Callback(byte a0, uint a1, byte a2, F81_S0 a3, sbyte a4, SwiftSelf self)
    {
        try
        {
            Assert.Equal((byte)53, a0);
            Assert.Equal((uint)57591489, a1);
            Assert.Equal((byte)19, a2);
            Assert.Equal((float)5675845, a3.F0);
            Assert.Equal((float)6469988, a3.F1);
            Assert.Equal((nint)unchecked((nint)5775316279348621124), a3.F2);
            Assert.Equal((nint)unchecked((nint)7699091894067057939), a3.F3);
            Assert.Equal((nint)unchecked((nint)1049086627558950131), a3.F4);
            Assert.Equal((sbyte)15, a4);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F81_Ret(unchecked((nint)1055606720535823947));
    }

    [Fact]
    public static void TestSwiftCallbackFunc81()
    {
        Console.Write("Running SwiftCallbackFunc81: ");
        ExceptionDispatchInfo ex = null;
        F81_Ret val = SwiftCallbackFunc81(&SwiftCallbackFunc81Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)1055606720535823947), val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F82_S0_S0
    {
        public float F0;
        public nuint F1;
        public ushort F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F82_S0
    {
        public nuint F0;
        public F82_S0_S0 F1;
        public ushort F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F82_S1
    {
        public int F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F82_S2
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F82_S3_S0
    {
        public int F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F82_S3
    {
        public double F0;
        public nuint F1;
        public F82_S3_S0 F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F82_S4
    {
        public ulong F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func821fS2fs5Int64V_AA6F82_S0Vs5Int16Vs4Int8Vs6UInt32VAA0H3_S1Vs5Int32VAeKSdAA0H3_S2VAA0H3_S3VAA0H3_S4VtXE_tF")]
    private static extern float SwiftCallbackFunc82(delegate* unmanaged[Swift]<long, F82_S0, short, sbyte, uint, F82_S1, int, long, sbyte, double, F82_S2, F82_S3, F82_S4, SwiftSelf, float> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static float SwiftCallbackFunc82Callback(long a0, F82_S0 a1, short a2, sbyte a3, uint a4, F82_S1 a5, int a6, long a7, sbyte a8, double a9, F82_S2 a10, F82_S3 a11, F82_S4 a12, SwiftSelf self)
    {
        try
        {
            Assert.Equal((long)6454754584537364459, a0);
            Assert.Equal((nuint)unchecked((nuint)6703634779264968131), a1.F0);
            Assert.Equal((float)1010059, a1.F1.F0);
            Assert.Equal((nuint)unchecked((nuint)4772968591609202284), a1.F1.F1);
            Assert.Equal((ushort)64552, a1.F1.F2);
            Assert.Equal((ushort)47126, a1.F2);
            Assert.Equal((short)9869, a2);
            Assert.Equal((sbyte)-8, a3);
            Assert.Equal((uint)1741550381, a4);
            Assert.Equal((int)705741282, a5.F0);
            Assert.Equal((int)1998781399, a6);
            Assert.Equal((long)7787961471254401526, a7);
            Assert.Equal((sbyte)-27, a8);
            Assert.Equal((double)4429830670351707, a9);
            Assert.Equal((nint)unchecked((nint)4975772762589349422), a10.F0);
            Assert.Equal((double)1423948098664774, a11.F0);
            Assert.Equal((nuint)unchecked((nuint)504607538824251986), a11.F1);
            Assert.Equal((int)1940911018, a11.F2.F0);
            Assert.Equal((ulong)2988623645681463667, a12.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 7514083;
    }

    [Fact]
    public static void TestSwiftCallbackFunc82()
    {
        Console.Write("Running SwiftCallbackFunc82: ");
        ExceptionDispatchInfo ex = null;
        float val = SwiftCallbackFunc82(&SwiftCallbackFunc82Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((float)7514083, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F83_S0
    {
        public int F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F83_Ret
    {
        public short F0;

        public F83_Ret(short f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func831fAA7F83_RetVAEs4Int8V_AA0G3_S0Vs5Int16VtXE_tF")]
    private static extern F83_Ret SwiftCallbackFunc83(delegate* unmanaged[Swift]<sbyte, F83_S0, short, SwiftSelf, F83_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F83_Ret SwiftCallbackFunc83Callback(sbyte a0, F83_S0 a1, short a2, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)17, a0);
            Assert.Equal((int)530755056, a1.F0);
            Assert.Equal((short)-11465, a2);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F83_Ret(-32475);
    }

    [Fact]
    public static void TestSwiftCallbackFunc83()
    {
        Console.Write("Running SwiftCallbackFunc83: ");
        ExceptionDispatchInfo ex = null;
        F83_Ret val = SwiftCallbackFunc83(&SwiftCallbackFunc83Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((short)-32475, val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 36)]
    struct F84_S0
    {
        public nuint F0;
        public uint F1;
        public nuint F2;
        public ulong F3;
        public int F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F84_S1
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F84_S2
    {
        public float F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F84_S3
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F84_S4
    {
        public short F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F84_S5
    {
        public nint F0;
        public short F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F84_S6
    {
        public short F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F84_S7
    {
        public int F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func841fS2is5Int32V_AA6F84_S0VAA0H3_S1VSdAEs5Int16VSdAA0H3_S2VAA0H3_S3VSdAA0H3_S4VAA0H3_S5VAA0H3_S6VAA0H3_S7VSutXE_tF")]
    private static extern nint SwiftCallbackFunc84(delegate* unmanaged[Swift]<int, F84_S0, F84_S1, double, int, short, double, F84_S2, F84_S3, double, F84_S4, F84_S5, F84_S6, F84_S7, nuint, SwiftSelf, nint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static nint SwiftCallbackFunc84Callback(int a0, F84_S0 a1, F84_S1 a2, double a3, int a4, short a5, double a6, F84_S2 a7, F84_S3 a8, double a9, F84_S4 a10, F84_S5 a11, F84_S6 a12, F84_S7 a13, nuint a14, SwiftSelf self)
    {
        try
        {
            Assert.Equal((int)1605022009, a0);
            Assert.Equal((nuint)unchecked((nuint)6165049220831866664), a1.F0);
            Assert.Equal((uint)1235491183, a1.F1);
            Assert.Equal((nuint)unchecked((nuint)7926620970405586826), a1.F2);
            Assert.Equal((ulong)2633248816907294140, a1.F3);
            Assert.Equal((int)2012834055, a1.F4);
            Assert.Equal((nuint)unchecked((nuint)2881830362339122988), a2.F0);
            Assert.Equal((double)4065309434963087, a3);
            Assert.Equal((int)1125165825, a4);
            Assert.Equal((short)-32360, a5);
            Assert.Equal((double)1145602045200029, a6);
            Assert.Equal((float)5655563, a7.F0);
            Assert.Equal((byte)14, a8.F0);
            Assert.Equal((double)3919593995303128, a9);
            Assert.Equal((short)26090, a10.F0);
            Assert.Equal((nint)unchecked((nint)8584898862398781737), a11.F0);
            Assert.Equal((short)-5185, a11.F1);
            Assert.Equal((short)144, a12.F0);
            Assert.Equal((int)2138004352, a13.F0);
            Assert.Equal((nuint)unchecked((nuint)9102562043027810686), a14);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return unchecked((nint)2320162198211027422);
    }

    [Fact]
    public static void TestSwiftCallbackFunc84()
    {
        Console.Write("Running SwiftCallbackFunc84: ");
        ExceptionDispatchInfo ex = null;
        nint val = SwiftCallbackFunc84(&SwiftCallbackFunc84Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)2320162198211027422), val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F85_S0
    {
        public double F0;
        public double F1;
        public sbyte F2;
        public int F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F85_S1
    {
        public long F0;
        public ushort F1;
        public ulong F2;
        public nuint F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F85_S2
    {
        public float F0;
        public float F1;
        public uint F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F85_S3
    {
        public byte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F85_S4
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F85_S5
    {
        public double F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    struct F85_Ret
    {
        public uint F0;
        public ushort F1;
        public int F2;
        public double F3;
        public nint F4;
        public ulong F5;
        public long F6;

        public F85_Ret(uint f0, ushort f1, int f2, double f3, nint f4, ulong f5, long f6)
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
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func851fAA7F85_RetVAeA0G3_S0V_AA0G3_S1Vs6UInt32VAA0G3_S2Vs5Int64VAA0G3_S3VAoA0G3_S4Vs6UInt16Vs5UInt8Vs5Int32VAkYSfAA0G3_S5VAOtXE_tF")]
    private static extern F85_Ret SwiftCallbackFunc85(delegate* unmanaged[Swift]<F85_S0, F85_S1, uint, F85_S2, long, F85_S3, long, F85_S4, ushort, byte, int, uint, int, float, F85_S5, long, SwiftSelf, F85_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F85_Ret SwiftCallbackFunc85Callback(F85_S0 a0, F85_S1 a1, uint a2, F85_S2 a3, long a4, F85_S3 a5, long a6, F85_S4 a7, ushort a8, byte a9, int a10, uint a11, int a12, float a13, F85_S5 a14, long a15, SwiftSelf self)
    {
        try
        {
            Assert.Equal((double)4325646965362202, a0.F0);
            Assert.Equal((double)3313084380250914, a0.F1);
            Assert.Equal((sbyte)42, a0.F2);
            Assert.Equal((int)2034100272, a0.F3);
            Assert.Equal((long)1365643665271339575, a1.F0);
            Assert.Equal((ushort)25442, a1.F1);
            Assert.Equal((ulong)3699631470459352980, a1.F2);
            Assert.Equal((nuint)unchecked((nuint)7611776251925132200), a1.F3);
            Assert.Equal((uint)911446742, a2);
            Assert.Equal((float)352423, a3.F0);
            Assert.Equal((float)7150341, a3.F1);
            Assert.Equal((uint)2090089360, a3.F2);
            Assert.Equal((long)5731257538910387688, a4);
            Assert.Equal((byte)171, a5.F0);
            Assert.Equal((long)5742887585483060342, a6);
            Assert.Equal((nuint)unchecked((nuint)1182236975680416316), a7.F0);
            Assert.Equal((ushort)32137, a8);
            Assert.Equal((byte)44, a9);
            Assert.Equal((int)2143531010, a10);
            Assert.Equal((uint)1271996557, a11);
            Assert.Equal((int)1035188446, a12);
            Assert.Equal((float)1925443, a13);
            Assert.Equal((double)2591574394337603, a14.F0);
            Assert.Equal((long)721102428782331317, a15);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F85_Ret(1768798158, 27348, 1836190158, 2058478254572549, unchecked((nint)7881716796049851507), 5099946246805224241, 1499623158991084417);
    }

    [Fact]
    public static void TestSwiftCallbackFunc85()
    {
        Console.Write("Running SwiftCallbackFunc85: ");
        ExceptionDispatchInfo ex = null;
        F85_Ret val = SwiftCallbackFunc85(&SwiftCallbackFunc85Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((uint)1768798158, val.F0);
        Assert.Equal((ushort)27348, val.F1);
        Assert.Equal((int)1836190158, val.F2);
        Assert.Equal((double)2058478254572549, val.F3);
        Assert.Equal((nint)unchecked((nint)7881716796049851507), val.F4);
        Assert.Equal((ulong)5099946246805224241, val.F5);
        Assert.Equal((long)1499623158991084417, val.F6);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 15)]
    struct F86_S0
    {
        public nint F0;
        public float F1;
        public short F2;
        public sbyte F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F86_S1
    {
        public double F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F86_S2
    {
        public nint F0;
        public float F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F86_S3
    {
        public ushort F0;
        public float F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 17)]
    struct F86_Ret
    {
        public short F0;
        public uint F1;
        public double F2;
        public byte F3;

        public F86_Ret(short f0, uint f1, double f2, byte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func861fAA7F86_RetVAESf_s5Int16VSiAGSfAA0G3_S0VAA0G3_S1VAA0G3_S2VSis6UInt32VS2uSfs5Int64VAA0G3_S3VSutXE_tF")]
    private static extern F86_Ret SwiftCallbackFunc86(delegate* unmanaged[Swift]<float, short, nint, short, float, F86_S0, F86_S1, F86_S2, nint, uint, nuint, nuint, float, long, F86_S3, nuint, SwiftSelf, F86_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F86_Ret SwiftCallbackFunc86Callback(float a0, short a1, nint a2, short a3, float a4, F86_S0 a5, F86_S1 a6, F86_S2 a7, nint a8, uint a9, nuint a10, nuint a11, float a12, long a13, F86_S3 a14, nuint a15, SwiftSelf self)
    {
        try
        {
            Assert.Equal((float)2913632, a0);
            Assert.Equal((short)3735, a1);
            Assert.Equal((nint)unchecked((nint)2773655476379499086), a2);
            Assert.Equal((short)22973, a3);
            Assert.Equal((float)8292778, a4);
            Assert.Equal((nint)unchecked((nint)5562042565258891920), a5.F0);
            Assert.Equal((float)8370233, a5.F1);
            Assert.Equal((short)18292, a5.F2);
            Assert.Equal((sbyte)-32, a5.F3);
            Assert.Equal((double)486951152980016, a6.F0);
            Assert.Equal((nint)unchecked((nint)170033426151098456), a7.F0);
            Assert.Equal((float)3867810, a7.F1);
            Assert.Equal((nint)unchecked((nint)7390780928011218856), a8);
            Assert.Equal((uint)1504267943, a9);
            Assert.Equal((nuint)unchecked((nuint)2046987193814931100), a10);
            Assert.Equal((nuint)unchecked((nuint)4860202472307588968), a11);
            Assert.Equal((float)1644019, a12);
            Assert.Equal((long)8084012412562897328, a13);
            Assert.Equal((ushort)46301, a14.F0);
            Assert.Equal((float)5633701, a14.F1);
            Assert.Equal((nuint)unchecked((nuint)1911608136082175332), a15);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F86_Ret(23398, 842205070, 544883763911905, 215);
    }

    [Fact]
    public static void TestSwiftCallbackFunc86()
    {
        Console.Write("Running SwiftCallbackFunc86: ");
        ExceptionDispatchInfo ex = null;
        F86_Ret val = SwiftCallbackFunc86(&SwiftCallbackFunc86Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((short)23398, val.F0);
        Assert.Equal((uint)842205070, val.F1);
        Assert.Equal((double)544883763911905, val.F2);
        Assert.Equal((byte)215, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F87_S0
    {
        public int F0;
        public short F1;
        public int F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F87_S1
    {
        public float F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func871fs6UInt64VAESf_SiAA6F87_S0VAA0H3_S1VtXE_tF")]
    private static extern ulong SwiftCallbackFunc87(delegate* unmanaged[Swift]<float, nint, F87_S0, F87_S1, SwiftSelf, ulong> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static ulong SwiftCallbackFunc87Callback(float a0, nint a1, F87_S0 a2, F87_S1 a3, SwiftSelf self)
    {
        try
        {
            Assert.Equal((float)1413086, a0);
            Assert.Equal((nint)unchecked((nint)4206825694012787823), a1);
            Assert.Equal((int)70240457, a2.F0);
            Assert.Equal((short)30503, a2.F1);
            Assert.Equal((int)671751848, a2.F2);
            Assert.Equal((float)6641304, a3.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 7817329728997505478;
    }

    [Fact]
    public static void TestSwiftCallbackFunc87()
    {
        Console.Write("Running SwiftCallbackFunc87: ");
        ExceptionDispatchInfo ex = null;
        ulong val = SwiftCallbackFunc87(&SwiftCallbackFunc87Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)7817329728997505478, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F88_S0
    {
        public sbyte F0;
        public short F1;
        public byte F2;
        public double F3;
        public ushort F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 9)]
    struct F88_S1
    {
        public double F0;
        public byte F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F88_S2
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F88_S3
    {
        public sbyte F0;
        public uint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F88_Ret
    {
        public int F0;
        public uint F1;
        public nint F2;
        public ulong F3;

        public F88_Ret(int f0, uint f1, nint f2, ulong f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func881fAA7F88_RetVAeA0G3_S0V_AA0G3_S1VSfSuSfSiAA0G3_S2Vs6UInt64VAA0G3_S3VAMtXE_tF")]
    private static extern F88_Ret SwiftCallbackFunc88(delegate* unmanaged[Swift]<F88_S0, F88_S1, float, nuint, float, nint, F88_S2, ulong, F88_S3, ulong, SwiftSelf, F88_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F88_Ret SwiftCallbackFunc88Callback(F88_S0 a0, F88_S1 a1, float a2, nuint a3, float a4, nint a5, F88_S2 a6, ulong a7, F88_S3 a8, ulong a9, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)125, a0.F0);
            Assert.Equal((short)-10705, a0.F1);
            Assert.Equal((byte)21, a0.F2);
            Assert.Equal((double)361845689097003, a0.F3);
            Assert.Equal((ushort)41749, a0.F4);
            Assert.Equal((double)1754583995806427, a1.F0);
            Assert.Equal((byte)178, a1.F1);
            Assert.Equal((float)4705205, a2);
            Assert.Equal((nuint)unchecked((nuint)5985040566226273121), a3);
            Assert.Equal((float)2484194, a4);
            Assert.Equal((nint)unchecked((nint)1904196135427766362), a5);
            Assert.Equal((nuint)unchecked((nuint)5436710892090266406), a6.F0);
            Assert.Equal((ulong)4250368992471675181, a7);
            Assert.Equal((sbyte)-87, a8.F0);
            Assert.Equal((uint)362108395, a8.F1);
            Assert.Equal((ulong)3388632419732870796, a9);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F88_Ret(46260161, 1256453227, unchecked((nint)1136413683894590872), 5467618237876965483);
    }

    [Fact]
    public static void TestSwiftCallbackFunc88()
    {
        Console.Write("Running SwiftCallbackFunc88: ");
        ExceptionDispatchInfo ex = null;
        F88_Ret val = SwiftCallbackFunc88(&SwiftCallbackFunc88Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((int)46260161, val.F0);
        Assert.Equal((uint)1256453227, val.F1);
        Assert.Equal((nint)unchecked((nint)1136413683894590872), val.F2);
        Assert.Equal((ulong)5467618237876965483, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F89_S0
    {
        public double F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F89_Ret_S0
    {
        public double F0;

        public F89_Ret_S0(double f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F89_Ret
    {
        public int F0;
        public F89_Ret_S0 F1;
        public nuint F2;
        public long F3;

        public F89_Ret(int f0, F89_Ret_S0 f1, nuint f2, long f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func891fAA7F89_RetVAeA0G3_S0VXE_tF")]
    private static extern F89_Ret SwiftCallbackFunc89(delegate* unmanaged[Swift]<F89_S0, SwiftSelf, F89_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F89_Ret SwiftCallbackFunc89Callback(F89_S0 a0, SwiftSelf self)
    {
        try
        {
            Assert.Equal((double)2137010348736191, a0.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F89_Ret(891143792, new F89_Ret_S0(3363709596088133), unchecked((nuint)18782615486598250), 1765451025668395967);
    }

    [Fact]
    public static void TestSwiftCallbackFunc89()
    {
        Console.Write("Running SwiftCallbackFunc89: ");
        ExceptionDispatchInfo ex = null;
        F89_Ret val = SwiftCallbackFunc89(&SwiftCallbackFunc89Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((int)891143792, val.F0);
        Assert.Equal((double)3363709596088133, val.F1.F0);
        Assert.Equal((nuint)unchecked((nuint)18782615486598250), val.F2);
        Assert.Equal((long)1765451025668395967, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F90_S0_S0_S0
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F90_S0_S0
    {
        public F90_S0_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 34)]
    struct F90_S0
    {
        public F90_S0_S0 F0;
        public nuint F1;
        public uint F2;
        public long F3;
        public short F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F90_S1
    {
        public ushort F0;
        public short F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F90_S2
    {
        public nint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F90_S3
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F90_S4
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F90_Ret
    {
        public short F0;
        public nint F1;

        public F90_Ret(short f0, nint f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func901fAA7F90_RetVAEs5Int64V_SfAA0G3_S0Vs6UInt32Vs6UInt16VAA0G3_S1VAA0G3_S2VAA0G3_S3VAA0G3_S4VtXE_tF")]
    private static extern F90_Ret SwiftCallbackFunc90(delegate* unmanaged[Swift]<long, float, F90_S0, uint, ushort, F90_S1, F90_S2, F90_S3, F90_S4, SwiftSelf, F90_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F90_Ret SwiftCallbackFunc90Callback(long a0, float a1, F90_S0 a2, uint a3, ushort a4, F90_S1 a5, F90_S2 a6, F90_S3 a7, F90_S4 a8, SwiftSelf self)
    {
        try
        {
            Assert.Equal((long)920081051198141017, a0);
            Assert.Equal((float)661904, a1);
            Assert.Equal((nuint)unchecked((nuint)3898354148166517637), a2.F0.F0.F0);
            Assert.Equal((nuint)unchecked((nuint)1003118682503285076), a2.F1);
            Assert.Equal((uint)1418362079, a2.F2);
            Assert.Equal((long)3276689793574299746, a2.F3);
            Assert.Equal((short)-18559, a2.F4);
            Assert.Equal((uint)1773011602, a3);
            Assert.Equal((ushort)32638, a4);
            Assert.Equal((ushort)47129, a5.F0);
            Assert.Equal((short)-31849, a5.F1);
            Assert.Equal((nint)unchecked((nint)4795020225668482328), a6.F0);
            Assert.Equal((nuint)unchecked((nuint)5307513663902191175), a7.F0);
            Assert.Equal((ulong)7057074401404034083, a8.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F90_Ret(25416, unchecked((nint)5015525780568020281));
    }

    [Fact]
    public static void TestSwiftCallbackFunc90()
    {
        Console.Write("Running SwiftCallbackFunc90: ");
        ExceptionDispatchInfo ex = null;
        F90_Ret val = SwiftCallbackFunc90(&SwiftCallbackFunc90Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((short)25416, val.F0);
        Assert.Equal((nint)unchecked((nint)5015525780568020281), val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F91_S0
    {
        public sbyte F0;
        public nint F1;
        public ushort F2;
        public ushort F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 36)]
    struct F91_S1
    {
        public double F0;
        public ulong F1;
        public sbyte F2;
        public long F3;
        public float F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F91_S2_S0_S0
    {
        public long F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F91_S2_S0
    {
        public F91_S2_S0_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F91_S2
    {
        public double F0;
        public F91_S2_S0 F1;
        public short F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F91_S3_S0
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F91_S3
    {
        public F91_S3_S0 F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F91_Ret
    {
        public long F0;
        public ulong F1;
        public short F2;
        public uint F3;

        public F91_Ret(long f0, ulong f1, short f2, uint f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func911fAA7F91_RetVAeA0G3_S0V_s5Int16Vs6UInt32VSdAA0G3_S1Vs5Int64Vs6UInt64VSfAA0G3_S2VSiAA0G3_S3VtXE_tF")]
    private static extern F91_Ret SwiftCallbackFunc91(delegate* unmanaged[Swift]<F91_S0, short, uint, double, F91_S1, long, ulong, float, F91_S2, nint, F91_S3, SwiftSelf, F91_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F91_Ret SwiftCallbackFunc91Callback(F91_S0 a0, short a1, uint a2, double a3, F91_S1 a4, long a5, ulong a6, float a7, F91_S2 a8, nint a9, F91_S3 a10, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)-117, a0.F0);
            Assert.Equal((nint)unchecked((nint)6851485542307521521), a0.F1);
            Assert.Equal((ushort)23224, a0.F2);
            Assert.Equal((ushort)28870, a0.F3);
            Assert.Equal((short)-26318, a1);
            Assert.Equal((uint)874052395, a2);
            Assert.Equal((double)3651199868446152, a3);
            Assert.Equal((double)3201729800438540, a4.F0);
            Assert.Equal((ulong)7737032265509566019, a4.F1);
            Assert.Equal((sbyte)123, a4.F2);
            Assert.Equal((long)7508633930609553617, a4.F3);
            Assert.Equal((float)8230501, a4.F4);
            Assert.Equal((long)2726677037673277403, a5);
            Assert.Equal((ulong)4990410590084533996, a6);
            Assert.Equal((float)3864639, a7);
            Assert.Equal((double)1763083442463892, a8.F0);
            Assert.Equal((long)6783710957456602933, a8.F1.F0.F0);
            Assert.Equal((short)2927, a8.F2);
            Assert.Equal((nint)unchecked((nint)3359440517385934325), a9);
            Assert.Equal((nuint)unchecked((nuint)3281136825102667421), a10.F0.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F91_Ret(8703949006228331232, 4839530995689756024, 14798, 1337111683);
    }

    [Fact]
    public static void TestSwiftCallbackFunc91()
    {
        Console.Write("Running SwiftCallbackFunc91: ");
        ExceptionDispatchInfo ex = null;
        F91_Ret val = SwiftCallbackFunc91(&SwiftCallbackFunc91Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((long)8703949006228331232, val.F0);
        Assert.Equal((ulong)4839530995689756024, val.F1);
        Assert.Equal((short)14798, val.F2);
        Assert.Equal((uint)1337111683, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F92_S0
    {
        public double F0;
        public double F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F92_S1
    {
        public uint F0;
        public long F1;
        public uint F2;
        public short F3;
        public ulong F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F92_S2_S0
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 18)]
    struct F92_S2
    {
        public uint F0;
        public long F1;
        public F92_S2_S0 F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F92_Ret
    {
        public int F0;

        public F92_Ret(int f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func921fAA7F92_RetVAEs6UInt32V_s5Int64VAA0G3_S0VSis5UInt8VAA0G3_S1VAA0G3_S2VAMSis5Int32VtXE_tF")]
    private static extern F92_Ret SwiftCallbackFunc92(delegate* unmanaged[Swift]<uint, long, F92_S0, nint, byte, F92_S1, F92_S2, byte, nint, int, SwiftSelf, F92_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F92_Ret SwiftCallbackFunc92Callback(uint a0, long a1, F92_S0 a2, nint a3, byte a4, F92_S1 a5, F92_S2 a6, byte a7, nint a8, int a9, SwiftSelf self)
    {
        try
        {
            Assert.Equal((uint)479487770, a0);
            Assert.Equal((long)3751818229732502126, a1);
            Assert.Equal((double)3486664439392893, a2.F0);
            Assert.Equal((double)1451061144702448, a2.F1);
            Assert.Equal((nint)unchecked((nint)1103649059951788126), a3);
            Assert.Equal((byte)17, a4);
            Assert.Equal((uint)1542537473, a5.F0);
            Assert.Equal((long)2256304993713022795, a5.F1);
            Assert.Equal((uint)1773847876, a5.F2);
            Assert.Equal((short)-4712, a5.F3);
            Assert.Equal((ulong)2811859744132572185, a5.F4);
            Assert.Equal((uint)290315682, a6.F0);
            Assert.Equal((long)4847587202070249866, a6.F1);
            Assert.Equal((ushort)20774, a6.F2.F0);
            Assert.Equal((byte)8, a7);
            Assert.Equal((nint)unchecked((nint)2206063999764082749), a8);
            Assert.Equal((int)1481391120, a9);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F92_Ret(2031462105);
    }

    [Fact]
    public static void TestSwiftCallbackFunc92()
    {
        Console.Write("Running SwiftCallbackFunc92: ");
        ExceptionDispatchInfo ex = null;
        F92_Ret val = SwiftCallbackFunc92(&SwiftCallbackFunc92Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((int)2031462105, val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F93_S0
    {
        public sbyte F0;
        public uint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F93_S1
    {
        public uint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F93_Ret
    {
        public nint F0;
        public ulong F1;

        public F93_Ret(nint f0, ulong f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func931fAA7F93_RetVAESu_s6UInt16VSdAA0G3_S0VAA0G3_S1VtXE_tF")]
    private static extern F93_Ret SwiftCallbackFunc93(delegate* unmanaged[Swift]<nuint, ushort, double, F93_S0, F93_S1, SwiftSelf, F93_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F93_Ret SwiftCallbackFunc93Callback(nuint a0, ushort a1, double a2, F93_S0 a3, F93_S1 a4, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nuint)unchecked((nuint)5170226481546239050), a0);
            Assert.Equal((ushort)2989, a1);
            Assert.Equal((double)1630717078645270, a2);
            Assert.Equal((sbyte)-46, a3.F0);
            Assert.Equal((uint)859171256, a3.F1);
            Assert.Equal((uint)254449240, a4.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F93_Ret(unchecked((nint)7713003294977630041), 4769707787914611024);
    }

    [Fact]
    public static void TestSwiftCallbackFunc93()
    {
        Console.Write("Running SwiftCallbackFunc93: ");
        ExceptionDispatchInfo ex = null;
        F93_Ret val = SwiftCallbackFunc93(&SwiftCallbackFunc93Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)7713003294977630041), val.F0);
        Assert.Equal((ulong)4769707787914611024, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F94_S0
    {
        public nuint F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F94_S1
    {
        public int F0;
        public nuint F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 14)]
    struct F94_S2
    {
        public nint F0;
        public uint F1;
        public ushort F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F94_S3
    {
        public byte F0;
        public int F1;
        public float F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F94_S4
    {
        public int F0;
        public long F1;
        public float F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 19)]
    struct F94_S5
    {
        public short F0;
        public nuint F1;
        public short F2;
        public sbyte F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F94_Ret
    {
        public long F0;

        public F94_Ret(long f0)
        {
            F0 = f0;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func941fAA7F94_RetVAeA0G3_S0V_s5Int16VAA0G3_S1VAA0G3_S2VAA0G3_S3VSfAA0G3_S4Vs6UInt32VAA0G3_S5VAItXE_tF")]
    private static extern F94_Ret SwiftCallbackFunc94(delegate* unmanaged[Swift]<F94_S0, short, F94_S1, F94_S2, F94_S3, float, F94_S4, uint, F94_S5, short, SwiftSelf, F94_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F94_Ret SwiftCallbackFunc94Callback(F94_S0 a0, short a1, F94_S1 a2, F94_S2 a3, F94_S3 a4, float a5, F94_S4 a6, uint a7, F94_S5 a8, short a9, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nuint)unchecked((nuint)8626725032375870186), a0.F0);
            Assert.Equal((short)-7755, a1);
            Assert.Equal((int)544707027, a2.F0);
            Assert.Equal((nuint)unchecked((nuint)2251410026467996594), a2.F1);
            Assert.Equal((nint)unchecked((nint)2972912419231960385), a3.F0);
            Assert.Equal((uint)740529487, a3.F1);
            Assert.Equal((ushort)34526, a3.F2);
            Assert.Equal((byte)41, a4.F0);
            Assert.Equal((int)1598856955, a4.F1);
            Assert.Equal((float)5126603, a4.F2);
            Assert.Equal((float)7242977, a5);
            Assert.Equal((int)473684762, a6.F0);
            Assert.Equal((long)4023878650965716094, a6.F1);
            Assert.Equal((float)2777693, a6.F2);
            Assert.Equal((uint)1612378906, a7);
            Assert.Equal((short)-17074, a8.F0);
            Assert.Equal((nuint)unchecked((nuint)2666903737827472071), a8.F1);
            Assert.Equal((short)418, a8.F2);
            Assert.Equal((sbyte)106, a8.F3);
            Assert.Equal((short)-14547, a9);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F94_Ret(4965341488842559693);
    }

    [Fact]
    public static void TestSwiftCallbackFunc94()
    {
        Console.Write("Running SwiftCallbackFunc94: ");
        ExceptionDispatchInfo ex = null;
        F94_Ret val = SwiftCallbackFunc94(&SwiftCallbackFunc94Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((long)4965341488842559693, val.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F95_S0
    {
        public ushort F0;
        public long F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F95_S1
    {
        public uint F0;
        public short F1;
        public double F2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F95_S2
    {
        public ushort F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F95_Ret_S0
    {
        public short F0;

        public F95_Ret_S0(short f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 14)]
    struct F95_Ret
    {
        public nint F0;
        public short F1;
        public sbyte F2;
        public byte F3;
        public F95_Ret_S0 F4;

        public F95_Ret(nint f0, short f1, sbyte f2, byte f3, F95_Ret_S0 f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func951fAA7F95_RetVAeA0G3_S0V_SuAA0G3_S1VAA0G3_S2VtXE_tF")]
    private static extern F95_Ret SwiftCallbackFunc95(delegate* unmanaged[Swift]<F95_S0, nuint, F95_S1, F95_S2, SwiftSelf, F95_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F95_Ret SwiftCallbackFunc95Callback(F95_S0 a0, nuint a1, F95_S1 a2, F95_S2 a3, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ushort)45388, a0.F0);
            Assert.Equal((long)6620047889014935849, a0.F1);
            Assert.Equal((nuint)unchecked((nuint)97365157264460373), a1);
            Assert.Equal((uint)357234637, a2.F0);
            Assert.Equal((short)-13720, a2.F1);
            Assert.Equal((double)3313430568949662, a2.F2);
            Assert.Equal((ushort)14248, a3.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F95_Ret(unchecked((nint)6503817931835164175), 1481, 117, 79, new F95_Ret_S0(-2735));
    }

    [Fact]
    public static void TestSwiftCallbackFunc95()
    {
        Console.Write("Running SwiftCallbackFunc95: ");
        ExceptionDispatchInfo ex = null;
        F95_Ret val = SwiftCallbackFunc95(&SwiftCallbackFunc95Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)6503817931835164175), val.F0);
        Assert.Equal((short)1481, val.F1);
        Assert.Equal((sbyte)117, val.F2);
        Assert.Equal((byte)79, val.F3);
        Assert.Equal((short)-2735, val.F4.F0);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct F96_S0
    {
        public long F0;
        public uint F1;
        public short F2;
        public double F3;
        public double F4;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F96_S1
    {
        public ulong F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F96_S2
    {
        public float F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func961fs6UInt64VAEs6UInt32V_AA6F96_S0VSfAe2gA0I3_S1VAA0I3_S2Vs5Int64VtXE_tF")]
    private static extern ulong SwiftCallbackFunc96(delegate* unmanaged[Swift]<uint, F96_S0, float, ulong, uint, uint, F96_S1, F96_S2, long, SwiftSelf, ulong> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static ulong SwiftCallbackFunc96Callback(uint a0, F96_S0 a1, float a2, ulong a3, uint a4, uint a5, F96_S1 a6, F96_S2 a7, long a8, SwiftSelf self)
    {
        try
        {
            Assert.Equal((uint)1103144790, a0);
            Assert.Equal((long)496343164737276588, a1.F0);
            Assert.Equal((uint)1541085564, a1.F1);
            Assert.Equal((short)-16271, a1.F2);
            Assert.Equal((double)1062575289573718, a1.F3);
            Assert.Equal((double)570255786498865, a1.F4);
            Assert.Equal((float)7616839, a2);
            Assert.Equal((ulong)7370881799887414383, a3);
            Assert.Equal((uint)390392554, a4);
            Assert.Equal((uint)1492692139, a5);
            Assert.Equal((ulong)1666031716012978365, a6.F0);
            Assert.Equal((float)3427394, a7.F0);
            Assert.Equal((long)4642371619161527189, a8);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 8803914823303717324;
    }

    [Fact]
    public static void TestSwiftCallbackFunc96()
    {
        Console.Write("Running SwiftCallbackFunc96: ");
        ExceptionDispatchInfo ex = null;
        ulong val = SwiftCallbackFunc96(&SwiftCallbackFunc96Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)8803914823303717324, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F97_S0
    {
        public sbyte F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F97_S1
    {
        public long F0;
        public ulong F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct F97_S2
    {
        public byte F0;
        public long F1;
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F97_S3
    {
        public double F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F97_Ret_S0
    {
        public int F0;

        public F97_Ret_S0(int f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    struct F97_Ret
    {
        public double F0;
        public nuint F1;
        public F97_Ret_S0 F2;
        public ushort F3;
        public uint F4;

        public F97_Ret(double f0, nuint f1, F97_Ret_S0 f2, ushort f3, uint f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func971fAA7F97_RetVAeA0G3_S0V_AA0G3_S1VAA0G3_S2VAA0G3_S3VtXE_tF")]
    private static extern F97_Ret SwiftCallbackFunc97(delegate* unmanaged[Swift]<F97_S0, F97_S1, F97_S2, F97_S3, SwiftSelf, F97_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F97_Ret SwiftCallbackFunc97Callback(F97_S0 a0, F97_S1 a1, F97_S2 a2, F97_S3 a3, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)-87, a0.F0);
            Assert.Equal((long)1414208343412494909, a1.F0);
            Assert.Equal((ulong)453284654311256466, a1.F1);
            Assert.Equal((byte)224, a2.F0);
            Assert.Equal((long)1712859616922087053, a2.F1);
            Assert.Equal((double)3987671154739178, a3.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F97_Ret(3262802544778620, unchecked((nuint)988644880611380240), new F97_Ret_S0(1818371708), 15694, 2068394006);
    }

    [Fact]
    public static void TestSwiftCallbackFunc97()
    {
        Console.Write("Running SwiftCallbackFunc97: ");
        ExceptionDispatchInfo ex = null;
        F97_Ret val = SwiftCallbackFunc97(&SwiftCallbackFunc97Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((double)3262802544778620, val.F0);
        Assert.Equal((nuint)unchecked((nuint)988644880611380240), val.F1);
        Assert.Equal((int)1818371708, val.F2.F0);
        Assert.Equal((ushort)15694, val.F3);
        Assert.Equal((uint)2068394006, val.F4);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    struct F98_S0
    {
        public int F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func981fS2iSf_s6UInt16VAA6F98_S0VAEtXE_tF")]
    private static extern nint SwiftCallbackFunc98(delegate* unmanaged[Swift]<float, ushort, F98_S0, ushort, SwiftSelf, nint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static nint SwiftCallbackFunc98Callback(float a0, ushort a1, F98_S0 a2, ushort a3, SwiftSelf self)
    {
        try
        {
            Assert.Equal((float)2863898, a0);
            Assert.Equal((ushort)37573, a1);
            Assert.Equal((int)1073068257, a2.F0);
            Assert.Equal((ushort)53560, a3);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return unchecked((nint)6686142382639170849);
    }

    [Fact]
    public static void TestSwiftCallbackFunc98()
    {
        Console.Write("Running SwiftCallbackFunc98: ");
        ExceptionDispatchInfo ex = null;
        nint val = SwiftCallbackFunc98(&SwiftCallbackFunc98Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)6686142382639170849), val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F99_S0
    {
        public nint F0;
        public uint F1;
        public int F2;
        public uint F3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct F99_S1
    {
        public short F0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    struct F99_S2
    {
        public byte F0;
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB6Func991fs6UInt64VAEs5Int64V_SuSfs6UInt16VAA6F99_S0Vs5UInt8VSfAMs4Int8VAA0J3_S1VAA0J3_S2VtXE_tF")]
    private static extern ulong SwiftCallbackFunc99(delegate* unmanaged[Swift]<long, nuint, float, ushort, F99_S0, byte, float, byte, sbyte, F99_S1, F99_S2, SwiftSelf, ulong> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static ulong SwiftCallbackFunc99Callback(long a0, nuint a1, float a2, ushort a3, F99_S0 a4, byte a5, float a6, byte a7, sbyte a8, F99_S1 a9, F99_S2 a10, SwiftSelf self)
    {
        try
        {
            Assert.Equal((long)1152281003884062246, a0);
            Assert.Equal((nuint)unchecked((nuint)2482384127373829622), a1);
            Assert.Equal((float)3361150, a2);
            Assert.Equal((ushort)2121, a3);
            Assert.Equal((nint)unchecked((nint)4484545590050696958), a4.F0);
            Assert.Equal((uint)422528630, a4.F1);
            Assert.Equal((int)1418346646, a4.F2);
            Assert.Equal((uint)1281567856, a4.F3);
            Assert.Equal((byte)223, a5);
            Assert.Equal((float)1917656, a6);
            Assert.Equal((byte)103, a7);
            Assert.Equal((sbyte)-46, a8);
            Assert.Equal((short)14554, a9.F0);
            Assert.Equal((byte)68, a10.F0);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 8220698022338840251;
    }

    [Fact]
    public static void TestSwiftCallbackFunc99()
    {
        Console.Write("Running SwiftCallbackFunc99: ");
        ExceptionDispatchInfo ex = null;
        ulong val = SwiftCallbackFunc99(&SwiftCallbackFunc99Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)8220698022338840251, val);
        Console.WriteLine("OK");
    }

}
