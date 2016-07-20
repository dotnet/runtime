// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaChoice.uex' path='docs/doc[@for="XmlSchemaChoice"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaChoice : XmlSchemaGroupBase
    {
        private XmlSchemaObjectCollection _items = new XmlSchemaObjectCollection();

        /// <include file='doc\XmlSchemaChoice.uex' path='docs/doc[@for="XmlSchemaChoice.Items"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("element", typeof(XmlSchemaElement)),
         XmlElement("group", typeof(XmlSchemaGroupRef)),
         XmlElement("choice", typeof(XmlSchemaChoice)),
         XmlElement("sequence", typeof(XmlSchemaSequence)),
         XmlElement("any", typeof(XmlSchemaAny))]
        public override XmlSchemaObjectCollection Items
        {
            get { return _items; }
        }

        internal override bool IsEmpty
        {
            get { return base.IsEmpty /*|| items.Count == 0*/; }
        }

        internal override void SetItems(XmlSchemaObjectCollection newItems)
        {
            _items = newItems;
        }
    }
}
