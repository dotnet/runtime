// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct EditAndContinue_1 : IEditAndContinue
{
    private readonly Target _target;

    public EditAndContinue_1(Target target)
    {
        _target = target;
    }

    IEnumerable<TargetPointer> IEditAndContinue.EnumerateAddedFieldDescs(TypeHandle typeHandle, bool staticFields)
    {
        // Only MethodTable type handles can have EnC-added fields. TypeDescs (TypeVar, FnPtr, etc.) cannot.
        if (!typeHandle.IsMethodTable())
            yield break;

        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        ILoader loader = _target.Contracts.Loader;

        TargetPointer modulePtr = rts.GetModule(typeHandle);
        if (modulePtr == TargetPointer.Null)
            yield break;

        ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);
        if (!loader.GetFlags(moduleHandle).HasFlag(ModuleFlags.EditAndContinue))
            yield break;

        // The Module fields backing the EnC class list are only present in builds compiled with
        // FEATURE_METADATA_UPDATER. If absent, there can be no EnC data.
        Target.TypeInfo moduleType = _target.GetTypeInfo(DataType.Module);
        if (!moduleType.Fields.TryGetValue("EnCClassListCount", out Target.FieldInfo countField)
            || !moduleType.Fields.TryGetValue("EnCClassListTable", out Target.FieldInfo tableField))
        {
            yield break;
        }

        uint count = _target.Read<uint>(modulePtr + (ulong)countField.Offset);
        if (count == 0)
            yield break;

        TargetPointer table = _target.ReadPointer(modulePtr + (ulong)tableField.Offset);
        if (table == TargetPointer.Null)
            yield break;

        // Locate the EnCEEClassData entry for this MethodTable.
        TargetPointer mtPtr = typeHandle.Address;
        ulong ptrSize = (ulong)_target.PointerSize;
        TargetPointer classDataPtr = TargetPointer.Null;
        for (uint i = 0; i < count; i++)
        {
            TargetPointer entry = _target.ReadPointer(table + i * ptrSize);
            if (entry == TargetPointer.Null)
                continue;
            Data.EnCEEClassData candidate = _target.ProcessedData.GetOrAdd<Data.EnCEEClassData>(entry);
            if (candidate.MethodTable == mtPtr)
            {
                classDataPtr = entry;
                break;
            }
        }
        if (classDataPtr == TargetPointer.Null)
            yield break;

        Data.EnCEEClassData classData = _target.ProcessedData.GetOrAdd<Data.EnCEEClassData>(classDataPtr);
        TargetPointer node = staticFields ? classData.AddedStaticFields : classData.AddedInstanceFields;
        while (node != TargetPointer.Null)
        {
            Data.EnCAddedFieldElement element = _target.ProcessedData.GetOrAdd<Data.EnCAddedFieldElement>(node);
            yield return element.FieldDesc;
            node = element.Next;
        }
    }
}
