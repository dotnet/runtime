// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public class OptionalFieldAttributeTests
    {
        [Fact]
        public void VersionAdded_InvalidArgument_ThrowsException()
        {
            var ofa = new OptionalFieldAttribute();
            AssertExtensions.Throws<ArgumentException>(null, () => ofa.VersionAdded = 0);
            AssertExtensions.Throws<ArgumentException>(null, () => ofa.VersionAdded = -1);
        }

        [Fact]
        public void VersionAdded_Roundtrips()
        {
            var ofa = new OptionalFieldAttribute();
            Assert.Equal(1, ofa.VersionAdded);
            ofa.VersionAdded = 2;
            Assert.Equal(2, ofa.VersionAdded);
            ofa.VersionAdded = int.MaxValue;
            Assert.Equal(int.MaxValue, ofa.VersionAdded);
        }
    }
}
