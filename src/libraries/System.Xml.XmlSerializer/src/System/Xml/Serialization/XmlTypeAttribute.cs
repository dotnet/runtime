// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;


namespace System.Xml.Serialization
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class XmlTypeAttribute : System.Attribute
    {
        private bool _includeInSchema = true;
        private bool _anonymousType;
        private string _ns;
        private string _typeName;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTypeAttribute()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlTypeAttribute(string typeName)
        {
            _typeName = typeName;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool AnonymousType
        {
            get { return _anonymousType; }
            set { _anonymousType = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IncludeInSchema
        {
            get { return _includeInSchema; }
            set { _includeInSchema = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeName
        {
            get { return _typeName == null ? string.Empty : _typeName; }
            set { _typeName = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }
    }
}
