// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable 0618 // ignore obsolete warning about XmlDataDocument

namespace System.Xml
{
    internal sealed class DataPointer : IXmlDataVirtualNode
    {
        private XmlDataDocument _doc;
        private XmlNode _node;
        private DataColumn? _column;
        private bool _fOnValue;
        private bool _bNeedFoliate;
        private bool _isInUse;

        internal DataPointer(XmlDataDocument doc, XmlNode node)
        {
            _doc = doc;
            _node = node;
            _column = null;
            _fOnValue = false;
            _bNeedFoliate = false;
            _isInUse = true;
            AssertValid();
        }

        internal DataPointer(DataPointer pointer)
        {
            _doc = pointer._doc;
            _node = pointer._node;
            _column = pointer._column;
            _fOnValue = pointer._fOnValue;
            _bNeedFoliate = false;
            _isInUse = true;
            AssertValid();
        }

        internal void AddPointer() => _doc.AddPointer(this);

        // Returns the row element of the region that the pointer points into
        private XmlBoundElement? GetRowElement()
        {
            XmlBoundElement? rowElem;
            if (_column != null)
            {
                rowElem = _node as XmlBoundElement;
                Debug.Assert(rowElem != null);
                Debug.Assert(rowElem.Row != null);
                return rowElem;
            }

            _doc.Mapper.GetRegion(_node, out rowElem);
            return rowElem;
        }

        private DataRow? Row
        {
            get
            {
                XmlBoundElement? rowElem = GetRowElement();
                if (rowElem == null)
                {
                    return null;
                }

                Debug.Assert(rowElem.Row != null);
                return rowElem.Row;
            }
        }

        private static bool IsFoliated(XmlNode node) =>
            node != null && node is XmlBoundElement ?
                ((XmlBoundElement)node).IsFoliated : true;

        internal void MoveTo(DataPointer pointer)
        {
            AssertValid();

            // You should not move outside of this document
            Debug.Assert(_node == _doc || _node.OwnerDocument == _doc);

            _doc = pointer._doc;
            _node = pointer._node;
            _column = pointer._column;
            _fOnValue = pointer._fOnValue;
            AssertValid();
        }
        private void MoveTo(XmlNode node)
        {
            // You should not move outside of this document
            Debug.Assert(node == _doc || node.OwnerDocument == _doc);

            _node = node;
            _column = null;
            _fOnValue = false;
            AssertValid();
        }

        private void MoveTo(XmlNode node, DataColumn? column, bool fOnValue)
        {
            // You should not move outside of this document
            Debug.Assert(node == _doc || node.OwnerDocument == _doc);

            _node = node;
            _column = column;
            _fOnValue = fOnValue;
            AssertValid();
        }

        private DataColumn? NextColumn(DataRow row, DataColumn? col, bool fAttribute, bool fNulls)
        {
            if (row.RowState == DataRowState.Deleted)
            {
                return null;
            }

            DataTable table = row.Table;
            DataColumnCollection columns = table.Columns;
            int iColumn = (col != null) ? col.Ordinal + 1 : 0;
            int cColumns = columns.Count;
            DataRowVersion rowVersion = (row.RowState == DataRowState.Detached) ? DataRowVersion.Proposed : DataRowVersion.Current;

            for (; iColumn < cColumns; iColumn++)
            {
                DataColumn c = columns[iColumn];
                if (!_doc.IsNotMapped(c) && (c.ColumnMapping == MappingType.Attribute) == fAttribute && (fNulls || !Convert.IsDBNull(row[c, rowVersion])))
                {
                    return c;
                }
            }

            return null;
        }

        private DataColumn? NthColumn(DataRow row, bool fAttribute, int iColumn, bool fNulls)
        {
            DataColumn? c = null;
            while ((c = NextColumn(row, c, fAttribute, fNulls)) != null)
            {
                if (iColumn == 0)
                {
                    return c;
                }

                iColumn = checked((iColumn - 1));
            }
            return null;
        }

        private int ColumnCount(DataRow row, bool fAttribute, bool fNulls)
        {
            DataColumn? c = null;
            int count = 0;
            while ((c = NextColumn(row, c, fAttribute, fNulls)) != null)
            {
                count++;
            }
            return count;
        }

        internal bool MoveToFirstChild()
        {
            RealFoliate();
            AssertValid();
            if (_node == null)
            {
                return false;
            }

            if (_column != null)
            {
                if (_fOnValue)
                {
                    return false;
                }

                _fOnValue = true;
                return true;
            }
            else if (!IsFoliated(_node))
            {
                // find virtual column elements first
                DataColumn? c = NextColumn(Row!, null, false, false);
                if (c != null)
                {
                    MoveTo(_node, c, _doc.IsTextOnly(c));
                    return true;
                }
            }

            // look for anything
            XmlNode? n = _doc.SafeFirstChild(_node);
            if (n != null)
            {
                MoveTo(n);
                return true;
            }

            return false;
        }

        internal bool MoveToNextSibling()
        {
            RealFoliate();
            AssertValid();
            if (_node != null)
            {
                if (_column != null)
                {
                    if (_fOnValue && !_doc.IsTextOnly(_column))
                    {
                        return false;
                    }

                    DataColumn? c = NextColumn(Row!, _column, false, false);
                    if (c != null)
                    {
                        MoveTo(_node, c, false);
                        return true;
                    }

                    XmlNode? n = _doc.SafeFirstChild(_node);
                    if (n != null)
                    {
                        MoveTo(n);
                        return true;
                    }
                }
                else
                {
                    XmlNode? n = _doc.SafeNextSibling(_node);
                    if (n != null)
                    {
                        MoveTo(n);
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool MoveToParent()
        {
            RealFoliate();
            AssertValid();
            if (_node != null)
            {
                if (_column != null)
                {
                    if (_fOnValue && !_doc.IsTextOnly(_column))
                    {
                        MoveTo(_node, _column, false);
                        return true;
                    }

                    if (_column.ColumnMapping != MappingType.Attribute)
                    {
                        MoveTo(_node, null, false);
                        return true;
                    }
                }
                else
                {
                    XmlNode? n = _node.ParentNode;
                    if (n != null)
                    {
                        MoveTo(n);
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool MoveToOwnerElement()
        {
            RealFoliate();
            AssertValid();
            if (_node != null)
            {
                if (_column != null)
                {
                    if (_fOnValue || _doc.IsTextOnly(_column) || _column.ColumnMapping != MappingType.Attribute)
                    {
                        return false;
                    }

                    MoveTo(_node, null, false);
                    return true;
                }
                else if (_node.NodeType == XmlNodeType.Attribute)
                {
                    XmlNode? n = ((XmlAttribute)_node).OwnerElement;
                    if (n != null)
                    {
                        MoveTo(n, null, false);
                        return true;
                    }
                }
            }

            return false;
        }


        internal int AttributeCount
        {
            get
            {
                RealFoliate();
                AssertValid();
                if (_node != null)
                {
                    if (_column == null && _node.NodeType == XmlNodeType.Element)
                    {
                        if (!IsFoliated(_node))
                        {
                            return ColumnCount(Row!, true, false);
                        }
                        else
                        {
                            return _node.Attributes!.Count;
                        }
                    }
                }
                return 0;
            }
        }

        internal bool MoveToAttribute(int i)
        {
            RealFoliate();
            AssertValid();
            if (i < 0)
            {
                return false;
            }

            if (_node != null)
            {
                if ((_column == null || _column.ColumnMapping == MappingType.Attribute) && _node.NodeType == XmlNodeType.Element)
                {
                    if (!IsFoliated(_node))
                    {
                        DataColumn? c = NthColumn(Row!, true, i, false);
                        if (c != null)
                        {
                            MoveTo(_node, c, false);
                            return true;
                        }
                    }
                    else
                    {
                        XmlNode? n = _node.Attributes!.Item(i);
                        if (n != null)
                        {
                            MoveTo(n, null, false);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        internal XmlNodeType NodeType
        {
            get
            {
                RealFoliate();
                AssertValid();
                if (_node == null)
                {
                    return XmlNodeType.None;
                }
                else if (_column == null)
                {
                    return _node.NodeType;
                }
                else if (_fOnValue)
                {
                    return XmlNodeType.Text;
                }
                else if (_column.ColumnMapping == MappingType.Attribute)
                {
                    return XmlNodeType.Attribute;
                }
                else
                {
                    return XmlNodeType.Element;
                }
            }
        }

        internal string LocalName
        {
            get
            {
                RealFoliate();
                AssertValid();
                if (_node == null)
                {
                    return string.Empty;
                }
                else if (_column == null)
                {
                    string name = _node.LocalName;
                    Debug.Assert(name != null);
                    if (IsLocalNameEmpty(_node.NodeType))
                    {
                        return string.Empty;
                    }

                    return name;
                }
                else if (_fOnValue)
                {
                    return string.Empty;
                }
                else
                {
                    return _doc.NameTable.Add(_column.EncodedColumnName);
                }
            }
        }

        internal string NamespaceURI
        {
            get
            {
                RealFoliate();
                AssertValid();
                if (_node == null)
                {
                    return string.Empty;
                }
                else if (_column == null)
                {
                    return _node.NamespaceURI;
                }
                else if (_fOnValue)
                {
                    return string.Empty;
                }
                else
                {
                    return _doc.NameTable.Add(_column.Namespace);
                }
            }
        }

        internal string Name
        {
            get
            {
                RealFoliate();
                AssertValid();
                if (_node == null)
                {
                    return string.Empty;
                }
                else if (_column == null)
                {
                    string name = _node.Name;
                    //Again it could be String.Empty at null position
                    Debug.Assert(name != null);
                    if (IsLocalNameEmpty(_node.NodeType))
                    {
                        return string.Empty;
                    }
                    return name;
                }
                else
                {
                    string prefix = Prefix;
                    string lname = LocalName;
                    if (prefix != null && prefix.Length > 0)
                    {
                        if (lname != null && lname.Length > 0)
                        {
                            return _doc.NameTable.Add(prefix + ":" + lname);
                        }
                        else
                        {
                            return prefix;
                        }
                    }
                    else
                    {
                        return lname;
                    }
                }
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

        internal string Prefix
        {
            get
            {
                RealFoliate();
                AssertValid();
                if (_node == null)
                {
                    return string.Empty;
                }
                else if (_column == null)
                {
                    return _node.Prefix;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        internal string? Value
        {
            [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
            get
            {
                RealFoliate();
                AssertValid();
                if (_node == null)
                {
                    return null;
                }
                else if (_column == null)
                {
                    return _node.Value;
                }
                else if (_column.ColumnMapping == MappingType.Attribute || _fOnValue)
                {
                    DataRow row = Row!;
                    DataRowVersion rowVersion = (row.RowState == DataRowState.Detached) ? DataRowVersion.Proposed : DataRowVersion.Current;
                    object value = row[_column, rowVersion];
                    if (!Convert.IsDBNull(value))
                    {
                        return _column.ConvertObjectToXml(value);
                    }
                    return null;
                }
                else
                {
                    // column element has no value
                    return null;
                }
            }
        }

        bool IXmlDataVirtualNode.IsOnNode(XmlNode nodeToCheck)
        {
            RealFoliate();
            return nodeToCheck == _node;
        }

        bool IXmlDataVirtualNode.IsOnColumn(DataColumn? col)
        {
            RealFoliate();
            return col == _column;
        }

        internal XmlNode GetNode() => _node;

        internal bool IsEmptyElement
        {
            get
            {
                RealFoliate();
                AssertValid();
                if (_node != null && _column == null)
                {
                    if (_node.NodeType == XmlNodeType.Element)
                    {
                        return ((XmlElement)_node).IsEmpty;
                    }
                }
                return false;
            }
        }

        internal bool IsDefault
        {
            get
            {
                RealFoliate();
                AssertValid();
                if (_node != null && _column == null && _node.NodeType == XmlNodeType.Attribute)
                {
                    return !((XmlAttribute)_node).Specified;
                }

                return false;
            }
        }

        void IXmlDataVirtualNode.OnFoliated(XmlNode foliatedNode)
        {
            // update the pointer if the element node has been foliated
            if (_node == foliatedNode)
            {
                // if already on this node, nothing to do!
                if (_column == null)
                {
                    return;
                }
                _bNeedFoliate = true;
            }
        }

        internal void RealFoliate()
        {
            if (!_bNeedFoliate)
            {
                return;
            }

            XmlNode? n = null;

            if (_doc.IsTextOnly(_column!))
            {
                n = _node.FirstChild;
            }
            else
            {
                if (_column!.ColumnMapping == MappingType.Attribute)
                {
                    n = _node.Attributes!.GetNamedItem(_column.EncodedColumnName, _column.Namespace);
                }
                else
                {
                    for (n = _node.FirstChild; n != null; n = n.NextSibling)
                    {
                        if (n.LocalName == _column.EncodedColumnName && n.NamespaceURI == _column.Namespace)
                            break;
                    }
                }

                if (n != null && _fOnValue)
                {
                    n = n.FirstChild;
                }
            }

            if (n == null)
            {
                throw new InvalidOperationException(SR.DataDom_Foliation);
            }

            // Cannot use MoveTo( n ); b/c the initial state for MoveTo is invalid (region is foliated but this is not)
            _node = n;
            _column = null;
            _fOnValue = false;
            AssertValid();

            _bNeedFoliate = false;
        }

        //for the 6 properties below, only when the this.column == null that the nodetype could be XmlDeclaration node
        internal string? PublicId
        {
            get
            {
                XmlNodeType nt = NodeType;
                switch (nt)
                {
                    case XmlNodeType.DocumentType:
                        {
                            Debug.Assert(_column == null);
                            return ((XmlDocumentType)(_node)).PublicId;
                        }
                    case XmlNodeType.Entity:
                        {
                            Debug.Assert(_column == null);
                            return ((XmlEntity)(_node)).PublicId;
                        }
                    case XmlNodeType.Notation:
                        {
                            Debug.Assert(_column == null);
                            return ((XmlNotation)(_node)).PublicId;
                        }
                }
                return null;
            }
        }

        internal string? SystemId
        {
            get
            {
                XmlNodeType nt = NodeType;
                switch (nt)
                {
                    case XmlNodeType.DocumentType:
                        {
                            Debug.Assert(_column == null);
                            return ((XmlDocumentType)(_node)).SystemId;
                        }
                    case XmlNodeType.Entity:
                        {
                            Debug.Assert(_column == null);
                            return ((XmlEntity)(_node)).SystemId;
                        }
                    case XmlNodeType.Notation:
                        {
                            Debug.Assert(_column == null);
                            return ((XmlNotation)(_node)).SystemId;
                        }
                }
                return null;
            }
        }

        internal string? InternalSubset
        {
            get
            {
                if (NodeType == XmlNodeType.DocumentType)
                {
                    Debug.Assert(_column == null);
                    return ((XmlDocumentType)(_node)).InternalSubset;
                }
                return null;
            }
        }

        internal XmlDeclaration? Declaration
        {
            get
            {
                XmlNode? child = _doc.SafeFirstChild(_doc);
                if (child != null && child.NodeType == XmlNodeType.XmlDeclaration)
                    return (XmlDeclaration)child;
                return null;
            }
        }

        internal string? Encoding
        {
            get
            {
                if (NodeType == XmlNodeType.XmlDeclaration)
                {
                    Debug.Assert(_column == null);
                    return ((XmlDeclaration)(_node)).Encoding;
                }
                else if (NodeType == XmlNodeType.Document)
                {
                    XmlDeclaration? dec = Declaration;
                    if (dec != null)
                    {
                        return dec.Encoding;
                    }
                }
                return null;
            }
        }

        internal string? Standalone
        {
            get
            {
                if (NodeType == XmlNodeType.XmlDeclaration)
                {
                    Debug.Assert(_column == null);
                    return ((XmlDeclaration)(_node)).Standalone;
                }
                else if (NodeType == XmlNodeType.Document)
                {
                    XmlDeclaration? dec = Declaration;
                    if (dec != null)
                    {
                        return dec.Standalone;
                    }
                }
                return null;
            }
        }

        internal string? Version
        {
            get
            {
                if (NodeType == XmlNodeType.XmlDeclaration)
                {
                    Debug.Assert(_column == null);
                    return ((XmlDeclaration)(_node)).Version;
                }
                else if (NodeType == XmlNodeType.Document)
                {
                    XmlDeclaration? dec = Declaration;
                    if (dec != null)
                        return dec.Version;
                }
                return null;
            }
        }

        [Conditional("DEBUG")]
        private void AssertValid()
        {
            // This pointer must be int the document list
            if (_column != null)
            {
                // We must be on a de-foliated region
                XmlBoundElement? rowElem = _node as XmlBoundElement;
                Debug.Assert(rowElem != null);

                DataRow? row = rowElem.Row;
                Debug.Assert(row != null);

                ElementState state = rowElem.ElementState;
                Debug.Assert(state == ElementState.Defoliated, "Region is accessed using column, but it's state is FOLIATED");

                // We cannot be on a column for which the value is DBNull
                DataRowVersion rowVersion = (row.RowState == DataRowState.Detached) ? DataRowVersion.Proposed : DataRowVersion.Current;
                Debug.Assert(!Convert.IsDBNull(row[_column, rowVersion]));

                // If we are on the Text column, we should always have fOnValue == true
                Debug.Assert((_column.ColumnMapping == MappingType.SimpleContent) ? (_fOnValue == true) : true);
            }
        }

        bool IXmlDataVirtualNode.IsInUse() => _isInUse;

        internal void SetNoLongerUse()
        {
            _node = null!;
            _column = null;
            _fOnValue = false;
            _bNeedFoliate = false;
            _isInUse = false;
        }
    }
}
