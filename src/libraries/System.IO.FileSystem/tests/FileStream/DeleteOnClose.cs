// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_DeleteOnClose : FileSystemTest
    {
        [InlineData(FileMode.Append)] // FileModes that open an existing file, or create a new one when it doesn't exist.
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.OpenOrCreate)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsFileLockingEnabled), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task DeleteOnClose_UsableAsMutex(FileMode mode)
        {
            var cts = new CancellationTokenSource();
            int enterCount = 0;
            int locksRemaining = int.MaxValue;
            bool exclusive = true;

            string path = GetTestFilePath();
            Assert.False(File.Exists(path));

            Func<Task> lockFile = async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        using (var fs = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, FileOptions.DeleteOnClose))
                        {
                            int counter = Interlocked.Increment(ref enterCount);
                            if (counter != 1)
                            {
                                exclusive = false;
                                cts.Cancel();
                                return;
                            }

                            // Hold the lock for a little bit.
                            await Task.Delay(TimeSpan.FromMilliseconds(5));

                            Interlocked.Decrement(ref enterCount);

                            if (Interlocked.Decrement(ref locksRemaining) <= 0)
                            {
                                return;
                            }
                        }
                        await Task.Delay(TimeSpan.FromMilliseconds(1));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // This can occur when the file is being deleted on Windows.
                        await Task.Delay(TimeSpan.FromMilliseconds(1));
                    }
                    catch (IOException)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(1));
                    }
                }
            };

            var tasks = new Task[50];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(lockFile);
            }

            // Wait for 1000 locks.
            cts.CancelAfter(TimeSpan.FromSeconds(100));
            Volatile.Write(ref locksRemaining, 500);
            await Task.WhenAll(tasks);

            Assert.True(exclusive, "Exclusive");
            Assert.False(cts.IsCancellationRequested, "Test cancelled");
            Assert.False(File.Exists(path), "File exists");
        }
    }
}
