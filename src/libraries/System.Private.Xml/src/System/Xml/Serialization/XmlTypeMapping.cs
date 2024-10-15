// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace System.Xml.Serialization
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlTypeMapping : XmlMapping
    {
        internal XmlTypeMapping(TypeScope? scope, ElementAccessor accessor) : base(scope, accessor)
        {
        }

        internal TypeMapping? Mapping
        {
            get { return Accessor.Mapping; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeName
        {
            get
            {
                return Mapping!.TypeDesc!.Name;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeFullName
        {
            get
            {
                return Mapping!.TypeDesc!.FullName;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string? XsdTypeName
        {
            get
            {
                return Mapping!.TypeName;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string? XsdTypeNamespace
        {
            get
            {
                return Mapping!.Namespace;
            }
        }
    }
}
