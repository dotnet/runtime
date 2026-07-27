// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;
using Microsoft.Diagnostics.DataContractReader.Data.GeneratorTests;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DataGeneratorTests;

/// <summary>
/// Verifies that a type originally backed by managed metadata can be
/// migrated to a native data descriptor WITHOUT changing the IData type.
/// The runtime simply starts publishing a native descriptor using the
/// same managed type name and field names. The cdac resolves fields
/// from the native descriptor transparently.
/// </summary>
public class ManagedToNativeTests
{
    private const ulong Addr = 0x1000;

    private static byte[] U32(uint v) => BitConverter.GetBytes(v);
    private static byte[] U64(ulong v) => BitConverter.GetBytes(v);

    private static T Materialize<T>(TestTarget target, ulong address) where T : IData<T>
        => target.ProcessedData.GetOrAdd<T>(new TargetPointer(address));

    [Fact]
    public void Fields_BeforeMigration_ReadsFromManaged()
    {
        var target = new TestTarget()
            .AddManagedType("Test.MigrateMN", size: 12,
                ("_value", 0),
                ("_ptr", 4))
            .Allocate(Addr, 12,
                (0, U32(0xAAAAu)),
                (4, U64(0xBBBBu)));

        MigrateMNFields t = Materialize<MigrateMNFields>(target, Addr);

        Assert.Equal(0xAAAAu, t._value);
        Assert.Equal((TargetPointer)0xBBBBu, t._ptr);
    }

    [Fact]
    public void Fields_AfterMigration_ReadsFromNativeWithSameNames()
    {
        var target = new TestTarget()
            .AddNativeType("Test.MigrateMN", size: 16,
                ("_value", 0),
                ("_ptr", 8))
            .Allocate(Addr, 16,
                (0, U32(0xCCCCu)),
                (8, U64(0xDDDDu)));

        MigrateMNFields t = Materialize<MigrateMNFields>(target, Addr);

        Assert.Equal(0xCCCCu, t._value);
        Assert.Equal((TargetPointer)0xDDDDu, t._ptr);
    }

    [Fact]
    public void Writable_BeforeMigration_WritesToManaged()
    {
        var target = new TestTarget()
            .AddManagedType("Test.MigrateMNWritable", size: 4,
                ("_flags", 0))
            .Allocate(Addr, 4, (0, U32(0u)));

        MigrateMNWritable t = Materialize<MigrateMNWritable>(target, Addr);
        t.Write_flags(0xFACEu);

        Assert.Equal(0xFACEu, t._flags);
    }

    [Fact]
    public void Writable_AfterMigration_WritesToNative()
    {
        var target = new TestTarget()
            .AddNativeType("Test.MigrateMNWritable", size: 4,
                ("_flags", 0))
            .Allocate(Addr, 4, (0, U32(0u)));

        MigrateMNWritable t = Materialize<MigrateMNWritable>(target, Addr);
        t.Write_flags(0xBEEFu);

        Assert.Equal(0xBEEFu, t._flags);
    }

    [Fact]
    public void Static_BeforeMigration_ResolvesFromManaged()
    {
        const ulong slotAddr = 0x5000;
        var target = new TestTarget()
            .AddManagedStaticField("Test.MigrateMNStatic", "s_instance", slotAddr);

        Assert.Equal((TargetPointer)slotAddr, MigrateMNStatic.Instance(target));
    }

    [Fact]
    public void Static_AfterMigration_ResolvesFromNativeGlobal()
    {
        const ulong slotAddr = 0x6000;
        var target = new TestTarget()
            .AddNativeType("Test.MigrateMNStatic", size: 0)
            .AddGlobal("Test.MigrateMNStatic.s_instance", slotAddr);

        Assert.Equal((TargetPointer)slotAddr, MigrateMNStatic.Instance(target));
    }
}
