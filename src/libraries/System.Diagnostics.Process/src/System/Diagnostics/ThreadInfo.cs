// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public int threadId;
        public int processId;
        public int basePriority;
        public int currentPriority;
        public IntPtr startAddress;
        public ThreadState threadState;
        public ThreadWaitReason threadWaitReason;
    }
}