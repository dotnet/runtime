// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// WARNING: This file is generated and should not be modified directly.
// Instead, modify XmlRawTextWriterGenerator.ttinclude

#nullable disable
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace System.Xml
{
    // Concrete implementation of XmlWriter abstract class that serializes events as encoded XML
    // text.  The general-purpose XmlEncodedTextWriter uses the Encoder class to output to any
    // encoding.  The XmlUtf8TextWriter class combined the encoding operation with serialization
    // in order to achieve better performance.
    internal partial class XmlEncodedRawTextWriter : XmlRawWriter
    {
        //
        // Fields
        //
        private readonly bool _useAsync;

        // main buffer
        protected byte[] _bufBytes;

        // output stream
        protected Stream _stream;

        // encoding of the stream or text writer
        protected Encoding _encoding;

        // buffer positions
        protected int _bufPos = 1;     // buffer position starts at 1, because we need to be able to safely step back -1 in case we need to
                                       // close an empty element or in CDATA section detection of double ]; _bufChars[0] will always be 0
        protected int _textPos = 1;    // text end position; don't indent first element, pi, or comment
        protected int _contentPos;     // element content end position
        protected int _cdataPos;       // cdata end position
        protected int _attrEndPos;     // end of the last attribute
        protected int _bufLen = BUFSIZE;

        // flags
        protected bool _writeToNull;
        protected bool _hadDoubleBracket;
        protected bool _inAttributeValue;
        protected int _bufBytesUsed;
        protected char[] _bufChars;

        // encoder for encoding chars in specified encoding when writing to stream
        protected Encoder _encoder;

        // output text writer
        protected TextWriter _writer;

        // escaping of characters invalid in the output encoding
        protected bool _trackTextContent;
        protected bool _inTextContent;
        private int _lastMarkPos;
        private int[] _textContentMarks;   // even indices contain text content start positions
                                           // odd indices contain markup start positions
        private readonly CharEntityEncoderFallback _charEntityFallback;

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
        private const int INIT_MARKS_COUNT = 64;

        //
        // Constructors
        //
        // Construct and initialize an instance of this class.
        protected XmlEncodedRawTextWriter(XmlWriterSettings settings)
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

        // Construct an instance of this class that outputs text to the TextWriter interface.
        public XmlEncodedRawTextWriter(TextWriter writer, XmlWriterSettings settings) : this(settings)
        {
            Debug.Assert(writer != null && settings != null);

            this._writer = writer;
            this._encoding = writer.Encoding;
            // the buffer is allocated will OVERFLOW in order to reduce checks when writing out constant size markup
            if (settings.Async)
            {
                _bufLen = ASYNCBUFSIZE;
            }
            this._bufChars = new char[_bufLen + OVERFLOW];

            // Write the xml declaration
            if (settings.AutoXmlDeclaration)
            {
                WriteXmlDeclaration(_standalone);
                _autoXmlDeclaration = true;
            }
        }

        // Construct an instance of this class that serializes to a Stream interface.
        public XmlEncodedRawTextWriter(Stream stream, XmlWriterSettings settings) : this(settings)
        {
            Debug.Assert(stream != null && settings != null);

            this._stream = stream;
            this._encoding = settings.Encoding;

            // the buffer is allocated will OVERFLOW in order to reduce checks when writing out constant size markup
            if (settings.Async)
            {
                _bufLen = ASYNCBUFSIZE;
            }

            _bufChars = new char[_bufLen + OVERFLOW];
            _bufBytes = new byte[_bufChars.Length];
            _bufBytesUsed = 0;

            // Init escaping of characters not fitting into the target encoding
            _trackTextContent = true;
            _inTextContent = false;
            _lastMarkPos = 0;
            _textContentMarks = new int[INIT_MARKS_COUNT];
            _textContentMarks[0] = 1;

            _charEntityFallback = new CharEntityEncoderFallback();

            // grab bom before possibly changing encoding settings
            ReadOnlySpan<byte> bom = _encoding.Preamble;

            _encoding = (Encoding)settings.Encoding.Clone();
            _encoding.EncoderFallback = _charEntityFallback;

            _encoder = _encoding.GetEncoder();

            if (!stream.CanSeek || stream.Position == 0)
            {
                if (bom.Length != 0)
                {
                    this._stream.Write(bom);
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
                if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }
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
        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            Debug.Assert(name != null && name.Length > 0);

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

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
                _bufChars[_bufPos++] = (char)'"';
            }
            else if (sysid != null)
            {
                RawText(" SYSTEM \"");
                RawText(sysid);
                _bufChars[_bufPos++] = (char)'"';
            }
            else
            {
                _bufChars[_bufPos++] = (char)' ';
            }

            if (subset != null)
            {
                _bufChars[_bufPos++] = (char)'[';
                RawText(subset);
                _bufChars[_bufPos++] = (char)']';
            }

            _bufChars[_bufPos++] = (char)'>';
        }

        // Serialize the beginning of an element start tag: "<prefix:localName"
        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            Debug.Assert(localName != null && localName.Length > 0);
            Debug.Assert(prefix != null);

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'<';
            if (prefix != null && prefix.Length != 0)
            {
                RawText(prefix);
                _bufChars[_bufPos++] = (char)':';
            }

            RawText(localName);

            _attrEndPos = _bufPos;
        }

        // Serialize the end of an element start tag in preparation for content serialization: ">"
        internal override void StartElementContent()
        {
            _bufChars[_bufPos++] = (char)'>';

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

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

            if (_contentPos != _bufPos)
            {
                // Content has been output, so can't use shortcut syntax
                _bufChars[_bufPos++] = (char)'<';
                _bufChars[_bufPos++] = (char)'/';

                if (prefix != null && prefix.Length != 0)
                {
                    RawText(prefix);
                    _bufChars[_bufPos++] = (char)':';
                }
                RawText(localName);
                _bufChars[_bufPos++] = (char)'>';
            }
            else
            {
                // Use shortcut syntax; overwrite the already output '>' character
                _bufPos--;
                _bufChars[_bufPos++] = (char)' ';
                _bufChars[_bufPos++] = (char)'/';
                _bufChars[_bufPos++] = (char)'>';
            }
        }

        // Serialize a full element end tag: "</prefix:localName>"
        internal override void WriteFullEndElement(string prefix, string localName, string ns)
        {
            Debug.Assert(localName != null && localName.Length > 0);
            Debug.Assert(prefix != null);

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'<';
            _bufChars[_bufPos++] = (char)'/';

            if (prefix != null && prefix.Length != 0)
            {
                RawText(prefix);
                _bufChars[_bufPos++] = (char)':';
            }
            RawText(localName);
            _bufChars[_bufPos++] = (char)'>';
        }

        // Serialize an attribute tag using double quotes around the attribute value: 'prefix:localName="'
        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            Debug.Assert(localName != null && localName.Length > 0);
            Debug.Assert(prefix != null);

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

            if (_attrEndPos == _bufPos)
            {
                _bufChars[_bufPos++] = (char)' ';
            }

            if (prefix != null && prefix.Length > 0)
            {
                RawText(prefix);
                _bufChars[_bufPos++] = (char)':';
            }
            RawText(localName);
            _bufChars[_bufPos++] = (char)'=';
            _bufChars[_bufPos++] = (char)'"';

            _inAttributeValue = true;
        }

        // Serialize the end of an attribute value using double quotes: '"'
        public override void WriteEndAttribute()
        {
            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'"';
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

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

            if (prefix.Length == 0)
            {
                RawText(" xmlns=\"");
            }
            else
            {
                RawText(" xmlns:");
                RawText(prefix);
                _bufChars[_bufPos++] = (char)'=';
                _bufChars[_bufPos++] = (char)'"';
            }

            _inAttributeValue = true;

            if (_trackTextContent && _inTextContent != true) { ChangeTextContentMark(true); }
        }

        internal override void WriteEndNamespaceDeclaration()
        {
            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }
            _inAttributeValue = false;

            _bufChars[_bufPos++] = (char)'"';
            _attrEndPos = _bufPos;
        }

        // Serialize a CData section.  If the "]]>" pattern is found within
        // the text, replace it with "]]><![CDATA[>".
        public override void WriteCData(string text)
        {
            Debug.Assert(text != null);

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

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

            WriteCDataSection(text);

            _bufChars[_bufPos++] = (char)']';
            _bufChars[_bufPos++] = (char)']';
            _bufChars[_bufPos++] = (char)'>';

            _textPos = _bufPos;
            _cdataPos = _bufPos;
        }

        // Serialize a comment.
        public override void WriteComment(string text)
        {
            Debug.Assert(text != null);

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'<';
            _bufChars[_bufPos++] = (char)'!';
            _bufChars[_bufPos++] = (char)'-';
            _bufChars[_bufPos++] = (char)'-';

            WriteCommentOrPi(text, '-');

            _bufChars[_bufPos++] = (char)'-';
            _bufChars[_bufPos++] = (char)'-';
            _bufChars[_bufPos++] = (char)'>';
        }

        // Serialize a processing instruction.
        public override void WriteProcessingInstruction(string name, string text)
        {
            Debug.Assert(name != null && name.Length > 0);
            Debug.Assert(text != null);

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'<';
            _bufChars[_bufPos++] = (char)'?';
            RawText(name);

            if (text.Length > 0)
            {
                _bufChars[_bufPos++] = (char)' ';
                WriteCommentOrPi(text, '?');
            }

            _bufChars[_bufPos++] = (char)'?';
            _bufChars[_bufPos++] = (char)'>';
        }

        // Serialize an entity reference.
        public override void WriteEntityRef(string name)
        {
            Debug.Assert(name != null && name.Length > 0);

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'&';
            RawText(name);
            _bufChars[_bufPos++] = (char)';';

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

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

            _bufChars[_bufPos++] = (char)'&';
            _bufChars[_bufPos++] = (char)'#';
            _bufChars[_bufPos++] = (char)'x';
            RawText(strVal);
            _bufChars[_bufPos++] = (char)';';

            if (_bufPos > _bufLen)
            {
                FlushBuffer();
            }

            _textPos = _bufPos;
        }

        // Serialize a whitespace node.

        public override unsafe void WriteWhitespace(string ws)
        {
            Debug.Assert(ws != null);

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

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

        public override unsafe void WriteString(string text)
        {
            Debug.Assert(text != null);

            if (_trackTextContent && _inTextContent != true) { ChangeTextContentMark(true); }

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
            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }
            int surrogateChar = XmlCharType.CombineSurrogateChar(lowChar, highChar);

            _bufChars[_bufPos++] = (char)'&';
            _bufChars[_bufPos++] = (char)'#';
            _bufChars[_bufPos++] = (char)'x';
            RawText(surrogateChar.ToString("X", NumberFormatInfo.InvariantInfo));
            _bufChars[_bufPos++] = (char)';';
            _textPos = _bufPos;
        }

        // Serialize either attribute or element text using XML rules.
        // Arguments are validated in the XmlWellformedWriter layer.

        public override unsafe void WriteChars(char[] buffer, int index, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(index >= 0);
            Debug.Assert(count >= 0 && index + count <= buffer.Length);

            if (_trackTextContent && _inTextContent != true) { ChangeTextContentMark(true); }

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

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

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

            if (_trackTextContent && _inTextContent != false) { ChangeTextContentMark(false); }

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
                FlushEncoder();
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
                            _stream = null;
                        }
                    }
                }
                else if (_writer != null)
                {
                    try
                    {
                        _writer.Flush();
                    }
                    finally
                    {
                        try
                        {
                            if (_closeOutput)
                            {
                                _writer.Dispose();
                            }
                        }
                        finally
                        {
                            _writer = null;
                        }
                    }
                }
            }
        }

        // Flush all characters in the buffer to output and call Flush() on the output object.
        public override void Flush()
        {
            FlushBuffer();
            FlushEncoder();

            if (_stream != null)
            {
                _stream.Flush();
            }
            else if (_writer != null)
            {
                _writer.Flush();
            }
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
                    Debug.Assert(_stream != null || _writer != null);

                    if (_stream != null)
                    {
                        if (_trackTextContent)
                        {
                            _charEntityFallback.Reset(_textContentMarks, _lastMarkPos);
                            // reset text content tracking

                            if ((_lastMarkPos & 1) != 0)
                            {
                                // If the previous buffer ended inside a text content we need to preserve that info
                                //   which means the next index to which we write has to be even
                                _textContentMarks[1] = 1;
                                _lastMarkPos = 1;
                            }
                            else
                            {
                                _lastMarkPos = 0;
                            }
                            Debug.Assert(_textContentMarks[0] == 1);
                        }
                        EncodeChars(1, _bufPos, true);
                    }
                    else
                    {
                        if (_bufPos - 1 > 0)
                        {
                            // Write text to TextWriter
                            _writer.Write(_bufChars, 1, _bufPos - 1);
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
                                   // close an empty element or in CDATA section detection of double ]; _bufChars[0] will always be 0
            }
        }

        private void EncodeChars(int startOffset, int endOffset, bool writeAllToStream)
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
                _encoder.Convert(_bufChars, startOffset, endOffset - startOffset, _bufBytes, _bufBytesUsed, _bufBytes.Length - _bufBytesUsed, false, out chEnc, out bEnc, out _);
                startOffset += chEnc;
                _bufBytesUsed += bEnc;
                if (_bufBytesUsed >= (_bufBytes.Length - 16))
                {
                    _stream.Write(_bufBytes, 0, _bufBytesUsed);
                    _bufBytesUsed = 0;
                }
            }
            if (writeAllToStream && _bufBytesUsed > 0)
            {
                _stream.Write(_bufBytes, 0, _bufBytesUsed);
                _bufBytesUsed = 0;
            }
        }

        private void FlushEncoder()
        {
            Debug.Assert(_bufPos == 1);
            if (_stream != null)
            {
                int bEnc;
                // decode no chars, just flush
                _encoder.Convert(_bufChars, 1, 0, _bufBytes, 0, _bufBytes.Length, true, out _, out bEnc, out _);
                if (bEnc != 0)
                {
                    _stream.Write(_bufBytes, 0, bEnc);
                }
            }
        }

        // Serialize text that is part of an attribute value.  The '&', '<', '>', and '"' characters
        // are entitized.
        protected unsafe void WriteAttributeTextBlock(char* pSrc, char* pSrcEnd)
        {
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
        }

        // Serialize text that is part of element content.  The '&', '<', and '>' characters
        // are entitized.
        protected unsafe void WriteElementTextBlock(char* pSrc, char* pSrcEnd)
        {
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
                            *pDst = (char)ch;
                            pDst++;
                            break;
                        case (char)0xA:
                            if (_newLineHandling == NewLineHandling.Replace)
                            {
                                pDst = WriteNewLine(pDst);
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

                                    pDst = WriteNewLine(pDst);
                                    break;

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
        }

        protected unsafe void WriteRawWithCharChecking(char* pSrcBegin, char* pSrcEnd)
        {
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

                                pDst = WriteNewLine(pDst);
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
                                pDst = WriteNewLine(pDst);
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

            fixed (char* pDstBegin = _bufChars)
            {
                char* pSrc = pSrcBegin;

                char* pSrcEnd = pSrcBegin + text.Length;

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
                        FlushBuffer();
                        pDst = pDstBegin + 1;
                        continue;
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

                                pDst = WriteNewLine(pDst);
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
                                pDst = WriteNewLine(pDst);
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

            fixed (char* pDstBegin = _bufChars)
            {
                char* pSrc = pSrcBegin;

                char* pSrcEnd = pSrcBegin + text.Length;

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
                        FlushBuffer();
                        pDst = pDstBegin + 1;
                        continue;
                    }

                    // handle special characters
                    switch (ch)
                    {
                        case '>':
                            if (_hadDoubleBracket && pDst[-1] == (char)']')
                            {   // pDst[-1] will always correct - there is a padding character at _bufChars[0]
                                // The characters "]]>" were found within the CData text
                                pDst = RawEndCData(pDst);
                                pDst = RawStartCData(pDst);
                            }
                            *pDst = (char)'>';
                            pDst++;
                            break;
                        case ']':
                            if (pDst[-1] == (char)']')
                            {   // pDst[-1] will always correct - there is a padding character at _bufChars[0]
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

                                pDst = WriteNewLine(pDst);
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
                                pDst = WriteNewLine(pDst);
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
        }

        private static unsafe char* EncodeSurrogate(char* pSrc, char* pSrcEnd, char* pDst)
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
                        pDst[0] = (char)ch;
                        pDst[1] = (char)lowChar;
                        pDst += 2;

                        return pDst;
                    }
                    throw XmlConvert.CreateInvalidSurrogatePairException((char)lowChar, (char)ch);
                }
                throw new ArgumentException(SR.Xml_InvalidSurrogateMissingLowChar);
            }
            throw XmlConvert.CreateInvalidHighSurrogateCharException((char)ch);
        }

        private unsafe char* InvalidXmlChar(int ch, char* pDst, bool entitize)
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
                    *pDst = (char)ch;
                    pDst++;

                    return pDst;
                }
            }
        }

        internal unsafe void EncodeChar(ref char* pSrc, char* pSrcEnd, ref char* pDst)
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
            /* Other character between SurLowEnd and 0xFFFE */
            else
            {
                *pDst = (char)ch;
                pDst++;
                pSrc++;
            }
        }

        protected void ChangeTextContentMark(bool value)
        {
            Debug.Assert(_inTextContent != value);
            Debug.Assert(_inTextContent || ((_lastMarkPos & 1) == 0));
            _inTextContent = value;
            if (_lastMarkPos + 1 == _textContentMarks.Length)
            {
                GrowTextContentMarks();
            }
            _textContentMarks[++_lastMarkPos] = _bufPos;
        }

        private void GrowTextContentMarks()
        {
            Debug.Assert(_lastMarkPos + 1 == _textContentMarks.Length);
            int[] newTextContentMarks = new int[_textContentMarks.Length * 2];
            Array.Copy(_textContentMarks, newTextContentMarks, _textContentMarks.Length);
            _textContentMarks = newTextContentMarks;
        }

        // Write NewLineChars to the specified buffer position and return an updated position.
        protected unsafe char* WriteNewLine(char* pDst)
        {
            fixed (char* pDstBegin = _bufChars)
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

        protected static unsafe char* LtEntity(char* pDst)
        {
            pDst[0] = (char)'&';
            pDst[1] = (char)'l';
            pDst[2] = (char)'t';
            pDst[3] = (char)';';
            return pDst + 4;
        }

        // Entitize '>' as "&gt;".  Return an updated pointer.

        protected static unsafe char* GtEntity(char* pDst)
        {
            pDst[0] = (char)'&';
            pDst[1] = (char)'g';
            pDst[2] = (char)'t';
            pDst[3] = (char)';';
            return pDst + 4;
        }

        // Entitize '&' as "&amp;".  Return an updated pointer.

        protected static unsafe char* AmpEntity(char* pDst)
        {
            pDst[0] = (char)'&';
            pDst[1] = (char)'a';
            pDst[2] = (char)'m';
            pDst[3] = (char)'p';
            pDst[4] = (char)';';
            return pDst + 5;
        }

        // Entitize '"' as "&quot;".  Return an updated pointer.

        protected static unsafe char* QuoteEntity(char* pDst)
        {
            pDst[0] = (char)'&';
            pDst[1] = (char)'q';
            pDst[2] = (char)'u';
            pDst[3] = (char)'o';
            pDst[4] = (char)'t';
            pDst[5] = (char)';';
            return pDst + 6;
        }

        // Entitize '\t' as "&#x9;".  Return an updated pointer.

        protected static unsafe char* TabEntity(char* pDst)
        {
            pDst[0] = (char)'&';
            pDst[1] = (char)'#';
            pDst[2] = (char)'x';
            pDst[3] = (char)'9';
            pDst[4] = (char)';';
            return pDst + 5;
        }

        // Entitize 0xa as "&#xA;".  Return an updated pointer.

        protected static unsafe char* LineFeedEntity(char* pDst)
        {
            pDst[0] = (char)'&';
            pDst[1] = (char)'#';
            pDst[2] = (char)'x';
            pDst[3] = (char)'A';
            pDst[4] = (char)';';
            return pDst + 5;
        }

        // Entitize 0xd as "&#xD;".  Return an updated pointer.

        protected static unsafe char* CarriageReturnEntity(char* pDst)
        {
            pDst[0] = (char)'&';
            pDst[1] = (char)'#';
            pDst[2] = (char)'x';
            pDst[3] = (char)'D';
            pDst[4] = (char)';';
            return pDst + 5;
        }

        private static unsafe char* CharEntity(char* pDst, char ch)
        {
            string s = ((int)ch).ToString("X", NumberFormatInfo.InvariantInfo);
            pDst[0] = (char)'&';
            pDst[1] = (char)'#';
            pDst[2] = (char)'x';
            pDst += 3;

            fixed (char* pSrc = s)
            {
                char* pS = pSrc;
                while ((*pDst++ = (char)*pS++) != 0) ;
            }

            pDst[-1] = (char)';';
            return pDst;
        }

        // Write "<![CDATA[" to the specified buffer.  Return an updated pointer.

        protected static unsafe char* RawStartCData(char* pDst)
        {
            pDst[0] = (char)'<';
            pDst[1] = (char)'!';
            pDst[2] = (char)'[';
            pDst[3] = (char)'C';
            pDst[4] = (char)'D';
            pDst[5] = (char)'A';
            pDst[6] = (char)'T';
            pDst[7] = (char)'A';
            pDst[8] = (char)'[';
            return pDst + 9;
        }

        // Write "]]>" to the specified buffer.  Return an updated pointer.

        protected static unsafe char* RawEndCData(char* pDst)
        {
            pDst[0] = (char)']';
            pDst[1] = (char)']';
            pDst[2] = (char)'>';
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
    internal partial class XmlEncodedRawTextWriterIndent : XmlEncodedRawTextWriter
    {
        //
        // Fields
        //
        protected int _indentLevel;
        protected bool _newLineOnAttributes;
        protected string _indentChars;

        protected bool _mixedContent;
        private BitStack _mixedContentStack;

        protected ConformanceLevel _conformanceLevel = ConformanceLevel.Auto;

        //
        // Constructors
        //
        public XmlEncodedRawTextWriterIndent(TextWriter writer, XmlWriterSettings settings) : base(writer, settings)
        {
            Init(settings);
        }

        public XmlEncodedRawTextWriterIndent(Stream stream, XmlWriterSettings settings) : base(stream, settings)
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

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            // Add indentation
            if (!_mixedContent && base._textPos != base._bufPos)
            {
                WriteIndent();
            }
            base.WriteDocType(name, pubid, sysid, subset);
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            Debug.Assert(localName != null && localName.Length != 0 && prefix != null && ns != null);

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
        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            // Add indentation
            if (_newLineOnAttributes)
            {
                WriteIndent();
            }

            base.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteCData(string text)
        {
            _mixedContent = true;
            base.WriteCData(text);
        }

        public override void WriteComment(string text)
        {
            if (!_mixedContent && base._textPos != base._bufPos)
            {
                WriteIndent();
            }

            base.WriteComment(text);
        }

        public override void WriteProcessingInstruction(string target, string text)
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

        public override void WriteWhitespace(string ws)
        {
            _mixedContent = true;
            base.WriteWhitespace(ws);
        }

        public override void WriteString(string text)
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
