// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace CoreXml.Test.XLinq.FunctionalTests.EventsTests
{
    public class EventsXObjectValue
    {
        [Fact]
        public void XTextChangeValue()
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
                        Assert.True(toChange.Value.Equals(newValue), "Value did not change");
                        xElem.Verify();
                        textHelper.Verify(XObjectChange.Value, toChange);
                    }
                    eHelper.Verify(XObjectChange.Value, toChange);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xElem, xElemOriginal), "Undo did not work!");
            }
        }

        [Fact]
        public void XCDataChangeValue()
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
                        Assert.True(toChange.Value.Equals(newValue), "Value did not change");
                        xElem.Verify();
                        dataHelper.Verify(XObjectChange.Value, toChange);
                    }
                    eHelper.Verify(XObjectChange.Value, toChange);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xElem, xElemOriginal), "Undo did not work!");
            }
        }

        [Fact]
        public void XCommentChangeValue()
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
                        Assert.True(toChange.Value.Equals(newValue), "Value did not change");
                        xElem.Verify();
                        comHelper.Verify(XObjectChange.Value, toChange);
                    }
                    eHelper.Verify(XObjectChange.Value, toChange);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xElem, xElemOriginal), "Undo did not work!");
            }
        }

        [Fact]
        public void XProcessingInstructionChangeValue()
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
                        Assert.True(toChange.Data.Equals(newValue), "Value did not change");
                        xElem.Verify();
                        piHelper.Verify(XObjectChange.Value, toChange);
                    }
                    eHelper.Verify(XObjectChange.Value, toChange);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(xElem, xElemOriginal), "Undo did not work!");
            }
        }

        [Fact]
        public void XDocumentTypeChangePublicId()
        {
            XDocumentType toChange = new XDocumentType("root", "", "", "");
            XDocument xDoc = new XDocument(toChange);
            XDocument xDocOriginal = new XDocument(xDoc);
            using (EventsHelper docHelper = new EventsHelper(xDoc))
            {
                using (EventsHelper typeHelper = new EventsHelper(toChange))
                {
                    toChange.PublicId = "newValue";
                    Assert.True(toChange.PublicId.Equals("newValue"), "PublicID did not change");
                    typeHelper.Verify(XObjectChange.Value, toChange);
                }
                docHelper.Verify(XObjectChange.Value, toChange);
            }
        }

        [Fact]
        public void XDocumentTypeChangeSystemId()
        {
            XDocumentType toChange = new XDocumentType("root", "", "", "");
            XDocument xDoc = new XDocument(toChange);
            XDocument xDocOriginal = new XDocument(xDoc);
            using (EventsHelper docHelper = new EventsHelper(xDoc))
            {
                using (EventsHelper typeHelper = new EventsHelper(toChange))
                {
                    toChange.SystemId = "newValue";
                    Assert.True(toChange.SystemId.Equals("newValue"), "SystemID did not change");
                    typeHelper.Verify(XObjectChange.Value, toChange);
                }
                docHelper.Verify(XObjectChange.Value, toChange);
            }
        }

        [Fact]
        public void XDocumentTypeChangeInternalSubset()
        {
            XDocumentType toChange = new XDocumentType("root", "", "", "");
            XDocument xDoc = new XDocument(toChange);
            XDocument xDocOriginal = new XDocument(xDoc);
            using (EventsHelper docHelper = new EventsHelper(xDoc))
            {
                using (EventsHelper typeHelper = new EventsHelper(toChange))
                {
                    toChange.InternalSubset = "newValue";
                    Assert.True(toChange.InternalSubset.Equals("newValue"), "Internal Subset did not change");
                    typeHelper.Verify(XObjectChange.Value, toChange);
                }
                docHelper.Verify(XObjectChange.Value, toChange);
            }
        }
    }

    public class EventsXElementValue
    {
        public static object[][] ExecuteXElementVariationParams = new object[][] {
            new object[] { new XElement("element"), "newValue" },
            new object[] { new XElement("element", "value"), "newValue" },
            new object[] { new XElement("element", new XAttribute("a", "aa")), "newValue" },
            new object[] { new XElement("parent", new XElement("child", "child text")), "newValue" },
            new object[] { new XElement("root", InputSpace.GetElement(100, 10).DescendantNodes()), "newValue" }
        };
        [Theory, MemberData(nameof(ExecuteXElementVariationParams))]
        public void ExecuteXElementVariation(XElement toChange, String newValue)
        {
            int count = toChange.Nodes().Count();
            XElement xElem = new XElement("root", toChange);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(toChange))
                {
                    toChange.Value = newValue;
                    Assert.True(toChange.Value == newValue, "Value change was not correct");
                    toChange.Verify();
                    eHelper.Verify(count + 1);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }
    }

    public class EventsXAttributeValue
    {
        public static object[][] ExecuteXAttributeVariationParams = new object[][] {
            new object[] { new XAttribute("xxx", ""), "newValue" },
            new object[] { new XAttribute("xxx", "yyy"), "newValue" },
            new object[] { new XAttribute("{a}xxx", "a_yyy"), "newValue" }
        };
        [Theory, MemberData(nameof(ExecuteXAttributeVariationParams))]
        public void ExecuteXAttributeVariation(XAttribute toChange, string newValue)
        {
            XElement xElem = new XElement("root", toChange);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(toChange))
                {
                    toChange.Value = newValue;
                    Assert.True(toChange.Value.Equals(newValue), "Value did not change");
                    xElem.Verify();
                    eHelper.Verify(XObjectChange.Value, toChange);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }
    }

    public class EventsXAttributeSetValue
    {
        public static object[][] ExecuteXAttributeVariationParams = new object[][] {
            new object[] { new XAttribute("xxx", ""), "newValue" },
            new object[] { new XAttribute("xxx", "yyy"), "newValue" },
            new object[] { new XAttribute("{a}xxx", "a_yyy"), "newValue" }
        };
        [Theory, MemberData(nameof(ExecuteXAttributeVariationParams))]
        public void ExecuteXAttributeVariation(XAttribute toChange, object newValue)
        {
            XElement xElem = new XElement("root", toChange);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(toChange))
                {
                    toChange.SetValue(newValue);
                    Assert.True(newValue.Equals(toChange.Value), "Value did not change");
                    xElem.Verify();
                    eHelper.Verify(XObjectChange.Value, toChange);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }
    }

    public class EventsXElementSetValue
    {
        public static object[][] ExecuteXElementVariationParams = new object[][] {
            new object[] { new XElement("element"), "newValue" },
            new object[] { new XElement("element", "value"), "newValue" },
            new object[] { new XElement("element", new XAttribute("a", "aa")), "newValue" },
            new object[] { new XElement("parent", new XElement("child", "child text")), "newValue" },
            new object[] { new XElement("root", InputSpace.GetElement(100, 10).DescendantNodes()), "newValue" }
        };
        [Theory, MemberData(nameof(ExecuteXElementVariationParams))]
        public void ExecuteXElementVariation(XElement toChange, object newValue)
        {
            int count = toChange.Nodes().Count();
            XElement xElemOriginal = new XElement(toChange);
            using (UndoManager undo = new UndoManager(toChange))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(toChange))
                {
                    toChange.SetValue(newValue);
                    Assert.True(newValue.Equals(toChange.Value), "Value change was not correct");
                    toChange.Verify();
                    eHelper.Verify(count + 1);
                }
                undo.Undo();
                Assert.True(toChange.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(toChange.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }
    }

    public class EventsXElementSetAttributeValue
    {
        public static object[][] ExecuteXAttributeVariationParams = new object[][] {
            new object[] { new XAttribute("xxx", ""), "newValue" },
            new object[] { new XAttribute("xxx", "yyy"), "newValue" },
            new object[] { new XAttribute("{a}xxx", "a_yyy"), "newValue" }
        };
        [Theory, MemberData(nameof(ExecuteXAttributeVariationParams))]
        public void ExecuteXAttributeVariation(XAttribute toChange, object newValue)
        {
            XElement xElem = new XElement("root", toChange);
            XElement xElemOriginal = new XElement(xElem);
            using (UndoManager undo = new UndoManager(xElem))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(toChange))
                {
                    xElem.SetAttributeValue(toChange.Name, newValue);
                    Assert.True(newValue.Equals(toChange.Value), "Value did not change");
                    xElem.Verify();
                    eHelper.Verify(XObjectChange.Value, toChange);
                }
                undo.Undo();
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }
    }

    public class EventsXElementSetElementValue
    {
        public static object[][] ExecuteAddVariationParams = new object[][] {
            new object[] { null, new XElement("element") },
            new object[] { new XComment("comment"), new XElement("element", "value") },
            new object[] { new XCData("cdata"), new XElement("element", new XAttribute("a", "aa")) },
            new object[] { new XAttribute("a", "aa"), new XElement("parent", new XElement("child", "child text")) },
            new object[] { new XElement("element"), new XElement("nodes", InputSpace.GetElement(100, 10).DescendantNodes()) }
        };
        [Theory, MemberData(nameof(ExecuteAddVariationParams))]
        public void ExecuteAddVariation(XObject content, XElement toAdd)
        {
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
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }

        public static object[][] ExecuteRemoveVariationParams = new object[][] {
            new object[] { new XElement("element") },
            new object[] { new XElement("element", "value") },
            new object[] { new XElement("element", new XAttribute("a", "aa")) },
            new object[] { new XElement("parent", new XElement("child", "child text")) },
            new object[] { new XElement("nodes", InputSpace.GetElement(100, 10).DescendantNodes()) }
        };
        [Theory, MemberData(nameof(ExecuteRemoveVariationParams))]
        public void ExecuteRemoveVariation(XElement content)
        {
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
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }

        public static object[][] ExecuteValueVariationParams = new object[][] {
            new object[] { new XElement("element"), (double)10 },
            new object[] { new XElement("element", "value"), "newValue" },
            new object[] { new XElement("element", new XAttribute("a", "aa")), System.DateTime.Now },
            new object[] { new XElement("parent", new XElement("child", "child text")), "Windows 8" },
            new object[] { new XElement("nodes", InputSpace.GetElement(100, 10).DescendantNodes()), "StackTrace" }
        };
        [Theory, MemberData(nameof(ExecuteValueVariationParams))]
        public void ExecuteValueVariation(XElement content, object newValue)
        {
            int count = content.Nodes().Count();
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
                Assert.True(xElem.Nodes().SequenceEqual(xElemOriginal.Nodes(), XNode.EqualityComparer), "Undo did not work!");
                Assert.True(xElem.Attributes().EqualsAllAttributes(xElemOriginal.Attributes(), Helpers.MyAttributeComparer), "Undo did not work!");
            }
        }
    }
}
