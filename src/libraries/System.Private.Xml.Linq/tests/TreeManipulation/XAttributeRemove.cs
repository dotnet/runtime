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
    public class XAttributeRemove : XLinqTestCase
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+XAttributeRemove
        // Test Case

        #region Fields

        private EventsHelper _eHelper;

        private bool _runWithEvents;

        #endregion

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(RemoveAttribute) { Attribute = new VariationAttribute("MIddle attribute") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns:p='ns' p:a3='pa3'/>", "a2", false }, Priority = 2 } });
            AddChild(new TestVariation(RemoveAttribute) { Attribute = new VariationAttribute("First attribute, in Document") { Params = new object[] { "<A attr='1' a2='a2' a3='a3'/>", "attr", true }, Priority = 0 } });
            AddChild(new TestVariation(RemoveAttribute) { Attribute = new VariationAttribute("Last attribute, in Document") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns:p='ns' p:a3='pa3'/>", "{ns}a3", true }, Priority = 1 } });
            AddChild(new TestVariation(RemoveAttribute) { Attribute = new VariationAttribute("Last attribute") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns:p='ns' p:a3='pa3'/>", "{ns}a3", false }, Priority = 2 } });
            AddChild(new TestVariation(RemoveAttribute) { Attribute = new VariationAttribute("Remove namespace, in Document") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns:p='ns' p:a3='pa3'/>", "{http://www.w3.org/2000/xmlns/}p", true }, Priority = 1 } });
            AddChild(new TestVariation(RemoveAttribute) { Attribute = new VariationAttribute("Remove namespace") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns:p='ns' p:a3='pa3'/>", "{http://www.w3.org/2000/xmlns/}p", false }, Priority = 2 } });
            AddChild(new TestVariation(RemoveAttribute) { Attribute = new VariationAttribute("Remove default namespace, in Document") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns='def' xmlns:p='ns' p:a3='pa3'/>", "xmlns", true }, Priority = 1 } });
            AddChild(new TestVariation(RemoveAttribute) { Attribute = new VariationAttribute("Remove default namespace") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns:p='ns' xmlns='def' p:a3='pa3'/>", "xmlns", false }, Priority = 2 } });
            AddChild(new TestVariation(RemoveAttribute) { Attribute = new VariationAttribute("The only attribute, in Document") { Params = new object[] { "<A attr='1'/>", "attr", true }, Priority = 1 } });
            AddChild(new TestVariation(RemoveAttribute) { Attribute = new VariationAttribute("The only attribute") { Params = new object[] { "<A attr='1'/>", "attr", false }, Priority = 2 } });
            AddChild(new TestVariation(RemoveAttribute) { Attribute = new VariationAttribute("First attribute") { Params = new object[] { "<A attr='1' a2='a2' a3='a3'/>", "attr", false }, Priority = 2 } });
            AddChild(new TestVariation(RemoveAttribute) { Attribute = new VariationAttribute("MIddle attribute, in Document") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns:p='ns' p:a3='pa3'/>", "a2", true }, Priority = 0 } });
            AddChild(new TestVariation(RemoveStandaloneAttribute) { Attribute = new VariationAttribute("Remove standalone attribute I.") { Params = new object[] { "{a}aa", "value" }, Priority = 2 } });
            AddChild(new TestVariation(RemoveStandaloneAttribute) { Attribute = new VariationAttribute("Remove standalone attribute - namespace") { Params = new object[] { "{http://www.w3.org/2000/xmlns/}p", "value" }, Priority = 2 } });
            AddChild(new TestVariation(RemoveStandaloneAttribute) { Attribute = new VariationAttribute("Remove standalone attribute - def namespace") { Params = new object[] { "xmlns", "value" }, Priority = 2 } });
            AddChild(new TestVariation(RemoveStandaloneAttribute) { Attribute = new VariationAttribute("Remove standalone attribute II.") { Params = new object[] { "aa", "value" }, Priority = 2 } });
        }

        // Remove standalone attribute
        // Remove the only attribute
        // Remove from multiple attributes - first, middle, last
        // Remove namespace decl {http://www.w3.org/2000/xmlns/}

        //[Variation(Priority = 1, Desc = "The only attribute, in Document", Params = new object[] { @"<A attr='1'/>", "attr", true })]
        //[Variation(Priority = 2, Desc = "The only attribute", Params = new object[] { @"<A attr='1'/>", "attr", false })]
        //[Variation(Priority = 0, Desc = "First attribute, in Document", Params = new object[] { @"<A attr='1' a2='a2' a3='a3'/>", "attr", true })]
        //[Variation(Priority = 2, Desc = "First attribute", Params = new object[] { @"<A attr='1' a2='a2' a3='a3'/>", "attr", false })]
        //[Variation(Priority = 0, Desc = "MIddle attribute, in Document", Params = new object[] { @"<A attr='1' a2='a2' a3='a3' xmlns:p='ns' p:a3='pa3'/>", "a2", true })]
        //[Variation(Priority = 2, Desc = "MIddle attribute", Params = new object[] { @"<A attr='1' a2='a2' a3='a3' xmlns:p='ns' p:a3='pa3'/>", "a2", false })]
        //[Variation(Priority = 1, Desc = "Last attribute, in Document", Params = new object[] { @"<A attr='1' a2='a2' a3='a3' xmlns:p='ns' p:a3='pa3'/>", "{ns}a3", true })]
        //[Variation(Priority = 2, Desc = "Last attribute", Params = new object[] { @"<A attr='1' a2='a2' a3='a3' xmlns:p='ns' p:a3='pa3'/>", "{ns}a3", false })]
        //[Variation(Priority = 1, Desc = "Remove namespace, in Document", Params = new object[] { @"<A attr='1' a2='a2' a3='a3' xmlns:p='ns' p:a3='pa3'/>", "{http://www.w3.org/2000/xmlns/}p", true })]
        //[Variation(Priority = 2, Desc = "Remove namespace", Params = new object[] { @"<A attr='1' a2='a2' a3='a3' xmlns:p='ns' p:a3='pa3'/>", "{http://www.w3.org/2000/xmlns/}p", false })]
        //[Variation(Priority = 1, Desc = "Remove default namespace, in Document", Params = new object[] { @"<A attr='1' a2='a2' a3='a3' xmlns='def' xmlns:p='ns' p:a3='pa3'/>", "xmlns", true })]
        //[Variation(Priority = 2, Desc = "Remove default namespace", Params = new object[] { @"<A attr='1' a2='a2' a3='a3' xmlns:p='ns' xmlns='def' p:a3='pa3'/>", "xmlns", false })]
        public void RemoveAttribute()
        {
            _runWithEvents = (bool)Params[0];
            var xml = (string)Variation.Params[0];
            var attrName = (string)Variation.Params[1];
            var useDoc = (bool)Variation.Params[2];

            XElement elem = useDoc ? XDocument.Parse(xml).Root : XElement.Parse(xml);
            XAttribute a = elem.Attribute(attrName);
            a.Verify();

            List<ExpectedValue> expValues = GetExpectedResults(elem, a).ToList();

            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(elem);
            }
            a.Remove();
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, a);
            }
            a.Verify();

            TestLog.Compare(a.Parent, null, "Parent after");
            TestLog.Compare(a.NextAttribute, null, "NextAttribute after");
            TestLog.Compare(a.PreviousAttribute, null, "PrevAttribute after");

            try
            {
                a.Remove();
                TestLog.Compare(false, "Exception was expected here");
            }
            catch (InvalidOperationException)
            {
                // Expected exception
            }

            TestLog.Compare(expValues.EqualAllAttributes(elem.Attributes(), Helpers.MyAttributeComparer), "The rest of the attributes");

            elem.Verify();
        }

        //[Variation(Priority = 2, Desc = "Remove standalone attribute - def namespace", Params = new object[] { "xmlns", "value" })]
        //[Variation(Priority = 2, Desc = "Remove standalone attribute - namespace", Params = new object[] { "{http://www.w3.org/2000/xmlns/}p", "value" })]
        //[Variation(Priority = 2, Desc = "Remove standalone attribute I.", Params = new object[] { "{a}aa", "value" })]
        //[Variation(Priority = 2, Desc = "Remove standalone attribute II.", Params = new object[] { "aa", "value" })]
        public void RemoveStandaloneAttribute()
        {
            _runWithEvents = (bool)Params[0];
            var name = (string)Variation.Params[0];
            var value = (string)Variation.Params[1];
            var a = new XAttribute(name, value);

            try
            {
                if (_runWithEvents)
                {
                    _eHelper = new EventsHelper(a);
                }
                a.Remove();
                if (_runWithEvents)
                {
                    _eHelper.Verify(XObjectChange.Remove, a);
                }
                TestLog.Compare(false, "Exception was expected here");
            }
            catch (InvalidOperationException)
            {
                // Expected exception
            }
            a.Verify();
        }

        #endregion

        #region Methods

        private IEnumerable<ExpectedValue> GetExpectedResults(XElement elem, XAttribute toExclude)
        {
            foreach (XAttribute a in elem.Attributes())
            {
                if (a != toExclude)
                {
                    yield return new ExpectedValue(true, a);
                }
            }
        }
        #endregion
    }
}
