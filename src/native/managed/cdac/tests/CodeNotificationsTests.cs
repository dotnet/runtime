// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class CodeNotificationsTests
{
    // JITNotification struct layout for 64-bit LE:
    //   offset 0: state (ushort, 2 bytes)
    //   offset 2: 6 bytes padding
    //   offset 8: clrModule (ulong, 8 bytes)
    //   offset 16: methodToken (uint, 4 bytes)
    //   offset 20: 4 bytes padding
    //   total size: 24 bytes
    private const int StateOffset = 0;
    private const int ClrModuleOffset = 8;
    private const int MethodTokenOffset = 16;
    private const int EntrySize = 24;
    private const uint TableCapacity = 10;
    private const ulong TableAddress = 0x1_0000;
    private const ulong TablePointerAddress = 0x2_0000;

    private static ICodeNotifications CreateContractWithJITTable()
    {
        var arch = new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true };
        var helpers = new TargetTestHelpers(arch);

        var typeFields = new Dictionary<string, Target.FieldInfo>
        {
            [nameof(Data.JITNotification.State)] = new Target.FieldInfo { Offset = StateOffset },
            [nameof(Data.JITNotification.ClrModule)] = new Target.FieldInfo { Offset = ClrModuleOffset },
            [nameof(Data.JITNotification.MethodToken)] = new Target.FieldInfo { Offset = MethodTokenOffset },
        };
        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.JITNotification] = new Target.TypeInfo { Fields = typeFields, Size = EntrySize },
        };

        // Allocate table memory: (TableCapacity + 1) entries for bookkeeping + actual entries
        int totalTableSize = EntrySize * ((int)TableCapacity + 1);
        byte[] tableData = new byte[totalTableSize];

        // Initialize bookkeeping at index 0: length=0 (capacity now comes from the global).
        helpers.Write(tableData.AsSpan(MethodTokenOffset), (uint)0);

        byte[] tablePointerData = new byte[8];
        helpers.WritePointer(tablePointerData.AsSpan(), TableAddress);

        var builder = new TestPlaceholderTarget.Builder(arch);
        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = TableAddress,
            Data = tableData,
            Name = "JITNotificationTable"
        });
        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = TablePointerAddress,
            Data = tablePointerData,
            Name = "JITNotificationTablePointer"
        });

        builder.AddTypes(types);
        builder.AddGlobals(
            (Constants.Globals.JITNotificationTable, TablePointerAddress),
            (Constants.Globals.JITNotificationTableSize, TableCapacity)
        );
        builder.AddContract<ICodeNotifications>(version: "c1");

        var target = builder.Build();
        return target.Contracts.CodeNotifications;
    }

    [Fact]
    public void SetCodeNotification_NewEntry_CanBeRead()
    {
        ICodeNotifications contract = CreateContractWithJITTable();
        TargetPointer module = new(0xAABB_CCDD);
        uint token = 0x0600_0001;

        contract.SetCodeNotification(module, token, CodeNotificationKind.Generated);

        CodeNotificationKind result = contract.GetCodeNotification(module, token);
        Assert.Equal(CodeNotificationKind.Generated, result);
    }

    [Fact]
    public void GetCodeNotification_NotFound_ReturnsNone()
    {
        ICodeNotifications contract = CreateContractWithJITTable();
        TargetPointer module = new(0xDEAD);
        uint token = 0x0600_9999;

        CodeNotificationKind result = contract.GetCodeNotification(module, token);
        Assert.Equal(CodeNotificationKind.None, result);
    }

    [Fact]
    public void SetCodeNotification_Update_ChangesFlags()
    {
        ICodeNotifications contract = CreateContractWithJITTable();
        TargetPointer module = new(0x1234);
        uint token = 0x0600_0001;

        contract.SetCodeNotification(module, token, CodeNotificationKind.Generated);
        Assert.Equal(CodeNotificationKind.Generated, contract.GetCodeNotification(module, token));

        contract.SetCodeNotification(module, token, CodeNotificationKind.Discarded);
        Assert.Equal(CodeNotificationKind.Discarded, contract.GetCodeNotification(module, token));
    }

    [Fact]
    public void SetCodeNotification_ClearEntry_ReturnsNone()
    {
        ICodeNotifications contract = CreateContractWithJITTable();
        TargetPointer module = new(0x1234);
        uint token = 0x0600_0001;

        contract.SetCodeNotification(module, token, CodeNotificationKind.Generated);
        Assert.Equal(CodeNotificationKind.Generated, contract.GetCodeNotification(module, token));

        contract.SetCodeNotification(module, token, CodeNotificationKind.None);
        Assert.Equal(CodeNotificationKind.None, contract.GetCodeNotification(module, token));
    }

    [Fact]
    public void SetCodeNotification_MultipleEntries_IndependentlyReadable()
    {
        ICodeNotifications contract = CreateContractWithJITTable();
        TargetPointer module1 = new(0x1000);
        TargetPointer module2 = new(0x2000);
        uint token1 = 0x0600_0001;
        uint token2 = 0x0600_0002;

        contract.SetCodeNotification(module1, token1, CodeNotificationKind.Generated);
        contract.SetCodeNotification(module2, token2, CodeNotificationKind.Discarded);

        Assert.Equal(CodeNotificationKind.Generated, contract.GetCodeNotification(module1, token1));
        Assert.Equal(CodeNotificationKind.Discarded, contract.GetCodeNotification(module2, token2));
    }

    [Fact]
    public void SetAllCodeNotifications_ClearsAllEntries()
    {
        ICodeNotifications contract = CreateContractWithJITTable();
        TargetPointer module = new(0x1000);
        uint token1 = 0x0600_0001;
        uint token2 = 0x0600_0002;

        contract.SetCodeNotification(module, token1, CodeNotificationKind.Generated);
        contract.SetCodeNotification(module, token2, CodeNotificationKind.Generated);

        contract.SetAllCodeNotifications(TargetPointer.Null, CodeNotificationKind.None);

        Assert.Equal(CodeNotificationKind.None, contract.GetCodeNotification(module, token1));
        Assert.Equal(CodeNotificationKind.None, contract.GetCodeNotification(module, token2));
    }

    [Fact]
    public void SetAllCodeNotifications_FilterByModule_ClearsOnlyMatchingEntries()
    {
        ICodeNotifications contract = CreateContractWithJITTable();
        TargetPointer module1 = new(0x1000);
        TargetPointer module2 = new(0x2000);
        uint token = 0x0600_0001;

        contract.SetCodeNotification(module1, token, CodeNotificationKind.Generated);
        contract.SetCodeNotification(module2, token, CodeNotificationKind.Generated);

        contract.SetAllCodeNotifications(module1, CodeNotificationKind.None);

        Assert.Equal(CodeNotificationKind.None, contract.GetCodeNotification(module1, token));
        Assert.Equal(CodeNotificationKind.Generated, contract.GetCodeNotification(module2, token));
    }

    [Fact]
    public void SetAllCodeNotifications_UpdateFlags_ChangesAllMatching()
    {
        ICodeNotifications contract = CreateContractWithJITTable();
        TargetPointer module = new(0x1000);
        uint token1 = 0x0600_0001;
        uint token2 = 0x0600_0002;

        contract.SetCodeNotification(module, token1, CodeNotificationKind.Generated);
        contract.SetCodeNotification(module, token2, CodeNotificationKind.Generated);

        contract.SetAllCodeNotifications(TargetPointer.Null, CodeNotificationKind.Discarded);

        Assert.Equal(CodeNotificationKind.Discarded, contract.GetCodeNotification(module, token1));
        Assert.Equal(CodeNotificationKind.Discarded, contract.GetCodeNotification(module, token2));
    }

    // --- Null Table / Lazy Allocation Tests ---

    private static ICodeNotifications CreateContractWithNullTable(TestPlaceholderTarget.AllocateMemoryDelegate? allocateMemory = null)
    {
        var arch = new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true };

        var typeFields = new Dictionary<string, Target.FieldInfo>
        {
            [nameof(Data.JITNotification.State)] = new Target.FieldInfo { Offset = StateOffset },
            [nameof(Data.JITNotification.ClrModule)] = new Target.FieldInfo { Offset = ClrModuleOffset },
            [nameof(Data.JITNotification.MethodToken)] = new Target.FieldInfo { Offset = MethodTokenOffset },
        };
        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.JITNotification] = new Target.TypeInfo { Fields = typeFields, Size = EntrySize },
        };

        byte[] tablePointerData = new byte[8];

        var builder = new TestPlaceholderTarget.Builder(arch);
        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = TablePointerAddress,
            Data = tablePointerData,
            Name = "JITNotificationTablePointer"
        });

        builder.AddTypes(types);
        builder.AddGlobals(
            (Constants.Globals.JITNotificationTable, TablePointerAddress),
            (Constants.Globals.JITNotificationTableSize, TableCapacity)
        );
        builder.AddContract<ICodeNotifications>(version: "c1");

        if (allocateMemory is not null)
            builder.UseAllocateMemory(allocateMemory);

        return builder.Build().Contracts.CodeNotifications;
    }

    [Fact]
    public void GetCodeNotification_NullTable_ReturnsNone()
    {
        ICodeNotifications contract = CreateContractWithNullTable();
        CodeNotificationKind result = contract.GetCodeNotification(new TargetPointer(0x1000), 0x0600_0001);
        Assert.Equal(CodeNotificationKind.None, result);
    }

    [Fact]
    public void SetAllCodeNotifications_NullTable_NoOp()
    {
        ICodeNotifications contract = CreateContractWithNullTable();
        contract.SetAllCodeNotifications(TargetPointer.Null, CodeNotificationKind.None);
    }

    [Fact]
    public void SetCodeNotification_NullTable_ClearIsNoOp()
    {
        ICodeNotifications contract = CreateContractWithNullTable();
        contract.SetCodeNotification(new TargetPointer(0x1000), 0x0600_0001, CodeNotificationKind.None);
    }

    [Fact]
    public void SetCodeNotification_NullTable_NoAllocator_Throws()
    {
        ICodeNotifications contract = CreateContractWithNullTable(allocateMemory: null);
        Assert.Throws<NotImplementedException>(() =>
            contract.SetCodeNotification(new TargetPointer(0x1000), 0x0600_0001, CodeNotificationKind.Generated));
    }

    [Fact]
    public void SetCodeNotification_NullTable_LazyAllocates_ThenWorks()
    {
        var arch = new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true };

        int totalTableSize = EntrySize * ((int)TableCapacity + 1);
        byte[] allocatedTableData = new byte[totalTableSize];
        const ulong AllocatedTableAddress = 0x3_0000;

        var typeFields = new Dictionary<string, Target.FieldInfo>
        {
            [nameof(Data.JITNotification.State)] = new Target.FieldInfo { Offset = StateOffset },
            [nameof(Data.JITNotification.ClrModule)] = new Target.FieldInfo { Offset = ClrModuleOffset },
            [nameof(Data.JITNotification.MethodToken)] = new Target.FieldInfo { Offset = MethodTokenOffset },
        };
        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.JITNotification] = new Target.TypeInfo { Fields = typeFields, Size = EntrySize },
        };

        byte[] tablePointerData = new byte[8];

        var builder = new TestPlaceholderTarget.Builder(arch);
        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = TablePointerAddress,
            Data = tablePointerData,
            Name = "JITNotificationTablePointer"
        });
        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = AllocatedTableAddress,
            Data = allocatedTableData,
            Name = "AllocatedJITNotificationTable"
        });

        builder.AddTypes(types);
        builder.AddGlobals(
            (Constants.Globals.JITNotificationTable, TablePointerAddress),
            (Constants.Globals.JITNotificationTableSize, TableCapacity)
        );
        builder.AddContract<ICodeNotifications>(version: "c1");
        uint? requestedAllocationSize = null;
        builder.UseAllocateMemory((size) =>
        {
            requestedAllocationSize = size;
            return new TargetPointer(AllocatedTableAddress);
        });

        var target = builder.Build();
        ICodeNotifications contract = target.Contracts.CodeNotifications;

        TargetPointer module = new(0xAABB_CCDD);
        uint token = 0x0600_0001;

        contract.SetCodeNotification(module, token, CodeNotificationKind.Generated);

        CodeNotificationKind result = contract.GetCodeNotification(module, token);
        Assert.Equal(CodeNotificationKind.Generated, result);
        Assert.Equal((uint)(EntrySize * (TableCapacity + 1)), requestedAllocationSize);
    }
}
