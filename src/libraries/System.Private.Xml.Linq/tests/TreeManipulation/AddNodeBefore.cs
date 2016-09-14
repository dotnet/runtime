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
    public class AddNodeBefore : AddNodeBeforeAfterBase
    {
        // to test:
        // - order
        // - parent, document property
        // - query
        // - value
        // - assigning/cloning nodes (connected, not connected)
        // ----

        // add valid:
        //  - elem (just root), PI, Comment, XDecl/XDocType (order!)

        //[Variation(Priority = 1, Desc = "Adding multiple (4) objects into XElement - not connected", Params = new object[] { false, 4 })]
        //[Variation(Priority = 1, Desc = "Adding multiple (4) objects into XElement - connected", Params = new object[] { true, 4 })]
        //[Variation(Priority = 1, Desc = "Adding single object into XElement - not connected", Params = new object[] { false, 1 })]
        //[Variation(Priority = 1, Desc = "Adding single object into XElement - connected", Params = new object[] { true, 1 })]

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(AddingMultipleNodesIntoElement) { Attribute = new VariationAttribute("Adding single object into XElement - connected") { Params = new object[] { true, 1 }, Priority = 1 } });
            AddChild(new TestVariation(AddingMultipleNodesIntoElement) { Attribute = new VariationAttribute("Adding multiple (4) objects into XElement - connected") { Params = new object[] { true, 4 }, Priority = 1 } });
            AddChild(new TestVariation(AddingMultipleNodesIntoElement) { Attribute = new VariationAttribute("Adding multiple (4) objects into XElement - not connected") { Params = new object[] { false, 4 }, Priority = 1 } });
            AddChild(new TestVariation(AddingMultipleNodesIntoElement) { Attribute = new VariationAttribute("Adding single object into XElement - not connected") { Params = new object[] { false, 1 }, Priority = 1 } });
            AddChild(new TestVariation(InvalidNodeTypes) { Attribute = new VariationAttribute("Invalid node types - single object") { Priority = 2 } });
            AddChild(new TestVariation(ValidAddIntoXDocument) { Attribute = new VariationAttribute("XDocument valid add - connected (single)") { Params = new object[] { true, 1 }, Priority = 0 } });
            AddChild(new TestVariation(ValidAddIntoXDocument) { Attribute = new VariationAttribute("XDocument valid add - connected (multiple)") { Params = new object[] { true, 3 }, Priority = 1 } });
            AddChild(new TestVariation(ValidAddIntoXDocument) { Attribute = new VariationAttribute("XDocument valid add - not connected (single)") { Params = new object[] { false, 1 }, Priority = 0 } });
            AddChild(new TestVariation(ValidAddIntoXDocument) { Attribute = new VariationAttribute("XDocument valid add - not connected (multiple)") { Params = new object[] { false, 3 }, Priority = 1 } });
            AddChild(new TestVariation(InvalidAddIntoXDocument) { Attribute = new VariationAttribute("XDocument Invalid Add") { Priority = 0 } });
            AddChild(new TestVariation(InvalidAddIntoXDocument1) { Attribute = new VariationAttribute("XDocument invalid add - double DTD") { Priority = 1 } });
            AddChild(new TestVariation(InvalidAddIntoXDocument2) { Attribute = new VariationAttribute("XDocument invalid add - DTD after element") { Priority = 1 } });
            AddChild(new TestVariation(InvalidAddIntoXDocument3) { Attribute = new VariationAttribute("XDocument invalid add - multiple root elements") { Priority = 1 } });
            AddChild(new TestVariation(InvalidAddIntoXDocument5) { Attribute = new VariationAttribute("XDocument invalid add - CData, attribute, text (no whitespace)") { Priority = 1 } });
            AddChild(new TestVariation(WorkOnTextNodes1) { Attribute = new VariationAttribute("Working on the text nodes 1.") { Priority = 1 } });
            AddChild(new TestVariation(WorkOnTextNodes2) { Attribute = new VariationAttribute("Working on the text nodes 2.") { Priority = 1 } });
        }

        public void AddingMultipleNodesIntoElement()
        {
            AddingMultipleNodesIntoElement(delegate(XNode n, object[] content) { n.AddBeforeSelf(content); }, CalculateExpectedValuesAddBefore);
        }

        public IEnumerable<ExpectedValue> CalculateExpectedValuesAddBefore(XContainer orig, int startPos, IEnumerable<object> newNodes)
        {
            int counter = 0;
            for (XNode node = orig.FirstNode; node != null; node = node.NextNode, counter++)
            {
                if (counter == startPos)
                {
                    foreach (object o in newNodes)
                    {
                        yield return new ExpectedValue(o is XNode && (o as XNode).Parent == null && (o as XNode).Document == null, o);
                    }
                }
                yield return new ExpectedValue(true, node); // Don't build on the text node identity
            }
        }

        //[Variation(Priority = 2, Desc = "Invalid node types - single object")]
        //[Variation(Priority = 0, Desc = "XDocument Invalid Add")]
        public void InvalidAddIntoXDocument()
        {
            runWithEvents = (bool)Params[0];

            object[] nodes = { new XDocument(), new XAttribute("id", "a1") };

            XNode[] origNodes = { new XComment("original"), new XProcessingInstruction("OO", "oo") };

            foreach (object o in nodes)
            {
                var doc1 = new XDocument(origNodes);
                var doc2 = new XDocument(origNodes);

                try
                {
                    if (runWithEvents)
                    {
                        eHelper = new EventsHelper(doc1);
                    }
                    doc1.FirstNode.AddBeforeSelf(o);
                    if (runWithEvents)
                    {
                        eHelper.Verify(XObjectChange.Add, o);
                    }
                    TestLog.Compare(false, "Exception has been expected here");
                }
                catch (ArgumentException)
                {
                }
            }
        }

        //[Variation(Priority = 1, Desc = "XDocument invalid add - double DTD")]
        public void InvalidAddIntoXDocument1()
        {
            runWithEvents = (bool)Params[0];
            try
            {
                var doc = new XDocument(new XDocumentType("root", null, null, null), new XElement("A"));
                var o = new XDocumentType("D", null, null, null);
                if (runWithEvents)
                {
                    eHelper = new EventsHelper(doc);
                }
                doc.FirstNode.AddBeforeSelf(o);
                if (runWithEvents)
                {
                    eHelper.Verify(XObjectChange.Add, o);
                }
                TestLog.Compare(false, "Exception expected");
            }
            catch (InvalidOperationException)
            {
            }
        }

        //[Variation(Priority = 1, Desc = "XDocument invalid add - DTD after element")]
        public void InvalidAddIntoXDocument2()
        {
            runWithEvents = (bool)Params[0];
            try
            {
                var doc = new XDocument(new XElement("A"), new XComment("comm"));
                var o = new XDocumentType("D", null, null, null);
                if (runWithEvents)
                {
                    eHelper = new EventsHelper(doc);
                }
                doc.LastNode.AddBeforeSelf(o);
                if (runWithEvents)
                {
                    eHelper.Verify(XObjectChange.Add, o);
                }
                TestLog.Compare(false, "Exception expected");
            }
            catch (InvalidOperationException)
            {
            }
        }

        //[Variation(Priority = 1, Desc = "XDocument invalid add - multiple root elements")]
        public void InvalidAddIntoXDocument3()
        {
            runWithEvents = (bool)Params[0];
            try
            {
                var doc = new XDocument(new XProcessingInstruction("pi", "halala"), new XElement("A"));
                var o = new XElement("C");
                if (runWithEvents)
                {
                    eHelper = new EventsHelper(doc);
                }
                doc.LastNode.AddBeforeSelf(o);
                if (runWithEvents)
                {
                    eHelper.Verify(XObjectChange.Add, o);
                }
                TestLog.Compare(false, "Exception expected");
            }
            catch (InvalidOperationException)
            {
            }
        }

        //[Variation(Priority = 1, Desc = "XDocument invalid add - CData, attribute, text (no whitespace)")]
        public void InvalidAddIntoXDocument5()
        {
            runWithEvents = (bool)Params[0];
            foreach (object o in new object[] { new XCData("CD"), new XAttribute("a1", "avalue"), "text1", new XText("text2"), new XDocument() })
            {
                try
                {
                    var doc = new XDocument(new XElement("A"));
                    if (runWithEvents)
                    {
                        eHelper = new EventsHelper(doc);
                    }
                    doc.LastNode.AddBeforeSelf(o);
                    if (runWithEvents)
                    {
                        eHelper.Verify(XObjectChange.Add, o);
                    }
                    TestLog.Compare(false, "Exception expected");
                }
                catch (ArgumentException)
                {
                }
            }
        }

        public void InvalidNodeTypes()
        {
            runWithEvents = (bool)Params[0];
            var root = new XElement("root", new XAttribute("a", "b"), new XElement("here"), "tests");
            var rootCopy = new XElement(root);
            XElement elem = root.Element("here");

            object[] nodes = { new XAttribute("xx", "yy"), new XDocument(),
                new XDocumentType("root", null, null, null) };

            if (runWithEvents)
            {
                eHelper = new EventsHelper(elem);
            }
            foreach (object o in nodes)
            {
                try
                {
                    elem.AddBeforeSelf(o);
                    if (runWithEvents)
                    {
                        eHelper.Verify(XObjectChange.Add, o);
                    }
                    TestLog.Compare(false, "Should fail!");
                }
                catch (ArgumentException)
                {
                    TestLog.Compare(XNode.DeepEquals(root, rootCopy), "root.DeepEquals(rootCopy)");
                }
            }
        }

        //[Variation(Priority = 0, Desc = "XDocument valid add - connected (single)", Params = new object[] { true, 1 })]
        //[Variation(Priority = 0, Desc = "XDocument valid add - not connected (single)", Params = new object[] { false, 1 })]
        //[Variation(Priority = 1, Desc = "XDocument valid add - connected (multiple)", Params = new object[] { true, 3 })]
        //[Variation(Priority = 1, Desc = "XDocument valid add - not connected (multiple)", Params = new object[] { false, 3 })]
        public void ValidAddIntoXDocument()
        {
            ValidAddIntoXDocument(delegate(XNode n, object[] content) { n.AddBeforeSelf(content); }, CalculateExpectedValuesAddBefore);
        }

        //[Variation(Priority = 1, Desc = "Working on the text nodes 1.")]
        public void WorkOnTextNodes1()
        {
            runWithEvents = (bool)Params[0];
            var elem = new XElement("A", new XElement("B"), "text2", new XElement("C"));
            XNode n = elem.FirstNode.NextNode;

            if (runWithEvents)
            {
                eHelper = new EventsHelper(elem);
            }
            n.AddBeforeSelf("text0");
            n.AddBeforeSelf("text1");
            if (runWithEvents)
            {
                eHelper.Verify(new[] { XObjectChange.Add, XObjectChange.Value });
            }

            TestLog.Compare(elem.Nodes().Count(), 4, "elem.Nodes().Count(), 4");
            TestLog.Compare((n as XText).Value, "text2", "(n as XText).Value, text2");
            TestLog.Compare((n.PreviousNode as XText).Value, "text0text1", "(n as XText).Value, text0text1");
        }

        //[Variation(Priority = 1, Desc = "Working on the text nodes 2.")]
        public void WorkOnTextNodes2()
        {
            runWithEvents = (bool)Params[0];
            var elem = new XElement("A", new XElement("B"), "text2", new XElement("C"));
            XNode n = elem.FirstNode.NextNode;

            if (runWithEvents)
            {
                eHelper = new EventsHelper(elem);
            }
            n.AddBeforeSelf("text0", "text1");
            if (runWithEvents)
            {
                eHelper.Verify(XObjectChange.Add);
            }

            TestLog.Compare(elem.Nodes().Count(), 4, "elem.Nodes().Count(), 4");
            TestLog.Compare((n as XText).Value, "text2", "(n as XText).Value, text2");
            TestLog.Compare((n.PreviousNode as XText).Value, "text0text1", "(n as XText).Value, text0text1");
        }
        #endregion

        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+AddNodeBefore
        // Test Case
    }
}
