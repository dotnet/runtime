// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections;

namespace System.Security.Cryptography.Xml
{
    // This class does lexicographic sorting by NamespaceURI first and then by LocalName.
    internal class AttributeSortOrder : IComparer
    {
        internal AttributeSortOrder() { }

        public int Compare(object a, object b)
        {
            XmlNode nodeA = a as XmlNode;
            XmlNode nodeB = b as XmlNode;
            if ((nodeA == null) || (nodeB == null))
                throw new ArgumentException();
            int namespaceCompare = string.CompareOrdinal(nodeA.NamespaceURI, nodeB.NamespaceURI);
            if (namespaceCompare != 0) return namespaceCompare;
            return string.CompareOrdinal(nodeA.LocalName, nodeB.LocalName);
        }
    }
}
