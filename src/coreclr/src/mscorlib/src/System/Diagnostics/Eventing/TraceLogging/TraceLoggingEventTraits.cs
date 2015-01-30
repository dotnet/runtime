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
    /// Tags are flags that are not interpreted by EventSource but are passed along
    /// to the EventListener. The EventListener determines the semantics of the flags.
    /// </summary>
    [Flags]
    public enum EventTags
    {
        /// <summary>
        /// No special traits are added to the event.
        /// </summary>
        None = 0,

        /* Bits below 0x10000 are available for any use by the provider. */
        /* Bits at or above 0x10000 are reserved for definition by Microsoft. */
    }
}
