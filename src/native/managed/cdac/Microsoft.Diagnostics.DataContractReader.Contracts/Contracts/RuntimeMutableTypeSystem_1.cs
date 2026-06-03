// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct RuntimeMutableTypeSystem_1 : IRuntimeMutableTypeSystem
{
    private readonly Target _target;

    public RuntimeMutableTypeSystem_1(Target target)
    {
        _target = target;
    }

    internal enum FieldDescFlags2 : uint
    {
        OffsetMask = 0x07ffffff,
    }
    bool IRuntimeMutableTypeSystem.IsFieldDescEnCNew(TargetPointer fieldDescPointer)
    {
        Data.FieldDesc fieldDesc = _target.ProcessedData.GetOrAdd<Data.FieldDesc>(fieldDescPointer);
        uint offset = fieldDesc.DWord2 & (uint)FieldDescFlags2.OffsetMask;
        return offset == _target.ReadGlobal<uint>(Constants.Globals.FieldOffsetNewEnc);
    }

    IEnumerable<TargetPointer> IRuntimeMutableTypeSystem.EnumerateAddedFieldDescs(TypeHandle typeHandle, bool staticFields)
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

        // The Module field backing the EnC class list is only present in builds compiled with
        // FEATURE_METADATA_UPDATER. If absent, there can be no EnC data.
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(modulePtr);
        if (module.EnCClassList is not TargetPointer classListAddr)
            yield break;

        Data.UnorderedArrayBase classList = _target.ProcessedData.GetOrAdd<Data.UnorderedArrayBase>(classListAddr);
        if (classList.Count == 0 || classList.Table == TargetPointer.Null)
            yield break;

        // Locate the EnCEEClassData entry for this MethodTable.
        TargetPointer mtPtr = typeHandle.Address;
        ulong ptrSize = (ulong)_target.PointerSize;
        TargetPointer classDataPtr = TargetPointer.Null;
        for (uint i = 0; i < classList.Count; i++)
        {
            TargetPointer entry = _target.ReadPointer(classList.Table + i * ptrSize);
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
