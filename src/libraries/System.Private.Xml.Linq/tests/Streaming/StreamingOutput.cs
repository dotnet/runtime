// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using System.Xml.XmlDiff;
using CoreXml.Test.XLinq;

using Xunit;

namespace XDocumentTests.Streaming
{
    public class XStreamingElementAPI
    {
        private XDocument _xDoc = null;
        private XDocument _xmlDoc = null;
        private XmlDiff _diff;
        private bool _invokeStatus = false;
        private bool _invokeError = false;
        private Stream _sourceStream = null;
        private Stream _targetStream = null;

        public void GetFreshStream()
        {
            _sourceStream = new MemoryStream();
            _targetStream = new MemoryStream();
        }

        public void ResetStreamPos()
        {
            if (_sourceStream.CanSeek)
            {
                _sourceStream.Position = 0;
            }
            if (_targetStream.CanSeek)
            {
                _targetStream.Position = 0;
            }
        }

        private XmlDiff Diff
        {
            get
            {
                return _diff ?? (_diff = new XmlDiff());
            }
        }

        [Fact]
        public void XNameAsNullConstructor()
        {
            Assert.Throws<ArgumentNullException>(() => new XStreamingElement(null));
        }

        [Fact]
        public void XNameAsEmptyStringConstructor()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new XStreamingElement(string.Empty));
            Assert.Throws<XmlException>(() => new XStreamingElement(" "));
        }

        [Fact]
        public void XNameConstructor()
        {
            XStreamingElement streamElement = new XStreamingElement("contact");
            Assert.Equal("<contact />", streamElement.ToString());
        }

        [Fact]
        public void XNameWithNamespaceConstructor()
        {
            XNamespace ns = @"http:\\www.contacts.com\";
            XElement contact = new XElement(ns + "contact");
            XStreamingElement streamElement = new XStreamingElement(ns + "contact");
            GetFreshStream();
            streamElement.Save(_sourceStream);
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void XNameAndNullObjectConstructor()
        {
            XStreamingElement streamElement = new XStreamingElement("contact", null);
            Assert.Equal("<contact />", streamElement.ToString());
        }

        [Fact]
        public void XNameAndXElementObjectConstructor()
        {
            XElement contact = new XElement("contact", new XElement("phone", "925-555-0134"));
            XStreamingElement streamElement = new XStreamingElement("contact", contact.Element("phone"));
            GetFreshStream();
            streamElement.Save(_sourceStream);
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void XNameAndEmptyStringConstructor()
        {
            XElement contact = new XElement("contact", "");
            XStreamingElement streamElement = new XStreamingElement("contact", "");
            GetFreshStream();
            streamElement.Save(_sourceStream);
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void XDocTypeInXStreamingElement()
        {
            XDocumentType node = new XDocumentType(
                "DOCTYPE",
                "note",
                "SYSTEM",
                "<!ELEMENT note (to,from,heading,body)><!ELEMENT to (#PCDATA)><!ELEMENT from (#PCDATA)><!ELEMENT heading (#PCDATA)><!ELEMENT body (#PCDATA)>");

            XStreamingElement streamElement = new XStreamingElement("Root", node);
            Assert.Throws<InvalidOperationException>(() => streamElement.Save(new MemoryStream()));
        }

        [Fact]
        public void XmlDeclInXStreamingElement()
        {
            XDeclaration node = new XDeclaration("1.0", "utf-8", "yes");
            XElement element = new XElement("Root", node);
            XStreamingElement streamElement = new XStreamingElement("Root", node);
            GetFreshStream();
            streamElement.Save(_sourceStream);
            element.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void XCDataInXStreamingElement()
        {
            XCData node = new XCData("CDATA Text '%^$#@!&*()'");
            XElement element = new XElement("Root", node);
            XStreamingElement streamElement = new XStreamingElement("Root", node);
            GetFreshStream();
            streamElement.Save(_sourceStream);
            element.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void XCommentInXStreamingElement()
        {
            XComment node = new XComment("This is a comment");
            XElement element = new XElement("Root", node);
            XStreamingElement streamElement = new XStreamingElement("Root", node);
            GetFreshStream();
            streamElement.Save(_sourceStream);
            element.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void XDocInXStreamingElement()
        {
            InputSpace.Contacts(ref _xDoc, ref _xmlDoc);
            XStreamingElement streamElement = new XStreamingElement("Root", _xDoc);
            Assert.Throws<InvalidOperationException>(() => streamElement.Save(new MemoryStream()));
        }

        [Fact]
        public void XNameAndCollectionObjectConstructor()
        {
            XElement contact = new XElement(
                "contacts",
                new XElement("contact1", "jane"),
                new XElement("contact2", "john"));
            List<object> list = new List<object>();
            list.Add(contact.Element("contact1"));
            list.Add(contact.Element("contact2"));
            XStreamingElement streamElement = new XStreamingElement("contacts", list);
            GetFreshStream();
            streamElement.Save(_sourceStream);
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void XNameAndObjectArrayConstructor()
        {
            XElement contact = new XElement(
                "contact",
                new XElement("name", "jane"),
                new XElement("phone", new XAttribute("type", "home"), "925-555-0134"));
            XStreamingElement streamElement = new XStreamingElement(
                "contact",
                new object[] { contact.Element("name"), contact.Element("phone") });
            GetFreshStream();
            streamElement.Save(_sourceStream);
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void NamePropertyGet()
        {
            XStreamingElement streamElement = new XStreamingElement("contact");
            Assert.Equal("contact", streamElement.Name);
        }

        [Fact]
        public void NamePropertySet()
        {
            XStreamingElement streamElement = new XStreamingElement("ThisWillChangeToContact");
            streamElement.Name = "contact";
            Assert.Equal("contact", streamElement.Name.ToString());
        }

        [Fact]
        public void NamePropertySetInvalid()
        {
            XStreamingElement streamElement = new XStreamingElement("ThisWillChangeToInValidName");
            Assert.Throws<ArgumentNullException>(() => streamElement.Name = null);
        }

        [Fact]
        public void XMLPropertyGet()
        {
            XElement contact = new XElement("contact", new XElement("phone", "925-555-0134"));
            XStreamingElement streamElement = new XStreamingElement("contact", contact.Element("phone"));
            Assert.Equal(contact.ToString(SaveOptions.None), streamElement.ToString(SaveOptions.None));
        }

        [Fact]
        public void AddWithNull()
        {
            XStreamingElement streamElement = new XStreamingElement("contact");
            streamElement.Add(null);
            Assert.Equal("<contact />", streamElement.ToString());
        }

        [Theory]
        [InlineData(9255550134)]
        [InlineData("9255550134")]
        [InlineData(9255550134.0)]
        public void AddObject(object content)
        {
            XElement contact = new XElement("phone", content);
            XStreamingElement streamElement = new XStreamingElement("phone");
            streamElement.Add(content);
            GetFreshStream();
            streamElement.Save(_sourceStream);
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void AddTimeSpanObject()
        {
            XElement contact = new XElement("Time", TimeSpan.FromMinutes(12));
            XStreamingElement streamElement = new XStreamingElement("Time");
            streamElement.Add(TimeSpan.FromMinutes(12));
            GetFreshStream();
            streamElement.Save(_sourceStream);
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void AddAttribute()
        {
            XElement contact = new XElement("phone", new XAttribute("type", "home"), "925-555-0134");
            XStreamingElement streamElement = new XStreamingElement("phone");
            streamElement.Add(contact.Attribute("type"));
            streamElement.Add("925-555-0134");
            GetFreshStream();
            streamElement.Save(_sourceStream);
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        //An attribute cannot be written after content.
        [Fact]
        public void AddAttributeAfterContent()
        {
            XElement contact = new XElement("phone", new XAttribute("type", "home"), "925-555-0134");
            XStreamingElement streamElement = new XStreamingElement("phone", "925-555-0134");
            streamElement.Add(contact.Attribute("type"));
            using (XmlWriter w = XmlWriter.Create(new MemoryStream(), null))
            {
                Assert.Throws<InvalidOperationException>(() => streamElement.WriteTo(w));
            }
        }

        [Fact]
        public void AddIEnumerableOfNulls()
        {
            XElement element = new XElement("root", GetNulls());
            XStreamingElement streamElement = new XStreamingElement("root");
            streamElement.Add(GetNulls());
            GetFreshStream();
            streamElement.Save(_sourceStream);
            element.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        ///<summary>
        /// This function returns an IEnumeralb of nulls
        ///</summary>
        public IEnumerable<XNode> GetNulls()
        {
            return Enumerable.Repeat((XNode)null, 1000);
        }

        [Fact]
        public void AddIEnumerableOfXNodes()
        {
            XElement x = InputSpace.GetElement(100, 3);
            XElement element = new XElement("root", x.Nodes());
            XStreamingElement streamElement = new XStreamingElement("root");
            streamElement.Add(x.Nodes());
            GetFreshStream();
            streamElement.Save(_sourceStream);
            element.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void AddIEnumerableOfMixedNodes()
        {
            XElement element = new XElement("root", GetMixedNodes());
            XStreamingElement streamElement = new XStreamingElement("root");
            streamElement.Add(GetMixedNodes());
            GetFreshStream();
            streamElement.Save(_sourceStream);
            element.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        ///<summary>
        /// This function returns mixed IEnumerable of XObjects with XAttributes first
        ///</summary>
        public IEnumerable<XObject> GetMixedNodes()
        {
            InputSpace.Load("Word.xml", ref _xDoc, ref _xmlDoc);
            foreach (XAttribute x in _xDoc.Root.Attributes())
                yield return x;
            foreach (XNode n in _xDoc.Root.DescendantNodes())
                yield return n;
        }

        [Fact]
        public void AddIEnumerableOfXNodesPlusString()
        {
            InputSpace.Contacts(ref _xDoc, ref _xmlDoc);
            XElement element = new XElement("contacts", _xDoc.Root.DescendantNodes(), "This String");
            XStreamingElement streamElement = new XStreamingElement("contacts");
            streamElement.Add(_xDoc.Root.DescendantNodes(), "This String");
            GetFreshStream();
            streamElement.Save(_sourceStream);
            element.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void AddIEnumerableOfXNodesPlusAttribute()
        {
            InputSpace.Contacts(ref _xDoc, ref _xmlDoc);
            XAttribute xAttrib = new XAttribute("Attribute", "Value");
            XElement element = new XElement("contacts", xAttrib, _xDoc.Root.DescendantNodes());
            XStreamingElement streamElement = new XStreamingElement("contacts");
            streamElement.Add(xAttrib, _xDoc.Root.DescendantNodes());
            GetFreshStream();
            streamElement.Save(_sourceStream);
            element.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void SaveWithNull()
        {
            XStreamingElement streamElement = new XStreamingElement("phone", "925-555-0134");
            Assert.Throws<ArgumentNullException>(() => streamElement.Save((XmlWriter)null));
        }

        [Fact]
        public void SaveWithXmlTextWriter()
        {
            XElement contact = new XElement(
                "contacts",
                new XElement("contact", "jane"),
                new XElement("contact", "john"));
            XStreamingElement streamElement = new XStreamingElement("contacts", contact.Elements());
            GetFreshStream();
            TextWriter w = new StreamWriter(_sourceStream);
            streamElement.Save(w);
            w.Flush();
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void SaveTwice()
        {
            XElement contact = new XElement(
                "contacts",
                new XElement("contact", "jane"),
                new XElement("contact", "john"));
            XStreamingElement streamElement = new XStreamingElement("contacts", contact.Elements());
            GetFreshStream();
            streamElement.Save(_sourceStream);
            _sourceStream.Position = 0;
            streamElement.Save(_sourceStream);
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void WriteToWithNull()
        {
            XStreamingElement streamElement = new XStreamingElement("phone", "925-555-0134");
            Assert.Throws<ArgumentNullException>(() => streamElement.WriteTo((XmlWriter)null));
        }

        [Fact]
        public void ModifyOriginalElement()
        {
            XElement contact = new XElement(
                "contact",
                new XElement("name", "jane"),
                new XElement("phone", new XAttribute("type", "home"), "925-555-0134"));
            XStreamingElement streamElement = new XStreamingElement("contact", new object[] { contact.Elements() });
            foreach (XElement x in contact.Elements())
            {
                x.Remove();
            }
            GetFreshStream();
            streamElement.Save(_sourceStream);
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void NestedXStreamingElement()
        {
            XElement name = new XElement("name", "jane");
            XElement phone = new XElement("phone", new XAttribute("type", "home"), "925-555-0134");
            XElement contact = new XElement("contact", name, new XElement("phones", phone));
            XStreamingElement streamElement = new XStreamingElement(
                "contact",
                name,
                new XStreamingElement("phones", phone));
            GetFreshStream();
            streamElement.Save(_sourceStream);
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void NestedXStreamingElementPlusIEnumerable()
        {
            InputSpace.Contacts(ref _xDoc, ref _xmlDoc);
            XElement element = new XElement("contacts", new XElement("Element", "Value"), _xDoc.Root.DescendantNodes());
            XStreamingElement streamElement = new XStreamingElement("contacts");
            streamElement.Add(new XStreamingElement("Element", "Value"), _xDoc.Root.DescendantNodes());
            GetFreshStream();
            streamElement.Save(_sourceStream);
            element.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void IEnumerableLazinessTest1()
        {
            XElement name = new XElement("name", "jane");
            XElement phone = new XElement("phone", new XAttribute("type", "home"), "925-555-0134");
            XElement contact = new XElement("contact", name, phone);
            IEnumerable<XElement> elements = contact.Elements();
            name.Remove();
            phone.Remove();
            XStreamingElement streamElement = new XStreamingElement("contact", new object[] { elements });
            GetFreshStream();
            streamElement.Save(_sourceStream);
            contact.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void IEnumerableLazinessTest2()
        {
            XElement name = new XElement("name", "jane");
            XElement phone = new XElement("phone", new XAttribute("type", "home"), "925-555-0134");
            XElement contact = new XElement("contact", name, phone);
            // During debug this test will not work correctly since ToString() of
            // streamElement gets called for displaying the value in debugger local window.
            XStreamingElement streamElement = new XStreamingElement("contact", GetElements(contact));
            GetFreshStream();
            contact.Save(_targetStream);
            _invokeStatus = true;
            streamElement.Save(_sourceStream);
            Assert.False(_invokeError, "IEnumerable walked before expected");
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        ///<summary>
        /// This function is used in above variation to make sure that the 
        /// IEnumerable is indeed walked lazily
        ///</summary>
        public IEnumerable<XElement> GetElements(XElement element)
        {
            if (_invokeStatus == false)
            {
                _invokeError = true;
            }

            foreach (XElement x in element.Elements())
            {
                yield return x;
            }
        }

        [Fact]
        public void XStreamingElementInXElement()
        {
            XElement element = new XElement("contacts");
            XStreamingElement streamElement = new XStreamingElement("contact", "SomeValue");
            element.Add(streamElement);
            XElement x = element.Element("contact");
            GetFreshStream();
            streamElement.Save(_sourceStream);
            x.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }

        [Fact]
        public void XStreamingElementInXDocument()
        {
            _xDoc = new XDocument();
            XStreamingElement streamElement = new XStreamingElement("contacts", "SomeValue");
            _xDoc.Add(streamElement);
            GetFreshStream();
            streamElement.Save(_sourceStream);
            _xDoc.Save(_targetStream);
            ResetStreamPos();
            Assert.True(Diff.Compare(_sourceStream, _targetStream));
        }
    }
}
