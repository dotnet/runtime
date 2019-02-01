// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionarySyncRootTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // Different implementation
        public void SyncRoot(int count)
        {
            StringDictionary stringDictionary1 = Helpers.CreateStringDictionary(count);
            StringDictionary stringDictionary2 = Helpers.CreateStringDictionary(count);

            Assert.Same(stringDictionary1.SyncRoot, stringDictionary1.SyncRoot);
            Assert.IsType<Hashtable>(stringDictionary1.SyncRoot);

            Assert.NotSame(stringDictionary1.SyncRoot, stringDictionary2.SyncRoot);
        }
    }
}
