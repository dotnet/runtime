// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class File_GetSetTimes : StaticGetSetTimes
    {
        // OSX has the limitation of setting upto 2262-04-11T23:47:16 (long.Max) date.
        // 32bit Unix has time_t up to ~ 2038.
        private static bool SupportsLongMaxDateTime => PlatformDetection.IsWindows || (RuntimeInformation.ProcessArchitecture != Architecture.Arm && RuntimeInformation.ProcessArchitecture != Architecture.X86 && !PlatformDetection.IsOSX);

        public override string GetExistingItem()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            return path;
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
            File.SetLastWriteTime(path, DateTime.Now.AddMinutes(1));
            File.SetLastAccessTime(path, DateTime.Now.AddMinutes(2));

            // Assert.InRange is inclusive.
            Assert.InRange(File.GetCreationTimeUtc(path), DateTime.MinValue, File.GetLastWriteTimeUtc(path));
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
            DateTime newCreationTimeUTC = System.DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds(300));
            File.SetCreationTimeUtc(path, newCreationTimeUTC);

            Assert.Equal(newCreationTimeUTC, File.GetLastWriteTimeUtc(path));

            Assert.Equal(newCreationTimeUTC, File.GetCreationTimeUtc(path));
        }

        public override IEnumerable<TimeFunction> TimeFunctions(bool requiresRoundtripping = false)
        {
            if (IOInputs.SupportsGettingCreationTime && (!requiresRoundtripping || IOInputs.SupportsSettingCreationTime))
            {
                yield return TimeFunction.Create(
                    ((path, time) => File.SetCreationTime(path, time)),
                    ((path) => File.GetCreationTime(path)),
                    DateTimeKind.Local);
                yield return TimeFunction.Create(
                    ((path, time) => File.SetCreationTimeUtc(path, time)),
                    ((path) => File.GetCreationTimeUtc(path)),
                    DateTimeKind.Unspecified);
                yield return TimeFunction.Create(
                    ((path, time) => File.SetCreationTimeUtc(path, time)),
                    ((path) => File.GetCreationTimeUtc(path)),
                    DateTimeKind.Utc);
            }
            yield return TimeFunction.Create(
                ((path, time) => File.SetLastAccessTime(path, time)),
                ((path) => File.GetLastAccessTime(path)),
                DateTimeKind.Local);
            yield return TimeFunction.Create(
                ((path, time) => File.SetLastAccessTimeUtc(path, time)),
                ((path) => File.GetLastAccessTimeUtc(path)),
                DateTimeKind.Unspecified);
            yield return TimeFunction.Create(
                ((path, time) => File.SetLastAccessTimeUtc(path, time)),
                ((path) => File.GetLastAccessTimeUtc(path)),
                DateTimeKind.Utc);
            yield return TimeFunction.Create(
                ((path, time) => File.SetLastWriteTime(path, time)),
                ((path) => File.GetLastWriteTime(path)),
                DateTimeKind.Local);
            yield return TimeFunction.Create(
                ((path, time) => File.SetLastWriteTimeUtc(path, time)),
                ((path) => File.GetLastWriteTimeUtc(path)),
                DateTimeKind.Unspecified);
            yield return TimeFunction.Create(
                ((path, time) => File.SetLastWriteTimeUtc(path, time)),
                ((path) => File.GetLastWriteTimeUtc(path)),
                DateTimeKind.Utc);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInAppContainer))] // Can't read root in appcontainer
        [PlatformSpecific(TestPlatforms.Windows)]
        public void PageFileHasTimes()
        {
            // Typically there is a page file on the C: drive, if not, don't bother trying to track it down.
            string pageFilePath = Directory.EnumerateFiles(@"C:\", "pagefile.sys").FirstOrDefault();
            if (pageFilePath != null)
            {
                Assert.All(TimeFunctions(), (item) =>
                {
                    var time = item.Getter(pageFilePath);
                    Assert.NotEqual(DateTime.FromFileTime(0), time);
                });
            }
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

        [ConditionalFact(nameof(isNotHFS))] // OSX HFS driver format does not support nanosecond granularity.
        public void SetUptoNanoseconds()
        {
            string file = GetTestFilePath();
            File.WriteAllText(file, "");

            DateTime dateTime = DateTime.UtcNow;
            File.SetLastWriteTimeUtc(file, dateTime);
            long ticks = File.GetLastWriteTimeUtc(file).Ticks;

            Assert.Equal(dateTime, File.GetLastWriteTimeUtc(file));
            Assert.Equal(ticks, dateTime.Ticks);
        }

        [ConditionalFact(nameof(SupportsLongMaxDateTime))]
        public void SetDateTimeMax()
        {
            string file = GetTestFilePath();
            File.WriteAllText(file, "");

            DateTime dateTime = new DateTime(9999, 4, 11, 23, 47, 17, 21, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(file, dateTime);
            long ticks = File.GetLastWriteTimeUtc(file).Ticks;

            Assert.Equal(dateTime, File.GetLastWriteTimeUtc(file));
            Assert.Equal(ticks, dateTime.Ticks);
        }

        [Fact]
        public void SetLastAccessTimeTicks()
        {
            string firstFile = GetTestFilePath();
            string secondFile = GetTestFilePath();

            File.WriteAllText(firstFile, "");
            File.WriteAllText(secondFile, "");

            File.SetLastWriteTimeUtc(secondFile, DateTime.UtcNow);
            long firstFileTicks = File.GetLastAccessTimeUtc(firstFile).Ticks;
            long secondFileTicks = File.GetLastAccessTimeUtc(secondFile).Ticks;
            Assert.True(firstFileTicks <= secondFileTicks, $"First File Ticks\t{firstFileTicks}\nSecond File Ticks\t{secondFileTicks}");
        }
    }
}
