// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class JoinTests : EnumerableBasedTests
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
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            OrderRec[] inner = new []
            {
                new OrderRec{ orderID = 45321, custID = 99022, total = 50 },
                new OrderRec{ orderID = 43421, custID = 29022, total = 20 },
                new OrderRec{ orderID = 95421, custID = 98022, total = 9 }
            };
            JoinRec[] expected = new []
            {
                new JoinRec{ name = "Prakash", orderID = 95421, total = 9 },
                new JoinRec{ name = "Robert", orderID = 45321, total = 50 }
            };

            Assert.Equal(expected, outer.AsQueryable().Join(inner.AsQueryable(), e => e.custID, e => e.custID, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void NullComparer()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = new []
            {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };
            JoinRec[] expected = new [] { new JoinRec{ name = "Prakash", orderID = 323232, total = 9 } };

            Assert.Equal(expected, outer.AsQueryable().Join(inner.AsQueryable(), e => e.name, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }, null));
        }

        [Fact]
        public void CustomComparer()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = new []
            {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };
            JoinRec[] expected = new []
            {
                new JoinRec{ name = "Prakash", orderID = 323232, total = 9 },
                new JoinRec{ name = "Tim", orderID = 43455, total = 10 }
            };

            Assert.Equal(expected, outer.AsQueryable().Join(inner.AsQueryable(), e => e.name, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterNull()
        {
            IQueryable<CustomerRec> outer = null;
            AnagramRec[] inner = new []
            {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("outer", () => outer.Join(inner.AsQueryable(), e => e.name, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }, new AnagramEqualityComparer()));
        }

        [Fact]
        public void InnerNull()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            IQueryable<AnagramRec> inner = null;

            AssertExtensions.Throws<ArgumentNullException>("inner", () => outer.AsQueryable().Join(inner, e => e.name, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterKeySelectorNull()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = new []
            {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("outerKeySelector", () => outer.AsQueryable().Join(inner.AsQueryable(), null, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }, new AnagramEqualityComparer()));
        }

        [Fact]
        public void InnerKeySelectorNull()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = new []
            {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("innerKeySelector", () => outer.AsQueryable().Join(inner.AsQueryable(), e => e.name, null, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }, new AnagramEqualityComparer()));
        }

        [Fact]
        public void ResultSelectorNull()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = new []
            {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => outer.AsQueryable().Join(inner.AsQueryable(), e => e.name, e => e.name, (Expression<Func<CustomerRec, AnagramRec, JoinRec>>)null, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterNullNoComparer()
        {
            IQueryable<CustomerRec> outer = null;
            AnagramRec[] inner = new[]
            {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("outer", () => outer.Join(inner.AsQueryable(), e => e.name, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void InnerNullNoComparer()
        {
            CustomerRec[] outer = new[]
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            IQueryable<AnagramRec> inner = null;

            AssertExtensions.Throws<ArgumentNullException>("inner", () => outer.AsQueryable().Join(inner, e => e.name, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void OuterKeySelectorNullNoComparer()
        {
            CustomerRec[] outer = new[]
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = new[]
            {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("outerKeySelector", () => outer.AsQueryable().Join(inner.AsQueryable(), null, e => e.name, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void InnerKeySelectorNullNoComparer()
        {
            CustomerRec[] outer = new[]
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = new[]
            {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("innerKeySelector", () => outer.AsQueryable().Join(inner.AsQueryable(), e => e.name, null, (cr, or) => new JoinRec { name = cr.name, orderID = or.orderID, total = or.total }));
        }

        [Fact]
        public void ResultSelectorNullNoComparer()
        {
            CustomerRec[] outer = new[]
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            AnagramRec[] inner = new[]
            {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => outer.AsQueryable().Join(inner.AsQueryable(), e => e.name, e => e.name, (Expression<Func<CustomerRec, AnagramRec, JoinRec>>)null));
        }

        [Fact]
        public void SelectorsReturnNull()
        {
            int?[] inner = { null, null, null };
            int?[] outer = { null, null };
            Assert.Empty(outer.AsQueryable().Join(inner.AsQueryable(), e => e, e => e, (x, y) => x));
        }

        [Fact]
        public void Join1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Join(new int[] { 1, 2, 3 }, n1 => n1, n2 => n2, (n1, n2) => n1 + n2).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void Join2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Join(new int[] { 1, 2, 3 }, n1 => n1, n2 => n2, (n1, n2) => n1 + n2, EqualityComparer<int>.Default).Count();
            Assert.Equal(2, count);
        }
    }
}
