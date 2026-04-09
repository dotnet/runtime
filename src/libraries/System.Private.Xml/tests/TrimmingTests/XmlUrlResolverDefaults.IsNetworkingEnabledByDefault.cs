// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

class XmlUrlResolverDefaults
{
    public static int Main()
    {
        File.WriteAllText("file.xml", """
            <?xml version="1.0" encoding="utf-8" ?>
            <root>
              <some-element>test-value</some-element>
            </root>
            """);

        XDocument doc = XDocument.Load("file.xml");

        string value = doc.Descendants("some-element").Single().Value;
        if (value == "test-value")
        {
            Type? urlResolver = GetXmlType("System.Xml.XmlUrlResolver");
            if (urlResolver != null)
            {
                // we should preserve the type but we want to avoid doing web requests during the test
                return 100;
            }

            return -1;
        }

        return -2;
    }

    // The intention of this method is to ensure the trimmer preserves the Type.
    private static Type? GetXmlType(string name) =>
        typeof(XmlReader).Assembly.GetType(name, throwOnError: false);
}
