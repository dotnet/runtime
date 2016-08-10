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
    public abstract class XContainerReplaceNodes : XLinqTestCase
    {
        private EventsHelper _eHelper;
        private bool _runWithEvents;

        private object[] Data4XDocument
        {
            get
            {
                return new object[] { new XElement("X2", new XElement("X1", new XAttribute("id", "myID")), "textX", new XElement("X")), new XElement("X2", new XElement("X1", new XAttribute("id", "myID")), "textX", new XElement("X")), new XDocumentType("A", null, null, null), new XDocumentType("X2", null, null, null), new XText(""), new XText(" "), new XText("\n"), new XText("\t"), new XCData("cdatata"), new XProcessingInstruction("PI", "click"), new XComment("comma"), "\n", "\t", "", " ", null };
            }
        }

        private object[] Data4XElem
        {
            get
            {
                return new object[] { new XElement("X"), new XElement("X1", new XAttribute("id", "myID")), new XElement("X2", new XElement("X1", new XAttribute("id", "myID")), "textX", new XElement("X")), new XText("textNewBoom"), new XText(""), new XText(" "), new XCData("cdatata"), new XProcessingInstruction("PI", "click"), new XComment("comma"), "stringplain", "", " ", null };
            }
        }

        protected void OnDocument()
        {
            _runWithEvents = (bool)Params[0];
            var numOfNodes = (int)Variation.Params[0];
            var xml = (string)Variation.Params[1];
            var touchNodes = (bool)Variation.Params[2];

            //Workaround for problem with loading doc without root elem
            XDocument doc = null;
            switch (xml)
            {
                case "":
                    doc = new XDocument();
                    break;
                case "\t":
                    doc = new XDocument("\t");
                    break;
                default:
                    doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
                    break;
            }

            TestReplacement(doc, Data4XDocument, numOfNodes, touchNodes, 1);

            object[] data = Data4XDocument;
            // connecting nodes into the allowed places
            new XElement("foo", data.Where(n => n is XNode && !(n is XDocumentType)));
            foreach (XNode nn in data.Where(n => n is XDocumentType))
            {
                new XDocument(nn);
            }
            TestReplacement(doc, data, numOfNodes, touchNodes, 1);
        }

        protected void OnXElement()
        {
            _runWithEvents = (bool)Params[0];
            var numOfNodes = (int)Variation.Params[0];
            var xml = (string)Variation.Params[1];
            var touchNodes = (bool)Variation.Params[2];

            XElement e = XElement.Parse(xml);
            TestReplacement(e, Data4XElem, numOfNodes, touchNodes, 2);
            e.Verify();

            // Connect input data
            object[] data = Data4XElem;
            new XDocument(new XElement("pseudoRoot", data));
            TestReplacement(e, data, numOfNodes, touchNodes, 2);
            e.Verify();
        }

        /// <summary>
        /// <param name="e"></param>
        /// <param name="nodes"></param>
        /// <param name="numOfNodes"></param>
        /// <param name="touchOrigNodes"></param>
        /// <param name="type">type 1 = XDocument; type 2 = XElement</param>
        /// </summary>
        private void TestReplacement(XContainer e, object[] nodes, int numOfNodes, bool touchOrigNodes, int type)
        {
            int count = 0;
            foreach (var replacement in nodes.NonRecursiveVariations(numOfNodes))
            {
                XContainer elem;
                if (type == 1)
                {
                    elem = new XDocument(e as XDocument);
                }
                else
                {
                    elem = new XElement(e as XElement);
                }

                IEnumerable<ExpectedValue> expValues = replacement.Select(o => new ExpectedValue(o is XNode && (o as XNode).Parent == null && (o as XNode).Document == null, o)).ProcessNodes().ToList();
                IEnumerable<XNode> originalContent = touchOrigNodes ? elem.Nodes().ToList() : null;

                bool shouldFail = (e is XDocument) && expValues.IsXDocValid();
                try
                {
                    if (_runWithEvents)
                    {
                        _eHelper = new EventsHelper(elem);
                        count = elem.Nodes().Count();
                    }
                    elem.ReplaceNodes(replacement);

                    TestLog.Compare(!shouldFail, "Should fail ... ");
                    TestLog.Compare(originalContent == null || originalContent.Where(o => o.Parent != null).IsEmpty(), "originalContent.Where(o=>o.Parent!=null).IsEmpty()");
                    TestLog.Compare(originalContent == null || originalContent.Where(o => o.Document != null).IsEmpty(), "originalContent.Where(o=>o.Document!=null).IsEmpty()");
                    TestLog.Compare(expValues.EqualAll(elem.Nodes(), XNode.EqualityComparer), "expected values");
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
                    elem.RemoveNodes();
                }
            }
        }
    }
}
