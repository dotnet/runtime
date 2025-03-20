// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    internal record TypeFields
    {
        public DataType DataType;
        public TargetTestHelpers.Field[] Fields;
        public TypeFields BaseTypeFields;
    }

    private static readonly TypeFields MethodTableFields = new TypeFields()
    {
        DataType = DataType.MethodTable,
        Fields =
        [
            new(nameof(Data.MethodTable.MTFlags), DataType.uint32),
            new(nameof(Data.MethodTable.BaseSize), DataType.uint32),
            new(nameof(Data.MethodTable.MTFlags2), DataType.uint32),
            new(nameof(Data.MethodTable.EEClassOrCanonMT), DataType.nuint),
            new(nameof(Data.MethodTable.Module), DataType.pointer),
            new(nameof(Data.MethodTable.ParentMethodTable), DataType.pointer),
            new(nameof(Data.MethodTable.NumInterfaces), DataType.uint16),
            new(nameof(Data.MethodTable.NumVirtuals), DataType.uint16),
            new(nameof(Data.MethodTable.PerInstInfo), DataType.pointer),
            new(nameof(Data.MethodTable.AuxiliaryData), DataType.pointer),
        ]
    };

    private static readonly TypeFields EEClassFields = new TypeFields()
    {
        DataType = DataType.EEClass,
        Fields =
        [
            new(nameof(Data.EEClass.MethodTable), DataType.pointer),
            new(nameof(Data.EEClass.CorTypeAttr), DataType.uint32),
            new(nameof(Data.EEClass.NumMethods), DataType.uint16),
            new(nameof(Data.EEClass.InternalCorElementType), DataType.uint8),
            new(nameof(Data.EEClass.NumNonVirtualSlots), DataType.uint16),
        ]
    };

    private static readonly TypeFields MethodTableAuxiliaryDataFields = new TypeFields()
    {
        DataType = DataType.MethodTableAuxiliaryData,
        Fields =
        [
            new(nameof(Data.MethodTableAuxiliaryData.LoaderModule), DataType.pointer),
            new(nameof(Data.MethodTableAuxiliaryData.OffsetToNonVirtualSlots), DataType.int16),
        ]
    };

    private static readonly TypeFields ArrayClassFields = new TypeFields()
    {
        DataType = DataType.ArrayClass,
        Fields =
        [
            new(nameof(Data.ArrayClass.Rank), DataType.uint8),
        ],
        BaseTypeFields = EEClassFields
    };

    private static readonly TypeFields ObjectFields = new TypeFields()
    {
        DataType = DataType.Object,
        Fields =
        [
            new("m_pMethTab", DataType.pointer),
        ]
    };

    private static readonly TypeFields StringFields = new TypeFields()
    {
        DataType = DataType.String,
        Fields =
        [
            new("m_StringLength", DataType.uint32),
            new("m_FirstChar", DataType.uint16),
        ],
        BaseTypeFields = ObjectFields
    };

    private static readonly TypeFields ArrayFields = new TypeFields()
    {
        DataType = DataType.Array,
        Fields =
        [
            new("m_NumComponents", DataType.uint32),
        ],
        BaseTypeFields = ObjectFields
    };

    private static readonly TypeFields SyncTableEntryFields = new TypeFields()
    {
        DataType = DataType.SyncTableEntry,
        Fields =
        [
            new(nameof(Data.SyncTableEntry.SyncBlock), DataType.pointer),
        ]
    };

    private static readonly TypeFields SyncBlockFields = new TypeFields()
    {
        DataType = DataType.SyncBlock,
        Fields =
        [
            new(nameof(Data.SyncBlock.InteropInfo), DataType.pointer),
        ]
    };

    private static readonly TypeFields InteropSyncBlockFields = new TypeFields()
    {
        DataType = DataType.InteropSyncBlockInfo,
        Fields =
        [
            new(nameof(Data.InteropSyncBlockInfo.RCW), DataType.pointer),
            new(nameof(Data.InteropSyncBlockInfo.CCW), DataType.pointer),
        ]
    };

    internal static readonly TypeFields ModuleFields = new TypeFields()
    {
        DataType = DataType.Module,
        Fields =
        [
            new(nameof(Data.Module.Assembly), DataType.pointer),
            new(nameof(Data.Module.Flags), DataType.uint32),
            new(nameof(Data.Module.Base), DataType.pointer),
            new(nameof(Data.Module.LoaderAllocator), DataType.pointer),
            new(nameof(Data.Module.DynamicMetadata), DataType.pointer),
            new(nameof(Data.Module.Path), DataType.pointer),
            new(nameof(Data.Module.FileName), DataType.pointer),
            new(nameof(Data.Module.FieldDefToDescMap), DataType.pointer),
            new(nameof(Data.Module.ManifestModuleReferencesMap), DataType.pointer),
            new(nameof(Data.Module.MemberRefToDescMap), DataType.pointer),
            new(nameof(Data.Module.MethodDefToDescMap), DataType.pointer),
            new(nameof(Data.Module.TypeDefToMethodTableMap), DataType.pointer),
            new(nameof(Data.Module.TypeRefToMethodTableMap), DataType.pointer),
            new(nameof(Data.Module.MethodDefToILCodeVersioningStateMap), DataType.pointer),
            new(nameof(Data.Module.ReadyToRunInfo), DataType.pointer),
        ]
    };

    private static readonly TypeFields AssemblyFields = new TypeFields()
    {
        DataType = DataType.Assembly,
        Fields =
        [
            new(nameof(Data.Assembly.IsCollectible), DataType.uint8),
        ]
    };

    private static readonly TypeFields ExceptionInfoFields = new TypeFields()
    {
        DataType = DataType.ExceptionInfo,
        Fields =
        [
            new(nameof(Data.ExceptionInfo.PreviousNestedInfo), DataType.pointer),
            new(nameof(Data.ExceptionInfo.ThrownObject), DataType.pointer),
        ]
    };

    private static readonly TypeFields ThreadFields = new TypeFields()
    {
        DataType = DataType.Thread,
        Fields =
        [
            new(nameof(Data.Thread.Id), DataType.uint32),
            new(nameof(Data.Thread.OSId), DataType.nuint),
            new(nameof(Data.Thread.State), DataType.uint32),
            new(nameof(Data.Thread.PreemptiveGCDisabled), DataType.uint32),
            new(nameof(Data.Thread.RuntimeThreadLocals), DataType.pointer),
            new(nameof(Data.Thread.Frame), DataType.pointer),
            new(nameof(Data.Thread.TEB), DataType.pointer),
            new(nameof(Data.Thread.LastThrownObject), DataType.pointer),
            new(nameof(Data.Thread.LinkNext), DataType.pointer),
            new(nameof(Data.Thread.ExceptionTracker), DataType.pointer),
        ]
    };

    private static readonly TypeFields ThreadStoreFields = new TypeFields()
    {
        DataType = DataType.ThreadStore,
        Fields =
        [
            new(nameof(Data.ThreadStore.ThreadCount), DataType.uint32),
            new(nameof(Data.ThreadStore.FirstThreadLink), DataType.pointer),
            new(nameof(Data.ThreadStore.UnstartedCount), DataType.uint32),
            new(nameof(Data.ThreadStore.BackgroundCount), DataType.uint32),
            new(nameof(Data.ThreadStore.PendingCount), DataType.uint32),
            new(nameof(Data.ThreadStore.DeadCount), DataType.uint32),
        ]
    };

    private static readonly TypeFields GCCoverageInfoFields = new TypeFields()
    {
        DataType = DataType.GCCoverageInfo,
        Fields =
        [
            // Add DummyField to ensure the offset of SavedCode is not added to the TargetPointer.Null
            new("DummyField", DataType.pointer),
            new("SavedCode", DataType.pointer),
        ]
    };

    internal static Dictionary<DataType, Target.TypeInfo> GetTypesForTypeFields(TargetTestHelpers helpers, TypeFields[] typeFields)
    {
        Dictionary<DataType, Target.TypeInfo> types = new();
        foreach (var toAdd in typeFields)
        {
            TargetTestHelpers.LayoutResult layout = GetLayout(helpers, toAdd);
            types[toAdd.DataType] = new Target.TypeInfo()
            {
                Fields = layout.Fields,
                Size = layout.Stride,
            };
        }
        return types;

        static TargetTestHelpers.LayoutResult GetLayout(TargetTestHelpers helpers, TypeFields typeFields)
        {
            return typeFields.BaseTypeFields == null
                ? helpers.LayoutFields(typeFields.Fields)
                : helpers.ExtendLayout(typeFields.Fields, GetLayout(helpers, typeFields.BaseTypeFields));
        }
    }
}
