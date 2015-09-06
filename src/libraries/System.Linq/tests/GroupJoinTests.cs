// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class GroupJoinTests
    {
        private class AnagramEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null | y == null) return false;
                int length = x.Length;
                if (length != y.Length) return false;
                using (var en = x.OrderBy(i => i).GetEnumerator())
                {
                    foreach (char c in y.OrderBy(i => i))
                    {
                        en.MoveNext();
                        if (c != en.Current) return false;
                    }
                }
                return true;
            }

            public int GetHashCode(string obj)
            {
                int hash = 0;
                foreach (char c in obj)
                    hash ^= (int)c;
                return hash;
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

        public struct JoinRec : IEquatable<JoinRec>
        {
            public string name;
            public int?[] orderID;
            public int?[] total;
            
            public override int GetHashCode()
            {
                // Not great, but it'll serve.
                return name.GetHashCode() ^ orderID.Length ^ (total.Length * 31);
            }
            
            public bool Equals(JoinRec other)
            {
                if (!string.Equals(name, other.name)) return false;
                if (orderID == null)
                {
                    if (other.orderID != null) return false;
                }
                else
                {
                    if (other.orderID == null) return false;
                    if (orderID.Length != other.orderID.Length) return false;
                    for (int i = 0; i != other.orderID.Length; ++i)
                        if (orderID[i] != other.orderID[i]) return false;
                }
                if (total == null)
                {
                    if (other.total != null) return false;
                }
                else
                {
                    if (other.total == null) return false;
                    if (total.Length != other.total.Length) return false;
                    for (int i = 0; i != other.total.Length; ++i)
                        if (total[i] != other.total[i]) return false;
                }
                return true;
            }
            
            public override bool Equals(object obj)
            {
                return obj is JoinRec && Equals((JoinRec)obj);
            }
        }

        public static JoinRec createJoinRec(CustomerRec cr, IEnumerable<OrderRec> orIE)
        {
            return new JoinRec
            {
                name = cr.name,
                orderID = orIE.Select(o => o.orderID).ToArray(),
                total = orIE.Select(o => o.total).ToArray(),
            };
        }

        public static JoinRec createJoinRec(CustomerRec cr, IEnumerable<AnagramRec> arIE)
        {
            return new JoinRec
            {
                name = cr.name,
                orderID = arIE.Select(o => o.orderID).ToArray(),
                total = arIE.Select(o => o.total).ToArray(),
            };
        }

        [Fact]
        public void OuterEmptyInnerNonEmpty()
        {
            CustomerRec[] outer = { };
            OrderRec[] inner = new []
            {
                new OrderRec{ orderID = 45321, custID = 98022, total = 50 },
                new OrderRec{ orderID = 97865, custID = 32103, total = 25 }
            };
            JoinRec[] expected = { };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void CustomComparer()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 },
                new CustomerRec{ name = "Robert", custID = 9895 }
            };
            AnagramRec[] inner = new []
            {
                new AnagramRec{ name = "Robert", orderID = 93483, total = 19 },
                new AnagramRec{ name = "miT", orderID = 93489, total = 45 }
            };
            JoinRec[] expected = new []
            {
                new JoinRec{ name = "Tim", orderID = new int?[]{ 93489 }, total = new int?[]{ 45 } },
                new JoinRec{ name = "Bob", orderID = new int?[]{ }, total = new int?[]{ } },
                new JoinRec{ name = "Robert", orderID = new int?[]{ 93483 }, total = new int?[]{ 19 } }
            };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e.name, e => e.name, createJoinRec, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterNull()
        {
            CustomerRec[] outer = null;
            AnagramRec[] inner = new AnagramRec[]
            {
                new AnagramRec{ name = "Robert", orderID = 93483, total = 19 },
                new AnagramRec{ name = "miT", orderID = 93489, total = 45 }
            };
            
            Assert.Throws<ArgumentNullException>("outer", () => outer.GroupJoin(inner, e => e.name, e => e.name, createJoinRec, new AnagramEqualityComparer()));
        }

        [Fact]
        public void InnerNull()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 },
                new CustomerRec{ name = "Robert", custID = 9895 }
            };
            AnagramRec[] inner = null;
            
            Assert.Throws<ArgumentNullException>("inner", () => outer.GroupJoin(inner, e => e.name, e => e.name, createJoinRec, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterKeySelectorNull()
        {
            CustomerRec[] outer = new CustomerRec[]
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 },
                new CustomerRec{ name = "Robert", custID = 9895 }
            };
            AnagramRec[] inner = new AnagramRec[]
            {
                new AnagramRec{ name = "Robert", orderID = 93483, total = 19 },
                new AnagramRec{ name = "miT", orderID = 93489, total = 45 }
            };
            
            Assert.Throws<ArgumentNullException>("outerKeySelector", () => outer.GroupJoin(inner, null, e => e.name, createJoinRec, new AnagramEqualityComparer()));
        }

        [Fact]
        public void InnerKeySelectorNull()
        {
            CustomerRec[] outer = new CustomerRec[]
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 },
                new CustomerRec{ name = "Robert", custID = 9895 }
            };
            AnagramRec[] inner = new AnagramRec[]
            {
                new AnagramRec{ name = "Robert", orderID = 93483, total = 19 },
                new AnagramRec{ name = "miT", orderID = 93489, total = 45 }
            };
            
            Assert.Throws<ArgumentNullException>("innerKeySelector", () => outer.GroupJoin(inner, e => e.name, null, createJoinRec, new AnagramEqualityComparer()));
        }

        [Fact]
        public void ResultSelectorNull()
        {
            CustomerRec[] outer = new CustomerRec[]
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 },
                new CustomerRec{ name = "Robert", custID = 9895 }
            };
            AnagramRec[] inner = new AnagramRec[]
            {
                new AnagramRec{ name = "Robert", orderID = 93483, total = 19 },
                new AnagramRec{ name = "miT", orderID = 93489, total = 45 }
            };
            
            Assert.Throws<ArgumentNullException>("resultSelector", () => outer.GroupJoin(inner, e => e.name, e => e.name, (Func<CustomerRec, IEnumerable<AnagramRec>, JoinRec>)null, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterInnerBothSingleNullElement()
        {
            string[] outer = new string[] { null };
            string[] inner = new string[] { null };
            string[] expected = new string[] { null };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e, e => e, (x, y) => x, EqualityComparer<string>.Default));
        }

        [Fact]
        public void OuterNonEmptyInnerEmpty()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Tim", custID = 43434 },
                new CustomerRec{ name = "Bob", custID = 34093 }
            };
            OrderRec[] inner = { };
            JoinRec[] expected = new []
            {
                new JoinRec{ name = "Tim", orderID = new int?[]{ }, total = new int?[]{ } },
                new JoinRec{ name = "Bob", orderID = new int?[]{ }, total = new int?[]{ } }
            };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void SingleElementEachAndMatches()
        {
            CustomerRec[] outer = new [] { new CustomerRec{ name = "Tim", custID = 43434 } };
            OrderRec[] inner = new [] { new OrderRec{ orderID = 97865, custID = 43434, total = 25 } };
            JoinRec[] expected = new [] { new JoinRec{ name = "Tim", orderID = new int?[]{ 97865 }, total = new int?[]{ 25 } } };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void SingleElementEachAndDoesntMatch()
        {
            CustomerRec[] outer = new [] { new CustomerRec{ name = "Tim", custID = 43434 } };
            OrderRec[] inner = new [] { new OrderRec{ orderID = 97865, custID = 49434, total = 25 } };
            JoinRec[] expected = new JoinRec[] { new JoinRec{ name = "Tim", orderID = new int?[]{ }, total = new int?[]{ } } };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void SelectorsReturnNull()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Tim", custID = null },
                new CustomerRec{ name = "Bob", custID = null }
            };
            OrderRec[] inner = new []
            {
                new OrderRec{ orderID = 97865, custID = null, total = 25 },
                new OrderRec{ orderID = 34390, custID = null, total = 19 }
            };
            JoinRec[] expected = new []
            {
                new JoinRec{ name = "Tim", orderID = new int?[]{ }, total = new int?[]{ } },
                new JoinRec{ name = "Bob", orderID = new int?[]{ }, total = new int?[]{ } }
            };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void InnerSameKeyMoreThanOneElementAndMatches()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 }
            };
            OrderRec[] inner = new []
            {
                new OrderRec{ orderID = 97865, custID = 1234, total = 25 },
                new OrderRec{ orderID = 34390, custID = 1234, total = 19 },
                new OrderRec{ orderID = 34390, custID = 9865, total = 19 }
            };
            JoinRec[] expected = new []
            {
                new JoinRec { name = "Tim", orderID = new int?[]{ 97865, 34390 }, total = new int?[] { 25, 19 } },
                new JoinRec { name = "Bob", orderID = new int?[]{ 34390 }, total = new int?[]{ 19 } }
            };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void OuterSameKeyMoreThanOneElementAndMatches()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 },
                new CustomerRec{ name = "Robert", custID = 9865 }
            };
            OrderRec[] inner = new []
            {
                new OrderRec{ orderID = 97865, custID = 1234, total = 25 },
                new OrderRec{ orderID = 34390, custID = 9865, total = 19 }
            };
            JoinRec[] expected = new []
            {
                new JoinRec { name = "Tim", orderID = new int?[]{ 97865 }, total = new int?[]{ 25 } },
                new JoinRec { name = "Bob", orderID = new int?[]{ 34390 }, total = new int?[]{ 19 } },
                new JoinRec { name = "Robert", orderID = new int?[]{ 34390 }, total = new int?[]{ 19 } }
            };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void NoMatches()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 },
                new CustomerRec{ name = "Robert", custID = 9895 }
            };
            OrderRec[] inner = new []
            {
                new OrderRec{ orderID = 97865, custID = 2334, total = 25 },
                new OrderRec{ orderID = 34390, custID = 9065, total = 19 }
            };
            JoinRec[] expected = new []
            {
                new JoinRec{ name = "Tim", orderID = new int?[]{ }, total = new int?[]{ } },
                new JoinRec{ name = "Bob", orderID = new int?[]{ }, total = new int?[]{ } },
                new JoinRec{ name = "Robert", orderID = new int?[]{ }, total = new int?[]{ } }
            };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void NullComparer()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 },
                new CustomerRec{ name = "Robert", custID = 9895 }
            };
            AnagramRec[] inner = new []
            {
                new AnagramRec{ name = "Robert", orderID = 93483, total = 19 },
                new AnagramRec{ name = "miT", orderID = 93489, total = 45 }
            };
            JoinRec[] expected = new []
            {
                new JoinRec{ name = "Tim", orderID = new int?[]{ }, total = new int?[]{ } },
                new JoinRec{ name = "Bob", orderID = new int?[]{ }, total = new int?[]{ } },
                new JoinRec{ name = "Robert", orderID = new int?[]{ 93483 }, total = new int?[]{ 19 } }
            };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e.name, e => e.name, createJoinRec, null));
        }
    }
}
