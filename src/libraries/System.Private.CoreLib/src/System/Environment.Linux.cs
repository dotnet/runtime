// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public static partial class Environment
    {
        private static ReusableTextReader reusableReader = new ReusableTextReader();
        public static long WorkingSet => (long)(Interop.procfs.TryReadStatusFile(ProcessId, out Interop.procfs.ParsedStatus status, reusableReader) ? status.VmRSS : 0);
    }
}
