// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Xml.Tests
{
    public class UriElementContentTests
    {
        [Fact]
        public static void ReadElementContentAsTypeOfUri1()
        {
            var reader = Utils.CreateFragmentReader("<a b=' http://wddata '> http://wddata </a>");
            reader.PositionOnElement("a");
            Assert.Equal(new Uri("http://wddata/"), reader.ReadElementContentAs(typeof(Uri), null));
        }

        [Fact]
        public static void ReadElementContentAsTypeOfUri2()
        {
            var reader = Utils.CreateFragmentReader("<a b='    '>    </a>");
            reader.PositionOnElement("a");
            Assert.Throws<XmlException>(() => reader.ReadElementContentAs(typeof(Uri), null));
        }
    }
}