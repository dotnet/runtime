// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    // A Version object contains four hierarchical numeric components: major, minor,
    // build and revision.  Build and revision may be unspecified, which is represented
    // internally as a -1.  By definition, an unspecified component matches anything
    // (both unspecified and specified), and an unspecified component is "less than" any
    // specified component.

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class Version : ICloneable, IComparable, IComparable<Version?>, IEquatable<Version?>, ISpanFormattable, IUtf8SpanFormattable
    {
        // AssemblyName depends on the order staying the same
        private readonly int _Major; // Do not rename (binary serialization)
        private readonly int _Minor; // Do not rename (binary serialization)
        private readonly int _Build; // Do not rename (binary serialization)
        private readonly int _Revision; // Do not rename (binary serialization)

        public Version(int major, int minor, int build, int revision)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(major);
            ArgumentOutOfRangeException.ThrowIfNegative(minor);
            ArgumentOutOfRangeException.ThrowIfNegative(build);
            ArgumentOutOfRangeException.ThrowIfNegative(revision);

            _Major = major;
            _Minor = minor;
            _Build = build;
            _Revision = revision;
        }

        public Version(int major, int minor, int build)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(major);
            ArgumentOutOfRangeException.ThrowIfNegative(minor);
            ArgumentOutOfRangeException.ThrowIfNegative(build);

            _Major = major;
            _Minor = minor;
            _Build = build;
            _Revision = -1;
        }

        public Version(int major, int minor)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(major);
            ArgumentOutOfRangeException.ThrowIfNegative(minor);

            _Major = major;
            _Minor = minor;
            _Build = -1;
            _Revision = -1;
        }

        public Version(string version)
        {
            Version v = Parse(version);
            _Major = v.Major;
            _Minor = v.Minor;
            _Build = v.Build;
            _Revision = v.Revision;
        }

        public Version()
        {
            //_Major = 0;
            //_Minor = 0;
            _Build = -1;
            _Revision = -1;
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
        public int Major => _Major;

        public int Minor => _Minor;

        public int Build => _Build;

        public int Revision => _Revision;

        public short MajorRevision => (short)(_Revision >> 16);

        public short MinorRevision => (short)(_Revision & 0xFFFF);

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

            throw new ArgumentException(SR.Arg_MustBeVersion, nameof(version));
        }

        public int CompareTo(Version? value)
        {
            return
                ReferenceEquals(value, this) ? 0 :
                value is null ? 1 :
                _Major != value._Major ? (_Major > value._Major ? 1 : -1) :
                _Minor != value._Minor ? (_Minor > value._Minor ? 1 : -1) :
                _Build != value._Build ? (_Build > value._Build ? 1 : -1) :
                _Revision != value._Revision ? (_Revision > value._Revision ? 1 : -1) :
                0;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return Equals(obj as Version);
        }

        public bool Equals([NotNullWhen(true)] Version? obj)
        {
            return ReferenceEquals(obj, this) ||
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

        public string ToString(int fieldCount)
        {
            Span<char> dest = stackalloc char[(4 * Number.Int32NumberBufferLength) + 3]; // at most 4 Int32s and 3 periods
            bool success = TryFormat(dest, fieldCount, out int charsWritten);
            Debug.Assert(success);
            return dest.Slice(0, charsWritten).ToString();
        }

        string IFormattable.ToString(string? format, IFormatProvider? formatProvider) =>
            ToString();

        public bool TryFormat(Span<char> destination, out int charsWritten) =>
            TryFormatCore(destination, DefaultFormatFieldCount, out charsWritten);

        public bool TryFormat(Span<char> destination, int fieldCount, out int charsWritten) =>
            TryFormatCore(destination, fieldCount, out charsWritten);

        /// <summary>Tries to format this version instance into a span of bytes.</summary>
        /// <param name="utf8Destination">The span in which to write this instance's value formatted as a span of UTF-8 bytes.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes that were written in <paramref name="utf8Destination"/>.</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten) =>
            TryFormatCore(utf8Destination, DefaultFormatFieldCount, out bytesWritten);

        /// <summary>Tries to format this version instance into a span of bytes.</summary>
        /// <param name="utf8Destination">The span in which to write this instance's value formatted as a span of UTF-8 bytes.</param>
        /// <param name="fieldCount">The number of components to return. This value ranges from 0 to 4.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes that were written in <paramref name="utf8Destination"/>.</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        public bool TryFormat(Span<byte> utf8Destination, int fieldCount, out int bytesWritten) =>
            TryFormatCore(utf8Destination, fieldCount, out bytesWritten);

        private bool TryFormatCore<TChar>(Span<TChar> destination, int fieldCount, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            switch ((uint)fieldCount)
            {
                case > 4:
                    ThrowArgumentException("4");
                    break;

                case >= 3 when _Build == -1:
                    ThrowArgumentException("2");
                    break;

                case 4 when _Revision == -1:
                    ThrowArgumentException("3");
                    break;

                static void ThrowArgumentException(string failureUpperBound) =>
                    throw new ArgumentException(SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, "0", failureUpperBound), nameof(fieldCount));
            }

            int totalCharsWritten = 0;

            for (int i = 0; i < fieldCount; i++)
            {
                if (i != 0)
                {
                    if (destination.IsEmpty)
                    {
                        charsWritten = 0;
                        return false;
                    }

                    destination[0] = TChar.CastFrom('.');
                    destination = destination.Slice(1);
                    totalCharsWritten++;
                }

                int value = i switch
                {
                    0 => _Major,
                    1 => _Minor,
                    2 => _Build,
                    _ => _Revision
                };

                int valueCharsWritten;
                bool formatted = typeof(TChar) == typeof(char) ?
                    ((uint)value).TryFormat(MemoryMarshal.Cast<TChar, char>(destination), out valueCharsWritten) :
                    ((uint)value).TryFormat(MemoryMarshal.Cast<TChar, byte>(destination), out valueCharsWritten, default, CultureInfo.InvariantCulture);

                if (!formatted)
                {
                    charsWritten = 0;
                    return false;
                }

                totalCharsWritten += valueCharsWritten;
                destination = destination.Slice(valueCharsWritten);
            }

            charsWritten = totalCharsWritten;
            return true;
        }

        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            // format and provider are ignored.
            TryFormatCore(destination, DefaultFormatFieldCount, out charsWritten);

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat" />
        bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            // format and provider are ignored.
            TryFormatCore(utf8Destination, DefaultFormatFieldCount, out bytesWritten);

        private int DefaultFormatFieldCount =>
            _Build == -1 ? 2 :
            _Revision == -1 ? 3 :
            4;

        public static Version Parse(string input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return ParseVersion(input.AsSpan(), throwOnFailure: true)!;
        }

        public static Version Parse(ReadOnlySpan<char> input) =>
            ParseVersion(input, throwOnFailure: true)!;

        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out Version? result)
        {
            if (input == null)
            {
                result = null;
                return false;
            }

            return (result = ParseVersion(input.AsSpan(), throwOnFailure: false)) != null;
        }

        public static bool TryParse(ReadOnlySpan<char> input, [NotNullWhen(true)] out Version? result) =>
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
            if (minorEnd >= 0)
            {
                minorEnd += (majorEnd + 1);
                buildEnd = input.Slice(minorEnd + 1).IndexOf('.');
                if (buildEnd >= 0)
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
                parsedComponent = int.Parse(component, NumberStyles.Integer, CultureInfo.InvariantCulture);
                ArgumentOutOfRangeException.ThrowIfNegative(parsedComponent, componentName);
                return true;
            }

            return int.TryParse(component, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedComponent) && parsedComponent >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Version? v1, Version? v2)
        {
            if (ReferenceEquals(v1, v2))
            {
                return true;
            }

            return v2 is not null && v2.Equals(v1);
        }

        public static bool operator !=(Version? v1, Version? v2) => !(v1 == v2);

        public static bool operator <(Version? v1, Version? v2)
        {
            if (v1 is null)
            {
                return !(v2 is null);
            }

            return v1.CompareTo(v2) < 0;
        }

        public static bool operator <=(Version? v1, Version? v2)
        {
            if (v1 is null)
            {
                return true;
            }

            return v1.CompareTo(v2) <= 0;
        }

        public static bool operator >(Version? v1, Version? v2) => v2 < v1;

        public static bool operator >=(Version? v1, Version? v2) => v2 <= v1;
    }
}
