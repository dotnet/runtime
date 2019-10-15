// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class LabelHandleTests
    {
        [Fact]
        public void Equality()
        {
            var a1 = new LabelHandle(1);
            var a2 = new LabelHandle(2);
            var b1 = new LabelHandle(1);

            Assert.False(((object)a1).Equals(a2));
            Assert.False(a1.Equals(new object()));
            Assert.False(a1.Equals(a2));
            Assert.False(a1 == a2);

            Assert.True(((object)a1).Equals(b1));
            Assert.True(a1.Equals(b1));
            Assert.True(a1 == b1);

            Assert.Equal(a1.GetHashCode(), b1.GetHashCode());
        }
    }
}
