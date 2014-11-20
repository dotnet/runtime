// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using System.Xml.XPath;

namespace MS.Internal.Xml.Cache
{
    /// <summary>
    /// The 0th node in each page contains a non-null reference to an XPathNodePageInfo internal class that provides
    /// information about that node's page.  The other fields in the 0th node are undefined and should never
    /// be used.
    /// </summary>
    sealed internal class XPathNodePageInfo
    {
        private int pageNum;
        private int nodeCount;
        private XPathNode[] pagePrev;
        private XPathNode[] pageNext;

        /// <summary>
        /// Constructor.
        /// </summary>
        public XPathNodePageInfo(XPathNode[] pagePrev, int pageNum)
        {
            this.pagePrev = pagePrev;
            this.pageNum = pageNum;
            this.nodeCount = 1;         // Every node page contains PageInfo at 0th position
        }

        /// <summary>
        /// Return the sequential page number of the page containing nodes that share this information atom.
        /// </summary>
        public int PageNumber
        {
            get { return this.pageNum; }
        }

        /// <summary>
        /// Return the number of nodes allocated in this page.
        /// </summary>
        public int NodeCount
        {
            get { return this.nodeCount; }
            set { this.nodeCount = value; }
        }

        /// <summary>
        /// Return the previous node page in the document.
        /// </summary>
        public XPathNode[] PreviousPage
        {
            get { return this.pagePrev; }
        }

        /// <summary>
        /// Return the next node page in the document.
        /// </summary>
        public XPathNode[] NextPage
        {
            get { return this.pageNext; }
            set { this.pageNext = value; }
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
    sealed internal class XPathNodeInfoAtom
    {
        private string localName;
        private string namespaceUri;
        private string prefix;
        private string baseUri;
        private XPathNode[] pageParent;
        private XPathNode[] pageSibling;
        private XPathNode[] pageSimilar;
        private XPathDocument doc;
        private int lineNumBase;
        private int linePosBase;
        private int hashCode;
        private int localNameHash;
        private XPathNodeInfoAtom next;
        private XPathNodePageInfo pageInfo;


        /// <summary>
        /// Construct information for the 0th node in each page.  The only field which is defined is this.pageInfo,
        /// and it contains information about that page (pageNum, nextPage, etc.).
        /// </summary>
        public XPathNodeInfoAtom(XPathNodePageInfo pageInfo)
        {
            this.pageInfo = pageInfo;
        }

        /// <summary>
        /// Construct a new shared information atom.  This method should only be used by the XNodeInfoTable.
        /// </summary>
        public XPathNodeInfoAtom(string localName, string namespaceUri, string prefix, string baseUri,
                                         XPathNode[] pageParent, XPathNode[] pageSibling, XPathNode[] pageSimilar,
                                         XPathDocument doc, int lineNumBase, int linePosBase)
        {
            Init(localName, namespaceUri, prefix, baseUri, pageParent, pageSibling, pageSimilar, doc, lineNumBase, linePosBase);
        }

        /// <summary>
        /// Initialize an existing shared information atom.  This method should only be used by the XNodeInfoTable.
        /// </summary>
        public void Init(string localName, string namespaceUri, string prefix, string baseUri,
                         XPathNode[] pageParent, XPathNode[] pageSibling, XPathNode[] pageSimilar,
                         XPathDocument doc, int lineNumBase, int linePosBase)
        {
            Debug.Assert(localName != null && namespaceUri != null && prefix != null && doc != null);

            this.localName = localName;
            this.namespaceUri = namespaceUri;
            this.prefix = prefix;
            this.baseUri = baseUri;
            this.pageParent = pageParent;
            this.pageSibling = pageSibling;
            this.pageSimilar = pageSimilar;
            this.doc = doc;
            this.lineNumBase = lineNumBase;
            this.linePosBase = linePosBase;
            this.next = null;
            this.pageInfo = null;

            this.hashCode = 0;
            this.localNameHash = 0;
            for (int i = 0; i < this.localName.Length; i++)
                this.localNameHash += (this.localNameHash << 7) ^ this.localName[i];
        }

        /// <summary>
        /// Returns information about the node page.  Only the 0th node on each page has this property defined.
        /// </summary>
        public XPathNodePageInfo PageInfo
        {
            get { return this.pageInfo; }
        }

        /// <summary>
        /// Return the local name part of nodes that share this information atom.
        /// </summary>
        public string LocalName
        {
            get { return this.localName; }
        }

        /// <summary>
        /// Return the namespace name part of nodes that share this information atom.
        /// </summary>
        public string NamespaceUri
        {
            get { return this.namespaceUri; }
        }

        /// <summary>
        /// Return the prefix name part of nodes that share this information atom.
        /// </summary>
        public string Prefix
        {
            get { return this.prefix; }
        }

        /// <summary>
        /// Return the base Uri of nodes that share this information atom.
        /// </summary>
        public string BaseUri
        {
            get { return this.baseUri; }
        }

        /// <summary>
        /// Return the page containing the next sibling of nodes that share this information atom.
        /// </summary>
        public XPathNode[] SiblingPage
        {
            get { return this.pageSibling; }
        }

        /// <summary>
        /// Return the page containing the next element having a name which has same hashcode as this element.
        /// </summary>
        public XPathNode[] SimilarElementPage
        {
            get { return this.pageSimilar; }
        }

        /// <summary>
        /// Return the page containing the parent of nodes that share this information atom.
        /// </summary>
        public XPathNode[] ParentPage
        {
            get { return this.pageParent; }
        }

        /// <summary>
        /// Return the page containing the owner document of nodes that share this information atom.
        /// </summary>
        public XPathDocument Document
        {
            get { return this.doc; }
        }

        /// <summary>
        /// Return the line number to which a line number offset stored in the XPathNode is added.
        /// </summary>
        public int LineNumberBase
        {
            get { return this.lineNumBase; }
        }

        /// <summary>
        /// Return the line position to which a line position offset stored in the XPathNode is added.
        /// </summary>
        public int LinePositionBase
        {
            get { return this.linePosBase; }
        }

        /// <summary>
        /// Return cached hash code of the local name of nodes which share this information atom.
        /// </summary>
        public int LocalNameHashCode
        {
            get { return this.localNameHash; }
        }

        /// <summary>
        /// Link together InfoAtoms that hash to the same hashtable bucket (should only be used by XPathNodeInfoTable)
        /// </summary>
        public XPathNodeInfoAtom Next
        {
            get { return this.next; }
            set { this.next = value; }
        }

        /// <summary>
        /// Return this information atom's hash code, previously computed for performance.
        /// </summary>
        public override int GetHashCode()
        {
            if (this.hashCode == 0)
            {
                int hashCode;

                // Start with local name
                hashCode = this.localNameHash;

                // Add page indexes
                if (this.pageSibling != null)
                    hashCode += (hashCode << 7) ^ this.pageSibling[0].PageInfo.PageNumber;

                if (this.pageParent != null)
                    hashCode += (hashCode << 7) ^ this.pageParent[0].PageInfo.PageNumber;

                if (this.pageSimilar != null)
                    hashCode += (hashCode << 7) ^ this.pageSimilar[0].PageInfo.PageNumber;

                // Save hashcode.  Don't save 0, so that it won't ever be recomputed.
                this.hashCode = ((hashCode == 0) ? 1 : hashCode);
            }

            return this.hashCode;
        }

        /// <summary>
        /// Return true if this InfoAtom has the same values as another InfoAtom.
        /// </summary>
        public override bool Equals(object other)
        {
            XPathNodeInfoAtom that = other as XPathNodeInfoAtom;
            Debug.Assert(that != null);
            Debug.Assert((object)this.doc == (object)that.doc);
            Debug.Assert(this.pageInfo == null);

            // Assume that name parts are atomized
            if (this.GetHashCode() == that.GetHashCode())
            {
                if ((object)this.localName == (object)that.localName &&
                    (object)this.pageSibling == (object)that.pageSibling &&
                    (object)this.namespaceUri == (object)that.namespaceUri &&
                    (object)this.pageParent == (object)that.pageParent &&
                    (object)this.pageSimilar == (object)that.pageSimilar &&
                    (object)this.prefix == (object)that.prefix &&
                    (object)this.baseUri == (object)that.baseUri &&
                    this.lineNumBase == that.lineNumBase &&
                    this.linePosBase == that.linePosBase)
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

            if (this.localName.Length != 0)
            {
                bldr.Append('{');
                bldr.Append(this.namespaceUri);
                bldr.Append('}');

                if (this.prefix.Length != 0)
                {
                    bldr.Append(this.prefix);
                    bldr.Append(':');
                }

                bldr.Append(this.localName);
                bldr.Append(", ");
            }

            if (this.pageParent != null)
            {
                bldr.Append("parent=");
                bldr.Append(this.pageParent[0].PageInfo.PageNumber);
                bldr.Append(", ");
            }

            if (this.pageSibling != null)
            {
                bldr.Append("sibling=");
                bldr.Append(this.pageSibling[0].PageInfo.PageNumber);
                bldr.Append(", ");
            }

            if (this.pageSimilar != null)
            {
                bldr.Append("similar=");
                bldr.Append(this.pageSimilar[0].PageInfo.PageNumber);
                bldr.Append(", ");
            }

            bldr.Append("lineNum=");
            bldr.Append(this.lineNumBase);
            bldr.Append(", ");

            bldr.Append("linePos=");
            bldr.Append(this.linePosBase);

            return bldr.ToString();
        }
    }


    /// <summary>
    /// An atomization table for XPathNodeInfoAtom.
    /// </summary>
    sealed internal class XPathNodeInfoTable
    {
        private XPathNodeInfoAtom[] hashTable;
        private int sizeTable;
        private XPathNodeInfoAtom infoCached;

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
            this.hashTable = new XPathNodeInfoAtom[DefaultTableSize];
            this.sizeTable = 0;
        }

        /// <summary>
        /// Create a new XNodeInfoAtom and ensure it is atomized in the table.
        /// </summary>
        public XPathNodeInfoAtom Create(string localName, string namespaceUri, string prefix, string baseUri,
                                          XPathNode[] pageParent, XPathNode[] pageSibling, XPathNode[] pageSimilar,
                                          XPathDocument doc, int lineNumBase, int linePosBase)
        {
            XPathNodeInfoAtom info;

            // If this.infoCached already exists, then reuse it; else create new InfoAtom
            if (this.infoCached == null)
            {
                info = new XPathNodeInfoAtom(localName, namespaceUri, prefix, baseUri,
                                             pageParent, pageSibling, pageSimilar,
                                             doc, lineNumBase, linePosBase);
            }
            else
            {
                info = this.infoCached;
                this.infoCached = info.Next;

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
            XPathNodeInfoAtom infoNew, infoNext;

            // Search for existing XNodeInfoAtom in the table
            infoNew = this.hashTable[info.GetHashCode() & (this.hashTable.Length - 1)];
            while (infoNew != null)
            {
                if (info.Equals(infoNew))
                {
                    // Found existing atom, so return that.  Reuse "info".
                    info.Next = this.infoCached;
                    this.infoCached = info;
                    return infoNew;
                }
                infoNew = infoNew.Next;
            }

            // Expand table and rehash if necessary
            if (this.sizeTable >= this.hashTable.Length)
            {
                XPathNodeInfoAtom[] oldTable = this.hashTable;
                this.hashTable = new XPathNodeInfoAtom[oldTable.Length * 2];

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
            int idx = info.GetHashCode() & (this.hashTable.Length - 1);
            info.Next = this.hashTable[idx];
            this.hashTable[idx] = info;
            this.sizeTable++;
        }

        /// <summary>
        /// Return InfoAtomTable formatted as a string.
        /// </summary>
        public override string ToString()
        {
            StringBuilder bldr = new StringBuilder();
            XPathNodeInfoAtom infoAtom;

            for (int i = 0; i < this.hashTable.Length; i++)
            {
                bldr.AppendFormat("{0,4}: ", i);

                infoAtom = this.hashTable[i];

                while (infoAtom != null)
                {
                    if ((object)infoAtom != (object)this.hashTable[i])
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
