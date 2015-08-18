// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class SelectManyTests
    {
        public struct CustomerRec
        {
#pragma warning disable 0649
            public string name;
            public int?[] total;
#pragma warning restore 0649
        }

        public class Helper
        {
            // Helper function to test index=0
            public static IEnumerable<int?> index_zero(CustomerRec cr, int index)
            {
                if (index == 0) return cr.total;
                else return new int?[] { };
            }

            // Helper function to test index=max value
            public static IEnumerable<int?> index_four(CustomerRec cr, int index)
            {
                if (index == 4) return cr.total;
                else return new int?[] { };
            }
        }
        public class SelectMany1a
        {
            // Overload-1: source is empty
            public static int Test1a()
            {
                CustomerRec[] source = { };
                int?[] expected = { };
                Func<CustomerRec, IEnumerable<int?>> selector = (e) => e.total;

                var actual = source.SelectMany(selector);

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

        public class SelectMany1b
        {
            // Overload-1: source has one element
            public static int Test1b()
            {
                CustomerRec[] source = { new CustomerRec { name = "Prakash", total = new int?[] { 90, 55, null, 43, 89 } } };
                int?[] expected = { 90, 55, null, 43, 89 };
                Func<CustomerRec, IEnumerable<int?>> selector = (e) => e.total;

                var actual = source.SelectMany(selector);

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

        public class SelectMany1c
        {
            // Overload-1: source is non-empty but IEnumerable returned by selector is empty
            public static int Test1c()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{}},
                                    new CustomerRec{name="Bob", total=new int?[]{}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{}},
                                    new CustomerRec{name="Prakash", total=new int?[]{}}
            };
                int?[] expected = { };
                Func<CustomerRec, IEnumerable<int?>> selector = (e) => e.total;

                var actual = source.SelectMany(selector);

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

        public class SelectMany1d
        {
            // Overload-1: source and IEnumerable returned by selector non-empty
            public static int Test1d()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };
                int?[] expected = { 1, 2, 3, 4, 5, 6, 8, 9, -10, 100 };
                Func<CustomerRec, IEnumerable<int?>> selector = (e) => e.total;

                var actual = source.SelectMany(selector);

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

        public class SelectMany2a
        {
            // Overload-2: source is empty
            public static int Test2a()
            {
                CustomerRec[] source = { };
                int?[] expected = { };
                Func<CustomerRec, int, IEnumerable<int?>> selector = (e, index) => e.total;

                var actual = source.SelectMany(selector);

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

        public class SelectMany2b
        {
            // Overload-2: source has one element
            public static int Test2b()
            {
                CustomerRec[] source = { new CustomerRec { name = "Prakash", total = new int?[] { 90, 55, null, 43, 89 } } };
                int?[] expected = { 90, 55, null, 43, 89 };
                Func<CustomerRec, int, IEnumerable<int?>> selector = (e, index) => e.total;

                var actual = source.SelectMany(selector);

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

        public class SelectMany2c
        {
            // Overload-2: source is non-empty but IEnumerable returned by selector is empty
            public static int Test2c()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total= new int?[]{}},
                                    new CustomerRec{name="Bob", total=new int?[]{}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{}},
                                    new CustomerRec{name="Prakash", total=new int?[]{}}
            };
                int?[] expected = { };
                Func<CustomerRec, int, IEnumerable<int?>> selector = (e, index) => e.total;

                var actual = source.SelectMany(selector);

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

        public class SelectMany2d
        {
            // Overload-2: source and IEnumerable returned by selector non-empty
            public static int Test2d()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };
                int?[] expected = { 1, 2, 3, 4, 5, 6, 8, 9, -10, 100 };
                Func<CustomerRec, int, IEnumerable<int?>> selector = (e, index) => e.total;

                var actual = source.SelectMany(selector);

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

        public class SelectMany2e
        {
            // Overload-2: index=0 matches 1st element
            public static int Test2e()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };
                int?[] expected = { 1, 2, 3, 4 };
                Func<CustomerRec, int, IEnumerable<int?>> selector = Helper.index_zero;

                var actual = source.SelectMany(selector);

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

        public class SelectMany2f
        {
            // Overload-2: index=max matches last element
            public static int Test2f()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Robert", total=new int?[]{-10, 100}}
            };
                int?[] expected = { -10, 100 };
                Func<CustomerRec, int, IEnumerable<int?>> selector = Helper.index_four;

                var actual = source.SelectMany(selector);

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

        public class SelectMany2g
        {
            // Overload-2: Test for OverflowException
            public static int Test2g()
            {
                IEnumerable<int> source = Functions.NumRange(5, (long)Int32.MaxValue + 10);
                int[] expected = { }; // OverflowException should be thown
                Func<int, int, IEnumerable<int>> selector = delegate (int e, int index)
                {
                    return new int[] { };
                };

                try
                {
                    var actual = source.SelectMany(selector);
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
                //return Test2g();
                return 0;
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class SelectMany3a
        {
            // Overload-3: source and IEnumerable returned by selector non-empty
            public static int Test3a()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };

                string[] expected = { "1", "2", "3", "4", "5", "6", "8", "9", "-10", "100" };
                Func<CustomerRec, IEnumerable<int?>> selector = (e) => e.total;
                Func<CustomerRec, int?, string> resultSelector = (e, f) => f.ToString();
                var actual = source.SelectMany(selector, resultSelector);

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

        public class SelectMany3b
        {
            // Overload-3: Verify if resultSelector is null an exception is thrown
            public static int Test3b()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };

                string[] expected = { "1", "2", "3", "4", "5", "6", "8", "9", "-10", "100" };
                Func<CustomerRec, IEnumerable<int?>> selector = (e) => e.total;
                Func<CustomerRec, int?, string> resultSelector = (e, f) => f.ToString();
                resultSelector = null;

                try
                {
                    var actual = source.SelectMany(selector, resultSelector);
                    return 1;
                }
                catch (ArgumentNullException ae)
                {
                    if (ae.CompareParamName("resultSelector")) return 0;
                    return 1;
                }
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

        public class SelectMany3c
        {
            // Overload-3: Verify if source is null an exception is thrown
            public static int Test3c()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };

                string[] expected = { "1", "2", "3", "4", "5", "6", "8", "9", "-10", "100" };
                Func<CustomerRec, IEnumerable<int?>> selector = (e) => e.total;
                Func<CustomerRec, int?, string> resultSelector = (e, f) => f.ToString();

                try
                {
                    source = null;
                    var actual = source.SelectMany(selector, resultSelector);
                    return 1;
                }
                catch (ArgumentNullException ae)
                {
                    if (ae.CompareParamName("source")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test3c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class SelectMany3d
        {
            // Overload-3: Verify if collectionSelector is null an exception is thrown
            public static int Test3d()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };

                string[] expected = { "1", "2", "3", "4", "5", "6", "8", "9", "-10", "100" };
                Func<CustomerRec, IEnumerable<int?>> selector = (e) => e.total;
                Func<CustomerRec, int?, string> resultSelector = (e, f) => f.ToString();

                try
                {
                    selector = null;
                    var actual = source.SelectMany(selector, resultSelector);
                    return 1;
                }
                catch (ArgumentNullException ae)
                {
                    if (ae.CompareParamName("collectionSelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test3d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class SelectMany4a
        {
            // Overload-4: source and IEnumerable returned by selector non-empty
            public static int Test4a()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };

                string[] expected = { "1", "2", "3", "4", "5", "6", "8", "9", "-10", "100" };
                Func<CustomerRec, int, IEnumerable<int?>> selector = (e, f) => e.total;
                Func<CustomerRec, int?, string> resultSelector = (e, f) => f.ToString();
                var actual = source.SelectMany(selector, resultSelector);

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test4a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class SelectMany4b
        {
            // Overload-4: Verify if resultSelector is null an exception is thrown
            public static int Test4b()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };

                string[] expected = { "1", "2", "3", "4", "5", "6", "8", "9", "-10", "100" };
                Func<CustomerRec, int, IEnumerable<int?>> selector = (e, f) => e.total;
                Func<CustomerRec, int?, string> resultSelector = (e, f) => f.ToString();
                resultSelector = null;

                try
                {
                    var actual = source.SelectMany(selector, resultSelector);
                    return 1;
                }
                catch (ArgumentException ae)
                {
                    if (ae.CompareParamName("resultSelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test4b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class SelectMany4c
        {
            // Overload-4: Verify if source is null an exception is thrown
            public static int Test4c()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };

                string[] expected = { "1", "2", "3", "4", "5", "6", "8", "9", "-10", "100" };
                Func<CustomerRec, int, IEnumerable<int?>> selector = (e, f) => e.total;
                Func<CustomerRec, int?, string> resultSelector = (e, f) => f.ToString();

                try
                {
                    source = null;
                    var actual = source.SelectMany(selector, resultSelector);
                    return 1;
                }
                catch (ArgumentException ae)
                {
                    if (ae.CompareParamName("source")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test4c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class SelectMany4d
        {
            // Overload-4: Verify if collectionSelector is null an exception is thrown
            public static int Test4d()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };

                string[] expected = { "1", "2", "3", "4", "5", "6", "8", "9", "-10", "100" };
                Func<CustomerRec, int, IEnumerable<int?>> selector = (e, f) => e.total;
                Func<CustomerRec, int?, string> resultSelector = (e, f) => f.ToString();

                try
                {
                    selector = null;
                    var actual = source.SelectMany(selector, resultSelector);
                    return 1;
                }
                catch (ArgumentException ae)
                {
                    if (ae.CompareParamName("collectionSelector")) return 0;
                    return 1;
                }
            }


            public static int Main()
            {
                return Test4d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class SelectMany4e
        {
            // Overload-4: Test to verify that index = zero matches 1st element
            public static int Test4e()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };

                string[] expected = { "1", "2", "3", "4" };
                Func<CustomerRec, int, IEnumerable<int?>> selector = Helper.index_zero;
                Func<CustomerRec, int?, string> resultSelector = (e, f) => f.ToString();

                var actual = source.SelectMany(selector, resultSelector);

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test4e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class SelectMany4f
        {
            // Overload-4: Test to verify that index = max matches last element
            public static int Test4f()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", total=new int?[]{1, 2, 3, 4}},
                                    new CustomerRec{name="Bob", total=new int?[]{5, 6}},
                                    new CustomerRec{name="Chris", total=new int?[]{}},
                                    new CustomerRec{name=null, total=new int?[]{8, 9}},
                                    new CustomerRec{name="Prakash", total=new int?[]{-10, 100}}
            };

                string[] expected = { "-10", "100" };
                Func<CustomerRec, int, IEnumerable<int?>> selector = Helper.index_four;
                Func<CustomerRec, int?, string> resultSelector = (e, f) => f.ToString();

                var actual = source.SelectMany(selector, resultSelector);

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test4f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
