// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Tests.LegacyTests
{
    public static class Extension
    {
        /// <summary>
        /// Verify whether the ArgumentException is thrown due to the expected argument input being invalid
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="expected"></param>
        /// <returns></returns>
        public static bool CompareParamName(this ArgumentException ex, string expected)
        {
#if SILVERLIGHT
#if SLRESOURCES
        return ex.Message.Substring(ex.Message.LastIndexOf(": ") + 2) == expected;
#else
        return true;
#endif
#else
            return ex.ParamName == expected;
#endif
        }

        /// <summary>
        /// Verify whether the ArgumentNullException is thrown due to the expected argument input being null
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="expected"></param>
        /// <returns></returns>
        public static bool CompareParamName(this ArgumentNullException ex, string expected)
        {
#if SILVERLIGHT
#if SLRESOURCES
        return ex.Message.Substring(ex.Message.LastIndexOf(": ") + 2) == expected;
#else
        return true;
#endif
#else
            return ex.ParamName == expected;
#endif
        }
    }

    public static class Verification
    {
        public static int Allequal(string expected, string actual)
        {
            return string.Compare(expected, actual, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Compare two IEnumerable to see whether they contain the same data (orderless and use the default EqualityComparer)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <returns>return 0 if the two sets are equal, otherwise return 1</returns>
        /// <remarks>The order of the elements doesn't really matter when PLINQ team runs these tests.
        ///          The following verification functions will be used when PLINQ team runs our tests.
        ///          These verification functios ignores the order of the elements.</remarks>
        public static int Allequal<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            return AllequalComparer(expected, actual, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Compare two IEnumerable to see whether they contain the same data (orderless and use the specific IEqualityComparer)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="comparer"></param>
        /// <returns>return 0 if the two sets are equal, otherwise return 1</returns>
        /// <remarks>The order of the elements doesn't really matter when PLINQ team runs these tests.
        ///          The following verification functions will be used when PLINQ team runs our tests.
        ///          These verification functios ignores the order of the elements.</remarks>    
        public static int AllequalComparer<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer)
        {
            if ((expected == null) && (actual == null)) return 0;

            if ((expected == null) || (actual == null))
            {
                Console.WriteLine("expected : {0}", expected == null ? "null" : expected.Count().ToString());
                Console.WriteLine("actual: {0}", actual == null ? "null" : actual.Count().ToString());
                return 1;
            }

            try
            {
                List<T> contents = new List<T>(expected);
                foreach (T e in actual)
                {
                    for (int i = 0; i < contents.Count; i++)
                    {
                        if (comparer.Equals(contents[i], e))
                        {
                            contents.RemoveAt(i);
                            break;
                        }
                    }
                }

                return contents.Count == 0 ? 0 : 1;
            }
            catch (AggregateException ae)
            {
                var innerExceptions = ae.Flatten().InnerExceptions;
                if (innerExceptions.Where(ex => ex != null).Select(ex => ex.GetType()).Distinct().Count() == 1)
                {
                    throw innerExceptions.First();
                }
                else
                {
                    Console.WriteLine(ae);
                }
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Helper function to verify that all elements in dictionary are matched using the default EqualityComparer
        /// This verification function MatchAll is used by the GroupBy operator
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="key"></param>
        /// <param name="element"></param>
        /// <param name="result"></param>
        /// <returns>0 if matching, otherwise 1</returns>
        public static int MatchAll<K, E>(IEnumerable<K> key, IEnumerable<E> element, IEnumerable<System.Linq.IGrouping<K, E>> result)
        {
            return MatchAll(key, element, result, EqualityComparer<K>.Default);
        }

        /// <summary>
        /// Helper function to verify that all elements in dictionary are matched using the specific IEqualityComparer
        /// This verification function MatchAll is used by the GroupBy operator
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="key"></param>
        /// <param name="element"></param>
        /// <param name="result"></param>
        /// <returns>0 if matching, otherwise 1</returns>
        public static int MatchAll<K, E>(IEnumerable<K> key, IEnumerable<E> element, IEnumerable<System.Linq.IGrouping<K, E>> result, IEqualityComparer<K> keyComparer)
        {
            if ((result == null) && (element == null)) return 0;

            try
            {
                Dictionary<K, List<E>> dict = new Dictionary<K, List<E>>(keyComparer);
                List<E> groupingForNullKeys = new List<E>();
                using (IEnumerator<E> e1 = element.GetEnumerator())
                using (IEnumerator<K> k1 = key.GetEnumerator())
                {
                    while (e1.MoveNext() && k1.MoveNext())
                    {
                        K mkey1 = k1.Current;

                        if (mkey1 == null)
                        {
                            groupingForNullKeys.Add(e1.Current);
                        }
                        else
                        {
                            List<E> list;
                            if (!dict.TryGetValue(mkey1, out list))
                            {
                                list = new List<E>();
                                dict.Add(mkey1, list);
                            }
                            list.Add(e1.Current);
                        }
                    }
                }
                foreach (System.Linq.IGrouping<K, E> r1 in result)
                {
                    K mkey2 = r1.Key;
                    List<E> list;

                    if (mkey2 == null)
                    {
                        list = groupingForNullKeys;
                    }
                    else
                    {
                        if (!dict.TryGetValue(mkey2, out list)) return 1;
                        dict.Remove(mkey2);
                    }
                    foreach (E e1 in r1)
                    {
                        if (!list.Contains(e1)) return 1;
                        list.Remove(e1);
                    }
                }
                return 0;
            }
            catch (AggregateException ae)
            {
                var innerExceptions = ae.Flatten().InnerExceptions;
                if (innerExceptions.Where(ex => ex != null).Select(ex => ex.GetType()).Distinct().Count() == 1)
                {
                    throw innerExceptions.First();
                }
                else
                {
                    Console.WriteLine(ae);
                }
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    /// <summary>
    /// Some helpers for quick generation/examination of inputs during PLINQ testing 
    /// </summary>
    public static class Functions
    {
        public static bool IsEven(int num)
        {
            if (num % 2 == 0) return true;
            return false;
        }

        public static bool IsEmpty(string str)
        {
            if (String.IsNullOrEmpty(str)) return true;
            return false;
        }

        public static bool IsEven_Index(int num, int index)
        {
            if (num % 2 == 0) return true;
            return false;
        }

        public static IEnumerable<int> NumRange(int num, long count)
        {
            for (long i = 0; i < count; i++)
                yield return num;
        }

        public static IEnumerable<int> NumList(int start, int count)
        {
            for (int i = 0; i < count; i++)
                yield return start + i;
        }

        public static IEnumerable<int?> NullSeq(long num)
        {
            for (long i = 0; i < num; i++)
                yield return null;
        }

        public static IEnumerable<int> InfiniteNum()
        {
            for (; ;)
                yield return 2;
        }
    }
}