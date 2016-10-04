// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using System;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>
    ///         Extends ISupportInitialize to allow dependent components to be notified when initialization is complete.
    ///    </para>
    /// </summary>
    public interface ISupportInitializeNotification : ISupportInitialize
    {
        /// <summary>
        ///    <para>
        ///         Indicates whether initialization is complete yet.
        ///    </para>
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        ///    <para>
        ///         Sent when initialization is complete.
        ///    </para>
        /// </summary>
        event EventHandler Initialized;
    }
}
