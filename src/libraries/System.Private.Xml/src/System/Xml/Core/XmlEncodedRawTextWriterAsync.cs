// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// WARNING: This file is generated and should not be modified directly.
// Instead, modify XmlRawTextWriterGeneratorAsync.ttinclude

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Threading.Tasks;

namespace System.Xml
{
    // Concrete implementation of XmlWriter abstract class that serializes events as encoded XML
    // text.  The general-purpose XmlEncodedTextWriter uses the Encoder class to output to any
    // encoding.  The XmlUtf8TextWriter class combined the encoding operation with serialization
    // in order to achieve better performance.
    internal partial class XmlEncodedRawTextWriter : XmlRawWriter
    {
        protected void CheckAsyncCall()
        {
            if (!_useAsync)
            {
                throw new InvalidOperationException(SR.Xml_WriterAsyncNotSetException);
            }
        }

        // Write the xml declaration.  This must be the first call.
        internal override async Task WriteXmlDeclarationAsync(XmlStandalone standalone)
        {
            CheckAsyncCall();
            // Output xml declaration only if user allows it and it was not already output
            if (!_omitXmlDeclaration && !_autoXmlDeclaration)
            {
                if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }
                await RawTextAsync("<?xml version=\"").ConfigureAwait(false);

                // Version
                await RawTextAsync("1.0").ConfigureAwait(false);

                // Encoding
                if (_encoding != null)
                {
                    await RawTextAsync("\" encoding=\"").ConfigureAwait(false);
                    await RawTextAsync(_encoding.WebName).ConfigureAwait(false);
                }

                // Standalone
                if (standalone != XmlStandalone.Omit)
                {
                    await RawTextAsync("\" standalone=\"").ConfigureAwait(false);
                    await RawTextAsync(standalone == XmlStandalone.Yes ? "yes" : "no").ConfigureAwait(false);
                }

                await RawTextAsync("\"?>").ConfigureAwait(false);
            }
        }

        internal override Task WriteXmlDeclarationAsync(string xmldecl)
        {
            CheckAsyncCall();
            // Output xml declaration only if user allows it and it was not already output
            if (!_omitXmlDeclaration && !_autoXmlDeclaration)
            {
                return WriteProcessingInstructionAsync("xml", xmldecl);
            }

            return Task.CompletedTask;
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            try
            {
                await FlushBufferAsync().ConfigureAwait(false);
            }
            finally
            {
                // Future calls to Close or Flush shouldn't write to Stream or Writer
                _writeToNull = true;

                if (_stream != null)
                {
                    try
                    {
                        await _stream.FlushAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        try
                        {
                            if (_closeOutput)
                            {
                                await _stream.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        finally
                        {
                            _stream = null!;
                        }
                    }
                }
                else if (_writer != null)
                {
                    try
                    {
                        await _writer.FlushAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        try
                        {
                            if (_closeOutput)
                            {
                                await _writer.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        finally
                        {
                            _writer = null!;
                        }
                    }
                }
            }
        }

        // Serialize the document type declaration.
        public override async Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset)
        {
            CheckAsyncCall();
            Debug.Assert(name != null && name.Length > 0);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            await RawTextAsync("<!DOCTYPE ").ConfigureAwait(false);
            await RawTextAsync(name).ConfigureAwait(false);
            if (pubid != null)
            {
                await RawTextAsync(" PUBLIC \"").ConfigureAwait(false);
                await RawTextAsync(pubid).ConfigureAwait(false);
                await RawTextAsync("\" \"").ConfigureAwait(false);
                if (sysid != null)
                {
                    await RawTextAsync(sysid).ConfigureAwait(false);
                }
                _bufChars[_bufPos++] = (char)'"';
            }
            else if (sysid != null)
            {
                await RawTextAsync(" SYSTEM \"").ConfigureAwait(false);
                await RawTextAsync(sysid).ConfigureAwait(false);
                _bufChars[_bufPos++] = (char)'"';
            }
            else
            {
                _bufChars[_bufPos++] = (char)' ';
            }

            if (subset != null)
            {
                _bufChars[_bufPos++] = (char)'[';
                await RawTextAsync(subset).ConfigureAwait(false);
                _bufChars[_bufPos++] = (char)']';
            }

            _bufChars[_bufPos++] = (char)'>';
        }

        // Serialize the beginning of an element start tag: "<prefix:localName"
        public override Task WriteStartElementAsync(string? prefix, string localName, string? ns)
        {
            CheckAsyncCall();
            Debug.Assert(localName != null && localName.Length > 0);
            Debug.Assert(prefix != null);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            Task task;
            _bufChars[_bufPos++] = (char)'<';
            if (!string.IsNullOrEmpty(prefix))
            {
                task = RawTextAsync(prefix, ":", localName);
            }
            else
            {
                task = RawTextAsync(localName);
            }
            return task.CallVoidFuncWhenFinishAsync(thisRef => thisRef.WriteStartElementAsync_SetAttEndPos(), this);
        }

        private void WriteStartElementAsync_SetAttEndPos()
        {
            _attrEndPos = _bufPos;
        }

        // Serialize an element end tag: "</prefix:localName>", if content was output.  Otherwise, serialize
        // the shortcut syntax: " />".
        internal override Task WriteEndElementAsync(string prefix, string localName, string ns)
        {
            CheckAsyncCall();
            Debug.Assert(localName != null && localName.Length > 0);
            Debug.Assert(prefix != null);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            if (_contentPos != _bufPos)
            {
                // Content has been output, so can't use shortcut syntax
                _bufChars[_bufPos++] = (char)'<';
                _bufChars[_bufPos++] = (char)'/';

                if (!string.IsNullOrEmpty(prefix))
                {
                    return RawTextAsync(prefix, ":", localName, ">");
                }
                else
                {
                    return RawTextAsync(localName, ">");
                }
            }
            else
            {
                // Use shortcut syntax; overwrite the already output '>' character
                _bufPos--;
                _bufChars[_bufPos++] = (char)' ';
                _bufChars[_bufPos++] = (char)'/';
                _bufChars[_bufPos++] = (char)'>';
            }
            return Task.CompletedTask;
        }

        // Serialize a full element end tag: "</prefix:localName>"
        internal override Task WriteFullEndElementAsync(string prefix, string localName, string ns)
        {
            CheckAsyncCall();
            Debug.Assert(localName != null && localName.Length > 0);
            Debug.Assert(prefix != null);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'<';
            _bufChars[_bufPos++] = (char)'/';

            if (!string.IsNullOrEmpty(prefix))
            {
                return RawTextAsync(prefix, ":", localName, ">");
            }
            else
            {
                return RawTextAsync(localName, ">");
            }
        }

        // Serialize an attribute tag using double quotes around the attribute value: 'prefix:localName="'
        protected internal override Task WriteStartAttributeAsync(string? prefix, string localName, string? ns)
        {
            CheckAsyncCall();
            Debug.Assert(localName != null && localName.Length > 0);
            Debug.Assert(prefix != null);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            if (_attrEndPos == _bufPos)
            {
                _bufChars[_bufPos++] = (char)' ';
            }
            Task task;
            if (prefix != null && prefix.Length > 0)
            {
                task = RawTextAsync(prefix, ":", localName);
            }
            else
            {
                task = RawTextAsync(localName);
            }
            return task.CallVoidFuncWhenFinishAsync(thisRef => thisRef.WriteStartAttribute_SetInAttribute(), this);
        }

        private void WriteStartAttribute_SetInAttribute()
        {
            _bufChars[_bufPos++] = (char)'=';
            _bufChars[_bufPos++] = (char)'"';
            _inAttributeValue = true;
        }

        // Serialize the end of an attribute value using double quotes: '"'
        protected internal override Task WriteEndAttributeAsync()
        {
            CheckAsyncCall();

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'"';
            _inAttributeValue = false;
            _attrEndPos = _bufPos;

            return Task.CompletedTask;
        }

        internal override async Task WriteNamespaceDeclarationAsync(string prefix, string namespaceName)
        {
            CheckAsyncCall();
            Debug.Assert(prefix != null && namespaceName != null);

            await WriteStartNamespaceDeclarationAsync(prefix).ConfigureAwait(false);
            await WriteStringAsync(namespaceName).ConfigureAwait(false);
            await WriteEndNamespaceDeclarationAsync().ConfigureAwait(false);
        }

        internal override async Task WriteStartNamespaceDeclarationAsync(string prefix)
        {
            CheckAsyncCall();
            Debug.Assert(prefix != null);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            if (_attrEndPos == _bufPos)
            {
                _bufChars[_bufPos++] = (char)' ';
            }

            if (prefix.Length == 0)
            {
                await RawTextAsync("xmlns=\"").ConfigureAwait(false);
            }
            else
            {
                await RawTextAsync("xmlns:").ConfigureAwait(false);
                await RawTextAsync(prefix).ConfigureAwait(false);
                _bufChars[_bufPos++] = (char)'=';
                _bufChars[_bufPos++] = (char)'"';
            }

            _inAttributeValue = true;

            if (_trackTextContent && _inTextContent != true) { ChangeTextContentMark(true); }
        }

        internal override Task WriteEndNamespaceDeclarationAsync()
        {
            CheckAsyncCall();

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            _inAttributeValue = false;

            _bufChars[_bufPos++] = (char)'"';
            _attrEndPos = _bufPos;

            return Task.CompletedTask;
        }

        // Serialize a CData section.  If the "]]>" pattern is found within
        // the text, replace it with "]]><![CDATA[>".
        public override async Task WriteCDataAsync(string? text)
        {
            CheckAsyncCall();
            Debug.Assert(text != null);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            if (_mergeCDataSections && _bufPos == _cdataPos)
            {
                // Merge adjacent cdata sections - overwrite the "]]>" characters
                Debug.Assert(_bufPos >= 4);
                _bufPos -= 3;
            }
            else
            {
                // Start a new cdata section
                _bufChars[_bufPos++] = (char)'<';
                _bufChars[_bufPos++] = (char)'!';
                _bufChars[_bufPos++] = (char)'[';
                _bufChars[_bufPos++] = (char)'C';
                _bufChars[_bufPos++] = (char)'D';
                _bufChars[_bufPos++] = (char)'A';
                _bufChars[_bufPos++] = (char)'T';
                _bufChars[_bufPos++] = (char)'A';
                _bufChars[_bufPos++] = (char)'[';
            }

            await WriteCDataSectionAsync(text).ConfigureAwait(false);

            _bufChars[_bufPos++] = (char)']';
            _bufChars[_bufPos++] = (char)']';
            _bufChars[_bufPos++] = (char)'>';

            _textPos = _bufPos;
            _cdataPos = _bufPos;
        }

        // Serialize a comment.
        public override async Task WriteCommentAsync(string? text)
        {
            CheckAsyncCall();
            Debug.Assert(text != null);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'<';
            _bufChars[_bufPos++] = (char)'!';
            _bufChars[_bufPos++] = (char)'-';
            _bufChars[_bufPos++] = (char)'-';

            await WriteCommentOrPiAsync(text, '-').ConfigureAwait(false);

            _bufChars[_bufPos++] = (char)'-';
            _bufChars[_bufPos++] = (char)'-';
            _bufChars[_bufPos++] = (char)'>';
        }

        // Serialize a processing instruction.
        public override async Task WriteProcessingInstructionAsync(string name, string? text)
        {
            CheckAsyncCall();
            Debug.Assert(name != null && name.Length > 0);
            Debug.Assert(text != null);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'<';
            _bufChars[_bufPos++] = (char)'?';
            await RawTextAsync(name).ConfigureAwait(false);

            if (text.Length > 0)
            {
                _bufChars[_bufPos++] = (char)' ';
                await WriteCommentOrPiAsync(text, '?').ConfigureAwait(false);
            }

            _bufChars[_bufPos++] = (char)'?';
            _bufChars[_bufPos++] = (char)'>';
        }

        // Serialize an entity reference.
        public override async Task WriteEntityRefAsync(string name)
        {
            CheckAsyncCall();
            Debug.Assert(name != null && name.Length > 0);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'&';
            await RawTextAsync(name).ConfigureAwait(false);
            _bufChars[_bufPos++] = (char)';';

            if (_bufPos > _bufLen)
            {
                await FlushBufferAsync().ConfigureAwait(false);
            }

            _textPos = _bufPos;
        }

        // Serialize a character entity reference.
        public override async Task WriteCharEntityAsync(char ch)
        {
            CheckAsyncCall();
            string strVal = ((int)ch).ToString("X", NumberFormatInfo.InvariantInfo);

            if (_checkCharacters && !XmlCharType.IsCharData(ch))
            {
                // we just have a single char, not a surrogate, therefore we have to pass in '\0' for the second char
                throw XmlConvert.CreateInvalidCharException(ch, '\0');
            }

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'&';
            _bufChars[_bufPos++] = (char)'#';
            _bufChars[_bufPos++] = (char)'x';
            await RawTextAsync(strVal).ConfigureAwait(false);
            _bufChars[_bufPos++] = (char)';';

            if (_bufPos > _bufLen)
            {
                await FlushBufferAsync().ConfigureAwait(false);
            }

            _textPos = _bufPos;
        }

        // Serialize a whitespace node.

        public override Task WriteWhitespaceAsync(string? ws)
        {
            CheckAsyncCall();
            Debug.Assert(ws != null);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            if (_inAttributeValue)
            {
                return WriteAttributeTextBlockAsync(ws);
            }
            else
            {
                return WriteElementTextBlockAsync(ws);
            }
        }

        // Serialize either attribute or element text using XML rules.

        public override Task WriteStringAsync(string? text)
        {
            CheckAsyncCall();
            Debug.Assert(text != null);

            if (_trackTextContent && _inTextContent != true) { ChangeTextContentMark(true); }

            if (_inAttributeValue)
            {
                return WriteAttributeTextBlockAsync(text);
            }
            else
            {
                return WriteElementTextBlockAsync(text);
            }
        }

        // Serialize surrogate character entity.
        public override async Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
        {
            CheckAsyncCall();

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            int surrogateChar = XmlCharType.CombineSurrogateChar(lowChar, highChar);

            _bufChars[_bufPos++] = (char)'&';
            _bufChars[_bufPos++] = (char)'#';
            _bufChars[_bufPos++] = (char)'x';
            await RawTextAsync(surrogateChar.ToString("X", NumberFormatInfo.InvariantInfo)).ConfigureAwait(false);
            _bufChars[_bufPos++] = (char)';';
            _textPos = _bufPos;
        }

        // Serialize either attribute or element text using XML rules.
        // Arguments are validated in the XmlWellformedWriter layer.

        public override Task WriteCharsAsync(char[] buffer, int index, int count)
        {
            CheckAsyncCall();
            Debug.Assert(buffer != null);
            Debug.Assert(index >= 0);
            Debug.Assert(count >= 0 && index + count <= buffer.Length);

            if (_trackTextContent && _inTextContent != true) { ChangeTextContentMark(true); }

            if (_inAttributeValue)
            {
                return WriteAttributeTextBlockAsync(buffer, index, count);
            }
            else
            {
                return WriteElementTextBlockAsync(buffer, index, count);
            }
        }

        // Serialize raw data.
        // Arguments are validated in the XmlWellformedWriter layer

        public override async Task WriteRawAsync(char[] buffer, int index, int count)
        {
            CheckAsyncCall();
            Debug.Assert(buffer != null);
            Debug.Assert(index >= 0);
            Debug.Assert(count >= 0 && index + count <= buffer.Length);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            await WriteRawWithCharCheckingAsync(buffer, index, count).ConfigureAwait(false);

            _textPos = _bufPos;
        }

        // Serialize raw data.

        public override async Task WriteRawAsync(string data)
        {
            CheckAsyncCall();
            Debug.Assert(data != null);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            await WriteRawWithCharCheckingAsync(data).ConfigureAwait(false);

            _textPos = _bufPos;
        }

        // Flush all characters in the buffer to output and call Flush() on the output object.
        public override async Task FlushAsync()
        {
            CheckAsyncCall();
            await FlushBufferAsync().ConfigureAwait(false);
            await FlushEncoderAsync().ConfigureAwait(false);

            if (_stream != null)
            {
                await _stream.FlushAsync().ConfigureAwait(false);
            }
            else if (_writer != null)
            {
                await _writer.FlushAsync().ConfigureAwait(false);
            }
        }

        //
        // Implementation methods
        //
        // Flush all characters in the buffer to output.  Do not flush the output object.
        protected virtual async Task FlushBufferAsync()
        {
            try
            {
                // Output all characters (except for previous characters stored at beginning of buffer)
                if (!_writeToNull)
                {
                    Debug.Assert(_stream != null || _writer != null);

                    if (_stream != null)
                    {
                        if (_trackTextContent)
                        {
                            _charEntityFallback!.Reset(_textContentMarks!, _lastMarkPos);
                            // reset text content tracking

                            if ((_lastMarkPos & 1) != 0)
                            {
                                // If the previous buffer ended inside a text content we need to preserve that info
                                //   which means the next index to which we write has to be even
                                _textContentMarks![1] = 1;
                                _lastMarkPos = 1;
                            }
                            else
                            {
                                _lastMarkPos = 0;
                            }
                            Debug.Assert(_textContentMarks![0] == 1);
                        }
                        await EncodeCharsAsync(1, _bufPos, true).ConfigureAwait(false);
                    }
                    else
                    {
                        if (_bufPos - 1 > 0)
                        {
                            // Write text to TextWriter
                            await _writer!.WriteAsync(_bufChars.AsMemory(1, _bufPos - 1)).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch
            {
                // Future calls to flush (i.e. when Close() is called) don't attempt to write to stream
                _writeToNull = true;
                throw;
            }
            finally
            {
                // Move last buffer character to the beginning of the buffer (so that previous character can always be determined)
                _bufChars[0] = _bufChars[_bufPos - 1];

                // Reset buffer position
                _textPos = (_textPos == _bufPos) ? 1 : 0;
                _attrEndPos = (_attrEndPos == _bufPos) ? 1 : 0;
                _contentPos = 0;    // Needs to be zero, since overwriting '>' character is no longer possible
                _cdataPos = 0;      // Needs to be zero, since overwriting ']]>' characters is no longer possible
                _bufPos = 1;        // Buffer position starts at 1, because we need to be able to safely step back -1 in case we need to
                                   // close an empty element or in CDATA section detection of double ]; _BUFFER[0] will always be 0
            }
        }

        private async Task EncodeCharsAsync(int startOffset, int endOffset, bool writeAllToStream)
        {
            // Write encoded text to stream
            int chEnc;
            int bEnc;
            while (startOffset < endOffset)
            {
                if (_charEntityFallback != null)
                {
                    _charEntityFallback.StartOffset = startOffset;
                }
                _encoder!.Convert(_bufChars, startOffset, endOffset - startOffset, _bufBytes!, _bufBytesUsed, _bufBytes!.Length - _bufBytesUsed, false, out chEnc, out bEnc, out _);
                startOffset += chEnc;
                _bufBytesUsed += bEnc;
                if (_bufBytesUsed >= (_bufBytes.Length - 16))
                {
                    await _stream!.WriteAsync(_bufBytes.AsMemory(0, _bufBytesUsed)).ConfigureAwait(false);
                    _bufBytesUsed = 0;
                }
            }
            if (writeAllToStream && _bufBytesUsed > 0)
            {
                await _stream!.WriteAsync(_bufBytes.AsMemory(0, _bufBytesUsed)).ConfigureAwait(false);
                _bufBytesUsed = 0;
            }
        }

        private Task FlushEncoderAsync()
        {
            Debug.Assert(_bufPos == 1);
            if (_stream != null)
            {
                int bEnc;
                // decode no chars, just flush
                _encoder!.Convert(_bufChars, 1, 0, _bufBytes!, 0, _bufBytes!.Length, true, out _, out bEnc, out _);
                if (bEnc != 0)
                {
                    return _stream.WriteAsync(_bufBytes, 0, bEnc);
                }
            }

            return Task.CompletedTask;
        }

        // Serialize text that is part of an attribute value.  The '&', '<', '>', and '"' characters
        // are entitized.
        protected unsafe int WriteAttributeTextBlockNoFlush(char* pSrc, char* pSrcEnd)
        {
            char* pRaw = pSrc;

            fixed (char* pDstBegin = _bufChars)
            {
                char* pDst = pDstBegin + _bufPos;

                int ch = 0;
                while (true)
                {
                    char* pDstEnd = pDst + (pSrcEnd - pSrc);
                    if (pDstEnd > pDstBegin + _bufLen)
                    {
                        pDstEnd = pDstBegin + _bufLen;
                    }

                    while (pDst < pDstEnd && XmlCharType.IsAttributeValueChar((char)(ch = *pSrc)))
                    {
                        *pDst = (char)ch;
                        pDst++;
                        pSrc++;
                    }
                    Debug.Assert(pSrc <= pSrcEnd);

                    // end of value
                    if (pSrc >= pSrcEnd)
                    {
                        break;
                    }

                    // end of buffer
                    if (pDst >= pDstEnd)
                    {
                        _bufPos = (int)(pDst - pDstBegin);
                        return (int)(pSrc - pRaw);
                    }

                    // some character needs to be escaped
                    switch (ch)
                    {
                        case '&':
                            pDst = AmpEntity(pDst);
                            break;
                        case '<':
                            pDst = LtEntity(pDst);
                            break;
                        case '>':
                            pDst = GtEntity(pDst);
                            break;
                        case '"':
                            pDst = QuoteEntity(pDst);
                            break;
                        case '\'':
                            *pDst = (char)ch;
                            pDst++;
                            break;
                        case (char)0x9:
                            if (_newLineHandling == NewLineHandling.None)
                            {
                                *pDst = (char)ch;
                                pDst++;
                            }
                            else
                            {
                                // escape tab in attributes
                                pDst = TabEntity(pDst);
                            }
                            break;
                        case (char)0xD:
                            if (_newLineHandling == NewLineHandling.None)
                            {
                                *pDst = (char)ch;
                                pDst++;
                            }
                            else
                            {
                                // escape new lines in attributes
                                pDst = CarriageReturnEntity(pDst);
                            }
                            break;
                        case (char)0xA:
                            if (_newLineHandling == NewLineHandling.None)
                            {
                                *pDst = (char)ch;
                                pDst++;
                            }
                            else
                            {
                                // escape new lines in attributes
                                pDst = LineFeedEntity(pDst);
                            }
                            break;
                        default:
                            /* Surrogate character */
                            if (XmlCharType.IsSurrogate(ch))
                            {
                                pDst = EncodeSurrogate(pSrc, pSrcEnd, pDst);
                                pSrc += 2;
                            }
                            /* Invalid XML character */
                            else if (ch <= 0x7F || ch >= 0xFFFE)
                            {
                                pDst = InvalidXmlChar(ch, pDst, true);
                                pSrc++;
                            }
                            /* Other character between SurLowEnd and 0xFFFE */
                            else
                            {
                                *pDst = (char)ch;
                                pDst++;
                                pSrc++;
                            }
                            continue;
                    }
                    pSrc++;
                }
                _bufPos = (int)(pDst - pDstBegin);
            }

            return -1;
        }

        protected unsafe int WriteAttributeTextBlockNoFlush(char[] chars, int index, int count)
        {
            if (count == 0)
            {
                return -1;
            }
            fixed (char* pSrc = &chars[index])
            {
                char* pSrcBeg = pSrc;
                char* pSrcEnd = pSrcBeg + count;
                return WriteAttributeTextBlockNoFlush(pSrcBeg, pSrcEnd);
            }
        }

        protected unsafe int WriteAttributeTextBlockNoFlush(string text, int index, int count)
        {
            if (count == 0)
            {
                return -1;
            }
            fixed (char* pSrc = text)
            {
                char* pSrcBeg = pSrc + index;
                char* pSrcEnd = pSrcBeg + count;
                return WriteAttributeTextBlockNoFlush(pSrcBeg, pSrcEnd);
            }
        }

        protected async Task WriteAttributeTextBlockAsync(char[] chars, int index, int count)
        {
            int writeLen;
            int curIndex = index;
            int leftCount = count;
            do
            {
                writeLen = WriteAttributeTextBlockNoFlush(chars, curIndex, leftCount);
                curIndex += writeLen;
                leftCount -= writeLen;
                if (writeLen >= 0)
                {
                    await FlushBufferAsync().ConfigureAwait(false);
                }
            } while (writeLen >= 0);
        }

        protected Task WriteAttributeTextBlockAsync(string text)
        {
            int writeLen;
            int curIndex = 0;
            int leftCount = text.Length;

            writeLen = WriteAttributeTextBlockNoFlush(text, curIndex, leftCount);
            curIndex += writeLen;
            leftCount -= writeLen;
            if (writeLen >= 0)
            {
                return _WriteAttributeTextBlockAsync(text, curIndex, leftCount);
            }

            return Task.CompletedTask;
        }

        private async Task _WriteAttributeTextBlockAsync(string text, int curIndex, int leftCount)
        {
            int writeLen;
            await FlushBufferAsync().ConfigureAwait(false);
            do
            {
                writeLen = WriteAttributeTextBlockNoFlush(text, curIndex, leftCount);
                curIndex += writeLen;
                leftCount -= writeLen;
                if (writeLen >= 0)
                {
                    await FlushBufferAsync().ConfigureAwait(false);
                }
            } while (writeLen >= 0);
        }

        // Serialize text that is part of element content.  The '&', '<', and '>' characters
        // are entitized.
        protected unsafe int WriteElementTextBlockNoFlush(char* pSrc, char* pSrcEnd, out bool needWriteNewLine)
        {
            needWriteNewLine = false;
            char* pRaw = pSrc;

            fixed (char* pDstBegin = _bufChars)
            {
                char* pDst = pDstBegin + _bufPos;

                int ch = 0;
                while (true)
                {
                    char* pDstEnd = pDst + (pSrcEnd - pSrc);
                    if (pDstEnd > pDstBegin + _bufLen)
                    {
                        pDstEnd = pDstBegin + _bufLen;
                    }

                    while (pDst < pDstEnd && XmlCharType.IsAttributeValueChar((char)(ch = *pSrc)))
                    {
                        *pDst = (char)ch;
                        pDst++;
                        pSrc++;
                    }

                    Debug.Assert(pSrc <= pSrcEnd);

                    // end of value
                    if (pSrc >= pSrcEnd)
                    {
                        break;
                    }

                    // end of buffer
                    if (pDst >= pDstEnd)
                    {
                        _bufPos = (int)(pDst - pDstBegin);
                        return (int)(pSrc - pRaw);
                    }

                    // some character needs to be escaped
                    switch (ch)
                    {
                        case '&':
                            pDst = AmpEntity(pDst);
                            break;
                        case '<':
                            pDst = LtEntity(pDst);
                            break;
                        case '>':
                            pDst = GtEntity(pDst);
                            break;
                        case '"':
                        case '\'':
                        case (char)0x9:
                            *pDst = (char)ch;
                            pDst++;
                            break;
                        case (char)0xA:
                            if (_newLineHandling == NewLineHandling.Replace)
                            {
                                _bufPos = (int)(pDst - pDstBegin);
                                needWriteNewLine = true;
                                return (int)(pSrc - pRaw);
                            }
                            else
                            {
                                *pDst = (char)ch;
                                pDst++;
                            }
                            break;
                        case (char)0xD:
                            switch (_newLineHandling)
                            {
                                case NewLineHandling.Replace:
                                    // Replace "\r\n", or "\r" with NewLineChars
                                    if (pSrc + 1 < pSrcEnd && pSrc[1] == '\n')
                                    {
                                        pSrc++;
                                    }

                                    _bufPos = (int)(pDst - pDstBegin);
                                    needWriteNewLine = true;
                                    return (int)(pSrc - pRaw);

                                case NewLineHandling.Entitize:
                                    // Entitize 0xD
                                    pDst = CarriageReturnEntity(pDst);
                                    break;
                                case NewLineHandling.None:
                                    *pDst = (char)ch;
                                    pDst++;
                                    break;
                            }
                            break;
                        default:
                            /* Surrogate character */
                            if (XmlCharType.IsSurrogate(ch))
                            {
                                pDst = EncodeSurrogate(pSrc, pSrcEnd, pDst);
                                pSrc += 2;
                            }
                            /* Invalid XML character */
                            else if (ch <= 0x7F || ch >= 0xFFFE)
                            {
                                pDst = InvalidXmlChar(ch, pDst, true);
                                pSrc++;
                            }
                            /* Other character between SurLowEnd and 0xFFFE */
                            else
                            {
                                *pDst = (char)ch;
                                pDst++;
                                pSrc++;
                            }
                            continue;
                    }
                    pSrc++;
                }
                _bufPos = (int)(pDst - pDstBegin);
                _textPos = _bufPos;
                _contentPos = 0;
            }

            return -1;
        }

        protected unsafe int WriteElementTextBlockNoFlush(char[] chars, int index, int count, out bool needWriteNewLine)
        {
            needWriteNewLine = false;
            if (count == 0)
            {
                _contentPos = 0;
                return -1;
            }
            fixed (char* pSrc = &chars[index])
            {
                char* pSrcBeg = pSrc;
                char* pSrcEnd = pSrcBeg + count;
                return WriteElementTextBlockNoFlush(pSrcBeg, pSrcEnd, out needWriteNewLine);
            }
        }

        protected unsafe int WriteElementTextBlockNoFlush(string text, int index, int count, out bool needWriteNewLine)
        {
            needWriteNewLine = false;
            if (count == 0)
            {
                _contentPos = 0;
                return -1;
            }
            fixed (char* pSrc = text)
            {
                char* pSrcBeg = pSrc + index;
                char* pSrcEnd = pSrcBeg + count;
                return WriteElementTextBlockNoFlush(pSrcBeg, pSrcEnd, out needWriteNewLine);
            }
        }

        protected async Task WriteElementTextBlockAsync(char[] chars, int index, int count)
        {
            int writeLen;
            int curIndex = index;
            int leftCount = count;
            bool needWriteNewLine;
            do
            {
                writeLen = WriteElementTextBlockNoFlush(chars, curIndex, leftCount, out needWriteNewLine);
                curIndex += writeLen;
                leftCount -= writeLen;
                if (needWriteNewLine)
                {
                    //hit WriteNewLine
                    await RawTextAsync(_newLineChars).ConfigureAwait(false);
                    curIndex++;
                    leftCount--;
                }
                else if (writeLen >= 0)
                {
                    await FlushBufferAsync().ConfigureAwait(false);
                }
            } while (writeLen >= 0 || needWriteNewLine);
        }

        protected Task WriteElementTextBlockAsync(string text)
        {
            int writeLen;
            int curIndex = 0;
            int leftCount = text.Length;
            bool needWriteNewLine;

            writeLen = WriteElementTextBlockNoFlush(text, curIndex, leftCount, out needWriteNewLine);
            curIndex += writeLen;
            leftCount -= writeLen;
            if (needWriteNewLine)
            {
                return _WriteElementTextBlockAsync(true, text, curIndex, leftCount);
            }
            else if (writeLen >= 0)
            {
                return _WriteElementTextBlockAsync(false, text, curIndex, leftCount);
            }

            return Task.CompletedTask;
        }

        private async Task _WriteElementTextBlockAsync(bool newLine, string text, int curIndex, int leftCount)
        {
            int writeLen;
            bool needWriteNewLine;

            if (newLine)
            {
                await RawTextAsync(_newLineChars).ConfigureAwait(false);
                curIndex++;
                leftCount--;
            }
            else
            {
                await FlushBufferAsync().ConfigureAwait(false);
            }

            do
            {
                writeLen = WriteElementTextBlockNoFlush(text, curIndex, leftCount, out needWriteNewLine);
                curIndex += writeLen;
                leftCount -= writeLen;
                if (needWriteNewLine)
                {
                    //hit WriteNewLine
                    await RawTextAsync(_newLineChars).ConfigureAwait(false);
                    curIndex++;
                    leftCount--;
                }
                else if (writeLen >= 0)
                {
                    await FlushBufferAsync().ConfigureAwait(false);
                }
            } while (writeLen >= 0 || needWriteNewLine);
        }

        protected unsafe int RawTextNoFlush(char* pSrcBegin, char* pSrcEnd)
        {
            char* pRaw = pSrcBegin;

            fixed (char* pDstBegin = _bufChars)
            {
                char* pDst = pDstBegin + _bufPos;
                char* pSrc = pSrcBegin;

                int ch = 0;
                while (true)
                {
                    char* pDstEnd = pDst + (pSrcEnd - pSrc);
                    if (pDstEnd > pDstBegin + _bufLen)
                    {
                        pDstEnd = pDstBegin + _bufLen;
                    }

                    while (pDst < pDstEnd && ((ch = *pSrc) < XmlCharType.SurHighStart))
                    {
                        pSrc++;
                        *pDst = (char)ch;
                        pDst++;
                    }
                    Debug.Assert(pSrc <= pSrcEnd);

                    // end of value
                    if (pSrc >= pSrcEnd)
                    {
                        break;
                    }

                    // end of buffer
                    if (pDst >= pDstEnd)
                    {
                        _bufPos = (int)(pDst - pDstBegin);
                        return (int)(pSrc - pRaw);
                    }

                    /* Surrogate character */
                    if (XmlCharType.IsSurrogate(ch))
                    {
                        pDst = EncodeSurrogate(pSrc, pSrcEnd, pDst);
                        pSrc += 2;
                    }
                    /* Invalid XML character */
                    else if (ch <= 0x7F || ch >= 0xFFFE)
                    {
                        pDst = InvalidXmlChar(ch, pDst, false);
                        pSrc++;
                    }
                    /* Other character between SurLowEnd and 0xFFFE */
                    else
                    {
                        *pDst = (char)ch;
                        pDst++;
                        pSrc++;
                    }
                }

                _bufPos = (int)(pDst - pDstBegin);
            }

            return -1;
        }

        protected unsafe int RawTextNoFlush(string text, int index, int count)
        {
            if (count == 0)
            {
                return -1;
            }
            fixed (char* pSrc = text)
            {
                char* pSrcBegin = pSrc + index;
                char* pSrcEnd = pSrcBegin + count;
                return RawTextNoFlush(pSrcBegin, pSrcEnd);
            }
        }

        // special-case the one string overload, as it's so common
        protected Task RawTextAsync(string text)
        {
            int writeLen = RawTextNoFlush(text, 0, text.Length);
            return writeLen >= 0 ?
                _RawTextAsync(text, writeLen, text.Length - writeLen) :
                Task.CompletedTask;
        }

        protected Task RawTextAsync(string text1, string? text2 = null, string? text3 = null, string? text4 = null)
        {
            Debug.Assert(text1 != null);
            Debug.Assert(text2 != null || (text3 == null && text4 == null));
            Debug.Assert(text3 != null || (text4 == null));

            int writeLen;

            // Write out the first string
            writeLen = RawTextNoFlush(text1, 0, text1.Length);
            if (writeLen >= 0)
            {
                // If we were only able to partially write it, write out the remainder
                // and then write out the other strings.
                return _RawTextAsync(text1, writeLen, text1.Length - writeLen, text2, text3, text4);
            }

            // We wrote out the first string.  Try to write out the second, if it exists.
            if (text2 != null)
            {
                writeLen = RawTextNoFlush(text2, 0, text2.Length);
                if (writeLen >= 0)
                {
                    // If we were only able to write out some of the second string,
                    // write out the remainder and then the other strings,
                    return _RawTextAsync(text2, writeLen, text2.Length - writeLen, text3, text4);
                }
            }

            // We wrote out the first and second strings.  Try to write out the third
            // if it exists.
            if (text3 != null)
            {
                writeLen = RawTextNoFlush(text3, 0, text3.Length);
                if (writeLen >= 0)
                {
                    // If we were only able to write out some of the third string,
                    // write out the remainder and then the last string.
                    return _RawTextAsync(text3, writeLen, text3.Length - writeLen, text4);
                }
            }

            // Finally, try to write out the fourth string, if it exists.
            if (text4 != null)
            {
                writeLen = RawTextNoFlush(text4, 0, text4.Length);
                if (writeLen >= 0)
                {
                    return _RawTextAsync(text4, writeLen, text4.Length - writeLen);
                }
            }

            // All strings written successfully.
            return Task.CompletedTask;
        }

        private async Task _RawTextAsync(
            string text1, int curIndex1, int leftCount1,
            string? text2 = null, string? text3 = null, string? text4 = null)
        {
            Debug.Assert(text1 != null);
            Debug.Assert(text2 != null || (text3 == null && text4 == null));
            Debug.Assert(text3 != null || (text4 == null));

            // Write out the remainder of the first string
            await FlushBufferAsync().ConfigureAwait(false);
            int writeLen;
            do
            {
                writeLen = RawTextNoFlush(text1, curIndex1, leftCount1);
                curIndex1 += writeLen;
                leftCount1 -= writeLen;
                if (writeLen >= 0)
                {
                    await FlushBufferAsync().ConfigureAwait(false);
                }
            } while (writeLen >= 0);

            // If there are additional strings, write them out as well
            if (text2 != null)
            {
                await RawTextAsync(text2, text3, text4).ConfigureAwait(false);
            }
        }

        protected unsafe int WriteRawWithCharCheckingNoFlush(char* pSrcBegin, char* pSrcEnd, out bool needWriteNewLine)
        {
            needWriteNewLine = false;
            char* pRaw = pSrcBegin;

            fixed (char* pDstBegin = _bufChars)
            {
                char* pSrc = pSrcBegin;
                char* pDst = pDstBegin + _bufPos;

                int ch = 0;
                while (true)
                {
                    char* pDstEnd = pDst + (pSrcEnd - pSrc);
                    if (pDstEnd > pDstBegin + _bufLen)
                    {
                        pDstEnd = pDstBegin + _bufLen;
                    }

                    while (pDst < pDstEnd && XmlCharType.IsTextChar((char)(ch = *pSrc)))
                    {
                        *pDst = (char)ch;
                        pDst++;
                        pSrc++;
                    }

                    Debug.Assert(pSrc <= pSrcEnd);

                    // end of value
                    if (pSrc >= pSrcEnd)
                    {
                        break;
                    }

                    // end of buffer
                    if (pDst >= pDstEnd)
                    {
                        _bufPos = (int)(pDst - pDstBegin);
                        return (int)(pSrc - pRaw);
                    }

                    // handle special characters
                    switch (ch)
                    {
                        case ']':
                        case '<':
                        case '&':
                        case (char)0x9:
                            *pDst = (char)ch;
                            pDst++;
                            break;
                        case (char)0xD:
                            if (_newLineHandling == NewLineHandling.Replace)
                            {
                                // Normalize "\r\n", or "\r" to NewLineChars
                                if (pSrc + 1 < pSrcEnd && pSrc[1] == '\n')
                                {
                                    pSrc++;
                                }

                                _bufPos = (int)(pDst - pDstBegin);
                                needWriteNewLine = true;
                                return (int)(pSrc - pRaw);
                            }
                            else
                            {
                                *pDst = (char)ch;
                                pDst++;
                            }
                            break;
                        case (char)0xA:
                            if (_newLineHandling == NewLineHandling.Replace)
                            {
                                _bufPos = (int)(pDst - pDstBegin);
                                needWriteNewLine = true;
                                return (int)(pSrc - pRaw);
                            }
                            else
                            {
                                *pDst = (char)ch;
                                pDst++;
                            }
                            break;
                        default:
                            /* Surrogate character */
                            if (XmlCharType.IsSurrogate(ch))
                            {
                                pDst = EncodeSurrogate(pSrc, pSrcEnd, pDst);
                                pSrc += 2;
                            }
                            /* Invalid XML character */
                            else if (ch <= 0x7F || ch >= 0xFFFE)
                            {
                                pDst = InvalidXmlChar(ch, pDst, false);
                                pSrc++;
                            }
                            /* Other character between SurLowEnd and 0xFFFE */
                            else
                            {
                                *pDst = (char)ch;
                                pDst++;
                                pSrc++;
                            }
                            continue;
                    }
                    pSrc++;
                }
                _bufPos = (int)(pDst - pDstBegin);
            }

            return -1;
        }

        protected unsafe int WriteRawWithCharCheckingNoFlush(char[] chars, int index, int count, out bool needWriteNewLine)
        {
            needWriteNewLine = false;
            if (count == 0)
            {
                return -1;
            }
            fixed (char* pSrc = &chars[index])
            {
                char* pSrcBeg = pSrc;
                char* pSrcEnd = pSrcBeg + count;
                return WriteRawWithCharCheckingNoFlush(pSrcBeg, pSrcEnd, out needWriteNewLine);
            }
        }

        protected unsafe int WriteRawWithCharCheckingNoFlush(string text, int index, int count, out bool needWriteNewLine)
        {
            needWriteNewLine = false;
            if (count == 0)
            {
                return -1;
            }
            fixed (char* pSrc = text)
            {
                char* pSrcBeg = pSrc + index;
                char* pSrcEnd = pSrcBeg + count;
                return WriteRawWithCharCheckingNoFlush(pSrcBeg, pSrcEnd, out needWriteNewLine);
            }
        }

        protected async Task WriteRawWithCharCheckingAsync(char[] chars, int index, int count)
        {
            int writeLen;
            int curIndex = index;
            int leftCount = count;
            bool needWriteNewLine;
            do
            {
                writeLen = WriteRawWithCharCheckingNoFlush(chars, curIndex, leftCount, out needWriteNewLine);
                curIndex += writeLen;
                leftCount -= writeLen;
                if (needWriteNewLine)
                {
                    await RawTextAsync(_newLineChars).ConfigureAwait(false);
                    curIndex++;
                    leftCount--;
                }
                else if (writeLen >= 0)
                {
                    await FlushBufferAsync().ConfigureAwait(false);
                }
            } while (writeLen >= 0 || needWriteNewLine);
        }

        protected async Task WriteRawWithCharCheckingAsync(string text)
        {
            int writeLen;
            int curIndex = 0;
            int leftCount = text.Length;
            bool needWriteNewLine;
            do
            {
                writeLen = WriteRawWithCharCheckingNoFlush(text, curIndex, leftCount, out needWriteNewLine);
                curIndex += writeLen;
                leftCount -= writeLen;
                if (needWriteNewLine)
                {
                    await RawTextAsync(_newLineChars).ConfigureAwait(false);
                    curIndex++;
                    leftCount--;
                }
                else if (writeLen >= 0)
                {
                    await FlushBufferAsync().ConfigureAwait(false);
                }
            } while (writeLen >= 0 || needWriteNewLine);
        }

        protected unsafe int WriteCommentOrPiNoFlush(string text, int index, int count, int stopChar, out bool needWriteNewLine)
        {
            needWriteNewLine = false;
            if (count == 0)
            {
                return -1;
            }
            fixed (char* pSrcText = text)
            {
                char* pSrcBegin = pSrcText + index;

                fixed (char* pDstBegin = _bufChars)
                {
                    char* pSrc = pSrcBegin;

                    char* pRaw = pSrc;

                    char* pSrcEnd = pSrcBegin + count;

                    char* pDst = pDstBegin + _bufPos;

                    int ch = 0;
                    while (true)
                    {
                        char* pDstEnd = pDst + (pSrcEnd - pSrc);
                        if (pDstEnd > pDstBegin + _bufLen)
                        {
                            pDstEnd = pDstBegin + _bufLen;
                        }

                        while (pDst < pDstEnd && (XmlCharType.IsTextChar((char)(ch = *pSrc)) && ch != stopChar))
                        {
                            *pDst = (char)ch;
                            pDst++;
                            pSrc++;
                        }

                        Debug.Assert(pSrc <= pSrcEnd);

                        // end of value
                        if (pSrc >= pSrcEnd)
                        {
                            break;
                        }

                        // end of buffer
                        if (pDst >= pDstEnd)
                        {
                            _bufPos = (int)(pDst - pDstBegin);
                            return (int)(pSrc - pRaw);
                        }

                        // handle special characters
                        switch (ch)
                        {
                            case '-':
                                *pDst = (char)'-';
                                pDst++;
                                if (ch == stopChar)
                                {
                                    // Insert space between adjacent dashes or before comment's end dashes
                                    if (pSrc + 1 == pSrcEnd || *(pSrc + 1) == '-')
                                    {
                                        *pDst = (char)' ';
                                        pDst++;
                                    }
                                }
                                break;
                            case '?':
                                *pDst = (char)'?';
                                pDst++;
                                if (ch == stopChar)
                                {
                                    // Processing instruction: insert space between adjacent '?' and '>'
                                    if (pSrc + 1 < pSrcEnd && *(pSrc + 1) == '>')
                                    {
                                        *pDst = (char)' ';
                                        pDst++;
                                    }
                                }
                                break;
                            case ']':
                                *pDst = (char)']';
                                pDst++;
                                break;
                            case (char)0xD:
                                if (_newLineHandling == NewLineHandling.Replace)
                                {
                                    // Normalize "\r\n", or "\r" to NewLineChars
                                    if (pSrc + 1 < pSrcEnd && pSrc[1] == '\n')
                                    {
                                        pSrc++;
                                    }

                                    _bufPos = (int)(pDst - pDstBegin);
                                    needWriteNewLine = true;
                                    return (int)(pSrc - pRaw);
                                }
                                else
                                {
                                    *pDst = (char)ch;
                                    pDst++;
                                }
                                break;
                            case (char)0xA:
                                if (_newLineHandling == NewLineHandling.Replace)
                                {
                                    _bufPos = (int)(pDst - pDstBegin);
                                    needWriteNewLine = true;
                                    return (int)(pSrc - pRaw);
                                }
                                else
                                {
                                    *pDst = (char)ch;
                                    pDst++;
                                }
                                break;
                            case '<':
                            case '&':
                            case (char)0x9:
                                *pDst = (char)ch;
                                pDst++;
                                break;
                            default:
                                /* Surrogate character */
                                if (XmlCharType.IsSurrogate(ch))
                                {
                                    pDst = EncodeSurrogate(pSrc, pSrcEnd, pDst);
                                    pSrc += 2;
                                }
                                /* Invalid XML character */
                                else if (ch <= 0x7F || ch >= 0xFFFE)
                                {
                                    pDst = InvalidXmlChar(ch, pDst, false);
                                    pSrc++;
                                }
                                /* Other character between SurLowEnd and 0xFFFE */
                                else
                                {
                                    *pDst = (char)ch;
                                    pDst++;
                                    pSrc++;
                                }
                                continue;
                        }
                        pSrc++;
                    }
                    _bufPos = (int)(pDst - pDstBegin);
                }

                return -1;
            }
        }

        protected async Task WriteCommentOrPiAsync(string text, int stopChar)
        {
            if (text.Length == 0)
            {
                if (_bufPos >= _bufLen)
                {
                    await FlushBufferAsync().ConfigureAwait(false);
                }
                return;
            }

            int writeLen;
            int curIndex = 0;
            int leftCount = text.Length;
            bool needWriteNewLine;
            do
            {
                writeLen = WriteCommentOrPiNoFlush(text, curIndex, leftCount, stopChar, out needWriteNewLine);
                curIndex += writeLen;
                leftCount -= writeLen;
                if (needWriteNewLine)
                {
                    await RawTextAsync(_newLineChars).ConfigureAwait(false);
                    curIndex++;
                    leftCount--;
                }
                else if (writeLen >= 0)
                {
                    await FlushBufferAsync().ConfigureAwait(false);
                }
            } while (writeLen >= 0 || needWriteNewLine);
        }

        protected unsafe int WriteCDataSectionNoFlush(string text, int index, int count, out bool needWriteNewLine)
        {
            needWriteNewLine = false;
            if (count == 0)
            {
                return -1;
            }

            // write text

            fixed (char* pSrcText = text)
            {
                char* pSrcBegin = pSrcText + index;

                fixed (char* pDstBegin = _bufChars)
                {
                    char* pSrc = pSrcBegin;

                    char* pSrcEnd = pSrcBegin + count;

                    char* pRaw = pSrc;

                    char* pDst = pDstBegin + _bufPos;

                    int ch = 0;
                    while (true)
                    {
                        char* pDstEnd = pDst + (pSrcEnd - pSrc);
                        if (pDstEnd > pDstBegin + _bufLen)
                        {
                            pDstEnd = pDstBegin + _bufLen;
                        }

                        while (pDst < pDstEnd && (XmlCharType.IsAttributeValueChar((char)(ch = *pSrc)) && ch != ']'))
                        {
                            *pDst = (char)ch;
                            pDst++;
                            pSrc++;
                        }

                        Debug.Assert(pSrc <= pSrcEnd);

                        // end of value
                        if (pSrc >= pSrcEnd)
                        {
                            break;
                        }

                        // end of buffer
                        if (pDst >= pDstEnd)
                        {
                            _bufPos = (int)(pDst - pDstBegin);
                            return (int)(pSrc - pRaw);
                        }

                        // handle special characters
                        switch (ch)
                        {
                            case '>':
                                if (_hadDoubleBracket && pDst[-1] == (char)']')
                                {   // pDst[-1] will always correct - there is a padding character at _BUFFER[0]
                                    // The characters "]]>" were found within the CData text
                                    pDst = RawEndCData(pDst);
                                    pDst = RawStartCData(pDst);
                                }
                                *pDst = (char)'>';
                                pDst++;
                                break;
                            case ']':
                                if (pDst[-1] == (char)']')
                                {   // pDst[-1] will always correct - there is a padding character at _BUFFER[0]
                                    _hadDoubleBracket = true;
                                }
                                else
                                {
                                    _hadDoubleBracket = false;
                                }
                                *pDst = (char)']';
                                pDst++;
                                break;
                            case (char)0xD:
                                if (_newLineHandling == NewLineHandling.Replace)
                                {
                                    // Normalize "\r\n", or "\r" to NewLineChars
                                    if (pSrc + 1 < pSrcEnd && pSrc[1] == '\n')
                                    {
                                        pSrc++;
                                    }

                                    _bufPos = (int)(pDst - pDstBegin);
                                    needWriteNewLine = true;
                                    return (int)(pSrc - pRaw);
                                }
                                else
                                {
                                    *pDst = (char)ch;
                                    pDst++;
                                }
                                break;
                            case (char)0xA:
                                if (_newLineHandling == NewLineHandling.Replace)
                                {
                                    _bufPos = (int)(pDst - pDstBegin);
                                    needWriteNewLine = true;
                                    return (int)(pSrc - pRaw);
                                }
                                else
                                {
                                    *pDst = (char)ch;
                                    pDst++;
                                }
                                break;
                            case '&':
                            case '<':
                            case '"':
                            case '\'':
                            case (char)0x9:
                                *pDst = (char)ch;
                                pDst++;
                                break;
                            default:
                                /* Surrogate character */
                                if (XmlCharType.IsSurrogate(ch))
                                {
                                    pDst = EncodeSurrogate(pSrc, pSrcEnd, pDst);
                                    pSrc += 2;
                                }
                                /* Invalid XML character */
                                else if (ch <= 0x7F || ch >= 0xFFFE)
                                {
                                    pDst = InvalidXmlChar(ch, pDst, false);
                                    pSrc++;
                                }
                                /* Other character between SurLowEnd and 0xFFFE */
                                else
                                {
                                    *pDst = (char)ch;
                                    pDst++;
                                    pSrc++;
                                }
                                continue;
                        }
                        pSrc++;
                    }
                    _bufPos = (int)(pDst - pDstBegin);
                }

                return -1;
            }
        }

        protected async Task WriteCDataSectionAsync(string text)
        {
            if (text.Length == 0)
            {
                if (_bufPos >= _bufLen)
                {
                    await FlushBufferAsync().ConfigureAwait(false);
                }
                return;
            }

            int writeLen;
            int curIndex = 0;
            int leftCount = text.Length;
            bool needWriteNewLine;
            do
            {
                writeLen = WriteCDataSectionNoFlush(text, curIndex, leftCount, out needWriteNewLine);
                curIndex += writeLen;
                leftCount -= writeLen;
                if (needWriteNewLine)
                {
                    await RawTextAsync(_newLineChars).ConfigureAwait(false);
                    curIndex++;
                    leftCount--;
                }
                else if (writeLen >= 0)
                {
                    await FlushBufferAsync().ConfigureAwait(false);
                }
            } while (writeLen >= 0 || needWriteNewLine);
        }
    }

    // Same as base text writer class except that elements, attributes, comments, and pi's are indented.
    internal partial class XmlEncodedRawTextWriterIndent : XmlEncodedRawTextWriter
    {
        public override async Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset)
        {
            CheckAsyncCall();
            // Add indentation
            if (!_mixedContent && base._textPos != base._bufPos)
            {
                await WriteIndentAsync().ConfigureAwait(false);
            }
            await base.WriteDocTypeAsync(name, pubid, sysid, subset).ConfigureAwait(false);
        }

        public override async Task WriteStartElementAsync(string? prefix, string localName, string? ns)
        {
            CheckAsyncCall();
            Debug.Assert(!string.IsNullOrEmpty(localName) && prefix != null && ns != null);

            // Add indentation
            if (!_mixedContent && base._textPos != base._bufPos)
            {
                await WriteIndentAsync().ConfigureAwait(false);
            }
            _indentLevel++;
            _mixedContentStack.PushBit(_mixedContent);

            await base.WriteStartElementAsync(prefix, localName, ns).ConfigureAwait(false);
        }

        internal override async Task WriteEndElementAsync(string prefix, string localName, string ns)
        {
            CheckAsyncCall();
            // Add indentation
            _indentLevel--;
            if (!_mixedContent && base._contentPos != base._bufPos)
            {
                // There was content, so try to indent
                if (base._textPos != base._bufPos)
                {
                    await WriteIndentAsync().ConfigureAwait(false);
                }
            }
            _mixedContent = _mixedContentStack.PopBit();

            await base.WriteEndElementAsync(prefix, localName, ns).ConfigureAwait(false);
        }

        internal override async Task WriteFullEndElementAsync(string prefix, string localName, string ns)
        {
            CheckAsyncCall();
            // Add indentation
            _indentLevel--;
            if (!_mixedContent && base._contentPos != base._bufPos)
            {
                // There was content, so try to indent
                if (base._textPos != base._bufPos)
                {
                    await WriteIndentAsync().ConfigureAwait(false);
                }
            }
            _mixedContent = _mixedContentStack.PopBit();

            await base.WriteFullEndElementAsync(prefix, localName, ns).ConfigureAwait(false);
        }

        // Same as base class, plus possible indentation.
        protected internal override async Task WriteStartAttributeAsync(string? prefix, string localName, string? ns)
        {
            CheckAsyncCall();
            // Add indentation
            if (_newLineOnAttributes)
            {
                await WriteIndentAsync().ConfigureAwait(false);
            }

            await base.WriteStartAttributeAsync(prefix, localName, ns).ConfigureAwait(false);
        }

        // Same as base class, plus possible indentation.
        internal override async Task WriteStartNamespaceDeclarationAsync(string prefix)
        {
            CheckAsyncCall();
            // Add indentation
            if (_newLineOnAttributes)
            {
                await WriteIndentAsync().ConfigureAwait(false);
            }

            await base.WriteStartNamespaceDeclarationAsync(prefix).ConfigureAwait(false);
        }

        public override Task WriteCDataAsync(string? text)
        {
            CheckAsyncCall();
            _mixedContent = true;
            return base.WriteCDataAsync(text);
        }

        public override async Task WriteCommentAsync(string? text)
        {
            CheckAsyncCall();
            if (!_mixedContent && base._textPos != base._bufPos)
            {
                await WriteIndentAsync().ConfigureAwait(false);
            }

            await base.WriteCommentAsync(text).ConfigureAwait(false);
        }

        public override async Task WriteProcessingInstructionAsync(string target, string? text)
        {
            CheckAsyncCall();
            if (!_mixedContent && base._textPos != base._bufPos)
            {
                await WriteIndentAsync().ConfigureAwait(false);
            }

            await base.WriteProcessingInstructionAsync(target, text).ConfigureAwait(false);
        }

        public override Task WriteEntityRefAsync(string name)
        {
            CheckAsyncCall();
            _mixedContent = true;
            return base.WriteEntityRefAsync(name);
        }

        public override Task WriteCharEntityAsync(char ch)
        {
            CheckAsyncCall();
            _mixedContent = true;
            return base.WriteCharEntityAsync(ch);
        }

        public override Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
        {
            CheckAsyncCall();
            _mixedContent = true;
            return base.WriteSurrogateCharEntityAsync(lowChar, highChar);
        }

        public override Task WriteWhitespaceAsync(string? ws)
        {
            CheckAsyncCall();
            _mixedContent = true;
            return base.WriteWhitespaceAsync(ws);
        }

        public override Task WriteStringAsync(string? text)
        {
            CheckAsyncCall();
            _mixedContent = true;
            return base.WriteStringAsync(text);
        }

        public override Task WriteCharsAsync(char[] buffer, int index, int count)
        {
            CheckAsyncCall();
            _mixedContent = true;
            return base.WriteCharsAsync(buffer, index, count);
        }

        public override Task WriteRawAsync(char[] buffer, int index, int count)
        {
            CheckAsyncCall();
            _mixedContent = true;
            return base.WriteRawAsync(buffer, index, count);
        }

        public override Task WriteRawAsync(string data)
        {
            CheckAsyncCall();
            _mixedContent = true;
            return base.WriteRawAsync(data);
        }

        public override Task WriteBase64Async(byte[] buffer, int index, int count)
        {
            CheckAsyncCall();
            _mixedContent = true;
            return base.WriteBase64Async(buffer, index, count);
        }

        // Add indentation to output.  Write newline and then repeat IndentChars for each indent level.
        private async Task WriteIndentAsync()
        {
            CheckAsyncCall();
            await RawTextAsync(base._newLineChars).ConfigureAwait(false);
            for (int i = _indentLevel; i > 0; i--)
            {
                await RawTextAsync(_indentChars).ConfigureAwait(false);
            }
        }
    }
}
