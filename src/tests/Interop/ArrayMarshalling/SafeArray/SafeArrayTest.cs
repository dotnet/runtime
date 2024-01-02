// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
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
    }

    public struct NonBlittableRecord
    {
        public bool b;
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

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void MeanVariantIntArray(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)]
        object[] objects,
        out int result
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void DistanceBetweenDates(
        [MarshalAs(UnmanagedType.SafeArray)] DateTime[] dates,
        out double result
    );

    [DllImport(nameof(SafeArrayNative), PreserveSig = false)]
    public static extern void XorBoolArrayInStruct(StructWithSafeArray str, out bool result);
}
