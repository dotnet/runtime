// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Xml.XPath;

namespace System.Xml
{
    // Used to quote or escape blocks of text to keep that text from being
    // interpreted as markup language.
    public class XmlCDataSection : XmlCharacterData
    {
        protected internal XmlCDataSection(string data, XmlDocument doc) : base(data, doc)
        {
        }

        // Gets the name of the node.
        public override string Name
        {
            get
            {
                return OwnerDocument.strCDataSectionName;
            }
        }

        // Gets the name of the node without the namespace prefix.
        public override string LocalName
        {
            get
            {
                return OwnerDocument.strCDataSectionName;
            }
        }

        // Gets the type of the current node.
        public override XmlNodeType NodeType
        {
            get
            {
                return XmlNodeType.CDATA;
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
            return OwnerDocument.CreateCDataSection(Data);
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w)
        {
            w.WriteCData(Data);
        }

        // Saves the node to the specified XmlWriter.
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
