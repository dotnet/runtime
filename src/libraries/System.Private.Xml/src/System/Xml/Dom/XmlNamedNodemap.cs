// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Xml
{
    // Represents a collection of nodes that can be accessed by name or index.
    public partial class XmlNamedNodeMap : IEnumerable
    {
        internal XmlNode parent;
        internal SmallXmlNodeList nodes;

        internal XmlNamedNodeMap(XmlNode parent)
        {
            this.parent = parent;
        }

        // Retrieves a XmlNode specified by name.
        public virtual XmlNode GetNamedItem(String name)
        {
            int offset = FindNodeOffset(name);
            if (offset >= 0)
                return (XmlNode)nodes[offset];
            return null;
        }

        // Adds a XmlNode using its Name property
        public virtual XmlNode SetNamedItem(XmlNode node)
        {
            if (node == null)
                return null;

            int offset = FindNodeOffset(node.LocalName, node.NamespaceURI);
            if (offset == -1)
            {
                AddNode(node);
                return null;
            }
            else
            {
                return ReplaceNodeAt(offset, node);
            }
        }

        // Removes the node specified by name.
        public virtual XmlNode RemoveNamedItem(String name)
        {
            int offset = FindNodeOffset(name);
            if (offset >= 0)
            {
                return RemoveNodeAt(offset);
            }
            return null;
        }

        // Gets the number of nodes in this XmlNamedNodeMap.
        public virtual int Count
        {
            get
            {
                return nodes.Count;
            }
        }

        // Retrieves the node at the specified index in this XmlNamedNodeMap.
        public virtual XmlNode Item(int index)
        {
            if (index < 0 || index >= nodes.Count)
                return null;
            try
            {
                return (XmlNode)nodes[index];
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new IndexOutOfRangeException(SR.Xdom_IndexOutOfRange);
            }
        }

        //
        // DOM Level 2
        //

        // Retrieves a node specified by LocalName and NamespaceURI.
        public virtual XmlNode GetNamedItem(String localName, String namespaceURI)
        {
            int offset = FindNodeOffset(localName, namespaceURI);
            if (offset >= 0)
                return (XmlNode)nodes[offset];
            return null;
        }

        // Removes a node specified by local name and namespace URI.
        public virtual XmlNode RemoveNamedItem(String localName, String namespaceURI)
        {
            int offset = FindNodeOffset(localName, namespaceURI);
            if (offset >= 0)
            {
                return RemoveNodeAt(offset);
            }
            return null;
        }

        public virtual IEnumerator GetEnumerator()
        {
            return nodes.GetEnumerator();
        }

        internal int FindNodeOffset(string name)
        {
            int c = this.Count;
            for (int i = 0; i < c; i++)
            {
                XmlNode node = (XmlNode)nodes[i];

                if (name == node.Name)
                    return i;
            }

            return -1;
        }

        internal int FindNodeOffset(string localName, string namespaceURI)
        {
            int c = this.Count;
            for (int i = 0; i < c; i++)
            {
                XmlNode node = (XmlNode)nodes[i];

                if (node.LocalName == localName && node.NamespaceURI == namespaceURI)
                    return i;
            }

            return -1;
        }

        internal virtual XmlNode AddNode(XmlNode node)
        {
            XmlNode oldParent;
            if (node.NodeType == XmlNodeType.Attribute)
                oldParent = ((XmlAttribute)node).OwnerElement;
            else
                oldParent = node.ParentNode;
            string nodeValue = node.Value;
            XmlNodeChangedEventArgs args = parent.GetEventArgs(node, oldParent, parent, nodeValue, nodeValue, XmlNodeChangedAction.Insert);

            if (args != null)
                parent.BeforeEvent(args);

            nodes.Add(node);
            node.SetParent(parent);

            if (args != null)
                parent.AfterEvent(args);

            return node;
        }

        internal virtual XmlNode AddNodeForLoad(XmlNode node, XmlDocument doc)
        {
            XmlNodeChangedEventArgs args = doc.GetInsertEventArgsForLoad(node, parent);
            if (args != null)
            {
                doc.BeforeEvent(args);
            }
            nodes.Add(node);
            node.SetParent(parent);
            if (args != null)
            {
                doc.AfterEvent(args);
            }
            return node;
        }

        internal virtual XmlNode RemoveNodeAt(int i)
        {
            XmlNode oldNode = (XmlNode)nodes[i];

            string oldNodeValue = oldNode.Value;
            XmlNodeChangedEventArgs args = parent.GetEventArgs(oldNode, parent, null, oldNodeValue, oldNodeValue, XmlNodeChangedAction.Remove);

            if (args != null)
                parent.BeforeEvent(args);

            nodes.RemoveAt(i);
            oldNode.SetParent(null);

            if (args != null)
                parent.AfterEvent(args);

            return oldNode;
        }

        internal XmlNode ReplaceNodeAt(int i, XmlNode node)
        {
            XmlNode oldNode = RemoveNodeAt(i);
            InsertNodeAt(i, node);
            return oldNode;
        }

        internal virtual XmlNode InsertNodeAt(int i, XmlNode node)
        {
            XmlNode oldParent;
            if (node.NodeType == XmlNodeType.Attribute)
                oldParent = ((XmlAttribute)node).OwnerElement;
            else
                oldParent = node.ParentNode;

            string nodeValue = node.Value;
            XmlNodeChangedEventArgs args = parent.GetEventArgs(node, oldParent, parent, nodeValue, nodeValue, XmlNodeChangedAction.Insert);

            if (args != null)
                parent.BeforeEvent(args);

            nodes.Insert(i, node);
            node.SetParent(parent);

            if (args != null)
                parent.AfterEvent(args);

            return node;
        }
    }
}
