// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class CastTests
    {
        public class Helper
        {
            // Helper Method for Test15 and Test16
            public static void GenericTest<T>(object o)
            {
                byte? i = 10;
                Object[] source = { -1, 0, o, i };

                IEnumerable<int?> source1 = source.Cast<int?>();
                IEnumerable<T> actual = source1.Cast<T>();

                Assert.Throws<InvalidCastException>(() => actual.ToList());
            }
        }

        public class Cast019
        {
            [Fact]
            public void Cast001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.Cast<long>();
                var rst2 = q.Cast<long>();

                Assert.Throws<InvalidCastException>(() => { foreach (var t in rst1) ; });
            }

            [Fact]
            public void Cast002()
            {
                var q = from x in new byte[] { 0, 255, 127, 128, 1, 33, 99 }
                        select x;

                var rst1 = q.Cast<ushort>();
                var rst2 = q.Cast<ushort>();
                Assert.Throws<InvalidCastException>(() => { foreach (var t in rst1) ; });
            }
        }

        public class Cast1
        {
            // source is empty
            public static int Test1()
            {
                Object[] source = { };
                int[] expected = { };

                IEnumerable<int> actual = source.Cast<int>();

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

        public class Cast10
        {
            [Fact]
            // source of type int? to object and IEnumerable<int?> Cast to type long
            // DDB: 137558
            public void Test10()
            {
                int? i = 10;
                Object[] source = { -4, 1, 2, 3, 9, i };

                IEnumerable<int?> source1 = source.Cast<int?>();
                IEnumerable<long> actual = source1.Cast<long>();
                Assert.Throws<InvalidCastException>(() => actual.ToList());
            }
        }

        public class Cast11
        {
            [Fact]
            // source of type int? to object and IEnumerable<int?> Cast to type long?
            // DDB: 137558
            public void Test11()
            {
                int? i = 10;
                Object[] source = { -4, 1, 2, 3, 9, null, i };

                IEnumerable<int?> source1 = source.Cast<int?>();
                IEnumerable<long?> actual = source1.Cast<long?>();
                Assert.Throws<InvalidCastException>(() => actual.ToList());
            }
        }

        public class Cast12
        {
            // source of type int? to object cast to IEnumerable<int?> 
            // DDB: 137558
            public static int Test12()
            {
                int? i = 10;
                Object[] source = { -4, 1, 2, 3, 9, null, i };
                int?[] expected = { -4, 1, 2, 3, 9, null, i };

                IEnumerable<int?> actual = source.Cast<int?>();

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

        public class Cast13
        {
            [Fact]
            // source of type object cast to IEnumerable<int> 
            // DDB: 137558
            public void Test()
            {
                Object[] source = { -4, 1, 2, 3, 9, "45" };

                IEnumerable<int> actual = source.Cast<int>();
                Assert.Throws<InvalidCastException>(() => actual.ToList());
            }
        }

        public class Cast14
        {
            [Fact]
            // source of type int Cast to type double
            // DDB: 137558
            public void Test()
            {
                int[] source = new int[] { -4, 1, 2, 9 };

                IEnumerable<double> actual = source.Cast<double>();
                Assert.Throws<InvalidCastException>(() => actual.ToList());
            }
        }

        public class Cast15
        {
            [Fact]
            // Cast involving Generic types
            // DDB: 137558
            public void Test()
            {
                Helper.GenericTest<long?>(null);
            }

        }

        public class Cast16
        {
            [Fact]
            // Cast involving Generic types
            // DDB: 137558
            public void Test()
            {
                Helper.GenericTest<long>(9L);
            }
        }

        public class Cast17
        {
            // source of type object Cast to type string
            // DDB: 137558
            public static int Test17()
            {
                object[] source = { "Test1", "4.5", null, "Test2" };
                string[] expected = { "Test1", "4.5", null, "Test2" };

                IEnumerable<string> actual = source.Cast<string>();

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test17();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Cast18
        {
            [Fact]
            //testing array conversion using .Cast()
            // From Silverlight testing
            public void Test()
            {
                Assert.Throws<InvalidCastException>(() => new[] { -4 }.Cast<long>().ToList());
            }
        }

        public class Cast2
        {
            [Fact]
            // first element cannot be cast to type int: Test for InvalidCastException
            public void Test()
            {
                Object[] source = { "Test", 3, 5, 10 };

                IEnumerable<int> actual = source.Cast<int>();
                Assert.Throws<InvalidCastException>(() => actual.ToList());
            }
        }

        public class Cast3
        {
            [Fact]
            // last element cannot be cast to type int: Test for InvalidCastException
            public void Test()
            {
                Object[] source = { -5, 9, 0, 5, 9, "Test" };

                IEnumerable<int> actual = source.Cast<int>();
                Assert.Throws<InvalidCastException>(() => actual.ToList());
            }
        }

        public class Cast4
        {
            // All elements in source can be cast to int?
            public static int Test4()
            {
                Object[] source = { 3, null, 5, -4, 0, null, 9 };
                int?[] expected = { 3, null, 5, -4, 0, null, 9 };

                IEnumerable<int?> actual = source.Cast<int?>();

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

        public class Cast5
        {
            [Fact]
            // source of type int Cast to type long
            // DDB: 137558
            public void Test()
            {
                int[] source = new int[] { -4, 1, 2, 3, 9 };

                IEnumerable<long> actual = source.Cast<long>();
                Assert.Throws<InvalidCastException>(() => actual.ToList());
            }
        }

        public class Cast6
        {
            [Fact]
            // source of type int Cast to type long?
            // DDB: 137558
            public void Test()
            {
                int[] source = new int[] { -4, 1, 2, 3, 9 };

                IEnumerable<long?> actual = source.Cast<long?>();
                Assert.Throws<InvalidCastException>(() => actual.ToList());
            }
        }

        public class Cast7
        {
            [Fact]
            // source of type int? Cast to type long
            // DDB: 137558
            public static void Test()
            {
                int?[] source = new int?[] { -4, 1, 2, 3, 9 };

                IEnumerable<long> actual = source.Cast<long>();
                Assert.Throws<InvalidCastException>(() => actual.ToList());
            }
        }

        public class Cast8
        {
            // source of type int? Cast to type long with null value
            // DDB: 137558
            public void Test()
            {
                int?[] source = new int?[] { -4, 1, 2, 3, 9 };

                IEnumerable<long> actual = source.Cast<long>();
                Assert.Throws<InvalidCastException>(() => actual.ToList());
            }
        }

        public class Cast9
        {
            [Fact]
            // source of type int? Cast to type long?
            // DDB: 137558
            public void Test()
            {
                int?[] source = new int?[] { -4, 1, 2, 3, 9, null };

                IEnumerable<long?> actual = source.Cast<long?>();
                Assert.Throws<InvalidCastException>(() => actual.ToList());
            }
        }
    }
}