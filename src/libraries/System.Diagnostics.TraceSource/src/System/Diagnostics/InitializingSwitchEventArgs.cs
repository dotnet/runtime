// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Provides data for the <see cref="Switch.Initializing"/> event.
    /// </summary>
    public sealed class InitializingSwitchEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InitializingSwitchEventArgs"/> class.
        /// </summary>
        /// <param name="switch">The switch that is being initialized.</param>
        public InitializingSwitchEventArgs(Switch @switch)
        {
            Switch = @switch;
        }

        /// <summary>
        /// Gets the <see cref="Switch" /> that is being initialized.
        /// </summary>
        public Switch Switch { get; }
    }
}
