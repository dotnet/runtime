// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Xml.Schema;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System.Xml
{
    internal partial class DtdParser : IDtdParser
    {
        //
        // Private types
        //
        private enum Token
        {
            CDATA = XmlTokenizedType.CDATA,       // == 0
            ID = XmlTokenizedType.ID,          // == 1
            IDREF = XmlTokenizedType.IDREF,       // == 2
            IDREFS = XmlTokenizedType.IDREFS,      // == 3
            ENTITY = XmlTokenizedType.ENTITY,      // == 4
            ENTITIES = XmlTokenizedType.ENTITIES,    // == 5
            NMTOKEN = XmlTokenizedType.NMTOKEN,     // == 6
            NMTOKENS = XmlTokenizedType.NMTOKENS,    // == 7
            NOTATION = XmlTokenizedType.NOTATION,    // == 8
            None,
            PERef,
            AttlistDecl,
            ElementDecl,
            EntityDecl,
            NotationDecl,
            Comment,
            PI,
            CondSectionStart,
            CondSectionEnd,
            Eof,
            REQUIRED,
            IMPLIED,
            FIXED,
            QName,
            Name,
            Nmtoken,
            Quote,
            LeftParen,
            RightParen,
            GreaterThan,
            Or,
            LeftBracket,
            RightBracket,
            PUBLIC,
            SYSTEM,
            Literal,
            DOCTYPE,
            NData,
            Percent,
            Star,
            QMark,
            Plus,
            PCDATA,
            Comma,
            ANY,
            EMPTY,
            IGNORE,
            INCLUDE,
        }

        private enum ScanningFunction
        {
            SubsetContent,
            Name,
            QName,
            Nmtoken,
            Doctype1,
            Doctype2,
            Element1,
            Element2,
            Element3,
            Element4,
            Element5,
            Element6,
            Element7,
            Attlist1,
            Attlist2,
            Attlist3,
            Attlist4,
            Attlist5,
            Attlist6,
            Attlist7,
            Entity1,
            Entity2,
            Entity3,
            Notation1,
            CondSection1,
            CondSection2,
            CondSection3,
            Literal,
            SystemId,
            PublicId1,
            PublicId2,
            ClosingTag,
            ParamEntitySpace,
            None,
        }

        private enum LiteralType
        {
            AttributeValue,
            EntityReplText,
            SystemOrPublicID
        }

#if !SILVERLIGHT
        private class UndeclaredNotation
        {
            internal string name;
            internal int lineNo;
            internal int linePos;
            internal UndeclaredNotation next;

            internal UndeclaredNotation(string name, int lineNo, int linePos)
            {
                this.name = name;
                this.lineNo = lineNo;
                this.linePos = linePos;
                this.next = null;
            }
        }
#endif

        //
        // Fields
        //
        // connector to reader
        private IDtdParserAdapter _readerAdapter;
#if !SILVERLIGHT
        private IDtdParserAdapterWithValidation _readerAdapterWithValidation;
#endif

        // name table
        private XmlNameTable _nameTable;

        // final schema info
        private SchemaInfo _schemaInfo;

        // XmlCharType instance
        private XmlCharType _xmlCharType = XmlCharType.Instance;

        // system & public id
        private string _systemId = string.Empty;
        private string _publicId = string.Empty;

        // flags
#if !SILVERLIGHT
        private bool _normalize = true;
        private bool _validate = false;
        private bool _supportNamespaces = true;
        private bool _v1Compat = false;
#endif

        // cached character buffer
        private char[] _chars;
        private int _charsUsed;
        private int _curPos;

        // scanning function for the next token
        private ScanningFunction _scanningFunction;
        private ScanningFunction _nextScaningFunction;
        private ScanningFunction _savedScanningFunction; // this one is used only for adding spaces around parameter entities

        // flag if whitespace seen before token
        private bool _whitespaceSeen;

        // start position of the last token (for name and value)
        private int _tokenStartPos;

        // colon position (for name)
        private int _colonPos;

        // value of the internal subset
        private StringBuilder _internalSubsetValueSb = null;

        // entities
        private int _externalEntitiesDepth = 0;
        private int _currentEntityId = 0;

        // free-floating DTD support
        private bool _freeFloatingDtd = false;
        private bool _hasFreeFloatingInternalSubset = false;

        // misc
        private StringBuilder _stringBuilder;
        private int _condSectionDepth = 0;
        private LineInfo _literalLineInfo = new LineInfo(0, 0);
        private char _literalQuoteChar = '"';
        private string _documentBaseUri = string.Empty;
        private string _externalDtdBaseUri = string.Empty;

#if !SILVERLIGHT
        private Dictionary<string, UndeclaredNotation> _undeclaredNotations = null;
        private int[] _condSectionEntityIds = null;
#endif

        private const int CondSectionEntityIdsInitialSize = 2;

        //
        // Constructor
        //

        static DtdParser()
        {
#if DEBUG
            //  The absolute numbering is utilized in attribute type parsing
            Debug.Assert( (int)Token.CDATA    == (int)XmlTokenizedType.CDATA    && (int)XmlTokenizedType.CDATA    == 0 );
            Debug.Assert( (int)Token.ID       == (int)XmlTokenizedType.ID       && (int)XmlTokenizedType.ID       == 1 );
            Debug.Assert( (int)Token.IDREF    == (int)XmlTokenizedType.IDREF    && (int)XmlTokenizedType.IDREF    == 2 );
            Debug.Assert( (int)Token.IDREFS   == (int)XmlTokenizedType.IDREFS   && (int)XmlTokenizedType.IDREFS   == 3 );
            Debug.Assert( (int)Token.ENTITY   == (int)XmlTokenizedType.ENTITY   && (int)XmlTokenizedType.ENTITY   == 4 );
            Debug.Assert( (int)Token.ENTITIES == (int)XmlTokenizedType.ENTITIES && (int)XmlTokenizedType.ENTITIES == 5 );
            Debug.Assert( (int)Token.NMTOKEN  == (int)XmlTokenizedType.NMTOKEN  && (int)XmlTokenizedType.NMTOKEN  == 6 );
            Debug.Assert( (int)Token.NMTOKENS == (int)XmlTokenizedType.NMTOKENS && (int)XmlTokenizedType.NMTOKENS == 7 );
            Debug.Assert( (int)Token.NOTATION == (int)XmlTokenizedType.NOTATION && (int)XmlTokenizedType.NOTATION == 8 );
#endif
        }

        private DtdParser()
        {
        }

        internal static IDtdParser Create()
        {
            return new DtdParser();
        }

        //
        // Initialization methods
        //

        private void Initialize(IDtdParserAdapter readerAdapter)
        {
            Debug.Assert(readerAdapter != null);
            _readerAdapter = readerAdapter;
#if !SILVERLIGHT
            _readerAdapterWithValidation = readerAdapter as IDtdParserAdapterWithValidation;
#endif

            _nameTable = readerAdapter.NameTable;

#if !SILVERLIGHT
            IDtdParserAdapterWithValidation raWithValidation = readerAdapter as IDtdParserAdapterWithValidation;
            if (raWithValidation != null)
            {
                _validate = raWithValidation.DtdValidation;
            }
            IDtdParserAdapterV1 raV1 = readerAdapter as IDtdParserAdapterV1;
            if (raV1 != null)
            {
                _v1Compat = raV1.V1CompatibilityMode;
                _normalize = raV1.Normalization;
                _supportNamespaces = raV1.Namespaces;
            }
#endif

            _schemaInfo = new SchemaInfo();
#if !SILVERLIGHT
            _schemaInfo.SchemaType = SchemaType.DTD;
#endif

            _stringBuilder = new StringBuilder();

            Uri baseUri = readerAdapter.BaseUri;
            if (baseUri != null)
            {
                _documentBaseUri = baseUri.ToString();
            }

            _freeFloatingDtd = false;
        }

        private void InitializeFreeFloatingDtd(string baseUri, string docTypeName, string publicId, string systemId, string internalSubset, IDtdParserAdapter adapter)
        {
            Initialize(adapter);

            if (docTypeName == null || docTypeName.Length == 0)
            {
                throw XmlConvert.CreateInvalidNameArgumentException(docTypeName, nameof(docTypeName));
            }

            // check doctype name
            XmlConvert.VerifyName(docTypeName);
            int colonPos = docTypeName.IndexOf(':');
            if (colonPos == -1)
            {
                _schemaInfo.DocTypeName = new XmlQualifiedName(_nameTable.Add(docTypeName));
            }
            else
            {
                _schemaInfo.DocTypeName = new XmlQualifiedName(_nameTable.Add(docTypeName.Substring(0, colonPos)),
                                                               _nameTable.Add(docTypeName.Substring(colonPos + 1)));
            }

            int i;
            // check system id
            if (systemId != null && systemId.Length > 0)
            {
                if ((i = _xmlCharType.IsOnlyCharData(systemId)) >= 0)
                {
                    ThrowInvalidChar(_curPos, systemId, i);
                }
                _systemId = systemId;
            }

            // check public id
            if (publicId != null && publicId.Length > 0)
            {
                if ((i = _xmlCharType.IsPublicId(publicId)) >= 0)
                {
                    ThrowInvalidChar(_curPos, publicId, i);
                }
                _publicId = publicId;
            }

            // init free-floating internal subset
            if (internalSubset != null && internalSubset.Length > 0)
            {
                _readerAdapter.PushInternalDtd(baseUri, internalSubset);
                _hasFreeFloatingInternalSubset = true;
            }

            Uri baseUriOb = _readerAdapter.BaseUri;
            if (baseUriOb != null)
            {
                _documentBaseUri = baseUriOb.ToString();
            }

            _freeFloatingDtd = true;
        }

        //
        // IDtdParser interface
        //
        #region IDtdParser Members

        IDtdInfo IDtdParser.ParseInternalDtd(IDtdParserAdapter adapter, bool saveInternalSubset)
        {
            Initialize(adapter);
            Parse(saveInternalSubset);
            return _schemaInfo;
        }

        IDtdInfo IDtdParser.ParseFreeFloatingDtd(string baseUri, string docTypeName, string publicId, string systemId, string internalSubset, IDtdParserAdapter adapter)
        {
            InitializeFreeFloatingDtd(baseUri, docTypeName, publicId, systemId, internalSubset, adapter);
            Parse(false);
            return _schemaInfo;
        }
        #endregion

        //
        // Private properties
        //
        private bool ParsingInternalSubset
        {
            get
            {
                return _externalEntitiesDepth == 0;
            }
        }

        private bool IgnoreEntityReferences
        {
            get
            {
                return _scanningFunction == ScanningFunction.CondSection3;
            }
        }

        private bool SaveInternalSubsetValue
        {
            get
            {
                return _readerAdapter.EntityStackLength == 0 && _internalSubsetValueSb != null;
            }
        }

        private bool ParsingTopLevelMarkup
        {
            get
            {
                return _scanningFunction == ScanningFunction.SubsetContent ||
                    (_scanningFunction == ScanningFunction.ParamEntitySpace && _savedScanningFunction == ScanningFunction.SubsetContent);
            }
        }

        private bool SupportNamespaces
        {
            get
            {
#if SILVERLIGHT
            return true;
#else
                return _supportNamespaces;
#endif
            }
        }

        private bool Normalize
        {
            get
            {
#if SILVERLIGHT
            return true;
#else
                return _normalize;
#endif
            }
        }

        //
        // Parsing methods
        //

        private void Parse(bool saveInternalSubset)
        {
            if (_freeFloatingDtd)
            {
                ParseFreeFloatingDtd();
            }
            else
            {
                ParseInDocumentDtd(saveInternalSubset);
            }

            _schemaInfo.Finish();

#if !SILVERLIGHT
            // check undeclared forward references
            if (_validate && _undeclaredNotations != null)
            {
                foreach (UndeclaredNotation un in _undeclaredNotations.Values)
                {
                    UndeclaredNotation tmpUn = un;
                    while (tmpUn != null)
                    {
                        SendValidationEvent(XmlSeverityType.Error, new XmlSchemaException(SR.Sch_UndeclaredNotation, un.name, BaseUriStr, (int)un.lineNo, (int)un.linePos));
                        tmpUn = tmpUn.next;
                    }
                }
            }
#endif
        }

        private void ParseInDocumentDtd(bool saveInternalSubset)
        {
            LoadParsingBuffer();

            _scanningFunction = ScanningFunction.QName;
            _nextScaningFunction = ScanningFunction.Doctype1;

            // doctype name
            if (GetToken(false) != Token.QName)
            {
                OnUnexpectedError();
            }
            _schemaInfo.DocTypeName = GetNameQualified(true);

            // SYSTEM or PUBLIC id
            Token token = GetToken(false);
            if (token == Token.SYSTEM || token == Token.PUBLIC)
            {
                ParseExternalId(token, Token.DOCTYPE, out _publicId, out _systemId);

                token = GetToken(false);
            }

            switch (token)
            {
                case Token.LeftBracket:
                    if (saveInternalSubset)
                    {
                        SaveParsingBuffer(); // this will cause saving the internal subset right from the point after '['
                        _internalSubsetValueSb = new StringBuilder();
                    }
                    ParseInternalSubset();
                    break;
                case Token.GreaterThan:
                    break;
                default:
                    OnUnexpectedError();
                    break;
            }
            SaveParsingBuffer();

            if (_systemId != null && _systemId.Length > 0)
            {
                ParseExternalSubset();
            }
        }

        private void ParseFreeFloatingDtd()
        {
            if (_hasFreeFloatingInternalSubset)
            {
                LoadParsingBuffer();
                ParseInternalSubset();
                SaveParsingBuffer();
            }

            if (_systemId != null && _systemId.Length > 0)
            {
                ParseExternalSubset();
            }
        }

        private void ParseInternalSubset()
        {
            Debug.Assert(ParsingInternalSubset);
            ParseSubset();
        }

        private void ParseExternalSubset()
        {
            Debug.Assert(_externalEntitiesDepth == 0);

            // push external subset
            if (!_readerAdapter.PushExternalSubset(_systemId, _publicId))
            {
                return;
            }

            Uri baseUri = _readerAdapter.BaseUri;
            if (baseUri != null)
            {
                _externalDtdBaseUri = baseUri.ToString();
            }

            _externalEntitiesDepth++;
            LoadParsingBuffer();

            // parse
            ParseSubset();

#if DEBUG
            Debug.Assert( readerAdapter.EntityStackLength == 0 ||
                         ( freeFloatingDtd && readerAdapter.EntityStackLength == 1 ) );
#endif
        }

        private void ParseSubset()
        {
            int startTagEntityId;
            for (;;)
            {
                Token token = GetToken(false);
                startTagEntityId = _currentEntityId;
                switch (token)
                {
                    case Token.AttlistDecl:
                        ParseAttlistDecl();
                        break;

                    case Token.ElementDecl:
                        ParseElementDecl();
                        break;

                    case Token.EntityDecl:
                        ParseEntityDecl();
                        break;

                    case Token.NotationDecl:
                        ParseNotationDecl();
                        break;

                    case Token.Comment:
                        ParseComment();
                        break;

                    case Token.PI:
                        ParsePI();
                        break;

                    case Token.CondSectionStart:
                        if (ParsingInternalSubset)
                        {
                            Throw(_curPos - 3, SR.Xml_InvalidConditionalSection); // 3==strlen("<![")
                        }
                        ParseCondSection();
                        startTagEntityId = _currentEntityId;
                        break;
                    case Token.CondSectionEnd:
                        if (_condSectionDepth > 0)
                        {
                            _condSectionDepth--;
#if !SILVERLIGHT
                            if (_validate && _currentEntityId != _condSectionEntityIds[_condSectionDepth])
                            {
                                SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                            }
#endif
                        }
                        else
                        {
                            Throw(_curPos - 3, SR.Xml_UnexpectedCDataEnd);
                        }
                        break;
                    case Token.RightBracket:
                        if (ParsingInternalSubset)
                        {
                            if (_condSectionDepth != 0)
                            {
                                Throw(_curPos, SR.Xml_UnclosedConditionalSection);
                            }
                            // append the rest to internal subset value but not the closing ']'
                            if (_internalSubsetValueSb != null)
                            {
                                Debug.Assert(_curPos > 0 && _chars[_curPos - 1] == ']');
                                SaveParsingBuffer(_curPos - 1);
                                _schemaInfo.InternalDtdSubset = _internalSubsetValueSb.ToString();
                                _internalSubsetValueSb = null;
                            }
                            // check '>'
                            if (GetToken(false) != Token.GreaterThan)
                            {
                                ThrowUnexpectedToken(_curPos, ">");
                            }
#if DEBUG
                            // check entity nesting
                            Debug.Assert( readerAdapter.EntityStackLength == 0 || 
                                          ( freeFloatingDtd && readerAdapter.EntityStackLength == 1 ) );
#endif
                        }
                        else
                        {
                            Throw(_curPos, SR.Xml_ExpectDtdMarkup);
                        }
                        return;
                    case Token.Eof:
                        if (ParsingInternalSubset && !_freeFloatingDtd)
                        {
                            Throw(_curPos, SR.Xml_IncompleteDtdContent);
                        }
                        if (_condSectionDepth != 0)
                        {
                            Throw(_curPos, SR.Xml_UnclosedConditionalSection);
                        }
                        return;
                    default:
                        Debug.Assert(false);
                        break;
                }

                Debug.Assert(_scanningFunction == ScanningFunction.SubsetContent);

                if (_currentEntityId != startTagEntityId)
                {
#if SILVERLIGHT
                    Throw(curPos, SR.Sch_ParEntityRefNesting);
#else
                    if (_validate)
                    {
                        SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                    }
                    else
                    {
                        if (!_v1Compat)
                        {
                            Throw(_curPos, SR.Sch_ParEntityRefNesting);
                        }
                    }
#endif
                }
            }
        }

        private void ParseAttlistDecl()
        {
            if (GetToken(true) != Token.QName)
            {
                goto UnexpectedError;
            }

            // element name
            XmlQualifiedName elementName = GetNameQualified(true);
            SchemaElementDecl elementDecl;
            if (!_schemaInfo.ElementDecls.TryGetValue(elementName, out elementDecl))
            {
                if (!_schemaInfo.UndeclaredElementDecls.TryGetValue(elementName, out elementDecl))
                {
                    elementDecl = new SchemaElementDecl(elementName, elementName.Namespace);
                    _schemaInfo.UndeclaredElementDecls.Add(elementName, elementDecl);
                }
            }

            SchemaAttDef attrDef = null;
            for (;;)
            {
                switch (GetToken(false))
                {
                    case Token.QName:
                        XmlQualifiedName attrName = GetNameQualified(true);
                        attrDef = new SchemaAttDef(attrName, attrName.Namespace);
                        attrDef.IsDeclaredInExternal = !ParsingInternalSubset;
                        attrDef.LineNumber = (int)LineNo;
                        attrDef.LinePosition = (int)LinePos - (_curPos - _tokenStartPos);
                        break;
                    case Token.GreaterThan:
#if !SILVERLIGHT
                        if (_v1Compat)
                        {
                            // check xml:space and xml:lang
                            // BUG BUG: For backward compatibility, we check the correct type and values of the
                            // xml:space attribute only on the last attribute in the list.
                            // See Webdata bugs #97457 and #93935.
                            if (attrDef != null && attrDef.Prefix.Length > 0 && attrDef.Prefix.Equals("xml") && attrDef.Name.Name == "space")
                            {
                                attrDef.Reserved = SchemaAttDef.Reserve.XmlSpace;
                                if (attrDef.Datatype.TokenizedType != XmlTokenizedType.ENUMERATION)
                                {
                                    Throw(SR.Xml_EnumerationRequired, string.Empty, attrDef.LineNumber, attrDef.LinePosition);
                                }
                                if (_validate)
                                {
                                    attrDef.CheckXmlSpace(_readerAdapterWithValidation.ValidationEventHandling);
                                }
                            }
                        }
#endif
                        return;
                    default:
                        goto UnexpectedError;
                }

                bool attrDefAlreadyExists = (elementDecl.GetAttDef(attrDef.Name) != null);

                ParseAttlistType(attrDef, elementDecl, attrDefAlreadyExists);
                ParseAttlistDefault(attrDef, attrDefAlreadyExists);

                // check xml:space and xml:lang
                if (attrDef.Prefix.Length > 0 && attrDef.Prefix.Equals("xml"))
                {
                    if (attrDef.Name.Name == "space")
                    {
#if !SILVERLIGHT
                        if (_v1Compat)
                        {
                            // BUG BUG: For backward compatibility, we check the correct type and values of the
                            // xml:space attribute only on the last attribute in the list, and mark it as reserved 
                            // only its value is correct (=prevent XmlTextReader from fhrowing on invalid value). 
                            // See Webdata bugs #98168, #97457 and #93935.
                            string val = attrDef.DefaultValueExpanded.Trim();
                            if (val.Equals("preserve") || val.Equals("default"))
                            {
                                attrDef.Reserved = SchemaAttDef.Reserve.XmlSpace;
                            }
                        }
                        else
                        {
#endif
                            attrDef.Reserved = SchemaAttDef.Reserve.XmlSpace;
                            if (attrDef.TokenizedType != XmlTokenizedType.ENUMERATION)
                            {
                                Throw(SR.Xml_EnumerationRequired, string.Empty, attrDef.LineNumber, attrDef.LinePosition);
                            }
#if !SILVERLIGHT
                            if (_validate)
                            {
                                attrDef.CheckXmlSpace(_readerAdapterWithValidation.ValidationEventHandling);
                            }
                        }
#endif
                    }
                    else if (attrDef.Name.Name == "lang")
                    {
                        attrDef.Reserved = SchemaAttDef.Reserve.XmlLang;
                    }
                }

                // add attribute to element decl
                if (!attrDefAlreadyExists)
                {
                    elementDecl.AddAttDef(attrDef);
                }
            }

        UnexpectedError:
            OnUnexpectedError();
        }

        private void ParseAttlistType(SchemaAttDef attrDef, SchemaElementDecl elementDecl, bool ignoreErrors)
        {
            Token token = GetToken(true);

            if (token != Token.CDATA)
            {
                elementDecl.HasNonCDataAttribute = true;
            }

            if (IsAttributeValueType(token))
            {
                attrDef.TokenizedType = (XmlTokenizedType)(int)token;
#if !SILVERLIGHT
                attrDef.SchemaType = XmlSchemaType.GetBuiltInSimpleType(attrDef.Datatype.TypeCode);
#endif

                switch (token)
                {
                    case Token.NOTATION:
                        break;
                    case Token.ID:
#if !SILVERLIGHT
                        if (_validate && elementDecl.IsIdDeclared)
                        {
                            SchemaAttDef idAttrDef = elementDecl.GetAttDef(attrDef.Name);
                            if ((idAttrDef == null || idAttrDef.Datatype.TokenizedType != XmlTokenizedType.ID) && !ignoreErrors)
                            {
                                SendValidationEvent(XmlSeverityType.Error, SR.Sch_IdAttrDeclared, elementDecl.Name.ToString());
                            }
                        }
#endif
                        elementDecl.IsIdDeclared = true;
                        return;
                    default:
                        return;
                }
#if !SILVERLIGHT
                // check notation constrains
                if (_validate)
                {
                    if (elementDecl.IsNotationDeclared && !ignoreErrors)
                    {
                        SendValidationEvent(_curPos - 8, XmlSeverityType.Error, SR.Sch_DupNotationAttribute, elementDecl.Name.ToString()); // 8 == strlen("NOTATION")
                    }
                    else
                    {
                        if (elementDecl.ContentValidator != null &&
                            elementDecl.ContentValidator.ContentType == XmlSchemaContentType.Empty &&
                            !ignoreErrors)
                        {
                            SendValidationEvent(_curPos - 8, XmlSeverityType.Error, SR.Sch_NotationAttributeOnEmptyElement, elementDecl.Name.ToString());// 8 == strlen("NOTATION")
                        }
                        elementDecl.IsNotationDeclared = true;
                    }
                }
#endif

                if (GetToken(true) != Token.LeftParen)
                {
                    goto UnexpectedError;
                }

                // parse notation list
                if (GetToken(false) != Token.Name)
                {
                    goto UnexpectedError;
                }
                for (;;)
                {
                    string notationName = GetNameString();
#if !SILVERLIGHT
                    if (!_schemaInfo.Notations.ContainsKey(notationName))
                    {
                        AddUndeclaredNotation(notationName);
                    }
                    if (_validate && !_v1Compat && attrDef.Values != null && attrDef.Values.Contains(notationName) && !ignoreErrors)
                    {
                        SendValidationEvent(XmlSeverityType.Error, new XmlSchemaException(SR.Xml_AttlistDuplNotationValue, notationName, BaseUriStr, (int)LineNo, (int)LinePos));
                    }
                    attrDef.AddValue(notationName);
#endif

                    switch (GetToken(false))
                    {
                        case Token.Or:
                            if (GetToken(false) != Token.Name)
                            {
                                goto UnexpectedError;
                            }
                            continue;
                        case Token.RightParen:
                            return;
                        default:
                            goto UnexpectedError;
                    }
                }
            }
            else if (token == Token.LeftParen)
            {
                attrDef.TokenizedType = XmlTokenizedType.ENUMERATION;
#if !SILVERLIGHT
                attrDef.SchemaType = XmlSchemaType.GetBuiltInSimpleType(attrDef.Datatype.TypeCode);
#endif

                // parse nmtoken list
                if (GetToken(false) != Token.Nmtoken)
                    goto UnexpectedError;
#if !SILVERLIGHT
                attrDef.AddValue(GetNameString());
#endif

                for (;;)
                {
                    switch (GetToken(false))
                    {
                        case Token.Or:
                            if (GetToken(false) != Token.Nmtoken)
                                goto UnexpectedError;
                            string nmtoken = GetNmtokenString();
#if !SILVERLIGHT
                            if (_validate && !_v1Compat && attrDef.Values != null && attrDef.Values.Contains(nmtoken) && !ignoreErrors)
                            {
                                SendValidationEvent(XmlSeverityType.Error, new XmlSchemaException(SR.Xml_AttlistDuplEnumValue, nmtoken, BaseUriStr, (int)LineNo, (int)LinePos));
                            }
                            attrDef.AddValue(nmtoken);
#endif
                            break;
                        case Token.RightParen:
                            return;
                        default:
                            goto UnexpectedError;
                    }
                }
            }
            else
            {
                goto UnexpectedError;
            }

        UnexpectedError:
            OnUnexpectedError();
        }

        private void ParseAttlistDefault(SchemaAttDef attrDef, bool ignoreErrors)
        {
            switch (GetToken(true))
            {
                case Token.REQUIRED:
                    attrDef.Presence = SchemaDeclBase.Use.Required;
                    return;
                case Token.IMPLIED:
                    attrDef.Presence = SchemaDeclBase.Use.Implied;
                    return;
                case Token.FIXED:
                    attrDef.Presence = SchemaDeclBase.Use.Fixed;
                    if (GetToken(true) != Token.Literal)
                    {
                        goto UnexpectedError;
                    }
                    break;
                case Token.Literal:
                    break;
                default:
                    goto UnexpectedError;
            }

#if !SILVERLIGHT
            if (_validate && attrDef.Datatype.TokenizedType == XmlTokenizedType.ID && !ignoreErrors)
            {
                SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_AttListPresence, string.Empty);
            }
#endif

            if (attrDef.TokenizedType != XmlTokenizedType.CDATA)
            {
                // non-CDATA attribute type normalization - strip spaces
                attrDef.DefaultValueExpanded = GetValueWithStrippedSpaces();
            }
            else
            {
                attrDef.DefaultValueExpanded = GetValue();
            }
            attrDef.ValueLineNumber = (int)_literalLineInfo.lineNo;
            attrDef.ValueLinePosition = (int)_literalLineInfo.linePos + 1;

#if !SILVERLIGHT
            DtdValidator.SetDefaultTypedValue(attrDef, _readerAdapter);
#endif
            return;

        UnexpectedError:
            OnUnexpectedError();
        }

        private void ParseElementDecl()
        {
            // element name
            if (GetToken(true) != Token.QName)
            {
                goto UnexpectedError;
            }

            // get schema decl for element
            SchemaElementDecl elementDecl = null;
            XmlQualifiedName name = GetNameQualified(true);

            if (_schemaInfo.ElementDecls.TryGetValue(name, out elementDecl))
            {
#if !SILVERLIGHT
                if (_validate)
                {
                    SendValidationEvent(_curPos - name.Name.Length, XmlSeverityType.Error, SR.Sch_DupElementDecl, GetNameString());
                }
#endif
            }
            else
            {
                if (_schemaInfo.UndeclaredElementDecls.TryGetValue(name, out elementDecl))
                {
                    _schemaInfo.UndeclaredElementDecls.Remove(name);
                }
                else
                {
                    elementDecl = new SchemaElementDecl(name, name.Namespace);
                }
                _schemaInfo.ElementDecls.Add(name, elementDecl);
            }
            elementDecl.IsDeclaredInExternal = !ParsingInternalSubset;

            // content spec
#if SILVERLIGHT
            switch ( GetToken( true ) ) {
                case Token.EMPTY:
                case Token.ANY:
                    break;
                case Token.LeftParen:
                    switch ( GetToken( false ) ) {
                        case Token.PCDATA:
                            ParseElementMixedContentNoValidation();
                            break;
                        case Token.None:
                            ParseElementOnlyContentNoValidation();
                            break;
                        default:
                            goto UnexpectedError;
                    }
                    break;
                default:
                    goto UnexpectedError;
            }
#else
            switch (GetToken(true))
            {
                case Token.EMPTY:
                    elementDecl.ContentValidator = ContentValidator.Empty;
                    break;
                case Token.ANY:
                    elementDecl.ContentValidator = ContentValidator.Any;
                    break;
                case Token.LeftParen:
                    int startParenEntityId = _currentEntityId;
                    switch (GetToken(false))
                    {
                        case Token.PCDATA:
                            {
                                ParticleContentValidator pcv = new ParticleContentValidator(XmlSchemaContentType.Mixed);
                                pcv.Start();
                                pcv.OpenGroup();

                                ParseElementMixedContent(pcv, startParenEntityId);

                                elementDecl.ContentValidator = pcv.Finish(true);
                                break;
                            }
                        case Token.None:
                            {
                                ParticleContentValidator pcv = null;
                                pcv = new ParticleContentValidator(XmlSchemaContentType.ElementOnly);
                                pcv.Start();
                                pcv.OpenGroup();

                                ParseElementOnlyContent(pcv, startParenEntityId);

                                elementDecl.ContentValidator = pcv.Finish(true);
                                break;
                            }
                        default:
                            goto UnexpectedError;
                    }
                    break;
                default:
                    goto UnexpectedError;
            }
#endif
            if (GetToken(false) != Token.GreaterThan)
            {
                ThrowUnexpectedToken(_curPos, ">");
            }
            return;

        UnexpectedError:
            OnUnexpectedError();
        }

#if SILVERLIGHT // Element content model parsing methods without validation

        private class ParseElementOnlyContentNoValidation_LocalFrame
        {
            public ParseElementOnlyContentNoValidation_LocalFrame() {
                parsingSchema = Token.None;
            }

            public Token parsingSchema;
        }

        private void ParseElementOnlyContentNoValidation() {
            Stack<ParseElementOnlyContentNoValidation_LocalFrame> localFrames = 
                new Stack<ParseElementOnlyContentNoValidation_LocalFrame>();
            ParseElementOnlyContentNoValidation_LocalFrame currentFrame =
                new ParseElementOnlyContentNoValidation_LocalFrame();
            localFrames.Push(currentFrame);

        RecursiveCall:
            
        Loop:

            switch ( GetToken( false ) ) {
                case Token.QName:
                    GetNameQualified(true);
                    ParseHowManyNoValidation();
                    break;
                case Token.LeftParen:
                    // We could just do this:
                    // ParseElementOnlyContentNoValidation();
                    //
                    // But that would be a recursion - so we will simulate the call using our localFrames stack
                    //   instead.
                    currentFrame =
                        new ParseElementOnlyContentNoValidation_LocalFrame();
                    localFrames.Push(currentFrame);
                    goto RecursiveCall;
                    // And we should return here when we return from the recursion
                    //   but it's the samea s returning after the switch statement

                case Token.GreaterThan:
                    Throw( curPos, SR.Xml_InvalidContentModel );
                    goto Return;
                default:
                    goto UnexpectedError;
            }

        ReturnFromRecursiveCall:

            switch ( GetToken( false ) ) {
                case Token.Comma:
                    if ( currentFrame.parsingSchema == Token.Or ) {
                        Throw( curPos, SR.Xml_InvalidContentModel );
                    }
                    currentFrame.parsingSchema = Token.Comma;
                    break;
                case Token.Or:
                    if ( currentFrame.parsingSchema == Token.Comma ) {
                        Throw( curPos, SR.Xml_InvalidContentModel );
                    }
                    currentFrame.parsingSchema = Token.Or;
                    break;
                case Token.RightParen:
                    ParseHowManyNoValidation();
                    goto Return;
                case Token.GreaterThan:
                    Throw( curPos, SR.Xml_InvalidContentModel );
                    goto Return;
                default:
                    goto UnexpectedError;
            }
            goto Loop;

        UnexpectedError:
            OnUnexpectedError();

        Return:
            // This is equivalent to return; statement
            //   we simulate it using our localFrames stack
            localFrames.Pop();
            if (localFrames.Count > 0) {
                currentFrame = localFrames.Peek();
                goto ReturnFromRecursiveCall;
            }
            else {
                return;
            }
        }

        private void ParseHowManyNoValidation() {
            GetToken( false );
        }

        private void ParseElementMixedContentNoValidation() {
            bool hasNames = false;

            for (;;) {
                switch ( GetToken( false ) ) {
                    case Token.RightParen:
                        if ( GetToken( false ) != Token.Star && hasNames ) {
                            ThrowUnexpectedToken( curPos, "*" );
                        }
                        return;
                    case Token.Or:
                        if ( !hasNames ) {
                            hasNames = true;
                        }
                        if ( GetToken( false ) != Token.QName ) {
                            goto default;
                        }
                        GetNameQualified( true );
                        continue;
                    default:
                        OnUnexpectedError();
                        break;
                }
            }
        }

#else // Element content model parsing methods with validation support

        private class ParseElementOnlyContent_LocalFrame
        {
            public ParseElementOnlyContent_LocalFrame(int startParentEntityIdParam)
            {
                startParenEntityId = startParentEntityIdParam;
                parsingSchema = Token.None;
            }

            // pcv doesn't need to be stored for each frame as it never changes
            public int startParenEntityId;
            public Token parsingSchema;
        }

        private void ParseElementOnlyContent(ParticleContentValidator pcv, int startParenEntityId)
        {
            Stack<ParseElementOnlyContent_LocalFrame> localFrames = new Stack<ParseElementOnlyContent_LocalFrame>();
            ParseElementOnlyContent_LocalFrame currentFrame = new ParseElementOnlyContent_LocalFrame(startParenEntityId);
            localFrames.Push(currentFrame);

        RecursiveCall:

        Loop:
            switch (GetToken(false))
            {
                case Token.QName:
                    pcv.AddName(GetNameQualified(true), null);
                    ParseHowMany(pcv);
                    break;
                case Token.LeftParen:
                    pcv.OpenGroup();

                    // We could just do this:
                    // ParseElementOnlyContent( pcv, currentEntityId );
                    // 
                    // But that would be recursion - so we will simulate the call using our localFrames stack 
                    //   instead. 
                    currentFrame =
                        new ParseElementOnlyContent_LocalFrame(_currentEntityId);
                    localFrames.Push(currentFrame);
                    goto RecursiveCall;
                // And we should return here when we return from recursion call 
                //   but it's the same as returning after the switch statement 

                case Token.GreaterThan:
                    Throw(_curPos, SR.Xml_InvalidContentModel);
                    goto Return;
                default:
                    goto UnexpectedError;
            }

        ReturnFromRecursiveCall:
            switch (GetToken(false))
            {
                case Token.Comma:
                    if (currentFrame.parsingSchema == Token.Or)
                    {
                        Throw(_curPos, SR.Xml_InvalidContentModel);
                    }
                    pcv.AddSequence();
                    currentFrame.parsingSchema = Token.Comma;
                    break;
                case Token.Or:
                    if (currentFrame.parsingSchema == Token.Comma)
                    {
                        Throw(_curPos, SR.Xml_InvalidContentModel);
                    }
                    pcv.AddChoice();
                    currentFrame.parsingSchema = Token.Or;
                    break;
                case Token.RightParen:
                    pcv.CloseGroup();
                    if (_validate && _currentEntityId != currentFrame.startParenEntityId)
                    {
                        SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                    }
                    ParseHowMany(pcv);
                    goto Return;
                case Token.GreaterThan:
                    Throw(_curPos, SR.Xml_InvalidContentModel);
                    goto Return;
                default:
                    goto UnexpectedError;
            }
            goto Loop;

        UnexpectedError:
            OnUnexpectedError();

        Return:
            // This is equivalent to return; statement
            //   we simlate it using our localFrames stack
            localFrames.Pop();
            if (localFrames.Count > 0)
            {
                currentFrame = (ParseElementOnlyContent_LocalFrame)localFrames.Peek();
                goto ReturnFromRecursiveCall;
            }
            else
            {
                return;
            }
        }

        private void ParseHowMany(ParticleContentValidator pcv)
        {
            switch (GetToken(false))
            {
                case Token.Star:
                    pcv.AddStar();
                    return;
                case Token.QMark:
                    pcv.AddQMark();
                    return;
                case Token.Plus:
                    pcv.AddPlus();
                    return;
                default:
                    return;
            }
        }

        private void ParseElementMixedContent(ParticleContentValidator pcv, int startParenEntityId)
        {
            bool hasNames = false;
            int connectorEntityId = -1;
            int contentEntityId = _currentEntityId;

            for (;;)
            {
                switch (GetToken(false))
                {
                    case Token.RightParen:
                        pcv.CloseGroup();
                        if (_validate && _currentEntityId != startParenEntityId)
                        {
                            SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                        }
                        if (GetToken(false) == Token.Star && hasNames)
                        {
                            pcv.AddStar();
                        }
                        else if (hasNames)
                        {
                            ThrowUnexpectedToken(_curPos, "*");
                        }
                        return;
                    case Token.Or:
                        if (!hasNames)
                        {
                            hasNames = true;
                        }
                        else
                        {
                            pcv.AddChoice();
                        }
                        if (_validate)
                        {
                            connectorEntityId = _currentEntityId;
                            if (contentEntityId < connectorEntityId)
                            {  // entity repl.text starting with connector
                                SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                            }
                        }

                        if (GetToken(false) != Token.QName)
                        {
                            goto default;
                        }

                        XmlQualifiedName name = GetNameQualified(true);
                        if (pcv.Exists(name) && _validate)
                        {
                            SendValidationEvent(XmlSeverityType.Error, SR.Sch_DupElement, name.ToString());
                        }
                        pcv.AddName(name, null);

                        if (_validate)
                        {
                            contentEntityId = _currentEntityId;
                            if (contentEntityId < connectorEntityId)
                            { // entity repl.text ending with connector
                                SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                            }
                        }
                        continue;
                    default:
                        OnUnexpectedError();
                        break;
                }
            }
        }
#endif // Element content model parsing methods with validation support

        private void ParseEntityDecl()
        {
            bool isParamEntity = false;
            SchemaEntity entity = null;

            // get entity name and type
            switch (GetToken(true))
            {
                case Token.Percent:
                    isParamEntity = true;
                    if (GetToken(true) != Token.Name)
                    {
                        goto UnexpectedError;
                    }
                    goto case Token.Name;
                case Token.Name:
                    // create entity object
                    XmlQualifiedName entityName = GetNameQualified(false);
                    entity = new SchemaEntity(entityName, isParamEntity);

                    entity.BaseURI = BaseUriStr;
                    entity.DeclaredURI = (_externalDtdBaseUri.Length == 0) ? _documentBaseUri : _externalDtdBaseUri;

                    if (isParamEntity)
                    {
                        if (!_schemaInfo.ParameterEntities.ContainsKey(entityName))
                        {
                            _schemaInfo.ParameterEntities.Add(entityName, entity);
                        }
                    }
                    else
                    {
                        if (!_schemaInfo.GeneralEntities.ContainsKey(entityName))
                        {
                            _schemaInfo.GeneralEntities.Add(entityName, entity);
                        }
                    }
                    entity.DeclaredInExternal = !ParsingInternalSubset;
                    entity.ParsingInProgress = true;
                    break;
                default:
                    goto UnexpectedError;
            }

            Token token = GetToken(true);
            switch (token)
            {
                case Token.PUBLIC:
                case Token.SYSTEM:
                    string systemId;
                    string publicId;

                    ParseExternalId(token, Token.EntityDecl, out publicId, out systemId);

                    entity.IsExternal = true;
                    entity.Url = systemId;
                    entity.Pubid = publicId;

                    if (GetToken(false) == Token.NData)
                    {
                        if (isParamEntity)
                        {
                            ThrowUnexpectedToken(_curPos - 5, ">"); // 5 == strlen("NDATA")
                        }
                        if (!_whitespaceSeen)
                        {
                            Throw(_curPos - 5, SR.Xml_ExpectingWhiteSpace, "NDATA");
                        }

                        if (GetToken(true) != Token.Name)
                        {
                            goto UnexpectedError;
                        }

                        entity.NData = GetNameQualified(false);
#if !SILVERLIGHT
                        string notationName = entity.NData.Name;
                        if (!_schemaInfo.Notations.ContainsKey(notationName))
                        {
                            AddUndeclaredNotation(notationName);
                        }
#endif
                    }
                    break;
                case Token.Literal:
                    entity.Text = GetValue();
                    entity.Line = (int)_literalLineInfo.lineNo;
                    entity.Pos = (int)_literalLineInfo.linePos;
                    break;
                default:
                    goto UnexpectedError;
            }

            if (GetToken(false) == Token.GreaterThan)
            {
                entity.ParsingInProgress = false;
                return;
            }

        UnexpectedError:
            OnUnexpectedError();
        }

        private void ParseNotationDecl()
        {
            // notation name
            if (GetToken(true) != Token.Name)
            {
                OnUnexpectedError();
            }

            XmlQualifiedName notationName = GetNameQualified(false);
#if !SILVERLIGHT
            SchemaNotation notation = null;
            if (!_schemaInfo.Notations.ContainsKey(notationName.Name))
            {
                if (_undeclaredNotations != null)
                {
                    _undeclaredNotations.Remove(notationName.Name);
                }
                notation = new SchemaNotation(notationName);
                _schemaInfo.Notations.Add(notation.Name.Name, notation);
            }
            else
            {
                // duplicate notation
                if (_validate)
                {
                    SendValidationEvent(_curPos - notationName.Name.Length, XmlSeverityType.Error, SR.Sch_DupNotation, notationName.Name);
                }
            }
#endif

            // public / system id
            Token token = GetToken(true);
            if (token == Token.SYSTEM || token == Token.PUBLIC)
            {
                string notationPublicId, notationSystemId;

                ParseExternalId(token, Token.NOTATION, out notationPublicId, out notationSystemId);

#if !SILVERLIGHT
                if (notation != null)
                {
                    notation.SystemLiteral = notationSystemId;
                    notation.Pubid = notationPublicId;
                }
#endif
            }
            else
            {
                OnUnexpectedError();
            }

            if (GetToken(false) != Token.GreaterThan)
                OnUnexpectedError();
        }

#if !SILVERLIGHT
        private void AddUndeclaredNotation(string notationName)
        {
            if (_undeclaredNotations == null)
            {
                _undeclaredNotations = new Dictionary<string, UndeclaredNotation>();
            }
            UndeclaredNotation un = new UndeclaredNotation(notationName, LineNo, LinePos - notationName.Length);
            UndeclaredNotation loggedUn;
            if (_undeclaredNotations.TryGetValue(notationName, out loggedUn))
            {
                un.next = loggedUn.next;
                loggedUn.next = un;
            }
            else
            {
                _undeclaredNotations.Add(notationName, un);
            }
        }
#endif

        private void ParseComment()
        {
            SaveParsingBuffer();
#if !SILVERLIGHT
            try
            {
#endif
                if (SaveInternalSubsetValue)
                {
                    _readerAdapter.ParseComment(_internalSubsetValueSb);
                    _internalSubsetValueSb.Append("-->");
                }
                else
                {
                    _readerAdapter.ParseComment(null);
                }
#if !SILVERLIGHT
            }
            catch (XmlException e)
            {
                if (e.ResString == SR.Xml_UnexpectedEOF && _currentEntityId != 0)
                {
                    SendValidationEvent(XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, null);
                }
                else
                {
                    throw;
                }
            }
#endif
            LoadParsingBuffer();
        }

        private void ParsePI()
        {
            SaveParsingBuffer();
            if (SaveInternalSubsetValue)
            {
                _readerAdapter.ParsePI(_internalSubsetValueSb);
                _internalSubsetValueSb.Append("?>");
            }
            else
            {
                _readerAdapter.ParsePI(null);
            }
            LoadParsingBuffer();
        }

        private void ParseCondSection()
        {
            int csEntityId = _currentEntityId;

            switch (GetToken(false))
            {
                case Token.INCLUDE:
                    if (GetToken(false) != Token.LeftBracket)
                    {
                        goto default;
                    }
#if !SILVERLIGHT
                    if (_validate && csEntityId != _currentEntityId)
                    {
                        SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                    }
                    if (_validate)
                    {
                        if (_condSectionEntityIds == null)
                        {
                            _condSectionEntityIds = new int[CondSectionEntityIdsInitialSize];
                        }
                        else if (_condSectionEntityIds.Length == _condSectionDepth)
                        {
                            int[] tmp = new int[_condSectionEntityIds.Length * 2];
                            Array.Copy(_condSectionEntityIds, 0, tmp, 0, _condSectionEntityIds.Length);
                            _condSectionEntityIds = tmp;
                        }
                        _condSectionEntityIds[_condSectionDepth] = csEntityId;
                    }
#endif
                    _condSectionDepth++;
                    break;
                case Token.IGNORE:
                    if (GetToken(false) != Token.LeftBracket)
                    {
                        goto default;
                    }
#if !SILVERLIGHT
                    if (_validate && csEntityId != _currentEntityId)
                    {
                        SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                    }
#endif
                    // the content of the ignore section is parsed & skipped by scanning function
                    if (GetToken(false) != Token.CondSectionEnd)
                    {
                        goto default;
                    }
#if !SILVERLIGHT
                    if (_validate && csEntityId != _currentEntityId)
                    {
                        SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                    }
#endif
                    break;
                default:
                    OnUnexpectedError();
                    break;
            }
        }

        private void ParseExternalId(Token idTokenType, Token declType, out string publicId, out string systemId)
        {
            LineInfo keywordLineInfo = new LineInfo(LineNo, LinePos - 6);
            publicId = null;
            systemId = null;

            if (GetToken(true) != Token.Literal)
            {
                ThrowUnexpectedToken(_curPos, "\"", "'");
            }

            if (idTokenType == Token.SYSTEM)
            {
                systemId = GetValue();

                if (systemId.IndexOf('#') >= 0)
                {
                    Throw(_curPos - systemId.Length - 1, SR.Xml_FragmentId, new string[] { systemId.Substring(systemId.IndexOf('#')), systemId });
                }

                if (declType == Token.DOCTYPE && !_freeFloatingDtd)
                {
                    _literalLineInfo.linePos++;
                    _readerAdapter.OnSystemId(systemId, keywordLineInfo, _literalLineInfo);
                }
            }
            else
            {
                Debug.Assert(idTokenType == Token.PUBLIC);
                publicId = GetValue();

                // verify if it contains chars valid for public ids
                int i;
                if ((i = _xmlCharType.IsPublicId(publicId)) >= 0)
                {
                    ThrowInvalidChar(_curPos - 1 - publicId.Length + i, publicId, i);
                }

                if (declType == Token.DOCTYPE && !_freeFloatingDtd)
                {
                    _literalLineInfo.linePos++;
                    _readerAdapter.OnPublicId(publicId, keywordLineInfo, _literalLineInfo);

                    if (GetToken(false) == Token.Literal)
                    {
                        if (!_whitespaceSeen)
                        {
                            Throw(SR.Xml_ExpectingWhiteSpace, new string(_literalQuoteChar, 1), (int)_literalLineInfo.lineNo, (int)_literalLineInfo.linePos);
                        }
                        systemId = GetValue();
                        _literalLineInfo.linePos++;
                        _readerAdapter.OnSystemId(systemId, keywordLineInfo, _literalLineInfo);
                    }
                    else
                    {
                        ThrowUnexpectedToken(_curPos, "\"", "'");
                    }
                }
                else
                {
                    if (GetToken(false) == Token.Literal)
                    {
                        if (!_whitespaceSeen)
                        {
                            Throw(SR.Xml_ExpectingWhiteSpace, new string(_literalQuoteChar, 1), (int)_literalLineInfo.lineNo, (int)_literalLineInfo.linePos);
                        }
                        systemId = GetValue();
                    }
                    else if (declType != Token.NOTATION)
                    {
                        ThrowUnexpectedToken(_curPos, "\"", "'");
                    }
                }
            }
        }
        //
        // Scanning methods - works directly with parsing buffer
        //
        private Token GetToken(bool needWhiteSpace)
        {
            _whitespaceSeen = false;
            for (;;)
            {
                switch (_chars[_curPos])
                {
                    case (char)0:
                        if (_curPos == _charsUsed)
                        {
                            goto ReadData;
                        }
                        else
                        {
                            ThrowInvalidChar(_chars, _charsUsed, _curPos);
                        }
                        break;
                    case (char)0xA:
                        _whitespaceSeen = true;
                        _curPos++;
                        _readerAdapter.OnNewLine(_curPos);
                        continue;
                    case (char)0xD:
                        _whitespaceSeen = true;
                        if (_chars[_curPos + 1] == (char)0xA)
                        {
                            if (Normalize)
                            {
                                SaveParsingBuffer();          // EOL normalization of 0xD 0xA
                                _readerAdapter.CurrentPosition++;
                            }
                            _curPos += 2;
                        }
                        else if (_curPos + 1 < _charsUsed || _readerAdapter.IsEof)
                        {
                            _chars[_curPos] = (char)0xA;             // EOL normalization of 0xD
                            _curPos++;
                        }
                        else
                        {
                            goto ReadData;
                        }
                        _readerAdapter.OnNewLine(_curPos);
                        continue;
                    case (char)0x9:
                    case (char)0x20:
                        _whitespaceSeen = true;
                        _curPos++;
                        continue;
                    case '%':
                        if (_charsUsed - _curPos < 2)
                        {
                            goto ReadData;
                        }
                        if (!_xmlCharType.IsWhiteSpace(_chars[_curPos + 1]))
                        {
                            if (IgnoreEntityReferences)
                            {
                                _curPos++;
                            }
                            else
                            {
                                HandleEntityReference(true, false, false);
                            }
                            continue;
                        }
                        goto default;
                    default:
                        if (needWhiteSpace && !_whitespaceSeen && _scanningFunction != ScanningFunction.ParamEntitySpace)
                        {
                            Throw(_curPos, SR.Xml_ExpectingWhiteSpace, ParseUnexpectedToken(_curPos));
                        }
                        _tokenStartPos = _curPos;
                    SwitchAgain:
                        switch (_scanningFunction)
                        {
                            case ScanningFunction.Name: return ScanNameExpected();
                            case ScanningFunction.QName: return ScanQNameExpected();
                            case ScanningFunction.Nmtoken: return ScanNmtokenExpected();
                            case ScanningFunction.SubsetContent: return ScanSubsetContent();
                            case ScanningFunction.Doctype1: return ScanDoctype1();
                            case ScanningFunction.Doctype2: return ScanDoctype2();
                            case ScanningFunction.Element1: return ScanElement1();
                            case ScanningFunction.Element2: return ScanElement2();
                            case ScanningFunction.Element3: return ScanElement3();
                            case ScanningFunction.Element4: return ScanElement4();
                            case ScanningFunction.Element5: return ScanElement5();
                            case ScanningFunction.Element6: return ScanElement6();
                            case ScanningFunction.Element7: return ScanElement7();
                            case ScanningFunction.Attlist1: return ScanAttlist1();
                            case ScanningFunction.Attlist2: return ScanAttlist2();
                            case ScanningFunction.Attlist3: return ScanAttlist3();
                            case ScanningFunction.Attlist4: return ScanAttlist4();
                            case ScanningFunction.Attlist5: return ScanAttlist5();
                            case ScanningFunction.Attlist6: return ScanAttlist6();
                            case ScanningFunction.Attlist7: return ScanAttlist7();
                            case ScanningFunction.Notation1: return ScanNotation1();
                            case ScanningFunction.SystemId: return ScanSystemId();
                            case ScanningFunction.PublicId1: return ScanPublicId1();
                            case ScanningFunction.PublicId2: return ScanPublicId2();
                            case ScanningFunction.Entity1: return ScanEntity1();
                            case ScanningFunction.Entity2: return ScanEntity2();
                            case ScanningFunction.Entity3: return ScanEntity3();
                            case ScanningFunction.CondSection1: return ScanCondSection1();
                            case ScanningFunction.CondSection2: return ScanCondSection2();
                            case ScanningFunction.CondSection3: return ScanCondSection3();
                            case ScanningFunction.ClosingTag: return ScanClosingTag();
                            case ScanningFunction.ParamEntitySpace:
                                _whitespaceSeen = true;
                                _scanningFunction = _savedScanningFunction;
                                goto SwitchAgain;
                            default:
                                Debug.Assert(false);
                                return Token.None;
                        }
                }
            ReadData:
                if (_readerAdapter.IsEof || ReadData() == 0)
                {
                    if (HandleEntityEnd(false))
                    {
                        continue;
                    }
                    if (_scanningFunction == ScanningFunction.SubsetContent)
                    {
                        return Token.Eof;
                    }
                    else
                    {
                        Throw(_curPos, SR.Xml_IncompleteDtdContent);
                    }
                }
            }
        }

        private Token ScanSubsetContent()
        {
            for (;;)
            {
                switch (_chars[_curPos])
                {
                    case '<':
                        switch (_chars[_curPos + 1])
                        {
                            case '!':
                                switch (_chars[_curPos + 2])
                                {
                                    case 'E':
                                        if (_chars[_curPos + 3] == 'L')
                                        {
                                            if (_charsUsed - _curPos < 9)
                                            {
                                                goto ReadData;
                                            }
                                            if (_chars[_curPos + 4] != 'E' || _chars[_curPos + 5] != 'M' ||
                                                 _chars[_curPos + 6] != 'E' || _chars[_curPos + 7] != 'N' ||
                                                 _chars[_curPos + 8] != 'T')
                                            {
                                                Throw(_curPos, SR.Xml_ExpectDtdMarkup);
                                            }
                                            _curPos += 9;
                                            _scanningFunction = ScanningFunction.QName;
                                            _nextScaningFunction = ScanningFunction.Element1;
                                            return Token.ElementDecl;
                                        }
                                        else if (_chars[_curPos + 3] == 'N')
                                        {
                                            if (_charsUsed - _curPos < 8)
                                            {
                                                goto ReadData;
                                            }
                                            if (_chars[_curPos + 4] != 'T' || _chars[_curPos + 5] != 'I' ||
                                                 _chars[_curPos + 6] != 'T' || _chars[_curPos + 7] != 'Y')
                                            {
                                                Throw(_curPos, SR.Xml_ExpectDtdMarkup);
                                            }
                                            _curPos += 8;
                                            _scanningFunction = ScanningFunction.Entity1;
                                            return Token.EntityDecl;
                                        }
                                        else
                                        {
                                            if (_charsUsed - _curPos < 4)
                                            {
                                                goto ReadData;
                                            }
                                            Throw(_curPos, SR.Xml_ExpectDtdMarkup);
                                            return Token.None;
                                        }

                                    case 'A':
                                        if (_charsUsed - _curPos < 9)
                                        {
                                            goto ReadData;
                                        }
                                        if (_chars[_curPos + 3] != 'T' || _chars[_curPos + 4] != 'T' ||
                                             _chars[_curPos + 5] != 'L' || _chars[_curPos + 6] != 'I' ||
                                             _chars[_curPos + 7] != 'S' || _chars[_curPos + 8] != 'T')
                                        {
                                            Throw(_curPos, SR.Xml_ExpectDtdMarkup);
                                        }
                                        _curPos += 9;
                                        _scanningFunction = ScanningFunction.QName;
                                        _nextScaningFunction = ScanningFunction.Attlist1;
                                        return Token.AttlistDecl;

                                    case 'N':
                                        if (_charsUsed - _curPos < 10)
                                        {
                                            goto ReadData;
                                        }
                                        if (_chars[_curPos + 3] != 'O' || _chars[_curPos + 4] != 'T' ||
                                             _chars[_curPos + 5] != 'A' || _chars[_curPos + 6] != 'T' ||
                                             _chars[_curPos + 7] != 'I' || _chars[_curPos + 8] != 'O' ||
                                             _chars[_curPos + 9] != 'N')
                                        {
                                            Throw(_curPos, SR.Xml_ExpectDtdMarkup);
                                        }
                                        _curPos += 10;
                                        _scanningFunction = ScanningFunction.Name;
                                        _nextScaningFunction = ScanningFunction.Notation1;
                                        return Token.NotationDecl;

                                    case '[':
                                        _curPos += 3;
                                        _scanningFunction = ScanningFunction.CondSection1;
                                        return Token.CondSectionStart;
                                    case '-':
                                        if (_chars[_curPos + 3] == '-')
                                        {
                                            _curPos += 4;
                                            return Token.Comment;
                                        }
                                        else
                                        {
                                            if (_charsUsed - _curPos < 4)
                                            {
                                                goto ReadData;
                                            }
                                            Throw(_curPos, SR.Xml_ExpectDtdMarkup);
                                            break;
                                        }
                                    default:
                                        if (_charsUsed - _curPos < 3)
                                        {
                                            goto ReadData;
                                        }
                                        Throw(_curPos + 2, SR.Xml_ExpectDtdMarkup);
                                        break;
                                }
                                break;
                            case '?':
                                _curPos += 2;
                                return Token.PI;
                            default:
                                if (_charsUsed - _curPos < 2)
                                {
                                    goto ReadData;
                                }
                                Throw(_curPos, SR.Xml_ExpectDtdMarkup);
                                return Token.None;
                        }
                        break;
                    case ']':
                        if (_charsUsed - _curPos < 2 && !_readerAdapter.IsEof)
                        {
                            goto ReadData;
                        }
                        if (_chars[_curPos + 1] != ']')
                        {
                            _curPos++;
                            _scanningFunction = ScanningFunction.ClosingTag;
                            return Token.RightBracket;
                        }
                        if (_charsUsed - _curPos < 3 && !_readerAdapter.IsEof)
                        {
                            goto ReadData;
                        }
                        if (_chars[_curPos + 1] == ']' && _chars[_curPos + 2] == '>')
                        {
                            _curPos += 3;
                            return Token.CondSectionEnd;
                        }
                        goto default;
                    default:
                        if (_charsUsed - _curPos == 0)
                        {
                            goto ReadData;
                        }
                        Throw(_curPos, SR.Xml_ExpectDtdMarkup);
                        break;
                }
            ReadData:
                if (ReadData() == 0)
                {
                    Throw(_charsUsed, SR.Xml_IncompleteDtdContent);
                }
            }
        }

        private Token ScanNameExpected()
        {
            ScanName();
            _scanningFunction = _nextScaningFunction;
            return Token.Name;
        }

        private Token ScanQNameExpected()
        {
            ScanQName();
            _scanningFunction = _nextScaningFunction;
            return Token.QName;
        }

        private Token ScanNmtokenExpected()
        {
            ScanNmtoken();
            _scanningFunction = _nextScaningFunction;
            return Token.Nmtoken;
        }

        private Token ScanDoctype1()
        {
            switch (_chars[_curPos])
            {
                case 'P':
                    if (!EatPublicKeyword())
                    {
                        Throw(_curPos, SR.Xml_ExpectExternalOrClose);
                    }
                    _nextScaningFunction = ScanningFunction.Doctype2;
                    _scanningFunction = ScanningFunction.PublicId1;
                    return Token.PUBLIC;
                case 'S':
                    if (!EatSystemKeyword())
                    {
                        Throw(_curPos, SR.Xml_ExpectExternalOrClose);
                    }
                    _nextScaningFunction = ScanningFunction.Doctype2;
                    _scanningFunction = ScanningFunction.SystemId;
                    return Token.SYSTEM;
                case '[':
                    _curPos++;
                    _scanningFunction = ScanningFunction.SubsetContent;
                    return Token.LeftBracket;
                case '>':
                    _curPos++;
                    _scanningFunction = ScanningFunction.SubsetContent;
                    return Token.GreaterThan;
                default:
                    Throw(_curPos, SR.Xml_ExpectExternalOrClose);
                    return Token.None;
            }
        }

        private Token ScanDoctype2()
        {
            switch (_chars[_curPos])
            {
                case '[':
                    _curPos++;
                    _scanningFunction = ScanningFunction.SubsetContent;
                    return Token.LeftBracket;
                case '>':
                    _curPos++;
                    _scanningFunction = ScanningFunction.SubsetContent;
                    return Token.GreaterThan;
                default:
                    Throw(_curPos, SR.Xml_ExpectSubOrClose);
                    return Token.None;
            }
        }

        private Token ScanClosingTag()
        {
            if (_chars[_curPos] != '>')
            {
                ThrowUnexpectedToken(_curPos, ">");
            }
            _curPos++;
            _scanningFunction = ScanningFunction.SubsetContent;
            return Token.GreaterThan;
        }

        private Token ScanElement1()
        {
            for (;;)
            {
                switch (_chars[_curPos])
                {
                    case '(':
                        _scanningFunction = ScanningFunction.Element2;
                        _curPos++;
                        return Token.LeftParen;
                    case 'E':
                        if (_charsUsed - _curPos < 5)
                        {
                            goto ReadData;
                        }
                        if (_chars[_curPos + 1] == 'M' && _chars[_curPos + 2] == 'P' &&
                             _chars[_curPos + 3] == 'T' && _chars[_curPos + 4] == 'Y')
                        {
                            _curPos += 5;
                            _scanningFunction = ScanningFunction.ClosingTag;
                            return Token.EMPTY;
                        }
                        goto default;
                    case 'A':
                        if (_charsUsed - _curPos < 3)
                        {
                            goto ReadData;
                        }
                        if (_chars[_curPos + 1] == 'N' && _chars[_curPos + 2] == 'Y')
                        {
                            _curPos += 3;
                            _scanningFunction = ScanningFunction.ClosingTag;
                            return Token.ANY;
                        }
                        goto default;
                    default:
                        Throw(_curPos, SR.Xml_InvalidContentModel);
                        break;
                }
            ReadData:
                if (ReadData() == 0)
                {
                    Throw(_curPos, SR.Xml_IncompleteDtdContent);
                }
            }
        }

        private Token ScanElement2()
        {
            if (_chars[_curPos] == '#')
            {
                while (_charsUsed - _curPos < 7)
                {
                    if (ReadData() == 0)
                    {
                        Throw(_curPos, SR.Xml_IncompleteDtdContent);
                    }
                }
                if (_chars[_curPos + 1] == 'P' && _chars[_curPos + 2] == 'C' &&
                     _chars[_curPos + 3] == 'D' && _chars[_curPos + 4] == 'A' &&
                     _chars[_curPos + 5] == 'T' && _chars[_curPos + 6] == 'A')
                {
                    _curPos += 7;
                    _scanningFunction = ScanningFunction.Element6;
                    return Token.PCDATA;
                }
                else
                {
                    Throw(_curPos + 1, SR.Xml_ExpectPcData);
                }
            }

            _scanningFunction = ScanningFunction.Element3;
            return Token.None;
        }

        private Token ScanElement3()
        {
            switch (_chars[_curPos])
            {
                case '(':
                    _curPos++;
                    return Token.LeftParen;
                case '>':
                    _curPos++;
                    _scanningFunction = ScanningFunction.SubsetContent;
                    return Token.GreaterThan;
                default:
                    ScanQName();
                    _scanningFunction = ScanningFunction.Element4;
                    return Token.QName;
            }
        }

        private Token ScanElement4()
        {
            _scanningFunction = ScanningFunction.Element5;

            Token t;
            switch (_chars[_curPos])
            {
                case '*':
                    t = Token.Star;
                    break;
                case '?':
                    t = Token.QMark;
                    break;
                case '+':
                    t = Token.Plus;
                    break;
                default:
                    return Token.None;
            }
            if (_whitespaceSeen)
            {
                Throw(_curPos, SR.Xml_ExpectNoWhitespace);
            }
            _curPos++;
            return t;
        }

        private Token ScanElement5()
        {
            switch (_chars[_curPos])
            {
                case ',':
                    _curPos++;
                    _scanningFunction = ScanningFunction.Element3;
                    return Token.Comma;
                case '|':
                    _curPos++;
                    _scanningFunction = ScanningFunction.Element3;
                    return Token.Or;
                case ')':
                    _curPos++;
                    _scanningFunction = ScanningFunction.Element4;
                    return Token.RightParen;
                case '>':
                    _curPos++;
                    _scanningFunction = ScanningFunction.SubsetContent;
                    return Token.GreaterThan;
                default:
                    Throw(_curPos, SR.Xml_ExpectOp);
                    return Token.None;
            }
        }

        private Token ScanElement6()
        {
            switch (_chars[_curPos])
            {
                case ')':
                    _curPos++;
                    _scanningFunction = ScanningFunction.Element7;
                    return Token.RightParen;
                case '|':
                    _curPos++;
                    _nextScaningFunction = ScanningFunction.Element6;
                    _scanningFunction = ScanningFunction.QName;
                    return Token.Or;
                default:
                    ThrowUnexpectedToken(_curPos, ")", "|");
                    return Token.None;
            }
        }

        private Token ScanElement7()
        {
            _scanningFunction = ScanningFunction.ClosingTag;
            if (_chars[_curPos] == '*' && !_whitespaceSeen)
            {
                _curPos++;
                return Token.Star;
            }
            return Token.None;
        }

        private Token ScanAttlist1()
        {
            switch (_chars[_curPos])
            {
                case '>':
                    _curPos++;
                    _scanningFunction = ScanningFunction.SubsetContent;
                    return Token.GreaterThan;
                default:
                    if (!_whitespaceSeen)
                    {
                        Throw(_curPos, SR.Xml_ExpectingWhiteSpace, ParseUnexpectedToken(_curPos));
                    }
                    ScanQName();
                    _scanningFunction = ScanningFunction.Attlist2;
                    return Token.QName;
            }
        }

        private Token ScanAttlist2()
        {
            for (;;)
            {
                switch (_chars[_curPos])
                {
                    case '(':
                        _curPos++;
                        _scanningFunction = ScanningFunction.Nmtoken;
                        _nextScaningFunction = ScanningFunction.Attlist5;
                        return Token.LeftParen;
                    case 'C':
                        if (_charsUsed - _curPos < 5)
                            goto ReadData;
                        if (_chars[_curPos + 1] != 'D' || _chars[_curPos + 2] != 'A' ||
                             _chars[_curPos + 3] != 'T' || _chars[_curPos + 4] != 'A')
                        {
                            Throw(_curPos, SR.Xml_InvalidAttributeType1);
                        }
                        _curPos += 5;
                        _scanningFunction = ScanningFunction.Attlist6;
                        return Token.CDATA;
                    case 'E':
                        if (_charsUsed - _curPos < 9)
                            goto ReadData;
                        _scanningFunction = ScanningFunction.Attlist6;
                        if (_chars[_curPos + 1] != 'N' || _chars[_curPos + 2] != 'T' ||
                             _chars[_curPos + 3] != 'I' || _chars[_curPos + 4] != 'T')
                        {
                            Throw(_curPos, SR.Xml_InvalidAttributeType);
                        }
                        switch (_chars[_curPos + 5])
                        {
                            case 'I':
                                if (_chars[_curPos + 6] != 'E' || _chars[_curPos + 7] != 'S')
                                {
                                    Throw(_curPos, SR.Xml_InvalidAttributeType);
                                }
                                _curPos += 8;
                                return Token.ENTITIES;
                            case 'Y':
                                _curPos += 6;
                                return Token.ENTITY;
                            default:
                                Throw(_curPos, SR.Xml_InvalidAttributeType);
                                break;
                        }
                        break;
                    case 'I':
                        if (_charsUsed - _curPos < 6)
                            goto ReadData;
                        _scanningFunction = ScanningFunction.Attlist6;
                        if (_chars[_curPos + 1] != 'D')
                        {
                            Throw(_curPos, SR.Xml_InvalidAttributeType);
                        }

                        if (_chars[_curPos + 2] != 'R')
                        {
                            _curPos += 2;
                            return Token.ID;
                        }

                        if (_chars[_curPos + 3] != 'E' || _chars[_curPos + 4] != 'F')
                        {
                            Throw(_curPos, SR.Xml_InvalidAttributeType);
                        }

                        if (_chars[_curPos + 5] != 'S')
                        {
                            _curPos += 5;
                            return Token.IDREF;
                        }
                        else
                        {
                            _curPos += 6;
                            return Token.IDREFS;
                        }
                    case 'N':
                        if (_charsUsed - _curPos < 8 && !_readerAdapter.IsEof)
                        {
                            goto ReadData;
                        }
                        switch (_chars[_curPos + 1])
                        {
                            case 'O':
                                if (_chars[_curPos + 2] != 'T' || _chars[_curPos + 3] != 'A' ||
                                     _chars[_curPos + 4] != 'T' || _chars[_curPos + 5] != 'I' ||
                                     _chars[_curPos + 6] != 'O' || _chars[_curPos + 7] != 'N')
                                {
                                    Throw(_curPos, SR.Xml_InvalidAttributeType);
                                }
                                _curPos += 8;
                                _scanningFunction = ScanningFunction.Attlist3;
                                return Token.NOTATION;
                            case 'M':
                                if (_chars[_curPos + 2] != 'T' || _chars[_curPos + 3] != 'O' ||
                                     _chars[_curPos + 4] != 'K' || _chars[_curPos + 5] != 'E' ||
                                    _chars[_curPos + 6] != 'N')
                                {
                                    Throw(_curPos, SR.Xml_InvalidAttributeType);
                                }
                                _scanningFunction = ScanningFunction.Attlist6;

                                if (_chars[_curPos + 7] == 'S')
                                {
                                    _curPos += 8;
                                    return Token.NMTOKENS;
                                }
                                else
                                {
                                    _curPos += 7;
                                    return Token.NMTOKEN;
                                }
                            default:
                                Throw(_curPos, SR.Xml_InvalidAttributeType);
                                break;
                        }
                        break;
                    default:
                        Throw(_curPos, SR.Xml_InvalidAttributeType);
                        break;
                }

            ReadData:
                if (ReadData() == 0)
                {
                    Throw(_curPos, SR.Xml_IncompleteDtdContent);
                }
            }
        }

        private Token ScanAttlist3()
        {
            if (_chars[_curPos] == '(')
            {
                _curPos++;
                _scanningFunction = ScanningFunction.Name;
                _nextScaningFunction = ScanningFunction.Attlist4;
                return Token.LeftParen;
            }
            else
            {
                ThrowUnexpectedToken(_curPos, "(");
                return Token.None;
            }
        }

        private Token ScanAttlist4()
        {
            switch (_chars[_curPos])
            {
                case ')':
                    _curPos++;
                    _scanningFunction = ScanningFunction.Attlist6;
                    return Token.RightParen;
                case '|':
                    _curPos++;
                    _scanningFunction = ScanningFunction.Name;
                    _nextScaningFunction = ScanningFunction.Attlist4;
                    return Token.Or;
                default:
                    ThrowUnexpectedToken(_curPos, ")", "|");
                    return Token.None;
            }
        }

        private Token ScanAttlist5()
        {
            switch (_chars[_curPos])
            {
                case ')':
                    _curPos++;
                    _scanningFunction = ScanningFunction.Attlist6;
                    return Token.RightParen;
                case '|':
                    _curPos++;
                    _scanningFunction = ScanningFunction.Nmtoken;
                    _nextScaningFunction = ScanningFunction.Attlist5;
                    return Token.Or;
                default:
                    ThrowUnexpectedToken(_curPos, ")", "|");
                    return Token.None;
            }
        }

        private Token ScanAttlist6()
        {
            for (;;)
            {
                switch (_chars[_curPos])
                {
                    case '"':
                    case '\'':
                        ScanLiteral(LiteralType.AttributeValue);
                        _scanningFunction = ScanningFunction.Attlist1;
                        return Token.Literal;
                    case '#':
                        if (_charsUsed - _curPos < 6)
                            goto ReadData;
                        switch (_chars[_curPos + 1])
                        {
                            case 'R':
                                if (_charsUsed - _curPos < 9)
                                    goto ReadData;
                                if (_chars[_curPos + 2] != 'E' || _chars[_curPos + 3] != 'Q' ||
                                     _chars[_curPos + 4] != 'U' || _chars[_curPos + 5] != 'I' ||
                                     _chars[_curPos + 6] != 'R' || _chars[_curPos + 7] != 'E' ||
                                     _chars[_curPos + 8] != 'D')
                                {
                                    Throw(_curPos, SR.Xml_ExpectAttType);
                                }
                                _curPos += 9;
                                _scanningFunction = ScanningFunction.Attlist1;
                                return Token.REQUIRED;
                            case 'I':
                                if (_charsUsed - _curPos < 8)
                                    goto ReadData;
                                if (_chars[_curPos + 2] != 'M' || _chars[_curPos + 3] != 'P' ||
                                     _chars[_curPos + 4] != 'L' || _chars[_curPos + 5] != 'I' ||
                                     _chars[_curPos + 6] != 'E' || _chars[_curPos + 7] != 'D')
                                {
                                    Throw(_curPos, SR.Xml_ExpectAttType);
                                }
                                _curPos += 8;
                                _scanningFunction = ScanningFunction.Attlist1;
                                return Token.IMPLIED;
                            case 'F':
                                if (_chars[_curPos + 2] != 'I' || _chars[_curPos + 3] != 'X' ||
                                     _chars[_curPos + 4] != 'E' || _chars[_curPos + 5] != 'D')
                                {
                                    Throw(_curPos, SR.Xml_ExpectAttType);
                                }
                                _curPos += 6;
                                _scanningFunction = ScanningFunction.Attlist7;
                                return Token.FIXED;
                            default:
                                Throw(_curPos, SR.Xml_ExpectAttType);
                                break;
                        }
                        break;
                    default:
                        Throw(_curPos, SR.Xml_ExpectAttType);
                        break;
                }
            ReadData:
                if (ReadData() == 0)
                {
                    Throw(_curPos, SR.Xml_IncompleteDtdContent);
                }
            }
        }

        private Token ScanAttlist7()
        {
            switch (_chars[_curPos])
            {
                case '"':
                case '\'':
                    ScanLiteral(LiteralType.AttributeValue);
                    _scanningFunction = ScanningFunction.Attlist1;
                    return Token.Literal;
                default:
                    ThrowUnexpectedToken(_curPos, "\"", "'");
                    return Token.None;
            }
        }

        private Token ScanLiteral(LiteralType literalType)
        {
            Debug.Assert(_chars[_curPos] == '"' || _chars[_curPos] == '\'');

            char quoteChar = _chars[_curPos];
            char replaceChar = (literalType == LiteralType.AttributeValue) ? (char)0x20 : (char)0xA;
            int startQuoteEntityId = _currentEntityId;

            _literalLineInfo.Set(LineNo, LinePos);

            _curPos++;
            _tokenStartPos = _curPos;

#if SILVERLIGHT
            stringBuilder.Clear();
#else
            _stringBuilder.Length = 0;
#endif

            for (;;)
            {
                while (_xmlCharType.IsAttributeValueChar(_chars[_curPos]) && _chars[_curPos] != '%')
                {
                    _curPos++;
                }

                if (_chars[_curPos] == quoteChar && _currentEntityId == startQuoteEntityId)
                {
                    if (_stringBuilder.Length > 0)
                    {
                        _stringBuilder.Append(_chars, _tokenStartPos, _curPos - _tokenStartPos);
                    }
                    _curPos++;
                    _literalQuoteChar = quoteChar;
                    return Token.Literal;
                }

                int tmp1 = _curPos - _tokenStartPos;
                if (tmp1 > 0)
                {
                    _stringBuilder.Append(_chars, _tokenStartPos, tmp1);
                    _tokenStartPos = _curPos;
                }

                switch (_chars[_curPos])
                {
                    case '"':
                    case '\'':
                    case '>':
                        _curPos++;
                        continue;
                    // eol
                    case (char)0xA:
                        _curPos++;
                        if (Normalize)
                        {
                            _stringBuilder.Append(replaceChar);        // For attributes: CDATA normalization of 0xA
                            _tokenStartPos = _curPos;
                        }
                        _readerAdapter.OnNewLine(_curPos);
                        continue;
                    case (char)0xD:
                        if (_chars[_curPos + 1] == (char)0xA)
                        {
                            if (Normalize)
                            {
                                if (literalType == LiteralType.AttributeValue)
                                {
                                    _stringBuilder.Append(_readerAdapter.IsEntityEolNormalized ? "\u0020\u0020" : "\u0020"); // CDATA normalization of 0xD 0xA
                                }
                                else
                                {
                                    _stringBuilder.Append(_readerAdapter.IsEntityEolNormalized ? "\u000D\u000A" : "\u000A"); // EOL normalization of 0xD 0xA                                    
                                }
                                _tokenStartPos = _curPos + 2;

                                SaveParsingBuffer();          // EOL normalization of 0xD 0xA in internal subset value
                                _readerAdapter.CurrentPosition++;
                            }
                            _curPos += 2;
                        }
                        else if (_curPos + 1 == _charsUsed)
                        {
                            goto ReadData;
                        }
                        else
                        {
                            _curPos++;
                            if (Normalize)
                            {
                                _stringBuilder.Append(replaceChar); // For attributes: CDATA normalization of 0xD and 0xD 0xA
                                _tokenStartPos = _curPos;              // For entities:   EOL normalization of 0xD and 0xD 0xA
                            }
                        }
                        _readerAdapter.OnNewLine(_curPos);
                        continue;
                    // tab
                    case (char)0x9:
                        if (literalType == LiteralType.AttributeValue && Normalize)
                        {
                            _stringBuilder.Append((char)0x20);      // For attributes: CDATA normalization of 0x9
                            _tokenStartPos++;
                        }
                        _curPos++;
                        continue;
                    // attribute values cannot contain '<'
                    case '<':
                        if (literalType == LiteralType.AttributeValue)
                        {
                            Throw(_curPos, SR.Xml_BadAttributeChar, XmlException.BuildCharExceptionArgs('<', '\0'));
                        }
                        _curPos++;
                        continue;
                    // parameter entity reference
                    case '%':
                        if (literalType != LiteralType.EntityReplText)
                        {
                            _curPos++;
                            continue;
                        }
                        HandleEntityReference(true, true, literalType == LiteralType.AttributeValue);
                        _tokenStartPos = _curPos;
                        continue;
                    // general entity reference
                    case '&':
                        if (literalType == LiteralType.SystemOrPublicID)
                        {
                            _curPos++;
                            continue;
                        }
                        if (_curPos + 1 == _charsUsed)
                        {
                            goto ReadData;
                        }
                        // numeric characters reference
                        if (_chars[_curPos + 1] == '#')
                        {
                            SaveParsingBuffer();
                            int endPos = _readerAdapter.ParseNumericCharRef(SaveInternalSubsetValue ? _internalSubsetValueSb : null);
                            LoadParsingBuffer();
                            _stringBuilder.Append(_chars, _curPos, endPos - _curPos);
                            _readerAdapter.CurrentPosition = endPos;
                            _tokenStartPos = endPos;
                            _curPos = endPos;
                            continue;
                        }
                        else
                        {
                            // general entity reference
                            SaveParsingBuffer();
                            if (literalType == LiteralType.AttributeValue)
                            {
                                int endPos = _readerAdapter.ParseNamedCharRef(true, SaveInternalSubsetValue ? _internalSubsetValueSb : null);
                                LoadParsingBuffer();

                                if (endPos >= 0)
                                {
                                    _stringBuilder.Append(_chars, _curPos, endPos - _curPos);
                                    _readerAdapter.CurrentPosition = endPos;
                                    _tokenStartPos = endPos;
                                    _curPos = endPos;
                                    continue;
                                }
                                else
                                {
                                    HandleEntityReference(false, true, true);
                                    _tokenStartPos = _curPos;
                                }
                                continue;
                            }
                            else
                            {
                                int endPos = _readerAdapter.ParseNamedCharRef(false, null);
                                LoadParsingBuffer();

                                if (endPos >= 0)
                                {
                                    _tokenStartPos = _curPos;
                                    _curPos = endPos;
                                    continue;
                                }
                                else
                                {
                                    _stringBuilder.Append('&');
                                    _curPos++;
                                    _tokenStartPos = _curPos;
                                    // Bypass general entities in entity values
                                    XmlQualifiedName entityName = ScanEntityName();
                                    VerifyEntityReference(entityName, false, false, false);
                                }
                                continue;
                            }
                        }
                    default:
                        // end of buffer
                        if (_curPos == _charsUsed)
                        {
                            goto ReadData;
                        }
                        // surrogate chars
                        else
                        {
                            char ch = _chars[_curPos];
                            if (XmlCharType.IsHighSurrogate(ch))
                            {
                                if (_curPos + 1 == _charsUsed)
                                {
                                    goto ReadData;
                                }
                                _curPos++;
                                if (XmlCharType.IsLowSurrogate(_chars[_curPos]))
                                {
                                    _curPos++;
                                    continue;
                                }
                            }
                            ThrowInvalidChar(_chars, _charsUsed, _curPos);
                            return Token.None;
                        }
                }

            ReadData:
                Debug.Assert(_curPos - _tokenStartPos == 0);

                // read new characters into the buffer
                if (_readerAdapter.IsEof || ReadData() == 0)
                {
                    if (literalType == LiteralType.SystemOrPublicID || !HandleEntityEnd(true))
                    {
                        Throw(_curPos, SR.Xml_UnclosedQuote);
                    }
                }
                _tokenStartPos = _curPos;
            }
        }

        private XmlQualifiedName ScanEntityName()
        {
            try
            {
                ScanName();
            }
            catch (XmlException e)
            {
                Throw(SR.Xml_ErrorParsingEntityName, string.Empty, e.LineNumber, e.LinePosition);
            }

            if (_chars[_curPos] != ';')
            {
                ThrowUnexpectedToken(_curPos, ";");
            }
            XmlQualifiedName entityName = GetNameQualified(false);
            _curPos++;

            return entityName;
        }

        private Token ScanNotation1()
        {
            switch (_chars[_curPos])
            {
                case 'P':
                    if (!EatPublicKeyword())
                    {
                        Throw(_curPos, SR.Xml_ExpectExternalOrClose);
                    }
                    _nextScaningFunction = ScanningFunction.ClosingTag;
                    _scanningFunction = ScanningFunction.PublicId1;
                    return Token.PUBLIC;
                case 'S':
                    if (!EatSystemKeyword())
                    {
                        Throw(_curPos, SR.Xml_ExpectExternalOrClose);
                    }
                    _nextScaningFunction = ScanningFunction.ClosingTag;
                    _scanningFunction = ScanningFunction.SystemId;
                    return Token.SYSTEM;
                default:
                    Throw(_curPos, SR.Xml_ExpectExternalOrPublicId);
                    return Token.None;
            }
        }

        private Token ScanSystemId()
        {
            if (_chars[_curPos] != '"' && _chars[_curPos] != '\'')
            {
                ThrowUnexpectedToken(_curPos, "\"", "'");
            }

            ScanLiteral(LiteralType.SystemOrPublicID);

            _scanningFunction = _nextScaningFunction;
            return Token.Literal;
        }

        private Token ScanEntity1()
        {
            if (_chars[_curPos] == '%')
            {
                _curPos++;
                _nextScaningFunction = ScanningFunction.Entity2;
                _scanningFunction = ScanningFunction.Name;
                return Token.Percent;
            }
            else
            {
                ScanName();
                _scanningFunction = ScanningFunction.Entity2;
                return Token.Name;
            }
        }

        private Token ScanEntity2()
        {
            switch (_chars[_curPos])
            {
                case 'P':
                    if (!EatPublicKeyword())
                    {
                        Throw(_curPos, SR.Xml_ExpectExternalOrClose);
                    }
                    _nextScaningFunction = ScanningFunction.Entity3;
                    _scanningFunction = ScanningFunction.PublicId1;
                    return Token.PUBLIC;
                case 'S':
                    if (!EatSystemKeyword())
                    {
                        Throw(_curPos, SR.Xml_ExpectExternalOrClose);
                    }
                    _nextScaningFunction = ScanningFunction.Entity3;
                    _scanningFunction = ScanningFunction.SystemId;
                    return Token.SYSTEM;

                case '"':
                case '\'':
                    ScanLiteral(LiteralType.EntityReplText);
                    _scanningFunction = ScanningFunction.ClosingTag;
                    return Token.Literal;
                default:
                    Throw(_curPos, SR.Xml_ExpectExternalIdOrEntityValue);
                    return Token.None;
            }
        }

        private Token ScanEntity3()
        {
            if (_chars[_curPos] == 'N')
            {
                while (_charsUsed - _curPos < 5)
                {
                    if (ReadData() == 0)
                    {
                        goto End;
                    }
                }
                if (_chars[_curPos + 1] == 'D' && _chars[_curPos + 2] == 'A' &&
                     _chars[_curPos + 3] == 'T' && _chars[_curPos + 4] == 'A')
                {
                    _curPos += 5;
                    _scanningFunction = ScanningFunction.Name;
                    _nextScaningFunction = ScanningFunction.ClosingTag;
                    return Token.NData;
                }
            }
        End:
            _scanningFunction = ScanningFunction.ClosingTag;
            return Token.None;
        }

        private Token ScanPublicId1()
        {
            if (_chars[_curPos] != '"' && _chars[_curPos] != '\'')
            {
                ThrowUnexpectedToken(_curPos, "\"", "'");
            }

            ScanLiteral(LiteralType.SystemOrPublicID);

            _scanningFunction = ScanningFunction.PublicId2;
            return Token.Literal;
        }

        private Token ScanPublicId2()
        {
            if (_chars[_curPos] != '"' && _chars[_curPos] != '\'')
            {
                _scanningFunction = _nextScaningFunction;
                return Token.None;
            }

            ScanLiteral(LiteralType.SystemOrPublicID);
            _scanningFunction = _nextScaningFunction;

            return Token.Literal;
        }

        private Token ScanCondSection1()
        {
            if (_chars[_curPos] != 'I')
            {
                Throw(_curPos, SR.Xml_ExpectIgnoreOrInclude);
            }
            _curPos++;

            for (;;)
            {
                if (_charsUsed - _curPos < 5)
                {
                    goto ReadData;
                }
                switch (_chars[_curPos])
                {
                    case 'N':
                        if (_charsUsed - _curPos < 6)
                        {
                            goto ReadData;
                        }
                        if (_chars[_curPos + 1] != 'C' || _chars[_curPos + 2] != 'L' ||
                             _chars[_curPos + 3] != 'U' || _chars[_curPos + 4] != 'D' ||
                             _chars[_curPos + 5] != 'E' || _xmlCharType.IsNameSingleChar(_chars[_curPos + 6])
#if XML10_FIFTH_EDITION
                             || xmlCharType.IsNCNameHighSurrogateChar( chars[curPos+6] ) 
#endif
                            )
                        {
                            goto default;
                        }
                        _nextScaningFunction = ScanningFunction.SubsetContent;
                        _scanningFunction = ScanningFunction.CondSection2;
                        _curPos += 6;
                        return Token.INCLUDE;
                    case 'G':
                        if (_chars[_curPos + 1] != 'N' || _chars[_curPos + 2] != 'O' ||
                             _chars[_curPos + 3] != 'R' || _chars[_curPos + 4] != 'E' ||
                             _xmlCharType.IsNameSingleChar(_chars[_curPos + 5])
#if XML10_FIFTH_EDITION
                            ||xmlCharType.IsNCNameHighSurrogateChar( chars[curPos+5] ) 
#endif
                            )
                        {
                            goto default;
                        }
                        _nextScaningFunction = ScanningFunction.CondSection3;
                        _scanningFunction = ScanningFunction.CondSection2;
                        _curPos += 5;
                        return Token.IGNORE;
                    default:
                        Throw(_curPos - 1, SR.Xml_ExpectIgnoreOrInclude);
                        return Token.None;
                }
            ReadData:
                if (ReadData() == 0)
                {
                    Throw(_curPos, SR.Xml_IncompleteDtdContent);
                }
            }
        }

        private Token ScanCondSection2()
        {
            if (_chars[_curPos] != '[')
            {
                ThrowUnexpectedToken(_curPos, "[");
            }
            _curPos++;
            _scanningFunction = _nextScaningFunction;
            return Token.LeftBracket;
        }

        private Token ScanCondSection3()
        {
            int ignoreSectionDepth = 0;

            // skip ignored part
            for (;;)
            {
                while (_xmlCharType.IsTextChar(_chars[_curPos]) && _chars[_curPos] != ']')
                {
                    _curPos++;
                }

                switch (_chars[_curPos])
                {
                    case '"':
                    case '\'':
                    case (char)0x9:
                    case '&':
                        _curPos++;
                        continue;
                    // eol
                    case (char)0xA:
                        _curPos++;
                        _readerAdapter.OnNewLine(_curPos);
                        continue;
                    case (char)0xD:
                        if (_chars[_curPos + 1] == (char)0xA)
                        {
                            _curPos += 2;
                        }
                        else if (_curPos + 1 < _charsUsed || _readerAdapter.IsEof)
                        {
                            _curPos++;
                        }
                        else
                        {
                            goto ReadData;
                        }
                        _readerAdapter.OnNewLine(_curPos);
                        continue;
                    case '<':
                        if (_charsUsed - _curPos < 3)
                        {
                            goto ReadData;
                        }
                        if (_chars[_curPos + 1] != '!' || _chars[_curPos + 2] != '[')
                        {
                            _curPos++;
                            continue;
                        }
                        ignoreSectionDepth++;
                        _curPos += 3;
                        continue;
                    case ']':
                        if (_charsUsed - _curPos < 3)
                        {
                            goto ReadData;
                        }
                        if (_chars[_curPos + 1] != ']' || _chars[_curPos + 2] != '>')
                        {
                            _curPos++;
                            continue;
                        }
                        if (ignoreSectionDepth > 0)
                        {
                            ignoreSectionDepth--;
                            _curPos += 3;
                            continue;
                        }
                        else
                        {
                            _curPos += 3;
                            _scanningFunction = ScanningFunction.SubsetContent;
                            return Token.CondSectionEnd;
                        }
                    default:
                        // end of buffer
                        if (_curPos == _charsUsed)
                        {
                            goto ReadData;
                        }
                        // surrogate chars
                        else
                        {
                            char ch = _chars[_curPos];
                            if (XmlCharType.IsHighSurrogate(ch))
                            {
                                if (_curPos + 1 == _charsUsed)
                                {
                                    goto ReadData;
                                }
                                _curPos++;
                                if (XmlCharType.IsLowSurrogate(_chars[_curPos]))
                                {
                                    _curPos++;
                                    continue;
                                }
                            }
                            ThrowInvalidChar(_chars, _charsUsed, _curPos);
                            return Token.None;
                        }
                }

            ReadData:
                // read new characters into the buffer
                if (_readerAdapter.IsEof || ReadData() == 0)
                {
                    if (HandleEntityEnd(false))
                    {
                        continue;
                    }
                    Throw(_curPos, SR.Xml_UnclosedConditionalSection);
                }
                _tokenStartPos = _curPos;
            }
        }

        private void ScanName()
        {
            ScanQName(false);
        }

        private void ScanQName()
        {
            ScanQName(SupportNamespaces);
        }

        private void ScanQName(bool isQName)
        {
            _tokenStartPos = _curPos;
            int colonOffset = -1;

            for (;;)
            {
                unsafe
                {
                    if (_xmlCharType.IsStartNCNameSingleChar(_chars[_curPos]) || _chars[_curPos] == ':')
                    {
                        _curPos++;
                    }
#if XML10_FIFTH_EDITION
                    else if ( curPos + 1 < charsUsed && xmlCharType.IsNCNameSurrogateChar(chars[curPos+1], chars[curPos])) {
                        curPos += 2;
                    }
#endif
                    else
                    {
                        if (_curPos + 1 >= _charsUsed)
                        {
                            if (ReadDataInName())
                            {
                                continue;
                            }
                            Throw(_curPos, SR.Xml_UnexpectedEOF, "Name");
                        }
                        else
                        {
                            Throw(_curPos, SR.Xml_BadStartNameChar, XmlException.BuildCharExceptionArgs(_chars, _charsUsed, _curPos));
                        }
                    }
                }

            ContinueName:

                unsafe
                {
                    for (;;)
                    {
                        if (_xmlCharType.IsNCNameSingleChar(_chars[_curPos]))
                        {
                            _curPos++;
                        }
#if XML10_FIFTH_EDITION
                        else if ( curPos + 1 < charsUsed && xmlCharType.IsNameSurrogateChar(chars[curPos + 1], chars[curPos]) ) {
                            curPos += 2;
                        }
#endif
                        else
                        {
                            break;
                        }
                    }
                }

                if (_chars[_curPos] == ':')
                {
                    if (isQName)
                    {
                        if (colonOffset != -1)
                        {
                            Throw(_curPos, SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(':', '\0'));
                        }
                        colonOffset = _curPos - _tokenStartPos;
                        _curPos++;
                        continue;
                    }
                    else
                    {
                        _curPos++;
                        goto ContinueName;
                    }
                }
                // end of buffer
                else if (_curPos == _charsUsed
#if XML10_FIFTH_EDITION
                    || ( curPos + 1 == charsUsed && xmlCharType.IsNCNameHighSurrogateChar( chars[curPos] ) ) 
#endif
                    )
                {
                    if (ReadDataInName())
                    {
                        goto ContinueName;
                    }
                    if (_tokenStartPos == _curPos)
                    {
                        Throw(_curPos, SR.Xml_UnexpectedEOF, "Name");
                    }
                }
                // end of name
                _colonPos = (colonOffset == -1) ? -1 : _tokenStartPos + colonOffset;
                return;
            }
        }

        private bool ReadDataInName()
        {
            int offset = _curPos - _tokenStartPos;
            _curPos = _tokenStartPos;
            bool newDataRead = (ReadData() != 0);
            _tokenStartPos = _curPos;
            _curPos += offset;
            return newDataRead;
        }

        private void ScanNmtoken()
        {
            _tokenStartPos = _curPos;

            for (;;)
            {
                unsafe
                {
                    for (;;)
                    {
                        if ( _xmlCharType.IsNCNameSingleChar(_chars[_curPos]) || _chars[_curPos] == ':' )
                        {
                            _curPos++;
                        }
#if XML10_FIFTH_EDITION
                        else if (curPos + 1 < charsUsed && xmlCharType.IsNCNameSurrogateChar(chars[curPos + 1], chars[curPos])) {
                            curPos += 2;
                        }
#endif
                        else
                        {
                            break;
                        }
                    }
                }

                if (_curPos < _charsUsed
#if XML10_FIFTH_EDITION
                    && ( !xmlCharType.IsNCNameHighSurrogateChar( chars[curPos] ) || curPos + 1 < charsUsed ) 
#endif
                    )
                {
                    if (_curPos - _tokenStartPos == 0)
                    {
                        Throw(_curPos, SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(_chars, _charsUsed, _curPos));
                    }
                    return;
                }

                int len = _curPos - _tokenStartPos;
                _curPos = _tokenStartPos;
                if (ReadData() == 0)
                {
                    if (len > 0)
                    {
                        _tokenStartPos = _curPos;
                        _curPos += len;
                        return;
                    }
                    Throw(_curPos, SR.Xml_UnexpectedEOF, "NmToken");
                }
                _tokenStartPos = _curPos;
                _curPos += len;
            }
        }

        private bool EatPublicKeyword()
        {
            Debug.Assert(_chars[_curPos] == 'P');
            while (_charsUsed - _curPos < 6)
            {
                if (ReadData() == 0)
                {
                    return false;
                }
            }
            if (_chars[_curPos + 1] != 'U' || _chars[_curPos + 2] != 'B' ||
                 _chars[_curPos + 3] != 'L' || _chars[_curPos + 4] != 'I' ||
                 _chars[_curPos + 5] != 'C')
            {
                return false;
            }
            _curPos += 6;
            return true;
        }

        private bool EatSystemKeyword()
        {
            Debug.Assert(_chars[_curPos] == 'S');
            while (_charsUsed - _curPos < 6)
            {
                if (ReadData() == 0)
                {
                    return false;
                }
            }
            if (_chars[_curPos + 1] != 'Y' || _chars[_curPos + 2] != 'S' ||
                 _chars[_curPos + 3] != 'T' || _chars[_curPos + 4] != 'E' ||
                 _chars[_curPos + 5] != 'M')
            {
                return false;
            }
            _curPos += 6;
            return true;
        }

        //
        // Scanned data getters
        //
        private XmlQualifiedName GetNameQualified(bool canHavePrefix)
        {
            Debug.Assert(_curPos - _tokenStartPos > 0);
            if (_colonPos == -1)
            {
                return new XmlQualifiedName(_nameTable.Add(_chars, _tokenStartPos, _curPos - _tokenStartPos));
            }
            else
            {
                if (canHavePrefix)
                {
                    return new XmlQualifiedName(_nameTable.Add(_chars, _colonPos + 1, _curPos - _colonPos - 1),
                                                 _nameTable.Add(_chars, _tokenStartPos, _colonPos - _tokenStartPos));
                }
                else
                {
                    Throw(_tokenStartPos, SR.Xml_ColonInLocalName, GetNameString());
                    return null;
                }
            }
        }

        private string GetNameString()
        {
            Debug.Assert(_curPos - _tokenStartPos > 0);
            return new string(_chars, _tokenStartPos, _curPos - _tokenStartPos);
        }

        private string GetNmtokenString()
        {
            return GetNameString();
        }

        private string GetValue()
        {
            if (_stringBuilder.Length == 0)
            {
                return new string(_chars, _tokenStartPos, _curPos - _tokenStartPos - 1);
            }
            else
            {
                return _stringBuilder.ToString();
            }
        }

        private string GetValueWithStrippedSpaces()
        {
            Debug.Assert(_curPos == 0 || _chars[_curPos - 1] == '"' || _chars[_curPos - 1] == '\'');
            // We cannot StripSpaces directly in the buffer - we need the original value inthe buffer intact so that the internal subset value is correct
            string val = (_stringBuilder.Length == 0) ? new string(_chars, _tokenStartPos, _curPos - _tokenStartPos - 1) : _stringBuilder.ToString();
            return StripSpaces(val);
        }

        //
        // Parsing buffer maintainance methods
        //
        private int ReadData()
        {
            SaveParsingBuffer();
            int charsRead = _readerAdapter.ReadData();
            LoadParsingBuffer();
            return charsRead;
        }

        private void LoadParsingBuffer()
        {
            _chars = _readerAdapter.ParsingBuffer;
            _charsUsed = _readerAdapter.ParsingBufferLength;
            _curPos = _readerAdapter.CurrentPosition;
        }

        private void SaveParsingBuffer()
        {
            SaveParsingBuffer(_curPos);
        }

        private void SaveParsingBuffer(int internalSubsetValueEndPos)
        {
            if (SaveInternalSubsetValue)
            {
                Debug.Assert(_internalSubsetValueSb != null);

                int readerCurPos = _readerAdapter.CurrentPosition;
                if (internalSubsetValueEndPos - readerCurPos > 0)
                {
                    _internalSubsetValueSb.Append(_chars, readerCurPos, internalSubsetValueEndPos - readerCurPos);
                }
            }
            _readerAdapter.CurrentPosition = _curPos;
        }

        //
        // Entity handling
        //
        private bool HandleEntityReference(bool paramEntity, bool inLiteral, bool inAttribute)
        {
            Debug.Assert(_chars[_curPos] == '&' || _chars[_curPos] == '%');
            _curPos++;

            return HandleEntityReference(ScanEntityName(), paramEntity, inLiteral, inAttribute);
        }

        private bool HandleEntityReference(XmlQualifiedName entityName, bool paramEntity, bool inLiteral, bool inAttribute)
        {
            Debug.Assert(_chars[_curPos - 1] == ';');

            SaveParsingBuffer();
            if (paramEntity && ParsingInternalSubset && !ParsingTopLevelMarkup)
            {
                Throw(_curPos - entityName.Name.Length - 1, SR.Xml_InvalidParEntityRef);
            }

            SchemaEntity entity = VerifyEntityReference(entityName, paramEntity, true, inAttribute);
            if (entity == null)
            {
                return false;
            }
            if (entity.ParsingInProgress)
            {
                Throw(_curPos - entityName.Name.Length - 1, paramEntity ? SR.Xml_RecursiveParEntity : SR.Xml_RecursiveGenEntity, entityName.Name);
            }

            int newEntityId;
            if (entity.IsExternal)
            {
                if (!_readerAdapter.PushEntity(entity, out newEntityId))
                {
                    return false;
                }
                _externalEntitiesDepth++;
            }
            else
            {
                if (entity.Text.Length == 0)
                {
                    return false;
                }

                if (!_readerAdapter.PushEntity(entity, out newEntityId))
                {
                    return false;
                }
            }
            _currentEntityId = newEntityId;

            if (paramEntity && !inLiteral && _scanningFunction != ScanningFunction.ParamEntitySpace)
            {
                _savedScanningFunction = _scanningFunction;
                _scanningFunction = ScanningFunction.ParamEntitySpace;
            }

            LoadParsingBuffer();
            return true;
        }

        private bool HandleEntityEnd(bool inLiteral)
        {
            SaveParsingBuffer();

            IDtdEntityInfo oldEntity;
            if (!_readerAdapter.PopEntity(out oldEntity, out _currentEntityId))
            {
                return false;
            }
            LoadParsingBuffer();

            if (oldEntity == null)
            {
                // external subset popped
                Debug.Assert(!ParsingInternalSubset || _freeFloatingDtd);
                Debug.Assert(_currentEntityId == 0);
                if (_scanningFunction == ScanningFunction.ParamEntitySpace)
                {
                    _scanningFunction = _savedScanningFunction;
                }
                return false;
            }

            if (oldEntity.IsExternal)
            {
                _externalEntitiesDepth--;
            }

            if (!inLiteral && _scanningFunction != ScanningFunction.ParamEntitySpace)
            {
                _savedScanningFunction = _scanningFunction;
                _scanningFunction = ScanningFunction.ParamEntitySpace;
            }

            return true;
        }

        private SchemaEntity VerifyEntityReference(XmlQualifiedName entityName, bool paramEntity, bool mustBeDeclared, bool inAttribute)
        {
            Debug.Assert(_chars[_curPos - 1] == ';');

            SchemaEntity entity;
            if (paramEntity)
            {
                _schemaInfo.ParameterEntities.TryGetValue(entityName, out entity);
            }
            else
            {
                _schemaInfo.GeneralEntities.TryGetValue(entityName, out entity);
            }

            if (entity == null)
            {
                if (paramEntity)
                {
#if !SILVERLIGHT
                    if (_validate)
                    {
                        SendValidationEvent(_curPos - entityName.Name.Length - 1, XmlSeverityType.Error, SR.Xml_UndeclaredParEntity, entityName.Name);
                    }
#endif
                }
                else if (mustBeDeclared)
                {
                    if (!ParsingInternalSubset)
                    {
#if !SILVERLIGHT
                        if (_validate)
                        {
                            SendValidationEvent(_curPos - entityName.Name.Length - 1, XmlSeverityType.Error, SR.Xml_UndeclaredEntity, entityName.Name);
                        }
#endif
                    }
                    else
                    {
                        Throw(_curPos - entityName.Name.Length - 1, SR.Xml_UndeclaredEntity, entityName.Name);
                    }
                }
                return null;
            }

            if (!entity.NData.IsEmpty)
            {
                Throw(_curPos - entityName.Name.Length - 1, SR.Xml_UnparsedEntityRef, entityName.Name);
            }

            if (inAttribute && entity.IsExternal)
            {
                Throw(_curPos - entityName.Name.Length - 1, SR.Xml_ExternalEntityInAttValue, entityName.Name);
            }

            return entity;
        }

        //
        // Helper methods and properties
        //
#if !SILVERLIGHT
        private void SendValidationEvent(int pos, XmlSeverityType severity, string code, string arg)
        {
            Debug.Assert(_validate);
            SendValidationEvent(severity, new XmlSchemaException(code, arg, BaseUriStr, (int)LineNo, (int)LinePos + (pos - _curPos)));
        }

        private void SendValidationEvent(XmlSeverityType severity, string code, string arg)
        {
            Debug.Assert(_validate);
            SendValidationEvent(severity, new XmlSchemaException(code, arg, BaseUriStr, (int)LineNo, (int)LinePos));
        }

        private void SendValidationEvent(XmlSeverityType severity, XmlSchemaException e)
        {
            Debug.Assert(_validate);
            IValidationEventHandling eventHandling = _readerAdapterWithValidation.ValidationEventHandling;
            if (eventHandling != null)
            {
                eventHandling.SendEvent(e, severity);
            }
        }
#endif

        private bool IsAttributeValueType(Token token)
        {
            return (int)token >= (int)Token.CDATA && (int)token <= (int)Token.NOTATION;
        }

        private int LineNo
        {
            get
            {
                return _readerAdapter.LineNo;
            }
        }

        private int LinePos
        {
            get
            {
                return _curPos - _readerAdapter.LineStartPosition;
            }
        }

        private string BaseUriStr
        {
            get
            {
                Uri tmp = _readerAdapter.BaseUri;
                return (tmp != null) ? tmp.ToString() : string.Empty;
            }
        }

        private void OnUnexpectedError()
        {
            Debug.Assert(false, "This is an unexpected error that should have been handled in the ScanXXX methods.");
            Throw(_curPos, SR.Xml_InternalError);
        }

        private void Throw(int curPos, string res)
        {
            Throw(curPos, res, string.Empty);
        }

        private void Throw(int curPos, string res, string arg)
        {
            _curPos = curPos;
            Uri baseUri = _readerAdapter.BaseUri;
            _readerAdapter.Throw(new XmlException(res, arg, (int)LineNo, (int)LinePos, baseUri == null ? null : baseUri.ToString()));
        }
        private void Throw(int curPos, string res, string[] args)
        {
            _curPos = curPos;
            Uri baseUri = _readerAdapter.BaseUri;
            _readerAdapter.Throw(new XmlException(res, args, (int)LineNo, (int)LinePos, baseUri == null ? null : baseUri.ToString()));
        }

        private void Throw(string res, string arg, int lineNo, int linePos)
        {
            Uri baseUri = _readerAdapter.BaseUri;
            _readerAdapter.Throw(new XmlException(res, arg, (int)lineNo, (int)linePos, baseUri == null ? null : baseUri.ToString()));
        }

        private void ThrowInvalidChar(int pos, string data, int invCharPos)
        {
            Throw(pos, SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(data, invCharPos));
        }

        private void ThrowInvalidChar(char[] data, int length, int invCharPos)
        {
            Throw(invCharPos, SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(data, length, invCharPos));
        }

        private void ThrowUnexpectedToken(int pos, string expectedToken)
        {
            ThrowUnexpectedToken(pos, expectedToken, null);
        }

        private void ThrowUnexpectedToken(int pos, string expectedToken1, string expectedToken2)
        {
            string unexpectedToken = ParseUnexpectedToken(pos);
            if (expectedToken2 != null)
            {
                Throw(_curPos, SR.Xml_UnexpectedTokens2, new string[3] { unexpectedToken, expectedToken1, expectedToken2 });
            }
            else
            {
                Throw(_curPos, SR.Xml_UnexpectedTokenEx, new string[2] { unexpectedToken, expectedToken1 });
            }
        }

        private string ParseUnexpectedToken(int startPos)
        {
            if (_xmlCharType.IsNCNameSingleChar(_chars[startPos])
#if XML10_FIFTH_EDITION
                || xmlCharType.IsNCNameHighSurrogateChar( chars[startPos] ) 
#endif
                )
            { // postpone the proper surrogate checking to the loop below
                int endPos = startPos;
                for (;;)
                {
                    if (_xmlCharType.IsNCNameSingleChar(_chars[endPos]))
                    {
                        endPos++;
                    }
#if XML10_FIFTH_EDITION
                    else if ( chars[endPos] != 0 && // check for end of the buffer
                              xmlCharType.IsNCNameSurrogateChar( chars[endPos], chars[endPos + 1] ) ) {
                        endPos += 2;
                    }
#endif
                    else
                    {
                        break;
                    }
                }
                int len = endPos - startPos;
                return new string(_chars, startPos, len > 0 ? len : 1);
            }
            else
            {
                Debug.Assert(startPos < _charsUsed);
                return new string(_chars, startPos, 1);
            }
        }

        // StripSpaces removes spaces at the beginning and at the end of the value and replaces sequences of spaces with a single space
        // !!! This method exists in 2 copies, here and in the XmlTextReaderImplHelper.cs
        //   If you're changing this one, please also fix the other one.
        //   (This is necessary due to packaging)
        internal static string StripSpaces(string value)
        {
            int len = value.Length;
            if (len <= 0)
            {
                return string.Empty;
            }

            int startPos = 0;
            StringBuilder norValue = null;

            while (value[startPos] == 0x20)
            {
                startPos++;
                if (startPos == len)
                {
                    return " ";
                }
            }

            int i;
            for (i = startPos; i < len; i++)
            {
                if (value[i] == 0x20)
                {
                    int j = i + 1;
                    while (j < len && value[j] == 0x20)
                    {
                        j++;
                    }
                    if (j == len)
                    {
                        if (norValue == null)
                        {
                            return value.Substring(startPos, i - startPos);
                        }
                        else
                        {
                            norValue.Append(value, startPos, i - startPos);
                            return norValue.ToString();
                        }
                    }
                    if (j > i + 1)
                    {
                        if (norValue == null)
                        {
                            norValue = new StringBuilder(len);
                        }
                        norValue.Append(value, startPos, i - startPos + 1);
                        startPos = j;
                        i = j - 1;
                    }
                }
            }
            if (norValue == null)
            {
                return (startPos == 0) ? value : value.Substring(startPos, len - startPos);
            }
            else
            {
                if (i > startPos)
                {
                    norValue.Append(value, startPos, i - startPos);
                }
                return norValue.ToString();
            }
        }
    }
}

