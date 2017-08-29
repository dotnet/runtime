//------------------------------------------------------------------------------
// <copyright file="HtmlRawTextWriterGenerator.cxx" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">helenak</owner>
//------------------------------------------------------------------------------

// WARNING: This file is generated and should not be modified directly.  Instead,
// modify XmlTextWriterGenerator.cxx and run gen.bat in the same directory.
// This batch file will execute the following commands:
//
//   cl.exe /C /EP /D _XML_UTF8_TEXT_WRITER HtmlTextWriterGenerator.cxx > HtmlUtf8TextWriter.cs
//   cl.exe /C /EP /D _XML_ENCODED_TEXT_WRITER HtmlTextWriterGenerator.cxx > HtmlEncodedTextWriter.cs
//
// Because these two implementations of XmlTextWriter are so similar, the C++ preprocessor
// is used to generate each implementation from one template file, using macros and ifdefs.

#ifdef _HTML_UTF8_TEXT_WRITER
#define _CLASS_NAME HtmlUtf8RawTextWriter
#define _BASE_CLASS_NAME XmlUtf8RawTextWriter
#define _CLASS_NAME_INDENT HtmlUtf8RawTextWriterIndent
#define _BUFFER bufBytes
#define _BUFFER_TYPE byte
#define _SET_TEXT_CONTENT_MARK(value) 
#endif

#ifdef _HTML_ENCODED_TEXT_WRITER
#define _CLASS_NAME HtmlEncodedRawTextWriter
#define _BASE_CLASS_NAME XmlEncodedRawTextWriter
#define _CLASS_NAME_INDENT HtmlEncodedRawTextWriterIndent
#define _BUFFER bufChars
#define _BUFFER_TYPE char
#define _SET_TEXT_CONTENT_MARK(value) \
    if ( trackTextContent && inTextContent != value ) { \
        ChangeTextContentMark( value );  \
    }
#endif

#define XMLCHARTYPE_TEST(ch, flag)              ( ( xmlCharType.charProperties[ch] & XmlCharType.##flag ) != 0 )
#define XMLCHARTYPE_ISATTRIBUTEVALUECHAR(ch)    XMLCHARTYPE_TEST(ch, fAttrValue)

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
//using System.Xml.Query;
using System.Diagnostics;
using MS.Internal.Xml;

namespace System.Xml {

    internal class _CLASS_NAME : _BASE_CLASS_NAME {
//
// Fields
//
        protected ByteStack         elementScope;
        protected ElementProperties currentElementProperties;
        private AttributeProperties currentAttributeProperties;

        private bool    endsWithAmpersand;
        private byte [] uriEscapingBuffer;

        // settings
        private string  mediaType;
        private bool doNotEscapeUriAttributes;

//
// Static fields
//
        protected static TernaryTreeReadOnly elementPropertySearch;
        protected static TernaryTreeReadOnly attributePropertySearch;

//
// Constants
//
        private const int StackIncrement = 10;

//
// Constructors
//

#ifdef _HTML_ENCODED_TEXT_WRITER

        public _CLASS_NAME( TextWriter writer, XmlWriterSettings settings ) : base( writer, settings ) {
            Init( settings );
        }
#endif

        public _CLASS_NAME( Stream stream, XmlWriterSettings settings ) : base( stream, settings ) {
            Init( settings );
        }

//
// XmlRawWriter implementation
//
        internal override void WriteXmlDeclaration( XmlStandalone standalone ) {
            // Ignore xml declaration
        }

        internal override void WriteXmlDeclaration( string xmldecl ) {
            // Ignore xml declaration
        }

        /// Html rules allow public ID without system ID and always output "html"
        public override void WriteDocType( string name, string pubid, string sysid, string subset ) {
            Debug.Assert( name != null && name.Length > 0 );

            _SET_TEXT_CONTENT_MARK(false)

            RawText( "<!DOCTYPE ");

            // Bug 114337: Always output "html" or "HTML" in doc-type, even if "name" is something else
            if ( name == "HTML" )
                RawText( "HTML" );
            else
                RawText( "html" );

            if ( pubid != null ) {
                RawText( " PUBLIC \"" );
                RawText( pubid );
                if ( sysid != null ) {
                    RawText( "\" \"" );
                    RawText( sysid );
                }
                _BUFFER[bufPos++] = (_BUFFER_TYPE) '"';
            }
            else if ( sysid != null ) {
                RawText( " SYSTEM \"" );
                RawText( sysid );
                _BUFFER[bufPos++] = (_BUFFER_TYPE) '"';
            }
            else {
                _BUFFER[bufPos++] = (_BUFFER_TYPE) ' ';
            }

            if ( subset != null ) {
                _BUFFER[bufPos++] = (_BUFFER_TYPE) '[';
                RawText( subset );
                _BUFFER[bufPos++] = (_BUFFER_TYPE) ']';
            }

            _BUFFER[this.bufPos++] = (_BUFFER_TYPE) '>';
        }

        // For the HTML element, it should call this method with ns and prefix as String.Empty
        public override void WriteStartElement( string prefix, string localName, string ns ) {
            Debug.Assert( localName != null && localName.Length != 0 && prefix != null && ns != null );

            elementScope.Push( (byte)currentElementProperties );

            if ( ns.Length == 0 ) {
                Debug.Assert( prefix.Length == 0 );

                _SET_TEXT_CONTENT_MARK(false)
                currentElementProperties = (ElementProperties)elementPropertySearch.FindCaseInsensitiveString( localName );
                base._BUFFER[bufPos++] = (_BUFFER_TYPE) '<';
                base.RawText( localName );
                base.attrEndPos = bufPos;
            }
            else {
                // Since the HAS_NS has no impact to the ElementTextBlock behavior,
                // we don't need to push it into the stack.
                currentElementProperties = ElementProperties.HAS_NS;
                base.WriteStartElement( prefix, localName, ns );
            }
        }

        // Output >. For HTML needs to output META info
        internal override void StartElementContent() {
            base._BUFFER[base.bufPos++] = (_BUFFER_TYPE) '>';

            // Detect whether content is output
            this.contentPos = this.bufPos;

            if ( ( currentElementProperties & ElementProperties.HEAD ) != 0 ) {
                WriteMetaElement();
            }
        }

        // end element with />
        // for HTML(ns.Length == 0)
        //    not an empty tag <h1></h1>
        //    empty tag <basefont>
        internal override void WriteEndElement( string prefix, string localName, string ns ) {
            if ( ns.Length == 0 ) {
                Debug.Assert( prefix.Length == 0 );

                _SET_TEXT_CONTENT_MARK(false)

                if ( ( currentElementProperties & ElementProperties.EMPTY ) == 0 ) {
                    _BUFFER[base.bufPos++] = (_BUFFER_TYPE) '<'; 
                    _BUFFER[base.bufPos++] = (_BUFFER_TYPE) '/'; 
                    base.RawText( localName ); 
                    _BUFFER[base.bufPos++] = (_BUFFER_TYPE) '>';
                }
            }
            else {
                //xml content
                base.WriteEndElement( prefix, localName, ns );
            }

            currentElementProperties = (ElementProperties)elementScope.Pop();
       }

        internal override void WriteFullEndElement( string prefix, string localName, string ns ) {
            if ( ns.Length == 0 ) {
                Debug.Assert( prefix.Length == 0 );

                _SET_TEXT_CONTENT_MARK(false)

                if ( ( currentElementProperties & ElementProperties.EMPTY ) == 0 ) {
                    _BUFFER[base.bufPos++] = (_BUFFER_TYPE) '<'; 
                    _BUFFER[base.bufPos++] = (_BUFFER_TYPE) '/'; 
                    base.RawText( localName ); 
                    _BUFFER[base.bufPos++] = (_BUFFER_TYPE) '>';
                }
            }
            else {
                //xml content
                base.WriteFullEndElement( prefix, localName, ns );
            }

            currentElementProperties = (ElementProperties)elementScope.Pop();
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
        public override void WriteStartAttribute( string prefix, string localName, string ns ) {
            Debug.Assert( localName != null && localName.Length != 0 && prefix != null  && ns != null );

            if ( ns.Length == 0 ) {
                Debug.Assert( prefix.Length == 0 );
                _SET_TEXT_CONTENT_MARK(false)

                if ( base.attrEndPos == bufPos ) {
                    base._BUFFER[bufPos++] = (_BUFFER_TYPE) ' ';
                }
                base.RawText( localName );

                if ( ( currentElementProperties & ( ElementProperties.BOOL_PARENT | ElementProperties.URI_PARENT | ElementProperties.NAME_PARENT ) ) != 0 ) {
                    currentAttributeProperties = (AttributeProperties)attributePropertySearch.FindCaseInsensitiveString( localName ) &
                                                 (AttributeProperties)currentElementProperties;

                    if ( ( currentAttributeProperties & AttributeProperties.BOOLEAN ) != 0 ) {
                        base.inAttributeValue = true;
                        return;
                    }
                }
                else {
                    currentAttributeProperties = AttributeProperties.DEFAULT;
                }

                base._BUFFER[bufPos++] = (_BUFFER_TYPE) '=';
                base._BUFFER[bufPos++] = (_BUFFER_TYPE) '"';
            }
            else {
                base.WriteStartAttribute( prefix, localName, ns );
                currentAttributeProperties = AttributeProperties.DEFAULT;
            }

            base.inAttributeValue = true;
        }

        // Output the amp; at end of EndAttribute
        public override void WriteEndAttribute() {

            if ( ( currentAttributeProperties & AttributeProperties.BOOLEAN ) != 0 ) {
                base.attrEndPos = bufPos;
            }
            else {
                if ( endsWithAmpersand ) {
                    OutputRestAmps();
                    endsWithAmpersand = false;
                }

                _SET_TEXT_CONTENT_MARK(false)

                base._BUFFER[bufPos++] = (_BUFFER_TYPE) '"';
            }
            base.inAttributeValue = false;
            base.attrEndPos = bufPos;
        }

        // HTML PI's use ">" to terminate rather than "?>".
        public override void WriteProcessingInstruction( string target, string text ) {
            Debug.Assert( target != null && target.Length != 0 && text != null );

            _SET_TEXT_CONTENT_MARK(false)

            _BUFFER[base.bufPos++] = (_BUFFER_TYPE) '<';
            _BUFFER[base.bufPos++] = (_BUFFER_TYPE) '?';
            base.RawText( target );
            _BUFFER[base.bufPos++] = (_BUFFER_TYPE) ' ';

            base.WriteCommentOrPi( text, '?' );

            base._BUFFER[base.bufPos++] = (_BUFFER_TYPE) '>';

            if ( base.bufPos > base.bufLen ) {
                FlushBuffer();
            }
        }

        // Serialize either attribute or element text using HTML rules.
        public override unsafe void WriteString( string text ) {
            Debug.Assert( text != null );

            _SET_TEXT_CONTENT_MARK(true)

            fixed ( char * pSrc = text ) {
                char * pSrcEnd = pSrc + text.Length;
                if ( base.inAttributeValue) {
                    WriteHtmlAttributeTextBlock( pSrc, pSrcEnd );
                }
                else {
                    WriteHtmlElementTextBlock( pSrc, pSrcEnd );
                }
            }
        }

        public override void WriteEntityRef( string name ) {
            throw new InvalidOperationException( string.Format( SR.Xml_InvalidOperation ) );
        }

        public override void WriteCharEntity( char ch ) {
            throw new InvalidOperationException( string.Format( SR.Xml_InvalidOperation ) );
        }

        public override void WriteSurrogateCharEntity( char lowChar, char highChar ) {
            throw new InvalidOperationException( string.Format( SR.Xml_InvalidOperation ) );
        }

        public override unsafe void WriteChars( char[] buffer, int index, int count ) {
            Debug.Assert( buffer != null );
            Debug.Assert( index >= 0 );
            Debug.Assert( count >= 0 && index + count <= buffer.Length );

            _SET_TEXT_CONTENT_MARK(true)

            fixed ( char * pSrcBegin = &buffer[index] ) {
                if ( inAttributeValue ) {
                    WriteAttributeTextBlock( pSrcBegin, pSrcBegin + count );
                }
                else {
                    WriteElementTextBlock( pSrcBegin, pSrcBegin + count );
                }
            }
        }

//
// Private methods
//

        private void Init( XmlWriterSettings settings ) {
            Debug.Assert( (int)ElementProperties.URI_PARENT == (int)AttributeProperties.URI );
            Debug.Assert( (int)ElementProperties.BOOL_PARENT == (int)AttributeProperties.BOOLEAN );
            Debug.Assert( (int)ElementProperties.NAME_PARENT == (int)AttributeProperties.NAME );

            if ( elementPropertySearch == null ) {
                //elementPropertySearch should be init last for the mutli thread safe situation.
                attributePropertySearch = new TernaryTreeReadOnly(HtmlTernaryTree.htmlAttributes );
                elementPropertySearch = new TernaryTreeReadOnly( HtmlTernaryTree.htmlElements );
            }

            elementScope = new ByteStack( StackIncrement );
            uriEscapingBuffer = new byte[5];
            currentElementProperties = ElementProperties.DEFAULT;

            mediaType = settings.MediaType;
            doNotEscapeUriAttributes = settings.DoNotEscapeUriAttributes;
        }

        protected void WriteMetaElement() {
            base.RawText("<META http-equiv=\"Content-Type\"");

            if ( mediaType == null ) {
                mediaType = "text/html";
            }

            base.RawText( " content=\"" );
            base.RawText( mediaType );
            base.RawText( "; charset=" );
            base.RawText( base.encoding.WebName );
            base.RawText( "\">" );
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
        protected unsafe void WriteHtmlElementTextBlock( char * pSrc, char * pSrcEnd ) {
            if ( ( currentElementProperties & ElementProperties.NO_ENTITIES ) != 0 ) {
                base.RawText( pSrc, pSrcEnd );
            } else {
                base.WriteElementTextBlock( pSrc, pSrcEnd );
            }

        }

        protected unsafe void WriteHtmlAttributeTextBlock( char * pSrc, char * pSrcEnd ) {
            if ( ( currentAttributeProperties & ( AttributeProperties.BOOLEAN | AttributeProperties.URI | AttributeProperties.NAME ) ) != 0 ) {
                if ( ( currentAttributeProperties & AttributeProperties.BOOLEAN ) != 0 ) {
                    //if output boolean attribute, ignore this call.
                    return;
                }

                if ( ( currentAttributeProperties & ( AttributeProperties.URI | AttributeProperties.NAME ) ) != 0 && !doNotEscapeUriAttributes ) {
                    WriteUriAttributeText( pSrc, pSrcEnd );
                }
                else {
                    WriteHtmlAttributeText( pSrc, pSrcEnd );
                }
            }
            else if ( ( currentElementProperties & ElementProperties.HAS_NS ) != 0 ) {
                base.WriteAttributeTextBlock( pSrc, pSrcEnd );
            }
            else {
                WriteHtmlAttributeText( pSrc, pSrcEnd );
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

        private unsafe void WriteHtmlAttributeText( char * pSrc, char *pSrcEnd ) {
            if ( endsWithAmpersand ) {
                if ( pSrcEnd - pSrc > 0 && pSrc[0] != '{' ) {
                    OutputRestAmps();
                }
                endsWithAmpersand = false;
            }

            fixed ( _BUFFER_TYPE * pDstBegin = _BUFFER ) {
                _BUFFER_TYPE * pDst = pDstBegin + this.bufPos;

                char ch = (char)0;
                for (;;) {
                    _BUFFER_TYPE * pDstEnd = pDst + ( pSrcEnd - pSrc );
                    if ( pDstEnd > pDstBegin + bufLen ) {
                        pDstEnd = pDstBegin + bufLen;
                    }

#if _HTML_UTF8_TEXT_WRITER
                    while ( pDst < pDstEnd && ( XMLCHARTYPE_ISATTRIBUTEVALUECHAR( ( ch = *pSrc ) ) && ch <= 0x7F ) ) {
#else
                    while ( pDst < pDstEnd && ( XMLCHARTYPE_ISATTRIBUTEVALUECHAR( ( ch = *pSrc ) ) ) ) {
#endif
                        *pDst++ = (_BUFFER_TYPE)ch;
                        pSrc++;
                    }
                    Debug.Assert( pSrc <= pSrcEnd );

                    // end of value
                    if ( pSrc >= pSrcEnd ) {
                        break;
                    }

                    // end of buffer
                    if ( pDst >= pDstEnd ) {
                        bufPos = (int)(pDst - pDstBegin);
                        FlushBuffer();
                        pDst = pDstBegin + 1;
                        continue;
                    }

                    // some character needs to be escaped
                    switch ( ch ) {
                        case '&':
                            if ( pSrc + 1 == pSrcEnd ) {
                                endsWithAmpersand = true;
                            }
                            else if ( pSrc[1] != '{' ) {
                                pDst = _BASE_CLASS_NAME.AmpEntity(pDst);
                                break;
                            }
                            *pDst++ = (_BUFFER_TYPE)ch;
                            break;
                        case '"':
                            pDst = QuoteEntity( pDst );
                            break;
                        case '<':
                        case '>':
                        case '\'':
                        case (char)0x9:
                            *pDst++ = (_BUFFER_TYPE)ch;
                            break;
                        case (char)0xD:
                            // do not normalize new lines in attributes - just escape them
                            pDst = CarriageReturnEntity( pDst );
                            break;
                        case (char)0xA:
                            // do not normalize new lines in attributes - just escape them
                            pDst = LineFeedEntity( pDst );
                            break;
                        default:
                            EncodeChar( ref pSrc, pSrcEnd, ref pDst);
                            continue;
                    }
                    pSrc++;
                }
                bufPos = (int)(pDst - pDstBegin);
            }
        }

        private unsafe void WriteUriAttributeText( char * pSrc, char * pSrcEnd ) {
            if ( endsWithAmpersand ) {
                if ( pSrcEnd - pSrc > 0 && pSrc[0] != '{' ) {
                    OutputRestAmps();
                }
                this.endsWithAmpersand = false;
            }

            fixed ( _BUFFER_TYPE * pDstBegin = _BUFFER ) {
                _BUFFER_TYPE * pDst = pDstBegin + this.bufPos;

                char ch = (char)0;
                for (;;) {
                    _BUFFER_TYPE * pDstEnd = pDst + ( pSrcEnd - pSrc );
                    if ( pDstEnd > pDstBegin + bufLen ) {
                        pDstEnd = pDstBegin + bufLen;
                    }

                    while ( pDst < pDstEnd && ( XMLCHARTYPE_ISATTRIBUTEVALUECHAR( ( ch = *pSrc ) ) && ch < 0x80 ) ) {
                        *pDst++ = (_BUFFER_TYPE)ch;
                        pSrc++;
                    }
                    Debug.Assert( pSrc <= pSrcEnd );

                    // end of value
                    if ( pSrc >= pSrcEnd ) {
                        break;
                    }

                    // end of buffer
                    if ( pDst >= pDstEnd ) {
                        bufPos = (int)(pDst - pDstBegin);
                        FlushBuffer();
                        pDst = pDstBegin + 1;
                        continue;
                    }

                    // some character needs to be escaped
                    switch ( ch ) {
                        case '&':
                            if ( pSrc + 1 == pSrcEnd ) {
                                endsWithAmpersand = true;
                            }
                            else if ( pSrc[1] != '{' ) {
                                pDst = _BASE_CLASS_NAME.AmpEntity(pDst);
                                break;
                            }
                            *pDst++ = (_BUFFER_TYPE)ch;
                            break;
                        case '"':
                            pDst = QuoteEntity( pDst );
                            break;
                        case '<':
                        case '>':
                        case '\'':
                        case (char)0x9:
                            *pDst++ = (_BUFFER_TYPE)ch;
                            break;
                        case (char)0xD:
                            // do not normalize new lines in attributes - just escape them
                            pDst = CarriageReturnEntity( pDst );
                            break;
                        case (char)0xA:
                            // do not normalize new lines in attributes - just escape them
                            pDst = LineFeedEntity( pDst );
                            break;
                        default:
                            const string hexDigits = "0123456789ABCDEF";
                            fixed ( byte * pUriEscapingBuffer = uriEscapingBuffer ) {
                                byte * pByte = pUriEscapingBuffer;
                                byte * pEnd = pByte;

                                XmlUtf8RawTextWriter.CharToUTF8( ref pSrc, pSrcEnd, ref pEnd );

                                while ( pByte < pEnd ) {
                                    *pDst++ = (_BUFFER_TYPE) '%'; 
                                    *pDst++ = (_BUFFER_TYPE) hexDigits[*pByte >> 4];
                                    *pDst++ = (_BUFFER_TYPE) hexDigits[*pByte & 0xF];
                                    pByte++;
                                }
                            }
                            continue;
                    }
                    pSrc++;
                }
                bufPos = (int)(pDst - pDstBegin);
            }
        }

        // For handling &{ in Html text field. If & is not followed by {, it still needs to be escaped.
        private void OutputRestAmps() {
            base._BUFFER[bufPos++] = (_BUFFER_TYPE)'a';
            base._BUFFER[bufPos++] = (_BUFFER_TYPE)'m';
            base._BUFFER[bufPos++] = (_BUFFER_TYPE)'p';
            base._BUFFER[bufPos++] = (_BUFFER_TYPE)';';
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
    internal class _CLASS_NAME_INDENT : _CLASS_NAME {
//
// Fields
//
        int indentLevel;

        // for detecting SE SC situation
        int endBlockPos;

        // settings
        string  indentChars;
        bool    newLineOnAttributes;

//
// Constructors
//
#ifdef _HTML_ENCODED_TEXT_WRITER

        public _CLASS_NAME_INDENT( TextWriter writer, XmlWriterSettings settings ) : base( writer, settings ) {
            Init( settings );
        }
#endif

        public _CLASS_NAME_INDENT( Stream stream, XmlWriterSettings settings ) : base( stream, settings ) {
            Init( settings );
        }

//
// XmlRawWriter overrides
//
        /// <summary>
        /// Serialize the document type declaration.
        /// </summary>
        public override void WriteDocType( string name, string pubid, string sysid, string subset ) {
            base.WriteDocType( name, pubid, sysid, subset );

            // Allow indentation after DocTypeDecl
            endBlockPos = base.bufPos;
        }

        public override void WriteStartElement(string prefix, string localName, string ns ) {
            Debug.Assert( localName != null && localName.Length != 0 && prefix != null && ns != null );

            _SET_TEXT_CONTENT_MARK(false)

            base.elementScope.Push( (byte)base.currentElementProperties );

            if ( ns.Length == 0 ) {
                Debug.Assert( prefix.Length == 0 );

                base.currentElementProperties = (ElementProperties)elementPropertySearch.FindCaseInsensitiveString( localName );

                if ( endBlockPos == base.bufPos && ( base.currentElementProperties & ElementProperties.BLOCK_WS ) != 0 ) {
                    WriteIndent();
                }
                indentLevel++;

                base._BUFFER[bufPos++] = (_BUFFER_TYPE) '<';
            }
            else {
                base.currentElementProperties = ElementProperties.HAS_NS | ElementProperties.BLOCK_WS;

                if ( endBlockPos == base.bufPos ) {
                    WriteIndent();
                }
                indentLevel++;

                base._BUFFER[base.bufPos++] = (_BUFFER_TYPE) '<';
                if ( prefix.Length != 0 ) {
                    base.RawText( prefix );
                    base._BUFFER[base.bufPos++] = (_BUFFER_TYPE) ':';
                }
            }
            base.RawText( localName );
            base.attrEndPos = bufPos;
        }

        internal override void StartElementContent() {
            base._BUFFER[base.bufPos++] = (_BUFFER_TYPE) '>';

            // Detect whether content is output
            base.contentPos = base.bufPos;

            if ( ( currentElementProperties & ElementProperties.HEAD ) != 0) {
                WriteIndent();
                WriteMetaElement();
                endBlockPos = base.bufPos;
            }
            else if ( ( base.currentElementProperties & ElementProperties.BLOCK_WS ) != 0 ) {
                // store the element block position
                endBlockPos = base.bufPos;
            }
        }

        internal override void WriteEndElement( string prefix, string localName, string ns ) {
            bool isBlockWs;
            Debug.Assert( localName != null && localName.Length != 0 && prefix != null && ns != null );

            indentLevel--;

            // If this element has block whitespace properties,
            isBlockWs = ( base.currentElementProperties & ElementProperties.BLOCK_WS ) != 0;
            if ( isBlockWs ) {
                // And if the last node to be output had block whitespace properties,
                // And if content was output within this element,
                if ( endBlockPos == base.bufPos && base.contentPos != base.bufPos ) {
                    // Then indent
                    WriteIndent();
                }
            }

            base.WriteEndElement(prefix, localName, ns);

            // Reset contentPos in case of empty elements
            base.contentPos = 0;

            // Mark end of element in buffer for element's with block whitespace properties
            if ( isBlockWs ) {
                endBlockPos = base.bufPos;
            }
        }

        public override void WriteStartAttribute( string prefix, string localName, string ns ) {
            if ( newLineOnAttributes ) {
                RawText( base.newLineChars );
                indentLevel++;
                WriteIndent();
                indentLevel--;
            }
            base.WriteStartAttribute( prefix, localName, ns );
        }

        protected override void FlushBuffer() {
            // Make sure the buffer will reset the block position
            endBlockPos = ( endBlockPos == base.bufPos ) ? 1 : 0;
            base.FlushBuffer();
        }

//
// Private methods
//
        private void Init( XmlWriterSettings settings ) {
            indentLevel = 0;
            indentChars = settings.IndentChars;
            newLineOnAttributes = settings.NewLineOnAttributes;
        }

        private void WriteIndent() {
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

            RawText( base.newLineChars );
            for ( int i = indentLevel; i > 0; i-- ) {
                RawText( indentChars );
            }
        }
    }
}

