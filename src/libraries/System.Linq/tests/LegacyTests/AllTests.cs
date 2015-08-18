// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class AllTests
    {
        public class All007
        {
            private static int All001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                Func<int, bool> predicate = Functions.IsEven;
                var rst1 = q.All(predicate);
                var rst2 = q.All(predicate);

                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int All002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        select x;

                Func<string, bool> predicate = Functions.IsEmpty;
                var rst1 = q.All(predicate);
                var rst2 = q.All(predicate);

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(All001) + RunTest(All002);
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

        public class All01
        {
            // source is empty        
            public static int Test1()
            {
                int[] source = { };
                Func<int, bool> predicate = Functions.IsEven;
                bool expected = true;

                var actual = source.All(predicate);

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

        public class All02
        {
            // source has one element and predicate is false        
            public static int Test2()
            {
                int[] source = { 3 };
                Func<int, bool> predicate = Functions.IsEven;
                bool expected = false;

                var actual = source.All(predicate);

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

        public class All03
        {
            // source has one element and predicate is true        
            public static int Test3()
            {
                int[] source = { 4 };
                Func<int, bool> predicate = Functions.IsEven;
                bool expected = true;

                var actual = source.All(predicate);

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

        public class All04
        {
            // predicate is true for all except 3rd and 4th elements        
            public static int Test4()
            {
                int[] source = { 4, 8, 3, 5, 10, 20, 12 };
                Func<int, bool> predicate = Functions.IsEven;
                bool expected = false;

                var actual = source.All(predicate);

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

        public class All05
        {
            // predicate is true for all except the last element        
            public static int Test5()
            {
                int[] source = { 4, 2, 10, 12, 8, 6, 3 };
                Func<int, bool> predicate = Functions.IsEven;
                bool expected = false;

                var actual = source.All(predicate);

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

        public class All06
        {
            // predicate is true for all elements        
            public static int Test6()
            {
                int[] source = { 4, 2, 10, 12, 8, 6, 14 };
                Func<int, bool> predicate = Functions.IsEven;
                bool expected = true;

                var actual = source.All(predicate);

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
    }
}