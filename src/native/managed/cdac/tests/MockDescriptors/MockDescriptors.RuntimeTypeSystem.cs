// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class MockMethodTable : TypedView
{
    private const string MTFlagsFieldName = nameof(Data.MethodTable.MTFlags);
    private const string BaseSizeFieldName = nameof(Data.MethodTable.BaseSize);
    private const string MTFlags2FieldName = nameof(Data.MethodTable.MTFlags2);
    private const string EEClassOrCanonMTFieldName = nameof(Data.MethodTable.EEClassOrCanonMT);
    private const string ModuleFieldName = nameof(Data.MethodTable.Module);
    private const string ParentMethodTableFieldName = nameof(Data.MethodTable.ParentMethodTable);
    private const string NumInterfacesFieldName = nameof(Data.MethodTable.NumInterfaces);
    private const string NumVirtualsFieldName = nameof(Data.MethodTable.NumVirtuals);
    private const string PerInstInfoFieldName = nameof(Data.MethodTable.PerInstInfo);
    private const string AuxiliaryDataFieldName = nameof(Data.MethodTable.AuxiliaryData);

    public static Layout<MockMethodTable> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("MethodTable", architecture)
            .AddUInt32Field(MTFlagsFieldName)
            .AddUInt32Field(BaseSizeFieldName)
            .AddUInt32Field(MTFlags2FieldName)
            .AddPointerField(EEClassOrCanonMTFieldName)
            .AddPointerField(ModuleFieldName)
            .AddPointerField(ParentMethodTableFieldName)
            .AddUInt16Field(NumInterfacesFieldName)
            .AddUInt16Field(NumVirtualsFieldName)
            .AddPointerField(PerInstInfoFieldName)
            .AddPointerField(AuxiliaryDataFieldName)
            .Build<MockMethodTable>();

    public uint MTFlags
    {
        get => ReadUInt32Field(MTFlagsFieldName);
        set => WriteUInt32Field(MTFlagsFieldName, value);
    }

    public uint BaseSize
    {
        get => ReadUInt32Field(BaseSizeFieldName);
        set => WriteUInt32Field(BaseSizeFieldName, value);
    }

    public uint MTFlags2
    {
        get => ReadUInt32Field(MTFlags2FieldName);
        set => WriteUInt32Field(MTFlags2FieldName, value);
    }

    public ulong EEClassOrCanonMT
    {
        get => ReadPointerField(EEClassOrCanonMTFieldName);
        set => WritePointerField(EEClassOrCanonMTFieldName, value);
    }

    public ulong Module
    {
        get => ReadPointerField(ModuleFieldName);
        set => WritePointerField(ModuleFieldName, value);
    }

    public ulong ParentMethodTable
    {
        get => ReadPointerField(ParentMethodTableFieldName);
        set => WritePointerField(ParentMethodTableFieldName, value);
    }

    public ushort NumInterfaces
    {
        get => ReadUInt16Field(NumInterfacesFieldName);
        set => WriteUInt16Field(NumInterfacesFieldName, value);
    }

    public ushort NumVirtuals
    {
        get => ReadUInt16Field(NumVirtualsFieldName);
        set => WriteUInt16Field(NumVirtualsFieldName, value);
    }

    public ulong PerInstInfo
    {
        get => ReadPointerField(PerInstInfoFieldName);
        set => WritePointerField(PerInstInfoFieldName, value);
    }

    public ulong AuxiliaryData
    {
        get => ReadPointerField(AuxiliaryDataFieldName);
        set => WritePointerField(AuxiliaryDataFieldName, value);
    }
}

internal sealed class MockEEClass : TypedView
{
    private const string MethodTableFieldName = nameof(Data.EEClass.MethodTable);
    private const string MethodDescChunkFieldName = nameof(Data.EEClass.MethodDescChunk);
    private const string CorTypeAttrFieldName = nameof(Data.EEClass.CorTypeAttr);
    private const string NumMethodsFieldName = nameof(Data.EEClass.NumMethods);
    private const string InternalCorElementTypeFieldName = nameof(Data.EEClass.InternalCorElementType);
    private const string NumInstanceFieldsFieldName = nameof(Data.EEClass.NumInstanceFields);
    private const string NumStaticFieldsFieldName = nameof(Data.EEClass.NumStaticFields);
    private const string NumThreadStaticFieldsFieldName = nameof(Data.EEClass.NumThreadStaticFields);
    private const string FieldDescListFieldName = nameof(Data.EEClass.FieldDescList);
    private const string NumNonVirtualSlotsFieldName = nameof(Data.EEClass.NumNonVirtualSlots);

    public static Layout<MockEEClass> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("EEClass", architecture)
            .AddPointerField(MethodTableFieldName)
            .AddPointerField(MethodDescChunkFieldName)
            .AddUInt32Field(CorTypeAttrFieldName)
            .AddUInt16Field(NumMethodsFieldName)
            .AddByteField(InternalCorElementTypeFieldName)
            .AddUInt16Field(NumInstanceFieldsFieldName)
            .AddUInt16Field(NumStaticFieldsFieldName)
            .AddUInt16Field(NumThreadStaticFieldsFieldName)
            .AddPointerField(FieldDescListFieldName)
            .AddUInt16Field(NumNonVirtualSlotsFieldName)
            .Build<MockEEClass>();

    public ulong MethodTable
    {
        get => ReadPointerField(MethodTableFieldName);
        set => WritePointerField(MethodTableFieldName, value);
    }

    public uint CorTypeAttr
    {
        get => ReadUInt32Field(CorTypeAttrFieldName);
        set => WriteUInt32Field(CorTypeAttrFieldName, value);
    }

    public ushort NumMethods
    {
        get => ReadUInt16Field(NumMethodsFieldName);
        set => WriteUInt16Field(NumMethodsFieldName, value);
    }

    public ushort NumNonVirtualSlots
    {
        get => ReadUInt16Field(NumNonVirtualSlotsFieldName);
        set => WriteUInt16Field(NumNonVirtualSlotsFieldName, value);
    }
}

internal sealed class MockMethodTableAuxiliaryData : TypedView
{
    private const string LoaderModuleFieldName = nameof(Data.MethodTableAuxiliaryData.LoaderModule);
    private const string OffsetToNonVirtualSlotsFieldName = nameof(Data.MethodTableAuxiliaryData.OffsetToNonVirtualSlots);
    private const string FlagsFieldName = nameof(Data.MethodTableAuxiliaryData.Flags);

    public static Layout<MockMethodTableAuxiliaryData> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("MethodTableAuxiliaryData", architecture)
            .AddPointerField(LoaderModuleFieldName)
            .AddInt16Field(OffsetToNonVirtualSlotsFieldName)
            .AddUInt32Field(FlagsFieldName)
            .Build<MockMethodTableAuxiliaryData>();

    public ulong LoaderModule
    {
        get => ReadPointerField(LoaderModuleFieldName);
        set => WritePointerField(LoaderModuleFieldName, value);
    }

    public short OffsetToNonVirtualSlots
    {
        get => ReadInt16Field(OffsetToNonVirtualSlotsFieldName);
        set => WriteInt16Field(OffsetToNonVirtualSlotsFieldName, value);
    }

    public uint Flags
    {
        get => ReadUInt32Field(FlagsFieldName);
        set => WriteUInt32Field(FlagsFieldName, value);
    }
}

internal class MockTypeDesc : TypedView
{
    private const string TypeAndFlagsFieldName = nameof(Data.TypeDesc.TypeAndFlags);

    public static Layout<MockTypeDesc> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("TypeDesc", architecture)
            .AddUInt32Field(TypeAndFlagsFieldName)
            .Build<MockTypeDesc>();

    public uint TypeAndFlags
    {
        get => ReadUInt32Field(TypeAndFlagsFieldName);
        set => WriteUInt32Field(TypeAndFlagsFieldName, value);
    }
}

internal sealed class MockFnPtrTypeDesc : MockTypeDesc
{
    private const string NumArgsFieldName = nameof(Data.FnPtrTypeDesc.NumArgs);
    private const string CallConvFieldName = nameof(Data.FnPtrTypeDesc.CallConv);
    private const string LoaderModuleFieldName = nameof(Data.FnPtrTypeDesc.LoaderModule);
    private const string RetAndArgTypesFieldName = nameof(Data.FnPtrTypeDesc.RetAndArgTypes);

    public new static Layout<MockFnPtrTypeDesc> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("FnPtrTypeDesc", architecture, MockTypeDesc.CreateLayout(architecture))
            .AddUInt32Field(NumArgsFieldName)
            .AddUInt32Field(CallConvFieldName)
            .AddPointerField(LoaderModuleFieldName)
            .AddPointerField(RetAndArgTypesFieldName)
            .Build<MockFnPtrTypeDesc>();

    public uint NumArgs
    {
        get => ReadUInt32Field(NumArgsFieldName);
        set => WriteUInt32Field(NumArgsFieldName, value);
    }

    public uint CallConv
    {
        get => ReadUInt32Field(CallConvFieldName);
        set => WriteUInt32Field(CallConvFieldName, value);
    }

    public ulong LoaderModule
    {
        get => ReadPointerField(LoaderModuleFieldName);
        set => WritePointerField(LoaderModuleFieldName, value);
    }

    public ulong this[int index]
    {
        get => ReadPointer(GetFieldSlice(index).Span);
        set => WritePointer(GetFieldSlice(index).Span, value);
    }

    private Memory<byte> GetFieldSlice(int index)
    {
        int pointerSize = Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        LayoutField field = Layout.GetField(RetAndArgTypesFieldName);
        int offset = field.Offset + (index * pointerSize);
        return Memory.Slice(offset, pointerSize);
    }
}

internal sealed class MockParamTypeDesc : MockTypeDesc
{
    private const string TypeArgFieldName = nameof(Data.ParamTypeDesc.TypeArg);

    public new static Layout<MockParamTypeDesc> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("ParamTypeDesc", architecture, MockTypeDesc.CreateLayout(architecture))
            .AddPointerField(TypeArgFieldName)
            .Build<MockParamTypeDesc>();

    public ulong TypeArg
    {
        get => ReadPointerField(TypeArgFieldName);
        set => WritePointerField(TypeArgFieldName, value);
    }
}

internal sealed class MockTypeVarTypeDesc : MockTypeDesc
{
    private const string ModuleFieldName = nameof(Data.TypeVarTypeDesc.Module);
    private const string TokenFieldName = nameof(Data.TypeVarTypeDesc.Token);

    public new static Layout<MockTypeVarTypeDesc> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("TypeVarTypeDesc", architecture, MockTypeDesc.CreateLayout(architecture))
            .AddPointerField(ModuleFieldName)
            .AddUInt32Field(TokenFieldName)
            .Build<MockTypeVarTypeDesc>();

    public ulong Module
    {
        get => ReadPointerField(ModuleFieldName);
        set => WritePointerField(ModuleFieldName, value);
    }

    public uint Token
    {
        get => ReadUInt32Field(TokenFieldName);
        set => WriteUInt32Field(TokenFieldName, value);
    }
}

internal partial class MockDescriptors
{
    public class RuntimeTypeSystem
    {
        internal const ulong TestFreeObjectMethodTableGlobalAddress = 0x00000000_7a0000a0;
        internal const ulong TestContinuationMethodTableGlobalAddress = 0x00000000_7a0000b0;

        private const ulong DefaultAllocationRangeStart = 0x00000000_4a000000;
        private const ulong DefaultAllocationRangeEnd = 0x00000000_4b000000;

        internal static uint GetMethodDescAlignment(TargetTestHelpers helpers) => helpers.Arch.Is64Bit ? 8u : 4u;

        internal readonly MockMemorySpace.Builder Builder;
        internal MockMemorySpace.BumpAllocator TypeSystemAllocator { get; }

        internal Layout<MockMethodTable> MethodTableLayout { get; }
        internal Layout<MockEEClass> EEClassLayout { get; }
        internal Layout<MockMethodTableAuxiliaryData> MethodTableAuxiliaryDataLayout { get; }
        internal Layout<MockTypeDesc> TypeDescLayout { get; }
        internal Layout<MockFnPtrTypeDesc> FnPtrTypeDescLayout { get; }
        internal Layout<MockParamTypeDesc> ParamTypeDescLayout { get; }
        internal Layout<MockTypeVarTypeDesc> TypeVarTypeDescLayout { get; }
        internal Layout<MockGCCoverageInfo> GCCoverageInfoLayout { get; }

        internal MockEEClass SystemObjectEEClass { get; private set; } = null!;
        internal MockMethodTable SystemObjectMethodTable { get; private set; } = null!;
        internal MockEEClass ContinuationEEClass { get; private set; } = null!;
        internal MockMethodTable ContinuationMethodTable { get; private set; } = null!;

        internal ulong FreeObjectMethodTableAddress { get; private set; }
        internal ulong FreeObjectMethodTableGlobalAddress => TestFreeObjectMethodTableGlobalAddress;
        internal ulong ContinuationMethodTableGlobalAddress => TestContinuationMethodTableGlobalAddress;
        internal ulong MethodDescAlignment => GetMethodDescAlignment(Builder.TargetTestHelpers);
        internal ulong ArrayBaseSize => Builder.TargetTestHelpers.ArrayBaseBaseSize;
        // sizeof(ContinuationObject) = sizeof(MT*) + sizeof(Next*) + sizeof(Resume*) + sizeof(Flags) + sizeof(State)
        internal uint ContinuationObjectSize => 3u * (uint)Builder.TargetTestHelpers.PointerSize + 8;

        public RuntimeTypeSystem(MockMemorySpace.Builder builder)
            : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        {
        }

        public RuntimeTypeSystem(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
        {
            Builder = builder;
            TypeSystemAllocator = builder.CreateAllocator(allocationRange.Start, allocationRange.End);

            MethodTableLayout = MockMethodTable.CreateLayout(Builder.TargetTestHelpers.Arch);
            EEClassLayout = MockEEClass.CreateLayout(Builder.TargetTestHelpers.Arch);
            MethodTableAuxiliaryDataLayout = MockMethodTableAuxiliaryData.CreateLayout(Builder.TargetTestHelpers.Arch);
            TypeDescLayout = MockTypeDesc.CreateLayout(Builder.TargetTestHelpers.Arch);
            FnPtrTypeDescLayout = MockFnPtrTypeDesc.CreateLayout(Builder.TargetTestHelpers.Arch);
            ParamTypeDescLayout = MockParamTypeDesc.CreateLayout(Builder.TargetTestHelpers.Arch);
            TypeVarTypeDescLayout = MockTypeVarTypeDesc.CreateLayout(Builder.TargetTestHelpers.Arch);
            GCCoverageInfoLayout = MockGCCoverageInfo.CreateLayout(Builder.TargetTestHelpers.Arch);

            AddGlobalPointers();
            AddDefaultTypes();
        }

        private void AddGlobalPointers()
        {
            AddFreeObjectMethodTable();
            AddContinuationMethodTableGlobal();
        }

        private void AddDefaultTypes()
        {
            AddSystemObjectType();
            AddContinuationType();
        }

        private void AddFreeObjectMethodTable()
        {
            MockMethodTable freeObjectMethodTable = AddMethodTable("Free Object Method Table");
            FreeObjectMethodTableAddress = freeObjectMethodTable.Address;
            AddPointerGlobal("Address of Free Object Method Table", TestFreeObjectMethodTableGlobalAddress, FreeObjectMethodTableAddress);
        }

        private void AddContinuationMethodTableGlobal()
        {
            AddPointerGlobal("Address of Continuation Method Table", TestContinuationMethodTableGlobalAddress, 0);
        }

        private void AddSystemObjectType()
        {
            const int NumMethods = 8;
            const int NumVirtuals = 3;

            SystemObjectEEClass = AddEEClass("System.Object");
            SystemObjectEEClass.CorTypeAttr = (uint)(System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class);
            SystemObjectEEClass.NumMethods = NumMethods;

            SystemObjectMethodTable = AddMethodTable("System.Object");
            SystemObjectMethodTable.BaseSize = Builder.TargetTestHelpers.ObjectBaseSize;
            SystemObjectMethodTable.NumVirtuals = NumVirtuals;
            SystemObjectEEClass.MethodTable = SystemObjectMethodTable.Address;
            SystemObjectMethodTable.EEClassOrCanonMT = SystemObjectEEClass.Address;
        }

        private void AddContinuationType()
        {
            ContinuationEEClass = AddEEClass("Continuation");

            ContinuationMethodTable = AddMethodTable("Continuation");
            ContinuationMethodTable.BaseSize = Builder.TargetTestHelpers.ObjectBaseSize;
            ContinuationMethodTable.ParentMethodTable = SystemObjectMethodTable.Address;
            ContinuationMethodTable.NumVirtuals = 3;
            ContinuationEEClass.MethodTable = ContinuationMethodTable.Address;
            ContinuationMethodTable.EEClassOrCanonMT = ContinuationEEClass.Address;
            SetContinuationMethodTable(ContinuationMethodTable.Address);
        }

        private void AddPointerGlobal(string name, ulong address, ulong value)
        {
            TargetTestHelpers targetTestHelpers = Builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment global = new()
            {
                Name = name,
                Address = address,
                Data = new byte[targetTestHelpers.PointerSize]
            };
            targetTestHelpers.WritePointer(global.Data, value);
            Builder.AddHeapFragment(global);
        }

        internal void SetContinuationMethodTable(ulong continuationMethodTable)
        {
            Span<byte> globalAddrBytes = Builder.BorrowAddressRange(TestContinuationMethodTableGlobalAddress, Builder.TargetTestHelpers.PointerSize);
            Builder.TargetTestHelpers.WritePointer(globalAddrBytes, continuationMethodTable);
        }

        internal MockEEClass AddEEClass(string name)
            => Add(EEClassLayout, $"EEClass '{name}'");

        internal MockMethodTable AddMethodTable(string name)
            => Add(MethodTableLayout, $"MethodTable '{name}'");

        internal MockMethodTableAuxiliaryData AddMethodTableAuxiliaryData()
        {
            MockMethodTableAuxiliaryData auxData = Add(MethodTableAuxiliaryDataLayout, "MethodTableAuxiliaryData");
            return auxData;
        }

        internal MockFnPtrTypeDesc AddFunctionPointerTypeDesc(int retAndArgTypeCount)
        {
            TargetTestHelpers helpers = Builder.TargetTestHelpers;
            ulong size = (ulong)(FnPtrTypeDescLayout.Size + (retAndArgTypeCount * helpers.PointerSize));
            return Add(FnPtrTypeDescLayout, size, "FnPtrTypeDesc");
        }

        internal MockParamTypeDesc AddParamTypeDesc()
            => Add(ParamTypeDescLayout, "ParamTypeDesc");

        internal MockTypeVarTypeDesc AddTypeVarTypeDesc()
            => Add(TypeVarTypeDescLayout, "TypeVarTypeDesc");

        private TView Add<TView>(Layout<TView> layout, string name)
            where TView : TypedView, new()
            => Add(layout, (ulong)layout.Size, name);

        private TView Add<TView>(Layout<TView> layout, ulong size, string name)
            where TView : TypedView, new()
        {
            MockMemorySpace.HeapFragment fragment = TypeSystemAllocator.Allocate(size, name);
            return layout.Create(fragment.Data.AsMemory(), fragment.Address);
        }

        /// <summary>
        /// Allocates a method table together with a CGCDesc immediately before it in memory.
        /// </summary>
        /// <param name="name">Descriptive name for the allocation.</param>
        /// <param name="baseSize">Value to store in the method table's <c>BaseSize</c> field.</param>
        /// <param name="series">
        /// GC descriptor series ordered from highest to lowest
        /// (matching <c>CGCDesc::GetHighestSeries</c> down to <c>GetLowestSeries</c>).
        /// Each entry is <c>(Size, Offset)</c> – the raw field values stored in the
        /// <c>CGCDescSeries</c> struct (i.e. <c>seriessize</c> already has <c>BaseSize</c> subtracted).
        /// </param>
        /// <returns>
        /// A <see cref="MockMethodTable"/> whose address is <em>after</em> the GCDesc bytes.
        /// The caller is responsible for setting additional method table flags (e.g.
        /// <c>ContainsGCPointers = 0x01000000</c>) and linking to an EEClass.
        /// </returns>
        internal MockMethodTable AddMethodTableWithGCDesc(string name, uint baseSize, (ulong Size, ulong Offset)[] series)
        {
            int pointerSize = Builder.TargetTestHelpers.PointerSize;

            // GCDesc layout (each slot is pointer-sized):
            //   [ series[N-1].seriessize ] [ series[N-1].startoffset ]  <- lowest series (fragment start)
            //   ...
            //   [ series[0].seriessize   ] [ series[0].startoffset   ]  <- highest series (MT - 3*ptrSize)
            //   [ NumSeries              ]                               <- MT - 1*ptrSize
            //   [ MethodTable data starts here ]
            int gcDescSize = (1 + 2 * series.Length) * pointerSize;
            int totalSize = gcDescSize + MethodTableLayout.Size;

            MockMemorySpace.HeapFragment fragment = TypeSystemAllocator.Allocate((ulong)totalSize, $"GCDesc+MethodTable '{name}'");

            // Write series entries. The highest series (index 0) lives closest to the MT (highest address),
            // and the lowest series (index N-1) lives farthest from the MT (lowest address).
            // So series[i] goes at fragment offset (N-1-i)*2*pointerSize.
            for (int i = 0; i < series.Length; i++)
            {
                int seriesBase = (series.Length - 1 - i) * 2 * pointerSize;
                Builder.TargetTestHelpers.WritePointer(fragment.Data.AsSpan(seriesBase, pointerSize), series[i].Size);
                Builder.TargetTestHelpers.WritePointer(fragment.Data.AsSpan(seriesBase + pointerSize, pointerSize), series[i].Offset);
            }

            // Write NumSeries immediately before the MT
            Builder.TargetTestHelpers.WritePointer(
                fragment.Data.AsSpan(series.Length * 2 * pointerSize, pointerSize),
                (ulong)series.Length);

            // The MockMethodTable lives at offset gcDescSize within the combined fragment
            ulong mtAddress = fragment.Address + (ulong)gcDescSize;
            MockMethodTable mt = MethodTableLayout.Create(fragment.Data.AsMemory(gcDescSize, MethodTableLayout.Size), mtAddress);
            mt.BaseSize = baseSize;
            return mt;
        }

        /// <summary>
        /// Allocates a method table together with a value-class (repeating) CGCDesc immediately before it.
        /// </summary>
        /// <param name="name">Descriptive name for the allocation.</param>
        /// <param name="baseSize">Value to store in the method table's <c>BaseSize</c> field.</param>
        /// <param name="startOffset">The <c>startoffset</c> field of the CGCDescSeries (offset from object start).</param>
        /// <param name="valSeries">
        /// The <c>val_serie_item</c> entries, each as <c>(nptrs, skip)</c>.
        /// Index 0 corresponds to <c>val_serie[0]</c> (overlapping <c>seriessize</c> in the union).
        /// </param>
        /// <returns>
        /// A <see cref="MockMethodTable"/> whose address is <em>after</em> the GCDesc bytes.
        /// The caller is responsible for setting additional method table flags (e.g.
        /// <c>ContainsGCPointers = 0x01000000</c>) and linking to an EEClass.
        /// </returns>
        internal MockMethodTable AddMethodTableWithValueClassGCDesc(string name, uint baseSize, ulong startOffset, (uint Nptrs, uint Skip)[] valSeries)
        {
            TargetTestHelpers helpers = Builder.TargetTestHelpers;
            int pointerSize = helpers.PointerSize;
            int halfSize = pointerSize / 2;

            // Value-class GCDesc layout (each slot is pointer-sized unless noted):
            //   [ val_serie[N-1] ]    <- lowest address (fragment start)
            //   ...
            //   [ val_serie[0]   ]    <- overlaps seriessize in CGCDescSeries union
            //   [ startoffset    ]    <- one ptrSize slot
            //   [ NumSeries (-N) ]    <- one ptrSize slot (negative)
            //   [ MethodTable data starts here ]
            //
            // ComputeSizeRepeating = sizeof(size_t) + sizeof(CGCDescSeries) + (N-1)*sizeof(val_serie_item)
            //                      = ptrSize + 2*ptrSize + (N-1)*ptrSize = (N+2)*ptrSize
            int gcDescSize = (valSeries.Length + 2) * pointerSize;
            int totalSize = gcDescSize + MethodTableLayout.Size;

            MockMemorySpace.HeapFragment fragment = TypeSystemAllocator.Allocate((ulong)totalSize, $"ValueClassGCDesc+MethodTable '{name}'");

            // Write val_serie items. val_serie[0] is closest to MT (highest address in the GCDesc region),
            // val_serie[N-1] is farthest (lowest address).
            // Each val_serie_item is { HALF_SIZE_T nptrs; HALF_SIZE_T skip; }
            for (int i = 0; i < valSeries.Length; i++)
            {
                int itemOffset = (valSeries.Length - 1 - i) * pointerSize;
                if (pointerSize == sizeof(uint))
                {
                    helpers.Write(fragment.Data.AsSpan(itemOffset, halfSize), (ushort)valSeries[i].Nptrs);
                    helpers.Write(fragment.Data.AsSpan(itemOffset + halfSize, halfSize), (ushort)valSeries[i].Skip);
                }
                else
                {
                    helpers.Write(fragment.Data.AsSpan(itemOffset, halfSize), valSeries[i].Nptrs);
                    helpers.Write(fragment.Data.AsSpan(itemOffset + halfSize, halfSize), valSeries[i].Skip);
                }
            }

            // Write startoffset
            helpers.WritePointer(fragment.Data.AsSpan(valSeries.Length * pointerSize, pointerSize), startOffset);

            // Write NumSeries as a negative value (-N)
            long negativeCount = -valSeries.Length;
            if (pointerSize == sizeof(uint))
            {
                helpers.Write(fragment.Data.AsSpan((valSeries.Length + 1) * pointerSize, pointerSize), (int)negativeCount);
            }
            else
            {
                helpers.WritePointer(fragment.Data.AsSpan((valSeries.Length + 1) * pointerSize, pointerSize), unchecked((ulong)negativeCount));
            }

            // The MockMethodTable lives at offset gcDescSize within the combined fragment
            ulong mtAddress = fragment.Address + (ulong)gcDescSize;
            MockMethodTable mt = MethodTableLayout.Create(fragment.Data.AsMemory(gcDescSize, MethodTableLayout.Size), mtAddress);
            mt.BaseSize = baseSize;
            return mt;
        }
    }
}
