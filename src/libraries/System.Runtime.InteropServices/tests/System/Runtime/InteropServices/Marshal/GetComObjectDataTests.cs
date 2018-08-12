// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GetComObjectDataTests
    {
        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.GetComObjectData is not implemented in .NET Core.")]
        public void GetComObjectData_NetFramework_ReturnsExpected()
        {
            Type type = Type.GetTypeFromCLSID(new Guid("927971f5-0939-11d1-8be1-00c04fd8d503"));
            object comObject = Activator.CreateInstance(type);

            Assert.Null(Marshal.GetComObjectData(comObject, "key"));

            Marshal.SetComObjectData(comObject, "key", 1);
            Assert.Equal(1, Marshal.GetComObjectData(comObject, "key"));
            Assert.Null(Marshal.GetComObjectData(comObject, "noSuchKey"));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Marshal.GetComObjectData is not implemented in .NET Core.")]
        [ActiveIssue(31214)]
        public void GetComObjectData_NetCore_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetComObjectData(null, null));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.GetComObjectData is not implemented in .NET Core.")]
        public void GetComObjectData_NullObj_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Marshal.GetComObjectData(null, new object()));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.GetComObjectData is not implemented in .NET Core.")]
        public void GetComObjectData_NullKey_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => Marshal.GetComObjectData(new object(), null));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.GetComObjectData is not implemented in .NET Core.")]
        public void GetComObjectData_NonComObjectObj_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentException>("obj", () => Marshal.GetComObjectData(1, 2));
        }
    }
}
