// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class TargetTests
    {
        [Fact]
        public static void TargetTest1()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<?PI1 processing instruction?> <root> <?PI2 processing instruction2?> </root>");

            Assert.Equal("PI1", ((XmlProcessingInstruction)xmlDocument.FirstChild).Target);
            Assert.Equal("PI2", ((XmlProcessingInstruction)xmlDocument.DocumentElement.FirstChild).Target);
        }
    }
}
