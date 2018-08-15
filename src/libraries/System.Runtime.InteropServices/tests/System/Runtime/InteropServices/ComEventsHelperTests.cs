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
        [SkipOnTargetFramework(~TargetFrameworkMonikers.UapAot, "Throws PlatformNotSupportedException in UapAot")]
        public void Combine_UapAot_PlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ComEventsHelper.Combine(null, Guid.Empty, 1, null));
        }
        
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Combine_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ComEventsHelper.Combine(null, Guid.Empty, 1, null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Throws PlatformNotSupportedException in UapAot")]
        public void Combine_NullRcw_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>(null, () => ComEventsHelper.Combine(null, Guid.Empty, 1, null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Throws PlatformNotSupportedException in UapAot")]
        public void Combine_NotComObject_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("obj", () => ComEventsHelper.Combine(1, Guid.Empty, 1, null));
        }

        [SkipOnTargetFramework(~TargetFrameworkMonikers.UapAot, "Throws PlatformNotSupportedException in UapAot")]
        public void Remove_UapAot_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ComEventsHelper.Remove(null, Guid.Empty, 1, null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Remove_Unix_ThrowPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ComEventsHelper.Remove(null, Guid.Empty, 1, null));   
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Throws PlatformNotSupportedException in UapAot")]
        public void Remove_NullRcw_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>(null, () => ComEventsHelper.Remove(null, Guid.Empty, 1, null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "ComEventsHelper.Combine is not supported in .NET Core.")]
        public void Remove_NotComObject_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("obj", () => ComEventsHelper.Remove(1, Guid.Empty, 1, null));
        }
    }
#pragma warning restore 0618
}
