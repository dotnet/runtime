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
        protected override IEqualityComparer<string> GetComparer(Dictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedDefaultComparerType;
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
        protected override IEqualityComparer<string> GetComparer(Dictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedDefaultComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => EqualityComparer<string>.Default;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => randomizedOrdinalComparerType;
    }

    public class InternalHashCodeTests_Dictionary_OrdinalComparer : InternalHashCodeTests<Dictionary<string, string>>
    {
        protected override Dictionary<string, string> CreateCollection() => new Dictionary<string, string>(StringComparer.Ordinal);
        protected override void AddKey(Dictionary<string, string> collection, string key) => collection.Add(key, key);
        protected override bool ContainsKey(Dictionary<string, string> collection, string key) => collection.ContainsKey(key);
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
        protected override IEqualityComparer<string> GetComparer(HashSet<string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedDefaultComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => EqualityComparer<string>.Default;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => randomizedOrdinalComparerType;
    }

    public class InternalHashCodeTests_HashSet_DefaultComparer : InternalHashCodeTests<HashSet<string>>
    {
        protected override HashSet<string> CreateCollection() => new HashSet<string>(EqualityComparer<string>.Default);
        protected override void AddKey(HashSet<string> collection, string key) => collection.Add(key);
        protected override bool ContainsKey(HashSet<string> collection, string key) => collection.Contains(key);
        protected override IEqualityComparer<string> GetComparer(HashSet<string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedDefaultComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => EqualityComparer<string>.Default;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => randomizedOrdinalComparerType;
    }

    public class InternalHashCodeTests_HashSet_OrdinalComparer : InternalHashCodeTests<HashSet<string>>
    {
        protected override HashSet<string> CreateCollection() => new HashSet<string>(StringComparer.Ordinal);
        protected override void AddKey(HashSet<string> collection, string key) => collection.Add(key);
        protected override bool ContainsKey(HashSet<string> collection, string key) => collection.Contains(key);
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

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedDefaultComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => EqualityComparer<string>.Default;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => EqualityComparer<string>.Default.GetType();
    }

    public class InternalHashCodeTests_OrderedDictionary_DefaultComparer : InternalHashCodeTests<OrderedDictionary<string, string>>
    {
        protected override OrderedDictionary<string, string> CreateCollection() => new OrderedDictionary<string, string>(EqualityComparer<string>.Default);
        protected override void AddKey(OrderedDictionary<string, string> collection, string key) => collection.Add(key, key);
        protected override bool ContainsKey(OrderedDictionary<string, string> collection, string key) => collection.ContainsKey(key);
        protected override IEqualityComparer<string> GetComparer(OrderedDictionary<string, string> collection) => collection.Comparer;

        protected override Type ExpectedInternalComparerTypeBeforeCollisionThreshold => nonRandomizedDefaultComparerType;
        protected override IEqualityComparer<string> ExpectedPublicComparerBeforeCollisionThreshold => EqualityComparer<string>.Default;
        protected override Type ExpectedInternalComparerTypeAfterCollisionThreshold => EqualityComparer<string>.Default.GetType();
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
    }
    #endregion

    public abstract class InternalHashCodeTests<TCollection>
    {
        protected static Type nonRandomizedDefaultComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.NonRandomizedStringEqualityComparer+DefaultComparer", throwOnError: true);
        protected static Type nonRandomizedOrdinalComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.NonRandomizedStringEqualityComparer+OrdinalComparer", throwOnError: true);
        protected static Type nonRandomizedOrdinalIgnoreCaseComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.NonRandomizedStringEqualityComparer+OrdinalIgnoreCaseComparer", throwOnError: true);
        protected static Type randomizedOrdinalComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.RandomizedStringEqualityComparer+OrdinalComparer", throwOnError: true);
        protected static Type randomizedOrdinalIgnoreCaseComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.RandomizedStringEqualityComparer+OrdinalIgnoreCaseComparer", throwOnError: true);

        protected abstract TCollection CreateCollection();
        protected abstract void AddKey(TCollection collection, string key);
        protected abstract bool ContainsKey(TCollection collection, string key);
        protected abstract IEqualityComparer<string> GetComparer(TCollection collection);

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
                Assert.Equal(0, _lazyGetNonRandomizedHashCodeDel.Value(newKey)); // ensure has a zero hash code Ordinal
                Assert.Equal(0x24716ca0, _lazyGetNonRandomizedOrdinalIgnoreCaseHashCodeDel.Value(newKey)); // ensure has a zero hash code OrdinalIgnoreCase

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
        private static readonly List<string> _collidingStrings = GenerateCollidingStrings(110);

        private static List<string> GenerateCollidingStrings(int count)
        {
            const int StartOfRange = 0xE020; // use the Unicode Private Use range to avoid accidentally creating strings that really do compare as equal OrdinalIgnoreCase
            const int Stride = 0x40; // to ensure we don't accidentally reset the 0x20 bit of the seed, which is used to negate OrdinalIgnoreCase effects

            int currentSeed = StartOfRange;

            List<string> collidingStrings = new List<string>(count);
            while (collidingStrings.Count < count)
            {
                if (currentSeed > ushort.MaxValue)
                {
                    throw new Exception($"Couldn't create enough colliding strings? Created {collidingStrings.Count}, needed {count}.");
                }

                string candidate = GenerateCollidingStringCandidate(currentSeed);

                int ordinalHashCode = _lazyGetNonRandomizedHashCodeDel.Value(candidate);
                Assert.Equal(0, ordinalHashCode); // ensure has a zero hash code Ordinal

                int ordinalIgnoreCaseHashCode = _lazyGetNonRandomizedOrdinalIgnoreCaseHashCodeDel.Value(candidate);
                if (ordinalIgnoreCaseHashCode == 0x24716ca0) // ensure has a zero hash code OrdinalIgnoreCase (might not have one)
                {
                    collidingStrings.Add(candidate); // success!
                }

                currentSeed += Stride;
            }

            return collidingStrings;

            // Generates a possible string with a well-known non-randomized hash code:
            // - string.GetNonRandomizedHashCode returns 0.
            // - string.GetNonRandomizedHashCodeOrdinalIgnoreCase returns 0x24716ca0.
            // Provide a different seed to produce a different string.
            // Caller must check OrdinalIgnoreCase hash code to ensure correctness.
            static string GenerateCollidingStringCandidate(int seed)
            {
                return string.Create(8, seed, (span, seed) =>
                {
                    Span<byte> asBytes = MemoryMarshal.AsBytes(span);

                    uint hash1 = (5381 << 16) + 5381;
                    uint hash2 = BitOperations.RotateLeft(hash1, 5) + hash1;

                    MemoryMarshal.Write(asBytes, in seed);
                    MemoryMarshal.Write(asBytes.Slice(4), in hash2); // set hash2 := 0 (for Ordinal)

                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (uint)seed;
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1);

                    MemoryMarshal.Write(asBytes.Slice(8), in hash1); // set hash1 := 0 (for Ordinal)
                });
            }
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
