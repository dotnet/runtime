// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    [FriendAccessAllowed]
    internal static class CompatibilitySwitches
    {
        private static bool s_AreSwitchesSet;
        private static bool s_useLatestBehaviorWhenTFMNotSpecified; // Which behavior to use when the TFM is not specified.

        public static bool IsCompatibilityBehaviorDefined
        {
            get
            {
                return s_AreSwitchesSet;
            }
        }

        private static bool IsCompatibilitySwitchSet(string compatibilitySwitch)
        {
            bool? result = AppDomain.CurrentDomain.IsCompatibilitySwitchSet(compatibilitySwitch);
            return (result.HasValue && result.Value);
        }

        internal static void InitializeSwitches()
        {
            s_AreSwitchesSet = true;
        }

        public static bool IsNetFx40TimeSpanLegacyFormatMode
        {
            get
            {
                return false;
            }
        }

        public static bool IsNetFx40LegacySecurityPolicy
        {
            get
            {
                return false;
            }
        }

        public static bool IsNetFx45LegacyManagedDeflateStream
        {
            get
            {
                return false;
            }
        }
    }
}
