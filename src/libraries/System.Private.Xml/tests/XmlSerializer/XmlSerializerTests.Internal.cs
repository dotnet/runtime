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

    // Move this test to XmlSerializerTests.cs once #1401 is fixed for the ReflectionOnly serializer.
    [Fact]
    public static void Xml_DerivedIXmlSerializable()
    {
        var dClass = new XmlSerializableDerivedClass() { AttributeString = "derivedIXmlSerTest", DateTimeValue = DateTime.Parse("Dec 31, 1999"), BoolValue = true };

        var expectedXml = WithXmlHeader(@$"<BaseIXmlSerializable xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""DerivedIXmlSerializable"" AttributeString=""derivedIXmlSerTest"" DateTimeValue=""1999-12-31T00:00:00"" BoolValue=""True"" xmlns=""{XmlSerializableBaseClass.XmlNamespace}"" />");
        var fromBase = SerializeAndDeserialize(dClass, expectedXml, () => new XmlSerializer(typeof(XmlSerializableBaseClass), new Type[] { typeof(XmlSerializableDerivedClass) }));
        Assert.Equal(dClass.AttributeString, fromBase.AttributeString);
        Assert.Equal(dClass.DateTimeValue, fromBase.DateTimeValue);
        Assert.Equal(dClass.BoolValue, fromBase.BoolValue);

        // Derived class does not apply XmlRoot attribute to force itself to be emitted with the base class element name, so update expected xml accordingly.
        // Since we can't smartly emit xsi:type during serialization though, it is still there even though it isn't needed.
        expectedXml = WithXmlHeader(@"<DerivedIXmlSerializable xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""DerivedIXmlSerializable"" AttributeString=""derivedIXmlSerTest"" DateTimeValue=""1999-12-31T00:00:00"" BoolValue=""True"" />");
        var fromDerived = SerializeAndDeserialize(dClass, expectedXml, () => new XmlSerializer(typeof(XmlSerializableDerivedClass)));
        Assert.Equal(dClass.AttributeString, fromDerived.AttributeString);
        Assert.Equal(dClass.DateTimeValue, fromDerived.DateTimeValue);
        Assert.Equal(dClass.BoolValue, fromDerived.BoolValue);
    }
}
