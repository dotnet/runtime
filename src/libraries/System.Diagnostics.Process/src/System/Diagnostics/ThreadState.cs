// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>Specifies the current execution state of the thread.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// > [!IMPORTANT]
    /// >  There are two thread state enumerations, <xref:System.Diagnostics.ThreadState?displayProperty=nameWithType> and <xref:System.Threading.ThreadState?displayProperty=nameWithType>.  The thread state enumerations are only of interest in a few debugging scenarios. Your code should never use thread state to synchronize the activities of threads.
    /// ]]></format></remarks>
    /// <altmember cref="System.Diagnostics.ProcessThread"/>
    /// <altmember cref="System.Diagnostics.Process"/>
    public enum ThreadState
    {
        /// <summary>A state that indicates the thread has been initialized, but has not yet started.</summary>
        Initialized,

        /// <summary>A state that indicates the thread is waiting to use a processor because no processor is free. The thread is prepared to run on the next available processor.</summary>
        Ready,

        /// <summary>A state that indicates the thread is currently using a processor.</summary>
        Running,

        /// <summary>A state that indicates the thread is about to use a processor. Only one thread can be in this state at a time.</summary>
        Standby,

        /// <summary>A state that indicates the thread has finished executing and has exited.</summary>
        Terminated,

        /// <summary>A state that indicates the thread is not ready to use the processor because it is waiting for a peripheral operation to complete or a resource to become free. When the thread is ready, it will be rescheduled.</summary>
        Wait,

        /// <summary>A state that indicates the thread is waiting for a resource, other than the processor, before it can execute. For example, it might be waiting for its execution stack to be paged in from disk.</summary>
        Transition,

        /// <summary>The state of the thread is unknown.</summary>
        Unknown
    }
}
