// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class File_GetSetTimes : FileSystemTest
    {
        public delegate void SetTime(string path, DateTime time);
        public delegate DateTime GetTime(string path);

        public IEnumerable<Tuple<SetTime, GetTime, DateTimeKind>> TimeFunctions()
        {
            if (Interop.IsWindows | Interop.IsOSX)
            {
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                    ((path, time) => File.SetCreationTime(path, time)),
                    ((path) => File.GetCreationTime(path)),
                    DateTimeKind.Local);
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                    ((path, time) => File.SetCreationTimeUtc(path, time)),
                    ((path) => File.GetCreationTimeUtc(path)),
                    DateTimeKind.Utc);
            }
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((path, time) => File.SetLastAccessTime(path, time)),
                ((path) => File.GetLastAccessTime(path)),
                DateTimeKind.Local);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((path, time) => File.SetLastAccessTimeUtc(path, time)),
                ((path) => File.GetLastAccessTimeUtc(path)),
                DateTimeKind.Utc);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((path, time) => File.SetLastWriteTime(path, time)),
                ((path) => File.GetLastWriteTime(path)),
                DateTimeKind.Local);
            yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                ((path, time) => File.SetLastWriteTimeUtc(path, time)),
                ((path) => File.GetLastWriteTimeUtc(path)),
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
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();

            Assert.All(TimeFunctions(), (tuple) =>
            {
                DateTime dt = new DateTime(2014, 12, 1, 12, 0, 0, tuple.Item3);
                tuple.Item1(testFile.FullName, dt);
                Assert.Equal(dt, tuple.Item2(testFile.FullName));
            });
        }

        [Fact]
        public void CreationSetsAllTimes()
        {
            string path = GetTestFilePath();
            long beforeTime = DateTime.Now.AddSeconds(-3).Ticks;
            long utcBeforeTime = DateTime.UtcNow.AddSeconds(-3).Ticks;

            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();

            long afterTime = DateTime.Now.AddSeconds(3).Ticks;
            long utcAfterTime = DateTime.UtcNow.AddSeconds(3).Ticks;

            Assert.All(TimeFunctions(), (tuple) =>
            {
                Assert.InRange(tuple.Item2(testFile.FullName).Ticks, tuple.Item3 == DateTimeKind.Local ? beforeTime : utcBeforeTime, tuple.Item3 == DateTimeKind.Local ? afterTime : utcAfterTime);
            });
        }

        [OuterLoop]
        [Fact]
        [ActiveIssue(1728, PlatformID.AnyUnix)]
        public void FileDoesntExist_ReturnDefaultValues()
        {
            string path = GetTestFilePath();

            //non-utc
            Assert.Equal(DateTime.FromFileTime(0).Ticks, File.GetLastAccessTime(path).Ticks);
            Assert.Equal(DateTime.FromFileTime(0).Ticks, new FileInfo(path).LastAccessTime.Ticks);
            Assert.Equal(DateTime.FromFileTime(0).Ticks, File.GetLastWriteTime(path).Ticks);
            Assert.Equal(DateTime.FromFileTime(0).Ticks, new FileInfo(path).LastWriteTime.Ticks);
            if (Interop.IsWindows | Interop.IsOSX)
            {
                Assert.Equal(DateTime.FromFileTime(0).Ticks, File.GetCreationTime(path).Ticks);
                Assert.Equal(DateTime.FromFileTime(0).Ticks, new FileInfo(path).CreationTime.Ticks);
            }

            //utc
            Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, File.GetLastAccessTimeUtc(path).Ticks);
            Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, new FileInfo(path).LastAccessTimeUtc.Ticks);
            Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, File.GetLastWriteTimeUtc(path).Ticks);
            Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, new FileInfo(path).LastWriteTimeUtc.Ticks);
            if (Interop.IsWindows | Interop.IsOSX)
            {
                Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, File.GetCreationTimeUtc(path).Ticks);
                Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, new FileInfo(path).CreationTimeUtc.Ticks);
            }
        }
    }
}
