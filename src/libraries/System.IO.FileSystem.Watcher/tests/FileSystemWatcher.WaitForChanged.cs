// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class WaitForChangedTests : FileSystemWatcherTest
    {
        private const int BetweenOperationsDelayMilliseconds = 100;

        [Fact]
        public static void WaitForChangedResult_DefaultValues()
        {
            var result = new WaitForChangedResult();
            Assert.Equal((WatcherChangeTypes)0, result.ChangeType);
            Assert.Null(result.Name);
            Assert.Null(result.OldName);
            Assert.False(result.TimedOut);
        }

        [Theory]
        [InlineData(WatcherChangeTypes.All)]
        [InlineData(WatcherChangeTypes.Changed)]
        [InlineData(WatcherChangeTypes.Created)]
        [InlineData(WatcherChangeTypes.Deleted)]
        [InlineData(WatcherChangeTypes.Renamed)]
        [InlineData(WatcherChangeTypes.Changed | WatcherChangeTypes.Created)]
        [InlineData(WatcherChangeTypes.Deleted | WatcherChangeTypes.Renamed)]
        [InlineData((WatcherChangeTypes)0)]
        [InlineData((WatcherChangeTypes)int.MinValue)]
        [InlineData((WatcherChangeTypes)int.MaxValue)]
        public static void WaitForChangedResult_ChangeType_Roundtrip(WatcherChangeTypes changeType)
        {
            var result = new WaitForChangedResult();
            result.ChangeType = changeType;
            Assert.Equal(changeType, result.ChangeType);
        }

        [Theory]
        [InlineData("")]
        [InlineData("myfile.txt")]
        [InlineData("    ")]
        [InlineData("  myfile.txt  ")]
        public static void WaitForChangedResult_Name_Roundtrip(string name)
        {
            var result = new WaitForChangedResult();
            result.Name = name;
            Assert.Equal(name, result.Name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("myfile.txt")]
        [InlineData("    ")]
        [InlineData("  myfile.txt  ")]
        public static void WaitForChangedResult_OldName_Roundtrip(string name)
        {
            var result = new WaitForChangedResult();
            result.OldName = name;
            Assert.Equal(name, result.OldName);
        }

        [Fact]
        public static void WaitForChangedResult_TimedOut_Roundtrip()
        {
            var result = new WaitForChangedResult();
            result.TimedOut = true;
            Assert.True(result.TimedOut);
            result.TimedOut = false;
            Assert.False(result.TimedOut);
            result.TimedOut = true;
            Assert.True(result.TimedOut);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData((long)int.MaxValue + 1)]
        public void TimeSpan_ArgumentValidation(long milliseconds)
        {
            TimeSpan timeout = TimeSpan.FromMilliseconds(milliseconds);
            string _ = CreateTestDirectory(TestDirectory, GetTestFileName());
            using var fsw = new FileSystemWatcher(TestDirectory);

            Assert.Throws<ArgumentOutOfRangeException>("timeout", () => fsw.WaitForChanged(WatcherChangeTypes.All, timeout));
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void ZeroTimeout_TimesOut(bool enabledBeforeWait, bool useTimeSpan)
        {
            using (var fsw = new FileSystemWatcher(TestDirectory))
            {
                if (enabledBeforeWait) fsw.EnableRaisingEvents = true;

                const int timeoutMilliseconds = 0;
                AssertTimedOut(useTimeSpan
                    ? fsw.WaitForChanged(WatcherChangeTypes.All, TimeSpan.FromMilliseconds(timeoutMilliseconds))
                    : fsw.WaitForChanged(WatcherChangeTypes.All, timeoutMilliseconds));
                Assert.Equal(enabledBeforeWait, fsw.EnableRaisingEvents);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void NonZeroTimeout_NoEvents_TimesOut(bool enabledBeforeWait, bool useTimeSpan)
        {
            using (var fsw = new FileSystemWatcher(TestDirectory))
            {
                if (enabledBeforeWait) fsw.EnableRaisingEvents = true;
                const int timeoutMilliseconds = 1;
                AssertTimedOut(useTimeSpan
                    ? fsw.WaitForChanged(0, TimeSpan.FromMilliseconds(timeoutMilliseconds))
                    : fsw.WaitForChanged(0, timeoutMilliseconds));
                Assert.Equal(enabledBeforeWait, fsw.EnableRaisingEvents);
            }
        }

        [Theory]
        [InlineData(WatcherChangeTypes.Deleted, false, true)]
        [InlineData(WatcherChangeTypes.Created, true, false)]
        [InlineData(WatcherChangeTypes.Changed, false, true)]
        [InlineData(WatcherChangeTypes.Renamed, true, false)]
        [InlineData(WatcherChangeTypes.All, true, true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58418", typeof(PlatformDetection), nameof(PlatformDetection.IsMacCatalyst), nameof(PlatformDetection.IsArm64Process))]
        public void NonZeroTimeout_NoActivity_TimesOut(WatcherChangeTypes changeType, bool enabledBeforeWait, bool useTimeSpan)
        {
            using (var fsw = new FileSystemWatcher(TestDirectory))
            {
                if (enabledBeforeWait) fsw.EnableRaisingEvents = true;
                const int timeoutMilliseconds = 1;
                AssertTimedOut(useTimeSpan
                    ? fsw.WaitForChanged(changeType, TimeSpan.FromMilliseconds(timeoutMilliseconds))
                    : fsw.WaitForChanged(changeType, timeoutMilliseconds));
                Assert.Equal(enabledBeforeWait, fsw.EnableRaisingEvents);
            }
        }

        [Theory]
        [OuterLoop("This test has a longer than average timeout and may fail intermittently")]
        [InlineData(WatcherChangeTypes.Created)]
        [InlineData(WatcherChangeTypes.Deleted)]
        public void CreatedDeleted_Success(WatcherChangeTypes changeType)
        {
            using (var fsw = new FileSystemWatcher(TestDirectory))
            {
                for (int i = 1; i <= DefaultAttemptsForExpectedEvent; i++)
                {
                    Task<WaitForChangedResult> t = Task.Run(() => fsw.WaitForChanged(changeType, LongWaitTimeout));
                    while (!t.IsCompleted)
                    {
                        string path = Path.Combine(TestDirectory, Path.GetRandomFileName());
                        File.WriteAllText(path, "text");
                        Task.Delay(BetweenOperationsDelayMilliseconds).Wait();
                        if ((changeType & WatcherChangeTypes.Deleted) != 0)
                        {
                            File.Delete(path);
                        }
                    }

                    try
                    {
                        Assert.Equal(TaskStatus.RanToCompletion, t.Status);
                        Assert.Equal(changeType, t.Result.ChangeType);
                        Assert.NotNull(t.Result.Name);
                        Assert.Null(t.Result.OldName);
                        Assert.False(t.Result.TimedOut);
                    }
                    catch when (i < DefaultAttemptsForExpectedEvent)
                    {
                        continue;
                    }
                    return;
                }
            }
        }

        [Fact]
        [OuterLoop("This test has a longer than average timeout and may fail intermittently")]
        public void Changed_Success()
        {
            using (var fsw = new FileSystemWatcher(TestDirectory))
            {
                for (int i = 1; i <= DefaultAttemptsForExpectedEvent; i++)
                {
                    string name = CreateTestFile(TestDirectory, Path.GetRandomFileName());

                    Task<WaitForChangedResult> t = Task.Run(() => fsw.WaitForChanged(WatcherChangeTypes.Changed, LongWaitTimeout));
                    while (!t.IsCompleted)
                    {
                        File.AppendAllText(name, "text");
                        Task.Delay(BetweenOperationsDelayMilliseconds).Wait();
                    }

                    try
                    {
                        Assert.Equal(TaskStatus.RanToCompletion, t.Status);
                        Assert.Equal(WatcherChangeTypes.Changed, t.Result.ChangeType);
                        Assert.NotNull(t.Result.Name);
                        Assert.Null(t.Result.OldName);
                        Assert.False(t.Result.TimedOut);
                    }
                    catch when (i < DefaultAttemptsForExpectedEvent)
                    {
                        continue;
                    }
                    return;
                }
            }
        }

        [Fact]
        [OuterLoop("This test has a longer than average timeout and may fail intermittently")]
        public void Renamed_Success()
        {
            using (var fsw = new FileSystemWatcher(TestDirectory))
            {
                for (int i = 1; i <= DefaultAttemptsForExpectedEvent; i++)
                {
                    Task<WaitForChangedResult> t = Task.Run(() =>
                        fsw.WaitForChanged(WatcherChangeTypes.Renamed | WatcherChangeTypes.Created, LongWaitTimeout)); // on some OSes, the renamed might come through as Deleted/Created

                    string name = CreateTestFile(TestDirectory, Path.GetRandomFileName());

                    while (!t.IsCompleted)
                    {
                        string newName = Path.Combine(TestDirectory, Path.GetRandomFileName());
                        File.Move(name, newName);
                        name = newName;
                        Task.Delay(BetweenOperationsDelayMilliseconds).Wait();
                    }

                    try
                    {
                        Assert.Equal(TaskStatus.RanToCompletion, t.Status);
                        Assert.True(t.Result.ChangeType == WatcherChangeTypes.Created || t.Result.ChangeType == WatcherChangeTypes.Renamed);
                        Assert.NotNull(t.Result.Name);
                        Assert.False(t.Result.TimedOut);
                    }
                    catch when (i < DefaultAttemptsForExpectedEvent)
                    {
                        continue;
                    }
                    return;
                }
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
