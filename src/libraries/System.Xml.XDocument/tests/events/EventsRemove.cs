// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class EventsTests : XLinqTestCase
        {
            public partial class EventsRemove : EventsBase
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
                    AddChild(func, 1, "XDocument - empty element", new XNode[] { new XElement("element") }, 0);
                    AddChild(func, 0, "XDocument - element with child", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, 0);
                    AddChild(func, 0, "XDocument - document type", new XNode[] { new XDocumentType("root", "", "", "") }, 0);
                    AddChild(func, 0, "XDocument - PI", new XNode[] { new XProcessingInstruction("PI", "Data") }, 0);
                    AddChild(func, 0, "XDocument - comment", new XNode[] { new XComment("Comment") }, 0);
                    AddChild(func, 1, "XDocument - text nodes", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, 1);
                }

                void VariationsForXElement(TestFunc func)
                {
                    AddChild(func, 1, "XElement - empty element", new XNode[] { new XElement("element") }, 0);
                    AddChild(func, 0, "XElement - first element", new XNode[] { new XElement("parent", new XElement("child", "child text")) }, 0);
                    AddChild(func, 0, "XElement - last element", new XNode[] { new XElement("parent", "parent text"), new XElement("child", "child text") }, 1);
                    AddChild(func, 0, "XElement - middle node", new XNode[] { new XElement("parent", "parent text"), new XText("text"), new XElement("child", "child text") }, 1);
                    AddChild(func, 0, "XElement - CData", new XNode[] { new XCData("x+y >= z-m") }, 0);
                    AddChild(func, 0, "XElement - PI", new XNode[] { new XProcessingInstruction("PI", "Data") }, 0);
                    AddChild(func, 0, "XElement - comment", new XNode[] { new XComment("Comment") }, 0);
                    AddChild(func, 0, "XElement - first text nodes", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, 0);
                    AddChild(func, 1, "XElement - second text nodes", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, 1);
                    AddChild(func, 1, "XElement - third text nodes", new XNode[] { new XText(""), new XText(" "), new XText("\t") }, 2);
                    AddChild(func, 1, "XElement - IEnumerable of XNodes", InputSpace.GetElement(100, 10).DescendantNodes().ToArray(), 50);
                }

                void VariationsForXAttribute(TestFunc func)
                {
                    AddChild(func, 0, "XAttribute - only attribute", new XAttribute[] { new XAttribute("xxx", "yyy") }, 0);
                    AddChild(func, 0, "XAttribute - only attribute with namespace", new XAttribute[] { new XAttribute("{a}xxx", "a_yyy") }, 0);
                    AddChild(func, 0, "XAttribute - mulitple attributes", new XAttribute[] { new XAttribute("xxx", "yyy"), new XAttribute("a", "aa") }, 1);
                    AddChild(func, 1, "XAttribute - multiple attributes with namespace", new XAttribute[] { new XAttribute("{b}xxx", "b_yyy"), new XAttribute("{a}xxx", "a_yyy") }, 0);
                    AddChild(func, 1, "XAttribute - IEnumerable of XAttributes", InputSpace.GetAttributeElement(10, 1000).Elements().Attributes().ToArray(), 10);
                }

                public void ExecuteXDocumentVariation()
                {
                    XNode[] content = Variation.Params[0] as XNode[];
                    int index = (int)Variation.Params[1];
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
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                public void ExecuteXElementVariation()
                {
                    XNode[] content = Variation.Params[0] as XNode[];
                    int index = (int)Variation.Params[1];
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
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }

                public void ExecuteXAttributeVariation()
                {
                    XAttribute[] content = Variation.Params[0] as XAttribute[];
                    int index = (int)Variation.Params[1];
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
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Working on the text nodes 1.")]
                public void WorkOnTextNodes1()
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
                            TestLog.Compare(elem.IsEmpty, "Did not remove correctly");
                        }
                        undo.Undo();
                        TestLog.Compare(elem.Value, "text2text0text1", "Undo did not work");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Working on the text nodes 2.")]
                public void WorkOnTextNodes2()
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
                            TestLog.Compare(!elem.IsEmpty, "Did not remove correctly");
                            elem.FirstNode.Remove();
                            eHelper.Verify(XObjectChange.Remove);
                            TestLog.Compare(!elem.IsEmpty, "Did not remove correctly");
                            elem.FirstNode.Remove();
                            eHelper.Verify(XObjectChange.Remove);
                            TestLog.Compare(elem.IsEmpty, "Did not remove correctly");
                        }
                        undo.Undo();
                        TestLog.Compare(elem.Value, "text1text0text2", "Undo did not work");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Working on the text nodes 3.")]
                public void WorkOnTextNodes3()
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
                            TestLog.Compare(!elem.IsEmpty, "Did not remove correctly");
                            elem.FirstNode.Remove();
                            eHelper.Verify(XObjectChange.Remove);
                            TestLog.Compare(!elem.IsEmpty, "Did not remove correctly");
                            elem.FirstNode.Remove();
                            eHelper.Verify(XObjectChange.Remove);
                            TestLog.Compare(elem.IsEmpty, "Did not remove correctly");
                        }
                        undo.Undo();
                        TestLog.Compare(elem.Value, "text1text0text2", "Undo did not work");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Working on the text nodes 4.")]
                public void WorkOnTextNodes4()
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
                            TestLog.Compare(elem.IsEmpty, "Did not remove correctly");
                        }
                        undo.Undo();
                        TestLog.Compare(elem.Value, "text2text0text1", "Undo did not work");
                    }
                }

                //[Variation(Priority = 1, Desc = "XAttribute - Remove one attribute at a time")]
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
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Remove one element (with children) at a time")]
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
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "Remove Sequence - IEnumerable<XAttribute>")]
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
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "Remove Sequence - IEnumerable<XElement>")]
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
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - Change xnode's parent in the pre-event handler")]
                public void ParentedXNode()
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

                    try
                    {
                        child.Remove();
                    }
                    catch (InvalidOperationException)
                    {
                        element.Verify();
                        return;
                    }

                    throw new TestFailedException("Should have thrown an InvalidOperationException");
                }

                //[Variation(Priority = 1, Desc = "XElement - Change attribute's parent in the pre-event handler")]
                public void ParentedAttribute()
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

                    try
                    {
                        child.Remove();
                    }
                    catch (InvalidOperationException)
                    {
                        element.Verify();
                        return;
                    }

                    throw new TestFailedException("Should have thrown an InvalidOperationException");
                }
            }

            public partial class EventsRemoveNodes : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXDocument(ExecuteXDocumentVariation);
                    VariationsForXElement(ExecuteXElementVariation);
                    base.DetermineChildren();
                }

                void VariationsForXDocument(TestFunc func)
                {
                    AddChild(func, 0, "XDocument - empty element", new XNode[] { new XElement("element") });
                    AddChild(func, 0, "XDocument - element with child", new XNode[] { new XElement("parent", new XElement("child", "child text")) });
                    AddChild(func, 0, "XDocument - document type", new XNode[] { new XDocumentType("root", "", "", "") });
                    AddChild(func, 0, "XDocument - PI", new XNode[] { new XProcessingInstruction("PI", "Data") });
                    AddChild(func, 0, "XDocument - comment", new XNode[] { new XComment("Comment") });
                    AddChild(func, 0, "XDocument - text nodes", new XNode[] { new XText(""), new XText(" "), new XText("\t") });
                }

                void VariationsForXElement(TestFunc func)
                {
                    AddChild(func, 0, "XElement - empty element", new XNode[] { new XElement("element") });
                    AddChild(func, 0, "XElement - element with child", new XNode[] { new XElement("parent", new XElement("child", "child text")) });
                    AddChild(func, 0, "XElement - CData", new XNode[] { new XCData("x+y >= z-m") });
                    AddChild(func, 0, "XElement - PI", new XNode[] { new XProcessingInstruction("PI", "Data") });
                    AddChild(func, 0, "XElement - comment", new XNode[] { new XComment("Comment") });
                    AddChild(func, 0, "XElement - text nodes", new XNode[] { new XText(""), new XText(" "), new XText("\t") });
                    AddChild(func, 1, "XElement - IEnumerable of XNodes", InputSpace.GetElement(100, 10).DescendantNodes().ToArray());
                }

                public void ExecuteXDocumentVariation()
                {
                    XNode[] content = Variation.Params as XNode[];
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
                        TestLog.Compare(XNode.DeepEquals(xDoc, xDocOriginal), "Undo did not work!");
                    }
                }

                public void ExecuteXElementVariation()
                {
                    XNode[] content = Variation.Params as XNode[];
                    XElement xElem = new XElement("root", InputSpace.GetAttributeElement(10, 1000).Elements().Attributes(), content);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper elemHelper = new EventsHelper(xElem))
                        {
                            xElem.RemoveNodes();
                            TestLog.Compare(xElem.IsEmpty, "Not all content were removed");
                            TestLog.Compare(xElem.HasAttributes, "RemoveNodes removed the attributes");
                            xElem.Verify();
                            elemHelper.Verify(XObjectChange.Remove, content);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 1, Desc = "XElement - empty element special case <A></A>")]
                public void RemoveNodesBug()
                {
                    XElement e = XElement.Parse(@"<A></A>");
                    EventsHelper eHelper = new EventsHelper(e);
                    e.RemoveNodes();
                    //eHelper.Verify(XObjectChange.Remove, e.Nodes());
                    eHelper.Verify(XObjectChange.Value);
                }

                //[Variation(Priority = 1, Desc = "XElement - Change node value in the pre-event handler")]
                public void ChangeContentBeforeRemove()
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

                    try
                    {
                        element.RemoveNodes();
                    }
                    catch (InvalidOperationException)
                    {
                        element.Verify();
                        TestLog.Compare(element.Element("Add") != null, "Operation should have been aborted");
                        return;
                    }

                    throw new TestFailedException("Should have thrown an InvalidOperationException");
                }

                //[Variation(Priority = 1, Desc = "XElement - Change nodes in the pre-event handler")]
                public void RemoveNodeInPreEvent()
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

                    try
                    {
                        element.RemoveNodes();
                    }
                    catch (InvalidOperationException)
                    {
                        element.Verify();
                        return;
                    }

                    throw new TestFailedException("Should have thrown an InvalidOperationException");
                }

                //[Variation(Priority = 1, Desc = "XElement - Change attributes in the pre-event handler")]
                public void RemoveAttributeInPreEvent()
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

                    try
                    {
                        element.RemoveAttributes();
                    }
                    catch (InvalidOperationException)
                    {
                        element.Verify();
                        return;
                    }

                    throw new TestFailedException("Should have thrown an InvalidOperationException");
                }
            }

            public partial class EventsRemoveAll : EventsBase
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
                    XObject[] content = Variation.Params as XObject[];
                    XElement xElem = new XElement("root", content);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper elemHelper = new EventsHelper(xElem))
                        {
                            xElem.RemoveAll();
                            TestLog.Compare(xElem.IsEmpty, "Not all content were removed");
                            TestLog.Compare(!xElem.HasAttributes, "RemoveAll did not remove attributes");
                            xElem.Verify();
                            elemHelper.Verify(XObjectChange.Remove, content);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }
            }
        }
    }
}