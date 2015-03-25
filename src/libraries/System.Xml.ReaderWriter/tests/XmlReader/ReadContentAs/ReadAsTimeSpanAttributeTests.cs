// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XMLTests.ReaderWriter.ReadContentTests
{
    public class TimeSpanAttributeTests
    {
        [Fact]
        public static void ReadContentAsTimeSpanAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<Root a='   PT0S  '/>");
            reader.PositionOnElement("Root");
            reader.MoveToAttribute("a");
            Assert.Equal("00:00:00", reader.ReadContentAs(typeof(TimeSpan), null).ToString());
        }
    }
}