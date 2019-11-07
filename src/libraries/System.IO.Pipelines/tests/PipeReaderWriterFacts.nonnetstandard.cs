// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public partial class PipelineReaderWriterFacts : IDisposable
    {
        [Fact]
        public async Task ResetAfterCompleteReaderAndWriterWithoutAdvancingClearsEverything()
        {
           _pipe.Writer.WriteEmpty(4094);
           _pipe.Writer.WriteEmpty(4094);
           await _pipe.Writer.FlushAsync();
           ReadResult result = await _pipe.Reader.ReadAsync();
           ReadOnlySequence<byte> buffer = result.Buffer;

           SequenceMarshal.TryGetReadOnlySequenceSegment(
               buffer,
               out ReadOnlySequenceSegment<byte> start,
               out int startIndex,
               out ReadOnlySequenceSegment<byte> end,
               out int endIndex);

           var startSegment = (BufferSegment)start;
           var endSegment = (BufferSegment)end;
           Assert.NotNull(startSegment.MemoryOwner);
           Assert.NotNull(endSegment.MemoryOwner);

           _pipe.Reader.Complete();

           // Nothing cleaned up
           Assert.NotNull(startSegment.MemoryOwner);
           Assert.NotNull(endSegment.MemoryOwner);

           _pipe.Writer.Complete();

           // Should be cleaned up now
           Assert.Null(startSegment.MemoryOwner);
           Assert.Null(endSegment.MemoryOwner);

           _pipe.Reset();
        }
    }
}
