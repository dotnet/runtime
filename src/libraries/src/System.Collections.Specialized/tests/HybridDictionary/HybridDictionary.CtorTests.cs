// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionaryCtorTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            HybridDictionary hybridDictionary = new HybridDictionary();
            VerifyCtor(hybridDictionary, caseInsensitive: false);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(50)]
        public void Ctor_Int(int initialSize)
        {
            HybridDictionary hybridDictionary = new HybridDictionary(initialSize);
            VerifyCtor(hybridDictionary, caseInsensitive: false);

            Ctor_Int_Bool(initialSize, true);
            Ctor_Int_Bool(initialSize, false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool caseInsensitive)
        {
            HybridDictionary hybridDictionary = new HybridDictionary(caseInsensitive);
            VerifyCtor(hybridDictionary, caseInsensitive: caseInsensitive);
        }

        public void Ctor_Int_Bool(int initialSize, bool caseInsensitive)
        {
            HybridDictionary hybridDictionary = new HybridDictionary(initialSize, caseInsensitive);
            VerifyCtor(hybridDictionary, caseInsensitive: caseInsensitive);
        }

        public static void VerifyCtor(HybridDictionary hybridDictionary, bool caseInsensitive)
        {
            Assert.Equal(0, hybridDictionary.Count);
            Assert.Equal(0, hybridDictionary.Keys.Count);
            Assert.Equal(0, hybridDictionary.Values.Count);

            Assert.False(hybridDictionary.IsFixedSize);
            Assert.False(hybridDictionary.IsReadOnly);
            Assert.False(hybridDictionary.IsSynchronized);

            if (caseInsensitive)
            {
                hybridDictionary["key"] = "value";
                Assert.Equal("value", hybridDictionary["KEY"]);
            }
            else
            {
                hybridDictionary["key"] = "value";
                Assert.Null(hybridDictionary["KEY"]);
            }
        }
    }
}
