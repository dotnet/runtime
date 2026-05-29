// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// A minimal mock of <c>Module</c> that contains only the Edit-and-Continue
/// related fields read by <c>EditAndContinue_1</c>. The EnC contract uses
/// <c>DataType.Module</c>, so tests register this layout under that key.
/// </summary>
internal sealed class MockEnCModule : TypedView
{
    private const string EnCClassListCountFieldName = "EnCClassListCount";
    private const string EnCClassListTableFieldName = "EnCClassListTable";

    public static Layout<MockEnCModule> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("Module", architecture)
            .AddUInt32Field(EnCClassListCountFieldName)
            .AddPointerField(EnCClassListTableFieldName)
            .Build<MockEnCModule>();

    public uint EnCClassListCount
    {
        get => ReadUInt32Field(EnCClassListCountFieldName);
        set => WriteUInt32Field(EnCClassListCountFieldName, value);
    }

    public ulong EnCClassListTable
    {
        get => ReadPointerField(EnCClassListTableFieldName);
        set => WritePointerField(EnCClassListTableFieldName, value);
    }
}

internal sealed class MockEnCEEClassData : TypedView
{
    private const string MethodTableFieldName = "MethodTable";
    private const string NumAddedInstanceFieldsFieldName = "NumAddedInstanceFields";
    private const string NumAddedStaticFieldsFieldName = "NumAddedStaticFields";
    private const string AddedInstanceFieldsFieldName = "AddedInstanceFields";
    private const string AddedStaticFieldsFieldName = "AddedStaticFields";

    public static Layout<MockEnCEEClassData> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("EnCEEClassData", architecture)
            .AddPointerField(MethodTableFieldName)
            .AddUInt32Field(NumAddedInstanceFieldsFieldName)
            .AddUInt32Field(NumAddedStaticFieldsFieldName)
            .AddPointerField(AddedInstanceFieldsFieldName)
            .AddPointerField(AddedStaticFieldsFieldName)
            .Build<MockEnCEEClassData>();

    public ulong MethodTable
    {
        get => ReadPointerField(MethodTableFieldName);
        set => WritePointerField(MethodTableFieldName, value);
    }

    public uint NumAddedInstanceFields
    {
        get => ReadUInt32Field(NumAddedInstanceFieldsFieldName);
        set => WriteUInt32Field(NumAddedInstanceFieldsFieldName, value);
    }

    public uint NumAddedStaticFields
    {
        get => ReadUInt32Field(NumAddedStaticFieldsFieldName);
        set => WriteUInt32Field(NumAddedStaticFieldsFieldName, value);
    }

    public ulong AddedInstanceFields
    {
        get => ReadPointerField(AddedInstanceFieldsFieldName);
        set => WritePointerField(AddedInstanceFieldsFieldName, value);
    }

    public ulong AddedStaticFields
    {
        get => ReadPointerField(AddedStaticFieldsFieldName);
        set => WritePointerField(AddedStaticFieldsFieldName, value);
    }
}

internal sealed class MockEnCAddedFieldElement : TypedView
{
    private const string NextFieldName = "Next";
    private const string FieldDescFieldName = "FieldDesc";

    public static Layout<MockEnCAddedFieldElement> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("EnCAddedFieldElement", architecture)
            .AddPointerField(NextFieldName)
            // FieldDesc is the address of the embedded EnCFieldDesc payload. We model that
            // payload as a single pointer-sized opaque blob; tests use the FieldDesc field
            // address directly as the returned FieldDesc pointer.
            .AddPointerField(FieldDescFieldName)
            .Build<MockEnCAddedFieldElement>();

    public ulong Next
    {
        get => ReadPointerField(NextFieldName);
        set => WritePointerField(NextFieldName, value);
    }
}

internal sealed class MockEditAndContinueBuilder
{
    private const ulong DefaultAllocationRangeStart = 0x0010_0000;
    private const ulong DefaultAllocationRangeEnd = 0x0020_0000;

    internal MockMemorySpace.Builder Builder { get; }
    internal Layout<MockEnCModule> ModuleLayout { get; }
    internal Layout<MockEnCEEClassData> ClassDataLayout { get; }
    internal Layout<MockEnCAddedFieldElement> AddedFieldElementLayout { get; }

    private readonly MockMemorySpace.BumpAllocator _allocator;

    public MockEditAndContinueBuilder(MockMemorySpace.Builder builder)
        : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
    {
    }

    public MockEditAndContinueBuilder(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Builder = builder;
        _allocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);

        ModuleLayout = MockEnCModule.CreateLayout(builder.TargetTestHelpers.Arch);
        ClassDataLayout = MockEnCEEClassData.CreateLayout(builder.TargetTestHelpers.Arch);
        AddedFieldElementLayout = MockEnCAddedFieldElement.CreateLayout(builder.TargetTestHelpers.Arch);
    }

    /// <summary>
    /// Allocate a Module with the EnC class list pre-populated. <paramref name="classDataEntries"/>
    /// is written into a contiguous pointer-sized array referenced by EnCClassListTable.
    /// </summary>
    public MockEnCModule AddModule(ReadOnlySpan<MockEnCEEClassData> classDataEntries)
    {
        MockEnCModule module = ModuleLayout.Create(_allocator.Allocate((ulong)ModuleLayout.Size, "EnC Module"));
        if (classDataEntries.Length > 0)
        {
            ulong ptrSize = (ulong)Builder.TargetTestHelpers.PointerSize;
            MockMemorySpace.HeapFragment table = _allocator.Allocate(ptrSize * (ulong)classDataEntries.Length, "EnC ClassList table");
            for (int i = 0; i < classDataEntries.Length; i++)
            {
                Span<byte> slot = table.Data.AsSpan((int)(ptrSize * (ulong)i), (int)ptrSize);
                Builder.TargetTestHelpers.WritePointer(slot, classDataEntries[i].Address);
            }
            module.EnCClassListTable = table.Address;
            module.EnCClassListCount = (uint)classDataEntries.Length;
        }
        else
        {
            module.EnCClassListTable = 0;
            module.EnCClassListCount = 0;
        }
        return module;
    }

    public MockEnCEEClassData AddClassData(ulong methodTable)
    {
        MockEnCEEClassData entry = ClassDataLayout.Create(_allocator.Allocate((ulong)ClassDataLayout.Size, "EnCEEClassData"));
        entry.MethodTable = methodTable;
        return entry;
    }

    /// <summary>
    /// Build a linked list of EnCAddedFieldElement nodes whose FieldDesc addresses are the
    /// addresses of the FieldDesc subfield inside each element. Returns the list of element
    /// allocations in the order requested; the head (first element) is element[0].
    /// </summary>
    public IReadOnlyList<MockEnCAddedFieldElement> BuildFieldList(int count)
    {
        if (count <= 0)
            return Array.Empty<MockEnCAddedFieldElement>();
        var elements = new List<MockEnCAddedFieldElement>(count);
        for (int i = 0; i < count; i++)
        {
            MockEnCAddedFieldElement element = AddedFieldElementLayout.Create(
                _allocator.Allocate((ulong)AddedFieldElementLayout.Size, $"EnCAddedFieldElement[{i}]"));
            elements.Add(element);
        }
        // Link them: elements[i].Next = elements[i+1].Address; last.Next = 0
        for (int i = 0; i < count - 1; i++)
        {
            elements[i].Next = elements[i + 1].Address;
        }
        elements[count - 1].Next = 0;
        return elements;
    }

    /// <summary>
    /// Attach an instance-field linked list to <paramref name="classData"/>. Returns the
    /// elements for inspection.
    /// </summary>
    public IReadOnlyList<MockEnCAddedFieldElement> AddInstanceFields(MockEnCEEClassData classData, int count)
    {
        IReadOnlyList<MockEnCAddedFieldElement> elements = BuildFieldList(count);
        classData.AddedInstanceFields = elements.Count > 0 ? elements[0].Address : 0;
        classData.NumAddedInstanceFields = (uint)elements.Count;
        return elements;
    }

    /// <summary>
    /// Attach a static-field linked list to <paramref name="classData"/>. Returns the
    /// elements for inspection.
    /// </summary>
    public IReadOnlyList<MockEnCAddedFieldElement> AddStaticFields(MockEnCEEClassData classData, int count)
    {
        IReadOnlyList<MockEnCAddedFieldElement> elements = BuildFieldList(count);
        classData.AddedStaticFields = elements.Count > 0 ? elements[0].Address : 0;
        classData.NumAddedStaticFields = (uint)elements.Count;
        return elements;
    }
}
