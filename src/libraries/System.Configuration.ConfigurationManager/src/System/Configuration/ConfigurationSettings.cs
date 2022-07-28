// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;

namespace System.Configuration
{
    public sealed class ConfigurationSettings
    {
        internal ConfigurationSettings() { }

        [Obsolete("ConfigurationSettings.AppSettings has been deprecated. Use System.Configuration.ConfigurationManager.AppSettings instead.")]
        public static NameValueCollection AppSettings
        {
            get
            {
                return ConfigurationManager.AppSettings;
            }
        }

        [Obsolete("ConfigurationSettings.GetConfig has been deprecated. Use System.Configuration.ConfigurationManager.GetSection instead.")]
        public static object GetConfig(string sectionName)
        {
            return ConfigurationManager.GetSection(sectionName);
        }
    }
}
