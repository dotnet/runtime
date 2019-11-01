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
    public class ServiceDocumentTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var document = new ServiceDocument();
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Empty(document.Workspaces);
        }

        public static IEnumerable<object[]> Ctor_Workspaces_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Workspace[0] };
            yield return new object[] { new Workspace[] { new Workspace("title", new ResourceCollectionInfo[0]) } };
        }

        [Theory]
        [MemberData(nameof(Ctor_Workspaces_TestData))]
        public void Ctor_Workspaces(IEnumerable<Workspace> workspaces)
        {
            var document = new ServiceDocument(workspaces);
            Assert.Empty(document.AttributeExtensions);
            Assert.Null(document.BaseUri);
            Assert.Empty(document.ElementExtensions);
            Assert.Null(document.Language);
            Assert.Equal(workspaces?.Count() ?? 0, document.Workspaces.Count);
        }

        [Fact]
        public void Ctor_NullValueInWorkspaces_ThrowsArgumentNullException()
        {
            var workspaces = new Collection<Workspace> { null };
            AssertExtensions.Throws<ArgumentNullException>("item", () => new ServiceDocument(workspaces));
        }

        [Fact]
        public void GetFormatter_Invoke_ReturnsExpected()
        {
            var document = new ServiceDocument();
            AtomPub10ServiceDocumentFormatter formatter = Assert.IsType<AtomPub10ServiceDocumentFormatter>(document.GetFormatter());
            Assert.Same(document, formatter.Document);
            Assert.Equal("http://www.w3.org/2007/app", formatter.Version);
        }

        [Fact]
        public void Workspaces_AddNonNullItem_Success()
        {
            Collection<Workspace> collection = new ServiceDocument().Workspaces;
            collection.Add(new Workspace());
            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public void Workspaces_AddNullItem_ThrowsArgumentNullException()
        {
            Collection<Workspace> collection = new ServiceDocument().Workspaces;
            AssertExtensions.Throws<ArgumentNullException>("item", () => collection.Add(null));
        }

        [Fact]
        public void Workspaces_SetNonNullItem_GetReturnsExpected()
        {
            Collection<Workspace> collection = new ServiceDocument().Workspaces;
            collection.Add(new Workspace());

            var newValue = new Workspace();
            collection[0] = newValue;
            Assert.Same(newValue, collection[0]);
        }

        [Fact]
        public void Workspaces_SetNullItem_ThrowsArgumentNullException()
        {
            Collection<Workspace> collection = new ServiceDocument().Workspaces;
            collection.Add(new Workspace());

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
            var document = new ServiceDocumentSubclass();
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
            var document = new ServiceDocumentSubclass();
            Assert.False(document.TryParseElementEntryPoint(reader, version));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteAttributeExtensions_Invoke_ReturnsExpected(string version)
        {
            var document = new ServiceDocumentSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => document.WriteAttributeExtensionsEntryPoint(writer, version));

            document.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            document.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            document.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => document.WriteAttributeExtensionsEntryPoint(writer, "version"));
        }

        [Fact]
        public void WriteAttributeExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var document = new ServiceDocumentSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => document.WriteAttributeExtensionsEntryPoint(null, "version"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteElementExtensions_Invoke_ReturnsExpected(string version)
        {
            var document = new ServiceDocumentSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => document.WriteElementExtensionsEntryPoint(writer, version));

            document.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            document.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<ServiceDocumentTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</ServiceDocumentTests.ExtensionObject>
<ServiceDocumentTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</ServiceDocumentTests.ExtensionObject>", writer => document.WriteElementExtensionsEntryPoint(writer, version));
        }

        [Fact]
        public void WriteElementExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var document = new ServiceDocumentSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => document.WriteElementExtensionsEntryPoint(null, "version"));
        }

        public class ServiceDocumentSubclass : ServiceDocument
        {
            public Workspace CreateWorkspaceEntryPoint() => CreateWorkspace();

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
