// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class SelectTests
    {
        public struct CustomerRec
        {
#pragma warning disable 0649
            public string name;
            public int custID;
#pragma warning restore 0649
        }

        public class Helper
        {
            // selector function to test index=0
            public static string index_zero(CustomerRec cr, int index)
            {
                if (index == 0) return cr.name;
                else return null;
            }

            // selector function to test index=max value is right
            // Tests if index increments correctly
            public static string index_five(CustomerRec cr, int index)
            {
                if (index == 5) return cr.name;
                else return null;
            }
        }

        public class Select003
        {
            private static int Select001()
            {
                var q1 = from x1 in new string[] { "Alen", "Felix", null, null, "X", "Have Space", "Clinton", "" }
                         select x1; ;

                var q2 = from x2 in new int[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                         select x2;

                var q = from x3 in q1
                        from x4 in q2
                        select new { a1 = x3, a2 = x4 };

                var rst1 = q.Select(e => e.a1);
                var rst2 = q.Select(e => e.a1);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(Select001);
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

        public class Select1a
        {
            // Overload-1: source is empty
            public static int Test1a()
            {
                CustomerRec[] source = { };
                string[] expected = { };
                Func<CustomerRec, string> selector = (e) => e.name;

                var actual = source.Select(selector);

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

        public class Select1b
        {
            // Overload-1: source has one element
            public static int Test1b()
            {
                CustomerRec[] source = { new CustomerRec { name = "Prakash", custID = 98088 } };
                string[] expected = { "Prakash" };
                Func<CustomerRec, string> selector = (e) => e.name;

                var actual = source.Select(selector);

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

        public class Select1c
        {
            // Overload-1: source has limited number of elements
            public static int Test1c()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", custID=98088},
                                    new CustomerRec{name="Bob", custID=29099},
                                    new CustomerRec{name="Chris", custID=39033},
                                    new CustomerRec{name=null, custID=30349},
                                    new CustomerRec{name="Prakash", custID=39030}
            };
                string[] expected = { "Prakash", "Bob", "Chris", null, "Prakash" };
                Func<CustomerRec, string> selector = (e) => e.name;

                var actual = source.Select(selector);

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

        public class Select2a
        {
            // Overload-2: source is empty
            public static int Test2a()
            {
                CustomerRec[] source = { };
                string[] expected = { };
                Func<CustomerRec, int, string> selector = (e, index) => e.name;

                var actual = source.Select(selector);

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

        public class Select2b
        {
            // Overload-2: source has one element
            public static int Test2b()
            {
                CustomerRec[] source = { new CustomerRec { name = "Prakash", custID = 98088 } };
                string[] expected = { "Prakash" };
                Func<CustomerRec, int, string> selector = (e, index) => e.name;

                var actual = source.Select(selector);

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

        public class Select2c
        {
            // Overload-2: source has limited number of elements
            public static int Test2c()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", custID=98088},
                                    new CustomerRec{name="Bob", custID=29099},
                                    new CustomerRec{name="Chris", custID=39033},
                                    new CustomerRec{name=null, custID=30349},
                                    new CustomerRec{name="Prakash", custID=39030}
            };
                string[] expected = { "Prakash", "Bob", "Chris", null, "Prakash" };
                Func<CustomerRec, int, string> selector = (e, index) => e.name;

                var actual = source.Select(selector);

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

        public class Select2d
        {
            // Overload-2: index=0 returns the first element from source
            public static int Test2d()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", custID=98088},
                                    new CustomerRec{name="Bob", custID=29099},
                                    new CustomerRec{name="Chris", custID=39033},
            };
                string[] expected = { "Prakash", null, null };
                Func<CustomerRec, int, string> selector = Helper.index_zero;

                var actual = source.Select(selector);

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

        public class Select2e
        {
            // Overload-2: index=max value is right
            public static int Test2e()
            {
                CustomerRec[] source = {new CustomerRec{name="Prakash", custID=98088},
                                    new CustomerRec{name="Bob", custID=29099},
                                    new CustomerRec{name="Chris", custID=39033},
                                    new CustomerRec{name="Robert", custID=39033},
                                    new CustomerRec{name="Allen", custID=39033},
                                    new CustomerRec{name="Chuck", custID=39033}
            };
                string[] expected = { null, null, null, null, null, "Chuck" };
                Func<CustomerRec, int, string> selector = Helper.index_five;

                var actual = source.Select(selector);

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

        public class Select2f
        {
            // Overload-2: Check for overflow exception
            public static int Test2f()
            {
                IEnumerable<int> source = Functions.NumRange(5, (long)Int32.MaxValue + 10);
                int[] expected = { }; // Overflow Exception is thrown
                Func<int, int, int> selector = (e, index) => e;

                try
                {
                    var actual = source.Select(selector);
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
                //return Test2f();
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
