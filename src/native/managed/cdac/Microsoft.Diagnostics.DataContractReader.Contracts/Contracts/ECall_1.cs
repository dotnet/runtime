// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct ECall_1 : IECall
{
    private readonly Target _target;

    internal ECall_1(Target target)
    {
        _target = target;
    }

    TargetPointer IECall.MapTargetBackToMethodDesc(TargetCodePointer codePointer)
    {
        if (codePointer == TargetCodePointer.Null)
            return TargetPointer.Null;


        TargetPointer pECHashTable = _target.ReadGlobalPointer(Constants.Globals.FCallMethods);

        TargetPointer pECHash = pECHashTable + (ulong)(FCallHash(codePointer) * _target.PointerSize);
        TargetPointer ecHashAddr = _target.ReadPointer(pECHash);

        while (ecHashAddr != TargetPointer.Null)
        {
            Data.ECHash eCHash = _target.ProcessedData.GetOrAdd<Data.ECHash>(ecHashAddr);
            if (eCHash.Implementation == codePointer)
            {
                return eCHash.MethodDesc;
            }
            ecHashAddr = eCHash.Next;
        }

        return TargetPointer.Null; // not found
    }

    private uint FCallHash(TargetCodePointer codePointer)
    {
        // see FCallHash in ecall.cpp
        uint hashBucketCount = _target.ReadGlobal<uint>(Constants.Globals.FCallHashSize);
        return (uint)(codePointer.Value % hashBucketCount);
    }
}
