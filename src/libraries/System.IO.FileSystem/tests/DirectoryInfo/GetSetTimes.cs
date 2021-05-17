// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.IO.Tests
{
    public class DirectoryInfo_GetSetTimes : InfoGetSetTimes<DirectoryInfo>
    {
        protected override DirectoryInfo GetExistingItem() => Directory.CreateDirectory(GetTestFilePath());

        protected override DirectoryInfo GetMissingItem() => new DirectoryInfo(GetTestFilePath());

        protected override string GetItemPath(DirectoryInfo item) => item.FullName;

        protected override void InvokeCreate(DirectoryInfo item) => item.Create();

        public override IEnumerable<TimeFunction> TimeFunctions(bool requiresRoundtripping = false)
        {
            if (IOInputs.SupportsGettingCreationTime && (!requiresRoundtripping || IOInputs.SupportsSettingCreationTime))
            {
                yield return TimeFunction.Create(
                    ((testDir, time) => {testDir.CreationTime = time; }),
                    ((testDir) => testDir.CreationTime),
                    DateTimeKind.Local,
                    "CreationTime_Local");
                yield return TimeFunction.Create(
                    ((testDir, time) => {testDir.CreationTimeUtc = time; }),
                    ((testDir) => testDir.CreationTimeUtc),
                    DateTimeKind.Unspecified,
                    "CreationTime_Unspecified");
                yield return TimeFunction.Create(
                     ((testDir, time) => { testDir.CreationTimeUtc = time; }),
                     ((testDir) => testDir.CreationTimeUtc),
                     DateTimeKind.Utc,
                    "CreationTime_Utc");
            }
            yield return TimeFunction.Create(
                ((testDir, time) => {testDir.LastAccessTime = time; }),
                ((testDir) => testDir.LastAccessTime),
                DateTimeKind.Local,
                "LastAccessTime_Local");
            yield return TimeFunction.Create(
                ((testDir, time) => {testDir.LastAccessTimeUtc = time; }),
                ((testDir) => testDir.LastAccessTimeUtc),
                DateTimeKind.Unspecified,
                "LastAccessTime_Unspecified");
            yield return TimeFunction.Create(
                ((testDir, time) => { testDir.LastAccessTimeUtc = time; }),
                ((testDir) => testDir.LastAccessTimeUtc),
                DateTimeKind.Utc,
                "LastAccessTime_Utc");
            yield return TimeFunction.Create(
                ((testDir, time) => {testDir.LastWriteTime = time; }),
                ((testDir) => testDir.LastWriteTime),
                DateTimeKind.Local,
                "LastWriteTime_Local");
            yield return TimeFunction.Create(
                ((testDir, time) => {testDir.LastWriteTimeUtc = time; }),
                ((testDir) => testDir.LastWriteTimeUtc),
                DateTimeKind.Unspecified,
                "LastWriteTime_Unspecified");
            yield return TimeFunction.Create(
                ((testDir, time) => { testDir.LastWriteTimeUtc = time; }),
                ((testDir) => testDir.LastWriteTimeUtc),
                DateTimeKind.Utc,
                "LastWriteTime_Utc");
        }

        protected override DirectoryInfo CreateSymlinkToItem(DirectoryInfo item)
        {
            var link = new DirectoryInfo(item.FullName + ".link");
            if (link.Exists) link.Delete();
            bool failed = !MountHelper.CreateSymbolicLink(link.FullName, item.FullName, true);
            link.Refresh();
            if (failed || !link.Exists) throw new Exception("Could not create symlink.");
            return link;
        }
    }
}
