// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetIUnknownForObjectTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetIUnknownForObject_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetIUnknownForObject(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void GetIUnknownForObject_NullObject_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("o", () => Marshal.GetIUnknownForObject(null));
        }
    }
}
