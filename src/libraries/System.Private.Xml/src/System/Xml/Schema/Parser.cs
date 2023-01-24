// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Schema
{
    internal sealed partial class Parser
    {
        private SchemaType _schemaType;
        private readonly XmlNameTable _nameTable;
        private readonly SchemaNames _schemaNames;
        private readonly ValidationEventHandler? _eventHandler;
        private XmlNamespaceManager? _namespaceManager;
        private XmlReader? _reader;
        private PositionInfo? _positionInfo;
        private bool _isProcessNamespaces;
        private int _schemaXmlDepth;
        private int _markupDepth;
        private SchemaBuilder? _builder;
        private XmlSchema? _schema;
        private SchemaInfo? _xdrSchema;
        private XmlResolver? _xmlResolver; //to be used only by XDRBuilder

        //xs:Annotation perf fix
        private readonly XmlDocument _dummyDocument;
        private bool _processMarkup;
        private XmlNode? _parentNode;
        private XmlNamespaceManager? _annotationNSManager;
        private string? _xmlns;

        public Parser(SchemaType schemaType, XmlNameTable nameTable, SchemaNames schemaNames, ValidationEventHandler? eventHandler)
        {
            _schemaType = schemaType;
            _nameTable = nameTable;
            _schemaNames = schemaNames;
            _eventHandler = eventHandler;
            _xmlResolver = null;
            _processMarkup = true;
            _dummyDocument = new XmlDocument();
        }

        public SchemaType Parse(XmlReader reader, string? targetNamespace)
        {
            StartParsing(reader, targetNamespace);
            while (ParseReaderNode() && reader.Read()) { }
            return FinishParsing();
        }

        public void StartParsing(XmlReader reader, string? targetNamespace)
        {
            _reader = reader;
            _positionInfo = PositionInfo.GetPositionInfo(reader);
            _namespaceManager = reader.NamespaceManager;
            if (_namespaceManager == null)
            {
                _namespaceManager = new XmlNamespaceManager(_nameTable);
                _isProcessNamespaces = true;
            }
            else
            {
                _isProcessNamespaces = false;
            }
            while (reader.NodeType != XmlNodeType.Element && reader.Read()) { }

            _markupDepth = int.MaxValue;
            _schemaXmlDepth = reader.Depth;
            SchemaType rootType = _schemaNames.SchemaTypeFromRoot(reader.LocalName, reader.NamespaceURI);

            if (!CheckSchemaRoot(rootType, out string? code))
            {
                throw new XmlSchemaException(code, reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition);
            }

            if (_schemaType == SchemaType.XSD)
            {
                _schema = new XmlSchema();
                _schema.BaseUri = new Uri(reader.BaseURI!, UriKind.RelativeOrAbsolute);
                _builder = new XsdBuilder(reader, _namespaceManager, _schema, _nameTable, _schemaNames, _eventHandler);
            }
            else
            {
                Debug.Assert(_schemaType == SchemaType.XDR);
                _xdrSchema = new SchemaInfo();
                _xdrSchema.SchemaType = SchemaType.XDR;
                _builder = new XdrBuilder(reader, _namespaceManager, _xdrSchema, targetNamespace, _nameTable, _schemaNames, _eventHandler);
                ((XdrBuilder)_builder).XmlResolver = _xmlResolver;
            }
        }

        private bool CheckSchemaRoot(SchemaType rootType, [NotNullWhen(false)] out string? code)
        {
            code = null;
            if (_schemaType == SchemaType.None)
            {
                _schemaType = rootType;
            }
            switch (rootType)
            {
                case SchemaType.XSD:
                    if (_schemaType != SchemaType.XSD)
                    {
                        code = SR.Sch_MixSchemaTypes;
                        return false;
                    }
                    break;

                case SchemaType.XDR:
                    if (_schemaType == SchemaType.XSD)
                    {
                        code = SR.Sch_XSDSchemaOnly;
                        return false;
                    }
                    else if (_schemaType != SchemaType.XDR)
                    {
                        code = SR.Sch_MixSchemaTypes;
                        return false;
                    }
                    break;

                case SchemaType.DTD: //Did not detect schema type that can be parsed by this parser
                case SchemaType.None:
                    code = SR.Sch_SchemaRootExpected;
                    if (_schemaType == SchemaType.XSD)
                    {
                        code = SR.Sch_XSDSchemaRootExpected;
                    }
                    return false;

                default:
                    Debug.Fail($"Unexpected root type {rootType}");
                    break;
            }
            return true;
        }

        public SchemaType FinishParsing()
        {
            return _schemaType;
        }

        public XmlSchema? XmlSchema
        {
            get { return _schema; }
        }

        internal XmlResolver? XmlResolver
        {
            set
            {
                _xmlResolver = value;
            }
        }

        public SchemaInfo? XdrSchema
        {
            get { return _xdrSchema; }
        }

        public bool ParseReaderNode()
        {
            if (_reader!.Depth > _markupDepth)
            {
                if (_processMarkup)
                {
                    ProcessAppInfoDocMarkup(false);
                }
                return true;
            }
            else if (_reader.NodeType == XmlNodeType.Element)
            {
                if (_builder!.ProcessElement(_reader.Prefix, _reader.LocalName, _reader.NamespaceURI))
                {
                    _namespaceManager!.PushScope();
                    if (_reader.MoveToFirstAttribute())
                    {
                        do
                        {
                            _builder.ProcessAttribute(_reader.Prefix, _reader.LocalName, _reader.NamespaceURI, _reader.Value);
                            if (Ref.Equal(_reader.NamespaceURI, _schemaNames.NsXmlNs) && _isProcessNamespaces)
                            {
                                _namespaceManager.AddNamespace(_reader.Prefix.Length == 0 ? string.Empty : _reader.LocalName, _reader.Value);
                            }
                        }
                        while (_reader.MoveToNextAttribute());
                        _reader.MoveToElement(); // get back to the element
                    }
                    _builder.StartChildren();
                    if (_reader.IsEmptyElement)
                    {
                        _namespaceManager.PopScope();
                        _builder.EndChildren();
                        if (_reader.Depth == _schemaXmlDepth)
                        {
                            return false; // done
                        }
                    }
                    else if (!_builder.IsContentParsed())
                    { //AppInfo and Documentation
                        _markupDepth = _reader.Depth;
                        _processMarkup = true;
                        if (_annotationNSManager == null)
                        {
                            _annotationNSManager = new XmlNamespaceManager(_nameTable);
                            _xmlns = _nameTable.Add("xmlns");
                        }
                        ProcessAppInfoDocMarkup(true);
                    }
                }
                else if (!_reader.IsEmptyElement)
                { //UnsupportedElement in that context
                    _markupDepth = _reader.Depth;
                    _processMarkup = false; //Hack to not process unsupported elements
                }
            }
            else if (_reader.NodeType == XmlNodeType.Text)
            { //Check for whitespace
                if (!XmlCharType.IsOnlyWhitespace(_reader.Value))
                {
                    _builder!.ProcessCData(_reader.Value);
                }
            }
            else if (_reader.NodeType == XmlNodeType.EntityReference ||
                _reader.NodeType == XmlNodeType.SignificantWhitespace ||
                _reader.NodeType == XmlNodeType.CDATA)
            {
                _builder!.ProcessCData(_reader.Value);
            }
            else if (_reader.NodeType == XmlNodeType.EndElement)
            {
                if (_reader.Depth == _markupDepth)
                {
                    if (_processMarkup)
                    {
                        Debug.Assert(_parentNode != null);
                        XmlNodeList list = _parentNode.ChildNodes;
                        XmlNode?[] markup = new XmlNode[list.Count];
                        for (int i = 0; i < list.Count; i++)
                        {
                            markup[i] = list[i];
                        }

                        _builder!.ProcessMarkup(markup);
                        _namespaceManager!.PopScope();
                        _builder.EndChildren();
                    }

                    _markupDepth = int.MaxValue;
                }
                else
                {
                    _namespaceManager!.PopScope();
                    _builder!.EndChildren();
                }
                if (_reader.Depth == _schemaXmlDepth)
                {
                    return false; // done
                }
            }
            return true;
        }

        private void ProcessAppInfoDocMarkup(bool root)
        {
            //First time reader is positioned on AppInfo or Documentation element
            XmlNode? currentNode = null;

            switch (_reader!.NodeType)
            {
                case XmlNodeType.Element:
                    _annotationNSManager!.PushScope();
                    LoadElementNode(root);
                    //  Dev10 (TFS) #479761: The following code was to address the issue of where an in-scope namespace delaration attribute
                    //      was not added when an element follows an empty element. This fix will result in persisting schema in a consistent form
                    //      although it does not change the semantic meaning of the schema.
                    //      Since it is as a breaking change and Dev10 needs to maintain the backward compatibility, this fix is being reverted.
                    //  if (reader.IsEmptyElement) {
                    //      annotationNSManager.PopScope();
                    //  }
                    break;

                case XmlNodeType.Text:
                    currentNode = _dummyDocument.CreateTextNode(_reader.Value);
                    goto default;

                case XmlNodeType.SignificantWhitespace:
                    currentNode = _dummyDocument.CreateSignificantWhitespace(_reader.Value);
                    goto default;

                case XmlNodeType.CDATA:
                    currentNode = _dummyDocument.CreateCDataSection(_reader.Value);
                    goto default;

                case XmlNodeType.EntityReference:
                    currentNode = _dummyDocument.CreateEntityReference(_reader.Name);
                    goto default;

                case XmlNodeType.Comment:
                    currentNode = _dummyDocument.CreateComment(_reader.Value);
                    goto default;

                case XmlNodeType.ProcessingInstruction:
                    currentNode = _dummyDocument.CreateProcessingInstruction(_reader.Name, _reader.Value);
                    goto default;

                case XmlNodeType.EndEntity:
                    break;

                case XmlNodeType.Whitespace:
                    break;

                case XmlNodeType.EndElement:
                    _annotationNSManager!.PopScope();
                    _parentNode = _parentNode!.ParentNode;
                    break;

                default: //other possible node types: Document/DocType/DocumentFrag/Entity/Notation/Xmldecl cannot appear as children of xs:appInfo or xs:doc
                    Debug.Assert(currentNode != null);
                    Debug.Assert(_parentNode != null);
                    _parentNode.AppendChild(currentNode);
                    break;
            }
        }

        private XmlElement LoadElementNode(bool root)
        {
            Debug.Assert(_reader!.NodeType == XmlNodeType.Element);

            XmlReader r = _reader;
            bool fEmptyElement = r.IsEmptyElement;

            XmlElement element = _dummyDocument.CreateElement(r.Prefix, r.LocalName, r.NamespaceURI);
            element.IsEmpty = fEmptyElement;

            if (root)
            {
                _parentNode = element;
            }
            else
            {
                XmlAttributeCollection attributes = element.Attributes;
                if (r.MoveToFirstAttribute())
                {
                    do
                    {
                        if (Ref.Equal(r.NamespaceURI, _schemaNames.NsXmlNs))
                        { //Namespace Attribute
                            _annotationNSManager!.AddNamespace(r.Prefix.Length == 0 ? string.Empty : _reader.LocalName, _reader.Value);
                        }
                        XmlAttribute attr = LoadAttributeNode();
                        attributes.Append(attr);
                    } while (r.MoveToNextAttribute());
                }
                r.MoveToElement();
                string? ns = _annotationNSManager!.LookupNamespace(r.Prefix);
                if (ns == null)
                {
                    XmlAttribute attr = CreateXmlNsAttribute(r.Prefix, _namespaceManager!.LookupNamespace(r.Prefix)!);
                    attributes.Append(attr);
                }
                else if (ns.Length == 0)
                { //string.Empty prefix is mapped to string.Empty NS by default
                    string elemNS = _namespaceManager!.LookupNamespace(r.Prefix)!;
                    if (elemNS != string.Empty)
                    {
                        XmlAttribute attr = CreateXmlNsAttribute(r.Prefix, elemNS);
                        attributes.Append(attr);
                    }
                }

                while (r.MoveToNextAttribute())
                {
                    if (r.Prefix.Length != 0)
                    {
                        string? attNS = _annotationNSManager.LookupNamespace(r.Prefix);
                        if (attNS == null)
                        {
                            XmlAttribute attr = CreateXmlNsAttribute(r.Prefix, _namespaceManager!.LookupNamespace(r.Prefix)!);
                            attributes.Append(attr);
                        }
                    }
                }
                r.MoveToElement();

                _parentNode!.AppendChild(element);
                if (!r.IsEmptyElement)
                {
                    _parentNode = element;
                }
            }
            return element;
        }

        private XmlAttribute CreateXmlNsAttribute(string prefix, string value)
        {
            XmlAttribute attr;
            if (prefix.Length == 0)
            {
                attr = _dummyDocument.CreateAttribute(string.Empty, _xmlns!, XmlReservedNs.NsXmlNs);
            }
            else
            {
                attr = _dummyDocument.CreateAttribute(_xmlns, prefix, XmlReservedNs.NsXmlNs);
            }
            attr.AppendChild(_dummyDocument.CreateTextNode(value));
            _annotationNSManager!.AddNamespace(prefix, value);
            return attr;
        }

        private XmlAttribute LoadAttributeNode()
        {
            Debug.Assert(_reader!.NodeType == XmlNodeType.Attribute);

            XmlReader r = _reader;

            XmlAttribute attr = _dummyDocument.CreateAttribute(r.Prefix, r.LocalName, r.NamespaceURI);

            while (r.ReadAttributeValue())
            {
                switch (r.NodeType)
                {
                    case XmlNodeType.Text:
                        attr.AppendChild(_dummyDocument.CreateTextNode(r.Value));
                        continue;
                    case XmlNodeType.EntityReference:
                        attr.AppendChild(LoadEntityReferenceInAttribute());
                        continue;
                    default:
                        throw XmlLoader.UnexpectedNodeType(r.NodeType);
                }
            }

            return attr;
        }

        private XmlEntityReference LoadEntityReferenceInAttribute()
        {
            Debug.Assert(_reader!.NodeType == XmlNodeType.EntityReference);

            XmlEntityReference eref = _dummyDocument.CreateEntityReference(_reader.LocalName);
            if (!_reader.CanResolveEntity)
            {
                return eref;
            }
            _reader.ResolveEntity();

            while (_reader.ReadAttributeValue())
            {
                switch (_reader.NodeType)
                {
                    case XmlNodeType.Text:
                        eref.AppendChild(_dummyDocument.CreateTextNode(_reader.Value));
                        continue;
                    case XmlNodeType.EndEntity:
                        if (eref.ChildNodes.Count == 0)
                        {
                            eref.AppendChild(_dummyDocument.CreateTextNode(string.Empty));
                        }
                        return eref;
                    case XmlNodeType.EntityReference:
                        eref.AppendChild(LoadEntityReferenceInAttribute());
                        break;
                    default:
                        throw XmlLoader.UnexpectedNodeType(_reader.NodeType);
                }
            }

            return eref;
        }
    };
} // namespace System.Xml
