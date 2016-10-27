// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SerializationTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Xunit;

public static partial class DataContractJsonSerializerTests
{
#if ReflectionOnly
    private static readonly string SerializationOptionSetterName = "set_Option";

    static DataContractJsonSerializerTests()
    {
        var method = typeof(DataContractSerializer).GetMethod(SerializationOptionSetterName);
        Assert.True(method != null, $"No method named {SerializationOptionSetterName}");
        method.Invoke(null, new object[] { 1 });
    }
#endif
    [Fact]
    public static void DCJS_BoolAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<bool>(true, "true"), true);
        Assert.StrictEqual(SerializeAndDeserialize<bool>(false, "false"), false);
    }

    [Fact]
    public static void DCJS_ByteArrayAsRoot()
    {
        Assert.Null(SerializeAndDeserialize<byte[]>(null, "null"));
        byte[] x = new byte[] { 1, 2 };
        byte[] y = SerializeAndDeserialize<byte[]>(x, @"[1,2]");
        Assert.Equal(x, y);
    }

    [Fact]
    public static void DCJS_CharAsRoot()
    {
        // Special characters
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0x2f, @"""\/"""), (char)0x2f); // Expected output string is: \/
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0x5c, @"""\\"""), (char)0x5c); // \\
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0x27, @"""'"""), (char)0x27); // '
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0x22, @"""\"""""), (char)0x22); // \"

        // There are 5 ranges of characters that have output in the form of "\u<code>".
        // The following tests the start and end character and at least one character in each range
        // and also in between the ranges.

        // #1. 0x0000 - 0x001F
        Assert.StrictEqual(SerializeAndDeserialize<char>(char.MinValue, @"""\u0000"""), char.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0x10, @"""\u0010"""), (char)0x10);
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0x1f, @"""\u001f"""), (char)0x1f);

        // Between #1 and #2
        Assert.StrictEqual(SerializeAndDeserialize<char>('a', @"""a"""), 'a'); // 0x0061

        // #2. 0x0085
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0x85, @"""\u0085"""), (char)0x85);

        // Between #2 and #3
        Assert.StrictEqual(SerializeAndDeserialize<char>('ñ', @"""ñ"""), 'ñ'); // 0x00F1

        // #3. 0x2028 - 0x2029
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0x2028, @"""\u2028"""), (char)0x2028);
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0x2029, @"""\u2029"""), (char)0x2029);

        // Between #3 and #4
        Assert.StrictEqual(SerializeAndDeserialize<char>('?', @"""?"""), '?'); // 0x6F22

        // #4. 0xD800 - 0xDFFF
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0xd800, @"""\ud800"""), (char)0xd800);
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0xdabc, @"""\udabc"""), (char)0xdabc);
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0xdfff, @"""\udfff"""), (char)0xdfff);

        // Between #4 and #5
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0xeabc, @""""""), (char)0xeabc);

        // #5. 0xFFFE - 0xFFFF
        Assert.StrictEqual(SerializeAndDeserialize<char>((char)0xfffe, @"""\ufffe"""), (char)0xfffe);
        Assert.StrictEqual(SerializeAndDeserialize<char>(char.MaxValue, @"""\uffff"""), char.MaxValue);
    }

    [Fact]
    public static void DCJS_NewLineChars()
    {
        char ch1 = (char)0x0085;
        char ch2 = (char)0x2028;
        char ch3 = (char)0x2029;
        Assert.StrictEqual<char>(SerializeAndDeserialize<char>(ch1, @"""\u0085"""), ch1);
        Assert.StrictEqual<char>(SerializeAndDeserialize<char>(ch2, @"""\u2028"""), ch2);
        Assert.StrictEqual<char>(SerializeAndDeserialize<char>(ch3, @"""\u2029"""), ch3);
    }

    [Fact]
    public static void DCJS_ByteAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<byte>(10, "10"), 10);
        Assert.StrictEqual(SerializeAndDeserialize<byte>(byte.MinValue, "0"), byte.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<byte>(byte.MaxValue, "255"), byte.MaxValue);
    }

    [Fact]
    public static void DCJS_DateTimeAsRoot()
    {
        // Assume that UTC offset doesn't change more often than once in the day 2013-01-02
        // DO NOT USE TimeZoneInfo.Local.BaseUtcOffset !
        var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(new DateTime(2013, 1, 2)).TotalMinutes;
        var timeZoneString = string.Format("{0:+;-}{1}", offsetMinutes, new TimeSpan(0, offsetMinutes, 0).ToString(@"hhmm"));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2).AddMinutes(offsetMinutes), string.Format(@"""\/Date(1357084800000{0})\/""", timeZoneString)), new DateTime(2013, 1, 2).AddMinutes(offsetMinutes));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local).AddMinutes(offsetMinutes), string.Format(@"""\/Date(1357095845006{0})\/""", timeZoneString)), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local).AddMinutes(offsetMinutes));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified).AddMinutes(offsetMinutes), string.Format(@"""\/Date(1357095845006{0})\/""", timeZoneString)), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified).AddMinutes(offsetMinutes));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc), @"""\/Date(1357095845006)\/"""), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), @"""\/Date(-62135596800000)\/"""), DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
        SerializeAndDeserialize<DateTime>(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), @"""\/Date(253402300799999)\/""");
    }

    [Fact]
    public static void DCJS_DecimalAsRoot()
    {
        foreach (decimal value in new decimal[] { (decimal)-1.2, (decimal)0, (decimal)2.3, decimal.MinValue, decimal.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<decimal>(value, value.ToString(CultureInfo.InvariantCulture)), value);
        }
    }

    [Fact]
    public static void DCJS_DoubleAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<double>(-1.2, "-1.2"), -1.2);
        Assert.StrictEqual(SerializeAndDeserialize<double>(0, "0"), 0);
        Assert.StrictEqual(SerializeAndDeserialize<double>(2.3, "2.3"), 2.3);
        Assert.StrictEqual(SerializeAndDeserialize<double>(double.MinValue, "-1.7976931348623157E+308"), double.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<double>(double.MaxValue, "1.7976931348623157E+308"), double.MaxValue);
    }

    [Fact]
    public static void DCJS_FloatAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)-1.2, "-1.2"), (float)-1.2);
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)0, "0"), (float)0);
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)2.3, "2.3"), (float)2.3);
        Assert.StrictEqual(SerializeAndDeserialize<float>(float.MinValue, "-3.40282347E+38"), float.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<float>(float.MaxValue, "3.40282347E+38"), float.MaxValue);
    }

    [Fact]
    public static void DCJS_GuidAsRoot()
    {
        foreach (Guid value in new Guid[] { Guid.NewGuid(), Guid.Empty })
        {
            Assert.StrictEqual(SerializeAndDeserialize<Guid>(value, string.Format(@"""{0}""", value.ToString())), value);
        }
    }

    [Fact]
    public static void DCJS_IntAsRoot()
    {
        foreach (int value in new int[] { -1, 0, 2, int.MinValue, int.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<int>(value, value.ToString()), value);
        }
    }

    [Fact]
    public static void DCJS_LongAsRoot()
    {
        foreach (long value in new long[] { (long)-1, (long)0, (long)2, long.MinValue, long.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<long>(value, value.ToString()), value);
        }
    }

    [Fact]
    public static void DCJS_ObjectAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<object>(1, "1"), 1);
        Assert.StrictEqual(SerializeAndDeserialize<object>(true, "true"), true);
        Assert.StrictEqual(SerializeAndDeserialize<object>(null, "null"), null);
        Assert.StrictEqual(SerializeAndDeserialize<object>("abc", @"""abc"""), "abc");
    }

    [Fact]
    public static void DCJS_XmlQualifiedNameAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<XmlQualifiedName>(new XmlQualifiedName("abc", "def"), @"""abc:def"""), new XmlQualifiedName("abc", "def"));
        Assert.StrictEqual(SerializeAndDeserialize<XmlQualifiedName>(XmlQualifiedName.Empty, @""""""), XmlQualifiedName.Empty);
    }

    [Fact]
    public static void DCJS_ShortAsRoot()
    {
        foreach (short value in new short[] { (short)-1.2, (short)0, (short)2.3, short.MinValue, short.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<short>(value, value.ToString()), value);
        }
    }

    [Fact]
    public static void DCJS_SbyteAsRoot()
    {
        foreach (sbyte value in new sbyte[] { (sbyte)3, (sbyte)0, sbyte.MinValue, sbyte.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<sbyte>(value, value.ToString()), value);
        }
    }

    [Fact]
    public static void DCJS_StringAsRoot()
    {
        foreach (string value in new string[] { "abc", "  a b  ", null, "", " ", "Hello World! 漢 ñ" })
        {
            Assert.StrictEqual(SerializeAndDeserialize<string>(value, value == null ? "null" : string.Format(@"""{0}""", value.ToString())), value);
        }

        var testStrings = new[]
        {
            new { value = "\u0008", baseline = "\\b" }, // BACKSPACE
            new { value = "\u000C", baseline = "\\f" }, // FORM FEED (FF)
            new { value = "\u000A", baseline = "\\n" }, // LINE FEED (LF)
            new { value = "\u000D", baseline = "\\r" }, // CARRIAGE RETURN (CR)
            new { value = "\u0009", baseline = "\\t" }, // HORIZONTAL TABULATION
            new { value = "\u0022", baseline = "\\\"" }, // QUOTATION MARK
            new { value = "\u005C", baseline = "\\\\" }, // REVERSE SOLIDUS
            new { value = "\u0000", baseline = "\\u0000" }, // NULL
            new { value = "\u000B", baseline = "\\u000b" }, // LINE TABULATION
            new { value = "\u000F", baseline = "\\u000f" }, // SHIFT IN
            new { value = "\u0027", baseline = "'" },
        };

        foreach (var pair in testStrings)
        {
            Assert.StrictEqual(SerializeAndDeserialize<string>(pair.value, string.Format(@"""{0}""", pair.baseline)), pair.value);
        }
    }

    [Fact]
    public static void DCJS_StringAsRoot_BackwardCompatibility()
    {
        var testStrings = new[]
        {
            new { value = "\u0008", text = @"""\u0008""" }, // BACKSPACE
            new { value = "\u000C", text = @"""\u000c""" }, // FORM FEED (FF)
            new { value = "\u000A", text = @"""\u000a""" }, // LINE FEED (LF)
            new { value = "\u000D", text = @"""\u000d""" }, // CARRIAGE RETURN (CR)
            new { value = "\u0009", text = @"""\u0009""" }, // HORIZONTAL TABULATION
        };

        var serializer = new DataContractJsonSerializer(typeof(string));
        foreach (var pair in testStrings)
        {
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms))
                {
                    {
                        sw.Write(pair.text);
                        sw.Flush();
                        ms.Position = 0;
                        string actual = (string)serializer.ReadObject(ms);
                        Assert.StrictEqual(pair.value, actual);
                    }
                }
            }
        }
    }

    [Fact]
    public static void DCJS_TimeSpanAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<TimeSpan>(new TimeSpan(1, 2, 3), @"""PT1H2M3S"""), new TimeSpan(1, 2, 3));
        Assert.StrictEqual(SerializeAndDeserialize<TimeSpan>(TimeSpan.Zero, @"""PT0S"""), TimeSpan.Zero);
        Assert.StrictEqual(SerializeAndDeserialize<TimeSpan>(TimeSpan.MinValue, @"""-P10675199DT2H48M5.4775808S"""), TimeSpan.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<TimeSpan>(TimeSpan.MaxValue, @"""P10675199DT2H48M5.4775807S"""), TimeSpan.MaxValue);
    }

    [Fact]
    public static void DCJS_UintAsRoot()
    {
        foreach (uint value in new uint[] { (uint)3, (uint)0, uint.MinValue, uint.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<uint>(value, value.ToString()), value);
        }
    }

    [Fact]
    public static void DCJS_UlongAsRoot()
    {
        foreach (ulong value in new ulong[] { (ulong)3, (ulong)0, ulong.MinValue, ulong.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<ulong>(value, value.ToString()), value);
        }
    }

    [Fact]
    public static void DCJS_UshortAsRoot()
    {
        foreach (ushort value in new ushort[] { (ushort)3, (ushort)0, ushort.MinValue, ushort.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<ushort>(value, value.ToString()), value);
        }
    }

    [Fact]
    public static void DCJS_UriAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<Uri>(new Uri(@"http://abc/"), @"""http:\/\/abc\/"""), new Uri(@"http://abc/"));
        Assert.StrictEqual(SerializeAndDeserialize<Uri>(new Uri(@"http://abc/def/x.aspx?p1=12&p2=34"), @"""http:\/\/abc\/def\/x.aspx?p1=12&p2=34"""), new Uri(@"http://abc/def/x.aspx?p1=12&p2=34"));
    }

    [Fact]
    public static void DCJS_ArrayAsRoot()
    {
        SimpleType[] x = new SimpleType[] { new SimpleType { P1 = "abc", P2 = 11 }, new SimpleType { P1 = "def", P2 = 12 } };
        SimpleType[] y = SerializeAndDeserialize<SimpleType[]>(x, @"[{""P1"":""abc"",""P2"":11},{""P1"":""def"",""P2"":12}]");

        Utils.Equal(x, y, (a, b) => { return SimpleType.AreEqual(a, b); });
    }

    [Fact]
    public static void DCJS_ArrayAsGetSet()
    {
        TypeWithGetSetArrayMembers x = new TypeWithGetSetArrayMembers
        {
            F1 = new SimpleType[] { new SimpleType { P1 = "ab", P2 = 1 }, new SimpleType { P1 = "cd", P2 = 2 } },
            F2 = new int[] { -1, 3 },
            P1 = new SimpleType[] { new SimpleType { P1 = "ef", P2 = 5 }, new SimpleType { P1 = "gh", P2 = 7 } },
            P2 = new int[] { 11, 12 }
        };
        TypeWithGetSetArrayMembers y = SerializeAndDeserialize<TypeWithGetSetArrayMembers>(x, @"{""F1"":[{""P1"":""ab"",""P2"":1},{""P1"":""cd"",""P2"":2}],""F2"":[-1,3],""P1"":[{""P1"":""ef"",""P2"":5},{""P1"":""gh"",""P2"":7}],""P2"":[11,12]}");

        Assert.NotNull(y);
        Utils.Equal(x.F1, y.F1, (a, b) => { return SimpleType.AreEqual(a, b); });
        Assert.Equal(x.F2, y.F2);
        Utils.Equal(x.P1, y.P1, (a, b) => { return SimpleType.AreEqual(a, b); });
        Assert.Equal(x.P2, y.P2);
    }

    [Fact]
    public static void DCJS_ArrayAsGetOnly()
    {
        TypeWithGetOnlyArrayProperties x = new TypeWithGetOnlyArrayProperties();
        x.P1[0] = new SimpleType { P1 = "ab", P2 = 1 };
        x.P1[1] = new SimpleType { P1 = "cd", P2 = 2 };
        x.P2[0] = -1;
        x.P2[1] = 3;

        TypeWithGetOnlyArrayProperties y = SerializeAndDeserialize<TypeWithGetOnlyArrayProperties>(x, @"{""P1"":[{""P1"":""ab"",""P2"":1},{""P1"":""cd"",""P2"":2}],""P2"":[-1,3]}");

        Assert.NotNull(y);
        Utils.Equal(x.P1, y.P1, (a, b) => { return SimpleType.AreEqual(a, b); });
        Assert.Equal(x.P2, y.P2);
    }

    [Fact]
    public static void DCJS_DictionaryGenericRoot()
    {
        Dictionary<string, int> x = new Dictionary<string, int>();
        x.Add("one", 1);
        x.Add("two", 2);

        Dictionary<string, int> y = SerializeAndDeserialize<Dictionary<string, int>>(x, @"[{""Key"":""one"",""Value"":1},{""Key"":""two"",""Value"":2}]");

        Assert.NotNull(y);
        Assert.Equal(y.Count, 2);
        Assert.Equal(y["one"], 1);
        Assert.Equal(y["two"], 2);
    }

    [Fact]
    public static void DCJS_DictionaryGenericMembers()
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

        TypeWithDictionaryGenericMembers y = SerializeAndDeserialize<TypeWithDictionaryGenericMembers>(x, @"{""F1"":[{""Key"":""ab"",""Value"":12},{""Key"":""cd"",""Value"":15}],""F2"":[{""Key"":""ef"",""Value"":17},{""Key"":""gh"",""Value"":19}],""P1"":[{""Key"":""12"",""Value"":120},{""Key"":""13"",""Value"":130}],""P2"":[{""Key"":""14"",""Value"":140},{""Key"":""15"",""Value"":150}],""RO1"":[{""Key"":true,""Value"":""t""},{""Key"":false,""Value"":""f""}],""RO2"":[{""Key"":true,""Value"":""a""},{""Key"":false,""Value"":""b""}]}");
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
    public static void DCJS_GetOonlyDictionary_UseSimpleDictionaryFormat()
    {
        var x = new TypeWithDictionaryGenericMembers();

        x.RO1.Add(true, 't');
        x.RO1.Add(false, 'f');

        x.RO2.Add(true, 'a');
        x.RO2.Add(false, 'b');

        var settings = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
        string baseline = "{\"F1\":null,\"F2\":null,\"P1\":null,\"P2\":null,\"RO1\":{\"True\":\"t\",\"False\":\"f\"},\"RO2\":{\"True\":\"a\",\"False\":\"b\"}}";
        var y = SerializeAndDeserialize(x, baseline, settings);
        Assert.NotNull(y);

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
    public static void DCJS_DictionaryRoot()
    {
        MyDictionary x = new MyDictionary();
        x.Add(1, "one");
        x.Add(2, "two");

        MyDictionary y = SerializeAndDeserialize<MyDictionary>(x, @"[{""Key"":1,""Value"":""one""},{""Key"":2,""Value"":""two""}]");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((string)y[1] == "one");
        Assert.True((string)y[2] == "two");
    }

    [Fact]
    public static void DCJS_DictionaryMembers()
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

        TypeWithDictionaryMembers y = SerializeAndDeserialize<TypeWithDictionaryMembers>(x, @"{""F1"":[{""Key"":""ab"",""Value"":12},{""Key"":""cd"",""Value"":15}],""F2"":[{""Key"":""ef"",""Value"":17},{""Key"":""gh"",""Value"":19}],""P1"":[{""Key"":""12"",""Value"":120},{""Key"":""13"",""Value"":130}],""P2"":[{""Key"":""14"",""Value"":140},{""Key"":""15"",""Value"":150}],""RO1"":[{""Key"":true,""Value"":""t""},{""Key"":false,""Value"":""f""}],""RO2"":[{""Key"":true,""Value"":""a""},{""Key"":false,""Value"":""b""}]}");
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
        Assert.True((string)y.RO1[true] == "t");
        Assert.True((string)y.RO1[false] == "f");

        Assert.NotNull(y.RO2);
        Assert.True(y.RO2.Count == 2);
        Assert.True((string)y.RO2[true] == "a");
        Assert.True((string)y.RO2[false] == "b");
    }

    [Fact]
    public static void DCJS_ListGenericRoot()
    {
        List<string> x = new List<string>();
        x.Add("zero");
        x.Add("one");

        List<string> y = SerializeAndDeserialize<List<string>>(x, @"[""zero"",""one""]");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True(y[0] == "zero");
        Assert.True(y[1] == "one");
    }

    [Fact]
    public static void DCJS_ListGenericMembers()
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

        TypeWithListGenericMembers y = SerializeAndDeserialize<TypeWithListGenericMembers>(x, @"{""F1"":[""zero"",""one""],""F2"":[""abc"",""def""],""P1"":[10,20],""P2"":[12,34],""RO1"":[""a"",""b""],""RO2"":[""c"",""d""]}");
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
    public static void DCJS_CollectionGenericRoot()
    {
        MyCollection<string> x = new MyCollection<string>("a1", "a2");
        MyCollection<string> y = SerializeAndDeserialize<MyCollection<string>>(x, @"[""a1"",""a2""]");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);

        foreach (var item in x)
        {
            Assert.True(y.Contains(item));
        }
    }

    [Fact]
    public static void DCJS_CollectionGenericMembers()
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

        TypeWithCollectionGenericMembers y = SerializeAndDeserialize<TypeWithCollectionGenericMembers>(x, @"{""F1"":[""a1"",""a2""],""F2"":[""b1"",""b2""],""P1"":[""c1"",""c2""],""P2"":[""d1"",""d2""],""RO1"":[""abc""],""RO2"":[""xyz""]}");
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
    public static void DCJS_ListRoot()
    {
        MyList x = new MyList("a1", "a2");
        MyList y = SerializeAndDeserialize<MyList>(x, @"[""a1"",""a2""]");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        foreach (var item in x)
        {
            Assert.True(y.Contains(item));
        }
    }

    [Fact]
    public static void DCJS_ListMembers()
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

        TypeWithListMembers y = SerializeAndDeserialize<TypeWithListMembers>(x, @"{""F1"":[""a1"",""a2""],""F2"":[""b1"",""b2""],""P1"":[""c1"",""c2""],""P2"":[""d1"",""d2""],""RO1"":[""abc""],""RO2"":[""xyz""]}");
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
    public static void DCJS_EnumerableGenericRoot()
    {
        MyEnumerable<string> x = new MyEnumerable<string>("a1", "a2");
        MyEnumerable<string> y = SerializeAndDeserialize<MyEnumerable<string>>(x, @"[""a1"",""a2""]");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);

        string actual = string.Join("", y);
        Assert.StrictEqual(actual, "a1a2");
    }

    [Fact]
    public static void DCJS_EnumerableGenericMembers()
    {
        TypeWithEnumerableGenericMembers x = new TypeWithEnumerableGenericMembers
        {
            F1 = new MyEnumerable<string>("a1", "a2"),
            F2 = new MyEnumerable<string>("b1", "b2"),
            P1 = new MyEnumerable<string>("c1", "c2"),
            P2 = new MyEnumerable<string>("d1", "d2")
        };
        x.RO1.Add("abc");

        TypeWithEnumerableGenericMembers y = SerializeAndDeserialize<TypeWithEnumerableGenericMembers>(x, @"{""F1"":[""a1"",""a2""],""F2"":[""b1"",""b2""],""P1"":[""c1"",""c2""],""P2"":[""d1"",""d2""],""RO1"":[""abc""]}");

        Assert.NotNull(y);
        Assert.True(y.F1.Count == 2);
        Assert.True(((string[])y.F2).Length == 2);
        Assert.True(y.P1.Count == 2);
        Assert.True(((string[])y.P2).Length == 2);
        Assert.True(y.RO1.Count == 1);
    }

    [Fact]
    public static void DCJS_CollectionRoot()
    {
        MyCollection x = new MyCollection('a', 45);
        MyCollection y = SerializeAndDeserialize<MyCollection>(x, @"[""a"",45]");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((string)y[0] == "a");
        Assert.True((int)y[1] == 45);
    }

    [Fact]
    public static void DCJS_CollectionMembers()
    {
        TypeWithCollectionMembers x = new TypeWithCollectionMembers
        {
            F1 = new MyCollection('a', 45),
            F2 = new MyCollection("ab", true),
            P1 = new MyCollection("x", "y"),
            P2 = new MyCollection(false, true)
        };
        x.RO1.Add("abc");

        TypeWithCollectionMembers y = SerializeAndDeserialize<TypeWithCollectionMembers>(x, @"{""F1"":[""a"",45],""F2"":[""ab"",true],""P1"":[""x"",""y""],""P2"":[false,true],""RO1"":[""abc""]}");
        Assert.NotNull(y);

        Assert.NotNull(y.F1);
        Assert.True(y.F1.Count == 2);
        Assert.True((string)y.F1[0] == "a");
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
    public static void DCJS_EnumerableRoot()
    {
        MyEnumerable x = new MyEnumerable("abc", 3);
        MyEnumerable y = SerializeAndDeserialize<MyEnumerable>(x, @"[""abc"",3]");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((string)y[0] == "abc");
        Assert.True((int)y[1] == 3);
    }

    [Fact]
    public static void DCJS_EnumerableMembers()
    {
        TypeWithEnumerableMembers x = new TypeWithEnumerableMembers
        {
            F1 = new MyEnumerable('a', 45),
            F2 = new MyEnumerable("ab", true),
            P1 = new MyEnumerable("x", "y"),
            P2 = new MyEnumerable(false, true)
        };
        x.RO1.Add('x');

        TypeWithEnumerableMembers y = SerializeAndDeserialize<TypeWithEnumerableMembers>(x, @"{""F1"":[""a"",45],""F2"":[""ab"",true],""P1"":[""x"",""y""],""P2"":[false,true],""RO1"":[""x""]}");
        Assert.NotNull(y);

        Assert.True(y.F1.Count == 2);
        Assert.True((string)y.F1[0] == "a");
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
        Assert.True((string)y.RO1[0] == "x");
    }

    [Fact]
    public static void DCJS_CustomType()
    {
        MyTypeA x = new MyTypeA
        {
            PropX = new MyTypeC { PropC = 'a', PropB = true },
            PropY = 45,
        };
        MyTypeA y = SerializeAndDeserialize<MyTypeA>(x, @"{""P_Col_Array"":null,""PropX"":{""__type"":""MyTypeC:#SerializationTypes"",""PropA"":null,""PropC"":""a"",""PropB"":true},""PropY"":45}");

        Assert.NotNull(y);
        Assert.NotNull(y.PropX);
        Assert.StrictEqual(x.PropX.PropC, y.PropX.PropC);
        Assert.StrictEqual(((MyTypeC)x.PropX).PropB, ((MyTypeC)y.PropX).PropB);
        Assert.StrictEqual(x.PropY, y.PropY);
    }

    [Fact]
    public static void DCJS_DataContractAttribute()
    {
        SerializeAndDeserialize<DCA_1>(new DCA_1 { P1 = "xyz" }, @"{}");
        SerializeAndDeserialize<DCA_2>(new DCA_2 { P1 = "xyz" }, @"{}");
        SerializeAndDeserialize<DCA_3>(new DCA_3 { P1 = "xyz" }, @"{}");
        SerializeAndDeserialize<DCA_5>(new DCA_5 { P1 = "xyz" }, @"{}");
    }

    [Fact]
    public static void DCJS_DataMemberAttribute()
    {
        SerializeAndDeserialize<DMA_1>(new DMA_1 { P1 = "abc", P2 = 12, P3 = true, P4 = 'a', P5 = 10, MyDataMemberInAnotherNamespace = new MyDataContractClass04_1() { MyDataMember = "Test" }, Order100 = true, OrderMaxValue = false }, @"{""MyDataMemberInAnotherNamespace"":{""MyDataMember"":""Test""},""P1"":""abc"",""P4"":""a"",""P5"":10,""xyz"":12,""P3"":true,""Order100"":true,""OrderMaxValue"":false}");
    }

    [Fact]
    public static void DCJS_IgnoreDataMemberAttribute()
    {
        IDMA_1 x = new IDMA_1 { MyDataMember = "MyDataMember", MyIgnoreDataMember = "MyIgnoreDataMember", MyUnsetDataMember = "MyUnsetDataMember" };
        IDMA_1 y = SerializeAndDeserialize<IDMA_1>(x, @"{""MyDataMember"":""MyDataMember""}");
        Assert.NotNull(y);
        Assert.StrictEqual(x.MyDataMember, y.MyDataMember);
        Assert.Null(y.MyIgnoreDataMember);
        Assert.Null(y.MyUnsetDataMember);
    }

    [Fact]
    public static void DCJS_EnumAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<MyEnum>(MyEnum.Two, "1"), MyEnum.Two);
        Assert.StrictEqual(SerializeAndDeserialize<ByteEnum>(ByteEnum.Option1, "1"), ByteEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<SByteEnum>(SByteEnum.Option1, "1"), SByteEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<ShortEnum>(ShortEnum.Option1, "1"), ShortEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<IntEnum>(IntEnum.Option1, "1"), IntEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<UIntEnum>(UIntEnum.Option1, "1"), UIntEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<LongEnum>(LongEnum.Option1, "1"), LongEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<ULongEnum>(ULongEnum.Option1, "1"), ULongEnum.Option1);
    }

    [Fact]
    public static void DCJS_EnumAsMember()
    {
        TypeWithEnumMembers x = new TypeWithEnumMembers { F1 = MyEnum.Three, P1 = MyEnum.Two };
        TypeWithEnumMembers y = SerializeAndDeserialize<TypeWithEnumMembers>(x, @"{""F1"":2,""P1"":1}");

        Assert.NotNull(y);
        Assert.StrictEqual(x.F1, y.F1);
        Assert.StrictEqual(x.P1, y.P1);
    }

    [Fact]
    public static void DCJS_DCClassWithEnumAndStruct()
    {
        var x = new DCClassWithEnumAndStruct(true);
        var y = SerializeAndDeserialize<DCClassWithEnumAndStruct>(x, @"{""MyEnum1"":0,""MyStruct"":{""Data"":""Data""}}");

        Assert.StrictEqual<DCStruct>(x.MyStruct, y.MyStruct);
        Assert.StrictEqual(x.MyEnum1, y.MyEnum1);
    }

    [Fact]
    public static void DCJS_SuspensionManager()
    {
        var dict2 = new Dictionary<string, object> { { "Key2-0", "Value2-0" } };
        var dict1 = new Dictionary<string, object> { { "Key1-0", "Value1-0" }, { "Key1-1", dict2 } };
        var dict0 = new Dictionary<string, object> { { "Key0", dict1 } };

        var y = SerializeAndDeserialize<Dictionary<string, object>>(dict0, @"[{""Key"":""Key0"",""Value"":[{""__type"":""KeyValuePairOfstringanyType:#System.Collections.Generic"",""key"":""Key1-0"",""value"":""Value1-0""},{""__type"":""KeyValuePairOfstringanyType:#System.Collections.Generic"",""key"":""Key1-1"",""value"":[{""__type"":""KeyValuePairOfstringanyType:#System.Collections.Generic"",""key"":""Key2-0"",""value"":""Value2-0""}]}]}]"
#if NET_NATIVE
            , null, () => { return new DataContractJsonSerializer(typeof(Dictionary<string, object>), new Type[] { typeof(KeyValuePair<string, object>) }); }
#endif
        );
        Assert.NotNull(y);
        Assert.StrictEqual(y.Count, 1);
        Assert.True(y["Key0"] is object[]);
        Assert.StrictEqual(((KeyValuePair<string, object>)((object[])y["Key0"])[0]).Key, "Key1-0");
        Assert.StrictEqual(((KeyValuePair<string, object>)((object[])y["Key0"])[0]).Value, "Value1-0");
        Assert.True(((KeyValuePair<string, object>)((object[])y["Key0"])[1]).Value is object[]);
        Assert.StrictEqual(((KeyValuePair<string, object>)((object[])((KeyValuePair<string, object>)((object[])y["Key0"])[1]).Value)[0]).Value, "Value2-0");
    }

    [Fact]
    public static void DCJS_KeyValuePairOfStringObject()
    {
        var x = new KeyValuePair<string, object>("key1", "key1value");

        var y = SerializeAndDeserialize<KeyValuePair<string, object>>(x, @"{""key"":""key1"",""value"":""key1value""}");
        Assert.NotNull(y);
        Assert.StrictEqual(y.Key, "key1");
        Assert.StrictEqual(y.Value, "key1value");
    }

    [Fact]
    public static void DCJS_BuiltInTypes()
    {
        BuiltInTypes x = new BuiltInTypes
        {
            ByteArray = new byte[] { 1, 2 }
        };
        BuiltInTypes y = SerializeAndDeserialize<BuiltInTypes>(x, @"{""ByteArray"":[1,2]}");

        Assert.NotNull(y);
        Assert.Equal<byte>(x.ByteArray, y.ByteArray);
    }

    [Fact]
    public static void DCJS_DictionaryWithVariousKeyValueTypes()
    {
        var x = new DictionaryWithVariousKeyValueTypes(true);
        var y = SerializeAndDeserialize<DictionaryWithVariousKeyValueTypes>(x, @"{""WithEnums"":[{""Key"":1,""Value"":2},{""Key"":0,""Value"":0}],""WithNullables"":[{""Key"":-32768,""Value"":true},{""Key"":0,""Value"":false},{""Key"":32767,""Value"":null}],""WithStructs"":[{""Key"":{""value"":10},""Value"":{""value"":12}},{""Key"":{""value"":2147483647},""Value"":{""value"":-2147483648}}]}");

        Assert.StrictEqual(y.WithEnums[MyEnum.Two], MyEnum.Three);
        Assert.StrictEqual(y.WithEnums[MyEnum.One], MyEnum.One);
        Assert.StrictEqual<StructNotSerializable>(y.WithStructs[new StructNotSerializable() { value = 10 }], new StructNotSerializable() { value = 12 });
        Assert.StrictEqual<StructNotSerializable>(y.WithStructs[new StructNotSerializable() { value = int.MaxValue }], new StructNotSerializable() { value = int.MinValue });
        Assert.StrictEqual(y.WithNullables[Int16.MinValue], true);
        Assert.StrictEqual(y.WithNullables[0], false);
        Assert.StrictEqual(y.WithNullables[Int16.MaxValue], null);
    }

    [Fact]
    public static void DCJS_DictionaryWithTypeSimpleWithDictionaryMembers()
    {
        Type type = typeof(TypeWithSimpleDictionaryMember);
        DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);

        // Note that there is an extra "," before the final "}"
        string input = @"{""F1"":[{""Key"":""key"",""Value"":1}],}";

        TypeWithSimpleDictionaryMember obj;
        using (var ms = new MemoryStream())
        {
            using (var writer = new StreamWriter(ms))
            {
                writer.Write(input);
                writer.Flush();
                ms.Position = 0;

                obj = (TypeWithSimpleDictionaryMember)serializer.ReadObject(ms);
            }
        }

        var x = obj;
        var y = SerializeAndDeserialize<TypeWithSimpleDictionaryMember>(x, @"{""F1"":[{""Key"":""key"",""Value"":1}]}");

        Assert.True(y != null);
        Assert.True(y.F1["key"] == 1);
    }

    [Fact]
    public static void DCJS_TypesWithArrayOfOtherTypes()
    {
        var x = new TypeHasArrayOfASerializedAsB(true);

        var y = SerializeAndDeserialize<TypeHasArrayOfASerializedAsB>(x, @"{""Items"":[{""Name"":""typeAValue""},{""Name"":""typeBValue""}]}");
        Assert.StrictEqual(x.Items[0].Name, y.Items[0].Name);
        Assert.StrictEqual(x.Items[1].Name, y.Items[1].Name);
    }

    [Fact]
    public static void DCJS_WithDuplicateNames()
    {
        var x = new WithDuplicateNames(true);
        var y = SerializeAndDeserialize<WithDuplicateNames>(x, @"{""ClassA1"":{""Name"":""Hello World! 漢 ñ""},""ClassA2"":{""Nombre"":""""},""EnumA1"":1,""EnumA2"":1,""StructA1"":{""Text"":""""},""StructA2"":{""Texto"":""""}}");

        Assert.StrictEqual(x.ClassA1.Name, y.ClassA1.Name);
        Assert.StrictEqual(x.StructA1, y.StructA1);
        Assert.StrictEqual(x.EnumA1, y.EnumA1);
        Assert.StrictEqual(x.EnumA2, y.EnumA2);
        Assert.StrictEqual(x.StructA2, y.StructA2);
    }

    [Fact]
    public static void DCJS_XElementAsRoot()
    {
        var original = new XElement("ElementName1");
        original.SetAttributeValue(XName.Get("Attribute1"), "AttributeValue1");
        original.SetValue("Value1");
        var actual = SerializeAndDeserialize<XElement>(original, @"""<ElementName1 Attribute1=\""AttributeValue1\"">Value1<\/ElementName1>""");

        VerifyXElementObject(original, actual);
    }

    [Fact]
    public static void DCJS_WithXElement()
    {
        var original = new WithXElement(true);
        var actual = SerializeAndDeserialize<WithXElement>(original, @"{""e"":""<ElementName1 Attribute1=\""AttributeValue1\"">Value1<\/ElementName1>""}",
            skipStringCompare: true);

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
    public static void DCJS_WithXElementWithNestedXElement()
    {
        var original = new WithXElementWithNestedXElement(true);
        var actual = SerializeAndDeserialize<WithXElementWithNestedXElement>(original, @"{""e1"":""<ElementName1 Attribute1=\""AttributeValue1\""><ElementName2 Attribute2=\""AttributeValue2\"">Value2<\/ElementName2><\/ElementName1>""}");

        VerifyXElementObject(original.e1, actual.e1);
        VerifyXElementObject((XElement)original.e1.FirstNode, (XElement)actual.e1.FirstNode);
    }

    [Fact]
    public static void DCJS_WithArrayOfXElement()
    {
        var original = new WithArrayOfXElement(true);
        var actual = SerializeAndDeserialize<WithArrayOfXElement>(original, @"{""a"":[""<item xmlns=\""http:\/\/p.com\/\"">item0<\/item>"",""<item xmlns=\""http:\/\/p.com\/\"">item1<\/item>"",""<item xmlns=\""http:\/\/p.com\/\"">item2<\/item>""]}");

        Assert.StrictEqual(original.a.Length, actual.a.Length);
        VerifyXElementObject(original.a[0], actual.a[0], checkFirstAttribute: false);
        VerifyXElementObject(original.a[1], actual.a[1], checkFirstAttribute: false);
        VerifyXElementObject(original.a[2], actual.a[2], checkFirstAttribute: false);
    }

    [Fact]
    public static void DCJS_WithListOfXElement()
    {
        var original = new WithListOfXElement(true);
        var actual = SerializeAndDeserialize<WithListOfXElement>(original, @"{""list"":[""<item xmlns=\""http:\/\/p.com\/\"">item0<\/item>"",""<item xmlns=\""http:\/\/p.com\/\"">item1<\/item>"",""<item xmlns=\""http:\/\/p.com\/\"">item2<\/item>""]}");

        Assert.StrictEqual(original.list.Count, actual.list.Count);
        VerifyXElementObject(original.list[0], actual.list[0], checkFirstAttribute: false);
        VerifyXElementObject(original.list[1], actual.list[1], checkFirstAttribute: false);
        VerifyXElementObject(original.list[2], actual.list[2], checkFirstAttribute: false);
    }

    [Fact]
    public static void DCJS_TypeNamesWithSpecialCharacters()
    {
        var x = new __TypeNameWithSpecialCharacters漢ñ() { PropertyNameWithSpecialCharacters漢ñ = "Test" };
        var y = SerializeAndDeserialize<__TypeNameWithSpecialCharacters漢ñ>(x, @"{""PropertyNameWithSpecialCharacters漢ñ"":""Test""}");

        Assert.StrictEqual(x.PropertyNameWithSpecialCharacters漢ñ, y.PropertyNameWithSpecialCharacters漢ñ);
    }

    [Fact]
    public static void DCJS_JaggedArrayAsRoot()
    {
        int[][] jaggedIntegerArray = new int[][] { new int[] { 1, 3, 5, 7, 9 }, new int[] { 0, 2, 4, 6 }, new int[] { 11, 22 } };
        var actualJaggedIntegerArray = SerializeAndDeserialize<int[][]>(jaggedIntegerArray, "[[1,3,5,7,9],[0,2,4,6],[11,22]]");

        Assert.Equal(jaggedIntegerArray[0], actualJaggedIntegerArray[0]);
        Assert.Equal(jaggedIntegerArray[1], actualJaggedIntegerArray[1]);
        Assert.Equal(jaggedIntegerArray[2], actualJaggedIntegerArray[2]);


        string[][] jaggedStringArray = new string[][] { new string[] { "1", "3", "5", "7", "9" }, new string[] { "0", "2", "4", "6" }, new string[] { "11", "22" } };
        var actualJaggedStringArray = SerializeAndDeserialize<string[][]>(jaggedStringArray, @"[[""1"",""3"",""5"",""7"",""9""],[""0"",""2"",""4"",""6""],[""11"",""22""]]");

        Assert.Equal(jaggedStringArray[0], actualJaggedStringArray[0]);
        Assert.Equal(jaggedStringArray[1], actualJaggedStringArray[1]);
        Assert.Equal(jaggedStringArray[2], actualJaggedStringArray[2]);

        var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(new DateTime(2013, 1, 2)).TotalMinutes;
        var timeZoneString = string.Format("{0:+;-}{1}", offsetMinutes, new TimeSpan(0, offsetMinutes, 0).ToString(@"hhmm"));
        object[] objectArray = new object[] { 1, 1.0F, 1.0, "string", Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860"), new DateTime(2013, 1, 2).AddMinutes(offsetMinutes) };
        var actualObjectArray = SerializeAndDeserialize<object[]>(objectArray, string.Format(@"[1,1,1,""string"",""2054fd3e-e118-476a-9962-1a882be51860"",""\/Date(1357084800000{0})\/""]", timeZoneString));

        Assert.StrictEqual(1, actualObjectArray[0]);
        Assert.StrictEqual(1, actualObjectArray[1]);
        Assert.StrictEqual(1, actualObjectArray[2]);
        Assert.StrictEqual("string", actualObjectArray[3]);
        Assert.StrictEqual(Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860"), Guid.Parse(actualObjectArray[4].ToString()));
        Assert.StrictEqual(string.Format("/Date(1357084800000{0})/", timeZoneString), actualObjectArray[5].ToString());

        int[][][] jaggedIntegerArray2 = new int[][][] { new int[][] { new int[] { 1 }, new int[] { 3 } }, new int[][] { new int[] { 0 } }, new int[][] { new int[] { } } };
        var actualJaggedIntegerArray2 = SerializeAndDeserialize<int[][][]>(jaggedIntegerArray2, "[[[1],[3]],[[0]],[[]]]");

        Assert.True(actualJaggedIntegerArray2.Length == 3);
        Assert.True(actualJaggedIntegerArray2[0][0][0] == 1);
        Assert.True(actualJaggedIntegerArray2[0][1][0] == 3);
        Assert.True(actualJaggedIntegerArray2[1][0][0] == 0);
        Assert.True(actualJaggedIntegerArray2[2][0].Length == 0);
    }

    [Fact]
    public static void DCJS_EnumerableStruct()
    {
        var original = new EnumerableStruct();
        original.Add("a");
        original.Add("b");
        var actual = SerializeAndDeserialize<EnumerableStruct>(original, @"[""a"",""b""]");

        Assert.Equal((IEnumerable<string>)actual, (IEnumerable<string>)original);
    }

    [Fact]
    public static void DCJS_EnumerableCollection()
    {
        var dates = new DateTime[] { new DateTime(2000, 1, 1), new DateTime(2000, 1, 2), new DateTime(2000, 1, 3) };
        var original = new EnumerableCollection();
        var timeZoneStrings = new List<string>();
        foreach (var date in dates)
        {
            // DO NOT USE TimeZoneInfo.Local.BaseUtcOffset !
            var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(date).TotalMinutes;
            original.Add(date.AddMinutes(offsetMinutes));
            timeZoneStrings.Add(string.Format("{0:+;-}{1}", offsetMinutes, new TimeSpan(0, offsetMinutes, 0).ToString(@"hhmm")));
        }
        var actual = SerializeAndDeserialize<EnumerableCollection>(original, string.Format(@"[""\/Date(946684800000{0})\/"",""\/Date(946771200000{0})\/"",""\/Date(946857600000{0})\/""]", timeZoneStrings.ToArray()));

        Assert.Equal((IEnumerable<DateTime>)actual, (IEnumerable<DateTime>)original);
    }

    [Fact]
    public static void DCJS_ContainsLinkedList()
    {
        var original = new ContainsLinkedList(true);
        var actual = SerializeAndDeserialize<ContainsLinkedList>(original, @"{""Data"":[{""Data"":{""Data"":""23:59:59""},""RefData"":{""Data"":""23:59:59""}},{""Data"":{""Data"":""23:59:59""},""RefData"":{""Data"":""23:59:59""}},{""Data"":{""Data"":""23:59:59""},""RefData"":{""Data"":""23:59:59""}},{""Data"":{""Data"":""23:59:59""},""RefData"":{""Data"":""23:59:59""}},{""Data"":{""Data"":""23:59:59""},""RefData"":{""Data"":""23:59:59""}},{""Data"":{""Data"":""23:59:59""},""RefData"":{""Data"":""23:59:59""}},{""Data"":{""Data"":""23:59:59""},""RefData"":{""Data"":""23:59:59""}}]}");

        var actualEnumerator = actual.Data.GetEnumerator();
        var originalEnumerator = original.Data.GetEnumerator();

        while (originalEnumerator.MoveNext() && actualEnumerator.MoveNext())
        {
            var orininalElement = originalEnumerator.Current;
            var actualElement = actualEnumerator.Current;

            Assert.StrictEqual(orininalElement.Data.Data, actualElement.Data.Data);
            Assert.StrictEqual(orininalElement.RefData.Data, actualElement.RefData.Data);
        }
    }

    [Fact]
    public static void DCJS_SimpleCollectionDataContract()
    {
        var original = new SimpleCDC(true);
        var actual = SerializeAndDeserialize<SimpleCDC>(original, @"[""One"",""Two"",""Three""]");

        Assert.Equal((IEnumerable<string>)actual, (IEnumerable<string>)original);
    }

    [Fact]
    public static void DCJS_EnumFlags()
    {
        EnumFlags original = EnumFlags.One | EnumFlags.Four;
        var actual = SerializeAndDeserialize<EnumFlags>(original, "9");
        Assert.StrictEqual(original, actual);
    }



    [Fact]
    public static void DCJS_Nullables()
    {
        // Arrange
        var baseline = @"{""Optional"":1,""OptionalInt"":42,""Optionull"":null,""OptionullInt"":null,""Struct1"":{""A"":1,""B"":2},""Struct2"":null}";
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
    public static void DCJS_InternalTypeSerialization()
    {
        var value = new InternalType() { InternalProperty = 12 };
        var deserializedValue = SerializeAndDeserialize<InternalType>(value, @"{""InternalProperty"":12,""PrivateProperty"":100}");
        Assert.StrictEqual(deserializedValue.InternalProperty, value.InternalProperty);
        Assert.StrictEqual(deserializedValue.GetPrivatePropertyValue(), value.GetPrivatePropertyValue());
    }

    [Fact]
    public static void DCJS_PrivateTypeSerialization()
    {
        var value = new PrivateType();
        var deserializedValue = SerializeAndDeserialize<PrivateType>(value, @"{""InternalProperty"":1,""PrivateProperty"":2}");
        Assert.StrictEqual(deserializedValue.GetInternalPropertyValue(), value.GetInternalPropertyValue());
        Assert.StrictEqual(deserializedValue.GetPrivatePropertyValue(), value.GetPrivatePropertyValue());
    }

    [Fact]
    public static void DCJS_KnownTypesThroughConstructor()
    {
        //Constructor # 3
        var value = new KnownTypesThroughConstructor() { EnumValue = MyEnum.One, SimpleTypeValue = new SimpleKnownTypeValue() { StrProperty = "PropertyValue" } };
        var actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value,
        @"{""EnumValue"":0,""SimpleTypeValue"":{""__type"":""SimpleKnownTypeValue:#SerializationTypes"",""StrProperty"":""PropertyValue""}}",
        null, () => { return new DataContractJsonSerializer(typeof(KnownTypesThroughConstructor), new Type[] { typeof(MyEnum), typeof(SimpleKnownTypeValue) }); });

        Assert.StrictEqual((MyEnum)value.EnumValue, (MyEnum)actual.EnumValue);
        Assert.True(actual.SimpleTypeValue is SimpleKnownTypeValue);
        Assert.StrictEqual(((SimpleKnownTypeValue)actual.SimpleTypeValue).StrProperty, "PropertyValue");
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
    public static void DCJS_EmptyString_Throws()
    {
        Type type = typeof(string);
        DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
        string input = "";

        using (var ms = new MemoryStream())
        {
            using (var writer = new StreamWriter(ms))
            {
                writer.Write(input);
                writer.Flush();
                ms.Position = 0;

                Assert.Throws<SerializationException>(() =>
                {
                    serializer.ReadObject(ms);
                });
            }
        }
    }

    [Fact]
    public static void DCJS_ClassWithDatetimeOffsetTypeProperty()
    {
        var value = new TypeWithDateTimeOffsetTypeProperty() { ModifiedTime = new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc)) };
        var actual = SerializeAndDeserialize(value, @"{""ModifiedTime"":{""DateTime"":""\/Date(1357095845006)\/"",""OffsetMinutes"":0}}");
        Assert.StrictEqual<DateTimeOffset>(value.ModifiedTime, actual.ModifiedTime);
    }


    [Fact]
    public static void DCJS_GenericTypeWithPrivateSetter()
    {
        var value = new GenericTypeWithPrivateSetter<string>("PropertyWithPrivateSetter's value");
        var actual = SerializeAndDeserialize(value, @"{""PropertyWithPrivateSetter"":""PropertyWithPrivateSetter's value""}");
        Assert.StrictEqual(value.PropertyWithPrivateSetter, actual.PropertyWithPrivateSetter);
    }

    [Fact]
    public static void DCJS_TypeWithDateTimeStringProperty()
    {
        DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(TypeWithDateTimeStringProperty));
        var obj = new TypeWithDateTimeStringProperty()
        {
            DateTimeString = @"\/Date(1411072352108-0700)\/",
            CurrentDateTime = new DateTime(2015, 1, 1)
        };

        // Serialization-deserialization
        using (MemoryStream stream = new MemoryStream())
        {
            dcjs.WriteObject(stream, obj);
            stream.Position = 0;
            var serializedStr = new StreamReader(stream).ReadToEnd();

            stream.Position = 0;
            var obj2 = (TypeWithDateTimeStringProperty)dcjs.ReadObject(stream);
            Assert.StrictEqual(obj.DateTimeString, obj2.DateTimeString);
            Assert.StrictEqual(obj.CurrentDateTime, obj2.CurrentDateTime);
        }

        // Deserialization only. The provided string of DateTimeString is in different format than the serialized string.
        // The serialized string is: {"DateTimeString":"\/Date(1411072352108-0700)\/"}
        using (MemoryStream ms = new MemoryStream())
        {
            StreamWriter sw = new StreamWriter(ms);
            sw.WriteLine(@"{""DateTimeString"":""\/Date(1411072352108-0700)\/""}");
            sw.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            var result = (TypeWithDateTimeStringProperty)dcjs.ReadObject(ms);
            Assert.StrictEqual(result.DateTimeString, @"/Date(1411072352108-0700)/");
        }
    }

    [Fact]
    public static void DCJS_TypeWithGenericDictionaryAsKnownType()
    {
        TypeWithGenericDictionaryAsKnownType value = new TypeWithGenericDictionaryAsKnownType { };
        value.Foo.Add(10, new Level() { Name = "Foo", LevelNo = 1 });
        value.Foo.Add(20, new Level() { Name = "Bar", LevelNo = 2 });
        var deserializedValue = SerializeAndDeserialize<TypeWithGenericDictionaryAsKnownType>(value, @"{""Foo"":[{""Key"":10,""Value"":{""LevelNo"":1,""Name"":""Foo""}},{""Key"":20,""Value"":{""LevelNo"":2,""Name"":""Bar""}}]}");

        Assert.StrictEqual(2, deserializedValue.Foo.Count);
        Assert.StrictEqual("Foo", deserializedValue.Foo[10].Name);
        Assert.StrictEqual(1, deserializedValue.Foo[10].LevelNo);
        Assert.StrictEqual("Bar", deserializedValue.Foo[20].Name);
        Assert.StrictEqual(2, deserializedValue.Foo[20].LevelNo);
    }

    [Fact]
    public static void DCJS_TypeWithKnownTypeAttributeAndInterfaceMember()
    {
        TypeWithKnownTypeAttributeAndInterfaceMember value = new TypeWithKnownTypeAttributeAndInterfaceMember();
        value.HeadLine = new NewsArticle() { Title = "Foo News" };
        var deserializedValue = SerializeAndDeserialize<TypeWithKnownTypeAttributeAndInterfaceMember>(value, @"{""HeadLine"":{""__type"":""NewsArticle:#SerializationTypes"",""Category"":""News"",""Title"":""Foo News""}}");

        Assert.StrictEqual("News", deserializedValue.HeadLine.Category);
        Assert.StrictEqual("Foo News", deserializedValue.HeadLine.Title);
    }

    [Fact]
    public static void DCJS_TypeWithKnownTypeAttributeAndListOfInterfaceMember()
    {
        TypeWithKnownTypeAttributeAndListOfInterfaceMember value = new TypeWithKnownTypeAttributeAndListOfInterfaceMember();
        value.Articles = new List<IArticle>() { new SummaryArticle() { Title = "Bar Summary" } };
        var deserializedValue = SerializeAndDeserialize<TypeWithKnownTypeAttributeAndListOfInterfaceMember>(value, @"{""Articles"":[{""__type"":""SummaryArticle:#SerializationTypes"",""Category"":""Summary"",""Title"":""Bar Summary""}]}");

        Assert.StrictEqual(1, deserializedValue.Articles.Count);
        Assert.StrictEqual("Summary", deserializedValue.Articles[0].Category);
        Assert.StrictEqual("Bar Summary", deserializedValue.Articles[0].Title);
    }

    [Fact]
    public static void DCJS_Tuple()
    {
        DCJS_Tuple1();
        DCJS_Tuple2();
        DCJS_Tuple3();
        DCJS_Tuple4();
        DCJS_Tuple5();
        DCJS_Tuple6();
        DCJS_Tuple7();
        DCJS_Tuple8();
    }

    private static void DCJS_Tuple1()
    {
        Tuple<int> value = new Tuple<int>(1);
        var deserializedValue = SerializeAndDeserialize<Tuple<int>>(value, @"{""m_Item1"":1}");
        Assert.StrictEqual<Tuple<int>>(value, deserializedValue);
    }

    private static void DCJS_Tuple2()
    {
        Tuple<int, int> value = new Tuple<int, int>(1, 2);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int>>(value, @"{""m_Item1"":1,""m_Item2"":2}");
        Assert.StrictEqual<Tuple<int, int>>(value, deserializedValue);
    }

    private static void DCJS_Tuple3()
    {
        Tuple<int, int, int> value = new Tuple<int, int, int>(1, 2, 3);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int>>(value, @"{""m_Item1"":1,""m_Item2"":2,""m_Item3"":3}");
        Assert.StrictEqual<Tuple<int, int, int>>(value, deserializedValue);
    }

    private static void DCJS_Tuple4()
    {
        Tuple<int, int, int, int> value = new Tuple<int, int, int, int>(1, 2, 3, 4);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int>>(value, @"{""m_Item1"":1,""m_Item2"":2,""m_Item3"":3,""m_Item4"":4}");
        Assert.StrictEqual<Tuple<int, int, int, int>>(value, deserializedValue);
    }

    private static void DCJS_Tuple5()
    {
        Tuple<int, int, int, int, int> value = new Tuple<int, int, int, int, int>(1, 2, 3, 4, 5);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int, int>>(value, @"{""m_Item1"":1,""m_Item2"":2,""m_Item3"":3,""m_Item4"":4,""m_Item5"":5}");
        Assert.StrictEqual<Tuple<int, int, int, int, int>>(value, deserializedValue);
    }

    private static void DCJS_Tuple6()
    {
        Tuple<int, int, int, int, int, int> value = new Tuple<int, int, int, int, int, int>(1, 2, 3, 4, 5, 6);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int, int, int>>(value, @"{""m_Item1"":1,""m_Item2"":2,""m_Item3"":3,""m_Item4"":4,""m_Item5"":5,""m_Item6"":6}");
        Assert.StrictEqual<Tuple<int, int, int, int, int, int>>(value, deserializedValue);
    }

    private static void DCJS_Tuple7()
    {
        Tuple<int, int, int, int, int, int, int> value = new Tuple<int, int, int, int, int, int, int>(1, 2, 3, 4, 5, 6, 7);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int, int, int, int>>(value, @"{""m_Item1"":1,""m_Item2"":2,""m_Item3"":3,""m_Item4"":4,""m_Item5"":5,""m_Item6"":6,""m_Item7"":7}");
        Assert.StrictEqual<Tuple<int, int, int, int, int, int, int>>(value, deserializedValue);
    }

    private static void DCJS_Tuple8()
    {
        Tuple<int, int, int, int, int, int, int, Tuple<int>> value = new Tuple<int, int, int, int, int, int, int, Tuple<int>>(1, 2, 3, 4, 5, 6, 7, new Tuple<int>(8));
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int, int, int, int, Tuple<int>>>(value, @"{""m_Item1"":1,""m_Item2"":2,""m_Item3"":3,""m_Item4"":4,""m_Item5"":5,""m_Item6"":6,""m_Item7"":7,""m_Rest"":{""m_Item1"":8}}");
        Assert.StrictEqual<Tuple<int, int, int, int, int, int, int, Tuple<int>>>(value, deserializedValue);
    }

    [Fact]
    public static void DCJS_GenericQueue()
    {
        Queue<int> value = new Queue<int>();
        value.Enqueue(1);
        object syncRoot = ((ICollection)value).SyncRoot;
        var deserializedValue = SerializeAndDeserialize<Queue<int>>(value, @"{""_array"":[1,0,0,0],""_head"":0,""_size"":1,""_tail"":1,""_version"":2}");
        var a1 = value.ToArray();
        var a2 = deserializedValue.ToArray();
        Assert.StrictEqual(a1.Length, a2.Length);
        Assert.StrictEqual(a1[0], a2[0]);
    }

    [Fact]
    public static void DCJS_GenericStack()
    {
        var value = new Stack<int>();
        value.Push(123);
        value.Push(456);
        object syncRoot = ((ICollection)value).SyncRoot;
        var deserializedValue = SerializeAndDeserialize<Stack<int>>(value, @"{""_array"":[123,456,0,0],""_size"":2,""_version"":2}");
        var a1 = value.ToArray();
        var a2 = deserializedValue.ToArray();
        Assert.StrictEqual(a1.Length, a2.Length);
        Assert.StrictEqual(a1[0], a2[0]);
        Assert.StrictEqual(a1[1], a2[1]);
    }

    [Fact]
    public static void DCJS_Queue()
    {
        var value = new Queue();
        value.Enqueue(123);
        value.Enqueue("Foo");
        object syncRoot = ((ICollection)value).SyncRoot;
        var deserializedValue = SerializeAndDeserialize<Queue>(value, @"{""_array"":[123,""Foo"",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null],""_growFactor"":200,""_head"":0,""_size"":2,""_tail"":2,""_version"":2}");
        var a1 = value.ToArray();
        var a2 = deserializedValue.ToArray();
        Assert.StrictEqual(a1.Length, a2.Length);
        Assert.StrictEqual(a1[0], a2[0]);
    }

    [Fact]
    public static void DCJS_Stack()
    {
        var value = new Stack();
        value.Push(123);
        value.Push("Foo");
        object syncRoot = ((ICollection)value).SyncRoot;
        var deserializedValue = SerializeAndDeserialize<Stack>(value, @"{""_array"":[123,""Foo"",null,null,null,null,null,null,null,null],""_size"":2,""_version"":2}");
        var a1 = value.ToArray();
        var a2 = deserializedValue.ToArray();
        Assert.StrictEqual(a1.Length, a2.Length);
        Assert.StrictEqual(a1[0], a2[0]);
        Assert.StrictEqual(a1[1], a2[1]);
    }

    [Fact]
    public static void DCJS_SortedList()
    {
        var value = new SortedList();
        value.Add(456, "Foo");
        value.Add(123, "Bar");
        var deserializedValue = SerializeAndDeserialize<SortedList>(value, @"[{""Key"":123,""Value"":""Bar""},{""Key"":456,""Value"":""Foo""}]");
        Assert.StrictEqual(value.Count, deserializedValue.Count);
        Assert.StrictEqual(value[0], deserializedValue[0]);
        Assert.StrictEqual(value[1], deserializedValue[1]);
    }

    [Fact]
    public static void DCJS_SystemVersion()
    {
        Version value = new Version(1, 2, 3, 4);
        var deserializedValue = SerializeAndDeserialize<Version>(value, @"{""_Build"":3,""_Major"":1,""_Minor"":2,""_Revision"":4}");
        Assert.StrictEqual(value.Major, deserializedValue.Major);
        Assert.StrictEqual(value.Minor, deserializedValue.Minor);
        Assert.StrictEqual(value.Build, deserializedValue.Build);
        Assert.StrictEqual(value.Revision, deserializedValue.Revision);
    }

    [Fact]
    public static void DCJS_ObjectArrayAsObject()
    {
        string jsonString = @"[""Foo"",123,""456""]";
        MemoryStream stream = new MemoryStream();
        StreamWriter sw = new StreamWriter(stream);
        sw.Write(jsonString);
        sw.Flush();
        stream.Seek(0, SeekOrigin.Begin);

        DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(object));
        var deserializedValue = (object[])dcjs.ReadObject(stream);
        Assert.StrictEqual(3, deserializedValue.Length);
        Assert.StrictEqual("Foo", deserializedValue[0]);
        Assert.StrictEqual(123, deserializedValue[1]);
        Assert.StrictEqual("456", deserializedValue[2]);
    }

    [Fact]
    public static void DCJS_GenericTypeWithNestedGenerics()
    {
        GenericTypeWithNestedGenerics<int>.InnerGeneric<double> value = new GenericTypeWithNestedGenerics<int>.InnerGeneric<double>()
        {
            data1 = 123,
            data2 = 4.56
        };
        var deserializedValue = SerializeAndDeserialize<GenericTypeWithNestedGenerics<int>.InnerGeneric<double>>(value, @"{""data1"":123,""data2"":4.56}");
        Assert.StrictEqual(value.data1, deserializedValue.data1);
        Assert.StrictEqual(value.data2, deserializedValue.data2);
    }

    [Fact]
    public static void DCJS_ClassImplementingIXmlSerialiable()
    {
        ClassImplementingIXmlSerialiable value = new ClassImplementingIXmlSerialiable() { StringValue = "Foo" };
        var deserializedValue = SerializeAndDeserialize<ClassImplementingIXmlSerialiable>(value, @"""<ClassImplementingIXmlSerialiable StringValue=\""Foo\"" BoolValue=\""True\"" xmlns=\""http:\/\/schemas.datacontract.org\/2004\/07\/SerializationTypes\""\/>""");
        Assert.StrictEqual(value.StringValue, deserializedValue.StringValue);
    }

    [Fact]
    public static void DCJS_TypeWithNestedGenericClassImplementingIXmlSerialiable()
    {
        TypeWithNestedGenericClassImplementingIXmlSerialiable.NestedGenericClassImplementingIXmlSerialiable<bool> value = new TypeWithNestedGenericClassImplementingIXmlSerialiable.NestedGenericClassImplementingIXmlSerialiable<bool>() { StringValue = "Foo" };
        var deserializedValue = SerializeAndDeserialize<TypeWithNestedGenericClassImplementingIXmlSerialiable.NestedGenericClassImplementingIXmlSerialiable<bool>>(value, @"""<TypeWithNestedGenericClassImplementingIXmlSerialiable.NestedGenericClassImplementingIXmlSerialiableOfbooleanRvdAXEcW StringValue=\""Foo\"" xmlns=\""http:\/\/schemas.datacontract.org\/2004\/07\/SerializationTypes\""\/>""");
        Assert.StrictEqual(value.StringValue, deserializedValue.StringValue);
    }

    public static void DCJS_SerializationEvents()
    {
        var input = new MyType() { Value = "string value" };
        var output = SerializeAndDeserialize<MyType>(input, @"{""Value"":""string value""}");

        Assert.True(input.OnSerializingMethodInvoked, "input.OnSerializingMethodInvoked is false");
        Assert.True(input.OnSerializedMethodInvoked, "input.OnSerializedMethodInvoked is false");
        Assert.True(output.OnDeserializingMethodInvoked, "output.OnDeserializingMethodInvoked is false");
        Assert.True(output.OnDeserializedMethodInvoked, "output.OnDeserializedMethodInvoked is false");
    }

    [Fact]
    public static void DCJS_DeserializeEmptyString()
    {
        var serializer = new DataContractJsonSerializer(typeof(object));
        Assert.Throws<SerializationException>(() =>
        {
            serializer.ReadObject(new MemoryStream());
        });
    }

    [Fact]
    public static void DCJS_UseSimpleDictionaryFormat()
    {
        Dictionary<string, string> dict = new Dictionary<string, string>();
        dict.Add("key1", "value1");
        dict.Add("key2", "value2");
        var deserialized = SerializeAndDeserialize(dict, @"{""key1"":""value1"",""key2"":""value2""}",
            new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
        Assert.StrictEqual(2, deserialized.Count);
        Assert.True(deserialized.ContainsKey("key1"));
        Assert.True(deserialized.ContainsKey("key2"));
        Assert.StrictEqual(dict["key1"], deserialized["key1"]);
        Assert.StrictEqual(dict["key2"], deserialized["key2"]);
    }

    [Fact]
    public static void DCJS_DeserializeTypeWithInnerInvalidDataContract()
    {
        DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(TypeWithPropertyWithoutDefaultCtor));
        string jsonString = @"{""Name"":""Foo""}";
        MemoryStream ms = new MemoryStream();
        StreamWriter sw = new StreamWriter(ms);
        sw.Write(jsonString);
        sw.Flush();
        ms.Seek(0, SeekOrigin.Begin);

        TypeWithPropertyWithoutDefaultCtor deserializedValue = (TypeWithPropertyWithoutDefaultCtor)dcjs.ReadObject(ms);
        Assert.StrictEqual("Foo", deserializedValue.Name);
        Assert.StrictEqual(null, deserializedValue.MemberWithInvalidDataContract);
    }

    [Fact]
    public static void DCJS_ReadOnlyCollection()
    {
        List<string> list = new List<string>() { "Foo", "Bar" };
        ReadOnlyCollection<string> value = new ReadOnlyCollection<string>(list);
        var deserializedValue = SerializeAndDeserialize<ReadOnlyCollection<string>>(value, @"{""list"":[""Foo"",""Bar""]}");
        Assert.StrictEqual(value.Count, deserializedValue.Count);
        Assert.StrictEqual(value[0], deserializedValue[0]);
        Assert.StrictEqual(value[1], deserializedValue[1]);
    }

    [Fact]
    public static void DCJS_ReadOnlyDictionary()
    {
        var dict = new Dictionary<string, int>();
        dict["Foo"] = 1;
        dict["Bar"] = 2;
        ReadOnlyDictionary<string, int> value = new ReadOnlyDictionary<string, int>(dict);
        var deserializedValue = SerializeAndDeserialize(value, @"{""_dictionary"":[{""Key"":""Foo"",""Value"":1},{""Key"":""Bar"",""Value"":2}]}");

        Assert.StrictEqual(value.Count, deserializedValue.Count);
        Assert.StrictEqual(value["Foo"], deserializedValue["Foo"]);
        Assert.StrictEqual(value["Bar"], deserializedValue["Bar"]);
    }

    [Fact]
    public static void DCJS_KeyValuePair()
    {
        var value = new KeyValuePair<string, object>("FooKey", "FooValue");
        var deserializedValue = SerializeAndDeserialize<KeyValuePair<string, object>>(value, @"{""key"":""FooKey"",""value"":""FooValue""}");

        Assert.StrictEqual(value.Key, deserializedValue.Key);
        Assert.StrictEqual(value.Value, deserializedValue.Value);
    }

    [Fact]
    public static void DCJS_DataContractWithDotInName()
    {
        DataContractWithDotInName value = new DataContractWithDotInName() { Name = "Foo" };
        var deserializedValue = SerializeAndDeserialize<DataContractWithDotInName>(value, @"{""Name"":""Foo""}");

        Assert.NotNull(deserializedValue);
        Assert.StrictEqual(value.Name, deserializedValue.Name);
    }

    [Fact]
    public static void DCJS_DataContractWithMinusSignInName()
    {
        DataContractWithMinusSignInName value = new DataContractWithMinusSignInName() { Name = "Foo" };
        var deserializedValue = SerializeAndDeserialize<DataContractWithMinusSignInName>(value, @"{""Name"":""Foo""}");

        Assert.NotNull(deserializedValue);
        Assert.StrictEqual(value.Name, deserializedValue.Name);
    }

    [Fact]
    public static void DCJS_DataContractWithOperatorsInName()
    {
        DataContractWithOperatorsInName value = new DataContractWithOperatorsInName() { Name = "Foo" };
        var deserializedValue = SerializeAndDeserialize<DataContractWithOperatorsInName>(value, @"{""Name"":""Foo""}");

        Assert.NotNull(deserializedValue);
        Assert.StrictEqual(value.Name, deserializedValue.Name);
    }

    [Fact]
    public static void DCJS_DataContractWithOtherSymbolsInName()
    {
        DataContractWithOtherSymbolsInName value = new DataContractWithOtherSymbolsInName() { Name = "Foo" };
        var deserializedValue = SerializeAndDeserialize<DataContractWithOtherSymbolsInName>(value, @"{""Name"":""Foo""}");

        Assert.NotNull(deserializedValue);
        Assert.StrictEqual(value.Name, deserializedValue.Name);
    }

    [Fact]
    public static void DCJS_CollectionDataContractWithCustomKeyName()
    {
        CollectionDataContractWithCustomKeyName value = new CollectionDataContractWithCustomKeyName();
        value.Add(100, 123);
        value.Add(200, 456);
        var deserializedValue = SerializeAndDeserialize<CollectionDataContractWithCustomKeyName>(value, @"[{""Key"":100,""Value"":123},{""Key"":200,""Value"":456}]");

        Assert.NotNull(deserializedValue);
        Assert.StrictEqual(value[100], deserializedValue[100]);
        Assert.StrictEqual(value[200], deserializedValue[200]);
    }

    [Fact]
    public static void DCJS_CollectionDataContractWithCustomKeyNameDuplicate()
    {
        CollectionDataContractWithCustomKeyNameDuplicate value = new CollectionDataContractWithCustomKeyNameDuplicate();
        value.Add(100, 123);
        value.Add(200, 456);
        var deserializedValue = SerializeAndDeserialize<CollectionDataContractWithCustomKeyNameDuplicate>(value, @"[{""Key"":100,""Value"":123},{""Key"":200,""Value"":456}]");

        Assert.NotNull(deserializedValue);
        Assert.StrictEqual(value[100], deserializedValue[100]);
        Assert.StrictEqual(value[200], deserializedValue[200]);
    }

    [Fact]
    public static void DCJS_TypeWithCollectionWithoutDefaultConstructor()
    {
        TypeWithCollectionWithoutDefaultConstructor value = new TypeWithCollectionWithoutDefaultConstructor();
        value.CollectionProperty.Add("Foo");
        value.CollectionProperty.Add("Bar");
        var deserializedValue = SerializeAndDeserialize<TypeWithCollectionWithoutDefaultConstructor>(value, @"{""CollectionProperty"":[""Foo"",""Bar""]}");

        Assert.NotNull(deserializedValue);
        Assert.NotNull(deserializedValue.CollectionProperty);
        Assert.StrictEqual(value.CollectionProperty.Count, deserializedValue.CollectionProperty.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.CollectionProperty, deserializedValue.CollectionProperty));
    }

    [Fact]
    public static void DCJS_DataMemberNames()
    {
        var obj = new AppEnvironment()
        {
            ScreenDpi = 440,
            ScreenOrientation = "horizontal"
        };
        var actual = SerializeAndDeserialize(obj, @"{""screen_dpi(x:y)"":440,""screen:orientation"":""horizontal""}");
        Assert.StrictEqual(obj.ScreenDpi, actual.ScreenDpi);
        Assert.StrictEqual(obj.ScreenOrientation, actual.ScreenOrientation);
    }

    [Fact]
    public static void DCJS_CollectionInterfaceGetOnlyCollection()
    {
        var obj = new TypeWithCollectionInterfaceGetOnlyCollection(new List<string>() { "item1", "item2", "item3" });
        var deserializedObj = SerializeAndDeserialize(obj, @"{""Items"":[""item1"",""item2"",""item3""]}");
        Assert.Equal(obj.Items, deserializedObj.Items);
    }

    [Fact]
    public static void DCJS_EnumerableInterfaceGetOnlyCollection()
    {
        // Expect exception in deserialization process
        Assert.Throws<InvalidDataContractException>(() => {
            var obj = new TypeWithEnumerableInterfaceGetOnlyCollection(new List<string>() { "item1", "item2", "item3" });
            SerializeAndDeserialize(obj, @"{""Items"":[""item1"",""item2"",""item3""]}");
        });
    }

    [Fact]
    public static void DCJS_XmlElementAsRoot()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml(@"<html></html>");
        XmlElement expected = xDoc.CreateElement("Element");
        expected.InnerText = "Element innertext";
        var actual = SerializeAndDeserialize(expected, @"""<Element>Element innertext<\/Element>""");
        Assert.NotNull(actual);
        Assert.StrictEqual(expected.InnerText, actual.InnerText);
    }

    [Fact]
    public static void DCJS_TypeWithXmlElementProperty()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml(@"<html></html>");
        XmlElement productElement = xDoc.CreateElement("Product");
        productElement.InnerText = "Product innertext";
        XmlElement categoryElement = xDoc.CreateElement("Category");
        categoryElement.InnerText = "Category innertext";
        var expected = new TypeWithXmlElementProperty() { Elements = new[] { productElement, categoryElement } };
        var actual = SerializeAndDeserialize(expected, @"{""Elements"":[""<Product>Product innertext<\/Product>"",""<Category>Category innertext<\/Category>""]}");
        Assert.StrictEqual(expected.Elements.Length, actual.Elements.Length);
        for (int i = 0; i < expected.Elements.Length; ++i)
        {
            Assert.StrictEqual(expected.Elements[i].InnerText, actual.Elements[i].InnerText);
        }
    }

    [Fact]
    public static void DCJS_RecursiveCollection()
    {
        Assert.Throws<InvalidDataContractException>(() =>
        {
            (new DataContractJsonSerializer(typeof(RecursiveCollection))).WriteObject(new MemoryStream(), new RecursiveCollection());
        });
    }

    [Fact]
    public static void DCJS_TypeWithInternalDefaultConstructor()
    {
        var value = TypeWithInternalDefaultConstructor.CreateInstance();

        value.Name = "foo";
        var actual = SerializeAndDeserialize(value, "{\"Name\":\"foo\"}");

        Assert.NotNull(actual);
        Assert.Equal(value.Name, actual.Name);
    }

    [Fact]
    public static void DCJS_TypeWithInternalDefaultConstructorWithoutDataContractAttribute()
    {
        var value = TypeWithInternalDefaultConstructorWithoutDataContractAttribute.CreateInstance();

        value.Name = "foo";
        var actual = SerializeAndDeserialize(value, "{\"Name\":\"foo\"}");

        Assert.NotNull(actual);
        Assert.Equal(value.Name, actual.Name);
    }

    [Fact]
    public static void DCJS_TypeWithPrimitiveProperties()
    {
        TypeWithPrimitiveProperties x = new TypeWithPrimitiveProperties { P1 = "abc", P2 = 11 };
        TypeWithPrimitiveProperties y = SerializeAndDeserialize<TypeWithPrimitiveProperties>(x, "{\"P1\":\"abc\",\"P2\":11}");
        Assert.StrictEqual(x.P1, y.P1);
        Assert.StrictEqual(x.P2, y.P2);
    }

    [Fact]
    public static void DCJS_TypeWithPrimitiveFields()
    {
        TypeWithPrimitiveFields x = new TypeWithPrimitiveFields { P1 = "abc", P2 = 11 };
        TypeWithPrimitiveFields y = SerializeAndDeserialize<TypeWithPrimitiveFields>(x, "{\"P1\":\"abc\",\"P2\":11}");
        Assert.StrictEqual(x.P1, y.P1);
        Assert.StrictEqual(x.P2, y.P2);
    }

    [Fact]
    public static void DCJS_TypeWithAllPrimitiveProperties()
    {
        TypeWithAllPrimitiveProperties x = new TypeWithAllPrimitiveProperties
        {
            BooleanMember = true,
            //ByteArrayMember = new byte[] { 1, 2, 3, 4 },
            CharMember = 'C',
            DateTimeMember = new DateTime(2016, 7, 8, 9, 10, 11, DateTimeKind.Utc),
            DecimalMember = new decimal(123, 456, 789, true, 0),
            DoubleMember = 123.456,
            FloatMember = 456.789f,
            GuidMember = Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860"),
            //public byte[] HexBinaryMember 
            StringMember = "abc",
            IntMember = 123
        };
        TypeWithAllPrimitiveProperties y = SerializeAndDeserialize<TypeWithAllPrimitiveProperties>(x, "{\"BooleanMember\":true,\"CharMember\":\"C\",\"DateTimeMember\":\"\\/Date(1467969011000)\\/\",\"DecimalMember\":-14554481076115341312123,\"DoubleMember\":123.456,\"FloatMember\":456.789,\"GuidMember\":\"2054fd3e-e118-476a-9962-1a882be51860\",\"IntMember\":123,\"StringMember\":\"abc\"}");
        Assert.StrictEqual(x.BooleanMember, y.BooleanMember);
        //Assert.StrictEqual(x.ByteArrayMember, y.ByteArrayMember);
        Assert.StrictEqual(x.CharMember, y.CharMember);
        Assert.StrictEqual(x.DateTimeMember, y.DateTimeMember);
        Assert.StrictEqual(x.DecimalMember, y.DecimalMember);
        Assert.StrictEqual(x.DoubleMember, y.DoubleMember);
        Assert.StrictEqual(x.FloatMember, y.FloatMember);
        Assert.StrictEqual(x.GuidMember, y.GuidMember);
        Assert.StrictEqual(x.StringMember, y.StringMember);
        Assert.StrictEqual(x.IntMember, y.IntMember);
    }

    #region Array of primitive types

    [Fact]
    public static void DCJS_ArrayOfBoolean()
    {
        var value = new bool[] { true, false, true };
        var deserialized = SerializeAndDeserialize(value, "[true,false,true]");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_ArrayOfDateTime()
    {
        var value = new DateTime[] { new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc), new DateTime(2011, 2, 3, 4, 5, 6, DateTimeKind.Utc) };
        var deserialized = SerializeAndDeserialize(value, "[\"\\/Date(946782245000)\\/\",\"\\/Date(1296705906000)\\/\"]");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_ArrayOfDecimal()
    {
        var value = new decimal[] { new decimal(1, 2, 3, false, 1), new decimal(4, 5, 6, true, 2) };
        var deserialized = SerializeAndDeserialize(value, "[5534023222971858944.1,-1106804644637321461.80]");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_ArrayOfInt32()
    {
        var value = new int[] { 123, int.MaxValue, int.MinValue };
        var deserialized = SerializeAndDeserialize(value, "[123,2147483647,-2147483648]");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_ArrayOfInt64()
    {
        var value = new long[] { 123, long.MaxValue, long.MinValue };
        var deserialized = SerializeAndDeserialize(value, "[123,9223372036854775807,-9223372036854775808]");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_ArrayOfSingle()
    {
        var value = new float[] { 1.23f, 4.56f, 7.89f };
        var deserialized = SerializeAndDeserialize(value, "[1.23,4.56,7.89]");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_ArrayOfDouble()
    {
        var value = new double[] { 1.23, 4.56, 7.89 };
        var deserialized = SerializeAndDeserialize(value, "[1.23,4.56,7.89]");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_ArrayOfString()
    {
        var value = new string[] { "abc", "def", "xyz" };
        var deserialized = SerializeAndDeserialize(value, "[\"abc\",\"def\",\"xyz\"]");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_ArrayOfTypeWithPrimitiveProperties()
    {
        var value = new TypeWithPrimitiveProperties[]
        {
            new TypeWithPrimitiveProperties() { P1 = "abc" , P2 = 123 },
            new TypeWithPrimitiveProperties() { P1 = "def" , P2 = 456 },
        };
        var deserialized = SerializeAndDeserialize(value, "[{\"P1\":\"abc\",\"P2\":123},{\"P1\":\"def\",\"P2\":456}]");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    #endregion

    #region Collection

    [Fact]
    public static void DCJS_GenericICollectionOfBoolean()
    {
        var value = new TypeImplementsGenericICollection<bool>() { true, false, true };
        var deserialized = SerializeAndDeserialize(value, "[true,false,true]");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_GenericICollectionOfDecimal()
    {
        var value = new TypeImplementsGenericICollection<decimal>() { new decimal(1, 2, 3, false, 1), new decimal(4, 5, 6, true, 2) };
        var deserialized = SerializeAndDeserialize(value, "[5534023222971858944.1,-1106804644637321461.80]");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_GenericICollectionOfInt32()
    {
        TypeImplementsGenericICollection<int> x = new TypeImplementsGenericICollection<int>(123, int.MaxValue, int.MinValue);
        TypeImplementsGenericICollection<int> y = SerializeAndDeserialize(x, "[123,2147483647,-2147483648]");

        Assert.NotNull(y);
        Assert.StrictEqual(x.Count, y.Count);
        Assert.True(x.SequenceEqual(y));
    }

    [Fact]
    public static void DCJS_GenericICollectionOfInt64()
    {
        var value = new TypeImplementsGenericICollection<long>() { 123, long.MaxValue, long.MinValue };
        var deserialized = SerializeAndDeserialize(value, "[123,9223372036854775807,-9223372036854775808]");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_GenericICollectionOfSingle()
    {
        var value = new TypeImplementsGenericICollection<float>() { 1.23f, 4.56f, 7.89f };
        var deserialized = SerializeAndDeserialize(value, "[1.23,4.56,7.89]");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_GenericICollectionOfDouble()
    {
        var value = new TypeImplementsGenericICollection<double>() { 1.23, 4.56, 7.89 };
        var deserialized = SerializeAndDeserialize(value, "[1.23,4.56,7.89]");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCJS_GenericICollectionOfString()
    {
        TypeImplementsGenericICollection<string> value = new TypeImplementsGenericICollection<string>("a1", "a2");
        TypeImplementsGenericICollection<string> deserialized = SerializeAndDeserialize(value, "[\"a1\",\"a2\"]");

        Assert.NotNull(deserialized);
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.True(value.SequenceEqual(deserialized));
    }

    [Fact]
    public static void DCJS_GenericICollectionOfTypeWithPrimitiveProperties()
    {
        var value = new TypeImplementsGenericICollection<TypeWithPrimitiveProperties>()
        {
            new TypeWithPrimitiveProperties() { P1 = "abc" , P2 = 123 },
            new TypeWithPrimitiveProperties() { P1 = "def" , P2 = 456 },
        };
        var deserialized = SerializeAndDeserialize(value, "[{\"P1\":\"abc\",\"P2\":123},{\"P1\":\"def\",\"P2\":456}]");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    #endregion

    #region Generic Dictionary

    [Fact]
    public static void DCJS_GenericDictionaryOfInt32Boolean()
    {
        var value = new Dictionary<int, bool>();
        value.Add(123, true);
        value.Add(456, false);
        var deserialized = SerializeAndDeserialize(value, "[{\"Key\":123,\"Value\":true},{\"Key\":456,\"Value\":false}]");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.ToArray(), deserialized.ToArray()));
    }

    [Fact]
    public static void DCJS_GenericDictionaryOfInt32String()
    {
        var value = new Dictionary<int, string>();
        value.Add(123, "abc");
        value.Add(456, "def");
        var deserialized = SerializeAndDeserialize(value, "[{\"Key\":123,\"Value\":\"abc\"},{\"Key\":456,\"Value\":\"def\"}]");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.ToArray(), deserialized.ToArray()));
    }

    [Fact]
    public static void DCJS_GenericDictionaryOfStringInt32()
    {
        var value = new Dictionary<string, int>();
        value.Add("abc", 123);
        value.Add("def", 456);
        var deserialized = SerializeAndDeserialize(value, "[{\"Key\":\"abc\",\"Value\":123},{\"Key\":\"def\",\"Value\":456}]");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.ToArray(), deserialized.ToArray()));
    }

    #endregion

    #region Non-Generic Dictionary

    [Fact]
    public static void DCJS_NonGenericDictionaryOfInt32Boolean()
    {
        var value = new MyNonGenericDictionary();
        value.Add(123, true);
        value.Add(456, false);
        var deserialized = SerializeAndDeserialize(value, "[{\"Key\":123,\"Value\":true},{\"Key\":456,\"Value\":false}]");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.Keys.Cast<int>().ToArray(), deserialized.Keys.Cast<int>().ToArray()));
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.Values.Cast<bool>().ToArray(), deserialized.Values.Cast<bool>().ToArray()));
    }

    [Fact]
    public static void DCJS_NonGenericDictionaryOfInt32String()
    {
        var value = new MyNonGenericDictionary();
        value.Add(123, "abc");
        value.Add(456, "def");
        var deserialized = SerializeAndDeserialize(value, "[{\"Key\":123,\"Value\":\"abc\"},{\"Key\":456,\"Value\":\"def\"}]");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.Keys.Cast<int>().ToArray(), deserialized.Keys.Cast<int>().ToArray()));
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.Values.Cast<string>().ToArray(), deserialized.Values.Cast<string>().ToArray()));
    }

    [Fact]
    public static void DCJS_NonGenericDictionaryOfStringInt32()
    {
        var value = new MyNonGenericDictionary();
        value.Add("abc", 123);
        value.Add("def", 456);
        var deserialized = SerializeAndDeserialize(value, "[{\"Key\":\"abc\",\"Value\":123},{\"Key\":\"def\",\"Value\":456}]");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.Keys.Cast<string>().ToArray(), deserialized.Keys.Cast<string>().ToArray()));
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.Values.Cast<int>().ToArray(), deserialized.Values.Cast<int>().ToArray()));
    }

    [Fact]
    public static void DCJS_TypeWithEmitDefaultValueFalse()
    {
        var value = new TypeWithEmitDefaultValueFalse();

        var actual = SerializeAndDeserialize(value, "{}");

        Assert.NotNull(actual);
        Assert.Equal(value.Name, actual.Name);
        Assert.Equal(value.ID, actual.ID);
    }

    #endregion

    [Fact]
    public static void DCJS_CreateJsonReaderTest()
    {
        const string json = @"{
                                ""Toy"":""Car"",
                                ""School"": {
                                ""Student"":""Mike""
                                }
                                }";
        byte[] bytes = Encoding.ASCII.GetBytes(json);
        using (var stream = new MemoryStream(bytes))
        {
            var quotas = new XmlDictionaryReaderQuotas();
            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(stream, quotas);
            var xml = XDocument.Load(jsonReader);
            string expected = "<root type=\"object\">\r\n  <Toy type=\"string\">Car</Toy>\r\n  <School type=\"object\">\r\n    <Student type=\"string\">Mike</Student>\r\n  </School>\r\n</root>";
            Utils.CompareResult result = Utils.Compare(expected, xml.ToString());
            Assert.True(result.Equal);
        }
    }

    [Fact]
    public static void DCJS_CreateJsonWriterTest()
    {
        using (var mo = new MemoryStream())
        {
            var p = new Person1();
            p.Name = "David";
            p.Age = 15;
            XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(mo, Encoding.UTF8);
            var serializer = new DataContractJsonSerializer(typeof(Person1));
            var sr = new StreamReader(mo);
            serializer.WriteObject(writer, p);
            writer.Flush();
            mo.Position = 0;
            var output = sr.ReadToEnd();
            string expected = "{\"Age\":15,\"Name\":\"David\"}";
            Utils.CompareResult result = Utils.Compare(expected, output);
            Assert.True(result.Equal);
        }
    }

    private static T SerializeAndDeserialize<T>(T value, string baseline, DataContractJsonSerializerSettings settings = null, Func<DataContractJsonSerializer> serializerFactory = null, bool skipStringCompare = false)
    {
        DataContractJsonSerializer dcjs;
        if (serializerFactory != null)
        {
            dcjs = serializerFactory();
        }
        else
        {
            dcjs = (settings != null) ? new DataContractJsonSerializer(typeof(T), settings) : new DataContractJsonSerializer(typeof(T));
        }

        using (MemoryStream ms = new MemoryStream())
        {
            dcjs.WriteObject(ms, value);
            ms.Position = 0;

            string actualOutput = new StreamReader(ms).ReadToEnd();
            ms.Position = 0;

            if (!skipStringCompare)
            {
                Utils.CompareResult result = Utils.Compare(baseline, actualOutput, false);
                Assert.True(result.Equal, string.Format("{1}{0}Test failed for input: {2}{0}Expected: {3}{0}Actual: {4}",
                    Environment.NewLine, result.ErrorMessage, value, baseline, actualOutput));
            }

            ms.Position = 0;
            T deserialized = (T)dcjs.ReadObject(ms);

            return deserialized;
        }
    }

    private static string s_errorMsg = "The field/property {0} value of deserialized object is wrong";
    private static string getCheckFailureMsg(string propertyName)
    {
        return string.Format(s_errorMsg, propertyName);
    }
}
