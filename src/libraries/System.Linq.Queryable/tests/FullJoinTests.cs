// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class FullJoinTests : EnumerableBasedTests
    {
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

        [Fact]
        public void FirstOuterMatchesLastInnerLastOuterMatchesFirstInnerSameNumberElements()
        {
            CustomerRec[] outer = {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            OrderRec[] inner = {
                new OrderRec{ orderID = 45321, custID = 99022, total = 50 },
                new OrderRec{ orderID = 43421, custID = 29022, total = 20 },
                new OrderRec{ orderID = 95421, custID = 98022, total = 9 }
            };
            JoinRec[] expected = {
                new JoinRec{ name = "Prakash", orderID = 95421, total = 9 },
                new JoinRec{ name = "Tim", orderID = 0, total = 0 },
                new JoinRec{ name = "Robert", orderID = 45321, total = 50 },
                new JoinRec{ name = null, orderID = 43421, total = 20 }
            };

            Assert.Equal(expected, outer.AsQueryable().FullJoin(inner.AsQueryable(), e => e.custID, e => e.custID, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void NullComparer()
        {
            CustomerRec[] outer = {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };
            JoinRec[] expected = {
                new JoinRec{ name = "Prakash", orderID = 323232, total = 9 },
                new JoinRec{ name = "Tim", orderID = 0, total = 0 },
                new JoinRec{ name = "Robert", orderID = 0, total = 0 },
                new JoinRec{ name = null, orderID = 43455, total = 10 }
            };

            Assert.Equal(expected, outer.AsQueryable().FullJoin(inner.AsQueryable(), e => e.name, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }, null));
        }

        [Fact]
        public void CustomComparer()
        {
            CustomerRec[] outer = {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };
            JoinRec[] expected = {
                new JoinRec{ name = "Prakash", orderID = 323232, total = 9 },
                new JoinRec{ name = "Tim", orderID = 43455, total = 10 },
                new JoinRec{ name = "Robert", orderID = 0, total = 0 }
            };

            Assert.Equal(expected, outer.AsQueryable().FullJoin(inner.AsQueryable(), e => e.name, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterNull()
        {
            IQueryable<CustomerRec> outer = null;
            AnagramRec[] inner = {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("outer", () => outer.FullJoin(inner.AsQueryable(), e => e.name, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }, new AnagramEqualityComparer()));
        }

        [Fact]
        public void InnerNull()
        {
            CustomerRec[] outer = {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            IQueryable<AnagramRec> inner = null;

            AssertExtensions.Throws<ArgumentNullException>("inner", () => outer.AsQueryable().FullJoin(inner, e => e.name, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterKeySelectorNull()
        {
            CustomerRec[] outer = {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("outerKeySelector", () => outer.AsQueryable().FullJoin(inner.AsQueryable(), null, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }, new AnagramEqualityComparer()));
        }

        [Fact]
        public void InnerKeySelectorNull()
        {
            CustomerRec[] outer = {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("innerKeySelector", () => outer.AsQueryable().FullJoin(inner.AsQueryable(), e => e.name, null, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }, new AnagramEqualityComparer()));
        }

        [Fact]
        public void ResultSelectorNull()
        {
            CustomerRec[] outer = {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => Queryable.FullJoin<CustomerRec, AnagramRec, string, JoinRec>(outer.AsQueryable(), inner.AsQueryable(), e => e.name, e => e.name, (Expression<Func<CustomerRec, AnagramRec, JoinRec>>)null, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterNullNoComparer()
        {
            IQueryable<CustomerRec> outer = null;
            AnagramRec[] inner = {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("outer", () => outer.FullJoin(inner.AsQueryable(), e => e.name, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void InnerNullNoComparer()
        {
            CustomerRec[] outer = {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            IQueryable<AnagramRec> inner = null;

            AssertExtensions.Throws<ArgumentNullException>("inner", () => outer.AsQueryable().FullJoin(inner, e => e.name, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void OuterKeySelectorNullNoComparer()
        {
            CustomerRec[] outer = {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("outerKeySelector", () => outer.AsQueryable().FullJoin(inner.AsQueryable(), null, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void InnerKeySelectorNullNoComparer()
        {
            CustomerRec[] outer = {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("innerKeySelector", () => outer.AsQueryable().FullJoin(inner.AsQueryable(), e => e.name, null, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void ResultSelectorNullNoComparer()
        {
            CustomerRec[] outer = {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => Queryable.FullJoin<CustomerRec, AnagramRec, string, JoinRec>(outer.AsQueryable(), inner.AsQueryable(), e => e.name, e => e.name, (Expression<Func<CustomerRec, AnagramRec, JoinRec>>)null));
        }

        [Fact]
        public void SelectorsReturnNull()
        {
            int?[] outer = { null, null };
            int?[] inner = { null, null, null };
            int?[] expected = { null, null, null, null, null };

            Assert.Equal(expected, outer.AsQueryable().FullJoin(inner.AsQueryable(), e => e, e => e, (x, y) => x));
            Assert.Equal(expected, outer.AsQueryable().FullJoin(inner.AsQueryable(), e => e, e => e, (x, y) => y));
        }

        [Fact]
        public void NullKeysAreUnmatchedButPreserved()
        {
            string[] outer = { "#o1", "a", "#o2" };
            string[] inner = { "#i1", "A", "#i2", "b" };
            (string? Outer, string? Inner)[] expected =
            {
                ("#o1", null),
                ("a", "A"),
                ("#o2", null),
                (null, "#i1"),
                (null, "#i2"),
                (null, "b")
            };

            Assert.Equal(expected, outer.AsQueryable().FullJoin(inner.AsQueryable(), s => s[0] == '#' ? null : s, s => s[0] == '#' ? null : s, StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void Join1()
        {
            var count = new[] { 0, 1, 2 }.AsQueryable().FullJoin(new[] { 1, 2, 3 }, n1 => n1, n2 => n2, (n1, n2) => n1 + n2).Count();
            Assert.Equal(4, count);
        }

        [Fact]
        public void Join2()
        {
            var count = new[] { 0, 1, 2 }.AsQueryable().FullJoin(new[] { 1, 2, 3 }, n1 => n1, n2 => n2, (n1, n2) => n1 + n2, EqualityComparer<int>.Default).Count();
            Assert.Equal(4, count);
        }

        [Fact]
        public void TupleOverload()
        {
            var result = new[] { 0, 1, 2 }.AsQueryable().FullJoin(new[] { 1, 2, 3 }.AsQueryable(), n1 => n1, n2 => n2).ToArray();
            Assert.Equal(4, result.Length);
            Assert.Equal((0, 0), result[0]);
            Assert.Equal((1, 1), result[1]);
            Assert.Equal((2, 2), result[2]);
            Assert.Equal((0, 3), result[3]);
        }

        [Fact]
        public void TupleOverloadWithComparer()
        {
            var result = new[] { 0, 1, 2 }.AsQueryable().FullJoin(new[] { 1, 2, 3 }.AsQueryable(), n1 => n1, n2 => n2, EqualityComparer<int>.Default).ToArray();
            Assert.Equal(4, result.Length);
            Assert.Equal((0, 0), result[0]);
            Assert.Equal((1, 1), result[1]);
            Assert.Equal((2, 2), result[2]);
            Assert.Equal((0, 3), result[3]);
        }
    }
}
