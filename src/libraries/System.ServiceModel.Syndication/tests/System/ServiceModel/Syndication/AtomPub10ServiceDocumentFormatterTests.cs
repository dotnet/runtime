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
    public class AtomPub10ServiceDocumentFormatterTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var formatter = new AtomPub10ServiceDocumentFormatter();
            Assert.Null(formatter.Document);
            Assert.Equal("http://www.w3.org/2007/app", formatter.Version);
        }

        [Fact]
        public void Ctor_GenericDefault()
        {
            var formatter = new AtomPub10ServiceDocumentFormatter<ServiceDocument>();
            Assert.Null(formatter.Document);
            Assert.Equal("http://www.w3.org/2007/app", formatter.Version);
        }

        [Fact]
        public void Ctor_ServiceDocument()
        {
            var document = new ServiceDocument();
            var formatter = new AtomPub10ServiceDocumentFormatter(document);
            Assert.Same(document, formatter.Document);
            Assert.Equal("http://www.w3.org/2007/app", formatter.Version);
        }

        [Fact]
        public void Ctor_GenericServiceDocument()
        {
            var document = new ServiceDocument();
            var formatter = new AtomPub10ServiceDocumentFormatter<ServiceDocument>(document);
            Assert.Same(document, formatter.Document);
            Assert.Equal("http://www.w3.org/2007/app", formatter.Version);
        }

        [Fact]
        public void Ctor_NullDocumentToWrite_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("documentToWrite", () => new AtomPub10ServiceDocumentFormatter((ServiceDocument)null));
            AssertExtensions.Throws<ArgumentNullException>("documentToWrite", () => new AtomPub10ServiceDocumentFormatter<ServiceDocument>(null));
        }

        [Theory]
        [InlineData(typeof(ServiceDocument))]
        [InlineData(typeof(ServiceDocumentSubclass))]
        public void Ctor_Type(Type documentTypeToCreate)
        {
            var formatter = new AtomPub10ServiceDocumentFormatter(documentTypeToCreate);
            Assert.Null(formatter.Document);
            Assert.Equal("http://www.w3.org/2007/app", formatter.Version);
        }

        [Fact]
        public void Ctor_NullDocumentTypeToCreate_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("documentTypeToCreate", () => new AtomPub10ServiceDocumentFormatter((Type)null));
        }

        [Fact]
        public void Ctor_InvalidDocumentTypeToCreate_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("documentTypeToCreate", () => new AtomPub10ServiceDocumentFormatter(typeof(int)));
        }

        [Fact]
        public void GetSchema_Invoke_ReturnsNull()
        {
            IXmlSerializable formatter = new AtomPub10ServiceDocumentFormatter();
            Assert.Null(formatter.GetSchema());
        }

        public static IEnumerable<object[]> WriteTo_TestData()
        {
            // Empty document.
            yield return new object[]
            {
                new ServiceDocument(),
                @"<app:service xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app"" />"
            };

            // Full document
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

            var fullResourceCollectionInfoTitle = new TextSyndicationContent("resourcecollectioninfo_title", TextSyndicationContentKind.XHtml);
            fullResourceCollectionInfoTitle.AttributeExtensions.Add(new XmlQualifiedName("resourcecollectioninfotitle_name1"), null);
            fullResourceCollectionInfoTitle.AttributeExtensions.Add(new XmlQualifiedName("resourcecollectioninfotitle_name2", "resourcecollectioninfotitle_namespace"), "");
            fullResourceCollectionInfoTitle.AttributeExtensions.Add(new XmlQualifiedName("resourcecollectioninfotitle_name3", "resourcecollectioninfotitle_namespace"), "resourcecollectioninfotitle_value");
            fullResourceCollectionInfoTitle.AttributeExtensions.Add(new XmlQualifiedName("resourcecollectioninfotitle_name4", "xmlns"), "");

            var fullResourceCollectionInfo = new ResourceCollectionInfo(fullResourceCollectionInfoTitle, new Uri("http://resourcecollectioninfo_link.com"), new CategoriesDocument[]
            {
                new InlineCategoriesDocument(),
                fullInlineCategoriesDocument,
                new ReferencedCategoriesDocument(),
                fullReferenceCategoriesDocument
            }, new string[] { "text/html", "image/*", "" })
            {
                BaseUri = new Uri("http://resourcecollectioninfo_url.com")
            };
            fullResourceCollectionInfo.AttributeExtensions.Add(new XmlQualifiedName("resourcecollectioninfo_name1"), null);
            fullResourceCollectionInfo.AttributeExtensions.Add(new XmlQualifiedName("resourcecollectioninfo_name2", "resourcecollectioninfo_namespace"), "");
            fullResourceCollectionInfo.AttributeExtensions.Add(new XmlQualifiedName("resourcecollectioninfo_name3", "resourcecollectioninfo_namespace"), "resourcecollectioninfo_value");
            fullResourceCollectionInfo.AttributeExtensions.Add(new XmlQualifiedName("resourcecollectioninfo_name4", "xmlns"), "");

            fullResourceCollectionInfo.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            var fullWorkspaceTitle = new TextSyndicationContent("workspace_title", TextSyndicationContentKind.Html);
            fullWorkspaceTitle.AttributeExtensions.Add(new XmlQualifiedName("workspacetitle_name1"), null);
            fullWorkspaceTitle.AttributeExtensions.Add(new XmlQualifiedName("workspacetitle_name2", "workspacetitle_namespace"), "");
            fullWorkspaceTitle.AttributeExtensions.Add(new XmlQualifiedName("workspacetitle_name3", "workspacetitle_namespace"), "workspacetitle_value");
            fullWorkspaceTitle.AttributeExtensions.Add(new XmlQualifiedName("workspacetitle_name4", "xmlns"), "");

            var fullWorkspace = new Workspace(fullWorkspaceTitle, new ResourceCollectionInfo[]
            {
                new ResourceCollectionInfo(),
                fullResourceCollectionInfo,
            })
            {
                BaseUri = new Uri("http://workspace_url.com"),
            };
            fullWorkspace.AttributeExtensions.Add(new XmlQualifiedName("workspace_name1"), null);
            fullWorkspace.AttributeExtensions.Add(new XmlQualifiedName("workspace_name2", "workspace_namespace"), "");
            fullWorkspace.AttributeExtensions.Add(new XmlQualifiedName("workspace_name3", "workspace_namespace"), "workspace_value");
            fullWorkspace.AttributeExtensions.Add(new XmlQualifiedName("workspace_name4", "xmlns"), "");

            fullWorkspace.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            var nullWorkspace = new Workspace(new TextSyndicationContent(null), null);

            var fullDocument = new ServiceDocument()
            {
                Language = "document_language",
                BaseUri = new Uri("http://document_url.com")
            };
            fullDocument.AttributeExtensions.Add(new XmlQualifiedName("document_name1"), null);
            fullDocument.AttributeExtensions.Add(new XmlQualifiedName("document_name2", "document_namespace"), "");
            fullDocument.AttributeExtensions.Add(new XmlQualifiedName("document_name3", "document_namespace"), "document_value");
            fullDocument.AttributeExtensions.Add(new XmlQualifiedName("document_name4", "xmlns"), "");

            fullDocument.ElementExtensions.Add(new ExtensionObject { Value = 10 });
    
            fullDocument.Workspaces.Add(new Workspace());
            fullDocument.Workspaces.Add(fullWorkspace);
            fullDocument.Workspaces.Add(nullWorkspace);

            yield return new object[]
            {
                fullDocument,
@"<app:service xmlns:a10=""http://www.w3.org/2005/Atom"" xml:lang=""document_language"" xml:base=""http://document_url.com/"" document_name1="""" d1p1:document_name2="""" d1p1:document_name3=""document_value"" d1p2:document_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""document_namespace"" xmlns:app=""http://www.w3.org/2007/app"">
    <app:workspace />
    <app:workspace xml:base=""http://workspace_url.com/"" workspace_name1="""" d2p1:workspace_name2="""" d2p1:workspace_name3=""workspace_value"" d1p2:workspace_name4="""" xmlns:d2p1=""workspace_namespace"">
        <a10:title type=""html"" workspacetitle_name1="""" d3p1:workspacetitle_name2="""" d3p1:workspacetitle_name3=""workspacetitle_value"" d1p2:workspacetitle_name4="""" xmlns:d3p1=""workspacetitle_namespace"">workspace_title</a10:title>
        <app:collection />
        <app:collection xml:base=""http://resourcecollectioninfo_url.com/"" href=""http://resourcecollectioninfo_link.com/"" resourcecollectioninfo_name1="""" d3p1:resourcecollectioninfo_name2="""" d3p1:resourcecollectioninfo_name3=""resourcecollectioninfo_value"" d1p2:resourcecollectioninfo_name4="""" xmlns:d3p1=""resourcecollectioninfo_namespace"">
            <a10:title type=""xhtml"" resourcecollectioninfotitle_name1="""" d4p1:resourcecollectioninfotitle_name2="""" d4p1:resourcecollectioninfotitle_name3=""resourcecollectioninfotitle_value"" d1p2:resourcecollectioninfotitle_name4="""" xmlns:d4p1=""resourcecollectioninfotitle_namespace"">resourcecollectioninfo_title</a10:title>
            <app:accept>text/html</app:accept>
            <app:accept>image/*</app:accept>
            <app:accept />
            <app:categories />
            <app:categories xml:base=""http://inlinecategories_url.com/"" xml:lang=""inlinecategories_Language"" scheme=""inlinecategories_scheme"" fixed=""yes"" inlinecategories_name1="""" d4p1:inlinecategories_name2="""" d4p1:inlinecategories_name3=""inlinecategories_value"" d1p2:inlinecategories_name4="""" xmlns:d4p1=""inlinecategories_namespace"">
                <a10:category term="""" />
                <a10:category category_name1="""" d5p1:category_name2="""" d5p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d5p1=""category_namespace"">
                    <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                        <Value>10</Value>
                    </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
                </a10:category>
                <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
            </app:categories>
            <app:categories />
            <app:categories xml:base=""http://referencecategories_url.com/"" xml:lang=""referencecategories_language"" href=""http://referencecategories_link.com/"" referencecategories_name1="""" d4p1:referencecategories_name2="""" d4p1:referencecategories_name3=""referencecategories_value"" d1p2:referencecategories_name4="""" xmlns:d4p1=""referencecategories_namespace"">
                <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
            </app:categories>
            <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
        </app:collection>
        <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
    </app:workspace>
    <app:workspace>
        <a10:title type=""text"" />
    </app:workspace>
    <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
</app:service>"
        };
        }

        [Theory]
        [MemberData(nameof(WriteTo_TestData))]
        public void WriteTo_HasDocument_SerializesExpected(ServiceDocument document, string expected)
        {
            var formatter = new AtomPub10ServiceDocumentFormatter(document);
            CompareHelper.AssertEqualWriteOutput(expected, writer => formatter.WriteTo(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer => document.Save(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer =>
            {
                writer.WriteStartElement("app", "service", "http://www.w3.org/2007/app");
                ((IXmlSerializable)formatter).WriteXml(writer);
                writer.WriteEndElement();
            });

            var genericFormatter = new AtomPub10ServiceDocumentFormatter<ServiceDocument>(document);
            CompareHelper.AssertEqualWriteOutput(expected, writer => formatter.WriteTo(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer => document.Save(writer));
            CompareHelper.AssertEqualWriteOutput(expected, writer =>
            {
                writer.WriteStartElement("app", "service", "http://www.w3.org/2007/app");
                ((IXmlSerializable)formatter).WriteXml(writer);
                writer.WriteEndElement();
            });
        }

        [Fact]
        public void WriteTo_NullWriter_ThrowsArgumentNullException()
        {
            var formatter = new AtomPub10ServiceDocumentFormatter();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => formatter.WriteTo(null));
        }

        [Fact]
        public void WriteTo_NoDocument_ThrowsInvalidOperationException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new AtomPub10ServiceDocumentFormatter();
                Assert.Throws<InvalidOperationException>(() => formatter.WriteTo(writer));
            }
        }

        [Fact]
        public void WriteXml_NullWriter_ThrowsArgumentNullException()
        {
            IXmlSerializable formatter = new AtomPub10ServiceDocumentFormatter();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => formatter.WriteXml(null));
        }

        [Fact]
        public void WriteXml_NoDocument_ThrowsInvalidOperationException()
        {
            using (var stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter))
            {
                IXmlSerializable formatter = new AtomPub10ServiceDocumentFormatter();
                Assert.Throws<InvalidOperationException>(() => formatter.WriteXml(writer));
            }
        }

        public static IEnumerable<object[]> CanRead_TestData()
        {
            yield return new object[] { @"<service xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app"" />", false };
            yield return new object[] { @"<app:different xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app"" />", false };
            yield return new object[] { @"<app:service xmlns:app=""http://www.w3.org/2007/app"" />", true };
            yield return new object[] { @"<app:service xmlns:app=""http://www.w3.org/2007/app""></app:service>", true };
        }

        [Theory]
        [MemberData(nameof(CanRead_TestData))]
        public void CanRead_ValidReader_ReturnsExpected(string xmlString, bool expected)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10ServiceDocumentFormatter();
                Assert.Equal(expected, formatter.CanRead(reader));
            }
        }

        [Fact]
        public void CanRead_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new AtomPub10ServiceDocumentFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.CanRead(null));
        }

        [Fact]
        public void ReadFrom_FullDocument_ReturnsExpected()
        {
            string xmlString = @"<app:service xmlns:a10=""http://www.w3.org/2005/Atom"" xml:lang=""document_language"" xml:base=""http://document_url.com/"" document_name1="""" d1p1:document_name2="""" d1p1:document_name3=""document_value"" d1p2:document_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""document_namespace"" xmlns:app=""http://www.w3.org/2007/app"">
    <app:workspace></app:workspace>
    <app:workspace xml:base=""http://workspace_url.com/"" workspace_name1="""" d2p1:workspace_name2="""" d2p1:workspace_name3=""workspace_value"" d1p2:workspace_name4="""" xmlns:d2p1=""workspace_namespace"">
        <a10:title type=""html"" workspacetitle_name1="""" d3p1:workspacetitle_name2="""" d3p1:workspacetitle_name3=""workspacetitle_value"" d1p2:workspacetitle_name4="""" xmlns:d3p1=""workspacetitle_namespace"">workspace_title</a10:title>
        <app:collection></app:collection>
        <app:collection xml:base=""http://resourcecollectioninfo_url.com/"" href=""http://resourcecollectioninfo_link.com/"" resourcecollectioninfo_name1="""" d3p1:resourcecollectioninfo_name2="""" d3p1:resourcecollectioninfo_name3=""resourcecollectioninfo_value"" d1p2:resourcecollectioninfo_name4="""" xmlns:d3p1=""resourcecollectioninfo_namespace"">
            <a10:title type=""xhtml"" resourcecollectioninfotitle_name1="""" d4p1:resourcecollectioninfotitle_name2="""" d4p1:resourcecollectioninfotitle_name3=""resourcecollectioninfotitle_value"" d1p2:resourcecollectioninfotitle_name4="""" xmlns:d4p1=""resourcecollectioninfotitle_namespace"">resourcecollectioninfo_title</a10:title>
            <app:accept>text/html</app:accept>
            <app:accept>image/*</app:accept>
            <app:accept></app:accept>
            <app:categories />
            <app:categories xml:base=""http://inlinecategories_url.com/"" xml:lang=""inlinecategories_Language"" scheme=""inlinecategories_scheme"" fixed=""yes"" inlinecategories_name1="""" d4p1:inlinecategories_name2="""" d4p1:inlinecategories_name3=""inlinecategories_value"" d1p2:inlinecategories_name4="""" xmlns:d4p1=""inlinecategories_namespace"">
                <a10:category term="""" />
                <a10:category category_name1="""" d5p1:category_name2="""" d5p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d5p1=""category_namespace"">
                    <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                        <Value>10</Value>
                    </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
                </a10:category>
                <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
            </app:categories>
            <app:categories></app:categories>
            <app:categories xml:base=""http://referencecategories_url.com/"" xml:lang=""referencecategories_language"" href=""http://referencecategories_link.com/"" referencecategories_name1="""" d4p1:referencecategories_name2="""" d4p1:referencecategories_name3=""referencecategories_value"" d1p2:referencecategories_name4="""" xmlns:d4p1=""referencecategories_namespace"">
                <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
            </app:categories>
            <app:categories xml:base=""/relative_link"" href=""http://emptyelement_link.com/"" />
            <app:categories xml:base=""/relative_link"" href=""http://emptyelement_link.com/""></app:categories>
            <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
        </app:collection>
        <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
    </app:workspace>
    <app:workspace>
        <a10:title type=""text"" />
    </app:workspace>
    <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
</app:service>";
            VerifyRead(xmlString, document =>
            {
                Assert.Equal(4, document.AttributeExtensions.Count);
                Assert.Equal("", document.AttributeExtensions[new XmlQualifiedName("document_name1")]);
                Assert.Equal("", document.AttributeExtensions[new XmlQualifiedName("document_name2", "document_namespace")]);
                Assert.Equal("document_value", document.AttributeExtensions[new XmlQualifiedName("document_name3", "document_namespace")]);
                Assert.Equal("", document.AttributeExtensions[new XmlQualifiedName("document_name4", "xmlns")]);
                Assert.Equal(new Uri("http://document_url.com"), document.BaseUri);
                Assert.Equal(1, document.ElementExtensions.Count);
                Assert.Equal(10, document.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                Assert.Equal("document_language", document.Language);
                Assert.Equal(3, document.Workspaces.Count);

                Workspace firstWorkspace = document.Workspaces[0];
                Assert.Empty(firstWorkspace.AttributeExtensions);
                Assert.Equal(new Uri("http://document_url.com"), firstWorkspace.BaseUri);
                Assert.Empty(firstWorkspace.Collections);
                Assert.Empty(firstWorkspace.ElementExtensions);
                Assert.Null(firstWorkspace.Title);

                Workspace secondWorkspace = document.Workspaces[1];
                Assert.Equal(4, secondWorkspace.AttributeExtensions.Count);
                Assert.Equal("", secondWorkspace.AttributeExtensions[new XmlQualifiedName("workspace_name1")]);
                Assert.Equal("", secondWorkspace.AttributeExtensions[new XmlQualifiedName("workspace_name2", "workspace_namespace")]);
                Assert.Equal("workspace_value", secondWorkspace.AttributeExtensions[new XmlQualifiedName("workspace_name3", "workspace_namespace")]);
                Assert.Equal("", secondWorkspace.AttributeExtensions[new XmlQualifiedName("workspace_name4", "xmlns")]);
                Assert.Equal(new Uri("http://workspace_url.com"), secondWorkspace.BaseUri);
                Assert.Equal(2, secondWorkspace.Collections.Count);
                Assert.Equal(1, secondWorkspace.ElementExtensions.Count);
                Assert.Equal(10, secondWorkspace.ElementExtensions[0].GetObject<ExtensionObject>().Value);

                TextSyndicationContent workspaceTitle = secondWorkspace.Title;
                Assert.Equal(4, workspaceTitle.AttributeExtensions.Count);
                Assert.Equal("", workspaceTitle.AttributeExtensions[new XmlQualifiedName("workspacetitle_name1")]);
                Assert.Equal("", workspaceTitle.AttributeExtensions[new XmlQualifiedName("workspacetitle_name2", "workspacetitle_namespace")]);
                Assert.Equal("workspacetitle_value", workspaceTitle.AttributeExtensions[new XmlQualifiedName("workspacetitle_name3", "workspacetitle_namespace")]);
                Assert.Equal("", workspaceTitle.AttributeExtensions[new XmlQualifiedName("workspacetitle_name4", "xmlns")]);
                Assert.Equal("workspace_title", workspaceTitle.Text);
                Assert.Equal("html", workspaceTitle.Type);

                ResourceCollectionInfo firstCollection = secondWorkspace.Collections[0];
                Assert.Empty(firstCollection.Accepts);
                Assert.Empty(firstCollection.AttributeExtensions);
                Assert.Equal(new Uri("http://workspace_url.com/"), firstCollection.BaseUri);
                Assert.Empty(firstCollection.Categories);
                Assert.Empty(firstCollection.ElementExtensions);
                Assert.Null(firstCollection.Link);
                Assert.Null(firstCollection.Title);

                ResourceCollectionInfo secondCollection = secondWorkspace.Collections[1];
                Assert.Equal(new string[] { "text/html", "image/*", "" }, secondCollection.Accepts);
                Assert.Equal(4, secondCollection.AttributeExtensions.Count);
                Assert.Equal("", secondCollection.AttributeExtensions[new XmlQualifiedName("resourcecollectioninfo_name1")]);
                Assert.Equal("", secondCollection.AttributeExtensions[new XmlQualifiedName("resourcecollectioninfo_name2", "resourcecollectioninfo_namespace")]);
                Assert.Equal("resourcecollectioninfo_value", secondCollection.AttributeExtensions[new XmlQualifiedName("resourcecollectioninfo_name3", "resourcecollectioninfo_namespace")]);
                Assert.Equal("", secondCollection.AttributeExtensions[new XmlQualifiedName("resourcecollectioninfo_name4", "xmlns")]);
                Assert.Equal(new Uri("http://resourcecollectioninfo_url.com"), secondCollection.BaseUri);
                Assert.Equal(6, secondCollection.Categories.Count);
                Assert.Equal(1, secondCollection.ElementExtensions.Count);
                Assert.Equal(10, secondCollection.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                Assert.Equal(new Uri("http://resourcecollectioninfo_link.com"), secondCollection.Link);

                TextSyndicationContent collectionTitle = secondCollection.Title;
                Assert.Equal(4, collectionTitle.AttributeExtensions.Count);
                Assert.Equal("", collectionTitle.AttributeExtensions[new XmlQualifiedName("resourcecollectioninfotitle_name1")]);
                Assert.Equal("", collectionTitle.AttributeExtensions[new XmlQualifiedName("resourcecollectioninfotitle_name2", "resourcecollectioninfotitle_namespace")]);
                Assert.Equal("resourcecollectioninfotitle_value", collectionTitle.AttributeExtensions[new XmlQualifiedName("resourcecollectioninfotitle_name3", "resourcecollectioninfotitle_namespace")]);
                Assert.Equal("", collectionTitle.AttributeExtensions[new XmlQualifiedName("resourcecollectioninfotitle_name4", "xmlns")]);
                Assert.Equal("resourcecollectioninfo_title", collectionTitle.Text);
                Assert.Equal("xhtml", collectionTitle.Type);

                InlineCategoriesDocument firstDocument = Assert.IsType<InlineCategoriesDocument>(secondCollection.Categories[0]);
                Assert.Empty(firstDocument.AttributeExtensions);
                Assert.Equal(new Uri("http://resourcecollectioninfo_url.com/"), firstDocument.BaseUri);
                Assert.Empty(firstDocument.Categories);
                Assert.Empty(firstDocument.ElementExtensions);
                Assert.False(firstDocument.IsFixed);
                Assert.Null(firstDocument.Language);
                Assert.Null(firstDocument.Scheme);

                InlineCategoriesDocument secondDocument = Assert.IsType<InlineCategoriesDocument>(secondCollection.Categories[1]);
                Assert.Equal(4, secondDocument.AttributeExtensions.Count);
                Assert.Equal("", secondDocument.AttributeExtensions[new XmlQualifiedName("inlinecategories_name1")]);
                Assert.Equal("", secondDocument.AttributeExtensions[new XmlQualifiedName("inlinecategories_name2", "inlinecategories_namespace")]);
                Assert.Equal("inlinecategories_value", secondDocument.AttributeExtensions[new XmlQualifiedName("inlinecategories_name3", "inlinecategories_namespace")]);
                Assert.Equal("", secondDocument.AttributeExtensions[new XmlQualifiedName("inlinecategories_name4", "xmlns")]);
                Assert.Equal(new Uri("http://inlinecategories_url.com/"), secondDocument.BaseUri);
                Assert.Equal(2, secondDocument.Categories.Count);
                Assert.Equal(1, secondDocument.ElementExtensions.Count);
                Assert.Equal(10, secondDocument.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                Assert.True(secondDocument.IsFixed);
                Assert.Equal("inlinecategories_Language", secondDocument.Language);
                Assert.Equal("inlinecategories_scheme", secondDocument.Scheme);

                SyndicationCategory secondDocumentFirstCategory = secondDocument.Categories[0];
                Assert.Empty(secondDocumentFirstCategory.AttributeExtensions);
                Assert.Empty(secondDocumentFirstCategory.ElementExtensions);
                Assert.Empty(secondDocumentFirstCategory.Name);
                Assert.Equal("inlinecategories_scheme", secondDocumentFirstCategory.Scheme);
                Assert.Null(secondDocumentFirstCategory.Label);

                SyndicationCategory secondDocumentSecondCategory = secondDocument.Categories[1];
                Assert.Equal(4, secondDocumentSecondCategory.AttributeExtensions.Count);
                Assert.Equal("", secondDocumentSecondCategory.AttributeExtensions[new XmlQualifiedName("category_name1")]);
                Assert.Equal("", secondDocumentSecondCategory.AttributeExtensions[new XmlQualifiedName("category_name2", "category_namespace")]);
                Assert.Equal("category_value", secondDocumentSecondCategory.AttributeExtensions[new XmlQualifiedName("category_name3", "category_namespace")]);
                Assert.Equal("", secondDocumentSecondCategory.AttributeExtensions[new XmlQualifiedName("category_name4", "xmlns")]);
                Assert.Equal(1, secondDocumentSecondCategory.ElementExtensions.Count);
                Assert.Equal(10, secondDocumentSecondCategory.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                Assert.Equal("category_name", secondDocumentSecondCategory.Name);
                Assert.Equal("category_scheme", secondDocumentSecondCategory.Scheme);
                Assert.Equal("category_label", secondDocumentSecondCategory.Label);

                InlineCategoriesDocument thirdDocument = Assert.IsType<InlineCategoriesDocument>(secondCollection.Categories[2]);
                Assert.Empty(secondDocumentFirstCategory.AttributeExtensions);
                Assert.Empty(secondDocumentFirstCategory.ElementExtensions);
                Assert.Empty(secondDocumentFirstCategory.Name);
                Assert.Equal("inlinecategories_scheme", secondDocumentFirstCategory.Scheme);
                Assert.Null(secondDocumentFirstCategory.Label);

                ReferencedCategoriesDocument fourthDocument = Assert.IsType<ReferencedCategoriesDocument>(secondCollection.Categories[3]);
                Assert.Equal(4, fourthDocument.AttributeExtensions.Count);
                Assert.Equal("", fourthDocument.AttributeExtensions[new XmlQualifiedName("referencecategories_name1")]);
                Assert.Equal("", fourthDocument.AttributeExtensions[new XmlQualifiedName("referencecategories_name2", "referencecategories_namespace")]);
                Assert.Equal("referencecategories_value", fourthDocument.AttributeExtensions[new XmlQualifiedName("referencecategories_name3", "referencecategories_namespace")]);
                Assert.Equal("", fourthDocument.AttributeExtensions[new XmlQualifiedName("referencecategories_name4", "xmlns")]);
                Assert.Equal(new Uri("http://referencecategories_url.com/"), fourthDocument.BaseUri);
                Assert.Equal(1, fourthDocument.ElementExtensions.Count);
                Assert.Equal(10, fourthDocument.ElementExtensions[0].GetObject<ExtensionObject>().Value);
                Assert.Equal("referencecategories_language", fourthDocument.Language);
                Assert.Equal(new Uri("http://referencecategories_link.com"), fourthDocument.Link);

                ReferencedCategoriesDocument fifthDocument = Assert.IsType<ReferencedCategoriesDocument>(secondCollection.Categories[4]);
                Assert.Empty(fifthDocument.AttributeExtensions);
                Assert.Equal(new Uri("http://resourcecollectioninfo_url.com/relative_link"), fifthDocument.BaseUri);
                Assert.Empty(fifthDocument.ElementExtensions);
                Assert.Null(fifthDocument.Language);
                Assert.Equal(new Uri("http://emptyelement_link.com"), fifthDocument.Link);

                ReferencedCategoriesDocument sixthDocument = Assert.IsType<ReferencedCategoriesDocument>(secondCollection.Categories[5]);
                Assert.Empty(fifthDocument.AttributeExtensions);
                Assert.Equal(new Uri("http://resourcecollectioninfo_url.com/relative_link"), fifthDocument.BaseUri);
                Assert.Empty(fifthDocument.ElementExtensions);
                Assert.Null(fifthDocument.Language);
                Assert.Equal(new Uri("http://emptyelement_link.com"), fifthDocument.Link);
            });
        }

        [Fact]
        public void Read_TryParseReturnsTrue_ReturnsExpected()
        {
            using (var stringReader = new StringReader(
@"<app:service xmlns:a10=""http://www.w3.org/2005/Atom"" xml:lang=""document_language"" xml:base=""http://document_url.com/"" document_name1="""" d1p1:document_name2="""" d1p1:document_name3=""document_value"" d1p2:document_name4="""" xmlns:d1p2=""xmlns"" xmlns:d1p1=""document_namespace"" xmlns:app=""http://www.w3.org/2007/app"">
    <app:workspace></app:workspace>
    <app:workspace xml:base=""http://workspace_url.com/"" workspace_name1="""" d2p1:workspace_name2="""" d2p1:workspace_name3=""workspace_value"" d1p2:workspace_name4="""" xmlns:d2p1=""workspace_namespace"">
        <a10:title type=""html"" workspacetitle_name1="""" d3p1:workspacetitle_name2="""" d3p1:workspacetitle_name3=""workspacetitle_value"" d1p2:workspacetitle_name4="""" xmlns:d3p1=""workspacetitle_namespace"">workspace_title</a10:title>
        <app:collection></app:collection>
        <app:collection xml:base=""http://resourcecollectioninfo_url.com/"" href=""http://resourcecollectioninfo_link.com/"" resourcecollectioninfo_name1="""" d3p1:resourcecollectioninfo_name2="""" d3p1:resourcecollectioninfo_name3=""resourcecollectioninfo_value"" d1p2:resourcecollectioninfo_name4="""" xmlns:d3p1=""resourcecollectioninfo_namespace"">
            <a10:title type=""xhtml"" resourcecollectioninfotitle_name1="""" d4p1:resourcecollectioninfotitle_name2="""" d4p1:resourcecollectioninfotitle_name3=""resourcecollectioninfotitle_value"" d1p2:resourcecollectioninfotitle_name4="""" xmlns:d4p1=""resourcecollectioninfotitle_namespace"">resourcecollectioninfo_title</a10:title>
            <app:accept>text/html</app:accept>
            <app:accept>image/*</app:accept>
            <app:accept></app:accept>
            <app:categories />
            <app:categories xml:base=""http://inlinecategories_url.com/"" xml:lang=""inlinecategories_Language"" scheme=""inlinecategories_scheme"" fixed=""yes"" inlinecategories_name1="""" d4p1:inlinecategories_name2="""" d4p1:inlinecategories_name3=""inlinecategories_value"" d1p2:inlinecategories_name4="""" xmlns:d4p1=""inlinecategories_namespace"">
                <a10:category term="""" />
                <a10:category category_name1="""" d5p1:category_name2="""" d5p1:category_name3=""category_value"" d1p2:category_name4="""" term=""category_name"" label=""category_label"" scheme=""category_scheme"" xmlns:d5p1=""category_namespace"">
                    <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                        <Value>10</Value>
                    </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
                </a10:category>
                <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
            </app:categories>
            <app:categories></app:categories>
            <app:categories xml:base=""http://referencecategories_url.com/"" xml:lang=""referencecategories_language"" href=""http://referencecategories_link.com/"" referencecategories_name1="""" d4p1:referencecategories_name2="""" d4p1:referencecategories_name3=""referencecategories_value"" d1p2:referencecategories_name4="""" xmlns:d4p1=""referencecategories_namespace"">
                <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                    <Value>10</Value>
                </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
            </app:categories>
            <app:categories xml:base=""/relative_link"" href=""http://emptyelement_link.com/"" />
            <app:categories xml:base=""/relative_link"" href=""http://emptyelement_link.com/""></app:categories>
            <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
                <Value>10</Value>
            </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
        </app:collection>
        <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
            <Value>10</Value>
        </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
    </app:workspace>
    <app:workspace>
        <a10:title type=""text"" />
    </app:workspace>
    <AtomPub10ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
        <Value>10</Value>
    </AtomPub10ServiceDocumentFormatterTests.ExtensionObject>
</app:service>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10ServiceDocumentFormatter<ServieDocumentTryParseTrueSubclass>();
                formatter.ReadFrom(reader);

                ServiceDocument document = formatter.Document;
                Assert.Empty(document.AttributeExtensions);
                Assert.Equal(new Uri("http://document_url.com"), document.BaseUri);
                Assert.Empty(document.ElementExtensions);
                Assert.Equal("document_language", document.Language);
                Assert.Equal(3, document.Workspaces.Count);

                Workspace firstWorkspace = document.Workspaces[0];
                Assert.Empty(firstWorkspace.AttributeExtensions);
                Assert.Equal(new Uri("http://document_url.com"), firstWorkspace.BaseUri);
                Assert.Empty(firstWorkspace.Collections);
                Assert.Empty(firstWorkspace.ElementExtensions);
                Assert.Null(firstWorkspace.Title);

                Workspace secondWorkspace = document.Workspaces[1];
                Assert.Empty(secondWorkspace.AttributeExtensions);
                Assert.Equal(new Uri("http://workspace_url.com"), secondWorkspace.BaseUri);
                Assert.Equal(2, secondWorkspace.Collections.Count);
                Assert.Empty(secondWorkspace.ElementExtensions);

                TextSyndicationContent workspaceTitle = secondWorkspace.Title;
                Assert.Equal(4, workspaceTitle.AttributeExtensions.Count);
                Assert.Equal("", workspaceTitle.AttributeExtensions[new XmlQualifiedName("workspacetitle_name1")]);
                Assert.Equal("", workspaceTitle.AttributeExtensions[new XmlQualifiedName("workspacetitle_name2", "workspacetitle_namespace")]);
                Assert.Equal("workspacetitle_value", workspaceTitle.AttributeExtensions[new XmlQualifiedName("workspacetitle_name3", "workspacetitle_namespace")]);
                Assert.Equal("", workspaceTitle.AttributeExtensions[new XmlQualifiedName("workspacetitle_name4", "xmlns")]);
                Assert.Equal("workspace_title", workspaceTitle.Text);
                Assert.Equal("html", workspaceTitle.Type);

                ResourceCollectionInfo firstCollection = secondWorkspace.Collections[0];
                Assert.Empty(firstCollection.Accepts);
                Assert.Empty(firstCollection.AttributeExtensions);
                Assert.Equal(new Uri("http://workspace_url.com/"), firstCollection.BaseUri);
                Assert.Empty(firstCollection.Categories);
                Assert.Empty(firstCollection.ElementExtensions);
                Assert.Null(firstCollection.Link);
                Assert.Null(firstCollection.Title);

                ResourceCollectionInfo secondCollection = secondWorkspace.Collections[1];
                Assert.Equal(new string[] { "text/html", "image/*", "" }, secondCollection.Accepts);
                Assert.Empty(secondCollection.AttributeExtensions);
                Assert.Equal(new Uri("http://resourcecollectioninfo_url.com"), secondCollection.BaseUri);
                Assert.Equal(6, secondCollection.Categories.Count);
                Assert.Empty(secondCollection.ElementExtensions);
                Assert.Equal(new Uri("http://resourcecollectioninfo_link.com"), secondCollection.Link);

                TextSyndicationContent collectionTitle = secondCollection.Title;
                Assert.Equal(4, collectionTitle.AttributeExtensions.Count);
                Assert.Equal("", collectionTitle.AttributeExtensions[new XmlQualifiedName("resourcecollectioninfotitle_name1")]);
                Assert.Equal("", collectionTitle.AttributeExtensions[new XmlQualifiedName("resourcecollectioninfotitle_name2", "resourcecollectioninfotitle_namespace")]);
                Assert.Equal("resourcecollectioninfotitle_value", collectionTitle.AttributeExtensions[new XmlQualifiedName("resourcecollectioninfotitle_name3", "resourcecollectioninfotitle_namespace")]);
                Assert.Equal("", collectionTitle.AttributeExtensions[new XmlQualifiedName("resourcecollectioninfotitle_name4", "xmlns")]);
                Assert.Equal("resourcecollectioninfo_title", collectionTitle.Text);
                Assert.Equal("xhtml", collectionTitle.Type);

                InlineCategoriesDocumentTryParseTrueSubclass firstDocument = Assert.IsType<InlineCategoriesDocumentTryParseTrueSubclass>(secondCollection.Categories[0]);
                Assert.Empty(firstDocument.AttributeExtensions);
                Assert.Equal(new Uri("http://resourcecollectioninfo_url.com/"), firstDocument.BaseUri);
                Assert.Empty(firstDocument.Categories);
                Assert.Empty(firstDocument.ElementExtensions);
                Assert.False(firstDocument.IsFixed);
                Assert.Null(firstDocument.Language);
                Assert.Null(firstDocument.Scheme);

                InlineCategoriesDocumentTryParseTrueSubclass secondDocument = Assert.IsType<InlineCategoriesDocumentTryParseTrueSubclass>(secondCollection.Categories[1]);
                Assert.Empty(secondDocument.AttributeExtensions);
                Assert.Equal(new Uri("http://inlinecategories_url.com/"), secondDocument.BaseUri);
                Assert.Equal(2, secondDocument.Categories.Count);
                Assert.Empty(secondDocument.ElementExtensions);
                Assert.True(secondDocument.IsFixed);
                Assert.Equal("inlinecategories_Language", secondDocument.Language);
                Assert.Equal("inlinecategories_scheme", secondDocument.Scheme);

                SyndicationCategory secondDocumentFirstCategory = secondDocument.Categories[0];
                Assert.Empty(secondDocumentFirstCategory.AttributeExtensions);
                Assert.Empty(secondDocumentFirstCategory.ElementExtensions);
                Assert.Empty(secondDocumentFirstCategory.Name);
                Assert.Equal("inlinecategories_scheme", secondDocumentFirstCategory.Scheme);
                Assert.Null(secondDocumentFirstCategory.Label);

                SyndicationCategory secondDocumentSecondCategory = secondDocument.Categories[1];
                Assert.Empty(secondDocumentSecondCategory.AttributeExtensions);
                Assert.Empty(secondDocumentSecondCategory.ElementExtensions);
                Assert.Equal("category_name", secondDocumentSecondCategory.Name);
                Assert.Equal("category_scheme", secondDocumentSecondCategory.Scheme);
                Assert.Equal("category_label", secondDocumentSecondCategory.Label);

                InlineCategoriesDocumentTryParseTrueSubclass thirdDocument = Assert.IsType<InlineCategoriesDocumentTryParseTrueSubclass>(secondCollection.Categories[2]);
                Assert.Empty(secondDocumentFirstCategory.AttributeExtensions);
                Assert.Empty(secondDocumentFirstCategory.ElementExtensions);
                Assert.Empty(secondDocumentFirstCategory.Name);
                Assert.Equal("inlinecategories_scheme", secondDocumentFirstCategory.Scheme);
                Assert.Null(secondDocumentFirstCategory.Label);

                ReferencedCategoriesDocumentTryParseTrueSubclass fourthDocument = Assert.IsType<ReferencedCategoriesDocumentTryParseTrueSubclass>(secondCollection.Categories[3]);
                Assert.Empty(fourthDocument.AttributeExtensions);
                Assert.Equal(new Uri("http://referencecategories_url.com/"), fourthDocument.BaseUri);
                Assert.Empty(fourthDocument.ElementExtensions);
                Assert.Equal("referencecategories_language", fourthDocument.Language);
                Assert.Equal(new Uri("http://referencecategories_link.com"), fourthDocument.Link);

                ReferencedCategoriesDocumentTryParseTrueSubclass fifthDocument = Assert.IsType<ReferencedCategoriesDocumentTryParseTrueSubclass>(secondCollection.Categories[4]);
                Assert.Empty(fifthDocument.AttributeExtensions);
                Assert.Equal(new Uri("http://resourcecollectioninfo_url.com/relative_link"), fifthDocument.BaseUri);
                Assert.Empty(fifthDocument.ElementExtensions);
                Assert.Null(fifthDocument.Language);
                Assert.Equal(new Uri("http://emptyelement_link.com"), fifthDocument.Link);

                ReferencedCategoriesDocumentTryParseTrueSubclass sixthDocument = Assert.IsType<ReferencedCategoriesDocumentTryParseTrueSubclass>(secondCollection.Categories[5]);
                Assert.Empty(fifthDocument.AttributeExtensions);
                Assert.Equal(new Uri("http://resourcecollectioninfo_url.com/relative_link"), fifthDocument.BaseUri);
                Assert.Empty(fifthDocument.ElementExtensions);
                Assert.Null(fifthDocument.Language);
                Assert.Equal(new Uri("http://emptyelement_link.com"), fifthDocument.Link);
            }
        }

        [Fact]
        public void ReadFrom_EmptyDocument_ReturnsExpected()
        {
            VerifyRead(@"<app:service xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app""></app:service>", document =>
            {
                Assert.Empty(document.AttributeExtensions);
                Assert.Null(document.BaseUri);
                Assert.Empty(document.ElementExtensions);
                Assert.Null(document.Language);
                Assert.Empty(document.Workspaces);
            });
        }

        private static void VerifyRead(string xmlString, Action<ServiceDocument> verifyAction)
        {
            // ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10ServiceDocumentFormatter();
                formatter.ReadFrom(reader);
                verifyAction(formatter.Document);
            }

            // ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new AtomPub10ServiceDocumentFormatter();
                ((IXmlSerializable)formatter).ReadXml(reader);
                verifyAction(formatter.Document);
            }

            // Derived ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10ServiceDocumentFormatter(typeof(ServiceDocumentSubclass));
                formatter.ReadFrom(reader);
                verifyAction(formatter.Document);
            }

            // Derived ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new AtomPub10ServiceDocumentFormatter(typeof(ServiceDocumentSubclass));
                ((IXmlSerializable)formatter).ReadXml(reader);
                verifyAction(formatter.Document);
            }

            // Generic ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10ServiceDocumentFormatter<ServiceDocument>();
                formatter.ReadFrom(reader);
                verifyAction(formatter.Document);
            }

            // Generic ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new AtomPub10ServiceDocumentFormatter<ServiceDocument>();
                ((IXmlSerializable)formatter).ReadXml(reader);
                verifyAction(formatter.Document);
            }

            // Generic Derived ReadFrom.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10ServiceDocumentFormatter<ServiceDocumentSubclass>();
                formatter.ReadFrom(reader);
                verifyAction(formatter.Document);
            }

            // Generic Derived ReadXml.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new AtomPub10ServiceDocumentFormatter<ServiceDocumentSubclass>();
                ((IXmlSerializable)formatter).ReadXml(reader);
                verifyAction(formatter.Document);
            }

            // Load.
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                ServiceDocument document = ServiceDocument.Load(reader);
                verifyAction(document);
            }
        }

        [Fact]
        public void ReadFrom_NullReader_ThrowsArgumentNullException()
        {
            var formatter = new AtomPub10ServiceDocumentFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.ReadFrom(null));
        }

        [Fact]
        public void ReadFrom_NullCreatedDocument_ThrowsXmlException()
        {
            using (var stringReader = new StringReader(@"<app:service xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app""></app:service>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new NullCreatedDocumentFormatter();
                Assert.Throws<XmlException>(() => formatter.ReadFrom(reader));
            }
        }

        [Theory]
        [InlineData(@"<app:different xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app""></app:different>")]
        [InlineData(@"<app:service xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app"" />")]
        public void ReadFrom_CantRead_ThrowsXmlException(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                var formatter = new AtomPub10ServiceDocumentFormatter();
                Assert.Throws<XmlException>(() => formatter.ReadFrom(reader));
            }
        }

        [Theory]
        [InlineData("<service></service>")]
        [InlineData(@"<app:service xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app""></app:service>")]
        [InlineData(@"<service xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app""></service>")]
        [InlineData(@"<app:different xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app""></app:different>")]
        public void ReadXml_ValidReader_Success(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new AtomPub10ServiceDocumentFormatter();
                ((IXmlSerializable)formatter).ReadXml(reader);

                ServiceDocument document = formatter.Document;
                Assert.Empty(document.AttributeExtensions);
                Assert.Null(document.BaseUri);
                Assert.Empty(document.ElementExtensions);
                Assert.Null(document.Language);
                Assert.Empty(document.Workspaces);
            }
        }

        [Fact]
        public void ReadXml_NullReader_ThrowsArgumentNullException()
        {
            IXmlSerializable formatter = new AtomPub10ServiceDocumentFormatter();
            AssertExtensions.Throws<ArgumentNullException>("reader", () => formatter.ReadXml(null));
        }

        [Fact]
        public void ReadXml_NullCreatedDocument_ThrowsXmlException()
        {
            using (var stringReader = new StringReader(@"<app:service xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app""></app:service>"))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                IXmlSerializable formatter = new NullCreatedDocumentFormatter();
                Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
            }
        }

        [Theory]
        [InlineData("<service/>")]
        [InlineData(@"<app:service xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app"" />")]
        [InlineData(
@"<app:service xmlns:a10=""http://www.w3.org/2005/Atom"" xmlns:app=""http://www.w3.org/2007/app"">
    <app:workspace xml:base=""http://workspace_url.com/"">
        <a10:title type=""UNKNOWN"" workspacetitle_name1="""">workspace_title</a10:title>
    </app:workspace>
</app:service>")]
        public void ReadXml_EmptyElement_ThrowsXmlException(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                reader.MoveToContent();

                var formatter = new AtomPub10ServiceDocumentFormatter();
                Assert.Throws<XmlException>(() => ((IXmlSerializable)formatter).ReadXml(reader));
            }
        }

        [Fact]
        public void ReadXml_ThrowsArgumentException_RethrowsAsXmlException()
        {
            var reader = new ThrowingXmlReader(new ArgumentException());
            IXmlSerializable formatter = new AtomPub10ServiceDocumentFormatter();
            Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
        }

        [Fact]
        public void ReadXml_ThrowsFormatException_RethrowsAsXmlException()
        {
            var reader = new ThrowingXmlReader(new FormatException());
            IXmlSerializable formatter = new AtomPub10ServiceDocumentFormatter();
            Assert.Throws<XmlException>(() => formatter.ReadXml(reader));
        }

        [Fact]
        public void CreateItemInstance_NonGeneric_Success()
        {
            var formatter = new Formatter();
            ServiceDocument document = Assert.IsType<ServiceDocument>(formatter.CreateDocumentInstanceEntryPoint());
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Empty(document.Workspaces);

            var typedFormatter = new Formatter(typeof(ServiceDocumentSubclass));
            document = Assert.IsType<ServiceDocumentSubclass>(typedFormatter.CreateDocumentInstanceEntryPoint());
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Empty(document.Workspaces);
        }

        [Fact]
        public void CreateItemInstance_Generic_Success()
        {
            var formatter = new GenericFormatter<ServiceDocument>();
            ServiceDocument document = Assert.IsType<ServiceDocument>(formatter.CreateDocumentInstanceEntryPoint());
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Empty(document.Workspaces);

            var typedFormatter = new GenericFormatter<ServiceDocumentSubclass>();
            ServiceDocumentSubclass item = Assert.IsType<ServiceDocumentSubclass>(typedFormatter.CreateDocumentInstanceEntryPoint());
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Empty(document.Workspaces);
        }

        public class ServiceDocumentSubclass : ServiceDocument { }

        public class ServieDocumentTryParseTrueSubclass : ServiceDocument
        {
            protected override bool TryParseAttribute(string name, string ns, string value, string version) => true;

            protected override bool TryParseElement(XmlReader reader, string version)
            {
                reader.Skip();
                return true;
            }

            protected override Workspace CreateWorkspace() => new WorkspaceTryParseTrueSubclass();
        }

        public class WorkspaceTryParseTrueSubclass : Workspace
        {
            protected override bool TryParseAttribute(string name, string ns, string value, string version) => true;

            protected override bool TryParseElement(XmlReader reader, string version)
            {
                reader.Skip();
                return true;
            }

            protected override ResourceCollectionInfo CreateResourceCollection() => new ResourceCollectionInfoTryParseTrueSubclass();
        }

        public class ResourceCollectionInfoTryParseTrueSubclass : ResourceCollectionInfo
        {
            protected override bool TryParseAttribute(string name, string ns, string value, string version) => true;

            protected override bool TryParseElement(XmlReader reader, string version)
            {
                reader.Skip();
                return true;
            }

            protected override InlineCategoriesDocument CreateInlineCategoriesDocument() => new InlineCategoriesDocumentTryParseTrueSubclass();

            protected override ReferencedCategoriesDocument CreateReferencedCategoriesDocument() => new ReferencedCategoriesDocumentTryParseTrueSubclass();
        }

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

        public class NullCreatedDocumentFormatter : AtomPub10ServiceDocumentFormatter
        {
            protected override ServiceDocument CreateDocumentInstance() => null;
        }

        public class Formatter : AtomPub10ServiceDocumentFormatter
        {
            public Formatter() : base() { }

            public Formatter(ServiceDocument documentToWrite) : base(documentToWrite) { }

            public Formatter(Type documentTypeToCreate) : base(documentTypeToCreate) { }

            public ServiceDocument CreateDocumentInstanceEntryPoint() => CreateDocumentInstance();
        }

        public class GenericFormatter<T> : AtomPub10ServiceDocumentFormatter<T> where T : ServiceDocument, new()
        {
            public ServiceDocument CreateDocumentInstanceEntryPoint() => CreateDocumentInstance();
        }

        [DataContract]
        public class ExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }
    }
}
