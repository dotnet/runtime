// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Xunit.Abstractions;
using System.Text;
using Xunit;

namespace R2RDumpTest
{
    class TestHelpers
    {
        public static void RunTest(string expectedXmlPath, string name)
        {
            List<XmlNode> testXmlNodes = ReadXmlNodes($"{name}-test.xml", true).Cast<XmlNode>().ToList();
            List<XmlNode> expectedXmlNodes = ReadXmlNodes($"{expectedXmlPath}{name}.xml", true).Cast<XmlNode>().ToList();
            bool identical = XmlDiff(testXmlNodes, expectedXmlNodes);
            Assert.True(identical);
        }

        public static bool XmlDiff(List<XmlNode> testXmlNodes, List<XmlNode> expectedXmlNodes)
        {
            testXmlNodes.RemoveAll(node => !IsLeaf(node));
            expectedXmlNodes.RemoveAll(node => !IsLeaf(node));

            Dictionary<string, XmlNode> allTest = testXmlNodes.ToDictionary(node => XmlNodeFullName(node));
            Dictionary<string, XmlNode> allExpected = expectedXmlNodes.ToDictionary(node => XmlNodeFullName(node));
            Dictionary<string, XmlNode> diffTest = testXmlNodes.Except(expectedXmlNodes, new XElementEqualityComparer()).ToDictionary(node => XmlNodeFullName(node));
            Dictionary<string, XmlNode> diffExpected = expectedXmlNodes.Except(testXmlNodes, new XElementEqualityComparer()).ToDictionary(node => XmlNodeFullName(node));

            foreach (KeyValuePair<string, XmlNode> diff in diffExpected)
            {
                XmlNode expectedNode = diff.Value;
                Console.WriteLine("Expected:");
                Console.WriteLine("\t" + XmlNodeFullName(expectedNode) + ": " + expectedNode.InnerText);
                if (allTest.ContainsKey(diff.Key))
                {
                    XmlNode testNode = allTest[diff.Key];
                    Console.WriteLine("Test:");
                    Console.WriteLine("\t" + XmlNodeFullName(testNode) + ": " + testNode.InnerText);
                }
                else
                {
                    Console.WriteLine("Test:");
                    Console.WriteLine("\tnone");
                }
                Console.WriteLine("");
            }
            foreach (KeyValuePair<string, XmlNode> diff in diffTest)
            {
                if (!allExpected.ContainsKey(diff.Key))
                {
                    Console.WriteLine("Expected:");
                    Console.WriteLine("\tnone");
                    Console.WriteLine("Test:");
                    Console.WriteLine("\t" + XmlNodeFullName(diff.Value) + ": " + diff.Value.InnerText);
                }
                Console.WriteLine("");
            }

            return diffExpected.Count == 0 && diffTest.Count == 0;
        }

        private class XElementEqualityComparer : IEqualityComparer<XmlNode>
        {
            public bool Equals(XmlNode x, XmlNode y)
            {
                return XmlNodeFullName(x).Equals(XmlNodeFullName(y)) && x.InnerText.Equals(y.InnerText);
            }
            public int GetHashCode(XmlNode obj)
            {
                return 0;
            }
        }

        private static bool IsLeaf(XmlNode node)
        {
            return !node.HasChildNodes || node.FirstChild.NodeType == XmlNodeType.Text;
        }

        private static string XmlNodeFullName(XmlNode node)
        {
            string fullName = "";
            XmlNode n = node;
            while (node != null && node.NodeType != XmlNodeType.Document)
            {
                string index = "";
                XmlAttribute indexAttribute = node.Attributes["Index"];
                if (indexAttribute != null) {
                    index = indexAttribute.Value;
                }
                fullName = node.Name + index + "." + fullName;
                node = node.ParentNode;
            }
            return fullName;
        }

        public static XmlNodeList ReadXmlNodes(string filenameOrXmlString, bool fromFile)
        {
            XmlDocument expectedXml = new XmlDocument();
            if (fromFile)
            {
                expectedXml.Load(filenameOrXmlString);
            }
            else
            {
                expectedXml.LoadXml(filenameOrXmlString);
            }
            return expectedXml.SelectNodes("//*");
        }
    }
}
