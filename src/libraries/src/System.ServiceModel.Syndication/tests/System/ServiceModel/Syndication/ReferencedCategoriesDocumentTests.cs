// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class ReferencedCategoriesDocumentTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var document = new ReferencedCategoriesDocument();
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Null(document.Link);
        }

        public static IEnumerable<object[]> Ctor_Uri_TestData()
        {
            yield return new object[] { new Uri("http://microsoft.com") };
            yield return new object[] { new Uri("/relative", UriKind.Relative) };
        }

        [Theory]
        [MemberData(nameof(Ctor_Uri_TestData))]
        public void Ctor_Uri(Uri link)
        {
            var document = new ReferencedCategoriesDocument(link);
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Equal(link, document.Link);
        }

        [Fact]
        public void Ctor_NullLink_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("link", () => new ReferencedCategoriesDocument(null));
        }

        [Fact]
        public void Accepts_AddNonNullItem_Success()
        {
            Collection<string> collection = new ResourceCollectionInfo().Accepts;
            collection.Add("value");
            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public void Accepts_AddNullItem_ThrowsArgumentNullException()
        {
            Collection<string> collection = new ResourceCollectionInfo().Accepts;
            AssertExtensions.Throws<ArgumentNullException>("item", () => collection.Add(null));
        }

        [Fact]
        public void Accepts_SetNonNullItem_GetReturnsExpected()
        {
            Collection<string> collection = new ResourceCollectionInfo().Accepts;
            collection.Add("value");

            collection[0] = "newValue";
            Assert.Equal("newValue", collection[0]);
        }

        [Fact]
        public void Accepts_SetNullItem_ThrowsArgumentNullException()
        {
            Collection<string> collection = new ResourceCollectionInfo().Accepts;
            collection.Add("value");

            AssertExtensions.Throws<ArgumentNullException>("item", () => collection[0] = null);
        }

        [Fact]
        public void Categories_AddNonNullItem_Success()
        {
            Collection<CategoriesDocument> collection = new ResourceCollectionInfo().Categories;
            collection.Add(new InlineCategoriesDocument());
            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public void Categories_AddNullItem_ThrowsArgumentNullException()
        {
            Collection<CategoriesDocument> collection = new ResourceCollectionInfo().Categories;
            AssertExtensions.Throws<ArgumentNullException>("item", () => collection.Add(null));
        }

        [Fact]
        public void Categories_SetNonNullItem_GetReturnsExpected()
        {
            Collection<CategoriesDocument> collection = new ResourceCollectionInfo().Categories;
            collection.Add(new InlineCategoriesDocument());

            var newValue = new InlineCategoriesDocument();
            collection[0] = newValue;
            Assert.Same(newValue, collection[0]);
        }

        [Fact]
        public void Categories_SetNullItem_ThrowsArgumentNullException()
        {
            Collection<CategoriesDocument> collection = new ResourceCollectionInfo().Categories;
            collection.Add(new InlineCategoriesDocument());

            AssertExtensions.Throws<ArgumentNullException>("item", () => collection[0] = null);
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData("", "", "", "")]
        [InlineData("name", "ns", "value", "version")]
        [InlineData("xmlns", "ns", "value", "version")]
        [InlineData("name", "http://www.w3.org/2000/xmlns/", "value", "version")]
        [InlineData("type", "ns", "value", "version")]
        [InlineData("name", "http://www.w3.org/2001/XMLSchema-instance", "value", "version")]
        public void TryParseAttribute_Invoke_ReturnsFalse(string name, string ns, string value, string version)
        {
            var document = new ReferencedCategoriesDocumentSubclass();
            Assert.False(document.TryParseAttributeEntryPoint(name, ns, value, version));
        }

        public static IEnumerable<object[]> TryParseElement_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new XElement("name").CreateReader(), "" };
            yield return new object[] { new XElement("name").CreateReader(), "version" };
        }

        [Theory]
        [MemberData(nameof(TryParseElement_TestData))]
        public void TryParseElement_Invoke_ReturnsFalse(XmlReader reader, string version)
        {
            var document = new ReferencedCategoriesDocumentSubclass();
            Assert.False(document.TryParseElementEntryPoint(reader, version));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteAttributeExtensions_Invoke_ReturnsExpected(string version)
        {
            var document = new ReferencedCategoriesDocumentSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => document.WriteAttributeExtensionsEntryPoint(writer, version));

            document.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            document.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            document.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => document.WriteAttributeExtensionsEntryPoint(writer, "version"));
        }

        [Fact]
        public void WriteAttributeExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var document = new ReferencedCategoriesDocumentSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => document.WriteAttributeExtensionsEntryPoint(null, "version"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteElementExtensions_Invoke_ReturnsExpected(string version)
        {
            var document = new ReferencedCategoriesDocumentSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => document.WriteElementExtensionsEntryPoint(writer, version));

            document.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            document.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<ReferencedCategoriesDocumentTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</ReferencedCategoriesDocumentTests.ExtensionObject>
<ReferencedCategoriesDocumentTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</ReferencedCategoriesDocumentTests.ExtensionObject>", writer => document.WriteElementExtensionsEntryPoint(writer, version));
        }

        [Fact]
        public void WriteElementExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var document = new ReferencedCategoriesDocumentSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => document.WriteElementExtensionsEntryPoint(null, "version"));
        }

        public class ReferencedCategoriesDocumentSubclass : ReferencedCategoriesDocument
        {
            public bool TryParseAttributeEntryPoint(string name, string ns, string value, string version) => TryParseAttribute(name, ns, value, version);

            public bool TryParseElementEntryPoint(XmlReader reader, string version) => TryParseElement(reader, version);

            public void WriteAttributeExtensionsEntryPoint(XmlWriter writer, string version) => WriteAttributeExtensions(writer, version);

            public void WriteElementExtensionsEntryPoint(XmlWriter writer, string version) => WriteElementExtensions(writer, version);
        }

        [DataContract]
        public class ExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }
    }
}
