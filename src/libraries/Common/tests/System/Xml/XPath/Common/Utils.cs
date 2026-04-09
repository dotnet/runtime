// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using Xunit;

namespace XPathTests.Common
{
    public static partial class Utils
    {
        private static readonly ICreateNavigator s_xmlDocumentNavigatorCreator = new CreateNavigatorFromXmlDocument();
        private static readonly ICreateNavigator s_xmlReaderNavigatorCreator = new CreateNavigatorFromXmlReader();
        private static readonly ICreateNavigator s_xdocumentNavigatorCreator = new CreateNavigatorComparer();

        public static readonly string ResourceFilesPath = "System.Xml.Tests.TestData.";

        public enum NavigatorKind
        {
            XmlDocument,
            XPathDocument,
            XDocument,
        }

        private static ICreateNavigator NavigatorFromKind(NavigatorKind kind) =>
            kind switch
            {
                NavigatorKind.XmlDocument => s_xmlDocumentNavigatorCreator,
                NavigatorKind.XPathDocument => s_xmlReaderNavigatorCreator,
                NavigatorKind.XDocument => s_xdocumentNavigatorCreator,
                _ => throw new Exception($"Unknown kind: {kind}"),
            };

        public static XPathNavigator CreateNavigatorFromFile(NavigatorKind kind, string fileName)
        {
            var navigator = NavigatorFromKind(kind).CreateNavigatorFromFile(fileName);
            // Will fail if file not found
            Assert.NotNull(navigator);
            return navigator;
        }

        public static XPathNavigator CreateNavigator(NavigatorKind kind, string xml)
        {
            return NavigatorFromKind(kind).CreateNavigator(xml);
        }

        private static XPathNavigator CreateNavigator(NavigatorKind kind, string xml, string startingNodePath, XmlNamespaceManager namespaceManager)
        {
            var xPathNavigator = CreateNavigatorFromFile(kind, xml);

            if (string.IsNullOrWhiteSpace(startingNodePath))
                return xPathNavigator;

            var startingNode = xPathNavigator.Compile(startingNodePath);

            if (namespaceManager != null)
                startingNode.SetContext(namespaceManager);

            var xPathNodeIterator = xPathNavigator.Select(startingNode);

            Assert.True(xPathNodeIterator.MoveNext());

            return xPathNodeIterator.Current;
        }

        public static void XPathMatchTest(NavigatorKind kind, string xml, string testExpression, bool expected, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
        {
            var result = XPathMatch(kind, xml, testExpression, namespaceManager, startingNodePath);
            Assert.Equal(expected, result);
        }

        private static bool XPathMatch(NavigatorKind kind, string xml, string testExpression, XmlNamespaceManager namespaceManager, string startingNodePath)
        {
            var xPathNavigator = CreateNavigator(kind, xml, startingNodePath, namespaceManager);

            var xPathExpression = xPathNavigator.Compile(testExpression);

            if (namespaceManager != null)
                xPathExpression.SetContext(namespaceManager);

            var xPathNodeIterator = xPathNavigator.Select(xPathExpression);

            xPathNodeIterator.MoveNext();
            var current = xPathNodeIterator.Current;

            return namespaceManager == null ? current.Matches(testExpression) : current.Matches(XPathExpression.Compile(testExpression, namespaceManager));
        }

        public static void XPathMatchTestThrows<T>(NavigatorKind kind, string xml, string testExpression, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
            where T : Exception
        {
            Assert.Throws<T>(() => XPathMatch(kind, xml, testExpression, namespaceManager, startingNodePath));
        }

        private static T XPathObject<T>(NavigatorKind kind, string xml, string testExpression, XmlNamespaceManager namespaceManager, string startingNodePath)
        {
            var xPathNavigator = CreateNavigator(kind, xml, startingNodePath, namespaceManager);

            var xPathExpression = xPathNavigator.Compile(testExpression);

            if (namespaceManager != null)
                xPathExpression.SetContext(namespaceManager);

            var evaluated = xPathNavigator.Evaluate(xPathExpression);

            return (T)Convert.ChangeType(evaluated, typeof(T), CultureInfo.InvariantCulture);
        }

        internal static void XPathStringTest(NavigatorKind kind, string xml, string testExpression, object expected, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
        {
            var result = XPathObject<string>(kind, xml, testExpression, namespaceManager, startingNodePath);

            Assert.Equal(expected, result);
        }

        internal static void XPathStringTestThrows<T>(NavigatorKind kind, string xml, string testExpression, string startingNodePath = null)
            where T : Exception
        {
            Assert.Throws<T>(() => XPathObject<string>(kind, xml, testExpression, null, startingNodePath));
        }

        internal static void XPathNumberTest(NavigatorKind kind, string xml, string testExpression, double expected, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
        {
            var result = XPathObject<double>(kind, xml, testExpression, namespaceManager, startingNodePath);
            Assert.Equal(expected, (double)result);
        }

        internal static void XPathBooleanTest(NavigatorKind kind, string xml, string testExpression, bool expected, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
        {
            var result = XPathObject<bool>(kind, xml, testExpression, namespaceManager, startingNodePath);
            Assert.Equal(expected, result);
        }

        internal static void XPathNumberTestThrows<T>(NavigatorKind kind, string xml, string testExpression, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
            where T : Exception
        {
            Assert.Throws<T>(() => XPathObject<double>(kind, xml, testExpression, namespaceManager, startingNodePath));
        }

        internal static void XPathNodesetTest(NavigatorKind kind, string xml, string testExpression, XPathResult expected, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
        {
            var xPathNavigator = CreateNavigator(kind, xml, startingNodePath, namespaceManager);
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

                if (string.IsNullOrWhiteSpace(xPathSelection.Current.Value))
                    Assert.Equal(expectedResult.Value.Trim(), xPathSelection.Current.Value.Trim());
                else
                    Assert.Equal(expectedResult.Value, xPathSelection.Current.Value.Replace("\r\n", "\n"));
            }
        }

        internal static void XPathNodesetTestThrows<T>(NavigatorKind kind, string xml, string testExpression, XmlNamespaceManager namespaceManager = null, string startingNodePath = null)
            where T : Exception
        {
            var xPathNavigator = CreateNavigator(kind, xml, startingNodePath, namespaceManager);

            Assert.Throws<T>(() => xPathNavigator.Select(testExpression));
        }
    }
}
