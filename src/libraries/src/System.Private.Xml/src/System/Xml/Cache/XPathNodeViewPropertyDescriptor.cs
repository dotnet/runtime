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
    internal class XPathNodeViewPropertyDescriptor : PropertyDescriptor {
        Shape rowShape;
        Shape colShape;
        int colIndex;

        internal XPathNodeViewPropertyDescriptor(Shape rowShape) 
            : base( rowShape.Name, null) {
            this.rowShape = rowShape;
            this.colShape = rowShape;
            this.colIndex = 0;
        }

        internal XPathNodeViewPropertyDescriptor(Shape rowShape, Shape colShape, int colIndex) 
            : base( colShape.Name, null) {
            this.rowShape = rowShape;
            this.colShape = colShape;
            this.colIndex = colIndex;
        }

        public Shape Shape { 
            get { return colShape; } 
        }

        public override Type ComponentType {
            get {  return null;  }
        }

        public override string Name {
            get {  return this.colShape.Name;  }
        }

        public override bool IsReadOnly {
            get { return true;  }
        }

        public override Type PropertyType {
            get { 
                return this.colShape.IsNestedTable 
                    ? typeof(XPathDocumentView) 
                    : typeof(string); 
            }
        }

        public override bool CanResetValue(object o) {
            return false;
        }

        public override object GetValue(object o) {
            if (null == o)
                throw new ArgumentNullException("XPathNodeViewPropertyDescriptor.GetValue");
            XPathNodeView xiv = (XPathNodeView)o;
            if (xiv.Collection.RowShape != this.rowShape)
                throw new ArgumentException("XPathNodeViewPropertyDescriptor.GetValue");
            object val = xiv.Column(this.colIndex);
            XPathNode nd = val as XPathNode;
            if (null != nd) {
                XPathDocumentNavigator nav = new XPathDocumentNavigator(nd, null);
                XmlSchemaType xst = nd.SchemaType;
                XmlSchemaComplexType xsct = xst as XmlSchemaComplexType;
                if (null == xst || ( (null != xsct) && xsct.IsMixed) ) {
                    return nav.InnerXml;
                }
                else {
                    return nav.TypedValue;
                }
            }
            return val;
        }

        public override void ResetValue(object o) {
            throw new NotImplementedException();
        }

        public override void SetValue(object o, object value) {
            throw new NotImplementedException();
        }

        public override bool ShouldSerializeValue(object o) {
            return false;
        }
    }
}
#endif