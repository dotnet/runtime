// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>Specifies what messages to output for debugging
    ///       and tracing.</para>
    /// </devdoc>
    public enum TraceLevel
    {
        /// <devdoc>
        ///    <para>
        ///       Output no tracing and debugging
        ///       messages.
        ///    </para>
        /// </devdoc>
        Off = 0,
        /// <devdoc>
        ///    <para>
        ///       Output error-handling messages.
        ///    </para>
        /// </devdoc>
        Error = 1,
        /// <devdoc>
        ///    <para>
        ///       Output warnings and error-handling
        ///       messages.
        ///    </para>
        /// </devdoc>
        Warning = 2,
        /// <devdoc>
        ///    <para>
        ///       Output informational messages, warnings, and error-handling messages.
        ///    </para>
        /// </devdoc>
        Info = 3,
        /// <devdoc>
        ///    Output all debugging and tracing messages.
        /// </devdoc>
        Verbose = 4,
    }
}
