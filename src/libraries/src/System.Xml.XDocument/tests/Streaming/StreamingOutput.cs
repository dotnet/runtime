// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XmlDiff;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class StreamingTests : XLinqTestCase
        {
            public partial class XStreamingElementAPI : XLinqTestCase
            {
                private XDocument _xDoc = null;
                private XDocument _xmlDoc = null;
                private XmlDiff _diff = null;
                private bool _invokeStatus = false, _invokeError = false;
                private Stream _sourceStream = null;
                private Stream _targetStream = null;

                public void getFreshStream()
                {
                    _sourceStream = new MemoryStream();
                    _targetStream = new MemoryStream();
                }

                public void resetStreamPos()
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

                public XStreamingElementAPI()
                {
                    _diff = new XmlDiff();
                }

                //[Variation(Priority = 1, Desc = "Constructor - XStreamingElement(null)")]
                public void XNameAsNullConstructor()
                {
                    try
                    {
                        XStreamingElement streamElement = new XStreamingElement(null);
                    }
                    catch (System.ArgumentNullException)
                    {
                        return;
                    }
                    throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "Constructor - XStreamingElement('')")]
                public void XNameAsEmptyStringConstructor()
                {
                    try
                    {
                        XStreamingElement streamElement = new XStreamingElement(" ");
                    }
                    catch (System.Xml.XmlException)
                    {
                        return;
                    }
                    throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Constructor - XStreamingElement(XName)")]
                public void XNameConstructor()
                {
                    XStreamingElement streamElement = new XStreamingElement("contact");
                    if (!streamElement.ToString().Equals("<contact />"))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Constructor - XStreamingElement(XName with Namespace)")]
                public void XNameWithNamespaceConstructor()
                {
                    XNamespace ns = @"http:\\www.contacts.com\";
                    XElement contact = new XElement(ns + "contact");
                    XStreamingElement streamElement = new XStreamingElement(ns + "contact");
                    getFreshStream();
                    streamElement.Save(_sourceStream); contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "Constructor - XStreamingElement(XName, object as null)")]
                public void XNameAndNullObjectConstructor()
                {
                    XStreamingElement streamElement = new XStreamingElement("contact", null);
                    if (!streamElement.ToString().Equals("<contact />"))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Constructor - XStreamingElement(XName, XElement)")]
                public void XNameAndXElementObjectConstructor()
                {
                    XElement contact = new XElement("contact", new XElement("phone", "925-555-0134"));
                    XStreamingElement streamElement = new XStreamingElement("contact", contact.Element("phone"));
                    getFreshStream();
                    streamElement.Save(_sourceStream); contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "Constructor - XStreamingElement(XName, Empty String)")]
                public void XNameAndEmptyStringConstructor()
                {
                    XElement contact = new XElement("contact", "");
                    XStreamingElement streamElement = new XStreamingElement("contact", "");
                    getFreshStream();
                    streamElement.Save(_sourceStream); contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Constructor - XStreamingElement(XName, XDocumentType)")]
                public void XDocTypeInXStreamingElement()
                {
                    XDocumentType node = new XDocumentType("DOCTYPE", "note", "SYSTEM",
                        "<!ELEMENT note (to,from,heading,body)><!ELEMENT to (#PCDATA)><!ELEMENT from (#PCDATA)><!ELEMENT heading (#PCDATA)><!ELEMENT body (#PCDATA)>");
                    try
                    {
                        XStreamingElement streamElement = new XStreamingElement("Root", node);
                        streamElement.Save(new MemoryStream());
                    }
                    catch (System.InvalidOperationException)
                    {
                        return;
                    }
                    throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Constructor - XStreamingElement(XName, XDeclaration)")]
                public void XmlDeclInXStreamingElement()
                {
                    XDeclaration node = new XDeclaration("1.0", "utf-8", "yes");
                    XElement element = new XElement("Root", node);
                    XStreamingElement streamElement = new XStreamingElement("Root", node);
                    getFreshStream();
                    streamElement.Save(_sourceStream); element.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Constructor - XStreamingElement(XName, XCDATA)")]
                public void XCDataInXStreamingElement()
                {
                    XCData node = new XCData("CDATA Text '%^$#@!&*()'");
                    XElement element = new XElement("Root", node);
                    XStreamingElement streamElement = new XStreamingElement("Root", node);
                    getFreshStream();
                    streamElement.Save(_sourceStream); element.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Constructore - XStreamingElement(XName, XComment)")]
                public void XCommentInXStreamingElement()
                {
                    XComment node = new XComment("This is a comment");
                    XElement element = new XElement("Root", node);
                    XStreamingElement streamElement = new XStreamingElement("Root", node);
                    getFreshStream();
                    streamElement.Save(_sourceStream); element.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Constructore - XStreamingElement(XName, XDocument)")]
                public void XDocInXStreamingElement()
                {
                    InputSpace.Contacts(ref _xDoc, ref _xmlDoc);
                    try
                    {
                        XStreamingElement streamElement = new XStreamingElement("Root", _xDoc);
                        streamElement.Save(new MemoryStream());
                    }
                    catch (System.InvalidOperationException)
                    {
                        return;
                    }
                    throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "Constructor - XStreamingElement(XName, object as List<object>)")]
                public void XNameAndCollectionObjectConstructor()
                {
                    XElement contact = new XElement("contacts", new XElement("contact1", "jane"), new XElement("contact2", "john"));
                    List<Object> list = new List<Object>();
                    list.Add(contact.Element("contact1")); list.Add(contact.Element("contact2"));
                    XStreamingElement streamElement = new XStreamingElement("contacts", list);
                    getFreshStream();
                    streamElement.Save(_sourceStream); contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Constructor - XStreamingElement(XName, object[])")]
                public void XNameAndObjectArrayConstructor()
                {
                    XElement contact = new XElement("contact", new XElement("name", "jane"),
                        new XElement("phone", new XAttribute("type", "home"), "925-555-0134"));
                    XStreamingElement streamElement = new XStreamingElement("contact", new object[] { contact.Element("name"), contact.Element("phone") });
                    getFreshStream();
                    streamElement.Save(_sourceStream); contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Name Property - Get")]
                public void NamePropertyGet()
                {
                    XStreamingElement streamElement = new XStreamingElement("contact");
                    if (!streamElement.Name.ToString().Equals("contact"))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Name Property - Set")]
                public void NamePropertySet()
                {
                    XStreamingElement streamElement = new XStreamingElement("ThisWillChangeToContact");
                    streamElement.Name = "contact";
                    if (!streamElement.Name.ToString().Equals("contact"))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "Name Property - Set(InvalIdName)")]
                public void NamePropertySetInvalid()
                {
                    try
                    {
                        XStreamingElement streamElement = new XStreamingElement("ThisWillChangeToInValidName");
                        streamElement.Name = null;
                        streamElement.Name.ToString();
                    }
                    catch (System.ArgumentNullException)
                    {
                        return;
                    }
                    throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "XML Property")]
                public void XMLPropertyGet()
                {
                    XElement contact = new XElement("contact", new XElement("phone", "925-555-0134"));
                    XStreamingElement streamElement = new XStreamingElement("contact", contact.Element("phone"));
                    if (!streamElement.ToString(SaveOptions.None).Equals(contact.ToString(SaveOptions.None)))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "Add(null)")]
                public void AddWithNull()
                {
                    XStreamingElement streamElement = new XStreamingElement("contact");
                    streamElement.Add(null);
                    if (!streamElement.ToString().Equals("<contact />"))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Add(String)", Params = new object[] { "9255550134" })]
                //[Variation(Priority = 0, Desc = "Add(Double)", Params = new object[] { (Double)9255550134 })]
                //[Variation(Priority = 0, Desc = "Add(Int)", Params = new object[] { (Int64)9255550134 })]
                public void AddObject()
                {
                    XElement contact = new XElement("phone", Variation.Params[0]);
                    XStreamingElement streamElement = new XStreamingElement("phone");
                    streamElement.Add(Variation.Params[0]);
                    getFreshStream();
                    streamElement.Save(_sourceStream); contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "Add(TimeSpan)")]
                public void AddTimeSpanObject()
                {
                    XElement contact = new XElement("Time", TimeSpan.FromMinutes(12));
                    XStreamingElement streamElement = new XStreamingElement("Time");
                    streamElement.Add(TimeSpan.FromMinutes(12));
                    getFreshStream();
                    streamElement.Save(_sourceStream); contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Add(XAttribute)")]
                public void AddAttribute()
                {
                    XElement contact = new XElement("phone", new XAttribute("type", "home"), "925-555-0134");
                    XStreamingElement streamElement = new XStreamingElement("phone");
                    streamElement.Add(contact.Attribute("type"));
                    streamElement.Add("925-555-0134");
                    getFreshStream();
                    streamElement.Save(_sourceStream); contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //An attribute cannot be written after content.
                //[Variation(Priority = 1, Desc = "Add(XAttribute) After Content is Added)")]
                public void AddAttributeAfterContent()
                {
                    try
                    {
                        XElement contact = new XElement("phone", new XAttribute("type", "home"), "925-555-0134");
                        XStreamingElement streamElement = new XStreamingElement("phone", "925-555-0134");
                        streamElement.Add(contact.Attribute("type"));
                        using (XmlWriter w = XmlWriter.Create(new MemoryStream(), null))
                        {
                            streamElement.WriteTo(w);
                        }
                    }
                    catch (System.InvalidOperationException)
                    {
                        return;
                    }
                    throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "Add(IEnumerable of Nulls)")]
                public void AddIEnumerableOfNulls()
                {
                    XElement element = new XElement("root", GetNulls());
                    XStreamingElement streamElement = new XStreamingElement("root");
                    streamElement.Add(GetNulls());
                    getFreshStream();
                    streamElement.Save(_sourceStream); element.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                ///<summary>
                /// This function returns an IEnumeralb of nulls
                ///</summary>
                public IEnumerable<XNode> GetNulls()
                {
                    for (int i = 0; i < 1000; i++)
                        yield return null;
                }

                //[Variation(Priority = 0, Desc = "Add(IEnumerable of XNodes)")]
                public void AddIEnumerableOfXNodes()
                {
                    XElement x = InputSpace.GetElement(100, 3);
                    XElement element = new XElement("root", x.Nodes());
                    XStreamingElement streamElement = new XStreamingElement("root");
                    streamElement.Add(x.Nodes());
                    getFreshStream();
                    streamElement.Save(_sourceStream); element.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "Add(IEnumerable of Mixed Nodes)")]
                public void AddIEnumerableOfMixedNodes()
                {
                    XElement element = new XElement("root", GetMixedNodes());
                    XStreamingElement streamElement = new XStreamingElement("root");
                    streamElement.Add(GetMixedNodes());
                    getFreshStream();
                    streamElement.Save(_sourceStream); element.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
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

                //[Variation(Priority = 0, Desc = "Add(IEnumerable of XNodes + string)")]
                public void AddIEnumerableOfXNodesPlusString()
                {
                    InputSpace.Contacts(ref _xDoc, ref _xmlDoc);
                    XElement element = new XElement("contacts", _xDoc.Root.DescendantNodes(), "This String");
                    XStreamingElement streamElement = new XStreamingElement("contacts");
                    streamElement.Add(_xDoc.Root.DescendantNodes(), "This String");
                    getFreshStream();
                    streamElement.Save(_sourceStream); element.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Add(XAttribute + IEnumerable of XNodes)")]
                public void AddIEnumerableOfXNodesPlusAttribute()
                {
                    InputSpace.Contacts(ref _xDoc, ref _xmlDoc);
                    XAttribute xAttrib = new XAttribute("Attribute", "Value");
                    XElement element = new XElement("contacts", xAttrib, _xDoc.Root.DescendantNodes());
                    XStreamingElement streamElement = new XStreamingElement("contacts");
                    streamElement.Add(xAttrib, _xDoc.Root.DescendantNodes());
                    getFreshStream();
                    streamElement.Save(_sourceStream); element.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Save(null)")]
                public void SaveWithNull()
                {
                    try
                    {
                        XStreamingElement streamElement = new XStreamingElement("phone", "925-555-0134");
                        streamElement.Save((XmlWriter)null);
                    }
                    catch (System.ArgumentNullException)
                    {
                        return;
                    }
                    throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Save(TextWriter)")]
                public void SaveWithXmlTextWriter()
                {
                    XElement contact = new XElement("contacts", new XElement("contact", "jane"), new XElement("contact", "john"));
                    XStreamingElement streamElement = new XStreamingElement("contacts", contact.Elements());
                    getFreshStream();
                    TextWriter w = new StreamWriter(_sourceStream);
                    streamElement.Save(w);
                    w.Flush();
                    contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Save Twice")]
                public void SaveTwice()
                {
                    XElement contact = new XElement("contacts", new XElement("contact", "jane"), new XElement("contact", "john"));
                    XStreamingElement streamElement = new XStreamingElement("contacts", contact.Elements());
                    getFreshStream();
                    streamElement.Save(_sourceStream);
                    _sourceStream.Position = 0;
                    streamElement.Save(_sourceStream);
                    contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "WriteTo(null)")]
                public void WriteToWithNull()
                {
                    try
                    {
                        XStreamingElement streamElement = new XStreamingElement("phone", "925-555-0134");
                        streamElement.WriteTo((XmlWriter)null);
                    }
                    catch (System.ArgumentNullException)
                    {
                        return;
                    }
                    throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "Modify Original Elements")]
                public void ModifyOriginalElement()
                {
                    XElement contact = new XElement("contact", new XElement("name", "jane"),
                        new XElement("phone", new XAttribute("type", "home"), "925-555-0134"));
                    XStreamingElement streamElement = new XStreamingElement("contact", new object[] { contact.Elements() });
                    foreach (XElement x in contact.Elements())
                    {
                        x.Remove();
                    }
                    getFreshStream();
                    streamElement.Save(_sourceStream); contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Nested XStreamingElements")]
                public void NestedXStreamingElement()
                {
                    XElement name = new XElement("name", "jane");
                    XElement phone = new XElement("phone", new XAttribute("type", "home"), "925-555-0134");
                    XElement contact = new XElement("contact", name, new XElement("phones", phone));
                    XStreamingElement streamElement = new XStreamingElement("contact", name, new XStreamingElement("phones", phone));
                    getFreshStream();
                    streamElement.Save(_sourceStream); contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "Nested XStreamingElements + IEnumerable")]
                public void NestedXStreamingElementPlusIEnumerable()
                {
                    InputSpace.Contacts(ref _xDoc, ref _xmlDoc);
                    XElement element = new XElement("contacts", new XElement("Element", "Value"), _xDoc.Root.DescendantNodes());
                    XStreamingElement streamElement = new XStreamingElement("contacts");
                    streamElement.Add(new XStreamingElement("Element", "Value"), _xDoc.Root.DescendantNodes());
                    getFreshStream();
                    streamElement.Save(_sourceStream); element.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "Laziness of IEnumerables - Modify IEnumerable after adding")]
                public void IEnumerableLazinessTest1()
                {
                    XElement name = new XElement("name", "jane");
                    XElement phone = new XElement("phone", new XAttribute("type", "home"), "925-555-0134");
                    XElement contact = new XElement("contact", name, phone);
                    IEnumerable<XElement> elements = contact.Elements();
                    name.Remove(); phone.Remove();
                    XStreamingElement streamElement = new XStreamingElement("contact", new object[] { elements });
                    getFreshStream();
                    streamElement.Save(_sourceStream); contact.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 1, Desc = "Laziness of IEnumerables - Make Sure IEnumerable is walked after Save")]
                public void IEnumerableLazinessTest2()
                {
                    XElement name = new XElement("name", "jane");
                    XElement phone = new XElement("phone", new XAttribute("type", "home"), "925-555-0134");
                    XElement contact = new XElement("contact", name, phone);
                    // During debug this test will not work correctly since ToString() of
                    // streamElement gets called for displaying the value in debugger local window.
                    XStreamingElement streamElement = new XStreamingElement("contact", GetElements(contact));
                    getFreshStream();
                    contact.Save(_targetStream);
                    _invokeStatus = true;
                    streamElement.Save(_sourceStream);
                    TestLog.Compare(_invokeError == false, "IEnumerable walked before expected");
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                ///<summary>
                /// This function is used in above variation to make sure that the 
                /// IEnumerable is indeed walked lazily
                ///</summary>
                public IEnumerable<XElement> GetElements(XElement element)
                {
                    if (_invokeStatus == false)
                        _invokeError = true;
                    foreach (XElement x in element.Elements())
                        yield return x;
                }

                //[Variation(Priority = 0, Desc = "XStreamingElement in XElement")]
                public void XStreamingElementInXElement()
                {
                    XElement element = new XElement("contacts");
                    XStreamingElement streamElement = new XStreamingElement("contact", "SomeValue");
                    element.Add(streamElement);
                    XElement x = element.Element("contact");
                    getFreshStream();
                    streamElement.Save(_sourceStream); x.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }

                //[Variation(Priority = 0, Desc = "XStreamingElement in XDocument")]
                public void XStreamingElementInXDocument()
                {
                    _xDoc = new XDocument();
                    XStreamingElement streamElement = new XStreamingElement("contacts", "SomeValue");
                    _xDoc.Add(streamElement);
                    getFreshStream();
                    streamElement.Save(_sourceStream); _xDoc.Save(_targetStream);
                    resetStreamPos();
                    if (!_diff.Compare(_sourceStream, _targetStream))
                        throw new TestFailedException("");
                }
            }
        }
    }
}