// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// WARNING: This file is generated and should not be modified directly.
// Instead, modify HtmlRawTextWriterGenerator.ttinclude

using System.IO;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    internal class HtmlEncodedRawTextWriter : XmlEncodedRawTextWriter
    {
        protected ByteStack _elementScope;
        protected ElementProperties _currentElementProperties;
        private AttributeProperties _currentAttributeProperties;

        private bool _endsWithAmpersand;
        private byte[] _uriEscapingBuffer;

        private string? _mediaType;
        private bool _doNotEscapeUriAttributes;

        private const int StackIncrement = 10;

        public HtmlEncodedRawTextWriter(TextWriter writer, XmlWriterSettings settings) : base(writer, settings)
        {
            Init(settings);
        }

        public HtmlEncodedRawTextWriter(Stream stream, XmlWriterSettings settings) : base(stream, settings)
        {
            Init(settings);
        }

        internal override void WriteXmlDeclaration(XmlStandalone standalone)
        {
            // Ignore xml declaration
        }

        internal override void WriteXmlDeclaration(string xmldecl)
        {
            // Ignore xml declaration
        }

        /// Html rules allow public ID without system ID and always output "html"
        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            Debug.Assert(name != null && name.Length > 0);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            RawText("<!DOCTYPE ");

            // Bug: Always output "html" or "HTML" in doc-type, even if "name" is something else
            if (name == "HTML")
                RawText("HTML");
            else
                RawText("html");

            if (pubid != null)
            {
                RawText(" PUBLIC \"");
                RawText(pubid);
                if (sysid != null)
                {
                    RawText("\" \"");
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

        // For the HTML element, it should call this method with ns and prefix as String.Empty
        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            Debug.Assert(localName != null && localName.Length != 0 && prefix != null && ns != null);

            _elementScope.Push((byte)_currentElementProperties);

            if (ns.Length == 0)
            {
                Debug.Assert(prefix.Length == 0);

                if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

                _currentElementProperties = TernaryTreeReadOnly.FindElementProperty(localName);
                base._bufChars[_bufPos++] = (char)'<';
                base.RawText(localName);
                base._attrEndPos = _bufPos;
            }
            else
            {
                // Since the HAS_NS has no impact to the ElementTextBlock behavior,
                // we don't need to push it into the stack.
                _currentElementProperties = ElementProperties.HAS_NS;
                base.WriteStartElement(prefix, localName, ns);
            }
        }

        // Output >. For HTML needs to output META info
        internal override void StartElementContent()
        {
            base._bufChars[base._bufPos++] = (char)'>';

            // Detect whether content is output
            _contentPos = _bufPos;

            if ((_currentElementProperties & ElementProperties.HEAD) != 0)
            {
                WriteMetaElement();
            }
        }

        // end element with />
        // for HTML(ns.Length == 0)
        //    not an empty tag <h1></h1>
        //    empty tag <basefont>
        internal override void WriteEndElement(string prefix, string localName, string ns)
        {
            if (ns.Length == 0)
            {
                Debug.Assert(prefix.Length == 0);

                if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

                if ((_currentElementProperties & ElementProperties.EMPTY) == 0)
                {
                    _bufChars[base._bufPos++] = (char)'<';
                    _bufChars[base._bufPos++] = (char)'/';
                    base.RawText(localName);
                    _bufChars[base._bufPos++] = (char)'>';
                }
            }
            else
            {
                //xml content
                base.WriteEndElement(prefix, localName, ns);
            }

            _currentElementProperties = (ElementProperties)_elementScope.Pop();
        }

        internal override void WriteFullEndElement(string prefix, string localName, string ns)
        {
            if (ns.Length == 0)
            {
                Debug.Assert(prefix.Length == 0);

                if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

                if ((_currentElementProperties & ElementProperties.EMPTY) == 0)
                {
                    _bufChars[base._bufPos++] = (char)'<';
                    _bufChars[base._bufPos++] = (char)'/';
                    base.RawText(localName);
                    _bufChars[base._bufPos++] = (char)'>';
                }
            }
            else
            {
                //xml content
                base.WriteFullEndElement(prefix, localName, ns);
            }

            _currentElementProperties = (ElementProperties)_elementScope.Pop();
        }

        // 1. How the outputBooleanAttribute(fBOOL) and outputHtmlUriText(fURI) being set?
        // When SA is called.
        //
        //             BOOL_PARENT   URI_PARENT   Others
        //  fURI
        //  URI att       false         true       false
        //
        //  fBOOL
        //  BOOL att      true          false      false
        //
        //  How they change the attribute output behaviors?
        //
        //  1)       fURI=true             fURI=false
        //  SA         a="                      a="
        //  AT       HtmlURIText             HtmlText
        //  EA          "                       "
        //
        //  2)      fBOOL=true             fBOOL=false
        //  SA         a                       a="
        //  AT      HtmlText                output nothing
        //  EA     output nothing               "
        //
        // When they get reset?
        //  At the end of attribute.

        // 2. How the outputXmlTextElementScoped(fENs) and outputXmlTextattributeScoped(fANs) are set?
        //  fANs is in the scope of the fENs.
        //
        //          SE(localName)    SE(ns, pre, localName)  SA(localName)  SA(ns, pre, localName)
        //  fENs      false(default)      true(action)
        //  fANs      false(default)     false(default)      false(default)      true(action)

        // how they get reset?
        //
        //          EE(localName)  EE(ns, pre, localName) EENC(ns, pre, localName) EA(localName)  EA(ns, pre, localName)
        //  fENs                      false(action)
        //  fANs                                                                                        false(action)

        // How they change the TextOutput?
        //
        //         fENs | fANs              Else
        //  AT      XmlText                  HtmlText
        //
        //
        // 3. Flags for processing &{ split situations
        //
        // When the flag is set?
        //
        //  AT     src[lastchar]='&' flag&{ = true;
        //
        // when it get result?
        //
        //  AT method.
        //
        // How it changes the behaviors?
        //
        //         flag&{=true
        //
        //  AT     if (src[0] == '{') {
        //             output "&{"
        //         }
        //         else {
        //             output &amp;
        //         }
        //
        //  EA     output amp;
        //

        //  SA  if (flagBOOL == false) { output =";}
        //
        //  AT  if (flagBOOL) { return};
        //      if (flagNS) {XmlText;} {
        //      }
        //      else if (flagURI) {
        //          HtmlURIText;
        //      }
        //      else {
        //          HtmlText;
        //      }
        //

        //  AT  if (flagNS) {XmlText;} {
        //      }
        //      else if (flagURI) {
        //          HtmlURIText;
        //      }
        //      else if (!flagBOOL) {
        //          HtmlText; //flag&{ handling
        //      }
        //
        //
        //  EA if (flagBOOL == false) { output "
        //     }
        //     else if (flag&{) {
        //          output amp;
        //     }
        //
        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            Debug.Assert(localName != null && localName.Length != 0 && prefix != null && ns != null);

            if (ns.Length == 0)
            {
                Debug.Assert(prefix.Length == 0);

                if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

                if (base._attrEndPos == _bufPos)
                {
                    base._bufChars[_bufPos++] = (char)' ';
                }
                base.RawText(localName);

                if ((_currentElementProperties & (ElementProperties.BOOL_PARENT | ElementProperties.URI_PARENT | ElementProperties.NAME_PARENT)) != 0)
                {
                    _currentAttributeProperties = TernaryTreeReadOnly.FindAttributeProperty(localName) &
                                                 (AttributeProperties)_currentElementProperties;

                    if ((_currentAttributeProperties & AttributeProperties.BOOLEAN) != 0)
                    {
                        base._inAttributeValue = true;
                        return;
                    }
                }
                else
                {
                    _currentAttributeProperties = AttributeProperties.DEFAULT;
                }

                base._bufChars[_bufPos++] = (char)'=';
                base._bufChars[_bufPos++] = (char)'"';
            }
            else
            {
                base.WriteStartAttribute(prefix, localName, ns);
                _currentAttributeProperties = AttributeProperties.DEFAULT;
            }

            base._inAttributeValue = true;
        }

        // Output the amp; at end of EndAttribute
        public override void WriteEndAttribute()
        {
            if ((_currentAttributeProperties & AttributeProperties.BOOLEAN) != 0)
            {
                base._attrEndPos = _bufPos;
            }
            else
            {
                if (_endsWithAmpersand)
                {
                    OutputRestAmps();
                    _endsWithAmpersand = false;
                }

                if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

                base._bufChars[_bufPos++] = (char)'"';
            }
            base._inAttributeValue = false;
            base._attrEndPos = _bufPos;
        }

        // HTML PI's use ">" to terminate rather than "?>".
        public override void WriteProcessingInstruction(string target, string? text)
        {
            Debug.Assert(target != null && target.Length != 0 && text != null);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            _bufChars[base._bufPos++] = (char)'<';
            _bufChars[base._bufPos++] = (char)'?';
            base.RawText(target);
            _bufChars[base._bufPos++] = (char)' ';

            base.WriteCommentOrPi(text, '?');

            base._bufChars[base._bufPos++] = (char)'>';

            if (base._bufPos > base._bufLen)
            {
                FlushBuffer();
            }
        }

        // Serialize either attribute or element text using HTML rules.
        public override unsafe void WriteString(string? text)
        {
            Debug.Assert(text != null);

            if (_trackTextContent && _inTextContent != true) { ChangeTextContentMark(true); }

            fixed (char* pSrc = text)
            {
                char* pSrcEnd = pSrc + text.Length;
                if (base._inAttributeValue)
                {
                    WriteHtmlAttributeTextBlock(pSrc, pSrcEnd);
                }
                else
                {
                    WriteHtmlElementTextBlock(pSrc, pSrcEnd);
                }
            }
        }

        public override void WriteEntityRef(string name)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override void WriteCharEntity(char ch)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

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

        //
        // Private methods
        //

        [MemberNotNull(nameof(_elementScope))]
        [MemberNotNull(nameof(_uriEscapingBuffer))]
        private void Init(XmlWriterSettings settings)
        {
            Debug.Assert((int)ElementProperties.URI_PARENT == (int)AttributeProperties.URI);
            Debug.Assert((int)ElementProperties.BOOL_PARENT == (int)AttributeProperties.BOOLEAN);
            Debug.Assert((int)ElementProperties.NAME_PARENT == (int)AttributeProperties.NAME);

            _elementScope = new ByteStack(StackIncrement);
            _uriEscapingBuffer = new byte[5];
            _currentElementProperties = ElementProperties.DEFAULT;

            _mediaType = settings.MediaType;
            _doNotEscapeUriAttributes = settings.DoNotEscapeUriAttributes;
        }

        protected void WriteMetaElement()
        {
            base.RawText("<META http-equiv=\"Content-Type\"");

            _mediaType ??= "text/html";

            base.RawText(" content=\"");
            base.RawText(_mediaType);
            base.RawText("; charset=");
            base.RawText(base._encoding!.WebName);
            base.RawText("\">");
        }

        // Justify the stack usage:
        //
        // Nested elements has following possible position combinations
        // 1. <E1>Content1<E2>Content2</E2></E1>
        // 2. <E1><E2>Content2</E2>Content1</E1>
        // 3. <E1>Content<E2>Cotent2</E2>Content1</E1>
        //
        // In the situation 2 and 3, the stored currentElementProrperties will be E2's,
        // only the top of the stack is the real E1 element properties.
        protected unsafe void WriteHtmlElementTextBlock(char* pSrc, char* pSrcEnd)
        {
            if ((_currentElementProperties & ElementProperties.NO_ENTITIES) != 0)
            {
                base.RawText(pSrc, pSrcEnd);
            }
            else
            {
                base.WriteElementTextBlock(pSrc, pSrcEnd);
            }
        }

        protected unsafe void WriteHtmlAttributeTextBlock(char* pSrc, char* pSrcEnd)
        {
            if ((_currentAttributeProperties & (AttributeProperties.BOOLEAN | AttributeProperties.URI | AttributeProperties.NAME)) != 0)
            {
                if ((_currentAttributeProperties & AttributeProperties.BOOLEAN) != 0)
                {
                    //if output boolean attribute, ignore this call.
                    return;
                }

                if ((_currentAttributeProperties & (AttributeProperties.URI | AttributeProperties.NAME)) != 0 && !_doNotEscapeUriAttributes)
                {
                    WriteUriAttributeText(pSrc, pSrcEnd);
                }
                else
                {
                    WriteHtmlAttributeText(pSrc, pSrcEnd);
                }
            }
            else if ((_currentElementProperties & ElementProperties.HAS_NS) != 0)
            {
                base.WriteAttributeTextBlock(pSrc, pSrcEnd);
            }
            else
            {
                WriteHtmlAttributeText(pSrc, pSrcEnd);
            }
        }

        //
        // &{ split cases
        // 1). HtmlAttributeText("a&");
        //     HtmlAttributeText("{b}");
        //
        // 2). HtmlAttributeText("a&");
        //     EndAttribute();

        // 3).split with Flush by the user
        //     HtmlAttributeText("a&");
        //     FlushBuffer();
        //     HtmlAttributeText("{b}");

        //
        // Solutions:
        // case 1)hold the &amp; output as &
        //      if the next income character is {, output {
        //      else output amp;
        //

        private unsafe void WriteHtmlAttributeText(char* pSrc, char* pSrcEnd)
        {
            if (_endsWithAmpersand)
            {
                if (pSrcEnd - pSrc > 0 && pSrc[0] != '{')
                {
                    OutputRestAmps();
                }
                _endsWithAmpersand = false;
            }

            fixed (char* pDstBegin = _bufChars)
            {
                char* pDst = pDstBegin + _bufPos;

                char ch = (char)0;
                while (true)
                {
                    char* pDstEnd = pDst + (pSrcEnd - pSrc);
                    if (pDstEnd > pDstBegin + _bufLen)
                    {
                        pDstEnd = pDstBegin + _bufLen;
                    }

                    while (pDst < pDstEnd && XmlCharType.IsAttributeValueChar((char)(ch = *pSrc)))
                    {
                        *pDst++ = (char)ch;
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
                            if (pSrc + 1 == pSrcEnd)
                            {
                                _endsWithAmpersand = true;
                            }
                            else if (pSrc[1] != '{')
                            {
                                pDst = AmpEntity(pDst);
                                break;
                            }
                            *pDst++ = (char)ch;
                            break;
                        case '"':
                            pDst = QuoteEntity(pDst);
                            break;
                        case '<':
                        case '>':
                        case '\'':
                        case (char)0x9:
                            *pDst++ = (char)ch;
                            break;
                        case (char)0xD:
                            // do not normalize new lines in attributes - just escape them
                            pDst = CarriageReturnEntity(pDst);
                            break;
                        case (char)0xA:
                            // do not normalize new lines in attributes - just escape them
                            pDst = LineFeedEntity(pDst);
                            break;
                        default:
                            EncodeChar(ref pSrc, pSrcEnd, ref pDst);
                            continue;
                    }
                    pSrc++;
                }
                _bufPos = (int)(pDst - pDstBegin);
            }
        }

        private unsafe void WriteUriAttributeText(char* pSrc, char* pSrcEnd)
        {
            if (_endsWithAmpersand)
            {
                if (pSrcEnd - pSrc > 0 && pSrc[0] != '{')
                {
                    OutputRestAmps();
                }
                _endsWithAmpersand = false;
            }

            fixed (char* pDstBegin = _bufChars)
            {
                char* pDst = pDstBegin + _bufPos;

                char ch = (char)0;
                while (true)
                {
                    char* pDstEnd = pDst + (pSrcEnd - pSrc);
                    if (pDstEnd > pDstBegin + _bufLen)
                    {
                        pDstEnd = pDstBegin + _bufLen;
                    }

                    while (pDst < pDstEnd && XmlCharType.IsAttributeValueChar((char)(ch = *pSrc)) && ch < 0x80)
                    {
                        *pDst++ = (char)ch;
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
                            if (pSrc + 1 == pSrcEnd)
                            {
                                _endsWithAmpersand = true;
                            }
                            else if (pSrc[1] != '{')
                            {
                                pDst = AmpEntity(pDst);
                                break;
                            }
                            *pDst++ = (char)ch;
                            break;
                        case '"':
                            pDst = QuoteEntity(pDst);
                            break;
                        case '<':
                        case '>':
                        case '\'':
                        case (char)0x9:
                            *pDst++ = (char)ch;
                            break;
                        case (char)0xD:
                            // do not normalize new lines in attributes - just escape them
                            pDst = CarriageReturnEntity(pDst);
                            break;
                        case (char)0xA:
                            // do not normalize new lines in attributes - just escape them
                            pDst = LineFeedEntity(pDst);
                            break;
                        default:
                            Debug.Assert(_uriEscapingBuffer?.Length > 0);
                            fixed (byte* pUriEscapingBuffer = _uriEscapingBuffer)
                            {
                                byte* pByte = pUriEscapingBuffer;
                                byte* pEnd = pByte;

                                XmlUtf8RawTextWriter.CharToUTF8(ref pSrc, pSrcEnd, ref pEnd);

                                while (pByte < pEnd)
                                {
                                    *pDst++ = (char)'%';
                                    *pDst++ = (char)HexConverter.ToCharUpper(*pByte >> 4);
                                    *pDst++ = (char)HexConverter.ToCharUpper(*pByte);
                                    pByte++;
                                }
                            }
                            continue;
                    }
                    pSrc++;
                }
                _bufPos = (int)(pDst - pDstBegin);
            }
        }

        // For handling &{ in Html text field. If & is not followed by {, it still needs to be escaped.
        private void OutputRestAmps()
        {
            base._bufChars[_bufPos++] = (char)'a';
            base._bufChars[_bufPos++] = (char)'m';
            base._bufChars[_bufPos++] = (char)'p';
            base._bufChars[_bufPos++] = (char)';';
        }
    }

    //
    // Indentation HtmlWriter only indent <BLOCK><BLOCK> situations
    //
    // Here are all the cases:
    //       ELEMENT1     actions          ELEMENT2          actions                                 SC              EE
    // 1).    SE SC   store SE blockPro       SE           a). check ELEMENT1 blockPro                  <A>           </A>
    //        EE     if SE, EE are blocks                  b). true: check ELEMENT2 blockPro                <B>            <B>
    //                                                     c). detect ELEMENT is SE, SC
    //                                                     d). increase the indexlevel
    //
    // 2).    SE SC,  Store EE blockPro       EE            a). check stored blockPro                    <A></A>            </A>
    //         EE    if SE, EE are blocks                  b). true:  indexLevel same                                  </B>
    //


    //
    // This is an alternative way to make the output looks better
    //
    // Indentation HtmlWriter only indent <BLOCK><BLOCK> situations
    //
    // Here are all the cases:
    //       ELEMENT1     actions           ELEMENT2          actions                                 Samples
    // 1).    SE SC   store SE blockPro       SE            a). check ELEMENT1 blockPro                  <A>(blockPos)
    //                                                     b). true: check ELEMENT2 blockPro                <B>
    //                                                     c). detect ELEMENT is SE, SC
    //                                                     d). increase the indentLevel
    //
    // 2).     EE     Store EE blockPro       SE            a). check stored blockPro                    </A>
    //                                                     b). true:  indentLevel same                   <B>
    //                                                     c). output block2
    //
    // 3).     EE      same as above          EE            a). check stored blockPro                          </A>
    //                                                     b). true:  --indentLevel                        </B>
    //                                                     c). output block2
    //
    // 4).    SE SC    same as above          EE            a). check stored blockPro                      <A></A>
    //                                                     b). true:  indentLevel no change
    internal sealed class HtmlEncodedRawTextWriterIndent : HtmlEncodedRawTextWriter
    {
        //
        // Fields
        //
        private int _indentLevel;

        // for detecting SE SC sitution
        private int _endBlockPos;

        // settings
        private string _indentChars;
        private bool _newLineOnAttributes;

        //
        // Constructors
        //
        public HtmlEncodedRawTextWriterIndent(TextWriter writer, XmlWriterSettings settings) : base(writer, settings)
        {
            Init(settings);
        }

        public HtmlEncodedRawTextWriterIndent(Stream stream, XmlWriterSettings settings) : base(stream, settings)
        {
            Init(settings);
        }

        //
        // XmlRawWriter overrides
        //
        /// <summary>
        /// Serialize the document type declaration.
        /// </summary>
        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            base.WriteDocType(name, pubid, sysid, subset);

            // Allow indentation after DocTypeDecl
            _endBlockPos = base._bufPos;
        }

        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            Debug.Assert(localName != null && localName.Length != 0 && prefix != null && ns != null);

            if (_trackTextContent && _inTextContent) { ChangeTextContentMark(false); }

            base._elementScope.Push((byte)base._currentElementProperties);

            if (ns.Length == 0)
            {
                Debug.Assert(prefix.Length == 0);

                base._currentElementProperties = TernaryTreeReadOnly.FindElementProperty(localName);

                if (_endBlockPos == base._bufPos && (base._currentElementProperties & ElementProperties.BLOCK_WS) != 0)
                {
                    WriteIndent();
                }
                _indentLevel++;

                base._bufChars[_bufPos++] = (char)'<';
            }
            else
            {
                base._currentElementProperties = ElementProperties.HAS_NS | ElementProperties.BLOCK_WS;

                if (_endBlockPos == base._bufPos)
                {
                    WriteIndent();
                }
                _indentLevel++;

                base._bufChars[base._bufPos++] = (char)'<';
                if (prefix.Length != 0)
                {
                    base.RawText(prefix);
                    base._bufChars[base._bufPos++] = (char)':';
                }
            }
            base.RawText(localName);
            base._attrEndPos = _bufPos;
        }

        internal override void StartElementContent()
        {
            base._bufChars[base._bufPos++] = (char)'>';

            // Detect whether content is output
            base._contentPos = base._bufPos;

            if ((_currentElementProperties & ElementProperties.HEAD) != 0)
            {
                WriteIndent();
                WriteMetaElement();
                _endBlockPos = base._bufPos;
            }
            else if ((base._currentElementProperties & ElementProperties.BLOCK_WS) != 0)
            {
                // store the element block position
                _endBlockPos = base._bufPos;
            }
        }

        internal override void WriteEndElement(string? prefix, string localName, string? ns)
        {
            bool isBlockWs;
            Debug.Assert(localName != null && localName.Length != 0 && prefix != null && ns != null);

            _indentLevel--;

            // If this element has block whitespace properties,
            isBlockWs = (base._currentElementProperties & ElementProperties.BLOCK_WS) != 0;
            if (isBlockWs)
            {
                // And if the last node to be output had block whitespace properties,
                // And if content was output within this element,
                if (_endBlockPos == base._bufPos && base._contentPos != base._bufPos)
                {
                    // Then indent
                    WriteIndent();
                }
            }

            base.WriteEndElement(prefix, localName, ns);

            // Reset contentPos in case of empty elements
            base._contentPos = 0;

            // Mark end of element in buffer for element's with block whitespace properties
            if (isBlockWs)
            {
                _endBlockPos = base._bufPos;
            }
        }

        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            if (_newLineOnAttributes)
            {
                RawText(base._newLineChars);
                _indentLevel++;
                WriteIndent();
                _indentLevel--;
            }
            base.WriteStartAttribute(prefix, localName, ns);
        }

        protected override void FlushBuffer()
        {
            // Make sure the buffer will reset the block position
            _endBlockPos = (_endBlockPos == base._bufPos) ? 1 : 0;
            base.FlushBuffer();
        }

        //
        // Private methods
        //
        [MemberNotNull(nameof(_indentChars))]
        private void Init(XmlWriterSettings settings)
        {
            _indentLevel = 0;
            _indentChars = settings.IndentChars;
            _newLineOnAttributes = settings.NewLineOnAttributes;
        }

        private void WriteIndent()
        {
            // <block><inline>  -- suppress ws betw <block> and <inline>
            // <block><block>   -- don't suppress ws betw <block> and <block>
            // <block>text      -- suppress ws betw <block> and text (handled by wcharText method)
            // <block><?PI?>    -- suppress ws betw <block> and PI
            // <block><!-- -->  -- suppress ws betw <block> and comment

            // <inline><block>  -- suppress ws betw <inline> and <block>
            // <inline><inline> -- suppress ws betw <inline> and <inline>
            // <inline>text     -- suppress ws betw <inline> and text (handled by wcharText method)
            // <inline><?PI?>   -- suppress ws betw <inline> and PI
            // <inline><!-- --> -- suppress ws betw <inline> and comment

            RawText(base._newLineChars);
            for (int i = _indentLevel; i > 0; i--)
            {
                RawText(_indentChars);
            }
        }
    }
}
