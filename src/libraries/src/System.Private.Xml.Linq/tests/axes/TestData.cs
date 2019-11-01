// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Linq.Tests
{
    public static class TestData
    {
        public static XDocument GetDocumentWithContacts()
        {
            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XProcessingInstruction("AppName", "Processing Instruction Data"),
                new XComment("Personal Contacts"),
                new XElement("contacts",
                    new XAttribute("category", "friends"),
                    new XAttribute("gender", "male"),
                     new XElement("contact",
                        new XAttribute("netWorth", "100"),
                        new XElement("name", "John Hopkins"),
                        new XElement("phone",
                            new XAttribute("type", "home"),
                            "214-459-8907"),
                        new XElement("phone",
                            new XAttribute("type", "work"),
                            "817-283-9670")),
                    new XElement("contact",
                        new XAttribute("netWorth", "10"),
                        new XElement("name", "Patrick Hines"),
                        new XElement("phone",
                            new XAttribute("type", "home"),
                            "206-555-0144"),
                        new XElement("phone",
                            new XAttribute("type", "work"),
                            "425-555-0145"))));

            return doc;
        }
    }
}
