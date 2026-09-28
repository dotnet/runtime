// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface ICallingConvention : IContract
{
    static string IContract.Name => nameof(CallingConvention);

    bool TryComputeArgGCRefMapBlob(MethodDescHandle methodDesc, out byte[] blob)
        => throw new NotImplementedException();
}

public readonly struct CallingConvention : ICallingConvention
{
    // Everything throws NotImplementedException
}
