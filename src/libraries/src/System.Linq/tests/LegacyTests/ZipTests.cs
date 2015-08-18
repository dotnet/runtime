// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class ZipTests
    {
        public class Zip01
        {
            // implicit type parameters
            public static int Test1()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3 };
                IEnumerable<int> second = new int[] { 2, 5, 9 };
                IEnumerable<int> expected = new int[] { 3, 7, 12 };

                var actual = first.Zip(second, (x, y) => x + y);

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

        public class Zip02
        {
            // explicit type parameters
            public static int Test2()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3 };
                IEnumerable<int> second = new int[] { 2, 5, 9 };
                IEnumerable<int> expected = new int[] { 3, 7, 12 };

                var actual = first.Zip<int, int, int>(second, (x, y) => x + y);

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

        public class Zip03
        {
            // first is null
            public static int Test3()
            {
                IEnumerable<int> first = null;
                IEnumerable<int> second = new int[] { 2, 5, 9 };

                try
                {
                    var actual = first.Zip<int, int, int>(second, (x, y) => x + y);
                    return 1;
                }

                catch (ArgumentNullException e)
                {
                    if (e.ParamName == "first")
                        return 0;
                    else
                        return 1;
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

        public class Zip04
        {
            // second is null
            public static int Test4()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3 };
                IEnumerable<int> second = null;

                try
                {
                    var actual = first.Zip<int, int, int>(second, (x, y) => x + y);
                    return 1;
                }

                catch (ArgumentNullException e)
                {
                    if (e.ParamName == "second")
                        return 0;
                    else
                        return 1;
                }
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

        public class Zip05
        {
            // func is null
            public static int Test5()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3 };
                IEnumerable<int> second = new int[] { 2, 4, 6 };
                Func<int, int, int> func = null;

                try
                {
                    var actual = first.Zip(second, func);
                    return 1;
                }

                catch (ArgumentNullException e)
                {
                    if (e.ParamName == "resultSelector")
                        return 0;
                    else
                        return 1;
                }
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

        public class Zip06
        {
            // exception thrown out from first.GetEnumerator
            public static int Test6()
            {
                MyIEnum<int> first = new MyIEnum<int>(new int[] { 1, 3, 3 });
                IEnumerable<int> second = new int[] { 2, 4, 6 };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { 3, 7, 9 };

                try
                {
                    var actual = first.Zip(second, func);
                    actual.ToList();        // No exception

                    if (Verification.Allequal(expected, actual) != 0)
                        return 1;
                }

                catch (Exception)
                {
                    return 1;
                }

                first = new MyIEnum<int>(new int[] { 1, 2, 3 });

                try
                {
                    var actual = first.Zip(second, func);
                    actual.ToList();        // exception                

                    return 1;
                }

                catch (Exception)
                {
                    return 0;
                }
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

            private class MyIEnum<T> : IEnumerable<T>
            {
                public T[] datas;

                public MyIEnum(T[] array)
                {
                    datas = array;
                }

                public IEnumerator<T> GetEnumerator()
                {
                    foreach (var data in datas)
                    {
                        if (data.Equals(2))
                            throw new Exception();
                        yield return data;
                    }
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }
            }
        }

        public class Zip07
        {
            // exception thrown out from second.GetEnumerator
            public static int Test6()
            {
                MyIEnum<int> second = new MyIEnum<int>(new int[] { 1, 3, 3 });
                IEnumerable<int> first = new int[] { 2, 4, 6 };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { 3, 7, 9 };

                try
                {
                    var actual = first.Zip(second, func);
                    actual.ToList();        // No exception

                    if (Verification.Allequal(expected, actual) != 0)
                        return 1;
                }

                catch (Exception)
                {
                    return 1;
                }

                second = new MyIEnum<int>(new int[] { 1, 2, 3 });

                try
                {
                    var actual = first.Zip(second, func);
                    actual.ToList();        // exception                

                    return 1;
                }

                catch (Exception)
                {
                    return 0;
                }
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

            private class MyIEnum<T> : IEnumerable<T>
            {
                public T[] datas;

                public MyIEnum(T[] array)
                {
                    datas = array;
                }

                public IEnumerator<T> GetEnumerator()
                {
                    foreach (var data in datas)
                    {
                        if (data.Equals(2))
                            throw new Exception();
                        yield return data;
                    }
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }
            }
        }
        public class Zip08
        {
            // first is empty & second is empty
            public static int Test8()
            {
                IEnumerable<int> first = new int[] { };
                IEnumerable<int> second = new int[] { };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { };

                var actual = first.Zip(second, func);

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

        public class Zip09
        {
            // first is empty & second has 1 elment
            public static int Test9()
            {
                IEnumerable<int> first = new int[] { };
                IEnumerable<int> second = new int[] { 2 };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { };

                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
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

        public class Zip10
        {
            // first is empty & second has over 1 elments
            public static int Test10()
            {
                IEnumerable<int> first = new int[] { };
                IEnumerable<int> second = new int[] { 2, 4, 8 };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { };

                var actual = first.Zip(second, func);

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

        public class Zip11
        {
            // second is empty & first has 1 element
            public static int Test11()
            {
                IEnumerable<int> first = new int[] { 1 };
                IEnumerable<int> second = new int[] { };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { };

                var actual = first.Zip(second, func);

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

        public class Zip12
        {
            // second is empty & first has over 1 elements
            public static int Test12()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3 };
                IEnumerable<int> second = new int[] { };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { };

                var actual = first.Zip(second, func);

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

        public class Zip13
        {
            // first.Count == second.Count == 1
            public static int Test13()
            {
                IEnumerable<int> first = new int[] { 1 };
                IEnumerable<int> second = new int[] { 2 };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { 3 };

                var actual = first.Zip(second, func);

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

        public class Zip14
        {
            // first.Count == second.Count > 1
            public static int Test14()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3 };
                IEnumerable<int> second = new int[] { 2, 3, 4 };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { 3, 5, 7 };

                var actual = first.Zip(second, func);

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

        public class Zip15
        {
            // second.Count > first.Count > 1
            // second.Count - first.Count == 1
            public static int Test15()
            {
                IEnumerable<int> first = new int[] { 1, 2 };
                IEnumerable<int> second = new int[] { 2, 4, 8 };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { 3, 6 };

                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test15();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip16
        {
            // second.Count > first.Count > 1
            // second.Count - first.Count > 1
            public static int Test16()
            {
                IEnumerable<int> first = new int[] { 1, 2 };
                IEnumerable<int> second = new int[] { 2, 4, 8, 16 };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { 3, 6 };

                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test16();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip17
        {
            // first.Count > second.Count > 1
            // first.Count - second.Count == 1
            public static int Test17()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3 };
                IEnumerable<int> second = new int[] { 2, 4 };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { 3, 6 };

                var actual = first.Zip(second, func);

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

        public class Zip18
        {
            // first.Count > second.Count > 1
            // first.Count - second.Count > 1
            public static int Test18()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3, 4 };
                IEnumerable<int> second = new int[] { 2, 4 };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { 3, 6 };

                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test18();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip19
        {
            // func is changed - delegate
            public static int Test19()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3, 4 };
                IEnumerable<int> second = new int[] { 2, 4, 8 };
                Func<int, int, int> func = (x, y) => x + y;
                IEnumerable<int> expected = new int[] { 3, 6, 11 };
                var actual = first.Zip(second, func);
                if (Verification.Allequal(expected, actual) != 0)
                    return 1;
                func = (x, y) => x - y;
                expected = new int[] { -1, -2, -5 };
                actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test19();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip20
        {
            // func is changed - lambda expression
            public static int Test20()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3, 4 };
                IEnumerable<int> second = new int[] { 2, 4, 8 };
                IEnumerable<int> expected = new int[] { 3, 6, 11 };
                var actual = first.Zip(second, (x, y) => x + y);

                if (Verification.Allequal(expected, actual) != 0)
                    return 1;

                expected = new int[] { -1, -2, -5 };
                actual = first.Zip(second, (x, y) => x - y);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test20();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip21
        {
            // first: first element is null
            public static int Test21()
            {
                IEnumerable<int?> first = new[] { (int?)null, 2, 3, 4 };
                IEnumerable<int> second = new int[] { 2, 4, 8 };
                Func<int?, int, int?> func = (x, y) => x + y;
                IEnumerable<int?> expected = new int?[] { null, 6, 11 };
                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test21();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip22
        {
            // first: last element is null
            public static int Test22()
            {
                IEnumerable<int?> first = new[] { 1, 2, (int?)null };
                IEnumerable<int> second = new int[] { 2, 4, 6, 8 };
                Func<int?, int, int?> func = (x, y) => x + y;
                IEnumerable<int?> expected = new int?[] { 3, 6, null };
                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test22();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip23
        {
            // first - middle element is null value
            public static int Test23()
            {
                IEnumerable<int?> first = new[] { 1, (int?)null, 3 };
                IEnumerable<int> second = new int[] { 2, 4, 6, 8 };
                Func<int?, int, int?> func = (x, y) => x + y;
                IEnumerable<int?> expected = new int?[] { 3, null, 9 };
                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test23();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip24
        {
            // first - all elements are null
            public static int Test24()
            {
                IEnumerable<int?> first = new int?[] { null, null, null };
                IEnumerable<int> second = new int[] { 2, 4, 6, 8 };
                Func<int?, int, int?> func = (x, y) => x + y;
                IEnumerable<int?> expected = new int?[] { null, null, null };
                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test24();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip25
        {
            // second - first element is null
            public static int Test25()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3, 4 };
                IEnumerable<int?> second = new int?[] { null, 4, 6 };
                Func<int, int?, int?> func = (x, y) => x + y;
                IEnumerable<int?> expected = new int?[] { null, 6, 9 };
                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test25();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip26
        {
            // second - last element is null
            public static int Test26()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3, 4 };
                IEnumerable<int?> second = new int?[] { 2, 4, null };
                Func<int, int?, int?> func = (x, y) => x + y;
                IEnumerable<int?> expected = new int?[] { 3, 6, null };
                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test26();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip27
        {
            // second - middle element is null
            public static int Test27()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3, 4 };
                IEnumerable<int?> second = new int?[] { 2, null, 6 };
                Func<int, int?, int?> func = (x, y) => x + y;
                IEnumerable<int?> expected = new int?[] { 3, null, 9 };
                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test27();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip28
        {
            // second - all elements are null
            public static int Test28()
            {
                IEnumerable<int> first = new int[] { 1, 2, 3, 4 };
                IEnumerable<int?> second = new int?[] { null, null, null };
                Func<int, int?, int?> func = (x, y) => x + y;
                IEnumerable<int?> expected = new int?[] { null, null, null };
                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test28();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip29
        {
            // all elements in first and second are null
            // first.Count > second.Count
            public static int Test29()
            {
                IEnumerable<int?> first = new int?[] { null, null, null, null };
                IEnumerable<int?> second = new int?[] { null, null, null };
                Func<int?, int?, int?> func = (x, y) => x + y;
                IEnumerable<int?> expected = new int?[] { null, null, null };
                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test29();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip30
        {
            // all elements in first and second are null
            // first.Count == second.Count
            public static int Test30()
            {
                IEnumerable<int?> first = new int?[] { null, null, null };
                IEnumerable<int?> second = new int?[] { null, null, null };
                Func<int?, int?, int?> func = (x, y) => x + y;
                IEnumerable<int?> expected = new int?[] { null, null, null };
                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test30();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Zip31
        {
            // all elements in first and second are null
            // first.Count < second.Count
            public static int Test31()
            {
                IEnumerable<int?> first = new int?[] { null, null, null };
                IEnumerable<int?> second = new int?[] { null, null, null, null };
                Func<int?, int?, int?> func = (x, y) => x + y;
                IEnumerable<int?> expected = new int?[] { null, null, null };
                var actual = first.Zip(second, func);

                return Verification.Allequal(expected, actual);
            }

            public static int Main()
            {
                return Test31();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}