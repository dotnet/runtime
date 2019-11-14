// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class NodeList_CountTests
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
