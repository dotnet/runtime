// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class WhereTests
    {
        public class Where003
        {
            private static int Where001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                Func<int, bool> predicate = Functions.IsEven;
                var rst1 = q.Where(predicate);
                var rst2 = q.Where(predicate);

                return Verification.Allequal(rst1, rst2);
            }

            private static int Where002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", null, "SoS", String.Empty }
                        select x;

                Func<string, bool> predicate = Functions.IsEmpty;

                var rst1 = q.Where(predicate);
                var rst2 = q.Where(predicate);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(Where001) + RunTest(Where002);
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

        public class Where1a
        {
            // Overload-1: source is empty
            public static int Test1a()
            {
                int[] source = { };
                int[] expected = { };
                Func<int, bool> predicate = Functions.IsEven;

                var actual = source.Where(predicate);

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

        public class Where1b
        {
            // Overload-1: source has only one element and predicate returns true
            public static int Test1b()
            {
                int[] source = { 2 };
                int[] expected = { 2 };
                Func<int, bool> predicate = Functions.IsEven;

                var actual = source.Where(predicate);

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

        public class Where1c
        {
            // Overload-1: source has only one element and predicate returns false
            public static int Test1c()
            {
                int[] source = { 2 };
                int[] expected = { 2 };
                Func<int, bool> predicate = Functions.IsEven;

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
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

        public class Where1d
        {
            // Overload-1: predicate returns false for all
            public static int Test1d()
            {
                int[] source = { 9, 7, 15, 3, 27 };
                int[] expected = { };
                Func<int, bool> predicate = Functions.IsEven;

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
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

        public class Where1e
        {
            // Overload-1: predicate returns true for first element only
            public static int Test1e()
            {
                int[] source = { 10, 9, 7, 15, 3, 27 };
                int[] expected = { 10 };
                Func<int, bool> predicate = Functions.IsEven;

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
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

        public class Where1f
        {
            // Overload-1: predicate returns true for last element only
            public static int Test1f()
            {
                int[] source = { 9, 7, 15, 3, 27, 20 };
                int[] expected = { 20 };
                Func<int, bool> predicate = Functions.IsEven;

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
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

        public class Where1g
        {
            // Overload-1: predicate returns true for 1st, 3rd and 6th
            public static int Test1g()
            {
                int[] source = { 20, 7, 18, 9, 7, 10, 21 };
                int[] expected = { 20, 18, 10 };
                Func<int, bool> predicate = Functions.IsEven;

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
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

        public class Where1h
        {
            // Overload-1: source has null and predicate returns true
            public static int Test1h()
            {
                int?[] source = { null, null, null, null };
                int?[] expected = { null, null, null, null };
                Func<int?, bool> predicate = (num) => true;

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
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

        public class Where2a
        {
            // Overload-2: source is empty
            public static int Test2a()
            {
                int[] source = { };
                int[] expected = { };
                Func<int, int, bool> predicate = Functions.IsEven_Index;

                var actual = source.Where(predicate);

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

        public class Where2b
        {
            // Overload-2: source has only one element and predicate returns true
            public static int Test2b()
            {
                int[] source = { 2 };
                int[] expected = { 2 };
                Func<int, int, bool> predicate = Functions.IsEven_Index;

                var actual = source.Where(predicate);

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

        public class Where2c
        {
            // Overload-2: source has only one element and predicate returns false
            public static int Test2c()
            {
                int[] source = { 2 };
                int[] expected = { 2 };
                Func<int, int, bool> predicate = Functions.IsEven_Index;

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
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

        public class Where2d
        {
            // Overload-2: predicate returns false for all
            public static int Test2d()
            {
                int[] source = { 9, 7, 15, 3, 27 };
                int[] expected = { };
                Func<int, int, bool> predicate = Functions.IsEven_Index;

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
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

        public class Where2e
        {
            // Overload-2: predicate returns true for first element only
            public static int Test2e()
            {
                int[] source = { 10, 9, 7, 15, 3, 27 };
                int[] expected = { 10 };
                Func<int, int, bool> predicate = Functions.IsEven_Index;

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
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

        public class Where2f
        {
            // Overload-2: predicate returns true for last element only
            public static int Test2f()
            {
                int[] source = { 9, 7, 15, 3, 27, 20 };
                int[] expected = { 20 };
                Func<int, int, bool> predicate = Functions.IsEven_Index;

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
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

        public class Where2g
        {
            // Overload-2: predicate returns true for 1st, 3rd and 6th
            public static int Test2g()
            {
                int[] source = { 20, 7, 18, 9, 7, 10, 21 };
                int[] expected = { 20, 18, 10 };
                Func<int, int, bool> predicate = Functions.IsEven_Index;

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
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

        public class Where2h
        {
            // Overload-2: source has null and predicate returns true
            public static int Test2h()
            {
                int?[] source = { null, null, null, null };
                int?[] expected = { null, null, null, null };
                Func<int?, int, bool> predicate = (num, index) => true;

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test2h();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Where2i
        {
            // Overload-2: Test for index=0
            public static int Test2i()
            {
                int[] source = { -40, 20, 100, 5, 4, 9 };
                int[] expected = { -40 };
                Func<int, int, bool> predicate = (num, index) => (index == 0);

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test2i();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Where2j
        {
            // Overload-2: Test for index=max source size-1
            public static int Test2j()
            {
                int[] source = { -40, 20, 100, 5, 4, 9 };
                int[] expected = { 9 };
                Func<int, int, bool> predicate = (num, index) => (index == 5);

                var actual = source.Where(predicate);

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test2j();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Where2k
        {
            // Overload-2: Test for OverflowException 
            public static int Test2k()
            {
                IEnumerable<int> source = Functions.NumRange(5, (long)Int32.MaxValue + 10);
                int[] expected = { }; // Overflow Exception should be thrown
                Func<int, int, bool> predicate = (num, index) => true;

                try
                {
                    var actual = source.Where(predicate);
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
                // return Test2k();
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
