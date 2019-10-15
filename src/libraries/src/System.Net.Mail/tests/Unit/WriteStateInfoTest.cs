// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Mime.Tests
{
    public class WriteStateInfoTest
    {
        [Fact]
        public void WriteStateInfo_WithSmallBuffer_WhenResize_ShouldDoubleBufferSize()
        {
            int bufferSize = 1024;
            var stateInfo = new Base64WriteStateInfo(bufferSize, null, null, 0, 0);

            // Fill the buffer + 1
            for (int i = 0; i <= bufferSize; i++)
            {
                stateInfo.Append((byte)'a');
            }

            Assert.Equal(bufferSize * 2, stateInfo.Buffer.Length);
        }

        [Fact]
        public void WriteStateInfo_WithSmallBuffer_WhenResizeAlot_ShouldQuadrupleBufferSize()
        {
            int bufferSize = 1024;
            var stateInfo = new Base64WriteStateInfo(bufferSize, null, null, 0, 0);

            // Fill the buffer * 2 + 1, Make it resize twice
            for (int i = 0; i <= bufferSize * 2; i++)
            {
                stateInfo.Append((byte)'a');
            }

            Assert.Equal(bufferSize * 4, stateInfo.Buffer.Length);
        }
    }
}
