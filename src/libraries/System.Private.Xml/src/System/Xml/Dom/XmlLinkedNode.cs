// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    // Gets the node immediately preceding or following this node.
    public abstract class XmlLinkedNode : XmlNode
    {
        internal XmlLinkedNode next;

        internal XmlLinkedNode(XmlDocument doc) : base(doc)
        {
            next = null;
        }

        // Gets the node immediately preceding this node.
        public override XmlNode PreviousSibling
        {
            get
            {
                XmlNode parent = ParentNode;
                if (parent != null)
                {
                    XmlNode node = parent.FirstChild;
                    while (node != null)
                    {
                        XmlNode nextSibling = node.NextSibling;
                        if (nextSibling == this)
                        {
                            break;
                        }
                        node = nextSibling;
                    }
                    return node;
                }
                return null;
            }
        }

        // Gets the node immediately following this node.
        public override XmlNode NextSibling
        {
            get
            {
                XmlNode parent = ParentNode;
                if (parent != null)
                {
                    if (next != parent.FirstChild)
                        return next;
                }
                return null;
            }
        }
    }
}
