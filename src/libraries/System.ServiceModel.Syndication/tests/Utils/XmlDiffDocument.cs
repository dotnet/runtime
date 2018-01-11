// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace System.ServiceModel.Syndication.Tests
{
    public enum NodePosition
    {
        Before = 0,
        After = 1,
        Unknown = 2
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
        public override int LinePosition { get { return _mlineInfo.LinePosition; } }
    }

    //nametable could be added for next "release"
    public class XmlDiffNameTable
    {
    }


    public class XmlDiffDocument : XmlDiffNode, IXPathNavigable
    {
        //XmlDiffNameTable    _nt;
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
        private bool _bTreatWhitespaceTextAsWSNode = false;
        private bool _bIgnoreEmptyTextNodes = false;
        private bool _bWhitespaceAsText = false;
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
            _bWhitespaceAsText = false;
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
                this.TreatWhitespaceTextAsWSNode = (((int)value & (int)(XmlDiffOption.TreatWhitespaceTextAsWSNode)) > 0);
                this.IgnoreEmptyTextNodes = (((int)value & (int)(XmlDiffOption.IgnoreEmptyTextNodes)) > 0);
                this.WhitespaceAsText = (((int)value & (int)(XmlDiffOption.WhitespaceAsText)) > 0);
            }
        }
        public override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Document; } }

        public bool IgnoreAttributeOrder
        {
            get { return _bIgnoreAttributeOrder; }
            set { _bIgnoreAttributeOrder = value; }
        }

        public bool IgnoreChildOrder
        {
            get { return _bIgnoreChildOrder; }
            set { _bIgnoreChildOrder = value; }
        }

        public bool IgnoreComments
        {
            get { return _bIgnoreComments; }
            set { _bIgnoreComments = value; }
        }

        public bool IgnoreWhitespace
        {
            get { return _bIgnoreWhitespace; }
            set { _bIgnoreWhitespace = value; }
        }

        public bool IgnoreDTD
        {
            get { return _bIgnoreDTD; }
            set { _bIgnoreDTD = value; }
        }

        public bool IgnoreNS
        {
            get { return _bIgnoreNS; }
            set { _bIgnoreNS = value; }
        }

        public bool IgnorePrefix
        {
            get { return _bIgnorePrefix; }
            set { _bIgnorePrefix = value; }
        }

        public bool CDataAsText
        {
            get { return _bCDataAsText; }
            set { _bCDataAsText = value; }
        }

        public bool NormalizeNewline
        {
            get { return _bNormalizeNewline; }
            set { _bNormalizeNewline = value; }
        }

        public bool TreatWhitespaceTextAsWSNode
        {
            get { return _bTreatWhitespaceTextAsWSNode; }
            set { _bTreatWhitespaceTextAsWSNode = value; }
        }

        public bool IgnoreEmptyTextNodes
        {
            get { return _bIgnoreEmptyTextNodes; }
            set { _bIgnoreEmptyTextNodes = value; }
        }

        public bool WhitespaceAsText
        {
            get { return _bWhitespaceAsText; }
            set { _bWhitespaceAsText = value; }
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
                Debug.Assert(false, "ComparePosition meets an undecision situation.");
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
            {
                //pi2 > pi1
                return NodePosition.After;
            }
            else
            {
                //pi2 < pi1
                return NodePosition.Before;
            }
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
            return Math.Sign(String.Compare(s1, s2, StringComparison.Ordinal));
        }

        public virtual void Load(string xmlFileName)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            if (IgnoreDTD)
            {
                readerSettings.ValidationType = ValidationType.None;
            }
            else
            {
                readerSettings.ValidationType = ValidationType.DTD;
            }

            using (XmlReader reader = XmlReader.Create(xmlFileName))
            {
                Load(reader);
                reader.Close();
            }
        }

        public virtual void Load(XmlReader reader)
        {
            if (_bLoaded)
                throw new InvalidOperationException("The document already contains data and should not be used again.");
            if (reader.ReadState == ReadState.Initial)
            {
                if (!reader.Read())
                {
                    return;
                }
            }
            PositionInfo pInfo = PositionInfo.GetPositionInfo(reader);
            ReadChildNodes(this, reader, pInfo);
            _bLoaded = true;
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
                    case XmlNodeType.Whitespace:
                        if (!IgnoreWhitespace)
                        {
                            if (this.WhitespaceAsText)
                            {
                                LoadTextNode(parent, reader, pInfo, XmlDiffNodeType.Text);
                            }
                            else
                            {
                                LoadTextNode(parent, reader, pInfo, XmlDiffNodeType.WS);
                            }
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
                            LoadTextNode(parent, reader, pInfo, TextNodeIsWhitespace(reader.Value) ? XmlDiffNodeType.WS : XmlDiffNodeType.Text);
                        }
                        else //merge with adjacent text/CDATA nodes
                        {
                            StringBuilder text = new StringBuilder();
                            text.Append(reader.Value);
                            while ((lookAhead = reader.Read()) && (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA))
                            {
                                text.Append(reader.Value);
                            }
                            string txt = text.ToString();
                            LoadTextNode(parent, txt, pInfo, TextNodeIsWhitespace(txt) ? XmlDiffNodeType.WS : XmlDiffNodeType.Text);
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

        private bool TextNodeIsWhitespace(string p)
        {
            if (!this.TreatWhitespaceTextAsWSNode) return false;
            for (int i = 0; i < p.Length; i++)
            {
                if (!Char.IsWhiteSpace(p[i])) return false;
            }
            return true;
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
                //            bool rtn = reader.Read();
                //			rtn = reader.Read();
                reader.Read(); //move to child
                ReadChildNodes(elem, reader, pInfo);
            }
            InsertChild(parent, elem);
        }

        private void ReadAttributes(XmlDiffElement parent, XmlReader reader, PositionInfo pInfo)
        {
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    XmlDiffAttribute attr = new XmlDiffAttribute(reader.LocalName, reader.Prefix, reader.NamespaceURI, reader.Value);
                    attr.SetValueAsQName(reader, reader.Value);
                    attr.LineNumber = pInfo.LineNumber;
                    attr.LinePosition = pInfo.LinePosition;
                    InsertAttribute(parent, attr);
                }
                while (reader.MoveToNextAttribute());
            }
        }

        private void LoadTextNode(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo, XmlDiffNodeType nt)
        {
            LoadTextNode(parent, reader.Value, pInfo, nt);
        }

        private void LoadTextNode(XmlDiffNode parent, string text, PositionInfo pInfo, XmlDiffNodeType nt)
        {
            if (!this.IgnoreEmptyTextNodes || !String.IsNullOrEmpty(text))
            {
                XmlDiffCharacterData textNode = new XmlDiffCharacterData(text, nt, this.NormalizeNewline);
                textNode.LineNumber = pInfo.LineNumber;
                textNode.LinePosition = pInfo.LinePosition;
                InsertChild(parent, textNode);
            }
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

        //IXPathNavigable override
        public XPathNavigator CreateNavigator()
        {
            return new XmlDiffNavigator(this);
        }

        /*
        public void SortChildren(string expr, XmlNamespaceManager mngr)
        {
            XPathNavigator _nav = this.CreateNavigator();        
            XPathExpression _expr = _nav.Compile(expr);
            if(mngr != null)
            {
                _expr.SetContext(mngr);
            }
            XPathNodeIterator _iter = _nav.Select(_expr);
            while(_iter.MoveNext())
            {
                if(((XmlDiffNavigator)_iter.Current).CurrentNode is XmlDiffElement)
                {
                    SortChildren((XmlDiffElement)((XmlDiffNavigator)_iter.Current).CurrentNode);
                }
            }
        }
        */

        public void SortChildren(XPathExpression expr)
        {
            if (expr == null)
            {
                return;
            }
            XPathNavigator _nav = this.CreateNavigator();
            XPathNodeIterator _iter = _nav.Select(expr);
            while (_iter.MoveNext())
            {
                if (((XmlDiffNavigator)_iter.Current).CurrentNode is XmlDiffElement)
                {
                    SortChildren((XmlDiffElement)((XmlDiffNavigator)_iter.Current).CurrentNode);
                }
            }
        }

        public void IgnoreNodes(XPathExpression expr)
        {
            if (expr == null)
            {
                return;
            }
            XPathNavigator _nav = this.CreateNavigator();
            XPathNodeIterator _iter = _nav.Select(expr);
            while (_iter.MoveNext())
            {
                if (((XmlDiffNavigator)_iter.Current).CurrentNode is XmlDiffAttribute)
                {
                    ((XmlDiffElement)((XmlDiffNavigator)_iter.Current).CurrentNode.ParentNode).DeleteAttribute((XmlDiffAttribute)((XmlDiffNavigator)_iter.Current).CurrentNode);
                }
                else
                {
                    ((XmlDiffNavigator)_iter.Current).CurrentNode.ParentNode.DeleteChild(((XmlDiffNavigator)_iter.Current).CurrentNode);
                }
            }
        }

        public void IgnoreValues(XPathExpression expr)
        {
            if (expr == null)
            {
                return;
            }
            XPathNavigator _nav = this.CreateNavigator();
            XPathNodeIterator _iter = _nav.Select(expr);
            while (_iter.MoveNext())
            {
                ((XmlDiffNavigator)_iter.Current).CurrentNode.IgnoreValue = true; ;
            }
        }

        private void SortChildren(XmlDiffElement elem)
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
    public class XmlDiffNavigator : XPathNavigator
    {
        private XmlDiffDocument _document;
        private XmlDiffNode _currentNode;

        public XmlDiffNavigator(XmlDiffDocument doc)
        {
            _document = doc;
            _currentNode = _document;
        }
        public override XPathNavigator Clone()
        {
            XmlDiffNavigator _clone = new XmlDiffNavigator(_document);
            if (!_clone.MoveTo(this))
                throw new Exception("Cannot clone");
            return _clone;
        }
        public override XmlNodeOrder ComparePosition(XPathNavigator nav)
        {
            XmlDiffNode targetNode = ((XmlDiffNavigator)nav).CurrentNode;
            //        Debug.Assert(false, "ComparePosition is NYI");
            if (!(nav is XmlDiffNavigator))
            {
                return XmlNodeOrder.Unknown;
            }
            if (targetNode == this.CurrentNode)
            {
                return XmlNodeOrder.Same;
            }
            else
            {
                if (this.CurrentNode.ParentNode == null) //this is root
                {
                    return XmlNodeOrder.After;
                }
                else if (targetNode.ParentNode == null) //this is root
                {
                    return XmlNodeOrder.Before;
                }
                else //look in the following nodes
                {
                    if (targetNode.LineNumber + targetNode.LinePosition > this.CurrentNode.LinePosition + this.CurrentNode.LineNumber)
                    {
                        return XmlNodeOrder.After;
                    }
                    return XmlNodeOrder.Before;
                }
            }
        }
        public override String GetAttribute(String localName, String namespaceURI)
        {
            if (_currentNode is XmlDiffElement)
            {
                return ((XmlDiffElement)_currentNode).GetAttributeValue(localName, namespaceURI);
            }
            return "";
        }

        public override String GetNamespace(String name)
        {
            Debug.Assert(false, "GetNamespace is NYI");
            return "";
        }

        /*public override bool IsDescendant (XPathNavigator nav)  
        {
            Debug.Assert(false, "IsDescendant is NYI");
            return false;
        }
        */

        public override bool IsSamePosition(XPathNavigator other)
        {
            if (other is XmlDiffNavigator)
            {
                if (_currentNode == ((XmlDiffNavigator)other).CurrentNode)
                    return true;
            }
            return false;
        }

        public override bool MoveTo(XPathNavigator other)
        {
            if (other is XmlDiffNavigator)
            {
                _currentNode = ((XmlDiffNavigator)other).CurrentNode;
                return true;
            }
            return false;
        }

        public override bool MoveToAttribute(String localName, String namespaceURI)
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

        public override bool MoveToFirst()
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

        public override bool MoveToFirstAttribute()
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

        public override bool MoveToFirstChild()
        {
            if ((_currentNode is XmlDiffDocument || _currentNode is XmlDiffElement) && _currentNode.FirstChild != null)
            {
                _currentNode = _currentNode.FirstChild;
                return true;
            }
            return false;
        }

        //sunghonhack
        public new bool MoveToFirstNamespace()
        {
            if (_currentNode is XmlDiffElement)
            {
                if (((XmlDiffElement)_currentNode).FirstAttribute != null)
                {
                    XmlDiffAttribute _attr = ((XmlDiffElement)_currentNode).FirstAttribute;
                    while (_attr != null && !IsNamespaceNode(_attr))
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
        public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
        {
            return this.MoveToFirstNamespace();
        }
        public override bool MoveToId(String id)
        {
            Debug.Assert(false, "MoveToId is NYI");
            return false;
        }
        public override bool MoveToNamespace(String name)
        {
            Debug.Assert(false, "MoveToNamespace is NYI");
            return false;
        }
        public override bool MoveToNext()
        {
            if (!(_currentNode is XmlDiffAttribute) && _currentNode._next != null)
            {
                _currentNode = _currentNode._next;
                return true;
            }
            return false;
        }
        public override bool MoveToNextAttribute()
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

        //sunghonhack
        public new bool MoveToNextNamespace()
        {
            if (_currentNode is XmlDiffAttribute)
            {
                XmlDiffAttribute _attr = (XmlDiffAttribute)_currentNode._next;
                while (_attr != null && !IsNamespaceNode(_attr))
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
            return attr.LocalName.ToLower(System.Globalization.CultureInfo.InvariantCulture) == "xmlns" ||
                   attr.Prefix.ToLower(System.Globalization.CultureInfo.InvariantCulture) == "xmlns";
        }
        public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
        {
            return this.MoveToNextNamespace();
        }
        public override bool MoveToParent()
        {
            if (!(_currentNode is XmlDiffDocument))
            {
                _currentNode = _currentNode.ParentNode;
                return true;
            }
            return false;
        }
        public override bool MoveToPrevious()
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
        public override void MoveToRoot()
        {
            _currentNode = _document;
        }

        //properties

        public override XPathNodeType NodeType
        {
            get
            {
                //namespace, comment and whitespace node types are not supported
                switch (_currentNode.NodeType)
                {
                    case XmlDiffNodeType.Element:
                        return XPathNodeType.Element;
                    case XmlDiffNodeType.Attribute:
                        return XPathNodeType.Attribute;
                    case XmlDiffNodeType.ER:
                        return XPathNodeType.Text;
                    case XmlDiffNodeType.Text:
                        return XPathNodeType.Text;
                    case XmlDiffNodeType.CData:
                        return XPathNodeType.Text;
                    case XmlDiffNodeType.Comment:
                        return XPathNodeType.Comment;
                    case XmlDiffNodeType.PI:
                        return XPathNodeType.ProcessingInstruction;
                    case XmlDiffNodeType.WS:
                        return XPathNodeType.SignificantWhitespace;
                    case XmlDiffNodeType.Document:
                        return XPathNodeType.Root;
                    default:
                        return XPathNodeType.All;
                }
            }
        }

        public override string LocalName
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

        public override string Name
        {
            get
            {
                if (_currentNode.NodeType == XmlDiffNodeType.Element)
                {
                    //return ((XmlDiffElement)m_currentNode).Name;
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

        public override string NamespaceURI
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

        public override string Value
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

        public override string Prefix
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

        public override string BaseURI
        {
            get
            {
                Debug.Assert(false, "BaseURI is NYI");
                return "";
            }
        }
        public override string XmlLang
        {
            get
            {
                Debug.Assert(false, "XmlLang not supported");
                return "";
            }
        }
        public override bool HasAttributes
        {
            get
            {
                return (_currentNode is XmlDiffElement && ((XmlDiffElement)_currentNode).FirstAttribute != null) ? true : false;
            }
        }
        public override bool HasChildren
        {
            get
            {
                return _currentNode._next != null ? true : false;
            }
        }
        public override bool IsEmptyElement
        {
            get
            {
                return _currentNode is XmlDiffEmptyElement ? true : false;
            }
        }
        public override XmlNameTable NameTable
        {
            get
            {
                return _document.nameTable;
                //return new NameTable();
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



    public class PropertyCollection : Hashtable { }

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
                XmlTextWriter xw = new XmlTextWriter(sw);

                WriteTo(xw);
                xw.Close();

                return sw.ToString();
            }
        }
        public virtual string InnerXml
        {
            get
            {
                StringWriter sw = new StringWriter();
                XmlTextWriter xw = new XmlTextWriter(sw);

                WriteContentTo(xw);
                xw.Close();

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
            _lName = localName;
            _prefix = prefix;
            _ns = ns;
            _firstAttribute = null;
            _lastAttribute = null;
            _attrC = -1;
        }

        public override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Element; } }
        public string LocalName { get { return _lName; } }
        public string NamespaceURI { get { return _ns; } }
        public string Prefix { get { return _prefix; } }

        public string Name
        {
            get
            {
                if (_prefix.Length > 0)
                    return Prefix + ":" + LocalName;
                else
                    return LocalName;
            }
        }

        public XmlDiffAttribute FirstAttribute
        {
            get
            {
                return _firstAttribute;
            }
        }
        public XmlDiffAttribute LastAttribute
        {
            get
            {
                return _lastAttribute;
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
                newAttr._next = _firstAttribute;
                _firstAttribute = newAttr;
            }
            else
            {
                Debug.Assert(attr._ownerElement == this);
                newAttr._next = attr._next;
                attr._next = newAttr;
            }
            if (newAttr._next == null)
                _lastAttribute = newAttr;
        }

        internal void DeleteAttribute(XmlDiffAttribute attr)
        {
            if (attr == this.FirstAttribute)//delete head
            {
                if (attr == this.LastAttribute) //tail being deleted
                {
                    _lastAttribute = (XmlDiffAttribute)attr.NextSibling;
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
                    _lastAttribute = (XmlDiffAttribute)current.NextSibling;
                }
                previous._next = current.NextSibling;
            }
        }

        public int AttributeCount
        {
            get
            {
                if (_attrC != -1)
                    return _attrC;
                XmlDiffAttribute attr = _firstAttribute;
                _attrC = 0;
                while (attr != null)
                {
                    _attrC++;
                    attr = (XmlDiffAttribute)attr.NextSibling;
                }
                return _attrC;
            }
        }
        public override bool IgnoreValue
        {
            set
            {
                base.IgnoreValue = value;
                XmlDiffAttribute current = _firstAttribute;
                while (current != null)
                {
                    current.IgnoreValue = value;
                    current = (XmlDiffAttribute)current._next;
                }
            }
        }

        public int EndLineNumber
        {
            get { return _endLineNumber; }
            set { _endLineNumber = value; }
        }

        public int EndLinePosition
        {
            get { return _endLinePosition; }
            set { _endLinePosition = value; }
        }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteStartElement(Prefix, LocalName, NamespaceURI);
            XmlDiffAttribute attr = _firstAttribute;
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
        private XmlQualifiedName _valueAsQName;

        public XmlDiffAttribute(string localName, string prefix, string ns, string value)
            : base()
        {
            _lName = localName;
            _prefix = prefix;
            _ns = ns;
            _value = value;
        }

        public string Value
        {
            get
            {
                if (this.IgnoreValue)
                {
                    return "";
                }
                return _value;
            }
        }
        public XmlQualifiedName ValueAsQName
        {
            get { return _valueAsQName; }
        }

        internal void SetValueAsQName(XmlReader reader, string value)
        {
            int indexOfColon = value.IndexOf(':');
            if (indexOfColon == -1)
            {
                _valueAsQName = new XmlQualifiedName(value);
            }
            else
            {
                string prefix = value.Substring(0, indexOfColon);
                string ns = reader.LookupNamespace(prefix);
                if (ns == null)
                {
                    _valueAsQName = null;
                }
                else
                {
                    try
                    {
                        string localName = XmlConvert.VerifyNCName(value.Substring(indexOfColon + 1));
                        _valueAsQName = new XmlQualifiedName(localName, ns);
                    }
                    catch (XmlException)
                    {
                        _valueAsQName = null;
                    }
                }
            }
        }

        public string LocalName { get { return _lName; } }
        public string NamespaceURI { get { return _ns; } }
        public string Prefix { get { return _prefix; } }

        public string Name
        {
            get
            {
                if (_prefix.Length > 0)
                    return _prefix + ":" + _lName;
                else
                    return _lName;
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
            _name = name;
        }
        public override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.ER; } }
        public string Name { get { return _name; } }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteEntityRef(_name);
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
            _value = value;
            if (NormalizeNewline)
            {
                _value = _value.Replace("\n", "");
                _value = _value.Replace("\r", "");
            }
            _nodetype = nt;
        }

        public string Value
        {
            get
            {
                if (this.IgnoreValue)
                {
                    return "";
                }
                return _value;
            }
            set
            {
                _value = value;
            }
        }
        public override XmlDiffNodeType NodeType { get { return _nodetype; } }

        public override void WriteTo(XmlWriter w)
        {
            switch (_nodetype)
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
                    Debug.Assert(false, "Wrong type for text-like node : " + _nodetype.ToString());
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
            _name = name;
        }
        public string Name { get { return _name; } }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteProcessingInstruction(_name, Value);
        }
        public override void WriteContentTo(XmlWriter w) { }
    }
}
