// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionarySyncRootTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        public void SyncRoot(int count)
        {
            HybridDictionary hybridDictionary1 = Helpers.CreateHybridDictionary(count);
            HybridDictionary hybridDictionary2 = Helpers.CreateHybridDictionary(count);

            Assert.Same(hybridDictionary1.SyncRoot, hybridDictionary1.SyncRoot);
            Assert.NotSame(hybridDictionary1.SyncRoot, hybridDictionary2.SyncRoot);
        }
    }
}
