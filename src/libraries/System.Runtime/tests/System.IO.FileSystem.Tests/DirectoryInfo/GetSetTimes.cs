// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.IO.Tests
{
    public class DirectoryInfo_GetSetTimes : InfoGetSetTimes<DirectoryInfo>
    {
        protected override bool CanBeReadOnly => false;

        protected override DirectoryInfo GetExistingItem(bool _) => Directory.CreateDirectory(GetTestFilePath());

        protected override DirectoryInfo GetMissingItem() => new DirectoryInfo(GetTestFilePath());

        protected override DirectoryInfo CreateSymlink(string path, string pathToTarget) => (DirectoryInfo)Directory.CreateSymbolicLink(path, pathToTarget);

        protected override string GetItemPath(DirectoryInfo item) => item.FullName;

        protected override void InvokeCreate(DirectoryInfo item) => item.Create();

        public override IEnumerable<TimeFunction> TimeFunctions(bool requiresRoundtripping = false)
        {
            if (IOInputs.SupportsGettingCreationTime && (!requiresRoundtripping || IOInputs.SupportsSettingCreationTime))
            {
                yield return TimeFunction.Create(
                    ((testDir, time) => {testDir.CreationTime = time; }),
                    ((testDir) => testDir.CreationTime),
                    DateTimeKind.Local);
                yield return TimeFunction.Create(
                    ((testDir, time) => {testDir.CreationTimeUtc = time; }),
                    ((testDir) => testDir.CreationTimeUtc),
                    DateTimeKind.Unspecified);
                yield return TimeFunction.Create(
                     ((testDir, time) => { testDir.CreationTimeUtc = time; }),
                     ((testDir) => testDir.CreationTimeUtc),
                     DateTimeKind.Utc);
            }
            yield return TimeFunction.Create(
                ((testDir, time) => {testDir.LastAccessTime = time; }),
                ((testDir) => testDir.LastAccessTime),
                DateTimeKind.Local);
            yield return TimeFunction.Create(
                ((testDir, time) => {testDir.LastAccessTimeUtc = time; }),
                ((testDir) => testDir.LastAccessTimeUtc),
                DateTimeKind.Unspecified);
            yield return TimeFunction.Create(
                ((testDir, time) => { testDir.LastAccessTimeUtc = time; }),
                ((testDir) => testDir.LastAccessTimeUtc),
                DateTimeKind.Utc);
            yield return TimeFunction.Create(
                ((testDir, time) => {testDir.LastWriteTime = time; }),
                ((testDir) => testDir.LastWriteTime),
                DateTimeKind.Local);
            yield return TimeFunction.Create(
                ((testDir, time) => {testDir.LastWriteTimeUtc = time; }),
                ((testDir) => testDir.LastWriteTimeUtc),
                DateTimeKind.Unspecified);
            yield return TimeFunction.Create(
                ((testDir, time) => { testDir.LastWriteTimeUtc = time; }),
                ((testDir) => testDir.LastWriteTimeUtc),
                DateTimeKind.Utc);
        }
    }
}
