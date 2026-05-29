// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    public unsafe partial class IPropertiesTests
    {
        [LibraryImport(NativeExportsNE.NativeExportsNE_Binary, EntryPoint = "new_properties")]
        public static partial void* NewProperties();

        // -------------------------------------------------------------------------
        // Round-trip an in-process [GeneratedComClass] through CCW + RCW. Each test
        // exercises one property to keep failure modes precisely localized.
        // -------------------------------------------------------------------------

        private static (Properties Impl, IProperties Rcw) CreateRcwAroundCcw()
        {
            var impl = new Properties();
            var cw = new StrategyBasedComWrappers();
            var comPtr = cw.GetOrCreateComInterfaceForObject(impl, CreateComInterfaceFlags.None);
            var comObject = cw.GetOrCreateObjectForComInstance(comPtr, CreateObjectFlags.None);
            return (impl, (IProperties)comObject);
        }

        [Fact]
        public void RcwAroundCcw_IntProperty_RoundTrips()
        {
            (Properties impl, IProperties rcw) = CreateRcwAroundCcw();

            rcw.IntProperty = 123;
            Assert.Equal(123, rcw.IntProperty);
            Assert.Equal(123, impl.IntProperty);
        }

        [Fact]
        public void RcwAroundCcw_ReadOnlyInt_ReturnsImplValue()
        {
            (_, IProperties rcw) = CreateRcwAroundCcw();

            Assert.Equal(111, rcw.ReadOnlyInt);
        }

        [Fact]
        public void RcwAroundCcw_WriteOnlyInt_PropagatesToImpl()
        {
            (Properties impl, IProperties rcw) = CreateRcwAroundCcw();

            rcw.WriteOnlyInt = 99;
            Assert.Equal(99, impl.WriteOnlyIntSink);
        }

        [Fact]
        public void RcwAroundCcw_GuidProperty_RoundTrips()
        {
            (_, IProperties rcw) = CreateRcwAroundCcw();

            Guid value = Guid.NewGuid();
            rcw.GuidProperty = value;
            Assert.Equal(value, rcw.GuidProperty);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Hello, World!")]
        [InlineData("Unicode \u4F60\u597D \uD83D\uDE00")]
        public void RcwAroundCcw_StringProperty_RoundTrips(string value)
        {
            (_, IProperties rcw) = CreateRcwAroundCcw();

            rcw.StringProperty = value;
            Assert.Equal(value, rcw.StringProperty);
        }

        [Fact]
        public void RcwAroundCcw_Self_NullByDefault()
        {
            (_, IProperties rcw) = CreateRcwAroundCcw();

            Assert.Null(rcw.Self);
        }

        [Fact]
        public void RcwAroundCcw_Self_RoundTripsInterfaceReference()
        {
            (_, IProperties rcw) = CreateRcwAroundCcw();

            var other = new Properties();
            other.IntProperty = 88;
            rcw.Self = other;

            IProperties? selfRcw = rcw.Self;
            Assert.NotNull(selfRcw);
            Assert.Equal(88, selfRcw!.IntProperty);
        }

        // -------------------------------------------------------------------------
        // RCW path only, driven by a hand-rolled native vtable (`new_properties`
        // from NativeExports/ComInterfaceGenerator/Properties.cs). This proves the
        // generated RCW marshals correctly against external native code, not just
        // against our own [GeneratedComClass] CCW.
        // -------------------------------------------------------------------------

        private static IProperties CreateRcwOverNativeShim()
        {
            var cw = new StrategyBasedComWrappers();
            void* nativePtr = NewProperties();
            return (IProperties)cw.GetOrCreateObjectForComInstance((nint)nativePtr, CreateObjectFlags.None);
        }

        [Fact]
        public void NativeShim_IntProperty_RoundTrips()
        {
            IProperties rcw = CreateRcwOverNativeShim();

            rcw.IntProperty = 7;
            Assert.Equal(7, rcw.IntProperty);
        }

        [Fact]
        public void NativeShim_ReadOnlyInt_ReturnsShimValue()
        {
            IProperties rcw = CreateRcwOverNativeShim();

            Assert.Equal(111, rcw.ReadOnlyInt);
        }

        [Fact]
        public void NativeShim_WriteOnlyInt_DoesNotThrow()
        {
            IProperties rcw = CreateRcwOverNativeShim();

            rcw.WriteOnlyInt = 13;
        }

        [Fact]
        public void NativeShim_GuidProperty_RoundTrips()
        {
            IProperties rcw = CreateRcwOverNativeShim();

            Guid value = Guid.NewGuid();
            rcw.GuidProperty = value;
            Assert.Equal(value, rcw.GuidProperty);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Hello, World!")]
        [InlineData("Unicode \u4F60\u597D \uD83D\uDE00")]
        public void NativeShim_StringProperty_RoundTrips(string value)
        {
            IProperties rcw = CreateRcwOverNativeShim();

            rcw.StringProperty = value;
            Assert.Equal(value, rcw.StringProperty);
        }
    }
}
