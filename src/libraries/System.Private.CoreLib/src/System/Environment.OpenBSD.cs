// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System
{
    public static partial class Environment
    {
        public static unsafe long WorkingSet
        {
            get
            {
                Interop.Process.kinfo_proc* processInfo = Interop.Process.GetProcInfo(ProcessId, out int count);
                try
                {
                    // p_vm_rssize is the current resident set size in pages.
                    return count >= 1 ? (long)processInfo->p_vm_rssize * SystemPageSize : 0;
                }
                finally
                {
                    NativeMemory.Free(processInfo);
                }
            }
        }
    }
}
