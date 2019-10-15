// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class DataTests
    {
        [Fact]
        public static void DataTest1()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<?PI1 processing instruction?><root><?PI2 processinginstruction?></root>");

            var pi1 = (XmlProcessingInstruction)xmlDocument.FirstChild;
            var pi2 = (XmlProcessingInstruction)xmlDocument.DocumentElement.FirstChild;

            Assert.Equal("processing instruction", pi1.Data);
            Assert.Equal("processinginstruction", pi2.Data);

            pi1.Data = "new pi value";
            Assert.Equal("<?PI1 new pi value?><root><?PI2 processinginstruction?></root>", xmlDocument.OuterXml);
        }
    }
}
