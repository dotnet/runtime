// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct AuxiliarySymbols_1 : IAuxiliarySymbols
{
    private readonly Target _target;

    internal AuxiliarySymbols_1(Target target)
    {
        _target = target;
    }

    bool IAuxiliarySymbols.TryGetAuxiliarySymbolName(TargetPointer ip, [NotNullWhen(true)] out string? symbolName)
    {
        symbolName = null;

        TargetCodePointer codePointer = CodePointerUtils.CodePointerFromAddress(ip, _target);

        TargetPointer helperArrayPtr = _target.ReadGlobalPointer(Constants.Globals.AuxiliarySymbols);
        uint helperCount = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.AuxiliarySymbolCount));

        Target.TypeInfo typeInfo = _target.GetTypeInfo(DataType.AuxiliarySymbolInfo);
        uint entrySize = typeInfo.Size!.Value;

        for (uint i = 0; i < helperCount; i++)
        {
            TargetPointer entryAddr = helperArrayPtr + (ulong)(i * entrySize);
            Data.AuxiliarySymbolInfo entry = _target.ProcessedData.GetOrAdd<Data.AuxiliarySymbolInfo>(entryAddr);

            if (entry.Address == codePointer)
            {
                if (entry.Name != TargetPointer.Null)
                {
                    symbolName = _target.ReadUtf8String(entry.Name);
                    return true;
                }

                return false;
            }
        }

        return false;
    }
}
