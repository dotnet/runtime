// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;
using XmlCoreTest.Common;

namespace XLinqTests
{
    public class XNodeSequenceRemove : XLinqTestCase
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+XNodeSequenceRemove
        // Test Case

        #region Fields

        private EventsHelper _eHelper;
        private bool _runWithEvents;

        #endregion

        #region Public Methods and Operators

        public override void AddChildren()
        {
                AddChild(new TestVariation(ElementsFromMixedContent) { Attribute = new VariationAttribute("All elements from mixed content") { Priority = 0 } });
                AddChild(new TestVariation(AllFromDocument) { Attribute = new VariationAttribute("All content from the XDocument (doc level)") { Priority = 0 } });
                AddChild(new TestVariation(AllNodes) { Attribute = new VariationAttribute("All nodes from the XDocument") { Priority = 0 } });
                AddChild(new TestVariation(TwoDocuments) { Attribute = new VariationAttribute("Nodes from two documents") { Priority = 0 } });
                AddChild(new TestVariation(DuplicateNodes) { Attribute = new VariationAttribute("Duplicate nodes in sequence") { Priority = 0 } });
                AddChild(new TestVariation(IdAttrsNulls) { Attribute = new VariationAttribute("Nodes from multiple elements + nulls") { Priority = 1 } });
                AddChild(new TestVariation(EmptySequence) { Attribute = new VariationAttribute("Empty sequence") { Priority = 1 } });
                AddChild(new TestVariation(XNodeAncestors) { Attribute = new VariationAttribute("XNode.Ancestors") { Priority = 1 } });
                AddChild(new TestVariation(XNodeAncestorsXName) { Attribute = new VariationAttribute("XNode.Ancestors(XName)") { Priority = 1 } });
                AddChild(new TestVariation(XNodesBeforeSelf) { Attribute = new VariationAttribute("XNode.NodesBeforeSelf") { Priority = 1 } });
                AddChild(new TestVariation(XNodesAfterSelf) { Attribute = new VariationAttribute("XNode.NodesAfterSelf") { Priority = 1 } });
                AddChild(new TestVariation(XElementsBeforeSelf) { Attribute = new VariationAttribute("XNode.ElementsBeforeSelf") { Priority = 1 } });
                AddChild(new TestVariation(XElementsAfterSelf) { Attribute = new VariationAttribute("XNode.ElementsAfterSelf") { Priority = 1 } });
                AddChild(new TestVariation(XElementsBeforeSelfXName) { Attribute = new VariationAttribute("XNode.ElementsBeforeSelf(XName)") { Priority = 1 } });
                AddChild(new TestVariation(XElementsAfterSelfXName) { Attribute = new VariationAttribute("XNode.ElementsAfterSelf(XName)") { Priority = 1 } });
                AddChild(new TestVariation(Document_Nodes) { Attribute = new VariationAttribute("XDocument.Nodes") { Priority = 2 } });
                AddChild(new TestVariation(Document_DescendantNodes) { Attribute = new VariationAttribute("XDocument.DescendantNodes") { Priority = 2 } });
                AddChild(new TestVariation(Document_Descendants) { Attribute = new VariationAttribute("XDocument.Descendants") { Priority = 2 } });
                AddChild(new TestVariation(Document_Elements) { Attribute = new VariationAttribute("XDocument.Elements") { Priority = 2 } });
                AddChild(new TestVariation(Document_DescendantsXName) { Attribute = new VariationAttribute("XDocument.Descendants(XName)") { Priority = 2 } });
                AddChild(new TestVariation(Document_ElementsXName) { Attribute = new VariationAttribute("XDocument.Elements(XName)") { Priority = 2 } });
                AddChild(new TestVariation(Element_Nodes) { Attribute = new VariationAttribute("XElement.Nodes") { Priority = 2 } });
                AddChild(new TestVariation(Element_DescendantNodes) { Attribute = new VariationAttribute("XElement.DescendantNodes") { Priority = 2 } });
                AddChild(new TestVariation(Element_Descendants) { Attribute = new VariationAttribute("XElement.Descendants") { Priority = 2 } });
                AddChild(new TestVariation(Element_Elements) { Attribute = new VariationAttribute("XElement.Elements") { Priority = 2 } });
                AddChild(new TestVariation(Element_DescendantsXName) { Attribute = new VariationAttribute("XElement.Descendants(XName)") { Priority = 2 } });
                AddChild(new TestVariation(Element_ElementsXName) { Attribute = new VariationAttribute("XElement.Elements(XName)") { Priority = 2 } });
                AddChild(new TestVariation(Element_AncestorsAndSelf) { Attribute = new VariationAttribute("XElement.AncestorsAndSelf") { Priority = 2 } });
                AddChild(new TestVariation(Element_DescendantNodesAndSelf) { Attribute = new VariationAttribute("XElement.DescendantNodesAndSelf") { Priority = 2 } });
                AddChild(new TestVariation(Element_DescendantsAndSelf) { Attribute = new VariationAttribute("XElement.DescendantsAndSelf") { Priority = 2 } });
                AddChild(new TestVariation(Element_DescendantsAndSelfXName) { Attribute = new VariationAttribute("XElement.DescendantsAndSelf(XName) II.") { Param = false, Priority = 2 } });
                AddChild(new TestVariation(Element_DescendantsAndSelfXName) { Attribute = new VariationAttribute("XElement.DescendantsAndSelf(XName) I.") { Param = true, Priority = 2 } });
                AddChild(new TestVariation(Element_AncestorsAndSelfXName) { Attribute = new VariationAttribute("XElement.AncestorsAndSelf(XName) I.") { Param = true, Priority = 2 } });
                AddChild(new TestVariation(Element_AncestorsAndSelfXName) { Attribute = new VariationAttribute("XElement.AncestorsAndSelf(XName) II.") { Param = false, Priority = 2 } });
        }

        //[Variation(Priority = 0, Desc = "All elements from mixed content")]

        //[Variation(Priority = 0, Desc = "All content from the XDocument (doc level)")]

        public void AllFromDocument()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse("\t<?PI?><A xmlns='a'/>\r\n <!--comment-->", LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> toRemove = doc.Nodes();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        //[Variation(Priority = 0, Desc = "All nodes from the XDocument")]
        public void AllNodes()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse("\t<?PI?><A xmlns='a'/>\r\n <!--comment-->", LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> toRemove = doc.DescendantNodes();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        //[Variation(Priority = 0, Desc = "Nodes from two documents")]

        //[Variation(Priority = 2, Desc = "XDocument.DescendantNodes")]
        public void Document_DescendantNodes()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> toRemove = doc.DescendantNodes();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = doc.Nodes().Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        //[Variation(Priority = 2, Desc = "XDocument.Descendants")]
        public void Document_Descendants()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine(@"TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> toRemove = doc.Descendants().OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = doc.Elements().Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        //[Variation(Priority = 2, Desc = "XDocument.Elements")]

        //[Variation(Priority = 2, Desc = "XDocument.Descendants(XName)")]
        public void Document_DescendantsXName()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> toRemove = doc.Descendants(@"{http://www.books.com/}book").OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void Document_Elements()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> toRemove = doc.Elements().OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        //[Variation(Priority = 2, Desc = "XDocument.Elements(XName)")]
        public void Document_ElementsXName()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> toRemove = doc.Elements("bookstore").OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void Document_Nodes()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> toRemove = doc.Nodes();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void DuplicateNodes()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse("<A xmlns='a'><!--comment-->text1<X/></A>", LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> toRemove = doc.Root.Nodes().Take(2).Concat2(doc.Root.Elements());
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        //[Variation(Priority = 2, Desc = "XElement.Nodes")]

        // XElement:
        // IEnumerable<XElement> AncestorsAndSelf() 
        // IEnumerable<XNode> SelfAndDescendantNodes() 
        // IEnumerable<XElement> DescendantsAndSelf() 
        // IEnumerable<XElement> DescendantsAndSelf(XName name) 
        // IEnumerable<XElement> AncestorsAndSelf(XName name) 

        //[Variation(Priority = 2, Desc = "XElement.AncestorsAndSelf")]
        public void Element_AncestorsAndSelf()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants(@"{http://www.books.com/}book").Where(x => x.Element("title").Value == "XQL The Golden Years").First();
            TestLog.Compare(e != null, "TEST_FAILED: wrong starting position");
            IEnumerable<XNode> toRemove = e.AncestorsAndSelf().OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void Element_AncestorsAndSelfXName()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            var useSelf = (bool)Variation.Param;
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants(@"{http://www.books.com/}book").Where(x => x.Element("title").Value == "XQL The Golden Years").First();
            TestLog.Compare(e != null, "TEST_FAILED: wrong starting position");
            IEnumerable<XNode> toRemove = e.AncestorsAndSelf(useSelf ? @"{http://www.books.com/}book" : @"bookstore").OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void Element_DescendantNodes()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants(@"{http://www.books.com/}book").Where(x => x.Element("title").Value == "XQL The Golden Years").First();
            TestLog.Compare(e != null, "TEST_FAILING: wrong starting position");
            IEnumerable<XNode> toRemove = e.DescendantNodes();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = e.Nodes().Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        //[Variation(Priority = 2, Desc = "XElement.DescendantNodesAndSelf")]
        public void Element_DescendantNodesAndSelf()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants(@"{http://www.books.com/}book").Where(x => x.Element("title").Value == "XQL The Golden Years").First();
            TestLog.Compare(e != null, "TEST_FAILING: wrong starting position");
            IEnumerable<XNode> toRemove = e.DescendantNodesAndSelf();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = 1;
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void Element_Descendants()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants(@"{http://www.books.com/}book").Where(x => x.Element("title").Value == "XQL The Golden Years").First();
            TestLog.Compare(e != null, "TEST_FAILING: wrong starting position");
            IEnumerable<XNode> toRemove = e.Descendants().OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = e.Elements().Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        //[Variation(Priority = 2, Desc = "XElement.DescendantsAndSelf")]
        public void Element_DescendantsAndSelf()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants(@"{http://www.books.com/}book").Where(x => x.Element("title").Value == "XQL The Golden Years").First();
            TestLog.Compare(e != null, "TEST_FAILING: wrong starting position");
            IEnumerable<XNode> toRemove = e.DescendantsAndSelf().OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = 1;
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        //[Variation(Priority = 2, Desc = "XElement.DescendantsAndSelf(XName) I.", Param = true)]
        //[Variation(Priority = 2, Desc = "XElement.DescendantsAndSelf(XName) II.", Param = false)]
        public void Element_DescendantsAndSelfXName()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            var useSelf = (bool)Variation.Param;
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = useSelf ? doc.Descendants(@"{http://www.books.com/}book").Where(x => x.Element("title").Value == "XQL The Golden Years").First() : doc.Root;
            TestLog.Compare(e != null, "TEST_FAILING: wrong starting position");
            IEnumerable<XNode> toRemove = e.DescendantsAndSelf(@"{http://www.books.com/}book").OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void Element_DescendantsXName()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> toRemove = doc.Root.Descendants(@"{http://www.books.com/}book").OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void Element_Elements()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants(@"{http://www.books.com/}book").Where(x => x.Element("title").Value == "XQL The Golden Years").First();
            TestLog.Compare(e != null, "TEST_FAILING: wrong starting position");
            IEnumerable<XNode> toRemove = e.Elements().OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void Element_ElementsXName()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> toRemove = doc.Root.Elements(@"{http://www.books.com/}book").OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void Element_Nodes()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants(@"{http://www.books.com/}book").Where(x => x.Element("title").Value == "XQL The Golden Years").First();
            TestLog.Compare(e != null, "TEST_FAILED: wrong starting position");
            IEnumerable<XNode> toRemove = e.Nodes();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void ElementsFromMixedContent()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse(@"<A xmlns='a'><B/>text1<p:B xmlns:p='nsp'/>text2<!--comment--><C><innerElement/></C></A>", LoadOptions.PreserveWhitespace);
            IEnumerable<XElement> toRemove = doc.Root.Elements();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void EmptySequence()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse(@"<A id='a' xmlns:p1='nsp1'><B id='b' xmlns='nbs' xmlns:p='nsp' p:x='xx'>text</B><C/><p1:D id='x' datrt='dat'/></A>");
            IEnumerable<XNode> noNodes = doc.Descendants().Where(x => x.Name == "NonExisting").OfType<XNode>();

            var ms1 = new MemoryStream();
            doc.Save(new StreamWriter(ms1));

            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = noNodes.IsEmpty() ? 0 : noNodes.Count();
            }
            noNodes.Remove();
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }

            var ms2 = new MemoryStream();
            doc.Save(new StreamWriter(ms2));

            TestLog.Compare(ms1.ToArray().SequenceEqual(ms2.ToArray()), "Documents different");
        }

        public void IdAttrsNulls()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse(@"<A id='a' xmlns:p1='nsp1'><B id='b' xmlns='nbs' xmlns:p='nsp' p:x='xx'>text</B><C/><p1:D id='x' datrt='dat'/></A>");
            IEnumerable<XNode> someNodes = doc.Root.Descendants().OfType<XNode>().InsertNulls(1);
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = someNodes.IsEmpty() ? 0 : someNodes.Count() / 2;
            }
            VerifyDeleteNodes(someNodes);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void TwoDocuments()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc1 = XDocument.Parse("<A xmlns='a'><!--comment-->text1<X/></A>", LoadOptions.PreserveWhitespace);
            XDocument doc2 = XDocument.Parse("<A xmlns='b'>text1<X/>text2</A>", LoadOptions.PreserveWhitespace);
            IEnumerable<XNode> toRemove = doc1.Root.DescendantNodes().Where(x => x.NodeType == XmlNodeType.Comment).Concat2(doc2.Root.Elements());
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc1);
                count = doc1.Root.DescendantNodes().Where(x => x.NodeType == XmlNodeType.Comment).Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void XElementsAfterSelf()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants("magazine").Where(x => x.Element("title").Value == "PC Week").First();
            TestLog.Compare(e != null, "TEST_FAILED: wrong starting position");
            IEnumerable<XNode> toRemove = e.ElementsAfterSelf().OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void XElementsAfterSelfXName()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants("magazine").Where(x => x.Element("title").Value == "PC Week").First();
            TestLog.Compare(e != null, "TEST_FAILED: wrong starting position");
            IEnumerable<XNode> toRemove = e.ElementsAfterSelf(@"{http://www.books.com/}book").OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void XElementsBeforeSelf()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants("magazine").Where(x => x.Element("title").Value == "PC Week").First();
            TestLog.Compare(e != null, "TEST_FAILED: wrong starting position");
            IEnumerable<XNode> toRemove = e.ElementsBeforeSelf().OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void XElementsBeforeSelfXName()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants("magazine").Where(x => x.Element("title").Value == "PC Week").First();
            TestLog.Compare(e != null, "TEST_FAILED: wrong starting position");
            IEnumerable<XNode> toRemove = e.ElementsBeforeSelf(@"{http://www.books.com/}book").OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }

            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void XNodeAncestors()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants("last.name").Where(x => x.Value == "Marsh").First();
            TestLog.Compare(e != null, "TEST_FAILED: wrong starting position");
            IEnumerable<XNode> toRemove = e.Ancestors().OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(e.Ancestors());
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        //[Variation(Priority = 1, Desc = "XNode.Ancestors(XName)")]
        public void XNodeAncestorsXName()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants("last.name").Where(x => x.Value == "Marsh").First();
            TestLog.Compare(e != null, "TEST_FAILED: wrong starting position");
            IEnumerable<XNode> toRemove = e.Ancestors("author").OfType<XNode>();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(e.Ancestors("author"));
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void XNodesAfterSelf()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants("magazine").Where(x => x.Element("title").Value == "PC Week").First();
            TestLog.Compare(e != null, "TEST_FAILED: wrong starting position");
            IEnumerable<XNode> toRemove = e.NodesAfterSelf();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void XNodesBeforeSelf()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Load(FilePathUtil.getStream(Path.Combine("TestData", "XLinq", "Books.xml")), LoadOptions.PreserveWhitespace);
            XElement e = doc.Descendants("magazine").Where(x => x.Element("title").Value == "PC Week").First();
            TestLog.Compare(e != null, "TEST_FAILED: wrong starting position");
            IEnumerable<XNode> toRemove = e.NodesBeforeSelf();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = toRemove.IsEmpty() ? 0 : toRemove.Count();
            }
            VerifyDeleteNodes(toRemove);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        #endregion

        //[Variation(Priority = 2, Desc = "XElement.AncestorsAndSelf(XName) I.", Param = true)]
        //[Variation(Priority = 2, Desc = "XElement.AncestorsAndSelf(XName) II.", Param = false)]

        #region Methods

        private void VerifyDeleteNodes<T>(IEnumerable<T> toRemove) where T : XNode
        {
            // copy of the data to delete
            IEnumerable<XNode> toRemoveCopy = toRemove.OfType<XNode>().ToList();

            // Create array of parents
            IEnumerable<XContainer> parents = toRemove.Select(x => (x == null) ? (XContainer)null : (x.Parent != null ? (XContainer)x.Parent : (XContainer)x.Document)).ToList();

            // calculate the expected results for the parents of the processed elements
            var expectedNodesForParent = new Dictionary<XContainer, List<ExpectedValue>>();
            foreach (XContainer p in parents)
            {
                if (p != null)
                {
                    expectedNodesForParent.TryAdd(p, p.Nodes().Except(toRemoveCopy.Where(x => x != null)).Select(a => new ExpectedValue(!(a is XText), a)).ProcessNodes().ToList());
                }
            }

            toRemove.Remove();

            IEnumerator<XNode> copyToRemove = toRemoveCopy.GetEnumerator();
            IEnumerator<XContainer> parentsEnum = parents.GetEnumerator();

            // verify on parents: deleted elements should not be found
            while (copyToRemove.MoveNext() && parentsEnum.MoveNext())
            {
                XNode node = copyToRemove.Current;
                if (node != null)
                {
                    XContainer parent = parentsEnum.Current;

                    TestLog.Compare(node.Parent, null, "Parent of deleted");
                    TestLog.Compare(node.Document, null, "Document of deleted");
                    TestLog.Compare(node.NextNode, null, "NextNode of deleted");
                    TestLog.Compare(node.PreviousNode, null, "PreviousNode of deleted");

                    if (parent != null)
                    {
                        TestLog.Compare(parent.Nodes().Where(x => x == node).IsEmpty(), "Nodes axis");
                        if (node is XElement)
                        {
                            var e = node as XElement;
                            e.Verify();
                            TestLog.Compare(parent.Element(e.Name) != node, "Element axis");
                            TestLog.Compare(parent.Elements(e.Name).Where(x => x == e).IsEmpty(), "Elements axis");
                        }

                        // Compare the rest of the elements
                        TestLog.Compare(expectedNodesForParent[parent].EqualAll(parent.Nodes(), XNode.EqualityComparer), "The rest of the nodes");
                    }
                }
            }
        }
        #endregion
    }
}
