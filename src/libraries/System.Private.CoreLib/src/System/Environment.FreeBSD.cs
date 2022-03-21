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
                Interop.Process.kinfo_proc* processInfo = Interop.Process.GetProcInfo(ProcessId, true, out _);
                try
                {
                    return processInfo->ki_rssize;
                }
                finally
                {
                    NativeMemory.Free(processInfo);
                }
            }
        }
    }
}
