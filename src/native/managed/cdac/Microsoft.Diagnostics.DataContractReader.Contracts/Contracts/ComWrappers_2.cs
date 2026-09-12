// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct ComWrappers_2 : IComWrappers
{
    private readonly Target _target;
    private ComWrappers_1 _previous;

    public ComWrappers_2(Target target)
    {
        _target = target;
        _previous = new ComWrappers_1(target);
    }

    public TargetPointer GetComWrappersIdentity(TargetPointer address) => _previous.GetComWrappersIdentity(address);
    public TargetPointer GetManagedObjectWrapperFromCCW(TargetPointer ccw) => _previous.GetManagedObjectWrapperFromCCW(ccw);
    public TargetPointer GetComWrappersObjectFromMOW(TargetPointer mow) => _previous.GetComWrappersObjectFromMOW(mow);
    public long GetMOWReferenceCount(TargetPointer mow) => _previous.GetMOWReferenceCount(mow);
    public TargetPointer GetIdentityForMOW(TargetPointer mow) => _previous.GetIdentityForMOW(mow);
    public List<TargetPointer> GetMOWs(TargetPointer obj, out bool hasMOWTable) => _previous.GetMOWs(obj, out hasMOWTable);
    public bool IsComWrappersRCW(TargetPointer rcw) => _previous.IsComWrappersRCW(rcw);

    public TargetPointer GetComWrappersRCWForObject(TargetPointer obj)
    {
        // The base type need not have been loaded if the process has not used it.
        if (_target.Contracts.ManagedTypeSource.TryGetTypeHandle(Data.ComWrappersObject.ManagedTypeName, out ITypeHandle? baseType))
        {
            IRuntimeTypeSystem types = _target.Contracts.RuntimeTypeSystem;
            TargetPointer methodTable = _target.Contracts.Object.GetMethodTableAddress(obj);
            while (methodTable != TargetPointer.Null)
            {
                if (methodTable == baseType.Address)
                {
                    return _target.ProcessedData.GetOrAdd<Data.ComWrappersObject>(obj).NativeObjectWrapper;
                }

                methodTable = types.GetParentMethodTable(types.GetTypeHandle(methodTable));
            }
        }

        return _previous.GetComWrappersRCWForObject(obj);
    }
}
