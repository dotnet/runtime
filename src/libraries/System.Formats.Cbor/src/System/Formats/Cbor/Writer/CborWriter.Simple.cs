// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        // CBOR RFC 8949 says: if NaN is an allowed value, and there is no intent to support NaN payloads or signaling NaNs, the protocol needs to pick a single representation, typically 0xf97e00. If that simple choice is not possible, specific attention will be needed for NaN handling.
        // In this implementation "that simple choice is not possible" for CTAP2 mode (RequiresPreservingFloatPrecision), in which "representations of any floating-point values are not changed".
        private const ushort PositiveQNaNBitsHalf = 0x7e00;

        // Implements major type 7 encoding per https://tools.ietf.org/html/rfc7049#section-2.1

        /// <summary>Writes a single-precision floating point number (major type 7).</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException"><para>Writing a new value exceeds the definite length of the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The major type of the encoded value is not permitted in the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The written data is not accepted under the current conformance mode.</para></exception>
        public void WriteSingle(float value)
        {
            if (!CborConformanceModeHelpers.RequiresPreservingFloatPrecision(ConformanceMode) &&
                 TryConvertSingleToHalf(value, out var half))
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
        /// <exception cref="InvalidOperationException"><para>Writing a new value exceeds the definite length of the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The major type of the encoded value is not permitted in the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The written data is not accepted under the current conformance mode.</para></exception>
        public void WriteDouble(double value)
        {
            if (!CborConformanceModeHelpers.RequiresPreservingFloatPrecision(ConformanceMode) &&
                 TryConvertDoubleToSingle(value, out float single))
            {
                if (TryConvertSingleToHalf(single, out var half))
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
            CborHelpers.WriteSingleBigEndian(_buffer.AsSpan(_offset), value);
            _offset += sizeof(float);
            AdvanceDataItemCounters();
        }

        private void WriteDoubleCore(double value)
        {
            EnsureWriteCapacity(1 + sizeof(double));
            WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional64BitData));
            CborHelpers.WriteDoubleBigEndian(_buffer.AsSpan(_offset), value);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertDoubleToSingle(double value, out float result)
        {
            result = (float)value;
            return double.IsNaN(value) || BitConverter.DoubleToInt64Bits(result) == BitConverter.DoubleToInt64Bits(value);
        }
    }
}
