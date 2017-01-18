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
    }

}