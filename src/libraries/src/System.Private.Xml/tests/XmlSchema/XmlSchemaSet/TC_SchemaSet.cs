// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Tests
{
    public class TestData
    {
        internal static string _Root = @"TestFiles\TestData\";
        internal static string StandardPath = @"TestFiles\StandardTests\";
        internal static string _FileXSD1 = _Root + "schema1.xsd";
        internal static string _FileXSD1bis = _Root + "schema1bis.xsd";
        internal static string _NmspXSD1 = _Root + "schema1.xsd";

        internal static string _FileXSD2 = _Root + "schema2.xsd";
        internal static string _NmspXSD2 = _Root + "schema2.xsd";

        internal static string _XsdAuthor = _Root + "xsdauthor.xsd";
        internal static string _XsdAuthorNoNs = _Root + "xsdauthor_nons.xsd"; // No targetNS
        internal static string _XsdAuthorDup = _Root + "xsdauthor_dup.xsd";  // Colliding xsd
        internal static string _XsdPrice = _Root + "xsdprice.xsd";
        internal static string _XsdBookExternal = _Root + "xsdbookexternal.xsd";
        internal static string _NmspBook = _Root + "xsdbook";

        internal static string _XsdError = _Root + "xsderror.xsd";
        internal static string _XsdError2 = _Root + "xsderror2.xsd";
        internal static string _NmspError = _Root + "xsderror";
        internal static string _XdrError = _Root + "xdrerror.xdr";

        internal static string _XsdNoNs = _Root + "nons.xsd";

        internal static string _SchemaXdr = _Root + "schema1.xdr";
    }
}