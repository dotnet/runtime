// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Internal;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class ImmutableByteArrayInteropTest
    {
        [Fact]
        public void DangerousCreateFromUnderlyingArray()
        {
            byte[] array = new byte[3] { 1, 2, 3 };
            byte[] arrayCopy = array;
            ImmutableArray<byte> immutable = ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref array);

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
            byte[] array = null;
            ImmutableArray<byte> immutable = ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref array);

            Assert.True(immutable.IsDefault);
        }

        [Fact]
        public void DangerousGetUnderlyingArray()
        {
            ImmutableArray<byte> immutable = ImmutableArray.Create<byte>(1, 2, 3);
            byte[] array = ImmutableByteArrayInterop.DangerousGetUnderlyingArray(immutable);

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
            ImmutableArray<byte> immutable = default(ImmutableArray<byte>);

            Assert.Null(ImmutableByteArrayInterop.DangerousGetUnderlyingArray(immutable));
        }
    }
}
