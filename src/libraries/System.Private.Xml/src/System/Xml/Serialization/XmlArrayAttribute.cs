// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Schema;


namespace System.Xml.Serialization
{
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

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlArrayAttribute()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlArrayAttribute(string elementName)
        {
            _elementName = elementName;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string ElementName
        {
            get { return _elementName == null ? string.Empty : _elementName; }
            set { _elementName = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsNullable
        {
            get { return _nullable; }
            set { _nullable = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaForm Form
        {
            get { return _form; }
            set { _form = value; }
        }

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
