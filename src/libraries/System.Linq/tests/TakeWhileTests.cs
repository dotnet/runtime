// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class TakeWhileTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;
                    
            Assert.Equal(q.TakeWhile(x => true), q.TakeWhile(x => true));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.TakeWhile(x => true), q.TakeWhile(x => true));
        }

        [Fact]
        public void SourceNonEmptyPredicateFalseForAll()
        {
            int[] source = { 9, 7, 15, 3, 11 };
            int[] expected = { };
            
            Assert.Equal(expected, source.TakeWhile(x => x % 2 == 0));
        }

        [Fact]
        public void SourceNonEmptyPredicateFalseForAllWithIndex()
        {
            int[] source = { 9, 7, 15, 3, 11 };
            int[] expected = { };
            
            Assert.Equal(expected, source.TakeWhile((x, i) => x % 2 == 0));
        }

        [Fact]
        public void SourceNonEmptyPredicateTrueSomeFalseSecond()
        {
            int[] source = { 8, 3, 12, 4, 6, 10 };
            int[] expected = { 8 };

            Assert.Equal(expected, source.TakeWhile(x => x % 2 == 0));
        }

        [Fact]
        public void SourceNonEmptyPredicateTrueSomeFalseSecondWithIndex()
        {
            int[] source = { 8, 3, 12, 4, 6, 10 };
            int[] expected = { 8 };

            Assert.Equal(expected, source.TakeWhile((x, i) => x % 2 == 0));
        }

        [Fact]
        public void SourceNonEmptyPredicateTrueSomeFalseFirst()
        {
            int[] source = { 3, 2, 4, 12, 6 };
            int[] expected = { };
            
            Assert.Equal(expected, source.TakeWhile(x => x % 2 == 0));
        }

        [Fact]
        public void SourceNonEmptyPredicateTrueSomeFalseFirstWithIndex()
        {
            int[] source = { 3, 2, 4, 12, 6 };
            int[] expected = { };
            
            Assert.Equal(expected, source.TakeWhile((x, i) => x % 2 == 0));
        }

        [Fact]
        public void FirstTakenByIndex()
        {
            int[] source = { 6, 2, 5, 3, 8 };
            int[] expected = { 6 };

            Assert.Equal(expected, source.TakeWhile((element, index) => index == 0));
        }

        [Fact]
        public void AllButLastTakenByIndex()
        {
            int[] source = { 6, 2, 5, 3, 8 };
            int[] expected = { 6, 2, 5, 3 };

            Assert.Equal(expected, source.TakeWhile((element, index) => index < source.Length - 1));
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
        public void IndexTakeWhileOverflowBeyondIntMaxValueElements()
        {
            var taken = new FastInfiniteEnumerator().TakeWhile((e, i) => true);
            
            using(var en = taken.GetEnumerator())
                Assert.Throws<OverflowException>(() =>
                {
                    while(en.MoveNext())
                    {
                    }
                });
        }

        [Fact]
        public void ThrowsOnNullSource()
        {
            int[] source = null;
            Assert.Throws<ArgumentNullException>(() => source.TakeWhile(x => true));
        }

        [Fact]
        public void ThrowsOnNullPredicate()
        {
            int[] source = { 1, 2, 3 };
            Func<int, bool> nullPredicate = null;

            Assert.Throws<ArgumentNullException>(() => source.TakeWhile(nullPredicate));
        }
    }
}