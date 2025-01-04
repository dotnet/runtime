// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        private bool HasPartialCodePoint => PartialCodePointLength != 0;

        private void ClearPartialCodePoint() => PartialCodePointLength = 0;

        private void ValidateEncodingDidNotChange(SegmentEncoding currentSegmentEncoding)
        {
            if (PreviousSegmentEncoding != currentSegmentEncoding)
            {
                ThrowHelper.ThrowInvalidOperationException_CannotMixEncodings(PreviousSegmentEncoding, currentSegmentEncoding);
            }
        }

        private void ValidateNotWithinUnfinalizedString()
        {
            if (_tokenType == StringSegmentSentinel)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWriteWithinString, currentDepth: default, maxDepth: _options.MaxDepth, token: default, _tokenType);
            }

            Debug.Assert(PreviousSegmentEncoding == SegmentEncoding.None);
            Debug.Assert(!HasPartialCodePoint);
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
