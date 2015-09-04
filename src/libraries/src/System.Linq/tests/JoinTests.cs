// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class JoinTests
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
            return new JoinRec { name = cr.name, orderID = or.orderID, total = or.total };
        }

        public static JoinRec createJoinRec(CustomerRec cr, AnagramRec or)
        {
            return new JoinRec { name = cr.name, orderID = or.orderID, total = or.total };
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

            Assert.Equal(expected, outer.Join(inner, e => e.custID, e => e.custID, createJoinRec));
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

            Assert.Equal(expected, outer.Join(inner, e => e.custID, e => e.custID, createJoinRec));
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

            Assert.Equal(expected, outer.Join(inner, e => e.name, e => e.name, createJoinRec, null));
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

            Assert.Equal(expected, outer.Join(inner, e => e.name, e => e.name, createJoinRec, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterNull()
        {
            CustomerRec[] outer = null;
            AnagramRec[] inner = new []
            {
                new AnagramRec{ name = "miT", orderID = 43455, total = 10 },
                new AnagramRec{ name = "Prakash", orderID = 323232, total = 9 }
            };

            Assert.Throws<ArgumentNullException>("outer", () => outer.Join(inner, e => e.name, e => e.name, createJoinRec, new AnagramEqualityComparer()));
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
            AnagramRec[] inner = null;

            Assert.Throws<ArgumentNullException>("inner", () => outer.Join(inner, e => e.name, e => e.name, createJoinRec, new AnagramEqualityComparer()));
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
            JoinRec[] expected = new []
            {
                new JoinRec{ name = "Prakash", orderID = 323232, total = 9 },
                new JoinRec{ name = "Tim", orderID = 43455, total = 10 }
            };

            Assert.Throws<ArgumentNullException>("outerKeySelector", () => outer.Join(inner, null, e => e.name, createJoinRec, new AnagramEqualityComparer()));
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

            Assert.Throws<ArgumentNullException>("innerKeySelector", () => outer.Join(inner, e => e.name, null, createJoinRec, new AnagramEqualityComparer()));
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

            Assert.Throws<ArgumentNullException>("resultSelector", () => outer.Join(inner, e => e.name, e => e.name, (Func<CustomerRec, AnagramRec, JoinRec>)null, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SkipsNullElements()
        {
            string[] outer = new [] { null, string.Empty };
            string[] inner = new [] { null, string.Empty };
            string[] expected = new [] { string.Empty };

            Assert.Equal(expected, outer.Join(inner, e => e, e => e, (x, y) => y, EqualityComparer<string>.Default));
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
            JoinRec[] expected = { };
            
            Assert.Equal(expected, outer.Join(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void SingleElementEachAndMatches()
        {
            CustomerRec[] outer = new [] { new CustomerRec { name = "Prakash", custID = 98022 } };
            OrderRec[] inner = new [] { new OrderRec { orderID = 45321, custID = 98022, total = 50 } };
            JoinRec[] expected = new [] { new JoinRec { name = "Prakash", orderID = 45321, total = 50 } };

            Assert.Equal(expected, outer.Join(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void SingleElementEachAndDoesntMatch()
        {
            CustomerRec[] outer = new [] { new CustomerRec { name = "Prakash", custID = 98922 } };
            OrderRec[] inner = new [] { new OrderRec { orderID = 45321, custID = 98022, total = 50 } };
            JoinRec[] expected = { };

            Assert.Equal(expected, outer.Join(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void SelectorsReturnNull()
        {
            int?[] inner = { null, null, null };
            int?[] outer = { null, null };
            int?[] expected = { };

            Assert.Equal(expected, outer.Join(inner, e => e, e => e, (x, y) => x));
        }

        [Fact]
        public void InnerSameKeyMoreThanOneElementAndMatches()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            OrderRec[] inner = new []
            {
                new OrderRec{ orderID = 45321, custID = 98022, total = 50 },
                new OrderRec{ orderID = 45421, custID = 98022, total = 10 },
                new OrderRec{ orderID = 43421, custID = 99022, total = 20 },
                new OrderRec{ orderID = 85421, custID = 98022, total = 18 },
                new OrderRec{ orderID = 95421, custID = 99021, total = 9 }
            };
            JoinRec[] expected = new []
            {
                new JoinRec{ name = "Prakash", orderID = 45321, total = 50 },
                new JoinRec{ name = "Prakash", orderID = 45421, total = 10 },
                new JoinRec{ name = "Prakash", orderID = 85421, total = 18 },
                new JoinRec{ name = "Tim", orderID = 95421, total = 9 },
                new JoinRec{ name = "Robert", orderID = 43421, total = 20 }
            };

            Assert.Equal(expected, outer.Join(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void OuterSameKeyMoreThanOneElementAndMatches()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Bob", custID = 99022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            OrderRec[] inner = new []
            {
                new OrderRec{ orderID = 45321, custID = 98022, total = 50 },
                new OrderRec{ orderID = 43421, custID = 99022, total = 20 },
                new OrderRec{ orderID = 95421, custID = 99021, total = 9 }
            };
            JoinRec[] expected = new []
            {
                new JoinRec{ name = "Prakash", orderID = 45321, total = 50 },
                new JoinRec{ name = "Bob", orderID = 43421, total = 20 },
                new JoinRec{ name = "Tim", orderID = 95421, total = 9 },
                new JoinRec{ name = "Robert", orderID = 43421, total = 20 }
            };

            Assert.Equal(expected, outer.Join(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void NoMatches()
        {
            CustomerRec[] outer = new []
            {
                new CustomerRec{ name = "Prakash", custID = 98022 },
                new CustomerRec{ name = "Bob", custID = 99022 },
                new CustomerRec{ name = "Tim", custID = 99021 },
                new CustomerRec{ name = "Robert", custID = 99022 }
            };
            OrderRec[] inner = new []
            {
                new OrderRec{ orderID = 45321, custID = 18022, total = 50 },
                new OrderRec{ orderID = 43421, custID = 29022, total = 20 },
                new OrderRec{ orderID = 95421, custID = 39021, total = 9 }
            };
            JoinRec[] expected = { };

            Assert.Equal(expected, outer.Join(inner, e => e.custID, e => e.custID, createJoinRec));
        }
    }
}
