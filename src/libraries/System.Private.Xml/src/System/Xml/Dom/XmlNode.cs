// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System.Xml.Schema;
using System.Xml.XPath;
using MS.Internal.Xml.XPath;
using System.Globalization;

namespace System.Xml
{
    // Represents a single node in the document.
    [DebuggerDisplay("{debuggerDisplayProxy}")]
    public abstract class XmlNode : ICloneable, IEnumerable, IXPathNavigable
    {
        internal XmlNode parentNode; //this pointer is reused to save the userdata information, need to prevent internal user access the pointer directly.

        internal XmlNode()
        {
        }

        internal XmlNode(XmlDocument doc)
        {
            if (doc == null)
                throw new ArgumentException(SR.Xdom_Node_Null_Doc);
            this.parentNode = doc;
        }

        public virtual XPathNavigator CreateNavigator()
        {
            XmlDocument thisAsDoc = this as XmlDocument;
            if (thisAsDoc != null)
            {
                return thisAsDoc.CreateNavigator(this);
            }
            XmlDocument doc = OwnerDocument;
            Debug.Assert(doc != null);
            return doc.CreateNavigator(this);
        }

        // Selects the first node that matches the xpath expression
        public XmlNode SelectSingleNode(string xpath)
        {
            XmlNodeList list = SelectNodes(xpath);
            // SelectNodes returns null for certain node types
            return list != null ? list[0] : null;
        }

        // Selects the first node that matches the xpath expression and given namespace context.
        public XmlNode SelectSingleNode(string xpath, XmlNamespaceManager nsmgr)
        {
            XPathNavigator xn = (this).CreateNavigator();
            //if the method is called on node types like DocType, Entity, XmlDeclaration,
            //the navigator returned is null. So just return null from here for those node types.
            if (xn == null)
                return null;
            XPathExpression exp = xn.Compile(xpath);
            exp.SetContext(nsmgr);
            return new XPathNodeList(xn.Select(exp))[0];
        }

        // Selects all nodes that match the xpath expression
        public XmlNodeList SelectNodes(string xpath)
        {
            XPathNavigator n = (this).CreateNavigator();
            //if the method is called on node types like DocType, Entity, XmlDeclaration,
            //the navigator returned is null. So just return null from here for those node types.
            if (n == null)
                return null;
            return new XPathNodeList(n.Select(xpath));
        }

        // Selects all nodes that match the xpath expression and given namespace context.
        public XmlNodeList SelectNodes(string xpath, XmlNamespaceManager nsmgr)
        {
            XPathNavigator xn = (this).CreateNavigator();
            //if the method is called on node types like DocType, Entity, XmlDeclaration,
            //the navigator returned is null. So just return null from here for those node types.
            if (xn == null)
                return null;
            XPathExpression exp = xn.Compile(xpath);
            exp.SetContext(nsmgr);
            return new XPathNodeList(xn.Select(exp));
        }

        // Gets the name of the node.
        public abstract string Name
        {
            get;
        }

        // Gets or sets the value of the node.
        public virtual string Value
        {
            get { return null; }
            set { throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.Xdom_Node_SetVal, NodeType.ToString())); }
        }

        // Gets the type of the current node.
        public abstract XmlNodeType NodeType
        {
            get;
        }

        // Gets the parent of this node (for nodes that can have parents).
        public virtual XmlNode ParentNode
        {
            get
            {
                Debug.Assert(parentNode != null);

                if (parentNode.NodeType != XmlNodeType.Document)
                {
                    return parentNode;
                }

                // Linear lookup through the children of the document
                XmlLinkedNode firstChild = parentNode.FirstChild as XmlLinkedNode;
                if (firstChild != null)
                {
                    XmlLinkedNode node = firstChild;
                    do
                    {
                        if (node == this)
                        {
                            return parentNode;
                        }
                        node = node.next;
                    }
                    while (node != null
                           && node != firstChild);
                }
                return null;
            }
        }

        // Gets all children of this node.
        public virtual XmlNodeList ChildNodes
        {
            get { return new XmlChildNodes(this); }
        }

        // Gets the node immediately preceding this node.
        public virtual XmlNode PreviousSibling
        {
            get { return null; }
        }

        // Gets the node immediately following this node.
        public virtual XmlNode NextSibling
        {
            get { return null; }
        }

        // Gets a XmlAttributeCollection containing the attributes
        // of this node.
        public virtual XmlAttributeCollection Attributes
        {
            get { return null; }
        }

        // Gets the XmlDocument that contains this node.
        public virtual XmlDocument OwnerDocument
        {
            get
            {
                Debug.Assert(parentNode != null);
                if (parentNode.NodeType == XmlNodeType.Document)
                    return (XmlDocument)parentNode;
                return parentNode.OwnerDocument;
            }
        }

        // Gets the first child of this node.
        public virtual XmlNode FirstChild
        {
            get
            {
                XmlLinkedNode linkedNode = LastNode;
                if (linkedNode != null)
                    return linkedNode.next;

                return null;
            }
        }

        // Gets the last child of this node.
        public virtual XmlNode LastChild
        {
            get { return LastNode; }
        }

        internal virtual bool IsContainer
        {
            get { return false; }
        }

        internal virtual XmlLinkedNode LastNode
        {
            get { return null; }
            set { }
        }

        internal bool AncestorNode(XmlNode node)
        {
            XmlNode n = this.ParentNode;

            while (n != null && n != this)
            {
                if (n == node)
                    return true;
                n = n.ParentNode;
            }

            return false;
        }

        //trace to the top to find out its parent node.
        internal bool IsConnected()
        {
            XmlNode parent = ParentNode;
            while (parent != null && !(parent.NodeType == XmlNodeType.Document))
                parent = parent.ParentNode;
            return parent != null;
        }

        // Inserts the specified node immediately before the specified reference node.
        public virtual XmlNode InsertBefore(XmlNode newChild, XmlNode refChild)
        {
            if (this == newChild || AncestorNode(newChild))
                throw new ArgumentException(SR.Xdom_Node_Insert_Child);

            if (refChild == null)
                return AppendChild(newChild);

            if (!IsContainer)
                throw new InvalidOperationException(SR.Xdom_Node_Insert_Contain);

            if (refChild.ParentNode != this)
                throw new ArgumentException(SR.Xdom_Node_Insert_Path);

            if (newChild == refChild)
                return newChild;

            XmlDocument childDoc = newChild.OwnerDocument;
            XmlDocument thisDoc = OwnerDocument;
            if (childDoc != null && childDoc != thisDoc && childDoc != this)
                throw new ArgumentException(SR.Xdom_Node_Insert_Context);

            if (!CanInsertBefore(newChild, refChild))
                throw new InvalidOperationException(SR.Xdom_Node_Insert_Location);

            if (newChild.ParentNode != null)
                newChild.ParentNode.RemoveChild(newChild);

            // special case for doc-fragment.
            if (newChild.NodeType == XmlNodeType.DocumentFragment)
            {
                XmlNode first = newChild.FirstChild;
                XmlNode node = first;
                if (node != null)
                {
                    newChild.RemoveChild(node);
                    InsertBefore(node, refChild);
                    // insert the rest of the children after this one.
                    InsertAfter(newChild, node);
                }
                return first;
            }

            if (!(newChild is XmlLinkedNode) || !IsValidChildType(newChild.NodeType))
                throw new InvalidOperationException(SR.Xdom_Node_Insert_TypeConflict);

            XmlLinkedNode newNode = (XmlLinkedNode)newChild;
            XmlLinkedNode refNode = (XmlLinkedNode)refChild;

            string newChildValue = newChild.Value;
            XmlNodeChangedEventArgs args = GetEventArgs(newChild, newChild.ParentNode, this, newChildValue, newChildValue, XmlNodeChangedAction.Insert);

            if (args != null)
                BeforeEvent(args);

            if (refNode == FirstChild)
            {
                newNode.next = refNode;
                LastNode.next = newNode;
                newNode.SetParent(this);

                if (newNode.IsText)
                {
                    if (refNode.IsText)
                    {
                        NestTextNodes(newNode, refNode);
                    }
                }
            }
            else
            {
                XmlLinkedNode prevNode = (XmlLinkedNode)refNode.PreviousSibling;

                newNode.next = refNode;
                prevNode.next = newNode;
                newNode.SetParent(this);

                if (prevNode.IsText)
                {
                    if (newNode.IsText)
                    {
                        NestTextNodes(prevNode, newNode);
                        if (refNode.IsText)
                        {
                            NestTextNodes(newNode, refNode);
                        }
                    }
                    else
                    {
                        if (refNode.IsText)
                        {
                            UnnestTextNodes(prevNode, refNode);
                        }
                    }
                }
                else
                {
                    if (newNode.IsText)
                    {
                        if (refNode.IsText)
                        {
                            NestTextNodes(newNode, refNode);
                        }
                    }
                }
            }

            if (args != null)
                AfterEvent(args);

            return newNode;
        }

        // Inserts the specified node immediately after the specified reference node.
        public virtual XmlNode InsertAfter(XmlNode newChild, XmlNode refChild)
        {
            if (this == newChild || AncestorNode(newChild))
                throw new ArgumentException(SR.Xdom_Node_Insert_Child);

            if (refChild == null)
                return PrependChild(newChild);

            if (!IsContainer)
                throw new InvalidOperationException(SR.Xdom_Node_Insert_Contain);

            if (refChild.ParentNode != this)
                throw new ArgumentException(SR.Xdom_Node_Insert_Path);

            if (newChild == refChild)
                return newChild;

            XmlDocument childDoc = newChild.OwnerDocument;
            XmlDocument thisDoc = OwnerDocument;
            if (childDoc != null && childDoc != thisDoc && childDoc != this)
                throw new ArgumentException(SR.Xdom_Node_Insert_Context);

            if (!CanInsertAfter(newChild, refChild))
                throw new InvalidOperationException(SR.Xdom_Node_Insert_Location);

            if (newChild.ParentNode != null)
                newChild.ParentNode.RemoveChild(newChild);

            // special case for doc-fragment.
            if (newChild.NodeType == XmlNodeType.DocumentFragment)
            {
                XmlNode last = refChild;
                XmlNode first = newChild.FirstChild;
                XmlNode node = first;
                while (node != null)
                {
                    XmlNode next = node.NextSibling;
                    newChild.RemoveChild(node);
                    InsertAfter(node, last);
                    last = node;
                    node = next;
                }
                return first;
            }

            if (!(newChild is XmlLinkedNode) || !IsValidChildType(newChild.NodeType))
                throw new InvalidOperationException(SR.Xdom_Node_Insert_TypeConflict);

            XmlLinkedNode newNode = (XmlLinkedNode)newChild;
            XmlLinkedNode refNode = (XmlLinkedNode)refChild;

            string newChildValue = newChild.Value;
            XmlNodeChangedEventArgs args = GetEventArgs(newChild, newChild.ParentNode, this, newChildValue, newChildValue, XmlNodeChangedAction.Insert);

            if (args != null)
                BeforeEvent(args);

            if (refNode == LastNode)
            {
                newNode.next = refNode.next;
                refNode.next = newNode;
                LastNode = newNode;
                newNode.SetParent(this);

                if (refNode.IsText)
                {
                    if (newNode.IsText)
                    {
                        NestTextNodes(refNode, newNode);
                    }
                }
            }
            else
            {
                XmlLinkedNode nextNode = refNode.next;

                newNode.next = nextNode;
                refNode.next = newNode;
                newNode.SetParent(this);

                if (refNode.IsText)
                {
                    if (newNode.IsText)
                    {
                        NestTextNodes(refNode, newNode);
                        if (nextNode.IsText)
                        {
                            NestTextNodes(newNode, nextNode);
                        }
                    }
                    else
                    {
                        if (nextNode.IsText)
                        {
                            UnnestTextNodes(refNode, nextNode);
                        }
                    }
                }
                else
                {
                    if (newNode.IsText)
                    {
                        if (nextNode.IsText)
                        {
                            NestTextNodes(newNode, nextNode);
                        }
                    }
                }
            }


            if (args != null)
                AfterEvent(args);

            return newNode;
        }

        // Replaces the child node oldChild with newChild node.
        public virtual XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild)
        {
            XmlNode nextNode = oldChild.NextSibling;
            RemoveChild(oldChild);
            XmlNode node = InsertBefore(newChild, nextNode);
            return oldChild;
        }

        // Removes specified child node.
        public virtual XmlNode RemoveChild(XmlNode oldChild)
        {
            if (!IsContainer)
                throw new InvalidOperationException(SR.Xdom_Node_Remove_Contain);

            if (oldChild.ParentNode != this)
                throw new ArgumentException(SR.Xdom_Node_Remove_Child);

            XmlLinkedNode oldNode = (XmlLinkedNode)oldChild;

            string oldNodeValue = oldNode.Value;
            XmlNodeChangedEventArgs args = GetEventArgs(oldNode, this, null, oldNodeValue, oldNodeValue, XmlNodeChangedAction.Remove);

            if (args != null)
                BeforeEvent(args);

            XmlLinkedNode lastNode = LastNode;

            if (oldNode == FirstChild)
            {
                if (oldNode == lastNode)
                {
                    LastNode = null;
                    oldNode.next = null;
                    oldNode.SetParent(null);
                }
                else
                {
                    XmlLinkedNode nextNode = oldNode.next;

                    if (nextNode.IsText)
                    {
                        if (oldNode.IsText)
                        {
                            UnnestTextNodes(oldNode, nextNode);
                        }
                    }

                    lastNode.next = nextNode;
                    oldNode.next = null;
                    oldNode.SetParent(null);
                }
            }
            else
            {
                if (oldNode == lastNode)
                {
                    XmlLinkedNode prevNode = (XmlLinkedNode)oldNode.PreviousSibling;
                    prevNode.next = oldNode.next;
                    LastNode = prevNode;
                    oldNode.next = null;
                    oldNode.SetParent(null);
                }
                else
                {
                    XmlLinkedNode prevNode = (XmlLinkedNode)oldNode.PreviousSibling;
                    XmlLinkedNode nextNode = oldNode.next;

                    if (nextNode.IsText)
                    {
                        if (prevNode.IsText)
                        {
                            NestTextNodes(prevNode, nextNode);
                        }
                        else
                        {
                            if (oldNode.IsText)
                            {
                                UnnestTextNodes(oldNode, nextNode);
                            }
                        }
                    }

                    prevNode.next = nextNode;
                    oldNode.next = null;
                    oldNode.SetParent(null);
                }
            }

            if (args != null)
                AfterEvent(args);

            return oldChild;
        }

        // Adds the specified node to the beginning of the list of children of this node.
        public virtual XmlNode PrependChild(XmlNode newChild)
        {
            return InsertBefore(newChild, FirstChild);
        }

        // Adds the specified node to the end of the list of children of this node.
        public virtual XmlNode AppendChild(XmlNode newChild)
        {
            XmlDocument thisDoc = OwnerDocument;
            if (thisDoc == null)
            {
                thisDoc = this as XmlDocument;
            }
            if (!IsContainer)
                throw new InvalidOperationException(SR.Xdom_Node_Insert_Contain);

            if (this == newChild || AncestorNode(newChild))
                throw new ArgumentException(SR.Xdom_Node_Insert_Child);

            if (newChild.ParentNode != null)
                newChild.ParentNode.RemoveChild(newChild);

            XmlDocument childDoc = newChild.OwnerDocument;
            if (childDoc != null && childDoc != thisDoc && childDoc != this)
                throw new ArgumentException(SR.Xdom_Node_Insert_Context);

            // special case for doc-fragment.
            if (newChild.NodeType == XmlNodeType.DocumentFragment)
            {
                XmlNode first = newChild.FirstChild;
                XmlNode node = first;
                while (node != null)
                {
                    XmlNode next = node.NextSibling;
                    newChild.RemoveChild(node);
                    AppendChild(node);
                    node = next;
                }
                return first;
            }

            if (!(newChild is XmlLinkedNode) || !IsValidChildType(newChild.NodeType))
                throw new InvalidOperationException(SR.Xdom_Node_Insert_TypeConflict);


            if (!CanInsertAfter(newChild, LastChild))
                throw new InvalidOperationException(SR.Xdom_Node_Insert_Location);

            string newChildValue = newChild.Value;
            XmlNodeChangedEventArgs args = GetEventArgs(newChild, newChild.ParentNode, this, newChildValue, newChildValue, XmlNodeChangedAction.Insert);

            if (args != null)
                BeforeEvent(args);

            XmlLinkedNode refNode = LastNode;
            XmlLinkedNode newNode = (XmlLinkedNode)newChild;

            if (refNode == null)
            {
                newNode.next = newNode;
                LastNode = newNode;
                newNode.SetParent(this);
            }
            else
            {
                newNode.next = refNode.next;
                refNode.next = newNode;
                LastNode = newNode;
                newNode.SetParent(this);

                if (refNode.IsText)
                {
                    if (newNode.IsText)
                    {
                        NestTextNodes(refNode, newNode);
                    }
                }
            }

            if (args != null)
                AfterEvent(args);

            return newNode;
        }

        //the function is provided only at Load time to speed up Load process
        internal virtual XmlNode AppendChildForLoad(XmlNode newChild, XmlDocument doc)
        {
            XmlNodeChangedEventArgs args = doc.GetInsertEventArgsForLoad(newChild, this);

            if (args != null)
                doc.BeforeEvent(args);

            XmlLinkedNode refNode = LastNode;
            XmlLinkedNode newNode = (XmlLinkedNode)newChild;

            if (refNode == null)
            {
                newNode.next = newNode;
                LastNode = newNode;
                newNode.SetParentForLoad(this);
            }
            else
            {
                newNode.next = refNode.next;
                refNode.next = newNode;
                LastNode = newNode;
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

        internal virtual bool IsValidChildType(XmlNodeType type)
        {
            return false;
        }

        internal virtual bool CanInsertBefore(XmlNode newChild, XmlNode refChild)
        {
            return true;
        }

        internal virtual bool CanInsertAfter(XmlNode newChild, XmlNode refChild)
        {
            return true;
        }

        // Gets a value indicating whether this node has any child nodes.
        public virtual bool HasChildNodes
        {
            get { return LastNode != null; }
        }

        // Creates a duplicate of this node.
        public abstract XmlNode CloneNode(bool deep);

        internal virtual void CopyChildren(XmlDocument doc, XmlNode container, bool deep)
        {
            for (XmlNode child = container.FirstChild; child != null; child = child.NextSibling)
            {
                AppendChildForLoad(child.CloneNode(deep), doc);
            }
        }

        // DOM Level 2

        // Puts all XmlText nodes in the full depth of the sub-tree
        // underneath this XmlNode into a "normal" form where only
        // markup (e.g., tags, comments, processing instructions, CDATA sections,
        // and entity references) separates XmlText nodes, that is, there
        // are no adjacent XmlText nodes.
        public virtual void Normalize()
        {
            XmlNode firstChildTextLikeNode = null;
            StringBuilder sb = StringBuilderCache.Acquire();
            for (XmlNode crtChild = this.FirstChild; crtChild != null;)
            {
                XmlNode nextChild = crtChild.NextSibling;
                switch (crtChild.NodeType)
                {
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        {
                            sb.Append(crtChild.Value);
                            XmlNode winner = NormalizeWinner(firstChildTextLikeNode, crtChild);
                            if (winner == firstChildTextLikeNode)
                            {
                                this.RemoveChild(crtChild);
                            }
                            else
                            {
                                if (firstChildTextLikeNode != null)
                                    this.RemoveChild(firstChildTextLikeNode);
                                firstChildTextLikeNode = crtChild;
                            }
                            break;
                        }
                    case XmlNodeType.Element:
                        {
                            crtChild.Normalize();
                            goto default;
                        }
                    default:
                        {
                            if (firstChildTextLikeNode != null)
                            {
                                firstChildTextLikeNode.Value = sb.ToString();
                                firstChildTextLikeNode = null;
                            }
                            sb.Remove(0, sb.Length);
                            break;
                        }
                }
                crtChild = nextChild;
            }
            if (firstChildTextLikeNode != null && sb.Length > 0)
                firstChildTextLikeNode.Value = sb.ToString();

            StringBuilderCache.Release(sb);
        }

        private XmlNode NormalizeWinner(XmlNode firstNode, XmlNode secondNode)
        {
            //first node has the priority
            if (firstNode == null)
                return secondNode;
            Debug.Assert(firstNode.NodeType == XmlNodeType.Text
                        || firstNode.NodeType == XmlNodeType.SignificantWhitespace
                        || firstNode.NodeType == XmlNodeType.Whitespace
                        || secondNode.NodeType == XmlNodeType.Text
                        || secondNode.NodeType == XmlNodeType.SignificantWhitespace
                        || secondNode.NodeType == XmlNodeType.Whitespace);
            if (firstNode.NodeType == XmlNodeType.Text)
                return firstNode;
            if (secondNode.NodeType == XmlNodeType.Text)
                return secondNode;
            if (firstNode.NodeType == XmlNodeType.SignificantWhitespace)
                return firstNode;
            if (secondNode.NodeType == XmlNodeType.SignificantWhitespace)
                return secondNode;
            if (firstNode.NodeType == XmlNodeType.Whitespace)
                return firstNode;
            if (secondNode.NodeType == XmlNodeType.Whitespace)
                return secondNode;
            Debug.Assert(true, "shouldn't have fall through here.");
            return null;
        }

        // Test if the DOM implementation implements a specific feature.
        public virtual bool Supports(string feature, string version)
        {
            if (String.Equals("XML", feature, StringComparison.OrdinalIgnoreCase))
            {
                if (version == null || version == "1.0" || version == "2.0")
                    return true;
            }
            return false;
        }

        // Gets the namespace URI of this node.
        public virtual string NamespaceURI
        {
            get { return string.Empty; }
        }

        // Gets or sets the namespace prefix of this node.
        public virtual string Prefix
        {
            get { return string.Empty; }
            set { }
        }

        // Gets the name of the node without the namespace prefix.
        public abstract string LocalName
        {
            get;
        }

        // Microsoft extensions

        // Gets a value indicating whether the node is read-only.
        public virtual bool IsReadOnly
        {
            get
            {
                XmlDocument doc = OwnerDocument;
                return HasReadOnlyParent(this);
            }
        }

        internal static bool HasReadOnlyParent(XmlNode n)
        {
            while (n != null)
            {
                switch (n.NodeType)
                {
                    case XmlNodeType.EntityReference:
                    case XmlNodeType.Entity:
                        return true;

                    case XmlNodeType.Attribute:
                        n = ((XmlAttribute)n).OwnerElement;
                        break;

                    default:
                        n = n.ParentNode;
                        break;
                }
            }
            return false;
        }

        // Creates a duplicate of this node.
        public virtual XmlNode Clone()
        {
            return this.CloneNode(true);
        }

        object ICloneable.Clone()
        {
            return this.CloneNode(true);
        }

        // Provides a simple ForEach-style iteration over the
        // collection of nodes in this XmlNamedNodeMap.
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new XmlChildEnumerator(this);
        }

        public IEnumerator GetEnumerator()
        {
            return new XmlChildEnumerator(this);
        }

        private void AppendChildText(StringBuilder builder)
        {
            for (XmlNode child = FirstChild; child != null; child = child.NextSibling)
            {
                if (child.FirstChild == null)
                {
                    if (child.NodeType == XmlNodeType.Text || child.NodeType == XmlNodeType.CDATA
                        || child.NodeType == XmlNodeType.Whitespace || child.NodeType == XmlNodeType.SignificantWhitespace)
                        builder.Append(child.InnerText);
                }
                else
                {
                    child.AppendChildText(builder);
                }
            }
        }

        // Gets or sets the concatenated values of the node and
        // all its children.
        public virtual string InnerText
        {
            get
            {
                XmlNode fc = FirstChild;
                if (fc == null)
                {
                    return string.Empty;
                }
                if (fc.NextSibling == null)
                {
                    XmlNodeType nodeType = fc.NodeType;
                    switch (nodeType)
                    {
                        case XmlNodeType.Text:
                        case XmlNodeType.CDATA:
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                            return fc.Value;
                    }
                }
                StringBuilder builder = StringBuilderCache.Acquire();
                AppendChildText(builder);
                return StringBuilderCache.GetStringAndRelease(builder);
            }

            set
            {
                XmlNode firstChild = FirstChild;
                if (firstChild != null  //there is one child
                    && firstChild.NextSibling == null // and exactly one
                    && firstChild.NodeType == XmlNodeType.Text)//which is a text node
                {
                    //this branch is for perf reason and event fired when TextNode.Value is changed
                    firstChild.Value = value;
                }
                else
                {
                    RemoveAll();
                    AppendChild(OwnerDocument.CreateTextNode(value));
                }
            }
        }

        // Gets the markup representing this node and all its children.
        public virtual string OuterXml
        {
            get
            {
                StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
                XmlDOMTextWriter xw = new XmlDOMTextWriter(sw);
                try
                {
                    WriteTo(xw);
                }
                finally
                {
                    xw.Close();
                }
                return sw.ToString();
            }
        }

        // Gets or sets the markup representing just the children of this node.
        public virtual string InnerXml
        {
            get
            {
                StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
                XmlDOMTextWriter xw = new XmlDOMTextWriter(sw);
                try
                {
                    WriteContentTo(xw);
                }
                finally
                {
                    xw.Close();
                }
                return sw.ToString();
            }

            set
            {
                throw new InvalidOperationException(SR.Xdom_Set_InnerXml);
            }
        }

        public virtual IXmlSchemaInfo SchemaInfo
        {
            get
            {
                return XmlDocument.NotKnownSchemaInfo;
            }
        }

        public virtual String BaseURI
        {
            get
            {
                XmlNode curNode = this.ParentNode; //save one while loop since if going to here, the nodetype of this node can't be document, entity and entityref
                while (curNode != null)
                {
                    XmlNodeType nt = curNode.NodeType;
                    //EntityReference's children come from the dtd where they are defined.
                    //we need to investigate the same thing for entity's children if they are defined in an external dtd file.
                    if (nt == XmlNodeType.EntityReference)
                        return ((XmlEntityReference)curNode).ChildBaseURI;
                    if (nt == XmlNodeType.Document
                        || nt == XmlNodeType.Entity
                        || nt == XmlNodeType.Attribute)
                        return curNode.BaseURI;
                    curNode = curNode.ParentNode;
                }
                return String.Empty;
            }
        }

        // Saves the current node to the specified XmlWriter.
        public abstract void WriteTo(XmlWriter w);

        // Saves all the children of the node to the specified XmlWriter.
        public abstract void WriteContentTo(XmlWriter w);

        // Removes all the children and/or attributes
        // of the current node.
        public virtual void RemoveAll()
        {
            XmlNode child = FirstChild;
            XmlNode sibling = null;

            while (child != null)
            {
                sibling = child.NextSibling;
                RemoveChild(child);
                child = sibling;
            }
        }

        internal XmlDocument Document
        {
            get
            {
                if (NodeType == XmlNodeType.Document)
                    return (XmlDocument)this;
                return OwnerDocument;
            }
        }

        // Looks up the closest xmlns declaration for the given
        // prefix that is in scope for the current node and returns
        // the namespace URI in the declaration.
        public virtual string GetNamespaceOfPrefix(string prefix)
        {
            string namespaceName = GetNamespaceOfPrefixStrict(prefix);
            return namespaceName != null ? namespaceName : string.Empty;
        }

        internal string GetNamespaceOfPrefixStrict(string prefix)
        {
            XmlDocument doc = Document;
            if (doc != null)
            {
                prefix = doc.NameTable.Get(prefix);
                if (prefix == null)
                    return null;

                XmlNode node = this;
                while (node != null)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        XmlElement elem = (XmlElement)node;
                        if (elem.HasAttributes)
                        {
                            XmlAttributeCollection attrs = elem.Attributes;
                            if (prefix.Length == 0)
                            {
                                for (int iAttr = 0; iAttr < attrs.Count; iAttr++)
                                {
                                    XmlAttribute attr = attrs[iAttr];
                                    if (attr.Prefix.Length == 0)
                                    {
                                        if (Ref.Equal(attr.LocalName, doc.strXmlns))
                                        {
                                            return attr.Value; // found xmlns
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (int iAttr = 0; iAttr < attrs.Count; iAttr++)
                                {
                                    XmlAttribute attr = attrs[iAttr];
                                    if (Ref.Equal(attr.Prefix, doc.strXmlns))
                                    {
                                        if (Ref.Equal(attr.LocalName, prefix))
                                        {
                                            return attr.Value; // found xmlns:prefix
                                        }
                                    }
                                    else if (Ref.Equal(attr.Prefix, prefix))
                                    {
                                        return attr.NamespaceURI; // found prefix:attr
                                    }
                                }
                            }
                        }
                        if (Ref.Equal(node.Prefix, prefix))
                        {
                            return node.NamespaceURI;
                        }
                        node = node.ParentNode;
                    }
                    else if (node.NodeType == XmlNodeType.Attribute)
                    {
                        node = ((XmlAttribute)node).OwnerElement;
                    }
                    else
                    {
                        node = node.ParentNode;
                    }
                }
                if (Ref.Equal(doc.strXml, prefix))
                { // xmlns:xml
                    return doc.strReservedXml;
                }
                else if (Ref.Equal(doc.strXmlns, prefix))
                { // xmlns:xmlns
                    return doc.strReservedXmlns;
                }
            }
            return null;
        }

        // Looks up the closest xmlns declaration for the given namespace
        // URI that is in scope for the current node and returns
        // the prefix defined in that declaration.
        public virtual string GetPrefixOfNamespace(string namespaceURI)
        {
            string prefix = GetPrefixOfNamespaceStrict(namespaceURI);
            return prefix != null ? prefix : string.Empty;
        }

        internal string GetPrefixOfNamespaceStrict(string namespaceURI)
        {
            XmlDocument doc = Document;
            if (doc != null)
            {
                namespaceURI = doc.NameTable.Add(namespaceURI);

                XmlNode node = this;
                while (node != null)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        XmlElement elem = (XmlElement)node;
                        if (elem.HasAttributes)
                        {
                            XmlAttributeCollection attrs = elem.Attributes;
                            for (int iAttr = 0; iAttr < attrs.Count; iAttr++)
                            {
                                XmlAttribute attr = attrs[iAttr];
                                if (attr.Prefix.Length == 0)
                                {
                                    if (Ref.Equal(attr.LocalName, doc.strXmlns))
                                    {
                                        if (attr.Value == namespaceURI)
                                        {
                                            return string.Empty; // found xmlns="namespaceURI"
                                        }
                                    }
                                }
                                else if (Ref.Equal(attr.Prefix, doc.strXmlns))
                                {
                                    if (attr.Value == namespaceURI)
                                    {
                                        return attr.LocalName; // found xmlns:prefix="namespaceURI"
                                    }
                                }
                                else if (Ref.Equal(attr.NamespaceURI, namespaceURI))
                                {
                                    return attr.Prefix; // found prefix:attr
                                                        // with prefix bound to namespaceURI
                                }
                            }
                        }
                        if (Ref.Equal(node.NamespaceURI, namespaceURI))
                        {
                            return node.Prefix;
                        }
                        node = node.ParentNode;
                    }
                    else if (node.NodeType == XmlNodeType.Attribute)
                    {
                        node = ((XmlAttribute)node).OwnerElement;
                    }
                    else
                    {
                        node = node.ParentNode;
                    }
                }
                if (Ref.Equal(doc.strReservedXml, namespaceURI))
                { // xmlns:xml
                    return doc.strXml;
                }
                else if (Ref.Equal(doc.strReservedXmlns, namespaceURI))
                { // xmlns:xmlns
                    return doc.strXmlns;
                }
            }
            return null;
        }

        // Retrieves the first child element with the specified name.
        public virtual XmlElement this[string name]
        {
            get
            {
                for (XmlNode n = FirstChild; n != null; n = n.NextSibling)
                {
                    if (n.NodeType == XmlNodeType.Element && n.Name == name)
                        return (XmlElement)n;
                }
                return null;
            }
        }

        // Retrieves the first child element with the specified LocalName and
        // NamespaceURI.
        public virtual XmlElement this[string localname, string ns]
        {
            get
            {
                for (XmlNode n = FirstChild; n != null; n = n.NextSibling)
                {
                    if (n.NodeType == XmlNodeType.Element && n.LocalName == localname && n.NamespaceURI == ns)
                        return (XmlElement)n;
                }
                return null;
            }
        }

        internal virtual void SetParent(XmlNode node)
        {
            if (node == null)
            {
                this.parentNode = OwnerDocument;
            }
            else
            {
                this.parentNode = node;
            }
        }

        internal virtual void SetParentForLoad(XmlNode node)
        {
            this.parentNode = node;
        }

        internal static void SplitName(string name, out string prefix, out string localName)
        {
            int colonPos = name.IndexOf(':'); // ordinal compare
            if (-1 == colonPos || 0 == colonPos || name.Length - 1 == colonPos)
            {
                prefix = string.Empty;
                localName = name;
            }
            else
            {
                prefix = name.Substring(0, colonPos);
                localName = name.Substring(colonPos + 1);
            }
        }

        internal virtual XmlNode FindChild(XmlNodeType type)
        {
            for (XmlNode child = FirstChild; child != null; child = child.NextSibling)
            {
                if (child.NodeType == type)
                {
                    return child;
                }
            }
            return null;
        }

        internal virtual XmlNodeChangedEventArgs GetEventArgs(XmlNode node, XmlNode oldParent, XmlNode newParent, string oldValue, string newValue, XmlNodeChangedAction action)
        {
            XmlDocument doc = OwnerDocument;
            if (doc != null)
            {
                if (!doc.IsLoading)
                {
                    if (((newParent != null && newParent.IsReadOnly) || (oldParent != null && oldParent.IsReadOnly)))
                        throw new InvalidOperationException(SR.Xdom_Node_Modify_ReadOnly);
                }
                return doc.GetEventArgs(node, oldParent, newParent, oldValue, newValue, action);
            }
            return null;
        }

        internal virtual void BeforeEvent(XmlNodeChangedEventArgs args)
        {
            if (args != null)
                OwnerDocument.BeforeEvent(args);
        }

        internal virtual void AfterEvent(XmlNodeChangedEventArgs args)
        {
            if (args != null)
                OwnerDocument.AfterEvent(args);
        }

        internal virtual XmlSpace XmlSpace
        {
            get
            {
                XmlNode node = this;
                XmlElement elem = null;
                do
                {
                    elem = node as XmlElement;
                    if (elem != null && elem.HasAttribute("xml:space"))
                    {
                        switch (XmlConvert.TrimString(elem.GetAttribute("xml:space")))
                        {
                            case "default":
                                return XmlSpace.Default;
                            case "preserve":
                                return XmlSpace.Preserve;
                            default:
                                //should we throw exception if value is otherwise?
                                break;
                        }
                    }
                    node = node.ParentNode;
                }
                while (node != null);
                return XmlSpace.None;
            }
        }

        internal virtual String XmlLang
        {
            get
            {
                XmlNode node = this;
                XmlElement elem = null;
                do
                {
                    elem = node as XmlElement;
                    if (elem != null)
                    {
                        if (elem.HasAttribute("xml:lang"))
                            return elem.GetAttribute("xml:lang");
                    }
                    node = node.ParentNode;
                } while (node != null);
                return String.Empty;
            }
        }

        internal virtual XPathNodeType XPNodeType
        {
            get
            {
                return (XPathNodeType)(-1);
            }
        }

        internal virtual string XPLocalName
        {
            get
            {
                return string.Empty;
            }
        }

        internal virtual string GetXPAttribute(string localName, string namespaceURI)
        {
            return String.Empty;
        }

        internal virtual bool IsText
        {
            get
            {
                return false;
            }
        }

        public virtual XmlNode PreviousText
        {
            get
            {
                return null;
            }
        }

        internal static void NestTextNodes(XmlNode prevNode, XmlNode nextNode)
        {
            Debug.Assert(prevNode.IsText);
            Debug.Assert(nextNode.IsText);

            nextNode.parentNode = prevNode;
        }

        internal static void UnnestTextNodes(XmlNode prevNode, XmlNode nextNode)
        {
            Debug.Assert(prevNode.IsText);
            Debug.Assert(nextNode.IsText);

            nextNode.parentNode = prevNode.ParentNode;
        }

        private object debuggerDisplayProxy { get { return new DebuggerDisplayXmlNodeProxy(this); } }

        [DebuggerDisplay("{ToString()}")]
        internal readonly struct DebuggerDisplayXmlNodeProxy
        {
            private readonly XmlNode _node;

            public DebuggerDisplayXmlNodeProxy(XmlNode node)
            {
                _node = node;
            }

            public override string ToString()
            {
                XmlNodeType nodeType = _node.NodeType;
                string result = nodeType.ToString();
                switch (nodeType)
                {
                    case XmlNodeType.Element:
                    case XmlNodeType.EntityReference:
                        result += ", Name=\"" + _node.Name + "\"";
                        break;
                    case XmlNodeType.Attribute:
                    case XmlNodeType.ProcessingInstruction:
                        result += ", Name=\"" + _node.Name + "\", Value=\"" + XmlConvert.EscapeValueForDebuggerDisplay(_node.Value) + "\"";
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Comment:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.XmlDeclaration:
                        result += ", Value=\"" + XmlConvert.EscapeValueForDebuggerDisplay(_node.Value) + "\"";
                        break;
                    case XmlNodeType.DocumentType:
                        XmlDocumentType documentType = (XmlDocumentType)_node;
                        result += ", Name=\"" + documentType.Name + "\", SYSTEM=\"" + documentType.SystemId + "\", PUBLIC=\"" + documentType.PublicId + "\", Value=\"" + XmlConvert.EscapeValueForDebuggerDisplay(documentType.InternalSubset) + "\"";
                        break;
                    default:
                        break;
                }
                return result;
            }
        }
    }
}
