// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class ResourceCollectionInfoTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var collectionInfo = new ResourceCollectionInfo();
            Assert.Empty(collectionInfo.Accepts);
            Assert.Empty(collectionInfo.AttributeExtensions);
            Assert.Null(collectionInfo.BaseUri);
            Assert.Empty(collectionInfo.Categories);
            Assert.Empty(collectionInfo.ElementExtensions);
            Assert.Null(collectionInfo.Link);
            Assert.Null(collectionInfo.Title);
        }

        public static IEnumerable<object[]> Ctor_String_Uri_TestData()
        {
            yield return new object[] { "", new Uri("http://microsoft.com") };
            yield return new object[] { "title", new Uri("/relative", UriKind.Relative) };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_Uri_TestData))]
        public void Ctor_String_Uri(string title, Uri link)
        {
            var collectionInfo = new ResourceCollectionInfo(title, link);
            Assert.Empty(collectionInfo.Accepts);
            Assert.Empty(collectionInfo.AttributeExtensions);
            Assert.Null(collectionInfo.BaseUri);
            Assert.Empty(collectionInfo.Categories);
            Assert.Empty(collectionInfo.ElementExtensions);
            Assert.Equal(link, collectionInfo.Link);
            Assert.Equal(title, collectionInfo.Title.Text);
            Assert.Equal("text", collectionInfo.Title.Type);
        }

        public static IEnumerable<object[]> Ctor_TextSyndicationContent_Uri_TestData()
        {
            yield return new object[] { new TextSyndicationContent(""), new Uri("http://microsoft.com") };
            yield return new object[] { new TextSyndicationContent("title", TextSyndicationContentKind.Html), new Uri("/relative", UriKind.Relative) };
        }

        [Theory]
        [MemberData(nameof(Ctor_TextSyndicationContent_Uri_TestData))]
        public void Ctor_TextSyndicationContent_Uri(TextSyndicationContent title, Uri link)
        {
            var collectionInfo = new ResourceCollectionInfo(title, link);
            Assert.Empty(collectionInfo.Accepts);
            Assert.Empty(collectionInfo.AttributeExtensions);
            Assert.Null(collectionInfo.BaseUri);
            Assert.Empty(collectionInfo.Categories);
            Assert.Empty(collectionInfo.ElementExtensions);
            Assert.Equal(link, collectionInfo.Link);
            Assert.Equal(title, collectionInfo.Title);
        }

        public static IEnumerable<object[]> Ctor_TextSyndicationContent_Uri_Categories_Bool_TestData()
        {
            yield return new object[] { new TextSyndicationContent(""), new Uri("http://microsoft.com"), null, true };
            yield return new object[] { new TextSyndicationContent(""), new Uri("http://microsoft.com"), null, false };
            yield return new object[] { new TextSyndicationContent("title", TextSyndicationContentKind.Html), new Uri("/relative", UriKind.Relative), new CategoriesDocument[0], true };
            yield return new object[] { new TextSyndicationContent("title", TextSyndicationContentKind.Html), new Uri("/relative", UriKind.Relative), new CategoriesDocument[0], false };
            yield return new object[] { new TextSyndicationContent("title", TextSyndicationContentKind.Html), new Uri("/relative", UriKind.Relative), new CategoriesDocument[] { CategoriesDocument.Create(new Uri("http://microsoft.com")) }, true };
            yield return new object[] { new TextSyndicationContent("title", TextSyndicationContentKind.Html), new Uri("/relative", UriKind.Relative), new CategoriesDocument[] { CategoriesDocument.Create(new Uri("http://microsoft.com")) }, false };
        }

        [Theory]
        [MemberData(nameof(Ctor_TextSyndicationContent_Uri_Categories_Bool_TestData))]
        public void Ctor_TextSyndicationContent_Uri_Categories_Bool(TextSyndicationContent title, Uri link, IEnumerable<CategoriesDocument> categories, bool allowsNewEntries)
        {
            var collectionInfo = new ResourceCollectionInfo(title, link, categories, allowsNewEntries);
            Assert.Equal(allowsNewEntries ? new string[0] : new string[] { "" }, collectionInfo.Accepts);
            Assert.Empty(collectionInfo.AttributeExtensions);
            Assert.Null(collectionInfo.BaseUri);
            Assert.Equal(categories?.Count() ?? 0, collectionInfo.Categories.Count);
            Assert.Empty(collectionInfo.ElementExtensions);
            Assert.Equal(link, collectionInfo.Link);
            Assert.Equal(title, collectionInfo.Title);
        }

        public static IEnumerable<object[]> Ctor_TextSyndicationContent_Uri_Categories_Accepts_TestData()
        {
            yield return new object[] { new TextSyndicationContent(""), new Uri("http://microsoft.com"), null, null };
            yield return new object[] { new TextSyndicationContent("title", TextSyndicationContentKind.Html), new Uri("/relative", UriKind.Relative), new CategoriesDocument[0], new string[0] };
            yield return new object[] { new TextSyndicationContent("title", TextSyndicationContentKind.Html), new Uri("/relative", UriKind.Relative), new CategoriesDocument[] { CategoriesDocument.Create(new Uri("http://microsoft.com")) }, new string[] { "accepts" } };
        }

        [Theory]
        [MemberData(nameof(Ctor_TextSyndicationContent_Uri_Categories_Accepts_TestData))]
        public void Ctor_TextSyndicationContent_Uri_Categories_Accepts(TextSyndicationContent title, Uri link, IEnumerable<CategoriesDocument> categories, IEnumerable<string> accepts)
        {
            var collectionInfo = new ResourceCollectionInfo(title, link, categories, accepts);
            Assert.Equal(accepts ?? new string[0], collectionInfo.Accepts);
            Assert.Empty(collectionInfo.AttributeExtensions);
            Assert.Null(collectionInfo.BaseUri);
            Assert.Equal(categories?.Count() ?? 0, collectionInfo.Categories.Count);
            Assert.Empty(collectionInfo.ElementExtensions);
            Assert.Equal(link, collectionInfo.Link);
            Assert.Equal(title, collectionInfo.Title);
        }

        [Fact]
        public void Ctor_NullTitle_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("title", () => new ResourceCollectionInfo((string)null, new Uri("http://microsoft.com")));
            AssertExtensions.Throws<ArgumentNullException>("title", () => new ResourceCollectionInfo((TextSyndicationContent)null, new Uri("http://microsoft.com")));
            AssertExtensions.Throws<ArgumentNullException>("title", () => new ResourceCollectionInfo(null, new Uri("http://microsoft.com"), new CategoriesDocument[0], true));
            AssertExtensions.Throws<ArgumentNullException>("title", () => new ResourceCollectionInfo(null, new Uri("http://microsoft.com"), new CategoriesDocument[0], new string[0]));
        }

        [Fact]
        public void Ctor_NullLink_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("link", () => new ResourceCollectionInfo("title", null));
            AssertExtensions.Throws<ArgumentNullException>("link", () => new ResourceCollectionInfo(new TextSyndicationContent("title"), null));
            AssertExtensions.Throws<ArgumentNullException>("link", () => new ResourceCollectionInfo(new TextSyndicationContent("title"), null, new CategoriesDocument[0], true));
            AssertExtensions.Throws<ArgumentNullException>("link", () => new ResourceCollectionInfo(new TextSyndicationContent("title"), null, new CategoriesDocument[0], new string[0]));
        }

        [Fact]
        public void Ctor_NullValueInCategories_ThrowsArgumentNullException()
        {
            var categories = new CategoriesDocument[] { null };
            AssertExtensions.Throws<ArgumentNullException>("item", () => new ResourceCollectionInfo(new TextSyndicationContent("title"), new Uri("http://microsoft.com"), categories, true));
            AssertExtensions.Throws<ArgumentNullException>("item", () => new ResourceCollectionInfo(new TextSyndicationContent("title"), new Uri("http://microsoft.com"), categories, new string[0]));
        }

        [Fact]
        public void Ctor_NullValueInAccepts_ThrowsArgumentNullException()
        {
            var accepts = new string[] { null };
            AssertExtensions.Throws<ArgumentNullException>("item", () => new ResourceCollectionInfo(new TextSyndicationContent("title"), new Uri("http://microsoft.com"), new CategoriesDocument[0], accepts));
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

            var newValue = new ReferencedCategoriesDocument();
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

        [Fact]
        public void CreateInlineCategoriesDocument_Invoke_ReturnsExpected()
        {
            var workspace = new ResourceCollectionInfoSubclass();
            InlineCategoriesDocument document = workspace.CreateInlineCategoriesDocumentEntryPoint();
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.Categories);
            Assert.Empty(document.ElementExtensions);
            Assert.False(document.IsFixed);
            Assert.Null(document.Language);
            Assert.Null(document.Scheme);
        }

        [Fact]
        public void CreateReferencedCategoriesDocument_Invoke_ReturnsExpected()
        {
            var workspace = new ResourceCollectionInfoSubclass();
            ReferencedCategoriesDocument document = workspace.CreateReferencedCategoriesDocumentEntryPoint();
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Null(document.Link);
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
            var collectionInfo = new ResourceCollectionInfoSubclass();
            Assert.False(collectionInfo.TryParseAttributeEntryPoint(name, ns, value, version));
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
            var collectionInfo = new ResourceCollectionInfoSubclass();
            Assert.False(collectionInfo.TryParseElementEntryPoint(reader, version));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteAttributeExtensions_Invoke_ReturnsExpected(string version)
        {
            var collectionInfo = new ResourceCollectionInfoSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => collectionInfo.WriteAttributeExtensionsEntryPoint(writer, version));

            collectionInfo.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            collectionInfo.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            collectionInfo.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => collectionInfo.WriteAttributeExtensionsEntryPoint(writer, "version"));
        }

        [Fact]
        public void WriteAttributeExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var collectionInfo = new ResourceCollectionInfoSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => collectionInfo.WriteAttributeExtensionsEntryPoint(null, "version"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteElementExtensions_Invoke_ReturnsExpected(string version)
        {
            var collectionInfo = new ResourceCollectionInfoSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => collectionInfo.WriteElementExtensionsEntryPoint(writer, version));

            collectionInfo.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            collectionInfo.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<ResourceCollectionInfoTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</ResourceCollectionInfoTests.ExtensionObject>
<ResourceCollectionInfoTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</ResourceCollectionInfoTests.ExtensionObject>", writer => collectionInfo.WriteElementExtensionsEntryPoint(writer, version));
        }

        [Fact]
        public void WriteElementExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var collectionInfo = new ResourceCollectionInfoSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => collectionInfo.WriteElementExtensionsEntryPoint(null, "version"));
        }

        public class ResourceCollectionInfoSubclass : ResourceCollectionInfo
        {
            public InlineCategoriesDocument CreateInlineCategoriesDocumentEntryPoint() => CreateInlineCategoriesDocument();

            public ReferencedCategoriesDocument CreateReferencedCategoriesDocumentEntryPoint() => CreateReferencedCategoriesDocument();

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
