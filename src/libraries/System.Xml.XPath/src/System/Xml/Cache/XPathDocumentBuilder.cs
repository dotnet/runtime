// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace MS.Internal.Xml.Cache
{
    /// <summary>
    /// Although the XPath data model does not differentiate between text and whitespace, Managed Xml 1.0
    /// does.  Therefore, when building from an XmlReader, we must preserve these designations in order
    /// to remain backwards-compatible.
    /// </summary>
    internal enum TextBlockType
    {
        None = 0,
        Text = XPathNodeType.Text,
        SignificantWhitespace = XPathNodeType.SignificantWhitespace,
        Whitespace = XPathNodeType.Whitespace,
    };


    /// <summary>
    /// Implementation of XmlRawWriter that builds nodes in an XPathDocument.
    /// </summary>
    internal sealed class XPathDocumentBuilder : XmlRawWriter
    {
        private NodePageFactory nodePageFact;   // Creates non-namespace node pages
        private NodePageFactory nmspPageFact;   // Creates namespace node pages
        private TextBlockBuilder textBldr;      // Concatenates adjacent text blocks

        private Stack<XPathNodeRef> stkNmsp;    // In-scope namespaces
        private XPathNodeInfoTable infoTable;   // Atomization table for shared node information
        private XPathDocument doc;              // Currently building document
        private IXmlLineInfo lineInfo;          // Line information provider
        private XmlNameTable nameTable;         // Atomization table for all names in the document
        private bool atomizeNames;              // True if all names should be atomized (false if they are pre-atomized)

        private XPathNode[] pageNmsp;           // Page of last in-scope namespace node
        private int idxNmsp;                    // Page index of last in-scope namespace node
        private XPathNode[] pageParent;         // Page of last parent-type node (Element or Root)
        private int idxParent;                  // Page index of last parent-type node (Element or Root)
        private XPathNode[] pageSibling;        // Page of previous sibling node (may be null if no previous sibling)
        private int idxSibling;                 // Page index of previous sibling node
        private int lineNumBase;                // Line number from which offsets are computed
        private int linePosBase;                // Line position from which offsets are computed

        private XPathNodeRef[] elemNameIndex;   // Elements with the same name are linked together so that they can be searched quickly

        private const int ElementIndexSize = 64;

        /// <summary>
        /// Create a new XPathDocumentBuilder which creates nodes in "doc".
        /// </summary>
        public XPathDocumentBuilder(XPathDocument doc, IXmlLineInfo lineInfo, string baseUri, XPathDocument.LoadFlags flags)
        {
            // Allocate the initial node (for non-namespaces) page, and the initial namespace page
            this.nodePageFact.Init(256);
            this.nmspPageFact.Init(16);

            this.stkNmsp = new Stack<XPathNodeRef>();

            Initialize(doc, lineInfo, baseUri, flags);
        }

        /// <summary>
        /// Start construction of a new document.  This must be called before any other methods are called.
        /// It may also be called after Close(), in order to build further documents.
        /// </summary>
        public void Initialize(XPathDocument doc, IXmlLineInfo lineInfo, string baseUri, XPathDocument.LoadFlags flags)
        {
            XPathNode[] page;
            int idx;

            this.doc = doc;
            this.nameTable = doc.NameTable;
            this.atomizeNames = (flags & XPathDocument.LoadFlags.AtomizeNames) != 0;
            this.idxParent = this.idxSibling = 0;
            this.elemNameIndex = new XPathNodeRef[ElementIndexSize];

            // Prepare line number information
            this.textBldr.Initialize(lineInfo);
            this.lineInfo = lineInfo;
            this.lineNumBase = 0;
            this.linePosBase = 0;

            // Allocate the atomization table
            this.infoTable = new XPathNodeInfoTable();

            // Allocate singleton collapsed text node
            idx = NewNode(out page, XPathNodeType.Text, string.Empty, string.Empty, string.Empty, string.Empty);
            this.doc.SetCollapsedTextNode(page, idx);

            // Allocate xmlns:xml namespace node
            this.idxNmsp = NewNamespaceNode(out this.pageNmsp, this.nameTable.Add("xml"), this.nameTable.Add(XmlConst.ReservedNsXml), null, 0);
            this.doc.SetXmlNamespaceNode(this.pageNmsp, this.idxNmsp);

            if ((flags & XPathDocument.LoadFlags.Fragment) == 0)
            {
                // This tree has a document root node
                this.idxParent = NewNode(out this.pageParent, XPathNodeType.Root, string.Empty, string.Empty, string.Empty, baseUri);
                this.doc.SetRootNode(this.pageParent, this.idxParent);
            }
            else
            {
                // This tree is an XQuery fragment (no document root node), so root will be next node in the current page
                this.doc.SetRootNode(this.nodePageFact.NextNodePage, this.nodePageFact.NextNodeIndex);
            }
        }


        //-----------------------------------------------
        // XmlWriter interface
        //-----------------------------------------------

        /// <summary>
        /// XPathDocument ignores the DocType information.
        /// </summary>
        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
        }

        /// <summary>
        /// Shortcut for calling WriteStartElement with elemType == null.
        /// </summary>
        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            this.WriteStartElement(prefix, localName, ns, string.Empty);
        }

        /// <summary>
        /// Build an element node and attach it to its parent, if one exists.  Make the element the new parent node.
        /// </summary>
        public void WriteStartElement(string prefix, string localName, string ns, string baseUri)
        {
            int hash;
            Debug.Assert(prefix != null && localName != null && ns != null && localName.Length != 0 && baseUri != null);

            if (this.atomizeNames)
            {
                prefix = this.nameTable.Add(prefix);
                localName = this.nameTable.Add(localName);
                ns = this.nameTable.Add(ns);
            }

            AddSibling(XPathNodeType.Element, localName, ns, prefix, baseUri);
            this.pageParent = this.pageSibling;
            this.idxParent = this.idxSibling;
            this.idxSibling = 0;

            // Link elements with the same name together
            hash = (this.pageParent[this.idxParent].LocalNameHashCode & (ElementIndexSize - 1));
            this.elemNameIndex[hash] = LinkSimilarElements(this.elemNameIndex[hash].Page, this.elemNameIndex[hash].Index, this.pageParent, this.idxParent);
        }

        /// <summary>
        /// Must be called when an element node's children have been fully enumerated.
        /// </summary>
        public override void WriteEndElement()
        {
            WriteEndElement(true);
        }

        /// <summary>
        /// Must be called when an element node's children have been fully enumerated.
        /// </summary>
        public override void WriteFullEndElement()
        {
            WriteEndElement(false);
        }

        /// <summary>
        /// Must be called when an element node's children have been fully enumerated.
        /// </summary>
        internal override void WriteEndElement(string prefix, string localName, string namespaceName)
        {
            WriteEndElement(true);
        }

        /// <summary>
        /// Must be called when an element node's children have been fully enumerated.
        /// </summary>
        internal override void WriteFullEndElement(string prefix, string localName, string namespaceName)
        {
            WriteEndElement(false);
        }

        /// <summary>
        /// Must be called when an element node's children have been fully enumerated.
        /// </summary>
        public void WriteEndElement(bool allowShortcutTag)
        {
            XPathNodeRef nodeRef;
            Debug.Assert(this.pageParent[this.idxParent].NodeType == XPathNodeType.Element);

            // If element has no content-typed children except for the one about to be added, then
            // its value is the same as its only text child's.
            if (!this.pageParent[this.idxParent].HasContentChild)
            {
                switch (this.textBldr.TextType)
                {
                    case TextBlockType.Text:
                        // Collapsed text node can be created if text line number information can be encoded efficiently in parent node
                        if (this.lineInfo != null)
                        {
                            // If collapsed text node is not on same line as parent, don't collapse text
                            if (this.textBldr.LineNumber != this.pageParent[this.idxParent].LineNumber)
                                goto case TextBlockType.Whitespace;

                            // If position is not within 256 of parent, don't collapse text
                            int posDiff = this.textBldr.LinePosition - this.pageParent[this.idxParent].LinePosition;
                            if (posDiff < 0 || posDiff > XPathNode.MaxCollapsedPositionOffset)
                                goto case TextBlockType.Whitespace;

                            // Set collapsed node line position offset
                            this.pageParent[this.idxParent].SetCollapsedLineInfoOffset(posDiff);
                        }

                        // Set collapsed node text
                        this.pageParent[this.idxParent].SetCollapsedValue(this.textBldr.ReadText());
                        break;

                    case TextBlockType.SignificantWhitespace:
                    case TextBlockType.Whitespace:
                        // Create separate whitespace node
                        CachedTextNode();
                        this.pageParent[this.idxParent].SetValue(this.pageSibling[this.idxSibling].Value);
                        break;

                    default:
                        // Empty value, so don't create collapsed text node
                        this.pageParent[this.idxParent].SetEmptyValue(allowShortcutTag);
                        break;
                }
            }
            else
            {
                if (this.textBldr.HasText)
                {
                    // Element's last child (one of several) is a text or whitespace node
                    CachedTextNode();
                }
            }

            // If namespaces were added to this element,
            if (this.pageParent[this.idxParent].HasNamespaceDecls)
            {
                // Add it to the document's element --> namespace mapping
                this.doc.AddNamespace(this.pageParent, this.idxParent, this.pageNmsp, this.idxNmsp);

                // Restore the previous namespace chain
                nodeRef = this.stkNmsp.Pop();
                this.pageNmsp = nodeRef.Page;
                this.idxNmsp = nodeRef.Index;
            }

            // Make parent of this element the current element
            this.pageSibling = this.pageParent;
            this.idxSibling = this.idxParent;
            this.idxParent = this.pageParent[this.idxParent].GetParent(out this.pageParent);
        }

        /// <summary>
        /// Shortcut for calling WriteStartAttribute with attrfType == null.
        /// </summary>
        public override void WriteStartAttribute(string prefix, string localName, string namespaceName)
        {
            Debug.Assert(!prefix.Equals("xmlns"));
            Debug.Assert(this.idxParent == 0 || this.pageParent[this.idxParent].NodeType == XPathNodeType.Element);
            Debug.Assert(this.idxSibling == 0 || this.pageSibling[this.idxSibling].NodeType == XPathNodeType.Attribute);

            if (this.atomizeNames)
            {
                prefix = this.nameTable.Add(prefix);
                localName = this.nameTable.Add(localName);
                namespaceName = this.nameTable.Add(namespaceName);
            }

            AddSibling(XPathNodeType.Attribute, localName, namespaceName, prefix, string.Empty);
        }

        /// <summary>
        /// Attach the attribute's text or typed value to the previously constructed attribute node.
        /// </summary>
        public override void WriteEndAttribute()
        {
            Debug.Assert(this.pageSibling[this.idxSibling].NodeType == XPathNodeType.Attribute);

            this.pageSibling[this.idxSibling].SetValue(this.textBldr.ReadText());
        }

        /// <summary>
        /// Map CData text into regular text.
        /// </summary>
        public override void WriteCData(string text)
        {
            WriteString(text, TextBlockType.Text);
        }

        /// <summary>
        /// Construct comment node.
        /// </summary>
        public override void WriteComment(string text)
        {
            AddSibling(XPathNodeType.Comment, string.Empty, string.Empty, string.Empty, string.Empty);
            this.pageSibling[this.idxSibling].SetValue(text);
        }

        /// <summary>
        /// Shortcut for calling WriteProcessingInstruction with baseUri = string.Empty.
        /// </summary>
        public override void WriteProcessingInstruction(string name, string text)
        {
            this.WriteProcessingInstruction(name, text, string.Empty);
        }

        /// <summary>
        /// Construct pi node.
        /// </summary>
        public void WriteProcessingInstruction(string name, string text, string baseUri)
        {
            if (this.atomizeNames)
                name = this.nameTable.Add(name);

            AddSibling(XPathNodeType.ProcessingInstruction, name, string.Empty, string.Empty, baseUri);
            this.pageSibling[this.idxSibling].SetValue(text);
        }

        /// <summary>
        /// Write a whitespace text block.
        /// </summary>
        public override void WriteWhitespace(string ws)
        {
            WriteString(ws, TextBlockType.Whitespace);
        }

        /// <summary>
        /// Write an attribute or element text block.
        /// </summary>
        public override void WriteString(string text)
        {
            WriteString(text, TextBlockType.Text);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            WriteString(new string(buffer, index, count), TextBlockType.Text);
        }

        /// <summary>
        /// Map RawText to Text.  This will lose entitization and won't roundtrip.
        /// </summary>
        public override void WriteRaw(string data)
        {
            WriteString(data, TextBlockType.Text);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            WriteString(new string(buffer, index, count), TextBlockType.Text);
        }

        /// <summary>
        /// Write an element text block with the specified text type (whitespace, significant whitespace, or text).
        /// </summary>
        public void WriteString(string text, TextBlockType textType)
        {
            this.textBldr.WriteTextBlock(text, textType);
        }

        /// <summary>
        /// Cache does not handle entity references.
        /// </summary>
        public override void WriteEntityRef(string name)
        {
            throw NotImplemented.ByDesign;
        }

        /// <summary>
        /// Don't entitize, since the cache cannot represent character entities.
        /// </summary>
        public override void WriteCharEntity(char ch)
        {
            WriteString(new string(ch, 1), TextBlockType.Text);
        }

        /// <summary>
        /// Don't entitize, since the cache cannot represent character entities.
        /// </summary>
        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            char[] chars = { highChar, lowChar };
            WriteString(new string(chars), TextBlockType.Text);
        }

        /// <summary>
        /// Signals the end of tree construction.
        /// </summary>
        internal void CloseWithoutDisposing()
        {
            XPathNode[] page;
            int idx;

            // If cached text exists, then create a text node at the top-level
            if (this.textBldr.HasText)
                CachedTextNode();

            // If document does not yet contain nodes, then an empty text node must have been created
            idx = this.doc.GetRootNode(out page);
            if (idx == this.nodePageFact.NextNodeIndex && page == this.nodePageFact.NextNodePage)
            {
                AddSibling(XPathNodeType.Text, string.Empty, string.Empty, string.Empty, string.Empty);
                this.pageSibling[this.idxSibling].SetValue(string.Empty);
            }
        }

        /// <summary>
        /// Since output is not forwarded to another object, this does nothing.
        /// </summary>
        public override void Flush()
        {
        }


        //-----------------------------------------------
        // XmlRawWriter interface
        //-----------------------------------------------

        /// <summary>
        /// Write the xml declaration.  This must be the first call after Open.
        /// </summary>
        internal override void WriteXmlDeclaration(XmlStandalone standalone)
        {
            // Ignore the xml declaration when building the cache
        }

        internal override void WriteXmlDeclaration(string xmldecl)
        {
            // Ignore the xml declaration when building the cache
        }

        /// <summary>
        /// Called as element node's children are about to be enumerated.
        /// </summary>
        internal override void StartElementContent()
        {
            Debug.Assert(this.pageParent[this.idxParent].NodeType == XPathNodeType.Element);
        }

        /// <summary>
        /// Build a namespace declaration node.  Attach it to an element parent, if one was previously constructed.
        /// All namespace declarations are linked together in an in-scope namespace tree.
        /// </summary>
        internal override void WriteNamespaceDeclaration(string prefix, string namespaceName)
        {
            XPathNode[] pageTemp, pageOverride, pageNew, pageOrig, pageCopy;
            int idxTemp, idxOverride, idxNew, idxOrig, idxCopy;
            Debug.Assert(this.idxSibling == 0 || this.pageSibling[this.idxSibling].NodeType == XPathNodeType.Attribute);
            Debug.Assert(!prefix.Equals("xmlns") && !namespaceName.Equals(XmlConst.ReservedNsXmlNs));
            Debug.Assert(this.idxParent == 0 || this.idxNmsp != 0);
            Debug.Assert(this.idxParent == 0 || this.pageParent[this.idxParent].NodeType == XPathNodeType.Element);

            if (this.atomizeNames)
                prefix = this.nameTable.Add(prefix);

            namespaceName = this.nameTable.Add(namespaceName);

            // Does the new namespace override a previous namespace node?
            pageOverride = this.pageNmsp;
            idxOverride = this.idxNmsp;
            while (idxOverride != 0)
            {
                if ((object)pageOverride[idxOverride].LocalName == (object)prefix)
                {
                    // Need to clone all namespaces up until the overridden node in order to bypass it
                    break;
                }
                idxOverride = pageOverride[idxOverride].GetSibling(out pageOverride);
            }

            // Create new namespace node and add it to front of namespace list
            idxNew = NewNamespaceNode(out pageNew, prefix, namespaceName, this.pageParent, this.idxParent);

            if (idxOverride != 0)
            {
                // Bypass overridden node by cloning nodes in list leading to it
                pageOrig = this.pageNmsp;
                idxOrig = this.idxNmsp;
                pageCopy = pageNew;
                idxCopy = idxNew;

                while (idxOrig != idxOverride || pageOrig != pageOverride)
                {
                    // Make a copy of the original namespace node
                    idxTemp = pageOrig[idxOrig].GetParent(out pageTemp);
                    idxTemp = NewNamespaceNode(out pageTemp, pageOrig[idxOrig].LocalName, pageOrig[idxOrig].Value, pageTemp, idxTemp);

                    // Attach copy to chain of copied nodes
                    pageCopy[idxCopy].SetSibling(this.infoTable, pageTemp, idxTemp);

                    // Position on the new copy
                    pageCopy = pageTemp;
                    idxCopy = idxTemp;

                    // Get next original sibling
                    idxOrig = pageOrig[idxOrig].GetSibling(out pageOrig);
                }

                // Link farther up in the original chain, just past the last overridden node
                idxOverride = pageOverride[idxOverride].GetSibling(out pageOverride);

                if (idxOverride != 0)
                    pageCopy[idxCopy].SetSibling(this.infoTable, pageOverride, idxOverride);
                else
                    Debug.Assert(prefix.Equals("xml"), "xmlns:xml namespace declaration should always be present in the list.");
            }
            else if (this.idxParent != 0)
            {
                // Link new node directly to last in-scope namespace.  No overrides necessary.
                pageNew[idxNew].SetSibling(this.infoTable, this.pageNmsp, this.idxNmsp);
            }
            else
            {
                // Floating namespace, so make this the root of the tree
                this.doc.SetRootNode(pageNew, idxNew);
            }

            if (this.idxParent != 0)
            {
                // If this is the first namespace on the current element,
                if (!this.pageParent[this.idxParent].HasNamespaceDecls)
                {
                    // Then save the last in-scope namespace on a stack so that EndElementNode can restore it.
                    this.stkNmsp.Push(new XPathNodeRef(this.pageNmsp, this.idxNmsp));

                    // Mark element parent as having namespace nodes declared on it
                    this.pageParent[this.idxParent].HasNamespaceDecls = true;
                }

                // New namespace is now last in-scope namespace
                this.pageNmsp = pageNew;
                this.idxNmsp = idxNew;
            }
        }


        //-----------------------------------------------
        // Custom Build Helper Methods
        //-----------------------------------------------

        /// <summary>
        /// Link "prev" element with "next" element, which has a "similar" name.  This increases the performance of searches by element name.
        /// </summary>
        private XPathNodeRef LinkSimilarElements(XPathNode[] pagePrev, int idxPrev, XPathNode[] pageNext, int idxNext)
        {
            // Set link on previous element
            if (pagePrev != null)
                pagePrev[idxPrev].SetSimilarElement(this.infoTable, pageNext, idxNext);

            // Add next element to index
            return new XPathNodeRef(pageNext, idxNext);
        }

        /// <summary>
        /// Helper method that constructs a new Namespace XPathNode.
        /// </summary>
        private int NewNamespaceNode(out XPathNode[] page, string prefix, string namespaceUri, XPathNode[] pageElem, int idxElem)
        {
            XPathNode[] pageNode;
            int idxNode, lineNumOffset, linePosOffset;
            XPathNodeInfoAtom info;
            Debug.Assert(pageElem == null || pageElem[idxElem].NodeType == XPathNodeType.Element);

            // Allocate a page slot for the new XPathNode
            this.nmspPageFact.AllocateSlot(out pageNode, out idxNode);

            // Compute node's line number information
            ComputeLineInfo(false, out lineNumOffset, out linePosOffset);

            // Obtain a XPathNodeInfoAtom object for this node
            info = this.infoTable.Create(prefix, string.Empty, string.Empty, string.Empty,
                                         pageElem, pageNode, null,
                                         this.doc, this.lineNumBase, this.linePosBase);

            // Initialize the new node
            pageNode[idxNode].Create(info, XPathNodeType.Namespace, idxElem);
            pageNode[idxNode].SetValue(namespaceUri);
            pageNode[idxNode].SetLineInfoOffsets(lineNumOffset, linePosOffset);

            page = pageNode;
            return idxNode;
        }

        /// <summary>
        /// Helper method that constructs a new XPathNode.
        /// </summary>
        private int NewNode(out XPathNode[] page, XPathNodeType xptyp, string localName, string namespaceUri, string prefix, string baseUri)
        {
            XPathNode[] pageNode;
            int idxNode, lineNumOffset, linePosOffset;
            XPathNodeInfoAtom info;
            Debug.Assert(xptyp != XPathNodeType.Namespace);

            // Allocate a page slot for the new XPathNode
            this.nodePageFact.AllocateSlot(out pageNode, out idxNode);

            // Compute node's line number information
            ComputeLineInfo(XPathNavigator.IsText(xptyp), out lineNumOffset, out linePosOffset);

            // Obtain a XPathNodeInfoAtom object for this node
            info = this.infoTable.Create(localName, namespaceUri, prefix, baseUri,
                                         this.pageParent, pageNode, pageNode,
                                         this.doc, this.lineNumBase, this.linePosBase);

            // Initialize the new node
            pageNode[idxNode].Create(info, xptyp, this.idxParent);
            pageNode[idxNode].SetLineInfoOffsets(lineNumOffset, linePosOffset);

            page = pageNode;
            return idxNode;
        }

        /// <summary>
        /// Compute current node's line number information.
        /// </summary>
        private void ComputeLineInfo(bool isTextNode, out int lineNumOffset, out int linePosOffset)
        {
            int lineNum, linePos;

            if (this.lineInfo == null)
            {
                lineNumOffset = 0;
                linePosOffset = 0;
                return;
            }

            // Get line number info from TextBlockBuilder if current node is a text node
            if (isTextNode)
            {
                lineNum = this.textBldr.LineNumber;
                linePos = this.textBldr.LinePosition;
            }
            else
            {
                Debug.Assert(this.lineInfo.HasLineInfo(), "HasLineInfo should have been checked before this.");
                lineNum = this.lineInfo.LineNumber;
                linePos = this.lineInfo.LinePosition;
            }

            lineNumOffset = lineNum - this.lineNumBase;
            if (lineNumOffset < 0 || lineNumOffset > XPathNode.MaxLineNumberOffset)
            {
                this.lineNumBase = lineNum;
                lineNumOffset = 0;
            }

            linePosOffset = linePos - this.linePosBase;
            if (linePosOffset < 0 || linePosOffset > XPathNode.MaxLinePositionOffset)
            {
                this.linePosBase = linePos;
                linePosOffset = 0;
            }
        }

        /// <summary>
        /// Add a sibling node.  If no previous sibling exists, add the node as the first child of the parent.
        /// If no parent exists, make this node the root of the document.
        /// </summary>
        private void AddSibling(XPathNodeType xptyp, string localName, string namespaceUri, string prefix, string baseUri)
        {
            XPathNode[] pageNew;
            int idxNew;
            Debug.Assert(xptyp != XPathNodeType.Root && xptyp != XPathNodeType.Namespace);

            if (this.textBldr.HasText)
                CachedTextNode();

            idxNew = NewNode(out pageNew, xptyp, localName, namespaceUri, prefix, baseUri);

            // this.idxParent is only 0 for the top-most node
            if (this.idxParent != 0)
            {
                // Set properties on parent
                this.pageParent[this.idxParent].SetParentProperties(xptyp);

                if (this.idxSibling == 0)
                {
                    // This is the first child of the parent (so should be allocated immediately after parent)
                    Debug.Assert(this.idxParent + 1 == idxNew || idxNew == 1);
                }
                else
                {
                    // There is already a previous sibling
                    this.pageSibling[this.idxSibling].SetSibling(this.infoTable, pageNew, idxNew);
                }
            }

            this.pageSibling = pageNew;
            this.idxSibling = idxNew;
        }

        /// <summary>
        /// Creates a text node from cached text parts.
        /// </summary>
        private void CachedTextNode()
        {
            TextBlockType textType;
            string text;
            Debug.Assert(this.textBldr.HasText || (this.idxSibling == 0 && this.idxParent == 0), "Cannot create empty text node unless it's a top-level text node.");
            Debug.Assert(this.idxSibling == 0 || !this.pageSibling[this.idxSibling].IsText, "Cannot create adjacent text nodes.");

            // Create a text node
            textType = this.textBldr.TextType;
            text = this.textBldr.ReadText();
            AddSibling((XPathNodeType)textType, string.Empty, string.Empty, string.Empty, string.Empty);
            this.pageSibling[this.idxSibling].SetValue(text);
        }

        /// <summary>
        /// Allocates pages of nodes for the XPathDocumentBuilder.  The initial pages and arrays are
        /// fairly small.  As each page fills, a new page that is twice as big is allocated.
        /// The max size of a page is 65536 nodes, since XPathNode indexes are 16-bits.
        /// </summary>
        private struct NodePageFactory
        {
            private XPathNode[] page;
            private XPathNodePageInfo pageInfo;
            private int pageSize;

            /// <summary>
            /// Allocates and returns the initial node page.
            /// </summary>
            public void Init(int initialPageSize)
            {
                // 0th slot: Index 0 is reserved to mean "null node".  Only use 0th slot to store PageInfo.
                this.pageSize = initialPageSize;
                this.page = new XPathNode[this.pageSize];
                this.pageInfo = new XPathNodePageInfo(null, 1);
                this.page[0].Create(this.pageInfo);
            }

            /// <summary>
            /// Return the page on which the next node will be allocated.
            /// </summary>
            public XPathNode[] NextNodePage
            {
                get { return this.page; }
            }

            /// <summary>
            /// Return the page index that the next node will be given.
            /// </summary>
            public int NextNodeIndex
            {
                get { return this.pageInfo.NodeCount; }
            }

            /// <summary>
            /// Allocate the next slot in the current node page.  Return a reference to the page and the index
            /// of the allocated slot.
            /// </summary>
            public void AllocateSlot(out XPathNode[] page, out int idx)
            {
                page = this.page;
                idx = this.pageInfo.NodeCount;

                // Allocate new page if necessary
                if (++this.pageInfo.NodeCount >= this.page.Length)
                {
                    if (this.pageSize < (1 << 16))
                    {
                        // New page shouldn't contain more slots than 16 bits can address
                        this.pageSize *= 2;
                    }
                    this.page = new XPathNode[this.pageSize];
                    this.pageInfo.NextPage = this.page;
                    this.pageInfo = new XPathNodePageInfo(page, this.pageInfo.PageNumber + 1);
                    this.page[0].Create(this.pageInfo);
                }
            }
        }

        /// <summary>
        /// This class concatenates adjacent text blocks and tracks TextBlockType and line number information.
        /// </summary>
        private struct TextBlockBuilder
        {
            private IXmlLineInfo lineInfo;
            private TextBlockType textType;
            private string text;
            private int lineNum, linePos;

            /// <summary>
            /// Constructor.
            /// </summary>
            public void Initialize(IXmlLineInfo lineInfo)
            {
                this.lineInfo = lineInfo;
                this.textType = TextBlockType.None;
            }

            /// <summary>
            /// Return the type of the cached text block.
            /// </summary>
            public TextBlockType TextType
            {
                get { return this.textType; }
            }

            /// <summary>
            /// Returns true if text has been cached.
            /// </summary>
            public bool HasText
            {
                get { return this.textType != TextBlockType.None; }
            }

            /// <summary>
            /// Returns the line number of the last text block to be cached.
            /// </summary>
            public int LineNumber
            {
                get { return this.lineNum; }
            }

            /// <summary>
            /// Returns the line position of the last text block to be cached.
            /// </summary>
            public int LinePosition
            {
                get { return this.linePos; }
            }

            /// <summary>
            /// Append a text block with the specified type.
            /// </summary>
            public void WriteTextBlock(string text, TextBlockType textType)
            {
                Debug.Assert((int)XPathNodeType.Text < (int)XPathNodeType.SignificantWhitespace);
                Debug.Assert((int)XPathNodeType.SignificantWhitespace < (int)XPathNodeType.Whitespace);

                if (text.Length != 0)
                {
                    if (this.textType == TextBlockType.None)
                    {
                        this.text = text;
                        this.textType = textType;

                        if (this.lineInfo != null)
                        {
                            this.lineNum = this.lineInfo.LineNumber;
                            this.linePos = this.lineInfo.LinePosition;
                        }
                    }
                    else
                    {
                        this.text = string.Concat(this.text, text);

                        // Determine whether text is Text, Whitespace, or SignificantWhitespace
                        if ((int)textType < (int)this.textType)
                            this.textType = textType;
                    }
                }
            }

            /// <summary>
            /// Read all cached text, or string.Empty if no text has been cached, and clear the text block type.
            /// </summary>
            public string ReadText()
            {
                if (this.textType == TextBlockType.None)
                    return string.Empty;

                this.textType = TextBlockType.None;
                return this.text;
            }
        }
    }
}
