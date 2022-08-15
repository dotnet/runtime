// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ComEventsHelperTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Combine_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ComEventsHelper.Combine(null, Guid.Empty, 1, null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void Combine_NullRcw_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>(null, () => ComEventsHelper.Combine(null, Guid.Empty, 1, null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void Combine_NotComObject_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("obj", () => ComEventsHelper.Combine(1, Guid.Empty, 1, null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Remove_Unix_ThrowPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ComEventsHelper.Remove(null, Guid.Empty, 1, null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void Remove_NullRcw_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>(null, () => ComEventsHelper.Remove(null, Guid.Empty, 1, null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void Remove_NotComObject_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("obj", () => ComEventsHelper.Remove(1, Guid.Empty, 1, null));
        }
    }
}
