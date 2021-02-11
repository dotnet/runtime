// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>Specifies the reason a thread is waiting.</summary>
    /// <remarks>The thread wait reason is only valid when the <see cref="System.Diagnostics.ThreadState" /> is <see cref="System.Diagnostics.ThreadState.Wait" />.</remarks>
    /// <altmember cref="System.Diagnostics.ThreadState"/>
    public enum ThreadWaitReason
    {
        /// <summary>The thread is waiting for the scheduler.</summary>
        Executive,

        /// <summary>The thread is waiting for a free virtual memory page.</summary>
        FreePage,

        /// <summary>The thread is waiting for a virtual memory page to arrive in memory.</summary>
        PageIn,

        /// <summary>The thread is waiting for system allocation.</summary>
        SystemAllocation,

        /// <summary>Thread execution is delayed.</summary>
        ExecutionDelay,

        /// <summary>Thread execution is suspended.</summary>
        Suspended,

        /// <summary>The thread is waiting for a user request.</summary>
        UserRequest,

        /// <summary>The thread is waiting for event pair high.</summary>
        EventPairHigh,

        /// <summary>The thread is waiting for event pair low.</summary>
        EventPairLow,

        /// <summary>The thread is waiting for a local procedure call to arrive.</summary>
        LpcReceive,

        /// <summary>The thread is waiting for reply to a local procedure call to arrive.</summary>
        LpcReply,

        /// <summary>The thread is waiting for the system to allocate virtual memory.</summary>
        VirtualMemory,

        /// <summary>The thread is waiting for a virtual memory page to be written to disk.</summary>
        PageOut,

        /// <summary>The thread is waiting for an unknown reason.</summary>
        Unknown
    }
}
