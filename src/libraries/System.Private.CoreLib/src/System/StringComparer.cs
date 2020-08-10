// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract class StringComparer : IComparer, IEqualityComparer, IComparer<string?>, IEqualityComparer<string?>
    {
        public static StringComparer InvariantCulture => CultureAwareComparer.InvariantCaseSensitiveInstance;

        public static StringComparer InvariantCultureIgnoreCase => CultureAwareComparer.InvariantIgnoreCaseInstance;

        public static StringComparer CurrentCulture =>
            new CultureAwareComparer(CultureInfo.CurrentCulture, CompareOptions.None);

        public static StringComparer CurrentCultureIgnoreCase =>
            new CultureAwareComparer(CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);

        public static StringComparer Ordinal => OrdinalCaseSensitiveComparer.Instance;

        public static StringComparer OrdinalIgnoreCase => OrdinalIgnoreCaseComparer.Instance;

        // Convert a StringComparison to a StringComparer
        public static StringComparer FromComparison(StringComparison comparisonType)
        {
            return comparisonType switch
            {
                StringComparison.CurrentCulture => CurrentCulture,
                StringComparison.CurrentCultureIgnoreCase => CurrentCultureIgnoreCase,
                StringComparison.InvariantCulture => InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase => InvariantCultureIgnoreCase,
                StringComparison.Ordinal => Ordinal,
                StringComparison.OrdinalIgnoreCase => OrdinalIgnoreCase,
                _ => throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType)),
            };
        }

        public static StringComparer Create(CultureInfo culture, bool ignoreCase)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            return new CultureAwareComparer(culture, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        public static StringComparer Create(CultureInfo culture, CompareOptions options)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            return new CultureAwareComparer(culture, options);
        }

        public int Compare(object? x, object? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            if (x is string sa)
            {
                if (y is string sb)
                {
                    return Compare(sa, sb);
                }
            }

            if (x is IComparable ia)
            {
                return ia.CompareTo(y);
            }

            throw new ArgumentException(SR.Argument_ImplementIComparable);
        }

        public new bool Equals(object? x, object? y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;

            if (x is string sa)
            {
                if (y is string sb)
                {
                    return Equals(sa, sb);
                }
            }
            return x.Equals(y);
        }

        public int GetHashCode(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (obj is string s)
            {
                return GetHashCode(s);
            }
            return obj.GetHashCode();
        }

        public abstract int Compare(string? x, string? y);
        public abstract bool Equals(string? x, string? y);
#pragma warning disable CS8614 // Remove warning disable when nullable attributes are respected
        public abstract int GetHashCode(string obj);
#pragma warning restore CS8614
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class CultureAwareComparer : StringComparer, ISerializable
    {
        internal static readonly CultureAwareComparer InvariantCaseSensitiveInstance = new CultureAwareComparer(CompareInfo.Invariant, CompareOptions.None);
        internal static readonly CultureAwareComparer InvariantIgnoreCaseInstance = new CultureAwareComparer(CompareInfo.Invariant, CompareOptions.IgnoreCase);

        private const CompareOptions ValidCompareMaskOffFlags = ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType | CompareOptions.StringSort);

        private readonly CompareInfo _compareInfo; // Do not rename (binary serialization)
        private readonly CompareOptions _options;

        internal CultureAwareComparer(CultureInfo culture, CompareOptions options) : this(culture.CompareInfo, options) { }

        internal CultureAwareComparer(CompareInfo compareInfo, CompareOptions options)
        {
            _compareInfo = compareInfo;

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }
            _options = options;
        }

        private CultureAwareComparer(SerializationInfo info, StreamingContext context)
        {
            _compareInfo = (CompareInfo)info.GetValue("_compareInfo", typeof(CompareInfo))!;
            bool ignoreCase = info.GetBoolean("_ignoreCase");

            object? obj = info.GetValueNoThrow("_options", typeof(CompareOptions));
            if (obj != null)
                _options = (CompareOptions)obj;

            // fix up the _options value in case we are getting old serialized object not having _options
            _options |= ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
        }

        public override int Compare(string? x, string? y)
        {
            if (object.ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return _compareInfo.Compare(x, y, _options);
        }

        public override bool Equals(string? x, string? y)
        {
            if (object.ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return _compareInfo.Compare(x, y, _options) == 0;
        }

        public override int GetHashCode(string obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            return _compareInfo.GetHashCode(obj, _options);
        }

        // Equals method for the comparer itself.
        public override bool Equals(object? obj)
        {
            return
                obj is CultureAwareComparer comparer &&
                _options == comparer._options &&
                _compareInfo.Equals(comparer._compareInfo);
        }

        public override int GetHashCode()
        {
            return _compareInfo.GetHashCode() ^ ((int)_options & 0x7FFFFFFF);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("_compareInfo", _compareInfo);
            info.AddValue("_options", _options);
            info.AddValue("_ignoreCase", (_options & CompareOptions.IgnoreCase) != 0);
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class OrdinalComparer : StringComparer
    {
        private readonly bool _ignoreCase; // Do not rename (binary serialization)

        internal OrdinalComparer(bool ignoreCase)
        {
            _ignoreCase = ignoreCase;
        }

        public override int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            if (_ignoreCase)
            {
                return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
            }

            return string.CompareOrdinal(x, y);
        }

        public override bool Equals(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x == null || y == null)
                return false;

            if (_ignoreCase)
            {
                if (x.Length != y.Length)
                {
                    return false;
                }
                return CompareInfo.EqualsOrdinalIgnoreCase(ref x.GetRawStringData(), ref y.GetRawStringData(), x.Length);
            }
            return x.Equals(y);
        }

        public override int GetHashCode(string obj)
        {
            if (obj == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.obj);
            }

            if (_ignoreCase)
            {
                return obj.GetHashCodeOrdinalIgnoreCase();
            }

            return obj.GetHashCode();
        }

        // Equals method for the comparer itself.
        public override bool Equals(object? obj)
        {
            if (!(obj is OrdinalComparer comparer))
            {
                return false;
            }
            return this._ignoreCase == comparer._ignoreCase;
        }

        public override int GetHashCode()
        {
            int hashCode = nameof(OrdinalComparer).GetHashCode();
            return _ignoreCase ? (~hashCode) : hashCode;
        }
    }

    [Serializable]
    internal sealed class OrdinalCaseSensitiveComparer : OrdinalComparer, ISerializable
    {
        internal static readonly OrdinalCaseSensitiveComparer Instance = new OrdinalCaseSensitiveComparer();

        private OrdinalCaseSensitiveComparer() : base(false)
        {
        }

        public override int Compare(string? x, string? y) => string.CompareOrdinal(x, y);

        public override bool Equals(string? x, string? y) => string.Equals(x, y);

        public override int GetHashCode(string obj)
        {
            if (obj == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.obj);
            }
            return obj.GetHashCode();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(OrdinalComparer));
            info.AddValue("_ignoreCase", false);
        }
    }

    [Serializable]
    internal sealed class OrdinalIgnoreCaseComparer : OrdinalComparer, ISerializable
    {
        internal static readonly OrdinalIgnoreCaseComparer Instance = new OrdinalIgnoreCaseComparer();

        private OrdinalIgnoreCaseComparer() : base(true)
        {
        }

        public override int Compare(string? x, string? y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.Length != y.Length)
            {
                return false;
            }

            return CompareInfo.EqualsOrdinalIgnoreCase(ref x.GetRawStringData(), ref y.GetRawStringData(), x.Length);
        }

        public override int GetHashCode(string obj)
        {
            if (obj == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.obj);
            }
            return obj.GetHashCodeOrdinalIgnoreCase();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(OrdinalComparer));
            info.AddValue("_ignoreCase", true);
        }
    }
}
