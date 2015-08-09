// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace StreamReaderTests
{
    public class AsyncStream
    {
        [Fact]
        public static async Task CopyToTest() {
            var ms = new MemoryStream();
            for (int i = 0; i < 1000; i++)
            {
                ms.WriteByte((byte)(i % 256));
            }
            ms.Position = 0;

            var ms2 = new MemoryStream();
            await ms.CopyToAsync(ms2);

            var buffer = ms2.ToArray();
            for (int i = 0; i < 1000; i++)
            {
                Assert.Equal(i % 256, buffer[i]);
            }

        }
    }
}
