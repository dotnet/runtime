// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class IntersectTests
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

        public class Intersect011
        {
            private static int Intersect001()
            {
                var first = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                            select x1;
                var second = from x2 in new int?[] { 1, 9, null, 4 }
                             select x2;

                var rst1 = first.Intersect(second);
                var rst2 = first.Intersect(second);

                return Verification.Allequal(rst1, rst2);
            }

            private static int Intersect002()
            {
                var first = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                            select x1;
                var second = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                             select x2;

                var rst1 = first.Intersect(second);
                var rst2 = first.Intersect(second);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(Intersect001) + RunTest(Intersect002);
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

        public class Intersect1
        {
            // first is empty and second is empty
            public static int Test1()
            {
                int[] first = { };
                int[] second = { };
                int[] expected = { };

                var actual = first.Intersect(second);
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

        public class Intersect10a
        {
            // Overload-2: Test when first=null
            public static int Test10a()
            {
                string[] first = { "Tim", "Bob", "Mike", "Robert" };
                string[] second = { "ekiM", "bBo" };

                try
                {
                    first = null;
                    var actual = first.Intersect(second, new AnagramEqualityComparer());
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
                return Test10a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Intersect10b
        {
            // Overload-2: Test when second=null
            public static int Test10b()
            {
                string[] first = { "Tim", "Bob", "Mike", "Robert" };
                string[] second = { "ekiM", "bBo" };

                try
                {
                    second = null;
                    var actual = first.Intersect(second, new AnagramEqualityComparer());
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
                return Test10b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Intersect12
        {
            // DDB:171937
            public static int Test12()
            {
                string[] first = { null };
                string[] second = new string[0];
                string[] expected = new string[0];

                var actual = first.Intersect(second, EqualityComparer<string>.Default);
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

        public class Intersect13
        {
            // DDB:171937
            public static int Test13()
            {
                string[] first = { null, null, string.Empty };
                string[] second = { null, null };
                string[] expected = { null };

                var actual = first.Intersect(second, EqualityComparer<string>.Default);
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

        public class Intersect14
        {
            // DDB:171937
            public static int Test14()
            {
                string[] first = { null, null };
                string[] second = new string[0];
                string[] expected = new string[0];

                var actual = first.Intersect(second, EqualityComparer<string>.Default);
                return Verification.Allequal(expected, actual);
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

        public class Intersect2
        {
            // first is empty and second is non-empty
            // second has null value
            public static int Test2()
            {
                int?[] first = { };
                int?[] second = { -5, 0, null, 1, 2, 9, 2 };
                int?[] expected = { };

                var actual = first.Intersect(second);
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

        public class Intersect3
        {
            // first is non-empty and second is empty
            // first has null value
            public static int Test3()
            {
                int?[] first = { -5, 0, 1, 2, null, 9, 2 };
                int?[] second = { };
                int?[] expected = { };

                var actual = first.Intersect(second);
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

        public class Intersect4
        {
            // first and second has all distinct elements
            public static int Test4()
            {
                int[] first = { -5, 3, -2, 6, 9 };
                int[] second = { 0, 5, 2, 10, 20 };
                int[] expected = { };

                var actual = first.Intersect(second);
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

        public class Intersect5
        {
            // first and the last elements of "first" and "second" are the same
            // null value is present both in first and second
            public static int Test5()
            {
                int?[] first = { 1, 2, null, 3, 4, 5, 6 };
                int?[] second = { 6, 7, 7, 7, null, 8, 1 };
                int?[] expected = { 1, null, 6 };

                var actual = first.Intersect(second);
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

        public class Intersect6
        {
            // Elements repeat within and between first and second
            public static int Test6()
            {
                int[] first = { 1, 2, 2, 3, 4, 3, 5 };
                int[] second = { 1, 4, 4, 2, 2, 2 };
                int[] expected = { 1, 2, 4 };

                var actual = first.Intersect(second);
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

        public class Intersect7
        {
            // first and second has the same elements
            public static int Test7()
            {
                int[] first = { 1, 1, 1, 1, 1, 1 };
                int[] second = { 1, 1, 1, 1, 1 };
                int[] expected = { 1 };

                var actual = first.Intersect(second);
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

        public class Intersect8
        {
            // Overload-2: Test when EqualityComparer is null
            public static int Test8()
            {
                string[] first = { "Tim", "Bob", "Mike", "Robert" };
                string[] second = { "ekiM", "bBo" };
                string[] expected = { };

                var actual = first.Intersect(second, null);
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

        public class Intersect9
        {
            // Overload-2: Test when EqualityComparer is not-null
            public static int Test9()
            {
                string[] first = { "Tim", "Bob", "Mike", "Robert" };
                string[] second = { "ekiM", "bBo" };
                string[] expected = { "Bob", "Mike" };

                var actual = first.Intersect(second, new AnagramEqualityComparer());

                return Verification.AllequalComparer(expected, actual, new AnagramEqualityComparer());
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
