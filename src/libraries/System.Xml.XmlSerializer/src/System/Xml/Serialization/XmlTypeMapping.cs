// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System.Reflection;
using System;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlTypeMapping.uex' path='docs/doc[@for="XmlTypeMapping"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlTypeMapping : XmlMapping
    {
        internal XmlTypeMapping(TypeScope scope, ElementAccessor accessor) : base(scope, accessor)
        {
        }

        internal TypeMapping Mapping
        {
            get { return Accessor.Mapping; }
        }

        /// <include file='doc\XmlTypeMapping.uex' path='docs/doc[@for="XmlTypeMapping.TypeName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeName
        {
            get
            {
                return Mapping.TypeDesc.Name;
            }
        }

        /// <include file='doc\XmlTypeMapping.uex' path='docs/doc[@for="XmlTypeMapping.TypeFullName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeFullName
        {
            get
            {
                return Mapping.TypeDesc.FullName;
            }
        }

        /// <include file='doc\XmlTypeMapping.uex' path='docs/doc[@for="XmlTypeMapping.XsdTypeName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string XsdTypeName
        {
            get
            {
                return Mapping.TypeName;
            }
        }

        /// <include file='doc\XmlTypeMapping.uex' path='docs/doc[@for="XmlTypeMapping.XsdTypeNamespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string XsdTypeNamespace
        {
            get
            {
                return Mapping.Namespace;
            }
        }
    }
}
