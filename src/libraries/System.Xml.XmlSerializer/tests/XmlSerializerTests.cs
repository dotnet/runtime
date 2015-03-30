// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using SerializationTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;


public class XmlSerializerTests
{
    [Fact]
    public static void Xml_BoolAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<bool>(true, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<boolean>true</boolean>"), true);
        Assert.StrictEqual(SerializeAndDeserialize<bool>(false, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<boolean>false</boolean>"), false);
    }

    [Fact]
    public static void Xml_ByteArrayAsRoot()
    {
        Assert.Null(SerializeAndDeserialize<byte[]>(null, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<base64Binary d1p1:nil=\"true\" xmlns:d1p1=\"http://www.w3.org/2001/XMLSchema-instance\" />"));
        byte[] x = new byte[] { 1, 2 };
        byte[] y = SerializeAndDeserialize<byte[]>(x, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<base64Binary>AQI=</base64Binary>");
        Assert.Equal(x, y);
    }

    [Fact]
    public static void Xml_CharAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<char>(char.MinValue, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<char>0</char>"), char.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<char>(char.MaxValue, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<char>65535</char>"), char.MaxValue);
        Assert.StrictEqual(SerializeAndDeserialize<char>('a', "<?xml version=\"1.0\"?>" + Environment.NewLine + "<char>97</char>"), 'a');
        Assert.StrictEqual(SerializeAndDeserialize<char>('ñ', "<?xml version=\"1.0\"?>" + Environment.NewLine + "<char>241</char>"), 'ñ');
        Assert.StrictEqual(SerializeAndDeserialize<char>('漢', "<?xml version=\"1.0\"?>" + Environment.NewLine + "<char>28450</char>"), '漢');
    }

    [Fact]
    public static void Xml_ByteAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<byte>(10, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<unsignedByte>10</unsignedByte>"), 10);
        Assert.StrictEqual(SerializeAndDeserialize<byte>(byte.MinValue, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<unsignedByte>0</unsignedByte>"), byte.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<byte>(byte.MaxValue, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<unsignedByte>255</unsignedByte>"), byte.MaxValue);
    }

    [Fact]
    public static void Xml_DateTimeAsRoot()
    {
        var offsetMinutes = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes;
        var timeZoneString = string.Format("{0:+;-}{1}", offsetMinutes, new TimeSpan(0, offsetMinutes, 0).ToString(@"hh\:mm"));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2), "<?xml version=\"1.0\"?>" + Environment.NewLine + "<dateTime>2013-01-02T00:00:00</dateTime>"), new DateTime(2013, 1, 2));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local), string.Format("<?xml version=\"1.0\"?>" + Environment.NewLine + "<dateTime>2013-01-02T03:04:05.006{0}</dateTime>", timeZoneString)), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified), "<?xml version=\"1.0\"?>" + Environment.NewLine + "<dateTime>2013-01-02T03:04:05.006</dateTime>"), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc), "<?xml version=\"1.0\"?>" + Environment.NewLine + "<dateTime>2013-01-02T03:04:05.006Z</dateTime>"), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), "<?xml version=\"1.0\"?>" + Environment.NewLine + "<dateTime>0001-01-01T00:00:00Z</dateTime>"), DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), "<?xml version=\"1.0\"?>" + Environment.NewLine + "<dateTime>9999-12-31T23:59:59.9999999Z</dateTime>"), DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
    }

    [Fact]
    public static void Xml_DecimalAsRoot()
    {
        foreach (decimal value in new decimal[] { -1.2m, 0.0m, 2.3m, decimal.MinValue, decimal.MaxValue })
        {
            var baseline = string.Format("<?xml version=\"1.0\"?>" + Environment.NewLine + "<decimal>{0}</decimal>", value.ToString(CultureInfo.InvariantCulture));
            Assert.StrictEqual(SerializeAndDeserialize<decimal>(value, baseline), value);
        }
    }

    [Fact]
    public static void Xml_DoubleAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<double>(-1.2, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<double>-1.2</double>"), -1.2);
        Assert.StrictEqual(SerializeAndDeserialize<double>(0, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<double>0</double>"), 0);
        Assert.StrictEqual(SerializeAndDeserialize<double>(2.3, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<double>2.3</double>"), 2.3);
        Assert.StrictEqual(SerializeAndDeserialize<double>(double.MinValue, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<double>-1.7976931348623157E+308</double>"), double.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<double>(double.MaxValue, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<double>1.7976931348623157E+308</double>"), double.MaxValue);
    }

    [Fact]
    public static void Xml_FloatAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)-1.2, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<float>-1.2</float>"), (float)-1.2);
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)0, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<float>0</float>"), (float)0);
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)2.3, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<float>2.3</float>"), (float)2.3);
        Assert.StrictEqual(SerializeAndDeserialize<float>(float.MinValue, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<float>-3.40282347E+38</float>"), float.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<float>(float.MaxValue, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<float>3.40282347E+38</float>"), float.MaxValue);
    }

    [Fact]
    public static void Xml_GuidAsRoot()
    {
        foreach (Guid value in new Guid[] { Guid.NewGuid(), Guid.Empty })
        {
            Assert.StrictEqual(SerializeAndDeserialize<Guid>(value, string.Format("<?xml version=\"1.0\"?>" + Environment.NewLine + "<guid>{0}</guid>", value.ToString())), value);
        }
    }

    [Fact]
    public static void Xml_IntAsRoot()
    {
        foreach (int value in new int[] { -1, 0, 2, int.MinValue, int.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<int>(value, string.Format("<?xml version=\"1.0\"?>" + Environment.NewLine + "<int>{0}</int>", value)), value);
        }
    }

    [Fact]
    public static void Xml_LongAsRoot()
    {
        foreach (long value in new long[] { (long)-1, (long)0, (long)2, long.MinValue, long.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<long>(value, string.Format("<?xml version=\"1.0\"?>" + Environment.NewLine + "<long>{0}</long>", value)), value);
        }
    }

    [Fact]
    public static void Xml_ObjectAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<object>(1, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<anyType xmlns:q1=\"http://www.w3.org/2001/XMLSchema\" d1p1:type=\"q1:int\" xmlns:d1p1=\"http://www.w3.org/2001/XMLSchema-instance\">1</anyType>"), 1);
        Assert.StrictEqual(SerializeAndDeserialize<object>(true, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<anyType xmlns:q1=\"http://www.w3.org/2001/XMLSchema\" d1p1:type=\"q1:boolean\" xmlns:d1p1=\"http://www.w3.org/2001/XMLSchema-instance\">true</anyType>"), true);
        Assert.StrictEqual(SerializeAndDeserialize<object>("abc", "<?xml version=\"1.0\"?>" + Environment.NewLine + "<anyType xmlns:q1=\"http://www.w3.org/2001/XMLSchema\" d1p1:type=\"q1:string\" xmlns:d1p1=\"http://www.w3.org/2001/XMLSchema-instance\">abc</anyType>"), "abc");
        Assert.StrictEqual(SerializeAndDeserialize<object>(null, "<?xml version=\"1.0\"?><anyType xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\" />"), null);
    }

    [Fact]
    public static void Xml_XmlQualifiedNameAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<XmlQualifiedName>(new XmlQualifiedName("abc", "def"), "<?xml version=\"1.0\"?>" + Environment.NewLine + "<QName xmlns:q1=\"def\">q1:abc</QName>"), new XmlQualifiedName("abc", "def"));
        Assert.StrictEqual(SerializeAndDeserialize<XmlQualifiedName>(XmlQualifiedName.Empty, "<?xml version=\"1.0\"?><QName xmlns=\"\" />"), XmlQualifiedName.Empty);
    }

    [Fact]
    public static void Xml_ShortAsRoot()
    {
        foreach (short value in new short[] { (short)-1.2, (short)0, (short)2.3, short.MinValue, short.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<short>(value, string.Format("<?xml version=\"1.0\"?>" + Environment.NewLine + "<short>{0}</short>", value)), value);
        }
    }

    [Fact]
    public static void Xml_SbyteAsRoot()
    {
        foreach (sbyte value in new sbyte[] { (sbyte)3, (sbyte)0, sbyte.MinValue, sbyte.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<sbyte>(value, string.Format("<?xml version=\"1.0\"?>" + Environment.NewLine + "<byte>{0}</byte>", value)), value);
        }
    }

    [Fact]
    public static void Xml_StringAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<string>("abc", "<?xml version=\"1.0\"?>" + Environment.NewLine + "<string>abc</string>"), "abc");
        Assert.StrictEqual(SerializeAndDeserialize<string>("  a b  ", "<?xml version=\"1.0\"?>" + Environment.NewLine + "<string>  a b  </string>"), "  a b  ");
        Assert.StrictEqual(SerializeAndDeserialize<string>(null, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<string d1p1:nil=\"true\" xmlns:d1p1=\"http://www.w3.org/2001/XMLSchema-instance\" />"), null);
        Assert.StrictEqual(SerializeAndDeserialize<string>("", "<?xml version=\"1.0\"?>" + Environment.NewLine + "<string />"), "");
        Assert.StrictEqual(SerializeAndDeserialize<string>(" ", "<?xml version=\"1.0\"?>" + Environment.NewLine + "<string> </string>"), "");
        Assert.StrictEqual(SerializeAndDeserialize<string>("Hello World! 漢 ñ", "<?xml version=\"1.0\"?>" + Environment.NewLine + "<string>Hello World! 漢 ñ</string>"), "Hello World! 漢 ñ");
    }

    [Fact]
    public static void Xml_UintAsRoot()
    {
        foreach (uint value in new uint[] { (uint)3, (uint)0, uint.MinValue, uint.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<uint>(value, string.Format("<?xml version=\"1.0\"?>" + Environment.NewLine + "<unsignedInt>{0}</unsignedInt>", value)), value);
        }
    }

    [Fact]
    public static void Xml_UlongAsRoot()
    {
        foreach (ulong value in new ulong[] { (ulong)3, (ulong)0, ulong.MinValue, ulong.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<ulong>(value, string.Format("<?xml version=\"1.0\"?>" + Environment.NewLine + "<unsignedLong>{0}</unsignedLong>", value)), value);
        }
    }

    [Fact]
    public static void Xml_UshortAsRoot()
    {
        foreach (ushort value in new ushort[] { (ushort)3, (ushort)0, ushort.MinValue, ushort.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<ushort>(value, string.Format("<?xml version=\"1.0\"?>" + Environment.NewLine + "<unsignedShort>{0}</unsignedShort>", value)), value);
        }
    }

    [Fact]
    public static void Xml_ArrayAsRoot()
    {
        SimpleType[] x = new SimpleType[] { new SimpleType { P1 = "abc", P2 = 11 }, new SimpleType { P1 = "def", P2 = 12 } };
        SimpleType[] y = SerializeAndDeserialize<SimpleType[]>(x, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ArrayOfSimpleType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <SimpleType>\r\n    <P1>abc</P1>\r\n    <P2>11</P2>\r\n  </SimpleType>\r\n  <SimpleType>\r\n    <P1>def</P1>\r\n    <P2>12</P2>\r\n  </SimpleType>" + Environment.NewLine + "</ArrayOfSimpleType>");

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
        TypeWithGetSetArrayMembers y = SerializeAndDeserialize<TypeWithGetSetArrayMembers>(x, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<TypeWithGetSetArrayMembers xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <F1>\r\n    <SimpleType>\r\n      <P1>ab</P1>\r\n      <P2>1</P2>\r\n    </SimpleType>\r\n    <SimpleType>\r\n      <P1>cd</P1>\r\n      <P2>2</P2>\r\n    </SimpleType>\r\n  </F1>\r\n  <F2>\r\n    <int>-1</int>\r\n    <int>3</int>\r\n  </F2>\r\n  <P1>\r\n    <SimpleType>\r\n      <P1>ef</P1>\r\n      <P2>5</P2>\r\n    </SimpleType>\r\n    <SimpleType>\r\n      <P1>gh</P1>\r\n      <P2>7</P2>\r\n    </SimpleType>\r\n  </P1>\r\n  <P2>\r\n    <int>11</int>\r\n    <int>12</int>\r\n  </P2>" + Environment.NewLine + "</TypeWithGetSetArrayMembers>");

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

        TypeWithGetOnlyArrayProperties y = SerializeAndDeserialize<TypeWithGetOnlyArrayProperties>(x, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<TypeWithGetOnlyArrayProperties xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" />");

        Assert.NotNull(y);
        // XmlSerializer seems not complain about missing public setter of Array property
        // However, it does not serialize the property. So for this test case, I'll use it to verify there are no complaints about missing public setter
    }

    [Fact]
    public static void Xml_ListGenericRoot()
    {
        List<string> x = new List<string>();
        x.Add("zero");
        x.Add("one");

        List<string> y = SerializeAndDeserialize<List<string>>(x, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <string>zero</string>\r\n  <string>one</string>" + Environment.NewLine + "</ArrayOfString>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True(y[0] == "zero");
        Assert.True(y[1] == "one");
    }

    [Fact]
    public static void Xml_CollectionGenericRoot()
    {
        MyCollection<string> x = new MyCollection<string>("a1", "a2");
        MyCollection<string> y = SerializeAndDeserialize<MyCollection<string>>(x, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <string>a1</string>\r\n  <string>a2</string>" + Environment.NewLine + "</ArrayOfString>");

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
        MyList y = SerializeAndDeserialize<MyList>(x, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ArrayOfAnyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <anyType xsi:type=\"xsd:string\">a1</anyType>\r\n  <anyType xsi:type=\"xsd:string\">a2</anyType>" + Environment.NewLine + "</ArrayOfAnyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.StrictEqual((string)x[0], (string)y[0]);
        Assert.StrictEqual((string)x[1], (string)y[1]);
    }

    [Fact]
    public static void Xml_EnumerableGenericRoot()
    {
        MyEnumerable<string> x = new MyEnumerable<string>("a1", "a2");
        MyEnumerable<string> y = SerializeAndDeserialize<MyEnumerable<string>>(x, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <string>a1</string>\r\n  <string>a2</string>" + Environment.NewLine + "</ArrayOfString>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);

        string itemsInY = string.Join("", y);
        Assert.StrictEqual("a1a2", itemsInY);
    }

    [Fact]
    public static void Xml_CollectionRoot()
    {
        MyCollection x = new MyCollection('a', 45);
        MyCollection y = SerializeAndDeserialize<MyCollection>(x, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ArrayOfAnyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <anyType xmlns:q1=\"http://microsoft.com/wsdl/types/\" xsi:type=\"q1:char\">97</anyType>\r\n  <anyType xsi:type=\"xsd:int\">45</anyType>" + Environment.NewLine + "</ArrayOfAnyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((char)y[0] == 'a');
        Assert.True((int)y[1] == 45);
    }

    [Fact]
    public static void Xml_EnumerableRoot()
    {
        MyEnumerable x = new MyEnumerable("abc", 3);
        MyEnumerable y = SerializeAndDeserialize<MyEnumerable>(x, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ArrayOfAnyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <anyType xsi:type=\"xsd:string\">abc</anyType>\r\n  <anyType xsi:type=\"xsd:int\">3</anyType>" + Environment.NewLine + "</ArrayOfAnyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((string)y[0] == "abc");
        Assert.True((int)y[1] == 3);
    }

    [Fact]
    public static void Xml_EnumAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<MyEnum>(MyEnum.Two, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<MyEnum>Two</MyEnum>"), MyEnum.Two);
        Assert.StrictEqual(SerializeAndDeserialize<ByteEnum>(ByteEnum.Option1, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ByteEnum>Option1</ByteEnum>"), ByteEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<SByteEnum>(SByteEnum.Option1, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<SByteEnum>Option1</SByteEnum>"), SByteEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<ShortEnum>(ShortEnum.Option1, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ShortEnum>Option1</ShortEnum>"), ShortEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<IntEnum>(IntEnum.Option1, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<IntEnum>Option1</IntEnum>"), IntEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<UIntEnum>(UIntEnum.Option1, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<UIntEnum>Option1</UIntEnum>"), UIntEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<LongEnum>(LongEnum.Option1, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<LongEnum>Option1</LongEnum>"), LongEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<ULongEnum>(ULongEnum.Option1, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ULongEnum>Option1</ULongEnum>"), ULongEnum.Option1);
    }

    [Fact]
    public static void Xml_EnumAsMember()
    {
        TypeWithEnumMembers x = new TypeWithEnumMembers { F1 = MyEnum.Three, P1 = MyEnum.Two };
        TypeWithEnumMembers y = SerializeAndDeserialize<TypeWithEnumMembers>(x, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<TypeWithEnumMembers xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <F1>Three</F1>\r\n  <P1>Two</P1>" + Environment.NewLine + "</TypeWithEnumMembers>");

        Assert.NotNull(y);
        Assert.StrictEqual(x.F1, y.F1);
        Assert.StrictEqual(x.P1, y.P1);
    }

    [Fact]
    public static void Xml_DCClassWithEnumAndStruct()
    {
        DCClassWithEnumAndStruct value = new DCClassWithEnumAndStruct(true);
        DCClassWithEnumAndStruct actual = SerializeAndDeserialize<DCClassWithEnumAndStruct>(value, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<DCClassWithEnumAndStruct xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <MyStruct>\r\n    <Data>Data</Data>\r\n  </MyStruct>\r\n  <MyEnum1>One</MyEnum1>" + Environment.NewLine + "</DCClassWithEnumAndStruct>");

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
        BuiltInTypes y = SerializeAndDeserialize<BuiltInTypes>(x, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<BuiltInTypes xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <ByteArray>AQI=</ByteArray>" + Environment.NewLine + "</BuiltInTypes>");

        Assert.NotNull(y);
        Assert.Equal(x.ByteArray, y.ByteArray);
    }

    [Fact]
    public static void Xml_GenericBase()
    {
        SerializeAndDeserialize<GenericBase2<SimpleBaseDerived, SimpleBaseDerived2>>(new GenericBase2<SimpleBaseDerived, SimpleBaseDerived2>(true), "<?xml version=\"1.0\"?>" + Environment.NewLine + "<GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <genericData1>\r\n    <BaseData />\r\n    <DerivedData />\r\n  </genericData1>\r\n  <genericData2>\r\n    <BaseData />\r\n    <DerivedData />\r\n  </genericData2>" + Environment.NewLine + "</GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2>");
    }

    [Fact]
    public static void Xml_TypesWithArrayOfOtherTypes()
    {
        SerializeAndDeserialize<TypeHasArrayOfASerializedAsB>(new TypeHasArrayOfASerializedAsB(true), "<?xml version=\"1.0\"?>" + Environment.NewLine + "<TypeHasArrayOfASerializedAsB xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Items>\r\n    <TypeA>\r\n      <Name>typeAValue</Name>\r\n    </TypeA>\r\n    <TypeA>\r\n      <Name>typeBValue</Name>\r\n    </TypeA>\r\n  </Items>" + Environment.NewLine + "</TypeHasArrayOfASerializedAsB>");
    }

    [Fact]
    public static void Xml_WithXElement()
    {
        var original = new WithXElement(true);
        var actual = SerializeAndDeserialize<WithXElement>(original, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<WithXElement xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <e>\r\n    <ElementName1 Attribute1=\"AttributeValue1\">Value1</ElementName1>\r\n  </e>" + Environment.NewLine + "</WithXElement>");

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
        var actual = SerializeAndDeserialize<WithXElementWithNestedXElement>(original, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<WithXElementWithNestedXElement xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <e1>\r\n    <ElementName1 Attribute1=\"AttributeValue1\">\r\n      <ElementName2 Attribute2=\"AttributeValue2\">Value2</ElementName2>\r\n    </ElementName1>\r\n  </e1>" + Environment.NewLine + "</WithXElementWithNestedXElement>");

        VerifyXElementObject(original.e1, actual.e1);
        VerifyXElementObject((XElement)original.e1.FirstNode, (XElement)actual.e1.FirstNode);
    }

    [Fact]
    public static void Xml_WithArrayOfXElement()
    {
        var original = new WithArrayOfXElement(true);
        var actual = SerializeAndDeserialize<WithArrayOfXElement>(original, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<WithArrayOfXElement xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <a>\r\n    <XElement>\r\n      <item xmlns=\"http://p.com/\">item0</item>\r\n    </XElement>\r\n    <XElement>\r\n      <item xmlns=\"http://p.com/\">item1</item>\r\n    </XElement>\r\n    <XElement>\r\n      <item xmlns=\"http://p.com/\">item2</item>\r\n    </XElement>\r\n  </a>" + Environment.NewLine + "</WithArrayOfXElement>");

        Assert.StrictEqual(original.a.Length, actual.a.Length);
        VerifyXElementObject(original.a[0], actual.a[0], checkFirstAttribute: false);
        VerifyXElementObject(original.a[1], actual.a[1], checkFirstAttribute: false);
        VerifyXElementObject(original.a[2], actual.a[2], checkFirstAttribute: false);
    }

    [Fact]
    public static void Xml_WithListOfXElement()
    {
        var original = new WithListOfXElement(true);
        var actual = SerializeAndDeserialize<WithListOfXElement>(original, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<WithListOfXElement xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <list>\r\n    <XElement>\r\n      <item xmlns=\"http://p.com/\">item0</item>\r\n    </XElement>\r\n    <XElement>\r\n      <item xmlns=\"http://p.com/\">item1</item>\r\n    </XElement>\r\n    <XElement>\r\n      <item xmlns=\"http://p.com/\">item2</item>\r\n    </XElement>\r\n  </list>" + Environment.NewLine + "</WithListOfXElement>");

        Assert.StrictEqual(original.list.Count, actual.list.Count);
        VerifyXElementObject(original.list[0], actual.list[0], checkFirstAttribute: false);
        VerifyXElementObject(original.list[1], actual.list[1], checkFirstAttribute: false);
        VerifyXElementObject(original.list[2], actual.list[2], checkFirstAttribute: false);
    }

    [Fact]
    public static void Xml_TypeNamesWithSpecialCharacters()
    {
        SerializeAndDeserialize<__TypeNameWithSpecialCharacters漢ñ>(new __TypeNameWithSpecialCharacters漢ñ() { PropertyNameWithSpecialCharacters漢ñ = "Test" }, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<__TypeNameWithSpecialCharacters漢ñ xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <PropertyNameWithSpecialCharacters漢ñ>Test</PropertyNameWithSpecialCharacters漢ñ>" + Environment.NewLine + "</__TypeNameWithSpecialCharacters漢ñ>");
    }

    [Fact]
    public static void Xml_JaggedArrayAsRoot()
    {
        int[][] jaggedIntegerArray = new int[][] { new int[] { 1, 3, 5, 7, 9 }, new int[] { 0, 2, 4, 6 }, new int[] { 11, 22 } };
        int[][] actualJaggedIntegerArray = SerializeAndDeserialize<int[][]>(jaggedIntegerArray, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ArrayOfArrayOfInt xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <ArrayOfInt>\r\n    <int>1</int>\r\n    <int>3</int>\r\n    <int>5</int>\r\n    <int>7</int>\r\n    <int>9</int>\r\n  </ArrayOfInt>\r\n  <ArrayOfInt>\r\n    <int>0</int>\r\n    <int>2</int>\r\n    <int>4</int>\r\n    <int>6</int>\r\n  </ArrayOfInt>\r\n  <ArrayOfInt>\r\n    <int>11</int>\r\n    <int>22</int>\r\n  </ArrayOfInt>" + Environment.NewLine + "</ArrayOfArrayOfInt>");
        Assert.Equal(jaggedIntegerArray[0], actualJaggedIntegerArray[0]);
        Assert.Equal(jaggedIntegerArray[1], actualJaggedIntegerArray[1]);
        Assert.Equal(jaggedIntegerArray[2], actualJaggedIntegerArray[2]);


        string[][] jaggedStringArray = new string[][] { new string[] { "1", "3", "5", "7", "9" }, new string[] { "0", "2", "4", "6" }, new string[] { "11", "22" } };
        string[][] actualJaggedStringArray = SerializeAndDeserialize<string[][]>(jaggedStringArray, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ArrayOfArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <ArrayOfString>\r\n    <string>1</string>\r\n    <string>3</string>\r\n    <string>5</string>\r\n    <string>7</string>\r\n    <string>9</string>\r\n  </ArrayOfString>\r\n  <ArrayOfString>\r\n    <string>0</string>\r\n    <string>2</string>\r\n    <string>4</string>\r\n    <string>6</string>\r\n  </ArrayOfString>\r\n  <ArrayOfString>\r\n    <string>11</string>\r\n    <string>22</string>\r\n  </ArrayOfString>" + Environment.NewLine + "</ArrayOfArrayOfString>");
        Assert.Equal(jaggedStringArray[0], actualJaggedStringArray[0]);
        Assert.Equal(jaggedStringArray[1], actualJaggedStringArray[1]);
        Assert.Equal(jaggedStringArray[2], actualJaggedStringArray[2]);


        object[] objectArray = new object[] { 1, 1.0F, 1.0, "string", Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860"), new DateTime(2013, 1, 2) };
        object[] actualObjectArray = SerializeAndDeserialize<object[]>(objectArray, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ArrayOfAnyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <anyType xsi:type=\"xsd:int\">1</anyType>\r\n  <anyType xsi:type=\"xsd:float\">1</anyType>\r\n  <anyType xsi:type=\"xsd:double\">1</anyType>\r\n  <anyType xsi:type=\"xsd:string\">string</anyType>\r\n  <anyType xmlns:q1=\"http://microsoft.com/wsdl/types/\" xsi:type=\"q1:guid\">2054fd3e-e118-476a-9962-1a882be51860</anyType>\r\n  <anyType xsi:type=\"xsd:dateTime\">2013-01-02T00:00:00</anyType>" + Environment.NewLine + "</ArrayOfAnyType>");
        Assert.True(1 == (int)actualObjectArray[0]);
        Assert.True(1.0F == (float)actualObjectArray[1]);
        Assert.True(1.0 == (double)actualObjectArray[2]);
        Assert.True("string" == (string)actualObjectArray[3]);
        Assert.True(Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860") == (Guid)actualObjectArray[4]);
        Assert.True(new DateTime(2013, 1, 2) == (DateTime)actualObjectArray[5]);


        int[][][] jaggedIntegerArray2 = new int[][][] { new int[][] { new int[] { 1 }, new int[] { 3 } }, new int[][] { new int[] { 0 } }, new int[][] { new int[] { } } };
        int[][][] actualJaggedIntegerArray2 = SerializeAndDeserialize<int[][][]>(jaggedIntegerArray2, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ArrayOfArrayOfArrayOfInt xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <ArrayOfArrayOfInt>\r\n    <ArrayOfInt>\r\n      <int>1</int>\r\n    </ArrayOfInt>\r\n    <ArrayOfInt>\r\n      <int>3</int>\r\n    </ArrayOfInt>\r\n  </ArrayOfArrayOfInt>\r\n  <ArrayOfArrayOfInt>\r\n    <ArrayOfInt>\r\n      <int>0</int>\r\n    </ArrayOfInt>\r\n  </ArrayOfArrayOfInt>\r\n  <ArrayOfArrayOfInt>\r\n    <ArrayOfInt />\r\n  </ArrayOfArrayOfInt>" + Environment.NewLine + "</ArrayOfArrayOfArrayOfInt>");

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
        SerializeAndDeserialize<string>("Teststring", "<?xml version=\"1.0\"?>" + Environment.NewLine + "<string xmlns=\"MycustomDefaultNamespace\">Teststring</string>",
        () => { return new XmlSerializer(typeof(string), "MycustomDefaultNamespace"); }),
        "Teststring");
    }

    [Fact]
    public static void Xml_KnownTypesThroughConstructor()
    {
        KnownTypesThroughConstructor value = new KnownTypesThroughConstructor() { EnumValue = MyEnum.One, SimpleTypeValue = new SimpleKnownTypeValue() { StrProperty = "PropertyValue" } };
        KnownTypesThroughConstructor actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<KnownTypesThroughConstructor xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <EnumValue xsi:type=\"MyEnum\">One</EnumValue>\r\n  <SimpleTypeValue xsi:type=\"SimpleKnownTypeValue\">\r\n    <StrProperty>PropertyValue</StrProperty>\r\n  </SimpleTypeValue>" + Environment.NewLine + "</KnownTypesThroughConstructor>", () => { return new XmlSerializer(typeof(KnownTypesThroughConstructor), new Type[] { typeof(MyEnum), typeof(SimpleKnownTypeValue) }); });

        Assert.StrictEqual((MyEnum)value.EnumValue, (MyEnum)actual.EnumValue);
        Assert.StrictEqual(((SimpleKnownTypeValue)value.SimpleTypeValue).StrProperty, ((SimpleKnownTypeValue)actual.SimpleTypeValue).StrProperty);
    }

    [Fact]
    public static void Xml_BaseClassAndDerivedClassWithSameProperty()
    {
        DerivedClassWithSameProperty value = new DerivedClassWithSameProperty() { DateTimeProperty = new DateTime(100), IntProperty = 5, StringProperty = "TestString", ListProperty = new List<string>() };
        value.ListProperty.AddRange(new string[] { "one", "two", "three" });

        DerivedClassWithSameProperty actual = SerializeAndDeserialize<DerivedClassWithSameProperty>(value, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<DerivedClassWithSameProperty xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <StringProperty>TestString</StringProperty>\r\n  <IntProperty>5</IntProperty>\r\n  <DateTimeProperty>0001-01-01T00:00:00.00001</DateTimeProperty>\r\n  <ListProperty>\r\n    <string>one</string>\r\n    <string>two</string>\r\n    <string>three</string>\r\n  </ListProperty>" + Environment.NewLine + "</DerivedClassWithSameProperty>");

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
        EnumerableCollection actual = SerializeAndDeserialize<EnumerableCollection>(original, @"<?xml version=""1.0""?><ArrayOfDateTime xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><dateTime>0001-01-01T00:00:00.00001</dateTime><dateTime>0001-01-01T00:00:00.00002</dateTime><dateTime>0001-01-01T00:00:00.00003</dateTime></ArrayOfDateTime>");

        Assert.Equal(actual, original);
    }

    [Fact]
    public static void Xml_SimpleCollectionDataContract()
    {
        var value = new SimpleCDC(true);
        var actual = SerializeAndDeserialize<SimpleCDC>(value, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <string>One</string>\r\n  <string>Two</string>\r\n  <string>Three</string>" + Environment.NewLine + "</ArrayOfString>");

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
        var value2 = SerializeAndDeserialize<EnumFlags>(value1, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<EnumFlags>One Four</EnumFlags>");
        Assert.StrictEqual(value1, value2);
    }

    [Fact]
    public static void Xml_SerializeClassThatImplementsInteface()
    {
        ClassImplementsInterface value = new ClassImplementsInterface() { ClassID = "ClassID", DisplayName = "DisplayName", Id = "Id", IsLoaded = true };
        ClassImplementsInterface actual = SerializeAndDeserialize<ClassImplementsInterface>(value, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ClassImplementsInterface xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <ClassID>ClassID</ClassID>\r\n  <DisplayName>DisplayName</DisplayName>\r\n  <Id>Id</Id>\r\n  <IsLoaded>true</IsLoaded>" + Environment.NewLine + "</ClassImplementsInterface>");

        Assert.StrictEqual(value.ClassID, actual.ClassID);
        Assert.StrictEqual(value.DisplayName, actual.DisplayName);
        Assert.StrictEqual(value.Id, actual.Id);
        Assert.StrictEqual(value.IsLoaded, actual.IsLoaded);
    }


    [Fact]
    public static void Xml_XmlAttributesTest()
    {
        var value = new XmlSerializerAttributes();
        var actual = SerializeAndDeserialize(value, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<AttributeTesting xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" XmlAttributeName=\"2\">\r\n  <Word>String choice value</Word>\r\n  <XmlIncludeProperty xsi:type=\"ItemChoiceType\">DecimalNumber</XmlIncludeProperty>\r\n  <XmlEnumProperty>\r\n    <ItemChoiceType>DecimalNumber</ItemChoiceType>\r\n    <ItemChoiceType>Number</ItemChoiceType>\r\n    <ItemChoiceType>Word</ItemChoiceType>\r\n    <ItemChoiceType>None</ItemChoiceType>\r\n  </XmlEnumProperty>&lt;xml&gt;Hello XML&lt;/xml&gt;<XmlNamespaceDeclarationsProperty>XmlNamespaceDeclarationsPropertyValue</XmlNamespaceDeclarationsProperty><XmlElementPropertyNode xmlns=\"http://element\">1</XmlElementPropertyNode><CustomXmlArrayProperty xmlns=\"http://mynamespace\"><string>one</string><string>two</string><string>three</string></CustomXmlArrayProperty></AttributeTesting>");

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
    public static void Xml_Struct()
    {
        var value = new WithStruct { Some = new SomeStruct { A = 1, B = 2 } };
        var result = SerializeAndDeserialize(value, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<WithStruct xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Some>\r\n    <A>1</A>\r\n    <B>2</B>\r\n  </Some>" + Environment.NewLine + "</WithStruct>");

        // Assert
        Assert.StrictEqual(result.Some.A, value.Some.A);
        Assert.StrictEqual(result.Some.B, value.Some.B);
    }

    [Fact]
    public static void Xml_Enums()
    {
        var item = new WithEnums() { Int = IntEnum.Option1, Short = ShortEnum.Option2 };
        var actual = SerializeAndDeserialize(item, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<WithEnums xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Int>Option1</Int>\r\n  <Short>Option2</Short>" + Environment.NewLine + "</WithEnums>");
        Assert.StrictEqual(item.Short, actual.Short);
        Assert.StrictEqual(item.Int, actual.Int);
    }

    [Fact]
    public static void Xml_Nullables()
    {
        var item = new WithNullables() { Optional = IntEnum.Option1, OptionalInt = 42, Struct1 = new SomeStruct { A = 1, B = 2 } };
        var actual = SerializeAndDeserialize(item, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<WithNullables xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Optional>Option1</Optional>\r\n  <Optionull xsi:nil=\"true\" />\r\n  <OptionalInt>42</OptionalInt>\r\n  <OptionullInt xsi:nil=\"true\" />\r\n  <Struct1>\r\n    <A>1</A>\r\n    <B>2</B>\r\n  </Struct1>\r\n  <Struct2 xsi:nil=\"true\" />" + Environment.NewLine + "</WithNullables>");
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
        var actual = SerializeAndDeserialize<ClassImplementingIXmlSerialiable>(value, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<ClassImplementingIXmlSerialiable StringValue=\"Hello world\" BoolValue=\"True\" />");
        Assert.StrictEqual(value.StringValue, actual.StringValue);
        Assert.StrictEqual(value.GetPrivateMember(), actual.GetPrivateMember());
        Assert.True(ClassImplementingIXmlSerialiable.ReadXmlInvoked);
        Assert.True(ClassImplementingIXmlSerialiable.WriteXmlInvoked);
    }

    [Fact]
    public static void Xml_TypeWithFieldNameEndBySpecified()
    {
        var value = new TypeWithPropertyNameSpecified() { MyField = "MyField", MyFieldIgnored = 99, MyFieldSpecified = true, MyFieldIgnoredSpecified = false };
        var actual = SerializeAndDeserialize<TypeWithPropertyNameSpecified>(value, "<?xml version=\"1.0\"?><TypeWithPropertyNameSpecified xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><MyField>MyField</MyField></TypeWithPropertyNameSpecified>");
        Assert.StrictEqual(value.MyField, actual.MyField);
        Assert.StrictEqual(actual.MyFieldIgnored, 0);
    }

    [Fact]
    public static void XML_TypeWithXmlSchemaFormAttribute()
    {
        var value = new TypeWithXmlSchemaFormAttribute() { NoneSchemaFormListProperty = new List<string> { "abc" }, QualifiedSchemaFormListProperty = new List<bool> { true }, UnqualifiedSchemaFormListProperty = new List<int> { 1 } };
        var acutal = SerializeAndDeserialize<TypeWithXmlSchemaFormAttribute>(value, "<?xml version=\"1.0\"?><TypeWithXmlSchemaFormAttribute xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><UnqualifiedSchemaFormListProperty><int>1</int></UnqualifiedSchemaFormListProperty><NoneSchemaFormListProperty><NoneParameter>abc</NoneParameter></NoneSchemaFormListProperty><QualifiedSchemaFormListProperty><QualifiedParameter>true</QualifiedParameter></QualifiedSchemaFormListProperty></TypeWithXmlSchemaFormAttribute>");

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

        SerializeAndDeserialize<TypeWithTypeNameInXmlTypeAttribute>(value, "<?xml version=\"1.0\"?><MyXmlType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" />");
    }

    [Fact]
    public static void XML_TypeWithMemberWithXmlNamespaceDeclarationsAttribute()
    {
        var original = new TypeWithMemberWithXmlNamespaceDeclarationsAttribute() { header = "foo", body = "bar" };

        var actual = SerializeAndDeserialize<TypeWithMemberWithXmlNamespaceDeclarationsAttribute>(original, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://www.w3.org/2003/05/soap-envelope\">\r\n  <header>foo</header>\r\n  <body>bar</body>" + Environment.NewLine + "</Envelope>");
        Assert.StrictEqual(original.header, actual.header);
        Assert.StrictEqual(original.body, actual.body);
    }

    [Fact]
    public static void XML_TypeWithXmlTextAttributeOnArray()
    {
        var original = new TypeWithXmlTextAttributeOnArray() { Text = new string[] { "val1", "val2" } };

        var actual = SerializeAndDeserialize<TypeWithXmlTextAttributeOnArray>(original, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<TypeWithXmlTextAttributeOnArray xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\">val1val2</TypeWithXmlTextAttributeOnArray>");
        Assert.NotNull(actual.Text);
        Assert.StrictEqual(1, actual.Text.Length);
        Assert.StrictEqual("val1val2", actual.Text[0]);
    }

    [Fact]
    public static void Xml_TypeWithSchemaFormInXmlAttribute()
    {
        var value = new TypeWithSchemaFormInXmlAttribute() { TestProperty = "hello" };
        var actual = SerializeAndDeserialize<TypeWithSchemaFormInXmlAttribute>(value, "<?xml version=\"1.0\"?><TypeWithSchemaFormInXmlAttribute xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" d1p1:TestProperty=\"hello\" xmlns:d1p1=\"http://test.com\" />");
        Assert.StrictEqual(value.TestProperty, actual.TestProperty);
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
    public static void TestIgnoreWhitespaceForDeserialization()
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
    public static void TestTypeWithListPropertiesWithoutPublicSetters()
    {
        var value = new TypeWithListPropertiesWithoutPublicSetters();
        value.IntList.Add(12345);
        value.StringList.Add("Foo");
        value.StringList.Add("Bar");
        value.AnotherStringList.Add("AnotherFoo");
        var actual = SerializeAndDeserialize<TypeWithListPropertiesWithoutPublicSetters>(value, @"<?xml version=""1.0""?>
<TypeWithListPropertiesWithoutPublicSetters xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <IntList>
    <int>12345</int>
  </IntList>
  <StringList>
    <string>Foo</string>
    <string>Bar</string>
  </StringList>
  <AnotherStringList>
    <string>AnotherFoo</string>
  </AnotherStringList>
</TypeWithListPropertiesWithoutPublicSetters>");
        Assert.StrictEqual(1, actual.IntList.Count);
        Assert.StrictEqual(12345, actual.IntList[0]);
        Assert.StrictEqual(2, actual.StringList.Count);
        Assert.StrictEqual("Foo", actual.StringList[0]);
        Assert.StrictEqual("Bar", actual.StringList[1]);
        Assert.StrictEqual(1, actual.AnotherStringList.Count);
        Assert.StrictEqual("AnotherFoo", actual.AnotherStringList[0]);
    }

    [Fact]
    public static void Xml_HighScoreManager()
    {
        List<HighScores.BridgeGameHighScore> value = new List<HighScores.BridgeGameHighScore>();
        HighScores.BridgeGameHighScore bghs = new HighScores.BridgeGameHighScore() { Id = 123, Name = "Foo" };
        value.Add(bghs);
        var actual = SerializeAndDeserialize<List<HighScores.BridgeGameHighScore>>(value, @"<?xml version=""1.0""?>
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
        var actual = SerializeAndDeserialize(value, "<?xml version=\"1.0\"?><RootElement xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" IntValue=\"120\" />");
        Assert.StrictEqual(value.IntValue, actual.IntValue);
    }

    private static T SerializeAndDeserialize<T>(T value, string baseline, Func<XmlSerializer> serializerFactory = null)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        if (serializerFactory != null)
        {
            serializer = serializerFactory();
        }

        Console.WriteLine("Testing input value : {0}", value);

        using (MemoryStream ms = new MemoryStream())
        {
            serializer.Serialize(ms, value);
            ms.Position = 0;

            string actualOutput = new StreamReader(ms).ReadToEnd();

            Utils.CompareResult result = Utils.Compare(baseline, actualOutput);

            if (!result.Equal)
            {
                Console.WriteLine(result.ErrorMessage);
                throw new Exception(string.Format("Test failed for input : {0}", value));
            }

            ms.Position = 0;
            T deserialized;
            try
            {
                deserialized = (T)serializer.Deserialize(ms);
            }
            catch
            {
                Console.WriteLine("Error deserializing value. the serialized string was:\r\n" + actualOutput);
                throw;
            }

            return deserialized;
        }
    }
}
