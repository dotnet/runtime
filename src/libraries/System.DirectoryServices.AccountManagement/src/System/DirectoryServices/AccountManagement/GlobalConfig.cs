// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Configuration;

namespace System.DirectoryServices.AccountManagement
{
    internal static class GlobalConfig
    {
        static GlobalConfig()
        {
            GlobalConfig.s_configSettings = (ConfigSettings)ConfigurationManager.GetSection("System.DirectoryServices.AccountManagement");
        }

#if DEBUG
        public const DebugLevel DefaultDebugLevel = DebugLevel.Warn;
#else
        public const DebugLevel DefaultDebugLevel = DebugLevel.None;
#endif

        static public DebugLevel DebugLevel
        {
            get
            {
                if (GlobalConfig.s_configSettings == null)
                    return GlobalConfig.DefaultDebugLevel;

                return GlobalConfig.s_configSettings.DebugLevel;
            }
        }

        static public string DebugLogFile
        {
            get
            {
                if (GlobalConfig.s_configSettings == null)
                    return null;

                return GlobalConfig.s_configSettings.DebugLogFile;
            }
        }

        private static ConfigSettings s_configSettings;
    }
}
