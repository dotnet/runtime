// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
        private NodePageFactory _nodePageFact;   // Creates non-namespace node pages
        private NodePageFactory _nmspPageFact;   // Creates namespace node pages
        private TextBlockBuilder _textBldr;      // Concatenates adjacent text blocks

        private readonly Stack<XPathNodeRef> _stkNmsp;    // In-scope namespaces
        private XPathNodeInfoTable _infoTable;   // Atomization table for shared node information
        private XPathDocument _doc;              // Currently building document
        private IXmlLineInfo? _lineInfo;          // Line information provider
        private XmlNameTable _nameTable;         // Atomization table for all names in the document
        private bool _atomizeNames;              // True if all names should be atomized (false if they are pre-atomized)

        private XPathNode[] _pageNmsp;           // Page of last in-scope namespace node
        private int _idxNmsp;                    // Page index of last in-scope namespace node
        private XPathNode[]? _pageParent;         // Page of last parent-type node (Element or Root)
        private int _idxParent;                  // Page index of last parent-type node (Element or Root)
        private XPathNode[]? _pageSibling;        // Page of previous sibling node (may be null if no previous sibling)
        private int _idxSibling;                 // Page index of previous sibling node
        private int _lineNumBase;                // Line number from which offsets are computed
        private int _linePosBase;                // Line position from which offsets are computed

        private XmlQualifiedName? _idAttrName;    // Cached name of an ID attribute
        private Hashtable? _elemIdMap;            // Map from element name to ID attribute name
        private XPathNodeRef[] _elemNameIndex;   // Elements with the same name are linked together so that they can be searched quickly

        private const int ElementIndexSize = 64;

        /// <summary>
        /// Create a new XPathDocumentBuilder which creates nodes in "doc".
        /// </summary>
        public XPathDocumentBuilder(XPathDocument doc, IXmlLineInfo? lineInfo, string? baseUri, XPathDocument.LoadFlags flags)
        {
            // Allocate the initial node (for non-namespaces) page, and the initial namespace page
            _nodePageFact.Init(256);
            _nmspPageFact.Init(16);

            _stkNmsp = new Stack<XPathNodeRef>();

            Initialize(doc, lineInfo, baseUri, flags);
        }

        /// <summary>
        /// Start construction of a new document.  This must be called before any other methods are called.
        /// It may also be called after Close(), in order to build further documents.
        /// </summary>
        [MemberNotNull(nameof(_doc))]
        [MemberNotNull(nameof(_elemNameIndex))]
        [MemberNotNull(nameof(_infoTable))]
        [MemberNotNull(nameof(_nameTable))]
        [MemberNotNull(nameof(_pageNmsp))]
        public void Initialize(XPathDocument doc, IXmlLineInfo? lineInfo, string? baseUri, XPathDocument.LoadFlags flags)
        {
            XPathNode[] page;
            int idx;

            _doc = doc;
            _nameTable = doc.NameTable;
            _atomizeNames = (flags & XPathDocument.LoadFlags.AtomizeNames) != 0;
            _idxParent = _idxSibling = 0;
            _elemNameIndex = new XPathNodeRef[ElementIndexSize];

            // Prepare line number information
            _textBldr.Initialize(lineInfo);
            _lineInfo = lineInfo;
            _lineNumBase = 0;
            _linePosBase = 0;

            // Allocate the atomization table
            _infoTable = new XPathNodeInfoTable();

            // Allocate singleton collapsed text node
            idx = NewNode(out page, XPathNodeType.Text, string.Empty, string.Empty, string.Empty, string.Empty);
            _doc.SetCollapsedTextNode(page, idx);

            // Allocate xmlns:xml namespace node
            _idxNmsp = NewNamespaceNode(out _pageNmsp, _nameTable.Add("xml"), _nameTable.Add(XmlReservedNs.NsXml), null, 0);
            _doc.SetXmlNamespaceNode(_pageNmsp, _idxNmsp);

            if ((flags & XPathDocument.LoadFlags.Fragment) == 0)
            {
                // This tree has a document root node
                _idxParent = NewNode(out _pageParent, XPathNodeType.Root, string.Empty, string.Empty, string.Empty, baseUri);
                _doc.SetRootNode(_pageParent, _idxParent);
            }
            else
            {
                // This tree is an XQuery fragment (no document root node), so root will be next node in the current page
                _doc.SetRootNode(_nodePageFact.NextNodePage, _nodePageFact.NextNodeIndex);
            }
        }


        //-----------------------------------------------
        // XmlWriter interface
        //-----------------------------------------------

        /// <summary>
        /// XPathDocument ignores the DocType information.
        /// </summary>
        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
        }

        /// <summary>
        /// Shortcut for calling WriteStartElement with elemType == null.
        /// </summary>
        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            this.WriteStartElement(prefix, localName, ns, string.Empty);
        }

        /// <summary>
        /// Build an element node and attach it to its parent, if one exists.  Make the element the new parent node.
        /// </summary>
        public void WriteStartElement(string? prefix, string localName, string? ns, string? baseUri)
        {
            int hash;
            Debug.Assert(prefix != null && localName != null && ns != null && localName.Length != 0 && baseUri != null);

            if (_atomizeNames)
            {
                prefix = _nameTable.Add(prefix);
                localName = _nameTable.Add(localName);
                ns = _nameTable.Add(ns);
            }

            AddSibling(XPathNodeType.Element, localName, ns, prefix, baseUri);
            _pageParent = _pageSibling;
            _idxParent = _idxSibling;
            _idxSibling = 0;

            // Link elements with the same name together
            hash = (_pageParent[_idxParent].LocalNameHashCode & (ElementIndexSize - 1));
            _elemNameIndex[hash] = LinkSimilarElements(_elemNameIndex[hash].Page, _elemNameIndex[hash].Index, _pageParent, _idxParent);

            // If elements within this document might have IDs, then cache the name of the ID attribute, if one exists
            if (_elemIdMap != null)
                _idAttrName = (XmlQualifiedName?)_elemIdMap[new XmlQualifiedName(localName, prefix)];
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
            Debug.Assert(_pageParent != null && _pageParent[_idxParent].NodeType == XPathNodeType.Element);

            // If element has no content-typed children except for the one about to be added, then
            // its value is the same as its only text child's.
            if (!_pageParent[_idxParent].HasContentChild)
            {
                switch (_textBldr.TextType)
                {
                    case TextBlockType.Text:
                        // Collapsed text node can be created if text line number information can be encoded efficiently in parent node
                        if (_lineInfo != null)
                        {
                            // If collapsed text node is not on same line as parent, don't collapse text
                            if (_textBldr.LineNumber != _pageParent[_idxParent].LineNumber)
                                goto case TextBlockType.Whitespace;

                            // If position is not within 256 of parent, don't collapse text
                            int posDiff = _textBldr.LinePosition - _pageParent[_idxParent].LinePosition;
                            if (posDiff < 0 || posDiff > XPathNode.MaxCollapsedPositionOffset)
                                goto case TextBlockType.Whitespace;

                            // Set collapsed node line position offset
                            _pageParent[_idxParent].SetCollapsedLineInfoOffset(posDiff);
                        }

                        // Set collapsed node text
                        _pageParent[_idxParent].SetCollapsedValue(_textBldr.ReadText());
                        break;

                    case TextBlockType.SignificantWhitespace:
                    case TextBlockType.Whitespace:
                        // Create separate whitespace node
                        CachedTextNode();
                        _pageParent[_idxParent].SetValue(_pageSibling[_idxSibling].Value);
                        break;

                    default:
                        // Empty value, so don't create collapsed text node
                        _pageParent[_idxParent].SetEmptyValue(allowShortcutTag);
                        break;
                }
            }
            else
            {
                if (_textBldr.HasText)
                {
                    // Element's last child (one of several) is a text or whitespace node
                    CachedTextNode();
                }
            }

            // If namespaces were added to this element,
            if (_pageParent[_idxParent].HasNamespaceDecls)
            {
                // Add it to the document's element --> namespace mapping
                _doc.AddNamespace(_pageParent, _idxParent, _pageNmsp, _idxNmsp);

                // Restore the previous namespace chain
                nodeRef = _stkNmsp.Pop();
                _pageNmsp = nodeRef.Page;
                _idxNmsp = nodeRef.Index;
            }

            // Make parent of this element the current element
            _pageSibling = _pageParent;
            _idxSibling = _idxParent;
            _idxParent = _pageParent[_idxParent].GetParent(out _pageParent);
        }

        /// <summary>
        /// Shortcut for calling WriteStartAttribute with attrfType == null.
        /// </summary>
        public override void WriteStartAttribute(string? prefix, string localName, string? namespaceName)
        {
            Debug.Assert(namespaceName != null);
            Debug.Assert(prefix != null && !prefix.Equals("xmlns"));
            Debug.Assert(_idxParent == 0 || (_pageParent != null && _pageParent[_idxParent].NodeType == XPathNodeType.Element));
            Debug.Assert(_idxSibling == 0 || (_pageSibling != null && _pageSibling[_idxSibling].NodeType == XPathNodeType.Attribute));

            if (_atomizeNames)
            {
                prefix = _nameTable.Add(prefix);
                localName = _nameTable.Add(localName);
                namespaceName = _nameTable.Add(namespaceName);
            }

            AddSibling(XPathNodeType.Attribute, localName, namespaceName, prefix, string.Empty);
        }

        /// <summary>
        /// Attach the attribute's text or typed value to the previously constructed attribute node.
        /// </summary>
        public override void WriteEndAttribute()
        {
            Debug.Assert(_pageSibling != null);
            Debug.Assert(_pageSibling[_idxSibling].NodeType == XPathNodeType.Attribute);

            _pageSibling[_idxSibling].SetValue(_textBldr.ReadText());

            if (_idAttrName != null)
            {
                // If this is an ID attribute,
                if (_pageSibling[_idxSibling].LocalName == _idAttrName.Name &&
                    _pageSibling[_idxSibling].Prefix == _idAttrName.Namespace)
                {
                    // Then add its value to the idValueMap map
                    Debug.Assert(_idxParent != 0 && _pageParent != null, "ID attribute must have an element parent");
                    string? id = _pageSibling[_idxSibling].Value;
                    Debug.Assert(id != null);
                    _doc.AddIdElement(id, _pageParent, _idxParent);
                }
            }
        }

        /// <summary>
        /// Map CData text into regular text.
        /// </summary>
        public override void WriteCData(string? text)
        {
            WriteString(text, TextBlockType.Text);
        }

        /// <summary>
        /// Construct comment node.
        /// </summary>
        public override void WriteComment(string? text)
        {
            AddSibling(XPathNodeType.Comment, string.Empty, string.Empty, string.Empty, string.Empty);
            _pageSibling[_idxSibling].SetValue(text);
        }

        /// <summary>
        /// Shortcut for calling WriteProcessingInstruction with baseUri = string.Empty.
        /// </summary>
        public override void WriteProcessingInstruction(string name, string? text)
        {
            this.WriteProcessingInstruction(name, text, string.Empty);
        }

        /// <summary>
        /// Construct pi node.
        /// </summary>
        public void WriteProcessingInstruction(string name, string? text, string? baseUri)
        {
            if (_atomizeNames)
                name = _nameTable.Add(name);

            AddSibling(XPathNodeType.ProcessingInstruction, name, string.Empty, string.Empty, baseUri);
            _pageSibling[_idxSibling].SetValue(text);
        }

        /// <summary>
        /// Write a whitespace text block.
        /// </summary>
        public override void WriteWhitespace(string? ws)
        {
            WriteString(ws, TextBlockType.Whitespace);
        }

        /// <summary>
        /// Write an attribute or element text block.
        /// </summary>
        public override void WriteString(string? text)
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
        public void WriteString(string? text, TextBlockType textType)
        {
            _textBldr.WriteTextBlock(text, textType);
        }

        /// <summary>
        /// Cache does not handle entity references.
        /// </summary>
        public override void WriteEntityRef(string name)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Don't entitize, since the cache cannot represent character entities.
        /// </summary>
        public override void WriteCharEntity(char ch)
        {
            WriteString(char.ToString(ch), TextBlockType.Text);
        }

        /// <summary>
        /// Don't entitize, since the cache cannot represent character entities.
        /// </summary>
        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            ReadOnlySpan<char> chars = stackalloc char[] { highChar, lowChar };
            WriteString(new string(chars), TextBlockType.Text);
        }

        /// <summary>
        /// Signals the end of tree construction.
        /// </summary>
        public override void Close()
        {
            XPathNode[]? page;
            int idx;

            // If cached text exists, then create a text node at the top-level
            if (_textBldr.HasText)
                CachedTextNode();

            // If document does not yet contain nodes, then an empty text node must have been created
            idx = _doc.GetRootNode(out page);
            if (idx == _nodePageFact.NextNodeIndex && page == _nodePageFact.NextNodePage)
            {
                AddSibling(XPathNodeType.Text, string.Empty, string.Empty, string.Empty, string.Empty);
                _pageSibling[_idxSibling].SetValue(string.Empty);
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
            Debug.Assert(_pageParent != null && _pageParent[_idxParent].NodeType == XPathNodeType.Element);
        }

        /// <summary>
        /// Build a namespace declaration node.  Attach it to an element parent, if one was previously constructed.
        /// All namespace declarations are linked together in an in-scope namespace tree.
        /// </summary>
        internal override void WriteNamespaceDeclaration(string prefix, string namespaceName)
        {
            XPathNode[] pageTemp, pageNew, pageCopy;
            XPathNode[]? pageOverride, pageOrig;
            int idxTemp, idxOverride, idxNew, idxOrig, idxCopy;
            Debug.Assert(_idxSibling == 0 || (_pageSibling != null && _pageSibling[_idxSibling].NodeType == XPathNodeType.Attribute));
            Debug.Assert(!prefix.Equals("xmlns") && !namespaceName.Equals(XmlReservedNs.NsXmlNs));
            Debug.Assert(_idxParent == 0 || _idxNmsp != 0);
            Debug.Assert(_idxParent == 0 || (_pageParent != null && _pageParent[_idxParent].NodeType == XPathNodeType.Element));

            if (_atomizeNames)
                prefix = _nameTable.Add(prefix);

            namespaceName = _nameTable.Add(namespaceName);

            // Does the new namespace override a previous namespace node?
            pageOverride = _pageNmsp;
            idxOverride = _idxNmsp;
            while (idxOverride != 0)
            {
                if ((object)pageOverride![idxOverride].LocalName == (object)prefix)
                {
                    // Need to clone all namespaces up until the overridden node in order to bypass it
                    break;
                }
                idxOverride = pageOverride[idxOverride].GetSibling(out pageOverride);
            }

            // Create new namespace node and add it to front of namespace list
            idxNew = NewNamespaceNode(out pageNew, prefix, namespaceName, _pageParent, _idxParent);

            if (idxOverride != 0)
            {
                // Bypass overridden node by cloning nodes in list leading to it
                pageOrig = _pageNmsp;
                idxOrig = _idxNmsp;
                pageCopy = pageNew;
                idxCopy = idxNew;

                while (idxOrig != idxOverride || pageOrig != pageOverride)
                {
                    Debug.Assert(pageOrig != null);
                    // Make a copy of the original namespace node
                    idxTemp = pageOrig[idxOrig].GetParent(out pageTemp!);
                    idxTemp = NewNamespaceNode(out pageTemp, pageOrig[idxOrig].LocalName, pageOrig[idxOrig].Value, pageTemp, idxTemp);

                    // Attach copy to chain of copied nodes
                    pageCopy[idxCopy].SetSibling(_infoTable, pageTemp, idxTemp);

                    // Position on the new copy
                    pageCopy = pageTemp;
                    idxCopy = idxTemp;

                    // Get next original sibling
                    idxOrig = pageOrig[idxOrig].GetSibling(out pageOrig);
                }

                // Link farther up in the original chain, just past the last overridden node
                idxOverride = pageOverride![idxOverride].GetSibling(out pageOverride);

                if (idxOverride != 0)
                    pageCopy[idxCopy].SetSibling(_infoTable, pageOverride!, idxOverride);
                else
                    Debug.Assert(prefix.Equals("xml"), "xmlns:xml namespace declaration should always be present in the list.");
            }
            else if (_idxParent != 0)
            {
                // Link new node directly to last in-scope namespace.  No overrides necessary.
                pageNew[idxNew].SetSibling(_infoTable, _pageNmsp, _idxNmsp);
            }
            else
            {
                // Floating namespace, so make this the root of the tree
                _doc.SetRootNode(pageNew, idxNew);
            }

            if (_idxParent != 0)
            {
                Debug.Assert(_pageParent != null);
                // If this is the first namespace on the current element,
                if (!_pageParent[_idxParent].HasNamespaceDecls)
                {
                    // Then save the last in-scope namespace on a stack so that EndElementNode can restore it.
                    _stkNmsp.Push(new XPathNodeRef(_pageNmsp, _idxNmsp));

                    // Mark element parent as having namespace nodes declared on it
                    _pageParent[_idxParent].HasNamespaceDecls = true;
                }

                // New namespace is now last in-scope namespace
                _pageNmsp = pageNew;
                _idxNmsp = idxNew;
            }
        }


        //-----------------------------------------------
        // Custom Build Helper Methods
        //-----------------------------------------------

        /// <summary>
        /// Build ID lookup tables from the XSD schema or DTD.
        /// </summary>
        public void CreateIdTables(IDtdInfo dtdInfo)
        {
            // Extract the elements which has attribute defined as ID from the element declarations
            foreach (IDtdAttributeListInfo attrList in dtdInfo.GetAttributeLists())
            {
                IDtdAttributeInfo? idAttribute = attrList.LookupIdAttribute();
                if (idAttribute != null)
                {
                    if (_elemIdMap == null)
                        _elemIdMap = new Hashtable();

                    // Id was defined in DTD and DTD doesn't have notion of namespace so we should
                    // use prefix instead of namespace here.  Schema already does this for us.
                    _elemIdMap.Add(new XmlQualifiedName(attrList.LocalName, attrList.Prefix),
                                       new XmlQualifiedName(idAttribute.LocalName, idAttribute.Prefix));
                }
            }
        }

        /// <summary>
        /// Link "prev" element with "next" element, which has a "similar" name.  This increases the performance of searches by element name.
        /// </summary>
        private XPathNodeRef LinkSimilarElements(XPathNode[] pagePrev, int idxPrev, XPathNode[] pageNext, int idxNext)
        {
            // Set link on previous element
            if (pagePrev != null)
                pagePrev[idxPrev].SetSimilarElement(_infoTable, pageNext, idxNext);

            // Add next element to index
            return new XPathNodeRef(pageNext, idxNext);
        }

        /// <summary>
        /// Helper method that constructs a new Namespace XPathNode.
        /// </summary>
        private int NewNamespaceNode(out XPathNode[] page, string prefix, string? namespaceUri, XPathNode[]? pageElem, int idxElem)
        {
            XPathNode[] pageNode;
            int idxNode, lineNumOffset, linePosOffset;
            XPathNodeInfoAtom info;
            Debug.Assert(pageElem == null || pageElem[idxElem].NodeType == XPathNodeType.Element);

            // Allocate a page slot for the new XPathNode
            _nmspPageFact.AllocateSlot(out pageNode, out idxNode);

            // Compute node's line number information
            ComputeLineInfo(false, out lineNumOffset, out linePosOffset);

            // Obtain a XPathNodeInfoAtom object for this node
            info = _infoTable.Create(prefix, string.Empty, string.Empty, string.Empty,
                                         pageElem, pageNode, null,
                                         _doc, _lineNumBase, _linePosBase);

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
        private int NewNode(out XPathNode[] page, XPathNodeType xptyp, string localName, string namespaceUri, string prefix, string? baseUri)
        {
            XPathNode[] pageNode;
            int idxNode, lineNumOffset, linePosOffset;
            XPathNodeInfoAtom info;
            Debug.Assert(xptyp != XPathNodeType.Namespace);

            // Allocate a page slot for the new XPathNode
            _nodePageFact.AllocateSlot(out pageNode, out idxNode);

            // Compute node's line number information
            ComputeLineInfo(XPathNavigator.IsText(xptyp), out lineNumOffset, out linePosOffset);

            // Obtain a XPathNodeInfoAtom object for this node
            info = _infoTable.Create(localName, namespaceUri, prefix, baseUri,
                                         _pageParent, pageNode, pageNode,
                                         _doc, _lineNumBase, _linePosBase);

            // Initialize the new node
            pageNode[idxNode].Create(info, xptyp, _idxParent);
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

            if (_lineInfo == null)
            {
                lineNumOffset = 0;
                linePosOffset = 0;
                return;
            }

            // Get line number info from TextBlockBuilder if current node is a text node
            if (isTextNode)
            {
                lineNum = _textBldr.LineNumber;
                linePos = _textBldr.LinePosition;
            }
            else
            {
                Debug.Assert(_lineInfo.HasLineInfo(), "HasLineInfo should have been checked before this.");
                lineNum = _lineInfo.LineNumber;
                linePos = _lineInfo.LinePosition;
            }

            lineNumOffset = lineNum - _lineNumBase;
            if (lineNumOffset < 0 || lineNumOffset > XPathNode.MaxLineNumberOffset)
            {
                _lineNumBase = lineNum;
                lineNumOffset = 0;
            }

            linePosOffset = linePos - _linePosBase;
            if (linePosOffset < 0 || linePosOffset > XPathNode.MaxLinePositionOffset)
            {
                _linePosBase = linePos;
                linePosOffset = 0;
            }
        }

        /// <summary>
        /// Add a sibling node.  If no previous sibling exists, add the node as the first child of the parent.
        /// If no parent exists, make this node the root of the document.
        /// </summary>
        [MemberNotNull(nameof(_pageSibling))]
        private void AddSibling(XPathNodeType xptyp, string localName, string namespaceUri, string prefix, string? baseUri)
        {
            XPathNode[] pageNew;
            int idxNew;
            Debug.Assert(xptyp != XPathNodeType.Root && xptyp != XPathNodeType.Namespace);

            if (_textBldr.HasText)
                CachedTextNode();

            idxNew = NewNode(out pageNew, xptyp, localName, namespaceUri, prefix, baseUri);

            // this.idxParent is only 0 for the top-most node
            if (_idxParent != 0)
            {
                Debug.Assert(_pageParent != null);
                // Set properties on parent
                _pageParent[_idxParent].SetParentProperties(xptyp);

                if (_idxSibling == 0)
                {
                    // This is the first child of the parent (so should be allocated immediately after parent)
                    Debug.Assert(_idxParent + 1 == idxNew || idxNew == 1);
                }
                else
                {
                    Debug.Assert(_pageSibling != null);
                    // There is already a previous sibling
                    _pageSibling[_idxSibling].SetSibling(_infoTable, pageNew, idxNew);
                }
            }

            _pageSibling = pageNew;
            _idxSibling = idxNew;
        }

        /// <summary>
        /// Creates a text node from cached text parts.
        /// </summary>
        [MemberNotNull(nameof(_pageSibling))]
        private void CachedTextNode()
        {
            TextBlockType textType;
            string text;
            Debug.Assert(_textBldr.HasText || (_idxSibling == 0 && _idxParent == 0), "Cannot create empty text node unless it's a top-level text node.");
            Debug.Assert(_idxSibling == 0 || (_pageSibling != null && !_pageSibling[_idxSibling].IsText), "Cannot create adjacent text nodes.");

            // Create a text node
            textType = _textBldr.TextType;
            text = _textBldr.ReadText();
            AddSibling((XPathNodeType)textType, string.Empty, string.Empty, string.Empty, string.Empty);
            _pageSibling[_idxSibling].SetValue(text);
        }

        /// <summary>
        /// Allocates pages of nodes for the XPathDocumentBuilder.  The initial pages and arrays are
        /// fairly small.  As each page fills, a new page that is twice as big is allocated.
        /// The max size of a page is 65536 nodes, since XPathNode indexes are 16-bits.
        /// </summary>
        private struct NodePageFactory
        {
            private XPathNode[] _page;
            private XPathNodePageInfo _pageInfo;
            private int _pageSize;

            /// <summary>
            /// Allocates and returns the initial node page.
            /// </summary>
            public void Init(int initialPageSize)
            {
                // 0th slot: Index 0 is reserved to mean "null node".  Only use 0th slot to store PageInfo.
                _pageSize = initialPageSize;
                _page = new XPathNode[_pageSize];
                _pageInfo = new XPathNodePageInfo(null, 1);
                _page[0].Create(_pageInfo);
            }

            /// <summary>
            /// Return the page on which the next node will be allocated.
            /// </summary>
            public XPathNode[] NextNodePage
            {
                get { return _page; }
            }

            /// <summary>
            /// Return the page index that the next node will be given.
            /// </summary>
            public int NextNodeIndex
            {
                get { return _pageInfo.NodeCount; }
            }

            /// <summary>
            /// Allocate the next slot in the current node page.  Return a reference to the page and the index
            /// of the allocated slot.
            /// </summary>
            public void AllocateSlot(out XPathNode[] page, out int idx)
            {
                page = _page;
                idx = _pageInfo.NodeCount;

                // Allocate new page if necessary
                if (++_pageInfo.NodeCount >= _page.Length)
                {
                    if (_pageSize < (1 << 16))
                    {
                        // New page shouldn't contain more slots than 16 bits can address
                        _pageSize *= 2;
                    }
                    _page = new XPathNode[_pageSize];
                    _pageInfo.NextPage = _page;
                    _pageInfo = new XPathNodePageInfo(page, _pageInfo.PageNumber + 1);
                    _page[0].Create(_pageInfo);
                }
            }
        }

        /// <summary>
        /// This class concatenates adjacent text blocks and tracks TextBlockType and line number information.
        /// </summary>
        private struct TextBlockBuilder
        {
            private IXmlLineInfo? _lineInfo;
            private TextBlockType _textType;
            private string _text;
            private int _lineNum, _linePos;

            /// <summary>
            /// Constructor.
            /// </summary>
            public void Initialize(IXmlLineInfo? lineInfo)
            {
                _lineInfo = lineInfo;
                _textType = TextBlockType.None;
            }

            /// <summary>
            /// Return the type of the cached text block.
            /// </summary>
            public TextBlockType TextType
            {
                get { return _textType; }
            }

            /// <summary>
            /// Returns true if text has been cached.
            /// </summary>
            public bool HasText
            {
                get { return _textType != TextBlockType.None; }
            }

            /// <summary>
            /// Returns the line number of the last text block to be cached.
            /// </summary>
            public int LineNumber
            {
                get { return _lineNum; }
            }

            /// <summary>
            /// Returns the line position of the last text block to be cached.
            /// </summary>
            public int LinePosition
            {
                get { return _linePos; }
            }

            /// <summary>
            /// Append a text block with the specified type.
            /// </summary>
            public void WriteTextBlock(string? text, TextBlockType textType)
            {
                Debug.Assert((int)XPathNodeType.Text < (int)XPathNodeType.SignificantWhitespace);
                Debug.Assert((int)XPathNodeType.SignificantWhitespace < (int)XPathNodeType.Whitespace);

                if (!string.IsNullOrEmpty(text))
                {
                    if (_textType == TextBlockType.None)
                    {
                        _text = text;
                        _textType = textType;

                        if (_lineInfo != null)
                        {
                            _lineNum = _lineInfo.LineNumber;
                            _linePos = _lineInfo.LinePosition;
                        }
                    }
                    else
                    {
                        _text = string.Concat(_text, text);

                        // Determine whether text is Text, Whitespace, or SignificantWhitespace
                        if ((int)textType < (int)_textType)
                            _textType = textType;
                    }
                }
            }

            /// <summary>
            /// Read all cached text, or string.Empty if no text has been cached, and clear the text block type.
            /// </summary>
            public string ReadText()
            {
                if (_textType == TextBlockType.None)
                    return string.Empty;

                _textType = TextBlockType.None;
                return _text;
            }
        }
    }
}
