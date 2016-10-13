// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.XPath;

#pragma warning disable 0618 // ignore obsolete warning about XmlDataDocument

namespace System.Xml
{
    internal sealed class DataDocumentXPathNavigator : XPathNavigator, IHasXmlNode
    {
        private readonly XPathNodePointer _curNode; //pointer to remember the current node position
        private XmlDataDocument _doc;     //pointer to remember the root -- can only be XmlDataDocument for DataDocumentXPathNavigator
        private readonly XPathNodePointer _temp;

        internal DataDocumentXPathNavigator(XmlDataDocument doc, XmlNode node)
        {
            _curNode = new XPathNodePointer(this, doc, node);
            _temp = new XPathNodePointer(this, doc, node);
            _doc = doc;
        }

        private DataDocumentXPathNavigator(DataDocumentXPathNavigator other)
        {
            _curNode = other._curNode.Clone(this);
            _temp = other._temp.Clone(this);
            _doc = other._doc;
        }
        public override XPathNavigator Clone() => new DataDocumentXPathNavigator(this);

        internal XPathNodePointer CurNode => _curNode;
        internal XmlDataDocument Document => _doc;

        //Convert will deal with nodeType as Attribute or Namespace nodes
        public override XPathNodeType NodeType => _curNode.NodeType;

        public override string LocalName => _curNode.LocalName;

        public override string NamespaceURI => _curNode.NamespaceURI;

        public override string Name => _curNode.Name;

        public override string Prefix => _curNode.Prefix;

        public override string Value
        {
            get
            {
                XPathNodeType xnt = _curNode.NodeType;
                return xnt == XPathNodeType.Element || xnt == XPathNodeType.Root ? _curNode.InnerText : _curNode.Value;
            }
        }

        public override string BaseURI => _curNode.BaseURI;

        public override string XmlLang => _curNode.XmlLang;

        public override bool IsEmptyElement => _curNode.IsEmptyElement;

        public override XmlNameTable NameTable => _doc.NameTable;

        // Attributes
        public override bool HasAttributes => _curNode.AttributeCount > 0;

        public override string GetAttribute(string localName, string namespaceURI)
        {
            if (_curNode.NodeType != XPathNodeType.Element)
            {
                return string.Empty; //other type of nodes can't have attributes
            }

            _temp.MoveTo(_curNode);
            return _temp.MoveToAttribute(localName, namespaceURI) ? _temp.Value : string.Empty;
        }

        public override string GetNamespace(string name) => _curNode.GetNamespace(name);

        public override bool MoveToNamespace(string name) =>
            _curNode.NodeType != XPathNodeType.Element ?
                false : _curNode.MoveToNamespace(name);

        public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope) =>
            _curNode.NodeType != XPathNodeType.Element ?
                false : _curNode.MoveToFirstNamespace(namespaceScope);

        public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope) =>
            _curNode.NodeType != XPathNodeType.Namespace ?
                false : _curNode.MoveToNextNamespace(namespaceScope);

        public override bool MoveToAttribute(string localName, string namespaceURI) =>
            _curNode.NodeType != XPathNodeType.Element ?
                false : //other type of nodes can't have attributes
                _curNode.MoveToAttribute(localName, namespaceURI);

        public override bool MoveToFirstAttribute() =>
            _curNode.NodeType != XPathNodeType.Element ?
                false : //other type of nodes can't have attributes
                _curNode.MoveToNextAttribute(true);

        public override bool MoveToNextAttribute() =>
            _curNode.NodeType != XPathNodeType.Attribute ?
                false : _curNode.MoveToNextAttribute(false);

        // Tree
        public override bool MoveToNext() =>
            _curNode.NodeType == XPathNodeType.Attribute ?
                false : _curNode.MoveToNextSibling();

        public override bool MoveToPrevious() =>
            _curNode.NodeType == XPathNodeType.Attribute ?
                false : _curNode.MoveToPreviousSibling();

        public override bool MoveToFirst() =>
            _curNode.NodeType == XPathNodeType.Attribute ?
                false : _curNode.MoveToFirst();

        public override bool HasChildren => _curNode.HasChildren;

        public override bool MoveToFirstChild() => _curNode.MoveToFirstChild();

        public override bool MoveToParent() => _curNode.MoveToParent();

        public override void MoveToRoot() => _curNode.MoveToRoot();

        public override bool MoveTo(XPathNavigator other)
        {
            if (other != null)
            {
                DataDocumentXPathNavigator otherDataDocXPathNav = other as DataDocumentXPathNavigator;
                if (otherDataDocXPathNav != null && _curNode.MoveTo(otherDataDocXPathNav.CurNode))
                {
                    _doc = _curNode.Document;
                    return true;
                }
            }
            return false;
        }

        //doesn't support MoveToId
        public override bool MoveToId(string id) => false;

        public override bool IsSamePosition(XPathNavigator other)
        {
            if (other != null)
            {
                DataDocumentXPathNavigator otherDataDocXPathNav = other as DataDocumentXPathNavigator;
                if (otherDataDocXPathNav != null &&
                    _doc == otherDataDocXPathNav.Document && _curNode.IsSamePosition(otherDataDocXPathNav.CurNode))
                {
                    return true;
                }
            }
            return false;
        }

        //the function is only called for XPathNodeList enumerate nodes and 
        // shouldn't be promoted to frequently use because it will cause foliation
        XmlNode IHasXmlNode.GetNode() => _curNode.Node;

        public override XmlNodeOrder ComparePosition(XPathNavigator other)
        {
            if (other == null)
            {
                return XmlNodeOrder.Unknown; // this is what XPathDocument does.
            }

            DataDocumentXPathNavigator otherDataDocXPathNav = other as DataDocumentXPathNavigator;

            return otherDataDocXPathNav == null || otherDataDocXPathNav.Document != _doc ?
                XmlNodeOrder.Unknown :
                _curNode.ComparePosition(otherDataDocXPathNav.CurNode);
        }
    }
}
