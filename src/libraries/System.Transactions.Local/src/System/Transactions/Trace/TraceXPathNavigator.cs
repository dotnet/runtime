// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.XPath;
using System.Xml;

#nullable disable

namespace System.Transactions.Diagnostics
{
    class TraceXPathNavigator : XPathNavigator
    {
        ElementNode root = null;
        ElementNode current = null;
        bool closed = false;
        XPathNodeType state = XPathNodeType.Element;

        class ElementNode
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
                if ((this.elementIndex + 1) < this.childNodes.Count)
                {
                    ++this.elementIndex;
                    retval = this.childNodes[this.elementIndex];
                }
                return retval;
            }

            internal bool MoveToFirstAttribute()
            {
                this.attributeIndex = 0;
                return this.attributes.Count > 0;
            }

            internal bool MoveToNextAttribute()
            {
                bool retval = false;
                if ((this.attributeIndex + 1) < this.attributes.Count)
                {
                    ++this.attributeIndex;
                    retval = true;
                }
                return retval;
            }

            internal void Reset()
            {
                this.attributeIndex = 0;
                this.elementIndex = 0;
                foreach (ElementNode node in this.childNodes)
                {
                    node.Reset();
                }
            }

            internal AttributeNode CurrentAttribute
            {
                get
                {
                    return this.attributes[this.attributeIndex];
                }
            }

            int attributeIndex = 0;
            int elementIndex = 0;
        }

        class AttributeNode
        {
            internal AttributeNode(string name, string prefix, string xmlns, string value)
            {
                this.name = name;
                this.prefix = prefix;
                this.xmlns = xmlns;
                this.nodeValue = value;
            }

            internal string name;
            internal string xmlns;
            internal string prefix;
            internal string nodeValue;
        }

        class TextNode
        {
            internal TextNode(string value)
            {
                this.nodeValue = value;
            }
            internal string nodeValue;
        }

        internal void AddElement(string prefix, string name, string xmlns)
        {
            ElementNode node = new ElementNode(name, prefix, xmlns, this.current);
            if (this.closed)
            {
                throw new InvalidOperationException(SR.CannotAddToClosedDocument);
            }
            else
            {
                if (this.current == null)
                {
                    this.root = node;
                    this.current = this.root;
                }
                else if (!this.closed)
                {
                    this.current.childNodes.Add(node);
                    this.current = node;
                }
            }
        }

        internal void AddText(string value)
        {
            if (this.closed)
            {
                throw new InvalidOperationException(SR.CannotAddToClosedDocument);
            }
            if (this.current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            else if (this.current.text != null)
            {
                throw new InvalidOperationException(SR.TextNodeAlreadyPopulated);
            }
            else
            {
                this.current.text = new TextNode(value);
            }
        }

        internal void AddAttribute(string name, string value, string xmlns, string prefix)
        {
            if (this.closed)
            {
                throw new InvalidOperationException(SR.CannotAddToClosedDocument);
            }
            if (this.current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            AttributeNode node = new AttributeNode(name, prefix, xmlns, value);
            this.current.attributes.Add(node);
        }

        internal void CloseElement()
        {
            if (this.closed)
            {
                throw new InvalidOperationException(SR.DocumentAlreadyClosed);
            }
            else
            {
                this.current = this.current.parent;
                if (this.current == null)
                {
                    this.closed = true;
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
                if (this.current != null)
                {
                    retval = this.current.text != null || this.current.childNodes.Count > 0;
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
            get { return this.Name; }
        }

        public override bool MoveTo(XPathNavigator other)
        {
            throw new NotSupportedException();
        }

        public override bool MoveToFirstAttribute()
        {
            if (this.current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            bool retval = this.current.MoveToFirstAttribute();
            if (retval)
            {
                this.state = XPathNodeType.Attribute;
            }
            return retval;
        }

        public override bool MoveToFirstChild()
        {
            if (this.current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            bool retval = false;
            if (this.current.childNodes.Count > 0)
            {
                this.current = this.current.childNodes[0];
                this.state = XPathNodeType.Element;
                retval = true;
            }
            else if (this.current.childNodes.Count == 0 && this.current.text != null)
            {
                this.state = XPathNodeType.Text;
                this.current.movedToText = true;
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
            if (this.current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            bool retval = false;
            if (this.state != XPathNodeType.Text)
            {
                ElementNode parent = this.current.parent;
                if (parent != null)
                {
                    ElementNode temp = parent.MoveToNext();
                    if (temp == null && parent.text != null && !parent.movedToText)
                    {
                        this.state = XPathNodeType.Text;
                        parent.movedToText = true;
                        retval = true;
                    }
                    else if (temp != null)
                    {
                        this.state = XPathNodeType.Element;
                        retval = true;
                        this.current = temp;
                    }
                }
            }
            return retval;
        }

        public override bool MoveToNextAttribute()
        {
            if (this.current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            bool retval = this.current.MoveToNextAttribute();
            if (retval)
            {
                this.state = XPathNodeType.Attribute;
            }
            return retval;
        }

        public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
        {
            return false;
        }

        public override bool MoveToParent()
        {
            if (this.current == null)
            {
                throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
            }
            bool retval = false;
            switch (this.state)
            {
                case XPathNodeType.Element:
                    if (this.current.parent != null)
                    {
                        this.current = this.current.parent;
                        this.state = XPathNodeType.Element;
                        retval = true;
                    }
                    break;
                case XPathNodeType.Attribute:
                    this.state = XPathNodeType.Element;
                    retval = true;
                    break;
                case XPathNodeType.Text:
                    this.state = XPathNodeType.Element;
                    retval = true;
                    break;
                case XPathNodeType.Namespace:
                    this.state = XPathNodeType.Element;
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
            this.current = this.root;
            this.state = XPathNodeType.Element;
            this.root.Reset();
        }

        public override string Name
        {
            get
            {
                if (this.current == null)
                {
                    throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
                }
                string retval = null;
                switch (this.state)
                {
                    case XPathNodeType.Element:
                        retval = this.current.name;
                        break;
                    case XPathNodeType.Attribute:
                        retval = this.current.CurrentAttribute.name;
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
            get { return this.state; }
        }

        public override string Prefix
        {
            get
            {
                if (this.current == null)
                {
                    throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
                }
                string retval = null;
                switch (this.state)
                {
                    case XPathNodeType.Element:
                        retval = this.current.prefix;
                        break;
                    case XPathNodeType.Attribute:
                        retval = this.current.CurrentAttribute.prefix;
                        break;
                    case XPathNodeType.Namespace:
                        retval = this.current.prefix;
                        break;
                }
                return retval;
            }
        }

        public override string Value
        {
            get
            {
                if (this.current == null)
                {
                    throw new InvalidOperationException(SR.OperationInvalidOnAnEmptyDocument);
                }
                string retval = null;
                switch (this.state)
                {
                    case XPathNodeType.Text:
                        retval = this.current.text.nodeValue;
                        break;
                    case XPathNodeType.Attribute:
                        retval = this.current.CurrentAttribute.nodeValue;
                        break;
                    case XPathNodeType.Namespace:
                        retval = this.current.xmlns;
                        break;
                }
                return retval;
            }
        }


        public override string ToString()
        {
            this.MoveToRoot();
            StringBuilder sb = new StringBuilder();
            XmlTextWriter writer = new XmlTextWriter(new StringWriter(sb, CultureInfo.CurrentCulture));
            writer.WriteNode(this, false);
            return sb.ToString();
        }
    }
}
