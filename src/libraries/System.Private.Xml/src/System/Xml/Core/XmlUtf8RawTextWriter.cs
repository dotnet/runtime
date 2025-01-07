// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// WARNING: This file is generated and should not be modified directly.
// Instead, modify XmlRawTextWriterGenerator.ttinclude

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace System.Xml
{
    // Concrete implementation of XmlWriter abstract class that serializes events as encoded XML
    // text.  The general-purpose XmlEncodedTextWriter uses the Encoder class to output to any
    // encoding.  The XmlUtf8TextWriter class combined the encoding operation with serialization
    // in order to achieve better performance.
    internal partial class XmlUtf8RawTextWriter : XmlRawWriter
    {
        //
        // Fields
        //
        private readonly bool _useAsync;

        // main buffer
        protected byte[] _bufBytes = null!;

        // output stream
        protected Stream _stream = null!;

        // encoding of the stream or text writer
        protected Encoding _encoding = null!;

        // buffer positions
        protected int _bufPos = 1;     // buffer position starts at 1, because we need to be able to safely step back -1 in case we need to
                                       // close an empty element or in CDATA section detection of double ]; _bufBytes[0] will always be 0
        protected int _textPos = 1;    // text end position; don't indent first element, pi, or comment
        protected int _contentPos;     // element content end position
        protected int _cdataPos;       // cdata end position
        protected int _attrEndPos;     // end of the last attribute
        protected int _bufLen = BUFSIZE;

        // flags
        protected bool _writeToNull;
        protected bool _hadDoubleBracket;
        protected bool _inAttributeValue;

        // writer settings
        protected NewLineHandling _newLineHandling;
        protected bool _closeOutput;
        protected bool _omitXmlDeclaration;
        protected string _newLineChars;
        protected bool _checkCharacters;

        protected XmlStandalone _standalone;
        protected XmlOutputMethod _outputMethod;

        protected bool _autoXmlDeclaration;
        protected bool _mergeCDataSections;

        //
        // Constants
        //
        private const int BUFSIZE = 2048 * 3;       // Should be greater than default FileStream size (4096), otherwise the FileStream will try to cache the data
        private const int ASYNCBUFSIZE = 64 * 1024; // Set async buffer size to 64KB
        private const int OVERFLOW = 32;            // Allow overflow in order to reduce checks when writing out constant size markup

        //
        // Constructors
        //
        // Construct and initialize an instance of this class.
        protected XmlUtf8RawTextWriter(XmlWriterSettings settings)
        {
            _useAsync = settings.Async;

            // copy settings
            _newLineHandling = settings.NewLineHandling;
            _omitXmlDeclaration = settings.OmitXmlDeclaration;
            _newLineChars = settings.NewLineChars;
            _checkCharacters = settings.CheckCharacters;
            _closeOutput = settings.CloseOutput;

            _standalone = settings.Standalone;
            _outputMethod = settings.OutputMethod;
            _mergeCDataSections = settings.MergeCDataSections;

            if (_checkCharacters && _newLineHandling == NewLineHandling.Replace)
            {
                ValidateContentChars(_newLineChars, "NewLineChars", false);
            }
        }

        // Construct an instance of this class that serializes to a Stream interface.
        public XmlUtf8RawTextWriter(Stream stream, XmlWriterSettings settings) : this(settings)
        {
            Debug.Assert(stream != null && settings != null);

            _stream = stream;
            _encoding = settings.Encoding;

            // the buffer is allocated will OVERFLOW in order to reduce checks when writing out constant size markup
            if (settings.Async)
            {
                _bufLen = ASYNCBUFSIZE;
            }

            _bufBytes = new byte[_bufLen + OVERFLOW];
            // Output UTF-8 byte order mark if Encoding object wants it
            if (!stream.CanSeek || stream.Position == 0)
            {
                ReadOnlySpan<byte> bom = _encoding.Preamble;
                if (bom.Length != 0)
                {
                    bom.CopyTo(new Span<byte>(_bufBytes).Slice(1));
                    _bufPos += bom.Length;
                    _textPos += bom.Length;
                }
            }

            // Write the xml declaration
            if (settings.AutoXmlDeclaration)
            {
                WriteXmlDeclaration(_standalone);
                _autoXmlDeclaration = true;
            }
        }

        //
        // XmlWriter implementation
        //
        // Returns settings the writer currently applies.
        public override XmlWriterSettings Settings
        {
            get
            {
                XmlWriterSettings settings = new XmlWriterSettings();

                settings.Encoding = _encoding;
                settings.OmitXmlDeclaration = _omitXmlDeclaration;
                settings.NewLineHandling = _newLineHandling;
                settings.NewLineChars = _newLineChars;
                settings.CloseOutput = _closeOutput;
                settings.ConformanceLevel = ConformanceLevel.Auto;
                settings.CheckCharacters = _checkCharacters;

                settings.AutoXmlDeclaration = _autoXmlDeclaration;
                settings.Standalone = _standalone;
                settings.OutputMethod = _outputMethod;

                settings.ReadOnly = true;
                return settings;
            }
        }

        // Write the xml declaration.  This must be the first call.
        internal override void WriteXmlDeclaration(XmlStandalone standalone)
        {
            // Output xml declaration only if user allows it and it was not already output
            if (!_omitXmlDeclaration && !_autoXmlDeclaration)
            {
                RawText("<?xml version=\"");

                // Version
                RawText("1.0");

                // Encoding
                if (_encoding != null)
                {
                    RawText("\" encoding=\"");
                    RawText(_encoding.WebName);
                }

                // Standalone
                if (standalone != XmlStandalone.Omit)
                {
                    RawText("\" standalone=\"");
                    RawText(standalone == XmlStandalone.Yes ? "yes" : "no");
                }

                RawText("\"?>");
            }
        }

        internal override void WriteXmlDeclaration(string xmldecl)
        {
            // Output xml declaration only if user allows it and it was not already output
            if (!_omitXmlDeclaration && !_autoXmlDeclaration)
            {
                WriteProcessingInstruction("xml", xmldecl);
            }
        }

        // Serialize the document type declaration.
        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            Debug.Assert(name != null && name.Length > 0);

            RawText("<!DOCTYPE ");
            RawText(name);
            if (pubid != null)
            {
                RawText(" PUBLIC \"");
                RawText(pubid);
                RawText("\" \"");
                if (sysid != null)
                {
                    RawText(sysid);
                }
                _bufBytes[_bufPos++] = (byte)'"';
            }
            else if (sysid != null)
            {
                RawText(" SYSTEM \"");
                RawText(sysid);
                _bufBytes[_bufPos++] = (byte)'"';
            }
            else
            {
                _bufBytes[_bufPos++] = (byte)' ';
            }

            if (subset != null)
            {
                _bufBytes[_bufPos++] = (byte)'[';
                RawText(subset);
                _bufBytes[_bufPos++] = (byte)']';
            }

            _bufBytes[_bufPos++] = (byte)'>';
        }

        // Serialize the beginning of an element start tag: "<prefix:localName"
        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            Debug.Assert(localName != null && localName.Length > 0);
            Debug.Assert(prefix != null);

            _bufBytes[_bufPos++] = (byte)'<';
            if (!string.IsNullOrEmpty(prefix))
            {
                RawText(prefix);
                _bufBytes[_bufPos++] = (byte)':';
            }

            RawText(localName);

            _attrEndPos = _bufPos;
        }

        // Serialize the end of an element start tag in preparation for content serialization: ">"
        internal override void StartElementContent()
        {
            _bufBytes[_bufPos++] = (byte)'>';

            // StartElementContent is always called; therefore, in order to allow shortcut syntax, we save the
            // position of the '>' character.  If WriteEndElement is called and no other characters have been
            // output, then the '>' character can be overwritten with the shortcut syntax " />".
            _contentPos = _bufPos;
        }

        // Serialize an element end tag: "</prefix:localName>", if content was output.  Otherwise, serialize
        // the shortcut syntax: " />".
        internal override void WriteEndElement(string prefix, string localName, string ns)
        {
            Debug.Assert(localName != null && localName.Length > 0);
            Debug.Assert(prefix != null);

            if (_contentPos != _bufPos)
            {
                // Content has been output, so can't use shortcut syntax
                _bufBytes[_bufPos++] = (byte)'<';
                _bufBytes[_bufPos++] = (byte)'/';

                if (!string.IsNullOrEmpty(prefix))
                {
                    RawText(prefix);
                    _bufBytes[_bufPos++] = (byte)':';
                }
                RawText(localName);
                _bufBytes[_bufPos++] = (byte)'>';
            }
            else
            {
                // Use shortcut syntax; overwrite the already output '>' character
                _bufPos--;
                _bufBytes[_bufPos++] = (byte)' ';
                _bufBytes[_bufPos++] = (byte)'/';
                _bufBytes[_bufPos++] = (byte)'>';
            }
        }

        // Serialize a full element end tag: "</prefix:localName>"
        internal override void WriteFullEndElement(string prefix, string localName, string ns)
        {
            Debug.Assert(localName != null && localName.Length > 0);
            Debug.Assert(prefix != null);

            _bufBytes[_bufPos++] = (byte)'<';
            _bufBytes[_bufPos++] = (byte)'/';

            if (!string.IsNullOrEmpty(prefix))
            {
                RawText(prefix);
                _bufBytes[_bufPos++] = (byte)':';
            }
            RawText(localName);
            _bufBytes[_bufPos++] = (byte)'>';
        }

        // Serialize an attribute tag using double quotes around the attribute value: 'prefix:localName="'
        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            Debug.Assert(localName != null && localName.Length > 0);
            Debug.Assert(prefix != null);

            if (_attrEndPos == _bufPos)
            {
                _bufBytes[_bufPos++] = (byte)' ';
            }

            if (prefix != null && prefix.Length > 0)
            {
                RawText(prefix);
                _bufBytes[_bufPos++] = (byte)':';
            }
            RawText(localName);
            _bufBytes[_bufPos++] = (byte)'=';
            _bufBytes[_bufPos++] = (byte)'"';

            _inAttributeValue = true;
        }

        // Serialize the end of an attribute value using double quotes: '"'
        public override void WriteEndAttribute()
        {

            _bufBytes[_bufPos++] = (byte)'"';
            _inAttributeValue = false;
            _attrEndPos = _bufPos;
        }

        internal override void WriteNamespaceDeclaration(string prefix, string namespaceName)
        {
            Debug.Assert(prefix != null && namespaceName != null);

            WriteStartNamespaceDeclaration(prefix);
            WriteString(namespaceName);
            WriteEndNamespaceDeclaration();
        }

        internal override bool SupportsNamespaceDeclarationInChunks
        {
            get
            {
                return true;
            }
        }

        internal override void WriteStartNamespaceDeclaration(string prefix)
        {
            Debug.Assert(prefix != null);

            if (_attrEndPos == _bufPos)
            {
                _bufBytes[_bufPos++] = (byte)' ';
            }

            if (prefix.Length == 0)
            {
                RawText("xmlns=\"");
            }
            else
            {
                RawText("xmlns:");
                RawText(prefix);
                _bufBytes[_bufPos++] = (byte)'=';
                _bufBytes[_bufPos++] = (byte)'"';
            }

            _inAttributeValue = true;
        }

        internal override void WriteEndNamespaceDeclaration()
        {
            _inAttributeValue = false;

            _bufBytes[_bufPos++] = (byte)'"';
            _attrEndPos = _bufPos;
        }

        // Serialize a CData section.  If the "]]>" pattern is found within
        // the text, replace it with "]]><![CDATA[>".
        public override void WriteCData(string? text)
        {
            Debug.Assert(text != null);

            if (_mergeCDataSections && _bufPos == _cdataPos)
            {
                // Merge adjacent cdata sections - overwrite the "]]>" characters
                Debug.Assert(_bufPos >= 4);
                _bufPos -= 3;
            }
            else
            {
                // Start a new cdata section
                _bufBytes[_bufPos++] = (byte)'<';
                _bufBytes[_bufPos++] = (byte)'!';
                _bufBytes[_bufPos++] = (byte)'[';
                _bufBytes[_bufPos++] = (byte)'C';
                _bufBytes[_bufPos++] = (byte)'D';
                _bufBytes[_bufPos++] = (byte)'A';
                _bufBytes[_bufPos++] = (byte)'T';
                _bufBytes[_bufPos++] = (byte)'A';
                _bufBytes[_bufPos++] = (byte)'[';
            }

            WriteCDataSection(text);

            _bufBytes[_bufPos++] = (byte)']';
            _bufBytes[_bufPos++] = (byte)']';
            _bufBytes[_bufPos++] = (byte)'>';

            _textPos = _bufPos;
            _cdataPos = _bufPos;
        }

        // Serialize a comment.
        public override void WriteComment(string? text)
        {
            Debug.Assert(text != null);

            _bufBytes[_bufPos++] = (byte)'<';
            _bufBytes[_bufPos++] = (byte)'!';
            _bufBytes[_bufPos++] = (byte)'-';
            _bufBytes[_bufPos++] = (byte)'-';

            WriteCommentOrPi(text, '-');

            _bufBytes[_bufPos++] = (byte)'-';
            _bufBytes[_bufPos++] = (byte)'-';
            _bufBytes[_bufPos++] = (byte)'>';
        }

        // Serialize a processing instruction.
        public override void WriteProcessingInstruction(string name, string? text)
        {
            Debug.Assert(name != null && name.Length > 0);
            Debug.Assert(text != null);

            _bufBytes[_bufPos++] = (byte)'<';
            _bufBytes[_bufPos++] = (byte)'?';
            RawText(name);

            if (text.Length > 0)
            {
                _bufBytes[_bufPos++] = (byte)' ';
                WriteCommentOrPi(text, '?');
            }

            _bufBytes[_bufPos++] = (byte)'?';
            _bufBytes[_bufPos++] = (byte)'>';
        }

        // Serialize an entity reference.
        public override void WriteEntityRef(string name)
        {
            Debug.Assert(name != null && name.Length > 0);

            _bufBytes[_bufPos++] = (byte)'&';
            RawText(name);
            _bufBytes[_bufPos++] = (byte)';';

            if (_bufPos > _bufLen)
            {
                FlushBuffer();
            }

            _textPos = _bufPos;
        }

        // Serialize a character entity reference.
        public override void WriteCharEntity(char ch)
        {
            string strVal = ((int)ch).ToString("X", NumberFormatInfo.InvariantInfo);

            if (_checkCharacters && !XmlCharType.IsCharData(ch))
            {
                // we just have a single char, not a surrogate, therefore we have to pass in '\0' for the second char
                throw XmlConvert.CreateInvalidCharException(ch, '\0');
            }

            _bufBytes[_bufPos++] = (byte)'&';
            _bufBytes[_bufPos++] = (byte)'#';
            _bufBytes[_bufPos++] = (byte)'x';
            RawText(strVal);
            _bufBytes[_bufPos++] = (byte)';';

            if (_bufPos > _bufLen)
            {
                FlushBuffer();
            }

            _textPos = _bufPos;
        }

        // Serialize a whitespace node.

        public override unsafe void WriteWhitespace(string? ws)
        {
            Debug.Assert(ws != null);

            fixed (char* pSrc = ws)
            {
                char* pSrcEnd = pSrc + ws.Length;
                if (_inAttributeValue)
                {
                    WriteAttributeTextBlock(pSrc, pSrcEnd);
                }
                else
                {
                    WriteElementTextBlock(pSrc, pSrcEnd);
                }
            }
        }

        // Serialize either attribute or element text using XML rules.

        public override unsafe void WriteString(string? text)
        {
            Debug.Assert(text != null);

            fixed (char* pSrc = text)
            {
                char* pSrcEnd = pSrc + text.Length;
                if (_inAttributeValue)
                {
                    WriteAttributeTextBlock(pSrc, pSrcEnd);
                }
                else
                {
                    WriteElementTextBlock(pSrc, pSrcEnd);
                }
            }
        }

        // Serialize surrogate character entity.
        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            int surrogateChar = XmlCharType.CombineSurrogateChar(lowChar, highChar);

            _bufBytes[_bufPos++] = (byte)'&';
            _bufBytes[_bufPos++] = (byte)'#';
            _bufBytes[_bufPos++] = (byte)'x';
            RawText(surrogateChar.ToString("X", NumberFormatInfo.InvariantInfo));
            _bufBytes[_bufPos++] = (byte)';';
            _textPos = _bufPos;
        }

        // Serialize either attribute or element text using XML rules.
        // Arguments are validated in the XmlWellformedWriter layer.

        public override unsafe void WriteChars(char[] buffer, int index, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(index >= 0);
            Debug.Assert(count >= 0 && index + count <= buffer.Length);

            fixed (char* pSrcBegin = &buffer[index])
            {
                if (_inAttributeValue)
                {
                    WriteAttributeTextBlock(pSrcBegin, pSrcBegin + count);
                }
                else
                {
                    WriteElementTextBlock(pSrcBegin, pSrcBegin + count);
                }
            }
        }

        // Serialize raw data.
        // Arguments are validated in the XmlWellformedWriter layer

        public override unsafe void WriteRaw(char[] buffer, int index, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(index >= 0);
            Debug.Assert(count >= 0 && index + count <= buffer.Length);

            fixed (char* pSrcBegin = &buffer[index])
            {
                WriteRawWithCharChecking(pSrcBegin, pSrcBegin + count);
            }

            _textPos = _bufPos;
        }

        // Serialize raw data.

        public override unsafe void WriteRaw(string data)
        {
            Debug.Assert(data != null);

            fixed (char* pSrcBegin = data)
            {
                WriteRawWithCharChecking(pSrcBegin, pSrcBegin + data.Length);
            }

            _textPos = _bufPos;
        }

        // Flush all bytes in the buffer to output and close the output stream or writer.
        public override void Close()
        {
            try
            {
                FlushBuffer();
            }
            finally
            {
                // Future calls to Close or Flush shouldn't write to Stream or Writer
                _writeToNull = true;

                if (_stream != null)
                {
                    try
                    {
                        _stream.Flush();
                    }
                    finally
                    {
                        try
                        {
                            if (_closeOutput)
                            {
                                _stream.Dispose();
                            }
                        }
                        finally
                        {
                            _stream = null!;
                        }
                    }
                }
            }
        }

        // Flush all characters in the buffer to output and call Flush() on the output object.
        public override void Flush()
        {
            FlushBuffer();

            _stream?.Flush();
        }

        //
        // Implementation methods
        //
        // Flush all characters in the buffer to output.  Do not flush the output object.
        protected virtual void FlushBuffer()
        {
            try
            {
                // Output all characters (except for previous characters stored at beginning of buffer)
                if (!_writeToNull)
                {
                    if (_bufPos - 1 > 0)
                    {
                        Debug.Assert(_stream != null);
                        _stream.Write(_bufBytes, 1, _bufPos - 1);
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
                _bufBytes[0] = _bufBytes[_bufPos - 1];

                if (IsSurrogateByte(_bufBytes[0]))
                {
                    // Last character was the first byte in a surrogate encoding, so move last three
                    // bytes of encoding to the beginning of the buffer.
                    _bufBytes[1] = _bufBytes[_bufPos];
                    _bufBytes[2] = _bufBytes[_bufPos + 1];
                    _bufBytes[3] = _bufBytes[_bufPos + 2];
                }

                // Reset buffer position
                _textPos = (_textPos == _bufPos) ? 1 : 0;
                _attrEndPos = (_attrEndPos == _bufPos) ? 1 : 0;
                _contentPos = 0;    // Needs to be zero, since overwriting '>' character is no longer possible
                _cdataPos = 0;      // Needs to be zero, since overwriting ']]>' characters is no longer possible
                _bufPos = 1;        // Buffer position starts at 1, because we need to be able to safely step back -1 in case we need to
                                   // close an empty element or in CDATA section detection of double ]; _bufBytes[0] will always be 0
            }
        }

        // Serialize text that is part of an attribute value.  The '&', '<', '>', and '"' characters
        // are entitized.
        protected unsafe void WriteAttributeTextBlock(char* pSrc, char* pSrcEnd)
        {
            fixed (byte* pDstBegin = _bufBytes)
            {
                byte* pDst = pDstBegin + _bufPos;

                int ch = 0;
                while (true)
                {
                    byte* pDstEnd = pDst + (pSrcEnd - pSrc);
                    if (pDstEnd > pDstBegin + _bufLen)
                    {
                        pDstEnd = pDstBegin + _bufLen;
                    }

                    while (pDst < pDstEnd && XmlCharType.IsAttributeValueChar((char)(ch = *pSrc)) && ch <= 0x7F)
                    {
                        *pDst = (byte)ch;
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
                        FlushBuffer();
                        pDst = pDstBegin + 1;
                        continue;
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
                            *pDst = (byte)ch;
                            pDst++;
                            break;
                        case (char)0x9:
                            if (_newLineHandling == NewLineHandling.None)
                            {
                                *pDst = (byte)ch;
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
                                *pDst = (byte)ch;
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
                                *pDst = (byte)ch;
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
                            /* Multibyte UTF8 character */
                            else
                            {
                                pDst = EncodeMultibyteUTF8(ch, pDst);
                                pSrc++;
                            }
                            continue;
                    }
                    pSrc++;
                }
                _bufPos = (int)(pDst - pDstBegin);
            }
        }

        // Serialize text that is part of element content.  The '&', '<', and '>' characters
        // are entitized.
        protected unsafe void WriteElementTextBlock(char* pSrc, char* pSrcEnd)
        {
            fixed (byte* pDstBegin = _bufBytes)
            {
                byte* pDst = pDstBegin + _bufPos;

                int ch = 0;
                while (true)
                {
                    byte* pDstEnd = pDst + (pSrcEnd - pSrc);
                    if (pDstEnd > pDstBegin + _bufLen)
                    {
                        pDstEnd = pDstBegin + _bufLen;
                    }

                    while (pDst < pDstEnd && XmlCharType.IsAttributeValueChar((char)(ch = *pSrc)) && ch <= 0x7F)
                    {
                        *pDst = (byte)ch;
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
                        FlushBuffer();
                        pDst = pDstBegin + 1;
                        continue;
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
                            *pDst = (byte)ch;
                            pDst++;
                            break;
                        case (char)0xA:
                            if (_newLineHandling == NewLineHandling.Replace)
                            {
                                pDst = WriteNewLine(pDst);
                            }
                            else
                            {
                                *pDst = (byte)ch;
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

                                    pDst = WriteNewLine(pDst);
                                    break;

                                case NewLineHandling.Entitize:
                                    // Entitize 0xD
                                    pDst = CarriageReturnEntity(pDst);
                                    break;
                                case NewLineHandling.None:
                                    *pDst = (byte)ch;
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
                            /* Multibyte UTF8 character */
                            else
                            {
                                pDst = EncodeMultibyteUTF8(ch, pDst);
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
        }

        protected unsafe void RawText(string s)
        {
            Debug.Assert(s != null);

            fixed (char* pSrcBegin = s)
            {
                RawText(pSrcBegin, pSrcBegin + s.Length);
            }
        }

        protected unsafe void RawText(char* pSrcBegin, char* pSrcEnd)
        {
            fixed (byte* pDstBegin = _bufBytes)
            {
                byte* pDst = pDstBegin + _bufPos;
                char* pSrc = pSrcBegin;

                int ch = 0;
                while (true)
                {
                    byte* pDstEnd = pDst + (pSrcEnd - pSrc);
                    if (pDstEnd > pDstBegin + _bufLen)
                    {
                        pDstEnd = pDstBegin + _bufLen;
                    }

                    while (pDst < pDstEnd && ((ch = *pSrc) <= 0x7F))
                    {
                        pSrc++;
                        *pDst = (byte)ch;
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
                        FlushBuffer();
                        pDst = pDstBegin + 1;
                        continue;
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
                    /* Multibyte UTF8 character */
                    else
                    {
                        pDst = EncodeMultibyteUTF8(ch, pDst);
                        pSrc++;
                    }
                }

                _bufPos = (int)(pDst - pDstBegin);
            }
        }

        protected unsafe void WriteRawWithCharChecking(char* pSrcBegin, char* pSrcEnd)
        {
            fixed (byte* pDstBegin = _bufBytes)
            {
                char* pSrc = pSrcBegin;
                byte* pDst = pDstBegin + _bufPos;

                int ch = 0;
                while (true)
                {
                    byte* pDstEnd = pDst + (pSrcEnd - pSrc);
                    if (pDstEnd > pDstBegin + _bufLen)
                    {
                        pDstEnd = pDstBegin + _bufLen;
                    }

                    while (pDst < pDstEnd && XmlCharType.IsTextChar((char)(ch = *pSrc)) && ch <= 0x7F)
                    {
                        *pDst = (byte)ch;
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
                        FlushBuffer();
                        pDst = pDstBegin + 1;
                        continue;
                    }

                    // handle special characters
                    switch (ch)
                    {
                        case ']':
                        case '<':
                        case '&':
                        case (char)0x9:
                            *pDst = (byte)ch;
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

                                pDst = WriteNewLine(pDst);
                            }
                            else
                            {
                                *pDst = (byte)ch;
                                pDst++;
                            }
                            break;
                        case (char)0xA:
                            if (_newLineHandling == NewLineHandling.Replace)
                            {
                                pDst = WriteNewLine(pDst);
                            }
                            else
                            {
                                *pDst = (byte)ch;
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
                            /* Multibyte UTF8 character */
                            else
                            {
                                pDst = EncodeMultibyteUTF8(ch, pDst);
                                pSrc++;
                            }
                            continue;
                    }
                    pSrc++;
                }
                _bufPos = (int)(pDst - pDstBegin);
            }
        }

        protected unsafe void WriteCommentOrPi(string text, int stopChar)
        {
            if (text.Length == 0)
            {
                if (_bufPos >= _bufLen)
                {
                    FlushBuffer();
                }
                return;
            }
            // write text
            fixed (char* pSrcBegin = text)

            fixed (byte* pDstBegin = _bufBytes)
            {
                char* pSrc = pSrcBegin;

                char* pSrcEnd = pSrcBegin + text.Length;

                byte* pDst = pDstBegin + _bufPos;

                int ch = 0;
                while (true)
                {
                    byte* pDstEnd = pDst + (pSrcEnd - pSrc);
                    if (pDstEnd > pDstBegin + _bufLen)
                    {
                        pDstEnd = pDstBegin + _bufLen;
                    }

                    while (pDst < pDstEnd && XmlCharType.IsTextChar((char)(ch = *pSrc)) && ch != stopChar && ch <= 0x7F)
                    {
                        *pDst = (byte)ch;
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
                        FlushBuffer();
                        pDst = pDstBegin + 1;
                        continue;
                    }

                    // handle special characters
                    switch (ch)
                    {
                        case '-':
                            *pDst = (byte)'-';
                            pDst++;
                            if (ch == stopChar)
                            {
                                // Insert space between adjacent dashes or before comment's end dashes
                                if (pSrc + 1 == pSrcEnd || *(pSrc + 1) == '-')
                                {
                                    *pDst = (byte)' ';
                                    pDst++;
                                }
                            }
                            break;
                        case '?':
                            *pDst = (byte)'?';
                            pDst++;
                            if (ch == stopChar)
                            {
                                // Processing instruction: insert space between adjacent '?' and '>'
                                if (pSrc + 1 < pSrcEnd && *(pSrc + 1) == '>')
                                {
                                    *pDst = (byte)' ';
                                    pDst++;
                                }
                            }
                            break;
                        case ']':
                            *pDst = (byte)']';
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

                                pDst = WriteNewLine(pDst);
                            }
                            else
                            {
                                *pDst = (byte)ch;
                                pDst++;
                            }
                            break;
                        case (char)0xA:
                            if (_newLineHandling == NewLineHandling.Replace)
                            {
                                pDst = WriteNewLine(pDst);
                            }
                            else
                            {
                                *pDst = (byte)ch;
                                pDst++;
                            }
                            break;
                        case '<':
                        case '&':
                        case (char)0x9:
                            *pDst = (byte)ch;
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
                            /* Multibyte UTF8 character */
                            else
                            {
                                pDst = EncodeMultibyteUTF8(ch, pDst);
                                pSrc++;
                            }
                            continue;
                    }
                    pSrc++;
                }
                _bufPos = (int)(pDst - pDstBegin);
            }
        }

        protected unsafe void WriteCDataSection(string text)
        {
            if (text.Length == 0)
            {
                if (_bufPos >= _bufLen)
                {
                    FlushBuffer();
                }
                return;
            }

            // write text

            fixed (char* pSrcBegin = text)

            fixed (byte* pDstBegin = _bufBytes)
            {
                char* pSrc = pSrcBegin;

                char* pSrcEnd = pSrcBegin + text.Length;

                byte* pDst = pDstBegin + _bufPos;

                int ch = 0;
                while (true)
                {
                    byte* pDstEnd = pDst + (pSrcEnd - pSrc);
                    if (pDstEnd > pDstBegin + _bufLen)
                    {
                        pDstEnd = pDstBegin + _bufLen;
                    }

                    while (pDst < pDstEnd && XmlCharType.IsAttributeValueChar((char)(ch = *pSrc)) && ch != ']' && ch <= 0x7F)
                    {
                        *pDst = (byte)ch;
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
                        FlushBuffer();
                        pDst = pDstBegin + 1;
                        continue;
                    }

                    // handle special characters
                    switch (ch)
                    {
                        case '>':
                            if (_hadDoubleBracket && pDst[-1] == (byte)']')
                            {   // pDst[-1] will always correct - there is a padding character at _bufBytes[0]
                                // The characters "]]>" were found within the CData text
                                pDst = RawEndCData(pDst);
                                pDst = RawStartCData(pDst);
                            }
                            *pDst = (byte)'>';
                            pDst++;
                            break;
                        case ']':
                            if (pDst[-1] == (byte)']')
                            {   // pDst[-1] will always correct - there is a padding character at _bufBytes[0]
                                _hadDoubleBracket = true;
                            }
                            else
                            {
                                _hadDoubleBracket = false;
                            }
                            *pDst = (byte)']';
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

                                pDst = WriteNewLine(pDst);
                            }
                            else
                            {
                                *pDst = (byte)ch;
                                pDst++;
                            }
                            break;
                        case (char)0xA:
                            if (_newLineHandling == NewLineHandling.Replace)
                            {
                                pDst = WriteNewLine(pDst);
                            }
                            else
                            {
                                *pDst = (byte)ch;
                                pDst++;
                            }
                            break;
                        case '&':
                        case '<':
                        case '"':
                        case '\'':
                        case (char)0x9:
                            *pDst = (byte)ch;
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
                            /* Multibyte UTF8 character */
                            else
                            {
                                pDst = EncodeMultibyteUTF8(ch, pDst);
                                pSrc++;
                            }
                            continue;
                    }
                    pSrc++;
                }
                _bufPos = (int)(pDst - pDstBegin);
            }
        }

        // Returns true if UTF8 encoded byte is first of four bytes that encode a surrogate pair.
        // To do this, detect the bit pattern 11110xxx.
        private static bool IsSurrogateByte(byte b)
        {
            return (b & 0xF8) == 0xF0;
        }

        private static unsafe byte* EncodeSurrogate(char* pSrc, char* pSrcEnd, byte* pDst)
        {
            Debug.Assert(XmlCharType.IsSurrogate(*pSrc));

            int ch = *pSrc;
            if (ch <= XmlCharType.SurHighEnd)
            {
                if (pSrc + 1 < pSrcEnd)
                {
                    int lowChar = pSrc[1];
                    if (lowChar >= XmlCharType.SurLowStart &&
                        (LocalAppContextSwitches.DontThrowOnInvalidSurrogatePairs || lowChar <= XmlCharType.SurLowEnd))
                    {
                        // Calculate Unicode scalar value for easier manipulations (see section 3.7 in Unicode spec)
                        // The scalar value repositions surrogate values to start at 0x10000.

                        ch = XmlCharType.CombineSurrogateChar(lowChar, ch);

                        pDst[0] = (byte)(0xF0 | (ch >> 18));
                        pDst[1] = (byte)(0x80 | (ch >> 12) & 0x3F);
                        pDst[2] = (byte)(0x80 | (ch >> 6) & 0x3F);
                        pDst[3] = (byte)(0x80 | ch & 0x3F);
                        pDst += 4;

                        return pDst;
                    }
                    throw XmlConvert.CreateInvalidSurrogatePairException((char)lowChar, (char)ch);
                }
                throw new ArgumentException(SR.Xml_InvalidSurrogateMissingLowChar);
            }
            throw XmlConvert.CreateInvalidHighSurrogateCharException((char)ch);
        }

        private unsafe byte* InvalidXmlChar(int ch, byte* pDst, bool entitize)
        {
            Debug.Assert(!XmlCharType.IsWhiteSpace((char)ch));
            Debug.Assert(!XmlCharType.IsAttributeValueChar((char)ch));

            if (_checkCharacters)
            {
                // This method will never be called on surrogates, so it is ok to pass in '\0' to the CreateInvalidCharException
                throw XmlConvert.CreateInvalidCharException((char)ch, '\0');
            }
            else
            {
                if (entitize)
                {
                    return CharEntity(pDst, (char)ch);
                }
                else
                {
                    if (ch < 0x80)
                    {
                        *pDst = (byte)ch;
                        pDst++;
                    }
                    else
                    {
                        pDst = EncodeMultibyteUTF8(ch, pDst);
                    }

                    return pDst;
                }
            }
        }

        internal unsafe void EncodeChar(ref char* pSrc, char* pSrcEnd, ref byte* pDst)
        {
            int ch = *pSrc;
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
            /* Multibyte UTF8 character */
            else
            {
                pDst = EncodeMultibyteUTF8(ch, pDst);
                pSrc++;
            }
        }

        internal static unsafe byte* EncodeMultibyteUTF8(int ch, byte* pDst)
        {
            Debug.Assert(ch >= 0x80 && !XmlCharType.IsSurrogate(ch));

            unchecked
            {
                /* UTF8-2: If ch is in 0x80-0x7ff range, then use 2 bytes to encode it */
                if (ch < 0x800)
                {
                    *pDst = (byte)((sbyte)0xC0 | (ch >> 6));
                }
                /* UTF8-3: If ch is anything else, then default to using 3 bytes to encode it. */
                else
                {
                    *pDst = (byte)((sbyte)0xE0 | (ch >> 12));
                    pDst++;

                    *pDst = (byte)((sbyte)0x80 | (ch >> 6) & 0x3F);
                }
            }

            pDst++;
            *pDst = (byte)(0x80 | ch & 0x3F);
            return pDst + 1;
        }

        // Encode *pSrc as a sequence of UTF8 bytes.  Write the bytes to pDst and return an updated pointer.

        internal static unsafe void CharToUTF8(ref char* pSrc, char* pSrcEnd, ref byte* pDst)
        {
            int ch = *pSrc;
            if (ch <= 0x7F)
            {
                *pDst = (byte)ch;
                pDst++;
                pSrc++;
            }
            else if (XmlCharType.IsSurrogate(ch))
            {
                pDst = EncodeSurrogate(pSrc, pSrcEnd, pDst);
                pSrc += 2;
            }
            else
            {
                pDst = EncodeMultibyteUTF8(ch, pDst);
                pSrc++;
            }
        }

        // Write NewLineChars to the specified buffer position and return an updated position.
        protected unsafe byte* WriteNewLine(byte* pDst)
        {
            fixed (byte* pDstBegin = _bufBytes)
            {
                _bufPos = (int)(pDst - pDstBegin);
                // Let RawText do the real work
                RawText(_newLineChars);
                return pDstBegin + _bufPos;
            }
        }

        // Following methods do not check whether pDst is beyond the bufSize because the buffer was allocated with a OVERFLOW to accommodate
        // for the writes of small constant-length string as below.

        // Entitize '<' as "&lt;".  Return an updated pointer.

        protected static unsafe byte* LtEntity(byte* pDst)
        {
            pDst[0] = (byte)'&';
            pDst[1] = (byte)'l';
            pDst[2] = (byte)'t';
            pDst[3] = (byte)';';
            return pDst + 4;
        }

        // Entitize '>' as "&gt;".  Return an updated pointer.

        protected static unsafe byte* GtEntity(byte* pDst)
        {
            pDst[0] = (byte)'&';
            pDst[1] = (byte)'g';
            pDst[2] = (byte)'t';
            pDst[3] = (byte)';';
            return pDst + 4;
        }

        // Entitize '&' as "&amp;".  Return an updated pointer.

        protected static unsafe byte* AmpEntity(byte* pDst)
        {
            pDst[0] = (byte)'&';
            pDst[1] = (byte)'a';
            pDst[2] = (byte)'m';
            pDst[3] = (byte)'p';
            pDst[4] = (byte)';';
            return pDst + 5;
        }

        // Entitize '"' as "&quot;".  Return an updated pointer.

        protected static unsafe byte* QuoteEntity(byte* pDst)
        {
            pDst[0] = (byte)'&';
            pDst[1] = (byte)'q';
            pDst[2] = (byte)'u';
            pDst[3] = (byte)'o';
            pDst[4] = (byte)'t';
            pDst[5] = (byte)';';
            return pDst + 6;
        }

        // Entitize '\t' as "&#x9;".  Return an updated pointer.

        protected static unsafe byte* TabEntity(byte* pDst)
        {
            pDst[0] = (byte)'&';
            pDst[1] = (byte)'#';
            pDst[2] = (byte)'x';
            pDst[3] = (byte)'9';
            pDst[4] = (byte)';';
            return pDst + 5;
        }

        // Entitize 0xa as "&#xA;".  Return an updated pointer.

        protected static unsafe byte* LineFeedEntity(byte* pDst)
        {
            pDst[0] = (byte)'&';
            pDst[1] = (byte)'#';
            pDst[2] = (byte)'x';
            pDst[3] = (byte)'A';
            pDst[4] = (byte)';';
            return pDst + 5;
        }

        // Entitize 0xd as "&#xD;".  Return an updated pointer.

        protected static unsafe byte* CarriageReturnEntity(byte* pDst)
        {
            pDst[0] = (byte)'&';
            pDst[1] = (byte)'#';
            pDst[2] = (byte)'x';
            pDst[3] = (byte)'D';
            pDst[4] = (byte)';';
            return pDst + 5;
        }

        private static unsafe byte* CharEntity(byte* pDst, char ch)
        {
            string s = ((int)ch).ToString("X", NumberFormatInfo.InvariantInfo);
            pDst[0] = (byte)'&';
            pDst[1] = (byte)'#';
            pDst[2] = (byte)'x';
            pDst += 3;

            fixed (char* pSrc = s)
            {
                char* pS = pSrc;
                while ((*pDst++ = (byte)*pS++) != 0) ;
            }

            pDst[-1] = (byte)';';
            return pDst;
        }

        // Write "<![CDATA[" to the specified buffer.  Return an updated pointer.

        protected static unsafe byte* RawStartCData(byte* pDst)
        {
            pDst[0] = (byte)'<';
            pDst[1] = (byte)'!';
            pDst[2] = (byte)'[';
            pDst[3] = (byte)'C';
            pDst[4] = (byte)'D';
            pDst[5] = (byte)'A';
            pDst[6] = (byte)'T';
            pDst[7] = (byte)'A';
            pDst[8] = (byte)'[';
            return pDst + 9;
        }

        // Write "]]>" to the specified buffer.  Return an updated pointer.

        protected static unsafe byte* RawEndCData(byte* pDst)
        {
            pDst[0] = (byte)']';
            pDst[1] = (byte)']';
            pDst[2] = (byte)'>';
            return pDst + 3;
        }

        protected static void ValidateContentChars(string chars, string propertyName, bool allowOnlyWhitespace)
        {
            if (allowOnlyWhitespace)
            {
                if (!XmlCharType.IsOnlyWhitespace(chars))
                {
                    throw new ArgumentException(SR.Format(SR.Xml_IndentCharsNotWhitespace, propertyName));
                }
            }
            else
            {
                string error;
                for (int i = 0; i < chars.Length; i++)
                {
                    if (!XmlCharType.IsTextChar(chars[i]))
                    {
                        switch (chars[i])
                        {
                            case '\n':
                            case '\r':
                            case '\t':
                                continue;
                            case '<':
                            case '&':
                            case ']':
                                error = SR.Format(SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(chars, i));
                                goto Error;
                            default:
                                if (XmlCharType.IsHighSurrogate(chars[i]))
                                {
                                    if (i + 1 < chars.Length)
                                    {
                                        if (XmlCharType.IsLowSurrogate(chars[i + 1]))
                                        {
                                            i++;
                                            continue;
                                        }
                                    }
                                    error = SR.Xml_InvalidSurrogateMissingLowChar;
                                    goto Error;
                                }
                                else if (XmlCharType.IsLowSurrogate(chars[i]))
                                {
                                    error = SR.Format(SR.Xml_InvalidSurrogateHighChar, ((uint)chars[i]).ToString("X", CultureInfo.InvariantCulture));
                                    goto Error;
                                }
                                continue;
                        }
                    }
                }
                return;

            Error:
                throw new ArgumentException(SR.Format(SR.Xml_InvalidCharsInIndent, new string[] { propertyName, error }));
            }
        }
    }

    // Same as base text writer class except that elements, attributes, comments, and pi's are indented.
    internal sealed partial class XmlUtf8RawTextWriterIndent : XmlUtf8RawTextWriter
    {
        //
        // Fields
        //
        private int _indentLevel;
        private bool _newLineOnAttributes;
        private string _indentChars;

        private bool _mixedContent;
        private BitStack _mixedContentStack;

        private ConformanceLevel _conformanceLevel = ConformanceLevel.Auto;

        //
        // Constructors
        //
        public XmlUtf8RawTextWriterIndent(Stream stream, XmlWriterSettings settings) : base(stream, settings)
        {
            Init(settings);
        }

        //
        // XmlWriter methods
        //
        public override XmlWriterSettings Settings
        {
            get
            {
                XmlWriterSettings settings = base.Settings;

                settings.ReadOnly = false;
                settings.Indent = true;
                settings.IndentChars = _indentChars;
                settings.NewLineOnAttributes = _newLineOnAttributes;
                settings.ReadOnly = true;

                return settings;
            }
        }

        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            // Add indentation
            if (!_mixedContent && base._textPos != base._bufPos)
            {
                WriteIndent();
            }
            base.WriteDocType(name, pubid, sysid, subset);
        }

        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            Debug.Assert(!string.IsNullOrEmpty(localName) && prefix != null && ns != null);

            // Add indentation
            if (!_mixedContent && base._textPos != base._bufPos)
            {
                WriteIndent();
            }
            _indentLevel++;
            _mixedContentStack.PushBit(_mixedContent);

            base.WriteStartElement(prefix, localName, ns);
        }

        internal override void StartElementContent()
        {
            // If this is the root element and we're writing a document
            //   do not inherit the _mixedContent flag into the root element.
            //   This is to allow for whitespace nodes on root level
            //   without disabling indentation for the whole document.
            if (_indentLevel == 1 && _conformanceLevel == ConformanceLevel.Document)
            {
                _mixedContent = false;
            }
            else
            {
                _mixedContent = _mixedContentStack.PeekBit();
            }
            base.StartElementContent();
        }

        internal override void OnRootElement(ConformanceLevel currentConformanceLevel)
        {
            // Just remember the current conformance level
            _conformanceLevel = currentConformanceLevel;
        }

        internal override void WriteEndElement(string prefix, string localName, string ns)
        {
            // Add indentation
            _indentLevel--;
            if (!_mixedContent && base._contentPos != base._bufPos)
            {
                // There was content, so try to indent
                if (base._textPos != base._bufPos)
                {
                    WriteIndent();
                }
            }
            _mixedContent = _mixedContentStack.PopBit();

            base.WriteEndElement(prefix, localName, ns);
        }

        internal override void WriteFullEndElement(string prefix, string localName, string ns)
        {
            // Add indentation
            _indentLevel--;
            if (!_mixedContent && base._contentPos != base._bufPos)
            {
                // There was content, so try to indent
                if (base._textPos != base._bufPos)
                {
                    WriteIndent();
                }
            }
            _mixedContent = _mixedContentStack.PopBit();

            base.WriteFullEndElement(prefix, localName, ns);
        }

        // Same as base class, plus possible indentation.
        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            // Add indentation
            if (_newLineOnAttributes)
            {
                WriteIndent();
            }

            base.WriteStartAttribute(prefix, localName, ns);
        }

        // Same as base class, plus possible indentation.
        internal override void WriteStartNamespaceDeclaration(string prefix)
        {
            // Add indentation
            if (_newLineOnAttributes)
            {
                WriteIndent();
            }

            base.WriteStartNamespaceDeclaration(prefix);
        }

        public override void WriteCData(string? text)
        {
            _mixedContent = true;
            base.WriteCData(text);
        }

        public override void WriteComment(string? text)
        {
            if (!_mixedContent && base._textPos != base._bufPos)
            {
                WriteIndent();
            }

            base.WriteComment(text);
        }

        public override void WriteProcessingInstruction(string target, string? text)
        {
            if (!_mixedContent && base._textPos != base._bufPos)
            {
                WriteIndent();
            }

            base.WriteProcessingInstruction(target, text);
        }

        public override void WriteEntityRef(string name)
        {
            _mixedContent = true;
            base.WriteEntityRef(name);
        }

        public override void WriteCharEntity(char ch)
        {
            _mixedContent = true;
            base.WriteCharEntity(ch);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            _mixedContent = true;
            base.WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override void WriteWhitespace(string? ws)
        {
            _mixedContent = true;
            base.WriteWhitespace(ws);
        }

        public override void WriteString(string? text)
        {
            _mixedContent = true;
            base.WriteString(text);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            _mixedContent = true;
            base.WriteChars(buffer, index, count);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            _mixedContent = true;
            base.WriteRaw(buffer, index, count);
        }

        public override void WriteRaw(string data)
        {
            _mixedContent = true;
            base.WriteRaw(data);
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            _mixedContent = true;
            base.WriteBase64(buffer, index, count);
        }

        //
        // Private methods
        //
        [MemberNotNull(nameof(_indentChars))]
        [MemberNotNull(nameof(_mixedContentStack))]
        private void Init(XmlWriterSettings settings)
        {
            _indentLevel = 0;
            _indentChars = settings.IndentChars;
            _newLineOnAttributes = settings.NewLineOnAttributes;
            _mixedContentStack = new BitStack();

            // check indent characters that they are valid XML characters
            if (base._checkCharacters)
            {
                if (_newLineOnAttributes)
                {
                    ValidateContentChars(_indentChars, "IndentChars", true);
                    ValidateContentChars(_newLineChars, "NewLineChars", true);
                }
                else
                {
                    ValidateContentChars(_indentChars, "IndentChars", false);
                    if (base._newLineHandling != NewLineHandling.Replace)
                    {
                        ValidateContentChars(_newLineChars, "NewLineChars", false);
                    }
                }
            }
        }

        // Add indentation to output.  Write newline and then repeat IndentChars for each indent level.
        private void WriteIndent()
        {
            RawText(base._newLineChars);
            for (int i = _indentLevel; i > 0; i--)
            {
                RawText(_indentChars);
            }
        }
    }
}
