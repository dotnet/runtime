// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Xml.Serialization;

namespace System.Xml.Serialization
{
    [Flags]
    public enum XmlMappingAccess
    {
        None = 0x00,
        Read = 0x01,
        Write = 0x02,
    }

    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public abstract class XmlMapping
    {
        private readonly TypeScope? _scope;
        private bool _generateSerializer;
        private bool _isSoap;
        private readonly ElementAccessor _accessor;
        private string? _key;
        private readonly bool _shallow;
        private readonly XmlMappingAccess _access;

        internal XmlMapping(TypeScope? scope, ElementAccessor accessor) : this(scope, accessor, XmlMappingAccess.Read | XmlMappingAccess.Write)
        {
        }

        internal XmlMapping(TypeScope? scope, ElementAccessor accessor, XmlMappingAccess access)
        {
            _scope = scope;
            _accessor = accessor;
            _access = access;
            _shallow = scope == null;
        }

        internal ElementAccessor Accessor
        {
            get { return _accessor; }
        }

        internal TypeScope? Scope
        {
            get { return _scope; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string ElementName
        {
            get { return System.Xml.Serialization.Accessor.UnescapeName(Accessor.Name); }
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
        public string? Namespace
        {
            get { return _accessor.Namespace; }
        }

        internal bool GenerateSerializer
        {
            get { return _generateSerializer; }
            set { _generateSerializer = value; }
        }

        internal bool IsReadable
        {
            get { return ((_access & XmlMappingAccess.Read) != 0); }
        }

        internal bool IsWriteable
        {
            get { return ((_access & XmlMappingAccess.Write) != 0); }
        }

        internal bool IsSoap
        {
            get { return _isSoap; }
            set { _isSoap = value; }
        }

        ///<internalonly/>
        public void SetKey(string? key)
        {
            SetKeyInternal(key);
        }

        ///<internalonly/>
        internal void SetKeyInternal(string? key)
        {
            _key = key;
        }

        internal static string GenerateKey(Type type, XmlRootAttribute? root, string? ns)
        {
            if (root == null)
            {
                root = (XmlRootAttribute?)XmlAttributes.GetAttr(type, typeof(XmlRootAttribute));
            }
            return $"{type.FullName}:{(root == null ? string.Empty : root.GetKey())}:{ns ?? string.Empty}";
        }

        internal string? Key { get { return _key; } }

        internal void CheckShallow()
        {
            if (_shallow)
            {
                throw new InvalidOperationException(SR.XmlMelformMapping);
            }
        }

        internal static bool IsShallow(XmlMapping[] mappings)
        {
            for (int i = 0; i < mappings.Length; i++)
            {
                if (mappings[i] == null || mappings[i]._shallow)
                    return true;
            }
            return false;
        }
    }
}
