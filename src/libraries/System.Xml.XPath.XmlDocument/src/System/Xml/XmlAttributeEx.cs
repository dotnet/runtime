// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    internal static class XmlAttributeEx
    {
        public static bool IsNamespace(this XmlAttribute attribute)
        {
            return Ref.Equal(attribute.NamespaceURI, XmlConst.ReservedNsXmlNs);
        }
    }
}
