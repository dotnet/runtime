// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System.Reflection;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlMemberMapping.uex' path='docs/doc[@for="XmlMemberMapping"]/*' />
    /// <internalonly/>
    public class XmlMemberMapping
    {
        private MemberMapping _mapping;

        internal XmlMemberMapping(MemberMapping mapping)
        {
            _mapping = mapping;
        }


        internal Accessor Accessor
        {
            get { return _mapping.Accessor; }
        }

        /// <include file='doc\XmlMemberMapping.uex' path='docs/doc[@for="XmlMemberMapping.Any"]/*' />
        public bool Any
        {
            get { return Accessor.Any; }
        }

        /// <include file='doc\XmlMemberMapping.uex' path='docs/doc[@for="XmlMemberMapping.ElementName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string ElementName
        {
            get { return Accessor.UnescapeName(Accessor.Name); }
        }

        /// <include file='doc\XmlMemberMapping.uex' path='docs/doc[@for="XmlMemberMapping.XsdElementName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string XsdElementName
        {
            get { return Accessor.Name; }
        }

        /// <include file='doc\XmlMemberMapping.uex' path='docs/doc[@for="XmlMemberMapping.Namespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Namespace
        {
            get { return Accessor.Namespace; }
        }

        /// <include file='doc\XmlMemberMapping.uex' path='docs/doc[@for="XmlMemberMapping.MemberName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string MemberName
        {
            get { return _mapping.Name; }
        }

        /// <include file='doc\XmlMemberMapping.uex' path='docs/doc[@for="XmlMemberMapping.TypeName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeName
        {
            get { return Accessor.Mapping != null ? Accessor.Mapping.TypeName : String.Empty; }
        }

        /// <include file='doc\XmlMemberMapping.uex' path='docs/doc[@for="XmlMemberMapping.TypeNamespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeNamespace
        {
            get { return Accessor.Mapping != null ? Accessor.Mapping.Namespace : null; }
        }

        /// <include file='doc\XmlMemberMapping.uex' path='docs/doc[@for="XmlMemberMapping.TypeFullName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeFullName
        {
            get { return _mapping.TypeDesc.FullName; }
        }

        /// <include file='doc\XmlMemberMapping.uex' path='docs/doc[@for="XmlMemberMapping.CheckSpecified"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool CheckSpecified
        {
            get { return _mapping.CheckSpecified != SpecifiedAccessor.None; }
        }
    }
}
