// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.IO.Tests
{
    public class Directory_GetSetTimes : StaticGetSetTimes
    {
        protected override string GetExistingItem() => Directory.CreateDirectory(GetTestFilePath()).FullName;

        public override IEnumerable<TimeFunction> TimeFunctions(bool requiresRoundtripping = false)
        {
            if (IOInputs.SupportsGettingCreationTime && (!requiresRoundtripping || IOInputs.SupportsSettingCreationTime))
            {
                yield return TimeFunction.Create(
                    ((path, time) => Directory.SetCreationTime(path, time)),
                    ((path) => Directory.GetCreationTime(path)),
                    DateTimeKind.Local,
                    "CreationTime_Local");
                yield return TimeFunction.Create(
                    ((path, time) => Directory.SetCreationTimeUtc(path, time)),
                    ((path) => Directory.GetCreationTimeUtc(path)),
                    DateTimeKind.Unspecified,
                    "CreationTime_Unspecified");
                yield return TimeFunction.Create(
                    ((path, time) => Directory.SetCreationTimeUtc(path, time)),
                    ((path) => Directory.GetCreationTimeUtc(path)),
                    DateTimeKind.Utc,
                    "CreationTime_Utc");
            }
            yield return TimeFunction.Create(
                ((path, time) => Directory.SetLastAccessTime(path, time)),
                ((path) => Directory.GetLastAccessTime(path)),
                DateTimeKind.Local,
                "LastAccessTime_Local");
            yield return TimeFunction.Create(
                ((path, time) => Directory.SetLastAccessTimeUtc(path, time)),
                ((path) => Directory.GetLastAccessTimeUtc(path)),
                DateTimeKind.Unspecified,
                "LastAccessTime_Unspecified");
            yield return TimeFunction.Create(
                ((path, time) => Directory.SetLastAccessTimeUtc(path, time)),
                ((path) => Directory.GetLastAccessTimeUtc(path)),
                DateTimeKind.Utc,
                "LastAccessTime_Utc");
            yield return TimeFunction.Create(
                ((path, time) => Directory.SetLastWriteTime(path, time)),
                ((path) => Directory.GetLastWriteTime(path)),
                DateTimeKind.Local,
                "LastWriteTime_Local");
            yield return TimeFunction.Create(
                ((path, time) => Directory.SetLastWriteTimeUtc(path, time)),
                ((path) => Directory.GetLastWriteTimeUtc(path)),
                DateTimeKind.Unspecified,
                "LastWriteTime_Unspecified");
            yield return TimeFunction.Create(
                ((path, time) => Directory.SetLastWriteTimeUtc(path, time)),
                ((path) => Directory.GetLastWriteTimeUtc(path)),
                DateTimeKind.Utc,
                "LastWriteTime_Utc");
        }

        protected override string CreateSymlinkToItem(string item)
        {
            var link = item + ".link";
            if (Directory.Exists(link)) Directory.Delete(link);
            if (!MountHelper.CreateSymbolicLink(link, item, true) || !Directory.Exists(link)) throw new Exception("Could not create symlink.");
            return link;
        }
    }
}
