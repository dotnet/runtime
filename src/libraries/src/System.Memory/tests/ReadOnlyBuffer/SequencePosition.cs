// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Memory.Tests
{
    public class SequencePostionTests
    {
        [Fact]
        public void ComparisonMembers_NotNullSegment()
        {
            var segment = new object();
            var position = new SequencePosition(segment, 2);
            var position2 = new SequencePosition(segment, 2);

            Assert.True(position == position2);
            Assert.True(position.Equals(position2));
            Assert.True(position.Equals((object)position2));
            Assert.False(position != position2);
            Assert.Equal(position.GetHashCode(), position2.GetHashCode());
        }

        [Fact]
        public void ComparisonMembers_NullSegment()
        {
            var position = new SequencePosition(null, 2);
            var position2 = new SequencePosition(null, 2);

            Assert.True(position == position2);
            Assert.True(position.Equals(position2));
            Assert.True(position.Equals((object)position2));
            Assert.False(position != position2);
            Assert.Equal(position.GetHashCode(), position2.GetHashCode());
        }
    }
}
