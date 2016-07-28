// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaSimpleTypeList.uex' path='docs/doc[@for="XmlSchemaSimpleTypeList"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaSimpleTypeList : XmlSchemaSimpleTypeContent
    {
        private XmlQualifiedName _itemTypeName = XmlQualifiedName.Empty;
        private XmlSchemaSimpleType _itemType;
        private XmlSchemaSimpleType _baseItemType; //Compiled

        /// <include file='doc\XmlSchemaSimpleTypeList.uex' path='docs/doc[@for="XmlSchemaSimpleTypeList.ItemTypeName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("itemType")]
        public XmlQualifiedName ItemTypeName
        {
            get { return _itemTypeName; }
            set { _itemTypeName = (value == null ? XmlQualifiedName.Empty : value); }
        }

        /// <include file='doc\XmlSchemaSimpleTypeList.uex' path='docs/doc[@for="XmlSchemaSimpleTypeList.BaseType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlElement("simpleType", typeof(XmlSchemaSimpleType))]
        public XmlSchemaSimpleType ItemType
        {
            get { return _itemType; }
            set { _itemType = value; }
        }

        //Compiled
        /// <include file='doc\XmlSchemaSimpleTypeList.uex' path='docs/doc[@for="XmlSchemaSimpleTypeList.BaseItemType"]/*' />
        [XmlIgnore]
        public XmlSchemaSimpleType BaseItemType
        {
            get { return _baseItemType; }
            set { _baseItemType = value; }
        }

        internal override XmlSchemaObject Clone()
        {
            XmlSchemaSimpleTypeList newList = (XmlSchemaSimpleTypeList)MemberwiseClone();
            newList.ItemTypeName = _itemTypeName.Clone();
            return newList;
        }
    }
}
