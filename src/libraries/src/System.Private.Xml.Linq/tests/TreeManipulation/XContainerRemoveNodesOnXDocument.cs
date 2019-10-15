// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XLinqTests
{
    public class XContainerRemoveNodesOnXDocument : XLinqTestCase
    {
        private EventsHelper _eHelper;
        private bool _runWithEvents;

        public override void AddChildren()
        {
            AddChild(new TestVariation(OnXDocument1) { Attribute = new VariationAttribute("On XDocument - root element only") { Param = "<A/>", Priority = 1 } });
            AddChild(new TestVariation(OnXDocument1) { Attribute = new VariationAttribute("On XDocument - Empty") { Param = "", Priority = 1 } });
            AddChild(new TestVariation(OnXDocument1) { Attribute = new VariationAttribute("On XDocument - Decl + root element only") { Param = "<?xml version='1.0'?><A/>", Priority = 1 } });
            AddChild(new TestVariation(OnXDocument1) { Attribute = new VariationAttribute("On XDocument - Decl + DTD + nodes") { Param = "<?xml version='1.0'?><!DOCTYPE square [<!ELEMENT square (B*)><!ATTLIST square wIdth CDATA '10'>]><square>text<B/><!--comment--></square>", Priority = 0 } });
            AddChild(new TestVariation(OnXDocument1) { Attribute = new VariationAttribute("On XDocument - Decl + DTD + nodes") { Param = "<?xml version='1.0'?><!DOCTYPE square [<!ELEMENT square (B*)><!ATTLIST square wIdth CDATA '10'>]>\t\n<?PI?><square>text<B/><!--comment--></square>\n\t<!--here is the end ... -->", Priority = 1 } });
        }

        /// <summary>
        /// On XDocument
        ///  ~ Empty
        ///  ~ Not empty
        ///      With XDecl
        ///      With DTD
        ///      Just root elem
        ///      Root elem + PI + whitespace + comment
        /// </summary>
        public void OnXDocument1()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            var xml = Variation.Param as string;
            XDocument e = xml == "" ? new XDocument() : XDocument.Parse(xml);
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

        private void VerifyRemoveNodes(XDocument doc)
        {
            IEnumerable<XNode> nodesBefore = doc.Nodes().ToList();
            XDeclaration decl = doc.Declaration;

            doc.RemoveNodes();

            TestLog.Compare(doc.Nodes().IsEmpty(), "e.Nodes().IsEmpty()");
            TestLog.Compare(nodesBefore.Where(n => n.Parent != null).IsEmpty(), "nodesBefore.Where(n=>n.Parent!=null).IsEmpty()");
            TestLog.Compare(nodesBefore.Where(n => n.Document != null).IsEmpty(), "nodesBefore.Where(n=>n.Parent!=null).IsEmpty()");
            TestLog.Compare(doc.Declaration == decl, "doc.Declaration == decl");
        }
    }
}
