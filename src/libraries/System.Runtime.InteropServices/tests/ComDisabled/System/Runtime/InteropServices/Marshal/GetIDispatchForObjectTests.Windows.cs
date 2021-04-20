// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetIDispatchForObjectTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetIDispatchForObject_DispatchObject_Fail()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetIDispatchForObject(new object()));
        }
    }
}
