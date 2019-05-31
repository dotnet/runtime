// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            if (!Options.SkipValidation)
            {
                if (_inObject)
                {
                    Debug.Assert(_tokenType != JsonTokenType.None && _tokenType != JsonTokenType.StartArray);
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWriteValueWithinObject, currentDepth: default, token: default, _tokenType);
                }
                else
                {
                    if (!_isNotPrimitive && _tokenType != JsonTokenType.None)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWriteValueAfterPrimitive, currentDepth: default, token: default, _tokenType);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Base64EncodeAndWrite(ReadOnlySpan<byte> bytes, Span<byte> output, int encodingLength)
        {
            byte[] outputText = null;

            Span<byte> encodedBytes = encodingLength <= JsonConstants.StackallocThreshold ?
                stackalloc byte[encodingLength] :
                (outputText = ArrayPool<byte>.Shared.Rent(encodingLength));

            OperationStatus status = Base64.EncodeToUtf8(bytes, encodedBytes, out int consumed, out int written);
            Debug.Assert(status == OperationStatus.Done);
            Debug.Assert(consumed == bytes.Length);

            encodedBytes = encodedBytes.Slice(0, written);
            Span<byte> destination = output.Slice(BytesPending);

            int firstEscapeIndexVal = encodedBytes.IndexOfAny(JsonConstants.Plus, JsonConstants.Slash);
            if (firstEscapeIndexVal == -1)
            {
                Debug.Assert(destination.Length >= written);
                encodedBytes.Slice(0, written).CopyTo(destination);
                BytesPending += written;
            }
            else
            {
                Debug.Assert(destination.Length >= written * JsonConstants.MaxExpansionFactorWhileEscaping);
                JsonWriterHelper.EscapeString(encodedBytes, destination, firstEscapeIndexVal, out written);
                BytesPending += written;
            }

            if (outputText != null)
            {
                ArrayPool<byte>.Shared.Return(outputText);
            }
        }
    }
}
