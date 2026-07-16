// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;
using Microsoft.Diagnostics.DataContractReader.Data.GeneratorTests;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DataGeneratorTests;

/// <summary>
/// End-to-end tests for the IData source generator's emitted code. Test types
/// live in <see cref="TestNative"/> et al; <see cref="TestTarget"/> provides a
/// minimal Target subclass that doesn't depend on the production cdac mocking
/// framework.
/// </summary>
public class DataGeneratorTests
{
    // --- Helpers -----------------------------------------------------

    private const ulong InstanceAddr = 0x1000;

    private static byte[] U32(uint value) => BitConverter.GetBytes(value);
    private static byte[] U64(ulong value) => BitConverter.GetBytes(value);

    private static T Materialize<T>(TestTarget target, ulong address) where T : IData<T>
        => target.ProcessedData.GetOrAdd<T>(new TargetPointer(address));

    // ================================================================
    // Single-source baselines
    // ================================================================

    [Fact]
    public void Native_Baseline_Reads()
    {
        var target = new TestTarget()
            .AddNativeType("TestNative", size: 16,
                ("A", 0),
                ("B", 8))
            .Allocate(InstanceAddr, 16,
                (0, U32(42)),
                (8, U64(0xDEAD_BEEFul)));

        TestNative t = Materialize<TestNative>(target, InstanceAddr);

        Assert.Equal(42u, t.A);
        Assert.Equal((TargetPointer)0xDEAD_BEEFul, t.B);
        Assert.Equal((TargetPointer)InstanceAddr, t.Address);
    }

    [Fact]
    public void Managed_Baseline_Reads()
    {
        var target = new TestTarget()
            .AddManagedType("Test.Managed.Ref", size: 4, ("_a", 0))
            .Allocate(InstanceAddr, 4, (0, U32(0xABCDu)));

        TestManagedRef t = Materialize<TestManagedRef>(target, InstanceAddr);

        Assert.Equal(0xABCDu, t.A);
    }

    // ================================================================
    // Source cascade (native preferred over managed)
    // ================================================================

    [Fact]
    public void Cross_NativeAvailableHasField_UsesNative()
    {
        // Both sources have the same field names; native should win.
        var target = new TestTarget()
            .AddNativeType("TestCross", size: 16, ("A", 0), ("B", 8))
            .AddManagedType("Test.Cross", size: 32, ("A", 16), ("B", 20))
            .Allocate(InstanceAddr, 16,
                (0, U32(0xAAAAu)),
                (8, U64(0xBBBBu)));

        TestCross t = Materialize<TestCross>(target, InstanceAddr);

        Assert.Equal(0xAAAAu, t.A); // native won
    }

    [Fact]
    public void Cross_NativeMissingField_FallsBackToManaged()
    {
        // Native descriptor exists but doesn't contain "A"; managed has it.
        var target = new TestTarget()
            .AddNativeType("TestCross", size: 20, ("B", 12))
            .AddManagedType("Test.Cross", size: 8, ("A", 0), ("B", 4))
            .Allocate(InstanceAddr, 20,
                (0, U32(0xCAFEu)),
                (12, U64(0x4444u)));

        TestCross t = Materialize<TestCross>(target, InstanceAddr);

        Assert.Equal(0xCAFEu, t.A);                   // fell back to managed
        Assert.Equal((TargetPointer)0x4444u, t.B);    // native B kept
    }

    [Fact]
    public void Cross_NativeDescriptorAbsent_UsesManaged()
    {
        // Only managed registered.
        var target = new TestTarget()
            .AddManagedType("Test.Cross", size: 12, ("A", 0), ("B", 4))
            .Allocate(InstanceAddr, 12,
                (0, U32(0xDEEDu)),
                (4, U64(0xFEEDu)));

        TestCross t = Materialize<TestCross>(target, InstanceAddr);

        Assert.Equal(0xDEEDu, t.A);
        Assert.Equal((TargetPointer)0xFEEDu, t.B);
    }

    [Fact]
    public void Cross_BothMissingField_Throws()
    {
        // Neither source has "A".
        var target = new TestTarget()
            .AddNativeType("TestCross", size: 16, ("B", 8))
            .AddManagedType("Test.Cross", size: 8, ("B", 0))
            .Allocate(InstanceAddr, 32);

        Assert.Throws<InvalidOperationException>(
            () => Materialize<TestCross>(target, InstanceAddr));
    }

    // ================================================================
    // Name fallback (multiple candidate names tried in order)
    // ================================================================

    [Fact]
    public void Native_AliasFallback()
    {
        // Primary "A" missing; alias "A_old" present.
        var target = new TestTarget()
            .AddNativeType("TestNativeAlias", size: 4, ("A_old", 0))
            .Allocate(InstanceAddr, 4, (0, U32(99u)));

        TestNativeAlias t = Materialize<TestNativeAlias>(target, InstanceAddr);

        Assert.Equal(99u, t.A);
    }

    [Fact]
    public void Managed_AliasFallback()
    {
        // Primary "_a" missing; alias "_a_old" present.
        var target = new TestTarget()
            .AddManagedType("Test.ManagedAlias", size: 4, ("_a_old", 0))
            .Allocate(InstanceAddr, 4, (0, U32(123u)));

        TestManagedAlias t = Materialize<TestManagedAlias>(target, InstanceAddr);

        Assert.Equal(123u, t.A);
    }

    [Fact]
    public void Cross_NativeAliasOnly()
    {
        // Native has only the alias "A_old"; managed not registered.
        var target = new TestTarget()
            .AddNativeType("TestCrossAlias", size: 4, ("A_old", 0))
            .Allocate(InstanceAddr, 4, (0, U32(0x1111u)));

        TestCrossAlias t = Materialize<TestCrossAlias>(target, InstanceAddr);

        Assert.Equal(0x1111u, t.A);
    }

    [Fact]
    public void Cross_ManagedAliasOnly()
    {
        // No native descriptor; managed has only the alias "_a_old".
        var target = new TestTarget()
            .AddManagedType("Test.CrossAlias", size: 4, ("_a_old", 0))
            .Allocate(InstanceAddr, 4, (0, U32(0x2222u)));

        TestCrossAlias t = Materialize<TestCrossAlias>(target, InstanceAddr);

        Assert.Equal(0x2222u, t.A);
    }

    // ================================================================
    // Writable
    // ================================================================

    [Fact]
    public void WriteNative_RoundTrip()
    {
        var target = new TestTarget()
            .AddNativeType("TestWriteNative", size: 4, ("Flags", 0))
            .Allocate(InstanceAddr, 4, (0, U32(1u)));

        TestWriteNative t = Materialize<TestWriteNative>(target, InstanceAddr);
        Assert.Equal(1u, t.Flags);

        t.WriteFlags(0xFEED_FACEu);

        Assert.Equal(0xFEED_FACEu, t.Flags);
        Assert.Equal(0xFEED_FACEu, BitConverter.ToUInt32(target.Bytes(InstanceAddr)));
    }

    [Fact]
    public void WriteManaged_RoundTrip()
    {
        var target = new TestTarget()
            .AddManagedType("Test.WriteManaged", size: 4, ("_flags", 0))
            .Allocate(InstanceAddr, 4, (0, U32(1u)));

        TestWriteManaged t = Materialize<TestWriteManaged>(target, InstanceAddr);

        t.WriteFlags(0xABCDu);

        Assert.Equal(0xABCDu, t.Flags);
        Assert.Equal(0xABCDu, BitConverter.ToUInt32(target.Bytes(InstanceAddr)));
    }

    [Fact]
    public void WriteCross_NativeResolves_WritesNative()
    {
        var target = new TestTarget()
            .AddNativeType("TestWriteCross", size: 8, ("Flags", 0))
            .AddManagedType("Test.WriteCross", size: 4, ("Flags", 4))
            .Allocate(InstanceAddr, 8, (0, U32(0u)), (4, U32(0u)));

        TestWriteCross t = Materialize<TestWriteCross>(target, InstanceAddr);
        t.WriteFlags(0x33u);

        Assert.Equal(0x33u, BitConverter.ToUInt32(target.Bytes(InstanceAddr).Slice(0, 4)));
        Assert.Equal(0u, BitConverter.ToUInt32(target.Bytes(InstanceAddr).Slice(4, 4)));
    }

    [Fact]
    public void WriteCross_ManagedResolves_WritesManagedOffset()
    {
        // Native descriptor lacks "Flags"; write should fall back to managed.
        var target = new TestTarget()
            .AddNativeType("TestWriteCross", size: 8)
            .AddManagedType("Test.WriteCross", size: 4, ("Flags", 4))
            .Allocate(InstanceAddr, 8, (4, U32(0u)));

        TestWriteCross t = Materialize<TestWriteCross>(target, InstanceAddr);
        t.WriteFlags(0x44u);

        Assert.Equal(0x44u, BitConverter.ToUInt32(target.Bytes(InstanceAddr).Slice(4, 4)));
    }

    // ================================================================
    // Optional T?
    // ================================================================

    [Fact]
    public void Optional_FieldPresent_ReturnsValue()
    {
        var target = new TestTarget()
            .AddNativeType("TestOptional", size: 8, ("Required", 0), ("Optional", 4))
            .Allocate(InstanceAddr, 8, (0, U32(1u)), (4, U32(2u)));

        TestOptional t = Materialize<TestOptional>(target, InstanceAddr);

        Assert.Equal(1u, t.Required);
        Assert.Equal(2u, t.Optional);
    }

    [Fact]
    public void Optional_FieldAbsent_ReturnsNull()
    {
        var target = new TestTarget()
            .AddNativeType("TestOptional", size: 4, ("Required", 0))
            .Allocate(InstanceAddr, 4, (0, U32(5u)));

        TestOptional t = Materialize<TestOptional>(target, InstanceAddr);

        Assert.Equal(5u, t.Required);
        Assert.Null(t.Optional);
    }

    // ================================================================
    // FieldAddress under fallback
    // ================================================================

    [Fact]
    public void FieldAddress_NativeResolves()
    {
        var target = new TestTarget()
            .AddNativeType("TestFieldAddr", size: 16, ("A", 0), ("Anchor", 12))
            .Allocate(InstanceAddr, 16, (0, U32(1u)));

        TestFieldAddr t = Materialize<TestFieldAddr>(target, InstanceAddr);

        Assert.Equal((TargetPointer)(InstanceAddr + 12), t.AnchorAddress);
    }

    [Fact]
    public void FieldAddress_ManagedResolves()
    {
        var target = new TestTarget()
            .AddManagedType("Test.FieldAddr", size: 16, ("A", 0), ("Anchor", 12))
            .Allocate(InstanceAddr, 16, (0, U32(1u)));

        TestFieldAddr t = Materialize<TestFieldAddr>(target, InstanceAddr);

        Assert.Equal((TargetPointer)(InstanceAddr + 12), t.AnchorAddress);
    }

    [Fact]
    public void FieldAddress_OptionalPresent()
    {
        var target = new TestTarget()
            .AddNativeType("TestOptionalFieldAddr", size: 8, ("Required", 0), ("OptionalAddress", 4))
            .Allocate(InstanceAddr, 8, (0, U32(7u)));

        TestOptionalFieldAddr t = Materialize<TestOptionalFieldAddr>(target, InstanceAddr);

        Assert.Equal(7u, t.Required);
        Assert.Equal((TargetPointer)(InstanceAddr + 4), t.OptionalAddress);
    }

    [Fact]
    public void FieldAddress_OptionalAbsent()
    {
        var target = new TestTarget()
            .AddNativeType("TestOptionalFieldAddr", size: 4, ("Required", 0))
            .Allocate(InstanceAddr, 4, (0, U32(9u)));

        TestOptionalFieldAddr t = Materialize<TestOptionalFieldAddr>(target, InstanceAddr);

        Assert.Equal(9u, t.Required);
        Assert.Null(t.OptionalAddress);
    }

    // ================================================================
    // UsePropertyName = false
    // ================================================================

    [Fact]
    public void UsePropertyName_False_OnlyExplicitNamesUsed()
    {
        var target = new TestTarget()
            .AddNativeType("TestNoPropertyName", size: 4, ("m_flags", 0))
            .Allocate(InstanceAddr, 4, (0, U32(0xBEEFu)));

        TestNoPropertyName t = Materialize<TestNoPropertyName>(target, InstanceAddr);

        Assert.Equal(0xBEEFu, t.Flags);
    }

    [Fact]
    public void UsePropertyName_False_PropertyNameNotUsed()
    {
        var target = new TestTarget()
            .AddNativeType("TestNoPropertyName", size: 4, ("Flags", 0))
            .Allocate(InstanceAddr, 4, (0, U32(0xBEEFu)));

        Assert.Throws<InvalidOperationException>(
            () => Materialize<TestNoPropertyName>(target, InstanceAddr));
    }

    // ================================================================
    // DataPointer (IData<T> with Pointer = true)
    // ================================================================

    [Fact]
    public void DataPointer_ReadsPointerThenMaterializesIData()
    {
        const ulong innerAddr = 0x2000;
        var target = new TestTarget()
            .AddNativeType("TestDataPointer", size: 8, ("Inner", 0))
            .AddNativeType("TestNative", size: 16, ("A", 0), ("B", 8))
            .Allocate(InstanceAddr, 8, (0, U64(innerAddr)))
            .Allocate(innerAddr, 16, (0, U32(42u)), (8, U64(0xCAFEul)));

        TestDataPointer t = Materialize<TestDataPointer>(target, InstanceAddr);

        Assert.NotNull(t.Inner);
        Assert.Equal(42u, t.Inner.A);
        Assert.Equal((TargetPointer)0xCAFEul, t.Inner.B);
    }

    // ================================================================
    // Static field accessors (native globals + managed fallback)
    // ================================================================

    [Fact]
    public void StaticAddress_NativeGlobal_Resolves()
    {
        const ulong slotAddr = 0x5000;
        var target = new TestTarget()
            .AddNativeType("TestStaticAddr", size: 0)
            .AddGlobal("TestStaticAddr.s_instance", slotAddr);

        TargetPointer result = TestStaticAddr.Instance(target);

        Assert.Equal((TargetPointer)slotAddr, result);
    }

    [Fact]
    public void StaticAddress_ManagedFallback_Resolves()
    {
        const ulong slotAddr = 0x6000;
        var target = new TestTarget()
            .AddManagedStaticField("Test.StaticAddr", "s_instance", slotAddr);

        TargetPointer result = TestStaticAddr.Instance(target);

        Assert.Equal((TargetPointer)slotAddr, result);
    }

    [Fact]
    public void StaticAddress_NativeGlobalTakesPrecedence()
    {
        const ulong nativeAddr = 0x7000;
        const ulong managedAddr = 0x8000;
        var target = new TestTarget()
            .AddNativeType("TestStaticAddr", size: 0)
            .AddGlobal("TestStaticAddr.s_instance", nativeAddr)
            .AddManagedStaticField("Test.StaticAddr", "s_instance", managedAddr);

        TargetPointer result = TestStaticAddr.Instance(target);

        Assert.Equal((TargetPointer)nativeAddr, result);
    }

    [Fact]
    public void StaticReference_NativeGlobal_DereferencesSlot()
    {
        const ulong slotAddr = 0x9000;
        const ulong objAddr = 0xBEEF;
        var target = new TestTarget()
            .AddNativeType("TestStaticRef", size: 0)
            .AddGlobal("TestStaticRef.s_cache", slotAddr)
            .Allocate(slotAddr, 8, (0, U64(objAddr)));

        TargetPointer? result = TestStaticRef.Cache(target);

        Assert.Equal((TargetPointer)objAddr, result);
    }

    [Fact]
    public void StaticReference_NoGlobalOrManaged_ReturnsNull()
    {
        var target = new TestTarget()
            .AddNativeType("TestStaticRef", size: 0);

        TargetPointer? result = TestStaticRef.Cache(target);

        Assert.Null(result);
    }

}
