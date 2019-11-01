// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <devdoc>
    ///     Specifies the reason a thread is waiting.
    /// </devdoc>
    public enum ThreadWaitReason
    {
        /// <devdoc>
        ///     Thread is waiting for the scheduler.
        /// </devdoc>
        Executive,

        /// <devdoc>
        ///     Thread is waiting for a free virtual memory page.
        /// </devdoc>
        FreePage,

        /// <devdoc>
        ///     Thread is waiting for a virtual memory page to arrive in memory.
        /// </devdoc>
        PageIn,

        /// <devdoc>
        ///     Thread is waiting for a system allocation.
        /// </devdoc>
        SystemAllocation,

        /// <devdoc>
        ///     Thread execution is delayed.
        /// </devdoc>
        ExecutionDelay,

        /// <devdoc>
        ///     Thread execution is suspended.
        /// </devdoc>
        Suspended,

        /// <devdoc>
        ///     Thread is waiting for a user request.
        /// </devdoc>
        UserRequest,

        /// <devdoc>
        ///     Thread is waiting for event pair high.
        /// </devdoc>
        EventPairHigh,

        /// <devdoc>
        ///     Thread is waiting for event pair low.
        /// </devdoc>
        EventPairLow,

        /// <devdoc>
        ///     Thread is waiting for a local procedure call to arrive.
        /// </devdoc>
        LpcReceive,

        /// <devdoc>
        ///     Thread is waiting for reply to a local procedure call to arrive.
        /// </devdoc>
        LpcReply,

        /// <devdoc>
        ///     Thread is waiting for virtual memory.
        /// </devdoc>
        VirtualMemory,

        /// <devdoc>
        ///     Thread is waiting for a virtual memory page to be written to disk.
        /// </devdoc>
        PageOut,

        /// <devdoc>
        ///     Thread is waiting for an unknown reason.
        /// </devdoc>
        Unknown
    }
}
