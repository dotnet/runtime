// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class UnionTests
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
        public class Union012
        {
            private static int Union001()
            {
                var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                         select x1;
                var q2 = from x2 in new int?[] { 1, 9, null, 4 }
                         select x2;

                var rst1 = q1.Union(q2);
                var rst2 = q1.Union(q2);

                return Verification.Allequal(rst1, rst2);
            }

            private static int Union002()
            {
                var q1 = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                         select x1;
                var q2 = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                         select x2;

                var rst1 = q1.Union(q2);
                var rst2 = q1.Union(q2);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(Union001) + RunTest(Union002);
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

        public class Union1
        {
            // first is empty and second is empty
            public static int Test1()
            {
                int[] first = { };
                int[] second = { };
                int[] expected = { };

                var actual = first.Union(second);
                return Verification.Allequal(expected, actual);
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

        public class Union10
        {
            // Overload-2: Test when EqualityComparer is not-null
            // This test calls the AllequalComparer function to perform the verification
            // This will help the PLINQ team to run these tests, since "Tim", "miT" are the same when the approriate comparer is passed
            public static int Test10()
            {
                string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
                string[] second = { "ttaM", "Charlie", "Bbo" };
                string[] expected = { "Bob", "Robert", "Tim", "Matt", "Charlie" };

                var actual = first.Union(second, new AnagramEqualityComparer());
                return Verification.AllequalComparer(expected, actual, new AnagramEqualityComparer());
            }


            public static int Main()
            {
                return Test10();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Union11a
        {
            // Overload-2: first is null
            public static int Test11a()
            {
                string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
                string[] second = { "ttaM", "Charlie", "Bbo" };

                try
                {
                    first = null;
                    var actual = first.Union(second, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (!ane.CompareParamName("first")) return 1;
                    return 0;
                }
            }


            public static int Main()
            {
                return Test11a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Union11b
        {
            // Overload-2: second is null
            public static int Test11b()
            {
                string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
                string[] second = { "ttaM", "Charlie", "Bbo" };

                try
                {
                    second = null;
                    var actual = first.Union(second, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (!ane.CompareParamName("second")) return 1;
                    return 0;
                }
            }


            public static int Main()
            {
                return Test11b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Union13
        {
            // DDB:171937
            public static int Test13()
            {
                string[] first = { null };
                string[] second = new string[0];
                string[] expected = { null };

                var actual = first.Union(second, EqualityComparer<string>.Default);
                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test13();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Union14
        {
            // DDB:171937
            public static int Test13()
            {
                string[] first = { null, null, string.Empty };
                string[] second = { null, null };
                string[] expected = { null, string.Empty };

                var actual = first.Union(second, EqualityComparer<string>.Default);
                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test13();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Union15
        {
            // DDB:171937
            public static int Test13()
            {
                string[] first = { null, null };
                string[] second = new string[0];
                string[] expected = { null };

                var actual = first.Union(second, EqualityComparer<string>.Default);
                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test13();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Union2
        {
            // first is empty and second is non-empty
            public static int Test2()
            {
                int[] first = { };
                int[] second = { 2, 4, 5, 3, 2, 3, 9 };
                int[] expected = { 2, 4, 5, 3, 9 };

                var actual = first.Union(second);
                return Verification.Allequal(expected, actual);
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

        public class Union3
        {
            // first is non-empty and second is empty
            public static int Test3()
            {
                int[] first = { 2, 4, 5, 3, 2, 3, 9 };
                int[] second = { };
                int[] expected = { 2, 4, 5, 3, 9 };

                var actual = first.Union(second);
                return Verification.Allequal(expected, actual);
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

        public class Union4
        {
            // Common elements in first and second 
            public static int Test4()
            {
                int[] first = { 1, 2, 3, 4, 5, 6 };
                int[] second = { 6, 7, 7, 7, 8, 1 };
                int[] expected = { 1, 2, 3, 4, 5, 6, 7, 8 };

                var actual = first.Union(second);
                return Verification.Allequal(expected, actual);
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

        public class Union5
        {
            // first and second has the same element
            public static int Test5()
            {
                int[] first = { 1, 1, 1, 1, 1, 1 };
                int[] second = { 1, 1, 1, 1, 1, 1 };
                int[] expected = { 1 };

                var actual = first.Union(second);
                return Verification.Allequal(expected, actual);
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

        public class Union6
        {
            // first has repeated elements and second has only one element
            public static int Test6()
            {
                int[] first = { 1, 2, 3, 5, 3, 6 };
                int[] second = { 7 };
                int[] expected = { 1, 2, 3, 5, 6, 7 };

                var actual = first.Union(second);
                return Verification.Allequal(expected, actual);
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

        public class Union7
        {
            // first has only one element and second has all unique elements
            public static int Test7()
            {
                int?[] first = { 2 };
                int?[] second = { 3, null, 4, 5 };
                int?[] expected = { 2, 3, null, 4, 5 };

                var actual = first.Union(second);
                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test7();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Union8
        {
            // first and second has repeated elements between and among themselves
            public static int Test8()
            {
                int?[] first = { 1, 2, 3, 4, null, 5, 1 };
                int?[] second = { 6, 2, 3, 4, 5, 6 };
                int?[] expected = { 1, 2, 3, 4, null, 5, 6 };

                var actual = first.Union(second);
                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test8();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Union9
        {
            // Overload-2: Test when EqualityComparer is null
            public static int Test9()
            {
                string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
                string[] second = { "ttaM", "Charlie", "Bbo" };
                string[] expected = { "Bob", "Robert", "Tim", "Matt", "miT", "ttaM", "Charlie", "Bbo" };

                var actual = first.Union(second, null);
                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test9();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}