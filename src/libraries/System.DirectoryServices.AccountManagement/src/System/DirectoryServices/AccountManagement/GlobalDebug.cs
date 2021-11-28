// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    internal enum DebugLevel
    {
        None = 0,
        Info,
        Warn,
        Error
    }

    internal static class GlobalDebug
    {
        private static readonly DebugLevel s_debugLevel = GlobalConfig.DebugLevel;

        public static bool Error => DebugLevel.Error >= s_debugLevel;

        public static bool Warn => DebugLevel.Warn >= s_debugLevel;

        public static bool Info => DebugLevel.Info >= s_debugLevel;

        [ConditionalAttribute("DEBUG")]
        public static void WriteLineIf(bool f, string category, string message, params object[] args)
        {
            message = "[" + Interop.Kernel32.GetCurrentThreadId().ToString("x", CultureInfo.InvariantCulture) + "] " + message;

            Debug.WriteLineIf(f, string.Format(CultureInfo.InvariantCulture, message, args), category);
        }

        [ConditionalAttribute("DEBUG")]
        public static void WriteLineIf(bool f, string category, string message)
        {
            message = "[" + Interop.Kernel32.GetCurrentThreadId().ToString("x", CultureInfo.InvariantCulture) + "] " + message;

            Debug.WriteLineIf(f, message, category);
        }
    }
}
