// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <devdoc>
    ///     This data structure contains information about a thread in a process that
    ///     is collected in bulk by querying the operating system.  The reason to
    ///     make this a separate structure from the ProcessThread component is so that we
    ///     can throw it away all at once when Refresh is called on the component.
    /// </devdoc>
    /// <internalonly/>
    internal sealed class ThreadInfo
    {
#pragma warning disable CS0649 // The fields are unused on iOS/tvOS as the respective managed logic (mostly around libproc) is excluded.
        internal ulong _threadId;
        internal int _processId;
        internal int _basePriority;
        internal int _currentPriority;
        internal IntPtr _startAddress;
        internal ThreadState _threadState;
        internal ThreadWaitReason _threadWaitReason;
#pragma warning restore CS0649
    }
}
