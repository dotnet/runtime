// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;

namespace System.Diagnostics.Contracts.Tests
{
    public class ForAllTests
    {
        [Fact]
        public static void ArgumentExceptions()
        {
            Assert.Throws<ArgumentNullException>(() => Contract.ForAll(0, 1, null));
            Assert.Throws<ArgumentNullException>(() => Contract.ForAll<int>(null, i => true));
            Assert.Throws<ArgumentNullException>(() => Contract.ForAll<int>(Enumerable.Empty<int>(), null));
            Assert.Throws<ArgumentException>(() => Contract.ForAll(1, 0, i => true)); // fromInclusive > toExclusive
        }

        [Fact]
        public static void EmptyInputReturnsTrue()
        {
            Assert.True(Contract.ForAll(Enumerable.Empty<int>(), i => { Assert.True(false, "Should never be invoked"); return true; }));
            Assert.True(Contract.ForAll(-2, -2, i => { Assert.True(false, "Should never be invoked"); return true; }));
            Assert.True(Contract.ForAll(1, 1, i => { Assert.True(false, "Should never be invoked"); return true; }));
        }

        [Fact]
        public static void AnyFailsPredicateReturnsFalseImmediately()
        {
            int count;

            count = 0;
            Assert.False(Contract.ForAll(Enumerable.Range(0, 10), i => {
                count++;
                return i != 3;
            }));
            Assert.Equal(4, count);

            count = 0;
            Assert.False(Contract.ForAll(-10, 0, i => {
                count++;
                return i != -8;
            }));
            Assert.Equal(3, count);
        }

        [Fact]
        public static void AllPassPredicateReturnsTrue()
        {
            Assert.True(Contract.ForAll(Enumerable.Range(0, 10), i => true));
            Assert.True(Contract.ForAll(0, 10, i => true));
        }

        [Fact]
        public static void ExceptionsPropagatedImmediately()
        {
            int count;

            count = 0;
            Assert.Throws<FormatException>(() => Contract.ForAll(Enumerable.Range(0, 10), i => {
                count++;
                if (i == 3) throw new FormatException();
                return true;
            }));
            Assert.Equal(4, count);

            count = 0;
            Assert.Throws<FormatException>(() => Contract.ForAll(100, 110, i => {
                count++;
                if (i == 105) throw new FormatException();
                return true;
            }));
            Assert.Equal(6, count);
        }

    }
}
