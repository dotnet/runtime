// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Xml.XPath;

namespace System.Xml
{
    // Represents the text content of an element or attribute.
    public class XmlText : XmlCharacterData
    {
        internal XmlText(string strData) : this(strData, null)
        {
        }

        protected internal XmlText(string strData, XmlDocument doc) : base(strData, doc)
        {
        }

        // Gets the name of the node.
        public override String Name
        {
            get
            {
                return OwnerDocument.strTextName;
            }
        }

        // Gets the name of the current node without the namespace prefix.
        public override String LocalName
        {
            get
            {
                return OwnerDocument.strTextName;
            }
        }

        // Gets the type of the current node.
        public override XmlNodeType NodeType
        {
            get
            {
                return XmlNodeType.Text;
            }
        }

        public override XmlNode ParentNode
        {
            get
            {
                switch (parentNode.NodeType)
                {
                    case XmlNodeType.Document:
                        return null;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        XmlNode parent = parentNode.parentNode;
                        while (parent.IsText)
                        {
                            parent = parent.parentNode;
                        }
                        return parent;
                    default:
                        return parentNode;
                }
            }
        }

        // Creates a duplicate of this node.
        public override XmlNode CloneNode(bool deep)
        {
            Debug.Assert(OwnerDocument != null);
            return OwnerDocument.CreateTextNode(Data);
        }

        public override String Value
        {
            get
            {
                return Data;
            }

            set
            {
                Data = value;
                XmlNode parent = parentNode;
                if (parent != null && parent.NodeType == XmlNodeType.Attribute)
                {
                    XmlUnspecifiedAttribute attr = parent as XmlUnspecifiedAttribute;
                    if (attr != null && !attr.Specified)
                    {
                        attr.SetSpecified(true);
                    }
                }
            }
        }

        // Splits the node into two nodes at the specified offset, keeping
        // both in the tree as siblings.
        public virtual XmlText SplitText(int offset)
        {
            XmlNode parentNode = this.ParentNode;
            int length = this.Length;
            if (offset > length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            //if the text node is out of the living tree, throw exception.
            if (parentNode == null)
                throw new InvalidOperationException(SR.Xdom_TextNode_SplitText);

            int count = length - offset;
            String splitData = Substring(offset, count);
            DeleteData(offset, count);
            XmlText newTextNode = OwnerDocument.CreateTextNode(splitData);
            parentNode.InsertAfter(newTextNode, this);
            return newTextNode;
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w)
        {
            w.WriteString(Data);
        }

        // Saves all the children of the node to the specified XmlWriter.
        public override void WriteContentTo(XmlWriter w)
        {
            // Intentionally do nothing
        }

        internal override XPathNodeType XPNodeType
        {
            get
            {
                return XPathNodeType.Text;
            }
        }

        internal override bool IsText
        {
            get
            {
                return true;
            }
        }

        public override XmlNode PreviousText
        {
            get
            {
                if (parentNode.IsText)
                {
                    return parentNode;
                }
                return null;
            }
        }
    }
}
