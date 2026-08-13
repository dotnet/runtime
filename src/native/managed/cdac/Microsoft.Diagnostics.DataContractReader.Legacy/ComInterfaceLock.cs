// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal readonly struct ComInterfaceLock : IDisposable
{
    private readonly object _lock;

    internal ComInterfaceLock(object apiLock)
    {
        _lock = apiLock;
        System.Threading.Monitor.Enter(apiLock);
    }

    public void Dispose() => System.Threading.Monitor.Exit(_lock);
}
