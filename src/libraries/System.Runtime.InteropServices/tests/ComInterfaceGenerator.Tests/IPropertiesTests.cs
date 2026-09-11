// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    [Collection(nameof(TrackedIntMarshaller))]
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
            try
            {
                var comObject = cw.GetOrCreateObjectForComInstance(comPtr, CreateObjectFlags.None);
                return (impl, (IProperties)comObject);
            }
            finally
            {
                Marshal.Release(comPtr);
            }
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
            nint nativePtr = (nint)NewProperties();
            try
            {
                return (IProperties)cw.GetOrCreateObjectForComInstance(nativePtr, CreateObjectFlags.None);
            }
            finally
            {
                Marshal.Release(nativePtr);
            }
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

        // -------------------------------------------------------------------------
        // Property-accessor marshalling-attribute coverage. Verifies that
        // `[return: MarshalUsing(...)]` on a get accessor and `[param: MarshalUsing(...)]`
        // on a set accessor are honored end-to-end through both the RCW and CCW
        // marshalling pipelines.
        // -------------------------------------------------------------------------

        private static (PropertyMarshalling Impl, IPropertyMarshalling Rcw) CreatePropertyMarshallingRcwAroundCcw()
        {
            var impl = new PropertyMarshalling();
            var cw = new StrategyBasedComWrappers();
            var comPtr = cw.GetOrCreateComInterfaceForObject(impl, CreateComInterfaceFlags.None);
            try
            {
                var comObject = cw.GetOrCreateObjectForComInstance(comPtr, CreateObjectFlags.None);
                return (impl, (IPropertyMarshalling)comObject);
            }
            finally
            {
                Marshal.Release(comPtr);
            }
        }

        [Fact]
        public void PropertyAccessorMarshalling_ReadWrite_InvokesMarshallerOnBothAccessors()
        {
            TrackedIntMarshaller.Reset();

            (_, IPropertyMarshalling rcw) = CreatePropertyMarshallingRcwAroundCcw();

            rcw.TargetScoped = 7;
            int observed = rcw.TargetScoped;

            Assert.Equal(7, observed);

            // CCW + RCW round-trip causes each marshaller direction to fire at least once
            // per accessor invocation.
            Assert.True(TrackedIntMarshaller.ManagedToUnmanagedCount > 0, "ConvertToUnmanaged was never called.");
            Assert.True(TrackedIntMarshaller.UnmanagedToManagedCount > 0, "ConvertToManaged was never called.");
        }

        [Fact]
        public void PropertyAccessorMarshalling_ReadOnly_InvokesMarshallerOnGetterOnly()
        {
            TrackedIntMarshaller.Reset();

            (_, IPropertyMarshalling rcw) = CreatePropertyMarshallingRcwAroundCcw();

            int observed = rcw.ReadOnlyMarshalled;

            Assert.Equal(99, observed);

            Assert.True(TrackedIntMarshaller.ManagedToUnmanagedCount > 0, "ConvertToUnmanaged was never called for the getter return.");
            Assert.True(TrackedIntMarshaller.UnmanagedToManagedCount > 0, "ConvertToManaged was never called for the getter return.");
        }

        [Fact]
        public void PropertyAccessorMarshalling_WriteOnly_InvokesMarshallerOnSetterOnly()
        {
            TrackedIntMarshaller.Reset();

            (PropertyMarshalling impl, IPropertyMarshalling rcw) = CreatePropertyMarshallingRcwAroundCcw();

            rcw.WriteOnlyMarshalled = 23;

            Assert.Equal(23, impl.WriteOnlySink);

            Assert.True(TrackedIntMarshaller.ManagedToUnmanagedCount > 0, "ConvertToUnmanaged was never called for the setter value.");
            Assert.True(TrackedIntMarshaller.UnmanagedToManagedCount > 0, "ConvertToManaged was never called for the setter value.");
        }

        // -------------------------------------------------------------------------
        // Property-level marshalling-attribute coverage. Verifies that a bare
        // [MarshalUsing(...)] placed on the property declaration is propagated
        // to both accessors, and that per-accessor [return:]/[param:] attributes
        // take precedence over the property-level fallback.
        // -------------------------------------------------------------------------

        [Fact]
        public void PropertyLevelMarshalling_Bare_InvokesMarshallerOnBothAccessors()
        {
            TrackedIntMarshaller.Reset();
            AlternateIntMarshaller.Reset();

            (_, IPropertyMarshalling rcw) = CreatePropertyMarshallingRcwAroundCcw();

            rcw.BareMarshalled = 5;
            int observed = rcw.BareMarshalled;

            Assert.Equal(5, observed);

            Assert.True(TrackedIntMarshaller.ManagedToUnmanagedCount > 0, "Property-level [MarshalUsing] was not propagated to an accessor (ConvertToUnmanaged never invoked).");
            Assert.True(TrackedIntMarshaller.UnmanagedToManagedCount > 0, "Property-level [MarshalUsing] was not propagated to an accessor (ConvertToManaged never invoked).");
            Assert.Equal(0, AlternateIntMarshaller.ManagedToUnmanagedCount);
            Assert.Equal(0, AlternateIntMarshaller.UnmanagedToManagedCount);
        }

        [Fact]
        public void PropertyLevelMarshalling_AccessorOverride_AccessorMarshallerWinsOnBothSides()
        {
            TrackedIntMarshaller.Reset();
            AlternateIntMarshaller.Reset();

            (_, IPropertyMarshalling rcw) = CreatePropertyMarshallingRcwAroundCcw();

            rcw.AccessorOverridesProperty = 11;
            int observed = rcw.AccessorOverridesProperty;

            Assert.Equal(11, observed);

            Assert.True(TrackedIntMarshaller.ManagedToUnmanagedCount > 0, "Accessor-level [MarshalUsing] was overridden by the property-level fallback (ConvertToUnmanaged).");
            Assert.True(TrackedIntMarshaller.UnmanagedToManagedCount > 0, "Accessor-level [MarshalUsing] was overridden by the property-level fallback (ConvertToManaged).");
            Assert.Equal(0, AlternateIntMarshaller.ManagedToUnmanagedCount);
            Assert.Equal(0, AlternateIntMarshaller.UnmanagedToManagedCount);
        }

        [Fact]
        public void PropertyLevelMarshalling_MixedOverride_AccessorWinsOnGetterPropertyFallbackOnSetter()
        {
            TrackedIntMarshaller.Reset();
            AlternateIntMarshaller.Reset();

            (_, IPropertyMarshalling rcw) = CreatePropertyMarshallingRcwAroundCcw();

            rcw.MixedPropertyAndAccessor = 13;
            int observed = rcw.MixedPropertyAndAccessor;

            Assert.Equal(13, observed);

            // Getter has [return: MarshalUsing(typeof(TrackedIntMarshaller))]; setter has no override
            // and falls back to the property-level [MarshalUsing(typeof(AlternateIntMarshaller))].
            Assert.True(TrackedIntMarshaller.ManagedToUnmanagedCount > 0, "Tracked marshaller was not invoked on the getter return path.");
            Assert.True(TrackedIntMarshaller.UnmanagedToManagedCount > 0, "Tracked marshaller was not invoked on the getter return path.");
            Assert.True(AlternateIntMarshaller.ManagedToUnmanagedCount > 0, "Property-level fallback marshaller was not invoked on the setter value path.");
            Assert.True(AlternateIntMarshaller.UnmanagedToManagedCount > 0, "Property-level fallback marshaller was not invoked on the setter value path.");
        }

        [Fact]
        public void PropertyLevelMarshalling_ElementIndirection_PropertyDepth1AndAccessorDepth0Coexist()
        {
            TrackedIntMarshaller.Reset();

            (_, IPropertyMarshalling rcw) = CreatePropertyMarshallingRcwAroundCcw();

            int[] payload = new int[IPropertyMarshalling.ElementIndirectionArrayLength];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = i + 1;
            }

            rcw.ElementIndirectionArray = payload;
            int[] observed = rcw.ElementIndirectionArray;

            Assert.Equal(payload, observed);

            // The per-element marshaller runs once per element on each direction of each accessor:
            // setter writes N elements managed-to-unmanaged (CCW receives) and reads them back
            // unmanaged-to-managed (CCW stores); getter reverses. So we expect at least N invocations
            // of each direction.
            Assert.True(
                TrackedIntMarshaller.ManagedToUnmanagedCount >= IPropertyMarshalling.ElementIndirectionArrayLength,
                $"Per-element marshaller (depth 1) was dropped on the managed-to-unmanaged path: only {TrackedIntMarshaller.ManagedToUnmanagedCount} call(s) observed.");
            Assert.True(
                TrackedIntMarshaller.UnmanagedToManagedCount >= IPropertyMarshalling.ElementIndirectionArrayLength,
                $"Per-element marshaller (depth 1) was dropped on the unmanaged-to-managed path: only {TrackedIntMarshaller.UnmanagedToManagedCount} call(s) observed.");
        }

        // -------------------------------------------------------------------------
        // Shadow-property attribute propagation. A derived [GeneratedComInterface]
        // emits `new T Prop { get => ((Base)this).Prop; set => ((Base)this).Prop = value; }`
        // for each property inherited from a base [GeneratedComInterface]. User-defined
        // property-level attributes are propagated onto the shadow header so they remain
        // visible via reflection on the derived interface type. Marshalling attributes
        // ([MarshalUsing], [MarshalAs]) are intentionally stripped from the shadow because
        // it is a pure forwarder and the underlying base accessor already carries the
        // marshalling semantics.
        // -------------------------------------------------------------------------

        private static PropertyInfo GetDerivedShadowProperty(string propertyName)
        {
            PropertyInfo? shadow = typeof(IShadowAttributePropagationDerived).GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.NotNull(shadow);
            return shadow!;
        }

        [Fact]
        public void DerivedShadowProperty_PropagatesUserPropertyAttribute()
        {
            PropertyInfo shadow = GetDerivedShadowProperty(nameof(IShadowAttributePropagationBase.MarkedValue));

            ShadowAttributeMarkerAttribute? marker = shadow.GetCustomAttribute<ShadowAttributeMarkerAttribute>(inherit: false);

            Assert.NotNull(marker);
            Assert.Equal(7, marker!.Tag);
        }

        [Fact]
        public void DerivedShadowProperty_OmitsPropertyLevelMarshalUsingAttribute()
        {
            PropertyInfo shadow = GetDerivedShadowProperty(nameof(IShadowAttributePropagationBase.MarshalledValue));

            Assert.Empty(shadow.GetCustomAttributes(typeof(MarshalUsingAttribute), inherit: false));
        }

        [Fact]
        public void DerivedShadowProperty_KeepsUserAttributesAndStripsMarshallingAttributes()
        {
            PropertyInfo shadow = GetDerivedShadowProperty(nameof(IShadowAttributePropagationBase.MarkedAndMarshalledValue));

            ShadowAttributeMarkerAttribute? marker = shadow.GetCustomAttribute<ShadowAttributeMarkerAttribute>(inherit: false);
            Assert.NotNull(marker);
            Assert.Equal(13, marker!.Tag);

            Assert.Empty(shadow.GetCustomAttributes(typeof(MarshalUsingAttribute), inherit: false));
        }
    }
}
