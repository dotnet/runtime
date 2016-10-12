// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class BufferedStream_NS17
    {
        protected Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }

        public void EndCallback(IAsyncResult ar)
        { }

        [Fact]
        public void BeginEndReadTest()
        {
            Stream stream = CreateStream();
            IAsyncResult result = stream.BeginRead(new byte[1], 0, 1, new AsyncCallback(EndCallback), new object());
            stream.EndRead(result);
        }

        [Fact]
        public void BeginEndWriteTest()
        {
            Stream stream = CreateStream();
            IAsyncResult result = stream.BeginWrite(new byte[1], 0, 1, new AsyncCallback(EndCallback), new object());
            stream.EndWrite(result);
        }
    }

}
