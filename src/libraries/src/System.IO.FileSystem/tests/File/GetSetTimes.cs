// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

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
