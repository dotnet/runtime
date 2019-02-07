// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonWriter
    {
        private void ValidateWritingValue()
        {
            if (!_writerOptions.SkipValidation)
            {
                if (_inObject)
                {
                    Debug.Assert(_tokenType != JsonTokenType.None && _tokenType != JsonTokenType.StartArray);
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWriteValueWithinObject, tokenType: _tokenType);
                }
                else
                {
                    if (!_isNotPrimitive && _tokenType != JsonTokenType.None)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWriteValueAfterPrimitive, tokenType: _tokenType);
                    }
                }
            }
        }

        private int WriteCommaAndFormattingPreamble()
        {
            int idx = 0;
            WriteListSeparator(ref idx);
            WriteFormattingPreamble(ref idx);
            return idx;
        }

        private void WriteFormattingPreamble(ref int idx)
        {
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
        }

        private void WriteListSeparator(ref int idx)
        {
            if (_currentDepth < 0)
            {
                if (_buffer.Length <= idx)
                {
                    GrowAndEnsure();
                }
                _buffer[idx++] = JsonConstants.ListSeparator;
            }
        }
    }
}
