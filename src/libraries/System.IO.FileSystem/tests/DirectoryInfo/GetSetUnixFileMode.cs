// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.IO.Tests
{
    public class DirectoryInfo_GetSetUnixFileMode : BaseGetSetUnixFileMode
    {
        protected override bool GetModeThrowsPNSE => false;

        protected override string CreateTestItem(string path = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            path =  path ?? GetTestFilePath(null, memberName, lineNumber);
            Directory.CreateDirectory(path);
            return path;
        }

        protected override UnixFileMode GetMode(string path)
            => new DirectoryInfo(path).UnixFileMode;

        protected override void SetMode(string path, UnixFileMode mode)
            => new DirectoryInfo(path).UnixFileMode = mode;
    }
}
