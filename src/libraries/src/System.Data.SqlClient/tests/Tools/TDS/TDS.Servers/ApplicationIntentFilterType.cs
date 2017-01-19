// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.Servers
{
    /// <summary>
    /// Type of application intent filter
    /// </summary>
    public enum ApplicationIntentFilterType
    {
        /// <summary>
        /// Filter out all requests and don't let anyone connect
        /// </summary>
        None,

        /// <summary>
        /// Allow read connections only
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Don't filter anything and let everyone connect
        /// </summary>
        All,
    }
}
