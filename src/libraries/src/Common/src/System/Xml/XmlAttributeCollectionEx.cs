// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    internal static class XmlAttributeCollectionEx
    {
        public static int FindNodeOffsetNS(this XmlAttributeCollection collection, XmlAttribute node)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                XmlAttribute tmp = collection[i];
                if (tmp.LocalName == node.LocalName
                    && tmp.NamespaceURI == node.NamespaceURI)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
