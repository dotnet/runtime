// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class JoinTests
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

        public struct CustomerRec
        {
            public string name;
            public int custID;
        }

        public struct OrderRec
        {
            public int orderID;
            public int custID;
            public int total;
        }

        public struct AnagramRec
        {
            public string name;
            public int orderID;
            public int total;
        }

        public struct JoinRec
        {
            public string name;
            public int orderID;
            public int total;
        }

        public class Helper
        {
            public static JoinRec createJoinRec(CustomerRec cr, OrderRec or)
            {
                JoinRec jr = new JoinRec();

                jr.name = cr.name;
                jr.orderID = or.orderID;
                jr.total = or.total;

                return jr;
            }

            public static JoinRec createJoinRec(CustomerRec cr, AnagramRec or)
            {
                JoinRec jr = new JoinRec();

                jr.name = cr.name;
                jr.orderID = or.orderID;
                jr.total = or.total;

                return jr;
            }
        }
        public class Join1
        {
            // outer is empty and inner is non-empty
            public static int Test1()
            {
                CustomerRec[] outer = new CustomerRec[] { };
                OrderRec[] inner = new OrderRec[]{  new OrderRec{orderID=45321, custID=98022, total=50},
                                                new OrderRec{orderID=97865, custID=32103, total=25}
            };
                JoinRec[] expected = new JoinRec[] { };
                Func<CustomerRec, OrderRec, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.Join(inner, (e) => e.custID, (o) => o.custID, resultSelector);

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

        public class Join10
        {
            // key for first element in outer matches with key for last element in inner
            // key for last element in outer matches with key for first element in inner
            // Also tests the scnario where inner and outer has the same number of elements
            public static int Test10()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Prakash", custID=98022},
                                                     new CustomerRec{name="Tim", custID=99021},
                                                     new CustomerRec{name="Robert", custID=99022}
            };
                OrderRec[] inner = new OrderRec[]{  new OrderRec{orderID=45321, custID=99022, total=50},
                                                new OrderRec{orderID=43421, custID=29022, total=20},
                                                new OrderRec{orderID=95421, custID=98022, total=9}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Prakash", orderID=95421, total=9},
                                                new JoinRec{name="Robert", orderID=45321, total=50}
            };
                Func<CustomerRec, OrderRec, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.Join(inner, (e) => e.custID, (o) => o.custID, resultSelector);

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

        public class Join11
        {
            // Overload-2: Test when IEqualityComparer is null
            public static int Test11()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Prakash", custID=98022},
                                                     new CustomerRec{name="Tim", custID=99021},
                                                     new CustomerRec{name="Robert", custID=99022}
            };
                AnagramRec[] inner = new AnagramRec[]{  new AnagramRec{name = "miT", orderID = 43455, total = 10},
                                                    new AnagramRec{name = "Prakash", orderID = 323232, total = 9}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Prakash", orderID=323232, total=9},
            };
                Func<CustomerRec, AnagramRec, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.Join(inner, (e) => e.name, (o) => o.name, resultSelector, null);

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

        public class Join12
        {
            // Overload-2: Test when IEqualityComparer is not-null
            public static int Test12()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Prakash", custID=98022},
                                                     new CustomerRec{name="Tim", custID=99021},
                                                     new CustomerRec{name="Robert", custID=99022}
            };
                AnagramRec[] inner = new AnagramRec[]{  new AnagramRec{name = "miT", orderID = 43455, total = 10},
                                                    new AnagramRec{name = "Prakash", orderID = 323232, total = 9}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Prakash", orderID=323232, total=9},
                                                new JoinRec{name="Tim", orderID=43455, total=10}
            };
                Func<CustomerRec, AnagramRec, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.Join(inner, (e) => e.name, (o) => o.name, resultSelector, new AnagramEqualityComparer());

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

        public class Join13a
        {
            // Overload-2: Test when outer=null
            public static int Test13a()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Prakash", custID=98022},
                                                     new CustomerRec{name="Tim", custID=99021},
                                                     new CustomerRec{name="Robert", custID=99022}
            };
                AnagramRec[] inner = new AnagramRec[]{  new AnagramRec{name = "miT", orderID = 43455, total = 10},
                                                    new AnagramRec{name = "Prakash", orderID = 323232, total = 9}
            };
                Func<CustomerRec, AnagramRec, JoinRec> resultSelector = Helper.createJoinRec;

                try
                {
                    outer = null;
                    var actual = outer.Join(inner, (e) => e.name, (o) => o.name, resultSelector, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (!ane.CompareParamName("outer")) return 1;
                    return 0;
                }
            }


            public static int Main()
            {
                return Test13a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Join13b
        {
            // Overload-2: Test when inner=null
            public static int Test13b()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Prakash", custID=98022},
                                                     new CustomerRec{name="Tim", custID=99021},
                                                     new CustomerRec{name="Robert", custID=99022}
            };
                AnagramRec[] inner = new AnagramRec[]{  new AnagramRec{name = "miT", orderID = 43455, total = 10},
                                                    new AnagramRec{name = "Prakash", orderID = 323232, total = 9}
            };
                Func<CustomerRec, AnagramRec, JoinRec> resultSelector = Helper.createJoinRec;

                try
                {
                    inner = null;
                    var actual = outer.Join(inner, (e) => e.name, (o) => o.name, resultSelector, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (!ane.CompareParamName("inner")) return 1;
                    return 0;
                }
            }


            public static int Main()
            {
                return Test13b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Join13c
        {
            // Overload-2: Test when outerKeySelector=null
            public static int Test13c()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Prakash", custID=98022},
                                                     new CustomerRec{name="Tim", custID=99021},
                                                     new CustomerRec{name="Robert", custID=99022}
            };
                AnagramRec[] inner = new AnagramRec[]{  new AnagramRec{name = "miT", orderID = 43455, total = 10},
                                                    new AnagramRec{name = "Prakash", orderID = 323232, total = 9}
            };
                Func<CustomerRec, AnagramRec, JoinRec> resultSelector = Helper.createJoinRec;

                try
                {
                    var actual = outer.Join(inner, null, (o) => o.name, resultSelector, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (!ane.CompareParamName("outerKeySelector")) return 1;
                    return 0;
                }
            }


            public static int Main()
            {
                return Test13c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Join13d
        {
            // Overload-2: Test when innerKeySelector=null
            public static int Test13d()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Prakash", custID=98022},
                                                     new CustomerRec{name="Tim", custID=99021},
                                                     new CustomerRec{name="Robert", custID=99022}
            };
                AnagramRec[] inner = new AnagramRec[]{  new AnagramRec{name = "miT", orderID = 43455, total = 10},
                                                    new AnagramRec{name = "Prakash", orderID = 323232, total = 9}
            };
                Func<CustomerRec, AnagramRec, JoinRec> resultSelector = Helper.createJoinRec;

                try
                {
                    var actual = outer.Join(inner, (e) => e.name, null, resultSelector, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (!ane.CompareParamName("innerKeySelector")) return 1;
                    return 0;
                }
            }


            public static int Main()
            {
                return Test13d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Join13e
        {
            // Overload-2: Test when resultSelector=null
            public static int Test13e()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Prakash", custID=98022},
                                                     new CustomerRec{name="Tim", custID=99021},
                                                     new CustomerRec{name="Robert", custID=99022}
            };
                AnagramRec[] inner = new AnagramRec[]{  new AnagramRec{name = "miT", orderID = 43455, total = 10},
                                                    new AnagramRec{name = "Prakash", orderID = 323232, total = 9}
            };
                Func<CustomerRec, AnagramRec, JoinRec> resultSelector = Helper.createJoinRec;

                try
                {
                    resultSelector = null;
                    var actual = outer.Join(inner, (e) => e.name, (o) => o.name, resultSelector, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (!ane.CompareParamName("resultSelector")) return 1;
                    return 0;
                }
            }


            public static int Main()
            {
                return Test13e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Join14
        {
            // DDB:171937
            public static int Test14()
            {
                string[] outer = new string[] { null, string.Empty };
                string[] inner = new string[] { null, string.Empty };
                string[] expected = new string[] { string.Empty };

                Func<string, string, string> resultSelector = (string x, string y) => y;

                var actual = outer.Join(inner, (e) => e, (o) => o, resultSelector, EqualityComparer<string>.Default).ToList();

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

        public class Join2
        {
            // outer is non-empty and inner is non-empty
            public static int Test2()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=43434},
                                                     new CustomerRec{name="Bob", custID=34093}
            };
                OrderRec[] inner = new OrderRec[] { };
                JoinRec[] expected = new JoinRec[] { };
                Func<CustomerRec, OrderRec, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.Join(inner, (e) => e.custID, (o) => o.custID, resultSelector);

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

        public class Join3
        {
            // outer and inner has only one element and the key matches
            public static int Test3()
            {
                CustomerRec[] outer = new CustomerRec[] { new CustomerRec { name = "Prakash", custID = 98022 } };
                OrderRec[] inner = new OrderRec[] { new OrderRec { orderID = 45321, custID = 98022, total = 50 } };
                JoinRec[] expected = new JoinRec[] { new JoinRec { name = "Prakash", orderID = 45321, total = 50 } };
                Func<CustomerRec, OrderRec, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.Join(inner, (e) => e.custID, (o) => o.custID, resultSelector);

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

        public class Join4
        {
            // outer and inner has only one element and the key do not match
            public static int Test4()
            {
                CustomerRec[] outer = new CustomerRec[] { new CustomerRec { name = "Prakash", custID = 98922 } };
                OrderRec[] inner = new OrderRec[] { new OrderRec { orderID = 45321, custID = 98022, total = 50 } };
                JoinRec[] expected = new JoinRec[] { };
                Func<CustomerRec, OrderRec, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.Join(inner, (e) => e.custID, (o) => o.custID, resultSelector);

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

        public class Join5
        {
            // innerKeySelector, outerKeySelector and resultSelector returns null
            public static int Test5()
            {
                int?[] inner = { null, null, null };
                int?[] outer = { null, null };

                int?[] expected = { };

                var actual = outer.Join(inner, (e) => e, (o) => o, (e1, e2) => (e1));

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

        public class Join6
        {
            // innerKeySelector produces same key for more than one element and is
            // matched by one of the outerKeySelector.
            // Also tests the scenario where outerKeySelector matches more than one
            // of the innerKeySelector.
            // Also tests the scenario where inner has more elements than outer.
            public static int Test6()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Prakash", custID=98022},
                                                     new CustomerRec{name="Tim", custID=99021},
                                                     new CustomerRec{name="Robert", custID=99022}
            };
                OrderRec[] inner = new OrderRec[]{  new OrderRec{orderID=45321, custID=98022, total=50},
                                                new OrderRec{orderID=45421, custID=98022, total=10},
                                                new OrderRec{orderID=43421, custID=99022, total=20},
                                                new OrderRec{orderID=85421, custID=98022, total=18},
                                                new OrderRec{orderID=95421, custID=99021, total=9}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Prakash", orderID=45321, total=50},
                                                new JoinRec{name="Prakash", orderID=45421, total=10},
                                                new JoinRec{name="Prakash", orderID=85421, total=18},
                                                new JoinRec{name="Tim", orderID=95421, total=9},
                                                new JoinRec{name="Robert", orderID=43421, total=20}
            };
                Func<CustomerRec, OrderRec, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.Join(inner, (e) => e.custID, (o) => o.custID, resultSelector);

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

        public class Join7
        {
            // outerKeySelector produces same key for more than one element and is
            // matched by one of the innerKeySelector.
            // Also tests the scenario where outer has more elements than inner.
            public static int Test7()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Prakash", custID=98022},
                                                     new CustomerRec{name="Bob", custID=99022},
                                                     new CustomerRec{name="Tim", custID=99021},
                                                     new CustomerRec{name="Robert", custID=99022}
            };
                OrderRec[] inner = new OrderRec[]{  new OrderRec{orderID=45321, custID=98022, total=50},
                                                new OrderRec{orderID=43421, custID=99022, total=20},
                                                new OrderRec{orderID=95421, custID=99021, total=9}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Prakash", orderID=45321, total=50},
                                                new JoinRec{name="Bob", orderID=43421, total=20},
                                                new JoinRec{name="Tim", orderID=95421, total=9},
                                                new JoinRec{name="Robert", orderID=43421, total=20}
            };
                Func<CustomerRec, OrderRec, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.Join(inner, (e) => e.custID, (o) => o.custID, resultSelector);

                return Verification.Allequal(expected, actual);
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

        public class Join8
        {
            // No match between outerKeySelector and innerKeySelector
            public static int Test8()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Prakash", custID=98022},
                                                     new CustomerRec{name="Bob", custID=99022},
                                                     new CustomerRec{name="Tim", custID=99021},
                                                     new CustomerRec{name="Robert", custID=99022}
            };
                OrderRec[] inner = new OrderRec[]{  new OrderRec{orderID=45321, custID=18022, total=50},
                                                new OrderRec{orderID=43421, custID=29022, total=20},
                                                new OrderRec{orderID=95421, custID=39021, total=9}
            };
                JoinRec[] expected = new JoinRec[] { };
                Func<CustomerRec, OrderRec, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.Join(inner, (e) => e.custID, (o) => o.custID, resultSelector);

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

        public class Join9
        {
            // outerKeySelector matches more than one innerKeySelector
            public static int Test9()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Prakash", custID=98022},
                                                     new CustomerRec{name="Bob", custID=99022},
                                                     new CustomerRec{name="Tim", custID=99021},
                                                     new CustomerRec{name="Robert", custID=99022}
            };
                OrderRec[] inner = new OrderRec[]{  new OrderRec{orderID=45321, custID=18022, total=50},
                                                new OrderRec{orderID=43421, custID=29022, total=20},
                                                new OrderRec{orderID=95421, custID=39021, total=9}
            };
                JoinRec[] expected = new JoinRec[] { };
                Func<CustomerRec, OrderRec, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.Join(inner, (e) => e.custID, (o) => o.custID, resultSelector);

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
    }
}
