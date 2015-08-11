// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE Directory in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class Directory_GetSetTimes : FileSystemTest
    {
        public delegate void SetTime(string path, DateTime time);
        public delegate DateTime GetTime(string path);

        public IEnumerable<Tuple<SetTime, GetTime, DateTimeKind>> TimeFunctions()
        {
            if (IOInputs.SupportsCreationTime)
            {
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                    ((path, time) => Directory.SetCreationTime(path, time)),
                    ((path) => Directory.GetCreationTime(path)),
                    DateTimeKind.Local);
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                    ((path, time) => Directory.SetCreationTimeUtc(path, time)),
                    ((path) => Directory.GetCreationTimeUtc(path)),
                    DateTimeKind.Utc);
            }
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((path, time) => Directory.SetLastAccessTime(path, time)),
                ((path) => Directory.GetLastAccessTime(path)),
                DateTimeKind.Local);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((path, time) => Directory.SetLastAccessTimeUtc(path, time)),
                ((path) => Directory.GetLastAccessTimeUtc(path)),
                DateTimeKind.Utc);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((path, time) => Directory.SetLastWriteTime(path, time)),
                ((path) => Directory.GetLastWriteTime(path)),
                DateTimeKind.Local);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((path, time) => Directory.SetLastWriteTimeUtc(path, time)),
                ((path) => Directory.GetLastWriteTimeUtc(path)),
                DateTimeKind.Utc);
        }

        [Fact]
        public void NullPath_ThrowsArgumentNullException()
        {
            Assert.All(TimeFunctions(), (tuple) =>
            {
                Assert.Throws<ArgumentNullException>(() => tuple.Item1(null, DateTime.Today));
                Assert.Throws<ArgumentNullException>(() => tuple.Item2(null));
            });
        }

        [Fact]
        public void EmptyPath_ThrowsArgumentException()
        {
            Assert.All(TimeFunctions(), (tuple) =>
            {
                Assert.Throws<ArgumentException>(() => tuple.Item1(string.Empty, DateTime.Today));
                Assert.Throws<ArgumentException>(() => tuple.Item2(string.Empty));
            });
        }

        [Fact]
        public void SettingUpdatesProperties()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            Assert.All(TimeFunctions(), (tuple) =>
            {
                DateTime dt = new DateTime(2014, 12, 1, 12, 0, 0, tuple.Item3);
                tuple.Item1(testDir.FullName, dt);
                var result = tuple.Item2(testDir.FullName);
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

            DirectoryInfo testDirectory = new DirectoryInfo(GetTestFilePath());
            testDirectory.Create();

            long afterTime = DateTime.UtcNow.AddSeconds(3).Ticks;

            Assert.All(TimeFunctions(), (tuple) =>
            {
                Assert.InRange(tuple.Item2(testDirectory.FullName).ToUniversalTime().Ticks, beforeTime, afterTime);
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void Windows_DirectoryDoesntExist_ReturnDefaultValues()
        {
            string path = GetTestFilePath();

            //non-utc
            Assert.Equal(DateTime.FromFileTime(0).Ticks, Directory.GetLastAccessTime(path).Ticks);
            Assert.Equal(DateTime.FromFileTime(0).Ticks, new DirectoryInfo(path).LastAccessTime.Ticks);
            Assert.Equal(DateTime.FromFileTime(0).Ticks, Directory.GetLastWriteTime(path).Ticks);
            Assert.Equal(DateTime.FromFileTime(0).Ticks, new DirectoryInfo(path).LastWriteTime.Ticks);
            if (IOInputs.SupportsCreationTime)
            {
                Assert.Equal(DateTime.FromFileTime(0).Ticks, Directory.GetCreationTime(path).Ticks);
                Assert.Equal(DateTime.FromFileTime(0).Ticks, new DirectoryInfo(path).CreationTime.Ticks);
            }

            //utc
            Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, Directory.GetLastAccessTimeUtc(path).Ticks);
            Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, new DirectoryInfo(path).LastAccessTimeUtc.Ticks);
            Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, Directory.GetLastWriteTimeUtc(path).Ticks);
            Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, new DirectoryInfo(path).LastWriteTimeUtc.Ticks);
            if (IOInputs.SupportsCreationTime)
            {
                Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, Directory.GetCreationTimeUtc(path).Ticks);
                Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, new DirectoryInfo(path).CreationTimeUtc.Ticks);
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Unix_DirectoryDoesntExist_Throws()
        {
            string path = GetTestFilePath();

            //non-utc
            Assert.Throws<FileNotFoundException>(() => Directory.GetLastAccessTime(path));
            Assert.Throws<FileNotFoundException>(() => new DirectoryInfo(path).LastAccessTime);
            Assert.Throws<FileNotFoundException>(() => Directory.GetLastWriteTime(path));
            Assert.Throws<FileNotFoundException>(() => new DirectoryInfo(path).LastWriteTime);
            if (IOInputs.SupportsCreationTime)
            {
                Assert.Throws<FileNotFoundException>(() => Directory.GetCreationTime(path));
                Assert.Throws<FileNotFoundException>(() => new DirectoryInfo(path).CreationTime);
            }

            //utc
            Assert.Throws<FileNotFoundException>(() => Directory.GetLastAccessTimeUtc(path));
            Assert.Throws<FileNotFoundException>(() => new DirectoryInfo(path).LastAccessTimeUtc);
            Assert.Throws<FileNotFoundException>(() => Directory.GetLastWriteTimeUtc(path));
            Assert.Throws<FileNotFoundException>(() => new DirectoryInfo(path).LastWriteTimeUtc);
            if (IOInputs.SupportsCreationTime)
            {
                Assert.Throws<FileNotFoundException>(() => Directory.GetCreationTimeUtc(path));
                Assert.Throws<FileNotFoundException>(() => new DirectoryInfo(path).CreationTimeUtc);
            }
        }
    }
}
