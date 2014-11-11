// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlNodeListTests
{
    class CountTests
    {
        [Fact]
        public static void CountTest1()
        {
            var xd = new XmlDocument();
            xd.LoadXml("<a><sub1/><sub2/></a>");

            Assert.Equal(2, xd.DocumentElement.ChildNodes.Count);
        }
    }
}
