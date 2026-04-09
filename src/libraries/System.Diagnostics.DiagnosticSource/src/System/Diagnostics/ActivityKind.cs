// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Kind describes the relationship between the Activity, its parents, and its children in a Trace.
    ///     --------------------------------------------------------------------------------
    ///    ActivityKind    Synchronous  Asynchronous    Remote Incoming    Remote Outgoing
    ///     --------------------------------------------------------------------------------
    ///       Internal
    ///       Client          yes                                               yes
    ///       Server          yes                            yes
    ///       Producer                     yes                                  maybe
    ///       Consumer                     yes               maybe
    ///     --------------------------------------------------------------------------------
    /// </summary>
    public enum ActivityKind
    {
        /// <summary>
        /// Default value.
        /// Indicates that the Activity represents an internal operation within an application, as opposed to an operations with remote parents or children.
        /// </summary>
        Internal = 0,

        /// <summary>
        /// Server activity represents request incoming from external component.
        /// </summary>
        Server = 1,

        /// <summary>
        /// Client activity represents outgoing request to the external component.
        /// </summary>
        Client = 2,

        /// <summary>
        /// Producer activity represents output provided to external components.
        /// </summary>
        Producer = 3,

        /// <summary>
        /// Consumer activity represents output received from an external component.
        /// </summary>
        Consumer = 4,
    }
}
