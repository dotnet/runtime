// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class ServiceElementExtensionTests
    {
        [Fact]
        public void Ctor_Reader()
        {
            var extensionObject = new ExtensionObject { Value = 10 };
            var extension = new SyndicationElementExtension(new XElement("ExtensionObject", new XElement("Value", 10)).CreateReader());
            Assert.Equal("ExtensionObject", extension.OuterName);
            Assert.Empty(extension.OuterNamespace);
            Assert.Equal(0, extension.GetObject<ExtensionObject>().Value);
            Assert.Equal(0, extension.GetObject<ExtensionObject>(new DataContractSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, extension.GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
        }

        [Fact]
        public void Ctor_ReaderNotAtStart_ReturnsExpected()
        {
            using (XmlReader reader = new XElement("parent", new XElement("ExtensionObject", new XElement("Value", 10))).CreateReader())
            {
                reader.MoveToElement();
                var extension = new SyndicationElementExtension(new XElement("ExtensionObject", new XElement("Value", 10)).CreateReader());
                Assert.Equal("ExtensionObject", extension.OuterName);
                Assert.Empty(extension.OuterNamespace);
                Assert.Equal(0, extension.GetObject<ExtensionObject>().Value);
                Assert.Equal(0, extension.GetObject<ExtensionObject>(new DataContractSerializer(typeof(ExtensionObject))).Value);
                Assert.Equal(10, extension.GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
            }
        }

        [Fact]
        public void Ctor_EmptyReader_ThrowsXmlException()
        {
            using (var stringReader = new StringReader(""))
            using (var reader = XmlReader.Create(stringReader))
            {
                Assert.Throws<XmlException>(() => new SyndicationElementExtension(reader));
            }
        }
 
        [Fact]
        public void Ctor_NullReader_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("xmlReader", () => new SyndicationElementExtension(null));
        }

        [Fact]
        public void Ctor_DataContractExtension()
        {
            var extensionObject = new ExtensionObject { Value = 10 };

            // Get OuterName first.
            var extension = new SyndicationElementExtension(extensionObject);
            Assert.Equal("ServiceElementExtensionTests.ExtensionObject", extension.OuterName);
            Assert.Equal("http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests", extension.OuterNamespace);
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);

            // Get OuterNamespace first.
            extension = new SyndicationElementExtension(extensionObject);
            Assert.Equal("http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests", extension.OuterNamespace);
            Assert.Equal("ServiceElementExtensionTests.ExtensionObject", extension.OuterName);
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);

            // Get Object first.
            extension = new SyndicationElementExtension(extensionObject);
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);
            Assert.Equal("ServiceElementExtensionTests.ExtensionObject", extension.OuterName);
            Assert.Equal("http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests", extension.OuterNamespace);
        }

        public static IEnumerable<object[]> Ctor_XmlObjectSerializer_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new DataContractSerializer(typeof(ExtensionObject)) };
        }

        [Theory]
        [MemberData(nameof(Ctor_XmlObjectSerializer_TestData))]
        public void Ctor_DataContractExtension_XmlObjectSerializer(XmlObjectSerializer serializer)
        {
            var extensionObject = new ExtensionObject { Value = 10 };
            var extension = new SyndicationElementExtension(extensionObject, serializer);
            Assert.Equal("ServiceElementExtensionTests.ExtensionObject", extension.OuterName);
            Assert.Equal("http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests", extension.OuterNamespace);
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("outerName", null)]
        [InlineData(null, "")]
        [InlineData(null, "outerNamespace")]
        [InlineData("outerName", "")]
        [InlineData("outerName", "outerNamespace")]
        public void Ctor_String_String_Object(string outerName, string outerNamespace)
        {
            var extensionObject = new ExtensionObject { Value = 10 };

            // Get OuterName first.
            var extension = new SyndicationElementExtension(outerName, outerNamespace, extensionObject);
            Assert.Equal(outerName ?? "ServiceElementExtensionTests.ExtensionObject", extension.OuterName);
            Assert.Equal(outerName == null ? "http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests" : outerNamespace, extension.OuterNamespace);
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);

            // Get OuterNamespace first.
            extension = new SyndicationElementExtension(outerName, outerNamespace, extensionObject);
            Assert.Equal(outerName == null ? "http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests" : outerNamespace, extension.OuterNamespace);
            Assert.Equal(outerName ?? "ServiceElementExtensionTests.ExtensionObject", extension.OuterName);
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);

            // Get Object first.
            extension = new SyndicationElementExtension(outerName, outerNamespace, extensionObject);
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);
            Assert.Equal(outerName ?? "ServiceElementExtensionTests.ExtensionObject", extension.OuterName);
            Assert.Equal(outerName == null ? "http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests" : outerNamespace, extension.OuterNamespace);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("outerName", null)]
        [InlineData(null, "")]
        [InlineData(null, "outerNamespace")]
        [InlineData("outerName", "")]
        [InlineData("outerName", "outerNamespace")]
        public void Ctor_String_String_Object_XmlObjectSerializer(string outerName, string outerNamespace)
        {
            var extensionObject = new ExtensionObject { Value = 10 };

            // Get OuterName first.
            var extension = new SyndicationElementExtension(outerName, outerNamespace, extensionObject, new DataContractSerializer(typeof(ExtensionObject)));
            Assert.Equal(outerName ?? "ServiceElementExtensionTests.ExtensionObject", extension.OuterName);
            Assert.Equal(outerName == null ? "http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests" : outerNamespace, extension.OuterNamespace);
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);

            // Get OuterNamespace first.
            extension = new SyndicationElementExtension(outerName, outerNamespace, extensionObject, new DataContractSerializer(typeof(ExtensionObject)));
            Assert.Equal(outerName == null ? "http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests" : outerNamespace, extension.OuterNamespace);
            Assert.Equal(outerName ?? "ServiceElementExtensionTests.ExtensionObject", extension.OuterName);
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);

            // Get Object first.
            extension = new SyndicationElementExtension(outerName, outerNamespace, extensionObject, new DataContractSerializer(typeof(ExtensionObject)));
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);
            Assert.Equal(outerName ?? "ServiceElementExtensionTests.ExtensionObject", extension.OuterName);
            Assert.Equal(outerName == null ? "http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests" : outerNamespace, extension.OuterNamespace);
        }

        [Fact]
        public void Ctor_EmptyOuterName_ThrowsArgumentException()
        {
            var extensionObject = new ExtensionObject { Value = 10 };
            AssertExtensions.Throws<ArgumentException>("outerName", null, () => new SyndicationElementExtension("", "outerNamespace", extensionObject));
        }

        [Fact]
        public void Ctor_NullDataContractExtension_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("dataContractExtension", () => new SyndicationElementExtension((object)null));
            AssertExtensions.Throws<ArgumentNullException>("dataContractExtension", () => new SyndicationElementExtension(null, new DataContractSerializer(typeof(ExtensionObject))));
            AssertExtensions.Throws<ArgumentNullException>("dataContractExtension", () => new SyndicationElementExtension("OuterName", "OuterNamespace", null));
        }

        public static IEnumerable<object[]> Ctor_XmlContractExtension_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new XmlSerializer(typeof(ExtensionObject)) };
        }

        [Theory]
        [MemberData(nameof(Ctor_XmlContractExtension_TestData))]
        public void Ctor_XmlContractExtension_XmlSerializer(XmlSerializer serializer)
        {
            var extensionObject = new ExtensionObject { Value = 10 };

            // Get OuterName first.
            var extension = new SyndicationElementExtension(extensionObject, serializer);
            Assert.Equal("ExtensionObject", extension.OuterName);
            Assert.Empty(extension.OuterNamespace);
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);

            // Get OuterNamespace first.
            extension = new SyndicationElementExtension(extensionObject, serializer);
            Assert.Empty(extension.OuterNamespace);
            Assert.Equal("ExtensionObject", extension.OuterName);
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);
            Assert.Equal(10, extension.GetObject<ExtensionObject>(new DataContractSerializer(typeof(ExtensionObject))).Value);
            Assert.Equal(10, extension.GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);

            // Get Object first.
            extension = new SyndicationElementExtension(extensionObject, serializer);
            Assert.Equal(10, extension.GetObject<ExtensionObject>().Value);
            Assert.Equal("ExtensionObject", extension.OuterName);
            Assert.Empty(extension.OuterNamespace);
        }

        [Fact]
        public void Ctor_NullXmlContractExtension_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("xmlSerializerExtension", () => new SyndicationElementExtension(null, new XmlSerializer(typeof(ExtensionObject))));
        }

        [Fact]
        public void GetReader_WithReader_ReturnsExpected()
        {
            var extensionObject = new ExtensionObject { Value = 10 };
            var extension = new SyndicationElementExtension(new XElement("ExtensionObject", new XElement("Value", 10)).CreateReader());
            XmlReader reader = extension.GetReader();
            Assert.Equal(@"<ExtensionObject><Value>10</Value></ExtensionObject>", reader.ReadOuterXml());
        }

        [Fact]
        public void GetReader_ObjectWithXmlObjectSerializer_ReturnsExpected()
        {
            var extensionObject = new ExtensionObject() { Value = 10 };
            var extension = new SyndicationElementExtension(extensionObject, new DataContractSerializer(typeof(ExtensionObject)));
            XmlReader reader = extension.GetReader();
            Assert.Equal(@"<ServiceElementExtensionTests.ExtensionObject xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Value>10</Value></ServiceElementExtensionTests.ExtensionObject>", reader.ReadOuterXml());
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void GetReader_ObjectWithXmlSerializer_ReturnsExpected()
        {
            var extensionObject = new ExtensionObject() { Value = 10 };
            var extension = new SyndicationElementExtension(extensionObject, new XmlSerializer(typeof(ExtensionObject)));
            XmlReader reader = extension.GetReader();
            Assert.Equal(@"<ExtensionObject xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><Value>10</Value></ExtensionObject>", reader.ReadOuterXml());
        }

        [Fact]
        public void GetObject_NullXmlSerializer_ThrowsArgumentNullException()
        {
            var extension = new SyndicationElementExtension(new ExtensionObject());
            Assert.Throws<ArgumentNullException>("serializer", () => extension.GetObject<ExtensionObject>((XmlSerializer)null));
        }

        [Fact]
        public void GetObject_NullXmlObjectSerializer_ThrowsArgumentNullException()
        {
            var extension = new SyndicationElementExtension(new ExtensionObject());
            Assert.Throws<ArgumentNullException>("serializer", () => extension.GetObject<ExtensionObject>((XmlObjectSerializer)null));
        }

        [Fact]
        public void WriteTo_WithReader_ReturnsExpected()
        {
            var extension = new SyndicationElementExtension(new XElement("ExtensionObject", new XElement("Value", 10)).CreateReader());

            using (var stringWriter = new StringWriter())
            {
                using (var writer = new XmlTextWriter(stringWriter))
                {
                    extension.WriteTo(writer);
                }

                Assert.Equal(@"<ExtensionObject><Value>10</Value></ExtensionObject>", stringWriter.ToString());
            }
        }

        [Fact]
        public void WriteTo_ObjectWithXmlObjectSerializer_ReturnsExpected()
        {
            var extensionObject = new ExtensionObject { Value = 10 };
            var extension = new SyndicationElementExtension(extensionObject, new DataContractSerializer(typeof(ExtensionObject)));

            using (var stringWriter = new StringWriter())
            {
                using (var writer = new XmlTextWriter(stringWriter))
                {
                    extension.WriteTo(writer);
                }

                Assert.Equal(@"<ServiceElementExtensionTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests""><Value>10</Value></ServiceElementExtensionTests.ExtensionObject>", stringWriter.ToString());
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void WriteTo_ObjectWithXmlSerializer_ReturnsExpected()
        {
            var extensionObject = new ExtensionObject { Value = 10 };
            var extension = new SyndicationElementExtension(extensionObject, new XmlSerializer(typeof(ExtensionObject)));

            using (var stringWriter = new StringWriter())
            {
                using (var writer = new XmlTextWriter(stringWriter))
                {
                    extension.WriteTo(writer);
                }

                Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-16""?><ExtensionObject xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><Value>10</Value></ExtensionObject>", stringWriter.ToString());
            }
        }

        [Fact]
        public void WriteTo_NullWriter_ThrowsArgumentNullException()
        {
            var extension = new SyndicationElementExtension(new ExtensionObject());
            Assert.Throws<ArgumentNullException>("writer", () => extension.WriteTo(null));
        }

        [Fact]
        public void ISerializableIsAny_XmlObjectSerializer_OuterNameReturnsExpected()
        {
            var extensionObject = new XmlSerializabWithIsAnyNull();
            var extension = new SyndicationElementExtension(extensionObject, new DataContractSerializer(typeof(XmlSerializabWithIsAnyNull)));
            Assert.Equal("name", extension.OuterName);
            Assert.Empty(extension.OuterNamespace);
            Assert.NotNull(extension.GetObject<XmlSerializabWithIsAnyNull>(new DataContractSerializer(typeof(XmlSerializabWithIsAnyNull))));
        }

        [Fact]
        public void ISerializableIsAny_XmlSerializer_OuterNameReturnsExpected()
        {
            var extensionObject = new XmlSerializabWithIsAny();
            var extension = new SyndicationElementExtension(extensionObject, new XmlSerializer(typeof(XmlSerializabWithIsAny)));
            Assert.Equal("name", extension.OuterName);
            Assert.Empty(extension.OuterNamespace);
            Assert.NotNull(extension.GetObject<XmlSerializabWithIsAny>(new XmlSerializer(typeof(XmlSerializabWithIsAny))));
        }

        [DataContract]
        public class ExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }

        [XmlSchemaProvider("GetXsdType", IsAny = true)]
        public class XmlSerializabWithIsAny : IXmlSerializable
        {
            public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet) => new XmlQualifiedName("string", XmlSchema.Namespace);

            public XmlSchema GetSchema() => null;

            public void ReadXml(XmlReader reader) { }

            public void WriteXml(XmlWriter writer)
            {
                writer.WriteElementString("name", "value");
            }
        }

        [XmlSchemaProvider("GetXsdType", IsAny = true)]
        public class XmlSerializabWithIsAnyNull : IXmlSerializable
        {
            public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet) => null;

            public XmlSchema GetSchema() => null;

            public void ReadXml(XmlReader reader) { }

            public void WriteXml(XmlWriter writer)
            {
                writer.WriteElementString("name", "value");
            }
        }
    }
}
