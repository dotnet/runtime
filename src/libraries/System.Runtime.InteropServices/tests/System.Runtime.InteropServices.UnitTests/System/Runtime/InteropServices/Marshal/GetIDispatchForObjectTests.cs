// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetIDispatchForObjectTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetIDispatchForObject_NetCore_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetIDispatchForObject(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void GetIDispatchForObject_NullObject_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("o", () => Marshal.GetIDispatchForObject(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void GetIDispatchForObject_NonDispatchObject_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => Marshal.GetIDispatchForObject(string.Empty));
        }
    }
}
