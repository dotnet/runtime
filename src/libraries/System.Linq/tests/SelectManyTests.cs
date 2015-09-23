// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class SelectManyTests
    {
        private struct CustomerRec
        {
            public string name { get; set; }
            public int?[] total { get; set; }
        }

        [Fact]
        public void EmptySource()
        {
            Assert.Empty(Enumerable.Empty<CustomerRec>().SelectMany(e => e.total));
        }

        [Fact]
        public void EmptySourceIndexedSelector()
        {
            Assert.Empty(Enumerable.Empty<CustomerRec>().SelectMany((e, i) => e.total));
        }

        [Fact]
        public void EmptySourceResultSelector()
        {
            Assert.Empty(Enumerable.Empty<CustomerRec>().SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void EmptySourceResultSelectorIndexedSelector()
        {
            Assert.Empty(Enumerable.Empty<CustomerRec>().SelectMany((e, i) => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void SingleElement()
        {
            int?[] expected = { 90, 55, null, 43, 89 };
            CustomerRec[] source =
            {
                new CustomerRec { name = "Prakash", total = expected }
            };
            Assert.Equal(expected, source.SelectMany(e => e.total));
        }

        [Fact]
        public void NonEmptySelectingEmpty()
        {
            CustomerRec[] source =
            {
                new CustomerRec { name="Prakash", total=new int?[0] },
                new CustomerRec { name="Bob", total=new int?[0] },
                new CustomerRec { name="Chris", total=new int?[0] },
                new CustomerRec { name=null, total=new int?[0] },
                new CustomerRec { name="Prakash", total=new int?[0] }
            };

            Assert.Empty(source.SelectMany(e => e.total));
        }

        [Fact]
        public void NonEmptySelectingEmptyIndexedSelector()
        {
            CustomerRec[] source =
            {
                new CustomerRec { name="Prakash", total=new int?[0] },
                new CustomerRec { name="Bob", total=new int?[0] },
                new CustomerRec { name="Chris", total=new int?[0] },
                new CustomerRec { name=null, total=new int?[0] },
                new CustomerRec { name="Prakash", total=new int?[0] }
            };

            Assert.Empty(source.SelectMany((e, i) => e.total));
        }

        [Fact]
        public void NonEmptySelectingEmptyWithResultSelector()
        {
            CustomerRec[] source =
            {
                new CustomerRec { name="Prakash", total=new int?[0] },
                new CustomerRec { name="Bob", total=new int?[0] },
                new CustomerRec { name="Chris", total=new int?[0] },
                new CustomerRec { name=null, total=new int?[0] },
                new CustomerRec { name="Prakash", total=new int?[0] }
            };

            Assert.Empty(source.SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NonEmptySelectingEmptyIndexedSelectorWithResultSelector()
        {
            CustomerRec[] source =
            {
                new CustomerRec { name="Prakash", total=new int?[0] },
                new CustomerRec { name="Bob", total=new int?[0] },
                new CustomerRec { name="Chris", total=new int?[0] },
                new CustomerRec { name=null, total=new int?[0] },
                new CustomerRec { name="Prakash", total=new int?[0] }
            };

            Assert.Empty(source.SelectMany((e, i) => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void ResultsSelected()
        {
            CustomerRec[] source =
            {
                new CustomerRec { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new CustomerRec { name="Bob", total=new int?[]{5, 6} },
                new CustomerRec { name="Chris", total=new int?[0] },
                new CustomerRec { name=null, total=new int?[]{8, 9} },
                new CustomerRec { name="Prakash", total=new int?[]{-10, 100} }
            };
            int?[] expected = { 1, 2, 3, 4, 5, 6, 8, 9, -10, 100 };
            Assert.Equal(expected, source.SelectMany(e => e.total));
        }

        [Fact]
        public void SourceEmptyIndexUsed()
        {
            Assert.Empty(Enumerable.Empty<CustomerRec>().SelectMany((e, index) => e.total));
        }

        [Fact]
        public void SingleElementIndexUsed()
        {
            int?[] expected = { 90, 55, null, 43, 89 };
            CustomerRec[] source =
            {
                new CustomerRec { name = "Prakash", total = expected }
            };
            Assert.Equal(expected, source.SelectMany((e, index) => e.total));
        }

        [Fact]
        public void NonEmptySelectingEmptyIndexUsed()
        {
            CustomerRec[] source =
            {
                new CustomerRec { name="Prakash", total= new int?[0] },
                new CustomerRec { name="Bob", total=new int?[0] },
                new CustomerRec { name="Chris", total=new int?[0] },
                new CustomerRec { name=null, total=new int?[0] },
                new CustomerRec { name="Prakash", total=new int?[0] }
            };
            Assert.Empty(source.SelectMany((e, index) => e.total));
        }

        [Fact]
        public void ResultsSelectedIndexUsed()
        {
            CustomerRec[] source =
            {
                new CustomerRec { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new CustomerRec { name="Bob", total=new int?[]{5, 6} },
                new CustomerRec { name="Chris", total=new int?[0] },
                new CustomerRec { name=null, total=new int?[]{8, 9} },
                new CustomerRec { name="Prakash", total=new int?[]{-10, 100} }
            };
            int?[] expected = { 1, 2, 3, 4, 5, 6, 8, 9, -10, 100 };
            Assert.Equal(expected, source.SelectMany((e, index) => e.total));
        }

        [Fact]
        public void IndexCausingFirstToBeSelected()
        {
            CustomerRec[] source =
            {
                new CustomerRec { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new CustomerRec { name="Bob", total=new int?[]{5, 6} },
                new CustomerRec { name="Chris", total=new int?[0] },
                new CustomerRec { name=null, total=new int?[]{8, 9} },
                new CustomerRec { name="Prakash", total=new int?[]{-10, 100} }
            };

            Assert.Equal(source.First().total, source.SelectMany((e, i) => i == 0 ? e.total : Enumerable.Empty<int?>()));
        }

        [Fact]
        public void IndexCausingLastToBeSelected()
        {
            CustomerRec[] source =
            {
                new CustomerRec { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new CustomerRec { name="Bob", total=new int?[]{5, 6} },
                new CustomerRec { name="Chris", total=new int?[0] },
                new CustomerRec { name=null, total=new int?[]{8, 9} },
                new CustomerRec { name="Robert", total=new int?[]{-10, 100} }
            };

            Assert.Equal(source.Last().total, source.SelectMany((e, i) => i == 4 ? e.total : Enumerable.Empty<int?>()));
        }

        private sealed class FastInfiniteEnumerator : IEnumerable<int>, IEnumerator<int>
        {
            public IEnumerator<int> GetEnumerator()
            {
                return this;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
            public bool MoveNext()
            {
                return true;
            }
            public void Reset()
            {
            }
            object IEnumerator.Current
            {
                get { return 0; }
            }
            public void Dispose()
            {
            }
            public int Current
            {
                get { return 0; }
            }
        }

        [Fact]
        [OuterLoop]
        public void IndexOverflow()
        {
            var selected = new FastInfiniteEnumerator().SelectMany((e, i) => Enumerable.Empty<int>());
            using (var en = selected.GetEnumerator())
                Assert.Throws<OverflowException>(() =>
                {
                    while(en.MoveNext())
                    {
                    }
                });
        }

        [Fact]
        public void ResultSelector()
        {
            CustomerRec[] source =
            {
                new CustomerRec { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new CustomerRec { name="Bob", total=new int?[]{5, 6} },
                new CustomerRec { name="Chris", total=new int?[0] },
                new CustomerRec { name=null, total=new int?[]{8, 9} },
                new CustomerRec { name="Prakash", total=new int?[]{-10, 100} }
            };
            string[] expected = { "1", "2", "3", "4", "5", "6", "8", "9", "-10", "100" };

            Assert.Equal(expected, source.SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullResultSelector()
        {
            Func<CustomerRec, int?, string> resultSelector = null;
            Assert.Throws<ArgumentNullException>("resultSelector", () => Enumerable.Empty<CustomerRec>().SelectMany(e => e.total, resultSelector));
        }

        [Fact]
        public void NullResultSelectorIndexedSelector()
        {
            Func<CustomerRec, int?, string> resultSelector = null;
            Assert.Throws<ArgumentNullException>("resultSelector", () => Enumerable.Empty<CustomerRec>().SelectMany((e, i) => e.total, resultSelector));
        }

        [Fact]
        public void NullSourceWithResultSelector()
        {
            CustomerRec[] source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullCollectionSelector()
        {
            Func<CustomerRec, IEnumerable<int?>> collectionSelector = null;
            Assert.Throws<ArgumentNullException>("collectionSelector", () => Enumerable.Empty<CustomerRec>().SelectMany(collectionSelector, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullIndexedCollectionSelector()
        {
            Func<CustomerRec, int, IEnumerable<int?>> collectionSelector = null;
            Assert.Throws<ArgumentNullException>("collectionSelector", () => Enumerable.Empty<CustomerRec>().SelectMany(collectionSelector, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullSource()
        {
            CustomerRec[] source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SelectMany(e => e.total));
        }

        [Fact]
        public void NullSourceIndexedSelector()
        {
            CustomerRec[] source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SelectMany((e, i) => e.total));
        }

        [Fact]
        public void NullSourceIndexedSelectorWithResultSelector()
        {
            CustomerRec[] source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SelectMany((e, i) => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullSelector()
        {
            Func<CustomerRec, int[]> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => new CustomerRec[0].SelectMany(selector));
        }

        [Fact]
        public void NullIndexedSelector()
        {
            Func<CustomerRec, int, int[]> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => new CustomerRec[0].SelectMany(selector));
        }

        [Fact]
        public void IndexCausingFirstToBeSelectedWithResultSelector()
        {
            CustomerRec[] source =
            {
                new CustomerRec { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new CustomerRec { name="Bob", total=new int?[]{5, 6} },
                new CustomerRec { name="Chris", total=new int?[0] },
                new CustomerRec { name=null, total=new int?[]{8, 9} },
                new CustomerRec { name="Prakash", total=new int?[]{-10, 100} }
            };
            string[] expected = { "1", "2", "3", "4" };
            Assert.Equal(expected, source.SelectMany((e, i) => i == 0 ? e.total : Enumerable.Empty<int?>(), (e, f) => f.ToString()));
        }

        [Fact]
        public void IndexCausingLastToBeSelectedWithResultSelector()
        {
            CustomerRec[] source =
            {
                new CustomerRec { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new CustomerRec { name="Bob", total=new int?[]{5, 6} },
                new CustomerRec { name="Chris", total=new int?[0] },
                new CustomerRec { name=null, total=new int?[]{8, 9} },
                new CustomerRec { name="Robert", total=new int?[]{-10, 100} }
            };

            string[] expected = { "-10", "100" };
            Assert.Equal(expected, source.SelectMany((e, i) => i == 4 ? e.total : Enumerable.Empty<int?>(), (e, f) => f.ToString()));
        }
    }
}
