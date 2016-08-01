// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.XPath;

namespace System.Transactions.Diagnostics
{
    /// <summary>
    /// Very basic performance-oriented XmlWriter implementation. No validation/encoding is made.
    /// Namespaces are not supported
    /// Minimal formatting support
    /// </summary>
    internal class TraceXPathNavigator : XPathNavigator
    {
        private ElementNode _root = null;
        private ElementNode _current = null;
        private bool _closed = false;
        private XPathNodeType _state = XPathNodeType.Element;

        private class ElementNode
        {
            internal ElementNode(string name, string prefix, string xmlns, ElementNode parent)
            {
                this.name = name;
                this.prefix = prefix;
                this.xmlns = xmlns;
                this.parent = parent;
            }

            internal string name;
            internal string xmlns;
            internal string prefix;
            internal List<ElementNode> childNodes = new List<ElementNode>();
            internal ElementNode parent;
            internal List<AttributeNode> attributes = new List<AttributeNode>();
            internal TextNode text;
            internal bool movedToText = false;

            internal ElementNode MoveToNext()
            {
                ElementNode retval = null;
                if ((_elementIndex + 1) < childNodes.Count)
                {
                    ++_elementIndex;
                    retval = childNodes[_elementIndex];
                }
                return retval;
            }

            internal bool MoveToFirstAttribute()
            {
                _attributeIndex = 0;
                return attributes.Count > 0;
            }

            internal bool MoveToNextAttribute()
            {
                bool retval = false;
                if ((_attributeIndex + 1) < attributes.Count)
                {
                    ++_attributeIndex;
                    retval = true;
                }
                return retval;
            }

            internal void Reset()
            {
                _attributeIndex = 0;
                _elementIndex = 0;
                foreach (ElementNode node in childNodes)
                {
                    node.Reset();
                }
            }

            internal AttributeNode CurrentAttribute
            {
                get
                {
                    return attributes[_attributeIndex];
                }
            }

            private int _attributeIndex = 0;
            private int _elementIndex = 0;
        }

        private class AttributeNode
        {
            internal AttributeNode(string name, string prefix, string xmlns, string value)
            {
                this.name = name;
                this.prefix = prefix;
                this.xmlns = xmlns;
                nodeValue = value;
            }

            internal string name;
            internal string xmlns;
            internal string prefix;
            internal string nodeValue;
        }

        private class TextNode
        {
            internal TextNode(string value)
            {
                nodeValue = value;
            }
            internal string nodeValue;
        }

        internal void AddElement(string prefix, string name, string xmlns)
        {
            ElementNode node = new ElementNode(name, prefix, xmlns, _current);
            if (_closed)
            {
                throw new InvalidOperationException(SR.CannotAddToClosedDocument);
            }
            else
            {
                if (_current == null)
                {
                    _root = node;
                    _current = _root;
                }
                else if (!_closed)
                {
                    _current.childNodes.Add(node);
                    _current = node;
                }
            }
        }

        internal void AddText(string value)
        {
            if (_closed)
            {
                throw new InvalidOperationException(SR.CannotAddToClosedDocument);
            }
            if (_current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            else if (_current.text != null)
            {
                throw new InvalidOperationException(SR.TextNodeAlreadyPopulated);
            }
            else
            {
                _current.text = new TextNode(value);
            }
        }

        internal void AddAttribute(string name, string value, string xmlns, string prefix)
        {
            if (_closed)
            {
                throw new InvalidOperationException(SR.CannotAddToClosedDocument);
            }
            if (_current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            AttributeNode node = new AttributeNode(name, prefix, xmlns, value);
            _current.attributes.Add(node);
        }

        internal void CloseElement()
        {
            if (_closed)
            {
                throw new InvalidOperationException(SR.DocumentAlreadyClosed);
            }
            else
            {
                _current = _current.parent;
                if (_current == null)
                {
                    _closed = true;
                }
            }
        }

        public override string BaseURI
        {
            get { return null; }
        }

        public override XPathNavigator Clone()
        {
            return this;
        }

        public override bool IsEmptyElement
        {
            get
            {
                bool retval = true;
                if (_current != null)
                {
                    retval = _current.text != null || _current.childNodes.Count > 0;
                }
                return retval;
            }
        }

        public override bool IsSamePosition(XPathNavigator other)
        {
            throw new NotSupportedException();
        }

        public override string LocalName
        {
            get { return Name; }
        }

        public override bool MoveTo(XPathNavigator other)
        {
            throw new NotSupportedException();
        }

        public override bool MoveToFirstAttribute()
        {
            if (_current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            bool retval = _current.MoveToFirstAttribute();
            if (retval)
            {
                _state = XPathNodeType.Attribute;
            }
            return retval;
        }

        public override bool MoveToFirstChild()
        {
            if (_current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            bool retval = false;
            if (_current.childNodes.Count > 0)
            {
                _current = _current.childNodes[0];
                _state = XPathNodeType.Element;
                retval = true;
            }
            else if (_current.childNodes.Count == 0 && _current.text != null)
            {
                _state = XPathNodeType.Text;
                _current.movedToText = true;
                retval = true;
            }
            return retval;
        }

        public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
        {
            return false;
        }

        public override bool MoveToId(string id)
        {
            throw new NotSupportedException();
        }

        public override bool MoveToNext()
        {
            if (_current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            bool retval = false;
            if (_state != XPathNodeType.Text)
            {
                ElementNode parent = _current.parent;
                if (parent != null)
                {
                    ElementNode temp = parent.MoveToNext();
                    if (temp == null && parent.text != null && !parent.movedToText)
                    {
                        _state = XPathNodeType.Text;
                        parent.movedToText = true;
                        retval = true;
                    }
                    else if (temp != null)
                    {
                        _state = XPathNodeType.Element;
                        retval = true;
                        _current = temp;
                    }
                }
            }
            return retval;
        }

        public override bool MoveToNextAttribute()
        {
            if (_current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            bool retval = _current.MoveToNextAttribute();
            if (retval)
            {
                _state = XPathNodeType.Attribute;
            }
            return retval;
        }

        public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
        {
            return false;
        }

        public override bool MoveToParent()
        {
            if (_current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            bool retval = false;
            switch (_state)
            {
                case XPathNodeType.Element:
                    if (_current.parent != null)
                    {
                        _current = _current.parent;
                        _state = XPathNodeType.Element;
                        retval = true;
                    }
                    break;
                case XPathNodeType.Attribute:
                    _state = XPathNodeType.Element;
                    retval = true;
                    break;
                case XPathNodeType.Text:
                    _state = XPathNodeType.Element;
                    retval = true;
                    break;
                case XPathNodeType.Namespace:
                    _state = XPathNodeType.Element;
                    retval = true;
                    break;
            }
            return retval;
        }

        public override bool MoveToPrevious()
        {
            throw new NotSupportedException();
        }

        public override void MoveToRoot()
        {
            _current = _root;
            _state = XPathNodeType.Element;
            _root.Reset();
        }

        public override string Name
        {
            get
            {
                if (_current == null)
                {
                    throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
                }
                string retval = null;
                switch (_state)
                {
                    case XPathNodeType.Element:
                        retval = _current.name;
                        break;
                    case XPathNodeType.Attribute:
                        retval = _current.CurrentAttribute.name;
                        break;
                }
                return retval;
            }
        }

        public override System.Xml.XmlNameTable NameTable
        {
            get { return null; }
        }

        public override string NamespaceURI
        {
            get { return null; }
        }

        public override XPathNodeType NodeType
        {
            get { return _state; }
        }

        public override string Prefix
        {
            get
            {
                if (_current == null)
                {
                    throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
                }
                string retval = null;
                switch (_state)
                {
                    case XPathNodeType.Element:
                        retval = _current.prefix;
                        break;
                    case XPathNodeType.Attribute:
                        retval = _current.CurrentAttribute.prefix;
                        break;
                    case XPathNodeType.Namespace:
                        retval = _current.prefix;
                        break;
                }
                return retval;
            }
        }

        public override string Value
        {
            get
            {
                if (_current == null)
                {
                    throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
                }
                string retval = null;
                switch (_state)
                {
                    case XPathNodeType.Text:
                        retval = _current.text.nodeValue;
                        break;
                    case XPathNodeType.Attribute:
                        retval = _current.CurrentAttribute.nodeValue;
                        break;
                    case XPathNodeType.Namespace:
                        retval = _current.xmlns;
                        break;
                }
                return retval;
            }
        }


        public override string ToString()
        {
            MoveToRoot();
            return OuterXml;
            //StringBuilder sb = new StringBuilder();
            //XmlTextWriter writer = new XmlTextWriter(new StringWriter(sb, CultureInfo.CurrentCulture));
            //writer.WriteNode(this, false);
            //return sb.ToString();
        }
    }
}
