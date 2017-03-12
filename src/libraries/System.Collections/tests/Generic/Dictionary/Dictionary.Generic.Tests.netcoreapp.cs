using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public abstract partial class Dictionary_Generic_Tests<TKey, TValue> : IDictionary_Generic_Tests<TKey, TValue>
    {
        [Fact]
        public void Dictionary_Generic_Constructor_IEnumerable_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>("collection", () => new Dictionary<TKey, TValue>((IEnumerable<KeyValuePair<TKey, TValue>>)null));
            Assert.Throws<ArgumentNullException>("collection", () => new Dictionary<TKey, TValue>((IEnumerable<KeyValuePair<TKey, TValue>>)null, EqualityComparer<TKey>.Default));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Dictionary_Generic_Constructor_IEnumerable(int count)
        {
            IEnumerable<KeyValuePair<TKey, TValue>> collection = GenericIEnumerableFactory(count);
            var copied = new Dictionary<TKey, TValue>(collection);
            Assert.Equal(count, collection.Count());
            Assert.Equal(collection, copied);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Dictionary_Generic_Constructor_IEnumerable_IEqualityComparer(int count)
        {
            IEqualityComparer<TKey> comparer = GetKeyIEqualityComparer();
            IEnumerable<KeyValuePair<TKey, TValue>> collection = GenericIEnumerableFactory(count);
            var copied = new Dictionary<TKey, TValue>(collection, comparer);
            Assert.Equal(collection, copied);
        }

        [Fact]
        public void GetValueOrDefault_KeyExists_ReturnsValue()
        {
            int seed = 9600;
            TKey key = CreateTKey(seed++);
            TValue value = CreateTValue(seed++);
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>() { { key, value } };

            Assert.Equal(value, dictionary.GetValueOrDefault(key));
            Assert.Equal(value, dictionary.GetValueOrDefault(key, CreateTValue(seed++)));
        }

        [Fact]
        public void GetValueOrDefault_KeyDoesntExist_ReturnsDefaultValue()
        {
            int seed = 9600;
            TKey key = CreateTKey(seed++);
            TValue defaultValue = CreateTValue(seed++);
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>() { { CreateTKey(seed++), CreateTValue(seed++) } };
            Assert.Equal(default(TValue), dictionary.GetValueOrDefault(key));
            Assert.Equal(defaultValue, dictionary.GetValueOrDefault(key, defaultValue));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Dictionary_Generic_TryAdd(int count)
        {
            var dictionary = (Dictionary<TKey, TValue>)GenericIDictionaryFactory(count);

            TKey key = CreateTKey(seed: count);
            TValue value = CreateTValue(seed: count);
            dictionary.Remove(key);
            int originalCount = dictionary.Count;

            Assert.True(dictionary.TryAdd(key, value));
            Assert.Equal(originalCount + 1, dictionary.Count);
            Assert.Equal(dictionary[key], value);

            // After adding the key, make sure that trying to add it again fails.
            originalCount = dictionary.Count;

            Assert.False(dictionary.TryAdd(key, default(TValue)));
            Assert.Equal(originalCount, dictionary.Count);
            Assert.Equal(dictionary[key], value);
        }
    }
}
