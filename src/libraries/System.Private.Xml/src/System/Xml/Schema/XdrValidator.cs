// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
namespace System.Xml.Schema
{
    using System.IO;
    using System.Text;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.Versioning;
    using System.Diagnostics.CodeAnalysis;

#pragma warning disable 618
    internal sealed class XdrValidator : BaseValidator
    {
        private const int STACK_INCREMENT = 10;
        private HWStack _validationStack;  // validaton contexts
        private Hashtable _attPresence;
        private XmlQualifiedName _name = XmlQualifiedName.Empty;
        private XmlNamespaceManager _nsManager;
        private bool _isProcessContents;
        private Hashtable? _IDs;
        private IdRefNode? _idRefListHead;
        private Parser? _inlineSchemaParser;
        private const string x_schema = "x-schema:";

        internal XdrValidator(BaseValidator validator) : base(validator)
        {
            Init();
        }

        internal XdrValidator(XmlValidatingReaderImpl reader, XmlSchemaCollection schemaCollection, IValidationEventHandling? eventHandling) : base(reader, schemaCollection, eventHandling)
        {
            Init();
        }

        [MemberNotNull(nameof(_nsManager))]
        [MemberNotNull(nameof(_validationStack))]
        [MemberNotNull(nameof(_name))]
        [MemberNotNull(nameof(_attPresence))]
        private void Init()
        {
            _nsManager = reader.NamespaceManager!;
            if (_nsManager == null)
            {
                _nsManager = new XmlNamespaceManager(NameTable);
                _isProcessContents = true;
            }
            _validationStack = new HWStack(STACK_INCREMENT);
            textValue = new StringBuilder();
            _name = XmlQualifiedName.Empty;
            _attPresence = new Hashtable();
            Push(XmlQualifiedName.Empty);
            schemaInfo = new SchemaInfo();
            checkDatatype = false;
        }

        public override void Validate()
        {
            if (IsInlineSchemaStarted)
            {
                ProcessInlineSchema();
            }
            else
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        ValidateElement();
                        if (reader.IsEmptyElement)
                        {
                            goto case XmlNodeType.EndElement;
                        }
                        break;
                    case XmlNodeType.Whitespace:
                        ValidateWhitespace();
                        break;
                    case XmlNodeType.Text:          // text inside a node
                    case XmlNodeType.CDATA:         // <![CDATA[...]]>
                    case XmlNodeType.SignificantWhitespace:
                        ValidateText();
                        break;
                    case XmlNodeType.EndElement:
                        ValidateEndElement();
                        break;
                }
            }
        }

        private void ValidateElement()
        {
            elementName.Init(reader.LocalName, XmlSchemaDatatype.XdrCanonizeUri(reader.NamespaceURI, NameTable, SchemaNames));
            ValidateChildElement();
            if (SchemaNames.IsXDRRoot(elementName.Name, elementName.Namespace) && reader.Depth > 0)
            {
                _inlineSchemaParser = new Parser(SchemaType.XDR, NameTable, SchemaNames, EventHandler);
                _inlineSchemaParser.StartParsing(reader, null);
                _inlineSchemaParser.ParseReaderNode();
            }
            else
            {
                ProcessElement();
            }
        }

        private void ValidateChildElement()
        {
            if (context!.NeedValidateChildren)
            {
                int errorCode = 0;
                context.ElementDecl!.ContentValidator!.ValidateElement(elementName, context, out errorCode);
                if (errorCode < 0)
                {
                    XmlSchemaValidator.ElementValidationError(elementName, context, EventHandler, reader, reader.BaseURI, PositionInfo.LineNumber, PositionInfo.LinePosition, null);
                }
            }
        }

        private bool IsInlineSchemaStarted
        {
            get { return _inlineSchemaParser != null; }
        }

        private void ProcessInlineSchema()
        {
            if (!_inlineSchemaParser!.ParseReaderNode())
            { // Done
                _inlineSchemaParser.FinishParsing();
                SchemaInfo? xdrSchema = _inlineSchemaParser.XdrSchema;
                if (xdrSchema != null && xdrSchema.ErrorCount == 0)
                {
                    foreach (string inlineNS in xdrSchema.TargetNamespaces.Keys)
                    {
                        if (!this.schemaInfo!.HasSchema(inlineNS))
                        {
                            schemaInfo.Add(xdrSchema, EventHandler);
                            SchemaCollection!.Add(inlineNS, xdrSchema, null, false);
                            break;
                        }
                    }
                }
                _inlineSchemaParser = null;
            }
        }

        private void ProcessElement()
        {
            Push(elementName);
            if (_isProcessContents)
            {
                _nsManager.PopScope();
            }

            context!.ElementDecl = ThoroughGetElementDecl();
            if (context.ElementDecl != null)
            {
                ValidateStartElement();
                ValidateEndStartElement();
                context.NeedValidateChildren = true;
                context.ElementDecl.ContentValidator!.InitValidation(context);
            }
        }

        private void ValidateEndElement()
        {
            if (_isProcessContents)
            {
                _nsManager.PopScope();
            }

            if (context!.ElementDecl != null)
            {
                if (context.NeedValidateChildren)
                {
                    if (!context.ElementDecl.ContentValidator!.CompleteValidation(context))
                    {
                        XmlSchemaValidator.CompleteValidationError(context, EventHandler, reader, reader.BaseURI, PositionInfo.LineNumber, PositionInfo.LinePosition, null);
                    }
                }
                if (checkDatatype)
                {
                    string stringValue = !hasSibling ? textString! : textValue!.ToString();  // only for identity-constraint exception reporting
                    CheckValue(stringValue, null);
                    checkDatatype = false;
                    textValue!.Length = 0; // cleanup
                    textString = string.Empty;
                }
            }

            Pop();
        }

        // SxS: This method processes resource names read from the source document and does not expose
        // any resources to the caller. It is fine to suppress the SxS warning.
        private SchemaElementDecl? ThoroughGetElementDecl()
        {
            if (reader.Depth == 0)
            {
                LoadSchema(string.Empty);
            }
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    string objectNs = reader.NamespaceURI;
                    string objectName = reader.LocalName;
                    if (Ref.Equal(objectNs, SchemaNames.NsXmlNs))
                    {
                        LoadSchema(reader.Value);
                        if (_isProcessContents)
                        {
                            _nsManager.AddNamespace(reader.Prefix.Length == 0 ? string.Empty : reader.LocalName, reader.Value);
                        }
                    }
                    if (
                        Ref.Equal(objectNs, SchemaNames.QnDtDt.Namespace) &&
                        Ref.Equal(objectName, SchemaNames.QnDtDt.Name)
                    )
                    {
                        reader.SchemaTypeObject = XmlSchemaDatatype.FromXdrName(reader.Value);
                    }
                } while (reader.MoveToNextAttribute());
                reader.MoveToElement();
            }

            SchemaElementDecl? elementDecl = schemaInfo!.GetElementDecl(elementName);
            if (elementDecl == null)
            {
                if (schemaInfo.TargetNamespaces.ContainsKey(context!.Namespace!))
                {
                    SendValidationEvent(SR.Sch_UndeclaredElement, XmlSchemaValidator.QNameString(context.LocalName!, context.Namespace!));
                }
            }
            return elementDecl;
        }

        private void ValidateStartElement()
        {
            if (context!.ElementDecl != null)
            {
                if (context.ElementDecl.SchemaType != null)
                {
                    reader.SchemaTypeObject = context.ElementDecl.SchemaType;
                }
                else
                {
                    reader.SchemaTypeObject = context.ElementDecl.Datatype;
                }
                if (reader.IsEmptyElement && !context.IsNill && context.ElementDecl.DefaultValueTyped != null)
                {
                    reader.TypedValueObject = context.ElementDecl.DefaultValueTyped;
                    context.IsNill = true; // reusing IsNill
                }
                if (this.context.ElementDecl.HasRequiredAttribute)
                {
                    _attPresence.Clear();
                }
            }

            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    if ((object)reader.NamespaceURI == (object)SchemaNames.NsXmlNs)
                    {
                        continue;
                    }

                    try
                    {
                        reader.SchemaTypeObject = null;
                        SchemaAttDef? attnDef = schemaInfo!.GetAttributeXdr(context.ElementDecl, QualifiedName(reader.LocalName, reader.NamespaceURI));
                        if (attnDef != null)
                        {
                            if (context.ElementDecl != null && context.ElementDecl.HasRequiredAttribute)
                            {
                                _attPresence.Add(attnDef.Name, attnDef);
                            }
                            reader.SchemaTypeObject = (attnDef.SchemaType != null) ? (object)attnDef.SchemaType : (object)attnDef.Datatype;
                            if (attnDef.Datatype != null)
                            {
                                string attributeValue = reader.Value;
                                // need to check the contents of this attribute to make sure
                                // it is valid according to the specified attribute type.
                                CheckValue(attributeValue, attnDef);
                            }
                        }
                    }
                    catch (XmlSchemaException e)
                    {
                        e.SetSource(reader.BaseURI, PositionInfo.LineNumber, PositionInfo.LinePosition);
                        SendValidationEvent(e);
                    }
                } while (reader.MoveToNextAttribute());
                reader.MoveToElement();
            }
        }

        private void ValidateEndStartElement()
        {
            if (context!.ElementDecl!.HasDefaultAttribute)
            {
                for (int i = 0; i < context.ElementDecl.DefaultAttDefs!.Count; ++i)
                {
                    reader.AddDefaultAttribute((SchemaAttDef)context.ElementDecl.DefaultAttDefs[i]);
                }
            }

            if (context.ElementDecl.HasRequiredAttribute)
            {
                try
                {
                    context.ElementDecl.CheckAttributes(_attPresence, reader.StandAlone);
                }
                catch (XmlSchemaException e)
                {
                    e.SetSource(reader.BaseURI, PositionInfo.LineNumber, PositionInfo.LinePosition);
                    SendValidationEvent(e);
                }
            }

            if (context.ElementDecl.Datatype != null)
            {
                checkDatatype = true;
                hasSibling = false;
                textString = string.Empty;
                textValue!.Length = 0;
            }
        }

        private void LoadSchemaFromLocation(string uri)
        {
            // is x-schema
            if (!XdrBuilder.IsXdrSchema(uri))
            {
                return;
            }
            string url = uri.Substring(x_schema.Length);
            XmlReader? reader = null;
            SchemaInfo? xdrSchema = null;
            try
            {
                Uri ruri = this.XmlResolver!.ResolveUri(BaseUri, url);
                Stream stm = (Stream)this.XmlResolver.GetEntity(ruri, null, null)!;
                reader = new XmlTextReader(ruri.ToString(), stm, NameTable);
                ((XmlTextReader)reader).XmlResolver = this.XmlResolver;
                Parser parser = new Parser(SchemaType.XDR, NameTable, SchemaNames, EventHandler);
                parser.XmlResolver = this.XmlResolver;
                parser.Parse(reader, uri);
                while (reader.Read()) ; // wellformness check
                xdrSchema = parser.XdrSchema;
            }
            catch (XmlSchemaException e)
            {
                SendValidationEvent(SR.Sch_CannotLoadSchema, new string[] { uri, e.Message }, XmlSeverityType.Error);
            }
            catch (Exception e)
            {
                SendValidationEvent(SR.Sch_CannotLoadSchema, new string[] { uri, e.Message }, XmlSeverityType.Warning);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
            if (xdrSchema != null && xdrSchema.ErrorCount == 0)
            {
                schemaInfo!.Add(xdrSchema, EventHandler);
                SchemaCollection!.Add(uri, xdrSchema, null, false);
            }
        }

        private void LoadSchema(string uri)
        {
            if (this.schemaInfo!.TargetNamespaces.ContainsKey(uri))
            {
                return;
            }
            if (this.XmlResolver == null)
            {
                return;
            }

            SchemaInfo? schemaInfo = null;
            if (SchemaCollection != null)
                schemaInfo = SchemaCollection.GetSchemaInfo(uri);

            if (schemaInfo != null)
            {
                if (schemaInfo.SchemaType != SchemaType.XDR)
                {
                    throw new XmlException(SR.Xml_MultipleValidaitonTypes, string.Empty, this.PositionInfo.LineNumber, this.PositionInfo.LinePosition);
                }

                this.schemaInfo.Add(schemaInfo, EventHandler);
                return;
            }

            LoadSchemaFromLocation(uri);
        }

        private bool HasSchema { get { return schemaInfo!.SchemaType != SchemaType.None; } }

        public override bool PreserveWhitespace
        {
            get { return context!.ElementDecl != null ? context.ElementDecl.ContentValidator!.PreserveWhitespace : false; }
        }

        private void ProcessTokenizedType(
            XmlTokenizedType ttype,
            string name
        )
        {
            switch (ttype)
            {
                case XmlTokenizedType.ID:
                    if (FindId(name) != null)
                    {
                        SendValidationEvent(SR.Sch_DupId, name);
                    }
                    else
                    {
                        AddID(name, context!.LocalName);
                    }
                    break;
                case XmlTokenizedType.IDREF:
                    object? p = FindId(name);
                    if (p == null)
                    { // add it to linked list to check it later
                        _idRefListHead = new IdRefNode(_idRefListHead, name, this.PositionInfo.LineNumber, this.PositionInfo.LinePosition);
                    }
                    break;
                case XmlTokenizedType.ENTITY:
                    ProcessEntity(schemaInfo!, name, this, EventHandler, reader.BaseURI, PositionInfo.LineNumber, PositionInfo.LinePosition);
                    break;
                default:
                    break;
            }
        }


        public override void CompleteValidation()
        {
            if (HasSchema)
            {
                CheckForwardRefs();
            }
            else
            {
                SendValidationEvent(new XmlSchemaException(SR.Xml_NoValidation, string.Empty), XmlSeverityType.Warning);
            }
        }


        private void CheckValue(
            string value,
            SchemaAttDef? attdef
        )
        {
            try
            {
                reader.TypedValueObject = null;
                bool isAttn = attdef != null;
                XmlSchemaDatatype? dtype = isAttn ? attdef!.Datatype : context!.ElementDecl!.Datatype;

                if (dtype == null)
                {
                    return; // no reason to check
                }

                if (dtype.TokenizedType != XmlTokenizedType.CDATA)
                {
                    value = value.Trim();
                }
                if (value.Length == 0)
                {
                    return; // don't need to check
                }


                object typedValue = dtype.ParseValue(value, NameTable, _nsManager);
                reader.TypedValueObject = typedValue;
                // Check special types
                XmlTokenizedType ttype = dtype.TokenizedType;
                if (ttype == XmlTokenizedType.ENTITY || ttype == XmlTokenizedType.ID || ttype == XmlTokenizedType.IDREF)
                {
                    if (dtype.Variety == XmlSchemaDatatypeVariety.List)
                    {
                        string[] ss = (string[])typedValue;
                        for (int i = 0; i < ss.Length; ++i)
                        {
                            ProcessTokenizedType(dtype.TokenizedType, ss[i]);
                        }
                    }
                    else
                    {
                        ProcessTokenizedType(dtype.TokenizedType, (string)typedValue);
                    }
                }

                SchemaDeclBase decl = isAttn ? (SchemaDeclBase)attdef! : (SchemaDeclBase)context!.ElementDecl!;

                if (decl.MaxLength != uint.MaxValue)
                {
                    if (value.Length > decl.MaxLength)
                    {
                        SendValidationEvent(SR.Sch_MaxLengthConstraintFailed, value);
                    }
                }
                if (decl.MinLength != uint.MaxValue)
                {
                    if (value.Length < decl.MinLength)
                    {
                        SendValidationEvent(SR.Sch_MinLengthConstraintFailed, value);
                    }
                }
                if (decl.Values != null && !decl.CheckEnumeration(typedValue))
                {
                    if (dtype.TokenizedType == XmlTokenizedType.NOTATION)
                    {
                        SendValidationEvent(SR.Sch_NotationValue, typedValue.ToString());
                    }
                    else
                    {
                        SendValidationEvent(SR.Sch_EnumerationValue, typedValue.ToString());
                    }
                }
                if (!decl.CheckValue(typedValue))
                {
                    if (isAttn)
                    {
                        SendValidationEvent(SR.Sch_FixedAttributeValue, attdef!.Name.ToString());
                    }
                    else
                    {
                        SendValidationEvent(SR.Sch_FixedElementValue, XmlSchemaValidator.QNameString(context!.LocalName!, context.Namespace!));
                    }
                }
            }
            catch (XmlSchemaException)
            {
                if (attdef != null)
                {
                    SendValidationEvent(SR.Sch_AttributeValueDataType, attdef.Name.ToString());
                }
                else
                {
                    SendValidationEvent(SR.Sch_ElementValueDataType, XmlSchemaValidator.QNameString(context!.LocalName!, context.Namespace!));
                }
            }
        }

        public static void CheckDefaultValue(
            string value,
            SchemaAttDef attdef,
            SchemaInfo sinfo,
            XmlNamespaceManager nsManager,
            XmlNameTable NameTable,
            object? sender,
            ValidationEventHandler? eventhandler,
            string? baseUri,
            int lineNo,
            int linePos
        )
        {
            try
            {
                XmlSchemaDatatype dtype = attdef.Datatype;
                if (dtype == null)
                {
                    return; // no reason to check
                }

                if (dtype.TokenizedType != XmlTokenizedType.CDATA)
                {
                    value = value.Trim();
                }
                if (value.Length == 0)
                {
                    return; // don't need to check
                }
                object typedValue = dtype.ParseValue(value, NameTable, nsManager);

                // Check special types
                XmlTokenizedType ttype = dtype.TokenizedType;
                if (ttype == XmlTokenizedType.ENTITY)
                {
                    if (dtype.Variety == XmlSchemaDatatypeVariety.List)
                    {
                        string[] ss = (string[])typedValue;
                        for (int i = 0; i < ss.Length; ++i)
                        {
                            ProcessEntity(sinfo, ss[i], sender, eventhandler, baseUri, lineNo, linePos);
                        }
                    }
                    else
                    {
                        ProcessEntity(sinfo, (string)typedValue, sender, eventhandler, baseUri, lineNo, linePos);
                    }
                }
                else if (ttype == XmlTokenizedType.ENUMERATION)
                {
                    if (!attdef.CheckEnumeration(typedValue))
                    {
                        XmlSchemaException e = new XmlSchemaException(SR.Sch_EnumerationValue, typedValue.ToString(), baseUri, lineNo, linePos);
                        if (eventhandler != null)
                        {
                            eventhandler(sender, new ValidationEventArgs(e));
                        }
                        else
                        {
                            throw e;
                        }
                    }
                }
                attdef.DefaultValueTyped = typedValue;
            }
#if DEBUG
            catch (XmlSchemaException)
#else
            catch
#endif
            {
                XmlSchemaException e = new XmlSchemaException(SR.Sch_AttributeDefaultDataType, attdef.Name.ToString(), baseUri, lineNo, linePos);
                if (eventhandler != null)
                {
                    eventhandler(sender, new ValidationEventArgs(e));
                }
                else
                {
                    throw e;
                }
            }
        }

        internal void AddID(string name, object? node)
        {
            // Note: It used to be true that we only called this if _fValidate was true,
            // but due to the fact that you can now dynamically type somethign as an ID
            // that is no longer true.
            if (_IDs == null)
            {
                _IDs = new Hashtable();
            }

            _IDs.Add(name, node);
        }

        public override object? FindId(string name)
        {
            return _IDs == null ? null : _IDs[name];
        }

        private void Push(XmlQualifiedName elementName)
        {
            context = (ValidationState)_validationStack.Push();
            if (context == null)
            {
                context = new ValidationState();
                _validationStack.AddToTop(context);
            }

            context.LocalName = elementName.Name;
            context.Namespace = elementName.Namespace;
            context.HasMatched = false;
            context.IsNill = false;
            context.NeedValidateChildren = false;
        }

        private void Pop()
        {
            if (_validationStack.Length > 1)
            {
                _validationStack.Pop();
                context = (ValidationState?)_validationStack.Peek();
            }
        }

        private void CheckForwardRefs()
        {
            IdRefNode? next = _idRefListHead;
            while (next != null)
            {
                if (FindId(next.Id) == null)
                {
                    SendValidationEvent(new XmlSchemaException(SR.Sch_UndeclaredId, next.Id, reader.BaseURI, next.LineNo, next.LinePos));
                }

                IdRefNode? ptr = next.Next;
                next.Next = null; // unhook each object so it is cleaned up by Garbage Collector
                next = ptr;
            }
            // not needed any more.
            _idRefListHead = null;
        }

        private XmlQualifiedName QualifiedName(string name, string ns)
        {
            return new XmlQualifiedName(name, XmlSchemaDatatype.XdrCanonizeUri(ns, NameTable, SchemaNames));
        }
    };
#pragma warning restore 618
}
