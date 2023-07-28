// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class CreateWrapperOfTypeTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void CreateWrapperOfType_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.CreateWrapperOfType("object", null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void CreateWrapperOfType_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("t", () => Marshal.CreateWrapperOfType("object", null));
        }
    }
}
