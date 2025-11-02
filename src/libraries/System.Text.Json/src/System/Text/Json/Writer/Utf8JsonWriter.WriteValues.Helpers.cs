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
        /// Returns whether a JSON value can be written at the current position based on the current <see cref="_enclosingContainer"/>:
        /// <list type="bullet">
        /// <item>
        /// <see cref="EnclosingContainerType.Array"/>: Writing a value is always allowed.
        /// </item>
        /// <item>
        /// <see cref="EnclosingContainerType.Object"/>: Writing a value is allowed only if <see cref="_tokenType"/> is a property name.
        /// Because we designed <see cref="EnclosingContainerType.Object"/> == <see cref="JsonTokenType.PropertyName"/>, we can just check for equality.
        /// </item>
        /// <item>
        /// <see cref="EnclosingContainerType.None"/>: Writing a value is allowed only if <see cref="_tokenType"/> is None (only one value may be written at the root).
        /// This case is identical to the previous case.
        /// </item>
        /// <item>
        /// <see cref="EnclosingContainerType.Utf8StringSequence"/>, <see cref="EnclosingContainerType.Utf16StringSequence"/>, <see cref="EnclosingContainerType.Base64StringSequence"/>:
        /// Writing a value is never valid and <see cref="_enclosingContainer"/> does not equal any <see cref="JsonTokenType"/> by construction.
        /// </item>
        /// </list>
        /// This method performs better without short circuiting (this often gets inlined so using simple branch free code seems to have some benefits).
        /// </summary>
        private bool CanWriteValue => _enclosingContainer == EnclosingContainerType.Array | (byte)_enclosingContainer == (byte)_tokenType;

        private bool HasPartialStringData => _partialStringDataLength != 0;

        private void ClearPartialStringData() => _partialStringDataLength = 0;

        private void ValidateWritingValue()
        {
            if (!CanWriteValue)
            {
                OnValidateWritingValueFailed();
            }
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnValidateWritingValueFailed()
        {
            Debug.Assert(!_options.SkipValidation);

            if (IsWritingPartialString)
            {
                ThrowInvalidOperationException(ExceptionResource.CannotWriteWithinString);
            }

            Debug.Assert(!HasPartialStringData);

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

        private void ValidateWritingSegment(EnclosingContainerType currentSegmentEncoding)
        {
            Debug.Assert(currentSegmentEncoding is EnclosingContainerType.Utf8StringSequence or EnclosingContainerType.Utf16StringSequence or EnclosingContainerType.Base64StringSequence);

            // A string segment can be written if either:
            // 1) The writer is currently in a partial string of the same type. In this case the new segment
            // will continue the partial string.
            // - or -
            // 2) The writer can write a value at the current position, in which case a new string can be started.
            if (_enclosingContainer != currentSegmentEncoding && !CanWriteValue)
            {
                OnValidateWritingSegmentFailed(currentSegmentEncoding);
            }
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnValidateWritingSegmentFailed(EnclosingContainerType currentSegmentEncoding)
        {
            if (IsWritingPartialString)
            {
                ThrowHelper.ThrowInvalidOperationException_CannotMixEncodings(_enclosingContainer, currentSegmentEncoding);
            }

            Debug.Assert(!HasPartialStringData);

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

        private void ValidateNotWithinUnfinalizedString()
        {
            if (IsWritingPartialString)
            {
                ThrowInvalidOperationException(ExceptionResource.CannotWriteWithinString);
            }

            Debug.Assert(!HasPartialStringData);
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
