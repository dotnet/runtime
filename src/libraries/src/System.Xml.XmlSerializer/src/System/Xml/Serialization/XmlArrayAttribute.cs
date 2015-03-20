// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Xml.Schema;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlArrayAttribute.uex' path='docs/doc[@for="XmlArrayAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
    public class XmlArrayAttribute : System.Attribute
    {
        private string _elementName;
        private string _ns;
        private bool _nullable;
        private XmlSchemaForm _form = XmlSchemaForm.None;
        private int _order = -1;

        /// <include file='doc\XmlArrayAttribute.uex' path='docs/doc[@for="XmlArrayAttribute.XmlArrayAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlArrayAttribute()
        {
        }

        /// <include file='doc\XmlArrayAttribute.uex' path='docs/doc[@for="XmlArrayAttribute.XmlArrayAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlArrayAttribute(string elementName)
        {
            _elementName = elementName;
        }

        /// <include file='doc\XmlArrayAttribute.uex' path='docs/doc[@for="XmlArrayAttribute.ElementName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string ElementName
        {
            get { return _elementName == null ? string.Empty : _elementName; }
            set { _elementName = value; }
        }

        /// <include file='doc\XmlArrayAttribute.uex' path='docs/doc[@for="XmlArrayAttribute.Namespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        /// <include file='doc\XmlArrayAttribute.uex' path='docs/doc[@for="XmlArrayAttribute.IsNullable"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsNullable
        {
            get { return _nullable; }
            set { _nullable = value; }
        }

        /// <include file='doc\XmlArrayAttribute.uex' path='docs/doc[@for="XmlArrayAttribute.Form"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaForm Form
        {
            get { return _form; }
            set { _form = value; }
        }

        /// <include file='doc\XmlArrayAttribute.uex' path='docs/doc[@for="XmlArrayAttribute.Order"]/*' />
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
