// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>Indicates the priority that the system associates with a process. This value, together with the priority value of each thread of the process, determines each thread's base priority level.</summary>
    /// <remarks>A process priority class encompasses a range of thread priority levels. Threads with different priorities running in the process run relative to the process's priority class. The operating system uses the base-priority level of all executable threads to determine which thread gets the next slice of processor time.
    /// Win32 uses four priority classes with seven base priority levels per class. Based on time elapsed or other boosts, the operating system can change the base priority level when a process needs to be put ahead of others for access to the processor. In addition, you can set <see cref="System.Diagnostics.Process.PriorityBoostEnabled" /> to temporarily boost the priority level of threads that have been taken out of the wait state. The priority is reset when the process returns to the wait state.</remarks>
    /// <altmember cref="System.Diagnostics.Process.PriorityClass"/>
    public enum ProcessPriorityClass
    {
        /// <summary>Specifies that the process has no special scheduling needs.</summary>
        Normal = 0x20,

        /// <summary>Specifies that the threads of this process run only when the system is idle, such as a screen saver. The threads of the process are preempted by the threads of any process running in a higher priority class. This priority class is inherited by child processes.</summary>
        Idle = 0x40,

        /// <summary>Specifies that the process performs time-critical tasks that must be executed immediately, such as the <see langword="Task List" /> dialog, which must respond quickly when called by the user, regardless of the load on the operating system. The threads of the process preempt the threads of normal or idle priority class processes. <br />Use extreme care when specifying <see langword="High" /> for the process's priority class, because a high priority class application can use nearly all available processor time.</summary>
        High = 0x80,

        /// <summary>Specifies that the process has the highest possible priority. <br />The threads of a process with <see langword="RealTime" /> priority preempt the threads of all other processes, including operating system processes performing important tasks. Thus, a <see langword="RealTime" /> priority process that executes for more than a very brief interval can cause disk caches not to flush or cause the mouse to be unresponsive.</summary>
        RealTime = 0x100,

        /// <summary>Specifies that the process has priority above <see langword="Idle" /> but below <see langword="Normal" />.</summary>
        BelowNormal = 0x4000,

        /// <summary>Specifies that the process has priority higher than <see langword="Normal" /> but lower than <see langword="High" />.</summary>
        AboveNormal = 0x8000
    }
}
