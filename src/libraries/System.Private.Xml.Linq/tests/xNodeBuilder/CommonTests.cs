// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XmlDiff;
using Microsoft.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeBuilderFunctionalTests : TestModule
    {
        public partial class XNodeBuilderTests : XLinqTestCase
        {
            //[TestCase(Name = "Auto-completion of tokens", Param = "XNodeBuilder")]
            public partial class TCAutoComplete : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Missing EndAttr, followed by element", Priority = 1)]
                public void var_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr");
                    w.WriteStartElement("child");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root attr=''><child /></Root>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Missing EndAttr, followed by comment", Priority = 1)]
                public void var_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr");
                    w.WriteComment("This text is a comment");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root attr=''><!--This text is a comment--></Root>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Write EndDocument with unclosed element tag", Priority = 1)]
                public void var_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndDocument();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "WriteStartDocument - WriteEndDocument", Priority = 1)]
                public void var_4()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteEndDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteEndDocument();
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 5, Desc = "WriteEndElement without WriteStartElement", Priority = 1)]
                public void var_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            w.WriteEndElement();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteEndElement();
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 6, Desc = "WriteFullEndElement without WriteStartElement", Priority = 1)]
                public void var_6()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteFullEndElement();
                            w.WriteFullEndElement();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteFullEndElement();
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }
            }

            //[TestCase(Name = "WriteStart/EndDocument", Param = "XNodeBuilder")]
            public partial class TCDocument : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "StartDocument-EndDocument Sanity Test", Priority = 0)]
                public void document_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteEndDocument();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Multiple StartDocument should error", Priority = 1)]
                public void document_2()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteStartDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteStartDocument();
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 3, Desc = "Missing StartDocument should be fixed", Priority = 1)]
                public void document_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteEndDocument();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root />"))
                        throw new TestException(TestResult.Failed, "");
                }


                //[Variation(Id = 4, Desc = "Multiple EndDocument should error", Priority = 1)]
                public void document_4()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            w.WriteEndDocument();
                            w.WriteEndDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteEndDocument();
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 5, Desc = "Missing EndDocument should be fixed", Priority = 1)]
                public void document_5()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartDocument();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "Call Start-EndDocument multiple times, should error", Priority = 2)]
                public void document_6()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            w.WriteEndDocument();

                            w.WriteStartDocument();
                            w.WriteEndDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteEndDocument();
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 7, Desc = "Multiple root elements should error", Priority = 1)]
                public void document_7()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            w.WriteStartElement("Root");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteStartElement("Root");
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 8, Desc = "Start-EndDocument without any element should error", Priority = 2)]
                public void document_8()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteEndDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteEndDocument();
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 9, Desc = "Top level text should error - PROLOG", Priority = 1)]
                public void document_9()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteString("Top level text");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteString("Top level text");
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 10, Desc = "Top level text should error - EPILOG", Priority = 1)]
                public void document_10()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            w.WriteString("Top level text");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteString("Top level text");
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }


                //[Variation(Id = 11, Desc = "Top level atomic value should error - PROLOG", Priority = 1)]
                public void document_11()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            int i = 1;
                            w.WriteValue(i);
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteValue(1);
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 12, Desc = "Top level atomic value should error - EPILOG", Priority = 1)]
                public void document_12()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            int i = 1;
                            w.WriteValue(i);
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteValue(1);
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCDocType : BridgeHelpers
            {
                //[Variation(Id = 4, Desc = "WriteDocType with name value = String.Empty", Param = "String.Empty", Priority = 1)]
                //[Variation(Id = 5, Desc = "WriteDocType with name value = null", Param = "null", Priority = 1)]
                public void docType_4()
                {
                    XDocument doc = new XDocument();
                    string docName = "";
                    if (Variation.Param.ToString() == "String.Empty")
                        docName = string.Empty;
                    else if (Variation.Param.ToString() == "null")
                        docName = null;
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteDocType(docName, null, null, "test1");
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            try
                            {
                                w.WriteDocType(docName, null, null, "test1");
                            }
                            catch (ArgumentException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 7, Desc = "Call WriteDocType in the root element", Priority = 1)]
                public void docType_6()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteDocType("doc1", null, null, "test1");
                            w.WriteEndElement();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 8, Desc = "Call WriteDocType following root element", Priority = 1)]
                public void docType_7()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            w.WriteDocType("doc1", null, null, "test1");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }
            }

            //[TestCase(Name = "WriteStart/EndElement", Param = "XNodeBuilder")]
            public partial class TCElement : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "StartElement-EndElement Sanity Test", Priority = 0)]
                public void element_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Sanity test for overload WriteStartElement(string prefix, string name, string ns)", Priority = 0)]
                public void element_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("pre1", "Root", "http://my.com");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<pre1:Root xmlns:pre1=\"http://my.com\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Sanity test for overload WriteStartElement(string name, string ns)", Priority = 0)]
                public void element_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root", "http://my.com");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root xmlns=\"http://my.com\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "Element name = String.Empty should error", Priority = 1)]
                public void element_4()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement(string.Empty);
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 5, Desc = "Element name = null should error", Priority = 1)]
                public void element_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement(null);
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 6, Desc = "Element NS = String.Empty", Priority = 1)]
                public void element_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root", string.Empty);
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 7, Desc = "Element NS = null", Priority = 1)]
                public void element_7()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root", null);
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "Write 100 nested elements", Priority = 1)]
                public void element_8()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    for (int i = 0; i < 100; i++)
                    {
                        string eName = "Node" + i.ToString();
                        w.WriteStartElement(eName);
                    }
                    for (int i = 0; i < 100; i++)
                        w.WriteEndElement();
                    w.Dispose();
                    if (!CompareBaseline(doc, "100Elements.txt"))
                        throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCAttribute : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Sanity test for WriteAttribute", Priority = 0)]
                public void attribute_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr1");
                    w.WriteEndAttribute();
                    w.WriteAttributeString("attr2", "val2");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root attr1=\"\" attr2=\"val2\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Missing EndAttribute should be fixed", Priority = 0)]
                public void attribute_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr1");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root attr1=\"\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "WriteStartAttribute followed by WriteStartAttribute", Priority = 0)]
                public void attribute_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr1");
                    w.WriteStartAttribute("attr2");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root attr1=\"\" attr2=\"\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "Multiple WritetAttributeString", Priority = 0)]
                public void attribute_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("attr1", "val1");
                    w.WriteAttributeString("attr2", "val2");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root attr1=\"val1\" attr2=\"val2\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "WriteStartAttribute followed by WriteString", Priority = 0)]
                public void attribute_5()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr1");
                    w.WriteString("test");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root attr1=\"test\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "Sanity test for overload WriteStartAttribute(name, ns)", Priority = 1)]
                public void attribute_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr1", "http://my.com");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root p1:attr1=\"\" xmlns:p1=\"http://my.com\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 7, Desc = "Sanity test for overload WriteStartAttribute(prefix, name, ns)", Priority = 0)]
                public void attribute_7()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("pre1", "attr1", "http://my.com");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root pre1:attr1=\"\" xmlns:pre1=\"http://my.com\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "Duplicate attribute 'attr1'", Priority = 1)]
                public void attribute_8()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteStartAttribute("attr1");
                            w.WriteStartAttribute("attr1");
                        }
                        catch (XmlException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 9, Desc = "Duplicate attribute 'ns1:attr1'", Priority = 1)]
                public void attribute_9()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteStartAttribute("ns1", "attr1", "http://my.com");
                            w.WriteStartAttribute("ns1", "attr1", "http://my.com");
                        }
                        catch (XmlException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 10, Desc = "Attribute name = String.Empty should error", Priority = 1)]
                public void attribute_10()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteStartAttribute(string.Empty);
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 11, Desc = "Attribute name = null", Priority = 1)]
                public void attribute_11()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteStartAttribute(null);
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 12, Desc = "WriteAttribute with names Foo, fOo, foO, FOO", Priority = 1)]
                public void attribute_12()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    string[] attrNames = { "Foo", "fOo", "foO", "FOO" };
                    w.WriteStartElement("Root");
                    for (int i = 0; i < attrNames.Length; i++)
                    {
                        w.WriteAttributeString(attrNames[i], "x");
                    }
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root Foo=\"x\" fOo=\"x\" foO=\"x\" FOO=\"x\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 13, Desc = "Invalid value of xml:space", Priority = 1)]
                public void attribute_13()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "invalid");
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "Did not throw exception");
                }

                //[Variation(Id = 14, Desc = "SingleQuote in attribute value should be allowed")]
                public void attribute_14()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", null, "b'c");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root a=\"b'c\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 15, Desc = "DoubleQuote in attribute value should be escaped")]
                public void attribute_15()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", null, "b\"c");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root a=\"b&quot;c\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 16, Desc = "WriteAttribute with value = &, #65, #x20", Priority = 1)]
                public void attribute_16()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", "&");
                    w.WriteAttributeString("b", "&#65;");
                    w.WriteAttributeString("c", "&#x43;");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root a=\"&amp;\" b=\"&amp;#65;\" c=\"&amp;#x43;\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 17, Desc = "WriteAttributeString followed by WriteString", Priority = 1)]
                public void attribute_17()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", null, "b");
                    w.WriteString("test");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root a=\"b\">test</Root>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 18, Desc = "WriteAttribute followed by WriteString", Priority = 1)]
                public void attribute_18()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteString("test");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root a=\"test\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 19, Desc = "WriteAttribute with all whitespace characters", Priority = 1)]
                public void attribute_19()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", null, "\x20\x9\xD\xA");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root a=\" &#x9;&#xD;&#xA;\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 20, Desc = "< > & chars should be escaped in attribute value", Priority = 1)]
                public void attribute_20()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", null, "< > &");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root a=\"&lt; &gt; &amp;\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 21, Desc = "testcase: Redefine auto generated prefix n1")]
                public void attribute_21()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("test");
                    w.WriteAttributeString("xmlns", "n1", null, "http://testbasens");
                    w.WriteStartElement("base");
                    w.WriteAttributeString("id", "http://testbasens", "5");
                    w.WriteAttributeString("lang", "http://common", "en");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<test xmlns:n1=\"http://testbasens\"><base n1:id=\"5\" p4:lang=\"en\" xmlns:p4=\"http://common\" /></test>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 22, Desc = "Reuse and redefine existing prefix")]
                public void attribute_22()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("test");
                    w.WriteAttributeString("p", "a1", "ns1", "v");
                    w.WriteStartElement("base");
                    w.WriteAttributeString("a2", "ns1", "v");
                    w.WriteAttributeString("p", "a3", "ns2", "v");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<test p:a1=\"v\" xmlns:p=\"ns1\"><base p:a2=\"v\" p4:a3=\"v\" xmlns:p4=\"ns2\" /></test>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 23, Desc = "WriteStartAttribute(attr) sanity test")]
                public void attribute_23()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("test");
                    w.WriteStartAttribute("attr");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<test attr=\"\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 24, Desc = "WriteStartAttribute(attr) inside an element with changed default namespace")]
                public void attribute_24()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement(string.Empty, "test", "ns");
                    w.WriteStartAttribute("attr");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<test attr=\"\" xmlns=\"ns\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 25, Desc = "WriteStartAttribute(attr) and duplicate attrs")]
                public void attribute_25()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("test");
                            w.WriteStartAttribute(null, "attr", null);
                            w.WriteStartAttribute("attr");
                        }
                        catch (XmlException)
                        {
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw error for duplicate attrs");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 26, Desc = "WriteStartAttribute(attr) when element has ns:attr")]
                public void attribute_26()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("pre", "test", "ns");
                    w.WriteStartAttribute(null, "attr", "ns");
                    w.WriteStartAttribute("attr");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<pre:test pre:attr=\"\" attr=\"\" xmlns:pre=\"ns\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 27, Desc = "XmlCharCheckingWriter should not normalize newLines in attribute values when NewLinesHandling = Replace")]
                public void attribute_27()
                {
                    XmlWriterSettings s = new XmlWriterSettings();
                    s.NewLineHandling = NewLineHandling.Replace;
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");
                    w.WriteAttributeString("a", "|\x0D|\x0A|\x0D\x0A|");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<root a=\"|&#xD;|&#xA;|&#xD;&#xA;|\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 29, Desc = "442897: WriteAttributeString doesn't fail on invalid surrogate pair sequences")]
                public void attribute_29()
                {
                    XAttribute xa = new XAttribute("attribute", "\ud800\ud800");
                    XElement doc = new XElement("root");
                    doc.Add(xa);
                    XmlWriter w = doc.CreateWriter();
                    w.Dispose();
                    try
                    {
                        doc.Save(new MemoryStream());
                    }
                    catch (ArgumentException)
                    {
                        return;
                    }
                    finally
                    {
                        w.Dispose();
                    }
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCWriteAttributes : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Call WriteAttributes with default DTD attributes = true", Priority = 1)]
                public void writeAttributes_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    XmlReader xr = CreateReader(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "name")
                        {
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, true);
                    w.WriteEndElement();
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<Root a=\"b\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Call WriteAttributes with default DTD attributes = false", Priority = 1)]
                public void writeAttributes_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    XmlReader xr = CreateReader(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "name")
                        {
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<Root a=\"b\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Call WriteAttributes with XmlReader = null")]
                public void writeAttributes_3()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        XmlReader xr = null;
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributes(xr, false);
                        }
                        catch (ArgumentNullException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "Call WriteAttributes when reader is located on element", Priority = 1)]
                public void writeAttributes_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    XmlReader xr = CreateReader(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "AttributesGeneric")
                        {
                            do { xr.Read(); } while (xr.LocalName != "node");
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Element)
                    {
                        TestLog.WriteLine("Reader not positioned element");
                        TestLog.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        throw new TestException(TestResult.Failed, "");
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                    w.Dispose();
                    xr.Dispose();

                    if (!CompareReader(doc, "<Root a=\"b\" c=\"d\" e=\"f\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "Call WriteAttributes when reader is located in the mIddle attribute", Priority = 1)]
                public void writeAttributes_5()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    XmlReader xr = CreateReader(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "AttributesGeneric")
                        {
                            do { xr.Read(); } while (xr.LocalName != "node");
                            xr.MoveToAttribute(1);
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        TestLog.WriteLine("Reader not positioned on attribute");
                        TestLog.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        throw new TestException(TestResult.Failed, "");
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<Root c=\"d\" e=\"f\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "Call WriteAttributes when reader is located in the last attribute", Priority = 1)]
                public void writeAttributes_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    XmlReader xr = CreateReader(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "AttributesGeneric")
                        {
                            do { xr.Read(); } while (xr.LocalName != "node");
                            xr.MoveToNextAttribute();
                            xr.MoveToNextAttribute();
                            xr.MoveToNextAttribute();
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        TestLog.WriteLine("Reader not positioned on attribute");
                        TestLog.WriteLine(xr.LocalName);
                        xr.Dispose();
                        w.Dispose();
                        throw new TestException(TestResult.Failed, "");
                    }
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<Root e=\"f\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "Call WriteAttributes with reader on XmlDeclaration", Priority = 1)]
                public void writeAttributes_8()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    XmlReader xr = GetReader(Path.Combine(FilePathUtil.GetTestDataPath(), @"XmlWriter2\Simple.xml"), false);
                    xr.Read();
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<?xml version=\"1.0\" encoding=\"utf-8\"?><Root />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 9, Desc = "Call WriteAttributes with reader on DocType", Priority = 1, Param = "DocumentType")]
                //[Variation(Id = 10, Desc = "Call WriteAttributes with reader on CDATA", Priority = 1, Param = "CDATA")]
                //[Variation(Id = 11, Desc = "Call WriteAttributes with reader on Text", Priority = 1, Param = "Text")]
                //[Variation(Id = 12, Desc = "Call WriteAttributes with reader on PI", Priority = 1, Param = "ProcessingInstruction")]
                //[Variation(Id = 13, Desc = "Call WriteAttributes with reader on Comment", Priority = 1, Param = "Comment")]
                public void writeAttributes_9()
                {
                    XDocument doc = new XDocument();
                    string strxml = "";
                    switch (Variation.Param.ToString())
                    {
                        case "DocumentType":
                            strxml = "<!DOCTYPE Root[]><Root/>";
                            break;
                        case "CDATA":
                            strxml = "<root><![CDATA[Test]]></root>";
                            break;
                        case "Text":
                            strxml = "<root>Test</root>";
                            break;
                        case "ProcessingInstruction":
                            strxml = "<root><?pi test?></root>";
                            break;
                        case "Comment":
                            strxml = "<root><!-- comment --></root>";
                            break;
                        case "SignificantWhitespace":
                            strxml = "<root xml:space=\"preserve\">			 </root>";
                            break;
                        case "Whitespace":
                            strxml = "<root>			 </root>";
                            break;
                    }

                    XmlReader xr;
                    xr = CreateReader(new StringReader(strxml));

                    do { xr.Read(); }
                    while ((xr.NodeType.ToString() != Variation.Param.ToString()) && (xr.ReadState != ReadState.EndOfFile));

                    if (xr.ReadState == ReadState.EndOfFile || xr.NodeType.ToString() != Variation.Param.ToString())
                    {
                        xr.Dispose();
                        TestLog.WriteLine("Reader not positioned on correct node");
                        TestLog.WriteLine("ReadState: {0}", xr.ReadState);
                        TestLog.WriteLine("NodeType: {0}", xr.NodeType);
                        throw new TestException(TestResult.Failed, "");
                    }

                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            if (Variation.Param.ToString() != "DocType")
                                w.WriteStartElement("root");
                            w.WriteAttributes(xr, false);
                        }
                        catch (XmlException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                            return;
                        }
                        finally
                        {
                            xr.Dispose();
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 19, Desc = "Call WriteAttributes with 100 attributes", Priority = 1)]
                public void writeAttributes_12()
                {
                    XDocument doc = new XDocument();
                    XmlReader xr = CreateReader(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "OneHundredAttributes")
                        {
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }

                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        TestLog.WriteLine("Reader positioned on {0}", xr.NodeType.ToString());
                        xr.Dispose();
                        throw new TestException(TestResult.Failed, "");
                    }

                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("OneHundredAttributes");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                    xr.Dispose();
                    w.Dispose();
                    if (!CompareBaseline(doc, "OneHundredAttributes.xml"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 20, Desc = "WriteAttributes with different builtin entities in attribute value", Priority = 1)]
                public void writeAttributes_13()
                {
                    XDocument doc = new XDocument();
                    string strxml = "<E a=\"&gt;&lt;&quot;&apos;&amp;\" />";
                    XmlReader xr = CreateReader(new StringReader(strxml));
                    xr.Read();
                    xr.MoveToFirstAttribute();

                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        TestLog.WriteLine("Reader positioned on {0}", xr.NodeType.ToString());
                        xr.Dispose();
                        throw new TestException(TestResult.Failed, "");
                    }

                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributes(xr, false);
                    w.WriteEndElement();
                    xr.Dispose();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root  a=\"&gt;&lt;&quot;&apos;&amp;\" />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 21, Desc = "WriteAttributes tries to duplicate attribute", Priority = 1)]
                public void writeAttributes_14()
                {
                    XDocument doc = new XDocument();
                    string strxml = "<root attr='test' />";
                    XmlReader xr = CreateReader(new StringReader(strxml));
                    xr.Read();
                    xr.MoveToFirstAttribute();

                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        TestLog.WriteLine("Reader positioned on {0}", xr.NodeType.ToString());
                        xr.Dispose();
                        throw new TestException(TestResult.Failed, "");
                    }

                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteStartAttribute("attr");
                            w.WriteAttributes(xr, false);
                            w.WriteEndElement();
                        }
                        catch (Exception)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                        finally
                        {
                            xr.Dispose();
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCWriteNode_XmlReader : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "WriteNode with null reader", Priority = 1)]
                public void writeNode_XmlReader1()
                {
                    XDocument doc = new XDocument();
                    XmlReader xr = null;
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteNode(xr, false);
                        }
                        catch (ArgumentNullException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "WriteNode with reader positioned on attribute, no operation", Priority = 1)]
                public void writeNode_XmlReader2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "defattr")
                        {
                            xr.Read();
                            xr.MoveToFirstAttribute();
                            break;
                        }
                    }

                    if (xr.NodeType != XmlNodeType.Attribute)
                    {
                        TestLog.WriteLine("Reader positioned on {0}", xr.NodeType.ToString());
                        xr.Dispose();
                        w.Dispose();
                        throw new TestException(TestResult.Failed, "");
                    }
                    w.WriteStartElement("Root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root  />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "WriteNode before reader.Read()", Priority = 1)]
                public void writeNode_XmlReader3()
                {
                    XDocument doc = new XDocument();
                    XmlReader xr = CreateReader(new StringReader("<root />"));
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    w.Dispose();
                    xr.Dispose();

                    if (!CompareReader(doc, "<root />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "WriteNode after first reader.Read()", Priority = 1)]
                public void writeNode_XmlReader4()
                {
                    XDocument doc = new XDocument();
                    XmlReader xr = CreateReader(new StringReader("<root />"));
                    xr.Read();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<root />"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "WriteNode when reader is positioned on mIddle of an element node", Priority = 1)]
                public void writeNode_XmlReader5()
                {
                    XDocument doc = new XDocument();
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "Middle")
                        {
                            xr.Read();
                            break;
                        }
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    w.Dispose();

                    TestLog.Compare(xr.NodeType, XmlNodeType.Comment, "Error");
                    TestLog.Compare(xr.Value, "WriteComment", "Error");
                    xr.Dispose();
                    if (!CompareReader(doc, "<node2>Node Text<node3></node3><?name Instruction?></node2>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "WriteNode when reader state is EOF", Priority = 1)]
                public void writeNode_XmlReader6()
                {
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read()) { }
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();
                    xr.Dispose();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 7, Desc = "WriteNode when reader state is Closed", Priority = 1)]
                public void writeNode_XmlReader7()
                {
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read()) { }
                    xr.Dispose();
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(doc, "<Root />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "WriteNode with reader on empty element node", Priority = 1)]
                public void writeNode_XmlReader8()
                {
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "EmptyElement")
                        {
                            xr.Read();
                            break;
                        }
                    }
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    w.Dispose();

                    TestLog.Compare(xr.NodeType, XmlNodeType.EndElement, "Error");
                    TestLog.Compare(xr.Name, "EmptyElement", "Error");
                    xr.Dispose();
                    if (!CompareReader(doc, "<node1 />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 9, Desc = "WriteNode with reader on 100 Nodes", Priority = 1)]
                public void writeNode_XmlReader9()
                {
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "OneHundredElements")
                        {
                            xr.Read();
                            break;
                        }
                    }
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareBaseline(doc, "100Nodes.txt")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 10, Desc = "WriteNode with reader on node with mixed content", Priority = 1)]
                public void writeNode_XmlReader10()
                {
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "MixedContent")
                        {
                            xr.Read();
                            break;
                        }
                    }
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    w.Dispose();

                    TestLog.Compare(xr.NodeType, XmlNodeType.EndElement, "Error");
                    TestLog.Compare(xr.Name, "MixedContent", "Error");
                    xr.Dispose();
                    if (!CompareReader(doc, "<node1><?PI Instruction?><!--Comment-->Text<![CDATA[cdata]]></node1>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 11, Desc = "WriteNode with reader on node with declared namespace in parent", Priority = 1)]
                public void writeNode_XmlReader11()
                {
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "NamespaceNoPrefix")
                        {
                            xr.Read();
                            xr.Read();
                            break;
                        }
                    }
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<node1 xmlns=\"foo\" ></node1>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 14, Desc = "WriteNode with element that has different prefix", Priority = 1)]
                public void writeNode_XmlReader14()
                {
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "DiffPrefix")
                        {
                            xr.Read();
                            break;
                        }
                    }
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("x", "bar", "foo");
                    w.WriteNode(xr, true);
                    w.WriteStartElement("blah", "foo");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<x:bar xmlns:x=\"foo\"><z:node xmlns:z=\"foo\" /><x:blah /></x:bar>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 15, Desc = "Call WriteNode with default attributes = true and DTD", Priority = 1)]
                public void writeNode_XmlReader15()
                {
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "DefaultAttributesTrue")
                        {
                            xr.Read();
                            break;
                        }
                    }
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteNode(xr, true);
                    w.WriteEndElement();
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<Root><name a=\"b\" /></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 16, Desc = "Call WriteNode with default attributes = false and DTD", Priority = 1)]
                public void writeNode_XmlReader16()
                {
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "DefaultAttributesTrue")
                        {
                            xr.Read();
                            break;
                        }
                    }
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<Root><name a='b' /></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 17, Desc = "testcase: WriteNode with reader on empty element with attributes", Priority = 1)]
                public void writeNode_XmlReader17()
                {
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "EmptyElementWithAttributes")
                        {
                            xr.Read();
                            break;
                        }
                    }
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    w.Dispose();
                    if (!CompareReader(doc, "<node1 a='foo' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 18, Desc = "testcase: WriteNode with document containing just empty element with attributes", Priority = 1)]
                public void writeNode_XmlReader18()
                {
                    string xml = "<Root a=\"foo\"/>";
                    XmlReader xr = CreateReader(new StringReader(xml));
                    xr.Read();
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<Root a=\"foo\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 19, Desc = "testcase: Call WriteNode with special entity references as attribute value", Priority = 1)]
                public void writeNode_XmlReader19()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    string xml = "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>";
                    XmlReader xr = CreateReader(new StringReader(xml));
                    while (xr.Read())
                        w.WriteNode(xr, true);
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 21, Desc = "Call WriteNode with full end element", Priority = 1)]
                public void writeNode_XmlReader21()
                {
                    XDocument doc = new XDocument();
                    string strxml = "<root></root>";
                    XmlReader xr = CreateReader(new StringReader(strxml));
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<root></root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 22, Desc = "Call WriteNode with reader on element with 100 attributes", Priority = 1)]
                public void writeNode_XmlReader22()
                {
                    XDocument doc = new XDocument();
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "OneHundredAttributes")
                        {
                            break;
                        }
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareBaseline(doc, "OneHundredAttributes.xml")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 23, Desc = "Call WriteNode with reader on text node", Priority = 1)]
                public void writeNode_XmlReader23()
                {
                    XDocument doc = new XDocument();
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "Middle")
                        {
                            xr.Read();
                            xr.Read();
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Text)
                    {
                        TestLog.WriteLine("Reader positioned on {0}", xr.NodeType);
                        xr.Dispose();
                        throw new TestException(TestResult.Failed, "");
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<root>Node Text</root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 24, Desc = "Call WriteNode with reader on CDATA node", Priority = 1)]
                public void writeNode_XmlReader24()
                {
                    XDocument doc = new XDocument();
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "CDataNode")
                        {
                            xr.Read();
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.CDATA)
                    {
                        TestLog.WriteLine("Reader positioned on {0}", xr.NodeType);
                        xr.Dispose();
                        throw new TestException(TestResult.Failed, "");
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();

                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<root><![CDATA[cdata content]]></root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 25, Desc = "Call WriteNode with reader on PI node", Priority = 1)]
                public void writeNode_XmlReader25()
                {
                    XDocument doc = new XDocument();
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "PINode")
                        {
                            xr.Read();
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.ProcessingInstruction)
                    {
                        TestLog.WriteLine("Reader positioned on {0}", xr.NodeType);
                        xr.Dispose();
                        throw new TestException(TestResult.Failed, "");
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<root><?PI Text?></root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 26, Desc = "Call WriteNode with reader on Comment node", Priority = 1)]
                public void writeNode_XmlReader26()
                {
                    XDocument doc = new XDocument();
                    XmlReader xr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "CommentNode")
                        {
                            xr.Read();
                            break;
                        }
                    }
                    if (xr.NodeType != XmlNodeType.Comment)
                    {
                        TestLog.WriteLine("Reader positioned on {0}", xr.NodeType);
                        xr.Dispose();
                        throw new TestException(TestResult.Failed, "");
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");
                    w.WriteNode(xr, false);
                    w.WriteEndElement();
                    w.Dispose();
                    xr.Dispose();
                    if (!CompareReader(doc, "<root><!--Comment--></root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 27, Desc = "WriteNode should only write required namespaces", Priority = 1)]
                public void writeNode_XmlReader27()
                {
                    XDocument doc = new XDocument();
                    string strxml = @"<root xmlns:p1='p1'><p2:child xmlns:p2='p2' /></root>";
                    XmlReader xr = CreateReader(new StringReader(strxml));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "child")
                            break;
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    xr.Dispose();
                    w.Dispose();
                    if (!CompareReader(doc, "<p2:child xmlns:p2='p2' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 28, Desc = "WriteNode should only write required namespaces, include xmlns:xml", Priority = 1)]
                public void writeNode_XmlReader28()
                {
                    XDocument doc = new XDocument();
                    string strxml = @"<root xmlns:p1='p1'><p2:child xmlns:p2='p2' xmlns:xml='http://www.w3.org/XML/1998/namespace' /></root>";
                    XmlReader xr = CreateReader(new StringReader(strxml));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "child")
                            break;
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    xr.Dispose();
                    w.Dispose();

                    if (!CompareReader(doc, "<p2:child xmlns:p2='p2'  xmlns:xml='http://www.w3.org/XML/1998/namespace' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 29, Desc = "WriteNode should only write required namespaces, exclude xmlns:xml", Priority = 1)]
                public void writeNode_XmlReader29()
                {
                    XDocument doc = new XDocument();
                    string strxml = @"<root xmlns:p1='p1' xmlns:xml='http://www.w3.org/XML/1998/namespace'><p2:child xmlns:p2='p2' /></root>";
                    XmlReader xr = CreateReader(new StringReader(strxml));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "child")
                            break;
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    xr.Dispose();
                    w.Dispose();

                    if (!CompareReader(doc, "<p2:child xmlns:p2=\"p2\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 30, Desc = "WriteNode should only write required namespaces, change default ns at top level", Priority = 1)]
                public void writeNode_XmlReader30()
                {
                    XDocument doc = new XDocument();
                    string strxml = @"<root xmlns='p1'><child /></root>";
                    XmlReader xr = CreateReader(new StringReader(strxml));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "child")
                            break;
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    xr.Dispose();
                    w.Dispose();

                    if (!CompareReader(doc, "<child xmlns='p1' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 31, Desc = "WriteNode should only write required namespaces, change default ns at same level", Priority = 1)]
                public void writeNode_XmlReader31()
                {
                    XDocument doc = new XDocument();
                    string strxml = @"<root xmlns:p1='p1'><child xmlns='p2'/></root>";
                    XmlReader xr = CreateReader(new StringReader(strxml));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "child")
                            break;
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    xr.Dispose();
                    w.Dispose();

                    if (!CompareReader(doc, "<child xmlns='p2' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 32, Desc = "WriteNode should only write required namespaces, change default ns at both levels", Priority = 1)]
                public void writeNode_XmlReader32()
                {
                    XDocument doc = new XDocument();
                    string strxml = @"<root xmlns='p1'><child xmlns='p2'/></root>";
                    XmlReader xr = CreateReader(new StringReader(strxml));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "child")
                            break;
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    xr.Dispose();
                    w.Dispose();

                    if (!CompareReader(doc, "<child xmlns='p2' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 33, Desc = "WriteNode should only write required namespaces, change ns uri for same prefix", Priority = 1)]
                public void writeNode_XmlReader33()
                {
                    XDocument doc = new XDocument();
                    string strxml = @"<p1:root xmlns:p1='p1'><p1:child xmlns:p1='p2'/></p1:root>";
                    XmlReader xr = CreateReader(new StringReader(strxml));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "child")
                            break;
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    xr.Dispose();
                    w.Dispose();

                    if (!CompareReader(doc, "<p1:child xmlns:p1='p2' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 34, Desc = "WriteNode should only write required namespaces, reuse prefix from top level", Priority = 1)]
                public void writeNode_XmlReader34()
                {
                    XDocument doc = new XDocument();
                    string strxml = @"<root xmlns:p1='p1'><p1:child /></root>";
                    XmlReader xr = CreateReader(new StringReader(strxml));
                    while (xr.Read())
                    {
                        if (xr.LocalName == "child")
                            break;
                    }
                    XmlWriter w = CreateWriter(doc);
                    w.WriteNode(xr, false);
                    xr.Dispose();
                    w.Dispose();

                    if (!CompareReader(doc, "<p1:child xmlns:p1='p1' />")) throw new TestException(TestResult.Failed, "");
                }
            }

            //[TestCase(Name = "WriteFullEndElement", Param = "XNodeBuilder")]
            public partial class TCFullEndElement : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Sanity test for WriteFullEndElement()", Priority = 0)]
                public void fullEndElement_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteFullEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Call WriteFullEndElement before calling WriteStartElement", Priority = 2)]
                public void fullEndElement_2()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteFullEndElement();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Call WriteFullEndElement after WriteEndElement", Priority = 2)]
                public void fullEndElement_3()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            w.WriteFullEndElement();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "Call WriteFullEndElement without closing attributes", Priority = 1)]
                public void fullEndElement_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteString("b");
                    w.WriteFullEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root a=\"b\"></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "Call WriteFullEndElement after WriteStartAttribute", Priority = 1)]
                public void fullEndElement_5()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteFullEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root a=\"\"></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "WriteFullEndElement for 100 nested elements", Priority = 1)]
                public void fullEndElement_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    for (int i = 0; i < 100; i++)
                    {
                        string eName = "Node" + i.ToString();
                        w.WriteStartElement(eName);
                    }
                    for (int i = 0; i < 100; i++)
                        w.WriteFullEndElement();
                    w.Dispose();

                    if (!CompareBaseline(doc, "100FullEndElements.txt")) throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCElemNamespace : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Multiple NS decl for same prefix on an element", Priority = 1)]
                public void elemNamespace_1()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xmlns", "x", null, "foo");
                            w.WriteAttributeString("xmlns", "x", null, "bar");
                        }
                        catch (XmlException)
                        {
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Multiple NS decl for same prefix (same NS value) on an element", Priority = 1)]
                public void elemNamespace_2()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xmlns", "x", null, "foo");
                            w.WriteAttributeString("xmlns", "x", null, "foo");
                            w.WriteEndElement();
                        }
                        catch (XmlException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Element and attribute have same prefix, but different namespace value", Priority = 2)]
                public void elemNamespace_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("x", "a", "bar", "b");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<x:Root p1:a=\"b\" xmlns:p1=\"bar\" xmlns:x=\"foo\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "Nested elements have same prefix, but different namespace", Priority = 1)]
                public void elemNamespace_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteStartElement("x", "level1", "bar");
                    w.WriteStartElement("x", "level2", "blah");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<x:Root xmlns:x=\"foo\"><x:level1 xmlns:x=\"bar\"><x:level2 xmlns:x=\"blah\" /></x:level1></x:Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "Mapping reserved prefix xml to invalid namespace", Priority = 1)]
                public void elemNamespace_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("xml", "Root", "blah");
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "Mapping reserved prefix xml to correct namespace", Priority = 1)]
                public void elemNamespace_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("xml", "Root", "http://www.w3.org/XML/1998/namespace");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<xml:Root />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 7, Desc = "Write element with prefix beginning with xml", Priority = 1)]
                public void elemNamespace_7()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartElement("xmlA", "elem1", "test");
                    w.WriteEndElement();
                    w.WriteStartElement("xMlB", "elem2", "test");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><xmlA:elem1 xmlns:xmlA=\"test\" /><xMlB:elem2 xmlns:xMlB=\"test\" /></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "Reuse prefix that refers the same as default namespace", Priority = 2)]
                public void elemNamespace_8()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("x", "foo", "uri-1");
                    w.WriteStartElement("", "bar", "uri-1");
                    w.WriteStartElement("x", "bop", "uri-1");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();

                    throw new TestSkippedException("");
                }

                //[Variation(Id = 9, Desc = "Should throw error for prefix=xmlns", Priority = 2)]
                public void elemNamespace_9()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("xmlns", "localname", "uri:bogus");
                            w.WriteEndElement();
                        }
                        catch (Exception)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw error");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 10, Desc = "Create nested element without prefix but with namespace of parent element with a defined prefix", Priority = 2)]
                public void elemNamespace_10()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteStartElement("level1", "fo");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root xmlns:x=\"fo\"><x:level1 /></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 11, Desc = "Create different prefix for element and attribute that have same namespace", Priority = 2)]
                public void elemNamespace_11()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("y", "attr", "foo", "b");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<y:Root y:attr=\"b\" xmlns:y=\"foo\" xmlns:x=\"foo\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 12, Desc = "Create same prefix for element and attribute that have same namespace", Priority = 2)]
                public void elemNamespace_12()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("x", "attr", "foo", "b");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<x:Root x:attr=\"b\" xmlns:x=\"foo\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 13, Desc = "Try to re-define NS prefix on attribute which is already defined on an element", Priority = 2)]
                public void elemNamespace_13()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("x", "attr", "bar", "test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<x:Root p1:attr=\"test\" xmlns:p1=\"bar\" xmlns:x=\"foo\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 14, Desc = "testcase: Namespace string contains surrogates, reuse at different levels", Priority = 1)]
                public void elemNamespace_14()
                {
                    string uri = "urn:\uD800\uDC00";
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");
                    w.WriteAttributeString("xmlns", "pre", null, uri);
                    w.WriteElementString("elt", uri, "text");
                    w.WriteEndElement();

                    w.Dispose();

                    string strExpected = string.Format("<root xmlns:pre=\"{0}\"><pre:elt>text</pre:elt></root>", uri);
                    if (!CompareReader(doc, strExpected)) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 15, Desc = "Namespace containing entities, use at multiple levels", Priority = 1)]
                public void elemNamespace_15()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    string strxml = "<?xml version=\"1.0\" ?><root xmlns:foo=\"urn:&lt;&gt;\"><foo:elt1 /><foo:elt2 /><foo:elt3 /></root>";

                    XmlReader xr = XmlReader.Create(new StringReader(strxml));
                    w.WriteNode(xr, false);
                    xr.Dispose();
                    w.Dispose();

                    if (!CompareReader(doc, "<root xmlns:foo=\"urn:&lt;&gt;\"><foo:elt1 /><foo:elt2 /><foo:elt3 /></root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 16, Desc = "testcase: Verify it resets default namespace when redefined earlier in the stack", Priority = 1)]
                public void elemNamespace_16()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("", "x", "foo");
                    w.WriteAttributeString("xmlns", "foo");
                    w.WriteStartElement("", "y", "");
                    w.WriteStartElement("", "z", "foo");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<x xmlns=\"foo\"><y xmlns=\"\"><z xmlns=\"foo\" /></y></x>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 17, Desc = "The default namespace for an element can not be changed once it is written out", Priority = 1)]
                public void elemNamespace_17()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xmlns", null, "test");
                            w.WriteEndElement();
                        }
                        catch (XmlException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 18, Desc = "Map XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix", Priority = 1)]
                public void elemNamespace_18()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("foo", "bar", "http://www.w3.org/XML/1998/namaespace");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<foo:bar xmlns:foo=\"http://www.w3.org/XML/1998/namaespace\"  />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 19, Desc = "testcase: Pass NULL as NS to WriteStartElement", Priority = 1)]
                public void elemNamespace_19()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("foo", "Root", "NS");
                    w.WriteStartElement("bar", null);
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<foo:Root xmlns:foo=\"NS\"><bar /></foo:Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 20, Desc = "Write element in reserved XML namespace, should error", Priority = 1)]
                public void elemNamespace_20()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    try
                    {
                        w.WriteStartElement("foo", "Root", "http://www.w3.org/XML/1998/namespace");
                    }
                    catch (ArgumentException)
                    {
                        TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                    finally
                    {
                        w.Dispose();
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 21, Desc = "Write element in reserved XMLNS namespace, should error", Priority = 1)]
                public void elemNamespace_21()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    try
                    {
                        w.WriteStartElement("foo", "Root", "http://www.w3.org/XML/1998/namespace");
                    }
                    catch (ArgumentException)
                    {
                        TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                    finally
                    {
                        w.Dispose();
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 22, Desc = "Mapping a prefix to empty ns should error", Priority = 1)]
                public void elemNamespace_22()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("pre", "test", string.Empty);
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 23, Desc = "Pass null prefix to WriteStartElement()", Priority = 1)]
                public void elemNamespace_23()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement(null, "Root", "ns");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root xmlns='ns' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 24, Desc = "Pass String.Empty prefix to WriteStartElement()", Priority = 1)]
                public void elemNamespace_24()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement(string.Empty, "Root", "ns");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root xmlns='ns' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 25, Desc = "Pass null ns to WriteStartElement()", Priority = 1)]
                public void elemNamespace_25()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root", null);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root/>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 26, Desc = "Pass String.Empty ns to WriteStartElement()", Priority = 1)]
                public void elemNamespace_26()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root", string.Empty);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root/>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 27, Desc = "Pass null prefix to WriteStartElement() when namespace is in scope", Priority = 1)]
                public void elemNamespace_27()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteElementString(null, "child", "ns", "test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<pre:Root xmlns:pre='ns'><pre:child>test</pre:child></pre:Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 28, Desc = "Pass String.Empty prefix to WriteStartElement() when namespace is in scope", Priority = 1)]
                public void elemNamespace_28()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteElementString(string.Empty, "child", "ns", "test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<pre:Root xmlns:pre=\"ns\"><pre:child xmlns=\"ns\">test</pre:child></pre:Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 29, Desc = "Pass null ns to WriteStartElement() when prefix is in scope", Priority = 1)]
                public void elemNamespace_29()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteElementString("pre", "child", null, "test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<pre:Root xmlns:pre='ns'><pre:child>test</pre:child></pre:Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 30, Desc = "Pass String.Empty ns to WriteStartElement() when prefix is in scope", Priority = 1)]
                public void elemNamespace_30()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("pre", "Root", "ns");
                            w.WriteElementString("pre", "child", string.Empty, "test");
                        }
                        catch (ArgumentException)
                        {
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 31, Desc = "Pass String.Empty ns to WriteStartElement() when prefix is in scope", Priority = 1)]
                public void elemNamespace_31()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("pre", "Root", "ns");
                            w.WriteElementString("pre", "child", string.Empty, "test");
                        }
                        catch (ArgumentException)
                        {
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 31, Desc = "Mapping empty ns uri to a prefix should error", Priority = 1)]
                public void elemNamespace_32()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("prefix", "localname", null);
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }
            }

            //[TestCase(Name = "Attribute Namespace", Param = "XNodeBuilder")]
            public partial class TCAttrNamespace : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Define prefix 'xml' with invalid namespace URI 'foo'", Priority = 1)]
                public void attrNamespace_1()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xmlns", "xml", null, "foo");
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Bind NS prefix 'xml' with valid namespace URI", Priority = 1)]
                public void attrNamespace_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "xml", null, "http://www.w3.org/XML/1998/namespace");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root xmlns:xml=\"http://www.w3.org/XML/1998/namespace\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Bind NS prefix 'xmlA' with namespace URI 'foo'", Priority = 1)]
                public void attrNamespace_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "xmlA", null, "foo");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root xmlns:xmlA=\"foo\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "Write attribute xml:space with correct namespace", Priority = 1)]
                public void attrNamespace_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "default");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root xml:space=\"default\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "Write attribute xml:space with incorrect namespace", Priority = 1)]
                public void attrNamespace_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xml", "space", "foo", "default");
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "Write attribute xml:lang with incorrect namespace", Priority = 1)]
                public void attrNamespace_6()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xml", "lang", "foo", "EN");
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }


                //[Variation(Id = 7, Desc = "WriteAttribute, define namespace attribute before value attribute", Priority = 1)]
                public void attrNamespace_7()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteAttributeString("a", "fo", "b");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root xmlns:x=\"fo\" x:a=\"b\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "WriteAttribute, define namespace attribute after value attribute", Priority = 1)]
                public void attrNamespace_8()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", "fo", "b");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root x:a=\"b\" xmlns:x=\"fo\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 9, Desc = "WriteAttribute, redefine prefix at different scope and use both of them", Priority = 1)]
                public void attrNamespace_9()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("level1");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteAttributeString("a", "fo", "b");
                    w.WriteStartElement("level2");
                    w.WriteAttributeString("xmlns", "x", null, "bar");
                    w.WriteAttributeString("c", "bar", "d");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<level1 xmlns:x=\"fo\" x:a=\"b\"><level2 xmlns:x=\"bar\" x:c=\"d\" /></level1>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 10, Desc = "WriteAttribute, redefine namespace at different scope and use both of them", Priority = 1)]
                public void attrNamespace_10()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("level1");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteAttributeString("a", "fo", "b");
                    w.WriteStartElement("level2");
                    w.WriteAttributeString("xmlns", "y", null, "fo");
                    w.WriteAttributeString("c", "fo", "d");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<level1 xmlns:x=\"fo\" x:a=\"b\"><level2 xmlns:y=\"fo\" y:c=\"d\" /></level1>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 11, Desc = "WriteAttribute with collIding prefix with element", Priority = 1)]
                public void attrNamespace_11()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("x", "Root", "fo");
                    w.WriteAttributeString("x", "a", "bar", "b");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<x:Root p1:a=\"b\" xmlns:p1=\"bar\" xmlns:x=\"fo\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 12, Desc = "WriteAttribute with collIding namespace with element", Priority = 1)]
                public void attrNamespace_12()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("x", "Root", "fo");
                    w.WriteAttributeString("y", "a", "fo", "b");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<y:Root y:a=\"b\" xmlns:y=\"fo\" xmlns:x=\"fo\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 13, Desc = "WriteAttribute with namespace but no prefix", Priority = 1)]
                public void attrNamespace_13()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", "fo", "b");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root p1:a=\"b\" xmlns:p1=\"fo\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 14, Desc = "WriteAttribute for 2 attributes with same prefix but different namespace", Priority = 1)]
                public void attrNamespace_14()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", "fo", "b");
                    w.WriteAttributeString("x", "c", "bar", "d");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root x:a=\"b\" p2:c=\"d\" xmlns:p2=\"bar\" xmlns:x=\"fo\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 15, Desc = "WriteAttribute with String.Empty and null as namespace and prefix values", Priority = 1)]
                public void attrNamespace_15()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString(null, "a", null, "b");
                    w.WriteAttributeString(string.Empty, "c", string.Empty, "d");
                    w.WriteAttributeString(null, "e", string.Empty, "f");
                    w.WriteAttributeString(string.Empty, "g", null, "h");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root a=\"b\" c=\"d\" e=\"f\" g=\"h\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 16, Desc = "WriteAttribute to manually create attribute of xmlns:x", Priority = 1)]
                public void attrNamespace_16()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "x", null, "test");
                    w.WriteStartElement("x", "level1", null);
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root xmlns:x=\"test\"><x:level1 /></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 17, Desc = "testcase: WriteAttribute with namespace value = null while a prefix exists", Priority = 1)]
                public void attrNamespace_17()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", null, "b");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root a=\"b\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 18, Desc = "testcase: WriteAttribute with namespace value = String.Empty while a prefix exists", Priority = 1)]
                public void attrNamespace_18()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", string.Empty, "b");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root a=\"b\" />")) throw new TestException(TestResult.Failed, "");
                }


                //[Variation(Id = 19, Desc = "WriteAttribe in nested elements with same namespace but different prefix", Priority = 1)]
                public void attrNamespace_19()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", "x", "fo", "y");
                    w.WriteAttributeString("xmlns", "a", null, "fo");
                    w.WriteStartElement("level1");
                    w.WriteAttributeString("b", "x", "fo", "y");
                    w.WriteAttributeString("xmlns", "b", null, "fo");
                    w.WriteStartElement("level2");
                    w.WriteAttributeString("c", "x", "fo", "y");
                    w.WriteAttributeString("xmlns", "c", null, "fo");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.WriteEndElement();

                    w.Dispose();

                    if (!CompareReader(doc, "<Root a:x=\"y\" xmlns:a=\"fo\"><level1 b:x=\"y\" xmlns:b=\"fo\"><level2 c:x=\"y\" xmlns:c=\"fo\" /></level1></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 20, Desc = "WriteAttribute for x:a and xmlns:a diff namespace", Priority = 1)]
                public void attrNamespace_20()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", "bar", "b");
                    w.WriteAttributeString("xmlns", "a", null, "foo");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root x:a=\"b\" xmlns:a=\"foo\" xmlns:x=\"bar\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 21, Desc = "WriteAttribute for x:a and xmlns:a same namespace", Priority = 1)]
                public void attrNamespace_21()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", "foo", "b");
                    w.WriteAttributeString("xmlns", "a", null, "foo");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root a:a=\"b\" xmlns:a=\"foo\" xmlns:x=\"foo\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 22, Desc = "WriteAttribute with collIding NS and prefix for 2 attributes", Priority = 1)]
                public void attrNamespace_22()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "x", null, "foo");
                    w.WriteAttributeString("x", "a", "foo", "b");
                    w.WriteAttributeString("x", "c", "foo", "b");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root xmlns:x=\"foo\" x:a=\"b\" x:c=\"b\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 23, Desc = "WriteAttribute with DQ in namespace", Priority = 2)]
                public void attrNamespace_23()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", "\"", "b");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root p1:a=\"b\" xmlns:p1=\"&quot;\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 24, Desc = "testcase: Attach prefix with empty namespace", Priority = 1)]
                public void attrNamespace_24()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xmlns", "foo", "bar", "");
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 25, Desc = "Explicitly write namespace attribute that maps XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix", Priority = 1)]
                public void attrNamespace_25()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "foo", "", "http://www.w3.org/XML/1998/namaespace");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root xmlns:foo=\"http://www.w3.org/XML/1998/namaespace\"  />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 26, Desc = "Map XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix", Priority = 1)]
                public void attrNamespace_26()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("foo", "bar", "http://www.w3.org/XML/1998/namaespace", "test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root foo:bar=\"test\" xmlns:foo=\"http://www.w3.org/XML/1998/namaespace\"  />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 27, Desc = "Pass empty namespace to WriteAttributeString(prefix, name, ns, value)", Priority = 1)]
                public void attrNamespace_27()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("pre", "Root", "urn:pre");
                    w.WriteAttributeString("pre", "attr", "", "test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<pre:Root xmlns:pre=\"urn:pre\" attr=\"test\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 28, Desc = "Write attribute with prefix = xmlns", Priority = 1)]
                public void attrNamespace_28()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xmlns", "xmlns", null, "test");
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 29, Desc = "Write attribute in reserved XML namespace, should error", Priority = 1)]
                public void attrNamespace_29()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    try
                    {
                        w.WriteStartElement("foo");
                        w.WriteAttributeString("aaa", "bbb", "http://www.w3.org/XML/1998/namespace", "ccc");
                    }
                    catch (ArgumentException)
                    {
                        TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                    finally
                    {
                        w.Dispose();
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 30, Desc = "Write attribute in reserved XMLNS namespace, should error", Priority = 1)]
                public void attrNamespace_30()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    try
                    {
                        w.WriteStartElement("foo");
                        w.WriteStartAttribute("aaa", "bbb", "http://www.w3.org/XML/1998/namespace");
                    }
                    catch (ArgumentException)
                    {
                        TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                    finally
                    {
                        w.Dispose();
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 31, Desc = "WriteAttributeString with no namespace under element with empty prefix", Priority = 1)]
                public void attrNamespace_31()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    w.WriteStartElement("d", "Data", "http://example.org/data");
                    w.WriteStartElement("g", "GoodStuff", "http://example.org/data/good");
                    w.WriteAttributeString("hello", "world");
                    w.WriteEndElement();
                    w.WriteStartElement("BadStuff", "http://example.org/data/bad");
                    w.WriteAttributeString("hello", "world");
                    w.WriteEndElement();
                    w.WriteEndElement();

                    w.Dispose();

                    if (!CompareReader(doc, "<d:Data xmlns:d='http://example.org/data'>" +
                                        "<g:GoodStuff hello='world' xmlns:g='http://example.org/data/good' />" +
                                        "<BadStuff hello='world' xmlns='http://example.org/data/bad' />" +
                                        "</d:Data>"))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 32, Desc = "Pass null prefix to WriteAttributeString()", Priority = 1)]
                public void attrNamespace_32()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString(null, "attr", "ns", "value");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root p1:attr=\"value\" xmlns:p1=\"ns\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 33, Desc = "Pass String.Empty prefix to WriteAttributeString()", Priority = 1)]
                public void attrNamespace_33()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString(string.Empty, "attr", "ns", "value");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root p1:attr=\"value\" xmlns:p1=\"ns\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 34, Desc = "Pass null ns to WriteAttributeString()", Priority = 1)]
                public void attrNamespace_34()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("pre", "attr", null, "value");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root attr='value' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 35, Desc = "Pass String.Empty ns to WriteAttributeString()", Priority = 1)]
                public void attrNamespace_35()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("pre", "attr", string.Empty, "value");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root attr='value' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 36, Desc = "Pass null prefix to WriteAttributeString() when namespace is in scope", Priority = 1)]
                public void attrNamespace_36()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteAttributeString(null, "child", "ns", "test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<pre:Root pre:child='test' xmlns:pre='ns' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 37, Desc = "Pass String.Empty prefix to WriteAttributeString() when namespace is in scope", Priority = 1)]
                public void attrNamespace_37()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteAttributeString(string.Empty, "child", "ns", "test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<pre:Root pre:child='test' xmlns:pre='ns' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 38, Desc = "Pass null ns to WriteAttributeString() when prefix is in scope", Priority = 1)]
                public void attrNamespace_38()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteAttributeString("pre", "child", null, "test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<pre:Root pre:child='test' xmlns:pre='ns' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 39, Desc = "Pass String.Empty ns to WriteAttributeString() when prefix is in scope", Priority = 1)]
                public void attrNamespace_39()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteAttributeString("pre", "child", string.Empty, "test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<pre:Root child='test' xmlns:pre='ns' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 40, Desc = "Mapping empty ns uri to a prefix should error", Priority = 1)]
                public void attrNamespace_40()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            w.WriteAttributeString("xmlns", null, null, "test");
                            w.WriteEndElement();
                        }
                        catch (XmlException)
                        {
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 41, Desc = "WriteStartAttribute with prefix = null, localName = xmlns - case 1", Priority = 1)]
                public void attrNamespace_41()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("foo");
                    try
                    {
                        w.WriteAttributeString(null, "xmlns", "http://www.w3.org/2000/xmlns/", "ns");
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    finally
                    {
                        w.Dispose();
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 42, Desc = "WriteStartAttribute with prefix = null, localName = xmlns - case 2", Priority = 1)]
                public void attrNamespace_42()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("pre", "foo", "ns1");
                    w.WriteAttributeString(null, "xmlns", "http://www.w3.org/2000/xmlns/", "ns");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<pre:foo xmlns:pre='ns1' xmlns='ns' />")) throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCCData : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "WriteCData with null", Priority = 1)]
                private void CData_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteCData(null);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><![CDATA[]]></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "WriteCData with String.Empty", Priority = 1)]
                private void CData_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteCData(string.Empty);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><![CDATA[]]></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "WriteCData Sanity test", Priority = 0)]
                private void CData_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteCData("This text is in a CDATA section");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><![CDATA[This text is in a CDATA section]]></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "WriteCData with valid surrogate pair", Priority = 1)]
                private void CData_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteCData("\uD812\uDD12");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><![CDATA[\uD812\uDD12]]></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "WriteCData with ]]>", Priority = 1)]
                [Fact]
                [ActiveIssue("https://github.com/dotnet/runtime/issues/50944", TestPlatforms.Android)]
                public void WriteCDataWithTwoClosingBrackets_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = doc.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteCData("test ]]> test");
                        w.WriteEndElement();
                    }

                    using (XmlReader reader = doc.CreateReader())
                    {
                        Exception exception = AssertExtensions.Throws<ArgumentException>(null, () => MoveToFirstElement(reader).ReadOuterXml());
                        // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                        // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                        // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                        Assert.Matches(@"[\p{Pi}\p{Po}]" + Regex.Escape("]]>") + @"[\p{Pf}\p{Po}]", exception.Message);
                        Assert.Matches(@"\b" + "XML" + @"\b", exception.Message);
                        Assert.Matches(@"\b" + "CDATA" + @"\b", exception.Message);
                    }
                }

                //[Variation(Id = 6, Desc = "WriteCData with & < > chars, they should not be escaped", Priority = 2)]
                private void CData_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteCData("<greeting>Hello World! & Hello XML</greeting>");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><![CDATA[<greeting>Hello World! & Hello XML</greeting>]]></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 7, Desc = "WriteCData with <![CDATA[", Priority = 2)]
                private void CData_7()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteCData("<![CDATA[");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><![CDATA[<![CDATA[]]></Root>")) throw new TestException(TestResult.Failed, "");
                }
                //[Variation(Id = 8, Desc = "CData state machine", Priority = 2)]
                private void CData_8()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteCData("]x]>]]x> x]x]x> x]]x]]x>");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><![CDATA[]x]>]]x> x]x]x> x]]x]]x>]]></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 9, Desc = "WriteCData with invalid surrogate pair", Priority = 1)]
                private void CData_9()
                {
                    XCData xa = new XCData("\uD812");
                    XElement doc = new XElement("root");
                    doc.Add(xa);
                    XmlWriter w = doc.CreateWriter();
                    w.Dispose();
                    try
                    {
                        doc.Save(new MemoryStream());
                    }
                    catch (ArgumentException)
                    {
                        CheckClosedState(w.WriteState);
                        return;
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 10, Desc = "WriteCData after root element")]
                private void CData_10()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            w.WriteCData("foo");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 11, Desc = "Call WriteCData twice - that should write two CData blocks", Priority = 1)]
                private void CData_11()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteCData("foo");
                    w.WriteCData("bar");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><![CDATA[foo]]><![CDATA[bar]]></Root>")) throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCComment : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Sanity test for WriteComment", Priority = 0)]
                private void comment_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteComment("This text is a comment");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><!--This text is a comment--></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Comment value = String.Empty", Priority = 0)]
                private void comment_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteComment(string.Empty);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><!----></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Comment value = null", Priority = 0)]
                private void comment_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteComment(null);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><!----></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "WriteComment with valid surrogate pair", Priority = 1)]
                private void comment_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteComment("\uD812\uDD12");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><!--\uD812\uDD12--></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "WriteComment with invalid surrogate pair", Priority = 1)]
                private void comment_5()
                {
                    XComment xa = new XComment("\uD812");
                    XElement doc = new XElement("root");
                    doc.Add(xa);
                    XmlWriter w = doc.CreateWriter();
                    w.Dispose();
                    try
                    {
                        doc.Save(new MemoryStream());
                    }
                    catch (ArgumentException)
                    {
                        CheckClosedState(w.WriteState);
                        return;
                    }
                    TestLog.WriteLine("Did not throw error");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "WriteComment with -- in value", Priority = 1)]
                [Fact]
                [ActiveIssue("https://github.com/dotnet/runtime/issues/50944", TestPlatforms.Android)]
                public void WriteCommentWithDoubleHyphensInValue()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = doc.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteComment("test --");
                        w.WriteEndElement();
                    }

                    using (XmlReader reader = doc.CreateReader())
                    {
                        Exception exception = AssertExtensions.Throws<ArgumentException>(null, () => MoveToFirstElement(reader).ReadOuterXml());
                        // \b word boundary
                        // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                        // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                        // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                        Assert.Matches(@"\b" + "XML" + @"\b", exception.Message);
                        Assert.Matches(@"[\p{Pi}\p{Po}]" + Regex.Escape("--") + @"[\p{Pf}\p{Po}]", exception.Message);
                        Assert.Matches(@"[\p{Pi}\p{Po}]" + Regex.Escape("-") + @"[\p{Pf}\p{Po}]", exception.Message);
                    }
                }
            }

            public partial class TCEntityRef : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "WriteEntityRef with value = null", Param = "null", Priority = 1)]
                //[Variation(Id = 2, Desc = "WriteEntityRef with value = String.Empty", Param = "String.Empty", Priority = 1)]
                //[Variation(Id = 3, Desc = "WriteEntityRef with invalid value <;", Param = "test<test", Priority = 1)]
                //[Variation(Id = 4, Desc = "WriteEntityRef with invalid value >", Param = "test>test", Priority = 1)]
                //[Variation(Id = 5, Desc = "WriteEntityRef with invalid value &", Param = "test&test", Priority = 1)]
                //[Variation(Id = 6, Desc = "WriteEntityRef with invalid value & and ;", Param = "&test;", Priority = 1)]
                //[Variation(Id = 7, Desc = "WriteEntityRef with invalid value SQ", Param = "test'test", Priority = 1)]
                //[Variation(Id = 8, Desc = "WriteEntityRef with invalid value DQ", Param = "test\"test", Priority = 1)]
                //[Variation(Id = 9, Desc = "WriteEntityRef with #xD", Param = "\xD", Priority = 1)]
                //[Variation(Id = 10, Desc = "WriteEntityRef with #xA", Param = "\xD", Priority = 1)]
                //[Variation(Id = 11, Desc = "WriteEntityRef with #xD#xA", Param = "\xD\xA", Priority = 1)]
                public void entityRef_1()
                {
                    XDocument doc = new XDocument();
                    string temp = null;
                    switch (Variation.Param.ToString())
                    {
                        case "null":
                            temp = null;
                            break;
                        case "String.Empty":
                            temp = string.Empty;
                            break;
                        default:
                            temp = Variation.Param.ToString();
                            break;
                    }
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteEntityRef(temp);
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw error");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 12, Desc = "XmlWriter: Entity Refs: amp", Priority = 1)]
                public void entityRef_2()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteDocType("Root", null, null, "<!ENTITY e \"test\">");
                    w.WriteStartElement("Root");
                    w.WriteEntityRef("amp");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d, "<!DOCTYPE Root [<!ENTITY e \"test\">]><Root>&amp;</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 13, Desc = "XmlWriter: Entity Refs: apos", Priority = 1)]
                public void entityRef_3()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteDocType("root", null, null, "<!ENTITY e \"en-us\">");
                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteEntityRef("apos");
                    w.WriteString("<");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d, "<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"'&lt;\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 14, Desc = "XmlWriter: Entity Refs: lt", Priority = 1)]
                public void var_4()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteDocType("root", null, null, "<!ENTITY e \"en-us\">");
                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteEntityRef("lt");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d, "<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"&lt;\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 15, Desc = "XmlWriter: Entity Refs: quot", Priority = 1)]
                public void var_5()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteDocType("root", null, null, "<!ENTITY e \"en-us\">");
                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteEntityRef("quot");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d, "<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"&quot;\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 16, Desc = "XmlWriter: Entity Refs: gt", Priority = 1)]
                public void entityRef_6()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.WriteDocType("Root", null, null, "<!ENTITY e \"test\">");
                    w.WriteStartElement("Root");
                    w.WriteEntityRef("gt");
                    w.WriteEndElement();
                    w.Dispose();
                    if (!CompareReader(d, "<!DOCTYPE Root [<!ENTITY e \"test\">]><Root>&gt;</Root>")) throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCCharEntity : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "WriteCharEntity with valid Unicode character", Priority = 0)]
                public void charEntity_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteCharEntity('\uD23E');
                    w.WriteEndAttribute();
                    w.WriteCharEntity('\uD7FF');
                    w.WriteCharEntity('\uE000');
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root a=\"&#xD23E;\">&#xD7FF;&#xE000;</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Call WriteCharEntity after WriteStartElement/WriteEndElement", Priority = 0)]
                public void charEntity_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteCharEntity('\uD001');
                    w.WriteStartElement("elem");
                    w.WriteCharEntity('\uF345');
                    w.WriteEndElement();
                    w.WriteCharEntity('\u0048');
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>&#xD001;<elem>&#xF345;</elem>&#x48;</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Call WriteCharEntity after WriteStartAttribute/WriteEndAttribute", Priority = 0)]
                public void charEntity_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteCharEntity('\u1289');
                    w.WriteEndAttribute();
                    w.WriteCharEntity('\u2584');
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root a=\"&#x1289;\">&#x2584;</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "Character from low surrogate range", Priority = 1)]
                public void charEntity_4()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteCharEntity('\uDD12');
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "Character from high surrogate range", Priority = 1)]
                public void charEntity_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteCharEntity('\uD812');
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 7, Desc = "Sanity test, pass 'a'", Priority = 0)]
                public void charEntity_7()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");
                    w.WriteCharEntity('c');
                    w.WriteEndElement();

                    w.Dispose();

                    string strExp = "<root>&#x63;</root>";
                    if (!CompareReader(doc, strExp)) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "WriteCharEntity for special attributes", Priority = 1)]
                public void charEntity_8()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteCharEntity('A');
                    w.WriteString("\n");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<root xml:lang=\"A&#xA;\" />")) throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCSurrogateCharEntity : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "SurrogateCharEntity after WriteStartElement/WriteEndElement", Priority = 1)]
                public void surrogateEntity_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteSurrogateCharEntity('\uDF41', '\uD920');
                    w.WriteStartElement("Elem");
                    w.WriteSurrogateCharEntity('\uDE44', '\uDAFF');
                    w.WriteEndElement();
                    w.WriteSurrogateCharEntity('\uDC22', '\uD820');
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "SurrogateCharEntity after WriteStartAttribute/WriteEndAttribute", Priority = 1)]
                public void surrogateEntity_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteSurrogateCharEntity('\uDF41', '\uD920');
                    w.WriteEndAttribute();
                    w.WriteSurrogateCharEntity('\uDE44', '\uDAFF');
                    w.WriteEndElement();

                    w.Dispose();

                    if (!CompareReader(doc, "<Root a=\"&#x58341;\">&#xCFE44;</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Test with limits of surrogate range", Priority = 1)]
                public void surrogateEntity_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteSurrogateCharEntity('\uDC00', '\uD800');
                    w.WriteEndAttribute();
                    w.WriteSurrogateCharEntity('\uDFFF', '\uD800');
                    w.WriteSurrogateCharEntity('\uDC00', '\uDBFF');
                    w.WriteSurrogateCharEntity('\uDFFF', '\uDBFF');
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root a=\"&#x10000;\">&#x103FF;&#x10FC00;&#x10FFFF;</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "MIddle surrogate character", Priority = 1)]
                public void surrogateEntity_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteSurrogateCharEntity('\uDD12', '\uDA34');
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>&#x9D112;</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "Invalid high surrogate character", Priority = 1)]
                public void surrogateEntity_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteSurrogateCharEntity('\uDD12', '\uDD01');
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "Invalid low surrogate character", Priority = 1)]
                public void surrogateEntity_6()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteSurrogateCharEntity('\u1025', '\uD900');
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 7, Desc = "Swap high-low surrogate characters", Priority = 1)]
                public void surrogateEntity_7()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteSurrogateCharEntity('\uD9A2', '\uDE34');
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "WriteSurrogateCharEntity for special attributes", Priority = 1)]
                public void surrogateEntity_8()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteSurrogateCharEntity('\uDC00', '\uDBFF');
                    w.WriteEndAttribute();
                    w.WriteEndElement();

                    w.Dispose();

                    string strExp = "<root xml:lang=\"&#x10FC00;\" />";
                    if (!CompareReader(doc, strExp)) throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCPI : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Sanity test for WritePI", Priority = 0)]
                private void pi_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("test", "This text is a PI");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><?test This text is a PI?></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "PI text value = null", Priority = 1)]
                private void pi_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("test", null);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><?test?></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "PI text value = String.Empty", Priority = 1)]
                private void pi_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("test", string.Empty);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><?test?></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "PI name = null should error", Priority = 1)]
                private void pi_4()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteProcessingInstruction(null, "test");
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "PI name = String.Empty should error", Priority = 1)]
                private void pi_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteProcessingInstruction(string.Empty, "test");
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "WritePI with xmlns as the name value")]
                private void pi_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("xmlns", "text");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!(CompareReader(doc, "<Root><?xmlns text?></Root>")))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 7, Desc = "WritePI with XmL as the name value")]
                private void pi_7()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteProcessingInstruction("XmL", "text");
                            w.WriteEndElement();
                            w.Dispose();
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "WritePI before XmlDecl", Priority = 1)]
                private void pi_8()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteProcessingInstruction("pi", "text");
                            w.WriteStartDocument(true);
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 9, Desc = "WritePI (after StartDocument) with name = 'xml' text = 'version = 1.0' should error", Priority = 1)]
                private void pi_9()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteProcessingInstruction("xml", "version = \"1.0\"");
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 10, Desc = "WritePI (before StartDocument) with name = 'xml' text = 'version = 1.0' should error", Priority = 1)]
                private void pi_10()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteProcessingInstruction("xml", "version = \"1.0\"");
                            w.WriteStartDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 11, Desc = "Include PI end tag ?> as part of the text value", Priority = 1)]
                [Fact]
                [ActiveIssue("https://github.com/dotnet/runtime/issues/50944", TestPlatforms.Android)]
                public void IncludePIEndTagAsPartOfTextValue()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        w.WriteStartElement("Root");
                        w.WriteProcessingInstruction("badpi", "text ?>");
                        w.WriteEndElement();
                    }

                    using (XmlReader reader = doc.CreateReader())
                    {
                        Exception exception = AssertExtensions.Throws<ArgumentException>(null, () => MoveToFirstElement(reader).ReadOuterXml());
                        // \b word boundary
                        // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                        // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                        // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                        Assert.Matches(@"[\p{Pi}\p{Po}]" + Regex.Escape("?>") + @"[\p{Pf}\p{Po}]", exception.Message);
                        Assert.Matches(@"\b" + "XML" + @"\b", exception.Message);
                    }
                }

                //[Variation(Id = 12, Desc = "WriteProcessingInstruction with valid surrogate pair", Priority = 1)]
                private void pi_12()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("pi", "\uD812\uDD12");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root><?pi \uD812\uDD12?></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 13, Desc = "WritePI with invalid surrogate pair", Priority = 1)]
                private void pi_13()
                {
                    XProcessingInstruction xa = new XProcessingInstruction("pi", "\uD812");
                    XElement doc = new XElement("root");
                    doc.Add(xa);
                    XmlWriter w = doc.CreateWriter();
                    w.Dispose();
                    try
                    {
                        doc.Save(new MemoryStream());
                    }
                    catch (ArgumentException)
                    {
                        CheckClosedState(w.WriteState);
                        return;
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCWriteNmToken : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Name = null", Param = "null", Priority = 1)]
                //[Variation(Id = 2, Desc = "Name = String.Empty", Param = "String.Empty", Priority = 1)]
                public void writeNmToken_1()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            string temp;
                            if (Variation.Param.ToString() == "null")
                                temp = null;
                            else
                                temp = string.Empty;
                            w.WriteNmToken(temp);
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            CheckElementState(w.WriteState);
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Sanity test, Name = foo", Priority = 1)]
                public void writeNmToken_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");
                    w.WriteNmToken("foo");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<root>foo</root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Name contains letters, digits, . _ - : chars", Priority = 1)]
                public void writeNmToken_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");
                    w.WriteNmToken("_foo:1234.bar-");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<root>_foo:1234.bar-</root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "Name contains whitespace char", Param = "test test", Priority = 1)]
                //[Variation(Id = 5, Desc = "Name contains ? char", Param = "test?", Priority = 1)]
                //[Variation(Id = 6, Desc = "Name contains SQ", Param = "test'", Priority = 1)]
                //[Variation(Id = 7, Desc = "Name contains DQ", Param = "\"test", Priority = 1)]
                public void writeNmToken_4()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            w.WriteNmToken(Variation.Param.ToString());
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            CheckElementState(w.WriteState);
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCWriteName : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Name = null", Param = "null", Priority = 1)]
                //[Variation(Id = 2, Desc = "Name = String.Empty", Param = "String.Empty", Priority = 1)]
                public void writeName_1()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            string temp;
                            if (Variation.Param.ToString() == "null")
                                temp = null;
                            else
                                temp = string.Empty;
                            w.WriteName(temp);
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            CheckElementState(w.WriteState);
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Sanity test, Name = foo", Priority = 1)]
                public void writeName_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");
                    w.WriteName("foo");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<root>foo</root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Sanity test, Name = foo:bar", Priority = 1)]
                public void writeName_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");
                    w.WriteName("foo:bar");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<root>foo:bar</root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "Name starts with :", Param = ":bar", Priority = 1)]
                //[Variation(Id = 5, Desc = "Name contains whitespace char", Param = "foo bar", Priority = 1)]
                public void writeName_4()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            w.WriteName(Variation.Param.ToString());
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            CheckElementState(w.WriteState);
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCWriteQName : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Name = null", Param = "null", Priority = 1)]
                //[Variation(Id = 2, Desc = "Name = String.Empty", Param = "String.Empty", Priority = 1)]
                public void writeQName_1()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            w.WriteAttributeString("xmlns", "foo", null, "test");
                            string temp;
                            if (Variation.Param.ToString() == "null")
                                temp = null;
                            else
                                temp = string.Empty;
                            w.WriteQualifiedName(temp, "test");
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "WriteQName with correct NS", Priority = 1)]
                public void writeQName_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");
                    w.WriteAttributeString("xmlns", "foo", null, "test");
                    w.WriteQualifiedName("bar", "test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<root xmlns:foo=\"test\">foo:bar</root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "WriteQName when NS is auto-generated", Priority = 1)]
                public void writeQName_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("foo", "root", "test");
                    w.WriteQualifiedName("bar", "test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<foo:root xmlns:foo=\"test\">foo:bar</foo:root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "QName = foo:bar when foo is not in scope", Priority = 1)]
                public void writeQName_4()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            w.WriteQualifiedName("bar", "foo");
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "Name starts with :", Param = ":bar", Priority = 1)]
                //[Variation(Id = 7, Desc = "Name contains whitespace char", Param = "foo bar", Priority = 1)]
                public void writeQName_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            w.WriteAttributeString("xmlns", "foo", null, "test");
                            w.WriteQualifiedName(Variation.Param.ToString(), "test");
                            w.WriteEndElement();
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCWriteChars : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "WriteChars with valid buffer, number, count", Priority = 0)]
                public void writeChars_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    string s = "test the buffer";
                    char[] buf = s.ToCharArray();
                    w.WriteStartElement("Root");
                    w.WriteChars(buf, 0, 4);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>test</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "WriteChars with & < >", Priority = 1)]
                public void writeChars_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    string s = "&<>theend";
                    char[] buf = s.ToCharArray();

                    w.WriteStartElement("Root");
                    w.WriteChars(buf, 0, 5);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>&amp;&lt;&gt;th</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "WriteChars following WriteStartAttribute", Priority = 1)]
                public void writeChars_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    string s = "valid";
                    char[] buf = s.ToCharArray();

                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteChars(buf, 0, 5);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root a=\"valid\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "WriteChars with entity ref included", Priority = 1)]
                public void writeChars_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    string s = "this is an entity &foo;";
                    char[] buf = s.ToCharArray();

                    w.WriteStartElement("Root");
                    w.WriteChars(buf, 0, buf.Length);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>this is an entity &amp;foo;</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "WriteChars with buffer = null", Priority = 2)]
                public void writeChars_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteChars(null, 0, 0);
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "WriteChars with count > buffer size", Priority = 1)]
                public void writeChars_6()
                {
                    VerifyInvalidWrite("WriteChars", 5, 0, 6, typeof(System.ArgumentOutOfRangeException));
                }

                //[Variation(Id = 7, Desc = "WriteChars with count < 0", Priority = 1)]
                public void writeChars_7()
                {
                    VerifyInvalidWrite("WriteChars", 5, 2, -1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 8, Desc = "WriteChars with index > buffer size", Priority = 1)]
                public void writeChars_8()
                {
                    VerifyInvalidWrite("WriteChars", 5, 6, 1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 9, Desc = "WriteChars with index < 0", Priority = 1)]
                public void writeChars_9()
                {
                    VerifyInvalidWrite("WriteChars", 5, -1, 1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 10, Desc = "WriteChars with index + count exceeds buffer", Priority = 1)]
                public void writeChars_10()
                {
                    VerifyInvalidWrite("WriteChars", 5, 2, 5, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 11, Desc = "WriteChars for xml:lang attribute, index = count = 0", Priority = 1)]
                public void writeChars_11()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    string s = "en-us;";
                    char[] buf = s.ToCharArray();

                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteChars(buf, 0, 0);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<root xml:lang='' />")) throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCWriteString : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "WriteString(null)", Priority = 0)]
                public void writeString_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteString(null);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "WriteString(String.Empty)", Priority = 1)]
                public void writeString_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteString(string.Empty);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "WriteString with valid surrogate pair", Priority = 1)]
                public void writeString_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteString("\uD812\uDD12");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>\uD812\uDD12</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "WriteString with invalid surrogate pair", Priority = 1)]
                public void writeString_4()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteString("\uD812");
                            w.WriteEndElement();
                            w.Dispose();
                            doc.Save(new MemoryStream());
                        }
                        catch (ArgumentException)
                        {
                            CheckClosedState(w.WriteState);
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "WriteString with entity reference", Priority = 1)]
                public void writeString_5()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteString("&test;");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>&amp;test;</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "WriteString with single/double quote, &, <, >", Priority = 1)]
                public void writeString_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteString("' & < > \"");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>&apos; &amp; &lt; &gt; \"</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 9, Desc = "WriteString for value greater than x1F", Priority = 1)]
                public void writeString_9()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteString(XmlConvert.ToString('\x21'));
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>!</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 11, Desc = "WriteString with CR, LF, CR LF inside attribute value", Priority = 1)]
                public void writeString_11()
                {
                    // \r, \n and \r\n gets replaced by char entities &#xD; &#xA; and &#xD;&#xA; respectively
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr1");
                    w.WriteString("\r");
                    w.WriteStartAttribute("attr2");
                    w.WriteString("\n");
                    w.WriteStartAttribute("attr3");
                    w.WriteString("\r\n");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareBaseline(doc, "writeStringWhiespaceInAttr.txt")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 12, Desc = "Call WriteString for LF inside attribute", Priority = 1)]
                public void writeString_12()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root", "");
                    w.WriteStartAttribute("a1", "");
                    w.WriteString("x\ny");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root a1=\"x&#xA;y\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 13, Desc = "Surrogate characters in text nodes, range limits", Priority = 1)]
                public void writeString_13()
                {
                    char[] invalidXML = { '\uD800', '\uDC00', '\uD800', '\uDFFF', '\uDBFF', '\uDC00', '\uDBFF', '\uDFFF' };
                    string invXML = new string(invalidXML);
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteString(invXML);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>\uD800\uDC00\uD800\uDFFF\uDBFF\uDC00\uDBFF\uDFFF</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 14, Desc = "High surrogate on last position", Priority = 1)]
                public void writeString_14()
                {
                    char[] invalidXML = { 'a', 'b', '\uDA34' };
                    string invXML = new string(invalidXML);
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteString(invXML);
                            w.Dispose();
                            doc.Save(new MemoryStream());
                        }
                        catch (ArgumentException)
                        {
                            CheckClosedState(w.WriteState);
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 15, Desc = "Low surrogate on first position", Priority = 1)]
                public void writeString_15()
                {
                    char[] invalidXML = { '\uDF20', 'b', 'c' };
                    string invXML = new string(invalidXML);
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteString(invXML);
                            w.Dispose();
                            doc.Save(new MemoryStream());
                        }
                        catch (ArgumentException)
                        {
                            CheckClosedState(w.WriteState);
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 16, Desc = "Swap low-high surrogates", Priority = 1)]
                public void writeString_16()
                {
                    char[] invalidXML = { 'a', '\uDE40', '\uDA72', 'c' };
                    string invXML = new string(invalidXML);
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteString(invXML);
                            w.Dispose();
                            doc.Save(new MemoryStream());
                        }
                        catch (ArgumentException)
                        {
                            CheckClosedState(w.WriteState);
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCWhiteSpace : BridgeHelpers
            {
                //[Variation(Id = 3, Desc = "WriteWhitespace before and after root element", Priority = 1)]
                public void whitespace_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartDocument();
                    w.WriteWhitespace("\x20");
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteWhitespace("\x20");
                    w.WriteEndDocument();
                    w.Dispose();

                    if (!CompareBaseline(doc, "whitespace3.txt")) throw new TestException(TestResult.Failed, "");
                }

                // Factory writer behavior is inconsistent with XmlTextWriter, but consistent with other Write(string) methods
                //[Variation(Id = 4, Desc = "WriteWhitespace with null ", Param = "null", Priority = 1)]
                //[Variation(Id = 5, Desc = "WriteWhitespace with String.Empty ", Param = "String.Empty", Priority = 1)]
                public void whitespace_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    string temp;
                    if (Variation.Param.ToString() == "null")
                        temp = null;
                    else
                        temp = string.Empty;
                    w.WriteStartElement("Root");

                    w.WriteWhitespace(temp);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "WriteWhitespace with invalid char", Param = (int)0x61, Priority = 1)]
                //[Variation(Id = 7, Desc = "WriteWhitespace with invalid char", Param = (int)0xE, Priority = 1)]
                //[Variation(Id = 8, Desc = "WriteWhitespace with invalid char", Param = (int)0x0, Priority = 1)]
                //[Variation(Id = 9, Desc = "WriteWhitespace with invalid char", Param = (int)0x10, Priority = 1)]
                //[Variation(Id = 10, Desc = "WriteWhitespace with invalid char", Param = (int)0x1F, Priority = 1)]
                public void whitespace_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteWhitespace(((char)(int)Variation.Param).ToString());
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCWriteValue : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "WriteValue(boolean)", Priority = 1)]
                public void writeValue_1()
                {
                    bool b = true;
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteValue(b);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>true</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "WriteValue(DateTime)", Priority = 1)]
                public void writeValue_2()
                {
                    DateTime myDT = new DateTime(2002, 4, 3, 0, 0, 0, DateTimeKind.Utc);
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteValue(myDT);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>2002-04-03T00:00:00Z</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "WriteValue(decimal)", Priority = 1)]
                public void writeValue_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteValue(decimal.MaxValue);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>79228162514264337593543950335</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "WriteValue(double)", Priority = 1)]
                public void writeValue_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteValue(double.MaxValue);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>1.7976931348623157E+308</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "WriteValue(int32)", Priority = 1)]
                public void writeValue_5()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteValue(int.MaxValue);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>2147483647</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "WriteValue(int64)", Priority = 1)]
                public void writeValue_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteValue(long.MaxValue);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>9223372036854775807</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 7, Desc = "WriteValue(single)", Priority = 1)]
                public void writeValue_7()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteValue(float.MaxValue);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, $"<Root>{float.MaxValue.ToString("R", CultureInfo.InvariantCulture)}</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "WriteValue(string)", Priority = 1)]
                public void writeValue_8()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteValue("Test");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>Test</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 9, Desc = "WriteValue(DateTimeOffset)", Priority = 1)]
                public void writeValue_9()
                {
                    DateTimeOffset myDTO = new DateTimeOffset(2011, 5, 9, 11, 18, 23, new TimeSpan(8, 0, 0));
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteValue(myDTO);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>2011-05-09T11:18:23+08:00</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 11, Desc = "Write multiple atomic values inside element", Priority = 1)]
                public void writeValue_11()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    w.WriteStartElement("Root");
                    w.WriteValue((int)2);
                    w.WriteValue((bool)true);
                    w.WriteValue((double)3.14);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>2true3.14</Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 12, Desc = "Write multiple atomic values inside attribute", Priority = 1)]
                public void writeValue_12()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr");
                    w.WriteValue((int)2);
                    w.WriteValue((bool)true);
                    w.WriteValue((double)3.14);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root attr=\"2true3.14\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 13, Desc = "Write multiple atomic values inside element, separate by WriteWhitespace(' ')", Priority = 1)]
                public void writeValue_13()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    w.WriteStartElement("Root");
                    w.WriteValue((int)2);
                    w.WriteWhitespace(" ");
                    w.WriteValue((bool)true);
                    w.WriteWhitespace(" ");
                    w.WriteValue((double)3.14);
                    w.WriteWhitespace(" ");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>2 true 3.14 </Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 14, Desc = "Write multiple atomic values inside element, separate by WriteString(' ')", Priority = 1)]
                public void writeValue_14()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    w.WriteStartElement("Root");
                    w.WriteValue((int)2);
                    w.WriteString(" ");
                    w.WriteValue((bool)true);
                    w.WriteString(" ");
                    w.WriteValue((double)3.14);
                    w.WriteString(" ");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>2 true 3.14 </Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 15, Desc = "Write multiple atomic values inside attribute, separate by WriteWhitespace(' ')", Priority = 1)]
                public void writeValue_15()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr");
                    w.WriteValue((int)2);
                    w.WriteWhitespace(" ");
                    w.WriteValue((bool)true);
                    w.WriteWhitespace(" ");
                    w.WriteValue((double)3.14);
                    w.WriteWhitespace(" ");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root attr=\"2 true 3.14 \" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 16, Desc = "Write multiple atomic values inside attribute, separate by WriteString(' ')", Priority = 1)]
                public void writeValue_16()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr");
                    w.WriteValue((int)2);
                    w.WriteString(" ");
                    w.WriteValue((bool)true);
                    w.WriteString(" ");
                    w.WriteValue((double)3.14);
                    w.WriteString(" ");
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root attr=\"2 true 3.14 \" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 17, Desc = "WriteValue(long)", Priority = 1)]
                public void writeValue_17()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteValue(long.MaxValue);
                    w.WriteStartElement("child");
                    w.WriteValue(long.MinValue);
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root>9223372036854775807<child>-9223372036854775808</child></Root>")) throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCLookUpPrefix : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "LookupPrefix with null", Priority = 2)]
                public void lookupPrefix_1()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            string s = w.LookupPrefix(null);
                            w.Dispose();
                        }
                        catch (ArgumentException)
                        {
                            CheckErrorState(w.WriteState);
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "LookupPrefix with String.Empty should if (!String.Empty", Priority = 1)]
                public void lookupPrefix_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    string s = w.LookupPrefix(string.Empty);
                    TestLog.Compare(s, string.Empty, "Error");
                    w.Dispose();
                }

                //[Variation(Id = 3, Desc = "LookupPrefix with generated namespace used for attributes", Priority = 1)]
                public void lookupPrefix_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", "foo", "b");
                    string s = w.LookupPrefix("foo");
                    string exp = "p1";
                    TestLog.Compare(s, exp, "Error");
                    w.Dispose();
                }

                //[Variation(Id = 4, Desc = "LookupPrefix for namespace used with element", Priority = 0)]
                public void lookupPrefix_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("ns1", "Root", "foo");
                    string s = w.LookupPrefix("foo");
                    TestLog.Compare(s, "ns1", "Error");
                    w.Dispose();
                }

                //[Variation(Id = 5, Desc = "LookupPrefix for namespace used with attribute", Priority = 0)]
                public void lookupPrefix_5()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("ns1", "attr1", "foo", "val1");
                    string s = w.LookupPrefix("foo");
                    TestLog.Compare(s, "ns1", "Error");
                    w.Dispose();
                }

                //[Variation(Id = 6, Desc = "Lookup prefix for a default namespace", Priority = 1)]
                public void lookupPrefix_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root", "foo");
                    w.WriteString("content");
                    string s = w.LookupPrefix("foo");
                    TestLog.Compare(s, string.Empty, "Error");
                    w.Dispose();
                }

                //[Variation(Id = 7, Desc = "Lookup prefix for nested element with same namespace but different prefix", Priority = 1)]
                public void lookupPrefix_7()
                {
                    XDocument doc = new XDocument();
                    string s = "";
                    XmlWriter w = CreateWriter(doc);

                    w.WriteStartElement("x", "Root", "foo");
                    s = w.LookupPrefix("foo");
                    TestLog.Compare(s, "x", "Error");

                    w.WriteStartElement("y", "node", "foo");
                    s = w.LookupPrefix("foo");
                    TestLog.Compare(s, "y", "Error");

                    w.WriteStartElement("z", "node1", "foo");
                    s = w.LookupPrefix("foo");
                    TestLog.Compare(s, "z", "Error");
                    w.WriteEndElement();

                    s = w.LookupPrefix("foo");
                    TestLog.Compare(s, "y", "Error");
                    w.WriteEndElement();

                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 8, Desc = "Lookup prefix for multiple prefix associated with the same namespace", Priority = 1)]
                public void lookupPrefix_8()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("y", "a", "foo", "b");
                    string s = w.LookupPrefix("foo");
                    TestLog.Compare(s, "y", "Error");
                    w.Dispose();
                }

                //[Variation(Id = 9, Desc = "Lookup prefix for namespace defined outside the scope of an empty element and also defined in its parent", Priority = 1)]
                public void lookupPrefix_9()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteStartElement("y", "node", "foo");
                    w.WriteEndElement();
                    string s = w.LookupPrefix("foo");
                    TestLog.Compare(s, "x", "Error");
                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 10, Desc = "Lookup prefix for namespace declared as default and also with a prefix", Priority = 1)]
                public void lookupPrefix_10()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root", "foo");
                    w.WriteStartElement("x", "node", "foo");
                    string s = w.LookupPrefix("foo");
                    TestLog.Compare(s, "x", "Error in nested element");
                    w.WriteEndElement();
                    s = w.LookupPrefix("foo");
                    TestLog.Compare(s, string.Empty, "Error in root element");
                    w.WriteEndElement();
                    w.Dispose();
                }
            }

            public partial class TCXmlSpaceWriter : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Verify XmlSpace as Preserve", Priority = 0)]
                public void xmlSpace_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xml", "space", null, "preserve");
                    TestLog.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");
                    w.Dispose();
                }

                //[Variation(Id = 2, Desc = "Verify XmlSpace as Default", Priority = 0)]
                public void xmlSpace_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xml", "space", null, "default");
                    TestLog.Compare(w.XmlSpace, XmlSpace.Default, "Error");
                    w.Dispose();
                }

                //[Variation(Id = 3, Desc = "Verify XmlSpace as None", Priority = 0)]
                public void xmlSpace_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    TestLog.Compare(w.XmlSpace, XmlSpace.None, "Error");
                    w.Dispose();
                }

                //[Variation(Id = 4, Desc = "Verify XmlSpace within an empty element", Priority = 1)]
                public void xmlSpace_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xml", "space", null, "preserve");
                    w.WriteStartElement("node", null);

                    TestLog.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");

                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 5, Desc = "Verify XmlSpace - scope with nested elements (both PROLOG and EPILOG)", Priority = 1)]
                public void xmlSpace_5()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");

                    w.WriteStartElement("node", null);
                    w.WriteAttributeString("xml", "space", null, "preserve");
                    TestLog.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");

                    w.WriteStartElement("node1");
                    TestLog.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");

                    w.WriteStartElement("node2");
                    w.WriteAttributeString("xml", "space", null, "default");
                    TestLog.Compare(w.XmlSpace, XmlSpace.Default, "Error");
                    w.WriteEndElement();

                    TestLog.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");
                    w.WriteEndElement();

                    TestLog.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");
                    w.WriteEndElement();

                    TestLog.Compare(w.XmlSpace, XmlSpace.None, "Error");
                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 6, Desc = "Verify XmlSpace - outside defined scope", Priority = 1)]
                public void xmlSpace_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartElement("node", null);
                    w.WriteAttributeString("xml", "space", null, "preserve");
                    w.WriteEndElement();

                    TestLog.Compare(w.XmlSpace, XmlSpace.None, "Error");
                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 7, Desc = "Verify XmlSpace with invalid space value", Priority = 0)]
                public void xmlSpace_7()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteStartElement("node", null);
                            w.WriteAttributeString("xml", "space", null, "reserve");
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Exception expected");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 8, Desc = "Duplicate xml:space attr should error", Priority = 1)]
                public void xmlSpace_8()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xml", "space", null, "preserve");
                            w.WriteAttributeString("xml", "space", null, "default");
                        }
                        catch (XmlException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Exception expected");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 9, Desc = "Verify XmlSpace value when received through WriteString", Priority = 1)]
                public void xmlSpace_9()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("xml", "space", null);
                    w.WriteString("default");
                    w.WriteEndAttribute();

                    TestLog.Compare(w.XmlSpace, XmlSpace.Default, "Error");
                    w.WriteEndElement();
                    w.Dispose();
                }
            }

            public partial class TCXmlLangWriter : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Verify XmlLang sanity test", Priority = 0)]
                public void XmlLang_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartElement("node", null);
                    w.WriteAttributeString("xml", "lang", null, "en");

                    TestLog.Compare(w.XmlLang, "en", "Error");

                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 2, Desc = "Verify that default value of XmlLang is NULL", Priority = 1)]
                public void XmlLang_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    if (w.XmlLang != null)
                    {
                        w.Dispose();
                        TestLog.WriteLine("Default value if no xml:lang attributes are currently on the stack should be null");
                        TestLog.WriteLine("Actual value: {0}", w.XmlLang.ToString());
                        throw new TestException(TestResult.Failed, "");
                    }
                    w.Dispose();
                }

                //[Variation(Id = 3, Desc = "Verify XmlLang scope inside nested elements (both PROLOG and EPILOG)", Priority = 1)]
                public void XmlLang_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");

                    w.WriteStartElement("node", null);
                    w.WriteAttributeString("xml", "lang", null, "fr");
                    TestLog.Compare(w.XmlLang, "fr", "Error");

                    w.WriteStartElement("node1");
                    w.WriteAttributeString("xml", "lang", null, "en-US");
                    TestLog.Compare(w.XmlLang, "en-US", "Error");

                    w.WriteStartElement("node2");
                    TestLog.Compare(w.XmlLang, "en-US", "Error");
                    w.WriteEndElement();

                    TestLog.Compare(w.XmlLang, "en-US", "Error");
                    w.WriteEndElement();

                    TestLog.Compare(w.XmlLang, "fr", "Error");
                    w.WriteEndElement();

                    TestLog.Compare(w.XmlLang, null, "Error");
                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 4, Desc = "Duplicate xml:lang attr should error", Priority = 1)]
                public void XmlLang_4()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xml", "lang", null, "en-us");
                            w.WriteAttributeString("xml", "lang", null, "ja");
                        }
                        catch (XmlException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Exception expected");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "Verify XmlLang value when received through WriteAttributes", Priority = 1)]
                public void XmlLang_5()
                {
                    XmlReader tr = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));

                    while (tr.Read())
                    {
                        if (tr.LocalName == "XmlLangNode")
                        {
                            tr.Read();
                            tr.MoveToNextAttribute();
                            break;
                        }
                    }
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributes(tr, false);

                    TestLog.Compare(w.XmlLang, "fr", "Error");
                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 6, Desc = "Verify XmlLang value when received through WriteString")]
                public void XmlLang_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteString("en-US");
                    w.WriteEndAttribute();

                    TestLog.Compare(w.XmlLang, "en-US", "Error");
                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 7, Desc = "Should not check XmlLang value", Priority = 2)]
                public void XmlLang_7()
                {
                    string[] langs = new string[] { "en-", "e n", "en", "en-US", "e?", "en*US" };
                    for (int i = 0; i < langs.Length; i++)
                    {
                        XDocument doc = new XDocument();
                        XmlWriter w = CreateWriter(doc);
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "lang", null, langs[i]);
                        w.WriteEndElement();
                        w.Dispose();


                        string strExp = "<Root xml:lang=\"" + langs[i] + "\" />";
                        if (!CompareReader(doc, strExp))
                            throw new TestException(TestResult.Failed, "");
                    }
                }

                //[Variation(Id = 8, Desc = "More XmlLang with valid sequence", Priority = 1)]
                public void XmlLang_8()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xml", "lang", null, "U.S.A.");
                    w.Dispose();
                }
            }

            public partial class TCWriteRaw : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Call both WriteRaw Methods", Priority = 1)]
                public void writeRaw_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    string t = "Test Case";
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteRaw(t);
                    w.WriteStartAttribute("b");
                    w.WriteRaw(t.ToCharArray(), 0, 4);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root a=\"Test Case\" b=\"Test\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "Call WriteRaw to write the value of xml:space")]
                public void writeRaw_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("xml", "space", null);
                    w.WriteRaw("default");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<Root xml:space=\"default\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "Call WriteRaw to write the value of xml:lang", Priority = 1)]
                public void writerRaw_5()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    string strraw = "abc";
                    char[] buffer = strraw.ToCharArray();

                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteRaw(buffer, 1, 1);
                    w.WriteRaw(buffer, 0, 2);
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<root xml:lang=\"bab\" />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 6, Desc = "WriteRaw with count > buffer size", Priority = 1)]
                public void writeRaw_6()
                {
                    VerifyInvalidWrite("WriteRaw", 5, 0, 6, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 7, Desc = "WriteRaw with count < 0", Priority = 1)]
                public void writeRaw_7()
                {
                    VerifyInvalidWrite("WriteRaw", 5, 2, -1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 8, Desc = "WriteRaw with index > buffer size", Priority = 1)]
                public void writeRaw_8()
                {
                    VerifyInvalidWrite("WriteRaw", 5, 6, 1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 9, Desc = "WriteRaw with index < 0", Priority = 1)]
                public void writeRaw_9()
                {
                    VerifyInvalidWrite("WriteRaw", 5, -1, 1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 10, Desc = "WriteRaw with index + count exceeds buffer", Priority = 1)]
                public void writeRaw_10()
                {
                    VerifyInvalidWrite("WriteRaw", 5, 2, 5, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 11, Desc = "WriteRaw with buffer = null", Priority = 1)]
                public void writeRaw_11()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteRaw(null, 0, 0);
                        }
                        catch (ArgumentNullException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 12, Desc = "WriteRaw with valid surrogate pair", Priority = 1)]
                public void writeRaw_12()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");

                    string str = "\uD812\uDD12";
                    char[] chr = str.ToCharArray();

                    w.WriteRaw(str);
                    w.WriteRaw(chr, 0, chr.Length);
                    w.WriteEndElement();
                    w.Dispose();

                    string strExp = "<Root>\uD812\uDD12\uD812\uDD12</Root>";
                    if (!CompareReader(doc, strExp)) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 13, Desc = "WriteRaw with invalid surrogate pair", Priority = 1)]
                public void writeRaw_13()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteRaw("\uD812");
                            w.Dispose();
                            doc.Save(new MemoryStream());
                        }
                        catch (ArgumentException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Closed, "WriteState should be Closed");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 14, Desc = "Index = Count = 0", Priority = 1)]
                public void writeRaw_14()
                {
                    string lang = new string('a', 1);
                    char[] buffer = lang.ToCharArray();
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);

                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteRaw(buffer, 0, 0);
                    w.WriteEndElement();
                    w.Dispose();

                    if (!CompareReader(doc, "<root xml:lang=\"\" />")) throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCWriteBase64 : BridgeHelpers
            {
                //[Variation(Id = 20, Desc = "WriteBase64 with count > buffer size", Priority = 1)]
                public void Base64_2()
                {
                    VerifyInvalidWrite("WriteBase64", 5, 0, 6, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 30, Desc = "WriteBase64 with count < 0", Priority = 1)]
                public void Base64_3()
                {
                    VerifyInvalidWrite("WriteBase64", 5, 2, -1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 40, Desc = "WriteBase64 with index > buffer size", Priority = 1)]
                public void Base64_4()
                {
                    VerifyInvalidWrite("WriteBase64", 5, 5, 1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 50, Desc = "WriteBase64 with index < 0", Priority = 1)]
                public void Base64_5()
                {
                    VerifyInvalidWrite("WriteBase64", 5, -1, 1, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 60, Desc = "WriteBase64 with index + count exceeds buffer", Priority = 1)]
                public void Base64_6()
                {
                    VerifyInvalidWrite("WriteBase64", 5, 2, 5, typeof(ArgumentOutOfRangeException));
                }

                //[Variation(Id = 70, Desc = "WriteBase64 with buffer = null", Priority = 1)]
                public void Base64_7()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            w.WriteBase64(null, 0, 0);
                        }
                        catch (ArgumentNullException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 90, Desc = "Base64 should not be allowed inside xml:lang value", Priority = 1, Param = "lang")]
                //[Variation(Id = 91, Desc = "Base64 should not be allowed inside xml:space value", Priority = 1, Param = "space")]
                //[Variation(Id = 92, Desc = "Base64 should not be allowed inside namespace decl", Priority = 1, Param = "ns")]
                public void Base64_9()
                {
                    byte[] buffer = new byte[10];
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            switch (Variation.Param.ToString())
                            {
                                case "lang":
                                    w.WriteStartAttribute("xml", "lang", null);
                                    break;
                                case "space":
                                    w.WriteStartAttribute("xml", "space", null);
                                    break;
                                case "ns":
                                    w.WriteStartAttribute("xmlns", "foo", null);
                                    break;
                            }
                            w.WriteBase64(buffer, 0, 5);
                        }
                        catch (InvalidOperationException)
                        {
                            return;
                        }
                    }

                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                public static bool CompareResult(Stream fileOld, Stream fileNew)
                {
                    int bufferSize = 4096;

                    int readByteOld = 0;
                    int readByteNew = 0;
                    int count;

                    byte[] bufferOld = new byte[bufferSize];
                    byte[] bufferNew = new byte[bufferSize];


                    BinaryReader binaryReaderOld = new BinaryReader(fileOld);
                    BinaryReader binaryReaderNew = new BinaryReader(fileNew);

                    binaryReaderOld.BaseStream.Seek(0, SeekOrigin.Begin);
                    binaryReaderNew.BaseStream.Seek(0, SeekOrigin.Begin);
                    do
                    {
                        readByteOld = binaryReaderOld.Read(bufferOld, 0, bufferSize);
                        readByteNew = binaryReaderNew.Read(bufferNew, 0, bufferSize);

                        if (readByteOld != readByteNew)
                        {
                            return false;
                        }

                        for (count = 0; count < bufferSize; ++count)
                        {
                            if (bufferOld[count] != bufferNew[count])
                            {
                                return false;
                            }
                        }
                    } while (count <= readByteOld);
                    return true;
                }
            }

            public partial class TCWriteState : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Verify WriteState.Start when nothing has been written yet", Priority = 0)]
                public void writeState_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                    try
                    {
                        w.Dispose();
                    }
                    catch (Exception)
                    {
                        throw new TestException(TestResult.Failed, "");
                    }
                }

                //[Variation(Id = 2, Desc = "Verify correct state when writing in Prolog", Priority = 1)]
                public void writeState_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                    w.WriteDocType("Root", null, null, "<!ENTITY e \"test\">");
                    TestLog.Compare(w.WriteState, WriteState.Prolog, "Error");
                    w.WriteStartElement("Root");
                    TestLog.Compare(w.WriteState, WriteState.Element, "Error");
                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 3, Desc = "Verify correct state when writing an attribute", Priority = 1)]
                public void writeState_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    TestLog.Compare(w.WriteState, WriteState.Attribute, "Error");
                    w.WriteString("content");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 4, Desc = "Verify correct state when writing element content", Priority = 1)]
                public void writeState_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteString("content");
                    TestLog.Compare(w.WriteState, WriteState.Content, "Error");
                    w.WriteEndElement();
                    w.Dispose();
                }

                //[Variation(Id = 5, Desc = "Verify correct state after Close has been called", Priority = 1)]
                public void writeState_5()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.Dispose();
                    TestLog.Compare(w.WriteState, WriteState.Closed, "Error");
                }

                //[Variation(Id = 6, Desc = "Verify WriteState = Error after an exception", Priority = 1)]
                public void writeState_6()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartElement("Root");
                    }
                    catch (InvalidOperationException e)
                    {
                        TestLog.WriteLineIgnore(e.ToString());
                        TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                    }
                    finally
                    {
                        w.Dispose();
                    }
                }

                //[Variation(Id = 7, Desc = "Call WriteStartDocument after WriteState = Error", Priority = 1, Param = "WriteStartDocument")]
                //[Variation(Id = 8, Desc = "Call WriteStartElement after WriteState = Error", Priority = 1, Param = "WriteStartElement")]
                //[Variation(Id = 9, Desc = "Call WriteEndElement after WriteState = Error", Priority = 1, Param = "WriteEndElement")]
                //[Variation(Id = 10, Desc = "Call WriteStartAttribute after WriteState = Error", Priority = 1, Param = "WriteStartAttribute")]
                //[Variation(Id = 11, Desc = "Call WriteEndAttribute after WriteState = Error", Priority = 1, Param = "WriteEndAttribute")]
                //[Variation(Id = 12, Desc = "Call WriteCData after WriteState = Error", Priority = 1, Param = "WriteCData")]
                //[Variation(Id = 13, Desc = "Call WriteComment after WriteState = Error", Priority = 1, Param = "WriteComment")]
                //[Variation(Id = 14, Desc = "Call WritePI after WriteState = Error", Priority = 1, Param = "WritePI")]
                //[Variation(Id = 15, Desc = "Call WriteEntityRef after WriteState = Error", Priority = 1, Param = "WriteEntityRef")]
                //[Variation(Id = 16, Desc = "Call WriteCharEntiry after WriteState = Error", Priority = 1, Param = "WriteCharEntity")]
                //[Variation(Id = 17, Desc = "Call WriteSurrogateCharEntity after WriteState = Error", Priority = 1, Param = "WriteSurrogateCharEntity")]
                //[Variation(Id = 18, Desc = "Call WriteWhitespace after WriteState = Error", Priority = 1, Param = "WriteWhitespace")]
                //[Variation(Id = 19, Desc = "Call WriteString after WriteState = Error", Priority = 1, Param = "WriteString")]
                //[Variation(Id = 20, Desc = "Call WriteChars after WriteState = Error", Priority = 1, Param = "WriteChars")]
                //[Variation(Id = 21, Desc = "Call WriteRaw after WriteState = Error", Priority = 1, Param = "WriteRaw")]
                //[Variation(Id = 22, Desc = "Call WriteBase64 after WriteState = Error", Priority = 1, Param = "WriteBase64")]
                //[Variation(Id = 23, Desc = "Call WriteBinHex after WriteState = Error", Priority = 1, Param = "WriteBinHex")]
                //[Variation(Id = 24, Desc = "Call LookupPrefix after WriteState = Error", Priority = 1, Param = "LookupPrefix")]
                //[Variation(Id = 25, Desc = "Call WriteNmToken after WriteState = Error", Priority = 1, Param = "WriteNmToken")]
                //[Variation(Id = 26, Desc = "Call WriteName after WriteState = Error", Priority = 1, Param = "WriteName")]
                //[Variation(Id = 27, Desc = "Call WriteQualifiedName after WriteState = Error", Priority = 1, Param = "WriteQualifiedName")]
                //[Variation(Id = 28, Desc = "Call WriteValue after WriteState = Error", Priority = 1, Param = "WriteValue")]
                //[Variation(Id = 29, Desc = "Call WriteAttributes after WriteState = Error", Priority = 1, Param = "WriteAttributes")]
                //[Variation(Id = 30, Desc = "Call WriteNode(nav) after WriteState = Error", Priority = 1, Param = "WriteNodeNavigator")]
                //[Variation(Id = 31, Desc = "Call WriteNode(reader) after WriteState = Error", Priority = 1, Param = "WriteNodeReader")]
                //[Variation(Id = 32, Desc = "Call Flush after WriteState = Error", Priority = 1, Param = "Flush")]
                public void writeState_7()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    try
                    {
                        w.WriteStartDocument();
                        w.WriteStartDocument();
                    }
                    catch (InvalidOperationException)
                    {
                        TestLog.Equals(w.WriteState, WriteState.Error, "Error");
                        try
                        {
                            switch (Variation.Param.ToString())
                            {
                                case "WriteStartDocument":
                                    w.WriteStartDocument();
                                    break;
                                case "WriteStartElement":
                                    w.WriteStartElement("a");
                                    break;
                                case "WriteEndElement":
                                    w.WriteEndElement();
                                    break;
                                case "WriteStartAttribute":
                                    w.WriteStartAttribute("a");
                                    break;
                                case "WriteEndAttribute":
                                    w.WriteEndAttribute();
                                    break;
                                case "WriteCData":
                                    w.WriteCData("");
                                    break;
                                case "WriteComment":
                                    w.WriteComment("");
                                    break;
                                case "WritePI":
                                    w.WriteProcessingInstruction("a", "a");
                                    break;
                                case "WriteEntityRef":
                                    w.WriteEntityRef("a");
                                    break;
                                case "WriteCharEntity":
                                    w.WriteCharEntity('a');
                                    break;
                                case "WriteSurrogateCharEntity":
                                    w.WriteSurrogateCharEntity((char)0xDC00, (char)0xD800);
                                    break;
                                case "WriteWhitespace":
                                    w.WriteWhitespace("");
                                    break;
                                case "WriteString":
                                    w.WriteString("");
                                    break;
                                case "WriteChars":
                                    w.WriteChars(new char[10], 0, 0);
                                    break;
                                case "WriteRaw":
                                    w.WriteRaw("a");
                                    break;
                                case "WriteBase64":
                                    w.WriteBase64(new byte[10], 0, 0);
                                    break;
                                case "WriteBinHex":
                                    w.WriteBinHex(new byte[10], 0, 0);
                                    break;
                                case "LookupPrefix":
                                    w.LookupPrefix("");
                                    break;
                                case "WriteNmToken":
                                    w.WriteNmToken("a");
                                    break;
                                case "WriteName":
                                    w.WriteName("a");
                                    break;
                                case "WriteQualifiedName":
                                    w.WriteQualifiedName("a", "ns");
                                    break;
                                case "WriteValue":
                                    w.WriteValue("");
                                    break;
                                case "WriteAttributes":
                                    {
                                        XmlReader reader = CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml")));
                                        while (reader.Read())
                                        {
                                            if (reader.LocalName == "defattr")
                                            {
                                                reader.Read();
                                                break;
                                            }
                                        }
                                        w.WriteAttributes(reader, false);
                                        break;
                                    }
                                case "WriteNodeReader":
                                    w.WriteNode(CreateReaderIgnoreWS(Path.Combine(FilePathUtil.GetTestDataPath(), Path.Combine("XmlWriter2", "XmlReader.xml"))), false);
                                    break;
                                case "Flush":
                                    w.Flush();
                                    break;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            return;
                        }
                        // Flush/LookupPrefix is a NOOP
                        if (Variation.Param.ToString() == "Flush" || Variation.Param.ToString() == "LookupPrefix")
                            return;
                    }
                    finally
                    {
                        w.Dispose();
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 33, Desc = "XmlSpace property after WriteState = Error", Priority = 1, Param = "XmlSpace")]
                //[Variation(Id = 34, Desc = "XmlLang property after WriteState = Error", Priority = 1, Param = "XmlSpace")]
                public void writeState_8()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    try
                    {
                        w.WriteStartDocument();
                        w.WriteStartDocument();
                    }
                    catch (InvalidOperationException)
                    {
                        TestLog.Equals(w.WriteState, WriteState.Error, "Error");
                        switch (Variation.Param.ToString())
                        {
                            case "XmlSpace":
                                TestLog.Equals(w.XmlSpace, XmlSpace.None, "Error");
                                break;
                            case "XmlLang":
                                TestLog.Equals(w.XmlLang, null, "Error");
                                break;
                        }
                    }
                    finally
                    {
                        w.Dispose();
                    }
                }

                //[Variation(Id = 6, Desc = "Call WriteStartDocument after Close()", Priority = 1, Param = "WriteStartDocument")]
                //[Variation(Id = 7, Desc = "Call WriteStartElement after Close()", Priority = 1, Param = "WriteStartElement")]
                //[Variation(Id = 8, Desc = "Call WriteEndElement after Close()", Priority = 1, Param = "WriteEndElement")]
                //[Variation(Id = 9, Desc = "Call WriteStartAttribute after Close()", Priority = 1, Param = "WriteStartAttribute")]
                //[Variation(Id = 10, Desc = "Call WriteEndAttribute after Close()", Priority = 1, Param = "WriteEndAttribute")]
                //[Variation(Id = 11, Desc = "Call WriteCData after Close()", Priority = 1, Param = "WriteCData")]
                //[Variation(Id = 12, Desc = "Call WriteComment after Close()", Priority = 1, Param = "WriteComment")]
                //[Variation(Id = 13, Desc = "Call WritePI after Close()", Priority = 1, Param = "WritePI")]
                //[Variation(Id = 14, Desc = "Call WriteEntityRef after Close()", Priority = 1, Param = "WriteEntityRef")]
                //[Variation(Id = 15, Desc = "Call WriteCharEntiry after Close()", Priority = 1, Param = "WriteCharEntity")]
                //[Variation(Id = 16, Desc = "Call WriteSurrogateCharEntity after Close()", Priority = 1, Param = "WriteSurrogateCharEntity")]
                //[Variation(Id = 17, Desc = "Call WriteWhitespace after Close()", Priority = 1, Param = "WriteWhitespace")]
                //[Variation(Id = 18, Desc = "Call WriteString after Close()", Priority = 1, Param = "WriteString")]
                //[Variation(Id = 19, Desc = "Call WriteChars after Close()", Priority = 1, Param = "WriteChars")]
                //[Variation(Id = 20, Desc = "Call WriteRaw after Close()", Priority = 1, Param = "WriteRaw")]
                //[Variation(Id = 21, Desc = "Call WriteBase64 after Close()", Priority = 1, Param = "WriteBase64")]
                //[Variation(Id = 22, Desc = "Call WriteBinHex after Close()", Priority = 1, Param = "WriteBinHex")]
                //[Variation(Id = 23, Desc = "Call LookupPrefix after Close()", Priority = 1, Param = "LookupPrefix")]
                //[Variation(Id = 24, Desc = "Call WriteNmToken after Close()", Priority = 1, Param = "WriteNmToken")]
                //[Variation(Id = 25, Desc = "Call WriteName after Close()", Priority = 1, Param = "WriteName")]
                //[Variation(Id = 26, Desc = "Call WriteQualifiedName after Close()", Priority = 1, Param = "WriteQualifiedName")]
                //[Variation(Id = 27, Desc = "Call WriteValue after Close()", Priority = 1, Param = "WriteValue")]
                //[Variation(Id = 28, Desc = "Call WriteAttributes after Close()", Priority = 1, Param = "WriteAttributes")]
                //[Variation(Id = 29, Desc = "Call WriteNode(nav) after Close()", Priority = 1, Param = "WriteNodeNavigator")]
                //[Variation(Id = 30, Desc = "Call WriteNode(reader) after Close()", Priority = 1, Param = "WriteNodeReader")]
                //[Variation(Id = 31, Desc = "Call Flush after Close()", Priority = 1, Param = "Flush")]
                public void writeState_9()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteElementString("root", "");  // line added by t-goranh
                    w.Dispose();
                    try
                    {
                        this.InvokeMethod(w, Variation.Param.ToString());
                    }
                    catch (InvalidOperationException)
                    {
                        return;
                    }
                    // Flush/LookupPrefix is a NOOP
                    if (Variation.Param.ToString() == "Flush" || Variation.Param.ToString() == "LookupPrefix")
                        return;

                    throw new TestException(TestResult.Failed, "");
                }

                private void InvokeMethod(XmlWriter w, string methodName)
                {
                    byte[] buffer = new byte[10];
                    switch (methodName)
                    {
                        case "WriteStartDocument":
                            w.WriteStartDocument();
                            break;
                        case "WriteStartElement":
                            w.WriteStartElement("root");
                            break;
                        case "WriteEndElement":
                            w.WriteEndElement();
                            break;
                        case "WriteStartAttribute":
                            w.WriteStartAttribute("attr");
                            break;
                        case "WriteEndAttribute":
                            w.WriteEndAttribute();
                            break;
                        case "WriteCData":
                            w.WriteCData("test");
                            break;
                        case "WriteComment":
                            w.WriteComment("test");
                            break;
                        case "WritePI":
                            w.WriteProcessingInstruction("name", "test");
                            break;
                        case "WriteEntityRef":
                            w.WriteEntityRef("e");
                            break;
                        case "WriteCharEntity":
                            w.WriteCharEntity('c');
                            break;
                        case "WriteSurrogateCharEntity":
                            w.WriteSurrogateCharEntity('\uDC00', '\uDBFF');
                            break;
                        case "WriteWhitespace":
                            w.WriteWhitespace(" ");
                            break;
                        case "WriteString":
                            w.WriteString("foo");
                            break;
                        case "WriteChars":
                            char[] charArray = new char[] { 'a', 'b', 'c', 'd' };
                            w.WriteChars(charArray, 0, 3);
                            break;
                        case "WriteRaw":
                            w.WriteRaw("<foo>bar</foo>");
                            break;
                        case "WriteBase64":
                            w.WriteBase64(buffer, 0, 9);
                            break;
                        case "WriteBinHex":
                            w.WriteBinHex(buffer, 0, 9);
                            break;
                        case "LookupPrefix":
                            string str = w.LookupPrefix("foo");
                            break;
                        case "WriteNmToken":
                            w.WriteNmToken("foo");
                            break;
                        case "WriteName":
                            w.WriteName("foo");
                            break;
                        case "WriteQualifiedName":
                            w.WriteQualifiedName("foo", "bar");
                            break;
                        case "WriteValue":
                            w.WriteValue(int.MaxValue);
                            break;
                        case "WriteAttributes":
                            XmlReader xr1 = XmlReader.Create(new StringReader("<root attr='test'/>"));
                            xr1.Read();
                            w.WriteAttributes(xr1, false);
                            break;
                        case "WriteNodeReader":
                            XmlReader xr2 = XmlReader.Create(new StringReader("<root/>"));
                            xr2.Read();
                            w.WriteNode(xr2, false);
                            break;
                        case "Flush":
                            w.Flush();
                            break;
                        default:
                            TestLog.Equals(false, "Unexpected param in testcase: {0}", methodName);
                            break;
                    }
                }
            }

            public partial class TC_NDP20_NewMethods : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "WriteElementString(prefix, name, ns, value) sanity test", Priority = 0)]
                public void var_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteElementString("foo", "elem", "bar", "test");

                    w.Dispose();

                    if (!CompareReader(doc, "<foo:elem xmlns:foo='bar'>test</foo:elem>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "WriteElementString(prefix = xml, ns = XML namespace)", Priority = 1)]
                public void var_2()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteElementString("xml", "elem", "http://www.w3.org/XML/1998/namespace", "test");

                    w.Dispose();

                    if (!CompareReader(doc, "<xml:elem>test</xml:elem>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "WriteStartAttribute(string name) sanity test", Priority = 0)]
                public void var_3()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("elem");
                    w.WriteStartAttribute("attr");
                    w.WriteEndElement();

                    w.Dispose();

                    if (!CompareReader(doc, "<elem attr='' />")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 4, Desc = "WriteElementString followed by attribute should error", Priority = 1)]
                public void var_4()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteElementString("foo", "elem", "bar", "test");
                            w.WriteStartAttribute("attr");
                        }
                        catch (InvalidOperationException)
                        {
                            return;
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 5, Desc = "429445: XmlWellformedWriter wrapping another XmlWriter should check the duplicate attributes first", Priority = 1)]
                public void var_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter wf = CreateWriter(doc))
                    {
                        using (XmlWriter w = XmlWriter.Create(wf))
                        {
                            w.WriteStartElement("B");
                            w.WriteStartAttribute("aaa");
                            try
                            {
                                w.WriteStartAttribute("aaa");
                            }
                            catch (XmlException)
                            {
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCGlobalization : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Characters between 0xdfff and 0xfffe are valid Unicode characters", Priority = 1)]
                public void var_1()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    string UniStr = "";

                    for (char ch = '\ue000'; ch < '\ufffe'; ch++) UniStr += ch;
                    w.WriteElementString("root", UniStr);
                    w.Dispose();

                    if (!CompareReader(doc, "<root>" + UniStr + "</root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "XmlWriter using UTF-16BE encoding writes out wrong encoding name value in the xml decl", Priority = 1)]
                public void var_2()
                {
                    Encoding enc = Encoding.GetEncoding("UTF-16BE");

                    Stream s = new MemoryStream();
                    byte[] preamble = enc.GetPreamble();
                    s.Write(preamble, 0, preamble.Length);
                    s.Flush();
                    StreamWriter sw = new StreamWriter(s, enc);
                    using (XmlWriter xw = XmlWriter.Create(sw))
                    {
                        xw.WriteStartDocument();
                        xw.WriteElementString("A", "value");
                        xw.WriteEndDocument();
                    }
                    sw.Flush();
                    if (s.CanSeek)
                    {
                        s.Position = 0;
                    }
                    StreamReader sr = new StreamReader(s);
                    string str = sr.ReadToEnd();

                    if (!(str == "<?xml version=\"1.0\" encoding=\"utf-16BE\"?><A>value</A>")) throw new TestException(TestResult.Failed, "");
                }
            }

            public partial class TCClose : BridgeHelpers
            {
                //[Variation(Id = 1, Desc = "Closing an XmlWriter should close all opened elements", Priority = 1)]
                public void var_1()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter writer = CreateWriter(doc))
                    {
                        writer.WriteStartElement("Root");
                        writer.WriteStartElement("Nesting");
                        writer.WriteStartElement("SomeDeep");
                        writer.Dispose();
                    }

                    if (!CompareReader(doc, "<Root><Nesting><SomeDeep/></Nesting></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 2, Desc = "Disposing an XmlWriter should close all opened elements", Priority = 1)]
                public void var_2()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter writer = CreateWriter(doc))
                    {
                        writer.WriteStartElement("Root");
                        writer.WriteStartElement("Nesting");
                        writer.WriteStartElement("SomeDeep");
                    }

                    if (!CompareReader(doc, "<Root><Nesting><SomeDeep/></Nesting></Root>")) throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Id = 3, Desc = "Dispose() shouldn't throw when a tag is not closed and inner stream is closed", Priority = 1)]
                public void var_3()
                {
                    StringWriter sw = new StringWriter();
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.WriteStartElement("root");

                    ((IDisposable)sw).Dispose();
                    sw = null;
                    ((IDisposable)w).Dispose();
                }

                //[Variation(Id = 4, Desc = "Close() should be allowed when XML doesn't have content", Priority = 1)]
                public void var_4()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    w.Dispose();

                    try
                    {
                        CompareReader(doc, "");
                    }
                    catch (XmlException e)
                    {
                        if (e.Message.EndsWith(".."))
                        {
                            throw new TestException(TestResult.Failed, "");
                        }
                    }
                }
            }

            // Helper method
            protected static XmlReader MoveToFirstElement(XmlReader reader)
            {
                while (reader.Read() && reader.NodeType != XmlNodeType.Element)
                {
                    // nop
                }

                return reader;
            }
        }
    }
}
