// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System
{
    /// <summary>
    /// Represents AppContext switches that were in effect when the AppDomain was created.
    /// </summary>
    /// <remarks>
    /// This class provides a place to store the values of security-sensitive switches as they
    /// were when the application started. It guards against dependency code inadvertently
    /// calling AppContext.SetSwitch and subverting app-level policy.
    ///
    /// This is not meant to be a perfect defense. A determined caller can always use private
    /// reflection to modify the contents of these switches. But that doesn't fall under the
    /// realm of "inadvertent" so is outside the scope of our threat model.
    /// </remarks>
    internal static class SecureAppContext
    {
#if DEBUG
        private static bool s_isInitialized;
#endif

        /// <summary>
        /// Returns a value stating whether BinaryFormatter serialization is allowed.
        /// </summary>
        internal static bool BinaryFormatterEnabled { get; private set; }

        /// <summary>
        /// Returns a value stating whether Serialization Guard is enabled.
        /// </summary>
        internal static bool SerializationGuardEnabled { get; private set; }

        private static bool GetSwitchValue(string switchName)
        {
            // These calls take place very early in the application's initialization,
            // even before Program.Main is invoked. We only want to capture the state
            // that was present when the AppDomain was initially created. If we were
            // to call the LocalAppContextSwitches class's static property getters here,
            // we'd end up permanently locking those values and not giving the app
            // developer the change to programmatically override them.

            int cachedValue = 0; // = unknown, will be discarded
            return LocalAppContextSwitches.GetCachedSwitchValue(switchName, ref cachedValue);
        }

        internal static void Initialize()
        {
#if DEBUG
            Debug.Assert(!s_isInitialized, "Initialize shouldn't be called multiple times.");
            s_isInitialized = true;
#endif

            BinaryFormatterEnabled = GetSwitchValue("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization");
            SerializationGuardEnabled = GetSwitchValue("Switch.System.Runtime.Serialization.SerializationGuard");
        }
    }
}
