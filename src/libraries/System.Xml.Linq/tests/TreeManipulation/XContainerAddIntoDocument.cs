// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace XLinqTests
{
    public class XContainerAddIntoDocument : XLinqTestCase
    {
        private EventsHelper _eHelper;
        private bool _runWithEvents;

        public override void AddChildren()
        {
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument whitespace only") { Params = new object[] { " ", 3, false, false, " " }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("XDocument with Root Elem") { Params = new object[] { "<A/>", 1, true, false }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument") { Params = new object[] { " <?PI?><!--comm-->", 4, false, false }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument with Root Elem") { Params = new object[] { "<A/>", 3, true, false }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument with DTD and Root Elem") { Params = new object[] { "<!DOCTYPE copyright [<!ELEMENT A (#PCDATA)>]><A>aaaa</A>", 4, true, false }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("XDocument - connected") { Params = new object[] { " <?PI?><!--comm-->", 1, false, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("XDocument whitespace only - connected") { Params = new object[] { " ", 1, false, true, " " }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("XDocument with Root Elem - connected") { Params = new object[] { "<A/>", 1, true, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("XDocument with DTD and Root Elem - connected") { Params = new object[] { "<!DOCTYPE copyright [<!ELEMENT A (#PCDATA)>]><A>aaaa</A>", 3, true, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument - connected") { Params = new object[] { " <?PI?><!--comm-->", 4, false, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument whitespace only - connected") { Params = new object[] { " ", 3, false, true, " " }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument with Root Elem - connected") { Params = new object[] { "<A/>", 3, true, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("XDocument whitespace only") { Params = new object[] { " ", 1, false, false, " " }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("XDocument with DTD and Root Elem") { Params = new object[] { "<!DOCTYPE copyright [<!ELEMENT A (#PCDATA)>]><A>aaaa</A>", 3, true, false }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument with DTD and Root Elem - connected") { Params = new object[] { "<!DOCTYPE copyright [<!ELEMENT A (#PCDATA)>]><A>aaaa</A>", 4, true, true }, Priority = 1 } });
            AddChild(new TestVariation(AddIntoDocument) { Attribute = new VariationAttribute("XDocument") { Params = new object[] { " <?PI?><!--comm-->", 1, false, false }, Priority = 1 } });
        }

        /// <summary>
        /// XDocument: 
        ///  - with content, empty
        ///  - adding valid nodes
        ///  - XDecl + XDocType (correct/incorrect order)
        /// </summary>
        public void AddIntoDocument()
        {
            _runWithEvents = (bool)Params[0];
            var xml = Variation.Params[0] as string;
            var variationLength = (int)Variation.Params[1];
            var hasRoot = (bool)Variation.Params[2];
            var isConnected = (bool)Variation.Params[3];
            string stringOnlyContent = Variation.Params.Length > 4 ? Variation.Params[4] as string : null;

            var rs = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                IgnoreWhitespace = false,
                DtdProcessing = DtdProcessing.Ignore,
            };

            object[] nodes = { new XDocumentType("Q", null, null, "<!ENTITY e SYSTEM 'e.ent'!>"), new XDocumentType("A", null, null, "<!ENTITY e SYSTEM 'e.ent'!>"), new XElement("B", new XElement("C"), new XAttribute("a", "aa")), new XElement("B1", new XElement("C1"), new XAttribute("a1", "aa1")), new XProcessingInstruction("PI", "data"), new XProcessingInstruction("PI2", ""), new XComment("comment"), new XComment(""), new XText(""), new XText(" "), new XText("data"), // invalid
                               "text", // invalid
                               new XCData(""), // invalid
                               "\n", "\t", " ", "", null };

            if (isConnected)
            {
                foreach (XNode n in nodes.OfType<XNode>())
                {
                    if (n is XDocumentType)
                    {
                        new XDocument(n);
                        continue;
                    }
                    new XDocument(new XElement("dumma", n));
                }
            }

            foreach (var toInsert in nodes.NonRecursiveVariations(variationLength))
            {
                XDocument doc = null;
                if (hasRoot)
                {
                    doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
                }
                else
                {
                    doc = new XDocument();
                    using (XmlReader r = XmlReader.Create(new StringReader(xml), rs))
                    {
                        while (r.Read())
                        {
                            doc.Add(XNode.ReadFrom(r));
                        }
                    }
                }

                string stringOnlyContentCopy = stringOnlyContent == null ? null : new string(stringOnlyContent.ToCharArray());
                List<ExpectedValue> expectedNodes = CalculateExpectedContent(doc, toInsert, stringOnlyContentCopy).ProcessNodes().ToList();

                bool shouldFail = expectedNodes.IsXDocValid();

                try
                {
                    if (_runWithEvents)
                    {
                        _eHelper = new EventsHelper(doc);
                    }
                    doc.Add(toInsert);
                    TestLog.Compare(!shouldFail, "exception was expected here");
                    TestLog.Compare(expectedNodes.EqualAll(doc.Nodes(), XNode.EqualityComparer), "AddFirst");
                }
                catch (ArgumentException)
                {
                    TestLog.Compare(shouldFail, "exception was NOT expected here");
                }
                catch (InvalidOperationException)
                {
                    TestLog.Compare(shouldFail, "exception was NOT expected here");
                }
                finally
                {
                    doc.RemoveNodes();
                }
            }
        }

        private IEnumerable<ExpectedValue> CalculateExpectedContent(XContainer orig, IEnumerable<object> newNodes, string stringOnlyContent)
        {
            if (stringOnlyContent == null)
            {
                foreach (object o in orig.Nodes())
                {
                    yield return new ExpectedValue(true, o);
                }
            }
            else
            {
                yield return new ExpectedValue(false, new XText(stringOnlyContent));
            }

            foreach (object n in newNodes.Flatten())
            {
                if (n is XAttribute)
                {
                    continue;
                }
                yield return new ExpectedValue((n is XNode) && (n as XNode).Parent == null && (n as XNode).Document == null, n);
            }
        }
    }
}
