// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class ExtendedDateTimeTests
    {
        [Fact]
        public static void ReadContentAsExtendedDateTime1()
        {
            var reader = Utils.CreateFragmentReader(@"<doc>    00:00:00+00:00   </doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            reader.Read();
            Assert.Equal(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, DateTimeKind.Utc).ToLocalTime(), reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsExtendedDateTime2()
        {
            var reader = Utils.CreateFragmentReader(@"<doc>
    <item><foo></foo></item>
    <item><bar/></item>
    <item><bar>99<!-- Comment inbetween-->99-1<![CDATA[2]]>-31T01:59:5<?a?>9.99<?a?>9999<![CDATA[4]]>9<?Zz?>-00<![CDATA[:]]>00</bar></item>
</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("bar");
            reader.Read();
            Assert.Equal(new DateTime(9999, 12, 31, 1, 59, 59).AddTicks(9999995).Add(TimeZoneInfo.Local.GetUtcOffset(new DateTime(9999, 12, 31))), reader.ReadContentAs(typeof(DateTime), null));
        }

        [Fact]
        public static void ReadContentAsExtendedDateTime3()
        {
            var reader = Utils.CreateFragmentReader(@"<doc>
    <item><foo></foo></item>
    <item><bar/></item>
    <item><bar>99<!-- Comment inbetween-->99-1<![CDATA[2]]>-31T01:59:5<?a?>9.99<?a?>9999<![CDATA[4]]>9<?Zz?>-00<![CDATA[:]]>00</bar></item>
</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("bar");
            reader.Read();
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 1, 59, 59, new TimeSpan(0, 0, 0)).AddTicks(9999995), reader.ReadContentAs(typeof(DateTimeOffset), null));
        }

        [Fact]
        public static void ReadContentAsExtendedDateTime4()
        {
            var reader = Utils.CreateFragmentReader(@"<doc>    00:00:00+00:00   </doc>");
            reader.PositionOnElementNonEmptyNoDoctype("doc");
            reader.Read();
            Assert.Equal(new DateTimeOffset(DateTimeOffset.Now.Year, DateTimeOffset.Now.Month, DateTimeOffset.Now.Day, 0, 0, 0, TimeSpan.FromHours(0)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }

        [Fact]
        public static void ReadContentAsExtendedDateTime5()
        {
            var reader = Utils.CreateFragmentReader(@"<doc>
    <item><foo></foo></item>
    <item><bar/></item>
    <item><bar>99<!-- Comment inbetween-->99-1<![CDATA[2]]>-31T01:59:5<?a?>9.99<?a?>9999<![CDATA[4]]>9<?Zz?>-00<![CDATA[:]]>00</bar></item>
</doc>");
            reader.PositionOnElementNonEmptyNoDoctype("bar");
            reader.Read();
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 1, 59, 59, TimeSpan.FromHours(0)).ToString(), reader.ReadContentAs(typeof(DateTimeOffset), null).ToString());
        }
    }
}
