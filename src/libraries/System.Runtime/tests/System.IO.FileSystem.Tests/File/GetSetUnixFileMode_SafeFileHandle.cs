// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Tests
{
    public class File_GetSetUnixFileMode_SafeFileHandle : BaseGetSetUnixFileMode
    {
        protected override bool GetThrowsWhenDoesntExist => true;
        protected override bool GetModeNeedsReadableFile => true;

        protected override UnixFileMode GetMode(string path)
        {
            using SafeFileHandle fileHandle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return File.GetUnixFileMode(fileHandle);
        }

        protected override void SetMode(string path, UnixFileMode mode)
        {
            using SafeFileHandle fileHandle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            File.SetUnixFileMode(fileHandle, mode);
        }
    }
}
