// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using SerializationTypes;

using Xunit;

public static class DataContractSerializerTests
{
    [Fact]
    public static void DCS_DateTimeOffsetAsRoot()
    {
        var offsetMinutes = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2013, 1, 2)).TotalMinutes;
        var objs = new DateTimeOffset[]
        {
            // Adding offsetMinutes so the DateTime component in serialized strings are time-zone independent
            new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6).AddMinutes(offsetMinutes)),
            new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local).AddMinutes(offsetMinutes)),
            new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified).AddMinutes(offsetMinutes)),

            new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc))
        };
        var serializedStrings = new string[]
        {
            string.Format("<DateTimeOffset xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><DateTime>2013-01-02T03:04:05.006Z</DateTime><OffsetMinutes>{0}</OffsetMinutes></DateTimeOffset>", offsetMinutes),
            string.Format("<DateTimeOffset xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><DateTime>2013-01-02T03:04:05.006Z</DateTime><OffsetMinutes>{0}</OffsetMinutes></DateTimeOffset>", offsetMinutes),
            string.Format("<DateTimeOffset xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><DateTime>2013-01-02T03:04:05.006Z</DateTime><OffsetMinutes>{0}</OffsetMinutes></DateTimeOffset>", offsetMinutes),
            "<DateTimeOffset xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><DateTime>2013-01-02T03:04:05.006Z</DateTime><OffsetMinutes>0</OffsetMinutes></DateTimeOffset>",
            "<DateTimeOffset xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><DateTime>0001-01-01T00:00:00Z</DateTime><OffsetMinutes>0</OffsetMinutes></DateTimeOffset>",
            "<DateTimeOffset xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><DateTime>9999-12-31T23:59:59.9999999Z</DateTime><OffsetMinutes>0</OffsetMinutes></DateTimeOffset>"
        };
        for (int i = 0; i < objs.Length; ++i)
        {
            var actual = objs[i];
            var expected = SerializeAndDeserialize<DateTimeOffset>(actual, serializedStrings[i]);
            Assert.StrictEqual(expected, actual);
            Assert.StrictEqual(expected.Offset, actual.Offset);
        }
    }

    [Fact]
    public static void DCS_BoolAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<bool>(true, "<boolean xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">true</boolean>"), true);
        Assert.StrictEqual(SerializeAndDeserialize<bool>(false, "<boolean xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">false</boolean>"), false);
    }

    [Fact]
    public static void DCS_ByteArrayAsRoot()
    {
        Assert.Null(SerializeAndDeserialize<byte[]>(null, "<base64Binary i:nil=\"true\" xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"/>"));
        byte[] x = new byte[] { 1, 2 };
        byte[] y = SerializeAndDeserialize<byte[]>(x, "<base64Binary xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">AQI=</base64Binary>");
        Assert.Equal<byte>(x, y);
    }

    [Fact]
    public static void DCS_CharAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<char>(char.MinValue, "<char xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">0</char>"), char.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<char>(char.MaxValue, "<char xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">65535</char>"), char.MaxValue);
        Assert.StrictEqual(SerializeAndDeserialize<char>('a', "<char xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">97</char>"), 'a');
        Assert.StrictEqual(SerializeAndDeserialize<char>('ñ', "<char xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">241</char>"), 'ñ');
        Assert.StrictEqual(SerializeAndDeserialize<char>('漢', "<char xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">28450</char>"), '漢');
    }

    [Fact]
    public static void DCS_ByteAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<byte>(10, "<unsignedByte xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">10</unsignedByte>"), 10);
        Assert.StrictEqual(SerializeAndDeserialize<byte>(byte.MinValue, "<unsignedByte xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">0</unsignedByte>"), byte.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<byte>(byte.MaxValue, "<unsignedByte xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">255</unsignedByte>"), byte.MaxValue);
    }

    [Fact]
    public static void DCS_DateTimeAsRoot()
    {
        var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(new DateTime(2013, 1, 2)).TotalMinutes;
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2), "<dateTime xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">2013-01-02T00:00:00</dateTime>"), new DateTime(2013, 1, 2));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local), string.Format("<dateTime xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">2013-01-02T03:04:05.006{0:+;-}{1}</dateTime>", offsetMinutes, new TimeSpan(0, offsetMinutes, 0).ToString(@"hh\:mm"))), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified), "<dateTime xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">2013-01-02T03:04:05.006</dateTime>"), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc), "<dateTime xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">2013-01-02T03:04:05.006Z</dateTime>"), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), "<dateTime xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">0001-01-01T00:00:00Z</dateTime>"), DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), "<dateTime xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">9999-12-31T23:59:59.9999999Z</dateTime>"), DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
    }

    [Fact]
    public static void DCS_DecimalAsRoot()
    {
        foreach (decimal value in new decimal[] { -1.2m, 0.0m, 2.3m, decimal.MinValue, decimal.MaxValue })
        {
            var baseline = string.Format("<decimal xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{0}</decimal>", value.ToString(CultureInfo.InvariantCulture));
            Assert.StrictEqual(SerializeAndDeserialize<decimal>(value, baseline), value);
        }
    }

    [Fact]
    public static void DCS_DoubleAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<double>(-1.2, "<double xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">-1.2</double>"), -1.2);
        Assert.StrictEqual(SerializeAndDeserialize<double>(0, "<double xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">0</double>"), 0);
        Assert.StrictEqual(SerializeAndDeserialize<double>(2.3, "<double xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">2.3</double>"), 2.3);
        Assert.StrictEqual(SerializeAndDeserialize<double>(double.MinValue, "<double xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">-1.7976931348623157E+308</double>"), double.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<double>(double.MaxValue, "<double xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">1.7976931348623157E+308</double>"), double.MaxValue);
    }

    [Fact]
    public static void DCS_FloatAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)-1.2, "<float xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">-1.2</float>"), (float)-1.2);
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)0, "<float xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">0</float>"), (float)0);
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)2.3, "<float xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">2.3</float>"), (float)2.3);
        Assert.StrictEqual(SerializeAndDeserialize<float>(float.MinValue, "<float xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">-3.40282347E+38</float>"), float.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<float>(float.MaxValue, "<float xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">3.40282347E+38</float>"), float.MaxValue);
    }

    [Fact]
    public static void DCS_GuidAsRoot()
    {
        foreach (Guid value in new Guid[] { Guid.NewGuid(), Guid.Empty })
        {
            Assert.StrictEqual(SerializeAndDeserialize<Guid>(value, string.Format("<guid xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{0}</guid>", value.ToString())), value);
        }
    }

    [Fact]
    public static void DCS_IntAsRoot()
    {
        foreach (int value in new int[] { -1, 0, 2, int.MinValue, int.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<int>(value, string.Format("<int xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{0}</int>", value)), value);
        }
    }

    [Fact]
    public static void DCS_LongAsRoot()
    {
        foreach (long value in new long[] { (long)-1, (long)0, (long)2, long.MinValue, long.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<long>(value, string.Format("<long xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{0}</long>", value)), value);
        }
    }

    [Fact]
    public static void DCS_ObjectAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<object>(1, "<z:anyType i:type=\"a:int\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">1</z:anyType>"), 1);
        Assert.StrictEqual(SerializeAndDeserialize<object>(true, "<z:anyType i:type=\"a:boolean\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">true</z:anyType>"), true);
        Assert.StrictEqual(SerializeAndDeserialize<object>("abc", "<z:anyType i:type=\"a:string\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">abc</z:anyType>"), "abc");
        Assert.StrictEqual(SerializeAndDeserialize<object>(null, "<z:anyType i:nil=\"true\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"/>"), null);
    }

    [Fact]
    public static void DCS_XmlQualifiedNameAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<XmlQualifiedName>(new XmlQualifiedName("abc", "def"), "<z:QName xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"def\">a:abc</z:QName>"), new XmlQualifiedName("abc", "def"));
        Assert.StrictEqual(SerializeAndDeserialize<XmlQualifiedName>(XmlQualifiedName.Empty, "<z:QName xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/>"), XmlQualifiedName.Empty);
    }

    [Fact]
    public static void DCS_ShortAsRoot()
    {
        foreach (short value in new short[] { (short)-1.2, (short)0, (short)2.3, short.MinValue, short.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<short>(value, string.Format("<short xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{0}</short>", value)), value);
        }
    }

    [Fact]
    public static void DCS_SbyteAsRoot()
    {
        foreach (sbyte value in new sbyte[] { (sbyte)3, (sbyte)0, sbyte.MinValue, sbyte.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<sbyte>(value, string.Format("<byte xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{0}</byte>", value)), value);
        }
    }

    [Fact]
    public static void DCS_StringAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<string>("abc", "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">abc</string>"), "abc");
        Assert.StrictEqual(SerializeAndDeserialize<string>("  a b  ", "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">  a b  </string>"), "  a b  ");
        Assert.StrictEqual(SerializeAndDeserialize<string>(null, "<string i:nil=\"true\" xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"/>"), null);
        Assert.StrictEqual(SerializeAndDeserialize<string>("", "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\"/>"), "");
        Assert.StrictEqual(SerializeAndDeserialize<string>(" ", "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\"> </string>"), " ");
        Assert.StrictEqual(SerializeAndDeserialize<string>("Hello World! 漢 ñ", "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">Hello World! 漢 ñ</string>"), "Hello World! 漢 ñ");
    }

    [Fact]
    public static void DCS_TimeSpanAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<TimeSpan>(new TimeSpan(1, 2, 3), "<duration xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">PT1H2M3S</duration>"), new TimeSpan(1, 2, 3));
        Assert.StrictEqual(SerializeAndDeserialize<TimeSpan>(TimeSpan.Zero, "<duration xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">PT0S</duration>"), TimeSpan.Zero);
        Assert.StrictEqual(SerializeAndDeserialize<TimeSpan>(TimeSpan.MinValue, "<duration xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">-P10675199DT2H48M5.4775808S</duration>"), TimeSpan.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<TimeSpan>(TimeSpan.MaxValue, "<duration xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">P10675199DT2H48M5.4775807S</duration>"), TimeSpan.MaxValue);
    }

    [Fact]
    public static void DCS_UintAsRoot()
    {
        foreach (uint value in new uint[] { (uint)3, (uint)0, uint.MinValue, uint.MaxValue })
        {
            Assert.StrictEqual<uint>(SerializeAndDeserialize<uint>(value, string.Format("<unsignedInt xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{0}</unsignedInt>", value)), value);
        }
    }

    [Fact]
    public static void DCS_UlongAsRoot()
    {
        foreach (ulong value in new ulong[] { (ulong)3, (ulong)0, ulong.MinValue, ulong.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<ulong>(value, string.Format("<unsignedLong xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{0}</unsignedLong>", value)), value);
        }
    }

    [Fact]
    public static void DCS_UshortAsRoot()
    {
        foreach (ushort value in new ushort[] { (ushort)3, (ushort)0, ushort.MinValue, ushort.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<ushort>(value, string.Format("<unsignedShort xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{0}</unsignedShort>", value)), value);
        }
    }

    [Fact]
    public static void DCS_UriAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<Uri>(new Uri("http://abc/"), "<anyURI xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">http://abc/</anyURI>"), new Uri("http://abc/"));
        Assert.StrictEqual(SerializeAndDeserialize<Uri>(new Uri("http://abc/def/x.aspx?p1=12&p2=34"), "<anyURI xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">http://abc/def/x.aspx?p1=12&amp;p2=34</anyURI>"), new Uri("http://abc/def/x.aspx?p1=12&p2=34"));
    }

    [Fact]
    public static void DCS_ArrayAsRoot()
    {
        SimpleType[] x = new SimpleType[] { new SimpleType { P1 = "abc", P2 = 11 }, new SimpleType { P1 = "def", P2 = 12 } };
        SimpleType[] y = SerializeAndDeserialize<SimpleType[]>(x, "<ArrayOfSimpleType xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><SimpleType><P1>abc</P1><P2>11</P2></SimpleType><SimpleType><P1>def</P1><P2>12</P2></SimpleType></ArrayOfSimpleType>");

        Utils.Equal<SimpleType>(x, y, (a, b) => { return SimpleType.AreEqual(a, b); });
    }

    [Fact]
    public static void DCS_ArrayAsGetSet()
    {
        TypeWithGetSetArrayMembers x = new TypeWithGetSetArrayMembers
        {
            F1 = new SimpleType[] { new SimpleType { P1 = "ab", P2 = 1 }, new SimpleType { P1 = "cd", P2 = 2 } },
            F2 = new int[] { -1, 3 },
            P1 = new SimpleType[] { new SimpleType { P1 = "ef", P2 = 5 }, new SimpleType { P1 = "gh", P2 = 7 } },
            P2 = new int[] { 11, 12 }
        };
        TypeWithGetSetArrayMembers y = SerializeAndDeserialize<TypeWithGetSetArrayMembers>(x, "<TypeWithGetSetArrayMembers xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><F1><SimpleType><P1>ab</P1><P2>1</P2></SimpleType><SimpleType><P1>cd</P1><P2>2</P2></SimpleType></F1><F2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:int>-1</a:int><a:int>3</a:int></F2><P1><SimpleType><P1>ef</P1><P2>5</P2></SimpleType><SimpleType><P1>gh</P1><P2>7</P2></SimpleType></P1><P2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:int>11</a:int><a:int>12</a:int></P2></TypeWithGetSetArrayMembers>");

        Assert.NotNull(y);
        Utils.Equal<SimpleType>(x.F1, y.F1, (a, b) => { return SimpleType.AreEqual(a, b); });
        Assert.Equal<int>(x.F2, y.F2);
        Utils.Equal<SimpleType>(x.P1, y.P1, (a, b) => { return SimpleType.AreEqual(a, b); });
        Assert.Equal<int>(x.P2, y.P2);
    }

    [Fact]
    public static void DCS_ArrayAsGetOnly()
    {
        TypeWithGetOnlyArrayProperties x = new TypeWithGetOnlyArrayProperties();
        x.P1[0] = new SimpleType { P1 = "ab", P2 = 1 };
        x.P1[1] = new SimpleType { P1 = "cd", P2 = 2 };
        x.P2[0] = -1;
        x.P2[1] = 3;

        TypeWithGetOnlyArrayProperties y = SerializeAndDeserialize<TypeWithGetOnlyArrayProperties>(x, "<TypeWithGetOnlyArrayProperties xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><P1><SimpleType><P1>ab</P1><P2>1</P2></SimpleType><SimpleType><P1>cd</P1><P2>2</P2></SimpleType></P1><P2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:int>-1</a:int><a:int>3</a:int></P2></TypeWithGetOnlyArrayProperties>");

        Assert.NotNull(y);
        Utils.Equal<SimpleType>(x.P1, y.P1, (a, b) => { return SimpleType.AreEqual(a, b); });
        Assert.Equal<int>(x.P2, y.P2);
    }

    [Fact]
    public static void DCS_DictionaryGenericRoot()
    {
        Dictionary<string, int> x = new Dictionary<string, int>();
        x.Add("one", 1);
        x.Add("two", 2);

        Dictionary<string, int> y = SerializeAndDeserialize<Dictionary<string, int>>(x, "<ArrayOfKeyValueOfstringint xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><KeyValueOfstringint><Key>one</Key><Value>1</Value></KeyValueOfstringint><KeyValueOfstringint><Key>two</Key><Value>2</Value></KeyValueOfstringint></ArrayOfKeyValueOfstringint>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True(y["one"] == 1);
        Assert.True(y["two"] == 2);
    }

    [Fact]
    public static void DCS_DictionaryGenericMembers()
    {
        TypeWithDictionaryGenericMembers x = new TypeWithDictionaryGenericMembers
        {
            F1 = new Dictionary<string, int>(),
            F2 = new Dictionary<string, int>(),
            P1 = new Dictionary<string, int>(),
            P2 = new Dictionary<string, int>()
        };
        x.F1.Add("ab", 12);
        x.F1.Add("cd", 15);
        x.F2.Add("ef", 17);
        x.F2.Add("gh", 19);
        x.P1.Add("12", 120);
        x.P1.Add("13", 130);
        x.P2.Add("14", 140);
        x.P2.Add("15", 150);

        x.RO1.Add(true, 't');
        x.RO1.Add(false, 'f');

        x.RO2.Add(true, 'a');
        x.RO2.Add(false, 'b');

        TypeWithDictionaryGenericMembers y = SerializeAndDeserialize<TypeWithDictionaryGenericMembers>(x, "<TypeWithDictionaryGenericMembers xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><F1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfstringint><a:Key>ab</a:Key><a:Value>12</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key>cd</a:Key><a:Value>15</a:Value></a:KeyValueOfstringint></F1><F2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfstringint><a:Key>ef</a:Key><a:Value>17</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key>gh</a:Key><a:Value>19</a:Value></a:KeyValueOfstringint></F2><P1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfstringint><a:Key>12</a:Key><a:Value>120</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key>13</a:Key><a:Value>130</a:Value></a:KeyValueOfstringint></P1><P2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfstringint><a:Key>14</a:Key><a:Value>140</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key>15</a:Key><a:Value>150</a:Value></a:KeyValueOfstringint></P2><RO1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfbooleanchar><a:Key>true</a:Key><a:Value>116</a:Value></a:KeyValueOfbooleanchar><a:KeyValueOfbooleanchar><a:Key>false</a:Key><a:Value>102</a:Value></a:KeyValueOfbooleanchar></RO1><RO2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfbooleanchar><a:Key>true</a:Key><a:Value>97</a:Value></a:KeyValueOfbooleanchar><a:KeyValueOfbooleanchar><a:Key>false</a:Key><a:Value>98</a:Value></a:KeyValueOfbooleanchar></RO2></TypeWithDictionaryGenericMembers>");
        Assert.NotNull(y);

        Assert.NotNull(y.F1);
        Assert.True(y.F1.Count == 2);
        Assert.True(y.F1["ab"] == 12);
        Assert.True(y.F1["cd"] == 15);

        Assert.NotNull(y.F2);
        Assert.True(y.F2.Count == 2);
        Assert.True(y.F2["ef"] == 17);
        Assert.True(y.F2["gh"] == 19);

        Assert.NotNull(y.P1);
        Assert.True(y.P1.Count == 2);
        Assert.True(y.P1["12"] == 120);
        Assert.True(y.P1["13"] == 130);

        Assert.NotNull(y.P2);
        Assert.True(y.P2.Count == 2);
        Assert.True(y.P2["14"] == 140);
        Assert.True(y.P2["15"] == 150);

        Assert.NotNull(y.RO1);
        Assert.True(y.RO1.Count == 2);
        Assert.True(y.RO1[true] == 't');
        Assert.True(y.RO1[false] == 'f');

        Assert.NotNull(y.RO2);
        Assert.True(y.RO2.Count == 2);
        Assert.True(y.RO2[true] == 'a');
        Assert.True(y.RO2[false] == 'b');
    }

    [Fact]
    public static void DCS_DictionaryRoot()
    {
        MyDictionary x = new MyDictionary();
        x.Add(1, "one");
        x.Add(2, "two");

        MyDictionary y = SerializeAndDeserialize<MyDictionary>(x, "<ArrayOfKeyValueOfanyTypeanyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><KeyValueOfanyTypeanyType><Key i:type=\"a:int\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">1</Key><Value i:type=\"a:string\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">one</Value></KeyValueOfanyTypeanyType><KeyValueOfanyTypeanyType><Key i:type=\"a:int\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">2</Key><Value i:type=\"a:string\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">two</Value></KeyValueOfanyTypeanyType></ArrayOfKeyValueOfanyTypeanyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((string)y[1] == "one");
        Assert.True((string)y[2] == "two");
    }

    [Fact]
    public static void DCS_DictionaryMembers()
    {
        TypeWithDictionaryMembers x = new TypeWithDictionaryMembers();

        x.F1 = new MyDictionary();
        x.F1.Add("ab", 12);
        x.F1.Add("cd", 15);

        x.F2 = new MyDictionary();
        x.F2.Add("ef", 17);
        x.F2.Add("gh", 19);

        x.P1 = new MyDictionary();
        x.P1.Add("12", 120);
        x.P1.Add("13", 130);

        x.P2 = new MyDictionary();
        x.P2.Add("14", 140);
        x.P2.Add("15", 150);

        x.RO1.Add(true, 't');
        x.RO1.Add(false, 'f');

        x.RO2.Add(true, 'a');
        x.RO2.Add(false, 'b');

        TypeWithDictionaryMembers y = SerializeAndDeserialize<TypeWithDictionaryMembers>(x, "<TypeWithDictionaryMembers xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><F1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfanyTypeanyType><a:Key i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">ab</a:Key><a:Value i:type=\"b:int\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">12</a:Value></a:KeyValueOfanyTypeanyType><a:KeyValueOfanyTypeanyType><a:Key i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">cd</a:Key><a:Value i:type=\"b:int\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">15</a:Value></a:KeyValueOfanyTypeanyType></F1><F2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfanyTypeanyType><a:Key i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">ef</a:Key><a:Value i:type=\"b:int\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">17</a:Value></a:KeyValueOfanyTypeanyType><a:KeyValueOfanyTypeanyType><a:Key i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">gh</a:Key><a:Value i:type=\"b:int\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">19</a:Value></a:KeyValueOfanyTypeanyType></F2><P1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfanyTypeanyType><a:Key i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">12</a:Key><a:Value i:type=\"b:int\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">120</a:Value></a:KeyValueOfanyTypeanyType><a:KeyValueOfanyTypeanyType><a:Key i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">13</a:Key><a:Value i:type=\"b:int\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">130</a:Value></a:KeyValueOfanyTypeanyType></P1><P2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfanyTypeanyType><a:Key i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">14</a:Key><a:Value i:type=\"b:int\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">140</a:Value></a:KeyValueOfanyTypeanyType><a:KeyValueOfanyTypeanyType><a:Key i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">15</a:Key><a:Value i:type=\"b:int\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">150</a:Value></a:KeyValueOfanyTypeanyType></P2><RO1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfanyTypeanyType><a:Key i:type=\"b:boolean\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">true</a:Key><a:Value i:type=\"b:char\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/\">116</a:Value></a:KeyValueOfanyTypeanyType><a:KeyValueOfanyTypeanyType><a:Key i:type=\"b:boolean\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">false</a:Key><a:Value i:type=\"b:char\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/\">102</a:Value></a:KeyValueOfanyTypeanyType></RO1><RO2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfanyTypeanyType><a:Key i:type=\"b:boolean\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">true</a:Key><a:Value i:type=\"b:char\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/\">97</a:Value></a:KeyValueOfanyTypeanyType><a:KeyValueOfanyTypeanyType><a:Key i:type=\"b:boolean\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">false</a:Key><a:Value i:type=\"b:char\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/\">98</a:Value></a:KeyValueOfanyTypeanyType></RO2></TypeWithDictionaryMembers>");
        Assert.NotNull(y);

        Assert.NotNull(y.F1);
        Assert.True(y.F1.Count == 2);
        Assert.True((int)y.F1["ab"] == 12);
        Assert.True((int)y.F1["cd"] == 15);

        Assert.NotNull(y.F2);
        Assert.True(y.F2.Count == 2);
        Assert.True((int)y.F2["ef"] == 17);
        Assert.True((int)y.F2["gh"] == 19);

        Assert.NotNull(y.P1);
        Assert.True(y.P1.Count == 2);
        Assert.True((int)y.P1["12"] == 120);
        Assert.True((int)y.P1["13"] == 130);

        Assert.NotNull(y.P2);
        Assert.True(y.P2.Count == 2);
        Assert.True((int)y.P2["14"] == 140);
        Assert.True((int)y.P2["15"] == 150);

        Assert.NotNull(y.RO1);
        Assert.True(y.RO1.Count == 2);
        Assert.True((char)y.RO1[true] == 't');
        Assert.True((char)y.RO1[false] == 'f');

        Assert.NotNull(y.RO2);
        Assert.True(y.RO2.Count == 2);
        Assert.True((char)y.RO2[true] == 'a');
        Assert.True((char)y.RO2[false] == 'b');
    }

    [Fact]
    public static void DCS_TypeWithIDictionaryPropertyInitWithConcreteType()
    {
        // Test for Bug 876869 : [Serialization] Concrete type not inferred for DCS
        var dict = new TypeWithIDictionaryPropertyInitWithConcreteType();
        dict.DictionaryProperty.Add("key1", "value1");
        dict.DictionaryProperty.Add("key2", "value2");

        var dict2 = SerializeAndDeserialize<TypeWithIDictionaryPropertyInitWithConcreteType>(dict, "<TypeWithIDictionaryPropertyInitWithConcreteType xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><DictionaryProperty xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfstringstring><a:Key>key1</a:Key><a:Value>value1</a:Value></a:KeyValueOfstringstring><a:KeyValueOfstringstring><a:Key>key2</a:Key><a:Value>value2</a:Value></a:KeyValueOfstringstring></DictionaryProperty></TypeWithIDictionaryPropertyInitWithConcreteType>");

        Assert.True(dict2 != null && dict2.DictionaryProperty != null);
        Assert.True(dict.DictionaryProperty.Count == dict2.DictionaryProperty.Count);
        foreach (var entry in dict.DictionaryProperty)
        {
            Assert.True(dict2.DictionaryProperty.ContainsKey(entry.Key) && dict2.DictionaryProperty[entry.Key].Equals(dict.DictionaryProperty[entry.Key]));
        }
    }

    [Fact]
    public static void DCS_ListGenericRoot()
    {
        List<string> x = new List<string>();
        x.Add("zero");
        x.Add("one");

        List<string> y = SerializeAndDeserialize<List<string>>(x, "<ArrayOfstring xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><string>zero</string><string>one</string></ArrayOfstring>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True(y[0] == "zero");
        Assert.True(y[1] == "one");
    }

    [Fact]
    public static void DCS_ListGenericMembers()
    {
        TypeWithListGenericMembers x = new TypeWithListGenericMembers();

        x.F1 = new List<string>();
        x.F1.Add("zero");
        x.F1.Add("one");

        x.F2 = new List<string>();
        x.F2.Add("abc");
        x.F2.Add("def");

        x.P1 = new List<int>();
        x.P1.Add(10);
        x.P1.Add(20);

        x.P2 = new List<int>();
        x.P2.Add(12);
        x.P2.Add(34);

        x.RO1.Add('a');
        x.RO1.Add('b');

        x.RO2.Add('c');
        x.RO2.Add('d');

        TypeWithListGenericMembers y = SerializeAndDeserialize<TypeWithListGenericMembers>(x, "<TypeWithListGenericMembers xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><F1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>zero</a:string><a:string>one</a:string></F1><F2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>abc</a:string><a:string>def</a:string></F2><P1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:int>10</a:int><a:int>20</a:int></P1><P2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:int>12</a:int><a:int>34</a:int></P2><RO1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:char>97</a:char><a:char>98</a:char></RO1><RO2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:char>99</a:char><a:char>100</a:char></RO2></TypeWithListGenericMembers>");
        Assert.NotNull(y);

        Assert.NotNull(y.F1);
        Assert.True(y.F1.Count == 2);
        Assert.True(y.F1[0] == "zero");
        Assert.True(y.F1[1] == "one");

        Assert.NotNull(y.F2);
        Assert.True(y.F2.Count == 2);
        Assert.True(y.F2[0] == "abc");
        Assert.True(y.F2[1] == "def");

        Assert.NotNull(y.P1);
        Assert.True(y.P1.Count == 2);
        Assert.True(y.P1[0] == 10);
        Assert.True(y.P1[1] == 20);

        Assert.NotNull(y.P2);
        Assert.True(y.P2.Count == 2);
        Assert.True(y.P2[0] == 12);
        Assert.True(y.P2[1] == 34);

        Assert.NotNull(y.RO1);
        Assert.True(y.RO1.Count == 2);
        Assert.True(y.RO1[0] == 'a');
        Assert.True(y.RO1[1] == 'b');

        Assert.NotNull(y.RO2);
        Assert.True(y.RO2.Count == 2);
        Assert.True(y.RO2[0] == 'c');
        Assert.True(y.RO2[1] == 'd');
    }

    [Fact]
    public static void DCS_CollectionGenericRoot()
    {
        MyCollection<string> x = new MyCollection<string>("a1", "a2");
        MyCollection<string> y = SerializeAndDeserialize<MyCollection<string>>(x, "<ArrayOfstring xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><string>a1</string><string>a2</string></ArrayOfstring>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        foreach (var item in x)
        {
            Assert.True(y.Contains(item));
        }
    }

    [Fact]
    public static void DCS_CollectionGenericMembers()
    {
        TypeWithCollectionGenericMembers x = new TypeWithCollectionGenericMembers
        {
            F1 = new MyCollection<string>("a1", "a2"),
            F2 = new MyCollection<string>("b1", "b2"),
            P1 = new MyCollection<string>("c1", "c2"),
            P2 = new MyCollection<string>("d1", "d2"),
        };
        x.RO1.Add("abc");
        x.RO2.Add("xyz");

        TypeWithCollectionGenericMembers y = SerializeAndDeserialize<TypeWithCollectionGenericMembers>(x, "<TypeWithCollectionGenericMembers xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><F1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>a1</a:string><a:string>a2</a:string></F1><F2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>b1</a:string><a:string>b2</a:string></F2><P1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>c1</a:string><a:string>c2</a:string></P1><P2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>d1</a:string><a:string>d2</a:string></P2><RO1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>abc</a:string></RO1><RO2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>xyz</a:string></RO2></TypeWithCollectionGenericMembers>");
        Assert.NotNull(y);
        Assert.True(y.F1.Count == 2, getCheckFailureMsg("F1"));
        Assert.True(y.F2.Count == 2, getCheckFailureMsg("F2"));
        Assert.True(y.P1.Count == 2, getCheckFailureMsg("P1"));
        Assert.True(y.P2.Count == 2, getCheckFailureMsg("P2"));
        Assert.True(y.RO1.Count == 1, getCheckFailureMsg("RO1"));
        Assert.True(y.RO2.Count == 1, getCheckFailureMsg("RO2"));

        foreach (var item in x.F1)
        {
            Assert.True(y.F1.Contains(item), getCheckFailureMsg("F1"));
        }

        foreach (var item in x.F2)
        {
            Assert.True(y.F2.Contains(item), getCheckFailureMsg("F2"));
        }

        foreach (var item in x.P1)
        {
            Assert.True(y.P1.Contains(item), getCheckFailureMsg("P1"));
        }

        foreach (var item in x.P2)
        {
            Assert.True(y.P2.Contains(item), getCheckFailureMsg("P2"));
        }

        foreach (var item in x.RO1)
        {
            Assert.True(y.RO1.Contains(item), getCheckFailureMsg("RO1"));
        }

        foreach (var item in x.RO2)
        {
            Assert.True(y.RO2.Contains(item), getCheckFailureMsg("RO2"));
        }
    }

    [Fact]
    public static void DCS_ListRoot()
    {
        MyList x = new MyList("a1", "a2");
        MyList y = SerializeAndDeserialize<MyList>(x, "<ArrayOfanyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><anyType i:type=\"a:string\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">a1</anyType><anyType i:type=\"a:string\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">a2</anyType></ArrayOfanyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);

        foreach (var item in x)
        {
            Assert.True(y.Contains(item));
        }
    }

    [Fact]
    public static void DCS_ListMembers()
    {
        TypeWithListMembers x = new TypeWithListMembers
        {
            F1 = new MyList("a1", "a2"),
            F2 = new MyList("b1", "b2"),
            P1 = new MyList("c1", "c2"),
            P2 = new MyList("d1", "d2"),
        };
        x.RO1.Add("abc");
        x.RO2.Add("xyz");

        TypeWithListMembers y = SerializeAndDeserialize<TypeWithListMembers>(x, "<TypeWithListMembers xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><F1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">a1</a:anyType><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">a2</a:anyType></F1><F2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">b1</a:anyType><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">b2</a:anyType></F2><P1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">c1</a:anyType><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">c2</a:anyType></P1><P2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">d1</a:anyType><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">d2</a:anyType></P2><RO1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">abc</a:anyType></RO1><RO2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">xyz</a:anyType></RO2></TypeWithListMembers>");
        Assert.NotNull(y);
        Assert.True(y.F1.Count == 2, getCheckFailureMsg("F1"));
        Assert.True(y.F2.Count == 2, getCheckFailureMsg("F2"));
        Assert.True(y.P1.Count == 2, getCheckFailureMsg("P1"));
        Assert.True(y.P2.Count == 2, getCheckFailureMsg("P2"));
        Assert.True(y.RO1.Count == 1, getCheckFailureMsg("RO1"));
        Assert.True(y.RO2.Count == 1, getCheckFailureMsg("RO2"));

        Assert.True((string)x.F1[0] == (string)y.F1[0], getCheckFailureMsg("F1"));
        Assert.True((string)x.F1[1] == (string)y.F1[1], getCheckFailureMsg("F1"));
        Assert.True((string)x.F2[0] == (string)y.F2[0], getCheckFailureMsg("F2"));
        Assert.True((string)x.F2[1] == (string)y.F2[1], getCheckFailureMsg("F2"));
        Assert.True((string)x.P1[0] == (string)y.P1[0], getCheckFailureMsg("P1"));
        Assert.True((string)x.P1[1] == (string)y.P1[1], getCheckFailureMsg("P1"));
        Assert.True((string)x.P2[0] == (string)y.P2[0], getCheckFailureMsg("P2"));
        Assert.True((string)x.P2[1] == (string)y.P2[1], getCheckFailureMsg("P2"));
        Assert.True((string)x.RO1[0] == (string)y.RO1[0], getCheckFailureMsg("RO1"));
        Assert.True((string)x.RO2[0] == (string)y.RO2[0], getCheckFailureMsg("RO2"));
    }

    [Fact]
    public static void DCS_EnumerableGenericRoot()
    {
        MyEnumerable<string> x = new MyEnumerable<string>("a1", "a2");
        MyEnumerable<string> y = SerializeAndDeserialize<MyEnumerable<string>>(x, "<ArrayOfstring xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><string>a1</string><string>a2</string></ArrayOfstring>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);

        string actual = string.Join("", y);
        Assert.StrictEqual(actual, "a1a2");
    }

    [Fact]
    public static void DCS_EnumerableGenericMembers()
    {
        TypeWithEnumerableGenericMembers x = new TypeWithEnumerableGenericMembers
        {
            F1 = new MyEnumerable<string>("a1", "a2"),
            F2 = new MyEnumerable<string>("b1", "b2"),
            P1 = new MyEnumerable<string>("c1", "c2"),
            P2 = new MyEnumerable<string>("d1", "d2")
        };
        x.RO1.Add("abc");

        TypeWithEnumerableGenericMembers y = SerializeAndDeserialize<TypeWithEnumerableGenericMembers>(x, "<TypeWithEnumerableGenericMembers xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><F1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>a1</a:string><a:string>a2</a:string></F1><F2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>b1</a:string><a:string>b2</a:string></F2><P1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>c1</a:string><a:string>c2</a:string></P1><P2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>d1</a:string><a:string>d2</a:string></P2><RO1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>abc</a:string></RO1></TypeWithEnumerableGenericMembers>");

        Assert.NotNull(y);
        Assert.True(y.F1.Count == 2);
        Assert.True(((string[])y.F2).Length == 2);
        Assert.True(y.P1.Count == 2);
        Assert.True(((string[])y.P2).Length == 2);
        Assert.True(y.RO1.Count == 1);
    }

    [Fact]
    public static void DCS_CollectionRoot()
    {
        MyCollection x = new MyCollection('a', 45);
        MyCollection y = SerializeAndDeserialize<MyCollection>(x, "<ArrayOfanyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><anyType i:type=\"a:char\" xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/\">97</anyType><anyType i:type=\"a:int\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">45</anyType></ArrayOfanyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((char)y[0] == 'a');
        Assert.True((int)y[1] == 45);
    }

    [Fact]
    public static void DCS_CollectionMembers()
    {
        TypeWithCollectionMembers x = new TypeWithCollectionMembers
        {
            F1 = new MyCollection('a', 45),
            F2 = new MyCollection("ab", true),
            P1 = new MyCollection("x", "y"),
            P2 = new MyCollection(false, true)
        };
        x.RO1.Add("abc");

        TypeWithCollectionMembers y = SerializeAndDeserialize<TypeWithCollectionMembers>(x, "<TypeWithCollectionMembers xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><F1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:char\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/\">97</a:anyType><a:anyType i:type=\"b:int\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">45</a:anyType></F1><F2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">ab</a:anyType><a:anyType i:type=\"b:boolean\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">true</a:anyType></F2><P1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">x</a:anyType><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">y</a:anyType></P1><P2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:boolean\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">false</a:anyType><a:anyType i:type=\"b:boolean\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">true</a:anyType></P2><RO1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">abc</a:anyType></RO1></TypeWithCollectionMembers>");
        Assert.NotNull(y);

        Assert.NotNull(y.F1);
        Assert.True(y.F1.Count == 2);
        Assert.True((char)y.F1[0] == 'a');
        Assert.True((int)y.F1[1] == 45);

        Assert.NotNull(y.F2);
        Assert.True(((object[])y.F2).Length == 2);
        Assert.True((string)((object[])y.F2)[0] == "ab");
        Assert.True((bool)((object[])y.F2)[1] == true);

        Assert.True(y.P1.Count == 2);
        Assert.True((string)y.P1[0] == "x");
        Assert.True((string)y.P1[1] == "y");

        Assert.True(((object[])y.P2).Length == 2);
        Assert.True((bool)((object[])y.P2)[0] == false);
        Assert.True((bool)((object[])y.P2)[1] == true);

        Assert.True(y.RO1.Count == 1);
        Assert.True((string)y.RO1[0] == "abc");
    }

    [Fact]
    public static void DCS_EnumerableRoot()
    {
        MyEnumerable x = new MyEnumerable("abc", 3);
        MyEnumerable y = SerializeAndDeserialize<MyEnumerable>(x, "<ArrayOfanyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><anyType i:type=\"a:string\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">abc</anyType><anyType i:type=\"a:int\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">3</anyType></ArrayOfanyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((string)y[0] == "abc");
        Assert.True((int)y[1] == 3);
    }

    [Fact]
    public static void DCS_EnumerableMembers()
    {
        TypeWithEnumerableMembers x = new TypeWithEnumerableMembers
        {
            F1 = new MyEnumerable('a', 45),
            F2 = new MyEnumerable("ab", true),
            P1 = new MyEnumerable("x", "y"),
            P2 = new MyEnumerable(false, true)
        };
        x.RO1.Add('x');

        TypeWithEnumerableMembers y = SerializeAndDeserialize<TypeWithEnumerableMembers>(x, "<TypeWithEnumerableMembers xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><F1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:char\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/\">97</a:anyType><a:anyType i:type=\"b:int\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">45</a:anyType></F1><F2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">ab</a:anyType><a:anyType i:type=\"b:boolean\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">true</a:anyType></F2><P1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">x</a:anyType><a:anyType i:type=\"b:string\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">y</a:anyType></P1><P2 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:boolean\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">false</a:anyType><a:anyType i:type=\"b:boolean\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">true</a:anyType></P2><RO1 xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"b:char\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/\">120</a:anyType></RO1></TypeWithEnumerableMembers>");
        Assert.NotNull(y);

        Assert.True(y.F1.Count == 2);
        Assert.True((char)y.F1[0] == 'a');
        Assert.True((int)y.F1[1] == 45);

        Assert.True(((object[])y.F2).Length == 2);
        Assert.True((string)((object[])y.F2)[0] == "ab");
        Assert.True((bool)((object[])y.F2)[1] == true);

        Assert.True(y.P1.Count == 2);
        Assert.True((string)y.P1[0] == "x");
        Assert.True((string)y.P1[1] == "y");

        Assert.True(((object[])y.P2).Length == 2);
        Assert.True((bool)((object[])y.P2)[0] == false);
        Assert.True((bool)((object[])y.P2)[1] == true);

        Assert.True(y.RO1.Count == 1);
        Assert.True((char)y.RO1[0] == 'x');
    }

    [Fact]
    public static void DCS_CustomType()
    {
        MyTypeA x = new MyTypeA
        {
            PropX = new MyTypeC { PropC = 'a', PropB = true },
            PropY = 45,
        };

        MyTypeA y = SerializeAndDeserialize<MyTypeA>(x, "<MyTypeA xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><P_Col_Array i:nil=\"true\"/><PropX i:type=\"MyTypeC\"><PropA i:nil=\"true\"/><PropC>97</PropC><PropB>true</PropB></PropX><PropY>45</PropY></MyTypeA>");

        Assert.NotNull(y);
        Assert.NotNull(y.PropX);
        Assert.StrictEqual(x.PropX.PropC, y.PropX.PropC);
        Assert.StrictEqual(((MyTypeC)x.PropX).PropB, ((MyTypeC)y.PropX).PropB);
        Assert.StrictEqual(x.PropY, y.PropY);
    }

    [Fact]
    public static void DCS_DataContractAttribute()
    {
        SerializeAndDeserialize<DCA_1>(new DCA_1 { P1 = "xyz" }, "<DCA_1 xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"/>");
        SerializeAndDeserialize<DCA_2>(new DCA_2 { P1 = "xyz" }, "<abc xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"/>");
        SerializeAndDeserialize<DCA_3>(new DCA_3 { P1 = "xyz" }, "<DCA_3 xmlns=\"def\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"/>");
        SerializeAndDeserialize<DCA_4>(new DCA_4 { P1 = "xyz" }, "<DCA_4 z:Id=\"i1\" xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/>");
        SerializeAndDeserialize<DCA_5>(new DCA_5 { P1 = "xyz" }, "<abc xmlns=\"def\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"/>");
    }

    [Fact]
    public static void DCS_DataMemberAttribute()
    {
        SerializeAndDeserialize<DMA_1>(new DMA_1 { P1 = "abc", P2 = 12, P3 = true, P4 = 'a', P5 = 10, MyDataMemberInAnotherNamespace = new MyDataContractClass04_1() { MyDataMember = "Test" }, Order100 = true, OrderMaxValue = false }, "<DMA_1 xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><MyDataMemberInAnotherNamespace xmlns:a=\"http://MyDataContractClass04_1.com/\"><a:MyDataMember>Test</a:MyDataMember></MyDataMemberInAnotherNamespace><P1>abc</P1><P4>97</P4><P5>10</P5><xyz>12</xyz><P3>true</P3><Order100>true</Order100><OrderMaxValue>false</OrderMaxValue></DMA_1>");
    }

    [Fact]
    public static void DCS_IgnoreDataMemberAttribute()
    {
        IDMA_1 x = new IDMA_1 { MyDataMember = "MyDataMember", MyIgnoreDataMember = "MyIgnoreDataMember", MyUnsetDataMember = "MyUnsetDataMember" };
        IDMA_1 y = SerializeAndDeserialize<IDMA_1>(x, "<IDMA_1 xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><MyDataMember>MyDataMember</MyDataMember></IDMA_1>");
        Assert.NotNull(y);
        Assert.StrictEqual(x.MyDataMember, y.MyDataMember);
        Assert.Null(y.MyIgnoreDataMember);
        Assert.Null(y.MyUnsetDataMember);
    }

    [Fact]
    public static void DCS_EnumAsRoot()
    {
        //The approved types for an enum are byte, sbyte, short, ushort, int, uint, long, or ulong.
        Assert.StrictEqual(SerializeAndDeserialize<MyEnum>(MyEnum.Two, "<MyEnum xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\">Two</MyEnum>"), MyEnum.Two);
        Assert.StrictEqual(SerializeAndDeserialize<ByteEnum>(ByteEnum.Option1, "<ByteEnum xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\">Option1</ByteEnum>"), ByteEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<SByteEnum>(SByteEnum.Option1, "<SByteEnum xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\">Option1</SByteEnum>"), SByteEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<ShortEnum>(ShortEnum.Option1, "<ShortEnum xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\">Option1</ShortEnum>"), ShortEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<IntEnum>(IntEnum.Option1, "<IntEnum xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\">Option1</IntEnum>"), IntEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<UIntEnum>(UIntEnum.Option1, "<UIntEnum xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\">Option1</UIntEnum>"), UIntEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<LongEnum>(LongEnum.Option1, "<LongEnum xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\">Option1</LongEnum>"), LongEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<ULongEnum>(ULongEnum.Option1, "<ULongEnum xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\">Option1</ULongEnum>"), ULongEnum.Option1);
    }

    [Fact]
    public static void DCS_EnumAsMember()
    {
        TypeWithEnumMembers x = new TypeWithEnumMembers { F1 = MyEnum.Three, P1 = MyEnum.Two };
        TypeWithEnumMembers y = SerializeAndDeserialize<TypeWithEnumMembers>(x, "<TypeWithEnumMembers xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><F1>Three</F1><P1>Two</P1></TypeWithEnumMembers>");

        Assert.NotNull(y);
        Assert.StrictEqual(x.F1, y.F1);
        Assert.StrictEqual(x.P1, y.P1);
    }

    [Fact]
    public static void DCS_DCClassWithEnumAndStruct()
    {
        var x = new DCClassWithEnumAndStruct(true);
        var y = SerializeAndDeserialize<DCClassWithEnumAndStruct>(x, "<DCClassWithEnumAndStruct xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><MyEnum1>One</MyEnum1><MyStruct><Data>Data</Data></MyStruct></DCClassWithEnumAndStruct>");

        Assert.StrictEqual(x.MyStruct, y.MyStruct);
        Assert.StrictEqual(x.MyEnum1, y.MyEnum1);
    }

    [Fact]
    public static void DCS_SuspensionManager()
    {
        var x = new Dictionary<string, object>();
        var subDictionary = new Dictionary<string, object>();
        subDictionary.Add("subkey1", "subkey1value");
        x.Add("Key1", subDictionary);

        Dictionary<string, object> y = SerializeAndDeserialize<Dictionary<string, object>>(x, "<ArrayOfKeyValueOfstringanyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><KeyValueOfstringanyType><Key>Key1</Key><Value i:type=\"ArrayOfKeyValueOfstringanyType\"><KeyValueOfstringanyType><Key>subkey1</Key><Value i:type=\"a:string\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">subkey1value</Value></KeyValueOfstringanyType></Value></KeyValueOfstringanyType></ArrayOfKeyValueOfstringanyType>");
        Assert.NotNull(y);
        Assert.StrictEqual(y.Count, 1);
        Assert.True(y["Key1"] is Dictionary<string, object>);
        Assert.StrictEqual(((y["Key1"] as Dictionary<string, object>)["subkey1"]) as string, "subkey1value");
    }

    [Fact]
    public static void DCS_BuiltInTypes()
    {
        BuiltInTypes x = new BuiltInTypes
        {
            ByteArray = new byte[] { 1, 2 }
        };
        BuiltInTypes y = SerializeAndDeserialize<BuiltInTypes>(x, "<BuiltInTypes xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><ByteArray>AQI=</ByteArray></BuiltInTypes>");

        Assert.NotNull(y);
        Assert.Equal<byte>(x.ByteArray, y.ByteArray);
    }

    [Fact]
    public static void DCS_CircularLink()
    {
        CircularLinkDerived circularLinkDerived = new CircularLinkDerived(true);
        SerializeAndDeserialize<CircularLinkDerived>(circularLinkDerived, "<CircularLinkDerived z:Id=\"i1\" xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><Link z:Id=\"i2\"><Link z:Id=\"i3\"><Link z:Ref=\"i1\"/><RandomHangingLink i:nil=\"true\"/></Link><RandomHangingLink i:nil=\"true\"/></Link><RandomHangingLink z:Id=\"i4\"><Link z:Id=\"i5\"><Link z:Id=\"i6\" i:type=\"CircularLinkDerived\"><Link z:Ref=\"i4\"/><RandomHangingLink i:nil=\"true\"/></Link><RandomHangingLink i:nil=\"true\"/></Link><RandomHangingLink i:nil=\"true\"/></RandomHangingLink></CircularLinkDerived>");
    }

    [Fact]
    public static void DCS_GenericBase()
    {
        var actual = SerializeAndDeserialize<GenericBase2<SimpleBaseDerived, SimpleBaseDerived2>>(new GenericBase2<SimpleBaseDerived, SimpleBaseDerived2>(true), "<GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2zbP0weY4 z:Id=\"i1\" xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><genericData1 z:Id=\"i2\"><BaseData/><DerivedData/></genericData1><genericData2 z:Id=\"i3\"><BaseData/><DerivedData/></genericData2></GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2zbP0weY4>");

        Assert.True(actual.genericData1 is SimpleBaseDerived);
        Assert.True(actual.genericData2 is SimpleBaseDerived2);
    }

    [Fact]
    public static void DCS_GenericContainer()
    {
        SerializeAndDeserialize<GenericContainer>(new GenericContainer(true), "<GenericContainer z:Id=\"i1\" xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><GenericData z:Id=\"i2\" i:type=\"GenericBaseOfSimpleBaseContainervjX03eZJ\"><genericData z:Id=\"i3\" i:type=\"SimpleBaseContainer\"><Base1 i:nil=\"true\"/><Base2 i:nil=\"true\"/></genericData></GenericData></GenericContainer>");
    }

    [Fact]
    public static void DCS_DictionaryWithVariousKeyValueTypes()
    {
        var x = new DictionaryWithVariousKeyValueTypes(true);

        var y = SerializeAndDeserialize<DictionaryWithVariousKeyValueTypes>(x, "<DictionaryWithVariousKeyValueTypes xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><WithEnums xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfMyEnumMyEnumzbP0weY4><a:Key>Two</a:Key><a:Value>Three</a:Value></a:KeyValueOfMyEnumMyEnumzbP0weY4><a:KeyValueOfMyEnumMyEnumzbP0weY4><a:Key>One</a:Key><a:Value>One</a:Value></a:KeyValueOfMyEnumMyEnumzbP0weY4></WithEnums><WithNullables xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfNullableOfshortNullableOfboolean_ShTDFhl_P><a:Key>-32768</a:Key><a:Value>true</a:Value></a:KeyValueOfNullableOfshortNullableOfboolean_ShTDFhl_P><a:KeyValueOfNullableOfshortNullableOfboolean_ShTDFhl_P><a:Key>0</a:Key><a:Value>false</a:Value></a:KeyValueOfNullableOfshortNullableOfboolean_ShTDFhl_P><a:KeyValueOfNullableOfshortNullableOfboolean_ShTDFhl_P><a:Key>32767</a:Key><a:Value i:nil=\"true\"/></a:KeyValueOfNullableOfshortNullableOfboolean_ShTDFhl_P></WithNullables><WithStructs xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfStructNotSerializableStructNotSerializablezbP0weY4><a:Key><value>10</value></a:Key><a:Value><value>12</value></a:Value></a:KeyValueOfStructNotSerializableStructNotSerializablezbP0weY4><a:KeyValueOfStructNotSerializableStructNotSerializablezbP0weY4><a:Key><value>2147483647</value></a:Key><a:Value><value>-2147483648</value></a:Value></a:KeyValueOfStructNotSerializableStructNotSerializablezbP0weY4></WithStructs></DictionaryWithVariousKeyValueTypes>");

        Assert.StrictEqual(y.WithEnums[MyEnum.Two], MyEnum.Three);
        Assert.StrictEqual(y.WithEnums[MyEnum.One], MyEnum.One);
        Assert.StrictEqual(y.WithStructs[new StructNotSerializable() { value = 10 }], new StructNotSerializable() { value = 12 });
        Assert.StrictEqual(y.WithStructs[new StructNotSerializable() { value = int.MaxValue }], new StructNotSerializable() { value = int.MinValue });
        Assert.StrictEqual(y.WithNullables[Int16.MinValue], true);
        Assert.StrictEqual(y.WithNullables[0], false);
        Assert.StrictEqual(y.WithNullables[Int16.MaxValue], null);
    }

    [Fact]
    public static void DCS_TypesWithArrayOfOtherTypes()
    {
        var x = new TypeHasArrayOfASerializedAsB(true);
        var y = SerializeAndDeserialize<TypeHasArrayOfASerializedAsB>(x, "<TypeHasArrayOfASerializedAsB xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Items><TypeA><Name>typeAValue</Name></TypeA><TypeA><Name>typeBValue</Name></TypeA></Items></TypeHasArrayOfASerializedAsB>");

        Assert.StrictEqual(x.Items[0].Name, y.Items[0].Name);
        Assert.StrictEqual(x.Items[1].Name, y.Items[1].Name);
    }

    [Fact]
    public static void DCS_WithDuplicateNames()
    {
        var x = new WithDuplicateNames(true);
        var y = SerializeAndDeserialize<WithDuplicateNames>(x, "<WithDuplicateNames xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><ClassA1 xmlns:a=\"http://schemas.datacontract.org/2004/07/DuplicateTypeNamesTest.ns1\"><a:Name>Hello World! 漢 ñ</a:Name></ClassA1><ClassA2 xmlns:a=\"http://schemas.datacontract.org/2004/07/DuplicateTypeNamesTest.ns2\"><a:Nombre/></ClassA2><EnumA1>two</EnumA1><EnumA2>dos</EnumA2><StructA1 xmlns:a=\"http://schemas.datacontract.org/2004/07/DuplicateTypeNamesTest.ns1\"><a:Text/></StructA1><StructA2 xmlns:a=\"http://schemas.datacontract.org/2004/07/DuplicateTypeNamesTest.ns2\"><a:Texto/></StructA2></WithDuplicateNames>");

        Assert.StrictEqual(x.ClassA1.Name, y.ClassA1.Name);
        Assert.StrictEqual(x.StructA1, y.StructA1);
        Assert.StrictEqual(x.EnumA1, y.EnumA1);
        Assert.StrictEqual(x.EnumA2, y.EnumA2);
        Assert.StrictEqual(x.StructA2, y.StructA2);
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void DCS_XElementAsRoot()
    {
        var original = new XElement("ElementName1");
        original.SetAttributeValue(XName.Get("Attribute1"), "AttributeValue1");
        original.SetValue("Value1");
        var actual = SerializeAndDeserialize<XElement>(original, "<ElementName1 Attribute1=\"AttributeValue1\">Value1</ElementName1>");

        VerifyXElementObject(original, actual);
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void DCS_WithXElement()
    {
        var original = new WithXElement(true);
        var actual = SerializeAndDeserialize<WithXElement>(original, "<WithXElement xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><e><ElementName1 Attribute1=\"AttributeValue1\" xmlns=\"\">Value1</ElementName1></e></WithXElement>");

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
    public static void DCS_WithXElementWithNestedXElement()
    {
        var original = new WithXElementWithNestedXElement(true);
        var actual = SerializeAndDeserialize<WithXElementWithNestedXElement>(original, "<WithXElementWithNestedXElement xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><e1><ElementName1 Attribute1=\"AttributeValue1\" xmlns=\"\"><ElementName2 Attribute2=\"AttributeValue2\">Value2</ElementName2></ElementName1></e1></WithXElementWithNestedXElement>");

        VerifyXElementObject(original.e1, actual.e1);
        VerifyXElementObject((XElement)original.e1.FirstNode, (XElement)actual.e1.FirstNode);
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void DCS_WithArrayOfXElement()
    {
        var original = new WithArrayOfXElement(true);
        var actual = SerializeAndDeserialize<WithArrayOfXElement>(original, "<WithArrayOfXElement xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><a xmlns:a=\"http://schemas.datacontract.org/2004/07/System.Xml.Linq\"><a:XElement><item xmlns=\"http://p.com/\">item0</item></a:XElement><a:XElement><item xmlns=\"http://p.com/\">item1</item></a:XElement><a:XElement><item xmlns=\"http://p.com/\">item2</item></a:XElement></a></WithArrayOfXElement>");

        Assert.StrictEqual(original.a.Length, actual.a.Length);
        VerifyXElementObject(original.a[0], actual.a[0], checkFirstAttribute: false);
        VerifyXElementObject(original.a[1], actual.a[1], checkFirstAttribute: false);
        VerifyXElementObject(original.a[2], actual.a[2], checkFirstAttribute: false);
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void DCS_WithListOfXElement()
    {
        var original = new WithListOfXElement(true);
        var actual = SerializeAndDeserialize<WithListOfXElement>(original, "<WithListOfXElement xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><list xmlns:a=\"http://schemas.datacontract.org/2004/07/System.Xml.Linq\"><a:XElement><item xmlns=\"http://p.com/\">item0</item></a:XElement><a:XElement><item xmlns=\"http://p.com/\">item1</item></a:XElement><a:XElement><item xmlns=\"http://p.com/\">item2</item></a:XElement></list></WithListOfXElement>");

        Assert.StrictEqual(original.list.Count, actual.list.Count);
        VerifyXElementObject(original.list[0], actual.list[0], checkFirstAttribute: false);
        VerifyXElementObject(original.list[1], actual.list[1], checkFirstAttribute: false);
        VerifyXElementObject(original.list[2], actual.list[2], checkFirstAttribute: false);
    }

    [Fact]
    public static void DCS_DerivedTypeWithDifferentOverrides()
    {
        var x = new DerivedTypeWithDifferentOverrides() { Name1 = "Name1", Name2 = "Name2", Name3 = "Name3", Name4 = "Name4", Name5 = "Name5" };
        var y = SerializeAndDeserialize<DerivedTypeWithDifferentOverrides>(x, "<DerivedTypeWithDifferentOverrides xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Name1>Name1</Name1><Name2 i:nil=\"true\"/><Name3 i:nil=\"true\"/><Name4 i:nil=\"true\"/><Name5 i:nil=\"true\"/><Name2>Name2</Name2><Name3>Name3</Name3><Name5>Name5</Name5></DerivedTypeWithDifferentOverrides>");

        Assert.StrictEqual(x.Name1, y.Name1);
        Assert.StrictEqual(x.Name2, y.Name2);
        Assert.StrictEqual(x.Name3, y.Name3);
        Assert.Null(y.Name4);
        Assert.StrictEqual(x.Name5, y.Name5);
    }

    [Fact]
    public static void DCS_TypeNamesWithSpecialCharacters()
    {
        var x = new __TypeNameWithSpecialCharacters漢ñ() { PropertyNameWithSpecialCharacters漢ñ = "Test" };
        var y = SerializeAndDeserialize<__TypeNameWithSpecialCharacters漢ñ>(x, "<__TypeNameWithSpecialCharacters漢ñ xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PropertyNameWithSpecialCharacters漢ñ>Test</PropertyNameWithSpecialCharacters漢ñ></__TypeNameWithSpecialCharacters漢ñ>");

        Assert.StrictEqual(x.PropertyNameWithSpecialCharacters漢ñ, y.PropertyNameWithSpecialCharacters漢ñ);
    }

    [Fact]
    public static void DCS_JaggedArrayAsRoot()
    {
        int[][] jaggedIntegerArray = new int[][] { new int[] { 1, 3, 5, 7, 9 }, new int[] { 0, 2, 4, 6 }, new int[] { 11, 22 } };
        var actualJaggedIntegerArray = SerializeAndDeserialize<int[][]>(jaggedIntegerArray, "<ArrayOfArrayOfint xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><ArrayOfint><int>1</int><int>3</int><int>5</int><int>7</int><int>9</int></ArrayOfint><ArrayOfint><int>0</int><int>2</int><int>4</int><int>6</int></ArrayOfint><ArrayOfint><int>11</int><int>22</int></ArrayOfint></ArrayOfArrayOfint>");

        Assert.Equal<int>(jaggedIntegerArray[0], actualJaggedIntegerArray[0]);
        Assert.Equal<int>(jaggedIntegerArray[1], actualJaggedIntegerArray[1]);
        Assert.Equal<int>(jaggedIntegerArray[2], actualJaggedIntegerArray[2]);

        string[][] jaggedStringArray = new string[][] { new string[] { "1", "3", "5", "7", "9" }, new string[] { "0", "2", "4", "6" }, new string[] { "11", "22" } };
        var actualJaggedStringArray = SerializeAndDeserialize<string[][]>(jaggedStringArray, "<ArrayOfArrayOfstring xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><ArrayOfstring><string>1</string><string>3</string><string>5</string><string>7</string><string>9</string></ArrayOfstring><ArrayOfstring><string>0</string><string>2</string><string>4</string><string>6</string></ArrayOfstring><ArrayOfstring><string>11</string><string>22</string></ArrayOfstring></ArrayOfArrayOfstring>");

        Assert.Equal<string>(jaggedStringArray[0], actualJaggedStringArray[0]);
        Assert.Equal<string>(jaggedStringArray[1], actualJaggedStringArray[1]);
        Assert.Equal<string>(jaggedStringArray[2], actualJaggedStringArray[2]);

        object[] objectArray = new object[] { 1, 1.0F, 1.0, "string", Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860"), new DateTime(2013, 1, 2) };
        var actualObjectArray = SerializeAndDeserialize<object[]>(objectArray, "<ArrayOfanyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><anyType i:type=\"a:int\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">1</anyType><anyType i:type=\"a:float\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">1</anyType><anyType i:type=\"a:double\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">1</anyType><anyType i:type=\"a:string\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">string</anyType><anyType i:type=\"a:guid\" xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/\">2054fd3e-e118-476a-9962-1a882be51860</anyType><anyType i:type=\"a:dateTime\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">2013-01-02T00:00:00</anyType></ArrayOfanyType>");

        Assert.True(1 == (int)actualObjectArray[0]);
        Assert.True(1.0F == (float)actualObjectArray[1]);
        Assert.True(1.0 == (double)actualObjectArray[2]);
        Assert.True("string" == (string)actualObjectArray[3]);
        Assert.True(Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860") == (Guid)actualObjectArray[4]);
        Assert.True(new DateTime(2013, 1, 2) == (DateTime)actualObjectArray[5]);

        int[][][] jaggedIntegerArray2 = new int[][][] { new int[][] { new int[] { 1 }, new int[] { 3 } }, new int[][] { new int[] { 0 } }, new int[][] { new int[] { } } };
        var actualJaggedIntegerArray2 = SerializeAndDeserialize<int[][][]>(jaggedIntegerArray2, "<ArrayOfArrayOfArrayOfint xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><ArrayOfArrayOfint><ArrayOfint><int>1</int></ArrayOfint><ArrayOfint><int>3</int></ArrayOfint></ArrayOfArrayOfint><ArrayOfArrayOfint><ArrayOfint><int>0</int></ArrayOfint></ArrayOfArrayOfint><ArrayOfArrayOfint><ArrayOfint/></ArrayOfArrayOfint></ArrayOfArrayOfArrayOfint>");

        Assert.True(actualJaggedIntegerArray2.Length == 3);
        Assert.True(actualJaggedIntegerArray2[0][0][0] == 1);
        Assert.True(actualJaggedIntegerArray2[0][1][0] == 3);
        Assert.True(actualJaggedIntegerArray2[1][0][0] == 0);
        Assert.True(actualJaggedIntegerArray2[2][0].Length == 0);
    }

    [Fact]
    public static void DCS_MyDataContractResolver()
    {
        var myresolver = new MyResolver();
        var settings = new DataContractSerializerSettings() { DataContractResolver = myresolver, KnownTypes = new Type[] { typeof(MyOtherType) } };
        var input = new MyType() { Value = new MyOtherType() { Str = "Hello World" } };
        var output = SerializeAndDeserialize<MyType>(input, "<MyType xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Value i:type=\"MyOtherType\"><Str>Hello World</Str></Value></MyType>", settings);

        Assert.True(myresolver.ResolveNameInvoked, "myresolver.ResolveNameInvoked is false");
        Assert.True(myresolver.TryResolveTypeInvoked, "myresolver.TryResolveTypeInvoked is false");
        Assert.True(input.OnSerializingMethodInvoked, "input.OnSerializingMethodInvoked is false");
        Assert.True(input.OnSerializedMethodInvoked, "input.OnSerializedMethodInvoked is false");
        Assert.True(output.OnDeserializingMethodInvoked, "output.OnDeserializingMethodInvoked is false");
        Assert.True(output.OnDeserializedMethodInvoked, "output.OnDeserializedMethodInvoked is false");
    }

    [Fact]
    public static void DCS_EnumerableStruct()
    {
        var original = new EnumerableStruct();
        original.Add("a");
        original.Add("b");

        var actual = SerializeAndDeserialize<EnumerableStruct>(original, "<ArrayOfstring xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><string i:type=\"a:string\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">a</string><string i:type=\"a:string\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">b</string></ArrayOfstring>");

        Assert.Equal((IEnumerable<string>)actual, (IEnumerable<string>)original);
    }

    [Fact]
    public static void DCS_EnumerableCollection()
    {
        var original = new EnumerableCollection();
        original.Add(new DateTime(100, DateTimeKind.Utc));
        original.Add(new DateTime(200, DateTimeKind.Utc));
        original.Add(new DateTime(300, DateTimeKind.Utc));

        var actual = SerializeAndDeserialize<EnumerableCollection>(original, "<ArrayOfdateTime xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><dateTime i:type=\"a:dateTime\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">0001-01-01T00:00:00.00001Z</dateTime><dateTime i:type=\"a:dateTime\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">0001-01-01T00:00:00.00002Z</dateTime><dateTime i:type=\"a:dateTime\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">0001-01-01T00:00:00.00003Z</dateTime></ArrayOfdateTime>");

        Assert.Equal((IEnumerable<DateTime>)actual, (IEnumerable<DateTime>)original);
    }

    [Fact]
    public static void DCS_BaseClassAndDerivedClassWithSameProperty()
    {
        var value = new DerivedClassWithSameProperty() { DateTimeProperty = new DateTime(100), IntProperty = 5, StringProperty = "TestString", ListProperty = new List<string>() };
        value.ListProperty.AddRange(new string[] { "one", "two", "three" });
        var actual = SerializeAndDeserialize<DerivedClassWithSameProperty>(value, "<DerivedClassWithSameProperty xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><DateTimeProperty>0001-01-01T00:00:00</DateTimeProperty><IntProperty>0</IntProperty><ListProperty i:nil=\"true\" xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><StringProperty i:nil=\"true\"/><DateTimeProperty>0001-01-01T00:00:00.00001</DateTimeProperty><IntProperty>5</IntProperty><ListProperty xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:string>one</a:string><a:string>two</a:string><a:string>three</a:string></ListProperty><StringProperty>TestString</StringProperty></DerivedClassWithSameProperty>");

        Assert.StrictEqual(value.DateTimeProperty, actual.DateTimeProperty);
        Assert.StrictEqual(value.IntProperty, actual.IntProperty);
        Assert.StrictEqual(value.StringProperty, actual.StringProperty);
        Assert.NotNull(actual.ListProperty);
        Assert.True(value.ListProperty.Count == actual.ListProperty.Count);
        Assert.StrictEqual(value.ListProperty[0], actual.ListProperty[0]);
        Assert.StrictEqual(value.ListProperty[1], actual.ListProperty[1]);
        Assert.StrictEqual(value.ListProperty[2], actual.ListProperty[2]);
    }

    [Fact]
    public static void DCS_ContainsLinkedList()
    {
        var value = new ContainsLinkedList(true);

        SerializeAndDeserialize<ContainsLinkedList>(value, "<ContainsLinkedList xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Data><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef></Data></ContainsLinkedList>");
    }

    [Fact]
    public static void DCS_SimpleCollectionDataContract()
    {
        var value = new SimpleCDC(true);
        var actual = SerializeAndDeserialize<SimpleCDC>(value, "<SimpleCDC xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Item>One</Item><Item>Two</Item><Item>Three</Item></SimpleCDC>");

        Assert.True(actual.Count == 3);
        Assert.True(actual.Contains("One"));
        Assert.True(actual.Contains("Two"));
        Assert.True(actual.Contains("Three"));
    }

    [Fact]
    public static void DCS_MyDerivedCollectionContainer()
    {
        var value = new MyDerivedCollectionContainer();
        value.Items.AddLast("One");
        value.Items.AddLast("Two");
        value.Items.AddLast("Three");
        SerializeAndDeserialize<MyDerivedCollectionContainer>(value, "<MyDerivedCollectionContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Items><string>One</string><string>Two</string><string>Three</string></Items></MyDerivedCollectionContainer>");
    }

    [Fact]
    public static void DCS_EnumFlags()
    {
        EnumFlags value1 = EnumFlags.One | EnumFlags.Four;
        var value2 = SerializeAndDeserialize<EnumFlags>(value1, "<EnumFlags xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\">One Four</EnumFlags>");
        Assert.StrictEqual(value1, value2);
    }

    [Fact]
    public static void DCS_SerializeClassThatImplementsInteface()
    {
        ClassImplementsInterface value = new ClassImplementsInterface() { ClassID = "ClassID", DisplayName = "DisplayName", Id = "Id", IsLoaded = true };
        var actual = SerializeAndDeserialize<ClassImplementsInterface>(value, "<ClassImplementsInterface xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><DisplayName>DisplayName</DisplayName><Id>Id</Id></ClassImplementsInterface>");

        Assert.StrictEqual(value.DisplayName, actual.DisplayName);
        Assert.StrictEqual(value.Id, actual.Id);
    }

    [Fact]
    public static void DCS_Nullables()
    {
        // Arrange
        var baseline = @"<WithNullables xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Optional>Option1</Optional><OptionalInt>42</OptionalInt><Optionull i:nil=""true""/><OptionullInt i:nil=""true""/><Struct1><A>1</A><B>2</B></Struct1><Struct2 i:nil=""true""/></WithNullables>";

        var item = new WithNullables()
        {
            Optional = IntEnum.Option1,
            OptionalInt = 42,
            Struct1 = new SomeStruct { A = 1, B = 2 }
        };

        // Act
        var actual = SerializeAndDeserialize(item, baseline);

        // Assert
        Assert.StrictEqual(item.OptionalInt, actual.OptionalInt);
        Assert.StrictEqual(item.Optional, actual.Optional);
        Assert.StrictEqual(item.Optionull, actual.Optionull);
        Assert.StrictEqual(item.OptionullInt, actual.OptionullInt);
        Assert.Null(actual.Struct2);
        Assert.StrictEqual(item.Struct1.Value.A, actual.Struct1.Value.A);
        Assert.StrictEqual(item.Struct1.Value.B, actual.Struct1.Value.B);
    }

    [Fact]
    public static void DCS_InternalTypeSerialization()
    {
        var value = new InternalType() { InternalProperty = 12 };
        var deserializedValue = SerializeAndDeserialize<InternalType>(value, "<InternalType xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><InternalProperty>12</InternalProperty><PrivateProperty>100</PrivateProperty></InternalType>");
        Assert.StrictEqual(deserializedValue.InternalProperty, value.InternalProperty);
        Assert.StrictEqual(deserializedValue.GetPrivatePropertyValue(), value.GetPrivatePropertyValue());
    }

    [Fact]
    public static void DCS_PrivateTypeSerialization()
    {
        var value = new PrivateType();
        var deserializedValue = SerializeAndDeserialize<PrivateType>(value, "<DataContractSerializerTests.PrivateType xmlns=\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><InternalProperty>1</InternalProperty><PrivateProperty>2</PrivateProperty></DataContractSerializerTests.PrivateType>");
        Assert.StrictEqual(deserializedValue.GetInternalPropertyValue(), value.GetInternalPropertyValue());
        Assert.StrictEqual(deserializedValue.GetPrivatePropertyValue(), value.GetPrivatePropertyValue());
    }

    #region private type has to be in with in the class
    [DataContract]
    private class PrivateType
    {
        public PrivateType()
        {
            InternalProperty = 1;
            PrivateProperty = 2;
        }

        [DataMember]
        internal int InternalProperty { get; set; }

        [DataMember]
        private int PrivateProperty { get; set; }

        public int GetInternalPropertyValue()
        {
            return InternalProperty;
        }

        public int GetPrivatePropertyValue()
        {
            return PrivateProperty;
        }
    }

    #endregion

    [Fact]
    public static void DCS_RootNameAndNamespaceThroughConstructorAsString()
    {
        //Constructor# 3
        var obj = new MyOtherType() { Str = "Hello" };
        Func<DataContractSerializer> serializerFactory = () => new DataContractSerializer(typeof(MyOtherType), "ChangedRoot", "http://changedNamespace");
        string baselineXml = "<ChangedRoot xmlns=\"http://changedNamespace\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><a:Str>Hello</a:Str></ChangedRoot>";
        var result = SerializeAndDeserialize(obj, baselineXml, serializerFactory: serializerFactory);
        Assert.StrictEqual(result.Str, "Hello");
    }

    [Fact]
    public static void DCS_RootNameAndNamespaceThroughConstructorAsXmlDictionary()
    {
        //Constructor# 4
        var xmlDictionary = new XmlDictionary();
        var obj = new MyOtherType() { Str = "Hello" };
        Func<DataContractSerializer> serializerFactory = () => new DataContractSerializer(typeof(MyOtherType), xmlDictionary.Add("ChangedRoot"), xmlDictionary.Add("http://changedNamespace"));
        string baselineXml = "<ChangedRoot xmlns=\"http://changedNamespace\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><a:Str>Hello</a:Str></ChangedRoot>";
        var result = SerializeAndDeserialize(obj, baselineXml, serializerFactory: serializerFactory);
        Assert.StrictEqual(result.Str, "Hello");
    }

    [Fact]
    public static void DCS_KnownTypesThroughConstructor()
    {
        //Constructor# 5
        var value = new KnownTypesThroughConstructor() { EnumValue = MyEnum.One, SimpleTypeValue = new SimpleKnownTypeValue() { StrProperty = "PropertyValue" } };
        var actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value,
        "<KnownTypesThroughConstructor xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><EnumValue i:type=\"MyEnum\">One</EnumValue><SimpleTypeValue i:type=\"SimpleKnownTypeValue\"><StrProperty>PropertyValue</StrProperty></SimpleTypeValue></KnownTypesThroughConstructor>",
        null, () => { return new DataContractSerializer(typeof(KnownTypesThroughConstructor), new Type[] { typeof(MyEnum), typeof(SimpleKnownTypeValue) }); });

        Assert.StrictEqual((MyEnum)value.EnumValue, (MyEnum)actual.EnumValue);
        Assert.True(actual.SimpleTypeValue is SimpleKnownTypeValue);
        Assert.StrictEqual(((SimpleKnownTypeValue)actual.SimpleTypeValue).StrProperty, "PropertyValue");
    }

    [Fact]
    public static void DCS_KnownTypesThroughSettings()
    {
        //Constructor# 2.1
        var value = new KnownTypesThroughConstructor() { EnumValue = MyEnum.One, SimpleTypeValue = new SimpleKnownTypeValue() { StrProperty = "PropertyValue" } };
        var actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value,
        "<KnownTypesThroughConstructor xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><EnumValue i:type=\"MyEnum\">One</EnumValue><SimpleTypeValue i:type=\"SimpleKnownTypeValue\"><StrProperty>PropertyValue</StrProperty></SimpleTypeValue></KnownTypesThroughConstructor>",
        new DataContractSerializerSettings() { KnownTypes = new Type[] { typeof(MyEnum), typeof(SimpleKnownTypeValue) } });

        Assert.StrictEqual((MyEnum)value.EnumValue, (MyEnum)actual.EnumValue);
        Assert.True(actual.SimpleTypeValue is SimpleKnownTypeValue);
        Assert.StrictEqual(((SimpleKnownTypeValue)actual.SimpleTypeValue).StrProperty, "PropertyValue");
    }

    [Fact]
    public static void DCS_RootNameNamespaceAndKnownTypesThroughConstructorAsStrings()
    {
        //Constructor# 6
        var value = new KnownTypesThroughConstructor() { EnumValue = MyEnum.One, SimpleTypeValue = new SimpleKnownTypeValue() { StrProperty = "PropertyValue" } };
        var actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value,
        "<ChangedRoot xmlns=\"http://changedNamespace\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><a:EnumValue i:type=\"a:MyEnum\">One</a:EnumValue><a:SimpleTypeValue i:type=\"a:SimpleKnownTypeValue\"><a:StrProperty>PropertyValue</a:StrProperty></a:SimpleTypeValue></ChangedRoot>",
        null, () => { return new DataContractSerializer(typeof(KnownTypesThroughConstructor), "ChangedRoot", "http://changedNamespace", new Type[] { typeof(MyEnum), typeof(SimpleKnownTypeValue) }); });

        Assert.StrictEqual((MyEnum)value.EnumValue, (MyEnum)actual.EnumValue);
        Assert.True(actual.SimpleTypeValue is SimpleKnownTypeValue);
        Assert.StrictEqual(((SimpleKnownTypeValue)actual.SimpleTypeValue).StrProperty, "PropertyValue");
    }

    [Fact]
    public static void DCS_RootNameNamespaceAndKnownTypesThroughConstructorAsXmlDictionary()
    {
        //Constructor# 7
        var xmlDictionary = new XmlDictionary();
        var value = new KnownTypesThroughConstructor() { EnumValue = MyEnum.One, SimpleTypeValue = new SimpleKnownTypeValue() { StrProperty = "PropertyValue" } };
        var actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value,
        "<ChangedRoot xmlns=\"http://changedNamespace\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><a:EnumValue i:type=\"a:MyEnum\">One</a:EnumValue><a:SimpleTypeValue i:type=\"a:SimpleKnownTypeValue\"><a:StrProperty>PropertyValue</a:StrProperty></a:SimpleTypeValue></ChangedRoot>",
        null, () => { return new DataContractSerializer(typeof(KnownTypesThroughConstructor), xmlDictionary.Add("ChangedRoot"), xmlDictionary.Add("http://changedNamespace"), new Type[] { typeof(MyEnum), typeof(SimpleKnownTypeValue) }); });

        Assert.StrictEqual((MyEnum)value.EnumValue, (MyEnum)actual.EnumValue);
        Assert.True(actual.SimpleTypeValue is SimpleKnownTypeValue);
        Assert.StrictEqual(((SimpleKnownTypeValue)actual.SimpleTypeValue).StrProperty, "PropertyValue");
    }

    [Fact]
    public static void DCS_ExceptionObject()
    {
        var value = new ArgumentException("Test Exception");
        var actual = SerializeAndDeserialize<ArgumentException>(value, "<ArgumentException xmlns=\"http://schemas.datacontract.org/2004/07/System\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:x=\"http://www.w3.org/2001/XMLSchema\"><ClassName i:type=\"x:string\" xmlns=\"\">System.ArgumentException</ClassName><Message i:type=\"x:string\" xmlns=\"\">Test Exception</Message><Data i:nil=\"true\" xmlns=\"\"/><InnerException i:nil=\"true\" xmlns=\"\"/><HelpURL i:nil=\"true\" xmlns=\"\"/><StackTraceString i:nil=\"true\" xmlns=\"\"/><RemoteStackTraceString i:nil=\"true\" xmlns=\"\"/><RemoteStackIndex i:type=\"x:int\" xmlns=\"\">0</RemoteStackIndex><ExceptionMethod i:nil=\"true\" xmlns=\"\"/><HResult i:type=\"x:int\" xmlns=\"\">-2147024809</HResult><Source i:nil=\"true\" xmlns=\"\"/><WatsonBuckets i:nil=\"true\" xmlns=\"\"/><ParamName i:nil=\"true\" xmlns=\"\"/></ArgumentException>");

        Assert.StrictEqual(value.Message, actual.Message);
        Assert.StrictEqual(value.ParamName, actual.ParamName);
        Assert.StrictEqual(value.Source, actual.Source);
        Assert.StrictEqual(value.StackTrace, actual.StackTrace);
        Assert.StrictEqual(value.HResult, actual.HResult);
        Assert.StrictEqual(value.HelpLink, actual.HelpLink);
    }

    [Fact]
    public static void DCS_TypeWithUriTypeProperty()
    {
        var value = new TypeWithUriTypeProperty() { ConfigUri = new Uri("http://www.bing.com") };

        var actual = SerializeAndDeserialize(value, "<TypeWithUriTypeProperty xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><ConfigUri>http://www.bing.com/</ConfigUri></TypeWithUriTypeProperty>");

        Assert.StrictEqual(value.ConfigUri, actual.ConfigUri);
    }

    [Fact]
    public static void DCS_TypeWithDatetimeOffsetTypeProperty()
    {
        var value = new TypeWithDateTimeOffsetTypeProperty() { ModifiedTime = new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc)) };
        var actual = SerializeAndDeserialize(value, "<TypeWithDateTimeOffsetTypeProperty xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><ModifiedTime xmlns:a=\"http://schemas.datacontract.org/2004/07/System\"><a:DateTime>2013-01-02T03:04:05.006Z</a:DateTime><a:OffsetMinutes>0</a:OffsetMinutes></ModifiedTime></TypeWithDateTimeOffsetTypeProperty>");
        Assert.StrictEqual(value.ModifiedTime, actual.ModifiedTime);
        Assert.StrictEqual(value.ModifiedTime.Offset, actual.ModifiedTime.Offset);

        var offsetMinutes = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2013, 1, 2)).TotalMinutes;
        // Adding offsetMinutes to ModifiedTime property so the DateTime component in serialized strings are time-zone independent
        value = new TypeWithDateTimeOffsetTypeProperty() { ModifiedTime = new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6).AddMinutes(offsetMinutes)) };
        actual = SerializeAndDeserialize(value, string.Format("<TypeWithDateTimeOffsetTypeProperty xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><ModifiedTime xmlns:a=\"http://schemas.datacontract.org/2004/07/System\"><a:DateTime>2013-01-02T03:04:05.006Z</a:DateTime><a:OffsetMinutes>{0}</a:OffsetMinutes></ModifiedTime></TypeWithDateTimeOffsetTypeProperty>", offsetMinutes));
        Assert.StrictEqual(value.ModifiedTime, actual.ModifiedTime);
        Assert.StrictEqual(value.ModifiedTime.Offset, actual.ModifiedTime.Offset);

        value = new TypeWithDateTimeOffsetTypeProperty() { ModifiedTime = new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local).AddMinutes(offsetMinutes)) };
        actual = SerializeAndDeserialize(value, string.Format("<TypeWithDateTimeOffsetTypeProperty xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><ModifiedTime xmlns:a=\"http://schemas.datacontract.org/2004/07/System\"><a:DateTime>2013-01-02T03:04:05.006Z</a:DateTime><a:OffsetMinutes>{0}</a:OffsetMinutes></ModifiedTime></TypeWithDateTimeOffsetTypeProperty>", offsetMinutes));
        Assert.StrictEqual(value.ModifiedTime, actual.ModifiedTime);
        Assert.StrictEqual(value.ModifiedTime.Offset, actual.ModifiedTime.Offset);
    }

    [Fact]
    public static void DCS_Tuple()
    {
        DCS_Tuple1();
        DCS_Tuple2();
        DCS_Tuple3();
        DCS_Tuple4();
        DCS_Tuple5();
        DCS_Tuple6();
        DCS_Tuple7();
        DCS_Tuple8();
    }

    private static void DCS_Tuple1()
    {
        Tuple<int> value = new Tuple<int>(1);
        var deserializedValue = SerializeAndDeserialize<Tuple<int>>(value, "<TupleOfint xmlns=\"http://schemas.datacontract.org/2004/07/System\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><m_Item1>1</m_Item1></TupleOfint>");
        Assert.StrictEqual<Tuple<int>>(value, deserializedValue);
    }

    private static void DCS_Tuple2()
    {
        Tuple<int, int> value = new Tuple<int, int>(1, 2);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int>>(value, "<TupleOfintint xmlns=\"http://schemas.datacontract.org/2004/07/System\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><m_Item1>1</m_Item1><m_Item2>2</m_Item2></TupleOfintint>");
        Assert.StrictEqual<Tuple<int, int>>(value, deserializedValue);
    }

    private static void DCS_Tuple3()
    {
        Tuple<int, int, int> value = new Tuple<int, int, int>(1, 2, 3);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int>>(value, "<TupleOfintintint xmlns=\"http://schemas.datacontract.org/2004/07/System\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><m_Item1>1</m_Item1><m_Item2>2</m_Item2><m_Item3>3</m_Item3></TupleOfintintint>");
        Assert.StrictEqual<Tuple<int, int, int>>(value, deserializedValue);
    }

    private static void DCS_Tuple4()
    {
        Tuple<int, int, int, int> value = new Tuple<int, int, int, int>(1, 2, 3, 4);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int>>(value, "<TupleOfintintintint xmlns=\"http://schemas.datacontract.org/2004/07/System\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><m_Item1>1</m_Item1><m_Item2>2</m_Item2><m_Item3>3</m_Item3><m_Item4>4</m_Item4></TupleOfintintintint>");
        Assert.StrictEqual<Tuple<int, int, int, int>>(value, deserializedValue);
    }

    private static void DCS_Tuple5()
    {
        Tuple<int, int, int, int, int> value = new Tuple<int, int, int, int, int>(1, 2, 3, 4, 5);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int, int>>(value, "<TupleOfintintintintint xmlns=\"http://schemas.datacontract.org/2004/07/System\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><m_Item1>1</m_Item1><m_Item2>2</m_Item2><m_Item3>3</m_Item3><m_Item4>4</m_Item4><m_Item5>5</m_Item5></TupleOfintintintintint>");
        Assert.StrictEqual<Tuple<int, int, int, int, int>>(value, deserializedValue);
    }

    private static void DCS_Tuple6()
    {
        Tuple<int, int, int, int, int, int> value = new Tuple<int, int, int, int, int, int>(1, 2, 3, 4, 5, 6);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int, int, int>>(value, "<TupleOfintintintintintint xmlns=\"http://schemas.datacontract.org/2004/07/System\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><m_Item1>1</m_Item1><m_Item2>2</m_Item2><m_Item3>3</m_Item3><m_Item4>4</m_Item4><m_Item5>5</m_Item5><m_Item6>6</m_Item6></TupleOfintintintintintint>");
        Assert.StrictEqual<Tuple<int, int, int, int, int, int>>(value, deserializedValue);
    }

    private static void DCS_Tuple7()
    {
        Tuple<int, int, int, int, int, int, int> value = new Tuple<int, int, int, int, int, int, int>(1, 2, 3, 4, 5, 6, 7);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int, int, int, int>>(value, "<TupleOfintintintintintintint xmlns=\"http://schemas.datacontract.org/2004/07/System\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><m_Item1>1</m_Item1><m_Item2>2</m_Item2><m_Item3>3</m_Item3><m_Item4>4</m_Item4><m_Item5>5</m_Item5><m_Item6>6</m_Item6><m_Item7>7</m_Item7></TupleOfintintintintintintint>");
        Assert.StrictEqual<Tuple<int, int, int, int, int, int, int>>(value, deserializedValue);
    }

    private static void DCS_Tuple8()
    {
        Tuple<int, int, int, int, int, int, int, Tuple<int>> value = new Tuple<int, int, int, int, int, int, int, Tuple<int>>(1, 2, 3, 4, 5, 6, 7, new Tuple<int>(8));
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int, int, int, int, Tuple<int>>>(value, "<TupleOfintintintintintintintTupleOfintcd6ORBnm xmlns=\"http://schemas.datacontract.org/2004/07/System\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><m_Item1>1</m_Item1><m_Item2>2</m_Item2><m_Item3>3</m_Item3><m_Item4>4</m_Item4><m_Item5>5</m_Item5><m_Item6>6</m_Item6><m_Item7>7</m_Item7><m_Rest><m_Item1>8</m_Item1></m_Rest></TupleOfintintintintintintintTupleOfintcd6ORBnm>");
        Assert.StrictEqual<Tuple<int, int, int, int, int, int, int, Tuple<int>>>(value, deserializedValue);
    }

    [Fact]
    public static void DCS_GenericQueue()
    {
        var expectedFormat = @"<QueueOfint xmlns=""http://schemas.datacontract.org/2004/07/System.Collections.Generic"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_array xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:int>1</a:int><a:int>0</a:int><a:int>0</a:int><a:int>0</a:int></_array><_head>0</_head><_size>1</_size>{0}<_tail>1</_tail><_version>2</_version></QueueOfint>";
        #if DESKTOP
        var expectedOnDesktop = string.Format(expectedFormat, string.Empty);
        DCS_GenericQueueHelper(expectedOnDesktop, expectedOnDesktop);
        #else
        DCS_GenericQueueHelper(string.Format(expectedFormat, @"<_syncRoot i:nil=""true"" />"), string.Format(expectedFormat, @"<_syncRoot/>"));
        #endif
    }

    private static void DCS_GenericQueueHelper(string expectedWithUninitializedSyncRoot, string expectedWithInitializedSyncRoot)
    {
        Queue<int> value = new Queue<int>();
        value.Enqueue(1);
        var deserializedValue = SerializeAndDeserialize<Queue<int>>(value, expectedWithUninitializedSyncRoot);
        var a1 = value.ToArray();
        var a2 = deserializedValue.ToArray();
        Assert.StrictEqual(a1.Length, a2.Length);
        Assert.StrictEqual(a1[0], a2[0]);        
        object syncRoot = ((ICollection)value).SyncRoot;
        deserializedValue = SerializeAndDeserialize<Queue<int>>(value, expectedWithInitializedSyncRoot);
        a1 = value.ToArray();
        a2 = deserializedValue.ToArray();
        Assert.StrictEqual(a1.Length, a2.Length);
        Assert.StrictEqual(a1[0], a2[0]);
    }

    #region System.Version tests

    [Fact]
    public static void DCS_SystemVersion()
    {
        DCS_SystemVersion_1_0();
        DCS_SystemVersion_1_0_0();
        DCS_SystemVersion_1_0_0_0();
        DCS_SystemVersion_1_0_0_1();
        DCS_SystemVersion_3_14_159_26535();
        DCS_SystemVersion_a_b_0_1();
        DCS_SystemVersion_m1_0_0_0();
        DCS_SystemVersion_m1_m1_0_0();
        DCS_SystemVersion_m1_m1_0_m1();
        DCS_SystemVersion_m1_m1_m1_0();
        DCS_SystemVersion_m1_m1_m1_m1();
    }

    public static void DCS_SystemVersion_3_14_159_26535()
    {
        Version version = new Version(3, 14, 159, 26535);
        var deserializedValue = SerializeAndDeserialize<Version>(version, "<Version xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><_Build>159</_Build><_Major>3</_Major><_Minor>14</_Minor><_Revision>26535</_Revision></Version>");
        Assert.StrictEqual<Version>(version, deserializedValue);
    }

    public static void DCS_SystemVersion_1_0()
    {
        Version version = new Version(1, 0);
        var deserializedValue = SerializeAndDeserialize<Version>(version, "<Version xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><_Build>-1</_Build><_Major>1</_Major><_Minor>0</_Minor><_Revision>-1</_Revision></Version>");
        Assert.StrictEqual<Version>(version, deserializedValue);
    }

    public static void DCS_SystemVersion_1_0_0()
    {
        Version version = new Version(1, 0, 0);
        var deserializedValue = SerializeAndDeserialize<Version>(version, "<Version xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><_Build>0</_Build><_Major>1</_Major><_Minor>0</_Minor><_Revision>-1</_Revision></Version>");
        Assert.StrictEqual<Version>(version, deserializedValue);
    }

    public static void DCS_SystemVersion_1_0_0_0()
    {
        Version version = new Version(1, 0, 0, 0);
        var deserializedValue = SerializeAndDeserialize<Version>(version, "<Version xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><_Build>0</_Build><_Major>1</_Major><_Minor>0</_Minor><_Revision>0</_Revision></Version>");
        Assert.StrictEqual<Version>(version, deserializedValue);
    }

    public static void DCS_SystemVersion_1_0_0_1()
    {
        Version version = new Version(1, 0, 0, 1);
        var deserializedValue = SerializeAndDeserialize<Version>(version, "<Version xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><_Build>0</_Build><_Major>1</_Major><_Minor>0</_Minor><_Revision>1</_Revision></Version>");
        Assert.StrictEqual<Version>(version, deserializedValue);
    }

    public static void DCS_SystemVersion_m1_0_0_0()
    {
        // Desktop doesn't complain the negative version number
        var actualVersion = DeserializeString<Version>("<Version xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><_Build>0</_Build><_Major>-1</_Major><_Minor>0</_Minor><_Revision>0</_Revision></Version>", false);
        Assert.StrictEqual(actualVersion.Build, 0);
        Assert.StrictEqual(actualVersion.Major, -1);
        Assert.StrictEqual(actualVersion.Minor, 0);
        Assert.StrictEqual(actualVersion.Revision, 0);
    }

    public static void DCS_SystemVersion_m1_m1_0_0()
    {
        // Desktop doesn't complain the negative version number
        var actualVersion = DeserializeString<Version>("<Version xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><_Build>0</_Build><_Major>-1</_Major><_Minor>-1</_Minor><_Revision>0</_Revision></Version>", false);
        Assert.StrictEqual(actualVersion.Build, 0);
        Assert.StrictEqual(actualVersion.Major, -1);
        Assert.StrictEqual(actualVersion.Minor, -1);
        Assert.StrictEqual(actualVersion.Revision, 0);
    }

    public static void DCS_SystemVersion_m1_m1_m1_0()
    {
        // Desktop doesn't complain the negative version number
        var actualVersion = DeserializeString<Version>("<Version xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><_Build>-1</_Build><_Major>-1</_Major><_Minor>-1</_Minor><_Revision>0</_Revision></Version>", false);
        Assert.StrictEqual(actualVersion.Build, -1);
        Assert.StrictEqual(actualVersion.Major, -1);
        Assert.StrictEqual(actualVersion.Minor, -1);
        Assert.StrictEqual(actualVersion.Revision, 0);
    }

    public static void DCS_SystemVersion_m1_m1_m1_m1()
    {
        // Desktop doesn't complain the negative version number
        var actualVersion = DeserializeString<Version>("<Version xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><_Build>-1</_Build><_Major>-1</_Major><_Minor>-1</_Minor><_Revision>-1</_Revision></Version>", false);
        Assert.StrictEqual(actualVersion.Build, -1);
        Assert.StrictEqual(actualVersion.Major, -1);
        Assert.StrictEqual(actualVersion.Minor, -1);
        Assert.StrictEqual(actualVersion.Revision, -1);
    }

    public static void DCS_SystemVersion_m1_m1_0_m1()
    {
        // Desktop doesn't complain the negative version number
        var actualVersion = DeserializeString<Version>("<Version xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><_Build>0</_Build><_Major>-1</_Major><_Minor>-1</_Minor><_Revision>-1</_Revision></Version>", false);
        Assert.StrictEqual(actualVersion.Build, 0);
        Assert.StrictEqual(actualVersion.Major, -1);
        Assert.StrictEqual(actualVersion.Minor, -1);
        Assert.StrictEqual(actualVersion.Revision, -1);
    }

    public static void DCS_SystemVersion_a_b_0_1()
    {
        Assert.Throws<SerializationException>(() => { DeserializeString<Version>("<Version xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/System\"><_Build>0</_Build><_Major>a</_Major><_Minor>b</_Minor><_Revision>1</_Revision></Version>", false); });
    }

    [Fact]
    public static void DCS_TypeWithCommonTypeProperties()
    {
        TypeWithCommonTypeProperties value = new TypeWithCommonTypeProperties { Ts = new TimeSpan(1, 1, 1), Id = new Guid("ad948f1e-9ba9-44c8-8e2e-b6ba969ec987") };
        var deserializedValue = SerializeAndDeserialize<TypeWithCommonTypeProperties>(value, "<TypeWithCommonTypeProperties xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Id>ad948f1e-9ba9-44c8-8e2e-b6ba969ec987</Id><Ts>PT1H1M1S</Ts></TypeWithCommonTypeProperties>");
        Assert.StrictEqual<TypeWithCommonTypeProperties>(value, deserializedValue);
    }

    [Fact]
    public static void DCS_TypeWithTypeProperty()
    {
        TypeWithTypeProperty value = new TypeWithTypeProperty { Id = 123, Name = "Jon Doe" };
        var deserializedValue = SerializeAndDeserialize<TypeWithTypeProperty>(value, "<TypeWithTypeProperty xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Id>123</Id><Name>Jon Doe</Name><Type i:nil=\"true\" xmlns:a=\"http://schemas.datacontract.org/2004/07/System\"/></TypeWithTypeProperty>");
        Assert.StrictEqual(value.Id, deserializedValue.Id);
        Assert.StrictEqual(value.Name, deserializedValue.Name);
        Assert.StrictEqual(value.Type, deserializedValue.Type);
    }

    [Fact]
    public static void DCS_TypeWithExplicitIEnumerableImplementation()
    {
        TypeWithExplicitIEnumerableImplementation value = new TypeWithExplicitIEnumerableImplementation { };
        value.Add("Foo");
        value.Add("Bar");
        var deserializedValue = SerializeAndDeserialize<TypeWithExplicitIEnumerableImplementation>(value, "<ArrayOfanyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><anyType i:type=\"a:string\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">Foo</anyType><anyType i:type=\"a:string\" xmlns:a=\"http://www.w3.org/2001/XMLSchema\">Bar</anyType></ArrayOfanyType>");
        Assert.StrictEqual(2, deserializedValue.Count);
        IEnumerator enumerator = ((IEnumerable)deserializedValue).GetEnumerator();
        enumerator.MoveNext();
        Assert.StrictEqual("Foo", (string)enumerator.Current);
        enumerator.MoveNext();
        Assert.StrictEqual("Bar", (string)enumerator.Current);
    }

    [Fact]
    public static void DCS_TypeWithGenericDictionaryAsKnownType()
    {
        TypeWithGenericDictionaryAsKnownType value = new TypeWithGenericDictionaryAsKnownType { };
        value.Foo.Add(10, new Level() { Name = "Foo", LevelNo = 1 });
        value.Foo.Add(20, new Level() { Name = "Bar", LevelNo = 2 });
        var deserializedValue = SerializeAndDeserialize<TypeWithGenericDictionaryAsKnownType>(value, "<TypeWithGenericDictionaryAsKnownType xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Foo xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:KeyValueOfintLevelQk4Xq8_SP><a:Key>10</a:Key><a:Value><LevelNo>1</LevelNo><Name>Foo</Name></a:Value></a:KeyValueOfintLevelQk4Xq8_SP><a:KeyValueOfintLevelQk4Xq8_SP><a:Key>20</a:Key><a:Value><LevelNo>2</LevelNo><Name>Bar</Name></a:Value></a:KeyValueOfintLevelQk4Xq8_SP></Foo></TypeWithGenericDictionaryAsKnownType>");

        Assert.StrictEqual(2, deserializedValue.Foo.Count);
        Assert.StrictEqual("Foo", deserializedValue.Foo[10].Name);
        Assert.StrictEqual(1, deserializedValue.Foo[10].LevelNo);
        Assert.StrictEqual("Bar", deserializedValue.Foo[20].Name);
        Assert.StrictEqual(2, deserializedValue.Foo[20].LevelNo);
    }

    [Fact]
    public static void DCS_TypeWithKnownTypeAttributeAndInterfaceMember()
    {
        TypeWithKnownTypeAttributeAndInterfaceMember value = new TypeWithKnownTypeAttributeAndInterfaceMember();
        value.HeadLine = new NewsArticle() { Title = "Foo News" };
        var deserializedValue = SerializeAndDeserialize<TypeWithKnownTypeAttributeAndInterfaceMember>(value, "<TypeWithKnownTypeAttributeAndInterfaceMember xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><HeadLine i:type=\"NewsArticle\"><Category>News</Category><Title>Foo News</Title></HeadLine></TypeWithKnownTypeAttributeAndInterfaceMember>");

        Assert.StrictEqual("News", deserializedValue.HeadLine.Category);
        Assert.StrictEqual("Foo News", deserializedValue.HeadLine.Title);
    }

    [Fact]
    public static void DCS_TypeWithKnownTypeAttributeAndListOfInterfaceMember()
    {
        TypeWithKnownTypeAttributeAndListOfInterfaceMember value = new TypeWithKnownTypeAttributeAndListOfInterfaceMember();
        value.Articles = new List<IArticle>() { new SummaryArticle() { Title = "Bar Summary" } };
        var deserializedValue = SerializeAndDeserialize<TypeWithKnownTypeAttributeAndListOfInterfaceMember>(value, "<TypeWithKnownTypeAttributeAndListOfInterfaceMember xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Articles xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><a:anyType i:type=\"SummaryArticle\"><Category>Summary</Category><Title>Bar Summary</Title></a:anyType></Articles></TypeWithKnownTypeAttributeAndListOfInterfaceMember>");

        Assert.StrictEqual(1, deserializedValue.Articles.Count);
        Assert.StrictEqual("Summary", deserializedValue.Articles[0].Category);
        Assert.StrictEqual("Bar Summary", deserializedValue.Articles[0].Title);
    }

    // NOTE: we can't test for validity of mucking with the serialization string and passing in 0.0.-1.-1 and 0.0.0.-1 as they will be serialized as 0.0 or 0.0.0 respectively. 
    // See source code for detailed explanation of why this is the case. 

    #endregion

    //
    // Begin tests of the InvalidDataContract generated for illegal types
    //

    [Fact]
    public static void DCS_InvalidDataContract_Write_And_Read_Empty_Collection_Of_Invalid_Type_Succeeds()
    {
        // Collections of invalid types can be serialized and deserialized if they are empty.
        // This is consistent with .Net
        List<Invalid_Class_No_Parameterless_Ctor> list = new List<Invalid_Class_No_Parameterless_Ctor>();
        MemoryStream ms = new MemoryStream();
        DataContractSerializer dcs = new DataContractSerializer(list.GetType());
        dcs.WriteObject(ms, list);
        ms.Seek(0L, SeekOrigin.Begin);
        List<Invalid_Class_No_Parameterless_Ctor> list2 = (List<Invalid_Class_No_Parameterless_Ctor>)dcs.ReadObject(ms);
        Assert.True(list2.Count == 0, String.Format("Unexpected length {0}", list.Count));
    }

    [Fact]
    public static void DCS_InvalidDataContract_Write_NonEmpty_Collection_Of_Invalid_Type_Throws()
    {
        // Non-empty collections of invalid types throw
        // This is consistent with .Net
        Invalid_Class_No_Parameterless_Ctor c = new Invalid_Class_No_Parameterless_Ctor("test");
        List<Invalid_Class_No_Parameterless_Ctor> list = new List<Invalid_Class_No_Parameterless_Ctor>();
        list.Add(c);
        DataContractSerializer dcs = new DataContractSerializer(list.GetType());

        MemoryStream ms = new MemoryStream();
        Assert.Throws<InvalidDataContractException>(() =>
        {
            dcs.WriteObject(ms, c);
        });
    }
    //
    // End tests of the InvalidDataContract generated for illegal types
    //

    [Fact]
    public static void DCS_DerivedException()
    {
        TestableDerivedException value = new TestableDerivedException("Testable Derived Exception Message.", new Exception("Inner exception message."));
        value.Data["testkey"] = "test value";
        value.TestProperty = "TestPropertyValue!";

        try
        {
            throw value;
        }
        catch
        {
        }

        var actual = SerializeAndDeserialize<TestableDerivedException>(value, "<TestableDerivedException xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:x=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://schemas.datacontract.org/2004/07/\"><ClassName i:type=\"x:string\" xmlns=\"\">TestableDerivedException</ClassName><Message i:type=\"x:string\" xmlns=\"\">Testable Derived Exception Message.</Message><Data i:type=\"a:ArrayOfKeyValueOfanyTypeanyType\" xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns=\"\"><a:KeyValueOfanyTypeanyType><a:Key i:type=\"x:string\">testkey</a:Key><a:Value i:type=\"x:string\">test value</a:Value></a:KeyValueOfanyTypeanyType></Data><InnerException i:type=\"a:Exception\" xmlns:a=\"http://schemas.datacontract.org/2004/07/System\" xmlns=\"\"><ClassName i:type=\"x:string\">System.Exception</ClassName><Message i:type=\"x:string\">Inner exception message.</Message><Data i:nil=\"true\" /><InnerException i:nil=\"true\" /><HelpURL i:nil=\"true\" /><StackTraceString i:nil=\"true\" /><RemoteStackTraceString i:nil=\"true\" /><RemoteStackIndex i:type=\"x:int\">0</RemoteStackIndex><ExceptionMethod i:nil=\"true\" /><HResult i:type=\"x:int\">-2146233088</HResult><Source i:nil=\"true\" /><WatsonBuckets i:nil=\"true\" /></InnerException><HelpURL i:nil=\"true\" xmlns=\"\" /><StackTraceString i:type=\"x:string\" xmlns=\"\">" + value.StackTrace + "</StackTraceString><RemoteStackTraceString i:nil=\"true\" xmlns=\"\" /><RemoteStackIndex i:type=\"x:int\" xmlns=\"\">0</RemoteStackIndex><ExceptionMethod i:nil=\"true\" xmlns=\"\" /><HResult i:type=\"x:int\" xmlns=\"\">-2146233088</HResult><Source i:nil=\"true\" xmlns=\"\" /><WatsonBuckets i:nil=\"true\" xmlns=\"\" /><TestProperty i:type=\"x:string\" xmlns=\"\">TestPropertyValue!</TestProperty></TestableDerivedException>");

        Assert.StrictEqual(value.Message, actual.Message);
        Assert.NotNull(actual.InnerException);
        Assert.StrictEqual(value.InnerException.Message, actual.InnerException.Message);
        Assert.NotNull(actual.Data);
        foreach (string key in value.Data.Keys)
        {
            Assert.StrictEqual((String)value.Data[key], (String)actual.Data[key]);
        }
        Assert.StrictEqual(value.HResult, actual.HResult);
        Assert.StrictEqual(value.StackTrace, actual.StackTrace);
        Assert.StrictEqual(value.TestProperty, actual.TestProperty);
    }

    [Fact]
    public static void DCS_DeserializeEmptyString()
    {
        var serializer = new DataContractSerializer(typeof (object));
        Assert.Throws<XmlException>(() =>
        {
            serializer.ReadObject(new MemoryStream());
        });
    }

    private static T SerializeAndDeserialize<T>(T value, string baseline, DataContractSerializerSettings settings = null, Func<DataContractSerializer> serializerFactory = null)
    {
        DataContractSerializer dcs;
        if (serializerFactory != null)
        {
            dcs = serializerFactory();
        }
        else
        {
            dcs = (settings != null) ? new DataContractSerializer(typeof(T), settings) : new DataContractSerializer(typeof(T));
        }

        using (MemoryStream ms = new MemoryStream())
        {
            dcs.WriteObject(ms, value);
            ms.Position = 0;

            string actualOutput = new StreamReader(ms).ReadToEnd();
            Utils.CompareResult result = Utils.Compare(baseline, actualOutput);

            Assert.True(result.Equal, string.Format("{1}{0}Test failed for input: {2}{0}Expected: {3}{0}Actual: {4}",
                Environment.NewLine, result.ErrorMessage, value, baseline, actualOutput));

            ms.Position = 0;
            T deserialized;

            deserialized = (T)dcs.ReadObject(ms);

            return deserialized;
        }
    }

    private static T DeserializeString<T>(string stringToDeserialize, bool shouldReportDeserializationExceptions = true, DataContractSerializerSettings settings = null, Func<DataContractSerializer> serializerFactory = null)
    {
        DataContractSerializer dcs;
        if (serializerFactory != null)
        {
            dcs = serializerFactory();
        }
        else
        {
            dcs = (settings != null) ? new DataContractSerializer(typeof(T), settings) : new DataContractSerializer(typeof(T));
        }

        Console.WriteLine("Testing string to deserialize: {0}", stringToDeserialize);

        byte[] bytesToDeserialize = Encoding.UTF8.GetBytes(stringToDeserialize);
        using (MemoryStream ms = new MemoryStream(bytesToDeserialize))
        {
            ms.Position = 0;

            T deserialized;

            try
            {
                deserialized = (T)dcs.ReadObject(ms);
            }
            catch
            {
                if (shouldReportDeserializationExceptions)
                {
                    Console.WriteLine("Error deserializing value - could not create type {0}", typeof(T).ToString());
                }
                throw;
            }

            return deserialized;
        }
    }

    private static string s_errorMsg = "The field/property {0} value of deserialized object is wrong";
    private static string getCheckFailureMsg(string propertyName)
    {
        return string.Format(s_errorMsg, propertyName);
    }
}
