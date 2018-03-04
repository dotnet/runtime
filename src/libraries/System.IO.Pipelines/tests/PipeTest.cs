// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Pipelines.Tests
{
    public abstract class PipeTest : IDisposable
    {
        protected const int MaximumSizeHigh = 65;

        protected const int MaximumSizeLow = 6;

        private readonly TestMemoryPool _pool;

        protected Pipe Pipe;

        protected PipeTest(int pauseWriterThreshold = MaximumSizeHigh, int resumeWriterThreshold = MaximumSizeLow)
        {
            _pool = new TestMemoryPool();
            Pipe = new Pipe(
                new PipeOptions(
                    _pool,
                    pauseWriterThreshold: pauseWriterThreshold,
                    resumeWriterThreshold: resumeWriterThreshold,
                    readerScheduler: PipeScheduler.Inline,
                    writerScheduler: PipeScheduler.Inline,
                    useSynchronizationContext: false
                ));
        }

        public void Dispose()
        {
            Pipe.Writer.Complete();
            Pipe.Reader.Complete();
            _pool.Dispose();
        }
    }
}
