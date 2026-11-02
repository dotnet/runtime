// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class ExternalMemoryHandlesTests
{
    private static readonly MockTarget.Architecture Arch = new() { IsLittleEndian = true, Is64Bit = true };

    private const ulong ExternalMemoryHandlesHeadSlotAddr = 0x0500;
    private const ulong Handle1Addr = 0x2000;
    private const ulong Handle2Addr = 0x2100;
    private const ulong MethodTable1Addr = 0x9000;
    private const ulong MethodTable2Addr = 0x9100;
    private const ulong Memory1Addr = 0x9500;
    private const ulong Memory2Addr = 0x9600;

    private static TestPlaceholderTarget CreateTarget(bool hasHandles = true)
    {
        TargetTestHelpers helpers = new(Arch);
        int ptrSize = helpers.PointerSize;
        var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
        ITypeHandle typeHandle1 = new TargetTypeHandle(new TargetPointer(MethodTable1Addr));
        ITypeHandle typeHandle2 = new TargetTypeHandle(new TargetPointer(MethodTable2Addr));
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(MethodTable1Addr))).Returns(typeHandle1);
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(MethodTable2Addr))).Returns(typeHandle2);
        rts.Setup(r => r.IsValueType(typeHandle1)).Returns(false);
        rts.Setup(r => r.IsValueType(typeHandle2)).Returns(false);
        var gc = new Mock<IGC>();

        var targetBuilder = new TestPlaceholderTarget.Builder(Arch)
            .AddGlobals((Constants.Globals.ExternalMemoryHandles, ExternalMemoryHandlesHeadSlotAddr))
            .AddGlobals((nameof(Constants.Globals.ObjectToMethodTableUnmask), 0ul))
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.ExternalMemoryHandle] = new()
                {
                    Fields = new Dictionary<string, Target.FieldInfo>
                    {
                        { nameof(Data.ExternalMemoryHandle.Next), new() { Offset = 0, TypeName = DataType.pointer.ToString() } },
                        { nameof(Data.ExternalMemoryHandle.MethodTable), new() { Offset = ptrSize, TypeName = DataType.pointer.ToString() } },
                        { nameof(Data.ExternalMemoryHandle.Memory), new() { Offset = 2 * ptrSize, TypeName = DataType.pointer.ToString() } },
                        { nameof(Data.ExternalMemoryHandle.GCFlags), new() { Offset = 3 * ptrSize, TypeName = DataType.uint32.ToString() } },
                    }
                },
                [DataType.Object] = TargetTestHelpers.CreateTypeInfo(MockObjectData.CreateLayout(Arch)),
                [DataType.Array] = TargetTestHelpers.CreateTypeInfo(MockArrayObjectData.CreateLayout(Arch)),
                [DataType.String] = TargetTestHelpers.CreateTypeInfo(MockStringObjectData.CreateLayout(Arch)),
            })
            .AddContract<IExternalMemoryHandles>(version: "c1")
            .AddMockContract(gc)
            .AddMockContract(rts);

        // ExternalMemoryHandles global slot -> head of the list (or null when empty)
        targetBuilder.MemoryBuilder.AddHeapFragment(PointerFragment(helpers, ExternalMemoryHandlesHeadSlotAddr, hasHandles ? Handle1Addr : 0));

        // Handle1: Next -> Handle2, MethodTable1, Memory1, GCFlags=0
        targetBuilder.MemoryBuilder.AddHeapFragment(ExternalMemoryHandleFragment(helpers, Handle1Addr, Handle2Addr, MethodTable1Addr, Memory1Addr, 0));

        // Handle2: Next -> null, MethodTable2, Memory2, GCFlags=1
        targetBuilder.MemoryBuilder.AddHeapFragment(ExternalMemoryHandleFragment(helpers, Handle2Addr, 0, MethodTable2Addr, Memory2Addr, 1));
        targetBuilder.MemoryBuilder.AddHeapFragment(PointerFragment(helpers, Memory2Addr, 0x9700));

        return targetBuilder.Build();
    }

    private static MockMemorySpace.HeapFragment PointerFragment(TargetTestHelpers helpers, ulong address, ulong value)
    {
        byte[] data = new byte[helpers.PointerSize];
        helpers.WritePointer(data, value);
        return new MockMemorySpace.HeapFragment { Address = address, Data = data, Name = "Pointer" };
    }

    private static MockMemorySpace.HeapFragment ExternalMemoryHandleFragment(TargetTestHelpers helpers, ulong address, ulong next, ulong methodTable, ulong memory, uint gcFlags)
    {
        int ptrSize = helpers.PointerSize;
        byte[] data = new byte[3 * ptrSize + sizeof(uint)];
        helpers.WritePointer(data.AsSpan(0, ptrSize), next);
        helpers.WritePointer(data.AsSpan(ptrSize, ptrSize), methodTable);
        helpers.WritePointer(data.AsSpan(2 * ptrSize, ptrSize), memory);
        helpers.Write(data.AsSpan(3 * ptrSize, sizeof(uint)), gcFlags);
        return new MockMemorySpace.HeapFragment { Address = address, Data = data, Name = "ExternalMemoryHandle" };
    }

    [Fact]
    public void GetRoots_WalksChain()
    {
        TestPlaceholderTarget target = CreateTarget();
        IExternalMemoryHandles externalMemoryHandles = target.Contracts.ExternalMemoryHandles;

        IReadOnlyList<ExternalMemoryHandleRootData> roots = externalMemoryHandles.GetRoots(resolveInteriorPointers: false);

        Assert.Equal(2, roots.Count);
        Assert.False(roots[0].IsInteriorPointer);
        Assert.Equal(new TargetPointer(Memory1Addr), roots[0].Address);
        Assert.True(roots[1].IsInteriorPointer);
        Assert.Equal(new TargetPointer(Memory2Addr), roots[1].Address);
        Assert.Equal(new TargetPointer(0x9700), roots[1].Object);
    }

    [Fact]
    public void GetRoots_NoHandles_ReturnsEmpty()
    {
        TestPlaceholderTarget target = CreateTarget(hasHandles: false);
        IExternalMemoryHandles externalMemoryHandles = target.Contracts.ExternalMemoryHandles;

        IReadOnlyList<ExternalMemoryHandleRootData> roots = externalMemoryHandles.GetRoots(resolveInteriorPointers: true);

        Assert.Empty(roots);
    }

}
