// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class ServiceDocumentFormatterTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var formatter = new Formatter();
            Assert.Null(formatter.Document);
        }

        [Fact]
        public void Ctor_ServiceDocument()
        {
            var document = new ServiceDocument();
            var formatter = new Formatter(document);
            Assert.Same(document, formatter.Document);
        }

        [Fact]
        public void Ctor_NullDocumentToWrite_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("documentToWrite", () => new Formatter(null));
        }

        [Fact]
        public void CreateCategory_Invoke_ReturnsExpected()
        {
            var document = new InlineCategoriesDocument();
            SyndicationCategory category = Formatter.CreateCategoryEntryPoint(document);
            Assert.Empty(category.AttributeExtensions);
            Assert.Empty(category.ElementExtensions);
            Assert.Null(category.Label);
            Assert.Null(category.Name);
            Assert.Null(category.Scheme);
        }

        [Fact]
        public void CreateCategory_NullCategory_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("inlineCategories", () => Formatter.CreateCategoryEntryPoint(null));
        }

        [Fact]
        public void CreateCollection_Invoke_ReturnsExpected()
        {
            var workspace = new Workspace();
            ResourceCollectionInfo collection = Formatter.CreateCollectionEntryPoint(workspace);
            Assert.Empty(collection.Accepts);
            Assert.Empty(collection.AttributeExtensions);
            Assert.Null(collection.BaseUri);
            Assert.Empty(collection.Categories);
            Assert.Empty(collection.ElementExtensions);
            Assert.Null(collection.Link);
            Assert.Null(collection.Title);
        }

        [Fact]
        public void CreateCollection_NullWorkspace_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("workspace", () => Formatter.CreateCollectionEntryPoint(null));
        }

        [Fact]
        public void CreateDocumentInstance_Invoke_ReturnsExpected()
        {
            var formatter = new Formatter();
            ServiceDocument document = formatter.CreateDocumentInstanceEntryPoint();
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Empty(document.Workspaces);
        }

        [Fact]
        public void CreateInlineCategories_Invoke_ReturnsExpected()
        {
            var collection = new ResourceCollectionInfo();
            InlineCategoriesDocument document = Formatter.CreateInlineCategoriesEntryPoint(collection);
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.Categories);
            Assert.Empty(document.ElementExtensions);
            Assert.False(document.IsFixed);
            Assert.Null(document.Language);
            Assert.Null(document.Scheme);
        }

        [Fact]
        public void CreateInlineCategories_NullCategory_ThrowsArgumentNullException()
        {
            Assert.Throws<NullReferenceException>(() => Formatter.CreateInlineCategoriesEntryPoint(null));
        }

        [Fact]
        public void CreateReferencedCategories_Invoke_ReturnsExpected()
        {
            var collection = new ResourceCollectionInfo();
            ReferencedCategoriesDocument document = Formatter.CreateReferencedCategoriesEntryPoint(collection);
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Null(document.Link);
        }

        [Fact]
        public void CreateReferencedCategories_NullCategory_ThrowsArgumentNullException()
        {
            Assert.Throws<NullReferenceException>(() => Formatter.CreateReferencedCategoriesEntryPoint(null));
        }

        [Fact]
        public void CreateWorkspace_Invoke_ReturnsExpected()
        {
            var document = new ServiceDocument();
            Workspace workspace = Formatter.CreateWorkspaceEntryPoint(document);
            Assert.Empty(workspace.AttributeExtensions);
            Assert.Null(workspace.BaseUri);
            Assert.Empty(workspace.Collections);
            Assert.Empty(workspace.ElementExtensions);
            Assert.Null(workspace.Title);
        }

        [Fact]
        public void CreateWorkspace_NullDocument_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("document", () => Formatter.CreateWorkspaceEntryPoint(null));
        }

        [Fact]
        public void LoadElementExtensions_Categories_Success()
        {
            var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

            using (var stringReader = new StringReader(@"<ExtensionObject><Value>10</Value></ExtensionObject><ExtensionObject xmlns=""http://www.w3.org/TR/html4/""><Value>11</Value></ExtensionObject>"))
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                var document = new InlineCategoriesDocument();
                Formatter.LoadElementExtensionsEntryPoint(reader, document, int.MaxValue);

                Assert.Equal(2, document.ElementExtensions.Count);
                Assert.Equal(10, document.ElementExtensions[0].GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
                Assert.Equal("ExtensionObject", document.ElementExtensions[0].OuterName);
                Assert.Empty(document.ElementExtensions[0].OuterNamespace);
                Assert.Equal("ExtensionObject", document.ElementExtensions[1].OuterName);
                Assert.Equal("http://www.w3.org/TR/html4/", document.ElementExtensions[1].OuterNamespace);
            }
        }

        [Fact]
        public void LoadElementExtensions_NullCategories_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("categories", () => Formatter.LoadElementExtensionsEntryPoint(reader, (CategoriesDocument)null, int.MaxValue));
        }

        [Fact]
        public void LoadElementExtensions_Collection_Success()
        {
            var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

            using (var stringReader = new StringReader(@"<ExtensionObject><Value>10</Value></ExtensionObject><ExtensionObject xmlns=""http://www.w3.org/TR/html4/""><Value>11</Value></ExtensionObject>"))
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                var collection = new ResourceCollectionInfo();
                Formatter.LoadElementExtensionsEntryPoint(reader, collection, int.MaxValue);

                Assert.Equal(2, collection.ElementExtensions.Count);
                Assert.Equal(10, collection.ElementExtensions[0].GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
                Assert.Equal("ExtensionObject", collection.ElementExtensions[0].OuterName);
                Assert.Empty(collection.ElementExtensions[0].OuterNamespace);
                Assert.Equal("ExtensionObject", collection.ElementExtensions[1].OuterName);
                Assert.Equal("http://www.w3.org/TR/html4/", collection.ElementExtensions[1].OuterNamespace);
            }
        }

        [Fact]
        public void LoadElementExtensions_NullCollection_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("collection", () => Formatter.LoadElementExtensionsEntryPoint(reader, (ResourceCollectionInfo)null, int.MaxValue));
        }

        [Fact]
        public void LoadElementExtensions_Workspace_Success()
        {
            var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

            using (var stringReader = new StringReader(@"<ExtensionObject><Value>10</Value></ExtensionObject><ExtensionObject xmlns=""http://www.w3.org/TR/html4/""><Value>11</Value></ExtensionObject>"))
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                var workspace = new Workspace();
                Formatter.LoadElementExtensionsEntryPoint(reader, workspace, int.MaxValue);

                Assert.Equal(2, workspace.ElementExtensions.Count);
                Assert.Equal(10, workspace.ElementExtensions[0].GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
                Assert.Equal("ExtensionObject", workspace.ElementExtensions[0].OuterName);
                Assert.Empty(workspace.ElementExtensions[0].OuterNamespace);
                Assert.Equal("ExtensionObject", workspace.ElementExtensions[1].OuterName);
                Assert.Equal("http://www.w3.org/TR/html4/", workspace.ElementExtensions[1].OuterNamespace);
            }
        }

        [Fact]
        public void LoadElementExtensions_NullWorkspace_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("workspace", () => Formatter.LoadElementExtensionsEntryPoint(reader, (Workspace)null, int.MaxValue));
        }

        [Fact]
        public void LoadElementExtensions_ServiceDocument_Success()
        {
            var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

            using (var stringReader = new StringReader(@"<ExtensionObject><Value>10</Value></ExtensionObject><ExtensionObject xmlns=""http://www.w3.org/TR/html4/""><Value>11</Value></ExtensionObject>"))
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                var document = new ServiceDocument();
                Formatter.LoadElementExtensionsEntryPoint(reader, document, int.MaxValue);

                Assert.Equal(2, document.ElementExtensions.Count);
                Assert.Equal(10, document.ElementExtensions[0].GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
                Assert.Equal("ExtensionObject", document.ElementExtensions[0].OuterName);
                Assert.Empty(document.ElementExtensions[0].OuterNamespace);
                Assert.Equal("ExtensionObject", document.ElementExtensions[1].OuterName);
                Assert.Equal("http://www.w3.org/TR/html4/", document.ElementExtensions[1].OuterNamespace);
            }
        }

        [Fact]
        public void LoadElementExtensions_NullDocument_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("document", () => Formatter.LoadElementExtensionsEntryPoint(reader, (ServiceDocument)null, int.MaxValue));
        }

        [Fact]
        public void LoadElementExtensions_NullReader_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("readerOverUnparsedExtensions", () => Formatter.LoadElementExtensionsEntryPoint(null, new InlineCategoriesDocument(), int.MaxValue));
            AssertExtensions.Throws<ArgumentNullException>("readerOverUnparsedExtensions", () => Formatter.LoadElementExtensionsEntryPoint(null, new ResourceCollectionInfo(), int.MaxValue));
            AssertExtensions.Throws<ArgumentNullException>("readerOverUnparsedExtensions", () => Formatter.LoadElementExtensionsEntryPoint(null, new ServiceDocument(), int.MaxValue));
            AssertExtensions.Throws<ArgumentNullException>("readerOverUnparsedExtensions", () => Formatter.LoadElementExtensionsEntryPoint(null, new Workspace(), int.MaxValue));
        }

        [Fact]
        public void LoadElementExtensions_NegativeMaxExtensionSize_ThrowsArgumentOutOfRangeException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxExtensionSize", () => Formatter.LoadElementExtensionsEntryPoint(reader, new InlineCategoriesDocument(), -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxExtensionSize", () => Formatter.LoadElementExtensionsEntryPoint(reader, new ResourceCollectionInfo(), -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxExtensionSize", () => Formatter.LoadElementExtensionsEntryPoint(reader, new ServiceDocument(), -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxExtensionSize", () => Formatter.LoadElementExtensionsEntryPoint(reader, new Workspace(), -1));
        }

        [Fact]
        public void SetDocument_GetDocument_ReturnsExpected()
        {
            var formatter = new Formatter();
            var document = new ServiceDocument();
            formatter.SetDocumentEntryPoint(document);
            Assert.Same(document, formatter.Document);

            formatter.SetDocumentEntryPoint(null);
            Assert.Null(formatter.Document);
        }

        public static IEnumerable<object[]> TryParseAttribute_TestData()
        {
            yield return new object[] { null, null, null, null };
            yield return new object[] { "", "", "", "" };
            yield return new object[] { "name", "namespace", "value", "version" };
        }

        [Theory]
        [MemberData(nameof(TryParseAttribute_TestData))]
        public void TryParseAttribute_Categories_ReturnsFalse(string name, string ns, string value, string version)
        {
            Assert.False(Formatter.TryParseAttributeEntryPoint(name, ns, value, new InlineCategoriesDocument(), version));
            Assert.False(Formatter.TryParseAttributeEntryPoint(name, ns, value, new ReferencedCategoriesDocument(), version));
        }

        [Fact]
        public void TryParseAttribute_NullCategories_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("categories", () => Formatter.TryParseAttributeEntryPoint("name", "namespace", "value", (CategoriesDocument)null, "version"));
        }

        [Theory]
        [MemberData(nameof(TryParseAttribute_TestData))]
        public void TryParseAttribute_Collection_Success(string name, string ns, string value, string version)
        {
            Assert.False(Formatter.TryParseAttributeEntryPoint(name, ns, value, new ResourceCollectionInfo(), version));
        }

        [Fact]
        public void TryParseAttribute_NullCollection_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("collection", () => Formatter.TryParseAttributeEntryPoint("name", "namespace", "value", (ResourceCollectionInfo)null, "version"));
        }

        [Theory]
        [MemberData(nameof(TryParseAttribute_TestData))]
        public void TryParseAttribute_Workspace_Success(string name, string ns, string value, string version)
        {
            Assert.False(Formatter.TryParseAttributeEntryPoint(name, ns, value, new Workspace(), version));
        }

        [Fact]
        public void TryParseAttribute_NullWorkspace_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("workspace", () => Formatter.TryParseAttributeEntryPoint("name", "namespace", "value", (Workspace)null, "version"));
        }

        [Theory]
        [MemberData(nameof(TryParseAttribute_TestData))]
        public void TryParseAttribute_ServiceDocument_Success(string name, string ns, string value, string version)
        {
            Assert.False(Formatter.TryParseAttributeEntryPoint(name, ns, value, new ServiceDocument(), version));
        }

        [Fact]
        public void TryParseAttribute_NullDocument_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("document", () => Formatter.TryParseAttributeEntryPoint("name", "namespace", "value", (ServiceDocument)null, "version"));
        }

        public static IEnumerable<object[]> TryParseElement_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new XElement("Name").CreateReader(), "" };
            yield return new object[] { new XElement("Name").CreateReader(), "version" };
        }

        [Theory]
        [MemberData(nameof(TryParseElement_TestData))]
        public void TryParseElement_Categories_ReturnsFalse(XmlReader reader, string version)
        {
            Assert.False(Formatter.TryParseElementEntryPoint(reader, new InlineCategoriesDocument(), version));
            Assert.False(Formatter.TryParseElementEntryPoint(reader, new ReferencedCategoriesDocument(), version));
        }

        [Fact]
        public void TryParseElement_NullCategories_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("categories", () => Formatter.TryParseElementEntryPoint(reader, (CategoriesDocument)null, "version"));
        }

        [Theory]
        [MemberData(nameof(TryParseElement_TestData))]
        public void TryParseElement_Collection_Success(XmlReader reader, string version)
        {
            Assert.False(Formatter.TryParseElementEntryPoint(reader, new ResourceCollectionInfo(), version));
        }

        [Fact]
        public void TryParseElement_NullCollection_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("collection", () => Formatter.TryParseElementEntryPoint(reader, (ResourceCollectionInfo)null, "version"));
        }

        [Theory]
        [MemberData(nameof(TryParseElement_TestData))]
        public void TryParseElement_Workspace_Success(XmlReader reader, string version)
        {
            Assert.False(Formatter.TryParseElementEntryPoint(reader, new Workspace(), version));
        }

        [Fact]
        public void TryParseElement_NullWorkspace_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("workspace", () => Formatter.TryParseElementEntryPoint(reader, (Workspace)null, "version"));
        }

        [Theory]
        [MemberData(nameof(TryParseElement_TestData))]
        public void TryParseElement_ServiceDocument_Success(XmlReader reader, string version)
        {
            Assert.False(Formatter.TryParseElementEntryPoint(reader, new ServiceDocument(), version));
        }

        [Fact]
        public void TryParseElement_NullDocument_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("document", () => Formatter.TryParseElementEntryPoint(reader, (ServiceDocument)null, "version"));
        }

        public static IEnumerable<object[]> Version_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { "" };
            yield return new object[] { "version" };
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteElementExtensions_Categories_Success(string version)
        {
            var document = new InlineCategoriesDocument();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteElementExtensionsEntryPoint(writer, document, version));

            document.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            document.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</ServiceDocumentFormatterTests.ExtensionObject>
<ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</ServiceDocumentFormatterTests.ExtensionObject>", writer => Formatter.WriteElementExtensionsEntryPoint(writer, document, version));
        }

        [Fact]
        public void WriteElementExtensions_NullCategories_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("categories", () => Formatter.WriteElementExtensionsEntryPoint(writer, (CategoriesDocument)null, "version"));
            }
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteElementExtensions_Collection_Success(string version)
        {
            var collection = new ResourceCollectionInfo();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteElementExtensionsEntryPoint(writer, collection, version));

            collection.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            collection.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</ServiceDocumentFormatterTests.ExtensionObject>
<ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</ServiceDocumentFormatterTests.ExtensionObject>", writer => Formatter.WriteElementExtensionsEntryPoint(writer, collection, version));
        }

        [Fact]
        public void WriteElementExtensions_NullCollection_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("collection", () => Formatter.WriteElementExtensionsEntryPoint(writer, (ResourceCollectionInfo)null, "version"));
            }
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteElementExtensions_Workspace_Success(string version)
        {
            var workspace = new Workspace();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteElementExtensionsEntryPoint(writer, workspace, version));

            workspace.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            workspace.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</ServiceDocumentFormatterTests.ExtensionObject>
<ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</ServiceDocumentFormatterTests.ExtensionObject>", writer => Formatter.WriteElementExtensionsEntryPoint(writer, workspace, version));
        }

        [Fact]
        public void WriteElementExtensions_NullWorkspace_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("workspace", () => Formatter.WriteElementExtensionsEntryPoint(writer, (Workspace)null, "version"));
            }
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteElementExtensions_ServiceDocument_Success(string version)
        {
            var document = new ServiceDocument();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteElementExtensionsEntryPoint(writer, document, version));

            document.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            document.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</ServiceDocumentFormatterTests.ExtensionObject>
<ServiceDocumentFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</ServiceDocumentFormatterTests.ExtensionObject>", writer => Formatter.WriteElementExtensionsEntryPoint(writer, document, version));
        }

        [Fact]
        public void WriteElementExtensions_NullDocument_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("document", () => Formatter.WriteElementExtensionsEntryPoint(writer, (ServiceDocument)null, "version"));
            }
        }

        [Fact]
        public void WriteElementExtensions_NullWriter_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteElementExtensionsEntryPoint(null, new InlineCategoriesDocument(), "version"));
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteElementExtensionsEntryPoint(null, new ResourceCollectionInfo(), "version"));
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteElementExtensionsEntryPoint(null, new ServiceDocument(), "version"));
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteElementExtensionsEntryPoint(null, new Workspace(), "version"));
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteAttributeExtensions_Categories_Success(string version)
        {
            var document = new InlineCategoriesDocument();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, document, version));

            document.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            document.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            document.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, document, version));
        }

        [Fact]
        public void WriteAttributeExtensions_NullCategories_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("categories", () => Formatter.WriteAttributeExtensionsEntryPoint(writer, (CategoriesDocument)null, "version"));
            }
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteAttributeExtensions_Collection_Success(string version)
        {
            var collection = new ResourceCollectionInfo();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, collection, version));

            collection.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            collection.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            collection.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, collection, version));
        }

        [Fact]
        public void WriteAttributeExtensions_NullCollection_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("collection", () => Formatter.WriteAttributeExtensionsEntryPoint(writer, (ResourceCollectionInfo)null, "version"));
            }
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteAttributeExtensions_Workspace_Success(string version)
        {
            var workspace = new Workspace();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, workspace, version));

            workspace.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            workspace.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            workspace.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, workspace, version));
        }

        [Fact]
        public void WriteAttributeExtensions_NullWorkspace_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("workspace", () => Formatter.WriteAttributeExtensionsEntryPoint(writer, (Workspace)null, "version"));
            }
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteAttributeExtensions_ServiceDocument_Success(string version)
        {
            var document = new ServiceDocument();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, document, version));

            document.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            document.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            document.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, document, version));
        }

        [Fact]
        public void WriteAttributeExtensions_NullDocument_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("document", () => Formatter.WriteAttributeExtensionsEntryPoint(writer, (ServiceDocument)null, "version"));
            }
        }

        [Fact]
        public void WriteAttributeExtensions_NullWriter_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteAttributeExtensionsEntryPoint(null, new InlineCategoriesDocument(), "version"));
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteAttributeExtensionsEntryPoint(null, new ResourceCollectionInfo(), "version"));
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteAttributeExtensionsEntryPoint(null, new ServiceDocument(), "version"));
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteAttributeExtensionsEntryPoint(null, new Workspace(), "version"));
        }

        public class Formatter : ServiceDocumentFormatter
        {
            public Formatter() : base() { }

            public Formatter(ServiceDocument document) : base(document) { }

            public static SyndicationCategory CreateCategoryEntryPoint(InlineCategoriesDocument inlineCategories) => CreateCategory(inlineCategories);

            public static ResourceCollectionInfo CreateCollectionEntryPoint(Workspace workspace) => CreateCollection(workspace);

            public ServiceDocument CreateDocumentInstanceEntryPoint() => CreateDocumentInstance();

            public static InlineCategoriesDocument CreateInlineCategoriesEntryPoint(ResourceCollectionInfo collection) => CreateInlineCategories(collection);

            public static ReferencedCategoriesDocument CreateReferencedCategoriesEntryPoint(ResourceCollectionInfo collection) => CreateReferencedCategories(collection);

            public static Workspace CreateWorkspaceEntryPoint(ServiceDocument document) => CreateWorkspace(document);

            public static void LoadElementExtensionsEntryPoint(XmlReader reader, CategoriesDocument categories, int maxExtensionSize) => LoadElementExtensions(reader, categories, maxExtensionSize);

            public static void LoadElementExtensionsEntryPoint(XmlReader reader, ResourceCollectionInfo collection, int maxExtensionSize) => LoadElementExtensions(reader, collection, maxExtensionSize);

            public static void LoadElementExtensionsEntryPoint(XmlReader reader, Workspace workspace, int maxExtensionSize) => LoadElementExtensions(reader, workspace, maxExtensionSize);

            public static void LoadElementExtensionsEntryPoint(XmlReader reader, ServiceDocument document, int maxExtensionSize) => LoadElementExtensions(reader, document, maxExtensionSize);

            public void SetDocumentEntryPoint(ServiceDocument document) => SetDocument(document);

            public static bool TryParseAttributeEntryPoint(string name, string ns, string value, ServiceDocument document, string version)
            {
                return TryParseAttribute(name, ns, value, document, version);
            }

            public static bool TryParseAttributeEntryPoint(string name, string ns, string value, ResourceCollectionInfo collection, string version)
            {
                return TryParseAttribute(name, ns, value, collection, version);
            }

            public static bool TryParseAttributeEntryPoint(string name, string ns, string value, CategoriesDocument categories, string version)
            {
                return TryParseAttribute(name, ns, value, categories, version);
            }

            public static bool TryParseAttributeEntryPoint(string name, string ns, string value, Workspace workspace, string version)
            {
                return TryParseAttribute(name, ns, value, workspace, version);
            }

            public static bool TryParseElementEntryPoint(XmlReader reader, ServiceDocument document, string version)
            {
                return TryParseElement(reader, document, version);
            }

            public static bool TryParseElementEntryPoint(XmlReader reader, ResourceCollectionInfo collection, string version)
            {
                return TryParseElement(reader, collection, version);
            }

            public static bool TryParseElementEntryPoint(XmlReader reader, CategoriesDocument categories, string version)
            {
                return TryParseElement(reader, categories, version);
            }

            public static bool TryParseElementEntryPoint(XmlReader reader, Workspace workspace, string version)
            {
                return TryParseElement(reader, workspace, version);
            }

            public static void WriteAttributeExtensionsEntryPoint(XmlWriter writer, CategoriesDocument categories, string version)
            {
                WriteAttributeExtensions(writer, categories, version);
            }

            public static void WriteAttributeExtensionsEntryPoint(XmlWriter writer, ResourceCollectionInfo collection, string version)
            {
                WriteAttributeExtensions(writer, collection, version);
            }

            public static void WriteAttributeExtensionsEntryPoint(XmlWriter writer, ServiceDocument document, string version)
            {
                WriteAttributeExtensions(writer, document, version);
            }

            public static void WriteAttributeExtensionsEntryPoint(XmlWriter writer, Workspace workspace, string version)
            {
                WriteAttributeExtensions(writer, workspace, version);
            }

            public static void WriteElementExtensionsEntryPoint(XmlWriter writer, CategoriesDocument categories, string version)
            {
                WriteElementExtensions(writer, categories, version);
            }

            public static void WriteElementExtensionsEntryPoint(XmlWriter writer, ResourceCollectionInfo collection, string version)
            {
                WriteElementExtensions(writer, collection, version);
            }

            public static void WriteElementExtensionsEntryPoint(XmlWriter writer, ServiceDocument document, string version)
            {
                WriteElementExtensions(writer, document, version);
            }

            public static void WriteElementExtensionsEntryPoint(XmlWriter writer, Workspace workspace, string version)
            {
                WriteElementExtensions(writer, workspace, version);
            }

            public override string Version => throw new NotImplementedException();

            public override bool CanRead(XmlReader reader) => throw new NotImplementedException();

            public override void ReadFrom(XmlReader reader) => throw new NotImplementedException();

            public override void WriteTo(XmlWriter writer) => throw new NotImplementedException();
        }

        [DataContract]
        public class ExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }
    }
}
