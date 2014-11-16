// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Xml
{
    internal class DomNameTable
    {
        XmlName[] entries;
        int count;
        int mask;
        XmlDocument ownerDocument;
        XmlNameTable nameTable;

        const int InitialSize = 64; // must be a power of two

        public DomNameTable(XmlDocument document)
        {
            ownerDocument = document;
            nameTable = document.NameTable;
            entries = new XmlName[InitialSize];
            mask = InitialSize - 1;
            Debug.Assert((entries.Length & mask) == 0);  // entries.Length must be a power of two
        }

        public XmlName GetName(string prefix, string localName, string ns)
        {
            if (prefix == null)
            {
                prefix = string.Empty;
            }
            if (ns == null)
            {
                ns = string.Empty;
            }

            int hashCode = XmlNameHelper.GetHashCode(localName);

            for (XmlName e = entries[hashCode & mask]; e != null; e = e.next)
            {
                if (e.HashCode == hashCode
                    && ((object)e.LocalName == (object)localName
                        || e.LocalName.Equals(localName))
                    && ((object)e.Prefix == (object)prefix
                        || e.Prefix.Equals(prefix))
                    && ((object)e.NamespaceURI == (object)ns
                        || e.NamespaceURI.Equals(ns)))
                {
                    return e;
                }
            }
            return null;
        }

        public XmlName AddName(string prefix, string localName, string ns)
        {
            if (prefix == null)
            {
                prefix = string.Empty;
            }
            if (ns == null)
            {
                ns = string.Empty;
            }

            int hashCode = XmlNameHelper.GetHashCode(localName);

            for (XmlName e = entries[hashCode & mask]; e != null; e = e.next)
            {
                if (e.HashCode == hashCode
                    && ((object)e.LocalName == (object)localName
                        || e.LocalName.Equals(localName))
                    && ((object)e.Prefix == (object)prefix
                        || e.Prefix.Equals(prefix))
                    && ((object)e.NamespaceURI == (object)ns
                        || e.NamespaceURI.Equals(ns)))
                {
                    return e;
                }
            }

            prefix = nameTable.Add(prefix);
            localName = nameTable.Add(localName);
            ns = nameTable.Add(ns);
            int index = hashCode & mask;
            XmlName name = XmlName.Create(prefix, localName, ns, hashCode, ownerDocument, entries[index]);
            entries[index] = name;

            if (count++ == mask)
            {
                Grow();
            }

            return name;
        }

        private void Grow()
        {
            int newMask = mask * 2 + 1;
            XmlName[] oldEntries = entries;
            XmlName[] newEntries = new XmlName[newMask + 1];

            // use oldEntries.Length to eliminate the rangecheck            
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
            entries = newEntries;
            mask = newMask;
        }
    }
}
