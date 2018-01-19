// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Tests
{
    public class File_GetSetTimes : StaticGetSetTimes
    {
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
            // On Linux, we synthesize CreationTime from the oldest of statuc changed time and write time
            //  if birth time is not available. So WriteTime should never be earlier.

            // Set different values for all three
            // Status changed time will be when the file was first created, in this case)
            string path = GetExistingItem();
            File.SetLastWriteTime(path, DateTime.Now.AddMinutes(1));
            File.SetLastAccessTime(path, DateTime.Now.AddMinutes(2));

            // Assert.InRange is inclusive.
            Assert.InRange(File.GetCreationTimeUtc(path), DateTime.MinValue, File.GetLastWriteTimeUtc(path));
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
    }
}
