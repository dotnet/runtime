// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System;
    using System.Diagnostics;
    using System.Collections;
    using System.Collections.Generic;

    internal class SchemaNamespaceManager : XmlNamespaceManager
    {
        private XmlSchemaObject _node;

        public SchemaNamespaceManager(XmlSchemaObject node)
        {
            _node = node;
        }

        public override string LookupNamespace(string prefix)
        {
            if (prefix == "xml")
            { //Special case for the XML namespace
                return XmlReservedNs.NsXml;
            }
            Dictionary<string, string> namespaces;
            for (XmlSchemaObject current = _node; current != null; current = current.Parent)
            {
                namespaces = current.Namespaces.Namespaces;
                if (namespaces != null && namespaces.Count > 0)
                {
                    string uri;
                    if (namespaces.TryGetValue(prefix, out uri))
                        return uri;
                }
            }
            return prefix.Length == 0 ? string.Empty : null;
        }

        public override string LookupPrefix(string ns)
        {
            if (ns == XmlReservedNs.NsXml)
            { //Special case for the XML namespace
                return "xml";
            }
            Dictionary<string, string> namespaces;
            for (XmlSchemaObject current = _node; current != null; current = current.Parent)
            {
                namespaces = current.Namespaces.Namespaces;
                if (namespaces != null && namespaces.Count > 0)
                {
                    foreach (KeyValuePair<string, string> entry in namespaces)
                    {
                        if (entry.Value.Equals(ns))
                        {
                            return entry.Key;
                        }
                    }
                }
            }
            return null;
        }
    }; //SchemaNamespaceManager
}
