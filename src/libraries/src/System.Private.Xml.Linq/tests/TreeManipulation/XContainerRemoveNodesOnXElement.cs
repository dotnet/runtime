// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XmlCoreTest.Common;

namespace XLinqTests
{
    public class XContainerRemoveNodesOnXElement : XLinqTestCase
    {
        private EventsHelper _eHelper;
        private bool _runWithEvents;

        public override void AddChildren()
        {
            AddChild(new TestVariation(OnXElement1) { Attribute = new VariationAttribute("On XElement - empty I.") { Param = "<A></A>", Priority = 1 } });
            AddChild(new TestVariation(OnXElement1) { Attribute = new VariationAttribute("On XElement - with attributes") { Param = "<A xmlns='nsa' xmlns:p='nsp' p:a='aa'>text<B/><p:B/><?PI?></A>", Priority = 0 } });
            AddChild(new TestVariation(OnXElement1) { Attribute = new VariationAttribute("On XElement - child elements only") { Param = "<A><B>repere</B><C/></A>", Priority = 1 } });
            AddChild(new TestVariation(OnXElement1) { Attribute = new VariationAttribute("On XElement - empty II.") { Param = "<A/>", Priority = 1 } });
            AddChild(new TestVariation(OnXElement1) { Attribute = new VariationAttribute("On XElement - string content only") { Param = "<A>text</A>", Priority = 1 } });
            AddChild(new TestVariation(OnXElement1) { Attribute = new VariationAttribute("On XElement - without attributes") { Param = "<A><B/>text<!--comment--></A>", Priority = 1 } });
            AddChild(new TestVariation(OnXElement2) { Attribute = new VariationAttribute("On XElement - empty string content") { Priority = 2 } });
            AddChild(new TestVariation(OnXElement3) { Attribute = new VariationAttribute("On XElement - loaded content") { Param = Path.Combine("TestData", "XLinq", "books.xml"), Priority = 0 } });
        }

        /// <summary>
        /// On XElement
        ///  ~ With and Without attributes
        ///  ~ Empty
        ///  ~ Not Empty
        ///      text node only
        ///      mixed content
        ///      children but not mixed content
        /// </summary>
        public void OnXElement1()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            var xml = Variation.Param as string;
            XElement e = XElement.Parse(xml);
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(e);
                count = e.Nodes().Count();
            }
            VerifyRemoveNodes(e);
            if (_runWithEvents && !xml.Equals(@"<A></A>"))
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void OnXElement2()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            var e = new XElement("A", new XText(""));
            TestLog.Compare(e.Nodes().Any(), "Test failed:: e.Nodes().Any()");
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(e);
                count = e.Nodes().Count();
            }
            VerifyRemoveNodes(e);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void OnXElement3()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            var filename = (string)Variation.Param;
            XElement e1 = XElement.Load(FilePathUtil.getStream(filename));
            foreach (XElement e in e1.Elements())
            {
                if (_runWithEvents)
                {
                    _eHelper = new EventsHelper(e);
                    count = e.Nodes().Count();
                }
                VerifyRemoveNodes(e);
                if (_runWithEvents)
                {
                    _eHelper.Verify(XObjectChange.Remove, count);
                }
            }
        }

        private void VerifyRemoveNodes(XElement e)
        {
            bool hadAttributes = e.HasAttributes;
            IEnumerable<XAttribute> attributesBefore = e.Attributes().ToList();
            IEnumerable<XNode> nodesBefore = e.Nodes().ToList();

            e.RemoveNodes();

            TestLog.Compare(e.HasAttributes == hadAttributes, "e.HasAttributes == hadAttributes");
            TestLog.Compare(!e.HasElements, "!e.HasElements");
            TestLog.Compare(e.Nodes().IsEmpty(), "e.Nodes().IsEmpty()");
            TestLog.Compare(e.IsEmpty, "e.IsEmpty");
            TestLog.Compare(attributesBefore.SequenceEqual(e.Attributes()), "attributesBefore.SequenceEqual(e.Attributes())");
            TestLog.Compare(nodesBefore.Where(n => n.Parent != null).IsEmpty(), "nodesBefore.Where(n=>n.Parent!=null).IsEmpty()");
            TestLog.Compare(nodesBefore.Where(n => n.Document != null).IsEmpty(), "nodesBefore.Where(n=>n.Parent!=null).IsEmpty()");
        }
    }
}
