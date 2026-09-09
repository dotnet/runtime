// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Xunit;

namespace System.Collections.Tests
{
    #region Dictionary
    public class InternalHashCodeTests_Dictionary_NullComparer : InternalHashCodeTests<Dictionary<string, string>>
    {
        protected override Dictionary<string, string> CreateCollection() => new Dictionary<string, string>();
        protected override void AddKey(Dictionary<string, string> collection, string key) => collection.Add(key, key);
        protected override bool ContainsKey(Dictionary<string, string> collection, string key) => collection.ContainsKey(key);
        protected override bool ContainsKey(Dictionary<string, string> collection, ReadOnlySpan<char> key) =>
            collection.GetAlternateLookup<ReadOnlySpan<char>>().ContainsKey(key);
        protected override IEqualityComparer<string> GetComparer(Dictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => EqualityComparer<string>.Default;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => randomizedOrdinalComparerType;

        [Fact]
        [OuterLoop("Takes over 55% of System.Collections.Tests testing time")]
        public void OutOfBoundsRegression()
        {
            var dictionary = new Dictionary<string, string>();

            foreach (var item in TestData.GetData())
            {
                var operation = item.Item1;
                var keyBase64 = item.Item2;

                var key = keyBase64.Length > 0 ? GetString(Convert.FromBase64String(keyBase64)) : string.Empty;

                if (operation == InputAction.Add)
                    dictionary[key] = key;
                else if (operation == InputAction.Delete)
                    dictionary.Remove(key);
            }
        }

        /// <summary>
        /// Given a byte array, copies it to the string, without messing with any encoding.  This issue was hit on a x64 machine
        /// </summary>
        private static string GetString(byte[] bytes)
        {
            var chars = new char[bytes.Length / sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }

    public class InternalHashCodeTests_Dictionary_DefaultComparer : InternalHashCodeTests<Dictionary<string, string>>
    {
        protected override Dictionary<string, string> CreateCollection() => new Dictionary<string, string>(EqualityComparer<string>.Default);
        protected override void AddKey(Dictionary<string, string> collection, string key) => collection.Add(key, key);
        protected override bool ContainsKey(Dictionary<string, string> collection, string key) => collection.ContainsKey(key);
        protected override bool ContainsKey(Dictionary<string, string> collection, ReadOnlySpan<char> key) =>
            collection.GetAlternateLookup<ReadOnlySpan<char>>().ContainsKey(key);
        protected override IEqualityComparer<string> GetComparer(Dictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => EqualityComparer<string>.Default;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => randomizedOrdinalComparerType;
    }

    public class InternalHashCodeTests_Dictionary_OrdinalComparer : InternalHashCodeTests<Dictionary<string, string>>
    {
        protected override Dictionary<string, string> CreateCollection() => new Dictionary<string, string>(StringComparer.Ordinal);
        protected override void AddKey(Dictionary<string, string> collection, string key) => collection.Add(key, key);
        protected override bool ContainsKey(Dictionary<string, string> collection, string key) => collection.ContainsKey(key);
        protected override bool ContainsKey(Dictionary<string, string> collection, ReadOnlySpan<char> key) =>
            collection.GetAlternateLookup<ReadOnlySpan<char>>().ContainsKey(key);
        protected override IEqualityComparer<string> GetComparer(Dictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => StringComparer.Ordinal;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => randomizedOrdinalComparerType;
    }

    public class InternalHashCodeTests_Dictionary_OrdinalIgnoreCaseComparer : InternalHashCodeTests<Dictionary<string, string>>
    {
        protected override Dictionary<string, string> CreateCollection() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        protected override void AddKey(Dictionary<string, string> collection, string key) => collection.Add(key, key);
        protected override bool ContainsKey(Dictionary<string, string> collection, string key) => collection.ContainsKey(key);
        protected override bool ContainsKey(Dictionary<string, string> collection, ReadOnlySpan<char> key) =>
            collection.GetAlternateLookup<ReadOnlySpan<char>>().ContainsKey(key);
        protected override IEqualityComparer<string> GetComparer(Dictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalIgnoreCaseComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => StringComparer.OrdinalIgnoreCase;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => randomizedOrdinalIgnoreCaseComparerType;
    }

    public class InternalHashCodeTests_Dictionary_LinguisticComparer : InternalHashCodeTests<Dictionary<string, string>> // (not optimized)
    {
        protected override Dictionary<string, string> CreateCollection() => new Dictionary<string, string>(StringComparer.InvariantCulture);
        protected override void AddKey(Dictionary<string, string> collection, string key) => collection.Add(key, key);
        protected override bool ContainsKey(Dictionary<string, string> collection, string key) => collection.ContainsKey(key);
        protected override bool ContainsKey(Dictionary<string, string> collection, ReadOnlySpan<char> key) =>
            collection.GetAlternateLookup<ReadOnlySpan<char>>().ContainsKey(key);
        protected override IEqualityComparer<string> GetComparer(Dictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => StringComparer.InvariantCulture.GetType();
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => StringComparer.InvariantCulture;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => StringComparer.InvariantCulture.GetType();
    }



    public class InternalHashCodeTests_Dictionary_GetValueRefOrAddDefault : InternalHashCodeTests<Dictionary<string, string>>
    {
        protected override Dictionary<string, string> CreateCollection() => new Dictionary<string, string>(StringComparer.Ordinal);
        protected override void AddKey(Dictionary<string, string> collection, string key) => CollectionsMarshal.GetValueRefOrAddDefault(collection, key, out _) = null;
        protected override bool ContainsKey(Dictionary<string, string> collection, string key) => collection.ContainsKey(key);
        protected override bool ContainsKey(Dictionary<string, string> collection, ReadOnlySpan<char> key) =>
            collection.GetAlternateLookup<ReadOnlySpan<char>>().ContainsKey(key);
        protected override IEqualityComparer<string> GetComparer(Dictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => StringComparer.Ordinal;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => randomizedOrdinalComparerType;
    }
    #endregion

    #region HashSet
    public class InternalHashCodeTests_HashSet_NullComparer : InternalHashCodeTests<HashSet<string>>
    {
        protected override HashSet<string> CreateCollection() => new HashSet<string>();
        protected override void AddKey(HashSet<string> collection, string key) => collection.Add(key);
        protected override bool ContainsKey(HashSet<string> collection, string key) => collection.Contains(key);
        protected override bool ContainsKey(HashSet<string> collection, ReadOnlySpan<char> key) =>
            collection.GetAlternateLookup<ReadOnlySpan<char>>().Contains(key);
        protected override IEqualityComparer<string> GetComparer(HashSet<string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => EqualityComparer<string>.Default;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => randomizedOrdinalComparerType;
    }

    public class InternalHashCodeTests_HashSet_DefaultComparer : InternalHashCodeTests<HashSet<string>>
    {
        protected override HashSet<string> CreateCollection() => new HashSet<string>(EqualityComparer<string>.Default);
        protected override void AddKey(HashSet<string> collection, string key) => collection.Add(key);
        protected override bool ContainsKey(HashSet<string> collection, string key) => collection.Contains(key);
        protected override bool ContainsKey(HashSet<string> collection, ReadOnlySpan<char> key) =>
            collection.GetAlternateLookup<ReadOnlySpan<char>>().Contains(key);
        protected override IEqualityComparer<string> GetComparer(HashSet<string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => EqualityComparer<string>.Default;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => randomizedOrdinalComparerType;
    }

    public class InternalHashCodeTests_HashSet_OrdinalComparer : InternalHashCodeTests<HashSet<string>>
    {
        protected override HashSet<string> CreateCollection() => new HashSet<string>(StringComparer.Ordinal);
        protected override void AddKey(HashSet<string> collection, string key) => collection.Add(key);
        protected override bool ContainsKey(HashSet<string> collection, string key) => collection.Contains(key);
        protected override bool ContainsKey(HashSet<string> collection, ReadOnlySpan<char> key) =>
            collection.GetAlternateLookup<ReadOnlySpan<char>>().Contains(key);
        protected override IEqualityComparer<string> GetComparer(HashSet<string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => StringComparer.Ordinal;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => randomizedOrdinalComparerType;
    }

    public class InternalHashCodeTests_HashSet_OrdinalIgnoreCaseComparer : InternalHashCodeTests<HashSet<string>>
    {
        protected override HashSet<string> CreateCollection() => new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        protected override void AddKey(HashSet<string> collection, string key) => collection.Add(key);
        protected override bool ContainsKey(HashSet<string> collection, string key) => collection.Contains(key);
        protected override bool ContainsKey(HashSet<string> collection, ReadOnlySpan<char> key) =>
            collection.GetAlternateLookup<ReadOnlySpan<char>>().Contains(key);
        protected override IEqualityComparer<string> GetComparer(HashSet<string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalIgnoreCaseComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => StringComparer.OrdinalIgnoreCase;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => randomizedOrdinalIgnoreCaseComparerType;
    }

    public class InternalHashCodeTests_HashSet_LinguisticComparer : InternalHashCodeTests<HashSet<string>> // (not optimized)
    {
        protected override HashSet<string> CreateCollection() => new HashSet<string>(StringComparer.InvariantCulture);
        protected override void AddKey(HashSet<string> collection, string key) => collection.Add(key);
        protected override bool ContainsKey(HashSet<string> collection, string key) => collection.Contains(key);
        protected override bool ContainsKey(HashSet<string> collection, ReadOnlySpan<char> key) =>
            collection.GetAlternateLookup<ReadOnlySpan<char>>().Contains(key);
        protected override IEqualityComparer<string> GetComparer(HashSet<string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => StringComparer.InvariantCulture.GetType();
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => StringComparer.InvariantCulture;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => StringComparer.InvariantCulture.GetType();
    }
    #endregion

    #region OrderedDictionary
    public class InternalHashCodeTests_OrderedDictionary_NullComparer : InternalHashCodeTests<OrderedDictionary<string, string>>
    {
        protected override OrderedDictionary<string, string> CreateCollection() => new OrderedDictionary<string, string>();
        protected override void AddKey(OrderedDictionary<string, string> collection, string key) => collection.Add(key, key);
        protected override bool ContainsKey(OrderedDictionary<string, string> collection, string key) => collection.ContainsKey(key);
        protected override IEqualityComparer<string> GetComparer(OrderedDictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => EqualityComparer<string>.Default;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => EqualityComparer<string>.Default.GetType();

        protected override bool SupportsAlternateLookup(OrderedDictionary<string, string> collection) => false;
    }

    public class InternalHashCodeTests_OrderedDictionary_DefaultComparer : InternalHashCodeTests<OrderedDictionary<string, string>>
    {
        protected override OrderedDictionary<string, string> CreateCollection() => new OrderedDictionary<string, string>(EqualityComparer<string>.Default);
        protected override void AddKey(OrderedDictionary<string, string> collection, string key) => collection.Add(key, key);
        protected override bool ContainsKey(OrderedDictionary<string, string> collection, string key) => collection.ContainsKey(key);
        protected override IEqualityComparer<string> GetComparer(OrderedDictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => EqualityComparer<string>.Default;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => EqualityComparer<string>.Default.GetType();

        protected override bool SupportsAlternateLookup(OrderedDictionary<string, string> collection) => false;
    }

    public class InternalHashCodeTests_OrderedDictionary_OrdinalComparer : InternalHashCodeTests<OrderedDictionary<string, string>>
    {
        protected override OrderedDictionary<string, string> CreateCollection() => new OrderedDictionary<string, string>(StringComparer.Ordinal);
        protected override void AddKey(OrderedDictionary<string, string> collection, string key) => collection.Add(key, key);
        protected override bool ContainsKey(OrderedDictionary<string, string> collection, string key) => collection.ContainsKey(key);
        protected override IEqualityComparer<string> GetComparer(OrderedDictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => StringComparer.Ordinal;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => StringComparer.Ordinal.GetType();

        protected override bool SupportsAlternateLookup(OrderedDictionary<string, string> collection) => false;
    }

    public class InternalHashCodeTests_OrderedDictionary_OrdinalIgnoreCaseComparer : InternalHashCodeTests<OrderedDictionary<string, string>>
    {
        protected override OrderedDictionary<string, string> CreateCollection() => new OrderedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        protected override void AddKey(OrderedDictionary<string, string> collection, string key) => collection.Add(key, key);
        protected override bool ContainsKey(OrderedDictionary<string, string> collection, string key) => collection.ContainsKey(key);
        protected override IEqualityComparer<string> GetComparer(OrderedDictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedOrdinalIgnoreCaseComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => StringComparer.OrdinalIgnoreCase;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => StringComparer.OrdinalIgnoreCase.GetType();

        protected override bool SupportsAlternateLookup(OrderedDictionary<string, string> collection) => false;
    }

    public class InternalHashCodeTests_OrderedDictionary_LinguisticComparer : InternalHashCodeTests<OrderedDictionary<string, string>> // (not optimized)
    {
        protected override OrderedDictionary<string, string> CreateCollection() => new OrderedDictionary<string, string>(StringComparer.InvariantCulture);
        protected override void AddKey(OrderedDictionary<string, string> collection, string key) => collection.Add(key, key);
        protected override bool ContainsKey(OrderedDictionary<string, string> collection, string key) => collection.ContainsKey(key);
        protected override IEqualityComparer<string> GetComparer(OrderedDictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => StringComparer.InvariantCulture.GetType();
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => StringComparer.InvariantCulture;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => StringComparer.InvariantCulture.GetType();

        protected override bool SupportsAlternateLookup(OrderedDictionary<string, string> collection) => false;
    }
    #endregion

    public abstract class InternalHashCodeTests<TCollection>
    {
        protected static Type nonRandomizedOrdinalComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.NonRandomizedStringEqualityComparer+OrdinalComparer", throwOnError: true);
        protected static Type nonRandomizedOrdinalIgnoreCaseComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.NonRandomizedStringEqualityComparer+OrdinalIgnoreCaseComparer", throwOnError: true);
        protected static Type randomizedOrdinalComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.RandomizedStringEqualityComparer+OrdinalComparer", throwOnError: true);
        protected static Type randomizedOrdinalIgnoreCaseComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.RandomizedStringEqualityComparer+OrdinalIgnoreCaseComparer", throwOnError: true);

        protected abstract TCollection CreateCollection();
        protected abstract void AddKey(TCollection collection, string key);
        protected abstract bool ContainsKey(TCollection collection, string key);
        protected abstract IEqualityComparer<string> GetComparer(TCollection collection);
        protected virtual bool SupportsAlternateLookup(TCollection collection) => true;
        protected virtual bool ContainsKey(TCollection collection, ReadOnlySpan<char> key) => throw new NotSupportedException();

        protected abstract Type ExpectedInternalComparerTypeBeforeCollisionThreshold { get; }
        protected abstract IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold { get; }
        protected abstract Type ExpectedInternalComparerTypeAfterCollisionThreshold { get; }

        [Fact]
        public void ComparerImplementations_Dictionary_WithWellKnownStringComparers()
        {
            TCollection collection = CreateCollection();
            List<string> allKeys = new List<string>();

            // First, go right up to the collision threshold, but don't exceed it.

            for (int i = 0; i < 100; i++)
            {
                string newKey = _collidingStrings[i];
                AddKey(collection, newKey);
                allKeys.Add(newKey);
            }

            FieldInfo internalComparerField = collection.GetType().GetField("_comparer", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(internalComparerField);

            IEqualityComparer<string> actualInternalComparerBeforeCollisionThreshold = (IEqualityComparer<string>)internalComparerField.GetValue(collection);
            ValidateBehaviorOfInternalComparerVsPublicComparer(actualInternalComparerBeforeCollisionThreshold, ExpectedPublicComparerBeforeCollisionThreshold);

            Assert.Equal(ExpectedInternalComparerTypeBeforeCollisionThreshold, actualInternalComparerBeforeCollisionThreshold?.GetType());
            Assert.Equal(ExpectedPublicComparerBeforeCollisionThreshold, GetComparer(collection));

            // Now exceed the collision threshold, which should rebucket entries.
            // Continue adding a few more entries to ensure we didn't corrupt internal state.

            for (int i = 100; i < 110; i++)
            {
                string newKey = _collidingStrings[i];
                Assert.Equal(CollidingOrdinalHashCode, _lazyGetNonRandomizedHashCodeDel.Value(newKey)); // ensure all keys share one hash code Ordinal
                Assert.Equal(CollidingOrdinalHashCode, _lazyGetNonRandomizedOrdinalIgnoreCaseHashCodeDel.Value(newKey)); // ... and OrdinalIgnoreCase

                AddKey(collection, newKey);
                allKeys.Add(newKey);
            }

            IEqualityComparer<string> actualInternalComparerAfterCollisionThreshold = (IEqualityComparer<string>)internalComparerField.GetValue(collection);
            ValidateBehaviorOfInternalComparerVsPublicComparer(actualInternalComparerAfterCollisionThreshold, ExpectedPublicComparerBeforeCollisionThreshold);

            Assert.Equal(ExpectedInternalComparerTypeAfterCollisionThreshold, actualInternalComparerAfterCollisionThreshold?.GetType());
            Assert.Equal(ExpectedPublicComparerBeforeCollisionThreshold, GetComparer(collection)); // shouldn't change this return value after collision threshold met

            // And validate that all strings are present in the dictionary.

            foreach (string key in allKeys)
            {
                Assert.True(ContainsKey(collection, key));
                if (SupportsAlternateLookup(collection))
                {
                    Assert.True(ContainsKey(collection, key.AsSpan()));
                }
            }

            // Also make sure we didn't accidentally put the internal comparer in the serialized object data.

            collection = CreateCollection();
            if (collection is ISerializable)
            {
                SerializationInfo si = new SerializationInfo(collection.GetType(), new FormatterConverter());
                ((ISerializable)collection).GetObjectData(si, new StreamingContext());

                object serializedComparer = si.GetValue("Comparer", typeof(IEqualityComparer<string>));
                Assert.Equal(ExpectedPublicComparerBeforeCollisionThreshold, serializedComparer);
            }
        }

        private static Lazy<Func<string, int>> _lazyGetNonRandomizedHashCodeDel = new Lazy<Func<string, int>>(
            () => GetStringHashCodeOpenDelegate("GetNonRandomizedHashCode"));

        private static Lazy<Func<string, int>> _lazyGetNonRandomizedOrdinalIgnoreCaseHashCodeDel = new Lazy<Func<string, int>>(
            () => GetStringHashCodeOpenDelegate("GetNonRandomizedHashCodeOrdinalIgnoreCase"));

        // n.b., must be initialized *after* delegate fields above
        private const int CollidingOrdinalHashCode = unchecked((int)0x93D1146A);

        private static readonly List<string> _collidingStrings = GenerateCollidingStrings(110);

        private static List<string> GenerateCollidingStrings(int count)
        {
            // Every code unit is taken from the Unicode Private Use Area with bit 0x20 set.
            // Those are uncased, so ToUpperOrdinal leaves them alone, and they already carry
            // the bit that ignore-case hashing ORs in, so a string built from them has the
            // same non-randomized hash code under Ordinal and under OrdinalIgnoreCase, and
            // no two of them compare equal OrdinalIgnoreCase.
            List<ushort> pool = new List<ushort>();
            for (int c = 0xE000; c <= 0xF8FF; c++)
            {
                if ((c & 0x20) != 0)
                {
                    pool.Add((ushort)c);
                }
            }

            // For an eight char string the hash is a function of two 64-bit halves, the
            // first offset by a length term. Pinning the value the two halves are combined
            // into pins the hash, which lets the second half be chosen freely and the first
            // half be solved for. Only the solutions that land back inside the pool are kept.
            const ulong HashSeed = 0x9E3779B185EBCA87;
            const uint HashPrime = 0x9E3779B1u;
            ulong lengthTerm = unchecked(16u * HashPrime); // computed in 32-bit arithmetic

            ulong Pack(ushort a, ushort b, ushort c, ushort d) =>
                a | ((ulong)b << 16) | ((ulong)c << 32) | ((ulong)d << 48);

            ulong seedSecond = Pack(pool[0], pool[1], pool[2], pool[3]);
            ulong seedFirst = lengthTerm + Pack(pool[4], pool[5], pool[6], pool[7]);
            ulong combined = (seedFirst ^ BitOperations.RotateLeft(seedSecond, 27)) + BitOperations.RotateLeft(seedSecond, 41);

            List<string> collidingStrings = new List<string>(count);
            for (int i0 = 0; i0 < pool.Count && collidingStrings.Count < count; i0++)
            for (int i1 = 0; i1 < pool.Count && collidingStrings.Count < count; i1++)
            for (int i2 = 0; i2 < pool.Count && collidingStrings.Count < count; i2++)
            for (int i3 = 0; i3 < pool.Count && collidingStrings.Count < count; i3++)
            {
                ulong second = Pack(pool[i0], pool[i1], pool[i2], pool[i3]);
                ulong first = ((combined - BitOperations.RotateLeft(second, 41)) ^ BitOperations.RotateLeft(second, 27)) - lengthTerm;

                ushort c0 = (ushort)first, c1 = (ushort)(first >> 16), c2 = (ushort)(first >> 32), c3 = (ushort)(first >> 48);
                if (!InPool(c0) || !InPool(c1) || !InPool(c2) || !InPool(c3))
                {
                    continue;
                }

                string candidate = new string(new char[]
                {
                    (char)c0, (char)c1, (char)c2, (char)c3,
                    (char)pool[i0], (char)pool[i1], (char)pool[i2], (char)pool[i3]
                });

                // The construction above encodes the current implementation, so verify it
                // rather than trusting it. If the hash changes these asserts fire here,
                // pointing at the generator instead of at every test that consumes it.
                Assert.Equal(CollidingOrdinalHashCode, _lazyGetNonRandomizedHashCodeDel.Value(candidate));
                Assert.Equal(CollidingOrdinalHashCode, _lazyGetNonRandomizedOrdinalIgnoreCaseHashCodeDel.Value(candidate));
                collidingStrings.Add(candidate);
            }

            if (collidingStrings.Count < count)
            {
                throw new Exception($"Couldn't create enough colliding strings? Created {collidingStrings.Count}, needed {count}.");
            }

            _ = HashSeed; // documents the multiplier the construction relies on
            return collidingStrings;

            static bool InPool(ushort c) => c >= 0xE000 && c <= 0xF8FF && (c & 0x20) != 0;
        }

        private static Func<string, int> GetStringHashCodeOpenDelegate(string methodName)
        {
            MethodInfo method = typeof(string).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            return method.CreateDelegate<Func<string, int>>(target: null); // create open delegate unbound to 'this'
        }

        private static void ValidateBehaviorOfInternalComparerVsPublicComparer(IEqualityComparer<string> internalComparer, IEqualityComparer<string> publicComparer)
        {
            // This helper ensures that when we substitute one of our internal comparers
            // in place of the expected public comparer, the internal comparer's Equals
            // and GetHashCode behavior are consistent with the public comparer's.

            if (internalComparer is null)
            {
                internalComparer = EqualityComparer<string>.Default;
            }
            if (publicComparer is null)
            {
                publicComparer = EqualityComparer<string>.Default;
            }
            foreach (var pair in new[] {
                ("Hello", "Hello"), // exactly equal
                ("Hello", "Goodbye"), // not equal at all
                ("Hello", "hello"), // case-insensitive equal
                ("Hello", "He\u200dllo"), // equal under linguistic comparer
                ("Hello", "HE\u200dLLO"), // equal under case-insensitive linguistic comparer
                ("\u0430\u0431\u0432\u0433\u0434\u0435\u0451\u0436\u0437\u0438\u0439\u043A\u043B\u043C\u043D\u043E\u043F\u0440\u0441\u0442\u0443\u0444\u0445\u0446\u0447\u0448\u0449\u044C\u044B\u044A\u044D\u044E\u044F", "\u0410\u0411\u0412\u0413\u0414\u0415\u0401\u0416\u0417\u0418\u0419\u041A\u041B\u041C\u041D\u041E\u041F\u0420\u0421\u0422\u0423\u0424\u0425\u0426\u0427\u0428\u0429\u042C\u042B\u042A\u042D\u042E\u042F"), // Cyrillic, case-insensitive equal
            })
            {
                bool arePairElementsExpectedEqual = publicComparer.Equals(pair.Item1, pair.Item2);
                Assert.Equal(arePairElementsExpectedEqual, internalComparer.Equals(pair.Item1, pair.Item2));

                bool areInternalHashCodesEqual = internalComparer.GetHashCode(pair.Item1) == internalComparer.GetHashCode(pair.Item2);
                if (arePairElementsExpectedEqual)
                {
                    Assert.True(areInternalHashCodesEqual);
                }
                else if (!areInternalHashCodesEqual)
                {
                    Assert.False(arePairElementsExpectedEqual);
                }
            }
        }
    }
}
