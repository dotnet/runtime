// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class UriTests
    {
        [Fact]
        public static void ReadContentAsTypeOfUri1()
        {
            var reader = Utils.CreateFragmentReader("<a b=' http://wddata '> http://wddata </a>");
            reader.PositionOnElement("a");
            reader.Read();
            Assert.Equal(new Uri("http://wddata/"), reader.ReadContentAs(typeof(Uri), null));
        }

        [Fact]
        public static void ReadContentAsTypeOfUri2()
        {
            var reader = Utils.CreateFragmentReader("<a b='    '>    </a>");
            reader.PositionOnElement("a");
            reader.Read();
            Assert.Throws<XmlException>(() => reader.ReadContentAs(typeof(Uri), null));
        }
    }
}
