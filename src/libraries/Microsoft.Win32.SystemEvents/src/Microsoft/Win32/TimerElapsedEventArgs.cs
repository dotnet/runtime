// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Win32
{
    /// <devdoc>
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.TimerElapsed'/> event.</para>
    /// </devdoc>
    public class TimerElapsedEventArgs : EventArgs
    {
        private readonly IntPtr _timerId;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='Microsoft.Win32.TimerElapsedEventArgs'/> class.</para>
        /// </devdoc>
        public TimerElapsedEventArgs(IntPtr timerId)
        {
            _timerId = timerId;
        }

        /// <devdoc>
        ///    <para>Gets the ID number for the timer.</para>
        /// </devdoc>
        public IntPtr TimerId
        {
            get
            {
                return _timerId;
            }
        }
    }
}
