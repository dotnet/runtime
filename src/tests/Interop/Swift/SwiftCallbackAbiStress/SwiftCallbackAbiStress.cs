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

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F0_Ret
    {
        public ushort F0;
        public float F1;
        public int F2;
        public ulong F3;

        public F0_Ret(ushort f0, float f1, int f2, ulong f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func01fAA6F0_RetVAEs5Int16V_s5Int32Vs6UInt64Vs6UInt16Vs5Int64VSds6UInt32VAMSiAKtXE_tF")]
    private static extern F0_Ret SwiftCallbackFunc0(delegate* unmanaged[Swift]<short, int, ulong, ushort, long, double, uint, ushort, nint, ulong, SwiftSelf, F0_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F0_Ret SwiftCallbackFunc0Callback(short a0, int a1, ulong a2, ushort a3, long a4, double a5, uint a6, ushort a7, nint a8, ulong a9, SwiftSelf self)
    {
        try
        {
            Assert.Equal((short)-17813, a0);
            Assert.Equal((int)318006528, a1);
            Assert.Equal((ulong)1195162122024233590, a2);
            Assert.Equal((ushort)60467, a3);
            Assert.Equal((long)4587464142261794085, a4);
            Assert.Equal((double)2686980744237725, a5);
            Assert.Equal((uint)331986645, a6);
            Assert.Equal((ushort)56299, a7);
            Assert.Equal((nint)unchecked((nint)6785053689615432643), a8);
            Assert.Equal((ulong)6358078381523084952, a9);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F0_Ret(65117, 981990, 1192391225, 7001579272668151908);
    }

    [Fact]
    public static void TestSwiftCallbackFunc0()
    {
        Console.Write("Running SwiftCallbackFunc0: ");
        ExceptionDispatchInfo ex = null;
        F0_Ret val = SwiftCallbackFunc0(&SwiftCallbackFunc0Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)65117, val.F0);
        Assert.Equal((float)981990, val.F1);
        Assert.Equal((int)1192391225, val.F2);
        Assert.Equal((ulong)7001579272668151908, val.F3);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func11fS2uSd_s4Int8Vs5Int32Vs6UInt16Vs5UInt8VSdAKs6UInt64Vs5Int16VS2fAmEtXE_tF")]
    private static extern nuint SwiftCallbackFunc1(delegate* unmanaged[Swift]<double, sbyte, int, ushort, byte, double, byte, ulong, short, float, float, ulong, sbyte, SwiftSelf, nuint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static nuint SwiftCallbackFunc1Callback(double a0, sbyte a1, int a2, ushort a3, byte a4, double a5, byte a6, ulong a7, short a8, float a9, float a10, ulong a11, sbyte a12, SwiftSelf self)
    {
        try
        {
            Assert.Equal((double)3867437130564654, a0);
            Assert.Equal((sbyte)-64, a1);
            Assert.Equal((int)31081182, a2);
            Assert.Equal((ushort)20316, a3);
            Assert.Equal((byte)73, a4);
            Assert.Equal((double)3543740592144911, a5);
            Assert.Equal((byte)250, a6);
            Assert.Equal((ulong)6680393408153342744, a7);
            Assert.Equal((short)23758, a8);
            Assert.Equal((float)7189013, a9);
            Assert.Equal((float)5438196, a10);
            Assert.Equal((ulong)3310322731568932038, a11);
            Assert.Equal((sbyte)3, a12);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return unchecked((nuint)2172476334497055933);
    }

    [Fact]
    public static void TestSwiftCallbackFunc1()
    {
        Console.Write("Running SwiftCallbackFunc1: ");
        ExceptionDispatchInfo ex = null;
        nuint val = SwiftCallbackFunc1(&SwiftCallbackFunc1Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nuint)unchecked((nuint)2172476334497055933), val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    struct F2_Ret_S0
    {
        public long F0;
        public int F1;

        public F2_Ret_S0(long f0, int f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 14)]
    struct F2_Ret
    {
        public F2_Ret_S0 F0;
        public short F1;

        public F2_Ret(F2_Ret_S0 f0, short f1)
        {
            F0 = f0;
            F1 = f1;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func21fAA6F2_RetVAESu_s5UInt8VtXE_tF")]
    private static extern F2_Ret SwiftCallbackFunc2(delegate* unmanaged[Swift]<nuint, byte, SwiftSelf, F2_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F2_Ret SwiftCallbackFunc2Callback(nuint a0, byte a1, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nuint)unchecked((nuint)2153637757371267722), a0);
            Assert.Equal((byte)150, a1);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F2_Ret(new F2_Ret_S0(5628852360797741825, 939232542), -9943);
    }

    [Fact]
    public static void TestSwiftCallbackFunc2()
    {
        Console.Write("Running SwiftCallbackFunc2: ");
        ExceptionDispatchInfo ex = null;
        F2_Ret val = SwiftCallbackFunc2(&SwiftCallbackFunc2Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((long)5628852360797741825, val.F0.F0);
        Assert.Equal((int)939232542, val.F0.F1);
        Assert.Equal((short)-9943, val.F1);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 10)]
    struct F3_Ret_S0
    {
        public short F0;
        public int F1;
        public ushort F2;

        public F3_Ret_S0(short f0, int f1, ushort f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 33)]
    struct F3_Ret
    {
        public nint F0;
        public F3_Ret_S0 F1;
        public nuint F2;
        public sbyte F3;

        public F3_Ret(nint f0, F3_Ret_S0 f1, nuint f2, sbyte f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func31fAA6F3_RetVAEs6UInt16V_S2uSiSfAGtXE_tF")]
    private static extern F3_Ret SwiftCallbackFunc3(delegate* unmanaged[Swift]<ushort, nuint, nuint, nint, float, ushort, SwiftSelf, F3_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F3_Ret SwiftCallbackFunc3Callback(ushort a0, nuint a1, nuint a2, nint a3, float a4, ushort a5, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ushort)45065, a0);
            Assert.Equal((nuint)unchecked((nuint)8506742096411295359), a1);
            Assert.Equal((nuint)unchecked((nuint)8619375465417625458), a2);
            Assert.Equal((nint)unchecked((nint)5288917394772427257), a3);
            Assert.Equal((float)5678138, a4);
            Assert.Equal((ushort)33467, a5);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F3_Ret(unchecked((nint)3330016214205716187), new F3_Ret_S0(-29819, 2075852318, 671), unchecked((nuint)2368015527878194540), -79);
    }

    [Fact]
    public static void TestSwiftCallbackFunc3()
    {
        Console.Write("Running SwiftCallbackFunc3: ");
        ExceptionDispatchInfo ex = null;
        F3_Ret val = SwiftCallbackFunc3(&SwiftCallbackFunc3Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)3330016214205716187), val.F0);
        Assert.Equal((short)-29819, val.F1.F0);
        Assert.Equal((int)2075852318, val.F1.F1);
        Assert.Equal((ushort)671, val.F1.F2);
        Assert.Equal((nuint)unchecked((nuint)2368015527878194540), val.F2);
        Assert.Equal((sbyte)-79, val.F3);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    struct F4_Ret
    {
        public ulong F0;
        public uint F1;
        public ulong F2;

        public F4_Ret(ulong f0, uint f1, ulong f2)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func41fAA6F4_RetVAEs5Int64V_s6UInt16Vs5Int32VAISiSdAISfAkIs4Int8VSfs6UInt64Vs5Int16VSdA2mKSiAk2GtXE_tF")]
    private static extern F4_Ret SwiftCallbackFunc4(delegate* unmanaged[Swift]<long, ushort, int, ushort, nint, double, ushort, float, int, ushort, sbyte, float, ulong, short, double, sbyte, sbyte, int, nint, int, long, long, SwiftSelf, F4_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F4_Ret SwiftCallbackFunc4Callback(long a0, ushort a1, int a2, ushort a3, nint a4, double a5, ushort a6, float a7, int a8, ushort a9, sbyte a10, float a11, ulong a12, short a13, double a14, sbyte a15, sbyte a16, int a17, nint a18, int a19, long a20, long a21, SwiftSelf self)
    {
        try
        {
            Assert.Equal((long)8771527078890676837, a0);
            Assert.Equal((ushort)18667, a1);
            Assert.Equal((int)224631333, a2);
            Assert.Equal((ushort)13819, a3);
            Assert.Equal((nint)unchecked((nint)8888237425788084647), a4);
            Assert.Equal((double)2677321682649925, a5);
            Assert.Equal((ushort)50276, a6);
            Assert.Equal((float)2703201, a7);
            Assert.Equal((int)545337834, a8);
            Assert.Equal((ushort)11190, a9);
            Assert.Equal((sbyte)112, a10);
            Assert.Equal((float)4053251, a11);
            Assert.Equal((ulong)7107857019164433129, a12);
            Assert.Equal((short)-3092, a13);
            Assert.Equal((double)2176685406663423, a14);
            Assert.Equal((sbyte)57, a15);
            Assert.Equal((sbyte)-61, a16);
            Assert.Equal((int)866840318, a17);
            Assert.Equal((nint)unchecked((nint)5927291145767969522), a18);
            Assert.Equal((int)1818333546, a19);
            Assert.Equal((long)6272248211765159948, a20);
            Assert.Equal((long)6555966806846053216, a21);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F4_Ret(2182947204061522719, 1721424472, 7504841280611598884);
    }

    [Fact]
    public static void TestSwiftCallbackFunc4()
    {
        Console.Write("Running SwiftCallbackFunc4: ");
        ExceptionDispatchInfo ex = null;
        F4_Ret val = SwiftCallbackFunc4(&SwiftCallbackFunc4Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)2182947204061522719, val.F0);
        Assert.Equal((uint)1721424472, val.F1);
        Assert.Equal((ulong)7504841280611598884, val.F2);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    struct F5_Ret
    {
        public ulong F0;
        public int F1;
        public nint F2;
        public float F3;
        public short F4;
        public ulong F5;

        public F5_Ret(ulong f0, int f1, nint f2, float f3, short f4, ulong f5)
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
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func51fAA6F5_RetVAEs5Int32V_s6UInt16VAIs5Int16Vs5UInt8Vs4Int8VAMSis6UInt64VAQs5Int64VA2ksimItXE_tF")]
    private static extern F5_Ret SwiftCallbackFunc5(delegate* unmanaged[Swift]<int, ushort, ushort, short, byte, sbyte, byte, nint, ulong, ulong, long, short, short, long, ushort, byte, ushort, SwiftSelf, F5_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F5_Ret SwiftCallbackFunc5Callback(int a0, ushort a1, ushort a2, short a3, byte a4, sbyte a5, byte a6, nint a7, ulong a8, ulong a9, long a10, short a11, short a12, long a13, ushort a14, byte a15, ushort a16, SwiftSelf self)
    {
        try
        {
            Assert.Equal((int)359602150, a0);
            Assert.Equal((ushort)51495, a1);
            Assert.Equal((ushort)37765, a2);
            Assert.Equal((short)29410, a3);
            Assert.Equal((byte)95, a4);
            Assert.Equal((sbyte)-104, a5);
            Assert.Equal((byte)32, a6);
            Assert.Equal((nint)unchecked((nint)8530952551906271255), a7);
            Assert.Equal((ulong)706266487837805024, a8);
            Assert.Equal((ulong)707905209555595641, a9);
            Assert.Equal((long)8386588676727568762, a10);
            Assert.Equal((short)-8624, a11);
            Assert.Equal((short)26113, a12);
            Assert.Equal((long)8389143657021522019, a13);
            Assert.Equal((ushort)13337, a14);
            Assert.Equal((byte)229, a15);
            Assert.Equal((ushort)51876, a16);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F5_Ret(5224035852455624489, 493616651, unchecked((nint)3355493231962241213), 8151117, -6001, 2418751914358801711);
    }

    [Fact]
    public static void TestSwiftCallbackFunc5()
    {
        Console.Write("Running SwiftCallbackFunc5: ");
        ExceptionDispatchInfo ex = null;
        F5_Ret val = SwiftCallbackFunc5(&SwiftCallbackFunc5Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ulong)5224035852455624489, val.F0);
        Assert.Equal((int)493616651, val.F1);
        Assert.Equal((nint)unchecked((nint)3355493231962241213), val.F2);
        Assert.Equal((float)8151117, val.F3);
        Assert.Equal((short)-6001, val.F4);
        Assert.Equal((ulong)2418751914358801711, val.F5);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func61fs6UInt16VAEs5Int32V_s6UInt32Vs6UInt64VAGs4Int8VS2is5Int16VSiAi2Ks5Int64VAItXE_tF")]
    private static extern ushort SwiftCallbackFunc6(delegate* unmanaged[Swift]<int, uint, ulong, int, sbyte, nint, nint, short, nint, uint, ulong, ulong, long, uint, SwiftSelf, ushort> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static ushort SwiftCallbackFunc6Callback(int a0, uint a1, ulong a2, int a3, sbyte a4, nint a5, nint a6, short a7, nint a8, uint a9, ulong a10, ulong a11, long a12, uint a13, SwiftSelf self)
    {
        try
        {
            Assert.Equal((int)743741783, a0);
            Assert.Equal((uint)850236948, a1);
            Assert.Equal((ulong)5908745692727636656, a2);
            Assert.Equal((int)2106839818, a3);
            Assert.Equal((sbyte)77, a4);
            Assert.Equal((nint)unchecked((nint)291907785975160065), a5);
            Assert.Equal((nint)unchecked((nint)3560129042279209151), a6);
            Assert.Equal((short)-30568, a7);
            Assert.Equal((nint)unchecked((nint)5730241035812482149), a8);
            Assert.Equal((uint)18625011, a9);
            Assert.Equal((ulong)242340713355417257, a10);
            Assert.Equal((ulong)6962175160124965670, a11);
            Assert.Equal((long)2935089705514798822, a12);
            Assert.Equal((uint)2051956645, a13);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 45160;
    }

    [Fact]
    public static void TestSwiftCallbackFunc6()
    {
        Console.Write("Running SwiftCallbackFunc6: ");
        ExceptionDispatchInfo ex = null;
        ushort val = SwiftCallbackFunc6(&SwiftCallbackFunc6Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((ushort)45160, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct F7_Ret_S0
    {
        public nint F0;

        public F7_Ret_S0(nint f0)
        {
            F0 = f0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    struct F7_Ret
    {
        public sbyte F0;
        public sbyte F1;
        public byte F2;
        public F7_Ret_S0 F3;
        public uint F4;

        public F7_Ret(sbyte f0, sbyte f1, byte f2, F7_Ret_S0 f3, uint f4)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
            F4 = f4;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func71fAA6F7_RetVAEs6UInt64V_s5UInt8Vs5Int16VSutXE_tF")]
    private static extern F7_Ret SwiftCallbackFunc7(delegate* unmanaged[Swift]<ulong, byte, short, nuint, SwiftSelf, F7_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F7_Ret SwiftCallbackFunc7Callback(ulong a0, byte a1, short a2, nuint a3, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ulong)7625368278886567558, a0);
            Assert.Equal((byte)70, a1);
            Assert.Equal((short)26780, a2);
            Assert.Equal((nuint)unchecked((nuint)7739343395912136630), a3);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F7_Ret(-96, -93, 251, new F7_Ret_S0(unchecked((nint)3590193056511262571)), 13223810);
    }

    [Fact]
    public static void TestSwiftCallbackFunc7()
    {
        Console.Write("Running SwiftCallbackFunc7: ");
        ExceptionDispatchInfo ex = null;
        F7_Ret val = SwiftCallbackFunc7(&SwiftCallbackFunc7Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((sbyte)-96, val.F0);
        Assert.Equal((sbyte)-93, val.F1);
        Assert.Equal((byte)251, val.F2);
        Assert.Equal((nint)unchecked((nint)3590193056511262571), val.F3.F0);
        Assert.Equal((uint)13223810, val.F4);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func81fs5UInt8VAESf_SutXE_tF")]
    private static extern byte SwiftCallbackFunc8(delegate* unmanaged[Swift]<float, nuint, SwiftSelf, byte> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static byte SwiftCallbackFunc8Callback(float a0, nuint a1, SwiftSelf self)
    {
        try
        {
            Assert.Equal((float)6278007, a0);
            Assert.Equal((nuint)unchecked((nuint)1620979945874429615), a1);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 60;
    }

    [Fact]
    public static void TestSwiftCallbackFunc8()
    {
        Console.Write("Running SwiftCallbackFunc8: ");
        ExceptionDispatchInfo ex = null;
        byte val = SwiftCallbackFunc8(&SwiftCallbackFunc8Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((byte)60, val);
        Console.WriteLine("OK");
    }

    [StructLayout(LayoutKind.Sequential, Size = 26)]
    struct F9_Ret
    {
        public uint F0;
        public long F1;
        public ulong F2;
        public ushort F3;

        public F9_Ret(uint f0, long f1, ulong f2, ushort f3)
        {
            F0 = f0;
            F1 = f1;
            F2 = f2;
            F3 = f3;
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func91fAA6F9_RetVAEs4Int8V_Sis5Int16Vs5Int64VS2dSis6UInt16VAMS2fAMs6UInt32VAIs5Int32VAQs6UInt64VAiKSis5UInt8VAmISiAItXE_tF")]
    private static extern F9_Ret SwiftCallbackFunc9(delegate* unmanaged[Swift]<sbyte, nint, short, long, double, double, nint, ushort, ushort, float, float, ushort, uint, short, int, int, ulong, short, long, nint, byte, ushort, short, nint, short, SwiftSelf, F9_Ret> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static F9_Ret SwiftCallbackFunc9Callback(sbyte a0, nint a1, short a2, long a3, double a4, double a5, nint a6, ushort a7, ushort a8, float a9, float a10, ushort a11, uint a12, short a13, int a14, int a15, ulong a16, short a17, long a18, nint a19, byte a20, ushort a21, short a22, nint a23, short a24, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)17, a0);
            Assert.Equal((nint)unchecked((nint)4720638462358523954), a1);
            Assert.Equal((short)30631, a2);
            Assert.Equal((long)8206569929240962953, a3);
            Assert.Equal((double)1359667226908383, a4);
            Assert.Equal((double)3776001892555053, a5);
            Assert.Equal((nint)unchecked((nint)747160900180286726), a6);
            Assert.Equal((ushort)12700, a7);
            Assert.Equal((ushort)53813, a8);
            Assert.Equal((float)7860389, a9);
            Assert.Equal((float)1879743, a10);
            Assert.Equal((ushort)61400, a11);
            Assert.Equal((uint)1962814337, a12);
            Assert.Equal((short)17992, a13);
            Assert.Equal((int)677814589, a14);
            Assert.Equal((int)1019483263, a15);
            Assert.Equal((ulong)6326265259403184370, a16);
            Assert.Equal((short)-14633, a17);
            Assert.Equal((long)4127072498763789519, a18);
            Assert.Equal((nint)unchecked((nint)4008108205305320386), a19);
            Assert.Equal((byte)128, a20);
            Assert.Equal((ushort)21189, a21);
            Assert.Equal((short)32104, a22);
            Assert.Equal((nint)unchecked((nint)384827814282870543), a23);
            Assert.Equal((short)20647, a24);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return new F9_Ret(189282789, 114803850982111219, 4506415416389763390, 23584);
    }

    [Fact]
    public static void TestSwiftCallbackFunc9()
    {
        Console.Write("Running SwiftCallbackFunc9: ");
        ExceptionDispatchInfo ex = null;
        F9_Ret val = SwiftCallbackFunc9(&SwiftCallbackFunc9Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((uint)189282789, val.F0);
        Assert.Equal((long)114803850982111219, val.F1);
        Assert.Equal((ulong)4506415416389763390, val.F2);
        Assert.Equal((ushort)23584, val.F3);
        Console.WriteLine("OK");
    }

}
