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
    public abstract class XNodeRemove : XLinqTestCase
    {
        #region Fields

        private EventsHelper _eHelper;

        private bool _runWithEvents;

        #endregion

        #region Public Methods and Operators

        public void NodeWithNoParent()
        {
            _runWithEvents = (bool)Params[0];
            XNode[] nodes = { new XElement("a"), new XElement("b", new XAttribute("id", "a0")), new XElement("c", new XAttribute("id", "a0"), new XElement("cc")), new XComment("comm"), new XProcessingInstruction("PI", ""), new XText(""), new XText("  "), new XText("normal"), new XCData("cdata"), new XDocument(), new XDocument(new XDeclaration("1.0", "UTF8", "true"), new XElement("c", new XAttribute("id", "a0"), new XElement("cc"))) };

            foreach (XNode n in nodes)
            {
                try
                {
                    if (_runWithEvents)
                    {
                        _eHelper = new EventsHelper(n);
                    }
                    n.Remove();
                    if (_runWithEvents)
                    {
                        _eHelper.Verify(XObjectChange.Remove, n);
                    }
                    TestLog.Compare(false, "Exception expected [" + n.NodeType + "] " + n);
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        public void OnDocument()
        {
            _runWithEvents = (bool)Params[0];
            var itemCount = (int)Variation.Params[0];
            var addDecl = (bool)Variation.Params[1];

            object[] data = { new XDocumentType("root", null, null, null), new XElement("A"), new XElement("B", new XElement("x"), "string", new XAttribute("at", "home")), new XProcessingInstruction("PI1", ""), new XProcessingInstruction("PI2", ""), new XText(" "), new XText(" "), new XText(" "), new XComment("comment1"), new XComment("comment2") };

            foreach (var nodes in data.NonRecursiveVariations(itemCount))
            {
                if (nodes.Count(x => x is XElement) > 1 || nodes.CheckDTDAfterElement())
                {
                    continue; // double root elem check and dtd after elem check
                }

                int length = (new XDocument(nodes)).Nodes().Count();
                for (int i = 0; i < length; i++)
                {
                    XDocument doc = addDecl ? new XDocument(new XDeclaration("1.0", "UTF8", "true"), nodes) : new XDocument(nodes);
                    XNode o = doc.Nodes().ElementAt(i);

                    if (_runWithEvents)
                    {
                        _eHelper = new EventsHelper(doc);
                    }

                    DoRemoveTest(doc, i);

                    if (_runWithEvents)
                    {
                        _eHelper.Verify(XObjectChange.Remove, o);
                    }
                }
            }
        }

        public void OnElement()
        {
            _runWithEvents = (bool)Params[0];
            var useParentElement = (bool)Variation.Params[0];
            var useDocument = (bool)Variation.Params[1];
            var itemCount = (int)Variation.Params[2];

            object[] data = { new XElement("A"), new XElement("B", new XElement("X")), new XProcessingInstruction("PI1", ""), new XProcessingInstruction("PI2", ""), new XAttribute("id", "a0"), new XText("text"), new XText(""), new XText("text2"), new XCData("cdata1"), new XCData("cdata2"), null, "string1", "string2", new XComment("comment1"), new XComment("comment2") };

            foreach (var nodes in data.NonRecursiveVariations(itemCount))
            {
                int length = (new XElement("dummy", nodes)).Nodes().Count();
                for (int i = 0; i < length; i++)
                {
                    var elem = new XElement("X", nodes);
                    XElement parent = null;
                    if (useParentElement)
                    {
                        parent = new XElement("Parent", new XAttribute("id", "x07"), "text", elem, "text2");
                    }
                    if (useDocument)
                    {
                        var doc = new XDocument(useParentElement ? parent : elem);
                    }

                    var elemCopy = new XElement(elem);
                    XNode o = elem.Nodes().ElementAt(i);
                    if (_runWithEvents)
                    {
                        _eHelper = new EventsHelper(elem);
                    }
                    DoRemoveTest(elem, i);
                    if (_runWithEvents)
                    {
                        _eHelper.Verify(XObjectChange.Remove, o);
                    }
                }
            }
        }

        public void RemoveNodesFromMixedContent()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XElement a = XElement.Parse(@"<A>t1<B/>t2</A>");
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(a);
                count = a.Nodes().Skip(1).Count();
            }
            a.Nodes().Skip(1).Remove();
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
            TestLog.Compare(a.Nodes().Count(), 1, "Wrong node count ...");
            TestLog.Compare(a.FirstNode is XText, "text node");
            TestLog.Compare((a.FirstNode as XText).Value, "t1", "text node value");
        }

        public void UsagePattern1()
        {
            _runWithEvents = (bool)Params[0];
            var e = new XElement("root", new XElement("b", new XAttribute("id", "a0")), new XElement("c", new XAttribute("id", "a0"), new XElement("cc")), new XComment("comm"), new XProcessingInstruction("PI", ""), new XText(""), new XElement("a"), new XText("  "), new XText("normal"), new XCData("cdata"));

            // Need to do snapshot -> otherwise will stop on the first node when Removed.
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(e);
            }

            foreach (XNode n in e.Nodes().ToList())
            {
                n.Remove();
                if (_runWithEvents)
                {
                    _eHelper.Verify(XObjectChange.Remove, n);
                }
            }

            TestLog.Compare(e.Nodes().IsEmpty(), "nodes Removed");
        }

        #endregion

        #region Methods

        private void DoRemoveTest(XContainer elem, int position)
        {
            List<ExpectedValue> expectedData = elem.Nodes().Take(position).Concat(elem.Nodes().Skip(position + 1)).Select(n => new ExpectedValue(!(n is XText), n)).ProcessNodes().ToList();

            XNode toRemove = elem.Nodes().ElementAt(position);
            toRemove.Remove();

            TestLog.Compare(toRemove.Parent == null, "Parent of Removed");
            TestLog.Compare(toRemove.Document == null, "Document of Removed");
            TestLog.Compare(toRemove.NextNode == null, "NextNode");
            TestLog.Compare(toRemove.PreviousNode == null, "PreviousNode");
            if (toRemove is XContainer)
            {
                foreach (XNode child in (toRemove as XContainer).Nodes())
                {
                    TestLog.Compare(child.Document == null, "Document of child of Removed");
                    TestLog.Compare(child.Parent == toRemove, "Parent of child of Removed should be set");
                }
            }
            // try Remove Removed node
            try
            {
                toRemove.Remove();
                TestLog.Compare(false, "Exception expected [" + toRemove.NodeType + "] " + toRemove);
            }
            catch (InvalidOperationException)
            {
            }

            TestLog.Compare(expectedData.EqualAll(elem.Nodes(), XNode.EqualityComparer), "The rest of the tree - Nodes()");
        }
        #endregion
    }
}
