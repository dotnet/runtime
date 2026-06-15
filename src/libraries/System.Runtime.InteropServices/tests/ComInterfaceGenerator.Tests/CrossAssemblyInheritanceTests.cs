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

        [GeneratedComClass]
        [Guid("e0c6b35f-1234-4567-8901-123456789ac0")]
        internal partial class DerivedExternalIndexersAndPropertiesImpl : IDerivedExternalIndexersAndProperties
        {
            private int _setSentinel;
            private int _value = 11;
            private int _writeOnlyCounter;

            public int this[int i]
            {
                get => i * 10;
                set => _setSentinel = value;
            }

            public int this[int i, int j]
            {
                get => (i * 100) + j;
                set => _setSentinel = value;
            }

            public int this[long l] => unchecked((int)(l + 1));

            public int this[short s]
            {
                set => _setSentinel = value;
            }

            public int Value
            {
                get => _value;
                set => _value = value;
            }

            public string Name => "DerivedExternalIndexersAndProperties";

            public int WriteOnlyCounter
            {
                set => _writeOnlyCounter = value;
            }

            public int Marker => 7;

            internal int LastSetSentinel => _setSentinel;
            internal int LastWriteOnlyCounter => _writeOnlyCounter;
        }

        [Fact]
        public void IDerivedExternalIndexersAndProperties_AccessorsDispatchAcrossBothSurfaces()
        {
            var implementation = new DerivedExternalIndexersAndPropertiesImpl();
            var comWrappers = new StrategyBasedComWrappers();
            var nativeObj = comWrappers.GetOrCreateComInterfaceForObject(implementation, CreateComInterfaceFlags.None);
            var managedObj = comWrappers.GetOrCreateObjectForComInstance(nativeObj, CreateObjectFlags.None);

            var externalSurface = (IExternalIndexersAndProperties)managedObj;

            // Overloaded indexers dispatch by parameter signature even though every accessor
            // shares the get_Item / set_Item IL name.
            Assert.Equal(50, externalSurface[5]);
            Assert.Equal(305, externalSurface[3, 5]);
            Assert.Equal(101, externalSurface[100L]);

            externalSurface[5] = 1;
            externalSurface[3, 5] = 2;
            externalSurface[(short)7] = 3;

            // Regular property accessors on the same external interface are reachable through
            // the same RCW dispatch and round-trip values across the COM boundary.
            Assert.Equal(11, externalSurface.Value);
            externalSurface.Value = 22;
            Assert.Equal(22, externalSurface.Value);

            Assert.Equal("DerivedExternalIndexersAndProperties", externalSurface.Name);

            externalSurface.WriteOnlyCounter = 99;

            // The derived interface still sees both the inherited surfaces and its own Marker.
            var derived = (IDerivedExternalIndexersAndProperties)managedObj;
            Assert.Equal(7, derived.Marker);
            Assert.Equal(50, derived[5]);
            Assert.Equal(305, derived[3, 5]);
            Assert.Equal(101, derived[100L]);
            Assert.Equal(22, derived.Value);
            Assert.Equal("DerivedExternalIndexersAndProperties", derived.Name);
        }

        [Fact]
        public unsafe void IDerivedExternalIndexersAndProperties_VTableLayoutExtendsBase()
        {
            IIUnknownDerivedDetails baseDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy
                .GetIUnknownDerivedDetails(typeof(IExternalIndexersAndProperties).TypeHandle);
            IIUnknownDerivedDetails derivedDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy
                .GetIUnknownDerivedDetails(typeof(IDerivedExternalIndexersAndProperties).TypeHandle);

            // IExternalIndexersAndProperties contributes one slot per accessor in declaration order:
            //   indexer get/set [int]              (2)
            //   indexer get/set [int, int]         (2)
            //   indexer get [long]                 (1)
            //   indexer set [short]                (1)
            //   property get/set Value             (2)
            //   property get Name                  (1)
            //   property set WriteOnlyCounter      (1) = 10 slots
            // plus IUnknown's 3.
            const int BaseVTableSize = 3 + 10;

            var baseVTable = new ReadOnlySpan<nint>(baseDetails.ManagedVirtualMethodTable, BaseVTableSize);
            var derivedBaseVTable = new ReadOnlySpan<nint>(derivedDetails.ManagedVirtualMethodTable, BaseVTableSize);

            Assert.True(baseVTable.SequenceEqual(derivedBaseVTable),
                "IDerivedExternalIndexersAndProperties should preserve the IExternalIndexersAndProperties vtable prefix even though all indexer accessors share the get_Item/set_Item name.");
        }

        [GeneratedComClass]
        [Guid("e0c6b35f-1234-4567-8901-123456789ac1")]
        internal partial class DerivedFromExternalSameNameImpl
            : IDerivedFromExternalSameNameA, IDerivedFromExternalSameNameB
        {
            double IExternalSameNameA.MyMethod() => 1.5;
            int IExternalSameNameB.MyMethod() => 33;
        }

        [Fact]
        public void IDerivedFromExternalSameName_CanCallBothDisjointBases()
        {
            var implementation = new DerivedFromExternalSameNameImpl();
            var comWrappers = new StrategyBasedComWrappers();
            var nativeObj = comWrappers.GetOrCreateComInterfaceForObject(implementation, CreateComInterfaceFlags.None);
            var managedObj = comWrappers.GetOrCreateObjectForComInstance(nativeObj, CreateObjectFlags.None);

            var a = (IExternalSameNameA)managedObj;
            var b = (IExternalSameNameB)managedObj;

            Assert.Equal(1.5, a.MyMethod());
            Assert.Equal(33, b.MyMethod());
        }

        [GeneratedComClass]
        [Guid("e0c6b35f-1234-4567-8901-123456789ac2")]
        internal partial class DerivedFromExternalSameNameAOnlyImpl
            : IDerivedFromExternalSameNameA
        {
            public double MyMethod() => 2.5;
        }

        [GeneratedComClass]
        [Guid("e0c6b35f-1234-4567-8901-123456789ac3")]
        internal partial class DerivedFromExternalSameNameBOnlyImpl
            : IDerivedFromExternalSameNameB
        {
            public int MyMethod() => 77;
        }

        [Fact]
        public void IDerivedFromExternalSameNameA_CanCallStandalone()
        {
            var implementation = new DerivedFromExternalSameNameAOnlyImpl();
            var comWrappers = new StrategyBasedComWrappers();
            var nativeObj = comWrappers.GetOrCreateComInterfaceForObject(implementation, CreateComInterfaceFlags.None);
            var managedObj = comWrappers.GetOrCreateObjectForComInstance(nativeObj, CreateObjectFlags.None);

            var a = (IExternalSameNameA)managedObj;

            Assert.Equal(2.5, a.MyMethod());
        }

        [Fact]
        public void IDerivedFromExternalSameNameB_CanCallStandalone()
        {
            var implementation = new DerivedFromExternalSameNameBOnlyImpl();
            var comWrappers = new StrategyBasedComWrappers();
            var nativeObj = comWrappers.GetOrCreateComInterfaceForObject(implementation, CreateComInterfaceFlags.None);
            var managedObj = comWrappers.GetOrCreateObjectForComInstance(nativeObj, CreateObjectFlags.None);

            var b = (IExternalSameNameB)managedObj;

            Assert.Equal(77, b.MyMethod());
        }
    }
}
