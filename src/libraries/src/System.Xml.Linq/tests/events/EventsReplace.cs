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
    public  class EventsReplaceWith
    {
        public static object[][] ExecuteXDocumentVariationParams = new object[][] {
            new object[] { new XElement("element"), new XText(" ") },
            new object[] { new XElement("element", new XAttribute("a", "aa")), new XText("") },
            new object[] { new XElement("parent", new XElement("child", "child text")), new XElement("element") },
            new object[] { new XDocumentType("root", "", "", ""), new XComment("Comment") },
            new object[] { new XProcessingInstruction("PI", "Data"), new XDocumentType("root", "", "", "") },
            new object[] { new XComment("Comment"), new XText("\t") },
            new object[] { new XText(" "), XElement.Parse(@"<a></a>") }
        };
        [Theory, MemberData(nameof(ExecuteXDocumentVariationParams))]
        public void ExecuteXDocumentVariation(XNode toReplace, XNode newValue)
        {
            XDocument xDoc = new XDocument(toReplace);
            XDocument xDocOriginal = new XDocument(xDoc);
            using (UndoManager undo = new UndoManager(xDoc))
            {
                undo.Group();
                using (EventsHelper docHelper = new EventsHelper(xDoc))
                {
                    toReplace.ReplaceWith(newValue);
                    docHelper.Verify(new XObjectChange[] { XObjectChange.Remove, XObjectChange.Add }, new XObject[] { toReplace, newValue });
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        public static object[][] ExecuteXElementVariationParams = new object[][] {
            new object[] { new XElement("element"), new XText(" ") },
            new object[] { new XElement("element", new XAttribute("a", "aa")), new XText("") },
            new object[] { new XElement("parent", new XElement("child", "child text")), new XElement("element") },
            new object[] { new XCData("x+y >= z-m"), new XComment("Comment") },
            new object[] { new XProcessingInstruction("PI", "Data"), new XElement("element", new XAttribute("a", "aa")) },
            new object[] { new XComment("Comment"), new XText("\t") },
            new object[] { new XText("\t"), XElement.Parse(@"<a></a>") }
        };
        [Theory, MemberData(nameof(ExecuteXElementVariationParams))]
        public void ExecuteXElementVariation(XNode toReplace, XNode newValue)
        {
            XElement xElem = new XElement("root", toReplace);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(xElem))
                {
                    toReplace.ReplaceWith(newValue);
                    xElem.Verify();
                    eHelper.Verify(new XObjectChange[] { XObjectChange.Remove, XObjectChange.Add }, new XObject[] { toReplace, newValue });
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }
    }

    public class EventsReplaceNodes
    {
        public static object[][] ExecuteXDocumentVariationParams = new object[][] {
            new object[] { new XElement("element") },
            new object[] { new XElement("parent", new XElement("child", "child text")) },
            new object[] { new XDocumentType("root", "", "", "") },
            new object[] { new XProcessingInstruction("PI", "Data") },
            new object[] { new XComment("Comment") }
        };
        [Theory, MemberData(nameof(ExecuteXDocumentVariationParams))]
        public void ExecuteXDocumentVariation(XNode toReplace)
        {
            XNode newValue = new XText(" ");
            XDocument xDoc = new XDocument(toReplace);
            XDocument xDocOriginal = new XDocument(xDoc);
            using (UndoManager undo = new UndoManager(xDoc))
            {
                undo.Group();
                using (EventsHelper docHelper = new EventsHelper(xDoc))
                {
                    xDoc.ReplaceNodes(newValue);
                    Assert.True(xDoc.Nodes().Count() == 1, "Not all content were removed");
                    Assert.True(Object.ReferenceEquals(xDoc.FirstNode, newValue), "Did not replace correctly");
                    docHelper.Verify(new XObjectChange[] { XObjectChange.Remove, XObjectChange.Add }, new XObject[] { toReplace, newValue });
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
            }
        }

        public static object[][] ExecuteXElementVariationParams = new object[][] {
            new object[] { XElement.Parse(@"<a></a>") },
            new object[] { new XElement("element") },
            new object[] { new XElement("parent", new XElement("child", "child text")) },
            new object[] { new XCData("x+y >= z-m") },
            new object[] { new XProcessingInstruction("PI", "Data") },
            new object[] { new XComment("Comment") },
            new object[] { new XText("") }
        };
        [Theory, MemberData(nameof(ExecuteXElementVariationParams))]
        public void ExecuteXElementVariation(XNode toReplace)
        {
            XNode newValue = new XText("text");
            XElement xElem = new XElement("root", InputSpace.GetAttributeElement(10, 1000).Elements().Attributes(), toReplace);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(xElem))
                {
                    xElem.ReplaceNodes(newValue);
                    Assert.True(xElem.Nodes().Count() == 1, "Did not replace correctly");
                    Assert.True(Object.ReferenceEquals(xElem.FirstNode, newValue), "Did not replace correctly");
                    Assert.True(xElem.HasAttributes, "ReplaceNodes removed attributes");
                    xElem.Verify();
                    eHelper.Verify(new XObjectChange[] { XObjectChange.Remove, XObjectChange.Add }, new XObject[] { toReplace, newValue });
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }

        [Fact]
        public void XElementReplaceNodes()
        {
            XElement xElem = new XElement(InputSpace.GetElement(1000, 2));
            int count = xElem.Nodes().Count();
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(xElem))
                {
                    foreach (XElement x in xElem.Nodes())
                    {
                        using (EventsHelper xHelper = new EventsHelper(x))
                        {
                            x.ReplaceNodes("text");
                            xHelper.Verify(new XObjectChange[] { XObjectChange.Remove, XObjectChange.Remove, XObjectChange.Remove, XObjectChange.Add });
                        }
                        eHelper.Verify(new XObjectChange[] { XObjectChange.Remove, XObjectChange.Remove, XObjectChange.Remove, XObjectChange.Add });
                    }
                    undo.Undo();
                    Assert.True(XNode.DeepEquals(xElem, xElemOriginal), "Undo did not work!");
                }
            }
        }

        [Fact]
        public void XElementReplaceWithIEnumerable()
        {
            XElement xElem = new XElement("root");
            IEnumerable<XNode> newValue = InputSpace.GetElement(1000, 2).DescendantNodes();
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(xElem))
                {
                    xElem.ReplaceNodes(newValue);
                    eHelper.Verify(XObjectChange.Add, newValue.ToArray());
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xElem, xElemOriginal), "Undo did not work!");
            }
        }
    }

    public class EvensReplaceAttributes
    {
        public static object[][] ExecuteXAttributeVariationParams = new object[][] {
            new object[] { new XAttribute[] { new XAttribute("xxx", "yyy") } },
            new object[] { new XAttribute[] { new XAttribute("{a}xxx", "a_yyy") } },
            new object[] { new XAttribute[] { new XAttribute("xxx", "yyy"), new XAttribute("a", "aa") } },
            new object[] { new XAttribute[] { new XAttribute("{b}xxx", "b_yyy"), new XAttribute("{a}xxx", "a_yyy") } },
            new object[] { InputSpace.GetAttributeElement(10, 1000).Elements().Attributes().ToArray() }
        };
        [Theory, MemberData(nameof(ExecuteXAttributeVariationParams))]
        public void ExecuteXAttributeVariation(XAttribute[] content)
        {
            XElement xElem = new XElement("root", content);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(xElem))
                {
                    xElem.ReplaceAttributes(new XAttribute("a", "aa"));
                    Assert.True(XObject.ReferenceEquals(xElem.FirstAttribute, xElem.LastAttribute), "Did not replace attributes correctly");
                    xElem.Verify();
                    eHelper.Verify(content.Length + 1);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }
    }

    public class EventsReplaceAll
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
        public void ExecuteXElementVariation(XObject[] toReplace)
        {
            XNode newValue = new XText("text");
            XElement xElem = new XElement("root", toReplace);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(xElem))
                {
                    xElem.ReplaceAll(newValue);
                    Assert.True(xElem.Nodes().Count() == 1, "Did not replace correctly");
                    Assert.True(Object.ReferenceEquals(xElem.FirstNode, newValue), "Did not replace correctly");
                    Assert.True(!xElem.HasAttributes, "ReplaceAll did not remove attributes");
                    xElem.Verify();
                    eHelper.Verify(toReplace.Length + 1);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }

        [Fact]
        public void ElementWithAttributes()
        {
            XElement toReplace = new XElement("Automobile",
                new XAttribute("axles", 2),
                new XElement("Make", "Ford"),
                new XElement("Model", "Mustang"),
                new XElement("Year", "2004"));
            XElement xElemOriginal = new XElement(toReplace);

            using (UndoManager undo = new UndoManager(toReplace))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(toReplace))
                {
                    toReplace.ReplaceAll(new XAttribute("axles", 2),
                        new XElement("Make", "Chevrolet"),
                        new XElement("Model", "Impala"),
                        new XElement("Year", "2006"));
                    toReplace.Verify();
                }
                undo.Undo();
                Assert.True(toReplace.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(toReplace.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }
    }
}
