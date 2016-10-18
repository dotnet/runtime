// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <devdoc>
    ///     Specifies the execution state of a thread.
    /// </devdoc>
    [Serializable]
    public enum ThreadState
    {
        /// <devdoc>
        ///     The thread has been initialized, but has not started yet.
        /// </devdoc>
        Initialized,

        /// <devdoc>
        ///     The thread is in ready state.
        /// </devdoc>
        Ready,

        /// <devdoc>
        ///     The thread is running.
        /// </devdoc>
        Running,

        /// <devdoc>
        ///     The thread is in standby state.
        /// </devdoc>
        Standby,

        /// <devdoc>
        ///     The thread has exited.
        /// </devdoc>
        Terminated,

        /// <devdoc>
        ///     The thread is waiting.
        /// </devdoc>
        Wait,

        /// <devdoc>
        ///     The thread is transitioning between states.
        /// </devdoc>
        Transition,

        /// <devdoc>
        ///     The thread state is unknown.
        /// </devdoc>
        Unknown
    }
}
