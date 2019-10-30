// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;
using System;
using System.Xml.Linq;

namespace XLinqTests
{
    public class AddFirstInvalidIntoXDocument : XLinqTestCase
    {
        private EventsHelper _eHelper;
        private bool _runWithEvents;

        public override void AddChildren()
        {
            AddChild(new TestVariation(InvalidAddIntoXDocument1) { Attribute = new VariationAttribute("XDocument invalid add - double DTD") { Priority = 1 } });
            AddChild(new TestVariation(InvalidAddIntoXDocument3) { Attribute = new VariationAttribute("XDocument invalid add - multiple root elements") { Priority = 1 } });
            AddChild(new TestVariation(InvalidAddIntoXDocument5) { Attribute = new VariationAttribute("XDocument invalid add - CData, attribute, text (no whitespace)") { Priority = 1 } });
        }

        public void InvalidAddIntoXDocument1()
        {
            _runWithEvents = (bool)Params[0];
            try
            {
                var doc = new XDocument(new XDocumentType("root", null, null, null), new XElement("A"));
                var o = new XDocumentType("D", null, null, null);
                if (_runWithEvents)
                {
                    _eHelper = new EventsHelper(doc);
                }
                doc.AddFirst(o);
                if (_runWithEvents)
                {
                    _eHelper.Verify(XObjectChange.Add, o);
                }
                TestLog.Compare(false, "Exception expected");
            }
            catch (InvalidOperationException)
            {
            }
        }

        public void InvalidAddIntoXDocument3()
        {
            _runWithEvents = (bool)Params[0];
            try
            {
                var doc = new XDocument(new XProcessingInstruction("pi", "halala"), new XElement("A"));
                var o = new XElement("C");
                if (_runWithEvents)
                {
                    _eHelper = new EventsHelper(doc);
                }
                doc.AddFirst(o);
                if (_runWithEvents)
                {
                    _eHelper.Verify(XObjectChange.Add, o);
                }
                TestLog.Compare(false, "Exception expected");
            }
            catch (InvalidOperationException)
            {
            }
        }

        public void InvalidAddIntoXDocument5()
        {
            _runWithEvents = (bool)Params[0];
            foreach (object o in new object[] { new XCData("CD"), new XAttribute("a1", "avalue"), "text1", new XText("text2"), new XDocument() })
            {
                try
                {
                    var doc = new XDocument(new XElement("A"));
                    if (_runWithEvents)
                    {
                        _eHelper = new EventsHelper(doc);
                    }
                    doc.AddFirst(o);
                    if (_runWithEvents)
                    {
                        _eHelper.Verify(XObjectChange.Add, o);
                    }
                    TestLog.Compare(false, "Exception expected");
                }
                catch (ArgumentException)
                {
                }
            }
        }
    }
}
