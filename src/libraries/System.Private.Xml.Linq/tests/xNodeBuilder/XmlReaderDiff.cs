// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Test.ModuleCore;
using System.Xml.Linq;

namespace CoreXml.Test.XLinq
{
    public static class XMLReaderExtension
    {
        public static string[] GetSAData(this XmlReader r)
        {
            return new string[] { r.Prefix, r.LocalName, r.NamespaceURI, r.Value };
        }
    }

    public static class ReaderDiffNSAware
    {
        public class NSStack
        {
            private Dictionary<int, Dictionary<string, string>> _nsStack = new Dictionary<int, Dictionary<string, string>>();

            public NSStack()
            {
                // prepopulate ns stack with the xml namespace
                Dictionary<string, string> xmlNs = new Dictionary<string, string>();
                xmlNs.Add("xml", XNamespace.Xml.NamespaceName);
                _nsStack.Add(0, xmlNs);
            }

            public void Add(int depth, string prefix, string ns)
            {
                // verify whether we do have duplicate
                TestLog.Compare(!IsDuplicate(depth, prefix, ns), string.Format("Duplicate: {0}, {1}", prefix, ns));
                // no duplicates, add new one
                _nsStack.TryAdd(depth, new Dictionary<string, string>());
                _nsStack[depth].Add(prefix, ns);
            }

            public bool IsDuplicate(int depth, string prefix, string ns)
            {
                // verify whether we do have duplicate
                for (int i = depth; i >= 0; i--)
                {
                    if (_nsStack.ContainsKey(i) && _nsStack[i].ContainsKey(prefix))
                    {
                        return (_nsStack[i][prefix] == ns);
                    }
                }
                return false;
            }

            public void RemoveDepth(int depth)
            {
                if (_nsStack.ContainsKey(depth))
                {
                    _nsStack.Remove(depth);  // remove the lines and decrement current depth
                }
            }
        }

        public static void CompareNamespaceAware(SaveOptions so, XmlReader originalReader, XmlReader filteredReader)
        {
            if ((so & SaveOptions.OmitDuplicateNamespaces) > 0) CompareNamespaceAware(originalReader, filteredReader);
            else ReaderDiff.Compare(originalReader, filteredReader);
            return;
        }

        public static void CompareNamespaceAware(XmlReader originalReader, XmlReader filteredReader)
        {
            NSStack nsStack = new NSStack();

            while (true)
            {
                bool r1Read = originalReader.Read();
                TestLog.Compare(r1Read, filteredReader.Read(), "Read() out of sync");
                if (!r1Read) break;
                if (filteredReader.NodeType != XmlNodeType.Text)
                {
                    TestLog.Compare(originalReader.NodeType, filteredReader.NodeType, "nodeType");
                    TestLog.Compare(originalReader.LocalName, filteredReader.LocalName, "localname");
                    TestLog.Compare(originalReader.NamespaceURI, filteredReader.NamespaceURI, "Namespaceuri");
                }
                else
                {
                    TestLog.Compare(originalReader.Value, filteredReader.Value, "localname");
                }

                if (originalReader.NodeType == XmlNodeType.Element)
                {
                    // read all r1 attributes
                    var origAttrs = MoveAtrributeEnumerator(originalReader).ToArray();
                    // read all r2 attributes
                    var filteredAttrs = MoveAtrributeEnumerator(filteredReader).ToArray();

                    // Verify HasAttributes consistency
                    TestLog.Compare(filteredAttrs.Any(), filteredReader.HasAttributes, "has attributes");
                    TestLog.Compare(filteredAttrs.Length, filteredReader.AttributeCount, "attribute count");

                    // Verify int indexers consistency
                    for (int i = 0; i < filteredAttrs.Length; i++)
                    {
                        // Verify int indexers consistency
                        TestLog.Compare(filteredReader[i], filteredAttrs[i][3], "value of attribute ... indexer[int]");
                        TestLog.Compare(filteredReader.GetAttribute(i), filteredAttrs[i][3], "value of attribute ... GetAttribute[int]");
                        filteredReader.MoveToAttribute(i);
                        TestLog.Compare(SAEqComparer.Instance.Equals(filteredReader.GetSAData(), filteredAttrs[i]), "Move to attribute int - wrong position");
                        VerifyNextFirstAttribute(filteredReader, filteredAttrs, i, " after MoveToAttribute (int)");
                    }

                    // does not overreach the index
                    {
                        try
                        {
                            filteredReader.MoveToAttribute(filteredAttrs.Length);
                            TestLog.Compare(false, "READER over reached the index");
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                        }
                    }

                    // does not underreach the index
                    {
                        try
                        {
                            filteredReader.MoveToAttribute(-1);
                            TestLog.Compare(false, "READER under reached the index");
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                        }
                    }

                    // Verify string / string,string indexers consistency
                    // note: using int approach to be able to verify MoveToFirst/NextAttribute consistency
                    for (int i = 0; i < filteredAttrs.Length; i++)
                    {
                        var n = filteredAttrs[i];

                        TestLog.Compare(filteredReader[n[1], n[2]], n[3], "value of attribute ... indexer[string, string]");
                        TestLog.Compare(filteredReader.GetAttribute(n[1], n[2]), n[3], "value of attribute ... GetAttribute[string, string]");
                        TestLog.Compare(filteredReader.MoveToAttribute(n[1], n[2]), "Move to attribute string/string");
                        TestLog.Compare(SAEqComparer.Instance.Equals(filteredReader.GetSAData(), n), "Move to attribute string/string - wrong position");
                        VerifyNextFirstAttribute(filteredReader, filteredAttrs, i, " after MoveToAttribute (string,string)");

                        filteredReader.MoveToElement();   // reset the reader position

                        string qName = n[0] == "" ? n[1] : string.Format("{0}:{1}", n[0], n[1]);
                        TestLog.Compare(filteredReader[qName], n[3], "value of attribute ... indexer[string]");
                        TestLog.Compare(filteredReader.GetAttribute(qName), n[3], "value of attribute ... GetAttribute[string]");
                        TestLog.Compare(filteredReader.MoveToAttribute(qName), "Move to attribute string/string");
                        TestLog.Compare(SAEqComparer.Instance.Equals(filteredReader.GetSAData(), n), "Move to attribute string/string - wrong position");
                        VerifyNextFirstAttribute(filteredReader, filteredAttrs, i, " after MoveToAttribute (string)");

                        filteredReader.MoveToElement();   // reset the reader position

                        // lookup namespaces for the declaration nodes
                        if (n[2] == XNamespace.Xmlns || n[1] == "xmlns")
                        {
                            TestLog.Compare(filteredReader.LookupNamespace(n[0] != "" ? n[1] : ""), n[3], "Lookup namespace failed for kept declaration");
                        }
                    }

                    // Verify that not reported NS are not accessible
                    var duplicateAttrs = origAttrs.Except(filteredAttrs, SAEqComparer.Instance).ToArray();
                    foreach (var n in duplicateAttrs)
                    {
                        TestLog.Compare(filteredReader[n[1], n[2]], null, "Should not found : value of attribute ... indexer[string, string]");
                        TestLog.Compare(filteredReader.GetAttribute(n[1], n[2]), null, "Should not found : value of attribute ... GetAttribute[string, string]");
                        var orig = filteredReader.GetSAData();
                        TestLog.Compare(!filteredReader.MoveToAttribute(n[1], n[2]), "Should not found : Move to attribute string/string");
                        TestLog.Compare(SAEqComparer.Instance.Equals(filteredReader.GetSAData(), orig), "Should not found : Move to attribute string/string - wrong position");

                        filteredReader.MoveToElement();   // reset the reader position

                        string qName = n[0] == "" ? n[1] : string.Format("{0}:{1}", n[0], n[1]);
                        TestLog.Compare(filteredReader[qName], null, "Should not found : value of attribute ... indexer[string]");
                        TestLog.Compare(filteredReader.GetAttribute(qName), null, "Should not found : value of attribute ... GetAttribute[string]");
                        orig = filteredReader.GetSAData();
                        TestLog.Compare(!filteredReader.MoveToAttribute(qName), "Should not found : Move to attribute string/string");
                        TestLog.Compare(SAEqComparer.Instance.Equals(filteredReader.GetSAData(), orig), "Should not found : Move to attribute string/string - wrong position");

                        filteredReader.MoveToElement();   // reset the reader position

                        // removed are only namespace declarations
                        TestLog.Compare(n[2] == XNamespace.Xmlns || n[1] == "xmlns", "Removed is not namespace declaration");

                        // Lookup namespace ... should pass 
                        TestLog.Compare(filteredReader.LookupNamespace(n[0] != "" ? n[1] : ""), n[3], "Lookup namespace failed - for removed declaration");
                    }

                    // compare non-xmlns attributes
                    // compare xmlns attributes (those in r2 must be in r1)
                    // verify r2 namespace stack
                    Compare(origAttrs, filteredAttrs, nsStack, filteredReader.Depth);
                }

                // nsStack cleanup
                if (originalReader.NodeType == XmlNodeType.EndElement || originalReader.IsEmptyElement) nsStack.RemoveDepth(filteredReader.Depth);
            }
        }

        private static void VerifyNextFirstAttribute(XmlReader filteredReader, string[][] filteredAttrs, int attrPosition, string message)
        {
            // MoveToNextAttribute works OK
            bool shouldNAWork = (attrPosition < filteredAttrs.Length - 1);
            var orig = filteredReader.GetSAData();
            TestLog.Compare(filteredReader.MoveToNextAttribute(), shouldNAWork, "Move to next attribute :: " + message);
            TestLog.Compare(SAEqComparer.Instance.Equals(shouldNAWork ? filteredAttrs[attrPosition + 1] : orig, filteredReader.GetSAData()), "MoveToNextAttribute moved to bad position :: " + message);
            // MoveToFirstAttribute works OK
            TestLog.Compare(filteredReader.MoveToFirstAttribute(), "Move to first attribute should always work :: " + message);
            TestLog.Compare(SAEqComparer.Instance.Equals(filteredAttrs[0], filteredReader.GetSAData()), "MoveToNextAttribute moved to bad position :: " + message);
        }


        private class SAEqComparer : IEqualityComparer<string[]>
        {
            private static SAEqComparer s_instance = new SAEqComparer();
            public static SAEqComparer Instance
            {
                get { return s_instance; }
            }

            public bool Equals(string[] x, string[] y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(string[] strings)
            {
                if (strings.Length == 0) return 0;
                int hash = strings[0].GetHashCode();
                for (int i = 1; i < strings.Length; i++) hash ^= strings[i].GetHashCode();
                return hash;
            }
        }

        private static IEnumerable<string[]> IndexedAtrributeEnumerator(XmlReader r, bool moveToElement)
        {
            TestLog.Compare(r.NodeType, XmlNodeType.Element, "Assert for enumerator");  // assert
            int aCount = r.AttributeCount;
            for (int i = 0; i < aCount; i++)
            {
                r.MoveToAttribute(i);
                yield return r.GetSAData();
                if (moveToElement) r.MoveToElement();
            }
            r.MoveToElement();
        }

        private static IEnumerable<string[]> MoveAtrributeEnumerator(XmlReader r)
        {
            TestLog.Compare(r.NodeType, XmlNodeType.Element, "Assert for enumerator");  // assert
            if (r.MoveToFirstAttribute())
            {
                do
                {
                    yield return r.GetSAData();
                }
                while (r.MoveToNextAttribute());
                r.MoveToElement();
            }
        }

        public static void Compare(IEnumerable<string[]> r1, IEnumerable<string[]> r2, NSStack nsTable, int depth)
        {
            IEnumerator<string[]> r1E = r1.GetEnumerator();
            IEnumerator<string[]> r2E = r2.GetEnumerator();

            while (r2E.MoveNext())
            {
                bool found = false;
                while (!found)
                {
                    r1E.MoveNext();
                    found = r1E.Current.SequenceEqual(r2E.Current, StringComparer.Ordinal);
                    if (!found)
                    {
                        // Verify the one thrown out is a) ns decl: b) the nsTable detect it as duplicate
                        TestLog.Compare(r1E.Current[2] == XNamespace.Xmlns.NamespaceName || r1E.Current[1] == "xmlns", string.Format("Reader removed the non NS declaration attribute: {0},{1},{2},{3}", r1E.Current[0], r1E.Current[1], r1E.Current[2], r1E.Current[3]));
                        TestLog.Compare(nsTable.IsDuplicate(depth, r1E.Current[1], r1E.Current[3]), "The namespace decl was not duplicate");
                    }
                }
                TestLog.Compare(found, true, "Attribute from r2 not found in r1!");

                if (r2E.Current[2] == XNamespace.Xmlns.NamespaceName || r2E.Current[1] == "xmlns")
                {
                    nsTable.Add(depth, r2E.Current[1], r2E.Current[3]);
                }
            }
        }
    }
}
