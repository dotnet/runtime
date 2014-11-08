// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Schema;
using System.Security;
using System.Globalization;
using System.Runtime.Versioning;
using System.Collections.Generic;

namespace System.Xml
{
    // Represents an entire document. An XmlDocument contains XML data.
    public class XmlDocument : XmlNode
    {
        private XmlImplementation implementation;
        private DomNameTable domNameTable; // hash table of XmlName
        private XmlLinkedNode lastChild;
        private XmlNamedNodeMap entities;
        private Dictionary<string, List<WeakReference<XmlElement>>> htElementIdMap;
        //This variable represents the actual loading status. Since, IsLoading will
        //be manipulated soemtimes for adding content to EntityReference this variable
        //has been added which would always represent the loading status of document.
        private bool actualLoadingStatus;

        private XmlNodeChangedEventHandler onNodeInsertingDelegate;
        private XmlNodeChangedEventHandler onNodeInsertedDelegate;
        private XmlNodeChangedEventHandler onNodeRemovingDelegate;
        private XmlNodeChangedEventHandler onNodeRemovedDelegate;
        private XmlNodeChangedEventHandler onNodeChangingDelegate;
        private XmlNodeChangedEventHandler onNodeChangedDelegate;

        // false if there are no ent-ref present, true if ent-ref nodes are or were present (i.e. if all ent-ref were removed, the doc will not clear this flag)
        internal bool fEntRefNodesPresent;
        internal bool fCDataNodesPresent;

        private bool preserveWhitespace;
        private bool isLoading;

        // special name strings for
        internal string strDocumentName;
        internal string strDocumentFragmentName;
        internal string strCommentName;
        internal string strTextName;
        internal string strCDataSectionName;
        internal string strEntityName;
        internal string strID;
        internal string strXmlns;
        internal string strXml;
        internal string strSpace;
        internal string strLang;
        internal string strEmpty;

        internal string strNonSignificantWhitespaceName;
        internal string strSignificantWhitespaceName;
        internal string strReservedXmlns;
        internal string strReservedXml;

        internal String baseURI;

        internal object objLock;

        static internal EmptyEnumerator EmptyEnumerator = new EmptyEnumerator();

        // Initializes a new instance of the XmlDocument class.
        public XmlDocument() : this(new XmlImplementation())
        {
        }

        // Initializes a new instance
        // of the XmlDocument class with the specified XmlNameTable.
        public XmlDocument(XmlNameTable nt) : this(new XmlImplementation(nt))
        {
        }

        protected internal XmlDocument(XmlImplementation imp) : base()
        {
            implementation = imp;
            domNameTable = new DomNameTable(this);

            // force the following string instances to be default in the nametable
            XmlNameTable nt = this.NameTable;
            nt.Add(string.Empty);
            strDocumentName = nt.Add("#document");
            strDocumentFragmentName = nt.Add("#document-fragment");
            strCommentName = nt.Add("#comment");
            strTextName = nt.Add("#text");
            strCDataSectionName = nt.Add("#cdata-section");
            strEntityName = nt.Add("#entity");
            strID = nt.Add("id");
            strNonSignificantWhitespaceName = nt.Add("#whitespace");
            strSignificantWhitespaceName = nt.Add("#significant-whitespace");
            strXmlns = nt.Add("xmlns");
            strXml = nt.Add("xml");
            strSpace = nt.Add("space");
            strLang = nt.Add("lang");
            strReservedXmlns = nt.Add(XmlConst.ReservedNsXmlNs);
            strReservedXml = nt.Add(XmlConst.ReservedNsXml);
            strEmpty = nt.Add(String.Empty);
            baseURI = String.Empty;

            objLock = new object();
        }

        // NOTE: This does not correctly check start name char, but we cannot change it since it would be a breaking change.
        internal static void CheckName(String name)
        {
            int endPos = ValidateNames.ParseNmtoken(name, 0);
            if (endPos < name.Length)
            {
                throw new XmlException(SR.Format(SR.Xml_BadNameChar, XmlExceptionHelper.BuildCharExceptionArgs(name, endPos)));
            }
        }

        internal XmlName AddXmlName(string prefix, string localName, string namespaceURI)
        {
            XmlName n = domNameTable.AddName(prefix, localName, namespaceURI);
            Debug.Assert((prefix == null) ? (n.Prefix.Length == 0) : (prefix == n.Prefix));
            Debug.Assert(n.LocalName == localName);
            Debug.Assert((namespaceURI == null) ? (n.NamespaceURI.Length == 0) : (n.NamespaceURI == namespaceURI));
            return n;
        }

        internal XmlName GetXmlName(string prefix, string localName, string namespaceURI)
        {
            XmlName n = domNameTable.GetName(prefix, localName, namespaceURI);
            Debug.Assert(n == null || ((prefix == null) ? (n.Prefix.Length == 0) : (prefix == n.Prefix)));
            Debug.Assert(n == null || n.LocalName == localName);
            Debug.Assert(n == null || ((namespaceURI == null) ? (n.NamespaceURI.Length == 0) : (n.NamespaceURI == namespaceURI)));
            return n;
        }

        internal XmlName AddAttrXmlName(string prefix, string localName, string namespaceURI)
        {
            XmlName xmlName = AddXmlName(prefix, localName, namespaceURI);
            Debug.Assert((prefix == null) ? (xmlName.Prefix.Length == 0) : (prefix == xmlName.Prefix));
            Debug.Assert(xmlName.LocalName == localName);
            Debug.Assert((namespaceURI == null) ? (xmlName.NamespaceURI.Length == 0) : (xmlName.NamespaceURI == namespaceURI));

            if (!this.IsLoading)
            {
                // Use atomized versions instead of prefix, localName and nsURI
                object oPrefix = xmlName.Prefix;
                object oNamespaceURI = xmlName.NamespaceURI;
                object oLocalName = xmlName.LocalName;
                if ((oPrefix == (object)strXmlns || (oPrefix == (object)strEmpty && oLocalName == (object)strXmlns)) ^ (oNamespaceURI == (object)strReservedXmlns))
                    throw new ArgumentException(SR.Format(SR.Xdom_Attr_Reserved_XmlNS, namespaceURI));
            }
            return xmlName;
        }

        // This function returns null because it is implication of removing schema.
        // If removed more methods would have to be removed as well and it would make adding schema back much harder. 
        internal XmlName GetIDInfoByElement(XmlName eleName)
        {
            return null;
        }

        private WeakReference<XmlElement> GetElement(List<WeakReference<XmlElement>> elementList, XmlElement elem)
        {
            List<WeakReference<XmlElement>> gcElemRefs = new List<WeakReference<XmlElement>>();
            foreach (WeakReference<XmlElement> elemRef in elementList)
            {
                XmlElement target;
                if (!elemRef.TryGetTarget(out target))
                    //take notes on the garbage collected nodes
                    gcElemRefs.Add(elemRef);
                else
                {
                    if (target == elem)
                        return elemRef;
                }
            }
            //Clear out the gced elements
            foreach (WeakReference<XmlElement> elemRef in gcElemRefs)
                elementList.Remove(elemRef);
            return null;
        }

        internal void AddElementWithId(string id, XmlElement elem)
        {
            if (htElementIdMap == null || !htElementIdMap.ContainsKey(id))
            {
                if (htElementIdMap == null)
                    htElementIdMap = new Dictionary<string, List<WeakReference<XmlElement>>>();
                List<WeakReference<XmlElement>> elementList = new List<WeakReference<XmlElement>>();
                elementList.Add(new WeakReference<XmlElement>(elem));
                htElementIdMap.Add(id, elementList);
            }
            else
            {
                // there are other element(s) that has the same id
                List<WeakReference<XmlElement>> elementList = htElementIdMap[id];
                if (GetElement(elementList, elem) == null)
                    elementList.Add(new WeakReference<XmlElement>(elem));
            }
        }

        internal void RemoveElementWithId(string id, XmlElement elem)
        {
            if (htElementIdMap != null && htElementIdMap.ContainsKey(id))
            {
                List<WeakReference<XmlElement>> elementList = htElementIdMap[id];
                WeakReference<XmlElement> elemRef = GetElement(elementList, elem);
                if (elemRef != null)
                {
                    elementList.Remove(elemRef);
                    if (elementList.Count == 0)
                        htElementIdMap.Remove(id);
                }
            }
        }


        // Creates a duplicate of this node.
        public override XmlNode CloneNode(bool deep)
        {
            XmlDocument clone = Implementation.CreateDocument();
            clone.SetBaseURI(this.baseURI);
            if (deep)
                clone.ImportChildren(this, clone, deep);

            return clone;
        }

        // Gets the type of the current node.
        public override XmlNodeType NodeType
        {
            get { return XmlNodeType.Document; }
        }

        public override XmlNode ParentNode
        {
            get { return null; }
        }

        // Gets the node for the DOCTYPE declaration.
        internal virtual XmlDocumentType DocumentType
        {
            get { return (XmlDocumentType)FindChild(XmlNodeType.DocumentType); }
        }

        internal virtual XmlDeclaration Declaration
        {
            get
            {
                if (HasChildNodes)
                {
                    XmlDeclaration dec = FirstChild as XmlDeclaration;
                    return dec;
                }
                return null;
            }
        }

        // Gets the XmlImplementation object for this document.
        public XmlImplementation Implementation
        {
            get { return this.implementation; }
        }

        // Gets the name of the node.
        public override String Name
        {
            get { return strDocumentName; }
        }

        // Gets the name of the current node without the namespace prefix.
        public override String LocalName
        {
            get { return strDocumentName; }
        }

        // Gets the root XmlElement for the document.
        public XmlElement DocumentElement
        {
            get { return (XmlElement)FindChild(XmlNodeType.Element); }
        }

        internal override bool IsContainer
        {
            get { return true; }
        }

        internal override XmlLinkedNode LastNode
        {
            get { return lastChild; }
            set { lastChild = value; }
        }

        // Gets the XmlDocument that contains this node.
        public override XmlDocument OwnerDocument
        {
            get { return null; }
        }

        internal override bool IsValidChildType(XmlNodeType type)
        {
            switch (type)
            {
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.Comment:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    return true;

                case XmlNodeType.DocumentType:
                    if (DocumentType != null)
                        throw new InvalidOperationException(SR.Xdom_DualDocumentTypeNode);
                    return true;

                case XmlNodeType.Element:
                    if (DocumentElement != null)
                        throw new InvalidOperationException(SR.Xdom_DualDocumentElementNode);
                    return true;

                case XmlNodeType.XmlDeclaration:
                    if (Declaration != null)
                        throw new InvalidOperationException(SR.Xdom_DualDeclarationNode);
                    return true;

                default:
                    return false;
            }
        }
        // the function examines all the siblings before the refNode
        //  if any of the nodes has type equals to "nt", return true; otherwise, return false;
        private bool HasNodeTypeInPrevSiblings(XmlNodeType nt, XmlNode refNode)
        {
            if (refNode == null)
                return false;

            XmlNode node = null;
            if (refNode.ParentNode != null)
                node = refNode.ParentNode.FirstChild;
            while (node != null)
            {
                if (node.NodeType == nt)
                    return true;
                if (node == refNode)
                    break;
                node = node.NextSibling;
            }
            return false;
        }

        // the function examines all the siblings after the refNode
        //  if any of the nodes has the type equals to "nt", return true; otherwise, return false;
        private bool HasNodeTypeInNextSiblings(XmlNodeType nt, XmlNode refNode)
        {
            XmlNode node = refNode;
            while (node != null)
            {
                if (node.NodeType == nt)
                    return true;
                node = node.NextSibling;
            }
            return false;
        }

        internal override bool CanInsertBefore(XmlNode newChild, XmlNode refChild)
        {
            if (refChild == null)
                refChild = FirstChild;

            if (refChild == null)
                return true;

            switch (newChild.NodeType)
            {
                case XmlNodeType.XmlDeclaration:
                    return (refChild == FirstChild);

                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.Comment:
                    return refChild.NodeType != XmlNodeType.XmlDeclaration;

                case XmlNodeType.DocumentType:
                    {
                        if (refChild.NodeType != XmlNodeType.XmlDeclaration)
                        {
                            //if refChild is not the XmlDeclaration node, only need to go through the sibling before and including refChild to
                            //  make sure no Element ( rootElem node ) before the current position
                            return !HasNodeTypeInPrevSiblings(XmlNodeType.Element, refChild.PreviousSibling);
                        }
                    }
                    break;

                case XmlNodeType.Element:
                    {
                        if (refChild.NodeType != XmlNodeType.XmlDeclaration)
                        {
                            //if refChild is not the XmlDeclaration node, only need to go through the siblings after and including the refChild to
                            //  make sure no DocType node and XmlDeclaration node after the current posistion.
                            return !HasNodeTypeInNextSiblings(XmlNodeType.DocumentType, refChild);
                        }
                    }
                    break;
            }

            return false;
        }

        internal override bool CanInsertAfter(XmlNode newChild, XmlNode refChild)
        {
            if (refChild == null)
                refChild = LastChild;

            if (refChild == null)
                return true;

            switch (newChild.NodeType)
            {
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.Comment:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    return true;

                case XmlNodeType.DocumentType:
                    {
                        //we will have to go through all the siblings before the refChild just to make sure no Element node ( rootElem )
                        //  before the current position
                        return !HasNodeTypeInPrevSiblings(XmlNodeType.Element, refChild);
                    }

                case XmlNodeType.Element:
                    {
                        return !HasNodeTypeInNextSiblings(XmlNodeType.DocumentType, refChild.NextSibling);
                    }
            }

            return false;
        }

        // Creates an XmlAttribute with the specified name.
        public XmlAttribute CreateAttribute(String name)
        {
            String prefix = String.Empty;
            String localName = String.Empty;
            String namespaceURI = String.Empty;

            SplitName(name, out prefix, out localName);

            SetDefaultNamespace(prefix, localName, ref namespaceURI);

            return CreateAttribute(prefix, localName, namespaceURI);
        }

        internal void SetDefaultNamespace(String prefix, String localName, ref String namespaceURI)
        {
            if (prefix == strXmlns || (prefix.Length == 0 && localName == strXmlns))
            {
                namespaceURI = strReservedXmlns;
            }
            else if (prefix == strXml)
            {
                namespaceURI = strReservedXml;
            }
        }

        // Creates a XmlCDataSection containing the specified data.
        public virtual XmlCDataSection CreateCDataSection(String data)
        {
            fCDataNodesPresent = true;
            return new XmlCDataSection(data, this);
        }

        // Creates an XmlComment containing the specified data.
        public virtual XmlComment CreateComment(String data)
        {
            return new XmlComment(data, this);
        }

        // Returns a new XmlDocumentType object.
        internal virtual XmlDocumentType CreateDocumentType(string name, string publicId, string systemId, string internalSubset)
        {
            return new XmlDocumentType(name, publicId, systemId, internalSubset, this);
        }

        // Creates an XmlDocumentFragment.
        public virtual XmlDocumentFragment CreateDocumentFragment()
        {
            return new XmlDocumentFragment(this);
        }

        // Creates an element with the specified name.
        public XmlElement CreateElement(String name)
        {
            string prefix = String.Empty;
            string localName = String.Empty;
            SplitName(name, out prefix, out localName);
            return CreateElement(prefix, localName, string.Empty);
        }

        // Creates an XmlEntityReference with the specified name.
        internal virtual XmlEntityReference CreateEntityReference(String name)
        {
            return new XmlEntityReference(name, this);
        }

        // Creates a XmlProcessingInstruction with the specified name
        // and data strings.
        public virtual XmlProcessingInstruction CreateProcessingInstruction(String target, String data)
        {
            return new XmlProcessingInstruction(target, data, this);
        }

        // Creates a XmlDeclaration node with the specified values.
        public virtual XmlDeclaration CreateXmlDeclaration(String version, string encoding, string standalone)
        {
            return new XmlDeclaration(version, encoding, standalone, this);
        }

        // Creates an XmlText with the specified text.
        public virtual XmlText CreateTextNode(String text)
        {
            return new XmlText(text, this);
        }

        // Creates a XmlSignificantWhitespace node.
        public virtual XmlSignificantWhitespace CreateSignificantWhitespace(string text)
        {
            return new XmlSignificantWhitespace(text, this);
        }

        // Creates a XmlWhitespace node.
        public virtual XmlWhitespace CreateWhitespace(string text)
        {
            return new XmlWhitespace(text, this);
        }

        // Returns an XmlNodeList containing
        // a list of all descendant elements that match the specified name.
        public virtual XmlNodeList GetElementsByTagName(String name)
        {
            return new XmlElementList(this, name);
        }

        // DOM Level 2

        // Creates an XmlAttribute with the specified LocalName
        // and NamespaceURI.
        public XmlAttribute CreateAttribute(String qualifiedName, String namespaceURI)
        {
            string prefix = String.Empty;
            string localName = String.Empty;

            SplitName(qualifiedName, out prefix, out localName);
            return CreateAttribute(prefix, localName, namespaceURI);
        }

        // Creates an XmlElement with the specified LocalName and
        // NamespaceURI.
        public XmlElement CreateElement(String qualifiedName, String namespaceURI)
        {
            string prefix = String.Empty;
            string localName = String.Empty;
            SplitName(qualifiedName, out prefix, out localName);
            return CreateElement(prefix, localName, namespaceURI);
        }

        // Returns a XmlNodeList containing
        // a list of all descendant elements that match the specified name.
        public virtual XmlNodeList GetElementsByTagName(String localName, String namespaceURI)
        {
            return new XmlElementList(this, localName, namespaceURI);
        }

        // Returns the XmlElement with the specified ID.
        internal virtual XmlElement GetElementById(string elementId)
        {
            if (htElementIdMap != null)
            {
                List<WeakReference<XmlElement>> elementList;
                if (htElementIdMap.TryGetValue(elementId, out elementList))
                {
                    foreach (WeakReference<XmlElement> elemRef in elementList)
                    {
                        XmlElement elem;
                        if (elemRef.TryGetTarget(out elem))
                        {
                            if (elem != null
                                && elem.IsConnected())
                                return elem;
                        }
                    }
                }
            }
            return null;
        }

        // Imports a node from another document to this document.
        public virtual XmlNode ImportNode(XmlNode node, bool deep)
        {
            return ImportNodeInternal(node, deep);
        }

        private XmlNode ImportNodeInternal(XmlNode node, bool deep)
        {
            XmlNode newNode = null;

            if (node == null)
            {
                throw new InvalidOperationException(SR.Xdom_Import_NullNode);
            }
            else
            {
                switch (node.NodeType)
                {
                    case XmlNodeType.Element:
                        newNode = CreateElement(node.Prefix, node.LocalName, node.NamespaceURI);
                        ImportAttributes(node, newNode);
                        if (deep)
                            ImportChildren(node, newNode, deep);
                        break;

                    case XmlNodeType.Attribute:
                        Debug.Assert(((XmlAttribute)node).Specified);
                        newNode = CreateAttribute(node.Prefix, node.LocalName, node.NamespaceURI);
                        ImportChildren(node, newNode, true);
                        break;

                    case XmlNodeType.Text:
                        newNode = CreateTextNode(node.Value);
                        break;
                    case XmlNodeType.Comment:
                        newNode = CreateComment(node.Value);
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        newNode = CreateProcessingInstruction(node.Name, node.Value);
                        break;
                    case XmlNodeType.XmlDeclaration:
                        XmlDeclaration decl = (XmlDeclaration)node;
                        newNode = CreateXmlDeclaration(decl.Version, decl.Encoding, decl.Standalone);
                        break;
                    case XmlNodeType.CDATA:
                        newNode = CreateCDataSection(node.Value);
                        break;
                    case XmlNodeType.DocumentType:
                        XmlDocumentType docType = (XmlDocumentType)node;
                        newNode = CreateDocumentType(docType.Name, docType.PublicId, docType.SystemId, docType.InternalSubset);
                        break;
                    case XmlNodeType.DocumentFragment:
                        newNode = CreateDocumentFragment();
                        if (deep)
                            ImportChildren(node, newNode, deep);
                        break;

                    case XmlNodeType.EntityReference:
                        newNode = CreateEntityReference(node.Name);
                        // we don't import the children of entity reference because they might result in different
                        // children nodes given different namesapce context in the new document.
                        break;

                    case XmlNodeType.Whitespace:
                        newNode = CreateWhitespace(node.Value);
                        break;

                    case XmlNodeType.SignificantWhitespace:
                        newNode = CreateSignificantWhitespace(node.Value);
                        break;

                    default:
                        throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, SR.Xdom_Import, node.NodeType.ToString()));
                }
            }

            return newNode;
        }

        private void ImportAttributes(XmlNode fromElem, XmlNode toElem)
        {
            int cAttr = fromElem.Attributes.Count;
            for (int iAttr = 0; iAttr < cAttr; iAttr++)
            {
                if (fromElem.Attributes[iAttr].Specified)
                    toElem.Attributes.SetNamedItem(ImportNodeInternal(fromElem.Attributes[iAttr], true));
            }
        }

        private void ImportChildren(XmlNode fromNode, XmlNode toNode, bool deep)
        {
            Debug.Assert(toNode.NodeType != XmlNodeType.EntityReference);
            for (XmlNode n = fromNode.FirstChild; n != null; n = n.NextSibling)
            {
                toNode.AppendChild(ImportNodeInternal(n, deep));
            }
        }

        // Microsoft extensions

        // Gets the XmlNameTable associated with this
        // implementation.
        public XmlNameTable NameTable
        {
            get { return implementation.NameTable; }
        }

        // Creates a XmlAttribute with the specified Prefix, LocalName,
        // and NamespaceURI.
        public virtual XmlAttribute CreateAttribute(string prefix, string localName, string namespaceURI)
        {
            return new XmlAttribute(AddAttrXmlName(prefix, localName, namespaceURI), this);
        }

        internal virtual XmlAttribute CreateDefaultAttribute(string prefix, string localName, string namespaceURI)
        {
            return new XmlUnspecifiedAttribute(prefix, localName, namespaceURI, this);
        }

        public virtual XmlElement CreateElement(string prefix, string localName, string namespaceURI)
        {
            XmlElement elem = new XmlElement(AddXmlName(prefix, localName, namespaceURI), true, this);
            return elem;
        }

        // Gets or sets a value indicating whether to preserve whitespace.
        public bool PreserveWhitespace
        {
            get { return preserveWhitespace; }
            set { preserveWhitespace = value; }
        }

        // Gets a value indicating whether the node is read-only.
        public override bool IsReadOnly
        {
            get { return false; }
        }

        internal XmlNamedNodeMap Entities
        {
            get
            {
                if (entities == null)
                    entities = new XmlNamedNodeMap(this);
                return entities;
            }
        }

        internal bool IsLoading
        {
            get { return isLoading; }
            set { isLoading = value; }
        }

        internal bool ActualLoadingStatus
        {
            get { return actualLoadingStatus; }
        }


        // Creates a XmlNode with the specified XmlNodeType, Prefix, Name, and NamespaceURI.
        public virtual XmlNode CreateNode(XmlNodeType type, string prefix, string name, string namespaceURI)
        {
            switch (type)
            {
                case XmlNodeType.Element:
                    if (prefix != null)
                        return CreateElement(prefix, name, namespaceURI);
                    else
                        return CreateElement(name, namespaceURI);

                case XmlNodeType.Attribute:
                    if (prefix != null)
                        return CreateAttribute(prefix, name, namespaceURI);
                    else
                        return CreateAttribute(name, namespaceURI);

                case XmlNodeType.Text:
                    return CreateTextNode(string.Empty);

                case XmlNodeType.CDATA:
                    return CreateCDataSection(string.Empty);

                case XmlNodeType.EntityReference:
                    return CreateEntityReference(name);

                case XmlNodeType.ProcessingInstruction:
                    return CreateProcessingInstruction(name, string.Empty);

                case XmlNodeType.XmlDeclaration:
                    return CreateXmlDeclaration("1.0", null, null);

                case XmlNodeType.Comment:
                    return CreateComment(string.Empty);

                case XmlNodeType.DocumentFragment:
                    return CreateDocumentFragment();

                case XmlNodeType.DocumentType:
                    return CreateDocumentType(name, string.Empty, string.Empty, string.Empty);

                case XmlNodeType.Document:
                    return new XmlDocument();

                case XmlNodeType.SignificantWhitespace:
                    return CreateSignificantWhitespace(string.Empty);

                case XmlNodeType.Whitespace:
                    return CreateWhitespace(string.Empty);

                default:
                    throw new ArgumentException(SR.Format(SR.Arg_CannotCreateNode, type));
            }
        }

        // Creates an XmlNode with the specified node type, Name, and
        // NamespaceURI.
        public virtual XmlNode CreateNode(string nodeTypeString, string name, string namespaceURI)
        {
            return CreateNode(ConvertToNodeType(nodeTypeString), name, namespaceURI);
        }

        // Creates an XmlNode with the specified XmlNodeType, Name, and
        // NamespaceURI.
        public virtual XmlNode CreateNode(XmlNodeType type, string name, string namespaceURI)
        {
            return CreateNode(type, null, name, namespaceURI);
        }

        // Creates an XmlNode object based on the information in the XmlReader.
        // The reader must be positioned on a node or attribute.
        public virtual XmlNode ReadNode(XmlReader reader)
        {
            XmlNode node = null;
            try
            {
                IsLoading = true;
                XmlLoader loader = new XmlLoader();
                node = loader.ReadCurrentNode(this, reader);
            }
            finally
            {
                IsLoading = false;
            }
            return node;
        }

        internal XmlNodeType ConvertToNodeType(string nodeTypeString)
        {
            if (nodeTypeString == "element")
            {
                return XmlNodeType.Element;
            }
            else if (nodeTypeString == "attribute")
            {
                return XmlNodeType.Attribute;
            }
            else if (nodeTypeString == "text")
            {
                return XmlNodeType.Text;
            }
            else if (nodeTypeString == "cdatasection")
            {
                return XmlNodeType.CDATA;
            }
            else if (nodeTypeString == "entityreference")
            {
                return XmlNodeType.EntityReference;
            }
            else if (nodeTypeString == "entity")
            {
                return XmlNodeType.Entity;
            }
            else if (nodeTypeString == "processinginstruction")
            {
                return XmlNodeType.ProcessingInstruction;
            }
            else if (nodeTypeString == "comment")
            {
                return XmlNodeType.Comment;
            }
            else if (nodeTypeString == "document")
            {
                return XmlNodeType.Document;
            }
            else if (nodeTypeString == "documenttype")
            {
                return XmlNodeType.DocumentType;
            }
            else if (nodeTypeString == "documentfragment")
            {
                return XmlNodeType.DocumentFragment;
            }
            else if (nodeTypeString == "notation")
            {
                return XmlNodeType.Notation;
            }
            else if (nodeTypeString == "significantwhitespace")
            {
                return XmlNodeType.SignificantWhitespace;
            }
            else if (nodeTypeString == "whitespace")
            {
                return XmlNodeType.Whitespace;
            }
            throw new ArgumentException(SR.Format(SR.Xdom_Invalid_NT_String, nodeTypeString));
        }

        public virtual void Load(Stream inStream)
        {
            XmlReader reader = XmlReader.Create(inStream);
            try
            {
                Load(reader);
            }
            finally
            {
                reader.Dispose();
            }
        }

        // Loads the XML document from the specified TextReader.
        public virtual void Load(TextReader txtReader)
        {
            XmlReader reader = XmlReader.Create(txtReader);
            try
            {
                Load(reader);
            }
            finally
            {
                reader.Dispose();
            }
        }

        // Loads the XML document from the specified XmlReader.
        public virtual void Load(XmlReader reader)
        {
            try
            {
                IsLoading = true;
                actualLoadingStatus = true;
                RemoveAll();
                fEntRefNodesPresent = false;
                fCDataNodesPresent = false;

                XmlLoader loader = new XmlLoader();
                loader.Load(this, reader, preserveWhitespace);
            }
            finally
            {
                IsLoading = false;
                actualLoadingStatus = false;
            }
        }

        // Loads the XML document from the specified string.
        public virtual void LoadXml(string xml)
        {
            XmlReader reader = XmlReader.Create(new StringReader(xml));
            try
            {
                Load(reader);
            }
            finally
            {
                reader.Dispose();
            }
        }

        //TextEncoding is the one from XmlDeclaration if there is any
        internal Encoding TextEncoding
        {
            get
            {
                if (Declaration != null)
                {
                    string value = Declaration.Encoding;
                    if (value.Length > 0)
                    {
                        return System.Text.Encoding.GetEncoding(value);
                    }
                }
                return null;
            }
        }

        public override string InnerText
        {
            set
            {
                throw new InvalidOperationException(SR.Xdom_Document_Innertext);
            }
        }

        public override string InnerXml
        {
            get
            {
                return base.InnerXml;
            }
            set
            {
                LoadXml(value);
            }
        }

        //Saves out the to the file with exact content in the XmlDocument.
        public virtual void Save(Stream outStream)
        {
            XmlDOMTextWriter xw = new XmlDOMTextWriter(outStream, TextEncoding);
            if (preserveWhitespace == false)
                xw.Formatting = Formatting.Indented;
            WriteTo(xw);
            xw.Flush();
        }

        // Saves the XML document to the specified TextWriter.
        //
        //Saves out the file with xmldeclaration which has encoding value equal to
        //that of textwriter's encoding
        public virtual void Save(TextWriter writer)
        {
            XmlDOMTextWriter xw = new XmlDOMTextWriter(writer);
            if (preserveWhitespace == false)
                xw.Formatting = Formatting.Indented;
            Save(xw);
        }

        // Saves the XML document to the specified XmlWriter.
        // 
        //Saves out the file with xmldeclaration which has encoding value equal to
        //that of textwriter's encoding
        public virtual void Save(XmlWriter w)
        {
            XmlNode n = this.FirstChild;
            if (n == null)
                return;
            if (w.WriteState == WriteState.Start)
            {
                if (n is XmlDeclaration)
                {
                    if (Standalone.Length == 0)
                        w.WriteStartDocument();
                    else if (Standalone == "yes")
                        w.WriteStartDocument(true);
                    else if (Standalone == "no")
                        w.WriteStartDocument(false);
                    n = n.NextSibling;
                }
                else
                {
                    w.WriteStartDocument();
                }
            }
            while (n != null)
            {
                n.WriteTo(w);
                n = n.NextSibling;
            }
            w.Flush();
        }

        // Saves the node to the specified XmlWriter.
        // 
        //Writes out the to the file with exact content in the XmlDocument.
        public override void WriteTo(XmlWriter w)
        {
            WriteContentTo(w);
        }

        // Saves all the children of the node to the specified XmlWriter.
        // 
        //Writes out the to the file with exact content in the XmlDocument.
        public override void WriteContentTo(XmlWriter xw)
        {
            foreach (XmlNode n in this)
            {
                n.WriteTo(xw);
            }
        }

        public event XmlNodeChangedEventHandler NodeInserting
        {
            add
            {
                onNodeInsertingDelegate += value;
            }
            remove
            {
                onNodeInsertingDelegate -= value;
            }
        }

        public event XmlNodeChangedEventHandler NodeInserted
        {
            add
            {
                onNodeInsertedDelegate += value;
            }
            remove
            {
                onNodeInsertedDelegate -= value;
            }
        }

        public event XmlNodeChangedEventHandler NodeRemoving
        {
            add
            {
                onNodeRemovingDelegate += value;
            }
            remove
            {
                onNodeRemovingDelegate -= value;
            }
        }

        public event XmlNodeChangedEventHandler NodeRemoved
        {
            add
            {
                onNodeRemovedDelegate += value;
            }
            remove
            {
                onNodeRemovedDelegate -= value;
            }
        }

        public event XmlNodeChangedEventHandler NodeChanging
        {
            add
            {
                onNodeChangingDelegate += value;
            }
            remove
            {
                onNodeChangingDelegate -= value;
            }
        }

        public event XmlNodeChangedEventHandler NodeChanged
        {
            add
            {
                onNodeChangedDelegate += value;
            }
            remove
            {
                onNodeChangedDelegate -= value;
            }
        }

        internal override XmlNodeChangedEventArgs GetEventArgs(XmlNode node, XmlNode oldParent, XmlNode newParent, string oldValue, string newValue, XmlNodeChangedAction action)
        {
            switch (action)
            {
                case XmlNodeChangedAction.Insert:
                    if (onNodeInsertingDelegate == null && onNodeInsertedDelegate == null)
                    {
                        return null;
                    }
                    break;
                case XmlNodeChangedAction.Remove:
                    if (onNodeRemovingDelegate == null && onNodeRemovedDelegate == null)
                    {
                        return null;
                    }
                    break;
                case XmlNodeChangedAction.Change:
                    if (onNodeChangingDelegate == null && onNodeChangedDelegate == null)
                    {
                        return null;
                    }
                    break;
            }
            return new XmlNodeChangedEventArgs(node, oldParent, newParent, oldValue, newValue, action);
        }

        internal XmlNodeChangedEventArgs GetInsertEventArgsForLoad(XmlNode node, XmlNode newParent)
        {
            if (onNodeInsertingDelegate == null && onNodeInsertedDelegate == null)
            {
                return null;
            }
            string nodeValue = node.Value;
            return new XmlNodeChangedEventArgs(node, null, newParent, nodeValue, nodeValue, XmlNodeChangedAction.Insert);
        }

        internal override void BeforeEvent(XmlNodeChangedEventArgs args)
        {
            if (args != null)
            {
                switch (args.Action)
                {
                    case XmlNodeChangedAction.Insert:
                        if (onNodeInsertingDelegate != null)
                            onNodeInsertingDelegate(this, args);
                        break;

                    case XmlNodeChangedAction.Remove:
                        if (onNodeRemovingDelegate != null)
                            onNodeRemovingDelegate(this, args);
                        break;

                    case XmlNodeChangedAction.Change:
                        if (onNodeChangingDelegate != null)
                            onNodeChangingDelegate(this, args);
                        break;
                }
            }
        }

        internal override void AfterEvent(XmlNodeChangedEventArgs args)
        {
            if (args != null)
            {
                switch (args.Action)
                {
                    case XmlNodeChangedAction.Insert:
                        if (onNodeInsertedDelegate != null)
                            onNodeInsertedDelegate(this, args);
                        break;

                    case XmlNodeChangedAction.Remove:
                        if (onNodeRemovedDelegate != null)
                            onNodeRemovedDelegate(this, args);
                        break;

                    case XmlNodeChangedAction.Change:
                        if (onNodeChangedDelegate != null)
                            onNodeChangedDelegate(this, args);
                        break;
                }
            }
        }

        // The function such through schema info to find out if there exists a default attribute with passed in names in the passed in element
        // If so, return the newly created default attribute (with children tree);
        // Otherwise, return null.
        // This function returns null because it is implication of removing schema.
        // If removed more methods would have to be removed as well and it would make adding schema back much harder. 
        internal XmlAttribute GetDefaultAttribute(XmlElement elem, string attrPrefix, string attrLocalname, string attrNamespaceURI)
        {
            return null;
        }

        internal String Standalone
        {
            get
            {
                XmlDeclaration decl = Declaration;
                if (decl != null)
                    return decl.Standalone;
                return null;
            }
        }

        internal XmlEntity GetEntityNode(String name)
        {
            if (DocumentType != null)
            {
                XmlNamedNodeMap entites = DocumentType.Entities;
                if (entites != null)
                    return (XmlEntity)(entites.GetNamedItem(name));
            }
            return null;
        }

        public override String BaseURI
        {
            get { return baseURI; }
        }

        internal void SetBaseURI(String inBaseURI)
        {
            baseURI = inBaseURI;
        }

        internal override XmlNode AppendChildForLoad(XmlNode newChild, XmlDocument doc)
        {
            Debug.Assert(doc == this);

            if (!IsValidChildType(newChild.NodeType))
                throw new InvalidOperationException(SR.Xdom_Node_Insert_TypeConflict);

            if (!CanInsertAfter(newChild, LastChild))
                throw new InvalidOperationException(SR.Xdom_Node_Insert_Location);

            XmlNodeChangedEventArgs args = GetInsertEventArgsForLoad(newChild, this);

            if (args != null)
                BeforeEvent(args);

            XmlLinkedNode newNode = (XmlLinkedNode)newChild;

            if (lastChild == null)
            {
                newNode.next = newNode;
            }
            else
            {
                newNode.next = lastChild.next;
                lastChild.next = newNode;
            }

            lastChild = newNode;
            newNode.SetParentForLoad(this);

            if (args != null)
                AfterEvent(args);

            return newNode;
        }
    }
}
