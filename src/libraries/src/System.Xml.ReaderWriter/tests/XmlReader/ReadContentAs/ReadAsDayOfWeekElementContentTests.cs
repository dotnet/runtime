// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XMLTests.ReaderWriter.ReadContentTests
{
    public class DayOfWeekElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsDayOfWeek1()
        {
            var reader = Utils.CreateFragmentReader("<a>2000-02-29T23:59:59+13:60</a>");
            reader.PositionOnElement("a");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(DayOfWeek), null));
        }
    }
}