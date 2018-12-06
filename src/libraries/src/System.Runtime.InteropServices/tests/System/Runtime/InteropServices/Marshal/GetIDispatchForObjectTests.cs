// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetIDispatchForObjectTests
    {
        public static IEnumerable<object[]> GetIDispatchForObject_Valid_TestData()
        {
            yield return new object[] { new NonGenericClass() };
            yield return new object[] { new NonGenericStruct() };
        }

        [Theory]
        [MemberData(nameof(GetIDispatchForObject_Valid_TestData))]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.GetIDispatchForObject is not implemented in .NET Core.")]
        public void GetIDispatchForObject_ValidObject_Roundtrips(object o)
        {
            IntPtr ptr = Marshal.GetIDispatchForObject(o);
            try
            {
                Assert.NotEqual(IntPtr.Zero, ptr);
            }
            finally
            {
                Marshal.Release(ptr);
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

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.GetIDispatchForObject is not implemented in .NET Core.")]
        public void GetIDispatchForObject_NullObject_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("o", () => Marshal.GetIDispatchForObject(null));
        }

        [Theory]
        [MemberData(nameof(GetIDispatchForObject_Invalid_TestData))]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.GetIDispatchForObject is not implemented in .NET Core.")]
        public void GetIDispatchForObject_InvalidObject_ThrowsInvalidCastException(object o)
        {
            Assert.Throws<InvalidCastException>(() => Marshal.GetIDispatchForObject(o));
        }
    }
}
