// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Text;
using OLEDB.Test.ModuleCore;

namespace System.Xml.XmlDiff
{
    public enum NodePosition
    {
        Before = 0,
        After = 1,
        Unknown = 2,
        Same = 3
    }

    public enum XmlDiffNodeType
    {
        Element = 0,
        Attribute = 1,
        ER = 2,
        Text = 3,
        CData = 4,
        Comment = 5,
        PI = 6,
        WS = 7,
        Document = 8
    }

    internal class PositionInfo : IXmlLineInfo
    {
        public virtual bool HasLineInfo() { return false; }
        public virtual int LineNumber { get { return 0; } }
        public virtual int LinePosition { get { return 0; } }

        public static PositionInfo GetPositionInfo(Object o)
        {
            IXmlLineInfo lineInfo = o as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                return new ReaderPositionInfo(lineInfo);
            }
            else
            {
                return new PositionInfo();
            }
        }
    }

    internal class ReaderPositionInfo : PositionInfo
    {
        private IXmlLineInfo _mlineInfo;

        public ReaderPositionInfo(IXmlLineInfo lineInfo)
        {
            _mlineInfo = lineInfo;
        }

        public override bool HasLineInfo() { return true; }
        public override int LineNumber { get { return _mlineInfo.LineNumber; } }
        public override int LinePosition
        {
            get { return _mlineInfo.LinePosition; }
        }
    }

    public class XmlDiffDocument : XmlDiffNode
    {
        private bool _bLoaded;
        private bool _bIgnoreAttributeOrder;
        private bool _bIgnoreChildOrder;
        private bool _bIgnoreComments;
        private bool _bIgnoreWhitespace;
        private bool _bIgnoreDTD;
        private bool _bIgnoreNS;
        private bool _bIgnorePrefix;
        private bool _bCDataAsText;
        private bool _bNormalizeNewline;
        public XmlNameTable nameTable;

        public XmlDiffDocument()
            : base()
        {
            _bLoaded = false;
            _bIgnoreAttributeOrder = false;
            _bIgnoreChildOrder = false;
            _bIgnoreComments = false;
            _bIgnoreWhitespace = false;
            _bIgnoreDTD = false;
            _bCDataAsText = false;
        }

        public XmlDiffOption Option
        {
            set
            {
                this.IgnoreAttributeOrder = (((int)value & (int)(XmlDiffOption.IgnoreAttributeOrder)) > 0);
                this.IgnoreChildOrder = (((int)value & (int)(XmlDiffOption.IgnoreChildOrder)) > 0);
                this.IgnoreComments = (((int)value & (int)(XmlDiffOption.IgnoreComments)) > 0);
                this.IgnoreWhitespace = (((int)value & (int)(XmlDiffOption.IgnoreWhitespace)) > 0);
                this.IgnoreDTD = (((int)value & (int)(XmlDiffOption.IgnoreDTD)) > 0);
                this.IgnoreNS = (((int)value & (int)(XmlDiffOption.IgnoreNS)) > 0);
                this.IgnorePrefix = (((int)value & (int)(XmlDiffOption.IgnorePrefix)) > 0);
                this.CDataAsText = (((int)value & (int)(XmlDiffOption.CDataAsText)) > 0);
                this.NormalizeNewline = (((int)value & (int)(XmlDiffOption.NormalizeNewline)) > 0);
            }
        }
        public override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Document; } }

        public bool IgnoreAttributeOrder
        {
            get { return this._bIgnoreAttributeOrder; }
            set { this._bIgnoreAttributeOrder = value; }
        }

        public bool IgnoreChildOrder
        {
            get { return this._bIgnoreChildOrder; }
            set { this._bIgnoreChildOrder = value; }
        }

        public bool IgnoreComments
        {
            get { return this._bIgnoreComments; }
            set { this._bIgnoreComments = value; }
        }

        public bool IgnoreWhitespace
        {
            get { return this._bIgnoreWhitespace; }
            set { this._bIgnoreWhitespace = value; }
        }

        public bool IgnoreDTD
        {
            get { return this._bIgnoreDTD; }
            set { this._bIgnoreDTD = value; }
        }

        public bool IgnoreNS
        {
            get { return this._bIgnoreNS; }
            set { this._bIgnoreNS = value; }
        }

        public bool IgnorePrefix
        {
            get { return this._bIgnorePrefix; }
            set { this._bIgnorePrefix = value; }
        }

        public bool CDataAsText
        {
            get { return this._bCDataAsText; }
            set { this._bCDataAsText = value; }
        }

        public bool NormalizeNewline
        {
            get { return this._bNormalizeNewline; }
            set { this._bNormalizeNewline = value; }
        }

        //NodePosition.Before is returned if node2 should be before node1;
        //NodePosition.After is returned if node2 should be after node1;
        //In any case, NodePosition.Unknown should never be returned.
        internal NodePosition ComparePosition(XmlDiffNode node1, XmlDiffNode node2)
        {
            int nt1 = (int)(node1.NodeType);
            int nt2 = (int)(node2.NodeType);
            if (nt2 > nt1)
                return NodePosition.After;
            if (nt2 < nt1)
                return NodePosition.Before;
            //now nt1 == nt2
            if (nt1 == (int)XmlDiffNodeType.Element)
            {
                return CompareElements(node1 as XmlDiffElement, node2 as XmlDiffElement);
            }
            else if (nt1 == (int)XmlDiffNodeType.Attribute)
            {
                return CompareAttributes(node1 as XmlDiffAttribute, node2 as XmlDiffAttribute);
            }
            else if (nt1 == (int)XmlDiffNodeType.ER)
            {
                return CompareERs(node1 as XmlDiffEntityReference, node2 as XmlDiffEntityReference);
            }
            else if (nt1 == (int)XmlDiffNodeType.PI)
            {
                return ComparePIs(node1 as XmlDiffProcessingInstruction, node2 as XmlDiffProcessingInstruction);
            }
            else if (node1 is XmlDiffCharacterData)
            {
                return CompareTextLikeNodes(node1 as XmlDiffCharacterData, node2 as XmlDiffCharacterData);
            }
            else
            {
                //something really wrong here, what should we do???
                Debug.Assert(false, "ComparePosition meets an indecision situation.");
                return NodePosition.Unknown;
            }
        }

        private NodePosition CompareElements(XmlDiffElement elem1, XmlDiffElement elem2)
        {
            Debug.Assert(elem1 != null);
            Debug.Assert(elem2 != null);
            int nCompare = 0;
            if ((nCompare = CompareText(elem2.LocalName, elem1.LocalName)) == 0)
            {
                if (IgnoreNS || (nCompare = CompareText(elem2.NamespaceURI, elem1.NamespaceURI)) == 0)
                {
                    if (IgnorePrefix || (nCompare = CompareText(elem2.Prefix, elem1.Prefix)) == 0)
                    {
                        if ((nCompare = CompareText(elem2.Value, elem1.Value)) == 0)
                        {
                            if ((nCompare = CompareAttributes(elem2, elem1)) == 0)
                            {
                                return NodePosition.After;
                            }
                        }
                    }
                }
            }
            if (nCompare > 0)
                //elem2 > elem1
                return NodePosition.After;
            else
                //elem2 < elem1
                return NodePosition.Before;
        }

        private int CompareAttributes(XmlDiffElement elem1, XmlDiffElement elem2)
        {
            int count1 = elem1.AttributeCount;
            int count2 = elem2.AttributeCount;
            if (count1 > count2)
                return 1;
            else if (count1 < count2)
                return -1;
            else
            {
                XmlDiffAttribute current1 = elem1.FirstAttribute;
                XmlDiffAttribute current2 = elem2.FirstAttribute;
                //			NodePosition result = 0;
                int nCompare = 0;
                while (current1 != null && current2 != null && nCompare == 0)
                {
                    if ((nCompare = CompareText(current2.LocalName, current1.LocalName)) == 0)
                    {
                        if (IgnoreNS || (nCompare = CompareText(current2.NamespaceURI, current1.NamespaceURI)) == 0)
                        {
                            if (IgnorePrefix || (nCompare = CompareText(current2.Prefix, current1.Prefix)) == 0)
                            {
                                if ((nCompare = CompareText(current2.Value, current1.Value)) == 0)
                                {
                                    //do nothing!
                                }
                            }
                        }
                    }
                    current1 = (XmlDiffAttribute)current1._next;
                    current2 = (XmlDiffAttribute)current2._next;
                }
                if (nCompare > 0)
                    //elem1 > attr2
                    return 1;
                else
                    //elem1 < elem2
                    return -1;
            }
        }

        private NodePosition CompareAttributes(XmlDiffAttribute attr1, XmlDiffAttribute attr2)
        {
            Debug.Assert(attr1 != null);
            Debug.Assert(attr2 != null);

            int nCompare = 0;
            if ((nCompare = CompareText(attr2.LocalName, attr1.LocalName)) == 0)
            {
                if (IgnoreNS || (nCompare = CompareText(attr2.NamespaceURI, attr1.NamespaceURI)) == 0)
                {
                    if (IgnorePrefix || (nCompare = CompareText(attr2.Prefix, attr1.Prefix)) == 0)
                    {
                        if ((nCompare = CompareText(attr2.Value, attr1.Value)) == 0)
                        {
                            return NodePosition.After;
                        }
                    }
                }
            }
            if (nCompare > 0)
                //attr2 > attr1
                return NodePosition.After;
            else
                //attr2 < attr1
                return NodePosition.Before;
        }

        private NodePosition CompareERs(XmlDiffEntityReference er1, XmlDiffEntityReference er2)
        {
            Debug.Assert(er1 != null);
            Debug.Assert(er2 != null);

            int nCompare = CompareText(er2.Name, er1.Name);
            if (nCompare >= 0)
                return NodePosition.After;
            else
                return NodePosition.Before;
        }

        private NodePosition ComparePIs(XmlDiffProcessingInstruction pi1, XmlDiffProcessingInstruction pi2)
        {
            Debug.Assert(pi1 != null);
            Debug.Assert(pi2 != null);

            int nCompare = 0;
            if ((nCompare = CompareText(pi2.Name, pi1.Name)) == 0)
            {
                if ((nCompare = CompareText(pi2.Value, pi1.Value)) == 0)
                {
                    return NodePosition.After;
                }
            }
            if (nCompare > 0)
                //pi2 > pi1
                return NodePosition.After;
            else
                //pi2 < pi1
                return NodePosition.Before;
        }

        private NodePosition CompareTextLikeNodes(XmlDiffCharacterData t1, XmlDiffCharacterData t2)
        {
            Debug.Assert(t1 != null);
            Debug.Assert(t2 != null);

            int nCompare = CompareText(t2.Value, t1.Value);
            if (nCompare >= 0)
                return NodePosition.After;
            else
                return NodePosition.Before;
        }

        //returns 0 if the same string; 1 if s1 > s1 and -1 if s1 < s2
        private int CompareText(string s1, string s2)
        {
            int len = s1.Length;
            //len becomes the smaller of the two
            if (len > s2.Length)
                len = s2.Length;
            int nInd = 0;
            char c1 = (char)0x0;
            char c2 = (char)0x0;
            while (nInd < len)
            {
                c1 = s1[nInd];
                c2 = s2[nInd];
                if (c1 < c2)
                    return -1; //s1 < s2
                else if (c1 > c2)
                    return 1; //s1 > s2
                nInd++;
            }
            if (s2.Length > s1.Length)
                return -1; //s1 < s2
            else if (s2.Length < s1.Length)
                return 1; //s1 > s2
            else return 0;
        }

        public virtual void Load(XmlReader reader)
        {
            if (_bLoaded)
                throw new InvalidOperationException("The document already contains data and should not be used again.");
            if (reader.ReadState == ReadState.Initial)
            {
                if (!reader.Read())
                    return;
            }
            PositionInfo pInfo = PositionInfo.GetPositionInfo(reader);
            ReadChildNodes(this, reader, pInfo);
            this._bLoaded = true;
            this.nameTable = reader.NameTable;
        }

        internal void ReadChildNodes(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo)
        {
            bool lookAhead = false;
            do
            {
                lookAhead = false;
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        LoadElement(parent, reader, pInfo);
                        break;
                    case XmlNodeType.Comment:
                        if (!IgnoreComments)
                            LoadTextNode(parent, reader, pInfo, XmlDiffNodeType.Comment);
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        LoadPI(parent, reader, pInfo);
                        break;
                    case XmlNodeType.SignificantWhitespace:
                        if (reader.XmlSpace == XmlSpace.Preserve)
                        {
                            LoadTextNode(parent, reader, pInfo, XmlDiffNodeType.WS);
                        }
                        break;
                    case XmlNodeType.CDATA:
                        if (!CDataAsText)
                        {
                            LoadTextNode(parent, reader, pInfo, XmlDiffNodeType.CData);
                        }
                        else //merge with adjacent text/CDATA nodes
                        {
                            StringBuilder text = new StringBuilder();
                            text.Append(reader.Value);
                            while ((lookAhead = reader.Read()) && (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA))
                            {
                                text.Append(reader.Value);
                            }
                            LoadTextNode(parent, text.ToString(), pInfo, XmlDiffNodeType.Text);
                        }
                        break;
                    case XmlNodeType.Text:
                        if (!CDataAsText)
                        {
                            LoadTextNode(parent, reader, pInfo, XmlDiffNodeType.Text);
                        }
                        else //megre with adjacent text/CDATA nodes
                        {
                            StringBuilder text = new StringBuilder();
                            text.Append(reader.Value);
                            while ((lookAhead = reader.Read()) && (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA))
                            {
                                text.Append(reader.Value);
                            }
                            LoadTextNode(parent, text.ToString(), pInfo, XmlDiffNodeType.Text);
                        }

                        break;
                    case XmlNodeType.EntityReference:
                        LoadEntityReference(parent, reader, pInfo);
                        break;
                    case XmlNodeType.EndElement:
                        SetElementEndPosition(parent as XmlDiffElement, pInfo);
                        return;
                    case XmlNodeType.Attribute: //attribute at top level
                        string attrVal = reader.Name + "=\"" + reader.Value + "\"";
                        LoadTopLevelAttribute(parent, attrVal, pInfo, XmlDiffNodeType.Text);
                        break;
                    default:
                        break;
                }
            } while (lookAhead || reader.Read());
        }

        private void LoadElement(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo)
        {
            XmlDiffElement elem = null;
            bool bEmptyElement = reader.IsEmptyElement;
            if (bEmptyElement)
                elem = new XmlDiffEmptyElement(reader.LocalName, reader.Prefix, reader.NamespaceURI);
            else
                elem = new XmlDiffElement(reader.LocalName, reader.Prefix, reader.NamespaceURI);
            elem.LineNumber = pInfo.LineNumber;
            elem.LinePosition = pInfo.LinePosition;
            ReadAttributes(elem, reader, pInfo);
            if (!bEmptyElement)
            {
                reader.Read(); //move to child
                ReadChildNodes(elem, reader, pInfo);
            }
            InsertChild(parent, elem);
        }

        private void ReadAttributes(XmlDiffElement parent, XmlReader reader, PositionInfo pInfo)
        {
            if (reader.MoveToFirstAttribute())
            {
                XmlDiffAttribute attr = new XmlDiffAttribute(reader.LocalName, reader.Prefix, reader.NamespaceURI, reader.Value);
                attr.LineNumber = pInfo.LineNumber;
                attr.LinePosition = pInfo.LinePosition;
                InsertAttribute(parent, attr);

                while (reader.MoveToNextAttribute())
                {
                    attr = new XmlDiffAttribute(reader.LocalName, reader.Prefix, reader.NamespaceURI, reader.Value);
                    attr.LineNumber = pInfo.LineNumber;
                    attr.LinePosition = pInfo.LinePosition;
                    InsertAttribute(parent, attr);
                }
            }
        }

        private void LoadTextNode(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo, XmlDiffNodeType nt)
        {
            XmlDiffCharacterData textNode = new XmlDiffCharacterData(reader.Value, nt, this.NormalizeNewline);
            textNode.LineNumber = pInfo.LineNumber;
            textNode.LinePosition = pInfo.LinePosition;
            InsertChild(parent, textNode);
        }

        private void LoadTextNode(XmlDiffNode parent, string text, PositionInfo pInfo, XmlDiffNodeType nt)
        {
            XmlDiffCharacterData textNode = new XmlDiffCharacterData(text, nt, this.NormalizeNewline);
            textNode.LineNumber = pInfo.LineNumber;
            textNode.LinePosition = pInfo.LinePosition;
            InsertChild(parent, textNode);
        }

        private void LoadTopLevelAttribute(XmlDiffNode parent, string text, PositionInfo pInfo, XmlDiffNodeType nt)
        {
            XmlDiffCharacterData textNode = new XmlDiffCharacterData(text, nt, this.NormalizeNewline);
            textNode.LineNumber = pInfo.LineNumber;
            textNode.LinePosition = pInfo.LinePosition;
            InsertTopLevelAttributeAsText(parent, textNode);
        }

        private void LoadPI(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo)
        {
            XmlDiffProcessingInstruction pi = new XmlDiffProcessingInstruction(reader.Name, reader.Value);
            pi.LineNumber = pInfo.LineNumber;
            pi.LinePosition = pInfo.LinePosition;
            InsertChild(parent, pi);
        }

        private void LoadEntityReference(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo)
        {
            XmlDiffEntityReference er = new XmlDiffEntityReference(reader.Name);
            er.LineNumber = pInfo.LineNumber;
            er.LinePosition = pInfo.LinePosition;
            InsertChild(parent, er);
        }

        private void SetElementEndPosition(XmlDiffElement elem, PositionInfo pInfo)
        {
            Debug.Assert(elem != null);
            elem.EndLineNumber = pInfo.LineNumber;
            elem.EndLinePosition = pInfo.LinePosition;
        }


        private void InsertChild(XmlDiffNode parent, XmlDiffNode newChild)
        {
            if (IgnoreChildOrder)
            {
                XmlDiffNode child = parent.FirstChild;
                XmlDiffNode prevChild = null;
                while (child != null && (ComparePosition(child, newChild) == NodePosition.After))
                {
                    prevChild = child;
                    child = child.NextSibling;
                }
                parent.InsertChildAfter(prevChild, newChild);
            }
            else
                parent.InsertChildAfter(parent.LastChild, newChild);
        }

        private void InsertTopLevelAttributeAsText(XmlDiffNode parent, XmlDiffCharacterData newChild)
        {
            if (parent.LastChild != null && (parent.LastChild.NodeType == XmlDiffNodeType.Text || parent.LastChild.NodeType == XmlDiffNodeType.WS))
            {
                ((XmlDiffCharacterData)parent.LastChild).Value = ((XmlDiffCharacterData)parent.LastChild).Value + " " + newChild.Value;
            }
            else
            {
                parent.InsertChildAfter(parent.LastChild, newChild);
            }
        }

        private void InsertAttribute(XmlDiffElement parent, XmlDiffAttribute newAttr)
        {
            Debug.Assert(parent != null);
            Debug.Assert(newAttr != null);
            newAttr._parent = parent;
            if (IgnoreAttributeOrder)
            {
                XmlDiffAttribute attr = parent.FirstAttribute;
                XmlDiffAttribute prevAttr = null;
                while (attr != null && (CompareAttributes(attr, newAttr) == NodePosition.After))
                {
                    prevAttr = attr;
                    attr = (XmlDiffAttribute)(attr.NextSibling);
                }
                parent.InsertAttributeAfter(prevAttr, newAttr);
            }
            else
                parent.InsertAttributeAfter(parent.LastAttribute, newAttr);
        }

        public override void WriteTo(XmlWriter w)
        {
            WriteContentTo(w);
        }

        public override void WriteContentTo(XmlWriter w)
        {
            XmlDiffNode child = FirstChild;
            while (child != null)
            {
                child.WriteTo(w);
                child = child.NextSibling;
            }
        }
        public XmlDiffNavigator CreateNavigator()
        {
            return new XmlDiffNavigator(this);
        }

        public void SortChildren()
        {
            if (this.FirstChild != null)
            {
                XmlDiffNode _first = this.FirstChild;
                XmlDiffNode _current = this.FirstChild;
                XmlDiffNode _last = this.LastChild;
                this._firstChild = null;
                this._lastChild = null;
                //set flag to ignore child order
                bool temp = IgnoreChildOrder;
                IgnoreChildOrder = true;
                XmlDiffNode _next = null;
                do
                {
                    if (_current is XmlDiffElement)
                        _next = _current._next;
                    _current._next = null;
                    InsertChild(this, _current);
                    if (_current == _last)
                        break;
                    _current = _next;
                }
                while (true);
                //restore flag for ignoring child order
                IgnoreChildOrder = temp;
            }
        }

        void SortChildren(XmlDiffElement elem)
        {
            if (elem.FirstChild != null)
            {
                XmlDiffNode _first = elem.FirstChild;
                XmlDiffNode _current = elem.FirstChild;
                XmlDiffNode _last = elem.LastChild;
                elem._firstChild = null;
                elem._lastChild = null;
                //set flag to ignore child order
                bool temp = IgnoreChildOrder;
                IgnoreChildOrder = true;
                XmlDiffNode _next = null;
                do
                {
                    if (_current is XmlDiffElement)
                        _next = _current._next;
                    _current._next = null;
                    InsertChild(elem, _current);
                    if (_current == _last)
                        break;
                    _current = _next;
                }
                while (true);
                //restore flag for ignoring child order
                IgnoreChildOrder = temp;
            }
        }
    }

    //navgator over the xmldiffdocument
    public class XmlDiffNavigator
    {
        private XmlDiffDocument _document;
        private XmlDiffNode _currentNode;

        public XmlDiffNavigator(XmlDiffDocument doc)
        {
            _document = doc;
            _currentNode = _document;
        }
        public XmlDiffNavigator Clone()
        {
            XmlDiffNavigator _clone = new XmlDiffNavigator(_document);
            if (!_clone.MoveTo(this))
                throw new Exception("Cannot clone");
            return _clone;
        }

        public NodePosition ComparePosition(XmlDiffNavigator nav)
        {
            XmlDiffNode targetNode = ((XmlDiffNavigator)nav).CurrentNode;
            if (!(nav is XmlDiffNavigator))
            {
                return NodePosition.Unknown;
            }
            if (targetNode == this.CurrentNode)
            {
                return NodePosition.Same;
            }
            else
            {
                if (this.CurrentNode.ParentNode == null) //this is root
                {
                    return NodePosition.After;
                }
                else if (targetNode.ParentNode == null) //this is root
                {
                    return NodePosition.Before;
                }
                else //look in the following nodes
                {
                    if (targetNode.LineNumber != 0 && this.CurrentNode.LineNumber != 0)
                    {
                        if (targetNode.LineNumber > this.CurrentNode.LineNumber)
                        {
                            return NodePosition.Before;
                        }
                        else if (targetNode.LineNumber == this.CurrentNode.LineNumber && targetNode.LinePosition > this.CurrentNode.LinePosition)
                        {
                            return NodePosition.Before;
                        }
                        else
                            return NodePosition.After;
                    }

                    return NodePosition.Before;
                }
            }
        }
        public String GetAttribute(String localName, String namespaceURI)
        {
            if (_currentNode is XmlDiffElement)
            {
                return ((XmlDiffElement)_currentNode).GetAttributeValue(localName, namespaceURI);
            }
            return "";
        }

        public String GetNamespace(String name)
        {
            Debug.Assert(false, "GetNamespace is NYI");
            return "";
        }

        public bool IsSamePosition(XmlDiffNavigator other)
        {
            if (other is XmlDiffNavigator)
            {
                if (_currentNode == ((XmlDiffNavigator)other).CurrentNode)
                    return true;
            }
            return false;
        }

        public bool MoveTo(XmlDiffNavigator other)
        {
            if (other is XmlDiffNavigator)
            {
                _currentNode = ((XmlDiffNavigator)other).CurrentNode;
                return true;
            }
            return false;
        }

        public bool MoveToAttribute(String localName, String namespaceURI)
        {
            if (_currentNode is XmlDiffElement)
            {
                XmlDiffAttribute _attr = ((XmlDiffElement)_currentNode).GetAttribute(localName, namespaceURI);
                if (_attr != null)
                {
                    _currentNode = _attr;
                    return true;
                }
            }
            return false;
        }
        public bool MoveToFirst()
        {
            if (!(_currentNode is XmlDiffAttribute))
            {
                if (_currentNode.ParentNode.FirstChild == _currentNode)
                {
                    if (_currentNode.ParentNode.FirstChild._next != null)
                    {
                        _currentNode = _currentNode.ParentNode.FirstChild._next;
                        return true;
                    }
                }
                else
                {
                    _currentNode = _currentNode.ParentNode.FirstChild;
                    return true;
                }
            }
            return false;
        }
        public bool MoveToFirstAttribute()
        {
            if (_currentNode is XmlDiffElement)
            {
                if (((XmlDiffElement)_currentNode).FirstAttribute != null)
                {
                    XmlDiffAttribute _attr = ((XmlDiffElement)_currentNode).FirstAttribute;
                    while (_attr != null && IsNamespaceNode(_attr))
                    {
                        _attr = (XmlDiffAttribute)_attr._next;
                    }
                    if (_attr != null)
                    {
                        _currentNode = _attr;
                        return true;
                    }
                }
            }
            return false;
        }
        public bool MoveToFirstChild()
        {
            if ((_currentNode is XmlDiffDocument || _currentNode is XmlDiffElement) && _currentNode.FirstChild != null)
            {
                _currentNode = _currentNode.FirstChild;
                return true;
            }
            return false;
        }
        public bool MoveToId(String id)
        {
            Debug.Assert(false, "MoveToId is NYI");
            return false;
        }
        public bool MoveToNamespace(String name)
        {
            Debug.Assert(false, "MoveToNamespace is NYI");
            return false;
        }
        public bool MoveToNext()
        {
            if (!(_currentNode is XmlDiffAttribute) && _currentNode._next != null)
            {
                _currentNode = _currentNode._next;
                return true;
            }
            return false;
        }
        public bool MoveToNextAttribute()
        {
            if (_currentNode is XmlDiffAttribute)
            {
                XmlDiffAttribute _attr = (XmlDiffAttribute)_currentNode._next;
                while (_attr != null && IsNamespaceNode(_attr))
                {
                    _attr = (XmlDiffAttribute)_attr._next;
                }
                if (_attr != null)
                {
                    _currentNode = _attr;
                    return true;
                }
            }
            return false;
        }
        private bool IsNamespaceNode(XmlDiffAttribute attr)
        {
            return attr.LocalName.ToLower() == "xmlns" ||
                   attr.Prefix.ToLower() == "xmlns";
        }
        public bool MoveToParent()
        {
            if (!(_currentNode is XmlDiffDocument))
            {
                _currentNode = _currentNode.ParentNode;
                return true;
            }
            return false;
        }
        public bool MoveToPrevious()
        {
            if (_currentNode != _currentNode.ParentNode.FirstChild)
            {
                XmlDiffNode _current = _currentNode.ParentNode.FirstChild;
                XmlDiffNode _prev = _currentNode.ParentNode.FirstChild;
                while (_current != _currentNode)
                {
                    _prev = _current;
                    _current = _current._next;
                }
                _currentNode = _prev;
                return true;
            }
            return false;
        }
        public void MoveToRoot()
        {
            _currentNode = _document;
        }

        public string LocalName
        {
            get
            {
                if (_currentNode.NodeType == XmlDiffNodeType.Element)
                {
                    return ((XmlDiffElement)_currentNode).LocalName;
                }
                else if (_currentNode.NodeType == XmlDiffNodeType.Attribute)
                {
                    return ((XmlDiffAttribute)_currentNode).LocalName;
                }
                else if (_currentNode.NodeType == XmlDiffNodeType.PI)
                {
                    return ((XmlDiffProcessingInstruction)_currentNode).Name;
                }
                return "";
            }
        }

        public string Name
        {
            get
            {
                if (_currentNode.NodeType == XmlDiffNodeType.Element)
                {
                    return _document.nameTable.Get(((XmlDiffElement)_currentNode).Name);
                }
                else if (_currentNode.NodeType == XmlDiffNodeType.Attribute)
                {
                    return ((XmlDiffAttribute)_currentNode).Name;
                }
                else if (_currentNode.NodeType == XmlDiffNodeType.PI)
                {
                    return ((XmlDiffProcessingInstruction)_currentNode).Name;
                }
                return "";
            }
        }

        public string NamespaceURI
        {
            get
            {
                if (_currentNode is XmlDiffElement)
                {
                    return ((XmlDiffElement)_currentNode).NamespaceURI;
                }
                else if (_currentNode is XmlDiffAttribute)
                {
                    return ((XmlDiffAttribute)_currentNode).NamespaceURI;
                }
                return "";
            }
        }

        public string Value
        {
            get
            {
                if (_currentNode is XmlDiffAttribute)
                {
                    return ((XmlDiffAttribute)_currentNode).Value;
                }
                else if (_currentNode is XmlDiffCharacterData)
                {
                    return ((XmlDiffCharacterData)_currentNode).Value;
                }
                else if (_currentNode is XmlDiffElement)
                {
                    return ((XmlDiffElement)_currentNode).Value;
                }
                return "";
            }
        }

        public string Prefix
        {
            get
            {
                if (_currentNode is XmlDiffElement)
                {
                    return ((XmlDiffElement)_currentNode).Prefix;
                }
                else if (_currentNode is XmlDiffAttribute)
                {
                    return ((XmlDiffAttribute)_currentNode).Prefix;
                }
                return "";
            }
        }

        public string BaseURI
        {
            get
            {
                Debug.Assert(false, "BaseURI is NYI");
                return "";
            }
        }
        public string XmlLang
        {
            get
            {
                Debug.Assert(false, "XmlLang not supported");
                return "";
            }
        }
        public bool HasAttributes
        {
            get
            {
                return (_currentNode is XmlDiffElement && ((XmlDiffElement)_currentNode).FirstAttribute != null) ? true : false;
            }
        }
        public bool HasChildren
        {
            get
            {
                return _currentNode._next != null ? true : false;
            }
        }
        public bool IsEmptyElement
        {
            get
            {
                return _currentNode is XmlDiffEmptyElement ? true : false;
            }
        }
        public XmlNameTable NameTable
        {
            get
            {
                return _document.nameTable;
            }
        }
        public XmlDiffNode CurrentNode
        {
            get
            {
                return _currentNode;
            }
        }
        public bool IsOnRoot()
        {
            return _currentNode == null ? true : false;
        }
    }

    public class PropertyCollection : MyDict<string, object> { }

    public abstract class XmlDiffNode
    {
        internal XmlDiffNode _next;
        internal XmlDiffNode _firstChild;
        internal XmlDiffNode _lastChild;
        internal XmlDiffNode _parent;
        internal int _lineNumber, _linePosition;
        internal bool _bIgnoreValue;
        private PropertyCollection _extendedProperties;

        public XmlDiffNode()
        {
            this._next = null;
            this._firstChild = null;
            this._lastChild = null;
            this._parent = null;
            _lineNumber = 0;
            _linePosition = 0;
        }

        public XmlDiffNode FirstChild
        {
            get
            {
                return this._firstChild;
            }
        }
        public XmlDiffNode LastChild
        {
            get
            {
                return this._lastChild;
            }
        }
        public XmlDiffNode NextSibling
        {
            get
            {
                return this._next;
            }
        }
        public XmlDiffNode ParentNode
        {
            get
            {
                return this._parent;
            }
        }

        public virtual bool IgnoreValue
        {
            get
            {
                return _bIgnoreValue;
            }
            set
            {
                _bIgnoreValue = value;
                XmlDiffNode current = this._firstChild;
                while (current != null)
                {
                    current.IgnoreValue = value;
                    current = current._next;
                }
            }
        }


        public abstract XmlDiffNodeType NodeType { get; }

        public virtual string OuterXml
        {
            get
            {
                StringWriter sw = new StringWriter();
                XmlWriterSettings xws = new XmlWriterSettings();
                xws.ConformanceLevel = ConformanceLevel.Auto;
                xws.CheckCharacters = false;
                XmlWriter xw = XmlWriter.Create(sw, xws);

                WriteTo(xw);
                xw.Dispose();

                return sw.ToString();
            }
        }
        public virtual string InnerXml
        {
            get
            {
                StringWriter sw = new StringWriter();
                XmlWriterSettings xws = new XmlWriterSettings();
                xws.ConformanceLevel = ConformanceLevel.Auto;
                xws.CheckCharacters = false;
                XmlWriter xw = XmlWriter.Create(sw, xws);

                WriteTo(xw);
                xw.Dispose();

                return sw.ToString();
            }
        }

        public abstract void WriteTo(XmlWriter w);
        public abstract void WriteContentTo(XmlWriter w);

        public PropertyCollection ExtendedProperties
        {
            get
            {
                if (_extendedProperties == null)
                    _extendedProperties = new PropertyCollection();
                return _extendedProperties;
            }
        }
        public virtual void InsertChildAfter(XmlDiffNode child, XmlDiffNode newChild)
        {
            Debug.Assert(newChild != null);
            newChild._parent = this;
            if (child == null)
            {
                newChild._next = this._firstChild;
                this._firstChild = newChild;
            }
            else
            {
                Debug.Assert(child._parent == this);
                newChild._next = child._next;
                child._next = newChild;
            }
            if (newChild._next == null)
                this._lastChild = newChild;
        }

        public virtual void DeleteChild(XmlDiffNode child)
        {
            if (child == this.FirstChild)//delete head
            {
                _firstChild = this.FirstChild.NextSibling;
            }
            else
            {
                XmlDiffNode current = this.FirstChild;
                XmlDiffNode previous = null;
                while (current != child)
                {
                    previous = current;
                    current = current.NextSibling;
                }
                Debug.Assert(current != null);
                if (current == this.LastChild) //tail being deleted
                {
                    this._lastChild = current.NextSibling;
                }
                previous._next = current.NextSibling;
            }
        }

        public int LineNumber
        {
            get { return this._lineNumber; }
            set { this._lineNumber = value; }
        }

        public int LinePosition
        {
            get { return this._linePosition; }
            set { this._linePosition = value; }
        }
    }

    public class XmlDiffElement : XmlDiffNode
    {
        private string _lName;
        private string _prefix;
        private string _ns;
        private XmlDiffAttribute _firstAttribute;
        private XmlDiffAttribute _lastAttribute;
        private int _attrC;
        private int _endLineNumber, _endLinePosition;

        public XmlDiffElement(string localName, string prefix, string ns)
            : base()
        {
            this._lName = localName;
            this._prefix = prefix;
            this._ns = ns;
            this._firstAttribute = null;
            this._lastAttribute = null;
            this._attrC = -1;
        }

        public override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Element; } }
        public string LocalName { get { return this._lName; } }
        public string NamespaceURI { get { return this._ns; } }
        public string Prefix { get { return this._prefix; } }

        public string Name
        {
            get
            {
                if (this._prefix.Length > 0)
                    return Prefix + ":" + LocalName;
                else
                    return LocalName;
            }
        }

        public XmlDiffAttribute FirstAttribute
        {
            get
            {
                return this._firstAttribute;
            }
        }
        public XmlDiffAttribute LastAttribute
        {
            get
            {
                return this._lastAttribute;
            }
        }
        public string GetAttributeValue(string LocalName, string NamespaceUri)
        {
            if (_firstAttribute != null)
            {
                XmlDiffAttribute _current = _firstAttribute;
                do
                {
                    if (_current.LocalName == LocalName && _current.NamespaceURI == NamespaceURI)
                    {
                        return _current.Value;
                    }
                    _current = (XmlDiffAttribute)_current._next;
                }
                while (_current != _lastAttribute);
            }
            return "";
        }

        public XmlDiffAttribute GetAttribute(string LocalName, string NamespaceUri)
        {
            if (_firstAttribute != null)
            {
                XmlDiffAttribute _current = _firstAttribute;
                do
                {
                    if (_current.LocalName == LocalName && _current.NamespaceURI == NamespaceURI)
                    {
                        return _current;
                    }
                    _current = (XmlDiffAttribute)_current._next;
                }
                while (_current != _lastAttribute);
            }
            return null;
        }

        internal void InsertAttributeAfter(XmlDiffAttribute attr, XmlDiffAttribute newAttr)
        {
            Debug.Assert(newAttr != null);
            newAttr._ownerElement = this;
            if (attr == null)
            {
                newAttr._next = this._firstAttribute;
                this._firstAttribute = newAttr;
            }
            else
            {
                Debug.Assert(attr._ownerElement == this);
                newAttr._next = attr._next;
                attr._next = newAttr;
            }
            if (newAttr._next == null)
                this._lastAttribute = newAttr;
        }

        internal void DeleteAttribute(XmlDiffAttribute attr)
        {
            if (attr == this.FirstAttribute)//delete head
            {
                if (attr == this.LastAttribute) //tail being deleted
                {
                    this._lastAttribute = (XmlDiffAttribute)attr.NextSibling;
                }
                _firstAttribute = (XmlDiffAttribute)this.FirstAttribute.NextSibling;
            }
            else
            {
                XmlDiffAttribute current = this.FirstAttribute;
                XmlDiffAttribute previous = null;
                while (current != attr)
                {
                    previous = current;
                    current = (XmlDiffAttribute)current.NextSibling;
                }
                Debug.Assert(current != null);
                if (current == this.LastAttribute) //tail being deleted
                {
                    this._lastAttribute = (XmlDiffAttribute)current.NextSibling;
                }
                previous._next = current.NextSibling;
            }
        }

        public int AttributeCount
        {
            get
            {
                if (this._attrC != -1)
                    return this._attrC;
                XmlDiffAttribute attr = this._firstAttribute;
                this._attrC = 0;
                while (attr != null)
                {
                    this._attrC++;
                    attr = (XmlDiffAttribute)attr.NextSibling;
                }
                return this._attrC;
            }
        }
        public override bool IgnoreValue
        {
            set
            {
                base.IgnoreValue = value;
                XmlDiffAttribute current = this._firstAttribute;
                while (current != null)
                {
                    current.IgnoreValue = value;
                    current = (XmlDiffAttribute)current._next;
                }
            }
        }

        public int EndLineNumber
        {
            get { return this._endLineNumber; }
            set { this._endLineNumber = value; }
        }

        public int EndLinePosition
        {
            get { return this._endLinePosition; }
            set { this._endLinePosition = value; }
        }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteStartElement(Prefix, LocalName, NamespaceURI);
            XmlDiffAttribute attr = this._firstAttribute;
            while (attr != null)
            {
                attr.WriteTo(w);
                attr = (XmlDiffAttribute)(attr.NextSibling);
            }
            WriteContentTo(w);
            w.WriteFullEndElement();
        }

        public override void WriteContentTo(XmlWriter w)
        {
            XmlDiffNode child = FirstChild;
            while (child != null)
            {
                child.WriteTo(w);
                child = child.NextSibling;
            }
        }

        public string Value
        {
            get
            {
                if (this.IgnoreValue)
                {
                    return "";
                }
                if (_firstChild != null)
                {
                    StringBuilder _bldr = new StringBuilder();
                    XmlDiffNode _current = _firstChild;
                    do
                    {
                        if (_current is XmlDiffCharacterData && _current.NodeType != XmlDiffNodeType.Comment && _current.NodeType != XmlDiffNodeType.PI)
                        {
                            _bldr.Append(((XmlDiffCharacterData)_current).Value);
                        }
                        else if (_current is XmlDiffElement)
                        {
                            _bldr.Append(((XmlDiffElement)_current).Value);
                        }
                        _current = _current._next;
                    }
                    while (_current != null);
                    return _bldr.ToString();
                }
                return "";
            }
        }
    }

    public class XmlDiffEmptyElement : XmlDiffElement
    {
        public XmlDiffEmptyElement(string localName, string prefix, string ns) : base(localName, prefix, ns) { }
    }

    public class XmlDiffAttribute : XmlDiffNode
    {
        internal XmlDiffElement _ownerElement;
        private string _lName;
        private string _prefix;
        private string _ns;
        private string _value;

        public XmlDiffAttribute(string localName, string prefix, string ns, string value)
            : base()
        {
            this._lName = localName;
            this._prefix = prefix;
            this._ns = ns;
            this._value = value;
        }

        public string Value
        {
            get
            {
                if (this.IgnoreValue)
                {
                    return "";
                }
                return this._value;
            }
        }
        public string LocalName { get { return this._lName; } }
        public string NamespaceURI { get { return this._ns; } }
        public string Prefix { get { return this._prefix; } }

        public string Name
        {
            get
            {
                if (this._prefix.Length > 0)
                    return this._prefix + ":" + this._lName;
                else
                    return this._lName;
            }
        }
        public override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Attribute; } }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteStartAttribute(Prefix, LocalName, NamespaceURI);
            WriteContentTo(w);
            w.WriteEndAttribute();
        }

        public override void WriteContentTo(XmlWriter w)
        {
            w.WriteString(Value);
        }
    }

    public class XmlDiffEntityReference : XmlDiffNode
    {
        private string _name;
        public XmlDiffEntityReference(string name)
            : base()
        {
            this._name = name;
        }
        public override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.ER; } }
        public string Name { get { return this._name; } }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteEntityRef(this._name);
        }

        public override void WriteContentTo(XmlWriter w)
        {
            XmlDiffNode child = this.FirstChild;
            while (child != null)
            {
                child.WriteTo(w);
                child = child.NextSibling;
            }
        }
    }

    public class XmlDiffCharacterData : XmlDiffNode
    {
        private string _value;
        private XmlDiffNodeType _nodetype;
        public XmlDiffCharacterData(string value, XmlDiffNodeType nt, bool NormalizeNewline)
            : base()
        {
            this._value = value;
            if (NormalizeNewline)
            {
                this._value = this._value.Replace("\n", "");
                this._value = this._value.Replace("\r", "");
            }
            this._nodetype = nt;
        }

        public string Value
        {
            get
            {
                if (this.IgnoreValue)
                {
                    return "";
                }
                return this._value;
            }
            set
            {
                _value = value;
            }
        }
        public override XmlDiffNodeType NodeType { get { return _nodetype; } }

        public override void WriteTo(XmlWriter w)
        {
            switch (this._nodetype)
            {
                case XmlDiffNodeType.Comment:
                    w.WriteComment(Value);
                    break;
                case XmlDiffNodeType.CData:
                    w.WriteCData(Value);
                    break;
                case XmlDiffNodeType.WS:
                case XmlDiffNodeType.Text:
                    w.WriteString(Value);
                    break;
                default:
                    Debug.Assert(false, "Wrong type for text-like node : " + this._nodetype.ToString());
                    break;
            }
        }

        public override void WriteContentTo(XmlWriter w) { }
    }

    public class XmlDiffProcessingInstruction : XmlDiffCharacterData
    {
        private string _name;
        public XmlDiffProcessingInstruction(string name, string value)
            : base(value, XmlDiffNodeType.PI, false)
        {
            this._name = name;
        }
        public string Name { get { return this._name; } }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteProcessingInstruction(this._name, Value);
        }
        public override void WriteContentTo(XmlWriter w) { }
    }
}

