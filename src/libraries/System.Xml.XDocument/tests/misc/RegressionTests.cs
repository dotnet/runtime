// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XmlDiff;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class MiscTests : XLinqTestCase
        {
            public partial class RegressionTests : XLinqTestCase
            {
                //[Variation(Desc = "pi.Target = '' should not be allowed")]
                public void XPIEmptyStringShouldNotBeAllowed()
                {
                    XProcessingInstruction pi = new XProcessingInstruction("PI", "data");
                    try
                    {
                        pi.Target = "";
                    }
                    catch (ArgumentException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "Removing mixed content")]
                public void RemovingMixedContent()
                {
                    XElement a = XElement.Parse(@"<A>t1<B/>t2</A>");
                    a.Nodes().Skip(1).Remove();
                    TestLog.Compare(a.ToString(SaveOptions.DisableFormatting), @"<A>t1</A>", "a.Xml");
                }
                //[Variation(Desc = "Cannot parse DTD")]
                public void CannotParseDTD()
                {
                    string xml = "<!DOCTYPE x []><x/>";
                    XElement e = XElement.Parse(xml);
                    TestLog.Compare(e.ToString(SaveOptions.DisableFormatting), "<x />", "e.Xml");
                }

                //[Variation(Desc = "Replace content")]
                public void ReplaceContent()
                {
                    XElement a = XElement.Parse("<A><B><C/></B></A>");
                    a.Element("B").ReplaceNodes(a.Nodes());
                    XElement x = a;
                    foreach (string s in (new string[] { "A", "B", "B" }))
                    {
                        TestLog.Compare(x.Name.LocalName, s, s);
                        x = x.FirstNode as XElement;
                    }
                }

                //[Variation(Desc = "It is possible to add duplicate namespace declaration.")]
                public void DuplicateNamespaceDeclarationIsAllowed()
                {
                    try
                    {
                        XElement element = XElement.Parse("<A xmlns:p='ns'/>");
                        element.Add(new XAttribute(XNamespace.Xmlns + "p", "ns"));
                    }
                    catch (InvalidOperationException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "Manually declared prefix-namespace pair is not reflected in the XElement serialization")]
                public void ManuallyDeclaredPrefixNamespacePairIsNotReflectedInTheXElementSerialization()
                {
                    var element = XElement.Parse("<A/>");
                    element.Add(new XAttribute(XNamespace.Xmlns + "p", "ns"));
                    element.Add(new XElement("{ns}B", null));
                    MemoryStream sourceStream = new MemoryStream();
                    element.Save(sourceStream);
                    sourceStream.Position = 0;
                    // creating the following element with expected output so we can compare
                    XElement target = XElement.Parse("<A xmlns:p=\"ns\"><p:B /></A>");
                    MemoryStream targetStream = new MemoryStream();
                    target.Save(targetStream);
                    targetStream.Position = 0;
                    XmlDiff diff = new XmlDiff();
                    if (!diff.Compare(sourceStream, targetStream))
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "XName.Get(string, string) - for NULL, NULL :: wrong exception param")]
                public void XNameGetDoesThrowWhenPassingNulls1()
                {
                    try
                    {
                        XName.Get(null, null);
                    }
                    catch (System.ArgumentNullException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "XName.Get(string, string) - for NULL, string :: wrong exception param")]
                public void XNameGetDoesThrowWhenPassingNulls2()
                {
                    try
                    {
                        XName.Get(null, "MyName");
                    }
                    catch (System.ArgumentNullException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "'Hashing name parts should be same as hashing expanded name' when using namespaces")]
                public void HashingNamePartsShouldBeSameAsHashingExpandedNameWhenUsingNamespaces()
                {
                    try
                    {
                        XElement element1 = new XElement(XName.Get("e1", "ns1"), "e1 should be in \"ns1\"",
                            new XElement(XName.Get("e2", "ns-default1"), "e2 should be in ns-default1",
                            new XElement(XName.Get("e3", "ns-default2"), "e3 should be in ns-default2",
                            new XElement(XName.Get("e4", "ns2"), "e4 should be in ns2"))));
                    }
                    catch (Exception)
                    {
                        throw new TestException(TestResult.Failed, "");
                    }
                }

                //[Variation(Desc = "Creating new XElements passing null reader and/or null XName should throw ArgumentNullException")]
                public void CreatingNewXElementsPassingNullReaderAndOrNullXNameShouldThrow()
                {
                    try
                    {
                        var x = new XElement((XName)null);
                        x = (XElement)XNode.ReadFrom((XmlReader)null);
                    }
                    catch (ArgumentNullException)
                    {
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "XNode.AddBeforeSelf('text') - prepending the text node to the text node does disconnect the original node.")]
                public void XNodeAddBeforeSelfPrependingTextNodeToTextNodeDoesDisconnectTheOriginalNode()
                {
                    XElement e = new XElement("e1",
                        new XElement("e2"), "text1",
                        new XElement("e3"));
                    XNode t = e.FirstNode.NextNode;
                    t.AddBeforeSelf("text2");
                    t.AddBeforeSelf("text3");
                    TestLog.Compare(e.Value.Equals("text2text3text1"), e.Value);
                }

                //[Variation(Desc = "ReadSubtree () on the XReader throws NullRefException")]
                public void ReadSubtreeOnXReaderThrows()
                {
                    XElement xe = new XElement("root", new XElement("A", new XElement("B", "data")),
                        new XProcessingInstruction("PI", "joke"));

                    using (XmlReader r = xe.CreateReader())
                    {
                        r.Read();
                        r.Read();
                        using (XmlReader subR = r.ReadSubtree())
                        {
                            subR.Read();
                        }
                    }
                }

                //[Variation(Desc = "stack overflow for deep nesting")]
                public void StackOverflowForDeepNesting()
                {
                    StringBuilder sb = new StringBuilder();

                    for (long l = 0; l < 6600; l++) sb.Append("<A>");
                    sb.Append("<A/>");
                    for (long l = 0; l < 6600; l++) sb.Append("</A>");
                    XElement e = XElement.Parse(sb.ToString());
                }

                //[Variation(Desc = "The Empty CData text node is not preserved in the tree.")]
                public void EmptyCDataTextNodeIsNotPreservedInTheTree()
                {
                    XDocument d = XDocument.Parse("<root><![CDATA[]]></root>");
                    TestLog.Compare(d.Element("root").Nodes().Count(), 1, "Node missing");
                    TestLog.Compare(d.Root.FirstNode is XCData, "node type");
                    TestLog.Compare((d.Root.FirstNode as XCData).Value, "", "node value");
                }

                //[Variation(Desc = "XDocument.ToString() throw exception for the XDocument containing whitespace node only")]
                public void XDocumentToStringThrowsForXDocumentContainingOnlyWhitespaceNodes()
                {
                    XDocument d = new XDocument();
                    d.Add(" ");
                    string s = d.ToString();
                }

                //[Variation(Desc = "Nametable returns an incorrect XNamespace")]
                public void NametableReturnsIncorrectXNamespace()
                {
                    XNamespace ns = XNamespace.Get("h");
                    TestLog.Compare(!object.ReferenceEquals(ns, XNamespace.Xml), "XNamespace.Get('h') returns Xml namespace");
                }

                //[Variation(Desc = "Xml Namespace serialization")]
                public void XmlNamespaceSerialization()
                {
                    try
                    {
                        XElement e =
                            new XElement("a", new XAttribute(XNamespace.Xmlns.GetName("ns"), "def"),
                                new XElement("b",
                                    new XAttribute(XNamespace.Xmlns.GetName("ns1"), "def"),
                                    new XElement("{def}c",
                                        new XAttribute(XNamespace.Xmlns.GetName("ns1"), "abc"))));
                    }
                    catch (System.Xml.XmlException e)
                    {
                        throw new TestFailedException(e.ToString());
                    }
                }

                //[Variation(Desc = "Tuple - New Dev10 Types", Param = 1)]
                //[Variation(Desc = "DynamicObject - New Dev10 Types", Param = 2)]
                //[Variation(Desc = "Guid - old type", Param = 3)]
                //[Variation(Desc = "Dictionary - old type", Param = 4)]
                public void CreatingXElementsFromNewDev10Types()
                {
                    object t = null;
                    Type type = typeof(object);
                    int param = (int)this.Variation.Param;
                    switch (param)
                    {
                        case 1: t = Tuple.Create(1, "Melitta", 7.5); type = typeof(Tuple); break;
                        case 3: t = new Guid(); type = typeof(Guid); break;
                        case 4: t = new Dictionary<int, string>(); ((Dictionary<int, string>)t).Add(7, "a"); type = typeof(Dictionary<int, string>); break;
                    }

                    XElement e = new XElement("e1",
                        new XElement("e2"), "text1",
                        new XElement("e3"), t);
                    e.Add(t);
                    e.FirstNode.ReplaceWith(t);

                    XNode n = e.FirstNode.NextNode;
                    n.AddBeforeSelf(t);
                    n.AddAnnotation(t);
                    n.ReplaceWith(t);

                    e.FirstNode.AddAfterSelf(t);
                    e.AddFirst(t);
                    e.Annotation(type);
                    e.Annotations(type);
                    e.RemoveAnnotations(type);
                    e.ReplaceAll(t);
                    e.ReplaceAttributes(t);
                    e.ReplaceNodes(t);
                    e.SetAttributeValue("a", t);
                    e.SetElementValue("e2", t);
                    e.SetValue(t);

                    XAttribute a = new XAttribute("a", t);
                    XStreamingElement se = new XStreamingElement("se", t);
                    se.Add(t);

                    try
                    {
                        new XDocument(t);
                    }
                    catch (ArgumentException)
                    {
                        try
                        {
                            new XDocument(t);
                        }
                        catch (ArgumentException)
                        {
                            return;
                        }
                    }
                    TestLog.Compare(false, "Failed");
                }
            }
        }
    }
}