// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class SyndicationContentTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var content = new SyndicationContentSubclass();
            Assert.Empty(content.AttributeExtensions);
        }

        [Fact]
        public void Ctor_SyndicationCategory_WithNoAttributeExtensions()
        {
            var content = new SyndicationContentSubclass();

            SyndicationContentSubclass clone = new SyndicationContentSubclass(content);
            Assert.Equal(0, clone.AttributeExtensions.Count);
        }

        [Fact]
        public void Ctor_SyndicationCategory_WithAttributeExtensions()
        {
            var content = new SyndicationContentSubclass();
            content.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");

            SyndicationContentSubclass clone = new SyndicationContentSubclass(content);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);
        }

        [Fact]
        public void Ctor_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => new SyndicationContentSubclass(null));
        }

        public static IEnumerable<object[]> WriteTo_TestData()
        {
            yield return new object[] { null, "OuterName", null, @"<OuterName type=""CustomType"" name=""value"" />" };
            yield return new object[]
            {
                new Dictionary<XmlQualifiedName, string>
                {
                    { new XmlQualifiedName("type", "namespace"), "value1" },
                    { new XmlQualifiedName("type"), "value2" },
                    { new XmlQualifiedName("name1"), "value3" },
                    { new XmlQualifiedName("name2"), "" },
                    { new XmlQualifiedName("name3"), null }
                },
                "OuterName", "OuterNamespace",
                @"<OuterName type=""CustomType"" d1p1:type=""value1"" name1=""value3"" name2="""" name3="""" name=""value"" xmlns:d1p1=""namespace"" xmlns=""OuterNamespace"" />"
            };
        }

        [Theory]
        [MemberData(nameof(WriteTo_TestData))]
        public void WriteTo_Invoke_Success(Dictionary<XmlQualifiedName, string> attributeExtensions, string outerElementName, string outerElementNamespace, string expected)
        {
            var content = new SyndicationContentSubclass();
            if (attributeExtensions != null)
            {
                foreach (XmlQualifiedName name in attributeExtensions.Keys)
                {
                    content.AttributeExtensions.Add(name, attributeExtensions[name]);
                }
            }
            CompareHelper.AssertEqualWriteOutput(expected, writer => content.WriteTo(writer, outerElementName, outerElementNamespace));
        }

        [Fact]
        public void WriteTo_NullWriter_ThrowsArgumentNullException()
        {
            var content = new SyndicationContentSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => content.WriteTo(null, "outerElementName", "outerElementNamespace"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void WriteTo_EmptyOrNullName_ThrowsArgumentException(string outerElementName)
        {
            var content = new SyndicationContentSubclass();
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentException>("outerElementName", null, () => content.WriteTo(writer, outerElementName, "outerElementNamespace"));
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("content")]
        public void CreateHtmlContent_Invoke_ReturnsExpected(string content)
        {
            TextSyndicationContent syndicationContent = SyndicationContent.CreateHtmlContent(content);
            Assert.Empty(syndicationContent.AttributeExtensions);
            Assert.Equal(content, syndicationContent.Text);
            Assert.Equal("html", syndicationContent.Type);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("content")]
        public void CreatePlainContent_Invoke_ReturnsExpected(string content)
        {
            TextSyndicationContent syndicationContent = SyndicationContent.CreatePlaintextContent(content);
            Assert.Empty(syndicationContent.AttributeExtensions);
            Assert.Equal(content, syndicationContent.Text);
            Assert.Equal("text", syndicationContent.Type);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("content")]
        public void CreateXhtmlContent_Invoke_ReturnsExpected(string content)
        {
            TextSyndicationContent syndicationContent = SyndicationContent.CreateXhtmlContent(content);
            Assert.Empty(syndicationContent.AttributeExtensions);
            Assert.Equal(content, syndicationContent.Text);
            Assert.Equal("xhtml", syndicationContent.Type);
        }

        public static IEnumerable<object[]> CreateUrlContent_TestData()
        {
            yield return new object[] { new Uri("http://microsoft.com"), null };
            yield return new object[] { new Uri("/relative", UriKind.Relative), "" };
            yield return new object[] { new Uri("http://microsoft.com"), "mediaType" };
        }

        [Theory]
        [MemberData(nameof(CreateUrlContent_TestData))]
        public void CreateUrlContent_Invoke_ReturnsExpected(Uri url, string mediaType)
        {
            UrlSyndicationContent syndicationContent = SyndicationContent.CreateUrlContent(url, mediaType);
            Assert.Empty(syndicationContent.AttributeExtensions);
            Assert.Equal(url, syndicationContent.Url);
            Assert.Equal(mediaType, syndicationContent.Type);
        }

        [Fact]
        public void CreateUrlContent_NullUrl_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("url", () => SyndicationContent.CreateUrlContent(null, "mediaType"));
        }

        [Fact]
        public void CreateXmlContent_Reader_NoAttributes()
        {
            XmlSyndicationContent content = SyndicationContent.CreateXmlContent(
                new XElement("ParentObject",
                    new XElement("ExtensionObject",
                        new XElement("Value", 10)
                    )
                ).CreateReader()
            );
            Assert.Empty(content.AttributeExtensions);
            Assert.Equal("text/xml", content.Type);
            Assert.Null(content.Extension);
            Assert.Equal(0, content.ReadContent<ExtensionObject>().Value);
            Assert.Equal(0, content.ReadContent<ExtensionObject>(new DataContractSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(0, content.ReadContent<ExtensionObject>((XmlObjectSerializer)null).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlSerializer)null).Value);
        }

        [Fact]
        public void CreateXmlContent_Reader_Attributes()
        {
            XmlSyndicationContent content = SyndicationContent.CreateXmlContent(
                new XElement("ParentObject", new XAttribute("type", "CustomType"), new XAttribute(XNamespace.Xmlns + "name", "ignored"), new XAttribute("name1", "value1"), new XAttribute(XNamespace.Get("namespace") + "name2", "value2"),
                    new XElement("ExtensionObject", new XAttribute("ignored", "value"),
                        new XElement("Value", 10)
                    )
                ).CreateReader()
            );
            Assert.Equal(2, content.AttributeExtensions.Count);
            Assert.Equal("value1", content.AttributeExtensions[new XmlQualifiedName("name1")]);
            Assert.Equal("value2", content.AttributeExtensions[new XmlQualifiedName("name2", "namespace")]);
            Assert.Equal("CustomType", content.Type);
            Assert.Null(content.Extension);
            Assert.Equal(0, content.ReadContent<ExtensionObject>().Value);
            Assert.Equal(0, content.ReadContent<ExtensionObject>(new DataContractSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(0, content.ReadContent<ExtensionObject>((XmlObjectSerializer)null).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlSerializer)null).Value);
        }

        [Fact]
        public void CreateXmlContent_NullReader_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("reader", () => SyndicationContent.CreateXmlContent(null));
        }

        [Fact]
        public void CreateXmlContent_Object_ReturnsExpected()
        {
            XmlSyndicationContent content = SyndicationContent.CreateXmlContent(new ExtensionObject { Value = 10 });
            Assert.Empty(content.AttributeExtensions);
            Assert.Equal("text/xml", content.Type);
            Assert.Equal(10, content.Extension.GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>().Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new DataContractSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlObjectSerializer)null).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlSerializer)null).Value);
        }

        public static IEnumerable<object[]> CreateXmlContent_XmlObjectSerializer_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new DataContractSerializer(typeof(ExtensionObject)) };
        }

        [Theory]
        [MemberData(nameof(CreateXmlContent_XmlObjectSerializer_TestData))]
        public void CreateXmlContent_XmlObjectSerializer_ReturnsExpected(XmlObjectSerializer dataContractSerializer)
        {
            XmlSyndicationContent content = SyndicationContent.CreateXmlContent(new ExtensionObject { Value = 10 }, dataContractSerializer);
            Assert.Empty(content.AttributeExtensions);
            Assert.Equal("text/xml", content.Type);
            Assert.Equal(10, content.Extension.GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>().Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new DataContractSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlObjectSerializer)null).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlSerializer)null).Value);
        }

        public static IEnumerable<object[]> CreateXmlContent_XmlSerializer_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new XmlSerializer(typeof(ExtensionObject)) };
        }

        [Theory]
        [MemberData(nameof(CreateXmlContent_XmlSerializer_TestData))]
        public void CreateXmlContent_XmlSerializer_ReturnsExpected(XmlSerializer serializer)
        {
            XmlSyndicationContent content = SyndicationContent.CreateXmlContent(new ExtensionObject { Value = 10 }, serializer);
            Assert.Empty(content.AttributeExtensions);
            Assert.Equal("text/xml", content.Type);
            Assert.Equal(10, content.Extension.GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>().Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new DataContractSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlObjectSerializer)null).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlSerializer)null).Value);
        }

        private class SyndicationContentSubclass : SyndicationContent
        {
            public SyndicationContentSubclass() { }

            public SyndicationContentSubclass(SyndicationContentSubclass source) : base(source) { }

            public override string Type => "CustomType";

            public override SyndicationContent Clone() => throw new NotImplementedException();

            protected override void WriteContentsTo(XmlWriter writer) => writer.WriteAttributeString("name", "value");
        }

        public class ExtensionObject
        {
            public int Value { get; set; }
        }
    }
}
