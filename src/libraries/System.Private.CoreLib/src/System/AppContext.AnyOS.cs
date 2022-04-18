// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;

namespace System
{
    public static partial class AppContext
    {
#if !TARGET_BROWSER
        [UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file",
            Justification = "Single File apps should always set APP_CONTEXT_BASE_DIRECTORY therefore code handles Assembly.Location equals null")]
        private static string GetBaseDirectoryCore()
        {
            // Fallback path for hosts that do not set APP_CONTEXT_BASE_DIRECTORY explicitly
#if CORERT
            string? path = Environment.ProcessPath;
#else
            string? path = Assembly.GetEntryAssembly()?.Location;
#endif

            string? directory = Path.GetDirectoryName(path);

            if (directory == null)
                return string.Empty;

            if (!Path.EndsInDirectorySeparator(directory))
                directory += PathInternal.DirectorySeparatorCharAsString;

            return directory;
        }
#endif

#if FEATURE_PERFTRACING
        internal static void LogSwitchValues(RuntimeEventSource ev)
        {
            if (s_switches is not null)
            {
                lock (s_switches)
                {
                    foreach (KeyValuePair<string, bool> kvp in s_switches)
                    {
                        // Convert bool to int because it's cheaper to log (no boxing)
                        ev.LogAppContextSwitch(kvp.Key, kvp.Value ? 1 : 0);
                    }
                }
            }

            if (s_dataStore is not null)
            {
                lock (s_dataStore)
                {
                    if (s_switches is not null)
                    {
                        lock (s_switches)
                        {
                            LogDataStore(ev, s_switches);
                        }
                    }
                    else
                    {
                        LogDataStore(ev, null);
                    }

                    static void LogDataStore(RuntimeEventSource ev, Dictionary<string, bool>? switches)
                    {
                        Debug.Assert(s_dataStore is not null);
                        foreach (KeyValuePair<string, object?> kvp in s_dataStore)
                        {
                            if (kvp.Value is string s &&
                                bool.TryParse(s, out bool isEnabled) &&
                                switches?.ContainsKey(kvp.Key) != true)
                            {
                                ev.LogAppContextSwitch(kvp.Key, isEnabled ? 1 : 0);
                            }
                        }
                    }
                }
            }
        }
#endif // FEATURE_PERFTRACING
    }
}
