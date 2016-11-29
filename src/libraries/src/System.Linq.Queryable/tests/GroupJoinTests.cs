// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class GroupJoinTests : EnumerableBasedTests
    {
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

        [Fact]
        public void OuterEmptyInnerNonEmpty()
        {
            CustomerRec[] outer = { };
            OrderRec[] inner = new []
            {
                new OrderRec{ orderID = 45321, custID = 98022, total = 50 },
                new OrderRec{ orderID = 97865, custID = 32103, total = 25 }
            };
            Assert.Empty(outer.AsQueryable().GroupJoin(inner.AsQueryable(), e => e.custID, e => e.custID, (cr, orIE) => new JoinRec { name = cr.name, orderID = orIE.Select(o => o.orderID).ToArray(), total = orIE.Select(o => o.total).ToArray() }));
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

            Assert.Equal(expected, outer.AsQueryable().GroupJoin(inner.AsQueryable(), e => e.name, e => e.name, (cr, arIE) => new JoinRec { name = cr.name, orderID = arIE.Select(o => o.orderID).ToArray(), total = arIE.Select(o => o.total).ToArray() }, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterNull()
        {
            IQueryable<CustomerRec> outer = null;
            AnagramRec[] inner = new AnagramRec[]
            {
                new AnagramRec{ name = "Robert", orderID = 93483, total = 19 },
                new AnagramRec{ name = "miT", orderID = 93489, total = 45 }
            };
            
            Assert.Throws<ArgumentNullException>("outer", () => outer.GroupJoin(inner.AsQueryable(), e => e.name, e => e.name, (cr, arIE) => new JoinRec { name = cr.name, orderID = arIE.Select(o => o.orderID).ToArray(), total = arIE.Select(o => o.total).ToArray() }, new AnagramEqualityComparer()));
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
            IQueryable<AnagramRec> inner = null;
            
            Assert.Throws<ArgumentNullException>("inner", () => outer.AsQueryable().GroupJoin(inner, e => e.name, e => e.name, (cr, arIE) => new JoinRec { name = cr.name, orderID = arIE.Select(o => o.orderID).ToArray(), total = arIE.Select(o => o.total).ToArray() }, new AnagramEqualityComparer()));
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
            
            Assert.Throws<ArgumentNullException>("outerKeySelector", () => outer.AsQueryable().GroupJoin(inner.AsQueryable(), null, e => e.name, (cr, arIE) => new JoinRec { name = cr.name, orderID = arIE.Select(o => o.orderID).ToArray(), total = arIE.Select(o => o.total).ToArray() }, new AnagramEqualityComparer()));
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
            
            Assert.Throws<ArgumentNullException>("innerKeySelector", () => outer.AsQueryable().GroupJoin(inner.AsQueryable(), e => e.name, null, (cr, arIE) => new JoinRec { name = cr.name, orderID = arIE.Select(o => o.orderID).ToArray(), total = arIE.Select(o => o.total).ToArray() }, new AnagramEqualityComparer()));
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
            
            Assert.Throws<ArgumentNullException>("resultSelector", () => outer.AsQueryable().GroupJoin(inner.AsQueryable(), e => e.name, e => e.name, (Expression<Func<CustomerRec, IEnumerable<AnagramRec>, JoinRec>>)null, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterNullNoComparer()
        {
            IQueryable<CustomerRec> outer = null;
            AnagramRec[] inner = new AnagramRec[]
            {
                new AnagramRec{ name = "Robert", orderID = 93483, total = 19 },
                new AnagramRec{ name = "miT", orderID = 93489, total = 45 }
            };

            Assert.Throws<ArgumentNullException>("outer", () => outer.GroupJoin(inner.AsQueryable(), e => e.name, e => e.name, (cr, arIE) => new JoinRec { name = cr.name, orderID = arIE.Select(o => o.orderID).ToArray(), total = arIE.Select(o => o.total).ToArray() }));
        }

        [Fact]
        public void InnerNullNoComparer()
        {
            CustomerRec[] outer = new[]
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 },
                new CustomerRec{ name = "Robert", custID = 9895 }
            };
            IQueryable<AnagramRec> inner = null;

            Assert.Throws<ArgumentNullException>("inner", () => outer.AsQueryable().GroupJoin(inner, e => e.name, e => e.name, (cr, arIE) => new JoinRec { name = cr.name, orderID = arIE.Select(o => o.orderID).ToArray(), total = arIE.Select(o => o.total).ToArray() }));
        }

        [Fact]
        public void OuterKeySelectorNullNoComparer()
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

            Assert.Throws<ArgumentNullException>("outerKeySelector", () => outer.AsQueryable().GroupJoin(inner.AsQueryable(), null, e => e.name, (cr, arIE) => new JoinRec { name = cr.name, orderID = arIE.Select(o => o.orderID).ToArray(), total = arIE.Select(o => o.total).ToArray() }));
        }

        [Fact]
        public void InnerKeySelectorNullNoComparer()
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

            Assert.Throws<ArgumentNullException>("innerKeySelector", () => outer.AsQueryable().GroupJoin(inner.AsQueryable(), e => e.name, null, (cr, arIE) => new JoinRec { name = cr.name, orderID = arIE.Select(o => o.orderID).ToArray(), total = arIE.Select(o => o.total).ToArray() }));
        }

        [Fact]
        public void ResultSelectorNullNoComparer()
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

            Assert.Throws<ArgumentNullException>("resultSelector", () => outer.AsQueryable().GroupJoin(inner.AsQueryable(), e => e.name, e => e.name, (Expression<Func<CustomerRec, IEnumerable<AnagramRec>, JoinRec>>)null));
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

            Assert.Equal(expected, outer.AsQueryable().GroupJoin(inner.AsQueryable(), e => e.name, e => e.name, (cr, arIE) => new JoinRec { name = cr.name, orderID = arIE.Select(o => o.orderID).ToArray(), total = arIE.Select(o => o.total).ToArray() }, null));
        }

        [Fact]
        public void GroupJoin1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().GroupJoin(new int[] { 1, 2, 3 }, n1 => n1, n2 => n2, (n1, n2) => n1).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void GroupJoin2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().GroupJoin(new int[] { 1, 2, 3 }, n1 => n1, n2 => n2, (n1, n2) => n1, EqualityComparer<int>.Default).Count();
            Assert.Equal(3, count);
        }
    }
}
