// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;

namespace System.Runtime.Versioning
{
    public sealed class FrameworkName : IEquatable<FrameworkName>
    {
        // ---- SECTION:  members supporting exposed properties -------------*
        #region members supporting exposed properties
        private readonly String _identifier = null;
        private readonly Version _version = null;
        private readonly String _profile = null;
        private String _fullName = null;

        private const Char c_componentSeparator = ',';
        private const Char c_keyValueSeparator = '=';
        private const Char c_versionValuePrefix = 'v';
        private const String c_versionKey = "Version";
        private const String c_profileKey = "Profile";
        #endregion members supporting exposed properties


        // ---- SECTION: public properties --------------*
        #region public properties
        public String Identifier
        {
            get
            {
                Debug.Assert(_identifier != null);
                return _identifier;
            }
        }

        public Version Version
        {
            get
            {
                Debug.Assert(_version != null);
                return _version;
            }
        }

        public String Profile
        {
            get
            {
                Debug.Assert(_profile != null);
                return _profile;
            }
        }

        public String FullName
        {
            get
            {
                if (_fullName == null)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Identifier);
                    sb.Append(c_componentSeparator);
                    sb.Append(c_versionKey).Append(c_keyValueSeparator);
                    sb.Append(c_versionValuePrefix);
                    sb.Append(Version);
                    if (!String.IsNullOrEmpty(Profile))
                    {
                        sb.Append(c_componentSeparator);
                        sb.Append(c_profileKey).Append(c_keyValueSeparator);
                        sb.Append(Profile);
                    }
                    _fullName = sb.ToString();
                }
                Debug.Assert(_fullName != null);
                return _fullName;
            }
        }
        #endregion public properties


        // ---- SECTION: public instance methods --------------*
        #region public instance methods

        public override Boolean Equals(Object obj)
        {
            return Equals(obj as FrameworkName);
        }

        public Boolean Equals(FrameworkName other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }

            return Identifier == other.Identifier &&
                Version == other.Version &&
                Profile == other.Profile;
        }

        public override Int32 GetHashCode()
        {
            return Identifier.GetHashCode() ^ Version.GetHashCode() ^ Profile.GetHashCode();
        }

        public override String ToString()
        {
            return FullName;
        }
        #endregion public instance methods


        // -------- SECTION: constructors -----------------*
        #region constructors

        public FrameworkName(String identifier, Version version)
            : this(identifier, version, null)
        { }

        public FrameworkName(String identifier, Version version, String profile)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            identifier = identifier.Trim();
            if (identifier.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, "identifier"), nameof(identifier));
            }
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            _identifier = identifier;

            // Ensure we call the correct Version constructor to clone the Version
            if (version.Revision < 0)
            {
                if (version.Build < 0)
                {
                    _version = new Version(version.Major, version.Minor);
                }
                else
                {
                    _version = new Version(version.Major, version.Minor, version.Build);
                }
            }
            else
            {
                _version = new Version(version.Major, version.Minor, version.Build, version.Revision);
            }

            _profile = (profile == null) ? String.Empty : profile.Trim();
        }

        // Parses strings in the following format: "<identifier>, Version=[v|V]<version>, Profile=<profile>"
        //  - The identifier and version is required, profile is optional
        //  - Only three components are allowed.
        //  - The version string must be in the System.Version format; an optional "v" or "V" prefix is allowed
        public FrameworkName(String frameworkName)
        {
            if (frameworkName == null)
            {
                throw new ArgumentNullException(nameof(frameworkName));
            }
            if (frameworkName.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, "frameworkName"), nameof(frameworkName));
            }

            string[] components = frameworkName.Split(c_componentSeparator);

            // Identifer and Version are required, Profile is optional.
            if (components.Length < 2 || components.Length > 3)
            {
                throw new ArgumentException(SR.Argument_FrameworkNameTooShort, nameof(frameworkName));
            }

            //
            // 1) Parse the "Identifier", which must come first. Trim any whitespace
            //
            _identifier = components[0].Trim();

            if (_identifier.Length == 0)
            {
                throw new ArgumentException(SR.Argument_FrameworkNameInvalid, nameof(frameworkName));
            }

            bool versionFound = false;
            _profile = String.Empty;

            // 
            // The required "Version" and optional "Profile" component can be in any order
            //
            for (int i = 1; i < components.Length; i++)
            {
                // Get the key/value pair separated by '='
                string[] keyValuePair = components[i].Split(c_keyValueSeparator);

                if (keyValuePair.Length != 2)
                {
                    throw new ArgumentException(SR.Argument_FrameworkNameInvalid, nameof(frameworkName));
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
                    try
                    {
                        _version = new Version(value);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException(SR.Argument_FrameworkNameInvalidVersion, nameof(frameworkName), e);
                    }
                }
                //
                // 3) Parse the optional "Profile" key value
                //
                else if (key.Equals(c_profileKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (!String.IsNullOrEmpty(value))
                    {
                        _profile = value;
                    }
                }
                else
                {
                    throw new ArgumentException(SR.Argument_FrameworkNameInvalid, nameof(frameworkName));
                }
            }

            if (!versionFound)
            {
                throw new ArgumentException(SR.Argument_FrameworkNameMissingVersion, nameof(frameworkName));
            }
        }
        #endregion constructors


        // -------- SECTION: public static methods -----------------*
        #region public static methods
        public static Boolean operator ==(FrameworkName left, FrameworkName right)
        {
            if (Object.ReferenceEquals(left, null))
            {
                return Object.ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        public static Boolean operator !=(FrameworkName left, FrameworkName right)
        {
            return !(left == right);
        }
        #endregion public static methods
    }
}
