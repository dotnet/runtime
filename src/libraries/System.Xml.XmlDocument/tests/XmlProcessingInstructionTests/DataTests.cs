// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlProcessingInstructionTests
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
