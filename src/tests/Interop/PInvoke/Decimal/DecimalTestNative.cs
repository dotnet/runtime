// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

#pragma warning disable 0618
static class DecimalTestNative
{
    public struct DecimalWrapper
    {
        public decimal dec;
    };

    public struct CurrencyWrapper
    {
        [MarshalAs(UnmanagedType.Currency)]
        public decimal currency;
    };

    [DllImport(nameof(DecimalTestNative))]
    public static extern decimal CreateDecimalFromInt(int i);
    [DllImport(nameof(DecimalTestNative))]
    [return:MarshalAs(UnmanagedType.LPStruct)]
    public static extern decimal CreateLPDecimalFromInt(int i);
    [DllImport(nameof(DecimalTestNative))]
    public static extern DecimalWrapper CreateWrappedDecimalFromInt(int i);
    [DllImport(nameof(DecimalTestNative))]
    public static extern bool DecimalEqualToInt(decimal dec, int i);
    [DllImport(nameof(DecimalTestNative))]
    public static extern bool LPDecimalEqualToInt([MarshalAs(UnmanagedType.LPStruct)] decimal dec, int i);
    [DllImport(nameof(DecimalTestNative))]
    public static extern bool WrappedDecimalEqualToInt(DecimalWrapper dec, int i);
    [DllImport(nameof(DecimalTestNative))]
    public static extern bool ValidateAndChangeDecimal(ref decimal dec, int expected, int newValue);
    [DllImport(nameof(DecimalTestNative))]
    public static extern bool ValidateAndChangeWrappedDecimal(ref DecimalWrapper dec, int expected, int newValue);
    [DllImport(nameof(DecimalTestNative))]
    public static extern bool ValidateAndChangeLPDecimal([MarshalAs(UnmanagedType.LPStruct)] ref decimal dec, int expected, int newValue);
    [DllImport(nameof(DecimalTestNative))]
    public static extern void GetDecimalForInt(int i, out decimal dec);
    [DllImport(nameof(DecimalTestNative))]
    public static extern void GetLPDecimalForInt(int i, [MarshalAs(UnmanagedType.LPStruct)] out decimal dec);
    [DllImport(nameof(DecimalTestNative))]
    public static extern void GetWrappedDecimalForInt(int i, out DecimalWrapper dec);
    [DllImport(nameof(DecimalTestNative))]
    [return:MarshalAs(UnmanagedType.Currency)]
    public static extern decimal CreateCurrencyFromInt(int i);
    [DllImport(nameof(DecimalTestNative))]
    public static extern CurrencyWrapper CreateWrappedCurrencyFromInt(int i);
    [DllImport(nameof(DecimalTestNative))]
    public static extern bool CurrencyEqualToInt([MarshalAs(UnmanagedType.Currency)] decimal currency, int i);
    [DllImport(nameof(DecimalTestNative))]
    public static extern bool WrappedCurrencyEqualToInt(CurrencyWrapper currency, int i);
    [DllImport(nameof(DecimalTestNative))]
    public static extern bool ValidateAndChangeCurrency([MarshalAs(UnmanagedType.Currency)] ref decimal currency, int expected, int newValue);
    [DllImport(nameof(DecimalTestNative))]
    public static extern bool ValidateAndChangeWrappedCurrency(ref CurrencyWrapper currency, int expected, int newValue);
    [DllImport(nameof(DecimalTestNative))]
    public static extern void GetCurrencyForInt(int i, [MarshalAs(UnmanagedType.Currency)] out decimal currency);
    [DllImport(nameof(DecimalTestNative))]
    public static extern void GetWrappedCurrencyForInt(int i, out CurrencyWrapper currency);

    public delegate void DecimalCallback(decimal dec);
    public delegate void LPDecimalCallback([MarshalAs(UnmanagedType.LPStruct)] decimal dec);
    public delegate void CurrencyCallback([MarshalAs(UnmanagedType.Currency)] decimal dec);

    [DllImport(nameof(DecimalTestNative))]
    public static extern void PassThroughDecimalToCallback(decimal dec, DecimalCallback callback);
    [DllImport(nameof(DecimalTestNative))]
    public static extern void PassThroughLPDecimalToCallback([MarshalAs(UnmanagedType.LPStruct)] decimal dec, LPDecimalCallback callback);
    [DllImport(nameof(DecimalTestNative))]
    public static extern void PassThroughCurrencyToCallback([MarshalAs(UnmanagedType.Currency)] decimal dec, CurrencyCallback callback);
}
