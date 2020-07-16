// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    // NonRandomizedStringEqualityComparer is the comparer used by default with the Dictionary<string,...>
    // We use NonRandomizedStringEqualityComparer as default comparer as it doesnt use the randomized string hashing which
    // keeps the performance not affected till we hit collision threshold and then we switch to the comparer which is using
    // randomized string hashing.
    [Serializable] // Required for compatibility with .NET Core 2.0 as we exposed the NonRandomizedStringEqualityComparer inside the serialization blob
    // Needs to be public to support binary serialization compatibility
    public sealed class NonRandomizedStringEqualityComparer : INonRandomizedEqualityComparer<string?>, ISerializable
    {
        internal static readonly IEqualityComparer<string?> AroundDefaultComparer = new NonRandomizedStringEqualityComparer(wrapsOrdinalComparer: false);
        internal static readonly IEqualityComparer<string?> AroundStringComparerOrdinal = new NonRandomizedStringEqualityComparer(wrapsOrdinalComparer: true);

        // Flag indicates whether this instance wraps EqualityComparer<string>.Default
        // or StringComparer.Ordinal.
        private readonly bool _wrapsOrdinalComparer;

        private NonRandomizedStringEqualityComparer(bool wrapsOrdinalComparer)
        {
            _wrapsOrdinalComparer = wrapsOrdinalComparer;
        }

        // This is used by the serialization engine.
        private NonRandomizedStringEqualityComparer(SerializationInfo information, StreamingContext context)
            : this(wrapsOrdinalComparer: false)
        {
        }

        public bool Equals(string? x, string? y) => string.Equals(x, y);

        public int GetHashCode(string? obj) => obj?.GetNonRandomizedHashCode() ?? 0;

        public IEqualityComparer<string?> GetRandomizedComparer()
        {
            return (_wrapsOrdinalComparer)
                ? (IEqualityComparer<string?>)StringComparer.Ordinal
                : (IEqualityComparer<string?>)EqualityComparer<string>.Default;
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // We are doing this to stay compatible with .NET Framework.
            info.SetType(typeof(GenericEqualityComparer<string>));
        }
    }

    internal sealed class NonRandomizedStringOrdinalIgnoreCaseEqualityComparer : INonRandomizedEqualityComparer<string?>
    {
        internal static readonly IEqualityComparer<string?> Instance
            = new NonRandomizedStringOrdinalIgnoreCaseEqualityComparer();

        private NonRandomizedStringOrdinalIgnoreCaseEqualityComparer() { }

        public bool Equals(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Length != y.Length)
            {
                return false;
            }

            return CompareInfo.EqualsOrdinalIgnoreCase(
                ref x.GetRawStringData(),
                ref y.GetRawStringData(),
                y.Length);
        }

        public int GetHashCode(string? obj) => obj?.GetNonRandomizedHashCodeOrdinalIgnoreCase() ?? 0;

        public IEqualityComparer<string?> GetRandomizedComparer() => StringComparer.OrdinalIgnoreCase;
    }
}
