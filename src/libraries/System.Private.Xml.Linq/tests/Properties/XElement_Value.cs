// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class PropertiesTests : XLinqTestCase
        {
            //[TestCase(Name = "XElement.Name", Params = new object[] { false })]
            //[TestCase(Name = "XElement.Name with Events", Params = new object[] { true })]
            public partial class XElementName : XLinqTestCase
            {
                private EventsHelper _eHelper;
                private bool _runWithEvents;

                //[Variation(Priority = 0, Desc = "XElement - same name", Params = new object[] { "<element>value</element>", "element" })]
                //[Variation(Priority = 0, Desc = "XElement - different name", Params = new object[] { "<element>value</element>", "newElement" })]
                //[Variation(Priority = 0, Desc = "XElement - name with namespace", Params = new object[] { "<element>value</element>", "{a}newElement" })]
                //[Variation(Priority = 0, Desc = "XElement - name with xml namespace", Params = new object[] { "<element>value</element>", "{http://www.w3.org/XML/1998/namespace}newElement" })]
                //[Variation(Priority = 0, Desc = "XElement - element with namespace", Params = new object[] { "<p:element xmlns:p='mynamespace'><p:child>value</p:child></p:element>", "{a}newElement" })]
                public void ValidVariation()
                {
                    _runWithEvents = (bool)Params[0];
                    string xml = Variation.Params[0] as string;
                    XElement toChange = XElement.Parse(xml);
                    XName newName = Variation.Params[1] as string;
                    if (_runWithEvents) _eHelper = new EventsHelper(toChange);
                    toChange.Name = newName;
                    if (_runWithEvents) _eHelper.Verify(XObjectChange.Name);
                    TestLog.Compare(newName.Namespace == toChange.Name.Namespace, "Namespace did not change");
                    TestLog.Compare(newName.LocalName == toChange.Name.LocalName, "LocalName did not change");
                }

                //[Variation(Priority = 0, Desc = "XElement - space character name", Params = new object[] { "<element>value</element>", " " })]
                //[Variation(Priority = 0, Desc = "XElement - empty string name", Params = new object[] { "<element>value</element>", "" })]
                //[Variation(Priority = 0, Desc = "XElement - null name", Params = new object[] { "<element>value</element>", null })]
                public void InValidVariation()
                {
                    _runWithEvents = (bool)Params[0];
                    string xml = Variation.Params[0] as string;
                    XElement toChange = XElement.Parse(xml);
                    try
                    {
                        if (_runWithEvents) _eHelper = new EventsHelper(toChange);
                        toChange.Name = Variation.Params[1] as string;
                    }
                    catch (Exception)
                    {
                        if (_runWithEvents) _eHelper.Verify(0);
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 0, Desc = "XProcessingInstruction - Valid Name")]
                public void ValidPIVariation()
                {
                    _runWithEvents = (bool)Params[0];
                    XProcessingInstruction toChange = new XProcessingInstruction("target", "data");
                    if (_runWithEvents) _eHelper = new EventsHelper(toChange);
                    toChange.Target = "newTarget";
                    if (_runWithEvents) _eHelper.Verify(XObjectChange.Name);
                    TestLog.Compare(toChange.Target.Equals("newTarget"), "Name did not change");
                }

                //[Variation(Priority = 0, Desc = "XProcessingInstruction - Invalid Name")]
                public void InvalidPIVariation()
                {
                    _runWithEvents = (bool)Params[0];
                    XProcessingInstruction toChange = new XProcessingInstruction("target", "data");
                    if (_runWithEvents) _eHelper = new EventsHelper(toChange);

                    try
                    {
                        toChange.Target = null;
                    }
                    catch (Exception)
                    {
                        if (_runWithEvents) _eHelper.Verify(0);
                        return;
                    }

                    try
                    {
                        toChange.Target = " ";
                    }
                    catch (Exception)
                    {
                        if (_runWithEvents) _eHelper.Verify(0);

                        return;
                    }

                    try
                    {
                        toChange.Target = "";
                    }
                    catch (Exception)
                    {
                        if (_runWithEvents) _eHelper.Verify(0);
                        return;
                    }

                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 0, Desc = "XDocumentType - Valid Name")]
                public void ValidDocTypeVariation()
                {
                    _runWithEvents = (bool)Params[0];
                    XDocumentType toChange = new XDocumentType("root", "", "", "");
                    if (_runWithEvents) _eHelper = new EventsHelper(toChange);
                    toChange.Name = "newName";
                    if (_runWithEvents) _eHelper.Verify(XObjectChange.Name);
                    TestLog.Compare(toChange.Name.Equals("newName"), "Name did not change");
                }

                //[Variation(Priority = 0, Desc = "XDocumentType - Invalid Name")]
                public void InvalidDocTypeVariation()
                {
                    _runWithEvents = (bool)Params[0];
                    XDocumentType toChange = new XDocumentType("root", "", "", "");
                    if (_runWithEvents) _eHelper = new EventsHelper(toChange);

                    try
                    {
                        toChange.Name = null;
                    }
                    catch (Exception)
                    {
                        if (_runWithEvents) _eHelper.Verify(0);
                        return;
                    }

                    try
                    {
                        toChange.Name = " ";
                    }
                    catch (Exception)
                    {
                        if (_runWithEvents) _eHelper.Verify(0);
                        return;
                    }

                    try
                    {
                        toChange.Name = "";
                    }
                    catch (Exception)
                    {
                        if (_runWithEvents) _eHelper.Verify(0);
                        return;
                    }
                }
            }

            //[TestCase(Name = "XElement.Value", Params = new object[] { false })]
            //[TestCase(Name = "XElement.Value with Events", Params = new object[] { true })]
            public partial class XElementValue : XLinqTestCase
            {
                private EventsHelper _eHelper;
                private bool _runWithEvents;

                // GET: 
                // - no content
                // - empty string content
                // - child nodes (different namespaces)
                //  ~ no text nodes (value = "")
                //  ~ empty text nodes
                //  ~ text nodes in mixed content
                //  ~ whitespace nodes

                //[Variation(Priority = 0, Desc = "GET: Mixed content", Params = new object[] { "<X>t0<A>t1<B/><B xmlns='a'><![CDATA[t2]]></B><C>t3</C></A>t00</X>" })]
                //[Variation(Priority = 1, Desc = "GET: Mixed content - empty CDATA", Params = new object[] { "<X>t0<A>t1<B/><B xmlns='a'><![CDATA[]]></B><C>t3</C></A>t00</X>" })]
                //[Variation(Priority = 1, Desc = "GET: Mixed content - empty XText", Params = new object[] { "<X>t0<A>t1<B/><B xmlns='a'><![CDATA[]]></B><C></C></A>t00</X>" })]
                //[Variation(Priority = 1, Desc = "GET: Mixed content - whitespace", Params = new object[] { "<X>t0<A>t1\n<B/><B xmlns='a'>\t<![CDATA[]]> </B>\n<C></C></A>t00</X>" })]
                //[Variation(Priority = 1, Desc = "GET: Mixed content - no text nodes", Params = new object[] { "<X>t0<A Id='a0'><B/><B xmlns='a'><?Pi c?></B><!--commm--><C></C></A>t00</X>" })]
                //[Variation(Priority = 0, Desc = "GET: Empty string node", Params = new object[] { "<X>t0<A></A>t00</X>" })]
                //[Variation(Priority = 0, Desc = "GET: Empty string node", Params = new object[] { "<X>t0<A/>t00</X>" })]
                //[Variation(Priority = 0, Desc = "GET: String content", Params = new object[] { "<X>t0<A>truck</A>t00</X>" })]
                public void SmokeTest()
                {
                    // We don't need get value to be executed for events
                    if ((bool)Params[0])
                        TestLog.Skip("We don't need get value to be executed for events");
                    //throw new TestException(TestResult.Skipped, "");

                    string xml = Variation.Params[0] as string;
                    XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace).Element("A");
                    TestLog.Compare(e.Value, CalculateValue(e), "Value");
                }

                private string CalculateValue(XElement elem)
                {
                    StringBuilder value = new StringBuilder();
                    foreach (XNode n in elem.Nodes())
                    {
                        if (n is XText)
                        {
                            value.Append((n as XText).Value);
                        }
                        if (n is XElement)
                        {
                            value.Append(CalculateValue(n as XElement));
                        }
                    }
                    return value.ToString();
                }

                //  ---
                //  ~ API touched data and Value (sanity)
                //      ~ adjacent following text nodes
                //      ~ concatenated text node value
                //      ~ removed 
                //          ~ text node 
                //          ~ non text node
                //          ~ set value on subnodes

                //[Variation(Priority = 1, Desc = "GET: Adjacent text nodes I.", Params = new object[] { "<X>t0<A>truck</A>t00</X>" })]
                //[Variation(Priority = 1, Desc = "GET: Adjacent text nodes II.", Params = new object[] { "<X>t0<A xmlns:p='p'>truck<p:Y/></A>t00</X>" })]
                public void APIModified1()
                {
                    // We don't need get value to be executed for events
                    if ((bool)Params[0])
                        TestLog.Skip("We don't need get value to be executed for events");
                    //throw new TestException(TestResult.Skipped, "");

                    string xml = Variation.Params[0] as string;
                    XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace).Element("A");
                    e.FirstNode.AddAfterSelf(new XText("hello"));
                    TestLog.Compare(e.Value, CalculateValue(e), "Value");
                }

                //[Variation(Priority = 1, Desc = "GET: Adjacent text nodes III.", Params = new object[] { "<X>t0<A xmlns:p='p'>truck\n<p:Y/>\nhello</A>t00</X>" })]
                public void APIModified2()
                {
                    // We don't need get value to be executed for events
                    if ((bool)Params[0])
                        TestLog.Skip("We don't need get value to be executed for events");
                    //throw new TestException(TestResult.Skipped, "");

                    string xml = Variation.Params[0] as string;
                    XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace).Element("A");
                    e.FirstNode.NextNode.Remove();
                    TestLog.Compare(e.Value, CalculateValue(e), "Value");
                }

                //[Variation(Priority = 1, Desc = "GET: Concatenated text I.", Params = new object[] { "<X>t0<A>truck</A>t00</X>" })]
                //[Variation(Priority = 1, Desc = "GET: Concatenated text II.", Params = new object[] { "<X>t0<A xmlns:p='p'>truck<p:Y/></A>t00</X>" })]
                public void APIModified3()
                {
                    // We don't need get value to be executed for events
                    if ((bool)Params[0])
                        TestLog.Skip("We don't need get value to be executed for events");
                    //throw new TestException(TestResult.Skipped, "");

                    string xml = Variation.Params[0] as string;
                    XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace).Element("A");
                    e.FirstNode.AddAfterSelf("hello");
                    TestLog.Compare(e.Value, CalculateValue(e), "Value");
                }

                //[Variation(Priority = 1, Desc = "GET: Removed node.", Params = new object[] { "<X>t0<A xmlns:p='p'>truck\n<p:Y/>\nhello</A>t00</X>" })]
                public void APIModified4()
                {
                    // We don't need get value to be executed for events
                    if ((bool)Params[0])
                        TestLog.Skip("We don't need get value to be executed for events");
                    //throw new TestException(TestResult.Skipped, "");

                    string xml = Variation.Params[0] as string;
                    XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace).Element("A");
                    e.LastNode.Remove();
                    TestLog.Compare(e.Value, CalculateValue(e), "Value");
                }


                // ======================================
                // SET:
                //  ~ set null
                //  ~ set empty string
                //  ~ set string 
                //  :: verify the replaced nodes properties, node content, node value
                //  ~ replace:
                //      ~ no content
                //      ~ empty string
                //      ~ just string 
                //      ---- from here verify replaced nodes content
                //      ~ just text/CDATA node
                //      ~ mixed content

                //[Variation(Priority = 0, Desc = "SET: Empty element, String content", Params = new object[] { "<X>t0<A/>t00</X>", "\nt1 " })]
                //[Variation(Priority = 1, Desc = "SET: Empty element, Empty string content", Params = new object[] { "<X>t0<A/>t00</X>", "" })]
                //[Variation(Priority = 1, Desc = "SET: Empty string content, String content", Params = new object[] { "<X>t0<A></A>t00</X>", "\nt1 " })]
                //[Variation(Priority = 1, Desc = "SET: Empty string content, Empty string content", Params = new object[] { "<X>t0<A></A>t00</X>", "" })]
                //[Variation(Priority = 1, Desc = "SET: String content, String content", Params = new object[] { "<X>t0<A>orig</A>t00</X>", "\nt1 " })]
                //[Variation(Priority = 1, Desc = "SET: String content, Empty string content", Params = new object[] { "<X>t0<A>orig</A>t00</X>", "" })]
                public void Value_Set()
                {
                    _runWithEvents = (bool)Params[0];
                    string xml = Variation.Params[0] as string;
                    string newVal = Variation.Params[1] as string;

                    XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace).Element("A");
                    if (_runWithEvents) _eHelper = new EventsHelper(e);
                    e.Value = newVal;
                    // Not sure how to verify this yet( what possible events and in what order)
                    //if (runWithEvents) eHelper.Verify(XObjectChange.Value);

                    TestLog.Compare(e.Value, newVal, "value");
                    TestLog.Compare(e.Parent.Value, "t0" + newVal + "t00", "parent value");
                    TestLog.Compare(!e.IsEmpty, "!e.IsEmpty");
                }

                //[Variation(Priority = 1, Desc = "SET:  XText content, Empty string content", Params = new object[] { "<X>t0<A xml:space='preserve'>orig</A>t00</X>", "" })]
                //[Variation(Priority = 1, Desc = "SET:  XText content, string content", Params = new object[] { "<X>t0<A>orig</A>t00</X>", "\tt1 " })]
                //[Variation(Priority = 1, Desc = "SET:  CDATA content, Empty string content", Params = new object[] { "<X>t0<A><![CDATA[cdata]]></A>t00</X>", "" })]
                //[Variation(Priority = 1, Desc = "SET:  CDATA content, string content", Params = new object[] { "<X>t0<A><![CDATA[cdata]]></A>t00</X>", "\tt1 " })]
                //[Variation(Priority = 1, Desc = "SET:  Mixed content, Empty string content", Params = new object[] { "<X>t0<A xmlns:p='p'>t1<p:Y/></A>t00</X>", "" })]
                //[Variation(Priority = 0, Desc = "SET:  Mixed content, string content", Params = new object[] { "<X>t0<A is='is'><![CDATA[cdata]]>orig<C/><!--comment--></A>t00</X>", "\tt1 " })]
                //[Variation(Priority = 1, Desc = "SET:  Mixed content (comment only), string content", Params = new object[] { "<X>t0<A is='is'><!--comment--></A>t00</X>", "\tt1 " })]
                //[Variation(Priority = 1, Desc = "SET:  Mixed content (PI only), string content", Params = new object[] { "<X>t0<A is='is'><?PI aaa?></A>t00</X>", "\tt1 " })]
                public void Value_Set_WithNodes()
                {
                    _runWithEvents = (bool)Params[0];
                    string xml = Variation.Params[0] as string;
                    string newVal = Variation.Params[1] as string;

                    XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace).Element("A");
                    var origNodes = e.Nodes().ToList();
                    var origAttributes = e.Attributes().ToList();

                    if (_runWithEvents) _eHelper = new EventsHelper(e);
                    e.Value = newVal;
                    // Not sure how to verify this yet( what possible events and in what order)
                    //if (runWithEvents) eHelper.Verify(XObjectChange.Value);

                    TestLog.Compare(e.Value, newVal, "value");
                    TestLog.Compare(e.Parent.Value, "t0" + newVal + "t00", "parent value");
                    TestLog.Compare(!e.IsEmpty, "!e.IsEmpty");
                    TestLog.Compare(origNodes.Where(n => n.Parent != null || n.Document != null).IsEmpty(), "origNodes.Where(n=>n.Parent!=null || n.Document!=null).IsEmpty()");
                    TestLog.Compare(origAttributes.SequenceEqual(e.Attributes()), "origAttributes.SequenceEqual(e.Attributes())");
                }

                //[Variation(Priority = 2, Desc = "SET: Adjacent text nodes I.", Params = new object[] { "<X>t0<A>truck</A>t00</X>", "tn\n" })]
                //[Variation(Priority = 2, Desc = "SET: Adjacent text nodes II.", Params = new object[] { "<X>t0<A xmlns:p='p'>truck<p:Y/></A>t00</X>", "tn\n" })]
                public void set_APIModified1()
                {
                    _runWithEvents = (bool)Params[0];
                    string xml = Variation.Params[0] as string;
                    string newVal = Variation.Params[1] as string;

                    XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace).Element("A");
                    e.FirstNode.AddAfterSelf(new XText("hello"));

                    var origNodes = e.Nodes().ToList();
                    var origAttributes = e.Attributes().ToList();

                    if (_runWithEvents) _eHelper = new EventsHelper(e);
                    e.Value = newVal;
                    // Not sure how to verify this yet( what possible events and in what order)
                    //if (runWithEvents) eHelper.Verify(XObjectChange.Value);

                    TestLog.Compare(e.Value, newVal, "value");
                    TestLog.Compare(e.Parent.Value, "t0" + newVal + "t00", "parent value");
                    TestLog.Compare(!e.IsEmpty, "!e.IsEmpty");
                    TestLog.Compare(origNodes.Where(n => n.Parent != null || n.Document != null).IsEmpty(), "origNodes.Where(n=>n.Parent!=null || n.Document!=null).IsEmpty()");
                    TestLog.Compare(origAttributes.SequenceEqual(e.Attributes()), "origAttributes.SequenceEqual(e.Attributes())");
                }

                //[Variation(Priority = 2, Desc = "SET: Adjacent text nodes III.", Params = new object[] { "<X>t0<A xmlns:p='p'>truck\n<p:Y/>\nhello</A>t00</X>", "tn\n" })]
                public void set_APIModified2()
                {
                    _runWithEvents = (bool)Params[0];
                    string xml = Variation.Params[0] as string;
                    string newVal = Variation.Params[1] as string;

                    XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace).Element("A");
                    e.FirstNode.NextNode.Remove();

                    var origNodes = e.Nodes().ToList();
                    var origAttributes = e.Attributes().ToList();

                    if (_runWithEvents) _eHelper = new EventsHelper(e);
                    e.Value = newVal;
                    // Not sure how to verify this yet( what possible events and in what order)
                    //if (runWithEvents) eHelper.Verify(XObjectChange.Value);

                    TestLog.Compare(e.Value, newVal, "value");
                    TestLog.Compare(e.Parent.Value, "t0" + newVal + "t00", "parent value");
                    TestLog.Compare(!e.IsEmpty, "!e.IsEmpty");
                    TestLog.Compare(origNodes.Where(n => n.Parent != null || n.Document != null).IsEmpty(), "origNodes.Where(n=>n.Parent!=null || n.Document!=null).IsEmpty()");
                    TestLog.Compare(origAttributes.SequenceEqual(e.Attributes()), "origAttributes.SequenceEqual(e.Attributes())");
                }

                //[Variation(Priority = 2, Desc = "SET: Concatenated text I.", Params = new object[] { "<X>t0<A>truck</A>t00</X>", "tn\n" })]
                //[Variation(Priority = 2, Desc = "SET: Concatenated text II.", Params = new object[] { "<X>t0<A xmlns:p='p'>truck<p:Y/></A>t00</X>", "tn\n" })]
                public void set_APIModified3()
                {
                    _runWithEvents = (bool)Params[0];
                    string xml = Variation.Params[0] as string;
                    string newVal = Variation.Params[1] as string;

                    XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace).Element("A");
                    e.FirstNode.AddAfterSelf("hello");

                    var origNodes = e.Nodes().ToList();
                    var origAttributes = e.Attributes().ToList();

                    if (_runWithEvents) _eHelper = new EventsHelper(e);
                    e.Value = newVal;
                    // Not sure how to verify this yet( what possible events and in what order)
                    //if (runWithEvents) eHelper.Verify(XObjectChange.Value);

                    TestLog.Compare(e.Value, newVal, "value");
                    TestLog.Compare(e.Parent.Value, "t0" + newVal + "t00", "parent value");
                    TestLog.Compare(!e.IsEmpty, "!e.IsEmpty");
                    TestLog.Compare(origNodes.Where(n => n.Parent != null || n.Document != null).IsEmpty(), "origNodes.Where(n=>n.Parent!=null || n.Document!=null).IsEmpty()");
                    TestLog.Compare(origAttributes.SequenceEqual(e.Attributes()), "origAttributes.SequenceEqual(e.Attributes())");
                }

                //[Variation(Priority = 2, Desc = "SET: Removed node.", Params = new object[] { "<X>t0<A xmlns:p='p'>truck\n<p:Y/>\nhello</A>t00</X>", "tn\n" })]
                public void set_APIModified4()
                {
                    _runWithEvents = (bool)Params[0];
                    string xml = Variation.Params[0] as string;
                    string newVal = Variation.Params[1] as string;

                    XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace).Element("A");
                    e.LastNode.Remove();

                    var origNodes = e.Nodes().ToList();
                    var origAttributes = e.Attributes().ToList();

                    if (_runWithEvents) _eHelper = new EventsHelper(e);
                    e.Value = newVal;
                    // Not sure how to verify this yet( what possible events and in what order)
                    //if (runWithEvents) eHelper.Verify(XObjectChange.Value);

                    TestLog.Compare(e.Value, newVal, "value");
                    TestLog.Compare(e.Parent.Value, "t0" + newVal + "t00", "parent value");
                    TestLog.Compare(!e.IsEmpty, "!e.IsEmpty");
                    TestLog.Compare(origNodes.Where(n => n.Parent != null || n.Document != null).IsEmpty(), "origNodes.Where(n=>n.Parent!=null || n.Document!=null).IsEmpty()");
                    TestLog.Compare(origAttributes.SequenceEqual(e.Attributes()), "origAttributes.SequenceEqual(e.Attributes())");
                }
            }
        }
    }
}
