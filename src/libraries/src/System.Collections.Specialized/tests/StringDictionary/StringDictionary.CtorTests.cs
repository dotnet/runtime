// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryCtorTests
    {
        [Fact]
        public void Ctor()
        {
            StringDictionary stringDictionary = new StringDictionary();
            Assert.Equal(0, stringDictionary.Count);
            Assert.False(stringDictionary.IsSynchronized);
            Assert.Equal(0, stringDictionary.Keys.Count);
            Assert.Equal(0, stringDictionary.Values.Count);

            stringDictionary.Add("key", "value");
            Assert.False(stringDictionary.IsSynchronized);
        }
    }
}
