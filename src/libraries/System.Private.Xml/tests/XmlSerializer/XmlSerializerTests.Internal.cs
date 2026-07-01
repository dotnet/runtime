// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using SerializationTypes;
using Xunit;

public static partial class XmlSerializerTests
{
    // Move this test to XmlSerializerTests.cs once #1398 is fixed for the ReflectionOnly serializer.
    [Fact]
    public static void Xml_CustomDocumentWithXmlAttributesAsNodes()
    {
        var customDoc = new CustomDocument();
        var customElement = new CustomElement() { Name = "testElement" };
        customElement.AddAttribute(customDoc.CreateAttribute("regularAttribute", "regularValue"));
        customElement.AddAttribute(customDoc.CreateCustomAttribute("customAttribute", "customValue"));
        customDoc.CustomItems.Add(customElement);
        var element = customDoc.Document.CreateElement("regularElement");
        var innerElement = customDoc.Document.CreateElement("innerElement");
        innerElement.InnerXml = "<leafElement>innerText</leafElement>";
        element.InnerText = "regularText";
        element.AppendChild(innerElement);
        element.Attributes.Append(customDoc.CreateAttribute("regularElementAttribute", "regularElementAttributeValue"));
        customDoc.AddItem(element);
        var actual = SerializeAndDeserialize(customDoc,
            WithXmlHeader(@"<customElement name=""testElement"" regularAttribute=""regularValue"" customAttribute=""customValue""/>"), skipStringCompare: true);
        Assert.NotNull(actual);
    }

    // Move this test to XmlSerializerTests.cs once #1402 is fixed for the ReflectionOnly serializer.
    // Actually, this test is already there, but it's commented out. Uncomment it once #1402 is fixed.
    // BTW, there are multiple (4?) places in the refelction reader where this issue is referenced, although
    // one of those places is a potential optimization that is not required to get on par with ILGen afaik.
    [Fact]
    public static void XML_TypeWithFieldsOrdered()
    {
        var value = new TypeWithFieldsOrdered()
        {
            IntField1 = 1,
            IntField2 = 2,
            StringField1 = "foo1",
            StringField2 = "foo2"
        };

        var actual = SerializeAndDeserialize(value, WithXmlHeader("<TypeWithFieldsOrdered xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <IntField2>2</IntField2>\r\n  <IntField1>1</IntField1>\r\n  <strfld>foo2</strfld>\r\n  <strfld>foo1</strfld>\r\n</TypeWithFieldsOrdered>"));

        Assert.NotNull(actual);
        Assert.Equal(value.IntField1, actual.IntField1);
        Assert.Equal(value.IntField2, actual.IntField2);
        Assert.Equal(value.StringField1, actual.StringField1);
        Assert.Equal(value.StringField2, actual.StringField2);
    }
}
