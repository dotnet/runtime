// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;

namespace System.Configuration
{
    public sealed class ConfigurationSettings
    {
        public ConfigurationSettings() { }

        [Obsolete("This method is obsolete, it has been replaced by System.Configuration!System.Configuration.ConfigurationManager.AppSettings")]
        public static NameValueCollection AppSettings
        {
            get
            {
                return ConfigurationManager.AppSettings;
            }
        }

        [Obsolete("This method is obsolete, it has been replaced by System.Configuration!System.Configuration.ConfigurationManager.GetSection")]
        public static object GetConfig(string sectionName)
        {
            return ConfigurationManager.GetSection(sectionName);
        }
    }
}
