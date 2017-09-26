// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract class StringComparer : IComparer, IEqualityComparer, IComparer<string>, IEqualityComparer<string>
    {
        private static readonly CultureAwareComparer s_invariantCulture = new CultureAwareComparer(CultureInfo.InvariantCulture, false);
        private static readonly CultureAwareComparer s_invariantCultureIgnoreCase = new CultureAwareComparer(CultureInfo.InvariantCulture, true);
        private static readonly OrdinalCaseSensitiveComparer s_ordinal = new OrdinalCaseSensitiveComparer();
        private static readonly OrdinalIgnoreCaseComparer s_ordinalIgnoreCase = new OrdinalIgnoreCaseComparer();        

        public static StringComparer InvariantCulture
        {
            get
            {
                return s_invariantCulture;
            }
        }

        public static StringComparer InvariantCultureIgnoreCase
        {
            get
            {
                return s_invariantCultureIgnoreCase;
            }
        }

        public static StringComparer CurrentCulture
        {
            get
            {
                return new CultureAwareComparer(CultureInfo.CurrentCulture, false);
            }
        }

        public static StringComparer CurrentCultureIgnoreCase
        {
            get
            {
                return new CultureAwareComparer(CultureInfo.CurrentCulture, true);
            }
        }

        public static StringComparer Ordinal
        {
            get
            {
                return s_ordinal;
            }
        }

        public static StringComparer OrdinalIgnoreCase
        {
            get
            {
                return s_ordinalIgnoreCase;
            }
        }

        // Convert a StringComparison to a StringComparer
        public static StringComparer FromComparison(StringComparison comparisonType)
        {
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return CurrentCulture;
                case StringComparison.CurrentCultureIgnoreCase:
                    return CurrentCultureIgnoreCase;
                case StringComparison.InvariantCulture:
                    return InvariantCulture;
                case StringComparison.InvariantCultureIgnoreCase:
                    return InvariantCultureIgnoreCase;
                case StringComparison.Ordinal:
                    return Ordinal;
                case StringComparison.OrdinalIgnoreCase:
                    return OrdinalIgnoreCase;
                default:
                    throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }

        public static StringComparer Create(CultureInfo culture, bool ignoreCase)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            return new CultureAwareComparer(culture, ignoreCase);
        }

        public int Compare(object x, object y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            String sa = x as String;
            if (sa != null)
            {
                String sb = y as String;
                if (sb != null)
                {
                    return Compare(sa, sb);
                }
            }

            IComparable ia = x as IComparable;
            if (ia != null)
            {
                return ia.CompareTo(y);
            }

            throw new ArgumentException(SR.Argument_ImplementIComparable);
        }


        public new bool Equals(Object x, Object y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;

            String sa = x as String;
            if (sa != null)
            {
                String sb = y as String;
                if (sb != null)
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

            string s = obj as string;
            if (s != null)
            {
                return GetHashCode(s);
            }
            return obj.GetHashCode();
        }

        public abstract int Compare(String x, String y);
        public abstract bool Equals(String x, String y);
        public abstract int GetHashCode(string obj);
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class CultureAwareComparer : StringComparer
    {
        private readonly CompareInfo _compareInfo; // Do not rename (binary serialization)
        private readonly bool _ignoreCase; // Do not rename (binary serialization)

        internal CultureAwareComparer(CultureInfo culture, bool ignoreCase)
        {
            _compareInfo = culture.CompareInfo;
            _ignoreCase = ignoreCase;
        }

        private CompareOptions Options => _ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;

        public override int Compare(string x, string y)
        {
            if (object.ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return _compareInfo.Compare(x, y, Options);
        }

        public override bool Equals(string x, string y)
        {
            if (object.ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return _compareInfo.Compare(x, y, Options) == 0;
        }

        public override int GetHashCode(string obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            return _compareInfo.GetHashCodeOfString(obj, Options);
        }

        // Equals method for the comparer itself.
        public override bool Equals(object obj)
        {
            CultureAwareComparer comparer = obj as CultureAwareComparer;
            return
                comparer != null &&
                _ignoreCase == comparer._ignoreCase &&
                _compareInfo.Equals(comparer._compareInfo);
        }

        public override int GetHashCode()
        {
            int hashCode = _compareInfo.GetHashCode();
            return _ignoreCase ? ~hashCode : hashCode;
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

        public override int Compare(string x, string y)
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

        public override bool Equals(string x, string y)
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
                return (string.Compare(x, y, StringComparison.OrdinalIgnoreCase) == 0);
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
                return TextInfo.GetHashCodeOrdinalIgnoreCase(obj);
            }

            return obj.GetHashCode();
        }

        // Equals method for the comparer itself. 
        public override bool Equals(object obj)
        {
            OrdinalComparer comparer = obj as OrdinalComparer;
            if (comparer == null)
            {
                return false;
            }
            return (this._ignoreCase == comparer._ignoreCase);
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
        public OrdinalCaseSensitiveComparer() : base(false)
        {
        }

        public override int Compare(string x, string y) => string.CompareOrdinal(x, y);

        public override bool Equals(string x, string y) => string.Equals(x, y);

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
        public OrdinalIgnoreCaseComparer() : base(true)
        {
        }

        public override int Compare(string x, string y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(string x, string y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode(string obj)
        {
            if (obj == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.obj);
            }
            return TextInfo.GetHashCodeOrdinalIgnoreCase(obj);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(OrdinalComparer));
            info.AddValue("_ignoreCase", true);
        }
    }
}
