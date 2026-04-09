// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Serialization;

namespace System.Xml.Serialization
{
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlReflectionMember
    {
        private string? _memberName;
        private Type? _type;
        private XmlAttributes _xmlAttributes = new XmlAttributes();
        private SoapAttributes _soapAttributes = new SoapAttributes();
        private bool _isReturnValue;
        private bool _overrideIsNullable;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Type? MemberType
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributes XmlAttributes
        {
            get { return _xmlAttributes; }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _xmlAttributes = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SoapAttributes SoapAttributes
        {
            get { return _soapAttributes; }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _soapAttributes = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string MemberName
        {
            get { return _memberName ?? string.Empty; }
            set { _memberName = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsReturnValue
        {
            get { return _isReturnValue; }
            set { _isReturnValue = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool OverrideIsNullable
        {
            get { return _overrideIsNullable; }
            set { _overrideIsNullable = value; }
        }
    }
}
