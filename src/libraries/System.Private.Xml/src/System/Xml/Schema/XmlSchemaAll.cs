// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.Xml.Serialization;

    public class XmlSchemaAll : XmlSchemaGroupBase
    {
        private XmlSchemaObjectCollection _items = new XmlSchemaObjectCollection();

        [XmlElement("element", typeof(XmlSchemaElement))]
        public override XmlSchemaObjectCollection Items
        {
            get { return _items; }
        }

        internal override bool IsEmpty
        {
            get { return base.IsEmpty || _items.Count == 0; }
        }

        internal override void SetItems(XmlSchemaObjectCollection newItems)
        {
            _items = newItems;
        }
    }
}
