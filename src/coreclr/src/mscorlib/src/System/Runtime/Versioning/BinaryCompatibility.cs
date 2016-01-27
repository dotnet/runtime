// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
**
**
** Purpose: This class is used to determine which binary compatibility
**  behaviors are enabled at runtime.  A type for 
**  tracking which target Framework an app was built against, or an 
**  appdomain-wide setting from the host telling us which .NET 
**  Framework version we should emulate.
**
** 
===========================================================*/
using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Runtime.Versioning
{
    // Provides a simple way to test whether an application was built against specific .NET Framework
    // flavors and versions, with the intent of allowing Framework developers to mimic behavior of older
    // Framework releases.  This allows us to make behavioral breaking changes in a binary compatible way,
    // for an application.  This works at the per-AppDomain level, not process nor per-Assembly.
    // 
    // To opt into newer behavior, applications must specify a TargetFrameworkAttribute on their assembly
    // saying what version they targeted, or a host must set this when creating an AppDomain.  Note
    // that command line apps don't have this attribute!
    // 
    // To use this class:
    // Developers need to figure out whether they're working on the phone, desktop, or Silverlight, and
    // what version they are introducing a breaking change in.  Pick one predicate below, and use that
    // to decide whether to run the new or old behavior.  Example:
    //
    // if (BinaryCompatibility.TargetsAtLeast_Phone_V7_1) {
    //     // new behavior for phone 7.1 and other releases where we will integrate this change, like .NET Framework 4.5
    // }
    // else {
    //     // Legacy behavior
    // }
    //
    // If you are making a breaking change in one specific branch that won't be integrated normally to
    // all other branches (ie, say you're making breaking changes to Windows Phone 8 after .NET Framework v4.5
    // has locked down for release), then add in specific predicates for each relevant platform.
    // 
    // Maintainers of this class:
    // Revisit the table once per release, perhaps at the end of the last coding milestone, to verify a
    // default policy saying whether all quirks from a particular flavor & release should be enabled in
    // other releases (ie, should all Windows Phone 8.0 quirks be enabled in .NET Framework v5)?  
    // 
    // History:
    // Here is the order in which releases were made along with some basic integration information.  The idea
    // is to track what set of compatibility features are present in each other.
    // While we cannot guarantee this list is perfectly linear (ie, a feature could be implemented in the last
    // few weeks before shipping and make it into only one of three concommittent releases due to triaging),
    // this is a good high level summary of code flow.
    //
    //            Desktop            Silverlight             Windows Phone
    //      .NET Framework 3.0   ->  Silverlight 2
    //      .NET Framework 3.5
    //                               Silverlight 3
    //                               Silverlight 4
    //      .NET Framework 4                                   Phone 8.0
    //      .NET Framework 4.5                                 Phone 8.1
    //      .NET Framework 4.5.1                               Phone 8.1
    //           
    // (Note: Windows Phone 7.0 was built using the .NET Compact Framework, which forked around v1 or v1.1)
    // 
    // Compatibility Policy decisions:
    //  If we cannot determine that an app was built for a newer .NET Framework (ie, the app has no
    //  TargetFrameworkAttribute), then quirks will be enabled to emulate older behavior.
    //  As such, your test code should define the TargetFrameworkAttribute (which VS does for you)
    //  if you want to see the new behavior!
    [FriendAccessAllowed]
    internal static class BinaryCompatibility
    {
        // Use this for new behavior introduced in the phone branch.  It will do the right thing for desktop & SL.
        [FriendAccessAllowed]
        internal static bool TargetsAtLeast_Phone_V7_1 { [FriendAccessAllowed] get { return s_map.TargetsAtLeast_Phone_V7_1; } }

        [FriendAccessAllowed]
        internal static bool TargetsAtLeast_Phone_V8_0 { [FriendAccessAllowed] get { return s_map.TargetsAtLeast_Phone_V8_0; } }

        // Use this for new behavior introduced in the Desktop branch.  It will do the right thing for Phone & SL.
        [FriendAccessAllowed]
        internal static bool TargetsAtLeast_Desktop_V4_5 { [FriendAccessAllowed] get { return s_map.TargetsAtLeast_Desktop_V4_5; } }
        [FriendAccessAllowed]
        internal static bool TargetsAtLeast_Desktop_V4_5_1 { [FriendAccessAllowed] get { return s_map.TargetsAtLeast_Desktop_V4_5_1; } }
        [FriendAccessAllowed]
        internal static bool TargetsAtLeast_Desktop_V4_5_2 { [FriendAccessAllowed] get { return s_map.TargetsAtLeast_Desktop_V4_5_2; } }
        [FriendAccessAllowed]
        internal static bool TargetsAtLeast_Desktop_V4_5_3 { [FriendAccessAllowed] get { return s_map.TargetsAtLeast_Desktop_V4_5_3; } }
        [FriendAccessAllowed]
        internal static bool TargetsAtLeast_Desktop_V4_5_4 { [FriendAccessAllowed] get { return s_map.TargetsAtLeast_Desktop_V4_5_4; } }

        [FriendAccessAllowed]
        internal static bool TargetsAtLeast_Desktop_V5_0 { [FriendAccessAllowed] get { return s_map.TargetsAtLeast_Desktop_V5_0; } }

        // Use this for new behavior introduced in the Silverlight branch.  It will do the right thing for desktop & Phone.
        [FriendAccessAllowed]
        internal static bool TargetsAtLeast_Silverlight_V4 { [FriendAccessAllowed] get { return s_map.TargetsAtLeast_Silverlight_V4; } }
        [FriendAccessAllowed]
        internal static bool TargetsAtLeast_Silverlight_V5 { [FriendAccessAllowed] get { return s_map.TargetsAtLeast_Silverlight_V5; } }
        [FriendAccessAllowed]
        internal static bool TargetsAtLeast_Silverlight_V6 { [FriendAccessAllowed] get { return s_map.TargetsAtLeast_Silverlight_V6; } }

        [FriendAccessAllowed]
        internal static TargetFrameworkId AppWasBuiltForFramework {
            [FriendAccessAllowed]
            get {
                Contract.Ensures(Contract.Result<TargetFrameworkId>() > TargetFrameworkId.NotYetChecked);

                if (s_AppWasBuiltForFramework == TargetFrameworkId.NotYetChecked)
                    ReadTargetFrameworkId();

                return s_AppWasBuiltForFramework;
            }
        }

        // Version number is major * 10000 + minor * 100 + build  (ie, 4.5.1.0 would be version 40501).
        [FriendAccessAllowed]
        internal static int AppWasBuiltForVersion {
            [FriendAccessAllowed]
            get {
                Contract.Ensures(Contract.Result<int>() > 0 || s_AppWasBuiltForFramework == TargetFrameworkId.Unspecified);

                if (s_AppWasBuiltForFramework == TargetFrameworkId.NotYetChecked)
                    ReadTargetFrameworkId();

                Contract.Assert(s_AppWasBuiltForFramework != TargetFrameworkId.Unrecognized);

                return s_AppWasBuiltForVersion;
            }
        }

        #region private
        private static TargetFrameworkId s_AppWasBuiltForFramework;
        // Version number is major * 10000 + minor * 100 + build (ie, 4.5.1.0 would be version 40501).
        private static int s_AppWasBuiltForVersion;

        readonly static BinaryCompatibilityMap s_map = new BinaryCompatibilityMap();
        
        // For parsing a target Framework moniker, from the FrameworkName class
        private const char c_componentSeparator = ',';
        private const char c_keyValueSeparator = '=';
        private const char c_versionValuePrefix = 'v';
        private const String c_versionKey = "Version";
        private const String c_profileKey = "Profile";

        /// <summary>
        /// BinaryCompatibilityMap is basically a bitvector.  There is a boolean field for each of the
        /// properties in BinaryCompatibility
        /// </summary>
        private sealed class BinaryCompatibilityMap
        {
            // A bit for each property 
            internal bool TargetsAtLeast_Phone_V7_1;
            internal bool TargetsAtLeast_Phone_V8_0;
            internal bool TargetsAtLeast_Phone_V8_1;
            internal bool TargetsAtLeast_Desktop_V4_5;
            internal bool TargetsAtLeast_Desktop_V4_5_1;
            internal bool TargetsAtLeast_Desktop_V4_5_2;
            internal bool TargetsAtLeast_Desktop_V4_5_3;
            internal bool TargetsAtLeast_Desktop_V4_5_4;
            internal bool TargetsAtLeast_Desktop_V5_0;
            internal bool TargetsAtLeast_Silverlight_V4;
            internal bool TargetsAtLeast_Silverlight_V5;
            internal bool TargetsAtLeast_Silverlight_V6;

            internal BinaryCompatibilityMap()
            {
                AddQuirksForFramework(AppWasBuiltForFramework, AppWasBuiltForVersion);
            }

            // The purpose of this method is to capture information about integrations & behavioral compatibility
            // between our multiple different release vehicles.  IE, if a behavior shows up in Silverlight version 5,
            // does it show up in the .NET Framework version 4.5 and Windows Phone 8?
            // Version number is major * 10000 + minor * 100 + build (ie, 4.5.1.0 would be version 40501).
            private void AddQuirksForFramework(TargetFrameworkId builtAgainstFramework, int buildAgainstVersion)
            {
                Contract.Requires(buildAgainstVersion > 0  || builtAgainstFramework == TargetFrameworkId.Unspecified);

                switch (builtAgainstFramework)
                {
                    case TargetFrameworkId.NetFramework:
                    case TargetFrameworkId.NetCore:   // Treat Windows 8 tailored apps as normal desktop apps - same product
                        if (buildAgainstVersion >= 50000)
                            TargetsAtLeast_Desktop_V5_0 = true;

                        // Potential 4.5 servicing releases
                        if (buildAgainstVersion >= 40504)
                            TargetsAtLeast_Desktop_V4_5_4 = true;
                        if (buildAgainstVersion >= 40503)
                            TargetsAtLeast_Desktop_V4_5_3 = true;
                        if (buildAgainstVersion >= 40502)
                            TargetsAtLeast_Desktop_V4_5_2 = true;
                        if (buildAgainstVersion >= 40501)
                            TargetsAtLeast_Desktop_V4_5_1 = true;

                        if (buildAgainstVersion >= 40500)
                        {
                            TargetsAtLeast_Desktop_V4_5 = true;
                            // On XX/XX/XX we integrated all changes from the phone V7_1 into the branch from which contains Desktop V4_5, thus 
                            // Any application built for V4_5 (or above) should have all the quirks for Phone V7_1 turned on.
                            AddQuirksForFramework(TargetFrameworkId.Phone, 70100);
                            // All Silverlight 5 behavior should be in the .NET Framework version 4.5
                            AddQuirksForFramework(TargetFrameworkId.Silverlight, 50000);
                        }
                        break;

                    case TargetFrameworkId.Phone:
                        if (buildAgainstVersion >= 80000)
                        {
                            // This is for Apollo apps. For Apollo apps we don't want to enable any of the 4.5 or 4.5.1 quirks
                            TargetsAtLeast_Phone_V8_0 = true;
                            //TargetsAtLeast_Desktop_V4_5 = true;
                        }
                        if (buildAgainstVersion >= 80100)
                        {
                            // For WindowsPhone 8.1 and SL 8.1 scenarios we want to enable both 4.5 and 4.5.1 quirks.
                            TargetsAtLeast_Desktop_V4_5 = true;
                            TargetsAtLeast_Desktop_V4_5_1 = true;
                        }

                        if (buildAgainstVersion >= 710)
                            TargetsAtLeast_Phone_V7_1 = true;
                        break;

                    case TargetFrameworkId.Silverlight:
                        if (buildAgainstVersion >= 40000)
                            TargetsAtLeast_Silverlight_V4 = true;

                        if (buildAgainstVersion >= 50000)
                            TargetsAtLeast_Silverlight_V5 = true;

                        if (buildAgainstVersion >= 60000)
                        {
                            TargetsAtLeast_Silverlight_V6 = true;
                        }
                        break;

                    case TargetFrameworkId.Unspecified:
                        break;

                    case TargetFrameworkId.NotYetChecked:
                    case TargetFrameworkId.Unrecognized:
                        Contract.Assert(false, "Bad framework kind");
                        break;
                    default:
                        Contract.Assert(false, "Error: we introduced a new Target Framework but did not update our binary compatibility map");
                        break;
                }
            }
        }

        #region String Parsing

        // If this doesn't work, perhaps we could fall back to parsing the metadata version number.
        private static bool ParseTargetFrameworkMonikerIntoEnum(String targetFrameworkMoniker, out TargetFrameworkId targetFramework, out int targetFrameworkVersion)
        {
            Contract.Requires(!String.IsNullOrEmpty(targetFrameworkMoniker));

            targetFramework = TargetFrameworkId.NotYetChecked;
            targetFrameworkVersion = 0;

            String identifier = null;
            String profile = null;
            ParseFrameworkName(targetFrameworkMoniker, out identifier, out targetFrameworkVersion, out profile);

            switch (identifier)
            {
                case ".NETFramework":
                    targetFramework = TargetFrameworkId.NetFramework;
                    break;

                case ".NETPortable":
                    targetFramework = TargetFrameworkId.Portable;
                    break;

                case ".NETCore":
                    targetFramework = TargetFrameworkId.NetCore;
                    break;

                case "WindowsPhone":
                    if (targetFrameworkVersion >= 80100)
                    {
                        // A TFM of the form WindowsPhone,Version=v8.1 corresponds to SL 8.1 scenario
                        // and gets the same quirks as WindowsPhoneApp\v8.1 store apps.
                        targetFramework = TargetFrameworkId.Phone;
                    }
                    else
                    {
                        // There is no TFM for Apollo or below and hence we assign the targetFramework to Unspecified. 
                        targetFramework = TargetFrameworkId.Unspecified;
                    }
                    break;

                case "WindowsPhoneApp":
                    targetFramework = TargetFrameworkId.Phone;
                    break;

                case "Silverlight":
                    targetFramework = TargetFrameworkId.Silverlight;
                    // Windows Phone 7 is Silverlight,Version=v4.0,Profile=WindowsPhone
                    // Windows Phone 7.1 is Silverlight,Version=v4.0,Profile=WindowsPhone71
                    if (!String.IsNullOrEmpty(profile))
                    {
                        if (profile == "WindowsPhone")
                        {
                            targetFramework = TargetFrameworkId.Phone;
                            targetFrameworkVersion = 70000;
                        }
                        else if (profile == "WindowsPhone71")
                        {
                            targetFramework = TargetFrameworkId.Phone;
                            targetFrameworkVersion = 70100;
                        }
                        else if (profile == "WindowsPhone8") 
                        {
                            targetFramework = TargetFrameworkId.Phone;
                            targetFrameworkVersion = 80000;
                        }
                        else if (profile.StartsWith("WindowsPhone", StringComparison.Ordinal))
                        {
                            Contract.Assert(false, "This is a phone app, but we can't tell what version this is!");
                            targetFramework = TargetFrameworkId.Unrecognized;
                            targetFrameworkVersion = 70100;
                        }
                        else
                        {
                            Contract.Assert(false, String.Format(CultureInfo.InvariantCulture, "Unrecognized Silverlight profile \"{0}\".  What is this, an XBox app?", profile));
                            targetFramework = TargetFrameworkId.Unrecognized;
                        }
                    }
                    break;

                default:
                    Contract.Assert(false, String.Format(CultureInfo.InvariantCulture, "Unrecognized Target Framework Moniker in our Binary Compatibility class.  Framework name: \"{0}\"", targetFrameworkMoniker));
                    targetFramework = TargetFrameworkId.Unrecognized;
                    break;
            }

            return true;
        }

        // This code was a constructor copied from the FrameworkName class, which is located in System.dll.
        // Parses strings in the following format: "<identifier>, Version=[v|V]<version>, Profile=<profile>"
        //  - The identifier and version is required, profile is optional
        //  - Only three components are allowed.
        //  - The version string must be in the System.Version format; an optional "v" or "V" prefix is allowed
        private static void ParseFrameworkName(String frameworkName, out String identifier, out int version, out String profile)
        {
            if (frameworkName == null)
            {
                throw new ArgumentNullException("frameworkName");
            }
            if (frameworkName.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_StringZeroLength"), "frameworkName");
            }
            Contract.EndContractBlock();

            String[] components = frameworkName.Split(c_componentSeparator);
            version = 0;

            // Identifer and Version are required, Profile is optional.
            if (components.Length < 2 || components.Length > 3)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_FrameworkNameTooShort"), "frameworkName");
            }

            //
            // 1) Parse the "Identifier", which must come first. Trim any whitespace
            //
            identifier = components[0].Trim();

            if (identifier.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_FrameworkNameInvalid"), "frameworkName");
            }

            bool versionFound = false;
            profile = null;

            // 
            // The required "Version" and optional "Profile" component can be in any order
            //
            for (int i = 1; i < components.Length; i++)
            {
                // Get the key/value pair separated by '='
                string[] keyValuePair = components[i].Split(c_keyValueSeparator);

                if (keyValuePair.Length != 2)
                {
                    throw new ArgumentException(Environment.GetResourceString("SR.Argument_FrameworkNameInvalid"), "frameworkName");
                }

                // Get the key and value, trimming any whitespace
                string key = keyValuePair[0].Trim();
                string value = keyValuePair[1].Trim();

                //
                // 2) Parse the required "Version" key value
                //
                if (key.Equals(c_versionKey, StringComparison.OrdinalIgnoreCase))
                {
                    versionFound = true;

                    // Allow the version to include a 'v' or 'V' prefix...
                    if (value.Length > 0 && (value[0] == c_versionValuePrefix || value[0] == 'V'))
                    {
                        value = value.Substring(1);
                    }
                    Version realVersion = new Version(value);
                    // The version class will represent some unset values as -1 internally (instead of 0).
                    version = realVersion.Major * 10000;
                    if (realVersion.Minor > 0)
                        version += realVersion.Minor * 100;
                    if (realVersion.Build > 0)
                        version += realVersion.Build;
                }
                //
                // 3) Parse the optional "Profile" key value
                //
                else if (key.Equals(c_profileKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (!String.IsNullOrEmpty(value))
                    {
                        profile = value;
                    }
                }
                else
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_FrameworkNameInvalid"), "frameworkName");
                }
            }

            if (!versionFound)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_FrameworkNameMissingVersion"), "frameworkName");
            }
        }

#if FEATURE_CORECLR
        /// <summary>
        /// This method checks for CompatibilitySwitches for SL8.1 scenarios.
        /// PS - This is used only for SL 8.1
        /// </summary>
        [System.Security.SecuritySafeCritical]
        private static bool IsAppUnderSL81CompatMode()
        {
            Contract.Assert(s_AppWasBuiltForFramework == TargetFrameworkId.NotYetChecked);

            if (CompatibilitySwitches.IsAppSilverlight81)
            {
                // This is an SL8.1 scenario and hence it gets the same quirks as WPBlue+ settings.
                s_AppWasBuiltForFramework = TargetFrameworkId.Phone;
                s_AppWasBuiltForVersion = 80100;

                return true;
            }

            return false;
        }
#endif //FEATURE_CORECLR

        [System.Security.SecuritySafeCritical]
        private static void ReadTargetFrameworkId()
        {
#if FEATURE_CORECLR
            if (IsAppUnderSL81CompatMode())
            {
                // Since the SL does not have any Main() the reading of the TFM will not work and as a workaround we use the CompatibilitySwitch.IsAppSilverlight81 
                // to identify if the given app targets SL 8.1 and accordingly give it the value TargetFrameworkId.Phone;80100

                // PS - This also means that the CompatMode set by AppDomain m_compatFlags with AppDomainCompatMode.APPDOMAINCOMPAT_APP_SL81
                // will override any other mechanism like TFM, RegistryKey, env variable or config file settings. Since this option
                // is only used by SL8.1 scenario's I don't think this is an issue and is rather desirable.

                return;
            }
#endif //FEATURE_CORECLR
            String targetFrameworkName = AppDomain.CurrentDomain.GetTargetFrameworkName();

            var overrideValue = System.Runtime.Versioning.CompatibilitySwitch.GetValueInternal("TargetFrameworkMoniker");
            if (!string.IsNullOrEmpty(overrideValue))
            {
                targetFrameworkName = overrideValue;
            }

            // Write to a local then to _targetFramework, after writing the version number.
            TargetFrameworkId fxId;
            int fxVersion = 0;
            if (targetFrameworkName == null)
            {
#if FEATURE_CORECLR
                // if we don't have a value for targetFrameworkName we need to figure out if we should give the newest behavior or not.
                if (CompatibilitySwitches.UseLatestBehaviorWhenTFMNotSpecified)
                {
                    fxId = TargetFrameworkId.NetFramework;
                    fxVersion = 50000; // We are going to default to the latest value for version that we have in our code.
                }
                else
#endif
                    fxId = TargetFrameworkId.Unspecified;
            }
            else if (!ParseTargetFrameworkMonikerIntoEnum(targetFrameworkName, out fxId, out fxVersion))
                fxId = TargetFrameworkId.Unrecognized;

            s_AppWasBuiltForFramework = fxId;
            s_AppWasBuiltForVersion = fxVersion;
        }
        #endregion String Parsing

        #endregion private
    }
}
