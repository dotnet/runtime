// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Runtime.Serialization;
using Xunit;
using System.Text.Json.Serialization;

namespace System.Text.Json.Tests
{
    public class EnumConverterTests
    {
        [Fact]
        public static void SerializeEnumClass()
        {
            EnumClass enumClass = new EnumClass()
            {
                StoreColor = StoreColor.Red,
                NullableStoreColor1 = StoreColor.White,
                NullableStoreColor2 = null
            };

            string json = JsonSerializer.Serialize(enumClass, new JsonSerializerOptions { WriteIndented = true });

            Assert.Equal(@"{
  ""StoreColor"": 2,
  ""NullableStoreColor1"": 8,
  ""NullableStoreColor2"": null
}".NormalizeLineEndings(), json);
        }

        [Fact]
        public static void SerializeEnumClassUndefined()
        {
            EnumClass enumClass = new EnumClass()
            {
                StoreColor = (StoreColor)1000,
                NullableStoreColor1 = (StoreColor)1000,
                NullableStoreColor2 = null
            };

            string json = JsonSerializer.Serialize(enumClass, new JsonSerializerOptions { WriteIndented = true });

            Assert.Equal(@"{
  ""StoreColor"": 1000,
  ""NullableStoreColor1"": 1000,
  ""NullableStoreColor2"": null
}".NormalizeLineEndings(), json);
        }

        [Fact]
        public static void SerializeFlagEnum()
        {
            EnumClass enumClass = new EnumClass()
            {
                StoreColor = StoreColor.Red | StoreColor.White,
                NullableStoreColor1 = StoreColor.White & StoreColor.Yellow,
                NullableStoreColor2 = StoreColor.Red | StoreColor.White | StoreColor.Black
            };

            string json = JsonSerializer.Serialize(enumClass, new JsonSerializerOptions { WriteIndented = true });

            Assert.Equal(@"{
  ""StoreColor"": 10,
  ""NullableStoreColor1"": 0,
  ""NullableStoreColor2"": 11
}".NormalizeLineEndings(), json);
        }

        [Fact]
        public static void SerializeNegativeFlagsEnum()
        {
            NegativeFlagsEnumClass negativeEnumClass = new NegativeFlagsEnumClass();
            negativeEnumClass.Value1 = NegativeFlagsEnum.NegativeFour | NegativeFlagsEnum.NegativeTwo;
            negativeEnumClass.Value2 = NegativeFlagsEnum.Two | NegativeFlagsEnum.Four;

            string json = JsonSerializer.Serialize(negativeEnumClass, new JsonSerializerOptions { WriteIndented = true });

            Assert.Equal(@"{
  ""Value1"": -2,
  ""Value2"": 6
}".NormalizeLineEndings(), json);
        }

        [Fact]
        public static void SerializeNegativeEnum()
        {
            NegativeEnumClass negativeEnumClass = new NegativeEnumClass()
            {
                Value1 = NegativeEnum.Negative,
                Value2 = (NegativeEnum)int.MinValue
            };

            string json = JsonSerializer.Serialize(negativeEnumClass, new JsonSerializerOptions { WriteIndented = true });

            Assert.Equal(@"{
  ""Value1"": -1,
  ""Value2"": -2147483648
}".NormalizeLineEndings(), json);
        }

        [Fact]
        public static void EnumMemberPlusFlags()
        {
            List<Foo> lfoo =
                new List<Foo>
                {
                    Foo.Bat | Foo.SerializeAsBaz,
                    Foo.FooBar,
                    Foo.Bat,
                    Foo.SerializeAsBaz,
                    Foo.FooBar | Foo.SerializeAsBaz,
                    (Foo)int.MaxValue
                };

            string json1 = JsonSerializer.Serialize(lfoo, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Assert.Equal(@"[
  6,
  1,
  2,
  4,
  5,
  2147483647
]".NormalizeLineEndings(), json1);

            IList<Foo> foos = JsonSerializer.Deserialize<List<Foo>>(json1);

            Assert.Equal(6, foos.Count);
            Assert.Equal(Foo.Bat | Foo.SerializeAsBaz, foos[0]);
            Assert.Equal(Foo.FooBar, foos[1]);
            Assert.Equal(Foo.Bat, foos[2]);
            Assert.Equal(Foo.SerializeAsBaz, foos[3]);
            Assert.Equal(Foo.FooBar | Foo.SerializeAsBaz, foos[4]);
            Assert.Equal((Foo)int.MaxValue, foos[5]);

            List<Bar> lbar = new List<Bar>() { Bar.FooBar, Bar.Bat, Bar.SerializeAsBaz };

            string json2 = JsonSerializer.Serialize(lbar, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Assert.Equal(@"[
  0,
  1,
  2
]".NormalizeLineEndings(), json2);

            IList<Bar> bars = JsonSerializer.Deserialize<List<Bar>>(json2);

            Assert.Equal(3, bars.Count);
            Assert.Equal(Bar.FooBar, bars[0]);
            Assert.Equal(Bar.Bat, bars[1]);
            Assert.Equal(Bar.SerializeAsBaz, bars[2]);
        }

        [Fact]
        public static void TestValidIntegerValue()
        {
            StoreColor c = JsonSerializer.Deserialize<StoreColor>("1");
            Assert.Equal(StoreColor.Black, c);
        }

        [Fact]
        public static void DuplicateNameEnumTest()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DuplicateNameEnum>("\"foo_bar\""));
        }

        [Fact]
        public static void InvalidValueStringNumber()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StoreColor>("\"1\""));
        }

        [Fact]
        public static void SerializeEnumWithDifferentCases()
        {
            string json = JsonSerializer.Serialize(EnumWithDifferentCases.M);

            Assert.Equal("0", json);

            json = JsonSerializer.Serialize(EnumWithDifferentCases.m);

            Assert.Equal("1", json);
        }

        [Fact]
        public static void DeserializeEnumWithDifferentCases()
        {
            EnumWithDifferentCases e = JsonSerializer.Deserialize<EnumWithDifferentCases>("0");
            Assert.Equal(EnumWithDifferentCases.M, e);

            e = JsonSerializer.Deserialize<EnumWithDifferentCases>("1");
            Assert.Equal(EnumWithDifferentCases.m, e);
        }

        [Fact]
        public static void SerializeEnumMemberWithDifferentCases()
        {
            string json = JsonSerializer.Serialize(EnumMemberWithDifferentCases.Month);

            Assert.Equal("0", json);

            json = JsonSerializer.Serialize(EnumMemberWithDifferentCases.Minute);

            Assert.Equal("1", json);
        }

        [Fact]
        public static void InvalidValueDash()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StoreColor>("\"-\""));
        }

        [Fact]
        public static void DeserializeNegativeEnum()
        {
            string json = @"{
  ""Value1"": -1,
  ""Value2"": -2147483648
}";
            NegativeEnumClass negativeEnumClass = JsonSerializer.Deserialize<NegativeEnumClass>(json);
            Assert.Equal(NegativeEnum.Negative, negativeEnumClass.Value1);
            Assert.Equal((NegativeEnum)int.MinValue, negativeEnumClass.Value2);
        }

        [Fact]
        public static void DeserializeEnumClass()
        {
            string json = @"{
  ""StoreColor"": 2,
  ""NullableStoreColor1"": 8,
  ""NullableStoreColor2"": null
}";
            EnumClass enumClass = JsonSerializer.Deserialize<EnumClass>(json);
            Assert.Equal(StoreColor.Red, enumClass.StoreColor);
            Assert.Equal(StoreColor.White, enumClass.NullableStoreColor1);
            Assert.Null(enumClass.NullableStoreColor2);
        }

        [Fact]
        public static void DeserializeFlagEnum()
        {
            string json = @"{
  ""StoreColor"": 10,
  ""NullableStoreColor1"": 0,
  ""NullableStoreColor2"": 11
}";
            EnumClass enumClass = JsonSerializer.Deserialize<EnumClass>(json);
            Assert.Equal(StoreColor.Red | StoreColor.White, enumClass.StoreColor);
            Assert.Equal((StoreColor)0, enumClass.NullableStoreColor1);
            Assert.Equal(StoreColor.Red | StoreColor.White | StoreColor.Black, enumClass.NullableStoreColor2);
        }
    }

    [Flags]
    internal enum Foo
    {
        FooBar = 0x01,
        Bat = 0x02,
        SerializeAsBaz = 0x4,
    }

    internal enum Bar
    {
        FooBar,
        Bat,
        SerializeAsBaz
    }

    internal enum NegativeEnum
    {
        Negative = -1,
        Zero = 0,
        Positive = 1
    }

    [Flags]
    internal enum NegativeFlagsEnum
    {
        NegativeFour = -4,
        NegativeTwo = -2,
        NegativeOne = -1,
        Zero = 0,
        One = 1,
        Two = 2,
        Four = 4
    }

    internal enum EnumWithDifferentCases
    {
        M,
        m
    }

    internal enum EnumMemberWithDifferentCases
    {
        Month,
        Minute
    }

    internal class NegativeEnumClass
    {
        public NegativeEnum Value1 { get; set; }
        public NegativeEnum Value2 { get; set; }
    }

    internal class NegativeFlagsEnumClass
    {
        public NegativeFlagsEnum Value1 { get; set; }
        public NegativeFlagsEnum Value2 { get; set; }
    }

    [Flags]
    internal enum StoreColor
    {
        Black = 1,
        Red = 2,
        Yellow = 4,
        White = 8,
        DarkGoldenrod = 16
    }

    internal class EnumClass
    {
        public StoreColor StoreColor { get; set; }
        public StoreColor NullableStoreColor1 { get; set; }
        public StoreColor? NullableStoreColor2 { get; set; }
    }

    internal enum DuplicateNameEnum
    {
        [EnumMember]
        first = 0,

        [EnumMember]
        foo_bar = 1,

        [EnumMember(Value = "foo_bar")]
        FooBar = 2,

        [EnumMember]
        foo_bar_NOT_USED = 3
    }
}
