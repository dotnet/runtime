// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Xml.Linq;
using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class FirstNode : XLinqTestCase
    {
        // On XElement
        // Empty element
        // Element with string
        // Element with text node
        // Mixed content
        //[Variation(Priority = 0, Desc = "Empty XElement", Params = new object[] { "<A/>" })]
        //[Variation(Priority = 1, Desc = "Empty XElement - II.", Params = new object[] { "<A></A>" })]
        //[Variation(Priority = 0, Desc = "Empty XElement - with attribute", Params = new object[] { "<A attr='a'/>" })]
        //[Variation(Priority = 1, Desc = "Empty XElement - with parent", Params = new object[] { "<B><A attr='a'/></B>" })]

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(EmptyElement) { Attribute = new VariationAttribute("Empty XElement") { Params = new object[] { "<A/>" }, Priority = 0 } });
            AddChild(new TestVariation(EmptyElement) { Attribute = new VariationAttribute("Empty XElement - II.") { Params = new object[] { "<A></A>" }, Priority = 1 } });
            AddChild(new TestVariation(EmptyElement) { Attribute = new VariationAttribute("Empty XElement - with attribute") { Params = new object[] { "<A attr='a'/>" }, Priority = 0 } });
            AddChild(new TestVariation(EmptyElement) { Attribute = new VariationAttribute("Empty XElement - with parent") { Params = new object[] { "<B><A attr='a'/></B>" }, Priority = 1 } });
            AddChild(new TestVariation(elementWithText) { Attribute = new VariationAttribute("XElement - string content [whitespace]") { Params = new object[] { "<B><A attr='a'> </A></B>", " " }, Priority = 1 } });
            AddChild(new TestVariation(elementWithText) { Attribute = new VariationAttribute("XElement - string content") { Params = new object[] { "<B><A attr='a'>text</A></B>", "text" }, Priority = 0 } });
            AddChild(new TestVariation(elementWithText) { Attribute = new VariationAttribute("XElement - string content [cdata]") { Params = new object[] { "<B><A attr='a'><![CDATA[cdata]]></A></B>", "cdata" }, Priority = 1 } });
            AddChild(new TestVariation(ElementOtherTypes) { Attribute = new VariationAttribute("XElement - mixed content") { Priority = 1 } });
            AddChild(new TestVariation(DocumentOtherTypes) { Attribute = new VariationAttribute("XDocument - mixed content - with decl") { Param = true, Priority = 1 } });
            AddChild(new TestVariation(DocumentOtherTypes) { Attribute = new VariationAttribute("XDocument - mixed content - no decl") { Param = false, Priority = 1 } });
        }

        public void DocumentOtherTypes()
        {
            object[] nodeset = { " ", new XProcessingInstruction("pi", "data"), new XProcessingInstruction("pi2", "data2"), new XComment("comm"), new XDocumentType("root", null, null, null), new XElement("single"), new XElement("complex", new XElement("a1"), new XElement("a2")) };

            XDocument doc = ((bool)Variation.Param) ? new XDocument(new XDeclaration("1.0", null, null)) : new XDocument();

            int pos = ((bool)Param) ? 0 : nodeset.Length - 1;

            foreach (var combination in nodeset.PositionCombinations(pos))
            {
                if (combination.Select(c => new ExpectedValue(false, c)).IsXDocValid())
                {
                    continue;
                }
                doc.Add(combination);
                XNode ret = ((bool)Param) ? doc.FirstNode : doc.LastNode;

                if (combination[pos] is string)
                {
                    TestLog.Compare(ret is XText, "text node");
                    TestLog.Compare((ret as XText).Value, combination[pos], "text node value");
                }
                else
                {
                    TestLog.Compare(ret.Equals(combination[pos]), "Node not OK");
                }
                doc.RemoveNodes();
            }
        }

        public void ElementOtherTypes()
        {
            object[] nodeset = { "text", new XProcessingInstruction("pi", "data"), new XComment("comm"), new XElement("single"), new XElement("complex", new XElement("a1"), new XElement("a2")) };

            XElement e = XElement.Parse("<A/>");
            int pos = ((bool)Param) ? 0 : nodeset.Length - 1;

            foreach (var combination in nodeset.PositionCombinations(pos))
            {
                e.Add(combination);
                XNode ret = ((bool)Param) ? e.FirstNode : e.LastNode;

                if (combination[pos] is string)
                {
                    TestLog.Compare(ret is XText, "text node");
                    TestLog.Compare((ret as XText).Value, combination[pos], "text node value");
                }
                else
                {
                    TestLog.Compare(ret.Equals(combination[pos]), "Node not OK");
                }
                e.RemoveAll();
            }
        }

        public void EmptyElement()
        {
            var xml = Variation.Params[0] as string;
            XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
            e = e.DescendantsAndSelf("A").First();
            XNode ret = ((bool)Param) ? e.FirstNode : e.LastNode;
            TestLog.Compare(ret == null, "Node != null");
        }

        //[Variation(Priority = 0, Desc = "XElement - string content", Params = new object[] { "<B><A attr='a'>text</A></B>", "text" })]
        //[Variation(Priority = 1, Desc = "XElement - string content [cdata]", Params = new object[] { "<B><A attr='a'><![CDATA[cdata]]></A></B>", "cdata" })]
        //[Variation(Priority = 1, Desc = "XElement - string content [whitespace]", Params = new object[] { "<B><A attr='a'> </A></B>", " " })]
        public void elementWithText()
        {
            var xml = Variation.Params[0] as string;
            var expValue = Variation.Params[1] as string;

            XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
            e = e.DescendantsAndSelf("A").First();

            XNode ret = ((bool)Param) ? e.FirstNode : e.LastNode;

            TestLog.Compare(ret is XText, "Node is XText");
            TestLog.Compare((ret as XText).Value, expValue, "Node expected value");
            TestLog.Compare(ret.Parent.Equals(e), "Node.Parent");

            TestLog.Compare(ret.NextNode == null, "nextnode == null");
            TestLog.Compare(ret.PreviousNode == null, "Previousnode == null");
        }
        #endregion

        //[Variation(Priority = 1, Desc = "XElement - mixed content")]
        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+FirstNode
        // Test Case
    }
}
