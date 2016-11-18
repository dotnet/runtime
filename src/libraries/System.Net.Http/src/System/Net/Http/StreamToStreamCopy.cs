// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// Helper class is used to copy the content of a source stream to a destination stream,
    /// with optimizations based on expected usage within HttpClient and with the ability
    /// to dispose of the source stream when the copy has completed.
    /// </summary>
    internal static class StreamToStreamCopy
    {
        /// <summary>Copies the source stream from its current position to the destination stream at its current position.</summary>
        /// <param name="source">The source stream from which to copy.</param>
        /// <param name="destination">The destination stream to which to copy.</param>
        /// <param name="bufferSize">The size of the buffer to allocate if one needs to be allocated.</param>
        /// <param name="disposeSource">Whether to dispose of the source stream after the copy has finished successfully.</param>
        /// <param name="cancellationToken">CancellationToken used to cancel the copy operation.</param>
        public static Task CopyAsync(Stream source, Stream destination, int bufferSize, bool disposeSource, CancellationToken cancellationToken = default(CancellationToken))
        {
            Debug.Assert(source != null);
            Debug.Assert(destination != null);
            Debug.Assert(bufferSize > 0);

            try
            {
                Task copyTask = source.CopyToAsync(destination, bufferSize, cancellationToken);
                return disposeSource ?
                    DisposeSourceWhenCompleteAsync(copyTask, source) :
                    copyTask;
            }
            catch (Exception e)
            {
                // For compatibility with the previous implementation, catch everything (including arg exceptions) and
                // store errors into the task rather than letting them propagate to the synchronous caller.
                return Task.FromException(e);
            }
        }

        private static Task DisposeSourceWhenCompleteAsync(Task task, Stream source)
        {
            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                    DisposeSource(source);
                    return Task.CompletedTask;

                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                default:
                    return task.ContinueWith((completed, innerSource) =>
                    {
                        completed.GetAwaiter().GetResult(); // propagate any exceptions
                        DisposeSource((Stream)innerSource);
                    },
                    source, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        /// <summary>Disposes the source stream if <paramref name="disposeSource"/> is true.</summary>
        private static void DisposeSource(Stream source)
        {
            try
            {
                source.Dispose();
            }
            catch (Exception e)
            {
                // Dispose() should never throw, but since we're on an async codepath, make sure to catch the exception.
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, e);
            }
        }
    }
}
