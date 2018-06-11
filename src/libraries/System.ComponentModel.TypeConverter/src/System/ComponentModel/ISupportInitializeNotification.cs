// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Extends ISupportInitialize to allow dependent components to be notified when initialization is complete.
    /// </summary>
    public interface ISupportInitializeNotification : ISupportInitialize
    {
        /// <summary>
        /// Indicates whether initialization is complete yet.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Sent when initialization is complete.
        /// </summary>
        event EventHandler Initialized;
    }
}
