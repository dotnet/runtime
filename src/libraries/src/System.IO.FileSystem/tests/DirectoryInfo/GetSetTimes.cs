// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_GetSetTimes : FileSystemTest
    {
        public delegate void SetTime(DirectoryInfo testDir, DateTime time);
        public delegate DateTime GetTime(DirectoryInfo testDir);

        public IEnumerable<Tuple<SetTime, GetTime, DateTimeKind>> TimeFunctions(bool requiresRoundtripping = false)
        {
            if (IOInputs.SupportsGettingCreationTime && (!requiresRoundtripping || IOInputs.SupportsSettingCreationTime))
            {
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                    ((testDir, time) => {testDir.CreationTime = time; }), 
                    ((testDir) => testDir.CreationTime), 
                    DateTimeKind.Local);
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                    ((testDir, time) => {testDir.CreationTimeUtc = time; }),
                    ((testDir) => testDir.CreationTimeUtc),
                    DateTimeKind.Unspecified); 
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                     ((testDir, time) => { testDir.CreationTimeUtc = time; }),
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
                DateTimeKind.Unspecified);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((testDir, time) => { testDir.LastAccessTimeUtc = time; }),
                ((testDir) => testDir.LastAccessTimeUtc),
                DateTimeKind.Utc);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((testDir, time) => {testDir.LastWriteTime = time; }),
                ((testDir) => testDir.LastWriteTime),
                DateTimeKind.Local);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((testDir, time) => {testDir.LastWriteTimeUtc = time; }),
                ((testDir) => testDir.LastWriteTimeUtc),
                DateTimeKind.Unspecified);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((testDir, time) => { testDir.LastWriteTimeUtc = time; }),
                ((testDir) => testDir.LastWriteTimeUtc),
                DateTimeKind.Utc);
        }

        [Fact]
        public void SettingUpdatesProperties()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            Assert.All(TimeFunctions(requiresRoundtripping: true), (tuple) =>
            {
                DateTime dt = new DateTime(2014, 12, 1, 12, 0, 0, tuple.Item3);
                tuple.Item1(testDir, dt);
                var result = tuple.Item2(testDir);
                Assert.Equal(dt, result);
                Assert.Equal(dt.ToLocalTime(), result.ToLocalTime());

                // File and Directory UTC APIs treat a DateTimeKind.Unspecified as UTC whereas
                // ToUniversalTime treats it as local.
                if (tuple.Item3 == DateTimeKind.Unspecified)
                {
                    Assert.Equal(dt, result.ToUniversalTime());
                }
                else
                {
                    Assert.Equal(dt.ToUniversalTime(), result.ToUniversalTime());
                }
            });
        }

        [Fact]
        public void CreationSetsAllTimes()
        {
            string path = GetTestFilePath();
            long beforeTime = DateTime.UtcNow.AddSeconds(-3).Ticks;

            DirectoryInfo testDirectory = new DirectoryInfo(GetTestFilePath());
            testDirectory.Create();

            long afterTime = DateTime.UtcNow.AddSeconds(3).Ticks;

            Assert.All(TimeFunctions(), (tuple) =>
            {
                Assert.InRange(tuple.Item2(testDirectory).ToUniversalTime().Ticks, beforeTime, afterTime);
            });
        }
    }
}
