// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetComObjectDataTests
    {
        [Fact]

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetComObjectData_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetComObjectData(null, null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComObjectData_NullObj_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Marshal.GetComObjectData(null, new object()));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComObjectData_NullKey_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => Marshal.GetComObjectData(new object(), null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComObjectData_NonComObjectObj_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentException>("obj", () => Marshal.GetComObjectData(1, 2));
        }
    }
}
