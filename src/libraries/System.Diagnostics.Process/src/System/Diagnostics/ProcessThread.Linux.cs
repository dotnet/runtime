// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        private Interop.procfs.ParsedStat GetStat()
        {
            Interop.procfs.ParsedStat stat;
            if (!ProcessManager.TryReadStatFile(_processId, tid: Id, out stat))
            {
                throw new InvalidOperationException(SR.Format(SR.ThreadExited, Id));
            }
            return stat;
        }
    }
}
