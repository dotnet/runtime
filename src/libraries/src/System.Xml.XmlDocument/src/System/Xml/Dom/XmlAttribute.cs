// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Xml
{
    // Represents an attribute of the XMLElement object. Valid and default
    // values for the attribute are defined in a DTD or schema.
    public class XmlAttribute : XmlNode
    {
        private XmlName _name;
        private XmlLinkedNode _lastChild;

        internal XmlAttribute(XmlName name, XmlDocument doc) : base(doc)
        {
            Debug.Assert(name != null);
            Debug.Assert(doc != null);
            this.parentNode = null;
            if (!doc.IsLoading)
            {
                XmlDocument.CheckName(name.Prefix);
                XmlDocument.CheckName(name.LocalName);
            }
            if (name.LocalName.Length == 0)
                throw new ArgumentException(SR.Xdom_Attr_Name);
            _name = name;
        }

        internal int LocalNameHash
        {
            get { return _name.HashCode; }
        }

        protected internal XmlAttribute(string prefix, string localName, string namespaceURI, XmlDocument doc)
        : this(doc.AddAttrXmlName(prefix, localName, namespaceURI), doc)
        {
        }

        internal XmlName XmlName
        {
            get { return _name; }
        }

        // Creates a duplicate of this node.
        public override XmlNode CloneNode(bool deep)
        {
            // CloneNode for attributes is deep irrespective of parameter 'deep' value     
            Debug.Assert(OwnerDocument != null);
            XmlDocument doc = OwnerDocument;
            XmlAttribute attr = doc.CreateAttribute(Prefix, LocalName, NamespaceURI);
            attr.CopyChildren(doc, this, true);
            return attr;
        }

        // Gets the parent of this node (for nodes that can have parents).
        public override XmlNode ParentNode
        {
            get { return null; }
        }

        // Gets the name of the node.
        public override String Name
        {
            get { return _name.Name; }
        }

        // Gets the name of the node without the namespace prefix.
        public override String LocalName
        {
            get { return _name.LocalName; }
        }

        // Gets the namespace URI of this node.
        public override String NamespaceURI
        {
            get { return _name.NamespaceURI; }
        }

        // Gets or sets the namespace prefix of this node.
        public override String Prefix
        {
            get { return _name.Prefix; }
            set { _name = _name.OwnerDocument.AddAttrXmlName(value, LocalName, NamespaceURI); }
        }

        // Gets the type of the current node.
        public override XmlNodeType NodeType
        {
            get { return XmlNodeType.Attribute; }
        }

        // Gets the XmlDocument that contains this node.
        public override XmlDocument OwnerDocument
        {
            get
            {
                return _name.OwnerDocument;
            }
        }

        // Gets or sets the value of the node.
        public override String Value
        {
            get { return InnerText; }
            set { InnerText = value; } //use InnerText which has perf optimization
        }

        public override String InnerText
        {
            set
            {
                if (PrepareOwnerElementInElementIdAttrMap())
                {
                    string innerText = base.InnerText;
                    base.InnerText = value;
                    ResetOwnerElementInElementIdAttrMap(innerText);
                }
                else
                {
                    base.InnerText = value;
                }
            }
        }

        // This function returns false because it is implication of removing schema.
        // If removed more methods would have to be removed as well and it would make adding schema back much harder. 
        internal bool PrepareOwnerElementInElementIdAttrMap()
        {
            return false;
        }

        internal void ResetOwnerElementInElementIdAttrMap(string oldInnerText)
        {
            XmlElement ownerElement = OwnerElement;
            if (ownerElement != null)
            {
                ownerElement.Attributes.ResetParentInElementIdAttrMap(oldInnerText, InnerText);
            }
        }

        internal override bool IsContainer
        {
            get { return true; }
        }

        //the function is provided only at Load time to speed up Load process
        internal override XmlNode AppendChildForLoad(XmlNode newChild, XmlDocument doc)
        {
            XmlNodeChangedEventArgs args = doc.GetInsertEventArgsForLoad(newChild, this);

            if (args != null)
                doc.BeforeEvent(args);

            XmlLinkedNode newNode = (XmlLinkedNode)newChild;

            if (_lastChild == null)
            { // if LastNode == null
                newNode.next = newNode;
                _lastChild = newNode;
                newNode.SetParentForLoad(this);
            }
            else
            {
                XmlLinkedNode refNode = _lastChild; // refNode = LastNode;
                newNode.next = refNode.next;
                refNode.next = newNode;
                _lastChild = newNode; // LastNode = newNode;
                if (refNode.IsText
                    && newNode.IsText)
                {
                    NestTextNodes(refNode, newNode);
                }
                else
                {
                    newNode.SetParentForLoad(this);
                }
            }

            if (args != null)
                doc.AfterEvent(args);

            return newNode;
        }

        internal override XmlLinkedNode LastNode
        {
            get { return _lastChild; }
            set { _lastChild = value; }
        }

        internal override bool IsValidChildType(XmlNodeType type)
        {
            return (type == XmlNodeType.Text) || (type == XmlNodeType.EntityReference);
        }

        // Gets a value indicating whether the value was explicitly set.
        public virtual bool Specified
        {
            get { return true; }
        }

        public override XmlNode InsertBefore(XmlNode newChild, XmlNode refChild)
        {
            XmlNode node;
            if (PrepareOwnerElementInElementIdAttrMap())
            {
                string innerText = InnerText;
                node = base.InsertBefore(newChild, refChild);
                ResetOwnerElementInElementIdAttrMap(innerText);
            }
            else
            {
                node = base.InsertBefore(newChild, refChild);
            }
            return node;
        }

        public override XmlNode InsertAfter(XmlNode newChild, XmlNode refChild)
        {
            XmlNode node;
            if (PrepareOwnerElementInElementIdAttrMap())
            {
                string innerText = InnerText;
                node = base.InsertAfter(newChild, refChild);
                ResetOwnerElementInElementIdAttrMap(innerText);
            }
            else
            {
                node = base.InsertAfter(newChild, refChild);
            }
            return node;
        }

        public override XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild)
        {
            XmlNode node;
            if (PrepareOwnerElementInElementIdAttrMap())
            {
                string innerText = InnerText;
                node = base.ReplaceChild(newChild, oldChild);
                ResetOwnerElementInElementIdAttrMap(innerText);
            }
            else
            {
                node = base.ReplaceChild(newChild, oldChild);
            }
            return node;
        }

        public override XmlNode RemoveChild(XmlNode oldChild)
        {
            XmlNode node;
            if (PrepareOwnerElementInElementIdAttrMap())
            {
                string innerText = InnerText;
                node = base.RemoveChild(oldChild);
                ResetOwnerElementInElementIdAttrMap(innerText);
            }
            else
            {
                node = base.RemoveChild(oldChild);
            }
            return node;
        }

        public override XmlNode PrependChild(XmlNode newChild)
        {
            XmlNode node;
            if (PrepareOwnerElementInElementIdAttrMap())
            {
                string innerText = InnerText;
                node = base.PrependChild(newChild);
                ResetOwnerElementInElementIdAttrMap(innerText);
            }
            else
            {
                node = base.PrependChild(newChild);
            }
            return node;
        }

        public override XmlNode AppendChild(XmlNode newChild)
        {
            XmlNode node;
            if (PrepareOwnerElementInElementIdAttrMap())
            {
                string innerText = InnerText;
                node = base.AppendChild(newChild);
                ResetOwnerElementInElementIdAttrMap(innerText);
            }
            else
            {
                node = base.AppendChild(newChild);
            }
            return node;
        }

        // DOM Level 2

        // Gets the XmlElement node that contains this attribute.
        public virtual XmlElement OwnerElement
        {
            get
            {
                return parentNode as XmlElement;
            }
        }

        // Gets or sets the markup representing just the children of this node.
        public override string InnerXml
        {
            set
            {
                RemoveAll();
                XmlLoader loader = new XmlLoader();
                loader.LoadInnerXmlAttribute(this, value);
            }
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w)
        {
            w.WriteStartAttribute(Prefix, LocalName, NamespaceURI);
            WriteContentTo(w);
            w.WriteEndAttribute();
        }

        // Saves all the children of the node to the specified XmlWriter.
        public override void WriteContentTo(XmlWriter w)
        {
            for (XmlNode node = FirstChild; node != null; node = node.NextSibling)
            {
                node.WriteTo(w);
            }
        }

        public override String BaseURI
        {
            get
            {
                if (OwnerElement != null)
                    return OwnerElement.BaseURI;
                return String.Empty;
            }
        }

        internal override void SetParent(XmlNode node)
        {
            this.parentNode = node;
        }

        internal override XmlSpace XmlSpace
        {
            get
            {
                if (OwnerElement != null)
                    return OwnerElement.XmlSpace;
                return XmlSpace.None;
            }
        }

        internal override String XmlLang
        {
            get
            {
                if (OwnerElement != null)
                    return OwnerElement.XmlLang;
                return String.Empty;
            }
        }
    }
}
