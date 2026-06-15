// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    [Collection(nameof(TrackedIntMarshaller))]
    public unsafe partial class IIndexersTests
    {
        [LibraryImport(NativeExportsNE.NativeExportsNE_Binary, EntryPoint = "new_indexers")]
        public static partial void* NewIndexers();

        // -------------------------------------------------------------------------
        // Round-trip an in-process [GeneratedComClass] through CCW + RCW so each
        // indexer accessor is invoked through the generator's marshalling pipeline
        // in both directions.
        // -------------------------------------------------------------------------

        private static (Indexers Impl, IIndexers Rcw) CreateRcwAroundCcw()
        {
            var impl = new Indexers();
            var cw = new StrategyBasedComWrappers();
            var comPtr = cw.GetOrCreateComInterfaceForObject(impl, CreateComInterfaceFlags.None);
            try
            {
                var comObject = cw.GetOrCreateObjectForComInstance(comPtr, CreateObjectFlags.None);
                return (impl, (IIndexers)comObject);
            }
            finally
            {
                Marshal.Release(comPtr);
            }
        }

        [Theory]
        [InlineData(0, 100)]
        [InlineData(5, -3)]
        [InlineData(-1, int.MaxValue / 2)]
        public void SingleParamIndexer_RoundTrips(int index, int value)
        {
            (Indexers impl, IIndexers rcw) = CreateRcwAroundCcw();

            rcw[index] = value;
            int observed = rcw[index];

            Assert.Equal(value, observed);
            // The impl computes value-(i), stores value-(i), then the getter returns value-(i)+i = value.
            Assert.Equal(value, impl[index]);
        }

        [Theory]
        [InlineData(0, 0, 11)]
        [InlineData(2, 7, 999)]
        [InlineData(-3, 4, -123)]
        public void TwoParamIndexer_RoundTrips(int i, int j, int value)
        {
            (Indexers impl, IIndexers rcw) = CreateRcwAroundCcw();

            rcw[i, j] = value;
            int observed = rcw[i, j];

            Assert.Equal(value, observed);
            Assert.Equal(value, impl[i, j]);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(11L)]
        [InlineData(-3L)]
        public void ReadOnlyIndexer_ReturnsComputedValue(long l)
        {
            (_, IIndexers rcw) = CreateRcwAroundCcw();

            Assert.Equal(unchecked((int)(l * 7)), rcw[l]);
        }

        [Theory]
        [InlineData((short)0, 0)]
        [InlineData((short)5, 13)]
        [InlineData((short)-7, -200)]
        public void WriteOnlyIndexer_PropagatesToImpl(short s, int value)
        {
            (Indexers impl, IIndexers rcw) = CreateRcwAroundCcw();

            rcw[s] = value;

            Assert.Equal(value + s, impl.WriteOnlyShortSink);
        }

        [Theory]
        [InlineData("alpha", "first")]
        [InlineData("", "empty-key")]
        [InlineData("non-ascii-\u00e9\u00f1\u4e2d", "non-ascii-value-\u00fc\u30a2\u00df")]
        [InlineData("key", "")]
        public void StringKeyedIndexer_RoundTrips(string key, string value)
        {
            (Indexers impl, IIndexers rcw) = CreateRcwAroundCcw();

            rcw[key] = value;
            string observed = rcw[key];

            Assert.Equal(value, observed);
            Assert.Equal(value, impl[key]);
        }

        // -------------------------------------------------------------------------
        // RCW path only, driven by a hand-rolled native vtable (`new_indexers` from
        // NativeExports/ComInterfaceGenerator/Indexers.cs). This proves the generated
        // RCW marshals correctly against external native code, not just against our
        // own [GeneratedComClass] CCW.
        // -------------------------------------------------------------------------

        private static IIndexers CreateRcwOverNativeShim()
        {
            var cw = new StrategyBasedComWrappers();
            nint nativePtr = (nint)NewIndexers();
            try
            {
                return (IIndexers)cw.GetOrCreateObjectForComInstance(nativePtr, CreateObjectFlags.None);
            }
            finally
            {
                Marshal.Release(nativePtr);
            }
        }

        [Theory]
        [InlineData(0, 100)]
        [InlineData(5, -3)]
        [InlineData(-1, int.MaxValue / 2)]
        public void NativeShim_SingleParamIndexer_RoundTrips(int index, int value)
        {
            IIndexers rcw = CreateRcwOverNativeShim();

            rcw[index] = value;
            Assert.Equal(value, rcw[index]);
        }

        [Theory]
        [InlineData(0, 0, 11)]
        [InlineData(2, 7, 999)]
        [InlineData(-3, 4, -123)]
        public void NativeShim_TwoParamIndexer_RoundTrips(int i, int j, int value)
        {
            IIndexers rcw = CreateRcwOverNativeShim();

            rcw[i, j] = value;
            Assert.Equal(value, rcw[i, j]);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(11L)]
        [InlineData(-3L)]
        public void NativeShim_ReadOnlyIndexer_ReturnsShimValue(long l)
        {
            IIndexers rcw = CreateRcwOverNativeShim();

            Assert.Equal(unchecked((int)(l * 7)), rcw[l]);
        }

        [Theory]
        [InlineData((short)0, 0)]
        [InlineData((short)5, 13)]
        [InlineData((short)-7, -200)]
        public void NativeShim_WriteOnlyIndexer_DoesNotThrow(short s, int value)
        {
            IIndexers rcw = CreateRcwOverNativeShim();

            rcw[s] = value;
        }

        [Theory]
        [InlineData("alpha", "first")]
        [InlineData("", "empty-key")]
        [InlineData("non-ascii-\u00e9\u00f1\u4e2d", "non-ascii-value-\u00fc\u30a2\u00df")]
        [InlineData("key", "")]
        public void NativeShim_StringKeyedIndexer_RoundTrips(string key, string value)
        {
            IIndexers rcw = CreateRcwOverNativeShim();

            rcw[key] = value;
            Assert.Equal(value, rcw[key]);
        }

        // -------------------------------------------------------------------------
        // [IndexerName] propagation. Verifies the indexer accessor names baked into
        // the metadata are get_Element / set_Element (not the default get_Item / set_Item),
        // both on the user's interface and on the RCW returned from CCW marshalling.
        // -------------------------------------------------------------------------

        [Fact]
        public void RenamedIndexer_ILAccessorNames_MatchIndexerNameAttribute()
        {
            MethodInfo[] methods = typeof(IRenamedIndexer).GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.Contains(methods, m => m.Name == "get_Element");
            Assert.Contains(methods, m => m.Name == "set_Element");
            Assert.DoesNotContain(methods, m => m.Name == "get_Item");
            Assert.DoesNotContain(methods, m => m.Name == "set_Item");
        }

        [Theory]
        [InlineData(0, 100)]
        [InlineData(3, -50)]
        public void RenamedIndexer_RoundTrips(int index, int value)
        {
            var impl = new RenamedIndexer();
            var cw = new StrategyBasedComWrappers();
            nint comPtr = cw.GetOrCreateComInterfaceForObject(impl, CreateComInterfaceFlags.None);
            IRenamedIndexer rcw;
            try
            {
                rcw = (IRenamedIndexer)cw.GetOrCreateObjectForComInstance(comPtr, CreateObjectFlags.None);
            }
            finally
            {
                Marshal.Release(comPtr);
            }

            rcw[index] = value;
            Assert.Equal(value, rcw[index]);
        }

        // -------------------------------------------------------------------------
        // Property-level [MarshalUsing] on an indexer. Verifies that an attribute
        // placed on the indexer declaration applies only to the value surface
        // (getter return / setter value parameter) and is NOT propagated onto
        // index parameters or the setter's `void` return. The test interface
        // declares `[MarshalUsing(typeof(TrackedIntMarshaller))] int this[int]`
        // -- the index and value share the same `int` type, so a buggy generator
        // that propagates the attribute to every parameter would invoke the
        // marshaller on each index pass as well. Exact-count assertions catch
        // that regression.
        // -------------------------------------------------------------------------

        private static (IndexerMarshalling Impl, IIndexerMarshalling Rcw) CreateIndexerMarshallingRcwAroundCcw()
        {
            var impl = new IndexerMarshalling();
            var cw = new StrategyBasedComWrappers();
            nint comPtr = cw.GetOrCreateComInterfaceForObject(impl, CreateComInterfaceFlags.None);
            try
            {
                var comObject = cw.GetOrCreateObjectForComInstance(comPtr, CreateObjectFlags.None);
                return (impl, (IIndexerMarshalling)comObject);
            }
            finally
            {
                Marshal.Release(comPtr);
            }
        }

        [Fact]
        public void IndexerLevelMarshalling_Bare_InvokesMarshallerOnValueSurfaceOnly()
        {
            TrackedIntMarshaller.Reset();

            (_, IIndexerMarshalling rcw) = CreateIndexerMarshallingRcwAroundCcw();

            rcw[3] = 100;
            int observed = rcw[3];

            Assert.Equal(100, observed);

            // Each accessor traverses RCW (managed-to-unmanaged on the call out) and CCW
            // (unmanaged-to-managed on the call in) for inputs, and the reverse for outputs.
            // Value surface invocations across one set + one get:
            //   setter value: 1 RCW m2u + 1 CCW u2m
            //   getter return: 1 CCW m2u + 1 RCW u2m
            // Total: m2u == 2, u2m == 2.
            // The index parameter must NOT receive the marshaller. If it did, each accessor
            // would add an extra m2u (RCW outbound for the index) and u2m (CCW inbound for the
            // index), yielding 4 / 4 instead of 2 / 2.
            Assert.Equal(2, TrackedIntMarshaller.ManagedToUnmanagedCount);
            Assert.Equal(2, TrackedIntMarshaller.UnmanagedToManagedCount);
        }

        // -------------------------------------------------------------------------
        // Derived-interface shadow propagation. A derived [GeneratedComInterface]
        // that re-declares an inherited indexer with `new` emits a shadow indexer
        // forwarder on the derived interface. We assert that the shadow exists with
        // the correct shape via reflection.
        // -------------------------------------------------------------------------

        [Fact]
        public void DerivedShadowIndexer_DeclaresDefaultNamedIndexer()
        {
            PropertyInfo? shadow = typeof(IDerivedIndexers).GetProperty(
                "Item",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                binder: null,
                returnType: null,
                types: [typeof(int)],
                modifiers: null);
            Assert.NotNull(shadow);

            Assert.NotNull(shadow!.GetMethod);
            Assert.NotNull(shadow.SetMethod);
            // Default IndexerName means the IL accessors are get_Item/set_Item.
            Assert.Equal("get_Item", shadow.GetMethod!.Name);
            Assert.Equal("set_Item", shadow.SetMethod!.Name);
        }

        [Fact]
        public void DerivedShadowIndexer_PropagatesIndexerNameAttribute()
        {
            PropertyInfo? shadow = typeof(IRenamedDerivedIndexers).GetProperty(
                "Foo",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                binder: null,
                returnType: null,
                types: [typeof(long)],
                modifiers: null);
            Assert.NotNull(shadow);

            Assert.NotNull(shadow!.GetMethod);
            Assert.NotNull(shadow.SetMethod);
            // The base interface declares [IndexerName("Foo")]; the shadow must propagate it so
            // that the IL accessor names remain get_Foo / set_Foo on the derived interface.
            Assert.Equal("get_Foo", shadow.GetMethod!.Name);
            Assert.Equal("set_Foo", shadow.SetMethod!.Name);
        }
    }
}
