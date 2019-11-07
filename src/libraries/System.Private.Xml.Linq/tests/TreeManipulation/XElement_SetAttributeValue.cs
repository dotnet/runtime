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
    public class XElement_SetAttributeValue : XLinqTestCase
    {
        #region Fields

        private EventsHelper _eHelper;
        private bool _runWithEvents;

        #endregion

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Add attribute") { Params = new object[] { "<A a1='a1' b='b'>text<B/></A>", "{ns}a1", "ns_a1" }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Add attribute (empty elem)") { Params = new object[] { "<A a='a' b='b'/>", "a1", "a1" }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Add attribute (content text only)") { Params = new object[] { "<A a1='a1' b='b'>text</A>", "{ns}a1", "ns_a1" }, Priority = 2 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Replace attribute (empty elem, only one attr.)") { Params = new object[] { "<A a1='original' />", "a1", "a1" }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Replace attribute (empty elem, multiple attr., namespaces)") { Params = new object[] { "<A p:a1='x' a1='original' xmlns:p='ns'/>", "{ns}a1", "ns_a1" }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Replace attribute (multiple attr. (first))") { Params = new object[] { "<A a1='original' x='x' y='y'>text</A>", "a1", "a1" }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Replace attribute (multiple attr. (mIddle))") { Params = new object[] { "<A x='x' a1='original' y='y'>text</A>", "a1", "a1" }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Replace attribute (multiple attr. (last))") { Params = new object[] { "<A x='x' y='y' a1='original'>text</A>", "a1", "a1" }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Remove attribute (only attribute)") { Params = new object[] { "<A a1='original'></A>", "a1", null }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Remove attribute (from multiple attribs)") { Params = new object[] { "<A y='y' a1='original' x='x'></A>", "a1", null }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Remove attribute (from multiple attribs, namespace)") { Params = new object[] { "<A a1='original' p:a1='o' xmlns:p='A' ></A>", "{A}a1", null }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Remove attribute (from multiple attribs, content)") { Params = new object[] { "<A x='t' a1='original' y='r'>trt</A>", "a1", null }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Remove attribute (nonexisting)") { Params = new object[] { "<A x='t' a1='original' y='r'>trt</A>", "nonex", null }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Add attribute (empty elem, namespaces)") { Params = new object[] { "<A a1='a1' b='b'/>", "{ns}a1", "ns_a1" }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Add attribute (empty elem I., no attrs)") { Params = new object[] { "<A></A>", "a1", "a1" }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("Add attribute (empty elem II., no attrs)") { Params = new object[] { "<A/>", "a1", "a1" }, Priority = 0 } });
        }

        //[Variation(Priority = 1, Desc = "Add attribute (empty elem I., no attrs)", Params = new object[] { "<A></A>", "a1", "a1" })]
        //[Variation(Priority = 0, Desc = "Add attribute (empty elem II., no attrs)", Params = new object[] { "<A/>", "a1", "a1" })]
        //[Variation(Priority = 0, Desc = "Add attribute (empty elem)", Params = new object[] { "<A a='a' b='b'/>", "a1", "a1" })]
        //[Variation(Priority = 0, Desc = "Add attribute (empty elem, namespaces)", Params = new object[] { "<A a1='a1' b='b'/>", "{ns}a1", "ns_a1" })]
        //[Variation(Priority = 1, Desc = "Add attribute", Params = new object[] { "<A a1='a1' b='b'>text<B/></A>", "{ns}a1", "ns_a1" })]
        //[Variation(Priority = 2, Desc = "Add attribute (content text only)", Params = new object[] { "<A a1='a1' b='b'>text</A>", "{ns}a1", "ns_a1" })]

        //[Variation(Priority = 0, Desc = "Replace attribute (empty elem, only one attr.)", Params = new object[] { "<A a1='original' />", "a1", "a1" })]
        //[Variation(Priority = 0, Desc = "Replace attribute (empty elem, multiple attr., namespaces)", Params = new object[] { "<A p:a1='x' a1='original' xmlns:p='ns'/>", "{ns}a1", "ns_a1" })]
        //[Variation(Priority = 1, Desc = "Replace attribute (multiple attr. (first))", Params = new object[] { "<A a1='original' x='x' y='y'>text</A>", "a1", "a1" })]
        //[Variation(Priority = 1, Desc = "Replace attribute (multiple attr. (mIddle))", Params = new object[] { "<A x='x' a1='original' y='y'>text</A>", "a1", "a1" })]
        //[Variation(Priority = 1, Desc = "Replace attribute (multiple attr. (last))", Params = new object[] { "<A x='x' y='y' a1='original'>text</A>", "a1", "a1" })]

        //[Variation(Priority = 0, Desc = "Remove attribute (only attribute)", Params = new object[] { "<A a1='original'></A>", "a1", null })]
        //[Variation(Priority = 0, Desc = "Remove attribute (from multiple attribs)", Params = new object[] { "<A y='y' a1='original' x='x'></A>", "a1", null })]
        //[Variation(Priority = 1, Desc = "Remove attribute (from multiple attribs, namespace)", Params = new object[] { "<A a1='original' p:a1='o' xmlns:p='A' ></A>", "{A}a1", null })]
        //[Variation(Priority = 1, Desc = "Remove attribute (from multiple attribs, content)", Params = new object[] { "<A x='t' a1='original' y='r'>trt</A>", "a1", null })]
        //[Variation(Priority = 1, Desc = "Remove attribute (nonexisting)", Params = new object[] { "<A x='t' a1='original' y='r'>trt</A>", "nonex", null })]

        public void OnXElement()
        {
            _runWithEvents = (bool)Params[0];
            var xml = Variation.Params[0] as string;
            var newName = Variation.Params[1] as string;
            var newValue = Variation.Params[2] as string;

            XElement e = XElement.Parse(xml);

            XAttribute origAttrib = e.Attribute(newName);
            IEnumerable<XAttribute> origAttrs = e.Attributes().ToList();
            IEnumerable<ExpectedValue> refComparison = getExpectedAttributes(e, newName, newValue, true).ToList();
            IEnumerable<ExpectedValue> valueComparison = getExpectedAttributes(e, newName, newValue, false).ToList();
            IEnumerable<XNode> nodes = e.Nodes().ToList();

            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(e);
            }
            e.SetAttributeValue(newName, newValue);
            // Not sure how to verify this yet( what possible events and in what order)
            if (_runWithEvents)
            {
                if (newValue == null)
                {
                    if (origAttrib != null)
                    {
                        _eHelper.Verify(XObjectChange.Remove);
                    }
                }
                else
                {
                    if (origAttrib != null)
                    {
                        _eHelper.Verify(XObjectChange.Value);
                    }
                    else
                    {
                        _eHelper.Verify(XObjectChange.Add);
                    }
                }
            }

            if (newValue != null)
            {
                TestLog.Compare(origAttrib == null || ReferenceEquals(origAttrib, e.Attribute(newName)), "origAttrib == null || Object.ReferenceEquals (origAttrib, e.Attribute(newName))");
                TestLog.Compare(e.Attribute(newName).Value, newValue, "e.Attribute(newName), newValue");
            }
            else
            {
                TestLog.Compare(origAttrib == null || (origAttrib.Parent == null && origAttrib.Document == null), "origAttrib == null || (origAttrib.Parent==null && origAttrib.Document==null)");
                TestLog.Compare(e.Attribute(newName), null, "e.Attribute(newName), null");
            }

            TestLog.Compare(refComparison.EqualAllAttributes(e.Attributes(), Helpers.MyAttributeComparer), "refComparison.EqualAllAttributes(e.Attributes(), Helpers.MyAttributeComparer))");
            TestLog.Compare(valueComparison.EqualAllAttributes(e.Attributes(), Helpers.MyAttributeComparer), "valueComparison.EqualAllAttributes(e.Attributes(), Helpers.MyAttributeComparer))");
            TestLog.Compare(nodes.SequenceEqual(e.Nodes()), "nodes.EqualAll(e.Nodes())");

            e.Verify();
        }

        #endregion

        #region Methods

        private IEnumerable<ExpectedValue> getExpectedAttributes(XElement e, XName name, string value, bool isRefComparison)
        {
            bool wasReported = false;
            foreach (XAttribute a in e.Attributes())
            {
                if (a.Name == name)
                {
                    if (value == null)
                    {
                        continue;
                    }
                    yield return new ExpectedValue(isRefComparison, isRefComparison ? a : new XAttribute(name, value));
                    wasReported = true;
                }
                else
                {
                    yield return new ExpectedValue(true, a);
                }
            }
            if (!wasReported && value != null)
            {
                yield return new ExpectedValue(false, new XAttribute(name, value));
            }
        }
        #endregion
    }
}
