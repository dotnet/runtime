// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System
{
    public sealed class LocalDataStoreSlot
    {
        internal LocalDataStoreSlot(ThreadLocal<object?> data)
        {
            Data = data;
            GC.SuppressFinalize(this);
        }

        internal ThreadLocal<object?> Data { get; private set; }

        [SuppressMessage("Microsoft.Security", "CA1821", Justification = "Finalizer preserved for compat, it is suppressed by the constructor.")]
        ~LocalDataStoreSlot()
        {
        }
    }
}
