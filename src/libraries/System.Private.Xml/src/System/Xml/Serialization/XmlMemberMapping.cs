// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System;


namespace System.Xml.Serialization
{
    /// <internalonly/>
    public class XmlMemberMapping
    {
        private MemberMapping _mapping;

        internal XmlMemberMapping(MemberMapping mapping)
        {
            _mapping = mapping;
        }

        internal MemberMapping Mapping
        {
            get { return _mapping; }
        }

        internal Accessor Accessor
        {
            get { return _mapping.Accessor; }
        }

        public bool Any
        {
            get { return Accessor.Any; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string ElementName
        {
            get { return Accessor.UnescapeName(Accessor.Name); }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string XsdElementName
        {
            get { return Accessor.Name; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return Accessor.Namespace; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string MemberName
        {
            get { return _mapping.Name; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeName
        {
            get { return Accessor.Mapping != null ? Accessor.Mapping.TypeName : String.Empty; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeNamespace
        {
            get { return Accessor.Mapping != null ? Accessor.Mapping.Namespace : null; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeFullName
        {
            get { return _mapping.TypeDesc.FullName; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool CheckSpecified
        {
            get { return _mapping.CheckSpecified != SpecifiedAccessor.None; }
        }
    }
}
