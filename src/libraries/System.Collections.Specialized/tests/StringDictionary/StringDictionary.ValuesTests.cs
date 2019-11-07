// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryValuesTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Values(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);

            ICollection values = stringDictionary.Values;
            Assert.Equal(count, values.Count);

            stringDictionary.Add("duplicatevaluekey1", "value");
            stringDictionary.Add("duplicatevaluekey2", "value");
            Assert.Equal(count + 2, values.Count);

            IEnumerator enumerator = stringDictionary.GetEnumerator();
            foreach (string value in values)
            {
                enumerator.MoveNext();
                DictionaryEntry entry = (DictionaryEntry)enumerator.Current;

                Assert.Equal(value, entry.Value);
                Assert.True(stringDictionary.ContainsValue(value));
            }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(5, 0)]
        [InlineData(5, 1)]
        public void Values_CopyTo(int count, int index)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);
            ICollection values = stringDictionary.Values;

            string[] array = new string[count + index + 5];
            values.CopyTo(array, index);

            IEnumerator enumerator = stringDictionary.GetEnumerator();
            for (int i = 0; i < index; i++)
            {
                Assert.Null(array[i]);
            }
            for (int i = index; i < index + count; i++)
            {
                enumerator.MoveNext();
                DictionaryEntry entry = (DictionaryEntry)enumerator.Current;

                string value = array[i];
                Assert.Equal(entry.Value, value);
                Assert.True(stringDictionary.ContainsValue(value));
            }
            for (int i = index + count; i < array.Length; i++)
            {
                Assert.Null(array[i]);
            }
        }
    }
}
