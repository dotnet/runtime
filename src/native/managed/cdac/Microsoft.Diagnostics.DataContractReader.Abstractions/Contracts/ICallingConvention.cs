// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface ICallingConvention : IContract
{
    static string IContract.Name => nameof(CallingConvention);

    // Encode the argument GCRefMap blob byte-for-byte compatible with the
    // runtime's ComputeCallRefMap (frames.cpp). Returns false when this
    // contract declines to encode the method (e.g. an unported ABI path);
    // callers map false to E_NOTIMPL. On false, the value of <paramref name="blob"/>
    // is unspecified (callers should ignore it).
    bool TryComputeArgGCRefMapBlob(MethodDescHandle methodDesc, out byte[] blob)
        => throw new NotImplementedException();
}

public readonly struct CallingConvention : ICallingConvention
{
    // Everything throws NotImplementedException
}
