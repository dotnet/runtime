// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>Specifies the priority level of a thread.</summary>
    /// <remarks>Every thread has a base-priority level determined by the thread's priority value and the priority class of its process. The operating system uses the base-priority level of all executable threads to determine which thread gets the next slice of processor time.
    /// The priority level is not an absolute value, but instead is a range of priority values. The operating system computes the priority by using the process priority class to determine where, in the range specified by the <see cref="System.Diagnostics.ProcessThread.PriorityLevel" /> property, to set the thread's priority.</remarks>
    /// <altmember cref="System.Diagnostics.ProcessThread.PriorityLevel"/>
    /// <altmember cref="System.Diagnostics.ProcessPriorityClass"/>
    public enum ThreadPriorityLevel
    {
        /// <summary>Specifies idle priority. This is the lowest possible priority value of all threads, independent of the value of the associated <see cref="System.Diagnostics.ProcessPriorityClass" />.</summary>
        Idle = -15,

        /// <summary>Specifies lowest priority. This is two steps below the normal priority for the associated <see cref="System.Diagnostics.ProcessPriorityClass" />.</summary>
        Lowest = -2,

        /// <summary>Specifies one step below the normal priority for the associated <see cref="System.Diagnostics.ProcessPriorityClass" />.</summary>
        BelowNormal = -1,

        /// <summary>Specifies normal priority for the associated <see cref="System.Diagnostics.ProcessPriorityClass" />.</summary>
        Normal = 0,

        /// <summary>Specifies one step above the normal priority for the associated <see cref="System.Diagnostics.ProcessPriorityClass" />.</summary>
        AboveNormal = 1,

        /// <summary>Specifies highest priority. This is two steps above the normal priority for the associated <see cref="System.Diagnostics.ProcessPriorityClass" />.</summary>
        Highest = 2,

        /// <summary>Specifies time-critical priority. This is the highest priority of all threads, independent of the value of the associated <see cref="System.Diagnostics.ProcessPriorityClass" />.</summary>
        TimeCritical = 15
    }
}
