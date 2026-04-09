// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Tracing
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
