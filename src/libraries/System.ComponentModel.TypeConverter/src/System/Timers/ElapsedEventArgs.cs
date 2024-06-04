// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Timers
{
    /// <summary>
    /// Provides data for the <see cref='System.Timers.Timer.Elapsed'/> event.
    /// </summary>
    public sealed class ElapsedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.Timers.ElapsedEventArgs'/> class.
        /// </summary>
        /// <param name="signalTime">Time when the timer elapsed</param>
        public ElapsedEventArgs(DateTime signalTime)
        {
            SignalTime = signalTime;
        }

        /// <summary>
        /// Gets the time when the timer elapsed.
        /// </summary>
        public DateTime SignalTime { get; }
    }
}
