// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class CategoriesDocumentFormatterTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var formatter = new Formatter();
            Assert.Null(formatter.Document);
        }

        [Fact]
        public void Ctor_CategoriesDocument()
        {
            var document = new InlineCategoriesDocument();
            var formatter = new Formatter(document);
            Assert.Same(document, formatter.Document);
        }

        [Fact]
        public void Ctor_NullDocumentToWrite_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("documentToWrite", () => new Formatter(null));
        }

        [Fact]
        public void CreateInlineCategoriesDocument_Invoke_ReturnsExpected()
        {
            var formatter = new Formatter();
            InlineCategoriesDocument document = formatter.CreateInlineCategoriesDocumentEntryPoint();
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
            var formatter = new Formatter();
            ReferencedCategoriesDocument document = formatter.CreateReferencedCategoriesDocumentEntryPoint();
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Null(document.Link);
        }

        [Fact]
        public void SetDocument_GetDocument_ReturnsExpected()
        {
            var formatter = new Formatter();
            var document = new InlineCategoriesDocument();
            formatter.SetDocumentEntryPoint(document);
            Assert.Same(document, formatter.Document);

            formatter.SetDocumentEntryPoint(null);
            Assert.Null(formatter.Document);
        }

        public class Formatter : CategoriesDocumentFormatter
        {
            public Formatter() : base() { }

            public Formatter(CategoriesDocument documentToWrite) : base(documentToWrite) { }

            public InlineCategoriesDocument CreateInlineCategoriesDocumentEntryPoint() => CreateInlineCategoriesDocument();

            public ReferencedCategoriesDocument CreateReferencedCategoriesDocumentEntryPoint() => CreateReferencedCategoriesDocument();

            public void SetDocumentEntryPoint(CategoriesDocument document) => SetDocument(document);

            public override string Version => throw new NotImplementedException();

            public override bool CanRead(XmlReader reader) => throw new NotImplementedException();

            public override void ReadFrom(XmlReader reader) => throw new NotImplementedException();

            public override void WriteTo(XmlWriter writer) => throw new NotImplementedException();
        }
    }
}
