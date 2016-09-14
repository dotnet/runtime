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
    public class AddFirstAddFirstIntoDocument : XLinqTestCase
    {
        private EventsHelper _eHelper;
        private bool _runWithEvents;

        public override void AddChildren()
        {
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("XDocument") { Params = new object[] { " <?PI?><!--comm-->", 1, false, false }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("XDocument with DTD and Root Elem") { Params = new object[] { "<!DOCTYPE copyright [<!ELEMENT A (#PCDATA)>]><A>aaaa</A>", 1, true, false }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument") { Params = new object[] { " <?PI?><!--comm-->", 4, false, false }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument whitespace only") { Params = new object[] { " ", 3, false, false, " " }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument with Root Elem") { Params = new object[] { "<A/>", 3, true, false }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument with DTD and Root Elem") { Params = new object[] { "<!DOCTYPE copyright [<!ELEMENT A (#PCDATA)>]><A>aaaa</A>", 4, true, false }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("XDocument - connected") { Params = new object[] { " <?PI?><!--comm-->", 1, false, true }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("XDocument whitespace only - connected") { Params = new object[] { " ", 1, false, true, " " }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("XDocument with Root Elem - connected") { Params = new object[] { "<A/>", 1, true, true }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("XDocument with DTD and Root Elem - connected") { Params = new object[] { "<!DOCTYPE copyright [<!ELEMENT A (#PCDATA)>]><A>aaaa</A>", 1, true, true }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument - connected") { Params = new object[] { " <?PI?><!--comm-->", 4, false, true }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument whitespace only - connected") { Params = new object[] { " ", 3, false, true, " " }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("XDocument with Root Elem") { Params = new object[] { "<A/>", 1, true, false }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument with DTD and Root Elem - connected") { Params = new object[] { "<!DOCTYPE copyright [<!ELEMENT A (#PCDATA)>]><A>aaaa</A>", 4, true, true }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("(Multiple) XDocument with Root Elem - connected") { Params = new object[] { "<A/>", 3, true, true }, Priority = 1 } });
            AddChild(new TestVariation(AddFirstIntoDocument) { Attribute = new VariationAttribute("XDocument whitespace only") { Params = new object[] { " ", 1, false, false, " " }, Priority = 1 } });
        }

        /// <summary>
        /// XDocument: 
        ///  - with content, empty
        ///  - adding valid nodes
        ///  - adding invalid nodes
        ///  - XDecl + XDocType (correct/incorrect order)
        /// </summary>
        public void AddFirstIntoDocument()
        {
            _runWithEvents = (bool)Params[0];
            var xml = Variation.Params[0] as string;
            var variationLength = (int)Variation.Params[1];
            var hasRoot = (bool)Variation.Params[2]; // for source document creation
            var isConnected = (bool)Variation.Params[3]; // to force cloning
            string stringOnlyContent = Variation.Params.Length > 4 ? Variation.Params[4] as string : null;

            var rs = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                IgnoreWhitespace = false,
                DtdProcessing = DtdProcessing.Ignore,
            };

            object[] nodes = { new XDocumentType("Q", null, null, "<!ENTITY e SYSTEM 'e.ent'!>"), new XDocumentType("Q", null, null, "<!ENTITY e SYSTEM 'e.ent'!>"), new XElement("B", new XElement("C"), new XAttribute("a", "aa")), new XElement("B", new XElement("C"), new XAttribute("a", "aa")), new XProcessingInstruction("PI", "data"), new XProcessingInstruction("PI2", ""), new XComment("comment"), new XComment(""), new XText(""), new XText(" "), new XText("data"), // invalid
                               "data", // invalid
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
                    new XDocument(new XElement("dummy", n));
                }
            }

            foreach (var toInsert in nodes.NonRecursiveVariations(variationLength))
            {
                bool shouldFail = false;

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
                List<ExpectedValue> expectedNodes = CalculateExpectedValuesAddFirst(doc, toInsert, stringOnlyContentCopy).ProcessNodes().ToList();
                shouldFail = expectedNodes.IsXDocValid();

                try
                {
                    if (_runWithEvents)
                    {
                        _eHelper = new EventsHelper(doc);
                    }
                    doc.AddFirst(toInsert);
                    // Difficult to verifty the way this variation is setup
                    //if (runWithEvents) eHelper.Verify(XObjectChange.Add, toInsert);
                    TestLog.Compare(!shouldFail, "exception was expected here");
                    TestLog.Compare(expectedNodes.EqualAll(doc.Nodes(), XNode.EqualityComparer), "AddFirst");
                }
                catch (InvalidOperationException)
                {
                    TestLog.Compare(shouldFail, "exception was NOT expected here");
                }
                catch (ArgumentException)
                {
                    TestLog.Compare(shouldFail, "exception was NOT expected here");
                }
                finally
                {
                    doc.RemoveNodes();
                }
            }
        }

        private IEnumerable<ExpectedValue> CalculateExpectedValuesAddFirst(XContainer orig, IEnumerable<object> newNodes, string stringOnlyContent)
        {
            foreach (object n in newNodes.Flatten())
            {
                yield return new ExpectedValue((n is XNode) && (n as XNode).Parent == null && (n as XNode).Document == null, n);
            }
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
        }
    }
}
