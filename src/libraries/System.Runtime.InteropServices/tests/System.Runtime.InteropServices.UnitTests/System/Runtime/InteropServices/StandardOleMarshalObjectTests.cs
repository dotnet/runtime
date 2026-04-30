// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class StandardOleMarshalObjectTests
    {
        private static readonly Guid IID_IDispatch = new Guid("00020400-0000-0000-C000-000000000046");

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void CanGetIDispatchOfDerivedObject()
        {
            IntPtr disp = Marshal.GetIDispatchForObject(new DerivedObject());
            Assert.NotEqual(IntPtr.Zero, disp);
            Marshal.Release(disp);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void CanQueryInterfaceForIDispatchOfDerivedObject()
        {
            IntPtr unk = Marshal.GetIUnknownForObject(new DerivedObject());
            Assert.NotEqual(IntPtr.Zero, unk);

            int hr = Marshal.QueryInterface(unk, IID_IDispatch, out IntPtr disp);
            Assert.Equal(0, hr);
            Assert.NotEqual(IntPtr.Zero, disp);
 
            Marshal.Release(disp);
            Marshal.Release(unk);
        }

        [ComVisible(true)]
        public sealed class DerivedObject : StandardOleMarshalObject
        {
        }
    }
}
