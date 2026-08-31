// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
    public class XmlAnyElementAttribute : System.Attribute
    {
        private string? _name;
        private string? _ns;
        private int _order = -1;
        private bool _nsSpecified;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAnyElementAttribute()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAnyElementAttribute(string? name)
        {
            _name = name;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAnyElementAttribute(string? name, string? ns)
        {
            _name = name;
            _ns = ns;
            _nsSpecified = true;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [AllowNull]
        public string Name
        {
            get { return _name ?? string.Empty; }
            set { _name = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string? Namespace
        {
            get { return _ns; }
            set
            {
                _ns = value;
                _nsSpecified = true;
            }
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

        internal bool NamespaceSpecified
        {
            get { return _nsSpecified; }
        }

        internal bool GetNamespaceSpecified()
        {
            return NamespaceSpecified;
        }
    }
}
