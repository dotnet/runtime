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
    public class InternalHashCodeTests
    {
        /// <summary>
        /// Given a byte array, copies it to the string, without messing with any encoding.  This issue was hit on a x64 machine
        /// </summary>
        private static string GetString(byte[] bytes)
        {
            var chars = new char[bytes.Length / sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        [Fact]
        [OuterLoop("Takes over 55% of System.Collections.Tests testing time")]
        public static void OutOfBoundsRegression()
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

        [Fact]
        public static void ComparerImplementations_Dictionary_WithWellKnownStringComparers()
        {
            Type nonRandomizedOrdinalComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.NonRandomizedStringEqualityComparer+OrdinalComparer", throwOnError: true);
            Type nonRandomizedOrdinalIgnoreCaseComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.NonRandomizedStringEqualityComparer+OrdinalIgnoreCaseComparer", throwOnError: true);
            Type randomizedOrdinalComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.RandomizedStringEqualityComparer+OrdinalComparer", throwOnError: true);
            Type randomizedOrdinalIgnoreCaseComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.RandomizedStringEqualityComparer+OrdinalIgnoreCaseComparer", throwOnError: true);

            // null comparer

            RunDictionaryTest(
                equalityComparer: null,
                expectedInternalComparerBeforeCollisionThreshold: nonRandomizedOrdinalComparerType,
                expectedPublicComparerBeforeCollisionThreshold: EqualityComparer<string>.Default.GetType(),
                expectedComparerAfterCollisionThreshold: randomizedOrdinalComparerType);

            // EqualityComparer<string>.Default comparer

            RunDictionaryTest(
                equalityComparer: EqualityComparer<string>.Default,
                expectedInternalComparerBeforeCollisionThreshold: nonRandomizedOrdinalComparerType,
                expectedPublicComparerBeforeCollisionThreshold: EqualityComparer<string>.Default.GetType(),
                expectedComparerAfterCollisionThreshold: randomizedOrdinalComparerType);

            // Ordinal comparer

            RunDictionaryTest(
                equalityComparer: StringComparer.Ordinal,
                expectedInternalComparerBeforeCollisionThreshold: nonRandomizedOrdinalComparerType,
                expectedPublicComparerBeforeCollisionThreshold: StringComparer.Ordinal.GetType(),
                expectedComparerAfterCollisionThreshold: randomizedOrdinalComparerType);

            // OrdinalIgnoreCase comparer

            RunDictionaryTest(
                equalityComparer: StringComparer.OrdinalIgnoreCase,
                expectedInternalComparerBeforeCollisionThreshold: nonRandomizedOrdinalIgnoreCaseComparerType,
                expectedPublicComparerBeforeCollisionThreshold: StringComparer.OrdinalIgnoreCase.GetType(),
                expectedComparerAfterCollisionThreshold: randomizedOrdinalIgnoreCaseComparerType);

            // linguistic comparer (not optimized)

            RunDictionaryTest(
                equalityComparer: StringComparer.InvariantCulture,
                expectedInternalComparerBeforeCollisionThreshold: StringComparer.InvariantCulture.GetType(),
                expectedPublicComparerBeforeCollisionThreshold: StringComparer.InvariantCulture.GetType(),
                expectedComparerAfterCollisionThreshold: StringComparer.InvariantCulture.GetType());

            static void RunDictionaryTest(
                IEqualityComparer<string> equalityComparer,
                Type expectedInternalComparerBeforeCollisionThreshold,
                Type expectedPublicComparerBeforeCollisionThreshold,
                Type expectedComparerAfterCollisionThreshold)
            {
                RunCollectionTestCommon(
                    () => new Dictionary<string, object>(equalityComparer),
                    (dictionary, key) => dictionary.Add(key, null),
                    (dictionary, key) => dictionary.ContainsKey(key),
                    dictionary => dictionary.Comparer,
                    expectedInternalComparerBeforeCollisionThreshold,
                    expectedPublicComparerBeforeCollisionThreshold,
                    expectedComparerAfterCollisionThreshold);
            }
        }

        [Fact]
        public static void ComparerImplementations_HashSet_WithWellKnownStringComparers()
        {
            Type nonRandomizedOrdinalComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.NonRandomizedStringEqualityComparer+OrdinalComparer", throwOnError: true);
            Type nonRandomizedOrdinalIgnoreCaseComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.NonRandomizedStringEqualityComparer+OrdinalIgnoreCaseComparer", throwOnError: true);
            Type randomizedOrdinalComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.RandomizedStringEqualityComparer+OrdinalComparer", throwOnError: true);
            Type randomizedOrdinalIgnoreCaseComparerType = typeof(object).Assembly.GetType("System.Collections.Generic.RandomizedStringEqualityComparer+OrdinalIgnoreCaseComparer", throwOnError: true);

            // null comparer

            RunHashSetTest(
                equalityComparer: null,
                expectedInternalComparerBeforeCollisionThreshold: nonRandomizedOrdinalComparerType,
                expectedPublicComparerBeforeCollisionThreshold: EqualityComparer<string>.Default.GetType(),
                expectedComparerAfterCollisionThreshold: randomizedOrdinalComparerType);

            // EqualityComparer<string>.Default comparer

            RunHashSetTest(
                equalityComparer: EqualityComparer<string>.Default,
                expectedInternalComparerBeforeCollisionThreshold: nonRandomizedOrdinalComparerType,
                expectedPublicComparerBeforeCollisionThreshold: EqualityComparer<string>.Default.GetType(),
                expectedComparerAfterCollisionThreshold: randomizedOrdinalComparerType);

            // Ordinal comparer

            RunHashSetTest(
                equalityComparer: StringComparer.Ordinal,
                expectedInternalComparerBeforeCollisionThreshold: nonRandomizedOrdinalComparerType,
                expectedPublicComparerBeforeCollisionThreshold: StringComparer.Ordinal.GetType(),
                expectedComparerAfterCollisionThreshold: randomizedOrdinalComparerType);

            // OrdinalIgnoreCase comparer

            RunHashSetTest(
                equalityComparer: StringComparer.OrdinalIgnoreCase,
                expectedInternalComparerBeforeCollisionThreshold: nonRandomizedOrdinalIgnoreCaseComparerType,
                expectedPublicComparerBeforeCollisionThreshold: StringComparer.OrdinalIgnoreCase.GetType(),
                expectedComparerAfterCollisionThreshold: randomizedOrdinalIgnoreCaseComparerType);

            // linguistic comparer (not optimized)

            RunHashSetTest(
                equalityComparer: StringComparer.InvariantCulture,
                expectedInternalComparerBeforeCollisionThreshold: StringComparer.InvariantCulture.GetType(),
                expectedPublicComparerBeforeCollisionThreshold: StringComparer.InvariantCulture.GetType(),
                expectedComparerAfterCollisionThreshold: StringComparer.InvariantCulture.GetType());

            static void RunHashSetTest(
                IEqualityComparer<string> equalityComparer,
                Type expectedInternalComparerBeforeCollisionThreshold,
                Type expectedPublicComparerBeforeCollisionThreshold,
                Type expectedComparerAfterCollisionThreshold)
            {
                RunCollectionTestCommon(
                    () => new HashSet<string>(equalityComparer),
                    (set, key) => Assert.True(set.Add(key)),
                    (set, key) => set.Contains(key),
                    set => set.Comparer,
                    expectedInternalComparerBeforeCollisionThreshold,
                    expectedPublicComparerBeforeCollisionThreshold,
                    expectedComparerAfterCollisionThreshold);
            }
        }

        private static void RunCollectionTestCommon<TCollection>(
            Func<TCollection> collectionFactory,
            Action<TCollection, string> addKeyCallback,
            Func<TCollection, string, bool> containsKeyCallback,
            Func<TCollection, IEqualityComparer<string>> getComparerCallback,
            Type expectedInternalComparerBeforeCollisionThreshold,
            Type expectedPublicComparerBeforeCollisionThreshold,
            Type expectedComparerAfterCollisionThreshold)
        {
            TCollection collection = collectionFactory();
            List<string> allKeys = new List<string>();

            const int StartOfRange = 0xE020; // use the Unicode Private Use range to avoid accidentally creating strings that really do compare as equal OrdinalIgnoreCase
            const int Stride = 0x40; // to ensure we don't accidentally reset the 0x20 bit of the seed, which is used to negate OrdinalIgnoreCase effects

            // First, go right up to the collision threshold, but don't exceed it.

            for (int i = 0; i < 100; i++)
            {
                string newKey = GenerateCollidingString(i * Stride + StartOfRange);
                Assert.Equal(0, _lazyGetNonRandomizedHashCodeDel.Value(newKey)); // ensure has a zero hash code Ordinal
                Assert.Equal(0x24716ca0, _lazyGetNonRandomizedOrdinalIgnoreCaseHashCodeDel.Value(newKey)); // ensure has a zero hash code OrdinalIgnoreCase

                addKeyCallback(collection, newKey);
                allKeys.Add(newKey);
            }

            FieldInfo internalComparerField = collection.GetType().GetField("_comparer", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(internalComparerField);

            Assert.Equal(expectedInternalComparerBeforeCollisionThreshold, internalComparerField.GetValue(collection)?.GetType());
            Assert.Equal(expectedPublicComparerBeforeCollisionThreshold, getComparerCallback(collection).GetType());

            // Now exceed the collision threshold, which should rebucket entries.
            // Continue adding a few more entries to ensure we didn't corrupt internal state.

            for (int i = 100; i < 110; i++)
            {
                string newKey = GenerateCollidingString(i * Stride + StartOfRange);
                Assert.Equal(0, _lazyGetNonRandomizedHashCodeDel.Value(newKey)); // ensure has a zero hash code Ordinal
                Assert.Equal(0x24716ca0, _lazyGetNonRandomizedOrdinalIgnoreCaseHashCodeDel.Value(newKey)); // ensure has a zero hash code OrdinalIgnoreCase

                addKeyCallback(collection, newKey);
                allKeys.Add(newKey);
            }

            Assert.Equal(expectedComparerAfterCollisionThreshold, internalComparerField.GetValue(collection)?.GetType());
            Assert.Equal(expectedPublicComparerBeforeCollisionThreshold, getComparerCallback(collection).GetType()); // shouldn't change this return value after collision threshold met

            // And validate that all strings are present in the dictionary.

            foreach (string key in allKeys)
            {
                Assert.True(containsKeyCallback(collection, key));
            }

            // Also make sure we didn't accidentally put the internal comparer in the serialized object data.

            collection = collectionFactory();
            SerializationInfo si = new SerializationInfo(collection.GetType(), new FormatterConverter());
            ((ISerializable)collection).GetObjectData(si, new StreamingContext());

            object serializedComparer = si.GetValue("Comparer", typeof(IEqualityComparer<string>));
            Assert.Equal(expectedPublicComparerBeforeCollisionThreshold, serializedComparer.GetType());
        }

        private static Lazy<Func<string, int>> _lazyGetNonRandomizedHashCodeDel = new Lazy<Func<string, int>>(
            () => GetStringHashCodeOpenDelegate("GetNonRandomizedHashCode"));

        private static Lazy<Func<string, int>> _lazyGetNonRandomizedOrdinalIgnoreCaseHashCodeDel = new Lazy<Func<string, int>>(
            () => GetStringHashCodeOpenDelegate("GetNonRandomizedHashCodeOrdinalIgnoreCase"));

        // Generates a string with a well-known non-randomized hash code:
        // - string.GetNonRandomizedHashCode returns 0.
        // - string.GetNonRandomizedHashCodeOrdinalIgnoreCase returns 0x24716ca0.
        // Provide a different seed to produce a different string.
        private static string GenerateCollidingString(int seed)
        {
            return string.Create(8, seed, (span, seed) =>
            {
                Span<byte> asBytes = MemoryMarshal.AsBytes(span);

                uint hash1 = (5381 << 16) + 5381;
                uint hash2 = BitOperations.RotateLeft(hash1, 5) + hash1;

                MemoryMarshal.Write(asBytes, ref seed);
                MemoryMarshal.Write(asBytes.Slice(4), ref hash2); // set hash2 := 0 (for Ordinal)

                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (uint)seed;
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1);

                MemoryMarshal.Write(asBytes.Slice(8), ref hash1); // set hash1 := 0 (for Ordinal)
            });
        }

        private static Func<string, int> GetStringHashCodeOpenDelegate(string methodName)
        {
            MethodInfo method = typeof(string).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            return method.CreateDelegate<Func<string, int>>(target: null); // create open delegate unbound to 'this'
        }
    }
}
