// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using System.Configuration;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    internal class ConfigSettings
    {
        public ConfigSettings(DebugLevel debugLevel, string debugLogFile)
        {
            _debugLevel = debugLevel;
            _debugLogFile = debugLogFile;
        }

        public ConfigSettings() : this(GlobalConfig.DefaultDebugLevel, null)
        {
        }

        public DebugLevel DebugLevel
        {
            get { return _debugLevel; }
        }

        public string DebugLogFile
        {
            get { return _debugLogFile; }
        }

        private DebugLevel _debugLevel = GlobalConfig.DefaultDebugLevel;
        private string _debugLogFile = null;
    }
}
