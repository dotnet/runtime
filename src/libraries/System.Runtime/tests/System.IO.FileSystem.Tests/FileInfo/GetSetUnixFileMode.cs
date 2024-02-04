// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Tests
{
    public class FileInfo_GetSetUnixFileMode : BaseGetSetUnixFileMode
    {
        protected override bool GetModeThrowsPNSE => false;

        protected override UnixFileMode GetMode(string path)
            => new FileInfo(path).UnixFileMode;

        protected override void SetMode(string path, UnixFileMode mode)
            => new FileInfo(path).UnixFileMode = mode;
    }
}
