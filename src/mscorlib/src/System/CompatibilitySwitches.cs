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

        internal static void InitializeSwitches()
        {
            s_AreSwitchesSet = true;
        }
    }
}
