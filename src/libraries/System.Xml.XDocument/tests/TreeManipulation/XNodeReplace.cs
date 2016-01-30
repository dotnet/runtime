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
    public abstract class XNodeReplace : XLinqTestCase
    {
        private EventsHelper _eHelper;
        private bool _runWithEvents;
        private object[] Data4XDocument
        {
            get
            {
                return new object[] { new XElement("X"), new XElement("X1", new XAttribute("id", "myID")), new XDocumentType("root", null, null, null), new XDocumentType("root", null, null, null), new XElement("X2", new XElement("X1", new XAttribute("id", "myID")), "textX", new XElement("X")), new XText(""), new XText(" "), new XText("\t"), new XText("\n"), new XProcessingInstruction("PI1", ""), new XProcessingInstruction("PI2", "click"), new XComment("comma"), "", " ", "\n", "\t", null };
            }
        }

        private object[] Data4XElem
        {
            get
            {
                return new object[] { new XElement("X"), new XElement("X1", new XAttribute("id", "myID")), new XElement("X2", new XElement("X1", new XAttribute("id", "myID")), "textX", new XElement("X")), new XText("textNewBoom"), new XText(""), new XText(" "), new XCData("cdatata"), new XProcessingInstruction("PI", "click"), new XComment("comma"), "stringplain", "", " ", null };
            }
        }

        public void OnXDocument()
        {
            _runWithEvents = (bool)Params[0];
            var numOfNodes = (int)Variation.Params[0];
            var xml = (string)Variation.Params[1];
            XDocument doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);

            // not-connected nodes
            TestReplacement(doc, Data4XDocument, numOfNodes);

            // connected nodes mix
            object[] connected = Data4XDocument;
            new XDocument(new XElement("ddd", connected.Where(x => x is XNode && !(x is XDocumentType))));
            foreach (XDocumentType dtds in connected.Where(x => x is XDocumentType))
            {
                new XDocument(dtds);
            }
            TestReplacement(doc, connected, numOfNodes);
        }

        public void OnXElement()
        {
            _runWithEvents = (bool)Params[0];
            var numOfNodes = (int)Variation.Params[0];
            var xml = (string)Variation.Params[1];

            XElement e = XElement.Parse(xml);

            if (Variation.Params.Length > 2)
            {
                var doc = new XDocument();
                if ((bool)Variation.Params[2])
                {
                    doc.Add(new XElement("{nsxx}X", e));
                }
                else
                {
                    doc.Add(e);
                }
            }

            // not connected nodes
            TestReplacement(e, Data4XElem, numOfNodes);

            // connected node mix
            object[] copy = Data4XElem;
            var eTemp = new XElement("cc", copy);
            TestReplacement(e, copy, numOfNodes);
        }

        private IEnumerable<ExpectedValue> ReplaceWithExpValues(XContainer elem, XNode toReplace, IEnumerable<object> replacement)
        {
            foreach (XNode n in elem.Nodes())
            {
                if (n != toReplace)
                {
                    yield return new ExpectedValue(true, n);
                }
                else
                {
                    foreach (object o in replacement)
                    {
                        yield return new ExpectedValue(o is XNode && (o as XNode).Parent == null && (o as XNode).Document == null, o);
                    }
                }
            }
        }

        private void TestReplacement(XElement e, object[] nodes, int numOfNodes)
        {
            for (int i = 0; i < e.Nodes().Count(); i++)
            {
                foreach (var replacement in nodes.NonRecursiveVariations(numOfNodes))
                {
                    XContainer elem = new XElement(e);

                    XNode toReplace = elem.Nodes().ElementAt(i);
                    XNode prev = toReplace.PreviousNode;
                    XNode next = toReplace.NextNode;
                    bool isReplacementOriginal = replacement.Where(o => o is XNode).Where(o => (o as XNode).Parent != null).IsEmpty();

                    IEnumerable<ExpectedValue> expValues = ReplaceWithExpValues(elem, toReplace, replacement).ProcessNodes().ToList();

                    if (_runWithEvents)
                    {
                        _eHelper = new EventsHelper(elem);
                    }
                    toReplace.ReplaceWith(replacement);

                    TestLog.Compare(toReplace.Parent == null, "toReplace.Parent == null");
                    TestLog.Compare(toReplace.Document == null, "toReplace.Document == null");
                    TestLog.Compare(expValues.EqualAll(elem.Nodes(), XNode.EqualityComparer), "expected values");
                    if (isReplacementOriginal)
                    {
                        TestLog.Compare(replacement.Where(o => o is XNode).Where(o => (o as XNode).Parent != elem).IsEmpty(), "replacement.Where(o=>o is XNode).Where(o=>(o as XNode).Parent!=elem).IsEmpty()");
                        TestLog.Compare(replacement.Where(o => o is XNode).Where(o => (o as XNode).Document != elem.Document).IsEmpty(), "replacement.Where(o=>o is XNode).Where(o=>(o as XNode).Document!=elem.Document).IsEmpty()");
                    }
                    elem.RemoveNodes();
                }
            }
        }

        private void TestReplacement(XDocument e, object[] nodes, int numOfNodes)
        {
            for (int i = 0; i < e.Nodes().Count(); i++)
            {
                object[] allowedNodes = e.Nodes().ElementAt(i) is XElement ? nodes.Where(o => !(o is XElement)).ToArray() : nodes;
                foreach (var replacement in nodes.NonRecursiveVariations(numOfNodes))
                {
                    bool shouldFail = false;

                    var doc = new XDocument(e);
                    XNode toReplace = doc.Nodes().ElementAt(i);
                    XNode prev = toReplace.PreviousNode;
                    XNode next = toReplace.NextNode;
                    bool isReplacementOriginal = replacement.Where(o => o is XNode).Where(o => (o as XNode).Document != null).IsEmpty();

                    IEnumerable<ExpectedValue> expValues = ReplaceWithExpValues(doc, toReplace, replacement).ProcessNodes().ToList();

                    // detect invalid states
                    shouldFail = expValues.IsXDocValid();

                    try
                    {
                        if (_runWithEvents)
                        {
                            _eHelper = new EventsHelper(doc);
                        }
                        toReplace.ReplaceWith(replacement);

                        TestLog.Compare(!shouldFail, "Should fail ... ");
                        TestLog.Compare(toReplace.Parent == null, "toReplace.Parent == null");
                        TestLog.Compare(toReplace.Document == null, "toReplace.Document == null");
                        TestLog.Compare(expValues.EqualAll(doc.Nodes(), XNode.EqualityComparer), "expected values");
                        if (isReplacementOriginal)
                        {
                            TestLog.Compare(replacement.Where(o => o is XNode).Where(o => (o as XNode).Document != doc.Document).IsEmpty(), "replacement.Where(o=>o is XNode).Where(o=>(o as XNode).Document!=doc.Document).IsEmpty()");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        TestLog.Compare(shouldFail, "Exception not expected here");
                    }
                    catch (ArgumentException)
                    {
                        TestLog.Compare(shouldFail, "Exception not expected here");
                    }
                    finally
                    {
                        doc.RemoveNodes();
                    }
                }
            }
        }
    }
}
