// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_GetSetTimes : InfoGetSetTimes<FileInfo>
    {
        protected override FileInfo GetExistingItem()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            return new FileInfo(path);
        }

        private static bool HasNonZeroNanoseconds(DateTime dt) => dt.Ticks % 10 != 0;

        public FileInfo GetNonZeroMilliseconds()
        {
            FileInfo fileinfo = new FileInfo(GetTestFilePath());
            fileinfo.Create().Dispose();

            if (fileinfo.LastWriteTime.Millisecond == 0)
            {
                DateTime dt = fileinfo.LastWriteTime;
                dt = dt.AddMilliseconds(1);
                fileinfo.LastWriteTime = dt;
            }

            Assert.NotEqual(0, fileinfo.LastWriteTime.Millisecond);
            return fileinfo;
        }

        public FileInfo GetNonZeroNanoseconds()
        {
            FileInfo fileinfo = new FileInfo(GetTestFilePath());
            fileinfo.Create().Dispose();

            if (!HasNonZeroNanoseconds(fileinfo.LastWriteTime))
            {
                if (PlatformDetection.IsOSXLike)
                    return null;

                DateTime dt = fileinfo.LastWriteTime;
                dt = dt.AddTicks(1);
                fileinfo.LastWriteTime = dt;
            }

            Assert.True(HasNonZeroNanoseconds(fileinfo.LastWriteTime));
            return fileinfo;
        }

        protected override FileInfo GetMissingItem() => new FileInfo(GetTestFilePath());

        protected override string GetItemPath(FileInfo item) => item.FullName;

        protected override void InvokeCreate(FileInfo item) => item.Create();

        public override IEnumerable<TimeFunction> TimeFunctions(bool requiresRoundtripping = false)
        {
            if (IOInputs.SupportsGettingCreationTime && (!requiresRoundtripping || IOInputs.SupportsSettingCreationTime))
            {
                yield return TimeFunction.Create(
                    ((testFile, time) => { testFile.CreationTime = time; }),
                    ((testFile) => testFile.CreationTime),
                    DateTimeKind.Local);
                yield return TimeFunction.Create(
                    ((testFile, time) => { testFile.CreationTimeUtc = time; }),
                    ((testFile) => testFile.CreationTimeUtc),
                    DateTimeKind.Unspecified);
                yield return TimeFunction.Create(
                    ((testFile, time) => { testFile.CreationTimeUtc = time; }),
                    ((testFile) => testFile.CreationTimeUtc),
                    DateTimeKind.Utc);
            }
            yield return TimeFunction.Create(
                ((testFile, time) => { testFile.LastAccessTime = time; }),
                ((testFile) => testFile.LastAccessTime),
                DateTimeKind.Local);
            yield return TimeFunction.Create(
                ((testFile, time) => { testFile.LastAccessTimeUtc = time; }),
                ((testFile) => testFile.LastAccessTimeUtc),
                DateTimeKind.Unspecified);
            yield return TimeFunction.Create(
                ((testFile, time) => { testFile.LastAccessTimeUtc = time; }),
                ((testFile) => testFile.LastAccessTimeUtc),
                DateTimeKind.Utc);
            yield return TimeFunction.Create(
                ((testFile, time) => { testFile.LastWriteTime = time; }),
                ((testFile) => testFile.LastWriteTime),
                DateTimeKind.Local);
            yield return TimeFunction.Create(
                ((testFile, time) => { testFile.LastWriteTimeUtc = time; }),
                ((testFile) => testFile.LastWriteTimeUtc),
                DateTimeKind.Unspecified);
            yield return TimeFunction.Create(
                ((testFile, time) => { testFile.LastWriteTimeUtc = time; }),
                ((testFile) => testFile.LastWriteTimeUtc),
                DateTimeKind.Utc);
        }

        [ConditionalFact(nameof(isNotHFS))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/40530", TestPlatforms.Browser)]
        public void CopyToMillisecondPresent()
        {
            FileInfo input = GetNonZeroMilliseconds();
            FileInfo output = new FileInfo(Path.Combine(GetTestFilePath(), input.Name));

            Assert.Equal(0, output.LastWriteTime.Millisecond);
            output.Directory.Create();
            output = input.CopyTo(output.FullName, true);

            Assert.Equal(input.LastWriteTime.Millisecond, output.LastWriteTime.Millisecond);
            Assert.NotEqual(0, output.LastWriteTime.Millisecond);
        }

        [ConditionalFact(nameof(isNotHFS))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/40530", TestPlatforms.Browser)]
        public void CopyToNanosecondsPresent()
        {
            FileInfo input = GetNonZeroNanoseconds();
            if (input == null)
                return;

            FileInfo output = new FileInfo(Path.Combine(GetTestFilePath(), input.Name));

            output.Directory.Create();
            output = input.CopyTo(output.FullName, true);

            Assert.Equal(input.LastWriteTime.Ticks, output.LastWriteTime.Ticks);
            Assert.True(HasNonZeroNanoseconds(output.LastWriteTime));
        }

        [ConditionalFact(nameof(isHFS))]
        public void CopyToNanosecondsPresent_HFS()
        {
            FileInfo input = new FileInfo(GetTestFilePath());
            input.Create().Dispose();
            FileInfo output = new FileInfo(Path.Combine(GetTestFilePath(), input.Name));

            output.Directory.Create();
            output = input.CopyTo(output.FullName, true);

            Assert.Equal(input.LastWriteTime.Ticks, output.LastWriteTime.Ticks);
            Assert.False(HasNonZeroNanoseconds(output.LastWriteTime));
        }

        [ConditionalFact(nameof(isHFS))]
        public void MoveToMillisecondPresent_HFS()
        {
            FileInfo input = new FileInfo(GetTestFilePath());
            input.Create().Dispose();

            string dest = Path.Combine(input.DirectoryName, GetTestFileName());
            input.MoveTo(dest);
            FileInfo output = new FileInfo(dest);
            Assert.Equal(0, output.LastWriteTime.Millisecond);
        }

        [ConditionalFact(nameof(isNotHFS))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/40530", TestPlatforms.Browser)]
        public void MoveToMillisecondPresent()
        {
            FileInfo input = GetNonZeroMilliseconds();
            string dest = Path.Combine(input.DirectoryName, GetTestFileName());

            input.MoveTo(dest);
            FileInfo output = new FileInfo(dest);
            Assert.NotEqual(0, output.LastWriteTime.Millisecond);
        }

        [ConditionalFact(nameof(isHFS))]
        public void CopyToMillisecondPresent_HFS()
        {
            FileInfo input = new FileInfo(GetTestFilePath());
            input.Create().Dispose();
            FileInfo output = new FileInfo(Path.Combine(GetTestFilePath(), input.Name));
            output.Directory.Create();
            output = input.CopyTo(output.FullName, true);
            Assert.Equal(input.LastWriteTime.Millisecond, output.LastWriteTime.Millisecond);
            Assert.Equal(0, output.LastWriteTime.Millisecond);
        }

        [Fact]
        public void DeleteAfterEnumerate_TimesStillSet()
        {
            // When enumerating we populate the state as we already have it.
            string filePath = GetTestFilePath();
            File.Create(filePath).Dispose();
            FileInfo info = new DirectoryInfo(TestDirectory).EnumerateFiles().First();

            info.Delete();
            Assert.Equal(DateTime.FromFileTimeUtc(0), info.LastAccessTimeUtc);
        }


        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void BirthTimeIsNotNewerThanLowestOfAccessModifiedTimes()
        {
            // On Linux (if no birth time), we synthesize CreationTime from the oldest of
            // status changed time (ctime) and write time (mtime)
            // Sanity check that it is in that range.

            DateTime before = DateTime.UtcNow.AddMinutes(-1);

            FileInfo fi = GetExistingItem(); // should set ctime
            fi.LastWriteTimeUtc = DateTime.UtcNow.AddMinutes(1); // mtime
            fi.LastAccessTimeUtc = DateTime.UtcNow.AddMinutes(2); // atime

            // Assert.InRange is inclusive
            Assert.InRange(fi.CreationTimeUtc, before, fi.LastWriteTimeUtc);
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
                    var time = item.Getter(new FileInfo(pageFilePath));
                    Assert.NotEqual(DateTime.FromFileTime(0), time);
                });
            }
        }
    }
}
