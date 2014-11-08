// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
