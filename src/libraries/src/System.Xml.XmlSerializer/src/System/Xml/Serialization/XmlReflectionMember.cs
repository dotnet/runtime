// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlReflectionMember.uex' path='docs/doc[@for="XmlReflectionMember"]/*' />
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlReflectionMember
    {
        private string _memberName;
        private Type _type;
        private XmlAttributes _xmlAttributes = new XmlAttributes();
        private bool _isReturnValue;
        private bool _overrideIsNullable;

        /// <include file='doc\XmlReflectionMember.uex' path='docs/doc[@for="XmlReflectionMember.MemberType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Type MemberType
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <include file='doc\XmlReflectionMember.uex' path='docs/doc[@for="XmlReflectionMember.XmlAttributes"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttributes XmlAttributes
        {
            get { return _xmlAttributes; }
            set { _xmlAttributes = value; }
        }


        /// <include file='doc\XmlReflectionMember.uex' path='docs/doc[@for="XmlReflectionMember.MemberName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string MemberName
        {
            get { return _memberName == null ? string.Empty : _memberName; }
            set { _memberName = value; }
        }

        /// <include file='doc\XmlReflectionMember.uex' path='docs/doc[@for="XmlReflectionMember.IsReturnValue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsReturnValue
        {
            get { return _isReturnValue; }
            set { _isReturnValue = value; }
        }

        /// <include file='doc\XmlReflectionMember.uex' path='docs/doc[@for="XmlReflectionMember.OverrideIsNullable"]/*' />
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
