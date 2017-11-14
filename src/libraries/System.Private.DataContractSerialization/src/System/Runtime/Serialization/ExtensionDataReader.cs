// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Collections.Generic;

namespace System.Runtime.Serialization
{
    // NOTE: XmlReader methods that are not needed have been left un-implemented

    internal class ExtensionDataReader : XmlReader
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

        private ElementData[] _elements;
        private ElementData _element;
        private ElementData _nextElement;

        private ReadState _readState = ReadState.Initial;
        private ExtensionDataNodeType _internalNodeType;
        private XmlNodeType _nodeType;
        private int _depth;
        private string _localName;
        private string _ns;
        private string _prefix;
        private string _value;
        private int _attributeCount;
        private int _attributeIndex;

#pragma warning disable 0649
        private XmlNodeReader _xmlNodeReader;
#pragma warning restore 0649

        private XmlObjectSerializerReadContext _context;

        private static Dictionary<string, string> s_nsToPrefixTable;

        private static Dictionary<string, string> s_prefixToNsTable;

        static ExtensionDataReader()
        {
            s_nsToPrefixTable = new Dictionary<string, string>();
            s_prefixToNsTable = new Dictionary<string, string>();
            AddPrefix(Globals.XsiPrefix, Globals.SchemaInstanceNamespace);
            AddPrefix(Globals.SerPrefix, Globals.SerializationNamespace);
            AddPrefix(String.Empty, String.Empty);
        }

        internal ExtensionDataReader(XmlObjectSerializerReadContext context)
        {
            _attributeIndex = -1;
            _context = context;
        }

        internal IDataNode GetCurrentNode()
        {
            IDataNode retVal = _element.dataNode;
            Skip();
            return retVal;
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

        private bool IsXmlDataNode { get { return (_internalNodeType == ExtensionDataNodeType.Xml); } }

        public override XmlNodeType NodeType { get { return IsXmlDataNode ? _xmlNodeReader.NodeType : _nodeType; } }
        public override string LocalName { get { return IsXmlDataNode ? _xmlNodeReader.LocalName : _localName; } }
        public override string NamespaceURI { get { return IsXmlDataNode ? _xmlNodeReader.NamespaceURI : _ns; } }
        public override string Prefix { get { return IsXmlDataNode ? _xmlNodeReader.Prefix : _prefix; } }
        public override string Value { get { return IsXmlDataNode ? _xmlNodeReader.Value : _value; } }
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
        public override string this[string name] { get { return IsXmlDataNode ? _xmlNodeReader[name] : GetAttribute(name); } }
        public override string this[string name, string namespaceURI] { get { return IsXmlDataNode ? _xmlNodeReader[name, namespaceURI] : GetAttribute(name, namespaceURI); } }

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
                AttributeData attribute = _element.attributes[index];
                _localName = attribute.localName;
                _ns = attribute.ns;
                _prefix = attribute.prefix;
                _value = attribute.value;
                _attributeIndex = index;
            }
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            if (IsXmlDataNode)
                return _xmlNodeReader.GetAttribute(name, namespaceURI);

            for (int i = 0; i < _element.attributeCount; i++)
            {
                AttributeData attribute = _element.attributes[i];
                if (attribute.localName == name && attribute.ns == namespaceURI)
                    return attribute.value;
            }

            return null;
        }

        public override bool MoveToAttribute(string name, string namespaceURI)
        {
            if (IsXmlDataNode)
                return _xmlNodeReader.MoveToAttribute(name, _ns);

            for (int i = 0; i < _element.attributeCount; i++)
            {
                AttributeData attribute = _element.attributes[i];
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
            _localName = _element.localName;
            _ns = _element.ns;
            _prefix = _element.prefix;
            _value = String.Empty;
            _attributeCount = _element.attributeCount;
            _attributeIndex = -1;
        }

        public override string LookupNamespace(string prefix)
        {
            if (IsXmlDataNode)
                return _xmlNodeReader.LookupNamespace(prefix);

            string ns;
            if (!s_prefixToNsTable.TryGetValue(prefix, out ns))
                return null;
            return ns;
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

        private bool IsElementNode(ExtensionDataNodeType nodeType)
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

            MoveNext(_element.dataNode);

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
                    _prefix = String.Empty;
                    _ns = String.Empty;
                    _localName = String.Empty;
                    _attributeCount = 0;
                    _attributeIndex = -1;
                    break;

                case ExtensionDataNodeType.EndElement:
                    _nodeType = XmlNodeType.EndElement;
                    _prefix = String.Empty;
                    _ns = String.Empty;
                    _localName = String.Empty;
                    _value = String.Empty;
                    _attributeCount = 0;
                    _attributeIndex = -1;
                    PopElement();
                    break;

                case ExtensionDataNodeType.None:
                    if (_depth != 0)
                        throw new XmlException(SR.InvalidXmlDeserializingExtensionData);
                    _nodeType = XmlNodeType.None;
                    _prefix = String.Empty;
                    _ns = String.Empty;
                    _localName = String.Empty;
                    _value = String.Empty;
                    _attributeCount = 0;
                    _readState = ReadState.EndOfFile;
                    return false;

                case ExtensionDataNodeType.Xml:
                    // do nothing
                    break;

                default:
                    Fx.Assert("ExtensionDataReader in invalid state");
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
                Fx.Assert("ExtensionDataReader Name property should only be called for IXmlSerializable");
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
                Fx.Assert("ExtensionDataReader HasValue property should only be called for IXmlSerializable");
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
                Fx.Assert("ExtensionDataReader BaseURI property should only be called for IXmlSerializable");
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
                Fx.Assert("ExtensionDataReader NameTable property should only be called for IXmlSerializable");
                return null;
            }
        }

        public override string GetAttribute(string name)
        {
            if (IsXmlDataNode)
            {
                return _xmlNodeReader.GetAttribute(name);
            }
            Fx.Assert("ExtensionDataReader GetAttribute method should only be called for IXmlSerializable");
            return null;
        }

        public override string GetAttribute(int i)
        {
            if (IsXmlDataNode)
            {
                return _xmlNodeReader.GetAttribute(i);
            }
            Fx.Assert("ExtensionDataReader GetAttribute method should only be called for IXmlSerializable");
            return null;
        }

        public override bool MoveToAttribute(string name)
        {
            if (IsXmlDataNode)
            {
                return _xmlNodeReader.MoveToAttribute(name);
            }
            Fx.Assert("ExtensionDataReader MoveToAttribute method should only be called for IXmlSerializable");
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
                Fx.Assert("ExtensionDataReader ResolveEntity method should only be called for IXmlSerializable");
            }
        }

        public override bool ReadAttributeValue()
        {
            if (IsXmlDataNode)
            {
                return _xmlNodeReader.ReadAttributeValue();
            }
            Fx.Assert("ExtensionDataReader ReadAttributeValue method should only be called for IXmlSerializable");
            return false;
        }

        private void MoveNext(IDataNode dataNode)
        {
            throw NotImplemented.ByDesign;
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
            _prefix = _element.prefix;
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

        private void GrowElementsIfNeeded()
        {
            if (_elements == null)
                _elements = new ElementData[8];
            else if (_elements.Length == _depth)
            {
                ElementData[] newElements = new ElementData[_elements.Length * 2];
                Array.Copy(_elements, 0, newElements, 0, _elements.Length);
                _elements = newElements;
            }
        }

        private ElementData GetNextElement()
        {
            int nextDepth = _depth + 1;
            return (_elements == null || _elements.Length <= nextDepth || _elements[nextDepth] == null)
                ? new ElementData() : _elements[nextDepth];
        }

        internal static string GetPrefix(string ns)
        {
            string prefix;
            ns = ns ?? String.Empty;
            if (!s_nsToPrefixTable.TryGetValue(ns, out prefix))
            {
                lock (s_nsToPrefixTable)
                {
                    if (!s_nsToPrefixTable.TryGetValue(ns, out prefix))
                    {
                        prefix = (ns == null || ns.Length == 0) ? String.Empty : "p" + s_nsToPrefixTable.Count;
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

#if USE_REFEMIT || uapaot
    public class AttributeData
#else
    internal class AttributeData
#endif
    {
        public string prefix;
        public string ns;
        public string localName;
        public string value;
    }

#if USE_REFEMIT || uapaot
    public class ElementData
#else
    internal class ElementData
#endif
    {
        public string localName;
        public string ns;
        public string prefix;
        public int attributeCount;
        public AttributeData[] attributes;
        public IDataNode dataNode;
        public int childElementIndex;

        public void AddAttribute(string prefix, string ns, string name, string value)
        {
            GrowAttributesIfNeeded();
            AttributeData attribute = attributes[attributeCount];
            if (attribute == null)
                attributes[attributeCount] = attribute = new AttributeData();
            attribute.prefix = prefix;
            attribute.ns = ns;
            attribute.localName = name;
            attribute.value = value;
            attributeCount++;
        }

        private void GrowAttributesIfNeeded()
        {
            if (attributes == null)
                attributes = new AttributeData[4];
            else if (attributes.Length == attributeCount)
            {
                AttributeData[] newAttributes = new AttributeData[attributes.Length * 2];
                Array.Copy(attributes, 0, newAttributes, 0, attributes.Length);
                attributes = newAttributes;
            }
        }
    }
}
