// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <devdoc>
    ///     A category of priority for a process.  Threads within a process
    ///     run at a priority which is relative to the process priority class.
    /// </devdoc>
    public enum ProcessPriorityClass
    {
        /// <devdoc>
        ///      Specify this class for a process with no special scheduling needs. 
        /// </devdoc>
        Normal = 0x20,

        /// <devdoc>
        ///     Specify this class for a process whose threads run only when the system is idle. 
        ///     The threads of the process are preempted by the threads of any process running in 
        ///     a higher priority class. An example is a screen saver. The idle-priority class is 
        ///     inherited by child processes.
        /// </devdoc>
        Idle = 0x40,

        /// <devdoc>
        ///     Specify this class for a process that performs time-critical tasks that must 
        ///     be executed immediately. The threads of the process preempt the threads of 
        ///     normal or idle priority class processes. An example is the Task List, which 
        ///     must respond quickly when called by the user, regardless of the load on the 
        ///     operating system. Use extreme care when using the high-priority class, because 
        ///     a high-priority class application can use nearly all available CPU time.
        /// </devdoc>
        High = 0x80,

        /// <devdoc>
        ///     Specify this class for a process that has the highest possible priority. 
        ///     The threads of the process preempt the threads of all other processes, 
        ///     including operating system processes performing important tasks. For example, 
        ///     a real-time process that executes for more than a very brief interval can cause 
        ///     disk caches not to flush or cause the mouse to be unresponsive.
        /// </devdoc>
        RealTime = 0x100,

        /// <devdoc>
        ///     Indicates a process that has priority above Idle but below Normal.
        /// </devdoc>
        BelowNormal = 0x4000,

        /// <devdoc>
        ///     Indicates a process that has priority above Normal but below High.
        /// </devdoc>
        AboveNormal = 0x8000
    }
}
