// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class PipeOptionsTests
    {
        [Fact]
        public void DefaultPauseWriterThresholdIsSet()
        {
            Assert.Equal(65536, PipeOptions.Default.PauseWriterThreshold);
        }

        [Fact]
        public void DefaultResumeWriterThresholdIsSet()
        {
            Assert.Equal(32768, PipeOptions.Default.ResumeWriterThreshold);
        }

        [Fact]
        public void DefaultPoolIsSetToShared()
        {
            Assert.Equal(MemoryPool<byte>.Shared, PipeOptions.Default.Pool);
        }
    }
}
