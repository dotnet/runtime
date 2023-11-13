// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Text;

namespace System.Xml.Serialization
{
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlMembersMapping : XmlMapping
    {
        private readonly XmlMemberMapping[] _mappings;

        internal XmlMembersMapping(TypeScope scope, ElementAccessor accessor, XmlMappingAccess access) : base(scope, accessor, access)
        {
            MembersMapping mapping = (MembersMapping)accessor.Mapping!;
            StringBuilder key = new StringBuilder();
            key.Append(':');
            _mappings = new XmlMemberMapping[mapping.Members!.Length];
            for (int i = 0; i < _mappings.Length; i++)
            {
                if (mapping.Members[i].TypeDesc!.Type != null)
                {
                    key.Append(GenerateKey(mapping.Members[i].TypeDesc!.Type!, null, null));
                    key.Append(':');
                }
                _mappings[i] = new XmlMemberMapping(mapping.Members[i]);
            }
            SetKeyInternal(key.ToString());
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string? TypeName
        {
            get { return Accessor.Mapping!.TypeName; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string? TypeNamespace
        {
            get { return Accessor.Mapping!.Namespace; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlMemberMapping this[int index]
        {
            get { return _mappings[index]; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Count
        {
            get { return _mappings.Length; }
        }
    }
}
