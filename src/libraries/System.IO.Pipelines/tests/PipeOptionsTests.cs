// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class PipeOptionsTests
    {
        public static IEnumerable<object[]> Default_ExpectedValues_MemberData()
        {
            yield return new object[] { PipeOptions.Default };
            yield return new object[] { new PipeOptions() };
            yield return new object[] { new PipeOptions(null, null, null, -1, -1, -1, true) };
        }

        [Theory]
        [MemberData(nameof(Default_ExpectedValues_MemberData))]
        public void Default_ExpectedValues(PipeOptions options)
        {
            Assert.Equal(65536, options.PauseWriterThreshold);
            Assert.Equal(32768, options.ResumeWriterThreshold);
            Assert.Equal(4096, options.MinimumSegmentSize);
            Assert.True(options.UseSynchronizationContext);
            Assert.Same(MemoryPool<byte>.Shared, options.Pool);
        }

        [Fact]
        public void InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pauseWriterThreshold", () => new PipeOptions(pauseWriterThreshold: -2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("resumeWriterThreshold", () => new PipeOptions(resumeWriterThreshold: -2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("resumeWriterThreshold", () => new PipeOptions(pauseWriterThreshold: 50, resumeWriterThreshold: 100));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("resumeWriterThreshold", () => new PipeOptions(pauseWriterThreshold: 1, resumeWriterThreshold: -1));
        }

        [Theory]
        [InlineData(-100)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void InvalidArgs_NoThrow(int minimumSegmentSize)
        {
            // There's currently no validation performed on PipeOptions.MinimumSegmentSize.
            new PipeOptions(minimumSegmentSize: minimumSegmentSize);
            new PipeOptions(minimumSegmentSize: minimumSegmentSize);
            new PipeOptions(minimumSegmentSize: minimumSegmentSize);
            new PipeOptions(minimumSegmentSize: minimumSegmentSize);
        }
    }
}
