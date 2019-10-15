// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XLinqTests
{
    public class XContainerAddIntoElement : XLinqTestCase
    {
        private EventsHelper _eHelper;
        private bool _runWithEvents;

        public override void AddChildren()
        {
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement empty (isEmpty=False) - connected") { Params = new object[] { "<A xmlns='ns0'></A>", 1, true }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement empty - connected") { Params = new object[] { "<A xmlns='ns0'/>", 1, true }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement empty (isEmpty=False) - connected") { Params = new object[] { "<A xmlns='ns0'></A>", 3, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement with text - connected") { Params = new object[] { "<A xmlns='ns0'>tralala</A>", 3, true, "tralala" }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement with text/CDATA - connected") { Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>", 3, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement with text - connected") { Params = new object[] { "<A xmlns='ns0'>tralala</A>", 1, true, "tralala" }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement with text/CDATA - connected") { Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>", 1, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement with content II - connected") { Params = new object[] { "<A xmlns='ns0'>tteexxtt<?PI?><X/>text<Y/></A>", 3, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement with single child element - connected") { Params = new object[] { "<A xmlns='ns0'><X/></A>", 3, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement with content III - connected") { Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X/>text<Y/></A>", 3, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement with single child element - connected") { Params = new object[] { "<A xmlns='ns0'><X/></A>", 1, true }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement with content I - connected") { Params = new object[] { "<A xmlns='ns0'><?PI?><X/>text<Y/></A>", 1, true }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement with content I - connected") { Params = new object[] { "<A xmlns='ns0'><?PI?><X/>text<Y/></A>", 3, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement with content II - connected") { Params = new object[] { "<A xmlns='ns0'>tteexxtt<?PI?><X/>text<Y/></A>", 1, true }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement with content III - connected") { Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X/>text<Y/></A>", 1, true }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement empty - connected") { Params = new object[] { "<A xmlns='ns0'/>", 3, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement empty") { Params = new object[] { "<A xmlns='ns0'/>", 1, false }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement empty (isEmpty=False)") { Params = new object[] { "<A xmlns='ns0'></A>", 1, false }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement with text") { Params = new object[] { "<A xmlns='ns0'>tralala</A>", 1, false, "tralala" }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement with text/CDATA") { Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>", 1, false }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement with single child element") { Params = new object[] { "<A xmlns='ns0'><X/></A>", 1, false }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement with content I") { Params = new object[] { "<A xmlns='ns0'><?PI?><X/>text<Y/></A>", 1, false }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement with content II") { Params = new object[] { "<A xmlns='ns0'>tteexxtt<?PI?><X/>text<Y/></A>", 1, false }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Single node - XElement with content III") { Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X/>text<Y/></A>", 1, false }, Priority = 0 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement empty") { Params = new object[] { "<A xmlns='ns0'/>", 3, false }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement empty (isEmpty=False)") { Params = new object[] { "<A xmlns='ns0'></A>", 3, false }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement with text") { Params = new object[] { "<A xmlns='ns0'>tralala</A>", 3, false, "tralala" }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement with text/CDATA") { Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>", 3, false }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement with single child element") { Params = new object[] { "<A xmlns='ns0'><X/></A>", 3, false }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement with content I") { Params = new object[] { "<A xmlns='ns0'><?PI?><X/>text<Y/></A>", 3, false }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement with content II") { Params = new object[] { "<A xmlns='ns0'>tteexxtt<?PI?><X/>text<Y/></A>", 3, false }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoElement) { Attribute = new VariationAttribute("Multiple (3) nodes - XElement with content III") { Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X/>text<Y/></A>", 3, false }, Priority = 1 } });
        }

        /// <summary>
        /// XElement:
        ///  - with content, empty
        ///  - adding valid nodes
        ///  - adding text, text concatenation, node identity
        ///  - adding nulls
        ///  - adding IEnumerable (order)
        /// </summary>
        public void AddIntoElement()
        {
            _runWithEvents = (bool)Params[0];
            var xml = Variation.Params[0] as string;
            var variationLength = (int)Variation.Params[1];
            var isConnected = (bool)Variation.Params[2];
            string stringOnlyContent = Variation.Params.Length > 3 ? Variation.Params[3] as string : null;

            object[] nodes = { new XElement("B"), new XElement(XNamespace.Get("ns1") + "B"), new XElement("B", new XElement("C"), new XAttribute("a", "aa")), new XProcessingInstruction("PI", "data"), new XComment("comment"), new XAttribute("xxx", "yyy"), new XAttribute("{a}xxx", "a_yyy"), new XAttribute("{b}xxx", "b_yyy"), "text plain", " ", "", null, new XText("xtext"), new XText(""), new XCData("xcdata") };

            if (isConnected)
            {
                foreach (XNode n in nodes.OfType<XNode>())
                {
                    new XDocument(new XElement("dumma", n));
                }
            }

            foreach (var toInsert in nodes.NonRecursiveVariations(variationLength))
            {
                XElement e = XElement.Parse(xml);
                string stringOnlyContentCopy = stringOnlyContent == null ? null : new string(stringOnlyContent.ToCharArray());

                List<ExpectedValue> expectedNodes = CalculateExpectedContent(e, toInsert, stringOnlyContentCopy).ProcessNodes().ToList();
                List<ExpectedValue> expectedAttributes = CalculateExpectedAttributes(e, toInsert, stringOnlyContentCopy).ToList();

                if (_runWithEvents)
                {
                    _eHelper = new EventsHelper(e);
                }
                e.Add(toInsert);

                TestLog.Compare(expectedNodes.EqualAll(e.Nodes(), XNode.EqualityComparer), "Add - content");
                TestLog.Compare(expectedAttributes.EqualAllAttributes(e.Attributes(), Helpers.MyAttributeComparer), "expectedAttributes.EqualAllAttributes(e.Attributes(), Helpers.MyAttributeComparer)");
                e.RemoveAll();
            }
        }

        private IEnumerable<ExpectedValue> CalculateExpectedAttributes(XElement orig, IEnumerable<object> newNodes, string stringOnlyContent)
        {
            foreach (object o in orig.Attributes())
            {
                yield return new ExpectedValue(true, o);
            }

            foreach (object n in newNodes.Flatten().Where(o => o is XAttribute))
            {
                yield return new ExpectedValue((n as XAttribute).Parent == null, n);
            }
        }

        private IEnumerable<ExpectedValue> CalculateExpectedContent(XContainer orig, IEnumerable<object> newNodes, string stringOnlyContent)
        {
            if (stringOnlyContent == null)
            {
                foreach (object o in orig.Nodes())
                {
                    yield return new ExpectedValue(true, o);
                }
            }
            else
            {
                yield return new ExpectedValue(false, new XText(stringOnlyContent));
            }

            foreach (object n in newNodes.Flatten())
            {
                if (n is XAttribute)
                {
                    continue;
                }
                yield return new ExpectedValue((n is XNode) && (n as XNode).Parent == null && (n as XNode).Document == null, n);
            }
        }
    }
}
