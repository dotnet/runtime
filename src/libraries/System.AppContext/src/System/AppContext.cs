// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Windows.ApplicationModel;

namespace System
{
    public static class AppContext
    {
        [Flags]
        private enum SwitchValueState
        {
            HasFalseValue = 0x1,
            HasTrueValue = 0x2,
            HasLookedForOverride = 0x4, // Not used on .NET Native
            UnknownValue = 0x8 // Not used on .NET Native
        }

        private static readonly Dictionary<string, SwitchValueState> s_switchMap = new Dictionary<string, SwitchValueState>();

        public static string BaseDirectory
        {
            get
            {
                return Package.Current.InstalledLocation.Path;
            }
        }

        public static string TargetFrameworkName
        {
            get
            {
                // We are currently hard coding this.
                return ".NETCore,Version=v5.0";
            }
        }

        /// <summary>
        /// Try to get the value of the switch.
        /// </summary>
        /// <param name="switchName">The name of the switch</param>
        /// <param name="isEnabled">A variable where to place the value of the switch</param>
        /// <returns>A return value of true represents that the switch was set and <paramref name="isEnabled"/> contains the value of the switch</returns>
        public static bool TryGetSwitch(string switchName, out bool isEnabled)
        {
            if (switchName == null)
                throw new ArgumentNullException(nameof(switchName));
            if (switchName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(switchName));

            // By default, the switch is not enabled.
            isEnabled = false;

            SwitchValueState switchValue;
            lock (s_switchMap)
            {
                if (!s_switchMap.TryGetValue(switchName, out switchValue))
                    return false; // we did not find a value for the switch
            }

            // We get the value of isEnabled from the value that we stored in the dictionary
            isEnabled = (switchValue & SwitchValueState.HasTrueValue) == SwitchValueState.HasTrueValue;
            return true;
        }

        /// <summary>
        /// Assign a switch a value
        /// </summary>
        /// <param name="switchName">The name of the switch</param>
        /// <param name="isEnabled">The value to assign</param>
        public static void SetSwitch(string switchName, bool isEnabled)
        {
            if (switchName == null)
                throw new ArgumentNullException(nameof(switchName));
            if (switchName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(switchName));

            SwitchValueState switchValue = isEnabled ? SwitchValueState.HasTrueValue : SwitchValueState.HasFalseValue;

            lock (s_switchMap)
            {
                // Store the new value and the fact that we checked in the dictionary
                s_switchMap[switchName] = switchValue;
            }
        }
    }
}
