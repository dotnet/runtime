// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class ExternalMemoryHandleRootTests
{
    private static readonly MockTarget.Architecture Arch = new() { IsLittleEndian = true, Is64Bit = true };
    private const ulong ExternalMemoryHandlesHeadSlotAddr = 0x0500;
    private const ulong HandleAddr = 0x1800;
    private const ulong MethodTableAddr = 0x2000;
    private const ulong ObjectSize = 0x100;
    private const ulong ResolvedMethodTableAddr = 0x9000;

    private static TestPlaceholderTarget CreateTarget(
        TargetPointer memory,
        uint gcFlags,
        Mock<IRuntimeTypeSystem> rts,
        IEnumerable<MockMemorySpace.HeapFragment>? fragments = null,
        Mock<IGC>? gc = null,
        bool hasHandle = true)
    {
        TargetTestHelpers helpers = new(Arch);
        int pointerSize = helpers.PointerSize;
        Mock<IGC> mockGC = gc ?? new Mock<IGC>();
        if (gc is null)
            mockGC.Setup(g => g.GetGCIdentifiers()).Returns([]);

        var builder = new TestPlaceholderTarget.Builder(Arch)
            .AddGlobals((Constants.Globals.ExternalMemoryHandles, ExternalMemoryHandlesHeadSlotAddr))
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.ExternalMemoryHandle] = new()
                {
                    Fields = new Dictionary<string, Target.FieldInfo>
                    {
                        { nameof(Data.ExternalMemoryHandle.Next), new() { Offset = 0, TypeName = DataType.pointer.ToString() } },
                        { nameof(Data.ExternalMemoryHandle.MethodTable), new() { Offset = pointerSize, TypeName = DataType.pointer.ToString() } },
                        { nameof(Data.ExternalMemoryHandle.Memory), new() { Offset = 2 * pointerSize, TypeName = DataType.pointer.ToString() } },
                        { nameof(Data.ExternalMemoryHandle.GCFlags), new() { Offset = 3 * pointerSize, TypeName = DataType.uint32.ToString() } },
                    }
                },
                [DataType.Object] = TargetTestHelpers.CreateTypeInfo(MockObjectData.CreateLayout(Arch)),
                [DataType.Array] = TargetTestHelpers.CreateTypeInfo(MockArrayObjectData.CreateLayout(Arch)),
                [DataType.String] = TargetTestHelpers.CreateTypeInfo(MockStringObjectData.CreateLayout(Arch)),
            })
            .AddGlobals((nameof(Constants.Globals.ObjectToMethodTableUnmask), 0ul))
            .AddContract<IExternalMemoryHandles>(version: "c1")
            .AddMockContract(mockGC)
            .AddMockContract(rts);

        builder.MemoryBuilder.AddHeapFragment(PointerFragment(ExternalMemoryHandlesHeadSlotAddr, hasHandle ? HandleAddr : 0));
        if (hasHandle)
            builder.MemoryBuilder.AddHeapFragment(ExternalMemoryHandleFragment(memory.Value, gcFlags));

        if (fragments is not null)
        {
            foreach (MockMemorySpace.HeapFragment fragment in fragments)
                builder.MemoryBuilder.AddHeapFragment(fragment);
        }

        return builder.Build();
    }

    private static void SetupResolvableObjects(
        Mock<IGC> gc,
        Mock<IRuntimeTypeSystem> rts,
        List<MockMemorySpace.HeapFragment> fragments,
        params ulong[] objectAddresses)
    {
        ulong segmentStart = objectAddresses[0] - ObjectSize;
        ulong segmentEnd = objectAddresses[^1] + ObjectSize;
        var segment = new GCHeapSegmentInfo(new TargetPointer(segmentStart), new TargetPointer(segmentEnd), GCSegmentClassification.Gen0);

        gc.Setup(g => g.GetGCIdentifiers()).Returns([GCIdentifiers.Workstation]);
        gc.Setup(g => g.GetHeapData()).Returns(default(GCHeapData));
        gc.Setup(g => g.EnumerateHeapSegments(It.IsAny<GCHeapData>())).Returns([segment]);
        gc.Setup(g => g.AlignObjectSize(ObjectSize, GCSegmentClassification.Gen0)).Returns(ObjectSize);

        ulong previous = segmentStart;
        foreach (ulong objectAddress in objectAddresses)
        {
            gc.Setup(g => g.GetPotentialNextObjectAddress(new TargetPointer(previous), previous == segmentStart ? 0ul : ObjectSize, segment))
                .Returns(new TargetPointer(objectAddress));
            fragments.Add(PointerFragment(objectAddress, ResolvedMethodTableAddr));
            previous = objectAddress;
        }
        gc.Setup(g => g.GetPotentialNextObjectAddress(new TargetPointer(previous), ObjectSize, segment))
            .Returns(new TargetPointer(segmentEnd));

        ITypeHandle resolvedTypeHandle = new TargetTypeHandle(new TargetPointer(ResolvedMethodTableAddr));
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(ResolvedMethodTableAddr))).Returns(resolvedTypeHandle);
        rts.Setup(r => r.GetBaseSize(resolvedTypeHandle)).Returns((uint)ObjectSize);
        rts.Setup(r => r.GetComponentSize(resolvedTypeHandle)).Returns(0u);
    }

    private static MockMemorySpace.HeapFragment PointerFragment(ulong address, ulong value)
    {
        TargetTestHelpers helpers = new(Arch);
        byte[] data = new byte[helpers.PointerSize];
        helpers.WritePointer(data, value);
        return new MockMemorySpace.HeapFragment { Address = address, Data = data, Name = "Pointer" };
    }

    private static MockMemorySpace.HeapFragment ExternalMemoryHandleFragment(ulong memory, uint gcFlags)
    {
        TargetTestHelpers helpers = new(Arch);
        int pointerSize = helpers.PointerSize;
        byte[] data = new byte[3 * pointerSize + sizeof(uint)];
        helpers.WritePointer(data.AsSpan(pointerSize, pointerSize), MethodTableAddr);
        helpers.WritePointer(data.AsSpan(2 * pointerSize, pointerSize), memory);
        helpers.Write(data.AsSpan(3 * pointerSize, sizeof(uint)), gcFlags);
        return new MockMemorySpace.HeapFragment { Address = HandleAddr, Data = data, Name = "ExternalMemoryHandle" };
    }

    [Fact]
    public void ReferenceType_OrdinarySlot_ReportsAddress()
    {
        const ulong MemoryAddr = 0x3000;
        var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
        ITypeHandle typeHandle = new TargetTypeHandle(new TargetPointer(MethodTableAddr));
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(MethodTableAddr))).Returns(typeHandle);
        rts.Setup(r => r.IsValueType(typeHandle)).Returns(false);

        IExternalMemoryHandles externalMemoryHandles = CreateTarget(new TargetPointer(MemoryAddr), gcFlags: 0, rts).Contracts.ExternalMemoryHandles;

        ExternalMemoryHandleRootData root = Assert.Single(externalMemoryHandles.GetRoots(resolveInteriorPointers: true));
        Assert.False(root.IsInteriorPointer);
        Assert.Equal(new TargetPointer(MemoryAddr), root.Address);
        Assert.Equal(TargetPointer.Null, root.Object);
    }

    [Fact]
    public void ReferenceType_InteriorSlot_ReportsResolvedObject()
    {
        const ulong MemoryAddr = 0x3000;
        const ulong ObjectAddr = 0x4000;
        var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
        ITypeHandle typeHandle = new TargetTypeHandle(new TargetPointer(MethodTableAddr));
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(MethodTableAddr))).Returns(typeHandle);
        rts.Setup(r => r.IsValueType(typeHandle)).Returns(false);

        var gc = new Mock<IGC>();
        List<MockMemorySpace.HeapFragment> fragments = [PointerFragment(MemoryAddr, ObjectAddr)];
        SetupResolvableObjects(gc, rts, fragments, ObjectAddr);

        IExternalMemoryHandles externalMemoryHandles = CreateTarget(new TargetPointer(MemoryAddr), gcFlags: 1, rts, fragments, gc).Contracts.ExternalMemoryHandles;

        ExternalMemoryHandleRootData root = Assert.Single(externalMemoryHandles.GetRoots(resolveInteriorPointers: true));
        Assert.True(root.IsInteriorPointer);
        Assert.Equal(new TargetPointer(MemoryAddr), root.Address);
        Assert.Equal(new TargetPointer(ObjectAddr), root.Object);
    }

    [Fact]
    public void ReferenceType_InteriorSlot_UnresolvableInteriorPointer_IsDropped()
    {
        const ulong MemoryAddr = 0x3000;
        const ulong ObjectAddr = 0x4000;
        var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
        ITypeHandle typeHandle = new TargetTypeHandle(new TargetPointer(MethodTableAddr));
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(MethodTableAddr))).Returns(typeHandle);
        rts.Setup(r => r.IsValueType(typeHandle)).Returns(false);

        IExternalMemoryHandles externalMemoryHandles = CreateTarget(
            new TargetPointer(MemoryAddr),
            gcFlags: 1,
            rts,
            [PointerFragment(MemoryAddr, ObjectAddr)]).Contracts.ExternalMemoryHandles;

        Assert.Empty(externalMemoryHandles.GetRoots(resolveInteriorPointers: true));
    }

    [Fact]
    public void ValueType_ContainsGCPointers_ReportsSeriesSlots()
    {
        const ulong MemoryAddr = 0x5000;
        var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
        ITypeHandle typeHandle = new TargetTypeHandle(new TargetPointer(MethodTableAddr));
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(MethodTableAddr))).Returns(typeHandle);
        rts.Setup(r => r.IsValueType(typeHandle)).Returns(true);
        rts.Setup(r => r.IsByRefLike(typeHandle)).Returns(false);
        rts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(true);
        rts.Setup(r => r.GetGCDescSeries(typeHandle, 0u)).Returns([(16u, 16u)]);

        IExternalMemoryHandles externalMemoryHandles = CreateTarget(new TargetPointer(MemoryAddr), gcFlags: 0, rts).Contracts.ExternalMemoryHandles;

        IReadOnlyList<ExternalMemoryHandleRootData> roots = externalMemoryHandles.GetRoots(resolveInteriorPointers: true);
        Assert.Equal([MemoryAddr + 8, MemoryAddr + 16], roots.Select(r => r.Address.Value).ToArray());
        Assert.All(roots, r => Assert.False(r.IsInteriorPointer));
    }

    [Fact]
    public void ValueType_ByRefLike_ReportsResolvedObject()
    {
        const ulong MemoryAddr = 0x6000;
        const ulong ObjectAddr = 0x7000;
        const ulong FieldDescAddr = 0x8000;
        var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
        ITypeHandle typeHandle = new TargetTypeHandle(new TargetPointer(MethodTableAddr));
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(MethodTableAddr))).Returns(typeHandle);
        rts.Setup(r => r.IsValueType(typeHandle)).Returns(true);
        rts.Setup(r => r.IsByRefLike(typeHandle)).Returns(true);
        rts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(false);
        rts.Setup(r => r.IsInlineArray(typeHandle)).Returns(false);
        rts.Setup(r => r.GetFieldDescList(typeHandle)).Returns([new TargetPointer(FieldDescAddr)]);
        rts.Setup(r => r.IsFieldDescStatic(new TargetPointer(FieldDescAddr))).Returns(false);
        rts.Setup(r => r.GetFieldDescType(new TargetPointer(FieldDescAddr))).Returns(CorElementType.Byref);
        rts.Setup(r => r.GetFieldDescOffset(new TargetPointer(FieldDescAddr), null)).Returns(0u);

        var gc = new Mock<IGC>();
        List<MockMemorySpace.HeapFragment> fragments = [PointerFragment(MemoryAddr, ObjectAddr)];
        SetupResolvableObjects(gc, rts, fragments, ObjectAddr);

        IExternalMemoryHandles externalMemoryHandles = CreateTarget(new TargetPointer(MemoryAddr), gcFlags: 0, rts, fragments, gc).Contracts.ExternalMemoryHandles;

        ExternalMemoryHandleRootData root = Assert.Single(externalMemoryHandles.GetRoots(resolveInteriorPointers: true));
        Assert.True(root.IsInteriorPointer);
        Assert.Equal(new TargetPointer(ObjectAddr), root.Object);
    }

    [Fact]
    public void ValueType_ByRefLikeInlineArray_ReportsEveryElement()
    {
        const ulong MemoryAddr = 0x6100;
        const ulong FieldDescAddr = 0x8100;
        const uint ElementSize = 8;
        ulong[] objectAddresses = [0x7100, 0x7200, 0x7300];
        var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
        ITypeHandle typeHandle = new TargetTypeHandle(new TargetPointer(MethodTableAddr));
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(MethodTableAddr))).Returns(typeHandle);
        rts.Setup(r => r.IsValueType(typeHandle)).Returns(true);
        rts.Setup(r => r.IsByRefLike(typeHandle)).Returns(true);
        rts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(false);
        rts.Setup(r => r.IsInlineArray(typeHandle)).Returns(true);
        rts.Setup(r => r.GetNumInstanceFieldBytes(typeHandle)).Returns(ElementSize * (uint)objectAddresses.Length);
        rts.Setup(r => r.GetFieldDescList(typeHandle)).Returns([new TargetPointer(FieldDescAddr)]);
        rts.Setup(r => r.IsFieldDescStatic(new TargetPointer(FieldDescAddr))).Returns(false);
        rts.Setup(r => r.GetFieldDescType(new TargetPointer(FieldDescAddr))).Returns(CorElementType.Byref);

        List<MockMemorySpace.HeapFragment> fragments = [];
        for (int i = 0; i < objectAddresses.Length; i++)
            fragments.Add(PointerFragment(MemoryAddr + (ulong)i * ElementSize, objectAddresses[i]));

        var gc = new Mock<IGC>();
        SetupResolvableObjects(gc, rts, fragments, objectAddresses);

        IExternalMemoryHandles externalMemoryHandles = CreateTarget(new TargetPointer(MemoryAddr), gcFlags: 0, rts, fragments, gc).Contracts.ExternalMemoryHandles;

        IReadOnlyList<ExternalMemoryHandleRootData> roots = externalMemoryHandles.GetRoots(resolveInteriorPointers: true);
        Assert.Equal(objectAddresses, roots.Select(r => r.Object.Value).ToArray());
        Assert.All(roots, r => Assert.True(r.IsInteriorPointer));
    }

    [Fact]
    public void NoExternalMemoryHandles_YieldsNoRoots()
    {
        var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
        IExternalMemoryHandles externalMemoryHandles = CreateTarget(TargetPointer.Null, gcFlags: 0, rts, hasHandle: false).Contracts.ExternalMemoryHandles;

        Assert.Empty(externalMemoryHandles.GetRoots(resolveInteriorPointers: true));
    }
}

public class RefWalkExternalMemoryHandleTests
{
    private static readonly MockTarget.Architecture Arch = new() { IsLittleEndian = true, Is64Bit = true };

    [Fact]
    public void WalkExternalMemoryHandles_MapsContractRoots()
    {
        TargetPointer appDomain = new(0x1000);
        TargetPointer ordinarySlot = new(0x2000);
        TargetPointer interiorObject = new(0x3000);
        var loader = new Mock<ILoader>();
        loader.Setup(l => l.GetAppDomain()).Returns(appDomain);
        var externalMemoryHandles = new Mock<IExternalMemoryHandles>();
        externalMemoryHandles.Setup(c => c.GetRoots(true)).Returns(
        [
            new ExternalMemoryHandleRootData { Address = ordinarySlot },
            new ExternalMemoryHandleRootData { IsInteriorPointer = true, Object = interiorObject },
        ]);

        var gc = new Mock<IGC>();
        gc.Setup(g => g.GetSupportedHandleTypes()).Returns([]);
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(Arch)
            .AddMockContract(loader)
            .AddMockContract(externalMemoryHandles)
            .AddMockContract(gc)
            .Build();

        RefWalk walk = new(target, walkStacks: false, CorGCReferenceType.CorHandleStrong);
        List<DacGcReference> references = [];
        while (walk.Enumerator.MoveNext())
            references.Add(walk.Enumerator.Current);

        Assert.Collection(
            references,
            reference =>
            {
                Assert.Equal(CorGCReferenceType.CorHandleStrong, reference.dwType);
                Assert.Equal(appDomain.Value, reference.vmDomain);
                Assert.Equal(ordinarySlot.Value, reference.objHnd);
            },
            reference =>
            {
                Assert.Equal(CorGCReferenceType.CorHandleStrong, reference.dwType);
                Assert.Equal(appDomain.Value, reference.vmDomain);
                Assert.Equal(interiorObject.Value | 1, reference.pObject);
            });
    }
}
