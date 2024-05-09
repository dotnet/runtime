// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class Registry
{
    public Dictionary<Type, IContract> _contracts = [];
    public Target _target;

    public Registry(Target target)
    {
        _target = target;
    }

    public Thread Thread => GetContract<Thread>();

    private T GetContract<T>() where T : IContract
    {
        if (_contracts.TryGetValue(typeof(T), out IContract? contractMaybe))
            return (T)contractMaybe;

        IContract contract = T.Create(_target);

        // Still okay if contract was already registered by someone else
        _ = _contracts.TryAdd(typeof(T), contract);
        return (T)contract;
    }
}
