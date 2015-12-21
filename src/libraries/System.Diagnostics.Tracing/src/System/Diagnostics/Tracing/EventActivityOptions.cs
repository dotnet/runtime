// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// EventActivityOptions flags allow to specify different activity related characteristics.
    /// </summary>
    [Flags]
    public enum EventActivityOptions
    {
        /// <summary>
        /// No special options are added to the event.
        /// </summary>
        None = 0,

        /// <summary>
        /// Disable Implicit Activity Tracking
        /// </summary>
        Disable = 0x2,

        /// <summary>
        /// Allow activity event to call itself (directly or indirectly)
        /// </summary>
        Recursive = 0x4,

        /// <summary>
        /// Allows event activity to live beyond its parent.
        /// </summary>
        Detachable = 0x8
    }
}
