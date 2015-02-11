// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class EventsTests : XLinqTestCase
        {
            public partial class EventsXObjectValue : EventsBase
            {
                //[Variation(Priority = 0, Desc = "XText - change value")]
                public void XTextValue()
                {
                    XText toChange = new XText("Original Value");
                    String newValue = "New Value";
                    XElement xElem = new XElement("root", toChange);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(xElem))
                        {
                            using (EventsHelper textHelper = new EventsHelper(toChange))
                            {
                                toChange.Value = newValue;
                                TestLog.Compare(toChange.Value.Equals(newValue), "Value did not change");
                                xElem.Verify();
                                textHelper.Verify(XObjectChange.Value, toChange);
                            }
                            eHelper.Verify(XObjectChange.Value, toChange);
                        }
                        undo.Undo();
                        TestLog.Compare(XNode.DeepEquals(xElem, xElemOriginal), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 0, Desc = "XCData - change value")]
                public void XCDataValue()
                {
                    XCData toChange = new XCData("Original Value");
                    String newValue = "New Value";
                    XElement xElem = new XElement("root", toChange);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(xElem))
                        {
                            using (EventsHelper dataHelper = new EventsHelper(toChange))
                            {
                                toChange.Value = newValue;
                                TestLog.Compare(toChange.Value.Equals(newValue), "Value did not change");
                                xElem.Verify();
                                dataHelper.Verify(XObjectChange.Value, toChange);
                            }
                            eHelper.Verify(XObjectChange.Value, toChange);
                        }
                        undo.Undo();
                        TestLog.Compare(XNode.DeepEquals(xElem, xElemOriginal), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 0, Desc = "XComment - change value")]
                public void XCommentValue()
                {
                    XComment toChange = new XComment("Original Value");
                    String newValue = "New Value";
                    XElement xElem = new XElement("root", toChange);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(xElem))
                        {
                            using (EventsHelper comHelper = new EventsHelper(toChange))
                            {
                                toChange.Value = newValue;
                                TestLog.Compare(toChange.Value.Equals(newValue), "Value did not change");
                                xElem.Verify();
                                comHelper.Verify(XObjectChange.Value, toChange);
                            }
                            eHelper.Verify(XObjectChange.Value, toChange);
                        }
                        undo.Undo();
                        TestLog.Compare(XNode.DeepEquals(xElem, xElemOriginal), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 0, Desc = "XProcessingInstruction - change value")]
                public void XPIValue()
                {
                    XProcessingInstruction toChange = new XProcessingInstruction("target", "Original Value");
                    String newValue = "New Value";
                    XElement xElem = new XElement("root", toChange);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(xElem))
                        {
                            using (EventsHelper piHelper = new EventsHelper(toChange))
                            {
                                toChange.Data = newValue;
                                TestLog.Compare(toChange.Data.Equals(newValue), "Value did not change");
                                xElem.Verify();
                                piHelper.Verify(XObjectChange.Value, toChange);
                            }
                            eHelper.Verify(XObjectChange.Value, toChange);
                        }
                        undo.Undo();
                        TestLog.Compare(XNode.DeepEquals(xElem, xElemOriginal), "Undo did not work!");
                    }
                }

                //[Variation(Priority = 0, Desc = "XDocumentType - change public Id")]
                public void XDocTypePublicID()
                {
                    XDocumentType toChange = new XDocumentType("root", "", "", "");
                    XDocument xDoc = new XDocument(toChange);
                    XDocument xDocOriginal = new XDocument(xDoc);
                    using (EventsHelper docHelper = new EventsHelper(xDoc))
                    {
                        using (EventsHelper typeHelper = new EventsHelper(toChange))
                        {
                            toChange.PublicId = "newValue";
                            TestLog.Compare(toChange.PublicId.Equals("newValue"), "PublicID did not change");
                            typeHelper.Verify(XObjectChange.Value, toChange);
                        }
                        docHelper.Verify(XObjectChange.Value, toChange);
                    }
                }

                //[Variation(Priority = 0, Desc = "XDocumentType - change system Id")]
                public void XDocTypeSystemID()
                {
                    XDocumentType toChange = new XDocumentType("root", "", "", "");
                    XDocument xDoc = new XDocument(toChange);
                    XDocument xDocOriginal = new XDocument(xDoc);
                    using (EventsHelper docHelper = new EventsHelper(xDoc))
                    {
                        using (EventsHelper typeHelper = new EventsHelper(toChange))
                        {
                            toChange.SystemId = "newValue";
                            TestLog.Compare(toChange.SystemId.Equals("newValue"), "SystemID did not change");
                            typeHelper.Verify(XObjectChange.Value, toChange);
                        }
                        docHelper.Verify(XObjectChange.Value, toChange);
                    }
                }

                //[Variation(Priority = 0, Desc = "XDocumentType - change internal subset")]
                public void XDocTypeInternalSubset()
                {
                    XDocumentType toChange = new XDocumentType("root", "", "", "");
                    XDocument xDoc = new XDocument(toChange);
                    XDocument xDocOriginal = new XDocument(xDoc);
                    using (EventsHelper docHelper = new EventsHelper(xDoc))
                    {
                        using (EventsHelper typeHelper = new EventsHelper(toChange))
                        {
                            toChange.InternalSubset = "newValue";
                            TestLog.Compare(toChange.InternalSubset.Equals("newValue"), "Internal Subset did not change");
                            typeHelper.Verify(XObjectChange.Value, toChange);
                        }
                        docHelper.Verify(XObjectChange.Value, toChange);
                    }
                }
            }

            public partial class EventsXElementValue : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXElement(ExecuteXElementVariation);
                    base.DetermineChildren();
                }

                void VariationsForXElement(TestFunc func)
                {
                    AddChild(func, 0, "Empty element", new XElement("element"), "newValue");
                    AddChild(func, 0, "Element with string", new XElement("element", "value"), "newValue");
                    AddChild(func, 0, "Element with attributes", new XElement("element", new XAttribute("a", "aa")), "newValue");
                    AddChild(func, 0, "Element with nodes", new XElement("parent", new XElement("child", "child text")), "newValue");
                    AddChild(func, 1, "Element with IEnumerable of XNodes", new XElement("root", InputSpace.GetElement(100, 10).DescendantNodes()), "newValue");
                }

                public void ExecuteXElementVariation()
                {
                    XElement toChange = Variation.Params[0] as XElement;
                    int count = toChange.Nodes().Count();
                    String newValue = Variation.Params[1] as String;
                    XElement xElem = new XElement("root", toChange);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(toChange))
                        {
                            toChange.Value = newValue;
                            TestLog.Compare(toChange.Value == newValue, "Value change was not correct");
                            toChange.Verify();
                            eHelper.Verify(count + 1);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }
            }

            public partial class EventsXAttributeValue : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXAttribute(ExecuteXAttributeVariation);
                    base.DetermineChildren();
                }

                void VariationsForXAttribute(TestFunc func)
                {
                    AddChild(func, 0, "Attribute with empty string as value", new XAttribute("xxx", ""), "newValue");
                    AddChild(func, 0, "Attribute with value", new XAttribute("xxx", "yyy"), "newValue");
                    AddChild(func, 0, "Attribute with namespace and value", new XAttribute("{a}xxx", "a_yyy"), "newValue");
                }

                public void ExecuteXAttributeVariation()
                {
                    XAttribute toChange = Variation.Params[0] as XAttribute;
                    String newValue = Variation.Params[1] as String;
                    XElement xElem = new XElement("root", toChange);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(toChange))
                        {
                            toChange.Value = newValue;
                            TestLog.Compare(toChange.Value.Equals(newValue), "Value did not change");
                            xElem.Verify();
                            eHelper.Verify(XObjectChange.Value, toChange);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }
            }

            public partial class EventsXAttributeSetValue : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXAttribute(ExecuteXAttributeVariation);
                    base.DetermineChildren();
                }

                void VariationsForXAttribute(TestFunc func)
                {
                    AddChild(func, 0, "Empty value, string", new XAttribute("xxx", ""), "newValue");
                    AddChild(func, 0, "String, String", new XAttribute("xxx", "yyy"), "newValue");
                    AddChild(func, 0, "Attribute with namespace, String", new XAttribute("{a}xxx", "a_yyy"), "newValue");
                }

                public void ExecuteXAttributeVariation()
                {
                    XAttribute toChange = Variation.Params[0] as XAttribute;
                    Object newValue = Variation.Params[1];
                    XElement xElem = new XElement("root", toChange);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(toChange))
                        {
                            toChange.SetValue(newValue);
                            TestLog.Compare(newValue.Equals(toChange.Value), "Value did not change");
                            xElem.Verify();
                            eHelper.Verify(XObjectChange.Value, toChange);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }
            }

            public partial class EventsXElementSetValue : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXElement(ExecuteXElementVariation);
                    base.DetermineChildren();
                }

                void VariationsForXElement(TestFunc func)
                {
                    AddChild(func, 0, "Empty element, String", new XElement("element"), "newValue");
                    AddChild(func, 0, "String, String", new XElement("element", "value"), "newValue");
                    AddChild(func, 0, "Element with attributes, String", new XElement("element", new XAttribute("a", "aa")), "newValue");
                    AddChild(func, 0, "Element with child, String", new XElement("parent", new XElement("child", "child text")), "newValue");
                    AddChild(func, 1, "Element with nodes", new XElement("root", InputSpace.GetElement(100, 10).DescendantNodes()), "newValue");
                }

                public void ExecuteXElementVariation()
                {
                    XElement toChange = Variation.Params[0] as XElement;
                    int count = toChange.Nodes().Count();
                    Object newValue = Variation.Params[1];
                    XElement xElemOriginal = new XElement(toChange);
                    using (UndoManager undo = new UndoManager(toChange))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(toChange))
                        {
                            toChange.SetValue(newValue);
                            TestLog.Compare(newValue.Equals(toChange.Value), "Value change was not correct");
                            toChange.Verify();
                            eHelper.Verify(count + 1);
                        }
                        undo.Undo();
                        TestLog.Compare(toChange.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(toChange.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }
            }

            public partial class EventsXElementSetAttributeValue : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXAttribute(ExecuteXAttributeVariation);
                    base.DetermineChildren();
                }

                void VariationsForXAttribute(TestFunc func)
                {
                    AddChild(func, 0, "Empty value, string", new XAttribute("xxx", ""), "newValue");
                    AddChild(func, 0, "String, String", new XAttribute("xxx", "yyy"), "newValue");
                    AddChild(func, 0, "Attribute with namespace, String", new XAttribute("{a}xxx", "a_yyy"), "newValue");
                }

                public void ExecuteXAttributeVariation()
                {
                    XAttribute toChange = Variation.Params[0] as XAttribute;
                    Object newValue = Variation.Params[1];
                    XElement xElem = new XElement("root", toChange);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(toChange))
                        {
                            xElem.SetAttributeValue(toChange.Name, newValue);
                            TestLog.Compare(newValue.Equals(toChange.Value), "Value did not change");
                            xElem.Verify();
                            eHelper.Verify(XObjectChange.Value, toChange);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }
            }

            public partial class EventsXElementSetElementValue : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForAdd(ExecuteAddVariation);
                    VariationsForRemove(ExecuteRemoveVariation);
                    VariationsForValue(ExecuteValueVariation);
                    base.DetermineChildren();
                }

                void VariationsForAdd(TestFunc func)
                {
                    AddChild(func, 1, "Add - Empty element", null, new XElement("element"));
                    AddChild(func, 0, "Add - Element with value", new XComment("comment"), new XElement("element", "value"));
                    AddChild(func, 0, "Add - Element with attribute", new XCData("cdata"), new XElement("element", new XAttribute("a", "aa")));
                    AddChild(func, 0, "Add - Element with child", new XAttribute("a", "aa"), new XElement("parent", new XElement("child", "child text")));
                    AddChild(func, 1, "Add - Element with nodes", new XElement("element"), new XElement("nodes", InputSpace.GetElement(100, 10).DescendantNodes()));
                }

                void VariationsForRemove(TestFunc func)
                {
                    AddChild(func, 1, "Remove - Empty element", new XElement("element"));
                    AddChild(func, 0, "Remove - Element with value", new XElement("element", "value"));
                    AddChild(func, 0, "Remove - Element with attribute", new XElement("element", new XAttribute("a", "aa")));
                    AddChild(func, 0, "Remove - Element with child", new XElement("parent", new XElement("child", "child text")));
                    AddChild(func, 1, "Remove - Element with nodes", new XElement("nodes", InputSpace.GetElement(100, 10).DescendantNodes()));
                }

                void VariationsForValue(TestFunc func)
                {
                    AddChild(func, 1, "Value - Empty element", new XElement("element"), (double)10);
                    AddChild(func, 0, "Value - Element with value", new XElement("element", "value"), "newValue");
                    AddChild(func, 0, "Value - Element with attribute", new XElement("element", new XAttribute("a", "aa")), System.DateTime.Now);
                    AddChild(func, 0, "Value - Element with child", new XElement("parent", new XElement("child", "child text")), "Windows 8");
                    AddChild(func, 1, "Value - Element with nodes", new XElement("nodes", InputSpace.GetElement(100, 10).DescendantNodes()), "StackTrace");//Environment.StackTrace);
                }

                public void ExecuteAddVariation()
                {
                    XObject content = Variation.Params[0] as XObject;
                    XElement toAdd = Variation.Params[1] as XElement;
                    XElement xElem = new XElement("root", content);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(xElem))
                        {
                            xElem.SetElementValue(toAdd.Name, toAdd.Value);
                            xElem.Verify();
                            eHelper.Verify(XObjectChange.Add, xElem.Element(toAdd.Name));
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }

                public void ExecuteRemoveVariation()
                {
                    XElement content = Variation.Params[0] as XElement;
                    XElement xElem = new XElement("root", content);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(xElem))
                        {
                            xElem.SetElementValue(content.Name, null);
                            xElem.Verify();
                            eHelper.Verify(XObjectChange.Remove, content);
                        }
                        undo.Undo();
                        TestLog.Compare(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                        TestLog.Compare(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
                    }
                }

                public void ExecuteValueVariation()
                {
                    XElement content = Variation.Params[0] as XElement;
                    int count = content.Nodes().Count();
                    Object newValue = Variation.Params[1];
                    XElement xElem = new XElement("root", content);
                    XElement xElemOriginal = new XElement(xElem);
                    using (UndoManager undo = new UndoManager(xElem))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(xElem))
                        {
                            xElem.SetElementValue(content.Name, newValue);
                            // First all contents are removed and then new element with the value is added.
                            xElem.Verify();
                            eHelper.Verify(count + 1);
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