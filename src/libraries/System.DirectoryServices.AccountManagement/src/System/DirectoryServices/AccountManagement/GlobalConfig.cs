/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    GlobalConfig.cs

Abstract:

    Implements app-wide configuration settings

History:

    18-Aug-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;
using System.Configuration;


namespace System.DirectoryServices.AccountManagement
{    
    internal static class GlobalConfig
    {
        static GlobalConfig()
        {
            GlobalConfig.configSettings = (ConfigSettings) ConfigurationManager.GetSection("System.DirectoryServices.AccountManagement");
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
                if (GlobalConfig.configSettings == null)
                    return GlobalConfig.DefaultDebugLevel;
            
                return GlobalConfig.configSettings.DebugLevel;
            }
        }

        static public string DebugLogFile
        {
            get
            {
                if (GlobalConfig.configSettings == null)
                    return null;

                return GlobalConfig.configSettings.DebugLogFile;
            }
        }

        static ConfigSettings configSettings;
    }


}
