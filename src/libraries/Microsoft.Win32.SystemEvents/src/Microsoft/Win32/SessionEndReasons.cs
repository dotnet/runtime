// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Win32
{
    /// <devdoc>
    ///    <para> Specifies how the current
    ///       logon session is ending.</para>
    /// </devdoc>
    public enum SessionEndReasons
    {
        /// <devdoc>
        ///      The user is logging off.  The system may continue
        ///      running but the user who started this application
        ///      is logging off.
        /// </devdoc>
        Logoff = 1,

        /// <devdoc>
        ///      The system is shutting down.
        /// </devdoc>
        SystemShutdown = 2,
    }
}

