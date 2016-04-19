// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionaryCopyToTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(50)]
        public void CopyTo(int count)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count, caseInsensitive: false);
            VerifyCopyTo(hybridDictionary, 0);
            VerifyCopyTo(hybridDictionary, 1);

            hybridDictionary = Helpers.CreateHybridDictionary(count, caseInsensitive: true);
            VerifyCopyTo(hybridDictionary, 0);
            VerifyCopyTo(hybridDictionary, 1);
        }

        private void VerifyCopyTo(HybridDictionary hybridDictionary, int index)
        {
            DictionaryEntry[] copy = new DictionaryEntry[hybridDictionary.Count + index + 5];
            hybridDictionary.CopyTo(copy, index);

            IDictionaryEnumerator enumerator = hybridDictionary.GetEnumerator();
            for (int i = 0; i < index; i++)
            {
                Assert.Equal(default(DictionaryEntry), copy[i]);
            }
            for (int i = index; i < index + hybridDictionary.Count; i++)
            {
                enumerator.MoveNext();
                DictionaryEntry entry = copy[i];
                Assert.Equal(enumerator.Entry, entry);
                Assert.Equal(entry.Value, hybridDictionary[entry.Key]);
            }
            for (int i = index + hybridDictionary.Count; i < copy.Length; i++)
            {
                Assert.Equal(default(DictionaryEntry), copy[i]);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(50)]
        public void CopyTo_Invalid(int count)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count);

            Assert.Throws<ArgumentOutOfRangeException>(() => hybridDictionary.CopyTo(new DictionaryEntry[count], -1));

            Assert.Throws<ArgumentException>(() => hybridDictionary.CopyTo(new DictionaryEntry[count + 1], 2));
            Assert.Throws<ArgumentException>(() => hybridDictionary.CopyTo(new DictionaryEntry[count], count + 1));

            if (count > 0)
            {
                Assert.Throws<ArgumentException>(() => hybridDictionary.CopyTo(new DictionaryEntry[count], count));
                Assert.Throws<ArgumentException>(() => hybridDictionary.CopyTo(new DictionaryEntry[count, count], 0));
                Assert.Throws<InvalidCastException>(() => hybridDictionary.CopyTo(new string[count], 0));
            }
            else
            {
                // Should not throw, even though the type of the array is invalid
                hybridDictionary.CopyTo(new string[0], 0);
                hybridDictionary.CopyTo(new DictionaryEntry[0, 0], 0);
            }
        }
    }
}
