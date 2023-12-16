// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionGetValuesStringTests
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [InlineData(0)]
        [InlineData(5)]
        public void GetValues_NoSuchName_ReturnsNull(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);
            Assert.Null(nameValueCollection.GetValues("no-such-name"));
            Assert.Null(nameValueCollection.GetValues(null));
        }
    }
}
