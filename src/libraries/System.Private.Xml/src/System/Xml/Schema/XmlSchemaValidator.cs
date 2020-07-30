// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Threading;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Schema
{
    public delegate object? XmlValueGetter();

    [Flags]
    public enum XmlSchemaValidationFlags
    {
        None = 0x0000,
        ProcessInlineSchema = 0x0001,
        ProcessSchemaLocation = 0x0002,
        ReportValidationWarnings = 0x0004,
        ProcessIdentityConstraints = 0x0008,
        AllowXmlAttributes = 0x0010,
    }

    internal enum ValidatorState
    {
        None,
        Start,
        TopLevelAttribute,
        TopLevelTextOrWS,
        Element,
        Attribute,
        EndOfAttributes,
        Text,
        Whitespace,
        EndElement,
        SkipToEndElement,
        Finish,
    }
    internal class IdRefNode
    {
        internal string Id;
        internal int LineNo;
        internal int LinePos;
        internal IdRefNode? Next;

        internal IdRefNode(IdRefNode? next, string id, int lineNo, int linePos)
        {
            this.Id = id;
            this.LineNo = lineNo;
            this.LinePos = linePos;
            this.Next = next;
        }
    }

    public sealed class XmlSchemaValidator
    {
        //Schema Set
        private readonly XmlSchemaSet _schemaSet;

        //Validation Settings
        private readonly XmlSchemaValidationFlags _validationFlags;

        //Validation
        private int _startIDConstraint = -1;
        private const int STACK_INCREMENT = 10;
        private bool _isRoot;
        private bool _rootHasSchema;

        //PSVI
        private bool _attrValid;
        private bool _checkEntity;

        private SchemaInfo _compiledSchemaInfo;
        private IDtdInfo? _dtdSchemaInfo;
        private readonly Hashtable? _validatedNamespaces;

        private HWStack _validationStack;  // validaton contexts
        private ValidationState _context;          // current context
        private ValidatorState _currentState;

        //Attributes & IDS
        private Hashtable _attPresence;         //(AttName Vs AttIndex)
        private SchemaAttDef? _wildID;

        private Hashtable? _IDs;
        private IdRefNode? _idRefListHead;

        //Parsing
        private XmlQualifiedName _contextQName;

        //Avoid SchemaNames creation
        private string _nsXs;
        private string _nsXsi;
        private string _nsXmlNs;
        private string _nsXml;

        //PartialValidation
        private XmlSchemaObject? _partialValidationType;

        //text to typedValue
        private StringBuilder _textValue;

        //Other state
        private ValidationEventHandler? _eventHandler;
        private object _validationEventSender;
        private readonly XmlNameTable _nameTable;
        private IXmlLineInfo _positionInfo;
        private IXmlLineInfo _dummyPositionInfo;

        private XmlResolver? _xmlResolver;
        private Uri? _sourceUri;
        private string? _sourceUriString;
        private readonly IXmlNamespaceResolver _nsResolver;

        private XmlSchemaContentProcessing _processContents = XmlSchemaContentProcessing.Strict;

        private static XmlSchemaAttribute? s_xsiTypeSO;
        private static XmlSchemaAttribute? s_xsiNilSO;
        private static XmlSchemaAttribute? s_xsiSLSO;
        private static XmlSchemaAttribute? s_xsiNoNsSLSO;

        //Xsi Attributes that are atomized
        private string _xsiTypeString;
        private string _xsiNilString;
        private string _xsiSchemaLocationString;
        private string _xsiNoNamespaceSchemaLocationString;

        //Xsi Attributes parsing
        private static readonly XmlSchemaDatatype s_dtQName = XmlSchemaDatatype.FromXmlTokenizedTypeXsd(XmlTokenizedType.QName)!;
        private static readonly XmlSchemaDatatype s_dtCDATA = XmlSchemaDatatype.FromXmlTokenizedType(XmlTokenizedType.CDATA)!;
        private static readonly XmlSchemaDatatype s_dtStringArray = s_dtCDATA.DeriveByList(null);

        //Error message constants
        private const char Quote = '\'';

        //Empty arrays
        private static readonly XmlSchemaParticle[] s_emptyParticleArray = Array.Empty<XmlSchemaParticle>();
        private static readonly XmlSchemaAttribute[] s_emptyAttributeArray = Array.Empty<XmlSchemaAttribute>();

        //Whitespace check for text nodes
        private XmlCharType _xmlCharType = XmlCharType.Instance;

        internal static bool[,] ValidStates = new bool[12, 12] {
                                               /*ValidatorState.None*/      /*ValidatorState.Start  /*ValidatorState.TopLevelAttribute*/     /*ValidatorState.TopLevelTOrWS*/ /*ValidatorState.Element*/      /*ValidatorState.Attribute*/    /*ValidatorState.EndAttributes*/    /*ValidatorState.Text/      /*ValidatorState.WS/*       /*ValidatorState.EndElement*/   /*ValidatorState.SkipToEndElement*/         /*ValidatorState.Finish*/
        /*ValidatorState.None*/             {  true,                        true,                     false,                                 false,                           false,                          false,                          false,                              false,                      false,                      false,                          false,                                      false},
        /*ValidatorState.Start*/            {  false,                       true,                     true,                                  true,                            true,                           false,                          false,                              false,                      false,                      false,                          false,                                      true },
        /*ValidatorState.TopLevelAttribute*/{  false,                       false,                    false,                                 false,                           false,                          false,                          false,                              false,                      false,                      false,                          false,                                      true },
        /*ValidatorState.TopLevelTextOrWS*/ {  false,                       false,                    false,                                 true,                            true,                           false,                          false,                              false,                      false,                      false,                          false,                                      true },
        /*ValidatorState.Element*/          {  false,                       false,                    false,                                 true,                            false,                          true,                           true,                               false,                      false,                      true,                           true,                                       false},
        /*ValidatorState.Attribute*/        {  false,                       false,                    false,                                 false,                           false,                          true,                           true,                               false,                      false,                      true,                           true,                                       false},
        /*ValidatorState.EndAttributes*/    {  false,                       false,                    false,                                 false,                           true,                           false,                          false,                              true,                       true,                       true,                           true,                                       false},
        /*ValidatorState.Text*/             {  false,                       false,                    false,                                 false,                           true,                           false,                          false,                              true,                       true,                       true,                           true,                                       false},
        /*ValidatorState.Whitespace*/       {  false,                       false,                    false,                                 false,                           true,                           false,                          false,                              true,                       true,                       true,                           true,                                       false},
        /*ValidatorState.EndElement*/       {  false,                       false,                    false,                                 true,                            true,                           false,                          false,                              true,                       true,                       true,                           true /*?*/,                                 true },
        /*ValidatorState.SkipToEndElement*/ {  false,                       false,                    false,                                 true,                            true,                           false,                          false,                              true,                       true,                       true,                           true,                                       true },
        /*ValidatorState.Finish*/           {  false,                       true,                     false,                                 false,                           false,                          false,                          false,                              false,                      false,                      false,                          false,                                      false},
        };

        private static readonly string[] s_methodNames = new string[12] { "None", "Initialize", "top-level ValidateAttribute", "top-level ValidateText or ValidateWhitespace", "ValidateElement", "ValidateAttribute", "ValidateEndOfAttributes", "ValidateText", "ValidateWhitespace", "ValidateEndElement", "SkipToEndElement", "EndValidation" };

        public XmlSchemaValidator(XmlNameTable nameTable, XmlSchemaSet schemas, IXmlNamespaceResolver namespaceResolver, XmlSchemaValidationFlags validationFlags)
        {
            if (nameTable == null)
            {
                throw new ArgumentNullException(nameof(nameTable));
            }
            if (schemas == null)
            {
                throw new ArgumentNullException(nameof(schemas));
            }
            if (namespaceResolver == null)
            {
                throw new ArgumentNullException(nameof(namespaceResolver));
            }

            _nameTable = nameTable;
            _nsResolver = namespaceResolver;
            _validationFlags = validationFlags;


            if (((validationFlags & XmlSchemaValidationFlags.ProcessInlineSchema) != 0) || ((validationFlags & XmlSchemaValidationFlags.ProcessSchemaLocation) != 0))
            { //Process schema hints in xml document, hence user's set might change
                _schemaSet = new XmlSchemaSet(nameTable);
                _schemaSet.ValidationEventHandler += schemas.GetEventHandler();
                _schemaSet.CompilationSettings = schemas.CompilationSettings;
                _schemaSet.XmlResolver = schemas.GetResolver();
                _schemaSet.Add(schemas);
                _validatedNamespaces = new Hashtable();
            }
            else
            { //Use the same set from the user
                _schemaSet = schemas;
            }
            Init();
        }

        [MemberNotNull(nameof(_validationStack))]
        [MemberNotNull(nameof(_attPresence))]
        [MemberNotNull(nameof(_positionInfo))]
        [MemberNotNull(nameof(_dummyPositionInfo))]
        [MemberNotNull(nameof(_validationEventSender))]
        [MemberNotNull(nameof(_currentState))]
        [MemberNotNull(nameof(_textValue))]
        [MemberNotNull(nameof(_context))]
        [MemberNotNull(nameof(_contextQName))]
        [MemberNotNull(nameof(_nsXs))]
        [MemberNotNull(nameof(_nsXsi))]
        [MemberNotNull(nameof(_nsXmlNs))]
        [MemberNotNull(nameof(_nsXml))]
        [MemberNotNull(nameof(_xsiTypeString))]
        [MemberNotNull(nameof(_xsiNilString))]
        [MemberNotNull(nameof(_xsiSchemaLocationString))]
        [MemberNotNull(nameof(_xsiNoNamespaceSchemaLocationString))]
        [MemberNotNull(nameof(_compiledSchemaInfo))]
        private void Init()
        {
            _validationStack = new HWStack(STACK_INCREMENT);
            _attPresence = new Hashtable();
            Push(XmlQualifiedName.Empty);

            _dummyPositionInfo = new PositionInfo(); //Dummy position info, will return (0,0) if user does not set the LineInfoProvider property
            _positionInfo = _dummyPositionInfo;
            _validationEventSender = this;
            _currentState = ValidatorState.None;
            _textValue = new StringBuilder(100);
            _xmlResolver = null;
            _contextQName = new XmlQualifiedName(); //Re-use qname
            Reset();

            RecompileSchemaSet(); //Gets compiled info from set as well
            //Get already Atomized strings
            _nsXs = _nameTable.Add(XmlReservedNs.NsXs);
            _nsXsi = _nameTable.Add(XmlReservedNs.NsXsi);
            _nsXmlNs = _nameTable.Add(XmlReservedNs.NsXmlNs);
            _nsXml = _nameTable.Add(XmlReservedNs.NsXml);
            _xsiTypeString = _nameTable.Add("type");
            _xsiNilString = _nameTable.Add("nil");
            _xsiSchemaLocationString = _nameTable.Add("schemaLocation");
            _xsiNoNamespaceSchemaLocationString = _nameTable.Add("noNamespaceSchemaLocation");
        }

        private void Reset()
        {
            _isRoot = true;
            _rootHasSchema = true;
            while (_validationStack.Length > 1)
            { //Clear all other context from stack
                _validationStack.Pop();
            }
            _startIDConstraint = -1;
            _partialValidationType = null;

            //Clear previous tables
            if (_IDs != null)
            {
                _IDs.Clear();
            }
            if (ProcessSchemaHints)
            {
                _validatedNamespaces!.Clear();
            }
        }

        //Properties
        public XmlResolver? XmlResolver
        {
            set
            {
                _xmlResolver = value;
            }
        }

        [AllowNull]
        public IXmlLineInfo LineInfoProvider
        {
            get
            {
                return _positionInfo;
            }
            set
            {
                if (value == null)
                { //If value is null, retain the default dummy line info
                    _positionInfo = _dummyPositionInfo;
                }
                else
                {
                    _positionInfo = value;
                }
            }
        }

        [DisallowNull]
        public Uri? SourceUri
        {
            get
            {
                return _sourceUri;
            }
            set
            {
                _sourceUri = value;
                _sourceUriString = _sourceUri.ToString();
            }
        }

        public object ValidationEventSender
        {
            get
            {
                return _validationEventSender;
            }
            set
            {
                _validationEventSender = value;
            }
        }

        public event ValidationEventHandler? ValidationEventHandler
        {
            add
            {
                _eventHandler += value;
            }
            remove
            {
                _eventHandler -= value;
            }
        }

        //Methods
        public void AddSchema(XmlSchema schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }
            if ((_validationFlags & XmlSchemaValidationFlags.ProcessInlineSchema) == 0)
            { //Do not process schema if processInlineSchema is not set
                return;
            }
            string? tns = schema.TargetNamespace;
            if (tns == null)
            {
                tns = string.Empty;
            }
            //Store the previous locations
            Hashtable schemaLocations = _schemaSet.SchemaLocations;
            DictionaryEntry[] oldLocations = new DictionaryEntry[schemaLocations.Count];
            schemaLocations.CopyTo(oldLocations, 0);

            Debug.Assert(_validatedNamespaces != null);
            if (_validatedNamespaces[tns] != null && _schemaSet.FindSchemaByNSAndUrl(schema.BaseUri, tns, oldLocations) == null)
            {
                SendValidationEvent(SR.Sch_ComponentAlreadySeenForNS, tns, XmlSeverityType.Error);
            }
            if (schema.ErrorCount == 0)
            {
                try
                {
                    _schemaSet.Add(schema);
                    RecompileSchemaSet();
                }
                catch (XmlSchemaException e)
                {
                    SendValidationEvent(SR.Sch_CannotLoadSchema, new string[] { schema.BaseUri!.ToString(), e.Message }, e);
                }
                for (int i = 0; i < schema.ImportedSchemas.Count; ++i)
                {     //Check for its imports
                    XmlSchema impSchema = (XmlSchema)schema.ImportedSchemas[i]!;
                    tns = impSchema.TargetNamespace;
                    if (tns == null)
                    {
                        tns = string.Empty;
                    }
                    if (_validatedNamespaces[tns] != null && _schemaSet.FindSchemaByNSAndUrl(impSchema.BaseUri, tns, oldLocations) == null)
                    {
                        SendValidationEvent(SR.Sch_ComponentAlreadySeenForNS, tns, XmlSeverityType.Error);
                        _schemaSet.RemoveRecursive(schema);
                        break;
                    }
                }
            }
        }

        public void Initialize()
        {
            if (_currentState != ValidatorState.None && _currentState != ValidatorState.Finish)
            {
                throw new InvalidOperationException(SR.Format(SR.Sch_InvalidStateTransition, new string[] { s_methodNames[(int)_currentState], s_methodNames[(int)ValidatorState.Start] }));
            }
            _currentState = ValidatorState.Start;
            Reset();
        }

        public void Initialize(XmlSchemaObject partialValidationType)
        {
            if (_currentState != ValidatorState.None && _currentState != ValidatorState.Finish)
            {
                throw new InvalidOperationException(SR.Format(SR.Sch_InvalidStateTransition, new string[] { s_methodNames[(int)_currentState], s_methodNames[(int)ValidatorState.Start] }));
            }
            if (partialValidationType == null)
            {
                throw new ArgumentNullException(nameof(partialValidationType));
            }
            if (!(partialValidationType is XmlSchemaElement || partialValidationType is XmlSchemaAttribute || partialValidationType is XmlSchemaType))
            {
                throw new ArgumentException(SR.Sch_InvalidPartialValidationType);
            }
            _currentState = ValidatorState.Start;
            Reset();
            _partialValidationType = partialValidationType;
        }

        // SxS: This method passes null as resource names and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        public void ValidateElement(string localName, string namespaceUri, XmlSchemaInfo? schemaInfo)
        {
            ValidateElement(localName, namespaceUri, schemaInfo, null, null, null, null);
        }

        public void ValidateElement(string localName, string namespaceUri, XmlSchemaInfo? schemaInfo, string? xsiType, string? xsiNil, string? xsiSchemaLocation, string? xsiNoNamespaceSchemaLocation)
        {
            if (localName == null)
            {
                throw new ArgumentNullException(nameof(localName));
            }
            if (namespaceUri == null)
            {
                throw new ArgumentNullException(nameof(namespaceUri));
            }

            CheckStateTransition(ValidatorState.Element, s_methodNames[(int)ValidatorState.Element]);

            ClearPSVI();
            _contextQName.Init(localName, namespaceUri);
            XmlQualifiedName elementName = _contextQName;

            bool invalidElementInContext;
            object? particle = ValidateElementContext(elementName, out invalidElementInContext); //Check element name is allowed in current position
            SchemaElementDecl? elementDecl = FastGetElementDecl(elementName, particle);

            //Change context to current element and update element decl
            Push(elementName);

            //Change current context's error state depending on whether this element was validated in its context correctly
            if (invalidElementInContext)
            {
                _context.Validity = XmlSchemaValidity.Invalid;
            }

            //Check if there are Xsi attributes
            if ((_validationFlags & XmlSchemaValidationFlags.ProcessSchemaLocation) != 0 && _xmlResolver != null)
            { //we should process schema location
                ProcessSchemaLocations(xsiSchemaLocation, xsiNoNamespaceSchemaLocation);
            }

            if (_processContents != XmlSchemaContentProcessing.Skip)
            {
                if (elementDecl == null && _partialValidationType == null)
                { //Since new schemaLocations might have been added, try for decl from the set again only if no PVType is set
                    elementDecl = _compiledSchemaInfo.GetElementDecl(elementName);
                }
                bool declFound = elementDecl != null;
                if (xsiType != null || xsiNil != null)
                {
                    elementDecl = CheckXsiTypeAndNil(elementDecl, xsiType, xsiNil, ref declFound);
                }
                if (elementDecl == null)
                {
                    ThrowDeclNotFoundWarningOrError(declFound); //This updates processContents
                }
            }

            _context.ElementDecl = elementDecl;
            XmlSchemaElement? localSchemaElement = null;
            XmlSchemaType? localSchemaType = null;
            if (elementDecl != null)
            {
                CheckElementProperties();
                _attPresence.Clear(); //Clear attributes hashtable for every element
                _context.NeedValidateChildren = _processContents != XmlSchemaContentProcessing.Skip;
                ValidateStartElementIdentityConstraints();  //Need attr collection validated here
                elementDecl.ContentValidator!.InitValidation(_context);

                localSchemaType = elementDecl.SchemaType;
                localSchemaElement = GetSchemaElement();
            }

            if (schemaInfo != null)
            {
                schemaInfo.SchemaType = localSchemaType;
                schemaInfo.SchemaElement = localSchemaElement;
                schemaInfo.IsNil = _context.IsNill;
                schemaInfo.Validity = _context.Validity;
            }
            if (ProcessSchemaHints)
            {
                if (_validatedNamespaces![namespaceUri] == null)
                {
                    _validatedNamespaces!.Add(namespaceUri, namespaceUri);
                }
            }

            if (_isRoot)
            {
                _isRoot = false;
            }
        }

        public object? ValidateAttribute(string localName, string namespaceUri, string attributeValue, XmlSchemaInfo? schemaInfo)
        {
            if (attributeValue == null)
            {
                throw new ArgumentNullException(nameof(attributeValue));
            }

            return ValidateAttribute(localName, namespaceUri, null, attributeValue, schemaInfo);
        }

        public object? ValidateAttribute(string localName, string namespaceUri, XmlValueGetter attributeValue, XmlSchemaInfo? schemaInfo)
        {
            if (attributeValue == null)
            {
                throw new ArgumentNullException(nameof(attributeValue));
            }

            return ValidateAttribute(localName, namespaceUri, attributeValue, null, schemaInfo);
        }

        private object? ValidateAttribute(string localName, string namespaceUri, XmlValueGetter? attributeValueGetter, string? attributeStringValue, XmlSchemaInfo? schemaInfo)
        {
            if (localName == null)
            {
                throw new ArgumentNullException(nameof(localName));
            }
            if (namespaceUri == null)
            {
                throw new ArgumentNullException(nameof(namespaceUri));
            }

            ValidatorState toState = _validationStack.Length > 1 ? ValidatorState.Attribute : ValidatorState.TopLevelAttribute;
            CheckStateTransition(toState, s_methodNames[(int)toState]);

            object? typedVal = null;
            _attrValid = true;
            XmlSchemaValidity localValidity = XmlSchemaValidity.NotKnown;
            XmlSchemaAttribute? localAttribute = null;
            XmlSchemaSimpleType? localMemberType = null;

            namespaceUri = _nameTable.Add(namespaceUri);
            if (Ref.Equal(namespaceUri, _nsXmlNs))
            {
                return null;
            }

            SchemaAttDef? attributeDef = null;
            SchemaElementDecl? currentElementDecl = _context.ElementDecl;
            XmlQualifiedName attQName = new XmlQualifiedName(localName, namespaceUri);
            if (_attPresence[attQName] != null)
            { //this attribute already checked as it is duplicate;
                SendValidationEvent(SR.Sch_DuplicateAttribute, attQName.ToString());
                if (schemaInfo != null)
                {
                    schemaInfo.Clear();
                }
                return null;
            }

            if (!Ref.Equal(namespaceUri, _nsXsi))
            {
                XmlSchemaObject? pvtAttribute = _currentState == ValidatorState.TopLevelAttribute ? _partialValidationType : null;
                AttributeMatchState attributeMatchState;
                attributeDef = _compiledSchemaInfo.GetAttributeXsd(currentElementDecl, attQName, pvtAttribute, out attributeMatchState);

                switch (attributeMatchState)
                {
                    case AttributeMatchState.UndeclaredElementAndAttribute:
                        if ((attributeDef = CheckIsXmlAttribute(attQName)) != null)
                        { //Try for xml attribute
                            goto case AttributeMatchState.AttributeFound;
                        }
                        if (currentElementDecl == null
                            && _processContents == XmlSchemaContentProcessing.Strict
                            && attQName.Namespace.Length != 0
                            && _compiledSchemaInfo.Contains(attQName.Namespace)
                        )
                        {
                            _attrValid = false;
                            SendValidationEvent(SR.Sch_UndeclaredAttribute, attQName.ToString());
                        }
                        else if (_processContents != XmlSchemaContentProcessing.Skip)
                        {
                            SendValidationEvent(SR.Sch_NoAttributeSchemaFound, attQName.ToString(), XmlSeverityType.Warning);
                        }
                        break;

                    case AttributeMatchState.UndeclaredAttribute:
                        if ((attributeDef = CheckIsXmlAttribute(attQName)) != null)
                        {
                            goto case AttributeMatchState.AttributeFound;
                        }
                        else
                        {
                            _attrValid = false;
                            SendValidationEvent(SR.Sch_UndeclaredAttribute, attQName.ToString());
                        }
                        break;

                    case AttributeMatchState.ProhibitedAnyAttribute:
                        if ((attributeDef = CheckIsXmlAttribute(attQName)) != null)
                        {
                            goto case AttributeMatchState.AttributeFound;
                        }
                        else
                        {
                            _attrValid = false;
                            SendValidationEvent(SR.Sch_ProhibitedAttribute, attQName.ToString());
                        }
                        break;

                    case AttributeMatchState.ProhibitedAttribute:
                        _attrValid = false;
                        SendValidationEvent(SR.Sch_ProhibitedAttribute, attQName.ToString());
                        break;

                    case AttributeMatchState.AttributeNameMismatch:
                        _attrValid = false;
                        SendValidationEvent(SR.Sch_SchemaAttributeNameMismatch, new string[] { attQName.ToString(), ((XmlSchemaAttribute)pvtAttribute!).QualifiedName.ToString() });
                        break;

                    case AttributeMatchState.ValidateAttributeInvalidCall:
                        Debug.Assert(_currentState == ValidatorState.TopLevelAttribute); //Re-set state back to start on error with partial validation type
                        _currentState = ValidatorState.Start;
                        _attrValid = false;
                        SendValidationEvent(SR.Sch_ValidateAttributeInvalidCall, string.Empty);
                        break;

                    case AttributeMatchState.AnyIdAttributeFound:
                        if (_wildID == null)
                        {
                            _wildID = attributeDef;
                            Debug.Assert(currentElementDecl != null);
                            XmlSchemaComplexType ct = (currentElementDecl.SchemaType as XmlSchemaComplexType)!;
                            Debug.Assert(ct != null);
                            if (ct.ContainsIdAttribute(false))
                            {
                                SendValidationEvent(SR.Sch_AttrUseAndWildId, string.Empty);
                            }
                            else
                            {
                                goto case AttributeMatchState.AttributeFound;
                            }
                        }
                        else
                        { //More than one attribute per element cannot match wildcard if both their types are derived from ID
                            SendValidationEvent(SR.Sch_MoreThanOneWildId, string.Empty);
                        }
                        break;

                    case AttributeMatchState.AttributeFound:
                        Debug.Assert(attributeDef != null);
                        localAttribute = attributeDef.SchemaAttribute;
                        if (currentElementDecl != null)
                        { //Have to add to hashtable to check whether to add default attributes
                            _attPresence.Add(attQName, attributeDef);
                        }
                        object attValue;
                        if (attributeValueGetter != null)
                        {
                            attValue = attributeValueGetter()!;
                        }
                        else
                        {
                            attValue = attributeStringValue!;
                        }

                        typedVal = CheckAttributeValue(attValue, attributeDef);
                        XmlSchemaDatatype datatype = attributeDef.Datatype;
                        if (datatype.Variety == XmlSchemaDatatypeVariety.Union && typedVal != null)
                        { //Unpack the union
                            XsdSimpleValue simpleValue = (typedVal as XsdSimpleValue)!;
                            Debug.Assert(simpleValue != null);

                            localMemberType = simpleValue.XmlType;
                            datatype = simpleValue.XmlType.Datatype!;
                            typedVal = simpleValue.TypedValue;
                        }

                        CheckTokenizedTypes(datatype, typedVal, true);
                        if (HasIdentityConstraints)
                        {
                            AttributeIdentityConstraints(attQName.Name, attQName.Namespace, typedVal!, attValue!.ToString()!, datatype);
                        }
                        break;

                    case AttributeMatchState.AnyAttributeLax:
                        SendValidationEvent(SR.Sch_NoAttributeSchemaFound, attQName.ToString(), XmlSeverityType.Warning);
                        break;

                    case AttributeMatchState.AnyAttributeSkip:
                        break;

                    default:
                        break;
                }
            }
            else
            { //Attribute from xsi namespace
                localName = _nameTable.Add(localName);
                if (Ref.Equal(localName, _xsiTypeString) || Ref.Equal(localName, _xsiNilString) || Ref.Equal(localName, _xsiSchemaLocationString) || Ref.Equal(localName, _xsiNoNamespaceSchemaLocationString))
                {
                    _attPresence.Add(attQName, SchemaAttDef.Empty);
                }
                else
                {
                    _attrValid = false;
                    SendValidationEvent(SR.Sch_NotXsiAttribute, attQName.ToString());
                }
            }

            if (!_attrValid)
            {
                localValidity = XmlSchemaValidity.Invalid;
            }
            else if (attributeDef != null)
            {
                localValidity = XmlSchemaValidity.Valid;
            }
            if (schemaInfo != null)
            {
                schemaInfo.SchemaAttribute = localAttribute;
                schemaInfo.SchemaType = localAttribute == null ? null : localAttribute.AttributeSchemaType;
                schemaInfo.MemberType = localMemberType;
                schemaInfo.IsDefault = false;
                schemaInfo.Validity = localValidity;
            }
            if (ProcessSchemaHints)
            {
                if (_validatedNamespaces![namespaceUri] == null)
                {
                    _validatedNamespaces!.Add(namespaceUri, namespaceUri);
                }
            }
            return typedVal;
        }

        public void GetUnspecifiedDefaultAttributes(ArrayList defaultAttributes)
        {
            if (defaultAttributes == null)
            {
                throw new ArgumentNullException(nameof(defaultAttributes));
            }

            CheckStateTransition(ValidatorState.Attribute, "GetUnspecifiedDefaultAttributes");
            GetUnspecifiedDefaultAttributes(defaultAttributes, false);
        }

        public void ValidateEndOfAttributes(XmlSchemaInfo? schemaInfo)
        {
            CheckStateTransition(ValidatorState.EndOfAttributes, s_methodNames[(int)ValidatorState.EndOfAttributes]);
            //Check required attributes
            SchemaElementDecl? currentElementDecl = _context.ElementDecl;
            if (currentElementDecl != null && currentElementDecl.HasRequiredAttribute)
            {
                _context.CheckRequiredAttribute = false;
                CheckRequiredAttributes(currentElementDecl);
            }
            if (schemaInfo != null)
            { //set validity depending on whether all required attributes were validated successfully
                schemaInfo.Validity = _context.Validity;
            }
        }

        public void ValidateText(string elementValue)
        {
            if (elementValue == null)
            {
                throw new ArgumentNullException(nameof(elementValue));
            }
            ValidateText(elementValue, null);
        }

        public void ValidateText(XmlValueGetter elementValue)
        {
            if (elementValue == null)
            {
                throw new ArgumentNullException(nameof(elementValue));
            }
            ValidateText(null, elementValue);
        }

        private void ValidateText(string? elementStringValue, XmlValueGetter? elementValueGetter)
        {
            ValidatorState toState = _validationStack.Length > 1 ? ValidatorState.Text : ValidatorState.TopLevelTextOrWS;
            CheckStateTransition(toState, s_methodNames[(int)toState]);

            if (_context.NeedValidateChildren)
            {
                if (_context.IsNill)
                {
                    SendValidationEvent(SR.Sch_ContentInNill, QNameString(_context.LocalName!, _context.Namespace!));
                    return;
                }
                XmlSchemaContentType contentType = _context.ElementDecl!.ContentValidator!.ContentType;
                switch (contentType)
                {
                    case XmlSchemaContentType.Empty:
                        SendValidationEvent(SR.Sch_InvalidTextInEmpty, string.Empty);
                        break;

                    case XmlSchemaContentType.TextOnly:
                        if (elementValueGetter != null)
                        {
                            SaveTextValue(elementValueGetter()!);
                        }
                        else
                        {
                            SaveTextValue(elementStringValue!);
                        }
                        break;

                    case XmlSchemaContentType.ElementOnly:
                        string textValue = elementValueGetter != null ? elementValueGetter()!.ToString()! : elementStringValue!;
                        if (_xmlCharType.IsOnlyWhitespace(textValue))
                        {
                            break;
                        }
                        ArrayList? names = _context.ElementDecl.ContentValidator.ExpectedParticles(_context, false, _schemaSet);
                        if (names == null || names.Count == 0)
                        {
                            SendValidationEvent(SR.Sch_InvalidTextInElement, BuildElementName(_context.LocalName!, _context.Namespace!));
                        }
                        else
                        {
                            Debug.Assert(names.Count > 0);
                            SendValidationEvent(SR.Sch_InvalidTextInElementExpecting, new string[] { BuildElementName(_context.LocalName!, _context.Namespace!), PrintExpectedElements(names, true) });
                        }
                        break;

                    case XmlSchemaContentType.Mixed:
                        if (_context.ElementDecl.DefaultValueTyped != null)
                        {
                            if (elementValueGetter != null)
                            {
                                SaveTextValue(elementValueGetter()!);
                            }
                            else
                            {
                                SaveTextValue(elementStringValue!);
                            }
                        }
                        break;
                }
            }
        }

        public void ValidateWhitespace(string elementValue)
        {
            if (elementValue == null)
            {
                throw new ArgumentNullException(nameof(elementValue));
            }
            ValidateWhitespace(elementValue, null);
        }

        public void ValidateWhitespace(XmlValueGetter elementValue)
        {
            if (elementValue == null)
            {
                throw new ArgumentNullException(nameof(elementValue));
            }

            ValidateWhitespace(null, elementValue);
        }

        private void ValidateWhitespace(string? elementStringValue, XmlValueGetter? elementValueGetter)
        {
            ValidatorState toState = _validationStack.Length > 1 ? ValidatorState.Whitespace : ValidatorState.TopLevelTextOrWS;
            CheckStateTransition(toState, s_methodNames[(int)toState]);

            if (_context.NeedValidateChildren)
            {
                if (_context.IsNill)
                {
                    SendValidationEvent(SR.Sch_ContentInNill, QNameString(_context.LocalName!, _context.Namespace!));
                }

                XmlSchemaContentType contentType = _context.ElementDecl!.ContentValidator!.ContentType;
                switch (contentType)
                {
                    case XmlSchemaContentType.Empty:
                        SendValidationEvent(SR.Sch_InvalidWhitespaceInEmpty, string.Empty);
                        break;

                    case XmlSchemaContentType.TextOnly:
                        if (elementValueGetter != null)
                        {
                            SaveTextValue(elementValueGetter()!);
                        }
                        else
                        {
                            SaveTextValue(elementStringValue!);
                        }
                        break;

                    case XmlSchemaContentType.Mixed:
                        if (_context.ElementDecl.DefaultValueTyped != null)
                        {
                            if (elementValueGetter != null)
                            {
                                SaveTextValue(elementValueGetter()!);
                            }
                            else
                            {
                                SaveTextValue(elementStringValue!);
                            }
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public object? ValidateEndElement(XmlSchemaInfo? schemaInfo)
        {
            return InternalValidateEndElement(schemaInfo, null);
        }

        public object? ValidateEndElement(XmlSchemaInfo? schemaInfo, object typedValue)
        {
            if (typedValue == null)
            {
                throw new ArgumentNullException(nameof(typedValue));
            }
            if (_textValue.Length > 0)
            {
                throw new InvalidOperationException(SR.Sch_InvalidEndElementCall);
            }

            return InternalValidateEndElement(schemaInfo, typedValue);
        }

        public void SkipToEndElement(XmlSchemaInfo? schemaInfo)
        {
            if (_validationStack.Length <= 1)
            {
                throw new InvalidOperationException(SR.Format(SR.Sch_InvalidEndElementMultiple, s_methodNames[(int)ValidatorState.SkipToEndElement]));
            }

            CheckStateTransition(ValidatorState.SkipToEndElement, s_methodNames[(int)ValidatorState.SkipToEndElement]);

            if (schemaInfo != null)
            {
                SchemaElementDecl? currentElementDecl = _context.ElementDecl;
                if (currentElementDecl != null)
                {
                    schemaInfo.SchemaType = currentElementDecl.SchemaType;
                    schemaInfo.SchemaElement = GetSchemaElement();
                }
                else
                {
                    schemaInfo.SchemaType = null;
                    schemaInfo.SchemaElement = null;
                }
                schemaInfo.MemberType = null;
                schemaInfo.IsNil = _context.IsNill;
                schemaInfo.IsDefault = _context.IsDefault;
                Debug.Assert(_context.Validity != XmlSchemaValidity.Valid);
                schemaInfo.Validity = _context.Validity;
            }
            _context.ValidationSkipped = true;
            _currentState = ValidatorState.SkipToEndElement;
            Pop();
        }

        public void EndValidation()
        {
            if (_validationStack.Length > 1)
            { //We have pending elements in the stack to call ValidateEndElement
                throw new InvalidOperationException(SR.Sch_InvalidEndValidation);
            }
            CheckStateTransition(ValidatorState.Finish, s_methodNames[(int)ValidatorState.Finish]);
            CheckForwardRefs();
        }

        public XmlSchemaParticle[] GetExpectedParticles()
        {
            if (_currentState == ValidatorState.Start || _currentState == ValidatorState.TopLevelTextOrWS)
            { //Right after initialize
                if (_partialValidationType != null)
                {
                    XmlSchemaElement? element = _partialValidationType as XmlSchemaElement;
                    if (element != null)
                    {
                        return new XmlSchemaParticle[1] { element };
                    }

                    return s_emptyParticleArray;
                }
                else
                {
                    // Should return all global elements
                    ICollection elements = _schemaSet.GlobalElements.Values;
                    ArrayList expected = new ArrayList(elements.Count);
                    foreach (XmlSchemaElement? element in elements)
                    {
                        // Check for substitutions
                        ContentValidator.AddParticleToExpected(element!, _schemaSet, expected, true);
                    }

                    return (expected.ToArray(typeof(XmlSchemaParticle)) as XmlSchemaParticle[])!;
                }
            }
            if (_context.ElementDecl != null)
            {
                ArrayList? expected = _context.ElementDecl.ContentValidator!.ExpectedParticles(_context, false, _schemaSet);
                if (expected != null)
                {
                    return (expected.ToArray(typeof(XmlSchemaParticle)) as XmlSchemaParticle[])!;
                }
            }

            return s_emptyParticleArray;
        }

        public XmlSchemaAttribute[] GetExpectedAttributes()
        {
            if (_currentState == ValidatorState.Element || _currentState == ValidatorState.Attribute)
            {
                SchemaElementDecl? elementDecl = _context.ElementDecl;
                ArrayList attList = new ArrayList();
                if (elementDecl != null)
                {
                    foreach (SchemaAttDef attDef in elementDecl.AttDefs.Values)
                    {
                        if (_attPresence[attDef.Name] == null)
                        {
                            attList.Add(attDef.SchemaAttribute);
                        }
                    }
                }
                if (_nsResolver.LookupPrefix(_nsXsi) != null)
                { //Xsi namespace defined
                    AddXsiAttributes(attList);
                }

                return (attList.ToArray(typeof(XmlSchemaAttribute)) as XmlSchemaAttribute[])!;
            }
            else if (_currentState == ValidatorState.Start)
            {
                if (_partialValidationType != null)
                {
                    XmlSchemaAttribute? attribute = _partialValidationType as XmlSchemaAttribute;
                    if (attribute != null)
                    {
                        return new XmlSchemaAttribute[1] { attribute };
                    }
                }
            }
            return s_emptyAttributeArray;
        }

        internal void GetUnspecifiedDefaultAttributes(ArrayList defaultAttributes, bool createNodeData)
        {
            _currentState = ValidatorState.Attribute;
            SchemaElementDecl? currentElementDecl = _context.ElementDecl;

            if (currentElementDecl != null && currentElementDecl.HasDefaultAttribute)
            {
                for (int i = 0; i < currentElementDecl.DefaultAttDefs!.Count; ++i)
                {
                    SchemaAttDef attdef = (SchemaAttDef)currentElementDecl.DefaultAttDefs[i];
                    if (!_attPresence.Contains(attdef.Name))
                    {
                        if (attdef.DefaultValueTyped == null)
                        { //Invalid attribute default in the schema
                            continue;
                        }

                        //Check to see default attributes WILL be qualified if attributeFormDefault = qualified in schema
                        string attributeNS = _nameTable.Add(attdef.Name.Namespace);
                        string? defaultPrefix = string.Empty;
                        if (attributeNS.Length > 0)
                        {
                            defaultPrefix = GetDefaultAttributePrefix(attributeNS);
                            if (defaultPrefix == null || defaultPrefix.Length == 0)
                            {
                                SendValidationEvent(SR.Sch_DefaultAttributeNotApplied, new string[2] { attdef.Name.ToString(), QNameString(_context.LocalName!, _context.Namespace!) });
                                continue;
                            }
                        }

                        XmlSchemaDatatype datatype = attdef.Datatype;
                        if (createNodeData)
                        {
                            ValidatingReaderNodeData attrData = new ValidatingReaderNodeData();
                            attrData.LocalName = _nameTable.Add(attdef.Name.Name);
                            attrData.Namespace = attributeNS;
                            attrData.Prefix = _nameTable.Add(defaultPrefix);
                            attrData.NodeType = XmlNodeType.Attribute;

                            //set PSVI properties
                            AttributePSVIInfo attrValidInfo = new AttributePSVIInfo();
                            XmlSchemaInfo attSchemaInfo = attrValidInfo.attributeSchemaInfo;
                            Debug.Assert(attSchemaInfo != null);
                            if (attdef.Datatype.Variety == XmlSchemaDatatypeVariety.Union)
                            {
                                XsdSimpleValue simpleValue = (attdef.DefaultValueTyped as XsdSimpleValue)!;
                                attSchemaInfo.MemberType = simpleValue.XmlType;
                                datatype = simpleValue.XmlType.Datatype!;
                                attrValidInfo.typedAttributeValue = simpleValue.TypedValue;
                            }
                            else
                            {
                                attrValidInfo.typedAttributeValue = attdef.DefaultValueTyped;
                            }

                            attSchemaInfo.IsDefault = true;
                            attSchemaInfo.Validity = XmlSchemaValidity.Valid;
                            attSchemaInfo.SchemaType = attdef.SchemaType;
                            attSchemaInfo.SchemaAttribute = attdef.SchemaAttribute;
                            attrData.RawValue = attSchemaInfo.XmlType!.ValueConverter.ToString(attrValidInfo.typedAttributeValue);

                            attrData.AttInfo = attrValidInfo;
                            defaultAttributes.Add(attrData);
                        }
                        else
                        {
                            defaultAttributes.Add(attdef.SchemaAttribute);
                        }

                        CheckTokenizedTypes(datatype, attdef.DefaultValueTyped, true);
                        if (HasIdentityConstraints)
                        {
                            AttributeIdentityConstraints(attdef.Name.Name, attdef.Name.Namespace, attdef.DefaultValueTyped, attdef.DefaultValueRaw, datatype);
                        }
                    }
                }
            }
        }

        internal XmlSchemaSet SchemaSet
        {
            get
            {
                return _schemaSet;
            }
        }

        internal XmlSchemaValidationFlags ValidationFlags
        {
            get
            {
                return _validationFlags;
            }
        }

        internal XmlSchemaContentType CurrentContentType
        {
            get
            {
                if (_context.ElementDecl == null)
                {
                    return XmlSchemaContentType.Empty;
                }
                return _context.ElementDecl.ContentValidator!.ContentType;
            }
        }

        internal XmlSchemaContentProcessing CurrentProcessContents
        {
            get
            {
                return _processContents;
            }
        }

        internal void SetDtdSchemaInfo(IDtdInfo? dtdSchemaInfo)
        {
            _dtdSchemaInfo = dtdSchemaInfo;
            _checkEntity = true;
        }

        private bool StrictlyAssessed
        {
            get
            {
                return (_processContents == XmlSchemaContentProcessing.Strict || _processContents == XmlSchemaContentProcessing.Lax) && _context.ElementDecl != null && !_context.ValidationSkipped;
            }
        }

        private bool HasSchema
        {
            get
            {
                if (_isRoot)
                {
                    _isRoot = false;
                    if (!_compiledSchemaInfo.Contains(_context.Namespace!))
                    {
                        _rootHasSchema = false;
                    }
                }
                return _rootHasSchema;
            }
        }

        internal string GetConcatenatedValue()
        {
            return _textValue.ToString();
        }

        private object? InternalValidateEndElement(XmlSchemaInfo? schemaInfo, object? typedValue)
        {
            if (_validationStack.Length <= 1)
            {
                throw new InvalidOperationException(SR.Format(SR.Sch_InvalidEndElementMultiple, s_methodNames[(int)ValidatorState.EndElement]));
            }

            CheckStateTransition(ValidatorState.EndElement, s_methodNames[(int)ValidatorState.EndElement]);

            SchemaElementDecl? contextElementDecl = _context.ElementDecl;
            XmlSchemaSimpleType? memberType = null;
            XmlSchemaType? localSchemaType = null;
            XmlSchemaElement? localSchemaElement = null;

            string stringValue = string.Empty;

            if (contextElementDecl != null)
            {
                if (_context.CheckRequiredAttribute && contextElementDecl.HasRequiredAttribute)
                {
                    CheckRequiredAttributes(contextElementDecl);
                }
                if (!_context.IsNill)
                {
                    if (_context.NeedValidateChildren)
                    {
                        XmlSchemaContentType contentType = contextElementDecl.ContentValidator!.ContentType;
                        switch (contentType)
                        {
                            case XmlSchemaContentType.TextOnly:
                                if (typedValue == null)
                                {
                                    stringValue = _textValue.ToString();
                                    typedValue = ValidateAtomicValue(stringValue, out memberType);
                                }
                                else
                                { //Parsed object passed in, need to verify only facets
                                    typedValue = ValidateAtomicValue(typedValue, out memberType);
                                }
                                break;

                            case XmlSchemaContentType.Mixed:
                                if (contextElementDecl.DefaultValueTyped != null)
                                {
                                    if (typedValue == null)
                                    {
                                        stringValue = _textValue.ToString();
                                        typedValue = CheckMixedValueConstraint(stringValue);
                                    }
                                }
                                break;

                            case XmlSchemaContentType.ElementOnly:
                                if (typedValue != null)
                                { //Cannot pass in typedValue for complex content
                                    throw new InvalidOperationException(SR.Sch_InvalidEndElementCallTyped);
                                }
                                break;

                            default:
                                break;
                        }
                        if (!contextElementDecl.ContentValidator.CompleteValidation(_context))
                        {
                            CompleteValidationError(_context, _eventHandler, _nsResolver, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition, _schemaSet);
                            _context.Validity = XmlSchemaValidity.Invalid;
                        }
                    }
                }
                // for each level in the stack, endchildren and fill value from element
                if (HasIdentityConstraints)
                {
                    XmlSchemaType xmlType = memberType == null ? contextElementDecl.SchemaType! : memberType;
                    EndElementIdentityConstraints(typedValue!, stringValue, xmlType.Datatype!);
                }

                localSchemaType = contextElementDecl.SchemaType;
                localSchemaElement = GetSchemaElement();
            }
            if (schemaInfo != null)
            { //SET SchemaInfo
                schemaInfo.SchemaType = localSchemaType;
                schemaInfo.SchemaElement = localSchemaElement;
                schemaInfo.MemberType = memberType;
                schemaInfo.IsNil = _context.IsNill;
                schemaInfo.IsDefault = _context.IsDefault;
                if (_context.Validity == XmlSchemaValidity.NotKnown && StrictlyAssessed)
                {
                    _context.Validity = XmlSchemaValidity.Valid;
                }
                schemaInfo.Validity = _context.Validity;
            }
            Pop();
            return typedValue;
        }

        private void ProcessSchemaLocations(string? xsiSchemaLocation, string? xsiNoNamespaceSchemaLocation)
        {
            bool compile = false;
            if (xsiNoNamespaceSchemaLocation != null)
            {
                compile = true;
                LoadSchema(string.Empty, xsiNoNamespaceSchemaLocation);
            }

            if (xsiSchemaLocation != null)
            {
                object? typedValue;
                Exception? exception = s_dtStringArray.TryParseValue(xsiSchemaLocation, _nameTable, _nsResolver, out typedValue);
                if (exception != null)
                {
                    SendValidationEvent(SR.Sch_InvalidValueDetailedAttribute, new string[] { "schemaLocation", xsiSchemaLocation, s_dtStringArray.TypeCodeString, exception.Message }, exception);
                    return;
                }

                Debug.Assert(typedValue != null);
                string[] locations = (string[])typedValue;
                compile = true;
                try
                {
                    for (int j = 0; j < locations.Length - 1; j += 2)
                    {
                        LoadSchema((string)locations[j], (string)locations[j + 1]);
                    }
                }
                catch (XmlSchemaException schemaException)
                {
                    SendValidationEvent(schemaException);
                }
            }

            if (compile)
            {
                RecompileSchemaSet();
            }
        }


        private object? ValidateElementContext(XmlQualifiedName elementName, out bool invalidElementInContext)
        {
            object? particle = null;
            int errorCode = 0;
            XmlQualifiedName head;
            XmlSchemaElement? headElement = null;
            invalidElementInContext = false;

            if (_context.NeedValidateChildren)
            {
                if (_context.IsNill)
                {
                    SendValidationEvent(SR.Sch_ContentInNill, QNameString(_context.LocalName!, _context.Namespace!));
                    return null;
                }
                ContentValidator contentValidator = _context.ElementDecl!.ContentValidator!;
                if (contentValidator.ContentType == XmlSchemaContentType.Mixed && _context.ElementDecl.Presence == SchemaDeclBase.Use.Fixed)
                { //Mixed with default or fixed
                    SendValidationEvent(SR.Sch_ElementInMixedWithFixed, QNameString(_context.LocalName!, _context.Namespace!));
                    return null;
                }

                head = elementName;
                bool substitution = false;

                while (true)
                {
                    particle = _context.ElementDecl.ContentValidator!.ValidateElement(head, _context, out errorCode);
                    if (particle != null)
                    { //Match found
                        break;
                    }
                    if (errorCode == -2)
                    { //ContentModel all group error
                        SendValidationEvent(SR.Sch_AllElement, elementName.ToString());
                        invalidElementInContext = true;
                        _processContents = _context.ProcessContents = XmlSchemaContentProcessing.Skip;
                        return null;
                    }
                    //Match not found; check for substitutionGroup
                    substitution = true;
                    headElement = GetSubstitutionGroupHead(head);
                    if (headElement == null)
                    {
                        break;
                    }
                    else
                    {
                        head = headElement.QualifiedName;
                    }
                }

                if (substitution)
                {
                    XmlSchemaElement? matchedElem = particle as XmlSchemaElement;
                    if (matchedElem == null)
                    { //It matched an xs:any in that position
                        particle = null;
                    }
                    else if (matchedElem.RefName.IsEmpty)
                    { //It is not element ref but a local element
                        //If the head and matched particle are not hte same, then this is not substitutable, duped by a localElement with same QName
                        SendValidationEvent(SR.Sch_InvalidElementSubstitution, BuildElementName(elementName), BuildElementName(matchedElem.QualifiedName));
                        invalidElementInContext = true;
                        _processContents = _context.ProcessContents = XmlSchemaContentProcessing.Skip;
                    }
                    else
                    { //Correct substitution head found
                        particle = _compiledSchemaInfo.GetElement(elementName); //Re-assign correct particle
                        _context.NeedValidateChildren = true; //This will be reset to false once member match is not found
                    }
                }
                if (particle == null)
                {
                    ElementValidationError(elementName, _context, _eventHandler, _nsResolver, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition, _schemaSet);
                    invalidElementInContext = true;
                    _processContents = _context.ProcessContents = XmlSchemaContentProcessing.Skip;
                }
            }
            return particle;
        }


        private XmlSchemaElement? GetSubstitutionGroupHead(XmlQualifiedName member)
        {
            XmlSchemaElement? memberElem = _compiledSchemaInfo.GetElement(member);
            if (memberElem != null)
            {
                XmlQualifiedName head = memberElem.SubstitutionGroup;
                if (!head.IsEmpty)
                {
                    XmlSchemaElement? headElem = _compiledSchemaInfo.GetElement(head);
                    if (headElem != null)
                    {
                        if ((headElem.BlockResolved & XmlSchemaDerivationMethod.Substitution) != 0)
                        {
                            SendValidationEvent(SR.Sch_SubstitutionNotAllowed, new string[] { member.ToString(), head.ToString() });
                            return null;
                        }
                        if (!XmlSchemaType.IsDerivedFrom(memberElem.ElementSchemaType, headElem.ElementSchemaType, headElem.BlockResolved))
                        {
                            SendValidationEvent(SR.Sch_SubstitutionBlocked, new string[] { member.ToString(), head.ToString() });
                            return null;
                        }
                        return headElem;
                    }
                }
            }
            return null;
        }

        private object? ValidateAtomicValue(string stringValue, out XmlSchemaSimpleType? memberType)
        {
            object? typedVal = null;
            memberType = null;

            if (!_context.IsNill)
            {
                SchemaElementDecl currentElementDecl = _context.ElementDecl!;
                if (stringValue.Length == 0 && currentElementDecl.DefaultValueTyped != null)
                { //default value maybe present
                    SchemaElementDecl? declBeforeXsi = _context.ElementDeclBeforeXsi;
                    if (declBeforeXsi != null && declBeforeXsi != currentElementDecl)
                    { //There was xsi:type
                        Debug.Assert(currentElementDecl.Datatype != null);
                        Exception? exception = currentElementDecl.Datatype.TryParseValue(currentElementDecl.DefaultValueRaw, _nameTable, _nsResolver, out typedVal);
                        if (exception != null)
                        {
                            SendValidationEvent(SR.Sch_InvalidElementDefaultValue, new string[] { currentElementDecl.DefaultValueRaw, QNameString(_context.LocalName!, _context.Namespace!) });
                        }
                        else
                        {
                            _context.IsDefault = true;
                        }
                    }
                    else
                    {
                        _context.IsDefault = true;
                        typedVal = currentElementDecl.DefaultValueTyped;
                    }
                }
                else
                {
                    typedVal = CheckElementValue(stringValue);
                }

                XsdSimpleValue? simpleValue = typedVal as XsdSimpleValue;
                XmlSchemaDatatype dtype = currentElementDecl.Datatype;
                if (simpleValue != null)
                {
                    memberType = simpleValue.XmlType;
                    typedVal = simpleValue.TypedValue;
                    dtype = memberType.Datatype!;
                }

                CheckTokenizedTypes(dtype, typedVal, false);
            }
            return typedVal;
        }

        private object? ValidateAtomicValue(object parsedValue, out XmlSchemaSimpleType? memberType)
        {
            object? typedValue = null;
            memberType = null;
            if (!_context.IsNill)
            {
                SchemaElementDecl currentElementDecl = _context.ElementDecl!;
                SchemaDeclBase decl = (currentElementDecl as SchemaDeclBase)!;
                XmlSchemaDatatype dtype = currentElementDecl.Datatype!;
                Exception? exception = dtype.TryParseValue(parsedValue, _nameTable, _nsResolver, out typedValue);
                if (exception != null)
                {
                    string? stringValue = parsedValue as string;
                    if (stringValue == null)
                    {
                        stringValue = XmlSchemaDatatype.ConcatenatedToString(parsedValue);
                    }

                    SendValidationEvent(SR.Sch_ElementValueDataTypeDetailed, new string[] { QNameString(_context.LocalName!, _context.Namespace!), stringValue, GetTypeName(decl), exception.Message }, exception);
                    return null;
                }

                if (!decl.CheckValue(typedValue!))
                {
                    SendValidationEvent(SR.Sch_FixedElementValue, QNameString(_context.LocalName!, _context.Namespace!));
                }

                if (dtype.Variety == XmlSchemaDatatypeVariety.Union)
                {
                    XsdSimpleValue simpleValue = (typedValue as XsdSimpleValue)!;
                    Debug.Assert(simpleValue != null);
                    memberType = simpleValue.XmlType;
                    typedValue = simpleValue.TypedValue;
                    dtype = memberType.Datatype!;
                }

                CheckTokenizedTypes(dtype, typedValue, false);
            }

            return typedValue;
        }

        private string GetTypeName(SchemaDeclBase decl)
        {
            Debug.Assert(decl != null && decl.SchemaType != null);
            string typeName = decl.SchemaType.QualifiedName.ToString();
            if (typeName.Length == 0)
            {
                typeName = decl.Datatype.TypeCodeString;
            }

            return typeName;
        }

        private void SaveTextValue(object value)
        {
            string? s = value.ToString(); // For strings, which will mostly be the case, ToString() will return this. For other typedValues, need to go through value converter (eg: TimeSpan, DateTime etc)
            _textValue.Append(s);
        }

        [MemberNotNull(nameof(_context))]
        private void Push(XmlQualifiedName elementName)
        {
            _context = (ValidationState)_validationStack.Push();
            if (_context == null)
            {
                _context = new ValidationState();
                _validationStack.AddToTop(_context);
            }
            _context.LocalName = elementName.Name;
            _context.Namespace = elementName.Namespace;
            _context.HasMatched = false;
            _context.IsNill = false;
            _context.IsDefault = false;
            _context.CheckRequiredAttribute = true;
            _context.ValidationSkipped = false;
            _context.Validity = XmlSchemaValidity.NotKnown;
            _context.NeedValidateChildren = false;
            _context.ProcessContents = _processContents;
            _context.ElementDeclBeforeXsi = null;
            _context.Constr = null; //resetting the constraints to be null incase context != null
                                    // when pushing onto stack;
        }

        private void Pop()
        {
            Debug.Assert(_validationStack.Length > 1);
            ValidationState previousContext = (ValidationState)_validationStack.Pop()!;

            if (_startIDConstraint == _validationStack.Length)
            {
                _startIDConstraint = -1;
            }

            _context = (ValidationState)_validationStack.Peek()!;

            if (previousContext.Validity == XmlSchemaValidity.Invalid)
            { //Should set current context's validity to that of what was popped now in case of Invalid
                _context.Validity = XmlSchemaValidity.Invalid;
            }
            if (previousContext.ValidationSkipped)
            {
                _context.ValidationSkipped = true;
            }

            _processContents = _context.ProcessContents;
        }

        private void AddXsiAttributes(ArrayList attList)
        {
            BuildXsiAttributes();
            if (_attPresence[s_xsiTypeSO!.QualifiedName] == null)
            {
                attList.Add(s_xsiTypeSO);
            }
            if (_attPresence[s_xsiNilSO!.QualifiedName] == null)
            {
                attList.Add(s_xsiNilSO);
            }
            if (_attPresence[s_xsiSLSO!.QualifiedName] == null)
            {
                attList.Add(s_xsiSLSO);
            }
            if (_attPresence[s_xsiNoNsSLSO!.QualifiedName] == null)
            {
                attList.Add(s_xsiNoNsSLSO);
            }
        }

        private SchemaElementDecl? FastGetElementDecl(XmlQualifiedName elementName, object? particle)
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

            if (elementDecl == null && _processContents != XmlSchemaContentProcessing.Skip)
            {
                if (_isRoot && _partialValidationType != null)
                {
                    if (_partialValidationType is XmlSchemaElement)
                    {
                        XmlSchemaElement element = (XmlSchemaElement)_partialValidationType;
                        if (elementName.Equals(element.QualifiedName))
                        {
                            elementDecl = element.ElementDecl;
                        }
                        else
                        {
                            SendValidationEvent(SR.Sch_SchemaElementNameMismatch, elementName.ToString(), element.QualifiedName.ToString());
                        }
                    }
                    else if (_partialValidationType is XmlSchemaType)
                    { //Element name is wildcard
                        XmlSchemaType type = (XmlSchemaType)_partialValidationType;
                        elementDecl = type.ElementDecl;
                    }
                    else
                    { //its XmlSchemaAttribute
                        Debug.Assert(_partialValidationType is XmlSchemaAttribute);
                        SendValidationEvent(SR.Sch_ValidateElementInvalidCall, string.Empty);
                    }
                }
                else
                {
                    elementDecl = _compiledSchemaInfo.GetElementDecl(elementName);
                }
            }

            return elementDecl;
        }

        private SchemaElementDecl? CheckXsiTypeAndNil(SchemaElementDecl? elementDecl, string? xsiType, string? xsiNil, ref bool declFound)
        {
            XmlQualifiedName xsiTypeName = XmlQualifiedName.Empty;
            if (xsiType != null)
            {
                object? typedVal = null;
                Exception? exception = s_dtQName.TryParseValue(xsiType, _nameTable, _nsResolver, out typedVal);
                if (exception != null)
                {
                    SendValidationEvent(SR.Sch_InvalidValueDetailedAttribute, new string[] { "type", xsiType, s_dtQName.TypeCodeString, exception.Message }, exception);
                }
                else
                {
                    xsiTypeName = (typedVal as XmlQualifiedName)!;
                }
            }
            if (elementDecl != null)
            { //nillable is not dependent on xsi:type.
                if (elementDecl.IsNillable)
                {
                    if (xsiNil != null)
                    {
                        _context.IsNill = XmlConvert.ToBoolean(xsiNil);
                        if (_context.IsNill && elementDecl.Presence == SchemaDeclBase.Use.Fixed)
                        {
                            Debug.Assert(elementDecl.DefaultValueTyped != null);
                            SendValidationEvent(SR.Sch_XsiNilAndFixed);
                        }
                    }
                }
                else if (xsiNil != null)
                {
                    SendValidationEvent(SR.Sch_InvalidXsiNill);
                }
            }
            if (xsiTypeName.IsEmpty)
            {
                if (elementDecl != null && elementDecl.IsAbstract)
                {
                    SendValidationEvent(SR.Sch_AbstractElement, QNameString(_context.LocalName!, _context.Namespace!));
                    elementDecl = null;
                }
            }
            else
            {
                SchemaElementDecl? elementDeclXsi = _compiledSchemaInfo.GetTypeDecl(xsiTypeName);
                XmlSeverityType severity = XmlSeverityType.Warning;
                if (HasSchema && _processContents == XmlSchemaContentProcessing.Strict)
                {
                    severity = XmlSeverityType.Error;
                }
                if (elementDeclXsi == null && xsiTypeName.Namespace == _nsXs)
                {
                    XmlSchemaType? schemaType = DatatypeImplementation.GetSimpleTypeFromXsdType(xsiTypeName);
                    if (schemaType == null)
                    { //try getting complexType - xs:anyType
                        schemaType = XmlSchemaType.GetBuiltInComplexType(xsiTypeName);
                    }
                    if (schemaType != null)
                    {
                        elementDeclXsi = schemaType.ElementDecl;
                    }
                }
                if (elementDeclXsi == null)
                {
                    SendValidationEvent(SR.Sch_XsiTypeNotFound, xsiTypeName.ToString(), severity);
                    elementDecl = null;
                }
                else
                {
                    declFound = true;
                    if (elementDeclXsi.IsAbstract)
                    {
                        SendValidationEvent(SR.Sch_XsiTypeAbstract, xsiTypeName.ToString(), severity);
                        elementDecl = null;
                    }
                    else if (elementDecl != null && !XmlSchemaType.IsDerivedFrom(elementDeclXsi.SchemaType, elementDecl.SchemaType, elementDecl.Block))
                    {
                        SendValidationEvent(SR.Sch_XsiTypeBlockedEx, new string?[] { xsiTypeName.ToString(), QNameString(_context.LocalName!, _context.Namespace!) });
                        elementDecl = null;
                    }
                    else
                    {
                        if (elementDecl != null)
                        { //Get all element decl properties before assigning xsi:type decl; nillable already checked
                            elementDeclXsi = elementDeclXsi.Clone(); //Before updating properties onto xsi:type decl, clone it
                            elementDeclXsi.Constraints = elementDecl.Constraints;
                            elementDeclXsi.DefaultValueRaw = elementDecl.DefaultValueRaw;
                            elementDeclXsi.DefaultValueTyped = elementDecl.DefaultValueTyped;
                            elementDeclXsi.Block = elementDecl.Block;
                        }
                        _context.ElementDeclBeforeXsi = elementDecl;
                        elementDecl = elementDeclXsi;
                    }
                }
            }
            return elementDecl;
        }

        private void ThrowDeclNotFoundWarningOrError(bool declFound)
        {
            if (declFound)
            { //But invalid, so discontinue processing of children
                _processContents = _context.ProcessContents = XmlSchemaContentProcessing.Skip;
                _context.NeedValidateChildren = false;
            }
            else if (HasSchema && _processContents == XmlSchemaContentProcessing.Strict)
            { //Error and skip validation for children
                _processContents = _context.ProcessContents = XmlSchemaContentProcessing.Skip;
                _context.NeedValidateChildren = false;
                SendValidationEvent(SR.Sch_UndeclaredElement, QNameString(_context.LocalName!, _context.Namespace!));
            }
            else
            {
                SendValidationEvent(SR.Sch_NoElementSchemaFound, QNameString(_context.LocalName!, _context.Namespace!), XmlSeverityType.Warning);
            }
        }

        private void CheckElementProperties()
        {
            if (_context.ElementDecl!.IsAbstract)
            {
                SendValidationEvent(SR.Sch_AbstractElement, QNameString(_context.LocalName!, _context.Namespace!));
            }
        }

        private void ValidateStartElementIdentityConstraints()
        {
            // added on June 15, set the context here, so the stack can have them
            if (ProcessIdentityConstraints && _context.ElementDecl!.Constraints != null)
            {
                AddIdentityConstraints();
            }

            //foreach constraint in stack (including the current one)
            if (HasIdentityConstraints)
            {
                ElementIdentityConstraints();
            }
        }

        private SchemaAttDef? CheckIsXmlAttribute(XmlQualifiedName attQName)
        {
            SchemaAttDef? attdef = null;
            if (Ref.Equal(attQName.Namespace, _nsXml) && (_validationFlags & XmlSchemaValidationFlags.AllowXmlAttributes) != 0)
            {  //Need to check if this attribute is an xml attribute
                if (!_compiledSchemaInfo.Contains(_nsXml))
                { //We dont have a schema for xml namespace
                    // It can happen that the schemaSet already contains the schema for xml namespace
                    //   and we just have a stale compiled schema info (for example if the same schema set is used
                    //   by two validators at the same time and the one before us added the xml namespace schema
                    //   via this code here)
                    // In that case it is actually OK to try to add the schema for xml namespace again
                    //   since we're adding the exact same instance (the built in xml namespace schema is a singleton)
                    //   The addition on the schemaset is an effective no-op plus it's thread safe, so it's better to leave
                    //   that up to the schema set. The result of the below call will be simply that we update the
                    //   reference to the comipledSchemaInfo - which is exactly what we want in that case.
                    // In theory it can actually happen that there is some other schema registered for the xml namespace
                    //   (other than our built in one), and we don't know about it. In that case we don't support such scenario
                    //   as the user is modifying the schemaset as we're using it, which we don't support
                    //   for bunch of other reasons, so trying to add our built-in schema won't make it worse.
                    AddXmlNamespaceSchema();
                }
                _compiledSchemaInfo.AttributeDecls.TryGetValue(attQName, out attdef); //the xml attributes are all global attributes
            }
            return attdef;
        }

        private void AddXmlNamespaceSchema()
        {
            XmlSchemaSet localSet = new XmlSchemaSet(); //Avoiding cost of incremental compilation checks by compiling schema in a separate set and adding compiled set
            localSet.Add(Preprocessor.GetBuildInSchema());
            localSet.Compile();
            _schemaSet.Add(localSet);
            RecompileSchemaSet();
        }

        internal object? CheckMixedValueConstraint(string elementValue)
        {
            SchemaElementDecl elementDecl = _context.ElementDecl!;
            Debug.Assert(elementDecl.ContentValidator!.ContentType == XmlSchemaContentType.Mixed && elementDecl.DefaultValueTyped != null);
            if (_context.IsNill)
            { //Nil and fixed is error; Nil and default is compile time error
                return null;
            }
            if (elementValue.Length == 0)
            {
                _context.IsDefault = true;
                return elementDecl.DefaultValueTyped;
            }
            else
            {
                SchemaDeclBase decl = elementDecl as SchemaDeclBase;
                Debug.Assert(decl != null);
                if (decl.Presence == SchemaDeclBase.Use.Fixed && !elementValue.Equals(elementDecl.DefaultValueRaw))
                { //check string equality for mixed as it is untyped.
                    SendValidationEvent(SR.Sch_FixedElementValue, elementDecl.Name.ToString());
                }
                return elementValue;
            }
        }

        private void LoadSchema(string uri, string url)
        {
            Debug.Assert(_xmlResolver != null);
            XmlReader? Reader = null;
            try
            {
                Uri ruri = _xmlResolver.ResolveUri(_sourceUri, url);
                Stream stm = (Stream)_xmlResolver.GetEntity(ruri, null, null)!;
                XmlReaderSettings readerSettings = _schemaSet.ReaderSettings;
                readerSettings.CloseInput = true;
                readerSettings.XmlResolver = _xmlResolver;
                Reader = XmlReader.Create(stm, readerSettings, ruri.ToString());
                _schemaSet.Add(uri, Reader, _validatedNamespaces!);
                while (Reader.Read()) ; // wellformness check
            }
            catch (XmlSchemaException e)
            {
                SendValidationEvent(SR.Sch_CannotLoadSchema, new string[] { uri, e.Message }, e);
            }
            catch (Exception e)
            {
                SendValidationEvent(SR.Sch_CannotLoadSchema, new string[] { uri, e.Message }, e, XmlSeverityType.Warning);
            }
            finally
            {
                if (Reader != null)
                {
                    Reader.Close();
                }
            }
        }

        [MemberNotNull(nameof(_compiledSchemaInfo))]
        internal void RecompileSchemaSet()
        {
            if (!_schemaSet.IsCompiled)
            {
                try
                {
                    _schemaSet.Compile();
                }
                catch (XmlSchemaException e)
                {
                    SendValidationEvent(e);
                }
            }

            _compiledSchemaInfo = _schemaSet.CompiledInfo; //Fetch compiled info from set
        }

        private void ProcessTokenizedType(XmlTokenizedType ttype, string name, bool attrValue)
        {
            switch (ttype)
            {
                case XmlTokenizedType.ID:
                    if (ProcessIdentityConstraints)
                    {
                        if (FindId(name) != null)
                        {
                            if (attrValue)
                            {
                                _attrValid = false;
                            }
                            SendValidationEvent(SR.Sch_DupId, name);
                        }
                        else
                        {
                            if (_IDs == null)
                            {
                                _IDs = new Hashtable();
                            }

                            _IDs.Add(name, _context.LocalName);
                        }
                    }
                    break;
                case XmlTokenizedType.IDREF:
                    if (ProcessIdentityConstraints)
                    {
                        object? p = FindId(name);
                        if (p == null)
                        { // add it to linked list to check it later
                            _idRefListHead = new IdRefNode(_idRefListHead, name, _positionInfo.LineNumber, _positionInfo.LinePosition);
                        }
                    }
                    break;
                case XmlTokenizedType.ENTITY:
                    ProcessEntity(name);
                    break;
                default:
                    break;
            }
        }

        private object? CheckAttributeValue(object value, SchemaAttDef attdef)
        {
            object? typedValue = null;
            SchemaDeclBase decl = attdef as SchemaDeclBase;

            XmlSchemaDatatype dtype = attdef.Datatype;
            Debug.Assert(dtype != null);
            string? stringValue = value as string;
            Exception? exception = null;

            if (stringValue != null)
            {
                exception = dtype.TryParseValue(stringValue, _nameTable, _nsResolver, out typedValue);
                if (exception != null) goto Error;
            }
            else
            {
                // Calling object ParseValue for checking facets
                exception = dtype.TryParseValue(value, _nameTable, _nsResolver, out typedValue);
                if (exception != null) goto Error;
            }

            Debug.Assert(typedValue != null);

            if (!decl.CheckValue(typedValue))
            {
                _attrValid = false;
                SendValidationEvent(SR.Sch_FixedAttributeValue, attdef.Name.ToString());
            }

            return typedValue;

        Error:
            _attrValid = false;
            if (stringValue == null)
            {
                stringValue = XmlSchemaDatatype.ConcatenatedToString(value);
            }
            SendValidationEvent(SR.Sch_AttributeValueDataTypeDetailed, new string[] { attdef.Name.ToString(), stringValue, GetTypeName(decl), exception.Message }, exception);
            return null;
        }

        private object? CheckElementValue(string stringValue)
        {
            object? typedValue = null;
            SchemaDeclBase decl = (_context.ElementDecl as SchemaDeclBase)!;

            XmlSchemaDatatype dtype = decl.Datatype;
            Debug.Assert(dtype != null);

            Exception? exception = dtype.TryParseValue(stringValue, _nameTable, _nsResolver, out typedValue);
            if (exception != null)
            {
                SendValidationEvent(SR.Sch_ElementValueDataTypeDetailed, new string[] { QNameString(_context.LocalName!, _context.Namespace!), stringValue, GetTypeName(decl), exception.Message }, exception);
                return null;
            }

            Debug.Assert(typedValue != null);

            if (!decl.CheckValue(typedValue))
            {
                SendValidationEvent(SR.Sch_FixedElementValue, QNameString(_context.LocalName!, _context.Namespace!));
            }

            return typedValue;
        }

        private void CheckTokenizedTypes(XmlSchemaDatatype dtype, object? typedValue, bool attrValue)
        {
            // Check special types
            if (typedValue == null)
            {
                return;
            }

            XmlTokenizedType ttype = dtype.TokenizedType;
            if (ttype == XmlTokenizedType.ENTITY || ttype == XmlTokenizedType.ID || ttype == XmlTokenizedType.IDREF)
            {
                if (dtype.Variety == XmlSchemaDatatypeVariety.List)
                {
                    string[] ss = (string[])typedValue;
                    for (int i = 0; i < ss.Length; ++i)
                    {
                        ProcessTokenizedType(dtype.TokenizedType, ss[i], attrValue);
                    }
                }
                else
                {
                    ProcessTokenizedType(dtype.TokenizedType, (string)typedValue, attrValue);
                }
            }
        }

        private object? FindId(string name)
        {
            return _IDs == null ? null : _IDs[name];
        }

        private void CheckForwardRefs()
        {
            IdRefNode? next = _idRefListHead;
            while (next != null)
            {
                if (FindId(next.Id) == null)
                {
                    SendValidationEvent(new XmlSchemaValidationException(SR.Sch_UndeclaredId, next.Id, _sourceUriString, next.LineNo, next.LinePos), XmlSeverityType.Error);
                }

                IdRefNode? ptr = next.Next;
                next.Next = null; // unhook each object so it is cleaned up by Garbage Collector
                next = ptr;
            }
            // not needed any more.
            _idRefListHead = null;
        }


        private bool HasIdentityConstraints
        {
            get { return ProcessIdentityConstraints && _startIDConstraint != -1; }
        }

        internal bool ProcessIdentityConstraints
        {
            get
            {
                return (_validationFlags & XmlSchemaValidationFlags.ProcessIdentityConstraints) != 0;
            }
        }

        internal bool ReportValidationWarnings
        {
            get
            {
                return (_validationFlags & XmlSchemaValidationFlags.ReportValidationWarnings) != 0;
            }
        }

        internal bool ProcessSchemaHints
        {
            get
            {
                return (_validationFlags & XmlSchemaValidationFlags.ProcessInlineSchema) != 0 ||
                       (_validationFlags & XmlSchemaValidationFlags.ProcessSchemaLocation) != 0;
            }
        }

        private void CheckStateTransition(ValidatorState toState, string methodName)
        {
            if (!ValidStates[(int)_currentState, (int)toState])
            {
                if (_currentState == ValidatorState.None)
                {
                    throw new InvalidOperationException(SR.Format(SR.Sch_InvalidStartTransition, new string[] { methodName, s_methodNames[(int)ValidatorState.Start] }));
                }
                throw new InvalidOperationException(SR.Format(SR.Sch_InvalidStateTransition, new string[] { s_methodNames[(int)_currentState], methodName }));
            }
            _currentState = toState;
        }

        private void ClearPSVI()
        {
            if (_textValue != null)
            {
                _textValue.Length = 0;
            }

            _attPresence.Clear(); //Clear attributes hashtable for every element
            _wildID = null; //clear it for every element
        }

        private void CheckRequiredAttributes(SchemaElementDecl currentElementDecl)
        {
            Debug.Assert(currentElementDecl != null);
            Dictionary<XmlQualifiedName, SchemaAttDef> attributeDefs = currentElementDecl.AttDefs;
            foreach (SchemaAttDef attdef in attributeDefs.Values)
            {
                if (_attPresence[attdef.Name] == null)
                {
                    if (attdef.Presence == SchemaDeclBase.Use.Required || attdef.Presence == SchemaDeclBase.Use.RequiredFixed)
                    {
                        SendValidationEvent(SR.Sch_MissRequiredAttribute, attdef.Name.ToString());
                    }
                }
            }
        }

        private XmlSchemaElement? GetSchemaElement()
        {
            SchemaElementDecl? beforeXsiDecl = _context.ElementDeclBeforeXsi;
            SchemaElementDecl currentDecl = _context.ElementDecl!;

            if (beforeXsiDecl != null)
            {
                // Have a xsi:type
                if (beforeXsiDecl.SchemaElement != null)
                {
                    XmlSchemaElement xsiElement = (XmlSchemaElement)beforeXsiDecl.SchemaElement.Clone(null);
                    xsiElement.SchemaTypeName = XmlQualifiedName.Empty; //Reset typeName on element as this might be different
                    xsiElement.SchemaType = currentDecl.SchemaType;
                    xsiElement.SetElementType(currentDecl.SchemaType);
                    xsiElement.ElementDecl = currentDecl;
                    return xsiElement;
                }
            }

            return currentDecl.SchemaElement;
        }

        internal string? GetDefaultAttributePrefix(string attributeNS)
        {
            IDictionary<string, string> namespaceDecls = _nsResolver.GetNamespacesInScope(XmlNamespaceScope.All);
            string? defaultPrefix = null;
            string defaultNS;

            foreach (KeyValuePair<string, string> pair in namespaceDecls)
            {
                defaultNS = _nameTable.Add(pair.Value);
                if (Ref.Equal(defaultNS, attributeNS))
                {
                    defaultPrefix = pair.Key;
                    if (defaultPrefix.Length != 0)
                    {
                        // Locate first non-empty prefix
                        return defaultPrefix;
                    }
                }
            }

            return defaultPrefix;
        }

        private void AddIdentityConstraints()
        {
            SchemaElementDecl currentElementDecl = _context.ElementDecl!;
            _context.Constr = new ConstraintStruct[currentElementDecl.Constraints!.Length];
            int id = 0;
            for (int i = 0; i < currentElementDecl.Constraints.Length; ++i)
            {
                _context.Constr[id++] = new ConstraintStruct(currentElementDecl.Constraints[i]);
            } // foreach constraint /constraintstruct

            // added on June 19, make connections between new keyref tables with key/unique tables in stack
            // i can't put it in the above loop, coz there will be key on the same level
            for (int i = 0; i < _context.Constr.Length; ++i)
            {
                if (_context.Constr[i].constraint.Role == CompiledIdentityConstraint.ConstraintRole.Keyref)
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
                        ConstraintStruct[] constraintStructures = ((ValidationState)_validationStack[level]).Constr!;
                        for (int j = 0; j < constraintStructures.Length; ++j)
                        {
                            if (constraintStructures[j].constraint.name == _context.Constr[i].constraint.refer)
                            {
                                find = true;
                                if (constraintStructures[j].keyrefTable == null)
                                {
                                    constraintStructures[j].keyrefTable = new Hashtable();
                                }
                                _context.Constr[i].qualifiedTable = constraintStructures[j].keyrefTable;
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
                        SendValidationEvent(SR.Sch_RefNotInScope, QNameString(_context.LocalName!, _context.Namespace!));
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
            SchemaElementDecl? currentElementDecl = _context.ElementDecl;
            string localName = _context.LocalName!;
            string? namespaceUri = _context.Namespace;

            for (int i = _startIDConstraint; i < _validationStack.Length; i++)
            {
                // no constraint for this level
                if (((ValidationState)(_validationStack[i])).Constr == null)
                {
                    continue;
                }

                // else
                ConstraintStruct[] constraintStructures = ((ValidationState)_validationStack[i]).Constr!;
                for (int j = 0; j < constraintStructures.Length; ++j)
                {
                    // check selector from here
                    if (constraintStructures[j].axisSelector.MoveToStartElement(localName, namespaceUri))
                    {
                        // selector selects new node, activate a new set of fields
                        Debug.WriteLine("Selector Match!");
                        Debug.WriteLine("Name: " + localName + "\t|\tURI: " + namespaceUri + "\n");

                        // in which axisFields got updated
                        constraintStructures[j].axisSelector.PushKS(_positionInfo.LineNumber, _positionInfo.LinePosition);
                    }

                    // axisFields is not null, but may be empty
                    for (int k = 0; k < constraintStructures[j].axisFields.Count; ++k)
                    {
                        LocatedActiveAxis laxis = (LocatedActiveAxis)constraintStructures[j].axisFields[k]!;

                        // check field from here
                        if (laxis.MoveToStartElement(localName, namespaceUri))
                        {
                            Debug.WriteLine("Element Field Match!");
                            // checking simpleType / simpleContent
                            if (currentElementDecl != null)
                            {      // nextElement can be null when xml/xsd are not valid
                                if (currentElementDecl.Datatype == null || currentElementDecl.ContentValidator!.ContentType == XmlSchemaContentType.Mixed)
                                {
                                    SendValidationEvent(SR.Sch_FieldSimpleTypeExpected, localName);
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

        private void AttributeIdentityConstraints(string name, string? ns, object obj, string sobj, XmlSchemaDatatype datatype)
        {
            for (int ci = _startIDConstraint; ci < _validationStack.Length; ci++)
            {
                // no constraint for this level
                if (((ValidationState)(_validationStack[ci])).Constr == null)
                {
                    continue;
                }

                // else
                ConstraintStruct[] constraintStructures = ((ValidationState)_validationStack[ci]).Constr!;
                for (int i = 0; i < constraintStructures.Length; ++i)
                {
                    // axisFields is not null, but may be empty
                    for (int j = 0; j < constraintStructures[i].axisFields.Count; ++j)
                    {
                        LocatedActiveAxis laxis = (LocatedActiveAxis)constraintStructures[i].axisFields[j]!;

                        // check field from here
                        if (laxis.MoveToAttribute(name, ns))
                        {
                            Debug.WriteLine("Attribute Field Match!");
                            //attribute is only simpletype, so needn't checking...
                            // can fill value here, yeah!!
                            Debug.WriteLine("Attribute Field Filling Value!");
                            Debug.WriteLine("Name: " + name + "\t|\tURI: " + ns + "\t|\tValue: " + obj + "\n");
                            if (laxis.Ks[laxis.Column] != null)
                            {
                                // should be evaluated to either an empty node-set or a node-set with exactly one member
                                // two matches...
                                SendValidationEvent(SR.Sch_FieldSingleValueExpected, name);
                            }
                            else
                            {
                                Debug.Assert(datatype != null);
                                laxis.Ks[laxis.Column] = new TypedObject(obj, sobj, datatype);
                            }
                        }
                    }
                }
            }
        }

        private void EndElementIdentityConstraints(object typedValue, string stringValue, XmlSchemaDatatype datatype)
        {
            string localName = _context.LocalName!;
            string? namespaceUri = _context.Namespace;
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
                            Debug.WriteLine("Name: " + localName + "\t|\tURI: " + namespaceUri + "\t|\tValue: " + typedValue + "\n");
                            // fill value
                            laxis.isMatched = false;
                            if (laxis.Ks[laxis.Column] != null)
                            {
                                // [field...] should be evaluated to either an empty node-set or a node-set with exactly one member
                                // two matches... already existing field value in the table.
                                SendValidationEvent(SR.Sch_FieldSingleValueExpected, localName);
                            }
                            else
                            {
                                // for element, Reader.Value = "";
                                if (LocalAppContextSwitches.IgnoreEmptyKeySequences)
                                {
                                    if (typedValue != null && stringValue.Length != 0)
                                    {
                                        laxis.Ks[laxis.Column] = new TypedObject(typedValue, stringValue, datatype);
                                    }
                                }
                                else
                                {
                                    if (typedValue != null)
                                    {
                                        laxis.Ks[laxis.Column] = new TypedObject(typedValue, stringValue, datatype);
                                    }
                                }
                            }
                        }
                        // EndChildren
                        laxis.EndElement(localName, namespaceUri);
                    }

                    if (constraints[i].axisSelector.EndElement(localName, namespaceUri))
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
                                    SendValidationEvent(new XmlSchemaValidationException(SR.Sch_MissingKey, constraints[i].constraint.name.ToString(), _sourceUriString, ks.PosLine, ks.PosCol));
                                }
                                else if (constraints[i].qualifiedTable!.Contains(ks))
                                {
                                    // unique or key checking value confliction
                                    // for redundant key, reporting both occurings
                                    // doesn't work... how can i retrieve value out??
                                    //                                        KeySequence ks2 = (KeySequence) conuct.qualifiedTable[ks];
                                    SendValidationEvent(new XmlSchemaValidationException(SR.Sch_DuplicateKey,
                                        new string[2] { ks.ToString(), constraints[i].constraint.name.ToString() },
                                        _sourceUriString, ks.PosLine, ks.PosCol));
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
                                    //                                        KeySequence ks2 = (KeySequence) conuct.qualifiedTable[ks];
                                    SendValidationEvent(new XmlSchemaValidationException(SR.Sch_DuplicateKey,
                                        new string[2] { ks.ToString(), constraints[i].constraint.name.ToString() },
                                        _sourceUriString, ks.PosLine, ks.PosCol));
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
            ConstraintStruct[]? vcs = ((ValidationState)(_validationStack[_validationStack.Length - 1])).Constr;
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
                            SendValidationEvent(new XmlSchemaValidationException(SR.Sch_UnresolvedKeyref, new string[2] { ks!.ToString(), vcs[i].constraint.name.ToString() },
                                _sourceUriString, ks.PosLine, ks.PosCol));
                        }
                    }
                }
            }
        } //End of method

        private static void BuildXsiAttributes()
        {
            if (s_xsiTypeSO == null)
            { //xsi:type attribute
                XmlSchemaAttribute tempXsiTypeSO = new XmlSchemaAttribute();
                tempXsiTypeSO.Name = "type";
                tempXsiTypeSO.SetQualifiedName(new XmlQualifiedName("type", XmlReservedNs.NsXsi));
                tempXsiTypeSO.SetAttributeType(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.QName));
                Interlocked.CompareExchange<XmlSchemaAttribute?>(ref s_xsiTypeSO, tempXsiTypeSO, null);
            }
            if (s_xsiNilSO == null)
            { //xsi:nil
                XmlSchemaAttribute tempxsiNilSO = new XmlSchemaAttribute();
                tempxsiNilSO.Name = "nil";
                tempxsiNilSO.SetQualifiedName(new XmlQualifiedName("nil", XmlReservedNs.NsXsi));
                tempxsiNilSO.SetAttributeType(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Boolean));
                Interlocked.CompareExchange<XmlSchemaAttribute?>(ref s_xsiNilSO, tempxsiNilSO, null);
            }
            if (s_xsiSLSO == null)
            { //xsi:schemaLocation
                XmlSchemaSimpleType stringType = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String);
                XmlSchemaAttribute tempxsiSLSO = new XmlSchemaAttribute();
                tempxsiSLSO.Name = "schemaLocation";
                tempxsiSLSO.SetQualifiedName(new XmlQualifiedName("schemaLocation", XmlReservedNs.NsXsi));
                tempxsiSLSO.SetAttributeType(stringType);
                Interlocked.CompareExchange<XmlSchemaAttribute?>(ref s_xsiSLSO, tempxsiSLSO, null);
            }
            if (s_xsiNoNsSLSO == null)
            {
                XmlSchemaSimpleType stringType = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String);
                XmlSchemaAttribute tempxsiNoNsSLSO = new XmlSchemaAttribute();
                tempxsiNoNsSLSO.Name = "noNamespaceSchemaLocation";
                tempxsiNoNsSLSO.SetQualifiedName(new XmlQualifiedName("noNamespaceSchemaLocation", XmlReservedNs.NsXsi));
                tempxsiNoNsSLSO.SetAttributeType(stringType);
                Interlocked.CompareExchange<XmlSchemaAttribute?>(ref s_xsiNoNsSLSO, tempxsiNoNsSLSO, null);
            }
        }

        internal static void ElementValidationError(XmlQualifiedName name, ValidationState context, ValidationEventHandler? eventHandler, object sender, string? sourceUri, int lineNo, int linePos, XmlSchemaSet? schemaSet)
        {
            ArrayList? names = null;
            if (context.ElementDecl != null)
            {
                ContentValidator contentValidator = context.ElementDecl.ContentValidator!;
                XmlSchemaContentType contentType = contentValidator.ContentType;
                if (contentType == XmlSchemaContentType.ElementOnly || (contentType == XmlSchemaContentType.Mixed && contentValidator != ContentValidator.Mixed && contentValidator != ContentValidator.Any))
                {
                    Debug.Assert(contentValidator is DfaContentValidator || contentValidator is NfaContentValidator || contentValidator is RangeContentValidator || contentValidator is AllElementsContentValidator);
                    bool getParticles = schemaSet != null;
                    if (getParticles)
                    {
                        names = contentValidator.ExpectedParticles(context, false, schemaSet!);
                    }
                    else
                    {
                        names = contentValidator.ExpectedElements(context, false);
                    }

                    if (names == null || names.Count == 0)
                    {
                        if (context.TooComplex)
                        {
                            SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(SR.Sch_InvalidElementContentComplex, new string[] { BuildElementName(context.LocalName!, context.Namespace!), BuildElementName(name), SR.Sch_ComplexContentModel }, sourceUri, lineNo, linePos), XmlSeverityType.Error);
                        }
                        else
                        {
                            SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(SR.Sch_InvalidElementContent, new string[] { BuildElementName(context.LocalName!, context.Namespace!), BuildElementName(name) }, sourceUri, lineNo, linePos), XmlSeverityType.Error);
                        }
                    }
                    else
                    {
                        Debug.Assert(names.Count > 0);
                        if (context.TooComplex)
                        {
                            SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(SR.Sch_InvalidElementContentExpectingComplex, new string[] { BuildElementName(context.LocalName!, context.Namespace!), BuildElementName(name), PrintExpectedElements(names, getParticles), SR.Sch_ComplexContentModel }, sourceUri, lineNo, linePos), XmlSeverityType.Error);
                        }
                        else
                        {
                            SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(SR.Sch_InvalidElementContentExpecting, new string[] { BuildElementName(context.LocalName!, context.Namespace!), BuildElementName(name), PrintExpectedElements(names, getParticles) }, sourceUri, lineNo, linePos), XmlSeverityType.Error);
                        }
                    }
                }
                else
                { //Base ContentValidator: Empty || TextOnly || Mixed || Any
                    if (contentType == XmlSchemaContentType.Empty)
                    {
                        SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(SR.Sch_InvalidElementInEmptyEx, new string[] { QNameString(context.LocalName!, context.Namespace!), name.ToString() }, sourceUri, lineNo, linePos), XmlSeverityType.Error);
                    }
                    else if (!contentValidator.IsOpen)
                    {
                        SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(SR.Sch_InvalidElementInTextOnlyEx, new string[] { QNameString(context.LocalName!, context.Namespace!), name.ToString() }, sourceUri, lineNo, linePos), XmlSeverityType.Error);
                    }
                }
            }
        }

        internal static void CompleteValidationError(ValidationState context, ValidationEventHandler? eventHandler, object sender, string? sourceUri, int lineNo, int linePos, XmlSchemaSet? schemaSet)
        {
            ArrayList? names = null;
            bool getParticles = schemaSet != null;
            if (context.ElementDecl != null)
            {
                if (getParticles)
                {
                    Debug.Assert(schemaSet != null);
                    names = context.ElementDecl.ContentValidator!.ExpectedParticles(context, true, schemaSet);
                }
                else
                {
                    names = context.ElementDecl.ContentValidator!.ExpectedElements(context, true);
                }
            }
            if (names == null || names.Count == 0)
            {
                if (context.TooComplex)
                {
                    SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(SR.Sch_IncompleteContentComplex, new string?[] { BuildElementName(context.LocalName!, context.Namespace!), SR.Sch_ComplexContentModel }, sourceUri, lineNo, linePos), XmlSeverityType.Error);
                }

                SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(SR.Sch_IncompleteContent, BuildElementName(context.LocalName!, context.Namespace!), sourceUri, lineNo, linePos), XmlSeverityType.Error);
            }
            else
            {
                Debug.Assert(names.Count > 0);
                if (context.TooComplex)
                {
                    SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(SR.Sch_IncompleteContentExpectingComplex, new string?[] { BuildElementName(context.LocalName!, context.Namespace!), PrintExpectedElements(names, getParticles), SR.Sch_ComplexContentModel }, sourceUri, lineNo, linePos), XmlSeverityType.Error);
                }
                else
                {
                    SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(SR.Sch_IncompleteContentExpecting, new string?[] { BuildElementName(context.LocalName!, context.Namespace!), PrintExpectedElements(names, getParticles) }, sourceUri, lineNo, linePos), XmlSeverityType.Error);
                }
            }
        }

        internal static string PrintExpectedElements(ArrayList expected, bool getParticles)
        {
            if (getParticles)
            {
                string ContinuationString = SR.Format(SR.Sch_ContinuationString, new string[] { " " });
                XmlSchemaParticle? currentParticle = null;
                XmlSchemaParticle? nextParticle = null;
                XmlQualifiedName currentQName;
                ArrayList expectedNames = new ArrayList();
                StringBuilder builder = new StringBuilder();

                if (expected.Count == 1)
                {
                    nextParticle = expected[0] as XmlSchemaParticle;
                }
                else
                {
                    for (int i = 1; i < expected.Count; i++)
                    {
                        currentParticle = (expected[i - 1] as XmlSchemaParticle)!;
                        nextParticle = (expected[i] as XmlSchemaParticle)!;
                        currentQName = currentParticle.GetQualifiedName();
                        if (currentQName.Namespace != nextParticle.GetQualifiedName().Namespace)
                        {
                            expectedNames.Add(currentQName);
                            PrintNamesWithNS(expectedNames, builder);
                            expectedNames.Clear();
                            Debug.Assert(builder.Length != 0);
                            builder.Append(ContinuationString);
                        }
                        else
                        {
                            expectedNames.Add(currentQName);
                        }
                    }
                }

                //Add last one.
                Debug.Assert(nextParticle != null);
                expectedNames.Add(nextParticle.GetQualifiedName());
                PrintNamesWithNS(expectedNames, builder);

                return builder.ToString();
            }
            else
            {
                return PrintNames(expected);
            }
        }

        private static string PrintNames(ArrayList expected)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Quote);
            builder.Append(expected[0]!.ToString());
            for (int i = 1; i < expected.Count; ++i)
            {
                builder.Append(' ');
                builder.Append(expected[i]!.ToString());
            }
            builder.Append(Quote);
            return builder.ToString();
        }

        private static void PrintNamesWithNS(ArrayList expected, StringBuilder builder)
        {
            XmlQualifiedName name = (expected[0] as XmlQualifiedName)!;

            if (expected.Count == 1)
            { //In case of one element in a namespace or any
                if (name.Name == "*")
                { //Any
                    EnumerateAny(builder, name.Namespace);
                }
                else
                {
                    if (name.Namespace.Length != 0)
                    {
                        builder.Append(SR.Format(SR.Sch_ElementNameAndNamespace, name.Name, name.Namespace));
                    }
                    else
                    {
                        builder.Append(SR.Format(SR.Sch_ElementName, name.Name));
                    }
                }
            }
            else
            {
                bool foundAny = false;
                bool first = true;
                StringBuilder subBuilder = new StringBuilder();
                for (int i = 0; i < expected.Count; i++)
                {
                    name = (expected[i] as XmlQualifiedName)!;
                    if (name.Name == "*")
                    { //rare case where ns of element and that of Any match
                        foundAny = true;
                        continue;
                    }
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        subBuilder.Append(", ");
                    }
                    subBuilder.Append(name.Name);
                }
                if (foundAny)
                {
                    subBuilder.Append(", ");
                    subBuilder.Append(SR.Sch_AnyElement);
                }
                else
                {
                    if (name.Namespace.Length != 0)
                    {
                        builder.Append(SR.Format(SR.Sch_ElementNameAndNamespace, subBuilder, name.Namespace));
                    }
                    else
                    {
                        builder.Append(SR.Format(SR.Sch_ElementName, subBuilder));
                    }
                }
            }
        }

        private static void EnumerateAny(StringBuilder builder, string namespaces)
        {
            StringBuilder subBuilder = new StringBuilder();
            if (namespaces == "##any" || namespaces == "##other")
            {
                subBuilder.Append(namespaces);
            }
            else
            {
                string[] nsList = XmlConvert.SplitString(namespaces);
                Debug.Assert(nsList.Length > 0);
                subBuilder.Append(nsList[0]);
                for (int i = 1; i < nsList.Length; i++)
                {
                    subBuilder.Append(", ");
                    subBuilder.Append(nsList[i]);
                }
            }
            builder.Append(SR.Format(SR.Sch_AnyElementNS, subBuilder));
        }

        internal static string QNameString(string localName, string ns)
        {
            return (ns.Length != 0) ? string.Concat(ns, ":", localName) : localName;
        }

        internal static string BuildElementName(XmlQualifiedName qname)
        {
            return BuildElementName(qname.Name, qname.Namespace);
        }

        internal static string BuildElementName(string localName, string ns)
        {
            if (ns.Length != 0)
            {
                return SR.Format(SR.Sch_ElementNameAndNamespace, localName, ns);
            }
            else
            {
                return SR.Format(SR.Sch_ElementName, localName);
            }
        }

        private void ProcessEntity(string name)
        {
            if (!_checkEntity)
            {
                return;
            }

            IDtdEntityInfo? entityInfo = null;
            if (_dtdSchemaInfo != null)
            {
                entityInfo = _dtdSchemaInfo.LookupEntity(name);
            }
            if (entityInfo == null)
            {
                // validation error, see xml spec [68]
                SendValidationEvent(SR.Sch_UndeclaredEntity, name);
            }
            else if (entityInfo.IsUnparsedEntity)
            {
                // validation error, see xml spec [68]
                SendValidationEvent(SR.Sch_UnparsedEntityRef, name);
            }
        }

        private void SendValidationEvent(string code)
        {
            SendValidationEvent(code, string.Empty);
        }

        private void SendValidationEvent(string? code, string?[]? args)
        {
            SendValidationEvent(new XmlSchemaValidationException(code, args, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition));
        }

        private void SendValidationEvent(string? code, string? arg)
        {
            SendValidationEvent(new XmlSchemaValidationException(code, arg, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition));
        }

        private void SendValidationEvent(string? code, string? arg1, string? arg2)
        {
            SendValidationEvent(new XmlSchemaValidationException(code, new string?[] { arg1, arg2 }, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition));
        }

        private void SendValidationEvent(string? code, string?[]? args, Exception? innerException, XmlSeverityType severity)
        {
            if (severity != XmlSeverityType.Warning || ReportValidationWarnings)
            {
                SendValidationEvent(new XmlSchemaValidationException(code, args, innerException, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition), severity);
            }
        }

        private void SendValidationEvent(string? code, string?[]? args, Exception? innerException)
        {
            SendValidationEvent(new XmlSchemaValidationException(code, args, innerException, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition), XmlSeverityType.Error);
        }

        private void SendValidationEvent(XmlSchemaValidationException e)
        {
            SendValidationEvent(e, XmlSeverityType.Error);
        }

        private void SendValidationEvent(XmlSchemaException e)
        {
            SendValidationEvent(new XmlSchemaValidationException(e.GetRes, e.Args, e.SourceUri, e.LineNumber, e.LinePosition), XmlSeverityType.Error);
        }

        private void SendValidationEvent(string code, string msg, XmlSeverityType severity)
        {
            if (severity != XmlSeverityType.Warning || ReportValidationWarnings)
            {
                SendValidationEvent(new XmlSchemaValidationException(code, msg, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition), severity);
            }
        }

        private void SendValidationEvent(XmlSchemaValidationException e, XmlSeverityType severity)
        {
            bool errorSeverity = false;
            if (severity == XmlSeverityType.Error)
            {
                errorSeverity = true;
                _context.Validity = XmlSchemaValidity.Invalid;
            }
            if (errorSeverity)
            {
                if (_eventHandler != null)
                {
                    _eventHandler(_validationEventSender, new ValidationEventArgs(e, severity));
                }
                else
                {
                    throw e;
                }
            }
            else if (ReportValidationWarnings && _eventHandler != null)
            {
                _eventHandler(_validationEventSender, new ValidationEventArgs(e, severity));
            }
        }

        internal static void SendValidationEvent(ValidationEventHandler? eventHandler, object sender, XmlSchemaValidationException e, XmlSeverityType severity)
        {
            if (eventHandler != null)
            {
                eventHandler(sender, new ValidationEventArgs(e, severity));
            }
            else if (severity == XmlSeverityType.Error)
            {
                throw e;
            }
        }
    } //End of class
} //End of namespace
