// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization.DataContracts;
using System.Xml;

namespace System.Runtime.Serialization
{
    // NOTE: XmlReader methods that are not needed have been left un-implemented

    internal sealed class ExtensionDataReader : XmlReader
    {
        private enum ExtensionDataNodeType
        {
            None,
            Element,
            EndElement,
            Text,
            Xml,
            ReferencedElement,
            NullElement,
        }

        private ElementData?[]? _elements;
        private ElementData? _element;
        private ElementData? _nextElement;

        private ReadState _readState = ReadState.Initial;
        private ExtensionDataNodeType _internalNodeType;
        private XmlNodeType _nodeType;
        private int _depth;
        private string? _localName;
        private string? _ns;
        private string? _prefix;
        private string? _value;
        private int _attributeCount;
        private int _attributeIndex;

        private readonly Hashtable _cache = new Hashtable();
        private XmlNodeReader? _xmlNodeReader;
        private Queue<IDataNode>? _deserializedDataNodes;

        private static readonly object s_prefixLock = new object();


        private readonly XmlObjectSerializerReadContext _context;

        private static readonly Hashtable s_nsToPrefixTable = new Hashtable();

        private static readonly Hashtable s_prefixToNsTable = new Hashtable();

        static ExtensionDataReader()
        {
            AddPrefix(Globals.XsiPrefix, Globals.SchemaInstanceNamespace);
            AddPrefix(Globals.SerPrefix, Globals.SerializationNamespace);
            AddPrefix(string.Empty, string.Empty);
        }

        internal ExtensionDataReader(XmlObjectSerializerReadContext context)
        {
            _attributeIndex = -1;
            _context = context;
        }

        internal void SetDeserializedValue(object? obj)
        {
            IDataNode? deserializedDataNode = (_deserializedDataNodes == null || _deserializedDataNodes.Count == 0) ? null : _deserializedDataNodes.Dequeue();
            if (deserializedDataNode != null && !(obj is IDataNode))
            {
                deserializedDataNode.Value = obj;
                deserializedDataNode.IsFinalValue = true;
            }
        }

        internal IDataNode? GetCurrentNode()
        {
            IDataNode? retVal = _element!.dataNode;
            Skip();
            return retVal;
        }

        internal void SetDataNode(IDataNode dataNode, string? name, string? ns)
        {
            SetNextElement(dataNode, name, ns, null);
            _element = _nextElement;
            _nextElement = null;
            SetElement();
        }

        internal void Reset()
        {
            _localName = null;
            _ns = null;
            _prefix = null;
            _value = null;
            _attributeCount = 0;
            _attributeIndex = -1;
            _depth = 0;
            _element = null;
            _nextElement = null;
            _elements = null;
        }

        [MemberNotNullWhen(true, nameof(_xmlNodeReader))]
        [MemberNotNullWhen(false, nameof(_element))]
        private bool IsXmlDataNode { get { return (_internalNodeType == ExtensionDataNodeType.Xml); } }

        public override XmlNodeType NodeType { get { return IsXmlDataNode ? _xmlNodeReader.NodeType : _nodeType; } }
        public override string LocalName { get { return IsXmlDataNode ? _xmlNodeReader.LocalName : _localName!; } }
        public override string NamespaceURI { get { return IsXmlDataNode ? _xmlNodeReader.NamespaceURI : _ns!; } }
        public override string Prefix { get { return IsXmlDataNode ? _xmlNodeReader.Prefix : _prefix!; } }
        public override string Value { get { return IsXmlDataNode ? _xmlNodeReader.Value : _value!; } }
        public override int Depth { get { return IsXmlDataNode ? _xmlNodeReader.Depth : _depth; } }
        public override int AttributeCount { get { return IsXmlDataNode ? _xmlNodeReader.AttributeCount : _attributeCount; } }
        public override bool EOF { get { return IsXmlDataNode ? _xmlNodeReader.EOF : (_readState == ReadState.EndOfFile); } }
        public override ReadState ReadState { get { return IsXmlDataNode ? _xmlNodeReader.ReadState : _readState; } }
        public override bool IsEmptyElement { get { return IsXmlDataNode ? _xmlNodeReader.IsEmptyElement : false; } }
        public override bool IsDefault { get { return IsXmlDataNode ? _xmlNodeReader.IsDefault : base.IsDefault; } }
        //public override char QuoteChar { get { return IsXmlDataNode ? xmlNodeReader.QuoteChar : base.QuoteChar; } }
        public override XmlSpace XmlSpace { get { return IsXmlDataNode ? _xmlNodeReader.XmlSpace : base.XmlSpace; } }
        public override string XmlLang { get { return IsXmlDataNode ? _xmlNodeReader.XmlLang : base.XmlLang; } }
        public override string this[int i] { get { return IsXmlDataNode ? _xmlNodeReader[i] : GetAttribute(i); } }
        public override string? this[string name] { get { return IsXmlDataNode ? _xmlNodeReader[name] : GetAttribute(name); } }
        public override string? this[string name, string? namespaceURI] { get { return IsXmlDataNode ? _xmlNodeReader[name, namespaceURI] : GetAttribute(name, namespaceURI); } }

        public override bool MoveToFirstAttribute()
        {
            if (IsXmlDataNode)
                return _xmlNodeReader.MoveToFirstAttribute();

            if (_attributeCount == 0)
                return false;
            MoveToAttribute(0);
            return true;
        }

        public override bool MoveToNextAttribute()
        {
            if (IsXmlDataNode)
                return _xmlNodeReader.MoveToNextAttribute();

            if (_attributeIndex + 1 >= _attributeCount)
                return false;
            MoveToAttribute(_attributeIndex + 1);
            return true;
        }

        public override void MoveToAttribute(int index)
        {
            if (IsXmlDataNode)
                _xmlNodeReader.MoveToAttribute(index);
            else
            {
                if (index < 0 || index >= _attributeCount)
                    throw new XmlException(SR.InvalidXmlDeserializingExtensionData);

                _nodeType = XmlNodeType.Attribute;
                AttributeData attribute = _element.attributes![index];
                _localName = attribute.localName;
                _ns = attribute.ns;
                _prefix = attribute.prefix;
                _value = attribute.value;
                _attributeIndex = index;
            }
        }

        public override string? GetAttribute(string name, string? namespaceURI)
        {
            if (IsXmlDataNode)
                return _xmlNodeReader.GetAttribute(name, namespaceURI);

            for (int i = 0; i < _element.attributeCount; i++)
            {
                AttributeData attribute = _element.attributes![i];
                if (attribute.localName == name && attribute.ns == namespaceURI)
                    return attribute.value;
            }

            return null;
        }

        public override bool MoveToAttribute(string name, string? namespaceURI)
        {
            if (IsXmlDataNode)
                return _xmlNodeReader.MoveToAttribute(name, _ns);

            for (int i = 0; i < _element.attributeCount; i++)
            {
                AttributeData attribute = _element.attributes![i];
                if (attribute.localName == name && attribute.ns == namespaceURI)
                {
                    MoveToAttribute(i);
                    return true;
                }
            }

            return false;
        }

        public override bool MoveToElement()
        {
            if (IsXmlDataNode)
                return _xmlNodeReader.MoveToElement();

            if (_nodeType != XmlNodeType.Attribute)
                return false;

            SetElement();
            return true;
        }

        private void SetElement()
        {
            _nodeType = XmlNodeType.Element;
            _localName = _element!.localName;
            _ns = _element.ns;
            _prefix = _element.prefix;
            _value = string.Empty;
            _attributeCount = _element.attributeCount;
            _attributeIndex = -1;
        }

        public override string? LookupNamespace(string prefix)
        {
            if (IsXmlDataNode)
                return _xmlNodeReader.LookupNamespace(prefix);

            return (string?)s_prefixToNsTable[prefix];
        }

        public override void Skip()
        {
            if (IsXmlDataNode)
                _xmlNodeReader.Skip();
            else
            {
                if (ReadState != ReadState.Interactive)
                    return;
                MoveToElement();
                if (IsElementNode(_internalNodeType))
                {
                    int depth = 1;
                    while (depth != 0)
                    {
                        if (!Read())
                            throw new XmlException(SR.InvalidXmlDeserializingExtensionData);

                        if (IsElementNode(_internalNodeType))
                            depth++;
                        else if (_internalNodeType == ExtensionDataNodeType.EndElement)
                        {
                            ReadEndElement();
                            depth--;
                        }
                    }
                }
                else
                    Read();
            }
        }

        private static bool IsElementNode(ExtensionDataNodeType nodeType)
        {
            return (nodeType == ExtensionDataNodeType.Element ||
                nodeType == ExtensionDataNodeType.ReferencedElement ||
                nodeType == ExtensionDataNodeType.NullElement);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsXmlDataNode)
                _xmlNodeReader.Dispose();
            else
            {
                Reset();
                _readState = ReadState.Closed;
            }

            base.Dispose(disposing);
        }

        public override bool Read()
        {
            if (_nodeType == XmlNodeType.Attribute && MoveToNextAttribute())
                return true;

            MoveNext(_element!.dataNode);

            switch (_internalNodeType)
            {
                case ExtensionDataNodeType.Element:
                case ExtensionDataNodeType.ReferencedElement:
                case ExtensionDataNodeType.NullElement:
                    PushElement();
                    SetElement();
                    break;

                case ExtensionDataNodeType.Text:
                    _nodeType = XmlNodeType.Text;
                    _prefix = string.Empty;
                    _ns = string.Empty;
                    _localName = string.Empty;
                    _attributeCount = 0;
                    _attributeIndex = -1;
                    break;

                case ExtensionDataNodeType.EndElement:
                    _nodeType = XmlNodeType.EndElement;
                    _prefix = string.Empty;
                    _ns = string.Empty;
                    _localName = string.Empty;
                    _value = string.Empty;
                    _attributeCount = 0;
                    _attributeIndex = -1;
                    PopElement();
                    break;

                case ExtensionDataNodeType.None:
                    if (_depth != 0)
                        throw new XmlException(SR.InvalidXmlDeserializingExtensionData);
                    _nodeType = XmlNodeType.None;
                    _prefix = string.Empty;
                    _ns = string.Empty;
                    _localName = string.Empty;
                    _value = string.Empty;
                    _attributeCount = 0;
                    _readState = ReadState.EndOfFile;
                    return false;

                case ExtensionDataNodeType.Xml:
                    // do nothing
                    break;

                default:
                    Debug.Fail("ExtensionDataReader in invalid state");
                    throw new SerializationException(SR.InvalidStateInExtensionDataReader);
            }
            _readState = ReadState.Interactive;
            return true;
        }

        public override string Name
        {
            get
            {
                if (IsXmlDataNode)
                {
                    return _xmlNodeReader.Name;
                }
                Debug.Fail("ExtensionDataReader Name property should only be called for IXmlSerializable");
                return string.Empty;
            }
        }

        public override bool HasValue
        {
            get
            {
                if (IsXmlDataNode)
                {
                    return _xmlNodeReader.HasValue;
                }
                Debug.Fail("ExtensionDataReader HasValue property should only be called for IXmlSerializable");
                return false;
            }
        }

        public override string BaseURI
        {
            get
            {
                if (IsXmlDataNode)
                {
                    return _xmlNodeReader.BaseURI;
                }
                Debug.Fail("ExtensionDataReader BaseURI property should only be called for IXmlSerializable");
                return string.Empty;
            }
        }

        public override XmlNameTable NameTable
        {
            get
            {
                if (IsXmlDataNode)
                {
                    return _xmlNodeReader.NameTable;
                }
                Debug.Fail("ExtensionDataReader NameTable property should only be called for IXmlSerializable");
                return null;
            }
        }

        public override string? GetAttribute(string name)
        {
            if (IsXmlDataNode)
            {
                return _xmlNodeReader.GetAttribute(name);
            }
            Debug.Fail("ExtensionDataReader GetAttribute method should only be called for IXmlSerializable");
            return null;
        }

        public override string GetAttribute(int i)
        {
            if (IsXmlDataNode)
            {
                return _xmlNodeReader.GetAttribute(i);
            }
            Debug.Fail("ExtensionDataReader GetAttribute method should only be called for IXmlSerializable");
            return null;
        }

        public override bool MoveToAttribute(string name)
        {
            if (IsXmlDataNode)
            {
                return _xmlNodeReader.MoveToAttribute(name);
            }
            Debug.Fail("ExtensionDataReader MoveToAttribute method should only be called for IXmlSerializable");
            return false;
        }

        public override void ResolveEntity()
        {
            if (IsXmlDataNode)
            {
                _xmlNodeReader.ResolveEntity();
            }
            else
            {
                Debug.Fail("ExtensionDataReader ResolveEntity method should only be called for IXmlSerializable");
            }
        }

        public override bool ReadAttributeValue()
        {
            if (IsXmlDataNode)
            {
                return _xmlNodeReader.ReadAttributeValue();
            }
            Debug.Fail("ExtensionDataReader ReadAttributeValue method should only be called for IXmlSerializable");
            return false;
        }

        private void MoveNext(IDataNode? dataNode)
        {
            switch (_internalNodeType)
            {
                case ExtensionDataNodeType.Text:
                case ExtensionDataNodeType.ReferencedElement:
                case ExtensionDataNodeType.NullElement:
                    _internalNodeType = ExtensionDataNodeType.EndElement;
                    return;
                default:
                    Type? dataNodeType = dataNode?.DataType;
                    if (dataNodeType == Globals.TypeOfClassDataNode)
                        MoveNextInClass((ClassDataNode)dataNode!);
                    else if (dataNodeType == Globals.TypeOfCollectionDataNode)
                        MoveNextInCollection((CollectionDataNode)dataNode!);
                    else if (dataNodeType == Globals.TypeOfISerializableDataNode)
                        MoveNextInISerializable((ISerializableDataNode)dataNode!);
                    else if (dataNodeType == Globals.TypeOfXmlDataNode)
                        MoveNextInXml((XmlDataNode)dataNode!);
                    else if (dataNode?.Value != null)
                        MoveToDeserializedObject(dataNode!);
                    else
                    {
                        Debug.Fail("Encountered invalid data node when deserializing unknown data");
                        throw new SerializationException(SR.Format(SR.InvalidStateInExtensionDataReader));
                    }
                    break;
            }
        }

        private void SetNextElement(IDataNode? node, string? name, string? ns, string? prefix)
        {
            _internalNodeType = ExtensionDataNodeType.Element;
            _nextElement = GetNextElement();
            _nextElement.localName = name;
            _nextElement.ns = ns;
            _nextElement.prefix = prefix;
            if (node == null)
            {
                _nextElement.attributeCount = 0;
                _nextElement.AddAttribute(Globals.XsiPrefix, Globals.SchemaInstanceNamespace, Globals.XsiNilLocalName, Globals.True);
                _internalNodeType = ExtensionDataNodeType.NullElement;
            }
            else if (!CheckIfNodeHandled(node))
            {
                AddDeserializedDataNode(node);
                node.GetData(_nextElement);
                if (node is XmlDataNode xdn)
                    MoveNextInXml(xdn);
            }
        }

        private void AddDeserializedDataNode(IDataNode node)
        {
            if (node.Id != Globals.NewObjectId && (node.Value == null || !node.IsFinalValue))
            {
                _deserializedDataNodes ??= new Queue<IDataNode>();
                _deserializedDataNodes.Enqueue(node);
            }
        }

        private bool CheckIfNodeHandled(IDataNode node)
        {
            bool handled = false;
            if (node.Id != Globals.NewObjectId)
            {
                handled = (_cache[node] != null);
                if (handled)
                {
                    _nextElement ??= GetNextElement();
                    _nextElement.attributeCount = 0;
                    _nextElement.AddAttribute(Globals.SerPrefix, Globals.SerializationNamespace, Globals.RefLocalName, node.Id.ToString(NumberFormatInfo.InvariantInfo));
                    _nextElement.AddAttribute(Globals.XsiPrefix, Globals.SchemaInstanceNamespace, Globals.XsiNilLocalName, Globals.True);
                    _internalNodeType = ExtensionDataNodeType.ReferencedElement;
                }
                else
                {
                    _cache.Add(node, node);
                }
            }
            return handled;
        }

        private void MoveNextInClass(ClassDataNode dataNode)
        {
            // Two frames above here in Read(), _element is asserted not null.
            Debug.Assert(_element != null);
            if (dataNode.Members != null && _element.childElementIndex < dataNode.Members.Count)
            {
                if (_element.childElementIndex == 0)
                    _context.IncrementItemCount(-dataNode.Members.Count);

                ExtensionDataMember member = dataNode.Members[_element.childElementIndex++];
                SetNextElement(member.Value, member.Name, member.Namespace, GetPrefix(member.Namespace));
            }
            else
            {
                _internalNodeType = ExtensionDataNodeType.EndElement;
                _element.childElementIndex = 0;
            }
        }

        private void MoveNextInCollection(CollectionDataNode dataNode)
        {
            // Two frames above here in Read(), _element is asserted not null.
            Debug.Assert(_element != null);
            if (dataNode.Items != null && _element.childElementIndex < dataNode.Items.Count)
            {
                if (_element.childElementIndex == 0)
                    _context.IncrementItemCount(-dataNode.Items.Count);

                IDataNode? item = dataNode.Items[_element.childElementIndex++];
                SetNextElement(item, dataNode.ItemName, dataNode.ItemNamespace, GetPrefix(dataNode.ItemNamespace));
            }
            else
            {
                _internalNodeType = ExtensionDataNodeType.EndElement;
                _element.childElementIndex = 0;
            }
        }

        private void MoveNextInISerializable(ISerializableDataNode dataNode)
        {
            // Two frames above here in Read(), _element is asserted not null.
            Debug.Assert(_element != null);
            if (dataNode.Members != null && _element.childElementIndex < dataNode.Members.Count)
            {
                if (_element.childElementIndex == 0)
                    _context.IncrementItemCount(-dataNode.Members.Count);

                ISerializableDataMember member = dataNode.Members[_element.childElementIndex++];
                SetNextElement(member.Value, member.Name, string.Empty, string.Empty);
            }
            else
            {
                _internalNodeType = ExtensionDataNodeType.EndElement;
                _element.childElementIndex = 0;
            }
        }

        private void MoveNextInXml(XmlDataNode dataNode)
        {
            if (IsXmlDataNode)
            {
                _xmlNodeReader.Read();
                if (_xmlNodeReader.Depth == 0)
                {
                    _internalNodeType = ExtensionDataNodeType.EndElement;
                    _xmlNodeReader = null;
                }
            }
            else
            {
                _internalNodeType = ExtensionDataNodeType.Xml;
                if (_element == null)
                    _element = _nextElement;
                else
                    PushElement();

                Debug.Assert(dataNode.OwnerDocument != null); // OwnerDocument is always set on initialized dataNodes

                XmlElement wrapperElement = XmlObjectSerializerReadContext.CreateWrapperXmlElement(dataNode.OwnerDocument,
                    dataNode.XmlAttributes, dataNode.XmlChildNodes, _element?.prefix, _element?.localName, _element?.ns);
                if (_element != null)
                {
                    for (int i = 0; i < _element.attributeCount; i++)
                    {
                        AttributeData a = _element.attributes![i];
                        XmlAttribute xmlAttr = dataNode.OwnerDocument.CreateAttribute(a.prefix, a.localName!, a.ns);
                        xmlAttr.Value = a.value;
                        wrapperElement.Attributes.Append(xmlAttr);
                    }
                }
                _xmlNodeReader = new XmlNodeReader(wrapperElement);
                _xmlNodeReader.Read();
            }
        }

        private void MoveToDeserializedObject(IDataNode dataNode)
        {
            Type type = dataNode.DataType;
            bool isTypedNode = true;
            if (type == Globals.TypeOfObject && dataNode.Value != null)
            {
                type = dataNode.Value.GetType();
                if (type == Globals.TypeOfObject)
                {
                    _internalNodeType = ExtensionDataNodeType.EndElement;
                    return;
                }
                isTypedNode = false;
            }

            if (!MoveToText(type, dataNode, isTypedNode))
            {
                if (dataNode.IsFinalValue)
                {
                    _internalNodeType = ExtensionDataNodeType.EndElement;
                }
                else
                {
                    throw new XmlException(SR.Format(SR.InvalidDataNode, DataContract.GetClrTypeFullName(type)));
                }
            }
        }

        private bool MoveToText(Type type, IDataNode dataNode, bool isTypedNode)
        {
            Debug.Assert(dataNode.Value != null);

            bool handled = true;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    _value = XmlConvert.ToString(isTypedNode ? ((DataNode<bool>)dataNode).GetValue() : (bool)dataNode.Value);
                    break;
                case TypeCode.Char:
                    _value = XmlConvert.ToString((int)(isTypedNode ? ((DataNode<char>)dataNode).GetValue() : (char)dataNode.Value));
                    break;
                case TypeCode.Byte:
                    _value = XmlConvert.ToString(isTypedNode ? ((DataNode<byte>)dataNode).GetValue() : (byte)dataNode.Value);
                    break;
                case TypeCode.Int16:
                    _value = XmlConvert.ToString(isTypedNode ? ((DataNode<short>)dataNode).GetValue() : (short)dataNode.Value);
                    break;
                case TypeCode.Int32:
                    _value = XmlConvert.ToString(isTypedNode ? ((DataNode<int>)dataNode).GetValue() : (int)dataNode.Value);
                    break;
                case TypeCode.Int64:
                    _value = XmlConvert.ToString(isTypedNode ? ((DataNode<long>)dataNode).GetValue() : (long)dataNode.Value);
                    break;
                case TypeCode.Single:
                    _value = XmlConvert.ToString(isTypedNode ? ((DataNode<float>)dataNode).GetValue() : (float)dataNode.Value);
                    break;
                case TypeCode.Double:
                    _value = XmlConvert.ToString(isTypedNode ? ((DataNode<double>)dataNode).GetValue() : (double)dataNode.Value);
                    break;
                case TypeCode.Decimal:
                    _value = XmlConvert.ToString(isTypedNode ? ((DataNode<decimal>)dataNode).GetValue() : (decimal)dataNode.Value);
                    break;
                case TypeCode.DateTime:
                    DateTime dateTime = isTypedNode ? ((DataNode<DateTime>)dataNode).GetValue() : (DateTime)dataNode.Value;
                    _value = dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK", DateTimeFormatInfo.InvariantInfo);
                    break;
                case TypeCode.String:
                    _value = isTypedNode ? ((DataNode<string>)dataNode).GetValue() : (string?)dataNode.Value;
                    break;
                case TypeCode.SByte:
                    _value = XmlConvert.ToString(isTypedNode ? ((DataNode<sbyte>)dataNode).GetValue() : (sbyte)dataNode.Value);
                    break;
                case TypeCode.UInt16:
                    _value = XmlConvert.ToString(isTypedNode ? ((DataNode<ushort>)dataNode).GetValue() : (ushort)dataNode.Value);
                    break;
                case TypeCode.UInt32:
                    _value = XmlConvert.ToString(isTypedNode ? ((DataNode<uint>)dataNode).GetValue() : (uint)dataNode.Value);
                    break;
                case TypeCode.UInt64:
                    _value = XmlConvert.ToString(isTypedNode ? ((DataNode<ulong>)dataNode).GetValue() : (ulong)dataNode.Value);
                    break;
                case TypeCode.Object:
                default:
                    if (type == Globals.TypeOfByteArray)
                    {
                        byte[]? bytes = isTypedNode ? ((DataNode<byte[]>)dataNode).GetValue() : (byte[])dataNode.Value;
                        _value = (bytes == null) ? string.Empty : Convert.ToBase64String(bytes);
                    }
                    else if (type == Globals.TypeOfTimeSpan)
                        _value = XmlConvert.ToString(isTypedNode ? ((DataNode<TimeSpan>)dataNode).GetValue() : (TimeSpan)dataNode.Value);
                    else if (type == Globals.TypeOfGuid)
                    {
                        Guid guid = isTypedNode ? ((DataNode<Guid>)dataNode).GetValue() : (Guid)dataNode.Value;
                        _value = guid.ToString();
                    }
                    else if (type == Globals.TypeOfUri)
                    {
                        Uri uri = isTypedNode ? ((DataNode<Uri>)dataNode).GetValue() : (Uri)dataNode.Value;
                        _value = uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
                    }
                    else
                        handled = false;
                    break;
            }

            if (handled)
                _internalNodeType = ExtensionDataNodeType.Text;
            return handled;
        }

        private void PushElement()
        {
            GrowElementsIfNeeded();
            _elements[_depth++] = _element;
            if (_nextElement == null)
                _element = GetNextElement();
            else
            {
                _element = _nextElement;
                _nextElement = null;
            }
        }

        private void PopElement()
        {
            _prefix = _element!.prefix;
            _localName = _element.localName;
            _ns = _element.ns;

            if (_depth == 0)
                return;

            _depth--;

            if (_elements != null)
            {
                _element = _elements[_depth];
            }
        }

        [MemberNotNull(nameof(_elements))]
        private void GrowElementsIfNeeded()
        {
            if (_elements == null)
            {
                _elements = new ElementData[8];
            }
            else if (_elements.Length == _depth)
            {
                ElementData[] newElements = new ElementData[_elements.Length * 2];
                Array.Copy(_elements, newElements, _elements.Length);
                _elements = newElements;
            }
        }

        private ElementData GetNextElement()
        {
            int nextDepth = _depth + 1;
            return (_elements == null || _elements.Length <= nextDepth || _elements[nextDepth] == null)
                ? new ElementData() : _elements[nextDepth]!;
        }

        internal static string GetPrefix(string? ns)
        {
            ns ??= string.Empty;
            string? prefix = (string?)s_nsToPrefixTable[ns];
            if (prefix == null)
            {
                lock (s_prefixLock)
                {
                    prefix = (string?)s_nsToPrefixTable[ns];
                    if (prefix == null)
                    {
                        prefix = (ns.Length == 0) ? string.Empty : "p" + s_nsToPrefixTable.Count;
                        AddPrefix(prefix, ns);
                    }
                }
            }
            return prefix;
        }

        private static void AddPrefix(string prefix, string ns)
        {
            s_nsToPrefixTable.Add(ns, prefix);
            s_prefixToNsTable.Add(prefix, ns);
        }
    }

    internal sealed class AttributeData
    {
        public string? prefix;
        public string? ns;
        public string? localName;
        public string? value;
    }

    internal sealed class ElementData
    {
        public string? localName;
        public string? ns;
        public string? prefix;
        public int attributeCount;
        public AttributeData[]? attributes;
        public IDataNode? dataNode;
        public int childElementIndex;

        public void AddAttribute(string prefix, string ns, string name, string? value)
        {
            GrowAttributesIfNeeded();
            AttributeData attribute = attributes[attributeCount] ??= new AttributeData();
            attribute.prefix = prefix;
            attribute.ns = ns;
            attribute.localName = name;
            attribute.value = value;
            attributeCount++;
        }

        [MemberNotNull(nameof(attributes))]
        private void GrowAttributesIfNeeded()
        {
            if (attributes == null)
            {
                attributes = new AttributeData[4];
            }
            else if (attributes.Length == attributeCount)
            {
                AttributeData[] newAttributes = new AttributeData[attributes.Length * 2];
                Array.Copy(attributes, newAttributes, attributes.Length);
                attributes = newAttributes;
            }
        }
    }
}
