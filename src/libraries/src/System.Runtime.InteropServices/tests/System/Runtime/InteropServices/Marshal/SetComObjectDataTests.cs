// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class SetComObjectDataTests
    {
        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.SetComObjectData is not implemented in .NET Core.")]
        public void SetComObjectData_NetFramework_ReturnsExpected()
        {
            Type type = Type.GetTypeFromCLSID(new Guid("927971f5-0939-11d1-8be1-00c04fd8d503"));
            object comObject = Activator.CreateInstance(type);

            Assert.True(Marshal.SetComObjectData(comObject, "key", 1));
            Assert.Equal(1, Marshal.GetComObjectData(comObject, "key"));

            Assert.False(Marshal.SetComObjectData(comObject, "key", 2));
            Assert.Equal(1, Marshal.GetComObjectData(comObject, "key"));

            Assert.True(Marshal.SetComObjectData(comObject, "otherKey", 2));
            Assert.Equal(2, Marshal.GetComObjectData(comObject, "otherKey"));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Marshal.SetComObjectData is not implemented in .NET Core.")]
        [ActiveIssue(31214)]
        public void SetComObjectData_NetCore_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.SetComObjectData(null, null, null));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.SetComObjectData is not implemented in .NET Core.")]
        public void SetComObjectData_NullObj_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Marshal.SetComObjectData(null, new object(), 3));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.SetComObjectData is not implemented in .NET Core.")]
        public void SetComObjectData_NullKey_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => Marshal.SetComObjectData(new object(), null, 3));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.SetComObjectData is not implemented in .NET Core.")]
        public void SetComObjectData_NonComObjectObj_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentException>("obj", () => Marshal.SetComObjectData(1, 2, 3));
        }
    }
}
