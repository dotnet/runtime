// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Hosting.Systemd
{
    /// <summary>
    /// Provides support to notify systemd about the service status.
    /// </summary>
    public interface ISystemdNotifier
    {
        /// <summary>
        /// Sends a notification to systemd.
        /// </summary>
        void Notify(ServiceState state);
        /// <summary>
        /// Returns whether systemd is configured to receive service notifications.
        /// </summary>
        bool IsEnabled { get; }
    }
}
