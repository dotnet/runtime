// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace GenUnicodeProp
{
    /// <summary>
    /// Contains information about a code point's numeric representation
    /// and the manner in which it's treated for grapheme cluster segmentation
    /// purposes.
    /// </summary>
    internal sealed class NumericGraphemeInfo : IEquatable<NumericGraphemeInfo>
    {
        public readonly (int decimalDigitValue,
            int digitValue,
            double numericValue,
            GraphemeClusterBreakProperty graphemeClusterBreakProperty) _data;

        public NumericGraphemeInfo(CodePoint codePoint)
        {
            _data.decimalDigitValue = codePoint.DecimalDigitValue;
            _data.digitValue = codePoint.DigitValue;
            _data.numericValue = codePoint.NumericValue;
            _data.graphemeClusterBreakProperty = codePoint.GraphemeClusterBreakProperty;
        }

        public override bool Equals(object obj) => Equals(obj as NumericGraphemeInfo);

        public bool Equals(NumericGraphemeInfo other)
        {
            return !(other is null) && this._data.Equals(other._data);
        }

        public override int GetHashCode()
        {
            return _data.GetHashCode();
        }

        public static byte[] ToDigitBytes(NumericGraphemeInfo input)
        {
            // Bits 4 .. 7 contain (decimalDigitValue + 1).
            // Bits 0 .. 3 contain (digitValue + 1).
            // This means that each nibble will have a value 0x0 .. 0xa, inclusive.

            int adjustedDecimalDigitValue = input._data.decimalDigitValue + 1;
            int adjustedDigitValue = input._data.digitValue + 1;

            return new byte[] { (byte)((adjustedDecimalDigitValue << 4) | adjustedDigitValue) };
        }

        public static byte[] ToNumericBytes(NumericGraphemeInfo input)
        {
            byte[] bytes = new byte[sizeof(double)];
            double value = input._data.numericValue;
            BinaryPrimitives.WriteUInt64LittleEndian(bytes, Unsafe.As<double, ulong>(ref value));
            return bytes;
        }

        public static byte[] ToGraphemeBytes(NumericGraphemeInfo input)
        {
            return new byte[] { checked((byte)input._data.graphemeClusterBreakProperty) };
        }
    }
}
