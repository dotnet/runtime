// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class GroupJoinTests
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
            public int? custID;
        }

        public struct OrderRec
        {
            public int? orderID;
            public int? custID;
            public int? total;
        }

        public struct AnagramRec
        {
            public string name;
            public int? orderID;
            public int? total;
        }

        public struct JoinRec
        {
            public string name;
            public int?[] orderID;
            public int?[] total;
        }

        public class Helper
        {
            public static JoinRec createJoinRec(CustomerRec cr, IEnumerable<OrderRec> orIE)
            {
                int count = 0;

                JoinRec jr = new JoinRec();

                jr.name = cr.name;

                jr.orderID = new int?[orIE.Count()];
                jr.total = new int?[orIE.Count()];

                foreach (OrderRec or in orIE)
                {
                    jr.orderID[count] = or.orderID;
                    jr.total[count] = or.total;
                    count++;
                }

                return jr;
            }

            public static JoinRec createJoinRec(CustomerRec cr, IEnumerable<AnagramRec> arIE)
            {
                int count = 0;

                JoinRec jr = new JoinRec();

                jr.name = cr.name;
                jr.orderID = new int?[arIE.Count()];
                jr.total = new int?[arIE.Count()];

                foreach (AnagramRec ar in arIE)
                {
                    jr.orderID[count] = ar.orderID;
                    jr.total[count] = ar.total;
                    count++;
                }

                return jr;
            }

            // The following verification function will be used when the PLINQ team runs these tests
            // This is a non-order preserving verification function
#if PLINQ

        public static int DataEqual(IEnumerable<JoinRec> ele1, IEnumerable<JoinRec> ele2)
        {
            if ((ele1 == null) && (ele2 == null)) return 0;
            if (ele1.Count() != ele2.Count()) return 1;

            List<JoinRec> elt = new List<JoinRec>(ele1);
            foreach (JoinRec e2 in ele2)
            {
                bool contains = false;
                for (int i = 0; i < elt.Count; i++)
                {
                    JoinRec e1 = elt[i];
                    if (e1.name == e2.name && e1.orderID.Count() == e2.orderID.Count()&& e1.total.Count() == e2.total.Count())
                    {
                        bool eq = true;
                        for (int j = 0; j < e1.orderID.Count(); j++)
                        {
                            if (e1.orderID[j] != e2.orderID[j])
                            {
                                eq = false;
                                break;
                            }
                        }

                        for (int j = 0; j < e1.total.Count(); j++)
                        {
                            if (e1.total[j] != e2.total[j])
                            {
                                eq = false;
                                break;
                            }
                        }

                        if (!eq) continue;
                        elt.RemoveAt(i);
                        contains = true;
                        break;
                    }
                }
                if (!contains) return 1;
            }
            return 0;
        }
#else
            // The following is an order preserving verification function
            public static int DataEqual(IEnumerable<JoinRec> ele1, IEnumerable<JoinRec> ele2)
            {
                if ((ele1 == null) && (ele2 == null)) return 0;
                if (ele1.Count() != ele2.Count()) return 1;

                using (IEnumerator<JoinRec> e1 = ele1.GetEnumerator())
                using (IEnumerator<JoinRec> e2 = ele2.GetEnumerator())
                {
                    while (e1.MoveNext())
                    {
                        e2.MoveNext();
                        JoinRec rec1 = (JoinRec)e1.Current;
                        JoinRec rec2 = (JoinRec)e2.Current;

                        if (rec1.name != rec2.name) return 1;
                        if (rec1.orderID.Count() != rec2.orderID.Count()) return 1;
                        if (rec1.total.Count() != rec2.total.Count()) return 1;

                        int num = rec1.orderID.Count();

                        for (int i = 0; i < num; i++)
                            if (rec1.orderID[i] != rec2.orderID[i]) return 1;

                        num = rec1.total.Count();
                        for (int i = 0; i < num; i++)
                            if (rec1.total[i] != rec2.total[i]) return 1;
                    }
                }
                return 0;
            }
#endif
        }
        public class GroupJoin1
        {
            // outer is empty and inner is non-empty
            public static int Test1()
            {
                CustomerRec[] outer = new CustomerRec[] { };
                OrderRec[] inner = new OrderRec[]{  new OrderRec{orderID=45321, custID=98022, total=50},
                                                new OrderRec{orderID=97865, custID=32103, total=25}
            };
                JoinRec[] expected = new JoinRec[] { };
                Func<CustomerRec, IEnumerable<OrderRec>, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.GroupJoin(inner, (e) => e.custID, (o) => o.custID, resultSelector);

                return Helper.DataEqual(expected, actual);
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

        public class GroupJoin10
        {
            // Overload-2: Test when IEqualityComparer is not-null
            public static int Test10()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=1234},
                                                     new CustomerRec{name="Bob", custID=9865},
                                                     new CustomerRec{name="Robert", custID=9895}
            };
                AnagramRec[] inner = new AnagramRec[]{ new AnagramRec{name = "Robert", orderID=93483, total = 19},
                                                   new AnagramRec{name = "miT", orderID=93489, total = 45}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Tim", orderID=new int?[]{93489}, total=new int?[]{45}},
                                                new JoinRec{name="Bob", orderID=new int?[]{}, total=new int?[]{}},
                                                new JoinRec{name="Robert", orderID=new int?[]{93483}, total=new int?[]{19}}
            };
                Func<CustomerRec, IEnumerable<AnagramRec>, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.GroupJoin(inner, (e) => e.name, (o) => o.name, resultSelector, new AnagramEqualityComparer());

                return Helper.DataEqual(expected, actual);
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

        public class GroupJoin11a
        {
            // Overload-2: Test when outer is null
            public static int Test11a()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=1234},
                                                     new CustomerRec{name="Bob", custID=9865},
                                                     new CustomerRec{name="Robert", custID=9895}
            };
                AnagramRec[] inner = new AnagramRec[]{ new AnagramRec{name = "Robert", orderID=93483, total = 19},
                                                   new AnagramRec{name = "miT", orderID=93489, total = 45}
            };
                Func<CustomerRec, IEnumerable<AnagramRec>, JoinRec> resultSelector = Helper.createJoinRec;

                try
                {
                    outer = null;
                    var actual = outer.GroupJoin(inner, (e) => e.name, (o) => o.name, resultSelector, new AnagramEqualityComparer());
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
                return Test11a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupJoin11b
        {
            // Overload-2: Test when inner is null
            public static int Test11b()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=1234},
                                                     new CustomerRec{name="Bob", custID=9865},
                                                     new CustomerRec{name="Robert", custID=9895}
            };
                AnagramRec[] inner = new AnagramRec[]{ new AnagramRec{name = "Robert", orderID=93483, total = 19},
                                                   new AnagramRec{name = "miT", orderID=93489, total = 45}
            };
                Func<CustomerRec, IEnumerable<AnagramRec>, JoinRec> resultSelector = Helper.createJoinRec;

                try
                {
                    inner = null;
                    var actual = outer.GroupJoin(inner, (e) => e.name, (o) => o.name, resultSelector, new AnagramEqualityComparer());
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
                return Test11b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupJoin11c
        {
            // Overload-2: Test when outerKeySelector is null
            public static int Test11c()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=1234},
                                                     new CustomerRec{name="Bob", custID=9865},
                                                     new CustomerRec{name="Robert", custID=9895}
            };
                AnagramRec[] inner = new AnagramRec[]{ new AnagramRec{name = "Robert", orderID=93483, total = 19},
                                                   new AnagramRec{name = "miT", orderID=93489, total = 45}
            };
                Func<CustomerRec, IEnumerable<AnagramRec>, JoinRec> resultSelector = Helper.createJoinRec;

                try
                {
                    var actual = outer.GroupJoin(inner, null, (o) => o.name, resultSelector, new AnagramEqualityComparer());
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
                return Test11c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupJoin11d
        {
            // Overload-2: Test when innerKeySelector is null
            public static int Test11d()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=1234},
                                                     new CustomerRec{name="Bob", custID=9865},
                                                     new CustomerRec{name="Robert", custID=9895}
            };
                AnagramRec[] inner = new AnagramRec[]{ new AnagramRec{name = "Robert", orderID=93483, total = 19},
                                                   new AnagramRec{name = "miT", orderID=93489, total = 45}
            };
                Func<CustomerRec, IEnumerable<AnagramRec>, JoinRec> resultSelector = Helper.createJoinRec;

                try
                {
                    var actual = outer.GroupJoin(inner, (e) => e.name, null, resultSelector, new AnagramEqualityComparer());
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
                return Test11d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupJoin11e
        {
            // Overload-2: Test when resultSelector is null
            public static int Test11e()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=1234},
                                                     new CustomerRec{name="Bob", custID=9865},
                                                     new CustomerRec{name="Robert", custID=9895}
            };
                AnagramRec[] inner = new AnagramRec[]{ new AnagramRec{name = "Robert", orderID=93483, total = 19},
                                                   new AnagramRec{name = "miT", orderID=93489, total = 45}
            };
                Func<CustomerRec, IEnumerable<AnagramRec>, JoinRec> resultSelector = Helper.createJoinRec;

                try
                {
                    resultSelector = null;
                    var actual = outer.GroupJoin(inner, (e) => e.name, (o) => o.name, resultSelector, new AnagramEqualityComparer());
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
                return Test11e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class GroupJoin12
        {
            // DDB:171937
            public static int Test12()
            {
                string[] outer = new string[] { null };
                string[] inner = new string[] { null };
                string[] expected = new string[] { null };

                Func<string, IEnumerable<string>, string> resultSelector = (string x, IEnumerable<string> y) => x;

                var actual = outer.GroupJoin(inner, (e) => e, (o) => o, resultSelector, EqualityComparer<string>.Default);

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

        public class GroupJoin2
        {
            // outer is non-empty and inner is empty
            public static int Test2()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=43434},
                                                     new CustomerRec{name="Bob", custID=34093}
            };
                OrderRec[] inner = new OrderRec[] { };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Tim", orderID=new int?[]{}, total=new int?[]{}},
                                                new JoinRec{name="Bob", orderID=new int?[]{}, total=new int?[]{}}
            };
                Func<CustomerRec, IEnumerable<OrderRec>, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.GroupJoin(inner, (e) => e.custID, (o) => o.custID, resultSelector);

                return Helper.DataEqual(expected, actual);
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

        public class GroupJoin3
        {
            // outer and inner has only one element and the key matches
            public static int Test3()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=43434}
            };
                OrderRec[] inner = new OrderRec[]{ new OrderRec{orderID=97865, custID=43434, total=25}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Tim", orderID=new int?[]{97865}, total=new int?[]{25}}
            };
                Func<CustomerRec, IEnumerable<OrderRec>, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.GroupJoin(inner, (e) => e.custID, (o) => o.custID, resultSelector);

                return Helper.DataEqual(expected, actual);
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

        public class GroupJoin4
        {
            // outer and inner has only one element and the keys do not match
            public static int Test4()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=43434}
            };
                OrderRec[] inner = new OrderRec[]{ new OrderRec{orderID=97865, custID=49434, total=25}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Tim", orderID=new int?[]{}, total=new int?[]{}}
            };
                Func<CustomerRec, IEnumerable<OrderRec>, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.GroupJoin(inner, (e) => e.custID, (o) => o.custID, resultSelector);

                return Helper.DataEqual(expected, actual);
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

        public class GroupJoin5
        {
            // innerKeySelector, outerKeySelector returns null
            // Also tests outerKeySelector matches more than one innerKeySelector
            public static int Test5()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=null},
                                                     new CustomerRec{name="Bob", custID=null}
            };
                OrderRec[] inner = new OrderRec[]{ new OrderRec{orderID=97865, custID=null, total=25},
                                               new OrderRec{orderID=34390, custID=null, total=19}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Tim", orderID=new int?[]{}, total=new int?[]{}},
                                                new JoinRec{name="Bob", orderID=new int?[]{}, total=new int?[]{}}
            };
                Func<CustomerRec, IEnumerable<OrderRec>, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.GroupJoin(inner, (e) => e.custID, (o) => o.custID, resultSelector);

                return Helper.DataEqual(expected, actual);
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

        public class GroupJoin6
        {
            // innerKeySelector produces same key for more than one element
            // and is matched by one of the outerKeySelector.
            public static int Test6()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=1234},
                                                     new CustomerRec{name="Bob", custID=9865}
            };
                OrderRec[] inner = new OrderRec[]{ new OrderRec{orderID=97865, custID=1234, total=25},
                                               new OrderRec{orderID=34390, custID=1234, total=19},
                                               new OrderRec{orderID=34390, custID=9865, total=19}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Tim", orderID=new int?[]{97865, 34390}, total=new int?[]{25, 19}},
                                                new JoinRec{name="Bob", orderID=new int?[]{34390}, total=new int?[]{19}}
            };
                Func<CustomerRec, IEnumerable<OrderRec>, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.GroupJoin(inner, (e) => e.custID, (o) => o.custID, resultSelector);

                return Helper.DataEqual(expected, actual);
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

        public class GroupJoin7
        {
            // outerKeySelector produces same key for more than one element
            // and is matched by one of the innerKeySelector.
            public static int Test7()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=1234},
                                                     new CustomerRec{name="Bob", custID=9865},
                                                      new CustomerRec{name="Robert", custID=9865}
            };
                OrderRec[] inner = new OrderRec[]{ new OrderRec{orderID=97865, custID=1234, total=25},
                                               new OrderRec{orderID=34390, custID=9865, total=19}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Tim", orderID=new int?[]{97865}, total=new int?[]{25}},
                                                new JoinRec{name="Bob", orderID=new int?[]{34390}, total=new int?[]{19}},
                                                new JoinRec{name="Robert", orderID=new int?[]{34390}, total=new int?[]{19}}
            };
                Func<CustomerRec, IEnumerable<OrderRec>, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.GroupJoin(inner, (e) => e.custID, (o) => o.custID, resultSelector);

                return Helper.DataEqual(expected, actual);
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

        public class GroupJoin8
        {
            // No match between innerKeySelector and outerKeySelector
            public static int Test8()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=1234},
                                                     new CustomerRec{name="Bob", custID=9865},
                                                     new CustomerRec{name="Robert", custID=9895}
            };
                OrderRec[] inner = new OrderRec[]{ new OrderRec{orderID=97865, custID=2334, total=25},
                                               new OrderRec{orderID=34390, custID=9065, total=19}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Tim", orderID=new int?[]{}, total=new int?[]{}},
                                                new JoinRec{name="Bob", orderID=new int?[]{}, total=new int?[]{}},
                                                new JoinRec{name="Robert", orderID=new int?[]{}, total=new int?[]{}}
            };
                Func<CustomerRec, IEnumerable<OrderRec>, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.GroupJoin(inner, (e) => e.custID, (o) => o.custID, resultSelector);

                return Helper.DataEqual(expected, actual);
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

        public class GroupJoin9
        {
            // Overload-2: Test when IEqualityComparer is null
            public static int Test9()
            {
                CustomerRec[] outer = new CustomerRec[]{ new CustomerRec{name="Tim", custID=1234},
                                                     new CustomerRec{name="Bob", custID=9865},
                                                     new CustomerRec{name="Robert", custID=9895}
            };
                AnagramRec[] inner = new AnagramRec[]{ new AnagramRec{name = "Robert", orderID=93483, total = 19},
                                                   new AnagramRec{name = "miT", orderID=93489, total = 45}
            };
                JoinRec[] expected = new JoinRec[]{ new JoinRec{name="Tim", orderID=new int?[]{}, total=new int?[]{}},
                                                new JoinRec{name="Bob", orderID=new int?[]{}, total=new int?[]{}},
                                                new JoinRec{name="Robert", orderID=new int?[]{93483}, total=new int?[]{19}}
            };
                Func<CustomerRec, IEnumerable<AnagramRec>, JoinRec> resultSelector = Helper.createJoinRec;

                var actual = outer.GroupJoin(inner, (e) => e.name, (o) => o.name, resultSelector, null);

                return Helper.DataEqual(expected, actual);
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
