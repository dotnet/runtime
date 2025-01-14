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
        /// Assuming that the writer is currently in a valid state, this returns true if a JSON value is allowed at the current position.
        /// <list type="bullet">
        /// <item>
        /// If <see cref="_enclosingContainer"/> is an array then writing a value is always allowed.
        /// </item>
        /// <item>
        /// If <see cref="_enclosingContainer"/> is an object then writing a value is allowed only if <see cref="_tokenType"/> is a property name.
        /// Because we designed <see cref="EnclosingContainerType.Object"/> == <see cref="JsonTokenType.PropertyName"/>, we can just check for equality.
        /// </item>
        /// <item>
        /// If <see cref="_enclosingContainer"/> is none (the root level) then writing a value is allowed only if <see cref="_tokenType"/> is None (only
        /// one value may be written at the root). This case is identical to the previous case.
        /// </item>
        /// <item>
        /// If <see cref="_enclosingContainer"/> is a partial value, then it will never be a valid <see cref="_tokenType"/> by construction.
        /// </item>
        /// </list>
        /// This method performs better without short circuiting (this often gets inlined so using simple branch free code seems to have some benefits).
        /// </summary>
        private bool CanWriteValue => _enclosingContainer == EnclosingContainerType.Array | (byte)_enclosingContainer == (byte)_tokenType;

        private bool HasPartialStringData => PartialStringDataLength != 0;

        private void ClearPartialStringData() => PartialStringDataLength = 0;

        private void ValidateEncodingDidNotChange(SegmentEncoding currentSegmentEncoding)
        {
            if (PreviousSegmentEncoding != currentSegmentEncoding)
            {
                ThrowHelper.ThrowInvalidOperationException_CannotMixEncodings(PreviousSegmentEncoding, currentSegmentEncoding);
            }
        }

        private void ValidateNotWithinUnfinalizedString()
        {
            if (_enclosingContainer == EnclosingContainerType.PartialValue)
            {
                ThrowInvalidOperationException(ExceptionResource.CannotWriteWithinString);
            }

            Debug.Assert(PreviousSegmentEncoding == SegmentEncoding.None);
            Debug.Assert(!HasPartialStringData);
        }

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

            if (_enclosingContainer == EnclosingContainerType.PartialValue)
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
