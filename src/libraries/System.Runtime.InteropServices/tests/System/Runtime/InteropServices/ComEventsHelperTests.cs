// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
#pragma warning disable 0618 // ComEventsHelper is marked as Obsolete.
    public class ComEventsHelperTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Throws PlatformNotSupportedException in UapAot")]
        public void Combine_NullRcwWindows_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>(null, () => ComEventsHelper.Combine(null, Guid.Empty, 1, null));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.UapAot, "Throws PlatformNotSupportedException in UapAot")]
        public void Combine_NullRcwUapAot_PlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ComEventsHelper.Combine(null, Guid.Empty, 1, null));
        }
        
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Combine_NullRcwUnix_ThrowsArgumentNullException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ComEventsHelper.Combine(null, Guid.Empty, 1, null));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "ComEventsHelper.Combine is not supported in .NET Core.")]
        public void Combine_NonNullRcw_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ComEventsHelper.Combine(1, Guid.Empty, 1, null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Throws PlatformNotSupportedException in UapAot")]
        public void Remove_NullRcwWindows_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>(null, () => ComEventsHelper.Remove(null, Guid.Empty, 1, null));
        }

        [SkipOnTargetFramework(~TargetFrameworkMonikers.UapAot, "Throws PlatformNotSupportedException in UapAot")]
        public void Remove_NullRcwUapAot_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ComEventsHelper.Remove(null, Guid.Empty, 1, null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Remove_NullRcwUnix_ThrowPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ComEventsHelper.Remove(null, Guid.Empty, 1, null));   
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "ComEventsHelper.Combine is not supported in .NET Core.")]
        public void Remove_NonNullRcw_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ComEventsHelper.Remove(1, Guid.Empty, 1, null));
        }
    }
#pragma warning restore 0618
}
