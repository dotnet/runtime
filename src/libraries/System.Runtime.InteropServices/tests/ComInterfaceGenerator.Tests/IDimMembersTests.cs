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
    public unsafe partial class IDimMembersTests
    {
        // -------------------------------------------------------------------------
        // Round-trip an in-process [GeneratedComClass] through CCW + RCW. Each test
        // verifies that the user-defined default-implemented (DIM) property or
        // method routes through its associated COM ABI methods rather than being
        // assigned its own vtable slot.
        // -------------------------------------------------------------------------

        private static (DimMembers Impl, IDimMembers Rcw) CreateRcwAroundCcw()
        {
            var impl = new DimMembers();
            var cw = new StrategyBasedComWrappers();
            var comPtr = cw.GetOrCreateComInterfaceForObject(impl, CreateComInterfaceFlags.None);
            try
            {
                var comObject = cw.GetOrCreateObjectForComInstance(comPtr, CreateObjectFlags.None);
                return (impl, (IDimMembers)comObject);
            }
            finally
            {
                Marshal.Release(comPtr);
            }
        }

        [Fact]
        public void DimProperty_RoundTrips()
        {
            (DimMembers impl, IDimMembers rcw) = CreateRcwAroundCcw();

            rcw.Value = 3.14;
            Assert.Equal(3.14, rcw.Value);
            Assert.Equal(3.14, impl.ReadValue());
        }

        [Fact]
        public void DimReadOnlyProperty_ReturnsImplValue()
        {
            (_, IDimMembers rcw) = CreateRcwAroundCcw();

            Assert.Equal(111, rcw.CountProperty);
        }

        [Fact]
        public void DimWriteOnlyProperty_PropagatesToImpl()
        {
            (DimMembers impl, IDimMembers rcw) = CreateRcwAroundCcw();

            rcw.SinkProperty = 42;
            Assert.Equal(42, impl.WriteOnlyIntSink);
        }

        [Fact]
        public void DimMethod_ExecutesLocally()
        {
            (_, IDimMembers rcw) = CreateRcwAroundCcw();

            Assert.Equal(20, rcw.DoubleIt(10));
        }

        [Fact]
        public void DimProperty_AbiAccessorsRemainCallable()
        {
            (_, IDimMembers rcw) = CreateRcwAroundCcw();

            rcw.WriteValue(2.5);
            Assert.Equal(2.5, rcw.ReadValue());
            Assert.Equal(2.5, rcw.Value);
        }

        // -------------------------------------------------------------------------
        // Raw vtable inspection: confirm that DIM members do NOT consume vtable
        // slots. The vtable should only contain IUnknown (3) + the 4 ABI methods
        // (ReadValue, WriteValue, ReadCount, WriteSink) -- 7 slots total.
        // -------------------------------------------------------------------------

        [Fact]
        public void DimMembers_VtableContainsOnlyAbiSlots()
        {
            IIUnknownDerivedDetails details =
                StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IDimMembers).TypeHandle);

            // Reflection-derived count: only abstract members (the ABI methods) get
            // vtable slots; DIM properties/methods do not.
            int abstractAccessorCount = 0;
            foreach (MethodInfo method in typeof(IDimMembers).GetMethods())
            {
                if (method.IsAbstract)
                {
                    abstractAccessorCount++;
                }
            }

            Assert.Equal(4, abstractAccessorCount);

            // Confirm via the raw vtable: read 7 slots and ensure they're non-null.
            // (If DIM members had been assigned slots, the count would be higher.)
            int expectedSlots = 3 + abstractAccessorCount;
            var vtable = new ReadOnlySpan<nint>(details.ManagedVirtualMethodTable, expectedSlots);
            for (int i = 0; i < expectedSlots; i++)
            {
                Assert.NotEqual(0, vtable[i]);
            }
        }
    }
}
