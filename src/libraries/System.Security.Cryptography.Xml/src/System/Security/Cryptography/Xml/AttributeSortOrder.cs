// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    // This class does lexicographic sorting by NamespaceURI first and then by LocalName.
    internal sealed class AttributeSortOrder : IComparer
    {
        internal AttributeSortOrder() { }

        public int Compare(object? a, object? b)
        {
            XmlNode? nodeA = a as XmlNode;
            XmlNode? nodeB = b as XmlNode;
            if ((nodeA == null) || (nodeB == null))
                throw new ArgumentException();
            int namespaceCompare = string.CompareOrdinal(nodeA.NamespaceURI, nodeB.NamespaceURI);
            if (namespaceCompare != 0) return namespaceCompare;
            return string.CompareOrdinal(nodeA.LocalName, nodeB.LocalName);
        }
    }
}
