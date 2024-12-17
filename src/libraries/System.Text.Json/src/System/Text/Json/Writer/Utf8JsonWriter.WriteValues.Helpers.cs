// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        private const byte LengthMask =         0b000_000_11;
        private const byte EncodingMask =       0b000_111_00;

        private const byte Utf8EncodingFlag =   0b000_001_00;
        private const byte Utf16EncodingFlag =  0b000_010_00;

        private bool TryGetPartialUtf8CodePoint(out ReadOnlySpan<byte> codePointBytes)
        {
            ReadOnlySpan<byte> partialCodePointBytes = PartialCodePointRaw;
            Debug.Assert(partialCodePointBytes.Length == 4);

            if ((partialCodePointBytes[3] & Utf8EncodingFlag) == 0)
            {
                codePointBytes = ReadOnlySpan<byte>.Empty;
                return false;
            }

            int length = partialCodePointBytes[3] & LengthMask;
            Debug.Assert((uint)length < 4);

            codePointBytes = partialCodePointBytes.Slice(0, length);
            return true;
        }

        private bool TryGetPartialUtf16CodePoint(out ReadOnlySpan<char> codePointChars)
        {
            ReadOnlySpan<byte> partialCodePointBytes = PartialCodePointRaw;
            Debug.Assert(partialCodePointBytes.Length == 4);

            if ((partialCodePointBytes[3] & Utf16EncodingFlag) == 0)
            {
                codePointChars = ReadOnlySpan<char>.Empty;
                return false;
            }

            int length = partialCodePointBytes[3] & LengthMask;
            Debug.Assert(length == 2 || length == 0);

            codePointChars = MemoryMarshal.Cast<byte, char>(partialCodePointBytes.Slice(0, length));
            return true;
        }

        private void SetPartialUtf8CodePoint(ReadOnlySpan<byte> bytes)
        {
            Debug.Assert(bytes.Length <= 3);

            Span<byte> partialCodePointBytes = PartialCodePointRaw;

            bytes.CopyTo(partialCodePointBytes);
            partialCodePointBytes[3] = (byte)(bytes.Length | Utf8EncodingFlag);
        }

        private void SetPartialUtf16CodePoint(ReadOnlySpan<char> bytes)
        {
            Debug.Assert(bytes.Length <= 1);

            Span<byte> partialCodePointBytes = PartialCodePointRaw;

            bytes.CopyTo(MemoryMarshal.Cast<byte, char>(partialCodePointBytes));
            partialCodePointBytes[3] = (byte)((2 * bytes.Length) | Utf16EncodingFlag);
        }

        private bool HasPartialCodePoint => (PartialCodePointRaw[3] & LengthMask) != 0;

        private void ClearPartialCodePoint() => PartialCodePointRaw[3] = 0;

        private void WriteInvalidPartialCodePoint()
        {
            ReadOnlySpan<byte> partialCodePointBytes = PartialCodePointRaw;
            Debug.Assert(partialCodePointBytes.Length == 4);

            int length = partialCodePointBytes[3] & LengthMask;

            switch (partialCodePointBytes[3] & EncodingMask)
            {
                case Utf8EncodingFlag:
                    Debug.Assert((uint)length < 4);
                    WriteStringSegmentEscape(partialCodePointBytes.Slice(0, length), true);
                    break;
                case Utf16EncodingFlag:
                    Debug.Assert(length == 0 || length == 2);
                    WriteStringSegmentEscape(MemoryMarshal.Cast<byte, char>(partialCodePointBytes.Slice(0, length)), true);
                    break;
                default:
                    Debug.Fail("Encoding not recognized.");
                    break;
            }
        }

        private void ValidateNotWithinUnfinalizedString()
        {
            Debug.Assert(!HasPartialCodePoint);

            if (_tokenType == StringSegmentSentinel)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWriteWithinString, currentDepth: default, maxDepth: _options.MaxDepth, token: default, _tokenType);
            }
        }

        private void ValidateWritingValue()
        {
            Debug.Assert(!_options.SkipValidation);

            // Make sure a new value is not attempted within an unfinalized string.
            ValidateNotWithinUnfinalizedString();

            if (_inObject)
            {
                if (_tokenType != JsonTokenType.PropertyName)
                {
                    Debug.Assert(_tokenType != JsonTokenType.None && _tokenType != JsonTokenType.StartArray);
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWriteValueWithinObject, currentDepth: default, maxDepth: _options.MaxDepth, token: default, _tokenType);
                }
            }
            else
            {
                Debug.Assert(_tokenType != JsonTokenType.PropertyName);

                // It is more likely for CurrentDepth to not equal 0 when writing valid JSON, so check that first to rely on short-circuiting and return quickly.
                if (CurrentDepth == 0 && _tokenType != JsonTokenType.None)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWriteValueAfterPrimitiveOrClose, currentDepth: default, maxDepth: _options.MaxDepth, token: default, _tokenType);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Base64EncodeAndWrite(ReadOnlySpan<byte> bytes, Span<byte> output)
        {
            Span<byte> destination = output.Slice(BytesPending);
            OperationStatus status = Base64.EncodeToUtf8(bytes, destination, out int consumed, out int written);
            Debug.Assert(status == OperationStatus.Done);
            Debug.Assert(consumed == bytes.Length);
            BytesPending += written;
        }
    }
}
