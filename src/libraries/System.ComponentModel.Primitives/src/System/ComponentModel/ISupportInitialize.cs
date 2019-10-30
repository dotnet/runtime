// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies that this object supports a simple, transacted notification for batch
    /// initialization.
    /// </summary>
    public interface ISupportInitialize
    {
        /// <summary>
        /// Signals the object that initialization is starting.
        /// </summary>
        void BeginInit();

        /// <summary>
        /// Signals the object that initialization is complete.
        /// </summary>
        void EndInit();
    }
}
