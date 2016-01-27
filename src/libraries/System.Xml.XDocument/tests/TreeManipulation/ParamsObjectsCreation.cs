// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;

namespace XLinqTests
{

    public class ParamsObjectsCreation : XLinqTestCase
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+ParamsObjectsCreation
        // Test Case

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(XDocumentAddParams) { Attribute = new VariationAttribute("XDocument - adding Comment") { Param = "XComment", Priority = 1 } });
            AddChild(new TestVariation(XDocumentAddParams) { Attribute = new VariationAttribute("XDocument - combination off allowed types in correct order, without decl") { Param = "Mix2", Priority = 1 } });
            AddChild(new TestVariation(XDocumentAddParams) { Attribute = new VariationAttribute("XDocument - adding PI") { Param = "XPI", Priority = 1 } });
            AddChild(new TestVariation(XDocumentAddParams) { Attribute = new VariationAttribute("XDocument - adding element") { Param = "XElement", Priority = 0 } });
            AddChild(new TestVariation(XDocumentAddParams) { Attribute = new VariationAttribute("XDocument - adding string/whitespace") { Param = "Whitespace", Priority = 2 } });
            AddChild(new TestVariation(XDocumentAddParams) { Attribute = new VariationAttribute("XDocument - combination off allowed types in correct order") { Param = "Mix1", Priority = 1 } });
            AddChild(new TestVariation(XDocumentAddParamsCloning) { Attribute = new VariationAttribute("XDocument - combination off allowed types in correct order,  cloned") { Param = "Mix1", Priority = 1 } });
            AddChild(new TestVariation(XDocumentAddParamsCloning) { Attribute = new VariationAttribute("XDocument - adding string/whitespace") { Param = "Whitespace", Priority = 2 } });
            AddChild(new TestVariation(XDocumentAddParamsCloning) { Attribute = new VariationAttribute("XDocument - adding Comment cloned") { Param = "XComment", Priority = 1 } });
            AddChild(new TestVariation(XDocumentAddParamsCloning) { Attribute = new VariationAttribute("XDocument - adding element cloned") { Param = "XElement", Priority = 0 } });
            AddChild(new TestVariation(XDocumentAddParamsCloning) { Attribute = new VariationAttribute("XDocument - combination off allowed types in correct order, without decl;  cloned") { Param = "Mix2", Priority = 1 } });
            AddChild(new TestVariation(XDocumentAddParamsCloning) { Attribute = new VariationAttribute("XDocument - adding PI cloned") { Param = "XPI", Priority = 1 } });
            AddChild(new TestVariation(CreateXDocumentMixNoArrayNotConnected) { Attribute = new VariationAttribute("XDocument - params no array") { Priority = 0 } });
            AddChild(new TestVariation(CreateXDocumentMixNoArrayConnected) { Attribute = new VariationAttribute("XDocument - params no array - connected") { Priority = 0 } });
            AddChild(new TestVariation(XDocumentAddParamsInvalid) { Attribute = new VariationAttribute("XDocument - Invalid case - XDocument node") { Param = "Document", Priority = 2 } });
            AddChild(new TestVariation(XDocumentAddParamsInvalid) { Attribute = new VariationAttribute("XDocument - Invalid case - Mix with attribute") { Param = "MixAttr", Priority = 2 } });
            AddChild(new TestVariation(XDocumentAddParamsInvalid) { Attribute = new VariationAttribute("XDocument - Invalid case - attribute node") { Param = "Attribute", Priority = 1 } });
            AddChild(new TestVariation(XDocumentInvalidCloneSanity) { Attribute = new VariationAttribute("XDocument - Invalid case with clone - double root - sanity") { Priority = 1 } });
            AddChild(new TestVariation(XDocumentTheSameReferenceSanity) { Attribute = new VariationAttribute("XDocument - the same node instance, connected - sanity") { Param = true, Priority = 1 } });
            AddChild(new TestVariation(XDocumentTheSameReferenceSanity) { Attribute = new VariationAttribute("XDocument - the same node instance - sanity") { Param = false, Priority = 1 } });
            AddChild(new TestVariation(XDocumentIEnumerable) { Attribute = new VariationAttribute("XDocument - IEnumerable - connected") { Param = true, Priority = 0 } });
            AddChild(new TestVariation(XDocumentIEnumerable) { Attribute = new VariationAttribute("XDocument - IEnumerable - not connected") { Param = false, Priority = 0 } });
            AddChild(new TestVariation(XDocument2xIEnumerable) { Attribute = new VariationAttribute("XDocument - 2 x IEnumerable - sanity") { Priority = 0 } });
        }

        // XDocument
        // - from node without parent, from nodes with parent
        // - allowed types
        // - attributes
        // - Document
        // - doubled decl, root elem, doctype
        // - the same valid object instance multiple times 
        // - array/IEnumerable of allowed types
        // - array/IEnumerable including not allowed types

        //[Variation(Priority = 0, Desc = "XDocument - adding element", Param = "XElement")]
        //[Variation(Priority = 1, Desc = "XDocument - adding PI", Param = "XPI")]
        //[Variation(Priority = 1, Desc = "XDocument - adding XmlDecl", Param = "XmlDecl")]
        //[Variation (Priority=1, Desc="XDocument - adding dot type")]
        //[Variation(Priority = 1, Desc = "XDocument - adding Comment", Param = "XComment")]
        //[Variation(Priority = 1, Desc = "XDocument - combination off allowed types in correct order", Param = "Mix1")]
        //[Variation(Priority = 1, Desc = "XDocument - combination off allowed types in correct order, without decl", Param = "Mix2")]
        //[Variation(Priority = 2, Desc = "XDocument - adding string/whitespace", Param = "Whitespace")]

        public void CreateXDocumentMixNoArrayConnected()
        {
            var doc1 = new XDocument(new XProcessingInstruction("PI", "data"), new XComment("comm1"), new XElement("root", new XAttribute("id", "a1")), new XComment("comm2"));

            var nodes = new XNode[4];
            XNode nn = doc1.FirstNode;
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = nn;
                nn = nn.NextNode;
            }

            var doc = new XDocument(nodes[0], nodes[1], nodes[2], nodes[3]);
            int nodeCounter = 0;

            for (XNode n = doc.FirstNode; n != null; n = n.NextNode)
            {
                TestLog.Compare(n != nodes[nodeCounter], "identity");
                TestLog.Compare(XNode.DeepEquals(n, nodes[nodeCounter]), "Equals");
                TestLog.Compare(nodes[nodeCounter].Document == doc1, "orig Document");
                TestLog.Compare(n.Document == doc, "new Document");
                nodeCounter++;
            }
            TestLog.Compare(nodeCounter, nodes.Length, "All nodes added");
        }

        public void CreateXDocumentMixNoArrayNotConnected()
        {
            XNode[] nodes = { new XProcessingInstruction("PI", "data"), new XComment("comm1"), new XElement("root", new XAttribute("id", "a1")), new XComment("comm2") };

            var doc = new XDocument(nodes[0], nodes[1], nodes[2], nodes[3]);
            int nodeCounter = 0;

            for (XNode n = doc.FirstNode; n != null; n = n.NextNode)
            {
                TestLog.Compare(n == nodes[nodeCounter], "identity");
                TestLog.Compare(XNode.DeepEquals(n, nodes[nodeCounter]), "Equals");
                TestLog.Compare(n.Document == doc, "Document");
                nodeCounter++;
            }
            TestLog.Compare(nodeCounter, nodes.Length, "All nodes added");
        }

        public void XDocument2xIEnumerable()
        {
            XDocument doc1 = null;
            var paras = new object[2];

            doc1 = new XDocument(new XProcessingInstruction("PI", "data"), new XElement("root"));
            var list1 = new List<XNode>();
            for (XNode n = doc1.FirstNode; n != null; n = n.NextNode)
            {
                list1.Add(n);
            }
            paras[0] = list1;

            var list2 = new List<XNode>();
            list2.Add(new XProcessingInstruction("PI2", "data"));
            list2.Add(new XComment("como"));
            paras[1] = list2;

            var doc = new XDocument(paras);
            IEnumerator ien = (paras[0] as IEnumerable).GetEnumerator();
            bool enumerablesSwitched = false;
            for (XNode n = doc.FirstNode; n != null; n = n.NextNode)
            {
                if (!ien.MoveNext())
                {
                    ien = (paras[1] as IEnumerable).GetEnumerator();
                    ien.MoveNext();
                    enumerablesSwitched = true;
                }
                var orig = ien.Current as XNode;
                TestLog.Compare(XNode.DeepEquals(n, orig), "n.Equals(orig)");
                if (!enumerablesSwitched)
                {
                    TestLog.Compare(orig != n, "orig != n");
                    TestLog.Compare(orig.Document == doc1, "orig Document connected");
                    TestLog.Compare(n.Document == doc, "node Document connected");
                }
                else
                {
                    TestLog.Compare(orig == n, "orig == n");
                    TestLog.Compare(orig.Document == doc, "orig Document NOT connected");
                    TestLog.Compare(n.Document == doc, "node Document NOT connected");
                }
            }
        }

        public void XDocumentAddParams()
        {
            object[] parameters = null;
            var paramType = Variation.Param as string;

            switch (paramType)
            {
                case "XElement":
                    parameters = new object[] { new XElement("root") };
                    break;
                case "XPI":
                    parameters = new object[] { new XProcessingInstruction("Click", "data") };
                    break;
                case "XComment":
                    parameters = new object[] { new XComment("comment") };
                    break;
                case "Whitespace":
                    parameters = new object[] { new XText(" ") };
                    break;
                case "Mix1":
                    parameters = new object[] { new XComment("comment"), new XProcessingInstruction("Click", "data"), new XElement("root"), new XProcessingInstruction("Click2", "data2") };
                    break;
                case "Mix2":
                    parameters = new object[] { new XComment("comment"), new XProcessingInstruction("Click", "data"), new XElement("root"), new XProcessingInstruction("Click2", "data2") };
                    break;
                default:
                    TestLog.Compare(false, "Test case: Wrong param");
                    break;
            }

            var doc = new XDocument(parameters);
            TestLog.Compare(doc != null, "doc!=null");
            TestLog.Compare(doc.Document == doc, "doc.Document property");
            TestLog.Compare(doc.Parent == null, "doc.Parent property");
            int counter = 0;
            for (XNode node = doc.FirstNode; node.NextNode != null; node = node.NextNode)
            {
                TestLog.Compare(node != null, "node != null");
                TestLog.Compare(node == parameters[counter], "Node identity");
                TestLog.Compare(XNode.DeepEquals(node, parameters[counter] as XNode), "node equals param");
                TestLog.Compare(node.Document, doc, "Document property");
                counter++;
            }
        }

        //[Variation(Priority = 0, Desc = "XDocument - adding element cloned", Param = "XElement")]
        //[Variation(Priority = 1, Desc = "XDocument - adding PI cloned", Param = "XPI")]
        //[Variation(Priority = 1, Desc = "XDocument - adding XmlDecl cloned", Param = "XmlDecl")]
        //[Variation (Priority=1, Desc="XDocument - adding dot type")]
        //[Variation(Priority = 1, Desc = "XDocument - adding Comment cloned", Param = "XComment")]
        //[Variation(Priority = 1, Desc = "XDocument - combination off allowed types in correct order,  cloned", Param = "Mix1")]
        //[Variation(Priority = 1, Desc = "XDocument - combination off allowed types in correct order, without decl;  cloned", Param = "Mix2")]
        //[Variation(Priority = 2, Desc = "XDocument - adding string/whitespace", Param = "Whitespace")]
        public void XDocumentAddParamsCloning()
        {
            object[] paras = null;
            var paramType = Variation.Param as string;
            var doc1 = new XDocument();

            switch (paramType)
            {
                case "XElement":
                    var xe = new XElement("root");
                    doc1.Add(xe);
                    paras = new object[] { xe };
                    break;
                case "XPI":
                    var pi = new XProcessingInstruction("Click", "data");
                    doc1.Add(pi);
                    paras = new object[] { pi };
                    break;
                case "XComment":
                    var comm = new XComment("comment");
                    doc1.Add(comm);
                    paras = new object[] { comm };
                    break;
                case "Whitespace":
                    var txt = new XText(" ");
                    doc1.Add(txt);
                    paras = new object[] { txt };
                    break;
                case "Mix1":
                    {
                        var a2 = new XComment("comment");
                        doc1.Add(a2);
                        var a3 = new XProcessingInstruction("Click", "data");
                        doc1.Add(a3);
                        var a4 = new XElement("root");
                        doc1.Add(a4);
                        var a5 = new XProcessingInstruction("Click2", "data2");
                        doc1.Add(a5);
                        paras = new object[] { a2, a3, a4, a5 };
                    }
                    break;
                case "Mix2":
                    {
                        var a2 = new XComment("comment");
                        doc1.Add(a2);
                        var a3 = new XProcessingInstruction("Click", "data");
                        doc1.Add(a3);
                        var a4 = new XElement("root");
                        doc1.Add(a4);
                        var a5 = new XProcessingInstruction("Click2", "data2");
                        doc1.Add(a5);
                        paras = new object[] { a2, a3, a4, a5 };
                    }
                    break;
                default:
                    TestLog.Compare(false, "Test case: Wrong param");
                    break;
            }

            var doc = new XDocument(paras);
            TestLog.Compare(doc != null, "doc!=null");
            TestLog.Compare(doc.Document == doc, "doc.Document property");
            int counter = 0;
            for (XNode node = doc.FirstNode; node.NextNode != null; node = node.NextNode)
            {
                TestLog.Compare(node != null, "node != null");
                var orig = paras[counter] as XNode;
                TestLog.Compare(node != orig, "Not the same instance, cloned");
                TestLog.Compare(orig.Document, doc1, "Orig Document");
                TestLog.Compare(XNode.DeepEquals(node, orig), "node equals param");
                TestLog.Compare(node.Document, doc, "Document property");
                counter++;
            }
        }

        //[Variation(Priority = 0, Desc = "XDocument - params no array")]

        //[Variation(Priority = 1, Desc = "XDocument - Invalid case - attribute node", Param = "Attribute")]
        //[Variation(Priority = 2, Desc = "XDocument - Invalid case - XDocument node", Param = "Document")]
        //[Variation(Priority = 2, Desc = "XDocument - Invalid case - Mix with attribute", Param = "MixAttr")]
        //[Variation(Priority = 2, Desc = "XDocument - Invalid case - Double root element", Param = "DoubleRoot")]
        //[Variation(Priority = 2, Desc = "XDocument - Invalid case - Double XmlDecl", Param = "DoubleDecl")]
        //[Variation(Priority = 1, Desc = "XDocument - Invalid case - Invalid order ... Decl is not the first", Param = "InvalIdOrder")]
        public void XDocumentAddParamsInvalid()
        {
            object[] paras = null;
            var paramType = Variation.Param as string;

            switch (paramType)
            {
                case "Attribute":
                    paras = new object[] { new XAttribute("mu", "hu") };
                    break;
                case "Document":
                    paras = new object[] { new XDocument(), new XProcessingInstruction("Click", "data") };
                    break;
                case "MixAttr":
                    paras = new object[] { new XElement("aloha"), new XProcessingInstruction("PI", "data"), new XAttribute("id", "a") };
                    break;
                case "DoubleRoot":
                    paras = new object[] { new XElement("first"), new XElement("Second") };
                    break;
                case "DoubleDocType":
                    paras = new object[] { new XDocumentType("root", "", "", ""), new XDocumentType("root", "", "", "") };
                    break;
                case "InvalidOrder":
                    paras = new object[] { new XElement("first"), new XDocumentType("root", "", "", "") };
                    break;

                default:
                    TestLog.Compare(false, "Test case: Wrong param");
                    break;
            }

            XDocument doc = null;
            try
            {
                doc = new XDocument(paras);
                TestLog.Compare(false, "Should throw exception");
            }
            catch (ArgumentException)
            {
                TestLog.Compare(doc == null, "Should be null if exception appears");
                return;
            }
            throw new TestException(TestResult.Failed, "");
        }

        public void XDocumentIEnumerable()
        {
            var connected = (bool)Variation.Param;
            XDocument doc1 = null;
            object[] paras = null;

            if (connected)
            {
                doc1 = new XDocument(
                    new XProcessingInstruction("PI", "data"), new XElement("root"));
                var list = new List<XNode>();
                for (XNode n = doc1.FirstNode; n != null; n = n.NextNode)
                {
                    list.Add(n);
                }
                paras = new object[] { list };
            }
            else
            {
                var list = new List<XNode>();
                list.Add(new XProcessingInstruction("PI", "data"));
                list.Add(new XElement("root"));
                paras = new object[] { list };
            }

            var doc = new XDocument(paras);
            IEnumerator ien = (paras[0] as IEnumerable).GetEnumerator();
            for (XNode n = doc.FirstNode; n != null; n = n.NextNode)
            {
                ien.MoveNext();
                var orig = ien.Current as XNode;
                TestLog.Compare(XNode.DeepEquals(n, orig), "XNode.DeepEquals(n,orig)");
                if (connected)
                {
                    TestLog.Compare(orig != n, "orig != n");
                    TestLog.Compare(orig.Document == doc1, "orig Document connected");
                    TestLog.Compare(n.Document == doc, "node Document connected");
                }
                else
                {
                    TestLog.Compare(orig == n, "orig == n");
                    TestLog.Compare(orig.Document == doc, "orig Document NOT connected");
                    TestLog.Compare(n.Document == doc, "node Document NOT connected");
                }
            }
            TestLog.Compare(!ien.MoveNext(), "enumerator should be exhausted");
        }

        //[Variation(Priority = 1, Desc = "XDocument - Invalid case with clone - double root - sanity")]
        public void XDocumentInvalidCloneSanity()
        {
            var doc1 = new XDocument(new XElement("root", new XElement("A"), new XElement("B")));

            string origXml = doc1.ToString(SaveOptions.DisableFormatting);

            var paras = new object[] { doc1.Root.Element("A"), doc1.Root.Element("B") };

            XDocument doc = null;
            try
            {
                doc = new XDocument(paras);
                TestLog.Compare(false, "should throw");
            }
            catch (Exception)
            {
                foreach (object o in paras)
                {
                    var e = o as XElement;
                    TestLog.Compare(e.Parent, doc1.Root, "Orig Parent");
                    TestLog.Compare(e.Document, doc1, "Orig Document");
                }
                TestLog.Compare(doc1.ToString(SaveOptions.DisableFormatting), origXml, "Orig XML");
                return;
            }
            throw new TestException(TestResult.Failed, "");
        }

        //[Variation(Priority = 1, Desc = "XDocument - the same node instance, connected - sanity", Param = true)]
        //[Variation(Priority = 1, Desc = "XDocument - the same node instance - sanity", Param = false)]
        public void XDocumentTheSameReferenceSanity()
        {
            object[] paras = null;
            var connected = (bool)Variation.Param;
            XDocument doc1 = null;

            if (connected)
            {
                doc1 = new XDocument(new XElement("root", new XElement("A"), new XProcessingInstruction("PI", "data")));

                paras = new object[] { doc1.Root.LastNode, doc1.Root.LastNode, doc1.Root.Element("A") };
            }
            else
            {
                var e = new XElement("A");
                var pi = new XProcessingInstruction("PI", "data");
                paras = new object[] { pi, pi, e };
            }

            var doc = new XDocument(paras);

            XNode firstPI = doc.FirstNode;
            XNode secondPI = firstPI.NextNode;
            XNode rootElem = secondPI.NextNode;

            TestLog.Compare(firstPI != null, "firstPI != null");
            TestLog.Compare(firstPI.NodeType, XmlNodeType.ProcessingInstruction, "firstPI nodetype");
            TestLog.Compare(firstPI is XProcessingInstruction, "firstPI is XPI");

            TestLog.Compare(secondPI != null, "secondPI != null");
            TestLog.Compare(secondPI.NodeType, XmlNodeType.ProcessingInstruction, "secondPI nodetype");
            TestLog.Compare(secondPI is XProcessingInstruction, "secondPI is XPI");

            TestLog.Compare(rootElem != null, "rootElem != null");
            TestLog.Compare(rootElem.NodeType, XmlNodeType.Element, "rootElem nodetype");
            TestLog.Compare(rootElem is XElement, "rootElem is XElement");
            TestLog.Compare(rootElem.NextNode == null, "rootElem NextNode");

            TestLog.Compare(firstPI != secondPI, "firstPI != secondPI");
            TestLog.Compare(XNode.DeepEquals(firstPI, secondPI), "XNode.DeepEquals(firstPI,secondPI)");

            foreach (object o in paras)
            {
                var e = o as XNode;
                if (connected)
                {
                    TestLog.Compare(e.Parent, doc1.Root, "Orig Parent");
                    TestLog.Compare(e.Document, doc1, "Orig Document");
                }
                else
                {
                    TestLog.Compare(e.Parent == null, "Orig Parent not connected");
                    TestLog.Compare(e.Document, doc, "Orig Document not connected");
                }
            }
        }
        #endregion
    }
}
