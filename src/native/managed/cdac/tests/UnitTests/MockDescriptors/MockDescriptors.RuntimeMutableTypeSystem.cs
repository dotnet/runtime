// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// A minimal mock of <c>Module</c> that contains only the Edit-and-Continue
/// related fields read by <c>RuntimeMutableTypeSystem_1</c>. The contract uses
/// <c>DataType.Module</c>, so tests register this layout under that key.
/// The class list is modeled as a single embedded <see cref="MockUnorderedArrayBase"/>
/// substructure to mirror the native <c>CUnorderedArray</c>'s
/// <c>CUnorderedArrayBase</c> layout.
/// </summary>
internal sealed class MockEnCModule : TypedView
{
    private const string EnCClassListFieldName = nameof(Data.Module.EnCClassList);

    public static Layout<MockEnCModule> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("Module", architecture)
            // Data.Module is read in full by ProcessedData.GetOrAdd<Module>, which requires
            // every non-nullable [Field] / [FieldAddress] to be present in the layout. The
            // RuntimeMutableTypeSystem tests only inspect EnCClassList, so all other fields
            // are present at zero offsets and left at default (zero) values.
            .AddPointerField(nameof(Data.Module.Assembly))
            .AddPointerField(nameof(Data.Module.PEAssembly))
            .AddUInt32Field(nameof(Data.Module.Flags))
            .AddPointerField(nameof(Data.Module.Base))
            .AddPointerField(nameof(Data.Module.LoaderAllocator))
            .AddPointerField(nameof(Data.Module.DynamicMetadata))
            .AddPointerField(nameof(Data.Module.SimpleName))
            .AddPointerField(nameof(Data.Module.Path))
            .AddPointerField(nameof(Data.Module.FileName))
            .AddPointerField(nameof(Data.Module.ReadyToRunInfo))
            .AddPointerField(nameof(Data.Module.GrowableSymbolStream))
            .AddPointerField(nameof(Data.Module.AvailableTypeParams))
            .AddPointerField(nameof(Data.Module.InstMethodHashTable))
            .AddPointerField(nameof(Data.Module.FieldDefToDescMap))
            .AddPointerField(nameof(Data.Module.ManifestModuleReferencesMap))
            .AddPointerField(nameof(Data.Module.MemberRefToDescMap))
            .AddPointerField(nameof(Data.Module.MethodDefToDescMap))
            .AddPointerField(nameof(Data.Module.TypeDefToMethodTableMap))
            .AddPointerField(nameof(Data.Module.TypeRefToMethodTableMap))
            .AddPointerField(nameof(Data.Module.MethodDefToILCodeVersioningStateMap))
            .AddPointerField(nameof(Data.Module.DynamicILBlobTable))
            .AddField(EnCClassListFieldName, MockUnorderedArrayBase.GetSize(architecture))
            .Build<MockEnCModule>();

    public LayoutField EnCClassListField => Layout.GetField(EnCClassListFieldName);

    public ulong EnCClassListAddress => Address + (ulong)EnCClassListField.Offset;

    public Memory<byte> EnCClassListMemory => Memory.Slice(EnCClassListField.Offset, EnCClassListField.Size);
}

/// <summary>
/// Models the binary layout of <c>CUnorderedArrayWithAllocator&lt;T, N, A&gt;</c>:
/// an entry count, an allocated size, and a pointer to a contiguous array of
/// <c>T</c>. The cdac descriptor (<c>DataType.UnorderedArrayBase</c>) only exposes
/// the count and table; the size field is present here so the pointer falls at
/// the correct (pointer-aligned) offset.
/// </summary>
internal sealed class MockUnorderedArrayBase : TypedView
{
    private const string CountFieldName = "Count";
    private const string SizeFieldName = "Size";
    private const string TableFieldName = "Table";

    public static Layout<MockUnorderedArrayBase> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("UnorderedArrayBase", architecture)
            .AddUInt32Field(CountFieldName)
            .AddUInt32Field(SizeFieldName)
            .AddPointerField(TableFieldName)
            .Build<MockUnorderedArrayBase>();

    public static int GetSize(MockTarget.Architecture architecture)
        => CreateLayout(architecture).Size;

    public uint Count
    {
        get => ReadUInt32Field(CountFieldName);
        set => WriteUInt32Field(CountFieldName, value);
    }

    public uint Size
    {
        get => ReadUInt32Field(SizeFieldName);
        set => WriteUInt32Field(SizeFieldName, value);
    }

    public ulong Table
    {
        get => ReadPointerField(TableFieldName);
        set => WritePointerField(TableFieldName, value);
    }
}

internal sealed class MockEnCEEClassData : TypedView
{
    private const string MethodTableFieldName = "MethodTable";
    private const string AddedInstanceFieldsFieldName = "AddedInstanceFields";
    private const string AddedStaticFieldsFieldName = "AddedStaticFields";

    public static Layout<MockEnCEEClassData> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("EnCEEClassData", architecture)
            .AddPointerField(MethodTableFieldName)
            .AddPointerField(AddedInstanceFieldsFieldName)
            .AddPointerField(AddedStaticFieldsFieldName)
            .Build<MockEnCEEClassData>();

    public ulong MethodTable
    {
        get => ReadPointerField(MethodTableFieldName);
        set => WritePointerField(MethodTableFieldName, value);
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

internal sealed class MockRuntimeMutableTypeSystemBuilder
{
    private const ulong DefaultAllocationRangeStart = 0x0010_0000;
    private const ulong DefaultAllocationRangeEnd = 0x0020_0000;

    internal MockMemorySpace.Builder Builder { get; }
    internal Layout<MockEnCModule> ModuleLayout { get; }
    internal Layout<MockUnorderedArrayBase> UnorderedArrayBaseLayout { get; }
    internal Layout<MockEnCEEClassData> ClassDataLayout { get; }
    internal Layout<MockEnCAddedFieldElement> AddedFieldElementLayout { get; }

    private readonly MockMemorySpace.BumpAllocator _allocator;

    public MockRuntimeMutableTypeSystemBuilder(MockMemorySpace.Builder builder)
        : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
    {
    }

    public MockRuntimeMutableTypeSystemBuilder(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Builder = builder;
        _allocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);

        ModuleLayout = MockEnCModule.CreateLayout(builder.TargetTestHelpers.Arch);
        UnorderedArrayBaseLayout = MockUnorderedArrayBase.CreateLayout(builder.TargetTestHelpers.Arch);
        ClassDataLayout = MockEnCEEClassData.CreateLayout(builder.TargetTestHelpers.Arch);
        AddedFieldElementLayout = MockEnCAddedFieldElement.CreateLayout(builder.TargetTestHelpers.Arch);
    }

    /// <summary>
    /// Allocate a Module with the EnC class list pre-populated. <paramref name="classDataEntries"/>
    /// is written into a contiguous pointer-sized array referenced by the embedded
    /// <c>CUnorderedArrayWithAllocator</c>'s table pointer.
    /// </summary>
    public MockEnCModule AddModule(ReadOnlySpan<MockEnCEEClassData> classDataEntries)
    {
        MockEnCModule module = ModuleLayout.Create(_allocator.Allocate((ulong)ModuleLayout.Size, "EnC Module"));
        MockUnorderedArrayBase classList = UnorderedArrayBaseLayout.Create(module.EnCClassListMemory, module.EnCClassListAddress);
        if (classDataEntries.Length > 0)
        {
            ulong ptrSize = (ulong)Builder.TargetTestHelpers.PointerSize;
            MockMemorySpace.HeapFragment table = _allocator.Allocate(ptrSize * (ulong)classDataEntries.Length, "EnC ClassList table");
            for (int i = 0; i < classDataEntries.Length; i++)
            {
                Span<byte> slot = table.Data.AsSpan((int)(ptrSize * (ulong)i), (int)ptrSize);
                Builder.TargetTestHelpers.WritePointer(slot, classDataEntries[i].Address);
            }
            classList.Table = table.Address;
            classList.Count = (uint)classDataEntries.Length;
            classList.Size = (uint)classDataEntries.Length;
        }
        else
        {
            classList.Table = 0;
            classList.Count = 0;
            classList.Size = 0;
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
        return elements;
    }
}
