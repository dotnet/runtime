// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;

namespace System.DirectoryServices.AccountManagement
{
    internal static class GlobalConfig
    {
        public const DebugLevel DefaultDebugLevel =
#if DEBUG
            DebugLevel.Info;
#else
            DebugLevel.None;
#endif

        public static DebugLevel DebugLevel => s_configSettings?.DebugLevel ?? DefaultDebugLevel;

        public static string DebugLogFile => s_configSettings?.DebugLogFile;

        private static readonly ConfigSettings s_configSettings = (ConfigSettings)ConfigurationManager.GetSection("System.DirectoryServices.AccountManagement");
    }
}
