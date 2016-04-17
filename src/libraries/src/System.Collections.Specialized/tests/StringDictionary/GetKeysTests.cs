// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryKeysTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Keys(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);
            stringDictionary.Add("duplicatevaluekey1", "value");
            stringDictionary.Add("duplicatevaluekey2", "value");

            ICollection keys = stringDictionary.Keys;
            Assert.Equal(count + 2, keys.Count);

            foreach (string key in keys)
            {
                Assert.True(stringDictionary.ContainsKey(key));
            }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(5, 0)]
        [InlineData(5, 1)]
        public void Key_CopyTo(int count, int index)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);
            ICollection keys = stringDictionary.Keys;

            string[] array = new string[count + index + 5];

            keys.CopyTo(array, index);

            for (int i = 0; i < index; i++)
            {
                Assert.Null(array[i]);
            }
            for (int i = index; i < index + count; i++)
            {
                string key = array[i];
                Assert.True(stringDictionary.ContainsKey(key));
            }
            for (int i = index + count; i < array.Length; i++)
            {
                Assert.Null(array[i]);
            }
        }
    }
}
