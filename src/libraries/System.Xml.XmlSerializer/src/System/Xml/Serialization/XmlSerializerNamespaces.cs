// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System.Xml.Extensions;

namespace System.Xml.Serialization
{
    using System.Reflection;
    using System.Collections;
    using System.IO;
    using System.Xml.Schema;
    using System;
    // this[key] api throws KeyNotFoundException
    using Hashtable = System.Collections.InternalHashtable;

    /// <include file='doc\XmlSerializerNamespaces.uex' path='docs/doc[@for="XmlSerializerNamespaces"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSerializerNamespaces
    {
        private Hashtable _namespaces = null;

        /// <include file='doc\XmlSerializerNamespaces.uex' path='docs/doc[@for="XmlSerializerNamespaces.XmlSerializerNamespaces"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializerNamespaces()
        {
        }


        /// <include file='doc\XmlSerializerNamespaces.uex' path='docs/doc[@for="XmlSerializerNamespaces.XmlSerializerNamespaces1"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializerNamespaces(XmlSerializerNamespaces namespaces)
        {
            _namespaces = (Hashtable)namespaces.Namespaces.Clone();
        }

        /// <include file='doc\XmlSerializerNamespaces.uex' path='docs/doc[@for="XmlSerializerNamespaces.XmlSerializerNamespaces2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializerNamespaces(XmlQualifiedName[] namespaces)
        {
            for (int i = 0; i < namespaces.Length; i++)
            {
                XmlQualifiedName qname = namespaces[i];
                Add(qname.Name, qname.Namespace);
            }
        }

        /// <include file='doc\XmlSerializerNamespaces.uex' path='docs/doc[@for="XmlSerializerNamespaces.Add"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Add(string prefix, string ns)
        {
            // parameter value check
            if (prefix != null && prefix.Length > 0)
                XmlConvert.VerifyNCName(prefix);

            if (ns != null && ns.Length > 0)
                ExtensionMethods.ToUri(ns);
            AddInternal(prefix, ns);
        }

        internal void AddInternal(string prefix, string ns)
        {
            Namespaces[prefix] = ns;
        }

        /// <include file='doc\XmlSerializerNamespaces.uex' path='docs/doc[@for="XmlSerializerNamespaces.ToArray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlQualifiedName[] ToArray()
        {
            if (NamespaceList == null)
                return new XmlQualifiedName[0];
            return (XmlQualifiedName[])NamespaceList.ToArray(typeof(XmlQualifiedName));
        }

        /// <include file='doc\XmlSerializerNamespaces.uex' path='docs/doc[@for="XmlSerializerNamespaces.Count"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Count
        {
            get { return Namespaces.Count; }
        }

        internal ArrayList NamespaceList
        {
            get
            {
                if (_namespaces == null || _namespaces.Count == 0)
                    return null;
                ArrayList namespaceList = new ArrayList();
                foreach (string key in Namespaces.Keys)
                {
                    namespaceList.Add(new XmlQualifiedName(key, (string)Namespaces[key]));
                }
                return namespaceList;
            }
        }

        internal Hashtable Namespaces
        {
            get
            {
                if (_namespaces == null)
                    _namespaces = new Hashtable();
                return _namespaces;
            }
            set { _namespaces = value; }
        }

        internal string LookupPrefix(string ns)
        {
            if (string.IsNullOrEmpty(ns))
                return null;
            if (_namespaces == null || _namespaces.Count == 0)
                return null;

            foreach (string prefix in _namespaces.Keys)
            {
                if (!string.IsNullOrEmpty(prefix) && (string)_namespaces[prefix] == ns)
                {
                    return prefix;
                }
            }
            return null;
        }
    }
}

