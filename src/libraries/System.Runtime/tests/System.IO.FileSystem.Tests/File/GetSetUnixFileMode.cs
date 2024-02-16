// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Tests
{
    public class File_GetSetUnixFileMode : BaseGetSetUnixFileMode
    {
        protected override bool GetThrowsWhenDoesntExist => true;

        protected override UnixFileMode GetMode(string path)
            => File.GetUnixFileMode(path);

        protected override void SetMode(string path, UnixFileMode mode)
            => File.SetUnixFileMode(path, mode);
    }
}
