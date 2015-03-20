// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Xml.Schema;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlElementAttribute.uex' path='docs/doc[@for="XmlElementAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
    public class XmlElementAttribute : System.Attribute
    {
        private string _elementName;
        private Type _type;
        private string _ns;
        private string _dataType;
        private bool _nullable;
        private bool _nullableSpecified;
        private XmlSchemaForm _form = XmlSchemaForm.None;
        private int _order = -1;

        /// <include file='doc\XmlElementAttribute.uex' path='docs/doc[@for="XmlElementAttribute.XmlElementAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttribute()
        {
        }

        /// <include file='doc\XmlElementAttribute.uex' path='docs/doc[@for="XmlElementAttribute.XmlElementAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttribute(string elementName)
        {
            _elementName = elementName;
        }

        /// <include file='doc\XmlElementAttribute.uex' path='docs/doc[@for="XmlElementAttribute.XmlElementAttribute2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttribute(Type type)
        {
            _type = type;
        }

        /// <include file='doc\XmlElementAttribute.uex' path='docs/doc[@for="XmlElementAttribute.XmlElementAttribute3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttribute(string elementName, Type type)
        {
            _elementName = elementName;
            _type = type;
        }

        /// <include file='doc\XmlElementAttribute.uex' path='docs/doc[@for="XmlElementAttribute.Type"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <include file='doc\XmlElementAttribute.uex' path='docs/doc[@for="XmlElementAttribute.ElementName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string ElementName
        {
            get { return _elementName == null ? string.Empty : _elementName; }
            set { _elementName = value; }
        }

        /// <include file='doc\XmlElementAttribute.uex' path='docs/doc[@for="XmlElementAttribute.Namespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        /// <include file='doc\XmlElementAttribute.uex' path='docs/doc[@for="XmlElementAttribute.DataType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string DataType
        {
            get { return _dataType == null ? string.Empty : _dataType; }
            set { _dataType = value; }
        }

        /// <include file='doc\XmlElementAttribute.uex' path='docs/doc[@for="XmlElementAttribute.IsNullable"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsNullable
        {
            get { return _nullable; }
            set
            {
                _nullable = value;
                _nullableSpecified = true;
            }
        }

        internal bool IsNullableSpecified
        {
            get { return _nullableSpecified; }
        }

        /// <include file='doc\XmlElementAttribute.uex' path='docs/doc[@for="XmlElementAttribute.Form"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaForm Form
        {
            get { return _form; }
            set { _form = value; }
        }

        /// <include file='doc\XmlElementAttribute.uex' path='docs/doc[@for="XmlElementAttribute.Order"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Order
        {
            get { return _order; }
            set
            {
                if (value < 0)
                    throw new ArgumentException(SR.XmlDisallowNegativeValues, "Order");
                _order = value;
            }
        }
    }
}
