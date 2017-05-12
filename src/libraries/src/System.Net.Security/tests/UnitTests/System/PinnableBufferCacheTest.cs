// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Security.Tests
{
    public class PinnableBufferCacheTest
    {
        [Fact]
        public void PinnableBufferCache_AllocateBuffer_Ok()
        {
            string cacheName = "Test";
            int numberOfElements = 5;
            PinnableBufferCache p = new PinnableBufferCache(cacheName, numberOfElements);

            byte[] a = p.AllocateBuffer();

            Assert.Equal(numberOfElements, a.Length);
            foreach (byte t in a)
            {
                Assert.Equal(0, t);
            }
        }
    }
}
