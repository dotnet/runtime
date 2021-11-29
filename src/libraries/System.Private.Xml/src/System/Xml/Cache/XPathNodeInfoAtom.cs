// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.XPath;

namespace MS.Internal.Xml.Cache
{
    /// <summary>
    /// The 0th node in each page contains a non-null reference to an XPathNodePageInfo internal class that provides
    /// information about that node's page.  The other fields in the 0th node are undefined and should never
    /// be used.
    /// </summary>
    internal sealed class XPathNodePageInfo
    {
        private readonly int _pageNum;
        private int _nodeCount;
        private readonly XPathNode[]? _pagePrev;
        private XPathNode[]? _pageNext;

        /// <summary>
        /// Constructor.
        /// </summary>
        public XPathNodePageInfo(XPathNode[]? pagePrev, int pageNum)
        {
            _pagePrev = pagePrev;
            _pageNum = pageNum;
            _nodeCount = 1;         // Every node page contains PageInfo at 0th position
        }

        /// <summary>
        /// Return the sequential page number of the page containing nodes that share this information atom.
        /// </summary>
        public int PageNumber
        {
            get { return _pageNum; }
        }

        /// <summary>
        /// Return the number of nodes allocated in this page.
        /// </summary>
        public int NodeCount
        {
            get { return _nodeCount; }
            set { _nodeCount = value; }
        }

        /// <summary>
        /// Return the previous node page in the document.
        /// </summary>
        public XPathNode[]? PreviousPage
        {
            get { return _pagePrev; }
        }

        /// <summary>
        /// Return the next node page in the document.
        /// </summary>
        public XPathNode[]? NextPage
        {
            get { return _pageNext; }
            set { _pageNext = value; }
        }
    }


    /// <summary>
    /// There is a great deal of redundancy in typical Xml documents.  Even in documents with thousands or millions
    /// of nodes, there are a small number of common names and types.  And since nodes are allocated in pages in
    /// document order, nodes on the same page with the same name and type are likely to have the same sibling and
    /// parent pages as well.
    /// Redundant information is shared by creating immutable, atomized objects.  This is analogous to the
    /// string.Intern() operation.  If a node's name, type, or parent/sibling pages are modified, then a new
    /// InfoAtom needs to be obtained, since other nodes may still be referencing the old InfoAtom.
    /// </summary>
    internal sealed class XPathNodeInfoAtom : IEquatable<XPathNodeInfoAtom>
    {
        private string _localName = null!;
        private string _namespaceUri = null!;
        private string _prefix = null!;
        private string? _baseUri;
        private XPathNode[]? _pageParent;
        private XPathNode[]? _pageSibling;
        private XPathNode[]? _pageSimilar;
        private XPathDocument _doc = null!;
        private int _lineNumBase;
        private int _linePosBase;
        private int _hashCode;
        private int _localNameHash;
        private XPathNodeInfoAtom? _next;
        private XPathNodePageInfo? _pageInfo;


        /// <summary>
        /// Construct information for the 0th node in each page.  The only field which is defined is this.pageInfo,
        /// and it contains information about that page (pageNum, nextPage, etc.).
        /// </summary>
        public XPathNodeInfoAtom(XPathNodePageInfo pageInfo)
        {
            _pageInfo = pageInfo;
        }

        /// <summary>
        /// Construct a new shared information atom.  This method should only be used by the XNodeInfoTable.
        /// </summary>
        public XPathNodeInfoAtom(string localName, string namespaceUri, string prefix, string? baseUri,
                                         XPathNode[]? pageParent, XPathNode[]? pageSibling, XPathNode[]? pageSimilar,
                                         XPathDocument doc, int lineNumBase, int linePosBase)
        {
            Init(localName, namespaceUri, prefix, baseUri, pageParent, pageSibling, pageSimilar, doc, lineNumBase, linePosBase);
        }

        /// <summary>
        /// Initialize an existing shared information atom.  This method should only be used by the XNodeInfoTable.
        /// </summary>
        public void Init(string localName, string namespaceUri, string prefix, string? baseUri,
                         XPathNode[]? pageParent, XPathNode[]? pageSibling, XPathNode[]? pageSimilar,
                         XPathDocument doc, int lineNumBase, int linePosBase)
        {
            Debug.Assert(localName != null && namespaceUri != null && prefix != null && doc != null);

            _localName = localName;
            _namespaceUri = namespaceUri;
            _prefix = prefix;
            _baseUri = baseUri;
            _pageParent = pageParent;
            _pageSibling = pageSibling;
            _pageSimilar = pageSimilar;
            _doc = doc;
            _lineNumBase = lineNumBase;
            _linePosBase = linePosBase;
            _next = null;
            _pageInfo = null;

            _hashCode = 0;
            _localNameHash = 0;
            for (int i = 0; i < _localName.Length; i++)
                unchecked { _localNameHash += (_localNameHash << 7) ^ _localName[i]; }
        }

        /// <summary>
        /// Returns information about the node page.  Only the 0th node on each page has this property defined.
        /// </summary>
        public XPathNodePageInfo? PageInfo
        {
            get { return _pageInfo; }
        }

        /// <summary>
        /// Return the local name part of nodes that share this information atom.
        /// </summary>
        public string LocalName
        {
            get { return _localName; }
        }

        /// <summary>
        /// Return the namespace name part of nodes that share this information atom.
        /// </summary>
        public string NamespaceUri
        {
            get { return _namespaceUri; }
        }

        /// <summary>
        /// Return the prefix name part of nodes that share this information atom.
        /// </summary>
        public string Prefix
        {
            get { return _prefix; }
        }

        /// <summary>
        /// Return the base Uri of nodes that share this information atom.
        /// </summary>
        public string? BaseUri
        {
            get { return _baseUri; }
        }

        /// <summary>
        /// Return the page containing the next sibling of nodes that share this information atom.
        /// </summary>
        public XPathNode[]? SiblingPage
        {
            get { return _pageSibling; }
        }

        /// <summary>
        /// Return the page containing the next element having a name which has same hashcode as this element.
        /// </summary>
        public XPathNode[]? SimilarElementPage
        {
            get { return _pageSimilar; }
        }

        /// <summary>
        /// Return the page containing the parent of nodes that share this information atom.
        /// </summary>
        public XPathNode[]? ParentPage
        {
            get { return _pageParent; }
        }

        /// <summary>
        /// Return the page containing the owner document of nodes that share this information atom.
        /// </summary>
        public XPathDocument Document
        {
            get { return _doc; }
        }

        /// <summary>
        /// Return the line number to which a line number offset stored in the XPathNode is added.
        /// </summary>
        public int LineNumberBase
        {
            get { return _lineNumBase; }
        }

        /// <summary>
        /// Return the line position to which a line position offset stored in the XPathNode is added.
        /// </summary>
        public int LinePositionBase
        {
            get { return _linePosBase; }
        }

        /// <summary>
        /// Return cached hash code of the local name of nodes which share this information atom.
        /// </summary>
        public int LocalNameHashCode
        {
            get { return _localNameHash; }
        }

        /// <summary>
        /// Link together InfoAtoms that hash to the same hashtable bucket (should only be used by XPathNodeInfoTable)
        /// </summary>
        public XPathNodeInfoAtom? Next
        {
            get { return _next; }
            set { _next = value; }
        }

        /// <summary>
        /// Return this information atom's hash code, previously computed for performance.
        /// </summary>
        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                int hashCode;

                // Start with local name
                hashCode = _localNameHash;

                // Add page indexes
                unchecked
                {
                    if (_pageSibling != null)
                        hashCode += (hashCode << 7) ^ _pageSibling[0].PageInfo!.PageNumber;

                    if (_pageParent != null)
                        hashCode += (hashCode << 7) ^ _pageParent[0].PageInfo!.PageNumber;

                    if (_pageSimilar != null)
                        hashCode += (hashCode << 7) ^ _pageSimilar[0].PageInfo!.PageNumber;
                }

                // Save hashcode.  Don't save 0, so that it won't ever be recomputed.
                _hashCode = ((hashCode == 0) ? 1 : hashCode);
            }

            return _hashCode;
        }

        /// <summary>
        /// Return true if this InfoAtom has the same values as another InfoAtom.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? other)
        {
            return Equals(other as XPathNodeInfoAtom);
        }

        public bool Equals(XPathNodeInfoAtom? other)
        {
            Debug.Assert(other != null);
            Debug.Assert((object?)_doc == (object?)other._doc);
            Debug.Assert(_pageInfo == null);

            // Assume that name parts are atomized
            if (this.GetHashCode() == other.GetHashCode())
            {
                if ((object?)_localName == (object?)other._localName &&
                    (object?)_pageSibling == (object?)other._pageSibling &&
                    (object?)_namespaceUri == (object?)other._namespaceUri &&
                    (object?)_pageParent == (object?)other._pageParent &&
                    (object?)_pageSimilar == (object?)other._pageSimilar &&
                    (object?)_prefix == (object?)other._prefix &&
                    (object?)_baseUri == (object?)other._baseUri &&
                    _lineNumBase == other._lineNumBase &&
                    _linePosBase == other._linePosBase)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Return InfoAtom formatted as a string:
        ///     hash=xxx, {http://my.com}foo:bar, parent=1, sibling=1, lineNum=0, linePos=0
        /// </summary>
        public override string ToString()
        {
            StringBuilder bldr = new StringBuilder();

            bldr.Append("hash=");
            bldr.Append(GetHashCode());
            bldr.Append(", ");

            Debug.Assert(_localName != null);
            if (_localName.Length != 0)
            {
                bldr.Append('{');
                bldr.Append(_namespaceUri);
                bldr.Append('}');

                Debug.Assert(_prefix != null);
                if (_prefix.Length != 0)
                {
                    bldr.Append(_prefix);
                    bldr.Append(':');
                }

                bldr.Append(_localName);
                bldr.Append(", ");
            }

            if (_pageParent != null)
            {
                bldr.Append("parent=");
                bldr.Append(_pageParent[0].PageInfo!.PageNumber);
                bldr.Append(", ");
            }

            if (_pageSibling != null)
            {
                bldr.Append("sibling=");
                bldr.Append(_pageSibling[0].PageInfo!.PageNumber);
                bldr.Append(", ");
            }

            if (_pageSimilar != null)
            {
                bldr.Append("similar=");
                bldr.Append(_pageSimilar[0].PageInfo!.PageNumber);
                bldr.Append(", ");
            }

            bldr.Append("lineNum=");
            bldr.Append(_lineNumBase);
            bldr.Append(", ");

            bldr.Append("linePos=");
            bldr.Append(_linePosBase);

            return bldr.ToString();
        }
    }


    /// <summary>
    /// An atomization table for XPathNodeInfoAtom.
    /// </summary>
    internal sealed class XPathNodeInfoTable
    {
        private XPathNodeInfoAtom[] _hashTable;
        private int _sizeTable;
        private XPathNodeInfoAtom? _infoCached;

#if DEBUG
        private const int DefaultTableSize = 2;
#else
        private const int DefaultTableSize = 32;
#endif

        /// <summary>
        /// Constructor.
        /// </summary>
        public XPathNodeInfoTable()
        {
            _hashTable = new XPathNodeInfoAtom[DefaultTableSize];
            _sizeTable = 0;
        }

        /// <summary>
        /// Create a new XNodeInfoAtom and ensure it is atomized in the table.
        /// </summary>
        public XPathNodeInfoAtom Create(string localName, string namespaceUri, string prefix, string? baseUri,
                                          XPathNode[]? pageParent, XPathNode[]? pageSibling, XPathNode[]? pageSimilar,
                                          XPathDocument doc, int lineNumBase, int linePosBase)
        {
            XPathNodeInfoAtom info;

            // If this.infoCached already exists, then reuse it; else create new InfoAtom
            if (_infoCached == null)
            {
                info = new XPathNodeInfoAtom(localName, namespaceUri, prefix, baseUri,
                                             pageParent, pageSibling, pageSimilar,
                                             doc, lineNumBase, linePosBase);
            }
            else
            {
                info = _infoCached;
                _infoCached = info.Next;

                info.Init(localName, namespaceUri, prefix, baseUri,
                          pageParent, pageSibling, pageSimilar,
                          doc, lineNumBase, linePosBase);
            }

            return Atomize(info);
        }


        /// <summary>
        /// Add a shared information item to the atomization table.  If a matching item already exists, then that
        /// instance is returned.  Otherwise, a new item is created.  Thus, if itemX and itemY have both been added
        /// to the same InfoTable:
        /// 1. itemX.Equals(itemY) != true
        /// 2. (object) itemX != (object) itemY
        /// </summary>
        private XPathNodeInfoAtom Atomize(XPathNodeInfoAtom info)
        {
            XPathNodeInfoAtom? infoNew, infoNext;

            // Search for existing XNodeInfoAtom in the table
            infoNew = _hashTable[info.GetHashCode() & (_hashTable.Length - 1)];
            while (infoNew != null)
            {
                if (info.Equals(infoNew))
                {
                    // Found existing atom, so return that.  Reuse "info".
                    info.Next = _infoCached;
                    _infoCached = info;
                    return infoNew;
                }
                infoNew = infoNew.Next;
            }

            // Expand table and rehash if necessary
            if (_sizeTable >= _hashTable.Length)
            {
                XPathNodeInfoAtom[] oldTable = _hashTable;
                _hashTable = new XPathNodeInfoAtom[oldTable.Length * 2];

                for (int i = 0; i < oldTable.Length; i++)
                {
                    infoNew = oldTable[i];
                    while (infoNew != null)
                    {
                        infoNext = infoNew.Next;
                        AddInfo(infoNew);
                        infoNew = infoNext;
                    }
                }
            }

            // Can't find an existing XNodeInfoAtom, so use the one that was passed in
            AddInfo(info);

            return info;
        }

        /// <summary>
        /// Add a previously constructed InfoAtom to the table.  If a collision occurs, then insert "info"
        /// as the head of a linked list.
        /// </summary>
        private void AddInfo(XPathNodeInfoAtom info)
        {
            int idx = info.GetHashCode() & (_hashTable.Length - 1);
            info.Next = _hashTable[idx];
            _hashTable[idx] = info;
            _sizeTable++;
        }

        /// <summary>
        /// Return InfoAtomTable formatted as a string.
        /// </summary>
        public override string ToString()
        {
            StringBuilder bldr = new StringBuilder();
            XPathNodeInfoAtom? infoAtom;

            for (int i = 0; i < _hashTable.Length; i++)
            {
                bldr.Append($"{i,4}: ");

                infoAtom = _hashTable[i];

                while (infoAtom != null)
                {
                    if ((object)infoAtom != (object)_hashTable[i])
                        bldr.Append("\n      ");

                    bldr.Append(infoAtom);

                    infoAtom = infoAtom.Next;
                }

                bldr.Append('\n');
            }

            return bldr.ToString();
        }
    }
}
