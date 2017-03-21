// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class XAttributeEnumRemove : XLinqTestCase
    {
        #region Fields

        private EventsHelper _eHelper;

        private bool _runWithEvents;

        #endregion

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(IdAttrsMultipleDocs) { Attribute = new VariationAttribute("attributes from multiple elements") { Priority = 1 } });
            AddChild(new TestVariation(IdAttrs) { Attribute = new VariationAttribute("attributes from multiple documents") { Priority = 1 } });
            AddChild(new TestVariation(OneElementNonNS) { Attribute = new VariationAttribute("All non-namespace attributes in one element") { Priority = 1 } });
            AddChild(new TestVariation(OneElementNS) { Attribute = new VariationAttribute("All namespace attributes in one element") { Priority = 1 } });
            AddChild(new TestVariation(OneElement) { Attribute = new VariationAttribute("All attributes in one element") { Priority = 0 } });
            AddChild(new TestVariation(OneDocument) { Attribute = new VariationAttribute("All attributes in one document") { Priority = 1 } });
            AddChild(new TestVariation(IdAttrsNulls) { Attribute = new VariationAttribute("All attributes in one document + nulls") { Priority = 1 } });
            AddChild(new TestVariation(DuplicateAttributeInside) { Attribute = new VariationAttribute("Duplicate attribute in sequence") { Priority = 3 } });
            AddChild(new TestVariation(EmptySequence) { Attribute = new VariationAttribute("Empty sequence") { Priority = 1 } });
        }

        // From the same element 
        //   - all attributes
        //   - some attributes
        // From different elements - the same document
        // From different documents
        // Enumerable + nulls

        //[Variation(Priority = 1, Desc = "attributes from multiple elements")]

        //[Variation(Priority = 3, Desc = "Duplicate attribute in sequence")]

        public void DuplicateAttributeInside()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse(@"<A id='a' xmlns:p1='nsp1'><B id='b' xmlns='nbs' xmlns:p='nsp' p:x='xx'>text</B><C/><p1:D id='x' datrt='dat'/></A>");
            IEnumerable<XAttribute> allAttributes = doc.Root.Attributes().Concat(doc.Root.Attributes());
            try
            {
                if (_runWithEvents)
                {
                    _eHelper = new EventsHelper(doc);
                    count = allAttributes.IsEmpty() ? 0 : allAttributes.Count();
                }
                allAttributes.Remove(); // should throw because of snapshot logic
                if (_runWithEvents)
                {
                    _eHelper.Verify(XObjectChange.Remove, count);
                }
                TestLog.Compare(false, "exception expected here");
            }
            catch (InvalidOperationException)
            {
            }
        }

        //[Variation(Priority = 1, Desc = "Empty sequence")]
        public void EmptySequence()
        {
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse(@"<A id='a' xmlns:p1='nsp1'><B id='b' xmlns='nbs' xmlns:p='nsp' p:x='xx'>text</B><C/><p1:D id='x' datrt='dat'/></A>");
            IEnumerable<XAttribute> noAttributes = doc.Descendants().Where(x => !x.HasAttributes).Attributes();
            TestLog.Compare(noAttributes.IsEmpty(), "should be empty sequence");

            var ms1 = new MemoryStream();
            doc.Save(new StreamWriter(ms1));

            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
            }
            noAttributes.Remove();
            if (_runWithEvents)
            {
                _eHelper.Verify(0);
            }

            var ms2 = new MemoryStream();
            doc.Save(new StreamWriter(ms2));

            TestLog.Compare(ms1.ToArray().SequenceEqual(ms2.ToArray()), "Documents different");
        }

        public void IdAttrs()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse(@"<A id='a' xmlns:p1='nsp1'><B id='b' xmlns='nbs' xmlns:p='nsp' p:x='xx'>text</B><C/><p1:D id='x' datrt='dat'/></A>");
            XElement e = XElement.Parse(@"<X id='z'><Z xmlns='a' id='z'/></X>");
            IEnumerable<XAttribute> allAttributes = doc.Descendants().Attributes().Concat(e.Descendants().Attributes()).Where(a => a.Name == "id");
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = doc.Descendants().Attributes().Where(a => a.Name == "id").Count();
            }
            VerifyDeleteAttributes(allAttributes);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void IdAttrsMultipleDocs()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse(@"<A id='a' xmlns:p1='nsp1'><B id='b' xmlns='nbs' xmlns:p='nsp' p:x='xx'>text</B><C/><p1:D id='x' datrt='dat'/></A>");
            IEnumerable<XAttribute> allAttributes = doc.Descendants().Attributes().Where(a => a.Name == "id");
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = allAttributes.IsEmpty() ? 0 : allAttributes.Count();
            }
            VerifyDeleteAttributes(allAttributes);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void IdAttrsNulls()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse(@"<A id='a' xmlns:p1='nsp1'><B id='b' xmlns='nbs' xmlns:p='nsp' p:x='xx'>text</B><C/><p1:D id='x' datrt='dat'/></A>");
            IEnumerable<XAttribute> allAttributes = doc.Descendants().Attributes().Where(a => a.Name == "id").InsertNulls(1);
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                // null attribute will not cause the remove event.
                count = allAttributes.IsEmpty() ? 0 : allAttributes.Count() / 2;
            }
            VerifyDeleteAttributes(allAttributes);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void OneDocument()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse(@"<A id='a' xmlns:p1='nsp1'><B id='b' xmlns='nbs' xmlns:p='nsp' p:x='xx'>text</B><C/><p1:D datrt='dat'/></A>");
            IEnumerable<XAttribute> allAttributes = doc.Descendants().Attributes();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = allAttributes.IsEmpty() ? 0 : allAttributes.Count();
            }
            VerifyDeleteAttributes(allAttributes);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void OneElement()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse(@"<A id='a' xmlns:p1='nsp1'><B id='b' xmlns='nbs' xmlns:p='nsp' p:x='xx'>text</B><C/><p1:D datrt='dat'/></A>");
            IEnumerable<XAttribute> allAttributes = doc.Root.Element("{nbs}B").Attributes();
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = allAttributes.IsEmpty() ? 0 : allAttributes.Count();
            }
            VerifyDeleteAttributes(allAttributes);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        public void OneElementNS()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse(@"<A id='a' xmlns:p1='nsp1'><B id='b' xmlns='nbs' xmlns:p='nsp' p:x='xx'>text</B><C/><p1:D datrt='dat'/></A>");
            IEnumerable<XAttribute> allAttributes = doc.Root.Element("{nbs}B").Attributes().Where(a => a.IsNamespaceDeclaration);
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = allAttributes.IsEmpty() ? 0 : allAttributes.Count();
            }
            VerifyDeleteAttributes(allAttributes);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
            VerifyDeleteAttributes(allAttributes);
        }

        public void OneElementNonNS()
        {
            int count = 0;
            _runWithEvents = (bool)Params[0];
            XDocument doc = XDocument.Parse(@"<A id='a' xmlns:p1='nsp1'><B id='b' xmlns='nbs' xmlns:p='nsp' p:x='xx'>text</B><C/><p1:D datrt='dat'/></A>");
            IEnumerable<XAttribute> allAttributes = doc.Root.Element("{nbs}B").Attributes().Where(a => !a.IsNamespaceDeclaration);
            if (_runWithEvents)
            {
                _eHelper = new EventsHelper(doc);
                count = allAttributes.IsEmpty() ? 0 : allAttributes.Count();
            }
            VerifyDeleteAttributes(allAttributes);
            if (_runWithEvents)
            {
                _eHelper.Verify(XObjectChange.Remove, count);
            }
        }

        #endregion

        #region Methods

        private void VerifyDeleteAttributes(IEnumerable<XAttribute> allAttributes)
        {
            // specify enum + make copy of it
            IEnumerable<XAttribute> copyAllAttributes = allAttributes.ToList();

            // calculate parents + make copy
            IEnumerable<XElement> parents = allAttributes.Select(a => a == null ? null : a.Parent).ToList();

            // calculate the expected results for the parents of the processed elements
            var expectedAttrsForParent = new Dictionary<XElement, List<ExpectedValue>>();
            foreach (XElement p in parents)
            {
                if (p != null)
                {
                    expectedAttrsForParent.TryAdd(p, p.Attributes().Except(copyAllAttributes.Where(x => x != null)).Select(a => new ExpectedValue(true, a)).ToList());
                }
            }

            // enum.Remove ()
            allAttributes.Remove();

            // verify properties of the deleted attrs
            TestLog.Compare(allAttributes.IsEmpty(), "There should be no attributes left");

            IEnumerator<XAttribute> copyAttrib = copyAllAttributes.GetEnumerator();
            IEnumerator<XElement> parentsEnum = parents.GetEnumerator();

            // verify on parents: deleted elements should not be found
            while (copyAttrib.MoveNext() && parentsEnum.MoveNext())
            {
                XAttribute a = copyAttrib.Current;
                if (a != null)
                {
                    XElement parent = parentsEnum.Current;

                    a.Verify();
                    parent.Verify();

                    TestLog.Compare(a.Parent, null, "Parent of deleted");
                    TestLog.Compare(a.NextAttribute, null, "NextAttribute of deleted");
                    TestLog.Compare(a.PreviousAttribute, null, "PreviousAttribute of deleted");

                    if (parent != null)
                    {
                        TestLog.Compare(parent.Attribute(a.Name), null, "Attribute lookup");
                        TestLog.Compare(parent.Attributes().Where(x => x.Name == a.Name).IsEmpty(), "Attributes node");

                        // Compare the rest of the elements
                        TestLog.Compare(expectedAttrsForParent[parent].EqualAllAttributes(parent.Attributes(), Helpers.MyAttributeComparer), "The rest of the attributes");
                    }
                }
            }
        }
        #endregion

        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+XAttributeEnumRemove
        // Test Case
    }
}
