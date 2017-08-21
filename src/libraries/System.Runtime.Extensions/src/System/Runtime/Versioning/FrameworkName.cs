// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Runtime.Versioning
{
    public sealed class FrameworkName : IEquatable<FrameworkName>
    {
        private readonly string _identifier;
        private readonly Version _version;
        private readonly string _profile;
        private string _fullName;

        private const char ComponentSeparator = ',';
        private const char KeyValueSeparator = '=';
        private const char VersionValuePrefix = 'v';
        private const string VersionKey = "Version";
        private const string ProfileKey = "Profile";

        private static readonly char[] s_componentSplitSeparator = { ComponentSeparator };

        public string Identifier
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

        public string Profile
        {
            get
            {
                Debug.Assert(_profile != null);
                return _profile;
            }
        }

        public string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    if (string.IsNullOrEmpty(Profile))
                    {
                        _fullName =
                            Identifier +
                            ComponentSeparator + VersionKey + KeyValueSeparator + VersionValuePrefix +
                            Version.ToString();
                    }
                    else
                    {
                        _fullName =
                            Identifier +
                            ComponentSeparator + VersionKey + KeyValueSeparator + VersionValuePrefix +
                            Version.ToString() +
                            ComponentSeparator + ProfileKey + KeyValueSeparator +
                            Profile;
                    }
                }
                Debug.Assert(_fullName != null);
                return _fullName;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FrameworkName);
        }

        public bool Equals(FrameworkName other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            return Identifier == other.Identifier &&
                Version == other.Version &&
                Profile == other.Profile;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode() ^ Version.GetHashCode() ^ Profile.GetHashCode();
        }

        public override string ToString()
        {
            return FullName;
        }

        public FrameworkName(string identifier, Version version)
            : this(identifier, version, null)
        {
        }

        public FrameworkName(string identifier, Version version, string profile)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            identifier = identifier.Trim();
            if (identifier.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(identifier)), nameof(identifier));
            }
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            _identifier = identifier;
            _version = version;
            _profile = (profile == null) ? string.Empty : profile.Trim();
        }

        // Parses strings in the following format: "<identifier>, Version=[v|V]<version>, Profile=<profile>"
        //  - The identifier and version is required, profile is optional
        //  - Only three components are allowed.
        //  - The version string must be in the System.Version format; an optional "v" or "V" prefix is allowed
        public FrameworkName(string frameworkName)
        {
            if (frameworkName == null)
            {
                throw new ArgumentNullException(nameof(frameworkName));
            }
            if (frameworkName.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(frameworkName)), nameof(frameworkName));
            }

            string[] components = frameworkName.Split(s_componentSplitSeparator);

            // Identifier and Version are required, Profile is optional.
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
            _profile = string.Empty;

            //
            // The required "Version" and optional "Profile" component can be in any order
            //
            for (int i = 1; i < components.Length; i++)
            {
                // Get the key/value pair separated by '='
                string component = components[i];
                int separatorIndex = component.IndexOf(KeyValueSeparator);

                if (separatorIndex == -1 || separatorIndex != component.LastIndexOf(KeyValueSeparator))
                {
                    throw new ArgumentException(SR.Argument_FrameworkNameInvalid, nameof(frameworkName));
                }

                // Get the key and value, trimming any whitespace
                string key = component.Substring(0, separatorIndex).Trim();
                string value = component.Substring(separatorIndex + 1).Trim();

                //
                // 2) Parse the required "Version" key value
                //
                if (key.Equals(VersionKey, StringComparison.OrdinalIgnoreCase))
                {
                    versionFound = true;

                    // Allow the version to include a 'v' or 'V' prefix...
                    if (value.Length > 0 && (value[0] == VersionValuePrefix || value[0] == 'V'))
                    {
                        value = value.Substring(1);
                    }
                    try
                    {
                        _version = Version.Parse(value);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException(SR.Argument_FrameworkNameInvalidVersion, nameof(frameworkName), e);
                    }
                }
                //
                // 3) Parse the optional "Profile" key value
                //
                else if (key.Equals(ProfileKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(value))
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

        public static bool operator ==(FrameworkName left, FrameworkName right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        public static bool operator !=(FrameworkName left, FrameworkName right)
        {
            return !(left == right);
        }
    }
}
