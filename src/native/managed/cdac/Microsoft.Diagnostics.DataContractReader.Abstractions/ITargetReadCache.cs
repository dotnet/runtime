// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader;

public delegate void RawReadDelegate(ulong address, Span<byte> destination);

public interface ITargetReadCache : IDisposable
{
    bool TryRead(ulong address, Span<byte> destination, RawReadDelegate readDelegate);
    void Invalidate();
}
