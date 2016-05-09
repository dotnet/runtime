// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class WaitForChangedTests : FileSystemWatcherTest
    {
        private const int SuccessTimeoutSeconds = 10;
        private const int BetweenOperationsDelayMilliseconds = 100;

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ZeroTimeout_TimesOut(bool enabledBeforeWait)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var fsw = new FileSystemWatcher(testDirectory.Path))
            {
                if (enabledBeforeWait) fsw.EnableRaisingEvents = true;
                AssertTimedOut(fsw.WaitForChanged(WatcherChangeTypes.All, 0));
                Assert.Equal(enabledBeforeWait, fsw.EnableRaisingEvents);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NonZeroTimeout_NoEvents_TimesOut(bool enabledBeforeWait)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var fsw = new FileSystemWatcher(testDirectory.Path))
            {
                if (enabledBeforeWait) fsw.EnableRaisingEvents = true;
                AssertTimedOut(fsw.WaitForChanged(0, 1));
                Assert.Equal(enabledBeforeWait, fsw.EnableRaisingEvents);
            }
        }

        [Theory]
        [InlineData(WatcherChangeTypes.Deleted, false)]
        [InlineData(WatcherChangeTypes.Created, true)]
        [InlineData(WatcherChangeTypes.Changed, false)]
        [InlineData(WatcherChangeTypes.Renamed, true)]
        [InlineData(WatcherChangeTypes.All, true)]
        public void NonZeroTimeout_NoActivity_TimesOut(WatcherChangeTypes changeType, bool enabledBeforeWait)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var fsw = new FileSystemWatcher(testDirectory.Path))
            {
                if (enabledBeforeWait) fsw.EnableRaisingEvents = true;
                AssertTimedOut(fsw.WaitForChanged(changeType, 1));
                Assert.Equal(enabledBeforeWait, fsw.EnableRaisingEvents);
            }
        }

        [Theory]
        [InlineData(WatcherChangeTypes.Created)]
        [InlineData(WatcherChangeTypes.Deleted)]
        public void Created_Success(WatcherChangeTypes changeType)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var fsw = new FileSystemWatcher(testDirectory.Path))
            using (var b = new Barrier(2))
            {
                Task<WaitForChangedResult> t = Task.Run(() =>
                {
                    b.SignalAndWait();
                    return fsw.WaitForChanged(changeType);
                });

                b.SignalAndWait();
                DateTimeOffset end = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(SuccessTimeoutSeconds);
                while (!t.IsCompleted && DateTimeOffset.UtcNow < end)
                {
                    string path = Path.Combine(testDirectory.Path, Path.GetRandomFileName());
                    File.Create(path).Dispose();
                    Task.Delay(BetweenOperationsDelayMilliseconds).Wait();
                    if ((changeType & WatcherChangeTypes.Deleted) != 0)
                    {
                        File.Delete(path);
                    }
                }

                Assert.True(t.IsCompleted, "WaitForChanged didn't complete");
                Assert.Equal(TaskStatus.RanToCompletion, t.Status);
                Assert.Equal(changeType, t.Result.ChangeType);
                Assert.NotNull(t.Result.Name);
                Assert.Null(t.Result.OldName);
                Assert.False(t.Result.TimedOut);
            }
        }

        [Fact]
        public void Changed_Success()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var fsw = new FileSystemWatcher(testDirectory.Path))
            using (var b = new Barrier(2))
            {
                string name = Path.Combine(testDirectory.Path, Path.GetRandomFileName());
                File.Create(name).Dispose();

                Task<WaitForChangedResult> t = Task.Run(() =>
                {
                    b.SignalAndWait();
                    return fsw.WaitForChanged(WatcherChangeTypes.Changed);
                });

                b.SignalAndWait();
                DateTimeOffset end = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(SuccessTimeoutSeconds);
                while (!t.IsCompleted && DateTimeOffset.UtcNow < end)
                {
                    File.AppendAllText(name, "text");
                    Task.Delay(BetweenOperationsDelayMilliseconds).Wait();
                }

                Assert.True(t.IsCompleted, "WaitForChanged didn't complete");
                Assert.Equal(TaskStatus.RanToCompletion, t.Status);
                Assert.Equal(WatcherChangeTypes.Changed, t.Result.ChangeType);
                Assert.NotNull(t.Result.Name);
                Assert.Null(t.Result.OldName);
                Assert.False(t.Result.TimedOut);
            }
        }

        [Fact]
        public void Renamed_Success()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var fsw = new FileSystemWatcher(testDirectory.Path))
            using (var b = new Barrier(2))
            {
                Task<WaitForChangedResult> t = Task.Run(() =>
                {
                    b.SignalAndWait();
                    return fsw.WaitForChanged(WatcherChangeTypes.Renamed | WatcherChangeTypes.Created); // on some OSes, the renamed might come through as Deleted/Created
                });

                string name = Path.Combine(testDirectory.Path, Path.GetRandomFileName());
                File.Create(name).Dispose();

                b.SignalAndWait();
                DateTimeOffset end = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(SuccessTimeoutSeconds);
                while (!t.IsCompleted && DateTimeOffset.UtcNow < end)
                {
                    string newName = Path.Combine(testDirectory.Path, Path.GetRandomFileName());
                    File.Move(name, newName);
                    name = newName;
                    Task.Delay(BetweenOperationsDelayMilliseconds).Wait();
                }

                Assert.True(t.IsCompleted, "WaitForChanged didn't complete");
                Assert.Equal(TaskStatus.RanToCompletion, t.Status);
                Assert.True(t.Result.ChangeType == WatcherChangeTypes.Created || t.Result.ChangeType == WatcherChangeTypes.Renamed);
                Assert.NotNull(t.Result.Name);
                Assert.False(t.Result.TimedOut);
            }
        }

        private static void AssertTimedOut(WaitForChangedResult result)
        {
            Assert.Equal(0, (int)result.ChangeType);
            Assert.Null(result.Name);
            Assert.Null(result.OldName);
            Assert.True(result.TimedOut);
        }
    }
}
