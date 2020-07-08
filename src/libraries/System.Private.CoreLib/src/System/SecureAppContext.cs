// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>
    /// Represents AppContext switches that were in effect when the AppDomain was created.
    /// </summary>
    /// <remarks>
    /// This class provides a place to store the values of security-sensitive switches as they
    /// were when the application started. Attackers cannot use standard "open reflection"
    /// gadgets to modify these fields since the reflection stack forbids altering the contents
    /// of static initonly fields. This provides an extra layer of defense for applications
    /// which rely on these switches as part of an overall attack surface reduction strategy.
    ///
    /// This is not meant to be a perfect defense. A caller can always use unsafe code to modify
    /// these static fields. However, we assume such a caller is already running code within the
    /// process. Arbitrary memory writes can also alter these fields. Both of these scenarios are
    /// outside the scope of our threat model.
    /// </remarks>
    internal static class SecureAppContext
    {
        // Important: this field should be annotated 'static readonly'
        private static readonly Switches s_switches = InitSwitches();

        private static Switches InitSwitches()
        {
            return new Switches()
            {
                BinaryFormatterEnabled = GetSwitchValue("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization"),
                SerializationGuardEnabled = GetSwitchValue("Switch.System.Runtime.Serialization.SerializationGuard"),
            };
        }

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

        /// <summary>
        /// Returns a value stating whether BinaryFormatter serialization is allowed.
        /// </summary>
        internal static bool BinaryFormatterEnabled => s_switches.BinaryFormatterEnabled;

        /// <summary>
        /// Returns a value stating whether Serialization Guard is enabled.
        /// </summary>
        internal static bool SerializationGuardEnabled => s_switches.SerializationGuardEnabled;

        private struct Switches
        {
            internal bool BinaryFormatterEnabled { get; init; }
            internal bool SerializationGuardEnabled { get; init; }
        }
    }
}
