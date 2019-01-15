// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonWriter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidatePropertyNameAndDepth(ReadOnlySpan<char> propertyName)
        {
            if (propertyName.Length > JsonConstants.MaxCharacterTokenSize || CurrentDepth >= JsonConstants.MaxWriterDepth)
                ThrowHelper.ThrowInvalidOperationOrArgumentException(propertyName, _currentDepth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidatePropertyNameAndDepth(ReadOnlySpan<byte> utf8PropertyName)
        {
            if (utf8PropertyName.Length > JsonConstants.MaxTokenSize || CurrentDepth >= JsonConstants.MaxWriterDepth)
                ThrowHelper.ThrowInvalidOperationOrArgumentException(utf8PropertyName, _currentDepth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateWritingProperty()
        {
            if (!_writerOptions.SkipValidation)
            {
                if (!_inObject)
                {
                    Debug.Assert(_tokenType != JsonTokenType.StartObject);
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWritePropertyWithinArray, tokenType: _tokenType);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateWritingProperty(byte token)
        {
            if (!_writerOptions.SkipValidation)
            {
                if (!_inObject)
                {
                    Debug.Assert(_tokenType != JsonTokenType.StartObject);
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWritePropertyWithinArray, tokenType: _tokenType);
                }
                UpdateBitStackOnStart(token);
            }
        }

        private int WritePropertyNameMinimized(ReadOnlySpan<byte> escapedPropertyName)
        {
            int idx = 0;
            if (_currentDepth < 0)
            {
                if (_buffer.Length <= idx)
                {
                    GrowAndEnsure();
                }
                _buffer[idx++] = JsonConstants.ListSeparator;
            }

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;

            CopyLoop(escapedPropertyName, ref idx);

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.KeyValueSeperator;

            return idx;
        }

        private int WritePropertyNameIndented(ReadOnlySpan<byte> escapedPropertyName)
        {
            int idx = 0;
            if (_currentDepth < 0)
            {
                if (_buffer.Length <= idx)
                {
                    GrowAndEnsure();
                }
                _buffer[idx++] = JsonConstants.ListSeparator;
            }

            if (_tokenType != JsonTokenType.None)
                WriteNewLine(ref idx);

            int indent = Indentation;
            while (true)
            {
                bool result = JsonWriterHelper.TryWriteIndentation(_buffer.Slice(idx), indent, out int bytesWritten);
                idx += bytesWritten;
                if (result)
                {
                    break;
                }
                indent -= bytesWritten;
                AdvanceAndGrow(ref idx);
            }

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;

            CopyLoop(escapedPropertyName, ref idx);

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.KeyValueSeperator;

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Space;

            return idx;
        }

        private int WritePropertyNameMinimized(ReadOnlySpan<char> escapedPropertyName)
        {
            int idx = 0;
            if (_currentDepth < 0)
            {
                if (_buffer.Length <= idx)
                {
                    GrowAndEnsure();
                }
                _buffer[idx++] = JsonConstants.ListSeparator;
            }

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;

            ReadOnlySpan<byte> byteSpan = MemoryMarshal.AsBytes(escapedPropertyName);
            int partialConsumed = 0;
            while (true)
            {
                OperationStatus status = JsonWriterHelper.ToUtf8(byteSpan.Slice(partialConsumed), _buffer.Slice(idx), out int consumed, out int written);
                idx += written;
                if (status == OperationStatus.Done)
                {
                    break;
                }
                partialConsumed += consumed;
                AdvanceAndGrow(ref idx);
            }

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.KeyValueSeperator;

            return idx;
        }

        private int WritePropertyNameIndented(ReadOnlySpan<char> escapedPropertyName)
        {
            int idx = 0;
            if (_currentDepth < 0)
            {
                if (_buffer.Length <= idx)
                {
                    GrowAndEnsure();
                }
                _buffer[idx++] = JsonConstants.ListSeparator;
            }

            if (_tokenType != JsonTokenType.None)
                WriteNewLine(ref idx);

            int indent = Indentation;
            while (true)
            {
                bool result = JsonWriterHelper.TryWriteIndentation(_buffer.Slice(idx), indent, out int bytesWritten);
                idx += bytesWritten;
                if (result)
                {
                    break;
                }
                indent -= bytesWritten;
                AdvanceAndGrow(ref idx);
            }

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;

            ReadOnlySpan<byte> byteSpan = MemoryMarshal.AsBytes(escapedPropertyName);
            int partialConsumed = 0;
            while (true)
            {
                OperationStatus status = JsonWriterHelper.ToUtf8(byteSpan.Slice(partialConsumed), _buffer.Slice(idx), out int consumed, out int written);
                idx += written;
                if (status == OperationStatus.Done)
                {
                    break;
                }
                partialConsumed += consumed;
                AdvanceAndGrow(ref idx);
            }

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Quote;

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.KeyValueSeperator;

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.Space;

            return idx;
        }
    }
}
