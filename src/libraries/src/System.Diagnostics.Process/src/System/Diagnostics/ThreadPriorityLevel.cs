// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <devdoc>
    ///     Specifies the priority level of a thread.  The priority level is not an absolute 
    ///     level, but instead contributes to the actual thread priority by considering the 
    ///     priority class of the process.
    /// </devdoc>
    public enum ThreadPriorityLevel
    {
        /// <devdoc>
        ///     Idle priority
        /// </devdoc>
        Idle = -15,

        /// <devdoc>
        ///     Lowest priority
        /// </devdoc>
        Lowest = -2,

        /// <devdoc>
        ///     Below normal priority
        /// </devdoc>
        BelowNormal = -1,

        /// <devdoc>
        ///     Normal priority
        /// </devdoc>
        Normal = 0,

        /// <devdoc>
        ///     Above normal priority
        /// </devdoc>
        AboveNormal = 1,

        /// <devdoc>
        ///     Highest priority
        /// </devdoc>
        Highest = 2,

        /// <devdoc>
        ///     Time critical priority
        /// </devdoc>
        TimeCritical = 15
    }
}
