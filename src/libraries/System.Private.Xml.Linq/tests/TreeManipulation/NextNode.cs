// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using System.Xml.Linq;
using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class NextNode : XLinqTestCase
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+NextNode
        // Test Case

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(JustOneNode) { Attribute = new VariationAttribute("Just one node, Comment") { Params = new object[] { "<A attr='1'><!-- commo --></A>" }, Priority = 1 } });
            AddChild(new TestVariation(JustOneNode) { Attribute = new VariationAttribute("Just one node, cdata") { Params = new object[] { "<A attr='1'><![CDATA[cdata]]></A>" }, Priority = 1 } });
            AddChild(new TestVariation(JustOneNode) { Attribute = new VariationAttribute("Just one node, element") { Params = new object[] { "<A attr='1'><B/></A>" }, Priority = 0 } });
            AddChild(new TestVariation(JustOneNode) { Attribute = new VariationAttribute("Just one node, PI") { Params = new object[] { "<A attr='1'><?PI?></A>" }, Priority = 1 } });
            AddChild(new TestVariation(JustOneNode) { Attribute = new VariationAttribute("Just one node, text") { Params = new object[] { "<A attr='1'>text</A>" }, Priority = 0 } });
            AddChild(new TestVariation(JustOneNode) { Attribute = new VariationAttribute("Just one node, whitespace") { Params = new object[] { "<A attr='1'> </A>" }, Priority = 1 } });
            AddChild(new TestVariation(DisconectedNode) { Attribute = new VariationAttribute("Disconnected node, element") { Params = new object[] { "<A attr='1'><B/></A>" }, Priority = 0 } });
            AddChild(new TestVariation(DisconectedNode) { Attribute = new VariationAttribute("Disconnected node, text") { Params = new object[] { "<A attr='1'>text</A>" }, Priority = 0 } });
            AddChild(new TestVariation(DisconectedNode) { Attribute = new VariationAttribute("Disconnected node, PI") { Params = new object[] { "<A attr='1'><?PI?></A>" }, Priority = 1 } });
            AddChild(new TestVariation(DisconectedNode) { Attribute = new VariationAttribute("Disconnected node, cdata") { Params = new object[] { "<A attr='1'><![CDATA[cdata]]></A>" }, Priority = 1 } });
            AddChild(new TestVariation(DisconectedNode) { Attribute = new VariationAttribute("Disconnected node, Comment") { Params = new object[] { "<A attr='1'><!-- commo --></A>" }, Priority = 1 } });
            AddChild(new TestVariation(DisconectedNode) { Attribute = new VariationAttribute("Disconnected node, whitespace") { Params = new object[] { "<A attr='1'> </A>" }, Priority = 1 } });
        }

        public void DisconectedNode()
        {
            var xml = Variation.Params[0] as string;
            var rs = new XmlReaderSettings();
            rs.IgnoreWhitespace = false;
            XNode n;
            using (XmlReader r = XmlReader.Create(new StringReader(xml), rs))
            {
                r.Read();
                r.Read();
                n = XNode.ReadFrom(r);
            }
            TestLog.Compare(n.NextNode == null, "NextNode should be null");
            TestLog.Compare(n.PreviousNode == null, "PreviousNode should be null");
        }

        public void JustOneNode()
        {
            var xml = Variation.Params[0] as string;
            XElement e = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
            XNode n = e.FirstNode;
            TestLog.Compare(n != null, "TEST_FAILED: start");
            TestLog.Compare(n == e.LastNode, "lastnode == previousnode");
            TestLog.Compare(n.NextNode == null, "NextNode should be null");
            TestLog.Compare(n.PreviousNode == null, "PreviousNode should be null");
        }
        #endregion
    }
}
