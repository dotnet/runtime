// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Xunit;

namespace System.Private.Xml.Tests
{
    public static class XmlSerializationEmptyElementTests
    {
        [Fact]
        public static void XmlElement_EmptyElement_DoesNotConsumeSubsequentElements()
        {
            var serializer = new XmlSerializer(typeof(RootWithXmlElement));

            // Test case with empty description element followed by name element
            var xml = @"<root><description></description><name>Test</name></root>";
            var result = (RootWithXmlElement)serializer.Deserialize(new StringReader(xml));

            // The Description should be an empty element (not null, not consuming the Name element)
            Assert.NotNull(result.Description);
            Assert.Equal("description", result.Description.LocalName);
            Assert.True(string.IsNullOrEmpty(result.Description.InnerXml));
            Assert.True(result.Description.IsEmpty || result.Description.InnerXml == "");
            
            // The Name should still be correctly deserialized
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public static void XmlElement_NonEmptyElement_WorksCorrectly()
        {
            var serializer = new XmlSerializer(typeof(RootWithXmlElement));

            // Test case with non-empty description element
            var xml = @"<root><description><p>test</p></description><name>Test</name></root>";
            var result = (RootWithXmlElement)serializer.Deserialize(new StringReader(xml));

            // The Description should contain the paragraph
            Assert.NotNull(result.Description);
            Assert.Equal("description", result.Description.LocalName);
            Assert.Equal("<p>test</p>", result.Description.InnerXml);
            
            // The Name should still be correctly deserialized
            Assert.Equal("Test", result.Name);
        }
        
        [Fact]
        public static void XmlElement_EmptyElement_BehaviorConsistentWithNonEmpty()
        {
            var serializer = new XmlSerializer(typeof(RootWithXmlElement));

            // Empty element case
            var xml1 = @"<root><description></description><name>Test</name></root>";
            var result1 = (RootWithXmlElement)serializer.Deserialize(new StringReader(xml1));

            // Self-closing element case (equivalent to empty)
            var xml2 = @"<root><description/><name>Test</name></root>";
            var result2 = (RootWithXmlElement)serializer.Deserialize(new StringReader(xml2));

            // Both should behave the same way
            Assert.Equal(result1.Description.InnerXml, result2.Description.InnerXml);
            Assert.Equal(result1.Name, result2.Name);
            Assert.Equal("Test", result1.Name);
            Assert.Equal("Test", result2.Name);
            
            // Both should have empty description elements
            Assert.NotNull(result1.Description);
            Assert.NotNull(result2.Description);
            Assert.Equal("description", result1.Description.LocalName);
            Assert.Equal("description", result2.Description.LocalName);
            Assert.True(string.IsNullOrEmpty(result1.Description.InnerXml));
            Assert.True(string.IsNullOrEmpty(result2.Description.InnerXml));
        }

        [Fact]  
        public static void XmlElement_EmptyElement_ShouldNotBeNull()
        {
            var serializer = new XmlSerializer(typeof(RootWithXmlElement));

            // Test that empty elements create XmlElement objects, not null
            var xml = @"<root><description></description><name>Test</name></root>";
            var result = (RootWithXmlElement)serializer.Deserialize(new StringReader(xml));

            // Description should not be null - it should be an empty XmlElement
            Assert.NotNull(result.Description);
            Assert.Equal("description", result.Description.LocalName);
            Assert.Equal("", result.Description.InnerXml);
            Assert.Equal("Test", result.Name);
        }

        public class RootWithXmlElement
        {
            public XmlElement Description { get; set; }
            public string Name { get; set; }
        }
    }
}