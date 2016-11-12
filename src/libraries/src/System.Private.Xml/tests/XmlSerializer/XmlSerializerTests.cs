// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SerializationTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
#if !NET_NATIVE
using System.Xml.Schema;
#endif
using System.Xml.Serialization;
using Xunit;

public static partial class XmlSerializerTests
{
    [Fact]
    public static void Xml_BoolAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<bool>(true,
@"<?xml version=""1.0""?>
<boolean>true</boolean>"), true);
        Assert.StrictEqual(SerializeAndDeserialize<bool>(false,
@"<?xml version=""1.0""?>
<boolean>false</boolean>"), false);
    }

    [Fact]
    public static void Xml_ByteArrayAsRoot()
    {
        Assert.Null(SerializeAndDeserialize<byte[]>(null,
@"<?xml version=""1.0""?>
<base64Binary d1p1:nil=""true"" xmlns:d1p1=""http://www.w3.org/2001/XMLSchema-instance"" />"));
        byte[] x = new byte[] { 1, 2 };
        byte[] y = SerializeAndDeserialize<byte[]>(x,
@"<?xml version=""1.0""?>
<base64Binary>AQI=</base64Binary>");
        Assert.Equal(x, y);
    }

    [Fact]
    public static void Xml_CharAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<char>(char.MinValue,
@"<?xml version=""1.0""?>
<char>0</char>"), char.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<char>(char.MaxValue,
@"<?xml version=""1.0""?>
<char>65535</char>"), char.MaxValue);
        Assert.StrictEqual(SerializeAndDeserialize<char>('a',
@"<?xml version=""1.0""?>
<char>97</char>"), 'a');
        Assert.StrictEqual(SerializeAndDeserialize<char>('ñ',
@"<?xml version=""1.0""?>
<char>241</char>"), 'ñ');
        Assert.StrictEqual(SerializeAndDeserialize<char>('漢',
@"<?xml version=""1.0""?>
<char>28450</char>"), '漢');
    }

    [Fact]
    public static void Xml_ByteAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<byte>(10,
@"<?xml version=""1.0""?>
<unsignedByte>10</unsignedByte>"), 10);
        Assert.StrictEqual(SerializeAndDeserialize<byte>(byte.MinValue,
@"<?xml version=""1.0""?>
<unsignedByte>0</unsignedByte>"), byte.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<byte>(byte.MaxValue,
@"<?xml version=""1.0""?>
<unsignedByte>255</unsignedByte>"), byte.MaxValue);
    }

    [Fact]
    public static void Xml_DateTimeAsRoot()
    {
        // Assume that UTC offset doesn't change more often than once in the day 2013-01-02
        // DO NOT USE TimeZoneInfo.Local.BaseUtcOffset !
        var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(new DateTime(2013, 1, 2)).TotalMinutes;
        var timeZoneString = string.Format("{0:+;-}{1}", offsetMinutes, new TimeSpan(0, offsetMinutes, 0).ToString(@"hh\:mm"));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2),
@"<?xml version=""1.0""?>
<dateTime>2013-01-02T00:00:00</dateTime>"), new DateTime(2013, 1, 2));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local), string.Format(@"<?xml version=""1.0""?>
<dateTime>2013-01-02T03:04:05.006{0}</dateTime>", timeZoneString)), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified),
@"<?xml version=""1.0""?>
<dateTime>2013-01-02T03:04:05.006</dateTime>"), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc),
@"<?xml version=""1.0""?>
<dateTime>2013-01-02T03:04:05.006Z</dateTime>"), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
@"<?xml version=""1.0""?>
<dateTime>0001-01-01T00:00:00Z</dateTime>"), DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc),
@"<?xml version=""1.0""?>
<dateTime>9999-12-31T23:59:59.9999999Z</dateTime>"), DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
    }

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

        TypeWithDateTimePropertyAsXmlTime utcTimeRoundTrip = SerializeAndDeserialize(utcTimeOjbect,
@"<?xml version=""1.0"" encoding=""utf-8""?>
<TypeWithDateTimePropertyAsXmlTime xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">15:15:26.9870000Z</TypeWithDateTimePropertyAsXmlTime>");

        Assert.StrictEqual(utcTimeOjbect.Value, utcTimeRoundTrip.Value);
    }

    [Fact]
    public static void Xml_DecimalAsRoot()
    {
        foreach (decimal value in new decimal[] { (decimal)-1.2, (decimal)0, (decimal)2.3, decimal.MinValue, decimal.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<decimal>(value, string.Format(@"<?xml version=""1.0""?>
<decimal>{0}</decimal>", value.ToString(CultureInfo.InvariantCulture))), value);
        }
    }

    [Fact]
    public static void Xml_DoubleAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<double>(-1.2,
@"<?xml version=""1.0""?>
<double>-1.2</double>"), -1.2);
        Assert.StrictEqual(SerializeAndDeserialize<double>(0,
@"<?xml version=""1.0""?>
<double>0</double>"), 0);
        Assert.StrictEqual(SerializeAndDeserialize<double>(2.3,
@"<?xml version=""1.0""?>
<double>2.3</double>"), 2.3);
        Assert.StrictEqual(SerializeAndDeserialize<double>(double.MinValue,
@"<?xml version=""1.0""?>
<double>-1.7976931348623157E+308</double>"), double.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<double>(double.MaxValue,
@"<?xml version=""1.0""?>
<double>1.7976931348623157E+308</double>"), double.MaxValue);
    }

    [Fact]
    public static void Xml_FloatAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)-1.2,
@"<?xml version=""1.0""?>
<float>-1.2</float>"), (float)-1.2);
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)0,
@"<?xml version=""1.0""?>
<float>0</float>"), (float)0);
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)2.3,
@"<?xml version=""1.0""?>
<float>2.3</float>"), (float)2.3);
        Assert.StrictEqual(SerializeAndDeserialize<float>(float.MinValue,
@"<?xml version=""1.0""?>
<float>-3.40282347E+38</float>"), float.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<float>(float.MaxValue,
@"<?xml version=""1.0""?>
<float>3.40282347E+38</float>"), float.MaxValue);
    }

    [Fact]
    public static void Xml_GuidAsRoot()
    {
        Xml_GuidAsRoot(new XmlSerializer(typeof(Guid)));
    }

    private static void Xml_GuidAsRoot(XmlSerializer serializer)
    {
        foreach (Guid value in new Guid[] { Guid.NewGuid(), Guid.Empty })
        {
            Assert.StrictEqual(SerializeAndDeserialize<Guid>(value, string.Format(@"<?xml version=""1.0""?>
<guid>{0}</guid>", value.ToString())), value);
        }
    }

    [Fact]
    public static void Xml_IntAsRoot()
    {
        foreach (int value in new int[] { -1, 0, 2, int.MinValue, int.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<int>(value, string.Format(@"<?xml version=""1.0""?>
<int>{0}</int>", value)), value);
        }
    }

    [Fact]
    public static void Xml_LongAsRoot()
    {
        foreach (long value in new long[] { (long)-1, (long)0, (long)2, long.MinValue, long.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<long>(value, string.Format(@"<?xml version=""1.0""?>
<long>{0}</long>", value)), value);
        }
    }

    [Fact]
    public static void Xml_ObjectAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<object>(1,
@"<?xml version=""1.0""?>
<anyType xmlns:q1=""http://www.w3.org/2001/XMLSchema"" d1p1:type=""q1:int"" xmlns:d1p1=""http://www.w3.org/2001/XMLSchema-instance"">1</anyType>"), 1);
        Assert.StrictEqual(SerializeAndDeserialize<object>(true,
@"<?xml version=""1.0""?>
<anyType xmlns:q1=""http://www.w3.org/2001/XMLSchema"" d1p1:type=""q1:boolean"" xmlns:d1p1=""http://www.w3.org/2001/XMLSchema-instance"">true</anyType>"), true);
        Assert.StrictEqual(SerializeAndDeserialize<object>("abc",
@"<?xml version=""1.0""?>
<anyType xmlns:q1=""http://www.w3.org/2001/XMLSchema"" d1p1:type=""q1:string"" xmlns:d1p1=""http://www.w3.org/2001/XMLSchema-instance"">abc</anyType>"), "abc");
        Assert.StrictEqual(SerializeAndDeserialize<object>(null,
@"<?xml version=""1.0""?><anyType xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />"), null);
    }

    [Fact]
    public static void Xml_XmlQualifiedNameAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<XmlQualifiedName>(new XmlQualifiedName("abc", "def"),
@"<?xml version=""1.0""?>
<QName xmlns:q1=""def"">q1:abc</QName>"), new XmlQualifiedName("abc", "def"));
        Assert.StrictEqual(SerializeAndDeserialize<XmlQualifiedName>(XmlQualifiedName.Empty,
@"<?xml version=""1.0""?><QName xmlns="""" />"), XmlQualifiedName.Empty);
    }

    [Fact]
    public static void Xml_ShortAsRoot()
    {
        foreach (short value in new short[] { (short)-1.2, (short)0, (short)2.3, short.MinValue, short.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<short>(value, string.Format(@"<?xml version=""1.0""?>
<short>{0}</short>", value)), value);
        }
    }

    [Fact]
    public static void Xml_SbyteAsRoot()
    {
        foreach (sbyte value in new sbyte[] { (sbyte)3, (sbyte)0, sbyte.MinValue, sbyte.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<sbyte>(value, string.Format(@"<?xml version=""1.0""?>
<byte>{0}</byte>", value)), value);
        }
    }

    [Fact]
    public static void Xml_StringAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<string>("abc",
@"<?xml version=""1.0""?>
<string>abc</string>"), "abc");
        Assert.StrictEqual(SerializeAndDeserialize<string>("  a b  ",
@"<?xml version=""1.0""?>
<string>  a b  </string>"), "  a b  ");
        Assert.StrictEqual(SerializeAndDeserialize<string>(null,
@"<?xml version=""1.0""?>
<string d1p1:nil=""true"" xmlns:d1p1=""http://www.w3.org/2001/XMLSchema-instance"" />"), null);
        Assert.StrictEqual(SerializeAndDeserialize<string>("",
@"<?xml version=""1.0""?>
<string />"), "");
        Assert.StrictEqual(SerializeAndDeserialize<string>(" ",
@"<?xml version=""1.0""?>
<string> </string>"), string.Empty);
        Assert.StrictEqual(SerializeAndDeserialize<string>("Hello World! 漢 ñ",
@"<?xml version=""1.0""?>
<string>Hello World! 漢 ñ</string>"), "Hello World! 漢 ñ");
    }

    [Fact]
    public static void Xml_UintAsRoot()
    {
        foreach (uint value in new uint[] { (uint)3, (uint)0, uint.MinValue, uint.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<uint>(value, string.Format(@"<?xml version=""1.0""?>
<unsignedInt>{0}</unsignedInt>", value)), value);
        }
    }

    [Fact]
    public static void Xml_UlongAsRoot()
    {
        foreach (ulong value in new ulong[] { (ulong)3, (ulong)0, ulong.MinValue, ulong.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<ulong>(value, string.Format(@"<?xml version=""1.0""?>
<unsignedLong>{0}</unsignedLong>", value)), value);
        }
    }

    [Fact]
    public static void Xml_UshortAsRoot()
    {
        foreach (ushort value in new ushort[] { (ushort)3, (ushort)0, ushort.MinValue, ushort.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<ushort>(value, string.Format(@"<?xml version=""1.0""?>
<unsignedShort>{0}</unsignedShort>", value)), value);
        }
    }

    [Fact]
    public static void Xml_ArrayAsRoot()
    {
        SimpleType[] x = new SimpleType[] { new SimpleType { P1 = "abc", P2 = 11 }, new SimpleType { P1 = "def", P2 = 12 } };
        SimpleType[] y = SerializeAndDeserialize<SimpleType[]>(x,
@"<?xml version=""1.0""?>
<ArrayOfSimpleType xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <SimpleType>
    <P1>abc</P1>
    <P2>11</P2>
  </SimpleType>
  <SimpleType>
    <P1>def</P1>
    <P2>12</P2>
  </SimpleType>
</ArrayOfSimpleType>");

        Utils.Equal(x, y, (a, b) => { return SimpleType.AreEqual(a, b); });
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
    public static void Xml_ListGenericRoot()
    {
        Xml_ListGenericRoot(new XmlSerializer(typeof(List<string>)));
    }

    private static void Xml_ListGenericRoot(XmlSerializer serializer)
    {
        List<string> x = new List<string>();
        x.Add("zero");
        x.Add("one");

        List<string> y = SerializeAndDeserialize<List<string>>(x,
@"<?xml version=""1.0""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>zero</string>
  <string>one</string>
</ArrayOfString>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True(y[0] == "zero");
        Assert.True(y[1] == "one");
    }

    [Fact]
    public static void Xml_CollectionGenericRoot()
    {
        MyCollection<string> x = new MyCollection<string>("a1", "a2");
        MyCollection<string> y = SerializeAndDeserialize<MyCollection<string>>(x,
@"<?xml version=""1.0""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>a1</string>
  <string>a2</string>
</ArrayOfString>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        foreach (string item in x)
        {
            Assert.True(y.Contains(item));
        }
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
    public static void Xml_EnumerableGenericRoot()
    {
        MyEnumerable<string> x = new MyEnumerable<string>("a1", "a2");
        MyEnumerable<string> y = SerializeAndDeserialize<MyEnumerable<string>>(x,
@"<?xml version=""1.0""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>a1</string>
  <string>a2</string>
</ArrayOfString>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);

        string itemsInY = string.Join("", y);
        Assert.StrictEqual("a1a2", itemsInY);
    }

    [Fact]
    public static void Xml_CollectionRoot()
    {
        MyCollection x = new MyCollection('a', 45);
        MyCollection y = SerializeAndDeserialize<MyCollection>(x,
@"<?xml version=""1.0""?>
<ArrayOfAnyType xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <anyType xmlns:q1=""http://microsoft.com/wsdl/types/"" xsi:type=""q1:char"">97</anyType>
  <anyType xsi:type=""xsd:int"">45</anyType>
</ArrayOfAnyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((char)y[0] == 'a');
        Assert.True((int)y[1] == 45);
    }

    [Fact]
    public static void Xml_EnumerableRoot()
    {
        MyEnumerable x = new MyEnumerable("abc", 3);
        MyEnumerable y = SerializeAndDeserialize<MyEnumerable>(x,
@"<?xml version=""1.0""?>
<ArrayOfAnyType xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <anyType xsi:type=""xsd:string"">abc</anyType>
  <anyType xsi:type=""xsd:int"">3</anyType>
</ArrayOfAnyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((string)y[0] == "abc");
        Assert.True((int)y[1] == 3);
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
    public static void Xml_GenericBase()
    {
        SerializeAndDeserialize<GenericBase2<SimpleBaseDerived, SimpleBaseDerived2>>(new GenericBase2<SimpleBaseDerived, SimpleBaseDerived2>(true),
@"<?xml version=""1.0""?>
<GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2 xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <genericData1>
    <BaseData />
    <DerivedData />
  </genericData1>
  <genericData2>
    <BaseData />
    <DerivedData />
  </genericData2>
</GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2>");
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
    public static void Xml_XElementAsRoot()
    {
        var original = new XElement("ElementName1");
        original.SetAttributeValue(XName.Get("Attribute1"), "AttributeValue1");
        original.SetValue("Value1");
        var actual = SerializeAndDeserialize<XElement>(original,
@"<?xml version=""1.0""?>
<ElementName1 Attribute1=""AttributeValue1"">Value1</ElementName1>");

        VerifyXElementObject(original, actual);
    }

    [Fact]
    public static void Xml_WithXElement()
    {
        var original = new WithXElement(true);
        var actual = SerializeAndDeserialize<WithXElement>(original,
@"<?xml version=""1.0""?>
<WithXElement xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <e>
    <ElementName1 Attribute1=""AttributeValue1"">Value1</ElementName1>
  </e>
</WithXElement>");

        VerifyXElementObject(original.e, actual.e);
    }

    private static void VerifyXElementObject(XElement x1, XElement x2, bool checkFirstAttribute = true)
    {
        Assert.StrictEqual(x1.Value, x2.Value);
        Assert.StrictEqual(x1.Name, x2.Name);
        if (checkFirstAttribute)
        {
            Assert.StrictEqual(x1.FirstAttribute.Name, x2.FirstAttribute.Name);
            Assert.StrictEqual(x1.FirstAttribute.Value, x2.FirstAttribute.Value);
        }
    }

    [Fact]
    public static void Xml_WithXElementWithNestedXElement()
    {
        var original = new WithXElementWithNestedXElement(true);
        var actual = SerializeAndDeserialize<WithXElementWithNestedXElement>(original,
@"<?xml version=""1.0""?>
<WithXElementWithNestedXElement xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <e1>
    <ElementName1 Attribute1=""AttributeValue1"">
      <ElementName2 Attribute2=""AttributeValue2"">Value2</ElementName2>
    </ElementName1>
  </e1>
</WithXElementWithNestedXElement>");

        VerifyXElementObject(original.e1, actual.e1);
        VerifyXElementObject((XElement)original.e1.FirstNode, (XElement)actual.e1.FirstNode);
    }

    [Fact]
    public static void Xml_WithArrayOfXElement()
    {
        var original = new WithArrayOfXElement(true);
        var actual = SerializeAndDeserialize<WithArrayOfXElement>(original,
@"<?xml version=""1.0""?>
<WithArrayOfXElement xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <a>
    <XElement>
      <item xmlns=""http://p.com/"">item0</item>
    </XElement>
    <XElement>
      <item xmlns=""http://p.com/"">item1</item>
    </XElement>
    <XElement>
      <item xmlns=""http://p.com/"">item2</item>
    </XElement>
  </a>
</WithArrayOfXElement>");

        Assert.StrictEqual(original.a.Length, actual.a.Length);
        VerifyXElementObject(original.a[0], actual.a[0], checkFirstAttribute: false);
        VerifyXElementObject(original.a[1], actual.a[1], checkFirstAttribute: false);
        VerifyXElementObject(original.a[2], actual.a[2], checkFirstAttribute: false);
    }

    [Fact]
    public static void Xml_WithListOfXElement()
    {
        var original = new WithListOfXElement(true);
        var actual = SerializeAndDeserialize<WithListOfXElement>(original,
@"<?xml version=""1.0""?>
<WithListOfXElement xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <list>
    <XElement>
      <item xmlns=""http://p.com/"">item0</item>
    </XElement>
    <XElement>
      <item xmlns=""http://p.com/"">item1</item>
    </XElement>
    <XElement>
      <item xmlns=""http://p.com/"">item2</item>
    </XElement>
  </list>
</WithListOfXElement>");

        Assert.StrictEqual(original.list.Count, actual.list.Count);
        VerifyXElementObject(original.list[0], actual.list[0], checkFirstAttribute: false);
        VerifyXElementObject(original.list[1], actual.list[1], checkFirstAttribute: false);
        VerifyXElementObject(original.list[2], actual.list[2], checkFirstAttribute: false);
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
    public static void Xml_JaggedArrayAsRoot()
    {
        int[][] jaggedIntegerArray = new int[][] { new int[] { 1, 3, 5, 7, 9 }, new int[] { 0, 2, 4, 6 }, new int[] { 11, 22 } };
        int[][] actualJaggedIntegerArray = SerializeAndDeserialize<int[][]>(jaggedIntegerArray,
@"<?xml version=""1.0""?>
<ArrayOfArrayOfInt xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <ArrayOfInt>
    <int>1</int>
    <int>3</int>
    <int>5</int>
    <int>7</int>
    <int>9</int>
  </ArrayOfInt>
  <ArrayOfInt>
    <int>0</int>
    <int>2</int>
    <int>4</int>
    <int>6</int>
  </ArrayOfInt>
  <ArrayOfInt>
    <int>11</int>
    <int>22</int>
  </ArrayOfInt>
</ArrayOfArrayOfInt>");
        Assert.Equal(jaggedIntegerArray[0], actualJaggedIntegerArray[0]);
        Assert.Equal(jaggedIntegerArray[1], actualJaggedIntegerArray[1]);
        Assert.Equal(jaggedIntegerArray[2], actualJaggedIntegerArray[2]);


        string[][] jaggedStringArray = new string[][] { new string[] { "1", "3", "5", "7", "9" }, new string[] { "0", "2", "4", "6" }, new string[] { "11", "22" } };
        string[][] actualJaggedStringArray = SerializeAndDeserialize<string[][]>(jaggedStringArray,
@"<?xml version=""1.0""?>
<ArrayOfArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <ArrayOfString>
    <string>1</string>
    <string>3</string>
    <string>5</string>
    <string>7</string>
    <string>9</string>
  </ArrayOfString>
  <ArrayOfString>
    <string>0</string>
    <string>2</string>
    <string>4</string>
    <string>6</string>
  </ArrayOfString>
  <ArrayOfString>
    <string>11</string>
    <string>22</string>
  </ArrayOfString>
</ArrayOfArrayOfString>");
        Assert.Equal(jaggedStringArray[0], actualJaggedStringArray[0]);
        Assert.Equal(jaggedStringArray[1], actualJaggedStringArray[1]);
        Assert.Equal(jaggedStringArray[2], actualJaggedStringArray[2]);


        object[] objectArray = new object[] { 1, 1.0F, 1.0, "string", Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860"), new DateTime(2013, 1, 2) };
        object[] actualObjectArray = SerializeAndDeserialize<object[]>(objectArray,
@"<?xml version=""1.0""?>
<ArrayOfAnyType xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <anyType xsi:type=""xsd:int"">1</anyType>
  <anyType xsi:type=""xsd:float"">1</anyType>
  <anyType xsi:type=""xsd:double"">1</anyType>
  <anyType xsi:type=""xsd:string"">string</anyType>
  <anyType xmlns:q1=""http://microsoft.com/wsdl/types/"" xsi:type=""q1:guid"">2054fd3e-e118-476a-9962-1a882be51860</anyType>
  <anyType xsi:type=""xsd:dateTime"">2013-01-02T00:00:00</anyType>
</ArrayOfAnyType>");
        Assert.True(1 == (int)actualObjectArray[0]);
        Assert.True(1.0F == (float)actualObjectArray[1]);
        Assert.True(1.0 == (double)actualObjectArray[2]);
        Assert.True("string" == (string)actualObjectArray[3]);
        Assert.True(Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860") == (Guid)actualObjectArray[4]);
        Assert.True(new DateTime(2013, 1, 2) == (DateTime)actualObjectArray[5]);


        int[][][] jaggedIntegerArray2 = new int[][][] { new int[][] { new int[] { 1 }, new int[] { 3 } }, new int[][] { new int[] { 0 } }, new int[][] { new int[] { } } };
        int[][][] actualJaggedIntegerArray2 = SerializeAndDeserialize<int[][][]>(jaggedIntegerArray2,
@"<?xml version=""1.0""?>
<ArrayOfArrayOfArrayOfInt xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <ArrayOfArrayOfInt>
    <ArrayOfInt>
      <int>1</int>
    </ArrayOfInt>
    <ArrayOfInt>
      <int>3</int>
    </ArrayOfInt>
  </ArrayOfArrayOfInt>
  <ArrayOfArrayOfInt>
    <ArrayOfInt>
      <int>0</int>
    </ArrayOfInt>
  </ArrayOfArrayOfInt>
  <ArrayOfArrayOfInt>
    <ArrayOfInt />
  </ArrayOfArrayOfInt>
</ArrayOfArrayOfArrayOfInt>");

        Assert.True(actualJaggedIntegerArray2.Length == 3);
        Assert.True(actualJaggedIntegerArray2[0][0][0] == 1);
        Assert.True(actualJaggedIntegerArray2[0][1][0] == 3);
        Assert.True(actualJaggedIntegerArray2[1][0][0] == 0);
        Assert.True(actualJaggedIntegerArray2[2][0].Length == 0);
    }

    [Fact]
    public static void Xml_DefaultNamespaceChangeTest()
    {
        Assert.StrictEqual(
        SerializeAndDeserialize<string>("Teststring",
@"<?xml version=""1.0""?>
<string xmlns=""MycustomDefaultNamespace"">Teststring</string>",
        () => { return new XmlSerializer(typeof(string), "MycustomDefaultNamespace"); }),
        "Teststring");
    }

    [Fact]
    public static void Xml_DefaultNamespaceChange_SimpleTypeAsRoot()
    {
        var value = new SimpleType { P1 = "abc", P2 = 11 };
        var o = SerializeAndDeserialize<SimpleType>(value,
@"<?xml version=""1.0""?>
<SimpleType xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""MycustomDefaultNamespace"">
  <P1>abc</P1>
  <P2>11</P2>
</SimpleType>",
        () => { return new XmlSerializer(typeof(SimpleType), "MycustomDefaultNamespace"); });
        Assert.StrictEqual(value.P1, o.P1);
        Assert.StrictEqual(value.P2, o.P2);
    }

    [Fact]
    public static void Xml_DefaultNamespaceChange_SimpleTypeAsRoot_WithXmlSerializerNamespaces()
    {
        var value = new SimpleType { P1 = "abc", P2 = 11 };
        XmlSerializerNamespaces xns = new XmlSerializerNamespaces();
        xns.Add("aa", "testNs");
        var o = SerializeAndDeserialize<SimpleType>(value,
@"<?xml version=""1.0""?>
<SimpleType xmlns:aa=""testNs"" xmlns=""MycustomDefaultNamespace"">
  <P1>abc</P1>
  <P2>11</P2>
</SimpleType>",
            () => { return new XmlSerializer(typeof(SimpleType), "MycustomDefaultNamespace"); }, xns: xns);
        Assert.StrictEqual(value.P1, o.P1);
        Assert.StrictEqual(value.P2, o.P2);
    }

    [Fact]
    public static void Xml_DefaultNamespaceChange_ArrayAsRoot()
    {
        SimpleType[] x = new SimpleType[] { new SimpleType { P1 = "abc", P2 = 11 }, new SimpleType { P1 = "def", P2 = 12 } };
        SimpleType[] y = SerializeAndDeserialize<SimpleType[]>(x,
@"<?xml version=""1.0""?>
<ArrayOfSimpleType xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""MycustomDefaultNamespace"">
  <SimpleType>
    <P1>abc</P1>
    <P2>11</P2>
  </SimpleType>
  <SimpleType>
    <P1>def</P1>
    <P2>12</P2>
  </SimpleType>
</ArrayOfSimpleType>",
            () => { return new XmlSerializer(typeof(SimpleType[]), "MycustomDefaultNamespace"); });

        Utils.Equal(x, y, (a, b) => { return SimpleType.AreEqual(a, b); });
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
    public static void XML_EnumerableCollection()
    {
        EnumerableCollection original = new EnumerableCollection();
        original.Add(new DateTime(100));
        original.Add(new DateTime(200));
        original.Add(new DateTime(300));
        EnumerableCollection actual = SerializeAndDeserialize<EnumerableCollection>(original,
@"<?xml version=""1.0""?><ArrayOfDateTime xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><dateTime>0001-01-01T00:00:00.00001</dateTime><dateTime>0001-01-01T00:00:00.00002</dateTime><dateTime>0001-01-01T00:00:00.00003</dateTime></ArrayOfDateTime>");

        Assert.Equal(actual, original);
    }

    [Fact]
    public static void Xml_SimpleCollectionDataContract()
    {
        var value = new SimpleCDC(true);
        var actual = SerializeAndDeserialize<SimpleCDC>(value,
@"<?xml version=""1.0""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>One</string>
  <string>Two</string>
  <string>Three</string>
</ArrayOfString>");

        Assert.True(value.Count == actual.Count);

        foreach (var item in value)
        {
            Assert.True(actual.Contains(item));
        }
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
        var serializer = new XmlSerializer(typeof (TypeWithAnyAttribute));
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
        var acutal = SerializeAndDeserialize<TypeWithXmlSchemaFormAttribute>(value,
@"<?xml version=""1.0""?><TypeWithXmlSchemaFormAttribute xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><UnqualifiedSchemaFormListProperty><int>1</int></UnqualifiedSchemaFormListProperty><NoneSchemaFormListProperty><NoneParameter>abc</NoneParameter></NoneSchemaFormListProperty><QualifiedSchemaFormListProperty><QualifiedParameter>true</QualifiedParameter></QualifiedSchemaFormListProperty></TypeWithXmlSchemaFormAttribute>");

        Assert.StrictEqual(value.NoneSchemaFormListProperty.Count, acutal.NoneSchemaFormListProperty.Count);
        Assert.StrictEqual(value.NoneSchemaFormListProperty[0], acutal.NoneSchemaFormListProperty[0]);
        Assert.StrictEqual(value.UnqualifiedSchemaFormListProperty.Count, acutal.UnqualifiedSchemaFormListProperty.Count);
        Assert.StrictEqual(value.UnqualifiedSchemaFormListProperty[0], acutal.UnqualifiedSchemaFormListProperty[0]);
        Assert.StrictEqual(value.QualifiedSchemaFormListProperty.Count, acutal.QualifiedSchemaFormListProperty.Count);
        Assert.StrictEqual(value.QualifiedSchemaFormListProperty[0], acutal.QualifiedSchemaFormListProperty[0]);
    }

    [Fact]
    public static void XML_TypeWithTypeNameInXmlTypeAttribute()
    {
        var value = new TypeWithTypeNameInXmlTypeAttribute();

        SerializeAndDeserialize<TypeWithTypeNameInXmlTypeAttribute>(value,
@"<?xml version=""1.0""?><MyXmlType xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" />");
    }

    [Fact]
    public static void XML_TypeWithMemberWithXmlNamespaceDeclarationsAttribute()
    {
        var original = new TypeWithMemberWithXmlNamespaceDeclarationsAttribute() { header = "foo", body = "bar" };

        var actual = SerializeAndDeserialize<TypeWithMemberWithXmlNamespaceDeclarationsAttribute>(original,
@"<?xml version=""1.0""?>
<Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://www.w3.org/2003/05/soap-envelope"">
  <header>foo</header>
  <body>bar</body>
</Envelope>");
        Assert.StrictEqual(original.header, actual.header);
        Assert.StrictEqual(original.body, actual.body);
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
    public static void Xml_XmlElementAsRoot()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml(@"<html></html>");
        XmlElement expected = xDoc.CreateElement("Element");
        expected.InnerText = "Element innertext";
        var actual = SerializeAndDeserialize(expected,
@"<?xml version=""1.0"" encoding=""utf-8""?><Element>Element innertext</Element>");
        Assert.NotNull(actual);
        Assert.StrictEqual(expected.InnerText, actual.InnerText);
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
    public static void Xml_XmlDocumentAsRoot()
    {
        XmlDocument expected = new XmlDocument();
        expected.LoadXml(@"<html><head>Head content</head><body><h1>Heading1</h1><div>Text in body</div></body></html>");
        var actual = SerializeAndDeserialize(expected,
@"<?xml version=""1.0"" encoding=""utf-8""?><html><head>Head content</head><body><h1>Heading1</h1><div>Text in body</div></body></html>");
        Assert.NotNull(actual);
        Assert.StrictEqual(expected.OuterXml, actual.OuterXml);
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
    public static void Xml_TestTypeWithListPropertiesWithoutPublicSetters()
    {
        var value = new TypeWithListPropertiesWithoutPublicSetters();
        value.PropertyWithXmlElementAttribute.Add("Item1");
        value.PropertyWithXmlElementAttribute.Add("Item2");
        value.IntList.Add(123);
        value.StringList.Add("Foo");
        value.StringList.Add("Bar");
        value.AnotherStringList.Add("AnotherFoo");
        value.PublicIntListField.Add(456);
        value.PublicIntListFieldWithXmlElementAttribute.Add(789);
        var actual = SerializeAndDeserialize<TypeWithListPropertiesWithoutPublicSetters>(value,
@"<?xml version=""1.0""?>
<TypeWithListPropertiesWithoutPublicSetters xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <PublicIntListField>
    <int>456</int>
  </PublicIntListField>
  <FieldWithXmlElementAttr>789</FieldWithXmlElementAttr>
  <PropWithXmlElementAttr>Item1</PropWithXmlElementAttr>
  <PropWithXmlElementAttr>Item2</PropWithXmlElementAttr>
  <IntList>
    <int>123</int>
  </IntList>
  <StringList>
    <string>Foo</string>
    <string>Bar</string>
  </StringList>
  <AnotherStringList>
    <string>AnotherFoo</string>
  </AnotherStringList>
</TypeWithListPropertiesWithoutPublicSetters>");
        Assert.StrictEqual(value.PropertyWithXmlElementAttribute.Count, actual.PropertyWithXmlElementAttribute.Count);
        Assert.StrictEqual(value.PropertyWithXmlElementAttribute[0], actual.PropertyWithXmlElementAttribute[0]);
        Assert.StrictEqual(value.PropertyWithXmlElementAttribute[1], actual.PropertyWithXmlElementAttribute[1]);
        Assert.StrictEqual(value.IntList.Count, actual.IntList.Count);
        Assert.StrictEqual(value.IntList[0], actual.IntList[0]);
        Assert.StrictEqual(value.StringList.Count, actual.StringList.Count);
        Assert.StrictEqual(value.StringList[0], actual.StringList[0]);
        Assert.StrictEqual(value.StringList[1], actual.StringList[1]);
        Assert.StrictEqual(value.AnotherStringList.Count, actual.AnotherStringList.Count);
        Assert.StrictEqual(value.AnotherStringList[0], actual.AnotherStringList[0]);
        Assert.StrictEqual(value.PublicIntListField[0], actual.PublicIntListField[0]);
        Assert.StrictEqual(value.PublicIntListFieldWithXmlElementAttribute[0], actual.PublicIntListFieldWithXmlElementAttribute[0]);
    }

    [Fact]
    public static void Xml_HighScoreManager()
    {
        List<HighScores.BridgeGameHighScore> value = new List<HighScores.BridgeGameHighScore>();
        HighScores.BridgeGameHighScore bghs = new HighScores.BridgeGameHighScore() { Id = 123, Name = "Foo" };
        value.Add(bghs);
        var actual = SerializeAndDeserialize<List<HighScores.BridgeGameHighScore>>(value,
@"<?xml version=""1.0""?>
<ArrayOfBridgeGameHighScore xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <BridgeGameHighScore>
    <Id>123</Id>
    <Name>Foo</Name>
  </BridgeGameHighScore>
</ArrayOfBridgeGameHighScore>");
        Assert.StrictEqual(1, actual.Count);
        Assert.StrictEqual(value[0].Id, actual[0].Id);
        Assert.StrictEqual(value[0].Name, actual[0].Name);
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
    public static void Xml_TypeWithNestedPublicType()
    {
        var value = new List<TypeWithNestedPublicType.LevelData>();
        value.Add(new TypeWithNestedPublicType.LevelData() { Name = "Foo" });
        value.Add(new TypeWithNestedPublicType.LevelData() { Name = "Bar" });
        var actual = SerializeAndDeserialize(value,
@"<?xml version=""1.0""?>
<ArrayOfLevelData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <LevelData>
    <Name>Foo</Name>
  </LevelData>
  <LevelData>
    <Name>Bar</Name>
  </LevelData>
</ArrayOfLevelData>");
        Assert.StrictEqual(2, actual.Count);
        Assert.StrictEqual(value[0].Name, actual[0].Name);
        Assert.StrictEqual(value[1].Name, actual[1].Name);
    }

    [Fact]
    public static void Xml_PublicTypeWithNestedPublicTypeWithNestedPublicType()
    {
        var value = new List<PublicTypeWithNestedPublicTypeWithNestedPublicType.NestedPublicType.LevelData>();
        value.Add(new PublicTypeWithNestedPublicTypeWithNestedPublicType.NestedPublicType.LevelData() { Name = "Foo" });
        value.Add(new PublicTypeWithNestedPublicTypeWithNestedPublicType.NestedPublicType.LevelData() { Name = "Bar" });
        var actual = SerializeAndDeserialize(value,
@"<?xml version=""1.0""?>
<ArrayOfLevelData xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <LevelData>
    <Name>Foo</Name>
  </LevelData>
  <LevelData>
    <Name>Bar</Name>
  </LevelData>
</ArrayOfLevelData>");
        Assert.StrictEqual(2, actual.Count);
        Assert.StrictEqual(value[0].Name, actual[0].Name);
        Assert.StrictEqual(value[1].Name, actual[1].Name);
    }

    [Fact]
    public static void Xml_TestDeserializingUnknownNode()
    {
        string xmlFileContent = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<ArrayOfSerializableSlide>
  <SerializableSlide>
    <ImageName>SecondAdventureImage</ImageName>
    <ImagePath></ImagePath>
    <Description>
      Available Now!
      Episode 2
    </Description>
    <EventType>LaunchSection</EventType>
    <EventData>Adventures.Episode2.Details</EventData>
  </SerializableSlide>
  <SerializableSlide>
    <ImageName>SecondAdventureImage</ImageName>
    <ImagePath></ImagePath>
    <Description>
      Available Now!
      Episode 2
    </Description>
    <EventType>LaunchSection</EventType>
    <EventData>Adventures.Episode2.Details</EventData>
  </SerializableSlide>
</ArrayOfSerializableSlide>";
        MemoryStream stream = new MemoryStream();
        StreamWriter sw = new StreamWriter(stream);
        sw.WriteLine(xmlFileContent);
        sw.Flush();
        XmlSerializer serializer = new XmlSerializer(typeof(List<SerializableSlide>));
        stream.Seek(0, SeekOrigin.Begin);
        List<SerializableSlide> actual = (List<SerializableSlide>)serializer.Deserialize(stream);
        Assert.StrictEqual(2, actual.Count);
        Assert.StrictEqual("SecondAdventureImage", actual[0].ImageName);
        Assert.StrictEqual(SlideEventType.LaunchSection, actual[0].EventType);
        Assert.StrictEqual("Adventures.Episode2.Details", actual[0].EventData);
        Assert.StrictEqual(actual[0].ImageName, actual[1].ImageName);
        Assert.StrictEqual(actual[0].EventType, actual[1].EventType);
        Assert.StrictEqual(actual[0].EventData, actual[1].EventData);
    }

    [Fact]
    public static void Xml_TypeWithNonParameterlessConstructor()
    {
        var obj = new TypeWithNonParameterlessConstructor("string value");
        Assert.Throws<InvalidOperationException>(() => { SerializeAndDeserialize(obj, string.Empty); });
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
    public static void Xml_FromTypes()
    {
        var serializers = XmlSerializer.FromTypes(new Type[] { typeof(Guid), typeof(List<string>) });
        Xml_GuidAsRoot(serializers[0]);
        Xml_ListGenericRoot(serializers[1]);

        serializers = XmlSerializer.FromTypes(null);
        Assert.Equal(0, serializers.Length);
    }

#if NET_NATIVE
#endif
    [Fact]
    public static void Xml_ConstructorWithXmlRootAttr()
    {
        var serializer = new XmlSerializer(typeof (List<string>), new XmlRootAttribute()
        {
            ElementName = "Places",
            Namespace = "http://www.microsoft.com",
        });
        var expected = new List<string>() { "Madison", "Rochester", null, "Arlington" };
        var actual = SerializeAndDeserialize(expected,
@"<?xml version=""1.0"" encoding=""utf-8""?><Places xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://www.microsoft.com""><string>Madison</string><string>Rochester</string><string xsi:nil=""true"" /><string>Arlington</string></Places>",
            () => serializer);
        Assert.True(expected.SequenceEqual(actual));
    }

#if NET_NATIVE
#endif
    [Fact]
    public static void Xml_ConstructorWithXmlAttributeOverrides()
    {
        var expected = new Music.Orchestra()
        {
            Instruments = new Music.Instrument[]
            {
                new Music.Brass() { Name = "Trumpet", IsValved = true },
                new Music.Brass() { Name = "Cornet", IsValved = true }
            }
        };
        var overrides = new XmlAttributeOverrides();
        overrides.Add(typeof (Music.Orchestra), "Instruments", new XmlAttributes()
        {
            XmlElements = {new XmlElementAttribute("Brass", typeof (Music.Brass))}
        });

        // XmlSerializer(Type, XmlAttributeOverrides)
        var serializer = new XmlSerializer(typeof (Music.Orchestra), overrides);
        var actual = SerializeAndDeserialize(expected,
@"<?xml version=""1.0"" encoding=""utf-8""?><Orchestra xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><Brass><Name>Trumpet</Name><IsValved>true</IsValved></Brass><Brass><Name>Cornet</Name><IsValved>true</IsValved></Brass></Orchestra>",
            () => serializer);
        Assert.StrictEqual(expected.Instruments[0].Name, actual.Instruments[0].Name);

        // XmlSerializer(Type, XmlAttributeOverrides, Type[], XmlRootAttribute, String)
        var root = new XmlRootAttribute("Collection");
        serializer = new XmlSerializer(typeof(Music.Orchestra), overrides, new Type[0], root, "defaultNamespace");
        actual = SerializeAndDeserialize(expected,
@"<?xml version=""1.0"" encoding=""utf-8""?><Collection xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""  xmlns=""defaultNamespace""><Brass><Name>Trumpet</Name><IsValved>true</IsValved></Brass><Brass><Name>Cornet</Name><IsValved>true</IsValved></Brass></Collection>",
            () => serializer);
        Assert.StrictEqual(expected.Instruments[0].Name, actual.Instruments[0].Name);

        Assert.Throws<ArgumentNullException>(() =>
        {
            new XmlSerializer(null, overrides);
        });
    }

    [Fact]
    public static void Xml_DifferentSerializeDeserializeOverloads()
    {
        var expected = new SimpleType() { P1 = "p1 value", P2 = 123 };
        var serializer = new XmlSerializer(typeof (SimpleType));
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
            var actual = (SimpleType) serializer.Deserialize(reader);
            Assert.StrictEqual(expected.P1, actual.P1);
            Assert.StrictEqual(expected.P2, actual.P2);

            stream.Dispose();
        }
    }

    [Fact]
    public static void Xml_TimeSpanAsRoot()
    {
        Assert.StrictEqual(new TimeSpan(1, 2, 3), SerializeAndDeserialize<TimeSpan>(new TimeSpan(1, 2, 3),
@"<?xml version=""1.0"" encoding=""utf-8""?>
<TimeSpan>PT1H2M3S</TimeSpan>"));
        Assert.StrictEqual(TimeSpan.Zero, SerializeAndDeserialize<TimeSpan>(TimeSpan.Zero,
@"<?xml version=""1.0"" encoding=""utf-8""?>
<TimeSpan>PT0S</TimeSpan>"));
        Assert.StrictEqual(TimeSpan.MinValue, SerializeAndDeserialize<TimeSpan>(TimeSpan.MinValue,
@"<?xml version=""1.0"" encoding=""utf-8""?>
<TimeSpan>-P10675199DT2H48M5.4775808S</TimeSpan>"));
        Assert.StrictEqual(TimeSpan.MaxValue, SerializeAndDeserialize<TimeSpan>(TimeSpan.MaxValue,
@"<?xml version=""1.0"" encoding=""utf-8""?>
<TimeSpan>P10675199DT2H48M5.4775807S</TimeSpan>"));
    }

    [Fact]
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

    [Fact]
    public static void Xml_TypeWithByteProperty()
    {
        var obj = new TypeWithByteProperty() {ByteProperty = 123};
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
    public static void Xml_XmlAttributes_RemoveXmlElementAttribute_ThrowsOnMissingItem()
    {
        XmlAttributes attrs = new XmlAttributes();

        XmlElementAttribute item1 = new XmlElementAttribute("elem1");
        attrs.XmlElements.Add(item1);

        XmlElementAttribute item2 = new XmlElementAttribute("elem2");
        attrs.XmlElements.Add(item2);
        Assert.True(attrs.XmlElements.Contains(item1));
        Assert.True(attrs.XmlElements.Contains(item2));

        attrs.XmlElements.Remove(item2);
        Assert.False(attrs.XmlElements.Contains(item2));

        Assert.Throws<ArgumentException>(() => { attrs.XmlElements.Remove(item2); });
    }

    [Fact]
    public static void Xml_XmlAttributes_RemoveXmlArrayItemAttribute()
    {
        XmlAttributes attrs = new XmlAttributes();

        XmlArrayItemAttribute item = new XmlArrayItemAttribute("item1");
        attrs.XmlArrayItems.Add(item);
        Assert.True(attrs.XmlArrayItems.Contains(item));

        attrs.XmlArrayItems.Remove(item);
        Assert.False(attrs.XmlArrayItems.Contains(item));
    }

    [Fact]
    public static void Xml_XmlAttributes_RemoveXmlArrayItemAttribute_ThrowsOnMissingItem()
    {
        XmlAttributes attrs = new XmlAttributes();

        XmlArrayItemAttribute item1 = new XmlArrayItemAttribute("item1");
        attrs.XmlArrayItems.Add(item1);

        XmlArrayItemAttribute item2 = new XmlArrayItemAttribute("item2");
        attrs.XmlArrayItems.Add(item2);
        Assert.True(attrs.XmlArrayItems.Contains(item1));
        Assert.True(attrs.XmlArrayItems.Contains(item2));

        attrs.XmlArrayItems.Remove(item2);
        Assert.False(attrs.XmlArrayItems.Contains(item2));

        Assert.Throws<ArgumentException>(() => { attrs.XmlArrayItems.Remove(item2); });
    }

    [Fact]
    public static void Xml_XmlAttributes_RemoveXmlAnyElementAttribute()
    {
        XmlAttributes attrs = new XmlAttributes();

        XmlAnyElementAttribute item = new XmlAnyElementAttribute("elem1");
        attrs.XmlAnyElements.Add(item);
        Assert.True(attrs.XmlAnyElements.Contains(item));

        attrs.XmlAnyElements.Remove(item);
        Assert.False(attrs.XmlAnyElements.Contains(item));
    }

    [Fact]
    public static void Xml_XmlAttributes_RemoveXmlAnyElementAttributeThrowsOnMissingItem()
    {
        XmlAttributes attrs = new XmlAttributes();

        XmlAnyElementAttribute item1 = new XmlAnyElementAttribute("elem1");
        attrs.XmlAnyElements.Add(item1);

        XmlAnyElementAttribute item2 = new XmlAnyElementAttribute("elem2");
        attrs.XmlAnyElements.Add(item2);
        Assert.True(attrs.XmlAnyElements.Contains(item1));
        Assert.True(attrs.XmlAnyElements.Contains(item2));

        attrs.XmlAnyElements.Remove(item2);
        Assert.False(attrs.XmlAnyElements.Contains(item2));

        Assert.Throws<ArgumentException>(() => { attrs.XmlAnyElements.Remove(item2); });
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
    public static void Xml_TypeWithTwoDimensionalArrayProperty1()
    {
        SimpleType[][] simpleType2D = GetObjectwith2DArrayOfSimpleType();

        var obj = new TypeWith2DArrayProperty1()
        {
            TwoDArrayOfSimpleType = simpleType2D
        };

        string baseline = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<TypeWith2DArrayProperty1 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <TwoDArrayOfSimpleType>\r\n    <ArrayOfSimpleType>\r\n      <SimpleType>\r\n        <P1>0 0 value</P1>\r\n        <P2>1</P2>\r\n      </SimpleType>\r\n      <SimpleType>\r\n        <P1>0 1 value</P1>\r\n        <P2>2</P2>\r\n      </SimpleType>\r\n    </ArrayOfSimpleType>\r\n    <ArrayOfSimpleType>\r\n      <SimpleType>\r\n        <P1>1 0 value</P1>\r\n        <P2>3</P2>\r\n      </SimpleType>\r\n      <SimpleType>\r\n        <P1>1 1 value</P1>\r\n        <P2>4</P2>\r\n      </SimpleType>\r\n    </ArrayOfSimpleType>\r\n  </TwoDArrayOfSimpleType>\r\n</TypeWith2DArrayProperty1>";
        TypeWith2DArrayProperty1 actual = SerializeAndDeserialize(obj, baseline);
        Assert.NotNull(actual);
        Assert.True(SimpleType.AreEqual(simpleType2D[0][0], actual.TwoDArrayOfSimpleType[0][0]));
        Assert.True(SimpleType.AreEqual(simpleType2D[0][1], actual.TwoDArrayOfSimpleType[0][1]));
        Assert.True(SimpleType.AreEqual(simpleType2D[1][0], actual.TwoDArrayOfSimpleType[1][0]));
        Assert.True(SimpleType.AreEqual(simpleType2D[1][1], actual.TwoDArrayOfSimpleType[1][1]));
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
    [ActiveIssue("fails when using CodeGen as well")]
    public static void Xml_BaseClassAndDerivedClass2WithSameProperty()
    {
        var value = new DerivedClassWithSameProperty2() { DateTimeProperty = new DateTime(100, DateTimeKind.Utc), IntProperty = 5, StringProperty = "TestString", ListProperty = new List<string>() };
        value.ListProperty.AddRange(new string[] { "one", "two", "three" });

        var actual = SerializeAndDeserialize(value,
@"<?xml version=""1.0""?>
<DerivedClassWithSameProperty2 xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <StringProperty>TestString</StringProperty>
  <IntProperty>5</IntProperty>
  <DateTimeProperty>0001-01-01T00:00:00.00001</DateTimeProperty>
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

        var value = new KnownTypesThroughConstructorWithArrayProperties() { IntArrayValue = intArray, StringArrayValue = stringArray};
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
    static void Xml_TypeWithTypesHavingCustomFormatter()
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
    public static void Xml_TypeWithXmlElementsAndUnnamedXmlAny()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml(@"<html></html>");
        XmlElement element = xDoc.CreateElement("Element");
        element.InnerText = "Element innertext";

        var value = new TypeWithXmlElementsAndUnnamedXmlAny()
        {
            Things = new object[] { 1, "2", element}
        };

        var actual = SerializeAndDeserialize(value,
           "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<MyXmlType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <int>1</int>\r\n  <string>2</string>\r\n  <Element>Element innertext</Element>\r\n</MyXmlType>");

        Assert.NotNull(actual);
        Assert.NotNull(actual.Things);
        Assert.Equal(value.Things.Length, actual.Things.Length);
        Assert.True(actual.Things[2] is XmlElement);

        var expectedElem = (XmlElement)value.Things[2];
        var actualElem = (XmlElement)actual.Things[2];
        Assert.Equal(expectedElem.Name, actualElem.Name);
        Assert.Equal(expectedElem.NamespaceURI, actualElem.NamespaceURI);
        Assert.Equal(expectedElem.InnerText, actualElem.InnerText);
    }

    [Fact]
    public static void Xml_TypeWithMultiXmlAnyElement()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml(@"<html></html>");
        XmlElement element1 = xDoc.CreateElement("name1", "ns1");
        element1.InnerText = "Element innertext1";
        XmlElement element2 = xDoc.CreateElement("name2", "ns2");
        element2.InnerText = "Element innertext2";

        var value = new TypeWithMultiNamedXmlAnyElement()
        {
            Things = new object[] { element1, element2 }
        };

        var actual = SerializeAndDeserialize(value,
           "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<MyXmlType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <name1 xmlns=\"ns1\">Element innertext1</name1>\r\n  <name2 xmlns=\"ns2\">Element innertext2</name2>\r\n</MyXmlType>");

        Assert.NotNull(actual);
        Assert.NotNull(actual.Things);
        Assert.Equal(value.Things.Length, actual.Things.Length);

        var expectedElem = (XmlElement)value.Things[1];
        var actualElem = (XmlElement)actual.Things[1];
        Assert.Equal(expectedElem.Name, actualElem.Name);
        Assert.Equal(expectedElem.NamespaceURI, actualElem.NamespaceURI);
        Assert.Equal(expectedElem.InnerText, actualElem.InnerText);

        XmlElement element3 = xDoc.CreateElement("name3", "ns3");
        element3.InnerText = "Element innertext3";
        value = new TypeWithMultiNamedXmlAnyElement()
        {
            Things = new object[] { element3 }
        };

        Assert.Throws<InvalidOperationException>(() => actual = SerializeAndDeserialize(value, string.Empty, skipStringCompare: true));
    }


    [Fact]
    public static void Xml_TypeWithMultiNamedXmlAnyElementAndOtherFields()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml(@"<html></html>");
        XmlElement element1 = xDoc.CreateElement("name1", "ns1");
        element1.InnerText = "Element innertext1";
        XmlElement element2 = xDoc.CreateElement("name2", "ns2");
        element2.InnerText = "Element innertext2";

        var value = new TypeWithMultiNamedXmlAnyElementAndOtherFields()
        {
            Things = new object[] { element1, element2 },
            StringField = "foo",
            IntField = 123
        };

        var actual = SerializeAndDeserialize(value,
           "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<MyXmlType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <name1 xmlns=\"ns1\">Element innertext1</name1>\r\n  <name2 xmlns=\"ns2\">Element innertext2</name2>\r\n  <StringField>foo</StringField>\r\n  <IntField>123</IntField>\r\n</MyXmlType>");

        Assert.NotNull(actual);
        Assert.NotNull(actual.Things);
        Assert.Equal(value.Things.Length, actual.Things.Length);

        var expectedElem = (XmlElement)value.Things[1];
        var actualElem = (XmlElement)actual.Things[1];
        Assert.Equal(expectedElem.Name, actualElem.Name);
        Assert.Equal(expectedElem.NamespaceURI, actualElem.NamespaceURI);
        Assert.Equal(expectedElem.InnerText, actualElem.InnerText);
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
    public static void XML_TypeWithArrayLikeXmlAttribute()
    {
        var value = new TypeWithStringArrayAsXmlAttribute() { XmlAttributeForms = new string[] { "SomeValue1", "SomeValue2" } };

        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<MyXmlType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" XmlAttributeForms=\"SomeValue1 SomeValue2\" />");

        Assert.NotNull(actual);
        Assert.True(Enumerable.SequenceEqual(value.XmlAttributeForms, actual.XmlAttributeForms));
    }

    [Fact]
    public static void XML_TypeWithArrayLikeXmlAttributeWithFields()
    {
        var value = new TypeWithArrayLikeXmlAttributeWithFields() { XmlAttributeForms = new string[] { "SomeValue1", "SomeValue2" }, StringField = "foo", IntField = 123 };

        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<MyXmlType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" XmlAttributeForms=\"SomeValue1 SomeValue2\">\r\n  <StringField>foo</StringField>\r\n  <IntField>123</IntField>\r\n</MyXmlType>");

        Assert.NotNull(actual);
        Assert.Equal(value.StringField, actual.StringField);
        Assert.Equal(value.IntField, actual.IntField);
        Assert.True(Enumerable.SequenceEqual(value.XmlAttributeForms, actual.XmlAttributeForms));
    }

    [Fact]
    public static void XML_TypeWithByteArrayAsXmlAttribute()
    {
        var value = new TypeWithByteArrayAsXmlAttribute() { XmlAttributeForms = new byte[] { 0, 1, 2 } };

        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<MyXmlType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" XmlAttributeForms=\"AAEC\" />");

        Assert.NotNull(actual);
        Assert.True(Enumerable.SequenceEqual(value.XmlAttributeForms, actual.XmlAttributeForms));
    }

    [Fact]
    public static void XML_TypeWithByteArrayArrayAsXmlAttribute()
    {
        var value = new TypeWithByteArrayArrayAsXmlAttribute() { XmlAttributeForms = new byte[][] { new byte[] { 1 }, new byte[] { 2 } } };

        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<MyXmlType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" XmlAttributeForms=\"AQ== Ag==\" />");

        Assert.NotNull(actual);
        Assert.Equal(value.XmlAttributeForms[0][0], actual.XmlAttributeForms[0][0]);
        Assert.Equal(value.XmlAttributeForms[1][0], actual.XmlAttributeForms[1][0]);
    }

    [Fact]
    public static void XML_TypeWithQNameArrayAsXmlAttribute()
    {
        var value = new TypeWithQNameArrayAsXmlAttribute() { XmlAttributeForms = new XmlQualifiedName[] { new XmlQualifiedName("SomeValue1", "ns1"), new XmlQualifiedName("SomeValue2", "ns2") } };

        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<MyXmlType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:q1=\"ns1\" xmlns:q2=\"ns2\" XmlAttributeForms=\"q1:SomeValue1 q2:SomeValue2\" />");

        Assert.NotNull(actual);
        Assert.True(Enumerable.SequenceEqual(value.XmlAttributeForms, actual.XmlAttributeForms));
    }

    [Fact]
    public static void XML_TypeWithEnumArrayAsXmlAttribute()
    {
        var value = new TypeWithEnumArrayAsXmlAttribute() { XmlAttributeForms = new IntEnum[] { IntEnum.Option1, IntEnum.Option2 } };

        var actual = SerializeAndDeserialize(value,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<MyXmlType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" XmlAttributeForms=\"Option1 Option2\" />");

        Assert.NotNull(actual);
        Assert.True(Enumerable.SequenceEqual(value.XmlAttributeForms, actual.XmlAttributeForms));
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
    public static void XmlSchemaTest()
    {
        var schemas = new XmlSchemas();
        var exporter = new XmlSchemaExporter(schemas);
        //Import the type as an XML mapping
        XmlTypeMapping originalmapping = new XmlReflectionImporter().ImportTypeMapping(typeof(Dog));
        //Export the XML mapping into schemas
        exporter.ExportTypeMapping(originalmapping);
        //Print out the schemas
        var schemaEnumerator = new XmlSchemaEnumerator(schemas);
        var ms = new MemoryStream();
        string baseline = "<?xml version=\"1.0\"?>\r\n<xs:schema elementFormDefault=\"qualified\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\r\n  <xs:element name=\"Dog\" nillable=\"true\" type=\"Dog\" />\r\n  <xs:complexType name=\"Dog\">\r\n    <xs:complexContent mixed=\"false\">\r\n      <xs:extension base=\"Animal\">\r\n        <xs:sequence>\r\n          <xs:element minOccurs=\"1\" maxOccurs=\"1\" name=\"breed\" type=\"DogBreed\" />\r\n        </xs:sequence>\r\n      </xs:extension>\r\n    </xs:complexContent>\r\n  </xs:complexType>\r\n  <xs:complexType name=\"Animal\">\r\n    <xs:sequence>\r\n      <xs:element minOccurs=\"1\" maxOccurs=\"1\" name=\"age\" type=\"xs:int\" />\r\n      <xs:element minOccurs=\"0\" maxOccurs=\"1\" name=\"name\" type=\"xs:string\" />\r\n    </xs:sequence>\r\n  </xs:complexType>\r\n  <xs:simpleType name=\"DogBreed\">\r\n    <xs:restriction base=\"xs:string\">\r\n      <xs:enumeration value=\"GermanShepherd\" />\r\n      <xs:enumeration value=\"LabradorRetriever\" />\r\n    </xs:restriction>\r\n  </xs:simpleType>\r\n</xs:schema>";
        schemaEnumerator.MoveNext();
        schemaEnumerator.Current.Write(ms);
        ms.Position = 0;
        string actualOutput = new StreamReader(ms).ReadToEnd();
        Utils.CompareResult result = Utils.Compare(baseline, actualOutput);
        Assert.True(result.Equal, string.Format("{1}{0}Test failed for wrong output from schema: {0}Expected: {2}{0}Actual: {3}",
                Environment.NewLine, result.ErrorMessage, baseline, actualOutput));
        Assert.False(schemaEnumerator.MoveNext());
        schemas.Compile((o, args) => {
            throw new InvalidOperationException(string.Format("{1}{0} Test failed because schema compile failed", Environment.NewLine, args.Message));
        }, true);
        var importer = new XmlSchemaImporter(schemas);
        ////Import the schema element back into an XML mapping
        XmlTypeMapping newmapping = importer.ImportTypeMapping(new XmlQualifiedName(originalmapping.ElementName, originalmapping.Namespace));
        Assert.NotNull(newmapping);
        Assert.Equal(originalmapping.ElementName, newmapping.ElementName);
        Assert.Equal(originalmapping.TypeFullName, newmapping.TypeFullName);
        Assert.Equal(originalmapping.XsdTypeName, newmapping.XsdTypeName);
        Assert.Equal(originalmapping.XsdTypeNamespace, newmapping.XsdTypeNamespace);
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
    public static void  SoapAttributeTests()
    {
        SoapAttributes soapAttrs = new SoapAttributes();
        SoapAttributeOverrides soapOverrides = new SoapAttributeOverrides();
        SoapElementAttribute soapElement1 = new SoapElementAttribute("Truck");
        soapAttrs.SoapElement = soapElement1;
        soapOverrides.Add(typeof(Transportation), "Vehicle", soapAttrs);
    }

    [ActiveIssue(12799)]
    [Fact]
    public static void XmlTypeMappingTest()
    {
        SoapAttributes soapAttrs = new SoapAttributes();
        SoapAttributeOverrides soapOverrides = new SoapAttributeOverrides();
        SoapElementAttribute soapElement1 = new SoapElementAttribute("Truck");
        soapAttrs.SoapElement = soapElement1;
        soapOverrides.Add(typeof(Transportation), "Vehicle", soapAttrs);
        XmlTypeMapping myTypeMapping = (new SoapReflectionImporter(soapOverrides)).ImportTypeMapping(typeof(Transportation));
        XmlSerializer ser = new XmlSerializer(myTypeMapping);
        Transportation myTransportation = new Transportation();
        myTransportation.Vehicle = "MyCar";
        myTransportation.CreationDate = DateTime.Now;
        myTransportation.thing = new Thing();
        using (MemoryStream ms = new MemoryStream())
        {
            ser.Serialize(ms, myTransportation);
        }
    }

    [Fact]
    public static void XmlSerializationReaderWriterTest()
    {
        string s = "XmlSerializationReaderWriterTest";
        byte[] original = System.Text.Encoding.Default.GetBytes(s);
        byte[] converted = MyReader.HexToBytes(MyWriter.BytesToHex(original));
        Assert.Equal(original, converted);
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
}
