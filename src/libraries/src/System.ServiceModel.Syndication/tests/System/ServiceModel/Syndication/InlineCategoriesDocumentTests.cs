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
    public class InlineCategoriesDocumentTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var document = new InlineCategoriesDocument();
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.Categories);
            Assert.Empty(document.ElementExtensions);
            Assert.False(document.IsFixed);
            Assert.Null(document.Language);
            Assert.Null(document.Scheme);
        }

        public static IEnumerable<object[]> Ctor_Categories_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new SyndicationCategory[0] };
            yield return new object[] { new SyndicationCategory[] { new SyndicationCategory("name", "scheme", "label") } };
        }

        [Theory]
        [MemberData(nameof(Ctor_Categories_TestData))]
        public void Ctor_Categories(IEnumerable<SyndicationCategory> categories)
        {
            var document = new InlineCategoriesDocument(categories);
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Equal(categories?.Count() ?? 0, document.Categories.Count);
            Assert.Empty(document.ElementExtensions);
            Assert.False(document.IsFixed);
            Assert.Null(document.Language);
            Assert.Null(document.Scheme);
        }

        public static IEnumerable<object[]> Ctor_Categories_Bool_String_TestData()
        {
            yield return new object[] { null, true, null };
            yield return new object[] { new SyndicationCategory[0], false, "" };
            yield return new object[] { new SyndicationCategory[] { new SyndicationCategory("name", "scheme", "label") }, true, "scheme" };
        }

        [Theory]
        [MemberData(nameof(Ctor_Categories_Bool_String_TestData))]
        public void Ctor_Categories_Bool_String(IEnumerable<SyndicationCategory> categories, bool isFixed, string scheme)
        {
            var document = new InlineCategoriesDocument(categories, isFixed, scheme);
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Equal(categories?.Count() ?? 0, document.Categories.Count);
            Assert.Empty(document.ElementExtensions);
            Assert.Equal(isFixed, document.IsFixed);
            Assert.Null(document.Language);
            Assert.Equal(scheme, document.Scheme);
        }

        [Fact]
        public void Ctor_NullValueInCategories_ThrowsArgumentNullException()
        {
            var categories = new Collection<SyndicationCategory> { null };
            AssertExtensions.Throws<ArgumentNullException>("item", () => new InlineCategoriesDocument(categories));
            AssertExtensions.Throws<ArgumentNullException>("item", () => new InlineCategoriesDocument(categories, true, "scheme"));
        }

        [Fact]
        public void Categories_AddNonNullItem_Success()
        {
            Collection<SyndicationCategory> collection = new InlineCategoriesDocument().Categories;
            collection.Add(new SyndicationCategory());
            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public void Categories_AddNullItem_ThrowsArgumentNullException()
        {
            Collection<SyndicationCategory> collection = new InlineCategoriesDocument().Categories;
            AssertExtensions.Throws<ArgumentNullException>("item", () => collection.Add(null));
        }

        [Fact]
        public void Categories_SetNonNullItem_GetReturnsExpected()
        {
            Collection<SyndicationCategory> collection = new InlineCategoriesDocument().Categories;
            collection.Add(new SyndicationCategory());

            var newValue = new SyndicationCategory();
            collection[0] = newValue;
            Assert.Same(newValue, collection[0]);
        }

        [Fact]
        public void Categories_SetNullItem_ThrowsArgumentNullException()
        {
            Collection<SyndicationCategory> collection = new InlineCategoriesDocument().Categories;
            collection.Add(new SyndicationCategory());

            AssertExtensions.Throws<ArgumentNullException>("item", () => collection[0] = null);
        }

        [Fact]
        public void CreateCategory_Invoke_ReturnsExpected()
        {
            var document = new InlineCategoriesDocumentSubclass();
            SyndicationCategory category = document.CreateCategoryEntryPoint();
            Assert.Empty(category.AttributeExtensions);
            Assert.Empty(category.ElementExtensions);
            Assert.Null(category.Name);
            Assert.Null(category.Scheme);
            Assert.Null(category.Label);
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
            var document = new InlineCategoriesDocumentSubclass();
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
            var document = new InlineCategoriesDocumentSubclass();
            Assert.False(document.TryParseElementEntryPoint(reader, version));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteAttributeExtensions_Invoke_ReturnsExpected(string version)
        {
            var document = new InlineCategoriesDocumentSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => document.WriteAttributeExtensionsEntryPoint(writer, version));

            document.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            document.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            document.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => document.WriteAttributeExtensionsEntryPoint(writer, "version"));
        }

        [Fact]
        public void WriteAttributeExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var document = new InlineCategoriesDocumentSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => document.WriteAttributeExtensionsEntryPoint(null, "version"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteElementExtensions_Invoke_ReturnsExpected(string version)
        {
            var document = new InlineCategoriesDocumentSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => document.WriteElementExtensionsEntryPoint(writer, version));

            document.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            document.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<InlineCategoriesDocumentTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</InlineCategoriesDocumentTests.ExtensionObject>
<InlineCategoriesDocumentTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</InlineCategoriesDocumentTests.ExtensionObject>", writer => document.WriteElementExtensionsEntryPoint(writer, version));
        }

        [Fact]
        public void WriteElementExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var document = new InlineCategoriesDocumentSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => document.WriteElementExtensionsEntryPoint(null, "version"));
        }

        public class InlineCategoriesDocumentSubclass : InlineCategoriesDocument
        {
            public SyndicationCategory CreateCategoryEntryPoint() => CreateCategory();

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
