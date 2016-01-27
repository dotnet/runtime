// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace Microsoft.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // TestSpec
    //
    ////////////////////////////////////////////////////////////////
    public class TestSpec
    {
        //Data
        internal CXmlDocument pxmldoc;
        protected TestModule ptestmodule;
        protected Dictionary<object, TestItem> puniqueids;
        protected int pnextuniqueid;

        //Constructor
        public TestSpec(TestModule testmodule)
        {
            ptestmodule = testmodule;
            puniqueids = new Dictionary<object, TestItem>();
        }

        private IEnumerable<XElement> SelectNodes(string xpathquery)
        {
            string[] route = xpathquery.Split('/');
            XElement curNode = pxmldoc.Element(route[0]);
            int i = 1;
            for (; i < route.Length - 1; i++)
            {
                curNode = curNode.Element(route[i]);
                if (curNode == null)
                    break;
            }
            if (i == route.Length - 2)
            {
                return curNode.Elements();
            }
            return null;
        }

        public void ApplyFilter(TestItem testmodule, string xpathquery)
        {
            //Note: We only filter if the filter actually returning something, (ie: we don't remove
            //all nodes just because nothing was selected).  Unless you specificy indicate to allow
            //an empty testcase.  Useful for inlcuding other assemblie, but not filtering the current one
            IEnumerable<XElement> nodes = SelectNodes(xpathquery);
            if (Enumerable.Count(nodes) > 0)
            {
                //Build a (object indexable) hashtable of all found items
                Dictionary<TestItem, XElement> found = new Dictionary<TestItem, XElement>();
                foreach (XElement xmlnode in nodes)
                {
                    TestItem node = FindMatchingNode(xmlnode);
                    if (node != null)
                        found.Add(node, xmlnode);
                }

                //If the entire testmodule was selected as part of the filter, were done.
                //(as all children are implicitly included if the parent is selected)
                if (!found.ContainsKey(testmodule))
                {
                    ApplyFilter(testmodule, found.Keys);
                }
            }
            else
            {
                //No results
                testmodule.Children.Clear();
            }
        }

        public void ApplyFilter(TestItem testItem, ICollection<TestItem> found)
        {
            // Remove all test items not found in the list.
            for (int i = 0; i < testItem.Children.Count; i++)
            {
                //If the entire test item was selected as part of the filter, we are done.
                //(as all children are implicitly included if the parent is selected)
                TestItem child = testItem.Children[i];
                if (!found.Contains(child))
                {
                    //If this is a leave test item then remove
                    if (child.Children.Count <= 0)
                    {
                        testItem.Children.Remove(child);
                        i--;
                    }
                    //Otherwise, check its children
                    else
                    {
                        ApplyFilter(child, found);

                        //If no test item children are left, (and since the test item wasn't on the list),
                        //then the test item shouldn't be removed as well
                        if (child.Children.Count <= 0)
                        {
                            testItem.Children.Remove(child);
                            i--;
                        }
                    }
                }
            }
        }

        public delegate bool ApplyFilterPredicate(TestItem item);

        public void ApplyFilter(TestItem testItem, ApplyFilterPredicate predicate)
        {
            // Remove all test items not found in the list.
            // Not using foreach so that we can remove items from the list as we walk it
            for (int i = 0; i < testItem.Children.Count; i++)
            {
                TestItem child = testItem.Children[i];
                // If the child passes the filter we're done
                //   automatically include all its children as well
                if (!predicate(child))
                {
                    //If this is a leave test item then remove
                    if (child.Children.Count <= 0)
                    {
                        testItem.Children.Remove(child);
                        i--;
                    }
                    //Otherwise, check its children
                    else
                    {
                        ApplyFilter(child, predicate);

                        //If no test item children are left, (and since the test item wasn't on the list),
                        //then the test item shouldn't be removed as well
                        if (child.Children.Count <= 0)
                        {
                            testItem.Children.Remove(child);
                            i--;
                        }
                    }
                }
            }
        }

        protected TestItem FindMatchingNode(XElement xmlnode)
        {
            TestItem found = null;

            //Matching nodes are always elements (testmodule, testcase, variation)
            while (xmlnode.NodeType != XmlNodeType.Element)
                xmlnode = xmlnode.Parent;

            //Note: We actually placed a uniqueid in the XmlElement node itself (derived node type)
            CXmlElement element = xmlnode as CXmlElement;
            if (element != null)
                found = (TestItem)puniqueids[element.UserData];

            return found;
        }

        public XDocument XmlDocument
        {
            get
            {
                //Deferred creation
                if (pxmldoc == null)
                    this.CreateSpec();
                return pxmldoc;
            }
        }

        protected void CreateSpec()
        {
            //Note: We want both API and (xml) DataDriven tests to all to filter
            //We need to expose our attributes, (and testcase information) in a similar
            //xml structure so we can leverage xpath queries (not redesign our own filtering syntax),
            //plus this allows both teams to have a identical (similar as possible) queries/filters.

            //Xml Spec Format:
            //	Example:
            /*
                <TestModule Name="Functional" Created="10 October 2001" Modified="10 October 2001" Version="1">
                <-- Owner type is an enum, "test", "dev" or "pm" -->
                <Owner Alias="YourAlias" Type="test"/>
                <Description>XQuery conformance tests</Description>
                    <Data filePath="\\webxtest\wdtest\managed\..." DBName="Northwind" Anything="whatever you want to be global">
                        <!--My global data -->
                        <xml>http://webdata/data/mytest/test.xml</xml>
                        <xsd>http://webdata/data/mytest/test.xsd</xsd>
                    </Data>
                    <TestCase name="FLWR Expressions">
                           <Description>Tests for FLWR expressions</description>
                        <Variation id="1" Implemented="true" Priority="2">
                            <Description>Simple 1 FLWR expression</description>
                            <FilterCriteria>  
                                            <!-- Recommended place for filter criteria -->
                                            <Os>NT</Os>
                                            <Language>English</Language>
                            </FilterCriteria>
                            <Data >
                                            <!-- Override global data -->
                                            <xml>http://webdata/data/mytest/specialptest.xml</xml>
                            </Data>
                            <SoapData>  
                                            <!-- Additional data for SOAP tests -->
                                            <wsdl>http://webdata/data/mytest/test.wsdl</wsdl>
                            </SoapData>  
                        </Variation>
                        </TestCase>
                </TestModule>
            */

            //Create the document
            pxmldoc = new CXmlDocument();

            //Add the module (from the root)
            this.AddChild(pxmldoc, ptestmodule);
        }

        protected void AddChild(XNode parent, TestItem node)
        {
            //<TestModule/TestCase/Variation>
            string name = node.Type.ToString();

            //Create the Element
            CXmlElement element = (CXmlElement)new XElement(name);
            if (parent is XDocument)
            {
                ((XDocument)parent).Add(element);
            }
            else if (parent is XElement)
            {
                ((XElement)parent).Add(element);
            }
            else
            {
                throw new Exception("Error in Add Child");
            }

            //Add typed properties
            AddProperty(element, "Name", node.Name, 0);
            AddProperty(element, "Desc", node.Desc, 0);
            AddProperty(element, "Id", node.Order, 0);
            AddProperty(element, "Priority", node.Priority, 0);
            AddProperty(element, "Owner", node.Owner, 0);
            AddProperty(element, "Owners", node.Owners, TestPropertyFlags.MultipleValues);
            AddProperty(element, "Version", node.Version, 0);

            //Add extended properties
            AddProperties(element, node);

            //Add our own uniqueid (for fast assoication later)
            //Note: We place this 'userdata' into our own version of the XmlElement, (derived class)
            element.UserData = ++pnextuniqueid;
            puniqueids.Add(pnextuniqueid, node);

            //Recursure through the children
            foreach (TestItem childnode in node.Children)
                this.AddChild(element, childnode);
        }

        protected void AddProperties(XElement element, TestItem node)
        {
            //Obtain ALL the meta-data from the test.  This is stored in the properties
            //collection, which is at least all the attribute data, (plus potentially more)
            foreach (TestProperty property in node.Metadata)
                this.AddProperty(element, property.Name, property.Value, property.Flags);
        }

        protected void AddProperty(XElement element, string name, object value, TestPropertyFlags flags)
        {
            //Ignore all the properties that have no name or value (as to not bloat the xml)
            if (name == null || value == null)
                return;

            //How to serialize (elements or attributes)
            if ((flags & TestPropertyFlags.MultipleValues) != 0)
            {
                AddValue(element, name, value);
            }
            else
            {
                element.SetAttributeValue(name, StringEx.ToString(value));
            }
        }

        protected void AddValue(XElement element, string name, object value)
        {
            //Recurise through the value(s)
            if (value != null && value.GetType().HasElementType && value is System.Collections.IEnumerable)
            {
                //Recurse through the values
                foreach (object item in (System.Collections.IEnumerable)value)
                    AddValue(element, name, item);
            }
            else
            {
                element.SetElementValue(name, StringEx.ToString(value));
            }
        }
    }

    ////////////////////////////////////////////////////////////////
    // CXmlDocument
    //
    ////////////////////////////////////////////////////////////////
    internal class CXmlDocument : XDocument
    {
        public XElement CreateElement(string prefix, string name, string namespaceURI)
        {
            return new CXmlElement(prefix, name, namespaceURI, this);
        }
    }

    ////////////////////////////////////////////////////////////////
    // CXmlElement
    //
    ////////////////////////////////////////////////////////////////
    internal class CXmlElement : XElement
    {
        public object UserData = null;

        public CXmlElement(string prefix, string name, string namespaceURI, CXmlDocument xmldoc)
            : base(prefix, name, namespaceURI, xmldoc)
        {
        }
    }
}
