using System;
using System.Runtime;

namespace FileSystemWatcherHeapLeak
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /*
             * Set both min and max IOCP threads to 1 to serialize directory change callbacks
             * and give GC a higher chance of winning the race after disposing the FileSystemWatcher.
             */
            ThreadPool.GetMinThreads(out int minWorker, out _);
            ThreadPool.GetMaxThreads(out int maxWorker, out _);
            ThreadPool.SetMinThreads(minWorker, 1);
            ThreadPool.SetMaxThreads(maxWorker, 1);

            Console.WriteLine($"Starting {nameof(FileSystemWatcher)} heap leak test...");
            Console.WriteLine("Press Ctrl+C to stop the test.");

            int disposeProbability = 100;
            while (true)
            {
                Console.Write("Enter probability (0-100) to dispose FileSystemWatcher after each run: ");
                var input = Console.ReadLine();
                if (int.TryParse(input, out disposeProbability) &&
                    disposeProbability >= 0 && disposeProbability <= 100)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Please enter a valid integer between 0 and 100.");
                }
            }

            int maxGeneration = 2;
            while (true)
            {
                Console.Write("Enter max GC generation to collect (0, 1, or 2): ");
                var input = Console.ReadLine();
                if (int.TryParse(input, out maxGeneration) && maxGeneration >= 0 && maxGeneration <= GC.MaxGeneration)
                {
                    break;
                }
                else
                {
                    Console.WriteLine($"Please enter a valid integer between 0 and {GC.MaxGeneration}.");
                }
            }

            bool compacting = false;
            while (true)
            {
                Console.Write("Enable compacting GC? (y/n): ");
                var input = Console.ReadLine();
                if (string.Equals(input, "y", StringComparison.OrdinalIgnoreCase))
                {
                    compacting = true;
                    break;
                }
                else if (string.Equals(input, "n", StringComparison.OrdinalIgnoreCase))
                {
                    compacting = false;
                    break;
                }
                else
                {
                    Console.WriteLine("Please enter 'y' or 'n'.");
                }
            }

            Console.WriteLine($"Spawning {minWorker} parallel test loops (one per minWorkerThread).");

            var tasks = new Task[minWorker];

            int i = 0;
            for (i = 0; i < minWorker - 1; i++)
            {
                int taskId = i; // for closure capture

                tasks[i] = Task.Run(() =>
                {
                    var rng = new Random(Environment.TickCount ^ taskId);

                    while (true)
                    {
                        try
                        {
                            bool wasDisposed = false;
                            FileSystemWatcher watcher = null;

                            try
                            {
                                watcher = new FileSystemWatcher(@"C:\Temp");
                                watcher.EnableRaisingEvents = true;

                                Thread.Sleep(rng.Next(5, 10)); // Keep the watcher alive for a short while
                            }
                            finally
                            {
                                wasDisposed = rng.Next(0, 100) < disposeProbability;

                                if (wasDisposed)
                                {
                                    watcher?.Dispose();
                                }

                                watcher = null;
                            }

                            /*
                             * When we reach here, there are 2 cases:
                             * 1) When we have disposed the FileSystemWatcher
                             *    Disposal of FileSystemWatcher will dispose the underlying directory SafeFileHandle and kick off a race between:
                             *    a) The IOCP thread executing the FileSystemWatcher directory changes callback, which will come in with
                             *       errorCode = ERROR_OPERATION_ABORTED, and simultaneously,
                             *    b) The dedicated Server GC threads trying to collect the heap triggered below.
                             *    If a) wins the race, everything is fine :)
                             *          WHY? The AsycState's WeakRef<FileSystemwatcher> in the callback will be intact, so watcher.ReadDirectoryChangesCallback
                             *          will be called, it will find out that the directory handle is invalid now, and it will kick off the next Monitor() call,
                             *          which will in turn find out that the directory handle is invalid now and proceed to properly call PreAllocatedOverlapped.Dispose()
                             *          to unpin and free up the pinned 8K byte[] buffer.
                             *    However, if b) wins the race, !!  WE HAVE A HEAP LEAK !!
                             *          WHY? The AsycState's WeakRef<FileSystemwatcher> in the callback will be nulled out as soon as watcher is queued for finalization,
                             *          especially since its a *SHORT* WeakRef, and the callback will not be able to find the watcher anymore.
                             *          so watcher.ReadDirectoryChangesCallback is not at all called, so there is no chance of disposing the PreAllocatedOverlapped
                             *          and freeing up the pinned 8K byte[] buffer and we have leaked it for eternity.
                             * 2) When we have not disposed the FileSystemWatcher
                             *    This case makes the heap leak fully deterministic, and not probabilistic, because the watcher is disposed from Finalizer thread,
                             *    which will dispose the underlying directory SafeFileHandle and schedule the directory changes callback on IOCP thread, but by that time,
                             *    the AsycState's WeakRef<FileSystemwatcher> in the callback will *ALWAYS* be nulled out, since its a *SHORT* WeakRef.
                             *    so watcher.ReadDirectoryChangesCallback is not at all called, so there is no chance of disposing the PreAllocatedOverlapped
                             *    and freeing up the pinned 8K byte[] buffer and we have leaked it for eternity.
                             * 
                             * Note: We have already enabled Server GC mode in the project file, so as to pause all threads
                             *       including the IOCP threads executing the FileSystemWatcher directory changes callbacks.
                             *       This will enhance the ability of GC to win the above race after disposing the watcher.
                             */

                            if (compacting)
                            {
                                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                            }
                            else
                            {
                                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.Default;
                            }

                            GC.Collect(
                                maxGeneration,
                                GCCollectionMode.Forced | GCCollectionMode.Aggressive,
                                blocking: true,
                                compacting: compacting);

                            var gcInfo = GC.GetGCMemoryInfo();

                            Console.WriteLine(
                                $"[Thread {taskId}] Watcher was {(wasDisposed ? string.Empty : "not ")}disposed, " +
                                $"GC Gen: {maxGeneration}, Compacting: {compacting}, " +
                                $"Commited Bytes = {gcInfo.TotalCommittedBytes}, " +
                                $"Heap Size Bytes = {gcInfo.HeapSizeBytes}, " +
                                $"Fragmented Bytes = {gcInfo.FragmentedBytes}, " +
                                $"Fragmentation % = {100.0 * (double)gcInfo.FragmentedBytes / (double)gcInfo.TotalCommittedBytes}, " +
                                $"Pinned Object Count = {gcInfo.PinnedObjectsCount}.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Thread {taskId}] Exception encountered but continuing to next iteration. Exception: {ex.Message}");
                        }
                    }
                });
            }

            /*
             * The last task is reserved for a thread that will allocate large amount of memory
             * on SOH in parallel with other threads, and give a random chance for those allocs
             * to survive GC generations, thus creating pockets of free memory between the byte[]
             * instances that get leaked by the activity of other threads, inducing fragmentation.
             * This thread is designed to resemble the SOH allocations that are made in our server app
             * to conduct its business, all of which are mostly ephemeral and short-lived.
             */
            tasks[i] = Task.Run(() =>
            {
                var rng = new Random(Environment.TickCount ^ i);

                const int chunkSize = 50 * 1024; // 50 KB (making sure we allocate on SOH to that it gets fragmented)
                const int minTotal = 500 * 1024; // 500 KB
                const int maxTotal = 1024 * 1024;// 1 MB

                while (true)
                {
                    int totalToAllocate = rng.Next(minTotal, maxTotal + 1);
                    int allocated = 0;
                    var allocations = new List<string>();

                    while (allocated < totalToAllocate)
                    {
                        int charCount = chunkSize / 2;
                        string s = new string('A', charCount);
                        allocations.Add(s);
                        allocated += chunkSize;
                    }

                    Thread.Sleep(rng.Next(5, 50)); // Hold on to allocs for a while, giving a random chance for it to survive GC generations
                    allocations.Clear();           // Then release them and other threads will force GC to collect them.
                }
            });

            Task.WaitAll(tasks); // This will block forever unless the process is killed
        }
    }
}