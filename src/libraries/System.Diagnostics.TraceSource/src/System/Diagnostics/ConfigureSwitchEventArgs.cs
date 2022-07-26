// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    ///     Provides data for the <see cref="Trace.ConfigureSwitch"/> event.
    /// </summary>
    public sealed class ConfigureSwitchEventArgs : EventArgs
    {
        public ConfigureSwitchEventArgs(Switch @switch)
        {
            Switch = @switch;
        }

        public Switch Switch { get; }
    }
}
