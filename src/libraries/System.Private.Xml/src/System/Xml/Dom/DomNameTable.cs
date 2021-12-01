// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Xml.Schema;

namespace System.Xml
{
    internal sealed class DomNameTable
    {
        private XmlName[] _entries;
        private int _count;
        private int _mask;
        private readonly XmlDocument _ownerDocument;
        private readonly XmlNameTable _nameTable;

        private const int InitialSize = 64; // must be a power of two

        public DomNameTable(XmlDocument document)
        {
            _ownerDocument = document;
            _nameTable = document.NameTable;
            _entries = new XmlName[InitialSize];
            _mask = InitialSize - 1;
            Debug.Assert((_entries.Length & _mask) == 0);  // entries.Length must be a power of two
        }

        public XmlName? GetName(string? prefix, string localName, string? ns, IXmlSchemaInfo? schemaInfo)
        {
            if (prefix == null)
            {
                prefix = string.Empty;
            }
            if (ns == null)
            {
                ns = string.Empty;
            }

            int hashCode = XmlName.GetHashCode(localName);

            for (XmlName e = _entries[hashCode & _mask]; e != null; e = e.next)
            {
                if (e.HashCode == hashCode
                    && ((object)e.LocalName == (object)localName
                        || e.LocalName.Equals(localName))
                    && ((object)e.Prefix == (object)prefix
                        || e.Prefix.Equals(prefix))
                    && ((object)e.NamespaceURI == (object)ns
                        || e.NamespaceURI.Equals(ns))
                    && e.Equals(schemaInfo))
                {
                    return e;
                }
            }
            return null;
        }

        public XmlName AddName(string? prefix, string localName, string? ns, IXmlSchemaInfo? schemaInfo)
        {
            if (prefix == null)
            {
                prefix = string.Empty;
            }
            if (ns == null)
            {
                ns = string.Empty;
            }

            int hashCode = XmlName.GetHashCode(localName);

            for (XmlName e = _entries[hashCode & _mask]; e != null; e = e.next)
            {
                if (e.HashCode == hashCode
                    && ((object)e.LocalName == (object)localName
                        || e.LocalName.Equals(localName))
                    && ((object)e.Prefix == (object)prefix
                        || e.Prefix.Equals(prefix))
                    && ((object)e.NamespaceURI == (object)ns
                        || e.NamespaceURI.Equals(ns))
                    && e.Equals(schemaInfo))
                {
                    return e;
                }
            }

            prefix = _nameTable.Add(prefix);
            localName = _nameTable.Add(localName);
            ns = _nameTable.Add(ns);
            int index = hashCode & _mask;
            XmlName name = XmlName.Create(prefix, localName, ns, hashCode, _ownerDocument, _entries[index], schemaInfo);
            _entries[index] = name;

            if (_count++ == _mask)
            {
                Grow();
            }

            return name;
        }

        private void Grow()
        {
            int newMask = _mask * 2 + 1;
            XmlName[] oldEntries = _entries;
            XmlName[] newEntries = new XmlName[newMask + 1];

            // use oldEntries.Length to eliminate the range check
            for (int i = 0; i < oldEntries.Length; i++)
            {
                XmlName name = oldEntries[i];
                while (name != null)
                {
                    int newIndex = name.HashCode & newMask;
                    XmlName tmp = name.next;
                    name.next = newEntries[newIndex];
                    newEntries[newIndex] = name;
                    name = tmp;
                }
            }

            _entries = newEntries;
            _mask = newMask;
        }
    }
}
