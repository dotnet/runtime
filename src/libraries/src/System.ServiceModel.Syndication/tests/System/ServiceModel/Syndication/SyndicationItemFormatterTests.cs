// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public partial class SyndicationItemFormatterTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var formatter = new Formatter();
            Assert.Null(formatter.Item);
        }

        [Fact]
        public void Ctor_SyndicationItem()
        {
            var item = new SyndicationItem();
            var formatter = new Formatter(item);
            Assert.Same(item, formatter.Item);
        }

        [Fact]
        public void Ctor_NullItemToWrite_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("itemToWrite", () => new Formatter(null));
        }

        [Fact]
        public void CreateCategory_Invoke_ReturnsExpected()
        {
            var item = new SyndicationItem();
            SyndicationCategory category = Formatter.CreateCategoryEntryPoint(item);
            Assert.Empty(category.AttributeExtensions);
            Assert.Empty(category.ElementExtensions);
            Assert.Null(category.Label);
            Assert.Null(category.Name);
            Assert.Null(category.Scheme);
        }

        [Fact]
        public void CreateCategory_NullItem_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("item", () => Formatter.CreateCategoryEntryPoint(null));
        }

        [Fact]
        public void CreateCategory_ItemReturnsNull_ThrowsInvalidOperationException()
        {
            var item = new NullSyndicationItem();
            Assert.Throws<InvalidOperationException>(() => Formatter.CreateCategoryEntryPoint(item));
        }

        [Fact]
        public void CreateLink_Invoke_ReturnsExpected()
        {
            var item = new SyndicationItem();
            SyndicationLink link = Formatter.CreateLinkEntryPoint(item);
            Assert.Empty(link.AttributeExtensions);
            Assert.Empty(link.ElementExtensions);
            Assert.Equal(0, link.Length);
            Assert.Null(link.MediaType);
            Assert.Null(link.RelationshipType);
            Assert.Null(link.Title);
            Assert.Null(link.Uri);
        }

        [Fact]
        public void CreateLink_NullItem_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("item", () => Formatter.CreateLinkEntryPoint(null));
        }

        [Fact]
        public void CreateLink_ItemReturnsNull_ThrowsInvalidOperationException()
        {
            var item = new NullSyndicationItem();
            Assert.Throws<InvalidOperationException>(() => Formatter.CreateLinkEntryPoint(item));
        }

        [Fact]
        public void CreatePerson_Invoke_ReturnsExpected()
        {
            var item = new SyndicationItem();
            SyndicationPerson person = Formatter.CreatePersonEntryPoint(item);
            Assert.Empty(person.AttributeExtensions);
            Assert.Empty(person.ElementExtensions);
            Assert.Null(person.Email);
            Assert.Null(person.Name);
            Assert.Null(person.Uri);
        }

        [Fact]
        public void CreatePerson_NullItem_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("item", () => Formatter.CreatePersonEntryPoint(null));
        }

        [Fact]
        public void CreatePerson_ItemReturnsNull_ThrowsInvalidOperationException()
        {
            var item = new NullSyndicationItem();
            Assert.Throws<InvalidOperationException>(() => Formatter.CreatePersonEntryPoint(item));
        }

        [Fact]
        public void LoadElementExtensions_Categories_Success()
        {
            var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

            using (var stringReader = new StringReader(@"<ExtensionObject><Value>10</Value></ExtensionObject><ExtensionObject xmlns=""http://www.w3.org/TR/html4/""><Value>11</Value></ExtensionObject>"))
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                var category = new SyndicationCategory();
                Formatter.LoadElementExtensionsEntryPoint(reader, category, int.MaxValue);

                Assert.Equal(2, category.ElementExtensions.Count);
                Assert.Equal(10, category.ElementExtensions[0].GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
                Assert.Equal("ExtensionObject", category.ElementExtensions[0].OuterName);
                Assert.Empty(category.ElementExtensions[0].OuterNamespace);
                Assert.Equal("ExtensionObject", category.ElementExtensions[1].OuterName);
                Assert.Equal("http://www.w3.org/TR/html4/", category.ElementExtensions[1].OuterNamespace);
            }
        }

        [Fact]
        public void LoadElementExtensions_NullCategories_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("category", () => Formatter.LoadElementExtensionsEntryPoint(reader, (SyndicationCategory)null, int.MaxValue));
        }

        [Fact]
        public void LoadElementExtensions_Item_Success()
        {
            var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

            using (var stringReader = new StringReader(@"<ExtensionObject><Value>10</Value></ExtensionObject><ExtensionObject xmlns=""http://www.w3.org/TR/html4/""><Value>11</Value></ExtensionObject>"))
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                var item = new SyndicationItem();
                Formatter.LoadElementExtensionsEntryPoint(reader, item, int.MaxValue);

                Assert.Equal(2, item.ElementExtensions.Count);
                Assert.Equal(10, item.ElementExtensions[0].GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
                Assert.Equal("ExtensionObject", item.ElementExtensions[0].OuterName);
                Assert.Empty(item.ElementExtensions[0].OuterNamespace);
                Assert.Equal("ExtensionObject", item.ElementExtensions[1].OuterName);
                Assert.Equal("http://www.w3.org/TR/html4/", item.ElementExtensions[1].OuterNamespace);
            }
        }

        [Fact]
        public void LoadElementExtensions_NullItem_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("item", () => Formatter.LoadElementExtensionsEntryPoint(reader, (SyndicationItem)null, int.MaxValue));
        }

        [Fact]
        public void LoadElementExtensions_Link_Success()
        {
            var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

            using (var stringReader = new StringReader(@"<ExtensionObject><Value>10</Value></ExtensionObject><ExtensionObject xmlns=""http://www.w3.org/TR/html4/""><Value>11</Value></ExtensionObject>"))
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                var link = new SyndicationLink();
                Formatter.LoadElementExtensionsEntryPoint(reader, link, int.MaxValue);

                Assert.Equal(2, link.ElementExtensions.Count);
                Assert.Equal(10, link.ElementExtensions[0].GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
                Assert.Equal("ExtensionObject", link.ElementExtensions[0].OuterName);
                Assert.Empty(link.ElementExtensions[0].OuterNamespace);
                Assert.Equal("ExtensionObject", link.ElementExtensions[1].OuterName);
                Assert.Equal("http://www.w3.org/TR/html4/", link.ElementExtensions[1].OuterNamespace);
            }
        }

        [Fact]
        public void LoadElementExtensions_NullLink_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("link", () => Formatter.LoadElementExtensionsEntryPoint(reader, (SyndicationLink)null, int.MaxValue));
        }

        [Fact]
        public void LoadElementExtensions_Person_Success()
        {
            var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

            using (var stringReader = new StringReader(@"<ExtensionObject><Value>10</Value></ExtensionObject><ExtensionObject xmlns=""http://www.w3.org/TR/html4/""><Value>11</Value></ExtensionObject>"))
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                var person = new SyndicationPerson();
                Formatter.LoadElementExtensionsEntryPoint(reader, person, int.MaxValue);

                Assert.Equal(2, person.ElementExtensions.Count);
                Assert.Equal(10, person.ElementExtensions[0].GetObject<ExtensionObject>(new XmlSerializer(typeof(ExtensionObject))).Value);
                Assert.Equal("ExtensionObject", person.ElementExtensions[0].OuterName);
                Assert.Empty(person.ElementExtensions[0].OuterNamespace);
                Assert.Equal("ExtensionObject", person.ElementExtensions[1].OuterName);
                Assert.Equal("http://www.w3.org/TR/html4/", person.ElementExtensions[1].OuterNamespace);
            }
        }

        [Fact]
        public void LoadElementExtensions_NullPerson_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("person", () => Formatter.LoadElementExtensionsEntryPoint(reader, (SyndicationPerson)null, int.MaxValue));
        }

        [Fact]
        public void LoadElementExtensions_NullReader_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("readerOverUnparsedExtensions", () => Formatter.LoadElementExtensionsEntryPoint(null, new SyndicationCategory(), int.MaxValue));
            AssertExtensions.Throws<ArgumentNullException>("readerOverUnparsedExtensions", () => Formatter.LoadElementExtensionsEntryPoint(null, new SyndicationItem(), int.MaxValue));
            AssertExtensions.Throws<ArgumentNullException>("readerOverUnparsedExtensions", () => Formatter.LoadElementExtensionsEntryPoint(null, new SyndicationLink(), int.MaxValue));
            AssertExtensions.Throws<ArgumentNullException>("readerOverUnparsedExtensions", () => Formatter.LoadElementExtensionsEntryPoint(null, new SyndicationPerson(), int.MaxValue));
        }

        [Fact]
        public void LoadElementExtensions_NegativeMaxExtensionSize_ThrowsArgumentOutOfRangeException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxExtensionSize", () => Formatter.LoadElementExtensionsEntryPoint(reader, new SyndicationCategory(), -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxExtensionSize", () => Formatter.LoadElementExtensionsEntryPoint(reader, new SyndicationItem(), -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxExtensionSize", () => Formatter.LoadElementExtensionsEntryPoint(reader, new SyndicationLink(), -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxExtensionSize", () => Formatter.LoadElementExtensionsEntryPoint(reader, new SyndicationPerson(), -1));
        }

        [Fact]
        public void ToString_Invoke_ReturnsExpected()
        {
            var formatter = new Formatter();
            Assert.Equal("System.ServiceModel.Syndication.Tests.SyndicationItemFormatterTests+Formatter, SyndicationVersion=Version", formatter.ToString());
        }

        public static IEnumerable<object[]> TryParseAttribute_TestData()
        {
            yield return new object[] { null, null, null, null, false };
            yield return new object[] { "", "", "", "", false };
            yield return new object[] { "name", "ns", "value", "version", false };
            yield return new object[] { "xmlns", "ns", "value", "version", true };
            yield return new object[] { "name", "http://www.w3.org/2000/xmlns/", "value", "version", true };
            yield return new object[] { "type", "ns", "value", "version", false };
            yield return new object[] { "name", "http://www.w3.org/2001/XMLSchema-instance", "value", "version", false };
        }

        [Theory]
        [MemberData(nameof(TryParseAttribute_TestData))]
        public void TryParseAttribute_Category_ReturnsExpected(string name, string ns, string value, string version, bool expected)
        {
            Assert.Equal(expected, Formatter.TryParseAttributeEntryPoint(name, ns, value, new SyndicationCategory(), version));
        }

        [Fact]
        public void TryParseAttribute_NullCategories_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("category", () => Formatter.TryParseAttributeEntryPoint("name", "namespace", "value", (SyndicationCategory)null, "version"));
        }

        [Theory]
        [MemberData(nameof(TryParseAttribute_TestData))]
        public void TryParseAttribute_Item_ReturnsExpected(string name, string ns, string value, string version, bool expected)
        {
            Assert.Equal(expected, Formatter.TryParseAttributeEntryPoint(name, ns, value, new SyndicationItem(), version));
        }

        [Fact]
        public void TryParseAttribute_NullItem_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("item", () => Formatter.TryParseAttributeEntryPoint("name", "namespace", "value", (SyndicationItem)null, "version"));
        }

        [Theory]
        [MemberData(nameof(TryParseAttribute_TestData))]
        public void TryParseAttribute_Link_ReturnsExpected(string name, string ns, string value, string version, bool expected)
        {
            Assert.Equal(expected, Formatter.TryParseAttributeEntryPoint(name, ns, value, new SyndicationLink(), version));
        }

        [Fact]
        public void TryParseAttribute_NullLink_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("link", () => Formatter.TryParseAttributeEntryPoint("name", "namespace", "value", (SyndicationLink)null, "version"));
        }

        [Theory]
        [MemberData(nameof(TryParseAttribute_TestData))]
        public void TryParseAttribute_Person_ReturnsExpected(string name, string ns, string value, string version, bool expected)
        {
            Assert.Equal(expected, Formatter.TryParseAttributeEntryPoint(name, ns, value, new SyndicationPerson(), version));
        }

        [Fact]
        public void TryParseAttribute_NullPerson_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("person", () => Formatter.TryParseAttributeEntryPoint("name", "namespace", "value", (SyndicationPerson)null, "version"));
        }

        public static IEnumerable<object[]> TryParseContent_TestData()
        {
            yield return new object[] { null, null, null };
            yield return new object[] { new XElement("Name").CreateReader(), "", "" };
            yield return new object[] { new XElement("Name").CreateReader(), "contentType", "version" };
        }

        [Theory]
        [MemberData(nameof(TryParseContent_TestData))]
        public void TryParseContent_Invoke_ReturnsFalse(XmlReader reader, string contentType, string version)
        {
            Assert.False(Formatter.TryParseContentEntryPoint(reader, new SyndicationItem(), contentType, version, out SyndicationContent content));
            Assert.Null(content);
        }

        [Fact]
        public void TryPrseContent_NullItem_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => Formatter.TryParseContentEntryPoint(new XElement("Name").CreateReader(), null, "contentType", "version", out SyndicationContent content));
        }

        public static IEnumerable<object[]> TryParseElement_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new XElement("Name").CreateReader(), "" };
            yield return new object[] { new XElement("Name").CreateReader(), "version" };
        }

        [Theory]
        [MemberData(nameof(TryParseElement_TestData))]
        public void TryParseElement_Category_ReturnsFalse(XmlReader reader, string version)
        {
            Assert.False(Formatter.TryParseElementEntryPoint(reader, new SyndicationCategory(), version));
        }

        [Fact]
        public void TryParseElement_NullCategory_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("category", () => Formatter.TryParseElementEntryPoint(reader, (SyndicationCategory)null, "version"));
        }

        [Theory]
        [MemberData(nameof(TryParseElement_TestData))]
        public void TryParseElement_Item_Success(XmlReader reader, string version)
        {
            Assert.False(Formatter.TryParseElementEntryPoint(reader, new SyndicationItem(), version));
        }

        [Fact]
        public void TryParseElement_Item_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("item", () => Formatter.TryParseElementEntryPoint(reader, (SyndicationItem)null, "version"));
        }

        [Theory]
        [MemberData(nameof(TryParseElement_TestData))]
        public void TryParseElement_Link_Success(XmlReader reader, string version)
        {
            Assert.False(Formatter.TryParseElementEntryPoint(reader, new SyndicationLink(), version));
        }

        [Fact]
        public void TryParseElement_NullLink_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("link", () => Formatter.TryParseElementEntryPoint(reader, (SyndicationLink)null, "version"));
        }

        [Theory]
        [MemberData(nameof(TryParseElement_TestData))]
        public void TryParseElement_Person_Success(XmlReader reader, string version)
        {
            Assert.False(Formatter.TryParseElementEntryPoint(reader, new SyndicationPerson(), version));
        }

        [Fact]
        public void TryParseElement_NullDocument_ThrowsArgumentNullException()
        {
            XmlReader reader = new XElement("Name").CreateReader();
            AssertExtensions.Throws<ArgumentNullException>("person", () => Formatter.TryParseElementEntryPoint(reader, (SyndicationPerson)null, "version"));
        }

        public static IEnumerable<object[]> Version_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { "" };
            yield return new object[] { "version" };
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteAttributeExtensions_Category_Success(string version)
        {
            var category = new SyndicationCategory();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, category, version));

            category.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            category.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            category.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, category, version));
        }

        [Fact]
        public void WriteAttributeExtensions_NullCategory_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("category", () => Formatter.WriteAttributeExtensionsEntryPoint(writer, (SyndicationCategory)null, "version"));
            }
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteAttributeExtensions_Item_Success(string version)
        {
            var item = new SyndicationItem();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, item, version));

            item.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            item.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            item.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, item, version));
        }

        [Fact]
        public void WriteAttributeExtensions_NullItem_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("item", () => Formatter.WriteAttributeExtensionsEntryPoint(writer, (SyndicationItem)null, "version"));
            }
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteAttributeExtensions_Link_Success(string version)
        {
            var link = new SyndicationLink();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, link, version));

            link.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            link.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            link.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, link, version));
        }

        [Fact]
        public void WriteAttributeExtensions_NullLink_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("link", () => Formatter.WriteAttributeExtensionsEntryPoint(writer, (SyndicationLink)null, "version"));
            }
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteAttributeExtensions_Person_Success(string version)
        {
            var person = new SyndicationPerson();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, person, version));

            person.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            person.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            person.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => Formatter.WriteAttributeExtensionsEntryPoint(writer, person, version));
        }

        [Fact]
        public void WriteAttributeExtensions_NullPerson_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("person", () => Formatter.WriteAttributeExtensionsEntryPoint(writer, (SyndicationPerson)null, "version"));
            }
        }

        [Fact]
        public void WriteAttributeExtensions_NullWriter_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteAttributeExtensionsEntryPoint(null, new SyndicationCategory(), "version"));
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteAttributeExtensionsEntryPoint(null, new SyndicationItem(), "version"));
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteAttributeExtensionsEntryPoint(null, new SyndicationLink(), "version"));
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteAttributeExtensionsEntryPoint(null, new SyndicationPerson(), "version"));
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteElementExtensions_Category_Success(string version)
        {
            var category = new SyndicationCategory();
            var formatter = new Formatter();
            CompareHelper.AssertEqualWriteOutput("", writer => formatter.WriteElementExtensionsEntryPoint(writer, category, version));

            category.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            category.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<SyndicationItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</SyndicationItemFormatterTests.ExtensionObject>
<SyndicationItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</SyndicationItemFormatterTests.ExtensionObject>", writer => formatter.WriteElementExtensionsEntryPoint(writer, category, version));
        }

        [Fact]
        public void WriteElementExtensions_NullCategory_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Formatter();
                AssertExtensions.Throws<ArgumentNullException>("category", () => formatter.WriteElementExtensionsEntryPoint(writer, (SyndicationCategory)null, "version"));
            }
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteElementExtensions_Item_Success(string version)
        {
            var item = new SyndicationItem();
            CompareHelper.AssertEqualWriteOutput("", writer => Formatter.WriteElementExtensionsEntryPoint(writer, item, version));

            item.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            item.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<SyndicationItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</SyndicationItemFormatterTests.ExtensionObject>
<SyndicationItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</SyndicationItemFormatterTests.ExtensionObject>", writer => Formatter.WriteElementExtensionsEntryPoint(writer, item, version));
        }

        [Fact]
        public void WriteElementExtensions_NullItem_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                AssertExtensions.Throws<ArgumentNullException>("item", () => Formatter.WriteElementExtensionsEntryPoint(writer, null, "version"));
            }
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteElementExtensions_Link_Success(string version)
        {
            var link = new SyndicationLink();
            var formatter = new Formatter();
            CompareHelper.AssertEqualWriteOutput("", writer => formatter.WriteElementExtensionsEntryPoint(writer, link, version));

            link.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            link.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<SyndicationItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</SyndicationItemFormatterTests.ExtensionObject>
<SyndicationItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</SyndicationItemFormatterTests.ExtensionObject>", writer => formatter.WriteElementExtensionsEntryPoint(writer, link, version));
        }

        [Fact]
        public void WriteElementExtensions_NullLink_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Formatter();
                AssertExtensions.Throws<ArgumentNullException>("link", () => formatter.WriteElementExtensionsEntryPoint(writer, (SyndicationLink)null, "version"));
            }
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void WriteElementExtensions_Person_Success(string version)
        {
            var person = new SyndicationPerson();
            var formatter = new Formatter();
            CompareHelper.AssertEqualWriteOutput("", writer => formatter.WriteElementExtensionsEntryPoint(writer, person, version));

            person.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            person.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<SyndicationItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</SyndicationItemFormatterTests.ExtensionObject>
<SyndicationItemFormatterTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</SyndicationItemFormatterTests.ExtensionObject>", writer => formatter.WriteElementExtensionsEntryPoint(writer, person, version));
        }

        [Fact]
        public void WriteElementExtensions_NullPerson_ThrowsArgumentNullException()
        {
            using (var stringWriter = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(stringWriter))
            {
                var formatter = new Formatter();
                AssertExtensions.Throws<ArgumentNullException>("person", () => formatter.WriteElementExtensionsEntryPoint(writer, (SyndicationPerson)null, "version"));
            }
        }

        [Fact]
        public void WriteElementExtensions_NullWriter_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("writer", () => Formatter.WriteElementExtensionsEntryPoint(null, new SyndicationItem(), "version"));
        }

        public class NullSyndicationItem : SyndicationItem
        {
            protected override SyndicationCategory CreateCategory() => null;

            protected override SyndicationLink CreateLink() => null;

            protected override SyndicationPerson CreatePerson() => null;
        }

        public class Formatter : SyndicationItemFormatter
        {
            public Formatter() : base() { }
            public Formatter(SyndicationItem itemToWrite) : base(itemToWrite) { }

            public static SyndicationCategory CreateCategoryEntryPoint(SyndicationItem item) => CreateCategory(item);

            public static SyndicationLink CreateLinkEntryPoint(SyndicationItem item) => CreateLink(item);

            public static SyndicationPerson CreatePersonEntryPoint(SyndicationItem item) => CreatePerson(item);

            public static void LoadElementExtensionsEntryPoint(XmlReader reader, SyndicationCategory category, int maxExtensionSize) => LoadElementExtensions(reader, category, maxExtensionSize);

            public static void LoadElementExtensionsEntryPoint(XmlReader reader, SyndicationItem item, int maxExtensionSize) => LoadElementExtensions(reader, item, maxExtensionSize);

            public static void LoadElementExtensionsEntryPoint(XmlReader reader, SyndicationLink link, int maxExtensionSize) => LoadElementExtensions(reader, link, maxExtensionSize);

            public static void LoadElementExtensionsEntryPoint(XmlReader reader, SyndicationPerson person, int maxExtensionSize) => LoadElementExtensions(reader, person, maxExtensionSize);

            public static bool TryParseAttributeEntryPoint(string name, string ns, string value, SyndicationCategory category, string version)
            {
                return TryParseAttribute(name, ns, value, category, version);
            }

            public static bool TryParseAttributeEntryPoint(string name, string ns, string value, SyndicationItem item, string version)
            {
                return TryParseAttribute(name, ns, value, item, version);
            }

            public static bool TryParseAttributeEntryPoint(string name, string ns, string value, SyndicationLink link, string version)
            {
                return TryParseAttribute(name, ns, value, link, version);
            }

            public static bool TryParseAttributeEntryPoint(string name, string ns, string value, SyndicationPerson person, string version)
            {
                return TryParseAttribute(name, ns, value, person, version);
            }

            public static bool TryParseContentEntryPoint(XmlReader reader, SyndicationItem item, string contentType, string version, out SyndicationContent content)
            {
                return TryParseContent(reader, item, contentType, version, out content);
            }

            public static bool TryParseElementEntryPoint(XmlReader reader, SyndicationCategory category, string version)
            {
                return TryParseElement(reader, category, version);
            }

            public static bool TryParseElementEntryPoint(XmlReader reader, SyndicationItem item, string version)
            {
                return TryParseElement(reader, item, version);
            }

            public static bool TryParseElementEntryPoint(XmlReader reader, SyndicationLink link, string version)
            {
                return TryParseElement(reader, link, version);
            }

            public static bool TryParseElementEntryPoint(XmlReader reader, SyndicationPerson person, string version)
            {
                return TryParseElement(reader, person, version);
            }

            public static void WriteAttributeExtensionsEntryPoint(XmlWriter writer, SyndicationCategory category, string version)
            {
                WriteAttributeExtensions(writer, category, version);
            }

            public static void WriteAttributeExtensionsEntryPoint(XmlWriter writer, SyndicationItem item, string version)
            {
                WriteAttributeExtensions(writer, item, version);
            }

            public static void WriteAttributeExtensionsEntryPoint(XmlWriter writer, SyndicationLink link, string version)
            {
                WriteAttributeExtensions(writer, link, version);
            }

            public static void WriteAttributeExtensionsEntryPoint(XmlWriter writer, SyndicationPerson person, string version)
            {
                WriteAttributeExtensions(writer, person, version);
            }

            public void WriteElementExtensionsEntryPoint(XmlWriter writer, SyndicationCategory category, string version)
            {
                WriteElementExtensions(writer, category, version);
            }

            public static void WriteElementExtensionsEntryPoint(XmlWriter writer, SyndicationItem item, string version)
            {
                WriteElementExtensions(writer, item, version);
            }

            public void WriteElementExtensionsEntryPoint(XmlWriter writer, SyndicationLink link, string version)
            {
                WriteElementExtensions(writer, link, version);
            }

            public void WriteElementExtensionsEntryPoint(XmlWriter writer, SyndicationPerson person, string version)
            {
                WriteElementExtensions(writer, person, version);
            }

            public override string Version => "Version";

            public override bool CanRead(XmlReader reader) => throw new NotImplementedException();

            public override void ReadFrom(XmlReader reader) => throw new NotImplementedException();

            public override void WriteTo(XmlWriter writer) => throw new NotImplementedException();

            protected override SyndicationItem CreateItemInstance() => throw new NotImplementedException();
        }

        [DataContract]
        public class ExtensionObject
        {
            [DataMember]
            public int Value { get; set; }
        }
    }
}
