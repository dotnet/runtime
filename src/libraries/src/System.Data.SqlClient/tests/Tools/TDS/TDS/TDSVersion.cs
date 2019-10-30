// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// TDS version routines
    /// </summary>
    public static class TDSVersion
    {
        /// <summary>
        /// Yukon TDS version
        /// </summary>
        public static Version SqlServer2005 = new Version(7, 2, 9, 2);

        /// <summary>
        /// Katmai TDS version
        /// </summary>
        public static Version SqlServer2008 = new Version(7, 3, 11, 3);

        /// <summary>
        /// Denali TDS version
        /// </summary>
        public static Version SqlServer2010 = new Version(7, 4, 0, 4);

        /// <summary>
        /// Map SQL Server build version to TDS version
        /// </summary>
        /// <param name="buildVersion">Build version to analyze</param>
        /// <returns>TDS version that corresponding build version supports</returns>
        public static Version GetTDSVersion(Version buildVersion)
        {
            // Check build version Major part
            if (buildVersion.Major == 11)
            {
                // Denali
                return SqlServer2010;
            }
            else if (buildVersion.Major == 10)
            {
                // Katmai
                return SqlServer2008;
            }
            else if (buildVersion.Major == 9)
            {
                // Yukon
                return SqlServer2005;
            }
            else
            {
                // Not supported TDS version
                throw new NotSupportedException("Specified build version is not supported");
            }
        }

        /// <summary>
        /// Resolve conflicts between client and server TDS version
        /// </summary>
        /// <param name="tdsServer">Version of the server</param>
        /// <param name="tdsClient">Version of the client</param>
        /// <returns>Resulting version that both parties can talk</returns>
        public static Version Resolve(Version tdsServer, Version tdsClient)
        {
            // Pick the lowest TDS version between client and server
            if (tdsServer > tdsClient)
            {
                // Client doesn't talk our TDS version - downgrade it to client's
                return tdsClient;
            }
            else
            {
                // Client supports our TDS version
                return tdsServer;
            }
        }

        /// <summary>
        /// Check whether TDS version is supported by server
        /// </summary>
        public static bool IsSupported(Version tdsVersion)
        {
            return tdsVersion >= SqlServer2005 && tdsVersion <= SqlServer2010;
        }
    }
}
