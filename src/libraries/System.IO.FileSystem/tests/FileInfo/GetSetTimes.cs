// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_GetSetTimes : FileSystemTest
    {
        public delegate void SetTime(FileInfo testFile, DateTime time);
        public delegate DateTime GetTime(FileInfo testFile);

        public IEnumerable<Tuple<SetTime, GetTime, DateTimeKind>> TimeFunctions()
        {
            if (IOInputs.SupportsCreationTime)
            {
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                    ((testFile, time) => { testFile.CreationTime = time; }),
                    ((testFile) => testFile.CreationTime),
                    DateTimeKind.Local);
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                    ((testFile, time) => { testFile.CreationTimeUtc = time; }),
                    ((testFile) => testFile.CreationTimeUtc),
                    DateTimeKind.Utc);
            }
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((testFile, time) => { testFile.LastAccessTime = time; }),
                ((testFile) => testFile.LastAccessTime),
                DateTimeKind.Local);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((testFile, time) => { testFile.LastAccessTimeUtc = time; }),
                ((testFile) => testFile.LastAccessTimeUtc),
                DateTimeKind.Utc);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((testFile, time) => { testFile.LastWriteTime = time; }),
                ((testFile) => testFile.LastWriteTime),
                DateTimeKind.Local);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((testFile, time) => { testFile.LastWriteTimeUtc = time; }),
                ((testFile) => testFile.LastWriteTimeUtc),
                DateTimeKind.Utc);
        }

        [Fact]
        public void SettingUpdatesProperties()
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();

            Assert.All(TimeFunctions(), (tuple) =>
            {
                DateTime dt = new DateTime(2014, 12, 1, 12, 0, 0, tuple.Item3);
                tuple.Item1(testFile, dt);
                var result = tuple.Item2(testFile);
                Assert.Equal(dt, result);
                Assert.Equal(dt.ToLocalTime(), result.ToLocalTime());
                Assert.Equal(dt.ToUniversalTime(), result.ToUniversalTime());
            });
        }

        [Fact]
        public void CreationSetsAllTimes()
        {
            string path = GetTestFilePath();
            long beforeTime = DateTime.UtcNow.AddSeconds(-3).Ticks;

            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();

            long afterTime = DateTime.UtcNow.AddSeconds(3).Ticks;

            Assert.All(TimeFunctions(), (tuple) =>
            {
                Assert.InRange(tuple.Item2(testFile).ToUniversalTime().Ticks, beforeTime, afterTime);
            });
        }
    }
}
