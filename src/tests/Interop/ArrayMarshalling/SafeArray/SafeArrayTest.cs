// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

#pragma warning disable CS0612, CS0618

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class SafeArrayMarshallingTest
{
    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled))]
    [SkipOnMono("Requires COM support")]
    public static int TestEntryPoint()
    {
        try
        {
            var boolArray = new bool[] { true, false, true, false, false, true };
            SafeArrayNative.XorBoolArray(boolArray, out var xorResult);
            Assert.Equal(XorArray(boolArray), xorResult);

            var decimalArray = new decimal[] { 1.5M, 30.2M, 6432M, 12.5832M };
            SafeArrayNative.MeanDecimalArray(decimalArray, out var meanDecimalValue);
            Assert.Equal(decimalArray.Average(), meanDecimalValue);

            SafeArrayNative.SumCurrencyArray(decimalArray, out var sumCurrencyValue);
            Assert.Equal(decimalArray.Sum(), sumCurrencyValue);

            var strings = new [] {"ABCDE", "12345", "Microsoft"};
            var reversedStrings = strings.Select(str => Reverse(str)).ToArray();

            var ansiTest = strings.ToArray();
            SafeArrayNative.ReverseStringsAnsi(ansiTest);
            AssertExtensions.CollectionEqual(reversedStrings, ansiTest);

            var unicodeTest = strings.ToArray();
            SafeArrayNative.ReverseStringsUnicode(unicodeTest);
            AssertExtensions.CollectionEqual(reversedStrings, unicodeTest);

            var bstrTest = strings.ToArray();
            SafeArrayNative.ReverseStringsBSTR(bstrTest);
            AssertExtensions.CollectionEqual(reversedStrings, bstrTest);

            var blittableRecords = new SafeArrayNative.BlittableRecord[]
            {
                new SafeArrayNative.BlittableRecord { a = 1 },
                new SafeArrayNative.BlittableRecord { a = 5 },
                new SafeArrayNative.BlittableRecord { a = 7 },
                new SafeArrayNative.BlittableRecord { a = 3 },
                new SafeArrayNative.BlittableRecord { a = 9 },
                new SafeArrayNative.BlittableRecord { a = 15 },
            };
            AssertExtensions.CollectionEqual(blittableRecords, SafeArrayNative.CreateSafeArrayOfRecords(blittableRecords));

            var nonBlittableRecords = boolArray.Select(b => new SafeArrayNative.NonBlittableRecord{ b = b }).ToArray();
            AssertExtensions.CollectionEqual(nonBlittableRecords, SafeArrayNative.CreateSafeArrayOfRecords(nonBlittableRecords));

            var objects = new object[] { new object(), new object(), new object() };
            SafeArrayNative.VerifyIUnknownArray(objects);
            SafeArrayNative.VerifyIDispatchArray(objects);

            var variantInts = new object[] {1, 2, 3, 4, 5, 6, 7, 8, 9};

            SafeArrayNative.MeanVariantIntArray(variantInts, out var variantMean);
            Assert.Equal(variantInts.OfType<int>().Average(), variantMean);

            var dates = new DateTime[] { new DateTime(2008, 5, 1), new DateTime(2010, 1, 1) };
            SafeArrayNative.DistanceBetweenDates(dates, out var numDays);
            Assert.Equal((dates[1] - dates[0]).TotalDays, numDays);

            SafeArrayNative.XorBoolArrayInStruct(
                new SafeArrayNative.StructWithSafeArray
                {
                    values = boolArray
                },
                out var structXor);

            Assert.Equal(XorArray(boolArray), structXor);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 101;
        }
        return 100;
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/129581", TestRuntimes.Mono)]
    public static void MultidimensionalIntArray()
    {
        const int rows = 3;
        const int cols = 4;

        SafeArrayNative.Create2DIntSafeArray(rows, cols, out int[,] result);

        Assert.Equal(rows, result.GetLength(0));
        Assert.Equal(cols, result.GetLength(1));

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Assert.Equal(r * cols + c, result[r, c]);
            }
        }
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled))]
    public static void MultidimensionalIntArrayRoundTrip()
    {
        const int rows = 3;
        const int cols = 4;

        SafeArrayNative.Create2DIntSafeArray(rows, cols, out int[,] result);

        SafeArrayNative.Verify2DIntSafeArray(result, rows, cols);
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/129581", TestRuntimes.Mono)]
    public static void MultidimensionalBoolArray()
    {
        const int rows = 2;
        const int cols = 3;

        SafeArrayNative.Create2DBoolSafeArray(rows, cols, out bool[,] result);

        Assert.Equal(rows, result.GetLength(0));
        Assert.Equal(cols, result.GetLength(1));

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Assert.Equal((r + c) % 2 == 0, result[r, c]);
            }
        }
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled))]
    public static void MultidimensionalBoolArrayRoundTrip()
    {
        const int rows = 2;
        const int cols = 3;

        SafeArrayNative.Create2DBoolSafeArray(rows, cols, out bool[,] result);

        SafeArrayNative.Verify2DBoolSafeArray(result, rows, cols);
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/129581", TestRuntimes.Mono)]
    public static void MultidimensionalStringArray()
    {
        const int rows = 2;
        const int cols = 3;

        SafeArrayNative.Create2DStringSafeArray(rows, cols, out string[,] result);

        Assert.Equal(rows, result.GetLength(0));
        Assert.Equal(cols, result.GetLength(1));

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Assert.Equal($"{r},{c}", result[r, c]);
            }
        }
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/129581", TestRuntimes.Mono)]
    public static void MultidimensionalStringArrayRoundTrip()
    {
        const int rows = 2;
        const int cols = 3;

        SafeArrayNative.Create2DStringSafeArray(rows, cols, out string[,] result);

        SafeArrayNative.Verify2DStringSafeArray(result, rows, cols);
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled), nameof(TestLibrary.PlatformDetection.Is64BitProcess))]
    [SkipOnMono("Requires COM support")]
    public static void MultidimensionalIntPtrArray_MismatchedNativeElementSize()
    {
        // Native VT_I4 SAFEARRAY (4-byte elements) -> managed IntPtr[,] (8-byte elements).
        Assert.Throws<SafeArrayTypeMismatchException>(() => SafeArrayNative.Create2DIntSafeArrayAsIntPtr(2, 2, out _));

        // Managed IntPtr[,] (8-byte elements) -> native VT_I4 SAFEARRAY (4-byte elements).
        Assert.Throws<SafeArrayTypeMismatchException>(() => SafeArrayNative.Verify2DIntSafeArrayAsIntPtr(new IntPtr[2, 2], 2, 2));
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled))]
    public static void VariantArrayByRefElementIsReplacedWhenManagedTypeChanges()
    {
        SafeArrayNative.ReplaceVariantArrayElement(ReplaceVariantArrayElement, out int nativeValue, out ushort elementType);

        Assert.Equal(4, nativeValue);
        Assert.Equal((ushort)VarEnum.VT_BSTR, elementType);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int AutoDualArrayReader([MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] AutoDualArrayElement[] values);

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled))]
    public static unsafe void AutoDualClassArrayMarshalsDefaultInterface()
    {
        AutoDualArrayReader read = Marshal.GetDelegateForFunctionPointer<AutoDualArrayReader>(
            (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr*, int>)&HasInterfacePointer);

        Assert.Equal(1, read([new AutoDualArrayElement()]));
        SafeArrayNative.VerifyAutoDualArray([new AutoDualArrayElement()]);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int HasInterfacePointer(IntPtr* values) => values[0] != IntPtr.Zero ? 1 : 0;

    private static void ReplaceVariantArrayElement(object[] values)
    {
        values[0] = "7";
    }

    private static bool XorArray(bool[] values)
    {
        bool retVal = false;
        foreach (var item in values)
        {
            retVal ^= item;
        }
        return retVal;
    }

    private static string Reverse(string s)
    {
        var chars = s.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}

[ComVisible(true)]
[Guid("A7CC0C2F-8E38-46D8-B53B-BA845484A713")]
[ClassInterface(ClassInterfaceType.AutoDual)]
public class AutoDualArrayElement
{
    public int GetValue() => 42;
}

class SafeArrayNative
{
    public struct StructWithSafeArray
    {
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BOOL)]
        public bool[] values;
    }

    public struct BlittableRecord
    {
        public int a;

        public override string ToString() => $"BlittableRecord: {a}";
    }

    public struct NonBlittableRecord
    {
        public bool b;
        public override string ToString() => $"NonBlittableRecord: {b}";
    }

    [DllImport(nameof(SafeArrayNative))]
    [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_RECORD)]
    private static extern  BlittableRecord[] CreateSafeArrayOfRecords(
        BlittableRecord[] records,
        int numElements
    );

    public static BlittableRecord[] CreateSafeArrayOfRecords(BlittableRecord[] records)
    {
        return CreateSafeArrayOfRecords(records, records.Length);
    }

    [DllImport(nameof(SafeArrayNative), EntryPoint = "CreateSafeArrayOfNonBlittableRecords")]
    [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_RECORD)]
    private static extern NonBlittableRecord[] CreateSafeArrayOfRecords(
        NonBlittableRecord[] records,
        int numElements
    );

    public static NonBlittableRecord[] CreateSafeArrayOfRecords(NonBlittableRecord[] records)
    {
        return CreateSafeArrayOfRecords(records, records.Length);
    }

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void XorBoolArray(
        [MarshalAs(UnmanagedType.SafeArray)] bool[] values,
        out bool result
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void MeanDecimalArray(
        [MarshalAs(UnmanagedType.SafeArray)] decimal[] values,
        out decimal result
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void SumCurrencyArray(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_CY)] decimal[] values,
        [MarshalAs(UnmanagedType.Currency)] out decimal result
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false, EntryPoint = "ReverseStrings")]
    public static extern void ReverseStringsAnsi(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_LPSTR), In, Out] string[] strings
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false, EntryPoint = "ReverseStrings")]
    public static extern void ReverseStringsUnicode(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_LPWSTR), In, Out] string[] strings
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false, EntryPoint = "ReverseStrings")]
    public static extern void ReverseStringsBSTR(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR), In, Out] string[] strings
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false, EntryPoint = "VerifyInterfaceArray")]
    private static extern void VerifyInterfaceArrayIUnknown(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)] object[] objects,
        short expectedVarType
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false, EntryPoint = "VerifyInterfaceArray")]
    private static extern void VerifyInterfaceArrayIDispatch(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_DISPATCH)] object[] objects,
        short expectedVarType
    );

    public static void VerifyIUnknownArray(object[] objects)
    {
        VerifyInterfaceArrayIUnknown(objects, (short)VarEnum.VT_UNKNOWN);
    }

    public static void VerifyIDispatchArray(object[] objects)
    {
        VerifyInterfaceArrayIDispatch(objects, (short)VarEnum.VT_DISPATCH);
    }

    [DllImport(nameof(SafeArrayNative), PreserveSig = false, EntryPoint = "VerifyInterfaceArray")]
    private static extern void VerifyAutoDualSafeArray(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)] AutoDualArrayElement[] values,
        short expectedVarType);

    public static void VerifyAutoDualArray(AutoDualArrayElement[] values)
    {
        VerifyAutoDualSafeArray(values, (short)VarEnum.VT_UNKNOWN);
    }

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void MeanVariantIntArray(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)]
        object[] objects,
        out int result
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void VariantArrayCallback(
        [In, Out, MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] object[] values
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void ReplaceVariantArrayElement(
        VariantArrayCallback callback,
        out int nativeValue,
        out ushort elementType
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void DistanceBetweenDates(
        [MarshalAs(UnmanagedType.SafeArray)] DateTime[] dates,
        out double result
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void XorBoolArrayInStruct(StructWithSafeArray str, out bool result);

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void Create2DIntSafeArray(
        int rows,
        int cols,
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] out int[,] result
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void Verify2DIntSafeArray(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] int[,] array,
        int rows,
        int cols
    );

    // Deliberately mismatched declarations on 64-bit - the managed element type is IntPtr (8 bytes)
    // while the native SAFEARRAY element size is 4 bytes.
    [DllImport(nameof(SafeArrayNative), PreserveSig = false, EntryPoint = "Create2DIntSafeArray")]
    public static extern void Create2DIntSafeArrayAsIntPtr(
        int rows,
        int cols,
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] out IntPtr[,] result
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false, EntryPoint = "Verify2DIntSafeArray")]
    public static extern void Verify2DIntSafeArrayAsIntPtr(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] IntPtr[,] array,
        int rows,
        int cols
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void Create2DBoolSafeArray(
        int rows,
        int cols,
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BOOL)] out bool[,] result
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void Verify2DBoolSafeArray(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BOOL)] bool[,] array,
        int rows,
        int cols
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void Create2DStringSafeArray(
        int rows,
        int cols,
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[,] result
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void Verify2DStringSafeArray(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] string[,] array,
        int rows,
        int cols
    );
}
