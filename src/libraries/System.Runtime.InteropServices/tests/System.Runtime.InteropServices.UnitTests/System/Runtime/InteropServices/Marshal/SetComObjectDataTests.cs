// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class SetComObjectDataTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void SetComObjectData_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.SetComObjectData(null, null, null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void SetComObjectData_NullObj_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Marshal.SetComObjectData(null, new object(), 3));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void SetComObjectData_NullKey_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => Marshal.SetComObjectData(new object(), null, 3));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void SetComObjectData_NonComObjectObj_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentException>("obj", () => Marshal.SetComObjectData(1, 2, 3));
        }
    }
}
