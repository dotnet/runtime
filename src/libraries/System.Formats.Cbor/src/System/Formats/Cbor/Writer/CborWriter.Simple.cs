// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        // Implements major type 7 encoding per https://tools.ietf.org/html/rfc7049#section-2.1

        /// <summary>Writes a half-precision floating point number (major type 7).</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteHalf(Half value)
        {
            EnsureWriteCapacity(1 + HalfHelpers.SizeOfHalf);
            WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional16BitData));
            HalfHelpers.WriteHalfBigEndian(_buffer.AsSpan(_offset), value);
            _offset += HalfHelpers.SizeOfHalf;
            AdvanceDataItemCounters();
        }

        /// <summary>Writes a single-precision floating point number (major type 7).</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteSingle(float value)
        {
            if (!CborConformanceModeHelpers.RequiresPreservingFloatPrecision(ConformanceMode) &&
                 FloatSerializationHelpers.TryConvertSingleToHalf(value, out Half half))
            {
                WriteHalf(half);
            }
            else
            {
                WriteSingleCore(value);
            }
        }

        /// <summary>Writes a double-precision floating point number (major type 7).</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteDouble(double value)
        {
            if (!CborConformanceModeHelpers.RequiresPreservingFloatPrecision(ConformanceMode) &&
                 FloatSerializationHelpers.TryConvertDoubleToSingle(value, out float single))
            {
                if (FloatSerializationHelpers.TryConvertSingleToHalf(single, out Half half))
                {
                    WriteHalf(half);
                }
                else
                {
                    WriteSingleCore(single);
                }
            }
            else
            {
                WriteDoubleCore(value);
            }
        }

        private void WriteSingleCore(float value)
        {
            EnsureWriteCapacity(1 + sizeof(float));
            WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional32BitData));
            BinaryPrimitives.WriteSingleBigEndian(_buffer.AsSpan(_offset), value);
            _offset += sizeof(float);
            AdvanceDataItemCounters();
        }

        private void WriteDoubleCore(double value)
        {
            EnsureWriteCapacity(1 + sizeof(double));
            WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional64BitData));
            BinaryPrimitives.WriteDoubleBigEndian(_buffer.AsSpan(_offset), value);
            _offset += sizeof(double);
            AdvanceDataItemCounters();
        }

        /// <summary>Writes a boolean value (major type 7).</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteBoolean(bool value)
        {
            WriteSimpleValue(value ? CborSimpleValue.True : CborSimpleValue.False);
        }

        /// <summary>Writes a <see langword="null" /> value (major type 7).</summary>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteNull()
        {
            WriteSimpleValue(CborSimpleValue.Null);
        }

        /// <summary>Writes a simple value encoding (major type 7).</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="value" /> parameter is in the invalid 24-31 range.</exception>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteSimpleValue(CborSimpleValue value)
        {
            if (value < (CborSimpleValue)CborAdditionalInfo.Additional8BitData)
            {
                EnsureWriteCapacity(1);
                WriteInitialByte(new CborInitialByte(CborMajorType.Simple, (CborAdditionalInfo)value));
            }
            else if (value <= (CborSimpleValue)CborAdditionalInfo.IndefiniteLength &&
                     CborConformanceModeHelpers.RequireCanonicalSimpleValueEncodings(ConformanceMode))
            {
                throw new ArgumentOutOfRangeException(SR.Format(SR.Cbor_ConformanceMode_InvalidSimpleValueEncoding, ConformanceMode));
            }
            else
            {
                EnsureWriteCapacity(2);
                WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional8BitData));
                _buffer[_offset++] = (byte)value;
            }

            AdvanceDataItemCounters();
        }

        private static class FloatSerializationHelpers
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryConvertDoubleToSingle(double value, out float result)
            {
                result = (float)value;
                return BitConverter.DoubleToInt64Bits(result) == BitConverter.DoubleToInt64Bits(value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryConvertSingleToHalf(float value, out Half result)
            {
                result = (Half)value;
                return BitConverter.SingleToInt32Bits((float)result) == BitConverter.SingleToInt32Bits(value);
            }
        }
    }
}
