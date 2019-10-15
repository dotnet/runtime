// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class TimeSpanElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsTimeSpan1()
        {
            var reader = Utils.CreateFragmentReader("<a>2000-02-29T23:59:59+13:60</a>");
            reader.PositionOnElement("a");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(TimeSpan), null));
        }

        [Fact]
        public static void ReadElementContentAsTimeSpan2()
        {
            var reader = Utils.CreateFragmentReader("<Root>  P1067<!-- Comment inbetween-->51<![CDATA[9]]><![CDATA[9]]>DT2H48M5.4<?a?>775807S  </Root>");
            reader.PositionOnElement("Root");
            Assert.Equal("10675199.02:48:05.4775807", reader.ReadElementContentAs(typeof(TimeSpan), null).ToString());
        }
    }
}
