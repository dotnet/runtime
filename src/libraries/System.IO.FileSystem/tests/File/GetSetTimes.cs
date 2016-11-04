// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class File_GetSetTimes : FileSystemTest
    {
        public delegate void SetTime(string path, DateTime time);
        public delegate DateTime GetTime(string path);

        public IEnumerable<Tuple<SetTime, GetTime, DateTimeKind>> TimeFunctions(bool requiresRoundtripping = false)
        {
            if (IOInputs.SupportsGettingCreationTime && (!requiresRoundtripping || IOInputs.SupportsSettingCreationTime))
            {
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                    ((path, time) => File.SetCreationTime(path, time)),
                    ((path) => File.GetCreationTime(path)),
                    DateTimeKind.Local);
                yield return Tuple.Create<SetTime, GetTime, DateTimeKind>(
                    ((path, time) => File.SetCreationTimeUtc(path, time)),
                    ((path) => File.GetCreationTimeUtc(path)),
                    DateTimeKind.Unspecified);
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
                DateTimeKind.Unspecified);
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
                DateTimeKind.Unspecified);
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

            Assert.All(TimeFunctions(requiresRoundtripping: true), (tuple) =>
            {
                DateTime dt = new DateTime(2014, 12, 1, 12, 0, 0, tuple.Item3);
                tuple.Item1(testFile.FullName, dt);
                var result = tuple.Item2(testFile.FullName);
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

            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();

            long afterTime = DateTime.UtcNow.AddSeconds(3).Ticks;

            Assert.All(TimeFunctions(), (tuple) =>
            {
                Assert.InRange(tuple.Item2(testFile.FullName).ToUniversalTime().Ticks, beforeTime, afterTime);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Windows_FileDoesntExist_ReturnDefaultValues()
        {
            string path = GetTestFilePath();

            //non-utc
            Assert.Equal(DateTime.FromFileTime(0).Ticks, File.GetLastAccessTime(path).Ticks);
            Assert.Equal(DateTime.FromFileTime(0).Ticks, new FileInfo(path).LastAccessTime.Ticks);
            Assert.Equal(DateTime.FromFileTime(0).Ticks, File.GetLastWriteTime(path).Ticks);
            Assert.Equal(DateTime.FromFileTime(0).Ticks, new FileInfo(path).LastWriteTime.Ticks);
            if (IOInputs.SupportsGettingCreationTime)
            {
                Assert.Equal(DateTime.FromFileTime(0).Ticks, File.GetCreationTime(path).Ticks);
                Assert.Equal(DateTime.FromFileTime(0).Ticks, new FileInfo(path).CreationTime.Ticks);
            }

            //utc
            Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, File.GetLastAccessTimeUtc(path).Ticks);
            Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, new FileInfo(path).LastAccessTimeUtc.Ticks);
            Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, File.GetLastWriteTimeUtc(path).Ticks);
            Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, new FileInfo(path).LastWriteTimeUtc.Ticks);
            if (IOInputs.SupportsGettingCreationTime)
            {
                Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, File.GetCreationTimeUtc(path).Ticks);
                Assert.Equal(DateTime.FromFileTimeUtc(0).Ticks, new FileInfo(path).CreationTimeUtc.Ticks);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Unix_FileDoesntExist_Throws_FileNotFoundException()
        {
            string path = GetTestFilePath();

            //non-utc
            Assert.Throws<FileNotFoundException>(() => File.GetLastAccessTime(path));
            Assert.Throws<FileNotFoundException>(() => new FileInfo(path).LastAccessTime);
            Assert.Throws<FileNotFoundException>(() => File.GetLastWriteTime(path));
            Assert.Throws<FileNotFoundException>(() => new FileInfo(path).LastWriteTime);
            if (IOInputs.SupportsGettingCreationTime)
            {
                Assert.Throws<FileNotFoundException>(() => File.GetCreationTime(path));
                Assert.Throws<FileNotFoundException>(() => new FileInfo(path).CreationTime);
            }

            //utc
            Assert.Throws<FileNotFoundException>(() => File.GetLastAccessTimeUtc(path));
            Assert.Throws<FileNotFoundException>(() => new FileInfo(path).LastAccessTimeUtc);
            Assert.Throws<FileNotFoundException>(() => File.GetLastWriteTimeUtc(path));
            Assert.Throws<FileNotFoundException>(() => new FileInfo(path).LastWriteTimeUtc);
            if (IOInputs.SupportsGettingCreationTime)
            {
                Assert.Throws<FileNotFoundException>(() => File.GetCreationTimeUtc(path));
                Assert.Throws<FileNotFoundException>(() => new FileInfo(path).CreationTimeUtc);
            }
        }
    }
}
