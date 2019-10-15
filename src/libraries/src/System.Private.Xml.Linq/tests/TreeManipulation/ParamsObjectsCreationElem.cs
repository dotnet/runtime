// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    //[TestCase(Name = "Constructors with params - XElement - array", Param = InputParamStyle.Array)]
    //[TestCase(Name = "Constructors with params - XElement - node + array", Param = InputParamStyle.SingleAndArray)]
    //[TestCase(Name = "Constructors with params - XElement - IEnumerable", Param = InputParamStyle.IEnumerable)]

    public class ParamsObjectsCreationElem : XLinqTestCase
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+ParamsObjectsCreationElem
        // Test Case

        // XElement
        // - from node without parent, from nodes with parent
        // - elements
        // - attributes
        // - PI, Comments
        // - self reference
        // - nulls inside
        // - the same object multiple times
        // - Document
        // - array/IEnumerable of allowed types
        // - array/IEnumerable including not allowed types
        // - element state after invalid operations
        // - text as plaint text & text as XText node
        // - concatenation of the text nodes
        // sanity for creation of a complete tree using params constructor

        #region Enums

        public enum InputParamStyle
        {
            Array,

            SingleAndArray,

            IEnumerable
        };

        #endregion

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(XElementValidCreate) { Attribute = new VariationAttribute("XElement - multiple nodes, not connected") { Params = new object[] { false, 4 }, Priority = 1 } });
            AddChild(new TestVariation(XElementValidCreate) { Attribute = new VariationAttribute("XElement - single node, not connected") { Params = new object[] { false, 1 }, Priority = 0 } });
            AddChild(new TestVariation(XElementValidCreate) { Attribute = new VariationAttribute("XElement - single node, connected") { Params = new object[] { true, 1 }, Priority = 0 } });
            AddChild(new TestVariation(XElementValidCreate) { Attribute = new VariationAttribute("(BVT)XElement - multiple nodes, connected") { Params = new object[] { true, 2 }, Priority = 0 } });
            AddChild(new TestVariation(XElementValidCreate) { Attribute = new VariationAttribute("(BVT)XElement - multiple nodes, not connected") { Params = new object[] { false, 2 }, Priority = 0 } });
            AddChild(new TestVariation(XElementValidCreate) { Attribute = new VariationAttribute("XElement - multiple nodes, connected") { Params = new object[] { true, 4 }, Priority = 1 } });
            AddChild(new TestVariation(XElementDuppAttr) { Attribute = new VariationAttribute("XElement - Not allowed - duplicate attributes") { Priority = 2 } });
            AddChild(new TestVariation(XElementNotAllowedXDoc) { Attribute = new VariationAttribute("XElement - Not allowed - XDocument") { Priority = 2 } });
            AddChild(new TestVariation(XElementNotAllowedXDocType) { Attribute = new VariationAttribute("XElement - Not allowed - XDocumentType") { Param = 3, Priority = 2 } });
            AddChild(new TestVariation(XElementEmptyArray) { Attribute = new VariationAttribute("XElement - nulls") { Priority = 3 } });
            AddChild(new TestVariation(BuildFromQuery) { Attribute = new VariationAttribute("XElement - build from Query result") { Priority = 2 } });
            AddChild(new TestVariation(IsEmptyProp1) { Attribute = new VariationAttribute("IsEmpty property Manipulation I.") { Priority = 0 } });
            AddChild(new TestVariation(IsEmptyProp2) { Attribute = new VariationAttribute("IsEmpty property Manipulation II.") { Priority = 0 } });
        }

        //[Variation(Priority = 0, Desc = "(BVT)XElement - multiple nodes, connected", Params = new object[] { true, 2 })]
        //[Variation(Priority = 0, Desc = "(BVT)XElement - multiple nodes, not connected", Params = new object[] { false, 2 })]
        //[Variation(Priority = 1, Desc = "XElement - multiple nodes, connected", Params = new object[] { true, 4 })]
        //[Variation(Priority = 1, Desc = "XElement - multiple nodes, not connected", Params = new object[] { false, 4 })]
        //[Variation(Priority = 0, Desc = "XElement - single node, connected", Params = new object[] { true, 1 })]
        //[Variation(Priority = 0, Desc = "XElement - single node, not connected", Params = new object[] { false, 1 })]

        //[Variation(Priority = 2, Desc = "XElement - build from Query result")]

        public void BuildFromQuery()
        {
            var mode = (InputParamStyle)Param;

            XElement e1 = XElement.Parse(@"<A><B id='a1'/><B id='a2'/><B id='a4'/></A>");
            XElement e2 = XElement.Parse(@"<root><a1 a='a'/><a2/><a3 b='b'/><a4 c='c'/><a5/><a6/><a7/><a8/></root>");

            IEnumerable<XElement> nodes = from data1 in e1.Elements() join data2 in e2.Elements() on data1.Attribute("id").Value equals data2.Name.LocalName select data2;

            IEnumerable<XAttribute> attributes = from data1 in e1.Elements() join data2 in e2.Elements() on data1.Attribute("id").Value equals data2.Name.LocalName select data2.FirstAttribute;

            IEnumerable<ExpectedValue> expectedContent = ExpectedContent(nodes).ProcessNodes().ToList();
            IEnumerable<ExpectedValue> expectedAttributes = ExpectedContent(attributes).Where(n => n.Data is XAttribute).ToList();

            XElement e = CreateElement(mode, nodes.OfType<object>().Concat(attributes.OfType<object>()));

            TestLog.Compare(expectedContent.EqualAll(e.Nodes(), XNode.EqualityComparer), "Content");
            TestLog.Compare(expectedAttributes.EqualAllAttributes(e.Attributes(), Helpers.MyAttributeComparer), "Attributes");
        }

        //[Variation(Priority = 0, Desc = "IsEmpty property Manipulation I.")]
        public void IsEmptyProp1()
        {
            var e = new XElement("e");
            TestLog.Compare(e.IsEmpty, "Initial - empty");
            TestLog.Compare(e.Value, "", "value 0");

            e.RemoveNodes();
            TestLog.Compare(e.IsEmpty, "Initial - after RemoveNodes 1");
            TestLog.Compare(e.Value, "", "value 1");

            e.Add("");
            TestLog.Compare(!e.IsEmpty, "Initial - after Add");
            TestLog.Compare(e.Value, "", "value 2");

            e.RemoveNodes();
            TestLog.Compare(e.IsEmpty, "Initial - after RemoveNodes 2");
            TestLog.Compare(e.Value, "", "value 3");
        }

        //[Variation(Priority = 0, Desc = "IsEmpty property Manipulation II.")]
        public void IsEmptyProp2()
        {
            var e = new XElement("e", "");
            TestLog.Compare(!e.IsEmpty, "Initial - empty");
            TestLog.Compare(e.Value, "", "value 0");

            e.Add("");
            TestLog.Compare(!e.IsEmpty, "Initial - after Add");
            TestLog.Compare(e.Value, "", "value 1");

            e.RemoveNodes();
            TestLog.Compare(e.IsEmpty, "Initial - after RemoveNodes 1");
            TestLog.Compare(e.Value, "", "value 2");

            e.Add("");
            TestLog.Compare(!e.IsEmpty, "Initial - after Add");
            TestLog.Compare(e.Value, "", "value 3");
        }

        public void XElementDuppAttr()
        {
            var mode = (InputParamStyle)Param;
            object[] paras = { new XAttribute("id", "a1"), new XAttribute("other", "ooo"), new XAttribute("id", "a2"), null, "", "text", new XElement("aa"), new XProcessingInstruction("PI", "click"), new XComment("comment") };

            try
            {
                XElement e = CreateElement(mode, paras);
                TestLog.Compare(false, "Exception expected");
            }
            catch (InvalidOperationException)
            {
            }
        }

        public void XElementEmptyArray()
        {
            var mode = (InputParamStyle)Param;
            object[] nulls = { new object[] { }, new object[] { null, null }, null, "", new object[] { null, new object[] { null, null }, null }, new object[] { null, new object[] { null, null }, new List<object> { null, null, null } } };
            foreach (object paras in nulls)
            {
                XElement elem = CreateElement(mode, paras);
                TestLog.Compare(elem != null, "elem != null");
                TestLog.Compare(elem.FirstNode == null, "elem.FirstNode==null");
                elem.Verify();
            }
        }

        public void XElementNotAllowedXDoc()
        {
            var mode = (InputParamStyle)Param;
            object[] paras = { new XAttribute("id", "a1"), null, "text", new XDocument(), new XElement("aa"), new XProcessingInstruction("PI", "click"), new XComment("comment") };
            try
            {
                XElement e = CreateElement(mode, paras);
                TestLog.Compare(false, "Exception expected");
            }
            catch (ArgumentException)
            {
            }
        }

        //[Variation(Priority = 2, Desc = "XElement - Not allowed - XDocumentType", Param = 3)]
        public void XElementNotAllowedXDocType()
        {
            var mode = (InputParamStyle)Param;
            object[] paras = { new XAttribute("id", "a1"), new XDocumentType("doctype", "", "", ""), "text", null, new XElement("aa"), new XProcessingInstruction("PI", "click"), new XComment("comment") };
            try
            {
                XElement e = CreateElement(mode, paras);
                TestLog.Compare(false, "Exception expected");
            }
            catch (ArgumentException)
            {
            }
        }

        public void XElementValidCreate()
        {
            var mode = (InputParamStyle)Param;
            var isConnected = (bool)Variation.Params[0];
            var CombinationLength = (int)Variation.Params[1];

            object[] nodes = { new XElement("A"), new XElement("A", new XAttribute("a1", "a1")), new object[] { new XElement("B"), "", null, new XElement("C"), new XAttribute("xx", "yy") }, new XElement("{NS1}A"), new XAttribute("id", "a1"), new XAttribute("ie", "ie"), new XAttribute("{NS1}id", "b2"), "", new XAttribute(XNamespace.Xmlns + "NS1", "http://ns1"),
                new XProcessingInstruction("Pi", "data"), new XProcessingInstruction("P2", ""), null, new XComment("comment"), new XText("text1"), new XCData("textCDATA"), "textPlain1", "textPlain2" };

            XElement dummy = null;
            if (isConnected)
            {
                dummy = new XElement("dummy", nodes);
            }

            foreach (var data in nodes.NonRecursiveVariations(CombinationLength))
            {
                IEnumerable<ExpectedValue> expectedContent = ExpectedContent(data.Flatten()).ProcessNodes().ToList();
                IEnumerable<ExpectedValue> expectedAttributes = ExpectedContent(data.Flatten()).Where(n => n.Data is XAttribute).ToList();

                XElement e = CreateElement(mode, data);

                TestLog.Compare(expectedContent.EqualAll(e.Nodes(), XNode.EqualityComparer), "Content");
                TestLog.Compare(expectedAttributes.EqualAllAttributes(e.Attributes(), Helpers.MyAttributeComparer), "Attributes");

                e.Verify();
            }
        }

        #endregion

        #region Methods

        private XElement CreateElement(InputParamStyle mode, object[] data)
        {
            XElement e = null;
            switch (mode)
            {
                case InputParamStyle.Array:
                    e = new XElement(nameof(data), data);
                    break;
                case InputParamStyle.SingleAndArray:
                    if (data.Length < 2)
                    {
                        goto case InputParamStyle.Array;
                    }
                    var copy = new object[data.Length - 1];
                    Array.Copy(data, 1, copy, 0, data.Length - 1);
                    e = new XElement(nameof(data), data[0], copy);
                    break;
                case InputParamStyle.IEnumerable:
                    e = new XElement(nameof(data), data);
                    break;
                default:
                    TestLog.Compare(false, "test failed");
                    break;
            }
            return e;
        }

        private XElement CreateElement(InputParamStyle mode, object data)
        {
            return new XElement(nameof(data), data);
        }

        private IEnumerable<ExpectedValue> ExpectedContent<T>(IEnumerable<T> data) where T : class
        {
            return data.Select(n => new ExpectedValue((n is XNode) && (!(n is XText)) && (n as XNode).Parent == null, n));
        }
        #endregion
    }
}
