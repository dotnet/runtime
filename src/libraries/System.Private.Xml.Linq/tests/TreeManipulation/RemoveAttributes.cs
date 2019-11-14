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
    public class RemoveAttributes : XLinqTestCase
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+RemoveAttributes
        // Test Case

        #region Fields

        private EventsHelper _eHelper;

        private bool _runWithEvents;

        #endregion

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(NoAttributes) { Attribute = new VariationAttribute("No attributes") { Priority = 1 } });
            AddChild(new TestVariation(DeleteAttributes) { Attribute = new VariationAttribute("Single attribute") { Params = new object[] { "<square wIdth='1'>text<B/><!--comment--></square>", new[] { "wIdth" } }, Priority = 2 } });
            AddChild(new TestVariation(DeleteAttributes) { Attribute = new VariationAttribute("Default attribute from DTD") { Params = new object[] { "<!DOCTYPE square [<!ELEMENT square (B*)><!ATTLIST square wIdth CDATA '10'>]><square>text<B/><!--comment--></square>", new[] { "wIdth" } }, Priority = 2 } });
            AddChild(new TestVariation(DeleteAttributes) { Attribute = new VariationAttribute("Single namespace") { Params = new object[] { "<p:square xmlns:p='x'>text<B/><!--comment--></p:square>", new[] { "{http://www.w3.org/2000/xmlns/}p" } }, Priority = 2 } });
            AddChild(new TestVariation(DeleteAttributes) { Attribute = new VariationAttribute("Single namespace (default)") { Params = new object[] { "<square xmlns='x'>text<B/><!--comment--></square>", new[] { "xmlns" } }, Priority = 2 } });
            AddChild(new TestVariation(DeleteAttributes) { Attribute = new VariationAttribute("Multiple attributes") { Params = new object[] { "<square wIdth='1' depth='11'><sub1/>text</square>", new[] { "wIdth", "depth" } }, Priority = 2 } });
            AddChild(new TestVariation(DeleteAttributes) { Attribute = new VariationAttribute("Multiple namespaces") { Params = new object[] { "<p:square xmlns:p='x' xmlns='x1'><p:A/><B/></p:square>", new[] { "{http://www.w3.org/2000/xmlns/}p", "{http://www.w3.org/2000/xmlns/}p" } }, Priority = 2 } });
            AddChild(new TestVariation(DeleteAttributes) { Attribute = new VariationAttribute("Multiple namespaces + attributes") { Params = new object[] { "<p:square xmlns:p='x' xmlns='x1' a1='xxx' p:a1='yyy'><p:A/><B p:x='ayay'/></p:square>", new[] { "{http://www.w3.org/2000/xmlns/}p", "{http://www.w3.org/2000/xmlns/}p", "a1", "{x}a1" } }, Priority = 2 } });
        }

        // no attributes
        // single attribute/namespace
        // multiple attributes/namespaces
        // default attributes ... @"<!DOCTYPE square [<!ELEMENT square EMPTY><!ATTLIST square width CDATA "10">]><square/>"

        //[Variation(Priority = 1, Desc = "No attributes")]

        //[Variation(Priority = 2, Desc = "Default attribute from DTD", Params = new object[] { @"<!DOCTYPE square [<!ELEMENT square (B*)><!ATTLIST square wIdth CDATA '10'>]><square>text<B/><!--comment--></square>", new string[] { "wIdth" } })]
        //[Variation(Priority = 2, Desc = "Single attribute", Params = new object[] { @"<square wIdth='1'>text<B/><!--comment--></square>", new string[] { "wIdth" } })]
        //[Variation(Priority = 2, Desc = "Single namespace (default)", Params = new object[] { @"<square xmlns='x'>text<B/><!--comment--></square>", new string[] { "xmlns" } })]
        //[Variation(Priority = 2, Desc = "Single namespace", Params = new object[] { @"<p:square xmlns:p='x'>text<B/><!--comment--></p:square>", new string[] { "{http://www.w3.org/2000/xmlns/}p" } })]
        //[Variation(Priority = 2, Desc = "Multiple attributes", Params = new object[] { @"<square wIdth='1' depth='11'><sub1/>text</square>", new string[] { "wIdth", "depth" } })]
        //[Variation(Priority = 2, Desc = "Multiple namespaces", Params = new object[] { @"<p:square xmlns:p='x' xmlns='x1'><p:A/><B/></p:square>", new string[] { "{http://www.w3.org/2000/xmlns/}p", "{http://www.w3.org/2000/xmlns/}p" } })]
        //[Variation(Priority = 2, Desc = "Multiple namespaces + attributes", Params = new object[] { @"<p:square xmlns:p='x' xmlns='x1' a1='xxx' p:a1='yyy'><p:A/><B p:x='ayay'/></p:square>", new string[] { "{http://www.w3.org/2000/xmlns/}p", "{http://www.w3.org/2000/xmlns/}p", "a1", "{x}a1" } })]

        public void DeleteAttributes()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            var xml = Variation.Params[0] as string;
            var attNames = Variation.Params[1] as string[];

            XDocument doc = XDocument.Parse(xml);

            TestLog.Compare(doc.Root.HasAttributes, "HasAttributes");
            TestLog.Compare(doc.Root.Attributes().Count(), attNames.Count(), "default attribute - Attributes()");
            foreach (string name in attNames)
            {
                TestLog.Compare(doc.Root.Attribute(name) != null, "Attribute (XName) before");
            }
            IEnumerable<XNode> origNodes = doc.Root.Nodes().ToList();
            IEnumerable<XAttribute> origAttributes = doc.Root.Attributes().ToList();

            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc.Root);
                count = origAttributes.IsEmpty() ? 0 : origAttributes.Count();
            }
            doc.Root.RemoveAttributes();
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }

            TestLog.Compare(!doc.Root.HasAttributes, "default attribute not deleted");
            TestLog.Compare(doc.Root.Attributes().IsEmpty(), "default attribute - Attributes()");
            TestLog.Compare(doc.Root.FirstAttribute == null, "FirstAttribute");
            TestLog.Compare(doc.Root.LastAttribute == null, "LastAttribute");
            TestLog.Compare(doc.Root.Nodes().SequenceEqual(origNodes), "Content");
            TestLog.Compare(origAttributes.Where(a => a.Parent != null).IsEmpty(), "Deleted attributes property");

            foreach (string name in attNames)
            {
                TestLog.Compare(doc.Root.Attribute(name) == null, "Attribute (XName) after: " + name);
            }
        }

        public void NoAttributes()
        {
            _runWithEvents = (bool)Params[0];
            var e = new XElement("name", "some content", new XElement("child", ""));
            var eCopy = new XElement(e);
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(e);
            }
            e.RemoveAttributes();
            if (_runWithEvents)
            {
                _eHelper.Verify(0);
            }
            TestLog.Compare(XNode.EqualityComparer.Equals(e, eCopy), "Deep equals");
        }
        #endregion
    }
}
