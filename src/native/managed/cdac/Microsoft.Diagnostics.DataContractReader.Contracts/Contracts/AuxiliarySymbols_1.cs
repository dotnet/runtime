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

    bool IAuxiliarySymbols.TryGetJitHelperName(TargetPointer ip, [NotNullWhen(true)] out string? helperName)
    {
        helperName = null;

        TargetCodePointer codePointer = CodePointerUtils.CodePointerFromAddress(ip, _target);

        TargetPointer helperArrayPtr = _target.ReadGlobalPointer(Constants.Globals.InterestingJitHelpers);
        int helperCount = _target.Read<int>(_target.ReadGlobalPointer(Constants.Globals.InterestingJitHelperCount));

        Target.TypeInfo typeInfo = _target.GetTypeInfo(DataType.JitHelperInfo);
        uint entrySize = typeInfo.Size!.Value;

        for (int i = 0; i < helperCount; i++)
        {
            TargetPointer entryAddr = helperArrayPtr + (ulong)(i * entrySize);
            Data.JitHelperInfo entry = _target.ProcessedData.GetOrAdd<Data.JitHelperInfo>(entryAddr);

            if (entry.Address == codePointer)
            {
                if (entry.Name != TargetPointer.Null)
                {
                    helperName = _target.ReadUtf16String(entry.Name);
                    return true;
                }

                return false;
            }
        }

        return false;
    }
}
