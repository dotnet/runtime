// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    // NonRandomizedStringEqualityComparer is the comparer used by default with the Dictionary<string,...>
    // We use NonRandomizedStringEqualityComparer as default comparer as it doesnt use the randomized string hashing which
    // keeps the performance not affected till we hit collision threshold and then we switch to the comparer which is using
    // randomized string hashing.
    [Serializable] // Required for compatibility with .NET Core 2.0 as we exposed the NonRandomizedStringEqualityComparer inside the serialization blob
    // Needs to be public to support binary serialization compatibility
    public class NonRandomizedStringEqualityComparer : IEqualityComparer<string?>, IEqualityComparerProxy<string?>, ISerializable
    {
        // Dictionary<...>.Comparer and similar methods need to return the original IEqualityComparer
        // that was passed in to the ctor. The caller chooses one of these singletons so that the
        // GetUnderlyingEqualityComparer method can return the correct value.

        internal static readonly NonRandomizedStringEqualityComparer WrappedAroundDefaultComparer = new NonRandomizedStringEqualityComparer(EqualityComparer<string?>.Default);
        internal static readonly NonRandomizedStringEqualityComparer WrappedAroundStringComparerOrdinal = new NonRandomizedStringEqualityComparer(StringComparer.Ordinal);
        internal static readonly NonRandomizedStringEqualityComparer WrappedAroundStringComparerOrdinalIgnoreCase = new NonRandomizedOrdinalIgnoreCaseComparer();

        private readonly IEqualityComparer<string?> _underlyingComparer;

        private NonRandomizedStringEqualityComparer(IEqualityComparer<string?> underlyingComparer)
        {
            Debug.Assert(underlyingComparer != null);
            _underlyingComparer = underlyingComparer;
        }

        // This is used by the serialization engine.
        protected NonRandomizedStringEqualityComparer(SerializationInfo information, StreamingContext context)
            : this(EqualityComparer<string?>.Default)
        {
        }

        public virtual bool Equals(string? x, string? y) => string.Equals(x, y); // Ordinal

        public virtual int GetHashCode(string? obj) => obj?.GetNonRandomizedHashCode() ?? 0; // Ordinal

        internal virtual RandomizedStringEqualityComparer GetRandomizedEqualityComparer()
        {
            return RandomizedStringEqualityComparer.Create(_underlyingComparer, ignoreCase: false);
        }

        // Gets the comparer that should be returned back to the caller when querying the
        // ICollection.Comparer property. Also used for serialization purposes.
        public virtual IEqualityComparer<string?> GetUnderlyingEqualityComparer() => _underlyingComparer;

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // We are doing this to stay compatible with .NET Framework.
            // Our own collection types will never call this (since this type is a proxy),
            // but perhaps third-party collection types could try serializing an instance
            // of this.
            info.SetType(typeof(GenericEqualityComparer<string>));
        }

        private sealed class NonRandomizedOrdinalIgnoreCaseComparer : NonRandomizedStringEqualityComparer
        {
            internal NonRandomizedOrdinalIgnoreCaseComparer()
                : base(StringComparer.OrdinalIgnoreCase)
            {
            }

            public override bool Equals(string? x, string? y) => string.EqualsOrdinalIgnoreCase(x, y);

            public override int GetHashCode(string? obj) => obj?.GetNonRandomizedHashCodeOrdinalIgnoreCase() ?? 0;

            internal override RandomizedStringEqualityComparer GetRandomizedEqualityComparer()
            {
                return RandomizedStringEqualityComparer.Create(_underlyingComparer, ignoreCase: true);
            }
        }
    }
}
