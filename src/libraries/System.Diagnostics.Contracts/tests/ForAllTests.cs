// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            AssertExtensions.Throws<ArgumentException>(null, () => Contract.ForAll(1, 0, i => true)); // fromInclusive > toExclusive
        }

        [Fact]
        public static void EmptyInputReturnsTrue()
        {
            Assert.True(Contract.ForAll(Enumerable.Empty<int>(), i => { throw new ShouldNotBeInvokedException(); }));
            Assert.True(Contract.ForAll(-2, -2, i => { throw new ShouldNotBeInvokedException(); }));
            Assert.True(Contract.ForAll(1, 1, i => { throw new ShouldNotBeInvokedException(); }));
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
