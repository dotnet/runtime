// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XMLTests.ReaderWriter.ReadContentTests
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