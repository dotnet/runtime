// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryClearTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void Clear(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);

            stringDictionary.Clear();
            Assert.Equal(0, stringDictionary.Count);

            stringDictionary.Clear();
            Assert.Equal(0, stringDictionary.Count);
        }
    }
}
