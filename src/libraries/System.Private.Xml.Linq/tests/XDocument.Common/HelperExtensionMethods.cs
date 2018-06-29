// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public static class Helpers
    {
        private static XmlReaderSettings s_whitespacereaderSetting;

        static Helpers()
        {
            if (s_whitespacereaderSetting == null)
            {
                s_whitespacereaderSetting = new XmlReaderSettings();
                s_whitespacereaderSetting.IgnoreWhitespace = false;
            }
        }

        public static XmlReaderSettings WhitespaceReaderSettings { get { return s_whitespacereaderSetting; } }

        /// <summary>
        /// Returns the combinations of the items in array with each one item on the given position.
        /// For example for array {"A","B","C"} and position=1 will produce {"B","A","C"} , {"A","B","C"}, {"A","C","B"} - all items will appear on the pos=1.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">Source array</param>
        /// <param name="position">Position where should all items appear</param>
        /// <returns></returns>
        public static IEnumerable<T[]> PositionCombinations<T>(this T[] source, int position)
        {
            Debug.Assert(source.Length > position);
            for (int i = 0; i < source.Length; i++)
            {
                T[] retVal = (T[])source.Clone();
                retVal[position] = source[i];
                retVal[i] = source[position];
                yield return retVal;
            }
        }

        /// <summary>
        /// Returns all variations (order matters) of given length from the source array. 
        /// Limiting the default combinations with a maximum length of 2 using reduceVariations, to improve perf.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">source array</param>
        /// <param name="length">length of the variations</param>
        /// <returns></returns>
        public static IEnumerable<T[]> NonRecursiveVariations<T>(this T[] source, int length, bool reduceVariations = true)
        {
            Debug.Assert(source.Length > length);
            if (reduceVariations && length > 2) length = 2;
            Stack<int> stack = new Stack<int>();
            int xxx = 1;

            int position = 0;
            object[] selected = new object[length];
            int start = 0;
            switch (xxx)
            {
                case 1:
                    for (int i = start; i < source.Length; i++)
                    {
                        if (stack.Contains(i)) continue;
                        selected[position] = source[i];
                        if (position == (length - 1))
                        {
                            yield return (T[])selected.Clone();
                        }
                        else
                        {
                            position++;
                            stack.Push(i);
                            start = 0;
                            goto case 1;
                        }
                    }
                    position--;
                    if (position >= 0)
                    {
                        start = stack.Pop();
                        start++;
                        goto case 1;
                    }
                    break;
            }
        }

        /// <summary>
        /// Flatten the IEnumerable (aka. if a given IEnumerable contains another IEnumerable, this on will be flattened and items returned in sequential order).
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<object> Flatten(this IEnumerable<object> source)
        {
            List<object> list = new List<object>();
            return source.Flatten(list);
        }

        /// <summary>
        /// Flatten the IEnumerable (aka. if a given IEnumerable contains another IEnumerable, this on will be flattened and items returned in sequential order).
        /// </summary>
        /// <param name="source"></param>
        /// <param name="output">The List used for flattening</param>
        /// <returns></returns>
        public static IEnumerable<object> Flatten(this IEnumerable<object> source, List<object> output)
        {
            foreach (object element in source)
            {
                if (element is IEnumerable<object>)
                {
                    ((IEnumerable<object>)element).Flatten(output);
                }
                else
                {
                    output.Add(element);
                }
            }
            return output as IEnumerable<object>;
        }

        public static IEnumerable<T1> Concat2<T1, T2>(this IEnumerable<T1> source, IEnumerable<T2> addition)
            where T1 : class
            where T2 : T1
        {
            return source.Concat(addition.OfType<T1>());
        }

        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            return !source.Any();
        }

        public static bool EqualAll<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, IEqualityComparer<T2> comparer) where T1 : ExpectedValue
        {
            using (IEnumerator<T1> e1 = first.GetEnumerator())
            using (IEnumerator<T2> e2 = second.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    if (e2.MoveNext())
                    {
                        if (e1.Current.IsOriginalNode)
                        {
                            // special treatment for XText
                            if (e1.Current.Data is XText && !(e1.Current.Data is XCData))
                                if (!object.ReferenceEquals(e1.Current.originalReference, e2.Current))
                                {
                                    TestLog.WriteLine("XText reference comparison: ");
                                    return false;
                                }
                                else
                                {
                                    continue;
                                }

                            if (!e1.Current.Data.Equals(e2.Current))
                            {
                                TestLog.WriteLine("Reference comparison: " + e1.Current.Data.ToString() + ", " + e2.Current.ToString());
                                return false;
                            }
                        }
                        else
                        {
                            if (!comparer.Equals((T2)e1.Current.Data, e2.Current))
                            {
                                TestLog.WriteLine("Comparer comparison: " + e1.Current.Data.ToString() + ", " + e2.Current.ToString());
                                return false;
                            }
                        }
                    }
                    else
                    {
                        TestLog.WriteLine("No match in actual values");
                        return false;
                    }
                }
                if (e2.MoveNext())
                {
                    TestLog.WriteLine("Outstanding actual value");
                    return false;
                }
            }
            return true;
        }

        private static int compare1<T1>(T1 a, T1 b) where T1 : ExpectedValue
        {
            string namea = (a.Data as XAttribute).Name.LocalName;
            string nameb = (b.Data as XAttribute).Name.LocalName;
            return namea.CompareTo(nameb);
        }

        private static int compare1N<T1>(T1 a, T1 b) where T1 : ExpectedValue
        {
            string namea = (a.Data as XAttribute).Name.Namespace.NamespaceName;
            string nameb = (b.Data as XAttribute).Name.Namespace.NamespaceName;
            return namea.CompareTo(nameb);
        }

        private static int compare2<T2>(T2 a, T2 b) where T2 : XAttribute
        {
            string namea = a.Name.LocalName;
            string nameb = b.Name.LocalName;
            return namea.CompareTo(nameb);
        }

        private static int compare2N<T2>(T2 a, T2 b) where T2 : XAttribute
        {
            string namea = a.Name.Namespace.NamespaceName;
            string nameb = b.Name.Namespace.NamespaceName;
            return namea.CompareTo(nameb);
        }

        public static bool EqualAllAttributes<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, IEqualityComparer<T2> comparer)
            where T1 : ExpectedValue
            where T2 : XAttribute
        {
            if (first.IsEmpty() && (second == null || second.IsEmpty())) return true;
            if (first.IsEmpty())
            {
                TestLog.WriteLine("Expected values empty");
                return false;
            }
            if (second == null || second.IsEmpty())
            {
                TestLog.WriteLine("attributes empty");
                return false;
            }

            List<T1> expectedList = new List<T1>(first);
            expectedList.Sort(compare1);
            expectedList.Sort(compare1N);
            IEnumerable<T1> expectedSorted = expectedList;
            List<T2> actualList = new List<T2>(second);
            actualList.Sort(compare2);
            actualList.Sort(compare2N);
            IEnumerable<T2> actualSorted = actualList;
            return expectedSorted.EqualAll(actualSorted, comparer);
        }


        public static bool EqualsAllAttributes<T1>(this IEnumerable<T1> first, IEnumerable<T1> second, IEqualityComparer<T1> comparer)
           where T1 : XAttribute
        {
            if (first.IsEmpty() && (second == null || second.IsEmpty())) return true;
            if (first.IsEmpty())
            {
                TestLog.WriteLine("Expected Attributes empty");
                return false;
            }
            if (second == null || second.IsEmpty())
            {
                TestLog.WriteLine("Actual Attributes empty");
                return false;
            }

            List<T1> expectedList = new List<T1>(first);
            expectedList.Sort(compare2);
            expectedList.Sort(compare2N);
            IEnumerable<T1> expectedSorted = expectedList;
            List<T1> actualList = new List<T1>(second);
            actualList.Sort(compare2);
            actualList.Sort(compare2N);
            IEnumerable<T1> actualSorted = actualList;
            return expectedSorted.SequenceEqual(actualSorted, comparer);
        }

        /// <summary>
        /// Process the list of Expected values - removes attributes, nulls, concatenates text nodes
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<ExpectedValue> ProcessNodes(this IEnumerable<ExpectedValue> values)
        {
            IEnumerator<ExpectedValue> e = values.GetEnumerator();
            if (e.MoveNext())
            {
                ExpectedValue concatenator = null;

                do
                {
                    if (e.Current.Data == null) continue;
                    if (e.Current.Data is XAttribute) continue;
                    if ((e.Current.Data is string) && ((e.Current.Data as string) == string.Empty)) continue;
                    if (e.Current.Data is XNode)
                    {
                        if (concatenator != null)
                        {
                            yield return concatenator;
                            concatenator = null;
                        }

                        if (e.Current.Data is XText && !(e.Current.Data is XCData))
                        {
                            concatenator = new ExpectedValue(e.Current.IsOriginalNode, new XText(e.Current.Data as XText), e.Current.IsOriginalNode ? e.Current.Data as XText : null);
                            continue;
                        }

                        yield return e.Current;
                    }
                    else
                    {
                        if (concatenator != null)
                        {
                            (concatenator.Data as XText).Value += e.Current.Data.ToString();  // problem here!!!
                        }
                        else
                        {
                            concatenator = new ExpectedValue(false, new XText(e.Current.Data.ToString()));
                        }
                    }
                } while (e.MoveNext());

                if (concatenator != null) yield return concatenator;
            }
        }

        public static void Verify(this XAttribute at)
        {
            TestLog.Compare(at.NodeType, XmlNodeType.Attribute, "nodetype for attribute");
            TestLog.Compare(at.ToString() != null, "cannot serialize");
            if (at.PreviousAttribute != null)
            {
                TestLog.Compare(at.PreviousAttribute.NextAttribute == at, "previous->next consistency");
            }
            if (at.NextAttribute != null)
            {
                TestLog.Compare(at.NextAttribute.PreviousAttribute == at, "next->previous consistency");
            }
            if (at.Parent != null)
            {
                XElement parent = at.Parent;
                bool found = false;
                for (XAttribute a = parent.FirstAttribute; a != null && !found; a = a.NextAttribute)
                {
                    found = (a == at);
                }
                TestLog.Compare(found, "iteration - attributes");
                TestLog.Compare(parent.Attribute(at.Name) == at, "attribute axis");
            }
            if (at.IsNamespaceDeclaration && !at.Name.LocalName.Equals("xmlns"))
                TestLog.Compare(at.Name.Namespace == XNamespace.Xmlns, "namespace decl");
            else if (at.IsNamespaceDeclaration && at.Name.LocalName.Equals("xmlns"))
                TestLog.Compare(at.Name.Namespace == XNamespace.None, "namespace decl");
        }

        public static void Verify(this XElement elem)
        {
            TestLog.Compare(elem.NodeType, XmlNodeType.Element, "nodetype for element");
            bool hasElements = false;
            for (XNode n = elem.FirstNode; n != null; n = n.NextNode)
            {
                TestLog.Compare(n.Parent, elem, "parent property");
                TestLog.Compare(n.Document, elem.Document, "document property");
                if (n is XElement) hasElements = true;
            }

            for (XAttribute n = elem.FirstAttribute; n != null; n = n.NextAttribute)
            {
                TestLog.Compare(n.Parent, elem, "parent property");
            }

            TestLog.Compare(elem.NodeType, XmlNodeType.Element, "NodeType");
            TestLog.Compare(elem.HasAttributes, elem.FirstAttribute != null, "HasAttributes");
            TestLog.Compare(elem.HasElements, hasElements, "HasElements");
            TestLog.Compare(elem.ToString(SaveOptions.DisableFormatting) != null, "cannot serialize"); // just checking that the exception is not thrown

            if (elem.IsEmpty)
            {
                TestLog.Compare(elem.FirstNode == null, "IsEmpty has nodes");
                TestLog.Compare(!elem.ToString(SaveOptions.DisableFormatting).Contains("></"), "IsEmpty and serialization (true case)");
            }
            else
            {
                if (elem.FirstNode == null)
                {
                    TestLog.Compare(elem.ToString(SaveOptions.DisableFormatting).Contains("></"), "IsEmpty and serialization (false case)");
                }
            }

            if (elem.PreviousNode != null)
            {
                TestLog.Compare(elem.PreviousNode.NextNode == elem, "previous->next consistency");
            }
            if (elem.NextNode != null)
            {
                TestLog.Compare(elem.NextNode.PreviousNode == elem, "next->previous consistency");
            }

            if (elem.Parent != null)
            {
                XElement parent = elem.Parent;
                bool found = false;
                for (XNode a = parent.FirstNode; a != null && !found; a = a.NextNode)
                {
                    found = (a == elem);
                }
                TestLog.Compare(found, "iteration - nodes");
                TestLog.Compare(parent.Element(elem.Name) == elem, "element axis");
            }
        }

        public static bool EqualsAll<T1, T2>(this IEnumerable<T1> source, IEnumerable<T2> target, Func<T1, T2, bool> comparer)
        {
            using (IEnumerator<T1> e1 = source.GetEnumerator())
            using (IEnumerator<T2> e2 = target.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    if (e2.MoveNext())
                    {
                        if (!comparer(e1.Current, e2.Current))
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                if (e2.MoveNext())
                    return false;
            }
            return true;
        }

        public static XAttributeEqualityComparer<XAttribute> MyAttributeComparer = new XAttributeEqualityComparer<XAttribute>();

        public static IEnumerable<T> InsertNulls<T>(this IEnumerable<T> source, int howoften)
        {
            Debug.Assert(howoften > 0);
            int counter = 0;
            foreach (T t in source)
            {
                if (counter++ % howoften == 0) yield return default(T);
                yield return t;
            }
        }

        public static bool CheckDTDAfterElement(this IEnumerable<object> newNodes)
        {
            bool wasElement = false;
            foreach (object o in newNodes)
            {
                if (o is XElement) wasElement = true;
                if (o is XDocumentType && wasElement) return true;
            }
            return false;
        }

        public static bool IsXDocValid(this IEnumerable<ExpectedValue> expectedNodes)
        {
            bool shouldFail = false;
            var nn = expectedNodes.Select(n => n.Data).ToList();
            bool[] flags = new bool[] {
                    nn.Where(x=>x is XElement).Count() > 1,
                    nn.Where(x=>x is XDocumentType).Count() > 1,
                    nn.Where(x=>(x is XText)).Select(x=>(x as XText).Value.ToCharArray()).Where(x => x.Where(y=> y!= ' '&&y!='\n'&&y!='\t').Any()).Any(),
                    nn.Where(x=>(x is string)).Select(x=>(x as string).ToCharArray()).Where(x => x.Where(y=> y!= ' '&&y!='\n'&&y!='\t').Any()).Any(),
                    nn.Where(x=>x is XCData).Any(),
                    nn.CheckDTDAfterElement()};

            foreach (bool b in flags) shouldFail |= b;
            return shouldFail;
        }
    }



    public class XAttributeEqualityComparer<T> : IEqualityComparer, IEqualityComparer<T> where T : XAttribute
    {
        public static XAttributeEqualityComparer<T> GetInstance()
        {
            return new XAttributeEqualityComparer<T>();
        }

        public bool Equals(T n1, T n2)
        {
            Debug.Assert(n1 != null && n2 != null && n1.Value != null && n2.Value != null);
            return n1.Name.Equals(n2.Name) && n1.Value.Equals(n2.Value);
        }

        public int GetHashCode(T attr)
        {
            Debug.Assert(attr != null && attr.Value != null);
            return attr.Value.GetHashCode() ^ attr.Name.GetHashCode();
        }

        bool IEqualityComparer.Equals(object n1, object n2)
        {
            return Equals((T)n1, (T)n2);
        }

        int IEqualityComparer.GetHashCode(object n)
        {
            return GetHashCode((T)n);
        }
    }

    public class ExpectedValue
    {
        private bool _isOriginalNode;
        private object _data;

        public object Data { get { return _data; } }
        public bool IsOriginalNode { get { return _isOriginalNode; } }
        public XText originalReference; // it is used for checking the text node identities

        public ExpectedValue(bool isOriginal, object data)
            : this(isOriginal, data, null)
        {
        }

        public ExpectedValue(bool isOriginal, object data, XText origRef)
        {
            _data = data;
            _isOriginalNode = isOriginal;
            this.originalReference = origRef;
        }
    }

    public class XNodeStructuralEqualityComparer<T> : XNodeBaseEqualityComparer<T> where T : XObject
    {
        public override bool CompareContent(XContainer n1, XContainer n2)
        {
            XNode xn1, xn2;
            xn1 = (n1 as XContainer).FirstNode;
            xn2 = (n2 as XContainer).FirstNode;
            while (xn1 != null && xn2 != null)
            {
                if (!Equals(xn1, xn2)) return false;
                xn1 = xn1.NextNode;
                xn2 = xn2.NextNode;
            }
            if (xn1 != null || xn2 != null) return false;
            return true;
        }
        public override bool CompareAttributes(XElement n1, XElement n2)
        {
            XAttribute xa1, xa2;
            xa1 = (n1 as XElement).FirstAttribute;
            xa2 = (n2 as XElement).FirstAttribute;
            while (xa1 != null && xa2 != null)
            {
                if (!Equals(xa1, xa2)) return false;
                xa1 = xa1.NextAttribute;
                xa2 = xa2.NextAttribute;
            }
            if (xa1 != null || xa2 != null) return false;
            return true;
        }

        public override int ComputeHash4Content(XContainer n, int startHash)
        {
            foreach (XNode m in n.Nodes()) startHash ^= CalculateHashCode(m);
            return startHash;
        }

        public override int ComputeHash4Attributes(XElement n, int startHash)
        {
            foreach (XAttribute a in n.Attributes()) startHash ^= CalculateHashCode(a);
            return startHash;
        }
    }

    public abstract class XNodeBaseEqualityComparer<T> : IEqualityComparer, IEqualityComparer<T> where T : XObject
    {
        public abstract bool CompareContent(XContainer n1, XContainer n2);
        public abstract bool CompareAttributes(XElement n1, XElement n2);
        public abstract int ComputeHash4Content(XContainer n, int startHash);
        public abstract int ComputeHash4Attributes(XElement n, int startHash);

        public bool Equals(T n1, T n2)
        {
            if (n1 == null && n2 == null) return true;
            if (n1 == null || n2 == null) return false;
            if (n1.GetType() != n2.GetType()) return false;

            if (n1 is XElement)
            {
                XElement e1 = n1 as XElement;
                XElement e2 = n2 as XElement;
                return CompareContent(e1, e2) & CompareAttributes(e1, e2);
            }
            if (n1 is XDocument)
            {
                return CompareContent(n1 as XDocument, n2 as XDocument);
            }
            if (n1 is XNode)
            {
                return ((n1 as XNode).ToString(SaveOptions.DisableFormatting) == (n2 as XNode).ToString(SaveOptions.DisableFormatting));
            }
            if (n1 is XAttribute)
            {
                XAttribute a1 = n1 as XAttribute;
                XAttribute a2 = n2 as XAttribute;
                return (a1.Name == a2.Name && a1.Value == a2.Value);
            }

            return false;
        }

        public int GetHashCode(T t)
        {
            return CalculateHashCode(t as XObject);
        }

        public int CalculateHashCode(XObject t)
        {
            if (t == null) return 0;
            switch ((t as XObject).NodeType)
            {
                case XmlNodeType.Attribute:
                    XAttribute xa = t as XAttribute;
                    return xa.Name.GetHashCode() ^ xa.Value.GetHashCode();
                case XmlNodeType.CDATA:
                case XmlNodeType.Text:
                    return (t as XText).Value.GetHashCode();
                case XmlNodeType.Comment:
                    return (t as XComment).Value.GetHashCode();
                case XmlNodeType.ProcessingInstruction:
                    XProcessingInstruction pi = t as XProcessingInstruction;
                    return (pi.Data).GetHashCode() ^ (pi.Target.GetHashCode());
                case XmlNodeType.DocumentType:
                    XDocumentType dt = t as XDocumentType;
                    return dt.ToString(SaveOptions.DisableFormatting).GetHashCode();
                case XmlNodeType.Document:
                    return ComputeHash4Content(t as XDocument, 0);
                case XmlNodeType.Element:
                    XElement e = t as XElement;
                    return ComputeHash4Attributes(e, ComputeHash4Content(e, e.Name.GetHashCode()));
                default:
                    TestLog.Compare(false, "Not supported node type");
                    return 0;
            }
        }

        bool IEqualityComparer.Equals(object n1, object n2)
        {
            return Equals((T)n1, (T)n2);
        }

        int IEqualityComparer.GetHashCode(object n)
        {
            return GetHashCode((T)n);
        }
    }

    public static class ReaderDiff
    {
        public static void Compare(System.IO.Stream s1, System.IO.Stream s2)
        {
            using (XmlReader r1 = XmlReader.Create(s1))
            {
                using (XmlReader r2 = XmlReader.Create(s2))
                {
                    Compare(r1, r2);
                }
            }
        }

        public static void Compare(string str1, string str2)
        {
            using (XmlReader r1 = XmlReader.Create(new System.IO.StringReader(str1)))
            using (XmlReader r2 = XmlReader.Create(new System.IO.StringReader(str2)))
                Compare(r1, r2);
        }

        public static void Compare(XmlReader r1, XmlReader r2)
        {
            while (true)
            {
                bool r1Read = r1.Read();
                TestLog.Compare(r1Read, r2.Read(), "Read() out of sync");
                if (!r1Read) break;
                if (r2.NodeType != XmlNodeType.Text)
                {
                    TestLog.Compare(r1.NodeType, r2.NodeType, "nodeType");
                    TestLog.Compare(r1.LocalName, r2.LocalName, "localname");
                    TestLog.Compare(r1.NamespaceURI, r2.NamespaceURI, "Namespaceuri");
                }
                else
                {
                    TestLog.Compare(r1.Value, r2.Value, "localname");
                }
                TestLog.Compare(r1.HasAttributes, r2.HasAttributes, "hasAttributes");
                if (r1.HasAttributes)
                {
                    TestLog.Compare(r1.MoveToFirstAttribute(), r2.MoveToFirstAttribute(), "MoveToFirstAttribute");
                    while (true)
                    {
                        TestLog.Compare(r1.NodeType, r2.NodeType, "attr: nodeType");
                        TestLog.Compare(r1.LocalName, r2.LocalName, "attr: localname");
                        TestLog.Compare(r1.NamespaceURI, r2.NamespaceURI, "attr: Namespaceuri");
                        TestLog.Compare(r1.Value, r2.Value, "attr: localname");
                        bool moved = r1.MoveToNextAttribute();
                        TestLog.Compare(moved, r2.MoveToNextAttribute(), "MoveToNextAttribute()");
                        if (!moved) break;
                    }
                }
            }
        }
    }
}
