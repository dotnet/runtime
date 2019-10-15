// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryCopyToTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(5, 0)]
        [InlineData(5, 1)]
        public void CopyTo(int count, int index)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);

            DictionaryEntry[] array = new DictionaryEntry[count + index + 5];
            stringDictionary.CopyTo(array, index);

            IEnumerator enumerator = stringDictionary.GetEnumerator();
            for (int i = 0; i < index; i++)
            {
                Assert.Equal(default(DictionaryEntry), array[i]);
            }
            for (int i = index; i < index + count; i++)
            {
                enumerator.MoveNext();
                DictionaryEntry entry = array[i];
                Assert.Equal(enumerator.Current, entry);
                Assert.Equal(entry.Value, stringDictionary[(string)entry.Key]);
            }
            for (int i = index + count; i < array.Length; i++)
            {
                Assert.Equal(default(DictionaryEntry), array[i]);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void CopyTo_Invalid(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(10);

            Assert.Throws<ArgumentNullException>(() => stringDictionary.CopyTo(null, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => stringDictionary.CopyTo(new string[count], -1));
            AssertExtensions.Throws<ArgumentException>(null, () => stringDictionary.CopyTo(new string[count], count / 2 + 1));
            AssertExtensions.Throws<ArgumentException>(null, () => stringDictionary.CopyTo(new string[count], count));
            AssertExtensions.Throws<ArgumentException>(null, () => stringDictionary.CopyTo(new string[count], count + 1));
            AssertExtensions.Throws<ArgumentException>("array", null, () => stringDictionary.CopyTo(new string[count, count], 0));
        }
    }
}
