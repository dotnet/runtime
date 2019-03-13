// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Xml.Schema;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace System.Xml
{
    internal sealed partial class XmlValidatingReaderImpl : XmlReader, IXmlLineInfo, IXmlNamespaceResolver
    {
        //
        // Private helper types
        //
        // ParsingFunction = what should the reader do when the next Read() is called
        private enum ParsingFunction
        {
            Read = 0,
            Init,
            ParseDtdFromContext,
            ResolveEntityInternally,
            InReadBinaryContent,
            ReaderClosed,
            Error,
            None,
        }

        internal class ValidationEventHandling : IValidationEventHandling
        {
            // Fields
            private XmlValidatingReaderImpl _reader;
            private ValidationEventHandler _eventHandler;

            // Constructor
            internal ValidationEventHandling(XmlValidatingReaderImpl reader)
            {
                _reader = reader;
            }

            // IValidationEventHandling interface
            #region IValidationEventHandling interface
            object IValidationEventHandling.EventHandler
            {
                get { return _eventHandler; }
            }

            void IValidationEventHandling.SendEvent(Exception /*XmlSchemaException*/ exception, XmlSeverityType severity)
            {
                if (_eventHandler != null)
                {
                    _eventHandler(_reader, new ValidationEventArgs((XmlSchemaException)exception, severity));
                }
                else if (_reader._validationType != ValidationType.None && severity == XmlSeverityType.Error)
                {
                    throw exception;
                }
            }
            #endregion

            // XmlValidatingReaderImpl helper methods
            internal void AddHandler(ValidationEventHandler handler)
            {
                _eventHandler += handler;
            }

            internal void RemoveHandler(ValidationEventHandler handler)
            {
                _eventHandler -= handler;
            }
        }

        //
        // Fields
        //
        // core text reader
        private XmlReader _coreReader;
        private XmlTextReaderImpl _coreReaderImpl;
        private IXmlNamespaceResolver _coreReaderNSResolver;

        // validation
        private ValidationType _validationType;
        private BaseValidator _validator;

#pragma warning disable 618
        private XmlSchemaCollection _schemaCollection;
#pragma warning restore 618
        private bool _processIdentityConstraints;

        // parsing function (state)
        private ParsingFunction _parsingFunction = ParsingFunction.Init;

        // event handling
        private ValidationEventHandling _eventHandling;

        // misc
        private XmlParserContext _parserContext;

        // helper for Read[Element]ContentAs{Base64,BinHex} methods
        private ReadContentAsBinaryHelper _readBinaryHelper;

        // Outer XmlReader exposed to the user - either XmlValidatingReader or XmlValidatingReaderImpl (when created via XmlReader.Create).
        // Virtual methods called from within XmlValidatingReaderImpl must be called on the outer reader so in case the user overrides
        // some of the XmlValidatingReader methods we will call the overriden version.
        private XmlReader _outerReader;

        //
        // Constructors
        //
        // Initializes a new instance of XmlValidatingReaderImpl class with the specified XmlReader.
        // This constructor is used when creating XmlValidatingReaderImpl for V1 XmlValidatingReader
        internal XmlValidatingReaderImpl(XmlReader reader)
        {
            XmlAsyncCheckReader asyncCheckReader = reader as XmlAsyncCheckReader;
            if (asyncCheckReader != null)
            {
                reader = asyncCheckReader.CoreReader;
            }
            _outerReader = this;
            _coreReader = reader;
            _coreReaderNSResolver = reader as IXmlNamespaceResolver;
            _coreReaderImpl = reader as XmlTextReaderImpl;
            if (_coreReaderImpl == null)
            {
                XmlTextReader tr = reader as XmlTextReader;
                if (tr != null)
                {
                    _coreReaderImpl = tr.Impl;
                }
            }
            if (_coreReaderImpl == null)
            {
                throw new ArgumentException(SR.Arg_ExpectingXmlTextReader, nameof(reader));
            }
            _coreReaderImpl.EntityHandling = EntityHandling.ExpandEntities;
            _coreReaderImpl.XmlValidatingReaderCompatibilityMode = true;
            _processIdentityConstraints = true;

#pragma warning disable 618
            _schemaCollection = new XmlSchemaCollection(_coreReader.NameTable);
            _schemaCollection.XmlResolver = GetResolver();

            _eventHandling = new ValidationEventHandling(this);
            _coreReaderImpl.ValidationEventHandling = _eventHandling;
            _coreReaderImpl.OnDefaultAttributeUse = new XmlTextReaderImpl.OnDefaultAttributeUseDelegate(ValidateDefaultAttributeOnUse);

            _validationType = ValidationType.Auto;
            SetupValidation(ValidationType.Auto);
#pragma warning restore 618

        }

        // Initializes a new instance of XmlValidatingReaderImpl class for parsing fragments with the specified string, fragment type and parser context
        // This constructor is used when creating XmlValidatingReaderImpl for V1 XmlValidatingReader
        // SxS: This method resolves an Uri but does not expose it to the caller. It's OK to suppress the SxS warning.
        internal XmlValidatingReaderImpl(string xmlFragment, XmlNodeType fragType, XmlParserContext context)
            : this(new XmlTextReader(xmlFragment, fragType, context))
        {
            if (_coreReader.BaseURI.Length > 0)
            {
                _validator.BaseUri = GetResolver().ResolveUri(null, _coreReader.BaseURI);
            }

            if (context != null)
            {
                _parsingFunction = ParsingFunction.ParseDtdFromContext;
                _parserContext = context;
            }
        }

        // Initializes a new instance of XmlValidatingReaderImpl class for parsing fragments with the specified stream, fragment type and parser context
        // This constructor is used when creating XmlValidatingReaderImpl for V1 XmlValidatingReader
        // SxS: This method resolves an Uri but does not expose it to the caller. It's OK to suppress the SxS warning.
        internal XmlValidatingReaderImpl(Stream xmlFragment, XmlNodeType fragType, XmlParserContext context)
            : this(new XmlTextReader(xmlFragment, fragType, context))
        {
            if (_coreReader.BaseURI.Length > 0)
            {
                _validator.BaseUri = GetResolver().ResolveUri(null, _coreReader.BaseURI);
            }

            if (context != null)
            {
                _parsingFunction = ParsingFunction.ParseDtdFromContext;
                _parserContext = context;
            }
        }

        // Initializes a new instance of XmlValidatingReaderImpl class with the specified arguments.
        // This constructor is used when creating XmlValidatingReaderImpl reader via "XmlReader.Create(..)"
        internal XmlValidatingReaderImpl(XmlReader reader, ValidationEventHandler settingsEventHandler, bool processIdentityConstraints)
        {
            XmlAsyncCheckReader asyncCheckReader = reader as XmlAsyncCheckReader;
            if (asyncCheckReader != null)
            {
                reader = asyncCheckReader.CoreReader;
            }
            _outerReader = this;
            _coreReader = reader;
            _coreReaderImpl = reader as XmlTextReaderImpl;
            if (_coreReaderImpl == null)
            {
                XmlTextReader tr = reader as XmlTextReader;
                if (tr != null)
                {
                    _coreReaderImpl = tr.Impl;
                }
            }
            if (_coreReaderImpl == null)
            {
                throw new ArgumentException(SR.Arg_ExpectingXmlTextReader, nameof(reader));
            }
            _coreReaderImpl.XmlValidatingReaderCompatibilityMode = true;
            _coreReaderNSResolver = reader as IXmlNamespaceResolver;
            _processIdentityConstraints = processIdentityConstraints;

#pragma warning disable 618

            _schemaCollection = new XmlSchemaCollection(_coreReader.NameTable);

#pragma warning restore 618

            _schemaCollection.XmlResolver = GetResolver();

            _eventHandling = new ValidationEventHandling(this);
            if (settingsEventHandler != null)
            {
                _eventHandling.AddHandler(settingsEventHandler);
            }
            _coreReaderImpl.ValidationEventHandling = _eventHandling;
            _coreReaderImpl.OnDefaultAttributeUse = new XmlTextReaderImpl.OnDefaultAttributeUseDelegate(ValidateDefaultAttributeOnUse);

            _validationType = ValidationType.DTD;
            SetupValidation(ValidationType.DTD);
        }

        //
        // XmlReader members
        //
        // Returns the current settings of the reader
        public override XmlReaderSettings Settings
        {
            get
            {
                XmlReaderSettings settings;
                if (_coreReaderImpl.V1Compat)
                {
                    settings = null;
                }
                else
                {
                    settings = _coreReader.Settings;
                }
                if (settings != null)
                {
                    settings = settings.Clone();
                }
                else
                {
                    settings = new XmlReaderSettings();
                }
                settings.ValidationType = ValidationType.DTD;
                if (!_processIdentityConstraints)
                {
                    settings.ValidationFlags &= ~XmlSchemaValidationFlags.ProcessIdentityConstraints;
                }
                settings.ReadOnly = true;
                return settings;
            }
        }

        // Returns the type of the current node.
        public override XmlNodeType NodeType
        {
            get
            {
                return _coreReader.NodeType;
            }
        }

        // Returns the name of the current node, including prefix.
        public override string Name
        {
            get
            {
                return _coreReader.Name;
            }
        }

        // Returns local name of the current node (without prefix)
        public override string LocalName
        {
            get
            {
                return _coreReader.LocalName;
            }
        }

        // Returns namespace name of the current node.
        public override string NamespaceURI
        {
            get
            {
                return _coreReader.NamespaceURI;
            }
        }

        // Returns prefix associated with the current node.
        public override string Prefix
        {
            get
            {
                return _coreReader.Prefix;
            }
        }

        // Returns true if the current node can have Value property != string.Empty.
        public override bool HasValue
        {
            get
            {
                return _coreReader.HasValue;
            }
        }

        // Returns the text value of the current node.
        public override string Value
        {
            get
            {
                return _coreReader.Value;
            }
        }

        // Returns the depth of the current node in the XML element stack
        public override int Depth
        {
            get
            {
                return _coreReader.Depth;
            }
        }

        // Returns the base URI of the current node.
        public override string BaseURI
        {
            get
            {
                return _coreReader.BaseURI;
            }
        }

        // Returns true if the current node is an empty element (for example, <MyElement/>).
        public override bool IsEmptyElement
        {
            get
            {
                return _coreReader.IsEmptyElement;
            }
        }

        // Returns true of the current node is a default attribute declared in DTD.
        public override bool IsDefault
        {
            get
            {
                return _coreReader.IsDefault;
            }
        }

        // Returns the quote character used in the current attribute declaration
        public override char QuoteChar
        {
            get
            {
                return _coreReader.QuoteChar;
            }
        }

        // Returns the current xml:space scope.
        public override XmlSpace XmlSpace
        {
            get
            {
                return _coreReader.XmlSpace;
            }
        }

        // Returns the current xml:lang scope.</para>
        public override string XmlLang
        {
            get
            {
                return _coreReader.XmlLang;
            }
        }

        // Returns the current read state of the reader
        public override ReadState ReadState
        {
            get
            {
                return (_parsingFunction == ParsingFunction.Init) ? ReadState.Initial : _coreReader.ReadState;
            }
        }

        // Returns true if the reader reached end of the input data
        public override bool EOF
        {
            get
            {
                return _coreReader.EOF;
            }
        }

        // Returns the XmlNameTable associated with this XmlReader
        public override XmlNameTable NameTable
        {
            get
            {
                return _coreReader.NameTable;
            }
        }

        // Returns encoding of the XML document
        internal Encoding Encoding
        {
            get
            {
                return _coreReaderImpl.Encoding;
            }
        }

        // Returns the number of attributes on the current node.
        public override int AttributeCount
        {
            get
            {
                return _coreReader.AttributeCount;
            }
        }

        // Returns value of an attribute with the specified Name
        public override string GetAttribute(string name)
        {
            return _coreReader.GetAttribute(name);
        }

        // Returns value of an attribute with the specified LocalName and NamespaceURI
        public override string GetAttribute(string localName, string namespaceURI)
        {
            return _coreReader.GetAttribute(localName, namespaceURI);
        }

        // Returns value of an attribute at the specified index (position)
        public override string GetAttribute(int i)
        {
            return _coreReader.GetAttribute(i);
        }

        // Moves to an attribute with the specified Name
        public override bool MoveToAttribute(string name)
        {
            if (!_coreReader.MoveToAttribute(name))
            {
                return false;
            }
            _parsingFunction = ParsingFunction.Read;
            return true;
        }

        // Moves to an attribute with the specified LocalName and NamespceURI
        public override bool MoveToAttribute(string localName, string namespaceURI)
        {
            if (!_coreReader.MoveToAttribute(localName, namespaceURI))
            {
                return false;
            }
            _parsingFunction = ParsingFunction.Read;
            return true;
        }

        // Moves to an attribute at the specified index (position)
        public override void MoveToAttribute(int i)
        {
            _coreReader.MoveToAttribute(i);
            _parsingFunction = ParsingFunction.Read;
        }

        // Moves to the first attribute of the current node
        public override bool MoveToFirstAttribute()
        {
            if (!_coreReader.MoveToFirstAttribute())
            {
                return false;
            }
            _parsingFunction = ParsingFunction.Read;
            return true;
        }

        // Moves to the next attribute of the current node
        public override bool MoveToNextAttribute()
        {
            if (!_coreReader.MoveToNextAttribute())
            {
                return false;
            }
            _parsingFunction = ParsingFunction.Read;
            return true;
        }

        // If on attribute, moves to the element that contains the attribute node
        public override bool MoveToElement()
        {
            if (!_coreReader.MoveToElement())
            {
                return false;
            }
            _parsingFunction = ParsingFunction.Read;
            return true;
        }

        // Reads and validated next node from the input data
        public override bool Read()
        {
            switch (_parsingFunction)
            {
                case ParsingFunction.Read:
                    if (_coreReader.Read())
                    {
                        ProcessCoreReaderEvent();
                        return true;
                    }
                    else
                    {
                        _validator.CompleteValidation();
                        return false;
                    }
                case ParsingFunction.ParseDtdFromContext:
                    _parsingFunction = ParsingFunction.Read;
                    ParseDtdFromParserContext();
                    goto case ParsingFunction.Read;
                case ParsingFunction.Error:
                case ParsingFunction.ReaderClosed:
                    return false;
                case ParsingFunction.Init:
                    _parsingFunction = ParsingFunction.Read; // this changes the value returned by ReadState
                    if (_coreReader.ReadState == ReadState.Interactive)
                    {
                        ProcessCoreReaderEvent();
                        return true;
                    }
                    else
                    {
                        goto case ParsingFunction.Read;
                    }
                case ParsingFunction.ResolveEntityInternally:
                    _parsingFunction = ParsingFunction.Read;
                    ResolveEntityInternally();
                    goto case ParsingFunction.Read;
                case ParsingFunction.InReadBinaryContent:
                    _parsingFunction = ParsingFunction.Read;
                    _readBinaryHelper.Finish();
                    goto case ParsingFunction.Read;
                default:
                    Debug.Fail($"Unexpected parsing function {_parsingFunction}");
                    return false;
            }
        }

        // Closes the input stream ot TextReader, changes the ReadState to Closed and sets all properties to zero/string.Empty
        public override void Close()
        {
            _coreReader.Close();
            _parsingFunction = ParsingFunction.ReaderClosed;
        }

        // Returns NamespaceURI associated with the specified prefix in the current namespace scope.
        public override string LookupNamespace(string prefix)
        {
            return _coreReaderImpl.LookupNamespace(prefix);
        }

        // Iterates through the current attribute value's text and entity references chunks.
        public override bool ReadAttributeValue()
        {
            if (_parsingFunction == ParsingFunction.InReadBinaryContent)
            {
                _parsingFunction = ParsingFunction.Read;
                _readBinaryHelper.Finish();
            }
            if (!_coreReader.ReadAttributeValue())
            {
                return false;
            }
            _parsingFunction = ParsingFunction.Read;
            return true;
        }

        public override bool CanReadBinaryContent
        {
            get
            {
                return true;
            }
        }

        public override int ReadContentAsBase64(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadChunkHelper if called the first time
            if (_parsingFunction != ParsingFunction.InReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
            }

            // set parsingFunction to Read state in order to have a normal Read() behavior when called from readBinaryHelper
            _parsingFunction = ParsingFunction.Read;

            // call to the helper
            int readCount = _readBinaryHelper.ReadContentAsBase64(buffer, index, count);

            // setup parsingFunction 
            _parsingFunction = ParsingFunction.InReadBinaryContent;
            return readCount;
        }

        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadChunkHelper when called first time
            if (_parsingFunction != ParsingFunction.InReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
            }

            // set parsingFunction to Read state in order to have a normal Read() behavior when called from readBinaryHelper
            _parsingFunction = ParsingFunction.Read;

            // call to the helper
            int readCount = _readBinaryHelper.ReadContentAsBinHex(buffer, index, count);

            // setup parsingFunction 
            _parsingFunction = ParsingFunction.InReadBinaryContent;
            return readCount;
        }

        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadChunkHelper if called the first time
            if (_parsingFunction != ParsingFunction.InReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
            }

            // set parsingFunction to Read state in order to have a normal Read() behavior when called from readBinaryHelper
            _parsingFunction = ParsingFunction.Read;

            // call to the helper
            int readCount = _readBinaryHelper.ReadElementContentAsBase64(buffer, index, count);

            // setup parsingFunction 
            _parsingFunction = ParsingFunction.InReadBinaryContent;
            return readCount;
        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadChunkHelper when called first time
            if (_parsingFunction != ParsingFunction.InReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
            }

            // set parsingFunction to Read state in order to have a normal Read() behavior when called from readBinaryHelper
            _parsingFunction = ParsingFunction.Read;

            // call to the helper
            int readCount = _readBinaryHelper.ReadElementContentAsBinHex(buffer, index, count);

            // setup parsingFunction 
            _parsingFunction = ParsingFunction.InReadBinaryContent;
            return readCount;
        }

        // Returns true if the XmlReader knows how to resolve general entities
        public override bool CanResolveEntity
        {
            get
            {
                return true;
            }
        }

        // Resolves the current entity reference node
        public override void ResolveEntity()
        {
            if (_parsingFunction == ParsingFunction.ResolveEntityInternally)
            {
                _parsingFunction = ParsingFunction.Read;
            }
            _coreReader.ResolveEntity();
        }

        internal XmlReader OuterReader
        {
            get
            {
                return _outerReader;
            }
            set
            {
#pragma warning disable 618
                Debug.Assert(value is XmlValidatingReader);
#pragma warning restore 618
                _outerReader = value;
            }
        }

        internal void MoveOffEntityReference()
        {
            if (_outerReader.NodeType == XmlNodeType.EntityReference && _parsingFunction != ParsingFunction.ResolveEntityInternally)
            {
                if (!_outerReader.Read())
                {
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                }
            }
        }

        public override string ReadString()
        {
            MoveOffEntityReference();
            return base.ReadString();
        }

        //
        // IXmlLineInfo members
        //
        public bool HasLineInfo()
        {
            return true;
        }

        // Returns the line number of the current node
        public int LineNumber
        {
            get
            {
                return ((IXmlLineInfo)_coreReader).LineNumber;
            }
        }

        // Returns the line number of the current node
        public int LinePosition
        {
            get
            {
                return ((IXmlLineInfo)_coreReader).LinePosition;
            }
        }

        //
        // IXmlNamespaceResolver members
        //
        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return this.GetNamespacesInScope(scope);
        }

        string IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            return this.LookupNamespace(prefix);
        }

        string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return this.LookupPrefix(namespaceName);
        }

        // Internal IXmlNamespaceResolver methods
        internal IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return _coreReaderNSResolver.GetNamespacesInScope(scope);
        }

        internal string LookupPrefix(string namespaceName)
        {
            return _coreReaderNSResolver.LookupPrefix(namespaceName);
        }

        //
        // XmlValidatingReader members
        //
        // Specufies the validation event handler that wil get warnings and errors related to validation
        internal event ValidationEventHandler ValidationEventHandler
        {
            add
            {
                _eventHandling.AddHandler(value);
            }
            remove
            {
                _eventHandling.RemoveHandler(value); ;
            }
        }

        // returns the schema type of the current node
        internal object SchemaType
        {
            get
            {
                if (_validationType != ValidationType.None)
                {
                    XmlSchemaType schemaTypeObj = _coreReaderImpl.InternalSchemaType as XmlSchemaType;
                    if (schemaTypeObj != null && schemaTypeObj.QualifiedName.Namespace == XmlReservedNs.NsXs)
                    {
                        return schemaTypeObj.Datatype;
                    }
                    return _coreReaderImpl.InternalSchemaType;
                }
                else
                    return null;
            }
        }

        // returns the underlying XmlTextReader or XmlTextReaderImpl
        internal XmlReader Reader
        {
            get
            {
                return (XmlReader)_coreReader;
            }
        }

        // returns the underlying XmlTextReaderImpl
        internal XmlTextReaderImpl ReaderImpl
        {
            get
            {
                return _coreReaderImpl;
            }
        }

        // specifies the validation type (None, DDT, XSD, XDR, Auto)
        internal ValidationType ValidationType
        {
            get
            {
                return _validationType;
            }
            set
            {
                if (ReadState != ReadState.Initial)
                {
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                }
                _validationType = value;
                SetupValidation(value);
            }
        }

        // current schema collection used for validationg
#pragma warning disable 618
        internal XmlSchemaCollection Schemas
        {
            get
            {
                return _schemaCollection;
            }
        }
#pragma warning restore 618

        // Spefifies whether general entities should be automatically expanded or not
        internal EntityHandling EntityHandling
        {
            get
            {
                return _coreReaderImpl.EntityHandling;
            }
            set
            {
                _coreReaderImpl.EntityHandling = value;
            }
        }

        // Specifies XmlResolver used for opening the XML document and other external references
        internal XmlResolver XmlResolver
        {
            set
            {
                _coreReaderImpl.XmlResolver = value;
                _validator.XmlResolver = value;
                _schemaCollection.XmlResolver = value;
            }
        }

        // Disables or enables support of W3C XML 1.0 Namespaces
        internal bool Namespaces
        {
            get
            {
                return _coreReaderImpl.Namespaces;
            }
            set
            {
                _coreReaderImpl.Namespaces = value;
            }
        }

        // Returns typed value of the current node (based on the type specified by schema)
        public object ReadTypedValue()
        {
            if (_validationType == ValidationType.None)
            {
                return null;
            }

            switch (_outerReader.NodeType)
            {
                case XmlNodeType.Attribute:
                    return _coreReaderImpl.InternalTypedValue;
                case XmlNodeType.Element:
                    if (SchemaType == null)
                    {
                        return null;
                    }
                    XmlSchemaDatatype dtype = (SchemaType is XmlSchemaDatatype) ? (XmlSchemaDatatype)SchemaType : ((XmlSchemaType)SchemaType).Datatype;
                    if (dtype != null)
                    {
                        if (!_outerReader.IsEmptyElement)
                        {
                            for (;;)
                            {
                                if (!_outerReader.Read())
                                {
                                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                                }
                                XmlNodeType type = _outerReader.NodeType;
                                if (type != XmlNodeType.CDATA && type != XmlNodeType.Text &&
                                    type != XmlNodeType.Whitespace && type != XmlNodeType.SignificantWhitespace &&
                                    type != XmlNodeType.Comment && type != XmlNodeType.ProcessingInstruction)
                                {
                                    break;
                                }
                            }
                            if (_outerReader.NodeType != XmlNodeType.EndElement)
                            {
                                throw new XmlException(SR.Xml_InvalidNodeType, _outerReader.NodeType.ToString());
                            }
                        }
                        return _coreReaderImpl.InternalTypedValue;
                    }
                    return null;

                case XmlNodeType.EndElement:
                    return null;

                default:
                    if (_coreReaderImpl.V1Compat)
                    { //If v1 XmlValidatingReader return null
                        return null;
                    }
                    else
                    {
                        return Value;
                    }
            }
        }

        //
        // Private implementation methods
        //

        private void ParseDtdFromParserContext()
        {
            Debug.Assert(_parserContext != null);
            Debug.Assert(_coreReaderImpl.DtdInfo == null);

            if (_parserContext.DocTypeName == null || _parserContext.DocTypeName.Length == 0)
            {
                return;
            }

            IDtdParser dtdParser = DtdParser.Create();
            XmlTextReaderImpl.DtdParserProxy proxy = new XmlTextReaderImpl.DtdParserProxy(_coreReaderImpl);
            IDtdInfo dtdInfo = dtdParser.ParseFreeFloatingDtd(_parserContext.BaseURI, _parserContext.DocTypeName, _parserContext.PublicId,
                                                              _parserContext.SystemId, _parserContext.InternalSubset, proxy);
            _coreReaderImpl.SetDtdInfo(dtdInfo);

            ValidateDtd();
        }

        private void ValidateDtd()
        {
            IDtdInfo dtdInfo = _coreReaderImpl.DtdInfo;
            if (dtdInfo != null)
            {
                switch (_validationType)
                {
#pragma warning disable 618
                    case ValidationType.Auto:
                        SetupValidation(ValidationType.DTD);
                        goto case ValidationType.DTD;
#pragma warning restore 618
                    case ValidationType.DTD:
                    case ValidationType.None:
                        _validator.DtdInfo = dtdInfo;
                        break;
                }
            }
        }

        private void ResolveEntityInternally()
        {
            Debug.Assert(_coreReader.NodeType == XmlNodeType.EntityReference);
            int initialDepth = _coreReader.Depth;
            _outerReader.ResolveEntity();
            while (_outerReader.Read() && _coreReader.Depth > initialDepth) ;
        }

        // SxS: This method resolves an Uri but does not expose it to caller. It's OK to suppress the SxS warning.
        private void SetupValidation(ValidationType valType)
        {
            _validator = BaseValidator.CreateInstance(valType, this, _schemaCollection, _eventHandling, _processIdentityConstraints);

            XmlResolver resolver = GetResolver();
            _validator.XmlResolver = resolver;

            if (_outerReader.BaseURI.Length > 0)
            {
                _validator.BaseUri = (resolver == null) ? new Uri(_outerReader.BaseURI, UriKind.RelativeOrAbsolute) : resolver.ResolveUri(null, _outerReader.BaseURI);
            }
            _coreReaderImpl.ValidationEventHandling = (_validationType == ValidationType.None) ? null : _eventHandling;
        }

        private static XmlResolver s_tempResolver;

        // This is needed because we can't have the setter for XmlResolver public and with internal getter.
        private XmlResolver GetResolver()
        {
            XmlResolver tempResolver = _coreReaderImpl.GetResolver();

            if (tempResolver == null && !_coreReaderImpl.IsResolverSet)
            {
                // it is safe to return valid resolver as it'll be used in the schema validation 
                if (s_tempResolver == null)
                    s_tempResolver = new XmlUrlResolver();
                return s_tempResolver;
            }

            return tempResolver;
        }

        //
        // Internal methods for validators, DOM, XPathDocument etc.
        //
        private void ProcessCoreReaderEvent()
        {
            switch (_coreReader.NodeType)
            {
                case XmlNodeType.Whitespace:
                    if (_coreReader.Depth > 0 || _coreReaderImpl.FragmentType != XmlNodeType.Document)
                    {
                        if (_validator.PreserveWhitespace)
                        {
                            _coreReaderImpl.ChangeCurrentNodeType(XmlNodeType.SignificantWhitespace);
                        }
                    }
                    goto default;
                case XmlNodeType.DocumentType:
                    ValidateDtd();
                    break;
                case XmlNodeType.EntityReference:
                    _parsingFunction = ParsingFunction.ResolveEntityInternally;
                    goto default;
                default:
                    _coreReaderImpl.InternalSchemaType = null;
                    _coreReaderImpl.InternalTypedValue = null;
                    _validator.Validate();
                    break;
            }
        }

        internal BaseValidator Validator
        {
            get
            {
                return _validator;
            }
            set
            {
                _validator = value;
            }
        }

        internal override XmlNamespaceManager NamespaceManager
        {
            get
            {
                return _coreReaderImpl.NamespaceManager;
            }
        }

        internal bool StandAlone
        {
            get
            {
                return _coreReaderImpl.StandAlone;
            }
        }

        internal object SchemaTypeObject
        {
            set
            {
                _coreReaderImpl.InternalSchemaType = value;
            }
        }

        internal object TypedValueObject
        {
            get
            {
                return _coreReaderImpl.InternalTypedValue;
            }
            set
            {
                _coreReaderImpl.InternalTypedValue = value;
            }
        }

        internal bool AddDefaultAttribute(SchemaAttDef attdef)
        {
            return _coreReaderImpl.AddDefaultAttributeNonDtd(attdef);
        }

        internal override IDtdInfo DtdInfo
        {
            get { return _coreReaderImpl.DtdInfo; }
        }

        internal void ValidateDefaultAttributeOnUse(IDtdDefaultAttributeInfo defaultAttribute, XmlTextReaderImpl coreReader)
        {
            SchemaAttDef attdef = defaultAttribute as SchemaAttDef;
            if (attdef == null)
            {
                return;
            }

            if (!attdef.DefaultValueChecked)
            {
                SchemaInfo schemaInfo = coreReader.DtdInfo as SchemaInfo;
                if (schemaInfo == null)
                {
                    return;
                }
                DtdValidator.CheckDefaultValue(attdef, schemaInfo, _eventHandling, coreReader.BaseURI);
            }
        }
    }
}

