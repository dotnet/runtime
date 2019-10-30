// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class CategoriesDocumentTests
    {
        public static IEnumerable<object[]> Create_Categories_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new SyndicationCategory[0] };
            yield return new object[] { new SyndicationCategory[] { new SyndicationCategory("name", "scheme", "label") } };
        }

        [Theory]
        [MemberData(nameof(Create_Categories_TestData))]
        public void Create_Categories_ReturnsExpected(IList<SyndicationCategory> categories)
        {
            Collection<SyndicationCategory> categoriesCollection = categories == null ? null : new Collection<SyndicationCategory>(categories);
            InlineCategoriesDocument document = CategoriesDocument.Create(categoriesCollection);
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Equal(categoriesCollection?.Count ?? 0, document.Categories.Count);
            Assert.Empty(document.ElementExtensions);
            Assert.False(document.IsFixed);
            Assert.Null(document.Language);
            Assert.Null(document.Scheme);
        }

        public static IEnumerable<object[]> Create_CategoriesAdvanced_TestData()
        {
            yield return new object[] { null, true, null };
            yield return new object[] { new SyndicationCategory[0], false, "" };
            yield return new object[] { new SyndicationCategory[] { new SyndicationCategory("name", "scheme", "label") }, true, "scheme" };
        }

        [Theory]
        [MemberData(nameof(Create_CategoriesAdvanced_TestData))]
        public void Create_CategoriesAdvanced_ReturnsExpected(IList<SyndicationCategory> categories, bool isFixed, string scheme)
        {
            Collection<SyndicationCategory> categoriesCollection = categories == null ? null : new Collection<SyndicationCategory>(categories);
            InlineCategoriesDocument document = CategoriesDocument.Create(categoriesCollection, isFixed, scheme);
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Equal(categoriesCollection?.Count ?? 0, document.Categories.Count);
            Assert.Empty(document.ElementExtensions);
            Assert.Equal(isFixed, document.IsFixed);
            Assert.Null(document.Language);
            Assert.Equal(scheme, document.Scheme);
        }

        [Fact]
        public void Create_NullValueInCategories_ThrowsArgumentNullException()
        {
            var categories = new Collection<SyndicationCategory> { null };
            AssertExtensions.Throws<ArgumentNullException>("item", () => CategoriesDocument.Create(categories));
            AssertExtensions.Throws<ArgumentNullException>("item", () => CategoriesDocument.Create(categories, true, "scheme"));
        }

        public static IEnumerable<object[]> Create_LinkToCategoriesDocument_TestData()
        {
            yield return new object[] { new Uri("http://microsoft.com") };
            yield return new object[] { new Uri("/relative", UriKind.Relative) };
        }

        [Theory]
        [MemberData(nameof(Create_LinkToCategoriesDocument_TestData))]
        public void Create_LinkToCategoriesDocument_ReturnsExpected(Uri linkToCategoriesDocument)
        {
            ReferencedCategoriesDocument document = CategoriesDocument.Create(linkToCategoriesDocument);
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Equal(linkToCategoriesDocument, document.Link);
        }

        [Fact]
        public void Create_NullLinkToCategoriesDocument_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("link", () => CategoriesDocument.Create((Uri)null));
        }

        [Fact]
        public void GetFormatter_Invoke_ReturnsExpected()
        {
            var document = new InlineCategoriesDocument();
            AtomPub10CategoriesDocumentFormatter formatter = Assert.IsType<AtomPub10CategoriesDocumentFormatter>(document.GetFormatter());
            Assert.Same(document, formatter.Document);
            Assert.Equal("http://www.w3.org/2007/app", formatter.Version);
        }
    }
}
