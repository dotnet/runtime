// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        private void ValidateWritingValue()
        {
            Debug.Assert(!_options.SkipValidation);

            if (_inObject)
            {
                if (_tokenType != JsonTokenType.PropertyName)
                {
                    Debug.Assert(_tokenType != JsonTokenType.None && _tokenType != JsonTokenType.StartArray);
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWriteValueWithinObject, currentDepth: default, token: default, _tokenType);
                }
            }
            else
            {
                Debug.Assert(_tokenType != JsonTokenType.PropertyName);

                // It is more likely for CurrentDepth to not equal 0 when writing valid JSON, so check that first to rely on short-circuiting and return quickly.
                if (CurrentDepth == 0 && _tokenType != JsonTokenType.None)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWriteValueAfterPrimitiveOrClose, currentDepth: default, token: default, _tokenType);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Base64EncodeAndWrite(ReadOnlySpan<byte> bytes, Span<byte> output, int encodingLength)
        {
            byte[]? outputText = null;

            Span<byte> encodedBytes = encodingLength <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (outputText = ArrayPool<byte>.Shared.Rent(encodingLength));

            OperationStatus status = Base64.EncodeToUtf8(bytes, encodedBytes, out int consumed, out int written);
            Debug.Assert(status == OperationStatus.Done);
            Debug.Assert(consumed == bytes.Length);

            encodedBytes = encodedBytes.Slice(0, written);
            Span<byte> destination = output.Slice(BytesPending);

            Debug.Assert(destination.Length >= written);
            encodedBytes.Slice(0, written).CopyTo(destination);
            BytesPending += written;

            if (outputText != null)
            {
                ArrayPool<byte>.Shared.Return(outputText);
            }
        }
    }
}
