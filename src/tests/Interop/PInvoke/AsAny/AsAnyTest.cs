// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using Xunit;

#pragma warning disable CS0612, CS0618

public struct A
{
    public long a;
    public long b;
}

public struct AsAnyField
{
    [MarshalAs(UnmanagedType.AsAny)]
    public object intArray;
}

public class AsAnyTests
{
    private const char mappableChar = (char)0x2075;
    private const char unmappableChar = (char)0x7777;
    private const char NormalChar1 = '0';
    private const char NormalChar2 = '\n';

    private static readonly string MappableString = "" + NormalChar1 + mappableChar + NormalChar2;
    private static readonly string UnmappableString = "" + NormalChar1 + unmappableChar + NormalChar2;

    [DllImport("AsAnyNative")]
    public static extern bool PassArraySbyte(
        [MarshalAs(UnmanagedType.AsAny)] object sbyteArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object sbyteArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object sbyteArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object sbyteArray_Out,
        sbyte[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern bool PassArrayByte(
        [MarshalAs(UnmanagedType.AsAny)] object byteArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object byteArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object byteArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object byteArray_Out,
        byte[] expected,
        int len
    );

    //Short
    [DllImport("AsAnyNative")]
    public static extern bool PassArrayShort(
        [MarshalAs(UnmanagedType.AsAny)] object shortArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object shortArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object shortArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object shortArray_Out,
        short[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern bool PassArrayUshort(
        [MarshalAs(UnmanagedType.AsAny)] object ushortArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object ushortArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object ushortArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object ushortArray_Out,
        ushort[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern bool PassArrayInt(
        [MarshalAs(UnmanagedType.AsAny)] object intArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object intArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object intArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object intArray_Out,
        int[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern bool PassArrayUint(
        [In, MarshalAs(UnmanagedType.AsAny)] object uintArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object uintArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object uintArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object uintArray_Out,
        uint[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern bool PassArrayLong(
        [In, MarshalAs(UnmanagedType.AsAny)] object longArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object longArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object longArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object longArray_Out,
        long[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern bool PassArrayUlong(
        [In, MarshalAs(UnmanagedType.AsAny)] object ulongArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object ulongArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object ulongArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object ulongArray_Out,
        ulong[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern bool PassArraySingle(
        [In, MarshalAs(UnmanagedType.AsAny)] object singleArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object singleArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object singleArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object singleArray_Out,
        float[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern bool PassArrayDouble(
        [In, MarshalAs(UnmanagedType.AsAny)] object doubleArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object doubleArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object doubleArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object doubleArray_Out,
        double[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern bool PassArrayChar(
        [In, MarshalAs(UnmanagedType.AsAny)] object charArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object charArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object charArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object charArray_Out,
        char[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern bool PassArrayBool(
        [In, MarshalAs(UnmanagedType.AsAny)] object boolArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object boolArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object boolArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object boolArray_Out,
        bool[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern bool PassArrayIntPtr(
        [In, MarshalAs(UnmanagedType.AsAny)] object intPtrArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object intPtrArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object intPtrArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object intPtrArray_Out,
        IntPtr[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern bool PassArrayUIntPtr(
        [In, MarshalAs(UnmanagedType.AsAny)] object uIntPtrArray,
        [In, MarshalAs(UnmanagedType.AsAny)] object uIntPtrArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object uIntPtrArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object uIntPtrArray_Out,
        UIntPtr[] expected,
        int len
    );

    [DllImport("AsAnyNative")]
    public static extern long PassLayout(
    [MarshalAs(UnmanagedType.AsAny)] Object i);

    [DllImport("AsAnyNative", EntryPoint = "PassUnicodeStr", CharSet = CharSet.Unicode,
    BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool PassUnicodeStrTT(
    [MarshalAs(UnmanagedType.AsAny)]
    Object i);

    [DllImport("AsAnyNative", EntryPoint = "PassUnicodeStr", CharSet = CharSet.Unicode,
    BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool PassUnicodeStrFT(
    [MarshalAs(UnmanagedType.AsAny)]
    Object i);

    [DllImport("AsAnyNative", EntryPoint = "PassUnicodeStr", CharSet = CharSet.Unicode,
   BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool PassUnicodeStrFF(
    [MarshalAs(UnmanagedType.AsAny)]
    Object i);

    [DllImport("AsAnyNative", EntryPoint = "PassAnsiStr", CharSet = CharSet.Ansi,
    BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool PassAnsiStrTT(
    [MarshalAs(UnmanagedType.AsAny)]
    Object i, bool isIncludeUnMappableChar);

    [DllImport("AsAnyNative", EntryPoint = "PassAnsiStr", CharSet = CharSet.Ansi,
    BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool PassAnsiStrFT(
    [MarshalAs(UnmanagedType.AsAny)]
    Object i, bool isIncludeUnMappableChar);

    [DllImport("AsAnyNative", EntryPoint = "PassAnsiStr", CharSet = CharSet.Ansi,
    BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool PassAnsiStrFF(
    [MarshalAs(UnmanagedType.AsAny)]
    Object i, bool isIncludeUnMappableChar);

    [DllImport("AsAnyNative", EntryPoint = "PassUnicodeStrbd", CharSet = CharSet.Unicode,
    BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool PassUnicodeStrbdTT(
        [In, MarshalAs(UnmanagedType.AsAny)] object strbd_In);

    [DllImport("AsAnyNative", EntryPoint = "PassUnicodeStrbd", CharSet = CharSet.Unicode,
    BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool PassUnicodeStrbdFT(
        [In, MarshalAs(UnmanagedType.AsAny)] object strbd_In);

    [DllImport("AsAnyNative", EntryPoint = "PassUnicodeStrbd", CharSet = CharSet.Unicode,
   BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool PassUnicodeStrbdFF(
        [In, MarshalAs(UnmanagedType.AsAny)] object strbd_In);

    [DllImport("AsAnyNative", EntryPoint = "PassAnsiStrbd", CharSet = CharSet.Ansi,
    BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool PassAnsiStrbdTT(
        [In, MarshalAs(UnmanagedType.AsAny)] object strbd_In,
         bool isIncludeUnMappableChar);

    [DllImport("AsAnyNative", EntryPoint = "PassAnsiStrbd", CharSet = CharSet.Ansi,
    BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool PassAnsiStrbdFT(
        [In, MarshalAs(UnmanagedType.AsAny)] object strbd_In,
         bool isIncludeUnMappableChar);

    [DllImport("AsAnyNative", EntryPoint = "PassAnsiStrbd", CharSet = CharSet.Ansi,
    BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool PassAnsiStrbdFF(
        [In, MarshalAs(UnmanagedType.AsAny)] object strbd_In,
         bool isIncludeUnMappableChar);

    [DllImport("AsAnyNative", EntryPoint = "PassUnicodeCharArray", CharSet = CharSet.Unicode,
    BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool PassUnicodeCharArrayTT(
        [In, MarshalAs(UnmanagedType.AsAny)] object CharArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object CharArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object CharArray_Out);


    [DllImport("AsAnyNative", EntryPoint = "PassUnicodeCharArray", CharSet = CharSet.Unicode,
    BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool PassUnicodeCharArrayFT(
        [In, MarshalAs(UnmanagedType.AsAny)] object CharArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object CharArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object CharArray_Out);

    [DllImport("AsAnyNative", EntryPoint = "PassUnicodeCharArray", CharSet = CharSet.Unicode,
   BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool PassUnicodeCharArrayFF(
        [In, MarshalAs(UnmanagedType.AsAny)] object CharArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object CharArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object CharArray_Out);

    [DllImport("AsAnyNative", EntryPoint = "PassAnsiCharArray", CharSet = CharSet.Ansi,
    BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool PassAnsiCharArrayTT(
        [In, MarshalAs(UnmanagedType.AsAny)] object CharArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object CharArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object CharArray_Out,
        bool isIncludeUnMappableChar);

    [DllImport("AsAnyNative", EntryPoint = "PassAnsiCharArray", CharSet = CharSet.Ansi,
    BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool PassAnsiCharArrayFT(
        [In, MarshalAs(UnmanagedType.AsAny)] object CharArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object CharArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object CharArray_Out,
        bool isIncludeUnMappableChar);

    [DllImport("AsAnyNative", EntryPoint = "PassAnsiCharArray", CharSet = CharSet.Ansi,
    BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool PassAnsiCharArrayFF(
        [In, MarshalAs(UnmanagedType.AsAny)] object CharArray_In,
        [In, Out, MarshalAs(UnmanagedType.AsAny)] object CharArray_InOut,
        [Out, MarshalAs(UnmanagedType.AsAny)] object CharArray_Out,
        bool isIncludeUnMappableChar);

    [DllImport("AsAnyNative", EntryPoint = "PassMixStruct")]
    public static extern bool PassMixStruct(AsAnyField mix);

    [Fact]
    [SkipOnMono("needs triage")]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/169", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        try
        {
            TestSByteArray();
            TestByteArray();
            TestInt16Array();
            TestUInt16Array();
            TestInt32Array();
            TestUInt32Array();
            TestLongArray();
            TestULongArray();
            TestSingleArray();
            TestDoubleArray();
            TestCharArray();
            TestBoolArray();
            TestIntPtrArray();
            TestUIntPtrArray();
            TestLayout();
            RunAsAnyFieldTests();
            TestUnicodeString();
            TestUnicodeStringArray();
            TestUnicodeStringBuilder();
            if (OperatingSystem.IsWindows())
            {
                RunBestFitMappingTests();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 101;
        }
        return 100;
    }

    private static void RunAsAnyFieldTests()
    {
        Assert.Throws<TypeLoadException>(() => PassMixStruct(new AsAnyField()));
    }

    private static void RunBestFitMappingTests()
    {
        if (System.Globalization.CultureInfo.CurrentCulture.Name != "en-US")
        {
            Console.WriteLine($"Non-US English platforms are not supported.\nPassing {nameof(RunBestFitMappingTests)} without running.");
            return;
        }

        TestAnsiStringBestFitMapping();
        TestAnsiStringBuilder();
        TestAnsiStringArrayBestFitMapping();
    }

    private static void TestAnsiStringArrayBestFitMapping()
    {
        string unMappableAnsiStr_back = "" + NormalChar2 + (char)0x003f + NormalChar1;
        string mappableAnsiStr_back = "" + NormalChar2 + (char)0x0035 + NormalChar1;

        char[] unMappableCharArray_In = new char[3];
        char[] unMappableCharArray_InOut = new char[3];
        char[] unMappableCharArray_Out = new char[3];
        char[] mappableCharArray_In = new char[3];
        char[] mappableCharArray_InOut = new char[3];
        char[] mappableCharArray_Out = new char[3];

        CharArrayInit(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out,
           mappableCharArray_In, mappableCharArray_InOut, mappableCharArray_Out, UnmappableString, MappableString);
        Assert.Throws<ArgumentException>(() => PassAnsiCharArrayTT(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out, true));

        CharArrayInit(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out,
           mappableCharArray_In, mappableCharArray_InOut, mappableCharArray_Out, UnmappableString, MappableString);
        Assert.True(PassAnsiCharArrayTT(mappableCharArray_In, mappableCharArray_InOut, mappableCharArray_Out, false));
        AssertExtensions.CollectionEqual(mappableAnsiStr_back.ToCharArray(), mappableCharArray_InOut);
        AssertExtensions.CollectionEqual(mappableAnsiStr_back.ToCharArray(), mappableCharArray_Out);

        CharArrayInit(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out,
           mappableCharArray_In, mappableCharArray_InOut, mappableCharArray_Out, UnmappableString, MappableString);
        Assert.Throws<ArgumentException>(() => PassAnsiCharArrayFT(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out, true));

        CharArrayInit(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out,
           mappableCharArray_In, mappableCharArray_InOut, mappableCharArray_Out, UnmappableString, MappableString);
        Assert.Throws<ArgumentException>(() => PassAnsiCharArrayFT(mappableCharArray_In, mappableCharArray_InOut, mappableCharArray_Out, false));

        CharArrayInit(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out,
           mappableCharArray_In, mappableCharArray_InOut, mappableCharArray_Out, UnmappableString, MappableString);
        Assert.True(PassAnsiCharArrayFF(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out, true));
        AssertExtensions.CollectionEqual(unMappableAnsiStr_back.ToCharArray(), unMappableCharArray_InOut);
        AssertExtensions.CollectionEqual(unMappableAnsiStr_back.ToCharArray(), unMappableCharArray_Out);
    }

    private static void TestUnicodeStringArray()
    {
        string unMappableUnicodeStr_back = "" + NormalChar2 + unmappableChar + NormalChar1;

        char[] unMappableCharArray_In = new char[3];
        char[] unMappableCharArray_InOut = new char[3];
        char[] unMappableCharArray_Out = new char[3];
        char[] mappableCharArray_In = new char[3];
        char[] mappableCharArray_InOut = new char[3];
        char[] mappableCharArray_Out = new char[3];

        CharArrayInit(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out,
            mappableCharArray_In, mappableCharArray_InOut, mappableCharArray_Out, UnmappableString, MappableString);
        Assert.True(PassUnicodeCharArrayTT(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out));
        AssertExtensions.CollectionEqual(unMappableUnicodeStr_back.ToCharArray(), unMappableCharArray_InOut);
        AssertExtensions.CollectionEqual(unMappableUnicodeStr_back.ToCharArray(), unMappableCharArray_Out);

        CharArrayInit(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out,
            mappableCharArray_In, mappableCharArray_InOut, mappableCharArray_Out, UnmappableString, MappableString);
        Assert.True(PassUnicodeCharArrayFT(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out));
        AssertExtensions.CollectionEqual(unMappableUnicodeStr_back.ToCharArray(), unMappableCharArray_InOut);
        AssertExtensions.CollectionEqual(unMappableUnicodeStr_back.ToCharArray(), unMappableCharArray_Out);

        CharArrayInit(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out,
            mappableCharArray_In, mappableCharArray_InOut, mappableCharArray_Out, UnmappableString, MappableString);
        Assert.True(PassUnicodeCharArrayFF(unMappableCharArray_In, unMappableCharArray_InOut, unMappableCharArray_Out));
        AssertExtensions.CollectionEqual(unMappableUnicodeStr_back.ToCharArray(), unMappableCharArray_InOut);
        AssertExtensions.CollectionEqual(unMappableUnicodeStr_back.ToCharArray(), unMappableCharArray_Out);
    }

    private static void TestAnsiStringBuilder()
    {
        StringBuilder unMappableStrbd = new StringBuilder(UnmappableString);
        StringBuilder mappableStrbd = new StringBuilder(MappableString);

        Assert.Throws<ArgumentException>(() => PassAnsiStrbdTT(unMappableStrbd, true));
        Assert.True(PassAnsiStrbdTT(mappableStrbd, false));
        Assert.Throws<ArgumentException>(() => PassAnsiStrbdFT(unMappableStrbd, true));
        Assert.Throws<ArgumentException>(() => PassAnsiStrbdFT(mappableStrbd, false));
        Assert.True(PassAnsiStrbdFF(unMappableStrbd, true));
    }

    private static void TestUnicodeStringBuilder()
    {
        StringBuilder unMappableStrbd = new StringBuilder(UnmappableString);
        Assert.True(PassUnicodeStrbdTT(unMappableStrbd));
        Assert.True(PassUnicodeStrbdFT(unMappableStrbd));
        Assert.True(PassUnicodeStrbdFF(unMappableStrbd));
    }

    private static void TestAnsiStringBestFitMapping()
    {
        Assert.Throws<ArgumentException>(() => PassAnsiStrTT(UnmappableString, true));
        Assert.True(PassAnsiStrTT(MappableString, false));
        Assert.Throws<ArgumentException>(() => PassAnsiStrFT(UnmappableString, true));
        Assert.Throws<ArgumentException>(() => PassAnsiStrFT(MappableString, false));
        Assert.True(PassAnsiStrFF(UnmappableString, true));
    }

    private static void TestUnicodeString()
    {
        Assert.True(PassUnicodeStrTT(UnmappableString));
        Assert.True(PassUnicodeStrFT(UnmappableString));
        Assert.True(PassUnicodeStrFF(UnmappableString));
    }

    private static void TestUIntPtrArray()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for UIntPtr array ");
        UIntPtr[] uIntPtrArray = new UIntPtr[] { new UIntPtr(0), new UIntPtr(1), new UIntPtr(2) };
        UIntPtr[] uIntPtrArray_In = new UIntPtr[] { new UIntPtr(0), new UIntPtr(1), new UIntPtr(2) };
        UIntPtr[] uIntPtrArray_InOut = new UIntPtr[] { new UIntPtr(0), new UIntPtr(1), new UIntPtr(2) };
        UIntPtr[] uIntPtrArray_Out = new UIntPtr[] { new UIntPtr(0), new UIntPtr(1), new UIntPtr(2) };
        UIntPtr[] uIntPtrArray_Back = new UIntPtr[] { new UIntPtr(10), new UIntPtr(11), new UIntPtr(12) };
        UIntPtr[] expected = new UIntPtr[] { new UIntPtr(0), new UIntPtr(1), new UIntPtr(2) };
        Assert.True(PassArrayUIntPtr(uIntPtrArray, uIntPtrArray_In, uIntPtrArray_InOut, uIntPtrArray_Out, expected, 3));
        AssertExtensions.CollectionEqual(uIntPtrArray_Back, uIntPtrArray_InOut);
        AssertExtensions.CollectionEqual(uIntPtrArray_Back, uIntPtrArray_Out);
    }

    private static void TestIntPtrArray()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for IntPtr array ");
        IntPtr[] intPtrArray = new IntPtr[] { new IntPtr(0), new IntPtr(1), new IntPtr(2) };
        IntPtr[] intPtrArray_In = new IntPtr[] { new IntPtr(0), new IntPtr(1), new IntPtr(2) };
        IntPtr[] intPtrArray_InOut = new IntPtr[] { new IntPtr(0), new IntPtr(1), new IntPtr(2) };
        IntPtr[] intPtrArray_Out = new IntPtr[] { new IntPtr(0), new IntPtr(1), new IntPtr(2) };
        IntPtr[] intPtrArray_Back = new IntPtr[] { new IntPtr(10), new IntPtr(11), new IntPtr(12) };
        IntPtr[] expected = new IntPtr[] { new IntPtr(0), new IntPtr(1), new IntPtr(2) };
        Assert.True(PassArrayIntPtr(intPtrArray, intPtrArray_In, intPtrArray_InOut, intPtrArray_Out, expected, 3));
        AssertExtensions.CollectionEqual(intPtrArray_Back, intPtrArray_InOut);
        AssertExtensions.CollectionEqual(intPtrArray_Back, intPtrArray_Out);
    }

    private static void TestBoolArray()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for bool array ");
        bool[] boolArray = new bool[] { true, false, false };
        bool[] boolArray_In = new bool[] { true, false, false };
        bool[] boolArray_InOut = new bool[] { true, false, false };
        bool[] boolArray_Out = new bool[] { true, false, false };
        bool[] boolArray_Back = new bool[] { false, true, true };
        Assert.True(PassArrayBool(boolArray, boolArray_In, boolArray_InOut, boolArray_Out, new bool[] { true, false, false }, 3));
        AssertExtensions.CollectionEqual(boolArray_Back, boolArray_InOut);
        AssertExtensions.CollectionEqual(boolArray_Back, boolArray_Out);
    }

    private static void TestCharArray()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for char array ");
        char[] charArray = new char[] { 'a', 'b', 'c' };
        char[] charArray_In = new char[] { 'a', 'b', 'c' };
        char[] charArray_InOut = new char[] { 'a', 'b', 'c' };
        char[] charArray_Out = new char[] { 'a', 'b', 'c' };
        char[] charArray_Back = new char[] { 'd', 'e', 'f' };
        Assert.True(PassArrayChar(charArray, charArray_In, charArray_InOut, charArray_Out, new char[] { 'a', 'b', 'c' }, 3));
        AssertExtensions.CollectionEqual(charArray_Back, charArray_InOut);
        AssertExtensions.CollectionEqual(charArray_Back, charArray_Out);
    }

    private static void TestDoubleArray()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for double array ");
        double[] doubleArray = new double[] { 0.0, 1.1, 2.2 };
        double[] doubleArray_In = new double[] { 0.0, 1.1, 2.2 };
        double[] doubleArray_InOut = new double[] { 0.0, 1.1, 2.2 };
        double[] doubleArray_Out = new double[] { 0.0, 1.1, 2.2 };
        double[] doubleArray_Back = new double[] { 10.0, 11.1, 12.2 };
        Assert.True(PassArrayDouble(doubleArray, doubleArray_In, doubleArray_InOut, doubleArray_Out, new double[] { 0.0, 1.1, 2.2 }, 3));
        AssertExtensions.CollectionEqual(doubleArray_Back, doubleArray_InOut);
        AssertExtensions.CollectionEqual(doubleArray_Back, doubleArray_Out);
    }

    private static void TestSingleArray()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for float array ");
        float[] singleArray = new float[] { 0, 1, 2 };
        float[] singleArray_In = new float[] { 0, 1, 2 };
        float[] singleArray_InOut = new float[] { 0, 1, 2 };
        float[] singleArray_Out = new float[] { 0, 1, 2 };
        float[] singleArray_Back = new float[] { 10, 11, 12 };
        Assert.True(PassArraySingle(singleArray, singleArray_In, singleArray_InOut, singleArray_Out, new float[] { 0, 1, 2 }, 3));
        AssertExtensions.CollectionEqual(singleArray_Back, singleArray_InOut);
        AssertExtensions.CollectionEqual(singleArray_Back, singleArray_Out);
    }

    private static void TestULongArray()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for ulong array ");
        ulong[] ulongArray = new ulong[] { 0, 1, 2 };
        ulong[] ulongArray_In = new ulong[] { 0, 1, 2 };
        ulong[] ulongArray_InOut = new ulong[] { 0, 1, 2 };
        ulong[] ulongArray_Out = new ulong[] { 0, 1, 2 };
        ulong[] ulongArray_Back = new ulong[] { 10, 11, 12 };
        Assert.True(PassArrayUlong(ulongArray, ulongArray_In, ulongArray_InOut, ulongArray_Out, new ulong[] { 0, 1, 2 }, 3));
        AssertExtensions.CollectionEqual(ulongArray_Back, ulongArray_InOut);
        AssertExtensions.CollectionEqual(ulongArray_Back, ulongArray_Out);
    }

    private static void TestLongArray()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for long array ");
        long[] longArray = new long[] { 0, 1, 2 };
        long[] longArray_In = new long[] { 0, 1, 2 };
        long[] longArray_InOut = new long[] { 0, 1, 2 };
        long[] longArray_Out = new long[] { 0, 1, 2 };
        long[] longArray_Back = new long[] { 10, 11, 12 };
        Assert.True(PassArrayLong(longArray, longArray_In, longArray_InOut, longArray_Out, new long[] { 0, 1, 2 }, 3));
        AssertExtensions.CollectionEqual(longArray_Back, longArray_InOut);
        AssertExtensions.CollectionEqual(longArray_Back, longArray_Out);
    }

    private static void TestUInt32Array()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for uint array ");
        uint[] uintArray = new uint[] { 0, 1, 2 };
        uint[] uintArray_In = new uint[] { 0, 1, 2 };
        uint[] uintArray_InOut = new uint[] { 0, 1, 2 };
        uint[] uintArray_Out = new uint[] { 0, 1, 2 };
        uint[] uintArray_Back = new uint[] { 10, 11, 12 };
        Assert.True(PassArrayUint(uintArray, uintArray_In, uintArray_InOut, uintArray_Out, new uint[] { 0, 1, 2 }, 3));
        AssertExtensions.CollectionEqual(uintArray_Back, uintArray_InOut);
        AssertExtensions.CollectionEqual(uintArray_Back, uintArray_Out);
    }

    private static void TestInt32Array()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for Int array ");
        int[] intArray = new int[] { 0, 1, 2 };
        int[] intArray_In = new int[] { 0, 1, 2 };
        int[] intArray_InOut = new int[] { 0, 1, 2 };
        int[] intArray_Out = new int[] { 0, 1, 2 };
        int[] intArray_Back = new int[] { 10, 11, 12 };
        Assert.True(PassArrayInt(intArray, intArray_In, intArray_InOut, intArray_Out, new int[] { 0, 1, 2 }, 3));
        AssertExtensions.CollectionEqual(intArray_Back, intArray_InOut);
        AssertExtensions.CollectionEqual(intArray_Back, intArray_Out);
    }

    private static void TestUInt16Array()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for ushort array ");
        ushort[] ushortArray = new ushort[] { 0, 1, 2 };
        ushort[] ushortArray_In = new ushort[] { 0, 1, 2 };
        ushort[] ushortArray_InOut = new ushort[] { 0, 1, 2 };
        ushort[] ushortArray_Out = new ushort[] { 0, 1, 2 };
        ushort[] ushortArray_Back = new ushort[] { 10, 11, 12 };
        Assert.True(PassArrayUshort(ushortArray, ushortArray_In, ushortArray_InOut, ushortArray_Out, new ushort[] { 0, 1, 2 }, 3));
        AssertExtensions.CollectionEqual(ushortArray_Back, ushortArray_InOut);
        AssertExtensions.CollectionEqual(ushortArray_Back, ushortArray_Out);
    }

    private static void TestInt16Array()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for short array ");
        short[] shortArray = new short[] { -1, 0, 1 };
        short[] shortArray_In = new short[] { -1, 0, 1 };
        short[] shortArray_InOut = new short[] { -1, 0, 1 };
        short[] shortArray_Out = new short[] { -1, 0, 1 };
        short[] shortArray_Back = new short[] { 9, 10, 11 };
        Assert.True(PassArrayShort(shortArray, shortArray_In, shortArray_InOut, shortArray_Out, new short[] { -1, 0, 1 }, 3));
        AssertExtensions.CollectionEqual(shortArray_Back, shortArray_InOut);
        AssertExtensions.CollectionEqual(shortArray_Back, shortArray_Out);
    }

    private static void TestByteArray()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for byte array ");
        byte[] byteArray = new byte[] { 0, 1, 2 };
        byte[] byteArray_In = new byte[] { 0, 1, 2 };
        byte[] byteArray_InOut = new byte[] { 0, 1, 2 };
        byte[] byteArray_Out = new byte[] { 0, 1, 2 };
        byte[] byteArray_Back = new byte[] { 10, 11, 12 };
        Assert.True(PassArrayByte(byteArray, byteArray_In, byteArray_InOut, byteArray_Out, new byte[] { 0, 1, 2 }, 3));
        AssertExtensions.CollectionEqual(byteArray_Back, byteArray_InOut);
        AssertExtensions.CollectionEqual(byteArray_Back, byteArray_Out);
    }

    private static void TestSByteArray()
    {
        Console.WriteLine("Scenario : Checking Marshal AsAny for sbyte array ");
        sbyte[] sbyteArray = new sbyte[] { -1, 0, 1 };
        sbyte[] sbyteArray_In = new sbyte[] { -1, 0, 1 };
        sbyte[] sbyteArray_InOut = new sbyte[] { -1, 0, 1 };
        sbyte[] sbyteArray_Out = new sbyte[] { -1, 0, 1 };
        sbyte[] sbyteArray_Back = new sbyte[] { 9, 10, 11 };
        Assert.True(PassArraySbyte(sbyteArray, sbyteArray_In, sbyteArray_InOut, sbyteArray_Out, new sbyte[] {-1, 0, 1}, 3));
        AssertExtensions.CollectionEqual(sbyteArray_Back, sbyteArray_InOut);
        AssertExtensions.CollectionEqual(sbyteArray_Back, sbyteArray_Out);
    }

    public static void TestLayout() {
        Console.WriteLine("Scenario: Running Layout Tests:");
        Console.WriteLine("------------------------");

        A layoutStruct = new A
        {
            a = 12,
            b = 3
        };

        Assert.Equal(layoutStruct.b, PassLayout(layoutStruct));
        Console.WriteLine("------------------------");
    }

    static void CharArrayInit(char[] unMappableCharArray_In, char[] unMappableCharArray_InOut, char[] unMappableCharArray_Out,
        char[] mappableCharArray_In, char[] mappableCharArray_InOut, char[] mappableCharArray_Out,
        string unMappableStr, string mappableStr)
    {
        char[] u = unMappableStr.ToCharArray();
        char[] m = mappableStr.ToCharArray();
        for (int i = 0; i < 3; i++)
        {
            unMappableCharArray_In[i] = u[i];
            unMappableCharArray_InOut[i] = u[i];
            unMappableCharArray_Out[i] = u[i];
            mappableCharArray_In[i] = m[i];
            mappableCharArray_InOut[i] = m[i];
            mappableCharArray_Out[i] = m[i];
        }
    }
}
