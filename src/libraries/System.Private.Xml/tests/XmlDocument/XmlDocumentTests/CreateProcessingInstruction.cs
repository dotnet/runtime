// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Xml.XmlDocumentTests
{
    public class CreateProcessingInstruction
    {
        [Theory]
        [InlineData("bar", "foo", "<?bar foo?>")]
        [InlineData("bar", "", "<?bar ?>")]
        [InlineData("bar", null, "<?bar ?>")]
        [InlineData("foo.bar", null, "<?foo.bar ?>")]
        [InlineData("foo:bar", null, "<?foo:bar ?>")]
        public static void ProcessingInstructionCanBeCreatedAndSerialized(string target, string? data, string expectedOutput)
        {
            var xmlDocument = new XmlDocument();
            var newNode = xmlDocument.CreateProcessingInstruction(target, data);

            Assert.Equal(expectedOutput, newNode.OuterXml);
            Assert.Equal(XmlNodeType.ProcessingInstruction, newNode.NodeType);
        }

        [Fact]
        public static void NullTargetThrows()
        {
            var xmlDocument = new XmlDocument();
            Assert.Throws<ArgumentNullException>(() => xmlDocument.CreateProcessingInstruction(null, "anyData"));
        }

        [Fact]
        public static void EmptyTargetThrows()
        {
            var xmlDocument = new XmlDocument();
            Assert.Throws<ArgumentException>(() => xmlDocument.CreateProcessingInstruction("", "anyData"));
        }
    }
}
