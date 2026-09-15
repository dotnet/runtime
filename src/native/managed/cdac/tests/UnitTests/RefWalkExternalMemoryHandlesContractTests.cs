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

public class RefWalkExternalMemoryHandlesContractTests
{
    private static readonly MockTarget.Architecture Arch = new() { IsLittleEndian = true, Is64Bit = true };

    private const ulong AppDomainStaticSlotAddr = 0x0500;
    private const ulong AppDomainAddr = 0x1000;
    private const ulong HandleAddr = 0x2000;
    private const ulong MethodTableAddr = 0x9000;
    private const ulong MemoryAddr = 0x9500;

    private static TestPlaceholderTarget CreateTarget(Mock<IRuntimeTypeSystem> rts)
    {
        TargetTestHelpers helpers = new(Arch);
        int ptrSize = helpers.PointerSize;

        var mockGC = new Mock<IGC>();
        mockGC.Setup(g => g.GetSupportedHandleTypes()).Returns([]);

        var targetBuilder = new TestPlaceholderTarget.Builder(Arch)
            .AddGlobals(("AppDomain", AppDomainStaticSlotAddr))
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.AppDomain] = new()
                {
                    Fields = new Dictionary<string, Target.FieldInfo>
                    {
                        { nameof(Data.AppDomain.ExternalMemoryHandles), new() { Offset = 0, TypeName = DataType.pointer.ToString() } },
                    }
                },
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
                // GCInteriorPointerResolver (constructed unconditionally by RefWalk) reads these
                // minimal Object/Array/String descriptors regardless of whether a test exercises
                // interior-pointer resolution.
                [DataType.Object] = TargetTestHelpers.CreateTypeInfo(MockObjectData.CreateLayout(Arch)),
                [DataType.Array] = TargetTestHelpers.CreateTypeInfo(MockArrayObjectData.CreateLayout(Arch)),
                [DataType.String] = TargetTestHelpers.CreateTypeInfo(MockStringObjectData.CreateLayout(Arch)),
            })
            .AddGlobals((nameof(Constants.Globals.ObjectToMethodTableUnmask), 0ul))
            .AddContract<ILoader>(version: "c1")
            .AddContract<IExternalMemoryHandles>(version: "c1")
            .AddMockContract(mockGC)
            .AddMockContract(rts);

        // AppDomain* static slot -> the AppDomain instance
        targetBuilder.MemoryBuilder.AddHeapFragment(PointerFragment(helpers, AppDomainStaticSlotAddr, AppDomainAddr));

        // AppDomain.ExternalMemoryHandles -> a single handle
        targetBuilder.MemoryBuilder.AddHeapFragment(PointerFragment(helpers, AppDomainAddr, HandleAddr));

        // Handle: Next -> null, MethodTable, Memory, GCFlags=0 (ordinary reference-type root)
        targetBuilder.MemoryBuilder.AddHeapFragment(ExternalMemoryHandleFragment(helpers, HandleAddr, 0, MethodTableAddr, MemoryAddr, 0));

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

    private static List<DacGcReference> Walk(TestPlaceholderTarget target)
    {
        RefWalk walk = new(target, walkStacks: false, CorGCReferenceType.CorHandleStrong);
        List<DacGcReference> results = new();
        while (walk.Enumerator.MoveNext())
            results.Add(walk.Enumerator.Current);
        return results;
    }

    [Fact]
    public void ExternalMemoryHandlesContract_ContributesRoot()
    {
        var rts = new Mock<IRuntimeTypeSystem>(MockBehavior.Strict);
        ITypeHandle typeHandle = new TargetTypeHandle(new TargetPointer(MethodTableAddr));
        rts.Setup(r => r.GetTypeHandle(new TargetPointer(MethodTableAddr))).Returns(typeHandle);
        rts.Setup(r => r.IsValueType(typeHandle)).Returns(false);

        TestPlaceholderTarget target = CreateTarget(rts);

        List<DacGcReference> refs = Walk(target);

        DacGcReference reference = Assert.Single(refs);
        Assert.Equal(CorGCReferenceType.CorHandleStrong, reference.dwType);
        Assert.Equal(AppDomainAddr, reference.vmDomain);
        Assert.Equal(MemoryAddr, reference.objHnd);
        Assert.Equal(0ul, reference.i64ExtraData);
    }
}
