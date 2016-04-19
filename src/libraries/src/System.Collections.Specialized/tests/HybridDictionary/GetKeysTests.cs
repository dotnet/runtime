// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionaryKeysTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(5, 0)]
        [InlineData(5, 0)]
        [InlineData(50, 0)]
        [InlineData(50, 1)]
        public void CopyTo(int count, int index)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count);
            object[] copy = new object[count + index + 5];
            hybridDictionary.Keys.CopyTo(copy, index);

            IDictionaryEnumerator enumerator = hybridDictionary.GetEnumerator();
            for (int i = 0; i < index; i++)
            {
                Assert.Null(copy[i]);
            }
            for (int i = index; i < index + count; i++)
            {
                enumerator.MoveNext();
                object key = copy[i];
                Assert.Equal(enumerator.Key, key);
                Assert.True(hybridDictionary.Contains(key));
            }
            for (int i = index + count; i < copy.Length; i++)
            {
                Assert.Null(copy[i]);
            }
        }
    }
}
