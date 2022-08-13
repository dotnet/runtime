// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    // Represents a collection of attributes that can be accessed by name or index.
    public sealed class XmlAttributeCollection : XmlNamedNodeMap, ICollection
    {
        internal XmlAttributeCollection(XmlNode parent) : base(parent)
        {
        }

        // Gets the attribute with the specified index.
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public XmlAttribute this[int i]
        {
            get
            {
                try
                {
                    return (XmlAttribute)nodes[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new IndexOutOfRangeException(SR.Xdom_IndexOutOfRange);
                }
            }
        }

        // Gets the attribute with the specified name.
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public XmlAttribute? this[string name]
        {
            get
            {
                int hash = XmlName.GetHashCode(name);

                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlAttribute node = (XmlAttribute)nodes[i];

                    if (hash == node.LocalNameHash
                        && name == node.Name)
                    {
                        return node;
                    }
                }

                return null;
            }
        }

        // Gets the attribute with the specified LocalName and NamespaceUri.
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public XmlAttribute? this[string localName, string? namespaceURI]
        {
            get
            {
                int hash = XmlName.GetHashCode(localName);

                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlAttribute node = (XmlAttribute)nodes[i];

                    if (hash == node.LocalNameHash
                        && localName == node.LocalName
                        && namespaceURI == node.NamespaceURI)
                    {
                        return node;
                    }
                }

                return null;
            }
        }

        internal int FindNodeOffset(XmlAttribute node)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlAttribute tmp = (XmlAttribute)nodes[i];

                if (tmp.LocalNameHash == node.LocalNameHash
                    && tmp.Name == node.Name
                    && tmp.NamespaceURI == node.NamespaceURI)
                {
                    return i;
                }
            }

            return -1;
        }

        internal int FindNodeOffsetNS(XmlAttribute node)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlAttribute tmp = (XmlAttribute)nodes[i];
                if (tmp.LocalNameHash == node.LocalNameHash
                    && tmp.LocalName == node.LocalName
                    && tmp.NamespaceURI == node.NamespaceURI)
                {
                    return i;
                }
            }
            return -1;
        }

        // Adds a XmlNode using its Name property
        [return: NotNullIfNotNull(nameof(node))]
        public override XmlNode? SetNamedItem(XmlNode? node)
        {
            if (node == null)
                return null;

            if (!(node is XmlAttribute))
                throw new ArgumentException(SR.Xdom_AttrCol_Object);

            int offset = FindNodeOffset(node.LocalName, node.NamespaceURI);
            if (offset == -1)
            {
                return InternalAppendAttribute((XmlAttribute)node);
            }
            else
            {
                XmlNode oldNode = base.RemoveNodeAt(offset);
                InsertNodeAt(offset, node);
                return oldNode;
            }
        }

        // Inserts the specified node as the first node in the collection.
        public XmlAttribute Prepend(XmlAttribute node)
        {
            if (node.OwnerDocument != null && node.OwnerDocument != parent.OwnerDocument)
                throw new ArgumentException(SR.Xdom_NamedNode_Context);

            if (node.OwnerElement != null)
                Detach(node);

            RemoveDuplicateAttribute(node);

            InsertNodeAt(0, node);
            return node;
        }

        // Inserts the specified node as the last node in the collection.
        public XmlAttribute Append(XmlAttribute node)
        {
            XmlDocument doc = node.OwnerDocument;
            if (doc == null || doc.IsLoading == false)
            {
                if (doc != null && doc != parent.OwnerDocument)
                {
                    throw new ArgumentException(SR.Xdom_NamedNode_Context);
                }
                if (node.OwnerElement != null)
                {
                    Detach(node);
                }
                AddNode(node);
            }
            else
            {
                base.AddNodeForLoad(node, doc);
                InsertParentIntoElementIdAttrMap(node);
            }
            return node;
        }

        // Inserts the specified attribute immediately before the specified reference attribute.
        public XmlAttribute InsertBefore(XmlAttribute newNode, XmlAttribute? refNode)
        {
            if (newNode == refNode)
                return newNode;

            if (refNode == null)
                return Append(newNode);

            if (refNode.OwnerElement != parent)
                throw new ArgumentException(SR.Xdom_AttrCol_Insert);

            if (newNode.OwnerDocument != null && newNode.OwnerDocument != parent.OwnerDocument)
                throw new ArgumentException(SR.Xdom_NamedNode_Context);

            if (newNode.OwnerElement != null)
                Detach(newNode);

            int offset = FindNodeOffset(refNode.LocalName, refNode.NamespaceURI);
            Debug.Assert(offset != -1); // the if statement above guarantees that the ref node is in the collection

            int dupoff = RemoveDuplicateAttribute(newNode);
            if (dupoff >= 0 && dupoff < offset)
                offset--;
            InsertNodeAt(offset, newNode);

            return newNode;
        }

        // Inserts the specified attribute immediately after the specified reference attribute.
        public XmlAttribute InsertAfter(XmlAttribute newNode, XmlAttribute? refNode)
        {
            if (newNode == refNode)
                return newNode;

            if (refNode == null)
                return Prepend(newNode);

            if (refNode.OwnerElement != parent)
                throw new ArgumentException(SR.Xdom_AttrCol_Insert);

            if (newNode.OwnerDocument != null && newNode.OwnerDocument != parent.OwnerDocument)
                throw new ArgumentException(SR.Xdom_NamedNode_Context);

            if (newNode.OwnerElement != null)
                Detach(newNode);

            int offset = FindNodeOffset(refNode.LocalName, refNode.NamespaceURI);
            Debug.Assert(offset != -1); // the if statement above guarantees that the ref node is in the collection

            int dupoff = RemoveDuplicateAttribute(newNode);
            if (dupoff >= 0 && dupoff <= offset)
                offset--;
            InsertNodeAt(offset + 1, newNode);

            return newNode;
        }

        // Removes the specified attribute node from the map.
        public XmlAttribute? Remove(XmlAttribute? node)
        {
            int cNodes = nodes.Count;
            for (int offset = 0; offset < cNodes; offset++)
            {
                if (nodes[offset] == node)
                {
                    RemoveNodeAt(offset);
                    return node;
                }
            }

            return null;
        }

        // Removes the attribute node with the specified index from the map.
        public XmlAttribute? RemoveAt(int i)
        {
            if (i < 0 || i >= Count)
                return null;

            return (XmlAttribute)RemoveNodeAt(i);
        }

        // Removes all attributes from the map.
        public void RemoveAll()
        {
            int n = Count;
            while (n > 0)
            {
                n--;
                RemoveAt(n);
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            for (int i = 0, max = Count; i < max; i++, index++)
            {
                array.SetValue(nodes[i], index);
            }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return this; }
        }

        int ICollection.Count
        {
            get { return base.Count; }
        }

        public void CopyTo(XmlAttribute[] array, int index)
        {
            for (int i = 0, max = Count; i < max; i++, index++)
                array[index] = (XmlAttribute)(((XmlNode)nodes[i]).CloneNode(true));
        }

        internal override XmlNode AddNode(XmlNode node)
        {
            //should be sure by now that the node doesn't have the same name with an existing node in the collection
            RemoveDuplicateAttribute((XmlAttribute)node);
            XmlNode retNode = base.AddNode(node);
            Debug.Assert(retNode is XmlAttribute);
            InsertParentIntoElementIdAttrMap((XmlAttribute)node);
            return retNode;
        }

        internal override XmlNode InsertNodeAt(int i, XmlNode node)
        {
            XmlNode retNode = base.InsertNodeAt(i, node);
            InsertParentIntoElementIdAttrMap((XmlAttribute)node);
            return retNode;
        }

        internal override XmlNode RemoveNodeAt(int i)
        {
            //remove the node without checking replacement
            XmlNode retNode = base.RemoveNodeAt(i);
            Debug.Assert(retNode is XmlAttribute);
            RemoveParentFromElementIdAttrMap((XmlAttribute)retNode);
            // after remove the attribute, we need to check if a default attribute node should be created and inserted into the tree
            XmlAttribute? defattr = parent.OwnerDocument!.GetDefaultAttribute((XmlElement)parent, retNode.Prefix, retNode.LocalName, retNode.NamespaceURI);
            if (defattr != null)
                InsertNodeAt(i, defattr);

            return retNode;
        }

        internal static void Detach(XmlAttribute attr)
        {
            attr.OwnerElement!.Attributes.Remove(attr);
        }

        //insert the parent element node into the map
        internal void InsertParentIntoElementIdAttrMap(XmlAttribute attr)
        {
            XmlElement? parentElem = parent as XmlElement;
            if (parentElem != null)
            {
                if (parent.OwnerDocument == null)
                    return;

                XmlName? attrname = parent.OwnerDocument.GetIDInfoByElement(parentElem.XmlName);
                if (attrname != null && attrname.Prefix == attr.XmlName.Prefix && attrname.LocalName == attr.XmlName.LocalName)
                {
                    parent.OwnerDocument.AddElementWithId(attr.Value, parentElem); //add the element into the hashtable
                }
            }
        }

        //remove the parent element node from the map when the ID attribute is removed
        internal void RemoveParentFromElementIdAttrMap(XmlAttribute attr)
        {
            XmlElement? parentElem = parent as XmlElement;
            if (parentElem != null)
            {
                if (parent.OwnerDocument == null)
                    return;

                XmlName? attrname = parent.OwnerDocument.GetIDInfoByElement(parentElem.XmlName);
                if (attrname != null && attrname.Prefix == attr.XmlName.Prefix && attrname.LocalName == attr.XmlName.LocalName)
                {
                    parent.OwnerDocument.RemoveElementWithId(attr.Value, parentElem); //remove the element from the hashtable
                }
            }
        }

        //the function checks if there is already node with the same name existing in the collection
        // if so, remove it because the new one will be inserted to replace this one (could be in different position though )
        //  by the calling function later
        internal int RemoveDuplicateAttribute(XmlAttribute attr)
        {
            int ind = FindNodeOffset(attr.LocalName, attr.NamespaceURI);
            if (ind != -1)
            {
                XmlAttribute at = (XmlAttribute)nodes[ind];
                base.RemoveNodeAt(ind);
                RemoveParentFromElementIdAttrMap(at);
            }
            return ind;
        }

        internal bool PrepareParentInElementIdAttrMap(string attrPrefix, string attrLocalName)
        {
            XmlElement? parentElem = parent as XmlElement;
            Debug.Assert(parentElem != null);
            XmlDocument? doc = parent.OwnerDocument;
            Debug.Assert(doc != null);
            //The returned attrname if not null is the name with namespaceURI being set to string.Empty
            //Because DTD doesn't support namespaceURI so all comparisons are based on no namespaceURI (string.Empty);
            XmlName? attrname = doc.GetIDInfoByElement(parentElem.XmlName);
            if (attrname != null && attrname.Prefix == attrPrefix && attrname.LocalName == attrLocalName)
            {
                return true;
            }

            return false;
        }

        internal void ResetParentInElementIdAttrMap(string oldVal, string newVal)
        {
            XmlElement? parentElem = parent as XmlElement;
            Debug.Assert(parentElem != null);
            XmlDocument? doc = parent.OwnerDocument;
            Debug.Assert(doc != null);
            doc.RemoveElementWithId(oldVal, parentElem); //add the element into the hashtable
            doc.AddElementWithId(newVal, parentElem);
        }

        // WARNING:
        //  For performance reasons, this function does not check
        //  for xml attributes within the collection with the same full name.
        //  This means that any caller of this function must be sure that
        //  a duplicate attribute does not exist.
        internal XmlAttribute InternalAppendAttribute(XmlAttribute node)
        {
            // a duplicate node better not exist
            Debug.Assert(-1 == FindNodeOffset(node));

            XmlNode retNode = base.AddNode(node);
            Debug.Assert(retNode is XmlAttribute);
            InsertParentIntoElementIdAttrMap((XmlAttribute)node);
            return (XmlAttribute)retNode;
        }
    }
}
