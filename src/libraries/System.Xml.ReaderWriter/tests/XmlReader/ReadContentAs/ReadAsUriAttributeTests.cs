// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XMLTests.ReaderWriter.ReadContentTests
{
    public class UriAttributeTests
    {
        [Fact]
        public static void ReadContentAsTypeOfUriAttribute1()
        {
            var reader = Utils.CreateFragmentReader("<a b=' http://wddata '> http://wddata </a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            Assert.Equal(new Uri("http://wddata/"), reader.ReadContentAs(typeof(Uri), null));
        }

        [Fact]
        public static void ReadContentAsTypeOfUriAttribute2()
        {
            var reader = Utils.CreateFragmentReader("<a b='    '>    </a>");
            reader.PositionOnElement("a");
            reader.MoveToAttribute("b");
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(Uri), null));
        }
    }
}