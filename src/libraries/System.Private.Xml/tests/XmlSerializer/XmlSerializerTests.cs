// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SerializationTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Xunit;

public static partial class XmlSerializerTests
{
#if ReflectionOnly|| XMLSERIALIZERGENERATORTESTS
    private static readonly string SerializationModeSetterName = "set_Mode";

    static XmlSerializerTests()
    {
        if (!PlatformDetection.IsFullFramework)
        {
            MethodInfo method = typeof(XmlSerializer).GetMethod(SerializationModeSetterName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.True(method != null, $"No method named {SerializationModeSetterName}");
#if ReflectionOnly
            method.Invoke(null, new object[] { 1 });
#endif
#if XMLSERIALIZERGENERATORTESTS
            method.Invoke(null, new object[] { 3 });
#endif
        }
    }
#endif

    private static bool IsTimeSpanSerializationAvailable => !PlatformDetection.IsFullFramework || (AppContext.TryGetSwitch("Switch.System.Xml.EnableTimeSpanSerialization", out bool result) && result);

    [Fact]
    public static void Xml_TypeWithDateTimePropertyAsXmlTime()
    {
        DateTime localTime = new DateTime(549269870000L, DateTimeKind.Local);
        TypeWithDateTimePropertyAsXmlTime localTimeOjbect = new TypeWithDateTimePropertyAsXmlTime()
        {
            Value = localTime
        };

        // This is how we convert DateTime from time to string.
        var localTimeDateTime = DateTime.MinValue + localTime.TimeOfDay;
        string localTimeString = localTimeDateTime.ToString("HH:mm:ss.fffffffzzzzzz", DateTimeFormatInfo.InvariantInfo);
        TypeWithDateTimePropertyAsXmlTime localTimeOjbectRoundTrip = SerializeAndDeserialize(localTimeOjbect,
string.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<TypeWithDateTimePropertyAsXmlTime xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">{0}</TypeWithDateTimePropertyAsXmlTime>", localTimeString));

        Assert.StrictEqual(localTimeOjbect.Value, localTimeOjbectRoundTrip.Value);

        TypeWithDateTimePropertyAsXmlTime utcTimeOjbect = new TypeWithDateTimePropertyAsXmlTime()
        {
            Value = new DateTime(549269870000L, DateTimeKind.Utc)
        };

        if (IsTimeSpanSerializationAvailable)
        {
            TypeWithDateTimePropertyAsXmlTime utcTimeRoundTrip = SerializeAndDeserialize(utcTimeOjbect,
    @"<?xml version=""1.0"" encoding=""utf-8""?>
<TypeWithDateTimePropertyAsXmlTime xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">15:15:26.9870000Z</TypeWithDateTimePropertyAsXmlTime>");

            Assert.StrictEqual(utcTimeOjbect.Value, utcTimeRoundTrip.Value);
        }
    }

    [Fact]
    public static void Xml_ArrayAsGetSet()
    {
        TypeWithGetSetArrayMembers x = new TypeWithGetSetArrayMembers
        {
            F1 = new SimpleType[] { new SimpleType { P1 = "ab", P2 = 1 }, new SimpleType { P1 = "cd", P2 = 2 } },
            F2 = new int[] { -1, 3 },
            P1 = new SimpleType[] { new SimpleType { P1 = "ef", P2 = 5 }, new SimpleType { P1 = "gh", P2 = 7 } },
            P2 = new int[] { 11, 12 }
        };
        TypeWithGetSetArrayMembers y = SerializeAndDeserialize<TypeWithGetSetArrayMembers>(x,
@"<?xml version=""1.0""?>
<TypeWithGetSetArrayMembers xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <F1>
    <SimpleType>
      <P1>ab</P1>
      <P2>1</P2>
    </SimpleType>
    <SimpleType>
      <P1>cd</P1>
      <P2>2</P2>
    </SimpleType>
  </F1>
  <F2>
    <int>-1</int>
    <int>3</int>
  </F2>
  <P1>
    <SimpleType>
      <P1>ef</P1>
      <P2>5</P2>
    </SimpleType>
    <SimpleType>
      <P1>gh</P1>
      <P2>7</P2>
    </SimpleType>
  </P1>
  <P2>
    <int>11</int>
    <int>12</int>
  </P2>
</TypeWithGetSetArrayMembers>");

        Assert.NotNull(y);
        Utils.Equal<SimpleType>(x.F1, y.F1, (a, b) => { return SimpleType.AreEqual(a, b); });
        Assert.Equal(x.F2, y.F2);
        Utils.Equal<SimpleType>(x.P1, y.P1, (a, b) => { return SimpleType.AreEqual(a, b); });
        Assert.Equal(x.P2, y.P2);
    }

    [Fact]
    public static void Xml_ArrayAsGetOnly()
    {
        TypeWithGetOnlyArrayProperties x = new TypeWithGetOnlyArrayProperties();
        x.P1[0] = new SimpleType { P1 = "ab", P2 = 1 };
        x.P1[1] = new SimpleType { P1 = "cd", P2 = 2 };
        x.P2[0] = -1;
        x.P2[1] = 3;

        TypeWithGetOnlyArrayProperties y = SerializeAndDeserialize<TypeWithGetOnlyArrayProperties>(x,
@"<?xml version=""1.0""?>
<TypeWithGetOnlyArrayProperties xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" />");

        Assert.NotNull(y);
        // XmlSerializer seems not complain about missing public setter of Array property
        // However, it does not serialize the property. So for this test case, I'll use it to verify there are no complaints about missing public setter
    }

    [Fact]
    public static void Xml_ListRoot()
    {
        MyList x = new MyList("a1", "a2");
        MyList y = SerializeAndDeserialize<MyList>(x,
@"<?xml version=""1.0""?>
<ArrayOfAnyType xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <anyType xsi:type=""xsd:string"">a1</anyType>
  <anyType xsi:type=""xsd:string"">a2</anyType>
</ArrayOfAnyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.StrictEqual((string)x[0], (string)y[0]);
        Assert.StrictEqual((string)x[1], (string)y[1]);
    }

    [Fact]
    public static void Xml_EnumAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<MyEnum>(MyEnum.Two,
@"<?xml version=""1.0""?>
<MyEnum>Two</MyEnum>"), MyEnum.Two);
        Assert.StrictEqual(SerializeAndDeserialize<ByteEnum>(ByteEnum.Option1,
@"<?xml version=""1.0""?>
<ByteEnum>Option1</ByteEnum>"), ByteEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<SByteEnum>(SByteEnum.Option1,
@"<?xml version=""1.0""?>
<SByteEnum>Option1</SByteEnum>"), SByteEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<ShortEnum>(ShortEnum.Option1,
@"<?xml version=""1.0""?>
<ShortEnum>Option1</ShortEnum>"), ShortEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<IntEnum>(IntEnum.Option1,
@"<?xml version=""1.0""?>
<IntEnum>Option1</IntEnum>"), IntEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<UIntEnum>(UIntEnum.Option1,
@"<?xml version=""1.0""?>
<UIntEnum>Option1</UIntEnum>"), UIntEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<LongEnum>(LongEnum.Option1,
@"<?xml version=""1.0""?>
<LongEnum>Option1</LongEnum>"), LongEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<ULongEnum>(ULongEnum.Option1,
@"<?xml version=""1.0""?>
<ULongEnum>Option1</ULongEnum>"), ULongEnum.Option1);
    }

    [Fact]
    public static void Xml_EnumAsMember()
    {
        TypeWithEnumMembers x = new TypeWithEnumMembers { F1 = MyEnum.Three, P1 = MyEnum.Two };
        TypeWithEnumMembers y = SerializeAndDeserialize<TypeWithEnumMembers>(x,
@"<?xml version=""1.0""?>
<TypeWithEnumMembers xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <F1>Three</F1>
  <P1>Two</P1>
</TypeWithEnumMembers>");

        Assert.NotNull(y);
        Assert.StrictEqual(x.F1, y.F1);
        Assert.StrictEqual(x.P1, y.P1);
    }

    [Fact]
    public static void Xml_DCClassWithEnumAndStruct()
    {
        DCClassWithEnumAndStruct value = new DCClassWithEnumAndStruct(true);
        DCClassWithEnumAndStruct actual = SerializeAndDeserialize<DCClassWithEnumAndStruct>(value,
@"<?xml version=""1.0""?>
<DCClassWithEnumAndStruct xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <MyStruct>
    <Data>Data</Data>
  </MyStruct>
  <MyEnum1>One</MyEnum1>
</DCClassWithEnumAndStruct>");

        Assert.StrictEqual(value.MyEnum1, actual.MyEnum1);
        Assert.StrictEqual(value.MyStruct.Data, actual.MyStruct.Data);
    }

    [Fact]
    public static void Xml_BuiltInTypes()
    {
        BuiltInTypes x = new BuiltInTypes
        {
            ByteArray = new byte[] { 1, 2 }
        };
        BuiltInTypes y = SerializeAndDeserialize<BuiltInTypes>(x,
@"<?xml version=""1.0""?>
<BuiltInTypes xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <ByteArray>AQI=</ByteArray>
</BuiltInTypes>");

        Assert.NotNull(y);
        Assert.Equal(x.ByteArray, y.ByteArray);
    }

    [Fact]
    public static void Xml_TypesWithArrayOfOtherTypes()
    {
        SerializeAndDeserialize<TypeHasArrayOfASerializedAsB>(new TypeHasArrayOfASerializedAsB(true),
@"<?xml version=""1.0""?>
<TypeHasArrayOfASerializedAsB xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Items>
    <TypeA>
      <Name>typeAValue</Name>
    </TypeA>
    <TypeA>
      <Name>typeBValue</Name>
    </TypeA>
  </Items>
</TypeHasArrayOfASerializedAsB>");
    }

    [Fact]
    public static void Xml_TypeNamesWithSpecialCharacters()
    {
        SerializeAndDeserialize<__TypeNameWithSpecialCharacters漢ñ>(new __TypeNameWithSpecialCharacters漢ñ() { PropertyNameWithSpecialCharacters漢ñ = "Test" },
@"<?xml version=""1.0""?>
<__TypeNameWithSpecialCharacters漢ñ xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <PropertyNameWithSpecialCharacters漢ñ>Test</PropertyNameWithSpecialCharacters漢ñ>
</__TypeNameWithSpecialCharacters漢ñ>");
    }

    [Fact]
    public static void Xml_KnownTypesThroughConstructor()
    {
        KnownTypesThroughConstructor value = new KnownTypesThroughConstructor() { EnumValue = MyEnum.One, SimpleTypeValue = new SimpleKnownTypeValue() { StrProperty = "PropertyValue" } };
        KnownTypesThroughConstructor actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value,
@"<?xml version=""1.0""?>
<KnownTypesThroughConstructor xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <EnumValue xsi:type=""MyEnum"">One</EnumValue>
  <SimpleTypeValue xsi:type=""SimpleKnownTypeValue"">
    <StrProperty>PropertyValue</StrProperty>
  </SimpleTypeValue>
</KnownTypesThroughConstructor>",
            () => { return new XmlSerializer(typeof(KnownTypesThroughConstructor), new Type[] { typeof(MyEnum), typeof(SimpleKnownTypeValue) }); });

        Assert.StrictEqual((MyEnum)value.EnumValue, (MyEnum)actual.EnumValue);
        Assert.StrictEqual(((SimpleKnownTypeValue)value.SimpleTypeValue).StrProperty, ((SimpleKnownTypeValue)actual.SimpleTypeValue).StrProperty);
    }

    [Fact]
    public static void Xml_BaseClassAndDerivedClassWithSameProperty()
    {
        DerivedClassWithSameProperty value = new DerivedClassWithSameProperty() { DateTimeProperty = new DateTime(100), IntProperty = 5, StringProperty = "TestString", ListProperty = new List<string>() };
        value.ListProperty.AddRange(new string[] { "one", "two", "three" });

        DerivedClassWithSameProperty actual = SerializeAndDeserialize<DerivedClassWithSameProperty>(value,
@"<?xml version=""1.0""?>
<DerivedClassWithSameProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <StringProperty>TestString</StringProperty>
  <IntProperty>5</IntProperty>
  <DateTimeProperty>0001-01-01T00:00:00.00001</DateTimeProperty>
  <ListProperty>
    <string>one</string>
    <string>two</string>
    <string>three</string>
  </ListProperty>
</DerivedClassWithSameProperty>");

        Assert.StrictEqual(value.DateTimeProperty, actual.DateTimeProperty);
        Assert.StrictEqual(value.IntProperty, actual.IntProperty);
        Assert.StrictEqual(value.StringProperty, actual.StringProperty);
        Assert.Equal(value.ListProperty.ToArray(), actual.ListProperty.ToArray());
    }

    [Fact]
    public static void Xml_EnumFlags()
    {
        EnumFlags value1 = EnumFlags.One | EnumFlags.Four;
        var value2 = SerializeAndDeserialize<EnumFlags>(value1,
@"<?xml version=""1.0""?>
<EnumFlags>One Four</EnumFlags>");
        Assert.StrictEqual(value1, value2);
    }

    [Fact]
    public static void Xml_SerializeClassThatImplementsInteface()
    {
        ClassImplementsInterface value = new ClassImplementsInterface() { ClassID = "ClassID", DisplayName = "DisplayName", Id = "Id", IsLoaded = true };
        ClassImplementsInterface actual = SerializeAndDeserialize<ClassImplementsInterface>(value,
@"<?xml version=""1.0""?>
<ClassImplementsInterface xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <ClassID>ClassID</ClassID>
  <DisplayName>DisplayName</DisplayName>
  <Id>Id</Id>
  <IsLoaded>true</IsLoaded>
</ClassImplementsInterface>");

        Assert.StrictEqual(value.ClassID, actual.ClassID);
        Assert.StrictEqual(value.DisplayName, actual.DisplayName);
        Assert.StrictEqual(value.Id, actual.Id);
        Assert.StrictEqual(value.IsLoaded, actual.IsLoaded);
    }


    [Fact]
    public static void Xml_XmlAttributesTest()
    {
        var value = new XmlSerializerAttributes();
        var actual = SerializeAndDeserialize(value,
@"<?xml version=""1.0""?>
<AttributeTesting xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" XmlAttributeName=""2"">
  <Word>String choice value</Word>
  <XmlIncludeProperty xsi:type=""ItemChoiceType"">DecimalNumber</XmlIncludeProperty>
  <XmlEnumProperty>
    <ItemChoiceType>DecimalNumber</ItemChoiceType>
    <ItemChoiceType>Number</ItemChoiceType>
    <ItemChoiceType>Word</ItemChoiceType>
    <ItemChoiceType>None</ItemChoiceType>
  </XmlEnumProperty>&lt;xml&gt;Hello XML&lt;/xml&gt;<XmlNamespaceDeclarationsProperty>XmlNamespaceDeclarationsPropertyValue</XmlNamespaceDeclarationsProperty><XmlElementPropertyNode xmlns=""http://element"">1</XmlElementPropertyNode><CustomXmlArrayProperty xmlns=""http://mynamespace""><string>one</string><string>two</string><string>three</string></CustomXmlArrayProperty></AttributeTesting>");

        Assert.StrictEqual(actual.EnumType, value.EnumType);
        Assert.StrictEqual(actual.MyChoice, value.MyChoice);
        object[] stringArray = actual.XmlArrayProperty.Where(x => x != null)
            .Select(x => x.ToString())
            .ToArray();
        Assert.Equal(stringArray, value.XmlArrayProperty);
        Assert.StrictEqual(actual.XmlAttributeProperty, value.XmlAttributeProperty);
        Assert.StrictEqual(actual.XmlElementProperty, value.XmlElementProperty);
        Assert.Equal(actual.XmlEnumProperty, value.XmlEnumProperty);
        Assert.StrictEqual(actual.XmlIncludeProperty, value.XmlIncludeProperty);
        Assert.StrictEqual(actual.XmlNamespaceDeclarationsProperty, value.XmlNamespaceDeclarationsProperty);
        Assert.StrictEqual(actual.XmlTextProperty, value.XmlTextProperty);
    }

    [Fact]
    public static void Xml_XmlAnyAttributeTest()
    {
        var serializer = new XmlSerializer(typeof(TypeWithAnyAttribute));
        const string format = @"<?xml version=""1.0"" encoding=""utf-8""?><TypeWithAnyAttribute xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" GroupType = '{0}' IntProperty = '{1}' GroupBase = '{2}'><Name>{3}</Name></TypeWithAnyAttribute>";
        const int intProperty = 42;
        const string attribute1 = "Technical";
        const string attribute2 = "Red";
        const string name = "MyGroup";
        using (var stream = new MemoryStream())
        {
            var writer = new StreamWriter(stream);
            writer.Write(format, attribute1, intProperty, attribute2, name);
            writer.Flush();
            stream.Position = 0;
            var obj = (TypeWithAnyAttribute)serializer.Deserialize(stream);
            Assert.NotNull(obj);
            Assert.StrictEqual(intProperty, obj.IntProperty);
            Assert.StrictEqual(name, obj.Name);
            Assert.StrictEqual(2, obj.Attributes.Length);
            Assert.StrictEqual(attribute1, obj.Attributes[0].Value);
            Assert.StrictEqual(attribute2, obj.Attributes[1].Value);
        }
    }

    [Fact]
    public static void Xml_Struct()
    {
        var value = new WithStruct { Some = new SomeStruct { A = 1, B = 2 } };
        var result = SerializeAndDeserialize(value,
@"<?xml version=""1.0""?>
<WithStruct xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Some>
    <A>1</A>
    <B>2</B>
  </Some>
</WithStruct>");

        // Assert
        Assert.StrictEqual(result.Some.A, value.Some.A);
        Assert.StrictEqual(result.Some.B, value.Some.B);
    }

    [Fact]
    public static void Xml_Enums()
    {
        var item = new WithEnums() { Int = IntEnum.Option1, Short = ShortEnum.Option2 };
        var actual = SerializeAndDeserialize(item,
@"<?xml version=""1.0""?>
<WithEnums xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Int>Option1</Int>
  <Short>Option2</Short>
</WithEnums>");
        Assert.StrictEqual(item.Short, actual.Short);
        Assert.StrictEqual(item.Int, actual.Int);
    }

    [Fact]
    public static void Xml_Nullables()
    {
        var item = new WithNullables() { Optional = IntEnum.Option1, OptionalInt = 42, Struct1 = new SomeStruct { A = 1, B = 2 } };
        var actual = SerializeAndDeserialize(item,
@"<?xml version=""1.0""?>
<WithNullables xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Optional>Option1</Optional>
  <Optionull xsi:nil=""true"" />
  <OptionalInt>42</OptionalInt>
  <OptionullInt xsi:nil=""true"" />
  <Struct1>
    <A>1</A>
    <B>2</B>
  </Struct1>
  <Struct2 xsi:nil=""true"" />
</WithNullables>");
        Assert.StrictEqual(item.OptionalInt, actual.OptionalInt);
        Assert.StrictEqual(item.Optional, actual.Optional);
        Assert.StrictEqual(item.Optionull, actual.Optionull);
        Assert.StrictEqual(item.OptionullInt, actual.OptionullInt);
        Assert.Null(actual.Struct2);
        Assert.StrictEqual(item.Struct1.Value.A, actual.Struct1.Value.A);
        Assert.StrictEqual(item.Struct1.Value.B, actual.Struct1.Value.B);
    }

    [Fact]
    public static void Xml_ClassImplementingIXmlSerialiable()
    {
        var value = new ClassImplementingIXmlSerialiable() { StringValue = "Hello world" };
        var actual = SerializeAndDeserialize<ClassImplementingIXmlSerialiable>(value,
@"<?xml version=""1.0""?>
<ClassImplementingIXmlSerialiable StringValue=""Hello world"" BoolValue=""True"" />");
        Assert.StrictEqual(value.StringValue, actual.StringValue);
        Assert.StrictEqual(value.GetPrivateMember(), actual.GetPrivateMember());
        Assert.True(ClassImplementingIXmlSerialiable.ReadXmlInvoked);
        Assert.True(ClassImplementingIXmlSerialiable.WriteXmlInvoked);
    }

    [Fact]
    public static void Xml_TypeWithFieldNameEndBySpecified()
    {
        var value = new TypeWithPropertyNameSpecified() { MyField = "MyField", MyFieldIgnored = 99, MyFieldSpecified = true, MyFieldIgnoredSpecified = false };
        var actual = SerializeAndDeserialize<TypeWithPropertyNameSpecified>(value,
@"<?xml version=""1.0""?><TypeWithPropertyNameSpecified xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><MyField>MyField</MyField></TypeWithPropertyNameSpecified>");
        Assert.StrictEqual(value.MyField, actual.MyField);
        Assert.StrictEqual(actual.MyFieldIgnored, 0);
    }

    [Fact]
    public static void XML_TypeWithXmlSchemaFormAttribute()
    {
        var value = new TypeWithXmlSchemaFormAttribute() { NoneSchemaFormListProperty = new List<string> { "abc" }, QualifiedSchemaFormListProperty = new List<bool> { true }, UnqualifiedSchemaFormListProperty = new List<int> { 1 } };
        var actual = SerializeAndDeserialize<TypeWithXmlSchemaFormAttribute>(value,
@"<?xml version=""1.0""?><TypeWithXmlSchemaFormAttribute xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><UnqualifiedSchemaFormListProperty><int>1</int></UnqualifiedSchemaFormListProperty><NoneSchemaFormListProperty><NoneParameter>abc</NoneParameter></NoneSchemaFormListProperty><QualifiedSchemaFormListProperty><QualifiedParameter>true</QualifiedParameter></QualifiedSchemaFormListProperty></TypeWithXmlSchemaFormAttribute>");

        Assert.StrictEqual(value.NoneSchemaFormListProperty.Count, actual.NoneSchemaFormListProperty.Count);
        Assert.StrictEqual(value.NoneSchemaFormListProperty[0], actual.NoneSchemaFormListProperty[0]);
        Assert.StrictEqual(value.UnqualifiedSchemaFormListProperty.Count, actual.UnqualifiedSchemaFormListProperty.Count);
        Assert.StrictEqual(value.UnqualifiedSchemaFormListProperty[0], actual.UnqualifiedSchemaFormListProperty[0]);
        Assert.StrictEqual(value.QualifiedSchemaFormListProperty.Count, actual.QualifiedSchemaFormListProperty.Count);
        Assert.StrictEqual(value.QualifiedSchemaFormListProperty[0], actual.QualifiedSchemaFormListProperty[0]);
    }

    [Fact]
    public static void XML_TypeWithTypeNameInXmlTypeAttribute()
    {
        var value = new TypeWithTypeNameInXmlTypeAttribute();

        SerializeAndDeserialize<TypeWithTypeNameInXmlTypeAttribute>(value,
@"<?xml version=""1.0""?><MyXmlType xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" />");
    }

    [Fact]
    public static void XML_TypeWithXmlTextAttributeOnArray()
    {
        var original = new TypeWithXmlTextAttributeOnArray() { Text = new string[] { "val1", "val2" } };

        var actual = SerializeAndDeserialize<TypeWithXmlTextAttributeOnArray>(original,
@"<?xml version=""1.0""?>
<TypeWithXmlTextAttributeOnArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://schemas.xmlsoap.org/ws/2005/04/discovery"">val1val2</TypeWithXmlTextAttributeOnArray>");
        Assert.NotNull(actual.Text);
        Assert.StrictEqual(1, actual.Text.Length);
        Assert.StrictEqual("val1val2", actual.Text[0]);
    }

    [Fact]
    public static void Xml_TypeWithSchemaFormInXmlAttribute()
    {
        var value = new TypeWithSchemaFormInXmlAttribute() { TestProperty = "hello" };
        var actual = SerializeAndDeserialize<TypeWithSchemaFormInXmlAttribute>(value,
@"<?xml version=""1.0""?><TypeWithSchemaFormInXmlAttribute xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" d1p1:TestProperty=""hello"" xmlns:d1p1=""http://test.com"" />");
        Assert.StrictEqual(value.TestProperty, actual.TestProperty);
    }


    [Fact]
    public static void Xml_TypeWithXmlElementProperty()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml(@"<html></html>");
        XmlElement productElement = xDoc.CreateElement("Product");
        productElement.InnerText = "Product innertext";
        XmlElement categoryElement = xDoc.CreateElement("Category");
        categoryElement.InnerText = "Category innertext";
        var expected = new TypeWithXmlElementProperty() { Elements = new[] { productElement, categoryElement } };
        var actual = SerializeAndDeserialize(expected,
@"<?xml version=""1.0"" encoding=""utf-8""?><TypeWithXmlElementProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><Product>Product innertext</Product><Category>Category innertext</Category></TypeWithXmlElementProperty>");
        Assert.StrictEqual(expected.Elements.Length, actual.Elements.Length);
        for (int i = 0; i < expected.Elements.Length; ++i)
        {
            Assert.StrictEqual(expected.Elements[i].InnerText, actual.Elements[i].InnerText);
        }
    }

    [Fact]
    public static void Xml_TypeWithXmlDocumentProperty()
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(@"<html><head>Head content</head><body><h1>Heading1</h1><div>Text in body</div></body></html>");
        var expected = new TypeWithXmlDocumentProperty() { Document = xmlDoc };
        var actual = SerializeAndDeserialize(expected,
@"<TypeWithXmlDocumentProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><Document><html><head>Head content</head><body><h1>Heading1</h1><div>Text in body</div></body></html></Document></TypeWithXmlDocumentProperty>");
        Assert.NotNull(actual);
        Assert.NotNull(actual.Document);
        Assert.StrictEqual(expected.Document.OuterXml, actual.Document.OuterXml);
    }

    [Fact]
    public static void Xml_TypeWithNonPublicDefaultConstructor()
    {
        System.Reflection.TypeInfo ti = System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(TypeWithNonPublicDefaultConstructor));
        TypeWithNonPublicDefaultConstructor value = null;
        value = (TypeWithNonPublicDefaultConstructor)FindDefaultConstructor(ti).Invoke(null);
        Assert.StrictEqual("Mr. FooName", value.Name);
        var actual = SerializeAndDeserialize<TypeWithNonPublicDefaultConstructor>(value,
@"<?xml version=""1.0""?>
<TypeWithNonPublicDefaultConstructor xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Name>Mr. FooName</Name>
</TypeWithNonPublicDefaultConstructor>");
        Assert.StrictEqual(value.Name, actual.Name);
    }

    private static System.Reflection.ConstructorInfo FindDefaultConstructor(System.Reflection.TypeInfo ti)
    {
        foreach (System.Reflection.ConstructorInfo ci in ti.DeclaredConstructors)
        {
            if (!ci.IsStatic && ci.GetParameters().Length == 0)
            {
                return ci;
            }
        }
        return null;
    }

    [Fact]
    public static void Xml_TestIgnoreWhitespaceForDeserialization()
    {
        string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ServerSettings>
  <DS2Root>
    <![CDATA[ http://wxdata.weather.com/wxdata/]]>
  </DS2Root>
  <MetricConfigUrl><![CDATA[ http://s3.amazonaws.com/windows-prod-twc/desktop8/beacons.xml ]]></MetricConfigUrl>
</ServerSettings>";

        XmlSerializer serializer = new XmlSerializer(typeof(ServerSettings));
        StringReader reader = new StringReader(xml);
        var value = (ServerSettings)serializer.Deserialize(reader);
        Assert.StrictEqual(@" http://s3.amazonaws.com/windows-prod-twc/desktop8/beacons.xml ", value.MetricConfigUrl);
        Assert.StrictEqual(@" http://wxdata.weather.com/wxdata/", value.DS2Root);
    }


    [Fact]
    public static void Xml_TypeWithBinaryProperty()
    {
        var obj = new TypeWithBinaryProperty();
        var str = "The quick brown fox jumps over the lazy dog.";
        obj.Base64Content = Encoding.Unicode.GetBytes(str);
        obj.BinaryHexContent = Encoding.Unicode.GetBytes(str);
        var actual = SerializeAndDeserialize(obj,
@"<?xml version=""1.0"" encoding=""utf-8""?><TypeWithBinaryProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><BinaryHexContent>540068006500200071007500690063006B002000620072006F0077006E00200066006F00780020006A0075006D007000730020006F00760065007200200074006800650020006C0061007A007900200064006F0067002E00</BinaryHexContent><Base64Content>VABoAGUAIABxAHUAaQBjAGsAIABiAHIAbwB3AG4AIABmAG8AeAAgAGoAdQBtAHAAcwAgAG8AdgBlAHIAIAB0AGgAZQAgAGwAYQB6AHkAIABkAG8AZwAuAA==</Base64Content></TypeWithBinaryProperty>");
        Assert.StrictEqual(true, Enumerable.SequenceEqual(obj.Base64Content, actual.Base64Content));
        Assert.StrictEqual(true, Enumerable.SequenceEqual(obj.BinaryHexContent, actual.BinaryHexContent));
    }

    [Fact]
    public static void Xml_DifferentSerializeDeserializeOverloads()
    {
        var expected = new SimpleType() { P1 = "p1 value", P2 = 123 };
        var serializer = new XmlSerializer(typeof(SimpleType));
        var writerTypes = new Type[] { typeof(TextWriter), typeof(XmlWriter) };
        Assert.Throws<InvalidOperationException>(() =>
        {
            XmlWriter writer = null;
            serializer.Serialize(writer, expected);
        });
        Assert.Throws<InvalidOperationException>(() =>
        {
            XmlReader reader = null;
            serializer.Deserialize(reader);
        });
        foreach (var writerType in writerTypes)
        {
            var stream = new MemoryStream();

            if (writerType == typeof(TextWriter))
            {
                var writer = new StreamWriter(stream);
                serializer.Serialize(writer, expected);
            }
            else
            {
                var writer = XmlWriter.Create(stream);
                serializer.Serialize(writer, expected);
            }
            stream.Position = 0;
            var actualOutput = new StreamReader(stream).ReadToEnd();
            const string baseline =
    @"<?xml version=""1.0"" encoding=""utf-8""?><SimpleType xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><P1>p1 value</P1><P2>123</P2></SimpleType>";
            var result = Utils.Compare(baseline, actualOutput);
            Assert.True(result.Equal, string.Format("{1}{0}Test failed for input: {2}{0}Expected: {3}{0}Actual: {4}", Environment.NewLine, result.ErrorMessage, expected, baseline, actualOutput));
            stream.Position = 0;

            // XmlSerializer.CanSerialize(XmlReader)
            XmlReader reader = XmlReader.Create(stream);
            Assert.True(serializer.CanDeserialize(reader));

            // XmlSerializer.Deserialize(XmlReader)
            var actual = (SimpleType)serializer.Deserialize(reader);
            Assert.StrictEqual(expected.P1, actual.P1);
            Assert.StrictEqual(expected.P2, actual.P2);

            stream.Dispose();
        }
    }

    [ConditionalFact(nameof(IsTimeSpanSerializationAvailable))]
    public static void Xml_TypeWithTimeSpanProperty()
    {
        var obj = new TypeWithTimeSpanProperty { TimeSpanProperty = TimeSpan.FromMilliseconds(1) };
        var deserializedObj = SerializeAndDeserialize(obj,
@"<?xml version=""1.0"" encoding=""utf-16""?>
<TypeWithTimeSpanProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<TimeSpanProperty>PT0.001S</TimeSpanProperty>
</TypeWithTimeSpanProperty>");
        Assert.StrictEqual(obj.TimeSpanProperty, deserializedObj.TimeSpanProperty);
    }

    [ConditionalFact(nameof(IsTimeSpanSerializationAvailable))]
    public static void Xml_TypeWithDefaultTimeSpanProperty()
    {
        var obj = new TypeWithDefaultTimeSpanProperty { TimeSpanProperty2 = new TimeSpan(0, 1, 0) };
        var deserializedObj = SerializeAndDeserialize(obj,
@"<?xml version=""1.0""?>
<TypeWithDefaultTimeSpanProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><TimeSpanProperty2>PT1M</TimeSpanProperty2></TypeWithDefaultTimeSpanProperty>");
        Assert.NotNull(deserializedObj);
        Assert.Equal(obj.TimeSpanProperty, deserializedObj.TimeSpanProperty);
        Assert.Equal(obj.TimeSpanProperty2, deserializedObj.TimeSpanProperty2);
    }

    [Fact]
    public static void Xml_DeserializeTypeWithEmptyTimeSpanProperty()
    {
        string xml = 
            @"<?xml version=""1.0""?>
            <TypeWithTimeSpanProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
            <TimeSpanProperty />
            </TypeWithTimeSpanProperty>";
        XmlSerializer serializer = new XmlSerializer(typeof(TypeWithTimeSpanProperty));

        using (StringReader reader = new StringReader(xml))
        {
            TypeWithTimeSpanProperty deserializedObj = (TypeWithTimeSpanProperty)serializer.Deserialize(reader);
            Assert.NotNull(deserializedObj);
            Assert.Equal(default(TimeSpan), deserializedObj.TimeSpanProperty);
        }
    }

    [Fact]
    public static void Xml_DeserializeEmptyTimeSpanType()
    {
        string xml =
    @"<?xml version=""1.0""?>
     <TimeSpan />";
        XmlSerializer serializer = new XmlSerializer(typeof(TimeSpan));

        using (StringReader reader = new StringReader(xml))
        {
            TimeSpan deserializedObj = (TimeSpan)serializer.Deserialize(reader);
            Assert.NotNull(deserializedObj);
            Assert.Equal(default(TimeSpan), deserializedObj);
        }
    }

    [Fact]
    public static void Xml_TypeWithByteProperty()
    {
        var obj = new TypeWithByteProperty() { ByteProperty = 123 };
        var deserializedObj = SerializeAndDeserialize(obj,
@"<?xml version=""1.0"" encoding=""utf-8""?>
<TypeWithByteProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <ByteProperty>123</ByteProperty>
</TypeWithByteProperty>");
        Assert.StrictEqual(obj.ByteProperty, deserializedObj.ByteProperty);
    }

    [Fact]
    public static void Xml_DeserializeOutOfRangeByteProperty()
    {
        //Deserialize an instance with out-of-range value for the byte property, expecting exception from deserialization process
        var serializer = new XmlSerializer(typeof(TypeWithByteProperty));
        using (var stream = new MemoryStream())
        {
            var writer = new StreamWriter(stream);
            writer.Write(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<TypeWithByteProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <ByteProperty>-1</ByteProperty>
</TypeWithByteProperty>");
            writer.Flush();
            stream.Position = 0;
            Assert.Throws<InvalidOperationException>(() => {
                var deserializedObj = (TypeWithByteProperty)serializer.Deserialize(stream);
            });
        }
    }

    [Fact]
    public static void Xml_XmlAttributes_RemoveXmlElementAttribute()
    {
        XmlAttributes attrs = new XmlAttributes();

        XmlElementAttribute item = new XmlElementAttribute("elem1");
        attrs.XmlElements.Add(item);
        Assert.True(attrs.XmlElements.Contains(item));

        attrs.XmlElements.Remove(item);
        Assert.False(attrs.XmlElements.Contains(item));
    }

    [Fact]
    public static void Xml_ArrayOfXmlNodeProperty()
    {
        var obj = new TypeWithXmlNodeArrayProperty()
        {
            CDATA = new[] { new XmlDocument().CreateCDataSection("test&test") }
        };
        var deserializedObj = SerializeAndDeserialize<TypeWithXmlNodeArrayProperty>(obj, @"<TypeWithXmlNodeArrayProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><![CDATA[test&test]]></TypeWithXmlNodeArrayProperty>");
        Assert.Equal(obj.CDATA.Length, deserializedObj.CDATA.Length);
        Assert.Equal(obj.CDATA[0].InnerText, deserializedObj.CDATA[0].InnerText);
    }

    [Fact]
    public static void Xml_TypeWithTwoDimensionalArrayProperty2()
    {
        SimpleType[][] simpleType2D = GetObjectwith2DArrayOfSimpleType();

        var obj = new TypeWith2DArrayProperty2()
        {
            TwoDArrayOfSimpleType = simpleType2D
        };

        string baseline = "<?xml version=\"1.0\" encoding=\"utf - 8\"?>\r\n<TypeWith2DArrayProperty2 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <TwoDArrayOfSimpleType>\r\n    <SimpleType>\r\n      <SimpleType>\r\n        <P1>0 0 value</P1>\r\n        <P2>1</P2>\r\n      </SimpleType>\r\n      <SimpleType>\r\n        <P1>0 1 value</P1>\r\n        <P2>2</P2>\r\n      </SimpleType>\r\n    </SimpleType>\r\n    <SimpleType>\r\n      <SimpleType>\r\n        <P1>1 0 value</P1>\r\n        <P2>3</P2>\r\n      </SimpleType>\r\n      <SimpleType>\r\n        <P1>1 1 value</P1>\r\n        <P2>4</P2>\r\n      </SimpleType>\r\n    </SimpleType>\r\n  </TwoDArrayOfSimpleType>\r\n</TypeWith2DArrayProperty2>";
        TypeWith2DArrayProperty2 actual = SerializeAndDeserialize(obj, baseline);
        Assert.NotNull(actual);
        Assert.True(SimpleType.AreEqual(simpleType2D[0][0], actual.TwoDArrayOfSimpleType[0][0]));
        Assert.True(SimpleType.AreEqual(simpleType2D[0][1], actual.TwoDArrayOfSimpleType[0][1]));
        Assert.True(SimpleType.AreEqual(simpleType2D[1][0], actual.TwoDArrayOfSimpleType[1][0]));
        Assert.True(SimpleType.AreEqual(simpleType2D[1][1], actual.TwoDArrayOfSimpleType[1][1]));
    }

    private static SimpleType[][] GetObjectwith2DArrayOfSimpleType()
    {
        SimpleType[][] simpleType2D = new SimpleType[2][];
        simpleType2D[0] = new SimpleType[2];
        simpleType2D[1] = new SimpleType[2];
        simpleType2D[0][0] = new SimpleType() { P1 = "0 0 value", P2 = 1 };
        simpleType2D[0][1] = new SimpleType() { P1 = "0 1 value", P2 = 2 };
        simpleType2D[1][0] = new SimpleType() { P1 = "1 0 value", P2 = 3 };
        simpleType2D[1][1] = new SimpleType() { P1 = "1 1 value", P2 = 4 };
        return simpleType2D;
    }

    public static void Xml_TypeWithByteArrayAsXmlText()
    {
        var value = new TypeWithByteArrayAsXmlText() { Value = new byte[] { 1, 2, 3 } };
        var actual = SerializeAndDeserialize(value, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithByteArrayAsXmlText xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">AQID</TypeWithByteArrayAsXmlText>");

        Assert.NotNull(actual);
        Assert.NotNull(actual.Value);
        Assert.Equal(value.Value.Length, actual.Value.Length);
        Assert.True(Enumerable.SequenceEqual(value.Value, actual.Value));
    }

    [Fact]
    public static void Xml_SimpleType()
    {
        var serializer = new XmlSerializer(typeof(SimpleType));
        var obj = new SimpleType { P1 = "foo", P2 = 1 };
        var deserializedObj = SerializeAndDeserialize(obj,
@"<?xml version=""1.0"" encoding=""utf-16""?>
<SimpleType xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <P1>foo</P1>
  <P2>1</P2>
</SimpleType>");
        Assert.NotNull(deserializedObj);
        Assert.StrictEqual(obj.P1, deserializedObj.P1);
        Assert.StrictEqual(obj.P2, deserializedObj.P2);
    }

    [Fact]
    public static void Xml_BaseClassAndDerivedClass2WithSameProperty()
    {
        var value = new DerivedClassWithSameProperty2() { DateTimeProperty = new DateTime(100, DateTimeKind.Utc), IntProperty = 5, StringProperty = "TestString", ListProperty = new List<string>() };
        value.ListProperty.AddRange(new string[] { "one", "two", "three" });

        var actual = SerializeAndDeserialize(value,
@"<?xml version=""1.0""?>
<DerivedClassWithSameProperty2 xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <StringProperty>TestString</StringProperty>
  <IntProperty>5</IntProperty>
  <DateTimeProperty>0001-01-01T00:00:00.00001Z</DateTimeProperty>
  <ListProperty>
    <string>one</string>
    <string>two</string>
    <string>three</string>
  </ListProperty>
</DerivedClassWithSameProperty2>");

        Assert.StrictEqual(value.DateTimeProperty, actual.DateTimeProperty);
        Assert.StrictEqual(value.IntProperty, actual.IntProperty);
        Assert.StrictEqual(value.StringProperty, actual.StringProperty);
        Assert.Equal(value.ListProperty.ToArray(), actual.ListProperty.ToArray());
    }

    [Fact]
    public static void Xml_TypeWithPropertiesHavingDefaultValue_DefaultValue()
    {
        var value = new TypeWithPropertiesHavingDefaultValue()
        {
            StringProperty = "DefaultString",
            EmptyStringProperty = "",
            IntProperty = 11,
            CharProperty = 'm'
        };

        var actual = SerializeAndDeserialize(value, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithPropertiesHavingDefaultValue xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <CharProperty>109</CharProperty>\r\n</TypeWithPropertiesHavingDefaultValue>");

        Assert.NotNull(actual);
        Assert.StrictEqual(value.StringProperty, actual.StringProperty);
        Assert.StrictEqual(value.EmptyStringProperty, actual.EmptyStringProperty);
        Assert.StrictEqual(value.IntProperty, actual.IntProperty);
        Assert.StrictEqual(value.CharProperty, actual.CharProperty);
    }

    [Fact]
    public static void Xml_TypeWithStringPropertyWithDefaultValue_NonDefaultValue()
    {
        var value = new TypeWithPropertiesHavingDefaultValue()
        {
            StringProperty = "NonDefaultValue",
            EmptyStringProperty = "NonEmpty",
            IntProperty = 12,
            CharProperty = 'n'
        };

        var actual = SerializeAndDeserialize(value, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithPropertiesHavingDefaultValue xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <EmptyStringProperty>NonEmpty</EmptyStringProperty>\r\n  <StringProperty>NonDefaultValue</StringProperty>\r\n  <IntProperty>12</IntProperty>\r\n  <CharProperty>110</CharProperty>\r\n</TypeWithPropertiesHavingDefaultValue>");

        Assert.NotNull(actual);
        Assert.StrictEqual(value.StringProperty, actual.StringProperty);
    }

    [Fact]
    public static void Xml_TypeWithEnumPropertyHavingDefaultValue()
    {
        var value = new TypeWithEnumPropertyHavingDefaultValue() { EnumProperty = IntEnum.Option0 };
        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithEnumPropertyHavingDefaultValue xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <EnumProperty>Option0</EnumProperty>\r\n</TypeWithEnumPropertyHavingDefaultValue>",
            skipStringCompare: false);

        Assert.NotNull(actual);
        Assert.StrictEqual(value.EnumProperty, actual.EnumProperty);


        value = new TypeWithEnumPropertyHavingDefaultValue() { EnumProperty = IntEnum.Option1 };
        actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithEnumPropertyHavingDefaultValue xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" />",
            skipStringCompare: false);

        Assert.NotNull(actual);
        Assert.StrictEqual(value.EnumProperty, actual.EnumProperty);
    }

    [Fact]
    public static void Xml_TypeWithEnumFlagPropertyHavingDefaultValue()
    {
        var value = new TypeWithEnumFlagPropertyHavingDefaultValue() { EnumProperty = EnumFlags.Two | EnumFlags.Three };
        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithEnumFlagPropertyHavingDefaultValue xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <EnumProperty>Two Three</EnumProperty>\r\n</TypeWithEnumFlagPropertyHavingDefaultValue>");

        Assert.NotNull(actual);
        Assert.StrictEqual(value.EnumProperty, actual.EnumProperty);


        value = new TypeWithEnumFlagPropertyHavingDefaultValue();
        actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithEnumFlagPropertyHavingDefaultValue xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" />");

        Assert.NotNull(actual);
        Assert.StrictEqual(value.EnumProperty, actual.EnumProperty);
    }

    [Fact]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "dotnet/corefx #18964")]
    public static void Xml_Soap_TypeWithEnumFlagPropertyHavingDefaultValue()
    {
        var mapping = new SoapReflectionImporter().ImportTypeMapping(typeof(TypeWithEnumFlagPropertyHavingDefaultValue));
        var serializer = new XmlSerializer(mapping);

        var value = new TypeWithEnumFlagPropertyHavingDefaultValue() { EnumProperty = EnumFlags.Two | EnumFlags.Three };
        var actual = SerializeAndDeserialize(
            value,
            "<?xml version=\"1.0\"?>\r\n<TypeWithEnumFlagPropertyHavingDefaultValue xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" id=\"id1\">\r\n  <EnumProperty xsi:type=\"EnumFlags\">Two Three</EnumProperty>\r\n</TypeWithEnumFlagPropertyHavingDefaultValue>",
            () => serializer);

        Assert.NotNull(actual);
        Assert.StrictEqual(value.EnumProperty, actual.EnumProperty);


        value = new TypeWithEnumFlagPropertyHavingDefaultValue();
        actual = SerializeAndDeserialize(
            value,
            "<?xml version=\"1.0\"?>\r\n<TypeWithEnumFlagPropertyHavingDefaultValue xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" id=\"id1\">\r\n  <EnumProperty xsi:type=\"EnumFlags\">One Four</EnumProperty>\r\n</TypeWithEnumFlagPropertyHavingDefaultValue>",
            () => serializer);

        Assert.NotNull(actual);
        Assert.StrictEqual(value.EnumProperty, actual.EnumProperty);
    }

    [Fact]
    public static void Xml_TypeWithXmlQualifiedName()
    {
        var value = new TypeWithXmlQualifiedName()
        {
            Value = new XmlQualifiedName("FooName")
        };

        var actual = SerializeAndDeserialize(value, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithXmlQualifiedName xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Value xmlns=\"\">FooName</Value>\r\n</TypeWithXmlQualifiedName>", skipStringCompare: false);

        Assert.NotNull(actual);
        Assert.StrictEqual(value.Value, actual.Value);
    }

    [Fact]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "dotnet/corefx #18964")]
    public static void Xml_Soap_TypeWithXmlQualifiedName()
    {
        var mapping = new SoapReflectionImporter().ImportTypeMapping(typeof(TypeWithXmlQualifiedName));
        var serializer = new XmlSerializer(mapping);

        var value = new TypeWithXmlQualifiedName()
        {
            Value = new XmlQualifiedName("FooName")
        };

        var actual = SerializeAndDeserialize(
            value,
            "<?xml version=\"1.0\"?>\r\n<TypeWithXmlQualifiedName xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" id=\"id1\">\r\n  <Value xmlns=\"\" xsi:type=\"xsd:QName\">FooName</Value>\r\n</TypeWithXmlQualifiedName>",
            () => serializer);

        Assert.NotNull(actual);
        Assert.StrictEqual(value.Value, actual.Value);
    }

    [Fact]
    public static void Xml_TypeWithShouldSerializeMethod_WithDefaultValue()
    {
        var value = new TypeWithShouldSerializeMethod();

        var actual = SerializeAndDeserialize(value, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithShouldSerializeMethod xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" />");

        Assert.NotNull(actual);
        Assert.StrictEqual(value.Foo, actual.Foo);
    }

    [Fact]
    public static void Xml_TypeWithShouldSerializeMethod_WithNonDefaultValue()
    {
        var value = new TypeWithShouldSerializeMethod() { Foo = "SomeValue" };

        var actual = SerializeAndDeserialize(value, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithShouldSerializeMethod xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><Foo>SomeValue</Foo></TypeWithShouldSerializeMethod>");

        Assert.NotNull(actual);
        Assert.StrictEqual(value.Foo, actual.Foo);
    }

    [Fact]
    public static void Xml_KnownTypesThroughConstructorWithArrayProperties()
    {
        int[] intArray = new int[] { 1, 2, 3 };
        string[] stringArray = new string[] { "a", "b" };

        var value = new KnownTypesThroughConstructorWithArrayProperties() { IntArrayValue = intArray, StringArrayValue = stringArray };
        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<KnownTypesThroughConstructorWithArrayProperties xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <StringArrayValue xsi:type=\"ArrayOfString\">\r\n    <string>a</string>\r\n    <string>b</string>\r\n  </StringArrayValue>\r\n  <IntArrayValue xsi:type=\"ArrayOfInt\">\r\n    <int>1</int>\r\n    <int>2</int>\r\n    <int>3</int>\r\n  </IntArrayValue>\r\n</KnownTypesThroughConstructorWithArrayProperties>",
            () => { return new XmlSerializer(typeof(KnownTypesThroughConstructorWithArrayProperties), new Type[] { typeof(int[]), typeof(string[]) }); },
            skipStringCompare: false);

        Assert.NotNull(actual);

        var actualIntArray = (int[])actual.IntArrayValue;
        Assert.NotNull(actualIntArray);
        Assert.Equal(intArray.Length, actualIntArray.Length);
        Assert.True(Enumerable.SequenceEqual(intArray, actualIntArray));

        var actualStringArray = (string[])actual.StringArrayValue;
        Assert.NotNull(actualStringArray);
        Assert.True(Enumerable.SequenceEqual(stringArray, actualStringArray));
        Assert.Equal(stringArray.Length, actualStringArray.Length);
    }

    [Fact]
    public static void Xml_KnownTypesThroughConstructorWithEnumFlags()
    {
        var enumFlags = EnumFlags.One | EnumFlags.Four;
        var value = new KnownTypesThroughConstructorWithValue() { Value = enumFlags };
        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<KnownTypesThroughConstructorWithValue xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Value xsi:type=\"EnumFlags\">One Four</Value>\r\n</KnownTypesThroughConstructorWithValue>",
            () => { return new XmlSerializer(typeof(KnownTypesThroughConstructorWithValue), new Type[] { typeof(EnumFlags) }); },
            skipStringCompare: false);

        Assert.NotNull(actual);
        Assert.Equal((EnumFlags)value.Value, (EnumFlags)actual.Value);
    }

    [Fact]
    public static void Xml_KnownTypesThroughConstructorWithEnumFlagsXmlQualifiedName()
    {
        var value = new KnownTypesThroughConstructorWithValue() { Value = new XmlQualifiedName("foo") };
        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<KnownTypesThroughConstructorWithValue xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Value xsi:type=\"xsd:QName\">foo</Value>\r\n</KnownTypesThroughConstructorWithValue>",
            () => { return new XmlSerializer(typeof(KnownTypesThroughConstructorWithValue), new Type[] { typeof(XmlQualifiedName) }); },
            skipStringCompare: false);

        Assert.NotNull(actual);
        Assert.Equal((XmlQualifiedName)value.Value, (XmlQualifiedName)actual.Value);
    }

    [Fact]
    public static void Xml_TypeWithTypesHavingCustomFormatter()
    {
        var str = "The quick brown fox jumps over the lazy dog.";
        var value = new TypeWithTypesHavingCustomFormatter()
        {
            DateTimeContent = new DateTime(2016, 7, 18, 0, 0, 0, DateTimeKind.Utc),
            QNameContent = new XmlQualifiedName("QNameContent"),
            DateContent = new DateTime(2016, 7, 18, 0, 0, 0, DateTimeKind.Utc),
            NameContent = "NameContent",
            NCNameContent = "NCNameContent",
            NMTOKENContent = "NMTOKENContent",
            NMTOKENSContent = "NMTOKENSContent",
            Base64BinaryContent = Encoding.Unicode.GetBytes(str),
            HexBinaryContent = Encoding.Unicode.GetBytes(str),
        };

        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithTypesHavingCustomFormatter xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <DateTimeContent>2016-07-18T00:00:00Z</DateTimeContent>\r\n  <QNameContent xmlns=\"\">QNameContent</QNameContent>\r\n  <DateContent>2016-07-18</DateContent>\r\n  <NameContent>NameContent</NameContent>\r\n  <NCNameContent>NCNameContent</NCNameContent>\r\n  <NMTOKENContent>NMTOKENContent</NMTOKENContent>\r\n  <NMTOKENSContent>NMTOKENSContent</NMTOKENSContent>\r\n  <Base64BinaryContent>VABoAGUAIABxAHUAaQBjAGsAIABiAHIAbwB3AG4AIABmAG8AeAAgAGoAdQBtAHAAcwAgAG8AdgBlAHIAIAB0AGgAZQAgAGwAYQB6AHkAIABkAG8AZwAuAA==</Base64BinaryContent>\r\n  <HexBinaryContent>540068006500200071007500690063006B002000620072006F0077006E00200066006F00780020006A0075006D007000730020006F00760065007200200074006800650020006C0061007A007900200064006F0067002E00</HexBinaryContent>\r\n</TypeWithTypesHavingCustomFormatter>");

        Assert.NotNull(actual);
        Assert.True(value.DateTimeContent == actual.DateTimeContent, $"Actual DateTimeContent was not as expected. \r\n Expected: {value.DateTimeContent} \r\n Actual: {actual.DateTimeContent}");
        Assert.True(value.QNameContent == actual.QNameContent, $"Actual QNameContent was not as expected. \r\n Expected: {value.QNameContent} \r\n Actual: {actual.QNameContent}");
        Assert.True(value.DateContent == actual.DateContent, $"Actual DateContent was not as expected. \r\n Expected: {value.DateContent} \r\n Actual: {actual.DateContent}");
        Assert.True(value.NameContent == actual.NameContent, $"Actual NameContent was not as expected. \r\n Expected: {value.NameContent} \r\n Actual: {actual.NameContent}");
        Assert.True(value.NCNameContent == actual.NCNameContent, $"Actual NCNameContent was not as expected. \r\n Expected: {value.NCNameContent} \r\n Actual: {actual.NCNameContent}");
        Assert.True(value.NMTOKENContent == actual.NMTOKENContent, $"Actual NMTOKENContent was not as expected. \r\n Expected: {value.NMTOKENContent} \r\n Actual: {actual.NMTOKENContent}");
        Assert.True(value.NMTOKENSContent == actual.NMTOKENSContent, $"Actual NMTOKENSContent was not as expected. \r\n Expected: {value.NMTOKENSContent} \r\n Actual: {actual.NMTOKENSContent}");

        Assert.NotNull(actual.Base64BinaryContent);
        Assert.True(Enumerable.SequenceEqual(value.Base64BinaryContent, actual.Base64BinaryContent), "Actual Base64BinaryContent was not as expected.");

        Assert.NotNull(actual.HexBinaryContent);
        Assert.True(Enumerable.SequenceEqual(value.HexBinaryContent, actual.HexBinaryContent), "Actual HexBinaryContent was not as expected.");
    }

    [Fact]
    public static void Xml_TypeWithArrayPropertyHavingChoice()
    {
        object[] choices = new object[] { "Food", 5 };

        // For each item in the choices array, add an
        // enumeration value.
        MoreChoices[] itemChoices = new MoreChoices[] { MoreChoices.Item, MoreChoices.Amount };

        var value = new TypeWithArrayPropertyHavingChoice() { ManyChoices = choices, ChoiceArray = itemChoices };

        var actual = SerializeAndDeserialize(value, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithArrayPropertyHavingChoice xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Item>Food</Item>\r\n  <Amount>5</Amount>\r\n</TypeWithArrayPropertyHavingChoice>");

        Assert.NotNull(actual);
        Assert.NotNull(actual.ManyChoices);
        Assert.Equal(value.ManyChoices.Length, actual.ManyChoices.Length);
        Assert.True(Enumerable.SequenceEqual(value.ManyChoices, actual.ManyChoices));
    }

    [Fact]
    public static void XML_TypeWithTypeNameInXmlTypeAttribute_WithValue()
    {
        var value = new TypeWithTypeNameInXmlTypeAttribute() { XmlAttributeForm = "SomeValue" };

        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<MyXmlType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" XmlAttributeForm=\"SomeValue\" />",
            skipStringCompare: false);

        Assert.NotNull(actual);
        Assert.Equal(value.XmlAttributeForm, actual.XmlAttributeForm);
    }

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

        var actual = SerializeAndDeserialize(value, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWithFieldsOrdered xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <IntField1>1</IntField1>\r\n  <IntField2>2</IntField2>\r\n  <StringField2>foo2</StringField2>\r\n  <StringField1>foo1</StringField1>\r\n</TypeWithFieldsOrdered>");

        Assert.NotNull(actual);
        Assert.Equal(value.IntField1, actual.IntField1);
        Assert.Equal(value.IntField2, actual.IntField2);
        Assert.Equal(value.StringField1, actual.StringField1);
        Assert.Equal(value.StringField2, actual.StringField2);
    }

    [Fact]
    public static void XmlSerializerFactoryTest()
    {
        string baseline = "<?xml version=\"1.0\"?>\r\n<Dog xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Age>5</Age>\r\n  <Name>Bear</Name>\r\n  <Breed>GermanShepherd</Breed>\r\n</Dog>";
        var xsf = new XmlSerializerFactory();
        Func<XmlSerializer> serializerfunc = () => xsf.CreateSerializer(typeof(Dog));
        var dog1 = new Dog() { Name = "Bear", Age = 5, Breed = DogBreed.GermanShepherd };
        var dog2 = SerializeAndDeserialize(dog1, baseline, serializerfunc);
        Assert.Equal(dog1.Name, dog2.Name);
        Assert.Equal(dog1.Age, dog2.Age);
        Assert.Equal(dog1.Breed, dog2.Breed);
    }

    [Fact]
    public static void XmlUnknownElementAndEventHandlerTest()
    {
        List<string> grouplists = new List<string>();
        int count = 0;
        XmlSerializer serializer = new XmlSerializer(typeof(Group));
        serializer.UnknownElement += new XmlElementEventHandler((o, args) =>
        {
            Group myGroup = (Group)args.ObjectBeingDeserialized;
            Assert.NotNull(myGroup);
            grouplists.Add(args.Element.Name);
            ++count;
        });
        string xmlFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
  <Group xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd = ""http://www.w3.org/2001/XMLSchema"">
         <GroupName>MyGroup</GroupName>
         <GroupSize>Large</GroupSize>
         <GroupNumber>444</GroupNumber>
         <GroupBase>West</GroupBase>
       </Group >";
        Group group = (Group)serializer.Deserialize(GetStreamFromString(xmlFileContent));
        Assert.NotNull(group);
        Assert.NotNull(group.GroupName);
        Assert.Null(group.GroupVehicle);
        Assert.Equal(3, count);
        Assert.Equal(3, grouplists.Count());
        bool b = grouplists.Contains("GroupSize") && grouplists.Contains("GroupNumber") && grouplists.Contains("GroupBase");
        Assert.True(b);
    }

    [Fact]
    public static void XmlUnknownNodeAndEventHandlerTest()
    {
        List<string> grouplists = new List<string>();
        int count = 0;
        XmlSerializer serializer = new XmlSerializer(typeof(Group));
        serializer.UnknownNode += new XmlNodeEventHandler((o, args) =>
        {
            Group myGroup = (Group)args.ObjectBeingDeserialized;
            Assert.NotNull(myGroup);
            grouplists.Add(args.LocalName);
            ++count;
        });
        string xmlFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
  <Group xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:coho=""http://www.cohowinery.com"" xmlns:cp=""http://www.cpandl.com"">
              <coho:GroupName>MyGroup</coho:GroupName>
              <cp:GroupSize>Large</cp:GroupSize>
              <cp:GroupNumber>444</cp:GroupNumber>
              <coho:GroupBase>West</coho:GroupBase>
              <coho:ThingInfo>
                    <Number>1</Number>
                    <Name>Thing1</Name>
                    <Elmo>
                        <Glue>element</Glue>
                    </Elmo>
              </coho:ThingInfo>
  </Group>";
        Group group = (Group)serializer.Deserialize(GetStreamFromString(xmlFileContent));
        Assert.NotNull(group);
        Assert.Null(group.GroupName);
        Assert.Equal(5, count);
        Assert.Equal(5, grouplists.Count());
        bool b = grouplists.Contains("GroupName") && grouplists.Contains("GroupSize") && grouplists.Contains("GroupNumber") && grouplists.Contains("GroupBase") && grouplists.Contains("ThingInfo");
        Assert.True(b);
    }

    [Fact]
    public static void XmlUnknownAttributeAndEventHandlerTest()
    {
        List<string> grouplists = new List<string>();
        int count = 0;
        XmlSerializer serializer = new XmlSerializer(typeof(Group));
        serializer.UnknownAttribute += new XmlAttributeEventHandler((o, args) =>
        {
            Group myGroup = (Group)args.ObjectBeingDeserialized;
            Assert.NotNull(myGroup);
            grouplists.Add(args.Attr.LocalName);
            ++count;
        });
        string xmlFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
   <Group xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" GroupType='Technical' GroupNumber='42' GroupBase='Red'>
           <GroupName>MyGroup</GroupName>
         </Group>";
        Group group = (Group)serializer.Deserialize(GetStreamFromString(xmlFileContent));
        Assert.NotNull(group);
        Assert.NotNull(group.GroupName);
        Assert.Null(group.GroupVehicle);
        Assert.Equal(3, count);
        Assert.Equal(3, grouplists.Count());
        bool b = grouplists.Contains("GroupType") && grouplists.Contains("GroupNumber") && grouplists.Contains("GroupBase");
        Assert.True(b);
    }

    [Fact]
    public static void XmlDeserializationEventsTest()
    {
        List<string> grouplists = new List<string>();
        int count = 0;
        // Create an instance of the XmlSerializer class.
        XmlSerializer serializer = new XmlSerializer(typeof(Group));
        XmlDeserializationEvents events = new XmlDeserializationEvents();
        events.OnUnknownAttribute += new XmlAttributeEventHandler((o, args) =>
        {
            Group myGroup = (Group)args.ObjectBeingDeserialized;
            Assert.NotNull(myGroup);
            grouplists.Add(args.Attr.LocalName);
            ++count;
        });
        string xmlFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
   <Group xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" GroupType='Technical' GroupNumber='42' GroupBase='Red'>
           <GroupName>MyGroup</GroupName>
         </Group>";
        Group group = (Group)serializer.Deserialize(XmlReader.Create(GetStreamFromString(xmlFileContent)), events);
        Assert.NotNull(group);
        Assert.NotNull(group.GroupName);
        Assert.Null(group.GroupVehicle);
        Assert.Equal(3, count);
        Assert.Equal(3, grouplists.Count());
        bool b = grouplists.Contains("GroupType") && grouplists.Contains("GroupNumber") && grouplists.Contains("GroupBase");
        Assert.True(b);
    }
    private static Stream GetStreamFromString(string s)
    {
        MemoryStream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public static void XmlSerializerImplementationTest()
    {
        Employee emp = new Employee() { EmployeeName = "Allice" };
        SerializeIm sm = new SerializeIm();
        Func<XmlSerializer> serializerfunc = () => sm.GetSerializer(typeof(Employee));
        string expected = "<?xml version=\"1.0\"?>\r\n<Employee xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <EmployeeName>Allice</EmployeeName>\r\n</Employee>";
        SerializeAndDeserialize(emp, expected, serializerfunc);
    }

    [Fact]
    public static void Xml_HiddenDerivedFieldTest()
    {
        var value = new DerivedClass { value = "on derived" };
        var actual = SerializeAndDeserialize<BaseClass>(value,
@"<?xml version=""1.0""?>
<BaseClass xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""DerivedClass"">
  <value>on derived</value>
</BaseClass>");

        Assert.NotNull(actual);
        Assert.Null(actual.Value);
        Assert.Null(actual.value);
        Assert.Null(((DerivedClass)actual).Value);
        Assert.Equal(value.value, ((DerivedClass)actual).value);
    }

    [Fact]
    public static void Xml_NullRefInXmlSerializerCtorTest()
    {
        string defaultNamespace = "http://www.contoso.com";
        var value = PurchaseOrder.CreateInstance();
        string baseline =
@"<?xml version=""1.0""?>
<PurchaseOrder xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://www.contoso1.com"">
  <ShipTo Name=""John Doe"">
    <Line1>1 Main St.</Line1>
    <City>AnyTown</City>
    <State>WA</State>
    <Zip>00000</Zip>
  </ShipTo>
  <OrderDate>Monday, 10 April 2017</OrderDate>
  <Items>
    <OrderedItem>
      <ItemName>Widget S</ItemName>
      <Description>Small widget</Description>
      <UnitPrice>5.23</UnitPrice>
      <Quantity>3</Quantity>
      <LineTotal>15.69</LineTotal>
    </OrderedItem>
  </Items>
  <SubTotal>15.69</SubTotal>
  <ShipCost>12.51</ShipCost>
  <TotalCost>28.20</TotalCost>
</PurchaseOrder>";

        var actual = SerializeAndDeserialize(value,
            baseline,
            () => new XmlSerializer(value.GetType(), null, null, null, defaultNamespace)
            );
        Assert.NotNull(actual);
        Assert.Equal(value.OrderDate, actual.OrderDate);
        Assert.Equal(value.ShipCost, actual.ShipCost);
        Assert.Equal(value.SubTotal, actual.SubTotal);
        Assert.Equal(value.TotalCost, actual.TotalCost);
        Assert.Equal(value.ShipTo.City, actual.ShipTo.City);
        Assert.Equal(value.ShipTo.Line1, actual.ShipTo.Line1);
        Assert.Equal(value.ShipTo.Name, actual.ShipTo.Name);
        Assert.Equal(value.ShipTo.State, actual.ShipTo.State);
        Assert.Equal(value.ShipTo.Zip, actual.ShipTo.Zip);
        Assert.Equal(value.OrderedItems.Length, actual.OrderedItems.Length);
        for (int i = 0; i < value.OrderedItems.Length; i++)
        {
            Assert.Equal(value.OrderedItems.ElementAt(i).Description, actual.OrderedItems.ElementAt(i).Description);
            Assert.Equal(value.OrderedItems.ElementAt(i).ItemName, actual.OrderedItems.ElementAt(i).ItemName);
            Assert.Equal(value.OrderedItems.ElementAt(i).LineTotal, actual.OrderedItems.ElementAt(i).LineTotal);
            Assert.Equal(value.OrderedItems.ElementAt(i).Quantity, actual.OrderedItems.ElementAt(i).Quantity);
            Assert.Equal(value.OrderedItems.ElementAt(i).UnitPrice, actual.OrderedItems.ElementAt(i).UnitPrice);
        }

        actual = SerializeAndDeserialize(value,
            baseline,
            () => new XmlSerializer(value.GetType(), null, null, null, defaultNamespace, null)
            );
        Assert.NotNull(actual);
        Assert.Equal(value.OrderDate, actual.OrderDate);
        Assert.Equal(value.ShipCost, actual.ShipCost);
        Assert.Equal(value.SubTotal, actual.SubTotal);
        Assert.Equal(value.TotalCost, actual.TotalCost);
        Assert.Equal(value.ShipTo.City, actual.ShipTo.City);
        Assert.Equal(value.ShipTo.Line1, actual.ShipTo.Line1);
        Assert.Equal(value.ShipTo.Name, actual.ShipTo.Name);
        Assert.Equal(value.ShipTo.State, actual.ShipTo.State);
        Assert.Equal(value.ShipTo.Zip, actual.ShipTo.Zip);
        Assert.Equal(value.OrderedItems.Length, actual.OrderedItems.Length);
        for (int i = 0; i < value.OrderedItems.Length; i++)
        {
            Assert.Equal(value.OrderedItems.ElementAt(i).Description, actual.OrderedItems.ElementAt(i).Description);
            Assert.Equal(value.OrderedItems.ElementAt(i).ItemName, actual.OrderedItems.ElementAt(i).ItemName);
            Assert.Equal(value.OrderedItems.ElementAt(i).LineTotal, actual.OrderedItems.ElementAt(i).LineTotal);
            Assert.Equal(value.OrderedItems.ElementAt(i).Quantity, actual.OrderedItems.ElementAt(i).Quantity);
            Assert.Equal(value.OrderedItems.ElementAt(i).UnitPrice, actual.OrderedItems.ElementAt(i).UnitPrice);
        }
    }

    [Fact]
    public static void Xml_AliasedPropertyTest()
    {
        var inputList = new List<string> { "item0", "item1", "item2", "item3", "item4" };
        var value = new AliasedTestType { Aliased = inputList };
        var actual = SerializeAndDeserialize(value,
@"<?xml version=""1.0""?>
<AliasedTestType xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <Y>
    <string>item0</string>
    <string>item1</string>
    <string>item2</string>
    <string>item3</string>
    <string>item4</string>
  </Y>
</AliasedTestType>");

        Assert.NotNull(actual);
        Assert.NotNull(actual.Aliased);
        Assert.Equal(inputList.GetType(), actual.Aliased.GetType());
        Assert.Equal(inputList.Count, ((List<string>)actual.Aliased).Count);
        for (int i = 0; i < inputList.Count; i++)
        {
            Assert.Equal(inputList[i], ((List<string>)actual.Aliased).ElementAt(i));
        }
    }

    [Fact]
    public static void Xml_DeserializeHiddenMembersTest()
    {
        var xmlSerializer = new XmlSerializer(typeof(DerivedClass1));
        string inputXml = "<DerivedClass1><Prop>2012-07-07T00:18:29.7538612Z</Prop></DerivedClass1>";
        var dateTime = new DateTime(634772171097538612);

        using (var reader = new StringReader(inputXml))
        {
            var derivedClassInstance = (DerivedClass1)xmlSerializer.Deserialize(reader);
            Assert.NotNull(derivedClassInstance.Prop);
            Assert.Equal(1, derivedClassInstance.Prop.Count<DateTime>());
            Assert.Equal(dateTime, derivedClassInstance.Prop.ElementAt(0));
        }
    }

    [Fact]
    public static void Xml_SerializeClassNestedInStaticClassTest()
    {
        var value = new Outer.Person()
        {
            FirstName = "Harry",
            MiddleName = "James",
            LastName = "Potter"
        };

        var actual = SerializeAndDeserialize(value,
@"<?xml version=""1.0""?>
<Person xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <FirstName>Harry</FirstName>
  <MiddleName>James</MiddleName>
  <LastName>Potter</LastName>
</Person>");

        Assert.NotNull(actual);
        Assert.Equal(value.FirstName, actual.FirstName);
        Assert.Equal(value.MiddleName, actual.MiddleName);
        Assert.Equal(value.LastName, actual.LastName);
    }

    [Fact]
    public static void Xml_XSCoverTest()
    {
        var band = new Orchestra();
        var brass = new Brass()
        {
            Name = "Trumpet",
            IsValved = true
        };
        Instrument[] myInstruments = { brass };
        band.Instruments = myInstruments;

        var attrs = new XmlAttributes();
        var attr = new XmlElementAttribute()
        {
            ElementName = "Brass",
            Type = typeof(Brass)
        };

        attrs.XmlElements.Add(attr);
        var attrOverrides = new XmlAttributeOverrides();
        attrOverrides.Add(typeof(Orchestra), "Instruments", attrs);

        var actual = SerializeAndDeserialize(band,
@"<Orchestra xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
    <Brass>
      <Name>Trumpet</Name>
      <IsValved>true</IsValved>
    </Brass>
</Orchestra>", () => { return new XmlSerializer(typeof(Orchestra), attrOverrides); });

        Assert.Equal(band.Instruments.Length, actual.Instruments.Length);
        for (int i = 0; i < band.Instruments.Length; i++)
        {
            Assert.Equal(((Brass)band.Instruments.ElementAt(i)).Name, ((Brass)actual.Instruments[i]).Name);
            Assert.Equal(((Brass)band.Instruments.ElementAt(i)).IsValved, ((Brass)actual.Instruments[i]).IsValved);
        }

        band = new Orchestra();
        band.Instruments = new Instrument[1] { new Instrument { Name = "Instrument1" } };
        attrs = new XmlAttributes();
        var xArray = new XmlArrayAttribute("CommonInstruments");
        xArray.Namespace = "http://www.contoso.com";
        attrs.XmlArray = xArray;
        attrOverrides = new XmlAttributeOverrides();
        attrOverrides.Add(typeof(Orchestra), "Instruments", attrs);
        actual = SerializeAndDeserialize(band,
@"<?xml version=""1.0""?>
<Orchestra xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <CommonInstruments xmlns=""http://www.contoso.com"">
    <Instrument>
      <Name>Instrument1</Name>
    </Instrument>
  </CommonInstruments>
</Orchestra>", () => { return new XmlSerializer(typeof(Orchestra), attrOverrides); });
        Assert.Equal(band.Instruments.Length, actual.Instruments.Length);
        for (int i = 0; i < band.Instruments.Length; i++)
        {
            Assert.Equal((band.Instruments.ElementAt(i)).Name, (actual.Instruments[i]).Name);
        }

        band = new Orchestra();
        var trumpet = new Trumpet() { Name = "TrumpetKeyC", IsValved = false, Modulation = 'C' };
        band.Instruments = new Instrument[2] { brass, trumpet };

        attrs = new XmlAttributes();
        var xArrayItem = new XmlArrayItemAttribute(typeof(Brass));
        xArrayItem.Namespace = "http://www.contoso.com";
        attrs.XmlArrayItems.Add(xArrayItem);
        var xArrayItem2 = new XmlArrayItemAttribute(typeof(Trumpet));
        xArrayItem2.Namespace = "http://www.contoso.com";
        attrs.XmlArrayItems.Add(xArrayItem2);
        attrOverrides = new XmlAttributeOverrides();
        attrOverrides.Add(typeof(Orchestra), "Instruments", attrs);
        actual = SerializeAndDeserialize(band,
@"<?xml version=""1.0""?>
<Orchestra xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Instruments>
    <Brass xmlns=""http://www.contoso.com"">
      <Name>Trumpet</Name>
      <IsValved>true</IsValved>
    </Brass>
    <Trumpet xmlns=""http://www.contoso.com"">
      <Name>TrumpetKeyC</Name>
      <IsValved>false</IsValved>
      <Modulation>67</Modulation>
    </Trumpet>
  </Instruments>
</Orchestra>", () => { return new XmlSerializer(typeof(Orchestra), attrOverrides); });
        Assert.Equal(band.Instruments.Length, actual.Instruments.Length);
        for (int i = 0; i < band.Instruments.Length; i++)
        {
            Assert.Equal((band.Instruments.ElementAt(i)).Name, (actual.Instruments[i]).Name);
        }

        attrOverrides = new XmlAttributeOverrides();
        attrs = new XmlAttributes();
        object defaultAnimal = "Cat";
        attrs.XmlDefaultValue = defaultAnimal;
        attrOverrides.Add(typeof(Pet), "Animal", attrs);
        attrs = new XmlAttributes();
        attrs.XmlIgnore = false;
        attrOverrides.Add(typeof(Pet), "Comment", attrs);
        attrs = new XmlAttributes();
        var xType = new XmlTypeAttribute();
        xType.TypeName = "CuteFishes";
        xType.IncludeInSchema = true;
        attrs.XmlType = xType;
        attrOverrides.Add(typeof(Pet), attrs);

        var myPet = new Pet();
        myPet.Animal = "fish";
        myPet.Comment = "What a cute fish!";
        myPet.Comment2 = "I think it is cool!";

        var actual2 = SerializeAndDeserialize(myPet,
@"<?xml version=""1.0""?>
<CuteFishes xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Animal>fish</Animal>
  <Comment>What a cute fish!</Comment>
  <Comment2>I think it is cool!</Comment2>
</CuteFishes>
", () => { return new XmlSerializer(typeof(Pet), attrOverrides); });
        Assert.Equal(myPet.Animal, actual2.Animal);
        Assert.Equal(myPet.Comment, actual2.Comment);
        Assert.Equal(myPet.Comment2, actual2.Comment2);
    }

    [Fact]
    public static void Xml_TypeWithMyCollectionField()
    {
        var value = new TypeWithMyCollectionField();
        value.Collection = new MyCollection<string>() { "s1", "s2" };
        var actual = SerializeAndDeserializeWithWrapper(value, new XmlSerializer(typeof(TypeWithMyCollectionField)), "<root><TypeWithMyCollectionField xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><Collection><string>s1</string><string>s2</string></Collection></TypeWithMyCollectionField></root>");
        Assert.NotNull(actual);
        Assert.NotNull(actual.Collection);
        Assert.True(value.Collection.SequenceEqual(actual.Collection));
    }

    [Fact]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "dotnet/corefx #18964")]
    public static void Xml_Soap_TypeWithMyCollectionField()
    {
        XmlTypeMapping myTypeMapping = new SoapReflectionImporter().ImportTypeMapping(typeof(TypeWithMyCollectionField));
        var serializer = new XmlSerializer(myTypeMapping);
        var value = new TypeWithMyCollectionField();
        value.Collection = new MyCollection<string>() { "s1", "s2" };
        var actual = SerializeAndDeserializeWithWrapper(value, serializer, "<root><TypeWithMyCollectionField xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" id=\"id1\"><Collection href=\"#id2\" /></TypeWithMyCollectionField><q1:Array id=\"id2\" xmlns:q2=\"http://www.w3.org/2001/XMLSchema\" q1:arrayType=\"q2:string[]\" xmlns:q1=\"http://schemas.xmlsoap.org/soap/encoding/\"><Item>s1</Item><Item>s2</Item></q1:Array></root>");
        Assert.NotNull(actual);
        Assert.NotNull(actual.Collection);
        Assert.True(value.Collection.SequenceEqual(actual.Collection));
    }

    [Fact]
    public static void Xml_DefaultValueAttributeSetToNaNTest()
    {
        var value = new DefaultValuesSetToNaN();
        var actual = SerializeAndDeserialize(value,
@"<?xml version=""1.0""?>
<DefaultValuesSetToNaN xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <DoubleField>0</DoubleField>
  <SingleField>0</SingleField>
  <DoubleProp>0</DoubleProp>
  <FloatProp>0</FloatProp>
</DefaultValuesSetToNaN>");
        Assert.NotNull(actual);
        Assert.Equal(value, actual);
    }

    [Fact]
    public static void Xml_DefaultValueAttributeSetToPositiveInfinityTest()
    {
        var value = new DefaultValuesSetToPositiveInfinity();
        var actual = SerializeAndDeserialize(value,
@"<?xml version=""1.0""?>
<DefaultValuesSetToPositiveInfinity xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <DoubleField>0</DoubleField>
  <SingleField>0</SingleField>
  <DoubleProp>0</DoubleProp>
  <FloatProp>0</FloatProp>
</DefaultValuesSetToPositiveInfinity>");
        Assert.NotNull(actual);
        Assert.Equal(value, actual);
    }

    [Fact]
    public static void Xml_DefaultValueAttributeSetToNegativeInfinityTest()
    {
        var value = new DefaultValuesSetToNegativeInfinity();
        var actual = SerializeAndDeserialize(value,
@"<?xml version=""1.0""?>
<DefaultValuesSetToNegativeInfinity xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <DoubleField>0</DoubleField>
  <SingleField>0</SingleField>
  <DoubleProp>0</DoubleProp>
  <FloatProp>0</FloatProp>
</DefaultValuesSetToNegativeInfinity>");
        Assert.NotNull(actual);
        Assert.Equal(value, actual);
    }

    [ActiveIssue(28321)]
    [Fact]
    public static void SerializeWithDefaultValueSetToNaNTest()
    {
        var value = new DefaultValuesSetToNaN();
        value.DoubleField = double.NaN;
        value.SingleField = float.NaN;
        value.FloatProp = float.NaN;
        value.DoubleProp = double.NaN;

        bool result=SerializeWithDefaultValue(value,
@"<?xml version=""1.0""?>
<DefaultValuesSetToNaN xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" />");
        Assert.True(result);
    }

    [Fact]
    public static void SerializeWithDefaultValueSetToPositiveInfinityTest()
    {
        var value = new DefaultValuesSetToPositiveInfinity();
        value.DoubleField = double.PositiveInfinity;
        value.SingleField = float.PositiveInfinity;
        value.FloatProp = float.PositiveInfinity;
        value.DoubleProp = double.PositiveInfinity;

        bool result = SerializeWithDefaultValue(value,
@"<?xml version=""1.0""?>
<DefaultValuesSetToPositiveInfinity xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" />");
        Assert.True(result);
    }

    [Fact]
    public static void SerializeWithDefaultValueSetToNegativeInfinityTest()
    {
        var value = new DefaultValuesSetToNegativeInfinity();
        value.DoubleField = double.NegativeInfinity;
        value.SingleField = float.NegativeInfinity;
        value.FloatProp = float.NegativeInfinity;
        value.DoubleProp = double.NegativeInfinity;

        bool result = SerializeWithDefaultValue(value,
        @"<?xml version=""1.0""?>
<DefaultValuesSetToNegativeInfinity xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" />");
        Assert.True(result);
    }

    [Fact]
    public static void DeserializeIDREFSIntoStringTest()
    {
        string xmlstring = @"<?xml version = ""1.0"" encoding = ""utf-8"" ?><Document xmlns = ""http://example.com"" id = ""ID1"" refs=""ID1 ID2 ID3"" ></Document>";
        Stream ms = GenerateStreamFromString(xmlstring);
        XmlSerializer ser = new XmlSerializer(typeof(MsgDocumentType));
        var value = (MsgDocumentType)ser.Deserialize(ms);
        Assert.NotNull(value);
        Assert.Equal("ID1", value.Id);
        Assert.NotNull(value.Refs);
        Assert.Equal(3, value.Refs.Count());
        Assert.Equal("ID1", value.Refs[0]);
        Assert.Equal("ID2", value.Refs[1]);
        Assert.Equal("ID3", value.Refs[2]);
    }

    private static bool SerializeWithDefaultValue<T>(T value, string baseline)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        using (MemoryStream ms = new MemoryStream())
        {
            serializer.Serialize(ms, value);
            ms.Position = 0;
            string output = new StreamReader(ms).ReadToEnd();
            Utils.CompareResult result = Utils.Compare(baseline, output);
            return result.Equal;
        }
    }

    [Fact]
    public static void Xml_TypeWithMismatchBetweenAttributeAndPropertyType()
    {
        var value = new TypeWithMismatchBetweenAttributeAndPropertyType();
        var actual = SerializeAndDeserialize(value,
@"<?xml version=""1.0""?><RootElement xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" IntValue=""120"" />");
        Assert.StrictEqual(value.IntValue, actual.IntValue);
    }

    [Fact]
    public static void Xml_XsdValidationAndDeserialization()
    {
        var xsdstring = @"<?xml version='1.0' encoding='utf-8'?>
<xs:schema attributeFormDefault='unqualified' elementFormDefault='unqualified' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
  <xs:element name='RootClass'>
    <xs:complexType>
      <xs:sequence>
        <xs:element name='Parameters' type='parameters' minOccurs='1' maxOccurs='unbounded' />
      </xs:sequence>
    </xs:complexType>
  </xs:element>
  <xs:complexType name='parameters'>
    <xs:sequence>
      <xs:element name='Parameter' type='parameter' minOccurs='1' maxOccurs='unbounded' />
    </xs:sequence>
  </xs:complexType>
  <xs:complexType name='parameter'>
    <xs:attribute type='xs:string' name='Name' use='required' />
  </xs:complexType>
  <xs:complexType name='stringParameter' >
    <xs:complexContent>
      <xs:extension base='parameter'>
        <xs:sequence>
          <xs:element name='Value' minOccurs='0' maxOccurs='1'/>
        </xs:sequence>
      </xs:extension>
    </xs:complexContent>
  </xs:complexType>
</xs:schema>
";
        var param = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
  "<RootClass xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
        "<Parameters>" +
            "<Parameter xsi:type=\"stringParameter\" Name=\"SomeName\">" +
                "<Value />" +
            "</Parameter>" +
         "</Parameters>" +
    "</RootClass>";

        using (var stream = new MemoryStream())
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(param);
                writer.Flush();
                stream.Position = 0;

                var xmlReaderSettings = new XmlReaderSettings();
                xmlReaderSettings.ValidationType = ValidationType.Schema;
                xmlReaderSettings.ValidationEventHandler += (sender, args) =>
                {
                    throw new XmlSchemaValidationException(args.Message);
                };
                xmlReaderSettings.Schemas.Add(null, XmlReader.Create(new StringReader(xsdstring)));

                var xmlReader = XmlReader.Create(stream, xmlReaderSettings);

                var overrides = new XmlAttributeOverrides();
                var parametersXmlAttribute = new XmlAttributes { XmlType = new XmlTypeAttribute("stringParameter") };
                overrides.Add(typeof(Parameter<string>), parametersXmlAttribute);

                var serializer = new XmlSerializer(typeof(RootClass), overrides);
                var result=(RootClass)serializer.Deserialize(xmlReader);

                Assert.Equal("SomeName", result.Parameters[0].Name);
                Assert.Equal(string.Empty, ((Parameter<string>)result.Parameters[0]).Value);
            }
        }
    }

    private static readonly string s_defaultNs = "http://tempuri.org/";
    private static T RoundTripWithXmlMembersMapping<T>(object requestBodyValue, string memberName, string baseline, bool skipStringCompare = false, string wrapperName = null)
    {
        string ns = s_defaultNs;
        object[] value = new object[] { requestBodyValue };
        XmlReflectionMember member = GetReflectionMember<T>(memberName, ns);
        var members = new XmlReflectionMember[] { member };
        object[] actual = RoundTripWithXmlMembersMapping(value, baseline, skipStringCompare, members: members, wrapperName: wrapperName);
        Assert.Equal(value.Length, actual.Length);
        return (T)actual[0];
    }

    private static object[] RoundTripWithXmlMembersMapping(object[] value, string baseline, bool skipStringCompare, XmlReflectionMember[] members, string ns = null, string wrapperName = null, bool rpc = false)
    {
        ns = ns ?? s_defaultNs;
        var importer = new XmlReflectionImporter(null, ns);
        var membersMapping = importer.ImportMembersMapping(wrapperName, ns, members, wrapperName != null, rpc: rpc);
        var serializer = XmlSerializer.FromMappings(new XmlMapping[] { membersMapping })[0];
        using (var ms = new MemoryStream())
        {

            serializer.Serialize(ms, value);
            ms.Flush();
            ms.Position = 0;
            string actualOutput = new StreamReader(ms).ReadToEnd();
            if (!skipStringCompare)
            {
                Utils.CompareResult result = Utils.Compare(baseline, actualOutput);
                Assert.True(result.Equal, string.Format("{1}{0}Test failed for input: {2}{0}Expected: {3}{0}Actual: {4}",
                    Environment.NewLine, result.ErrorMessage, value, baseline, actualOutput));
            }

            ms.Position = 0;
            var actual = serializer.Deserialize(ms) as object[];
            Assert.NotNull(actual);

            return actual;
        }
    }

    private static T RoundTripWithXmlMembersMappingSoap<T>(object item, string memberName, string baseline, bool skipStringCompare = false, string wrapperName = null, bool validate = false)
    {
        string ns = s_defaultNs;
        object[] value = new object[] { item };
        XmlReflectionMember member = GetReflectionMember<T>(memberName, ns);
        var members = new XmlReflectionMember[] { member };
        object[] actual = RoundTripWithXmlMembersMappingSoap(value, baseline, skipStringCompare, members: members, wrapperName: wrapperName, validate: validate);
        Assert.Equal(value.Length, actual.Length);
        return (T)actual[0];
    }

    private static object[] RoundTripWithXmlMembersMappingSoap(object[] value, string baseline, bool skipStringCompare, XmlReflectionMember[] members, string ns = null, string wrapperName = null, bool writeAccessors = false, bool validate = false)
    {
        ns = ns ?? s_defaultNs;
        var importer = new SoapReflectionImporter(null, ns);
        var membersMapping = importer.ImportMembersMapping(wrapperName, ns, members, hasWrapperElement: wrapperName != null, writeAccessors: writeAccessors, validate: validate);
        var serializer = XmlSerializer.FromMappings(new XmlMapping[] { membersMapping })[0];
        using (var ms = new MemoryStream())
        {

            serializer.Serialize(ms, value);
            ms.Flush();
            ms.Position = 0;
            string actualOutput = new StreamReader(ms).ReadToEnd();
            if (!skipStringCompare)
            {
                Utils.CompareResult result = Utils.Compare(baseline, actualOutput);
                Assert.True(result.Equal, string.Format("{1}{0}Test failed for input: {2}{0}Expected: {3}{0}Actual: {4}",
                    Environment.NewLine, result.ErrorMessage, value, baseline, actualOutput));
            }

            ms.Position = 0;
            var actual = serializer.Deserialize(ms) as object[];
            Assert.NotNull(actual);

            return actual;
        }
    }

    private static XmlReflectionMember GetReflectionMember<T>(string memberName)
    {
        return GetReflectionMember<T>(memberName, s_defaultNs);
    }

    private static XmlReflectionMember GetReflectionMember<T>(string memberName, string ns)
    {
        var member = new XmlReflectionMember();
        member.MemberName = memberName;
        member.MemberType = typeof(T);
        member.XmlAttributes = new XmlAttributes();
        var elementAttribute = new XmlElementAttribute();
        elementAttribute.ElementName = memberName;
        elementAttribute.Namespace = ns;
        member.XmlAttributes.XmlElements.Add(elementAttribute);
        return member;
    }

    private static XmlReflectionMember GetReflectionMemberNoXmlElement<T>(string memberName, string ns = null)
    {
        ns = ns ?? s_defaultNs;
        var member = new XmlReflectionMember();
        member.MemberName = memberName;
        member.MemberType = typeof(T);
        member.XmlAttributes = new XmlAttributes();
        return member;
    }

    private static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private static T SerializeAndDeserialize<T>(T value, string baseline, Func<XmlSerializer> serializerFactory = null,
        bool skipStringCompare = false, XmlSerializerNamespaces xns = null)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        if (serializerFactory != null)
        {
            serializer = serializerFactory();
        }

        using (MemoryStream ms = new MemoryStream())
        {
            if (xns == null)
            {
                serializer.Serialize(ms, value);
            }
            else
            {
                serializer.Serialize(ms, value, xns);
            }

            ms.Position = 0;

            string actualOutput = new StreamReader(ms).ReadToEnd();

            if (!skipStringCompare)
            {
                Utils.CompareResult result = Utils.Compare(baseline, actualOutput);
                Assert.True(result.Equal, string.Format("{1}{0}Test failed for input: {2}{0}Expected: {3}{0}Actual: {4}",
                    Environment.NewLine, result.ErrorMessage, value, baseline, actualOutput));
            }

            ms.Position = 0;
            T deserialized = (T)serializer.Deserialize(ms);

            return deserialized;
        }
    }

    private static T SerializeAndDeserializeWithWrapper<T>(T value, XmlSerializer serializer, string baseline)
    {
        T actual;
        using (var ms = new MemoryStream())
        {
            var writer = new XmlTextWriter(ms, Encoding.UTF8);
            writer.WriteStartElement("root");
            serializer.Serialize(writer, value);
            writer.WriteEndElement();
            writer.Flush();

            ms.Position = 0;
            string actualOutput = new StreamReader(ms).ReadToEnd();
            Utils.CompareResult result = Utils.Compare(baseline, actualOutput);
            Assert.True(result.Equal, string.Format("{1}{0}Test failed for input: {2}{0}Expected: {3}{0}Actual: {4}",
                Environment.NewLine, result.ErrorMessage, value, baseline, actualOutput));

            ms.Position = 0;
            using (var reader = new XmlTextReader(ms))
            {
                reader.ReadStartElement("root");
                actual = (T)serializer.Deserialize(reader);
            }
        }

        return actual;
    }

    private static void AssertSerializationFailure<T, ExceptionType>() where T : new() where ExceptionType : Exception
    {
        try
        {
            SerializeAndDeserialize(new T(), string.Empty, skipStringCompare: true);
            Assert.True(false, $"Assert.True failed for {typeof(T)}. The above operation should have thrown, but it didn't.");
        }
        catch (Exception e)
        {
            Assert.True(e is ExceptionType, $"Assert.True failed for {typeof(T)}. Expected: {typeof(ExceptionType)}; Actual: {e.GetType()}");
        }
    }
}
