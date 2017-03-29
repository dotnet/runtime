// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace System
{
    [Serializable]
    public abstract class StringComparer : IComparer, IEqualityComparer, IComparer<string>, IEqualityComparer<string>
    {
        private static readonly CultureAwareComparer s_invariantCulture = new CultureAwareComparer(CultureInfo.InvariantCulture, false);
        private static readonly CultureAwareComparer s_invariantCultureIgnoreCase = new CultureAwareComparer(CultureInfo.InvariantCulture, true);
        private static readonly OrdinalComparer s_ordinal = new OrdinalComparer();
        private static readonly OrdinalIgnoreCaseComparer s_ordinalIgnoreCase = new OrdinalIgnoreCaseComparer();        

        public static StringComparer InvariantCulture
        {
            get
            {
                Contract.Ensures(Contract.Result<StringComparer>() != null);
                return s_invariantCulture;
            }
        }

        public static StringComparer InvariantCultureIgnoreCase
        {
            get
            {
                Contract.Ensures(Contract.Result<StringComparer>() != null);
                return s_invariantCultureIgnoreCase;
            }
        }

        public static StringComparer CurrentCulture
        {
            get
            {
                Contract.Ensures(Contract.Result<StringComparer>() != null);
                return new CultureAwareComparer(CultureInfo.CurrentCulture, false);
            }
        }

        public static StringComparer CurrentCultureIgnoreCase
        {
            get
            {
                Contract.Ensures(Contract.Result<StringComparer>() != null);
                return new CultureAwareComparer(CultureInfo.CurrentCulture, true);
            }
        }

        public static StringComparer Ordinal
        {
            get
            {
                Contract.Ensures(Contract.Result<StringComparer>() != null);
                return s_ordinal;
            }
        }

        public static StringComparer OrdinalIgnoreCase
        {
            get
            {
                Contract.Ensures(Contract.Result<StringComparer>() != null);
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
            Contract.Ensures(Contract.Result<StringComparer>() != null);
            Contract.EndContractBlock();

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
            Contract.EndContractBlock();

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
    internal sealed class CultureAwareComparer : StringComparer
#if FEATURE_RANDOMIZED_STRING_HASHING
        , IWellKnownStringEqualityComparer
#endif
    {
        private readonly CompareInfo _compareInfo;
        private readonly CompareOptions _options;

        internal CultureAwareComparer(CultureInfo culture, bool ignoreCase)
        {
            _compareInfo = culture.CompareInfo;
            _options = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
        }

        public override int Compare(string x, string y)
        {
            if (object.ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return _compareInfo.Compare(x, y, _options);
        }

        public override bool Equals(string x, string y)
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
            return _compareInfo.GetHashCodeOfString(obj, _options);
        }

        // Equals method for the comparer itself.
        public override bool Equals(object obj)
        {
            CultureAwareComparer comparer = obj as CultureAwareComparer;
            return
                comparer != null &&
                _options == comparer._options &&
                _compareInfo.Equals(comparer._compareInfo);
        }

        public override int GetHashCode()
        {
            int hashCode = _compareInfo.GetHashCode();
            return _options == CompareOptions.None ? hashCode : ~hashCode;
        }

#if FEATURE_RANDOMIZED_STRING_HASHING
        IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization() => this;
#endif
    }

    [Serializable]
    internal sealed class OrdinalComparer : StringComparer 
#if FEATURE_RANDOMIZED_STRING_HASHING           
        , IWellKnownStringEqualityComparer
#endif
    {
        public override int Compare(string x, string y) => string.CompareOrdinal(x, y);

        public override bool Equals(string x, string y) => string.Equals(x, y);

        public override int GetHashCode(string obj)
        {
            if (obj == null)
            {
#if CORECLR
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.obj);
#else
                throw new ArgumentNullException(nameof(obj));
#endif
            }
            return obj.GetHashCode();
        }

        // Equals/GetHashCode methods for the comparer itself. 
        public override bool Equals(object obj) => obj is OrdinalComparer;
        public override int GetHashCode() => nameof(OrdinalComparer).GetHashCode();

#if FEATURE_RANDOMIZED_STRING_HASHING           
        IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization() => this;
#endif
    }

    [Serializable]
    internal sealed class OrdinalIgnoreCaseComparer : StringComparer
#if FEATURE_RANDOMIZED_STRING_HASHING
        , IWellKnownStringEqualityComparer
#endif
    {
        public override int Compare(string x, string y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(string x, string y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode(string obj)
        {
            if (obj == null)
            {
#if CORECLR
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.obj);
#else
                throw new ArgumentNullException(nameof(obj));
#endif
            }
            return TextInfo.GetHashCodeOrdinalIgnoreCase(obj);
        }

        // Equals/GetHashCode methods for the comparer itself. 
        public override bool Equals(object obj) => obj is OrdinalIgnoreCaseComparer;
        public override int GetHashCode() => nameof(OrdinalIgnoreCaseComparer).GetHashCode();

#if FEATURE_RANDOMIZED_STRING_HASHING
        IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization() => this;
#endif
    }

#if FEATURE_RANDOMIZED_STRING_HASHING           
    // This interface is implemented by string comparers in the framework that can opt into
    // randomized hashing behaviors. 
    internal interface IWellKnownStringEqualityComparer
    {
        // Get an IEqaulityComparer that can be serailzied (e.g., it exists in older versions). 
        IEqualityComparer GetEqualityComparerForSerialization();
    }
#endif
}
