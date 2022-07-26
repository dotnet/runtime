// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration.Internal;

namespace System.Configuration
{
    internal static class ConfigurationManagerInternalFactory
    {
        private static volatile IConfigurationManagerInternal s_instance;

        internal static IConfigurationManagerInternal Instance => s_instance ??= new ConfigurationManagerInternal();
    }
}
