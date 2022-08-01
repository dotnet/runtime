// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        // Implements major type 2,3 encoding per https://tools.ietf.org/html/rfc7049#section-2.1

        // keeps track of chunk offsets for written indefinite-length string ranges
        private List<(int Offset, int Length)>? _currentIndefiniteLengthStringRanges;

        /// <summary>Writes a buffer as a byte string encoding (major type 2).</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentNullException">The provided value cannot be <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException"><para>Writing a new value exceeds the definite length of the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The major type of the encoded value is not permitted in the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The written data is not accepted under the current conformance mode.</para></exception>
        public void WriteByteString(byte[] value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            WriteByteString(value.AsSpan());
        }

        /// <summary>Writes a buffer as a byte string encoding (major type 2).</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException"><para>Writing a new value exceeds the definite length of the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The major type of the encoded value is not permitted in the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The written data is not accepted under the current conformance mode.</para></exception>
        public void WriteByteString(ReadOnlySpan<byte> value)
        {
            WriteUnsignedInteger(CborMajorType.ByteString, (ulong)value.Length);
            EnsureWriteCapacity(value.Length);

            if (ConvertIndefiniteLengthEncodings && _currentMajorType == CborMajorType.ByteString)
            {
                // operation is writing chunk of an indefinite-length string
                // the string will be converted to a definite-length encoding later,
                // so we need to record the ranges of each chunk
                Debug.Assert(_currentIndefiniteLengthStringRanges != null);
                _currentIndefiniteLengthStringRanges.Add((_offset, value.Length));
            }

            value.CopyTo(_buffer.AsSpan(_offset));
            _offset += value.Length;
            AdvanceDataItemCounters();
        }

        /// <summary>Writes the start of an indefinite-length byte string (major type 2).</summary>
        /// <exception cref="InvalidOperationException"><para>Writing a new value exceeds the definite length of the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The major type of the encoded value is not permitted in the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The written data is not accepted under the current conformance mode.</para></exception>
        /// <remarks>
        /// Pushes a context where definite-length chunks of the same major type can be written.
        /// In canonical conformance modes, the writer will reject indefinite-length writes unless
        /// the <see cref="ConvertIndefiniteLengthEncodings" /> flag is enabled.
        /// </remarks>
        public void WriteStartIndefiniteLengthByteString()
        {
            if (!ConvertIndefiniteLengthEncodings && CborConformanceModeHelpers.RequiresDefiniteLengthItems(ConformanceMode))
            {
                throw new InvalidOperationException(SR.Format(SR.Cbor_ConformanceMode_IndefiniteLengthItemsNotSupported, ConformanceMode));
            }

            if (ConvertIndefiniteLengthEncodings)
            {
                // Writer does not allow indefinite-length encodings.
                // We need to keep track of chunk offsets to convert to
                // a definite-length encoding once writing is complete.
                _currentIndefiniteLengthStringRanges ??= new List<(int, int)>();
            }

            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.ByteString, CborAdditionalInfo.IndefiniteLength));
            PushDataItem(CborMajorType.ByteString, definiteLength: null);
        }

        /// <summary>Writes the end of an indefinite-length byte string (major type 2).</summary>
        /// <exception cref="InvalidOperationException">The written data is not accepted under the current conformance mode.</exception>
        public void WriteEndIndefiniteLengthByteString()
        {
            PopDataItem(CborMajorType.ByteString);
            AdvanceDataItemCounters();
        }

        /// <summary>Writes a buffer as a UTF-8 string encoding (major type 3).</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentNullException">The provided value cannot be <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">The supplied string is not a valid UTF-8 encoding, which is not permitted under the current conformance mode.</exception>
        /// <exception cref="InvalidOperationException"><para>Writing a new value exceeds the definite length of the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The major type of the encoded value is not permitted in the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The written data is not accepted under the current conformance mode.</para></exception>
        public void WriteTextString(string value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            WriteTextString(value.AsSpan());
        }

        /// <summary>Writes a buffer as a UTF-8 string encoding (major type 3).</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">The supplied string is not a valid UTF-8 encoding, which is not permitted under the current conformance mode.</exception>
        /// <exception cref="InvalidOperationException"><para>Writing a new value exceeds the definite length of the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The major type of the encoded value is not permitted in the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The written data is not accepted under the current conformance mode.</para></exception>
        public void WriteTextString(ReadOnlySpan<char> value)
        {
            Encoding utf8Encoding = CborConformanceModeHelpers.GetUtf8Encoding(ConformanceMode);

            int length;
            try
            {
                length = CborHelpers.GetByteCount(utf8Encoding, value);
            }
            catch (EncoderFallbackException e)
            {
                throw new ArgumentException(SR.Cbor_Writer_InvalidUtf8String, e);
            }

            WriteUnsignedInteger(CborMajorType.TextString, (ulong)length);
            EnsureWriteCapacity(length);

            if (ConvertIndefiniteLengthEncodings && _currentMajorType == CborMajorType.TextString)
            {
                // operation is writing chunk of an indefinite-length string
                // the string will be converted to a definite-length encoding later,
                // so we need to record the ranges of each chunk
                Debug.Assert(_currentIndefiniteLengthStringRanges != null);
                _currentIndefiniteLengthStringRanges.Add((_offset, value.Length));
            }

            CborHelpers.GetBytes(utf8Encoding, value, _buffer.AsSpan(_offset, length));
            _offset += length;
            AdvanceDataItemCounters();
        }

        /// <summary>Writes the start of an indefinite-length UTF-8 string (major type 3).</summary>
        /// <exception cref="InvalidOperationException"><para>Writing a new value exceeds the definite length of the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The major type of the encoded value is not permitted in the parent data item.</para>
        /// <para>-or-</para>
        /// <para>The written data is not accepted under the current conformance mode.</para></exception>
        /// <remarks>
        /// Pushes a context where definite-length chunks of the same major type can be written.
        /// In canonical conformance modes, the writer will reject indefinite-length writes unless
        /// the <see cref="ConvertIndefiniteLengthEncodings" /> flag is enabled.
        /// </remarks>
        public void WriteStartIndefiniteLengthTextString()
        {
            if (!ConvertIndefiniteLengthEncodings && CborConformanceModeHelpers.RequiresDefiniteLengthItems(ConformanceMode))
            {
                throw new InvalidOperationException(SR.Format(SR.Cbor_ConformanceMode_IndefiniteLengthItemsNotSupported, ConformanceMode));
            }

            if (ConvertIndefiniteLengthEncodings)
            {
                // Writer does not allow indefinite-length encodings.
                // We need to keep track of chunk offsets to convert to
                // a definite-length encoding once writing is complete.
                _currentIndefiniteLengthStringRanges ??= new List<(int, int)>();
            }

            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.TextString, CborAdditionalInfo.IndefiniteLength));
            PushDataItem(CborMajorType.TextString, definiteLength: null);
        }

        /// <summary>Writes the end of an indefinite-length UTF-8 string (major type 3).</summary>
        /// <exception cref="InvalidOperationException">The written data is not accepted under the current conformance mode.</exception>
        public void WriteEndIndefiniteLengthTextString()
        {
            PopDataItem(CborMajorType.TextString);
            AdvanceDataItemCounters();
        }

        // perform an in-place conversion of an indefinite-length encoding into an equivalent definite-length
        private void PatchIndefiniteLengthString(CborMajorType type)
        {
            Debug.Assert(type == CborMajorType.ByteString || type == CborMajorType.TextString);
            Debug.Assert(_currentIndefiniteLengthStringRanges != null);

            int initialOffset = _offset;

            // calculate the definite length of the concatenated string
            int definiteLength = 0;
            foreach ((int _, int length) in _currentIndefiniteLengthStringRanges)
            {
                definiteLength += length;
            }

            Span<byte> buffer = _buffer.AsSpan();

            // copy chunks to a temporary buffer
            byte[] tempBuffer = s_bufferPool.Rent(definiteLength);
            Span<byte> tempSpan = tempBuffer.AsSpan(0, definiteLength);

            Span<byte> s = tempSpan;
            foreach ((int offset, int length) in _currentIndefiniteLengthStringRanges)
            {
                buffer.Slice(offset, length).CopyTo(s);
                s = s.Slice(length);
            }
            Debug.Assert(s.IsEmpty);

            // write back to the original buffer
            _offset = _frameOffset - 1;
            WriteUnsignedInteger(type, (ulong)definiteLength);
            tempSpan.CopyTo(buffer.Slice(_offset, definiteLength));
            _offset += definiteLength;

            // clean up
            s_bufferPool.Return(tempBuffer);
            _currentIndefiniteLengthStringRanges.Clear();
            buffer.Slice(_offset, initialOffset - _offset).Clear();
        }
    }
}
