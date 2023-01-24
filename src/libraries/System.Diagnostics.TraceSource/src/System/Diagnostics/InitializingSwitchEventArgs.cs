// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Provides data for the <see cref="Switch.Initializing"/> event.
    /// </summary>
    public sealed class InitializingSwitchEventArgs : EventArgs
    {
        public InitializingSwitchEventArgs(Switch @switch)
        {
            Switch = @switch;
        }

        public Switch Switch { get; }
    }
}
