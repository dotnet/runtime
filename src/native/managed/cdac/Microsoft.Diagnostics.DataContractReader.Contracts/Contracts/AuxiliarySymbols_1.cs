// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct AuxiliarySymbols_1 : IAuxiliarySymbols
{
    private readonly Target _target;

    internal AuxiliarySymbols_1(Target target)
    {
        _target = target;
    }

    IEnumerable<(TargetCodePointer Address, string Name)> IAuxiliarySymbols.EnumerateAuxiliarySymbols()
        => EnumerateAuxiliarySymbols();

    bool IAuxiliarySymbols.TryGetAuxiliarySymbolName(TargetPointer ip, [NotNullWhen(true)] out string? symbolName)
    {
        symbolName = null;

        TargetCodePointer codePointer = CodePointerUtils.CodePointerFromAddress(ip, _target);

        foreach ((TargetCodePointer address, string name) in EnumerateAuxiliarySymbols())
        {
            if (address == codePointer)
            {
                symbolName = name;
                return true;
            }
        }

        return false;
    }

    private IEnumerable<(TargetCodePointer Address, string Name)> EnumerateAuxiliarySymbols()
    {
        TargetPointer helperArrayPtr = _target.ReadGlobalPointer(Constants.Globals.AuxiliarySymbols);
        uint helperCount = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.AuxiliarySymbolCount));
        uint entrySize = Data.AuxiliarySymbolInfo.GetSize(_target);

        for (uint i = 0; i < helperCount; i++)
        {
            TargetPointer entryAddr = helperArrayPtr + (ulong)(i * entrySize);
            Data.AuxiliarySymbolInfo entry = _target.ProcessedData.GetOrAdd<Data.AuxiliarySymbolInfo>(entryAddr);

            if (entry.Name != TargetPointer.Null)
                yield return (entry.CodeAddress, _target.ReadUtf8String(entry.Name));
        }
    }
}
