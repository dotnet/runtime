// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface IDacStreams : IContract
{
    static string IContract.Name { get; } = nameof(DacStreams);
    static IContract IContract.Create(Target target, int version)
    {
        return version switch
        {
            1 => new DacStreams_1(target),
            _ => default(DacStreams),
        };
    }

    public virtual string? StringFromEEAddress(TargetPointer address) => throw new NotImplementedException();
}

internal readonly struct DacStreams : IDacStreams
{
    // Everything throws NotImplementedException
}
