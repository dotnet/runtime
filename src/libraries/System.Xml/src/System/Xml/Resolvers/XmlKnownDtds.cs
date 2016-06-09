// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Resolvers
{
    // 
    // XmlPreloadedResolver is an XmlResolver that which can be pre-loaded with data.
    // By default it contains well-known DTDs for XHTML 1.0 and RSS 0.91. 
    // Custom mappings of URIs to data can be added with the Add method.
    //
    [Flags]
    public enum XmlKnownDtds
    {
        None = 0x00,
        Xhtml10 = 0x01,
        Rss091 = 0x02,
        All = 0xFFFF,
    }
}
