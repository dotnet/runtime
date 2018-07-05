// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class SyndicationCategoryTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var category = new SyndicationCategory();
            Assert.Empty(category.AttributeExtensions);
            Assert.Empty(category.ElementExtensions);
            Assert.Null(category.Name);
            Assert.Null(category.Scheme);
            Assert.Null(category.Label);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void Ctor_String(string name)
        {
            var category = new SyndicationCategory(name);
            Assert.Empty(category.AttributeExtensions);
            Assert.Empty(category.ElementExtensions);
            Assert.Equal(name, category.Name);
            Assert.Null(category.Scheme);
            Assert.Null(category.Label);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("", "", "")]
        [InlineData("name", "scheme", "label")]
        public void Ctor_String_String_String(string name, string scheme, string label)
        {
            var category = new SyndicationCategory(name, scheme, label);
            Assert.Empty(category.AttributeExtensions);
            Assert.Empty(category.ElementExtensions);
            Assert.Equal(name, category.Name);
            Assert.Equal(scheme, category.Scheme);
            Assert.Equal(label, category.Label);
        }

        [Fact]
        public void Ctor_SyndicationCategory_Full()
        {
            var original = new SyndicationCategory("name", "scheme", "label");
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");
            original.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            SyndicationCategory clone = original.Clone();
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.NotSame(clone.ElementExtensions, original.ElementExtensions);
            Assert.Equal(1, clone.ElementExtensions.Count);
            Assert.Equal(10, clone.ElementExtensions[0].GetObject<ExtensionObject>().Value);

            Assert.Equal("label", clone.Label);
            Assert.Equal("name", clone.Name);
            Assert.Equal("scheme", clone.Scheme);
        }

        [Fact]
        public void Ctor_SyndicationCategory_Empty()
        {
            var original = new SyndicationCategory();
            var clone = new SyndicationCategorySubclass(original);
            Assert.Empty(clone.AttributeExtensions);
            Assert.Empty(clone.ElementExtensions);
            Assert.Null(clone.Label);
            Assert.Null(clone.Name);
            Assert.Null(clone.Scheme);
        }

        [Fact]
        public void Ctor_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => new SyndicationCategorySubclass(null));
        }

        [Fact]
        public void Clone_Full_ReturnsExpected()
        {
            var original = new SyndicationCategory("name", "scheme", "label");
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");
            original.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            SyndicationCategory clone = original.Clone();
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.NotSame(clone.ElementExtensions, original.ElementExtensions);
            Assert.Equal(1, clone.ElementExtensions.Count);
            Assert.Equal(10, clone.ElementExtensions[0].GetObject<ExtensionObject>().Value);

            Assert.Equal("label", clone.Label);
            Assert.Equal("name", clone.Name);
            Assert.Equal("scheme", clone.Scheme);
        }

        [Fact]
        public void Clone_Empty_ReturnsExpected()
        {
            var original = new SyndicationCategory();
            SyndicationCategory clone = original.Clone();
            Assert.Empty(clone.AttributeExtensions);
            Assert.Empty(clone.ElementExtensions);
            Assert.Null(clone.Label);
            Assert.Null(clone.Name);
            Assert.Null(clone.Scheme);
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
            var category = new SyndicationCategorySubclass();
            Assert.False(category.TryParseAttributeEntryPoint(name, ns, value, version));
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
            var category = new SyndicationCategorySubclass();
            Assert.False(category.TryParseElementEntryPoint(reader, version));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteAttributeExtensions_Invoke_ReturnsExpected(string version)
        {
            var category = new SyndicationCategorySubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => category.WriteAttributeExtensionsEntryPoint(writer, version));

            category.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            category.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            category.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => category.WriteAttributeExtensionsEntryPoint(writer, "version"));
        }

        [Fact]
        public void WriteAttributeExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var category = new SyndicationCategorySubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => category.WriteAttributeExtensionsEntryPoint(null, "version"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteElementExtensions_Invoke_ReturnsExpected(string version)
        {
            var category = new SyndicationCategorySubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => category.WriteElementExtensionsEntryPoint(writer, version));

            category.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            category.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<SyndicationCategoryTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</SyndicationCategoryTests.ExtensionObject>
<SyndicationCategoryTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</SyndicationCategoryTests.ExtensionObject>", writer => category.WriteElementExtensionsEntryPoint(writer, version));
        }

        [Fact]
        public void WriteElementExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var category = new SyndicationCategorySubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => category.WriteElementExtensionsEntryPoint(null, "version"));
        }

        private class SyndicationCategorySubclass : SyndicationCategory
        {
            public SyndicationCategorySubclass() : base() { }

            public SyndicationCategorySubclass(SyndicationCategory source) : base(source) { }

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
