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
            //////////////////////////////////////////////////////////////////////////////////////////////////////
            /// <summary>
            /// EventsBaseClass: Base class that allows the dynamic addition of variations.  This makes it more
            /// flexiable to add dynamic params to the variation.
            /// </summary>
            //////////////////////////////////////////////////////////////////////////////////////////////////////
            public partial class EventsBase : XLinqTestCase
            {
                public void AddChild(TestFunc myFunction, int priority, string description, params object[] paramList)
                {
                    TestVariation myVariation = new TestVariation(myFunction);
                    VariationAttribute myAttribute = new VariationAttribute(description, paramList);
                    myAttribute.Priority = priority;
                    myVariation.Attribute = myAttribute;
                    this.AddChild(myVariation);
                }
            }

            public partial class EventsAddBeforeSelf : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXDocument(ExecuteXDocumentVariation);
                    VariationsForXElement(ExecuteXElementVariation);
                    base.DetermineChildren();
                }

                void VariationsForXDocument(TestFunc func)
                {
                    AddChild(func, 0, "XDocument - empty element before comment", new XNode[] { new XElement("element") }, new XComment("Comment"));
                    AddChild(func, 0, "XDocument - element before PI", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data"));
                    AddChild(func, 0, "XDocument - document type before element", new XNode[] { new XDocumentType("root", "", "", "") }, new XElement("root"));
                    AddChild(func, 0, "XDocument - PI before text", new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" "));
                    AddChild(func, 0, "XDocument - comment before document type", new XNode[] { new XComment("Comment") }, new XDocumentType("root", "", "", ""));
                    AddChild(func, 1, "XDocument - text before text", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" "));
                }

                void VariationsForXElement(TestFunc func)
                {
                    AddChild(func, 0, "XElement - empty element before text", new XNode[] { new XElement("element") }, new XText("some text"));
                    AddChild(func, 0, "XElement - element before PI", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data"));
                    AddChild(func, 0, "XElement - CData before element", new XNode[] { new XCData("x+y >= z-m") }, new XElement("child"));
                    AddChild(func, 0, "XElement - PI before text", new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" "));
                    AddChild(func, 0, "XElement - comment before CData", new XNode[] { new XComment("Comment") }, new XCData("x+y >= z-m"));
                    AddChild(func, 1, "XElement - text before empty text", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" "));
                    AddChild(func, 1, "XElement - IEnumerable of XNodes", InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), new XText(".."));
                }

                public void ExecuteXDocumentVariation()
                {
                    XNode[] toAdd = Variation.Params[0] as XNode[];
                    XNode contextNode = Variation.Params[1] as XNode;
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
                                TestLog.Compare(toAddList.SequenceEqual(contextNode.NodesBeforeSelf(), XNode.EqualityComparer), "Nodes not added correctly!");
                                nodeHelper.Verify(0);
                            }
                            docHelper.Verify(XObjectChange.Add, toAdd);
                        }
                        undo.Undo();
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                public void ExecuteXElementVariation()
                {
                    XNode[] toAdd = Variation.Params[0] as XNode[];
                    XNode contextNode = Variation.Params[1] as XNode;
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
                                TestLog.Compare(toAddList.SequenceEqual(contextNode.NodesBeforeSelf(), XNode.EqualityComparer), "Nodes not added correctly!");
                                nodeHelper.Verify(0);
                            }
                            elemHelper.Verify(XObjectChange.Add, toAdd);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Add null")]
                public void AddNull()
                {
                    XElement xElem = new XElement("root", "text");
                    EventsHelper elemHelper = new EventsHelper(xElem);
                    xElem.FirstNode.AddBeforeSelf(null);
                    elemHelper.Verify(0);
                }

                //[Variation(Priority = 1, Desc = "XElement - Working on the text nodes 1.")]
                public void WorkOnTextNodes1()
                {
                    XElement elem = new XElement("A", "text2");
                    XNode n = elem.FirstNode;
                    using (UndoManager undo = new UndoManager(elem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(elem))
                        {
                            n.AddBeforeSelf("text0");
                            TestLog.Compare(elem.Value, "text0text2", "Did not concat text nodes correctly");
                            n.AddBeforeSelf("text1");
                            TestLog.Compare(elem.Value, "text0text1text2", "Did not concat text nodes correctly");
                            eHelper.Verify(new XObjectChange[] { XObjectChange.Add, XObjectChange.Value });
                        }
                        undo.Undo();
                        TestLog.Compare(elem.Value, "text2", "Undo did not work");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Working on the text nodes 2.")]
                public void WorkOnTextNodes2()
                {
                    XElement elem = new XElement("A", "text2");
                    XNode n = elem.FirstNode;
                    using (UndoManager undo = new UndoManager(elem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(elem))
                        {
                            n.AddBeforeSelf("text0", "text1");
                            TestLog.Compare(elem.Value, "text0text1text2", "Did not concat text nodes correctly");
                            eHelper.Verify(XObjectChange.Add);
                        }
                        undo.Undo();
                        TestLog.Compare(elem.Value, "text2", "Undo did not work");
                    }
                }
            }

            public partial class EventsAddAfterSelf : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXDocument(ExecuteXDocumentVariation);
                    VariationsForXElement(ExecuteXElementVariation);
                    base.DetermineChildren();
                }

                void VariationsForXDocument(TestFunc func)
                {
                    AddChild(func, 0, "XDocument - empty element after comment", new XNode[] { new XElement("element") }, new XComment("Comment"));
                    AddChild(func, 0, "XDocument - element after PI", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data"));
                    AddChild(func, 0, "XDocument - document type after text", new XNode[] { new XDocumentType("root", "", "", "") }, new XText(" "));
                    AddChild(func, 0, "XDocument - PI after text", new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" "));
                    AddChild(func, 0, "XDocument - comment after element", new XNode[] { new XComment("Comment") }, new XElement("root"));
                    AddChild(func, 1, "XDocument - text after text", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" "));
                }

                void VariationsForXElement(TestFunc func)
                {
                    AddChild(func, 0, "XElement - empty element after text", new XNode[] { new XElement("element") }, new XText("some text"));
                    AddChild(func, 0, "XElement - element after PI", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data"));
                    AddChild(func, 0, "XElement - CData after element", new XNode[] { new XCData("x+y >= z-m") }, new XElement("child"));
                    AddChild(func, 0, "XElement - PI after text", new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" "));
                    AddChild(func, 0, "XElement - comment after CData", new XNode[] { new XComment("Comment") }, new XCData("x+y >= z-m"));
                    AddChild(func, 1, "XElement - text after empty text", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" "));
                    AddChild(func, 1, "XElement - IEnumerable of XNodes", InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), new XText(".."));
                }

                public void ExecuteXDocumentVariation()
                {
                    XNode[] toAdd = Variation.Params[0] as XNode[];
                    XNode contextNode = Variation.Params[1] as XNode;
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
                                TestLog.Compare(toAddList.SequenceEqual(contextNode.NodesAfterSelf(), XNode.EqualityComparer), "Nodes not added correctly!");
                                nodeHelper.Verify(0);
                            }
                            docHelper.Verify(XObjectChange.Add, toAdd);
                        }
                        undo.Undo();
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                public void ExecuteXElementVariation()
                {
                    XNode[] toAdd = Variation.Params[0] as XNode[];
                    XNode contextNode = Variation.Params[1] as XNode;
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
                                TestLog.Compare(toAddList.SequenceEqual(contextNode.NodesAfterSelf(), XNode.EqualityComparer), "Nodes not added correctly!");
                                nodeHelper.Verify(0);
                            }
                            elemHelper.Verify(XObjectChange.Add, toAdd);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement-Add null")]
                public void AddNull()
                {
                    XElement xElem = new XElement("root", "text");
                    EventsHelper elemHelper = new EventsHelper(xElem);
                    xElem.LastNode.AddAfterSelf(null);
                    elemHelper.Verify(0);
                }

                //[Variation(Priority = 1, Desc = "XElement - Working on the text nodes 1.")]
                public void WorkOnTextNodes1()
                {
                    XElement elem = new XElement("A", "text2");
                    XNode n = elem.FirstNode;
                    using (UndoManager undo = new UndoManager(elem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(elem))
                        {
                            n.AddAfterSelf("text0");
                            TestLog.Compare(elem.Value, "text2text0", "Did not concat text nodes correctly");
                            n.AddAfterSelf("text1");
                            TestLog.Compare(elem.Value, "text2text0text1", "Did not concat text nodes correctly");
                            eHelper.Verify(new XObjectChange[] { XObjectChange.Value, XObjectChange.Value });
                        }
                        undo.Undo();
                        TestLog.Compare(elem.Value, "text2", "Undo did not work");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Working on the text nodes 2.")]
                public void WorkOnTextNodes2()
                {
                    XElement elem = new XElement("A", "text2");
                    XNode n = elem.FirstNode;
                    using (UndoManager undo = new UndoManager(elem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(elem))
                        {
                            n.AddAfterSelf("text0", "text1");
                            TestLog.Compare(elem.Value, "text2text0text1", "Did not concat text nodes correctly");
                            eHelper.Verify(XObjectChange.Value);
                        }
                        undo.Undo();
                        TestLog.Compare(elem.Value, "text2", "Undo did not work");
                    }
                }
            }

            public partial class EventsAddFirst : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXDocument(ExecuteXDocumentVariation);
                    VariationsForXElement(ExecuteXElementVariation);
                    base.DetermineChildren();
                }

                void VariationsForXDocument(TestFunc func)
                {
                    AddChild(func, 1, "XDocument - empty element in empty doc", new XNode[] { new XElement("element") }, null);
                    AddChild(func, 0, "XDocument - element in empty doc", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, null);
                    AddChild(func, 0, "XDocument - document type in empty doc", new XNode[] { new XDocumentType("root", "", "", "") }, null);
                    AddChild(func, 0, "XDocument - PI in empty doc", new XNode[] { new XProcessingInstruction("PI", "Data") }, null);
                    AddChild(func, 0, "XDocument - comment in empty doc", new XNode[] { new XComment("Comment") }, null);
                    AddChild(func, 0, "XDocument - text in empty doc", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, null);
                    AddChild(func, 0, "XDocument - empty element in doc with comment", new XNode[] { new XElement("element") }, new XComment("Comment"));
                    AddChild(func, 0, "XDocument - element in doc with PI", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data"));
                    AddChild(func, 0, "XDocument - document type in doc with text", new XNode[] { new XDocumentType("root", "", "", "") }, new XText(" "));
                    AddChild(func, 0, "XDocument - PI in doc with text", new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" "));
                    AddChild(func, 0, "XDocument - comment in doc with element", new XNode[] { new XComment("Comment") }, new XElement("root"));
                    AddChild(func, 1, "XDocument - text in doc with empty text", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" "));
                }

                void VariationsForXElement(TestFunc func)
                {
                    AddChild(func, 1, "XElement - empty element in empty element", new XNode[] { new XElement("element") }, null);
                    AddChild(func, 0, "XElement - element in empty element", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, null);
                    AddChild(func, 0, "XElement - CData in empty element", new XNode[] { new XCData("x+y >= z-m") }, null);
                    AddChild(func, 0, "XElement - PI in empty element", new XNode[] { new XProcessingInstruction("PI", "Data") }, null);
                    AddChild(func, 0, "XElement - comment in empty element", new XNode[] { new XComment("Comment") }, null);
                    AddChild(func, 0, "XElement - text in empty element", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, null);
                    AddChild(func, 1, "XElement - IEnumerable of XNodes in empty element", InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), null);
                    AddChild(func, 0, "XElement - empty element in element with text", new XNode[] { new XElement("element") }, new XText("some text"));
                    AddChild(func, 0, "XElement - element in element with PI", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data"));
                    AddChild(func, 0, "XElement - CData in element with child element", new XNode[] { new XCData("x+y >= z-m") }, new XElement("child"));
                    AddChild(func, 0, "XElement - PI in element with text", new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" "));
                    AddChild(func, 0, "XElement - comment in element with CData", new XNode[] { new XComment("Comment") }, new XCData("x+y >= z-m"));
                    AddChild(func, 1, "XElement - text in element with empty text", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" "));
                    AddChild(func, 1, "XElement - IEnumerable of XNodes in element with text", InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), new XText(".."));
                }

                public void ExecuteXDocumentVariation()
                {
                    XNode[] toAdd = Variation.Params[0] as XNode[];
                    XNode contextNode = Variation.Params[1] as XNode;
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
                            TestLog.Compare(toAddList.SequenceEqual(allNodes, XNode.EqualityComparer), "Nodes not added correctly!");
                            docHelper.Verify(XObjectChange.Add, toAdd);
                        }
                        undo.Undo();
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                public void ExecuteXElementVariation()
                {
                    XNode[] toAdd = Variation.Params[0] as XNode[];
                    XNode contextNode = Variation.Params[1] as XNode;
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
                            TestLog.Compare(toAddList.SequenceEqual(allNodes, XNode.EqualityComparer), "Nodes not added correctly!");
                            elemHelper.Verify(XObjectChange.Add, toAdd);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Add null")]
                public void AddNull()
                {
                    XElement xElem = new XElement("root", "text");
                    EventsHelper elemHelper = new EventsHelper(xElem);
                    xElem.AddFirst(null);
                    elemHelper.Verify(0);
                }

                //[Variation(Priority = 1, Desc = "XElement - Working on the text nodes 1.")]
                public void WorkOnTextNodes1()
                {
                    XElement elem = new XElement("A", "text2");
                    using (UndoManager undo = new UndoManager(elem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(elem))
                        {
                            elem.AddFirst("text0");
                            TestLog.Compare(elem.Value, "text0text2", "Did not concat text nodes correctly");
                            elem.AddFirst("text1");
                            TestLog.Compare(elem.Value, "text1text0text2", "Did not concat text nodes correctly");
                            eHelper.Verify(new XObjectChange[] { XObjectChange.Add, XObjectChange.Add });
                        }
                        undo.Undo();
                        TestLog.Compare(elem.Value, "text2", "Undo did not work");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Working on the text nodes 2.")]
                public void WorkOnTextNodes2()
                {
                    XElement elem = new XElement("A", "text2");
                    using (UndoManager undo = new UndoManager(elem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(elem))
                        {
                            elem.AddFirst("text0", "text1");
                            TestLog.Compare(elem.Value, "text0text1text2", "Did not concat text nodes correctly");
                            eHelper.Verify(XObjectChange.Add);
                        }
                        undo.Undo();
                        TestLog.Compare(elem.Value, "text2", "Undo did not work");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Change content in the pre-event handler")]
                public void StringContent()
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

                    try
                    {
                        element.AddFirst("");
                    }
                    catch (InvalidOperationException)
                    {
                        element.Verify();
                        return;
                    }

                    throw new TestFailedException("Should have thrown an InvalidOperationException");
                }

                //[Variation(Priority = 1, Desc = "XElement - Change xnode's parent in the pre-event handler")]
                public void ParentedXNode()
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

                    try
                    {
                        element.AddFirst(child);
                    }
                    catch (InvalidOperationException)
                    {
                        element.Verify();
                        TestLog.Compare(element.Element("Add") == null, "Added the element, operation should have been aborted");
                        return;
                    }

                    throw new TestFailedException("Should have thrown an InvalidOperationException");
                }
            }

            public partial class EventsAdd : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXDocument(ExecuteXDocumentVariation);
                    VariationsForXElement(ExecuteXElementVariation);
                    VariationsForXAttribute(ExecuteXAttributeVariation);
                    base.DetermineChildren();
                }

                void VariationsForXDocument(TestFunc func)
                {
                    AddChild(func, 1, "XDocument - empty element in empty doc", new XNode[] { new XElement("element") }, null);
                    AddChild(func, 0, "XDocument - element in empty doc", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, null);
                    AddChild(func, 0, "XDocument - document type in empty doc", new XNode[] { new XDocumentType("root", "", "", "") }, null);
                    AddChild(func, 0, "XDocument - PI in empty doc", new XNode[] { new XProcessingInstruction("PI", "Data") }, null);
                    AddChild(func, 0, "XDocument - comment in empty doc", new XNode[] { new XComment("Comment") }, null);
                    AddChild(func, 0, "XDocument - text in empty doc", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, null);
                    AddChild(func, 0, "XDocument - empty element in doc with comment", new XNode[] { new XElement("element") }, new XComment("Comment"));
                    AddChild(func, 0, "XDocument - element in doc with PI", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data"));
                    AddChild(func, 0, "XDocument - document type in doc with text", new XNode[] { new XDocumentType("root", "", "", "") }, new XText(" "));
                    AddChild(func, 0, "XDocument - PI in doc with text", new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" "));
                    AddChild(func, 0, "XDocument - comment in doc with element", new XNode[] { new XComment("Comment") }, new XElement("root"));
                    AddChild(func, 1, "XDocument - text in doc with empty text", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" "));
                }

                void VariationsForXElement(TestFunc func)
                {
                    AddChild(func, 1, "XElement - empty element in empty element", new XNode[] { new XElement("element") }, null);
                    AddChild(func, 0, "XElement - element in empty element", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, null);
                    AddChild(func, 0, "XElement - CData in empty element", new XNode[] { new XCData("x+y >= z-m") }, null);
                    AddChild(func, 0, "XElement - PI in empty element", new XNode[] { new XProcessingInstruction("PI", "Data") }, null);
                    AddChild(func, 0, "XElement - comment in empty element", new XNode[] { new XComment("Comment") }, null);
                    AddChild(func, 0, "XElement - text in empty element", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, null);
                    AddChild(func, 1, "XElement - IEnumerable of XNodes in empty element", InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), null);
                    AddChild(func, 0, "XElement - empty element in element with text", new XNode[] { new XElement("element") }, new XText("some text"));
                    AddChild(func, 0, "XElement - element in element with PI", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, new XProcessingInstruction("PI", "Data"));
                    AddChild(func, 0, "XElement - CData in element with child element", new XNode[] { new XCData("x+y >= z-m") }, new XElement("child"));
                    AddChild(func, 0, "XElement - PI in element with text", new XNode[] { new XProcessingInstruction("PI", "Data") }, new XText(" "));
                    AddChild(func, 0, "XElement - comment in element with CData", new XNode[] { new XComment("Comment") }, new XCData("x+y >= z-m"));
                    AddChild(func, 0, "XElement - text in element with empty text", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, new XText(" "));
                    AddChild(func, 1, "XElement - IEnumerable of XNodes in element with text", InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), new XText(".."));
                }

                void VariationsForXAttribute(TestFunc func)
                {
                    AddChild(func, 0, "XAttribute - attribute in element with no attributes", new XAttribute[] { new XAttribute("xxx", "yyy") }, null);
                    AddChild(func, 0, "XAttribute - attribute with namespace in element with no attributes", new XAttribute[] { new XAttribute("{a}xxx", "a_yyy") }, null);
                    AddChild(func, 1, "XAttribute - IEnumerable of XAttributes in element with no attributes", InputSpace.GetElement(100, 10).Attributes().ToArray(), null);
                    AddChild(func, 0, "XAttribute - attribute in element with attribute", new XAttribute[] { new XAttribute("xxx", "yyy") }, new XAttribute("a", "aa"));
                    AddChild(func, 0, "XAttribute - attribute with namespace in element with attributes", new XAttribute[] { new XAttribute("{b}xxx", "b_yyy") }, new XAttribute("a", "aa"));
                    AddChild(func, 1, "XAttribute - IEnumerable of XAttributes in element with no attributes", InputSpace.GetElement(100, 10).Attributes().ToArray(), new XAttribute("a", "aa"));
                }

                public void ExecuteXDocumentVariation()
                {
                    XNode[] toAdd = Variation.Params[0] as XNode[];
                    XNode contextNode = Variation.Params[1] as XNode;
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
                            TestLog.Compare(toAddList.SequenceEqual(allNodes, XNode.EqualityComparer), "Nodes not added correctly!");
                            docHelper.Verify(XObjectChange.Add, toAdd);
                        }
                        undo.Undo();
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                public void ExecuteXElementVariation()
                {
                    XNode[] toAdd = Variation.Params[0] as XNode[];
                    XNode contextNode = Variation.Params[1] as XNode;
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
                            TestLog.Compare(toAddList.SequenceEqual(allNodes, XNode.EqualityComparer), "Nodes not added correctly!");
                            elemHelper.Verify(XObjectChange.Add, toAdd);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }

                public void ExecuteXAttributeVariation()
                {
                    XAttribute[] toAdd = Variation.Params[0] as XAttribute[];
                    XAttribute contextNode = Variation.Params[1] as XAttribute;
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
                            TestLog.Compare(toAddList.SequenceEqual(allNodes, Helpers.MyAttributeComparer), "Attributes not added correctly!");
                            elemHelper.Verify(XObjectChange.Add, toAdd);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "XAttribute - Add at each level, nested elements")]
                public void XAttributeAddAtDeepLevel()
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
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Add at each level, nested elements")]
                public void XElementAddAtDeepLevel()
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
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Text node incarnation.")]
                public void WorkTextNodes()
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
                        TestLog.Compare(XNode.DeepEquals(elem, xElemOriginal), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Working on the text nodes 1.")]
                public void WorkOnTextNodes1()
                {
                    XElement elem = new XElement("A", "text2");
                    using (UndoManager undo = new UndoManager(elem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(elem))
                        {
                            elem.Add("text0");
                            TestLog.Compare(elem.Value, "text2text0", "Did not concat text nodes correctly");
                            elem.Add("text1");
                            TestLog.Compare(elem.Value, "text2text0text1", "Did not concat text nodes correctly");
                            eHelper.Verify(new XObjectChange[] { XObjectChange.Value, XObjectChange.Value });
                        }
                        undo.Undo();
                        TestLog.Compare(elem.Value, "text2", "Undo did not work");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Working on the text nodes 2.")]
                public void WorkOnTextNodes2()
                {
                    XElement elem = new XElement("A", "text2");
                    XElement xElemOriginal = new XElement(elem);
                    using (UndoManager undo = new UndoManager(elem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(elem))
                        {
                            elem.Add("text0", "text1");
                            TestLog.Compare(elem.Value, "text2text0text1", "Did not concat text nodes correctly");
                            eHelper.Verify(new XObjectChange[] { XObjectChange.Value, XObjectChange.Value });
                        }
                        undo.Undo();
                        TestLog.Compare(elem.Value, "text2", "Undo did not work");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Change content in the pre-event handler")]
                public void StringContent()
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

                    try
                    {
                        element.Add("");
                    }
                    catch (InvalidOperationException)
                    {
                        element.Verify();
                        return;
                    }

                    throw new TestFailedException("Should have thrown an InvalidOperationException");
                }

                //[Variation(Priority = 1, Desc = "XElement - Change xnode's parent in the pre-event handler")]
                public void ParentedXNode()
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

                    try
                    {
                        element.Add(child);
                    }
                    catch (InvalidOperationException)
                    {
                        element.Verify();
                        TestLog.Compare(element.Element("Add") == null, "Added the element, operation should have been aborted");
                        return;
                    }

                    throw new TestFailedException("Should have thrown an InvalidOperationException");
                }

                //[Variation(Priority = 1, Desc = "XElement - Change attribute's parent in the pre-event handler")]
                public void ParentedAttribute()
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

                    try
                    {
                        element.Add(child);
                    }
                    catch (InvalidOperationException)
                    {
                        element.Verify();
                        TestLog.Compare(element.Attribute("Add") == null, "Added the attribute, operation should have been aborted");
                        return;
                    }

                    throw new TestFailedException("Should have thrown an InvalidOperationException");
                }
            }
        }
    }
}