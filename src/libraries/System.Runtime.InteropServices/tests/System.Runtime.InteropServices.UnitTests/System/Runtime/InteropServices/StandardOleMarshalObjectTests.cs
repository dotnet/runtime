// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
    public class StandardOleMarshalObjectTests
    {
        [Fact]
        public void CanGetIDispatchOfDerivedObject()
        {
            IntPtr disp = Marshal.GetIDispatchForObject(new DerivedObject());
            Assert.NotEqual(IntPtr.Zero, disp);
            Marshal.Release(disp);
        }

        [ComVisible(true)]
        public sealed class DerivedObject : StandardOleMarshalObject
        {
        }
    }
}