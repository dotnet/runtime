// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class StreamAPMTests
    {
        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        private void EndCallback(IAsyncResult ar)
        {
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void BeginEndReadTest()
        {
            Stream stream = CreateStream();
            IAsyncResult result = stream.BeginRead(new byte[1], 0, 1, new AsyncCallback(EndCallback), new object());
            stream.EndRead(result);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void BeginEndWriteTest()
        {
            Stream stream = CreateStream();
            IAsyncResult result = stream.BeginWrite(new byte[1], 0, 1, new AsyncCallback(EndCallback), new object());
            stream.EndWrite(result);
        }
    }
}
