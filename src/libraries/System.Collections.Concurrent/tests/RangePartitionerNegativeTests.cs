// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// RangePartitionerNegativeTests.cs.cs
//
// Contains negative testcases for range partitioner:
//  - Passing range (to <= from)
//  - Passing invalid range size
//
// Taken from:
// \qa\clr\testsrc\pfx\Functional\Common\Partitioner\YetiTests\RangePartitioner\OutOfTheBoxPartitionerTests.cs
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Concurrent;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class RangePartitionerNegativeTests
    {
        /// <summary>
        /// Test passing invalid range, 'to' is smaller or equal than 'from'
        /// </summary>
        [Fact]
        public static void IntFromNotGreaterThanTo()
        {
            IntFromNotGreaterThanTo(1000, 0, 100);
            IntFromNotGreaterThanTo(899, 899, 100);
            IntFromNotGreaterThanTo(-19999, -299999, 100);
        }

        public static void IntFromNotGreaterThanTo(int from, int to, int rangesize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Partitioner.Create(from, to));
            Assert.Throws<ArgumentOutOfRangeException>(() => Partitioner.Create(from, to, rangesize));
        }

        /// <summary>
        /// Test passing invalid range, 'to' is smaller or equal than 'from', on long overload
        [Fact]
        public static void LongFromNotGreaterThanTo()
        {
            LongFromNotGreaterThanTo(1000, 0, 100);
            LongFromNotGreaterThanTo(899, 899, 100);
            LongFromNotGreaterThanTo(-19999, -299999, 100);
        }

        public static void LongFromNotGreaterThanTo(long from, long to, int rangesize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Partitioner.Create(from, to));
            Assert.Throws<ArgumentOutOfRangeException>(() => Partitioner.Create(from, to, rangesize));
        }

        /// <summary>
        /// Test passing invalid range size, less than or equal to 0
        /// </summary>
        [Fact]
        public static void InvalidIntRangeSize()
        {
            InvalidIntRangeSize(0, 1000, 0);
            InvalidIntRangeSize(899, 9000, -10);
        }

        public static void InvalidIntRangeSize(int from, int to, int rangesize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Partitioner.Create(from, to, rangesize));
        }

        /// <summary>
        /// Test passing invalid range size, less than or equal to 0, on long overload
        /// </summary>
        [Fact]
        public static void ATestMethod()
        {
            InvalidLongRangeSize(0, 1000, 0);
            InvalidLongRangeSize(899, 9000, -10);
        }

        public static void InvalidLongRangeSize(long from, long to, long rangesize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Partitioner.Create(from, to, rangesize));
        }
    }
}
