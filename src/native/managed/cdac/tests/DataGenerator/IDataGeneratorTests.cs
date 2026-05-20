// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Data;
using Microsoft.Diagnostics.DataContractReader.Data.GeneratorTests;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DataGeneratorTests;

/// <summary>
/// End-to-end tests for the IData source generator's emitted code. Test types
/// live in <see cref="TestNative"/> et al; <see cref="TestTarget"/> provides a
/// minimal Target subclass that doesn't depend on the production cdac mocking
/// framework.
/// </summary>
public class IDataGeneratorTests
{
    // --- Helpers -----------------------------------------------------

    /// <summary>Object.Size for tests that need the managed reference-type header offset.</summary>
    private const uint HeaderSize = 8;

    private const ulong InstanceAddr = 0x1000;

    /// <summary>Build a target with an "Object" TypeInfo so cross-source ref-type managed reads pick up the header offset.</summary>
    private static TestTarget NewTargetWithObject()
        => new TestTarget()
            .AddNativeType("Object", size: HeaderSize);

    private static byte[] U32(uint value) => BitConverter.GetBytes(value);
    private static byte[] U64(ulong value) => BitConverter.GetBytes(value);

    /// <summary>Materialize an IData via its generated ctor through the static abstract.</summary>
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
    public void Managed_Ref_AppliesObjectSizeOffset()
    {
        // Managed reference types: field reads at address + Object.Size + fieldOffset.
        var target = NewTargetWithObject()
            .AddManagedType("Test.Managed.Ref", size: 4, ("_a", 0))
            .Allocate(InstanceAddr, 16,
                ((int)HeaderSize + 0, U32(0xABCDu)));

        TestManagedRef t = Materialize<TestManagedRef>(target, InstanceAddr);

        Assert.Equal(0xABCDu, t.A);
    }

    [Fact]
    public void Managed_Val_NoObjectSizeOffset()
    {
        // Value types have no object header: reads at address + fieldOffset.
        var target = new TestTarget()
            .AddManagedType("Test.Managed.Val", size: 4, ("A", 0))
            .Allocate(InstanceAddr, 4, (0, U32(7u)));

        TestManagedVal t = Materialize<TestManagedVal>(target, InstanceAddr);

        Assert.Equal(7u, t.A);
    }

    // ================================================================
    // Cross-source cascade
    // ================================================================

    [Fact]
    public void Cross_NativeAvailableHasField_UsesNative()
    {
        var target = NewTargetWithObject()
            .AddNativeType("TestCross", size: 16, ("A", 0), ("B", 8))
            .AddManagedType("Test.Cross", size: 12, ("_a", 0), ("_b", 4))
            .Allocate(InstanceAddr, 16,
                (0, U32(0xAAAAu)),               // native A
                ((int)HeaderSize + 0, U32(0xBBBBu))); // would be managed _a if cascade went wrong

        TestCross t = Materialize<TestCross>(target, InstanceAddr);

        Assert.Equal(0xAAAAu, t.A); // native won
    }

    [Fact]
    public void Cross_NativeMissingField_FallsBackToManaged()
    {
        // Native descriptor exists but doesn't contain "A"; managed has "_a".
        // Offsets chosen so native and managed describe the SAME bytes (the
        // realistic relationship for a managed ref type with header):
        //   native B at offset 12   ==  managed _b at offset 4 (after Header=8)
        //   native (no A)               managed _a at offset 0 (after Header=8) == address+8
        var target = NewTargetWithObject()
            .AddNativeType("TestCross", size: 20, ("B", 12))
            .AddManagedType("Test.Cross", size: 8, ("_a", 0), ("_b", 4))
            .Allocate(InstanceAddr, 20,
                (8, U32(0xCAFEu)),    // bytes 8-11   = managed _a (addr+Header+0)
                (12, U64(0x4444u)));  // bytes 12-19  = native B (addr+12)

        TestCross t = Materialize<TestCross>(target, InstanceAddr);

        Assert.Equal(0xCAFEu, t.A);                   // fell back to managed
        Assert.Equal((TargetPointer)0x4444u, t.B);    // native B kept
    }

    [Fact]
    public void Cross_NativeDescriptorAbsent_UsesManaged()
    {
        // Only managed registered.
        var target = NewTargetWithObject()
            .AddManagedType("Test.Cross", size: 12, ("_a", 0), ("_b", 4))
            .Allocate(InstanceAddr, 16 + (int)HeaderSize,
                ((int)HeaderSize + 0, U32(0xDEEDu)),
                ((int)HeaderSize + 4, U64(0xFEEDu)));

        TestCross t = Materialize<TestCross>(target, InstanceAddr);

        Assert.Equal(0xDEEDu, t.A);
        Assert.Equal((TargetPointer)0xFEEDu, t.B);
    }

    [Fact]
    public void Cross_BothMissingField_Throws()
    {
        // Native descriptor exists but lacks "A"; managed descriptor exists but lacks "_a".
        // (Both have B/_b so the ctor only fails when reading A.)
        var target = NewTargetWithObject()
            .AddNativeType("TestCross", size: 16, ("B", 8))
            .AddManagedType("Test.Cross", size: 8, ("_b", 0))
            .Allocate(InstanceAddr, 32);

        Assert.Throws<InvalidOperationException>(
            () => Materialize<TestCross>(target, InstanceAddr));
    }

    // ================================================================
    // Aliases
    // ================================================================

    [Fact]
    public void Native_AliasFallback()
    {
        // Primary "A" missing in native descriptor; alias "A_old" present.
        var target = new TestTarget()
            .AddNativeType("TestNativeAlias", size: 4, ("A_old", 0))
            .Allocate(InstanceAddr, 4, (0, U32(99u)));

        TestNativeAlias t = Materialize<TestNativeAlias>(target, InstanceAddr);

        Assert.Equal(99u, t.A);
    }

    [Fact]
    public void Managed_AliasFallback()
    {
        var target = NewTargetWithObject()
            .AddManagedType("Test.ManagedAlias", size: 4, ("_a_old", 0))
            .Allocate(InstanceAddr, 4 + (int)HeaderSize,
                ((int)HeaderSize + 0, U32(123u)));

        TestManagedAlias t = Materialize<TestManagedAlias>(target, InstanceAddr);

        Assert.Equal(123u, t.A);
    }

    [Fact]
    public void Cross_NativeAliasOnly()
    {
        // Native has only the alias; managed isn't registered at all.
        var target = NewTargetWithObject()
            .AddNativeType("TestCrossAlias", size: 4, ("A_old", 0))
            .Allocate(InstanceAddr, 4, (0, U32(0x1111u)));

        TestCrossAlias t = Materialize<TestCrossAlias>(target, InstanceAddr);

        Assert.Equal(0x1111u, t.A);
    }

    [Fact]
    public void Cross_ManagedAliasOnly()
    {
        // No native descriptor; managed has only the alias.
        var target = NewTargetWithObject()
            .AddManagedType("Test.CrossAlias", size: 4, ("_a_old", 0))
            .Allocate(InstanceAddr, 4 + (int)HeaderSize,
                ((int)HeaderSize + 0, U32(0x2222u)));

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

        t.WriteFlags(target, 0xFEED_FACEu);

        Assert.Equal(0xFEED_FACEu, t.Flags); // in-memory snapshot refreshed
        Assert.Equal(0xFEED_FACEu, BitConverter.ToUInt32(target.Bytes(InstanceAddr)));
    }

    [Fact]
    public void WriteManaged_RoundTrip()
    {
        var target = NewTargetWithObject()
            .AddManagedType("Test.WriteManaged", size: 4, ("_flags", 0))
            .Allocate(InstanceAddr, 4 + (int)HeaderSize,
                ((int)HeaderSize + 0, U32(1u)));

        TestWriteManaged t = Materialize<TestWriteManaged>(target, InstanceAddr);

        t.WriteFlags(target, 0xABCDu);

        Assert.Equal(0xABCDu, t.Flags);
        Assert.Equal(0xABCDu, BitConverter.ToUInt32(target.Bytes(InstanceAddr).Slice((int)HeaderSize, 4)));
    }

    [Fact]
    public void WriteCross_NativeResolves_WritesNative()
    {
        var target = NewTargetWithObject()
            .AddNativeType("TestWriteCross", size: 4, ("Flags", 0))
            .AddManagedType("Test.WriteCross", size: 4, ("_flags", 0))
            .Allocate(InstanceAddr, 4 + (int)HeaderSize, (0, U32(0u)), ((int)HeaderSize + 0, U32(0u)));

        TestWriteCross t = Materialize<TestWriteCross>(target, InstanceAddr);
        t.WriteFlags(target, 0x33u);

        // Write must hit the NATIVE region (address + 0), not the managed region.
        Assert.Equal(0x33u, BitConverter.ToUInt32(target.Bytes(InstanceAddr).Slice(0, 4)));
        Assert.Equal(0u, BitConverter.ToUInt32(target.Bytes(InstanceAddr).Slice((int)HeaderSize, 4)));
    }

    [Fact]
    public void WriteCross_ManagedResolves_WritesManagedWithOffset()
    {
        // Native descriptor lacks "Flags"; write should fall back to managed _flags.
        var target = NewTargetWithObject()
            .AddNativeType("TestWriteCross", size: 4 + (int)HeaderSize) // No "Flags" field declared.
            .AddManagedType("Test.WriteCross", size: 4, ("_flags", 0))
            .Allocate(InstanceAddr, 4 + (int)HeaderSize, ((int)HeaderSize + 0, U32(0u)));

        TestWriteCross t = Materialize<TestWriteCross>(target, InstanceAddr);
        t.WriteFlags(target, 0x44u);

        Assert.Equal(0x44u, BitConverter.ToUInt32(target.Bytes(InstanceAddr).Slice((int)HeaderSize, 4)));
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
        // Descriptor doesn't include "Optional"; the IsOptional path uses ContainsKey + default.
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
        var target = NewTargetWithObject()
            .AddNativeType("TestFieldAddr", size: 16, ("A", 0), ("Anchor", 12))
            .Allocate(InstanceAddr, 16, (0, U32(1u)));

        TestFieldAddr t = Materialize<TestFieldAddr>(target, InstanceAddr);

        Assert.Equal((TargetPointer)(InstanceAddr + 12), t.AnchorAddress);
    }

    [Fact]
    public void FieldAddress_ManagedResolves()
    {
        // No native descriptor.
        var target = NewTargetWithObject()
            .AddManagedType("Test.FieldAddr", size: 16, ("_a", 0), ("Anchor", 12))
            .Allocate(InstanceAddr, 16 + (int)HeaderSize, ((int)HeaderSize, U32(1u)));

        TestFieldAddr t = Materialize<TestFieldAddr>(target, InstanceAddr);

        Assert.Equal((TargetPointer)(InstanceAddr + HeaderSize + 12), t.AnchorAddress);
    }

    // ================================================================
    // UsePropertyName = false
    // ================================================================

    [Fact]
    public void UsePropertyName_False_OnlyExplicitNamesUsed()
    {
        // Descriptor has "m_flags" but NOT "Flags" (the property name).
        // UsePropertyName=false means the property name is excluded from the cascade.
        var target = new TestTarget()
            .AddNativeType("TestNoPropertyName", size: 4, ("m_flags", 0))
            .Allocate(InstanceAddr, 4, (0, U32(0xBEEFu)));

        TestNoPropertyName t = Materialize<TestNoPropertyName>(target, InstanceAddr);

        Assert.Equal(0xBEEFu, t.Flags);
    }

    [Fact]
    public void UsePropertyName_False_PropertyNameNotUsed()
    {
        // Descriptor has only "Flags" (the property name), NOT "m_flags".
        // UsePropertyName=false means the property name should NOT be tried.
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
}
