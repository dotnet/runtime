// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Xml.Tests
{
    public class TestData
    {
        internal static string _Root = Path.Combine("TestFiles", "TestData");
        internal static string StandardPath = Path.Combine("TestFiles", "StandardTests");
        internal static string _FileXSD1 = Path.Combine(_Root, "schema1.xsd");
        internal static string _FileXSD1bis = Path.Combine(_Root, "schema1bis.xsd");
        internal static string _NmspXSD1 = Path.Combine(_Root, "schema1.xsd");

        internal static string _FileXSD2 = Path.Combine(_Root, "schema2.xsd");
        internal static string _NmspXSD2 = Path.Combine(_Root, "schema2.xsd");

        internal static string _XsdAuthor = Path.Combine(_Root, "xsdauthor.xsd");
        internal static string _XsdAuthorNoNs = Path.Combine(_Root, "xsdauthor_nons.xsd"); // No targetNS
        internal static string _XsdAuthorDup = Path.Combine(_Root, "xsdauthor_dup.xsd");  // Colliding xsd
        internal static string _XsdPrice = Path.Combine(_Root, "xsdprice.xsd");
        internal static string _XsdBookExternal = Path.Combine(_Root, "xsdbookexternal.xsd");
        internal static string _NmspBook = Path.Combine(_Root, "xsdbook");

        internal static string _XsdError = Path.Combine(_Root, "xsderror.xsd");
        internal static string _XsdError2 = Path.Combine(_Root, "xsderror2.xsd");
        internal static string _NmspError = Path.Combine(_Root, "xsderror");
        internal static string _XdrError = Path.Combine(_Root, "xdrerror.xdr");

        internal static string _XsdNoNs = Path.Combine(_Root, "nons.xsd");

        internal static string _SchemaXdr = Path.Combine(_Root, "schema1.xdr");
    }
}
