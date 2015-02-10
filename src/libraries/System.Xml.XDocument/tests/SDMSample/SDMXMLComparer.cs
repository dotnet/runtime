// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace CoreXml.Test.XLinq
{
    /// <summary>
    /// Xml comparison functionality for various tests of the programming model.
    /// </summary>
    internal static class XmlComparer
    {
        /// <summary>
        /// Checks if a node is of a certain type and throws if not.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="actualNode"></param>
        /// <param name="parentElement"></param>
        /// <returns></returns>
        private static T CheckNodeType<T>(object actualNode, XElement parentElement) where T : XNode
        {
            T actual = actualNode as T;
            if (actual == null)
            {
                throw new TestFailedException(string.Format(
                    "Expected {0}, got {1}, in element '{2}'",
                    typeof(T).Name,
                    actualNode.GetType().Name,
                    parentElement.Name.ToString()));
            }

            return actual;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="parentName"></param>
        private static void CompareTextualValues(string actual, string expected, string parentName)
        {
            bool equal = actual == expected;

            if (!equal && (actual != null) && (expected != null))
            {
                // Don't allow "\r\n" vs "\n" to be counted as a difference.
                string actual2 = actual.Replace("\r\n", "\n");
                string expected2 = expected.Replace("\r\n", "\n");

                equal = actual2 == expected2;
            }

            if (!equal)
            {
                throw new TestFailedException(string.Format(
                    "In text, cdata, or whitespace in element {0}, actual value '{1}' does not match expected value '{2}'",
                    parentName,
                    actual,
                    expected));
            }
        }
    }
}

