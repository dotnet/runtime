// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ConditionalWeakTableFactory : IContractFactory<IConditionalWeakTable>
{
    IConditionalWeakTable IContractFactory<IConditionalWeakTable>.CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new ConditionalWeakTable_1(target),
            _ => default(ConditionalWeakTable),
        };
    }
}
