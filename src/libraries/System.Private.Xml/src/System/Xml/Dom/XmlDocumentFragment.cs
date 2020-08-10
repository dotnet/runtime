// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Xml.XPath;

namespace System.Xml
{
    // Represents a lightweight object that is useful for tree insert
    // operations.
    // <code>DocumentFragment</code> is a "lightweight" or "minimal"
    // <code>Document</code> object. It is very common to want to be able to
    // extract a portion of a document's tree or to create a new fragment of a
    // document. Imagine implementing a user command like cut or rearranging a
    // document by moving fragments around. It is desirable to have an object
    // which can hold such fragments and it is quite natural to use a Node for
    // this purpose. While it is true that a <code>Document</code> object could
    // fulfil this role,  a <code>Document</code> object can potentially be a
    // heavyweight  object, depending on the underlying implementation. What is
    // really needed for this is a very lightweight object.
    // <code>DocumentFragment</code> is such an object.
    // <p>Furthermore, various operations -- such as inserting nodes as children
    // of another <code>Node</code> -- may take <code>DocumentFragment</code>
    // objects as arguments;  this results in all the child nodes of the
    // <code>DocumentFragment</code>  being moved to the child list of this node.
    // <p>The children of a <code>DocumentFragment</code> node are zero or more
    // nodes representing the tops of any sub-trees defining the structure of the
    // document. <code>DocumentFragment</code> nodes do not need to be
    // well-formed XML documents (although they do need to follow the rules
    // imposed upon well-formed XML parsed entities, which can have multiple top
    // nodes).  For example, a <code>DocumentFragment</code> might have only one
    // child and that child node could be a <code>Text</code> node. Such a
    // structure model  represents neither an HTML document nor a well-formed XML
    // document.
    // <p>When a <code>DocumentFragment</code> is inserted into a
    // <code>Document</code> (or indeed any other <code>Node</code> that may take
    // children) the children of the <code>DocumentFragment</code> and not the
    // <code>DocumentFragment</code>  itself are inserted into the
    // <code>Node</code>. This makes the <code>DocumentFragment</code> very
    // useful when the user wishes to create nodes that are siblings; the
    // <code>DocumentFragment</code> acts as the parent of these nodes so that the
    // user can use the standard methods from the <code>Node</code>  interface,
    // such as <code>insertBefore()</code> and  <code>appendChild()</code>.
    public class XmlDocumentFragment : XmlNode
    {
        private XmlLinkedNode? _lastChild;

        protected internal XmlDocumentFragment(XmlDocument ownerDocument) : base()
        {
            if (ownerDocument == null)
                throw new ArgumentException(SR.Xdom_Node_Null_Doc);

            parentNode = ownerDocument;
        }

        // Gets the name of the node.
        public override string Name
        {
            get { return OwnerDocument!.strDocumentFragmentName; }
        }

        // Gets the name of the current node without the namespace prefix.
        public override string LocalName
        {
            get { return OwnerDocument!.strDocumentFragmentName; }
        }

        // Gets the type of the current node.
        public override XmlNodeType NodeType
        {
            get { return XmlNodeType.DocumentFragment; }
        }

        // Gets the parent of this node (for nodes that can have parents).
        public override XmlNode? ParentNode
        {
            get { return null; }
        }

        // Gets the XmlDocument that contains this node.
        public override XmlDocument OwnerDocument
        {
            get
            {
                return (XmlDocument)parentNode!;
            }
        }

        // Gets or sets the markup representing just
        // the children of this node.
        public override string InnerXml
        {
            get
            {
                return base.InnerXml;
            }
            set
            {
                RemoveAll();
                XmlLoader loader = new XmlLoader();
                //Hack that the content is the same element
                loader.ParsePartialContent(this, value, XmlNodeType.Element);
            }
        }

        // Creates a duplicate of this node.
        public override XmlNode CloneNode(bool deep)
        {
            Debug.Assert(OwnerDocument != null);
            XmlDocument doc = OwnerDocument;
            XmlDocumentFragment clone = doc.CreateDocumentFragment();
            if (deep)
                clone.CopyChildren(doc, this, deep);
            return clone;
        }

        internal override bool IsContainer
        {
            get { return true; }
        }

        internal override XmlLinkedNode? LastNode
        {
            get { return _lastChild; }
            set { _lastChild = value; }
        }

        internal override bool IsValidChildType(XmlNodeType type)
        {
            switch (type)
            {
                case XmlNodeType.Element:
                case XmlNodeType.Text:
                case XmlNodeType.EntityReference:
                case XmlNodeType.Comment:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.CDATA:
                    return true;

                case XmlNodeType.XmlDeclaration:
                    //if there is an XmlDeclaration node, it has to be the first node;
                    XmlNode? firstNode = FirstChild;
                    if (firstNode == null || firstNode.NodeType != XmlNodeType.XmlDeclaration)
                        return true;
                    else
                        return false; //not allowed to insert a second XmlDeclaration node
                default:
                    return false;
            }
        }
        internal override bool CanInsertAfter(XmlNode newChild, XmlNode? refChild)
        {
            Debug.Assert(newChild != null); //should be checked that newChild is not null before this function call
            if (newChild.NodeType == XmlNodeType.XmlDeclaration)
            {
                if (refChild == null)
                {
                    //append at the end
                    return (LastNode == null);
                }
                else
                    return false;
            }
            return true;
        }

        internal override bool CanInsertBefore(XmlNode newChild, XmlNode? refChild)
        {
            Debug.Assert(newChild != null); //should be checked that newChild is not null before this function call
            if (newChild.NodeType == XmlNodeType.XmlDeclaration)
            {
                return (refChild == null || refChild == FirstChild);
            }
            return true;
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w)
        {
            WriteContentTo(w);
        }

        // Saves all the children of the node to the specified XmlWriter.
        public override void WriteContentTo(XmlWriter w)
        {
            foreach (XmlNode n in this)
            {
                n.WriteTo(w);
            }
        }

        internal override XPathNodeType XPNodeType { get { return XPathNodeType.Root; } }
    }
}
