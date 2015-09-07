// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class GroupByTests
    {
        // Class which is passed as an argument for EqualityComparer
        public class AnagramEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return getCanonicalString(x) == getCanonicalString(y);
            }

            public int GetHashCode(string obj)
            {
                return getCanonicalString(obj).GetHashCode();
            }

            private string getCanonicalString(string word)
            {
                char[] wordChars = word.ToCharArray();
                Array.Sort<char>(wordChars);
                return new string(wordChars);
            }
        }

        public struct Record
        {
            public string Name;
            public int Score;
        }

        public class Helper
        {
            // Helper function-1 to act as resultSelector
            public static long resultSelect1(string key, IEnumerable<Record> elements)
            {
                long result = 0;
                foreach (Record element in elements)
                {
                    result = result + element.Score;
                }

                if (key != null)
                    result = result * key.Length;

                return result;
            }

            // Helper function-2 to act as resultSelector
            public static long resultSelect2(string key, IEnumerable<int> elements)
            {
                long result = 0;

                foreach (int element in elements)
                {
                    result = result + element;
                }

                if (key != null)
                    result = result * key.Length;

                return result;
            }
        }

        public class GroupBy011
        {
            private static int GroupBy001()
            {
                var q1 = from x1 in new string[] { "Alen", "Felix", null, null, "X", "Have Space", "Clinton", "" }
                         select x1; ;

                var q2 = from x2 in new int[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                         select x2;

                var q = from x3 in q1
                        from x4 in q2
                        select new { a1 = x3, a2 = x4 };

                var rst1 = q.GroupBy(e => e.a1, e => e.a2);
                var rst2 = q.GroupBy(e => e.a1, e => e.a2);

                return (null == rst2 ? 1 : 0);
                // return Verification.Allequal(rst1, rst2);
                // return Verification.MatchAll((IEnumerable<string>)q1, (IEnumerable<int>)q2, q);
            }

            public static int Main()
            {
                int ret = RunTest(GroupBy001);
                if (0 != ret)
                    Console.Write(s_errorMessage);

                return ret;
            }

            private static string s_errorMessage = String.Empty;
            private delegate int D();

            private static int RunTest(D m)
            {
                int n = m();
                if (0 != n)
                    s_errorMessage += m.ToString() + " - FAILED!\r\n";
                return n;
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy012
        {
            // DDB:171937
            public static int Test012()
            {
                string[] key = { null };
                string[] element = { null };
                string[] source = new string[] { null };

                var result = source.GroupBy((e) => e, (e) => e, EqualityComparer<string>.Default);

                return Verification.MatchAll(key, element, result, EqualityComparer<string>.Default);
            }


            public static int Main()
            {
                return Test012();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy1
        {
            // Overload-4: source is empty
            public static int Test1()
            {
                string[] key = { };
                int[] element = { };
                Record[] source = new Record[] { };

                var result = source.GroupBy((e) => e.Name, (e) => e.Score, new AnagramEqualityComparer());

                return Verification.MatchAll(key, element, result, new AnagramEqualityComparer());
            }


            public static int Main()
            {
                return Test1();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy10a
        {
            // Overload-8: source is null
            public static int Test10a()
            {
                string[] key = { "Tim", "Tim", "Chris", "Chris", "Robert", "Prakash" };
                int[] element = { 55, 25, 49, 24, -100, 9 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = 55},
                                                new Record{Name = "Chris", Score = 49},
                                                new Record{Name = "Robert", Score = -100},
                                                new Record{Name = "Chris", Score = 24},
                                                new Record{Name = "Prakash", Score = 9},
                                                new Record{Name = "Tim", Score = 25},
            };
                Func<string, IEnumerable<int>, long> res2 = Helper.resultSelect2;

                try
                {
                    source = null;
                    var result = source.GroupBy((e) => e.Name, (e) => e.Score, res2, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("source")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test10a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy10b
        {
            // Overload-8: keySelector is null
            public static int Test10b()
            {
                string[] key = { "Tim", "Tim", "Chris", "Chris", "Robert", "Prakash" };
                int[] element = { 55, 25, 49, 24, -100, 9 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = 55},
                                                new Record{Name = "Chris", Score = 49},
                                                new Record{Name = "Robert", Score = -100},
                                                new Record{Name = "Chris", Score = 24},
                                                new Record{Name = "Prakash", Score = 9},
                                                new Record{Name = "Tim", Score = 25},
            };
                Func<string, IEnumerable<int>, long> res2 = Helper.resultSelect2;

                try
                {
                    var result = source.GroupBy(null, (e) => e.Score, res2, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("keySelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test10b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy10c
        {
            // Overload-8: elementSelector is null
            public static int Test10c()
            {
                string[] key = { "Tim", "Tim", "Chris", "Chris", "Robert", "Prakash" };
                int[] element = { 55, 25, 49, 24, -100, 9 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = 55},
                                                new Record{Name = "Chris", Score = 49},
                                                new Record{Name = "Robert", Score = -100},
                                                new Record{Name = "Chris", Score = 24},
                                                new Record{Name = "Prakash", Score = 9},
                                                new Record{Name = "Tim", Score = 25},
            };
                Func<string, IEnumerable<int>, long> res2 = Helper.resultSelect2;

                try
                {
                    var result = source.GroupBy((e) => e.Name, null, res2, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("elementSelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test10c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy10d
        {
            // Overload-8: resultSelector is null
            public static int Test10d()
            {
                string[] key = { "Tim", "Tim", "Chris", "Chris", "Robert", "Prakash" };
                int[] element = { 55, 25, 49, 24, -100, 9 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = 55},
                                                new Record{Name = "Chris", Score = 49},
                                                new Record{Name = "Robert", Score = -100},
                                                new Record{Name = "Chris", Score = 24},
                                                new Record{Name = "Prakash", Score = 9},
                                                new Record{Name = "Tim", Score = 25},
            };
                Func<string, IEnumerable<int>, long> res2 = Helper.resultSelect2;

                try
                {
                    res2 = null;
                    var result = source.GroupBy((e) => e.Name, (e) => e.Score, res2, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("resultSelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test10d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy10e
        {
            // Overload-8: source is empty
            public static int Test10e()
            {
                string[] key = { };
                int[] element = { };
                Record[] source = new Record[] { };
                Func<string, IEnumerable<int>, long> res2 = Helper.resultSelect2;
                long[] expected = { };

                var result = source.GroupBy((e) => e.Name, (e) => e.Score, res2, new AnagramEqualityComparer());

                return Verification.Allequal(expected, result);
            }


            public static int Main()
            {
                return Test10e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy10f
        {
            // Overload-8: Duplicate keys are present
            // Also tests Equality Comparer is called
            public static int Test10f()
            {
                string[] key = { "Tim", "Tim", "Chris", "Chris", "Robert", "Prakash" };
                int[] element = { 55, 25, 49, 24, -100, 9 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = 55},
                                                new Record{Name = "Chris", Score = 49},
                                                new Record{Name = "Robert", Score = -100},
                                                new Record{Name = "Chris", Score = 24},
                                                new Record{Name = "Prakash", Score = 9},
                                                new Record{Name = "miT", Score = 25},
            };
                Func<string, IEnumerable<int>, long> res2 = Helper.resultSelect2;
                long[] expected = { 240, 365, -600, 63 };

                var result = source.GroupBy((e) => e.Name, (e) => e.Score, res2, new AnagramEqualityComparer());

                return Verification.Allequal(expected, result);
            }


            public static int Main()
            {
                return Test10f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy10g
        {
            // Overload-8: Verifies Equality Comparer is null scenario, some of the key elements are also null
            public static int Test10g()
            {
                string[] key = { "Tim", null, null, "Robert", "Chris", "miT" };
                int[] element = { 55, 49, 9, -100, 24, 25 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = 55},
                                                new Record{Name = null, Score = 49},
                                                new Record{Name = "Robert", Score = -100},
                                                new Record{Name = "Chris", Score = 24},
                                                new Record{Name = null, Score = 9},
                                                new Record{Name = "miT", Score = 25},
            };
                Func<string, IEnumerable<int>, long> res2 = Helper.resultSelect2;
                long[] expected = { 165, 58, -600, 120, 75 };

                var result = source.GroupBy((e) => e.Name, (e) => e.Score, res2, null);

                return Verification.Allequal(expected, result);
            }


            public static int Main()
            {
                return Test10g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy2
        {
            // Overload-1: source has only one element
            public static int Test2()
            {
                string[] key = { "Tim" };
                int[] element = { 60 };
                Record[] source = new Record[] { new Record { Name = key[0], Score = element[0] } };

                var result = source.GroupBy((e) => e.Name);

                return Verification.MatchAll(key, source, result);
            }


            public static int Main()
            {
                return Test2();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy3
        {
            // Overload-2: source has all elements with same key.
            // Also verifies comparer function is called.
            public static int Test3()
            {
                // Key anagram of Tim
                string[] key = { "Tim", "Tim", "Tim", "Tim" };
                int[] element = { 60, -10, 40, 100 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = element[0]},
                                                new Record{Name = "Tim", Score = element[1]},
                                                new Record{Name = "miT", Score = element[2]},
                                                new Record{Name = "miT", Score = element[3]},
            };

                var result = source.GroupBy((e) => e.Name, new AnagramEqualityComparer());

                return Verification.MatchAll(key, source, result, new AnagramEqualityComparer());
            }


            public static int Main()
            {
                return Test3();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy4
        {
            // Overload-3: source has all elements with different keys.
            // Also verifies elementSelector is called
            public static int Test4()
            {
                string[] key = { "Tim", "Chris", "Robert", "Prakash" };
                int[] element = { 60, -10, 40, 100 };
                Record[] source = new Record[]{    new Record{Name = key[0], Score = element[0]},
                                                new Record{Name = key[1], Score = element[1]},
                                                new Record{Name = key[2], Score = element[2]},
                                                new Record{Name = key[3], Score = element[3]},
            };

                var result = source.GroupBy((e) => e.Name, (e) => e.Score);

                return Verification.MatchAll(key, element, result);
            }


            public static int Main()
            {
                return Test4();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy5
        {
            // Overload-4: Duplicate keys are present
            public static int Test5()
            {
                string[] key = { "Tim", "Tim", "Chris", "Chris", "Robert", "Prakash" };
                int[] element = { 55, 25, 49, 24, -100, 9 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = 55},
                                                new Record{Name = "Chris", Score = 49},
                                                new Record{Name = "Robert", Score = -100},
                                                new Record{Name = "Chris", Score = 24},
                                                new Record{Name = "Prakash", Score = 9},
                                                new Record{Name = "Tim", Score = 25},
            };

                var result = source.GroupBy((e) => e.Name, (e) => e.Score);

                return Verification.MatchAll(key, element, result);
            }


            public static int Main()
            {
                return Test5();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy6
        {
            // Overload-4: Duplicate elements are present
            // some of the key elements are null
            public static int Test6()
            {
                // Key anagram of Tim
                string[] key = { null, null, "Chris", "Chris", "Prakash", "Prakash" };
                int[] element = { 55, 25, 49, 24, 9, 9 };
                Record[] source = new Record[]{    new Record{Name = null, Score = 55},
                                                new Record{Name = "Chris", Score = 49},
                                                new Record{Name = "Prakash", Score = 9},
                                                new Record{Name = "Chris", Score = 24},
                                                new Record{Name = "Prakash", Score = 9},
                                                new Record{Name = null, Score = 25},
            };

                var result = source.GroupBy((e) => e.Name, (e) => e.Score);

                return Verification.MatchAll(key, element, result);
            }


            public static int Main()
            {
                return Test6();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy7a
        {
            // Overload-5: source is null
            public static int Test7a()
            {
                string[] key = { "Tim" };
                int[] element = { 60 };
                Record[] source = new Record[] { new Record { Name = key[0], Score = element[0] } };
                Func<string, IEnumerable<Record>, long> res1 = Helper.resultSelect1;

                try
                {
                    source = null;
                    var result = source.GroupBy((e) => e.Name, res1);
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("source")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test7a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy7b
        {
            // Overload-5: keySelector is null
            public static int Test7b()
            {
                string[] key = { "Tim" };
                int[] element = { 60 };
                Record[] source = new Record[] { new Record { Name = key[0], Score = element[0] } };
                Func<string, IEnumerable<Record>, long> res1 = Helper.resultSelect1;

                try
                {
                    var result = source.GroupBy(null, res1);
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("keySelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test7b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy7c
        {
            // Overload-5: resultSelector is null
            public static int Test7c()
            {
                string[] key = { "Tim" };
                int[] element = { 60 };
                Record[] source = new Record[] { new Record { Name = key[0], Score = element[0] } };
                Func<string, IEnumerable<Record>, long> res1 = Helper.resultSelect1;

                try
                {
                    res1 = null;
                    var result = source.GroupBy((e) => e.Name, res1);
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("resultSelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test7c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy7d
        {
            // Overload-5: source has only one element
            public static int Test7d()
            {
                string[] key = { "Tim" };
                int[] element = { 60 };
                Record[] source = new Record[] { new Record { Name = key[0], Score = element[0] } };
                Func<string, IEnumerable<Record>, long> res1 = Helper.resultSelect1;
                long[] expected = { 180 };

                var result = source.GroupBy((e) => e.Name, res1);

                return Verification.Allequal(expected, result);
            }


            public static int Main()
            {
                return Test7d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy7e
        {
            // Overload-5: Test to verify that ApplyResultSelector function resizes the grouping to remove empty elements
            // DDB: 58085
            public static int Test7e()
            {
                char[] elements = { 'q', 'q', 'q', 'q', 'q' };

                var result = elements.GroupBy((e) => (e), (e, f) => new { Key = e, Element = f });

                foreach (var e in result)
                {
                    if (e.Element.Count() != 5) return 1;
                    if (e.Key != 'q') return 1;
                    foreach (var ele in e.Element)
                        if (ele != 'q') return 1;
                }

                return 0;
            }


            public static int Main()
            {
                return Test7e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy8a
        {
            // Overload-6: source is null
            public static int Test8a()
            {
                string[] key = { "Tim", "Chris", "Robert", "Prakash" };
                int[] element = { 60, -10, 40, 100 };
                Record[] source = new Record[]{    new Record{Name = key[0], Score = element[0]},
                                                new Record{Name = key[1], Score = element[1]},
                                                new Record{Name = key[2], Score = element[2]},
                                                new Record{Name = key[3], Score = element[3]},
            };
                Func<string, IEnumerable<int>, long> res2 = Helper.resultSelect2;

                try
                {
                    source = null;
                    var result = source.GroupBy((e) => e.Name, (e) => e.Score, res2);
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("source")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test8a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy8b
        {
            // Overload-6: keySelector is null
            public static int Test8b()
            {
                string[] key = { "Tim", "Chris", "Robert", "Prakash" };
                int[] element = { 60, -10, 40, 100 };
                Record[] source = new Record[]{    new Record{Name = key[0], Score = element[0]},
                                                new Record{Name = key[1], Score = element[1]},
                                                new Record{Name = key[2], Score = element[2]},
                                                new Record{Name = key[3], Score = element[3]},
            };
                Func<string, IEnumerable<int>, long> res2 = Helper.resultSelect2;

                try
                {
                    var result = source.GroupBy(null, (e) => e.Score, res2);
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("keySelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test8b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy8c
        {
            // Overload-6: elementSelector is null
            public static int Test8c()
            {
                string[] key = { "Tim", "Chris", "Robert", "Prakash" };
                int[] element = { 60, -10, 40, 100 };
                Record[] source = new Record[]{    new Record{Name = key[0], Score = element[0]},
                                                new Record{Name = key[1], Score = element[1]},
                                                new Record{Name = key[2], Score = element[2]},
                                                new Record{Name = key[3], Score = element[3]},
            };
                Func<string, IEnumerable<int>, long> res2 = Helper.resultSelect2;

                try
                {
                    var result = source.GroupBy((e) => e.Name, null, res2);
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("elementSelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test8c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy8d
        {
            // Overload-6: resultSelector is null
            public static int Test8d()
            {
                string[] key = { "Tim", "Chris", "Robert", "Prakash" };
                int[] element = { 60, -10, 40, 100 };
                Record[] source = new Record[]{    new Record{Name = key[0], Score = element[0]},
                                                new Record{Name = key[1], Score = element[1]},
                                                new Record{Name = key[2], Score = element[2]},
                                                new Record{Name = key[3], Score = element[3]},
            };
                Func<string, IEnumerable<int>, long> res2 = Helper.resultSelect2;

                try
                {
                    res2 = null;
                    var result = source.GroupBy((e) => e.Name, (e) => e.Score, res2);
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("resultSelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test8d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy8e
        {
            // Overload-6: source has all elements with different keys.
            // Also verifies elementSelector is called
            public static int Test8e()
            {
                string[] key = { "Tim", "Chris", "Robert", "Prakash" };
                int[] element = { 60, -10, 40, 100 };
                Record[] source = new Record[]{    new Record{Name = key[0], Score = element[0]},
                                                new Record{Name = key[1], Score = element[1]},
                                                new Record{Name = key[2], Score = element[2]},
                                                new Record{Name = key[3], Score = element[3]},
            };
                Func<string, IEnumerable<int>, long> res2 = Helper.resultSelect2;
                long[] expected = { 180, -50, 240, 700 };

                var result = source.GroupBy((e) => e.Name, (e) => e.Score, res2);

                return Verification.Allequal(expected, result);
            }


            public static int Main()
            {
                return Test8e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy9a
        {
            // Overload-7: source is null
            public static int Test9a()
            {
                // Key anagram of Tim
                string[] key = { "Tim", "Tim", "Tim", "Tim" };
                int[] element = { 60, -10, 40, 100 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = element[0]},
                                                new Record{Name = "Tim", Score = element[1]},
                                                new Record{Name = "miT", Score = element[2]},
                                                new Record{Name = "miT", Score = element[3]},
            };
                Func<string, IEnumerable<Record>, long> res1 = Helper.resultSelect1;

                try
                {
                    source = null;
                    var result = source.GroupBy((e) => e.Name, res1, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("source")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test9a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy9b
        {
            // Overload-7: keySelector is null
            public static int Test9b()
            {
                // Key anagram of Tim
                string[] key = { "Tim", "Tim", "Tim", "Tim" };
                int[] element = { 60, -10, 40, 100 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = element[0]},
                                                new Record{Name = "Tim", Score = element[1]},
                                                new Record{Name = "miT", Score = element[2]},
                                                new Record{Name = "miT", Score = element[3]},
            };
                Func<string, IEnumerable<Record>, long> res1 = Helper.resultSelect1;

                try
                {
                    var result = source.GroupBy(null, res1, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("keySelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test9b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy9c
        {
            // Overload-7: resultSelector is null
            public static int Test9c()
            {
                // Key anagram of Tim
                string[] key = { "Tim", "Tim", "Tim", "Tim" };
                int[] element = { 60, -10, 40, 100 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = element[0]},
                                                new Record{Name = "Tim", Score = element[1]},
                                                new Record{Name = "miT", Score = element[2]},
                                                new Record{Name = "miT", Score = element[3]},
            };
                Func<string, IEnumerable<Record>, long> res1 = Helper.resultSelect1;

                try
                {
                    res1 = null;
                    var result = source.GroupBy((e) => e.Name, res1, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (ane.CompareParamName("resultSelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test9c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy9d
        {
            // Overload-7: source has all elements with same key
            // Also verifies comparer function is called.
            public static int Test9d()
            {
                // Key anagram of Tim
                string[] key = { "Tim", "Tim", "Tim", "Tim" };
                int[] element = { 60, -10, 40, 100 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = element[0]},
                                                new Record{Name = "Tim", Score = element[1]},
                                                new Record{Name = "miT", Score = element[2]},
                                                new Record{Name = "miT", Score = element[3]},
            };
                Func<string, IEnumerable<Record>, long> res1 = Helper.resultSelect1;
                long[] expected = { 570 };

                var result = source.GroupBy((e) => e.Name, res1, new AnagramEqualityComparer());

                return Verification.Allequal(expected, result);
            }


            public static int Main()
            {
                return Test9d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupBy9e
        {
            // Overload-7: Verifies comparer is null does not cause any exception
            public static int Test9e()
            {
                // Key anagram of Tim
                string[] key = { "Tim", "Tim", "Tim", "Tim" };
                int[] element = { 60, -10, 40, 100 };
                Record[] source = new Record[]{    new Record{Name = "Tim", Score = element[0]},
                                                new Record{Name = "Tim", Score = element[1]},
                                                new Record{Name = "miT", Score = element[2]},
                                                new Record{Name = "miT", Score = element[3]},
            };
                Func<string, IEnumerable<Record>, long> res1 = Helper.resultSelect1;
                long[] expected = { 150, 420 };

                var result = source.GroupBy((e) => e.Name, res1, null);

                return Verification.Allequal(expected, result);
            }


            public static int Main()
            {
                return Test9e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
        
        [Fact]
        public void GroupingToArray()
        {
            Record[] source = new Record[]
            {
                new Record{ Name = "Tim", Score = 55 },
                new Record{ Name = "Chris", Score = 49 },
                new Record{ Name = "Robert", Score = -100 },
                new Record{ Name = "Chris", Score = 24 },
                new Record{ Name = "Prakash", Score = 9 },
                new Record{ Name = "Tim", Score = 25 }
            };
            
            IGrouping<string, Record>[] groupedArray = source.GroupBy(r => r.Name).ToArray();
            Assert.Equal(4, groupedArray.Length);
            Assert.Equal(source.GroupBy(r => r.Name), groupedArray);
        }

        [Fact]
        public void GroupingWithResultsToArray()
        {
            Record[] source = new Record[]
            {
                new Record{ Name = "Tim", Score = 55 },
                new Record{ Name = "Chris", Score = 49 },
                new Record{ Name = "Robert", Score = -100 },
                new Record{ Name = "Chris", Score = 24 },
                new Record{ Name = "Prakash", Score = 9 },
                new Record{ Name = "Tim", Score = 25 }
            };

            IEnumerable<Record>[] groupedArray = source.GroupBy(r => r.Name, (r, e) => e).ToArray();
            Assert.Equal(4, groupedArray.Length);
            Assert.Equal(source.GroupBy(r => r.Name, (r, e) => e), groupedArray);
        }

    }
}
