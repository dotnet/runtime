// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;

namespace System.Diagnostics.Contracts.Tests
{
    public class ExistsTests
    {
        [Fact]
        public static void ArgumentExceptions()
        {
            Assert.Throws<ArgumentNullException>(() => Contract.Exists(0, 1, null));
            Assert.Throws<ArgumentNullException>(() => Contract.Exists<int>(null, i => true));
            Assert.Throws<ArgumentNullException>(() => Contract.Exists<int>(Enumerable.Empty<int>(), null));
            Assert.Throws<ArgumentException>(() => Contract.Exists(1, 0, i => true)); // fromInclusive > toExclusive
        }

        [Fact]
        public static void EmptyInputReturnsFalse()
        {
            Assert.False(Contract.Exists(Enumerable.Empty<int>(), i => { Assert.True(false, "Should never be invoked"); return true; }));
            Assert.False(Contract.Exists(-2, -2, i => { Assert.True(false, "Should never be invoked"); return true; }));
            Assert.False(Contract.Exists(1, 1, i => { Assert.True(false, "Should never be invoked"); return true; }));
        }

        [Fact]
        public static void AllFailPredicateReturnsFalse()
        {
            Assert.False(Contract.Exists(Enumerable.Range(0, 10), i => false));
            Assert.False(Contract.Exists(0, 10, i => false));
        }

        [Fact]
        public static void AnyPassesPredicateReturnsTrueImmediately()
        {
            int count;

            count = 0;
            Assert.True(Contract.Exists(Enumerable.Range(0, 10), i => {
                count++;
                return i == 3;
            }));
            Assert.Equal(4, count);

            count = 0;
            Assert.True(Contract.Exists(-10, 0, i => {
                count++;
                return i == -8;
            }));
            Assert.Equal(3, count);
        }

        [Fact]
        public static void ExceptionsPropagatedImmediately()
        {
            int count;

            count = 0;
            Assert.Throws<FormatException>(() => Contract.Exists(Enumerable.Range(0, 10), i => {
                count++;
                if (i == 3) throw new FormatException();
                return false;
            }));
            Assert.Equal(4, count);

            count = 0;
            Assert.Throws<FormatException>(() => Contract.Exists(100, 110, i => {
                count++;
                if (i == 105) throw new FormatException();
                return false;
            }));
            Assert.Equal(6, count);
        }

    }
}
