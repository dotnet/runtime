// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class RuntimeMutableTypeSystemTests
{
    private const string EnCContractVersion = "c1";

    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockRuntimeMutableTypeSystemBuilder builder)
        => new()
        {
            [DataType.Module] = TargetTestHelpers.CreateTypeInfo(builder.ModuleLayout),
            [DataType.UnorderedArrayBase] = TargetTestHelpers.CreateTypeInfo(builder.UnorderedArrayBaseLayout),
            [DataType.EnCEEClassData] = TargetTestHelpers.CreateTypeInfo(builder.ClassDataLayout),
            [DataType.EnCAddedFieldElement] = TargetTestHelpers.CreateTypeInfo(builder.AddedFieldElementLayout),
        };

    private static TestPlaceholderTarget CreateTarget(
        MockTarget.Architecture arch,
        MockRuntimeMutableTypeSystemBuilder builder,
        Mock<IRuntimeTypeSystem> mockRuntimeTypeSystem,
        Mock<ILoader> mockLoader)
    {
        return new TestPlaceholderTarget.Builder(arch)
            .UseReader(builder.Builder.GetMemoryContext().ReadFromTarget)
            .AddTypes(CreateContractTypes(builder))
            .AddContract<IRuntimeMutableTypeSystem>(version: EnCContractVersion)
            .AddMockContract(mockRuntimeTypeSystem)
            .AddMockContract(mockLoader)
            .Build();
    }

    private static (Mock<IRuntimeTypeSystem> Rts, Mock<ILoader> Loader) CreateMocks(
        TargetPointer mtPtr,
        TargetPointer modulePtr,
        ModuleFlags flags)
    {
        var rts = new Mock<IRuntimeTypeSystem>();
        rts.Setup(r => r.GetTypeHandle(mtPtr)).Returns(new TypeHandle(mtPtr));
        rts.Setup(r => r.GetModule(It.Is<TypeHandle>(th => th.Address == mtPtr))).Returns(modulePtr);

        var loader = new Mock<ILoader>();
        Contracts.ModuleHandle moduleHandle = new Contracts.ModuleHandle(modulePtr);
        loader.Setup(l => l.GetModuleHandleFromModulePtr(modulePtr)).Returns(moduleHandle);
        loader.Setup(l => l.GetFlags(moduleHandle)).Returns(flags);

        return (rts, loader);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TypeDescHandle_ReturnsEmpty(MockTarget.Architecture arch)
    {
        var builder = new MockRuntimeMutableTypeSystemBuilder(new MockMemorySpace.Builder(new TargetTestHelpers(arch)));
        // Allocate a Module just so the rest of the wiring is valid.
        MockEnCModule module = builder.AddModule(Array.Empty<MockEnCEEClassData>());

        // TypeDesc handles have a non-zero low bit. We pick a pointer with the low bit set.
        TargetPointer tdPtr = new TargetPointer(0x1000_0001);
        var (rts, loader) = CreateMocks(tdPtr, module.Address, ModuleFlags.EditAndContinue);

        TestPlaceholderTarget target = CreateTarget(arch, builder, rts, loader);
        IRuntimeMutableTypeSystem contract = target.Contracts.RuntimeMutableTypeSystem;
        Assert.NotNull(contract);

        TypeHandle th = target.Contracts.RuntimeTypeSystem.GetTypeHandle(tdPtr);
        Assert.Empty(contract.EnumerateAddedFieldDescs(th, staticFields: false));
        Assert.Empty(contract.EnumerateAddedFieldDescs(th, staticFields: true));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnCNotEnabled_ReturnsEmpty(MockTarget.Architecture arch)
    {
        var builder = new MockRuntimeMutableTypeSystemBuilder(new MockMemorySpace.Builder(new TargetTestHelpers(arch)));
        // Class data exists, but EditAndContinue flag is not set on the module.
        ulong mtPtr = 0x0000_8000;
        MockEnCEEClassData classData = builder.AddClassData(mtPtr);
        builder.AddInstanceFields(classData, count: 3);
        MockEnCModule module = builder.AddModule(new[] { classData });

        var (rts, loader) = CreateMocks(new TargetPointer(mtPtr), module.Address, flags: 0);
        TestPlaceholderTarget target = CreateTarget(arch, builder, rts, loader);

        IRuntimeMutableTypeSystem contract = target.Contracts.RuntimeMutableTypeSystem;
        TypeHandle th = target.Contracts.RuntimeTypeSystem.GetTypeHandle(mtPtr);
        Assert.Empty(contract.EnumerateAddedFieldDescs(th, staticFields: false));
        Assert.Empty(contract.EnumerateAddedFieldDescs(th, staticFields: true));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void NoMatchingClassData_ReturnsEmpty(MockTarget.Architecture arch)
    {
        var builder = new MockRuntimeMutableTypeSystemBuilder(new MockMemorySpace.Builder(new TargetTestHelpers(arch)));
        // ClassData entry exists but for a different MT.
        MockEnCEEClassData classData = builder.AddClassData(methodTable: 0x0000_8000);
        builder.AddInstanceFields(classData, count: 2);
        MockEnCModule module = builder.AddModule(new[] { classData });

        ulong otherMt = 0x0000_9000;
        var (rts, loader) = CreateMocks(new TargetPointer(otherMt), module.Address, ModuleFlags.EditAndContinue);
        TestPlaceholderTarget target = CreateTarget(arch, builder, rts, loader);

        IRuntimeMutableTypeSystem contract = target.Contracts.RuntimeMutableTypeSystem;
        TypeHandle th = target.Contracts.RuntimeTypeSystem.GetTypeHandle(otherMt);
        Assert.Empty(contract.EnumerateAddedFieldDescs(th, staticFields: false));
        Assert.Empty(contract.EnumerateAddedFieldDescs(th, staticFields: true));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EmptyClassList_ReturnsEmpty(MockTarget.Architecture arch)
    {
        var builder = new MockRuntimeMutableTypeSystemBuilder(new MockMemorySpace.Builder(new TargetTestHelpers(arch)));
        MockEnCModule module = builder.AddModule(Array.Empty<MockEnCEEClassData>());

        ulong mtPtr = 0x0000_8000;
        var (rts, loader) = CreateMocks(new TargetPointer(mtPtr), module.Address, ModuleFlags.EditAndContinue);
        TestPlaceholderTarget target = CreateTarget(arch, builder, rts, loader);

        IRuntimeMutableTypeSystem contract = target.Contracts.RuntimeMutableTypeSystem;
        TypeHandle th = target.Contracts.RuntimeTypeSystem.GetTypeHandle(mtPtr);
        Assert.Empty(contract.EnumerateAddedFieldDescs(th, staticFields: false));
        Assert.Empty(contract.EnumerateAddedFieldDescs(th, staticFields: true));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void InstanceFields_ReturnedInOrder(MockTarget.Architecture arch)
    {
        var builder = new MockRuntimeMutableTypeSystemBuilder(new MockMemorySpace.Builder(new TargetTestHelpers(arch)));
        ulong mtPtr = 0x0000_8000;
        MockEnCEEClassData classData = builder.AddClassData(mtPtr);
        IReadOnlyList<MockEnCAddedFieldElement> instanceElems = builder.AddInstanceFields(classData, count: 3);
        // No static fields for this class.
        MockEnCModule module = builder.AddModule(new[] { classData });

        var (rts, loader) = CreateMocks(new TargetPointer(mtPtr), module.Address, ModuleFlags.EditAndContinue);
        TestPlaceholderTarget target = CreateTarget(arch, builder, rts, loader);

        IRuntimeMutableTypeSystem contract = target.Contracts.RuntimeMutableTypeSystem;
        TypeHandle th = target.Contracts.RuntimeTypeSystem.GetTypeHandle(mtPtr);

        // FieldDesc is the address of the FieldDesc subfield within each element.
        ulong fieldDescOffset = (ulong)builder.AddedFieldElementLayout.GetField("FieldDesc").Offset;
        ulong[] expected = instanceElems.Select(e => e.Address + fieldDescOffset).ToArray();
        ulong[] actual = contract.EnumerateAddedFieldDescs(th, staticFields: false).Select(p => (ulong)p).ToArray();
        Assert.Equal(expected, actual);

        Assert.Empty(contract.EnumerateAddedFieldDescs(th, staticFields: true));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void StaticFields_ReturnedInOrder(MockTarget.Architecture arch)
    {
        var builder = new MockRuntimeMutableTypeSystemBuilder(new MockMemorySpace.Builder(new TargetTestHelpers(arch)));
        ulong mtPtr = 0x0000_8000;
        MockEnCEEClassData classData = builder.AddClassData(mtPtr);
        IReadOnlyList<MockEnCAddedFieldElement> staticElems = builder.AddStaticFields(classData, count: 2);
        MockEnCModule module = builder.AddModule(new[] { classData });

        var (rts, loader) = CreateMocks(new TargetPointer(mtPtr), module.Address, ModuleFlags.EditAndContinue);
        TestPlaceholderTarget target = CreateTarget(arch, builder, rts, loader);

        IRuntimeMutableTypeSystem contract = target.Contracts.RuntimeMutableTypeSystem;
        TypeHandle th = target.Contracts.RuntimeTypeSystem.GetTypeHandle(mtPtr);

        ulong fieldDescOffset = (ulong)builder.AddedFieldElementLayout.GetField("FieldDesc").Offset;
        ulong[] expected = staticElems.Select(e => e.Address + fieldDescOffset).ToArray();
        ulong[] actual = contract.EnumerateAddedFieldDescs(th, staticFields: true).Select(p => (ulong)p).ToArray();
        Assert.Equal(expected, actual);

        Assert.Empty(contract.EnumerateAddedFieldDescs(th, staticFields: false));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void InstanceAndStaticFields_ReturnedSeparately(MockTarget.Architecture arch)
    {
        var builder = new MockRuntimeMutableTypeSystemBuilder(new MockMemorySpace.Builder(new TargetTestHelpers(arch)));
        ulong mtPtr = 0x0000_8000;
        MockEnCEEClassData classData = builder.AddClassData(mtPtr);
        IReadOnlyList<MockEnCAddedFieldElement> instanceElems = builder.AddInstanceFields(classData, count: 4);
        IReadOnlyList<MockEnCAddedFieldElement> staticElems = builder.AddStaticFields(classData, count: 1);
        MockEnCModule module = builder.AddModule(new[] { classData });

        var (rts, loader) = CreateMocks(new TargetPointer(mtPtr), module.Address, ModuleFlags.EditAndContinue);
        TestPlaceholderTarget target = CreateTarget(arch, builder, rts, loader);

        IRuntimeMutableTypeSystem contract = target.Contracts.RuntimeMutableTypeSystem;
        TypeHandle th = target.Contracts.RuntimeTypeSystem.GetTypeHandle(mtPtr);

        ulong fieldDescOffset = (ulong)builder.AddedFieldElementLayout.GetField("FieldDesc").Offset;
        Assert.Equal(
            instanceElems.Select(e => e.Address + fieldDescOffset).ToArray(),
            contract.EnumerateAddedFieldDescs(th, staticFields: false).Select(p => (ulong)p).ToArray());
        Assert.Equal(
            staticElems.Select(e => e.Address + fieldDescOffset).ToArray(),
            contract.EnumerateAddedFieldDescs(th, staticFields: true).Select(p => (ulong)p).ToArray());
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SecondEntryMatches(MockTarget.Architecture arch)
    {
        // Validate linear search picks the second entry when the first does not match.
        var builder = new MockRuntimeMutableTypeSystemBuilder(new MockMemorySpace.Builder(new TargetTestHelpers(arch)));
        MockEnCEEClassData other = builder.AddClassData(methodTable: 0x0000_7000);
        builder.AddInstanceFields(other, count: 5);
        ulong mtPtr = 0x0000_8000;
        MockEnCEEClassData mine = builder.AddClassData(mtPtr);
        IReadOnlyList<MockEnCAddedFieldElement> instanceElems = builder.AddInstanceFields(mine, count: 2);
        MockEnCModule module = builder.AddModule(new[] { other, mine });

        var (rts, loader) = CreateMocks(new TargetPointer(mtPtr), module.Address, ModuleFlags.EditAndContinue);
        TestPlaceholderTarget target = CreateTarget(arch, builder, rts, loader);

        IRuntimeMutableTypeSystem contract = target.Contracts.RuntimeMutableTypeSystem;
        TypeHandle th = target.Contracts.RuntimeTypeSystem.GetTypeHandle(mtPtr);

        ulong fieldDescOffset = (ulong)builder.AddedFieldElementLayout.GetField("FieldDesc").Offset;
        ulong[] expected = instanceElems.Select(e => e.Address + fieldDescOffset).ToArray();
        Assert.Equal(expected, contract.EnumerateAddedFieldDescs(th, staticFields: false).Select(p => (ulong)p).ToArray());
    }
}
