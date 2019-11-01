// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SetComObjectData_NullObj_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Marshal.SetComObjectData(null, new object(), 3));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SetComObjectData_NullKey_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => Marshal.SetComObjectData(new object(), null, 3));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SetComObjectData_NonComObjectObj_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentException>("obj", () => Marshal.SetComObjectData(1, 2, 3));
        }
    }
}
