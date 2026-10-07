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
        bool isByRef,
        Mock<IRuntimeTypeSystem> rts,
        IEnumerable<MockMemorySpace.HeapFragment>? fragments = null,
        Mock<IGC>? gc = null,
        bool hasHandle = true,
        MockTarget.Architecture? architecture = null)
    {
        MockTarget.Architecture arch = architecture ?? Arch;
        TargetTestHelpers helpers = new(arch);
        int pointerSize = helpers.PointerSize;
        ITypeHandle typeHandle = new TargetTypeHandle(new TargetPointer(MethodTableAddr));
        ulong handleType = isByRef ? MethodTableAddr | 2 : MethodTableAddr;
        if (isByRef)
        {
            ITypeHandle byRefHandle = new TargetTypeHandle(new TargetPointer(handleType));
            rts.Setup(r => r.GetTypeHandle(new TargetPointer(handleType))).Returns(byRefHandle);
            rts.Setup(r => r.GetSignatureCorElementType(byRefHandle)).Returns(CorElementType.Byref);
            rts.Setup(r => r.GetTypeParam(byRefHandle)).Returns(typeHandle);
        }
        else
        {
            rts.Setup(r => r.GetSignatureCorElementType(It.IsAny<ITypeHandle>()))
                .Returns((ITypeHandle handle) => rts.Object.IsValueType(handle) ? CorElementType.ValueType : CorElementType.Class);
        }
        Mock<IGC> mockGC = gc ?? new Mock<IGC>();
        if (gc is null)
            mockGC.Setup(g => g.GetGCIdentifiers()).Returns([]);

        var builder = new TestPlaceholderTarget.Builder(arch)
            .AddGlobals((Constants.Globals.ExternalMemoryHandles, ExternalMemoryHandlesHeadSlotAddr))
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.ExternalMemoryHandle] = new()
                {
                    Fields = new Dictionary<string, Target.FieldInfo>
                    {
                        { nameof(Data.ExternalMemoryHandle.Next), new() { Offset = 0, TypeName = DataType.pointer.ToString() } },
                        { nameof(Data.ExternalMemoryHandle.TypeHandle), new() { Offset = pointerSize, TypeName = DataType.pointer.ToString() } },
                        { nameof(Data.ExternalMemoryHandle.Memory), new() { Offset = 2 * pointerSize, TypeName = DataType.pointer.ToString() } },
                    }
                },
                [DataType.Object] = TargetTestHelpers.CreateTypeInfo(MockObjectData.CreateLayout(arch)),
                [DataType.Array] = TargetTestHelpers.CreateTypeInfo(MockArrayObjectData.CreateLayout(arch)),
                [DataType.String] = TargetTestHelpers.CreateTypeInfo(MockStringObjectData.CreateLayout(arch)),
            })
            .AddGlobals((nameof(Constants.Globals.ObjectToMethodTableUnmask), 0ul))
            .AddContract<IExternalMemoryHandles>(version: "c1")
            .AddMockContract(mockGC)
            .AddMockContract(rts);

        builder.MemoryBuilder.AddHeapFragment(PointerFragment(ExternalMemoryHandlesHeadSlotAddr, hasHandle ? HandleAddr : 0, arch));
        if (hasHandle)
            builder.MemoryBuilder.AddHeapFragment(ExternalMemoryHandleFragment(memory.Value, handleType, arch));

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
        params ulong[] objectAddresses) =>
        SetupResolvableObjects(gc, rts, fragments, Arch, objectAddresses);

    private static void SetupResolvableObjects(
        Mock<IGC> gc,
        Mock<IRuntimeTypeSystem> rts,
        List<MockMemorySpace.HeapFragment> fragments,
        MockTarget.Architecture architecture,
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
            fragments.Add(PointerFragment(objectAddress, ResolvedMethodTableAddr, architecture));
            previous = objectAddress;
        }
        gc.Setup(g => g.GetPotentialNextObjectAddress(new TargetPointer(previous), ObjectSize, segment))
            .Returns(new TargetPointer(segmentEnd));

        ITypeHandle resolvedTypeHandle = new TargetTypeHandle(new TargetPointer(ResolvedMethodTableAddr));
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(ResolvedMethodTableAddr))).Returns(resolvedTypeHandle);
        rts.Setup(r => r.GetBaseSize(resolvedTypeHandle)).Returns((uint)ObjectSize);
        rts.Setup(r => r.GetComponentSize(resolvedTypeHandle)).Returns(0u);
    }

    private static MockMemorySpace.HeapFragment PointerFragment(
        ulong address,
        ulong value,
        MockTarget.Architecture? architecture = null)
    {
        TargetTestHelpers helpers = new(architecture ?? Arch);
        byte[] data = new byte[helpers.PointerSize];
        helpers.WritePointer(data, value);
        return new MockMemorySpace.HeapFragment { Address = address, Data = data, Name = "Pointer" };
    }

    private static MockMemorySpace.HeapFragment ExternalMemoryHandleFragment(
        ulong memory,
        ulong typeHandle,
        MockTarget.Architecture? architecture = null)
    {
        TargetTestHelpers helpers = new(architecture ?? Arch);
        int pointerSize = helpers.PointerSize;
        byte[] data = new byte[3 * pointerSize];
        helpers.WritePointer(data.AsSpan(pointerSize, pointerSize), typeHandle);
        helpers.WritePointer(data.AsSpan(2 * pointerSize, pointerSize), memory);
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

        IExternalMemoryHandles externalMemoryHandles = CreateTarget(new TargetPointer(MemoryAddr), isByRef: false, rts).Contracts.ExternalMemoryHandles;

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

        IExternalMemoryHandles externalMemoryHandles = CreateTarget(new TargetPointer(MemoryAddr), isByRef: true, rts, fragments, gc).Contracts.ExternalMemoryHandles;

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
            isByRef: true,
            rts,
            [PointerFragment(MemoryAddr, ObjectAddr)]).Contracts.ExternalMemoryHandles;

        Assert.Empty(externalMemoryHandles.GetRoots(resolveInteriorPointers: true));
    }

    public static IEnumerable<object[]> InteriorSlotTestData()
    {
        foreach (object[] architecture in new MockTarget.StdArch())
        {
            foreach ((bool isValueType, bool containsGCPointers) in new[] { (false, false), (true, false), (true, true) })
            {
                yield return [architecture[0], isValueType, containsGCPointers, false];
                yield return [architecture[0], isValueType, containsGCPointers, true];
            }
        }
    }

    [Theory]
    [MemberData(nameof(InteriorSlotTestData))]
    public void InteriorSlot_ReportsPointerInsteadOfTargetLayout(
        MockTarget.Architecture architecture,
        bool isValueType,
        bool containsGCPointers,
        bool resolveInteriorPointers)
    {
        const ulong MemoryAddr = 0x3000;
        const ulong ObjectAddr = 0x4000;
        ulong interiorAddress = ObjectAddr + 0x20;
        var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
        ITypeHandle typeHandle = new TargetTypeHandle(new TargetPointer(MethodTableAddr));
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(MethodTableAddr))).Returns(typeHandle);
        rts.Setup(r => r.IsValueType(typeHandle)).Returns(isValueType);
        rts.Setup(r => r.IsByRefLike(typeHandle)).Returns(false);
        rts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(containsGCPointers);
        uint pointerSize = architecture.Is64Bit ? 8u : 4u;
        rts.Setup(r => r.GetGCDescSeries(typeHandle, 0u)).Returns([(pointerSize, pointerSize * 2)]);

        var gc = new Mock<IGC>();
        List<MockMemorySpace.HeapFragment> fragments = [PointerFragment(MemoryAddr, interiorAddress, architecture)];
        SetupResolvableObjects(gc, rts, fragments, architecture, ObjectAddr);
        IExternalMemoryHandles handles = CreateTarget(
            new TargetPointer(MemoryAddr), isByRef: true, rts, fragments, gc, architecture: architecture).Contracts.ExternalMemoryHandles;

        ExternalMemoryHandleRootData root = Assert.Single(handles.GetRoots(resolveInteriorPointers));
        Assert.True(root.IsInteriorPointer);
        Assert.Equal(new TargetPointer(MemoryAddr), root.Address);
        Assert.Equal(new TargetPointer(resolveInteriorPointers ? ObjectAddr : interiorAddress), root.Object);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ByRefSlot_NullAndInvalidPointers_AreDropped(MockTarget.Architecture architecture)
    {
        const ulong MemoryAddr = 0x3000;
        ulong invalidPointer = architecture.Is64Bit ? ulong.MaxValue : uint.MaxValue;
        foreach (ulong pointer in new[] { 0UL, invalidPointer })
        {
            var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
            IExternalMemoryHandles handles = CreateTarget(
                new TargetPointer(MemoryAddr), isByRef: true, rts,
                [PointerFragment(MemoryAddr, pointer, architecture)], architecture: architecture).Contracts.ExternalMemoryHandles;

            Assert.Empty(handles.GetRoots(resolveInteriorPointers: false));
            Assert.Empty(handles.GetRoots(resolveInteriorPointers: true));
        }
    }

    [Theory]
    [InlineData(CorElementType.Ptr)]
    [InlineData(CorElementType.FnPtr)]
    public void UnmanagedPointerLocal_ProducesNoGcRoot(CorElementType elementType)
    {
        const ulong MemoryAddr = 0x3000;
        var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
        ITypeHandle typeHandle = new TargetTypeHandle(new TargetPointer(MethodTableAddr));
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(MethodTableAddr))).Returns(typeHandle);
        rts.Setup(r => r.IsValueType(typeHandle)).Returns(false);
        IExternalMemoryHandles handles = CreateTarget(new TargetPointer(MemoryAddr), isByRef: false, rts).Contracts.ExternalMemoryHandles;
        rts.Setup(r => r.GetSignatureCorElementType(typeHandle)).Returns(elementType);

        Assert.Empty(handles.GetRoots(resolveInteriorPointers: false));
        Assert.Empty(handles.GetRoots(resolveInteriorPointers: true));
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

        IExternalMemoryHandles externalMemoryHandles = CreateTarget(new TargetPointer(MemoryAddr), isByRef: false, rts).Contracts.ExternalMemoryHandles;

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

        IExternalMemoryHandles externalMemoryHandles = CreateTarget(new TargetPointer(MemoryAddr), isByRef: false, rts, fragments, gc).Contracts.ExternalMemoryHandles;

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

        IExternalMemoryHandles externalMemoryHandles = CreateTarget(new TargetPointer(MemoryAddr), isByRef: false, rts, fragments, gc).Contracts.ExternalMemoryHandles;

        IReadOnlyList<ExternalMemoryHandleRootData> roots = externalMemoryHandles.GetRoots(resolveInteriorPointers: true);
        Assert.Equal(objectAddresses, roots.Select(r => r.Object.Value).ToArray());
        Assert.All(roots, r => Assert.True(r.IsInteriorPointer));
    }

    [Fact]
    public void NoExternalMemoryHandles_YieldsNoRoots()
    {
        var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
        IExternalMemoryHandles externalMemoryHandles = CreateTarget(TargetPointer.Null, isByRef: false, rts, hasHandle: false).Contracts.ExternalMemoryHandles;

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

    [Fact]
    public void WalkExternalMemoryHandles_MissingContract_YieldsNoRoots()
    {
        var loader = new Mock<ILoader>();
        loader.Setup(l => l.GetAppDomain()).Returns(new TargetPointer(0x1000));

        var gc = new Mock<IGC>();
        gc.Setup(g => g.GetSupportedHandleTypes()).Returns([]);

        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(Arch)
            .AddMockContract(loader)
            .AddMockContract(gc)
            .Build();

        RefWalk walk = new(target, walkStacks: false, CorGCReferenceType.CorHandleStrong);

        Assert.False(walk.Enumerator.MoveNext());
    }
}
