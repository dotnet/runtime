// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Xml;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed class ConfigSettings
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

        private readonly DebugLevel _debugLevel = GlobalConfig.DefaultDebugLevel;
        private readonly string _debugLogFile;
    }
}
