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
    public class XmlSyndicationContentTests
    {
        [Fact]
        public void Ctor_Reader_NoAttributes()
        {
            var content = new XmlSyndicationContent(
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
        public void Ctor_Reader_Attributes()
        {
            var content = new XmlSyndicationContent(
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
        public void Ctor_NullReader_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("reader", () => new XmlSyndicationContent(null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("text/html")]
        public void Ctor_Type_SyndicationElementExtension(string type)
        {
            var extension = new SyndicationElementExtension(new ExtensionObject { Value = 10 });
            var content = new XmlSyndicationContent(type, extension);
            Assert.Empty(content.AttributeExtensions);
            Assert.Equal(string.IsNullOrEmpty(type) ? "text/xml" : type, content.Type);
            Assert.Same(extension, content.Extension);
            Assert.Equal(10, content.ReadContent<ExtensionObject>().Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new DataContractSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlObjectSerializer)null).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlSerializer)null).Value);
        }

        public static IEnumerable<object[]> Ctor_Type_SyndicationElementExtension_XmlObjectSerializer_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { "", new DataContractSerializer(typeof(ExtensionObject)) };
            yield return new object[] { "text/html", new DataContractSerializer(typeof(ExtensionObject)) };
        }

        [Theory]
        [MemberData(nameof(Ctor_Type_SyndicationElementExtension_XmlObjectSerializer_TestData))]
        public void Ctor_Type_SyndicationElementExtension_XmlObjectSerializer(string type, XmlObjectSerializer dataContractSerializer)
        {
            var content = new XmlSyndicationContent(type, new ExtensionObject { Value = 10 }, dataContractSerializer);
            Assert.Empty(content.AttributeExtensions);
            Assert.Equal(string.IsNullOrEmpty(type) ? "text/xml" : type, content.Type);
            Assert.Equal(10, content.Extension.GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>().Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new DataContractSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlObjectSerializer)null).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlSerializer)null).Value);
        }

        public static IEnumerable<object[]> Ctor_Type_SyndicationElementExtension_XmlSerializer_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { "", new XmlSerializer(typeof(ExtensionObject)) };
            yield return new object[] { "text/html", new XmlSerializer(typeof(ExtensionObject)) };
        }

        [Theory]
        [MemberData(nameof(Ctor_Type_SyndicationElementExtension_XmlSerializer_TestData))]
        public void Ctor_Type_SyndicationElementExtension_XmlSerializer(string type, XmlSerializer serializer)
        {
            var content = new XmlSyndicationContent(type, new ExtensionObject { Value = 10 }, serializer);
            Assert.Empty(content.AttributeExtensions);
            Assert.Equal(string.IsNullOrEmpty(type) ? "text/xml" : type, content.Type);
            Assert.Equal(10, content.Extension.GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>().Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new DataContractSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlObjectSerializer)null).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, content.ReadContent<ExtensionObject>((XmlSerializer)null).Value);
        }

        [Fact]
        public void Ctor_NullExtension_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("extension", () => new XmlSyndicationContent("type", null));
        }

        [Fact]
        public void Ctor_XmlSyndicationContent_Full()
        {
            var content = new SyndicationElementExtension(new ExtensionObject { Value = 10 });
            var original = new XmlSyndicationContent("type", content);
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");

            var clone = new XmlSyndicationContentSubclass(original);
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.Same(original.Extension, clone.Extension);
            Assert.Equal("type", clone.Type);
        }

        [Fact]
        public void Ctor_XmlSyndicationContent_Empty()
        {
            var content = new SyndicationElementExtension(new ExtensionObject { Value = 10 });
            var original = new XmlSyndicationContent("type", content);
            var clone = new XmlSyndicationContentSubclass(original);
            Assert.Empty(clone.AttributeExtensions);
            Assert.Same(original.Extension, clone.Extension);
            Assert.Equal("type", clone.Type);
        }

        [Fact]
        public void Ctor_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => new XmlSyndicationContentSubclass(null));
        }

        [Fact]
        public void Clone_Full_ReturnsExpected()
        {
            var content = new SyndicationElementExtension(new ExtensionObject { Value = 10 });
            var original = new XmlSyndicationContent("type", content);
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");

            XmlSyndicationContent clone = Assert.IsType<XmlSyndicationContent>(original.Clone());
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.Same(original.Extension, clone.Extension);
            Assert.Equal("type", clone.Type);
        }

        [Fact]
        public void Clone_Empty_ReturnsExpected()
        {
            var content = new SyndicationElementExtension(new ExtensionObject { Value = 10 });
            var original = new XmlSyndicationContent("type", content);
            XmlSyndicationContent clone = Assert.IsType<XmlSyndicationContent>(original.Clone());
            Assert.Empty(clone.AttributeExtensions);
            Assert.Same(original.Extension, clone.Extension);
            Assert.Equal("type", clone.Type);
        }

        [Fact]
        public void WriteTo_WithReader_ReturnsExpected()
        {
            var content = new XmlSyndicationContent(
                new XElement("ParentObject",
                    new XElement("ExtensionObject",
                        new XElement("Value", 10)
                    )
                ).CreateReader()
            );
            CompareHelper.AssertEqualWriteOutput(
@"<OuterElementName type=""text/xml"" xmlns=""OuterElementNamespace"">
    <ExtensionObject xmlns="""">
        <Value>10</Value>
    </ExtensionObject>
</OuterElementName>", writer => content.WriteTo(writer, "OuterElementName", "OuterElementNamespace"));
        }

        [Fact]
        public void WriteTo_WithEmptyReader_ReturnsExpected()
        {
            using (var stringReader = new StringReader("<ParentObject />"))
            using (var reader = XmlReader.Create(stringReader))

            {
                var content = new XmlSyndicationContent(reader);
                CompareHelper.AssertEqualWriteOutput(@"<OuterElementName type=""text/xml"" xmlns=""OuterElementNamespace"" />", writer => content.WriteTo(writer, "OuterElementName", "OuterElementNamespace"));
            }
        }

        [Fact]
        public void WriteTo_ObjectWithXmlObjectSerializer_ReturnsExpected()
        {
            var extensionObject = new ExtensionObject() { Value = 10 };
            var content = new XmlSyndicationContent("type", extensionObject, new DataContractSerializer(typeof(ExtensionObject)));
            CompareHelper.AssertEqualWriteOutput(
@"<OuterElementName type=""type"" xmlns=""OuterElementNamespace"">
    <XmlSyndicationContentTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </XmlSyndicationContentTests.ExtensionObject>
</OuterElementName>", writer => content.WriteTo(writer, "OuterElementName", "OuterElementNamespace"));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void WriteTo_ObjectWithXmlSerializer_ReturnsExpected()
        {
            var extensionObject = new ExtensionObject() { Value = 10 };
            var content = new XmlSyndicationContent("type", extensionObject, new XmlSerializer(typeof(ExtensionObject)));
            CompareHelper.AssertEqualWriteOutput(
@"<OuterElementName type=""type"" xmlns=""OuterElementNamespace"">
    <ExtensionObject xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns="""">
        <Value>10</Value>
    </ExtensionObject>
</OuterElementName>", writer => content.WriteTo(writer, "OuterElementName", "OuterElementNamespace"));
        }

        [Fact]
        public void WriteContentsTo_NullWriter_ThrowsArgumentNullException()
        {
            var content = new XmlSyndicationContentSubclass("type", new SyndicationElementExtension(new ExtensionObject { Value = 10 }));
            AssertExtensions.Throws<ArgumentNullException>("writer", () => content.WriteContentsToEntryPoint(null));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void GetReaderAtContent_WithReader_ReturnsExpected()
        {
            var content = new XmlSyndicationContent(
                new XElement("ParentObject",
                    new XElement("ExtensionObject",
                        new XElement("Value", 10)
                    )
                ).CreateReader()
            );
            XmlReader reader = content.GetReaderAtContent();
            CompareHelper.AssertEqualLongString(@"<ParentObject><ExtensionObject><Value>10</Value></ExtensionObject></ParentObject>", reader.ReadOuterXml());
        }

        [Fact]
        public void GetReaderAtContent_ObjectWithXmlObjectSerializer_ReturnsExpected()
        {
            var extensionObject = new ExtensionObject() { Value = 10 };
            var content = new XmlSyndicationContent("type", extensionObject, new DataContractSerializer(typeof(ExtensionObject)));
            using (XmlReader reader = content.GetReaderAtContent())
            {
                CompareHelper.AssertEqualLongString(@"<content type=""type"" xmlns=""http://www.w3.org/2005/Atom""><XmlSyndicationContentTests.ExtensionObject xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Value>10</Value></XmlSyndicationContentTests.ExtensionObject></content>", reader.ReadOuterXml());
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void GetReaderAtContent_ObjectWithXmlSerializer_ReturnsExpected()
        {
            var extensionObject = new ExtensionObject() { Value = 10 };
            var content = new XmlSyndicationContent("type", extensionObject, new XmlSerializer(typeof(ExtensionObject)));
            using (XmlReader reader = content.GetReaderAtContent())
            {
                CompareHelper.AssertEqualLongString(@"<content type=""type"" xmlns=""http://www.w3.org/2005/Atom""><ExtensionObject xmlns="""" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><Value>10</Value></ExtensionObject></content>", reader.ReadOuterXml());
            }
        }

        private class XmlSyndicationContentSubclass : XmlSyndicationContent
        {
            public XmlSyndicationContentSubclass(XmlSyndicationContent source) : base(source)
            {
            }

            public XmlSyndicationContentSubclass(string type, SyndicationElementExtension extension) : base(type, extension)
            {
            }

            public void WriteContentsToEntryPoint(XmlWriter writer) => WriteContentsTo(writer);
        }

        [DataContract]
        public class ExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }
    }
}
