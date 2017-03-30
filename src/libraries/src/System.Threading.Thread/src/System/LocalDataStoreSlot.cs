// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System
{
    public sealed class LocalDataStoreSlot
    {
        internal LocalDataStoreSlot(ThreadLocal<object> data)
        {
            Data = data;
            GC.SuppressFinalize(this);
        }

        internal ThreadLocal<object> Data { get; private set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA1821", Justification = "Finalizer preserved for compat, it is suppressed by the constructor.")]
        ~LocalDataStoreSlot()
        {
        }
    }
}
