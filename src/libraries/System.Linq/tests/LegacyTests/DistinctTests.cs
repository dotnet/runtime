// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class DistinctTests
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
        public class Distinct009
        {
            private static int Distinct001()
            {
                var q = from x in new[] { 0, 9999, 0, 888, -1, 66, -1, -777, 1, 2, -12345, 66, 66, -1, -1 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.Distinct();
                var rst2 = q.Distinct();

                return Verification.Allequal(rst1, rst2);
            }

            private static int Distinct002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "Calling Twice", "SoS" }
                        where String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.Distinct();
                var rst2 = q.Distinct();

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(Distinct001) + RunTest(Distinct002);
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

        public class Distinct1
        {
            // source is empty
            public static int Test1()
            {
                int[] source = { };
                int[] expected = { };

                var actual = source.Distinct();
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

        public class Distinct10
        {
            // DDB:171937
            public static int Test10()
            {
                string[] source = { null };
                string[] expected = { null };

                var actual = source.Distinct(EqualityComparer<string>.Default);

                return Verification.Allequal(expected, actual);
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

        public class Distinct11
        {
            // DDB:171937
            public static int Test11()
            {
                string[] source = { null, null, string.Empty };
                string[] expected = { null, string.Empty };

                var actual = source.Distinct(EqualityComparer<string>.Default);
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

        public class Distinct12
        {
            // DDB:171937
            public static int Test12()
            {
                string[] source = { null, null };
                string[] expected = { null };

                var actual = source.Distinct(EqualityComparer<string>.Default);
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

        public class Distinct2
        {
            // All elements in source are identical
            public static int Test2()
            {
                int[] source = { 5, 5, 5, 5, 5, 5 };
                int[] expected = { 5 };

                var actual = source.Distinct();
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

        public class Distinct3
        {
            // All elements in source are unique
            public static int Test3()
            {
                int[] source = { 2, -5, 0, 6, 10, 9 };
                int[] expected = { 2, -5, 0, 6, 10, 9 };

                var actual = source.Distinct();
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

        public class Distinct4
        {
            // Elements in source are of the form: 111222
            // Element has a null value
            public static int Test4()
            {
                int?[] source = { 1, 1, 1, 2, 2, 2, null, null };
                int?[] expected = { 1, 2, null };

                var actual = source.Distinct();
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

        public class Distinct5
        {
            // First and the Last elements in source are the same
            public static int Test5()
            {
                int[] source = { 1, 2, 3, 4, 5, 1 };
                int[] expected = { 1, 2, 3, 4, 5 };

                var actual = source.Distinct();
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

        public class Distinct6
        {
            // Multiple elements repeat non-consecutively
            public static int Test6()
            {
                int[] source = { 1, 1, 2, 2, 4, 3, 1, 3, 2 };
                int[] expected = { 1, 2, 4, 3 };

                var actual = source.Distinct();
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

        public class Distinct7a
        {
            // Overload-2: Test when EqualityComparer is null
            public static int Test7a()
            {
                string[] source = { "Bob", "Tim", "bBo", "miT", "Robert", "iTm" };
                string[] expected = { "Bob", "Tim", "bBo", "miT", "Robert", "iTm" };

                var actual = source.Distinct(null);
                return Verification.Allequal(expected, actual);
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

        public class Distinct7b
        {
            // Overload-2: Test when source is null
            public static int Test7b()
            {
                string[] source = { "Bob", "Tim", "bBo", "miT", "Robert", "iTm" };
                string[] expected = { "Bob", "Tim", "bBo", "miT", "Robert", "iTm" };

                try
                {
                    source = null;
                    var actual = source.Distinct(new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (!ane.CompareParamName("source")) return 1;
                    return 0;
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

        public class Distinct8
        {
            // Overload-2: Test when EqualityComparer is not-null
            // This test case calls the AllequalComparer to verify that the expected matches the result
            // This will help the PLINQ team when they run the test, since both "Tim", "miT", "iTm" are all the same and should not matter
            public static int Test8()
            {
                string[] source = { "Bob", "Tim", "bBo", "miT", "Robert", "iTm" };
                string[] expected = { "Bob", "Tim", "Robert" };

                var actual = source.Distinct(new AnagramEqualityComparer());
                return Verification.AllequalComparer(expected, actual, new AnagramEqualityComparer());
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
    }
}
