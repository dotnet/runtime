// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

internal partial class MockDescriptors
{
    private record TypeFields
    {
        public DataType DataType;
        public (string Name, DataType Type)[] Fields;
        public TypeFields BaseTypeFields;
    }

    private static readonly TypeFields MethodTableFields = new TypeFields()
    {
        DataType = DataType.MethodTable,
        Fields =
        [
            (nameof(Data.MethodTable.MTFlags), DataType.uint32),
            (nameof(Data.MethodTable.BaseSize), DataType.uint32),
            (nameof(Data.MethodTable.MTFlags2), DataType.uint32),
            (nameof(Data.MethodTable.EEClassOrCanonMT), DataType.nuint),
            (nameof(Data.MethodTable.Module), DataType.pointer),
            (nameof(Data.MethodTable.ParentMethodTable), DataType.pointer),
            (nameof(Data.MethodTable.NumInterfaces), DataType.uint16),
            (nameof(Data.MethodTable.NumVirtuals), DataType.uint16),
            (nameof(Data.MethodTable.PerInstInfo), DataType.pointer),
            (nameof(Data.MethodTable.AuxiliaryData), DataType.pointer),
        ]
    };

    private static readonly TypeFields EEClassFields = new TypeFields()
    {
        DataType = DataType.EEClass,
        Fields =
        [
            (nameof(Data.EEClass.MethodTable), DataType.pointer),
            (nameof(Data.EEClass.CorTypeAttr), DataType.uint32),
            (nameof(Data.EEClass.NumMethods), DataType.uint16),
            (nameof(Data.EEClass.InternalCorElementType), DataType.uint8),
            (nameof(Data.EEClass.NumNonVirtualSlots), DataType.uint16),
        ]
    };

    private static readonly TypeFields MethodTableAuxiliaryDataFields = new TypeFields()
    {
        DataType = DataType.MethodTableAuxiliaryData,
        Fields =
        [
            (nameof(Data.MethodTableAuxiliaryData.LoaderModule), DataType.pointer),
            (nameof(Data.MethodTableAuxiliaryData.OffsetToNonVirtualSlots), DataType.int16),
        ]
    };

    private static readonly TypeFields ArrayClassFields = new TypeFields()
    {
        DataType = DataType.ArrayClass,
        Fields =
        [
            (nameof(Data.ArrayClass.Rank), DataType.uint8),
        ],
        BaseTypeFields = EEClassFields
    };

    private static readonly TypeFields ObjectFields = new TypeFields()
    {
        DataType = DataType.Object,
        Fields =
        [
            ("m_pMethTab", DataType.pointer),
        ]
    };

    private static readonly TypeFields StringFields = new TypeFields()
    {
        DataType = DataType.String,
        Fields =
        [
            ("m_StringLength", DataType.uint32),
            ("m_FirstChar", DataType.uint16),
        ],
        BaseTypeFields = ObjectFields
    };

    private static readonly TypeFields ArrayFields = new TypeFields()
    {
        DataType = DataType.Array,
        Fields =
        [
            ("m_NumComponents", DataType.uint32),
        ],
        BaseTypeFields = ObjectFields
    };

    private static readonly TypeFields SyncTableEntryFields = new TypeFields()
    {
        DataType = DataType.SyncTableEntry,
        Fields =
        [
            (nameof(Data.SyncTableEntry.SyncBlock), DataType.pointer),
        ]
    };

    private static readonly TypeFields SyncBlockFields = new TypeFields()
    {
        DataType = DataType.SyncBlock,
        Fields =
        [
            (nameof(Data.SyncBlock.InteropInfo), DataType.pointer),
        ]
    };

    private static readonly TypeFields InteropSyncBlockFields = new TypeFields()
    {
        DataType = DataType.InteropSyncBlockInfo,
        Fields =
        [
            (nameof(Data.InteropSyncBlockInfo.RCW), DataType.pointer),
            (nameof(Data.InteropSyncBlockInfo.CCW), DataType.pointer),
        ]
    };

    private static readonly TypeFields ModuleFields = new TypeFields()
    {
        DataType = DataType.Module,
        Fields =
        [
            (nameof(Data.Module.Assembly), DataType.pointer),
            (nameof(Data.Module.Flags), DataType.uint32),
            (nameof(Data.Module.Base), DataType.pointer),
            (nameof(Data.Module.LoaderAllocator), DataType.pointer),
            (nameof(Data.Module.ThunkHeap), DataType.pointer),
            (nameof(Data.Module.DynamicMetadata), DataType.pointer),
            (nameof(Data.Module.Path), DataType.pointer),
            (nameof(Data.Module.FileName), DataType.pointer),
            (nameof(Data.Module.FieldDefToDescMap), DataType.pointer),
            (nameof(Data.Module.ManifestModuleReferencesMap), DataType.pointer),
            (nameof(Data.Module.MemberRefToDescMap), DataType.pointer),
            (nameof(Data.Module.MethodDefToDescMap), DataType.pointer),
            (nameof(Data.Module.TypeDefToMethodTableMap), DataType.pointer),
            (nameof(Data.Module.TypeRefToMethodTableMap), DataType.pointer),
            (nameof(Data.Module.MethodDefToILCodeVersioningStateMap), DataType.pointer),
        ]
    };

    private static readonly TypeFields AssemblyFields = new TypeFields()
    {
        DataType = DataType.Assembly,
        Fields =
        [
            (nameof(Data.Assembly.IsCollectible), DataType.uint8),
        ]
    };

    private static readonly TypeFields ExceptionInfoFields = new TypeFields()
    {
        DataType = DataType.ExceptionInfo,
        Fields =
        [
            (nameof(Data.ExceptionInfo.PreviousNestedInfo), DataType.pointer),
            (nameof(Data.ExceptionInfo.ThrownObject), DataType.pointer),
        ]
    };

    private static readonly TypeFields ThreadFields = new TypeFields()
    {
        DataType = DataType.Thread,
        Fields =
        [
            (nameof(Data.Thread.Id), DataType.uint32),
            (nameof(Data.Thread.OSId), DataType.nuint),
            (nameof(Data.Thread.State), DataType.uint32),
            (nameof(Data.Thread.PreemptiveGCDisabled), DataType.uint32),
            (nameof(Data.Thread.RuntimeThreadLocals), DataType.pointer),
            (nameof(Data.Thread.Frame), DataType.pointer),
            (nameof(Data.Thread.TEB), DataType.pointer),
            (nameof(Data.Thread.LastThrownObject), DataType.pointer),
            (nameof(Data.Thread.LinkNext), DataType.pointer),
            (nameof(Data.Thread.ExceptionTracker), DataType.pointer),
        ]
    };

    private static readonly TypeFields ThreadStoreFields = new TypeFields()
    {
        DataType = DataType.ThreadStore,
        Fields =
        [
            (nameof(Data.ThreadStore.ThreadCount), DataType.uint32),
            (nameof(Data.ThreadStore.FirstThreadLink), DataType.pointer),
            (nameof(Data.ThreadStore.UnstartedCount), DataType.uint32),
            (nameof(Data.ThreadStore.BackgroundCount), DataType.uint32),
            (nameof(Data.ThreadStore.PendingCount), DataType.uint32),
            (nameof(Data.ThreadStore.DeadCount), DataType.uint32),
        ]
    };

    private static Dictionary<DataType, Target.TypeInfo> GetTypesForTypeFields(TargetTestHelpers helpers, TypeFields[] typeFields)
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
