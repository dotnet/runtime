// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionaryGetEnumeratorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(50)]
        public void GetEnumerator(int count)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count);
            Assert.NotSame(hybridDictionary.GetEnumerator(), hybridDictionary.GetEnumerator());
            Assert.IsAssignableFrom<IDictionaryEnumerator>(((IEnumerable)hybridDictionary).GetEnumerator());

            IDictionaryEnumerator enumerator = hybridDictionary.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(enumerator.Current, enumerator.Current);
                    Assert.Equal(enumerator.Entry, enumerator.Entry);

                    DictionaryEntry entry = (DictionaryEntry)enumerator.Current;
                    Assert.Equal(entry.Key, enumerator.Entry.Key);
                    Assert.Equal(entry.Value, enumerator.Entry.Value);
                    Assert.Equal(enumerator.Key, enumerator.Entry.Key);
                    Assert.Equal(enumerator.Value, enumerator.Entry.Value);
                    Assert.Equal(entry.Value, hybridDictionary[entry.Key]);
                    counter++;
                }
                Assert.False(enumerator.MoveNext());
                Assert.Equal(hybridDictionary.Count, counter);
                enumerator.Reset();
            }
        }

        [Fact]
        public void GetEnumerator_CaseInsensitive()
        {
            HybridDictionary hybridDictionary = new HybridDictionary(true);
            Assert.NotNull(hybridDictionary.GetEnumerator());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(50)]
        public void GetEnumerator_Invalid(int count)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count);
            IDictionaryEnumerator enumerator = hybridDictionary.GetEnumerator();
            
            // Enumeration has not yet started
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
            Assert.Throws<InvalidOperationException>(() => enumerator.Key);
            Assert.Throws<InvalidOperationException>(() => enumerator.Value);

            // Enumeration has finished
            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
            Assert.Throws<InvalidOperationException>(() => enumerator.Key);
            Assert.Throws<InvalidOperationException>(() => enumerator.Value);

            // Enumerator has been reset
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
            Assert.Throws<InvalidOperationException>(() => enumerator.Key);
            Assert.Throws<InvalidOperationException>(() => enumerator.Value);

            if (count > 0)
            {
                // Underlying collection has been modified during enumeration
                enumerator.MoveNext();
                DictionaryEntry current = (DictionaryEntry)enumerator.Current;
                hybridDictionary.Add("key1", "value");
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                Assert.Equal(current, enumerator.Current);
                Assert.Equal(current, enumerator.Entry);
                Assert.Equal(current.Key, enumerator.Key);
                Assert.Equal(current.Value, enumerator.Value);

                // Underlying collection has been modified after enumeration
                enumerator = hybridDictionary.GetEnumerator();

                for (int i = 0; i < hybridDictionary.Count; i++)
                {
                    enumerator.MoveNext();
                }

                current = (DictionaryEntry)enumerator.Current;
                hybridDictionary.Add("key2", "value");
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                Assert.Equal(current, enumerator.Current);
                Assert.Equal(current, enumerator.Entry);
                Assert.Equal(current.Key, enumerator.Key);
                Assert.Equal(current.Value, enumerator.Value);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(50)]
        public void GetEnumerator_IEnumerable(int count)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count);
            IEnumerable enumerable = hybridDictionary;
            Assert.NotSame(enumerable.GetEnumerator(), enumerable.GetEnumerator());

            IEnumerator enumerator = enumerable.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    DictionaryEntry entry = (DictionaryEntry)enumerator.Current;
                    Assert.Equal(entry.Value, hybridDictionary[entry.Key]);
                    counter++;
                }
                Assert.False(enumerator.MoveNext());
                Assert.Equal(hybridDictionary.Count, counter);
                enumerator.Reset();
            }
        }

        [Fact]
        public void GetEnumerator_IEnumerable_CaseInsensitive()
        {
            IEnumerable hybridDictionary = new HybridDictionary(true);
            Assert.NotNull(hybridDictionary.GetEnumerator());
        }
    }
}
