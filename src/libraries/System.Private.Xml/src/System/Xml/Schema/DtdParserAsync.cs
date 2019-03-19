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

using System.Threading.Tasks;

namespace System.Xml
{
    internal partial class DtdParser : IDtdParser
    {
        //
        // IDtdParser interface
        //
        #region IDtdParser Members

        async Task<IDtdInfo> IDtdParser.ParseInternalDtdAsync(IDtdParserAdapter adapter, bool saveInternalSubset)
        {
            Initialize(adapter);
            await ParseAsync(saveInternalSubset).ConfigureAwait(false);
            return _schemaInfo;
        }

        async Task<IDtdInfo> IDtdParser.ParseFreeFloatingDtdAsync(string baseUri, string docTypeName, string publicId, string systemId, string internalSubset, IDtdParserAdapter adapter)
        {
            InitializeFreeFloatingDtd(baseUri, docTypeName, publicId, systemId, internalSubset, adapter);
            await ParseAsync(false).ConfigureAwait(false);
            return _schemaInfo;
        }
        #endregion

        //
        // Parsing methods
        //

        private async Task ParseAsync(bool saveInternalSubset)
        {
            if (_freeFloatingDtd)
            {
                await ParseFreeFloatingDtdAsync().ConfigureAwait(false);
            }
            else
            {
                await ParseInDocumentDtdAsync(saveInternalSubset).ConfigureAwait(false);
            }

            _schemaInfo.Finish();

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
        }

        private async Task ParseInDocumentDtdAsync(bool saveInternalSubset)
        {
            LoadParsingBuffer();

            _scanningFunction = ScanningFunction.QName;
            _nextScaningFunction = ScanningFunction.Doctype1;

            // doctype name
            if (await GetTokenAsync(false).ConfigureAwait(false) != Token.QName)
            {
                OnUnexpectedError();
            }
            _schemaInfo.DocTypeName = GetNameQualified(true);

            // SYSTEM or PUBLIC id
            Token token = await GetTokenAsync(false).ConfigureAwait(false);
            if (token == Token.SYSTEM || token == Token.PUBLIC)
            {
                var tuple_0 = await ParseExternalIdAsync(token, Token.DOCTYPE).ConfigureAwait(false);
                _publicId = tuple_0.Item1;
                _systemId = tuple_0.Item2;

                token = await GetTokenAsync(false).ConfigureAwait(false);
            }

            switch (token)
            {
                case Token.LeftBracket:
                    if (saveInternalSubset)
                    {
                        SaveParsingBuffer(); // this will cause saving the internal subset right from the point after '['
                        _internalSubsetValueSb = new StringBuilder();
                    }
                    await ParseInternalSubsetAsync().ConfigureAwait(false);
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
                await ParseExternalSubsetAsync().ConfigureAwait(false);
            }
        }

        private async Task ParseFreeFloatingDtdAsync()
        {
            if (_hasFreeFloatingInternalSubset)
            {
                LoadParsingBuffer();
                await ParseInternalSubsetAsync().ConfigureAwait(false);
                SaveParsingBuffer();
            }

            if (_systemId != null && _systemId.Length > 0)
            {
                await ParseExternalSubsetAsync().ConfigureAwait(false);
            }
        }

        private Task ParseInternalSubsetAsync()
        {
            Debug.Assert(ParsingInternalSubset);
            return ParseSubsetAsync();
        }

        private async Task ParseExternalSubsetAsync()
        {
            Debug.Assert(_externalEntitiesDepth == 0);

            // push external subset
            if (!await _readerAdapter.PushExternalSubsetAsync(_systemId, _publicId).ConfigureAwait(false))
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
            await ParseSubsetAsync().ConfigureAwait(false);

#if DEBUG
            Debug.Assert(_readerAdapter.EntityStackLength == 0 ||
                         (_freeFloatingDtd && _readerAdapter.EntityStackLength == 1));
#endif
        }

        private async Task ParseSubsetAsync()
        {
            int startTagEntityId;
            for (;;)
            {
                Token token = await GetTokenAsync(false).ConfigureAwait(false);
                startTagEntityId = _currentEntityId;
                switch (token)
                {
                    case Token.AttlistDecl:
                        await ParseAttlistDeclAsync().ConfigureAwait(false);
                        break;

                    case Token.ElementDecl:
                        await ParseElementDeclAsync().ConfigureAwait(false);
                        break;

                    case Token.EntityDecl:
                        await ParseEntityDeclAsync().ConfigureAwait(false);
                        break;

                    case Token.NotationDecl:
                        await ParseNotationDeclAsync().ConfigureAwait(false);
                        break;

                    case Token.Comment:
                        await ParseCommentAsync().ConfigureAwait(false);
                        break;

                    case Token.PI:
                        await ParsePIAsync().ConfigureAwait(false);
                        break;

                    case Token.CondSectionStart:
                        if (ParsingInternalSubset)
                        {
                            Throw(_curPos - 3, SR.Xml_InvalidConditionalSection); // 3==strlen("<![")
                        }
                        await ParseCondSectionAsync().ConfigureAwait(false);
                        startTagEntityId = _currentEntityId;
                        break;
                    case Token.CondSectionEnd:
                        if (_condSectionDepth > 0)
                        {
                            _condSectionDepth--;
                            if (_validate && _currentEntityId != _condSectionEntityIds[_condSectionDepth])
                            {
                                SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                            }
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
                            if (await GetTokenAsync(false).ConfigureAwait(false) != Token.GreaterThan)
                            {
                                ThrowUnexpectedToken(_curPos, ">");
                            }
#if DEBUG
                            // check entity nesting
                            Debug.Assert(_readerAdapter.EntityStackLength == 0 ||
                                          (_freeFloatingDtd && _readerAdapter.EntityStackLength == 1));
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
                        Debug.Fail($"Unexpected token {token}");
                        break;
                }

                Debug.Assert(_scanningFunction == ScanningFunction.SubsetContent);

                if (_currentEntityId != startTagEntityId)
                {
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
                }
            }
        }

        private async Task ParseAttlistDeclAsync()
        {
            if (await GetTokenAsync(true).ConfigureAwait(false) != Token.QName)
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
                switch (await GetTokenAsync(false).ConfigureAwait(false))
                {
                    case Token.QName:
                        XmlQualifiedName attrName = GetNameQualified(true);
                        attrDef = new SchemaAttDef(attrName, attrName.Namespace);
                        attrDef.IsDeclaredInExternal = !ParsingInternalSubset;
                        attrDef.LineNumber = (int)LineNo;
                        attrDef.LinePosition = (int)LinePos - (_curPos - _tokenStartPos);
                        break;
                    case Token.GreaterThan:
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
                        return;
                    default:
                        goto UnexpectedError;
                }

                bool attrDefAlreadyExists = (elementDecl.GetAttDef(attrDef.Name) != null);

                await ParseAttlistTypeAsync(attrDef, elementDecl, attrDefAlreadyExists).ConfigureAwait(false);
                await ParseAttlistDefaultAsync(attrDef, attrDefAlreadyExists).ConfigureAwait(false);

                // check xml:space and xml:lang
                if (attrDef.Prefix.Length > 0 && attrDef.Prefix.Equals("xml"))
                {
                    if (attrDef.Name.Name == "space")
                    {
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
                            attrDef.Reserved = SchemaAttDef.Reserve.XmlSpace;
                            if (attrDef.TokenizedType != XmlTokenizedType.ENUMERATION)
                            {
                                Throw(SR.Xml_EnumerationRequired, string.Empty, attrDef.LineNumber, attrDef.LinePosition);
                            }
                            if (_validate)
                            {
                                attrDef.CheckXmlSpace(_readerAdapterWithValidation.ValidationEventHandling);
                            }
                        }
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

        private async Task ParseAttlistTypeAsync(SchemaAttDef attrDef, SchemaElementDecl elementDecl, bool ignoreErrors)
        {
            Token token = await GetTokenAsync(true).ConfigureAwait(false);

            if (token != Token.CDATA)
            {
                elementDecl.HasNonCDataAttribute = true;
            }

            if (IsAttributeValueType(token))
            {
                attrDef.TokenizedType = (XmlTokenizedType)(int)token;
                attrDef.SchemaType = XmlSchemaType.GetBuiltInSimpleType(attrDef.Datatype.TypeCode);

                switch (token)
                {
                    case Token.NOTATION:
                        break;
                    case Token.ID:
                        if (_validate && elementDecl.IsIdDeclared)
                        {
                            SchemaAttDef idAttrDef = elementDecl.GetAttDef(attrDef.Name);
                            if ((idAttrDef == null || idAttrDef.Datatype.TokenizedType != XmlTokenizedType.ID) && !ignoreErrors)
                            {
                                SendValidationEvent(XmlSeverityType.Error, SR.Sch_IdAttrDeclared, elementDecl.Name.ToString());
                            }
                        }
                        elementDecl.IsIdDeclared = true;
                        return;
                    default:
                        return;
                }
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

                if (await GetTokenAsync(true).ConfigureAwait(false) != Token.LeftParen)
                {
                    goto UnexpectedError;
                }

                // parse notation list
                if (await GetTokenAsync(false).ConfigureAwait(false) != Token.Name)
                {
                    goto UnexpectedError;
                }
                for (;;)
                {
                    string notationName = GetNameString();
                    if (!_schemaInfo.Notations.ContainsKey(notationName))
                    {
                        AddUndeclaredNotation(notationName);
                    }
                    if (_validate && !_v1Compat && attrDef.Values != null && attrDef.Values.Contains(notationName) && !ignoreErrors)
                    {
                        SendValidationEvent(XmlSeverityType.Error, new XmlSchemaException(SR.Xml_AttlistDuplNotationValue, notationName, BaseUriStr, (int)LineNo, (int)LinePos));
                    }
                    attrDef.AddValue(notationName);

                    switch (await GetTokenAsync(false).ConfigureAwait(false))
                    {
                        case Token.Or:
                            if (await GetTokenAsync(false).ConfigureAwait(false) != Token.Name)
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
                attrDef.SchemaType = XmlSchemaType.GetBuiltInSimpleType(attrDef.Datatype.TypeCode);

                // parse nmtoken list
                if (await GetTokenAsync(false).ConfigureAwait(false) != Token.Nmtoken)
                    goto UnexpectedError;
                attrDef.AddValue(GetNameString());

                for (;;)
                {
                    switch (await GetTokenAsync(false).ConfigureAwait(false))
                    {
                        case Token.Or:
                            if (await GetTokenAsync(false).ConfigureAwait(false) != Token.Nmtoken)
                                goto UnexpectedError;
                            string nmtoken = GetNmtokenString();
                            if (_validate && !_v1Compat && attrDef.Values != null && attrDef.Values.Contains(nmtoken) && !ignoreErrors)
                            {
                                SendValidationEvent(XmlSeverityType.Error, new XmlSchemaException(SR.Xml_AttlistDuplEnumValue, nmtoken, BaseUriStr, (int)LineNo, (int)LinePos));
                            }
                            attrDef.AddValue(nmtoken);
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

        private async Task ParseAttlistDefaultAsync(SchemaAttDef attrDef, bool ignoreErrors)
        {
            switch (await GetTokenAsync(true).ConfigureAwait(false))
            {
                case Token.REQUIRED:
                    attrDef.Presence = SchemaDeclBase.Use.Required;
                    return;
                case Token.IMPLIED:
                    attrDef.Presence = SchemaDeclBase.Use.Implied;
                    return;
                case Token.FIXED:
                    attrDef.Presence = SchemaDeclBase.Use.Fixed;
                    if (await GetTokenAsync(true).ConfigureAwait(false) != Token.Literal)
                    {
                        goto UnexpectedError;
                    }
                    break;
                case Token.Literal:
                    break;
                default:
                    goto UnexpectedError;
            }

            if (_validate && attrDef.Datatype.TokenizedType == XmlTokenizedType.ID && !ignoreErrors)
            {
                SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_AttListPresence, string.Empty);
            }

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

            DtdValidator.SetDefaultTypedValue(attrDef, _readerAdapter);
            return;

        UnexpectedError:
            OnUnexpectedError();
        }

        private async Task ParseElementDeclAsync()
        {
            // element name
            if (await GetTokenAsync(true).ConfigureAwait(false) != Token.QName)
            {
                goto UnexpectedError;
            }

            // get schema decl for element
            SchemaElementDecl elementDecl = null;
            XmlQualifiedName name = GetNameQualified(true);

            if (_schemaInfo.ElementDecls.TryGetValue(name, out elementDecl))
            {
                if (_validate)
                {
                    SendValidationEvent(_curPos - name.Name.Length, XmlSeverityType.Error, SR.Sch_DupElementDecl, GetNameString());
                }
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
            switch (await GetTokenAsync(true).ConfigureAwait(false))
            {
                case Token.EMPTY:
                    elementDecl.ContentValidator = ContentValidator.Empty;
                    break;
                case Token.ANY:
                    elementDecl.ContentValidator = ContentValidator.Any;
                    break;
                case Token.LeftParen:
                    int startParenEntityId = _currentEntityId;
                    switch (await GetTokenAsync(false).ConfigureAwait(false))
                    {
                        case Token.PCDATA:
                            {
                                ParticleContentValidator pcv = new ParticleContentValidator(XmlSchemaContentType.Mixed);
                                pcv.Start();
                                pcv.OpenGroup();

                                await ParseElementMixedContentAsync(pcv, startParenEntityId).ConfigureAwait(false);

                                elementDecl.ContentValidator = pcv.Finish(true);
                                break;
                            }
                        case Token.None:
                            {
                                ParticleContentValidator pcv = null;
                                pcv = new ParticleContentValidator(XmlSchemaContentType.ElementOnly);
                                pcv.Start();
                                pcv.OpenGroup();

                                await ParseElementOnlyContentAsync(pcv, startParenEntityId).ConfigureAwait(false);

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

            if (await GetTokenAsync(false).ConfigureAwait(false) != Token.GreaterThan)
            {
                ThrowUnexpectedToken(_curPos, ">");
            }
            return;

        UnexpectedError:
            OnUnexpectedError();
        }

        private async Task ParseElementOnlyContentAsync(ParticleContentValidator pcv, int startParenEntityId)
        {
            Stack<ParseElementOnlyContent_LocalFrame> localFrames = new Stack<ParseElementOnlyContent_LocalFrame>();
            ParseElementOnlyContent_LocalFrame currentFrame = new ParseElementOnlyContent_LocalFrame(startParenEntityId);
            localFrames.Push(currentFrame);

        RecursiveCall:

        Loop:
            switch (await GetTokenAsync(false).ConfigureAwait(false))
            {
                case Token.QName:
                    pcv.AddName(GetNameQualified(true), null);
                    await ParseHowManyAsync(pcv).ConfigureAwait(false);
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
            switch (await GetTokenAsync(false).ConfigureAwait(false))
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
                    await ParseHowManyAsync(pcv).ConfigureAwait(false);
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

        private async Task ParseHowManyAsync(ParticleContentValidator pcv)
        {
            switch (await GetTokenAsync(false).ConfigureAwait(false))
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

        private async Task ParseElementMixedContentAsync(ParticleContentValidator pcv, int startParenEntityId)
        {
            bool hasNames = false;
            int connectorEntityId = -1;
            int contentEntityId = _currentEntityId;

            for (;;)
            {
                switch (await GetTokenAsync(false).ConfigureAwait(false))
                {
                    case Token.RightParen:
                        pcv.CloseGroup();
                        if (_validate && _currentEntityId != startParenEntityId)
                        {
                            SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                        }
                        if (await GetTokenAsync(false).ConfigureAwait(false) == Token.Star && hasNames)
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

                        if (await GetTokenAsync(false).ConfigureAwait(false) != Token.QName)
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

        private async Task ParseEntityDeclAsync()
        {
            bool isParamEntity = false;
            SchemaEntity entity = null;

            // get entity name and type
            switch (await GetTokenAsync(true).ConfigureAwait(false))
            {
                case Token.Percent:
                    isParamEntity = true;
                    if (await GetTokenAsync(true).ConfigureAwait(false) != Token.Name)
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

            Token token = await GetTokenAsync(true).ConfigureAwait(false);
            switch (token)
            {
                case Token.PUBLIC:
                case Token.SYSTEM:
                    string systemId;
                    string publicId;

                    var tuple_1 = await ParseExternalIdAsync(token, Token.EntityDecl).ConfigureAwait(false);
                    publicId = tuple_1.Item1;
                    systemId = tuple_1.Item2;

                    entity.IsExternal = true;
                    entity.Url = systemId;
                    entity.Pubid = publicId;

                    if (await GetTokenAsync(false).ConfigureAwait(false) == Token.NData)
                    {
                        if (isParamEntity)
                        {
                            ThrowUnexpectedToken(_curPos - 5, ">"); // 5 == strlen("NDATA")
                        }
                        if (!_whitespaceSeen)
                        {
                            Throw(_curPos - 5, SR.Xml_ExpectingWhiteSpace, "NDATA");
                        }

                        if (await GetTokenAsync(true).ConfigureAwait(false) != Token.Name)
                        {
                            goto UnexpectedError;
                        }

                        entity.NData = GetNameQualified(false);
                        string notationName = entity.NData.Name;
                        if (!_schemaInfo.Notations.ContainsKey(notationName))
                        {
                            AddUndeclaredNotation(notationName);
                        }
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

            if (await GetTokenAsync(false).ConfigureAwait(false) == Token.GreaterThan)
            {
                entity.ParsingInProgress = false;
                return;
            }

        UnexpectedError:
            OnUnexpectedError();
        }

        private async Task ParseNotationDeclAsync()
        {
            // notation name
            if (await GetTokenAsync(true).ConfigureAwait(false) != Token.Name)
            {
                OnUnexpectedError();
            }

            XmlQualifiedName notationName = GetNameQualified(false);
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

            // public / system id
            Token token = await GetTokenAsync(true).ConfigureAwait(false);
            if (token == Token.SYSTEM || token == Token.PUBLIC)
            {
                string notationPublicId, notationSystemId;

                var tuple_2 = await ParseExternalIdAsync(token, Token.NOTATION).ConfigureAwait(false);
                notationPublicId = tuple_2.Item1;
                notationSystemId = tuple_2.Item2;

                if (notation != null)
                {
                    notation.SystemLiteral = notationSystemId;
                    notation.Pubid = notationPublicId;
                }
            }
            else
            {
                OnUnexpectedError();
            }

            if (await GetTokenAsync(false).ConfigureAwait(false) != Token.GreaterThan)
                OnUnexpectedError();
        }

        private async Task ParseCommentAsync()
        {
            SaveParsingBuffer();
            try
            {
                if (SaveInternalSubsetValue)
                {
                    await _readerAdapter.ParseCommentAsync(_internalSubsetValueSb).ConfigureAwait(false);
                    _internalSubsetValueSb.Append("-->");
                }
                else
                {
                    await _readerAdapter.ParseCommentAsync(null).ConfigureAwait(false);
                }
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
            LoadParsingBuffer();
        }

        private async Task ParsePIAsync()
        {
            SaveParsingBuffer();
            if (SaveInternalSubsetValue)
            {
                await _readerAdapter.ParsePIAsync(_internalSubsetValueSb).ConfigureAwait(false);
                _internalSubsetValueSb.Append("?>");
            }
            else
            {
                await _readerAdapter.ParsePIAsync(null).ConfigureAwait(false);
            }
            LoadParsingBuffer();
        }

        private async Task ParseCondSectionAsync()
        {
            int csEntityId = _currentEntityId;

            switch (await GetTokenAsync(false).ConfigureAwait(false))
            {
                case Token.INCLUDE:
                    if (await GetTokenAsync(false).ConfigureAwait(false) != Token.LeftBracket)
                    {
                        goto default;
                    }
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
                    _condSectionDepth++;
                    break;
                case Token.IGNORE:
                    if (await GetTokenAsync(false).ConfigureAwait(false) != Token.LeftBracket)
                    {
                        goto default;
                    }
                    if (_validate && csEntityId != _currentEntityId)
                    {
                        SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                    }
                    // the content of the ignore section is parsed & skipped by scanning function
                    if (await GetTokenAsync(false).ConfigureAwait(false) != Token.CondSectionEnd)
                    {
                        goto default;
                    }
                    if (_validate && csEntityId != _currentEntityId)
                    {
                        SendValidationEvent(_curPos, XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, string.Empty);
                    }
                    break;
                default:
                    OnUnexpectedError();
                    break;
            }
        }

        private async Task<Tuple<string, string>> ParseExternalIdAsync(Token idTokenType, Token declType)
        {
            Tuple<string, string> tuple;
            string publicId;
            string systemId;

            LineInfo keywordLineInfo = new LineInfo(LineNo, LinePos - 6);
            publicId = null;
            systemId = null;

            if (await GetTokenAsync(true).ConfigureAwait(false) != Token.Literal)
            {
                ThrowUnexpectedToken(_curPos, "\"", "'");
            }

            if (idTokenType == Token.SYSTEM)
            {
                systemId = GetValue();

                if (systemId.Contains('#'))
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

                    if (await GetTokenAsync(false).ConfigureAwait(false) == Token.Literal)
                    {
                        if (!_whitespaceSeen)
                        {
                            Throw(SR.Xml_ExpectingWhiteSpace, char.ToString(_literalQuoteChar), (int)_literalLineInfo.lineNo, (int)_literalLineInfo.linePos);
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
                    if (await GetTokenAsync(false).ConfigureAwait(false) == Token.Literal)
                    {
                        if (!_whitespaceSeen)
                        {
                            Throw(SR.Xml_ExpectingWhiteSpace, char.ToString(_literalQuoteChar), (int)_literalLineInfo.lineNo, (int)_literalLineInfo.linePos);
                        }
                        systemId = GetValue();
                    }
                    else if (declType != Token.NOTATION)
                    {
                        ThrowUnexpectedToken(_curPos, "\"", "'");
                    }
                }
            }

            tuple = new Tuple<string, string>(publicId, systemId);
            return tuple;
        }
        //
        // Scanning methods - works directly with parsing buffer
        //
        private async Task<Token> GetTokenAsync(bool needWhiteSpace)
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
                                await HandleEntityReferenceAsync(true, false, false).ConfigureAwait(false);
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
                            case ScanningFunction.Name: return await ScanNameExpectedAsync().ConfigureAwait(false);
                            case ScanningFunction.QName: return await ScanQNameExpectedAsync().ConfigureAwait(false);
                            case ScanningFunction.Nmtoken: return await ScanNmtokenExpectedAsync().ConfigureAwait(false);
                            case ScanningFunction.SubsetContent: return await ScanSubsetContentAsync().ConfigureAwait(false);
                            case ScanningFunction.Doctype1: return await ScanDoctype1Async().ConfigureAwait(false);
                            case ScanningFunction.Doctype2: return ScanDoctype2();
                            case ScanningFunction.Element1: return await ScanElement1Async().ConfigureAwait(false);
                            case ScanningFunction.Element2: return await ScanElement2Async().ConfigureAwait(false);
                            case ScanningFunction.Element3: return await ScanElement3Async().ConfigureAwait(false);
                            case ScanningFunction.Element4: return ScanElement4();
                            case ScanningFunction.Element5: return ScanElement5();
                            case ScanningFunction.Element6: return ScanElement6();
                            case ScanningFunction.Element7: return ScanElement7();
                            case ScanningFunction.Attlist1: return await ScanAttlist1Async().ConfigureAwait(false);
                            case ScanningFunction.Attlist2: return await ScanAttlist2Async().ConfigureAwait(false);
                            case ScanningFunction.Attlist3: return ScanAttlist3();
                            case ScanningFunction.Attlist4: return ScanAttlist4();
                            case ScanningFunction.Attlist5: return ScanAttlist5();
                            case ScanningFunction.Attlist6: return await ScanAttlist6Async().ConfigureAwait(false);
                            case ScanningFunction.Attlist7: return ScanAttlist7();
                            case ScanningFunction.Notation1: return await ScanNotation1Async().ConfigureAwait(false);
                            case ScanningFunction.SystemId: return await ScanSystemIdAsync().ConfigureAwait(false);
                            case ScanningFunction.PublicId1: return await ScanPublicId1Async().ConfigureAwait(false);
                            case ScanningFunction.PublicId2: return await ScanPublicId2Async().ConfigureAwait(false);
                            case ScanningFunction.Entity1: return await ScanEntity1Async().ConfigureAwait(false);
                            case ScanningFunction.Entity2: return await ScanEntity2Async().ConfigureAwait(false);
                            case ScanningFunction.Entity3: return await ScanEntity3Async().ConfigureAwait(false);
                            case ScanningFunction.CondSection1: return await ScanCondSection1Async().ConfigureAwait(false);
                            case ScanningFunction.CondSection2: return ScanCondSection2();
                            case ScanningFunction.CondSection3: return await ScanCondSection3Async().ConfigureAwait(false);
                            case ScanningFunction.ClosingTag: return ScanClosingTag();
                            case ScanningFunction.ParamEntitySpace:
                                _whitespaceSeen = true;
                                _scanningFunction = _savedScanningFunction;
                                goto SwitchAgain;
                            default:
                                Debug.Fail($"Unexpected scanning function {_scanningFunction}");
                                return Token.None;
                        }
                }
            ReadData:
                if (_readerAdapter.IsEof || await ReadDataAsync().ConfigureAwait(false) == 0)
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

        private async Task<Token> ScanSubsetContentAsync()
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
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    Throw(_charsUsed, SR.Xml_IncompleteDtdContent);
                }
            }
        }

        private async Task<Token> ScanNameExpectedAsync()
        {
            await ScanNameAsync().ConfigureAwait(false);
            _scanningFunction = _nextScaningFunction;
            return Token.Name;
        }

        private async Task<Token> ScanQNameExpectedAsync()
        {
            await ScanQNameAsync().ConfigureAwait(false);
            _scanningFunction = _nextScaningFunction;
            return Token.QName;
        }

        private async Task<Token> ScanNmtokenExpectedAsync()
        {
            await ScanNmtokenAsync().ConfigureAwait(false);
            _scanningFunction = _nextScaningFunction;
            return Token.Nmtoken;
        }

        private async Task<Token> ScanDoctype1Async()
        {
            switch (_chars[_curPos])
            {
                case 'P':
                    if (!await EatPublicKeywordAsync().ConfigureAwait(false))
                    {
                        Throw(_curPos, SR.Xml_ExpectExternalOrClose);
                    }
                    _nextScaningFunction = ScanningFunction.Doctype2;
                    _scanningFunction = ScanningFunction.PublicId1;
                    return Token.PUBLIC;
                case 'S':
                    if (!await EatSystemKeywordAsync().ConfigureAwait(false))
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

        private async Task<Token> ScanElement1Async()
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
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    Throw(_curPos, SR.Xml_IncompleteDtdContent);
                }
            }
        }

        private async Task<Token> ScanElement2Async()
        {
            if (_chars[_curPos] == '#')
            {
                while (_charsUsed - _curPos < 7)
                {
                    if (await ReadDataAsync().ConfigureAwait(false) == 0)
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

        private async Task<Token> ScanElement3Async()
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
                    await ScanQNameAsync().ConfigureAwait(false);
                    _scanningFunction = ScanningFunction.Element4;
                    return Token.QName;
            }
        }

        private async Task<Token> ScanAttlist1Async()
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
                    await ScanQNameAsync().ConfigureAwait(false);
                    _scanningFunction = ScanningFunction.Attlist2;
                    return Token.QName;
            }
        }

        private async Task<Token> ScanAttlist2Async()
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
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    Throw(_curPos, SR.Xml_IncompleteDtdContent);
                }
            }
        }

        private async Task<Token> ScanAttlist6Async()
        {
            for (;;)
            {
                switch (_chars[_curPos])
                {
                    case '"':
                    case '\'':
                        await ScanLiteralAsync(LiteralType.AttributeValue).ConfigureAwait(false);
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
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    Throw(_curPos, SR.Xml_IncompleteDtdContent);
                }
            }
        }

        private async Task<Token> ScanLiteralAsync(LiteralType literalType)
        {
            Debug.Assert(_chars[_curPos] == '"' || _chars[_curPos] == '\'');

            char quoteChar = _chars[_curPos];
            char replaceChar = (literalType == LiteralType.AttributeValue) ? (char)0x20 : (char)0xA;
            int startQuoteEntityId = _currentEntityId;

            _literalLineInfo.Set(LineNo, LinePos);

            _curPos++;
            _tokenStartPos = _curPos;

            _stringBuilder.Length = 0;

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
                        await HandleEntityReferenceAsync(true, true, literalType == LiteralType.AttributeValue).ConfigureAwait(false);
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
                            int endPos = await _readerAdapter.ParseNumericCharRefAsync(SaveInternalSubsetValue ? _internalSubsetValueSb : null).ConfigureAwait(false);
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
                                int endPos = await _readerAdapter.ParseNamedCharRefAsync(true, SaveInternalSubsetValue ? _internalSubsetValueSb : null).ConfigureAwait(false);
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
                                    await HandleEntityReferenceAsync(false, true, true).ConfigureAwait(false);
                                    _tokenStartPos = _curPos;
                                }
                                continue;
                            }
                            else
                            {
                                int endPos = await _readerAdapter.ParseNamedCharRefAsync(false, null).ConfigureAwait(false);
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
                if (_readerAdapter.IsEof || await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    if (literalType == LiteralType.SystemOrPublicID || !HandleEntityEnd(true))
                    {
                        Throw(_curPos, SR.Xml_UnclosedQuote);
                    }
                }
                _tokenStartPos = _curPos;
            }
        }

        private async Task<Token> ScanNotation1Async()
        {
            switch (_chars[_curPos])
            {
                case 'P':
                    if (!await EatPublicKeywordAsync().ConfigureAwait(false))
                    {
                        Throw(_curPos, SR.Xml_ExpectExternalOrClose);
                    }
                    _nextScaningFunction = ScanningFunction.ClosingTag;
                    _scanningFunction = ScanningFunction.PublicId1;
                    return Token.PUBLIC;
                case 'S':
                    if (!await EatSystemKeywordAsync().ConfigureAwait(false))
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

        private async Task<Token> ScanSystemIdAsync()
        {
            if (_chars[_curPos] != '"' && _chars[_curPos] != '\'')
            {
                ThrowUnexpectedToken(_curPos, "\"", "'");
            }

            await ScanLiteralAsync(LiteralType.SystemOrPublicID).ConfigureAwait(false);

            _scanningFunction = _nextScaningFunction;
            return Token.Literal;
        }

        private async Task<Token> ScanEntity1Async()
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
                await ScanNameAsync().ConfigureAwait(false);
                _scanningFunction = ScanningFunction.Entity2;
                return Token.Name;
            }
        }

        private async Task<Token> ScanEntity2Async()
        {
            switch (_chars[_curPos])
            {
                case 'P':
                    if (!await EatPublicKeywordAsync().ConfigureAwait(false))
                    {
                        Throw(_curPos, SR.Xml_ExpectExternalOrClose);
                    }
                    _nextScaningFunction = ScanningFunction.Entity3;
                    _scanningFunction = ScanningFunction.PublicId1;
                    return Token.PUBLIC;
                case 'S':
                    if (!await EatSystemKeywordAsync().ConfigureAwait(false))
                    {
                        Throw(_curPos, SR.Xml_ExpectExternalOrClose);
                    }
                    _nextScaningFunction = ScanningFunction.Entity3;
                    _scanningFunction = ScanningFunction.SystemId;
                    return Token.SYSTEM;

                case '"':
                case '\'':
                    await ScanLiteralAsync(LiteralType.EntityReplText).ConfigureAwait(false);
                    _scanningFunction = ScanningFunction.ClosingTag;
                    return Token.Literal;
                default:
                    Throw(_curPos, SR.Xml_ExpectExternalIdOrEntityValue);
                    return Token.None;
            }
        }

        private async Task<Token> ScanEntity3Async()
        {
            if (_chars[_curPos] == 'N')
            {
                while (_charsUsed - _curPos < 5)
                {
                    if (await ReadDataAsync().ConfigureAwait(false) == 0)
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

        private async Task<Token> ScanPublicId1Async()
        {
            if (_chars[_curPos] != '"' && _chars[_curPos] != '\'')
            {
                ThrowUnexpectedToken(_curPos, "\"", "'");
            }

            await ScanLiteralAsync(LiteralType.SystemOrPublicID).ConfigureAwait(false);

            _scanningFunction = ScanningFunction.PublicId2;
            return Token.Literal;
        }

        private async Task<Token> ScanPublicId2Async()
        {
            if (_chars[_curPos] != '"' && _chars[_curPos] != '\'')
            {
                _scanningFunction = _nextScaningFunction;
                return Token.None;
            }

            await ScanLiteralAsync(LiteralType.SystemOrPublicID).ConfigureAwait(false);
            _scanningFunction = _nextScaningFunction;

            return Token.Literal;
        }

        private async Task<Token> ScanCondSection1Async()
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
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    Throw(_curPos, SR.Xml_IncompleteDtdContent);
                }
            }
        }

        private async Task<Token> ScanCondSection3Async()
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
                if (_readerAdapter.IsEof || await ReadDataAsync().ConfigureAwait(false) == 0)
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

        private Task ScanNameAsync()
        {
            return ScanQNameAsync(false);
        }

        private Task ScanQNameAsync()
        {
            return ScanQNameAsync(SupportNamespaces);
        }

        private async Task ScanQNameAsync(bool isQName)
        {
            _tokenStartPos = _curPos;
            int colonOffset = -1;

            for (;;)
            {
                //a tmp flag, used to avoid await keyword in unsafe context.
                bool awaitReadDataInNameAsync = false;
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
                            awaitReadDataInNameAsync = true;
                        }
                        else
                        {
                            Throw(_curPos, SR.Xml_BadStartNameChar, XmlException.BuildCharExceptionArgs(_chars, _charsUsed, _curPos));
                        }
                    }
                }

                if (awaitReadDataInNameAsync)
                {
                    if (await ReadDataInNameAsync().ConfigureAwait(false))
                    {
                        continue;
                    }
                    Throw(_curPos, SR.Xml_UnexpectedEOF, "Name");
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
                    if (await ReadDataInNameAsync().ConfigureAwait(false))
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

        private async Task<bool> ReadDataInNameAsync()
        {
            int offset = _curPos - _tokenStartPos;
            _curPos = _tokenStartPos;
            bool newDataRead = (await ReadDataAsync().ConfigureAwait(false) != 0);
            _tokenStartPos = _curPos;
            _curPos += offset;
            return newDataRead;
        }

        private async Task ScanNmtokenAsync()
        {
            _tokenStartPos = _curPos;

            for (;;)
            {
                unsafe
                {
                    for (;;)
                    {
                        if (_xmlCharType.IsNCNameSingleChar(_chars[_curPos]) || _chars[_curPos] == ':')
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
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
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

        private async Task<bool> EatPublicKeywordAsync()
        {
            Debug.Assert(_chars[_curPos] == 'P');
            while (_charsUsed - _curPos < 6)
            {
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
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

        private async Task<bool> EatSystemKeywordAsync()
        {
            Debug.Assert(_chars[_curPos] == 'S');
            while (_charsUsed - _curPos < 6)
            {
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
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
        // Parsing buffer maintainance methods
        //
        private async Task<int> ReadDataAsync()
        {
            SaveParsingBuffer();
            int charsRead = await _readerAdapter.ReadDataAsync().ConfigureAwait(false);
            LoadParsingBuffer();
            return charsRead;
        }

        //
        // Entity handling
        //
        private Task<bool> HandleEntityReferenceAsync(bool paramEntity, bool inLiteral, bool inAttribute)
        {
            Debug.Assert(_chars[_curPos] == '&' || _chars[_curPos] == '%');
            _curPos++;

            return HandleEntityReferenceAsync(ScanEntityName(), paramEntity, inLiteral, inAttribute);
        }

        private async Task<bool> HandleEntityReferenceAsync(XmlQualifiedName entityName, bool paramEntity, bool inLiteral, bool inAttribute)
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
                var tuple_3 = await _readerAdapter.PushEntityAsync(entity).ConfigureAwait(false);
                newEntityId = tuple_3.Item1;

                if (!tuple_3.Item2)
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

                var tuple_4 = await _readerAdapter.PushEntityAsync(entity).ConfigureAwait(false);
                newEntityId = tuple_4.Item1;

                if (!tuple_4.Item2)
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
    }
}

