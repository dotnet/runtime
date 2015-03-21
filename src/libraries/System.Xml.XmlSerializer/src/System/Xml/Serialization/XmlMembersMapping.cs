// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System.Reflection;
using System;
using System.Text;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlMembersMapping.uex' path='docs/doc[@for="XmlMembersMapping"]/*' />
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlMembersMapping : XmlMapping
    {
        private XmlMemberMapping[] _mappings;

        internal XmlMembersMapping(TypeScope scope, ElementAccessor accessor, XmlMappingAccess access) : base(scope, accessor, access)
        {
            MembersMapping mapping = (MembersMapping)accessor.Mapping;
            StringBuilder key = new StringBuilder();
            key.Append(":");
            _mappings = new XmlMemberMapping[mapping.Members.Length];
            for (int i = 0; i < _mappings.Length; i++)
            {
                if (mapping.Members[i].TypeDesc.Type != null)
                {
                    key.Append(GenerateKey(mapping.Members[i].TypeDesc.Type, null, null));
                    key.Append(":");
                }
                _mappings[i] = new XmlMemberMapping(mapping.Members[i]);
            }
            SetKeyInternal(key.ToString());
        }

        /// <include file='doc\XmlMembersMapping.uex' path='docs/doc[@for="XmlMembersMapping.TypeName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeName
        {
            get { return Accessor.Mapping.TypeName; }
        }

        /// <include file='doc\XmlMembersMapping.uex' path='docs/doc[@for="XmlMembersMapping.TypeNamespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TypeNamespace
        {
            get { return Accessor.Mapping.Namespace; }
        }

        /// <include file='doc\XmlMembersMapping.uex' path='docs/doc[@for="XmlMembersMapping.this"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlMemberMapping this[int index]
        {
            get { return _mappings[index]; }
        }

        /// <include file='doc\XmlMembersMapping.uex' path='docs/doc[@for="XmlMembersMapping.Count"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Count
        {
            get { return _mappings.Length; }
        }
    }
}
