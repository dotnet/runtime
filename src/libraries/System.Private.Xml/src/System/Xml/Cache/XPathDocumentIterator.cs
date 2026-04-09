// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal sealed class XPathDocumentElementChildIterator : XPathDocumentBaseIterator
    {
        private readonly string? _localName;
        private readonly string _namespaceUri;

        /// <summary>
        /// Create an iterator that ranges over all element children of "parent" having the specified QName.
        /// </summary>
        public XPathDocumentElementChildIterator(XPathDocumentNavigator parent, string name, string namespaceURI) : base(parent)
        {
            ArgumentNullException.ThrowIfNull(namespaceURI);

            _localName = parent.NameTable.Get(name);
            _namespaceUri = namespaceURI;
        }

        /// <summary>
        /// Create a new iterator that is a copy of "iter".
        /// </summary>
        public XPathDocumentElementChildIterator(XPathDocumentElementChildIterator iter) : base(iter)
        {
            _localName = iter._localName;
            _namespaceUri = iter._namespaceUri;
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
                if (!this.ctxt.MoveToChild(_localName, _namespaceUri))
                    return false;
            }
            else
            {
                if (!this.ctxt.MoveToNext(_localName, _namespaceUri))
                    return false;
            }

            this.pos++;
            return true;
        }
    }


    /// <summary>
    /// Iterate over all content children with a particular XPathNodeType.
    /// </summary>
    internal sealed class XPathDocumentKindChildIterator : XPathDocumentBaseIterator
    {
        private readonly XPathNodeType _typ;

        /// <summary>
        /// Create an iterator that ranges over all content children of "parent" having the specified XPathNodeType.
        /// </summary>
        public XPathDocumentKindChildIterator(XPathDocumentNavigator parent, XPathNodeType typ) : base(parent)
        {
            _typ = typ;
        }

        /// <summary>
        /// Create a new iterator that is a copy of "iter".
        /// </summary>
        public XPathDocumentKindChildIterator(XPathDocumentKindChildIterator iter) : base(iter)
        {
            _typ = iter._typ;
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
                if (!this.ctxt.MoveToChild(_typ))
                    return false;
            }
            else
            {
                if (!this.ctxt.MoveToNext(_typ))
                    return false;
            }

            this.pos++;
            return true;
        }
    }


    /// <summary>
    /// Iterate over all element descendants with a particular QName.
    /// </summary>
    internal sealed class XPathDocumentElementDescendantIterator : XPathDocumentBaseIterator
    {
        private readonly XPathDocumentNavigator? _end;
        private readonly string? _localName;
        private readonly string _namespaceUri;
        private bool _matchSelf;

        /// <summary>
        /// Create an iterator that ranges over all element descendants of "root" having the specified QName.
        /// </summary>
        public XPathDocumentElementDescendantIterator(XPathDocumentNavigator root, string name, string namespaceURI, bool matchSelf) : base(root)
        {
            ArgumentNullException.ThrowIfNull(namespaceURI);

            _localName = root.NameTable.Get(name);
            _namespaceUri = namespaceURI;
            _matchSelf = matchSelf;

            // Find the next non-descendant node that follows "root" in document order
            if (root.NodeType != XPathNodeType.Root)
            {
                _end = new XPathDocumentNavigator(root);
                _end.MoveToNonDescendant();
            }
        }

        /// <summary>
        /// Create a new iterator that is a copy of "iter".
        /// </summary>
        public XPathDocumentElementDescendantIterator(XPathDocumentElementDescendantIterator iter) : base(iter)
        {
            _end = iter._end;
            _localName = iter._localName;
            _namespaceUri = iter._namespaceUri;
            _matchSelf = iter._matchSelf;
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
            if (_matchSelf)
            {
                _matchSelf = false;

                if (this.ctxt.IsElementMatch(_localName, _namespaceUri))
                {
                    this.pos++;
                    return true;
                }
            }

            if (!this.ctxt.MoveToFollowing(_localName, _namespaceUri, _end))
                return false;

            this.pos++;
            return true;
        }
    }


    /// <summary>
    /// Iterate over all content descendants with a particular XPathNodeType.
    /// </summary>
    internal sealed class XPathDocumentKindDescendantIterator : XPathDocumentBaseIterator
    {
        private readonly XPathDocumentNavigator? _end;
        private readonly XPathNodeType _typ;
        private bool _matchSelf;

        /// <summary>
        /// Create an iterator that ranges over all content descendants of "root" having the specified XPathNodeType.
        /// </summary>
        public XPathDocumentKindDescendantIterator(XPathDocumentNavigator root, XPathNodeType typ, bool matchSelf) : base(root)
        {
            _typ = typ;
            _matchSelf = matchSelf;

            // Find the next non-descendant node that follows "root" in document order
            if (root.NodeType != XPathNodeType.Root)
            {
                _end = new XPathDocumentNavigator(root);
                _end.MoveToNonDescendant();
            }
        }

        /// <summary>
        /// Create a new iterator that is a copy of "iter".
        /// </summary>
        public XPathDocumentKindDescendantIterator(XPathDocumentKindDescendantIterator iter) : base(iter)
        {
            _end = iter._end;
            _typ = iter._typ;
            _matchSelf = iter._matchSelf;
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
            if (_matchSelf)
            {
                _matchSelf = false;

                if (this.ctxt.IsKindMatch(_typ))
                {
                    this.pos++;
                    return true;
                }
            }

            if (!this.ctxt.MoveToFollowing(_typ, _end))
                return false;

            this.pos++;
            return true;
        }
    }
}
