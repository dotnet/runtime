// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class XElement_SetElementValue : XLinqTestCase
    {
        #region Fields

        private EventsHelper _eHelper;
        private bool _runWithEvents;

        #endregion

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Delete Element (non existing, text only, full)") { Params = new object[] { "<A></A>", "X", null }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Add Element (empty)") { Params = new object[] { "<A/>", "X", "text" }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Add Element (text only)") { Params = new object[] { "<A>t1</A>", "X", "text" }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Add Element (Empty, full)") { Params = new object[] { "<A></A>", "X", "text" }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Add Element (mixed content)") { Params = new object[] { "<A><B/>t2<!--comm-->t3</A>", "X", "text" }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Replace Element (the only child)") { Params = new object[] { "<A><X xmlns='' Id='Id'/></A>", "X", "text" }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Replace Element (mixed content, empty)") { Params = new object[] { "<A><X/>tn<X xmlns='a'/></A>", "{a}X", "text" }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Replace Element (mixed content)") { Params = new object[] { "<A><X/>tn<X xmlns='a'><Y/>hum<?PI?></X></A>", "{a}X", "text" }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Delete Element (only element, empty)") { Params = new object[] { "<A><X xmlns='a'>tralala</X></A>", "{a}X", null }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Delete Element (only element)") { Params = new object[] { "<A><X xmlns='a'>tralala</X></A>", "{a}X", null }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Delete Element (mixed content, empty)") { Params = new object[] { "<A>t1<X xmlns='a'/>t2</A>", "{a}X", null }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Delete Element (mixed content)") { Params = new object[] { "<A>t1<X xmlns='a'><m/></X>t2</A>", "{a}X", null }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Delete Element (non existing)") { Params = new object[] { "<A>t1<X xmlns='a'><m/></X>t2</A>", "X", null }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Delete Element (non existing, text only)") { Params = new object[] { "<A>t1</A>", "X", null }, Priority = 1 } });
        }

        //[Variation(Priority = 0, Desc = "Add Element (empty)", Params = new object[] { "<A/>", "X", "text" })]
        //[Variation(Priority = 0, Desc = "Add Element (text only)", Params = new object[] { "<A>t1</A>", "X", "text" })]
        //[Variation(Priority = 0, Desc = "Add Element (Empty, full)", Params = new object[] { "<A></A>", "X", "text" })]
        //[Variation(Priority = 0, Desc = "Add Element (mixed content)", Params = new object[] { "<A><B/>t2<!--comm-->t3</A>", "X", "text" })]
        //[Variation(Priority = 0, Desc = "Replace Element (the only child)", Params = new object[] { "<A><X xmlns='' Id='Id'/></A>", "X", "text" })]
        //[Variation(Priority = 1, Desc = "Replace Element (mixed content, empty)", Params = new object[] { "<A><X/>tn<X xmlns='a'/></A>", "{a}X", "text" })]
        //[Variation(Priority = 0, Desc = "Replace Element (mixed content)", Params = new object[] { "<A><X/>tn<X xmlns='a'><Y/>hum<?PI?></X></A>", "{a}X", "text" })]
        //[Variation(Priority = 1, Desc = "Delete Element (only element, empty)", Params = new object[] { "<A><X xmlns='a'>tralala</X></A>", "{a}X", null })]
        //[Variation(Priority = 0, Desc = "Delete Element (only element)", Params = new object[] { "<A><X xmlns='a'>tralala</X></A>", "{a}X", null })]
        //[Variation(Priority = 0, Desc = "Delete Element (mixed content, empty)", Params = new object[] { "<A>t1<X xmlns='a'/>t2</A>", "{a}X", null })]
        //[Variation(Priority = 1, Desc = "Delete Element (mixed content)", Params = new object[] { "<A>t1<X xmlns='a'><m/></X>t2</A>", "{a}X", null })]
        //[Variation(Priority = 1, Desc = "Delete Element (non existing)", Params = new object[] { "<A>t1<X xmlns='a'><m/></X>t2</A>", "X", null })]
        //[Variation(Priority = 1, Desc = "Delete Element (non existing, text only)", Params = new object[] { "<A>t1</A>", "X", null })]
        //[Variation(Priority = 1, Desc = "Delete Element (non existing, text only, full)", Params = new object[] { "<A></A>", "X", null })]

        public void OnXElement()
        {
            _runWithEvents = (bool)Params[0];
            var xml = Variation.Params[0] as string;
            var newName = Variation.Params[1] as string;
            var newValue = Variation.Params[2] as string;

            XElement e = XElement.Parse(xml);

            XElement changed = e.Element(newName);

            IEnumerable<ExpectedValue> nodeIdentityExpValues = getExpectedValues(e, newName, newValue, true).ProcessNodes().ToList();
            IEnumerable<ExpectedValue> valueExpectedValues = getExpectedValues(e, newName, newValue, false).ProcessNodes().ToList();
            IEnumerable<XAttribute> attr = changed == null ? null : changed.Attributes().ToArray();

            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(e);
            }
            e.SetElementValue(newName, newValue);

            if (newValue == null)
            {
                TestLog.Compare(changed == null || changed.Parent == null, "delete: changed.Parent == null");
                TestLog.Compare(e.Element(newName) == null, "e.Element(newName)==null");
            }
            else
            {
                TestLog.Compare(changed == null || ReferenceEquals(changed, e.Element(newName)), "changed==null || Object.ReferenceEquals(changed, e.Element(newName))");
                TestLog.Compare(e.Element(newName).Value, newValue, "e.Element(newName).Value, newValue");
                TestLog.Compare(!e.Element(newName).HasElements, "!e.Element(newName).HasElements");
                TestLog.Compare(attr == null || attr.SequenceEqual(changed.Attributes()), "attr.EqualAll(changed.Attributes())");
            }

            TestLog.Compare(nodeIdentityExpValues.EqualAll(e.Nodes(), XNode.EqualityComparer), "nodeidentityExpValues.EqualAll (e.Nodes(), XNode.EqualityComparer)");
            TestLog.Compare(valueExpectedValues.EqualAll(e.Nodes(), XNode.EqualityComparer), "valueExpectedValues.EqualAll(e.Nodes(), XNode.EqualityComparer)");

            e.Verify();
        }

        #endregion

        #region Methods

        private IEnumerable<ExpectedValue> getExpectedValues(XElement parent, XName newName, string newValue, bool isCopy)
        {
            bool wasReplace = false;
            foreach (XNode node in parent.Nodes())
            {
                if (node is XElement && (node as XElement).Name == newName)
                {
                    if (newValue == null)
                    {
                        continue;
                    }
                    yield return isCopy ? new ExpectedValue(true, node) : new ExpectedValue(false, new XElement(newName, (node as XElement).Attributes(), newValue));
                    wasReplace = true;
                }
                else
                {
                    yield return new ExpectedValue(true, node);
                }
            }
            if (!wasReplace && newValue != null)
            {
                yield return new ExpectedValue(false, new XElement(newName, newValue));
            }
        }
        #endregion
    }
}
