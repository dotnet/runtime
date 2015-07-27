// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using SerializationTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(new DateTime(2013, 1, 2)).TotalMinutes;
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
        Xml_GuidAsRoot(new XmlSerializer(typeof(Guid)));
    }

     private static void Xml_GuidAsRoot(XmlSerializer serializer)
     {
         foreach (Guid value in new Guid[] { Guid.NewGuid(), Guid.Empty })
         {
             Assert.StrictEqual(SerializeAndDeserialize<Guid>(value, string.Format("<?xml version=\"1.0\"?>" + Environment.NewLine + "<guid>{0}</guid>", value.ToString()), () => serializer), value);
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
        SimpleType[] y = SerializeAndDeserialize<SimpleType[]>(x,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ArrayOfSimpleType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <SimpleType>" + Environment.NewLine +
            "    <P1>abc</P1>" + Environment.NewLine +
            "    <P2>11</P2>" + Environment.NewLine +
            "  </SimpleType>" + Environment.NewLine +
            "  <SimpleType>" + Environment.NewLine +
            "    <P1>def</P1>" + Environment.NewLine +
            "    <P2>12</P2>" + Environment.NewLine +
            "  </SimpleType>" + Environment.NewLine +
            "</ArrayOfSimpleType>");

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
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<TypeWithGetSetArrayMembers xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <F1>" + Environment.NewLine +
            "    <SimpleType>" + Environment.NewLine +
            "      <P1>ab</P1>" + Environment.NewLine +
            "      <P2>1</P2>" + Environment.NewLine +
            "    </SimpleType>" + Environment.NewLine +
            "    <SimpleType>" + Environment.NewLine +
            "      <P1>cd</P1>" + Environment.NewLine +
            "      <P2>2</P2>" + Environment.NewLine +
            "    </SimpleType>" + Environment.NewLine +
            "  </F1>" + Environment.NewLine +
            "  <F2>" + Environment.NewLine +
            "    <int>-1</int>" + Environment.NewLine +
            "    <int>3</int>" + Environment.NewLine +
            "  </F2>" + Environment.NewLine +
            "  <P1>" + Environment.NewLine +
            "    <SimpleType>" + Environment.NewLine +
            "      <P1>ef</P1>" + Environment.NewLine +
            "      <P2>5</P2>" + Environment.NewLine +
            "    </SimpleType>" + Environment.NewLine +
            "    <SimpleType>" + Environment.NewLine +
            "      <P1>gh</P1>" + Environment.NewLine +
            "      <P2>7</P2>" + Environment.NewLine +
            "    </SimpleType>" + Environment.NewLine +
            "  </P1>" + Environment.NewLine +
            "  <P2>" + Environment.NewLine +
            "    <int>11</int>" + Environment.NewLine +
            "    <int>12</int>" + Environment.NewLine +
            "  </P2>" + Environment.NewLine +
            "</TypeWithGetSetArrayMembers>");

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
        Xml_ListGenericRoot(new XmlSerializer(typeof(List<string>)));
    }

    private static void Xml_ListGenericRoot(XmlSerializer serializer)
    {
        List<string> x = new List<string>();
        x.Add("zero");
        x.Add("one");

        List<string> y = SerializeAndDeserialize<List<string>>(x,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <string>zero</string>" + Environment.NewLine +
            "  <string>one</string>" + Environment.NewLine +
            "</ArrayOfString>", () => serializer);

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
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <string>a1</string>" + Environment.NewLine +
            "  <string>a2</string>" + Environment.NewLine +
            "</ArrayOfString>");

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
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ArrayOfAnyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <anyType xsi:type=\"xsd:string\">a1</anyType>" + Environment.NewLine +
            "  <anyType xsi:type=\"xsd:string\">a2</anyType>" + Environment.NewLine +
            "</ArrayOfAnyType>");

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
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <string>a1</string>" + Environment.NewLine +
            "  <string>a2</string>" + Environment.NewLine +
            "</ArrayOfString>");

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
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ArrayOfAnyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <anyType xmlns:q1=\"http://microsoft.com/wsdl/types/\" xsi:type=\"q1:char\">97</anyType>" + Environment.NewLine +
            "  <anyType xsi:type=\"xsd:int\">45</anyType>" + Environment.NewLine +
            "</ArrayOfAnyType>");

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
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ArrayOfAnyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <anyType xsi:type=\"xsd:string\">abc</anyType>" + Environment.NewLine +
            "  <anyType xsi:type=\"xsd:int\">3</anyType>" + Environment.NewLine +
            "</ArrayOfAnyType>");

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
        TypeWithEnumMembers y = SerializeAndDeserialize<TypeWithEnumMembers>(x,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<TypeWithEnumMembers xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <F1>Three</F1>" + Environment.NewLine +
            "  <P1>Two</P1>" + Environment.NewLine +
            "</TypeWithEnumMembers>");

        Assert.NotNull(y);
        Assert.StrictEqual(x.F1, y.F1);
        Assert.StrictEqual(x.P1, y.P1);
    }

    [Fact]
    public static void Xml_DCClassWithEnumAndStruct()
    {
        DCClassWithEnumAndStruct value = new DCClassWithEnumAndStruct(true);
        DCClassWithEnumAndStruct actual = SerializeAndDeserialize<DCClassWithEnumAndStruct>(value,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<DCClassWithEnumAndStruct xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <MyStruct>" + Environment.NewLine +
            "    <Data>Data</Data>" + Environment.NewLine +
            "  </MyStruct>" + Environment.NewLine +
            "  <MyEnum1>One</MyEnum1>" + Environment.NewLine +
            "</DCClassWithEnumAndStruct>");

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
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<BuiltInTypes xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <ByteArray>AQI=</ByteArray>" + Environment.NewLine +
            "</BuiltInTypes>");

        Assert.NotNull(y);
        Assert.Equal(x.ByteArray, y.ByteArray);
    }

    [Fact]
    public static void Xml_GenericBase()
    {
        SerializeAndDeserialize<GenericBase2<SimpleBaseDerived, SimpleBaseDerived2>>(new GenericBase2<SimpleBaseDerived, SimpleBaseDerived2>(true),
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <genericData1>" + Environment.NewLine +
            "    <BaseData />" + Environment.NewLine +
            "    <DerivedData />" + Environment.NewLine +
            "  </genericData1>" + Environment.NewLine +
            "  <genericData2>" + Environment.NewLine +
            "    <BaseData />" + Environment.NewLine +
            "    <DerivedData />" + Environment.NewLine +
            "  </genericData2>" + Environment.NewLine +
            "</GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2>");
    }

    [Fact]
    public static void Xml_TypesWithArrayOfOtherTypes()
    {
        SerializeAndDeserialize<TypeHasArrayOfASerializedAsB>(new TypeHasArrayOfASerializedAsB(true),
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<TypeHasArrayOfASerializedAsB xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <Items>" + Environment.NewLine +
            "    <TypeA>" + Environment.NewLine +
            "      <Name>typeAValue</Name>" + Environment.NewLine +
            "    </TypeA>" + Environment.NewLine +
            "    <TypeA>" + Environment.NewLine +
            "      <Name>typeBValue</Name>" + Environment.NewLine +
            "    </TypeA>" + Environment.NewLine +
            "  </Items>" + Environment.NewLine +
            "</TypeHasArrayOfASerializedAsB>");
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void Xml_WithXElement()
    {
        var original = new WithXElement(true);
        var actual = SerializeAndDeserialize<WithXElement>(original,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<WithXElement xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <e>" + Environment.NewLine +
            "    <ElementName1 Attribute1=\"AttributeValue1\">Value1</ElementName1>" + Environment.NewLine +
            "  </e>" + Environment.NewLine +
            "</WithXElement>");

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
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void Xml_WithXElementWithNestedXElement()
    {
        var original = new WithXElementWithNestedXElement(true);
        var actual = SerializeAndDeserialize<WithXElementWithNestedXElement>(original,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<WithXElementWithNestedXElement xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <e1>" + Environment.NewLine +
            "    <ElementName1 Attribute1=\"AttributeValue1\">" + Environment.NewLine +
            "      <ElementName2 Attribute2=\"AttributeValue2\">Value2</ElementName2>" + Environment.NewLine +
            "    </ElementName1>" + Environment.NewLine +
            "  </e1>" + Environment.NewLine +
            "</WithXElementWithNestedXElement>");

        VerifyXElementObject(original.e1, actual.e1);
        VerifyXElementObject((XElement)original.e1.FirstNode, (XElement)actual.e1.FirstNode);
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void Xml_WithArrayOfXElement()
    {
        var original = new WithArrayOfXElement(true);
        var actual = SerializeAndDeserialize<WithArrayOfXElement>(original,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<WithArrayOfXElement xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <a>" + Environment.NewLine +
            "    <XElement>" + Environment.NewLine +
            "      <item xmlns=\"http://p.com/\">item0</item>" + Environment.NewLine +
            "    </XElement>" + Environment.NewLine +
            "    <XElement>" + Environment.NewLine +
            "      <item xmlns=\"http://p.com/\">item1</item>" + Environment.NewLine +
            "    </XElement>" + Environment.NewLine +
            "    <XElement>" + Environment.NewLine +
            "      <item xmlns=\"http://p.com/\">item2</item>" + Environment.NewLine +
            "    </XElement>" + Environment.NewLine +
            "  </a>" + Environment.NewLine +
            "</WithArrayOfXElement>");

        Assert.StrictEqual(original.a.Length, actual.a.Length);
        VerifyXElementObject(original.a[0], actual.a[0], checkFirstAttribute: false);
        VerifyXElementObject(original.a[1], actual.a[1], checkFirstAttribute: false);
        VerifyXElementObject(original.a[2], actual.a[2], checkFirstAttribute: false);
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void Xml_WithListOfXElement()
    {
        var original = new WithListOfXElement(true);
        var actual = SerializeAndDeserialize<WithListOfXElement>(original,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<WithListOfXElement xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <list>" + Environment.NewLine +
            "    <XElement>" + Environment.NewLine +
            "      <item xmlns=\"http://p.com/\">item0</item>" + Environment.NewLine +
            "    </XElement>" + Environment.NewLine +
            "    <XElement>" + Environment.NewLine +
            "      <item xmlns=\"http://p.com/\">item1</item>" + Environment.NewLine +
            "    </XElement>" + Environment.NewLine +
            "    <XElement>" + Environment.NewLine +
            "      <item xmlns=\"http://p.com/\">item2</item>" + Environment.NewLine +
            "    </XElement>" + Environment.NewLine +
            "  </list>" + Environment.NewLine +
            "</WithListOfXElement>");

        Assert.StrictEqual(original.list.Count, actual.list.Count);
        VerifyXElementObject(original.list[0], actual.list[0], checkFirstAttribute: false);
        VerifyXElementObject(original.list[1], actual.list[1], checkFirstAttribute: false);
        VerifyXElementObject(original.list[2], actual.list[2], checkFirstAttribute: false);
    }

    [Fact]
    public static void Xml_TypeNamesWithSpecialCharacters()
    {
        SerializeAndDeserialize<__TypeNameWithSpecialCharacters漢ñ>(new __TypeNameWithSpecialCharacters漢ñ() { PropertyNameWithSpecialCharacters漢ñ = "Test" },
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<__TypeNameWithSpecialCharacters漢ñ xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <PropertyNameWithSpecialCharacters漢ñ>Test</PropertyNameWithSpecialCharacters漢ñ>" + Environment.NewLine +
            "</__TypeNameWithSpecialCharacters漢ñ>");
    }

    [Fact]
    public static void Xml_JaggedArrayAsRoot()
    {
        int[][] jaggedIntegerArray = new int[][] { new int[] { 1, 3, 5, 7, 9 }, new int[] { 0, 2, 4, 6 }, new int[] { 11, 22 } };
        int[][] actualJaggedIntegerArray = SerializeAndDeserialize<int[][]>(jaggedIntegerArray,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ArrayOfArrayOfInt xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <ArrayOfInt>" + Environment.NewLine +
            "    <int>1</int>" + Environment.NewLine +
            "    <int>3</int>" + Environment.NewLine +
            "    <int>5</int>" + Environment.NewLine +
            "    <int>7</int>" + Environment.NewLine +
            "    <int>9</int>" + Environment.NewLine +
            "  </ArrayOfInt>" + Environment.NewLine +
            "  <ArrayOfInt>" + Environment.NewLine +
            "    <int>0</int>" + Environment.NewLine +
            "    <int>2</int>" + Environment.NewLine +
            "    <int>4</int>" + Environment.NewLine +
            "    <int>6</int>" + Environment.NewLine +
            "  </ArrayOfInt>" + Environment.NewLine +
            "  <ArrayOfInt>" + Environment.NewLine +
            "    <int>11</int>" + Environment.NewLine +
            "    <int>22</int>" + Environment.NewLine +
            "  </ArrayOfInt>" + Environment.NewLine +
            "</ArrayOfArrayOfInt>");
        Assert.Equal(jaggedIntegerArray[0], actualJaggedIntegerArray[0]);
        Assert.Equal(jaggedIntegerArray[1], actualJaggedIntegerArray[1]);
        Assert.Equal(jaggedIntegerArray[2], actualJaggedIntegerArray[2]);


        string[][] jaggedStringArray = new string[][] { new string[] { "1", "3", "5", "7", "9" }, new string[] { "0", "2", "4", "6" }, new string[] { "11", "22" } };
        string[][] actualJaggedStringArray = SerializeAndDeserialize<string[][]>(jaggedStringArray,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ArrayOfArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <ArrayOfString>" + Environment.NewLine +
            "    <string>1</string>" + Environment.NewLine +
            "    <string>3</string>" + Environment.NewLine +
            "    <string>5</string>" + Environment.NewLine +
            "    <string>7</string>" + Environment.NewLine +
            "    <string>9</string>" + Environment.NewLine +
            "  </ArrayOfString>" + Environment.NewLine +
            "  <ArrayOfString>" + Environment.NewLine +
            "    <string>0</string>" + Environment.NewLine +
            "    <string>2</string>" + Environment.NewLine +
            "    <string>4</string>" + Environment.NewLine +
            "    <string>6</string>" + Environment.NewLine +
            "  </ArrayOfString>" + Environment.NewLine +
            "  <ArrayOfString>" + Environment.NewLine +
            "    <string>11</string>" + Environment.NewLine +
            "    <string>22</string>" + Environment.NewLine +
            "  </ArrayOfString>" + Environment.NewLine +
            "</ArrayOfArrayOfString>");
        Assert.Equal(jaggedStringArray[0], actualJaggedStringArray[0]);
        Assert.Equal(jaggedStringArray[1], actualJaggedStringArray[1]);
        Assert.Equal(jaggedStringArray[2], actualJaggedStringArray[2]);


        object[] objectArray = new object[] { 1, 1.0F, 1.0, "string", Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860"), new DateTime(2013, 1, 2) };
        object[] actualObjectArray = SerializeAndDeserialize<object[]>(objectArray,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ArrayOfAnyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <anyType xsi:type=\"xsd:int\">1</anyType>" + Environment.NewLine +
            "  <anyType xsi:type=\"xsd:float\">1</anyType>" + Environment.NewLine +
            "  <anyType xsi:type=\"xsd:double\">1</anyType>" + Environment.NewLine +
            "  <anyType xsi:type=\"xsd:string\">string</anyType>" + Environment.NewLine +
            "  <anyType xmlns:q1=\"http://microsoft.com/wsdl/types/\" xsi:type=\"q1:guid\">2054fd3e-e118-476a-9962-1a882be51860</anyType>" + Environment.NewLine +
            "  <anyType xsi:type=\"xsd:dateTime\">2013-01-02T00:00:00</anyType>" + Environment.NewLine +
            "</ArrayOfAnyType>");
        Assert.True(1 == (int)actualObjectArray[0]);
        Assert.True(1.0F == (float)actualObjectArray[1]);
        Assert.True(1.0 == (double)actualObjectArray[2]);
        Assert.True("string" == (string)actualObjectArray[3]);
        Assert.True(Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860") == (Guid)actualObjectArray[4]);
        Assert.True(new DateTime(2013, 1, 2) == (DateTime)actualObjectArray[5]);


        int[][][] jaggedIntegerArray2 = new int[][][] { new int[][] { new int[] { 1 }, new int[] { 3 } }, new int[][] { new int[] { 0 } }, new int[][] { new int[] { } } };
        int[][][] actualJaggedIntegerArray2 = SerializeAndDeserialize<int[][][]>(jaggedIntegerArray2,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ArrayOfArrayOfArrayOfInt xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <ArrayOfArrayOfInt>" + Environment.NewLine +
            "    <ArrayOfInt>" + Environment.NewLine +
            "      <int>1</int>" + Environment.NewLine +
            "    </ArrayOfInt>" + Environment.NewLine +
            "    <ArrayOfInt>" + Environment.NewLine +
            "      <int>3</int>" + Environment.NewLine +
            "    </ArrayOfInt>" + Environment.NewLine +
            "  </ArrayOfArrayOfInt>" + Environment.NewLine +
            "  <ArrayOfArrayOfInt>" + Environment.NewLine +
            "    <ArrayOfInt>" + Environment.NewLine +
            "      <int>0</int>" + Environment.NewLine +
            "    </ArrayOfInt>" + Environment.NewLine +
            "  </ArrayOfArrayOfInt>" + Environment.NewLine +
            "  <ArrayOfArrayOfInt>" + Environment.NewLine +
            "    <ArrayOfInt />" + Environment.NewLine +
            "  </ArrayOfArrayOfInt>" + Environment.NewLine +
            "</ArrayOfArrayOfArrayOfInt>");

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

        Assert.Throws<ArgumentNullException>(() =>
        {
            new XmlSerializer(null, "defaultNamespace");
        });
    }

    [Fact]
    public static void Xml_KnownTypesThroughConstructor()
    {
        KnownTypesThroughConstructor value = new KnownTypesThroughConstructor() { EnumValue = MyEnum.One, SimpleTypeValue = new SimpleKnownTypeValue() { StrProperty = "PropertyValue" } };
        KnownTypesThroughConstructor actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<KnownTypesThroughConstructor xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <EnumValue xsi:type=\"MyEnum\">One</EnumValue>" + Environment.NewLine +
            "  <SimpleTypeValue xsi:type=\"SimpleKnownTypeValue\">" + Environment.NewLine +
            "    <StrProperty>PropertyValue</StrProperty>" + Environment.NewLine +
            "  </SimpleTypeValue>" + Environment.NewLine +
            "</KnownTypesThroughConstructor>",
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
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<DerivedClassWithSameProperty xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <StringProperty>TestString</StringProperty>" + Environment.NewLine +
            "  <IntProperty>5</IntProperty>" + Environment.NewLine +
            "  <DateTimeProperty>0001-01-01T00:00:00.00001</DateTimeProperty>" + Environment.NewLine +
            "  <ListProperty>" + Environment.NewLine +
            "    <string>one</string>" + Environment.NewLine +
            "    <string>two</string>" + Environment.NewLine +
            "    <string>three</string>" + Environment.NewLine +
            "  </ListProperty>" + Environment.NewLine +
            "</DerivedClassWithSameProperty>");

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
        var actual = SerializeAndDeserialize<SimpleCDC>(value,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <string>One</string>" + Environment.NewLine +
            "  <string>Two</string>" + Environment.NewLine +
            "  <string>Three</string>" + Environment.NewLine +
            "</ArrayOfString>");

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
        ClassImplementsInterface actual = SerializeAndDeserialize<ClassImplementsInterface>(value,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<ClassImplementsInterface xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <ClassID>ClassID</ClassID>" + Environment.NewLine +
            "  <DisplayName>DisplayName</DisplayName>" + Environment.NewLine +
            "  <Id>Id</Id>" + Environment.NewLine +
            "  <IsLoaded>true</IsLoaded>" + Environment.NewLine +
            "</ClassImplementsInterface>");

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
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<AttributeTesting xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" XmlAttributeName=\"2\">" + Environment.NewLine +
            "  <Word>String choice value</Word>" + Environment.NewLine +
            "  <XmlIncludeProperty xsi:type=\"ItemChoiceType\">DecimalNumber</XmlIncludeProperty>" + Environment.NewLine +
            "  <XmlEnumProperty>" + Environment.NewLine +
            "    <ItemChoiceType>DecimalNumber</ItemChoiceType>" + Environment.NewLine +
            "    <ItemChoiceType>Number</ItemChoiceType>" + Environment.NewLine +
            "    <ItemChoiceType>Word</ItemChoiceType>" + Environment.NewLine +
            "    <ItemChoiceType>None</ItemChoiceType>" + Environment.NewLine +
            "  </XmlEnumProperty>&lt;xml&gt;Hello XML&lt;/xml&gt;<XmlNamespaceDeclarationsProperty>XmlNamespaceDeclarationsPropertyValue</XmlNamespaceDeclarationsProperty><XmlElementPropertyNode xmlns=\"http://element\">1</XmlElementPropertyNode><CustomXmlArrayProperty xmlns=\"http://mynamespace\"><string>one</string><string>two</string><string>three</string></CustomXmlArrayProperty></AttributeTesting>");

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
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<WithStruct xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <Some>" + Environment.NewLine +
            "    <A>1</A>" + Environment.NewLine +
            "    <B>2</B>" + Environment.NewLine +
            "  </Some>" + Environment.NewLine +
            "</WithStruct>");

        // Assert
        Assert.StrictEqual(result.Some.A, value.Some.A);
        Assert.StrictEqual(result.Some.B, value.Some.B);
    }

    [Fact]
    public static void Xml_Enums()
    {
        var item = new WithEnums() { Int = IntEnum.Option1, Short = ShortEnum.Option2 };
        var actual = SerializeAndDeserialize(item,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<WithEnums xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <Int>Option1</Int>" + Environment.NewLine +
            "  <Short>Option2</Short>" + Environment.NewLine +
            "</WithEnums>");
        Assert.StrictEqual(item.Short, actual.Short);
        Assert.StrictEqual(item.Int, actual.Int);
    }

    [Fact]
    public static void Xml_Nullables()
    {
        var item = new WithNullables() { Optional = IntEnum.Option1, OptionalInt = 42, Struct1 = new SomeStruct { A = 1, B = 2 } };
        var actual = SerializeAndDeserialize(item,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<WithNullables xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
            "  <Optional>Option1</Optional>" + Environment.NewLine +
            "  <Optionull xsi:nil=\"true\" />" + Environment.NewLine +
            "  <OptionalInt>42</OptionalInt>" + Environment.NewLine +
            "  <OptionullInt xsi:nil=\"true\" />" + Environment.NewLine +
            "  <Struct1>" + Environment.NewLine +
            "    <A>1</A>" + Environment.NewLine +
            "    <B>2</B>" + Environment.NewLine +
            "  </Struct1>" + Environment.NewLine +
            "  <Struct2 xsi:nil=\"true\" />" + Environment.NewLine +
            "</WithNullables>");
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

        var actual = SerializeAndDeserialize<TypeWithMemberWithXmlNamespaceDeclarationsAttribute>(original,
            "<?xml version=\"1.0\"?>" + Environment.NewLine +
            "<Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://www.w3.org/2003/05/soap-envelope\">" + Environment.NewLine +
            "  <header>foo</header>" + Environment.NewLine +
            "  <body>bar</body>" + Environment.NewLine +
            "</Envelope>");
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

    [Fact]
    public static void Xml_XmlElementAsRoot()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml("<html></html>");
        XmlElement expected = xDoc.CreateElement("Element");
        expected.InnerText = "Element innertext";
        var actual = SerializeAndDeserialize(expected, @"<?xml version=""1.0"" encoding=""utf-8""?><Element>Element innertext</Element>");
        Assert.NotNull(actual);
        Assert.StrictEqual(expected.InnerText, actual.InnerText);
    }

    [Fact]
    public static void Xml_TypeWithXmlElementProperty()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml("<html></html>");
        XmlElement productElement = xDoc.CreateElement("Product");
        productElement.InnerText = "Product innertext";
        XmlElement categoryElement = xDoc.CreateElement("Category");
        categoryElement.InnerText = "Category innertext";
        var expected = new TypeWithXmlElementProperty() { Elements = new[] { productElement, categoryElement } };
        var actual = SerializeAndDeserialize(expected, @"<?xml version=""1.0"" encoding=""utf-8""?><TypeWithXmlElementProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><Product>Product innertext</Product><Category>Category innertext</Category></TypeWithXmlElementProperty>");
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
        expected.LoadXml("<html><head>Head content</head><body><h1>Heading1</h1><div>Text in body</div></body></html>");
        var actual = SerializeAndDeserialize(expected, @"<?xml version=""1.0"" encoding=""utf-8""?><html><head>Head content</head><body><h1>Heading1</h1><div>Text in body</div></body></html>");
        Assert.NotNull(actual);
        Assert.StrictEqual(expected.OuterXml, actual.OuterXml);
    }

    [Fact]
    public static void Xml_TypeWithXmlDocumentProperty()
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml("<html><head>Head content</head><body><h1>Heading1</h1><div>Text in body</div></body></html>");
        var expected = new TypeWithXmlDocumentProperty() { Document = xmlDoc };
        var actual = SerializeAndDeserialize(expected, @"<TypeWithXmlDocumentProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><Document><html><head>Head content</head><body><h1>Heading1</h1><div>Text in body</div></body></html></Document></TypeWithXmlDocumentProperty>");
        Assert.NotNull(actual);
        Assert.NotNull(actual.Document);
        Assert.StrictEqual(expected.Document.OuterXml, actual.Document.OuterXml);
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

    [Fact]
    public static void Xml_TypeWithNonPublicDefaultConstructor()
    {
        TypeInfo ti = IntrospectionExtensions.GetTypeInfo(typeof(TypeWithNonPublicDefaultConstructor));
        TypeWithNonPublicDefaultConstructor value = null;
        value = (TypeWithNonPublicDefaultConstructor)FindDefaultConstructor(ti).Invoke(null);
        Assert.StrictEqual("Mr. FooName", value.Name);
        var actual = SerializeAndDeserialize(value, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<TypeWithNonPublicDefaultConstructor xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine + "  <Name>Mr. FooName</Name>" + Environment.NewLine + "</TypeWithNonPublicDefaultConstructor>");
        Assert.StrictEqual(value.Name, actual.Name);
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
        var actual = SerializeAndDeserialize(obj, @"<?xml version=""1.0"" encoding=""utf-8""?><TypeWithBinaryProperty xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><BinaryHexContent>540068006500200071007500690063006B002000620072006F0077006E00200066006F00780020006A0075006D007000730020006F00760065007200200074006800650020006C0061007A007900200064006F0067002E00</BinaryHexContent><Base64Content>VABoAGUAIABxAHUAaQBjAGsAIABiAHIAbwB3AG4AIABmAG8AeAAgAGoAdQBtAHAAcwAgAG8AdgBlAHIAIAB0AGgAZQAgAGwAYQB6AHkAIABkAG8AZwAuAA==</Base64Content></TypeWithBinaryProperty>");
        Assert.StrictEqual(str, Encoding.Unicode.GetString(actual.Base64Content));
        Assert.StrictEqual(str, Encoding.Unicode.GetString(actual.BinaryHexContent));
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

    [Fact]
    public static void Xml_FromMappings()
    {
        var types = new[] {typeof (Guid), typeof (List<string>)};
        XmlReflectionImporter importer = new XmlReflectionImporter();
        XmlTypeMapping[] mappings = new XmlTypeMapping[types.Length];
        for (int i = 0; i < types.Length; i++)
        {
            mappings[i] = importer.ImportTypeMapping(types[i]);
        }
        var serializers = XmlSerializer.FromMappings(mappings, typeof(object));
        Xml_GuidAsRoot(serializers[0]);
        Xml_ListGenericRoot(serializers[1]);
    }

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
        serializer = new XmlSerializer(typeof(Music.Orchestra), overrides, Array.Empty<Type>(), root, "defaultNamespace");
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
    public static void Xml_ConstructorWithTypeMapping()
    {
        XmlTypeMapping mapping = null;
        XmlSerializer serializer = null;
        Assert.Throws<ArgumentNullException>(() => { new XmlSerializer(mapping); });
        
        mapping = new XmlReflectionImporter(null, null).ImportTypeMapping(typeof(List<string>));
        serializer = new XmlSerializer(mapping);
        Xml_ListGenericRoot(serializer);
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

    private static T SerializeAndDeserialize<T>(T value, string baseline, Func<XmlSerializer> serializerFactory = null)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        if (serializerFactory != null)
        {
            serializer = serializerFactory();
        }
        using (MemoryStream ms = new MemoryStream())
        {
            serializer.Serialize(ms, value);
            ms.Position = 0;
            string actualOutput = new StreamReader(ms).ReadToEnd();

            Utils.CompareResult result = Utils.Compare(baseline, actualOutput);
            Assert.True(result.Equal, string.Format("{1}{0}Test failed for input: {2}{0}Expected: {3}{0}Actual: {4}", 
                Environment.NewLine, result.ErrorMessage, value, baseline, actualOutput));

            ms.Position = 0;
            T deserialized;
            deserialized = (T)serializer.Deserialize(ms);

            return deserialized;
        }
    }
}

#region Type for Xml_ConstructorWithXmlAttributeOverrides

namespace Music
{
    public class Orchestra
    {
        public Instrument[] Instruments;
    }

    public class Instrument
    {
        public string Name;
    }

    public class Brass : Instrument
    {
        public bool IsValved;
    }
}

#endregion
