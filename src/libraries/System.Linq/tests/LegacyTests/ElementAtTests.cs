// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class ElementAtTests
    {
        public class ElementAt013
        {
            private static int ElementAt001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.ElementAt(3);
                var rst2 = q.ElementAt(3);

                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int ElementAt002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.ElementAt(4);
                var rst2 = q.ElementAt(4);

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(ElementAt001) + RunTest(ElementAt002);
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

        public class ElementAt1
        {
            // source is of type IList, index < 0;
            public static int Test1()
            {
                int[] source = { 9, 8 };
                int index = -1;

                IList<int> list = source as IList<int>;

                if (list == null) return 1;

                try
                {
                    var actual = source.ElementAt(index);
                    return 1;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return 0;
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine("Actually caught IndexOutOfRange");
                    return 1;
                }
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

        public class ElementAt10
        {
            // source is NOT of type IList, source has one element, index is zero
            public static int Test10()
            {
                IEnumerable<int> source = Functions.NumList(9, 1);
                int index = 0;
                int expected = 9;

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                var actual = source.ElementAt(index);

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

        public class ElementAt11
        {
            // source is NOT of type IList, source has > 1 element, index is (# of elements - 1)
            public static int Test11()
            {
                IEnumerable<int> source = Functions.NumList(9, 10);
                int index = 9;
                int expected = 18;

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                var actual = source.ElementAt(index);

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

        public class ElementAt12
        {
            // source is NOT of type IList, source has > 1 element, index is somewhere in the middle
            public static int Test12()
            {
                IEnumerable<int> source = Functions.NumList(-4, 10);
                int index = 3;
                int expected = -1;

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                var actual = source.ElementAt(index);

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

        public class ElementAt2
        {
            // source is of type IList, index = Number of elements in source;
            public static int Test2()
            {
                int[] source = { 1, 2, 3, 4 };
                int index = 4;

                IList<int> list = source as IList<int>;

                if (list == null) return 1;

                try
                {
                    var actual = source.ElementAt(index);
                    return 1;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return 0;
                }
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

        public class ElementAt3
        {
            // source is of type IList, source is empty, index is zero
            public static int Test3()
            {
                int[] source = { };
                int index = 0;

                IList<int> list = source as IList<int>;

                if (list == null) return 1;

                try
                {
                    var actual = source.ElementAt(index);
                    return 1;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return 0;
                }
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

        public class ElementAt4
        {
            // source is of type IList, source has one element, index is zero
            public static int Test4()
            {
                int[] source = { -4 };
                int index = 0;
                int expected = -4;

                IList<int> list = source as IList<int>;

                if (list == null) return 1;

                var actual = source.ElementAt(index);

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

        public class ElementAt5
        {
            // source is of type IList, source has > 1 element, index is (# of elements - 1)
            public static int Test5()
            {
                int[] source = { 9, 8, 0, -5, 10 };
                int index = 4;
                int expected = 10;

                IList<int> list = source as IList<int>;

                if (list == null) return 1;

                var actual = source.ElementAt(index);

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

        public class ElementAt6
        {
            // source is of type IList, source has > 1 element, index is somewhere in the middle
            public static int Test6()
            {
                int?[] source = { 9, 8, null, -5, 10 };
                int index = 2;
                int? expected = null;

                IList<int?> list = source as IList<int?>;

                if (list == null) return 1;

                var actual = source.ElementAt(index);

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

        public class ElementAt7
        {
            // source is NOT of type IList, index < 0;
            public static int Test7()
            {
                IEnumerable<int> source = Functions.NumList(-4, 5);
                int index = -1;

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                try
                {
                    var actual = source.ElementAt(index);
                    return 1;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return 0;
                }
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

        public class ElementAt8
        {
            // source is NOT of type IList, index = Number of elements in source;
            public static int Test8()
            {
                IEnumerable<int> source = Functions.NumList(5, 5);
                int index = 5;

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                try
                {
                    var actual = source.ElementAt(index);
                    return 1;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return 0;
                }
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

        public class ElementAt9
        {
            // source is NOT of type IList, source is empty, index is zero
            public static int Test9()
            {
                IEnumerable<int> source = Functions.NumList(0, 0);
                int index = 0;

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                try
                {
                    var actual = source.ElementAt(index);
                    return 1;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return 0;
                }
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
