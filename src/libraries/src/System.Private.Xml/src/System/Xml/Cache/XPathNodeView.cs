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
    public sealed class XPathNodeView : IXPathNavigable, ICustomTypeDescriptor, INotifyPropertyChanged {
        XPathDocumentView collection;
        XPathNode rowNd;
        object[] cols;

        internal XPathNodeView(XPathDocumentView col, XPathNode rowNd, object[] columns) {
            this.collection = col;
            this.rowNd = rowNd;
            this.cols = columns;
        }

        //
        // local methods

        public XPathDocumentView XPathDocumentView {
            get { return this.collection; }
        }

        public object this[string fieldname] {
            get {
                if (null == fieldname)
                    throw new ArgumentNullException(nameof(fieldname));
                int col = this.collection.RowShape.FindNamedSubShape(fieldname);
                if (col == -1)
                    throw new ArgumentException(nameof(fieldname));
                Debug.Assert(col >= 0 && col < this.cols.Length);
                return this.cols[col];
            }
            set {
                throw new NotSupportedException();
            }
        }

        public object this[int fieldIndex] {
            get {
                if (fieldIndex < 0 || fieldIndex >= this.cols.Length)
                    throw new ArgumentOutOfRangeException(nameof(fieldIndex));
                return this.cols[fieldIndex];
            }
            set {
                throw new NotSupportedException();
            }
        }

        //
        // IXPathNavigable Implementation
        
        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.CreateNavigator"]/*' />
        public XPathNavigator CreateNavigator() {
            XPathNode nd = this.rowNd;
            if (null != nd)
                return new XPathDocumentNavigator(this.rowNd, null);
            return null;
        }

        //
        // ICustomTypeDescriptor Implementation
        
        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.GetAttributes"]/*' />
        public AttributeCollection GetAttributes() {
            return new AttributeCollection((Attribute[])null);
        }
        
        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.GetClassName"]/*' />
        public String GetClassName() {
            return collection.RowShape.Name;
        }
        
        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.GetComponentName"]/*' />
        public String GetComponentName() {
            return null;
        }

        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.GetConverter"]/*' />
        public TypeConverter GetConverter() {
            return null;
        }

        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.GetDefaultEvent"]/*' />
        public EventDescriptor GetDefaultEvent() {
            return null;
        }

        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.GetDefaultProperty"]/*' />
        public PropertyDescriptor GetDefaultProperty() {
            return null;
        }

        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.GetEditor"]/*' />
        public object GetEditor(Type editorBaseType) {
            return null;
        }

        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.GetEvents"]/*' />
        public EventDescriptorCollection GetEvents() {
            return null;
        }

        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.GetEvents2"]/*' />
        public EventDescriptorCollection GetEvents(Attribute[] attributes) {
            return null;
        }

        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.GetPropertyOwner"]/*' />
        public object GetPropertyOwner(PropertyDescriptor pd) {
            return null;
        }

        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.GetProperties"]/*' />
        public PropertyDescriptorCollection GetProperties() {
            return collection.GetItemProperties(null);
        }

        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.GetProperties2"]/*' />
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes) {
            return collection.GetItemProperties(null);
        }


        //
        // INotifyPropertyChanged Implementation

        /// <include file='doc\XPathNodeView.uex' path='docs/doc[@for="XPathNodeView.PropertyChanged"]/*' />
        public event PropertyChangedEventHandler PropertyChanged {
            add {
                throw new NotSupportedException("INotifyPropertyChanged.PropertyChanged");
            }
            remove {
                throw new NotSupportedException("INotifyPropertyChanged.PropertyChanged");
            }
        }

        //
        // internal implementation

        // used by XPathNodeViewPropertyDescriptor to access values
        internal XPathDocumentView Collection { get { return this.collection; } }
        internal object Column(int index) { return cols[index]; }
    }
}
#endif