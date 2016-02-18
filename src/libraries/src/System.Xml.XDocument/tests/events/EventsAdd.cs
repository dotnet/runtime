// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace CoreXml.Test.XLinq.FunctionalTests.EventsTests
{
    public class EventsAddBeforeSelf
    {
        public static object[][] ExecuteXDocumentVariationParams = new object[][] {
            new object[] { new XNode[] { new XElement("element") }, new XComment("Comment") },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data") },
            new object[] { new XNode[] { new XDocumentType("root", "", "", "") }, new XElement("root") },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" ") },
            new object[] { new XNode[] { new XComment("Comment") }, new XDocumentType("root", "", "", "") },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" ") }
        };
        [Theory, MemberData(nameof(ExecuteXDocumentVariationParams))]
        public void ExecuteXDocumentVariation(XNode[] toAdd, XNode contextNode)
        {
            IEnumerable<XNode> toAddList = toAdd.OfType<XNode>();
            XDocument xDoc = new XDocument(contextNode);
            XDocument xDocOriginal = new XDocument(xDoc);
            using (UndoManager undo = new UndoManager(xDoc))
            {
                undo.Group();
                using (EventsHelper docHelper = new EventsHelper(xDoc))
                {
                    using (EventsHelper nodeHelper = new EventsHelper(contextNode))
                    {
                        contextNode.AddBeforeSelf(toAdd);
                        Assert.True(toAddList.SequenceEqual(contextNode.NodesBeforeSelf(), XNode.EqualityComparer), "Nodes not added correctly!");
                        nodeHelper.Verify(0);
                    }
                    docHelper.Verify(XObjectChange.Add, toAdd);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        public static object[][] ExecuteXElementVariationParams = new object[][] {
            new object[] { new XNode[] { new XElement("element") }, new XText("some text") },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data") },
            new object[] { new XNode[] { new XCData("x+y >= z-m") }, new XElement("child") },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" ") },
            new object[] { new XNode[] { new XComment("Comment") }, new XCData("x+y >= z-m") },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" ") },
            new object[] { InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), new XText("..") }
        };
        [Theory, MemberData(nameof(ExecuteXElementVariationParams))]
        public void ExecuteXElementVariation(XNode[] toAdd, XNode contextNode)
        {
            IEnumerable<XNode> toAddList = toAdd.OfType<XNode>();
            XElement xElem = new XElement("root", contextNode);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper elemHelper = new EventsHelper(xElem))
                {
                    using (EventsHelper nodeHelper = new EventsHelper(contextNode))
                    {
                        contextNode.AddBeforeSelf(toAdd);
                        Assert.True(toAddList.SequenceEqual(contextNode.NodesBeforeSelf(), XNode.EqualityComparer), "Nodes not added correctly!");
                        nodeHelper.Verify(0);
                    }
                    elemHelper.Verify(XObjectChange.Add, toAdd);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }

        [Fact]
        public void XDocumentAddNull()
        {
            XElement xElem = new XElement("root", "text");
            EventsHelper elemHelper = new EventsHelper(xElem);
            xElem.FirstNode.AddBeforeSelf(null);
            elemHelper.Verify(0);
        }

        [Fact]
        public void XElementWorkOnTextNodes1()
        {
            XElement elem = new XElement("A", "text2");
            XNode n = elem.FirstNode;
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    n.AddBeforeSelf("text0");
                    Assert.Equal("text0text2", elem.Value);
                    n.AddBeforeSelf("text1");
                    Assert.Equal("text0text1text2", elem.Value);
                    eHelper.Verify(new XObjectChange[] { XObjectChange.Add, XObjectChange.Value });
                }
                undo.Undo();
                Assert.Equal("text2", elem.Value);
            }
        }

        [Fact]
        public void XElementWorkOnTextNodes2()
        {
            XElement elem = new XElement("A", "text2");
            XNode n = elem.FirstNode;
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    n.AddBeforeSelf("text0", "text1");
                    Assert.Equal("text0text1text2", elem.Value);
                    eHelper.Verify(XObjectChange.Add);
                }
                undo.Undo();
                Assert.Equal("text2", elem.Value);
            }
        }
    }

    public class EventsAddAfterSelf
    {
        public static object[][] ExecuteXDocumentVariationParams = new object[][] {
            new object [] { new XNode[] { new XElement("element") }, new XComment("Comment") },
            new object [] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data") },
            new object [] { new XNode[] { new XDocumentType("root", "", "", "") }, new XText(" ") },
            new object [] { new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" ") },
            new object [] { new XNode[] { new XComment("Comment") }, new XElement("root") },
            new object [] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" ") }
        };
        [Theory, MemberData(nameof(ExecuteXDocumentVariationParams))]
        public void ExecuteXDocumentVariation(XNode[] toAdd, XNode contextNode)
        {
            IEnumerable<XNode> toAddList = toAdd.OfType<XNode>();
            XDocument xDoc = new XDocument(contextNode);
            XDocument xDocOriginal = new XDocument(xDoc);
            using (UndoManager undo = new UndoManager(xDoc))
            {
                undo.Group();
                using (EventsHelper docHelper = new EventsHelper(xDoc))
                {
                    using (EventsHelper nodeHelper = new EventsHelper(contextNode))
                    {
                        contextNode.AddAfterSelf(toAdd);
                        Assert.True(toAddList.SequenceEqual(contextNode.NodesAfterSelf(), XNode.EqualityComparer), "Nodes not added correctly!");
                        nodeHelper.Verify(0);
                    }
                    docHelper.Verify(XObjectChange.Add, toAdd);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        public static object[][] ExecuteXElementVariationParams = new object[][] {
            new object[] { new XNode[] { new XElement("element") }, new XText("some text") },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data") },
            new object[] { new XNode[] { new XCData("x+y >= z-m") }, new XElement("child") },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" ") },
            new object[] { new XNode[] { new XComment("Comment") }, new XCData("x+y >= z-m") },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" ") },
            new object[] { InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), new XText("..")  }
        };
        [Theory, MemberData(nameof(ExecuteXElementVariationParams))]
        public void ExecuteXElementVariation(XNode[] toAdd, XNode contextNode)
        {
            IEnumerable<XNode> toAddList = toAdd.OfType<XNode>();
            XElement xElem = new XElement("root", contextNode);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper elemHelper = new EventsHelper(xElem))
                {
                    using (EventsHelper nodeHelper = new EventsHelper(contextNode))
                    {
                        contextNode.AddAfterSelf(toAdd);
                        Assert.True(toAddList.SequenceEqual(contextNode.NodesAfterSelf(), XNode.EqualityComparer), "Nodes not added correctly!");
                        nodeHelper.Verify(0);
                    }
                    elemHelper.Verify(XObjectChange.Add, toAdd);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }

        [Fact]
        public void XElementAddNull()
        {
            XElement xElem = new XElement("root", "text");
            EventsHelper elemHelper = new EventsHelper(xElem);
            xElem.LastNode.AddAfterSelf(null);
            elemHelper.Verify(0);
        }

        [Fact]
        public void XElementWorkOnTextNodes1()
        {
            XElement elem = new XElement("A", "text2");
            XNode n = elem.FirstNode;
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    n.AddAfterSelf("text0");
                    Assert.Equal("text2text0", elem.Value);
                    n.AddAfterSelf("text1");
                    Assert.Equal("text2text0text1", elem.Value);
                    eHelper.Verify(new XObjectChange[] { XObjectChange.Value, XObjectChange.Value });
                }
                undo.Undo();
                Assert.Equal("text2", elem.Value);
            }
        }

        [Fact]
        public void XElementWorkOnTextNodes2()
        {
            XElement elem = new XElement("A", "text2");
            XNode n = elem.FirstNode;
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    n.AddAfterSelf("text0", "text1");
                    Assert.Equal("text2text0text1", elem.Value);
                    eHelper.Verify(XObjectChange.Value);
                }
                undo.Undo();
                Assert.Equal("text2", elem.Value);
            }
        }
    }

    public class EventsAddFirst
    {
        public static object[][] ExecuteXDocumentVariationParams = new object[][] {
            new object [] { new XNode[] { new XElement("element") }, null },
            new object [] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, null },
            new object [] { new XNode[] { new XDocumentType("root", "", "", "") }, null },
            new object [] { new XNode[] { new XProcessingInstruction("PI", "Data") }, null },
            new object [] { new XNode[] { new XComment("Comment") }, null },
            new object [] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, null },
            new object [] { new XNode[] { new XElement("element") }, new XComment("Comment") },
            new object [] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data") },
            new object [] { new XNode[] { new XDocumentType("root", "", "", "") }, new XText(" ") },
            new object [] { new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" ") },
            new object [] { new XNode[] { new XComment("Comment") }, new XElement("root") },
            new object [] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" ") }
        };
        [Theory, MemberData(nameof(ExecuteXDocumentVariationParams))]
        public void ExecuteXDocumentVariation(XNode[] toAdd, XNode contextNode)
        {
            IEnumerable<XNode> allNodes, toAddList = toAdd.OfType<XNode>();
            XDocument xDoc = contextNode == null ? new XDocument() : new XDocument(contextNode);
            XDocument xDocOriginal = new XDocument(xDoc);
            using (UndoManager undo = new UndoManager(xDoc))
            {
                undo.Group();
                using (EventsHelper docHelper = new EventsHelper(xDoc))
                {
                    xDoc.AddFirst(toAdd);
                    allNodes = contextNode == null ? xDoc.Nodes() : contextNode.NodesBeforeSelf();
                    Assert.True(toAddList.SequenceEqual(allNodes, XNode.EqualityComparer), "Nodes not added correctly!");
                    docHelper.Verify(XObjectChange.Add, toAdd);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        public static object[][] ExecuteXElementVariationParams = new object[][] {
            new object[] { new XNode[] { new XElement("element") }, null },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, null },
            new object[] { new XNode[] { new XCData("x+y >= z-m") }, null },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") }, null },
            new object[] { new XNode[] { new XComment("Comment") }, null },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, null },
            new object[] { InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), null },
            new object[] { new XNode[] { new XElement("element") }, new XText("some text") },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data") },
            new object[] { new XNode[] { new XCData("x+y >= z-m") }, new XElement("child") },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" ") },
            new object[] { new XNode[] { new XComment("Comment") }, new XCData("x+y >= z-m") },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" ") },
            new object[] { InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), new XText("..") }
        };
        [Theory, MemberData(nameof(ExecuteXElementVariationParams))]
        public void ExecuteXElementVariation(XNode[] toAdd, XNode contextNode)
        {
            IEnumerable<XNode> allNodes, toAddList = toAdd.OfType<XNode>();
            XElement xElem = contextNode == null ? new XElement("root") : new XElement("root", contextNode);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper elemHelper = new EventsHelper(xElem))
                {
                    xElem.AddFirst(toAdd);
                    allNodes = contextNode == null ? xElem.Nodes() : contextNode.NodesBeforeSelf();
                    Assert.True(toAddList.SequenceEqual(allNodes, XNode.EqualityComparer), "Nodes not added correctly!");
                    elemHelper.Verify(XObjectChange.Add, toAdd);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }

        [Fact]
        public void XElementAddNull()
        {
            XElement xElem = new XElement("root", "text");
            EventsHelper elemHelper = new EventsHelper(xElem);
            xElem.AddFirst(null);
            elemHelper.Verify(0);
        }

        [Fact]
        public void XElementWorkOnTextNodes1()
        {
            XElement elem = new XElement("A", "text2");
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    elem.AddFirst("text0");
                    Assert.Equal("text0text2", elem.Value);
                    elem.AddFirst("text1");
                    Assert.Equal("text1text0text2", elem.Value);
                    eHelper.Verify(new XObjectChange[] { XObjectChange.Add, XObjectChange.Add });
                }
                undo.Undo();
                Assert.Equal("text2", elem.Value);
            }
        }

        [Fact]
        public void XElementWorkOnTextNodes2()
        {
            XElement elem = new XElement("A", "text2");
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    elem.AddFirst("text0", "text1");
                    Assert.Equal("text0text1text2", elem.Value);
                    eHelper.Verify(XObjectChange.Add);
                }
                undo.Undo();
                Assert.Equal("text2", elem.Value);
            }
        }

        [Fact]
        public void XElementStringContent()
        {
            bool firstTime = true;
            XElement element = XElement.Parse("<root/>");
            element.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                        element.AddFirst("Value");
                    }
                });

            Assert.Throws<InvalidOperationException>(() => { element.AddFirst(""); });
            element.Verify();
        }

        [Fact]
        public void XElementParentedXNode()
        {
            bool firstTime = true;
            XElement element = XElement.Parse("<root></root>");
            XElement child = new XElement("Add", "Me");
            XElement newElement = new XElement("new", "element");
            element.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                        newElement.Add(child);
                    }
                });

            Assert.Throws<InvalidOperationException>(() => { element.AddFirst(child); });
            element.Verify();
            Assert.Null(element.Element("Add"));
        }
    }

    public class EventsAdd
    {
        public static object[][] ExecuteXDocumentVariationParams = new object[][] {
            new object[] { new XNode[] { new XElement("element") }, null },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, null },
            new object[] { new XNode[] { new XDocumentType("root", "", "", "") }, null },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") }, null },
            new object[] { new XNode[] { new XComment("Comment") }, null },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, null },
            new object[] { new XNode[] { new XElement("element") }, new XComment("Comment") },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data") },
            new object[] { new XNode[] { new XDocumentType("root", "", "", "") }, new XText(" ") },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" ") },
            new object[] { new XNode[] { new XComment("Comment") }, new XElement("root") },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" ") }
        };
        [Theory, MemberData(nameof(ExecuteXDocumentVariationParams))]
        public void ExecuteXDocumentVariation(XNode[] toAdd, XNode contextNode)
        {
            IEnumerable<XNode> allNodes, toAddList = toAdd.OfType<XNode>();
            XDocument xDoc = contextNode == null ? new XDocument() : new XDocument(contextNode);
            XDocument xDocOriginal = new XDocument(xDoc);
            using (UndoManager undo = new UndoManager(xDoc))
            {
                undo.Group();
                using (EventsHelper docHelper = new EventsHelper(xDoc))
                {
                    xDoc.Add(toAdd);
                    allNodes = contextNode == null ? xDoc.Nodes() : contextNode.NodesAfterSelf();
                    Assert.True(toAddList.SequenceEqual(allNodes, XNode.EqualityComparer), "Nodes not added correctly!");
                    docHelper.Verify(XObjectChange.Add, toAdd);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        public static object[][] VariationsForXElementParams = new object[][] {
            new object[] { new XNode[] { new XElement("element") }, null },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, null },
            new object[] { new XNode[] { new XCData("x+y >= z-m") }, null },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") }, null },
            new object[] { new XNode[] { new XComment("Comment") }, null },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, null },
            new object[] { InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), null },
            new object[] { new XNode[] { new XElement("element") }, new XText("some text") },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data") },
            new object[] { new XNode[] { new XCData("x+y >= z-m") }, new XElement("child") },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" ") },
            new object[] { new XNode[] { new XComment("Comment") }, new XCData("x+y >= z-m") },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" ") },
            new object[] { InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), new XText("..") }
        };
        [Theory, MemberData(nameof(VariationsForXElementParams))]
        public void ExecuteXElementVariation(XNode[] toAdd, XNode contextNode)
        {
            IEnumerable<XNode> allNodes, toAddList = toAdd.OfType<XNode>();
            XElement xElem = contextNode == null ? new XElement("root") : new XElement("root", contextNode);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper elemHelper = new EventsHelper(xElem))
                {
                    xElem.Add(toAdd);
                    allNodes = contextNode == null ? xElem.Nodes() : contextNode.NodesAfterSelf();
                    Assert.True(toAddList.SequenceEqual(allNodes, XNode.EqualityComparer), "Nodes not added correctly!");
                    elemHelper.Verify(XObjectChange.Add, toAdd);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }

        public static object[][] ExecuteXAttributeVariationParams = new object[][] {
            new object[] { new XAttribute[] { new XAttribute("xxx", "yyy") }, null },
            new object[] { new XAttribute[] { new XAttribute("{a}xxx", "a_yyy") }, null },
            new object[] { InputSpace.GetElement(100, 10).Attributes().ToArray(), null },
            new object[] { new XAttribute[] { new XAttribute("xxx", "yyy") }, new XAttribute("a", "aa") },
            new object[] { new XAttribute[] { new XAttribute("{b}xxx", "b_yyy") }, new XAttribute("a", "aa") },
            new object[] { InputSpace.GetElement(100, 10).Attributes().ToArray(), new XAttribute("a", "aa") }
        };
        [Theory, MemberData(nameof(ExecuteXAttributeVariationParams))]
        public void ExecuteXAttributeVariation(XAttribute[] toAdd, XAttribute contextNode)
        {
            IEnumerable<XAttribute> allNodes, toAddList = toAdd.OfType<XAttribute>();
            XElement xElem = contextNode == null ? new XElement("root") : new XElement("root", contextNode);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper elemHelper = new EventsHelper(xElem))
                {
                    xElem.Add(toAdd);
                    allNodes = contextNode == null ? xElem.Attributes() : xElem.Attributes().Skip(1);
                    Assert.True(toAddList.SequenceEqual(allNodes, Helpers.MyAttributeComparer), "Attributes not added correctly!");
                    elemHelper.Verify(XObjectChange.Add, toAdd);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }

        [Fact]
        public void XAttributeXAttributeAddAtDeepLevel()
        {
            XDocument xDoc = new XDocument(InputSpace.GetAttributeElement(100, 10));
            XDocument xDocOriginal = new XDocument(xDoc);
            using (UndoManager undo = new UndoManager(xDoc))
            {
                undo.Group();
                using (EventsHelper docHelper = new EventsHelper(xDoc))
                {
                    using (EventsHelper eHelper = new EventsHelper(xDoc.Root))
                    {
                        foreach (XElement x in xDoc.Root.Descendants())
                        {
                            x.Add(new XAttribute("at", "value"));
                            eHelper.Verify(XObjectChange.Add);
                        }
                        docHelper.Verify(XObjectChange.Add, xDoc.Root.Descendants().Count());
                    }
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        [Fact]
        public void XElementXElementAddAtDeepLevel()
        {
            XDocument xDoc = new XDocument(InputSpace.GetElement(100, 10));
            XDocument xDocOriginal = new XDocument(xDoc);
            using (UndoManager undo = new UndoManager(xDoc))
            {
                undo.Group();
                using (EventsHelper docHelper = new EventsHelper(xDoc))
                {
                    using (EventsHelper eHelper = new EventsHelper(xDoc.Root))
                    {
                        foreach (XElement x in xDoc.Root.Descendants())
                        {
                            x.Add(new XText("Add Me"));
                            eHelper.Verify(XObjectChange.Add);
                        }
                        docHelper.Verify(XObjectChange.Add, xDoc.Root.Descendants().Count());
                    }
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        [Fact]
        public void XElementWorkTextNodes()
        {
            XElement elem = new XElement("A", "text2");
            XElement xElemOriginal = new XElement(elem);
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    XNode x = elem.LastNode;
                    eHelper.Verify(0);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(elem, xElemOriginal), "Undo did not work!");
            }
        }

        [Fact]
        public void XElementWorkOnTextNodes1()
        {
            XElement elem = new XElement("A", "text2");
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    elem.Add("text0");
                    Assert.Equal("text2text0", elem.Value);
                    elem.Add("text1");
                    Assert.Equal("text2text0text1", elem.Value);
                    eHelper.Verify(new XObjectChange[] { XObjectChange.Value, XObjectChange.Value });
                }
                undo.Undo();
                Assert.Equal("text2", elem.Value);
            }
        }

        [Fact]
        public void XElementWorkOnTextNodes2()
        {
            XElement elem = new XElement("A", "text2");
            XElement xElemOriginal = new XElement(elem);
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    elem.Add("text0", "text1");
                    Assert.Equal("text2text0text1", elem.Value);
                    eHelper.Verify(new XObjectChange[] { XObjectChange.Value, XObjectChange.Value });
                }
                undo.Undo();
                Assert.Equal("text2", elem.Value);
            }
        }

        [Fact]
        public void XElementStringContent()
        {
            bool firstTime = true;
            XElement element = XElement.Parse("<root/>");
            element.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                        element.Add("Value");
                    }
                });

            Assert.Throws<InvalidOperationException>(() => { element.Add(""); });
            element.Verify();
        }

        [Fact]
        public void XElementParentedXNode()
        {
            bool firstTime = true;
            XElement element = XElement.Parse("<root></root>");
            XElement child = new XElement("Add", "Me");
            XElement newElement = new XElement("new", "element");
            element.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                        newElement.Add(child);
                    }
                });

            Assert.Throws<InvalidOperationException>(() => { element.Add(child); });
            element.Verify();
            Assert.Null(element.Element("Add"));
        }

        [Fact]
        public void XElementParentedAttribute()
        {
            bool firstTime = true;
            XElement element = XElement.Parse("<root></root>");
            XElement newElement = new XElement("new", "element");
            XAttribute child = new XAttribute("Add", "Me");
            element.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                        newElement.Add(child);
                    }
                });

            Assert.Throws<InvalidOperationException>(() => { element.Add(child); });
            element.Verify();
            Assert.Null(element.Attribute("Add"));
        }
    }
}
