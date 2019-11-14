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
    public class SyndicationPersonTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var person = new SyndicationPerson();
            Assert.Empty(person.AttributeExtensions);
            Assert.Empty(person.ElementExtensions);
            Assert.Null(person.Email);
            Assert.Null(person.Name);
            Assert.Null(person.Uri);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("email")]
        public void Ctor_String(string email)
        {
            var person = new SyndicationPerson(email);
            Assert.Empty(person.AttributeExtensions);
            Assert.Empty(person.ElementExtensions);
            Assert.Equal(email, person.Email);
            Assert.Null(person.Name);
            Assert.Null(person.Uri);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("", "", "")]
        [InlineData("email", "name", "uri")]
        public void Ctor_String_String_String(string email, string name, string uri)
        {
            var person = new SyndicationPerson(email, name, uri);
            Assert.Empty(person.AttributeExtensions);
            Assert.Empty(person.ElementExtensions);
            Assert.Equal(email, person.Email);
            Assert.Equal(name, person.Name);
            Assert.Equal(uri, person.Uri);
        }

        [Fact]
        public void Ctor_SyndicationPerson_Full()
        {
            var original = new SyndicationPerson("email", "name", "uri");
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");
            original.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            var clone = new SyndicationPersonSubclass(original);
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.NotSame(clone.ElementExtensions, original.ElementExtensions);
            Assert.Equal(1, clone.ElementExtensions.Count);
            Assert.Equal(10, clone.ElementExtensions[0].GetObject<ExtensionObject>().Value);

            Assert.Equal("email", clone.Email);
            Assert.Equal("name", clone.Name);
            Assert.Equal("uri", clone.Uri);
        }

        [Fact]
        public void Ctor_SyndicationPerson_Empty()
        {
            var original = new SyndicationPerson();
            var clone = new SyndicationPersonSubclass(original);
            Assert.Empty(clone.AttributeExtensions);
            Assert.Empty(clone.ElementExtensions);
            Assert.Null(clone.Email);
            Assert.Null(clone.Name);
            Assert.Null(clone.Uri);
        }

        [Fact]
        public void Ctor_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => new SyndicationPersonSubclass(null));
        }

        [Fact]
        public void Clone_Full_ReturnsExpected()
        {
            var original = new SyndicationPerson("email", "name", "uri");
            original.AttributeExtensions.Add(new XmlQualifiedName("name"), "value");
            original.ElementExtensions.Add(new ExtensionObject { Value = 10 });

            SyndicationPerson clone = original.Clone();
            Assert.NotSame(clone.AttributeExtensions, original.AttributeExtensions);
            Assert.Equal(1, clone.AttributeExtensions.Count);
            Assert.Equal("value", clone.AttributeExtensions[new XmlQualifiedName("name")]);

            Assert.NotSame(clone.ElementExtensions, original.ElementExtensions);
            Assert.Equal(1, clone.ElementExtensions.Count);
            Assert.Equal(10, clone.ElementExtensions[0].GetObject<ExtensionObject>().Value);

            Assert.Equal("email", clone.Email);
            Assert.Equal("name", clone.Name);
            Assert.Equal("uri", clone.Uri);
        }

        [Fact]
        public void Clone_Empty_ReturnsExpected()
        {
            var original = new SyndicationPerson();
            SyndicationPerson clone = original.Clone();
            Assert.Empty(clone.AttributeExtensions);
            Assert.Empty(clone.ElementExtensions);
            Assert.Null(clone.Email);
            Assert.Null(clone.Name);
            Assert.Null(clone.Uri);
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
            var person = new SyndicationPersonSubclass();
            Assert.False(person.TryParseAttributeEntryPoint(name, ns, value, version));
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
            var person = new SyndicationPersonSubclass();
            Assert.False(person.TryParseElementEntryPoint(reader, version));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteAttributeExtensions_Invoke_ReturnsExpected(string version)
        {
            var person = new SyndicationPersonSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => person.WriteAttributeExtensionsEntryPoint(writer, version));

            person.AttributeExtensions.Add(new XmlQualifiedName("name1"), "value");
            person.AttributeExtensions.Add(new XmlQualifiedName("name2", "namespace"), "");
            person.AttributeExtensions.Add(new XmlQualifiedName("name3"), null);
            CompareHelper.AssertEqualWriteOutput(@"name1=""value"" d0p1:name2="""" name3=""""", writer => person.WriteAttributeExtensionsEntryPoint(writer, "version"));
        }

        [Fact]
        public void WriteAttributeExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var person = new SyndicationPersonSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => person.WriteAttributeExtensionsEntryPoint(null, "version"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("version")]
        public void WriteElementExtensions_Invoke_ReturnsExpected(string version)
        {
            var person = new SyndicationPersonSubclass();
            CompareHelper.AssertEqualWriteOutput("", writer => person.WriteElementExtensionsEntryPoint(writer, version));

            person.ElementExtensions.Add(new ExtensionObject { Value = 10 });
            person.ElementExtensions.Add(new ExtensionObject { Value = 11 });
            CompareHelper.AssertEqualWriteOutput(
@"<SyndicationPersonTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>10</Value>
</SyndicationPersonTests.ExtensionObject>
<SyndicationPersonTests.ExtensionObject xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System.ServiceModel.Syndication.Tests"">
    <Value>11</Value>
</SyndicationPersonTests.ExtensionObject>", writer => person.WriteElementExtensionsEntryPoint(writer, version));
        }

        [Fact]
        public void WriteElementExtensions_NullWriter_ThrowsArgumentNullException()
        {
            var person = new SyndicationPersonSubclass();
            AssertExtensions.Throws<ArgumentNullException>("writer", () => person.WriteElementExtensionsEntryPoint(null, "version"));
        }

        public class SyndicationPersonSubclass : SyndicationPerson
        {
            public SyndicationPersonSubclass() : base() { }

            public SyndicationPersonSubclass(SyndicationPerson source) : base(source) { }

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
