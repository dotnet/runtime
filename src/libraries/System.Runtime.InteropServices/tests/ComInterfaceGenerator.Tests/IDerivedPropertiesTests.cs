// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    public unsafe partial class IDerivedPropertiesTests
    {
        [LibraryImport(NativeExportsNE.NativeExportsNE_Binary, EntryPoint = "new_derived_properties")]
        public static partial void* NewDerivedProperties();

        private static (DerivedProperties Impl, IDerivedProperties Rcw) CreateRcwAroundCcw()
        {
            var impl = new DerivedProperties();
            var cw = new StrategyBasedComWrappers();
            var comPtr = cw.GetOrCreateComInterfaceForObject(impl, CreateComInterfaceFlags.None);
            try
            {
                var comObject = cw.GetOrCreateObjectForComInstance(comPtr, CreateObjectFlags.None);
                return (impl, (IDerivedProperties)comObject);
            }
            finally
            {
                Marshal.Release(comPtr);
            }
        }

        // -------------------------------------------------------------------------
        // Inherited properties — exercised via the derived RCW. Proves that
        // accessor slots inherited from IProperties are emitted on the derived
        // interface and dispatch into the correct underlying implementation.
        // -------------------------------------------------------------------------

        [Fact]
        public void Inherited_IntProperty_RoundTrips()
        {
            (DerivedProperties impl, IDerivedProperties rcw) = CreateRcwAroundCcw();

            rcw.IntProperty = 321;
            Assert.Equal(321, rcw.IntProperty);
            Assert.Equal(321, impl.IntProperty);
        }

        [Fact]
        public void Inherited_ReadOnlyInt_ReturnsImplValue()
        {
            (_, IDerivedProperties rcw) = CreateRcwAroundCcw();

            Assert.Equal(111, rcw.ReadOnlyInt);
        }

        [Fact]
        public void Inherited_WriteOnlyInt_PropagatesToImpl()
        {
            (DerivedProperties impl, IDerivedProperties rcw) = CreateRcwAroundCcw();

            rcw.WriteOnlyInt = 77;
            Assert.Equal(77, impl.WriteOnlyIntSink);
        }

        [Fact]
        public void Inherited_GuidProperty_RoundTrips()
        {
            (_, IDerivedProperties rcw) = CreateRcwAroundCcw();

            Guid value = Guid.NewGuid();
            rcw.GuidProperty = value;
            Assert.Equal(value, rcw.GuidProperty);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Hello, World!")]
        [InlineData("Unicode \u4F60\u597D \uD83D\uDE00")]
        public void Inherited_StringProperty_RoundTrips(string value)
        {
            (_, IDerivedProperties rcw) = CreateRcwAroundCcw();

            rcw.StringProperty = value;
            Assert.Equal(value, rcw.StringProperty);
        }

        [Fact]
        public void Inherited_Self_NullByDefault()
        {
            (_, IDerivedProperties rcw) = CreateRcwAroundCcw();

            Assert.Null(rcw.Self);
        }

        [Fact]
        public void Inherited_Self_RoundTripsInterfaceReference()
        {
            (_, IDerivedProperties rcw) = CreateRcwAroundCcw();

            var other = new Properties();
            other.IntProperty = 88;
            rcw.Self = other;

            IProperties? selfRcw = rcw.Self;
            Assert.NotNull(selfRcw);
            Assert.Equal(88, selfRcw!.IntProperty);
        }

        // -------------------------------------------------------------------------
        // Derived-only properties — declared on IDerivedProperties.
        // -------------------------------------------------------------------------

        [Fact]
        public void Derived_DerivedIntProperty_RoundTrips()
        {
            (_, IDerivedProperties rcw) = CreateRcwAroundCcw();

            rcw.DerivedIntProperty = 555;
            Assert.Equal(555, rcw.DerivedIntProperty);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Derived value")]
        [InlineData("Unicode \u4F60\u597D \uD83D\uDE00")]
        public void Derived_DerivedStringProperty_RoundTrips(string value)
        {
            (_, IDerivedProperties rcw) = CreateRcwAroundCcw();

            rcw.DerivedStringProperty = value;
            Assert.Equal(value, rcw.DerivedStringProperty);
        }

        [Fact]
        public void Derived_DerivedReadOnlyInt_ReturnsImplValue()
        {
            (_, IDerivedProperties rcw) = CreateRcwAroundCcw();

            Assert.Equal(2222, rcw.DerivedReadOnlyInt);
        }

        // -------------------------------------------------------------------------
        // QI smoke test: the derived RCW can be cast back to the base interface
        // and inherited properties remain functional through the base RCW.
        // -------------------------------------------------------------------------

        [Fact]
        public void QI_DerivedToBase_AccessesInheritedProperties()
        {
            (DerivedProperties impl, IDerivedProperties derivedRcw) = CreateRcwAroundCcw();
            IProperties baseRcw = (IProperties)derivedRcw;

            baseRcw.IntProperty = 1234;
            Assert.Equal(1234, baseRcw.IntProperty);
            Assert.Equal(1234, impl.IntProperty);

            derivedRcw.DerivedIntProperty = 9999;
            Assert.Equal(9999, derivedRcw.DerivedIntProperty);
        }

        // -------------------------------------------------------------------------
        // VTable layout: the derived interface vtable must begin with the base
        // interface vtable verbatim. Mirrors IDerivedTests' equivalent check
        // and is the strongest proof that inherited-property codegen emits the
        // accessor slots in the correct order.
        // -------------------------------------------------------------------------

        [Fact]
        public unsafe void DerivedVtable_StartsWithBaseInterfaceLayout()
        {
            IIUnknownDerivedDetails baseInterfaceDetails =
                StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IProperties).TypeHandle);
            IIUnknownDerivedDetails derivedInterfaceDetails =
                StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IDerivedProperties).TypeHandle);

            int numBaseMethods = typeof(IProperties).GetMethods().Length;
            int numPointersToCompare = 3 + numBaseMethods;

            var expected = new ReadOnlySpan<nint>(baseInterfaceDetails.ManagedVirtualMethodTable, numPointersToCompare);
            var actual = new ReadOnlySpan<nint>(derivedInterfaceDetails.ManagedVirtualMethodTable, numPointersToCompare);

            Assert.True(expected.SequenceEqual(actual));
        }

        // -------------------------------------------------------------------------
        // RCW path only, driven by a hand-rolled native vtable (`new_derived_properties`
        // from NativeExports/ComInterfaceGenerator/DerivedProperties.cs). The native
        // shim registers two ComInterfaceEntry items (IProperties + IDerivedProperties);
        // the derived vtable replicates the base layout in its first 13 slots and
        // appends 5 derived-only slots. These tests prove the generated derived RCW
        // marshals correctly against external native code, including dispatch through
        // inherited accessor slots.
        // -------------------------------------------------------------------------

        private static IDerivedProperties CreateRcwOverNativeShim()
        {
            var cw = new StrategyBasedComWrappers();
            nint nativePtr = (nint)NewDerivedProperties();
            try
            {
                return (IDerivedProperties)cw.GetOrCreateObjectForComInstance(nativePtr, CreateObjectFlags.None);
            }
            finally
            {
                Marshal.Release(nativePtr);
            }
        }

        [Fact]
        public void NativeShim_Inherited_IntProperty_RoundTrips()
        {
            IDerivedProperties rcw = CreateRcwOverNativeShim();

            rcw.IntProperty = 11;
            Assert.Equal(11, rcw.IntProperty);
        }

        [Fact]
        public void NativeShim_Inherited_ReadOnlyInt_ReturnsShimValue()
        {
            IDerivedProperties rcw = CreateRcwOverNativeShim();

            Assert.Equal(111, rcw.ReadOnlyInt);
        }

        [Fact]
        public void NativeShim_Inherited_WriteOnlyInt_DoesNotThrow()
        {
            IDerivedProperties rcw = CreateRcwOverNativeShim();

            rcw.WriteOnlyInt = 17;
        }

        [Fact]
        public void NativeShim_Inherited_GuidProperty_RoundTrips()
        {
            IDerivedProperties rcw = CreateRcwOverNativeShim();

            Guid value = Guid.NewGuid();
            rcw.GuidProperty = value;
            Assert.Equal(value, rcw.GuidProperty);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Hello, World!")]
        [InlineData("Unicode \u4F60\u597D \uD83D\uDE00")]
        public void NativeShim_Inherited_StringProperty_RoundTrips(string value)
        {
            IDerivedProperties rcw = CreateRcwOverNativeShim();

            rcw.StringProperty = value;
            Assert.Equal(value, rcw.StringProperty);
        }

        [Fact]
        public void NativeShim_Derived_DerivedIntProperty_RoundTrips()
        {
            IDerivedProperties rcw = CreateRcwOverNativeShim();

            rcw.DerivedIntProperty = 222;
            Assert.Equal(222, rcw.DerivedIntProperty);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Derived value")]
        [InlineData("Unicode \u4F60\u597D \uD83D\uDE00")]
        public void NativeShim_Derived_DerivedStringProperty_RoundTrips(string value)
        {
            IDerivedProperties rcw = CreateRcwOverNativeShim();

            rcw.DerivedStringProperty = value;
            Assert.Equal(value, rcw.DerivedStringProperty);
        }

        [Fact]
        public void NativeShim_Derived_DerivedReadOnlyInt_ReturnsShimValue()
        {
            IDerivedProperties rcw = CreateRcwOverNativeShim();

            Assert.Equal(2222, rcw.DerivedReadOnlyInt);
        }

        [Fact]
        public void NativeShim_QI_DerivedToBase_AccessesInheritedProperties()
        {
            IDerivedProperties derivedRcw = CreateRcwOverNativeShim();
            IProperties baseRcw = (IProperties)derivedRcw;

            baseRcw.IntProperty = 4321;
            // Reads through the base interface go via the base vtable slot,
            // which the shim shares state with the derived vtable's inherited slot.
            Assert.Equal(4321, baseRcw.IntProperty);
            Assert.Equal(4321, derivedRcw.IntProperty);
        }

        // -------------------------------------------------------------------------
        // User-defined `new`-keyword property shadowing — mirrors the IHide method
        // pattern for properties. A derived [GeneratedComInterface] that declares
        // `new int Shadowed { get; set; }` should get its own fresh vtable slots
        // for the get/set accessors, independent of the inherited base slots.
        // The CCW class differentiates the two via explicit interface implementations
        // (IPropertyShadowingBase.Shadowed vs IPropertyShadowingDerived.Shadowed),
        // so reads/writes through each interface route to a distinct backing field.
        // -------------------------------------------------------------------------

        private static (PropertyShadowingImpl Impl, IPropertyShadowingDerived Rcw) CreatePropertyShadowingRcwAroundCcw()
        {
            var impl = new PropertyShadowingImpl();
            var cw = new StrategyBasedComWrappers();
            var comPtr = cw.GetOrCreateComInterfaceForObject(impl, CreateComInterfaceFlags.None);
            try
            {
                var comObject = cw.GetOrCreateObjectForComInstance(comPtr, CreateObjectFlags.None);
                return (impl, (IPropertyShadowingDerived)comObject);
            }
            finally
            {
                Marshal.Release(comPtr);
            }
        }

        [Fact]
        public void NewShadow_DerivedAccessor_RoutesToDerivedSlot()
        {
            (PropertyShadowingImpl impl, IPropertyShadowingDerived rcw) = CreatePropertyShadowingRcwAroundCcw();

            rcw.Shadowed = 42;

            Assert.Equal(42, rcw.Shadowed);
            Assert.Equal(42, impl.DerivedShadowedSink);
            Assert.Equal(0, impl.BaseShadowedSink);
        }

        [Fact]
        public void NewShadow_BaseAccessor_RoutesToBaseSlot()
        {
            (PropertyShadowingImpl impl, IPropertyShadowingDerived rcw) = CreatePropertyShadowingRcwAroundCcw();
            IPropertyShadowingBase baseRcw = (IPropertyShadowingBase)rcw;

            baseRcw.Shadowed = 99;

            Assert.Equal(99, baseRcw.Shadowed);
            Assert.Equal(99, impl.BaseShadowedSink);
            Assert.Equal(0, impl.DerivedShadowedSink);
        }

        [Fact]
        public void NewShadow_SlotsAreIndependent()
        {
            (PropertyShadowingImpl impl, IPropertyShadowingDerived rcw) = CreatePropertyShadowingRcwAroundCcw();
            IPropertyShadowingBase baseRcw = (IPropertyShadowingBase)rcw;

            baseRcw.Shadowed = 7;
            rcw.Shadowed = 13;

            Assert.Equal(7, baseRcw.Shadowed);
            Assert.Equal(13, rcw.Shadowed);
            Assert.Equal(7, impl.BaseShadowedSink);
            Assert.Equal(13, impl.DerivedShadowedSink);
        }

        [Fact]
        public void NewShadow_InheritedNonShadowedProperty_SharesSingleSlot()
        {
            (PropertyShadowingImpl impl, IPropertyShadowingDerived rcw) = CreatePropertyShadowingRcwAroundCcw();
            IPropertyShadowingBase baseRcw = (IPropertyShadowingBase)rcw;

            baseRcw.NotShadowed = 100;
            Assert.Equal(100, baseRcw.NotShadowed);
            Assert.Equal(100, rcw.NotShadowed);

            rcw.NotShadowed = 200;
            Assert.Equal(200, baseRcw.NotShadowed);
            Assert.Equal(200, rcw.NotShadowed);
            Assert.Equal(200, impl.NotShadowed);
        }

        [Fact]
        public unsafe void NewShadow_DerivedVtable_AppendsFreshSlotsForShadowedProperty()
        {
            IIUnknownDerivedDetails baseInterfaceDetails =
                StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IPropertyShadowingBase).TypeHandle);
            IIUnknownDerivedDetails derivedInterfaceDetails =
                StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IPropertyShadowingDerived).TypeHandle);

            int numBaseAccessors = typeof(IPropertyShadowingBase).GetMethods().Length;
            int numBaseSlots = 3 + numBaseAccessors;

            var baseVtable = new ReadOnlySpan<nint>(baseInterfaceDetails.ManagedVirtualMethodTable, numBaseSlots);
            var derivedInheritedRange = new ReadOnlySpan<nint>(derivedInterfaceDetails.ManagedVirtualMethodTable, numBaseSlots);

            Assert.True(baseVtable.SequenceEqual(derivedInheritedRange));

            (PropertyShadowingImpl impl, IPropertyShadowingDerived rcw) = CreatePropertyShadowingRcwAroundCcw();
            var (thisPtr, derivedVtable) = ((IUnmanagedVirtualMethodTableProvider)rcw).GetVirtualMethodTableInfoForKey(typeof(IPropertyShadowingDerived));

            int derivedSetSlot = numBaseSlots + 1;
            int derivedGetSlot = numBaseSlots;

            int sentinel = 12345;
            int hr = ((delegate* unmanaged[MemberFunction]<void*, int, int>)derivedVtable[derivedSetSlot])(thisPtr, sentinel);
            Assert.Equal(0, hr);
            Assert.Equal(sentinel, impl.DerivedShadowedSink);
            Assert.Equal(0, impl.BaseShadowedSink);

            int readBack;
            hr = ((delegate* unmanaged[MemberFunction]<void*, int*, int>)derivedVtable[derivedGetSlot])(thisPtr, &readBack);
            Assert.Equal(0, hr);
            Assert.Equal(sentinel, readBack);
        }
    }
}
