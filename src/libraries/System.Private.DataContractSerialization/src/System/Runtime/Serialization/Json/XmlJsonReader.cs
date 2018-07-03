// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal class XmlJsonReader : XmlBaseReader, IXmlJsonReaderInitializer
    {
        private const int MaxTextChunk = 2048;

        private static byte[] s_charType = new byte[256]
            {
                CharType.None, //   0 (.) 
                CharType.None, //   1 (.) 
                CharType.None, //   2 (.) 
                CharType.None, //   3 (.) 
                CharType.None, //   4 (.) 
                CharType.None, //   5 (.) 
                CharType.None, //   6 (.) 
                CharType.None, //   7 (.) 
                CharType.None, //   8 (.) 
                CharType.None, //   9 (.) 
                CharType.None, //   A (.) 
                CharType.None, //   B (.) 
                CharType.None, //   C (.) 
                CharType.None, //   D (.) 
                CharType.None, //   E (.) 
                CharType.None, //   F (.) 
                CharType.None, //  10 (.) 
                CharType.None, //  11 (.) 
                CharType.None, //  12 (.) 
                CharType.None, //  13 (.) 
                CharType.None, //  14 (.) 
                CharType.None, //  15 (.) 
                CharType.None, //  16 (.) 
                CharType.None, //  17 (.) 
                CharType.None, //  18 (.) 
                CharType.None, //  19 (.) 
                CharType.None, //  1A (.) 
                CharType.None, //  1B (.) 
                CharType.None, //  1C (.) 
                CharType.None, //  1D (.) 
                CharType.None, //  1E (.) 
                CharType.None, //  1F (.) 
                CharType.None, //  20 ( ) 
                CharType.None, //  21 (!) 
                CharType.None, //  22 (") 
                CharType.None, //  23 (#) 
                CharType.None, //  24 ($) 
                CharType.None, //  25 (%) 
                CharType.None, //  26 (&) 
                CharType.None, //  27 (') 
                CharType.None, //  28 (() 
                CharType.None, //  29 ()) 
                CharType.None, //  2A (*) 
                CharType.None, //  2B (+) 
                CharType.None, //  2C (,) 
                CharType.None | CharType.Name, //  2D (-) 
                CharType.None | CharType.Name, //  2E (.) 
                CharType.None, //  2F (/) 
                CharType.None | CharType.Name, //  30 (0) 
                CharType.None | CharType.Name, //  31 (1) 
                CharType.None | CharType.Name, //  32 (2) 
                CharType.None | CharType.Name, //  33 (3) 
                CharType.None | CharType.Name, //  34 (4) 
                CharType.None | CharType.Name, //  35 (5) 
                CharType.None | CharType.Name, //  36 (6) 
                CharType.None | CharType.Name, //  37 (7) 
                CharType.None | CharType.Name, //  38 (8) 
                CharType.None | CharType.Name, //  39 (9) 
                CharType.None, //  3A (:) 
                CharType.None, //  3B (;) 
                CharType.None, //  3C (<) 
                CharType.None, //  3D (=) 
                CharType.None, //  3E (>) 
                CharType.None, //  3F (?) 
                CharType.None, //  40 (@) 
                CharType.None | CharType.FirstName | CharType.Name, //  41 (A) 
                CharType.None | CharType.FirstName | CharType.Name, //  42 (B) 
                CharType.None | CharType.FirstName | CharType.Name, //  43 (C) 
                CharType.None | CharType.FirstName | CharType.Name, //  44 (D) 
                CharType.None | CharType.FirstName | CharType.Name, //  45 (E) 
                CharType.None | CharType.FirstName | CharType.Name, //  46 (F) 
                CharType.None | CharType.FirstName | CharType.Name, //  47 (G) 
                CharType.None | CharType.FirstName | CharType.Name, //  48 (H) 
                CharType.None | CharType.FirstName | CharType.Name, //  49 (I) 
                CharType.None | CharType.FirstName | CharType.Name, //  4A (J) 
                CharType.None | CharType.FirstName | CharType.Name, //  4B (K) 
                CharType.None | CharType.FirstName | CharType.Name, //  4C (L) 
                CharType.None | CharType.FirstName | CharType.Name, //  4D (M) 
                CharType.None | CharType.FirstName | CharType.Name, //  4E (N) 
                CharType.None | CharType.FirstName | CharType.Name, //  4F (O) 
                CharType.None | CharType.FirstName | CharType.Name, //  50 (P) 
                CharType.None | CharType.FirstName | CharType.Name, //  51 (Q) 
                CharType.None | CharType.FirstName | CharType.Name, //  52 (R) 
                CharType.None | CharType.FirstName | CharType.Name, //  53 (S) 
                CharType.None | CharType.FirstName | CharType.Name, //  54 (T) 
                CharType.None | CharType.FirstName | CharType.Name, //  55 (U) 
                CharType.None | CharType.FirstName | CharType.Name, //  56 (V) 
                CharType.None | CharType.FirstName | CharType.Name, //  57 (W) 
                CharType.None | CharType.FirstName | CharType.Name, //  58 (X) 
                CharType.None | CharType.FirstName | CharType.Name, //  59 (Y) 
                CharType.None | CharType.FirstName | CharType.Name, //  5A (Z) 
                CharType.None, //  5B ([) 
                CharType.None, //  5C (\) 
                CharType.None, //  5D (]) 
                CharType.None, //  5E (^) 
                CharType.None | CharType.FirstName | CharType.Name, //  5F (_) 
                CharType.None, //  60 (`) 
                CharType.None | CharType.FirstName | CharType.Name, //  61 (a) 
                CharType.None | CharType.FirstName | CharType.Name, //  62 (b) 
                CharType.None | CharType.FirstName | CharType.Name, //  63 (c) 
                CharType.None | CharType.FirstName | CharType.Name, //  64 (d) 
                CharType.None | CharType.FirstName | CharType.Name, //  65 (e) 
                CharType.None | CharType.FirstName | CharType.Name, //  66 (f) 
                CharType.None | CharType.FirstName | CharType.Name, //  67 (g) 
                CharType.None | CharType.FirstName | CharType.Name, //  68 (h) 
                CharType.None | CharType.FirstName | CharType.Name, //  69 (i) 
                CharType.None | CharType.FirstName | CharType.Name, //  6A (j) 
                CharType.None | CharType.FirstName | CharType.Name, //  6B (k) 
                CharType.None | CharType.FirstName | CharType.Name, //  6C (l) 
                CharType.None | CharType.FirstName | CharType.Name, //  6D (m) 
                CharType.None | CharType.FirstName | CharType.Name, //  6E (n) 
                CharType.None | CharType.FirstName | CharType.Name, //  6F (o) 
                CharType.None | CharType.FirstName | CharType.Name, //  70 (p) 
                CharType.None | CharType.FirstName | CharType.Name, //  71 (q) 
                CharType.None | CharType.FirstName | CharType.Name, //  72 (r) 
                CharType.None | CharType.FirstName | CharType.Name, //  73 (s) 
                CharType.None | CharType.FirstName | CharType.Name, //  74 (t) 
                CharType.None | CharType.FirstName | CharType.Name, //  75 (u) 
                CharType.None | CharType.FirstName | CharType.Name, //  76 (v) 
                CharType.None | CharType.FirstName | CharType.Name, //  77 (w) 
                CharType.None | CharType.FirstName | CharType.Name, //  78 (x) 
                CharType.None | CharType.FirstName | CharType.Name, //  79 (y) 
                CharType.None | CharType.FirstName | CharType.Name, //  7A (z) 
                CharType.None, //  7B ({) 
                CharType.None, //  7C (|) 
                CharType.None, //  7D (}) 
                CharType.None, //  7E (~) 
                CharType.None, //  7F (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  80 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  81 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  82 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  83 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  84 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  85 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  86 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  87 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  88 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  89 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  8A (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  8B (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  8C (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  8D (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  8E (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  8F (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  90 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  91 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  92 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  93 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  94 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  95 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  96 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  97 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  98 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  99 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  9A (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  9B (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  9C (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  9D (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  9E (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  9F (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  A0 (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  A1 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  A2 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  A3 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  A4 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  A5 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  A6 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  A7 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  A8 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  A9 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  AA (??) 
                CharType.None | CharType.FirstName | CharType.Name, //  AB (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  AC (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  AD (.) 
                CharType.None | CharType.FirstName | CharType.Name, //  AE (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  AF (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  B0 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  B1 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  B2 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  B3 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  B4 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  B5 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  B6 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  B7 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  B8 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  B9 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  BA (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  BB (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  BC (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  BD (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  BE (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  BF (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  C0 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  C1 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  C2 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  C3 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  C4 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  C5 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  C6 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  C7 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  C8 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  C9 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  CA (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  CB (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  CC (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  CD (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  CE (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  CF (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  D0 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  D1 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  D2 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  D3 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  D4 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  D5 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  D6 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  D7 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  D8 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  D9 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  DA (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  DB (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  DC (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  DD (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  DE (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  DF (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  E0 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  E1 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  E2 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  E3 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  E4 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  E5 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  E6 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  E7 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  E8 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  E9 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  EA (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  EB (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  EC (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  ED (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  EE (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  EF (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  F0 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  F1 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  F2 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  F3 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  F4 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  F5 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  F6 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  F7 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  F8 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  F9 (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  FA (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  FB (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  FC (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  FD (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  FE (?) 
                CharType.None | CharType.FirstName | CharType.Name, //  FF (?) 
            };
        private bool _buffered;
        private byte[] _charactersToSkipOnNextRead;
        private JsonComplexTextMode _complexTextMode = JsonComplexTextMode.None;
        private bool _expectingFirstElementInNonPrimitiveChild;
        private int _maxBytesPerRead;
        private OnXmlDictionaryReaderClose _onReaderClose;
        private bool _readServerTypeElement = false;
        private int _scopeDepth = 0;
        private JsonNodeType[] _scopes;

        private enum JsonComplexTextMode
        {
            QuotedText,
            NumericalText,
            None
        };

        public override bool CanCanonicalize
        {
            get
            {
                return false;
            }
        }

        public override string Value
        {
            get
            {
                if (IsAttributeValue && !this.IsLocalName(JsonGlobals.typeString))
                {
                    return UnescapeJsonString(base.Value);
                }
                return base.Value;
            }
        }

        private bool IsAttributeValue
        {
            get
            {
                return (this.Node.NodeType == XmlNodeType.Attribute || this.Node is XmlAttributeTextNode);
            }
        }

        private bool IsReadingCollection
        {
            get
            {
                return ((_scopeDepth > 0) && (_scopes[_scopeDepth] == JsonNodeType.Collection));
            }
        }

        private bool IsReadingComplexText
        {
            get
            {
                return ((!this.Node.IsAtomicValue) &&
                    (this.Node.NodeType == XmlNodeType.Text));
            }
        }

        protected override void Dispose(bool disposing)
        {
            OnXmlDictionaryReaderClose onClose = _onReaderClose;
            _onReaderClose = null;
            ResetState();
            if (onClose != null)
            {
                try
                {
                    onClose(this);
                }
                catch (Exception e)
                {
                    if (DiagnosticUtility.IsFatal(e))
                    {
                        throw;
                    }

                    throw new InvalidOperationException(SR.GenericCallbackException, e);
                }
            }
            base.Dispose(disposing);
        }

        public override void EndCanonicalization()
        {
            throw new NotSupportedException();
        }

        public override string GetAttribute(int index)
        {
            return UnescapeJsonString(base.GetAttribute(index));
        }

        public override string GetAttribute(string localName, string namespaceUri)
        {
            if (localName != JsonGlobals.typeString)
            {
                return UnescapeJsonString(base.GetAttribute(localName, namespaceUri));
            }
            return base.GetAttribute(localName, namespaceUri);
        }
        public override string GetAttribute(string name)
        {
            if (name != JsonGlobals.typeString)
            {
                return UnescapeJsonString(base.GetAttribute(name));
            }
            return base.GetAttribute(name);
        }

        public override string GetAttribute(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            if (XmlDictionaryString.GetString(localName) != JsonGlobals.typeString)
            {
                return UnescapeJsonString(base.GetAttribute(localName, namespaceUri));
            }
            return base.GetAttribute(localName, namespaceUri);
        }

        public override bool Read()
        {
            if (this.Node.CanMoveToElement)
            {
                // If we're positioned on an attribute or attribute text on an empty element, we need to move back
                // to the element in order to get the correct setting of ExitScope
                MoveToElement();
            }

            if (this.Node.ReadState == ReadState.Closed)
            {
                return false;
            }
            if (this.Node.ExitScope)
            {
                ExitScope();
            }
            if (!_buffered)
            {
                BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead);
            }

            byte ch;

            // Skip whitespace before checking EOF
            // Complex text check necessary because whitespace could be part of really long
            //    quoted text that's read using multiple Read() calls.
            // This also ensures that we deal with the whitespace-only input case properly by not
            //    floating a root element at all.
            if (!IsReadingComplexText)
            {
                SkipWhitespaceInBufferReader();

                if (TryGetByte(out ch))
                {
                    if (_charactersToSkipOnNextRead[0] == ch || _charactersToSkipOnNextRead[1] == ch)
                    {
                        BufferReader.SkipByte();
                        _charactersToSkipOnNextRead[0] = 0;
                        _charactersToSkipOnNextRead[1] = 0;
                    }
                }

                SkipWhitespaceInBufferReader();

                if (TryGetByte(out ch))
                {
                    if (ch == JsonGlobals.EndCollectionByte && IsReadingCollection)
                    {
                        BufferReader.SkipByte();
                        SkipWhitespaceInBufferReader();
                        ExitJsonScope();
                    }
                }

                if (BufferReader.EndOfFile)
                {
                    if (_scopeDepth > 0)
                    {
                        MoveToEndElement();
                        return true;
                    }
                    else
                    {
                        MoveToEndOfFile();
                        return false;
                    }
                }
            }

            ch = BufferReader.GetByte();

            if (_scopeDepth == 0)
            {
                ReadNonExistentElementName(StringHandleConstStringType.Root);
            }
            else if (IsReadingComplexText)
            {
                switch (_complexTextMode)
                {
                    case JsonComplexTextMode.NumericalText:
                        ReadNumericalText();
                        break;
                    case JsonComplexTextMode.QuotedText:
                        if (ch == (byte)'\\')
                        {
                            ReadEscapedCharacter(true); //  moveToText 
                        }
                        else
                        {
                            ReadQuotedText(true); //  moveToText 
                        }
                        break;
                    case JsonComplexTextMode.None:
                        XmlExceptionHelper.ThrowXmlException(this,
                            new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, (char)ch)));
                        break;
                }
            }
            else if (IsReadingCollection)
            {
                ReadNonExistentElementName(StringHandleConstStringType.Item);
            }
            else if (ch == JsonGlobals.EndCollectionByte)
            {
                BufferReader.SkipByte();
                MoveToEndElement();
                ExitJsonScope();
            }
            else if (ch == JsonGlobals.ObjectByte)
            {
                BufferReader.SkipByte();
                SkipWhitespaceInBufferReader();
                ch = (byte)BufferReader.GetByte();
                if (ch == JsonGlobals.EndObjectByte)
                {
                    BufferReader.SkipByte();
                    SkipWhitespaceInBufferReader();
                    if (TryGetByte(out ch))
                    {
                        if (ch == JsonGlobals.MemberSeparatorByte)
                        {
                            BufferReader.SkipByte();
                        }
                    }
                    else
                    {
                        _charactersToSkipOnNextRead[0] = JsonGlobals.MemberSeparatorByte;
                    }
                    MoveToEndElement();
                }
                else
                {
                    EnterJsonScope(JsonNodeType.Object);
                    ParseStartElement();
                }
            }
            else if (ch == JsonGlobals.EndObjectByte)
            {
                BufferReader.SkipByte();
                if (_expectingFirstElementInNonPrimitiveChild)
                {
                    SkipWhitespaceInBufferReader();
                    ch = BufferReader.GetByte();
                    if ((ch == JsonGlobals.MemberSeparatorByte) ||
                        (ch == JsonGlobals.EndObjectByte))
                    {
                        BufferReader.SkipByte();
                    }
                    else
                    {
                        XmlExceptionHelper.ThrowXmlException(this,
                            new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter,
                            (char)ch)));
                    }
                    _expectingFirstElementInNonPrimitiveChild = false;
                }
                MoveToEndElement();
            }
            else if (ch == JsonGlobals.MemberSeparatorByte)
            {
                BufferReader.SkipByte();
                MoveToEndElement();
            }
            else if (ch == JsonGlobals.QuoteByte)
            {
                if (_readServerTypeElement)
                {
                    _readServerTypeElement = false;
                    EnterJsonScope(JsonNodeType.Object);
                    ParseStartElement();
                }
                else if (this.Node.NodeType == XmlNodeType.Element)
                {
                    if (_expectingFirstElementInNonPrimitiveChild)
                    {
                        EnterJsonScope(JsonNodeType.Object);
                        ParseStartElement();
                    }
                    else
                    {
                        BufferReader.SkipByte();
                        ReadQuotedText(true); //  moveToText 
                    }
                }
                else if (this.Node.NodeType == XmlNodeType.EndElement)
                {
                    EnterJsonScope(JsonNodeType.Element);
                    ParseStartElement();
                }
                else
                {
                    XmlExceptionHelper.ThrowXmlException(this,
                        new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter,
                        JsonGlobals.QuoteChar)));
                }
            }
            else if (ch == (byte)'f')
            {
                int offset;
                byte[] buffer = BufferReader.GetBuffer(5, out offset);
                if (buffer[offset + 1] != (byte)'a' ||
                    buffer[offset + 2] != (byte)'l' ||
                    buffer[offset + 3] != (byte)'s' ||
                    buffer[offset + 4] != (byte)'e')
                {
                    XmlExceptionHelper.ThrowTokenExpected(this, "false", Encoding.UTF8.GetString(buffer, offset, 5));
                }
                BufferReader.Advance(5);

                if (TryGetByte(out ch))
                {
                    if (!IsWhitespace(ch) && ch != JsonGlobals.MemberSeparatorByte && ch != JsonGlobals.EndObjectChar && ch != JsonGlobals.EndCollectionByte)
                    {
                        XmlExceptionHelper.ThrowTokenExpected(this, "false", Encoding.UTF8.GetString(buffer, offset, 4) + (char)ch);
                    }
                }
                MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, 5);
            }
            else if (ch == (byte)'t')
            {
                int offset;
                byte[] buffer = BufferReader.GetBuffer(4, out offset);
                if (buffer[offset + 1] != (byte)'r' ||
                    buffer[offset + 2] != (byte)'u' ||
                    buffer[offset + 3] != (byte)'e')
                {
                    XmlExceptionHelper.ThrowTokenExpected(this, "true", Encoding.UTF8.GetString(buffer, offset, 4));
                }
                BufferReader.Advance(4);

                if (TryGetByte(out ch))
                {
                    if (!IsWhitespace(ch) && ch != JsonGlobals.MemberSeparatorByte && ch != JsonGlobals.EndObjectChar && ch != JsonGlobals.EndCollectionByte)
                    {
                        XmlExceptionHelper.ThrowTokenExpected(this, "true", Encoding.UTF8.GetString(buffer, offset, 4) + (char)ch);
                    }
                }
                MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, 4);
            }
            else if (ch == (byte)'n')
            {
                int offset;
                byte[] buffer = BufferReader.GetBuffer(4, out offset);
                if (buffer[offset + 1] != (byte)'u' ||
                    buffer[offset + 2] != (byte)'l' ||
                    buffer[offset + 3] != (byte)'l')
                {
                    XmlExceptionHelper.ThrowTokenExpected(this, "null", Encoding.UTF8.GetString(buffer, offset, 4));
                }
                BufferReader.Advance(4);
                SkipWhitespaceInBufferReader();

                if (TryGetByte(out ch))
                {
                    if (ch == JsonGlobals.MemberSeparatorByte || ch == JsonGlobals.EndObjectChar)
                    {
                        BufferReader.SkipByte();
                    }
                    else if (ch != JsonGlobals.EndCollectionByte)
                    {
                        XmlExceptionHelper.ThrowTokenExpected(this, "null", Encoding.UTF8.GetString(buffer, offset, 4) + (char)ch);
                    }
                }
                else
                {
                    _charactersToSkipOnNextRead[0] = JsonGlobals.MemberSeparatorByte;
                    _charactersToSkipOnNextRead[1] = JsonGlobals.EndObjectByte;
                }
                MoveToEndElement();
            }
            else if ((ch == (byte)'-') ||
                (((byte)'0' <= ch) && (ch <= (byte)'9')) ||
                (ch == (byte)'I') ||
                (ch == (byte)'N'))
            {
                ReadNumericalText();
            }
            else
            {
                XmlExceptionHelper.ThrowXmlException(this,
                    new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, (char)ch)));
            }

            return true;
        }

        public override decimal ReadContentAsDecimal()
        {
            string value = ReadContentAsString();
            try
            {
                return decimal.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "decimal", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "decimal", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "decimal", exception);
            }
        }

        public override int ReadContentAsInt()
        {
            return ParseInt(ReadContentAsString(), NumberStyles.Float);
        }

        public override long ReadContentAsLong()
        {
            string value = ReadContentAsString();
            try
            {
                return long.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Int64", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Int64", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Int64", exception);
            }
        }

        public override int ReadValueAsBase64(byte[] buffer, int offset, int count)
        {
            if (IsAttributeValue)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                if (offset < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative);
                }
                if (offset > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, buffer.Length));
                }
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative);
                }
                if (count > buffer.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset));
                }

                return 0;
            }

            return base.ReadValueAsBase64(buffer, offset, count);
        }

        public override int ReadValueChunk(char[] chars, int offset, int count)
        {
            if (IsAttributeValue)
            {
                if (chars == null)
                {
                    throw new ArgumentNullException(nameof(chars));
                }
                if (offset < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative);
                }
                if (offset > chars.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));
                }
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative);
                }
                if (count > chars.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset));
                }
                int actual;

                string value = UnescapeJsonString(this.Node.ValueAsString);
                actual = Math.Min(count, value.Length);
                if (actual > 0)
                {
                    value.CopyTo(0, chars, offset, actual);
                    if (this.Node.QNameType == QNameType.Xmlns)
                    {
                        this.Node.Namespace.Uri.SetValue(0, 0);
                    }
                    else
                    {
                        this.Node.Value.SetValue(ValueHandleType.UTF8, 0, 0);
                    }
                }
                return actual;
            }

            return base.ReadValueChunk(chars, offset, count);
        }

        public void SetInput(byte[] buffer, int offset, int count, Encoding encoding, XmlDictionaryReaderQuotas quotas,
            OnXmlDictionaryReaderClose onClose)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative);
            }
            if (offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.JsonOffsetExceedsBufferSize, buffer.Length));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative);
            }
            if (count > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.JsonSizeExceedsRemainingBufferSpace, buffer.Length - offset));
            }
            MoveToInitial(quotas, onClose);

            ArraySegment<byte> seg = JsonEncodingStreamWrapper.ProcessBuffer(buffer, offset, count, encoding);
            BufferReader.SetBuffer(seg.Array, seg.Offset, seg.Count, null, null);
            _buffered = true;
            ResetState();
        }

        public void SetInput(Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas,
            OnXmlDictionaryReaderClose onClose)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            MoveToInitial(quotas, onClose);

            stream = new JsonEncodingStreamWrapper(stream, encoding, true);

            BufferReader.SetBuffer(stream, null, null);
            _buffered = false;
            ResetState();
        }

        public override void StartCanonicalization(Stream stream, bool includeComments, string[] inclusivePrefixes)
        {
            throw new NotSupportedException();
        }

        internal static void CheckArray(Array array, int offset, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative);
            }
            if (offset > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, array.Length));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative);
            }
            if (count > array.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, array.Length - offset));
            }
        }

        private static int BreakText(byte[] buffer, int offset, int length)
        {
            // See if we might be breaking a utf8 sequence
            if (length > 0 && (buffer[offset + length - 1] & 0x80) == 0x80)
            {
                // Find the lead char of the utf8 sequence (0x11xxxxxx)
                int originalLength = length;
                do
                {
                    length--;
                } while (length > 0 && (buffer[offset + length] & 0xC0) != 0xC0);
                // Couldn't find the lead char
                if (length == 0)
                {
                    return originalLength; // Invalid utf8 sequence - can't break
                }
                // Count how many bytes follow the lead char
                byte b = unchecked((byte)(buffer[offset + length] << 2));
                int byteCount = 2;
                while ((b & 0x80) == 0x80)
                {
                    b = (byte)(b << 1);
                    byteCount++;
                    // There shouldn't be more than 3 bytes following the lead char
                    if (byteCount > 4)
                    {
                        return originalLength; // Invalid utf8 sequence - can't break
                    }
                }
                if (length + byteCount == originalLength)
                {
                    return originalLength; // sequence fits exactly
                }
            }
            return length;
        }

        private static int ComputeNumericalTextLength(byte[] buffer, int offset, int offsetMax)
        {
            int beginOffset = offset;
            while (offset < offsetMax)
            {
                byte ch = buffer[offset];
                if (ch == JsonGlobals.MemberSeparatorByte || ch == JsonGlobals.EndObjectByte || ch == JsonGlobals.EndCollectionByte
                    || IsWhitespace(ch))
                {
                    break;
                }
                offset++;
            }
            return offset - beginOffset;
        }

        private static int ComputeQuotedTextLengthUntilEndQuote(byte[] buffer, int offset, int offsetMax, out bool escaped)
        {
            // Assumes that for quoted text "someText", the first " has been consumed.
            // For original text "someText", buffer passed in is someText".
            // This method returns return 8 for someText" (s, o, m, e, T, e, x, t).
            int beginOffset = offset;
            escaped = false;

            while (offset < offsetMax)
            {
                byte ch = buffer[offset];
                if (ch < 0x20)
                {
                    throw new FormatException(SR.Format(SR.InvalidCharacterEncountered, (char)ch));
                }
                else if (ch == (byte)'\\' || ch == 0xEF)
                {
                    escaped = true;
                    break;
                }
                else if (ch == JsonGlobals.QuoteByte)
                {
                    break;
                }

                offset++;
            }

            return offset - beginOffset;
        }


        // From JSON spec:
        // ws = *(
        //    %x20 /              ; Space
        //    %x09 /              ; Horizontal tab
        //    %x0A /              ; Line feed or New line
        //    %x0D                ; Carriage return
        // )            
        private static bool IsWhitespace(byte ch)
        {
            return ((ch == 0x20) || (ch == 0x09) || (ch == 0x0A) || (ch == 0x0D));
        }

        private static char ParseChar(string value, NumberStyles style)
        {
            int intValue = ParseInt(value, style);
            try
            {
                return Convert.ToChar(intValue);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "char", exception);
            }
        }

        private static int ParseInt(string value, NumberStyles style)
        {
            try
            {
                return int.Parse(value, style, NumberFormatInfo.InvariantInfo);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Int32", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Int32", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Int32", exception);
            }
        }

        private void BufferElement()
        {
            int elementOffset = BufferReader.Offset;
            const int byteCount = 128;
            bool done = false;
            byte quoteChar = 0;
            while (!done)
            {
                int offset;
                int offsetMax;
                byte[] buffer = BufferReader.GetBuffer(byteCount, out offset, out offsetMax);
                if (offset + byteCount != offsetMax)
                {
                    break;
                }
                for (int i = offset; i < offsetMax && !done; i++)
                {
                    byte b = buffer[i];
                    if (b == '\\')
                    {
                        i++;
                        if (i >= offsetMax)
                        {
                            break;
                        }
                    }
                    else if (quoteChar == 0)
                    {
                        if (b == (byte)'\'' || b == JsonGlobals.QuoteByte)
                        {
                            quoteChar = b;
                        }
                        if (b == JsonGlobals.NameValueSeparatorByte)
                        {
                            done = true;
                        }
                    }
                    else
                    {
                        if (b == quoteChar)
                        {
                            quoteChar = 0;
                        }
                    }
                }
                BufferReader.Advance(byteCount);
            }
            BufferReader.Offset = elementOffset;
        }

        private void EnterJsonScope(JsonNodeType currentNodeType)
        {
            _scopeDepth++;
            if (_scopes == null)
            {
                _scopes = new JsonNodeType[4];
            }
            else if (_scopes.Length == _scopeDepth)
            {
                JsonNodeType[] newScopes = new JsonNodeType[_scopeDepth * 2];
                Array.Copy(_scopes, 0, newScopes, 0, _scopeDepth);
                _scopes = newScopes;
            }
            _scopes[_scopeDepth] = currentNodeType;
        }

        private JsonNodeType ExitJsonScope()
        {
            JsonNodeType nodeTypeToReturn = _scopes[_scopeDepth];
            _scopes[_scopeDepth] = JsonNodeType.None;
            _scopeDepth--;
            return nodeTypeToReturn;
        }

        private new void MoveToEndElement()
        {
            ExitJsonScope();
            base.MoveToEndElement();
        }

        private void MoveToInitial(XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            MoveToInitial(quotas);
            _maxBytesPerRead = quotas.MaxBytesPerRead;
            _onReaderClose = onClose;
        }

        private void ParseAndSetLocalName()
        {
            XmlElementNode elementNode = EnterScope();
            elementNode.NameOffset = BufferReader.Offset;

            do
            {
                if (BufferReader.GetByte() == '\\')
                {
                    ReadEscapedCharacter(false); //  moveToText 
                }
                else
                {
                    ReadQuotedText(false); //  moveToText 
                }
            } while (_complexTextMode == JsonComplexTextMode.QuotedText);

            int actualOffset = BufferReader.Offset - 1; //  -1 to ignore " at end of local name 
            elementNode.LocalName.SetValue(elementNode.NameOffset, actualOffset - elementNode.NameOffset);
            elementNode.NameLength = actualOffset - elementNode.NameOffset;
            elementNode.Namespace.Uri.SetValue(elementNode.NameOffset, 0);
            elementNode.Prefix.SetValue(PrefixHandleType.Empty);
            elementNode.IsEmptyElement = false;
            elementNode.ExitScope = false;
            elementNode.BufferOffset = actualOffset;

            int currentCharacter = (int)BufferReader.GetByte(elementNode.NameOffset);
            if ((s_charType[currentCharacter] & CharType.FirstName) == 0)
            {
                SetJsonNameWithMapping(elementNode);
            }
            else
            {
                for (int i = 0, offset = elementNode.NameOffset; i < elementNode.NameLength; i++, offset++)
                {
                    currentCharacter = (int)BufferReader.GetByte(offset);
                    if ((s_charType[currentCharacter] & CharType.Name) == 0 || currentCharacter >= 0x80)
                    {
                        SetJsonNameWithMapping(elementNode);
                        break;
                    }
                }
            }
        }

        private void ParseStartElement()
        {
            if (!_buffered)
            {
                BufferElement();
            }

            _expectingFirstElementInNonPrimitiveChild = false;

            byte ch = BufferReader.GetByte();
            if (ch == JsonGlobals.QuoteByte)
            {
                BufferReader.SkipByte();

                ParseAndSetLocalName();

                SkipWhitespaceInBufferReader();
                SkipExpectedByteInBufferReader(JsonGlobals.NameValueSeparatorByte);
                SkipWhitespaceInBufferReader();


                if (BufferReader.GetByte() == JsonGlobals.ObjectByte)
                {
                    BufferReader.SkipByte();
                    _expectingFirstElementInNonPrimitiveChild = true;
                }
                ReadAttributes();
            }
            else
            {
                // " and } are the only two valid characters that may follow a {
                XmlExceptionHelper.ThrowTokenExpected(this, "\"", (char)ch);
            }
        }

        private void ReadAttributes()
        {
            XmlAttributeNode attribute = AddAttribute();
            attribute.LocalName.SetConstantValue(StringHandleConstStringType.Type);
            attribute.Namespace.Uri.SetValue(0, 0);
            attribute.Prefix.SetValue(PrefixHandleType.Empty);

            SkipWhitespaceInBufferReader();
            byte nextByte = BufferReader.GetByte();
            switch (nextByte)
            {
                case JsonGlobals.QuoteByte:
                    if (!_expectingFirstElementInNonPrimitiveChild)
                    {
                        attribute.Value.SetConstantValue(ValueHandleConstStringType.String);
                    }
                    else
                    {
                        attribute.Value.SetConstantValue(ValueHandleConstStringType.Object);
                        ReadServerTypeAttribute(true);
                    }
                    break;
                case (byte)'n':
                    attribute.Value.SetConstantValue(ValueHandleConstStringType.Null);
                    break;
                case (byte)'t':
                case (byte)'f':
                    attribute.Value.SetConstantValue(ValueHandleConstStringType.Boolean);
                    break;
                case JsonGlobals.ObjectByte:
                    attribute.Value.SetConstantValue(ValueHandleConstStringType.Object);
                    ReadServerTypeAttribute(false);
                    break;
                case JsonGlobals.EndObjectByte:
                    if (_expectingFirstElementInNonPrimitiveChild)
                    {
                        attribute.Value.SetConstantValue(ValueHandleConstStringType.Object);
                    }
                    else
                    {
                        XmlExceptionHelper.ThrowXmlException(this,
                            new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, (char)nextByte)));
                    }
                    break;
                case JsonGlobals.CollectionByte:
                    attribute.Value.SetConstantValue(ValueHandleConstStringType.Array);
                    BufferReader.SkipByte();
                    EnterJsonScope(JsonNodeType.Collection);
                    break;
                default:
                    if (nextByte == '-' ||
                        (nextByte <= '9' && nextByte >= '0') ||
                        nextByte == 'N' ||
                        nextByte == 'I')
                    {
                        attribute.Value.SetConstantValue(ValueHandleConstStringType.Number);
                    }
                    else
                    {
                        XmlExceptionHelper.ThrowXmlException(this,
                            new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, (char)nextByte)));
                    }
                    break;
            }
        }

        private void ReadEscapedCharacter(bool moveToText)
        {
            BufferReader.SkipByte();
            char ch = (char)BufferReader.GetByte();
            if (ch == 'u')
            {
                BufferReader.SkipByte();
                int offset;
                byte[] buffer = BufferReader.GetBuffer(5, out offset);
                string bufferAsString = Encoding.UTF8.GetString(buffer, offset, 4);
                BufferReader.Advance(4);
                int charValue = ParseChar(bufferAsString, NumberStyles.HexNumber);
                if (char.IsHighSurrogate((char)charValue))
                {
                    byte nextByte = BufferReader.GetByte();
                    if (nextByte == (byte)'\\')
                    {
                        BufferReader.SkipByte();
                        SkipExpectedByteInBufferReader((byte)'u');
                        buffer = BufferReader.GetBuffer(5, out offset);
                        bufferAsString = Encoding.UTF8.GetString(buffer, offset, 4);
                        BufferReader.Advance(4);
                        char lowChar = ParseChar(bufferAsString, NumberStyles.HexNumber);
                        if (!char.IsLowSurrogate(lowChar))
                        {
                            XmlExceptionHelper.ThrowXmlException(this,
                                new XmlException(SR.Format(SR.XmlInvalidLowSurrogate, bufferAsString)));
                        }
                        charValue = new SurrogateChar(lowChar, (char)charValue).Char;
                    }
                }

                if (buffer[offset + 4] == JsonGlobals.QuoteByte)
                {
                    BufferReader.SkipByte();
                    if (moveToText)
                    {
                        MoveToAtomicText().Value.SetCharValue(charValue);
                    }
                    _complexTextMode = JsonComplexTextMode.None;
                }
                else
                {
                    if (moveToText)
                    {
                        MoveToComplexText().Value.SetCharValue(charValue);
                    }
                    _complexTextMode = JsonComplexTextMode.QuotedText;
                }
            }
            else
            {
                switch (ch)
                {
                    case 'b':
                        ch = '\b';
                        break;
                    case 'f':
                        ch = '\f';
                        break;
                    case 'n':
                        ch = '\n';
                        break;
                    case 'r':
                        ch = '\r';
                        break;
                    case 't':
                        ch = '\t';
                        break;
                    case '\"':
                    case '\\':
                    case '/':
                        // Do nothing. These are the actual unescaped values.
                        break;
                    default:
                        XmlExceptionHelper.ThrowXmlException(this,
                            new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, (char)ch)));
                        break;
                }
                BufferReader.SkipByte();
                if (BufferReader.GetByte() == JsonGlobals.QuoteByte)
                {
                    BufferReader.SkipByte();
                    if (moveToText)
                    {
                        MoveToAtomicText().Value.SetCharValue(ch);
                    }
                    _complexTextMode = JsonComplexTextMode.None;
                }
                else
                {
                    if (moveToText)
                    {
                        MoveToComplexText().Value.SetCharValue(ch);
                    }
                    _complexTextMode = JsonComplexTextMode.QuotedText;
                }
            }
        }

        private void ReadNonExistentElementName(StringHandleConstStringType elementName)
        {
            EnterJsonScope(JsonNodeType.Object);
            XmlElementNode elementNode = EnterScope();
            elementNode.LocalName.SetConstantValue(elementName);
            elementNode.Namespace.Uri.SetValue(elementNode.NameOffset, 0);
            elementNode.Prefix.SetValue(PrefixHandleType.Empty);
            elementNode.BufferOffset = BufferReader.Offset;
            elementNode.IsEmptyElement = false;
            elementNode.ExitScope = false;
            ReadAttributes();
        }

        private int ReadNonFFFE()
        {
            int off;
            byte[] buff = BufferReader.GetBuffer(3, out off);
            if (buff[off + 1] == 0xBF && (buff[off + 2] == 0xBE || buff[off + 2] == 0xBF))
            {
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.JsonInvalidFFFE));
            }
            return 3;
        }

        private void ReadNumericalText()
        {
            byte[] buffer;
            int offset;
            int offsetMax;
            int length;

            if (_buffered)
            {
                buffer = BufferReader.GetBuffer(out offset, out offsetMax);
                length = ComputeNumericalTextLength(buffer, offset, offsetMax);
            }
            else
            {
                buffer = BufferReader.GetBuffer(MaxTextChunk, out offset, out offsetMax);
                length = ComputeNumericalTextLength(buffer, offset, offsetMax);
                length = BreakText(buffer, offset, length);
            }
            BufferReader.Advance(length);

            if (offset <= offsetMax - length)
            {
                MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, length);
                _complexTextMode = JsonComplexTextMode.None;
            }
            else
            {
                MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, length);
                _complexTextMode = JsonComplexTextMode.NumericalText;
            }
        }

        private void ReadQuotedText(bool moveToText)
        {
            byte[] buffer;
            int offset;
            int offsetMax;
            int length;
            bool escaped;
            bool endReached;

            if (_buffered)
            {
                buffer = BufferReader.GetBuffer(out offset, out offsetMax);
                length = ComputeQuotedTextLengthUntilEndQuote(buffer, offset, offsetMax, out escaped);
                endReached = offset < offsetMax - length;
            }
            else
            {
                buffer = BufferReader.GetBuffer(MaxTextChunk, out offset, out offsetMax);
                length = ComputeQuotedTextLengthUntilEndQuote(buffer, offset, offsetMax, out escaped);
                endReached = offset < offsetMax - length;
                length = BreakText(buffer, offset, length);
            }

            if (escaped && BufferReader.GetByte() == 0xEF)
            {
                offset = BufferReader.Offset;
                length = ReadNonFFFE();
            }

            BufferReader.Advance(length);

            if (!escaped && endReached)
            {
                if (moveToText)
                {
                    MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, length);
                }
                SkipExpectedByteInBufferReader(JsonGlobals.QuoteByte);
                _complexTextMode = JsonComplexTextMode.None;
            }
            else
            {
                if ((length == 0) && escaped)
                {
                    ReadEscapedCharacter(moveToText);
                }
                else
                {
                    if (moveToText)
                    {
                        MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, length);
                    }
                    _complexTextMode = JsonComplexTextMode.QuotedText;
                }
            }
        }

        private void ReadServerTypeAttribute(bool consumedObjectChar)
        {
            if (!consumedObjectChar)
            {
                SkipExpectedByteInBufferReader(JsonGlobals.ObjectByte);
                SkipWhitespaceInBufferReader();

                // we only allow " or } after {
                byte ch = BufferReader.GetByte();
                if (ch != JsonGlobals.QuoteByte && ch != JsonGlobals.EndObjectByte)
                {
                    XmlExceptionHelper.ThrowTokenExpected(this, "\"", (char)ch);
                }
            }
            else
            {
                SkipWhitespaceInBufferReader();
            }

            int offset;
            int offsetMax;
            byte[] buffer = BufferReader.GetBuffer(8, out offset, out offsetMax);
            if (offset + 8 <= offsetMax)
            {
                if (buffer[offset + 0] == (byte)'\"' &&
                    buffer[offset + 1] == (byte)'_' &&
                    buffer[offset + 2] == (byte)'_' &&
                    buffer[offset + 3] == (byte)'t' &&
                    buffer[offset + 4] == (byte)'y' &&
                    buffer[offset + 5] == (byte)'p' &&
                    buffer[offset + 6] == (byte)'e' &&
                    buffer[offset + 7] == (byte)'\"')
                {
                    XmlAttributeNode attribute = AddAttribute();

                    attribute.LocalName.SetValue(offset + 1, 6);
                    attribute.Namespace.Uri.SetValue(0, 0);
                    attribute.Prefix.SetValue(PrefixHandleType.Empty);
                    BufferReader.Advance(8);

                    if (!_buffered)
                    {
                        BufferElement();
                    }

                    SkipWhitespaceInBufferReader();
                    SkipExpectedByteInBufferReader(JsonGlobals.NameValueSeparatorByte);
                    SkipWhitespaceInBufferReader();
                    SkipExpectedByteInBufferReader(JsonGlobals.QuoteByte);

                    buffer = BufferReader.GetBuffer(out offset, out offsetMax);

                    do
                    {
                        if (BufferReader.GetByte() == '\\')
                        {
                            ReadEscapedCharacter(false); //  moveToText 
                        }
                        else
                        {
                            ReadQuotedText(false); //  moveToText 
                        }
                    } while (_complexTextMode == JsonComplexTextMode.QuotedText);

                    attribute.Value.SetValue(ValueHandleType.UTF8, offset, BufferReader.Offset - 1 - offset);

                    SkipWhitespaceInBufferReader();

                    if (BufferReader.GetByte() == JsonGlobals.MemberSeparatorByte)
                    {
                        BufferReader.SkipByte();
                        _readServerTypeElement = true;
                    }
                }
            }
            if (BufferReader.GetByte() == JsonGlobals.EndObjectByte)
            {
                BufferReader.SkipByte();
                _readServerTypeElement = false;
                _expectingFirstElementInNonPrimitiveChild = false;
            }
            else
            {
                _readServerTypeElement = true;
            }
        }

        private void ResetState()
        {
            _complexTextMode = JsonComplexTextMode.None;
            _expectingFirstElementInNonPrimitiveChild = false;
            _charactersToSkipOnNextRead = new byte[2];
            _scopeDepth = 0;
            if ((_scopes != null) && (_scopes.Length > JsonGlobals.maxScopeSize))
            {
                _scopes = null;
            }
        }

        private void SetJsonNameWithMapping(XmlElementNode elementNode)
        {
            Namespace ns = AddNamespace();
            ns.Prefix.SetValue(PrefixHandleType.A);
            ns.Uri.SetConstantValue(StringHandleConstStringType.Item);
            AddXmlnsAttribute(ns);

            XmlAttributeNode attribute = AddAttribute();
            attribute.LocalName.SetConstantValue(StringHandleConstStringType.Item);
            attribute.Namespace.Uri.SetValue(0, 0);
            attribute.Prefix.SetValue(PrefixHandleType.Empty);
            attribute.Value.SetValue(ValueHandleType.UTF8, elementNode.NameOffset, elementNode.NameLength);

            elementNode.NameLength = 0;
            elementNode.Prefix.SetValue(PrefixHandleType.A);
            elementNode.LocalName.SetConstantValue(StringHandleConstStringType.Item);
            elementNode.Namespace = ns;
        }

        private void SkipExpectedByteInBufferReader(byte characterToSkip)
        {
            if (BufferReader.GetByte() != characterToSkip)
            {
                XmlExceptionHelper.ThrowTokenExpected(this, ((char)characterToSkip).ToString(), (char)BufferReader.GetByte());
            }
            BufferReader.SkipByte();
        }

        private void SkipWhitespaceInBufferReader()
        {
            byte ch;
            while (TryGetByte(out ch) && IsWhitespace(ch))
            {
                BufferReader.SkipByte();
            }
        }

        private bool TryGetByte(out byte ch)
        {
            int offset, offsetMax;
            byte[] buffer = BufferReader.GetBuffer(1, out offset, out offsetMax);

            if (offset < offsetMax)
            {
                ch = buffer[offset];
                return true;
            }
            else
            {
                ch = (byte)'\0';
                return false;
            }
        }

        private string UnescapeJsonString(string val)
        {
            if (val == null)
            {
                return null;
            }

            StringBuilder sb = null;
            int startIndex = 0, count = 0;
            for (int i = 0; i < val.Length; i++)
            {
                if (val[i] == '\\')
                {
                    i++;
                    if (sb == null)
                    {
                        sb = new StringBuilder();
                    }
                    sb.Append(val, startIndex, count);
                    Fx.Assert(i < val.Length, "Found that an '\' was the last character in a string. ReadServerTypeAttriute validates that the escape sequence is valid when it calls ReadQuotedText and ReadEscapedCharacter");
                    if (i >= val.Length)
                    {
                        XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, val[i])));
                    }
                    switch (val[i])
                    {
                        case '"':
                        case '\'':
                        case '/':
                        case '\\':
                            sb.Append(val[i]);
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            if ((i + 3) >= val.Length)
                            {
                                XmlExceptionHelper.ThrowXmlException(this,
                                    new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, val[i])));
                            }
                            sb.Append(ParseChar(val.Substring(i + 1, 4), NumberStyles.HexNumber));
                            i += 4;
                            break;
                    }
                    startIndex = i + 1;
                    count = 0;
                }
                else
                {
                    count++;
                }
            }
            if (sb == null)
            {
                return val;
            }
            if (count > 0)
            {
                sb.Append(val, startIndex, count);
            }

            return sb.ToString();
        }

        protected override XmlSigningNodeWriter CreateSigningNodeWriter()
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(SR.Format(SR.JsonMethodNotSupported, "CreateSigningNodeWriter")));
        }

        private static class CharType
        {
            public const byte FirstName = 0x01;
            public const byte Name = 0x02;
            public const byte None = 0x00;
        }
    }
}
