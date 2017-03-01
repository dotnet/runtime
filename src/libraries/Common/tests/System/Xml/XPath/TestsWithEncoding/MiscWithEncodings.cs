// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Location.Paths
{
    /// <summary>
    /// Location Paths - MiscWithEncodings
    /// </summary>
    public static partial class MiscWithEncodingTests
    {
        public static void TestInitialize()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Return all OrderDate's equal to ""11/16/94"".
        /// OrderIDs/CustomerIDs/EmployeeIDs/OrderDates/OrderDate[.="11/16/94"]
        /// </summary>
        [Fact]
        public static void AbbreviatedSyntaxTest125()
        {
            TestInitialize();

            var xml = "XQL_Orders_j1.xml";
            var testExpression = @"OrderIDs/CustomerIDs/EmployeeIDs/OrderDates/OrderDate[.='11/16/94']";
            var expected = new XPathResult(0);

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Russian: xpath testing problem char, return 1 node
        /// </summary>
        [Fact]
        public static void GlobalizationTest5612()
        {
            TestInitialize();

            var xml = "Russian_problem_chars.xml";
            var testExpression = @"//root[contains(text(), ""?? ¤ ?? ?? © ? ® ??"")]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "root",
                    Name = "root",
                    HasNameTable = true,
                    Value = "\n?? ¤ ?? ?? © ? ® ?? \n"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Testing multiple predicates - Return all CustomerIDs that have a Customer ID and EmployeeIDs with an EmployeeID and a Freight equal to 12.75.
        /// OrderIDs/CustomerIDs[CustomerID]/EmployeeIDs[EmployeeID][OrderDates/Freight=12.75]
        /// </summary>
        [Fact]
        public static void MatchesTest1136()
        {
            TestInitialize();

            var xml = "XQL_Orders_j3.xml";
            var startingNodePath = "//EmployeeIDs[1]";
            var testExpression = @"OrderIDs/CustomerIDs[CustomerID]/EmployeeIDs[EmployeeID][OrderDates/Freight=12.75]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Using a very large number for position - Return the 9999999999999999999999999999999999999999999999999999999999999999999999999999999999999th CustomerIDs (Should return nothing)
        /// OrderIDs/CustomerIDs[9999999999999999999999999999999999999999999999999999999999999999999999999999999999999]
        /// </summary>
        [Fact]
        public static void MatchesTest1137()
        {
            TestInitialize();

            var xml = "XQL_Orders_j3.xml";
            var startingNodePath = "//CustomerIDs";
            var testExpression =
                @"OrderIDs/CustomerIDs[9999999999999999999999999999999999999999999999999999999999999999999999999999999999999]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicates containing attribute - Return all OrderIDs CollatingOrder attribute greater than or equal to 1033.
        /// /ROOT/OrderIDs[OrderID/@CollatingOrder>=1033]/OrderID/@CollatingOrder
        /// </summary>
        [Fact]
        public static void MatchesTest1138()
        {
            TestInitialize();

            var xml = "xql_orders-flat-200a.xml";
            var startingNodePath = "/ROOT/OrderIDs[22]/OrderID/@CollatingOrder";
            var testExpression = @"/ROOT/OrderIDs[OrderID/@CollatingOrder>=1033]/OrderID/@CollatingOrder";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Multiple predicates - Return all OrderIDs where CustomerID's have an EmployeeIDs and EmployeeID equal to 3 and an OrderDate greater than or equal to ""11/16/94"".
        /// .//OrderIDs[CustomerIDs[EmployeeIDs][//EmployeeID='3']][//OrderDate>='11/16/94']
        /// </summary>
        [Fact]
        public static void PredicatesTest1038()
        {
            TestInitialize();

            var xml = "XQL_Orders_j3.xml";
            var startingNodePath = "/ROOT";
            var testExpression = @".//OrderIDs[CustomerIDs[EmployeeIDs][//EmployeeID='3']][//OrderDate>='11/16/94']";
            var expected = new XPathResult(0);

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Testing multiple predicates - Return all CustomerIDs that have a Customer ID and EmployeeIDs with an EmployeeID and a Freight equal to 12.75.
        /// OrderIDs/CustomerIDs[CustomerID]/EmployeeIDs[EmployeeID][OrderDates/Freight=12.75]
        /// </summary>
        [Fact]
        public static void PredicatesTest1039()
        {
            TestInitialize();
            var xml = "XQL_Orders_j3.xml";
            var startingNodePath = "/ROOT";
            var testExpression = @"OrderIDs/CustomerIDs[CustomerID]/EmployeeIDs[EmployeeID][OrderDates/Freight=12.75]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "EmployeeIDs",
                    Name = "EmployeeIDs",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\t\t3\n\t\t\t\t\n\t\t\t\t\t11/16/94\n\t\t\t\t\t12/14/94\n\t\t\t\t\t11/28/94\n\t\t\t\t\t1\n\t\t\t\t\t12.75\n\t\t\t\t\tLILA-Supermercado\n\t\t\t\t\tCarrera 52 con Ave. Bolívar #65-98 Llano Largo\n\t\t\t\t\tBarquisimeto\n\t\t\t\t\tLara\n\t\t\t\t\t3508\n\t\t\t\t\tVenezuela\n\t\t\t\t\n\t\t\t"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Using a very large number for position - Return the 9999999999999999999999999999999999999999999999999999999999999999999999999999999999999th CustomerIDs (Should return nothing)
        /// OrderIDs/CustomerIDs[9999999999999999999999999999999999999999999999999999999999999999999999999999999999999]
        /// </summary>
        [Fact]
        public static void PredicatesTest1040()
        {
            TestInitialize();
            var xml = "XQL_Orders_j1.xml";
            var startingNodePath = "/ROOT";
            var testExpression =
                @"OrderIDs/CustomerIDs[9999999999999999999999999999999999999999999999999999999999999999999999999999999999999]";
            var expected = new XPathResult(0);

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicates containing attribute - Return all OrderIDs CollatingOrder attribute greater than or equal to 1033.
        /// /ROOT/OrderIDs[OrderID/@CollatingOrder>=1033]/OrderID/@CollatingOrder
        /// </summary>
        [Fact]
        public static void PredicatesTest1041()
        {
            TestInitialize();
            var xml = "xql_orders-flat-200a.xml";
            var testExpression = @"/ROOT/OrderIDs[OrderID/@CollatingOrder>=1033]/OrderID/@CollatingOrder";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "CollatingOrder",
                    Name = "CollatingOrder",
                    HasNameTable = true,
                    Value = "1033"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "CollatingOrder",
                    Name = "CollatingOrder",
                    HasNameTable = true,
                    Value = "1033"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "CollatingOrder",
                    Name = "CollatingOrder",
                    HasNameTable = true,
                    Value = "1034"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "CollatingOrder",
                    Name = "CollatingOrder",
                    HasNameTable = true,
                    Value = "1033"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "CollatingOrder",
                    Name = "CollatingOrder",
                    HasNameTable = true,
                    Value = "1034"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "CollatingOrder",
                    Name = "CollatingOrder",
                    HasNameTable = true,
                    Value = "1034"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "CollatingOrder",
                    Name = "CollatingOrder",
                    HasNameTable = true,
                    Value = "1033"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "CollatingOrder",
                    Name = "CollatingOrder",
                    HasNameTable = true,
                    Value = "1033"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "CollatingOrder",
                    Name = "CollatingOrder",
                    HasNameTable = true,
                    Value = "1033"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "CollatingOrder",
                    Name = "CollatingOrder",
                    HasNameTable = true,
                    Value = "1033"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }
    }
}
