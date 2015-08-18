// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class EqualAllTests
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
        public class EqualAll013
        {
            private static int SequenceEqual001()
            {
                var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                         select x1;
                var q2 = from x2 in new int?[] { 1, 9, null, 4 }
                         select x2;

                var rst1 = q1.SequenceEqual(q2);
                var rst2 = q1.SequenceEqual(q2);

                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int SequenceEqual002()
            {
                var q1 = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                         select x1;
                var q2 = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                         select x2;

                var rst1 = q1.SequenceEqual(q2);
                var rst2 = q1.SequenceEqual(q2);

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(SequenceEqual001) + RunTest(SequenceEqual002);
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

        public class EqualAll1
        {
            // first is empty and second is empty
            public static int Test1()
            {
                int[] first = { };
                int[] second = { };
                bool expected = true;

                var actual = first.SequenceEqual(second);

                return ((expected == actual) ? 0 : 1);
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

        public class EqualAll10
        {
            // One element does not match in the middle
            public static int Test10()
            {
                int?[] first = { 1, 2, 3, 4 };
                int?[] second = { 1, 2, 6, 4 };
                bool expected = false;

                var actual = first.SequenceEqual(second);

                return ((expected == actual) ? 0 : 1);
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

        public class EqualAll11
        {
            // Overload-2: Test for EqualityComparer is null
            public static int Test11()
            {
                string[] first = { "Bob", "Tim", "Chris" };
                string[] second = { "Bbo", "mTi", "rishC" };
                bool expected = false;

                var actual = first.SequenceEqual(second, null);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class EqualAll12
        {
            // Overload-2: Test for EqualityComparer is not-null
            public static int Test12()
            {
                string[] first = { "Bob", "Tim", "Chris" };
                string[] second = { "Bbo", "mTi", "rishC" };
                bool expected = true;

                var actual = first.SequenceEqual(second, new AnagramEqualityComparer());

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test12();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class EqualAll14
        {
            // DDB:171937
            public static int Test14()
            {
                string[] first = { null };
                string[] second = { null };
                bool expected = true;

                var actual = first.SequenceEqual(second, StringComparer.Ordinal);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test14();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class EqualAll2
        {
            // Corresponding elements in first and second are same
            public static int Test2()
            {
                int?[] first = { -6, null, 0, -4, 9, 10, 20 };
                int?[] second = { -6, null, 0, -4, 9, 10, 20 };
                bool expected = true;

                var actual = first.SequenceEqual(second);

                return ((expected == actual) ? 0 : 1);
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

        public class EqualAll3
        {
            // first is empty and second is non-empty
            public static int Test3()
            {
                int?[] first = { };
                int?[] second = { 2, 3, 4 };
                bool expected = false;

                var actual = first.SequenceEqual(second);

                return ((expected == actual) ? 0 : 1);
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

        public class EqualAll4
        {
            // first is non-empty and second is empty
            public static int Test4()
            {
                int?[] first = { 2, 3, 4 };
                int?[] second = { };
                bool expected = false;

                var actual = first.SequenceEqual(second);

                return ((expected == actual) ? 0 : 1);
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

        public class EqualAll5
        {
            // first and second has only one element
            public static int Test5()
            {
                int?[] first = { 2 };
                int?[] second = { 4 };
                bool expected = false;

                var actual = first.SequenceEqual(second);

                return ((expected == actual) ? 0 : 1);
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

        public class EqualAll6
        {
            // first elements do not match
            public static int Test6()
            {
                int?[] first = { 1, 2, 3, 4, 5 };
                int?[] second = { 2, 2, 3, 4, 5 };
                bool expected = false;

                var actual = first.SequenceEqual(second);

                return ((expected == actual) ? 0 : 1);
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

        public class EqualAll7
        {
            // last elements do not match
            public static int Test7()
            {
                int?[] first = { 1, 2, 3, 4, 4 };
                int?[] second = { 1, 2, 3, 4, 5 };
                bool expected = false;

                var actual = first.SequenceEqual(second);

                return ((expected == actual) ? 0 : 1);
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

        public class EqualAll8
        {
            // second has one element more than first
            public static int Test8()
            {
                int?[] first = { 1, 2, 3, 4 };
                int?[] second = { 1, 2, 3, 4, 4 };
                bool expected = false;

                var actual = first.SequenceEqual(second);

                return ((expected == actual) ? 0 : 1);
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

        public class EqualAll9
        {
            // first has one element more than second
            public static int Test9()
            {
                int?[] first = { 1, 2, 3, 4, 4 };
                int?[] second = { 1, 2, 3, 4 };
                bool expected = false;

                var actual = first.SequenceEqual(second);

                return ((expected == actual) ? 0 : 1);
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
