// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GetIDispatchForObjectTests
    {
        public static IEnumerable<object[]> GetIDispatchForObject_Valid_TestData()
        {
            yield return new object[] { new NonGenericClass() };
            yield return new object[] { new NonGenericStruct() };

            Type type = Type.GetTypeFromCLSID(new Guid("927971f5-0939-11d1-8be1-00c04fd8d503"));
            object comObject = Activator.CreateInstance(type);
            yield return new object[] { comObject };
        }

        [Theory]
        [MemberData(nameof(GetIDispatchForObject_Valid_TestData))]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.GetIDispatchForObject is not implemented in .NET Core.")]
        public void GetIDispatchForObject_NetFramework_ReturnsNonZero(object o)
        {
            IntPtr iDispatch = Marshal.GetIDispatchForObject(o);
            try
            {
                Assert.NotEqual(IntPtr.Zero, iDispatch);
            }
            finally
            {
                Marshal.Release(iDispatch);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Marshal.GetIDispatchForObject is not implemented in .NET Core.")]
        public void GetIDispatchForObject_NetCore_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetIDispatchForObject(null));
        }

        public static IEnumerable<object[]> GetIDispatchForObject_Invalid_TestData()
        {
            yield return new object[] { new GenericClass<string>() };
            yield return new object[] { new GenericStruct<string>() };
        }

        [Theory]
        [MemberData(nameof(GetIDispatchForObject_Invalid_TestData))]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.GetIDispatchForObject is not implemented in .NET Core.")]
        public void GetIDispatchForObject_InvalidObject_ThrowsInvalidCastException(object o)
        {
            Assert.Throws<InvalidCastException>(() => Marshal.GetIDispatchForObject(o));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.GetIDispatchForObject is not implemented in .NET Core.")]
        public void GetIDispatchForObject_NullObject_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("o", () => Marshal.GetIDispatchForObject(null));
        }
    }
}
