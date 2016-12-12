//------------------------------------------------------------------------------
// <copyright file="PrivilegedConfigurationManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


namespace System.Configuration {

    using System.Collections.Specialized;
    using System.Security;
    using System.Security.Permissions;
    
    [ConfigurationPermission(SecurityAction.Assert, Unrestricted=true)]
    internal static class PrivilegedConfigurationManager {
        internal static ConnectionStringSettingsCollection ConnectionStrings { 
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get {
                return ConfigurationManager.ConnectionStrings;
            }
        }

        internal static object GetSection(string sectionName) {
            return ConfigurationManager.GetSection(sectionName);
        }
    }
}
