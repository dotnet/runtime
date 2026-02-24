// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class FullJoinTests : EnumerableTests
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

        public static JoinRec createJoinRec(CustomerRec cr, OrderRec or)
        {
            return new JoinRec
            {
                name = cr.name ?? string.Empty,
                orderID = or.orderID,
                total = or.total
            };
        }

        [Fact]
        public void BothEmpty()
        {
            CustomerRec[] outer = [];
            OrderRec[] inner = [];

            Assert.Empty(outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void OuterEmptyInnerNonEmpty()
        {
            CustomerRec[] outer = [];
            OrderRec[] inner =
            [
                new OrderRec{ orderID = 45321, custID = 98022, total = 50 },
                new OrderRec{ orderID = 97865, custID = 32103, total = 25 }
            ];
            JoinRec[] expected =
            [
                new JoinRec{ name = string.Empty, orderID = 45321, total = 50 },
                new JoinRec{ name = string.Empty, orderID = 97865, total = 25 }
            ];

            Assert.Equal(expected, outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void OuterNonEmptyInnerEmpty()
        {
            CustomerRec[] outer =
            [
                new CustomerRec{ name = "Tim", custID = 43434 },
                new CustomerRec{ name = "Bob", custID = 34093 }
            ];
            OrderRec[] inner = [];
            JoinRec[] expected =
            [
                new JoinRec{ name = "Tim", orderID = 0, total = 0 },
                new JoinRec{ name = "Bob", orderID = 0, total = 0 }
            ];

            Assert.Equal(expected, outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void AllMatch()
        {
            CustomerRec[] outer =
            [
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 }
            ];
            OrderRec[] inner =
            [
                new OrderRec{ orderID = 45321, custID = 98022, total = 50 },
                new OrderRec{ orderID = 95421, custID = 99021, total = 9 }
            ];
            JoinRec[] expected =
            [
                new JoinRec{ name = "Prakash", orderID = 45321, total = 50 },
                new JoinRec{ name = "Tim", orderID = 95421, total = 9 }
            ];

            Assert.Equal(expected, outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void NoMatch()
        {
            CustomerRec[] outer =
            [
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 }
            ];
            OrderRec[] inner =
            [
                new OrderRec{ orderID = 45321, custID = 11111, total = 50 },
                new OrderRec{ orderID = 95421, custID = 22222, total = 9 }
            ];
            JoinRec[] expected =
            [
                new JoinRec{ name = "Prakash", orderID = 0, total = 0 },
                new JoinRec{ name = "Tim", orderID = 0, total = 0 },
                new JoinRec{ name = string.Empty, orderID = 45321, total = 50 },
                new JoinRec{ name = string.Empty, orderID = 95421, total = 9 }
            ];

            Assert.Equal(expected, outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void MixedMatchAndUnmatched()
        {
            CustomerRec[] outer =
            [
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            ];
            OrderRec[] inner =
            [
                new OrderRec{ orderID = 45321, custID = 99022, total = 50 },
                new OrderRec{ orderID = 43421, custID = 29022, total = 20 },
                new OrderRec{ orderID = 95421, custID = 98022, total = 9 }
            ];
            JoinRec[] expected =
            [
                new JoinRec{ name = "Prakash", orderID = 95421, total = 9 },
                new JoinRec{ name = "Tim", orderID = 0, total = 0 },
                new JoinRec{ name = "Robert", orderID = 45321, total = 50 },
                new JoinRec{ name = string.Empty, orderID = 43421, total = 20 }
            ];

            Assert.Equal(expected, outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void InnerSameKeyMoreThanOneElement()
        {
            CustomerRec[] outer =
            [
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 }
            ];
            OrderRec[] inner =
            [
                new OrderRec{ orderID = 45321, custID = 98022, total = 50 },
                new OrderRec{ orderID = 45421, custID = 98022, total = 10 },
                new OrderRec{ orderID = 95421, custID = 99021, total = 9 }
            ];
            JoinRec[] expected =
            [
                new JoinRec{ name = "Prakash", orderID = 45321, total = 50 },
                new JoinRec{ name = "Prakash", orderID = 45421, total = 10 },
                new JoinRec{ name = "Tim", orderID = 95421, total = 9 }
            ];

            Assert.Equal(expected, outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void OuterSameKeyMoreThanOneElement()
        {
            CustomerRec[] outer =
            [
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Bob", custID = 99022 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            ];
            OrderRec[] inner =
            [
                new OrderRec{ orderID = 45321, custID = 98022, total = 50 },
                new OrderRec{ orderID = 43421, custID = 99022, total = 20 }
            ];
            JoinRec[] expected =
            [
                new JoinRec{ name = "Prakash", orderID = 45321, total = 50 },
                new JoinRec{ name = "Bob", orderID = 43421, total = 20 },
                new JoinRec{ name = "Robert", orderID = 43421, total = 20 }
            ];

            Assert.Equal(expected, outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void CustomComparer()
        {
            CustomerRec[] outer =
            [
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            ];
            AnagramRec[] inner =
            [
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            ];
            JoinRec[] expected =
            [
                new JoinRec{ name = "Prakash", orderID = 323232, total = 9 },
                new JoinRec{ name = "Tim", orderID = 43455, total = 10 },
                new JoinRec{ name = "Robert", orderID = 0, total = 0 }
            ];

            static JoinRec createRec(CustomerRec cr, AnagramRec or) =>
                new JoinRec { name = cr.name ?? string.Empty, orderID = or.orderID, total = or.total };

            Assert.Equal(expected, outer.FullJoin(inner, e => e.name, e => e.name, createRec, new AnagramEqualityComparer()));
        }

        [Fact]
        public void NullComparer()
        {
            CustomerRec[] outer =
            [
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            ];
            AnagramRec[] inner =
            [
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            ];
            JoinRec[] expected =
            [
                new JoinRec{ name = "Prakash", orderID = 323232, total = 9 },
                new JoinRec{ name = "Tim", orderID = 0, total = 0 },
                new JoinRec{ name = "Robert", orderID = 0, total = 0 },
                new JoinRec{ name = string.Empty, orderID = 43455, total = 10 }
            ];

            static JoinRec createRec(CustomerRec cr, AnagramRec or) =>
                new JoinRec { name = cr.name ?? string.Empty, orderID = or.orderID, total = or.total };

            Assert.Equal(expected, outer.FullJoin(inner, e => e.name, e => e.name, createRec, null));
        }

        [Fact]
        public void SelectorsReturnNull()
        {
            int?[] outer = [null, null];
            int?[] inner = [null, null, null];
            int?[] expected = [null, null];

            Assert.Equal(expected, outer.FullJoin(inner, e => e, e => e, (x, y) => x));
            Assert.Equal(expected, outer.FullJoin(inner, e => e, e => e, (x, y) => y));
        }

        [Fact]
        public void SingleElementEachAndMatches()
        {
            CustomerRec[] outer = [new CustomerRec { name = "Prakash", custID = 98022 }];
            OrderRec[] inner = [new OrderRec { orderID = 45321, custID = 98022, total = 50 }];
            JoinRec[] expected = [new JoinRec { name = "Prakash", orderID = 45321, total = 50 }];

            Assert.Equal(expected, outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void SingleElementEachAndDoesntMatch()
        {
            CustomerRec[] outer = [new CustomerRec { name = "Prakash", custID = 98922 }];
            OrderRec[] inner = [new OrderRec { orderID = 45321, custID = 98022, total = 50 }];
            JoinRec[] expected =
            [
                new JoinRec{ name = "Prakash", orderID = 0, total = 0 },
                new JoinRec{ name = string.Empty, orderID = 45321, total = 50 }
            ];

            Assert.Equal(expected, outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void OuterNull()
        {
            CustomerRec[] outer = null;
            OrderRec[] inner = [new OrderRec { orderID = 45321, custID = 98022, total = 50 }];

            AssertExtensions.Throws<ArgumentNullException>("outer", () => outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void InnerNull()
        {
            CustomerRec[] outer = [new CustomerRec { name = "Prakash", custID = 98022 }];
            OrderRec[] inner = null;

            AssertExtensions.Throws<ArgumentNullException>("inner", () => outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void OuterKeySelectorNull()
        {
            CustomerRec[] outer = [new CustomerRec { name = "Prakash", custID = 98022 }];
            OrderRec[] inner = [new OrderRec { orderID = 45321, custID = 98022, total = 50 }];

            AssertExtensions.Throws<ArgumentNullException>("outerKeySelector", () => outer.FullJoin(inner, null, e => e.custID, createJoinRec));
        }

        [Fact]
        public void InnerKeySelectorNull()
        {
            CustomerRec[] outer = [new CustomerRec { name = "Prakash", custID = 98022 }];
            OrderRec[] inner = [new OrderRec { orderID = 45321, custID = 98022, total = 50 }];

            AssertExtensions.Throws<ArgumentNullException>("innerKeySelector", () => outer.FullJoin(inner, e => e.custID, (Func<OrderRec, int>)null, createJoinRec));
        }

        [Fact]
        public void ResultSelectorNull()
        {
            CustomerRec[] outer = [new CustomerRec { name = "Prakash", custID = 98022 }];
            OrderRec[] inner = [new OrderRec { orderID = 45321, custID = 98022, total = 50 }];

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => outer.FullJoin(inner, e => e.custID, e => e.custID, (Func<CustomerRec, OrderRec, JoinRec>)null));
        }

        [Fact]
        public void OuterNullNoComparer()
        {
            CustomerRec[] outer = null;
            OrderRec[] inner = [new OrderRec { orderID = 45321, custID = 98022, total = 50 }];

            AssertExtensions.Throws<ArgumentNullException>("outer", () => outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec, EqualityComparer<int>.Default));
        }

        [Fact]
        public void InnerNullWithComparer()
        {
            CustomerRec[] outer = [new CustomerRec { name = "Prakash", custID = 98022 }];
            OrderRec[] inner = null;

            AssertExtensions.Throws<ArgumentNullException>("inner", () => outer.FullJoin(inner, e => e.custID, e => e.custID, createJoinRec, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullElements()
        {
            string[] outer = [null, string.Empty];
            string[] inner = [null, string.Empty];
            string[] expected = [null, string.Empty];

            Assert.Equal(expected, outer.FullJoin(inner, e => e, e => e, (x, y) => y, EqualityComparer<string>.Default));
        }
    }
}
