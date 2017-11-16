// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    using System;
    using System.Text;
    using System.IO;
    using System.Diagnostics;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml.Schema;
    using System.Globalization;

    internal class XmlNodeReaderNavigator
    {
        private XmlNode _curNode;
        private XmlNode _elemNode;
        private XmlNode _logNode;
        private int _attrIndex;
        private int _logAttrIndex;

        //presave these 2 variables since they shouldn't change.
        private XmlNameTable _nameTable;
        private XmlDocument _doc;

        private int _nAttrInd; //used to identify virtual attributes of DocumentType node and XmlDeclaration node

        private const String strPublicID = "PUBLIC";
        private const String strSystemID = "SYSTEM";
        private const String strVersion = "version";
        private const String strStandalone = "standalone";
        private const String strEncoding = "encoding";


        //caching variables for perf reasons
        private int _nDeclarationAttrCount;
        private int _nDocTypeAttrCount;

        //variables for roll back the moves
        private int _nLogLevel;
        private int _nLogAttrInd;
        private bool _bLogOnAttrVal;
        private bool _bCreatedOnAttribute;

        internal struct VirtualAttribute
        {
            internal String name;
            internal String value;

            internal VirtualAttribute(String name, String value)
            {
                this.name = name;
                this.value = value;
            }
        };

        internal VirtualAttribute[] decNodeAttributes = {
                new VirtualAttribute( null, null ),
                new VirtualAttribute( null, null ),
                new VirtualAttribute( null, null )
            };

        internal VirtualAttribute[] docTypeNodeAttributes = {
                new VirtualAttribute( null, null ),
                new VirtualAttribute( null, null )
            };

        private bool _bOnAttrVal;

        public XmlNodeReaderNavigator(XmlNode node)
        {
            _curNode = node;
            _logNode = node;
            XmlNodeType nt = _curNode.NodeType;
            if (nt == XmlNodeType.Attribute)
            {
                _elemNode = null;
                _attrIndex = -1;
                _bCreatedOnAttribute = true;
            }
            else
            {
                _elemNode = node;
                _attrIndex = -1;
                _bCreatedOnAttribute = false;
            }
            //presave this for pref reason since it shouldn't change.
            if (nt == XmlNodeType.Document)
                _doc = (XmlDocument)_curNode;
            else
                _doc = node.OwnerDocument;
            _nameTable = _doc.NameTable;
            _nAttrInd = -1;
            //initialize the caching variables
            _nDeclarationAttrCount = -1;
            _nDocTypeAttrCount = -1;
            _bOnAttrVal = false;
            _bLogOnAttrVal = false;
        }

        public XmlNodeType NodeType
        {
            get
            {
                XmlNodeType nt = _curNode.NodeType;
                if (_nAttrInd != -1)
                {
                    Debug.Assert(nt == XmlNodeType.XmlDeclaration || nt == XmlNodeType.DocumentType);
                    if (_bOnAttrVal)
                        return XmlNodeType.Text;
                    else
                        return XmlNodeType.Attribute;
                }
                return nt;
            }
        }

        public String NamespaceURI
        {
            get { return _curNode.NamespaceURI; }
        }

        public String Name
        {
            get
            {
                if (_nAttrInd != -1)
                {
                    Debug.Assert(_curNode.NodeType == XmlNodeType.XmlDeclaration || _curNode.NodeType == XmlNodeType.DocumentType);
                    if (_bOnAttrVal)
                        return String.Empty; //Text node's name is String.Empty
                    else
                    {
                        Debug.Assert(_nAttrInd >= 0 && _nAttrInd < AttributeCount);
                        if (_curNode.NodeType == XmlNodeType.XmlDeclaration)
                            return decNodeAttributes[_nAttrInd].name;
                        else
                            return docTypeNodeAttributes[_nAttrInd].name;
                    }
                }
                if (IsLocalNameEmpty(_curNode.NodeType))
                    return String.Empty;
                return _curNode.Name;
            }
        }

        public String LocalName
        {
            get
            {
                if (_nAttrInd != -1)
                    //for the nodes in this case, their LocalName should be the same as their name
                    return Name;
                if (IsLocalNameEmpty(_curNode.NodeType))
                    return String.Empty;
                return _curNode.LocalName;
            }
        }

        internal bool CreatedOnAttribute
        {
            get
            {
                return _bCreatedOnAttribute;
            }
        }

        private bool IsLocalNameEmpty(XmlNodeType nt)
        {
            switch (nt)
            {
                case XmlNodeType.None:
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.Comment:
                case XmlNodeType.Document:
                case XmlNodeType.DocumentFragment:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.EndElement:
                case XmlNodeType.EndEntity:
                    return true;
                case XmlNodeType.Element:
                case XmlNodeType.Attribute:
                case XmlNodeType.EntityReference:
                case XmlNodeType.Entity:
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.DocumentType:
                case XmlNodeType.Notation:
                case XmlNodeType.XmlDeclaration:
                    return false;
                default:
                    return true;
            }
        }

        public String Prefix
        {
            get { return _curNode.Prefix; }
        }

        public bool HasValue
        {
            //In DOM, DocumentType node and XmlDeclaration node doesn't value
            //In XPathNavigator, XmlDeclaration node's value is its InnerText; DocumentType doesn't have value
            //In XmlReader, DocumentType node's value is its InternalSubset which is never null ( at least String.Empty )
            get
            {
                if (_nAttrInd != -1)
                {
                    //Pointing at the one of virtual attributes of Declaration or DocumentType nodes
                    Debug.Assert(_curNode.NodeType == XmlNodeType.XmlDeclaration || _curNode.NodeType == XmlNodeType.DocumentType);
                    Debug.Assert(_nAttrInd >= 0 && _nAttrInd < AttributeCount);
                    return true;
                }
                if (_curNode.Value != null || _curNode.NodeType == XmlNodeType.DocumentType)
                    return true;
                return false;
            }
        }

        public String Value
        {
            //See comments in HasValue
            get
            {
                String retValue = null;
                XmlNodeType nt = _curNode.NodeType;
                if (_nAttrInd != -1)
                {
                    //Pointing at the one of virtual attributes of Declaration or DocumentType nodes
                    Debug.Assert(nt == XmlNodeType.XmlDeclaration || nt == XmlNodeType.DocumentType);
                    Debug.Assert(_nAttrInd >= 0 && _nAttrInd < AttributeCount);
                    if (_curNode.NodeType == XmlNodeType.XmlDeclaration)
                        return decNodeAttributes[_nAttrInd].value;
                    else
                        return docTypeNodeAttributes[_nAttrInd].value;
                }
                if (nt == XmlNodeType.DocumentType)
                    retValue = ((XmlDocumentType)_curNode).InternalSubset; //in this case nav.Value will be null
                else if (nt == XmlNodeType.XmlDeclaration)
                {
                    StringBuilder strb = new StringBuilder(String.Empty);
                    if (_nDeclarationAttrCount == -1)
                        InitDecAttr();
                    for (int i = 0; i < _nDeclarationAttrCount; i++)
                    {
                        strb.Append(decNodeAttributes[i].name + "=\"" + decNodeAttributes[i].value + "\"");
                        if (i != (_nDeclarationAttrCount - 1))
                            strb.Append(" ");
                    }
                    retValue = strb.ToString();
                }
                else
                    retValue = _curNode.Value;
                return (retValue == null) ? String.Empty : retValue;
            }
        }

        public String BaseURI
        {
            get { return _curNode.BaseURI; }
        }

        public XmlSpace XmlSpace
        {
            get { return _curNode.XmlSpace; }
        }

        public String XmlLang
        {
            get { return _curNode.XmlLang; }
        }

        public bool IsEmptyElement
        {
            get
            {
                if (_curNode.NodeType == XmlNodeType.Element)
                {
                    return ((XmlElement)_curNode).IsEmpty;
                }
                return false;
            }
        }

        public bool IsDefault
        {
            get
            {
                if (_curNode.NodeType == XmlNodeType.Attribute)
                {
                    return !((XmlAttribute)_curNode).Specified;
                }
                return false;
            }
        }

        public IXmlSchemaInfo SchemaInfo
        {
            get
            {
                return _curNode.SchemaInfo;
            }
        }

        public XmlNameTable NameTable
        {
            get { return _nameTable; }
        }

        public int AttributeCount
        {
            get
            {
                if (_bCreatedOnAttribute)
                    return 0;
                XmlNodeType nt = _curNode.NodeType;
                if (nt == XmlNodeType.Element)
                    return ((XmlElement)_curNode).Attributes.Count;
                else if (nt == XmlNodeType.Attribute
                        || (_bOnAttrVal && nt != XmlNodeType.XmlDeclaration && nt != XmlNodeType.DocumentType))
                    return _elemNode.Attributes.Count;
                else if (nt == XmlNodeType.XmlDeclaration)
                {
                    if (_nDeclarationAttrCount != -1)
                        return _nDeclarationAttrCount;
                    InitDecAttr();
                    return _nDeclarationAttrCount;
                }
                else if (nt == XmlNodeType.DocumentType)
                {
                    if (_nDocTypeAttrCount != -1)
                        return _nDocTypeAttrCount;
                    InitDocTypeAttr();
                    return _nDocTypeAttrCount;
                }
                return 0;
            }
        }

        private void CheckIndexCondition(int attributeIndex)
        {
            if (attributeIndex < 0 || attributeIndex >= AttributeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(attributeIndex));
            }
        }

        //8 functions below are the helper functions to deal with virtual attributes of XmlDeclaration nodes and DocumentType nodes.
        private void InitDecAttr()
        {
            int i = 0;
            String strTemp = _doc.Version;
            if (strTemp != null && strTemp.Length != 0)
            {
                decNodeAttributes[i].name = strVersion;
                decNodeAttributes[i].value = strTemp;
                i++;
            }
            strTemp = _doc.Encoding;
            if (strTemp != null && strTemp.Length != 0)
            {
                decNodeAttributes[i].name = strEncoding;
                decNodeAttributes[i].value = strTemp;
                i++;
            }
            strTemp = _doc.Standalone;
            if (strTemp != null && strTemp.Length != 0)
            {
                decNodeAttributes[i].name = strStandalone;
                decNodeAttributes[i].value = strTemp;
                i++;
            }
            _nDeclarationAttrCount = i;
        }

        public String GetDeclarationAttr(XmlDeclaration decl, String name)
        {
            //PreCondition: curNode is pointing at Declaration node or one of its virtual attributes
            if (name == strVersion)
                return decl.Version;
            if (name == strEncoding)
                return decl.Encoding;
            if (name == strStandalone)
                return decl.Standalone;
            return null;
        }

        public String GetDeclarationAttr(int i)
        {
            if (_nDeclarationAttrCount == -1)
                InitDecAttr();
            return decNodeAttributes[i].value;
        }

        public int GetDecAttrInd(String name)
        {
            if (_nDeclarationAttrCount == -1)
                InitDecAttr();
            for (int i = 0; i < _nDeclarationAttrCount; i++)
            {
                if (decNodeAttributes[i].name == name)
                    return i;
            }
            return -1;
        }

        private void InitDocTypeAttr()
        {
            int i = 0;
            XmlDocumentType docType = _doc.DocumentType;
            if (docType == null)
            {
                _nDocTypeAttrCount = 0;
                return;
            }
            String strTemp = docType.PublicId;
            if (strTemp != null)
            {
                docTypeNodeAttributes[i].name = strPublicID;
                docTypeNodeAttributes[i].value = strTemp;
                i++;
            }
            strTemp = docType.SystemId;
            if (strTemp != null)
            {
                docTypeNodeAttributes[i].name = strSystemID;
                docTypeNodeAttributes[i].value = strTemp;
                i++;
            }
            _nDocTypeAttrCount = i;
        }

        public String GetDocumentTypeAttr(XmlDocumentType docType, String name)
        {
            //PreCondition: nav is pointing at DocumentType node or one of its virtual attributes
            if (name == strPublicID)
                return docType.PublicId;
            if (name == strSystemID)
                return docType.SystemId;
            return null;
        }

        public String GetDocumentTypeAttr(int i)
        {
            if (_nDocTypeAttrCount == -1)
                InitDocTypeAttr();
            return docTypeNodeAttributes[i].value;
        }

        public int GetDocTypeAttrInd(String name)
        {
            if (_nDocTypeAttrCount == -1)
                InitDocTypeAttr();
            for (int i = 0; i < _nDocTypeAttrCount; i++)
            {
                if (docTypeNodeAttributes[i].name == name)
                    return i;
            }
            return -1;
        }

        private String GetAttributeFromElement(XmlElement elem, String name)
        {
            XmlAttribute attr = elem.GetAttributeNode(name);
            if (attr != null)
                return attr.Value;
            return null;
        }

        public String GetAttribute(String name)
        {
            if (_bCreatedOnAttribute)
                return null;
            switch (_curNode.NodeType)
            {
                case XmlNodeType.Element:
                    return GetAttributeFromElement((XmlElement)_curNode, name);
                case XmlNodeType.Attribute:
                    return GetAttributeFromElement((XmlElement)_elemNode, name);
                case XmlNodeType.XmlDeclaration:
                    return GetDeclarationAttr((XmlDeclaration)_curNode, name);
                case XmlNodeType.DocumentType:
                    return GetDocumentTypeAttr((XmlDocumentType)_curNode, name);
            }
            return null;
        }

        private String GetAttributeFromElement(XmlElement elem, String name, String ns)
        {
            XmlAttribute attr = elem.GetAttributeNode(name, ns);
            if (attr != null)
                return attr.Value;
            return null;
        }
        public String GetAttribute(String name, String ns)
        {
            if (_bCreatedOnAttribute)
                return null;
            switch (_curNode.NodeType)
            {
                case XmlNodeType.Element:
                    return GetAttributeFromElement((XmlElement)_curNode, name, ns);
                case XmlNodeType.Attribute:
                    return GetAttributeFromElement((XmlElement)_elemNode, name, ns);
                case XmlNodeType.XmlDeclaration:
                    return (ns.Length == 0) ? GetDeclarationAttr((XmlDeclaration)_curNode, name) : null;
                case XmlNodeType.DocumentType:
                    return (ns.Length == 0) ? GetDocumentTypeAttr((XmlDocumentType)_curNode, name) : null;
            }
            return null;
        }

        public String GetAttribute(int attributeIndex)
        {
            if (_bCreatedOnAttribute)
                return null;
            switch (_curNode.NodeType)
            {
                case XmlNodeType.Element:
                    CheckIndexCondition(attributeIndex);
                    return ((XmlElement)_curNode).Attributes[attributeIndex].Value;
                case XmlNodeType.Attribute:
                    CheckIndexCondition(attributeIndex);
                    return ((XmlElement)_elemNode).Attributes[attributeIndex].Value;
                case XmlNodeType.XmlDeclaration:
                    {
                        CheckIndexCondition(attributeIndex);
                        return GetDeclarationAttr(attributeIndex);
                    }
                case XmlNodeType.DocumentType:
                    {
                        CheckIndexCondition(attributeIndex);
                        return GetDocumentTypeAttr(attributeIndex);
                    }
            }
            throw new ArgumentOutOfRangeException(nameof(attributeIndex)); //for other senario, AttributeCount is 0, i has to be out of range
        }

        public void LogMove(int level)
        {
            _logNode = _curNode;
            _nLogLevel = level;
            _nLogAttrInd = _nAttrInd;
            _logAttrIndex = _attrIndex;
            _bLogOnAttrVal = _bOnAttrVal;
        }

        //The function has to be used in pair with ResetMove when the operation fails after LogMove() is
        //    called because it relies on the values of nOrigLevel, logNav and nOrigAttrInd to be accurate.
        public void RollBackMove(ref int level)
        {
            _curNode = _logNode;
            level = _nLogLevel;
            _nAttrInd = _nLogAttrInd;
            _attrIndex = _logAttrIndex;
            _bOnAttrVal = _bLogOnAttrVal;
        }

        private bool IsOnDeclOrDocType
        {
            get
            {
                XmlNodeType nt = _curNode.NodeType;
                return (nt == XmlNodeType.XmlDeclaration || nt == XmlNodeType.DocumentType);
            }
        }

        public void ResetToAttribute(ref int level)
        {
            //the current cursor is pointing at one of the attribute children -- this could be caused by
            //  the calls to ReadAttributeValue(..)
            if (_bCreatedOnAttribute)
                return;
            if (_bOnAttrVal)
            {
                if (IsOnDeclOrDocType)
                {
                    level -= 2;
                }
                else
                {
                    while (_curNode.NodeType != XmlNodeType.Attribute && ((_curNode = _curNode.ParentNode) != null))
                        level--;
                }
                _bOnAttrVal = false;
            }
        }

        public void ResetMove(ref int level, ref XmlNodeType nt)
        {
            LogMove(level);
            if (_bCreatedOnAttribute)
                return;
            if (_nAttrInd != -1)
            {
                Debug.Assert(IsOnDeclOrDocType);
                if (_bOnAttrVal)
                {
                    level--;
                    _bOnAttrVal = false;
                }
                _nLogAttrInd = _nAttrInd;
                level--;
                _nAttrInd = -1;
                nt = _curNode.NodeType;
                return;
            }
            if (_bOnAttrVal && _curNode.NodeType != XmlNodeType.Attribute)
                ResetToAttribute(ref level);
            if (_curNode.NodeType == XmlNodeType.Attribute)
            {
                _curNode = ((XmlAttribute)_curNode).OwnerElement;
                _attrIndex = -1;
                level--;
                nt = XmlNodeType.Element;
            }
            if (_curNode.NodeType == XmlNodeType.Element)
                _elemNode = _curNode;
        }

        public bool MoveToAttribute(string name)
        {
            return MoveToAttribute(name, string.Empty);
        }
        private bool MoveToAttributeFromElement(XmlElement elem, String name, String ns)
        {
            XmlAttribute attr = null;
            if (ns.Length == 0)
                attr = elem.GetAttributeNode(name);
            else
                attr = elem.GetAttributeNode(name, ns);
            if (attr != null)
            {
                _bOnAttrVal = false;
                _elemNode = elem;
                _curNode = attr;
                _attrIndex = elem.Attributes.FindNodeOffsetNS(attr);
                if (_attrIndex != -1)
                {
                    return true;
                }
            }
            return false;
        }

        public bool MoveToAttribute(string name, string namespaceURI)
        {
            if (_bCreatedOnAttribute)
                return false;
            XmlNodeType nt = _curNode.NodeType;
            if (nt == XmlNodeType.Element)
                return MoveToAttributeFromElement((XmlElement)_curNode, name, namespaceURI);
            else if (nt == XmlNodeType.Attribute)
                return MoveToAttributeFromElement((XmlElement)_elemNode, name, namespaceURI);
            else if (nt == XmlNodeType.XmlDeclaration && namespaceURI.Length == 0)
            {
                if ((_nAttrInd = GetDecAttrInd(name)) != -1)
                {
                    _bOnAttrVal = false;
                    return true;
                }
            }
            else if (nt == XmlNodeType.DocumentType && namespaceURI.Length == 0)
            {
                if ((_nAttrInd = GetDocTypeAttrInd(name)) != -1)
                {
                    _bOnAttrVal = false;
                    return true;
                }
            }
            return false;
        }

        public void MoveToAttribute(int attributeIndex)
        {
            if (_bCreatedOnAttribute)
                return;
            XmlAttribute attr = null;
            switch (_curNode.NodeType)
            {
                case XmlNodeType.Element:
                    CheckIndexCondition(attributeIndex);
                    attr = ((XmlElement)_curNode).Attributes[attributeIndex];
                    if (attr != null)
                    {
                        _elemNode = _curNode;
                        _curNode = (XmlNode)attr;
                        _attrIndex = attributeIndex;
                    }
                    break;
                case XmlNodeType.Attribute:
                    CheckIndexCondition(attributeIndex);
                    attr = ((XmlElement)_elemNode).Attributes[attributeIndex];
                    if (attr != null)
                    {
                        _curNode = (XmlNode)attr;
                        _attrIndex = attributeIndex;
                    }
                    break;
                case XmlNodeType.XmlDeclaration:
                case XmlNodeType.DocumentType:
                    CheckIndexCondition(attributeIndex);
                    _nAttrInd = attributeIndex;
                    break;
            }
        }

        public bool MoveToNextAttribute(ref int level)
        {
            if (_bCreatedOnAttribute)
                return false;
            XmlNodeType nt = _curNode.NodeType;
            if (nt == XmlNodeType.Attribute)
            {
                if (_attrIndex >= (_elemNode.Attributes.Count - 1))
                    return false;
                else
                {
                    _curNode = _elemNode.Attributes[++_attrIndex];
                    return true;
                }
            }
            else if (nt == XmlNodeType.Element)
            {
                if (_curNode.Attributes.Count > 0)
                {
                    level++;
                    _elemNode = _curNode;
                    _curNode = _curNode.Attributes[0];
                    _attrIndex = 0;
                    return true;
                }
            }
            else if (nt == XmlNodeType.XmlDeclaration)
            {
                if (_nDeclarationAttrCount == -1)
                    InitDecAttr();
                _nAttrInd++;
                if (_nAttrInd < _nDeclarationAttrCount)
                {
                    if (_nAttrInd == 0) level++;
                    _bOnAttrVal = false;
                    return true;
                }
                _nAttrInd--;
            }
            else if (nt == XmlNodeType.DocumentType)
            {
                if (_nDocTypeAttrCount == -1)
                    InitDocTypeAttr();
                _nAttrInd++;
                if (_nAttrInd < _nDocTypeAttrCount)
                {
                    if (_nAttrInd == 0) level++;
                    _bOnAttrVal = false;
                    return true;
                }
                _nAttrInd--;
            }
            return false;
        }

        public bool MoveToParent()
        {
            XmlNode parent = _curNode.ParentNode;
            if (parent != null)
            {
                _curNode = parent;
                if (!_bOnAttrVal)
                    _attrIndex = 0;
                return true;
            }
            return false;
        }

        public bool MoveToFirstChild()
        {
            XmlNode firstChild = _curNode.FirstChild;
            if (firstChild != null)
            {
                _curNode = firstChild;
                if (!_bOnAttrVal)
                    _attrIndex = -1;
                return true;
            }
            return false;
        }

        private bool MoveToNextSibling(XmlNode node)
        {
            XmlNode nextSibling = node.NextSibling;
            if (nextSibling != null)
            {
                _curNode = nextSibling;
                if (!_bOnAttrVal)
                    _attrIndex = -1;
                return true;
            }
            return false;
        }

        public bool MoveToNext()
        {
            if (_curNode.NodeType != XmlNodeType.Attribute)
                return MoveToNextSibling(_curNode);
            else
                return MoveToNextSibling(_elemNode);
        }

        public bool MoveToElement()
        {
            if (_bCreatedOnAttribute)
                return false;
            switch (_curNode.NodeType)
            {
                case XmlNodeType.Attribute:
                    if (_elemNode != null)
                    {
                        _curNode = _elemNode;
                        _attrIndex = -1;
                        return true;
                    }
                    break;
                case XmlNodeType.XmlDeclaration:
                case XmlNodeType.DocumentType:
                    {
                        if (_nAttrInd != -1)
                        {
                            _nAttrInd = -1;
                            return true;
                        }
                        break;
                    }
            }
            return false;
        }

        public String LookupNamespace(string prefix)
        {
            if (_bCreatedOnAttribute)
                return null;
            if (prefix == "xmlns")
            {
                return _nameTable.Add(XmlReservedNs.NsXmlNs);
            }
            if (prefix == "xml")
            {
                return _nameTable.Add(XmlReservedNs.NsXml);
            }

            // construct the name of the xmlns attribute
            string attrName;
            if (prefix == null)
                prefix = string.Empty;
            if (prefix.Length == 0)
                attrName = "xmlns";
            else
                attrName = "xmlns:" + prefix;

            // walk up the XmlNode parent chain, looking for the xmlns attribute
            XmlNode node = _curNode;
            while (node != null)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XmlElement elem = (XmlElement)node;
                    if (elem.HasAttributes)
                    {
                        XmlAttribute attr = elem.GetAttributeNode(attrName);
                        if (attr != null)
                        {
                            return attr.Value;
                        }
                    }
                }
                else if (node.NodeType == XmlNodeType.Attribute)
                {
                    node = ((XmlAttribute)node).OwnerElement;
                    continue;
                }
                node = node.ParentNode;
            }
            if (prefix.Length == 0)
            {
                return string.Empty;
            }
            return null;
        }

        internal string DefaultLookupNamespace(string prefix)
        {
            if (!_bCreatedOnAttribute)
            {
                if (prefix == "xmlns")
                {
                    return _nameTable.Add(XmlReservedNs.NsXmlNs);
                }
                if (prefix == "xml")
                {
                    return _nameTable.Add(XmlReservedNs.NsXml);
                }
                if (prefix == string.Empty)
                {
                    return _nameTable.Add(string.Empty);
                }
            }
            return null;
        }

        internal String LookupPrefix(string namespaceName)
        {
            if (_bCreatedOnAttribute || namespaceName == null)
            {
                return null;
            }
            if (namespaceName == XmlReservedNs.NsXmlNs)
            {
                return _nameTable.Add("xmlns");
            }
            if (namespaceName == XmlReservedNs.NsXml)
            {
                return _nameTable.Add("xml");
            }
            if (namespaceName == string.Empty)
            {
                return string.Empty;
            }
            // walk up the XmlNode parent chain, looking for the xmlns attribute with namespaceName value
            XmlNode node = _curNode;
            while (node != null)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XmlElement elem = (XmlElement)node;
                    if (elem.HasAttributes)
                    {
                        XmlAttributeCollection attrs = elem.Attributes;
                        for (int i = 0; i < attrs.Count; i++)
                        {
                            XmlAttribute a = attrs[i];
                            if (a.Value == namespaceName)
                            {
                                if (a.Prefix.Length == 0 && a.LocalName == "xmlns")
                                {
                                    if (LookupNamespace(string.Empty) == namespaceName)
                                    {
                                        return string.Empty;
                                    }
                                }
                                else if (a.Prefix == "xmlns")
                                {
                                    string pref = a.LocalName;
                                    if (LookupNamespace(pref) == namespaceName)
                                    {
                                        return _nameTable.Add(pref);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (node.NodeType == XmlNodeType.Attribute)
                {
                    node = ((XmlAttribute)node).OwnerElement;
                    continue;
                }
                node = node.ParentNode;
            }
            return null;
        }

        internal IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (_bCreatedOnAttribute)
                return dict;

            // walk up the XmlNode parent chain and add all namespace declarations to the dictionary
            XmlNode node = _curNode;
            while (node != null)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XmlElement elem = (XmlElement)node;
                    if (elem.HasAttributes)
                    {
                        XmlAttributeCollection attrs = elem.Attributes;
                        for (int i = 0; i < attrs.Count; i++)
                        {
                            XmlAttribute a = attrs[i];
                            if (a.LocalName == "xmlns" && a.Prefix.Length == 0)
                            {
                                if (!dict.ContainsKey(string.Empty))
                                {
                                    dict.Add(_nameTable.Add(string.Empty), _nameTable.Add(a.Value));
                                }
                            }
                            else if (a.Prefix == "xmlns")
                            {
                                string localName = a.LocalName;
                                if (!dict.ContainsKey(localName))
                                {
                                    dict.Add(_nameTable.Add(localName), _nameTable.Add(a.Value));
                                }
                            }
                        }
                    }
                    if (scope == XmlNamespaceScope.Local)
                    {
                        break;
                    }
                }
                else if (node.NodeType == XmlNodeType.Attribute)
                {
                    node = ((XmlAttribute)node).OwnerElement;
                    continue;
                }
                node = node.ParentNode;
            };

            if (scope != XmlNamespaceScope.Local)
            {
                if (dict.ContainsKey(string.Empty) && dict[string.Empty] == string.Empty)
                {
                    dict.Remove(string.Empty);
                }
                if (scope == XmlNamespaceScope.All)
                {
                    dict.Add(_nameTable.Add("xml"), _nameTable.Add(XmlReservedNs.NsXml));
                }
            }
            return dict;
        }

        public bool ReadAttributeValue(ref int level, ref bool bResolveEntity, ref XmlNodeType nt)
        {
            if (_nAttrInd != -1)
            {
                Debug.Assert(_curNode.NodeType == XmlNodeType.XmlDeclaration || _curNode.NodeType == XmlNodeType.DocumentType);
                if (!_bOnAttrVal)
                {
                    _bOnAttrVal = true;
                    level++;
                    nt = XmlNodeType.Text;
                    return true;
                }
                return false;
            }
            if (_curNode.NodeType == XmlNodeType.Attribute)
            {
                XmlNode firstChild = _curNode.FirstChild;
                if (firstChild != null)
                {
                    _curNode = firstChild;
                    nt = _curNode.NodeType;
                    level++;
                    _bOnAttrVal = true;
                    return true;
                }
            }
            else if (_bOnAttrVal)
            {
                XmlNode nextSibling = null;
                if (_curNode.NodeType == XmlNodeType.EntityReference && bResolveEntity)
                {
                    //going down to ent ref node
                    _curNode = _curNode.FirstChild;
                    nt = _curNode.NodeType;
                    Debug.Assert(_curNode != null);
                    level++;
                    bResolveEntity = false;
                    return true;
                }
                else
                    nextSibling = _curNode.NextSibling;
                if (nextSibling == null)
                {
                    XmlNode parentNode = _curNode.ParentNode;
                    //Check if its parent is entity ref node is sufficient, because in this senario, ent ref node can't have more than 1 level of children that are not other ent ref nodes
                    if (parentNode != null && parentNode.NodeType == XmlNodeType.EntityReference)
                    {
                        //come back from ent ref node
                        _curNode = parentNode;
                        nt = XmlNodeType.EndEntity;
                        level--;
                        return true;
                    }
                }
                if (nextSibling != null)
                {
                    _curNode = nextSibling;
                    nt = _curNode.NodeType;
                    return true;
                }
                else
                    return false;
            }
            return false;
        }

        public XmlDocument Document
        {
            get
            {
                return _doc;
            }
        }
    }

    // Represents a reader that provides fast, non-cached forward only stream access
    // to XML data in an XmlDocument or a specific XmlNode within an XmlDocument.
    public class XmlNodeReader : XmlReader, IXmlNamespaceResolver
    {
        private XmlNodeReaderNavigator _readerNav;

        private XmlNodeType _nodeType;   // nodeType of the node that the reader is currently positioned on
        private int _curDepth;   // depth of attrNav ( also functions as reader's depth )
        private ReadState _readState;  // current reader's state
        private bool _fEOF;       // flag to show if reaches the end of file
        //mark to the state that EntityReference node is supposed to be resolved
        private bool _bResolveEntity;
        private bool _bStartFromDocument;

        private bool _bInReadBinary;
        private ReadContentAsBinaryHelper _readBinaryHelper;


        // Creates an instance of the XmlNodeReader class using the specified XmlNode.
        public XmlNodeReader(XmlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            _readerNav = new XmlNodeReaderNavigator(node);
            _curDepth = 0;

            _readState = ReadState.Initial;
            _fEOF = false;
            _nodeType = XmlNodeType.None;
            _bResolveEntity = false;
            _bStartFromDocument = false;
        }

        //function returns if the reader currently in valid reading states
        internal bool IsInReadingStates()
        {
            return (_readState == ReadState.Interactive); // || readState == ReadState.EndOfFile
        }

        //
        // Node Properties
        //

        // Gets the type of the current node.
        public override XmlNodeType NodeType
        {
            get { return (IsInReadingStates()) ? _nodeType : XmlNodeType.None; }
        }

        // Gets the name of
        // the current node, including the namespace prefix.
        public override string Name
        {
            get
            {
                if (!IsInReadingStates())
                    return String.Empty;
                return _readerNav.Name;
            }
        }

        // Gets the name of the current node without the namespace prefix.
        public override string LocalName
        {
            get
            {
                if (!IsInReadingStates())
                    return String.Empty;
                return _readerNav.LocalName;
            }
        }

        // Gets the namespace URN (as defined in the W3C Namespace Specification)
        // of the current namespace scope.
        public override string NamespaceURI
        {
            get
            {
                if (!IsInReadingStates())
                    return String.Empty;
                return _readerNav.NamespaceURI;
            }
        }

        // Gets the namespace prefix associated with the current node.
        public override string Prefix
        {
            get
            {
                if (!IsInReadingStates())
                    return String.Empty;
                return _readerNav.Prefix;
            }
        }

        // Gets a value indicating whether
        // XmlNodeReader.Value has a value to return.
        public override bool HasValue
        {
            get
            {
                if (!IsInReadingStates())
                    return false;
                return _readerNav.HasValue;
            }
        }

        // Gets the text value of the current node.
        public override string Value
        {
            get
            {
                if (!IsInReadingStates())
                    return String.Empty;
                return _readerNav.Value;
            }
        }

        // Gets the depth of the
        // current node in the XML element stack.
        public override int Depth
        {
            get { return _curDepth; }
        }

        // Gets the base URI of the current node.
        public override String BaseURI
        {
            get { return _readerNav.BaseURI; }
        }

        public override bool CanResolveEntity
        {
            get { return true; }
        }

        // Gets a value indicating whether the current
        // node is an empty element (for example, <MyElement/>.
        public override bool IsEmptyElement
        {
            get
            {
                if (!IsInReadingStates())
                    return false;
                return _readerNav.IsEmptyElement;
            }
        }

        // Gets a value indicating whether the current node is an
        // attribute that was generated from the default value defined
        // in the DTD or schema.
        public override bool IsDefault
        {
            get
            {
                if (!IsInReadingStates())
                    return false;
                return _readerNav.IsDefault;
            }
        }

        // Gets the current xml:space scope.
        public override XmlSpace XmlSpace
        {
            get
            {
                if (!IsInReadingStates())
                    return XmlSpace.None;
                return _readerNav.XmlSpace;
            }
        }

        // Gets the current xml:lang scope.
        public override string XmlLang
        {
            // Assume everything is in Unicode
            get
            {
                if (!IsInReadingStates())
                    return String.Empty;
                return _readerNav.XmlLang;
            }
        }

        public override IXmlSchemaInfo SchemaInfo
        {
            get
            {
                if (!IsInReadingStates())
                {
                    return null;
                }
                return _readerNav.SchemaInfo;
            }
        }

        //
        // Attribute Accessors
        //

        // Gets the number of attributes on the current node.
        public override int AttributeCount
        {
            get
            {
                if (!IsInReadingStates() || _nodeType == XmlNodeType.EndElement)
                    return 0;
                return _readerNav.AttributeCount;
            }
        }

        // Gets the value of the attribute with the specified name.
        public override string GetAttribute(string name)
        {
            //if not on Attribute, only element node could have attributes
            if (!IsInReadingStates())
                return null;
            return _readerNav.GetAttribute(name);
        }

        // Gets the value of the attribute with the specified name and namespace.
        public override string GetAttribute(string name, string namespaceURI)
        {
            //if not on Attribute, only element node could have attributes
            if (!IsInReadingStates())
                return null;
            String ns = (namespaceURI == null) ? String.Empty : namespaceURI;
            return _readerNav.GetAttribute(name, ns);
        }

        // Gets the value of the attribute with the specified index.
        public override string GetAttribute(int attributeIndex)
        {
            if (!IsInReadingStates())
                throw new ArgumentOutOfRangeException(nameof(attributeIndex));
            //CheckIndexCondition( i );
            //Debug.Assert( nav.NodeType == XmlNodeType.Element );
            return _readerNav.GetAttribute(attributeIndex);
        }

        // Moves to the attribute with the specified name.
        public override bool MoveToAttribute(string name)
        {
            if (!IsInReadingStates())
                return false;
            _readerNav.ResetMove(ref _curDepth, ref _nodeType);
            if (_readerNav.MoveToAttribute(name))
            { //, ref curDepth ) ) {
                _curDepth++;
                _nodeType = _readerNav.NodeType;
                if (_bInReadBinary)
                {
                    FinishReadBinary();
                }
                return true;
            }
            _readerNav.RollBackMove(ref _curDepth);
            return false;
        }

        // Moves to the attribute with the specified name and namespace.
        public override bool MoveToAttribute(string name, string namespaceURI)
        {
            if (!IsInReadingStates())
                return false;
            _readerNav.ResetMove(ref _curDepth, ref _nodeType);
            String ns = (namespaceURI == null) ? String.Empty : namespaceURI;
            if (_readerNav.MoveToAttribute(name, ns))
            { //, ref curDepth ) ) {
                _curDepth++;
                _nodeType = _readerNav.NodeType;
                if (_bInReadBinary)
                {
                    FinishReadBinary();
                }
                return true;
            }
            _readerNav.RollBackMove(ref _curDepth);
            return false;
        }

        // Moves to the attribute with the specified index.
        public override void MoveToAttribute(int attributeIndex)
        {
            if (!IsInReadingStates())
                throw new ArgumentOutOfRangeException(nameof(attributeIndex));
            _readerNav.ResetMove(ref _curDepth, ref _nodeType);
            try
            {
                if (AttributeCount > 0)
                {
                    _readerNav.MoveToAttribute(attributeIndex);
                    if (_bInReadBinary)
                    {
                        FinishReadBinary();
                    }
                }
                else
                    throw new ArgumentOutOfRangeException(nameof(attributeIndex));
            }
            catch
            {
                _readerNav.RollBackMove(ref _curDepth);
                throw;
            }
            _curDepth++;
            _nodeType = _readerNav.NodeType;
        }

        // Moves to the first attribute.
        public override bool MoveToFirstAttribute()
        {
            if (!IsInReadingStates())
                return false;
            _readerNav.ResetMove(ref _curDepth, ref _nodeType);
            if (AttributeCount > 0)
            {
                _readerNav.MoveToAttribute(0);
                _curDepth++;
                _nodeType = _readerNav.NodeType;
                if (_bInReadBinary)
                {
                    FinishReadBinary();
                }
                return true;
            }
            _readerNav.RollBackMove(ref _curDepth);
            return false;
        }

        // Moves to the next attribute.
        public override bool MoveToNextAttribute()
        {
            if (!IsInReadingStates() || _nodeType == XmlNodeType.EndElement)
                return false;
            _readerNav.LogMove(_curDepth);
            _readerNav.ResetToAttribute(ref _curDepth);
            if (_readerNav.MoveToNextAttribute(ref _curDepth))
            {
                _nodeType = _readerNav.NodeType;
                if (_bInReadBinary)
                {
                    FinishReadBinary();
                }
                return true;
            }
            _readerNav.RollBackMove(ref _curDepth);
            return false;
        }

        // Moves to the element that contains the current attribute node.
        public override bool MoveToElement()
        {
            if (!IsInReadingStates())
                return false;
            _readerNav.LogMove(_curDepth);
            _readerNav.ResetToAttribute(ref _curDepth);
            if (_readerNav.MoveToElement())
            {
                _curDepth--;
                _nodeType = _readerNav.NodeType;
                if (_bInReadBinary)
                {
                    FinishReadBinary();
                }
                return true;
            }
            _readerNav.RollBackMove(ref _curDepth);
            return false;
        }

        //
        // Moving through the Stream
        //

        // Reads the next node from the stream.
        public override bool Read()
        {
            return Read(false);
        }
        private bool Read(bool fSkipChildren)
        {
            if (_fEOF)
                return false;

            if (_readState == ReadState.Initial)
            {
                // if nav is pointing at the document node, start with its children
                // otherwise,start with the node.
                if ((_readerNav.NodeType == XmlNodeType.Document) || (_readerNav.NodeType == XmlNodeType.DocumentFragment))
                {
                    _bStartFromDocument = true;
                    if (!ReadNextNode(fSkipChildren))
                    {
                        _readState = ReadState.Error;
                        return false;
                    }
                }
                ReSetReadingMarks();
                _readState = ReadState.Interactive;
                _nodeType = _readerNav.NodeType;
                //_depth = 0;
                _curDepth = 0;
                return true;
            }

            if (_bInReadBinary)
            {
                FinishReadBinary();
            }

            bool bRead = false;
            if ((_readerNav.CreatedOnAttribute))
                return false;
            ReSetReadingMarks();
            bRead = ReadNextNode(fSkipChildren);
            if (bRead)
            {
                return true;
            }
            else
            {
                if (_readState == ReadState.Initial || _readState == ReadState.Interactive)
                    _readState = ReadState.Error;
                if (_readState == ReadState.EndOfFile)
                    _nodeType = XmlNodeType.None;
                return false;
            }
        }

        private bool ReadNextNode(bool fSkipChildren)
        {
            if (_readState != ReadState.Interactive && _readState != ReadState.Initial)
            {
                _nodeType = XmlNodeType.None;
                return false;
            }

            bool bDrillDown = !fSkipChildren;
            XmlNodeType nt = _readerNav.NodeType;
            //only goes down when nav.NodeType is of element or of document at the initial state, other nav.NodeType will not be parsed down
            //if nav.NodeType is of EntityReference, ResolveEntity() could be called to get the content parsed;
            bDrillDown = bDrillDown
                        && (_nodeType != XmlNodeType.EndElement)
                        && (_nodeType != XmlNodeType.EndEntity)
                        && (nt == XmlNodeType.Element || (nt == XmlNodeType.EntityReference && _bResolveEntity) ||
                            (((_readerNav.NodeType == XmlNodeType.Document) || (_readerNav.NodeType == XmlNodeType.DocumentFragment)) && _readState == ReadState.Initial));
            //first see if there are children of current node, so to move down
            if (bDrillDown)
            {
                if (_readerNav.MoveToFirstChild())
                {
                    _nodeType = _readerNav.NodeType;
                    _curDepth++;
                    if (_bResolveEntity)
                        _bResolveEntity = false;
                    return true;
                }
                else if (_readerNav.NodeType == XmlNodeType.Element
                          && !_readerNav.IsEmptyElement)
                {
                    _nodeType = XmlNodeType.EndElement;
                    return true;
                }
                else if (_readerNav.NodeType == XmlNodeType.EntityReference && _bResolveEntity)
                {
                    _bResolveEntity = false;
                    _nodeType = XmlNodeType.EndEntity;
                    return true;
                }
                // if fails to move to it 1st Child, try to move to next below
                return ReadForward(fSkipChildren);
            }
            else
            {
                if (_readerNav.NodeType == XmlNodeType.EntityReference && _bResolveEntity)
                {
                    //The only way to get to here is because Skip() is called directly after ResolveEntity()
                    // in this case, user wants to skip the first Child of EntityRef node and fSkipChildren is true
                    // We want to pointing to the first child node.
                    if (_readerNav.MoveToFirstChild())
                    {
                        _nodeType = _readerNav.NodeType;
                        _curDepth++;
                    }
                    else
                    {
                        _nodeType = XmlNodeType.EndEntity;
                    }
                    _bResolveEntity = false;
                    return true;
                }
            }
            return ReadForward(fSkipChildren);  //has to get the next node by moving forward
        }

        private void SetEndOfFile()
        {
            _fEOF = true;
            _readState = ReadState.EndOfFile;
            _nodeType = XmlNodeType.None;
        }

        private bool ReadAtZeroLevel(bool fSkipChildren)
        {
            Debug.Assert(_curDepth == 0);
            if (!fSkipChildren
                && _nodeType != XmlNodeType.EndElement
                && _readerNav.NodeType == XmlNodeType.Element
                && !_readerNav.IsEmptyElement)
            {
                _nodeType = XmlNodeType.EndElement;
                return true;
            }
            else
            {
                SetEndOfFile();
                return false;
            }
        }

        private bool ReadForward(bool fSkipChildren)
        {
            if (_readState == ReadState.Error)
                return false;

            if (!_bStartFromDocument && _curDepth == 0)
            {
                //already on top most node and we shouldn't move to next
                return ReadAtZeroLevel(fSkipChildren);
            }
            //else either we are not on top level or we are starting from the document at the very beginning in which case
            //  we will need to read all the "top" most nodes
            if (_readerNav.MoveToNext())
            {
                _nodeType = _readerNav.NodeType;
                return true;
            }
            else
            {
                //need to check its parent
                if (_curDepth == 0)
                    return ReadAtZeroLevel(fSkipChildren);
                if (_readerNav.MoveToParent())
                {
                    if (_readerNav.NodeType == XmlNodeType.Element)
                    {
                        _curDepth--;
                        _nodeType = XmlNodeType.EndElement;
                        return true;
                    }
                    else if (_readerNav.NodeType == XmlNodeType.EntityReference)
                    {
                        //coming back from entity reference node -- must be getting down through call ResolveEntity()
                        _curDepth--;
                        _nodeType = XmlNodeType.EndEntity;
                        return true;
                    }
                    return true;
                }
            }
            return false;
        }

        //the function reset the marks used for ReadChars() and MoveToAttribute(...), ReadAttributeValue(...)
        private void ReSetReadingMarks()
        {
            //_attrValInd = -1;
            _readerNav.ResetMove(ref _curDepth, ref _nodeType);
            //attrNav.MoveTo( nav );
            //curDepth = _depth;
        }

        // Gets a value indicating whether the reader is positioned at the
        // end of the stream.
        public override bool EOF
        {
            get { return (_readState != ReadState.Closed) && _fEOF; }
        }

        // Closes the stream, changes the XmlNodeReader.ReadState
        // to Closed, and sets all the properties back to zero.
        public override void Close()
        {
            _readState = ReadState.Closed;
        }

        // Gets the read state of the stream.
        public override ReadState ReadState
        {
            get { return _readState; }
        }

        // Skips to the end tag of the current element.
        public override void Skip()
        {
            Read(true);
        }

        // Reads the contents of an element as a string.
        public override string ReadString()
        {
            if ((this.NodeType == XmlNodeType.EntityReference) && _bResolveEntity)
            {
                if (!this.Read())
                {
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                }
            }
            return base.ReadString();
        }

        //
        // Partial Content Read Methods
        //

        // Gets a value indicating whether the current node
        // has any attributes.
        public override bool HasAttributes
        {
            get
            {
                return (AttributeCount > 0);
            }
        }

        //
        // Nametable and Namespace Helpers
        //

        // Gets the XmlNameTable associated with this implementation.
        public override XmlNameTable NameTable
        {
            get { return _readerNav.NameTable; }
        }

        // Resolves a namespace prefix in the current element's scope.
        public override String LookupNamespace(string prefix)
        {
            if (!IsInReadingStates())
                return null;
            string ns = _readerNav.LookupNamespace(prefix);
            if (ns != null && ns.Length == 0)
            {
                return null;
            }
            return ns;
        }

        // Resolves the entity reference for nodes of NodeType EntityReference.
        public override void ResolveEntity()
        {
            if (!IsInReadingStates() || (_nodeType != XmlNodeType.EntityReference))
                throw new InvalidOperationException(SR.Xnr_ResolveEntity);
            _bResolveEntity = true; ;
        }

        // Parses the attribute value into one or more Text and/or
        // EntityReference node types.
        public override bool ReadAttributeValue()
        {
            if (!IsInReadingStates())
                return false;
            if (_readerNav.ReadAttributeValue(ref _curDepth, ref _bResolveEntity, ref _nodeType))
            {
                _bInReadBinary = false;
                return true;
            }
            return false;
        }

        public override bool CanReadBinaryContent
        {
            get
            {
                return true;
            }
        }

        public override int ReadContentAsBase64(byte[] buffer, int index, int count)
        {
            if (_readState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (!_bInReadBinary)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
            }

            // turn off bInReadBinary in order to have a normal Read() behavior when called from readBinaryHelper
            _bInReadBinary = false;

            // call to the helper
            int readCount = _readBinaryHelper.ReadContentAsBase64(buffer, index, count);

            // turn on bInReadBinary in again and return
            _bInReadBinary = true;
            return readCount;
        }

        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            if (_readState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (!_bInReadBinary)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
            }

            // turn off bInReadBinary in order to have a normal Read() behavior when called from readBinaryHelper
            _bInReadBinary = false;

            // call to the helper
            int readCount = _readBinaryHelper.ReadContentAsBinHex(buffer, index, count);

            // turn on bInReadBinary in again and return
            _bInReadBinary = true;
            return readCount;
        }

        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
        {
            if (_readState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (!_bInReadBinary)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
            }

            // turn off bInReadBinary in order to have a normal Read() behavior when called from readBinaryHelper
            _bInReadBinary = false;

            // call to the helper
            int readCount = _readBinaryHelper.ReadElementContentAsBase64(buffer, index, count);

            // turn on bInReadBinary in again and return
            _bInReadBinary = true;
            return readCount;
        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            if (_readState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (!_bInReadBinary)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
            }

            // turn off bInReadBinary in order to have a normal Read() behavior when called from readBinaryHelper
            _bInReadBinary = false;

            // call to the helper
            int readCount = _readBinaryHelper.ReadElementContentAsBinHex(buffer, index, count);

            // turn on bInReadBinary in again and return
            _bInReadBinary = true;
            return readCount;
        }

        private void FinishReadBinary()
        {
            _bInReadBinary = false;
            _readBinaryHelper.Finish();
        }

        //
        // IXmlNamespaceResolver
        //

        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return _readerNav.GetNamespacesInScope(scope);
        }

        string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return _readerNav.LookupPrefix(namespaceName);
        }

        String IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            if (!IsInReadingStates())
            {
                return _readerNav.DefaultLookupNamespace(prefix);
            }
            string ns = _readerNav.LookupNamespace(prefix);
            if (ns != null)
            {
                ns = _readerNav.NameTable.Add(ns);
            }
            return ns;
        }

        // DTD/Schema info used by XmlReader.GetDtdSchemaInfo()
        internal override IDtdInfo DtdInfo
        {
            get
            {
                return _readerNav.Document.DtdSchemaInfo;
            }
        }
    }
}
