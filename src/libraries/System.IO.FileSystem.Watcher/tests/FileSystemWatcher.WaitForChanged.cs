// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class WaitForChangedTests : FileSystemWatcherTest
    {
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
        public void CreatedDeleted_Success(WatcherChangeTypes changeType)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var fsw = new FileSystemWatcher(testDirectory.Path))
            {
                Task<WaitForChangedResult> t = Task.Run(() => fsw.WaitForChanged(changeType));
                DateTimeOffset end = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);
                while (!t.IsCompleted && DateTimeOffset.UtcNow < end)
                {
                    string name = Path.Combine(testDirectory.Path, Path.GetRandomFileName());
                    File.Create(name).Dispose();
                    File.Delete(name);
                    Task.Delay(100).Wait();
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
            {
                string name = Path.Combine(testDirectory.Path, Path.GetRandomFileName());
                File.Create(name).Dispose();

                Task<WaitForChangedResult> t = Task.Run(() => fsw.WaitForChanged(WatcherChangeTypes.Changed));
                DateTimeOffset end = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);

                while (!t.IsCompleted && DateTimeOffset.UtcNow < end)
                {
                    File.AppendAllText(name, "text");
                    Task.Delay(100).Wait();
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
            {
                Task<WaitForChangedResult> t = Task.Run(() => fsw.WaitForChanged(WatcherChangeTypes.Renamed));
                DateTimeOffset end = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);

                string name1 = Path.Combine(testDirectory.Path, Path.GetRandomFileName());
                string name2 = Path.Combine(testDirectory.Path, Path.GetRandomFileName());
                File.Create(name1).Dispose();

                while (!t.IsCompleted && DateTimeOffset.UtcNow < end)
                {
                    File.Move(name1, name2);
                    File.Move(name2, name1);
                    Task.Delay(100).Wait();
                }

                Assert.True(t.IsCompleted, "WaitForChanged didn't complete");
                Assert.Equal(TaskStatus.RanToCompletion, t.Status);
                Assert.Equal(WatcherChangeTypes.Renamed, t.Result.ChangeType);
                Assert.True(
                    (t.Result.Name == Path.GetFileName(name1) && t.Result.OldName == Path.GetFileName(name2)) ||
                    (t.Result.Name == Path.GetFileName(name2) && t.Result.OldName == Path.GetFileName(name1)));
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
