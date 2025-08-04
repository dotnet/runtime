// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    public partial class CrossAssemblyInheritanceTests
    {
        [GeneratedComClass]
        [Guid("e0c6b35f-1234-4567-8901-123456789abc")]
        internal partial class DerivedExternalBaseImpl : IDerivedExternalBase
        {
            private int _value = 10;

            public int GetInt() => _value;
            public void SetInt(int x) => _value = x;
            public string GetName() => "DerivedExternalBase";
        }

        [GeneratedComClass]
        [Guid("e0c6b35f-1234-4567-8901-123456789abd")]
        internal partial class DerivedExternalBase2Impl : IDerivedExternalBase2
        {
            private int _value = 20;

            public int GetInt() => _value;
            public void SetInt(int x) => _value = x;
            public string GetName() => "DerivedExternalBase2";
        }

        [GeneratedComClass]
        [Guid("e0c6b35f-1234-4567-8901-123456789abe")]
        internal partial class DerivedFromExternalDerivedImpl : IDerivedFromExternalDerived
        {
            private int _value = 30;
            private bool _boolValue = true;

            public int GetInt() => _value;
            public void SetInt(int x) => _value = x;
            public bool GetBool() => _boolValue;
            public void SetBool(bool x) => _boolValue = x;
            public string GetName() => "DerivedFromExternalDerived";
        }

        [GeneratedComClass]
        [Guid("e0c6b35f-1234-4567-8901-123456789abf")]
        internal partial class DerivedFromDerivedExternalDerivedImpl : IDerivedFromDerivedExternalDerived
        {
            private int _value = 40;
            private bool _boolValue = false;
            private float _floatValue = 3.14f;

            public int GetInt() => _value;
            public void SetInt(int x) => _value = x;
            public bool GetBool() => _boolValue;
            public void SetBool(bool x) => _boolValue = x;
            public string GetName() => "DerivedFromDerivedExternalDerived";
            public float GetFloat() => _floatValue;
        }

        [Fact]
        public void IDerivedExternalBase_CanCallMethods()
        {
            var implementation = new DerivedExternalBaseImpl();
            var comWrappers = new StrategyBasedComWrappers();
            var nativeObj = comWrappers.GetOrCreateComInterfaceForObject(implementation, CreateComInterfaceFlags.None);
            var managedObj = comWrappers.GetOrCreateObjectForComInstance(nativeObj, CreateObjectFlags.None);

            var externalBase = (IExternalBase)managedObj;
            Assert.Equal(10, externalBase.GetInt());
            externalBase.SetInt(15);
            Assert.Equal(15, externalBase.GetInt());

            var derivedExternalBase = (IDerivedExternalBase)managedObj;
            Assert.Equal(15, derivedExternalBase.GetInt());
            Assert.Equal("DerivedExternalBase", derivedExternalBase.GetName());
        }

        [Fact]
        public void IDerivedExternalBase2_CanCallMethods()
        {
            var implementation = new DerivedExternalBase2Impl();
            var comWrappers = new StrategyBasedComWrappers();
            var nativeObj = comWrappers.GetOrCreateComInterfaceForObject(implementation, CreateComInterfaceFlags.None);
            var managedObj = comWrappers.GetOrCreateObjectForComInstance(nativeObj, CreateObjectFlags.None);

            // Test as base interface
            var externalBase = (IExternalBase)managedObj;
            Assert.Equal(20, externalBase.GetInt());
            externalBase.SetInt(25);
            Assert.Equal(25, externalBase.GetInt());

            // Test as derived interface
            var derivedExternalBase2 = (IDerivedExternalBase2)managedObj;
            Assert.Equal(25, derivedExternalBase2.GetInt());
            Assert.Equal("DerivedExternalBase2", derivedExternalBase2.GetName());
        }

        [Fact]
        public void IDerivedFromExternalDerived_CanCallMethods()
        {
            var implementation = new DerivedFromExternalDerivedImpl();
            var comWrappers = new StrategyBasedComWrappers();
            var nativeObj = comWrappers.GetOrCreateComInterfaceForObject(implementation, CreateComInterfaceFlags.None);
            var managedObj = comWrappers.GetOrCreateObjectForComInstance(nativeObj, CreateObjectFlags.None);

            var externalBase = (IExternalBase)managedObj;
            Assert.Equal(30, externalBase.GetInt());
            externalBase.SetInt(35);
            Assert.Equal(35, externalBase.GetInt());

            var externalDerived = (IExternalDerived)managedObj;
            Assert.Equal(35, externalDerived.GetInt());
            Assert.True(externalDerived.GetBool());
            externalDerived.SetBool(false);
            Assert.False(externalDerived.GetBool());

            var derivedFromExternalDerived = (IDerivedFromExternalDerived)managedObj;
            Assert.Equal(35, derivedFromExternalDerived.GetInt());
            Assert.False(derivedFromExternalDerived.GetBool());
            Assert.Equal("DerivedFromExternalDerived", derivedFromExternalDerived.GetName());
        }

        [Fact]
        public void IDerivedFromDerivedExternalDerived_CanCallMethods()
        {
            var implementation = new DerivedFromDerivedExternalDerivedImpl();
            var comWrappers = new StrategyBasedComWrappers();
            var nativeObj = comWrappers.GetOrCreateComInterfaceForObject(implementation, CreateComInterfaceFlags.None);
            var managedObj = comWrappers.GetOrCreateObjectForComInstance(nativeObj, CreateObjectFlags.None);

            var externalBase = (IExternalBase)managedObj;
            Assert.Equal(40, externalBase.GetInt());
            externalBase.SetInt(45);
            Assert.Equal(45, externalBase.GetInt());

            var externalDerived = (IExternalDerived)managedObj;
            Assert.Equal(45, externalDerived.GetInt());
            Assert.False(externalDerived.GetBool());
            externalDerived.SetBool(true);
            Assert.True(externalDerived.GetBool());

            var derivedFromExternalDerived = (IDerivedFromExternalDerived)managedObj;
            Assert.Equal(45, derivedFromExternalDerived.GetInt());
            Assert.True(derivedFromExternalDerived.GetBool());
            Assert.Equal("DerivedFromDerivedExternalDerived", derivedFromExternalDerived.GetName());

            var derivedFromDerivedExternalDerived = (IDerivedFromDerivedExternalDerived)managedObj;
            Assert.Equal(45, derivedFromDerivedExternalDerived.GetInt());
            Assert.True(derivedFromDerivedExternalDerived.GetBool());
            Assert.Equal("DerivedFromDerivedExternalDerived", derivedFromDerivedExternalDerived.GetName());
            Assert.Equal(3.14f, derivedFromDerivedExternalDerived.GetFloat());
        }

        [Fact]
        public unsafe void MultipleInterfacesDerivedFromSameBase_ShareCommonVTableLayout()
        {
            IIUnknownDerivedDetails baseDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy
                .GetIUnknownDerivedDetails(typeof(IExternalBase).TypeHandle);

            IIUnknownDerivedDetails derived1Details = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy
                .GetIUnknownDerivedDetails(typeof(IDerivedExternalBase).TypeHandle);

            IIUnknownDerivedDetails derived2Details = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy
                .GetIUnknownDerivedDetails(typeof(IDerivedExternalBase2).TypeHandle);

            var numBaseMethods = typeof(IExternalBase).GetMethods().Length;
            var numPointersToCompare = 3 + numBaseMethods; // IUnknown (3) + base methods

            // Both derived interfaces should have the same base vtable layout
            var baseVTable = new ReadOnlySpan<nint>(baseDetails.ManagedVirtualMethodTable, numPointersToCompare);
            var derived1BaseVTable = new ReadOnlySpan<nint>(derived1Details.ManagedVirtualMethodTable, numPointersToCompare);
            var derived2BaseVTable = new ReadOnlySpan<nint>(derived2Details.ManagedVirtualMethodTable, numPointersToCompare);

            Assert.True(baseVTable.SequenceEqual(derived1BaseVTable),
                "IDerivedExternalBase should have consistent base vtable layout");
            Assert.True(baseVTable.SequenceEqual(derived2BaseVTable),
                "IDerivedExternalBase2 should have consistent base vtable layout");
            Assert.True(derived1BaseVTable.SequenceEqual(derived2BaseVTable),
                "Both derived interfaces should have identical base vtable layouts");
        }

        [Fact]
        public unsafe void CrossAssemblyInheritance_VTableLayoutIsCorrect()
        {
            IIUnknownDerivedDetails baseInterfaceDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy
                .GetIUnknownDerivedDetails(typeof(IExternalBase).TypeHandle);

            IIUnknownDerivedDetails derivedInterfaceDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy
                .GetIUnknownDerivedDetails(typeof(IDerivedExternalBase).TypeHandle);

            var numBaseMethods = typeof(IExternalBase).GetMethods().Length;
            var numPointersToCompare = 3 + numBaseMethods;

            // The first part of the vtable should match between base and derived
            var expectedBaseVTable = new ReadOnlySpan<nint>(baseInterfaceDetails.ManagedVirtualMethodTable, numPointersToCompare);
            var actualDerivedVTable = new ReadOnlySpan<nint>(derivedInterfaceDetails.ManagedVirtualMethodTable, numPointersToCompare);

            Assert.True(expectedBaseVTable.SequenceEqual(actualDerivedVTable),
                "Base interface methods should have the same vtable entries in derived interface");
        }

        [Fact]
        public unsafe void IDerivedFromDerivedExternalDerived_VTableLayoutIsCorrect()
        {
            var baseDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy
                .GetIUnknownDerivedDetails(typeof(IExternalBase).TypeHandle);
            var externalDerivedDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy
                .GetIUnknownDerivedDetails(typeof(IExternalDerived).TypeHandle);
            var derivedFromExternalDerivedDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy
                .GetIUnknownDerivedDetails(typeof(IDerivedFromExternalDerived).TypeHandle);
            var deepDerivedDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy
                .GetIUnknownDerivedDetails(typeof(IDerivedFromDerivedExternalDerived).TypeHandle);

            var baseMethods = typeof(IExternalBase).GetMethods().Length;
            var baseVTableSize = 3 + baseMethods;

            var baseVTable = new ReadOnlySpan<nint>(baseDetails.ManagedVirtualMethodTable, baseVTableSize);
            var externalDerivedBaseVTable = new ReadOnlySpan<nint>(externalDerivedDetails.ManagedVirtualMethodTable, baseVTableSize);
            var derivedFromExternalDerivedBaseVTable = new ReadOnlySpan<nint>(derivedFromExternalDerivedDetails.ManagedVirtualMethodTable, baseVTableSize);
            var deepDerivedBaseVTable = new ReadOnlySpan<nint>(deepDerivedDetails.ManagedVirtualMethodTable, baseVTableSize);

            Assert.True(baseVTable.SequenceEqual(externalDerivedBaseVTable),
                "IExternalDerived should have consistent base vtable layout");
            Assert.True(baseVTable.SequenceEqual(derivedFromExternalDerivedBaseVTable),
                "IDerivedFromExternalDerived should have consistent base vtable layout");
            Assert.True(baseVTable.SequenceEqual(deepDerivedBaseVTable),
                "IDerivedFromDerivedExternalDerived should have consistent base vtable layout");

            var externalDerivedMethods = typeof(IExternalDerived).GetMethods().Length;
            var externalDerivedVTableSize = 3 + externalDerivedMethods;

            var externalDerivedVTable = new ReadOnlySpan<nint>(externalDerivedDetails.ManagedVirtualMethodTable, externalDerivedVTableSize);
            var derivedFromExternalDerivedIntermediateVTable = new ReadOnlySpan<nint>(derivedFromExternalDerivedDetails.ManagedVirtualMethodTable, externalDerivedVTableSize);
            var deepDerivedIntermediateVTable = new ReadOnlySpan<nint>(deepDerivedDetails.ManagedVirtualMethodTable, externalDerivedVTableSize);

            Assert.True(externalDerivedVTable.SequenceEqual(derivedFromExternalDerivedIntermediateVTable),
                "IDerivedFromExternalDerived should have consistent IExternalDerived vtable layout");
            Assert.True(externalDerivedVTable.SequenceEqual(deepDerivedIntermediateVTable),
                "IDerivedFromDerivedExternalDerived should have consistent IExternalDerived vtable layout");

            var derivedFromExternalDerivedMethods = typeof(IDerivedFromExternalDerived).GetMethods().Length;
            var derivedFromExternalDerivedVTableSize = 3 + derivedFromExternalDerivedMethods;
            var derivedFromExternalDerivedVTable = new ReadOnlySpan<nint>(derivedFromExternalDerivedDetails.ManagedVirtualMethodTable, derivedFromExternalDerivedVTableSize);
            var deepDerivedVTable = new ReadOnlySpan<nint>(deepDerivedDetails.ManagedVirtualMethodTable, derivedFromExternalDerivedVTableSize);
            Assert.True(derivedFromExternalDerivedVTable.SequenceEqual(deepDerivedVTable),
                "IDerivedFromDerivedExternalDerived should have consistent IDerivedFromExternalDerived vtable layout");
        }
    }
}
