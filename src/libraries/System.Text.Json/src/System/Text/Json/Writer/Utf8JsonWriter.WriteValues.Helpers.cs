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
        /// <summary>
        /// Assuming that the writer is currently in a valid state, this returns true if a JSON value is not allowed at the current position.
        /// Note that every JsonTokenType is less than 16 (0b0001_0000) except string segment (which is 0b0010_0000), so for these tokens only the
        /// low nibble needs to be checked. There are 3 cases to consider:
        /// <list type="bullet">
        /// <item>
        /// The writer is in an array (<see cref="_enclosingContainer"/> = 0b0001_0000): The only invalid previous token is a string segment.
        /// <see cref="_enclosingContainer"/> ^ 0b0001_0000 is 0, so the entire expression is <see cref="_tokenType"/> > 0b0001_0000, which is true iff the previous token is a string segment.
        /// </item>
        /// <item>
        /// The writer is at the root level (<see cref="_enclosingContainer"/> = 0). The only valid previous token is none. <see cref="_enclosingContainer"/> ^ 0b0001_0000 = 0b0001_0000,
        /// so the entire expression is 0b0001_0000 ^ <see cref="_tokenType"/> > 0b0001_0000. For string segment this is true, and for all other tokens we just need to check the low
        /// nibble. 0000 ^ wxyz = 0 iff wxyz = 0000, which is JsonTokenType.None. For every other token, the inequality is true.
        /// </item>
        /// <item>
        /// The writer is in an object (<see cref="_enclosingContainer"/> = 0b0000_0101). The only valid previous token is a property. <see cref="_enclosingContainer"/> ^ 0b0001_0000 = 0b0001_0101,
        /// so the entire expression is 0b0001_0101 ^ <see cref="_tokenType"/> > 0b0001_0000. For string segment this inequality is true. For every other token, we just need
        /// to check the low nibble. 0101 ^ wxyz = 0 iff wxyz = 0101, which is JsonTokenType.PropertyName. For every other token, the inequality is true.
        /// </item>
        /// </list>
        /// </summary>
        private bool CannotWriteValue => (0b0001_0000 ^ (byte)_enclosingContainer ^ (byte)_tokenType) > 0b0001_0000;

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
                ThrowInvalidOperationException(ExceptionResource.CannotWriteWithinString);
            }

            Debug.Assert(PreviousSegmentEncoding == SegmentEncoding.None);
            Debug.Assert(!HasPartialCodePoint);
        }

        private void ValidateWritingValue()
        {
            if (CannotWriteValue)
            {
                OnValidateWritingValueFailed();
            }
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnValidateWritingValueFailed()
        {
            Debug.Assert(!_options.SkipValidation);

            if (_tokenType == StringSegmentSentinel)
            {
                ThrowInvalidOperationException(ExceptionResource.CannotWriteWithinString);
            }

            Debug.Assert(PreviousSegmentEncoding == SegmentEncoding.None);
            Debug.Assert(!HasPartialCodePoint);

            if (_enclosingContainer == EnclosingContainerType.Object)
            {
                Debug.Assert(_tokenType != JsonTokenType.PropertyName);
                Debug.Assert(_tokenType != JsonTokenType.None && _tokenType != JsonTokenType.StartArray);
                ThrowInvalidOperationException(ExceptionResource.CannotWriteValueWithinObject);
            }
            else
            {
                Debug.Assert(_tokenType != JsonTokenType.PropertyName);
                Debug.Assert(CurrentDepth == 0 && _tokenType != JsonTokenType.None);
                ThrowInvalidOperationException(ExceptionResource.CannotWriteValueAfterPrimitiveOrClose);
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
