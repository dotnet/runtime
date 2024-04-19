// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    // Polyfill CoreLib internal interfaces and methods
    // Define necessary members only

    internal interface IUtfChar<TSelf> :
        IEquatable<TSelf>
        where TSelf : unmanaged, IUtfChar<TSelf>
    {
        public static abstract TSelf CastFrom(byte value);

        public static abstract TSelf CastFrom(char value);

        public static abstract TSelf CastFrom(int value);

        public static abstract TSelf CastFrom(uint value);

        public static abstract TSelf CastFrom(ulong value);

        public static abstract uint CastToUInt32(TSelf value);
    }

#pragma warning disable CA1067 // Polyfill only type
    internal readonly struct Utf16Char(char ch) : IUtfChar<Utf16Char>
#pragma warning restore CA1067
    {
        private readonly char value = ch;

        public static Utf16Char CastFrom(byte value) => new((char)value);
        public static Utf16Char CastFrom(char value) => new(value);
        public static Utf16Char CastFrom(int value) => new((char)value);
        public static Utf16Char CastFrom(uint value) => new((char)value);
        public static Utf16Char CastFrom(ulong value) => new((char)value);
        public static uint CastToUInt32(Utf16Char value) => value.value;
        public bool Equals(Utf16Char other) => value == other.value;
    }

    internal static partial class Number
    {
        internal static bool AllowHyphenDuringParsing(this NumberFormatInfo info)
        {
            string negativeSign = info.NegativeSign;
            return negativeSign.Length == 1 &&
                   negativeSign[0] switch
                   {
                       '\u2012' or         // Figure Dash
                       '\u207B' or         // Superscript Minus
                       '\u208B' or         // Subscript Minus
                       '\u2212' or         // Minus Sign
                       '\u2796' or         // Heavy Minus Sign
                       '\uFE63' or         // Small Hyphen-Minus
                       '\uFF0D' => true,   // Fullwidth Hyphen-Minus
                       _ => false
                   };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PositiveSignTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(Utf16Char));
            return MemoryMarshal.Cast<char, TChar>(info.PositiveSign);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> NegativeSignTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(Utf16Char));
            return MemoryMarshal.Cast<char, TChar>(info.NegativeSign);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> CurrencySymbolTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(Utf16Char));
            return MemoryMarshal.Cast<char, TChar>(info.CurrencySymbol);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PercentSymbolTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(Utf16Char));
            return MemoryMarshal.Cast<char, TChar>(info.PercentSymbol);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PerMilleSymbolTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(Utf16Char));
            return MemoryMarshal.Cast<char, TChar>(info.PerMilleSymbol);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> CurrencyDecimalSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(Utf16Char));
            return MemoryMarshal.Cast<char, TChar>(info.CurrencyDecimalSeparator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> CurrencyGroupSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(Utf16Char));
            return MemoryMarshal.Cast<char, TChar>(info.CurrencyGroupSeparator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> NumberDecimalSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(Utf16Char));
            return MemoryMarshal.Cast<char, TChar>(info.NumberDecimalSeparator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> NumberGroupSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(Utf16Char));
            return MemoryMarshal.Cast<char, TChar>(info.NumberGroupSeparator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PercentDecimalSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(Utf16Char));
            return MemoryMarshal.Cast<char, TChar>(info.PercentDecimalSeparator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PercentGroupSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(Utf16Char));
            return MemoryMarshal.Cast<char, TChar>(info.PercentGroupSeparator);
        }
    }
}
