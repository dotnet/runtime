// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace SslStress.Utils
{
    public static class PipeExtensions
    {
        // Adapted from https://devblogs.microsoft.com/dotnet/system-io-pipelines-high-performance-io-in-net/
        public static async Task ReadLinesUsingPipesAsync(this Stream stream, Func<ReadOnlySequence<byte>, Task> callback, CancellationToken token = default, char separator = '\n')
        {
            var pipe = new Pipe();

            try
            {
                await StressTaskExtensions.WhenAllThrowOnFirstException(token, FillPipeAsync, ReadPipeAsync);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {

            }

            async Task FillPipeAsync(CancellationToken token)
            {
                try 
                {
                    await stream.CopyToAsync(pipe.Writer, token);
                }
                catch (Exception e)
                {
                    pipe.Writer.Complete(e);
                    throw;
                }

                pipe.Writer.Complete();
            }

            async Task ReadPipeAsync(CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    ReadResult result = await pipe.Reader.ReadAsync(token);
                    ReadOnlySequence<byte> buffer = result.Buffer;
                    SequencePosition? position;

                    do
                    {
                        position = buffer.PositionOf((byte)separator);

                        if (position != null)
                        {
                            await callback(buffer.Slice(0, position.Value));
                            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                        }
                    }
                    while (position != null);

                    pipe.Reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
        }
    }
}
