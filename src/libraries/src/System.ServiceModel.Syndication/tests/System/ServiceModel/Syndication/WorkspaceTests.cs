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
    public class WorkspaceTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var workspace = new Workspace();
            Assert.Empty(workspace.AttributeExtensions);
            Assert.Null(workspace.BaseUri);
            Assert.Empty(workspace.Collections);
            Assert.Empty(workspace.ElementExtensions);
            Assert.Null(workspace.Title);
        }

        public static IEnumerable<object[]> Ctor_String_Collections()
        {
            yield return new object[] { null, null };
            yield return new object[] { "", new ResourceCollectionInfo[0] };
            yield return new object[] { "title", new ResourceCollectionInfo[] { new ResourceCollectionInfo("title", new Uri("http://microsoft.com")) } };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_Collections))]
        public void Ctor_String_Collections(string title, ResourceCollectionInfo[] collections)
        {
            var workspace = new Workspace(title, collections);
            Assert.Empty(workspace.AttributeExtensions);
            Assert.Null(workspace.BaseUri);
            Assert.Equal(collections ?? new ResourceCollectionInfo[0], workspace.Collections);
            Assert.Empty(workspace.ElementExtensions);
            if (title == null)
            {
                Assert.Null(workspace.Title);
            }
            else
            {
                Assert.Equal(title, workspace.Title.Text);
                Assert.Equal("text", workspace.Title.Type);
            }
        }

        public static IEnumerable<object[]> Ctor_TextSyndicationContent_Collections()
        {
            yield return new object[] { null, null };
            yield return new object[] { new TextSyndicationContent("", TextSyndicationContentKind.Html), new ResourceCollectionInfo[0] };
            yield return new object[] { new TextSyndicationContent("title", TextSyndicationContentKind.Html), new ResourceCollectionInfo[] { new ResourceCollectionInfo("title", new Uri("http://microsoft.com")) } };
        }

        [Theory]
        [MemberData(nameof(Ctor_TextSyndicationContent_Collections))]
        public void Ctor_TextSyndicationContent_Collections(TextSyndicationContent title, ResourceCollectionInfo[] collections)
        {
            var workspace = new Workspace(title, collections);
            Assert.Empty(workspace.AttributeExtensions);
            Assert.Null(workspace.BaseUri);
            Assert.Equal(collections ?? new ResourceCollectionInfo[0], workspace.Collections);
            Assert.Empty(workspace.ElementExtensions);
            Assert.Equal(title, workspace.Title);
        }

        [Fact]
        public void Ctor_NullValueInCollections_ThrowsArgumentNullException()
        {
            var collections = new ResourceCollectionInfo[] { null };
            AssertExtensions.Throws<ArgumentNullException>("item", () => new Workspace("title", collections));
            AssertExtensions.Throws<ArgumentNullException>("item", () => new Workspace(new TextSyndicationContent("title"), collections));
        }

        [Fact]
        public void Collections_AddNonNullItem_Success()
        {
            Collection<ResourceCollectionInfo> collection = new Workspace().Collections;
            collection.Add(new ResourceCollectionInfo());
            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public void Collections_AddNullItem_ThrowsArgumentNullException()
        {
            Collection<ResourceCollectionInfo> collection = new Workspace().Collections;
            AssertExtensions.Throws<ArgumentNullException>("item", () => collection.Add(null));
        }

        [Fact]
        public void Collections_SetNonNullItem_GetReturnsExpected()
        {
            Collection<ResourceCollectionInfo> collection = new Workspace().Collections;
            collection.Add(new ResourceCollectionInfo());

            var newValue = new ResourceCollectionInfo();
            collection[0] = newValue;
            Assert.Same(newValue, collection[0]);
        }

        [Fact]
        public void Collections_SetNullItem_ThrowsArgumentNullException()
        {
            Collection<ResourceCollectionInfo> collection = new Workspace().Collections;
            collection.Add(new ResourceCollectionInfo());

            AssertExtensions.Throws<ArgumentNullException>("item", () => collection[0] = null);
        }

        [Fact]
        public void CreateResoureCollection_Invoke_ReturnsExpected()
        {
            var workspace = new WorkspaceSubclass();
            ResourceCollectionInfo collectionInfo = workspace.CreateResourceCollectionEntryPoint();
            Assert.Empty(collectionInfo.Accepts);
            Assert.Empty(collectionInfo.AttributeExtensions);
            Assert.Null(collectionInfo.BaseUri);
            Assert.Empty(collectionInfo.Categories);
            Assert.Empty(collectionInfo.ElementExtensions);
            Assert.Null(collectionInfo.Link);
            Assert.Null(collectionInfo.Title);
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
            var workspace = new WorkspaceSubclass();
            Assert.False(workspace.TryParseAttributeEntryPoint(name, ns, value, version));
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
            var workspace = new WorkspaceSubclass();
            Assert.False(workspace.TryParseElementEntryPoint(reader, version));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteAttributeExtensions_Invoke_ReturnsExpected(string version)
        {
            var workspace = new WorkspaceSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => workspace.WriteAttributeExtensionsEntryPoint(writer, version));

            workspace.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            workspace.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            workspace.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => workspace.WriteAttributeExtensionsEntryPoint(writer, "version"));
        }

        [Fact]
        public void WriteAttributeExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var workspace = new WorkspaceSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => workspace.WriteAttributeExtensionsEntryPoint(null, "version"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteElementExtensions_Invoke_ReturnsExpected(string version)
        {
            var workspace = new WorkspaceSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => workspace.WriteElementExtensionsEntryPoint(writer, version));

            workspace.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            workspace.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<WorkspaceTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</WorkspaceTests.ExtensionObject>
<WorkspaceTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</WorkspaceTests.ExtensionObject>", writer => workspace.WriteElementExtensionsEntryPoint(writer, version));
        }

        [Fact]
        public void WriteElementExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var workspace = new WorkspaceSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => workspace.WriteElementExtensionsEntryPoint(null, "version"));
        }

        public class WorkspaceSubclass : Workspace
        {
            public ResourceCollectionInfo CreateResourceCollectionEntryPoint() => CreateResourceCollection();

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
