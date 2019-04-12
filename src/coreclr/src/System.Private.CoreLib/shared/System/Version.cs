// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Globalization;
using System.Diagnostics;
using System.Text;
using System.Runtime.CompilerServices;

namespace System
{
    // A Version object contains four hierarchical numeric components: major, minor,
    // build and revision.  Build and revision may be unspecified, which is represented 
    // internally as a -1.  By definition, an unspecified component matches anything 
    // (both unspecified and specified), and an unspecified component is "less than" any
    // specified component.

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class Version : ICloneable, IComparable, IComparable<Version?>, IEquatable<Version?>, ISpanFormattable
    {
        // AssemblyName depends on the order staying the same
        private readonly int _Major; // Do not rename (binary serialization)
        private readonly int _Minor; // Do not rename (binary serialization)
        private readonly int _Build = -1; // Do not rename (binary serialization)
        private readonly int _Revision = -1; // Do not rename (binary serialization)

        public Version(int major, int minor, int build, int revision)
        {
            if (major < 0)
                throw new ArgumentOutOfRangeException(nameof(major), SR.ArgumentOutOfRange_Version);

            if (minor < 0)
                throw new ArgumentOutOfRangeException(nameof(minor), SR.ArgumentOutOfRange_Version);

            if (build < 0)
                throw new ArgumentOutOfRangeException(nameof(build), SR.ArgumentOutOfRange_Version);

            if (revision < 0)
                throw new ArgumentOutOfRangeException(nameof(revision), SR.ArgumentOutOfRange_Version);

            _Major = major;
            _Minor = minor;
            _Build = build;
            _Revision = revision;
        }

        public Version(int major, int minor, int build)
        {
            if (major < 0)
                throw new ArgumentOutOfRangeException(nameof(major), SR.ArgumentOutOfRange_Version);

            if (minor < 0)
                throw new ArgumentOutOfRangeException(nameof(minor), SR.ArgumentOutOfRange_Version);

            if (build < 0)
                throw new ArgumentOutOfRangeException(nameof(build), SR.ArgumentOutOfRange_Version);


            _Major = major;
            _Minor = minor;
            _Build = build;
        }

        public Version(int major, int minor)
        {
            if (major < 0)
                throw new ArgumentOutOfRangeException(nameof(major), SR.ArgumentOutOfRange_Version);

            if (minor < 0)
                throw new ArgumentOutOfRangeException(nameof(minor), SR.ArgumentOutOfRange_Version);

            _Major = major;
            _Minor = minor;
        }

        public Version(string version)
        {
            Version v = Version.Parse(version);
            _Major = v.Major;
            _Minor = v.Minor;
            _Build = v.Build;
            _Revision = v.Revision;
        }

        public Version()
        {
            _Major = 0;
            _Minor = 0;
        }

        private Version(Version version)
        {
            Debug.Assert(version != null);

            _Major = version._Major;
            _Minor = version._Minor;
            _Build = version._Build;
            _Revision = version._Revision;
        }

        public object Clone()
        {
            return new Version(this);
        }

        // Properties for setting and getting version numbers
        public int Major
        {
            get { return _Major; }
        }

        public int Minor
        {
            get { return _Minor; }
        }

        public int Build
        {
            get { return _Build; }
        }

        public int Revision
        {
            get { return _Revision; }
        }

        public short MajorRevision
        {
            get { return (short)(_Revision >> 16); }
        }

        public short MinorRevision
        {
            get { return (short)(_Revision & 0xFFFF); }
        }

        public int CompareTo(object? version)
        {
            if (version == null)
            {
                return 1;
            }

            if (version is Version v)
            {
                return CompareTo(v);
            }

            throw new ArgumentException(SR.Arg_MustBeVersion);
        }

        public int CompareTo(Version? value)
        {
            return
                object.ReferenceEquals(value, this) ? 0 :
                value is null ? 1 :
                _Major != value._Major ? (_Major > value._Major ? 1 : -1) :
                _Minor != value._Minor ? (_Minor > value._Minor ? 1 : -1) :
                _Build != value._Build ? (_Build > value._Build ? 1 : -1) :
                _Revision != value._Revision ? (_Revision > value._Revision ? 1 : -1) :
                0;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Version);
        }

        public bool Equals(Version? obj)
        {
            return object.ReferenceEquals(obj, this) ||
                (!(obj is null) &&
                _Major == obj._Major &&
                _Minor == obj._Minor &&
                _Build == obj._Build &&
                _Revision == obj._Revision);
        }

        public override int GetHashCode()
        {
            // Let's assume that most version numbers will be pretty small and just
            // OR some lower order bits together.

            int accumulator = 0;

            accumulator |= (_Major & 0x0000000F) << 28;
            accumulator |= (_Minor & 0x000000FF) << 20;
            accumulator |= (_Build & 0x000000FF) << 12;
            accumulator |= (_Revision & 0x00000FFF);

            return accumulator;
        }

        public override string ToString() =>
            ToString(DefaultFormatFieldCount);

        public string ToString(int fieldCount) =>
            fieldCount == 0 ? string.Empty :
            fieldCount == 1 ? _Major.ToString() :
            StringBuilderCache.GetStringAndRelease(ToCachedStringBuilder(fieldCount));

        public bool TryFormat(Span<char> destination, out int charsWritten) =>
            TryFormat(destination, DefaultFormatFieldCount, out charsWritten);

        public bool TryFormat(Span<char> destination, int fieldCount, out int charsWritten)
        {
            if (fieldCount == 0)
            {
                charsWritten = 0;
                return true;
            }
            else if (fieldCount == 1)
            {
                return _Major.TryFormat(destination, out charsWritten);
            }

            StringBuilder sb = ToCachedStringBuilder(fieldCount);
            if (sb.Length <= destination.Length)
            {
                sb.CopyTo(0, destination, sb.Length);
                StringBuilderCache.Release(sb);
                charsWritten = sb.Length;
                return true;
            }

            StringBuilderCache.Release(sb);
            charsWritten = 0;
            return false;
        }

        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            // format and provider are ignored.
            return TryFormat(destination, out charsWritten);
        }

        private int DefaultFormatFieldCount =>
            _Build == -1 ? 2 :
            _Revision == -1 ? 3 :
            4;

        private StringBuilder ToCachedStringBuilder(int fieldCount)
        {
            // Note: As we always have positive numbers then it is safe to convert the number to string
            // regardless of the current culture as we'll not have any punctuation marks in the number.

            if (fieldCount == 2)
            {
                StringBuilder sb = StringBuilderCache.Acquire();
                sb.Append(_Major);
                sb.Append('.');
                sb.Append(_Minor);
                return sb;
            }
            else
            {
                if (_Build == -1)
                {
                    throw new ArgumentException(SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, "0", "2"), nameof(fieldCount));
                }

                if (fieldCount == 3)
                {
                    StringBuilder sb = StringBuilderCache.Acquire();
                    sb.Append(_Major);
                    sb.Append('.');
                    sb.Append(_Minor);
                    sb.Append('.');
                    sb.Append(_Build);
                    return sb;
                }

                if (_Revision == -1)
                {
                    throw new ArgumentException(SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, "0", "3"), nameof(fieldCount));
                }

                if (fieldCount == 4)
                {
                    StringBuilder sb = StringBuilderCache.Acquire();
                    sb.Append(_Major);
                    sb.Append('.');
                    sb.Append(_Minor);
                    sb.Append('.');
                    sb.Append(_Build);
                    sb.Append('.');
                    sb.Append(_Revision);
                    return sb;
                }

                throw new ArgumentException(SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, "0", "4"), nameof(fieldCount));
            }
        }

        public static Version Parse(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return ParseVersion(input.AsSpan(), throwOnFailure: true)!;
        }

        public static Version Parse(ReadOnlySpan<char> input) =>
            ParseVersion(input, throwOnFailure: true)!;

        public static bool TryParse(string? input, out Version? result) // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
        {
            if (input == null)
            {
                result = null;
                return false;
            }

            return (result = ParseVersion(input.AsSpan(), throwOnFailure: false)) != null;
        }

        public static bool TryParse(ReadOnlySpan<char> input, out Version? result) =>
            (result = ParseVersion(input, throwOnFailure: false)) != null;

        private static Version? ParseVersion(ReadOnlySpan<char> input, bool throwOnFailure)
        {
            // Find the separator between major and minor.  It must exist.
            int majorEnd = input.IndexOf('.');
            if (majorEnd < 0)
            {
                if (throwOnFailure) throw new ArgumentException(SR.Arg_VersionString, nameof(input));
                return null;
            }

            // Find the ends of the optional minor and build portions.
            // We musn't have any separators after build.
            int buildEnd = -1;
            int minorEnd = input.Slice(majorEnd + 1).IndexOf('.');
            if (minorEnd != -1)
            {
                minorEnd += (majorEnd + 1);
                buildEnd = input.Slice(minorEnd + 1).IndexOf('.');
                if (buildEnd != -1)
                {
                    buildEnd += (minorEnd + 1);
                    if (input.Slice(buildEnd + 1).Contains('.'))
                    {
                        if (throwOnFailure) throw new ArgumentException(SR.Arg_VersionString, nameof(input));
                        return null;
                    }
                }
            }

            int minor, build, revision;

            // Parse the major version
            if (!TryParseComponent(input.Slice(0, majorEnd), nameof(input), throwOnFailure, out int major))
            {
                return null;
            }

            if (minorEnd != -1)
            {
                // If there's more than a major and minor, parse the minor, too.
                if (!TryParseComponent(input.Slice(majorEnd + 1, minorEnd - majorEnd - 1), nameof(input), throwOnFailure, out minor))
                {
                    return null;
                }

                if (buildEnd != -1)
                {
                    // major.minor.build.revision
                    return
                        TryParseComponent(input.Slice(minorEnd + 1, buildEnd - minorEnd - 1), nameof(build), throwOnFailure, out build) &&
                        TryParseComponent(input.Slice(buildEnd + 1), nameof(revision), throwOnFailure, out revision) ?
                            new Version(major, minor, build, revision) :
                            null;
                }
                else
                {
                    // major.minor.build
                    return TryParseComponent(input.Slice(minorEnd + 1), nameof(build), throwOnFailure, out build) ?
                        new Version(major, minor, build) :
                        null;
                }
            }
            else
            {
                // major.minor
                return TryParseComponent(input.Slice(majorEnd + 1), nameof(input), throwOnFailure, out minor) ?
                    new Version(major, minor) :
                    null;
            }
        }

        private static bool TryParseComponent(ReadOnlySpan<char> component, string componentName, bool throwOnFailure, out int parsedComponent)
        {
            if (throwOnFailure)
            {
                if ((parsedComponent = int.Parse(component, NumberStyles.Integer, CultureInfo.InvariantCulture)) < 0)
                {
                    throw new ArgumentOutOfRangeException(componentName, SR.ArgumentOutOfRange_Version);
                }
                return true;
            }

            return int.TryParse(component, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedComponent) && parsedComponent >= 0;
        }

        // Force inline as the true/false ternary takes it above ALWAYS_INLINE size even though the asm ends up smaller
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Version? v1, Version? v2)
        {
            // Test "right" first to allow branch elimination when inlined for null checks (== null)
            // so it can become a simple test
            if (v2 is null)
            {
                // return true/false not the test result https://github.com/dotnet/coreclr/issues/914
                return (v1 is null) ? true : false;
            }

            // Quick reference equality test prior to calling the virtual Equality
            return ReferenceEquals(v2, v1) ? true : v2.Equals(v1);
        }

        public static bool operator !=(Version? v1, Version? v2)
        {
            return !(v1 == v2);
        }

        public static bool operator <(Version? v1, Version? v2)
        {
            if (v1 is null)
            {
                return !(v2 is null);
            }

            return (v1.CompareTo(v2) < 0);
        }

        public static bool operator <=(Version? v1, Version? v2)
        {
            if (v1 is null)
            {
                return true;
            }

            return (v1.CompareTo(v2) <= 0);
        }

        public static bool operator >(Version? v1, Version? v2)
        {
            return (v2 < v1);
        }

        public static bool operator >=(Version? v1, Version? v2)
        {
            return (v2 <= v1);
        }
    }
}
