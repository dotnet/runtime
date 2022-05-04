// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.IO.Tests
{
    public class FileSystemWatcher_Multiple_Test : FileSystemWatcherTest
    {
        private readonly ITestOutputHelper _output;

        private void OnError(object source, ErrorEventArgs e)
        {
            string msg = $"Watcher failed: {e.GetException()} source={source} {source.GetHashCode()}";
            _output.WriteLine(msg);
            // Repeat on Console so it easier to triage in CI.
            Console.WriteLine(msg);
        }

        public FileSystemWatcher_Multiple_Test(ITestOutputHelper output)
        {
            _output = output;
        }

        [OuterLoop]
        [Fact]
        public void FileSystemWatcher_File_Create_ExecutionContextFlowed()
        {
            using (var watcher1 = new FileSystemWatcher(TestDirectory))
            using (var watcher2 = new FileSystemWatcher(TestDirectory))
            {
                string fileName = Path.Combine(TestDirectory, "file");
                watcher1.Filter = Path.GetFileName(fileName);
                watcher2.Filter = Path.GetFileName(fileName);

                var local = new AsyncLocal<int>();

                var tcs1 = new TaskCompletionSource<int>();
                var tcs2 = new TaskCompletionSource<int>();
                watcher1.Created += (s, e) => tcs1.SetResult(local.Value);
                watcher2.Created += (s, e) => tcs2.SetResult(local.Value);

                watcher1.Error += OnError;
                watcher2.Error += OnError;

                local.Value = 42;
                watcher1.EnableRaisingEvents = true;
                local.Value = 84;
                watcher2.EnableRaisingEvents = true;
                local.Value = 168;

                File.Create(fileName).Dispose();
                Task.WaitAll(new[] { tcs1.Task, tcs2.Task }, WaitForExpectedEventTimeout);

                Assert.Equal(42, tcs1.Task.Result);
                Assert.Equal(84, tcs2.Task.Result);
            }
        }

        [OuterLoop]
        [Fact]
        public void FileSystemWatcher_File_Create_SuppressedExecutionContextHandled()
        {
            using (var watcher1 = new FileSystemWatcher(TestDirectory))
            {
                string fileName = Path.Combine(TestDirectory, "FileSystemWatcher_File_Create_SuppressedExecutionContextHandled");
                watcher1.Filter = Path.GetFileName(fileName);
                watcher1.Error += OnError;

                var local = new AsyncLocal<int>();

                var tcs1 = new TaskCompletionSource<int>();
                watcher1.Created += (s, e) => tcs1.SetResult(local.Value);

                local.Value = 42;

                ExecutionContext.SuppressFlow();
                try
                {
                    watcher1.EnableRaisingEvents = true;
                }
                finally
                {
                    ExecutionContext.RestoreFlow();
                }

                    File.Create(fileName).Dispose();
                    tcs1.Task.Wait(WaitForExpectedEventTimeout);

                    Assert.Equal(0, tcs1.Task.Result);
           }
        }

        [OuterLoop]
        [Fact]
        public void FileSystemWatcher_File_Create_NotAffectEachOther()
        {
            using (var watcher1 = new FileSystemWatcher(TestDirectory))
            using (var watcher2 = new FileSystemWatcher(TestDirectory))
            using (var watcher3 = new FileSystemWatcher(TestDirectory))
            {
                string fileName = Path.Combine(TestDirectory, "FileSystemWatcher_File_Create_NotAffectEachOther");
                watcher1.Filter = Path.GetFileName(fileName);
                watcher2.Filter = Path.GetFileName(fileName);
                watcher3.Filter = Path.GetFileName(fileName);

                watcher1.Error += OnError;
                watcher2.Error += OnError;
                watcher3.Error += OnError;

                AutoResetEvent autoResetEvent1 = WatchCreated(watcher1, new[] { fileName }).EventOccured;
                AutoResetEvent autoResetEvent2 = WatchCreated(watcher2, new[] { fileName }).EventOccured;
                AutoResetEvent autoResetEvent3 = WatchCreated(watcher3, new[] { fileName }).EventOccured;

                watcher1.EnableRaisingEvents = true;
                watcher2.EnableRaisingEvents = true;
                watcher3.EnableRaisingEvents = true;

                File.Create(fileName).Dispose();
                Assert.True(WaitHandle.WaitAll(new[] { autoResetEvent1, autoResetEvent2, autoResetEvent3 }, WaitForExpectedEventTimeout_NoRetry));

                File.Delete(fileName);
                watcher1.EnableRaisingEvents = false;

                File.Create(fileName).Dispose();
                Assert.False(autoResetEvent1.WaitOne(WaitForUnexpectedEventTimeout));
                Assert.True(WaitHandle.WaitAll(new[] { autoResetEvent2, autoResetEvent3 }, WaitForExpectedEventTimeout_NoRetry));
            }
        }

        [OuterLoop]
        [Fact]
        public void FileSystemWatcher_File_Create_WatchOwnPath()
        {
            string dir1 = CreateTestDirectory(TestDirectory, "dir1");
            string dir2 = CreateTestDirectory(TestDirectory, "dir2");
            using (var watcher1 = new FileSystemWatcher(dir1, "*"))
            using (var watcher2 = new FileSystemWatcher(dir2, "*"))
            {
                watcher1.Error += OnError;
                watcher2.Error += OnError;

                string fileName1 = Path.Combine(dir1, "file");
                string fileName2 = Path.Combine(dir2, "file");

                AutoResetEvent autoResetEvent1 = WatchCreated(watcher1, new[] { fileName1 }).EventOccured;
                AutoResetEvent autoResetEvent2 = WatchCreated(watcher2, new[] { fileName2 }).EventOccured;

                watcher1.EnableRaisingEvents = true;
                watcher2.EnableRaisingEvents = true;

                File.Create(fileName1).Dispose();
                Assert.True(autoResetEvent1.WaitOne(WaitForExpectedEventTimeout_NoRetry));
                Assert.False(autoResetEvent2.WaitOne(WaitForUnexpectedEventTimeout));

                File.Create(fileName2).Dispose();

                Assert.True(autoResetEvent2.WaitOne(WaitForExpectedEventTimeout_NoRetry));
                Assert.False(autoResetEvent1.WaitOne(WaitForUnexpectedEventTimeout));
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FileSystemWatcher_File_Create_ForceLoopRestart(bool useExistingWatchers)
        {
            FileSystemWatcher[] watchers = new FileSystemWatcher[64];
            FileSystemWatcher[] watchers1 = new FileSystemWatcher[64];

            try
            {
                string fileName = Path.Combine(TestDirectory, "FileSystemWatcher_File_Create_ForceLoopRestart");
                AutoResetEvent[] autoResetEvents = new AutoResetEvent[64];
                for (var i = 0; i < watchers.Length; i++)
                {
                    watchers[i] = new FileSystemWatcher(TestDirectory);
                    watchers[i].Filter = Path.GetFileName(fileName);
                    autoResetEvents[i] = WatchCreated(watchers[i], new[] { fileName }).EventOccured;
                    watchers[i].EnableRaisingEvents = true;
                }

                File.Create(fileName).Dispose();
                Assert.True(WaitHandle.WaitAll(autoResetEvents, WaitForExpectedEventTimeout_NoRetry));

                File.Delete(fileName);
                for (var i = 0; i < watchers.Length; i++)
                {
                    watchers[i].EnableRaisingEvents = false;
                }

                File.Create(fileName).Dispose();
                Assert.False(WaitHandle.WaitAll(autoResetEvents, WaitForUnexpectedEventTimeout));

                File.Delete(fileName);

                if (useExistingWatchers)
                {
                    for (var i = 0; i < watchers.Length; i++)
                    {
                        watchers[i].EnableRaisingEvents = true;
                    }

                    File.Create(fileName).Dispose();
                    Assert.True(WaitHandle.WaitAll(autoResetEvents, WaitForExpectedEventTimeout_NoRetry));
                }
                else
                {
                    AutoResetEvent[] autoResetEvents1 = new AutoResetEvent[64];
                    for (var i = 0; i < watchers1.Length; i++)
                    {
                        watchers1[i] = new FileSystemWatcher(TestDirectory);
                        watchers1[i].Filter = Path.GetFileName(fileName);
                        autoResetEvents1[i] = WatchCreated(watchers1[i], new[] { fileName }).EventOccured;
                        watchers1[i].EnableRaisingEvents = true;
                    }

                    File.Create(fileName).Dispose();
                    Assert.True(WaitHandle.WaitAll(autoResetEvents1, WaitForExpectedEventTimeout_NoRetry));
                }
            }
            finally
            {
                for (var i = 0; i < watchers.Length; i++)
                {
                    watchers[i]?.Dispose();
                    watchers1[i]?.Dispose();
                }
            }
        }

        [OuterLoop]
        [Fact]
        public void  FileSystemWatcher_File_Changed_NotAffectEachOther()
        {
            string file = CreateTestFile(TestDirectory, "apple");
            string otherFile = CreateTestFile(TestDirectory, "pear");
            using (var watcher1 = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            using (var watcher2 = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            using (var watcher3 = new FileSystemWatcher(TestDirectory, Path.GetFileName(otherFile)))
            {
                AutoResetEvent autoResetEvent1 = WatchChanged(watcher1, new[] { Path.Combine(TestDirectory, "apple") }).EventOccured;
                AutoResetEvent autoResetEvent2 = WatchChanged(watcher2, new[] { Path.Combine(TestDirectory, "apple") }).EventOccured;
                AutoResetEvent autoResetEvent3 = WatchChanged(watcher3, new[] { Path.Combine(TestDirectory, "pear") }).EventOccured;

                watcher1.Error += OnError;
                watcher2.Error += OnError;
                watcher3.Error += OnError;

                watcher1.EnableRaisingEvents = true;
                watcher2.EnableRaisingEvents = true;
                watcher3.EnableRaisingEvents = true;

                Directory.SetLastWriteTime(file, DateTime.Now + TimeSpan.FromSeconds(10));
                Assert.True(WaitHandle.WaitAll(new[] { autoResetEvent1, autoResetEvent2 }, WaitForExpectedEventTimeout_NoRetry));
                Assert.False(autoResetEvent3.WaitOne(WaitForUnexpectedEventTimeout));

                Directory.SetLastWriteTime(otherFile, DateTime.Now + TimeSpan.FromSeconds(10));
                Assert.False(WaitHandle.WaitAll(new[] { autoResetEvent1, autoResetEvent2 }, WaitForUnexpectedEventTimeout));
                Assert.True(autoResetEvent3.WaitOne(WaitForExpectedEventTimeout_NoRetry));

                watcher1.EnableRaisingEvents = false;

                Directory.SetLastWriteTime(file, DateTime.Now + TimeSpan.FromSeconds(10));
                Assert.False(WaitHandle.WaitAll(new[] { autoResetEvent1, autoResetEvent3 }, WaitForUnexpectedEventTimeout));
                Assert.True(autoResetEvent2.WaitOne(WaitForExpectedEventTimeout_NoRetry));

                Directory.SetLastWriteTime(otherFile, DateTime.Now + TimeSpan.FromSeconds(10));
                Assert.False(WaitHandle.WaitAll(new[] { autoResetEvent1, autoResetEvent2 }, WaitForUnexpectedEventTimeout));
                Assert.True(autoResetEvent3.WaitOne(WaitForExpectedEventTimeout_NoRetry));
            }
        }

        [OuterLoop]
        [Fact]
        public void FileSystemWatcher_File_Delet_NotAffectEachOther()
        {
            using (var watcher1 = new FileSystemWatcher(TestDirectory))
            using (var watcher2 = new FileSystemWatcher(TestDirectory))
            using (var watcher3 = new FileSystemWatcher(TestDirectory))
            {
                string fileName = CreateTestFile(TestDirectory, "file");

                watcher1.Filter = Path.GetFileName(fileName);
                watcher2.Filter = Path.GetFileName(fileName);
                watcher3.Filter = Path.GetFileName(fileName);

                watcher1.Error += OnError;
                watcher2.Error += OnError;
                watcher3.Error += OnError;

                AutoResetEvent autoResetEvent1 = WatchDeleted(watcher1, new[] { fileName }, _output).EventOccured;
                AutoResetEvent autoResetEvent2 = WatchDeleted(watcher2, new[] { fileName }, _output).EventOccured;
                AutoResetEvent autoResetEvent3 = WatchDeleted(watcher3, new[] { fileName }, _output).EventOccured;

                watcher1.EnableRaisingEvents = true;
                watcher2.EnableRaisingEvents = true;
                watcher3.EnableRaisingEvents = true;

                File.Delete(fileName);
                Assert.True(WaitHandle.WaitAll(new[] { autoResetEvent1, autoResetEvent2, autoResetEvent3 }, WaitForExpectedEventTimeout_NoRetry));

                File.Create(fileName).Dispose();
                watcher1.EnableRaisingEvents = false;

                File.Delete(fileName);
                Assert.False(autoResetEvent1.WaitOne(WaitForUnexpectedEventTimeout));
                Assert.True(WaitHandle.WaitAll(new[] { autoResetEvent2, autoResetEvent3 }, WaitForExpectedEventTimeout_NoRetry));
            }
        }

        [OuterLoop]
        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void FileSystemWatcher_File_Rename_NotAffectEachOther()
        {
            string file = CreateTestFile(TestDirectory, "file");
            using (var watcher1 = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            using (var watcher2 = new FileSystemWatcher(TestDirectory, Path.GetFileName(file)))
            {
                string filePath = file;
                string filePathRenamed = file + "_renamed";

                AutoResetEvent autoResetEvent1 = WatchRenamed(watcher1, new[] { filePathRenamed }, _output).EventOccured;
                AutoResetEvent autoResetEvent2 = WatchRenamed(watcher2, new[] { filePathRenamed }, _output).EventOccured;

                watcher1.Error += OnError;
                watcher2.Error += OnError;

                watcher1.EnableRaisingEvents = true;
                watcher2.EnableRaisingEvents = true;

                File.Move(filePath, filePathRenamed);
                Assert.True(WaitHandle.WaitAll(
                    new[] { autoResetEvent1, autoResetEvent2}, WaitForExpectedEventTimeout_NoRetry));

                File.Move(filePathRenamed, filePath);
                watcher1.EnableRaisingEvents = false;

                File.Move(filePath, filePathRenamed);
                Assert.False(autoResetEvent1.WaitOne(WaitForUnexpectedEventTimeout));
                Assert.True(autoResetEvent2.WaitOne(WaitForExpectedEventTimeout_NoRetry));
            }
        }
    }
}
