// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests
{
    /// <summary>
    /// Customer Scenarios
    /// </summary>
    public static partial class CustomerScenariosTests
    {
        /// <summary>
        /// Expected: The last line element of the first section of the last chapter of the context node.
        /// Chapter[last()]/Section[1]/Line[last()]
        /// </summary>
        [Fact]
        public static void CustomerScenariosTest301()
        {
            var xml = "xpC001.xml";
            var startingNodePath = "/Book";
            var testExpression = @"Chapter[last()]/Section[1]/Line[last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Line",
                    Name = "Line",
                    HasNameTable = true,
                    Value = "Porsche 911"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: The last line element of the first section of the last chapter of the doc.
        /// /Book/Chapter[last()]/Section[1]/Line[last()]
        /// </summary>
        [Fact]
        public static void CustomerScenariosTest302()
        {
            var xml = "xpC001.xml";
            var startingNodePath = "/Book";
            var testExpression = @"/Book/Chapter[last()]/Section[1]/Line[last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Line",
                    Name = "Line",
                    HasNameTable = true,
                    Value = "Porsche 911"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the second chapter element child if that child has a name attribute with value ""Chapter2"".
        /// Chapter[2][@name="Chapter2"]
        /// </summary>
        [Fact]
        public static void CustomerScenariosTest303()
        {
            var xml = "xpC001.xml";
            var startingNodePath = "/Book";
            var testExpression = @"Chapter[2][@name='Entrees']";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Chapter",
                    Name = "Chapter",
                    HasNameTable = true,
                    Value =
                        "\n        \n            Almond Chicken\n            Sesame Chicken\n            Crispy Duck\n        \n        \n            Mashed potatoes with overcooked veggies and fat meat topped with some rich gravy\n            That's it!\n        \n        \n            Triple Bacon Cheeseburger\n            Double Bacon Cheeseburger\n            Bacon Cheeseburger\n        \n    "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: The 2nd to 4th chapter child node of the context node.
        /// Chapter[position() >= 2 and position() <= 4]
        /// </summary>
        [Fact]
        public static void CustomerScenariosTest304()
        {
            var xml = "xpC001.xml";
            var startingNodePath = "/Book";
            var testExpression = @"Chapter[position() >= 2 and position() <= 4]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Chapter",
                    Name = "Chapter",
                    HasNameTable = true,
                    Value =
                        "\n        \n            Almond Chicken\n            Sesame Chicken\n            Crispy Duck\n        \n        \n            Mashed potatoes with overcooked veggies and fat meat topped with some rich gravy\n            That's it!\n        \n        \n            Triple Bacon Cheeseburger\n            Double Bacon Cheeseburger\n            Bacon Cheeseburger\n        \n    "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Chapter",
                    Name = "Chapter",
                    HasNameTable = true,
                    Value =
                        "\n        \n            BMW M5\n            Mercedes S Class\n            Porsche 911\n        \n        \n            Acura CL 3.2\n            Lexus RX300\n            Infiniti QX4\n        \n        \n    "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }
    }
}
