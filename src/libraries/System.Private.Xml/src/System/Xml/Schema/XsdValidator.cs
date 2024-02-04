// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml.Schema
{
#pragma warning disable 618
    internal sealed class XsdValidator : BaseValidator
    {
        private int _startIDConstraint = -1;
        private const int STACK_INCREMENT = 10;
        private HWStack _validationStack;  // validaton contexts

        private Hashtable _attPresence;
        private XmlNamespaceManager _nsManager;
        private bool _bManageNamespaces;
        private Hashtable? _IDs;
        private IdRefNode? _idRefListHead;
        private Parser? _inlineSchemaParser;
        private XmlSchemaContentProcessing _processContents;

        private static readonly XmlSchemaDatatype s_dtCDATA = XmlSchemaDatatype.FromXmlTokenizedType(XmlTokenizedType.CDATA)!;
        private static readonly XmlSchemaDatatype s_dtQName = XmlSchemaDatatype.FromXmlTokenizedTypeXsd(XmlTokenizedType.QName)!;
        private static readonly XmlSchemaDatatype s_dtStringArray = s_dtCDATA.DeriveByList(null);

        //To avoid SchemaNames creation
        private string _nsXmlNs;
        private string _nsXs;
        private string _nsXsi;
        private string _xsiType;
        private string _xsiNil;
        private string _xsiSchemaLocation;
        private string _xsiNoNamespaceSchemaLocation;
        private string _xsdSchema;


        internal XsdValidator(BaseValidator validator) : base(validator)
        {
            Init();
        }

        internal XsdValidator(XmlValidatingReaderImpl reader, XmlSchemaCollection schemaCollection, IValidationEventHandling eventHandling) : base(reader, schemaCollection, eventHandling)
        {
            Init();
        }

        [MemberNotNull(nameof(_nsManager))]
        [MemberNotNull(nameof(_validationStack))]
        [MemberNotNull(nameof(_attPresence))]
        [MemberNotNull(nameof(_processContents))]
        [MemberNotNull(nameof(_nsXmlNs))]
        [MemberNotNull(nameof(_nsXs))]
        [MemberNotNull(nameof(_nsXsi))]
        [MemberNotNull(nameof(_xsiType))]
        [MemberNotNull(nameof(_xsiNil))]
        [MemberNotNull(nameof(_xsiSchemaLocation))]
        [MemberNotNull(nameof(_xsiNoNamespaceSchemaLocation))]
        [MemberNotNull(nameof(_xsdSchema))]
        private void Init()
        {
            _nsManager = reader.NamespaceManager!;
            if (_nsManager == null)
            {
                _nsManager = new XmlNamespaceManager(NameTable);
                _bManageNamespaces = true;
            }

            _validationStack = new HWStack(STACK_INCREMENT);
            textValue = new StringBuilder();
            _attPresence = new Hashtable();
            schemaInfo = new SchemaInfo();
            checkDatatype = false;
            _processContents = XmlSchemaContentProcessing.Strict;
            Push(XmlQualifiedName.Empty);

            //Add common strings to be compared to NameTable
            _nsXmlNs = NameTable.Add(XmlReservedNs.NsXmlNs);
            _nsXs = NameTable.Add(XmlReservedNs.NsXs);
            _nsXsi = NameTable.Add(XmlReservedNs.NsXsi);
            _xsiType = NameTable.Add("type");
            _xsiNil = NameTable.Add("nil");
            _xsiSchemaLocation = NameTable.Add("schemaLocation");
            _xsiNoNamespaceSchemaLocation = NameTable.Add("noNamespaceSchemaLocation");
            _xsdSchema = NameTable.Add("schema");
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


        public override void CompleteValidation()
        {
            CheckForwardRefs();
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
                XmlSchema? schema = _inlineSchemaParser.XmlSchema;
                string? inlineNS;
                if (schema != null && schema.ErrorCount == 0)
                {
                    try
                    {
                        SchemaInfo inlineSchemaInfo = new SchemaInfo();
                        inlineSchemaInfo.SchemaType = SchemaType.XSD;
                        inlineNS = schema.TargetNamespace ?? string.Empty;
                        if (!SchemaInfo!.TargetNamespaces.ContainsKey(inlineNS))
                        {
                            if (SchemaCollection!.Add(inlineNS, inlineSchemaInfo, schema, true) != null)
                            { //If no errors on compile
                                //Add to validator's SchemaInfo
                                SchemaInfo.Add(inlineSchemaInfo, EventHandler);
                            }
                        }
                    }
                    catch (XmlSchemaException e)
                    {
                        SendValidationEvent(SR.Sch_CannotLoadSchema, new string[] { BaseUri!.AbsoluteUri, e.Message }, XmlSeverityType.Error);
                    }
                }
                _inlineSchemaParser = null;
            }
        }

        private void ValidateElement()
        {
            elementName.Init(reader.LocalName, reader.NamespaceURI);
            object? particle = ValidateChildElement();
            if (IsXSDRoot(elementName.Name, elementName.Namespace) && reader.Depth > 0)
            {
                _inlineSchemaParser = new Parser(SchemaType.XSD, NameTable, SchemaNames, EventHandler);
                _inlineSchemaParser.StartParsing(reader, null);
                ProcessInlineSchema();
            }
            else
            {
                ProcessElement(particle);
            }
        }

        private object? ValidateChildElement()
        {
            object? particle = null;
            int errorCode;
            if (context!.NeedValidateChildren)
            {
                if (context.IsNill)
                {
                    SendValidationEvent(SR.Sch_ContentInNill, elementName.ToString());
                    return null;
                }
                particle = context.ElementDecl!.ContentValidator!.ValidateElement(elementName, context, out errorCode);
                if (particle == null)
                {
                    _processContents = context.ProcessContents = XmlSchemaContentProcessing.Skip;
                    if (errorCode == -2)
                    { //ContentModel all group error
                        SendValidationEvent(SR.Sch_AllElement, elementName.ToString());
                    }
                    XmlSchemaValidator.ElementValidationError(elementName, context, EventHandler, reader, reader.BaseURI, PositionInfo.LineNumber, PositionInfo.LinePosition, null);
                }
            }
            return particle;
        }

        private void ProcessElement(object? particle)
        {
            XmlQualifiedName xsiType;
            string? xsiNil;
            SchemaElementDecl? elementDecl = FastGetElementDecl(particle);
            Push(elementName);
            if (_bManageNamespaces)
            {
                _nsManager.PushScope();
            }
            ProcessXsiAttributes(out xsiType, out xsiNil);
            if (_processContents != XmlSchemaContentProcessing.Skip)
            {
                if (elementDecl == null || !xsiType.IsEmpty || xsiNil != null)
                {
                    elementDecl = ThoroughGetElementDecl(elementDecl, xsiType, xsiNil);
                }
                if (elementDecl == null)
                {
                    if (HasSchema && _processContents == XmlSchemaContentProcessing.Strict)
                    {
                        SendValidationEvent(SR.Sch_UndeclaredElement, XmlSchemaValidator.QNameString(context!.LocalName!, context.Namespace!));
                    }
                    else
                    {
                        SendValidationEvent(SR.Sch_NoElementSchemaFound, XmlSchemaValidator.QNameString(context!.LocalName!, context.Namespace!), XmlSeverityType.Warning);
                    }
                }
            }

            context!.ElementDecl = elementDecl;
            ValidateStartElementIdentityConstraints();
            ValidateStartElement();
            if (context.ElementDecl != null)
            {
                ValidateEndStartElement();
                context.NeedValidateChildren = _processContents != XmlSchemaContentProcessing.Skip;
                context.ElementDecl.ContentValidator!.InitValidation(context);
            }
        }

        // SxS: This method processes attributes read from source document and does not expose any resources.
        // It's OK to suppress the SxS warning.
        private void ProcessXsiAttributes(out XmlQualifiedName xsiType, out string? xsiNil)
        {
            string[]? xsiSchemaLocation = null;
            string? xsiNoNamespaceSchemaLocation = null;
            xsiType = XmlQualifiedName.Empty;
            xsiNil = null;

            if (reader.Depth == 0)
            {
                //Load schema for empty namespace
                LoadSchema(string.Empty, null);

                //Should load schemas for namespaces already added to nsManager
                foreach (string ns in _nsManager.GetNamespacesInScope(XmlNamespaceScope.ExcludeXml).Values)
                {
                    LoadSchema(ns, null);
                }
            }

            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    string objectNs = reader.NamespaceURI;
                    string objectName = reader.LocalName;
                    if (Ref.Equal(objectNs, _nsXmlNs))
                    {
                        LoadSchema(reader.Value, null);
                        if (_bManageNamespaces)
                        {
                            _nsManager.AddNamespace(reader.Prefix.Length == 0 ? string.Empty : reader.LocalName, reader.Value);
                        }
                    }
                    else if (Ref.Equal(objectNs, _nsXsi))
                    {
                        if (Ref.Equal(objectName, _xsiSchemaLocation))
                        {
                            xsiSchemaLocation = (string[])s_dtStringArray.ParseValue(reader.Value, NameTable, _nsManager);
                        }
                        else if (Ref.Equal(objectName, _xsiNoNamespaceSchemaLocation))
                        {
                            xsiNoNamespaceSchemaLocation = reader.Value;
                        }
                        else if (Ref.Equal(objectName, _xsiType))
                        {
                            xsiType = (XmlQualifiedName)s_dtQName.ParseValue(reader.Value, NameTable, _nsManager);
                        }
                        else if (Ref.Equal(objectName, _xsiNil))
                        {
                            xsiNil = reader.Value;
                        }
                    }
                } while (reader.MoveToNextAttribute());
                reader.MoveToElement();
            }
            if (xsiNoNamespaceSchemaLocation != null)
            {
                LoadSchema(string.Empty, xsiNoNamespaceSchemaLocation);
            }
            if (xsiSchemaLocation != null)
            {
                for (int i = 0; i < xsiSchemaLocation.Length - 1; i += 2)
                {
                    LoadSchema((string)xsiSchemaLocation[i], (string)xsiSchemaLocation[i + 1]);
                }
            }
        }

        private void ValidateEndElement()
        {
            if (_bManageNamespaces)
            {
                _nsManager.PopScope();
            }
            if (context!.ElementDecl != null)
            {
                if (!context.IsNill)
                {
                    if (context.NeedValidateChildren)
                    {
                        if (!context.ElementDecl.ContentValidator!.CompleteValidation(context))
                        {
                            XmlSchemaValidator.CompleteValidationError(context, EventHandler, reader, reader.BaseURI, PositionInfo.LineNumber, PositionInfo.LinePosition, null);
                        }
                    }

                    if (checkDatatype && !context.IsNill)
                    {
                        string stringValue = !hasSibling ? textString! : textValue!.ToString();  // only for identity-constraint exception reporting
                        if (!(stringValue.Length == 0 && context.ElementDecl.DefaultValueTyped != null))
                        {
                            CheckValue(stringValue, null);
                            checkDatatype = false;
                        }
                    }
                }

                // for each level in the stack, endchildren and fill value from element
                if (HasIdentityConstraints)
                {
                    EndElementIdentityConstraints();
                }
            }
            Pop();
        }

        private SchemaElementDecl? FastGetElementDecl(object? particle)
        {
            SchemaElementDecl? elementDecl = null;
            if (particle != null)
            {
                XmlSchemaElement? element = particle as XmlSchemaElement;
                if (element != null)
                {
                    elementDecl = element.ElementDecl;
                }
                else
                {
                    XmlSchemaAny any = (XmlSchemaAny)particle;
                    _processContents = any.ProcessContentsCorrect;
                }
            }
            return elementDecl;
        }

        private SchemaElementDecl? ThoroughGetElementDecl(SchemaElementDecl? elementDecl, XmlQualifiedName xsiType, string? xsiNil)
        {
            elementDecl ??= schemaInfo!.GetElementDecl(elementName);
            if (elementDecl != null)
            {
                if (xsiType.IsEmpty)
                {
                    if (elementDecl.IsAbstract)
                    {
                        SendValidationEvent(SR.Sch_AbstractElement, XmlSchemaValidator.QNameString(context!.LocalName!, context.Namespace!));
                        elementDecl = null;
                    }
                }
                else if (xsiNil != null && xsiNil.Equals("true"))
                {
                    SendValidationEvent(SR.Sch_XsiNilAndType);
                }
                else
                {
                    SchemaElementDecl? elementDeclXsi;
                    if (!schemaInfo!.ElementDeclsByType.TryGetValue(xsiType, out elementDeclXsi) && xsiType.Namespace == _nsXs)
                    {
                        XmlSchemaSimpleType? simpleType = DatatypeImplementation.GetSimpleTypeFromXsdType(new XmlQualifiedName(xsiType.Name, _nsXs));
                        if (simpleType != null)
                        {
                            elementDeclXsi = simpleType.ElementDecl;
                        }
                    }
                    if (elementDeclXsi == null)
                    {
                        SendValidationEvent(SR.Sch_XsiTypeNotFound, xsiType.ToString());
                        elementDecl = null;
                    }
                    else if (!XmlSchemaType.IsDerivedFrom(elementDeclXsi.SchemaType, elementDecl.SchemaType, elementDecl.Block))
                    {
                        SendValidationEvent(SR.Sch_XsiTypeBlockedEx, new string?[] { xsiType.ToString(), XmlSchemaValidator.QNameString(context!.LocalName!, context.Namespace!) });
                        elementDecl = null;
                    }
                    else
                    {
                        elementDecl = elementDeclXsi;
                    }
                }
                if (elementDecl != null && elementDecl.IsNillable)
                {
                    if (xsiNil != null)
                    {
                        context!.IsNill = XmlConvert.ToBoolean(xsiNil);
                        if (context.IsNill && elementDecl.DefaultValueTyped != null)
                        {
                            SendValidationEvent(SR.Sch_XsiNilAndFixed);
                        }
                    }
                }
                else if (xsiNil != null)
                {
                    SendValidationEvent(SR.Sch_InvalidXsiNill);
                }
            }
            return elementDecl;
        }

        private void ValidateStartElement()
        {
            if (context!.ElementDecl != null)
            {
                if (context.ElementDecl.IsAbstract)
                {
                    SendValidationEvent(SR.Sch_AbstractElement, XmlSchemaValidator.QNameString(context.LocalName!, context.Namespace!));
                }

                reader.SchemaTypeObject = context.ElementDecl.SchemaType;

                if (reader.IsEmptyElement && !context.IsNill && context.ElementDecl.DefaultValueTyped != null)
                {
                    reader.TypedValueObject = UnWrapUnion(context.ElementDecl.DefaultValueTyped);
                    context.IsNill = true; // reusing IsNill
                }
                else
                {
                    reader.TypedValueObject = null; //Typed value cleanup
                }
                if (this.context.ElementDecl.HasRequiredAttribute || HasIdentityConstraints)
                {
                    _attPresence.Clear();
                }
            }

            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    if ((object)reader.NamespaceURI == (object)_nsXmlNs)
                    {
                        continue;
                    }
                    if ((object)reader.NamespaceURI == (object)_nsXsi)
                    {
                        continue;
                    }

                    try
                    {
                        reader.SchemaTypeObject = null;
                        XmlQualifiedName attQName = new XmlQualifiedName(reader.LocalName, reader.NamespaceURI);
                        bool skipContents = (_processContents == XmlSchemaContentProcessing.Skip);
                        SchemaAttDef? attnDef = schemaInfo!.GetAttributeXsd(context.ElementDecl, attQName, ref skipContents);

                        if (attnDef != null)
                        {
                            if (context.ElementDecl != null && (context.ElementDecl.HasRequiredAttribute || _startIDConstraint != -1))
                            {
                                _attPresence.Add(attnDef.Name, attnDef);
                            }
                            Debug.Assert(attnDef.SchemaType != null);
                            reader.SchemaTypeObject = attnDef.SchemaType;
                            if (attnDef.Datatype != null)
                            {
                                // need to check the contents of this attribute to make sure
                                // it is valid according to the specified attribute type.
                                CheckValue(reader.Value, attnDef);
                            }
                            if (HasIdentityConstraints)
                            {
                                AttributeIdentityConstraints(reader.LocalName, reader.NamespaceURI, reader.TypedValueObject, reader.Value, attnDef);
                            }
                        }
                        else if (!skipContents)
                        {
                            if (context.ElementDecl == null
                                && _processContents == XmlSchemaContentProcessing.Strict
                                && attQName.Namespace.Length != 0
                                && schemaInfo.Contains(attQName.Namespace)
                                )
                            {
                                SendValidationEvent(SR.Sch_UndeclaredAttribute, attQName.ToString());
                            }
                            else
                            {
                                SendValidationEvent(SR.Sch_NoAttributeSchemaFound, attQName.ToString(), XmlSeverityType.Warning);
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
                    SchemaAttDef attdef = (SchemaAttDef)context.ElementDecl.DefaultAttDefs[i];
                    reader.AddDefaultAttribute(attdef);
                    // even default attribute i have to move to... but can't exist
                    if (HasIdentityConstraints && !_attPresence.Contains(attdef.Name))
                    {
                        AttributeIdentityConstraints(attdef.Name.Name, attdef.Name.Namespace, UnWrapUnion(attdef.DefaultValueTyped), attdef.DefaultValueRaw, attdef);
                    }
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

        private void LoadSchemaFromLocation(string uri, string url)
        {
            XmlTextReader? reader = null;
            SchemaInfo? schemaInfo = null;

            try
            {
                Uri ruri = this.XmlResolver!.ResolveUri(BaseUri, url);
                Stream stm = (Stream)this.XmlResolver.GetEntity(ruri, null, null)!;
                reader = new XmlTextReader(ruri.ToString(), stm, NameTable);
                //XmlSchema schema = SchemaCollection.Add(uri, reader, this.XmlResolver);

                Parser parser = new Parser(SchemaType.XSD, NameTable, SchemaNames, EventHandler);
                parser.XmlResolver = this.XmlResolver;
                SchemaType schemaType = parser.Parse(reader, uri);

                schemaInfo = new SchemaInfo();
                schemaInfo.SchemaType = schemaType;
                if (schemaType == SchemaType.XSD)
                {
                    if (SchemaCollection!.EventHandler == null)
                    {
                        SchemaCollection.EventHandler = this.EventHandler;
                    }
                    SchemaCollection.Add(uri, schemaInfo, parser.XmlSchema, true);
                }
                //Add to validator's SchemaInfo
                SchemaInfo!.Add(schemaInfo, EventHandler);

                while (reader.Read()) ; // wellformness check
            }
            catch (XmlSchemaException e)
            {
                schemaInfo = null;
                SendValidationEvent(SR.Sch_CannotLoadSchema, new string[] { uri, e.Message }, XmlSeverityType.Error);
            }
            catch (Exception e)
            {
                schemaInfo = null;
                SendValidationEvent(SR.Sch_CannotLoadSchema, new string[] { uri, e.Message }, XmlSeverityType.Warning);
            }
            finally
            {
                reader?.Close();
            }
        }

        private void LoadSchema(string uri, string? url)
        {
            if (this.XmlResolver == null)
            {
                return;
            }
            if (SchemaInfo!.TargetNamespaces.ContainsKey(uri) && _nsManager.LookupPrefix(uri) != null)
            {
                return;
            }

            SchemaInfo? schemaInfo = null;
            if (SchemaCollection != null)
                schemaInfo = SchemaCollection.GetSchemaInfo(uri);
            if (schemaInfo != null)
            {
                if (schemaInfo.SchemaType != SchemaType.XSD)
                {
                    throw new XmlException(SR.Xml_MultipleValidationTypes, string.Empty, this.PositionInfo.LineNumber, this.PositionInfo.LinePosition);
                }
                SchemaInfo.Add(schemaInfo, EventHandler);
                return;
            }
            if (url != null)
            {
                LoadSchemaFromLocation(uri, url);
            }
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
                        AddID(name, context!.LocalName!);
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

                object? typedValue = dtype.ParseValue(value, NameTable, _nsManager, true);

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
                if (dtype.Variety == XmlSchemaDatatypeVariety.Union)
                {
                    typedValue = UnWrapUnion(typedValue);
                }
                reader.TypedValueObject = typedValue;
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



        internal void AddID(string name, object node)
        {
            // Note: It used to be true that we only called this if _fValidate was true,
            // but due to the fact that you can now dynamically type somethign as an ID
            // that is no longer true.
            _IDs ??= new Hashtable();

            _IDs.Add(name, node);
        }

        public override object? FindId(string name)
        {
            return _IDs?[name];
        }

        public bool IsXSDRoot(string localName, string ns)
        {
            return Ref.Equal(ns, _nsXs) && Ref.Equal(localName, _xsdSchema);
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
            context.ProcessContents = _processContents;
            context.NeedValidateChildren = false;
            context.Constr = null; //resetting the constraints to be null incase context != null
                                   // when pushing onto stack;
        }


        private void Pop()
        {
            if (_validationStack.Length > 1)
            {
                _validationStack.Pop();
                if (_startIDConstraint == _validationStack.Length)
                {
                    _startIDConstraint = -1;
                }
                context = (ValidationState?)_validationStack.Peek();
                _processContents = context!.ProcessContents;
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


        private void ValidateStartElementIdentityConstraints()
        {
            // added on June 15, set the context here, so the stack can have them
            if (context!.ElementDecl != null)
            {
                if (context.ElementDecl.Constraints != null)
                {
                    AddIdentityConstraints();
                }
                //foreach constraint in stack (including the current one)
                if (HasIdentityConstraints)
                {
                    ElementIdentityConstraints();
                }
            }
        }

        private bool HasIdentityConstraints
        {
            get { return _startIDConstraint != -1; }
        }

        private void AddIdentityConstraints()
        {
            context!.Constr = new ConstraintStruct[context.ElementDecl!.Constraints!.Length];
            int id = 0;
            for (int i = 0; i < context.ElementDecl.Constraints.Length; ++i)
            {
                context.Constr[id++] = new ConstraintStruct(context.ElementDecl.Constraints[i]);
            } // foreach constraint /constraintstruct

            // added on June 19, make connections between new keyref tables with key/unique tables in stack
            // i can't put it in the above loop, because there will be key on the same level
            for (int i = 0; i < context.Constr.Length; ++i)
            {
                if (context.Constr[i].constraint.Role == CompiledIdentityConstraint.ConstraintRole.Keyref)
                {
                    bool find = false;
                    // go upwards checking or only in this level
                    for (int level = _validationStack.Length - 1; level >= ((_startIDConstraint >= 0) ? _startIDConstraint : _validationStack.Length - 1); level--)
                    {
                        // no constraint for this level
                        if (((ValidationState)(_validationStack[level])).Constr == null)
                        {
                            continue;
                        }
                        // else
                        ConstraintStruct[] constraints = ((ValidationState)_validationStack[level]).Constr!;
                        for (int j = 0; j < constraints.Length; ++j)
                        {
                            if (constraints[j].constraint.name == context.Constr[i].constraint.refer)
                            {
                                find = true;
                                if (constraints[j].keyrefTable == null)
                                {
                                    constraints[j].keyrefTable = new Hashtable();
                                }
                                context.Constr[i].qualifiedTable = constraints[j].keyrefTable;
                                break;
                            }
                        }

                        if (find)
                        {
                            break;
                        }
                    }
                    if (!find)
                    {
                        // didn't find connections, throw exceptions
                        SendValidationEvent(SR.Sch_RefNotInScope, XmlSchemaValidator.QNameString(context.LocalName!, context.Namespace!));
                    }
                } // finished dealing with keyref
            }  // end foreach

            // initial set
            if (_startIDConstraint == -1)
            {
                _startIDConstraint = _validationStack.Length - 1;
            }
        }

        private void ElementIdentityConstraints()
        {
            for (int i = _startIDConstraint; i < _validationStack.Length; i++)
            {
                // no constraint for this level
                if (((ValidationState)(_validationStack[i])).Constr == null)
                {
                    continue;
                }

                // else
                ConstraintStruct[] constraints = ((ValidationState)_validationStack[i]).Constr!;
                for (int j = 0; j < constraints.Length; ++j)
                {
                    // check selector from here
                    if (constraints[j].axisSelector.MoveToStartElement(reader.LocalName, reader.NamespaceURI))
                    {
                        // selector selects new node, activate a new set of fields
                        Debug.WriteLine("Selector Match!");
                        Debug.WriteLine($"Name: {reader.LocalName}\t|\tURI: {reader.NamespaceURI}\n");
                        // in which axisFields got updated
                        constraints[j].axisSelector.PushKS(PositionInfo.LineNumber, PositionInfo.LinePosition);
                    }

                    // axisFields is not null, but may be empty
                    for (int k = 0; k < constraints[j].axisFields.Count; ++k)
                    {
                        LocatedActiveAxis laxis = (LocatedActiveAxis)constraints[j].axisFields[k]!;

                        // check field from here
                        if (laxis.MoveToStartElement(reader.LocalName, reader.NamespaceURI))
                        {
                            Debug.WriteLine("Element Field Match!");
                            // checking simpleType / simpleContent
                            if (context!.ElementDecl != null)
                            {      // nextElement can be null when xml/xsd are not valid
                                if (context.ElementDecl.Datatype == null)
                                {
                                    SendValidationEvent(SR.Sch_FieldSimpleTypeExpected, reader.LocalName);
                                }
                                else
                                {
                                    // can't fill value here, wait till later....
                                    // fill type : xsdType
                                    laxis.isMatched = true;
                                    // since it's simpletyped element, the endchildren will come consequently... don't worry
                                }
                            }
                        }
                    }
                }
            }
        }

        // facilitate modifying
        private void AttributeIdentityConstraints(string name, string ns, object? obj, string sobj, SchemaAttDef attdef)
        {
            for (int ci = _startIDConstraint; ci < _validationStack.Length; ci++)
            {
                // no constraint for this level
                if (((ValidationState)(_validationStack[ci])).Constr == null)
                {
                    continue;
                }

                // else
                ConstraintStruct[] constraints = ((ValidationState)_validationStack[ci]).Constr!;
                for (int i = 0; i < constraints.Length; ++i)
                {
                    // axisFields is not null, but may be empty
                    for (int j = 0; j < constraints[i].axisFields.Count; ++j)
                    {
                        LocatedActiveAxis laxis = (LocatedActiveAxis)constraints[i].axisFields[j]!;

                        // check field from here
                        if (laxis.MoveToAttribute(name, ns))
                        {
                            Debug.WriteLine("Attribute Field Match!");
                            //attribute is only simpletype, so needn't checking...
                            // can fill value here, yeah!!
                            Debug.WriteLine("Attribute Field Filling Value!");
                            Debug.WriteLine($"Name: {name}\t|\tURI: {ns}\t|\tValue: {obj}\n");
                            if (laxis.Ks[laxis.Column] != null)
                            {
                                // should be evaluated to either an empty node-set or a node-set with exactly one member
                                // two matches...
                                SendValidationEvent(SR.Sch_FieldSingleValueExpected, name);
                            }
                            else if ((attdef != null) && (attdef.Datatype != null))
                            {
                                laxis.Ks[laxis.Column] = new TypedObject(obj, sobj, attdef.Datatype);
                            }
                        }
                    }
                }
            }
        }

        private static object? UnWrapUnion(object? typedValue)
        {
            XsdSimpleValue? simpleValue = typedValue as XsdSimpleValue;
            if (simpleValue != null)
            {
                typedValue = simpleValue.TypedValue;
            }

            return typedValue;
        }

        private void EndElementIdentityConstraints()
        {
            for (int ci = _validationStack.Length - 1; ci >= _startIDConstraint; ci--)
            {
                // no constraint for this level
                if (((ValidationState)(_validationStack[ci])).Constr == null)
                {
                    continue;
                }

                // else
                ConstraintStruct[] constraints = ((ValidationState)_validationStack[ci]).Constr!;
                for (int i = 0; i < constraints.Length; ++i)
                {
                    // EndChildren
                    // axisFields is not null, but may be empty
                    for (int j = 0; j < constraints[i].axisFields.Count; ++j)
                    {
                        LocatedActiveAxis laxis = (LocatedActiveAxis)constraints[i].axisFields[j]!;
                        // check field from here
                        // isMatched is false when nextElement is null. so needn't change this part.
                        if (laxis.isMatched)
                        {
                            Debug.WriteLine("Element Field Filling Value!");
                            Debug.WriteLine($"Name: {reader.LocalName}\t|\tURI: {reader.NamespaceURI}\t|\tValue: {reader.TypedValueObject}\n");
                            // fill value
                            laxis.isMatched = false;
                            if (laxis.Ks[laxis.Column] != null)
                            {
                                // [field...] should be evaluated to either an empty node-set or a node-set with exactly one member
                                // two matches... already existing field value in the table.
                                SendValidationEvent(SR.Sch_FieldSingleValueExpected, reader.LocalName);
                            }
                            else
                            {
                                // for element, reader.Value = "";
                                string stringValue = !hasSibling ? textString! : textValue!.ToString();  // only for identity-constraint exception reporting
                                if (reader.TypedValueObject != null && stringValue.Length != 0)
                                {
                                    laxis.Ks[laxis.Column] = new TypedObject(reader.TypedValueObject, stringValue, context!.ElementDecl!.Datatype);
                                }
                            }
                        }
                        // EndChildren
                        laxis.EndElement(reader.LocalName, reader.NamespaceURI);
                    }

                    if (constraints[i].axisSelector.EndElement(reader.LocalName, reader.NamespaceURI))
                    {
                        // insert key sequence into hash (+ located active axis tuple leave for later)
                        KeySequence ks = constraints[i].axisSelector.PopKS();
                        // unqualified keysequence are not allowed
                        switch (constraints[i].constraint.Role)
                        {
                            case CompiledIdentityConstraint.ConstraintRole.Key:
                                if (!ks.IsQualified())
                                {
                                    //Key's fields can't be null...  if we can return context node's line info maybe it will be better
                                    //only keymissing & keyduplicate reporting cases are necessary to be dealt with... 3 places...
                                    SendValidationEvent(new XmlSchemaException(SR.Sch_MissingKey, constraints[i].constraint.name.ToString(), reader.BaseURI, ks.PosLine, ks.PosCol));
                                }
                                else if (constraints[i].qualifiedTable!.Contains(ks))
                                {
                                    // unique or key checking value confliction
                                    // for redundant key, reporting both occurrings
                                    // doesn't work... how can i retrieve value out??
                                    SendValidationEvent(new XmlSchemaException(SR.Sch_DuplicateKey,
                                        new string[2] { ks.ToString(), constraints[i].constraint.name.ToString() },
                                        reader.BaseURI, ks.PosLine, ks.PosCol));
                                }
                                else
                                {
                                    constraints[i].qualifiedTable!.Add(ks, ks);
                                }
                                break;
                            case CompiledIdentityConstraint.ConstraintRole.Unique:
                                if (!ks.IsQualified())
                                {
                                    continue;
                                }
                                if (constraints[i].qualifiedTable!.Contains(ks))
                                {
                                    // unique or key checking confliction
                                    SendValidationEvent(new XmlSchemaException(SR.Sch_DuplicateKey,
                                        new string[2] { ks.ToString(), constraints[i].constraint.name.ToString() },
                                        reader.BaseURI, ks.PosLine, ks.PosCol));
                                }
                                else
                                {
                                    constraints[i].qualifiedTable!.Add(ks, ks);
                                }
                                break;
                            case CompiledIdentityConstraint.ConstraintRole.Keyref:
                                // is there any possibility:
                                // 2 keyrefs: value is equal, type is not
                                // both put in the hashtable, 1 reference, 1 not
                                if (constraints[i].qualifiedTable != null)
                                { //Will be null in cases when the keyref is outside the scope of the key, that is not allowed by our impl
                                    if (!ks.IsQualified() || constraints[i].qualifiedTable!.Contains(ks))
                                    {
                                        continue;
                                    }
                                    constraints[i].qualifiedTable!.Add(ks, ks);
                                }
                                break;
                        }
                    }
                }
            }

            // current level's constraint struct
            ConstraintStruct[] vcs = ((ValidationState)(_validationStack[_validationStack.Length - 1])).Constr!;
            if (vcs != null)
            {
                // validating all referencing tables...
                for (int i = 0; i < vcs.Length; ++i)
                {
                    if ((vcs[i].constraint.Role == CompiledIdentityConstraint.ConstraintRole.Keyref)
                        || (vcs[i].keyrefTable == null))
                    {
                        continue;
                    }
                    foreach (KeySequence? ks in vcs[i].keyrefTable!.Keys)
                    {
                        if (!vcs[i].qualifiedTable!.Contains(ks!))
                        {
                            SendValidationEvent(new XmlSchemaException(SR.Sch_UnresolvedKeyref, new string[2] { ks!.ToString(), vcs[i].constraint.name.ToString() },
                                reader.BaseURI, ks.PosLine, ks.PosCol));
                        }
                    }
                }
            }
        }
    }
#pragma warning restore 618
}
