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

        var expectedXml = WithXmlHeader(@$"<BaseIXmlSerializable xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""DerivedIXmlSerializable"" AttributeString=""derivedIXmlSerTest"" DateTimeValue=""12/31/1999 12:00:00 AM"" BoolValue=""True"" xmlns=""{XmlSerializableBaseClass.XmlNamespace}"" />");
        var fromBase = SerializeAndDeserialize(dClass, expectedXml, () => new XmlSerializer(typeof(XmlSerializableBaseClass), new Type[] { typeof(XmlSerializableDerivedClass) }));
        Assert.Equal(dClass.AttributeString, fromBase.AttributeString);
        Assert.StrictEqual(dClass.DateTimeValue, fromBase.DateTimeValue);
        Assert.Equal(dClass.BoolValue, fromBase.BoolValue);

        // Derived class does not apply XmlRoot attribute to force itself to be emitted with the base class element name, so update expected xml accordingly.
        // Since we can't smartly emit xsi:type during serialization though, it is still there even thout it isn't needed.
        expectedXml = WithXmlHeader(@"<DerivedIXmlSerializable xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""DerivedIXmlSerializable"" AttributeString=""derivedIXmlSerTest"" DateTimeValue=""12/31/1999 12:00:00 AM"" BoolValue=""True"" />");
        var fromDerived = SerializeAndDeserialize(dClass, expectedXml, () => new XmlSerializer(typeof(XmlSerializableDerivedClass)));
        Assert.Equal(dClass.AttributeString, fromDerived.AttributeString);
        Assert.StrictEqual(dClass.DateTimeValue, fromDerived.DateTimeValue);
        Assert.Equal(dClass.BoolValue, fromDerived.BoolValue);
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

    // TODO - Move this test to XmlSerializerTests.RuntimeOnly.cs once #1399 is fixed for the ReflectionOnly serializer.
    [Fact]
    public static void Xml_XmlSchema()
    {
        var expectedXml = WithXmlHeader("<xsd:schema xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" elementFormDefault=\"qualified\" targetNamespace=\"http://example.com/my-schema\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <xsd:element name=\"MyElement\" type=\"xsd:string\" />\r\n  <xsd:group name=\"MyGroup\">\r\n    <xsd:sequence>\r\n      <xsd:element name=\"Item1\" />\r\n      <xsd:element name=\"Item2\" />\r\n    </xsd:sequence>\r\n  </xsd:group>\r\n</xsd:schema>");

        XmlSchema schema = new XmlSchema
        {
            TargetNamespace = "http://example.com/my-schema",
            ElementFormDefault = XmlSchemaForm.Qualified
        };
        schema.Items.Add(new XmlSchemaElement
        {
            Name = "MyElement",
            SchemaTypeName = new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema")
        });
        schema.Items.Add(new XmlSchemaGroup
        {
            Name = "MyGroup",
            Particle = new XmlSchemaSequence
            {
                Items = { new XmlSchemaElement { Name = "Item1" }, new XmlSchemaElement { Name = "Item2" } }
            }
        });
        schema.Namespaces.Add("xsd", "http://www.w3.org/2001/XMLSchema");
        schema.Namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");

        var actual = SerializeAndDeserialize(schema, expectedXml, () => new XmlSerializer(typeof(XmlSchema)));

        Assert.Equivalent(schema, actual);
    }

}
