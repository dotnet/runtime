// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    // NOTE: Absolute numbering is utilized in DtdParser. -HelenaK
    public enum XmlTokenizedType
    {
        CDATA = 0,
        ID = 1,
        IDREF = 2,
        IDREFS = 3,
        ENTITY = 4,
        ENTITIES = 5,
        NMTOKEN = 6,
        NMTOKENS = 7,
        NOTATION = 8,
        ENUMERATION = 9,
        QName = 10,
        NCName = 11,
        None = 12
    }
}
