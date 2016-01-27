// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.Collections.Generic;
using System.Xml.Linq;

namespace System.Xml.Tests
{
    /// <summary>
    /// CXmlDriverParam
    /// </summary>
    public abstract class CXmlDriverParamRawNodes
    {
        protected XElement poriginalNode;
        protected XElement pvirtualNode;
        protected CXmlDriverParamRawNodes pparentParams;

        internal CXmlDriverParamRawNodes(XElement originalNode, XElement virtualNode, CXmlDriverParamRawNodes parentParams)
        {
            this.poriginalNode = originalNode;
            this.pvirtualNode = virtualNode;
            this.pparentParams = parentParams;
        }

        // original test module node
        public virtual XElement TestModule { get { return null; } }

        // original test case node
        public virtual XElement TestCase { get { return null; } }

        // original test variation node
        public virtual XElement Variation { get { return null; } }

        // dynamic node which includes all sections combined using Inheritance rules.
        public virtual XElement Virtual
        {
            get
            {
                // support of delayed building of the Virtual node
                if (pvirtualNode == null)
                {
                    pvirtualNode = CXmlDriverEngine.BuildVirtualNode(pparentParams.Virtual, poriginalNode);
                }
                return pvirtualNode;
            }
        }
    }


    internal class CXmlDriverParamRawNodes_TestModule : CXmlDriverParamRawNodes
    {
        internal CXmlDriverParamRawNodes_TestModule(XElement testModuleNode, XElement virtualNode, CXmlDriverParamRawNodes parentParams) : base(testModuleNode, virtualNode, parentParams) { }
        public override XElement TestModule { get { return poriginalNode; } }
    }


    internal class CXmlDriverParamRawNodes_TestCase : CXmlDriverParamRawNodes
    {
        internal CXmlDriverParamRawNodes_TestCase(XElement testCaseNode, XElement virtualNode, CXmlDriverParamRawNodes parentParams) : base(testCaseNode, virtualNode, parentParams) { }
        public override XElement TestModule { get { return pparentParams.TestModule; } }
        public override XElement TestCase { get { return poriginalNode; } }
    }


    internal class CXmlDriverParamRawNodes_Variation : CXmlDriverParamRawNodes
    {
        internal CXmlDriverParamRawNodes_Variation(XElement variationNode, XElement virtualNode, CXmlDriverParamRawNodes parentParams) : base(variationNode, virtualNode, parentParams) { }
        public override XElement TestModule { get { return pparentParams.TestModule; } }
        public override XElement TestCase { get { return pparentParams.TestCase; } }
        public override XElement Variation { get { return poriginalNode; } }
    }


    public class CXmlDriverParam
    {
        private CXmlDriverParamRawNodes _rawNodes;
        private string _defaultSection;

        internal CXmlDriverParam(CXmlDriverParamRawNodes rawNodes, string defaultSection)
        {
            _rawNodes = rawNodes;
            _defaultSection = defaultSection;
        }

        // returns raw nodes
        public CXmlDriverParamRawNodes RawNodes
        {
            get
            {
                return _rawNodes;
            }
        }

        // returns InnerText from the Data (default section) or null if the given xpath selects nothing
        public string SelectValue(string xpath)
        {
            XNode curNode = DefaultSection;
            return SelectValue(xpath, curNode);
        }

        private string SelectValue(string xpath, XNode curNode)
        {
            string[] route = xpath.Split('/');
            int i = 0;
            for (; i < route.Length; i++)
            {
                if (curNode == null || route[i].StartsWith("@") || curNode.NodeType != XmlNodeType.Element)
                {
                    break;
                }
                curNode = ((XElement)curNode).Element(route[i]);
            }
            if (i == route.Length - 1)
            {
                if (curNode != null)
                {
                    XAttribute attr = ((XElement)curNode).Attribute(route[i].Substring(1));
                    if (attr != null)
                    {
                        return attr.Value;
                    }
                }
            }
            else if (i == route.Length)
            {
                if (curNode != null)
                {
                    return ((XElement)curNode).Value;
                }
            }
            return null;
        }

        // returns InnerText from the given section, or null if the given xpath selects nothing
        public string SelectValue(string xpath, string sectionName)
        {
            XElement sectionNode = GetSection(sectionName);
            return SelectValue(xpath, sectionNode);
        }

        // returns InnerText from the Data (default section),
        // throws the CTestFailedException if node doesn't exist. 
        public string SelectExistingValue(string xpath)
        {
            string value = SelectValue(xpath);
            CError.Compare(value != null, true, "XmlDriver: '" + xpath + "' is not found.");
            return value;
        }

        // returns a value from the given section,
        // throws the CTestFailedException if value doesn't exist. 
        public string SelectExistingValue(string xpath, string sectionName)
        {
            string value = SelectValue(xpath, sectionName);
            CError.Compare(value != null, true, "XmlDriver: '" + xpath + "' is not found in the section '" + sectionName + "'.");
            return value;
        }

        // selects nodes by xpath from the Data (default section)
        public IEnumerable<XElement> SelectNodes(string xpath)
        {
            return DefaultSection.Elements(xpath);
        }


        // selects nodes by xpath from the given section
        public IEnumerable<XElement> SelectNodes(string xpath, string sectionName)
        {
            XElement sectionNode = GetSection(sectionName);
            if (sectionNode == null)
                return _rawNodes.Virtual.Elements("*[-1]"); // create a dummy empty XmlNodeList
            return sectionNode.Elements(xpath);
        }

        public string DefaultSectionName { get { return _defaultSection; } set { _defaultSection = value; } }

        public XElement DefaultSection
        {
            get
            {
                XElement node = GetSection(_defaultSection);
                CError.Compare(node != null, true, "XmlDriver: no default section found, defaultSectionName='" + _defaultSection);
                return node;
            }
        }

        internal XElement GetSection(string sectionName)
        {
            XElement node = _rawNodes.Virtual.Element(sectionName);
            return node;
        }


        // returns an attribute from the Virtual node by attribute name
        public string GetTopLevelAttributeValue(string attrName)
        {
            XAttribute attr = _rawNodes.Virtual.Attribute(attrName);
            if (attr == null)
                return null;
            return attr.Value;
        }

        public string GetTopLevelExistingAttributeValue(string attrName)
        {
            string value = GetTopLevelAttributeValue(attrName);
            CError.Compare(value != null, true, "XmlDriver: '" + attrName + "' attribute is not found.");
            return value;
        }

        // 'inline' implementation without using Virtual node for perf improvement (only for internal use)
        internal static string GetTopLevelAttributeValue_Inline(CXmlDriverParamRawNodes param, string attrName)
        {
            XAttribute attr = null;
            if (param.Variation != null)
                attr = param.Variation.Attribute(attrName);
            if (attr == null && param.TestCase != null)
                attr = param.TestCase.Attribute(attrName);
            if (attr == null && param.TestModule != null)
                attr = param.TestModule.Attribute(attrName);
            if (attr == null)
                return null;
            return attr.Value;
        }
    }
}
