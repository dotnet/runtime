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

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func01fs5UInt8VAEs5Int16V_s5Int32Vs6UInt64Vs6UInt16Vs5Int64VSds6UInt32VAMSiAKtXE_tF")]
    private static extern byte SwiftCallbackFunc0(delegate* unmanaged[Swift]<short, int, ulong, ushort, long, double, uint, ushort, nint, ulong, SwiftSelf, byte> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static byte SwiftCallbackFunc0Callback(short a0, int a1, ulong a2, ushort a3, long a4, double a5, uint a6, ushort a7, nint a8, ulong a9, SwiftSelf self)
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

        return 254;
    }

    [Fact]
    public static void TestSwiftCallbackFunc0()
    {
        Console.Write("Running SwiftCallbackFunc0: ");
        ExceptionDispatchInfo ex = null;
        byte val = SwiftCallbackFunc0(&SwiftCallbackFunc0Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((byte)254, val);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func11fS2ds4Int8V_s5Int16VSfs5Int64VtXE_tF")]
    private static extern double SwiftCallbackFunc1(delegate* unmanaged[Swift]<sbyte, short, float, long, SwiftSelf, double> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static double SwiftCallbackFunc1Callback(sbyte a0, short a1, float a2, long a3, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)-117, a0);
            Assert.Equal((short)24667, a1);
            Assert.Equal((float)7203656, a2);
            Assert.Equal((long)2859275717293113701, a3);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 1815003852073252;
    }

    [Fact]
    public static void TestSwiftCallbackFunc1()
    {
        Console.Write("Running SwiftCallbackFunc1: ");
        ExceptionDispatchInfo ex = null;
        double val = SwiftCallbackFunc1(&SwiftCallbackFunc1Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((double)1815003852073252, val);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func21fs5UInt8VAEs4Int8V_s6UInt32VSfSuAegIs6UInt64Vs5Int64Vs5Int16Vs5Int32VAOSiSutXE_tF")]
    private static extern byte SwiftCallbackFunc2(delegate* unmanaged[Swift]<sbyte, uint, float, nuint, byte, sbyte, uint, ulong, long, short, int, short, nint, nuint, SwiftSelf, byte> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static byte SwiftCallbackFunc2Callback(sbyte a0, uint a1, float a2, nuint a3, byte a4, sbyte a5, uint a6, ulong a7, long a8, short a9, int a10, short a11, nint a12, nuint a13, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)93, a0);
            Assert.Equal((uint)571731946, a1);
            Assert.Equal((float)1958727, a2);
            Assert.Equal((nuint)unchecked((nuint)3919017851053326963), a3);
            Assert.Equal((byte)156, a4);
            Assert.Equal((sbyte)-17, a5);
            Assert.Equal((uint)747962023, a6);
            Assert.Equal((ulong)1104840539654964163, a7);
            Assert.Equal((long)5642679323997486487, a8);
            Assert.Equal((short)29557, a9);
            Assert.Equal((int)660273506, a10);
            Assert.Equal((short)-14877, a11);
            Assert.Equal((nint)unchecked((nint)3546952189496193868), a12);
            Assert.Equal((nuint)unchecked((nuint)3599444831393612282), a13);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 141;
    }

    [Fact]
    public static void TestSwiftCallbackFunc2()
    {
        Console.Write("Running SwiftCallbackFunc2: ");
        ExceptionDispatchInfo ex = null;
        byte val = SwiftCallbackFunc2(&SwiftCallbackFunc2Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((byte)141, val);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func31fS2dSi_s5UInt8Vs4Int8Vs5Int64VAGs6UInt16VS2uSiSfAKs6UInt32VSiAEs5Int16Vs5Int32VAKSuAgESdSiAmGtXE_tF")]
    private static extern double SwiftCallbackFunc3(delegate* unmanaged[Swift]<nint, byte, sbyte, long, sbyte, ushort, nuint, nuint, nint, float, ushort, uint, nint, byte, short, int, ushort, nuint, sbyte, byte, double, nint, uint, sbyte, SwiftSelf, double> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static double SwiftCallbackFunc3Callback(nint a0, byte a1, sbyte a2, long a3, sbyte a4, ushort a5, nuint a6, nuint a7, nint a8, float a9, ushort a10, uint a11, nint a12, byte a13, short a14, int a15, ushort a16, nuint a17, sbyte a18, byte a19, double a20, nint a21, uint a22, sbyte a23, SwiftSelf self)
    {
        try
        {
            Assert.Equal((nint)unchecked((nint)5610153900386943274), a0);
            Assert.Equal((byte)236, a1);
            Assert.Equal((sbyte)-6, a2);
            Assert.Equal((long)3316874161259890183, a3);
            Assert.Equal((sbyte)-53, a4);
            Assert.Equal((ushort)37580, a5);
            Assert.Equal((nuint)unchecked((nuint)1683057726956349710), a6);
            Assert.Equal((nuint)unchecked((nuint)415152378126297632), a7);
            Assert.Equal((nint)unchecked((nint)2870393941738319551), a8);
            Assert.Equal((float)1652893, a9);
            Assert.Equal((ushort)16825, a10);
            Assert.Equal((uint)419224712, a11);
            Assert.Equal((nint)unchecked((nint)7680849977572141563), a12);
            Assert.Equal((byte)200, a13);
            Assert.Equal((short)22892, a14);
            Assert.Equal((int)2118994921, a15);
            Assert.Equal((ushort)44276, a16);
            Assert.Equal((nuint)unchecked((nuint)4006990310546323213), a17);
            Assert.Equal((sbyte)-39, a18);
            Assert.Equal((byte)67, a19);
            Assert.Equal((double)3008014901411425, a20);
            Assert.Equal((nint)unchecked((nint)7039812168807528075), a21);
            Assert.Equal((uint)1057070707, a22);
            Assert.Equal((sbyte)103, a23);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 171601370856717;
    }

    [Fact]
    public static void TestSwiftCallbackFunc3()
    {
        Console.Write("Running SwiftCallbackFunc3: ");
        ExceptionDispatchInfo ex = null;
        double val = SwiftCallbackFunc3(&SwiftCallbackFunc3Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((double)171601370856717, val);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func41fs5Int64VAEs6UInt16V_Sfs5Int32VAGs4Int8VSfs6UInt64Vs5Int16VSdA2kISiAi2eiMs6UInt32VAMs5UInt8VAISuAKtXE_tF")]
    private static extern long SwiftCallbackFunc4(delegate* unmanaged[Swift]<ushort, float, int, ushort, sbyte, float, ulong, short, double, sbyte, sbyte, int, nint, int, long, long, int, ulong, uint, ulong, byte, int, nuint, sbyte, SwiftSelf, long> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static long SwiftCallbackFunc4Callback(ushort a0, float a1, int a2, ushort a3, sbyte a4, float a5, ulong a6, short a7, double a8, sbyte a9, sbyte a10, int a11, nint a12, int a13, long a14, long a15, int a16, ulong a17, uint a18, ulong a19, byte a20, int a21, nuint a22, sbyte a23, SwiftSelf self)
    {
        try
        {
            Assert.Equal((ushort)64787, a0);
            Assert.Equal((float)1472942, a1);
            Assert.Equal((int)2042281537, a2);
            Assert.Equal((ushort)18667, a3);
            Assert.Equal((sbyte)-102, a4);
            Assert.Equal((float)1768897, a5);
            Assert.Equal((ulong)8888237425788084647, a6);
            Assert.Equal((short)20853, a7);
            Assert.Equal((double)3454973030441503, a8);
            Assert.Equal((sbyte)-46, a9);
            Assert.Equal((sbyte)-63, a10);
            Assert.Equal((int)366699691, a11);
            Assert.Equal((nint)unchecked((nint)4641984012938514811), a12);
            Assert.Equal((int)1691113876, a13);
            Assert.Equal((long)6912906265890433291, a14);
            Assert.Equal((long)6701017449244003958, a15);
            Assert.Equal((int)568887433, a16);
            Assert.Equal((ulong)9099941242643212987, a17);
            Assert.Equal((uint)1380054056, a18);
            Assert.Equal((ulong)595836183051442276, a19);
            Assert.Equal((byte)174, a20);
            Assert.Equal((int)1047364523, a21);
            Assert.Equal((nuint)unchecked((nuint)1417646176372805029), a22);
            Assert.Equal((sbyte)-35, a23);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 3100893724073759448;
    }

    [Fact]
    public static void TestSwiftCallbackFunc4()
    {
        Console.Write("Running SwiftCallbackFunc4: ");
        ExceptionDispatchInfo ex = null;
        long val = SwiftCallbackFunc4(&SwiftCallbackFunc4Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((long)3100893724073759448, val);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func51fS2is4Int8V_s5UInt8VSis6UInt64VAIs5Int64Vs5Int16VAmKs6UInt16VAgOSfAIs5Int32VtXE_tF")]
    private static extern nint SwiftCallbackFunc5(delegate* unmanaged[Swift]<sbyte, byte, nint, ulong, ulong, long, short, short, long, ushort, byte, ushort, float, ulong, int, SwiftSelf, nint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static nint SwiftCallbackFunc5Callback(sbyte a0, byte a1, nint a2, ulong a3, ulong a4, long a5, short a6, short a7, long a8, ushort a9, byte a10, ushort a11, float a12, ulong a13, int a14, SwiftSelf self)
    {
        try
        {
            Assert.Equal((sbyte)-86, a0);
            Assert.Equal((byte)201, a1);
            Assert.Equal((nint)unchecked((nint)3436765034579128495), a2);
            Assert.Equal((ulong)6305137336506323506, a3);
            Assert.Equal((ulong)6280137078630028944, a4);
            Assert.Equal((long)6252650621827449809, a5);
            Assert.Equal((short)306, a6);
            Assert.Equal((short)-27739, a7);
            Assert.Equal((long)8386588676727568762, a8);
            Assert.Equal((ushort)24144, a9);
            Assert.Equal((byte)230, a10);
            Assert.Equal((ushort)59907, a11);
            Assert.Equal((float)1791462, a12);
            Assert.Equal((ulong)8271399067416599310, a13);
            Assert.Equal((int)1699875267, a14);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return unchecked((nint)5224035852455624489);
    }

    [Fact]
    public static void TestSwiftCallbackFunc5()
    {
        Console.Write("Running SwiftCallbackFunc5: ");
        ExceptionDispatchInfo ex = null;
        nint val = SwiftCallbackFunc5(&SwiftCallbackFunc5Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)5224035852455624489), val);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func61fs5Int64VAEs6UInt32V_Sfs5UInt8Vs5Int32VAGs6UInt64VAKs4Int8VS2is5Int16VSiAg2meGs6UInt16VtXE_tF")]
    private static extern long SwiftCallbackFunc6(delegate* unmanaged[Swift]<uint, float, byte, int, uint, ulong, int, sbyte, nint, nint, short, nint, uint, ulong, ulong, long, uint, ushort, SwiftSelf, long> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static long SwiftCallbackFunc6Callback(uint a0, float a1, byte a2, int a3, uint a4, ulong a5, int a6, sbyte a7, nint a8, nint a9, short a10, nint a11, uint a12, ulong a13, ulong a14, long a15, uint a16, ushort a17, SwiftSelf self)
    {
        try
        {
            Assert.Equal((uint)743741783, a0);
            Assert.Equal((float)3321238, a1);
            Assert.Equal((byte)51, a2);
            Assert.Equal((int)1315779092, a3);
            Assert.Equal((uint)1375736443, a4);
            Assert.Equal((ulong)7022244764256789748, a5);
            Assert.Equal((int)156967479, a6);
            Assert.Equal((sbyte)-120, a7);
            Assert.Equal((nint)unchecked((nint)3560129042279209151), a8);
            Assert.Equal((nint)unchecked((nint)9064213378356024089), a9);
            Assert.Equal((short)7947, a10);
            Assert.Equal((nint)unchecked((nint)8756231901741598476), a11);
            Assert.Equal((uint)56423704, a12);
            Assert.Equal((ulong)6962175160124965670, a13);
            Assert.Equal((ulong)2935089705514798822, a14);
            Assert.Equal((long)1348139258363155351, a15);
            Assert.Equal((uint)1754662893, a16);
            Assert.Equal((ushort)35528, a17);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 2428870998079250366;
    }

    [Fact]
    public static void TestSwiftCallbackFunc6()
    {
        Console.Write("Running SwiftCallbackFunc6: ");
        ExceptionDispatchInfo ex = null;
        long val = SwiftCallbackFunc6(&SwiftCallbackFunc6Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((long)2428870998079250366, val);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func71fS2is5UInt8V_s5Int16VSus6UInt32Vs4Int8VAkESfSiAiESfs5Int64VSdAKSiAgMS2dtXE_tF")]
    private static extern nint SwiftCallbackFunc7(delegate* unmanaged[Swift]<byte, short, nuint, uint, sbyte, sbyte, byte, float, nint, uint, byte, float, long, double, sbyte, nint, short, long, double, double, SwiftSelf, nint> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static nint SwiftCallbackFunc7Callback(byte a0, short a1, nuint a2, uint a3, sbyte a4, sbyte a5, byte a6, float a7, nint a8, uint a9, byte a10, float a11, long a12, double a13, sbyte a14, nint a15, short a16, long a17, double a18, double a19, SwiftSelf self)
    {
        try
        {
            Assert.Equal((byte)134, a0);
            Assert.Equal((short)-32369, a1);
            Assert.Equal((nuint)unchecked((nuint)8380717554783122608), a2);
            Assert.Equal((uint)1860099027, a3);
            Assert.Equal((sbyte)-6, a4);
            Assert.Equal((sbyte)86, a5);
            Assert.Equal((byte)32, a6);
            Assert.Equal((float)1160734, a7);
            Assert.Equal((nint)unchecked((nint)6413974004534568863), a8);
            Assert.Equal((uint)835905835, a9);
            Assert.Equal((byte)1, a10);
            Assert.Equal((float)7455267, a11);
            Assert.Equal((long)6652417171359975799, a12);
            Assert.Equal((double)3767979765576223, a13);
            Assert.Equal((sbyte)-92, a14);
            Assert.Equal((nint)unchecked((nint)2188807859088864320), a15);
            Assert.Equal((short)22602, a16);
            Assert.Equal((long)6695605905030342661, a17);
            Assert.Equal((double)3516012226643358, a18);
            Assert.Equal((double)1125481383704537, a19);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return unchecked((nint)4420963390330795075);
    }

    [Fact]
    public static void TestSwiftCallbackFunc7()
    {
        Console.Write("Running SwiftCallbackFunc7: ");
        ExceptionDispatchInfo ex = null;
        nint val = SwiftCallbackFunc7(&SwiftCallbackFunc7Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((nint)unchecked((nint)4420963390330795075), val);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func81fs4Int8VAESd_Sds6UInt64Vs6UInt16VSds6UInt32VAiEs5UInt8Vs5Int16VAGSfAItXE_tF")]
    private static extern sbyte SwiftCallbackFunc8(delegate* unmanaged[Swift]<double, double, ulong, ushort, double, uint, ushort, sbyte, byte, short, ulong, float, ushort, SwiftSelf, sbyte> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static sbyte SwiftCallbackFunc8Callback(double a0, double a1, ulong a2, ushort a3, double a4, uint a5, ushort a6, sbyte a7, byte a8, short a9, ulong a10, float a11, ushort a12, SwiftSelf self)
    {
        try
        {
            Assert.Equal((double)378554201505534, a0);
            Assert.Equal((double)1650526878176435, a1);
            Assert.Equal((ulong)7125767448027274022, a2);
            Assert.Equal((ushort)19812, a3);
            Assert.Equal((double)1173178493312463, a4);
            Assert.Equal((uint)416842395, a5);
            Assert.Equal((ushort)46360, a6);
            Assert.Equal((sbyte)-60, a7);
            Assert.Equal((byte)107, a8);
            Assert.Equal((short)-2849, a9);
            Assert.Equal((ulong)3245727696885859461, a10);
            Assert.Equal((float)3340085, a11);
            Assert.Equal((ushort)24776, a12);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 30;
    }

    [Fact]
    public static void TestSwiftCallbackFunc8()
    {
        Console.Write("Running SwiftCallbackFunc8: ");
        ExceptionDispatchInfo ex = null;
        sbyte val = SwiftCallbackFunc8(&SwiftCallbackFunc8Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((sbyte)30, val);
        Console.WriteLine("OK");
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s22SwiftCallbackAbiStress05swiftB5Func91fS2ds5Int16V_SiAEs5UInt8Vs6UInt32Vs5Int64Vs6UInt64Vs6UInt16VAOs4Int8VAkgqKSfAQtXE_tF")]
    private static extern double SwiftCallbackFunc9(delegate* unmanaged[Swift]<short, nint, short, byte, uint, long, ulong, ushort, ushort, sbyte, long, byte, sbyte, long, float, sbyte, SwiftSelf, double> func, void* funcContext);

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static double SwiftCallbackFunc9Callback(short a0, nint a1, short a2, byte a3, uint a4, long a5, ulong a6, ushort a7, ushort a8, sbyte a9, long a10, byte a11, sbyte a12, long a13, float a14, sbyte a15, SwiftSelf self)
    {
        try
        {
            Assert.Equal((short)4555, a0);
            Assert.Equal((nint)unchecked((nint)4720638462358523954), a1);
            Assert.Equal((short)30631, a2);
            Assert.Equal((byte)123, a3);
            Assert.Equal((uint)2112687301, a4);
            Assert.Equal((long)1804058604961822948, a5);
            Assert.Equal((ulong)8772179036715198777, a6);
            Assert.Equal((ushort)54948, a7);
            Assert.Equal((ushort)29928, a8);
            Assert.Equal((sbyte)-36, a9);
            Assert.Equal((long)7573525757641791389, a10);
            Assert.Equal((byte)239, a11);
            Assert.Equal((sbyte)-71, a12);
            Assert.Equal((long)7143939705627605769, a13);
            Assert.Equal((float)2647713, a14);
            Assert.Equal((sbyte)-7, a15);
        }
        catch (Exception ex)
        {
            *(ExceptionDispatchInfo*)self.Value = ExceptionDispatchInfo.Capture(ex);
        }

        return 3088996708692961;
    }

    [Fact]
    public static void TestSwiftCallbackFunc9()
    {
        Console.Write("Running SwiftCallbackFunc9: ");
        ExceptionDispatchInfo ex = null;
        double val = SwiftCallbackFunc9(&SwiftCallbackFunc9Callback, &ex);
        if (ex != null)
            ex.Throw();

        Assert.Equal((double)3088996708692961, val);
        Console.WriteLine("OK");
    }

}
