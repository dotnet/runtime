// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace XPathTests.Common
{
    public static partial class Utils
    {
        public static XPathNavigator CreateNavigatorFromFile(string fileName)
        {
            var navigator = _navigatorCreator.CreateNavigatorFromFile(fileName);
            // Will fail if file not found
            Assert.NotNull(navigator);
            return navigator;
        }

        public static XPathNavigator CreateNavigator(string xml)
        {
            return _navigatorCreator.CreateNavigator(xml);
        }

        private static XPathNavigator CreateNavigator(string xml, string startingNodePath, XmlNamespaceManager namespaceManager)
        {
            var xPathNavigator = CreateNavigatorFromFile(xml);

            if (String.IsNullOrWhiteSpace(startingNodePath))
                return xPathNavigator;

            var startingNode = xPathNavigator.Compile(startingNodePath);

            if (namespaceManager != null)
                startingNode.SetContext(namespaceManager);

            var xPathNodeIterator = xPathNavigator.Select(startingNode);

            Assert.True(xPathNodeIterator.MoveNext());

            return xPathNodeIterator.Current;
        }

        public static void XPathMatchTest(string xml, string testExpression, bool expected, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
        {
            var result = XPathMatch(xml, testExpression, namespaceManager, startingNodePath);
            Assert.Equal(expected, result);
        }

        private static bool XPathMatch(string xml, string testExpression, XmlNamespaceManager namespaceManager, string startingNodePath)
        {
            var xPathNavigator = CreateNavigator(xml, startingNodePath, namespaceManager);

            var xPathExpression = xPathNavigator.Compile(testExpression);

            if (namespaceManager != null)
                xPathExpression.SetContext(namespaceManager);

            var xPathNodeIterator = xPathNavigator.Select(xPathExpression);

            xPathNodeIterator.MoveNext();
            var current = xPathNodeIterator.Current;

            return namespaceManager == null ? current.Matches(testExpression) : current.Matches(XPathExpression.Compile(testExpression, namespaceManager));
        }

        public static void XPathMatchTestThrows<T>(string xml, string testExpression, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
            where T : Exception
        {
            Assert.Throws<T>(() => XPathMatch(xml, testExpression, namespaceManager, startingNodePath));
        }

        private static T XPathObject<T>(string xml, string testExpression, XmlNamespaceManager namespaceManager, string startingNodePath)
        {
            var xPathNavigator = CreateNavigator(xml, startingNodePath, namespaceManager);

            var xPathExpression = xPathNavigator.Compile(testExpression);

            if (namespaceManager != null)
                xPathExpression.SetContext(namespaceManager);

            var evaluated = xPathNavigator.Evaluate(xPathExpression);

            return (T)Convert.ChangeType(evaluated, typeof(T), CultureInfo.InvariantCulture);
        }

        internal static void XPathStringTest(string xml, string testExpression, object expected, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
        {
            var result = XPathObject<string>(xml, testExpression, namespaceManager, startingNodePath);

            Assert.Equal(expected, result);
        }

        internal static void XPathStringTestThrows<T>(string xml, string testExpression, string startingNodePath = null)
            where T : Exception
        {
            Assert.Throws<T>(() => XPathObject<string>(xml, testExpression, null, startingNodePath));
        }

        internal static void XPathNumberTest(string xml, string testExpression, double expected, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
        {
            var result = XPathObject<double>(xml, testExpression, namespaceManager, startingNodePath);
            Assert.Equal(expected, (double)result);
        }

        internal static void XPathBooleanTest(string xml, string testExpression, bool expected, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
        {
            var result = XPathObject<bool>(xml, testExpression, namespaceManager, startingNodePath);
            Assert.Equal(expected, result);
        }

        internal static void XPathNumberTestThrows<T>(string xml, string testExpression, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
            where T : Exception
        {
            Assert.Throws<T>(() => XPathObject<double>(xml, testExpression, namespaceManager, startingNodePath));
        }

        internal static void XPathNodesetTest(string xml, string testExpression, XPathResult expected, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
        {
            var xPathNavigator = CreateNavigator(xml, startingNodePath, namespaceManager);
            var xExpression = xPathNavigator.Compile(testExpression);

            if (namespaceManager != null)
                xExpression.SetContext(namespaceManager);

            var xPathSelection = xPathNavigator.Select(xExpression);

            Assert.Equal(expected.CurrentPosition, xPathSelection.CurrentPosition);
            Assert.Equal(expected.Results.Length, xPathSelection.Count);

            foreach (var expectedResult in expected.Results)
            {
                Assert.True(xPathSelection.MoveNext());

                Assert.Equal(expectedResult.NodeType, xPathSelection.Current.NodeType);
                Assert.Equal(expectedResult.BaseURI, xPathSelection.Current.BaseURI);
                Assert.Equal(expectedResult.HasChildren, xPathSelection.Current.HasChildren);
                Assert.Equal(expectedResult.HasAttributes, xPathSelection.Current.HasAttributes);
                Assert.Equal(expectedResult.IsEmptyElement, xPathSelection.Current.IsEmptyElement);
                Assert.Equal(expectedResult.LocalName, xPathSelection.Current.LocalName);
                Assert.Equal(expectedResult.Name, xPathSelection.Current.Name);
                Assert.Equal(expectedResult.NamespaceURI, xPathSelection.Current.NamespaceURI);
                Assert.Equal(expectedResult.HasNameTable, xPathSelection.Current.NameTable != null);
                Assert.Equal(expectedResult.Prefix, xPathSelection.Current.Prefix);
                Assert.Equal(expectedResult.XmlLang, xPathSelection.Current.XmlLang);

                if (String.IsNullOrWhiteSpace(xPathSelection.Current.Value))
                    Assert.Equal(expectedResult.Value.Trim(), xPathSelection.Current.Value.Trim());
                else
                    Assert.Equal(expectedResult.Value, xPathSelection.Current.Value.Replace("\r\n", "\n"));
            }
        }

        internal static void XPathNodesetTestThrows<T>(string xml, string testExpression, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
            where T : Exception
        {
            var xPathNavigator = CreateNavigator(xml, startingNodePath, namespaceManager);

            Assert.Throws<T>(() => xPathNavigator.Select(testExpression));
        }
    }
}
