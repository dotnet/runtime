// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class ContainsTests
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

        public class Helper
        {
            // Helper function for Test2f
            public static IEnumerable<int?> NumList_Non_Null(int? start, int? count)
            {
                for (int? i = 0; i < count; i++)
                    yield return start + i;
            }

            // Helper function for Test 2g
            public static IEnumerable<int?> NumList_Null(int count)
            {
                for (int i = 0; i < count; i++)
                    yield return null;
            }
        }


        public class Contains003
        {
            private static int Contains001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.Contains(-1);
                var rst2 = q.Contains(-1);
                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int Contains002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.Contains("X");
                var rst2 = q.Contains("X");

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(Contains001) + RunTest(Contains002);
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

        public class Contains004
        {
            // DDB:171937
            public static int Test004()
            {
                string[] source = { null };
                string value = null;
                bool expected = true;

                // Check whether source is actually ICollection
                ICollection<string> collection = source as ICollection<string>;
                if (collection == null) return 1;

                var actual = source.Contains(value, StringComparer.Ordinal);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test004();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains1a
        {
            // source implements ICollection, source is empty
            public static int Test1a()
            {
                int[] source = { };
                int value = 6;
                bool expected = false;

                // Check whether source is actually ICollection
                ICollection<int> collection = source as ICollection<int>;
                if (collection == null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains1b
        {
            // value does not occur in source
            public static int Test1b()
            {
                int[] source = { 8, 10, 3, 0, -8 };
                int value = 6;
                bool expected = false;

                // Check whether source is actually ICollection
                ICollection<int> collection = source as ICollection<int>;
                if (collection == null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains1c
        {
            // value is 1st element in source
            public static int Test1c()
            {
                int[] source = { 8, 10, 3, 0, -8 };
                int value = 8;
                bool expected = true;

                // Check whether source is actually ICollection
                ICollection<int> collection = source as ICollection<int>;
                if (collection == null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains1d
        {
            // value is last element in source
            public static int Test1d()
            {
                int[] source = { 8, 10, 3, 0, -8 };
                int value = -8;
                bool expected = true;

                // Check whether source is actually ICollection
                ICollection<int> collection = source as ICollection<int>;
                if (collection == null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains1e
        {
            // value is present more than once in source
            public static int Test1e()
            {
                int[] source = { 8, 0, 10, 3, 0, -8, 0 };
                int value = 0;
                bool expected = true;

                // Check whether source is actually ICollection
                ICollection<int> collection = source as ICollection<int>;
                if (collection == null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains1f
        {
            // value is null and source does not has null
            public static int Test1f()
            {
                int?[] source = { 8, 0, 10, 3, 0, -8, 0 };
                int? value = null;
                bool expected = false;

                // Check whether source is actually ICollection
                ICollection<int?> collection = source as ICollection<int?>;
                if (collection == null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains1g
        {
            // value is null and source has null
            public static int Test1g()
            {
                int?[] source = { 8, 0, 10, null, 3, 0, -8, 0 };
                int? value = null;
                bool expected = true;

                // Check whether source is actually ICollection
                ICollection<int?> collection = source as ICollection<int?>;
                if (collection == null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains1h
        {
            // Overload-2: Test when EqualityComparer is null
            public static int Test1h()
            {
                string[] source = { "Bob", "Robert", "Tim" };
                string value = "trboeR";
                bool expected = false;

                // Check whether source is actually ICollection
                ICollection<string> collection = source as ICollection<string>;
                if (collection == null) return 1;

                var actual = source.Contains(value, null);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1h();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains1i
        {
            // Overload-2: Test when EqualityComparer is not null
            public static int Test1i()
            {
                string[] source = { "Bob", "Robert", "Tim" };
                string value = "trboeR";
                bool expected = true;

                // Check whether source is actually ICollection
                ICollection<string> collection = source as ICollection<string>;
                if (collection == null) return 1;

                var actual = source.Contains(value, new AnagramEqualityComparer());

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1i();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains2a
        {
            // source does NOT implement ICollection, source is empty
            public static int Test2a()
            {
                IEnumerable<int> source = Functions.NumList(0, 0);
                int value = 0;
                bool expected = false;

                // Check whether source is actually ICollection
                ICollection<int> collection = source as ICollection<int>;
                if (collection != null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains2b
        {
            // value does not occur in source
            public static int Test2b()
            {
                IEnumerable<int> source = Functions.NumList(4, 5);
                int value = 3;
                bool expected = false;

                // Check whether source is actually ICollection
                ICollection<int> collection = source as ICollection<int>;
                if (collection != null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains2c
        {
            // value is 1st element in source
            public static int Test2c()
            {
                IEnumerable<int> source = Functions.NumList(3, 5);
                int value = 3;
                bool expected = true;

                // Check whether source is actually ICollection
                ICollection<int> collection = source as ICollection<int>;
                if (collection != null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains2d
        {
            // value is last element in source
            public static int Test2d()
            {
                IEnumerable<int> source = Functions.NumList(3, 5);
                int value = 7;
                bool expected = true;

                // Check whether source is actually ICollection
                ICollection<int> collection = source as ICollection<int>;
                if (collection != null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains2e
        {
            // value is present more than once in source
            public static int Test2e()
            {
                IEnumerable<int> source = Functions.NumList(10, 3);
                int value = 10;
                bool expected = true;

                // Check whether source is actually ICollection
                ICollection<int> collection = source as ICollection<int>;
                if (collection != null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains2f
        {
            // value is null and source does not has null
            public static int Test2f()
            {
                IEnumerable<int?> source = Helper.NumList_Non_Null(3, 4);
                int? value = null;
                bool expected = false;

                // Check whether source is actually ICollection
                ICollection<int?> collection = source as ICollection<int?>;
                if (collection != null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Contains2g
        {
            // value is null and source has null
            public static int Test2g()
            {
                IEnumerable<int?> source = Helper.NumList_Null(5);
                int? value = null;
                bool expected = true;

                // Check whether source is actually ICollection
                ICollection<int?> collection = source as ICollection<int?>;
                if (collection != null) return 1;

                var actual = source.Contains(value);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
