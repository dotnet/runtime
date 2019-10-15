// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class AtomPub10CategoriesDocumentFormatterTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var formatter = new AtomPub10CategoriesDocumentFormatter();
            Assert.Null(formatter.Document);
            Assert.Equal("http://www.w3.org/2007/app", formatter.Version);
        }

        [Fact]
        public void Ctor_CategoriesDocument_Inline()
        {
            var document = new InlineCategoriesDocument();
            var formatter = new AtomPub10CategoriesDocumentFormatter(document);
            Assert.Same(document, formatter.Document);
            Assert.Equal("http://www.w3.org/2007/app", formatter.Version);
        }

        [Fact]
        public void Ctor_CategoriesDocument_Referenced()
        {
            var document = new ReferencedCategoriesDocument();
            var formatter = new AtomPub10CategoriesDocumentFormatter(document);
            Assert.Same(document, formatter.Document);
            Assert.Equal("http://www.w3.org/2007/app", formatter.Version);
        }

        [Fact]
        public void Ctor_NullDocumentToWrite_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("documentToWrite", () => new AtomPub10CategoriesDocumentFormatter(null));
        }

        [Theory]
        [InlineData(typeof(InlineCategoriesDocument), typeof(ReferencedCategoriesDocument))]
        [InlineData(typeof(InlineCategoriesDocumentSubclass), typeof(ReferencedCategoriesDocumentSubclass))]
        public void Ctor_Type_Type(Type inlineDocumentType, Type referencedDocumentType)
        {
            var document = new InlineCategoriesDocument();
            var formatter = new AtomPub10CategoriesDocumentFormatter(inlineDocumentType, referencedDocumentType);
            Assert.Null(formatter.Document);
            Assert.Equal("http://www.w3.org/2007/app", formatter.Version);
        }

        [Fact]
        public void Ctor_NullInlineDocumentType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("inlineDocumentType", () => new AtomPub10CategoriesDocumentFormatter(null, typeof(ReferencedCategoriesDocument)));
        }

        [Fact]
        public void Ctor_InvlaidInlineDocumentType_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("inlineDocumentType", () => new AtomPub10CategoriesDocumentFormatter(typeof(int), typeof(ReferencedCategoriesDocument)));
        }

        [Fact]
        public void Ctor_NullReferencedDocumentType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("referencedDocumentType", () => new AtomPub10CategoriesDocumentFormatter(typeof(InlineCategoriesDocument), null));
        }

        [Fact]
        public void Ctor_InvalidReferencedDocumentType_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("referencedDocumentType", () => new AtomPub10CategoriesDocumentFormatter(typeof(InlineCategoriesDocument), typeof(int)));
        }

        [Fact]
        public void GetSchema_Invoke_ReturnsNull()
        {
            IXmlSerializable formatter = new AtomPub10CategoriesDocumentFormatter();
            Assert.Null(formatter.GetSchema());
        }

        public static IEnumerable<object[]> WriteTo_TestData()
        {
            // Empty InlineCategoriesDocument.
            yield return new object[]
            {
                new InlineCategoriesDocument(),
                @"<app:categories xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app"" />"
            };

            // Full InlineCategoriesDocument
            var fullSyndicationCategory = new SyndicationCategory("category_name", "category_scheme", "category_label");
            fullSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("category_name1"), null);
            fullSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("category_name2", "category_namespace"), "");
            fullSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("category_name3", "category_namespace"), "category_value");
            fullSyndicationCategory.AttributeExtensions.Add(new XmlQualifiedName("category_name4", "xmlns"), "");

            fullSyndicationCategory.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            var fullInlineCategoriesDocument = new InlineCategoriesDocument(new SyndicationCategory[]
            {
                new SyndicationCategory(),
                fullSyndicationCategory
            })
            {
                BaseUri = new Uri("http://inlinecategories_url.com"),
                Language = "inlinecategories_Language",
                IsFixed = true,
                Scheme = "inlinecategories_scheme"
            };
            fullInlineCategoriesDocument.AttributeExtensions.Add(new XmlQualifiedName("inlinecategories_name1"), null);
            fullInlineCategoriesDocument.AttributeExtensions.Add(new XmlQualifiedName("inlinecategories_name2", "inlinecategories_namespace"), "");
            fullInlineCategoriesDocument.AttributeExtensions.Add(new XmlQualifiedName("inlinecategories_name3", "inlinecategories_namespace"), "inlinecategories_value");
            fullInlineCategoriesDocument.AttributeExtensions.Add(new XmlQualifiedName("inlinecategories_name4", "xmlns"), "");

            fullInlineCategoriesDocument.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            yield return new object[]
            {
                fullInlineCategoriesDocument,
@"<app:categories xmlns:a10=""http://www.w3.org/2005/Atom"" xml:base=""http://inlinecategories_url.com/"" xml:lang=""inlinecategories_Language"" scheme=""inlinecategories_scheme"" fixed=""yes"" inlinecategories_name1="""" d1p1:inlinecategories_name2="""" d1p1:inlinecategories_name3=""inlinecategories_value"" d1p2:inlinecategories_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""inlinecategories_namespace"" xmlns:app=""http://www.w3.org/2007/app"">
    <a10:category term="""" />
    <a10:category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d2p1=""category_namespace"">
        <AtomPub10CategoriesDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </AtomPub10CategoriesDocumentFormatterTests.ExtensionObject>
    </a10:category>
    <AtomPub10CategoriesDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </AtomPub10CategoriesDocumentFormatterTests.ExtensionObject>
</app:categories>"
            };

            // Empty ReferencedCategoriesDocument.
            yield return new object[]
            {
                new ReferencedCategoriesDocument(),
                @"<app:categories xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app"" />"
            };

            // Full ReferencedCategoriesDocument.
            var fullReferenceCategoriesDocument = new ReferencedCategoriesDocument(new Uri("http://referencecategories_link.com"))
            {
                BaseUri = new Uri("http://referencecategories_url.com"),
                Language = "referencecategories_language"
            };
            fullReferenceCategoriesDocument.AttributeExtensions.Add(new XmlQualifiedName("referencecategories_name1"), null);
            fullReferenceCategoriesDocument.AttributeExtensions.Add(new XmlQualifiedName("referencecategories_name2", "referencecategories_namespace"), "");
            fullReferenceCategoriesDocument.AttributeExtensions.Add(new XmlQualifiedName("referencecategories_name3", "referencecategories_namespace"), "referencecategories_value");
            fullReferenceCategoriesDocument.AttributeExtensions.Add(new XmlQualifiedName("referencecategories_name4", "xmlns"), "");

            fullReferenceCategoriesDocument.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            yield return new object[]
            {
                fullReferenceCategoriesDocument,
@"<app:categories xmlns:a10=""http://www.w3.org/2005/Atom"" xml:base=""http://referencecategories_url.com/"" xml:lang=""referencecategories_language"" href=""http://referencecategories_link.com/"" referencecategories_name1="""" d1p1:referencecategories_name2="""" d1p1:referencecategories_name3=""referencecategories_value"" d1p2:referencecategories_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""referencecategories_namespace"" xmlns:app=""http://www.w3.org/2007/app"">
    <AtomPub10CategoriesDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </AtomPub10CategoriesDocumentFormatterTests.ExtensionObject>
</app:categories>"
            };
        }

        [Theory]
        [MemberData(nameof(WriteTo_TestData))]
        public void WriteTo_HasDocument_SerializesExpected(CategoriesDocument document, string expected)
        {
            var formatter = new AtomPub10CategoriesDocumentFormatter(document);
            CompareHelper.AssertEqualWriteOutput(expected, writer => formatter.WriteTo(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer => document.Save(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer =>
            {
                writer.WriteStartElement("app", "categories", "http://www.w3.org/2007/app");
                ((IXmlSerializable)formatter).WriteXml(writer);
                writer.WriteEndElement();
            });
        }

        [Fact]
        public void WriteTo_NullWriter_ThrowsArgumentNullException()
        {
            var formatter = new AtomPub10CategoriesDocumentFormatter();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => formatter.WriteTo(null));
        }

        [Fact]
        public void WriteTo_NoDocument_ThrowsInvalidOperationException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new AtomPub10CategoriesDocumentFormatter();
                Assert.Throws<InvalidOperationException>(() => formatter.WriteTo(writer));
            }
        }

        [Fact]
        public void WriteXml_NullWriter_ThrowsArgumentNullException()
        {
            IXmlSerializable formatter = new AtomPub10CategoriesDocumentFormatter();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => formatter.WriteXml(null));
        }

        [Fact]
        public void WriteXml_NoDocument_ThrowsInvalidOperationException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                IXmlSerializable formatter = new AtomPub10CategoriesDocumentFormatter();
                Assert.Throws<InvalidOperationException>(() => formatter.WriteXml(writer));
            }
        }

        public static IEnumerable<object[]> CanRead_TestData()
        {
            yield return new object[] { @"<categories />", false };
            yield return new object[] { @"<app:different xmlns:app=""http://www.w3.org/2007/app"">", false };
            yield return new object[] { @"<app:categories xmlns:app=""http://www.w3.org/2007/app"" />", true };
        }

        [Theory]
        [MemberData(nameof(CanRead_TestData))]
        public void CanRead_ValidReader_ReturnsExpected(string xmlString, bool expected)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10CategoriesDocumentFormatter();
                Assert.Equal(expected, formatter.CanRead(reader));
            }
        }

        [Fact]
        public void CanRead_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new AtomPub10CategoriesDocumentFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.CanRead(null));
        }

        [Fact]
        public void ReadFrom_InlineCategoriesDocument_ReturnsExpected()
        {
            string xmlString =
@"<app:categories xmlns:a10=""http://www.w3.org/2005/Atom"" xml:base=""http://inlinecategories_url.com/"" xml:lang=""inlinecategories_Language"" scheme=""inlinecategories_scheme"" fixed=""yes"" inlinecategories_name1="""" d1p1:inlinecategories_name2="""" d1p1:inlinecategories_name3=""inlinecategories_value"" d1p2:inlinecategories_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""inlinecategories_namespace"" xmlns:app=""http://www.w3.org/2007/app"">
    <a10:category term="""" />
    <a10:category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d2p1=""category_namespace"">
        <AtomPub10CategoriesDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </AtomPub10CategoriesDocumentFormatterTests.ExtensionObject>
    </a10:category>
    <AtomPub10CategoriesDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </AtomPub10CategoriesDocumentFormatterTests.ExtensionObject>
</app:categories>";
            VerifyRead<InlineCategoriesDocument>(xmlString, document =>
            {
                Assert.Equal(4, document.AttributeExtensions.Count);
                Assert.Equal("", document.AttributeExtensions[new XmlQualifiedName("inlinecategories_name1")]);
                Assert.Equal("", document.AttributeExtensions[new XmlQualifiedName("inlinecategories_name2", "inlinecategories_namespace")]);
                Assert.Equal("inlinecategories_value", document.AttributeExtensions[new XmlQualifiedName("inlinecategories_name3", "inlinecategories_namespace")]);
                Assert.Equal("", document.AttributeExtensions[new XmlQualifiedName("inlinecategories_name4", "xmlns")]);
                Assert.Equal(new Uri("http://inlinecategories_url.com/"), document.BaseUri);
                Assert.Equal(2, document.Categories.Count);
                Assert.Equal(1, document.ElementExtensions.Count);
                Assert.Equal(10, document.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                Assert.True(document.IsFixed);
                Assert.Equal("inlinecategories_Language", document.Language);
                Assert.Equal("inlinecategories_scheme", document.Scheme);

                SyndicationCategory firstCategory = document.Categories[0];
                Assert.Empty(firstCategory.AttributeExtensions);
                Assert.Empty(firstCategory.ElementExtensions);
                Assert.Empty(firstCategory.Name);
                Assert.Equal("inlinecategories_scheme", firstCategory.Scheme);
                Assert.Null(firstCategory.Label);

                SyndicationCategory secondCategory = document.Categories[1];
                Assert.Equal(4, secondCategory.AttributeExtensions.Count);
                Assert.Equal("", secondCategory.AttributeExtensions[new XmlQualifiedName("category_name1")]);
                Assert.Equal("", secondCategory.AttributeExtensions[new XmlQualifiedName("category_name2", "category_namespace")]);
                Assert.Equal("category_value", secondCategory.AttributeExtensions[new XmlQualifiedName("category_name3", "category_namespace")]);
                Assert.Equal("", secondCategory.AttributeExtensions[new XmlQualifiedName("category_name4", "xmlns")]);
                Assert.Equal(1, secondCategory.ElementExtensions.Count);
                Assert.Equal(10, secondCategory.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                Assert.Equal("category_name", secondCategory.Name);
                Assert.Equal("category_scheme", secondCategory.Scheme);
                Assert.Equal("category_label", secondCategory.Label);
            });
        }

        [Fact]
        public void Read_InlineCategoriesDocumentTryParseTrue_ReturnsExpected()
        {
            using (var stringReader = new StringReader(
@"<app:categories xmlns:a10=""http://www.w3.org/2005/Atom"" xml:base=""http://inlinecategories_url.com/"" xml:lang=""inlinecategories_Language"" scheme=""inlinecategories_scheme"" fixed=""yes"" inlinecategories_name1="""" d1p1:inlinecategories_name2="""" d1p1:inlinecategories_name3=""inlinecategories_value"" d1p2:inlinecategories_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""inlinecategories_namespace"" xmlns:app=""http://www.w3.org/2007/app"">
    <a10:category term="""" />
    <a10:category category_name1="""" d2p1:category_name2="""" d2p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d2p1=""category_namespace"">
        <AtomPub10CategoriesDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </AtomPub10CategoriesDocumentFormatterTests.ExtensionObject>
    </a10:category>
    <AtomPub10CategoriesDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </AtomPub10CategoriesDocumentFormatterTests.ExtensionObject>
</app:categories>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10CategoriesDocumentFormatter(typeof(InlineCategoriesDocumentTryParseTrueSubclass), typeof(ReferencedCategoriesDocumentTryParseTrueSubclass));
                formatter.ReadFrom(reader);

                InlineCategoriesDocumentTryParseTrueSubclass document = Assert.IsType<InlineCategoriesDocumentTryParseTrueSubclass>(formatter.Document);
                Assert.Empty(document.AttributeExtensions);
                Assert.Equal(new Uri("http://inlinecategories_url.com/"), document.BaseUri);
                Assert.Equal(2, document.Categories.Count);
                Assert.Empty(document.ElementExtensions);
                Assert.True(document.IsFixed);
                Assert.Equal("inlinecategories_Language", document.Language);
                Assert.Equal("inlinecategories_scheme", document.Scheme);

                SyndicationCategory firstCategory = document.Categories[0];
                Assert.Empty(firstCategory.AttributeExtensions);
                Assert.Empty(firstCategory.ElementExtensions);
                Assert.Empty(firstCategory.Name);
                Assert.Equal("inlinecategories_scheme", firstCategory.Scheme);
                Assert.Null(firstCategory.Label);

                SyndicationCategory secondCategory = document.Categories[1];
                Assert.Empty(secondCategory.AttributeExtensions);
                Assert.Empty(secondCategory.ElementExtensions);
                Assert.Equal("category_name", secondCategory.Name);
                Assert.Equal("category_scheme", secondCategory.Scheme);
                Assert.Equal("category_label", secondCategory.Label);
            }
        }

        [Fact]
        public void ReadFrom_ReferencedCategoriesDocument_ReturnsExpected()
        {
            string xmlString =
@"<app:categories xmlns:a10=""http://www.w3.org/2005/Atom"" xml:base=""http://referencecategories_url.com/"" xml:lang=""referencecategories_language"" href=""http://referencecategories_link.com/"" referencecategories_name1="""" d1p1:referencecategories_name2="""" d1p1:referencecategories_name3=""referencecategories_value"" d1p2:referencecategories_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""referencecategories_namespace"" xmlns:app=""http://www.w3.org/2007/app"">
    <AtomPub10CategoriesDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </AtomPub10CategoriesDocumentFormatterTests.ExtensionObject>
</app:categories>";
            VerifyRead<ReferencedCategoriesDocument>(xmlString, document =>
            {
                Assert.Equal(4, document.AttributeExtensions.Count);
                Assert.Equal("", document.AttributeExtensions[new XmlQualifiedName("referencecategories_name1")]);
                Assert.Equal("", document.AttributeExtensions[new XmlQualifiedName("referencecategories_name2", "referencecategories_namespace")]);
                Assert.Equal("referencecategories_value", document.AttributeExtensions[new XmlQualifiedName("referencecategories_name3", "referencecategories_namespace")]);
                Assert.Equal("", document.AttributeExtensions[new XmlQualifiedName("referencecategories_name4", "xmlns")]);
                Assert.Equal(new Uri("http://referencecategories_url.com/"), document.BaseUri);
                Assert.Equal(1, document.ElementExtensions.Count);
                Assert.Equal(10, document.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                Assert.Equal("referencecategories_language", document.Language);
                Assert.Equal(new Uri("http://referencecategories_link.com"), document.Link);
            });
        }

        [Fact]
        public void ReadFrom_ReferencedCategoriesDocumentTryParseTrue_ReturnsExpected()
        {
            using (var stringReader = new StringReader(
@"<app:categories xmlns:a10=""http://www.w3.org/2005/Atom"" xml:base=""http://referencecategories_url.com/"" xml:lang=""referencecategories_language"" href=""http://referencecategories_link.com/"" referencecategories_name1="""" d1p1:referencecategories_name2="""" d1p1:referencecategories_name3=""referencecategories_value"" d1p2:referencecategories_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""referencecategories_namespace"" xmlns:app=""http://www.w3.org/2007/app"">
    <AtomPub10CategoriesDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </AtomPub10CategoriesDocumentFormatterTests.ExtensionObject>
</app:categories>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10CategoriesDocumentFormatter(typeof(InlineCategoriesDocumentTryParseTrueSubclass), typeof(ReferencedCategoriesDocumentTryParseTrueSubclass));
                formatter.ReadFrom(reader);

                ReferencedCategoriesDocumentTryParseTrueSubclass document = Assert.IsType<ReferencedCategoriesDocumentTryParseTrueSubclass>(formatter.Document);
                Assert.Empty(document.AttributeExtensions);
                Assert.Equal(new Uri("http://referencecategories_url.com/"), document.BaseUri);
                Assert.Empty(document.ElementExtensions);
                Assert.Equal("referencecategories_language", document.Language);
                Assert.Equal(new Uri("http://referencecategories_link.com"), document.Link);
            }
        }

        [Fact]
        public void ReadFrom_EmptyCategory_ReturnsExpected()
        {
            VerifyRead<InlineCategoriesDocument>(@"<app:categories xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app"" />", document =>
            {
                Assert.Empty(document.AttributeExtensions);
                Assert.Null(document.BaseUri);
                Assert.Empty(document.Categories);
                Assert.Empty(document.ElementExtensions);
                Assert.False(document.IsFixed);
                Assert.Null(document.Language);
                Assert.Null(document.Scheme);
            });
        }

        private static void VerifyRead<T>(string xmlString, Action<T> verifyAction) where T : CategoriesDocument
        {
            // ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10CategoriesDocumentFormatter();
                formatter.ReadFrom(reader);

                T document = Assert.IsType<T>(formatter.Document);
                verifyAction(document);
            }

            // ReadFrom with custom subclass.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10CategoriesDocumentFormatter(typeof(InlineCategoriesDocumentSubclass), typeof(ReferencedCategoriesDocumentSubclass));
                formatter.ReadFrom(reader);

                if (typeof(T) == typeof(InlineCategoriesDocument))
                {
                    InlineCategoriesDocumentSubclass document = Assert.IsType<InlineCategoriesDocumentSubclass>(formatter.Document);
                    verifyAction(document as T);
                }
                else
                {
                    ReferencedCategoriesDocumentSubclass document = Assert.IsType<ReferencedCategoriesDocumentSubclass>(formatter.Document);
                    verifyAction(document as T);
                }
            }

            // ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new AtomPub10CategoriesDocumentFormatter();
                ((IXmlSerializable)formatter).ReadXml(reader);

                T document = Assert.IsType<T>(formatter.Document);
                verifyAction(document);
            }

            // ReadXml with custom subclass.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new AtomPub10CategoriesDocumentFormatter(typeof(InlineCategoriesDocumentSubclass), typeof(ReferencedCategoriesDocumentSubclass));
                ((IXmlSerializable)formatter).ReadXml(reader);

                if (typeof(T) == typeof(InlineCategoriesDocument))
                {
                    InlineCategoriesDocumentSubclass document = Assert.IsType<InlineCategoriesDocumentSubclass>(formatter.Document);
                    verifyAction(document as T);
                }
                else
                {
                    ReferencedCategoriesDocumentSubclass document = Assert.IsType<ReferencedCategoriesDocumentSubclass>(formatter.Document);
                    verifyAction(document as T);
                }
            }

            // Load.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                T document = Assert.IsType<T>(CategoriesDocument.Load(reader));
                verifyAction(document);
            }
        }

        [Fact]
        public void ReadFrom_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new AtomPub10CategoriesDocumentFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.ReadFrom(null));
        }

        [Theory]
        [InlineData(@"<app:different xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app"" />")]
        [InlineData(@"<categories xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app"" />")]
        public void ReadFrom_CantRead_ThrowsXmlException(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10CategoriesDocumentFormatter();
                Assert.Throws<XmlException>(() => formatter.ReadFrom(reader));
            }
        }

        [Fact]
        public void ReadXml_ThrowsArgumentException_RethrowsAsXmlException()
        {
            var reader = new ThrowingXmlReader(new ArgumentException());
            IXmlSerializable formatter = new AtomPub10CategoriesDocumentFormatter();
            Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
        }

        [Fact]
        public void ReadXml_ThrowsFormatException_RethrowsAsXmlException()
        {
            var reader = new ThrowingXmlReader(new FormatException());
            IXmlSerializable formatter = new AtomPub10CategoriesDocumentFormatter();
            Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
        }

        [Fact]
        public void ReadXml_NullReader_ThrowsArgumentNullException()
        {
            IXmlSerializable formatter = new AtomPub10CategoriesDocumentFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.ReadXml(null));
        }

        [Fact]
        public void CreateInlineCategoriesDocument_NonGeneric_Success()
        {
            var formatter = new Formatter();
            InlineCategoriesDocument document = Assert.IsType<InlineCategoriesDocument>(formatter.CreateInlineCategoriesDocumentEntryPoint());
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.Categories);
            Assert.Empty(document.ElementExtensions);
            Assert.False(document.IsFixed);
            Assert.Null(document.Language);
            Assert.Null(document.Scheme);

            var typedFormatter = new Formatter(typeof(InlineCategoriesDocumentSubclass), typeof(ReferencedCategoriesDocumentSubclass));
            document = Assert.IsType<InlineCategoriesDocumentSubclass>(typedFormatter.CreateInlineCategoriesDocumentEntryPoint());
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.Categories);
            Assert.Empty(document.ElementExtensions);
            Assert.False(document.IsFixed);
            Assert.Null(document.Language);
            Assert.Null(document.Scheme);
        }

        [Fact]
        public void CreateReferencedCategoriesDocument_NonGeneric_Success()
        {
            var formatter = new Formatter();
            ReferencedCategoriesDocument document = Assert.IsType<ReferencedCategoriesDocument>(formatter.CreateReferencedCategoriesDocumentEntryPoint());
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Null(document.Link);

            var typedFormatter = new Formatter(typeof(InlineCategoriesDocumentSubclass), typeof(ReferencedCategoriesDocumentSubclass));
            document = Assert.IsType<ReferencedCategoriesDocumentSubclass>(typedFormatter.CreateReferencedCategoriesDocumentEntryPoint());
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Null(document.Link);
        }

        public class InlineCategoriesDocumentSubclass : InlineCategoriesDocument { }

        public class ReferencedCategoriesDocumentSubclass : ReferencedCategoriesDocument { }

        public class InlineCategoriesDocumentTryParseTrueSubclass : InlineCategoriesDocument
        {
            protected override bool TryParseAttribute(string name, string ns, string value, string version) => true;

            protected override bool TryParseElement(XmlReader reader, string version)
            {
                reader.Skip();
                return true;
            }

            protected override SyndicationCategory CreateCategory() => new SyndicationCategoryTryParseTrueSubclass();
        }

        public class SyndicationCategoryTryParseTrueSubclass : SyndicationCategory
        {
            protected override bool TryParseAttribute(string name, string ns, string value, string version) => true;

            protected override bool TryParseElement(XmlReader reader, string version)
            {
                reader.Skip();
                return true;
            }
        }

        public class ReferencedCategoriesDocumentTryParseTrueSubclass : ReferencedCategoriesDocument
        {
            protected override bool TryParseAttribute(string name, string ns, string value, string version) => true;

            protected override bool TryParseElement(XmlReader reader, string version)
            {
                reader.Skip();
                return true;
            }
        }

        public class Formatter : AtomPub10CategoriesDocumentFormatter
        {
            public Formatter() : base() { }

            public Formatter(CategoriesDocument documentToWrite) : base(documentToWrite) { }

            public Formatter(Type inlineDocumentType, Type referencedDocumentType) : base(inlineDocumentType, referencedDocumentType) { }

            public InlineCategoriesDocument CreateInlineCategoriesDocumentEntryPoint() => CreateInlineCategoriesDocument();

            public ReferencedCategoriesDocument CreateReferencedCategoriesDocumentEntryPoint() => CreateReferencedCategoriesDocument();
        }

        [DataContract]
        public class ExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }
    }
}
