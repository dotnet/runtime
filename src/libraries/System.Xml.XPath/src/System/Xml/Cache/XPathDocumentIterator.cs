// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml.XPath;

namespace MS.Internal.Xml.Cache
{
    /// <summary>
    /// Base internal class of all XPathDocument XPathNodeIterator implementations.
    /// </summary>
    internal abstract class XPathDocumentBaseIterator : XPathNodeIterator
    {
        protected XPathDocumentNavigator ctxt;
        protected int pos;

        /// <summary>
        /// Create a new iterator that is initially positioned on the "ctxt" node.
        /// </summary>
        protected XPathDocumentBaseIterator(XPathDocumentNavigator ctxt)
        {
            this.ctxt = new XPathDocumentNavigator(ctxt);
        }

        /// <summary>
        /// Create a new iterator that is a copy of "iter".
        /// </summary>
        protected XPathDocumentBaseIterator(XPathDocumentBaseIterator iter)
        {
            this.ctxt = new XPathDocumentNavigator(iter.ctxt);
            this.pos = iter.pos;
        }

        /// <summary>
        /// Return the current navigator.
        /// </summary>
        public override XPathNavigator Current
        {
            get { return this.ctxt; }
        }

        /// <summary>
        /// Return the iterator's current position.
        /// </summary>
        public override int CurrentPosition
        {
            get { return this.pos; }
        }
    }


    /// <summary>
    /// Iterate over all element children with a particular QName.
    /// </summary>
    internal class XPathDocumentElementChildIterator : XPathDocumentBaseIterator
    {
        private string localName, namespaceUri;

        /// <summary>
        /// Create an iterator that ranges over all element children of "parent" having the specified QName.
        /// </summary>
        public XPathDocumentElementChildIterator(XPathDocumentNavigator parent, string name, string namespaceURI) : base(parent)
        {
            if (namespaceURI == null) throw new ArgumentNullException("namespaceURI");

            this.localName = parent.NameTable.Get(name);
            this.namespaceUri = namespaceURI;
        }

        /// <summary>
        /// Create a new iterator that is a copy of "iter".
        /// </summary>
        public XPathDocumentElementChildIterator(XPathDocumentElementChildIterator iter) : base(iter)
        {
            this.localName = iter.localName;
            this.namespaceUri = iter.namespaceUri;
        }

        /// <summary>
        /// Create a copy of this iterator.
        /// </summary>
        public override XPathNodeIterator Clone()
        {
            return new XPathDocumentElementChildIterator(this);
        }

        /// <summary>
        /// Position the iterator to the next matching child.
        /// </summary>
        public override bool MoveNext()
        {
            if (this.pos == 0)
            {
                if (!this.ctxt.MoveToChild(this.localName, this.namespaceUri))
                    return false;
            }
            else
            {
                if (!this.ctxt.MoveToNext(this.localName, this.namespaceUri))
                    return false;
            }

            this.pos++;
            return true;
        }
    }


    /// <summary>
    /// Iterate over all content children with a particular XPathNodeType.
    /// </summary>
    internal class XPathDocumentKindChildIterator : XPathDocumentBaseIterator
    {
        private XPathNodeType typ;

        /// <summary>
        /// Create an iterator that ranges over all content children of "parent" having the specified XPathNodeType.
        /// </summary>
        public XPathDocumentKindChildIterator(XPathDocumentNavigator parent, XPathNodeType typ) : base(parent)
        {
            this.typ = typ;
        }

        /// <summary>
        /// Create a new iterator that is a copy of "iter".
        /// </summary>
        public XPathDocumentKindChildIterator(XPathDocumentKindChildIterator iter) : base(iter)
        {
            this.typ = iter.typ;
        }

        /// <summary>
        /// Create a copy of this iterator.
        /// </summary>
        public override XPathNodeIterator Clone()
        {
            return new XPathDocumentKindChildIterator(this);
        }

        /// <summary>
        /// Position the iterator to the next descendant.
        /// </summary>
        public override bool MoveNext()
        {
            if (this.pos == 0)
            {
                if (!this.ctxt.MoveToChild(this.typ))
                    return false;
            }
            else
            {
                if (!this.ctxt.MoveToNext(this.typ))
                    return false;
            }

            this.pos++;
            return true;
        }
    }


    /// <summary>
    /// Iterate over all element descendants with a particular QName.
    /// </summary>
    internal class XPathDocumentElementDescendantIterator : XPathDocumentBaseIterator
    {
        private XPathDocumentNavigator end;
        private string localName, namespaceUri;
        private bool matchSelf;

        /// <summary>
        /// Create an iterator that ranges over all element descendants of "root" having the specified QName.
        /// </summary>
        public XPathDocumentElementDescendantIterator(XPathDocumentNavigator root, string name, string namespaceURI, bool matchSelf) : base(root)
        {
            if (namespaceURI == null) throw new ArgumentNullException("namespaceURI");

            this.localName = root.NameTable.Get(name);
            this.namespaceUri = namespaceURI;
            this.matchSelf = matchSelf;

            // Find the next non-descendant node that follows "root" in document order
            if (root.NodeType != XPathNodeType.Root)
            {
                this.end = new XPathDocumentNavigator(root);
                this.end.MoveToNonDescendant();
            }
        }

        /// <summary>
        /// Create a new iterator that is a copy of "iter".
        /// </summary>
        public XPathDocumentElementDescendantIterator(XPathDocumentElementDescendantIterator iter) : base(iter)
        {
            this.end = iter.end;
            this.localName = iter.localName;
            this.namespaceUri = iter.namespaceUri;
            this.matchSelf = iter.matchSelf;
        }

        /// <summary>
        /// Create a copy of this iterator.
        /// </summary>
        public override XPathNodeIterator Clone()
        {
            return new XPathDocumentElementDescendantIterator(this);
        }

        /// <summary>
        /// Position the iterator to the next descendant.
        /// </summary>
        public override bool MoveNext()
        {
            if (this.matchSelf)
            {
                this.matchSelf = false;

                if (this.ctxt.IsElementMatch(this.localName, this.namespaceUri))
                {
                    this.pos++;
                    return true;
                }
            }

            if (!this.ctxt.MoveToFollowing(this.localName, this.namespaceUri, this.end))
                return false;

            this.pos++;
            return true;
        }
    }


    /// <summary>
    /// Iterate over all content descendants with a particular XPathNodeType.
    /// </summary>
    internal class XPathDocumentKindDescendantIterator : XPathDocumentBaseIterator
    {
        private XPathDocumentNavigator end;
        private XPathNodeType typ;
        private bool matchSelf;

        /// <summary>
        /// Create an iterator that ranges over all content descendants of "root" having the specified XPathNodeType.
        /// </summary>
        public XPathDocumentKindDescendantIterator(XPathDocumentNavigator root, XPathNodeType typ, bool matchSelf) : base(root)
        {
            this.typ = typ;
            this.matchSelf = matchSelf;

            // Find the next non-descendant node that follows "root" in document order
            if (root.NodeType != XPathNodeType.Root)
            {
                this.end = new XPathDocumentNavigator(root);
                this.end.MoveToNonDescendant();
            }
        }

        /// <summary>
        /// Create a new iterator that is a copy of "iter".
        /// </summary>
        public XPathDocumentKindDescendantIterator(XPathDocumentKindDescendantIterator iter) : base(iter)
        {
            this.end = iter.end;
            this.typ = iter.typ;
            this.matchSelf = iter.matchSelf;
        }

        /// <summary>
        /// Create a copy of this iterator.
        /// </summary>
        public override XPathNodeIterator Clone()
        {
            return new XPathDocumentKindDescendantIterator(this);
        }

        /// <summary>
        /// Position the iterator to the next descendant.
        /// </summary>
        public override bool MoveNext()
        {
            if (this.matchSelf)
            {
                this.matchSelf = false;

                if (this.ctxt.IsKindMatch(this.typ))
                {
                    this.pos++;
                    return true;
                }
            }

            if (!this.ctxt.MoveToFollowing(this.typ, this.end))
                return false;

            this.pos++;
            return true;
        }
    }
}

