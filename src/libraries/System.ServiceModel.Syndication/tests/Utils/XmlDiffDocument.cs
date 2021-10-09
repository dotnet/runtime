// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
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
        public virtual bool HasLineInfo() => false;
        public virtual int LineNumber => 0;
        public virtual int LinePosition => 0;

        public static PositionInfo GetPositionInfo(object o)
        {
            if (o is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
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

        public override bool HasLineInfo() => true;
        public override int LineNumber => _mlineInfo.LineNumber;
        public override int LinePosition => _mlineInfo.LinePosition;
    }

    //nametable could be added for next "release"
    public class XmlDiffNameTable
    {
    }

    public class XmlDiffDocument : XmlDiffNode, IXPathNavigable
    {
        private bool _bLoaded;
        public XmlNameTable nameTable;

        public XmlDiffDocument() : base()
        {
            _bLoaded = false;
            IgnoreAttributeOrder = false;
            IgnoreChildOrder = false;
            IgnoreComments = false;
            IgnoreWhitespace = false;
            IgnoreDTD = false;
            CDataAsText = false;
            WhitespaceAsText = false;
        }

        public XmlDiffOption Option
        {
            set
            {
                IgnoreAttributeOrder = (((int)value & (int)(XmlDiffOption.IgnoreAttributeOrder)) > 0);
                IgnoreChildOrder = (((int)value & (int)(XmlDiffOption.IgnoreChildOrder)) > 0);
                IgnoreComments = (((int)value & (int)(XmlDiffOption.IgnoreComments)) > 0);
                IgnoreWhitespace = (((int)value & (int)(XmlDiffOption.IgnoreWhitespace)) > 0);
                IgnoreDTD = (((int)value & (int)(XmlDiffOption.IgnoreDTD)) > 0);
                IgnoreNS = (((int)value & (int)(XmlDiffOption.IgnoreNS)) > 0);
                IgnorePrefix = (((int)value & (int)(XmlDiffOption.IgnorePrefix)) > 0);
                CDataAsText = (((int)value & (int)(XmlDiffOption.CDataAsText)) > 0);
                NormalizeNewline = (((int)value & (int)(XmlDiffOption.NormalizeNewline)) > 0);
                TreatWhitespaceTextAsWSNode = (((int)value & (int)(XmlDiffOption.TreatWhitespaceTextAsWSNode)) > 0);
                IgnoreEmptyTextNodes = (((int)value & (int)(XmlDiffOption.IgnoreEmptyTextNodes)) > 0);
                WhitespaceAsText = (((int)value & (int)(XmlDiffOption.WhitespaceAsText)) > 0);
            }
        }
        public override XmlDiffNodeType NodeType => XmlDiffNodeType.Document;

        public bool IgnoreAttributeOrder { get; set; }

        public bool IgnoreChildOrder { get; set; }

        public bool IgnoreComments { get; set; }

        public bool IgnoreWhitespace { get; set; }

        public bool IgnoreDTD { get; set; }

        public bool IgnoreNS { get; set; }

        public bool IgnorePrefix { get; set; }

        public bool CDataAsText { get; set; }

        public bool NormalizeNewline { get; set; }

        public bool TreatWhitespaceTextAsWSNode { get; set; } = false;
        public bool IgnoreEmptyTextNodes { get; set; } = false;

        public bool WhitespaceAsText { get; set; } = false;

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
                //            NodePosition result = 0;
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
        private int CompareText(string s1, string s2) => Math.Sign(string.Compare(s1, s2, StringComparison.Ordinal));

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
            nameTable = reader.NameTable;
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
                            if (WhitespaceAsText)
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
            if (!TreatWhitespaceTextAsWSNode)
            {
                return false;
            }

            for (int i = 0; i < p.Length; i++)
            {
                if (!char.IsWhiteSpace(p[i]))
                {
                    return false;
                }
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
                //            rtn = reader.Read();
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
            if (!IgnoreEmptyTextNodes || !string.IsNullOrEmpty(text))
            {
                XmlDiffCharacterData textNode = new XmlDiffCharacterData(text, nt, NormalizeNewline)
                {
                    LineNumber = pInfo.LineNumber,
                    LinePosition = pInfo.LinePosition
                };
                InsertChild(parent, textNode);
            }
        }

        private void LoadTopLevelAttribute(XmlDiffNode parent, string text, PositionInfo pInfo, XmlDiffNodeType nt)
        {
            XmlDiffCharacterData textNode = new XmlDiffCharacterData(text, nt, NormalizeNewline)
            {
                LineNumber = pInfo.LineNumber,
                LinePosition = pInfo.LinePosition
            };
            InsertTopLevelAttributeAsText(parent, textNode);
        }

        private void LoadPI(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo)
        {
            XmlDiffProcessingInstruction pi = new XmlDiffProcessingInstruction(reader.Name, reader.Value)
            {
                LineNumber = pInfo.LineNumber,
                LinePosition = pInfo.LinePosition
            };
            InsertChild(parent, pi);
        }

        private void LoadEntityReference(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo)
        {
            XmlDiffEntityReference er = new XmlDiffEntityReference(reader.Name)
            {
                LineNumber = pInfo.LineNumber,
                LinePosition = pInfo.LinePosition
            };
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
        public XPathNavigator CreateNavigator() => new XmlDiffNavigator(this);

        public void SortChildren(XPathExpression expr)
        {
            if (expr == null)
            {
                return;
            }
            XPathNavigator _nav = CreateNavigator();
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
            XPathNavigator _nav = CreateNavigator();
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
            XPathNavigator _nav = CreateNavigator();
            XPathNodeIterator _iter = _nav.Select(expr);
            while (_iter.MoveNext())
            {
                ((XmlDiffNavigator)_iter.Current).CurrentNode.IgnoreValue = true;
            }
        }

        private void SortChildren(XmlDiffElement elem)
        {
            if (elem.FirstChild != null)
            {
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

        public XmlDiffNavigator(XmlDiffDocument doc)
        {
            _document = doc;
            CurrentNode = _document;
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
            if (!(nav is XmlDiffNavigator))
            {
                return XmlNodeOrder.Unknown;
            }
            if (targetNode == CurrentNode)
            {
                return XmlNodeOrder.Same;
            }
            else
            {
                if (CurrentNode.ParentNode == null) //this is root
                {
                    return XmlNodeOrder.After;
                }
                else if (targetNode.ParentNode == null) //this is root
                {
                    return XmlNodeOrder.Before;
                }
                else //look in the following nodes
                {
                    if (targetNode.LineNumber + targetNode.LinePosition > CurrentNode.LinePosition + CurrentNode.LineNumber)
                    {
                        return XmlNodeOrder.After;
                    }
                    return XmlNodeOrder.Before;
                }
            }
        }
        public override string GetAttribute(string localName, string namespaceURI)
        {
            if (CurrentNode is XmlDiffElement)
            {
                return ((XmlDiffElement)CurrentNode).GetAttributeValue(localName, namespaceURI);
            }
            return "";
        }

        public override string GetNamespace(string name)
        {
            Debug.Assert(false, "GetNamespace is NYI");
            return "";
        }

        public override bool IsSamePosition(XPathNavigator other)
        {
            if (other is XmlDiffNavigator)
            {
                if (CurrentNode == ((XmlDiffNavigator)other).CurrentNode)
                    return true;
            }
            return false;
        }

        public override bool MoveTo(XPathNavigator other)
        {
            if (other is XmlDiffNavigator)
            {
                CurrentNode = ((XmlDiffNavigator)other).CurrentNode;
                return true;
            }
            return false;
        }

        public override bool MoveToAttribute(string localName, string namespaceURI)
        {
            if (CurrentNode is XmlDiffElement)
            {
                XmlDiffAttribute _attr = ((XmlDiffElement)CurrentNode).GetAttribute(localName, namespaceURI);
                if (_attr != null)
                {
                    CurrentNode = _attr;
                    return true;
                }
            }
            return false;
        }

        public override bool MoveToFirst()
        {
            if (!(CurrentNode is XmlDiffAttribute))
            {
                if (CurrentNode.ParentNode.FirstChild == CurrentNode)
                {
                    if (CurrentNode.ParentNode.FirstChild._next != null)
                    {
                        CurrentNode = CurrentNode.ParentNode.FirstChild._next;
                        return true;
                    }
                }
                else
                {
                    CurrentNode = CurrentNode.ParentNode.FirstChild;
                    return true;
                }
            }
            return false;
        }

        public override bool MoveToFirstAttribute()
        {
            if (CurrentNode is XmlDiffElement)
            {
                if (((XmlDiffElement)CurrentNode).FirstAttribute != null)
                {
                    XmlDiffAttribute _attr = ((XmlDiffElement)CurrentNode).FirstAttribute;
                    while (_attr != null && IsNamespaceNode(_attr))
                    {
                        _attr = (XmlDiffAttribute)_attr._next;
                    }
                    if (_attr != null)
                    {
                        CurrentNode = _attr;
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool MoveToFirstChild()
        {
            if ((CurrentNode is XmlDiffDocument || CurrentNode is XmlDiffElement) && CurrentNode.FirstChild != null)
            {
                CurrentNode = CurrentNode.FirstChild;
                return true;
            }
            return false;
        }

        public new bool MoveToFirstNamespace()
        {
            if (CurrentNode is XmlDiffElement)
            {
                if (((XmlDiffElement)CurrentNode).FirstAttribute != null)
                {
                    XmlDiffAttribute _attr = ((XmlDiffElement)CurrentNode).FirstAttribute;
                    while (_attr != null && !IsNamespaceNode(_attr))
                    {
                        _attr = (XmlDiffAttribute)_attr._next;
                    }
                    if (_attr != null)
                    {
                        CurrentNode = _attr;
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
        {
            return MoveToFirstNamespace();
        }

        public override bool MoveToId(string id)
        {
            Debug.Assert(false, "MoveToId is NYI");
            return false;
        }

        public override bool MoveToNamespace(string name)
        {
            Debug.Assert(false, "MoveToNamespace is NYI");
            return false;
        }

        public override bool MoveToNext()
        {
            if (!(CurrentNode is XmlDiffAttribute) && CurrentNode._next != null)
            {
                CurrentNode = CurrentNode._next;
                return true;
            }
            return false;
        }

        public override bool MoveToNextAttribute()
        {
            if (CurrentNode is XmlDiffAttribute)
            {
                XmlDiffAttribute _attr = (XmlDiffAttribute)CurrentNode._next;
                while (_attr != null && IsNamespaceNode(_attr))
                {
                    _attr = (XmlDiffAttribute)_attr._next;
                }
                if (_attr != null)
                {
                    CurrentNode = _attr;
                    return true;
                }
            }
            return false;
        }

        public new bool MoveToNextNamespace()
        {
            if (CurrentNode is XmlDiffAttribute)
            {
                XmlDiffAttribute _attr = (XmlDiffAttribute)CurrentNode._next;
                while (_attr != null && !IsNamespaceNode(_attr))
                {
                    _attr = (XmlDiffAttribute)_attr._next;
                }
                if (_attr != null)
                {
                    CurrentNode = _attr;
                    return true;
                }
            }
            return false;
        }

        private bool IsNamespaceNode(XmlDiffAttribute attr)
        {
            return attr.LocalName.ToLower(CultureInfo.InvariantCulture) == "xmlns" ||
                   attr.Prefix.ToLower(CultureInfo.InvariantCulture) == "xmlns";
        }

        public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
        {
            return MoveToNextNamespace();
        }

        public override bool MoveToParent()
        {
            if (!(CurrentNode is XmlDiffDocument))
            {
                CurrentNode = CurrentNode.ParentNode;
                return true;
            }
            return false;
        }

        public override bool MoveToPrevious()
        {
            if (CurrentNode != CurrentNode.ParentNode.FirstChild)
            {
                XmlDiffNode _current = CurrentNode.ParentNode.FirstChild;
                XmlDiffNode _prev = CurrentNode.ParentNode.FirstChild;
                while (_current != CurrentNode)
                {
                    _prev = _current;
                    _current = _current._next;
                }
                CurrentNode = _prev;
                return true;
            }
            return false;
        }

        public override void MoveToRoot() => CurrentNode = _document;

        public override XPathNodeType NodeType
        {
            get
            {
                //namespace, comment and whitespace node types are not supported
                switch (CurrentNode.NodeType)
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
                if (CurrentNode.NodeType == XmlDiffNodeType.Element)
                {
                    return ((XmlDiffElement)CurrentNode).LocalName;
                }
                else if (CurrentNode.NodeType == XmlDiffNodeType.Attribute)
                {
                    return ((XmlDiffAttribute)CurrentNode).LocalName;
                }
                else if (CurrentNode.NodeType == XmlDiffNodeType.PI)
                {
                    return ((XmlDiffProcessingInstruction)CurrentNode).Name;
                }
                return "";
            }
        }

        public override string Name
        {
            get
            {
                if (CurrentNode.NodeType == XmlDiffNodeType.Element)
                {
                    return _document.nameTable.Get(((XmlDiffElement)CurrentNode).Name);
                }
                else if (CurrentNode.NodeType == XmlDiffNodeType.Attribute)
                {
                    return ((XmlDiffAttribute)CurrentNode).Name;
                }
                else if (CurrentNode.NodeType == XmlDiffNodeType.PI)
                {
                    return ((XmlDiffProcessingInstruction)CurrentNode).Name;
                }
                return "";
            }
        }

        public override string NamespaceURI
        {
            get
            {
                if (CurrentNode is XmlDiffElement)
                {
                    return ((XmlDiffElement)CurrentNode).NamespaceURI;
                }
                else if (CurrentNode is XmlDiffAttribute)
                {
                    return ((XmlDiffAttribute)CurrentNode).NamespaceURI;
                }
                return "";
            }
        }

        public override string Value
        {
            get
            {
                if (CurrentNode is XmlDiffAttribute)
                {
                    return ((XmlDiffAttribute)CurrentNode).Value;
                }
                else if (CurrentNode is XmlDiffCharacterData)
                {
                    return ((XmlDiffCharacterData)CurrentNode).Value;
                }
                else if (CurrentNode is XmlDiffElement)
                {
                    return ((XmlDiffElement)CurrentNode).Value;
                }
                return "";
            }
        }

        public override string Prefix
        {
            get
            {
                if (CurrentNode is XmlDiffElement)
                {
                    return ((XmlDiffElement)CurrentNode).Prefix;
                }
                else if (CurrentNode is XmlDiffAttribute)
                {
                    return ((XmlDiffAttribute)CurrentNode).Prefix;
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
            get => (CurrentNode is XmlDiffElement && ((XmlDiffElement)CurrentNode).FirstAttribute != null) ? true : false;
        }

        public override bool HasChildren => CurrentNode._next != null ? true : false;

        public override bool IsEmptyElement => CurrentNode is XmlDiffEmptyElement ? true : false;

        public override XmlNameTable NameTable => _document.nameTable;

        public XmlDiffNode CurrentNode { get; private set; }

        public bool IsOnRoot() => CurrentNode == null ? true : false;
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
            _next = null;
            _firstChild = null;
            _lastChild = null;
            _parent = null;
            _lineNumber = 0;
            _linePosition = 0;
        }

        public XmlDiffNode FirstChild => _firstChild;

        public XmlDiffNode LastChild => _lastChild;

        public XmlDiffNode NextSibling => _next;

        public XmlDiffNode ParentNode => _parent;

        public virtual bool IgnoreValue
        {
            get => _bIgnoreValue;
            set
            {
                _bIgnoreValue = value;
                XmlDiffNode current = _firstChild;
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
            get => _extendedProperties ?? (_extendedProperties = new PropertyCollection());
        }

        public virtual void InsertChildAfter(XmlDiffNode child, XmlDiffNode newChild)
        {
            Debug.Assert(newChild != null);
            newChild._parent = this;
            if (child == null)
            {
                newChild._next = _firstChild;
                _firstChild = newChild;
            }
            else
            {
                Debug.Assert(child._parent == this);
                newChild._next = child._next;
                child._next = newChild;
            }
            if (newChild._next == null)
                _lastChild = newChild;
        }

        public virtual void DeleteChild(XmlDiffNode child)
        {
            if (child == FirstChild)//delete head
            {
                _firstChild = FirstChild.NextSibling;
            }
            else
            {
                XmlDiffNode current = FirstChild;
                XmlDiffNode previous = null;
                while (current != child)
                {
                    previous = current;
                    current = current.NextSibling;
                }
                Debug.Assert(current != null);
                if (current == LastChild) //tail being deleted
                {
                    _lastChild = current.NextSibling;
                }
                previous._next = current.NextSibling;
            }
        }

        public int LineNumber
        {
            get => _lineNumber;
            set => _lineNumber = value;
        }

        public int LinePosition
        {
            get => _linePosition;
            set => _linePosition = value;
        }
    }

    public class XmlDiffElement : XmlDiffNode
    {
        private int _attrC;

        public XmlDiffElement(string localName, string prefix, string ns)
            : base()
        {
            LocalName = localName;
            Prefix = prefix;
            NamespaceURI = ns;
            FirstAttribute = null;
            LastAttribute = null;
            _attrC = -1;
        }

        public override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Element; } }
        public string LocalName { get; }
        public string NamespaceURI { get; }
        public string Prefix { get; }

        public string Name
        {
            get
            {
                if (Prefix.Length > 0)
                    return Prefix + ":" + LocalName;
                else
                    return LocalName;
            }
        }

        public XmlDiffAttribute FirstAttribute { get; private set; }

        public XmlDiffAttribute LastAttribute { get; private set; }

        public string GetAttributeValue(string LocalName, string NamespaceUri)
        {
            if (FirstAttribute != null)
            {
                XmlDiffAttribute _current = FirstAttribute;
                do
                {
                    if (_current.LocalName == LocalName && _current.NamespaceURI == NamespaceURI)
                    {
                        return _current.Value;
                    }
                    _current = (XmlDiffAttribute)_current._next;
                }
                while (_current != LastAttribute);
            }
            return "";
        }

        public XmlDiffAttribute GetAttribute(string LocalName, string NamespaceUri)
        {
            if (FirstAttribute != null)
            {
                XmlDiffAttribute _current = FirstAttribute;
                do
                {
                    if (_current.LocalName == LocalName && _current.NamespaceURI == NamespaceURI)
                    {
                        return _current;
                    }
                    _current = (XmlDiffAttribute)_current._next;
                }
                while (_current != LastAttribute);
            }
            return null;
        }

        internal void InsertAttributeAfter(XmlDiffAttribute attr, XmlDiffAttribute newAttr)
        {
            Debug.Assert(newAttr != null);
            newAttr._ownerElement = this;
            if (attr == null)
            {
                newAttr._next = FirstAttribute;
                FirstAttribute = newAttr;
            }
            else
            {
                Debug.Assert(attr._ownerElement == this);
                newAttr._next = attr._next;
                attr._next = newAttr;
            }
            if (newAttr._next == null)
                LastAttribute = newAttr;
        }

        internal void DeleteAttribute(XmlDiffAttribute attr)
        {
            if (attr == FirstAttribute)//delete head
            {
                if (attr == LastAttribute) //tail being deleted
                {
                    LastAttribute = (XmlDiffAttribute)attr.NextSibling;
                }
                FirstAttribute = (XmlDiffAttribute)FirstAttribute.NextSibling;
            }
            else
            {
                XmlDiffAttribute current = FirstAttribute;
                XmlDiffAttribute previous = null;
                while (current != attr)
                {
                    previous = current;
                    current = (XmlDiffAttribute)current.NextSibling;
                }
                Debug.Assert(current != null);
                if (current == LastAttribute) //tail being deleted
                {
                    LastAttribute = (XmlDiffAttribute)current.NextSibling;
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
                XmlDiffAttribute attr = FirstAttribute;
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
                XmlDiffAttribute current = FirstAttribute;
                while (current != null)
                {
                    current.IgnoreValue = value;
                    current = (XmlDiffAttribute)current._next;
                }
            }
        }

        public int EndLineNumber { get; set; }

        public int EndLinePosition { get; set; }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteStartElement(Prefix, LocalName, NamespaceURI);
            XmlDiffAttribute attr = FirstAttribute;
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
                if (IgnoreValue)
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
        private readonly string _value;

        public XmlDiffAttribute(string localName, string prefix, string ns, string value) : base()
        {
            LocalName = localName;
            Prefix = prefix;
            NamespaceURI = ns;
            _value = value;
        }

        public string Value => IgnoreValue ? "" : _value;

        public XmlQualifiedName ValueAsQName { get; private set; }

        internal void SetValueAsQName(XmlReader reader, string value)
        {
            int indexOfColon = value.IndexOf(':');
            if (indexOfColon == -1)
            {
                ValueAsQName = new XmlQualifiedName(value);
            }
            else
            {
                string prefix = value.Substring(0, indexOfColon);
                string ns = reader.LookupNamespace(prefix);
                if (ns == null)
                {
                    ValueAsQName = null;
                }
                else
                {
                    try
                    {
                        string localName = XmlConvert.VerifyNCName(value.Substring(indexOfColon + 1));
                        ValueAsQName = new XmlQualifiedName(localName, ns);
                    }
                    catch (XmlException)
                    {
                        ValueAsQName = null;
                    }
                }
            }
        }

        public string LocalName { get; }
        public string NamespaceURI { get; }
        public string Prefix { get; }

        public string Name
        {
            get
            {
                if (Prefix.Length > 0)
                    return Prefix + ":" + LocalName;
                else
                    return LocalName;
            }
        }

        public override XmlDiffNodeType NodeType => XmlDiffNodeType.Attribute;

        public override void WriteTo(XmlWriter w)
        {
            w.WriteStartAttribute(Prefix, LocalName, NamespaceURI);
            WriteContentTo(w);
            w.WriteEndAttribute();
        }

        public override void WriteContentTo(XmlWriter w) => w.WriteString(Value);
    }

    public class XmlDiffEntityReference : XmlDiffNode
    {
        public XmlDiffEntityReference(string name) : base()
        {
            Name = name;
        }

        public override XmlDiffNodeType NodeType => XmlDiffNodeType.ER;

        public string Name { get; }

        public override void WriteTo(XmlWriter w) => w.WriteEntityRef(Name);

        public override void WriteContentTo(XmlWriter w)
        {
            XmlDiffNode child = FirstChild;
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

        public XmlDiffCharacterData(string value, XmlDiffNodeType nt, bool NormalizeNewline) : base()
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
            get => IgnoreValue ? "" : _value;
            set => _value = value;
        }

        public override XmlDiffNodeType NodeType => _nodetype;

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
                    Debug.Assert(false, $"Wrong type for text-like node : {_nodetype}");
                    break;
            }
        }

        public override void WriteContentTo(XmlWriter w) { }
    }

    public class XmlDiffProcessingInstruction : XmlDiffCharacterData
    {
        public XmlDiffProcessingInstruction(string name, string value) : base(value, XmlDiffNodeType.PI, false)
        {
            Name = name;
        }

        public string Name { get; }

        public override void WriteTo(XmlWriter w) => w.WriteProcessingInstruction(Name, Value);

        public override void WriteContentTo(XmlWriter w) { }
    }
}
