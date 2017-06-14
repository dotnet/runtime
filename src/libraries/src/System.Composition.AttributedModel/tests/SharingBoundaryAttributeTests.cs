// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Composition.Tests
{
    public class SharingBoundaryAttributeTests
    {
        public static IEnumerable<object[]> SharingBoundaryNames_TestData()
        {
            yield return new object[] { new string[0] };
            yield return new object[] { new string[] { "1", null, "2" } };
        }

        [Theory]
        [MemberData(nameof(SharingBoundaryNames_TestData))]
        public void Ctor_SharingBoundaryNames(string[] sharingBoundaryNames)
        {
            var attribute = new SharingBoundaryAttribute(sharingBoundaryNames);
            Assert.Equal(sharingBoundaryNames, attribute.SharingBoundaryNames);
        }

        [Fact]
        public void Ctor_NullSharingBoundaryNames_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("sharingBoundaryNames", () => new SharingBoundaryAttribute(null));
        }
    }
}

