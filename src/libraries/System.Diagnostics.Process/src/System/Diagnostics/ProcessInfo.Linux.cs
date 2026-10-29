// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    internal sealed partial class ProcessInfo
    {
        /// <summary>
        /// Whether <see cref="_threadInfoList"/> has been filled in. Reading the thread list is far more
        /// expensive than reading the rest of the process information, so it is deferred until it is needed.
        /// </summary>
        internal bool ThreadsPopulated { get; set; } = true;

        /// <summary>The procfs ID that was used to read this process's information, which is needed to read its threads later.</summary>
        internal Interop.procfs.ProcPid ProcPid { get; set; }
    }
}
