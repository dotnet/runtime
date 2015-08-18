// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class SkipWhileTests
    {
        public class SkipWhile007
        {
            private static int SkipWhile001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                // Skip all - PLINQ can return elements in any order
                Func<int, bool> predicate = (x) => true;

                var rst1 = q.SkipWhile(predicate);
                var rst2 = q.SkipWhile(predicate);

                return Verification.Allequal(rst1, rst2);
            }

            private static int SkipWhile002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x)
                        select x;

                // Skip all - PLINQ can return elements in any order
                Func<string, bool> predicate = (x) => true;

                var rst1 = q.SkipWhile(predicate);
                var rst2 = q.SkipWhile(predicate);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(SkipWhile001) + RunTest(SkipWhile002);
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

        public class SkipWhile1a
        {
            // Number of elements in source>0 and predicate returns true for all
            // (Predicate without index)
            public static int Test1a()
            {
                int[] source = { 6, 8, 10, 12, 4 };
                int[] expected = { };
                Func<int, bool> predicate = Functions.IsEven;

                var actual = source.SkipWhile(predicate);
                return Verification.Allequal(expected, actual);
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

        public class SkipWhile1b
        {
            // Number of elements in source>0 and predicate returns true for all
            // (Predicate with index)
            public static int Test1b()
            {
                int[] source = { 6, 8, 10, 12, 4 };
                int[] expected = { };
                Func<int, int, bool> predicate = Functions.IsEven_Index;

                var actual = source.SkipWhile(predicate);
                return Verification.Allequal(expected, actual);
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

        public class SkipWhile2a
        {
            // Predicate true for 1st, 3rd, 4th.... (except 2nd)
            // (Predicate without index)
            public static int Test2a()
            {
                int[] source = { 8, 3, 12, 4, 6, 10 };
                int[] expected = { 3, 12, 4, 6, 10 };
                Func<int, bool> predicate = Functions.IsEven;

                var actual = source.SkipWhile(predicate);
                return Verification.Allequal(expected, actual);
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

        public class SkipWhile2b
        {
            // Predicate true for 1st, 3rd, 4th.... (except 2nd)
            // (Predicate with index)
            public static int Test2b()
            {
                int[] source = { 8, 3, 12, 4, 6, 10 };
                int[] expected = { 3, 12, 4, 6, 10 };
                Func<int, int, bool> predicate = Functions.IsEven_Index;

                var actual = source.SkipWhile(predicate);
                return Verification.Allequal(expected, actual);
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

        public class SkipWhile3a
        {
            // Predicate true for 2nd, 3rd, 4th.... (except 1st)
            // (Predicate without index)
            public static int Test3a()
            {
                int[] source = { 3, 2, 4, 12, 6 };
                int[] expected = { 3, 2, 4, 12, 6 };
                Func<int, bool> predicate = Functions.IsEven;

                var actual = source.SkipWhile(predicate);
                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test3a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class SkipWhile3b
        {
            // Predicate true for 2nd, 3rd, 4th.... (except 1st)
            // (Predicate with index)
            public static int Test3b()
            {
                int[] source = { 3, 2, 4, 12, 6 };
                int[] expected = { 3, 2, 4, 12, 6 };
                Func<int, int, bool> predicate = Functions.IsEven_Index;

                var actual = source.SkipWhile(predicate);
                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test3b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class SkipWhile4
        {
            // index=0, corresponds to the first element
            public static int Test4()
            {
                int[] source = { 6, 2, 5, 3, 8 };
                int[] expected = { 2, 5, 3, 8 };

                var actual = source.SkipWhile((element, index) => (index == 0));
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

        public class SkipWhile5
        {
            // index=number of elements - 1, corresponds to the last element
            public static int Test5()
            {
                int[] source = { 6, 2, 5, 3, 8 };
                int[] expected = { 8 };

                var actual = source.SkipWhile((element, index) => (index < 4));
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

        public class SkipWhile6
        {
            // Test for OverflowException
            public static int Test6()
            {
                IEnumerable<int> source = Functions.NumRange(5, (long)Int32.MaxValue + 10);
                int[] expected = { }; // OverflowException is thrown

                try
                {
                    var actual = source.SkipWhile((element, index) => true);
                    Verification.Allequal(source, actual);
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                //return Test6();
                return 0;
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
