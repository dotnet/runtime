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
    public class EventsRemove
    {
        public static object[][] ExecuteXDocumentVariationParams = new object[][] {
            new object[] { new XNode[] { new XElement("element") }, 0 },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, 0 },
            new object[] { new XNode[] { new XDocumentType("root", "", "", "") }, 0 },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") }, 0 },
            new object[] { new XNode[] { new XComment("Comment") }, 0 },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, 1 }
        };
        [Theory, MemberData(nameof(ExecuteXDocumentVariationParams))]
        public void ExecuteXDocumentVariation(XNode[] content, int index)
        {
            XDocument xDoc = new XDocument(content);
            XDocument xDocOriginal = new XDocument(xDoc);
            XNode toRemove = xDoc.Nodes().ElementAt(index);
            using (UndoManager undo = new UndoManager(xDoc))
            {
                undo.Group();
                using (EventsHelper docHelper = new EventsHelper(xDoc))
                {
                    toRemove.Remove();
                    docHelper.Verify(XObjectChange.Remove, toRemove);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        public static object[][] ExecuteXElementVariationParams = new object[][] {
            new object[] { new XNode[] { new XElement("element") }, 0 },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) }, 0 },
            new object[] { new XNode[] { new XElement("parent", "parent text"), new XElement("child", "child text") }, 1 },
            new object[] { new XNode[] { new XElement("parent", "parent text"), new XText("text"), new XElement("child", "child text") }, 1 },
            new object[] { new XNode[] { new XCData("x+y >= z-m") }, 0 },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") }, 0 },
            new object[] { new XNode[] { new XComment("Comment") }, 0 },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, 0 },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, 1 },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") }, 2 },
            new object[] { InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), 50 }
        };
        [Theory, MemberData(nameof(ExecuteXElementVariationParams))]
        public void ExecuteXElementVariation(XNode[] content, int index)
        {
            XElement xElem = new XElement("root", content);
            XNode toRemove = xElem.Nodes().ElementAt(index);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper elemHelper = new EventsHelper(xElem))
                {
                    toRemove.Remove();
                    xElem.Verify();
                    elemHelper.Verify(XObjectChange.Remove, toRemove);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }

        public static object[][] ExecuteXAttributeVariationParams = new object[][] {
            new object[] { new XAttribute[] { new XAttribute("xxx", "yyy") }, 0 },
            new object[] { new XAttribute[] { new XAttribute("{a}xxx", "a_yyy") }, 0 },
            new object[] { new XAttribute[] { new XAttribute("xxx", "yyy"), new XAttribute("a", "aa") }, 1 },
            new object[] { new XAttribute[] { new XAttribute("{b}xxx", "b_yyy"), new XAttribute("{a}xxx", "a_yyy") }, 0 },
            new object[] { InputSpace.GetAttributeElement(10, 1000).Elements().Attributes().ToArray(), 10 }
        };
        [Theory, MemberData(nameof(ExecuteXAttributeVariationParams))]
        public void ExecuteXAttributeVariation(XAttribute[] content, int index)
        {
            XElement xElem = new XElement("root", content);
            XAttribute toRemove = xElem.Attributes().ElementAt(index);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper elemHelper = new EventsHelper(xElem))
                {
                    toRemove.Remove();
                    xElem.Verify();
                    elemHelper.Verify(XObjectChange.Remove, toRemove);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }

        [Fact]
        public void XElementWorkOnTextNodes1()
        {
            XElement elem = new XElement("A", "text2");
            elem.Add("text0");
            elem.Add("text1");
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    elem.FirstNode.Remove();
                    eHelper.Verify(XObjectChange.Remove);
                    Assert.True(elem.IsEmpty, "Did not remove correctly");
                }
                undo.Undo();
                Assert.Equal(elem.Value, "text2text0text1");
            }
        }

        [Fact]
        public void XElementWorkOnTextNodes2()
        {
            XElement elem = new XElement("A", "text2");
            elem.AddFirst("text0");
            elem.AddFirst("text1");
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    elem.FirstNode.Remove();
                    eHelper.Verify(XObjectChange.Remove);
                    Assert.False(elem.IsEmpty, "Did not remove correctly");
                    elem.FirstNode.Remove();
                    eHelper.Verify(XObjectChange.Remove);
                    Assert.False(elem.IsEmpty, "Did not remove correctly");
                    elem.FirstNode.Remove();
                    eHelper.Verify(XObjectChange.Remove);
                    Assert.True(elem.IsEmpty, "Did not remove correctly");
                }
                undo.Undo();
                Assert.Equal(elem.Value, "text1text0text2");
            }
        }

        [Fact]
        public void XElementWorkOnTextNodes3()
        {
            XElement elem = new XElement("A", "text2");
            elem.FirstNode.AddBeforeSelf("text0");
            elem.FirstNode.AddBeforeSelf("text1");
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    elem.FirstNode.Remove();
                    eHelper.Verify(XObjectChange.Remove);
                    Assert.False(elem.IsEmpty, "Did not remove correctly");
                    elem.FirstNode.Remove();
                    eHelper.Verify(XObjectChange.Remove);
                    Assert.False(elem.IsEmpty, "Did not remove correctly");
                    elem.FirstNode.Remove();
                    eHelper.Verify(XObjectChange.Remove);
                    Assert.True(elem.IsEmpty, "Did not remove correctly");
                }
                undo.Undo();
                Assert.Equal(elem.Value, "text1text0text2");
            }
        }

        [Fact]
        public void XElementWorkOnTextNodes4()
        {
            XElement elem = new XElement("A", "text2");
            elem.FirstNode.AddAfterSelf("text0");
            elem.FirstNode.AddAfterSelf("text1");
            using (UndoManager undo = new UndoManager(elem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(elem))
                {
                    elem.FirstNode.Remove();
                    eHelper.Verify(XObjectChange.Remove);
                    Assert.True(elem.IsEmpty, "Did not remove correctly");
                }
                undo.Undo();
                Assert.Equal(elem.Value, "text2text0text1");
            }
        }

        [Fact]
        public void XAttributeRemoveOneByOne()
        {
            XDocument xDoc = new XDocument(InputSpace.GetAttributeElement(1, 100));
            XDocument xDocOriginal = new XDocument(xDoc);
            using (UndoManager undo = new UndoManager(xDoc))
            {
                undo.Group();
                using (EventsHelper docHelper = new EventsHelper(xDoc))
                {
                    using (EventsHelper eHelper = new EventsHelper(xDoc.Root))
                    {
                        List<XAttribute> list = xDoc.Root.Attributes().ToList();
                        foreach (XAttribute x in list)
                        {
                            x.Remove();
                            eHelper.Verify(XObjectChange.Remove, x);
                        }
                        docHelper.Verify(XObjectChange.Remove, list.Count);
                    }
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        [Fact]
        public void XElementRemoveOneByOne()
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
                        List<XElement> list = xDoc.Root.Elements().ToList();
                        foreach (XElement x in list)
                        {
                            x.Remove();
                            eHelper.Verify(XObjectChange.Remove, x);
                        }
                        docHelper.Verify(XObjectChange.Remove, list.Count);
                    }
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        [Fact]
        public void XAttributeRemoveSeq()
        {
            XDocument xDoc = new XDocument(InputSpace.GetAttributeElement(100, 100));
            XDocument xDocOriginal = new XDocument(xDoc);
            using (UndoManager undo = new UndoManager(xDoc))
            {
                undo.Group();
                using (EventsHelper docHelper = new EventsHelper(xDoc))
                {
                    using (EventsHelper eHelper = new EventsHelper(xDoc.Root))
                    {
                        List<XAttribute> list = xDoc.Root.Attributes().ToList();
                        xDoc.Root.Attributes().Remove();
                        eHelper.Verify(XObjectChange.Remove, list.ToArray());
                        docHelper.Verify(XObjectChange.Remove, list.ToArray());
                    }
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        [Fact]
        public void XElementRemoveSeq()
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
                        List<XElement> list = xDoc.Root.Elements().ToList();
                        xDoc.Root.Elements().Remove();
                        eHelper.Verify(XObjectChange.Remove, list.ToArray());
                        docHelper.Verify(XObjectChange.Remove, list.ToArray());
                    }
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        [Fact]
        public void XElementParentedXNode()
        {
            bool firstTime = true;
            XElement element = XElement.Parse("<root></root>");
            XElement child = new XElement("Add", "Me");
            element.Add(child);
            element.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                        child.Remove();
                    }
                });

            Assert.Throws<InvalidOperationException>(() => { child.Remove(); });
            element.Verify();
        }

        [Fact]
        public void XElementChangeAttributesParentInThePreEventHandler()
        {
            bool firstTime = true;
            XElement element = XElement.Parse("<root></root>");
            XAttribute child = new XAttribute("Add", "Me");
            element.Add(child);
            element.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                        child.Remove();
                    }
                });

            Assert.Throws<InvalidOperationException>(() => { child.Remove(); });
            element.Verify();
        }
    }

    public class EventsRemoveNodes
    {
        public static object[][] ExecuteXDocumentVariationParams = new object[][] {
            new object[] { new XNode[] { new XElement("element") } },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) } },
            new object[] { new XNode[] { new XDocumentType("root", "", "", "") } },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") } },
            new object[] { new XNode[] { new XComment("Comment") } },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") } }
        };
        [Theory, MemberData(nameof(ExecuteXDocumentVariationParams))]
        public void ExecuteXDocumentVariation(XNode[] content)
        {
            XDocument xDoc = new XDocument(content);
            XDocument xDocOriginal = new XDocument(xDoc);
            using (UndoManager undo = new UndoManager(xDoc))
            {
                undo.Group();
                using (EventsHelper docHelper = new EventsHelper(xDoc))
                {
                    xDoc.RemoveNodes();
                    docHelper.Verify(XObjectChange.Remove, content);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        public static object[][] ExecuteXElementVariationParams = new object[][] {
            new object[] { new XNode[] { new XElement("element") } },
            new object[] { new XNode[] { new XElement("parent", new XElement("child", "child text")) } },
            new object[] { new XNode[] { new XCData("x+y >= z-m") } },
            new object[] { new XNode[] { new XProcessingInstruction("PI", "Data") } },
            new object[] { new XNode[] { new XComment("Comment") } },
            new object[] { new XNode[] { new XText(""), new XText(" "), new XText("\t") } },
            new object[] { InputSpace.GetElement(100, 10).DescendantNodes().ToArray() }
        };
        [Theory, MemberData(nameof(ExecuteXElementVariationParams))]
        public void ExecuteXElementVariation(XNode[] content)
        {
            XElement xElem = new XElement("root", InputSpace.GetAttributeElement(10, 1000).Elements().Attributes(), content);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper elemHelper = new EventsHelper(xElem))
                {
                    xElem.RemoveNodes();
                    Assert.True(xElem.IsEmpty, "Not all content were removed");
                    Assert.True(xElem.HasAttributes, "RemoveNodes removed the attributes");
                    xElem.Verify();
                    elemHelper.Verify(XObjectChange.Remove, content);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }

        [Fact]
        public void XElementEmptyElementSpecialCase()
        {
            XElement e = XElement.Parse(@"<A></A>");
            EventsHelper eHelper = new EventsHelper(e);
            e.RemoveNodes();
            //eHelper.Verify(XObjectChange.Remove, e.Nodes());
            eHelper.Verify(XObjectChange.Value);
        }

        [Fact]
        public void XElementChangeNodeValueInThePreEventHandler()
        {
            bool firstTime = true;
            XElement element = XElement.Parse("<root></root>");
            XElement child = new XElement("Add", "Me");
            element.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                        element.Add(child);
                    }
                });

            Assert.Throws<InvalidOperationException>(() => { element.RemoveNodes(); });
            element.Verify();
            Assert.NotNull(element.Element("Add"));
        }

        [Fact]
        public void XElementChangeNodesInThePreEventHandler()
        {
            bool firstTime = true;
            XElement element = XElement.Parse("<root></root>");
            XElement oneChild = new XElement("one", "1");
            XElement twoChild = new XElement("two", "2");
            element.Add(oneChild, twoChild);
            element.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                        element.RemoveNodes();
                    }
                });

            Assert.Throws<InvalidOperationException>(() => { element.RemoveNodes(); });
            element.Verify();
        }

        [Fact]
        public void XElementChangeAttributesInThePreEventHandler()
        {
            bool firstTime = true;
            XElement element = XElement.Parse("<root></root>");
            XAttribute oneChild = new XAttribute("one", "1");
            XAttribute twoChild = new XAttribute("two", "2");
            element.Add(oneChild, twoChild);
            element.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                        element.RemoveAttributes();
                    }
                });

            Assert.Throws<InvalidOperationException>(() => { element.RemoveAttributes(); });
            element.Verify();
        }
    }

    public class EventsRemoveAll
    {
        public static object[][] ExecuteXElementVariationParams = new object[][] {
            new object[] { new XObject[] { new XElement("element") } },
            new object[] { new XObject[] { new XElement("parent", new XElement("child", "child text")) } },
            new object[] { new XObject[] { new XCData("x+y >= z-m") } },
            new object[] { new XObject[] { new XProcessingInstruction("PI", "Data") } },
            new object[] { new XObject[] { new XComment("Comment") } },
            new object[] { new XObject[] { new XText(""), new XText(" "), new XText("\t") } },
            new object[] { InputSpace.GetElement(100, 10).DescendantNodes().ToArray() },
            new object[] { new XObject[] { new XAttribute("xxx", "yyy") } },
            new object[] { new XObject[] { new XAttribute("{a}xxx", "a_yyy") } },
            new object[] { new XObject[] { new XAttribute("xxx", "yyy"), new XAttribute("a", "aa") } },
            new object[] { new XObject[] { new XAttribute("{b}xxx", "b_yyy"), new XAttribute("{a}xxx", "a_yyy") } },
            new object[] { InputSpace.GetAttributeElement(10, 1000).Elements().Attributes().ToArray() },
            new object[] { new XObject[] { new XAttribute("{b}xxx", "b_yyy"), new XElement("parent", new XElement("child", "child text")) } }
        };
        [Theory, MemberData(nameof(ExecuteXElementVariationParams))]
        public void ExecuteXElementVariation(XObject[] content)
        {
            XElement xElem = new XElement("root", content);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper elemHelper = new EventsHelper(xElem))
                {
                    xElem.RemoveAll();
                    Assert.True(xElem.IsEmpty, "Not all content were removed");
                    Assert.True(!xElem.HasAttributes, "RemoveAll did not remove attributes");
                    xElem.Verify();
                    elemHelper.Verify(XObjectChange.Remove, content);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }
    }
}
