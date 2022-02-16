// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public sealed class File_GetSetTimes_SafeFileHandle : StaticGetSetTimes
    {
        // OSX has the limitation of setting upto 2262-04-11T23:47:16 (long.Max) date.
        // 32bit Unix has time_t up to ~ 2038.
        private static bool SupportsLongMaxDateTime => PlatformDetection.IsWindows ||
                                                       !PlatformDetection.Is32BitProcess &&
                                                       !PlatformDetection.IsOSXLike;

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

        public override IEnumerable<TimeFunction> TimeFunctions(bool requiresRoundtripping = false)
        {
            if (IOInputs.SupportsGettingCreationTime && (!requiresRoundtripping || IOInputs.SupportsSettingCreationTime))
            {
                yield return TimeFunction.Create(
                    SetCreationTimeUsingHandle,
                    GetCreationTimeUsingHandle,
                    DateTimeKind.Local);
                yield return TimeFunction.Create(
                    SetCreationTimeUsingHandleUtc,
                    GetCreationTimeUsingHandleUtc,
                    DateTimeKind.Unspecified);
                yield return TimeFunction.Create(
                    SetCreationTimeUsingHandleUtc,
                    GetCreationTimeUsingHandleUtc,
                    DateTimeKind.Utc);
            }
            yield return TimeFunction.Create(
                SetLastAccessTimeUsingHandle,
                GetLastAccessTimeUsingHandle,
                DateTimeKind.Local);
            yield return TimeFunction.Create(
                SetLastAccessTimeUsingHandleUtc,
                GetLastAccessTimeUsingHandleUtc,
                DateTimeKind.Unspecified);
            yield return TimeFunction.Create(
                SetLastAccessTimeUsingHandleUtc,
                GetLastAccessTimeUsingHandleUtc,
                DateTimeKind.Utc);
            yield return TimeFunction.Create(
                SetLastWriteTimeUsingHandle,
                GetLastWriteTimeUsingHandle,
                DateTimeKind.Local);
            yield return TimeFunction.Create(
                SetLastWriteTimeUsingHandleUtc,
                GetLastWriteTimeUsingHandleUtc,
                DateTimeKind.Unspecified);
            yield return TimeFunction.Create(
                SetLastWriteTimeUsingHandleUtc,
                GetLastWriteTimeUsingHandleUtc,
                DateTimeKind.Utc);
        }
        private static SafeFileHandle OpenFileHandle(string path)
        {
            return File.OpenHandle(
                path,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.ReadWrite);
        }

        private static void SetCreationTimeUsingHandle(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path);
            File.SetCreationTime(fileHandle, creationTime);
        }

        private static DateTime GetCreationTimeUsingHandle(string path)
        {
            using var fileHandle = OpenFileHandle(path);
            return File.GetCreationTime(fileHandle);
        }

        private static void SetCreationTimeUsingHandleUtc(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path);
            File.SetCreationTimeUtc(fileHandle, creationTime);
        }

        private static DateTime GetCreationTimeUsingHandleUtc(string path)
        {
            using var fileHandle = OpenFileHandle(path);
            return File.GetCreationTimeUtc(fileHandle);
        }

        private static void SetLastAccessTimeUsingHandle(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path);
            File.SetLastAccessTime(fileHandle, creationTime);
        }

        private static DateTime GetLastAccessTimeUsingHandle(string path)
        {
            using var fileHandle = OpenFileHandle(path);
            return File.GetLastAccessTime(fileHandle);
        }

        private static void SetLastAccessTimeUsingHandleUtc(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path);
            File.SetLastAccessTimeUtc(fileHandle, creationTime);
        }

        private static DateTime GetLastAccessTimeUsingHandleUtc(string path)
        {
            using var fileHandle = OpenFileHandle(path);
            return File.GetLastAccessTimeUtc(fileHandle);
        }

        private static void SetLastWriteTimeUsingHandle(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path);
            File.SetLastWriteTime(fileHandle, creationTime);
        }

        private static DateTime GetLastWriteTimeUsingHandle(string path)
        {
            using var fileHandle = OpenFileHandle(path);
            return File.GetLastWriteTime(fileHandle);
        }

        private static void SetLastWriteTimeUsingHandleUtc(string path, DateTime creationTime)
        {
            using var fileHandle = OpenFileHandle(path);
            File.SetLastWriteTimeUtc(fileHandle, creationTime);
        }

        private static DateTime GetLastWriteTimeUsingHandleUtc(string path)
        {
            using var fileHandle = OpenFileHandle(path);
            return File.GetLastWriteTimeUtc(fileHandle);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void BirthTimeIsNotNewerThanLowestOfAccessModifiedTimes()
        {
            // On Linux, we synthesize CreationTime from the oldest of status changed time and write time
            //  if birth time is not available. So WriteTime should never be earlier.

            // Set different values for all three
            // Status changed time will be when the file was first created, in this case)
            string path = GetExistingItem();
            using var fileHandle = OpenFileHandle(path);

            File.SetLastWriteTime(fileHandle, DateTime.Now.AddMinutes(1));
            File.SetLastAccessTime(fileHandle, DateTime.Now.AddMinutes(2));

            // Assert.InRange is inclusive.
            Assert.InRange(File.GetCreationTimeUtc(fileHandle), DateTime.MinValue, File.GetLastWriteTimeUtc(path));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task CreationTimeSet_GetReturnsExpected_WhenNotInFuture()
        {
            // On Linux, we synthesize CreationTime from the oldest of status changed time (ctime) and write time (mtime).
            // Changing the CreationTime, updates mtime and causes ctime to change to the current time.
            // When setting CreationTime to a value that isn't in the future, getting the CreationTime should return the same value.

            string path = GetTestFilePath();
            await File.WriteAllTextAsync(path, "");

            // Set the creation time to a value in the past that is between ctime and now.
            await Task.Delay(600);
            DateTime newCreationTimeUTC = DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds(300));

            using var fileHandle = OpenFileHandle(path);
            File.SetCreationTimeUtc(fileHandle, newCreationTimeUTC);

            Assert.Equal(newCreationTimeUTC, File.GetLastWriteTimeUtc(fileHandle));
            Assert.Equal(newCreationTimeUTC, File.GetCreationTimeUtc(fileHandle));
        }

        [Fact]
        public void SetLastWriteTimeTicks()
        {
            string firstFile = GetTestFilePath();
            string secondFile = GetTestFilePath();

            File.WriteAllText(firstFile, "");
            File.WriteAllText(secondFile, "");

            File.SetLastAccessTimeUtc(secondFile, DateTime.UtcNow);
            long firstFileTicks = File.GetLastWriteTimeUtc(firstFile).Ticks;
            long secondFileTicks = File.GetLastWriteTimeUtc(secondFile).Ticks;
            Assert.True(firstFileTicks <= secondFileTicks, $"First File Ticks\t{firstFileTicks}\nSecond File Ticks\t{secondFileTicks}");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SetLastWriteTimeTicks_SafeFileHandle()
        {
            string firstFilePath = GetTestFilePath();
            string secondFilePath = GetTestFilePath();

            File.WriteAllText(firstFilePath, "");
            File.WriteAllText(secondFilePath, "");

            using var secondFileHandle = OpenFileHandle(secondFilePath);
            using var firstFileHandle = OpenFileHandle(firstFilePath);

            File.SetLastAccessTimeUtc(secondFileHandle, DateTime.UtcNow);
            long firstFileTicks = File.GetLastWriteTimeUtc(firstFileHandle).Ticks;
            long secondFileTicks = File.GetLastWriteTimeUtc(secondFileHandle).Ticks;
            Assert.True(firstFileTicks <= secondFileTicks, $"First File Ticks\t{firstFileTicks}\nSecond File Ticks\t{secondFileTicks}");
        }

        [ConditionalFact(nameof(HighTemporalResolution))] // OSX HFS driver format/Browser Platform do not support nanosecond granularity.
        public void SetUptoNanoseconds()
        {
            string file = GetTestFilePath();
            File.WriteAllText(file, "");

            DateTime dateTime = DateTime.UtcNow;
            using var fileHandle = OpenFileHandle(file);

            File.SetLastWriteTimeUtc(fileHandle, dateTime);
            long ticks = File.GetLastWriteTimeUtc(fileHandle).Ticks;

            Assert.Equal(dateTime, File.GetLastWriteTimeUtc(fileHandle));
            Assert.Equal(ticks, dateTime.Ticks);
        }


        // Linux kernels no longer have long max date time support. Discussed in https://github.com/dotnet/runtime/issues/43166.
        [PlatformSpecific(~TestPlatforms.Linux)]
        [ConditionalFact(nameof(SupportsLongMaxDateTime))]
        public void SetDateTimeMax()
        {
            string file = GetTestFilePath();
            File.WriteAllText(file, "");

            using var fileHandle = OpenFileHandle(file);
            DateTime dateTime = new(9999, 4, 11, 23, 47, 17, 21, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(fileHandle, dateTime);
            long ticks = File.GetLastWriteTimeUtc(fileHandle).Ticks;

            Assert.Equal(dateTime, File.GetLastWriteTimeUtc(fileHandle));
            Assert.Equal(ticks, dateTime.Ticks);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SetLastAccessTimeTicks()
        {
            string firstFilePath = GetTestFilePath();
            string secondFilePath = GetTestFilePath();

            File.WriteAllText(firstFilePath, "");
            File.WriteAllText(secondFilePath, "");

            using var firstFileHandle = OpenFileHandle(firstFilePath);
            using var secondFileHandle = OpenFileHandle(secondFilePath);

            File.SetLastWriteTimeUtc(secondFileHandle, DateTime.UtcNow);
            long firstFileTicks = File.GetLastAccessTimeUtc(firstFileHandle).Ticks;
            long secondFileTicks = File.GetLastAccessTimeUtc(secondFileHandle).Ticks;
            Assert.True(firstFileTicks <= secondFileTicks, $"First File Ticks\t{firstFileTicks}\nSecond File Ticks\t{secondFileTicks}");
        }
    }
}
