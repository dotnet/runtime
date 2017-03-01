// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;

namespace System.Configuration
{
    // This is a class that abstracts the usage of the override mode setting
    internal struct OverrideModeSetting
    {
        private const byte ApiDefinedLegacy = 0x10; // allowOverride was set through the API
        private const byte ApiDefinedNewMode = 0x20; // overrideMode was set through the API

        // allowOverride or overrideMode was set through the API
        private const byte ApiDefinedAny = ApiDefinedLegacy | ApiDefinedNewMode;
        private const byte XmlDefinedLegacy = 0x40; // allowOverride was defined in the XML
        private const byte XmlDefinedNewMode = 0x80; // overrideMode was defined in the XML

        // overrideMode or allowOverride was defined in the XML
        private const byte XmlDefinedAny = XmlDefinedLegacy | XmlDefinedNewMode;

        private const byte ModeMask = 0x0f; // logical AND this with the current value to get the mode part only

        private byte _mode;

        internal static OverrideModeSetting s_sectionDefault;
        internal static OverrideModeSetting s_locationDefault;

        static OverrideModeSetting()
        {
            // Default for section is ALLOW
            s_sectionDefault = new OverrideModeSetting { _mode = (byte)OverrideMode.Allow };

            // Default for location tags is INHERIT. Note that we do not make the value as existent in the XML or specified by the API
            s_locationDefault = new OverrideModeSetting { _mode = (byte)OverrideMode.Inherit };
        }

        internal static OverrideModeSetting CreateFromXmlReadValue(bool allowOverride)
        {
            // Create a mode from the old "allowOverride" attribute in the xml

            // The conversion is true -> OverrideMode.Inherit
            // The conversion is false -> OverrideMode.Deny
            // This is consistent with Whidbey where true means true unless there is a false somewhere above
            OverrideModeSetting result = new OverrideModeSetting();

            result.SetMode(allowOverride ? OverrideMode.Inherit : OverrideMode.Deny);
            result._mode |= XmlDefinedLegacy;

            return result;
        }

        internal static OverrideModeSetting CreateFromXmlReadValue(OverrideMode mode)
        {
            OverrideModeSetting result = new OverrideModeSetting();

            result.SetMode(mode);
            result._mode |= XmlDefinedNewMode;

            return result;
        }

        internal static OverrideMode ParseOverrideModeXmlValue(string value, XmlUtil xmlUtil)
        {
            // 'value' is the string representation of OverrideMode enum
            // Try to parse the string to the enum and generate errors if not possible

            switch (value)
            {
                case BaseConfigurationRecord.OverrideModeInherit:
                    return OverrideMode.Inherit;

                case BaseConfigurationRecord.OverrideModeAllow:
                    return OverrideMode.Allow;

                case BaseConfigurationRecord.OverrideModeDeny:
                    return OverrideMode.Deny;

                default:
                    throw new ConfigurationErrorsException(
                        SR.Config_section_override_mode_attribute_invalid,
                        xmlUtil);
            }
        }

        internal static bool CanUseSameLocationTag(OverrideModeSetting x, OverrideModeSetting y)
        {
            // This function tells if the two OverrideModeSettings are compatible enough to be used in only one location tag
            // or each of them should go to a separate one

            // The rules here are ( in order of importance )
            // 1. The effective mode is the same ( we will use only the new OverrideMode to compare )
            // 2. When the mode was changed( i.e. API change ) - both must be changed the same way ( i.e. using either allowOverride or OverrideMode )
            // 3. When mode was not changed the XML - they must've been the same in the xml ( i.e. allowOverride specified on both, or overrideMode or neither of them )

            bool result = x.OverrideMode == y.OverrideMode;
            if (!result) return false;

            // Check for an API change for each setting first
            // If one mode was set through the API - the other mode has to be set in the same way through the API or has to be using the same type in the xml

            // Handle case where "x" was API modified
            if ((x._mode & ApiDefinedAny) != 0) result = IsMatchingApiChangedLocationTag(x, y);
            // Handle case where "y" was API modified
            else
            {
                if ((y._mode & ApiDefinedAny) != 0) result = IsMatchingApiChangedLocationTag(y, x);
                // Handle case where neither "x" nor "y" was API modified
                else
                {
                    // If one of the settings was XML defined - they are a match only if both were XML defined in the same way
                    if (((x._mode & XmlDefinedAny) != 0) ||
                        ((y._mode & XmlDefinedAny) != 0))
                        result = (x._mode & XmlDefinedAny) == (y._mode & XmlDefinedAny);

                    // Neither "x" nor "y" was XML defined - they are a match since they can both go 
                    // to a default <location> with no explicit mode setting written out
                }
            }

            return result;
        }

        private static bool IsMatchingApiChangedLocationTag(OverrideModeSetting x, OverrideModeSetting y)
        {
            // x must be a changed through the API setting
            // Returns true if x and y can share the same location tag

            Debug.Assert((x._mode & ApiDefinedAny) != 0);

            bool result = false;

            if ((y._mode & ApiDefinedAny) != 0)
            {
                // If "y" was modified through the API as well - the modified setting must be the same ( i.e. allowOvverride or overrideMode must be modified in both settings )
                result = (x._mode & ApiDefinedAny) == (y._mode & ApiDefinedAny);
            }
            else
            {
                // "y" was not API modified  - they are still a match if "y" was a XML setting from the same mode
                if ((y._mode & XmlDefinedAny) != 0)
                {
                    // "x" was API changed in Legacy and "y" was XML defined in Legacy
                    result = (((x._mode & ApiDefinedLegacy) != 0) && ((y._mode & XmlDefinedLegacy) != 0)) ||
                    // "x" was API changed in New and "y" was XML defined in New
                        (((x._mode & ApiDefinedNewMode) != 0) && ((y._mode & XmlDefinedNewMode) != 0));
                }
                // "y" was not API or XML modified - since "x" was API modified - they are not a match ( i.e. "y" should go to an <location> with no explicit mode written out )
            }

            return result;
        }

        internal bool IsDefaultForSection
        {
            get
            {
                // Returns true if the current value of the overrideMode setting is the default one on a section declaration

                // The current default value for a section's overrideMode ( i.e. overrideModeDefault ) is Allow ( see CreateDefaultForSection )
                // It would've been nice not to repeat that rule here but since OverrideMode.Inherited means the same in this specific context we have to
                // I.e. the default for a section is both Allow and Inherited. In this case they mean the same

                OverrideMode mode = OverrideMode;

                // Return  true if mode is Allow or Inherit
                return (mode == OverrideMode.Allow) || (mode == OverrideMode.Inherit);
            }
        }

        internal bool IsDefaultForLocationTag
        {
            get
            {
                // Returns true if the current setting is the same as the default value for a location tag
                // Note that if the setting was an API modified or XmlDefined  it is not a default since it
                // cannot go to the default <location> tag which does not explicitlly specify a mode

                OverrideModeSetting defaultSetting = s_locationDefault;

                return (defaultSetting.OverrideMode == OverrideMode) &&
                    ((_mode & ApiDefinedAny) == 0) &&
                    ((_mode & XmlDefinedAny) == 0);
            }
        }

        internal bool IsLocked => OverrideMode == OverrideMode.Deny;

        internal string LocationTagXmlString
        {
            get
            {
                // Returns the string for this setting which is to be written in the xml <location> tag

                string result = string.Empty;

                bool needToWrite = false;
                bool useLegacy = false;

                // If there was an API change - it has highest priority
                if ((_mode & ApiDefinedAny) != 0)
                {
                    // Whichever was changed by the API dictates what is to be written
                    useLegacy = (_mode & ApiDefinedLegacy) != 0;
                    needToWrite = true;

                    Debug.Assert(useLegacy || ((_mode & ApiDefinedNewMode) != 0));
                }
                // It wasn't changed through the API - check if it was read originally from the XML
                else
                {
                    if ((_mode & XmlDefinedAny) != 0)
                    {
                        // Whatever was defined in the XML is to be written out

                        useLegacy = (_mode & XmlDefinedLegacy) != 0;
                        needToWrite = true;

                        Debug.Assert(useLegacy || ((_mode & XmlDefinedNewMode) != 0));
                    }
                }

                if (needToWrite)
                {
                    string value;
                    string attrib;

                    if (useLegacy)
                    {
                        // Legacy - allowOverride
                        attrib = BaseConfigurationRecord.LocationAllowOverrideAttribute;
                        value = AllowOverride
                            ? BaseConfigurationRecord.KeywordTrue
                            : BaseConfigurationRecord.KeywordFalse;
                    }
                    else
                    {
                        attrib = BaseConfigurationRecord.LocationOverrideModeAttribute;
                        value = OverrideModeXmlValue;
                    }

                    result = string.Format(CultureInfo.InvariantCulture,
                        BaseConfigurationRecord.KeywordLocationOverrideModeString, attrib, value);
                }

                return result;
            }
        }

        internal string OverrideModeXmlValue
        {
            get
            {
                // Returns the xml (string) value of the current setting for override mode
                switch (OverrideMode)
                {
                    case OverrideMode.Inherit:
                        return BaseConfigurationRecord.OverrideModeInherit;
                    case OverrideMode.Allow:
                        return BaseConfigurationRecord.OverrideModeAllow;
                    case OverrideMode.Deny:
                        return BaseConfigurationRecord.OverrideModeDeny;
                    default:
                        Debug.Fail("Missing xml keyword for OverrideMode enum value");
                        break;
                }

                return null;
            }
        }

        // Use this method to change only the value of the setting when not done through the public API
        internal void ChangeModeInternal(OverrideMode mode)
        {
            SetMode(mode);
        }

        // Properties to enable external chnages to the mode.
        // Note that those changes will be tracked as made by the public API
        // There shouldn't be a reason for those to be used except in this specific case
        internal OverrideMode OverrideMode
        {
            get { return (OverrideMode)(_mode & ModeMask); }
            set
            {
                // Note that changing the mode through the API ( which is the purpose of this setter )
                // overrides the setting in the XML ( if any )
                // and hence we dont keep a track of it anymore

                // Error condition: We do not allow changes to the mode through both AllowOverride and OverrideMode
                // If one was changed first we require that API users stick with it
                VerifyConsistentChangeModel(ApiDefinedNewMode);

                SetMode(value);
                _mode |= ApiDefinedNewMode;
            }
        }

        internal bool AllowOverride
        {
            get
            {
                bool result = true;

                switch (OverrideMode)
                {
                    case OverrideMode.Inherit:
                    case OverrideMode.Allow:
                        break;
                    case OverrideMode.Deny:
                        result = false;
                        break;
                    default:
                        Debug.Assert(false, "Unrecognized OverrideMode");
                        break;
                }

                return result;
            }
            set
            {
                // Note that changing the mode through the API ( which is the purpose of this setter )
                // overrides the setting in the XML ( if any )
                // and hence we dont keep a track of it anymore

                // Error condition: We do not allow changes to the mode through both AllowOverride and OverrideMode
                // If one was changed first we require that API users stick with it
                VerifyConsistentChangeModel(ApiDefinedLegacy);

                SetMode(value ? OverrideMode.Inherit : OverrideMode.Deny);
                _mode |= ApiDefinedLegacy;
            }
        }

        private void SetMode(OverrideMode mode)
        {
            _mode = (byte)mode;
        }

        private void VerifyConsistentChangeModel(byte required)
        {
            // The required API change model ( i.e. was allowOverride used or OverrideMode ) should be consistent
            // I.e. its not possible to change both on the same OverrideModeSetting object
            byte current = (byte)(_mode & ApiDefinedAny);

            // Shows whats the current setting: 0 ( none ), ApiDefinedLegacy or ApiDefinedNew
            if ((current != 0) && (current != required))
                throw new ConfigurationErrorsException(SR.Cannot_change_both_AllowOverride_and_OverrideMode);
        }
    }
}
