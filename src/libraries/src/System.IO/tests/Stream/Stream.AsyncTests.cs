// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class StreamAsync
    {
        protected virtual Stream CreateStream() => new MemoryStream();

        [Fact]
        public async Task CopyToAsyncTest()
        {
            byte[] data = Enumerable.Range(0, 1000).Select(i => (byte)(i % 256)).ToArray();

            Stream ms = CreateStream();
            ms.Write(data, 0, data.Length);
            ms.Position = 0;

            var ms2 = new MemoryStream();
            await ms.CopyToAsync(ms2);

            Assert.Equal(data, ms2.ToArray());
        }
    }
}
