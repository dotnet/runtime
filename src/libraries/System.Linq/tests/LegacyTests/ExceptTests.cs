// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class ExceptTests
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
        public class Except010
        {
            private static int Except001()
            {
                var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                         select x1;
                var q2 = from x2 in new int?[] { 1, 9, null, 4 }
                         select x2;

                var rst1 = q1.Except(q2);
                var rst2 = q1.Except(q2);

                return Verification.Allequal(rst1, rst2);
            }

            private static int Except002()
            {
                var q1 = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                         select x1;
                var q2 = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                         select x2;

                var rst1 = q1.Except(q2);
                var rst2 = q1.Except(q2);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(Except001) + RunTest(Except002);
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

        public class Except1
        {
            // first is empty and second is empty
            public static int Test1()
            {
                int[] first = { };
                int[] second = { };
                int[] expected = { };

                var actual = first.Except(second);
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

        public class Except11
        {
            // DDB:171937
            public static int Test11()
            {
                string[] first = { null };
                string[] second = new string[0];
                string[] expected = { null };

                var actual = first.Except(second, EqualityComparer<string>.Default);
                return Verification.Allequal(expected, actual);
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

        public class Except12
        {
            // DDB:171937
            public static int Test12()
            {
                string[] first = { null, null, string.Empty };
                string[] second = { null };
                string[] expected = { string.Empty };

                var actual = first.Except(second, EqualityComparer<string>.Default);
                return Verification.Allequal(expected, actual);
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

        public class Except13
        {
            // DDB:171937
            public static int Test13()
            {
                string[] first = { null, null };
                string[] second = new string[0];
                string[] expected = { null };

                var actual = first.Except(second, EqualityComparer<string>.Default);
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

        public class Except2
        {
            // first is empty and second is non-empty
            public static int Test2()
            {
                int[] first = { };
                int[] second = { -6, -8, -6, 2, 0, 0, 5, 6 };
                int[] expected = { };

                var actual = first.Except(second);
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

        public class Except3
        {
            // first is non-empty and second is empty
            // first has null value
            public static int Test3()
            {
                int?[] first = { -6, -8, -6, 2, 0, 0, 5, 6, null, null };
                int?[] second = { };
                int?[] expected = { -6, -8, 2, 0, 5, 6, null };

                var actual = first.Except(second);
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

        public class Except4
        {
            // first has repeated elements, second has some elements in first and
            // additional elements
            // second has null value
            public static int Test4()
            {
                int?[] first = { 1, 2, 2, 3, 4, 5 };
                int?[] second = { 5, 3, 2, 6, 6, 3, 1, null, null };
                int?[] expected = { 4 };

                var actual = first.Except(second);
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

        public class Except5
        {
            // first has repeated elements, second has elements not in first
            public static int Test5()
            {
                int[] first = { 1, 1, 1, 1, 1 };
                int[] second = { 2, 3, 4 };
                int[] expected = { 1 };

                var actual = first.Except(second);
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

        public class Except6
        {
            // order of elements in first is preserved
            // first and second has null values
            public static int Test6()
            {
                int?[] first = { 2, 3, null, 2, null, 4, 5 };
                int?[] second = { 1, 9, null, 4 };
                int?[] expected = { 2, 3, 5 };

                var actual = first.Except(second);
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

        public class Except7
        {
            // Overload-2: Test for EqualityComparer is null
            public static int Test7()
            {
                string[] first = { "Bob", "Tim", "Robert", "Chris" };
                string[] second = { "bBo", "shriC" };
                string[] expected = { "Bob", "Tim", "Robert", "Chris" };

                var actual = first.Except(second, null);
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

        public class Except8
        {
            // Overload-2: Test for EqualityComparer is not-null
            public static int Test8()
            {
                string[] first = { "Bob", "Tim", "Robert", "Chris" };
                string[] second = { "bBo", "shriC" };
                string[] expected = { "Tim", "Robert" };

                var actual = first.Except(second, new AnagramEqualityComparer());
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

        public class Except9a
        {
            // Overload-2: Test for first is null
            public static int Test9a()
            {
                string[] first = { "Bob", "Tim", "Robert", "Chris" };
                string[] second = { "bBo", "shriC" };

                try
                {
                    first = null;
                    var actual = first.Except(second, new AnagramEqualityComparer());
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
                return Test9a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Except9b
        {
            // Overload-2: Test for second is null
            public static int Test9b()
            {
                string[] first = { "Bob", "Tim", "Robert", "Chris" };
                string[] second = { "bBo", "shriC" };

                try
                {
                    second = null;
                    var actual = first.Except(second, new AnagramEqualityComparer());
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
                return Test9b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
