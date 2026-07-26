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

    IEnumerable<TargetPointer> IRuntimeMutableTypeSystem.EnumerateAddedFieldDescs(ITypeHandle typeHandle, bool staticFields)
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

    bool IRuntimeMutableTypeSystem.DoesEnCFieldDescNeedFixup(TargetPointer encFieldDescPointer)
    {
        Data.EnCFieldDesc encFieldDesc = _target.ProcessedData.GetOrAdd<Data.EnCFieldDesc>(encFieldDescPointer);
        return encFieldDesc.NeedsFixup != 0;
    }

    TargetPointer IRuntimeMutableTypeSystem.GetEnCStaticFieldDataAddress(TargetPointer encFieldDescPointer)
    {
        Data.EnCFieldDesc encFieldDesc = _target.ProcessedData.GetOrAdd<Data.EnCFieldDesc>(encFieldDescPointer);
        if (encFieldDesc.StaticFieldData == TargetPointer.Null)
            return TargetPointer.Null;

        Data.EnCAddedStaticField staticField = _target.ProcessedData.GetOrAdd<Data.EnCAddedStaticField>(encFieldDesc.StaticFieldData);
        return staticField.FieldData;
    }

    TargetPointer IRuntimeMutableTypeSystem.GetEnCInstanceFieldAddress(TargetPointer objectAddress, TargetPointer encFieldDescPointer)
    {
        IObject objectContract = _target.Contracts.Object;
        IGC gcContract = _target.Contracts.GC;
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        TargetPointer syncBlockAddress = objectContract.GetSyncBlockAddress(objectAddress);
        if (syncBlockAddress == TargetPointer.Null)
            return TargetPointer.Null;

        Data.SyncBlock syncBlock = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(syncBlockAddress);
        if (syncBlock.EnCInfo is not TargetPointer encInfoAddress)
            return TargetPointer.Null;

        Data.EnCSyncBlockInfo encInfo = _target.ProcessedData.GetOrAdd<Data.EnCSyncBlockInfo>(encInfoAddress);

        // Walk the linked list of EnCAddedField entries to find the matching FieldDesc
        TargetPointer entryPtr = encInfo.List;
        while (entryPtr != TargetPointer.Null)
        {
            Data.EnCAddedField entry = _target.ProcessedData.GetOrAdd<Data.EnCAddedField>(entryPtr);
            if (entry.FieldDesc == encFieldDescPointer)
            {
                // Found it. Get the dependent handle secondary (the EnC helper object).
                TargetPointer handleAddress = entry.FieldData.Handle;
                if (handleAddress == TargetPointer.Null)
                    return TargetPointer.Null;

                TargetNUInt secondary = gcContract.GetHandleExtraInfo(handleAddress);
                TargetPointer helperObjectAddress = new TargetPointer(secondary.Value);
                if (helperObjectAddress == TargetPointer.Null)
                    return TargetPointer.Null;

                Data.EditAndContinueHelperObject helper =
                    _target.ProcessedData.GetOrAdd<Data.EditAndContinueHelperObject>(helperObjectAddress);
                TargetPointer objectReferenceAddress = helper.ObjectReferenceAddress;

                // Read the OBJECTREF stored in _objectReference
                TargetPointer fieldObject = _target.ReadPointer(objectReferenceAddress);
                // Determine field type and compute final address
                CorElementType fieldType = rts.GetFieldDescType(encFieldDescPointer);
                if (fieldType == CorElementType.ValueType)
                {
                    // Value type is boxed, so unbox to get at the data
                    if (fieldObject == TargetPointer.Null)
                        return TargetPointer.Null;
                    Data.Object boxedObj = _target.ProcessedData.GetOrAdd<Data.Object>(fieldObject);
                    return boxedObj.Data;
                }
                else if (fieldType == CorElementType.Class)
                {
                    // The OBJECTREF slot itself is the field value location
                    return objectReferenceAddress;
                }
                else
                {
                    // Primitive stored in a 1-element array. Get pointer to first element.
                    if (fieldObject == TargetPointer.Null)
                        return TargetPointer.Null;
                    return objectContract.GetArrayData(fieldObject, out _, out _, out _);
                }
            }
            entryPtr = entry.Next;
        }

        return TargetPointer.Null;
    }
}
