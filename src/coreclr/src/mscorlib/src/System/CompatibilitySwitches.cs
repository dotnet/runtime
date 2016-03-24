// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime;
using System.Runtime.CompilerServices;

namespace System
{
    [FriendAccessAllowed]
    internal static class CompatibilitySwitches
    {
        private static bool s_AreSwitchesSet;

#if FEATURE_CORECLR
        private static bool s_useLatestBehaviorWhenTFMNotSpecified; // Which behavior to use when the TFM is not specified.
#endif //FEATURE_CORECLR

#if !FEATURE_CORECLR
        private static bool s_isNetFx40TimeSpanLegacyFormatMode;
        private static bool s_isNetFx40LegacySecurityPolicy;
        private static bool s_isNetFx45LegacyManagedDeflateStream;
#endif //!FEATURE_CORECLR

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
#if FEATURE_CORECLR
            s_useLatestBehaviorWhenTFMNotSpecified = IsCompatibilitySwitchSet("UseLatestBehaviorWhenTFMNotSpecified");
#endif //FEATURE_CORECLR

#if !FEATURE_CORECLR
            s_isNetFx40TimeSpanLegacyFormatMode = IsCompatibilitySwitchSet("NetFx40_TimeSpanLegacyFormatMode");
            s_isNetFx40LegacySecurityPolicy = IsCompatibilitySwitchSet("NetFx40_LegacySecurityPolicy");
            s_isNetFx45LegacyManagedDeflateStream = IsCompatibilitySwitchSet("NetFx45_LegacyManagedDeflateStream");
#endif //FEATURE_CORECLR

            s_AreSwitchesSet = true;
        }

#if FEATURE_CORECLR
        /// <summary>
        /// This property returns whether to give the latest behavior when the TFM is missing
        /// </summary>
        internal static bool UseLatestBehaviorWhenTFMNotSpecified
        {
            get
            {
                return s_useLatestBehaviorWhenTFMNotSpecified;
            }
        }
#else //FEATURE_CORECLR

        public static bool IsAppEarlierThanSilverlight4
        {
            get
            {
                return false;
            }
        }

        public static bool IsAppEarlierThanWindowsPhone8
        {
            get
            {
                return false;
            }
        }

        public static bool IsAppEarlierThanWindowsPhoneMango
        {
            get
            {
                return false;
            }
        }

#endif //FEATURE_CORECLR

        public static bool IsNetFx40TimeSpanLegacyFormatMode
        {
            get
            {
#if !FEATURE_CORECLR
                return s_isNetFx40TimeSpanLegacyFormatMode;
#else
                return false;
#endif //!FEATURE_CORECLR
            }
        }

        public static bool IsNetFx40LegacySecurityPolicy
        {
            get
            {
#if !FEATURE_CORECLR
                return s_isNetFx40LegacySecurityPolicy;
#else
                return false;
#endif //!FEATURE_CORECLR
            }
        }

        public static bool IsNetFx45LegacyManagedDeflateStream
        {
            get
            {
#if !FEATURE_CORECLR
                return s_isNetFx45LegacyManagedDeflateStream;
#else
                return false;
#endif //!FEATURE_CORECLR
            }
        }
    }
}
