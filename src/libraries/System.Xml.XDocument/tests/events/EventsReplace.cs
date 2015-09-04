// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class EventsTests : XLinqTestCase
        {
            public partial class EventsReplaceWith : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXDocument(ExecuteXDocumentVariation);
                    VariationsForXElement(ExecuteXElementVariation);
                    base.DetermineChildren();
                }

                void VariationsForXDocument(TestFunc func)
                {
                    AddChild(func, 0, "XDocument - empty element, text", new XElement("element"), new XText(" "));
                    AddChild(func, 0, "XDocument - element with attribtue, text", new XElement("element", new XAttribute("a", "aa")), new XText(""));
                    AddChild(func, 0, "XDocument - element, empty element ", new XElement("parent", new XElement("child", "child text")), new XElement("element"));
                    AddChild(func, 0, "XDocument - document type, comment", new XDocumentType("root", "", "", ""), new XComment("Comment"));
                    AddChild(func, 0, "XDocument - PI, document type", new XProcessingInstruction("PI", "Data"), new XDocumentType("root", "", "", ""));
                    AddChild(func, 0, "XDocument - comment, text", new XComment("Comment"), new XText("\t"));
                    AddChild(func, 0, "XDocument - text node, element", new XText(" "), XElement.Parse(@"<a></a>"));
                }

                void VariationsForXElement(TestFunc func)
                {
                    AddChild(func, 0, "XElement - empty element, text", new XElement("element"), new XText(" "));
                    AddChild(func, 0, "XElement - element with attribtue, text", new XElement("element", new XAttribute("a", "aa")), new XText(""));
                    AddChild(func, 0, "XElement - element, empty element", new XElement("parent", new XElement("child", "child text")), new XElement("element"));
                    AddChild(func, 0, "XElement - CData, comment", new XCData("x+y >= z-m"), new XComment("Comment"));
                    AddChild(func, 0, "XElement - PI, element with attribute", new XProcessingInstruction("PI", "Data"), new XElement("element", new XAttribute("a", "aa")));
                    AddChild(func, 0, "XElement - comment, text", new XComment("Comment"), new XText("\t"));
                    AddChild(func, 0, "XElement - text node, element", new XText("\t"), XElement.Parse(@"<a></a>"));
                }

                public void ExecuteXDocumentVariation()
                {
                    XNode toReplace = Variation.Params[0] as XNode;
                    XNode newValue = Variation.Params[1] as XNode;
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
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                public void ExecuteXElementVariation()
                {
                    XNode toReplace = Variation.Params[0] as XNode;
                    XNode newValue = Variation.Params[1] as XNode;
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
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }
            }

            public partial class EventsReplaceNodes : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXDocument(ExecuteXDocumentVariation);
                    VariationsForXElement(ExecuteXElementVariation);
                    base.DetermineChildren();
                }

                void VariationsForXDocument(TestFunc func)
                {
                    AddChild(func, 0, "XDocument - empty element", new XElement("element"));
                    AddChild(func, 0, "XDocument - element with child", new XElement("parent", new XElement("child", "child text")));
                    AddChild(func, 0, "XDocument - document type", new XDocumentType("root", "", "", ""));
                    AddChild(func, 0, "XDocument - PI", new XProcessingInstruction("PI", "Data"));
                    AddChild(func, 0, "XDocument - comment", new XComment("Comment"));
                }

                void VariationsForXElement(TestFunc func)
                {
                    AddChild(func, 0, "XElement - empty element I", XElement.Parse(@"<a></a>"));
                    AddChild(func, 0, "XElement - empty element II", new XElement("element"));
                    AddChild(func, 0, "XElement - element with child", new XElement("parent", new XElement("child", "child text")));
                    AddChild(func, 0, "XElement - CData", new XCData("x+y >= z-m"));
                    AddChild(func, 0, "XElement - PI", new XProcessingInstruction("PI", "Data"));
                    AddChild(func, 0, "XElement - comment", new XComment("Comment"));
                    AddChild(func, 0, "XElement - text nodes", new XText(""));
                }

                public void ExecuteXDocumentVariation()
                {
                    XNode toReplace = Variation.Params[0] as XNode;
                    XNode newValue = new XText(" ");
                    XDocument xDoc = new XDocument(toReplace);
                    XDocument xDocOriginal = new XDocument(xDoc);
                    using (UndoManager undo = new UndoManager(xDoc))
                    {
                        undo.Group();
                        using (EventsHelper docHelper = new EventsHelper(xDoc))
                        {
                            xDoc.ReplaceNodes(newValue);
                            TestLog.Compare(xDoc.Nodes().Count() == 1, "Not all content were removed");
                            TestLog.Compare(Object.ReferenceEquals(xDoc.FirstNode, newValue), "Did not replace correctly");
                            docHelper.Verify(new XObjectChange[] { XObjectChange.Remove, XObjectChange.Add }, new XObject[] { toReplace, newValue });
                        }
                        undo.Undo();
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                public void ExecuteXElementVariation()
                {
                    XNode toReplace = Variation.Params[0] as XNode;
                    XNode newValue = new XText("text");
                    XElement xElem = new XElement("root", InputSpace.GetAttributeElement(10, 1000).Elements().Attributes(), toReplace);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(xElem))
                        {
                            xElem.ReplaceNodes(newValue);
                            TestLog.Compare(xElem.Nodes().Count() == 1, "Did not replace correctly");
                            TestLog.Compare(Object.ReferenceEquals(xElem.FirstNode, newValue), "Did not replace correctly");
                            TestLog.Compare(xElem.HasAttributes, "ReplaceNodes removed attributes");
                            xElem.Verify();
                            eHelper.Verify(new XObjectChange[] { XObjectChange.Remove, XObjectChange.Add }, new XObject[] { toReplace, newValue });
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Replace Nodes")]
                public void ReplaceNodes()
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
                            TestLog.Compare(XNode.DeepEquals(xElem, xElemOriginal), "Undo did not work!");
                        }
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Replace with IEnumerable")]
                public void ReplaceWithIEnum()
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
                        TestLog.Compare(XNode.DeepEquals(xElem, xElemOriginal), "Undo did not work!");
                    }
                }
            }

            public partial class EvensReplaceAttributes : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXAttribute(ExecuteXAttributeVariation);
                    base.DetermineChildren();
                }

                void VariationsForXAttribute(TestFunc func)
                {
                    AddChild(func, 0, "Only attribute", new XAttribute[] { new XAttribute("xxx", "yyy") });
                    AddChild(func, 0, "Only attribute with namespace", new XAttribute[] { new XAttribute("{a}xxx", "a_yyy") });
                    AddChild(func, 0, "Mulitple attributes", new XAttribute[] { new XAttribute("xxx", "yyy"), new XAttribute("a", "aa") });
                    AddChild(func, 0, "Multiple attributes with namespace", new XAttribute[] { new XAttribute("{b}xxx", "b_yyy"), new XAttribute("{a}xxx", "a_yyy") });
                    AddChild(func, 0, "IEnumerable of XAttributes", InputSpace.GetAttributeElement(10, 1000).Elements().Attributes().ToArray());
                }

                public void ExecuteXAttributeVariation()
                {
                    XAttribute[] content = Variation.Params as XAttribute[];
                    XElement xElem = new XElement("root", content);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(xElem))
                        {
                            xElem.ReplaceAttributes(new XAttribute("a", "aa"));
                            TestLog.Compare(XObject.ReferenceEquals(xElem.FirstAttribute, xElem.LastAttribute), "Did not replace attributes correctly");
                            xElem.Verify();
                            eHelper.Verify(content.Length + 1);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }
            }

            public partial class EventsReplaceAll : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXElement(ExecuteXElementVariation);
                    base.DetermineChildren();
                }

                void VariationsForXElement(TestFunc func)
                {
                    AddChild(func, 1, "XElement - empty element", new XObject[] { new XElement("element") });
                    AddChild(func, 0, "XElement - element with child", new XObject[] { new XElement("parent", new XElement("child", "child text")) });
                    AddChild(func, 0, "XElement - CData", new XObject[] { new XCData("x+y >= z-m") });
                    AddChild(func, 0, "XElement - PI", new XObject[] { new XProcessingInstruction("PI", "Data") });
                    AddChild(func, 0, "XElement - comment", new XObject[] { new XComment("Comment") });
                    AddChild(func, 0, "XElement - text nodes", new XObject[] { new XText(""), new XText(" "), new XText("\t") });
                    AddChild(func, 1, "XElement - IEnumerable of XNodes", InputSpace.GetElement(100, 10).DescendantNodes().ToArray());
                    AddChild(func, 0, "XAttribute - only attribute", new XObject[] { new XAttribute("xxx", "yyy") });
                    AddChild(func, 0, "XAttribute - only attribute with namespace", new XObject[] { new XAttribute("{a}xxx", "a_yyy") });
                    AddChild(func, 0, "XAttribute - mulitple attributes", new XObject[] { new XAttribute("xxx", "yyy"), new XAttribute("a", "aa") });
                    AddChild(func, 1, "XAttribute - multiple attributes with namespace", new XObject[] { new XAttribute("{b}xxx", "b_yyy"), new XAttribute("{a}xxx", "a_yyy") });
                    AddChild(func, 1, "XAttribute - IEnumerable of XAttributes", InputSpace.GetAttributeElement(10, 1000).Elements().Attributes().ToArray());
                    AddChild(func, 1, "Mixed - Nodes and attributes", new XObject[] { new XAttribute("{b}xxx", "b_yyy"), new XElement("parent", new XElement("child", "child text")) });
                }

                public void ExecuteXElementVariation()
                {
                    XObject[] toReplace = Variation.Params as XObject[];
                    XNode newValue = new XText("text");
                    XElement xElem = new XElement("root", toReplace);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(xElem))
                        {
                            xElem.ReplaceAll(newValue);
                            TestLog.Compare(xElem.Nodes().Count() == 1, "Did not replace correctly");
                            TestLog.Compare(Object.ReferenceEquals(xElem.FirstNode, newValue), "Did not replace correctly");
                            TestLog.Compare(!xElem.HasAttributes, "ReplaceAll did not remove attributes");
                            xElem.Verify();
                            eHelper.Verify(toReplace.Length + 1);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "Element with attributes, with Element with attributes")]
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
                        TestLog.Compare(toReplace.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(toReplace.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }
            }
        }
    }
}