// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Diagnostics;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    internal delegate void CachingEventHandler(XsdCachingReader cachingReader);

    internal sealed class AttributePSVIInfo
    {
        internal string? localName;
        internal string? namespaceUri;
        internal object? typedAttributeValue;
        internal XmlSchemaInfo attributeSchemaInfo;

        internal AttributePSVIInfo()
        {
            attributeSchemaInfo = new XmlSchemaInfo();
        }

        internal void Reset()
        {
            typedAttributeValue = null;
            localName = string.Empty;
            namespaceUri = string.Empty;
            attributeSchemaInfo.Clear();
        }
    }

    internal sealed partial class XsdValidatingReader : XmlReader, IXmlSchemaInfo, IXmlLineInfo, IXmlNamespaceResolver
    {
        private enum ValidatingReaderState
        {
            None = 0,
            Init = 1,
            Read = 2,
            OnDefaultAttribute = -1,
            OnReadAttributeValue = -2,
            OnAttribute = 3,
            ClearAttributes = 4,
            ParseInlineSchema = 5,
            ReadAhead = 6,
            OnReadBinaryContent = 7,
            ReaderClosed = 8,
            EOF = 9,
            Error = 10,
        }

        // Validation
        private XmlReader _coreReader;
        private readonly IXmlNamespaceResolver? _coreReaderNSResolver;
        private readonly IXmlNamespaceResolver _thisNSResolver;
        private XmlSchemaValidator _validator = null!;
        private readonly XmlResolver? _xmlResolver;
        private readonly ValidationEventHandler? _validationEvent;
        private ValidatingReaderState _validationState;
        private XmlValueGetter _valueGetter;

        // namespace management
        private readonly XmlNamespaceManager? _nsManager;
        private readonly bool _manageNamespaces;
        private readonly bool _processInlineSchema;
        private bool _replayCache;

        // Current Node handling
        private ValidatingReaderNodeData? _cachedNode; // Used to cache current node when looking ahead or default attributes
        private AttributePSVIInfo? _attributePSVI;

        // Attributes
        private int _attributeCount; // Total count of attributes including default
        private int _coreReaderAttributeCount;
        private int _currentAttrIndex;
        private AttributePSVIInfo[] _attributePSVINodes;
        private ArrayList _defaultAttributes;

        // Inline Schema
        private Parser? _inlineSchemaParser;

        // Typed Value & PSVI
        private object? _atomicValue;
        private XmlSchemaInfo _xmlSchemaInfo;

        // original string of the atomic value
        private string? _originalAtomicValueString;

        // cached coreReader information
        private readonly XmlNameTable _coreReaderNameTable;
        private XsdCachingReader? _cachingReader;

        // ReadAttributeValue TextNode
        private ValidatingReaderNodeData? _textNode;

        // To avoid SchemaNames creation
        private string _nsXmlNs;
        private string _nsXs;
        private string _nsXsi;
        private string _xsiType;
        private string _xsiNil;
        private string _xsdSchema;
        private string _xsiSchemaLocation;
        private string _xsiNoNamespaceSchemaLocation;

        // Underlying reader's IXmlLineInfo
        private IXmlLineInfo? _lineInfo;

        // helpers for Read[Element]ContentAs{Base64,BinHex} methods
        private ReadContentAsBinaryHelper? _readBinaryHelper;
        private ValidatingReaderState _savedState;

        // Constants
        private const int InitialAttributeCount = 8;

        private static volatile Type s_typeOfString = null!;

        // Constructor
        internal XsdValidatingReader(XmlReader reader, XmlResolver? xmlResolver, XmlReaderSettings readerSettings, XmlSchemaObject? partialValidationType)
        {
            _coreReader = reader;
            _coreReaderNSResolver = reader as IXmlNamespaceResolver;
            _lineInfo = reader as IXmlLineInfo;
            _coreReaderNameTable = _coreReader.NameTable;
            if (_coreReaderNSResolver == null)
            {
                _nsManager = new XmlNamespaceManager(_coreReaderNameTable);
                _manageNamespaces = true;
            }

            _thisNSResolver = this as IXmlNamespaceResolver;
            _xmlResolver = xmlResolver;
            _processInlineSchema = (readerSettings.ValidationFlags & XmlSchemaValidationFlags.ProcessInlineSchema) != 0;

            _validationState = ValidatingReaderState.Init;
            _defaultAttributes = new ArrayList();
            _currentAttrIndex = -1;
            _attributePSVINodes = new AttributePSVIInfo[InitialAttributeCount];
            _valueGetter = new XmlValueGetter(GetStringValue);
            s_typeOfString = typeof(string);
            _xmlSchemaInfo = new XmlSchemaInfo();

            // Add common strings to be compared to NameTable
            _nsXmlNs = _coreReaderNameTable.Add(XmlReservedNs.NsXmlNs);
            _nsXs = _coreReaderNameTable.Add(XmlReservedNs.NsXs);
            _nsXsi = _coreReaderNameTable.Add(XmlReservedNs.NsXsi);
            _xsiType = _coreReaderNameTable.Add("type");
            _xsiNil = _coreReaderNameTable.Add("nil");
            _xsiSchemaLocation = _coreReaderNameTable.Add("schemaLocation");
            _xsiNoNamespaceSchemaLocation = _coreReaderNameTable.Add("noNamespaceSchemaLocation");
            _xsdSchema = _coreReaderNameTable.Add("schema");

            SetupValidator(readerSettings, reader, partialValidationType);
            _validationEvent = readerSettings.GetEventHandler();
        }

        internal XsdValidatingReader(XmlReader reader, XmlResolver? xmlResolver, XmlReaderSettings readerSettings)
            : this(reader, xmlResolver, readerSettings, null)
        { }

        [MemberNotNull("_validator")]
        private void SetupValidator(XmlReaderSettings readerSettings, XmlReader reader, XmlSchemaObject? partialValidationType)
        {
            _validator = new XmlSchemaValidator(_coreReaderNameTable, readerSettings.Schemas, _thisNSResolver, readerSettings.ValidationFlags);
            _validator.XmlResolver = _xmlResolver;
            _validator.SourceUri = XmlConvert.ToUri(reader.BaseURI); // Not using XmlResolver.ResolveUri as it checks for relative Uris,reader.BaseURI will be absolute file paths or string.Empty
            _validator.ValidationEventSender = this;
            _validator.ValidationEventHandler += readerSettings.GetEventHandler();
            _validator.LineInfoProvider = _lineInfo;

            if (_validator.ProcessSchemaHints)
            {
                _validator.SchemaSet.ReaderSettings.DtdProcessing = readerSettings.DtdProcessing;
            }

            _validator.SetDtdSchemaInfo(reader.DtdInfo);
            if (partialValidationType != null)
            {
                _validator.Initialize(partialValidationType);
            }
            else
            {
                _validator.Initialize();
            }
        }

        // Settings
        public override XmlReaderSettings Settings
        {
            get
            {
                XmlReaderSettings? settings = _coreReader.Settings;
                settings = settings != null ? settings.Clone() : new XmlReaderSettings();

                settings.Schemas = _validator.SchemaSet;
                settings.ValidationType = ValidationType.Schema;
                settings.ValidationFlags = _validator.ValidationFlags;
                settings.ReadOnly = true;
                return settings;
            }
        }

        // Node Properties

        // Gets the type of the current node.
        public override XmlNodeType NodeType
        {
            get
            {
                if ((int)_validationState < 0)
                {
                    Debug.Assert(_cachedNode != null);
                    return _cachedNode.NodeType;
                }
                else
                {
                    XmlNodeType nodeType = _coreReader.NodeType;
                    // Check for significant whitespace
                    if (nodeType == XmlNodeType.Whitespace && (_validator.CurrentContentType == XmlSchemaContentType.TextOnly || _validator.CurrentContentType == XmlSchemaContentType.Mixed))
                    {
                        return XmlNodeType.SignificantWhitespace;
                    }

                    return nodeType;
                }
            }
        }

        // Gets the name of the current node, including the namespace prefix.
        public override string Name
        {
            get
            {
                if (_validationState == ValidatingReaderState.OnDefaultAttribute)
                {
                    Debug.Assert(_cachedNode != null);
                    string? prefix = _validator.GetDefaultAttributePrefix(_cachedNode.Namespace);
                    if (prefix != null && prefix.Length != 0)
                    {
                        return $"{prefix}:{_cachedNode.LocalName}";
                    }

                    return _cachedNode.LocalName;
                }

                return _coreReader.Name;
            }
        }

        // Gets the name of the current node without the namespace prefix.
        public override string LocalName
        {
            get
            {
                if ((int)_validationState < 0)
                {
                    Debug.Assert(_cachedNode != null);
                    return _cachedNode.LocalName;
                }

                return _coreReader.LocalName;
            }
        }

        // Gets the namespace URN (as defined in the W3C Namespace Specification) of the current namespace scope.
        public override string NamespaceURI
        {
            get
            {
                if ((int)_validationState < 0)
                {
                    Debug.Assert(_cachedNode != null);
                    return _cachedNode.Namespace;
                }

                return _coreReader.NamespaceURI;
            }
        }

        // Gets the namespace prefix associated with the current node.
        public override string Prefix
        {
            get
            {
                if ((int)_validationState < 0)
                {
                    Debug.Assert(_cachedNode != null);
                    return _cachedNode.Prefix;
                }

                return _coreReader.Prefix;
            }
        }

        // Gets a value indicating whether the current node can have a non-empty Value
        public override bool HasValue
        {
            get
            {
                if ((int)_validationState < 0)
                {
                    return true;
                }

                return _coreReader.HasValue;
            }
        }

        // Gets the text value of the current node.
        public override string Value
        {
            get
            {
                if ((int)_validationState < 0)
                {
                    Debug.Assert(_cachedNode != null);
                    return _cachedNode.RawValue;
                }

                return _coreReader.Value;
            }
        }

        // Gets the depth of the current node in the XML element stack.
        public override int Depth
        {
            get
            {
                if ((int)_validationState < 0)
                {
                    Debug.Assert(_cachedNode != null);
                    return _cachedNode.Depth;
                }

                return _coreReader.Depth;
            }
        }

        // Gets the base URI of the current node.
        public override string BaseURI
        {
            get
            {
                return _coreReader.BaseURI;
            }
        }

        // Gets a value indicating whether the current node is an empty element (for example, <MyElement/>).
        public override bool IsEmptyElement
        {
            get
            {
                return _coreReader.IsEmptyElement;
            }
        }

        // Gets a value indicating whether the current node is an attribute that was generated from the default value defined
        // in the DTD or schema.
        public override bool IsDefault
        {
            get
            {
                if (_validationState == ValidatingReaderState.OnDefaultAttribute)
                {
                    // XSD default attributes
                    return true;
                }

                return _coreReader.IsDefault; // This is DTD Default attribute
            }
        }

        // Gets the quotation mark character used to enclose the value of an attribute node.
        public override char QuoteChar
        {
            get
            {
                return _coreReader.QuoteChar;
            }
        }

        // Gets the current xml:space scope.
        public override XmlSpace XmlSpace
        {
            get
            {
                return _coreReader.XmlSpace;
            }
        }

        // Gets the current xml:lang scope.
        public override string XmlLang
        {
            get
            {
                return _coreReader.XmlLang;
            }
        }

        public override IXmlSchemaInfo SchemaInfo
        {
            get
            {
                return this as IXmlSchemaInfo;
            }
        }

        public override System.Type ValueType
        {
            get
            {
                switch (NodeType)
                {
                    case XmlNodeType.Element:
                    case XmlNodeType.EndElement:
                        if (_xmlSchemaInfo.ContentType == XmlSchemaContentType.TextOnly)
                        {
                            Debug.Assert(_xmlSchemaInfo.SchemaType!.Datatype != null);
                            return _xmlSchemaInfo.SchemaType.Datatype.ValueType;
                        }

                        goto default;

                    case XmlNodeType.Attribute:
                        if (_attributePSVI != null && AttributeSchemaInfo.ContentType == XmlSchemaContentType.TextOnly)
                        {
                            Debug.Assert(AttributeSchemaInfo.SchemaType!.Datatype != null);
                            return AttributeSchemaInfo.SchemaType.Datatype.ValueType;
                        }

                        goto default;

                    default:
                        return s_typeOfString;
                }
            }
        }

        public override object ReadContentAsObject()
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAsObject));
            }

            return InternalReadContentAsObject(true);
        }

        public override bool ReadContentAsBoolean()
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAsBoolean));
            }

            object typedValue = InternalReadContentAsObject();
            XmlSchemaType? xmlType = NodeType == XmlNodeType.Attribute ? AttributeXmlType : ElementXmlType;
            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToBoolean(typedValue);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToBoolean(typedValue);
                }
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Boolean", e, this as IXmlLineInfo);
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Boolean", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Boolean", e, this as IXmlLineInfo);
            }
        }

        public override DateTime ReadContentAsDateTime()
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAsDateTime));
            }

            object typedValue = InternalReadContentAsObject();
            XmlSchemaType? xmlType = NodeType == XmlNodeType.Attribute ? AttributeXmlType : ElementXmlType;
            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToDateTime(typedValue);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToDateTime(typedValue);
                }
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "DateTime", e, this as IXmlLineInfo);
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "DateTime", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "DateTime", e, this as IXmlLineInfo);
            }
        }

        public override double ReadContentAsDouble()
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAsDouble));
            }

            object? typedValue = InternalReadContentAsObject();
            XmlSchemaType? xmlType = NodeType == XmlNodeType.Attribute ? AttributeXmlType : ElementXmlType;
            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToDouble(typedValue);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToDouble(typedValue);
                }
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Double", e, this as IXmlLineInfo);
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Double", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Double", e, this as IXmlLineInfo);
            }
        }

        public override float ReadContentAsFloat()
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAsFloat));
            }

            object typedValue = InternalReadContentAsObject();
            XmlSchemaType? xmlType = NodeType == XmlNodeType.Attribute ? AttributeXmlType : ElementXmlType;
            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToSingle(typedValue);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToSingle(typedValue);
                }
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Float", e, this as IXmlLineInfo);
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Float", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Float", e, this as IXmlLineInfo);
            }
        }

        public override decimal ReadContentAsDecimal()
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAsDecimal));
            }

            object typedValue = InternalReadContentAsObject();
            XmlSchemaType? xmlType = NodeType == XmlNodeType.Attribute ? AttributeXmlType : ElementXmlType;
            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToDecimal(typedValue);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToDecimal(typedValue);
                }
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Decimal", e, this as IXmlLineInfo);
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Decimal", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Decimal", e, this as IXmlLineInfo);
            }
        }

        public override int ReadContentAsInt()
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAsInt));
            }

            object typedValue = InternalReadContentAsObject();
            XmlSchemaType? xmlType = NodeType == XmlNodeType.Attribute ? AttributeXmlType : ElementXmlType;
            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToInt32(typedValue);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToInt32(typedValue);
                }
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int", e, this as IXmlLineInfo);
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int", e, this as IXmlLineInfo);
            }
        }

        public override long ReadContentAsLong()
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAsLong));
            }

            object typedValue = InternalReadContentAsObject();
            XmlSchemaType? xmlType = NodeType == XmlNodeType.Attribute ? AttributeXmlType : ElementXmlType;

            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToInt64(typedValue);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToInt64(typedValue);
                }
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Long", e, this as IXmlLineInfo);
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Long", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Long", e, this as IXmlLineInfo);
            }
        }

        public override string ReadContentAsString()
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAsString));
            }

            object typedValue = InternalReadContentAsObject();
            XmlSchemaType? xmlType = NodeType == XmlNodeType.Attribute ? AttributeXmlType : ElementXmlType;

            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToString(typedValue);
                }
                else
                {
                    return (typedValue as string)!;
                }
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "String", e, this as IXmlLineInfo);
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "String", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "String", e, this as IXmlLineInfo);
            }
        }

        public override object ReadContentAs(Type returnType, IXmlNamespaceResolver? namespaceResolver)
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAs));
            }

            string originalStringValue;
            object typedValue = InternalReadContentAsObject(false, out originalStringValue);
            XmlSchemaType? xmlType = NodeType == XmlNodeType.Attribute ? AttributeXmlType : ElementXmlType;
            try
            {
                if (xmlType != null)
                {
                    // special-case convertions to DateTimeOffset; typedValue is by default a DateTime
                    // which cannot preserve time zone, so we need to convert from the original string
                    if (returnType == typeof(DateTimeOffset) && xmlType.Datatype is Datatype_dateTimeBase)
                    {
                        typedValue = originalStringValue!;
                    }

                    return xmlType.ValueConverter.ChangeType(typedValue, returnType);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ChangeType(typedValue, returnType, namespaceResolver);
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
            }
        }

        public override object ReadElementContentAsObject()
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAsObject));
            }

            XmlSchemaType? xmlType;
            return InternalReadElementContentAsObject(out xmlType, true)!;
        }

        public override bool ReadElementContentAsBoolean()
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAsBoolean));
            }

            XmlSchemaType? xmlType;
            object? typedValue = InternalReadElementContentAsObject(out xmlType);

            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToBoolean(typedValue!);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToBoolean(typedValue!);
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Boolean", e, this as IXmlLineInfo);
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Boolean", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Boolean", e, this as IXmlLineInfo);
            }
        }

        public override DateTime ReadElementContentAsDateTime()
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAsDateTime));
            }

            XmlSchemaType? xmlType;
            object? typedValue = InternalReadElementContentAsObject(out xmlType);

            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToDateTime(typedValue!);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToDateTime(typedValue!);
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "DateTime", e, this as IXmlLineInfo);
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "DateTime", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "DateTime", e, this as IXmlLineInfo);
            }
        }

        public override double ReadElementContentAsDouble()
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAsDouble));
            }

            XmlSchemaType? xmlType;
            object? typedValue = InternalReadElementContentAsObject(out xmlType);

            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToDouble(typedValue!);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToDouble(typedValue!);
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Double", e, this as IXmlLineInfo);
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Double", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Double", e, this as IXmlLineInfo);
            }
        }

        public override float ReadElementContentAsFloat()
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAsFloat));
            }

            XmlSchemaType? xmlType;
            object? typedValue = InternalReadElementContentAsObject(out xmlType);

            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToSingle(typedValue!);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToSingle(typedValue!);
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Float", e, this as IXmlLineInfo);
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Float", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Float", e, this as IXmlLineInfo);
            }
        }

        public override decimal ReadElementContentAsDecimal()
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAsDecimal));
            }

            XmlSchemaType? xmlType;
            object? typedValue = InternalReadElementContentAsObject(out xmlType);

            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToDecimal(typedValue!);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToDecimal(typedValue!);
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Decimal", e, this as IXmlLineInfo);
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Decimal", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Decimal", e, this as IXmlLineInfo);
            }
        }

        public override int ReadElementContentAsInt()
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAsInt));
            }

            XmlSchemaType? xmlType;
            object? typedValue = InternalReadElementContentAsObject(out xmlType);

            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToInt32(typedValue!);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToInt32(typedValue!);
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int", e, this as IXmlLineInfo);
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int", e, this as IXmlLineInfo);
            }
        }

        public override long ReadElementContentAsLong()
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAsLong));
            }

            XmlSchemaType? xmlType;
            object? typedValue = InternalReadElementContentAsObject(out xmlType);

            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToInt64(typedValue!);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ToInt64(typedValue!);
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Long", e, this as IXmlLineInfo);
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Long", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Long", e, this as IXmlLineInfo);
            }
        }

        public override string ReadElementContentAsString()
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAsString));
            }

            XmlSchemaType? xmlType;
            object? typedValue = InternalReadElementContentAsObject(out xmlType);

            try
            {
                if (xmlType != null && typedValue != null)
                {
                    return xmlType.ValueConverter.ToString(typedValue);
                }
                else
                {
                    return typedValue as string ?? string.Empty;
                }
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "String", e, this as IXmlLineInfo);
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "String", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "String", e, this as IXmlLineInfo);
            }
        }

        public override object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAs));
            }

            XmlSchemaType? xmlType;
            string? originalStringValue;
            object? typedValue = InternalReadElementContentAsObject(out xmlType, false, out originalStringValue);

            try
            {
                if (xmlType != null)
                {
                    // special-case convertions to DateTimeOffset; typedValue is by default a DateTime
                    // which cannot preserve time zone, so we need to convert from the original string
                    if (returnType == typeof(DateTimeOffset) && xmlType.Datatype is Datatype_dateTimeBase)
                    {
                        typedValue = originalStringValue;
                    }

                    return xmlType.ValueConverter.ChangeType(typedValue!, returnType, namespaceResolver);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ChangeType(typedValue!, returnType, namespaceResolver);
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
            }
        }

        // Attribute Accessors

        // The number of attributes on the current node.
        public override int AttributeCount
        {
            get
            {
                return _attributeCount;
            }
        }

        // Gets the value of the attribute with the specified Name.
        public override string? GetAttribute(string name)
        {
            string? attValue = _coreReader.GetAttribute(name);

            if (attValue == null && _attributeCount > 0)
            {
                // Could be default attribute
                ValidatingReaderNodeData? defaultNode = GetDefaultAttribute(name, false);
                if (defaultNode != null)
                {
                    // Default found
                    attValue = defaultNode.RawValue;
                }
            }

            return attValue;
        }

        // Gets the value of the attribute with the specified LocalName and NamespaceURI.
        public override string? GetAttribute(string name, string? namespaceURI)
        {
            string? attValue = _coreReader.GetAttribute(name, namespaceURI);

            if (attValue == null && _attributeCount > 0)
            {
                // Could be default attribute
                string? atomizedNamespaceURI = (namespaceURI == null) ? string.Empty : _coreReaderNameTable.Get(namespaceURI);
                string? atomizedName = _coreReaderNameTable.Get(name);

                if (atomizedName == null || atomizedNamespaceURI == null)
                {
                    // Attribute not present since we did not see it
                    return null;
                }

                ValidatingReaderNodeData? attNode = GetDefaultAttribute(atomizedName, atomizedNamespaceURI, false);
                if (attNode != null)
                {
                    return attNode.RawValue;
                }
            }

            return attValue;
        }

        // Gets the value of the attribute with the specified index.
        public override string GetAttribute(int i)
        {
            if (i < 0 || i >= _attributeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }

            if (i < _coreReaderAttributeCount)
            {
                return _coreReader.GetAttribute(i);
            }
            else
            {
                int defaultIndex = i - _coreReaderAttributeCount;
                ValidatingReaderNodeData attNode = (ValidatingReaderNodeData)_defaultAttributes[defaultIndex]!;
                Debug.Assert(attNode != null);
                return attNode.RawValue;
            }
        }

        // Moves to the attribute with the specified Name
        public override bool MoveToAttribute(string name)
        {
            if (_coreReader.MoveToAttribute(name))
            {
                _validationState = ValidatingReaderState.OnAttribute;
                _attributePSVI = GetAttributePSVI(name);
                goto Found;
            }
            else if (_attributeCount > 0)
            {
                // Default attribute
                ValidatingReaderNodeData? defaultNode = GetDefaultAttribute(name, true);
                if (defaultNode != null)
                {
                    _validationState = ValidatingReaderState.OnDefaultAttribute;
                    _attributePSVI = defaultNode.AttInfo;
                    _cachedNode = defaultNode;
                    goto Found;
                }
            }

            return false;
        Found:
            if (_validationState == ValidatingReaderState.OnReadBinaryContent)
            {
                Debug.Assert(_readBinaryHelper != null);
                _readBinaryHelper.Finish();
                _validationState = _savedState;
            }

            return true;
        }

        // Moves to the attribute with the specified LocalName and NamespaceURI
        public override bool MoveToAttribute(string name, string? ns)
        {
            // Check atomized local name and ns
            string? atomizedName = _coreReaderNameTable.Get(name);
            ns = ns != null ? _coreReaderNameTable.Get(ns) : string.Empty;

            if (atomizedName == null || ns == null)
            {
                // Name or ns not found in the nameTable, then attribute is not found
                return false;
            }

            if (_coreReader.MoveToAttribute(atomizedName, ns))
            {
                _validationState = ValidatingReaderState.OnAttribute;
                if (_inlineSchemaParser == null)
                {
                    _attributePSVI = GetAttributePSVI(atomizedName, ns);
                    Debug.Assert(_attributePSVI != null);
                }
                else
                {
                    // Parsing inline schema, no PSVI for schema attributes
                    _attributePSVI = null;
                }

                goto Found;
            }
            else
            {
                // Default attribute
                ValidatingReaderNodeData? defaultNode = GetDefaultAttribute(atomizedName, ns, true);
                if (defaultNode != null)
                {
                    _attributePSVI = defaultNode.AttInfo;
                    _cachedNode = defaultNode;
                    _validationState = ValidatingReaderState.OnDefaultAttribute;
                    goto Found;
                }
            }

            return false;

        Found:
            if (_validationState == ValidatingReaderState.OnReadBinaryContent)
            {
                Debug.Assert(_readBinaryHelper != null);
                _readBinaryHelper.Finish();
                _validationState = _savedState;
            }

            return true;
        }

        // Moves to the attribute with the specified index
        public override void MoveToAttribute(int i)
        {
            if (i < 0 || i >= _attributeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }

            _currentAttrIndex = i;
            if (i < _coreReaderAttributeCount)
            {
                // reader attribute
                _coreReader.MoveToAttribute(i);
                if (_inlineSchemaParser == null)
                {
                    _attributePSVI = _attributePSVINodes[i];
                }
                else
                {
                    _attributePSVI = null;
                }

                _validationState = ValidatingReaderState.OnAttribute;
            }
            else
            {
                // default attribute
                int defaultIndex = i - _coreReaderAttributeCount;
                _cachedNode = (ValidatingReaderNodeData)_defaultAttributes[defaultIndex]!;
                _attributePSVI = _cachedNode.AttInfo;
                _validationState = ValidatingReaderState.OnDefaultAttribute;
            }

            if (_validationState == ValidatingReaderState.OnReadBinaryContent)
            {
                Debug.Assert(_readBinaryHelper != null);
                _readBinaryHelper.Finish();
                _validationState = _savedState;
            }
        }

        // Moves to the first attribute.
        public override bool MoveToFirstAttribute()
        {
            if (_coreReader.MoveToFirstAttribute())
            {
                _currentAttrIndex = 0;
                if (_inlineSchemaParser == null)
                {
                    _attributePSVI = _attributePSVINodes[0];
                }
                else
                {
                    _attributePSVI = null;
                }

                _validationState = ValidatingReaderState.OnAttribute;
                goto Found;
            }
            else if (_defaultAttributes.Count > 0)
            {
                // check for default
                _cachedNode = (ValidatingReaderNodeData)_defaultAttributes[0]!;
                _attributePSVI = _cachedNode.AttInfo;
                _currentAttrIndex = 0;
                _validationState = ValidatingReaderState.OnDefaultAttribute;
                goto Found;
            }

            return false;
        Found:
            if (_validationState == ValidatingReaderState.OnReadBinaryContent)
            {
                Debug.Assert(_readBinaryHelper != null);
                _readBinaryHelper.Finish();
                _validationState = _savedState;
            }

            return true;
        }

        // Moves to the next attribute.
        public override bool MoveToNextAttribute()
        {
            if (_currentAttrIndex + 1 < _coreReaderAttributeCount)
            {
                bool moveTo = _coreReader.MoveToNextAttribute();
                Debug.Assert(moveTo);
                _currentAttrIndex++;
                if (_inlineSchemaParser == null)
                {
                    _attributePSVI = _attributePSVINodes[_currentAttrIndex];
                }
                else
                {
                    _attributePSVI = null;
                }

                _validationState = ValidatingReaderState.OnAttribute;
                goto Found;
            }
            else if (_currentAttrIndex + 1 < _attributeCount)
            {
                // default attribute
                int defaultIndex = ++_currentAttrIndex - _coreReaderAttributeCount;
                _cachedNode = (ValidatingReaderNodeData)_defaultAttributes[defaultIndex]!;
                _attributePSVI = _cachedNode.AttInfo;
                _validationState = ValidatingReaderState.OnDefaultAttribute;
                goto Found;
            }

            return false;
        Found:
            if (_validationState == ValidatingReaderState.OnReadBinaryContent)
            {
                Debug.Assert(_readBinaryHelper != null);
                _readBinaryHelper.Finish();
                _validationState = _savedState;
            }

            return true;
        }

        // Moves to the element that contains the current attribute node.
        public override bool MoveToElement()
        {
            if (_coreReader.MoveToElement() || (int)_validationState < 0)
            {
                // states OnDefaultAttribute or OnReadAttributeValue
                _currentAttrIndex = -1;
                _validationState = ValidatingReaderState.ClearAttributes;
                return true;
            }

            return false;
        }

        // Reads the next node from the stream/TextReader.
        public override bool Read()
        {
            switch (_validationState)
            {
                case ValidatingReaderState.Read:
                    if (_coreReader.Read())
                    {
                        ProcessReaderEvent();
                        return true;
                    }
                    else
                    {
                        _validator.EndValidation();
                        if (_coreReader.EOF)
                        {
                            _validationState = ValidatingReaderState.EOF;
                        }

                        return false;
                    }

                case ValidatingReaderState.ParseInlineSchema:
                    ProcessInlineSchema();
                    return true;

                case ValidatingReaderState.OnAttribute:
                case ValidatingReaderState.OnDefaultAttribute:
                case ValidatingReaderState.ClearAttributes:
                case ValidatingReaderState.OnReadAttributeValue:
                    ClearAttributesInfo();
                    if (_inlineSchemaParser != null)
                    {
                        _validationState = ValidatingReaderState.ParseInlineSchema;
                        goto case ValidatingReaderState.ParseInlineSchema;
                    }
                    else
                    {
                        _validationState = ValidatingReaderState.Read;
                        goto case ValidatingReaderState.Read;
                    }

                case ValidatingReaderState.ReadAhead: // Will enter here on calling Skip()
                    ClearAttributesInfo();
                    ProcessReaderEvent();
                    _validationState = ValidatingReaderState.Read;
                    return true;

                case ValidatingReaderState.OnReadBinaryContent:
                    _validationState = _savedState;
                    Debug.Assert(_readBinaryHelper != null);
                    _readBinaryHelper.Finish();
                    return Read();

                case ValidatingReaderState.Init:
                    _validationState = ValidatingReaderState.Read;
                    if (_coreReader.ReadState == ReadState.Interactive)
                    {
                        // If the underlying reader is already positioned on a ndoe, process it
                        ProcessReaderEvent();
                        return true;
                    }
                    else
                    {
                        goto case ValidatingReaderState.Read;
                    }

                case ValidatingReaderState.ReaderClosed:
                case ValidatingReaderState.EOF:
                    return false;

                default:
                    return false;
            }
        }

        // Gets a value indicating whether XmlReader is positioned at the end of the stream/TextReader.
        public override bool EOF
        {
            get
            {
                return _coreReader.EOF;
            }
        }

        // Closes the stream, changes the ReadState to Closed, and sets all the properties back to zero.
        public override void Close()
        {
            _coreReader.Close();
            _validationState = ValidatingReaderState.ReaderClosed;
        }

        // Returns the read state of the XmlReader.
        public override ReadState ReadState
        {
            get
            {
                return (_validationState == ValidatingReaderState.Init) ? ReadState.Initial : _coreReader.ReadState;
            }
        }

        // Skips to the end tag of the current element.
        public override void Skip()
        {
            switch (NodeType)
            {
                case XmlNodeType.Element:
                    if (_coreReader.IsEmptyElement)
                    {
                        break;
                    }

                    bool callSkipToEndElem = true;
                    // If union and unionValue has been parsed till EndElement, then validator.ValidateEndElement has been called
                    // Hence should not call SkipToEndElement as the current context has already been popped in the validator
                    if ((_xmlSchemaInfo.IsUnionType || _xmlSchemaInfo.IsDefault) && _coreReader is XsdCachingReader)
                    {
                        callSkipToEndElem = false;
                    }

                    _coreReader.Skip();
                    _validationState = ValidatingReaderState.ReadAhead;
                    if (callSkipToEndElem)
                    {
                        _validator.SkipToEndElement(_xmlSchemaInfo);
                    }

                    break;

                case XmlNodeType.Attribute:
                    MoveToElement();
                    goto case XmlNodeType.Element;
            }
            // For all other NodeTypes Skip() same as Read()
            Read();
            return;
        }

        // Gets the XmlNameTable associated with this implementation.
        public override XmlNameTable NameTable
        {
            get
            {
                return _coreReaderNameTable;
            }
        }

        // Resolves a namespace prefix in the current element's scope.
        public override string? LookupNamespace(string prefix)
        {
            return _thisNSResolver.LookupNamespace(prefix);
        }

        // Resolves the entity reference for nodes of NodeType EntityReference.
        public override void ResolveEntity()
        {
            throw new InvalidOperationException();
        }

        // Parses the attribute value into one or more Text and/or EntityReference node types.
        public override bool ReadAttributeValue()
        {
            if (_validationState == ValidatingReaderState.OnReadBinaryContent)
            {
                Debug.Assert(_readBinaryHelper != null);
                _readBinaryHelper.Finish();
                _validationState = _savedState;
            }

            if (NodeType == XmlNodeType.Attribute)
            {
                if (_validationState == ValidatingReaderState.OnDefaultAttribute)
                {
                    Debug.Assert(_cachedNode != null);
                    _cachedNode = CreateDummyTextNode(_cachedNode.RawValue, _cachedNode.Depth + 1);
                    _validationState = ValidatingReaderState.OnReadAttributeValue;
                    return true;
                }

                return _coreReader.ReadAttributeValue();
            }

            return false;
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

            // init ReadContentAsBinaryHelper when called first time
            if (_validationState != ValidatingReaderState.OnReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                _savedState = _validationState;
            }

            // restore original state in order to have a normal Read() behavior when called from readBinaryHelper
            _validationState = _savedState;

            // call to the helper
            Debug.Assert(_readBinaryHelper != null);
            int readCount = _readBinaryHelper.ReadContentAsBase64(buffer, index, count);

            // set OnReadBinaryContent state again and return
            _savedState = _validationState;
            _validationState = ValidatingReaderState.OnReadBinaryContent;
            return readCount;
        }

        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (_validationState != ValidatingReaderState.OnReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                _savedState = _validationState;
            }

            // restore original state in order to have a normal Read() behavior when called from readBinaryHelper
            _validationState = _savedState;

            // call to the helper
            Debug.Assert(_readBinaryHelper != null);
            int readCount = _readBinaryHelper.ReadContentAsBinHex(buffer, index, count);

            // set OnReadBinaryContent state again and return
            _savedState = _validationState;
            _validationState = ValidatingReaderState.OnReadBinaryContent;
            return readCount;
        }

        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (_validationState != ValidatingReaderState.OnReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                _savedState = _validationState;
            }

            // restore original state in order to have a normal Read() behavior when called from readBinaryHelper
            _validationState = _savedState;

            // call to the helper
            Debug.Assert(_readBinaryHelper != null);
            int readCount = _readBinaryHelper.ReadElementContentAsBase64(buffer, index, count);

            // set OnReadBinaryContent state again and return
            _savedState = _validationState;
            _validationState = ValidatingReaderState.OnReadBinaryContent;
            return readCount;
        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (_validationState != ValidatingReaderState.OnReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                _savedState = _validationState;
            }

            // restore original state in order to have a normal Read() behavior when called from readBinaryHelper
            _validationState = _savedState;

            // call to the helper
            Debug.Assert(_readBinaryHelper != null);
            int readCount = _readBinaryHelper.ReadElementContentAsBinHex(buffer, index, count);

            // set OnReadBinaryContent state again and return
            _savedState = _validationState;
            _validationState = ValidatingReaderState.OnReadBinaryContent;
            return readCount;
        }

        //
        // IXmlSchemaInfo interface
        //
        bool IXmlSchemaInfo.IsDefault
        {
            get
            {
                switch (NodeType)
                {
                    case XmlNodeType.Element:
                        if (!_coreReader.IsEmptyElement)
                        {
                            GetIsDefault();
                        }

                        return _xmlSchemaInfo.IsDefault;

                    case XmlNodeType.EndElement:
                        return _xmlSchemaInfo.IsDefault;

                    case XmlNodeType.Attribute:
                        if (_attributePSVI != null)
                        {
                            return AttributeSchemaInfo.IsDefault;
                        }

                        break;

                    default:
                        break;
                }

                return false;
            }
        }

        bool IXmlSchemaInfo.IsNil
        {
            get
            {
                switch (NodeType)
                {
                    case XmlNodeType.Element:
                    case XmlNodeType.EndElement:
                        return _xmlSchemaInfo.IsNil;

                    default:
                        break;
                }

                return false;
            }
        }

        XmlSchemaValidity IXmlSchemaInfo.Validity
        {
            get
            {
                switch (NodeType)
                {
                    case XmlNodeType.Element:
                        if (_coreReader.IsEmptyElement)
                        {
                            return _xmlSchemaInfo.Validity;
                        }

                        if (_xmlSchemaInfo.Validity == XmlSchemaValidity.Valid)
                        {
                            // It might be valid for unions since we read ahead, but report notknown for consistency
                            return XmlSchemaValidity.NotKnown;
                        }

                        return _xmlSchemaInfo.Validity;

                    case XmlNodeType.EndElement:
                        return _xmlSchemaInfo.Validity;

                    case XmlNodeType.Attribute:
                        if (_attributePSVI != null)
                        {
                            return AttributeSchemaInfo.Validity;
                        }

                        break;
                }

                return XmlSchemaValidity.NotKnown;
            }
        }

        XmlSchemaSimpleType? IXmlSchemaInfo.MemberType
        {
            get
            {
                switch (NodeType)
                {
                    case XmlNodeType.Element:
                        if (!_coreReader.IsEmptyElement)
                        {
                            GetMemberType();
                        }

                        return _xmlSchemaInfo.MemberType;

                    case XmlNodeType.EndElement:
                        return _xmlSchemaInfo.MemberType;

                    case XmlNodeType.Attribute:
                        if (_attributePSVI != null)
                        {
                            return AttributeSchemaInfo.MemberType;
                        }

                        return null;

                    default:
                        return null; // Text, PI, Comment etc
                }
            }
        }

        XmlSchemaType? IXmlSchemaInfo.SchemaType
        {
            get
            {
                switch (NodeType)
                {
                    case XmlNodeType.Element:
                    case XmlNodeType.EndElement:
                        return _xmlSchemaInfo.SchemaType;

                    case XmlNodeType.Attribute:
                        if (_attributePSVI != null)
                        {
                            return AttributeSchemaInfo.SchemaType;
                        }

                        return null;

                    default:
                        return null; // Text, PI, Comment etc
                }
            }
        }

        XmlSchemaElement? IXmlSchemaInfo.SchemaElement
        {
            get
            {
                if (NodeType == XmlNodeType.Element || NodeType == XmlNodeType.EndElement)
                {
                    return _xmlSchemaInfo.SchemaElement;
                }

                return null;
            }
        }

        XmlSchemaAttribute? IXmlSchemaInfo.SchemaAttribute
        {
            get
            {
                if (NodeType == XmlNodeType.Attribute)
                {
                    if (_attributePSVI != null)
                    {
                        return AttributeSchemaInfo.SchemaAttribute;
                    }
                }

                return null;
            }
        }

        //
        // IXmlLineInfo members
        //

        public bool HasLineInfo()
        {
            return true;
        }

        public int LineNumber
        {
            get
            {
                if (_lineInfo != null)
                {
                    return _lineInfo.LineNumber;
                }

                return 0;
            }
        }

        public int LinePosition
        {
            get
            {
                if (_lineInfo != null)
                {
                    return _lineInfo.LinePosition;
                }

                return 0;
            }
        }

        //
        // IXmlNamespaceResolver members
        //
        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            if (_coreReaderNSResolver != null)
            {
                return _coreReaderNSResolver.GetNamespacesInScope(scope);
            }
            else
            {
                Debug.Assert(_nsManager != null);
                return _nsManager.GetNamespacesInScope(scope);
            }
        }

        string? IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            if (_coreReaderNSResolver != null)
            {
                return _coreReaderNSResolver.LookupNamespace(prefix);
            }
            else
            {
                Debug.Assert(_nsManager != null);
                return _nsManager.LookupNamespace(prefix);
            }
        }

        string? IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            if (_coreReaderNSResolver != null)
            {
                return _coreReaderNSResolver.LookupPrefix(namespaceName);
            }
            else
            {
                Debug.Assert(_nsManager != null);
                return _nsManager.LookupPrefix(namespaceName);
            }
        }

        // Internal / Private methods

        private object GetStringValue()
        {
            return _coreReader.Value;
        }

        private XmlSchemaType? ElementXmlType
        {
            get
            {
                return _xmlSchemaInfo.XmlType;
            }
        }

        private XmlSchemaType? AttributeXmlType
        {
            get
            {
                if (_attributePSVI != null)
                {
                    return AttributeSchemaInfo.XmlType;
                }

                return null;
            }
        }

        private XmlSchemaInfo AttributeSchemaInfo
        {
            get
            {
                Debug.Assert(_attributePSVI != null);
                return _attributePSVI.attributeSchemaInfo;
            }
        }

        private void ProcessReaderEvent()
        {
            if (_replayCache)
            {
                // if in replay mode, do nothing since nodes have been validated already
                // If NodeType == XmlNodeType.EndElement && if manageNamespaces, may need to pop namespace scope, since scope is not popped in ReadAheadForMemberType

                return;
            }

            switch (_coreReader.NodeType)
            {
                case XmlNodeType.Element:

                    ProcessElementEvent();
                    break;

                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    _validator.ValidateWhitespace(_valueGetter);
                    break;

                case XmlNodeType.Text:          // text inside a node
                case XmlNodeType.CDATA:         // <![CDATA[...]]>
                    _validator.ValidateText(_valueGetter);
                    break;

                case XmlNodeType.EndElement:

                    ProcessEndElementEvent();
                    break;

                case XmlNodeType.EntityReference:
                    throw new InvalidOperationException();

                case XmlNodeType.DocumentType:
#if TEMP_HACK_FOR_SCHEMA_INFO
                    validator.SetDtdSchemaInfo((SchemaInfo)coreReader.DtdInfo);
#else
                    _validator.SetDtdSchemaInfo(_coreReader.DtdInfo);
#endif
                    break;

                default:
                    break;
            }
        }

        private void ProcessElementEvent()
        {
            if (_processInlineSchema && IsXSDRoot(_coreReader.LocalName, _coreReader.NamespaceURI) && _coreReader.Depth > 0)
            {
                _xmlSchemaInfo.Clear();
                _attributeCount = _coreReaderAttributeCount = _coreReader.AttributeCount;
                if (!_coreReader.IsEmptyElement)
                {
                    // If its not empty schema, then parse else ignore
                    _inlineSchemaParser = new Parser(SchemaType.XSD, _coreReaderNameTable, _validator.SchemaSet.GetSchemaNames(_coreReaderNameTable), _validationEvent);
                    _inlineSchemaParser.StartParsing(_coreReader, null);
                    _inlineSchemaParser.ParseReaderNode();
                    _validationState = ValidatingReaderState.ParseInlineSchema;
                }
                else
                {
                    _validationState = ValidatingReaderState.ClearAttributes;
                }
            }
            else
            {
                // Validate element
                // Clear previous data
                _atomicValue = null;
                _originalAtomicValueString = null;
                _xmlSchemaInfo.Clear();

                if (_manageNamespaces)
                {
                    Debug.Assert(_nsManager != null);
                    _nsManager.PushScope();
                }

                // Find Xsi attributes that need to be processed before validating the element
                string? xsiSchemaLocation = null;
                string? xsiNoNamespaceSL = null;
                string? xsiNil = null;
                string? xsiType = null;

                if (_coreReader.MoveToFirstAttribute())
                {
                    do
                    {
                        string objectNs = _coreReader.NamespaceURI;
                        string objectName = _coreReader.LocalName;

                        if (Ref.Equal(objectNs, _nsXsi))
                        {
                            if (Ref.Equal(objectName, _xsiSchemaLocation))
                            {
                                xsiSchemaLocation = _coreReader.Value;
                            }
                            else if (Ref.Equal(objectName, _xsiNoNamespaceSchemaLocation))
                            {
                                xsiNoNamespaceSL = _coreReader.Value;
                            }
                            else if (Ref.Equal(objectName, _xsiType))
                            {
                                xsiType = _coreReader.Value;
                            }
                            else if (Ref.Equal(objectName, _xsiNil))
                            {
                                xsiNil = _coreReader.Value;
                            }
                        }

                        if (_manageNamespaces && Ref.Equal(_coreReader.NamespaceURI, _nsXmlNs))
                        {
                            Debug.Assert(_nsManager != null);
                            _nsManager.AddNamespace(_coreReader.Prefix.Length == 0 ? string.Empty : _coreReader.LocalName, _coreReader.Value);
                        }
                    } while (_coreReader.MoveToNextAttribute());
                    _coreReader.MoveToElement();
                }

                _validator.ValidateElement(_coreReader.LocalName, _coreReader.NamespaceURI, _xmlSchemaInfo, xsiType, xsiNil, xsiSchemaLocation, xsiNoNamespaceSL);
                ValidateAttributes();
                _validator.ValidateEndOfAttributes(_xmlSchemaInfo);
                if (_coreReader.IsEmptyElement)
                {
                    ProcessEndElementEvent();
                }

                _validationState = ValidatingReaderState.ClearAttributes;
            }
        }

        private void ProcessEndElementEvent()
        {
            _atomicValue = _validator.ValidateEndElement(_xmlSchemaInfo);
            _originalAtomicValueString = GetOriginalAtomicValueStringOfElement();
            if (_xmlSchemaInfo.IsDefault)
            {
                // The atomicValue returned is a default value
                Debug.Assert(_atomicValue != null);
                int depth = _coreReader.Depth;
                _coreReader = GetCachingReader();
                Debug.Assert(_cachingReader != null);
                _cachingReader.RecordTextNode(_xmlSchemaInfo.XmlType!.ValueConverter.ToString(_atomicValue), _originalAtomicValueString, depth + 1, 0, 0);
                _cachingReader.RecordEndElementNode();
                _cachingReader.SetToReplayMode();
                _replayCache = true;
            }
            else if (_manageNamespaces)
            {
                Debug.Assert(_nsManager != null);
                _nsManager.PopScope();
            }
        }

        private void ValidateAttributes()
        {
            _attributeCount = _coreReaderAttributeCount = _coreReader.AttributeCount;
            AttributePSVIInfo attributePSVI;
            int attIndex = 0;
            bool attributeInvalid = false;
            if (_coreReader.MoveToFirstAttribute())
            {
                do
                {
                    string localName = _coreReader.LocalName;
                    string ns = _coreReader.NamespaceURI;

                    attributePSVI = AddAttributePSVI(attIndex);
                    attributePSVI.localName = localName;
                    attributePSVI.namespaceUri = ns;

                    if ((object)ns == (object)_nsXmlNs)
                    {
                        attIndex++;
                        continue;
                    }

                    attributePSVI.typedAttributeValue = _validator.ValidateAttribute(localName, ns, _valueGetter, attributePSVI.attributeSchemaInfo);
                    if (!attributeInvalid)
                    {
                        attributeInvalid = attributePSVI.attributeSchemaInfo.Validity == XmlSchemaValidity.Invalid;
                    }

                    attIndex++;
                } while (_coreReader.MoveToNextAttribute());
            }

            _coreReader.MoveToElement();
            if (attributeInvalid)
            {
                // If any of the attributes are invalid, Need to report element's validity as invalid
                _xmlSchemaInfo.Validity = XmlSchemaValidity.Invalid;
            }

            _validator.GetUnspecifiedDefaultAttributes(_defaultAttributes, true);
            _attributeCount += _defaultAttributes.Count;
        }

        private void ClearAttributesInfo()
        {
            _attributeCount = 0;
            _coreReaderAttributeCount = 0;
            _currentAttrIndex = -1;
            _defaultAttributes.Clear();
            _attributePSVI = null;
        }

        private AttributePSVIInfo? GetAttributePSVI(string name)
        {
            if (_inlineSchemaParser != null)
            {
                // Parsing inline schema, no PSVI for schema attributes
                return null;
            }

            string attrLocalName;
            string attrPrefix;
            string ns;
            ValidateNames.SplitQName(name, out attrPrefix, out attrLocalName);
            attrPrefix = _coreReaderNameTable.Add(attrPrefix);
            attrLocalName = _coreReaderNameTable.Add(attrLocalName);

            if (attrPrefix.Length == 0)
            {
                // empty prefix, not qualified
                ns = string.Empty;
            }
            else
            {
                ns = _thisNSResolver.LookupNamespace(attrPrefix)!;
            }

            return GetAttributePSVI(attrLocalName, ns);
        }

        private AttributePSVIInfo? GetAttributePSVI(string localName, string ns)
        {
            Debug.Assert(_coreReaderNameTable.Get(localName) != null);
            Debug.Assert(_coreReaderNameTable.Get(ns) != null);
            AttributePSVIInfo? attInfo;

            for (int i = 0; i < _coreReaderAttributeCount; i++)
            {
                attInfo = _attributePSVINodes[i];
                if (attInfo != null)
                {
                    // Will be null for invalid attributes
                    if (Ref.Equal(localName, attInfo.localName) && Ref.Equal(ns, attInfo.namespaceUri))
                    {
                        _currentAttrIndex = i;
                        return attInfo;
                    }
                }
            }

            return null;
        }

        private ValidatingReaderNodeData? GetDefaultAttribute(string name, bool updatePosition)
        {
            string attrLocalName;
            string attrPrefix;
            ValidateNames.SplitQName(name, out attrPrefix, out attrLocalName);

            // Atomize
            attrPrefix = _coreReaderNameTable.Add(attrPrefix);
            attrLocalName = _coreReaderNameTable.Add(attrLocalName);
            string ns;
            if (attrPrefix.Length == 0)
            {
                ns = string.Empty;
            }
            else
            {
                ns = _thisNSResolver.LookupNamespace(attrPrefix)!;
            }

            return GetDefaultAttribute(attrLocalName, ns, updatePosition);
        }

        private ValidatingReaderNodeData? GetDefaultAttribute(string attrLocalName, string ns, bool updatePosition)
        {
            Debug.Assert(_coreReaderNameTable.Get(attrLocalName) != null);
            Debug.Assert(_coreReaderNameTable.Get(ns) != null);
            ValidatingReaderNodeData? defaultNode;

            for (int i = 0; i < _defaultAttributes.Count; i++)
            {
                defaultNode = (ValidatingReaderNodeData)_defaultAttributes[i]!;
                if (Ref.Equal(defaultNode.LocalName, attrLocalName) && Ref.Equal(defaultNode.Namespace, ns))
                {
                    if (updatePosition)
                    {
                        _currentAttrIndex = _coreReader.AttributeCount + i;
                    }

                    return defaultNode;
                }
            }

            return null;
        }

        private AttributePSVIInfo AddAttributePSVI(int attIndex)
        {
            Debug.Assert(attIndex < _attributePSVINodes.Length);
            AttributePSVIInfo attInfo = _attributePSVINodes[attIndex];
            if (attInfo != null)
            {
                attInfo.Reset();
                return attInfo;
            }

            if (attIndex >= _attributePSVINodes.Length - 1)
            {
                // reached capacity of PSVIInfo array, Need to increase capacity to twice the initial
                AttributePSVIInfo[] newPSVINodes = new AttributePSVIInfo[_attributePSVINodes.Length * 2];
                Array.Copy(_attributePSVINodes, newPSVINodes, _attributePSVINodes.Length);
                _attributePSVINodes = newPSVINodes;
            }

            attInfo = _attributePSVINodes[attIndex];
            if (attInfo == null)
            {
                attInfo = new AttributePSVIInfo();
                _attributePSVINodes[attIndex] = attInfo;
            }

            return attInfo;
        }

        private bool IsXSDRoot(string localName, string ns)
        {
            return Ref.Equal(ns, _nsXs) && Ref.Equal(localName, _xsdSchema);
        }

        private void ProcessInlineSchema()
        {
            Debug.Assert(_inlineSchemaParser != null);
            if (_coreReader.Read())
            {
                if (_coreReader.NodeType == XmlNodeType.Element)
                {
                    _attributeCount = _coreReaderAttributeCount = _coreReader.AttributeCount;
                }
                else
                {
                    // Clear attributes info if nodeType is not element
                    ClearAttributesInfo();
                }

                if (!_inlineSchemaParser.ParseReaderNode())
                {
                    _inlineSchemaParser.FinishParsing();
                    XmlSchema schema = _inlineSchemaParser.XmlSchema!;
                    _validator.AddSchema(schema);
                    _inlineSchemaParser = null;
                    _validationState = ValidatingReaderState.Read;
                }
            }
        }

        private object InternalReadContentAsObject()
        {
            return InternalReadContentAsObject(false);
        }

        private object InternalReadContentAsObject(bool unwrapTypedValue)
        {
            return InternalReadContentAsObject(unwrapTypedValue, out _);
        }

        private object InternalReadContentAsObject(bool unwrapTypedValue, out string originalStringValue)
        {
            XmlNodeType nodeType = this.NodeType;
            if (nodeType == XmlNodeType.Attribute)
            {
                originalStringValue = this.Value;
                if (_attributePSVI != null && _attributePSVI.typedAttributeValue != null)
                {
                    if (_validationState == ValidatingReaderState.OnDefaultAttribute)
                    {
                        XmlSchemaAttribute schemaAttr = _attributePSVI.attributeSchemaInfo.SchemaAttribute!;
                        originalStringValue = (schemaAttr.DefaultValue != null) ? schemaAttr.DefaultValue : schemaAttr.FixedValue!;
                    }

                    return ReturnBoxedValue(_attributePSVI.typedAttributeValue, AttributeSchemaInfo.XmlType!, unwrapTypedValue);
                }
                else
                {
                    // return string value
                    return this.Value;
                }
            }
            else if (nodeType == XmlNodeType.EndElement)
            {
                if (_atomicValue != null)
                {
                    originalStringValue = _originalAtomicValueString!;

                    return _atomicValue;
                }
                else
                {
                    originalStringValue = string.Empty;

                    return string.Empty;
                }
            }
            else
            {
                // Positioned on text, CDATA, PI, Comment etc
                if (_validator.CurrentContentType == XmlSchemaContentType.TextOnly)
                {
                    // if current element is of simple type
                    object? value = ReturnBoxedValue(ReadTillEndElement(), _xmlSchemaInfo.XmlType!, unwrapTypedValue);
                    originalStringValue = _originalAtomicValueString!;

                    return value;
                }
                else
                {
                    if (_coreReader is XsdCachingReader cachingReader)
                    {
                        originalStringValue = cachingReader.ReadOriginalContentAsString();
                    }
                    else
                    {
                        originalStringValue = InternalReadContentAsString();
                    }

                    return originalStringValue;
                }
            }
        }

        private object? InternalReadElementContentAsObject(out XmlSchemaType? xmlType)
        {
            return InternalReadElementContentAsObject(out xmlType, false);
        }

        private object? InternalReadElementContentAsObject(out XmlSchemaType? xmlType, bool unwrapTypedValue)
        {
            return InternalReadElementContentAsObject(out xmlType, unwrapTypedValue, out _);
        }

        private object? InternalReadElementContentAsObject(out XmlSchemaType? xmlType, bool unwrapTypedValue, out string? originalString)
        {
            Debug.Assert(this.NodeType == XmlNodeType.Element);
            object? typedValue;
            xmlType = null;

            // If its an empty element, can have default/fixed value
            if (this.IsEmptyElement)

            {
                if (_xmlSchemaInfo.ContentType == XmlSchemaContentType.TextOnly)
                {
                    typedValue = ReturnBoxedValue(_atomicValue, _xmlSchemaInfo.XmlType!, unwrapTypedValue);
                }
                else
                {
                    typedValue = _atomicValue;
                }

                originalString = _originalAtomicValueString;
                xmlType = ElementXmlType; // Set this for default values
                this.Read();

                return typedValue;
            }

            // move to content and read typed value
            this.Read();

            if (this.NodeType == XmlNodeType.EndElement)
            {
                // If IsDefault is true, the next node will be EndElement
                if (_xmlSchemaInfo.IsDefault)
                {
                    if (_xmlSchemaInfo.ContentType == XmlSchemaContentType.TextOnly)
                    {
                        typedValue = ReturnBoxedValue(_atomicValue, _xmlSchemaInfo.XmlType!, unwrapTypedValue);
                    }
                    else
                    {
                        // anyType has default value
                        typedValue = _atomicValue;
                    }

                    originalString = _originalAtomicValueString;
                }
                else
                {
                    // Empty content
                    typedValue = string.Empty;
                    originalString = string.Empty;
                }
            }
            else if (this.NodeType == XmlNodeType.Element)
            {
                // the first child is again element node
                throw new XmlException(SR.Xml_MixedReadElementContentAs, string.Empty, this as IXmlLineInfo);
            }
            else
            {
                typedValue = InternalReadContentAsObject(unwrapTypedValue, out originalString);

                // ReadElementContentAsXXX cannot be called on mixed content, if positioned on node other than EndElement, Error
                if (this.NodeType != XmlNodeType.EndElement)
                {
                    throw new XmlException(SR.Xml_MixedReadElementContentAs, string.Empty, this as IXmlLineInfo);
                }
            }

            xmlType = ElementXmlType; // Set this as we are moving ahead to the next node

            // move to next node
            this.Read();

            return typedValue;
        }

        private object? ReadTillEndElement()
        {
            if (_atomicValue == null)
            {
                while (_coreReader.Read())
                {
                    if (_replayCache)
                    {
                        // If replaying nodes in the cache, they have already been validated
                        continue;
                    }

                    switch (_coreReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            ProcessReaderEvent();
                            goto breakWhile;

                        case XmlNodeType.Text:
                        case XmlNodeType.CDATA:
                            _validator.ValidateText(_valueGetter);
                            break;

                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                            _validator.ValidateWhitespace(_valueGetter);
                            break;

                        case XmlNodeType.Comment:
                        case XmlNodeType.ProcessingInstruction:
                            break;

                        case XmlNodeType.EndElement:
                            _atomicValue = _validator.ValidateEndElement(_xmlSchemaInfo);
                            _originalAtomicValueString = GetOriginalAtomicValueStringOfElement();
                            if (_manageNamespaces)
                            {
                                Debug.Assert(_nsManager != null);
                                _nsManager.PopScope();
                            }

                            goto breakWhile;
                    }

                    continue;
                breakWhile:
                    break;
                }
            }
            else
            {
                // atomicValue != null, meaning already read ahead - Switch reader
                if (_atomicValue == this)
                {
                    // switch back invalid marker; dont need it since coreReader moved to endElement
                    _atomicValue = null;
                }

                SwitchReader();
            }

            return _atomicValue;
        }

        private void SwitchReader()
        {
            if (_coreReader is XsdCachingReader cachingReader)
            {
                // Switch back without going over the cached contents again.
                _coreReader = cachingReader.GetCoreReader();
            }

            Debug.Assert(_coreReader.NodeType == XmlNodeType.EndElement);
            _replayCache = false;
        }

        private void ReadAheadForMemberType()
        {
            while (_coreReader.Read())
            {
                switch (_coreReader.NodeType)
                {
                    case XmlNodeType.Element:
                        Debug.Fail("Should not happen as the caching reader does not cache elements in simple content");
                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        _validator.ValidateText(_valueGetter);
                        break;

                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        _validator.ValidateWhitespace(_valueGetter);
                        break;

                    case XmlNodeType.Comment:
                    case XmlNodeType.ProcessingInstruction:
                        break;

                    case XmlNodeType.EndElement:
                        _atomicValue = _validator.ValidateEndElement(_xmlSchemaInfo); //?? pop namespaceManager scope
                        _originalAtomicValueString = GetOriginalAtomicValueStringOfElement();
                        if (_atomicValue == null)
                        {
                            // Invalid marker
                            _atomicValue = this;
                        }
                        else if (_xmlSchemaInfo.IsDefault)
                        {
                            // The atomicValue returned is a default value
                            Debug.Assert(_cachingReader != null);
                            _cachingReader.SwitchTextNodeAndEndElement(_xmlSchemaInfo.XmlType!.ValueConverter.ToString(_atomicValue), _originalAtomicValueString);
                        }

                        goto breakWhile;
                }

                continue;
            breakWhile:
                break;
            }
        }

        private void GetIsDefault()
        {
            XsdCachingReader? cachedReader = _coreReader as XsdCachingReader;
            if (cachedReader == null && _xmlSchemaInfo.HasDefaultValue)
            {
                // Get Isdefault
                _coreReader = GetCachingReader();
                Debug.Assert(_cachingReader != null);
                if (_xmlSchemaInfo.IsUnionType && !_xmlSchemaInfo.IsNil)
                {
                    // If it also union, get the memberType as well
                    ReadAheadForMemberType();
                }
                else
                {
                    if (_coreReader.Read())
                    {
                        switch (_coreReader.NodeType)
                        {
                            case XmlNodeType.Element:
                                Debug.Fail("Should not happen as the caching reader does not cache elements in simple content");
                                break;

                            case XmlNodeType.Text:
                            case XmlNodeType.CDATA:
                                _validator.ValidateText(_valueGetter);
                                break;

                            case XmlNodeType.Whitespace:
                            case XmlNodeType.SignificantWhitespace:
                                _validator.ValidateWhitespace(_valueGetter);
                                break;

                            case XmlNodeType.Comment:
                            case XmlNodeType.ProcessingInstruction:
                                break;

                            case XmlNodeType.EndElement:
                                _atomicValue = _validator.ValidateEndElement(_xmlSchemaInfo); //?? pop namespaceManager scope
                                _originalAtomicValueString = GetOriginalAtomicValueStringOfElement();
                                if (_xmlSchemaInfo.IsDefault)
                                {
                                    // The atomicValue returned is a default value
                                    _cachingReader.SwitchTextNodeAndEndElement(_xmlSchemaInfo.XmlType!.ValueConverter.ToString(_atomicValue!), _originalAtomicValueString);
                                }

                                break;

                            default:
                                break;
                        }
                    }
                }

                _cachingReader.SetToReplayMode();
                _replayCache = true;
            }
        }

        private void GetMemberType()
        {
            if (_xmlSchemaInfo.MemberType != null || _atomicValue == this)
            {
                return;
            }

            XsdCachingReader? cachedReader = _coreReader as XsdCachingReader;
            if (cachedReader == null && _xmlSchemaInfo.IsUnionType && !_xmlSchemaInfo.IsNil)
            {
                _coreReader = GetCachingReader();
                Debug.Assert(_cachingReader != null);
                ReadAheadForMemberType();
                _cachingReader.SetToReplayMode();
                _replayCache = true;
            }
        }

        private object ReturnBoxedValue(object? typedValue, XmlSchemaType xmlType, bool unWrap)
        {
            if (typedValue != null)
            {
                if (unWrap)
                {
                    // convert XmlAtomicValue[] to object[] for list of unions; The other cases return typed value of the valueType anyway
                    Debug.Assert(xmlType != null && xmlType.Datatype != null);
                    if (xmlType.Datatype.Variety == XmlSchemaDatatypeVariety.List)
                    {
                        Datatype_List? listType = xmlType.Datatype as Datatype_List;
                        Debug.Assert(listType != null);
                        if (listType.ItemType.Variety == XmlSchemaDatatypeVariety.Union)
                        {
                            typedValue = xmlType.ValueConverter.ChangeType(typedValue, xmlType.Datatype.ValueType, _thisNSResolver);
                        }
                    }
                }

                return typedValue;
            }
            else
            {
                // return the original string value of the element or attribute
                Debug.Assert(NodeType != XmlNodeType.Attribute);
                typedValue = _validator.GetConcatenatedValue();
            }

            return typedValue;
        }

        private XsdCachingReader GetCachingReader()
        {
            if (_cachingReader == null)
            {
                _cachingReader = new XsdCachingReader(_coreReader, _lineInfo, new CachingEventHandler(CachingCallBack));
            }
            else
            {
                _cachingReader.Reset(_coreReader);
            }

            _lineInfo = _cachingReader as IXmlLineInfo;
            return _cachingReader;
        }

        internal ValidatingReaderNodeData CreateDummyTextNode(string attributeValue, int depth)
        {
            if (_textNode == null)
            {
                _textNode = new ValidatingReaderNodeData(XmlNodeType.Text);
            }

            _textNode.Depth = depth;
            _textNode.RawValue = attributeValue;
            return _textNode;
        }

        internal void CachingCallBack(XsdCachingReader cachingReader)
        {
            _coreReader = cachingReader.GetCoreReader(); // re-switch the core-reader after caching reader is done
            _lineInfo = cachingReader.GetLineInfo();
            _replayCache = false;
        }

        private string? GetOriginalAtomicValueStringOfElement()
        {
            if (_xmlSchemaInfo.IsDefault)
            {
                XmlSchemaElement? schemaElem = _xmlSchemaInfo.SchemaElement;
                if (schemaElem != null)
                {
                    return (schemaElem.DefaultValue != null) ? schemaElem.DefaultValue : schemaElem.FixedValue;
                }
            }
            else
            {
                return _validator.GetConcatenatedValue();
            }

            return string.Empty;
        }
    }
}
