// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security;
using System.Threading;

namespace System.Runtime.Serialization
{
    /// <summary>The structure for holding all of the data needed for object serialization and deserialization.</summary>
    public sealed partial class SerializationInfo
    {
        internal static AsyncLocal<bool> AsyncDeserializationInProgress { get; } = new AsyncLocal<bool>();

        [ThreadStatic]
        private static DeserializationTracker? t_deserializationTracker;

        private static DeserializationTracker GetThreadDeserializationTracker() =>
            t_deserializationTracker ??= new DeserializationTracker();

        // Returns true if deserialization is currently in progress
        internal static bool DeserializationInProgress
        {
            get
            {
                if (AsyncDeserializationInProgress.Value)
                {
                    return true;
                }

                DeserializationTracker tracker = GetThreadDeserializationTracker();
                bool result = tracker.DeserializationInProgress;
                return result;
            }
        }

        // Throws a DeserializationBlockedException if dangerous deserialization is currently
        // in progress and the AppContext switch Switch.System.Runtime.Serialization.SerializationGuard.{switchSuffix}
        // is not true. The value of the switch is cached in cachedValue to avoid repeated lookups:
        // 0: No value cached
        // 1: The switch is true
        // -1: The switch is false
        internal static void ThrowIfDeserializationInProgress(string switchSuffix, ref int cachedValue)
        {
            const string SwitchPrefix = "Switch.System.Runtime.Serialization.SerializationGuard.";
            Debug.Assert(!string.IsNullOrWhiteSpace(switchSuffix));

            if (cachedValue == 0)
            {
                if (AppContext.TryGetSwitch(SwitchPrefix + switchSuffix, out bool isEnabled) && isEnabled)
                {
                    cachedValue = 1;
                }
                else
                {
                    cachedValue = -1;
                }
            }

            if (cachedValue == 1)
            {
                return;
            }
            else if (cachedValue == -1)
            {
                if (DeserializationInProgress)
                {
                    throw new SerializationException(SR.Format(SR.Serialization_DangerousDeserialization_Switch, SwitchPrefix + switchSuffix));
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(cachedValue));
            }
        }

        // Declares that the current thread and async context have begun deserialization.
        // In this state, if the SerializationGuard or other related AppContext switches are set,
        // actions likely to be dangerous during deserialization, such as starting a process will be blocked.
        // Returns a DeserializationToken that must be disposed to remove the deserialization state.
        public static DeserializationToken StartDeserialization()
        {
            if (LocalAppContextSwitches.SerializationGuard)
            {
                DeserializationTracker tracker = GetThreadDeserializationTracker();
                if (!tracker.DeserializationInProgress)
                {
                    lock (tracker)
                    {
                        if (!tracker.DeserializationInProgress)
                        {
                            AsyncDeserializationInProgress.Value = true;
                            tracker.DeserializationInProgress = true;
                            return new DeserializationToken(tracker);
                        }
                    }
                }
            }

            return new DeserializationToken(null);
        }
    }
}
