// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Xml.Schema;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlAnyElementAttribute.uex' path='docs/doc[@for="XmlAnyElementAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
    public class XmlAnyElementAttribute : System.Attribute
    {
        private string _name;
        private string _ns;
        private int _order = -1;
        private bool _nsSpecified = false;

        /// <include file='doc\XmlAnyElementAttribute.uex' path='docs/doc[@for="XmlAnyElementAttribute.XmlAnyElementAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAnyElementAttribute()
        {
        }

        /// <include file='doc\XmlAnyElementAttribute.uex' path='docs/doc[@for="XmlAnyElementAttribute.XmlAnyElementAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAnyElementAttribute(string name)
        {
            _name = name;
        }

        /// <include file='doc\XmlAnyElementAttribute.uex' path='docs/doc[@for="XmlAnyElementAttribute.XmlAnyElementAttribute2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAnyElementAttribute(string name, string ns)
        {
            _name = name;
            _ns = ns;
            _nsSpecified = true;
        }

        /// <include file='doc\XmlAnyElementAttribute.uex' path='docs/doc[@for="XmlAnyElementAttribute.Name"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Name
        {
            get { return _name == null ? string.Empty : _name; }
            set { _name = value; }
        }

        /// <include file='doc\XmlAnyElementAttribute.uex' path='docs/doc[@for="XmlAnyElementAttribute.Namespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return _ns; }
            set
            {
                _ns = value;
                _nsSpecified = true;
            }
        }

        /// <include file='doc\XmlAnyElementAttribute.uex' path='docs/doc[@for="XmlAnyElementAttribute.Order"]/*' />
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

        internal bool NamespaceSpecified
        {
            get { return _nsSpecified; }
        }
    }
}
