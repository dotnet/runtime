// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.Xml.Schema
{
    internal sealed class SchemaNamespaceManager : XmlNamespaceManager
    {
        private readonly XmlSchemaObject _node;

        public SchemaNamespaceManager(XmlSchemaObject node)
        {
            _node = node;
        }

        public override string? LookupNamespace(string prefix)
        {
            if (prefix == "xml")
            { //Special case for the XML namespace
                return XmlReservedNs.NsXml;
            }
            for (XmlSchemaObject? current = _node; current != null; current = current.Parent)
            {
                if (current.Namespaces.TryLookupNamespace(prefix, out string? uri))
                    return uri;
            }
            return prefix.Length == 0 ? string.Empty : null;
        }

        public override string? LookupPrefix(string ns)
        {
            if (ns == XmlReservedNs.NsXml)
            { //Special case for the XML namespace
                return "xml";
            }

            for (XmlSchemaObject? current = _node; current != null; current = current.Parent)
            {
                if (current.Namespaces.TryLookupPrefix(ns, out string? prefix))
                    return prefix;
            }
            return null;
        }
    }; //SchemaNamespaceManager
}
