// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Configuration
{
    internal static class PrivilegedConfigurationManager
    {
        internal static ConnectionStringSettingsCollection ConnectionStrings
        {
            [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
            get => ConfigurationManager.ConnectionStrings;
        }

        [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
        internal static object GetSection(string sectionName)
        {
            return ConfigurationManager.GetSection(sectionName);
        }
    }
}
