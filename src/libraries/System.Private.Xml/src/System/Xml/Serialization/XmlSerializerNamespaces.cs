// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System.Reflection;
    using System.Collections;
    using System.IO;
    using System.Xml.Schema;
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.Diagnostics.CodeAnalysis;

    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSerializerNamespaces
    {
        private Dictionary<string, XmlQualifiedName>? _namespaces;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializerNamespaces()
        {
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializerNamespaces(XmlSerializerNamespaces namespaces)
        {
            _namespaces = new Dictionary<string, XmlQualifiedName>(namespaces.NamespacesInternal);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializerNamespaces(XmlQualifiedName[] namespaces)
        {
            _namespaces = new Dictionary<string, XmlQualifiedName>(namespaces.Length);

            foreach (var qname in namespaces)
                _namespaces.Add(qname.Name, qname);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal XmlSerializerNamespaces(IList<XmlQualifiedName> namespaces)
        {
            _namespaces = new Dictionary<string, XmlQualifiedName>(namespaces.Count);

            foreach (var qname in namespaces)
                _namespaces.Add(qname.Name, qname);
        }


        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Add(string prefix, string? ns)
        {
            // parameter value check
            if (prefix != null && prefix.Length > 0)
                XmlConvert.VerifyNCName(prefix);

            if (ns != null && ns.Length > 0)
                XmlConvert.ToUri(ns);
            AddInternal(prefix!, ns);
        }

        internal void AddInternal(string prefix, string? ns)
        {
            NamespacesInternal[prefix] = new XmlQualifiedName(prefix, ns);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlQualifiedName[] ToArray()
        {
            if (_namespaces == null || _namespaces.Count == 0)
                return Array.Empty<XmlQualifiedName>();

            XmlQualifiedName[] array = new XmlQualifiedName[_namespaces.Count];
            _namespaces.Values.CopyTo(array, 0);
            return array;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Count
        {
            get { return (_namespaces == null) ? 0 : _namespaces.Count; }
        }

        internal Dictionary<string, XmlQualifiedName>.ValueCollection Namespaces => NamespacesInternal.Values;

        private Dictionary<string, XmlQualifiedName> NamespacesInternal
        {
            get
            {
                if (_namespaces == null)
                    _namespaces = new Dictionary<string, XmlQualifiedName>();
                return _namespaces;
            }
        }

        internal ArrayList? NamespaceList
        {
            get
            {
                if (_namespaces == null || _namespaces.Count == 0)
                    return null;

                return new ArrayList(_namespaces.Values);
            }
        }

        internal bool TryLookupPrefix(string? ns, out string? prefix)
        {
            prefix = null;

            if (_namespaces == null || _namespaces.Count == 0 || string.IsNullOrEmpty(ns))
                return false;

            foreach (var nsPair in _namespaces)
            {
                if (!string.IsNullOrEmpty(nsPair.Key) && nsPair.Value.Namespace == ns)
                {
                    prefix = nsPair.Key;
                    return true;
                }
            }
            return false;
        }

        internal bool TryLookupNamespace(string? prefix, out string? ns)
        {
            ns = null;

            if (_namespaces == null || _namespaces.Count == 0 || string.IsNullOrEmpty(prefix))
                return false;

            if (_namespaces.TryGetValue(prefix, out XmlQualifiedName? qName))
            {
                ns = qName.Namespace;
                return true;
            }

            return false;
        }
    }
}
