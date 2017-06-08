// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.IO.Tests
{
    public class Directory_GetSetTimes : StaticGetSetTimes
    {
        public override string GetExistingItem() => Directory.CreateDirectory(GetTestFilePath()).FullName;

        public override IEnumerable<TimeFunction> TimeFunctions(bool requiresRoundtripping = false)
        {
            if (IOInputs.SupportsGettingCreationTime && (!requiresRoundtripping || IOInputs.SupportsSettingCreationTime))
            {
                yield return TimeFunction.Create(
                    ((path, time) => Directory.SetCreationTime(path, time)),
                    ((path) => Directory.GetCreationTime(path)),
                    DateTimeKind.Local);
                yield return TimeFunction.Create(
                    ((path, time) => Directory.SetCreationTimeUtc(path, time)),
                    ((path) => Directory.GetCreationTimeUtc(path)),
                    DateTimeKind.Unspecified);
                yield return TimeFunction.Create(
                    ((path, time) => Directory.SetCreationTimeUtc(path, time)),
                    ((path) => Directory.GetCreationTimeUtc(path)),
                    DateTimeKind.Utc);
            }
            yield return TimeFunction.Create(
                ((path, time) => Directory.SetLastAccessTime(path, time)),
                ((path) => Directory.GetLastAccessTime(path)),
                DateTimeKind.Local);
            yield return TimeFunction.Create(
                ((path, time) => Directory.SetLastAccessTimeUtc(path, time)),
                ((path) => Directory.GetLastAccessTimeUtc(path)),
                DateTimeKind.Unspecified);
            yield return TimeFunction.Create(
                ((path, time) => Directory.SetLastAccessTimeUtc(path, time)),
                ((path) => Directory.GetLastAccessTimeUtc(path)),
                DateTimeKind.Utc);
            yield return TimeFunction.Create(
                ((path, time) => Directory.SetLastWriteTime(path, time)),
                ((path) => Directory.GetLastWriteTime(path)),
                DateTimeKind.Local);
            yield return TimeFunction.Create(
                ((path, time) => Directory.SetLastWriteTimeUtc(path, time)),
                ((path) => Directory.GetLastWriteTimeUtc(path)),
                DateTimeKind.Unspecified);
            yield return TimeFunction.Create(
                ((path, time) => Directory.SetLastWriteTimeUtc(path, time)),
                ((path) => Directory.GetLastWriteTimeUtc(path)),
                DateTimeKind.Utc);
        }
    }
}
