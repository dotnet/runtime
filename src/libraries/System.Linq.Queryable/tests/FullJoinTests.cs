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

        public struct JoinRec
        {
            public string name;
            public int orderID;
            public int total;
        }

        [Fact]
        public void MixedMatchAndUnmatched()
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
                new JoinRec{ name = string.Empty, orderID = 43421, total = 20 }
            };

            Assert.Equal(expected, outer.AsQueryable().FullJoin(inner.AsQueryable(), e => e.custID, e => e.custID, (cr, or) => new JoinRec { name = cr.name ?? string.Empty, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void NullComparer()
        {
            CustomerRec[] outer = {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 }
            };
            OrderRec[] inner = {
                new OrderRec{ orderID = 45321, custID = 98022, total = 50 },
                new OrderRec{ orderID = 95421, custID = 99021, total = 9 }
            };
            JoinRec[] expected = {
                new JoinRec{ name = "Prakash", orderID = 45321, total = 50 },
                new JoinRec{ name = "Tim", orderID = 95421, total = 9 }
            };

            Assert.Equal(expected, outer.AsQueryable().FullJoin(inner.AsQueryable(), e => e.custID, e => e.custID, (cr, or) => new JoinRec { name = cr.name ?? string.Empty, orderID = or.orderID, total = or.total }, null));
        }

        [Fact]
        public void OuterNull()
        {
            IQueryable<CustomerRec> outer = null;
            OrderRec[] inner = { new OrderRec { orderID = 45321, custID = 98022, total = 50 } };

            AssertExtensions.Throws<ArgumentNullException>("outer", () => outer.FullJoin(inner.AsQueryable(), e => e.custID, e => e.custID, (cr, or) => new JoinRec { name = cr.name ?? string.Empty, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void InnerNull()
        {
            CustomerRec[] outer = { new CustomerRec { name = "Prakash", custID = 98022 } };
            IQueryable<OrderRec> inner = null;

            AssertExtensions.Throws<ArgumentNullException>("inner", () => outer.AsQueryable().FullJoin(inner, e => e.custID, e => e.custID, (cr, or) => new JoinRec { name = cr.name ?? string.Empty, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void OuterKeySelectorNull()
        {
            CustomerRec[] outer = { new CustomerRec { name = "Prakash", custID = 98022 } };
            OrderRec[] inner = { new OrderRec { orderID = 45321, custID = 98022, total = 50 } };

            AssertExtensions.Throws<ArgumentNullException>("outerKeySelector", () => outer.AsQueryable().FullJoin(inner.AsQueryable(), null, e => e.custID, (cr, or) => new JoinRec { name = cr.name ?? string.Empty, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void InnerKeySelectorNull()
        {
            CustomerRec[] outer = { new CustomerRec { name = "Prakash", custID = 98022 } };
            OrderRec[] inner = { new OrderRec { orderID = 45321, custID = 98022, total = 50 } };

            AssertExtensions.Throws<ArgumentNullException>("innerKeySelector", () => outer.AsQueryable().FullJoin(inner.AsQueryable(), e => e.custID, null, (cr, or) => new JoinRec { name = cr.name ?? string.Empty, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void ResultSelectorNull()
        {
            CustomerRec[] outer = { new CustomerRec { name = "Prakash", custID = 98022 } };
            OrderRec[] inner = { new OrderRec { orderID = 45321, custID = 98022, total = 50 } };

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => outer.AsQueryable().FullJoin(inner.AsQueryable(), e => e.custID, e => e.custID, (Expression<Func<CustomerRec, OrderRec, JoinRec>>)null));
        }

        [Fact]
        public void OuterNullWithComparer()
        {
            IQueryable<CustomerRec> outer = null;
            OrderRec[] inner = { new OrderRec { orderID = 45321, custID = 98022, total = 50 } };

            AssertExtensions.Throws<ArgumentNullException>("outer", () => outer.FullJoin(inner.AsQueryable(), e => e.custID, e => e.custID, (cr, or) => new JoinRec { name = cr.name ?? string.Empty, orderID = or.orderID, total = or.total }, EqualityComparer<int>.Default));
        }

        [Fact]
        public void ResultSelectorNullWithComparer()
        {
            CustomerRec[] outer = { new CustomerRec { name = "Prakash", custID = 98022 } };
            OrderRec[] inner = { new OrderRec { orderID = 45321, custID = 98022, total = 50 } };

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => outer.AsQueryable().FullJoin(inner.AsQueryable(), e => e.custID, e => e.custID, (Expression<Func<CustomerRec, OrderRec, JoinRec>>)null, EqualityComparer<int>.Default));
        }

        [Fact]
        public void FullJoinNoComparer()
        {
            var count = new[] { 0, 1, 2 }.AsQueryable().FullJoin(new[] { 1, 2, 3 }, n1 => n1, n2 => n2, (n1, n2) => n1 + n2).Count();
            Assert.Equal(4, count); // 0-unmatched, 1+1, 2+2, 3-unmatched
        }

        [Fact]
        public void FullJoinWithComparer()
        {
            var count = new[] { 0, 1, 2 }.AsQueryable().FullJoin(new[] { 1, 2, 3 }, n1 => n1, n2 => n2, (n1, n2) => n1 + n2, EqualityComparer<int>.Default).Count();
            Assert.Equal(4, count);
        }
    }
}
