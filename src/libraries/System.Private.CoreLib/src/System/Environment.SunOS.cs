// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System
{
    public static partial class Environment
    {
        public static long WorkingSet =>
            (long)(Interop.procfs.TryReadProcessStatusInfo(ProcessId, out Interop.procfs.ProcessStatusInfo status) ? status.ResidentSetSize : 0);
    }
}
