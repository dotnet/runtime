// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Threading;

public static partial class Monitor
{
#if !FEATURE_WASM_MANAGED_THREADS
    [UnsupportedOSPlatform("browser")]
#endif
    public static bool Wait(object obj, int millisecondsTimeout)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return GetLockObject(obj).Wait(millisecondsTimeout, obj);
    }
}
