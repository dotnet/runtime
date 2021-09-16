// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Schema;
using System.Xml.XPath;
using Xunit;

namespace System.Xml.Tests
{
    public class ArrayTests
    {
        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader1()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (int[])reader.ReadContentAs(typeof(int[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);
            Assert.Equal(3, values[2]);
            Assert.Equal(4, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader10()
        {
            var reader = Utils.CreateFragmentReader("<a b='true  false'>true  false</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            reader.Read();
            var values = (bool[])reader.ReadContentAs(typeof(bool[]), null);
            Assert.Equal(2, values.Length);
            Assert.True(values[0]);
            Assert.False(values[1]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader11()
        {
            var reader = Utils.CreateFragmentReader
            (
                @"<a b=' 0002-01-01T00:00:00+00:00
   9998-12-31T12:59:59-00:00 
2000-02-29T23:59:59+13:60'> 0002-01-01T00:00:00+00:00
   9998-12-31T12:59:59-00:00 
2000-02-29T23:59:59+13:60</a>"
            );

            reader.PositionOnElement("a");
            reader.Read();
            var values = (DateTime[])reader.ReadContentAs(typeof(DateTime[]), null);
            Assert.Equal(3, values.Length);

            Assert.Equal
            (
                new DateTime(2, 1, 1, 0, 0, 0)
                    .Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2, 1, 1))),
                values[0]
            );

            Assert.Equal
            (
                new DateTime(9998, 12, 31, 12, 59, 59)
                    .Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(9998, 12, 31))),
                values[1]
            );

            Assert.Equal
            (
                new DateTime(2000, 2, 29, 23, 59, 59)
                    .Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2000, 2, 29))
                         - new TimeSpan(14, 0, 0)),
                values[2]
            );
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader12()
        {
            var reader = Utils.CreateFragmentReader
            (
                @"<a b=' 0002-01-01T00:00:00+00:00
   9998-12-31T12:59:59-00:00 
2000-02-29T23:59:59+13:60'> 0002-01-01T00:00:00+00:00
   9998-12-31T12:59:59-00:00 
2000-02-29T23:59:59+13:60</a>"
            );

            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (DateTime[])reader.ReadContentAs(typeof(DateTime[]), null);
            Assert.Equal(3, values.Length);

            Assert.Equal
            (
                new DateTime(2, 1, 1, 0, 0, 0)
                    .Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2, 1, 1))),
                values[0]
            );

            Assert.Equal
            (
                new DateTime(9998, 12, 31, 12, 59, 59)
                    .Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(9998, 12, 31))),
                values[1]
            );

            Assert.Equal
            (
                new DateTime(2000, 2, 29, 23, 59, 59)
                    .Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2000, 2, 29))
                         - new TimeSpan(14, 0, 0)),
                values[2]
            );
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader13()
        {
            var reader = Utils.CreateFragmentReader
            (
                @"<a b=' 3.14 42  13.75559 -5.743'>
 3.14 42  13.75559 -5.743 </a>"
            );

            reader.PositionOnElement("a");
            reader.Read();
            var values = (decimal[])reader.ReadContentAs(typeof(decimal[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(3.14M, values[0]);
            Assert.Equal(42M, values[1]);
            Assert.Equal(13.75559M, values[2]);
            Assert.Equal(-5.743M, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader14()
        {
            var reader = Utils.CreateFragmentReader
            (
                @"<a b=' 3.14 42  13.75559 -5.743'>
 3.14 42  13.75559 -5.743 </a>"
            );

            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (decimal[])reader.ReadContentAs(typeof(decimal[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(3.14M, values[0]);
            Assert.Equal(42M, values[1]);
            Assert.Equal(13.75559M, values[2]);
            Assert.Equal(-5.743M, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader15()
        {
            var reader = Utils.CreateFragmentReader
            (
                @"<a b=' 3.14 42  13.75559 -5.743'>
 3.14 42  13.75559 -5.743 </a>"
            );

            reader.PositionOnElement("a");
            reader.Read();
            var values = (double[])reader.ReadContentAs(typeof(double[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(3.14, values[0]);
            Assert.Equal(42, values[1]);
            Assert.Equal(13.75559, values[2]);
            Assert.Equal(-5.743, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader16()
        {
            var reader = Utils.CreateFragmentReader
            (
                @"<a b=' 3.14 42  13.75559 -5.743'>
 3.14 42  13.75559 -5.743 </a>"
            );

            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (double[])reader.ReadContentAs(typeof(double[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(3.14, values[0]);
            Assert.Equal(42, values[1]);
            Assert.Equal(13.75559, values[2]);
            Assert.Equal(-5.743, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader17()
        {
            var reader = Utils.CreateFragmentReader
            (
                @"<a b=' 36 442  19 -5743'>
  36 442  19 -5743</a>"
            );

            reader.PositionOnElement("a");
            reader.Read();
            var values = (short[])reader.ReadContentAs(typeof(short[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(36, values[0]);
            Assert.Equal(442, values[1]);
            Assert.Equal(19, values[2]);
            Assert.Equal(-5743, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader18()
        {
            var reader = Utils.CreateFragmentReader
            (
                @"<a b=' 36 442  19 -5743'>
  36 442  19 -5743</a>"
            );

            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (short[])reader.ReadContentAs(typeof(short[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(36, values[0]);
            Assert.Equal(442, values[1]);
            Assert.Equal(19, values[2]);
            Assert.Equal(-5743, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader19()
        {
            var reader = Utils.CreateFragmentReader
            (
                @"<a b=' 36 442  -39 9223372036854775807'>
  36 442  -39 9223372036854775807</a>"
            );

            reader.PositionOnElement("a");
            reader.Read();
            var values = (long[])reader.ReadContentAs(typeof(long[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(36L, values[0]);
            Assert.Equal(442L, values[1]);
            Assert.Equal(-39L, values[2]);
            Assert.Equal(9223372036854775807L, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader2()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (int[])reader.ReadContentAs(typeof(int[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);
            Assert.Equal(3, values[2]);
            Assert.Equal(4, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader20()
        {
            var reader = Utils.CreateFragmentReader
            (
                @"<a b=' 36 442  -39 9223372036854775807'>
  36 442  -39 9223372036854775807</a>"
            );

            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (long[])reader.ReadContentAs(typeof(long[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(36L, values[0]);
            Assert.Equal(442L, values[1]);
            Assert.Equal(-39L, values[2]);
            Assert.Equal(9223372036854775807L, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader21()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (sbyte[])reader.ReadContentAs(typeof(sbyte[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);
            Assert.Equal(3, values[2]);
            Assert.Equal(4, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader22()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (sbyte[])reader.ReadContentAs(typeof(sbyte[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);
            Assert.Equal(3, values[2]);
            Assert.Equal(4, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader23()
        {
            var reader = Utils.CreateFragmentReader
            (
                @"<a b=' 3.14 42  13.75559 -5.743'>
 3.14 42  13.75559 -5.743 </a>"
            );

            reader.PositionOnElement("a");
            reader.Read();
            var values = (float[])reader.ReadContentAs(typeof(float[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(3.14F, values[0]);
            Assert.Equal(42F, values[1]);
            Assert.Equal(13.75559F, values[2]);
            Assert.Equal(-5.743F, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader24()
        {
            var reader = Utils.CreateFragmentReader
            (
                @"<a b=' 3.14 42  13.75559 -5.743'>
 3.14 42  13.75559 -5.743 </a>"
            );

            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (float[])reader.ReadContentAs(typeof(float[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(3.14F, values[0]);
            Assert.Equal(42F, values[1]);
            Assert.Equal(13.75559F, values[2]);
            Assert.Equal(-5.743F, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader25()
        {
            var reader = Utils.CreateFragmentReader("<a b=' PT2M10S PT130S'> PT2M10S PT130S</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (TimeSpan[])reader.ReadContentAs(typeof(TimeSpan[]), null);
            Assert.Equal(2, values.Length);
            Assert.Equal(new TimeSpan(0, 0, 2, 10), values[0]);

            Assert.Equal
            (
                new TimeSpan(0, 0, 0, 130, 0),
                values[1]
            );
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader26()
        {
            var reader = Utils.CreateFragmentReader("<a b=' PT2M10S PT130S'> PT2M10S PT130S</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (TimeSpan[])reader.ReadContentAs(typeof(TimeSpan[]), null);
            Assert.Equal(2, values.Length);
            Assert.Equal(new TimeSpan(0, 0, 2, 10), values[0]);

            Assert.Equal
            (
                new TimeSpan(0, 0, 0, 130, 0),
                values[1]
            );
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader27()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (ushort[])reader.ReadContentAs(typeof(ushort[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);
            Assert.Equal(3, values[2]);
            Assert.Equal(4, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader28()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (ushort[])reader.ReadContentAs(typeof(ushort[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);
            Assert.Equal(3, values[2]);
            Assert.Equal(4, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader29()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (uint[])reader.ReadContentAs(typeof(uint[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal((uint)1, values[0]);
            Assert.Equal((uint)2, values[1]);
            Assert.Equal((uint)3, values[2]);
            Assert.Equal((uint)4, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader3()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (object[])reader.ReadContentAs(typeof(string[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
            Assert.Equal("3", values[2]);
            Assert.Equal("4", values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader30()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (uint[])reader.ReadContentAs(typeof(uint[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal((uint)1, values[0]);
            Assert.Equal((uint)2, values[1]);
            Assert.Equal((uint)3, values[2]);
            Assert.Equal((uint)4, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader31()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (ulong[])reader.ReadContentAs(typeof(ulong[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal((ulong)1, values[0]);
            Assert.Equal((ulong)2, values[1]);
            Assert.Equal((ulong)3, values[2]);
            Assert.Equal((ulong)4, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader32()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (ulong[])reader.ReadContentAs(typeof(ulong[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal((ulong)1, values[0]);
            Assert.Equal((ulong)2, values[1]);
            Assert.Equal((ulong)3, values[2]);
            Assert.Equal((ulong)4, values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader33()
        {
            var reader = Utils.CreateFragmentReader
            (
                "<a b='https://github.com/dotnet/wpf  https://sharplab.io/'>https://github.com/dotnet/wpf  https://sharplab.io/</a>"
            );

            reader.PositionOnElement("a");
            reader.Read();
            var values = (Uri[])reader.ReadContentAs(typeof(Uri[]), null);
            Assert.Equal(2, values.Length);
            Assert.Equal(new Uri("https://github.com/dotnet/wpf"), values[0]);
            Assert.Equal(new Uri("https://sharplab.io/"), values[1]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader34()
        {
            var reader = Utils.CreateFragmentReader
            (
                "<a b='https://github.com/dotnet/wpf  https://sharplab.io/'>https://github.com/dotnet/wpf  https://sharplab.io/</a>"
            );

            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (Uri[])reader.ReadContentAs(typeof(Uri[]), null);
            Assert.Equal(2, values.Length);
            Assert.Equal(new Uri("https://github.com/dotnet/wpf"), values[0]);
            Assert.Equal(new Uri("https://sharplab.io/"), values[1]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader35()
        {
            var reader = Utils.CreateFragmentReader("<a b='xmlns:os  xmlns:a'>xmlns:os xmlns:a</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (XmlQualifiedName[])reader.ReadContentAs(typeof(XmlQualifiedName[]), null);
            Assert.Equal(2, values.Length);
            Assert.Equal(new XmlQualifiedName("os", "http://www.w3.org/2000/xmlns/"), values[0]);
            Assert.Equal(new XmlQualifiedName("a", "http://www.w3.org/2000/xmlns/"), values[1]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader36()
        {
            var reader = Utils.CreateFragmentReader("<a b='xmlns:os  xmlns:a'>xmlns:os xmlns:a</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (XmlQualifiedName[])reader.ReadContentAs(typeof(XmlQualifiedName[]), null);
            Assert.Equal(2, values.Length);
            Assert.Equal(new XmlQualifiedName("os", "http://www.w3.org/2000/xmlns/"), values[0]);
            Assert.Equal(new XmlQualifiedName("a", "http://www.w3.org/2000/xmlns/"), values[1]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader37()
        {
            var reader = Utils.CreateFragmentReader("<a b='a  true 16 42 .555 0002-01-01T00:00:00+00:00'>sdf</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (XmlAtomicValue[])reader.ReadContentAs(typeof(XmlAtomicValue[]), null);
            Assert.Equal(6, values.Length);
            Assert.Equal("a", values[0].Value);
            Assert.True(values[1].ValueAsBoolean);
            Assert.Equal(16, values[2].ValueAsInt);
            Assert.Equal(42L, values[3].ValueAsLong);
            Assert.Equal(.555, values[4].ValueAsDouble);

            Assert.Equal
            (
                new DateTime(2, 1, 1).Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2, 1, 1))),
                values[5].ValueAsDateTime
            );
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader38()
        {
            var reader = Utils.CreateFragmentReader("<a b='a  true 16 42 .555 0002-01-01T00:00:00+00:00'>sdf</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (XPathItem[])reader.ReadContentAs(typeof(XPathItem[]), null);
            Assert.Equal(6, values.Length);
            Assert.Equal("a", values[0].Value);
            Assert.True(values[1].ValueAsBoolean);
            Assert.Equal(16, values[2].ValueAsInt);
            Assert.Equal(42L, values[3].ValueAsLong);
            Assert.Equal(.555, values[4].ValueAsDouble);

            Assert.Equal
            (
                new DateTime(2, 1, 1).Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2, 1, 1))),
                values[5].ValueAsDateTime
            );
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader4()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (object[])reader.ReadContentAs(typeof(string[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
            Assert.Equal("3", values[2]);
            Assert.Equal("4", values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader5()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (object[])reader.ReadContentAs(typeof(string[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
            Assert.Equal("3", values[2]);
            Assert.Equal("4", values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader6()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (object[])reader.ReadContentAs(typeof(string[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
            Assert.Equal("3", values[2]);
            Assert.Equal("4", values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader7()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (object[])reader.ReadContentAs(typeof(object[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
            Assert.Equal("3", values[2]);
            Assert.Equal("4", values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader8()
        {
            var reader = Utils.CreateFragmentReader("<a b='1  2 3 4'>1  2 3 4</a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            var values = (object[])reader.ReadContentAs(typeof(object[]), null);
            Assert.Equal(4, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
            Assert.Equal("3", values[2]);
            Assert.Equal("4", values[3]);
        }

        [Fact]
        public static void DeserializationOfTypedArraysByXmlReader9()
        {
            var reader = Utils.CreateFragmentReader("<a b='true  false'>true false</a>");
            reader.PositionOnElement("a");
            reader.Read();
            var values = (bool[])reader.ReadContentAs(typeof(bool[]), null);
            Assert.Equal(2, values.Length);
            Assert.True(values[0]);
            Assert.False(values[1]);
        }
    }
}
