// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if ENABLEDATABINDING
using System;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Xml.XPath.DataBinding
{
    /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView"]/*' />
    public sealed class XPathDocumentView : IBindingList, ITypedList {
        ArrayList rows;
        Shape rowShape;
        XPathNode ndRoot;
        XPathDocument document;
        string xpath;
        IXmlNamespaceResolver namespaceResolver;
        IXmlNamespaceResolver xpathResolver;

        //
        // Constructors
        //
        
        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.XPathDocumentView"]/*' />
        public XPathDocumentView(XPathDocument document) 
            : this(document, (IXmlNamespaceResolver)null) {
        }
        
        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.XPathDocumentView1"]/*' />
        public XPathDocumentView(XPathDocument document, IXmlNamespaceResolver namespaceResolver) {
            if (null == document)
                throw new ArgumentNullException(nameof(document));
            this.document = document;
            this.ndRoot = document.Root;
            if (null == this.ndRoot)
                throw new ArgumentException(nameof(document));
            this.namespaceResolver = namespaceResolver;
            ArrayList rows = new ArrayList();
            this.rows = rows;
            Debug.Assert(XPathNodeType.Root == this.ndRoot.NodeType);
            XPathNode nd = this.ndRoot.Child;
            while (null != nd) {
                if (XPathNodeType.Element == nd.NodeType) 
                    rows.Add(nd);
                nd = nd.Sibling;
            }
            DeriveShapeFromRows();
        }
            
        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.XPathDocumentView2"]/*' />
        public XPathDocumentView(XPathDocument document, string xpath)
            : this(document, xpath, null, true) {
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.XPathDocumentView3"]/*' />
        public XPathDocumentView(XPathDocument document, string xpath, IXmlNamespaceResolver namespaceResolver)
            : this(document, xpath, namespaceResolver, false) {
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.XPathDocumentView4"]/*' />
        public XPathDocumentView(XPathDocument document, string xpath, IXmlNamespaceResolver namespaceResolver, bool showPrefixes) {
            if (null == document)
                throw new ArgumentNullException(nameof(document));
            this.xpath = xpath;
            this.document = document;
            this.ndRoot = document.Root;
            if (null == this.ndRoot)
                throw new ArgumentException(nameof(document));
            this.ndRoot = document.Root;
            this.xpathResolver = namespaceResolver;
            if (showPrefixes)
                this.namespaceResolver = namespaceResolver;
            ArrayList rows = new ArrayList();
            this.rows = rows;
            InitFromXPath(this.ndRoot, xpath);
        }

        internal XPathDocumentView(XPathNode root, ArrayList rows, Shape rowShape) {
            this.rows = rows;
            this.rowShape = rowShape;
            this.ndRoot = root;
        }

        // 
        // public properties
        
        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Document"]/*' />
        public XPathDocument Document { get { return this.document; } }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.XPath"]/*' />
        public String XPath { get { return xpath; } }

        //
        // IEnumerable Implementation

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.GetEnumerator"]/*' />
        public IEnumerator GetEnumerator() {
            return new RowEnumerator(this);
        }

        //
        // ICollection implementation
        
        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Count"]/*' />
        public int Count {
            get { return this.rows.Count; }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.IsSynchronized"]/*' />
        public bool IsSynchronized {
            get {  return false ; }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.SyncRoot"]/*' />
        public object SyncRoot {
            get { return null; }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.CopyTo"]/*' />
        public void CopyTo(Array array, int index) {
            object o;
            ArrayList rows = this.rows;
            for (int i=0; i < rows.Count; i++)
                o = this[i]; // force creation lazy of row object
            rows.CopyTo(array, index);
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.CopyTo2"]/*' />
        /// <devdoc>
        ///    <para>strongly typed version of CopyTo, demanded by Fxcop.</para>
        /// </devdoc>
        public void CopyTo(XPathNodeView[] array, int index) {
            object o;
            ArrayList rows = this.rows;
            for (int i=0; i < rows.Count; i++)
                o = this[i]; // force creation lazy of row object
            rows.CopyTo(array, index);
        }

        //
        // IList Implementation

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.IsReadOnly"]/*' />
        bool IList.IsReadOnly {
            get { return true; }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.IsFixedSize"]/*' />
        bool IList.IsFixedSize {
            get { return true; }
        }        

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Contains"]/*' />
        bool IList.Contains(object value) {
            return this.rows.Contains(value);
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Remove"]/*' />
        void IList.Remove(object value) {            
            throw new NotSupportedException("IList.Remove");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.RemoveAt"]/*' />
        void IList.RemoveAt(int index) {            
            throw new NotSupportedException("IList.RemoveAt");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Clear"]/*' />
        void IList.Clear() {
            throw new NotSupportedException("IList.Clear");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Add"]/*' />
        int IList.Add(object value) {
            throw new NotSupportedException("IList.Add");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Insert"]/*' />
        void IList.Insert(int index, object value) {
            throw new NotSupportedException("IList.Insert");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.IndexOf"]/*' />
        int IList.IndexOf( object value )  {
            return this.rows.IndexOf(value);
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.this"]/*' />
        object IList.this[int index] {
            get { 
                object val = this.rows[index];
                if (val is XPathNodeView)
                    return val;
                XPathNodeView xiv = FillRow((XPathNode)val, this.rowShape);
                this.rows[index] = xiv;
                return xiv;
            }
            set { 
                throw new NotSupportedException("IList.this[]");
            }
        }
        
        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Contains2"]/*' />
        /// <devdoc>
        ///    <para>strongly typed version of Contains, demanded by Fxcop.</para>
        /// </devdoc>
        public bool Contains(XPathNodeView value) {
            return this.rows.Contains(value);
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Add2"]/*' />
        /// <devdoc>
        ///    <para>strongly typed version of Add, demanded by Fxcop.</para>
        /// </devdoc>
        public int Add(XPathNodeView value) {
            throw new NotSupportedException("IList.Add");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Insert2"]/*' />
        /// <devdoc>
        ///    <para>strongly typed version of Insert, demanded by Fxcop.</para>
        /// </devdoc>
        public void Insert(int index, XPathNodeView value) {
            throw new NotSupportedException("IList.Insert");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.IndexOf2"]/*' />
        /// <devdoc>
        ///    <para>strongly typed version of IndexOf, demanded by Fxcop.</para>
        /// </devdoc>
        public int IndexOf(XPathNodeView value)  {
            return this.rows.IndexOf(value);
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Remove2"]/*' />
        /// <devdoc>
        ///    <para>strongly typed version of Remove, demanded by Fxcop.</para>
        /// </devdoc>
        public void Remove(XPathNodeView value) {            
            throw new NotSupportedException("IList.Remove");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Item"]/*' />
        /// <devdoc>
        ///    <para>strongly typed version of Item, demanded by Fxcop.</para>
        /// </devdoc>
        public XPathNodeView this[int index] {
            get { 
                object val = this.rows[index];
                XPathNodeView nodeView;
                nodeView = val as XPathNodeView;
                if (nodeView != null) {
                    return nodeView;
                }
                nodeView = FillRow((XPathNode)val, this.rowShape);
                this.rows[index] = nodeView;
                return nodeView;
            }
            set { 
                throw new NotSupportedException("IList.this[]");
            }
        }

        //
        // IBindingList Implementation

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.AllowEdit"]/*' />
        public bool AllowEdit {
            get { return false; }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.AllowAdd"]/*' />
        public bool AllowAdd {
            get { return false; }
        }  

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.AllowRemove"]/*' />
        public bool AllowRemove {
            get { return false; }
        }          

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.AllowNew"]/*' />
        public bool AllowNew {
            get { return false; }
        }  

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.AddNew"]/*' />
        public object AddNew() {
            throw new NotSupportedException("IBindingList.AddNew");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.SupportsChangeNotification"]/*' />
        public bool SupportsChangeNotification {
            get { return false; }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.ListChanged"]/*' />
        public event ListChangedEventHandler ListChanged {
            add {
                throw new NotSupportedException("IBindingList.ListChanged");
            }
            remove {
                throw new NotSupportedException("IBindingList.ListChanged");
            }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.SupportsSearching"]/*' />
        public bool SupportsSearching {
            get { return false; }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.SupportsSorting"]/*' />
        public bool SupportsSorting {
            get { return false; }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.IsSorted"]/*' />
        public bool IsSorted {
            get { return false; }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.SortProperty"]/*' />
        public PropertyDescriptor SortProperty {
            get { throw new NotSupportedException("IBindingList.SortProperty"); }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.SortDirection"]/*' />
        public ListSortDirection SortDirection {
            get { throw new NotSupportedException("IBindingList.SortDirection"); }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.AddIndex"]/*' />
        public void AddIndex( PropertyDescriptor descriptor ) {
            throw new NotSupportedException("IBindingList.AddIndex");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.ApplySort"]/*' />
        public void ApplySort( PropertyDescriptor descriptor, ListSortDirection direction ) {
            throw new NotSupportedException("IBindingList.ApplySort");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.Find"]/*' />
        public int Find(PropertyDescriptor propertyDescriptor, object key) {
            throw new NotSupportedException("IBindingList.Find");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.RemoveIndex"]/*' />
        public void RemoveIndex(PropertyDescriptor propertyDescriptor) {
            throw new NotSupportedException("IBindingList.RemoveIndex");
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.RemoveSort"]/*' />
        public void RemoveSort() {
            throw new NotSupportedException("IBindingList.RemoveSort");
        }


        //
        // ITypedList Implementation

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.GetListName"]/*' />
        public string GetListName(PropertyDescriptor[] listAccessors) {            
            if( listAccessors == null ) {
                return this.rowShape.Name;
            }
            else {
                return listAccessors[listAccessors.Length-1].Name;
            }
        }

        /// <include file='doc\XPathDocumentView.uex' path='docs/doc[@for="XPathDocumentView.GetItemProperties"]/*' />
        public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors) {
            Shape shape = null;
            if( listAccessors == null ) {
                shape = this.rowShape;
            }
            else {
                XPathNodeViewPropertyDescriptor propdesc = listAccessors[listAccessors.Length-1] as XPathNodeViewPropertyDescriptor;
                if (null != propdesc)
                    shape = propdesc.Shape;
            }

            if (null == shape)
                throw new ArgumentException(nameof(listAccessors));
            return new PropertyDescriptorCollection(shape.PropertyDescriptors);
        }


        //
        // Internal Implementation


        internal Shape RowShape { get { return this.rowShape; } }

        internal void SetRows(ArrayList rows) {
            Debug.Assert(this.rows == null);
            this.rows = rows;
        }


        XPathNodeView FillRow(XPathNode ndRow, Shape shape) {
            object[] columns;
            XPathNode nd;
            switch (shape.BindingType) {
                case BindingType.Text:
                case BindingType.Attribute:
                    columns = new object[1];
                    columns[0] = ndRow;
                    return new XPathNodeView(this, ndRow, columns);

                case BindingType.Repeat:
                    columns = new object[1];
                    nd = TreeNavigationHelper.GetContentChild(ndRow);
                    columns[0] = FillColumn(new ContentIterator(nd, shape), shape);
                    return new XPathNodeView(this, ndRow, columns);

                case BindingType.Sequence:
                case BindingType.Choice:
                case BindingType.All:
                    int subShapesCount = shape.SubShapes.Count;
                    columns = new object[subShapesCount];
                    if (shape.BindingType == BindingType.Sequence
                        && shape.SubShape(0).BindingType == BindingType.Attribute) {
                        FillAttributes(ndRow, shape, columns);
                    }
                    Shape lastSubShape = (Shape)shape.SubShapes[subShapesCount - 1];
                    if (lastSubShape.BindingType == BindingType.Text) { //Attributes followed by simpe content or mixed content
                        columns[subShapesCount - 1] = ndRow;
                        return new XPathNodeView(this, ndRow, columns);
                    }
                    else {
                        nd = TreeNavigationHelper.GetContentChild(ndRow);
                        return FillSubRow(new ContentIterator(nd, shape), shape, columns);
                    }

                default:
                    // should not map to a row
#if DEBUG
                    throw new NotSupportedException("Unable to bind row to: "+shape.BindingType.ToString());
#else
                    throw new NotSupportedException();
#endif
            }
        }

        void FillAttributes(XPathNode nd, Shape shape, object[] cols) {
            int i = 0;
            while (i < cols.Length) {
                Shape attrShape = shape.SubShape(i);
                if (attrShape.BindingType != BindingType.Attribute)
                    break;
                XmlQualifiedName name = attrShape.AttributeName;
                XPathNode ndAttr = nd.GetAttribute( name.Name, name.Namespace );
                if (null != ndAttr)
                    cols[i] = ndAttr;
                i++;
            }
        }

        object FillColumn(ContentIterator iter, Shape shape) {
            object val;
            switch (shape.BindingType) {
                case BindingType.Element:
                    val = iter.Node;
                    iter.Next();
                    break;

                case BindingType.ElementNested: {
                    ArrayList rows = new ArrayList();
                    rows.Add(iter.Node);
                    iter.Next();
                    val = new XPathDocumentView(null, rows, shape.NestedShape);
                    break;
                }

                case BindingType.Repeat: {
                    ArrayList rows = new ArrayList();
                    Shape subShape = shape.SubShape(0);
                    if (subShape.BindingType == BindingType.ElementNested) {
                        Shape nestShape = subShape.NestedShape;
                        XPathDocumentView xivc = new XPathDocumentView(null, null, nestShape);
                        XPathNode nd;
                        while (null != (nd = iter.Node)
                            && subShape.IsParticleMatch(iter.Particle)) {
                            rows.Add(nd);
                            iter.Next();
                        }
                        xivc.SetRows(rows);
                        val = xivc;
                    }
                    else {
                        XPathDocumentView xivc = new XPathDocumentView(null, null, subShape);
                        XPathNode nd;
                        while (null != (nd = iter.Node)
                            && shape.IsParticleMatch(iter.Particle)) {
                            rows.Add(xivc.FillSubRow(iter, subShape, null));
                        }
                        xivc.SetRows(rows);
                        val = xivc;
                    }
                    break;
                }

                case BindingType.Sequence:
                case BindingType.Choice:
                case BindingType.All: {
                    XPathDocumentView docview = new XPathDocumentView(null, null, shape);
                    ArrayList rows = new ArrayList();
                    rows.Add(docview.FillSubRow(iter, shape, null));
                    docview.SetRows(rows);
                    val = docview;
                    break;
                }

                default:
                    case BindingType.Text:
                case BindingType.Attribute:
                    throw new NotSupportedException();
            }
            return val;
        }

        XPathNodeView FillSubRow(ContentIterator iter, Shape shape, object[] columns) {
            if (null == columns) {
                int colCount = shape.SubShapes.Count;
                if (0 == colCount)
                    colCount = 1;
                columns = new object[colCount];
            }

            switch (shape.BindingType) {
                case BindingType.Element:
                    columns[0] = FillColumn(iter, shape);
                    break;

                case BindingType.Sequence: {
                    int iPrev = -1;
                    int i;
                    while (null != iter.Node) {
                        i = shape.FindMatchingSubShape(iter.Particle);
                        if (i <= iPrev)
                            break;
                        columns[i] = FillColumn(iter, shape.SubShape(i));
                        iPrev = i;
                    }
                    break;
                }

                case BindingType.All: {
                    while (null != iter.Node) {
                        int i = shape.FindMatchingSubShape(iter.Particle);
                        if (-1 == i || null != columns[i])
                            break;
                        columns[i] = FillColumn(iter, shape.SubShape(i));
                    }
                    break;
                }

                case BindingType.Choice: {
                    int i = shape.FindMatchingSubShape(iter.Particle);
                    if (-1 != i) {
                        columns[i] = FillColumn(iter, shape.SubShape(i));
                    }
                    break;
                }

                case BindingType.Repeat:
                default:
                    // should not map to a row
                    throw new NotSupportedException();
            }
            return new XPathNodeView(this, null, columns);
        }

        //
        // XPath support
        //

        void InitFromXPath(XPathNode ndRoot, string xpath) {
            XPathStep[] steps = ParseXPath(xpath, this.xpathResolver);
            ArrayList rows = this.rows;
            rows.Clear();
            PopulateFromXPath(ndRoot, steps, 0);
            DeriveShapeFromRows();
        }

        void DeriveShapeFromRows() {
            object schemaInfo = null;
            for (int i=0; (i<rows.Count) && (null==schemaInfo); i++) {
                XPathNode nd = rows[i] as XPathNode;
                Debug.Assert(null != nd && (XPathNodeType.Attribute == nd.NodeType || XPathNodeType.Element == nd.NodeType));
                if (null != nd) {
                    if (XPathNodeType.Attribute == nd.NodeType)
                        schemaInfo = nd.SchemaAttribute;
                    else
                        schemaInfo = nd.SchemaElement;
                }
            }
            if (0 == rows.Count) {
                throw new NotImplementedException("XPath failed to match an elements");
            }
            if (null == schemaInfo) {
                rows.Clear();
                throw new XmlException(SR.XmlDataBinding_NoSchemaType, (string[])null);
            }
            ShapeGenerator shapeGen = new ShapeGenerator(this.namespaceResolver);
            XmlSchemaElement xse = schemaInfo as XmlSchemaElement;
            if (null != xse)
                this.rowShape = shapeGen.GenerateFromSchema(xse);
            else
                this.rowShape = shapeGen.GenerateFromSchema((XmlSchemaAttribute)schemaInfo);
        }

        void PopulateFromXPath(XPathNode nd, XPathStep[] steps, int step) {
            string ln = steps[step].name.Name;
            string ns = steps[step].name.Namespace;
            if (XPathNodeType.Attribute == steps[step].type) {
                XPathNode ndAttr = nd.GetAttribute( ln, ns, true);
                if (null != ndAttr) {
                    if (null != ndAttr.SchemaAttribute)
                        this.rows.Add(ndAttr);
                }
            }
            else {
                XPathNode ndChild = TreeNavigationHelper.GetElementChild(nd, ln, ns, true);
                if (null != ndChild) {
                    int nextStep = step+1;
                    do {
                        if (steps.Length == nextStep) {
                            if (null != ndChild.SchemaType)
                                this.rows.Add(ndChild);
                        }
                        else {
                            PopulateFromXPath(ndChild, steps, nextStep);
                        }
                        ndChild = TreeNavigationHelper.GetElementSibling(ndChild, ln, ns, true);
                    } while (null != ndChild);
                }
            }
        }


        // This is the limited grammar we support
        //  Path ::= '/ ' ( Step '/')* ( QName | '@' QName ) 
        //  Step ::= '.' | QName 
        // This is encoded as an array of XPathStep structs
        struct XPathStep {
            internal XmlQualifiedName name;
            internal XPathNodeType type;
        }

        // Parse xpath (limited to above grammar), using provided namespaceResolver
        // to resolve prefixes.
        XPathStep[] ParseXPath(string xpath, IXmlNamespaceResolver xnr) {
            int pos;
            int stepCount = 1;
            for (pos=1; pos<(xpath.Length-1); pos++) {
                if ( ('/' == xpath[pos]) && ('.' != xpath[pos+1]) )
                    stepCount++;
            }
            XPathStep[] steps = new XPathStep[stepCount];
            pos = 0;
            int i = 0;
            for (;;) {
                if (pos >= xpath.Length)
                    throw new XmlException(SR.XmlDataBinding_XPathEnd, (string[])null);
                if ('/' != xpath[pos])
                    throw new XmlException(SR.XmlDataBinding_XPathRequireSlash, (string[])null);
                pos++;
                char ch = xpath[pos];
                if (ch == '.') {
                    pos++;
                    // again...
                }
                else if ('@' == ch) {
                    if (0 == i)
                        throw new XmlException(SR.XmlDataBinding_XPathAttrNotFirst, (string[])null);
                    pos++;
                    if (pos >= xpath.Length)
                        throw new XmlException(SR.XmlDataBinding_XPathEnd, (string[])null);
                    steps[i].name = ParseQName(xpath, ref pos, xnr);
                    steps[i].type = XPathNodeType.Attribute;
                    i++;
                    if (pos != xpath.Length)
                        throw new XmlException(SR.XmlDataBinding_XPathAttrLast, (string[])null);
                    break;
                }
                else {
                    steps[i].name = ParseQName(xpath, ref pos, xnr);
                    steps[i].type = XPathNodeType.Element;
                    i++;
                    if (pos == xpath.Length)
                        break;
                }
            }
            Debug.Assert(i == steps.Length);
            return steps;
        }

        // Parse a QName from the string, and resolve prefix
        XmlQualifiedName ParseQName(string xpath, ref int pos, IXmlNamespaceResolver xnr) {
            string nm = ParseName(xpath, ref pos);
            if (pos < xpath.Length && ':' == xpath[pos]) {
                pos++;
                string ns = (null==xnr) ? null : xnr.LookupNamespace(nm);
                if (null == ns || 0 == ns.Length)
                    throw new XmlException(SR.Sch_UnresolvedPrefix, nm);
                return new XmlQualifiedName(ParseName(xpath, ref pos), ns);
            }
            else {
                return new XmlQualifiedName(nm);
            }
        }

        // Parse a NCNAME from the string
        string ParseName(string xpath, ref int pos) {
            char ch;
            int start = pos++;
            while (pos < xpath.Length
                && '/' != (ch = xpath[pos])
                && ':' != ch)
                pos++;
            string nm = xpath.Substring(start, pos - start);
            if (!XmlReader.IsName(nm))
                throw new XmlException(SR.Xml_InvalidNameChars, (string[])null);
            return this.document.NameTable.Add(nm);
        }

        
        //
        // Helper classes
        //

        class ContentIterator {
            XPathNode node;
            ContentValidator contentValidator;
            ValidationState currentState;
            object currentParticle;

            public ContentIterator(XPathNode nd, Shape shape) {
                this.node = nd;
                XmlSchemaElement xse = shape.XmlSchemaElement;
                Debug.Assert(null != xse);
                SchemaElementDecl decl = xse.ElementDecl;
                Debug.Assert(null != decl);
                this.contentValidator = decl.ContentValidator;
                this.currentState = new ValidationState();
                this.contentValidator.InitValidation(this.currentState);
                this.currentState.ProcessContents = XmlSchemaContentProcessing.Strict;
                if (nd != null)
                    Advance();
            }

            public XPathNode Node { get { return this.node; } }
            public object Particle { get { return this.currentParticle; } }
            
            public bool Next() {
                if (null != this.node) {
                    this.node = TreeNavigationHelper.GetContentSibling(this.node, XPathNodeType.Element);
                    if (node != null)
                        Advance();                    
                    return null != this.node;
                }
                return false;
            }

            private void Advance() {
                XPathNode nd = this.node;
                int errorCode;
                this.currentParticle = this.contentValidator.ValidateElement(new XmlQualifiedName(nd.LocalName, nd.NamespaceUri), this.currentState, out errorCode);
                if (null == this.currentParticle || 0 != errorCode) {
                    this.node = null;
                }
            }
        }


        // Helper class to implement enumerator over rows
        // We can't just use ArrayList enumerator because
        // sometims rows may be lazily constructed
        sealed class RowEnumerator : IEnumerator {
            XPathDocumentView collection;
            int pos;

            internal RowEnumerator(XPathDocumentView collection) {
                this.collection = collection;
                this.pos = -1;
            }

            public object Current {
                get {
                    if (this.pos < 0 || this.pos >= this.collection.Count)
                        return null;
                    return this.collection[this.pos];
                }
            }

            public void Reset() {
                this.pos = -1;
            }

            public bool MoveNext() {
                this.pos++;
                int max = this.collection.Count;
                if (this.pos > max)
                    this.pos = max;
                return this.pos < max;
            }
        }
    }
}
#endif