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
    public class AddNodeBeforeAfterBase : XLinqTestCase
    {
        #region Fields

        protected EventsHelper eHelper;

        protected bool runWithEvents;

        #endregion

        #region Delegates

        public delegate IEnumerable<ExpectedValue> CalculateExpectedValues(XContainer orig, int startPos, IEnumerable<object> newNodes);

        public delegate void TestedFunction(XNode n, params object[] content);

        #endregion

        #region Public Methods and Operators

        public override void AddChildren()
        {
        }

        public void AddingMultipleNodesIntoElement(TestedFunction testedFunction, CalculateExpectedValues calculateExpectedValues)
        {
            runWithEvents = (bool)Params[0];
            var isConnected = (bool)Variation.Params[0];
            var lengthOfVariations = (int)Variation.Params[1];

            object[] toAdd = { new XElement("ToAddEmpty"), new XElement("ToAddWithAttr", new XAttribute("id", "a1")), new XElement("ToAddWithContent", new XAttribute("id", "a1"), new XElement("inner", "innerContent"), "content"), new XProcessingInstruction("PiWithData", "data"), "", new XProcessingInstruction("PiNOData", ""), new XComment("comment"), new XCData("xtextCdata"), new XText("xtext"), "plaintext1", "plaintext2", null };

            if (isConnected)
            {
                var dummy = new XElement("dummy", toAdd);
            }

            var referenceElement = new XElement("testElement", "text0", new XElement("tin"), new XElement("tin2", new XAttribute("id", "a2")), //
                "text1", new XAttribute("hu", "ha"), new XProcessingInstruction("PI", "data"), new XText("heleho"), new XComment("M&M"), "textEnd");

            for (int startPos = 0; startPos < referenceElement.Nodes().Count(); startPos++)
            {
                // iterate over all nodes in the original element
                foreach (var newNodes in toAdd.NonRecursiveVariations(lengthOfVariations))
                {
                    var orig = new XElement(referenceElement);

                    IEnumerable<ExpectedValue> expectedNodes = Helpers.ProcessNodes(calculateExpectedValues(orig, startPos, newNodes)).ToList();

                    // Add node on the expected place
                    XNode n = orig.FirstNode;
                    for (int position = 0; position < startPos; position++)
                    {
                        n = n.NextNode;
                    }

                    if (runWithEvents)
                    {
                        eHelper = new EventsHelper(orig);
                    }
                    testedFunction(n, newNodes);

                    // Node Equals check
                    TestLog.Compare(expectedNodes.EqualAll(orig.Nodes(), XNode.EqualityComparer), "constructed != added :: nodes Deep equals");

                    // release nodes
                    orig.RemoveAll();
                }
            }
        }

        public void ValidAddIntoXDocument(TestedFunction testedFunction, CalculateExpectedValues calculateExpectedValues)
        {
            runWithEvents = (bool)Params[0];
            var isConnected = (bool)Variation.Params[0];
            var combCount = (int)Variation.Params[1];

            object[] nodes = { new XDocumentType("root", "", "", ""), new XDocumentType("roo2t", "", "", ""), new XProcessingInstruction("PI", "data"), new XComment("Comment"), new XElement("elem1"), new XElement("elem2", new XElement("C", "nodede")), new XText(""), new XText(" "), new XText("\t"), new XCData(""), new XCData("<A/>"), " ", "\t", "", null };

            object[] origNodes = { new XProcessingInstruction("OO", "oo"), new XComment("coco"), " ", new XDocumentType("root", null, null, null), new XElement("anUnexpectedlyLongNameForTheRootElement") };

            if (isConnected)
            {
                new XElement("foo", nodes.Where(n => n is XNode && !(n is XDocumentType)));
                foreach (XNode nn in nodes.Where(n => n is XDocumentType))
                {
                    new XDocument(nn);
                }
            }

            foreach (var origs in origNodes.NonRecursiveVariations(2))
            {
                if (origs.Select(o => new ExpectedValue(false, o)).IsXDocValid())
                {
                    continue;
                }
                foreach (var o in nodes.NonRecursiveVariations(combCount))
                {
                    var doc = new XDocument(origs);
                    XNode n = doc.FirstNode;

                    List<ExpectedValue> expNodes = Helpers.ProcessNodes(calculateExpectedValues(doc, 0, o)).ToList();
                    bool shouldFail = expNodes.IsXDocValid();

                    try
                    {
                        if (runWithEvents)
                        {
                            eHelper = new EventsHelper(doc);
                        }
                        testedFunction(n, o);
                        TestLog.Compare(!shouldFail, "should fail - exception expected here");
                        TestLog.Compare(expNodes.EqualAll(doc.Nodes(), XNode.EqualityComparer), "nodes does not pass");
                    }
                    catch (InvalidOperationException ex)
                    {
                        TestLog.Compare(shouldFail, "Unexpected exception : " + ex.Message);
                    }
                    catch (ArgumentException ex)
                    {
                        TestLog.Compare(shouldFail, "Unexpected exception : " + ex.Message);
                    }
                    finally
                    {
                        doc.RemoveNodes();
                    }
                }
            }
        }
        #endregion

        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+AddNodeBeforeAfterBase
        // Test Case
    }
}
