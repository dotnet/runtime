// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class DirectoryInfo_GetSetTimes : FileSystemTest
    {
        public delegate void SetTime(DirectoryInfo testDir, DateTime time);
        public delegate DateTime GetTime(DirectoryInfo testDir);

        public IEnumerable<Tuple<SetTime, GetTime, DateTimeKind>> TimeFunctions()
        {
            if (Interop.IsWindows | Interop.IsOSX)
            {
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                    ((testDir, time) => {testDir.CreationTime = time; }), 
                    ((testDir) => testDir.CreationTime), 
                    DateTimeKind.Local);
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                    ((testDir, time) => {testDir.CreationTimeUtc = time; }),
                    ((testDir) => testDir.CreationTimeUtc),
                    DateTimeKind.Utc);
            }
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((testDir, time) => {testDir.LastAccessTime = time; }),
                ((testDir) => testDir.LastAccessTime),
                DateTimeKind.Local);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((testDir, time) => {testDir.LastAccessTimeUtc = time; }),
                ((testDir) => testDir.LastAccessTimeUtc),
                DateTimeKind.Utc);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((testDir, time) => {testDir.LastWriteTime = time; }),
                ((testDir) => testDir.LastWriteTime),
                DateTimeKind.Local);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((testDir, time) => {testDir.LastWriteTimeUtc = time; }),
                ((testDir) => testDir.LastWriteTimeUtc),
                DateTimeKind.Utc);
        }

        [Fact]
        public void SettingUpdatesProperties()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            Assert.All(TimeFunctions(), (tuple) =>
            {
                DateTime dt = new DateTime(2014, 12, 1, 12, 0, 0, tuple.Item3);
                tuple.Item1(testDir, dt);
                Assert.Equal(dt, tuple.Item2(testDir));
            });
        }

        [Fact]
        public void CreationSetsAllTimes()
        {
            string path = GetTestFilePath();
            long beforeTime = DateTime.Now.AddSeconds(-3).Ticks;
            long utcBeforeTime = DateTime.UtcNow.AddSeconds(-3).Ticks;

            DirectoryInfo testDirectory = new DirectoryInfo(GetTestFilePath());
            testDirectory.Create();

            long afterTime = DateTime.Now.AddSeconds(3).Ticks;
            long utcAfterTime = DateTime.UtcNow.AddSeconds(3).Ticks;

            Assert.All(TimeFunctions(), (tuple) =>
            {
                Assert.InRange(tuple.Item2(testDirectory).Ticks, tuple.Item3 == DateTimeKind.Local ? beforeTime : utcBeforeTime, tuple.Item3 == DateTimeKind.Local ? afterTime : utcAfterTime);
            });
        }
    }
}
