// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;
using Xunit;

namespace XPathTests.FunctionalTests.CoreFunctionLibrary
{
    /// <summary>
    /// Core Function Library - Node Set Functions
    /// </summary>
    public static partial class NodeSetFunctionsTests
    {
        /// <summary>
        /// Expected: Selects the last element child of the context node.
        /// child::*[last()]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest221(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"child::*[last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child5",
                    Name = "Child5",
                    HasNameTable = true,
                    Value = "Last"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the second last element child of the context node.
        /// child::*[last() - 1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest222(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"child::*[last() - 1]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child4",
                    Name = "Child4",
                    HasNameTable = true,
                    Value = "Fourth"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the last attribute node of the context node.
        /// attribute::*[last()]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest223(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"attribute::*[last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "Attr5",
                    Name = "Attr5",
                    HasNameTable = true,
                    Value = "Last"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the second last attribute node of the context node.
        /// attribute::*[last() - 1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest224(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"attribute::*[last() - 1]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "Attr4",
                    Name = "Attr4",
                    HasNameTable = true,
                    Value = "Fourth"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the last element child of the context node.
        /// child::*[position() = last()]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest225(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"child::*[position() = last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child5",
                    Name = "Child5",
                    HasNameTable = true,
                    Value = "Last"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// *[position() = last()] (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest226(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child5";
            var testExpression = @"*[position() = last()]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// *[position() = last()] (Matches = false)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest227(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child4";
            var testExpression = @"*[position() = last()]";
            var expected = false;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// @*[position() = last()] (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest228(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr5";
            var testExpression = @"@*[position() = last()]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// @*[position() = last()] (Matches = false)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest229(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr4";
            var testExpression = @"@*[position() = last()]";
            var expected = false;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// *[last() = 1] (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2210(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test2/Child1";
            var testExpression = @"*[last() = 1]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// *[last() = 1] (Matches = false)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2211(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child1";
            var testExpression = @"*[last() = 1]";
            var expected = false;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// child::*[position() = 2]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2212(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"child::*[position() = 2]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child2",
                    Name = "Child2",
                    HasNameTable = true,
                    Value = "Second"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd attribute of the context node.
        /// attribute::*[position() = 2]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2213(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"attribute::*[position() = 2]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "Attr2",
                    Name = "Attr2",
                    HasNameTable = true,
                    Value = "Second"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// child::*[2]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2214(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"child::*[2]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child2",
                    Name = "Child2",
                    HasNameTable = true,
                    Value = "Second"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd attribute of the context node.
        /// attribute::*[2]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2215(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"attribute::*[2]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "Attr2",
                    Name = "Attr2",
                    HasNameTable = true,
                    Value = "Second"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all element children of the context node except the first two.
        /// child::*[position() > 2]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2216(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"child::*[position() > 2]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child3",
                    Name = "Child3",
                    HasNameTable = true,
                    Value = "Third"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child4",
                    Name = "Child4",
                    HasNameTable = true,
                    Value = "Fourth"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child5",
                    Name = "Child5",
                    HasNameTable = true,
                    Value = "Last"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all element children of the context node.
        /// child::*[position()]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2217(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"child::*[position()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child1",
                    Name = "Child1",
                    HasNameTable = true,
                    Value = "First"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child2",
                    Name = "Child2",
                    HasNameTable = true,
                    Value = "Second"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child3",
                    Name = "Child3",
                    HasNameTable = true,
                    Value = "Third"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child4",
                    Name = "Child4",
                    HasNameTable = true,
                    Value = "Fourth"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child5",
                    Name = "Child5",
                    HasNameTable = true,
                    Value = "Last"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// *[position() = 2] (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2218(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child2";
            var testExpression = @"*[position() = 2]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// *[position() = 2] (Matches = false)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2219(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child3";
            var testExpression = @"*[position() = 2]";
            var expected = false;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// @*[position() = 2] (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2220(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr2";
            var testExpression = @"@*[position() = 2]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// @*[position() = 2] (Matches = false)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2221(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr3";
            var testExpression = @"@*[position() = 2]";
            var expected = false;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// *[2] (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2222(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child2";
            var testExpression = @"*[2]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// *[2] (Matches = false)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2223(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child3";
            var testExpression = @"*[2]";
            var expected = false;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// @*[2] (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2224(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr2";
            var testExpression = @"@*[2]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// @*[2] (Matches = false)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2225(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr1";
            var testExpression = @"@*[2]";
            var expected = false;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// *[position() > 2] (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2226(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child3";
            var testExpression = @"*[position() > 2]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// *[position() > 2] (Matches = false)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2227(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child2";
            var testExpression = @"*[position() > 2]";
            var expected = false;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// @*[position() > 2] (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2228(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr3";
            var testExpression = @"@*[position() > 2]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// @*[position() > 2] (Matches = false)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2229(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr2";
            var testExpression = @"@*[position() > 2]";
            var expected = false;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Number of attribute nodes.
        /// count(attribute::*)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2230(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"count(attribute::*)";
            var expected = 5d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Number of attribute nodes.
        /// count(descendant::para)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2231(Utils.NavigatorKind kind)
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"count(descendant::Child3)";
            var expected = 1d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Namespace URI of the context node.
        /// namespace-uri() (element node)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2235(Utils.NavigatorKind kind)
        {
            var xml = "xp008.xml";
            var startingNodePath = "/Doc/ns:elem";
            var testExpression = @"namespace-uri()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "uri:this is a test");
            var expected = @"uri:this is a test";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Namespace URI of the context node.
        /// namespace-uri() (attribute node)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2236(Utils.NavigatorKind kind)
        {
            var xml = "xp008.xml";
            var startingNodePath = "/Doc/ns:elem/@ns:attr";
            var testExpression = @"namespace-uri()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "uri:this is a test");
            var expected = @"uri:this is a test";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Namespace URI of the first child node of the context node.
        /// namespace-uri(child::*)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2237(Utils.NavigatorKind kind)
        {
            var xml = "xp008.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"namespace-uri(child::*)";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "uri:this is a test");
            var expected = @"uri:this is a test";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Namespace URI of the first attribute node of the context node.
        /// namespace-uri(attribute::*)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2238(Utils.NavigatorKind kind)
        {
            var xml = "xp008.xml";
            var startingNodePath = "/Doc/ns:elem";
            var testExpression = @"namespace-uri(attribute::*)";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "uri:this is a test");
            var expected = @"uri:this is a test";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: QName of the context node.
        /// name() (with prefix, element node)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2241(Utils.NavigatorKind kind)
        {
            var xml = "xp008.xml";
            var startingNodePath = "/Doc/ns:elem";
            var testExpression = @"name()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "uri:this is a test");
            var expected = @"ns:elem";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: QName of the context node.
        /// name() (with prefix, attribute node)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2242(Utils.NavigatorKind kind)
        {
            var xml = "xp008.xml";
            var startingNodePath = "/Doc/ns:elem/@ns:attr";
            var testExpression = @"name()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "uri:this is a test");
            var expected = @"ns:attr";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: QName of the first child node of the context node.
        /// name(child::*)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2243(Utils.NavigatorKind kind)
        {
            var xml = "xp008.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"name(child::*)";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "uri:this is a test");
            var expected = @"ns:elem";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: QName of the first attribute node of the context node.
        /// name(attribute::*)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2244(Utils.NavigatorKind kind)
        {
            var xml = "xp008.xml";
            var startingNodePath = "/child::*/child::*";
            var testExpression = @"name(attribute::*)";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "uri:this is a test");
            var expected = @"ns:attr";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: QName of the context node.
        /// local-name() (with prefix, element node)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2247(Utils.NavigatorKind kind)
        {
            var xml = "xp008.xml";
            var startingNodePath = "/Doc/ns:elem";
            var testExpression = @"local-name()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "uri:this is a test");
            var expected = @"elem";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: QName of the context node.
        /// local-name() (with prefix, attribute node)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2248(Utils.NavigatorKind kind)
        {
            var xml = "xp008.xml";
            var startingNodePath = "/Doc/ns:elem/@ns:attr";
            var testExpression = @"local-name()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "uri:this is a test");
            var expected = @"attr";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Local part of the expanded-name of the first child node of the context node.
        /// local-name(child::*)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2249(Utils.NavigatorKind kind)
        {
            var xml = "xp008.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"local-name(child::*)";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "uri:this is a test");
            var expected = @"elem";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: QName of the first attribute node of the context node.
        /// local-name(attribute::*)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2250(Utils.NavigatorKind kind)
        {
            var xml = "xp008.xml";
            var startingNodePath = "/Doc/ns:elem";
            var testExpression = @"local-name(attribute::*)";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "uri:this is a test");
            var expected = @"attr";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Data file has no DTD, so no element has an ID, expected empty node-set
        /// id("1")
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        public static void NodeSetFunctionsTest2267(Utils.NavigatorKind kind)
        {
            var xml = "id4.xml";
            var testExpression = @"id(""1"")";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Expected: empty namespace uri
        /// namespace-uri() (namespace node)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2294(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var startingNodePath = "/ns:store/ns:booksection/namespace::NSbook";
            var testExpression = @"namespace-uri()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "http://default.htm");
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: empty namespace uri
        /// namespace-uri() (namespace node = xml)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2295(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var startingNodePath = "/ns:store/namespace::*[1]";
            var testExpression = @"namespace-uri()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "http://default.htm");
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: empty namespace uri
        /// namespace-uri() (namespace node = default ns)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2296(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var startingNodePath = "/ns:store/namespace::*[2]";
            var testExpression = @"namespace-uri()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "http://default.htm");
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: empty namespace uri
        /// namespace-uri() (namespace node)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2297(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var testExpression = @"namespace-uri(ns:store/ns:booksection/namespace::*)";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "http://default.htm");
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// Expected: empty namespace uri
        /// name() (namespace node)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2298(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var startingNodePath = "/ns:store/ns:booksection/namespace::NSbook";
            var testExpression = @"name()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "http://default.htm");
            var expected = @"NSbook";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: empty namespace uri
        /// name() (namespace node = xml)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest2299(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var startingNodePath = "/ns:store/namespace::*[1]";
            var testExpression = @"name()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "http://default.htm");
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: empty namespace uri
        /// name() (namespace node = default ns)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest22100(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var startingNodePath = "/ns:store/namespace::*[1]";
            var testExpression = @"name()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "http://default.htm");
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: empty namespace uri
        /// name() (namespace node)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NodeSetFunctionsTest22101(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var testExpression = @"name(ns:store/ns:booksection/namespace::*)";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "http://default.htm");
            var expected = @"NSbook";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager);
        }
    }
}
