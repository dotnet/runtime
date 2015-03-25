// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XMLTests.ReaderWriter.ReadContentTests
{
    public class TimeZoneInfoTests
    {
        [Fact]
        public static void ReadContentAsTimeZoneInfo1()
        {
            var reader = Utils.CreateFragmentReader("<a>2000-02-29T23:59:59+13:60</a>");
            reader.PositionOnElement("a");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(TimeZoneInfo), null));
        }
    }
}