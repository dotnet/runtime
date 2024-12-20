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
        private const byte PartialCodePointLengthMask =         0b000_000_11;
        private const byte PartialCodePointEncodingMask =       0b000_111_00;

        private const byte PartialCodePointUtf8EncodingFlag =   0b000_001_00;
        private const byte PartialCodePointUtf16EncodingFlag =  0b000_010_00;

        private bool TryGetPartialUtf8CodePoint(out ReadOnlySpan<byte> codePointBytes)
        {
            if ((_partialCodePointFlags & PartialCodePointUtf8EncodingFlag) == 0)
            {
                codePointBytes = [];
                return false;
            }

            ReadOnlySpan<byte> partialCodePointBytes = PartialCodePointRaw;
            Debug.Assert(partialCodePointBytes.Length == 3);

            int length = _partialCodePointFlags & PartialCodePointLengthMask;
            Debug.Assert((uint)length < 4);

            codePointBytes = partialCodePointBytes.Slice(0, length);
            return true;
        }

        private bool TryGetPartialUtf16CodePoint(out ReadOnlySpan<char> codePointChars)
        {
            if ((_partialCodePointFlags & PartialCodePointUtf16EncodingFlag) == 0)
            {
                codePointChars = [];
                return false;
            }

            ReadOnlySpan<byte> partialCodePointBytes = PartialCodePointRaw;
            Debug.Assert(partialCodePointBytes.Length == 3);

            int length = _partialCodePointFlags & PartialCodePointLengthMask;
            Debug.Assert(length is 2 or 0);

            codePointChars = MemoryMarshal.Cast<byte, char>(partialCodePointBytes.Slice(0, length));
            return true;
        }

        private void SetPartialUtf8CodePoint(ReadOnlySpan<byte> bytes)
        {
            Debug.Assert(bytes.Length <= 3);

            Span<byte> partialCodePointBytes = PartialCodePointRaw;

            bytes.CopyTo(partialCodePointBytes);
            _partialCodePointFlags = (byte)(bytes.Length | PartialCodePointUtf8EncodingFlag);
        }

        private void SetPartialUtf16CodePoint(ReadOnlySpan<char> bytes)
        {
            Debug.Assert(bytes.Length <= 1);

            Span<byte> partialCodePointBytes = PartialCodePointRaw;

            bytes.CopyTo(MemoryMarshal.Cast<byte, char>(partialCodePointBytes));
            _partialCodePointFlags = (byte)((2 * bytes.Length) | PartialCodePointUtf16EncodingFlag);
        }

        private bool HasPartialCodePoint => (_partialCodePointFlags & PartialCodePointLengthMask) != 0;

        private void ClearPartialCodePoint() => _partialCodePointFlags = 0;

        private void WriteInvalidPartialCodePoint()
        {
            ReadOnlySpan<byte> partialCodePointBytes = PartialCodePointRaw;
            Debug.Assert(partialCodePointBytes.Length == 3);

            int length = _partialCodePointFlags & PartialCodePointLengthMask;

            switch (_partialCodePointFlags & PartialCodePointEncodingMask)
            {
                case PartialCodePointUtf8EncodingFlag:
                    Debug.Assert((uint)length < 4);
                    WriteStringSegmentEscape(partialCodePointBytes.Slice(0, length), true);
                    break;
                case PartialCodePointUtf16EncodingFlag:
                    Debug.Assert(length is 0 or 2);
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
