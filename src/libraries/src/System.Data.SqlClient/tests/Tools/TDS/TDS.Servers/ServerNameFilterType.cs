// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.Servers
{
    /// <summary>
    /// Type of the filter to apply to server name
    /// </summary>
    public enum ServerNameFilterType
    {
        /// <summary>
        /// Do not perform any filtering
        /// </summary>
        None,

        /// <summary>
        /// Values must strictly match
        /// </summary>
        Equals,

        /// <summary>
        /// Value in the login packet starts with the filtering value
        /// </summary>
        StartsWith,

        /// <summary>
        /// Value in the login packet ends with the filtering value
        /// </summary>
        EndsWith,

        /// <summary>
        /// Value in the login packet contains filtering value
        /// </summary>
        Contains,
    }
}
