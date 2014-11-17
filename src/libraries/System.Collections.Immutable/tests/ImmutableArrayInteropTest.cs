// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Collections.Immutable.Test
{
    public class ImmutableArrayInteropTest : ImmutablesTestBase
    {
        [Fact]
        public void DangerousCreateFromUnderlyingArray()
        {
            int[] array = new int[3] { 1, 2, 3 };
            int[] arrayCopy = array;
            ImmutableArray<int> immutable = ImmutableArrayInterop.DangerousCreateFromUnderlyingArray(ref array);

            // DangerousCreateFromUnderlyingArray clears the given parameter as a signal that
            // the mutable array should no longer be modified through mutable references.
            Assert.Null(array);

            Assert.Equal(3, immutable.Length);
            Assert.Equal(1, immutable[0]);
            Assert.Equal(2, immutable[1]);
            Assert.Equal(3, immutable[2]);

            arrayCopy[0] = 9;

            Assert.Equal(9, immutable[0]);
        }

        [Fact]
        public void DangerousCreateFromUnderlyingArrayNegativeTests()
        {
            int[] array = null;
            ImmutableArray<int> immutable = ImmutableArrayInterop.DangerousCreateFromUnderlyingArray(ref array);

            Assert.True(immutable.IsDefault);
        }

        [Fact]
        public void DangerousGetUnderlyingArray()
        {
            ImmutableArray<int> immutable = ImmutableArray.Create(1, 2, 3);
            int[] array = ImmutableArrayInterop.DangerousGetUnderlyingArray(immutable);

            Assert.Equal(3, array.Length);
            Assert.Equal(1, array[0]);
            Assert.Equal(2, array[1]);
            Assert.Equal(3, array[2]);

            array[0] = 9;

            Assert.Equal(9, immutable[0]);
        }

        [Fact]
        public void DangerousGetUnderlyingArrayNegativeTests()
        {
            ImmutableArray<int> immutable = default(ImmutableArray<int>);

            Assert.Null(ImmutableArrayInterop.DangerousGetUnderlyingArray(immutable));
        }
    }
}
