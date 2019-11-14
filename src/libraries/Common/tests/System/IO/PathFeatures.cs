// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;

namespace System.IO
{
    public static class PathFeatures
    {
        private enum State
        {
            Uninitialized,
            True,
            False
        }

        // Note that this class is using APIs that allow it to run on all platforms (including Core 5.0)
        // That is why we have .GetTypeInfo(), don't use the Registry, etc...

        private static State s_osEnabled;
        private static State s_onCore;

        /// <summary>
        /// Returns true if you can use long paths, including long DOS style paths (e.g. over 260 without \\?\).
        /// </summary>
        public static bool AreAllLongPathsAvailable()
        {
            // We have support built-in for all platforms in Core
            if (RunningOnCoreLib)
                return true;

            // Otherwise we're running on Windows, see if we've got the capability in .NET, and that the feature is enabled in the OS
            return !AreLongPathsBlocked() && AreOsLongPathsEnabled();
        }

        public static bool IsUsingLegacyPathNormalization()
        {
            return HasLegacyIoBehavior("UseLegacyPathHandling");
        }

        /// <summary>
        /// Returns true if > MAX_PATH (260) character paths are blocked.
        /// Note that this doesn't reflect that you can actually use long paths without device syntax when on Windows.
        /// Use AreAllLongPathsAvailable() to see that you can use long DOS style paths if on Windows.
        /// </summary>
        public static bool AreLongPathsBlocked()
        {
            return HasLegacyIoBehavior("BlockLongPaths");
        }

        private static bool HasLegacyIoBehavior(string propertyName)
        {
            // Core doesn't have legacy behaviors
            if (RunningOnCoreLib)
                return false;

            Type t = typeof(object).GetTypeInfo().Assembly.GetType("System.AppContextSwitches");
            var p = t.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);

            // If the switch actually exists use it, otherwise we predate the switch and are effectively on
            return (bool)(p?.GetValue(null) ?? true);
        }

        private static bool RunningOnCoreLib
        {
            get
            {
                // Not particularly elegant
                if (s_onCore == State.Uninitialized)
                    s_onCore = typeof(object).GetTypeInfo().Assembly.GetName().Name == "System.Private.CoreLib" ? State.True : State.False;

                return s_onCore == State.True;
            }
        }

        private static bool AreOsLongPathsEnabled()
        {
            if (s_osEnabled == State.Uninitialized)
            {
                // No official way to check yet this is good enough for tests
                try
                {
                    s_osEnabled = RtlAreLongPathsEnabled() ? State.True : State.False;
                }
                catch
                {
                    s_osEnabled = State.False;
                }
            }

            return s_osEnabled == State.True;
        }

        [DllImport("ntdll", ExactSpelling = true)]
        private static extern bool RtlAreLongPathsEnabled();
    }
}
