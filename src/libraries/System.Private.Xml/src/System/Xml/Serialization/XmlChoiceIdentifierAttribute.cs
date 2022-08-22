// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Schema;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
    public class XmlChoiceIdentifierAttribute : System.Attribute
    {
        private string? _name;
        private MemberInfo? _memberInfo;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlChoiceIdentifierAttribute()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlChoiceIdentifierAttribute(string? name)
        {
            _name = name;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [AllowNull]
        public string MemberName
        {
            get { return _name ?? string.Empty; }
            set { _name = value; }
        }

        internal MemberInfo? MemberInfo
        {
            get { return _memberInfo; }
            set { _memberInfo = value; }
        }

        internal MemberInfo? GetMemberInfo()
        {
            return MemberInfo;
        }

        internal void SetMemberInfo(MemberInfo? memberInfo)
        {
            MemberInfo = memberInfo;
        }
    }
}
