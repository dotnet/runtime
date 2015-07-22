// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace System.Linq.Tests
{
    public class SkipTests
    {
        [Fact]
        public void SkipSome()
        {
            Assert.Equal(Enumerable.Range(10, 10), Enumerable.Range(0, 20).Skip(10));
        }

        [Fact]
        public void SkipNone()
        {
            Assert.Equal(Enumerable.Range(0, 20), Enumerable.Range(0, 20).Skip(0));
        }

        [Fact]
        public void SkipExcessive()
        {
            Assert.Equal(Enumerable.Empty<int>(), Enumerable.Range(0, 20).Skip(42));
        }

        [Fact]
        public void SkipAllExactly()
        {
            Assert.False(Enumerable.Range(0, 20).Skip(20).Any());
        }

        [Fact]
        public void SkipThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => ((IEnumerable<DateTime>)null).Skip(3));
        }

        [Fact]
        public void SkipNegative()
        {
            Assert.Equal(Enumerable.Range(0, 20), Enumerable.Range(0, 20).Skip(-42));
        }

        [Fact]
        public void SkipWhileAllTrue()
        {
            Assert.Equal(Enumerable.Empty<int>(), Enumerable.Range(0, 20).SkipWhile(i => i < 40));
            Assert.Equal(Enumerable.Empty<int>(), Enumerable.Range(0, 20).SkipWhile((i, idx) => i == idx));
        }

        [Fact]
        public void SkipWhileAllFalse()
        {
            Assert.Equal(Enumerable.Range(0, 20), Enumerable.Range(0, 20).SkipWhile(i => i != 0));
            Assert.Equal(Enumerable.Range(0, 20), Enumerable.Range(0, 20).SkipWhile((i, idx) => i != idx));
        }

        [Fact]
        public void SkipWhileThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => ((IEnumerable<int>)null).SkipWhile(i => i < 40));
            Assert.Throws<ArgumentNullException>(() => ((IEnumerable<int>)null).SkipWhile((i, idx) => i == idx));
            Assert.Throws<ArgumentNullException>(() => Enumerable.Range(0, 20).SkipWhile((Func<int, int, bool>)null));
            Assert.Throws<ArgumentNullException>(() => Enumerable.Range(0, 20).SkipWhile((Func<int, bool>)null));
        }

        [Fact]
        public void SkipWhilePassesPredicateExceptionWhenEnumerated()
        {
            var source = Enumerable.Range(-2, 5).SkipWhile(i => 1 / i <= 0);
            using(var en = source.GetEnumerator())
            {
                Assert.Throws<DivideByZeroException>(() => en.MoveNext());
            }
        }
        
        [Fact]
        public void SkipWhileHalf()
        {
            Assert.Equal(Enumerable.Range(10, 10), Enumerable.Range(0, 20).SkipWhile(i => i < 10));
            Assert.Equal(Enumerable.Range(10, 10), Enumerable.Range(0, 20).SkipWhile((i, idx) => idx < 10));
        }

        [Fact]
        public void SkipErrorWhenSourceErrors()
        {
            var source = Enumerable.Range(-2, 5).Select(i => (decimal)i).Select(m => 1 / m).Skip(4);
            using(var en = source.GetEnumerator())
            {
                Assert.Throws<DivideByZeroException>(() => en.MoveNext());
            }
        }
    }
}
