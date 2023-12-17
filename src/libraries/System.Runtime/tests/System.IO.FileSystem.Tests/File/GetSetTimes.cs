// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public abstract class File_GetSetTimes : StaticGetSetTimes
    {
        // OSX has the limitation of setting upto 2262-04-11T23:47:16 (long.Max) date.
        // 32bit Unix has time_t up to ~ 2038.
        protected static bool SupportsLongMaxDateTime => PlatformDetection.IsWindows || (!PlatformDetection.Is32BitProcess && !PlatformDetection.IsApplePlatform);

        protected override bool CanBeReadOnly => true;

        protected override string GetExistingItem(bool readOnly = false)
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();

            if (readOnly)
            {
                File.SetAttributes(path, FileAttributes.ReadOnly);
            }

            return path;
        }

        protected override string CreateSymlink(string path, string pathToTarget) => File.CreateSymbolicLink(path, pathToTarget).FullName;

        protected abstract void SetCreationTime(string path, DateTime creationTime);

        protected abstract DateTime GetCreationTime(string path);

        protected abstract void SetCreationTimeUtc(string path, DateTime creationTimeUtc);

        protected abstract DateTime GetCreationTimeUtc(string path);

        protected abstract void SetLastAccessTime(string path, DateTime lastAccessTime);

        protected abstract DateTime GetLastAccessTime(string path);

        protected abstract void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc);

        protected abstract DateTime GetLastAccessTimeUtc(string path);

        protected abstract void SetLastWriteTime(string path, DateTime lastWriteTime);

        protected abstract DateTime GetLastWriteTime(string path);

        protected abstract void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc);

        protected abstract DateTime GetLastWriteTimeUtc(string path);


        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void BirthTimeIsNotNewerThanLowestOfAccessModifiedTimes()
        {
            // On Linux, we synthesize CreationTime from the oldest of status changed time and write time
            //  if birth time is not available. So WriteTime should never be earlier.

            // Set different values for all three
            // Status changed time will be when the file was first created, in this case)
            string path = GetExistingItem();

            SetLastWriteTime(path, DateTime.Now.AddMinutes(1));
            SetLastAccessTime(path, DateTime.Now.AddMinutes(2));

            // Assert.InRange is inclusive.
            Assert.InRange(GetCreationTimeUtc(path), DateTime.MinValue, GetLastWriteTimeUtc(path));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task CreationTimeSet_GetReturnsExpected_WhenNotInFuture()
        {
            // On Linux, we synthesize CreationTime from the oldest of status changed time (ctime) and write time (mtime).
            // Changing the CreationTime, updates mtime and causes ctime to change to the current time.
            // When setting CreationTime to a value that isn't in the future, getting the CreationTime should return the same value.

            string path = GetTestFilePath();
            File.WriteAllText(path, "");

            // Set the creation time to a value in the past that is between ctime and now.
            await Task.Delay(600);
            DateTime newCreationTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds(300));

            SetCreationTimeUtc(path, newCreationTimeUtc);

            Assert.Equal(newCreationTimeUtc, GetLastWriteTimeUtc(path));
            Assert.Equal(newCreationTimeUtc, GetCreationTimeUtc(path));
        }

        public override IEnumerable<TimeFunction> TimeFunctions(bool requiresRoundtripping = false)
        {
            if (IOInputs.SupportsGettingCreationTime && (!requiresRoundtripping || IOInputs.SupportsSettingCreationTime))
            {
                yield return TimeFunction.Create(
                    SetCreationTime,
                    GetCreationTime,
                    DateTimeKind.Local);
                yield return TimeFunction.Create(
                    SetCreationTimeUtc,
                    GetCreationTimeUtc,
                    DateTimeKind.Unspecified);
                yield return TimeFunction.Create(
                    SetCreationTimeUtc,
                    GetCreationTimeUtc,
                    DateTimeKind.Utc);
            }
            yield return TimeFunction.Create(
                SetLastAccessTime,
                GetLastAccessTime,
                DateTimeKind.Local);
            yield return TimeFunction.Create(
                SetLastAccessTimeUtc,
                GetLastAccessTimeUtc,
                DateTimeKind.Unspecified);
            yield return TimeFunction.Create(
                SetLastAccessTimeUtc,
                GetLastAccessTimeUtc,
                DateTimeKind.Utc);
            yield return TimeFunction.Create(
                SetLastWriteTime,
                GetLastWriteTime,
                DateTimeKind.Local);
            yield return TimeFunction.Create(
                SetLastWriteTimeUtc,
                GetLastWriteTimeUtc,
                DateTimeKind.Unspecified);
            yield return TimeFunction.Create(
                SetLastWriteTimeUtc,
                GetLastWriteTimeUtc,
                DateTimeKind.Utc);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/83197", TestPlatforms.Browser)]
        public void SetLastWriteTimeTicks()
        {
            string firstFile = GetTestFilePath();
            string secondFile = GetTestFilePath();

            File.WriteAllText(firstFile, "");
            File.WriteAllText(secondFile, "");

            SetLastAccessTimeUtc(firstFile, DateTime.UtcNow);
            long firstFileTicks = GetLastWriteTimeUtc(firstFile).Ticks;
            long secondFileTicks = GetLastWriteTimeUtc(secondFile).Ticks;
            Assert.True(firstFileTicks <= secondFileTicks, $"First File Ticks\t{firstFileTicks}\nSecond File Ticks\t{secondFileTicks}");
        }

        [ConditionalFact(nameof(HighTemporalResolution))] // OSX HFS driver format/Browser Platform do not support nanosecond granularity.
        public void SetUptoNanoseconds()
        {
            string file = GetTestFilePath();
            File.WriteAllText(file, "");

            DateTime dateTime = DateTime.UtcNow;

            SetLastWriteTimeUtc(file, dateTime);
            long ticks = GetLastWriteTimeUtc(file).Ticks;

            Assert.Equal(dateTime, GetLastWriteTimeUtc(file));
            Assert.Equal(ticks, dateTime.Ticks);
        }

        // Linux kernels no longer have long max date time support. Discussed in https://github.com/dotnet/runtime/issues/43166.
        [PlatformSpecific(~TestPlatforms.Linux)]
        [ConditionalFact(nameof(SupportsLongMaxDateTime))]
        public void SetDateTimeMax()
        {
            string file = GetTestFilePath();
            File.WriteAllText(file, "");

            DateTime dateTime = new(9999, 4, 11, 23, 47, 17, 21, DateTimeKind.Utc);
            SetLastWriteTimeUtc(file, dateTime);
            long ticks = GetLastWriteTimeUtc(file).Ticks;

            Assert.Equal(dateTime, GetLastWriteTimeUtc(file));
            Assert.Equal(ticks, dateTime.Ticks);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/83197", TestPlatforms.Browser)]
        public void SetLastAccessTimeTicks()
        {
            string firstFile = GetTestFilePath();
            string secondFile = GetTestFilePath();

            File.WriteAllText(firstFile, "");
            File.WriteAllText(secondFile, "");

            SetLastWriteTimeUtc(firstFile, DateTime.UtcNow);
            long firstFileTicks = GetLastAccessTimeUtc(firstFile).Ticks;
            long secondFileTicks = GetLastAccessTimeUtc(secondFile).Ticks;
            Assert.True(firstFileTicks <= secondFileTicks, $"First File Ticks\t{firstFileTicks}\nSecond File Ticks\t{secondFileTicks}");
        }
    }
}
