// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Collections.Frozen
{
    public abstract partial class FrozenDictionary<TKey, TValue>
    {
        public System.Collections.Frozen.FrozenDictionary<TKey,TValue>.AlternateLookup<TAlternateKey> GetAlternateLookup<TAlternateKey>() where TAlternateKey : notnull, allows ref struct { throw null; }
        public bool TryGetAlternateLookup<TAlternateKey>(out System.Collections.Frozen.FrozenDictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup) where TAlternateKey : notnull, allows ref struct { throw null; }
        public readonly partial struct AlternateLookup<TAlternateKey> where TAlternateKey : notnull, allows ref struct
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public System.Collections.Frozen.FrozenDictionary<TKey, TValue> Dictionary { get { throw null; } }
            public TValue this[TAlternateKey key] { get { throw null; } }
            public bool ContainsKey(TAlternateKey key) { throw null; }
            public bool TryGetValue(TAlternateKey key, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out TValue value) { throw null; }
        }
    }
    public abstract partial class FrozenSet<T>
    {
        public System.Collections.Frozen.FrozenSet<T>.AlternateLookup<TAlternate> GetAlternateLookup<TAlternate>() { throw null; }
        public bool TryGetAlternateLookup<TAlternate>(out System.Collections.Frozen.FrozenSet<T>.AlternateLookup<TAlternate> lookup) { throw null; }
        public readonly partial struct AlternateLookup<TAlternate>
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public System.Collections.Frozen.FrozenSet<T> Set { get { throw null; } }
            public bool Contains(TAlternate item) { throw null; }
            public bool TryGetValue(TAlternate equalValue, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out T actualValue) { throw null; }
        }
    }
}
