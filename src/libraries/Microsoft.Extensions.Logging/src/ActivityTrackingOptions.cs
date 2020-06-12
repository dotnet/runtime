// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Flags to indicate which trace context parts should be included with the logging scopes.
    /// </summary>
    [Flags]
    public enum ActivityTrackingOptions
    {
        /// <summary>
        /// None of the trace context part wil be included in the logging.
        /// </summary>
        None        = 0x0000,

        /// <summary>
        /// Span Id wil be included in the logging.
        /// </summary>
        SpanId      = 0x0001,

        /// <summary>
        /// Trace Id wil be included in the logging.
        /// </summary>
        TraceId     = 0x0002,

        /// <summary>
        /// Parent Id wil be included in the logging.
        /// </summary>
        ParentId    = 0x0004,

        /// <summary>
        /// Trace State wil be included in the logging.
        /// </summary>
        TraceState  = 0x0008,

        /// <summary>
        /// Trace flags wil be included in the logging.
        /// </summary>
        TraceFlags  = 0x0010
    }
}