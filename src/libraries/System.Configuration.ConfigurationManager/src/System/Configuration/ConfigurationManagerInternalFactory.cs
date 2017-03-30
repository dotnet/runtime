// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;

namespace System.Configuration
{
    internal static class ConfigurationManagerInternalFactory
    {
        static private volatile IConfigurationManagerInternal s_instance;

        static internal IConfigurationManagerInternal Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new ConfigurationManagerInternal();
                }

                return s_instance;
            }
        }
    }
}
