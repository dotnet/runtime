// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class GroupJoinTests : EnumerableTests
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
                if (orderID is null)
                {
                    if (other.orderID is not null) return false;
                }
                else
                {
                    if (other.orderID is null) return false;
                    if (orderID.Length != other.orderID.Length) return false;
                    for (int i = 0; i != other.orderID.Length; ++i)
                        if (orderID[i] != other.orderID[i]) return false;
                }
                if (total is null)
                {
                    if (other.total is not null) return false;
                }
                else
                {
                    if (other.total is null) return false;
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
            Assert.Empty(outer.GroupJoin(inner, e => e.custID, e => e.custID, createJoinRec));
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

            AssertExtensions.Throws<ArgumentNullException>("outer", () => outer.GroupJoin(inner, e => e.name, e => e.name, createJoinRec, new AnagramEqualityComparer()));
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

            AssertExtensions.Throws<ArgumentNullException>("inner", () => outer.GroupJoin(inner, e => e.name, e => e.name, createJoinRec, new AnagramEqualityComparer()));
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

            AssertExtensions.Throws<ArgumentNullException>("outerKeySelector", () => outer.GroupJoin(inner, null, e => e.name, createJoinRec, new AnagramEqualityComparer()));
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

            AssertExtensions.Throws<ArgumentNullException>("innerKeySelector", () => outer.GroupJoin(inner, e => e.name, null, createJoinRec, new AnagramEqualityComparer()));
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

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => outer.GroupJoin(inner, e => e.name, e => e.name, (Func<CustomerRec, IEnumerable<AnagramRec>, JoinRec>)null, new AnagramEqualityComparer()));
        }

        [Fact]
        public void OuterNullNoComparer()
        {
            CustomerRec[] outer = null;
            AnagramRec[] inner = new AnagramRec[]
            {
                new AnagramRec{ name = "Robert", orderID = 93483, total = 19 },
                new AnagramRec{ name = "miT", orderID = 93489, total = 45 }
            };

            AssertExtensions.Throws<ArgumentNullException>("outer", () => outer.GroupJoin(inner, e => e.name, e => e.name, createJoinRec));
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
            AnagramRec[] inner = null;

            AssertExtensions.Throws<ArgumentNullException>("inner", () => outer.GroupJoin(inner, e => e.name, e => e.name, createJoinRec));
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

            AssertExtensions.Throws<ArgumentNullException>("outerKeySelector", () => outer.GroupJoin(inner, null, e => e.name, createJoinRec));
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

            AssertExtensions.Throws<ArgumentNullException>("innerKeySelector", () => outer.GroupJoin(inner, e => e.name, null, createJoinRec));
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

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => outer.GroupJoin(inner, e => e.name, e => e.name, (Func<CustomerRec, IEnumerable<AnagramRec>, JoinRec>)null));
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
            CustomerRec[] outer = new[]
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 }
            };
            OrderRec[] inner = new[]
            {
                new OrderRec{ orderID = 97865, custID = 1234, total = 25 },
                new OrderRec{ orderID = 34390, custID = 1234, total = 19 },
                new OrderRec{ orderID = 34390, custID = 9865, total = 19 }
            };
            JoinRec[] expected = new[]
            {
                new JoinRec { name = "Tim", orderID = new int?[]{ 97865, 34390 }, total = new int?[] { 25, 19 } },
                new JoinRec { name = "Bob", orderID = new int?[]{ 34390 }, total = new int?[]{ 19 } }
            };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e.custID, e => e.custID, createJoinRec));
        }

        [Fact]
        public void InnerSameKeyMoreThanOneElementAndMatchesRunOnce()
        {
            CustomerRec[] outer = new[]
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 }
            };
            OrderRec[] inner = new[]
            {
                new OrderRec{ orderID = 97865, custID = 1234, total = 25 },
                new OrderRec{ orderID = 34390, custID = 1234, total = 19 },
                new OrderRec{ orderID = 34390, custID = 9865, total = 19 }
            };
            JoinRec[] expected = new[]
            {
                new JoinRec { name = "Tim", orderID = new int?[]{ 97865, 34390 }, total = new int?[] { 25, 19 } },
                new JoinRec { name = "Bob", orderID = new int?[]{ 34390 }, total = new int?[]{ 19 } }
            };

            Assert.Equal(expected, outer.RunOnce().GroupJoin(inner.RunOnce(), e => e.custID, e => e.custID, createJoinRec));
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
            CustomerRec[] outer = new[]
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 },
                new CustomerRec{ name = "Robert", custID = 9895 }
            };
            AnagramRec[] inner = new[]
            {
                new AnagramRec{ name = "Robert", orderID = 93483, total = 19 },
                new AnagramRec{ name = "miT", orderID = 93489, total = 45 }
            };
            JoinRec[] expected = new[]
            {
                new JoinRec{ name = "Tim", orderID = new int?[]{ }, total = new int?[]{ } },
                new JoinRec{ name = "Bob", orderID = new int?[]{ }, total = new int?[]{ } },
                new JoinRec{ name = "Robert", orderID = new int?[]{ 93483 }, total = new int?[]{ 19 } }
            };

            Assert.Equal(expected, outer.GroupJoin(inner, e => e.name, e => e.name, createJoinRec, null));
        }

        [Fact]
        public void NullComparerRunOnce()
        {
            CustomerRec[] outer = new[]
            {
                new CustomerRec{ name = "Tim", custID = 1234 },
                new CustomerRec{ name = "Bob", custID = 9865 },
                new CustomerRec{ name = "Robert", custID = 9895 }
            };
            AnagramRec[] inner = new[]
            {
                new AnagramRec{ name = "Robert", orderID = 93483, total = 19 },
                new AnagramRec{ name = "miT", orderID = 93489, total = 45 }
            };
            JoinRec[] expected = new[]
            {
                new JoinRec{ name = "Tim", orderID = new int?[]{ }, total = new int?[]{ } },
                new JoinRec{ name = "Bob", orderID = new int?[]{ }, total = new int?[]{ } },
                new JoinRec{ name = "Robert", orderID = new int?[]{ 93483 }, total = new int?[]{ 19 } }
            };

            Assert.Equal(expected, outer.RunOnce().GroupJoin(inner.RunOnce(), e => e.name, e => e.name, createJoinRec, null));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).GroupJoin(Enumerable.Empty<int>(), i => i, i => i, (o, i) => i);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<IEnumerable<int>>;
            Assert.False(en is not null && en.MoveNext());
        }
    }
}
